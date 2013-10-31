# Azure Self Destruct

A library / single class allowing a WebRole/WorkerRole to delete itself from the deployment.


This will effectively scale down your infrastructure, and power off the current machine.


This does not work in the emulator.


To start, you need a Publish Settings file, which can be downloaded [here](http://go.microsoft.com/fwlink/?LinkId=254432).


If the file contains more than one subscription, it is recommended to remove other subscriptions, such that you only have the one in there you want to use.


To delete a machine, simply call 'DeleteInstance':


```c#
// immediate annihilation
SelfDestruct.DeleteInstance(@'.\Path\To\File.publishsettings');
```


Like all good self-destruction routines, you can also have a countdown:


```c#
// gone in 60 seconds
SelfDestruct.DeleteInstance(@'.\Path\To\File.publishsettings', TimeSpan.FromSeconds(60));
```


# Acknowledgements


Gaurav Mantri for his code samples [http://gauravmantri.com/].


Richie Custance for his ideas.

