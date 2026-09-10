// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "gcinternal.h"

#if defined(HOST_AMD64) && (defined(_MSC_VER) || defined(__GNUC__))
#include <xmmintrin.h>
#endif

#ifdef SERVER_GC
namespace SVR
{
#else // SERVER_GC
namespace WKS
{
#endif // SERVER_GC

#ifdef HOST_64BIT
static constexpr int CardByteShift = 11;
#else
static constexpr int CardByteShift = 10;
#endif

static bool SetCardByte(void* address)
{
    size_t cardByte = reinterpret_cast<size_t>(address) >> CardByteShift;
    uint8_t* card = reinterpret_cast<uint8_t*>(VolatileLoadWithoutBarrier(&g_gc_card_table)) + cardByte;
    if (*card != 0xff)
    {
        *card = 0xff;
        return true;
    }

    return false;
}

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
static void SetCardBundleByte(void* address)
{
    constexpr int CardBundleByteShift = 21;
    size_t cardBundleByte = reinterpret_cast<size_t>(address) >> CardBundleByteShift;
    uint8_t* cardBundle =
        reinterpret_cast<uint8_t*>(VolatileLoadWithoutBarrier(&g_gc_card_bundle_table)) + cardBundleByte;
    if (*cardBundle != 0xff)
    {
        *cardBundle = 0xff;
    }
}
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES

static FORCEINLINE void CopyForward(void* destination, const void* source, size_t length)
{
    uintptr_t* destinationPointer = static_cast<uintptr_t*>(destination);
    const uintptr_t* sourcePointer = static_cast<const uintptr_t*>(source);

    while (true)
    {
        if ((length & sizeof(uintptr_t)) != 0)
        {
            *destinationPointer = *sourcePointer;

            length ^= sizeof(uintptr_t);
            if (length == 0)
            {
                return;
            }

            sourcePointer++;
            destinationPointer++;
        }

#if defined(HOST_AMD64) && (defined(_MSC_VER) || defined(__GNUC__))
        if ((length & (2 * sizeof(uintptr_t))) != 0)
        {
            __m128 value = _mm_loadu_ps(reinterpret_cast<const float*>(sourcePointer));
            _mm_storeu_ps(reinterpret_cast<float*>(destinationPointer), value);

            length ^= 2 * sizeof(uintptr_t);
            if (length == 0)
            {
                return;
            }

            sourcePointer += 2;
            destinationPointer += 2;
        }

        if ((reinterpret_cast<uintptr_t>(destinationPointer) & sizeof(uintptr_t)) != 0)
        {
            *destinationPointer = *sourcePointer;

            sourcePointer++;
            destinationPointer++;
            length -= sizeof(uintptr_t);
            if (length < 4 * sizeof(uintptr_t))
            {
                continue;
            }
        }

        assert(length >= 4 * sizeof(uintptr_t));
        do
        {
            __m128 value = _mm_loadu_ps(reinterpret_cast<const float*>(sourcePointer));
            _mm_store_ps(reinterpret_cast<float*>(destinationPointer), value);
            value = _mm_loadu_ps(reinterpret_cast<const float*>(sourcePointer + 2));
            _mm_store_ps(reinterpret_cast<float*>(destinationPointer + 2), value);

            sourcePointer += 4;
            destinationPointer += 4;
            length -= 4 * sizeof(uintptr_t);
        }
        while (length >= 4 * sizeof(uintptr_t));

        if (length == 0)
        {
            return;
        }
#else // !(defined(HOST_AMD64) && (defined(_MSC_VER) || defined(__GNUC__)))
        if ((length & (2 * sizeof(uintptr_t))) != 0)
        {
            uintptr_t value0 = sourcePointer[0];
            uintptr_t value1 = sourcePointer[1];
            destinationPointer[0] = value0;
            destinationPointer[1] = value1;

            length ^= 2 * sizeof(uintptr_t);
            if (length == 0)
            {
                return;
            }

            sourcePointer += 2;
            destinationPointer += 2;
        }

        assert(length >= 4 * sizeof(uintptr_t));
        while (true)
        {
            uintptr_t value0 = sourcePointer[0];
            uintptr_t value1 = sourcePointer[1];
            destinationPointer[0] = value0;
            destinationPointer[1] = value1;
            value0 = sourcePointer[2];
            value1 = sourcePointer[3];
            destinationPointer[2] = value0;
            destinationPointer[3] = value1;

            length -= 4 * sizeof(uintptr_t);
            if (length == 0)
            {
                return;
            }

            sourcePointer += 4;
            destinationPointer += 4;
        }
#endif // defined(HOST_AMD64) && (defined(_MSC_VER) || defined(__GNUC__))
    }
}

static FORCEINLINE void CopyBackward(void* destination, const void* source, size_t length)
{
    uintptr_t* destinationPointer =
        reinterpret_cast<uintptr_t*>(static_cast<uint8_t*>(destination) + length);
    const uintptr_t* sourcePointer =
        reinterpret_cast<const uintptr_t*>(static_cast<const uint8_t*>(source) + length);

    while (true)
    {
        if ((length & sizeof(uintptr_t)) != 0)
        {
            sourcePointer--;
            destinationPointer--;

            *destinationPointer = *sourcePointer;

            length ^= sizeof(uintptr_t);
            if (length == 0)
            {
                return;
            }
        }

#if defined(HOST_AMD64) && (defined(_MSC_VER) || defined(__GNUC__))
        if ((length & (2 * sizeof(uintptr_t))) != 0)
        {
            sourcePointer -= 2;
            destinationPointer -= 2;

            __m128 value = _mm_loadu_ps(reinterpret_cast<const float*>(sourcePointer));
            _mm_storeu_ps(reinterpret_cast<float*>(destinationPointer), value);

            length ^= 2 * sizeof(uintptr_t);
            if (length == 0)
            {
                return;
            }
        }

        if ((reinterpret_cast<uintptr_t>(destinationPointer) & sizeof(uintptr_t)) != 0)
        {
            sourcePointer--;
            destinationPointer--;

            *destinationPointer = *sourcePointer;

            length -= sizeof(uintptr_t);
            if (length < 4 * sizeof(uintptr_t))
            {
                continue;
            }
        }

        assert(length >= 4 * sizeof(uintptr_t));
        do
        {
            sourcePointer -= 4;
            destinationPointer -= 4;

            __m128 value = _mm_loadu_ps(reinterpret_cast<const float*>(sourcePointer + 2));
            _mm_store_ps(reinterpret_cast<float*>(destinationPointer + 2), value);
            value = _mm_loadu_ps(reinterpret_cast<const float*>(sourcePointer));
            _mm_store_ps(reinterpret_cast<float*>(destinationPointer), value);

            length -= 4 * sizeof(uintptr_t);
        }
        while (length >= 4 * sizeof(uintptr_t));

        if (length == 0)
        {
            return;
        }
#else // !(defined(HOST_AMD64) && (defined(_MSC_VER) || defined(__GNUC__)))
        if ((length & (2 * sizeof(uintptr_t))) != 0)
        {
            sourcePointer -= 2;
            destinationPointer -= 2;

            uintptr_t value1 = sourcePointer[1];
            uintptr_t value0 = sourcePointer[0];
            destinationPointer[1] = value1;
            destinationPointer[0] = value0;

            length ^= 2 * sizeof(uintptr_t);
            if (length == 0)
            {
                return;
            }
        }

        assert(length >= 4 * sizeof(uintptr_t));
        do
        {
            sourcePointer -= 4;
            destinationPointer -= 4;

            uintptr_t value0 = sourcePointer[2];
            uintptr_t value1 = sourcePointer[3];
            destinationPointer[2] = value0;
            destinationPointer[3] = value1;
            value0 = sourcePointer[0];
            value1 = sourcePointer[1];
            destinationPointer[0] = value0;
            destinationPointer[1] = value1;

            length -= 4 * sizeof(uintptr_t);
        }
        while (length != 0);

        return;
#endif // defined(HOST_AMD64) && (defined(_MSC_VER) || defined(__GNUC__))
    }
}

static FORCEINLINE void BulkMoveWithWriteBarrierCore(
    void* destination,
    const void* source,
    size_t length)
{
    assert(destination != nullptr);
    assert(source != nullptr);
    assert(destination != source);
    assert(length != 0);
    assert((reinterpret_cast<uintptr_t>(destination) % sizeof(uintptr_t)) == 0);
    assert((reinterpret_cast<uintptr_t>(source) % sizeof(uintptr_t)) == 0);
    assert((length % sizeof(uintptr_t)) == 0);

    uint8_t* destinationAddress = static_cast<uint8_t*>(destination);
    bool destinationIsInHeap =
        destinationAddress >= g_gc_lowest_address && destinationAddress < g_gc_highest_address;

#if !defined(HOST_X86) && !defined(HOST_AMD64)
    if (destinationIsInHeap)
    {
        MemoryBarrier();
    }
#endif // !HOST_X86 && !HOST_AMD64

    if (reinterpret_cast<uintptr_t>(destination) - reinterpret_cast<uintptr_t>(source) >= length)
    {
        CopyForward(destination, source, length);
    }
    else
    {
        CopyBackward(destination, source, length);
    }

    if (!destinationIsInHeap)
    {
        return;
    }

#if defined(WRITE_BARRIER_CHECK) && !defined(SERVER_GC)
    if (GCConfig::GetHeapVerifyLevel() & GCConfig::HEAPVERIFY_BARRIERCHECK)
    {
        Object** objectDestination = static_cast<Object**>(destination);
        for (size_t i = 0; i < length / sizeof(Object*); i++)
        {
            updateGCShadow(&objectDestination[i], objectDestination[i]);
        }
    }
#endif // WRITE_BARRIER_CHECK && !SERVER_GC

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    if (SoftwareWriteWatch::IsEnabledForGCHeap())
    {
        SoftwareWriteWatch::SetDirtyRegion(destination, length);
    }
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

    size_t startAddress = reinterpret_cast<size_t>(destination);
    size_t endAddress = startAddress + length;
    size_t startingClump = startAddress >> CardByteShift;
    size_t endingClump = (endAddress + (static_cast<size_t>(1) << CardByteShift) - 1) >> CardByteShift;
    size_t clumpCount = endingClump - startingClump;
    uint8_t* card = reinterpret_cast<uint8_t*>(VolatileLoadWithoutBarrier(&g_gc_card_table)) + startingClump;

    do
    {
        if (*card != 0xff)
        {
            *card = 0xff;
        }

        card++;
        clumpCount--;
    }
    while (clumpCount != 0);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    const int cardBundleByteShift = 21;
    size_t startBundleByte = startAddress >> cardBundleByteShift;
    size_t endBundleByte =
        (endAddress + (static_cast<size_t>(1) << cardBundleByteShift) - 1) >> cardBundleByteShift;
    size_t bundleByteCount = endBundleByte - startBundleByte;
    uint8_t* bundleByte =
        reinterpret_cast<uint8_t*>(VolatileLoadWithoutBarrier(&g_gc_card_bundle_table)) + startBundleByte;

    do
    {
        if (*bundleByte != 0xff)
        {
            *bundleByte = 0xff;
        }

        bundleByte++;
        bundleByteCount--;
    }
    while (bundleByteCount != 0);
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
}

void GCHeap::BulkMoveWithWriteBarrier(
    void* destination,
    const void* source,
    size_t length)
{
    BulkMoveWithWriteBarrierCore(destination, source, length);
}

template <bool DestinationMustBeInHeap, bool ReturnResult>
FORCEINLINE WriteBarrierResult GCHeap::PostWriteBarrierCore(
    void** destination,
    void* value,
    Object* reference,
    bool requiresGenerationalTracking)
{
    uint8_t* destinationAddress = reinterpret_cast<uint8_t*>(destination);
    if (DestinationMustBeInHeap)
    {
        assert(destinationAddress >= g_gc_lowest_address && destinationAddress < g_gc_highest_address);
    }
    else if (destinationAddress < g_gc_lowest_address || destinationAddress >= g_gc_highest_address)
    {
        return WriteBarrierResult::None;
    }

    WriteBarrierResult result = WriteBarrierResult::DestinationInHeap;

#ifdef WRITE_BARRIER_CHECK
    updateGCShadow(reinterpret_cast<Object**>(destination), reinterpret_cast<Object*>(value));
#endif // WRITE_BARRIER_CHECK

    if (!requiresGenerationalTracking)
    {
        return result;
    }

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    if (SoftwareWriteWatch::IsEnabledForGCHeap())
    {
        SoftwareWriteWatch::SetDirtyRegion(destination, sizeof(*destination));
    }
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

#ifdef FEATURE_COUNT_GC_WRITE_BARRIERS
#if defined(USE_REGIONS) || !defined(SERVER_GC)
    if (destinationAddress >= gc_heap::ephemeral_low && destinationAddress < gc_heap::ephemeral_high)
    {
        if (ReturnResult)
        {
            result = result | WriteBarrierResult::DestinationInEphemeralRange;
        }
    }
#endif // USE_REGIONS || !SERVER_GC
#endif // FEATURE_COUNT_GC_WRITE_BARRIERS

#if defined(USE_REGIONS) || !defined(SERVER_GC)
    uint8_t* referenceAddress = reinterpret_cast<uint8_t*>(reference);
    bool referenceIsEphemeral =
        referenceAddress >= gc_heap::ephemeral_low && referenceAddress < gc_heap::ephemeral_high;
#else // !USE_REGIONS && SERVER_GC
    bool referenceIsEphemeral = reference != nullptr && IsEphemeral(reference);
#endif // USE_REGIONS || !SERVER_GC

    if (referenceIsEphemeral)
    {
        if (ReturnResult)
        {
            result = result | WriteBarrierResult::ReferenceInEphemeralRange;
        }

        bool cardMarked = SetCardByte(destination);
        if (cardMarked)
        {
            if (ReturnResult)
            {
                result = result | WriteBarrierResult::CardMarked;
            }
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
            SetCardBundleByte(destination);
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        }
    }

    return result;
}

void GCHeap::WriteBarrierCallback(void* context, void** destination, void* reference)
{
    static_cast<GCHeap*>(context)->PostWriteBarrierCore<true, false>(
        destination,
        reference,
        static_cast<Object*>(reference),
        true);
}

void GCHeap::CheckedWriteBarrierCallback(void* context, void** destination, void* reference)
{
    static_cast<GCHeap*>(context)->PostWriteBarrierCore<false, false>(
        destination,
        reference,
        static_cast<Object*>(reference),
        true);
}

bool GCHeap::IsInGCHeapCallback(void* context, void* address)
{
    return static_cast<GCHeap*>(context)->GCHeap::IsInGCHeap(address);
}

void GCHeap::BulkMoveWithWriteBarrierCallback(
    void* destination,
    const void* source,
    size_t length)
{
    BulkMoveWithWriteBarrierCore(destination, source, length);
}

WriteBarrierResult GCHeap::PostWriteBarrier(
    void** destination,
    void* value,
    Object* reference,
    bool requiresGenerationalTracking,
    bool destinationMustBeInHeap)
{
    return destinationMustBeInHeap
        ? PostWriteBarrierCore<true, true>(destination, value, reference, requiresGenerationalTracking)
        : PostWriteBarrierCore<false, true>(destination, value, reference, requiresGenerationalTracking);
}

void GCHeap::GetWriteBarrierFunctions(WriteBarrierFunctions* functions)
{
    functions->context = this;
    functions->write_barrier = WriteBarrierCallback;
    functions->checked_write_barrier = CheckedWriteBarrierCallback;
    functions->is_in_gc_heap = IsInGCHeapCallback;
    functions->bulk_move_with_write_barrier = BulkMoveWithWriteBarrierCallback;
}

}
