# Contract StackWalk

This contract encapsulates support for walking the stack of managed threads.

## APIs of contract

```csharp
public interface IStackDataFrameHandle
{
    // Describes what the current Context/FrameIter of this handle represents.
    StackWalkState State { get; }
}

public enum StackWalkState
{
    Complete,
    Error,
    // Current Context represents a managed method.
    Frameless,
    // Current Context is the seed native context from init (the thread's saved
    // CONTEXT). FrameIter may or may not be on a Frame.
    InitialNativeContext,
    // Current Context is native, produced by unwinding a managed frame down to
    // an M2U boundary. FrameIter is on the explicit Frame at the transition.
    NativeMarker,
    // FrameIter is on an explicit Frame (FrameAddress is valid), but the
    // current Context has not yet been bridged through that Frame. Bridging
    // occurs via UpdateContextFromCurrentFrame; the next step advances past
    // this Frame.
    Frame,
    SkippedFrame,
}
```

```csharp
// Creates a stack walk and returns a handle
IEnumerable<IStackDataFrameHandle> CreateStackWalk(ThreadData threadData);

// Creates a stack walk and returns a handle, using a caller-provided seed CONTEXT.
// `contextBuffer` must be at least `IPlatformAgnosticContext.Size` bytes.
// `isFirst` indicates whether the seed frame should be treated as the active leaf.
IEnumerable<IStackDataFrameHandle> CreateStackWalk(
    ThreadData threadData,
    byte[] contextBuffer,
    bool isFirst = true);

// Gets the thread context at the given stack dataframe.
byte[] GetRawContext(
    IStackDataFrameHandle stackDataFrameHandle,
    StackwalkFlag flags = StackwalkFlag.Default);

[Flags]
enum StackwalkFlag
{
    Default = 0,
}

// Gets the Frame address at the given stack dataframe. Returns TargetPointer.Null if the current dataframe does not have a valid Frame.
TargetPointer GetFrameAddress(IStackDataFrameHandle stackDataFrameHandle);

// Gets the Frame name associated with the given Frame identifier. If no matching Frame name found returns an empty string.
string GetFrameName(TargetPointer frameIdentifier);

// Gets the method desc pointer associated with the given Frame.
TargetPointer GetMethodDescPtr(TargetPointer framePtr);

// Gets the method desc pointer associated with a given IStackDataFrameHandle
TargetPointer GetMethodDescPtr(IStackDataFrameHandle stackDataFrameHandle);

// Gets the instruction pointer from the current frame's context.
TargetPointer GetInstructionPointer(IStackDataFrameHandle stackDataFrameHandle);

// Walks the Thread's explicit (capital "F") Frame chain and yields a StackFrameData per Frame.
IEnumerable<StackFrameData> GetFrames(TargetPointer threadPointer);

// Returns true if the Frame at the given address is an InlinedCallFrame
// installed by the new EH helpers (Datum tagged with ExceptionHandlingHelper).
bool IsExceptionHandlingHelperInlinedCallFrame(TargetPointer frameAddress);

// Reads the DebuggerEval associated with a FuncEvalFrame at the given address
// and returns the metadata token and Assembly* the eval is rooted in.
DebuggerEvalData GetDebuggerEvalData(TargetPointer funcEvalFrameAddress);

// Walks the stack and returns all GC references found on each frame.
// This is the primary API for GC reference enumeration, used by SOSDacImpl.GetStackReferences.
IReadOnlyList<StackReferenceData> WalkStackReferences(ThreadData threadData);

// Returns a context for the thread, trying (in order): the debugger filter context,
// the OS thread context, or a context derived from the explicit Frame chain.
byte[] GetContext(ThreadData threadData, ThreadContextSource contextSource, uint contextFlags);

// Returns the saved TargetContext pointer carried by the head Frame, if applicable.
TargetPointer GetRedirectedContextPointer(ThreadData threadData);
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
| `InlinedCallFrame` (arm) | `SPAfterProlog` | Value of the SP after prolog. Used on ARM to maintain additional JIT invariant |
| `InlinedCallFrame` | `Datum` | MethodDesc ptr or on 64 bit host: CALLI target address (if lowest bit is set) or on windows x86 host: argument stack size (if value is <64k) |
| `SoftwareExceptionFrame` | `TargetContext` | Context object saved in Frame |
| `SoftwareExceptionFrame` | `ReturnAddress` | Return address saved in Frame |
| `FramedMethodFrame` | `TransitionBlockPtr` | Pointer to Frame's TransitionBlock |
| `FramedMethodFrame` | `MethodDescPtr` | Pointer to Frame's method desc |
| `StubDispatchFrame` | `MethodDescPtr` | Pointer to Frame's method desc |
| `StubDispatchFrame` | `RepresentativeMTPtr` | Pointer to Frame's method table pointer |
| `StubDispatchFrame` | `RepresentativeSlot` | Frame's method table slot |
| `StubDispatchFrame` | `Indirection` | Import slot pointer for GCRefMap resolution via `FindReadyToRunModule` |
| `ExternalMethodFrame` | `Indirection` | Import slot pointer for GCRefMap resolution via `FindReadyToRunModule` |
| `DynamicHelperFrame` | `DynamicHelperFrameFlags` | Flags indicating which argument registers contain GC references |
| `TransitionBlock` | `ReturnAddress` | Return address associated with the TransitionBlock |
| `TransitionBlock` | `CalleeSavedRegisters` | Platform specific CalleeSavedRegisters struct associated with the TransitionBlock |
| `TransitionBlock` | `OffsetOfArgs` | Byte offset of stack arguments (first arg after registers) = `sizeof(TransitionBlock)` |
| `TransitionBlock` | `ArgumentRegisters` | Byte offset of the argument registers area within the TransitionBlock |
| `TransitionBlock` | `FirstGCRefMapSlot` | Byte offset where GCRefMap slot enumeration begins. ARM64: RetBuffArgReg offset; others: ArgumentRegisters offset |
| `ReadyToRunInfo` | `ImportSections` | Pointer to array of `READYTORUN_IMPORT_SECTION` structs for GCRefMap resolution |
| `ReadyToRunInfo` | `NumImportSections` | Count of import sections in the array |
| `FuncEvalFrame` | `DebuggerEvalPtr` | Pointer to the Frame's DebuggerEval object |
| `FuncEvalFrame` | `ReturnAddress` | Return address of the frame |
| `DebuggerEval` | `TargetContext` | Context saved inside DebuggerEval |
| `DebuggerEval` | `EvalUsesHijack` | Flag used in processing FuncEvalFrame |
| `DebuggerEval` | `MethodToken` | Metadata token of the method being evaluated |
| `DebuggerEval` | `AssemblyPtr` | Pointer to the Assembly the eval is rooted in |
| `ResumableFrame` | `TargetContextPtr` | Pointer to the Frame's Target Context |
| `FaultingExceptionFrame` | `TargetContext` | Frame's Target Context |
| `HijackFrame` | `ReturnAddress` | Frame's stored instruction pointer |
| `HijackFrame` | `HijackArgsPtr` | Pointer to the Frame's stored HijackArgs |
| `HijackArgs` (amd64) | `CalleeSavedRegisters` | CalleeSavedRegisters data structure |
| `HijackArgs` (amd64 Windows) | `Rsp` | Saved stack pointer |
| `HijackArgs` (arm/arm64/x86) | For each register `r` saved in HijackArgs, `r` | Register names associated with stored register values |
| `InterpreterFrame` | `TopInterpMethodContextFrame` | Pointer to the InterpreterFrame's top `InterpMethodContextFrame` |
| `InterpreterFrame` | `IsFaulting` | Boolean indicating whether the topmost interpreted frame has thrown an exception. When set, the context for the top interpreted frame must include `CONTEXT_EXCEPTION_ACTIVE` so exception unwinders treat the IP as a faulting instruction rather than a return-from-call |
| `InterpMethodContextFrame` | `StartIp` | Pointer to the `InterpByteCodeStart` for resolving the MethodDesc |
| `InterpMethodContextFrame` | `ParentPtr` | Pointer to the parent `InterpMethodContextFrame` in the call chain (null for outermost frame) |
| `InterpMethodContextFrame` | `Ip` | The actual instruction pointer within the method (null if frame is inactive/reusable) |
| `InterpMethodContextFrame` | `NextPtr` | Pointer to the next `InterpMethodContextFrame` toward the top of the stack |
| `InterpMethodContextFrame` | `Stack` | Pointer to the stack base for this interpreted method, used as the frame pointer when interpreter GC info uses a stack-base register |
| `ArgumentRegisters` (arm) | For each register `r` saved in ArgumentRegisters, `r` | Register names associated with stored register values |
| `CalleeSavedRegisters` | For each callee saved register `r`, `r` | Register names associated with stored register values |
| `TailCallFrame` (x86 Windows) | `CalleeSavedRegisters` | CalleeSavedRegisters data structure |
| `TailCallFrame` (x86 Windows) | `ReturnAddress` | Frame's stored instruction pointer |
| `ExceptionInfo` | `ExceptionFlags` | Bit flags from `ExceptionFlags` class (`exstatecommon.h`). Used for GC reference reporting during stack walks with funclet handling. |
| `ExceptionInfo` | `StackLowBound` | Low bound of the stack range unwound by this exception |
| `ExceptionInfo` | `StackHighBound` | High bound of the stack range unwound by this exception |
| `ExceptionInfo` | `CSFEHClause` | Caller stack frame of the current EH clause |
| `ExceptionInfo` | `CSFEnclosingClause` | Caller stack frame of the enclosing clause |
| `ExceptionInfo` | `CallerOfActualHandlerFrame` | Stack frame of the caller of the catch handler |
| `ExceptionInfo` | `PreviousNestedInfo` | Pointer to previous nested ExInfo |
| `ExceptionInfo` | `PassNumber` | Exception handling pass (1 or 2) |
| `ExceptionInfo` | `ClauseForCatchHandlerStartPC` | Start PC offset of the catch handler clause, used for interruptible offset override |
| `ExceptionInfo` | `ClauseForCatchHandlerEndPC` | End PC offset of the catch handler clause, used for interruptible offset override |
| `GCFrame` | `Next` | Pointer to the next `GCFrame` toward the top of the chain |
| `GCFrame` | `ObjRefs` | Pointer to the array of protected object reference slots |
| `GCFrame` | `NumObjRefs` | Count of protected object reference slots starting at `ObjRefs` |
| `GCFrame` | `GCFlags` | `GC_CALL_*` promotion flags applied when reporting the protected slots |

Global variables used:
| Global Name | Type | Purpose |
| --- | --- | --- |
| For each FrameType `<frameType>`, `<frameType>##Identifier` | `FrameIdentifier` enum value | Identifier used to determine concrete type of Frames |

