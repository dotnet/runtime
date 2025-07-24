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
