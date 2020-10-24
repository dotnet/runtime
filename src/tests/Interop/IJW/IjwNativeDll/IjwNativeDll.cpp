// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma unmanaged
int NativeFunction()
{
    return 100;
}

#pragma managed
public ref class TestClass
{
public:
    int ManagedEntryPoint()
    {
        return NativeFunction();
    }
};
