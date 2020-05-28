*This blog post originally appeared on David Broman's blog on 12/18/2007*


In this debugging post, I'll talk about the various IDs the profiling API exposes to your profiler, and how you can use SOS to give you more information about the IDs.  As usual, this post assumes you're using CLR 2.x.

## S.O.What Now?

SOS.DLL is a debugger extension DLL that ships with the CLR.  You'll find it sitting alongside mscorwks.dll.  While originally written as an extension to the windbg family of debuggers, Visual Studio can also load and use SOS.  If you search the MSDN blogs for "SOS" you'll find lots of info on it.  I'm not going to repeat all that's out there, but I'll give you a quick primer on getting it loaded.

In windbg, you'll need mscorwks.dll to load first, and then you can load SOS.  Often, I don't need SOS until well into my debugging session, at which point mscorwks.dll has already been loaded anyway.  However, there are some cases where you'd like SOS loaded at the first possible moment, so you can use some of its commands early (like !bpmd to set a breakpoint on a managed method).  So a surefire way to get SOS loaded ASAP is to have the debugger break when mscorwks gets loaded (e.g., "sxe ld mscorwks").  Once mscorwks is loaded, you can load SOS using the .loadby command:

| 
```
0:000\> **sxe ld mscorwks**
 0:000\> g
 ModLoad: 79e70000 7a3ff000 C:\Windows\Microsoft.NET\Framework\v2.0.50727\mscorwks.dll
 eax=00000000 ebx=00000000 ecx=00000000 edx=00000000 esi=7efdd000 edi=20000000
 eip=77a1a9fa esp=002fea38 ebp=002fea78 iopl=0 nv up ei pl nz na po nc
 cs=0023 ss=002b ds=002b es=002b fs=0053 gs=002b efl=00000202
 ntdll!NtMapViewOfSection+0x12:
 77a1a9fa c22800 ret 28h
 0:000\> **.loadby sos mscorwks**
```
 |

With SOS loaded, you can now use its commands to inspect the various IDs that the profiling API passes to your profiler.

Note: The following contains implementation details of the runtime.  While these details are useful as a debugging aid, your profiler code cannot make assumptions about them.  These implementation details are subject to change at whim.

## FunctionID Walkthrough

For starters, take a look at FunctionIDs.  Your profiler receives a FunctionID anytime you hit a callback that needs to, well, identify a function!  For example, when it's time to JIT, the CLR issues JITCompilationStarted (assuming your profiler subscribed to that callback), and one of the parameters to the callback is a FunctionID.  You can then use that FunctionID in later calls your profiler makes back into the CLR, such as GetFunctionInfo2.

As far as your profiler is concerned, a FunctionID is just an opaque number.  It has no meaning in itself; it's merely a handle you can pass back into the CLR to refer to the function.  Under the covers, however, a FunctionID is actually a pointer to an internal CLR data structure called a MethodDesc.  I must warn you again that you cannot rely on this information when coding your profiler.  The CLR team reserves the right to change the underlying meaning of a FunctionID to be something radically different in later versions.  This info is for entertainment and debugging purposes only!

Ok, so FunctionID = (MethodDesc \*).  How does that help you?  SOS just so happens to have a command to inspect MethodDescs: !dumpmd.  So if you're in a debugger looking at your profiler code that's operating on a FunctionID, it can beneficial to you to find out which function that FunctionID actually refers to.  In the example below, the debugger will break in my proifler's JITCompilationStarted callback and look at the FunctionID.  It's assumed that you've already loaded SOS as per above.

| 
```
0:000\> bu UnitTestSampleProfiler!SampleCallbackImpl::JITCompilationStarted
 0:000\> g
 ...
```

```
Breakpoint 0 hit
 eax=00c133f8 ebx=00000000 ecx=10001218 edx=00000001 esi=002fec74 edi=00000000
 eip=10003fc0 esp=002fec64 ebp=002feca4 iopl=0 nv up ei pl nz na po nc
 cs=0023 ss=002b ds=002b es=002b fs=0053 gs=002b efl=00000202
 UnitTestSampleProfiler!SampleCallbackImpl::JITCompilationStarted:
 10003fc0 55 push ebp
```
 |

The debugger is now sitting at the beginning of my profiler's JITCompilationStarted callback.  Let's take a look at the parameters.

