*This blog post originally appeared on David Broman's blog on 10/17/2011*


# Overview

In this post, I write about the two primary kinds of IDs your profiler deals with, when each kind is appropriate to use, how to convert between those two types of IDs, and some gotchas with those conversions—particularly in how they may invoke the type loader.

# The two kinds of IDs

Profilers have to deal with two kinds of IDs.  The first kind are IDs from metadata, a.k.a., **metadata tokens**.  These are the mdToken values, like mdMethodDef or mdTypeDef, which are read straight out of the metadata of managed modules.  These values do not change for a given module from process to process.  They are placed in the module by the language compiler that generates the IL (e.g., csc.exe).  Profilers typically use metadata tokens in order to look up symbolic information from the metadata (e.g., for pretty-printing names of methods or classes), and for performing IL rewriting.  Metadata tokens are also fantastic for deferring symbolic lookup to a post-processing phase.  For example, a sampling profiler could log metadata tokens for classes and functions encountered on a sample at run-time and defer looking up the names of those classes and functions to a post-processing phase that occurs after the profiled process has exited.  This keeps the profiler’s data collection lightweight, and is only possible because metadata tokens don’t change so long as the managed modules defining those tokens don’t change.

The second kind of IDs are **run-time IDs** , such as FunctionID or ClassID which are defined in corprof.idl.  These values do change from process to process, and they represent internal data structures that the CLR builds up at run-time as it loads modules, loads types, JIT compiles functions, etc.  Profilers use these values as its main currency between ICorProfilerInfo\* and ICorProfilerCallback\* methods.  The CLR uses these values when it notifies profilers of various events (ICorProfilerCallback\* methods), and the profiler passes these values back into the CLR (ICorProfilerInfo\* methods) in order to get further information about them.  These IDs are handy because they are your profiler’s key to unlocking class layout, generated code, object addresses, and everything else that the CLR maintains about the actively executing managed code at run-time.  See [this post](Debugging - SOS and IDs.md) for more info about what these IDs really are.

# Converting between metadata tokens and run-time IDs

Since metadata tokens are good for some things and run-time IDs are good for others, you will inevitably find yourself in situations where you have one kind of ID handy, but you really need the other kind of ID.  Can you convert from one kind of ID to another?  Yes, but there are some caveats!

It’s always safe to go this direction: run-time ID –\> metadata token.  Just use methods such as GetFunctionInfo2 and GetClassIDInfo2, which take run-time IDs as input, and provide their module + metadata token as (part of) the output.

However, it is problematic going the opposite direction: metadata token –\> run-time ID.  Why?  Because a given type may not be loaded yet, and thus the run-time ID may not exist.  There exist methods on the ICorProfilerInfo\* interfaces that go this direction, namely GetFunctionFromToken(AndTypeArgs) and GetClassFromToken(AndTypeArgs).  However, they are dangerous to use (see below), and should be avoided.  Instead, it’s preferable that your profiler build up its own run-time ID –\> metadata token map as it encounters run-time IDs, and then performing reverse lookups in that map as necessary.  For example, as your profiler encounters ClassIDs via callbacks like ClassLoadFinished, it goes the “safe” direction (run-time ID –\> metadata token), to build up its map.  When it later encounters an mdTypeDef for a class, it checks to see if that mdTypeDef exists yet in its map—if so, your profiler uses that map to find the corresponding ClassID.  Safe and easy.

“Dave, stop telling us to do impossible things.  You know full well that profilers which attach to a process after it has started up don’t have the benefit of seeing all the ClassLoad\* notifications.  Also, if regular NGEN’d images are used, ClassLoad\* notifications are not reliably sent.”

True.  Though you will come across ClassIDs other ways.  Memory profilers will encounter ObjectIDs on the heap, and can call GetClassFromObject to start filling up its map of ClassIDs and thus mdTypeDefs.  Similarly, sampling profilers encounter FunctionIDs during stack walks, and can then get the ClassIDs containing those FunctionIDs and thus build up its map that way.

“You’re a dreamer, man.  There will still be cases where I have a metadata token, but have not yet encountered the ClassID.  Think about deep inspection of embedded structs!”

Yes, that is a good example.  You are an astute reader.  Memory profilers that wish to deeply inspect values of classes and structures on the heap need to know the ClassIDs in order to call GetClassLayout.  This works great when you’re dealing with reference types whose fields point to other reference types: as you bounce from object to object, you can take the ObjectID (i.e., the location in memory where the object starts), pass it to GetClassFromObject, and there’s your ClassID.  But what happens when a struct is embedded inside an object?  Sure, you can get the layout of the object, and determine the offset into the object where the embedded struct lives.  But then what?  How to inspect and report on the values of fields _inside the embedded struct_?  At this point, all you can get is the mdTypeDef for the struct (from the metadata of the containing class), but you may never have seen the ClassID for that struct.

