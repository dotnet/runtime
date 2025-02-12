# Contract StackWalk

This contract encapsulates support for walking the stack of managed threads.

## APIs of contract

```csharp
public interface IStackWalkHandle { };
public interface IStackDataFrameHandle { };
```

```csharp
// Creates a stack walk and returns a handle
IStackWalkHandle CreateStackWalk(ThreadData threadData);
// Iterates the stackWalkHandle to the next frame. If successful, returns true. Otherwise false.
bool Next(IStackWalkHandle stackWalkHandle);
// Gets the current frame from a stack walk and returns a IStackDataFrameHandle to it.
IStackDataFrameHandle GetCurrentFrame(IStackWalkHandle stackWalkHandle);
// Gets the thread context at the given stack dataframe.
byte[] GetRawContext(IStackDataFrameHandle stackDataFrameHandle);
// Gets the Frame address at the given stack dataframe. Returns TargetPointer.Null if the current dataframe does not have a valid Frame.
TargetPointer GetFrameAddress(IStackDataFrameHandle stackDataFrameHandle);
```

## Version 1
To create a full walk of the managed stack, two types of 'stacks' must be read.

1. True call frames on the thread's stack
2. Capital "F" Frames (referred to as Frames as opposed to frames) which are used by the runtime for book keeping purposes.

Capital "F" Frames are pushed and popped to a singly-linked list on the runtime's Thread object and are accessible using the [IThread](./Thread.md) contract. These capital "F" Frames are allocated within a functions call frame, meaning they also live on the stack. A subset of Frame types store extra data allowing us to recover a portion of the context from when they were created For our purposes, these are relevant because they mark every transition where managed code calls native code. For more information about Frames see: [BOTR Stack Walking](https://github.com/dotnet/runtime/blob/44b7251f94772c69c2efb9daa7b69979d7ddd001/docs/design/coreclr/botr/stackwalking.md).

