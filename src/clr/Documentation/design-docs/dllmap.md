# Dllmap design document
This document is intended to describe a plan on delivering a Dllmap feature for .NET Core.

Author: Anna Aniol (@annaaniol)

## Background

### .NET Core P/invoke mechanism
There is a .NET directive ([DllImport](https://msdn.microsoft.com/en-us/library/system.runtime.interopservices.dllimportattribute(v=vs.110).aspx)) indicating that the attributed method is exposed by an unmanaged DLL as a static entry point. It enables to call a function exported from an unmanaged DLL inside a managed code. To make such a call, library name must be provided. Names of native libraries are OS-specific, so once the name is defined, the call can be executed correctly on one OS only.

Right now, CoreCLR has no good way to handle the differences between platforms when it comes to p/invoking native libraries. Here is an example usage of DllImport:
```c#
// Use DllImport to import the Win32 MessageBox function.
[DllImport("user32.dll", CharSet = CharSet.Unicode)]
public  static  extern  int  MessageBox(IntPtr  hWnd, String text, String caption, uint type);
```
This import works with Windows, but it doesn’t work with any other OS. If run e.g. on Linux, a DllNotFoundException will be thrown, which means that a DLL specified in a DLL import cannot be found.

### Mono’s Dllmap

Mono already provides a feature that addresses the problem of cross-platform p/invoke support. Mono’s [Dllmap](http://www.mono-project.com/docs/advanced/pinvoke/dllmap/) 
enables to configure p/invoke signatures at runtime. By providing an XML configuration file, 
user can define a custom mapping between OS-specific library names and methods. 
Thanks to that, even if a library defined in DllImport is incompatible with an OS that is currently running the application, 
a correct unmanaged method can be called (if it exists for this OS).

In Mono Dllmap feature custom mapping can be tightly specified based on the OS name, CPU name and a wordsize.

This simple Mono example maps references to the cygwin1.dll library to the libc.so.6 file:
```xml
<configuration>
    <dllmap  dll="cygwin1.dll" target="libc.so.6"/>
</configuration>
```

Mono dllmap logic is implemented in [metadata/mono-config.c](https://github.com/mono/mono/blob/master/mono/metadata/mono-config.c) 
and [metadata/loader.c](https://github.com/mono/mono/blob/master/mono/metadata/loader.c) files.

## Expectations

### .NET Core Dllmap

.NET Core Dllmap’s purpose is to deliver a cross-platform support for p/invoke mechanism in .NET. With Dllmap user will be able to control interop methods by defining custom mapping between OS-specific dlls and methods.

Dllmap will allow making changes in both library names and target method names (entrypoints). Changing entrypoint name will be optional (it will remain unchanged by default).

Target platforms for this feature are: Windows, Linux and OS X.

There will be a diagnostic mechanism available to monitor dllmap related issues.

### Interaction with Dllmap

#### Mono users: Mono compatibility
The Dllmap method is meant as a compatibility feature for Mono and provides users with a straightforward migration story from Mono to .NET Core applications having p/invokes.
The default dllmap will consume a configuration file of [the same style](http://www.mono-project.com/docs/advanced/pinvoke/dllmap/) as Mono does.
Users will be able to use their old Mono configuration files when specifying the mapping for .NET Core applications.
Configuration files must be placed next to the assemblies that they describe.

#### New users: flexibility
New users, who plan to support p/invokes in their cross-platform applications, should implement their custom mapping policies that satisfies their needs.
The runtime will use two dll specific callbacks on each dll load attempt (one on loading a library, one on determining an entrypoint).
The user’s code can subscribe to these callbacks and define any mapping strategy.
Users should keep in mind that the default dllmap methods are provided for an easier migration from Mono.
For newcomers, it’s highly recommended to use callbacks and implement their own handers. Details of the callback strategy are described in the Design section.

### Usage example (XML configuration)

Let’s say there is a customer developing a .NET application with p/invokes on Windows. The customer wants the application to run on both Windows and Linux without introducing any changes in the code.

The application calls a function GetCurrentProcessId from an OS-specific library kernel32.dll.
```c#
[DllImport("kernel32.dll")]
static  extern  uint  GetCurrentProcessId();
```

To make it work on Linux, that does not have kernel32.dll, user must define a mapping of the dll. 
There is no `GetCurrentProcessId` function in any corresponding Linux-specific library, so entrypoint name mapping must be defined too. 
To achieve this, the user puts an XML configuration file next to the dll that is about to be loaded. The file looks like this:
```xml
<configuration>
    <dllmap  dll="kernel32.dll">
        <dllentry  dll="libc.so.6" name="GetCurrentProcessId" target="getpid" />
    </dllmap>
</configuration>
```

With this file, all `GetCurrentProcessId` calls get automatically mapped to `getpid` calls on runtime and the end user of the application can’t see any difference in application’s behavior. Running the application cross-platform does not require any OS-specific changes in the code. All the mapping is defined in advance in the external configuration file.

When mapping a function into another function, both the source and the target functions must take the same number of arguments of compatible type. Otherwise, the mapping will not work.

This is a very basic scenario and it can be extended to different operating systems, libraries and entrypoints. 
It assumes that user does not implement any custom actions (handlers) but uses the default Mono-like dllmap behavior.

## Design
### XML configuration file
For a basic case, the mapping must be defined in an XML configuration file and placed next to the assembly that requires mapping of p/invokes. The file must be named AssemblyName.config where AssemblyName is a name of the executable for which the mapping is defined. 

XML parsing will be implemented in corefx.labs using XML parsers that .NET provides. 

### Library mapping
In [dllimport.cpp](https://github.com/dotnet/coreclr/blob/master/src/vm/dllimport.cpp) file there is a method that loads the DLL and finds the procaddress for an N/Direct call.
```c++
VOID NDirect::NDirectLink(NDirectMethodDesc *pMD)
{
    …
    HINSTANCE hmod = LoadLibraryModule( pMD, &errorTracker );
    …
    LPVOID pvTarget = NDirectGetEntryPoint(pMD, hmod);
    …
    pMD->SetNDirectTarget(pvTarget);
}
```
`LoadLibraryModule`  is responsible for loading a correct library and `NDirectGetEntryPoint ` is responsible for resolving a right entrypoint.

There are several functions that get called in `LoadLibraryModule`  to get an hmod of the unmanaged dll. If any of them returns a valid hmod, execution flow ends and the hmod gets returned. First line presents the proposed change:
```c++
hmod = LoadLibraryViaCallback(pMD, wszLibName); // this is the only intoduced step
hmod = LoadLibraryModuleViaHost(pMD, pDomain, wszLibName);
hmod = FindUnmanagedImageInCache(wszLibName)
If FEATURE_CORESYSTEM:
    hmod = LocalLoadLibraryHelper(wszLibName, LOAD_LIBRARY_SEARCH_SYSTEM32, pErrorTracker);
FOR currLibNameVariation IN VARIATIONS:
    hmod = LoadFromNativeDllSearchDirectories(pDomain, currLibNameVariation, loadWithAlteredPathFlags, pErrorTracker)
    IF !libNameIsRelativePath:
        hmod = LocalLoadLibraryHelper(currLibNameVariation, flags, pErrorTracker)
    ELSE IF searchAssemblyDirectory:
        hmod = LoadFromPInvokeAssemblyDirectory(pAssembly, currLibNameVariation, loadWithAlteredPathFlags | dllImportSearchPathFlag, pErrorTracker)
    hmod = LocalLoadLibraryHelper(currLibNameVariation, dllImportSearchPathFlag, pErrorTracker)
hmod = LocalLoadLibraryHelper(pModule->GetPath(), loadWithAlteredPathFlags | dllImportSearchPathFlag, pErrorTracker)
```
`LoadLibraryModuleViaHost`  already contains a callback (`AssemblyLoadContext` exposes `LoadUnmanagedDll()` API to load a dll but it can be used for a 
`CustomAssemblyLoadContext` only, not the default one).  `LoadLibraryViaCallback`  will do  a callback for all assemblies except `System.Private.CoreLib`, 
which can’t be mapped at any time. The check to determine if the assembly is `System.Private.CoreLib` will happen on runtime in the unmanaged code.

### Entrypoint mapping
Once hmod gets resolved and returned via `LoadLibraryModule`, an entrypoint must be find:

Currently `NDirectGetEntryPoint` takes two arguments: `pMD` and hmod. When `NDirectGetEntryPoint`  gets called, 
`pMD `points to a target dll and hmod is correlated with a target dll too. The entrypoint name mapping will be done at the beginning of `NDirectGetEntryPoint`. 
A new method ` IntPtr  GetMappedEntrypoint(sourceEntrypointName)`  will be a callback and will return a mapping for an entrypoint if it exists. 
Similarly to library mapping, the callback will be done for all assemblies except `System.Private.CoreLib`.
`pMD` will get updated to point to a target entrypoint. The rest of `NDirectGetEntryPoint()`  flow will remain the same.
```c++
HINSTANCE hmod = LoadLibraryModule( pMD, &errorTracker );
if ( hmod )
{
    LPVOID pvTarget = GetEntrypointViaCallback(pMD, hmod); // this is the only introduced step
    if (!pvTarget)
    {
        LPVOID pvTarget = NDirectGetEntryPoint(pMD, hmod);
    }
    …
}
```

In consequence, after `LoadLibraryModule()`  and `GetEntrypointViaCallback() / NDirectGetEntryPoint()`  execution, `pMD` will get updated: `pMD->SetNDirectTarget(pvTarget)` with a correctly mapped `pvTarget`.

### Callbacks

As explained above, runtime will rise two dll specific events on each load attempt:
* 1st callback - when loading a non-system library
* 2nd callback - when finding an entrypoint

Events will be defined in `AssemblyLoadContext` in `System.Private.CoreLib`. Default handlers that subscribe to dll load events will implement the mono-based dllmap logic.
They will take string as argument and return IntPtr of target libraries and entrypoints based on the parsed XML configuration file.

Handlers implementation will stay in `corefx.labs`. Load library resolver will cache all the dll mapping results that got resolved (as key-value: IntPtr-hmod pairs). 
Thanks to that, the same library won't get loaded multiple times. User’s code will be able to subscribe to events and implement any loading behavior. 
That will give a user full flexibility when using dllmap and won’t limit defining the mapping to only xml-based style. 
Callbacks can be executed for all assemblies except `System.Private.CoreLib`.

We do not plan to support unsubscribing from events at this point.

### Resolution flow

**User’s code [managed code]**

-   Includes `using System.Runtime.Dllmap`
-   Subscribes to `LoadNativeLibrary` and `LoadNativeEntrypoint` events with their default or custom handler
-   Uses DllImport directive and does the p/invoke
	 ```c#
    using System.Runtime.Dllmap;
	…
	System.Runtime.Loader.AssemblyLoadContext.Default.LoadNativeLibrary += LoadLibraryCustomHandler;
	System.Runtime.Loader.AssemblyLoadContext.Default.LoadNativeEntrypoint += LoadEntrypointCustomHandler;
	…
    [DllImport("MyLibrary.dll", EntryPoint="MyFunction")]
	static  extern  int  MyFunction();
	…
	MyFunction();
	```

**Runtime [unmanaged code]**
-   Calls `LoadLibraryModuleViaCallback` that raises `LoadNativeLibrary` event
-   Calls `GetEntrypointViaCallback` that raises `LoadNativeEntrypoint` event
    
**AssemblyLoadContext [unmanaged code]**
-   Defines `LoadNativeLibrary` and `LoadNativeEntrypoint` and exposes an API:
	```c#
	IntPtr  LoadNativeLibrary(string libraryName)
	IntPtr  LoadNativeEntrypoint(string entrypointName, HMOD hmod)
	```

**corefx.labs [managed code]**
-   Implements default handlers - `LoadLibraryCustomHandler` and `LoadEntrypointCustomHandler`
-   To avoid infinite looping, `LoadLibraryCustomHandler` takes a lock and releases it after the default library loading process is completed
	```c#
	IntPtr  LoadLibraryCustomHandler(string libraryName)
	{
        private Object dllLock = new Object(); 

        lock(dllLock)
        {  
            if (libraryName in cachedResults)
                return cachedResults[libraryName];
            if (!mapStructure)
                mapStructure = ReadAndParseXML();
            targetLibraryName = mapStructure.GetLibrary(libraryName);
            hmod = LoadLibrary(targetLibraryName);
            AddToCache(libraryName, hmod);
        }

        return hmod;
	}
	```
	```c#
	IntPtr  LoadEntrypointCustomHandler(string entrypointName, HMOD hmod)
	{
        targetEntrypointName = mapStructure.GetEntrypoint(entrypointName);
        pvTarget = GetProcAddress(targetEntrypointName, hmod);
        return pvTarget;
	}
	```

## Testing

To verify if Dllmap’s behavior is correct, cross-platform tests will be run: [Mono tests](https://github.com/mono/mono/tree/0bcbe39b148bb498742fc68416f8293ccd350fb6/mcs/tools/mono-shlib-cop) and custom tests created on purpose of this feature.
Custom tests must contain calls to native libraries. It doesn’t really matter what functions from which libraries get called, because the feature is about mapping things independently from method and library exact names. There are multiple dimensions that must be included in tests:

**OS**. When it comes to the OS, there are three main target platforms for this feature: Windows, Linux and OS X. All the Linux based systems are so architecturally similar, so they are named just “Linux” in this document. Ubuntu 16.04LTS will be used for testing.
Test cases:

|Develop (initial dll)  |Run (target dll)|
|--|--|
|Windows |Windows  |
|Windows |Linux |
|Windows |OS X |
|Linux | Windows|
|Linux |OS X |
|Linux |OS X |
|OS X |Windows|
|OS X |Linux |
|OS X |OS X

**System's architecture** – test cases:

|Develop |Run |
|--|--|
|32-bit |32-bit|
|32-bit|64-bit|
|64-bit|32-bit |
|64-bit| 32-bit|

**Path mapping** – test cases:

|Initial dll path |Target dll path|
|--|--|
|absolute |absolute|
|absolute|relative|
|relative|absolute |
|relative| relative|
 
**Dll naming** – test cases:

|Initial dll name |Target dll nanme|
|--|--
|with extension (foo.dll)|with extension|
|with extension|no extension|
|no extension (foo)|with extension|
|no extension| no extension|

**Resistance to errors in the config file** - test cases:
- correct config file
- config file that can't be parsed  &rightarrow; 
log a warning, ignore the mapping for the corresponding assembly &rightarrow;  on some platforms (where mapping is not required) execute application, on some throw DllNotFoundException
- config file pointing to a dll/entrypoint that can't be found &rightarrow; on the affected platforms throw a DllNotFoundException

All the above test cases will be covered.

### Related discussions
[Lightweight and dynamic driving of P/Invoke](https://github.com/dotnet/coreclr/issues/19112)

[Handling p/invokes for different platforms and discussions about dllmap](https://github.com/dotnet/coreclr/issues/930)

[Add NativeLibrary class PR](https://github.com/dotnet/coreclr/pull/16409/commits/7ece113b5f58111ee934d923e1ea213ba50f4224)
