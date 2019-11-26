# The current way of handling tail-calls
## Fast tail calls
These are tail calls that are handled directly by the jitter and no runtime cooperation is needed. They are limited to cases where:
* Return value and call target arguments are all either primitive types, reference types, or valuetypes with a single primitive type or reference type fields
* The aligned size of call target arguments is less or equal to aligned size of caller arguments

## Tail calls using a helper
Tail calls in cases where we cannot perform the call in a simple way are implemented using a tail call helper. Here is a rough description of how it works:
* For each tail call target, the jitter asks runtime to generate an assembler argument copying routine. This routine reads vararg list of arguments and places the arguments in their proper slots in the CONTEXT or on the stack. Together with the argument copying routine, the runtime also builds a list of offsets of references and byrefs for return value of reference type or structs returned in a hidden return buffer and for structs passed by ref. The gc layout data block is stored at the end of the argument copying thunk.
* At the time of the tail call, the caller generates a vararg list of all arguments of the tail called function and then calls JIT_TailCall runtime function. It passes it the copying routine address, the target address and the vararg list of the arguments.
* The JIT_TailCall then performs the following:
  * It calls RtlVirtualUnwind twice to get the context of the caller of the caller of the tail call to simulate the effect of running epilog of the caller of the tail call and also its return.
  * It prepares stack space for the callee stack arguments, a helper explicit TailCallFrame and also a CONTEXT structure where the argument registers of the callee, stack pointer and the target function address are set. In case the tail call caller has enough space for the callee arguments and the TailCallFrame in its stack frame, that space is used directly for the callee arguments. Otherwise the stack arguments area is allocated at the top of the stack. This slightly differs in the case the tail call was called from another tail called function - the TailCallFrame already exists and so it is not recreated. The TailCallFrame also keeps a pointer to the list of gc reference offsets of the arguments and structure return buffer members. The stack walker during GC then uses that to ensure proper GC liveness of those references.
  * It calls the copying routine to translate the arguments from the vararg list to the just reserved stack area and the context.
  * If the stack arguments and TailCaillFrame didn't fit into the caller's stack frame, these data are now moved to the final location
  * RtlRestoreContext is used to start executing the callee.

There are several issues with this approach:
* It is expensive to port to new platforms
  * Parsing the vararg list is not possible to do in a portable way on Unix. Unlike on Windows, the list is not stored a linear sequence of the parameter data bytes in memory. va_list on Unix is an opaque data type, some of the parameters can be in registers and some in the memory.
  * Generating the copying asm routine needs to be done for each target architecture / platform differently. And it is also very complex, error prone and impossible to do on platforms where code generation at runtime is not allowed.
* It is slower than it has to be
  * The parameters are copied possibly twice - once from the vararg list to the stack and then one more time if there was not enough space in the caller's stack frame.
  * RtlRestoreContext restores all registers from the CONTEXT structure, not just a subset of them that is really necessary for the functionality, so it results in another unnecessary memory accesses.
* Stack walking over the stack frames of the tail calls requires runtime assistance.

# The new approach to tail calls using helpers
## Objectives
The new way of handling tail calls using helpers was designed with the following objectives:
* It should be cheap to port to new platforms, architectures and code generators
* It needs to work in both jitted and AOT compiled scenarios
* It should support platforms where runtime code generation is not possible
* The tail calls should be reasonably fast compared to regular calls with the same arguments
* The tail calls should not be slower than existing mechanism on Windows
* No runtime assistance should be necessary for unwinding stack with tail call frames on it
* The stack should be unwindable at any spot during the tail calls to properly support sampling profilers and similar tools.
* Stack walk during GC must be able to always correctly report GC references.
* It should work in all cases except those where a tail call is not allowed as described in the ECMA 335 standard section III.2.4

## Requirements
* The code generator needs to be able to compile a tail call to a target as a call to a thunk with the same parameters as the target, but void return, followed by a jump to an assembler helper.

