# Host: providing information to the runtime

The host passes information to the runtime via a set of runtime properties - key and value strings. These properties include [runtime configuration settings](https://learn.microsoft.com/dotnet/core/runtime-config) that a user can specify in a [runtimeconfig.json](https://learn.microsoft.com/dotnet/core/runtime-config/#runtimeconfigjson) or [MSBuild properties](https://learn.microsoft.com/dotnet/core/project-sdk/msbuild-props#runtime-configuration-properties). The host itself also has a set of well-known properties that it will pass to the runtime when applicable.

Starting with .NET 8, one of those properties represents a contract between the host and runtime. This document describes that contract and other well-known properties.

## Host runtime contract

Relying on key-value strings as part of initialization of the runtime comes with some drawbacks:

- Any information has to be represented as a string. Other structured or non-string data becomes awkward to communicate.
- Each property comes with non-trivial overhead. As the properties flow through the hosting layer, runtime, and libraries, multiple copies are made of each name and value.
- Properties are pre-computed and set at startup. Every application, regardless of whether or not it requires a specific property, must pay the cost of all properties.

To allow a more flexible and less costly way to pass information between the host and runtime, in .NET 8+, the host passes a contract to the runtime as a property. This contract serves as a mechanism for runtime to query for information from the host and for the host to provide structured information to the runtime.

`HOST_RUNTIME_CONTRACT`

Hex string representation of a pointer to a [`host_runtime_contract` struct](/src/native/corehost/host_runtime_contract.h).

The `get_runtime_property` function provides key-value string information (like that provided for runtime initialization). This removes the requirement to pre-compute and store all properties. [Existing properties](#well-known-runtime-properties) can be migrated to go through this mechanism, allowing for pay-for-play properties and reducing the cost of properties.

Some existing properties (for example, [probing path properties](#probing-paths)) would benefit from being structured data rather than a single string. The contract can be extended to allow querying for that specific, structured information rather than relying only on strings. For backwards compatibility for existing properties, we would still allow getting the string via `get_runtime_property`, but that would be an on-demand cost.

## Well-known runtime properties

These can be retrieved via the `host_runtime_contract.get_runtime_property` function. Before the introduction of the host runtime contract, these all had to be passed directly to `coreclr_initialize`.

#### Path separator

All properties that contain a list of paths use a platform-specific path separator. This corresponds to `;` on Windows and `:` on Unix.

### App information

`APP_CONTEXT_BASE_DIRECTORY`

Directory containing the application. This is used for [`AppContext.BaseDirectory`](https://learn.microsoft.com/dotnet/api/system.appcontext.basedirectory).

`RUNTIME_IDENTIFIER`

[Runtime identifier](https://learn.microsoft.com/dotnet/core/rid-catalog) for the application. This is used for [`RuntimeInformation.RuntimeIdentifier`](https://learn.microsoft.com/dotnet/api/system.runtime.interopservices.runtimeinformation.runtimeidentifier).

### Deps files

`APP_CONTEXT_DEPS_FILES`

Path to the `deps.json` file for the application. This is used by [`Microsoft.Extensions.DependencyModel`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencymodel).

`FX_DEPS_FILE`

Path to the `deps.json` file the root shared framework - `Microsoft.NETCore.App` - for framework-dependent applications. This is used by [`Microsoft.Extensions.DependencyModel`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencymodel).

### Startup

`STARTUP_HOOKS`

List of assemblies (paths or names) containing a [`StartupHook`](./host-startup-hook.md) to be run before the application's main entry point. Paths are delimited by a [platform-specific path separator](#path-separator).

### Probing paths

`TRUSTED_PLATFORM_ASSEMBLIES`

List of platform and application assembly file paths. Paths are delimited by a [platform-specific path separator](#path-separator). This is used in [managed assembly probing](https://learn.microsoft.com/dotnet/core/dependency-loading/default-probing#managed-assembly-default-probing).

`NATIVE_DLL_SEARCH_DIRECTORIES`

List of directory paths to search for unmanaged (native) libraries. Paths are delimited by a [platform-specific path separator](#path-separator). This is used in [unmanaged (native) assembly probing](https://learn.microsoft.com/dotnet/core/dependency-loading/default-probing#unmanaged-native-library-probing).

`PLATFORM_RESOURCE_ROOTS`

List of directory paths to search for satellite resource assemblies. Paths are delimited by a [platform-specific path separator](#path-separator). This is used in [satellite (resource) assembly probing](https://learn.microsoft.com/dotnet/core/dependency-loading/default-probing#satellite-resource-assembly-probing).

`APP_PATHS`

List of directory paths to search for managed assemblies. Paths are delimited by a [platform-specific path separator](#path-separator). This is not set by default.

`PROBING_DIRECTORIES`

List of directory paths corresponding to shared store paths and additional probing paths used by the host for [probing paths](./host-probing.md#probing-paths). Paths are delimited by a [platform-specific path separator](#path-separator).

### Single-file

`BUNDLE_PROBE`

Hex string representation of a function pointer. It is set when running a single-file application. The function is called by the runtime to look for assemblies bundled into the application. The expected signature is defined as `BundleProbeFn` in [`coreclrhost.h`](/src/coreclr/hosts/inc/coreclrhost.h)

**.NET 9 and above** This property is no longer set by the host. `host_runtime_contract.bundle_probe` is set when running a single-file application.

`HOSTPOLICY_EMBEDDED`

Indicates whether or not [`hostpolicy`](./host-components.md#host-policy) is embedded in the host executable. It is set to `true` when running a self-contained single-file application.

**.NET 9 and above**  This property is no longer set by the host or read by the runtime. Self-contained single-file includes both host and runtime components in the executable, so the information is known at build-time.

`PINVOKE_OVERRIDE`

Hex string representation of a function pointer. It is set when running a self-contained single-file application. The function is called by the runtime to check for redirected p/invokes. The expected signature is defined as `PInvokeOverrideFn` in [`coreclrhost.h`](/src/coreclr/hosts/inc/coreclrhost.h) and [`mono-private-unstable-types.h`](/src/native/public/mono/metadata/details/mono-private-unstable-types.h).

**.NET 9 and above** This property is no longer set by the host. `host_runtime_contract.pinvoke_override` is set when running a self-contained single-file application.