Constants used:
| Source | Name | Value | Purpose |
| --- | --- | --- | --- |
| `ExceptionFlags` (`exstatecommon.h`) | `Ex_UnwindHasStarted` | `0x00000004` | Bit flag in `ExceptionInfo.ExceptionFlags` indicating exception unwinding (2nd pass) has started. Used by `IsInStackRegionUnwoundBySpecifiedException` to skip ExInfo trackers still in the 1st pass. |
| `InlinedCallFrameMarker` (`exceptionhandling.h`) | `ExceptionHandlingHelper` | `2 (64-bit), 1(32-bit)` | Used to determine whether an active call on an InlinedCallFrame is an EH helper. |
| N/A | `REDIRECTSTUB_ESTABLISHER_OFFSET_RBP` | 0 | AMD64 offset for redirect stubs. |
| N/A | `REDIRECTSTUB_SP_OFFSET_CONTEXT` | 0 | ARM, ARM64, Loongarch & RISCV64 offset for redirect stubs. |
| N/A | `REDIRECTSTUB_EBP_OFFSET_CONTEXT` | -4 | X86 offset for redirect stubs. |

Contracts used:
| Contract Name |
| --- |
| `ExecutionManager` |
| `Thread` |
| `RuntimeTypeSystem` |
| `GCInfo` |
| `Exception` |


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

