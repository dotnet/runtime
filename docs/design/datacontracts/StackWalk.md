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

Unwinding call frames on the stack usually requires an OS specific implementation. However, in our particular circumstance of unwinding only **managed function** call frames, the runtime uses Windows style unwind logic/codes for all platforms (this isn't true for NativeAOT). Therefore we can delegate to the existing native unwinding code located in `src/coreclr/unwinder/`. For more information on the Windows unwinding algorithm and unwind codes see the following docs:

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


### Stackwalk Algorithm
The intuition for walking a managed stack is relatively simply: unwind managed portions of the stack until we hit native code then use capital "F" Frames as checkpoints to get into new sections of managed code. Because Frames are added at each point before managed code (higher SP value) calls native code (lower SP values), we are guaranteed that a Frame exists at the top (lower SP value) of each managed call frame run.

In reality, the actual algorithm is a little more complex fow two reasons. It requires pausing to return the current context and Frame at certain points and it checks for "skipped Frames" which can occur if an capital "F" Frame is allocated in a managed stack frame (e.g. an inlined P/Invoke call).


1. Read the thread context. Fetched as part of the [ICorDebugDataTarget](https://learn.microsoft.com/en-us/dotnet/framework/unmanaged-api/debugging/icordebugdatatarget-getthreadcontext-method) COM interface.
2. If the current IP is in managed code, use Windows style unwinding iteratively, returning the updated context at each iteration, until the IP is not in managed code.
3. Iterate each Frame in the linked list:
    1. Check the current Frame, if it can update the context, do so.
    2. For all Frame except `InlinedCallFrame` with an active call, go to the next the Frame.
    3. Return the current context.
    4. Check for skipped Frames by comparing the address of the current Frame (allocated on the stack) with the caller of the current context's stack pointer (found by unwinding current context one iteration).
    If the address of the Frame is less than the caller's stack pointer, go to the next Frame, return the current context and repeat this step.
    5. If the context was updated and the new IP is in managed code, use Windows style unwinding iteratively, returning a new context each iteration, until the IP is not in managed code.

#### Simple Example

In this example we walk through the algorithm without instances of skipped Frames.

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
SoftwareExceptionFrame
   (Context = <B>)

          ||
          \/

SoftwareExceptionFrame
   (Context = <D>)

          ||
          \/

   NULL TERMINATOR
```

</td>
</tr>
</table>

* (1) We fetch the initial thread context `<A>`, with SP pointing to the top of the call stack.
* (2) In our example we can see that `<A>`'s SP is pointing to a native frame on the stack. Therefore context `<A>` is not pointing to managed code and we don't unwind.
In actuality, this is checked through the [ExecutionManager](./ExecutionManager.md) contract.
* (3.1) We look at the current (first) Frame, a SoftwareExceptionFrame with attached context `<B>`. Update our working context to `<B>`.
* (3.2) Since the current Frame is not an InlinedCallFrame, set the current Frame to the next Frame on the chain.
* (3.3) Return the current context and Frame.
* (3.4) The example has no skipped Frames so we skip this step.
* (3.5) Since we updated our context and our new context is in managed code, we use the Windows style unwinding tool to iteratively unwind until the context is no longer managed.
This could take multiple iterations, each time returning a new context to the caller. We end up with context `<C>` which is the first context inside of a native call frame.
* (3.1) We look at the current Frame, another SoftwareExceptionFrame with attached context `<D>`. Update our working context to `<D>`.
* (3.2) Since the current Frame is not an InlinedCallFrame, set the current Frame to the next Frame (`NULL TERMINATOR`) on the chain.
* (3.3) Return the current context and Frame.
* (3.4) The example has no skipped Frames so we skip this step.
* (3.5) Again, we updated our context and our new context is in managed code. Therefore, we use the Windows style unwinding tool to iteratively unwind until the context is no longer managed.
This could take multiple iterations, each time returning a new context to the caller. We end up with context `<E>` which is the first context inside of a native call frame.
* (3) Since our current context is in native code and the current Frame is the `NULL TERMINATOR`, we are done.

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

`GetRawContext` Retrieves the raw Windows style thread context of the current frame as a byte array. The size and shape of the context is platform dependent.

* On Windows the context is defined directly in Windows header `winnt.h`. See [CONTEXT structure](https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-context) for more info.
* On non-Windows platform the context's are defined in `src/coreclr/pal/inc/pal.h` and should mimic the Windows structure.

This context is not guaranteed to be complete. Not all capital "F" Frames store the entire context, some only store the IP/SP/FP. Therefore, at points where the context is based on these Frames it will be incomplete.
```csharp
byte[] GetRawContext(IStackDataFrameHandle stackDataFrameHandle);
```


`GetFrameAddress` gets the address of the current capital "F" Frame. This is only valid if the `IStackDataFrameHandle` is at a point where the context is based on a capital "F" Frame. For example, it is not valid when when the current context was created by using the stack frame unwinder.
If the Frame is not valid, returns `TargetPointer.Null`.

```csharp
TargetPointer GetFrameAddress(IStackDataFrameHandle stackDataFrameHandle);
```

