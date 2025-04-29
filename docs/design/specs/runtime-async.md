
This document is a draft of changes to ECMA-335 for the "runtime async" feature. When the feature is officially supported, it can be merged into the final ECMA-335 augments document.

# Runtime-async

Async is currently a feature implemented by various .NET languages as a compiler rewrite to support methods that can "yield" control back to their caller at specific "suspension" points. While effective, it's believed that implementation directly in the .NET runtime can provide improvements, especially in performance.

## Spec modifications

These are proposed modifications to the ECMA-335 specification for runtime-async.

### I.8.4.5 Sync and Async Methods

Methods may be either 'sync' or 'async'. Async method definitions are methods attributed with `[MethodImpl(MethodImplOptions.Async)]`. Inside async method bodies, certain methods are also invokable by a special signature encoding, described in [### I.8.6.1.5 Method signatures].

Applicability of `MethodImplOptions.Async`:
* The `[MethodImpl(MethodImplOptions.Async)]` only has effect when applied to method definitions that return generic or nongeneric variants of Task or ValueTask.
* The `[MethodImpl(MethodImplOptions.Async)]` only has effect when applied to method definitions with CIL implementation.
* Async method definitions are only valid inside async-capable assemblies. An async-capable assembly is one which references a corlib containing an `abstract sealed class RuntimeFeature` with a `public const string` field member named `Async`.
* Combining `MethodImplOptions.Async` with `MethodImplOptions.Synchronized` is invalid.
* Applying `MethodImplOptions.Async` to methods with a `byref` or `ref-like` return value is invalid.
* Applying `MethodImplOptions.Async` to vararg methods is invalid.

Sync methods are all other methods.

Unlike sync methods, async methods support suspension. Suspension allows async methods to yield control flow back to their caller at certain well-defined suspension points, and resume execution of the remaining method at a later time or location, potentially on another thread. Suspension points are where suspension may occur, but suspension is not required if all Task-like objects are completed.

Async methods also do not have matching return type conventions as sync methods. For sync methods, the stack should contain a value convertible to the stated return type before the `ret` instruction. For async methods, the stack should be empty in the case of `Task` or `ValueTask`, or the type argument in the case of `Task<T>` or `ValueTask<T>`.

Async methods support the following suspension points:

* Calling another method through the secondary encoding described in [### I.8.6.1.5 Method signatures]. No special instructions need to be provided. If the callee suspends, the caller will suspend as well.
* Using new .NET runtime APIs to "await" an "INotifyCompletion" type. The signatures of these methods shall be:
  ```C#
  namespace System.Runtime.CompilerServices
  {
      public static class RuntimeHelpers
      {
          [MethodImpl(MethodImplOptions.Async)]
          public static Task AwaitAwaiterFromRuntimeAsync<TAwaiter>(TAwaiter awaiter) where TAwaiter : INotifyCompletion { ... }
          [MethodImpl(MethodImplOptions.Async)]
          public static Task UnsafeAwaitAwaiterFromRuntimeAsync<TAwaiter>(TAwaiter awaiter) where TAwaiter : ICriticalNotifyCompletion
      }
  }
  ```

Each of the above methods will have semantics analogous to the current AsyncTaskMethodBuilder.AwaitOnCompleted/AwaitUnsafeOnCompleted methods. After calling this method, it can be presumed that the task has completed.

Local variables used across suspension points are considered "hoisted." That is, only "hoisted" local variables will have their state preserved after returning from a suspension. By-ref variables may not be hoisted across suspension points, and any read of a by-ref variable after a suspension point will produce null. Structs containing by-ref variables will also not be hoisted across suspension points and will have their default value after a suspension point. 
In the same way, pinning locals may not be "hoisted" across suspension points and will have `null` value after a suspension point.

Async methods have some temporary restrictions with may be lifted later:
* The `tail` prefix is forbidden
* Usage of the `localloc` instruction is forbidden

Other restrictions are likely to be permanent, including
* By-ref locals cannot be hoisted across suspension points
* Suspension points may not appear in exception handling blocks.
* Only four types will be supported as the return type for "runtime-async" methods: `System.Threading.Task`, `System.Threading.ValueTask`, `System.Threading.Task<T>`, and `System.Threading.ValueTask<T>`

All async methods effectively have two entry points, or signatures. The first signature is the one present in method definitions: a `Task` or `ValueTask` returning method. The second signature is a "secondary signature", described in further detail in [I.8.6.1.5 Method signatures].

Callers may retrieve a Task/ValueTask return type from an async method via calling its primary, definitional signature. This functionality is available in both sync and async methods.

### II.23.1.11 Flags for methods [MethodImplAttributes]

| Flag  | Value | Description |
| ------------- | ------------- | ------------- |
| . . . | . . . | . . . |
|Async |0x0400 |Method is an Async Method.|

### I.8.6.1.5 Method signatures

The list of relevant components is augmented to include sync vs. async method types. Async methods have some additions to normal signature compatibility.

Async methods are capable of calling certain methods using an "unwrapping signature." The target method must return one of `Task`, `ValueTask`, `Task<T>`, or `ValueTask<T>`.

The unwrapping signature is generated based on the primary signature. The transformation is as follows:
* If the target method return type is `Task` or `ValueTask`, the return type of the unwrapping signature is `void` with the first custom modifier on the return type being the original type.
* Otherwise, the return type is the type argument of the return type (either ``Task`1`` or ``ValueTask`1``) with the first custom modifier on the return type being the original type.

It is an error to declare a method with an "unwrapping signature".

_[Note: these rules operate before generic substitution, meaning that a method which only meets requirements after substitution would not be considered as valid.]_

### II.15.4.6 async methods

In certain cases described in [I.8.6.1.5 Method signatures], MethodDef definitions for some methods may have two valid invocation signatures. All call sites to a "secondary" signature must use a MethodRef, even if the method is definined inside the same module or assembly. The "primary" definition can be called using a regular `MethodDef` token.
