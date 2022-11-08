using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Identity;

namespace TrainingVMCreator
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                if (File.Exists(args[0]))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Using auth file passed in args.");
                    Console.ResetColor();
                }
            }
            else if (!File.Exists(Directory.GetCurrentDirectory() + "\\my.azureauth"))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("No auth file in directory.");
                Console.WriteLine("Please ensure you have created a my.azureauth file in your application directory.");
                Console.ResetColor();
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
                return;
            }

            bool countValid = false;
            int count = 0;
            while(!countValid)
            {
                Console.WriteLine("How many VMs to create?");
                var entry = Console.ReadLine();
                if(int.TryParse(entry, out count))
                {
                    if(count > 10)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Are you sure you want to create {count} VMs? (yes/no)");
                        Console.ResetColor();
                        string input = Console.ReadLine();
                        if(input.ToLower() == "yes")
                        {
                            countValid = true;
                        }
                    } else
                    {
                        countValid = true;
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Value entered is not a valid number.");
                    Console.ResetColor();
                }
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Creating {count} VM(s).");
            Console.ResetColor();

            bool optionsValid = false;
            VM_OPTIONS options = VM_OPTIONS.BOTH;
            while(!optionsValid)
            {
                int option = 0;
                Console.WriteLine("Please select one of the following VM options.");
                Console.WriteLine("1 - Create BOTH Windows and Linux per user. (default)");
                Console.WriteLine("2 - Create Linux Only per user.");
                Console.WriteLine("3 - Create Windows Only per user.");
                var entry = Console.ReadLine();
                if(int.TryParse(entry, out option))
                {
                    if(option > 0 && option <= 3)
                    {
                        switch(option)
                        {
                            case 1:
                                options = VM_OPTIONS.BOTH;
                                break;
                            case 2:
                                options = VM_OPTIONS.LINUX_ONLY;
                                break;
                            case 3:
                                options = VM_OPTIONS.WINDOWS_ONLY;
                                break;
                            default:
                                options = VM_OPTIONS.BOTH;
                                break;
                        }
                        optionsValid = true;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Please select a valid option (1-3).");
                        Console.ResetColor();
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Please select a valid option (1-3).");
                    Console.ResetColor();
                }
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Creating VMS: {options}");
            Console.ResetColor();

            try
            {
                var authFile = (args.Length > 0) ? args[0] : Directory.GetParent(AppContext.BaseDirectory).FullName + "\\my.azureauth";
                ClientSecretCredential credentials = AzAuthHelper.AcquireAzureCredentials(authFile);

                Console.WriteLine("Logging into Azure.");
                var azure = AzAuthHelper.LogIntoAzure(credentials);

                VMBuilder vmBuilder = new VMBuilder(azure);
                vmBuilder.BuildVMs(count, options).Wait();

                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: " + ex.Message);
                Console.ResetColor();
            }
        }
    }
}
