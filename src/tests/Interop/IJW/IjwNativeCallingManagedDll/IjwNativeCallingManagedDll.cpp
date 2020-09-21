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
public ref class TestClass
{
private:
    static int s_valueToReturn = 100;
public:
    int ManagedEntryPoint()
    {
        return NativeFunction();
    }

    static void ChangeReturnedValue(int i)
    {
        s_valueToReturn = i;
    }

    static int GetReturnValue()
    {
        return s_valueToReturn;
    }
};

int ManagedCallee()
{
    return TestClass::GetReturnValue();
}
