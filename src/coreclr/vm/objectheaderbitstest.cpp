// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef FEATURE_OBJECT_HEADER_BITS_TEST

#include "common.h"
#include "gcheaputilities.h"

#ifndef DLLEXPORT
#ifdef _MSC_VER
#define DLLEXPORT __declspec(dllexport)
#else
#define DLLEXPORT __attribute__((visibility("default")))
#endif
#endif

namespace
{
alignas(ObjHeader) uint8_t g_object_header_bits_test_storage[sizeof(ObjHeader)];

ObjectHeaderBitsParameters* GetParameters()
{
    ObjectHeaderBitsParameters* parameters =
        GCHeapUtilities::GetGCHeap()->GetObjectHeaderBitsParameters();
    if ((parameters == nullptr) ||
        (parameters->request_status != ObjectHeaderBitsRequestStatus::Accepted) ||
        (parameters->request != ObjectHeaderBitsRequest::Enabled))
    {
        return nullptr;
    }

    return parameters;
}

Object* GetObject(void* handle)
{
    OBJECTREF object = ObjectFromHandle(reinterpret_cast<OBJECTHANDLE>(handle));
    return OBJECTREFToObject(object);
}

ObjHeader* GetSyntheticHeader()
{
    return reinterpret_cast<ObjHeader*>(g_object_header_bits_test_storage);
}
}

extern "C" DLLEXPORT int32_t GC_ObjectHeaderBitsTest_GetParameters(
    ObjectHeaderBitsParameters* destination)
{
    if (destination == nullptr)
    {
        return 0;
    }

    ObjectHeaderBitsParameters* parameters = GetParameters();
    if (parameters == nullptr)
    {
        return 0;
    }

    *destination = *parameters;
    return 1;
}

extern "C" DLLEXPORT uint32_t GC_ObjectHeaderBitsTest_Load(void* handle)
{
    GCX_COOP();
    ObjectHeaderBitsParameters* parameters = GetParameters();
    if ((parameters == nullptr) || (handle == nullptr))
    {
        return UINT32_MAX;
    }

    return GetObject(handle)->GetHeader()->GetGCReservedBits(
        parameters->bit_mask,
        parameters->bit_shift);
}

extern "C" DLLEXPORT uint32_t GC_ObjectHeaderBitsTest_CompareExchange(
    void* handle,
    uint32_t value,
    uint32_t comparand)
{
    GCX_COOP();
    ObjectHeaderBitsParameters* parameters = GetParameters();
    if ((parameters == nullptr) || (handle == nullptr))
    {
        return UINT32_MAX;
    }

    return GetObject(handle)->GetHeader()->CompareExchangeGCReservedBits(
        parameters->bit_mask,
        parameters->bit_shift,
        value,
        comparand);
}

extern "C" DLLEXPORT uint32_t GC_ObjectHeaderBitsTest_Set(
    void* handle,
    uint32_t value)
{
    GCX_COOP();
    ObjectHeaderBitsParameters* parameters = GetParameters();
    if ((parameters == nullptr) || (handle == nullptr))
    {
        return UINT32_MAX;
    }

    return GetObject(handle)->GetHeader()->SetGCReservedBits(
        parameters->bit_mask,
        parameters->bit_shift,
        value);
}

extern "C" DLLEXPORT uint32_t GC_ObjectHeaderBitsTest_SetSynthetic(uint32_t value)
{
    ObjectHeaderBitsParameters* parameters = GetParameters();
    if (parameters == nullptr)
    {
        return UINT32_MAX;
    }

    return GetSyntheticHeader()->SetGCReservedBits(
        parameters->bit_mask,
        parameters->bit_shift,
        value);
}

extern "C" DLLEXPORT uint32_t GC_ObjectHeaderBitsTest_WaitSynthetic(uint32_t value)
{
    ObjectHeaderBitsParameters* parameters = GetParameters();
    if (parameters == nullptr)
    {
        return UINT32_MAX;
    }

    return GetSyntheticHeader()->WaitWhileGCReservedBits(
        parameters->bit_mask,
        parameters->bit_shift,
        value);
}

extern "C" DLLEXPORT uint32_t GC_ObjectHeaderBitsTest_LoadRaw(void* handle)
{
    GCX_COOP();
    if ((GetParameters() == nullptr) || (handle == nullptr))
    {
        return UINT32_MAX;
    }

    return GetObject(handle)->GetHeader()->GetGCReservedBits(UINT32_MAX, 0);
}

extern "C" DLLEXPORT uint32_t GC_ObjectHeaderBitsTest_SetRaw(
    void* handle,
    uint32_t value)
{
    GCX_COOP();
    if ((GetParameters() == nullptr) || (handle == nullptr))
    {
        return UINT32_MAX;
    }

    return GetObject(handle)->GetHeader()->SetGCReservedBits(UINT32_MAX, 0, value);
}

extern "C" DLLEXPORT uint32_t GC_ObjectHeaderBitsTest_GetSyncBlockValue(void* handle)
{
    GCX_COOP();
    if (handle == nullptr)
    {
        return UINT32_MAX;
    }

    return GetObject(handle)->GetHeader()->GetBits();
}

extern "C" DLLEXPORT uintptr_t GC_ObjectHeaderBitsTest_GetObjectAddress(void* handle)
{
    GCX_COOP();
    if (handle == nullptr)
    {
        return 0;
    }

    return reinterpret_cast<uintptr_t>(GetObject(handle));
}

#endif // FEATURE_OBJECT_HEADER_BITS_TEST
