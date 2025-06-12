// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#include <arpa/inet.h>
#include <limits.h>

#ifdef __cplusplus
extern "C" {
#endif

    bool TestC(void);
    bool TestCpp(void);

#ifdef __cplusplus
} // extern "C"
#endif

TRACELOGGING_DECLARE_PROVIDER(TestProviderC);
TRACELOGGING_DECLARE_PROVIDER(TestProviderCG);
TRACELOGGING_DECLARE_PROVIDER(TestProviderCpp);
TRACELOGGING_DECLARE_PROVIDER(TestProviderCppG);

static bool TestCommon(void)
{
    bool ok = true;

    // Validation of C/C++ provider interop.
    {
        bool enabled;

        ok = TraceLoggingRegister(TestProviderCG) == 0 && ok;
        ok = TraceLoggingRegister(TestProviderCppG) == 0 && ok;

        enabled = TraceLoggingProviderEnabled(TestProviderCG, 5, 0);
        TraceLoggingWrite(TestProviderCG, "EventCG", TraceLoggingBoolean(enabled));

        enabled = TraceLoggingProviderEnabled(TestProviderCppG, 5, 0);
        TraceLoggingWrite(TestProviderCppG, "EventCppG", TraceLoggingBoolean(enabled));

        TraceLoggingUnregister(TestProviderCG);
        TraceLoggingUnregister(TestProviderCppG);
        TraceLoggingUnregister(TestProviderCG);
        TraceLoggingUnregister(TestProviderCppG);

        enabled = TraceLoggingProviderEnabled(TestProviderCG, 5, 0);
        TraceLoggingWrite(TestProviderCG, "EventCG", TraceLoggingBoolean(enabled));

        enabled = TraceLoggingProviderEnabled(TestProviderCppG, 5, 0);
        TraceLoggingWrite(TestProviderCppG, "EventCppG", TraceLoggingBoolean(enabled));
    }

    const bool b0 = 0;
    const bool b1 = 1;
    const uint8_t b8 = 1;
    const int32_t b32 = 1;
    const int8_t i8 = 100;
    const uint8_t u8 = 200;
    const int16_t i16 = 30000;
    const uint16_t u16 = 60000;
    const int32_t i32 = 2000000000;
    const uint32_t u32 = 4000000000;
    const long iL = 2000000000;
    const unsigned long uL = 4000000000;
    const int64_t i64 = 9000000000000000000;
    const uint64_t u64 = 18000000000000000000u;
    const float f32 = 3.14f;
    const double f64 = 6.28;
    const char ch = 'A';
    const char16_t u16ch = u'A';
    const char32_t u32ch = U'A';
    const wchar_t wch = L'B';
    const intptr_t iptr = 1234;
    const uintptr_t uptr = 4321;
    char ch10[10] = "HowAreU8?";
    char16_t u16ch10[10] = u"HowAreU16";
    char32_t u32ch10[10] = U"HowAreU32";
    wchar_t wch10[10] = L"Goodbye!!";
    uint8_t const guid[16] = { 1,2,3,4,5,6,7,8,1,2,3,4,5,6,7,8};
    void const* const pSamplePtr = (void*)(intptr_t)(-12345);
    unsigned short n1 = 1;
    unsigned short n5 = 5;
    const uint16_t port80 = htons(80);

    const uint8_t ipv4data[] = { 127, 0, 0, 1 };
    in_addr_t ipv4;
    memcpy(&ipv4, ipv4data, sizeof(ipv4));

    const uint8_t ipv6data[] = { 1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16 };
    struct in6_addr ipv6;
    memcpy(&ipv6, ipv6data, sizeof(ipv6));

    TraceLoggingWrite(TestProvider, "CScalars1");
    TraceLoggingWrite(
        TestProvider,
        "CScalars2",
        TraceLoggingLevel(1),
        TraceLoggingKeyword(0xf123456789abcdef),
        TraceLoggingOpcode(2));
    TraceLoggingWrite(
        TestProvider,
        "CScalars3",
        TraceLoggingIdVersion(1000, 11),
        TraceLoggingLevel(2),
        TraceLoggingKeyword(0x80),
        TraceLoggingOpcode(3),
        TraceLoggingLevel(4),
        TraceLoggingKeyword(0x05),
        TraceLoggingEventTag(0x1234),
        TraceLoggingDescription("Hello"),
        TraceLoggingStruct(20, "Struct20", "Desc", 0xbeef),
            TraceLoggingString("hi", "hi", "Descr", 0x12),
            TraceLoggingCharArray(ch10, 3, "ch10", "Descri", 0x10));

    TraceLoggingWriteActivity(TestProvider, "Transfer00", NULL, NULL);
    TraceLoggingWriteActivity(TestProvider, "Transfer10", guid, NULL);
    TraceLoggingWriteActivity(TestProvider, "Transfer11", guid, guid);

    TraceLoggingWrite(TestProvider, "i8", TraceLoggingInt8(i8), TraceLoggingInt8(INT8_MIN));
    TraceLoggingWrite(TestProvider, "u8", TraceLoggingUInt8(u8), TraceLoggingUInt8(UINT8_MAX));
    TraceLoggingWrite(TestProvider, "i16", TraceLoggingInt16(i16), TraceLoggingInt16(INT16_MIN));
    TraceLoggingWrite(TestProvider, "u16", TraceLoggingUInt16(u16), TraceLoggingUInt16(UINT16_MAX));
    TraceLoggingWrite(TestProvider, "i32", TraceLoggingInt32(i32), TraceLoggingInt32(INT32_MIN));
    TraceLoggingWrite(TestProvider, "u32", TraceLoggingUInt32(u32), TraceLoggingUInt32(UINT32_MAX));
    TraceLoggingWrite(TestProvider, "i64", TraceLoggingInt64(i64), TraceLoggingInt64(INT64_MIN));
    TraceLoggingWrite(TestProvider, "u64", TraceLoggingUInt64(u64), TraceLoggingUInt64(UINT64_MAX));
    TraceLoggingWrite(TestProvider, "iptr", TraceLoggingIntPtr(iptr), TraceLoggingIntPtr(INTPTR_MIN));
    TraceLoggingWrite(TestProvider, "uptr", TraceLoggingUIntPtr(uptr), TraceLoggingUIntPtr(UINTPTR_MAX));
    TraceLoggingWrite(TestProvider, "iL", TraceLoggingLong(iL), TraceLoggingLong(LONG_MIN));
    TraceLoggingWrite(TestProvider, "uL", TraceLoggingULong(uL), TraceLoggingULong(ULONG_MAX));

    TraceLoggingWrite(TestProvider, "hi8",  TraceLoggingHexInt8(i8), TraceLoggingHexInt8(INT8_MIN));
    TraceLoggingWrite(TestProvider, "hu8",  TraceLoggingHexUInt8(u8), TraceLoggingHexUInt8(UINT8_MAX));
    TraceLoggingWrite(TestProvider, "hi16", TraceLoggingHexInt16(i16), TraceLoggingHexInt16(INT16_MIN));
    TraceLoggingWrite(TestProvider, "hu16", TraceLoggingHexUInt16(u16), TraceLoggingHexUInt16(UINT16_MAX));
    TraceLoggingWrite(TestProvider, "hi32", TraceLoggingHexInt32(i32), TraceLoggingHexInt32(INT32_MIN));
    TraceLoggingWrite(TestProvider, "hu32", TraceLoggingHexUInt32(u32), TraceLoggingHexUInt32(UINT32_MAX));
    TraceLoggingWrite(TestProvider, "hi64", TraceLoggingHexInt64(i64), TraceLoggingHexInt64(INT64_MIN));
    TraceLoggingWrite(TestProvider, "hu64", TraceLoggingHexUInt64(u64), TraceLoggingHexUInt64(UINT64_MAX));
    TraceLoggingWrite(TestProvider, "hiptr", TraceLoggingHexIntPtr(iptr), TraceLoggingHexIntPtr(INTPTR_MIN));
    TraceLoggingWrite(TestProvider, "huptr", TraceLoggingHexUIntPtr(uptr), TraceLoggingHexUIntPtr(UINTPTR_MAX));
    TraceLoggingWrite(TestProvider, "hiL", TraceLoggingHexLong(iL), TraceLoggingHexLong(LONG_MIN));
    TraceLoggingWrite(TestProvider, "huL", TraceLoggingHexULong(uL), TraceLoggingHexULong(ULONG_MAX));

    TraceLoggingWrite(TestProvider, "f32", TraceLoggingFloat32(f32), TraceLoggingFloat32(1.0f / 3.0f, "1/3"), TraceLoggingFloat32(0.0f / 0.0f, "NaN"), TraceLoggingFloat32(-1.0f / 0.0f, "-Inf"));
    TraceLoggingWrite(TestProvider, "f64", TraceLoggingFloat64(f64), TraceLoggingFloat64(1.0 / 3.0, "1/3"), TraceLoggingFloat64(0.0 / 0.0, "NaN"), TraceLoggingFloat64(-1.0 / 0.0, "-Inf"));
    TraceLoggingWrite(TestProvider, "b8", TraceLoggingBoolean(b0), TraceLoggingBoolean(b1), TraceLoggingBoolean(UINT8_MAX));
    TraceLoggingWrite(TestProvider, "b32", TraceLoggingBool(b0), TraceLoggingBool(b1), TraceLoggingBool(INT32_MIN));

    TraceLoggingWrite(TestProvider, "ch", TraceLoggingChar(ch), TraceLoggingChar('\xFE', "FE"));
    TraceLoggingWrite(TestProvider, "u16ch", TraceLoggingChar16(u16ch), TraceLoggingChar16(u'\xFFFE', "FFFE"));
    TraceLoggingWrite(TestProvider, "u32ch", TraceLoggingChar32(u32ch), TraceLoggingChar32(U'\xFFFE', "FFFE"), TraceLoggingChar32(U'\x10FFFF', "10FFFF"), TraceLoggingChar32(U'\xFFFEFDFC', "FFFEFDFC"));
    TraceLoggingWrite(TestProvider, "wch", TraceLoggingWChar(wch), TraceLoggingWChar(L'\xFFFE', "FFFE"), TraceLoggingWChar(L'\x10FFFF', "10FFFF"), TraceLoggingWChar(WCHAR_MAX));
    TraceLoggingWrite(TestProvider, "ptr", TraceLoggingPointer(pSamplePtr), TraceLoggingPointer((void*)UINTPTR_MAX, "UINTPTR_MAX"));
    TraceLoggingWrite(TestProvider, "pid", TraceLoggingPid(i32), TraceLoggingPid(INT32_MAX));
    TraceLoggingWrite(TestProvider, "port", TraceLoggingPort(port80), TraceLoggingPort(UINT16_MAX));
    TraceLoggingWrite(TestProvider, "errno", TraceLoggingErrno(INT32_MIN), TraceLoggingErrno(1), TraceLoggingErrno(131));
    TraceLoggingWrite(TestProvider, "t32", TraceLoggingTime32(i32), TraceLoggingTime32(INT32_MIN), TraceLoggingTime32(INT32_MAX), TraceLoggingTime32(0));
    TraceLoggingWrite(TestProvider, "t64", TraceLoggingTime64(i64), TraceLoggingTime64(INT64_MIN), TraceLoggingTime64(INT64_MAX),
        TraceLoggingTime64(0),
        TraceLoggingTime64(-11644473600),
        TraceLoggingTime64(-11644473601),
        TraceLoggingTime64(910692730085),
        TraceLoggingTime64(910692730086));

    TraceLoggingWrite(TestProvider, "guid", TraceLoggingGuid(guid));

    TraceLoggingWrite(TestProvider, "sz",
        TraceLoggingString(NULL, "NULL"),
        TraceLoggingString(ch10));
    TraceLoggingWrite(TestProvider, "sz8",
        TraceLoggingUtf8String(NULL, "NULL"),
        TraceLoggingUtf8String(ch10));
    TraceLoggingWrite(TestProvider, "wsz",
        TraceLoggingWideString(NULL, "NULL"),
        TraceLoggingWideString(wch10));
    TraceLoggingWrite(TestProvider, "sz16",
        TraceLoggingString16(NULL, "NULL"),
        TraceLoggingString16(u16ch10));
    TraceLoggingWrite(TestProvider, "sz32",
        TraceLoggingString32(NULL, "NULL"),
        TraceLoggingString32(u32ch10));

    TraceLoggingWrite(TestProvider, "csz",
        TraceLoggingCountedString(NULL, 0, "NULL"),
        TraceLoggingCountedString(ch10, 5));
    TraceLoggingWrite(TestProvider, "csz8",
        TraceLoggingCountedUtf8String(NULL, 0, "NULL"),
        TraceLoggingCountedUtf8String(ch10, 5));
    TraceLoggingWrite(TestProvider, "cwsz",
        TraceLoggingCountedWideString(NULL, 0, "NULL"),
        TraceLoggingCountedWideString(wch10, 5));
    TraceLoggingWrite(TestProvider, "csz16",
        TraceLoggingCountedString16(NULL, 0, "NULL"),
        TraceLoggingCountedString16(u16ch10, 5));
    TraceLoggingWrite(TestProvider, "csz32",
        TraceLoggingCountedString32(NULL, 0, "NULL"),
        TraceLoggingCountedString32(u32ch10, 5));

    TraceLoggingWrite(TestProvider, "bin",
        TraceLoggingBinary(NULL, 0, "NULL"),
        TraceLoggingBinary(ch10, 5));

    TraceLoggingWrite(TestProvider, "ipV4",
        TraceLoggingIPv4Address(ipv4),
        TraceLoggingIPv4Address(UINT32_MAX));
    TraceLoggingWrite(TestProvider, "ipV6",
        TraceLoggingIPv6Address(&ipv6, "ipv6"),
        TraceLoggingIPv6Address("\xfe\xfe\xfe\xfe\xfe\xfe\xfe\xfe\xfe\xfe\xfe\xfe\xfe\xfe\xfe", "fefe..fe00"));

    TraceLoggingWrite(TestProvider, "ai8",
        TraceLoggingInt8FixedArray(&i8, 1, "a1"),
        TraceLoggingInt8Array(&i8, n1, "s"));
    TraceLoggingWrite(TestProvider, "au8",
        TraceLoggingUInt8FixedArray(&u8, 1, "a1"),
        TraceLoggingUInt8Array(&u8, n1, "s"));
    TraceLoggingWrite(TestProvider, "ai16",
        TraceLoggingInt16FixedArray(&i16, 1, "a1"),
        TraceLoggingInt16Array(&i16, n1, "s"));
    TraceLoggingWrite(TestProvider, "au16",
        TraceLoggingUInt16FixedArray(&u16, 1, "a1"),
        TraceLoggingUInt16Array(&u16, n1, "s"));
    TraceLoggingWrite(TestProvider, "ai32",
        TraceLoggingInt32FixedArray(&i32, 1, "a1"),
        TraceLoggingInt32Array(&i32, n1, "s"));
    TraceLoggingWrite(TestProvider, "au32",
        TraceLoggingUInt32FixedArray(&u32, 1, "a1"),
        TraceLoggingUInt32Array(&u32, n1, "s"));
    TraceLoggingWrite(TestProvider, "ai64",
        TraceLoggingInt64FixedArray(&i64, 1, "a1"),
        TraceLoggingInt64Array(&i64, n1, "s"));
    TraceLoggingWrite(TestProvider, "au64",
        TraceLoggingUInt64FixedArray(&u64, 1, "a1"),
        TraceLoggingUInt64Array(&u64, n1, "s"));
    TraceLoggingWrite(TestProvider, "aiptr",
        TraceLoggingIntPtrFixedArray(&iptr, 1, "a1"),
        TraceLoggingIntPtrArray(&iptr, n1, "s"));
    TraceLoggingWrite(TestProvider, "auptr",
        TraceLoggingUIntPtrFixedArray(&uptr, 1, "a1"),
        TraceLoggingUIntPtrArray(&uptr, n1, "s"));
    TraceLoggingWrite(TestProvider, "aiL",
        TraceLoggingLongFixedArray(&iL, 1, "a1"),
        TraceLoggingLongArray(&iL, n1, "s"));
    TraceLoggingWrite(TestProvider, "auL",
        TraceLoggingULongFixedArray(&uL, 1, "a1"),
        TraceLoggingULongArray(&uL, n1, "s"));

    TraceLoggingWrite(TestProvider, "hai8",
        TraceLoggingHexInt8FixedArray(&i8, 1, "a1"),
        TraceLoggingHexInt8Array(&i8, n1, "s"));
    TraceLoggingWrite(TestProvider, "hau8",
        TraceLoggingHexUInt8FixedArray(&u8, 1, "a1"),
        TraceLoggingHexUInt8Array(&u8, n1, "s"));
    TraceLoggingWrite(TestProvider, "hai16",
        TraceLoggingHexInt16FixedArray(&i16, 1, "a1"),
        TraceLoggingHexInt16Array(&i16, n1, "s"));
    TraceLoggingWrite(TestProvider, "hau16",
        TraceLoggingHexUInt16FixedArray(&u16, 1, "a1"),
        TraceLoggingHexUInt16Array(&u16, n1, "s"));
    TraceLoggingWrite(TestProvider, "hai32",
        TraceLoggingHexInt32FixedArray(&i32, 1, "a1"),
        TraceLoggingHexInt32Array(&i32, n1, "s"));
    TraceLoggingWrite(TestProvider, "hau32",
        TraceLoggingHexUInt32FixedArray(&u32, 1, "a1"),
        TraceLoggingHexUInt32Array(&u32, n1, "s"));
    TraceLoggingWrite(TestProvider, "hai64",
        TraceLoggingHexInt64FixedArray(&i64, 1, "a1"),
        TraceLoggingHexInt64Array(&i64, n1, "s"));
    TraceLoggingWrite(TestProvider, "hau64",
        TraceLoggingHexUInt64FixedArray(&u64, 1, "a1"),
        TraceLoggingHexUInt64Array(&u64, n1, "s"));
    TraceLoggingWrite(TestProvider, "haiptr",
        TraceLoggingHexIntPtrFixedArray(&iptr, 1, "a1"),
        TraceLoggingHexIntPtrArray(&iptr, n1, "s"));
    TraceLoggingWrite(TestProvider, "hauptr",
        TraceLoggingHexUIntPtrFixedArray(&uptr, 1, "a1"),
        TraceLoggingHexUIntPtrArray(&uptr, n1, "s"));
    TraceLoggingWrite(TestProvider, "haiL",
        TraceLoggingHexLongFixedArray(&iL, 1, "a1"),
        TraceLoggingHexLongArray(&iL, n1, "s"));
    TraceLoggingWrite(TestProvider, "hauL",
        TraceLoggingHexULongFixedArray(&uL, 1, "a1"),
        TraceLoggingHexULongArray(&uL, n1, "s"));

    TraceLoggingWrite(TestProvider, "af32",
        TraceLoggingFloat32FixedArray(&f32, 1, "a1"),
        TraceLoggingFloat32Array(&f32, n1, "s"));
    TraceLoggingWrite(TestProvider, "af64",
        TraceLoggingFloat64FixedArray(&f64, 1, "a1"),
        TraceLoggingFloat64Array(&f64, n1, "s"));
    TraceLoggingWrite(TestProvider, "ab8",
        TraceLoggingBooleanFixedArray(&b8, 1, "a1"),
        TraceLoggingBooleanArray(&b8, n1, "s"));
    TraceLoggingWrite(TestProvider, "ab32",
        TraceLoggingBoolFixedArray(&b32, 1, "a1"),
        TraceLoggingBoolArray(&b32, n1, "s"));

    TraceLoggingWrite(TestProvider, "ach",
        TraceLoggingCharFixedArray(ch10, 4, "a4"),
        TraceLoggingCharArray(ch10, n5, "s5"));
    TraceLoggingWrite(TestProvider, "ach16",
        TraceLoggingChar16FixedArray(u16ch10, 4, "a4"),
        TraceLoggingChar16Array(u16ch10, n5, "s5"));
    TraceLoggingWrite(TestProvider, "ach32",
        TraceLoggingChar32FixedArray(u32ch10, 4, "a4"),
        TraceLoggingChar32Array(u32ch10, n5, "s5"));
    TraceLoggingWrite(TestProvider, "awch",
        TraceLoggingWCharFixedArray(wch10, 4, "a4"),
        TraceLoggingWCharArray(wch10, n5, "s5"));

    TraceLoggingWrite(TestProvider, "aptr",
        TraceLoggingPointerFixedArray(&pSamplePtr, 1, "a1"),
        TraceLoggingPointerArray(&pSamplePtr, n1, "s"));
    TraceLoggingWrite(TestProvider, "apid",
        TraceLoggingPidFixedArray(&i32, 1, "a1"),
        TraceLoggingPidArray(&i32, n1, "s"));
    TraceLoggingWrite(TestProvider, "aport",
        TraceLoggingPortFixedArray(&u16, 1, "a1"),
        TraceLoggingPortArray(&u16, n1, "s"));
    TraceLoggingWrite(TestProvider, "aerrno",
        TraceLoggingErrnoFixedArray(&i32, 1, "a1"),
        TraceLoggingErrnoArray(&i32, n1, "s"));
    TraceLoggingWrite(TestProvider, "aft",
        TraceLoggingTime32FixedArray(&i32, 1, "a1"),
        TraceLoggingTime32Array(&i32, n1, "s"));
    TraceLoggingWrite(TestProvider, "auft",
        TraceLoggingTime64FixedArray(&i64, 1, "a1"),
        TraceLoggingTime64Array(&i64, n1, "s"));

    TraceLoggingWrite(TestProvider, "ag",
        TraceLoggingPackedMetadataEx(event_field_encoding_value128 | event_field_encoding_varray_flag, event_field_format_uuid, "s0"),
        TraceLoggingPackedData(u"\x0", 2), // 0 elements in array
        TraceLoggingPackedMetadataEx(event_field_encoding_value128 | event_field_encoding_varray_flag, event_field_format_uuid, "s1"),
        TraceLoggingPackedData(u"\x1", 2), // 1 element in array
        TraceLoggingPackedData(guid, sizeof(guid)));

    TraceLoggingWrite(TestProvider, "ahexbytes",
        TraceLoggingPackedMetadataEx(event_field_encoding_value128 | event_field_encoding_varray_flag, event_field_format_hex_bytes, "s0"),
        TraceLoggingPackedData(u"\x0", 2), // 0 elements in array
        TraceLoggingPackedMetadataEx(event_field_encoding_value128 | event_field_encoding_varray_flag, event_field_format_hex_bytes, "s1"),
        TraceLoggingPackedData(u"\x1", 2), // 1 element in array
        TraceLoggingPackedData(guid, sizeof(guid)));

    struct LChar8  { uint16_t x; uint16_t l; char     c[10]; };
    struct LChar16 { uint16_t x; uint16_t l; char16_t c[10]; };
    struct LChar32 { uint16_t x; uint16_t l; char32_t c[10]; };

    static struct LChar8 const lchar8 = { 0, 4, { 'h','j','k','l' } };
    static struct LChar16 const lchar16 = { 0, 4, { 'h','j','k','l' } };
    static struct LChar32 const lchar32 = { 0, 4, { 'h','j','k','l' } };
    TraceLoggingWrite(TestProvider, "Default",
        TraceLoggingPackedField(&u8, 1, event_field_encoding_value8, "V8"),
        TraceLoggingPackedField(&u16, 2, event_field_encoding_value16, "V16"),
        TraceLoggingPackedField(&u32, 4, event_field_encoding_value32, "V32"),
        TraceLoggingPackedField(&u64, 8, event_field_encoding_value64, "V64"),
        TraceLoggingPackedField(&guid, 16, event_field_encoding_value128, "V128"),
        TraceLoggingPackedField(lchar8.c, 5, event_field_encoding_zstring_char8, "NChar8"),
        TraceLoggingPackedField(lchar16.c, 10, event_field_encoding_zstring_char16, "NChar16"),
        TraceLoggingPackedField(lchar32.c, 20, event_field_encoding_zstring_char32, "NChar32"),
        TraceLoggingPackedField(&lchar8.l, 6, event_field_encoding_string_length16_char8, "LChar8"),
        TraceLoggingPackedField(&lchar16.l, 10, event_field_encoding_string_length16_char16, "LChar16"),
        TraceLoggingPackedField(&lchar32.l, 18, event_field_encoding_string_length16_char32, "LChar32"));

    TraceLoggingWrite(TestProvider, "HexBytes",
        TraceLoggingPackedFieldEx(&guid, 1, event_field_encoding_value8, event_field_format_hex_bytes, "V8"),
        TraceLoggingPackedFieldEx(&guid, 2, event_field_encoding_value16, event_field_format_hex_bytes, "V16"),
        TraceLoggingPackedFieldEx(&guid, 4, event_field_encoding_value32, event_field_format_hex_bytes, "V32"),
        TraceLoggingPackedFieldEx(&guid, 8, event_field_encoding_value64, event_field_format_hex_bytes, "V64"),
        TraceLoggingPackedFieldEx(&guid, 16, event_field_encoding_value128, event_field_format_hex_bytes, "V128"),
        TraceLoggingPackedFieldEx(lchar8.c, 5, event_field_encoding_zstring_char8, event_field_format_hex_bytes, "NChar8"),
        TraceLoggingPackedFieldEx(lchar16.c, 10, event_field_encoding_zstring_char16, event_field_format_hex_bytes, "NChar16"),
        TraceLoggingPackedFieldEx(lchar32.c, 20, event_field_encoding_zstring_char32, event_field_format_hex_bytes, "NChar32"),
        TraceLoggingPackedFieldEx(&lchar8.l, 6, event_field_encoding_string_length16_char8, event_field_format_hex_bytes, "LChar8"),
        TraceLoggingPackedFieldEx(&lchar16.l, 10, event_field_encoding_string_length16_char16, event_field_format_hex_bytes, "LChar16"),
        TraceLoggingPackedFieldEx(&lchar32.l, 18, event_field_encoding_string_length16_char32, event_field_format_hex_bytes, "LChar32"));

    uint16_t false16 = 0;
    uint16_t true16 = 1;
    TraceLoggingWrite(TestProvider, "Bool16",
        TraceLoggingPackedFieldEx(&false16, 2, event_field_encoding_value16, event_field_format_boolean, "false16"),
        TraceLoggingPackedFieldEx(&true16, 2, event_field_encoding_value16, event_field_format_boolean, "true16"),
        TraceLoggingPackedFieldEx(&u16, 2, event_field_encoding_value16, event_field_format_boolean, "u16"),
        TraceLoggingPackedFieldEx(&i16, 2, event_field_encoding_value16, event_field_format_boolean, "i16"));

    TraceLoggingWrite(TestProvider, "StringUtf",
        TraceLoggingPackedFieldEx(lchar8.c, 5, event_field_encoding_zstring_char8, event_field_format_string_utf, "NChar8"),
        TraceLoggingPackedFieldEx(lchar16.c, 10, event_field_encoding_zstring_char16, event_field_format_string_utf, "NChar16"),
        TraceLoggingPackedFieldEx(lchar32.c, 20, event_field_encoding_zstring_char32, event_field_format_string_utf, "NChar32"),
        TraceLoggingPackedFieldEx(&lchar8.l, 6, event_field_encoding_string_length16_char8, event_field_format_string_utf, "LChar8"),
        TraceLoggingPackedFieldEx(&lchar16.l, 10, event_field_encoding_string_length16_char16, event_field_format_string_utf, "LChar16"),
        TraceLoggingPackedFieldEx(&lchar32.l, 18, event_field_encoding_string_length16_char32, event_field_format_string_utf, "LChar32"));

    TraceLoggingWrite(TestProvider, "StringUtfBom-NoBom",
        TraceLoggingPackedFieldEx(lchar8.c, 5, event_field_encoding_zstring_char8, event_field_format_string_utf_bom, "NChar8"),
        TraceLoggingPackedFieldEx(lchar16.c, 10, event_field_encoding_zstring_char16, event_field_format_string_utf_bom, "NChar16"),
        TraceLoggingPackedFieldEx(lchar32.c, 20, event_field_encoding_zstring_char32, event_field_format_string_utf_bom, "NChar32"),
        TraceLoggingPackedFieldEx(&lchar8.l, 6, event_field_encoding_string_length16_char8, event_field_format_string_utf_bom, "LChar8"),
        TraceLoggingPackedFieldEx(&lchar16.l, 10, event_field_encoding_string_length16_char16, event_field_format_string_utf_bom, "LChar16"),
        TraceLoggingPackedFieldEx(&lchar32.l, 18, event_field_encoding_string_length16_char32, event_field_format_string_utf_bom, "LChar32"));
    TraceLoggingWrite(TestProvider, "StringXml-NoBom",
        TraceLoggingPackedFieldEx(lchar8.c, 5, event_field_encoding_zstring_char8, event_field_format_string_xml, "NChar8"),
        TraceLoggingPackedFieldEx(lchar16.c, 10, event_field_encoding_zstring_char16, event_field_format_string_xml, "NChar16"),
        TraceLoggingPackedFieldEx(lchar32.c, 20, event_field_encoding_zstring_char32, event_field_format_string_xml, "NChar32"),
        TraceLoggingPackedFieldEx(&lchar8.l, 6, event_field_encoding_string_length16_char8, event_field_format_string_xml, "LChar8"),
        TraceLoggingPackedFieldEx(&lchar16.l, 10, event_field_encoding_string_length16_char16, event_field_format_string_xml, "LChar16"),
        TraceLoggingPackedFieldEx(&lchar32.l, 18, event_field_encoding_string_length16_char32, event_field_format_string_xml, "LChar32"));
    TraceLoggingWrite(TestProvider, "StringJson-NoBom",
        TraceLoggingPackedFieldEx(lchar8.c, 5, event_field_encoding_zstring_char8, event_field_format_string_json, "NChar8"),
        TraceLoggingPackedFieldEx(lchar16.c, 10, event_field_encoding_zstring_char16, event_field_format_string_json, "NChar16"),
        TraceLoggingPackedFieldEx(lchar32.c, 20, event_field_encoding_zstring_char32, event_field_format_string_json, "NChar32"),
        TraceLoggingPackedFieldEx(&lchar8.l, 6, event_field_encoding_string_length16_char8, event_field_format_string_json, "LChar8"),
        TraceLoggingPackedFieldEx(&lchar16.l, 10, event_field_encoding_string_length16_char16, event_field_format_string_json, "LChar16"),
        TraceLoggingPackedFieldEx(&lchar32.l, 18, event_field_encoding_string_length16_char32, event_field_format_string_json, "LChar32"));

    static struct LChar8 const lcharBom8 = { 0, 7, { '\xEF', '\xBB', '\xBF', 'h','j','k','l' } };
    static struct LChar16 const lcharBom16 = { 0, 5, { 0xFEFF, 'h','j','k','l' } };
    static struct LChar32 const lcharBom32 = { 0, 5, { 0xFEFF, 'h','j','k','l' } };
    TraceLoggingWrite(TestProvider, "StringUtfBom-Bom",
        TraceLoggingPackedFieldEx(lcharBom8.c, 8, event_field_encoding_zstring_char8, event_field_format_string_utf_bom, "NChar8"),
        TraceLoggingPackedFieldEx(lcharBom16.c, 12, event_field_encoding_zstring_char16, event_field_format_string_utf_bom, "NChar16"),
        TraceLoggingPackedFieldEx(lcharBom32.c, 24, event_field_encoding_zstring_char32, event_field_format_string_utf_bom, "NChar32"),
        TraceLoggingPackedFieldEx(&lcharBom8.l, 9, event_field_encoding_string_length16_char8, event_field_format_string_utf_bom, "LChar8"),
        TraceLoggingPackedFieldEx(&lcharBom16.l, 12, event_field_encoding_string_length16_char16, event_field_format_string_utf_bom, "LChar16"),
        TraceLoggingPackedFieldEx(&lcharBom32.l, 22, event_field_encoding_string_length16_char32, event_field_format_string_utf_bom, "LChar32"));
    TraceLoggingWrite(TestProvider, "StringXml-Bom",
        TraceLoggingPackedFieldEx(lcharBom8.c, 8, event_field_encoding_zstring_char8, event_field_format_string_xml, "NChar8"),
        TraceLoggingPackedFieldEx(lcharBom16.c, 12, event_field_encoding_zstring_char16, event_field_format_string_xml, "NChar16"),
        TraceLoggingPackedFieldEx(lcharBom32.c, 24, event_field_encoding_zstring_char32, event_field_format_string_xml, "NChar32"),
        TraceLoggingPackedFieldEx(&lcharBom8.l, 9, event_field_encoding_string_length16_char8, event_field_format_string_xml, "LChar8"),
        TraceLoggingPackedFieldEx(&lcharBom16.l, 12, event_field_encoding_string_length16_char16, event_field_format_string_xml, "LChar16"),
        TraceLoggingPackedFieldEx(&lcharBom32.l, 22, event_field_encoding_string_length16_char32, event_field_format_string_xml, "LChar32"));
    TraceLoggingWrite(TestProvider, "StringJson-Bom",
        TraceLoggingPackedFieldEx(lcharBom8.c, 8, event_field_encoding_zstring_char8, event_field_format_string_json, "NChar8"),
        TraceLoggingPackedFieldEx(lcharBom16.c, 12, event_field_encoding_zstring_char16, event_field_format_string_json, "NChar16"),
        TraceLoggingPackedFieldEx(lcharBom32.c, 24, event_field_encoding_zstring_char32, event_field_format_string_json, "NChar32"),
        TraceLoggingPackedFieldEx(&lcharBom8.l, 9, event_field_encoding_string_length16_char8, event_field_format_string_json, "LChar8"),
        TraceLoggingPackedFieldEx(&lcharBom16.l, 12, event_field_encoding_string_length16_char16, event_field_format_string_json, "LChar16"),
        TraceLoggingPackedFieldEx(&lcharBom32.l, 22, event_field_encoding_string_length16_char32, event_field_format_string_json, "LChar32"));

    static struct LChar8 const lcharXBom8 = { 0, 7, { '\xEF', '\xBB', '\xBF', 'h','j','k','l' } };
    static struct LChar16 const lcharXBom16 = { 0, 5, { 0xFFFE, 0x6800, 0x6a00, 0x6b00, 0x6c00 } };
    static struct LChar32 const lcharXBom32 = { 0, 5, { 0xFFFE0000, 0x68000000, 0x6a000000, 0x6b000000, 0x6c000000 } };
    TraceLoggingWrite(TestProvider, "StringUtfBom-XBom",
        TraceLoggingPackedFieldEx(lcharXBom8.c, 8, event_field_encoding_zstring_char8, event_field_format_string_utf_bom, "NChar8"),
        TraceLoggingPackedFieldEx(lcharXBom16.c, 12, event_field_encoding_zstring_char16, event_field_format_string_utf_bom, "NChar16"),
        TraceLoggingPackedFieldEx(lcharXBom32.c, 24, event_field_encoding_zstring_char32, event_field_format_string_utf_bom, "NChar32"),
        TraceLoggingPackedFieldEx(&lcharXBom8.l, 9, event_field_encoding_string_length16_char8, event_field_format_string_utf_bom, "LChar8"),
        TraceLoggingPackedFieldEx(&lcharXBom16.l, 12, event_field_encoding_string_length16_char16, event_field_format_string_utf_bom, "LChar16"),
        TraceLoggingPackedFieldEx(&lcharXBom32.l, 22, event_field_encoding_string_length16_char32, event_field_format_string_utf_bom, "LChar32"));
    TraceLoggingWrite(TestProvider, "StringXml-XBom",
        TraceLoggingPackedFieldEx(lcharXBom8.c, 8, event_field_encoding_zstring_char8, event_field_format_string_xml, "NChar8"),
        TraceLoggingPackedFieldEx(lcharXBom16.c, 12, event_field_encoding_zstring_char16, event_field_format_string_xml, "NChar16"),
        TraceLoggingPackedFieldEx(lcharXBom32.c, 24, event_field_encoding_zstring_char32, event_field_format_string_xml, "NChar32"),
        TraceLoggingPackedFieldEx(&lcharXBom8.l, 9, event_field_encoding_string_length16_char8, event_field_format_string_xml, "LChar8"),
        TraceLoggingPackedFieldEx(&lcharXBom16.l, 12, event_field_encoding_string_length16_char16, event_field_format_string_xml, "LChar16"),
        TraceLoggingPackedFieldEx(&lcharXBom32.l, 22, event_field_encoding_string_length16_char32, event_field_format_string_xml, "LChar32"));
    TraceLoggingWrite(TestProvider, "StringJson-XBom",
        TraceLoggingPackedFieldEx(lcharXBom8.c, 8, event_field_encoding_zstring_char8, event_field_format_string_json, "NChar8"),
        TraceLoggingPackedFieldEx(lcharXBom16.c, 12, event_field_encoding_zstring_char16, event_field_format_string_json, "NChar16"),
        TraceLoggingPackedFieldEx(lcharXBom32.c, 24, event_field_encoding_zstring_char32, event_field_format_string_json, "NChar32"),
        TraceLoggingPackedFieldEx(&lcharXBom8.l, 9, event_field_encoding_string_length16_char8, event_field_format_string_json, "LChar8"),
        TraceLoggingPackedFieldEx(&lcharXBom16.l, 12, event_field_encoding_string_length16_char16, event_field_format_string_json, "LChar16"),
        TraceLoggingPackedFieldEx(&lcharXBom32.l, 22, event_field_encoding_string_length16_char32, event_field_format_string_json, "LChar32"));

    uint16_t const n3 = 3;
    TraceLoggingWrite(TestProvider, "Packed",
        TraceLoggingInt32(5, "five"),
        TraceLoggingPackedStruct(1, "Struct1"),
            TraceLoggingPackedData(lchar8.c, 5),
            TraceLoggingPackedMetadata(event_field_encoding_zstring_char8, "NChar8"),
        TraceLoggingPackedStructArray(1, "StructArray"),
            TraceLoggingPackedData(&n3, 2),
            TraceLoggingPackedStruct(2, "Struct2"),
                TraceLoggingPackedMetadataEx(event_field_encoding_value8, event_field_format_string8, "K"),
                TraceLoggingPackedMetadataEx(event_field_encoding_zstring_char16, event_field_format_hex_bytes, "NChar16"),
                TraceLoggingPackedData("A", 1),
                TraceLoggingPackedData(lchar16.c, 10),
                TraceLoggingPackedData("B", 1),
                TraceLoggingPackedData(lcharBom16.c, 12),
                TraceLoggingPackedData("C", 1),
                TraceLoggingPackedData(lcharXBom16.c, 12),
        TraceLoggingInt32(5, "five"));
    TraceLoggingWrite(TestProvider, "Packed0",
        TraceLoggingInt32(5, "five"),
        TraceLoggingPackedStruct(1, "Struct1"),
            TraceLoggingPackedData(lchar8.c, 5),
            TraceLoggingPackedMetadata(event_field_encoding_zstring_char8, "NChar8"),
        TraceLoggingPackedStructArray(1, "StructArray"),
            TraceLoggingPackedData(u"", 2), // Zero items in array
            TraceLoggingPackedStruct(2, "Struct2"),
                TraceLoggingPackedMetadataEx(event_field_encoding_value8, event_field_format_string8, "K"),
                TraceLoggingPackedMetadataEx(event_field_encoding_zstring_char16, event_field_format_hex_bytes, "NChar16"),
        TraceLoggingInt32(5, "five"));
    static const char MyStrings3[] = "ABC\0\0XYZ";
    TraceLoggingWrite(TestProvider, "PackedComplexArray",
        TraceLoggingInt32(5, "five"),
        TraceLoggingPackedData(u"\x03", 2), // 3 items in array
        TraceLoggingPackedField(
            MyStrings3, sizeof(MyStrings3),
            event_field_encoding_zstring_char8 | event_field_encoding_varray_flag,
            "MyStrings3"),
        TraceLoggingInt32(5, "five"));

    return true;
}
