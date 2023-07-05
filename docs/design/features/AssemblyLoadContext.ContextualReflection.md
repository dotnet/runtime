# AssemblyLoadContext.CurrentContextualReflectionContext
## Problem
.NET Core 3.0 is trying to enable a simple isolated plugin loading model.

The issue is that the existing reflection API surface changes behavior depending on how the plugin dependencies are loaded. For the problematic APIs, the location of the `Assembly` directly calling the reflection API, is used to infer the `AssemblyLoadContext` for reflection loads.

Consider the following set of dependencies:
```C#
Assembly pluginLoader;     // Assume loaded in AssemblyLoadContext.Default
Assembly plugin;           // Assume loaded in custom AssemblyLoadContext
Assembly pluginDependency; // Behavior of plugin changes depending on where this is loaded.
Assembly framework;        // Required loaded in AssemblyLoadContext.Default
```

The .NET Core isolation model allows `pluginDependency` to be loaded into three distinct places in order to satisfy the dependency of `plugin`:
* `AssemblyLoadContext.Default`
* Same custom `AssemblyLoadContext` as `plugin`
* Different custom `AssemblyLoadContext` as `plugin` (unusual, but allowed)

Using `pluginDependency` to determine the `AssemblyLoadContext` used for loading leads to inconsistent behavior. The `plugin` expects `pluginDependency` to execute code on its behalf. Therefore it reasonably expects `pluginDependency` to use `plugin`'s `AssemblyLoadContext`. It leads to unexpected behavior except when loaded in the "Same custom `AssemblyLoadContext` as `plugin`."

### Failing Scenarios
#### Xunit story

We have been working on building a test harness in Xunit for running the CoreFX test suite inside `AssemblyLoadContext`s (each test case in its own context).  This has proven to be somewhat difficult due to Xunit being a very reflection heavy codebase with tons of instances of types, assemblies, etc. being converted to strings and then fed through `Activator`.  One of the main learnings is that it is not always obvious what will stay inside the “bounds” of an `AssemblyLoadContext` and what won’t.  The basic rule of thumb is that any `Assembly.Load()` will result in the assembly being loaded onto the `AssemblyLoadContext` of the calling code, so if code loaded by an ALC calls `Assembly.Load(...)`, the resulting assembly will be within the “bounds” of the ALC.  This unfortunately breaks down in some cases, specifically when code calls `Activator` which lives in `System.Private.CoreLib` which is always shared.

#### System.Xaml
This problem also manifests when using an `Object` deserialization framework which allows specifying assembly qualified type names.

We have seen this issue when porting WPF tests to run in a component in an isolation context.  These tests are using `System.Xaml` for deserialization. During deserialization, `System.Xaml` is using the affected APIs to create object instances using assembly-qualified type names.

### Scope of affected APIs
The problem exists whenever a reflection API can trigger a load or bind of an `Assembly` and the intended `AssemblyLoadContext` is ambiguous.
#### Currently affected APIs
These APIs are using the immediate caller to determine the `AssemblyLoadContext` to use. As shown above the immediate caller is not necessarily the desired context.

