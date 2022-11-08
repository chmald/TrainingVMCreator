using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Models;
using Azure.ResourceManager.Resources.Models;
using Azure.ResourceManager.Compute.Models;
using Azure.ResourceManager.Network.Models;
using System;
using System.Collections.Generic;
using System.IO;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Compute;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Linq;

namespace TrainingVMCreator
{
    public class VMBuilder
    {
        private AzureLocation region = AzureLocation.SouthCentralUS;
        private ImageInfo linuxImage = new ImageInfo() { Publisher = "canonical", Offer = "0001-com-ubuntu-server-focal", Sku = "20_04-lts-gen2" };
        private ImageInfo windowsImage = new ImageInfo() { Publisher = "MicrosoftWindowsServer", Offer = "WindowsServer", Sku = "2022-datacenter-azure-edition" };
        private VirtualMachineSizeType vmSize = VirtualMachineSizeType.StandardD2SV3;
        private string resourceGroup = $"TrainingRG_{DateTime.Now.Year.ToString()}-{DateTime.Now.Month.ToString()}-{DateTime.Now.Day.ToString()}-{DateTime.Now.Millisecond.ToString()}";
        private string vmNetwork = $"VMNetwork_{DateTime.Now.Year.ToString()}-{DateTime.Now.Month.ToString()}-{DateTime.Now.Day.ToString()}-{DateTime.Now.Millisecond.ToString()}";
        private VirtualNetworkResource network;
        private ResourceGroupResource rgResource;

        public VMBuilder(ArmClient azure)
        {
            azureInstance = azure;
        }
        protected ArmClient azureInstance;
        private List<VMInfo> vms = new List<VMInfo>();

        public async Task BuildVMs(int count, VM_OPTIONS options)
        {
            var subscription = await azureInstance.GetDefaultSubscriptionAsync();
            var rgList = subscription.GetResourceGroups();

            Console.WriteLine($"Build {count} VM(s) using option: {options}");
            Console.WriteLine($"Creating resource group: {resourceGroup}");
            rgResource = (await rgList.CreateOrUpdateAsync(Azure.WaitUntil.Completed, resourceGroup, new ResourceGroupData(region))).Value;

            Console.WriteLine($"Creating network: {vmNetwork}");
            var vnetList = rgResource.GetVirtualNetworks();
            network = await vnetList.CreateOrUpdate(Azure.WaitUntil.Completed, vmNetwork, new VirtualNetworkData()
            {
                Location = region,
                AddressPrefixes = { "10.0.0.0/16" },
                Subnets = { new SubnetData() { Name = "vms", AddressPrefix = "10.0.0.0/24", } }
            }).WaitForCompletionAsync();

            while (count > 0)
            {
                switch (options)
                {
                    case VM_OPTIONS.BOTH:
                        await CreateVM(count, true);
                        await CreateVM(count);
                        break;
                    case VM_OPTIONS.LINUX_ONLY:
                        await CreateVM(count, true);
                        break;
                    case VM_OPTIONS.WINDOWS_ONLY:
                        await CreateVM(count);
                        break;
                }
                count--;
            }

            using (StreamWriter sw = File.CreateText(@"VMData.txt"))
            {
                vms.Reverse();
                foreach (VMInfo info in vms)
                {
                    Console.WriteLine("=============================");
                    sw.WriteLine("=============================");
                    Console.WriteLine($"VM for User {info.Number}");
                    sw.WriteLine($"VM for User {info.Number}");
                    Console.WriteLine($"VM Name: {info.VMName}");
                    sw.WriteLine($"VM Name: {info.VMName}");
                    Console.WriteLine($"IP Address: {info.IPAddress}");
                    sw.WriteLine($"IP Address: {info.IPAddress}");
                    Console.WriteLine($"User: {info.Username}");
                    sw.WriteLine($"User: {info.Username}");
                    Console.WriteLine($"Pass: {info.Password}");
                    sw.WriteLine($"Pass: {info.Password}");
                    Console.WriteLine("=============================");
                    sw.WriteLine("=============================");
                }
            }

            Console.WriteLine($"File saved in directory: {Path.GetFullPath(Directory.GetCurrentDirectory())}\\VMData.txt");
        }

