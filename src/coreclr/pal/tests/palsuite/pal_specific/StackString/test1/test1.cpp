// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <new>
#include <palsuite.h>
#include <pal/stackstring.hpp>

template <class TString>
static void TestDefaultConstruction()
{
    alignas(TString) unsigned char storage[sizeof(TString)];
    // Nonzero storage makes the test independent of previous stack contents.
    memset(storage, 0xA5, sizeof(storage));
    TString* str = new (storage) TString;

    if (str->GetCount() != 0)
    {
        Fail("A default-constructed StackString must have a count of zero.");
    }
    if (!str->IsEmpty())
    {
        Fail("A default-constructed StackString must be empty.");
    }
    if (str->GetString()[0] != 0)
    {
        Fail("A default-constructed StackString must be null-terminated.");
    }

    str->~TString();
}

PALTEST(pal_specific_StackString_test1_paltest_stackstring_test1, "pal_specific/StackString/test1/paltest_stackstring_test1")
{
    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }

    TestDefaultConstruction<PathCharString>();
    TestDefaultConstruction<PathWCharString>();
    TestDefaultConstruction<StackString<0, CHAR>>();
    TestDefaultConstruction<StackString<0, WCHAR>>();

    PAL_Terminate();
    return PASS;
}
