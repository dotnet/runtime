# Specifying an assembly load context for components

Several native hosting entry points (custom host, COM, C++/CLI) for loading a component have a load context parameter. Currently, it only allows hard-coded sentinels indicating either the default `AssemblyLoadContext` (ALC) or an isolated ALC for that component.

When a component loads into the default ALC, framework assemblies win, so it can't override them with its own or a higher version. When it loads into an isolated ALC per assembly, types and static state are not easily shared across components. There is no option to place components into a shared, non-default context such that they can both override certain framework assemblies (for example, shipped out-of-band) and share types with other components.

**Goals**
- Expand the load context parameter to represent a specific context
- Allow grouping components into shared, non-default contexts with dependency resolution for each component in that context

**Non-goals**
- Targeting a context created independently by managed code (rather than through this mechanism)
- Unloading components (collectible load contexts)
- Loading an application into a non-default context

## Load context representation

The load context parameter has one of the following values:

- `NULL` (`0`): Default ALC (`AssemblyLoadContext.Default`)
- `ISOLATED_CONTEXT` (`(void*)-1`): a new isolated ALC (COM and C++/CLI only)
- any other value: pointer to a `load_context` struct

```C
struct load_context
{
    size_t        size;        // sizeof(struct) for versioning
    const char_t *identifier;  // load context identifier
};
```

The following entry points take a load context parameter:
- [Custom hosting](native-hosting.md): `get_function_pointer`, `load_assembly`, `load_assembly_bytes`
- [COM](COM-activation.md) via `comhost`: `GetClassFactoryForTypeInContext`, `RegisterClassForTypeInContext`, `UnregisterClassForTypeInContext`
- [C++/CLI](IJW-activation.md) via `ijwhost`: `LoadInMemoryAssemblyInContext`

## Grouping COM and C++/CLI components into shared load contexts

Components specify a runtime host property with an identifier for the load context. Components with the same identifier share an ALC. That ALC will resolve dependencies using the `.deps.json` from the components, in precedence order corresponding to the order in which the components were loaded into it.

```xml
<ItemGroup>
  <!-- COM component -->
  <RuntimeHostConfigurationOption Include="System.Runtime.InteropServices.COM.LoadContextIdentifier"
                                  Value="MyCompany.MyApp.Plugins" />
  <!-- C++/CLI component -->
  <RuntimeHostConfigurationOption Include="System.Runtime.InteropServices.CppCLI.LoadContextIdentifier"
                                  Value="MyCompany.MyApp.Plugins" />
</ItemGroup>
```

Setting `LoadComponentInDefaultContext` (COM) / `LoadComponentInIsolatedContext` (C++/CLI) and `LoadContextIdentifier` is invalid.

## Loading into a chosen context from a custom host

A custom native host can pass a load context parameter to hosting APIs.

```C
struct load_context context = { sizeof(context), _X("MyCompany.MyApp.Plugins") };
load_assembly(_X("plugin.dll"), &context, NULL);
get_function_pointer(_X("Plugin.Entry, plugin"), _X("Run"), NULL, &context, NULL, &fn);
```

## Determining the load context

The runtime keeps a mapping from `identifier` to context, tracking only contexts created via native hosting. If a load context parameter specifies an `identifier` for which no ALC exists yet, a new one is created and tracked. The `identifier` string is compared with ordinal equality. Since the mapping is process-wide, an `identifier` should be namespaced for the application or use case to avoid colliding with unrelated components in the same process.

Resolution happens in the context's `Load` override, such that it occurs before any fallback to the default ALC's resolution. When a component is loaded into a context, it adds an `AssemblyDependencyResolver` (corresponding to the component's path) to that resolution logic. Resolvers are queried in the component load order — the first one wins.

## Related

- [Native hosting](native-hosting.md)
- [COM activation](COM-activation.md)
- [C++/CLI activation](IJW-activation.md)
- [dotnet/runtime#127149](https://github.com/dotnet/runtime/issues/127149) — COM: an app-local (compatibility package) assembly fails to load using the default ALC
- [dotnet/runtime#118115](https://github.com/dotnet/runtime/issues/118115), [dotnet/runtime#95607](https://github.com/dotnet/runtime/issues/95607) — C++/CLI: higher framework assembly version resolves in C# but not from native
- [dotnet/runtime#66013](https://github.com/dotnet/runtime/issues/66013) — COM: option to load a component into the default ALC (`LoadComponentInDefaultContext`)
- [dotnet/runtime#104480](https://github.com/dotnet/runtime/issues/104480) — C++/CLI: the isolated-loading switch (`LoadComponentInIsolatedContext`)
