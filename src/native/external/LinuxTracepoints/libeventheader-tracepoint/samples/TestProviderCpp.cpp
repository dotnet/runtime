// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#include <eventheader/TraceLoggingProvider.h>

#define TestProvider TestProviderCpp
#include "TestProviderCommon.h"

static bool TestTraceLoggingLangSpecific()
{
    void const* const pSamplePtr = (void*)(intptr_t)(-12345);

    TraceLoggingWrite(TestProvider, "Value:bool",
        TraceLoggingValue(false, "false", "desc", 0x1234),
        TraceLoggingValue(true));
    TraceLoggingWrite(TestProvider, "Value:char",
        TraceLoggingValue('\0', "0"),
        TraceLoggingValue('A', "A"));
    TraceLoggingWrite(TestProvider, "Value:char16",
        TraceLoggingValue(u'\0', "0"),
        TraceLoggingValue(u'A', "A"));
    TraceLoggingWrite(TestProvider, "Value:char32",
        TraceLoggingValue(U'\0', "0"),
        TraceLoggingValue(U'A', "A"));
    TraceLoggingWrite(TestProvider, "Value:wchar",
        TraceLoggingValue(L'\0', "0"),
        TraceLoggingValue(L'A', "A"));
    TraceLoggingWrite(TestProvider, "Value:schar",
        TraceLoggingValue((signed char)0, "0"),
        TraceLoggingValue((signed char)L'A', "A"));
    TraceLoggingWrite(TestProvider, "Value:uchar",
        TraceLoggingValue((unsigned char)0, "0"),
        TraceLoggingValue((unsigned char)L'A', "A"));
    TraceLoggingWrite(TestProvider, "Value:sshort",
        TraceLoggingValue((signed short)0, "0"),
        TraceLoggingValue((signed short)L'A', "A"));
    TraceLoggingWrite(TestProvider, "Value:ushort",
        TraceLoggingValue((unsigned short)0, "0"),
        TraceLoggingValue((unsigned short)L'A', "A"));
    TraceLoggingWrite(TestProvider, "Value:sint",
        TraceLoggingValue((signed int)0, "0"),
        TraceLoggingValue((signed int)L'A', "A"));
    TraceLoggingWrite(TestProvider, "Value:uint",
        TraceLoggingValue((unsigned int)0, "0"),
        TraceLoggingValue((unsigned int)L'A', "A"));
    TraceLoggingWrite(TestProvider, "Value:slong",
        TraceLoggingValue((signed long)0, "0"),
        TraceLoggingValue((signed long)L'A', "A"));
    TraceLoggingWrite(TestProvider, "Value:ulong",
        TraceLoggingValue((unsigned long)0, "0"),
        TraceLoggingValue((unsigned long)L'A', "A"));
    TraceLoggingWrite(TestProvider, "Value:slonglong",
        TraceLoggingValue((signed long long)0, "0"),
        TraceLoggingValue((signed long long)L'A', "A"));
    TraceLoggingWrite(TestProvider, "Value:ulonglong",
        TraceLoggingValue((unsigned long long)0, "0"),
        TraceLoggingValue((unsigned long long)L'A', "A"));
    TraceLoggingWrite(TestProvider, "Value:float",
        TraceLoggingValue(0.0f, "0"),
        TraceLoggingValue(65.0f, "65"));
    TraceLoggingWrite(TestProvider, "Value:double",
        TraceLoggingValue(0.0, "0"),
        TraceLoggingValue(65.0, "65"));
    TraceLoggingWrite(TestProvider, "Value:void*",
        TraceLoggingValue((void*)0, "0"),
        TraceLoggingValue((void*)pSamplePtr, "p"));
    TraceLoggingWrite(TestProvider, "Value:cvoid*",
        TraceLoggingValue((void const*)0, "0"),
        TraceLoggingValue((void const*)pSamplePtr, "p"));
    TraceLoggingWrite(TestProvider, "Value:char*",
        TraceLoggingValue((char*)0, "0"),
        TraceLoggingValue((char*)"hello", "hello"));
    TraceLoggingWrite(TestProvider, "Value:cchar*",
        TraceLoggingValue((char const*)0, "0"),
        TraceLoggingValue((char const*)"hello", "hello"));
    TraceLoggingWrite(TestProvider, "Value:char16_t*",
        TraceLoggingValue((char16_t*)0, "0"),
        TraceLoggingValue((char16_t*)u"hello", "hello"));
    TraceLoggingWrite(TestProvider, "Value:cchar16_t*",
        TraceLoggingValue((char16_t const*)0, "0"),
        TraceLoggingValue((char16_t const*)u"hello", "hello"));
    TraceLoggingWrite(TestProvider, "Value:char32_t*",
        TraceLoggingValue((char32_t*)0, "0"),
        TraceLoggingValue((char32_t*)U"hello", "hello"));
    TraceLoggingWrite(TestProvider, "Value:cchar32_t*",
        TraceLoggingValue((char32_t const*)0, "0"),
        TraceLoggingValue((char32_t const*)U"hello", "hello"));
    TraceLoggingWrite(TestProvider, "Value:wchar_t*",
        TraceLoggingValue((wchar_t*)0, "0"),
        TraceLoggingValue((wchar_t*)L"hello", "hello"));
    TraceLoggingWrite(TestProvider, "Value:cwchar_t*",
        TraceLoggingValue((wchar_t const*)0, "0"),
        TraceLoggingValue((wchar_t const*)L"hello", "hello"));

    return true;
}

TRACELOGGING_DEFINE_PROVIDER(
    TestProviderCpp,
    "TestProviderCpp",
    // {3f3dc547-92d7-59d6-ed26-053336a36f9b}
    (0x3f3dc547, 0x92d7, 0x59d6, 0xed, 0x26, 0x05, 0x33, 0x36, 0xa3, 0x6f, 0x9b));

TRACELOGGING_DEFINE_PROVIDER(
    TestProviderCppG,
    "TestProviderCpp",
    // {3f3dc547-92d7-59d6-ed26-053336a36f9b}
    (0x3f3dc547, 0x92d7, 0x59d6, 0xed, 0x26, 0x05, 0x33, 0x36, 0xa3, 0x6f, 0x9b),
    TraceLoggingOptionGroupName("msft"));

#include <stdio.h>

bool TestCpp()
{
    printf("TestProvider Name: %s\n", TraceLoggingProviderName(TestProvider));

    int err = TraceLoggingRegister(TestProviderCpp);
    printf("TestProviderCpp register: %d\n", err);

    bool ok = TestCommon() && TestTraceLoggingLangSpecific();

    TraceLoggingUnregister(TestProviderCpp);
    return ok && err == 0;
}