These always trigger assembly loads and are always affected:
```C#
namespace System
{
    public static partial class Activator
    {
        public static ObjectHandle CreateInstance(string assemblyName, string typeName);
        public static ObjectHandle CreateInstance(string assemblyName, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes);
        public static ObjectHandle CreateInstance(string assemblyName, string typeName, object[] activationAttributes);
    }
}
namespace System.Reflection
{
    public abstract partial class Assembly : ICustomAttributeProvider, ISerializable
    {
        public static Assembly Load(string assemblyString);
        public static Assembly Load(AssemblyName assemblyRef);
        public static Assembly LoadWithPartialName (string partialName);
    }
}
```
These are only affected when they trigger assembly loads. Assembly loads for these occur when `typeName` includes a assembly-qualified type reference:
```C#
namespace System
{
    public abstract partial class Type : MemberInfo, IReflect
    {
        public static Type GetType(string typeName, bool throwOnError, bool ignoreCase);
        public static Type GetType(string typeName, bool throwOnError);
        public static Type GetType(string typeName);
    }
}

namespace System.Reflection
{
    public abstract partial class Assembly : ICustomAttributeProvider, ISerializable
    {
        public Type GetType(string typeName, bool throwOnError, bool ignoreCase);
        public Type GetType(string typeName, bool throwOnError);
        public Type GetType(string typeName);
    }
}
```
#### Normally unamiguous APIs related to affected APIs
```C#
namespace System
{
    public abstract partial class Type : MemberInfo, IReflect
    {
        public static Type GetType(string typeName, Func<AssemblyName, Assembly> assemblyResolver, Func<Assembly, string, bool, Type> typeResolver);
        public static Type GetType(string typeName, Func<AssemblyName, Assembly> assemblyResolver, Func<Assembly, string, bool, Type> typeResolver, bool throwOnError);
        public static Type GetType(string typeName, Func<AssemblyName, Assembly> assemblyResolver, Func<Assembly, string, bool, Type> typeResolver, bool throwOnError, bool ignoreCase);
    }
}
```
In this case, `assemblyResolver` functionally specifies the explicit mechanism to load.

If the `assemblyResolver` is `null`, assembly loads for these occur when `typeName` includes a assembly-qualified type reference.
### Root cause analysis
In .NET Framework, plugin isolation was provided by creating multiple `AppDomain` instances.  .NET Core dropped support for multiple `AppDomain` instances. Instead we introduced `AssemblyLoadContext`.

The isolation model for `AssemblyLoadContext` is very different from `AppDomain`. One major distinction was the existence of an ambient property `AppDomain.CurrentDomain` associated with the running code and its dependents. There is no equivalent ambient property for `AssemblyLoadContext`.

The issue is that the existing reflection API surface design was based on the existence of an ambient `AppDomain.CurrentDomain` associated with the current isolation environment. The `AppDomain.CurrentDomain` acted as the `Assembly` loader.  (In .NET Core the loader function is conceptually attached to `AssemblyLoadContext`.)

## Options

There are two main options:

1.  Add APIs which allow specifying an explicit callback to load assemblies.  Guide customers to avoid using the APIs which just infer assembly loading semantics on their own.

1. Add an ambient property which corresponds to the active `AssemblyLoadContext`.

We are already pursuing the first option. It is insufficient. For existing code with existing APIs this approach can be problematic.

The second option allows logical the separation of concerns.  Code loaded into an isolation context does not really need to be concerned with how it was loaded. It should expect APIs to logically behave in the same way independent of loading.

This proposal is recommending pursuing the second option while continuing to pursue the first.

## Proposed Solution
This proposal is for a mechanism for code to explicitly set a specific `AssemblyLoadContext` as the `CurrentContextualReflectionContext` for a using block and its asynchronous flow of control.  Previous context is restored upon exiting the using block. Blocks can be nested.

### `AssemblyLoadContext.CurrentContextualReflectionContext`

```C#
namespace System.Runtime.Loader
{
    public partial class AssemblyLoadContext
    {
        private static readonly AsyncLocal<AssemblyLoadContext> _asyncLocalActiveContext;
        public static AssemblyLoadContext CurrentContextualReflectionContext
        {
            get { return _asyncLocalCurrentContextualReflectionContext?.Value; }
        }
    }
}
```
`AssemblyLoadContext.CurrentContextualReflectionContext` is a static read only property. Its value is changed through the API below.

`AssemblyLoadContext.CurrentContextualReflectionContext` property is an `AsyncLocal<T>`. This means there is a distinct value which is associated with each asynchronous control flow.

The initial value at application startup is `null`. The value for a new async block will be inherited from its parent.

#### When `AssemblyLoadContext.CurrentContextualReflectionContext != null`

