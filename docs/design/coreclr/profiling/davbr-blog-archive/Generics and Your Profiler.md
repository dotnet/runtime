*This blog post originally appeared on David Broman's blog on 1/28/2010*


If you’re writing a profiler that you expect to run against CLR 2.0 or greater, you probably care about generics. Whether you're reporting call stacks or instrumenting code, it's possible the users of your profiler wrote some of that code using generic types. And if not, it's still quite likely they used generic types from libraries they depend on, such as those that ship with the .NET Framework. Reporting as much detail as you can, such as which type arguments were used to instantiate a generic type that appears in a call stack, can help your users diagnose their problems more effectively.

## Terminology

Let's say a C# developer writes code like this:


```
class MyClass<S>
{
	static string Foo<T>(S instanceOfS, T instanceOfT)
	{
		return instanceOfS.ToString() + instanceOfT.ToString();
	}
}
```

Here we have a generic function, MyClass\<S\>.Foo\<T\>.  Let's say the developer instantiated MyClass & Foo by making the following function call:

```
MyClass<int>.Foo<float>(4, 8.8);
```

It's important to distinguish between **function** arguments and **type** arguments.  The function arguments are the dudes inside the parentheses—4 and 8.8 in the example above.  Type arguments are the things you find inside the angle brackets \<\>.  Foo is given one type argument, float.  Foo belongs to class MyClass, which itself is given the type argument, int.

It’s worth spending a bit of time thinking about this.  When one sees the term “type arguments”, one might mistake that for “argument types”, or “types of the function arguments”, which in the above case would be int _and_ float, since the function takes two function arguments.  But this is not what I mean by “type argument”.  A “type argument” is what the developer provides in place of a generic type parameter that sits inside the angle brackets.  This is irrespective of what function arguments are passed to the function.  For example the generic function Alloc\<U\>:

```
U Alloc<U>() { return new U(); }
```

takes no function arguments at all, but it still requires a type argument (for the “U”) in order to be instantiated.

## GetFunctionInfo2

So if you were to get the FunctionID for MyClass\<int\>.Foo\<float\>, and you passed that FunctionID to GetFunctionInfo2, what should you get back in the [out] parameters?

```
HRESULT GetFunctionInfo2([in] FunctionID funcId,
						 [in] COR_PRF_FRAME_INFO frameInfo,
						 [out] ClassID *pClassId,
						 [out] ModuleID *pModuleId,
						 [out] mdToken *pToken,
						 [in] ULONG32 cTypeArgs,
						 [out] ULONG32 *pcTypeArgs,
						 [out] ClassID typeArgs[]);
```

\*pClassId: This will be the ClassID for the instantiated MyClass\<int\>.  More on this later.

\*pModuleId: module defining the mdMethodDef token returned (see next parameter).  If funcId is a generic function defined in one module, its instantiating type arguments are defined in other modules, and the function is instantiated and called from yet another module, this parameter will always tell you that first module—the one containing the original definition of the generic function (i.e., funcId’s mdMethodDef).

\*pToken: This is the metadata token (mdMethodDef) for MyClass\<S\>.Foo\<T\>.  Note that you get the same mdMethodDef for any conceivable instantiation of a generic method.

typeArgs[]: This is the array of **type arguments** to MyClass\<int\>.Foo\<float\>.  So this will be an array of only one element: the ClassID for float.  (The int in MyClass\<int\> is a type argument to MyClass, not to Foo, and you would only see that when you call GetClassIDInfo2 with MyClass\<int\>.)

## GetClassIDInfo2

OK, someone in parentheses said something about calling GetClassIDInfo2, so let’s do that.  Since we got the ClassID for MyClass\<int\> above, let’s pass it to GetClassIDInfo2 to see what we get:

```
HRESULT GetClassIDInfo2([in] ClassID classId,
						[out] ModuleID *pModuleId,
						[out] mdTypeDef *pTypeDefToken,
						[out] ClassID *pParentClassId,
						[in] ULONG32 cNumTypeArgs,
						[out] ULONG32 *pcNumTypeArgs,
						[out] ClassID typeArgs[]);
```

\*pModuleId: module defining the mdTypeDef token returned (see next parameter).  If classId is a generic class defined in one module, its instantiating type arguments are defined in other modules, and the class is instantiated in yet another module, this parameter will always tell you that first module—the one containing the definition of the generic class (i.e., classId’s mdTypeDef).

\*pTypeDefToken: This is the metadata token (mdTypeDef) for MyClass\<S\>.  As with the mdMethodDef in the previous section, you’ll get the same mdTypeDef for any conceivable instantiation of MyClass\<S\>.

