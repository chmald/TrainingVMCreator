using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using System;
using System.Collections.Generic;
using Microsoft.Azure.Management.Network.Fluent;
using System.IO;

namespace TrainingVMCreator
{
    public class VMBuilder
    {
        private Region region = Region.USSouthCentral;
        private ImageInfo linuxImage = new ImageInfo() { Publisher = "canonical", Offer = "0001-com-ubuntu-server-focal", Sku = "20_04-lts" };
        private ImageInfo windowsImage = new ImageInfo() { Publisher = "MicrosoftWindowsServer", Offer = "WindowsServer", Sku = "2019-datacenter-gensecond" };
        private VirtualMachineSizeTypes vmSize = VirtualMachineSizeTypes.StandardD2sV3;
        private string resourceGroup = $"TrainingRG_{DateTime.Now.Year.ToString()}-{DateTime.Now.Month.ToString()}-{DateTime.Now.Day.ToString()}-{DateTime.Now.Millisecond.ToString()}";
        private string vmNetwork = $"VMNetwork_{DateTime.Now.Year.ToString()}-{DateTime.Now.Month.ToString()}-{DateTime.Now.Day.ToString()}-{DateTime.Now.Millisecond.ToString()}";
        private INetwork network;

        public VMBuilder(IAzure azure)
        {
            azureInstance = azure;
        }
        protected IAzure azureInstance;
        private List<VMInfo> vms = new List<VMInfo>();

        public void BuildVMs(int count, VM_OPTIONS options)
        {
            Console.WriteLine($"Build {count} VM(s) using option: {options}");
            Console.WriteLine($"Creating resource group: {resourceGroup}");
            azureInstance.ResourceGroups.Define(resourceGroup)
                .WithRegion(region)
                .Create();
            Console.WriteLine($"Creating network: {vmNetwork}");
            network = azureInstance.Networks.Define(vmNetwork)
                .WithRegion(region)
                .WithExistingResourceGroup(resourceGroup)
                .WithAddressSpace("10.0.0.0/16")
                .WithSubnet("vms", "10.0.0.0/22")
                .Create();

            while (count > 0)
            {
                switch (options)
                {
                    case VM_OPTIONS.BOTH:
                        CreateLinuxVM(count);
                        CreateWindowsVM(count);
                        break;
                    case VM_OPTIONS.LINUX_ONLY:
                        CreateLinuxVM(count);
                        break;
                    case VM_OPTIONS.WINDOWS_ONLY:
                        CreateWindowsVM(count);
                        break;
                }
                count--;
            }

            using(StreamWriter sw = File.CreateText(@"VMData.txt"))
            {
                foreach (VMInfo info in vms)
                {
                    Console.WriteLine("=============================");
                    sw.WriteLine("=============================");
                    Console.WriteLine($"VM for User {info.Number}");
                    sw.WriteLine($"VM for User {info.Number}");
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

        public void CreateLinuxVM(int number)
        {
            VMInfo vm = new VMInfo();
            vm.Number = number;
            vm.Username = "azureUser";
            vm.Password = SdkContext.RandomResourceName("Pa5$", 15);
            vm.VMName = SdkContext.RandomResourceName("LinuxVM", 30);

            Console.WriteLine($"Creating Linux VM: {vm.VMName}");
            var linuxVM = azureInstance.VirtualMachines.Define(vm.VMName)
                .WithRegion(region)
                .WithExistingResourceGroup(resourceGroup)
                .WithExistingPrimaryNetwork(network)
                .WithSubnet("vms")
                .WithPrimaryPrivateIPAddressDynamic()
                .WithNewPrimaryPublicIPAddress($"vmIP{vm.VMName}")
                .WithLatestLinuxImage(linuxImage.Publisher, linuxImage.Offer, linuxImage.Sku)
                .WithRootUsername(vm.Username)
                .WithRootPassword(vm.Password)
                .WithSize(vmSize)
                .Create();
            vm.IPAddress = linuxVM.GetPrimaryPublicIPAddress().IPAddress;
            
            vms.Add(vm);
        }

        public void CreateWindowsVM(int number)
        {
            VMInfo vm = new VMInfo();
            vm.Number = number;
            vm.Username = "azureUser";
            vm.Password = SdkContext.RandomResourceName("Pa5$", 15);
            vm.VMName = SdkContext.RandomResourceName("WinVM", 30);

            Console.WriteLine($"Creating Windows VM: {vm.VMName}");
            var linuxVM = azureInstance.VirtualMachines.Define(vm.VMName)
                .WithRegion(region)
                .WithExistingResourceGroup(resourceGroup)
                .WithExistingPrimaryNetwork(network)
                .WithSubnet("vms")
                .WithPrimaryPrivateIPAddressDynamic()
                .WithNewPrimaryPublicIPAddress($"vmIP{vm.VMName}")
                .WithLatestWindowsImage(windowsImage.Publisher, windowsImage.Offer, windowsImage.Sku)
                .WithAdminUsername(vm.Username)
                .WithAdminPassword(vm.Password)
                .WithSize(vmSize)
                .Create();
            vm.IPAddress = linuxVM.GetPrimaryPublicIPAddress().IPAddress;

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
