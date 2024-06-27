
This document is a draft of changes to ECMA-335 for the "runtime async" feature. When the feature is officially supported, it can be merged into the final ECMA-335 augments document.

# Runtime-async

Async is currently a feature implemented by various .NET languages as a compiler rewrite to support methods that can "yield" control back to their caller at specific "suspension" points. While effective, it's believed that implementation directly in the .NET runtime can provide improvements, especially in performance.

## Spec modifications

These are proposed modifications to the ECMA-335 specification for runtime-async.

### I.8.4.5 Sync and Async Methods

Methods may be either 'sync' or 'async'. Async methods have a special signature encoding, described in [### I.8.6.1.5 Method signatures].

Sync methods are all other methods.

Unlike sync methods, async methods support suspension. Suspension allows async methods to yield control flow back to their caller at certain well-defined suspension points, and resume execution of the remaining method at a later time or location, potentially on another thread.

Async methods support the following suspension points:

* Calling another async method. No special instructions need to be provided. If the callee suspends, the caller will suspend as well.
* Using new .NET runtime APIs to "await" an "INotifyCompletion" type. The signatures of these methods shall be:
  ```C#
  // public static async2 Task AwaitAwaiterFromRuntimeAsync<TAwaiter>(TAwaiter awaiter) where TAwaiter : INotifyCompletion
  public static void modopt([System.Runtime]System.Threading.Tasks.Task) AwaitAwaiterFromRuntimeAsync<TAwaiter>(TAwaiter awaiter) where TAwaiter : INotifyCompletion
  // public static async2 Task UnsafeAwaitAwaiterFromRuntimeAsync<TAwaiter>(TAwaiter awaiter) where TAwaiter : ICriticalNotifyCompletion
  public static void modopt([System.Runtime]System.Threading.Tasks.Task) UnsafeAwaitAwaiterFromRuntimeAsync<TAwaiter>(TAwaiter awaiter) where TAwaiter : ICriticalNotifyCompletion
  ```

Each of the above methods will have semantics analogous to the current AsyncTaskMethodBuilder.AwaitOnCompleted/AwaitUnsafeOnCompleted methods. After calling this method, in can be presumed that the task has completed.

Async methods have the following restrictions:
* Usage of the `localloc` instruction is forbidden
* The `ldloca` and `ldarga` instructions are redefined to return managed pointers instead of pointers.
* Pinning locals may not be created
* The `tail` prefix is forbidden

Suspension points may not appear in exception handling blocks.

All async methods effectively have two entry points, or signatures. The first signature is the one present in the above code: a modopt before the return type. The second signature is a "Task-equivalent signature", described in further detail in [I.8.6.1.5 Method signatures].

Async methods have a special calling convention and may not be called directly outside of other async methods. To call an async method from a sync method, callers must use the second "Task-equivalent signature".

Callers may retrieve a Task-equivalent return type from an async method via calling the "Task-equivalent signature". This functionality is available in both sync and async methods.

### I.8.6.1.5 Method signatures

The list of relevant components is augmented to include sync vs. async method types. Async methods have some additions to normal signature compatibility.

Async MethodDef entries implicitly create two member definitions: one explicit, primary definiton, and a second implicit, runtime-generated definition.

The primary, mandatory definition must be present in metadata as a MethodDef. This signature is required to have a `modopt` (optional modifier) as the last custom modifier before the return type. The custom modifier must fit the following requirements:
* If the return type is void, the custom modifier must be either `System.Threading.Task` or `System.Threading.ValueTask`.
* If the return type is not `void`, the modifier must be to either `System.Threading.Task<T>` or `System.Threading.ValueTask<T>`. The return type must be a valid substitution for the type parameter of the custom modifier type.

_[Note: async methods have the same return type conventions as sync methods. If the async method produces a System.Int32, the return type must be System.Int32.]_

The second async signature is implicit and runtime-generated and is hereafter referred to as the "Task-equivalent" signature. It is generated based on the primary signature. The transformation is as follows:

If the async return type is void, the return type of the Task-equivalent signature is the type of the async custom modifier.

Otherwise, the Task-equivalent return type is the custom modifier type (either ``Task`1`` or ``ValueTask`1``), substituted with the async return type.

### I.8.10.2 Method inheritance

For the purposes of inheritance and hiding, both async signatures ([### I.8.6.1.5 Method signatures]) are used for hiding and overriding, but cannot be configured separately.

### II.10.3.2 The .override directive

Async methods participate in overrides through both definitions (both signatures). An async method with a .override overrides the target method signature, as well as the secondary "Task-equivalent" signature if applicable. An async method may also override only the "Task-equivalent" signature, if an async signature is not present on the base class. A sync method may not override an async method, even if it matches the async method's "Task-equivalent" definition.