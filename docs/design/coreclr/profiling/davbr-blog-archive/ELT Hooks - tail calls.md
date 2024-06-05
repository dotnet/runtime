*This blog post originally appeared on David Broman's blog on 6/20/2007*


For most people the idea of entering or returning from a function seems straightforward. Your profiler's Enter hook is called at the beginning of a function, and its Leave hook is called just before the function returns. But the idea of a tail call and exactly what that means for the Profiling API is less straightforward.

In [Part 1](ELT Hooks - The Basics.md) I talked about the basics of the Enter / Leave / Tailcall hooks and generally how they work. You may want to review that post first if you haven't seen it yet. This post builds on that one by talking exclusively about the Tailcall hook, how it works, and what profilers should do inside their Tailcall hooks.

## Tail calling in general

Tail calling is a compiler optimization that saves execution of instructions and saves reads and writes of stack memory. When the last thing a function does is call another function (and other conditions are favorable), the compiler may consider implementing that call as a tail call, instead of a regular call.

```
static public void Main()
{
	Helper();
}

static public void Helper()
{
	One();
	Two();
	Three();
}

static public void Three()
{
	...
}
```

In the code above, the compiler may consider implementing the call from Helper() to Three() as a tail call. What does that mean, and why would that optimize anything? Well, imagine this is compiled without a tail call optimization. By the time Three() is called, the stack looks like this (my stacks grow UP):

```
Three
Helper
Main
```

Each of those functions causes a separate frame to be allocated on the stack. All the usual contents of a frame, including locals, parameters, the return address, saved registers, etc., get stored in each of those frames. And when each of those functions returns, the return address is read from the frame, and the stack pointer is adjusted to collapse the frame of the returning function. That's just the usual overhead associated with making a function call.

Now, if the call from Helper() to Three() were implemented as a tail call, we'd avoid that overhead, and Three() would just "reuse" the stack frame that had been set up for Helper(). While Three() is executing, the call stack would look like this:

```
Three
Main
```

And when Three() returns, it returns directly to Main() without popping back through Helper() first.

Folks who live in functional programming languages like Scheme use recursion at least as often as C++ or C# folks use while and for loops. Such functional programming languages depend on tail call optimizations (in particular tail recursion) to avoid overflowing the stack. While imperative languages like C++ or C# don't have such a vital need for tail call optimizations, it's still pretty handy as it reduces the number of instructions executed and the writes to the stack.  Also, it's worth noting that the amount of stack space used for a single frame can be more than you'd expect.  For example, in CLR x64, each regular call (without the tail call optimization) uses a minimum of 48 bytes of stack space, even if it takes no arguments, has no locals, and returns nothing.  So for small functions, the tail call optimization can provide a significant overhead reduction in terms of stack space.

## The CLR and tail calls

When you're dealing with languages managed by the CLR, there are two kinds of compilers in play. There's the compiler that goes from your language's source code down to IL (C# developers know this as csc.exe), and then there's the compiler that goes from IL to native code (the JIT 32/64 bit compilers that are invoked at run time or NGEN time). Both the source-\>IL and IL-\>native compilers understand the tail call optimization. But the IL-\>native compiler--which I'll just refer to as JIT--has the final say on whether the tail call optimization will ultimately be used. The source-\>IL compiler can help to generate IL that is conducive to making tail calls, including the use of the "tail." IL prefix (more on that later). In this way, the source-\>IL compiler can structure the IL it generates to persuade the JIT into making a tail call. But the JIT always has the option to do whatever it wants.

### When does the JIT make tail calls?

