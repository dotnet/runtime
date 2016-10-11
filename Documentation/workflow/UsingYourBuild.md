
# Using your .NET Core Build

We assume that you have successfully built .NET Core Repository and thus have file of the form  
```
    bin\Product\<OS>.<arch>.<flavor>\.nuget\pkg\Microsoft.NETCore.Runtime.CoreCLR.<version>.nupkg
```
And now you wish to try it out.  We will be using Windows OS as an example and thus will use \ rather
than / for directory separators and things like Windows_NT instead of Linux but it should be
pretty obvious how to adapt these instructions for other operating systems.  

To run your newly built .NET Core Runtime in addition to the application itself, you will need
you need a 'host' program that will load the Runtime as well as all the other .NET Framework code
that your application needs.   The easiest way to get all this other stuff is to simply use the 
standard 'dotnet' host that installs with .NET Core SDK. 

Thus the first step is to confirm that you have the 'standard' .NET Core SDK installed.  If 
you can type

* dotnet -?

and it prints some help text, you are ready.  Otherwise 
see [Installing the .Net Core SDK](https://www.microsoft.com/net/core) to install it.

### Step 1: Create a App using the Default Runtime
At this point you can create a new 'Hello World' program in the standard way. 

```bat
mkdir HelloWorld
cd HelloWorld
dotnet new 
```

### Step 2: Get the Version number of the CoreCLR package you built.   

This makes a 'standard' hello world application but uses the .NET Core Runtime version that
came with the dotnet.exe tool.   First you need to modify your app to ask for the .NET Core
you have built, and to do that, we need to know the version number of what you built.  Get
this by simply listing the name of the Microsoft.NETCore.Runtime.CoreCLR you built. 

```bat
    dir bin\Product\Windows_NT.x64.Release\.nuget\pkg
```

and you will get name of the which looks something like this

```
    Microsoft.NETCore.Runtime.CoreCLR.1.2.0-beta-24528-0.nupkg
```

This gets us the version number, in the above case it is 1.2.0-beta-24528-0.   We will 
use this in the next step.   

### Step 3: Modify the Project.json for the App to refer to your Runtime.     

Now Modify the HelloWorld\project.json with the following modifications

1. **Remove** (or comment out) the following line from the Microsoft.NETCore.App dependency 
This tells the build system that you don't want to use runtime and libraries that came with
the dotnet.exe tool but to fetch the dependencies from the Nuget cache.  If you don't do this
the tools will ignore your request to make the app use an explicitly specified runtime.   
```
        "type": "platform",
``` 
2. Add the following 'runtimes' line at the top level.  The runtime name includes the OS name and the architecture name
you can find the appropriate name for your OS [here](https://github.com/dotnet/core-docs/blob/master/docs/core/rid-catalog.md).
This tells the tools exactly which flavor of OS and processor architecture you are running on, so it can find the right
Nuget package for the runtime.    
```
        "runtimes": { "win7-x64": {} }
```
3. Add the following line to the dependencies section.  This is where you need the version number 
for your build of the runtime.
This is the line that tells the tools that you want YOUR version of the CoreCLR runtime.    
```
       "Microsoft.NETCore.Runtime.CoreCLR": "1.2.0-beta-24528-0"
```
4. Be sure to make sure you have all the commas you need when you add the lines to make it valid JSON.  

You should end up with something that looks something like this.  

```javascript
{
  "version": "1.0.0-*",
  "buildOptions": {
    "debugType": "portable",
    "emitEntryPoint": true
  },
  "dependencies": {},

  "frameworks": {
    "netcoreapp1.0": {
      "dependencies": {
        "Microsoft.NETCore.App": {
          // REMOVED "type": "platform",
          "version": "1.0.0"
        },
       "Microsoft.NETCore.Runtime.CoreCLR": "1.2.0-beta-24528-0" // NEW, including comma before
      },
      "imports": "dnxcore50"
    }
  },
  "runtimes": { // NEW including comma before.  
    "win7-x64": {}
  },
}
```

### Step 4: Place your build directory on your Nuget Path

You can do this by creating a file named Nuget.Config in the 'HelloWorld' directory with the following XML 
Obviously **you need to update path in the XML to be the path to output directory for your build**.   
On Windows you also have the alternative of modifying the Nuget.Config 
at %HOMEPATH%\AppData\Roaming\Nuget\Nuget.Config (~/.nuget/NuGet/NuGet.Config on Linux) with the new location.   
This will allow your new 
runtime to be used on any 'dotnet restore' run by the current user. 
Alternatively you can skip creating this file and pass the path to your package directory using 
the -s SOURCE qualifer on the dotnet restore command below.   The important part is that somehow 
you have told the tools where to find your new package.  

```xml
<configuration>
  <packageRestore>
    <add key="enabled" value="True" />
  </packageRestore>
  <packageSources>
    <add key="Local CoreCLR" value="C:\Users\User\Source\Repos\coreclr-vancem\bin\Product\Windows_NT.x64.Release\.nuget\pkg" /> 
  </packageSources>
  <activePackageSource>
    <add key="All" value="(Aggregate source)" />
  </activePackageSource>
</configuration>
```

### Step 5: Restore the Nuget Packages for your application

This consist of simply running the command
```
     dotnet restore 
```
which should find the .NET Runtime package in your build output and unpacks it to the local Nuget cache (on windows this is in %HOMEPATH%\.nuget\packages) 


### Step 6: Run your application 

You can run your 'HelloWorld' applications by simply executing the following in the 'HelloWorld' directory.  

```
     dotnet run 
```
This will compile and run your app.   What the command is really doing is building files in helloWorld\bin\Debug\netcoreapp1.0\win7-x64\ 
and the runing 'dotnet helloWorld\bin\Debug\netcoreapp1.0\win7-x64\HelloWorld.dll' to actually run the app. 

### Step 6: (Optional) Publish your application

In Step 5 you will notice that the helloWorld\bin\Debug\netcoreapp1.0\win7-x64 does NOT actually contain your Runtime code.  
What is going on is that runtime is being loaded directly out of the local Nuget cache (on windows this is in %HOMEPATH%\.nuget\packages).
The app can find this cache because of the HelloWorld.runtimeconfig.dev.json file which specifies that that this location shoudl be
added to the list of places to look for dependencies.     

This setup fine for development time, but is not a reasonable way of allowing end users to use your new runtime.   Instead what
you want all the necessary code to be gather up so that the app is self-contained.   This is what the following command does. 
```
     dotnet publish 
```
After running this in the 'HelloWorld' directory you will see that the following path

* helloWorld\bin\Debug\netcoreapp1.0\win7-x64\publish

Has all the binaries needed, including the CoreCLR.dll and System.Private.CoreLib.dll that you build locally.  To
run the application simple run the EXE that is in this publish directory (it is the name of the app, or specified
in the project.json file).   Thus at this point this directory has NO dependency outside this publication directory
(including dotnet.exe).   You can copy this publication directory to another machine and run( the exe in it and
will 'just work'.   Note that your managed app's code is still in the 'app'.dll file, the 'app'.exe file is 
actually simply a rename of dotnet.exe.   

### Step 7: (Optional) Confirm that the app used your new runtime

Congratulations, you have successfully used your newly built runtime.   To confirm that everything worked, you 
should compare the file creation timestamps for the CoreCLR.dll and System.Private.Runtime.dll in the publishing
directory and the build output directory.  They should be identical.   If not, something went wrong and the
dotnet tool picked up a different version of your runtime.  

### Step 8: Update BuildNumberMinor Environment Variable!

One possible problem with the technique above is that Nuget assumes that distinct builds have distinct version numbers.
Thus if you modify the source and create a new NuGet package you must it a new version number and use that in your 
application's project.json.   Otherwise the dotnet.exe tool will assume that the existing version is fine and you 
won't get the updated bits.   This is what the Minor Build number is all about.  By default it is 0, but you can
give it a value by setting the BuildNumberMinor environment variable. 
```bat
    set BuildNumberMinor=3
```
before packaging.   You should see this number show up in the version number (e.g. 1.2.0-beta-24521-03).

As an alternative you can delete the existing copy of the package from the Nuget cache.   For example on 
windows (on Linux substitute ~/ for %HOMEPATH%) you could delete 
```bat
     %HOMEPATH%\.nuget\packages\Microsoft.NETCore.Runtime.CoreCLR\1.2.0-beta-24521-02
```
which should mke things work (but is fragile, confirm wile file timestamps that you are getting the version you expect)


## Step 8.1 Quick updates in place.  

The 'dotnet publish' step in step 6 above creates a directory that has all the files necessary to run your app
including the CoreCLR and the parts of CoreFX that were needed.    You can use this fact to skip some steps if
you wish to update the DLLs.   For example typically when you update CoreCLR you end up updating one of two DLLs

* Coreclr.dll - Most modifications (with the exception of the JIT compiler and tools) that are C++ code update
  this DLL. 
* System.Private.CoreLib.dll - If you modified C# it will end up here.  
* System.Private.CoreLib.ni.dll - the native image (code) for System.Private.Corelib.   If you modify C# code
you will want to update both of these together in the target installation.  

Thus after making a change and building, you can simply copy the updated binary from the `bin\Product\<OS>.<arch>.<flavor>`
directory to your publication directory (e.g. `helloWorld\bin\Debug\netcoreapp1.0\win7-x64\publish`) to quickly
deploy your new bits.   


### Using your Runtime For Real.  

You can see that it is straightforward for anyone to use your runtime.  They just need to modify their project.json 
and modify their NuGet search path.   This is the expected way of distributing your modified runtime.  

--------------------------
## Using CoreRun to run your .NET Core Application

Generally using dotnet.exe tool to run your .NET Core application is the preferred mechanism to run .NET Core Apps.
However there is a simpler 'host' for .NET Core applications called 'CoreRun' that can also be used.   The value
of this host is that it is simpler (in particular it knows nothing about NuGet), but precisely because of this
it can be harder to use (since you are responsible for insuring all the dependencies you need are gather together)
See [Using CoreRun To Run .NET Core Application](UsingCoreRun.md) for more.  