        public async Task<NetworkInterfaceResource> CreateInterface(string vmname)
        {
            var publicIpAddressCollection = rgResource.GetPublicIPAddresses();
            var publicIpData = new PublicIPAddressData()
            {
                Location = region,
                PublicIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
            };
            var ipResource = await publicIpAddressCollection.CreateOrUpdate(Azure.WaitUntil.Completed, vmname + "ip", publicIpData).WaitForCompletionAsync();

            var interfaceCollection = rgResource.GetNetworkInterfaces();
            var interfaceData = new NetworkInterfaceData()
            {
                Location = region,
                IPConfigurations =
                {
                    new NetworkInterfaceIPConfigurationData()
                    {
                        Name = "ipConfig",
                        PrivateIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                        PublicIPAddress = new PublicIPAddressData()
                        {
                            Id = ipResource.Value.Id
                        },
                        Subnet = new SubnetData()
                        {
                            Id = network.Data.Subnets[0].Id
                        }
                    }
                }
            };

            var interfaceResource = await interfaceCollection.CreateOrUpdate(Azure.WaitUntil.Completed, vmname + "interface", interfaceData).WaitForCompletionAsync();
            return interfaceResource.Value;
        }

        public async Task CreateVM(int number, bool isLinux = false)
        {
            VMInfo vm = new VMInfo();
            vm.Number = number;
            vm.Username = "azureUser";
            vm.Password = NameGen.RandomResourceName("Pa5$", 15);
            var storageProfile = new VirtualMachineStorageProfile();
            var osProfile = new VirtualMachineOSProfile()
            {
                AdminUsername = vm.Username,
                AdminPassword = vm.Password,
            };

            if (isLinux)
            {
                vm.VMName = NameGen.RandomResourceName("LinuxVM", 8);
                Console.WriteLine($"Creating Linux VM: {vm.VMName}");
                storageProfile.OSDisk = new VirtualMachineOSDisk(DiskCreateOptionType.FromImage)
                {
                    OSType = SupportedOperatingSystemType.Linux,
                    Caching = CachingType.ReadWrite,
                    ManagedDisk = new VirtualMachineManagedDisk()
                    {
                        StorageAccountType = StorageAccountType.StandardLrs
                    }
                };
                storageProfile.ImageReference = new ImageReference()
                {
                    Publisher = linuxImage.Publisher,
                    Offer = linuxImage.Offer,
                    Sku = linuxImage.Sku,
                    Version = "latest"
                };
            }
            else
            {
                vm.VMName = NameGen.RandomResourceName("WinVM", 8);
                Console.WriteLine($"Creating Windows VM: {vm.VMName}");
                osProfile.WindowsConfiguration = new WindowsConfiguration()
                {
                    PatchSettings = new PatchSettings()
                    {
                        PatchMode = WindowsVmGuestPatchMode.AutomaticByPlatform
                    }
                };
                storageProfile.OSDisk = new VirtualMachineOSDisk(DiskCreateOptionType.FromImage)
                {
                    OSType = SupportedOperatingSystemType.Windows,
                    Caching = CachingType.ReadWrite,
                    ManagedDisk = new VirtualMachineManagedDisk()
                    {
                        StorageAccountType = StorageAccountType.StandardLrs
                    }
                };
                storageProfile.ImageReference = new ImageReference()
                {
                    Publisher = windowsImage.Publisher,
                    Offer = windowsImage.Offer,
                    Sku = windowsImage.Sku,
                    Version = "latest"
                };
            }
            osProfile.ComputerName = vm.VMName;
            var vmCollection = rgResource.GetVirtualMachines();
            var vmData = new VirtualMachineData(region)
            {
                HardwareProfile = new VirtualMachineHardwareProfile()
                {
                    VmSize = vmSize
                },
                OSProfile = osProfile,
                NetworkProfile = new VirtualMachineNetworkProfile()
                {
                    NetworkInterfaces =
                    {
                        new VirtualMachineNetworkInterfaceReference()
                        {
                            Id = CreateInterface(vm.VMName).Result.Id,
                            Primary = true
                        }
                    }
                },
                StorageProfile = storageProfile
            };

            var vmResource = await vmCollection.CreateOrUpdateAsync(Azure.WaitUntil.Completed, vm.VMName, vmData);
            vm.IPAddress = rgResource.GetPublicIPAddressAsync(vm.VMName + "ip").Result.Value.Data.IPAddress;

            vms.Add(vm);
        }
    }

    public enum VM_OPTIONS
    {
        BOTH,
        LINUX_ONLY,
        WINDOWS_ONLY
    }
}
