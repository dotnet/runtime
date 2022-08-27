# Inline Size Estimates

Note this is work in progress....

Inlining is a heuristic-based optimization. There are some cases where
it's obvious that a particular inline is good or bad for performance,
but more typically, things are not that clear. In those cases where
the benfit is in doubt, the compiler will rely on heurstics, typically
based on various estimates of an inline's size impact, speed impact,
or other important factors.

In this writeup we consider approaches to what should be the simplest
of these estimates: the size impact of an inline.

## Background

There are a number of interesting facets to the inline size estimate
problem, but let's start with some generalities. Suppose we have some
size estimate for the caller `C` (ignoring for the moment exactly what
we are measuring), say `CallerSize`, and some size estimate for the
callee `E`, say `CalleeSize`. Now suppose we are contemplating what
would happen if `E` is inlined into `C` to create `C'`. We'd like some
sort of size estimate `CallerSize'`.  The simplest estimate is that
`CallerSize'` = `CallerSize + CalleeSize`.

```
(1)  `CallerSize'` = `CallerSize + CalleeSize`
```

However, calling conventions impose some additional code overhead on
both the caller and callee. The caller must set up arguments in
registers or on the stack, and if there is a return value, might need
to move it or store it somewhere. It also might need to spill values
that are stored in caller-saved registers around the call.

The callee must set up a stack frame and possibly spill callee-save
registers it intends to use, and then ultimately must undo all this.

When we inline `E` into `C`, none of this calling convention setup is
necessary. So one can imagine that there is some additional size
savings from inlining, one that depends on the calling convention and
the number and kind of arguments passed from caller to callee.

```
(2)  CallerSize' = CallerSize + CalleeSize - Overhead
```

Note that it's entirely possible that `Overhead > CalleeSize`, so
that `CallerSize' < CallerSize`, that is the inline not only results
in faster code but also in smaller code. Indeed this state of affairs
is increasingly common with that advent of modern programming styles
that emphasize building functionality out of lots of small procedures.

Alternatively, we can compute the size impact as the size change of C
because of the inline of E:

```
(2)  SizeImpact = CallerSize - CallerSize'
```

or with a bit of math,

```
(2)  SizeImpact = Overhead - CalleeSize
```

Now let's look at some actual data to see how well this simple model
for `SizeImpact` holds up. The data below shows the measured size
impact of a single inline into various calling methods in mscorlib.
This data was obtained by first compiling all the methods without
inlining to obtain a baseline size as measured by the encoded
instruction size in bytes.  The inliner was then modified so it would
only do a single inline per caller, with the candidate chosen by a
parameter `k` that could be varied externally. mscorlib was then
repeatedly compiled with varying values of `k`, and the inlines
performed and the subsequent modified caller sizes recorded. This data
was then permuted and histogrammed to show the variety of `SizeImpact`
values for a fixed `E`.  Since the calling convention and number and
kind of parameters is fixed across all observations for a fixed `E`,
the better model would predict `SizeImpact` is the same in every
case.

In this first example, case `E` is `System.Object:.ctor()`, which is
close to the simplest possible callee -- it takes one argument and has
no return value. In all the observed cases the `SizeImpact` is
negative, but it's far from the same in every case.


```
Inlining for System.Object:.ctor():this (size 6)
Instances 1160 Mean SizeImpact -12.53 Min -52 Max -5
Distribution
  less than -50:   1
      [-50,-41]:   0    1    1    0    0    1    0    0    1    1
      [-40,-31]:   0    1    0    0    1    0    0    3    1    2
      [-30,-21]:   1    3    2    2    1    8   76    5   22   18
      [-20,-11]:  38   23   98    7   29    4    8   27   14   21
      [-10, -1]:   1  362  373    0    0    3    0    0    0    0
      [  0,  9]:   0    0    0    0    0    0    0    0    0    0
      [ 10, 19]:   0    0    0    0    0    0    0    0    0    0
      [ 20, 29]:   0    0    0    0    0    0    0    0    0    0
      [ 30, 39]:   0    0    0    0    0    0    0    0    0    0
      [ 40, 49]:   0    0    0    0    0    0    0    0    0    0
greater than 49:   0
```

