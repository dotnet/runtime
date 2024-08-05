// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: castcache.cpp
//

#include "common.h"
#include "castcache.h"

#if !defined(DACCESS_COMPILE)

BASEARRAYREF* CastCache::s_pTableRef = NULL;
OBJECTHANDLE CastCache::s_sentinelTable = NULL;
DWORD CastCache::s_lastFlushSize     = INITIAL_CACHE_SIZE;
const DWORD CastCache::INITIAL_CACHE_SIZE;

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
    _ASSERTE(size > 1);
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
    EX_END_CATCH(RethrowTerminalExceptions)

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
        EX_END_CATCH(RethrowTerminalExceptions)

        if (!table)
        {
            // OK, no cache then
            return NULL;
        }
    }

    DWORD* tableData = TableData(table);
    TableMask(tableData) = size - 1;

    // Fibonacci hash reduces the value into desired range by shifting right by the number of leading zeroes in 'size-1'
    DWORD bitCnt;
#if HOST_64BIT
    BitScanReverse64(&bitCnt, size - 1);
    HashShift(tableData) = (BYTE)(63 - bitCnt);
#else
    BitScanReverse(&bitCnt, size - 1);
    HashShift(tableData) = (BYTE)(31 - bitCnt);
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

    SetObjectReference((OBJECTREF *)s_pTableRef, newTable);
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

    DWORD* tableData = TableData(*s_pTableRef);
    s_lastFlushSize = max(INITIAL_CACHE_SIZE, CacheElementCount(tableData));

    SetObjectReference((OBJECTREF *)s_pTableRef, ObjectFromHandle(s_sentinelTable));
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

    FieldDesc* pTableField = CoreLibBinder::GetField(FIELD__CASTCACHE__TABLE);

    GCX_COOP();
    s_pTableRef = (BASEARRAYREF*)pTableField->GetCurrentStaticAddress();

    BASEARRAYREF sentinelTable = CreateCastCache(2);
    if (!sentinelTable)
    {
        // no memory for 2 element cache while initializing?
        ThrowOutOfMemory();
    }

    s_sentinelTable = CreateGlobalHandle(sentinelTable);

    // initialize to the sentinel value, this should not be null.
    SetObjectReference((OBJECTREF *)s_pTableRef, sentinelTable);
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

    DWORD* tableData = TableData(*s_pTableRef);

    DWORD index = KeyToBucket(tableData, source, target);
    for (DWORD i = 0; i < BUCKET_SIZE;)
    {
        CastCacheEntry* pEntry = &Elements(tableData)[index];

        // must read in this order: version -> [entry parts] -> version
        // if version is odd or changes, the entry is inconsistent and thus ignored
        DWORD version1 = VolatileLoad(&pEntry->version);
        TADDR entrySource = pEntry->source;

        // mask the lower version bit to make it even.
        // This way we can check if version is odd or changing in just one compare.
        version1 &= ~1;

        if (entrySource == source)
        {
            TADDR entryTargetAndResult = pEntry->targetAndResult;
            // target never has its lower bit set.
            // a matching entryTargetAndResult would have the same bits, except for the lowest one, which is the result.
            entryTargetAndResult ^= target;
            if (entryTargetAndResult <= 1)
            {
                // make sure 'version' is loaded after 'source' and 'targetAndResults'
                VolatileLoadBarrier();
                if (version1 != pEntry->version)
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
        i++;
        index = (index + i) & TableMask(tableData);
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
    DWORD* tableData;

    do
    {
        tableData = TableData(*s_pTableRef);
        if (TableMask(tableData) == 1)
        {
            // 2-element table is used as a sentinel.
            // we did not allocate a real table yet or have flushed it.
            // try replacing the table, but do not insert anything.
            MaybeReplaceCacheWithLarger(s_lastFlushSize);
            return;
        }

        bucket = KeyToBucket(tableData, source, target);
        DWORD index = bucket;
        CastCacheEntry* pEntry = &Elements(tableData)[index];

        for (DWORD i = 0; i < BUCKET_SIZE;)
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

            // VolatileLoadWithoutBarrier is to ensure that the version cannot be re-fetched between here and CompareExchange.
            DWORD version = VolatileLoadWithoutBarrier(&pEntry->version);

            // mask the lower version bit to make it even.
            // This way we will detect both if version is changing (odd) or has changed (even, but different).
            version &= ~1;

            if ((version & VERSION_NUM_MASK) >= (VERSION_NUM_MASK - 2))
            {
                // If exactly VERSION_NUM_MASK updates happens between here and publishing, we may not recognise a race.
                // It is extremely unlikely, but to not worry about the possibility, lets not allow version to go this high and just get a new cache.
                // This will not happen often.
                FlushCurrentCache();
                return;
            }

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
            i++;
            index += i;
            pEntry = &Elements(tableData)[index & TableMask(tableData)];
        }

        // bucket is full.
    } while (TryGrow(tableData));

    // reread tableData after TryGrow.
    tableData = TableData(*s_pTableRef);
    if (TableMask(tableData) == 1)
    {
        // do not insert into a sentinel.
        return;
    }

    // pick a victim somewhat randomly within a bucket
    // NB: ++ is not interlocked. We are ok if we lose counts here. It is just a number that changes.
    DWORD victimDistance = VictimCounter(tableData)++ & (BUCKET_SIZE - 1);
    // position the victim in a quadratic reprobe bucket
    DWORD victim = (victimDistance * victimDistance + victimDistance) / 2;

    {
        CastCacheEntry* pEntry = &Elements(tableData)[(bucket + victim) & TableMask(tableData)];

        // VolatileLoadWithoutBarrier is to ensure that the version cannot be re-fetched between here and CompareExchange.
        DWORD version = VolatileLoadWithoutBarrier(&pEntry->version);

        // mask the lower version bit to make it even.
        // This way we will detect both if version is changing (odd) or has changed (even, but different).
        version &= ~1;

        if ((version & VERSION_NUM_MASK) >= (VERSION_NUM_MASK - 2))
        {
            // If exactly VERSION_NUM_MASK updates happens between here and publishing, we may not recognise a race.
            // It is extremely unlikely, but to not worry about the possibility, lets not allow version to go this high and just get a new cache.
            // This will not happen often.
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

#endif // !DACCESS_COMPILE
