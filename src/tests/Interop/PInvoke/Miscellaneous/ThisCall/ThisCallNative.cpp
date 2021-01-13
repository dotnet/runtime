// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <xplatform.h>
#include <platformdefines.h>

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

    virtual SizeF GetSize()
    {
        return {width, height};
    }

    virtual Width GetWidth()
    {
        return {width};
    }

    virtual IntWrapper GetHeightAsInt()
    {
        return {(int)height};
    }

    virtual E GetE()
    {
        return dummy;
    }

    virtual long GetWidthAsLong()
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
    return c->GetSize();
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
