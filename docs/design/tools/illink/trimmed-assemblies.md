# Controlling trimmed assemblies

## Background

The SDK publish targets run `ILLink`, which has subtargets that process `ResolvedFileToPublish`. This list gets filtered down to the managed assemblies with `PostProcessAssemblies == true`, which are passed to the IL Linker. Those with `IsTrimmable != true` by default are rooted and linked with the `copy` action, and the rest have the action determined by `TrimMode`.

It is worth reiterating that there are three conditions that influence the behavior:
1. `PostProcessAssemblies` controls whether the IL Linker will see the assembly at all
2. `IsTrimmable` controls whether the IL Linker will tree-shake the assembly (if not, it gets rooted, and gets action `copy`)
3. the action (per-assembly `TrimMode` metadata, or global `TrimMode`) controls the level of tree-shaking

Different SDKs have different defaults for these options. The .NET Core sets `TrimMode` to `copyused`, which does assembly-level trimming, by default, but the Blazor SDK sets it to `link` for more aggressive trimming. Blazor also uses extension points to control which assemblies are trimmed by filtering on the assembly name, and to generate custom "type-granularity" roots for some assemblies.

## .NET Core 3 options

### IsTrimmable metadata

An SDK target runs before the IL Linker to populate an ItemGroup of assemblies passed to the IL Linker. Assemblies in this ItemGroup with metadata `IsTrimmable` set to `true` are trimmed with the default mode. In 3.x, there are no public extension points for developers to set this metadata, but SDK authors can set `IsTrimmable` on `KnownFrameworkReference` and it is applied to all of the assemblies that are part of the framework reference. In 3.x, this was used to enable trimming of netcoreapp assemblies.

## .NET 5 options

### `TrimMode`
To enable aggressive trimming instead of assembly-level trimming, we provide a public property `TrimMode`. Setting this to`link` changes the default behavior from `copyused` to `link` (aggressive trimming) for assemblies that don't have per-assembly `TrimMode`. `TrimMode` can also be set as Item metadata to override the global property per-assembly.

### `PrepareForILLink`
There is a public target `PrepareForILLink` that runs before the `ILLink` target, and provides a convenient place to hook into the pipeline to modify metadata for trimming. SDK components can use this as an extension point via `BeforeTargets` and `AfterTargets`.

The global `TrimMode` may be set any time before `PrepareForILLink` runs, which sets it to a default value if not set previously.

### `ManagedAssemblyToLink`
The `PrepareForILLink` has a dependency that creates the ItemGroup `ManagedAssemblyToLink`, which represents the set of assemblies that will be passed to the IL Linker. Custom targets may modify `IsTrimmable` and `TrimMode` metadata on these assemblies before `PrepareForILLink`, which sets the assembly action based on this metadata, or they may modify the metadata after `PrepareForILLink` has run.

It is not possible to change the items in `ManagedAssemblyToLink`, since this represents the set that needs to be filtered and replaced in the publish output. To change which assemblies are passed to the IL Linker, a different extension point should be used to set `PostProcessAssemblies` metadata.

### Examples

This shows how a developer can turn on aggressive trimming for framework assemblies (which are defined to be `IsTrimmable` by the SDK):

```xml
<PropertyGroup>
  <TrimMode>link</TrimMode>
</PropertyGroup>
```

This shows how Blazor (or a developer) can hook into the build to opt assemblies into different levels of trimming based on the filename:

```xml
<Target Name="PrepareForBlazorILLink"
        BeforeTargets="PrepareForILLink">
  <PropertyGroup>
    <!-- Set the default TrimMode for IsTrimmable assemblies -->
    <TrimMode>link</TrimMode>
  </PropertyGroup>
  <ItemGroup>
    <ManagedAssemblyToLink Condition="'$([System.String]::Copy('%(ManagedAssemblyToLink.Filename)').StartsWith('Microsoft.AspNetCore.'))">
      <!-- Trim these assemblies using the global TrimMode -->
      <IsTrimmable>true</IsTrimmable>
    </ManagedAssemblyToLink>
    <ManagedAssemblyToLink Condition="'$([System.String]::Copy('%(ManagedAssemblyToLink.Filename)').StartsWith('ThirdPartyAssembly.'))">
      <!-- Trim these assemblies with assembly-level trimming. Implies IsTrimmable. -->
      <TrimMode>copyused</TrimMode>
    </ManagedAssemblyToLink>
  </ItemGroup>
</Target>
```

