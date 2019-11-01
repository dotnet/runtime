*This blog post originally appeared on David Broman's blog on 3/22/2007*


The CLR Profiling API allows you to hook managed functions so that your profiler is called when a function is entered, returns, or exits via tailcall. We refer to these as Enter/Leave/Tailcall hooks, or “ELT” hooks. In this special multi-part investigative series, I will uncover the truth behind ELT. Today I'll write about some of the basics, NGEN, and a word on what we call "slow-path" vs. "fast-path".

### Setting up the hooks

1.     On initialization, your profiler must call SetEnterLeaveFunctionHooks(2) to specify which functions inside your profiler should be called whenever a managed function is entered, returns, or exits via tail call, respectively.
        _(Profiler calls this…)_
        ```
          HRESULT SetEnterLeaveFunctionHooks(
                        [in] FunctionEnter    \*pFuncEnter,
                        [in] FunctionLeave    \*pFuncLeave,
                        [in] FunctionTailcall \*pFuncTailcall);
        ```
         
        _(Profiler implements these…)_
        ```
        typedef void FunctionEnter(FunctionID funcID);
        typedef void FunctionLeave(FunctionID funcID);
        typedef void FunctionTailcall(FunctionID funcID);
        ```

        **OR**

        _(Profiler calls this…)_
        ```
          HRESULT SetEnterLeaveFunctionHooks2(
                        [in] FunctionEnter2    *pFuncEnter,
                        [in] FunctionLeave2    *pFuncLeave,
                        [in] FunctionTailcall2 *pFuncTailcall);
        ```
         

        _(Profiler implements these…)_
        ```
        typedef void FunctionEnter2(
                        FunctionID funcId,
                        UINT_PTR clientData,
                        COR_PRF_FRAME_INFO func,
                        COR_PRF_FUNCTION_ARGUMENT_INFO *argumentInfo);

        typedef void FunctionLeave2(
                        FunctionID funcId,
                        UINT_PTR clientData,
                        COR_PRF_FRAME_INFO func,
                        COR_PRF_FUNCTION_ARGUMENT_RANGE *retvalRange);

        typedef void FunctionTailcall2(
                        FunctionID funcId,
                        UINT_PTR clientData,
                        COR_PRF_FRAME_INFO func);
        ```

        This step alone does not cause the enter/leave/tailcall (ELT) hooks to be called.  But you must do this on startup to get things rolling.

2.     At any time during the run, your profiler calls SetEventMask specifying COR\_PRF\_MONITOR\_ENTERLEAVE in the bitmask.  Your profiler may set or reset this flag at any time to cause ELT hooks to be called or ignored, respectively.

### FunctionIDMapper

In addition to the above two steps, your profiler may specify more granularly which managed functions should have ELT hooks compiled into them:

1.     At any time, your profiler may call ICorProfilerInfo2::SetFunctionIDMapper to specify a special hook to be called when a function is JITted.

_(Profiler calls this…)_
```
  HRESULT SetFunctionIDMapper([in] FunctionIDMapper \*pFunc);
```
 

     _(Profiler implements this…)_
```
typedef UINT_PTR __stdcall FunctionIDMapper(
                FunctionID funcId,
                BOOL *pbHookFunction);
```
 

2. When FunctionIDMapper is called:
    a. Your profiler sets the pbHookFunction [out] parameter appropriately to determine whether the function identified by funcId should have ELT hooks compiled into it.
    b. Of course, the primary purpose of FunctionIDMapper is to allow your profiler to specify an alternate ID for that function.  Your profiler does this by returning that ID from FunctionIDMapper .  The CLR will pass this alternate ID to your ELT hooks (as funcID if you're using the 1.x ELT, and as clientData if you're using the 2.x ELT).

### Writing your ELT hooks

