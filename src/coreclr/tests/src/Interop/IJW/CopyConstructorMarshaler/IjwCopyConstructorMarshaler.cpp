// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma managed
class A
{
    int copyCount;

public:
    A()
    :copyCount(0)
    {};

    A(A& other)
    :copyCount(other.copyCount + 1)
    {}

    int GetCopyCount()
    {
        return copyCount;
    }
};

class B : public A
{
    int bCopyCount;

public:
    B()
    :A(),
    bCopyCount(0)
    {};

    B(B& other)
    :A(other),
    bCopyCount(other.bCopyCount + 1)
    {}

    int GetBCopyCount()
    {
        return bCopyCount;
    }
};

int Managed_GetCopyCount(A a)
{
    return a.GetCopyCount();
}

int Managed_GetCopyCount(B b)
{
    return b.GetBCopyCount();
}

#pragma unmanaged

int GetCopyCount(A a)
{
    return a.GetCopyCount();
}

int GetCopyCount_ViaManaged(A a)
{
    return Managed_GetCopyCount(a);
}

int GetCopyCount(B b)
{
    return b.GetBCopyCount();
}

int GetCopyCount_ViaManaged(B b)
{
    return Managed_GetCopyCount(b);
}

#pragma managed

public ref class TestClass
{
public:
    int PInvokeNumCopies()
    {
        A a;
        return GetCopyCount(a);
    }

    int PInvokeNumCopiesDerivedType()
    {
        B b;
        return GetCopyCount(b);
    }

    int ReversePInvokeNumCopies()
    {
        A a;
        return GetCopyCount_ViaManaged(a);
    }

    int ReversePInvokeNumCopiesDerivedType()
    {
        B b;
        return GetCopyCount_ViaManaged(b);
    }
};
