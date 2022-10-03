# Tailcalls via helpers
## Introduction
Tailcall optimization is a transformation typically performed by compilers in
which a call appearing at the very end of a function allows the stack frame of
the caller to be cleaned up before the call is performed. This allows the
execution to use less stack space. For that reason it is an important
optimization for functional languages where recursion is typically the only way
of looping over data structures. Thus, these languages require this
transformation to be performed for correctness to guarantee that stack space
usage does not grow linear in the size of the data structures.

Since not all languages need this to be done .NET allows languages to request
this optimization to be done on a per-call basis. This is communicated to the
runtime by adding the `tail.` prefix before the call in the CIL bytecode and
marks that the call needs to be performed as a tailcall for correctness. This
prefix is legal only on calls in tail position and also requires certain other
conditions to be met (as described in ECMA-335).

When .NET sees the `tail.` prefix it has two mechanisms to perform the requested
tailcall: fast tailcalls and helper-based tailcalls. In fast tailcalls the JIT
is able to turn the call into cleanup of the stackframe followed by a jump or
branch instruction in the underlying assembly. However, not all platforms
support this and even on supported platforms it is not generally possible to do
for all callsites. In scenarios where the JIT is not able to do this it falls
back to another helper-based mechanism which allows dispatch of tailcalls more
generally. This mechanism is described in this document.

## An overview of the mechanism
The helper-based mechanism is designed to be both portable and flexible so that
it can support many forms of tailcalls and so that the mechanism itself can be
supported widely and easily ported to new platforms. To accomplish these goals
the mechanism makes use of IL stubs and as little as possible use of details of
the underlying architecture. These goals are tricky to accomplish for the
general definition of a tailcall which requires unwindining the stack before
performing the call. Instead, the mechanism solves a slightly different problem:
for any arbitrary sequence of tailcalls use constant stack space.

The idea is the following: at the first tailcall of a sequence let the stack
frame survive but set up a "dispatcher" that will know how to perform future
tailcalls. Otherwise detect that this is not the first tailcall and return to
let the previous dispatcher deal with the tailcall. The hard problems to solve
are then
1. Detecting whether returning will give control to a previous dispatcher.
1. Recording the required information about the tailcall: its arguments and
   target.
1. Performing the call once the dispatcher has control.

It turns out that the first problem is the only problem that requires
architectural details (and thus needs work when ported to new platforms) while
the other two problems can be solved once and for all in the runtime. This is
accomplished by having the runtime generate two IL stubs to do just those
things. When the JIT sees that a tailcall cannot be done using the fast tailcall
mechanism it asks the runtime to create these two IL stubs: one IL stub to
record the arguments and target for the call, and another to extract that
information and perform the call.

## Storing the args
The first stub will be used to store the arguments. In the scenario where there
is a previous dispatcher control flow will initially return here before the
tail. prefixed call is (semantically) performed. Thus, it will not be legal to
store the arguments on the stack. Instead the arguments are stored in
thread-local storage for this small transitionary period of time. For the first
tailcall it would be legal to store the arguments on the stack but for
simplicity and uniformity this is not done and TLS is always used. Concretely,
for an example like
```csharp
bool IsEven(int x)
{
    if (x == 0)
        return true;
    return /* tail. prefixed */ IsOdd(x - 1);
}
```

An IL stub will be generated which performs the following:
```csharp
void IL_STUB_StoreTailCallArgs(int x)
{
    IntPtr argBuffer = RuntimeHelpers.AllocTailCallArgBuffer(4, null);
    *(int*)argBuffer = x;
}
```

Here a runtime helper is used to allocate an area of space in TLS. Note that
since arguments can be any arbitrary value, including by-refs and object refs,
the GC needs special knowledge of the argument buffer. This is the purpose of
the second argument to the runtime helper: it is a so-called GC descriptor which
describes which slots contain what kind of GC pointers in the argument buffer.
When the runtime generates this IL stub it will also generate the matching GC
descriptor and pass it as the second argument to the runtime helper. For this
simple example, the GC descriptor is trivial and null is passed.

