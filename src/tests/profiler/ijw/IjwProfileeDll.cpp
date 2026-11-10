// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// C++/CLI (IJW) mixed-mode assembly used by the ijw profiler test.
//
// Repro shape for https://github.com/dotnet/runtime/issues/120151: a managed
// method calls a native helper 'call' which invokes the managed
// 'ManagedByPointerTarget' through a raw function pointer. Invoking a managed
// method by native function pointer goes through a reverse (unmanaged->managed)
// marshaling stub, and the profiler code transition callback for that stub used
// to report a bogus, non-null FunctionID.
//
// Compiled with default /clr per-function codegen (no #pragma managed/unmanaged);
// the reporter's sample used a file-static function, but a named function
// reproduces identically and lets the profiler match it by name.
__declspec(noinline) void ManagedByPointerTarget() {}
__declspec(noinline) static void call(void (*f)()) { f(); }

public ref class TestClass
{
public:
    // Managed -> native 'call' -> managed 'ManagedByPointerTarget' by pointer.
    int CallManagedFunctionByPointer()
    {
        call(ManagedByPointerTarget);
        return 100;
    }
};