I asked Fei Chen and [Grant Richins](https://learn.microsoft.com/archive/blogs/grantri/), neighbors down the hall from me who happen to work on the JIT, under what conditions the various JITs will employ the tail call optimization. The full answer is rather detailed. The quick summary is that the JITs try to use the tail call optimization whenever they can, but there are lots of reasons why the tail call optimization can't be used. Some reasons why tail calling is a non-option:

- Caller doesn't return immediately after the call (duh :-))
- Stack arguments between caller and callee are incompatible in a way that would require shifting things around in the caller's frame before the callee could execute
- Caller and callee return different types
- We inline the call instead (inlining is way better than tail calling, and opens the door to many more optimizations)
- Security gets in the way
- The debugger / profiler turned off JIT optimizations

[Here](Tail call JIT conditions.md) are their full, detailed answers.

_Note that how the JIT decides whether to use the tail calling optimization is an implementation detail that is prone to change at whim.  **You must not take dependencies on this behavior**. Use this information for your own personal entertainment only._

## Your Profiler's Tailcall hook

I'm assuming you've already read through [Part 1](ELT Hooks - The Basics.md) and are familiar with how your profiler sets up its Enter/Leave/Tailcall hooks, so I'm not repeating any of those details here. I will focus on what kind of code you will typically want to place inside your Tailcall hook:

```
typedef void FunctionTailcall2(
                FunctionID funcId,
                UINT_PTR clientData,
                COR_PRF_FRAME_INFO func);
```

**Tip** : More than once I've seen profiler writers make the following mistake. They will take their naked assembly-language wrapper for their Enter2 and Leave2 hooks, and paste it again to use as the Tailcall2 assembly-language wrapper. The problem is they forget that the Tailcall2 hook takes a different number of parameters than the Enter2 / Leave2 hooks (or, more to the point, a different number of _bytes_ is passed on the stack to invoke the Tailcall2 hook). So, they'll take the "ret 16" at the end of their Enter2/Leave2 hook wrappers and stick that into their Tailcall2 hook wrapper, forgetting to change it to a "ret 12". Don't make the same mistake!

It's worth noting what these parameters mean. With the Enter and Leave hooks it's pretty obvious that the parameters your hook is given (e.g., funcId) apply to the function being Entered or Left. But what about the Tailcall hook? Do the Tailcall hook's parameters describe the caller (function making the tail call) or the callee (function being tail called into)?

Answer: the parameters refer to tail call **er**.

The way I remember it is that the Tailcall hook is like an "Alternative Leave" hook. A function ends either by returning (in which case the CLR invokes your Leave hook) or a function ends by tail calling out to somewhere else (in which case the CLR invokes your Tailcall hook). In either case (Leave hook or Tailcall hook) the hook's parameters tell you about the function that's _ending_. If a function happens to end by making a tail call, your profiler is not told the target of that tail call. (The astute reader will realize that actually your profiler _is_ told what the target of the tail call is--you need only wait until your Enter hook is called next, and that function will be the tail call target, or "tail callee". (Well, actually, this is true most of the time, but not all! (More on that later, but consider this confusing, nested series of afterthoughts a hint to a question I pose further down in this post.)))

Did you just count the number of closing parentheses to ensure I got it right? If so, I'd like to make fun of you but I won't--I'd have counted the parentheses, too. My house is glass.

Ok, enough dilly-dallying. What should your profiler do in its Tailcall hook? Two of the more common reasons profilers use Enter/Leave/Tailcall hooks in the first place is to keep **shadow stacks** or to maintain **call traces** (sometimes with timing information).

### Shadow stacks

The [CLRProfiler](http://www.microsoft.com/downloads/details.aspx?FamilyID=a362781c-3870-43be-8926-862b40aa0cd0&DisplayLang=en) is a great example of using Enter/Leave/Tailcall hooks to maintain shadow stacks. A shadow stack is your profiler's own copy of the current stack of function calls on a given thread at any given time. Upon Enter of a function, you push that FunctionID (and whatever other info interests you, such as arguments) onto your data structure that represents that thread's stack. Upon Leave of a function, you pop that FunctionID. This gives you a live list of managed calls in play on the thread. The CLRProfiler uses shadow stacks so that whenever the managed app being profiled chooses to allocate a new object, the CLRProfiler can know the managed call stack that led to the allocation. (Note that an alternate way of accomplishing this would be to call DoStackSnapshot at every allocation point instead of maintaining a shadow stack. Since objects are allocated so frequently, however, you'd end up calling DoStackSnapshot extremely frequently and will often see worse performance than if you had been maintaining shadow stacks in the first place.)



OK, so when your profiler maintains a shadow stack, it's clear what your profiler should do on Enter or Leave, but what should it do on Tailcall? There are a couple ways one could imagine answering that question, but only one of them will work! Taking the example from the top of this post, imagine the stack looks like this:

```
Helper
Main
```

and Helper is about to make a tail call into Three().  What should your profiler do?

Method 1: On tailcall, pop the last FunctionID.  (In other words, treat Tailcall just like Leave.)

So, in this example, when Helper() calls Three(), we'd pop Helper().  As soon as Three() is called, our profiler would receive an Enter for Three(), and our shadow stack would look like this:

```
Three
Main
```

This approach mirrors reality, because this is what the actual physical stack will look like.  Indeed, if one attaches a debugger to a live process, and breaks in while the process is inside a tail call, the debugger will show a call stack just like this, where you see the tail callee, but not the tail caller.  However, it might be a little confusing to a user of your profiler who looks at his source code and sees that Helper() (not Main()) calls Three().  He may have no idea that when Helper() called Three(), the JIT chose to turn that into a tail call. In fact, your user may not even know what a tail call is. You might therefore be tempted to try this instead:

Method 2: On tailcall, "mark" the FunctionID at the top of your stack as needing a "deferred pop" when its callee is popped, but don't pop yet.

With this strategy, for the duration of the call to Three(), the shadow stack will look like this:

```
Three
Helper (marked for deferred pop)
Main
```

which some might consider more user-friendly. And as soon as Three() returns, your profiler will sneakily do a double-pop leaving just this:

```
Main
```

So which should your profiler use: Method 1 or Method 2? Before I answer, take some time to think about this, invoking that hint I cryptically placed above in nested parentheses. And no, the fact that the parentheses were nested is not part of the actual hint.

The answer: Method 1. In principle, either method should be fine. However, the behavior of the CLR under certain circumstances will break Method 2. Those "certain circumstances" are what I alluded to when I mentioned "this is true most of the time, but not all" above.  These mysterious "certain circumstances" involve a managed function tail calling into a native helper function inside the runtime. Here's an example:
```
static public void Main()
{
	Thread.Sleep(44);
	Helper();
}
```

It just so happens that the implementation of Thread.Sleep makes a call into a native helper function in the bowels of the runtime. And that call happens to be the last thing Thread.Sleep does. So the JIT may helpfully optimize that call into a tail call. Here are the hook calls your profiler will see in this case:

```
(1) Enter (into Main)
(2) Enter (into Thread.Sleep)
(3) Tailcall (from Thread.Sleep)
(4) Enter (into Helper)
(5) Leave (from Helper)
(6) Leave (from Main)
```

Note that after you get a Tailcall telling you that Thread.Sleep is done, (in (3)), the very next Enter you get (in (4)) is NOT the Enter for the function being tail called. This is because the CLR only provides Enter/Leave/Tailcall hooks for _managed_ functions, and the very next managed function being entered is Helper().  So, how will Method 1 and Method 2 fare in this example?

Method 1: Shadow stack works

By popping on every Tailcall hook, your shadow stack stays up to date.

Method 2: Shadow stack fails

At stage (4), the shadow stack looks like this:

```
Helper
Thread.Sleep (marked for "deferred pop")
Main
```

If you think it might be complicated to explain tail calls to your users so they can understand the Method 1 form of shadow stack presentation, just try explaining why it makes sense to present to them that Thread.Sleep() is calling Helper()!

And of course, this can get arbitrarily nasty:

```
static public void Main()
{
	Thread.Sleep(44);
	Thread.Sleep(44);
	Thread.Sleep(44);
	Thread.Sleep(44);
	Helper();
}
```

would yield:
```
Helper
Thread.Sleep (marked for "deferred pop")
Thread.Sleep (marked for "deferred pop")
Thread.Sleep (marked for "deferred pop")
Thread.Sleep (marked for "deferred pop")
Main
```

And things get more complicated if you start to think about when you actually pop a frame marked for "deferred pop". In all the above examples, you would do so as soon as the frame above it gets popped. So once Helper() is popped (due to Leave()), you'd cascade-pop all the Thread.Sleeps. But what if there is no frame above the frames marked for "deferred pop"?  To wit:

```
static public void Main()
{
	Helper()
}

static public void Helper()
{
	Thread.Sleep(44);
	Thread.Sleep(44);
	Thread.Sleep(44);
	Thread.Sleep(44);
}
```

would yield:
```
Thread.Sleep (marked for "deferred pop")
Thread.Sleep (marked for "deferred pop")
Thread.Sleep (marked for "deferred pop")
Thread.Sleep (marked for "deferred pop")
Helper
Main
```

until you get a Leave hook for Helper(). At this point, you need to pop Helper() from your shadow stack, but he's not at the top-- he's buried under all your "deferred pop" frames. So your profiler would need to perform the deferred pops if a frame above OR below them gets popped. Hopefully, the yuckiness of this implementation will scare you straight.  But the confusion of presenting invalid stacks to the user is the real reason to abandon Method 2 and go with Method 1.

### Call tracing

The important lesson to learn from the above section is that sometimes a Tailcall hook will match up with the next Enter hook (i.e., the tail call you're notified of in your Tailcall hook will have as its callee the very function you're notified of in the next Enter hook), and sometimes the Tailcall hook will NOT match with the next Enter hook (in particular when the Tailcall hook refers to a tail call into a native helper in the runtime). And the sad fact is that the Enter/Leave/Tailcall hook design does not currently allow you to predict whether a Tailcall will match the next Enter.

As an illustration, consider two simple tail call examples:

**Matching Example**

```
static public void Main()
{
	One();
	...(other code here)...
}

static public void One()
{
	Two();
}
```

**Non-matching Example**

```
static public void Main()
{
	Thread.Sleep(44);
	Two();
}
```

In either case, your profiler will see the following hook calls

```
(1) Enter (into Main)
(2) Enter (into One / Thread.Sleep)
(3) Tailcall (from One / Thread.Sleep)
(4) Enter (into Two)
...
```

In the first example, (3) and (4) match (i.e., the tail call really does call into Two()). But in the second example, they do not (the tail call does NOT call into Two()).

Since you don't know when Tailcall will match the next Enter, your implementation of call tracing, like shadow stack maintenance, must treat a Tailcall hook just like a Leave. If you're logging when functions begin and end, potentially with the amount of time spent inside the function, then your Tailcall hook should basically do the same thing as your Leave hook. A call to your Tailcall hook indicates that the specified function is over and done with, just like a call to your Leave hook.

As with shadow stacks, this will sometimes lead to call graphs that could be confusing. "Matching Example" had One tail call Two, but your graph will look like this:

```
Main
|
|-- One
|-- Two
```

But at least this effect is explainable to your users, and is self-correcting after the tail call is complete, while yielding graphs that are consistent with your timing measurements. If instead you try to outsmart this situation and assume Tailcalls match the following Enter, the errors can snowball into incomprehensible graphs (see the nasty examples from the shadow stack section above).

### How often does this happen?

So when does a managed function in the .NET Framework tail call into a native helper function inside the CLR? In the grand scheme of things, not a lot. But it's a pretty random and fragile list that depends on which JIT is in use (x86, x64, ia64), and can easily change as parts of the runtime are rev'd, or even as JIT compilation flags are modified by debuggers, profilers, and other environmental factors while a process is active. So you should not try to guess this list and make dependencies on it.

### Can't I just turn tail calling off?!

If all this confusion is getting you down, you might be tempted to just avoid the problem in the first place. And yes, there is a way to do so, but I wouldn't recommend it in general. If you call SetEventMask, specifying COR\_PRF\_DISABLE\_OPTIMIZATIONS inside your mask, that will tell the JIT to turn off the tail call optimization. But the JIT will also turn off ALL optimizations. Profilers that shouldn't perturb the behavior of the app should definitely _not_ do this, as the code generation will be very different.

## Watching CLR tail calls in action

If you're writing a profiler with Enter/Leave/Tailcall hooks, you'll want to make sure you exercise all your hooks so they're properly tested. It's easy enough to make sure your Enter/Leave hooks are called--just make sure the test app your profiler runs against has a Main()! But how to make sure your Tailcall hook is called?

The surest way is to have a simple managed app that includes an obvious tail call candidate, and make sure the "tail." IL prefix is in place. You can use ilasm / ildasm to help build such an assembly. Here's an example I tried on x86 using C#.

Start with some simple code that makes a call that should easily be optimized into a tail call:

```
using System;
class Class1
{
	static int Main(string[] args)
	{
		return Helper(4);
	}

	static int Helper(int i)
	{
		Random r = new Random();
		i = (i / 1000) + r.Next();
		i = (i / 1000) + r.Next();
		return MakeThisATailcall(i);
	}

	static int MakeThisATailcall(int i)
	{
		Random r = new Random();
		i = (i / 1000) + r.Next();
		i = (i / 1000) + r.Next();
		return i;
	}
}
```

You'll notice there's some extra gunk, like calls to Random.Next(), to make the functions big enough that the JIT won't inline them. There are other ways to avoid inlining (including from the profiling API itself), but padding your test functions is one of the easier ways to get started without impacting the code generation of the entire process. Now, compile that C# code into an IL assembly:

```
csc /o+ Class1.cs
```

(If you're wondering why I specified /o+, I've found that if I _don't_, then suboptimal IL gets generated, and some extraneous instructions appear inside Helper between the call to MakeThisATailcall and the return from Helper. Those extra instructions would prevent the JIT from making a tail call.)

Run ildasm to get at the generated IL

```
ildasm Class1.exe
```

Inside ildasm, use File.Dump to generate a text file that contains a textual representation of the IL from Class1.exe.  Call it Class1WithTail.il.  Open up that file and add the tail. prefix just before the call you want optimized into a tail call (see highlighted yellow for changes):

```
.method private hidebysig static int32
 Helper(int32 i) cil managed
 {
~~// Code size 45 (0x2d)
~~ // Code size 46 (0x2e)
 .maxstack 2
 .locals init (class [mscorlib]System.Random V_0)
 IL_0000: newobj instance void [mscorlib]System.Random::.ctor()
 IL_0005: stloc.0
 IL_0006: ldarg.0
 IL_0007: ldc.i4 0x3e8
 IL_000c: div
 IL_000d: ldloc.0
 IL_000e: callvirt instance int32 [mscorlib]System.Random::Next()
 IL_0013: add
 IL_0014: starg.s i
 IL_0016: ldarg.0
 IL_0017: ldc.i4 0x3e8
 IL_001c: div
 IL_001d: ldloc.0
 IL_001e: callvirt instance int32 [mscorlib]System.Random::Next()
 IL_0023: add
 IL_0024: starg.s i
 IL_0026: ldarg.0
~~IL_0027: call int32 Class1::MakeThisATailcall(int32)
 IL_002c: ret
~~ IL_0027: tail.
 IL_0028: call int32 Class1::MakeThisATailcall(int32)
 IL_002d: ret
 } // end of method Class1::Helper
```

Now you can use ilasm to recompile your modified textual IL back into an executable assembly.

```
ilasm /debug=opt Class1WithTail.il
```

You now have Class1WithTail.exe that you can run!  Hook up your profiler and step through your Tailcall hook.

## You Can Wake Up Now

If you didn't learn anything, I hope you at least got some refreshing sleep thanks to this post. Here's a quick recap of what I wrote while you were napping:

- If the last thing a function does is call another function, that call may be optimized into a simple jump (i.e., "tail call").  Tail calling is an optimization to save the time of stack manipulation and the space of generating an extra call frame.
- In the CLR, the JIT has the final say on when it employs the tail call optimization. The JIT does this whenever it can, except for a huge list of exceptions. Note that the x86, x64, and ia64 JITs are all different, and you'll see different behavior on when they'll use the tail call optimizations.
- Since some managed functions may tail call into native helper functions inside the CLR (for which you won't get an Enter hook notification), your Tailcall hook should treat the tail call as if it were a Leave, and not depend on the next Enter hook correlating to the target of the last tail call.  With shadow stacks, for example, this means you should simply pop the calling function off your shadow stack in your Tailcall hook.
- Since tail calls can be elusive to find in practice, it's well worth your while to use ildasm/ilasm to manufacture explicit tail calls so you can step through your Tailcall hook and test its logic.

_David has been a developer at Microsoft for over 70 years (allowing for his upcoming time-displacement correction). He joined Microsoft in 2079, first starting in the experimental time-travel group. His current assignment is to apply his knowledge of the future to eliminate the "Wait for V3" effect customers commonly experience in his source universe. By using Retroactive Hindsight-ellisenseTM his goal is to "get it right the first time, this time" in a variety of product groups._

