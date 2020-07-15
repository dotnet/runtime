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

class C
{
    int dummy = 0xcccccccc;
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
};


extern "C" DLL_EXPORT C* STDMETHODCALLTYPE CreateInstanceOfC(float width, float height)
{
    return new C(width, height);
}
