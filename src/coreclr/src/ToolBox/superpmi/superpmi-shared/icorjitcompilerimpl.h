//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#ifndef _ICorJitCompilerImpl
#define _ICorJitCompilerImpl

// ICorJitCompilerImpl: declare for implementation all the members of the ICorJitCompiler interface (which are
// specified as pure virtual methods). This is done once, here, and all implementations share it,
// to avoid duplicated declarations. This file is #include'd within all the ICorJitCompiler implementation
// classes.
//
// NOTE: this file is in exactly the same order, with exactly the same whitespace, as the ICorJitCompiler
// interface declaration (with the "virtual" and "= 0" syntax removed). This is to make it easy to compare
// against the interface declaration.

public:
// compileMethod is the main routine to ask the JIT Compiler to create native code for a method. The
// method to be compiled is passed in the 'info' parameter, and the code:ICorJitInfo is used to allow the
// JIT to resolve tokens, and make any other callbacks needed to create the code. nativeEntry, and
// nativeSizeOfCode are just for convenience because the JIT asks the EE for the memory to emit code into
// (see code:ICorJitInfo.allocMem), so really the EE already knows where the method starts and how big
// it is (in fact, it could be in more than one chunk).
//
// * In the 32 bit jit this is implemented by code:CILJit.compileMethod
// * For the 64 bit jit this is implemented by code:PreJit.compileMethod
//
// Note: Obfuscators that are hacking the JIT depend on this method having __stdcall calling convention
CorJitResult __stdcall compileMethod(ICorJitInfo*                comp,     /* IN */
                                     struct CORINFO_METHOD_INFO* info,     /* IN */
                                     unsigned /* code:CorJitFlag */ flags, /* IN */
                                     BYTE** nativeEntry,                   /* OUT */
                                     ULONG* nativeSizeOfCode               /* OUT */
                                     );

// Some JIT compilers (most notably Phoenix), cache information about EE structures from one invocation
// of the compiler to the next. This can be a problem when appdomains are unloaded, as some of this
// cached information becomes stale. The code:ICorJitCompiler.isCacheCleanupRequired is called by the EE
// early first to see if jit needs these notifications, and if so, the EE will call ClearCache is called
// whenever the compiler should abandon its cache (eg on appdomain unload)
void clearCache();
BOOL isCacheCleanupRequired();

// Do any appropriate work at process shutdown.  Default impl is to do nothing.
void ProcessShutdownWork(ICorStaticInfo* info); /* {}; */

// The EE asks the JIT for a "version identifier". This represents the version of the JIT/EE interface.
// If the JIT doesn't implement the same JIT/EE interface expected by the EE (because the JIT doesn't
// return the version identifier that the EE expects), then the EE fails to load the JIT.
//
void getVersionIdentifier(GUID* versionIdentifier /* OUT */
                          );

// When the EE loads the System.Numerics.Vectors assembly, it asks the JIT what length (in bytes) of
// SIMD vector it supports as an intrinsic type.  Zero means that the JIT does not support SIMD
// intrinsics, so the EE should use the default size (i.e. the size of the IL implementation).
unsigned getMaxIntrinsicSIMDVectorLength(CORJIT_FLAGS cpuCompileFlags); /* { return 0; } */

// IL obfuscators sometimes interpose on the EE-JIT interface. This function allows the VM to
// tell the JIT to use a particular ICorJitCompiler to implement the methods of this interface,
// and not to implement those methods itself. The JIT must not return this method when getJit()
// is called. Instead, it must pass along all calls to this interface from within its own
// ICorJitCompiler implementation. If 'realJitCompiler' is nullptr, then the JIT should resume
// executing all the functions itself.
void setRealJit(ICorJitCompiler* realJitCompiler); /* { } */

#endif
