**LoadContext** can be viewed as a container for assemblies, their code and data (e.g. statics). Whenever an assembly is loaded, it is loaded within a load context - independent of whether the load was triggered explicitly (e.g. via *Assembly.Load*), implicitly (e.g. resolving static assembly references from the manifest) or dynamically (by emitting code on the fly).

This concept is not new to .NET Core but has existed since the days of .NET Framework (see [this](https://blogs.msdn.microsoft.com/suzcook/2003/05/29/choosing-a-binding-context/) for details) where it operated behind the scenes and not exposed for the developer to interact with, aside from loading your assembly in one based upon the API used to perform the load.

In .NET Core, we have exposed a [managed API surface](https://github.com/dotnet/runtime/blob/main/src/libraries/System.Runtime.Loader/ref/System.Runtime.Loader.cs) that developers can use to interact with it - to inspect loaded assemblies or create their own **LoadContext** instance. Here are some of the scenarios that motivated this work:

* Ability to load multiple versions of the same assembly within a given process (e.g. for plugin frameworks)
* Ability to load assemblies explicitly in a context isolated from that of the application.
* Ability to override assemblies being resolved from application context.
* Ability to have isolation of statics (as they are tied to the **LoadContext**)
* Expose LoadContext as a first class concept for developers to interface with and not be a magic.

## Types of LoadContext
### Default LoadContext

Every .NET app has a **LoadContext** instance created during .NET Runtime startup that we will refer to as the *Default LoadContext*. All application assemblies (including their transitive closure) are loaded within this **LoadContext** instance.

### Custom LoadContext
For scenarios that wish to have isolation between loaded assemblies, applications can create their own **LoadContext** instance by deriving from **System.Runtime.Loader.AssemblyLoadContext** type and loading the assemblies within that instance.

Multiple assemblies with the same simple name cannot be loaded into a single load context (*Default* or *Custom*). Also, .NET Core ignores strong name token for assembly binding process.

## How Load is attempted

### Basics
If an assembly *A1* triggers the load of an assembly *C1*, the latter's load is attempted within the **LoadContext** instance of the former (which is also known as the *RequestingAssembly* or *ParentAssembly*).

Dynamically generated assemblies add a slight twist since they do not have a *ParentAssembly/RequestingAssembly* per-se. Thus, they are associated with the load context of their *Creator Assembly* and any subsequent loads (static or dynamic) will use that load context.

### Resolution Process
If the assembly was already present in *A1's* context, either because we had successfully loaded it earlier, or because we failed to load it for some reason, we return the corresponding status (and assembly reference for the success case).

However, if *C1* was not found in *A1's* context, the *Load* method override in *A1's* context is invoked.

* For *Custom LoadContext*, this override is an opportunity to load an assembly **before** the fallback (see below) to *Default LoadContext* is attempted to resolve the load.

* For *Default LoadContext*, this override always returns *null* since *Default Context* cannot override itself.

If the *Load* method override does not resolve the load, fallback to *Default LoadContext* is attempted to resolve the load incase the assembly was already loaded there. If the operating context is *Default LoadContext*, there is no fallback attempted since it has nothing to fallback to.

If the *Default LoadContext* fallback also did not resolve the load (or was not applicable), the *Resolving* event is invoked against *A1's* load context. This is the last opportunity to attempt to resolve the assembly load. If there are no subscribers for this event, or neither resolved the load, a *FileNotFoundException* is thrown.

## PInvoke Resolution

*Custom LoadContext* can override the **AssemblyLoadContext.LoadUnmanagedDll** method to intercept PInvokes from within the **LoadContext** instance so that can be resolved from custom binaries. If not overridden, or if the resolution is not able to resolve the PInvoke, the default PInvoke mechanism will be used as fallback.

## Constraints

* **System.Private.CoreLib.dll** is only loaded once, and into the **Default LoadContext**, during the .NET Runtime startup as it is a logical extension of the same. It cannot be loaded into **Custom LoadContext**.
* Currently, custom **LoadContext** cannot be unloaded once created. This is a feature we are looking into for a future release.
* If an attempt is made to load a [Ready-To-Run (R2R)](https://github.com/dotnet/runtime/blob/main/docs/design/coreclr/botr/readytorun-overview.md) image from the same location in multiple load context's, then precompiled code can only be used from the first image that got loaded. The subsequent images will have their code JITted. This happens because subsequent loading binaries from the same location results in OS mapping them to the same memory as the previous one was mapped to and thus, could corrupt internal state information required for use precompiled code.

## Tests

Tests are present [here](https://github.com/dotnet/runtime/tree/main/src/libraries/System.Runtime.Loader/tests).

## API Surface

Most of the **AssemblyLoadContext** [API surface](https://github.com/dotnet/runtime/blob/main/src/libraries/System.Runtime.Loader/ref/System.Runtime.Loader.cs) is self-explanatory. Key APIs/Properties, though, are described below:

### Default

This property will return a reference to the *Default LoadContext*.

### Load

This method should be overridden in a *Custom LoadContext* if the intent is to override the assembly resolution that would be done during fallback to *Default LoadContext*

### LoadFromAssemblyName

This method can be used to load an assembly into a load context different from the load context of the currently executing assembly. The assembly will be loaded into the load context on which the method is called. If the context can't resolve the assembly in its **Load** method the assembly loading will defer to the **Default** load context. In such case it's possible the loaded assembly is from the **Default** context even though the method was called on a non-default context.

Calling this method directly on the **AssemblyLoadContext.Default** will only load the assembly from the **Default** context. Depending on the caller the **Default** may or may not be different from the load context of the currently executing assembly.

This method does not "forcefully" load the assembly into the specified context. It basically initiates a bind to the specified assembly name on the specified context. That bind operation will go through the full binding resolution logic which is free to resolve the assembly from any context (in reality the most likely outcome is either the specified context or the default context). This process is described above.

To make sure a specified assembly is loaded into the specified load context call **AssemblyLoadContext.LoadFromAssemblyPath** and specify the path to the assembly file.

### Resolving

This event is raised to give the last opportunity to a *LoadContext* instance to attempt to resolve a requested assembly that has neither been resolved by **Load** method, nor by fallback to **Default LoadContext**.

## Assembly Load APIs and LoadContext

As part of .NET Standard 2.0 effort, certain assembly load APIs off the **Assembly** type, which were present in Desktop .NET Framework, have been brought back. The following maps the APIs to the load context in which they will load the assembly:

* Assembly.Load - loads the assembly into the context of the assembly that triggers the load.
* Assembly.LoadFrom - loads the assembly into the *Default LoadContext*
* Assembly.LoadFile - creates a new (anonymous) load context to load the assembly into.
* Assembly.Load(byte[]) - creates a new (anonymous) load context to load the assembly into.

If you need to influence the load process or the load context in which assemblies are loaded, please look at the various Load* APIs exposed by **AssemblyLoadContext** [API surface](https://github.com/dotnet/runtime/blob/main/src/libraries/System.Runtime.Loader/ref/System.Runtime.Loader.cs).
