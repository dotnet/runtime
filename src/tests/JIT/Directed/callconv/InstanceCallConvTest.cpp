// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <platformdefines.h>

#ifndef INSTANCE_CALLCONV
#error The INSTANCE_CALLCONV define must be defined as the calling convention to use for the instance methods.
#endif

struct SizeF
{
    float width;
    float height;
};

struct Width
{
    float width;
};

struct IntWrapper
{
    int i;
};

enum E : unsigned int
{
    Value = 42
};

class C
{
    E dummy = E::Value;
    float width;
    float height;

public:
    C(float width, float height)
        :width(width),
        height(height)
    {}

    virtual SizeF INSTANCE_CALLCONV GetSize(int)
    {
        return {width, height};
    }

    virtual Width INSTANCE_CALLCONV GetWidth()
    {
        return {width};
    }

    virtual IntWrapper INSTANCE_CALLCONV GetHeightAsInt()
    {
        return {(int)height};
    }

    virtual E INSTANCE_CALLCONV GetE()
    {
        return dummy;
    }

    virtual long INSTANCE_CALLCONV GetWidthAsLong()
    {
        return (long)width;
    }
};

extern "C" DLL_EXPORT C* STDMETHODCALLTYPE CreateInstanceOfC(float width, float height)
{
    return new C(width, height);
}

extern "C" DLL_EXPORT SizeF STDMETHODCALLTYPE GetSizeFromManaged(C* c)
{
    return c->GetSize(9876);
}

extern "C" DLL_EXPORT Width STDMETHODCALLTYPE GetWidthFromManaged(C* c)
{
    return c->GetWidth();
}

extern "C" DLL_EXPORT IntWrapper STDMETHODCALLTYPE GetHeightAsIntFromManaged(C* c)
{
    return c->GetHeightAsInt();
}

extern "C" DLL_EXPORT E STDMETHODCALLTYPE GetEFromManaged(C* c)
{
    return c->GetE();
}

extern "C" DLL_EXPORT long STDMETHODCALLTYPE GetWidthAsLongFromManaged(C* c)
{
    return c->GetWidthAsLong();
}