### Other options

.NET 5 introduced a host of additional SDK options that map directly to the underlying illink options. The full list is documented at https://learn.microsoft.com/dotnet/core/deploying/trimming-options.

## .NET 6

### `AssemblyMetadata("IsTrimmable", "True")`

An assembly-level `AssemblyMetadataAttribute` may be placed on an assembly to indicate that it can be trimmed:

```csharp
[assembly: AssemblyMetadata("IsTrimmable", "True")]
```

The behavior is the same as the `IsTrimmable` MSBuild metadata, so that:
- Assemblies with this attribute are trimmed with the global `TrimMode`
- Assemblies without this attribute are rooted, and given the `copy` action

The only understood value is `True` (case-insensitive). Adding `[assembly: AssemblyMetadata("IsTrimmable", "False")]` will have no effect on the IL Linker's behavior, because unattributed assemblies are assumed not to be trimmable by default. We will issue a warning in this case, to discourage misleading use of the attribute.

The attribute survives trimming like other assembly-level attributes do.

If `IsTrimmable` MSBuild metadata is set for an assembly, this overrides the `IsTrimmable` attribute. This allows a developer to opt an assembly into trimming even if it does not have the attribute, or to disable trimming of an assembly that has the attribute.

Instead of using `IsTrimmable` metadata in the SDK to control trimmable assemblies, we will move to a model where all trimmable SDK assemblies are built with `[assembly: AssemblyMetadata("IsTrimmable", "True")]`.

### `TrimmableAssembly`

This ItemGroup contains assembly names that get opted into trimming via `IsTrimmable` metadata. For simple cases, this provides an easier way to enable trimming of additional assemblies, without requiring a custom MSBuild target. It exists purely as a convenience because we expect this to be commonly done as .NET transitions to becoming more trim ready.

```xml
<ItemGroup>
  <TrimmableAssembly Include="MyAssembly" />
</ItemGroup>
```

