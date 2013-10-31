# Azure Self Destruct

A library allowing an Azure WebRole/WorkerRole to delete itself from the current deployment.


This will effectively scale down your infrastructure, by powering down the current machine.


This does not work in the emulator.

## Installation

You can just copy the `SelfDestruct.cs` file into your project. 

A package is also available on [NuGet](https://www.nuget.org/packages/Two10.Azure.SelfDestruct/).

```
PM> Install-Package Two10.Azure.SelfDestruct
```

## Usage

To start you need a Publish Settings file which can be downloaded [here](http://go.microsoft.com/fwlink/?LinkId=254432).


If the file contains more than one subscription, you'll need to remove the other subscriptions such that you only have the one in there you want to use.


To delete a machine, simply call 'DeleteInstance':


```c#
// immediate annihilation
SelfDestruct.DeleteInstance(@".\path\to\file.publishsettings");
```


Like all good self-destruction routines, you can also have a countdown:


```c#
// gone in 60 seconds
SelfDestruct.DeleteInstance(@".\path\to\file.publishsettings", TimeSpan.FromSeconds(60));
```

If your deployment only contains one instance, it won't be deleted. You'll need to delete the whole deployment instead.


## Acknowledgements

[Gaurav Mantri](http://gauravmantri.com/) for his code samples.


Richie Custance for his ideas.

## License

MIT