This case is relatively simple. In other cases the runtime might also require
the generated stub to be passed a function pointer to the target address. This
is for example the case when the tailcall is a `calli` instruction or for
certain tailcalls to generic methods.

## Calling the target
The second IL stub extracts the arguments and calls the target function. For the
above case a function like the following will be generated:
```csharp
void IL_STUB_CallTailCallTarget(IntPtr argBuffer, ref byte result, PortableTailCallFrame* pFrame)
{
    pFrame->NextCall = null;
    pFrame->TailCallAwareReturnAddress = StubHelpers.NextCallReturnAddress();
    int arg1 = *(int*)(argBuffer + 4);
    *argBuffer = TAILCALLARGBUFFER_ABANDONED;
    Unsafe.As<byte, bool>(ref result) = IsOdd(arg1);
}
```
It matches the function above by loading the argument that was written, and
then writing a sentinel value that communicates to GC that the arg buffer does
not need to be scanned anymore, to avoid extending the lifetime of (by-)refs
unnecessarily. In addition, it also includes a call to
`StubHelpers.NextCallReturnAddress`. This is a JIT intrinsic that represents
the address of where the next call will return to. This is part of how the
mechanism detects that there is a previous dispatcher that should be used,
which will be described in the next section.

As described above there are cases when the runtime needs to be passed the
target function pointer. In those cases this stub will instead load the function
pointer out of the arg buffer and dispatch to it using a `calli` instruction.

## The dispatcher
The dispatcher is the last function of the mechanism. Unlike the IL stubs
described in the previous two sections it is general and not specific to any one
callsite. Thus only one dispatcher function needs to exist. However, note that
it is possible for the dispatcher to be simultaneously live multiple times on
the stack. This happens for example when a tailcalled function does a regular
call to a function that then does tailcalls. In that case the dispatcher needs
to be set up again since returning would not return directly back to the
previous dispatcher.

The mechanism uses some data structures to describe the dispatchers that are
currently live on the stack and to facilitate detection of previous
dispatchers. The dispatchers themselves are described by a series of
`PortableTailCallFrame` entries. These entries are stored on the stack in each
live instance of the dispatcher. This structure looks like the following:

```csharp
struct PortableTailCallFrame
{
    public IntPtr TailCallAwareReturnAddress;
    public delegate*<IntPtr, ref byte, PortableTailCallFrame*, void> NextCall;
}
```

Here the `TailCallAwareReturnAddress` is an address that can be used to detect
whether a return would go to that particular dispatcher. `NextCall` is what the
dispatcher uses to perform the next tailcall of a sequence.

The current frame is stored in TLS along with information about the currently
allocated argument buffer that can be used by GC:

```csharp
struct TailCallTls
{
    public PortableTailCallFrame* Frame;
    public IntPtr ArgBuffer;
    public IntPtr ArgBufferGCDesc;
    public int ArgBufferSize;
}
```

Finally, the dispatcher follows:
```csharp
private static unsafe void DispatchTailCalls(
    IntPtr callersRetAddrSlot,
    delegate*<IntPtr, ref byte, PortableTailCallFrame*, void> callTarget,
    ref byte retVal)
{
    IntPtr callersRetAddr;
    TailCallTls* tls = GetTailCallInfo(callersRetAddrSlot, &callersRetAddr);
    PortableTailCallFrame* prevFrame = tls->Frame;
    if (callersRetAddr == prevFrame->TailCallAwareReturnAddress)
    {
        prevFrame->NextCall = callTarget;
        return;
    }

    PortableTailCallFrame newFrame;
    // GC uses NextCall to keep LoaderAllocator alive after we link it below,
    // so we must null it out before that.
    newFrame.NextCall = null;

    try
    {
        tls->Frame = &newFrame;

        do
        {
            callTarget(tls->ArgBuffer, ref retVal, &newFrame);
            callTarget = newFrame.NextCall;
        } while (callTarget != null);
    }
    finally
    {
        tls->Frame = prevFrame;

        // If the arg buffer is reporting inst argument, it is safe to abandon it now
        if (tls->ArgBuffer != IntPtr.Zero && *(int*)tls->ArgBuffer == 1 /* TAILCALLARGBUFFER_INSTARG_ONLY */)
        {
            *(int*)tls->ArgBuffer = 2 /* TAILCALLARGBUFFER_ABANDONED */;
        }
    }
}
```