It should be evident from this set of observations that `SizeImpact`
cannot be completely characterized by a simple formula like `(2)`. For
this inlinee, inlining always saves at least 5 bytes, roughly half the time
it saves 8 or 9 bytes, on average it saves about 12.5 bytes, but it often
saves considerably more.

Other inlinees show similar spreads in `SizeImpact`. Here we see a case
where sometimes the `SizeImpact` is negative and other times it's
positive.

```
Inlining for System.Threading.CancellationToken:get_IsCancellationRequested():bool:this (size 29)
Instances 42 SizeImpact Mean 11.33 Min -20 Max 28
Distribution
  less than -50:   0
      [-50,-41]:   0    0    0    0    0    0    0    0    0    0
      [-40,-31]:   0    0    0    0    0    0    0    0    0    0
      [-30,-21]:   0    0    0    0    0    0    0    0    0    0
      [-20,-11]:   1    0    0    0    0    0    0    1    0    0
      [-10, -1]:   0    2    0    0    0    0    0    0    1    2
      [  0,  9]:   0    0    1    1    0    2    0    1    4    1
      [ 10, 19]:   1    0    1    0    4    1    2    1    2    6
      [ 20, 29]:   0    0    2    0    2    0    0    0    3    0
      [ 30, 39]:   0    0    0    0    0    0    0    0    0    0
      [ 40, 49]:   0    0    0    0    0    0    0    0    0    0
greater than 49:   0
```

Not all inlinee `SizeImpacts` exhibit such wide distributions. Some spread
just a little:

```
Inlining for System.Environment:GetResourceString(ref):ref (size 15)
Instances 2238 SizeImpact Mean 0.01 Min -3 Max 6
Distribution
  less than -50:   0
      [-50,-41]:   0    0    0    0    0    0    0    0    0    0
      [-40,-31]:   0    0    0    0    0    0    0    0    0    0
      [-30,-21]:   0    0    0    0    0    0    0    0    0    0
      [-20,-11]:   0    0    0    0    0    0    0    0    0    0
      [-10, -1]:   0    0    0    0    0    0    0    7    0    7
      [  0,  9]:2212    3    0    5    0    0    4    0    0    0
      [ 10, 19]:   0    0    0    0    0    0    0    0    0    0
      [ 20, 29]:   0    0    0    0    0    0    0    0    0    0
      [ 30, 39]:   0    0    0    0    0    0    0    0    0    0
      [ 40, 49]:   0    0    0    0    0    0    0    0    0    0
greater than 49:   0
```

and some not at all:

```
Inlining for System.DateTime:get_Ticks():long:this (size 15)
Instances 129 SizeImpact Mean 0.00 Min 0 Max 0
Distribution
  less than -50:   0
      [-50,-41]:   0    0    0    0    0    0    0    0    0    0
      [-40,-31]:   0    0    0    0    0    0    0    0    0    0
      [-30,-21]:   0    0    0    0    0    0    0    0    0    0
      [-20,-11]:   0    0    0    0    0    0    0    0    0    0
      [-10, -1]:   0    0    0    0    0    0    0    0    0    0
      [  0,  9]: 129    0    0    0    0    0    0    0    0    0
      [ 10, 19]:   0    0    0    0    0    0    0    0    0    0
      [ 20, 29]:   0    0    0    0    0    0    0    0    0    0
      [ 30, 39]:   0    0    0    0    0    0    0    0    0    0
      [ 40, 49]:   0    0    0    0    0    0    0    0    0    0
greater than 49:   0
```