#### Interpreter Frame Expansion

When the stack walker encounters an `InterpreterFrame`, it expands it into multiple logical frames by walking the `InterpMethodContextFrame` chain. The runtime maintains a linked list of `InterpMethodContextFrame` nodes representing each interpreted method currently on the call stack within a single `InterpreterFrame`.

The `TopInterpMethodContextFrame` field is an approximate hint that may point to a stale frame during dump or native debugging. The actual top frame must be resolved using the `Ip` and `NextPtr`/`ParentPtr` fields, replicating `InterpreterFrame::GetTopInterpMethodContextFrame()`:

- If the hinted frame's `Ip` is non-null (active): seek upward via `NextPtr` while the next frame's `Ip` is also non-null.
- If the hinted frame's `Ip` is null (inactive/reusable): seek downward via `ParentPtr` until finding a frame with non-null `Ip`.

Only frames with non-null `Ip` (active frames) are yielded during the walk. Each node's `ParentPtr` points to its caller.

For each active `InterpMethodContextFrame` in the chain, the stack walker yields a separate frame. The `MethodDesc` for each frame is resolved by following:
`InterpMethodContextFrame.StartIp` -> `InterpByteCodeStart.Method` -> `InterpMethod.MethodDesc`

```
InterpreterFrame
  └-> TopInterpMethodContextFrame (hint, may be stale)
        └-> ResolveTop() -> InterpMethodContextFrame (method C, Ip != null)
                              └-> ParentPtr -> InterpMethodContextFrame (method B, Ip != null)
                                                 └-> ParentPtr -> InterpMethodContextFrame (method A, Ip != null)
                                                                    └-> ParentPtr -> null
```

This produces three frames in order: C, B, A (innermost to outermost).

When the stack walk starts with an explicit context in interpreted code (e.g., from a debugger breakpoint), the interpreted frames are already yielded from the initial context as frameless frames. When the walker subsequently encounters the corresponding `InterpreterFrame`, it skips expanding it to prevent the same frames from being walked twice.


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

* On ARM, the InlinedCallFrame stores the value of the SP after the prolog (`SPAfterProlog`) to allow unwinding for functions with stackalloc. When a function uses stackalloc, the CallSiteSP can already have been adjusted. This value should be placed in R9.

**Return Address**: `CallerReturnAddress`, but only when the frame has an active call (i.e., `CallerReturnAddress != 0`). Returns null otherwise.

#### SoftwareExceptionFrame

SoftwareExceptionFrames store a copy of the context struct. The IP, SP, and all ABI specified (platform specific) callee-saved registers are copied from the stored context to the working context.

