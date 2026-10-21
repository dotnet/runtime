// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef OBJECT_REFERENCE_ENUMERATION_TEST_H
#define OBJECT_REFERENCE_ENUMERATION_TEST_H

#ifdef FEATURE_OBJECT_REFERENCE_ENUMERATION_TEST

enum class ObjectReferenceEnumerationTestMode : int32_t
{
    Disabled = 0,
    Callback = 1,
    Visitor = 2,
    Cursor = 3,
};

struct ObjectReferenceEnumerationTestSnapshot
{
    int32_t mode;
    int32_t errors;
    int64_t objectScans;
    int64_t ranges;
    int64_t slots;
    int64_t nonNullSlots;
    uint64_t checksum;
};

extern int32_t g_object_reference_enumeration_test_mode;

void ObjectReferenceEnumerationTestScan(Object* object);

FORCEINLINE void ObjectReferenceEnumerationTestScanIfEnabled(Object* object)
{
    if (VolatileLoad(&g_object_reference_enumeration_test_mode) != 0)
    {
        ObjectReferenceEnumerationTestScan(object);
    }
}

#endif // FEATURE_OBJECT_REFERENCE_ENUMERATION_TEST

#endif // OBJECT_REFERENCE_ENUMERATION_TEST_H
