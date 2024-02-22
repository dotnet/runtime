// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdint.h>

//
// This is the mechanism whereby multiple linked modules contribute their global data for initialization at
// startup of the application.
//
// ILC creates sections in the output obj file to mark the beginning and end of merged global data.
// It defines sentinel symbols that are used to get the addresses of the start and end of global data
// at runtime. The section names are platform-specific to match platform-specific linker conventions.
//
#if defined(_MSC_VER)

#pragma section(".modules$A", read)
#pragma section(".modules$Z", read)
extern "C" __declspec(allocate(".modules$A")) void * __modules_a[];
extern "C" __declspec(allocate(".modules$Z")) void * __modules_z[];

__declspec(allocate(".modules$A")) void * __modules_a[] = { nullptr };
__declspec(allocate(".modules$Z")) void * __modules_z[] = { nullptr };

//
// Each obj file compiled from managed code has a .modules$I section containing a pointer to its ReadyToRun
// data (which points at eager class constructors, frozen strings, etc).
//
// The #pragma ... /merge directive folds the book-end sections and all .modules$I sections from all input
// obj files into .rdata in alphabetical order.
//
#pragma comment(linker, "/merge:.modules=.rdata")

//
// Unboxing stubs need to be merged, folded and sorted. They are delimited by two special sections (.unbox$A
// and .unbox$Z). All unboxing stubs are in .unbox$M sections.
//
#pragma comment(linker, "/merge:.unbox=.text")

char _bookend_a;
char _bookend_z;

//
// Generate bookends for the managed code section.
// We give them unique bodies to prevent folding.
//

#pragma code_seg(".managedcode$A")
void* __managedcode_a() { return &_bookend_a; }
#pragma code_seg(".managedcode$Z")
void* __managedcode_z() { return &_bookend_z; }
#pragma code_seg()

//
// Generate bookends for the unboxing stub section.
// We give them unique bodies to prevent folding.
//

#pragma code_seg(".unbox$A")
void* __unbox_a() { return &_bookend_a; }
#pragma code_seg(".unbox$Z")
void* __unbox_z() { return &_bookend_z; }
#pragma code_seg()

#else // _MSC_VER

#if defined(__APPLE__)

extern void * __modules_a[] __asm("section$start$__DATA$__modules");
extern void * __modules_z[] __asm("section$end$__DATA$__modules");
extern char __managedcode_a __asm("section$start$__TEXT$__managedcode");
extern char __managedcode_z __asm("section$end$__TEXT$__managedcode");
extern char __unbox_a __asm("section$start$__TEXT$__unbox");
extern char __unbox_z __asm("section$end$__TEXT$__unbox");

#else // __APPLE__

extern "C" void * __start___modules[];
extern "C" void * __stop___modules[];
static void * (&__modules_a)[] = __start___modules;
static void * (&__modules_z)[] = __stop___modules;

extern "C" char __start___managedcode;
extern "C" char __stop___managedcode;
static char& __managedcode_a = __start___managedcode;
static char& __managedcode_z = __stop___managedcode;

extern "C" char __start___unbox;
extern "C" char __stop___unbox;
static char& __unbox_a = __start___unbox;
static char& __unbox_z = __stop___unbox;

#endif // __APPLE__

#endif // _MSC_VER

extern "C" bool RhInitialize(bool isDll);
extern "C" void RhSetRuntimeInitializationCallback(int (*fPtr)());

extern "C" bool RhRegisterOSModule(void * pModule,
    void * pvManagedCodeStartRange, uint32_t cbManagedCodeRange,
    void * pvUnboxingStubsStartRange, uint32_t cbUnboxingStubsRange,
    void ** pClasslibFunctions, uint32_t nClasslibFunctions);

extern "C" void* PalGetModuleHandleFromPointer(void* pointer);

#if defined(HOST_X86) && defined(HOST_WINDOWS)
#define STRINGIFY(s) #s
#define MANAGED_RUNTIME_EXPORT_ALTNAME(_method) STRINGIFY(/alternatename:_##_method=_method)
#define MANAGED_RUNTIME_EXPORT(_name) \
    __pragma(comment (linker, MANAGED_RUNTIME_EXPORT_ALTNAME(_name))) \
    extern "C" void __cdecl _name();
#define MANAGED_RUNTIME_EXPORT_NAME(_name) _name
#else
#define MANAGED_RUNTIME_EXPORT(_name) \
    extern "C" void __cdecl _name();
#define MANAGED_RUNTIME_EXPORT_NAME(_name) _name
#endif

MANAGED_RUNTIME_EXPORT(GetRuntimeException)
MANAGED_RUNTIME_EXPORT(RuntimeFailFast)
MANAGED_RUNTIME_EXPORT(AppendExceptionStackFrame)
MANAGED_RUNTIME_EXPORT(GetSystemArrayEEType)
MANAGED_RUNTIME_EXPORT(OnFirstChanceException)
MANAGED_RUNTIME_EXPORT(OnUnhandledException)
MANAGED_RUNTIME_EXPORT(IDynamicCastableIsInterfaceImplemented)
MANAGED_RUNTIME_EXPORT(IDynamicCastableGetInterfaceImplementation)
#ifdef FEATURE_OBJCMARSHAL
MANAGED_RUNTIME_EXPORT(ObjectiveCMarshalTryGetTaggedMemory)
MANAGED_RUNTIME_EXPORT(ObjectiveCMarshalGetIsTrackedReferenceCallback)
MANAGED_RUNTIME_EXPORT(ObjectiveCMarshalGetOnEnteredFinalizerQueueCallback)
MANAGED_RUNTIME_EXPORT(ObjectiveCMarshalGetUnhandledExceptionPropagationHandler)
#endif

