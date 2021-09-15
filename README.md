# TrainingVMCreator
This tool helps a trainer create Azure VMs for their training class.

## Setting up for testing

You will need an auth file in order for the application to authenticate with Azure. [How to create an auth file](https://github.com/Azure/azure-libraries-for-net/blob/master/AUTH.md).

```cmd
TrainingVMCreator.exe path_to_auth_file

TrainingVMCreator.exe C:\Users\admin\my.authfile
```

You can also place your `my.authfile` inside the `TrainingVMCreator` directory and it will use it automatically.
