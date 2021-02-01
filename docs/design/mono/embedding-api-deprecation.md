# Mono Embedding API Deprecation

While the preferred hosting solution for .NET is the [shared hosting APIs](https://docs.microsoft.com/en-us/dotnet/core/tutorials/netcore-hosting), some workloads would prefer or need more runtime internals exposed via a native interface. Mono has a history of a rich embedding API, and so we can to try and fill this need within the .NET ecosystem.

We hope to make this process easier from a build perspective ([dotnet/runtime\#56994](https://github.com/dotnet/runtime/issues/46994)), but we also need to decide how we want to handle ABI/API compatibility. Below I outline the history, current state, and a proposal moving forward.

# History

With Framework Mono, the embedding API was never "designed" as such. It was the product of contributors moving symbols they needed to public headers, and over time this was codified into the embedding API documented and consumed today.

In addition, the Framework Mono policy on breaking API compatibility has been "don't". We technically do not guarantee this across stable versions, but in practice that has been the rule. This is obviously a boon to users, but from a maintenance perspective has become expensive and results in a lot of duplicated code, awkward type conversions, and overly complex architectural decisions. To ensure the outdated APIs are not used inside the runtime itself we introduced the `MONO_RT_EXTERNAL_ONLY` define, which prevents their usage internally but solves none of the above problems.

Finally, as the need has come up to extend existing APIs, new ones were added with an attempt at a descriptive postfix. Unfortunately, over a few iterations, you end up with headers that look something like this:

```c
MONO_API MONO_RT_EXTERNAL_ONLY
MonoAssembly* mono_assembly_load (MonoAssemblyName *aname, const char *basedir, MonoImageOpenStatus *status);
MONO_API MONO_RT_EXTERNAL_ONLY
MonoAssembly* mono_assembly_load_full (MonoAssemblyName *aname, const char *basedir, MonoImageOpenStatus *status, mono_bool refonly);
MONO_API MONO_RT_EXTERNAL_ONLY
MonoAssembly* mono_assembly_load_from (MonoImage *image, const char *fname, MonoImageOpenStatus *status);
MONO_API MONO_RT_EXTERNAL_ONLY
MonoAssembly* mono_assembly_load_from_full (MonoImage *image, const char *fname, MonoImageOpenStatus *status, mono_bool refonly);
MONO_API MONO_RT_EXTERNAL_ONLY
MonoAssembly* mono_assembly_load_with_partial_name (const char *name, MonoImageOpenStatus *status);
```

Note that *none* of those are even what we use internally! And none of them offer AssemblyLoadContext support, so the list isn't even complete.

# Current state in .NET 5

With the migration to dotnet/runtime and .NET 5 changes, many of the embedding APIs are no longer relevant. Currently the behavior with inapplicable APIs is inconsistent, and they will either do nothing, error, or silently ignore arguments.

There have also been some small breaks in the behavior of existing APIs, in particular some loader-related changes documented [here](https://www.mono-project.com/news/2020/08/24/native-loader-net5/).

Support for the `coreclrhost.h` APIs already exists in Mono, as the default `dotnet` host relies on them to function. Those are not defined in a shipping header, and instead users are expected to copy over the definitions and trust us to export the symbols. The relevant file is [mini/main-core.c](https://github.com/dotnet/runtime/blob/master/src/mono/mono/mini/main-core.c).

We have also been forced to add a handful of .NET 5-specific API extensions, mostly to offer the Xamarin products AssemblyLoadContext-aware versions of preexisting APIs. We have defined those in the `mono-private-unstable.h` headers, but this is intended as a temporary measure to unblock some .NET 5 migrations.

# Moving forward

For .NET 6, we have an opportunity to clean up the existing headers and define a clear policy around ABI/API stability and deprecation.

## ABI stability

ABI stability is only guaranteed across major version releases. .NET 6 will not receive a servicing update that breaks ABI compatibility, but you cannot link a .NET 5 app against the .NET 6 release, full stop.

## API stability

API stability is more complicated. There are two distinct cases here: .NET 6 and the behavior moving forward.

With the .NET 6 release specifically, we have an opportunity to do some initial API cleanup work because relatively few desktop users have migrated over to .NET 5 Mono. There is some low-hanging fruit to potentially clean up immediately, before we have significant migration: irrelevant functionality like `mono_domain_set` or `refonly` parameters, legacy compatibility hacks like `MonoLookupPInvokeStatus`, and subtly-incorrect behavior like fixed-size GC handles. 

The .NET 6 immediate removal should only be used for functions that will allow for significant cleanup of the embedding API, runtime internals, or that would otherwise be likely to result in difficult to diagnose customer bugs or crashes at runtime. There are a lot of functions we might not want to remove for .NET 6 for ease of migration or simply time constraints. The earlier `mono_assembly_load` example is a potential case of this, but we have many similar cases of symbol variants. We want to remove many of them eventually, but for one of the above reasons it may not happen for .NET 6.

To handle this moving forward, we will use a `MONO_DEPRECATED` define, which corresponds to the `[[deprecated]]` attribute on relevant compilers. For every API marked with this, we should have an entry on a new page on the Mono site explaining the deprecation and the suggested alternative. This creates a moderate bar for deprecations, but still leaves them as a possibility where necessary.

Any function appearing in a major release marked deprecated may, and likely will go away in the following major release. This means that if `mono_assembly_load` is deprecated for .NET 6, it can be removed at any time leading up to .NET 7 or beyond, including during the previews.

Deprecation is the preferred path for functions likely to see heavy use with embedders in .NET 6 and the _only_ option past that point. .NET 6 is the only release where removal may occur without the attribute first being present in a major release, though this should be avoided with functions likely to see heavy use in existing embedding workloads.

# Points for further discussion (to address before merging)

For deprecated APIs that include useless arguments or are irrelevant for netcore, should we silently ignore them/do nothing or throw a runtime error? What would be more useful for embedders, given that the deprecations allow them to detect potentially problematic functions at compile time?

How does this work with LTS releases? Should we try and deprecate on LTS and remove on non-LTS when possible, or does it not matter?

Should we include in the function declaration what release a given function was added or deprecated in? This would probably take the form of replacing `MONO_API` with `SINCE` and `DEPRECATED` calls, but increases complexity and effort for potentially limited value.