The above opts `MyAssembly.dll` into trimming. Note that the ItemGroup should contain assembly names without an extension, similar to [`TrimmerRootAssembly`](https://learn.microsoft.com/dotnet/core/deploying/trimming-options#root-assemblies). Before .NET 6 this would have been done with a target:

```xml
<Target Name="ConfigureTrimming"
        BeforeTargets="PrepareForILLink">
  <ItemGroup>
    <ManagedAssemblyToLink Condition="'%(Filename)' == 'MyAssembly'">
      <IsTrimmable>true</IsTrimmable>
    </ManagedAssemblyToLink>
  </ItemGroup>
</Target>
```

## Future evolution

As the .NET ecosystem shifts to support trimming of more libraries, we will be able to rely more on the trim analysis warnings to provide correctness guarantees. Since these guarantees are the same regardless of the `TrimMode`, we expect SDKs to move to more aggressive trimming defaults.

We expect that the .NET SDK will eventually set `TrimMode` to `link` instead of `copyused` as it does today. Long-term, we may even go as far as enabling trimming of all assemblies by default when using ILLink. Existing MSBuild `IsTrimmable` and `TrimMode` metadata will continue to make it possible for the developer to opt in or out of trimming. We could additionally provide further knobs to simplify controlling trimming behavior and opting out of such defaults.

### `TrimAllAssemblies` global opt-in

We could make it it easier to enable trimming for all assemblies with a simple boolean. This would be equivalent to setting `IsTrimmable` to `true` on every assembly that is input to the IL Linker. For example:

```xml
<PropertyGroup>
  <TrimAllAssemblies>true</TrimAllAssemblies>
</PropertyGroup>
```

could be used instead of

```xml
<Target Name="ConfigureTrimming"
        BeforeTargets="PrepareForILLink">
  <ItemGroup>
    <TrimmerRootAssembly Include="@(IntermediateAssembly)" />
    <ManagedAssemblyToLink>
      <IsTrimmable>true</IsTrimmable>
    </ManagedAssemblyToLink>
  </ItemGroup>
</Target>
```

This could be set by default in future SDKs, or it could be set by the developer in SDKs where it is not the default. We prefer not to introduce such a property at the moment, because it makes it too easy to enable this more "dangerous" behavior. We may consider adding this in the future when more of the .NET ecosystem has been made compatible with trimming.

### `AssemblyMetadata("IsTrimmable", "False")`

With more aggressive defaults, it could make sense to support an attribute opt-out via `[assembly: AssemblyMetadata("IsTrimmable", "False")]`. This would provide a way for developers to indicate that their assemblies should not be trimmed.

Its semantics should be the same as setting `IsTrimmable` MSBuild metadata to `false` for the assembly. These semantics currently result in the assembly getting rooted and getting the `copy` action, which keeps all members in the assembly but can still rewrite it to fix references to removed type forwarders.

The attribute opt-out would be useful for a scenario where multiple projects with aggressive trimming (not uncommon in Xamarin.iOS) reference a shared assembly which should not be trimmed. Instead of requiring MSBuild metadata in each referencing project, the attribute could opt the shared assembly out of trimming once and for all.

We would like to avoid a situation where developers overuse the attribute, and we end up with many libraries that can't be trimmed because of it. This would be especially counterproductive for developers interested in aggressive trimming. Its use should be reserved for cases where a library is intrinsically not trimmable - but it's not obvious when this would be the case. Typically, whether a library is safe to trim depends on the context of the application that uses it. We should discourage use of the assembly-level opt-out in cases where one might reasonably use only a part of the assembly.

We may also consider whether the opt-out should instead prevent the IL Linker from rewriting the attributed assembly. A developer might reasonably expect that adding this attribute would prevent modification by the IL Linker. This could be useful as a way to preserve assemblies that have invariants which would be broken by rewriting, or which contain data that would be removed by the IL Linker even with the `copy` action. We would need to decide how to handle removed type forwarders - we could preserve referenced type forwarders, or produce an error if the assembly references a removed type forwarder.

### `NonTrimmableAssembly` opt-out

Similar to `TrimmableAssembly`, we could introduce an ItemGroup to simplify opting out of trimming for an assembly. It would work the same way, setting `IsTrimmable` to `false` on the specified assembly. With the current defaults that don't trim unattributed assemblies, we expect this to be significantly less useful than the `TrimmableAssembly`, but it would be useful to opt out of more aggressive defaults.

We would also need to decide the precedence betwen `TrimmableAssembly` and `NonTrimmableAssembly`, or issue a warning if an assembly is in both ItemGroups.

An alternative to a separate ItemGroup would be to support `IsTrimmable` metadata on the `TrimmableAssembly` ItemGroup, which could be set to `false` to opt out of trimming.

## Alternatives considered

### `IsTrimmable` MSBuild metadata vs ItemGroup vs Property

It is not always obvious from the project file which assemblies will be included in the published application. Some assemblies are shipped as part of a framework reference, and others as OOB packages. Additionally, the SDK has its own configuration knobs that control whether files are eligible for post-processing. The final list is only "known" until the publish targets run, which is why the most flexible way to control the set of trimmed assemblies is during `PrepareForILLink`. Per-item metadata is a natural way to do this in MSBuild, and can be used for example to filter on the filenames in `ManagedAssemblyToLink`.

However, sometimes developers know beforehand that a particular assembly will be a part of the published app. In such cases, it doesn't make sense to require them to write a target, hence the proposal for a simplified opt-in via the `TrimmableAssembly` ItemGroup. This option does not replace `IsTrimmable` metadata, but works on top of it.

We also considered making the simplified option a property instead of an ItemGroup - defining a syntax for the property (for example, semicolon-delimited assembly names), and parsing it into an ItemGroup before applying it as metadata to `ManagedAssemblyToLink`. MSBuild properties have the advantage that they can be passed on the command-line, not just from the project file. However, it's not common practice to use a property to represent multiple entities in MSBuild (`NoWarn` and other warning options are an exception), especially when they relate to files.

If there is a use case for specifying trimmable assemblies on the command-line, we can always add a property as well.

### `AssemblyMetadataAttribute` vs `IsTrimmableAttribute`

We will use `AssemblyMetadataAttribute` to specify `IsTrimmable` on an assembly, instead of introducing a new attribute. The existing attribute seems well-suited for this use case, as it is already similarly used to control servicing for framework assemblies, for example via:

```csharp
[assembly: AssemblyMetadata("Serviceable", "True")]
[assembly: AssemblyMetadata("PreferInbox", "True")]
```

This way there is no need to define a new attribute in the framework, and library authors targeting previous versions of .NET will not need to inject the attribute definition into their own assemblies.

### `IsTrimmable` attribute vs `DefaultTrimMode` attribute

We considered allowing the assembly-level attribute to specify the IL Linker "action" to take, instead of making it a simple opt-in. This is a more flexible option, which would allow library authors to precisely control the default trimming behavior for their libraries. However, we prefer a simple `IsTrimmable` opt-in because this:
- Simplifies the IL Linker configuration options (it is already possible to specify the action from MSBuild)
- Handles the common scenario of enabling trimming by default for an assembly that is trimming friendly
- Avoids baking knowledge of the IL Linker "actions" into assembly metadata

We also anticipate that the SDK may in the future move to using `<TrimMode>link</TrimMode>` by default (like in Blazor today), deprecating `TrimMode` as a configuration knob.

### `IsTrimmable` attribute vs metadata priority

The `IsTrimmable` MSBuild metadata takes precedence over `IsTrimmable` `AssemblyMetadataAttribute`. We also considered allowing the attribute to override the metadata set in MSBuild, so that newer versions of an assembly can override default settings in the SDK. For example, if we ship with SDK defaults that set `IsTrimmable` MSBuild metadata on an assembly, this would allow a future version of the assembly to opt out of trimming. However, we are intending the MSBuild metadata to be used by developers to override defaults, and we will move away from setting this by default in the SDK, using attributes instead.
## Notes on the .NET 5 options

### `IsTrimmable` vs `TrimMode`

`IsTrimmable` exists in addition to `TrimMode` so that there can be a global default for assemblies without a per-assembly `TrimMode`. This lets the global property be used to set the mode for all `IsTrimmable` assemblies, and it lets individual assemblies be opted into trimming using the default mode set by the SDK for the target form factor.

### Naming of `TrimMode` values

We have considered a few naming conventions for the `TrimMode` values:
- `Conservative`/`Aggressive` - avoids complex terminology and would be easy to use for app developers without requiring an understanding of the IL Linker, and might let us change optimization levels in the future, but hides details from developers who are interested in the underlying behavior
- `TrimAssembly`/`TrimMembers` - describes what the IL Linker is doing in each mode, but is incomplete because it doesn't mention the various optimizations that are turned on
- `copyused`/`link` - maps directly to the underlying terminology used in the IL Linker, letting developers who understand the IL Linker make informed decisions, but requires more understanding of the IL Linker

We chose to stay with the `copyused`/`link` terminology that is used by the tool itself. `IsTrimmable` allows opting into or out of trimming without referencing this terminology. If we add higher-level options to the IL Linker in the future, we could expose those as new `TrimMode` values, or aliases for existing values.

### `Build` vs `Publish`

The public properties and targets exposed in this design do not require modifying `ResolvedFileToPublish` or other MSBuild entities that are related to publish, leaving some room for us to potentially reuse targets if we ever need to run the IL Linker during build instead of publish.
