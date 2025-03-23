# Contract StackWalk

This contract encapsulates support for walking the stack of managed threads.

## APIs of contract

```csharp
public interface IStackDataFrameHandle { };
```

```csharp
// Creates a stack walk and returns a handle
IEnumerable<IStackDataFrameHandle> CreateStackWalk(ThreadData threadData);

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
| `FramedMethodFrame` | `TransitionBlockPtr` | Pointer to Frame's TransitionBlock |
| `TransitionBlock` | `ReturnAddress` | Return address associated with the TransitionBlock |
| `TransitionBlock` | `CalleeSavedRegisters` | Platform specific CalleeSavedRegisters struct associated with the TransitionBlock |
| `FuncEvalFrame` | `DebuggerEvalPtr` | Pointer to the Frame's DebuggerEval object |
| `DebuggerEval` | `TargetContext` | Context saved inside DebuggerEval |
| `DebuggerEval` | `EvalDuringException` | Flag used in processing FuncEvalFrame |
| `ResumableFrame` | `TargetContextPtr` | Pointer to the Frame's Target Context |
| `FaultingExceptionFrame` | `TargetContext` | Frame's Target Context |
| `HijackFrame` | `ReturnAddress` | Frame's stored instruction pointer |
| `HijackFrame` | `HijackArgsPtr` | Pointer to the Frame's stored HijackArgs |
| `HijackArgs` (amd64) | `CalleeSavedRegisters` | CalleeSavedRegisters data structure |
| `HijackArgs` (amd64 Windows) | `Rsp` | Saved stack pointer |
| `HijackArgs` (arm64) | For each register `r` saved in HijackArgs, `r` | Register names associated with stored register values |
| `CalleeSavedRegisters` | For each callee saved register `r`, `r` | Register names associated with stored register values |

Global variables used:
| Global Name | Type | Purpose |
| --- | --- | --- |
| For each FrameType `<frameType>`, `<frameType>##Identifier` | `FrameIdentifier` enum value | Identifier used to determine concrete type of Frames |

Contracts used:
| Contract Name |
| --- |
| `ExecutionManager` |
| `Thread` |


### Stackwalk Algorithm
The intuition for walking a managed stack is relatively simply: unwind managed portions of the stack until we hit native code then use capital "F" Frames as checkpoints to get into new sections of managed code. Because Frames are added at each point before managed code (higher SP value) calls native code (lower SP values), we are guaranteed that a Frame exists at the top (lower SP value) of each managed call frame run.

In reality, the actual algorithm is a little more complex fow two reasons. It requires pausing to return the current context and Frame at certain points and it checks for "skipped Frames" which can occur if an capital "F" Frame is allocated in a managed stack frame (e.g. an inlined P/Invoke call).

1. Setup
    1. Set the current context `currContext` to be the thread's context. Fetched as part of the [ICorDebugDataTarget](https://learn.microsoft.com/en-us/dotnet/framework/unmanaged-api/debugging/icordebugdatatarget-getthreadcontext-method) COM interface.
    2. Create a stack of the thread's capital "F" Frames `frameStack`.
2. **Return the current context**.
3. While the `currContext` is in managed code or `frameStack` is not empty:
    1. If `currContext` is native code, pop the top Frame from `frameStack` update the context using the popped Frame. **Return the updated context** and **go to step 3**.
    2. If `frameStack` is not empty, check for skipped Frames. Peek `frameStack` to find a Frame `frame`. Compare the address of `frame` (allocated on the stack) with the caller of the current context's stack pointer (found by unwinding current context one iteration).
    If the address of the `frame` is less than the caller's stack pointer, **return the current context**, pop the top Frame from `frameStack`, and **go to step 3**.
    3. Unwind `currContext` using the Windows style unwinder. **Return the current context**.


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
Managed Call:   -----------

   |  Native   | <- <A>'s SP
 - |           |
   |-----------| <- <B>'s SP
   |           |
   |  Managed  |
   |           |
   |-----------| <- <C>'s SP
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

   NULL TERMINATOR
