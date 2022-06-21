# Mono code guidelines

This document is meant to capture guidelines for contributing code to
the [src/mono/mono/](../../src/mono/mono),
[src/native/public/mono](../../src/native/public/mono) areas of the
dotnet/runtime repo.

In general this guide does not apply to:

1. Shared native code in [src/native](../../src/native)
2. The Mono-specific C# code in [src/mono](../../src/mono) in System.Private.CoreLib or elsewhere

## Code style

Mono is written in C.

We follow the [Mono Coding guidelines](https://www.mono-project.com/community/contributing/coding-guidelines/) for the C code - in particular:

* tabs, not spaces
* space between a function name and the open parenthesis
* braces on the same line as `if`, `for`, `while` etc

## Naming

Mono reserves the following prefixes for symbols: `mono_`, `monovm_`, `m_`, `monoeg_` (used to remap [`eglib`](../../src/mono/mono/eglib)
functions which in source code have a `g_` prefix).

All non-`static` symbols should use one of these prefixes.  We generally use `mono_` for most
functions.  `m_` is used for some inline accessor functions and macros.  `g_` (`mono_eg_`) is used
for `eglib` functions.

Types in a single C file can use any name.  Types in a header should use a `Mono` (or sometimes
`mono_`) prefix.

Public API symbols and types *must* be prefixed.

For types Mono generally uses `typedef struct _MonoWhatever { ... } MonoWhatever`.  Opaque types may
define `typedef struct _MonoWhatever MonoWhatever` in a client header and define `struct
_MonoWhatever {...}` in an implementation header.  Occasionally we break `#include` cycles by adding
forward declarations for some types.

## Macros

Mono derives from an autotools-style project so we generally write `HOST_XYZ` for the machine on
which the runtime is executing (as opposed to the machine on which the runtime was compiled), and
`TARGET_XYZ` for the machine that will be targeted by the JIT and AOT compilers.  In the case of AOT
compilation host and target might be different: the host might be Windows, and the target might be
Browser WebAssembly, for example.

Macros generally use a `MONO_` prefix.  Macros in public API headers *must* use the prefix.

## Types

Prefer the standard C sized types `int32_t`, `intptr_t`, etc over the eglib types `gint32`, `gsize` etc.

One exception is `gboolean` is prefered over C `bool`.

There are actually three boolean types to keep in mind:

* `gboolean` used internally in the runtime
* `MonoBoolean` used as an interop type with C# bool in internal calls
* `mono_bool` used by the public C API - generally new code shouldn't use it except when adding a new public API function.

## Utility and platform abstraction functions

Mono generally tries to fill in POSIX-like abstractions on platforms that lack them (for example, Windows).
This is in contrast to the CoreCLR PAL which generally tries to add Windows API abstractions on top
of POSIX.

If an existing `eglib` utility function is available, it should be used.  New ones can be added.

Other platform specific code is in `src/mono/utils`

## Directory dependencies

For the code in `src/mono/mono`:

* `eglib` should not depend on other code from `src/mono`

* `utils` can depend on `eglib` and 3rd party code from `src/native/external`

* `sgen` depends on `eglib` and `utils`

* `metadata` depends on all of the above (but note that some Boehm-GC files in this directory should not depend on `sgen`)

* `mini` depends on all of the above

* `mini/interp` depends on all of the above

* `components` can use functions from all of the above provided they're marked with
  `MONO_COMPONENT_API`, see [../design/mono/components.md](../design/mono/components.md)

The main distinction between `metadata` and `utils` is that utils code should not assume that it is
part of an executing .NET runtime - anything that loads types, creates objects, etc does not belong
in utils but in metadata.

The `mini` directory contains execution engines.  If the execution engine has to provide some
functionality to `metadata` it generally does so by installing some callback that is invoked by
`metadata`.  For example, `mini` knows how to unwind exceptions and perform stack walks - while
`metadata` decides *when* to unwind or perform a stackwalk.  To coordinate, `metadata` exposes an
API to install hooks and `mini` provides the implementations.

## Error handling

New code should prefer to use the `MonoError` functions.

* A non-public-API function should take a `MonoError *` argument.
* In case of an error the function should call one of the `mono_error_set_` functions from [../../src/mono/mono/utils/mono-error-internals.h](../../src/mono/mono/utils/mono-error-internals.h)
* Inside the runtime check whether there was an error by calling `is_ok (error)`, `mono_error_assert_ok (error)`, `goto_if_nok` or `return_if_nok`/`return_val_if_nok`
* If there is an error and you're handling it, call `mono_error_cleanup (error)` to dispose of the resources.
* `MonoError*` is generally one-shot: after it's been cleaned up it needs to be re-inited with `mono_error_init_reuse`, but this is discouraged.
* Instead if you intend to deal with an error, use `ERROR_DECL (local_error)` then call the
  possibly-failing function and pass it `local_error`, then call `mono_error_assert_ok
  (local_error)` if it "can't fail" or `mono_error_cleanup (local_error)` if you want to ignore the
  failure.

## Managed Exceptions

New code should generally not deal with `MonoException*`, use `MonoError*` instead.

New code should avoid using `mono_error_set_pending_exception` - it affects a thread-local flag in
a way that is not obvious to the caller of your function and may trample existing in-flight
exceptions and make your code fail in unexpected ways.

There are two circumstances when you might need to call `mono_error_set_pending_exception`:

1. You're working with a public Mono API that sets a pending exception
2. You're implementing an icall, but can't use `HANDLES()` (see the [internal calls](#internal-calls) section below)

## Internal calls

Prefer P/Invokes or QCalls over internal calls.  That is, if your function only takes arguments that
are not managed objects, and does not need to interact with the runtime, it is better to define it
outside the runtime.

Internal calls generally have at least one argument that is a managed object, or may throw a managed exception.

Internal calls are declared in [`icall-def.h`](../../src/mono/mono/metadata/icall-def.h) See the comment in the header for details.

There are two styles of internal calls: `NOHANDLES` and `HANDLES`.  (This is a simplification as there
are also JIT internal calls added by the execution engines)

The difference is that `HANDLES` icalls receive references to managed objects wrapped in a handle
that attempts to keep the object alive for the duration of the internal call even if the thread
blocks or is suspended, while `NOHANDLES` functions don't.  Additionally `HANDLES` functions get a
`MonoError*` argument from the managed-to-native interop layer that will be converted to a managed
exception when the function returns.  `NOHANDLES` functions generally have to call
`mono_error_set_pending_exception` themselves.

## Suspend Safety

See [Cooperative Suspend at mono-project.com](https://www.mono-project.com/docs/advanced/runtime/docs/coop-suspend/) and the [mono thread state machine design document](../design/mono/mono-thread-state-machine.md)

In general runtime functions that may be called from the public Mono API should call
`MONO_ENTER_GC_UNSAFE`/`MONO_EXIT_GC_UNSAFE` if they call any other runtime API.  Internal calls are
already in GC Unsafe on entry, but QCalls and P/Invokes aren't.

When calling a blocking native API, wrap the call in `MONO_ENTER_GC_SAFE`/`MONO_EXIT_GC_SAFE`.

Prefer `mono_coop_` synchronization primitives (`MonoCoopMutex`, `MonoCoopCond`,
`MonoCoopSemaphore`, etc) over `mono_os_` primitives (`mono_mutex_t`, `mono_cond_t`).  The former
will automatically go into GC Safe mode before doing operations that may block.  The latter should
only be used for low-level leaf locks that may need to be shared with non-runtime code. You're
responsible for switching to GC Safe mode when doing blocking operations in that case.

## GC Memory Safety

We have explored many different policies on how to safely access managed memory from the runtime.  The existing code is not uniform.

This is the current policy (but check with a team member as this document may need to be updated):

1. It is never ok to access a managed object from GC Safe code.
2. Mono's GC scans the managed heap precisely.  Mono does not allow heap objects to contain pointers into the interior of other managed obejcts.
3. Mono's GC scans the native stack conservatively: any value that looks like a pointer into the GC
   heap, will cause the target object to be pinned and not collected.  Interior pointers from the
   stack into the middle of a managed object are allowed and will pin the object.

In general one of the following should be used to ensure that an object is kept alive, particularly
across a call back into managed from native, or across a call that may trigger a GC (effectively
nearly any runtime internal API, due to assembly loading potentially triggering managed callbacks):

* The object is pinned in managed code using `fixed` (common with strings) and the native code gets a `gunichar2*`
* a `ref` local is passed into native code and the native code gets a `MonoObject * volatile *` (the `volatile` is important)
* a `Span<T>` is passed into native code and the native code gets a `MonoSpanOfObjects*`
* an icall is declared with `HANDLES()` and a `MonoObjectHandle` (or a more specific type such as `MonoReflectionTypeHandle` is passed in).
* a GCHandle is passed in

Generally only functions on the boundary between managed and native should use one of the above
mechanisms (that is, it's enough that an object is pinned once). Callees can take a `MonoObject*`
argument and assume that it was pinned by the caller.

In cases where an object is created in native code, it should be kept alive:

1. By assigning into a `MonoObject * volatile *` (ie: use `out` or `ref` arguments in C#)
2. By creating a local handle using `MONO_HANDLE_NEW` or `MONO_HANDLE_PIN` (the function should then use `HANDLE_FUNCTION_ENTER`/`HANDLE_FUNCTION_RETURN` to set up and tear down a handle frame)
3. By creating a GCHandle
4. By assigning to a field of another managed object

In all cases, if there is any intervening internal API call or a call to managed code, the object
must be kept alive before the call using one of the above methods.

### Write barriers

When writing a managed object to a field of another managed object, use one of the
`mono_gc_wbarrier_` functions (for example, `mono_gc_wbarrier_generic_store`).  It is ok to call the write
barrier functions if the destination is not in the managed heap (in which case they will just do a normal write)

## Assertions

Mono code should use `g_assert`, `mono_error_assert_ok`, `g_assertf`, `g_assert_not_reached` etc.

Unlike CoreCLR, Mono assertions are always included in the runtime - both in Debug and in Release builds.

New code should try not to rely on the side-effects of assert conditions. (That is, one day we may want
to turn off assertions in Release builds.)

## Mono Public API

Mono maintains a public API for projects that embed the Mono runtime in order to provide the ability
to execute .NET code in the context of another application or framework.  The current Mono API is
`mono-2.0`.  We strive to maintain binary ABI and API stability for our embedders.

The public API headers are defined in
[`../../src/native/public/mono`](../../src/native/public/mono).  Great care should be taken when
modifying any of the functions declared in these headers.  In particular breaking the ABI by
removing these functions or changing their arguments is not allowed.  Semantic changes should be
avoided, or implemented in a way that is least disruptive to embedders (for example the runtime does
not support multiple appdomains anymore, but `mono_domain_get` continues to work).

### Hidden API functions

In practice certain functions have been tagged with `MONO_API` even if they are not declared in the
public headers.  These symbols have been used by projects that embed Mono despite not being in the
public headers.  They should be treated with the same care as the real public API.

### Unstable API functions

Functions declared in `mono-private-unstable.h` headers do not need to maintain API/ABI stability.
They are generally new APIs that have been added since .NET 5 but that have not been stabilized yet.

As a matter of courtesy, notify the .NET macios and .NET Android teams if changing the behavior of these functions.

### WASM

The WASM and WASI runtimes in [`src/mono/wasm/runtime`](../../src/mono/wasm/runtime) and
`src/mono/wasi` are effectively external API clients.  When possible they should use existing `MONO_API` functions.

As a matter of expedience, the wasm project has sometimes taken advantage of static linking by
adding declarations of internal Mono functions in `src/mono/wasm/runtime/driver.c` and directly
calling Mono internals.

In general new code should not do this.  When modifying existing code, mysterious WASM failures may
be attributed to symbol signature mismatches between WASM and the Mono runtime.
