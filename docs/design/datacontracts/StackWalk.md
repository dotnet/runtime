# Contract StackWalk

This contract encapsulates support for StackWalking managed threads.

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

This contract depends on the following descriptors:

| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| `Frame` | `Next` | Pointer to next from on linked list |
| `InlinedCallFrame` | `CallSiteSP` | SP saved in Frame |
| `InlinedCallFrame` | `CallerReturnAddress` | Return address saved in Frame |
| `InlinedCallFrame` | `CalleeSavedFP` | FP saved in Frame |
| `SoftwareExceptionFrame` | `TargetContext` | Context object saved in Frame |
| `SoftwareExceptionFrame` | `ReturnAddress` | Return address saved in Frame |

<!--- TODO: Add Enum values when they are merged -->

The `StackWalk` contract provides an interface for walking the stack of a managed thread. It includes methods to create a stack walk, move to the next frame, get the current frame, retrieve the raw context, and get the frame address.

### Stackwalking
To create a full walk of the managed stack, two types of 'stacks' must be read.

1. True call frames on the thread's stack
2. Capital "F" Frames (referred to as Frames as opposed to frames) which are used by the runtime for book keeping purposes.

Capital "F" Frames are pushed and popped to a singly-linked list on the runtime's Thread object and are accessible using the [IThread](./Thread.md) contract. These capital "F" Frames are allocated within a functions call frame. For our purposes, these are relevant because they mark every transition where managed code calls native code. For more information about Frames see: [BOTR Stack Walking](https://github.com/dotnet/runtime/blob/44b7251f94772c69c2efb9daa7b69979d7ddd001/docs/design/coreclr/botr/stackwalking.md).

Unwinding call frames on the stack usually requires an OS specific implementation. Unwind codes have different formats across Windows/MacOS/Linux. However, in our particular circumstance of unwinding only **managed function** call frames, the runtime uses Windows unwind logic/codes for all platforms. Therefore we delegate to the existing native unwinding code located in `src/coreclr/unwinder/`. For more information on Windows unwinding algorithm and codes see the following:

* [Windows x64](https://learn.microsoft.com/en-us/cpp/build/exception-handling-x64)
* [Windows ARM64](https://learn.microsoft.com/en-us/cpp/build/arm64-exception-handling)

#### Simplified Stackwalking Algorithm
The intuition for walking a managed stack is relatively simply. Capital "F" Frames are used as checkpoints to get into a section of managed code at which point we use the native unwinder until we get to a section of code that is not managed. Since captial "F" Frame mark every managed call into native code, by iterating the capital "F" Frames we ensure all managed call frames are walked.

1. Read thread context. Fetched as part of the [ICorDebugDataTarget](https://learn.microsoft.com/en-us/dotnet/framework/unmanaged-api/debugging/icordebugdatatarget-getthreadcontext-method) COM interface.
2. If the current IP is in managed code, use Windows style unwinding until the IP is not in managed code.
3. For each captial "F" Frame on the linked list:
    1. If the Frame can update the context, update the context. Frames where `NeedsUpdateRegDisplay() == TRUE` can update the context.
    2. If the context was updated and the new IP is in managed code, use Windows style unwinding until the IP is not in managed code.

##### Example

Given the following call stack and capital "F" Frames linked list, we can apply the above algorithm.
<table>
<tr>
<th> Call Stack (grow down) </th>
<th> Capital "F" Frames Linked List </th>
</tr>
<tr>
<td>

```
   |  Native   | <A>
 - |           |
   |-----------| <B>
   |  Managed  |
   |-----------| <C>
   |           |
   |  Native   |
   |           |
   |-----------| <D>
   |           |
   |           |
   |  Managed  |
   |           |
   |           |
   |-----------| <E>
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
* (2) The context `<A>` is not pointing to managed code, therefore we don't unwind.
This would be checked through the [ExecutionManager](./ExecutionManager.md) contract, but in our example we can see the SP corresponds to a native frame.
* (3.1) We look at the first Frame, an InlinedCallFrame with attatched context `<B>`. Update our working context to `<B>`.
* (3.2) Since we updated our context and our new context is in managed code, we use the Windows unwinding tool to iteratively unwind until the context is no longer managed.
This could take multible iterations, but would end up with context `<C>` which has the first SP pointing to a native portion of the stack.
* (3.1) We look at the next Frame, an InlinedCallFrame with attatched context `<D>`. Update our working context to `<D>`.
* (3.2) Again, our updated context is in managed code. We will use the Windows unwinding tool to iteratively unwind until the context is no longer managed. This yields context `<E>`.
* Since we are at a native context and have no more capital "F" Frames to process, the managed stack walk is complete.

#### Stackwalking Algorithm
The actual implementation is a little more complex because:
* It requires pausing to return the context and Frame at certain points.
* Handles checking for "skipped frames". This can occur if an explicit frame is allocated in a managed stack frame (e.g. an inlined pinvoke call).

1. Read thread context. Fetched as part of the [ICorDebugDataTarget](https://learn.microsoft.com/en-us/dotnet/framework/unmanaged-api/debugging/icordebugdatatarget-getthreadcontext-method) COM interface.
2. If the current IP is in managed code, use Windows style unwinding iteratively, returning a new context each iteration, until the IP is not in managed code.
3. Iterate each Frame in the linked list:
    1. Check the current Frame, if it can update the context, do so. Frames where `Frame::NeedsUpdateRegDisplay() == TRUE` can update the context.
    2. For all Frame types except `InlinedCallFrame` with an active call, go to the next the Frame.
    3. Return the current context.
    4. Check for skipped frames by comparing the address of the current Frame (allocated on the stack) with the caller of the current context's stack pointer (found by unwinding context one iteration).
    If the address of the Frame is less than the caller's stack pointer, go to the next Frame, return the current context and repeat this step.
    5. If the context was updated and the new IP is in managed code, use Windows style unwinding iteratively, returning a new context each iteration, until the IP is not in managed code.


### CreateStackWalk

```csharp
IStackWalkHandle CreateStackWalk(ThreadData threadData);
```

Creates a stack walk handle for the given thread data. This initializes the context and frame iterator for the stack walk.

### Next

```csharp
bool Next(IStackWalkHandle stackWalkHandle);
```

Moves to the next frame in the stack walk. Returns `true` if successfully moved to the next frame. Otherwise returns `false`.

### GetCurrentFrame

```csharp
IStackDataFrameHandle GetCurrentFrame(IStackWalkHandle stackWalkHandle);
```

Gets the current frame in the stack walk. Returns a handle to the stack data frame.

### GetRawContext

```csharp
byte[] GetRawContext(IStackDataFrameHandle stackDataFrameHandle);
```

Retrieves the raw Windows thread context of the current frame as a byte array. The size and shape of the context is platform dependent. See [CONTEXT structure](https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-context) for more info.

### GetFrameAddress

```csharp
TargetPointer GetFrameAddress(IStackDataFrameHandle stackDataFrameHandle);
```

Gets the frame address of the current frame. Returns `TargetPointer.Null` if the frame is not valid.
