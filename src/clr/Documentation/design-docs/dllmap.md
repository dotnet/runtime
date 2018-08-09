# Dllmap design document
This document is intended to describe a plan on delivering a Dllmap feature for .NET Core.

Author: Anna Aniol (@annaaniol)

## Background

### .NET Core P/invoke mechanism
There is a .NET directive ([DllImport](https://msdn.microsoft.com/en-us/library/system.runtime.interopservices.dllimportattribute(v=vs.110).aspx)) indicating that the attributed method is exposed by an unmanaged DLL as a static entry point. It enables to call a function exported from an unmanaged DLL inside a managed code. To make such a call, library name must be provided. Names of naive libraries are OS-specific, so once the name is defined, the call can be executed correctly on one OS only.

Right now, CoreCLR has no good way to handle the differences between platforms when it comes to p/invoking native libraries. Here is an example usage of DllImport:
```c++
// Use DllImport to import the Win32 MessageBox function.
   [DllImport("user32.dll", CharSet = CharSet.Unicode)]
   public  static  extern  int  MessageBox(IntPtr  hWnd, String text, String caption, uint type);
```
This import works with Windows, but it doesn’t work with any other OS. If run e.g. on Linux, a DllNotFoundException will be thrown, which means that a DLL specified in a DLL import cannot be found.

### Mono’s Dllmap

Mono already provides a feature that addresses the problem of cross-platform p/invoke support. Mono’s [Dllmap](http://www.mono-project.com/docs/advanced/pinvoke/dllmap/) enables to configure p/invoke signatures at runtime. By providing an XML configuration file, user can define a custom mapping between OS-specific library names and methods. Thanks to that, even if a library defined in DllImport is incompatible with an OS that is currently running the application, a correct unmanaged method can be called (if it exists for this OS).

In Mono Dllmap feature custom mapping can be tightly specified based on the OS name, CPU name and a wordsize.

This simple Mono example maps references to the cygwin1.dll library to the libc.so.6 file:
```c++
<configuration>
	<dllmap  dll="cygwin1.dll" target="libc.so.6"/>
</configuration>
```

Mono dllmap logic is implemented in [metadata/mono-config.c](https://github.com/mono/mono/blob/master/mono/metadata/mono-config.c) and [metadata/loader.c](https://github.com/mono/mono/blob/master/mono/metadata/loader.c) files.

## Expectations

### .NET Core Dllmap

.NET Core Dllmap’s purpose is to deliver a cross-platform support for p/invoke mechanism in .NET. With Dllmap user will be able to control interop methods by defining custom mapping between OS-specific dlls and methods.

Dllmap will allow making changes in both library names and target method names (entrypoints). Changing entrypoint name will be optional (it will remain unchanged by default).

Target platforms for this feature are: Windows, Linux and OS X.

### Interaction with Dllmap

#### Mono combability
Dllmap should be easy to use. It’s possible to achieve this easily by keeping Mono-compatible style of XML mapping configuration file. It’s described [here](http://www.mono-project.com/docs/advanced/pinvoke/dllmap/). Thanks to combability with Mono, users will be able to migrate from Mono’s Dllmap to .NET Core cross-platform applications.

#### Flexibility
For users, who want to manage their cross-platform dll imports in their own way, dll-load specific events will be exposed. Users will be able to subscribe to these events and implement any loading policies. Details are described in the Design section.

### Usage example (XML configuration)

Let’s say there is a customer developing a .NET application with p/invokes on Windows. The customer wants the application to run on both Windows and Linux without introducing any changes in the code.

The application calls a function GetCurrentProcessId from an OS-specific library kernel32.dll.
```c++
[DllImport("kernel32.dll")]
static  extern  uint  GetCurrentProcessId();
```

To make it work on Linux, that does not have kernel32.dll, user must define a mapping of dll. There is no GetCurrentProcessId function in any corresponding Linux-specific library, so entrypoint name mapping must be defined too. To achieve this, the user puts an XML configuration file in the main application directory. The file looks like this:
```c++
<configuration>
	<dllmap  dll="kernel32.dll">
		<dllentry  dll="libc.so.6" name="GetCurrentProcessId" target="getpid" />
	</dllmap>
</configuration>
```

With this file, all GetCurrentProcessId calls get automatically mapped to getpid calls on runtime and the end user of the application can’t see any difference in application’s behavior. Running the application cross-platform does not require any OS-specific changes in the code. All the mapping is defined in advance in the external configuration file.

This is a very basic scenario and it can be extended to different operating systems, libraries and entrypoints and to subscribing to dll specific events.

## Design
### XML configuration file
For a basic case, mapping must be defined in an XML configuration file, that must be placed in the main application folder. It must be named application_name.xml where application_name is the name of the application.

XML parsing will be implemented in corefxlab.

### Library mapping
In dllimport.cpp file there is a method that loads the DLL and finds the procaddress for an N/Direct call.
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
`LoadLibraryModuleViaHost`  already contains a callback (`AssemblyLoadContext` exposes `LoadUnmanagedDll()` API to load a dll but it can be used for a `CustomAssemblyLoadContext` only, not the default one).  `LoadLibraryViaCallback`  will do  a callback, but only for non-system assemblies. `System.Private.CoreLib` and other ‘system’ assemblies will not receive callbacks.

### Entrypoint mapping
Once hmod gets resolved and returned via `LoadLibraryModule`, an entrypoint must be find:

Currently `NDirectGetEntryPoint` takes two arguments: `pMD` and hmod. When `NDirectGetEntryPoint`  gets called, `pMD `points to a target dll and hmod is correlated with a target dll too.
The entrypoint name mapping will be done at the beginning of `NDirectGetEntryPoint`. A new method ` IntPtr  GetMappedEntrypoint(sourceEntrypointName)`  will be a callback and will return a mapping for an entrypoint if it exists. `pMD` will get updated to point to a target entrypoint. The rest of `NDirectGetEntryPoint()`  flow will remain the same.
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

Events will be defined in `AssemblyLoadContext` in `Private.CoreLib`. Default handlers that subscribe dll load events will implement the mono-based dllmap logic. They will take string as argument and return IntPtr of target libraries and entrypoints based on the parsed XML configuration file.

Handlers implementation will stay in `AssemblyLoadContext`. Load library resolver will cache all the dll mapping results that got resolved (as key-value: IntPtr-hmod pairs). Thanks to that, the same library won't get loaded multiple times. User’s code will be able to subscribe to events and implement any loading behavior. That will give a user full flexibility when using dllmap and won’t limit defining the mapping to only xml-based style. Callbacks will be executed for non-system assemblies only. `System.Private.CoreLib` and other system assemblies will not receive callbacks, because that could cause problems like an infinite dll-mapping loop.

### Resolution flow

**User’s code [managed code]**

-   Subscribes to `LoadLibraryModuleViaCallback` and `GetEntrypointViaCallback` events with their default or custom handler
-   Uses DllImport directive and does the p/invoke
	 ```c++
	[DllImport("MyLibrary.dll", EntryPoint="MyFunction")]
	static  extern  int  MyFunction();
	…
	System.Runtime.Loader.AssemblyLoadContext.Default.ResolveNativeDllName += LoadNativeDllViaDllMap;
	System.Runtime.Loader.AssemblyLoadContext.Default.ResolveNativeEntrypointName += GetEntrypointViaDllMap;
	…

	MyFunction();
	```

**Runtime [unmanaged code]**
-   Calls `LoadMappedLibrary` that raises `LoadLibraryModuleViaCallback` and `GetEntrypointViaCallback` events. Runtime uses a lock when it runs into a callback and releases the lock once the callback is done, to avoid infinite looping
    
**AssemblyLoadContext [unmanaged code]**
-   Defines `LoadLibraryModuleViaCallbackand` and exposes an API:
	```c++
	IntPtr  LoadLibraryModuleViaCallback(string libraryName)
	IntPtr  GetEntrypointViaCallback(string entrypointName, HMOD hmod)	
	```

**Dotnet.corefx.labs.dll [managed code]**
-   Implements LoadNativeDllViaDllMap and GetEntrypointViaDllMap
	```c++
	IntPtr  LoadNativeDllViaDllMap(string libraryName)
	{
		if (libraryName in cachedResults)
			return cachedResults[libraryName];
		if (!mapStructure)
			mapStructure = ReadAndParseXML();
		targetLibraryName = mapStructure.GetLibrary(libraryName);
		hmod = LoadLibrary(targetLibraryName);
		AddToCache(libraryName, hmod);
		return hmod;
	}
	```
	```c++
	IntPtr  GetEntrypointViaDllMap(string entrypointName, HMOD hmod)
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

All the above test cases will be covered.

### Related discussion
[Handling p/invokes for different platforms and discussions about dllmap](https://github.com/dotnet/coreclr/issues/930)
[Add NativeLibrary class PR](https://github.com/dotnet/coreclr/pull/16409/commits/7ece113b5f58111ee934d923e1ea213ba50f4224)