When `AssemblyLoadContext.CurrentContextualReflectionContext != null`, `CurrentContextualReflectionContext` will act as the primary `AssemblyLoadContext` for the affected APIs.
When used in an affected API, the primary, will:
* determine the set of known Assemblies and how to load the Assemblies.
* get the first chance to `AssemblyLoadContext.Load(...)` before falling back to `AssemblyLoadContext.Default` to try to load from its TPA list.
* fire its `AssemblyLoadContext.Resolving` event if the both of the preceding have failed

##### Key concepts

* Each `AssemblyLoadContext` is required to be idempotent. This means when it is asked to load a specific `Assembly` by name, it must always return the same result. The result would include whether an `Assembly` load occurred and into which `AssemblyLoadContext` it was loaded.
* The set of `Assemblies` related to an `AssemblyLoadContext` are not all loaded by the same `AssemblyLoadContext`.  They collaborate. An assembly loaded into one `AssemblyLoadContext`, can resolve its dependent `Assembly` references from another `AssemblyLoadContext`.
* The root framework (`System.Private.Corelib.dll`) is required to be loaded into the `AssemblyLoadContext.Default`. This means all custom `AssemblyLoadContext` depend on this code to implement fundamental code including the primitive types.
* If an `Assembly` has static state, its state will be associated with its load location. Each load location will have its own static state. This can guide and constrain the isolation strategy.
* `AssemblyLoadContext` loads lazily. Loads can be triggered for various reasons. Loads are often triggered as code begins to need the dependent `Assembly`. Triggers can come from any thread. Code using `AssemblyLoadContext` does not require external synchronization. Inherently this means that `AssemblyLoadContext` are required to load in a thread safe way.

#### When `AssemblyLoadContext.CurrentContextualReflectionContext == null`

The behavior of .NET Core will be unchanged. Specifically, the effective `AssemblyLoadContext` will continued to be inferred to be the ALC of the current
caller's `Assembly`.

### `AssemblyLoadContext.EnterContextualReflection()`

The API for setting `CurrentContextualReflectionContext` is intended to be used in a using block.

 ```C#
namespace System.Runtime.Loader
{
    public partial class AssemblyLoadContext
    {
        public ContextualReflectionScope EnterContextualReflection();

        static public ContextualReflectionScope EnterContextualReflection(Assembly activating);
    }
}
```

Two methods are proposed.
1. Activate `this` `AssemblyLoadContext`
2. Activate the `AssemblyLoadContext` containing `Assembly`.  This also serves as a mechanism to deactivate within a using block (`EnterContextualReflection(null)`).

#### Basic Usage
```C#
  AssemblyLoadContext alc = new AssemblyLoadContext();
  using (alc.EnterContextualReflection())
  {
    // AssemblyLoadContext.CurrentContextualReflectionContext == alc
    // In this block, alc acts as the primary Assembly loader for context sensitive reflection APIs.
    Assembly assembly = Assembly.Load(myPlugin);
  }
```
#### Maintaining and restoring original behavior
```C#
static void Main(string[] args)
{
  // On App startup, AssemblyLoadContext.CurrentContextualReflectionContext is null
  // Behavior prior to .NET Core 3.0 is unchanged
  Assembly assembly = Assembly.Load(myPlugin); // Will load into the Default ALC.
}

void SomeCallbackMethod()
{
  using (AssemblyLoadContext.EnterContextualReflection(null))
  {
    // AssemblyLoadContext.CurrentContextualReflectionContext is null
    // Behavior prior to .NET Core 3.0 is unchanged
    Assembly assembly = Assembly.Load(myPlugin); // Will load into the ALC containing SomeMethod().
  }
}
```
## Approved API changes