So it appears there must be other -- perhaps many -- factors that
influence the `SizeImpact` of a particular inline. The question is,
what factors are important, how do we measure them, and how do they
they combine to influence the overall result? The remainder of this
document will look into some of the ways we can answer this question.

## Some Ground Rules

Before we move on, however, we should address what kind of information
will actually be available to observe and use in building a heuristic
model.

It's not realistic to feed the heuristic on the actual encoded size of
methods. While compiling a method `C` obtaining the encoded size of
`C` is problematic. It's also quite likely that the encoded size of
the inlinee `E` is unknown, except possibly in some AOT scenarios
where one could generally arrange for methods to be compiled in
bottom-up order (that is, callees before callers). While one could
imagine spending compile time doing experimental compilations of `C`'s
and `E`'s to see what code size results, this is probably too costly
to be practical.

Second, even if we could obtain the actual size of prospective inline
candidates, we might not want to use this data. The final code
sequence emitted by the compiler depends intimately on details of the
target architecture, runtime conventions (ABIs), and capabilities of the
compiler phases that run after inlining. If we allow feedback into hey
heuristics by incorporating data from these "downstream" sources, we
introduce various forms of coupling that have important
consequences. For example, it would be challenging to work on a
downstream phase (say, register allocation) if some trivial change to
allocation in an inlinee `E` perturbs inlining decisions for other
methods like `C`. Likewise we may or may not want to allow runtime
conventions to influence inlining -- if our application can run cross
platform (say in both Windows and Linux) we might not want the various
versions to exhibit vastly different inlining patterns.

For that reason, we intend to restrict the information we can use in
forming heuristics to things that are generally true of the caller and
callee, and the general capabilities of the downstream phases. For the
caller and callee this largely means information that can be obtained
by inspecting either the input to the compiler, or the compiler's IR
in the stages leading up to inlining, and perhaps (if available) some
carefully chosen information obtained from actual compilation of the
callee.

At the same time, we still intend for the predictions made by the
heuristics to be indicative of the final binary size. We have no other
reliable source of truth to guide us.

Given all this, we can restate the central question as: how best can
the compiler estimate the ultimate size impact of an inline while
restricting itself to considering features that are generally true of
caller, callee, and the capabilities of the compiler?

## Building a Heuristic Manually

The tried-and-true approach is to build the heuristic manually. There
are two prongs to the approach: the first is case analysis of actual
behavior, and the second is modelling based on the compiler writer's
experience and intuition.

### Some Case Analysis

#### Case 1: Maximal Savings

It's always instructive to look at actual behavior, so let's consider
some of the cases. From the table above for `System.Object:.ctor()` we
see a number of cases where there were large savings in byte size. The
most significant of these is the following:

```
Inline System.Object:.ctor():this
into   System.IO.UnmanagedMemoryAccessor:.ctor(ref,long,long,int):this
CalleeSize  = 6
CallerSize  = 72
CallerSize' = 24
SizeImpact  = -48
```

where `System.Object:.ctor():this` is simply:

```Assembly
; System.Object:.ctor():this
; Total bytes of code 6, prolog size 5
       0F1F440000           nop
       C3                   ret
```

As an aside, one might wonder why there are 5 bytes of nops here --
they are put there to support the *rejit* feature used by the
application insights framework.

The caller's source code shows it simply delegates immediately to
another method to initialize the object:

```C#
 UnmanagedMemoryAccessor(
     SafeBuffer buffer, Int64 offset,
     Int64 capacity, FileAccess access) {
   Initialize(buffer, offset, capacity, access);
 }
```

Here's the code before the inline:

```Assembly
; BEFORE
; System.IO.UnmanagedMemoryAccessor:.ctor(ref,long,long,int):this
; Total bytes of code 72, prolog size 10
       4156                 push     r14
       57                   push     rdi
       56                   push     rsi
       55                   push     rbp
       53                   push     rbx
       4883EC20             sub      rsp, 32
       488BF1               mov      rsi, rcx
       488BFA               mov      rdi, rdx
       498BD8               mov      rbx, r8
       498BE9               mov      rbp, r9
       488BCE               mov      rcx, rsi
       E800000000           call     System.Object:.ctor():this
       448B742470           mov      r14d, dword ptr [rsp+70H]
       4489742470           mov      dword ptr [rsp+70H], r14d
       488BCE               mov      rcx, rsi
       488BD7               mov      rdx, rdi
       4C8BC3               mov      r8, rbx
       4C8BCD               mov      r9, rbp
       488D0500000000       lea      rax, [(reloc)]
       4883C420             add      rsp, 32
       5B                   pop      rbx
       5D                   pop      rbp
       5E                   pop      rsi
       5F                   pop      rdi
       415E                 pop      r14
       48FFE0               rex.jmp  rax
```

and here's the code after the inline:

```Assembly
; AFTER
; System.IO.UnmanagedMemoryAccessor:.ctor(ref,long,long,int):this
; Total bytes of code 24, prolog size 5
       0F1F440000           nop
       90                   nop
       8B442428             mov      eax, dword ptr [rsp+28H]
       89442428             mov      dword ptr [rsp+28H], eax
       488D0500000000       lea      rax, [(reloc)]
       48FFE0               rex.jmp  rax
```

In both cases this method ends up tail calling to `Initialize` (via the
`rex.jmp`), passing it the exact set of arguments that
was passed to the method.

In the before case, the un-inlined call changes the method from a leaf
to a non-leaf.  Leaf functions don't need to set up their own frame,
while non-leaves do. So some of the extra code in the prolog and
epilog is the frame setup. Beyond that, however, the un-inlined callee
forces the compiler to move the arguments that will be ultimately be
passed to the tail-called method out of the volatile registers and
into preserved registers before the call, then back again to again to
the proper argument registers after the call.  And in order to use the
callee saves these registers must be pushed in the prolog and popped
in the epilog.

In the after case, no frame setup is required, no registers need to be
preserved across the call, so no registers need saving and
restoring. The two `mov`s that remain appear to be gratuitous. The
first `nop' is there for rejit padding. Not sure why the second one is
there.

So we can now see why this particular case has such extreme
`SizeImpact`: the un-inlined call triggers frame creation and a fair
amount of shuffling to accommodate the potential side effects of the
call.

#### Case 2: Typical Savings

In the data above, the inline of `System.Object:.ctor()` typically
saves either 8 or 9 bytes of code. Let's look at one such case.

```
Inline System.Object:.ctor():this
into   System.Security.SecurityElement:.ctor():this
CalleeSize = 6
CallerSize = 15
CallerSize' = 6
SizeImpact = -9
```

Here are before and after listings for the caller:

```Assembly
; BEFORE
; System.Security.SecurityElement:.ctor():this
; Total bytes of code 15, prolog size 5
       0F1F440000           nop
       488D0500000000       lea      rax, [(reloc)]
       48FFE0               rex.jmp  rax
```

```Assembly
; AFTER
; System.Security.SecurityElement:.ctor():this
; Total bytes of code 6, prolog size 5
       0F1F440000           nop
       C3                   ret
```

In this case the call site was initially handled via a tail call that
required a 7 byte `lea` and a 3 byte `rex.jmp`. With the inline all
that's left is a single byte `ret`. This case covers 154 of the 187
instances where inlining `System.Object:.ctor()` saved 9 bytes.

### Cases 3, 4: Size May Decrease or Increase

Now let's look at a set of examples where an inline can either
decrease or increase the caller size. Here's the good case:

```
Inline System.IntPtr:op_Explicit(long):long
into   System.Runtime.InteropServices.Marshal:WriteIntPtr(ref,int,long)
CalleeSize  = 35
CallerSize  = 50
CallerSize' = 22
SizeImpact  = -28
```

Here's the callee code:

SIGH -- turns out the above has the wrong callee size -- there are
several overloaded versions and my script picks up the wrong one.
true callee is only 9 bytes long:

```Assembly
; System.IntPtr:op_Explicit(long):long
; Total bytes of code 9, prolog size 5
       0F1F440000           nop
       488BC1               mov      rax, rcx
       C3                   ret