## Implementation
This section describes the helper functions and data structures that the tail calls use and also describes the tail call sequence step by step.
### Helper functions
The tail calls use the following thunks and helpers:
* StoreArguments - this thunk stores the arguments into a thread local storage together with the address of the corresponding CallTarget thunk and a descriptor of locations and types of managed references in the stored arguments data. This thunk is generated as IL and compiled by the jitter or AOT compiler. There is one such thunk per tail call target.
It has a signature that is compatible with the tailcall target function except for the return type which is void. But it is not the same. It gets the same arguments as the tail call target function, but it would also get "this" pointer and the generic context as explicit arguments if the tail call target requires them. Arguments of generic reference types are passed as "object" so that the StoreArguments doesn't have to be generic.
* CallTarget - this thunk gets the arguments buffer that was filled by the StoreArguments thunk, loads the arguments from the buffer, releases the buffer and calls the target function using calli. The signature used for the calli would ensure that all arguments including the optional hidden return buffer and the generic context are passed in the right registers / stack slots. Generic reference arguments will be specified as "object" in the signature so that the CallTarget doesn't have to be generic.
The CallTarget is also generated as IL and compiled by the jitter or AOT compiler. There is one such thunk per tail call target. This thunk has the same return type as the tailcall target or return "object" if the return type of the tail call target is a generic reference type.
* TailCallHelper - this is an assembler helper that is responsible for restoring stack pointer to the location where it was when the first function in a tail call chain was entered and then jumping to the CallTarget thunk. This helper is common for all tail call targets.
In a context of each tailcall invocation, the TailCallHelper will be handled by the jitter as if it had the same return type as the tail call target. That means that if the tail call target needs a hidden return buffer for returning structs, the pointer to this buffer will be passed to the TailCallHelper the same way as it would be passed to the tail call target. The TailCallHelper would then pass this hidden argument to the CallTarget helper.
There will be two flavors of this helper, based on whether the tail call target needs a hidden return buffer or not:
  * TailCallHelper
  * TailCallHelper_RetBuf

### Helper data structures
The tail calls use the following data structures:
* Thread local storage for arguments. It stores the arguments of a tail call for a short period of time between the StoreArguments and CallTarget calls.
* Arguments GC descriptor - descriptor of locations and types of managed references in the arguments.
* TailCallHelperStack - a per thread stack of helper entries that is used to determine whether a tail call is chained or not. Its entries are allocated as local variables in CallTarget thunks. Each entry contains:
  * Stack pointer captured right before a call to a tail call target
  * ChainCall flag indicating whether the CallTarget thunk should return after the call to the tail call target or whether it should execute its epilog and jump to TailCallHelper instead. The latter is used by the TailCallHelper to remove the stack frame of the CallTarget before making a tail call from a tail called function.
  * Pointer to the next entry on the stack.

### Tail call sequence
* The caller calls the StoreArguments thunk corresponding to the callee to store the pointer to the tail call target function, its arguments, their GC descriptors, optional "this" and generic context arguments and the corresponding CallTarget thunk address in a thread local storage.
* The caller executes its epilog, restoring stack pointer and callee saved registers to their values when the caller was entered.
* The caller jumps to the TailCallHelper. This function performs the following operations:
  * Get the topmost TailCallHelperStack entry for the current thread.
  * Check if the previous stack frame is a CallTarget thunk frame by comparing the stack pointer value stored in the TailCallHelperStack entry to the current CFA (call frame address). If it matches, it means that the previous stack frame belongs to a CallTarget thunk and so the tail call caller was also tail called.
  * If the previous frame was a CallTarget thunk, its stack frame needs to be removed to ensure that the stack will not grow when tail calls are chained. Set ChainCall flag in the TailCallHelperStack entry and return. That returns control to the CallTarget thunk. It checks the ChainCall flag and since it set, it executes its epilog and jumps to the TailCallHelper again.
  * If the previous frame was not a CallTarget thunk, get the address of the CallTarget thunk of the tailcall target from the arguments buffer and jump to it.
* The CallTarget thunk function then does the following operation:
  * Create local instance of TailCallHelperStack entry and store the current stack pointer value in it.
  * Push the entry to the TailCallHelperStack of the current thread.
  * Get the arguments buffer from the thread local storage, extract the regular arguments and the optional "this" and generic context arguments and the target function pointer. Release the buffer and call the target function. The frame of the CallTarget thunk ensures that the arguments of the target are GC protected until the target function returns or tail calls to another function.
  * Pop the TailCallHelperStack entry from the TailCallHelperStack of the current thread.
  * Check the ChainCall flag in the TailCallHelperStack entry. If it is set, run epilog and jump to the TailCallHelper.
  * If the ChainCall flag is clear, it means that the last function in the tail call chain has returned. So return the return value of the target function.

## Work that needs to be done to implement the new tail calls mechanism
### JIT (compiler in the AOT scenario)
* Modify compilation of tail calls with helper so that a tail call is compiled as a call to the StoreArguments thunk followed by the jump to the assembler TailCallHelper. In other words, the
```
tail. call/callvirt <method>
ret
```
becomes
```
call/callvirt <StoreArguments thunk>
tail. call <TailCallHelper>
ret
```
### Runtime (compiler in the AOT scenario)
* Add generation of the StoreArguments and CallTarget IL thunks to the runtime (compiler tool chain in at AOT scenario). As a possible optimization, In the AOT scenario, the thunks can be generated by the compiler as native code directly without the intermediate IL.
* For the JIT scenario, add a new method to the JIT to EE interface to get the StoreArguments thunk method handle for a given target method and the TailCallHelper address.

