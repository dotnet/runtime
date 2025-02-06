
This document is a draft of changes to ECMA-335 for the "runtime async" feature. When the feature is officially supported, it can be merged into the final ECMA-335 augments document.

# Runtime-async

Async is currently a feature implemented by various .NET languages as a compiler rewrite to support methods that can "yield" control back to their caller at specific "suspension" points. While effective, it's believed that implementation directly in the .NET runtime can provide improvements, especially in performance.

## Spec modifications

These are proposed modifications to the ECMA-335 specification for runtime-async.

### I.8.4.5 Sync and Async Methods

Methods may be either 'sync' or 'async'. Async method definitions are methods attributed with `[MethodImpl(MethodImplOptions.Async)]`.

Applicability of `MethodImplOptions.Async`:
* The `[MethodImpl(MethodImplOptions.Async)]` only has effect when applied to method definitions that return generic or nongeneric variants of Task or ValueTask.
* The `[MethodImpl(MethodImplOptions.Async)]` only has effect when applied to method definitions with CIL implementation.
* Async method definitions are only valid inside async-capable assemblies. An async-capable assembly is one which references a corlib containing an `abstract sealed class RuntimeFeature` with a `public const string` field member named `Async`.
* Combining `MethodImplOptions.Async` with `MethodImplOptions.Synchronized` is invalid.
* Applying `MethodImplOptions.Async` to methods with `byref` or `ref-like` parameters is invalid.
* Applying `MethodImplOptions.Async` to vararg methods is invalid.

_[Note: these rules operate before generic substitution, meaning that a method which only meets requirements after substitution would not be considered as valid.]_

Sync methods are all other methods.

Unlike sync methods, async methods support suspension. Suspension allows async methods to yield control flow back to their caller at certain well-defined suspension points, and resume execution of the remaining method at a later time or location, potentially on another thread.

Async methods also do not have matching return type conventions as sync methods. For sync methods, the stack should contain a value convertible to the stated return type before the `ret` instruction. For async methods, the stack should be empty in the case of `Task` or `ValueTask`, or the type argument in the case of `Task<T>` or `ValueTask<T>`.

Async methods support suspension using one of the following methods:

  ```C#
  namespace System.Runtime.CompilerServices
  {
      public static class RuntimeHelpers
      {
          [MethodImpl(MethodImplOptions.Async)]
          public static void AwaitAwaiterFromRuntimeAsync<TAwaiter>(TAwaiter awaiter) where TAwaiter : INotifyCompletion { ... }
          [MethodImpl(MethodImplOptions.Async)]
          public static void UnsafeAwaitAwaiterFromRuntimeAsync<TAwaiter>(TAwaiter awaiter) where TAwaiter : ICriticalNotifyCompletion
          
          [MethodImpl(MethodImplOptions.Async)]
          public static void Await(Task task);
          [MethodImpl(MethodImplOptions.Async)]
          public static void Await(ValueTask task);
          [MethodImpl(MethodImplOptions.Async)]
          public static T Await<T>(Task<T> task);
          [MethodImpl(MethodImplOptions.Async)]
          public static T Await<T>(ValueTask<T> task);
      }
  }
  ```

These methods are only legal to call inside async methods. The `...AwaitAwaiter...` methods will have semantics analogous to the current `AsyncTaskMethodBuilder.AwaitOnCompleted/AwaitUnsafeOnCompleted` methods. After calling either method, it can be presumed that the task or awaiter has completed. The `Await` methods perform suspension like the `AwaitAwaiter...` methods, but are optimized for calling on the return value of a call to an async method. To achieve maximum performance, the IL sequence of two `call` instructions -- one to the async method and immediately one to the `Await` method -- should be preferred.

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


### II.23.1.11 Flags for methods [MethodImplAttributes]

| Flag  | Value | Description |
| ------------- | ------------- | ------------- |
| . . . | . . . | . . . |
|Async |0x0400 |Method is an Async Method.|