```

At any rate the `SizeImpact` is still correct.

Here's the before caller code:

```Assembly
; BEFORE
; System.Runtime.InteropServices.Marshal:WriteIntPtr(ref,int,long)
; Total bytes of code 50, prolog size 12
       55                   push     rbp
       57                   push     rdi
       56                   push     rsi
       4883EC20             sub      rsp, 32
       488D6C2430           lea      rbp, [rsp+30H]
       488BF1               mov      rsi, rcx
       8BFA                 mov      edi, edx
       498BC8               mov      rcx, r8
       E800000000           call     System.IntPtr:op_Explicit(long):long
       4C8BC0               mov      r8, rax
       8BD7                 mov      edx, edi
       488BCE               mov      rcx, rsi
       488D0500000000       lea      rax, [(reloc)]
       488D65F0             lea      rsp, [rbp-10H]
       5E                   pop      rsi
       5F                   pop      rdi
       5D                   pop      rbp
       48FFE0               rex.jmp  rax   // tail call WriteInt64
```

and the after caller code:

```Assembly
; AFTER
; System.Runtime.InteropServices.Marshal:WriteIntPtr(ref,int,long)
; Total bytes of code 22, prolog size 10
       55                   push     rbp
       4883EC20             sub      rsp, 32
       488D6C2420           lea      rbp, [rsp+20H]
       E800000000           call     System.Runtime.InteropServices.Marshal:WriteInt64(ref,int,long)
       90                   nop
       488D6500             lea      rsp, [rbp]
       5D                   pop      rbp
       C3                   ret
```

Somewhat confusingly, the inline has stopped the jit from making a
tail call, and the size savings should be even greater than they
are. There appears to be some kind of code generation issue within the
jit. The IL for the callee is

```
IL_0000  0f 00             ldarga.s     0x0
IL_0002  7b 8f 02 00 04    ldfld        0x400028F
IL_0007  6e                conv.u8
IL_0008  2a                ret

```
and the JIT rejects the tail call because

```
Rejecting tail call late for call [000008]: Local address taken
```

so presumably the `ldarga.s` in the inlinee is causing trouble.

Now let's see if that same inlinee can cause a size increase. Here's a
case:

(NOTE this is calling a different overload..., need to get this
straghtened out)

```
Inline System.IntPtr:op_Explicit(long):long [35]
into   EventData:get_DataPointer():long:this [18] size 35 delta 17
CalleeSize  = 35
CallerSize  = 18
CallerSize' = 35
SizeImpact  = 17
```

```Assembly
; BEFORE
; EventData:get_DataPointer():long:this
; Total bytes of code 18, prolog size
       0F1F440000           nop
       488B09               mov      rcx, qword ptr [rcx]
       488D0500000000       lea      rax, [(reloc)]
       48FFE0               rex.jmp  rax
```

```Assembly
; AFTER
; EventData:get_DataPointer():long:this
; Total bytes of code 35, prolog size 5
       4883EC28             sub      rsp, 40
       488B11               mov      rdx, qword ptr [rcx]
       33C9                 xor      rcx, rcx
       48894C2420           mov      qword ptr [rsp+20H], rcx
       488D4C2420           lea      rcx, bword ptr [rsp+20H]
       E800000000           call     System.IntPtr:.ctor(long):this
       488B442420           mov      rax, qword ptr [rsp+20H]
       4883C428             add      rsp, 40
       C3                   ret
```

Here the un-inlined case made a tail call. The inlined case was unable
to do the same optimization for some reason, though it looks like it
should have been able to do so as well.
