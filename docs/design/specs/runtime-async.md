
This document is a draft of changes to ECMA-335 for the "runtime async" feature. When the feature is officially supported, it can be merged into the final ECMA-335 augments document.

# Runtime-async

Async is currently a feature implemented by various .NET languages as a compiler rewrite to support methods that can "yield" control back to their caller at specific "suspension" points. While effective, it's believed that implementation directly in the .NET runtime can provide improvements, especially in performance.

## Spec modifications

These are proposed modifications to the ECMA-335 specification for runtime-async.

### I.8.4.5 Sync and Async Methods

Methods may be either 'sync' or 'async'. Async method definitions are methods with a return type of `System.Threading.Task`, `System.Threading.ValueTask`, `System.Threading.Task<T>`, or `System.Threading.ValueTask<T>`  attributed with `[RuntimeAsyncMethodAttribute]`. Inside async method bodies, async methods are also invokable by a special signature encoding, described in [### I.8.6.1.5 Method signatures].

Sync methods are all other methods.

Unlike sync methods, async methods support suspension. Suspension allows async methods to yield control flow back to their caller at certain well-defined suspension points, and resume execution of the remaining method at a later time or location, potentially on another thread.

Async methods also do not have matching return type conventions as sync methods. For sync methods, the stack should contain a value convertible to the stated return type before the `ret` instruction. For async methods, the stack should be empty in the case of `Task` or `ValueTask`, or the type argument in the case of `Task<T>` or `ValueTask<T>`.

Async methods support the following suspension points:

* Calling another async method. No special instructions need to be provided. If the callee suspends, the caller will suspend as well.
* Using new .NET runtime APIs to "await" an "INotifyCompletion" type. The signatures of these methods shall be:
  ```C#
  // public static async2 Task AwaitAwaiterFromRuntimeAsync<TAwaiter>(TAwaiter awaiter) where TAwaiter : INotifyCompletion
  public static void modreq([System.Runtime]System.Threading.Tasks.Task) AwaitAwaiterFromRuntimeAsync<TAwaiter>(TAwaiter awaiter) where TAwaiter : INotifyCompletion
  // public static async2 Task UnsafeAwaitAwaiterFromRuntimeAsync<TAwaiter>(TAwaiter awaiter) where TAwaiter : ICriticalNotifyCompletion
  public static void modreq([System.Runtime]System.Threading.Tasks.Task) UnsafeAwaitAwaiterFromRuntimeAsync<TAwaiter>(TAwaiter awaiter) where TAwaiter : ICriticalNotifyCompletion
  ```

Each of the above methods will have semantics analogous to the current AsyncTaskMethodBuilder.AwaitOnCompleted/AwaitUnsafeOnCompleted methods. After calling this method, it can be presumed that the task has completed.

Only local variables which are "hoisted" may be used across suspension points. That is, only "hoisted" local variables will have their state preserved after returning from a suspension. On methods with the `localsinit` flag set, non-"hoisted" local variables will be initialized to their default value when resuming from suspension. Otherwise, these variables will have an undefined value. To identify "hoisted" local variables, they must have an optional custom modifier to the `System.Runtime.CompilerServices.HoistedLocal` class, which will be a new .NET runtime API. This custom modifier must be the last custom modifier on the variable. It is invalid for by-ref variables, or variables with a by-ref-like type, to be marked hoisted. Hoisted local variables are stored in managed memory and cannot be converted to unmanaged pointers without explicit pinning.
The code generator is free to ignore the `HoistedLocal` modifier if it can prove that this makes no observable difference in the execution of the generated program. This can be observable in diagnostics since it may mean the value of a local with the `HoistedLocal` modifier will not be available after certain suspension points.

Async methods have some temporary restrictions with may be lifted later:
* The `tail` prefix is forbidden
* Usage of the `localloc` instruction is forbidden
* Pinning locals may not be marked `HoistedLocal`

Other restrictions are likely to be permanent, including
* By-ref locals cannot be hoisted across suspension points
* Suspension points may not appear in exception handling blocks.
* Only four types will be supported as the return type for "runtime-async" methods: `System.Threading.Task`, `System.Threading.ValueTask`, `System.Threading.Task<T>`, and `System.Threading.ValueTask<T>`

All async methods effectively have two entry points, or signatures. The first signature is the one present in the above code: a `Task` or `ValueTask` returning method. The second signature is a "secondary signature", described in further detail in [I.8.6.1.5 Method signatures].

Callers may retrieve a Task-equivalent return type from an async method via calling the "Task-equivalent signature". This functionality is available in both sync and async methods.

### I.8.6.1.5 Method signatures

The list of relevant components is augmented to include sync vs. async method types. Async methods have some additions to normal signature compatibility.

Async methods are capable of calling certain methods using an "async signature." The target method must return one of `Task`, `ValueTask`, `Task<T>`, or `ValueTask<T>`.

The async signature is generated based on the primary signature. The transformation is as follows:
* If the target method return type is `Task` or `ValueTask`, the return type of the secondary signature is `void` with the first custom modifier on the return type being the original type.
* Otherwise, the return type is the type argument of the return type (either ``Task`1`` or ``ValueTask`1``) with the first custom modifier on the return type being the original type.

It is an error to declare a method with an "async signature".

_[Note: these rules operate before generic substitution, meaning that a method which only meets requirements after substitution would not be considered as valid.]_

### II.15.4.6 async methods

In certain cases described in [I.8.6.1.5 Method signatures] MethodDef definitions for sync and async methods may result in the definition of additional "implicit" definitions. All call sites to an implicit definition must use a MethodRef, even if the implicit method is definined inside the same module or assembly. The "explicit" definition which exists in metadata can be called using a regular `MethodDef` token.
