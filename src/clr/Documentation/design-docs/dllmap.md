# DllMap
DllMap is a mechanism to influence native library load resolution via a name mapping. DllMap feature facilitates writing platform-agnostic library invocations (`pInvoke`) in managed code, while separating the platform-specific naming details to a separate configuration file. This document describes how DllMap can be realized in .Net Core.

### Dllmap in Mono

Mono implements [Dllmap](http://www.mono-project.com/docs/advanced/pinvoke/dllmap/)  using an XML configuration for name mappings.  For example:

```xml
<configuration>
    <dllmap dll="MyLib.dll" target="YourLib.dll"/>
    <dllmap os="windows" dll="libc.so.6" target="cygwin1.dll"/>
</configuration>
```

Mono also permits mapping of method names within libraries, but with the restriction that the original and mapped methods must have the same signature. This document does not deal with mapping method names or signatures.

### Dllmap in .Net Core

#### NativeLibrary APIs

.Net Core 3 provides a rich set of APIs to manage native libraries, as well as callbacks to influence native library resolution. 


- [NativeLibrary APIs](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.nativelibrary?view=netcore-3.0): Perform operations on native libraries (such as `Load()`, `Free()`, get the address of an exported  symbol, etc.) in a platform-independent way from managed code.
- [DllImport Resolver callback](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.nativelibrary.setdllimportresolver?view=netcore-3.0):  Gets a callback for first-chance native library resolution using custom logic. 
- [Native Library Resolve event](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.loader.assemblyloadcontext.resolvingunmanageddll?view=netcore-3.0): Get an event for last-chance native library resolution using custom logic.   

These APIs can be used to implement custom native library resolution logic, including Mono-style DllMap.

#### DllMap Sample

A sample implementation of DLLMap using the NativeLibrary APIs is here: [DllMap Sample](https://github.com/dotnet/samples/tree/master/core/extensions/DllMapDemo). 