“Told you so.”

# Going from metadata token to run-time ID

# 

# 

As I mentioned above, the safest way to do this is to build up your own map and do reverse-lookups as necessary.  If that scheme meets your needs, then by all means do that, and stop reading!  But in the cases where this is insufficient, you may need to resort to using GetFunctionFromToken(AndTypeArgs) and GetClassFromToken(AndTypeArgs).  There is no simple, foolproof way to use these APIs safely, but here is your guideline:

**Never call GetFunctionFromToken(AndTypeArgs) and GetClassFromToken(AndTypeArgs) unless you’re certain the relevant types have been loaded.**  (“Relevant types” include the ClassID containing the FunctionID whose mdMethodDef you pass to GetFunctionFromToken(AndTypeArgs), and the ClassID whose mdTypeDef you pass to GetClassFromToken(AndTypeArgs).)  If these types have not been loaded, _you may cause them to be loaded now_!  This is bad because:

- This is an easy way to crash the app.  Trying to load a type at the wrong time could cause cycles, causing infinite loops (depending on what your profiler does in response to class load notifications) or outright crashes.  For example, trying to load a type while its containing assembly is still in an early phase of loading is a great and fun way to crash the CLR. 
- You will impact the behavior of the app.  If you’re lucky enough not to crash the app, you’ve still impacted its behavior, by causing types to get loaded in a different order than they normally would.  Any impact to app behavior like this makes it difficult for your users to reproduce problems that they are trying to use your tool to diagnose, or may hide problems that they don’t discover until they run their application outside of your tool. 

## Determining whether a class was loaded

So how do you know a class has been fully loaded?