It is first responsible for detecting whether we can return and let a previous
dispatcher perform the tailcall. To do this it needs to obtain the caller's
return address (i.e. an address in the caller's caller). Furthermode, it needs
to obtain information about the current, existing dispatcher frame. Due to
return address hijacking in the VM it is not enough to simply read the return
address directly from the stack -- instead, assistance from the VM is required
in the form of a helper. This helper both returns the TLS information and the
correct return address.

In the case a return would go back to a dispatcher we simply record the next
call by saving the `callTarget` parameter, a function pointer to a
`CallTailCallTarget` stub. Otherwise the new dispatcher is recorded and a loop
is entered that starts dispatching tailcalls.

This loop calls into the `CallTailCallTarget` stubs so it is from these stubs
that we need to store the return address for comparisons in the future. These
stubs will call into the specified user function from the original tailcall
site, and it is if this function does a tailcall that we will need to detect
whether we can use a previous dispatcher. This will be the case when we return
directly to a `CallTailCallTarget` stub which will then return to the
dispatcher.

Note that we take care to zero out PortableTailCallFrame.NextCall from the
CallTailCallTarget stub instead of doing it in the dispatcher before calling
the stub. This is because GC will use NextCall to keep collectible assemblies
alive in the event that there is a GC inside the dispatcher. Once control has
been transferred to CallTailCallTarget we can safely reset the field.

## The JIT's transformation
Based on these functions the JIT needs to do a relatively simple transformation
when it sees a tailcall that it cannot dispatch as a fast tailcall. This
transformation is done in the JIT's internal IR, but for familiarity it is
described here in pseudo C#. For a tailcall site like

```csharp
return /*tail prefixed*/ IsOdd(x - 1);
```
the JIT requests the runtime to create the two IL stubs described above and
transforms the code into the equivalent of

```csharp
IL_STUB_StoreTailCallArgs(x - 1);
bool result;
DispatchTailCalls(_AddressOfReturnAddress(), &IL_STUB_CallTailCallTarget, ref result);
return result;
```

Here `_AddressOfReturnAddress()` represents the stack slot containing the return
address. Note that .NET requires that the return address is always stored on the
stack, even on ARM architectures, due to its return address hijacking mechanism.

When the result is returned by value the JIT will introduce a local and pass a
pointer to it in the second argument. For ret bufs the JIT will typically
directly pass along its own return buffer parameter to DispatchTailCalls. It is
possible that this return buffer is pointing into GC heap, so the result is
always tracked as a byref in the mechanism.

In certain cases the target function pointer is also stored. For some targets
this might require the JIT to perform the equivalent of `ldvirtftn` or `ldftn`
to obtain a properly callable function pointer.

## Debugging
The control flow when helper-based tailcalls are performed is non-standard.
Specifically, there are two possible paths of execution when a tailcall is
performed, depending on whether returning goes to an existing dispatcher. Due to
this the debugger requires special support to give users a good experience when
stepping in code involving heper-based tailcalls.

The debugger by default ignores IL stubs and implement stepping-in in a way that
makes the above work in both scenarios. It turns out that the only problematic
case is when stepping over tailcalls that are done by returning first. The
debugger uses the same TLS structure as the rest of the mechanism to detect this
case and simulates a step-over by performing a step-out to the nearest user
function instead.

## Conclusion
.NET implements tail prefixed calls based on two separate mechanisms: a fast
tailcall mechanism, where the JIT just directly jumps to the target function,
and a helper-based tailcall mechanism, where the JIT asks the runtime to
generate IL stubs that are used by a dispatcher to perform the tailcalls. Since
the mechanism is primarily based on these IL stubs the mechanism is portable and
the only architecture specific part is detecting whether there is a previous
dispatcher. However, this part was not hard to implement for ARM and XArch
architectures and it is expected that it will not be significantly harder for
other platforms in the future.