### Runtime in both scenarios
* Add support for the arguments buffer, which means:
  * Add functions to create, release and get the buffer for a thread
  * Add support for GC scanning the arguments buffers.
* Implement the TailCallHelper asm helper for all architectures
### Debugging in both scenarios
Ensure that the stepping in a debugger works correctly. In CoreCLR, the TailCallStubManager needs to be updated accordingly.

## Example code

```C#
struct S
{
    public S(long p1, long p2, long p3)
    {
        s1 = p1; s2 = p2; s3 = p3;
    }

    public long s1, s2, s3;
}

struct T
{
    public T(S s)
    {
        t1 = s.s1; t2 = s.s2; t3 = s.s3 t4 = 4;
    }
    public long t1, t2, t3, t4;
}

struct U
{
    public U(T t)
    {
        u1 = t.t1; u2 = t.t2; u3 = t.t3 u4 = t.t4; u5 = 5;
    }
    public long u1, u2, u3, u4, u5;
}

int D(U u)
{
    int local;
    Console.WriteLine("In C, U = [{0}, {1}, {2}, {3}, {4}", u.u1, u.u2, u.u3, u.u4, u.u5);
    return 1;
}

int C(T t)
{
    int local;
    Console.WriteLine("In C");
    U args = new U(t);
    return tailcall D(args);
}

int B(S s)
{
    int local;
    Console.WriteLine("In B");
    T args = new T(S);
    return tailcall C(args);
}

int A()
{
    S args = new S(1, 2, 3);
    int result = B(args);
    Console.WriteLine("Done, result = {0}\n", result);
}
```

## Example code execution
This section shows how stack evolves during the execution of the example code above. Execution starts at function A, but the details below start at the interesting point where the first tail call is about to be called.
### B is about to tail call C
```
* Return address of A
* Callee saved registers and locals of A
* Stack arguments of B
* Return address of B
* Callee saved registers and locals of B
```
### Arguments of C are stored in the thread local buffer, now we are in the TailCallHelper
The callee saved registers and locals of B are not on the stack anymore
```
* Return address of A
* Callee saved registers and locals of A
* Stack arguments of B
* Return address of B
```

### In CallTarget thunk for C, about to call C
The thunk will now extract parameters for C from the thread local storage and call C.
```
* Return address of A
* Callee saved registers and locals of A
* Stack arguments of B
* Return address of B
* Callee saved registers and locals of CallTarget thunk for C
```
### C is about to tail call D
```
* Return address of A
* Callee saved registers and locals of A
* Stack arguments of B
* Return address of B
* Callee saved registers and locals of CallTarget thunk for C
* Stack arguments of C
* Return address of C
* Callee saved registers and locals of C
```
### Arguments of D are stored in the thread local buffer, now we are in the TailCallHelper
The callee saved registers and locals of C are not on the stack anymore.
But we still have the return address of C, stack arguments of C and callee saved registers and locals of CallTarget thunk for C on the stack.
We need to remove them as well to prevent stack growing.
The TailCallHelper detects that the previous stack frame was the frame of the CallTarget thunk for C and so it sets the ChainCall flag in the topmost TailCallHelperStackEntry and returns to CallTarget thunk for C in order to let it cleanup its stack frame.
```
* Return address of A
* Callee saved registers and locals of A
* Stack arguments of B
* Return address of B
* Callee saved registers and locals of CallTarget thunk for C
* Stack arguments of C
* Return address of C
```
### Returned to CallTarget thunk for C with ChainCall flag in the TailCallHelperStackEntry **set**
The thunk checks the ChainCall flag and since it is set, it runs its epilog and then jumps to the TailCallHelper.
```
* Return address of A
* Callee saved registers and locals of A
* Stack arguments of B
* Return address of B
* Callee saved registers and locals of CallTarget thunk for C
```
### Back in TailCallHelper
Now the stack is back in the state where we have made the previous tail call. Since the previous stack frame was not a CallTarget thunk frame, we just jump to the CallTarget thunk for D.
```
* Return address of A
* Callee saved registers and locals of A
* Stack arguments of B
* Return address of B
```
### In CallTarget thunk for D, about to call D
The thunk will now extract parameters for D from the thread local storage and call D.
```
* Return address of A
* Callee saved registers and locals of A
* Stack arguments of B
* Return address of B
* Callee saved registers and locals of CallTarget thunk for D
```
### In D
We are in the last function of the chain, so after it does its work, it returns to its CallTarget thunk.
```
* Return address of A
* Callee saved registers and locals of A
* Stack arguments of B
* Return address of B
* Callee saved registers and locals of CallTarget thunk for D
* Stack arguments of D
* Return address of D
* Callee saved registers and locals of D
```
### Returned to CallTarget thunk for D with ChainCall flag in the TailCallHelperStackEntry **clear**
The thunk checks the ChainCall flag and since it is clear, it recognizes we are now returning from the call chain and so it returns the result of the D.
```
* Return address of A
* Callee saved registers and locals of A
* Stack arguments of B
* Return address of B
* Callee saved registers and locals of CallTarget thunk for D
```
### Returned to A
We are back in A and we have the return value of the call chain.
```
* Return address of A
* Callee saved registers and locals of A
```

