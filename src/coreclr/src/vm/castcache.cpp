// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: castcache.cpp
//

#include "common.h"
#include "castcache.h"

#if !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)

OBJECTHANDLE CastCache::s_cache = NULL;
DWORD CastCache::s_lastFlushSize = INITIAL_CACHE_SIZE;

BASEARRAYREF CastCache::CreateCastCache(DWORD size)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    // size must be positive
    _ASSERTE(size > 0);
    // size must be a power of two
    _ASSERTE((size & (size - 1)) == 0);

    BASEARRAYREF table = NULL;

    // if we get an OOM here, we try a smaller size
    EX_TRY
    {
        FAULT_NOT_FATAL();
        table = (BASEARRAYREF)AllocatePrimitiveArray(CorElementType::ELEMENT_TYPE_I4, (size + 1) * sizeof(CastCacheEntry) / sizeof(INT32));
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(RethrowCorruptingExceptions)

    if (!table)
    {
        size = INITIAL_CACHE_SIZE;
        // if we get an OOM again we return NULL
        EX_TRY
        {
            FAULT_NOT_FATAL();
            table = (BASEARRAYREF)AllocatePrimitiveArray(CorElementType::ELEMENT_TYPE_I4, (size + 1) * sizeof(CastCacheEntry) / sizeof(INT32));
        }
        EX_CATCH
        {
        }
        EX_END_CATCH(RethrowCorruptingExceptions)

        if (!table)
        {
            // OK, no cache then
            return NULL;
        }
    }

    TableMask(table) = size - 1;

    // Fibonacci hash reduces the value into desired range by shifting right by the number of leading zeroes in 'size-1'
    DWORD bitCnt;
#if BIT64
    BitScanReverse64(&bitCnt, size - 1);
    HashShift(table) = (BYTE)(63 - bitCnt);
#else
    BitScanReverse(&bitCnt, size - 1);
    HashShift(table) = (BYTE)(31 - bitCnt);
#endif

    return table;
}

BOOL CastCache::MaybeReplaceCacheWithLarger(DWORD size)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    BASEARRAYREF newTable = CreateCastCache(size);
    if (!newTable)
    {
        return FALSE;
    }

    StoreObjectInHandle(s_cache, newTable);
    return TRUE;
}

void CastCache::FlushCurrentCache()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    BASEARRAYREF currentTableRef = (BASEARRAYREF)ObjectFromHandle(s_cache);
    s_lastFlushSize = !currentTableRef ? INITIAL_CACHE_SIZE : CacheElementCount(currentTableRef);

    StoreObjectInHandle(s_cache, NULL);
}

void CastCache::Initialize()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    s_cache = CreateGlobalHandle(NULL);
}

TypeHandle::CastResult CastCache::TryGet(TADDR source, TADDR target)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    BASEARRAYREF table = (BASEARRAYREF)ObjectFromHandle(s_cache);

    // we use NULL as a sentinel for a rare case when a table could not be allocated
    // because we avoid OOMs in conversions
    // we could use 0-element table instead, but then we would have to check the size here.
    if (!table)
    {
        return TypeHandle::MaybeCast;
    }

    DWORD index = KeyToBucket(table, source, target);
    CastCacheEntry* pEntry = &Elements(table)[index];

    for (DWORD i = 0; i < BUCKET_SIZE; i++)
    {
        // must read in this order: version -> entry parts -> version
        // if version is odd or changes, the entry is inconsistent and thus ignored
        DWORD version1 = VolatileLoad(&pEntry->version);
        TADDR entrySource = pEntry->source;

        if (entrySource == source)
        {
            TADDR entryTargetAndResult = VolatileLoad(&pEntry->targetAndResult);

            // target never has its lower bit set.
            // a matching entryTargetAndResult would have same bits, except for the lowest one, which is the result.
            entryTargetAndResult ^= target;
            if (entryTargetAndResult <= 1)
            {
                DWORD version2 = pEntry->version;
                if (version2 != version1 || (version1 & 1))
                {
                    // oh, so close, the entry is in inconsistent state.
                    // it is either changing or has changed while we were reading.
                    // treat it as a miss.
                    break;
                }

                return TypeHandle::CastResult(entryTargetAndResult);
            }
        }

        if (version1 == 0)
        {
            // the rest of the bucket is unclaimed, no point to search further
            break;
        }

        // quadratic reprobe
        index += i;
        pEntry = &Elements(table)[index & TableMask(table)];
    }

    return TypeHandle::MaybeCast;
}

