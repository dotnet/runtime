*This blog post originally appeared on David Broman's blog on 3/6/2007*


A frequent topic of discussion between those of us on the CLR Profiling API team at Microsoft and our customers is how to write a profiler that rewrites IL to do cool stuff. Unfortunately, there still is very little documentation on how to do this, and what documentation there is, is rather scattered. I'm not going to say anything new here. But I will try to bring together the scattered info into one place.

**Q: Why do I care?**

You may want to instrument managed code to insert custom calls into your profiler to measure timings or code coverage, or record execution flow. Maybe you want to perform custom actions, like taking note whenever thread synchronization is invoked (e.g., Monitor.Enter,Leave). One way to do this is to take the original IL of the application you're profiling, and rewrite that IL to contain extra code or hooks into your profiler or managed code you ship alongside your profiler.

**Q: I can ship managed code alongside my profiler? Are you saying I can write my profiler in manage code?**

No, sorry to get your hopes up. Your profiler DLL must be unmanaged. Your ICorProfilerCallback implementations must be unmanaged (and should not call managed code). However, if you rewrite IL, it's perfectly fine for that IL to call into managed code that you've written and shipped alongside your profiler.

**Q: In a nutshell, what's involved?**

Well, first off, you're making a profiler. That means you create an unmanaged in-proc COM server DLL. If this much is already new to you, you should probably stop reading this, search MSDN for "ICorProfilerCallback", and grope through the table of contents for background info on how to write a profiler in general.

Keep in mind there are many ways to do this. I'll outline one of the more straightforward approaches here, and the adventurous should feel free to substitute their own ingredients:

- In your **ICorProfilerCallback2::ModuleLoadFinished** callback, you call **ICorProfilerInfo2::GetModuleMetadata** to get a pointer to a metadata interface on that module.
- QI for the metadata interface you want. Search MSDN for "IMetaDataImport", and grope through the table of contents to find topics on the metadata interfaces.
- Once you're in metadata-land, you have access to all the types in the module, including their fields and function prototypes. You may need to parse metadata signatures and [this signature parser](samples/sigparse.cpp) may be of use to you.
- In your **ICorProfilerCallback2::JITCompilationStarted** callback, you may use **ICorProfilerInfo2::GetILFunctionBody** to inspect the original IL, and **ICorProfilerInfo2::GetILFunctionBodyAllocator** and then **ICorProfilerInfo2::SetILFunctionBody** to replace that IL with your own.

**Q: What about NGEN?**

If you want to rewrite IL of NGENd modules, well, it's kind of too late because the original IL has already been compiled into native code. However, you do have some options.  If your profiler sets the **COR\_PRF\_USE\_PROFILE\_IMAGES** monitor event flag, that will force the "NGEN /Profile" version of the modules to load if they're available.  (I've already blogged a little about "NGEN /Profile", including how to generate those modules, [here](ELT&#32;Hooks&#32;-&#32;The&#32;Basics.md).)  So, at run-time, one of two things will happen for any given module.

1) If you set **COR\_PRF\_USE\_PROFILE\_IMAGES** and the NGEN /Profile version is available, it will load.  You will then have the opportunity to respond to the **JITCachedFunctionSearchStarted** callback.  When a function from an NGEN /Profile module is about to be executed for the first time, your profiler receives the **JITCachedFunctionSearchStarted** callback.  You may then set the \*pbUseCachedFunction [out] parameter to FALSE, and that will force the CLR to JIT the function instead of using the version that was already compiled into the NGEN /Profile module.  Then, when the CLR goes to JIT the function, your profiler receives the **JITCompilationStarted** callback and can perform IL rewriting just as it does above for functions that exist in non-NGENd mdoules.  What's nice about this approach is that, if you only need to instrument a few functions here and there, it can be faster not to have to JIT everything, just so you get the **JITCompilationStarted** callback for the few functions you're interested in.  This approach can therefore improve startup performance of the application while it's being profiled.  The disadvantage, though, is that your profiler must ensure the NGEN /Profile versions of all the modules get generated beforehand and get installed onto the user's machine.  Depending on your scenarios and customers, this may be too cumbersome to ensure.

2) If you set **COR\_PRF\_USE\_PROFILE\_IMAGES** and the NGEN /Profile version is _not_ available, the CLR will refuse to load the regular NGENd version of that module, and will instead JIT everything from the module.  Thus, it's ensured that you have the opportunity to intercept **JITCompilationStarted** , and can replace the IL as described above.

**Q: Any examples?**

Here is an MSDN article that talks about making an IL rewriting profiler:
[http://msdn.microsoft.com/en-us/magazine/cc188743.aspx](http://msdn.microsoft.com/en-us/magazine/cc188743.aspx)

**Q: Any caveats?**

Rewriting IL in mscorlib.dll functions can be dangerous, particularly in functions that are executed during startup initialization of the managed app or any of its AppDomains. The app may not be initialized enough to handle executing some of the managed code that might get called (directly or indirectly) from your rewritten IL.

If you're going to modify the IL to call into some of your own managed code, be careful about which functions you choose to modify. If you're not careful, you might accidentally modify the IL belonging to your own assembly and cause infinite recursion.

And then there's the worst of both worlds: when you need to rewrite IL to call into their own assemblies _and_ you happen to rewriting IL in mscorlib. Note that it's simply unsupported to force mscorlib.dll to reference any other assembly. The CLR loader treats mscorlib.dll pretty specially. The loader expects that, while everyone in the universe may reference mscorlib.dll, mscorlib.dll had better not reference any other assembly. If you absolutely must instrument mscorlib.dll by modifying IL, and you must have that IL reference some nifty new function of yours, you had better put that function into mscorlib.dll by dynamically modifying mscorlib.dll's metadata when it is loaded. In this case you no longer have the option of creating a separate assembly to house your custom code.

**Q: Has anyone else tried making an IL-rewriting profiler?**

Sure. If you want to learn from other people's experiences, read through the [Building Development and Diagnostic Tools for .NET Forum](https://social.msdn.microsoft.com/Forums/en-US/home?forum=netfxtoolsdev). Here are some interesting threads:

[http://social.msdn.microsoft.com/Forums/en-NZ/netfxtoolsdev/thread/5f30596b-e7b7-4b1f-b8e1-8172aa8dde31](http://social.msdn.microsoft.com/Forums/en-NZ/netfxtoolsdev/thread/5f30596b-e7b7-4b1f-b8e1-8172aa8dde31)
[http://social.msdn.microsoft.com/Forums/en-GB/netfxtoolsdev/thread/c352266f-ded3-4ee2-b2f9-fbeb41a70c27](http://social.msdn.microsoft.com/Forums/en-GB/netfxtoolsdev/thread/c352266f-ded3-4ee2-b2f9-fbeb41a70c27)