**Return Address**: Read from the `ReturnAddress` field on the frame.

#### TransitionFrame

TransitionFrames hold a pointer to a `TransitionBlock`. The TransitionBlock holds a return address along with a `CalleeSavedRegisters` struct which has values for all ABI specified callee-saved registers. The SP can be found using the address of the TransitionBlock. Since the TransitionBlock will be the lowest element on the stack, the SP is the address of the TransitionBlock + sizeof(TransitionBlock).

When updating the context from a TransitionFrame, the IP, SP, and all ABI specified callee-saved registers are copied over.

* On ARM, the additional register values stored in `ArgumentRegisters` are copied over. The `TransitionBlock` holds a pointer to the `ArgumentRegister` struct containing these values.

**Return Address**: Read from `TransitionBlock.ReturnAddress`. This applies to all frame types that use the TransitionFrame mechanism.

The following Frame types also use this mechanism:
* FramedMethodFrame
* PInvokeCallIFrame
* PrestubMethodFrame
* StubDispatchFrame
* CallCountingHelperFrame
* ExternalMethodFrame
* DynamicHelperFrame

#### FuncEvalFrame

FuncEvalFrames hold a pointer to a `DebuggerEval`. The DebuggerEval holds a full context which is completely copied over to the working context when updating.

**Return Address**: Returns null when using hijack evaluation (`EvalUsesHijack`). Otherwise, read from `TransitionBlock.ReturnAddress` like other TransitionFrame types.

#### ResumableFrame

ResumableFrames hold a pointer to a context object (Note this is different from SoftwareExceptionFrames which hold the context directly). The entire context object is copied over to the working context when updating.

RedirectedThreadFrames also use this mechanism.

**Return Address**: Extracted from the saved context's instruction pointer (`TargetContextPtr` -> context IP).

#### FaultingExceptionFrame

FaultingExceptionFrames have two different implementations. One for Windows x86 and another for all other builds (with funclets).

Given the cDAC does not yet support Windows x86, this version is not supported.

The other version stores a context struct. To update the working context, the entire stored context is copied over. In addition the `ContextFlags` are updated to ensure the `CONTEXT_XSTATE` bit is not set given the debug version of the contexts can not store extended state. This bit is architecture specific.

**Return Address**: Extracted from the saved context's instruction pointer (`TargetContext` -> context IP).

#### HijackFrame

HijackFrames carry a IP (ReturnAddress) and a pointer to `HijackArgs`. All platforms update the IP and use the platform specific HijackArgs to update further registers. The following details currently implemented platforms.

**Return Address**: Read from the `ReturnAddress` field directly.

* x64 - On x64, HijackArgs contains a CalleeSavedRegister struct. The saved registers values contained in the struct are copied over to the working context.
    * Windows - On Windows, HijackArgs also contains the SP value directly which is copied over to the working context.
    * Non-Windows - On OS's other than Windows, HijackArgs does not contain an SP value. Instead since the HijackArgs struct lives on the stack, the SP is `&hijackArgs + sizeof(HijackArgs)`. This value is also copied over.
* x86 - On x86, HijackArgs contains a list of register values instead of the CalleeSavedRegister struct. These values are copied over to the working context. The SP copied over to the working context and found using `SP = &hijackArgs + sizeof(HijackArgs)`.
* arm64 - Unlike on x64, on arm64 HijackArgs contains a list of register values instead of the CalleeSavedRegister struct. These values are copied over to the working context. The SP is fetched using the same technique as on x64 non-Windows where `SP = &hijackArgs + sizeof(HijackArgs) + (hijackArgsSize % 16)` and is copied over to the working context. Note: `HijackArgs` may be padded to maintain 16 byte stack alignment.
* arm - Similar to arm64, HijackArgs contains a list of register values. These values are copied over to the working context. The SP is fetched using the same technique as arm64 where `SP = &hijackArgs + sizeof(HijackArgs) + (hijackArgsSize % 8)` and is copied over to the working context. Note: `HijackArgs` may be padded to maintain 8 byte stack alignment.

#### TailCallFrame

TailCallFrames only appear on x86 Windows. They hold a `CalleeSavedRegisters` struct as well as a `ReturnAddress`. While the stack pointer is not directly contained in the TailCallFrame structure, it will be on the stack immediately following the Frame (found at the address of the Frame + size of the Frame). To process these Frames, update all of the registers in `CalleeSavedRegisters`, the instruction pointer from the stored return address, and the stack pointer from the address saved on the stack.

**Return Address**: Read from the `ReturnAddress` field directly.

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
byte[] GetRawContext(
    IStackDataFrameHandle stackDataFrameHandle,
    StackwalkFlag flags = StackwalkFlag.Default);