void CastCache::TrySet(TADDR source, TADDR target, BOOL result)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    DWORD bucket;
    BASEARRAYREF table;

    do
    {
        table = (BASEARRAYREF)ObjectFromHandle(s_cache);
        if (!table)
        {
            // we did not allocate a table or flushed it, try replacing, but do not continue looping.
            MaybeReplaceCacheWithLarger(s_lastFlushSize);
            return;
        }

        bucket = KeyToBucket(table, source, target);
        DWORD index = bucket;
        CastCacheEntry* pEntry = &Elements(table)[index];

        for (DWORD i = 0; i < BUCKET_SIZE; i++)
        {
            // claim the entry if unused or is more distant than us from its origin.
            // Note - someone familiar with Robin Hood hashing will notice that
            //        we do the opposite - we are "robbing the poor".
            //        Robin Hood strategy improves average lookup in a lossles dictionary by reducing
            //        outliers via giving preference to more distant entries.
            //        What we have here is a lossy cache with outliers bounded by the bucket size.
            //        We improve average lookup by giving preference to the "richer" entries.
            //        If we used Robin Hood strategy we could eventually end up with all
            //        entries in the table being maximally "poor".
            DWORD version = pEntry->version;
            if (version == 0 || (version >> VERSION_NUM_SIZE) > i)
            {
                DWORD newVersion = (i << VERSION_NUM_SIZE) + (version & VERSION_NUM_MASK) + 1;
                DWORD versionOrig = InterlockedCompareExchangeT(&pEntry->version, newVersion, version);
                if (versionOrig == version)
                {
                    pEntry->SetEntry(source, target, result);

                    // entry is in inconsistent state and cannot be read or written to until we
                    // update the version, which is the last thing we do here
                    VolatileStore(&pEntry->version, newVersion + 1);
                    return;
                }
                // someone snatched the entry. try the next one in the bucket.
            }

            if (pEntry->Source() == source && pEntry->Target() == target)
            {
                // looks like we already have an entry for this.
                // duplicate entries are harmless, but a bit of a waste.
                return;
            }

            // quadratic reprobe
            index += i;
            pEntry = &Elements(table)[index & TableMask(table)];
        }

        // bucket is full.
    } while (TryGrow(table));

    // reread table after TryGrow.
    table = (BASEARRAYREF)ObjectFromHandle(s_cache);
    if (!table)
    {
        // we did not allocate a table.
        return;
    }

    // pick a victim somewhat randomly within a bucket
    // NB: ++ is not interlocked. We are ok if we lose counts here. It is just a number that changes.
    DWORD victimDistance = VictimCounter(table)++ & (BUCKET_SIZE - 1);
    // position the victim in a quadratic reprobe bucket
    DWORD victim = (victimDistance * victimDistance + victimDistance) / 2;

    {
        CastCacheEntry* pEntry = &Elements(table)[(bucket + victim) & TableMask(table)];

        DWORD version = pEntry->version;
        if ((version & VERSION_NUM_MASK) >= (VERSION_NUM_MASK - 2))
        {
            // It is unlikely for a reader to sit between versions while exactly 2^VERSION_NUM_SIZE updates happens.
            // Anyways, to not bother about the possibility, lets get a new cache. It will not happen often, if ever.
            FlushCurrentCache();
            return;
        }

        DWORD newVersion = (victimDistance << VERSION_NUM_SIZE) + (version & VERSION_NUM_MASK) + 1;
        DWORD versionOrig = InterlockedCompareExchangeT(&pEntry->version, newVersion, version);

        if (versionOrig == version)
        {
            pEntry->SetEntry(source, target, result);
            VolatileStore(&pEntry->version, newVersion + 1);
        }
    }
}

#endif // !DACCESS_COMPILE && !CROSSGEN_COMPILE