```C#
namespace System.Runtime.Loader
{
    public partial class AssemblyLoadContext
    {
        public static AssemblyLoadContext CurrentContextualReflectionContext { get { return _asyncLocalCurrentContextualReflectionContext?.Value; }}

        public ContextualReflectionScope EnterContextualReflection();

        static public ContextualReflectionScope EnterContextualReflection(Assembly activating);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public struct ContextualReflectionScope : IDisposable
        {
        }
    }
}
```
## Design doc

### Affected runtime native calls

The affected runtime native calls correspond to the runtime's mechanism to load an assembly, and to get a type. Each affected native call is passed a managed reference to the CurrentContextualReflectionContext.  This prevents GC holes, while running the native code. The `CurrentContextualReflectionContext` acts as the mechanism for resolving assembly names to assemblies.

### Unloadability

`CurrentContextualReflectionContext` will hold an `AssemblyLoadContext` reference. This will prevent the context from being unloaded while it could be used. As an `AsyncLocal<AssemblyLoadContext>`, the setting will propagate to child threads and asynchronous tasks.
After a thread or asynchronous task completes, the `AsyncLocal<AssemblyLoadContext>` will eventually be cleared, this will unblock the `AssemblyLoadContext` unload. The timing of this unload depends on the ThreadPool implementation.

### ContextualReflectionScope

```C#
/// <summary>Opaque disposable struct used to restore CurrentContextualReflectionContext</summary>
/// <remarks>
/// This is an implementation detail of the AssemblyLoadContext.EnterContextualReflection APIs.
/// It is a struct, to avoid heap allocation.
/// It is required to be public to avoid boxing.
/// <see cref="System.Runtime.Loader.AssemblyLoadContext.EnterContextualReflection"/>
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public struct ContextualReflectionScope : IDisposable
{
    private readonly AssemblyLoadContext _activated;
    private readonly AssemblyLoadContext _predecessor;
    private readonly bool _initialized;

    internal ContextualReflectionScope(AssemblyLoadContext activating)
    {
        _predecessor = AssemblyLoadContext.CurrentContextualReflectionContext;
        AssemblyLoadContext.SetCurrentContextualReflectionContext(activating);
        _activated = activating;
        _initialized = true;
    }

    public void Dispose()
    {
        if (_initialized)
        {
            // Do not clear initialized. Always restore the _predecessor in Dispose()
            // _initialized = false;
            AssemblyLoadContext.SetCurrentContextualReflectionContext(_predecessor);
        }
    }
}
```

`_initialized` is included to prevent useful default construction. It prevents the `default(ContextualReflectionScope).Dispose()` case.

`_predecessor` represents the previous value of `CurrentContextualReflectionContext`. It is used by `Dispose()` to restore the previous state.

`_activated` is included as a potential aid to debugging. It serves no other useful purpose.

This struct is implemented as a readonly struct. No state is modified after construction. This means `Dispose()` can be called multiple times. This means `using` blocks will always restore the previous `CurrentContextualReflectionContext` as exiting.

### Unusual usage patterns

There are some unusual usage patterns which are not recommended, but not prohibited. They all have reasonable behaviors.

* Clear but never restore the CurrentContextualReflectionContext
```C#
myAssemblyLoadContext.EnterContextualReflection(null);
```

* Set but never clear the CurrentContextualReflectionContext
```C#
myAssemblyLoadContext.EnterContextualReflection();
```

* Manual dispose
```C#
myAssemblyLoadContext.EnterContextualReflection();

scope.Dispose();
```

* Multiple dispose
```C#
ContextualReflectionScope scope = myAssemblyLoadContext.EnterContextualReflection();

scope.Dispose(); // Will restore the context as set during `EnterContextualReflection()`
scope.Dispose(); // Will restore the context as set during `EnterContextualReflection()`  (again)
```

* Early dispose
```C#
using (ContextualReflectionScope scope = myAssemblyLoadContext.EnterContextualReflection())
{
    scope.Dispose(); // Will restore the context as set during `EnterContextualReflection()`
} // `using` will restore the context as set during `EnterContextualReflection()` (again)
```