```

`GetFrameAddress` gets the address of the current capital "F" Frame. This is only valid if the `IStackDataFrameHandle` is at a point where the context is based on a capital "F" Frame. For example, it is not valid when when the current context was created by using the stack frame unwinder.
If the Frame is not valid, returns `TargetPointer.Null`.

```csharp
TargetPointer GetFrameAddress(IStackDataFrameHandle stackDataFrameHandle);
```


`GetFrameName` gets the name associated with a FrameIdentifier (pointer sized value) from the Globals stored in the contract descriptor. If no associated Frame name is found, it returns an empty string.
```csharp
string GetFrameName(TargetPointer frameIdentifier);
```

`GetMethodDescPtr(TargetPointer framePtr)` returns the method desc pointer associated with a Frame. If not applicable, it returns TargetPointer.Null.
* For FramedMethodFrame and most of its subclasses the methoddesc is accessible as a pointer field on the object (MethodDescPtr). The two exceptions are PInvokeCalliFrame (no valid method desc) and StubDispatchFrame.
  * StubDispatchFrame's MD may be either found on MethodDescPtr, or if this field is null, we look it up using a method table (RepresentativeMTPtr) and MT slot (RepresentativeSlot).
* InlinedCallFrame also has a field from which we draw the method desc; however, we must first do some validation that the data in this field is valid.
* MD is not applicable for other types of frames.
```csharp
TargetPointer GetMethodDescPtr(TargetPointer framePtr)
```

`GetMethodDescPtr(IStackDataFrameHandle stackDataFrameHandle)` returns the method desc pointer associated with a `IStackDataFrameHandle`. Note there are two major differences between this API and the one above that operates on a TargetPointer.
* This API can either be at a capital 'F' frame or a managed frame unlike the TargetPointer overload which only works at capital 'F' frames.
* This API handles the special ReportInteropMD case which happens under the following conditions
    1. The dataFrame is at an `InlinedCallFrame`
    2. The dataFrame is in a `SkippedFrame` state
    3. The InlinedCallFrame's return address is managed code
    4. The InlinedCallFrame's return address method has a MDContext arg

  In this case, we report the actual interop MethodDesc. A pointer to the MethodDesc immediately follows the InlinedCallFrame in memory.
This API is implemented as follows:
1. Try to get the current frame address `framePtr` with `GetFrameAddress`.
2. If the address is not null, compute `reportInteropMD` as listed above. Otherwise skip to step 5.
3. If `reportInteropMD`, dereference the pointer immediately following the InlinedCallFrame and return that value.
4. If `!reportInteropMD`, return `GetMethodDescPtr(framePtr)`.
5. Check if the current context IP is a managed context using the ExecutionManager contract. If it is a managed context, use the ExecutionManager context to find the related MethodDesc and return the pointer to it.
```csharp
TargetPointer GetMethodDescPtr(IStackDataFrameHandle stackDataFrameHandle)
```

`GetFrames(TargetPointer threadPointer)` walks the thread's explicit (capital "F") Frame chain and yields a `StackFrameData` describing each Frame.

```csharp
public enum InternalFrameType
{
    None,
    M2U,
    U2M,
    FuncEval,
    InternalCall,
    ClassInit,
    Exception,
    JitCompilation,
}

public record struct StackFrameData(
    TargetPointer FrameAddress,
    TargetPointer FrameIdentifier,
    InternalFrameType InternalFrameType);
```

`GetFrames` yields one entry for every Frame in the chain.

`InternalFrameType` is computed per Frame based on the FrameType.

| Frame subclass | `InternalFrameType` |
| --- | --- |
| `FaultingExceptionFrame`, `SoftwareExceptionFrame` | `Exception` |
| `DebuggerClassInitMarkFrame` | `ClassInit` |
| `PrestubMethodFrame` | `JitCompilation` |
| `FuncEvalFrame` | `FuncEval` |
| `DebuggerU2MCatchHandlerFrame` | `U2M` |
| `DynamicHelperFrame` | `InternalCall` |
| `FramedMethodFrame`, `DebuggerExitFrame`, `PInvokeCalliFrame`, `CallCountingHelperFrame`, `ExternalMethodFrame`, `InterpreterFrame` | `M2U` |
| `InlinedCallFrame` with `FrameHasActiveCall` | `M2U` |
| `InlinedCallFrame` without an active call, `StubDispatchFrame`, all other Frame types | `None` |

```csharp
IEnumerable<StackFrameData> GetFrames(TargetPointer threadPointer)
```

`IsExceptionHandlingHelperInlinedCallFrame(TargetPointer frameAddress)` returns `true` if the Frame at the given address is an `InlinedCallFrame` that was installed by the new exception handling helpers.

A Frame qualifies when all of the following hold:
1. The Frame's identifier identifies it as an `InlinedCallFrame`.
2. `InlinedCallFrame::FrameHasActiveCall` is true (the frame's `CallerReturnAddress` is non-null and, on x86, `CallSiteSP` is non-null).
3. The low bits of the `Datum` field match `InlinedCallFrameMarker::ExceptionHandlingHelper` (`2` on 64-bit, `1` on 32-bit). The marker shares the low bits used by `InlinedCallFrameMarker::Mask`.

```csharp
bool IsExceptionHandlingHelperInlinedCallFrame(TargetPointer frameAddress)
```

`GetDebuggerEvalData(TargetPointer funcEvalFrameAddress)` reads the `DebuggerEval` referenced by a `FuncEvalFrame` and returns the metadata token and Assembly pointer the eval is rooted in. This is the data the debugger needs to populate `cStubFrame` entries for `FuncEval`.

```csharp
public record struct DebuggerEvalData(
    uint MethodToken,
    TargetPointer AssemblyPtr);
