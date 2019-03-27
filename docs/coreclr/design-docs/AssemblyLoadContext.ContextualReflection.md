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
```
#### Unamiguous APIs related to affected APIs
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
In this case, `assemblyResolver` functionally specifies the explicit mechanism to load. This indicates the current assembly's `AssmblyLoadContext` is not being used. If the `assemblyResolver` is only serving as a first or last chance resolver, then these would also be in the set of affected APIs.
#### Should be affected APIs
Issue https://github.com/dotnet/coreclr/issues/22213, discusses scenarios in which various flavors of the API `GetType()` is not functioning correctly. As part of the analysis and fix of that issue, the set of affected APIs may increase.
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
        private static readonly AsyncLocal<AssemblyLoadContext> asyncLocalActiveContext = new AsyncLocal<AssemblyLoadContext>(null);
        public static AssemblyLoadContext CurrentContextualReflectionContext
        {
            get { return _asyncLocalCurrentContextualReflectionContext.Value; }
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
        public static AssemblyLoadContext CurrentContextualReflectionContext { get { return _asyncLocalCurrentContextualReflectionContext.Value; }}

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

Mostly TBD.

### Performance Consideration
#### Avoiding native / managed transitions

My natural inclination would be to replace most native calls taking a `StackCrawlMark` with callS taking the `CurrentContextualReflectionContext`. When `CurrentContextualReflectionContext` is `null`, resolve the `StackCrawlMark` first, passing the result as the inferred context.

However this may require mutiple native/managed transitions. Performance considerations may require Native calls which currently take a `StackCrawlMark` will need to be modified to also take `CurrentContextualReflectionContext`.

### Hypothetical Advanced/Problematic use cases

One could imagine complicated scenarios in which we need to handle ALCs on event callback boundaries. These are expected to be rare. The following are representative patterns that demonstrate the possibility to be support these more complicated usages.

#### An incoming event handler into an AssemblyLoadContext
```C#
void OnEvent()
{
  using (alc.Activate())
  {
    ...
  }
}
```
#### An incoming event handler into a collectible AssemblyLoadContext
```C#
class WeakAssemblyLoadContextEventHandler
{
  WeakReference<AssemblyLoadContext> weakAlc;

  void OnEvent()
  {
    AssemblyLoadContext alc;
    if(weakAlc.TryGetTarget(out alc))
    {
      using (alc.Activate())
      {
        ...
      }
    }
  }
}
```
#### A outgoing callback
```C#
using (AssemblyLoadContext.Activate(null))
{
  Callback();
}
```
#### An outgoing event handler
```C#
void OnEvent()
{
  using (AssemblyLoadContext.Activate(null))
  {
    ...
  }
}
```