Unfortunately, receiving the **ClassLoadFinished** callback does not necessarily mean that ClassID has been fully loaded yet, as the MSDN [documentation](http://msdn.microsoft.com/en-us/library/ms230794.aspx) warns us.

Basically, the CLR type loader is one of the laziest things on this planet.  It doesn’t want to do anything unless it really, really has to.  The best guideline I can give you is this:  If the app is currently executing managed code that uses a type, then the type is loaded.  For example, if you do a stackwalk, and determine that the app is executing inside of

MyRetType MyClass::MyFunction(MyArgumentType myArgumentType)

then you can be reasonably assured that the following are loaded:

- MyClass 
- MyArgumentType (if it’s a value-type) 
- MyRetType (if it’s a value-type) 
- For any class you know is loaded, so should be: 
  - its base class 
  - its value-type fields (not necessarily reference-type fields!) 
  - implemented interfaces 
  - value-type generic type arguments (and even reference-type generic type arguments in the case of MyClass) 

So much for stacks.  What if you encounter an instance of a class on the heap?  Surely the class is loaded then, right?  Well, probably.  If you encounter an object on heap just after GC (inside **GarbageCollectionFinished** , before you return), it should be safe to inspect the class’s layout, and then peek through ObjectIDs to see the values of their fields.

But what if you encounter an object earlier than that?  For example, if you receive an **ObjectAllocated** callback, and call **GetClassFromObject** on the allocated ObjectID, can you be certain the ClassID has been fully loaded?  Well, usually.  But I have seen cases in the past, with types stored in NGENd images, where the CLR may issue an ObjectAllocated callback _just before_ the type has been fully loaded from the NGENd image.  I’ve recently tried to get this to happen again but couldn’t, which probably means this is rather unlikely, but not necessarily impossible.  Ugh.

In general, a lot of the uncertainty above comes from types stored in NGENd modules.  If we actually JIT-compile a function at run-time and load the types it uses from non-NGENd modules, then you can have much greater certainty about the above types being loaded.  You can even make further assumptions about locals and types from signatures of direct callees being loaded.

## Interlude: Remember the Unloads!

Now is a good time remind you that, not only is it dangerous to inspect run-time IDs too early (i.e., before they load); it’s also dangerous to inspect run-time IDs too late (i.e., after they **unload** ).  For example, if you store ClassIDs and FunctionIDs for later use, and use them “too late”, you can easily crash the CLR.  The profiling API does pretty much no validation of anything (in many cases, it’s incapable of doing so without using up significant amounts of memory to maintain lookup tables for everything).  So we generally take any run-time ID that you pass to ICorProfilerInfo\* methods, cast it to an internal CLR structure ptr, and go boom if the ID is bad.

There is no way to just ask the CLR if a FunctionID or ClassID is valid.  Indeed, classes could get unloaded, and new classes loaded, and your ClassID may now refer to a totally different (valid) class. 

You need to keep track of the unloads yourself.  You are notified when run-time IDs go out of scope (today, this happens at the level of an AppDomain unloading or a collectible assembly unloading—in both cases all IDs “contained” in the unloading thing are now invalid).  Once a run-time ID is out of scope, you are not allowed to pass that run-time ID back to the CLR.  In fact, you should consider whether thread synchronization will be necessary in your profiler to maintain this invariant.  For example, if a run-time ID gets unloaded on thread A, you’re still not allowed to pass that run-time ID back to the CLR on thread B.  So you may need to block on a critical section in thread A during the \*UnloadStarted / AppDomainShutdown\* callbacks, to prevent them from returning to the CLR until any uses of the contained IDs in thread B are finished.

Take a look at the [docs](http://msdn.microsoft.com/en-us/library/bb384619.aspx) is for more info.

# TypeRefs

So far I’ve been talking about how to go from a typeDef to its run-time ID, and by now that should seem hard enough that we don’t need to throw a monkey wrench into the works.  But the sad fact is we’re rarely lucky enough even to have a typeDef.  A class’s fields or even base type, might have their types defined in _other modules_, in which case the metadata tells us the fields or base type might actually be typeRefs, and not typeDefs.  Ugh.  Whaddya do with that?!

I’ll tell you what you _don’t_ do.  You don’t call the enticingly-named IMetaDataImport::ResolveTypeRef.  On the surface, it seems like ResolveTypeRef would do exactly what you want: starting from a typeRef, please find the referenced module and return an IMetaDataImport on that module, along with the typeDef in that target module to which the typeRef refers.  But the problem lies with how ResolveTypeRef determines the module to which a typeRef refers.

I think ResolveTypeRef was originally designed for use at build-time (by language compilers), though I don’t know if it’s even used in that scenario anymore.  It is certainly not good for use at run-time, where the loader’s decision on how to locate a referenced assembly can be arbitrarily complex.  Different AppDomains in the same process may have different rules on how to locate the referenced assembly due to varying permission sets, host settings, or assembly versions.  In the limit, the CLR may even _call into the user’s managed code_ to dynamically influence the decision of where the referenced assembly exists (see [AppDomain.AssemblyResolve Event](http://msdn.microsoft.com/en-us/library/system.appdomain.assemblyresolve.aspx)).

ResolveTypeRef doesn’t know about any of this—it was never designed to be used in a running application with all these environmental factors.  It has an extremely simple (and inaccurate) algorithm to iterate through a set of “known modules”, in an arbitrary order, looking for the first one that matches the reference.  What does “known modules” mean?  It’s a set of modules that have been opened into the metadata system, which is NOT the same as the list of modules already loaded by the assembly loader (and thus notified to your profiler).  And it’s certainly not the same as the set of modules installed onto the disk.

If you absolutely need to resolve refs to defs, your best bet may be to use your own algorithm which will be as accurate as you can make it, under the circumstances, and which will never try to locate a module that hasn’t been loaded yet.  That means that you shouldn’t try to resolve a ref to a def if that def hasn’t actually been loaded into a type by the CLR.  Consider using an algorithm similar to the following:

1. Get the AssemblyRef from the TypeRef to get to the name, public key token and version of the assembly where the type should reside. 
2. Enumerate all loaded modules that the Profiling API has notified you of (or via [EnumModules](http://msdn.microsoft.com/en-us/library/dd490890)) (you can filter out a specific AppDomain at this point if you want). 
3. In each enumerated module, search for a TypeDef with the same name and namespace as the TypeRef (IMetaDataImport::FindTypeDefByName) 
4. Pay attention to **type forwarding**!  Once you find the TypeDef, it may actually be an “exported” type, in which case you will need to follow the trail to the next module.  Read toward the bottom of [this post](Type Forwarding.md) for more info. 

The above can be a little bit smarter by paying attention to what order you choose to search through the modules:

- First search for the TypeDef in assemblies which exactly match the name, public key token and version for the AssemblyRef. 
- If that fails, then search through assemblies matching name and public key token (where the version is higher than the one supplied – this can happen for Framework assemblies). 
- If that fails, then search through all the other assemblies 

I must warn you that the above scheme is **not tested and not supported.  Use at your own risk!**

# Future

Although I cannot comment on what will or will not be in any particular future version of the CLR, I can tell you that it is clear to us on the CLR team that we have work to do, to make dealing with metadata tokens and their corresponding run-time type information easier from the profiling API.  After all, it doesn’t take a rocket scientist to read the above and conclude that it does take a rocket scientist to actually follow all this advice.  So for now, enjoy the fact that what you do is really hard, making you difficult to replace, and thus your job all the more secure.  You’re welcome.

 

Special thanks to David Wrighton and Karel Zikmund, who have helped considerably with all content in this entry around the type system and metadata.

