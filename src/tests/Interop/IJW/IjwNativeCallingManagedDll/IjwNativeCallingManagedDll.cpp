// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include "platformdefines.h"

#pragma managed
int ManagedCallee();

#pragma unmanaged
int NativeFunction()
{
    return ManagedCallee();
}

extern "C" DLL_EXPORT int __cdecl NativeEntryPoint()
{
    return NativeFunction();
}

struct IntWrapper
{
    int Value;
};

#pragma managed
int64_t ManagedSum18ByRef(const int& a0,
    const IntWrapper& a1,
    const IntWrapper& a2,
    const IntWrapper& a3,
    const IntWrapper& a4,
    const IntWrapper& a5,
    const IntWrapper& a6,
    const IntWrapper& a7,
    const IntWrapper& a8,
    const IntWrapper& a9,
    const IntWrapper& a10,
    const IntWrapper& a11,
    const IntWrapper& a12,
    const IntWrapper& a13,
    const IntWrapper& a14,
    const IntWrapper& a15,
    const IntWrapper& a16,
    const int& a17);

#pragma unmanaged
int64_t NativeSum18ByRef()
{
    int a0 = 0;
    IntWrapper w[16];
    for (int i = 0; i < 16; ++i)
    {
        w[i].Value = i + 1;
    }
    int a17 = 17;
    return ManagedSum18ByRef(a0, w[0], w[1], w[2], w[3], w[4], w[5], w[6], w[7],
        w[8], w[9], w[10], w[11], w[12], w[13], w[14], w[15], a17);
}

#pragma managed

// Needed to provide a regression case for https://github.com/dotnet/runtime/issues/110365
[assembly:System::Diagnostics::DebuggableAttribute(true, true)];
[module:System::Diagnostics::DebuggableAttribute(true, true)];

public value struct ValueToReturnStorage
{
    int valueToReturn;
    bool valueSet;
};

// store the value to return in an appdomain local static variable to allow this test to be a regression test for https://github.com/dotnet/runtime/issues/110365
static __declspec(appdomain) ValueToReturnStorage s_valueToReturnStorage;

public ref class TestClass
{
public:
    int ManagedEntryPoint()
    {
        return NativeFunction();
    }

    int64_t ManagedEntryPointSum18ByRef()
    {
        return NativeSum18ByRef();
    }

    static void ChangeReturnedValue(int i)
    {
        s_valueToReturnStorage.valueToReturn = i;
        s_valueToReturnStorage.valueSet = true;
    }

    static int GetReturnValue()
    {
        if (s_valueToReturnStorage.valueSet)
            return s_valueToReturnStorage.valueToReturn;
        else
            return 100;
    }
};

int ManagedCallee()
{
    return TestClass::GetReturnValue();
}

int64_t ManagedSum18ByRef(const int& a0,
    const IntWrapper& a1,
    const IntWrapper& a2,
    const IntWrapper& a3,
    const IntWrapper& a4,
    const IntWrapper& a5,
    const IntWrapper& a6,
    const IntWrapper& a7,
    const IntWrapper& a8,
    const IntWrapper& a9,
    const IntWrapper& a10,
    const IntWrapper& a11,
    const IntWrapper& a12,
    const IntWrapper& a13,
    const IntWrapper& a14,
    const IntWrapper& a15,
    const IntWrapper& a16,
    const int& a17)
{
    return (int64_t)a0 + a1.Value + a2.Value + a3.Value + a4.Value + a5.Value + a6.Value + a7.Value + a8.Value + a9.Value
        + a10.Value + a11.Value + a12.Value + a13.Value + a14.Value + a15.Value + a16.Value + a17;
}