| 
```
0:000\> dv
 this = 0x00c133f8
 **functionID = 0x1e3170**
 fIsSafeToBlock = 1
```
 |

Aha, that's the FunctionID about to get JITted.  Now use SOS to see what that function really is.

| 
```
0:000\> !dumpmd 0x1e3170
 Method Name: test.Class1.Main(System.String[])
 Class: 001e1288
**MethodTable: 001e3180** mdToken: 06000001
 Module: 001e2d8c
 IsJitted: no
 m\_CodeOrIL: ffffffff
```
 |

Lots of juicy info here, though the Method Name typically is what helps me the most in my debugging sessions.  mdToken tells us the metadata token for this method.  MethodTable tells us where another internal CLR data structure is stored that contains information about the class containing the function.  In fact, the profiing API's ClassID is simply a MethodTable \*.  [Note: the "Class: 001e1288" in the output above is very different from the MethodTable, and thus different from the profiling API's ClassID.  Don't let the name fool you!]  So we could go and inspect a bit further by dumping information about the MethodTable:

| 
```
0:000\> !dumpmt 0x001e3180
 EEClass: 001e1288
 Module: 001e2d8c
 Name: test.Class1
 mdToken: 02000002 (C:\proj\HelloWorld\Class1.exe)
 BaseSize: 0xc
 ComponentSize: 0x0
 Number of IFaces in IFaceMap: 0
 Slots in VTable: 6
```
 |

And of course, !dumpmt can be used anytime you come across a ClassID and want more info on it.

[

Update 12/29/2011

In the original posting, I neglected to mention that there are cases where ClassIDs are not actually MethodTable \*'s, and thus cannot be inspected via !dumpmt.  The most common case are some kinds of arrays, though there are other cases as well, such as function pointers, byrefs, and others.  In these cases, if you look at the ClassID value in a debugger, you'll see that it's not pointer-aligned.  Some of the low-order bits may be intentionally set by the CLR to distinguish these ClassIDs from MethodTable pointers.  Although !dumpmt cannot be used on these ClassIDs, you can safely call profiling API methods such as IsArrayClass or GetClassIDInfo(2) on them.

]

## IDs and their Dumpers

Now that you see how this works, you'll need to know how the profiling IDs relate to the various SOS commands that dump info on them:

| **ID** | **Internal CLR Structure** | **SOS command** |
| AssemblyID | Assembly \* | !DumpAssembly |
| AppDomainID | AppDomain \* | !DumpDomain |
| ModuleID | Module \* | !DumpModule |
| ClassID | MethodTable \* | !DumpMT |
| ThreadID | Thread \* | !Threads (see note) |
| FunctionID | MethodDesc \* | !DumpMD |
| ObjectID | Object \* (i.e., a managed object) | !DumpObject |

Note:  !Threads takes no arguments, but simply dumps info on all threads that have ever run managed code.  If you use "!Threads -special" you get to see other special threads separated out explicitly, including threads that perform GC in server-mode, the finalizer thread, and the debugger helper thread.

## More Useful SOS Commands

It would probably be quicker to list what _isn't_ useful!  I encourage you to do a !help to see what's included. Here's a sampling of what I commonly use:

!u is a nice SOS analog to the windbg command "u". While the latter gives you a no-frills disassembly, !u works nicely for managed code, including spanning the disassembly from start to finish, and converting metadata tokens to names.

!bpmd lets you place a breakpoint on a managed method. Just specify the module name and the fully-qualified method name. For example:

| 
```
!bpmd MyModule.exe MyNamespace.MyClass.Foo
```
 |

If the method hasn't jitted yet, no worries. A "pending" breakpoint is placed.  If your profiler performs IL rewriting, then using !bpmd on startup to set a managed breakpoint can be a handy way to break into the debugger just before your instrumented code will run (which, in turn, is typically just after your instrumented code has been jitted). This can help you in reproducing and diagnosing issues your profiler may run into when instrumenting particular functions (due to something interesting about the signature, generics, etc.).

!PrintException: If you use this without arguments you get to see a pretty-printing of the last outstanding managed exception on the thread; or specify a particular Exception object's address.

 

Ok, that about does it for SOS. Hopefully this info can help you track down problems a little faster, or better yet, perhaps this can help you step through and verify your code before problems arise.

