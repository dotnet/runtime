# Host: well-known runtime properties

The host passes information to the runtime via a set of runtime properties - key and value strings. These properties include [runtime configuration settings](https://learn.microsoft.com/dotnet/core/runtime-config) that a user can specify in a [runtimeconfig.json](https://learn.microsoft.com/dotnet/core/runtime-config/#runtimeconfigjson) or [MSBuild properties](https://learn.microsoft.com/dotnet/core/project-sdk/msbuild-props#runtime-configuration-properties). The host itself also has a set of well-known properties that it will pass to the runtime when applicable. This document describes those well-known properties.

### Path separator

All properties that contain a list of paths use a platform-specific path separator. This corresponds to `;` on Windows and `:` on Unix.

## App information

`APP_CONTEXT_BASE_DIRECTORY`

Directory containing the application. This is used for [`AppContext.BaseDirectory`](https://learn.microsoft.com/dotnet/api/system.appcontext.basedirectory).

`RUNTIME_IDENTIFIER`

[Runtime identifier](https://learn.microsoft.com/dotnet/core/rid-catalog) for the application. This is used for [`RuntimeInformation.RuntimeIdentifier`](https://learn.microsoft.com/dotnet/api/system.runtime.interopservices.runtimeinformation.runtimeidentifier).

## Deps files

`APP_CONTEXT_DEPS_FILES`

Path to the `deps.json` file for the application. This is used by [`Microsoft.Extensions.DependencyModel`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencymodel).

`FX_DEPS_FILE`

Path to the `deps.json` file the root shared framework - `Microsoft.NETCore.App` - for framework-dependent applications. This is used by [`Microsoft.Extensions.DependencyModel`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencymodel).

## Startup

`STARTUP_HOOKS`

List of assemblies (paths or names) containing a [`StartupHook`](./host-startup-hook.md) to be run before the application's main entry point. Paths are delimited by a [platform-specific path separator](#path-separator).

## Probing paths

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

## Single-file

`BUNDLE_PROBE`

Hex string representation of a function pointer. It is set when running a single-file application. The function is called by the runtime to look for assemblies bundled into the application. The expected signature is defined as `BundleProbeFn` in [`coreclrhost.h`](/src/coreclr/hosts/inc/coreclrhost.h)

`HOSTPOLICY_EMBEDDED`

Indicates whether or not [`hostpolicy`](./host-components.md#host-policy) is embedded in the host executable. It is set to `true` when running a self-contained single-file application.

`PINVOKE_OVERRIDE`

Hex string representation of a function pointer. It is set when running a self-contained single-file application. The function is called by the runtime to check for redirected p/invokes. The expected signature is defined as `PInvokeOverrideFn` in [`coreclrhost.h`](/src/coreclr/hosts/inc/coreclrhost.h) and [`mono-private-unstable-types.h`](/src/native/public/mono/metadata/details/mono-private-unstable-types.h).