Unwinding call frames on the stack usually requires an OS specific implementation. However, in our particular circumstance of unwinding only **managed function** call frames, the runtime uses Windows unwind logic/codes for all platforms (this isn't true for NativeAOT). Therefore we can delegate to the existing native unwinding code located in `src/coreclr/unwinder/`. For more information on the Windows unwinding algorithm and unwind codes see the following docs:

* [Windows x64](https://learn.microsoft.com/en-us/cpp/build/exception-handling-x64)
* [Windows ARM64](https://learn.microsoft.com/en-us/cpp/build/arm64-exception-handling)

This contract depends on the following descriptors:

| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| `Frame` | `Next` | Pointer to next from on linked list |
| `InlinedCallFrame` | `CallSiteSP` | SP saved in Frame |
| `InlinedCallFrame` | `CallerReturnAddress` | Return address saved in Frame |
| `InlinedCallFrame` | `CalleeSavedFP` | FP saved in Frame |
| `SoftwareExceptionFrame` | `TargetContext` | Context object saved in Frame |
| `SoftwareExceptionFrame` | `ReturnAddress` | Return address saved in Frame |

Global variables used:
| Global Name | Type | Purpose |
| --- | --- | --- |
| For each FrameType `<frameType>`, `<frameType>##Identifier` | FrameIdentifier enum value | Identifier used to determine concrete type of Frames |

Contracts used:
| Contract Name |
| --- |
| `ExecutionManager` |
| `Thread` |


### Simplified Stackwalking Algorithm
The intuition for walking a managed stack is relatively simply: unwind managed portions of the stack until we hit native code then use capital "F" Frames as checkpoints to get into new sections of managed code. Because Frames are added at each point before managed code (higher SP value) calls native code (lower SP values), we are guarenteed that a Frame exists at the top (lower SP value) of each managed call frame run.

1. Read the thread context. Fetched as part of the [ICorDebugDataTarget](https://learn.microsoft.com/en-us/dotnet/framework/unmanaged-api/debugging/icordebugdatatarget-getthreadcontext-method) COM interface.
2. If the current IP is in managed code, use Windows style unwinding until the IP is not in managed code.
3. For each captial "F" Frame on the linked list:
    1. If the Frame can update the context, update the context.
    2. If the context was updated and the new IP is in managed code, use Windows style unwinding until the IP is not in managed code.

#### Example

Given the following call stack and capital "F" Frames linked list, we can apply the above algorithm.
<table>
<tr>
<th> Call Stack (growing down)</th>
<th> Capital "F" Frames Linked List </th>
</tr>
<tr>
<td>

```
   |  Native   | <- <A>'s SP
 - |           |
   |-----------| <- <B>'s SP
   |  Managed  |
   |-----------| <- <C>'s SP
   |           |
   |  Native   |
   |           |
   |-----------| <- <D>'s SP
   |           |
   |           |
   |  Managed  |
   |           |
   |           |
   |-----------| <- <E>'s SP
   |           |
   |  Native   |
 + |           |
   | StackBase |
```
</td>
<td>

```
InlinedCallFrame
(Context = <B>)

       ||
       \/

InlinedCallFrame
(Context = <D>)

       ||
       \/

      NULL
```

</td>
</tr>
</table>

* (1) We fetch the initial thread context `<A>`, with SP pointing to the top of the call stack.
* (2) In our example we can see that `<A>`'s SP is pointing to a native frame on the stack. Therefore context `<A>` is not pointing to managed code and we don't unwind.
In actuality, this is checked through the [ExecutionManager](./ExecutionManager.md) contract.
* (3.1) We look at the first Frame, an InlinedCallFrame with attatched context `<B>`. Update our working context to `<B>`.
* (3.2) Since we updated our context and our new context is in managed code, we use the Windows unwinding tool to iteratively unwind until the context is no longer managed.
This could take multible iterations, but would end up with context `<C>` which has the first SP pointing to a native portion of the stack.
* (3.1) We look at the next Frame, an InlinedCallFrame with attatched context `<D>`. Update our working context to `<D>`.
* (3.2) Again, our updated context is in managed code. We will use the Windows unwinding tool to iteratively unwind until the context is no longer managed. This yields context `<E>`.
* Since we are at a native context and have no more capital "F" Frames to process, the managed stack walk is complete.

### Stackwalking Algorithm
In reality, the actual algorithm is a little more complex because:
* It requires pausing to return the current context and Frame at certain points.
* Handles checking for "skipped frames" which can occur if an explicit frame is allocated in a managed stack frame (e.g. an inlined pinvoke call).

1. Read the thread context. Fetched as part of the [ICorDebugDataTarget](https://learn.microsoft.com/en-us/dotnet/framework/unmanaged-api/debugging/icordebugdatatarget-getthreadcontext-method) COM interface.
2. If the current IP is in managed code, use Windows style unwinding iteratively, returning a new context each iteration, until the IP is not in managed code.
3. Iterate each Frame in the linked list:
    1. Check the current Frame, if it can update the context, do so.
    2. For all Frame except `InlinedCallFrame` with an active call, go to the next the Frame.
    3. Return the current context.
    4. Check for skipped frames by comparing the address of the current Frame (allocated on the stack) with the caller of the current context's stack pointer (found by unwinding current context one iteration).
    If the address of the Frame is less than the caller's stack pointer, go to the next Frame, return the current context and repeat this step.
    5. If the context was updated and the new IP is in managed code, use Windows style unwinding iteratively, returning a new context each iteration, until the IP is not in managed code.

### APIs


The majority of the contract's complexity comes from the stack walking algorithm implementation which is implemented and controlled through the following APIs.
These handle setting up and iterating the stackwalk state according to the algorithm detailed above.

```csharp
IStackWalkHandle CreateStackWalk(ThreadData threadData);
bool Next(IStackWalkHandle stackWalkHandle);
```

The rest of the APIs convey state about the stack walk at a given point which fall out of the stack walking algorithm relatively simply.

`GetCurrentFrame` creates a copy of the stack walk state which remains constant even if the stack walk is iterated.
```csharp
IStackDataFrameHandle GetCurrentFrame(IStackWalkHandle stackWalkHandle);
```

`GetRawContext` Retrieves the raw Windows thread context of the current frame as a byte array. The size and shape of the context is platform dependent. See [CONTEXT structure](https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-context) for more info.
This context is not guarenteed to be complete. Not all capital "F" Frames store the entire context, some only store the IP/SP/FP. Therefore, at points where the context is based on these Frames it will be incomplete.
```csharp
byte[] GetRawContext(IStackDataFrameHandle stackDataFrameHandle);
```


`GetFrameAddress` gets the address of the current capital "F" Frame. This is only valid if the `IStackDataFrameHandle` is at a point where the context is based on a capital "F" Frame. For example, it is not valid when when the current context was created by using the Windows unwinder.
If the Frame is not valid, returns `TargetPointer.Null`.

```csharp
TargetPointer GetFrameAddress(IStackDataFrameHandle stackDataFrameHandle);
```

