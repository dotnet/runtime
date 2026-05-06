# Host startup hook

For .NET Core 3+, we want to provide a low-level hook that allows
injecting managed code to run before the main application's entry
point. This hook will make it possible for the host to customize the
behavior of managed applications during process launch, after they
have been deployed.

## Motivation

This would allow hosting providers to define custom configuration and
policy in managed code, including settings that potentially influence
load behavior of the main entry point such as the
`AssemblyLoadContext` behavior. The hook could be used to set up
tracing or telemetry injection, to set up callbacks for handling
Debug.Assert (if we make such an API available), or other
environment-dependent behavior. The hook is separate from the entry
point, so that user code doesn't need to be modified.

## Proposed behavior

The `DOTNET_STARTUP_HOOKS` environment variable can be used to specify
a list of managed assemblies that contain a `StartupHook` type with a
`public static void Initialize()` method, each of which will be called
in the order specified, before the `Main` entry point

Unix:
```
DOTNET_STARTUP_HOOKS=/path/to/StartupHook1.dll:/path/to/StartupHook2.dll
```

Windows:
```
DOTNET_STARTUP_HOOKS=D:\path\to\StartupHook1.dll;D:\path\to\StartupHook2.dll
```

This variable is a list of assembly paths or names, delimited by the
platform-specific path separator (`;` on Windows and `:` on Unix). It
may contain leading, trailing or duplicate path separators. The
type must be named `StartupHook` without any namespace, and should be
`internal`.

Each part may be either
* absolute path to the assembly with the startup hook. In this case
  the assembly is loaded from the specified path before running
  the startup hook.
* name of the assembly with the startup hook. In this case the assembly
  is loaded by its name from the `AssemblyLoadContext.Default`. For
  this to work the assembly needs to be part of the application
  otherwise the default context won't be able to resolve it. The assembly
  name must not be a relative path, so the following rules apply
  * the assembly name must not contain directory separator characters
    `/` and `\`
  * the assembly name must not contain the space characters ` ` and
    the comma character `,`
  * the assembly name must not end with `.dll` (any casing)
  * the assembly name must be considered a valid assembly name as specified
    by the `AssemblyName` class.

Note that white-spaces are preserved and considered part of the specified
path/name. So for example path separator followed by a white-space and
another path separator is invalid, since the white-space only string
in between the path separators will be considered as assembly name.

Setting this environment variable will cause the `public static void
Initialize()` method of the `StartupHook` type in each of the
specified assemblies to be called in order, synchronously, before the
main assembly is loaded. The hooks are all called on the same managed
thread (the same thread that calls `Main`). The environment variable
will be inherited by child processes by default. It is up to the
`StartupHook.dll`s and user code to decide what to do about this -
`StartupHook.dll` may clear them to prevent this behavior globally, if
desired.

Specifically, hostpolicy starts up coreclr and sets up a new
AppDomain, passing in the startup hook variable as the property
`STARTUP_HOOKS` if it was set. This variable can be retrieved using
`AppContext.GetData("STARTUP_HOOKS")`. Hostpolicy then asks the
runtime to execute the main method.  Just before the main method is
called, the runtime will call a private method in
`System.Private.CoreLib`, which will call each
`StartupHook.Initialize()` in turn synchronously. This gives
`StartupHook` a chance to set up new `AssemblyLoadContext`s, or
register other callbacks. After all of the `Initialize()` methods
return, the runtime calls the main entry point of the app like usual.

Rather than forcing all configuration to be done through a single
predefined API, this creates a place where such configuration could be
centralized, while still allowing user code to do its own thing if it
so desires.

The producer of `StartupHook.dll` needs to ensure that
`StartupHook.dll` is compatible with the dependencies specified in the
main application's deps.json, since those dependencies are put on the
Trusted Platform Assemblies (TPA) list during the runtime startup,
before `StartupHook.dll` is loaded. This means that `StartupHook.dll`
needs to be built against the same or lower version of .NET Core than the app.

## Example

This could be used with `AssemblyLoadContext` APIs to resolve
dependencies not on the TPA list from a shared location, similar to
the GAC on .NET Framework. It could also be used to forcibly preload
assemblies that are on the TPA list from a different location. Future
changes to `AssemblyLoadContext` could make this easier to use by
making the default load context or TPA list modifiable.

Note that the `StartupHook` type is internal and in the global
namespace, and the signature of the `Initialize` method is `public
static void Initialize()`.

```c#
internal class StartupHook
{
    public static void Initialize()
    {
        AssemblyLoadContext.Default.Resolving += SharedHostPolicy.SharedAssemblyResolver.LoadAssemblyFromSharedLocation;
    }
}