## Example of thunks generated for a simple generic method

```C#
struct Point
{
    public int x;
    public int y;
    public int z;
}

class Foo
{
    public Point Test<T>(int x, T t) where T : class
    {
        Console.WriteLine("T: {0}", typeof(t));
        return new Point(x, x, x);
    }
}
```

For tail calling the Test function, the IL helpers could look e.g. as follows:

```IL
.method public static void StoreArgumentsTest(native int target, object thisObj, native int genericContext, int x, object t) cil managed
{
    .maxstack 4
    .locals init (
       [0] native int buffer,
    )

    call native int AllocateArgumentBuffer(56)
    stloc.0

    ldloc.0
    ldc.i8 0x12345678 // pointer to the GC descriptor of the arguments buffer
    stind.i
    ldloc.0
    sizeof native int
    add
    stloc.0

    ldloc.0
    ldftn Point CallTargetTest()
    stind.i
    ldloc.0
    sizeof native int
    add
    stloc.0

    ldloc.0
    ldarg.1 // "thisObj"
    stobj object
    ldloc.0
    sizeof object
    add
    stloc.0

    ldloc.0
    ldarg.2 // "genericContext"
    stind.i
    ldloc.0
    sizeof native int
    add
    stloc.0

    ldloc.0
    ldarg.3 // "x"
    stind.i4
    ldloc.0
    sizeof native int
    add
    stloc.0

    ldloc.0
    ldarg.4 // "t"
    stobj object
    ldloc.0
    sizeof object
    add
    stloc.0

    ldloc.0
    ldarg.0 // "target"
    stind.i

    ret
}

```

```IL
.method public static Point CallTargetTest() cil managed
{
    .maxstack 4
    .locals init (
       [0] native int buffer,
       [1] valuetype TailCallHelperStackEntry entry,
       [2] Point result
    )

    // Initialize the TailCallHelperStackEntry
    // chainCall = false
    // sp = current sp

    ldloca.s 1
    ldc.i4.0
    stfld bool TailCallHelperStackEntry::chainCall
    ldloca.s 1
    call native int GetCurrentSp()
    stfld native int TailCallHelperStackEntry::sp

    ldloca.s 1 // TailCallHelperStackEntry
    call void PushHelperStackEntry(native int)

    // Prepare arguments for the tail call target

    call native int FetchArgumentBuffer()
    sizeof native int
    add // skip the pointer to the GC descriptor of the arguments buffer
    sizeof native int
    add // skip the address of the CallTargetTest in the buffer, it is used by the TailCallHelper only
    stloc.0

    ldloc.0
    ldobj object // this
    ldloc.0
    sizeof object
    add
    stloc.0

    ldloc.0
    ldind.i // generic context
    ldloc.0
    sizeof native int
    add
    stloc.0

    ldloc.0
    ldind.i4 // int x
    ldloc.0
    sizeof native int
    add
    stloc.0

    ldloc.0
    ldobj object // T t
    ldloc.0
    sizeof object
    add

    ldobj native int // tailcall target

    // The arguments buffer is not needed anymore
    call void ReleaseArgumentBuffer()

    .try
    {
        calli Point (object, native int, int32, object) // this, generic context, x, t
        stloc.2
        leave.s Done
    }
    finally
    {
        ldloca.s 1 // TailCallHelperStackEntry
        call void PopHelperStackEntry(native int)
        endfinally
    }

Done:
    ldloc.1 // TailCallHelperStackEntry
    ldfld bool TailCallHelperStackEntry::chainCall
    brfalse.s NotChained

    // Jump to the TailCallHelper that will call to the next tail call in the chain.
    // The stack frame of the current CallTargetTest is reclaimed and epilog executed
    // before the TailCallHelper is entered.
    tail.call int32 TailCallHelper()
    ret

NotChained:
    // Now we are returning from a chain of tail calls to the caller of this chain
    ldloc.2
    ret
}
```