```

</td>
</tr>
</table>

1. (1) Set `currContext` to the thread context `<A>`. Create a stack of Frames `frameStack`.
2. (2) Return the `currContext` which has the threads context.
3. (3) `currContext` is in unmanaged code (native) however, because `frameStack` is not empty, we begin processing the context.
4. (3.1) Since `currContext` is unmanaged. We pop the SoftwareExceptionFrame from `frameStack` and use it to update `currContext`. The SoftwareExceptionFrame is holding context `<B>` which we set `currContext` to. Return the current context and go back to step 3.
5. (3) Now `currContext` is in managed code as shown by `<B>`'s SP. Therefore, we begin to process the context.
6. (3.1) Since `currContext` is managed, skip step 3.1.
7. (3.2) Since `frameStack` is empty, we do not check for skipped Frames.
8. (3.3) Unwind `currContext` a single iteration to `<C>` and return the current context.
9. (3) `currContext` is now at unmanaged (native) code and `frameStack` is empty. Therefore we are done.

The following C# code could yield a stack similar to the example above:
```csharp
void foo()
{
    // Call native code or function that calls down to native.
    Console.ReadLine();
    // Capture stack trace while inside native code.
}
```

#### Skipped Frame Example
The skipped Frame check is important when managed code calls managed code through an unmanaged boundary.
This occurs when calling a function marked with `[UnmanagedCallersOnly]` as an unmanaged delegate from a managed caller.
In this case, if we ignored the skipped Frame check we would miss the unmanaged boundary.

Given the following call stack and capital "F" Frames linked list, we can apply the above algorithm.
<table>
<tr>
<th> Call Stack (growing down)</th>
<th> Capital "F" Frames Linked List </th>
</tr>
<tr>
<td>

```
Unmanaged Call: -X-X-X-X-X-
Managed Call:   -----------
InlinedCallFrame location: [ICF]

   |  Managed  | <- <A>'s SP
 - |           |
   |           |
   |-X-X-X-X-X-| <- <B>'s SP
   |   [ICF]   |
   |  Managed  |
   |           |
   |-----------| <- <C>'s SP
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

 NULL TERMINATOR
```

</td>
</tr>
</table>

1. (1) Set `currContext` to the thread context `<A>`. Create a stack of Frames `frameStack`.
2. (2) Return the `currContext` which has the threads context.
3. (3) Since `currContext` is in managed code, we begin to process the context.
4. (3.1) Since `currContext` is managed, skip step 3.1.
5. (3.2) Check for skipped Frames. Copy `currContext` into `parentContext` and unwind `parentContext` once using the Windows style unwinder. As seen from the call stack, unwinding `currContext=<A>` will yield `<C>`. We peek the top of `frameStack` and find an InlinedCallFrame (shown in call stack above as `[ICF]`). Since `parentContext`'s SP is greater than the address of `[ICF]` there are no skipped Frames.
6. (3.3) Unwind `currContext` a single iteration to `<B>` and return the current context.
7. (3) Since `currContext` is still in managed code, we continue processing the context.
8. (3.1) Since `currContext` is managed, skip step 3.1.
9. (3.2) Check for skipped Frames. Copy `currContext` into `parentContext` and unwind `parentContext` once using the Windows style unwinder. As seen from the call stack, unwinding `currContext=<B>` will yield `<C>`. We peek the top of `frameStack` and find an InlinedCallFrame (shown in call stack above as `[ICF]`). This time the the address of `[ICF]` is less than `parentContext`'s SP. Therefore we return the current context then pop the InlinedCallFrame from `frameStack` which is now empty and return to step 3.
10. (3) Since `currContext` is still in managed code, we continue processing the context.
11. (3.1) Since `currContext` is managed, skip step 3.1.
12. (3.2) Since `frameStack` is empty, we do not check for skipped Frames.
13. (3.3) Unwind `currContext` a single iteration to `<C>` and return the current context.
14. (3) `currContext` is now at unmanaged (native) code and `frameStack` is empty. Therefore we are done.

The following C# code could yield a stack similar to the example above:
```csharp
void foo()
{
    var fptr = (delegate* unmanaged<void>)&bar;
    fptr();
}