You may have noticed that corprof.idl warns that your implementations of these hooks must be \_\_declspec(naked), and that you've got to save registers you use. Yikes! This keeps things nice and efficient on the CLR code generation side, but at the expense of making life a little more difficult for profilers. For great low-level details of writing the hooks (including yummy sample code!) visit Jonathan Keljo's blog entry [here](http://blogs.msdn.com/jkeljo/archive/2005/08/11/450506.aspx).

### NGEN /Profile

The profiling API makes use of the fact that it can control the JITting of functions to enable features like ELT hooks. When managed code is NGENd, however, this assumption goes out the door. Managed code is already compiled before the process is run, so there’s no opportunity for the CLR to bake in calls to ELT hooks.

The solution is “NGEN /Profile”. For example, if you run this command against your assembly:

`ngen install MyAssembly.dll /Profile`

 

it will NGEN MyAssembly.dll with the “Profile” flavor (also called “profiler-enhanced”). This flavor causes extra hooks to be baked in to enable features like ELT hooks, loader callbacks, managed/unmanaged code transition callbacks, and the JITCachedFunctionSearchStarted/Finished callbacks.

The original NGENd versions of all your assemblies still stay around in your NGEN cache. NGEN /Profile simply causes a new set of NGENd assemblies to be generated as well, marked as the “profiler-enhanced” set of NGENd assemblies. At run-time, the CLR determines which flavor should be loaded. If a profiler is attached and enables certain features that only work with profiler-enhanced (not regular) NGENd assemblies (such as ELT via a call to SetEnterLeaveFunctionHooks(2), or any of several other features that are requested by setting particular event flags via SetEventMask), then the CLR will only load profiler-enhanced NGENd images--and if none exist then the CLR degrades to JIT in order to support the features requested by the profiler. In contrast, if the profiler does not specify such event flags, or there is no profiler to begin with, then the CLR loads the regular-flavored NGENd assemblies.

So how does NGEN /Profile make ELT hooks work? Well, in a profiler-enhanced NGEN module, each function gets compiled with calls at enter, leave, and tailcall time to a thunk. At run-time, the CLR decides what this thunk does. Either nothing (if no profiler requested ELT hooks), or jmp to the profiler's ELT hook. For example, if a profiler is loaded, requesting ELT notifications, and the CPU is executing near the top of a function inside a profiler-enhanced NGEN module, the disassembly will look something like this:

  `5bcfb8b0 call mscorwks!JIT_Writeable_Thunks_Buf+0x1b8 (5d8401d8)`

And where's the target of that call? Right here:

  `5d8401d8 jmp UnitTestSampleProfiler!Enter2Naked (023136b0)`

As you may have guessed, I happen to have a profiler named "UnitTestSampleProfiler" loaded and responding to ELT notifications, so that thunk will jmp right into my Enter2 hook. When I return from my hook, control goes right back to the managed function that called the thunk.

### Fast-path vs. Slow-path

There are two paths the CLR might take to get to your ELT hooks: fast & slow.  Fast means the JIT inserts a call from the JITted function directly into the profiler. (In profiler-enhanced NGEN modules, this translates to the thunk jumping directly to your ELT hook.) Slow means that some fixup must be done before control can be passed to your profiler, so the JIT inserts a call from the JITted function into helper functions in the CLR to do the fixup and finally forward the call to your profiler. (Or, in NGEN-land, the thunks jmp to those CLR helper functions.)

There are also two supported signatures for the ELT hooks: CLR 1.x (set via SetEnterLeaveFunctionHooks) and CLR 2.x-style (set via SetEnterLeaveFunctionHooks **2** ).

If your profiler requests 1.x ELT hooks, then slow-path is used for them all, end of story.

If your profiler requests 2.x ELT hooks, then slow-path is used for them all if any of the following event flags were set by your profiler:

- COR\_PRF\_ENABLE\_STACK\_SNAPSHOT:  “Slow” ensures that the CLR has an opportunity to do some housekeeping on the stack before your profiler is called so that if your profiler calls DoStackSnapshot from within the ELT hook, then the stack walk will have a marker to begin from.
- COR\_PRF\_ENABLE\_FUNCTION\_ARGS: “Slow” gives the CLR an opportunity to gather the function’s arguments on the stack for passing to the profiler’s enter hook.
- COR\_PRF\_ENABLE\_FUNCTION\_RETVAL: “Slow” gives the CLR an opportunity to gather the function’s return value on the stack for passing to your profiler’s leave hook.
- COR\_PRF\_ENABLE\_FRAME\_INFO: “Slow” gives the CLR an opportunity to gather generics information into a COR\_PRF\_FRAME\_INFO parameter to pass to your profiler.

Why do you care? Well, it's always good to know what price you're paying. If you don't need any of the features above, then you're best off not specifying those flags. Because then you'll see better performance as the managed code may call directly into your profiler without any gunk going on in the middle. Also, this information gives you some incentive to upgrade your profiler's old 1.x ELT hooks to the hip, new 2.x ELT style. Since 1.x ELT hooks always go through the slow path (so the CLR has an opportunity to rearrange the parameters to fit the old 1.x prototype before calling your profiler), you're better off using the 2.x style.

### Next time...

That about covers it for the ELT basics. Next installment of this riveting series will talk about that enigma known as tailcall.

