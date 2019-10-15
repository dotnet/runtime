*This blog post originally appeared on David Broman's blog on 10/10/2011*

## Is ReJIT For You?

If you’re writing a monitoring tool, typically run in production, and…

If your tool is always on, always monitoring, but needs a way to fine-tune the amount of instrumentation it does without forcing the monitored application to restart, and…

If your tool instruments potentially everything, including framework assemblies like mscorlib, and you therefore disable the use of NGENd images and are willing to put up with longer startup times as a result, then…

ReJIT may be for you.

## List those Limitations!

ReJIT + Attach? No!

In order to enable ReJIT, your profiler must load at startup, and set an immutable flag in your Initialize method that enables the ReJIT functionality.

ReJIT + NGEN? No!

In order to set that ReJIT flag I just mentioned, you must also set a new flag which completely disables the use of NGENd images.  Kind of similar to the existing COR\_PRF\_USE\_PROFILE\_IMAGES flag, except even NGEN /Profile images (should they exist) will be ignored, and everything will be JITted, when you set this new flag.  This includes all framework assemblies like mscorlib.

Metadata changes in ModuleLoadFinished only

If you add any new methods, AssemblyRefs, MemberRefs, etc., your metadata changes must be done during the module’s ModuleLoadFinished callback.  This is not a new limitation, but it could be a surprise to some that it’s still a limitation even with ReJIT.  There is an exception to this.  If you need to create a new LocalVarSigToken, you may do this “late”, at ReJIT time, rather than early at ModuleLoadFinished time.

Memory reclaimed at AppDomain unload, _not_ revert

ReJIT will include the ability to “revert” back to the original IL from the assembly.  Doing so, however, will not reclaim any memory that was allocated to support the ReJIT (e.g., the instrumented IL, the JITted code, internal bookkeeping, etc.)  This memory will be reclaimed when the containing AppDomain is unloaded.  And if the code is owned by the shared domain, then, well, that memory is never reclaimed.

ReJIT inlined functions?  No!

If function A inlines function B, then you cannot ReJIT function B.  Well, technically you can, you just won’t see the effect of that anytime A (or another inlining caller of B) is called.  The reason is that, even if you create your new, instrumented B’, A still inlined the original B.  So every time A is called, the code from the original B will be executed, and your B’ will be ignored.

Since your profiler must be loaded at startup, you can work around this by either turning off inlining altogether, or by monitoring it (via the JITInlining callback), so you know which callers to ReJIT.  In the example above, you’d have to rejit A, and could then request that the rejitted A not inline anyone, so that your new B’ would get called.  Note that you’d have to track the inliners recursively, as there can be arbitrarily many levels of inlining.

ReJIT + managed debugging? No!

While not technically disabled, it is not advised or supported to run a managed debugger against a process that also has a ReJITting profiler enabled.  (Native-only debugging is just fine, though.)

Whatever debugging support there is, is only there for you, the profiler writer, and _not_ for your profiler’s users.  For example, there is no way for the ReJITting profiler to adjust the instrumented IL map for rejitted code (i.e., no equivalent of SetILInstrumentedCodeMap for ReJIT).  And attempting to step into or set breakpoints in rejitted code will have unpredictable results.

However, as a profiler writer attempting to debug your own profiler, you should have a good experience debugging other parts of the process.  For example, if rejitted user code calls into an on-disk profiler IL assembly, you could set breakpoints in your profiler’s IL assembly, and step through that code.

ReJIT dynamic code?  No!

Not a new limitation, but just to be explicit, profilers are not allowed to instrument dynamic code generated via the Reflection.Emit namespace, and that includes ReJIT.

## Why so strict?

ReJIT, as originally conceived by the CLR team, involved allowing profilers to attach to running processes and then instrument arbitrary code at any time.  Just that one sentence would eliminate almost all the restrictions mentioned above.  So what happened?

Reality, that’s what.

Stuff takes time.  And in this case a _lot_ of time.  Lifting just about any of the above restrictions may well have increased development or testing time, or general risk, to the point where the entire ReJIT feature might have been jeopardized.  So although it was a painful process, we had to think hard about every sub-feature we wanted to support that had non-trivial cost to implement.  And at the same time, we had to think about actual, real-world, end-to-end scenarios that would be using ReJIT to ensure that we ended up with something that would be useful, if not perfect.

So we picked the real-world use-case of production monitoring tools that use instrumentation to gather data from various servers in a data center.  “Attach” isn’t interesting to many of these tools (which run all the time), but they do want to dynamically change the level of instrumentation to help diagnose problems as they come up, without having to restart the process.  This scenario fit very nicely with the time we had, and so that’s what we shot for.

I can’t comment on what we will or will not do in any releases of the CLR after 4.5, but I like to think that ReJIT in .NET 4.5 might simply be a first step toward a richer instrumentation feature set, such that some of these limitations may eventually get lifted.  We won’t know if that’s true until the time comes, though.