typedef void (__cdecl *pfn)();

static const pfn c_classlibFunctions[] = {
    &MANAGED_RUNTIME_EXPORT_NAME(GetRuntimeException),
    &MANAGED_RUNTIME_EXPORT_NAME(RuntimeFailFast),
    nullptr, // &UnhandledExceptionHandler,
    &MANAGED_RUNTIME_EXPORT_NAME(AppendExceptionStackFrame),
    nullptr, // &CheckStaticClassConstruction,
    &MANAGED_RUNTIME_EXPORT_NAME(GetSystemArrayEEType),
    &MANAGED_RUNTIME_EXPORT_NAME(OnFirstChanceException),
    &MANAGED_RUNTIME_EXPORT_NAME(OnUnhandledException),
    &MANAGED_RUNTIME_EXPORT_NAME(IDynamicCastableIsInterfaceImplemented),
    &MANAGED_RUNTIME_EXPORT_NAME(IDynamicCastableGetInterfaceImplementation),
#ifdef FEATURE_OBJCMARSHAL
    &MANAGED_RUNTIME_EXPORT_NAME(ObjectiveCMarshalTryGetTaggedMemory),
    &MANAGED_RUNTIME_EXPORT_NAME(ObjectiveCMarshalGetIsTrackedReferenceCallback),
    &MANAGED_RUNTIME_EXPORT_NAME(ObjectiveCMarshalGetOnEnteredFinalizerQueueCallback),
    &MANAGED_RUNTIME_EXPORT_NAME(ObjectiveCMarshalGetUnhandledExceptionPropagationHandler),
#else
    nullptr,
    nullptr,
    nullptr,
    nullptr,
#endif
};

#ifndef _countof
#define _countof(_array) (sizeof(_array)/sizeof(_array[0]))
#endif

extern "C" void __cdecl InitializeModules(void* osModule, void ** modules, int count, void ** pClasslibFunctions, int nClasslibFunctions);

#ifndef NATIVEAOT_DLL
#define NATIVEAOT_ENTRYPOINT __managed__Main
#if defined(_WIN32)
extern "C" int __cdecl __managed__Main(int argc, wchar_t* argv[]);
#else
extern "C" int __cdecl __managed__Main(int argc, char* argv[]);
#endif
#else
#define NATIVEAOT_ENTRYPOINT __managed__Startup
extern "C" void __cdecl __managed__Startup();
#endif // !NATIVEAOT_DLL

static int InitializeRuntime()
{
    if (!RhInitialize(
#ifdef NATIVEAOT_DLL
        /* isDll */ true
#else
        /* isDll */ false
#endif
        ))
        return -1;

    void * osModule = PalGetModuleHandleFromPointer((void*)&NATIVEAOT_ENTRYPOINT);

    // TODO: pass struct with parameters instead of the large signature of RhRegisterOSModule
    if (!RhRegisterOSModule(
        osModule,
        (void*)&__managedcode_a, (uint32_t)((char *)&__managedcode_z - (char*)&__managedcode_a),
        (void*)&__unbox_a, (uint32_t)((char *)&__unbox_z - (char*)&__unbox_a),
        (void **)&c_classlibFunctions, _countof(c_classlibFunctions)))
    {
        return -1;
    }

    InitializeModules(osModule, __modules_a, (int)((__modules_z - __modules_a)), (void **)&c_classlibFunctions, _countof(c_classlibFunctions));

#ifdef NATIVEAOT_DLL
    // Run startup method immediately for a native library
    __managed__Startup();
#endif // NATIVEAOT_DLL

    return 0;
}

#ifndef NATIVEAOT_DLL

#if defined(_WIN32)
int __cdecl wmain(int argc, wchar_t* argv[])
#else
int main(int argc, char* argv[])
#endif
{
    int initval = InitializeRuntime();
    if (initval != 0)
        return initval;

    return __managed__Main(argc, argv);
}

#ifdef HAS_ADDRESS_SANITIZER
// We need to build the bootstrapper as a single object file, to ensure
// the linker can detect that we have ASAN components early enough in the build.
// Include our asan support sources for executable projects here to ensure they
// are compiled into the bootstrapper object.
#include "minipal/asansupport.cpp"
#endif // HAS_ADDRESS_SANITIZER

#endif // !NATIVEAOT_DLL

#ifdef NATIVEAOT_DLL
static struct InitializeRuntimePointerHelper
{
    InitializeRuntimePointerHelper()
    {
        RhSetRuntimeInitializationCallback(&InitializeRuntime);
    }
} initializeRuntimePointerHelper;
#endif // NATIVEAOT_DLL