\*pParentClassId: As with any class, this [out] parameter will tell you the base class.  If the base class itself were a generic class, then this would be the ClassID for the fully instantiated base class.  You could then use GetClassIDInfo2 on \*pParentClassId to determine its generic type arguments.

typeArgs: This is the array of type arguments used to instantiate classId, which in the above example is MyClass\<int\>.  So in this example, typeArgs will be an array of only one element: the ClassID for int.

## COR\_PRF\_FRAME\_INFO

You may have noticed I ignored this parameter in my description of GetFunctionInfo2.  You can pass NULL if you want, and nothing really bad will happen to you, but you’ll often get some incomplete results: you won’t get very useful typeArgs coming back, and you’ll often see NULL returned in \*pClassId.

To understand why, it’s necessary to understand an internal optimization the CLR uses around sharing code for generics: If two instantiations of the same generic function would result in identical JITted code, then why not have them share one copy of that code?  The CLR chooses to share code if all of the type parameters are instantiated with reference types.  If you want to read more about this, [here’s](https://docs.microsoft.com/en-us/archive/blogs/carlos/net-generics-and-code-bloat-or-its-lack-thereof) a place to go.

For now, the important point is that, once we’re inside JITted code that is shared across different generic instantiations, how can one know which instantiation is the actual one that caused the current invocation?  Well, in many cases, the CLR may not have that data readily lying around.  However, as a profiler, you can capture this information and pass it back to the CLR when it needs it.  This is done through a COR\_PRF\_FRAME\_INFO.  There are two ways your profiler can get a COR\_PRF\_FRAME\_INFO:

1. Via slow-path Enter/Leave/Tailcall probes
2. Via your DoStackSnapshot callback

I lied.  #1 is really the only way for your profiler to get a COR\_PRF\_FRAME\_INFO.  #2 may seem like a way—at least the profiling API suggests that the CLR gives your profiler a COR\_PRF\_FRAME\_INFO in the DSS callback—but unfortunately the COR\_PRF\_FRAME\_INFO you get there is pretty useless.  I suspect the COR\_PRF\_FRAME\_INFO parameter was added to the signature of the profiler’s DSS callback function so that it could “light up” at some point in the future when we could work on finding out how to create a sufficiently helpful COR\_PRF\_FRAME\_INFO during stack walks.  However, that day has not yet arrived.  So if you want a COR\_PRF\_FRAME\_INFO, you’ll need to grab it—and use it from—your slow-path Enter/Leave/Tailcall probe.

With a valid COR\_PRF\_FRAME\_INFO, GetFunctionInfo2 will give you helpful, specific ClassIDs in the typeArgs [out] array and pClassId [out] parameter.  If the profiler passes NULL for COR\_PRF\_FRAME\_INFO, here’s what you can expect:

- If you’re using CLR V2, pClassId will point to NULL if the function sits on _any_ generic class (shared or not).  In CLR V4 this got a little better, and you’ll generally only see pClassId point to NULL if the function sits on a “shared” generic class (instantiated with reference types).
  - Note: If it’s impossible for the profiler to have a COR\_PRF\_FRAME\_INFO handy to pass to GetFunctionInfo2, and that results in a NULL \*pClassID, the profiler can always use the metadata interfaces to find the mdTypeDef token of the class on which the function resides for the purposes of pretty-printing the class name to the user.  Of course, the profiler will not know the specific instantiating type arguments that were used on the class in that case.
- the typeArgs [out] array will contain the ClassID for **System.\_\_Canon** , rather than the actual instantiating type(s), if the function itself is generic and is instantiated with reference type argument(s).

It’s worth noting here that there is a bug in GetFunctionInfo2, in that the [out] pClassId you get for the class containing the function can be wrong with generic virtual functions.  Take a look at [this forum post](http://social.msdn.microsoft.com/Forums/en-US/netfxtoolsdev/thread/ed6f972f-712a-48df-8cce-74f8951503fa/) for more information and a workaround.

## ClassIDs & FunctionIDs vs. Metadata Tokens

Although you can infer this from the above, let’s take a breather and review.  When you have multiple generic instantiations of a generic type, that type is defined with one mdTypeDef (metadata token), but you’ll see multiple ClassIDs (one per instantiation).  When you have multiple generic instantiations of a generic method, it’s defined with one mdMethodDef (metadata token), but you’ll see multiple FunctionIDs (one per instantiation).

For example, if we have code that uses MyClass\<int\>.Foo\<float\> and MyClass\<int\>.Foo\<long\>, you will see two JITCompilationStarted/JITCompilationFinished pairs, with two different FunctionIDs (one for each instantiation).  But when you look up the metadata token for those two FunctionIDs via GetFunctionInfo2, you’ll get the same mdMethodDef.

CLR’s generics sharing optimization complicates this somewhat.  You’ll really only see separate JIT notifications and separate FunctionIDs for different _unshared_ instantiations, and not necessarily for every different instantiation.  So if instead we have code that uses MyClass\<object\>.Foo\<string\> and MyClass\<SomeClassICreated\>.Foo\<AnotherClassICreated\>, you may only see one JITCompilationStarted/JITCompilationFinished pair, with only one FunctionID (representing the instantiation using System.\_\_Canon for the type arguments).  I say “may”, because generics sharing is an internal CLR optimization that can change at any time without affecting the correctness of managed code.  So your profiler cannot rely on a particular scheme the CLR may use to share generic code.  But it would be wise to be aware that sharing _can_ happen, so your profiler can deal with it appropriately.

So that covers JIT notifications—what about ClassLoad\* notifications in the same example?  Although the CLR shares _JITted code_ across reference-type instantiations, the CLR still maintains separate loaded _types_ for each generic instantiation of a generic class.  So in the example from the paragraph above you will see separate ClassLoad\* notifications with different ClassIDs for MyClass\<object\> and MyClass\<SomeClassICreated\>.  In fact, you will also see a separate ClassLoad\* notification (with yet another ClassID) for MyClass\<System.\_\_Canon\>.

If you got curious, and ran such a profiler under the debugger, you could use the SOS !dumpmt command with those different ClassIDs to see what you get.  By doing so, you’ll notice something interesting.  !dumpmt shows many values, including “Name”, which will correctly be the specific, fully-instantiated name of the type (different for all three ClassIDs).  !dumpmt also shows a thing called “EEClass”.  And you’ll notice this “EEClass” value is actually the _same_ for all 3 types.  (Remember from this [post](https://docs.microsoft.com/en-us/archive/blogs/davbr/debugging-your-profiler-ii-sos-and-ids) that EEClass is NOT the same thing as ClassID!)  That gives you a little window into some additional data sharing optimizations the CLR uses.  Stuff that remains the same across different generic instantiations of a class can be stored in a single place (the EEClass) and that single place can be referenced by the different generic instantiations of the class.  Note that if you also use a value type as the type argument when instantiating MyClass\<T\> (e.g., MyClass\<int\>), and then run !dumpmt on that ClassID, you’ll see an entirely different EEClass value in the output, as the CLR will not be sharing that subset of type data across generic instantiations that use type arguments that are value types.

## Instrumenting Generic Functions

If your profiler performs IL rewriting, it’s important to understand that it must NOT do instantiation-specific IL rewriting.  Huh?  Let’s take an example.  Suppose you’re profiling code that uses MyClass\<int\>.Foo\<float\> and MyClass\<int\>.Foo\<long\>.  Your profiler will see two JITCompilationStarted callbacks, and will have two opportunities to rewrite the IL.  Your profiler may call GetFunctionInfo2 on those two FunctionIDs and determine that they’re two different instantiations of the same generic function.  You may then be tempted to make use of the fact that one is instantiated with float, and the other with long, and provide different IL for the two different JIT compilations.  The problem with this is that the IL stored in metadata, as well as the IL provided to SetILFunctionBody, is always specified relative to the mdMethodDef.  (Remember, SetILFunctionBody doesn’t take a FunctionID as input; it takes an mdMethodDef.)  And it’s the profiler’s responsibility always to specify the same rewritten IL for any given mdMethodDef no matter how many times it’s JITted.  And a given mdMethodDef can be JITted multiple times due to a number of reasons:

- Two threads simultaneously trying to call the same function for the first time (and thus both trying to JIT that function)
- Strange dependency chains involving class constructors (more on this in the MSDN [reference topic](http://msdn.microsoft.com/en-us/library/ms230586.aspx))
- Multiple AppDomains using the same (non-domain-neutral) function
- And of course multiple generic instantiations!

Regardless of the reason, the profiler must always rewrite with exactly the same IL.  Otherwise, an invariant in the CLR will have been broken by the profiler, and you will get strange, undefined behavior as a result.  And no one wants that.



That’s it!  Hopefully this gives you a good idea of how the CLR Profiling API will behave in the face of generic classes and functions, and what is expected of your profiler.