namespace SharedHostPolicy
{
    class SharedAssemblyResolver
    {
        public static Assembly LoadAssemblyFromSharedLocation(AssemblyLoadContext context, AssemblyName assemblyName)
        {
            string sharedAssemblyPath = ""; // find assemblyName in shared location...
            if (sharedAssemblyPath != null)
                return AssemblyLoadContext.Default.LoadFromAssemblyPath(sharedAssemblyPath);
            return null;
        }
    }
}
```

## Error handling details

Problems with the startup hook should be fairly straightforward to
diagnose. All of these exceptions will contain the startup hook path
(`System.StartupHookProvider.ProcessStartupHooks`) on the stack
trace. They fall into the following categories:

- Errors detected eagerly, with exceptions thrown before the execution
  of any startup hook.

  - Invalid syntax throws an `ArgumentException`.

  - Partially qualified paths in the startup hook throw an
    `ArgumentException`.

- Exceptions thrown during the call to a given startup hook. Previous
  hooks may have run successfully.

  - Missing startup hook assemblies throw a `FileNotFoundException`.

  - Invalid startup hook assemblies throw a `BadImageFormatException`.

  - Missing startup hook types throw a `TypeLoadException`.

  - Missing `Initialize` methods in startup hooks throw a
    `MissingMethodException`.

  - Invalid `Initialize` methods (with an incorrect signature - that
    take parameters, have a non-void return type, are not public, or
    are not static) throw an `ArgumentException`.

  - Unhandled exceptions thrown from a startup hook will have the same
    exception behavior as any other managed exception thrown from
    `Main` - by default, they will terminate the process and show a
    stack trace.

## Guidance and caveats

This hook is meant as a low-level, powerful way to inject code into
the process at runtime, for use by tool developers who truly have a
need for this kind of power. It should only be used in situations
where modifying application code is not an option and there is not an
existing structured dependency injection framework in place. An
example of such a use case is a hook that injects logging, telemetry,
or profiling into an existing deployed application at runtime.

It is prone to ordering issues when multiple hooks are used, and does
nothing to attempt to make dependencies of hooks easy to
manage. Multiple hooks should be independent of each other.

### No built-in solution to ordering issues

For example, if one hook sets global state that introduces logging in
the process, the new behavior will affect all subsequent hooks in the
process and the `Main` entry point. Subsequent hooks may attempt to
modify logging behavior in a way that conflicts with the first hook,
leading to unexpected results. This kind of problem exists for any
framework that gives independently-owned components access to shared
resources - often dependency injection frameworks will have a
dependency manager that loads components in a specific order. If this
kind of behavior is required, a proper dependency injection framework
should be used instead of multiple startup hooks.

### No dependency resolution for non-app assemblies

Another example regarding hook dependencies: the startup hook dll must
not depend on any assemblies outside of the app's TPA list. If a
startup hook has a static dependency on an assembly like
'Newtonsoft.Json' but the app does not, executing the hook will throw
a `FileNotFoundException`. There is no extra resolution logic for
startup hooks. Any startup hook that wants to modify load behavior
will have to use framework APIs like AssemblyLoadContexts to do this
manually.

### No conflict resolution for dependencies shared by hooks or the app

If a startup hook decides to do something dangerous like force the
load of a particular assembly, any later hooks (or the entry point)
that run in the same AssemblyLoadContext and depend on that assembly
will use the version that was forcefully loaded, even if they were
compiled against a different version.

### Threading behavior

Each startup hook will run on the same managed thread as the `Main`
method, so thread state will persist between startup hooks. The
threading apartment state will be set based on any attributes present
in the `Main` method of the app, before startup hooks execute. As a
result, attempts to explicitly set the thread apartment state in a
startup hook will fail if the requested state is incompatible with the
app's threading state.

While it may make sense to set global behavior in startup hooks, it is
not recommended to use the thread state as a communication mechanism
between startup hooks. Any setup that requires multiple communicating
hooks should consider using a plugin system instead.

In order to use `ThreadStatic` storage, for example, the class
containing the shared thread state needs to be a common dependency of
the hooks that use it. Because hooks can not depend on assemblies
outside of the app's TPA list, this requires the shared state class to
be defined either in the app or within the first hook that uses it:

- If defined in the app, the shared state used by startup hooks would
  need to be compiled into the app. In that case, consider explicitly
  activating the desired behavior by modifying the app code, instead
  of using startup hooks.

- If defined in the first startup hook, all subsequent hooks that
  access the `ThreadStatic` need to be compiled with references to the
  first. In a situation like this, consider making the components that
  need to communicate with each other part of a common plugin
  framework. If necessary, the plugin host could be injected into the
  process with a single startup hook.

### Visibility of `StartupHook` type

The type should be made `internal` to prevent exposing it as API
surface to any managed code that happens to have access to the startup
hook dll. However, the feature will also work if the type is `public`.

### Incompatible with trimming

Startup hooks are disabled by default on trimmed apps. The usage of
startup hooks on a trimmed app is potentially dangerous since these
could make use of assemblies, types or members that were removed by
trimming, causing the app to crash.