[UnmanagedCallersOnly]
private static void bar()
{
    // Do something
    // Capture stack trace while in here
}
```

### Capital 'F' Frame Handling

Capital 'F' Frame's store context data in a number of different ways. Of the couple dozen Frame types defined in `src/coreclr/vm/frames.h` several do not store any context data or update the context, signified by `NeedsUpdateRegDisplay_Impl() == false`. Of that Frames that do update the context, several share implementations of `UpdateRegDisplay_Impl` through inheritance. This leaves us with 9 distinct mechanisms to update the context that will be detailed below. Each mechanism is referred to using the Frame class that implements the mechanism and may be used by subclasses.

Most of the handlers are implemented in `BaseFrameHandler`. Platform specific components are implemented/overridden in `<arch>FrameHandler`.

#### InlinedCallFrame

InlinedCallFrames store and update only the IP, SP, and FP of a given context. If the stored IP (CallerReturnAddress) is 0 then the InlinedCallFrame does not have an active call and should not update the context.

#### SoftwareExceptionFrame

SoftwareExceptionFrames store a copy of the context struct. The IP, SP, and all ABI specified (platform specific) callee-saved registers are copied from the stored context to the working context.

#### TransitionFrame

TransitionFrames hold a pointer to a `TransitionBlock`. The TransitionBlock holds a return address along with a `CalleeSavedRegisters` struct which has values for all ABI specified callee-saved registers. The SP can be found using the address of the TransitionBlock. Since the TransitionBlock will be the lowest element on the stack, the SP is the address of the TransitionBlock + sizeof(TransitionBlock).

When updating the context from a TransitionFrame, the IP, SP, and all ABI specified callee-saved registers are copied over.

The following Frame types also use this mechanism:
* FramedMethodFrame
* CLRToCOMMethodFrame
* PInvokeCallIFrame
* PrestubMethodFrame
* StubDispatchFrame
* CallCountingHelperFrame
* ExternalMethodFrame
* DynamicHelperFrame

#### FuncEvalFrame

FuncEvalFrames hold a pointer to a `DebuggerEval`. The DebuggerEval holds a full context which is completely copied over to the working context when updating.

#### ResumableFrame

ResumableFrames hold a pointer to a context object (Note this is different from SoftwareExceptionFrames which hold the context directly). The entire context object is copied over to the working context when updating.

RedirectedThreadFrames also use this mechanism.

#### FaultingExceptionFrame

FaultingExceptionFrames have two different implementations. One for Windows x86 and another for all other builds (with funclets).

Given the cDAC does not yet support Windows x86, this version is not supported.

The other version stores a context struct. To update the working context, the entire stored context is copied over. In addition the `ContextFlags` are updated to ensure the `CONTEXT_XSTATE` bit is not set given the debug version of the contexts can not store extended state. This bit is architecture specific.

#### HijackFrame

HijackFrames carry a IP (ReturnAddress) and a pointer to `HijackArgs`. All platforms update the IP and use the platform specific HijackArgs to update further registers. The following details currently implemented platforms.

* x64 - On x64, HijackArgs contains a CalleeSavedRegister struct. The saved registers values contained in the struct are copied over to the working context.
    * Windows - On Windows, HijackArgs also contains the SP value directly which is copied over to the working context.
    * Non-Windows - On OS's other than Windows, HijackArgs does not contain an SP value. Instead since the HijackArgs struct lives on the stack, the SP is `&hijackArgs + sizeof(HijackArgs)`. This value is also copied over.
* arm64 - Unlike on x64, on arm64 HijackArgs contains a list of register values instead of the CalleeSavedRegister struct. These values are copied over to the working context. The SP is fetched using the same technique as on x64 non-Windows where `SP = &hijackArgs + sizeof(HijackArgs)` and is copied over to the working context.

#### TailCallFrame

TailCallFrames are only used on Windows x86 which is not yet supported in the cDAC and therefore not implemented.

#### HelperMethodFrame

HelperMethodFrames are on the way to being removed. They are not currently supported in the cDAC.

### APIs

The majority of the contract's complexity is the stack walking algorithm (detailed above) implemented as part of `CreateStackWalk`.
The `IEnumerable<IStackDataFrame>` return value is computed lazily.

```csharp
IEnumerable<IStackDataFrameHandle> CreateStackWalk(ThreadData threadData);
```

The rest of the APIs convey state about the stack walk at a given point which fall out of the stack walking algorithm relatively simply.

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