```

The implementation is:
1. Read the `FuncEvalFrame` at `funcEvalFrameAddress` and follow `DebuggerEvalPtr` to the underlying `DebuggerEval`.
2. Return `(DebuggerEval.MethodToken, DebuggerEval.AssemblyPtr)`.

```csharp
DebuggerEvalData GetDebuggerEvalData(TargetPointer funcEvalFrameAddress)
```

`GetInstructionPointer` returns the instruction pointer (IP) from the current frame's context. This is the address of the instruction being executed (or about to be executed) in the method associated with this frame.
```csharp
TargetPointer GetInstructionPointer(IStackDataFrameHandle stackDataFrameHandle)
```

`WalkStackReferences` walks the entire managed stack and enumerates all live GC references at each frame. It returns a list of `StackReferenceData` describing each GC-tracked slot (its address, whether it's an interior pointer, and the register/stack location). This API is the primary consumer for `SOSDacImpl.GetStackReferences`.

```csharp
IReadOnlyList<StackReferenceData> WalkStackReferences(ThreadData threadData)
```

The implementation uses the same stack walk algorithm as `CreateStackWalk`, but integrates the GC-aware `Filter` directly (rather than consuming pre-generated frames) and performs GC reference enumeration at each frame. See [GC Stack Reference Scanning](#gc-stack-reference-scanning) for details.

`GetContext` returns a thread context by trying three sources in order: (1) the debugger filter context from `ThreadData.DebuggerFilterContext` (when `ThreadContextSource.Debugger` is requested), (2) the OS thread context via `TryGetThreadContext`, or (3) a context derived from walking the explicit Frame chain (`Thread::Frame` linked list), returning the first frame that yields a usable context:
* If the current Frame is an `InterpreterFrame`, clear the context and update it from the Frame. Return the resulting bytes.
* Otherwise, clear the context and update it from the current Frame; accept the context when both the stack pointer and instruction pointer are non-zero (e.g. `RedirectedThreadFrame`, `InlinedCallFrame`, `DynamicHelperFrame`). Mark `RawContextFlags = FullContextFlags` so callers know SP/PC/FP are valid.

If no Frame in the chain produces a usable context (thread is not running managed code), a zeroed context of the target architecture's size is returned.

`GetRedirectedContextPointer` returns the saved `TargetContext` pointer carried by the head Frame when that Frame is a `RedirectedThreadFrame` (a `ResumableFrame`). Otherwise it returns `TargetPointer.Null`.

#### CreateStackWalk with a caller-provided CONTEXT

`CreateStackWalk(ThreadData, byte[], bool isFirst)` seeds the walker from `contextBuffer` rather than from the thread's saved CONTEXT. `isFirst` (default `true`) is used to determine whether the walker starts with internal state `isFirst` set to true.

1. Compute the caller SP by cloning the seed context and unwinding the clone.
2. Iterate the explicit Frame chain; update context from the first Frame `>= callerSP` (on non-x86) or after the additional ReturnAddress/FP cross-check See [text](https://github.com/dotnet/runtime/blob/ad50b412069ee7f274c585d191df797ac5548525/src/coreclr/vm/stackwalk.cpp#L1238). Do not update if no Frame meets these criteria.
3. For every Frame whose `GetCurrentReturnAddress() == seedIP`, rewrite the seed context via `UpdateContextFromCurrentFrame` and record the matched Frame type.
4. After the loop, if a match was found, override the first walker state `IsFirst` (true for `ResumableFrame`/`RedirectedThreadFrame`, and for `HijackFrame` on non-x86) and `IsInterrupted` (true for `FaultingExceptionFrame`/`SoftwareExceptionFrame`).

The frame iterator is left positioned at the first Frame `>= callerSP`, if such a frame exists.

#### Hijack-stub recovery in `Next()`

The runtime installs a small set of redirect/hijack stubs whose code blocks are tracked in `Debugger::s_hijackFunction`. When the walker is stopped at one of these stubs (state is `InitialNativeContext` or `NativeMarker` and the IP falls inside one of the tracked ranges), the on-thread CONTEXT does not represent the real pre-hijack execution state — the real CONTEXT was stashed on the stack by the stub at entry. `Next()` recovers it before continuing.

The recovery step is driven by `IDebugger.GetHijackKind(controlPC)`, which returns a `HijackKind`:

* `HijackKind.None` — the IP is not inside any tracked stub; `Next()` does nothing special.
* `HijackKind.UnhandledException` — the IP is inside the `ExceptionHijack` stub. The saved `PT_CONTEXT*` is at `*SP` (the stub pushed it directly), so the implementation reads `*context.StackPointer`.
* `HijackKind.Other` — the IP is inside another redirect stub. The saved `PT_CONTEXT*` is at a fixed offset from SP or FP, matching the `REDIRECTSTUB_*` constants.

When a non-`None` `HijackKind` is returned, the walker:

1. Reads the saved CONTEXT pointer from the appropriate stack slot, then materializes a fresh `IPlatformAgnosticContext` from target memory at that address.
2. Reclassifies the walker state from the recovered IP.
3. Re-runs the CONTEXT/Frame-chain reconciliation step (see [CreateStackWalk with a caller-provided CONTEXT](#createstackwalk-with-a-caller-provided-context)) against the freshly created `FrameIterator`, then writes back the updated walker state.

### GC Stack Reference Scanning

`WalkStackReferences` scans the stack for GC references by walking through each frame and reporting live object references and interior pointers, then reporting the thread's GCFrame (GCPROTECT) chain and in-flight exception (ExInfo) chain. This mirrors the GC's own root enumeration, `ScanStackRoots`.

#### Stack Walk Integration

The GC reference walk uses the `Filter` function to drive the stack walk. `Filter` is a port of native `StackFrameIterator::Filter` (with `GC_FUNCLET_REFERENCE_REPORTING` mode) that handles funclet-to-parent frame transitions, exception tracker correlation, and determines whether each frame should report GC references. Unlike `CreateStackWalk` which yields all frames, `Filter` calls `Next()` directly and may skip frames that don't contribute GC roots.

Key state tracked during the walk:

- **IsInterrupted**: Set when transitioning to a managed frame from a `FaultingExceptionFrame` or `SoftwareExceptionFrame` (frames with `FRAME_ATTR_EXCEPTION`). When true, the managed frame's GC enumeration uses `ExecutionAborted` mode, which causes the GcInfoDecoder to skip live slot reporting at non-interruptible offsets.
- **LastProcessedFrameType**: Records the frame type when processing `Frame` state, so `UpdateState` can detect exception frames during the transition to `Frameless`.
- **IsFirst**: Preserved during skipped frame processing (native `SFITER_SKIPPED_FRAME_FUNCTION` does not modify `IsFirst`), ensuring the subsequent managed frame is still treated as the leaf/active frame.
- **GetReturnAddress gating**: In `Frame` state, `UpdateContextFromFrame` is only called when `GetReturnAddress()` returns a non-null value. This matches native behavior where the context is only updated when the frame has a valid return address.

#### Per-Frame GC Enumeration

At each frame yielded by `Filter`, the walk determines whether to scan for GC references:

**Managed (frameless) frames** use `EnumGcRefsForManagedFrame`:

1. Get the code block handle and relative offset from the `ExecutionManager` contract.
2. Decode the GCInfo for the code block via the `GCInfo` contract.
3. Determine `GcSlotEnumerationOptions`: set `IsActiveFrame` if this is the leaf frame (`IsFirst`), `IsExecutionAborted` if the frame was interrupted, `IsParentOfFuncletStackFrame` if funclet GC reporting was delegated to the parent, `SuppressUntrackedSlots` if the code block is a filter funclet.
4. **Catch handler offset override**: When `ShouldParentFrameUseUnwindTargetPCforGCReporting` is set (parent frame resuming from a catch handler), the GC liveness offset is overridden to the first interruptible point within the catch handler clause range. This uses `GetInterruptibleRanges` from the `GCInfo` contract. See [How EH affects GC info/reporting](../coreclr/botr/clr-abi.md#how-eh-affects-gc-inforeporting) for background on why this override is needed.
5. Call `GcInfoDecoder.EnumerateLiveSlots` with the computed offset and flags to report all live register and stack slots. See the [GCInfo contract — EnumerateLiveSlots](./GCInfo.md#enumerateliveslots) for details on the algorithm.

**Capital "F" Frames** use `GcScanRoots`, which dispatches based on frame type:

- **StubDispatchFrame / ExternalMethodFrame**: Resolve GCRefMap via `FindGCRefMap` using the frame's `Indirection` pointer, otherwise fall back to signature-based scanning.
- **DynamicHelperFrame**: Use flag-based scanning (`DynamicHelperFrameFlags`).
- **PrestubMethodFrame / CallCountingHelperFrame**: Use signature-based scanning.
- Other frame types: No GC roots to report.

See [GCRefMap Format and Resolution](#gcrefmap-format-and-resolution) for the GCRefMap scanning path. The signature-based scanning fallback (`PromoteCallerStack`) is currently a stub awaiting an `ICallingConvention` contract port; the stress harness handles the resulting gaps via a deferred-frame sentinel (see [tests/StressTests/known-issues.md](../../../src/native/managed/cdac/tests/StressTests/known-issues.md) and tracking issue [dotnet/runtime#127765](https://github.com/dotnet/runtime/issues/127765)).

#### GCFrame and Exception Tracker Roots

After walking the thread's frames, `WalkStackReferences` reports two additional sets of roots that the GC keeps alive but that are not surfaced by per-frame GC info (matching native `gcenv.ee.cpp` `ScanStackRoots`):

- **GCFrame (GCPROTECT) chain**: starting from `Thread.GCFrame` (obtained via the `Thread` contract's `GetThreadData`), each `GCFrame` is walked via its `Next` pointer until TargetPointer.Null is reached. For each node, the `NumObjRefs` slots starting at `ObjRefs` are reported, applying the node's `GCFlags` (`GC_CALL_INTERIOR` / `GC_CALL_PINNED`) as the promotion flags. This mirrors native `GCFrame::GcScanRoots`.
- **Exception tracker (ExInfo) chain**: starting from the thread's exception tracker, each in-flight exception object (the current one and any superseded/nested ones reached via `PreviousNestedInfo`) is reported through its thrown-object slot.

Both sets carry a non-zero, stack-resident `Source` and `StackPointer` set to the GCFrame / ExInfo node address (the node lives on the stack). A `GCFrame` node belongs to a separate chain from the explicit `Frame` chain, and an ExInfo node is likewise not a capital-F `Frame`, so neither is reported with the `Frame` source type. Both use the `Other` source type, which marks a root reported outside the per-frame walk.

### GCRefMap Format and Resolution

A **GCRefMap** is a compact per-callsite encoding that describes which stack slots in a `TransitionBlock` contain GC references. GCRefMaps are pre-computed by the ReadyToRun compiler and stored in the PE image's import section auxiliary data.

The GCRefMap encoding format — including token values, bit encoding, lookup table structure, and per-architecture position semantics — is documented in the [ReadyToRun format specification](../coreclr/botr/readytorun-format.md#readytorun_import_sectionsauxiliarydata).

#### Resolution Flow

GCRefMap resolution from a frame's `Indirection` pointer proceeds as follows:

1. Call `FindReadyToRunModule(indirection)` (see [ExecutionManager contract](./ExecutionManager.md)) to find the ReadyToRun module containing the import slot.
2. Load the module's `ReadyToRunInfo` to access the import section array.
3. Compute the RVA of the indirection address: `rva = indirection - imageBase`.
4. Search through `READYTORUN_IMPORT_SECTION` entries to find the section containing the RVA.
5. Compute the slot index within the section: `index = (rva - sectionVA) / entrySize`.
6. Use the section's `AuxiliaryData` RVA to locate the GCRefMap lookup table.
7. Use stride-based lookup (stride = 1024) plus linear scan to find the specific GCRefMap entry.

#### Slot Mapping

GCRefMap positions map to `TransitionBlock` offsets using the formula:

```csharp
slotAddress = transitionBlockPtr + FirstGCRefMapSlot + (position * pointerSize)
```

Where `FirstGCRefMapSlot` is the byte offset in the `TransitionBlock` where GCRefMap slot enumeration begins (platform-dependent: on ARM64 it is the return buffer argument register offset; on other platforms it is the argument registers offset).

### x86 Specifics

The x86 platform has some major differences to other platforms. In general this stems from the platform being older and not having a defined unwinding codes. Instead, to unwind managed frames, we rely on GCInfo associated with JITted code. For the unwind, we do not defer to a 'Windows like' native unwinder, instead the custom unwinder implementation was ported to managed code.

#### GCInfo Parsing
The GCInfo structure is encoded using a variety of formats to optimize for speed of decoding and size on disk. For information on decoding and parsing refer to [GC Information Encoding for x86](../coreclr/jit/jit-gc-info-x86.md).

The x86 GCInfo decoder lives under the [GCInfo contract](GCInfo.md) at `src/native/managed/cdac/Microsoft.Diagnostics.DataContractReader.Contracts/Contracts/GCInfo/X86/` (`X86GCInfo` and the supporting `InfoHdr`, `GCArgTable`, `GCTransition`, `CallPattern`, `GCInfoTargetExtensions` types). It is shared between the GCInfo contract (which exposes the offset-independent queries `GetCodeLength` / `GetStackBaseRegister` / `GetCalleePoppedArgumentsSize` for SOS callers) and the x86 stack walker (which constructs `X86GCInfo` directly with a `relativeOffset` to access offset-bound state — `IsInProlog`, `IsInEpilog`, `PrologOffset`, `EpilogOffset`, `PushedArgSize` — that is not exposed through `IGCInfoDecoder`).

#### Unwinding Algorithm

The x86 architecture uses a custom unwinding algorithm defined in `gc_unwind_x86.inl`. The cDAC uses a copy of this algorithm ported to managed code in `X86Unwinder.cs`.

Currently there isn't great documentation on the algorithm, beyond inspecting the implementations.
