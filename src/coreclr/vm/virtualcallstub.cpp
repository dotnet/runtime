// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: VirtualCallStub.CPP
//
// This file contains the virtual call stub manager and caches
//



//

//
// ============================================================================

#include "common.h"
#include "array.h"

#ifdef FEATURE_PERFMAP
#include "perfmap.h"
#endif

#ifndef DACCESS_COMPILE

//@TODO: make these conditional on whether logs are being produced
//instrumentation counters
UINT32 g_site_counter = 0;              //# of call sites
UINT32 g_site_write = 0;                //# of call site backpatch writes
UINT32 g_site_write_poly = 0;           //# of call site backpatch writes to point to resolve stubs
UINT32 g_site_write_mono = 0;           //# of call site backpatch writes to point to dispatch stubs

UINT32 g_stub_lookup_counter = 0;       //# of lookup stubs
UINT32 g_stub_mono_counter = 0;         //# of dispatch stubs
UINT32 g_stub_poly_counter = 0;         //# of resolve stubs
UINT32 g_stub_vtable_counter = 0;       //# of vtable call stubs
UINT32 g_stub_space = 0;                //# of bytes of stubs

UINT32 g_reclaim_counter = 0;           //# of times a ReclaimAll was performed

UINT32 g_worker_call = 0;               //# of calls into ResolveWorker
UINT32 g_worker_call_no_patch = 0;
UINT32 g_worker_collide_to_mono = 0;    //# of times we converted a poly stub to a mono stub instead of writing the cache entry

UINT32 g_external_call = 0;             //# of calls into GetTarget(token, pMT)
UINT32 g_external_call_no_patch = 0;

UINT32 g_insert_cache_external = 0;     //# of times Insert was called for IK_EXTERNAL
UINT32 g_insert_cache_shared = 0;       //# of times Insert was called for IK_SHARED
UINT32 g_insert_cache_dispatch = 0;     //# of times Insert was called for IK_DISPATCH
UINT32 g_insert_cache_resolve = 0;      //# of times Insert was called for IK_RESOLVE
UINT32 g_insert_cache_hit = 0;          //# of times Insert found an empty cache entry
UINT32 g_insert_cache_miss = 0;         //# of times Insert already had a matching cache entry
UINT32 g_insert_cache_collide = 0;      //# of times Insert found a used cache entry
UINT32 g_insert_cache_write = 0;        //# of times Insert wrote a cache entry

UINT32 g_cache_entry_counter = 0;       //# of cache structs
UINT32 g_cache_entry_space = 0;         //# of bytes used by cache lookup structs

UINT32 g_call_lookup_counter = 0;       //# of times lookup stubs entered

UINT32 g_mono_call_counter = 0;         //# of time dispatch stubs entered
UINT32 g_mono_miss_counter = 0;         //# of times expected MT did not match actual MT (dispatch stubs)

UINT32 g_poly_call_counter = 0;         //# of times resolve stubs entered
UINT32 g_poly_miss_counter = 0;         //# of times cache missed (resolve stub)

UINT32 g_chained_lookup_call_counter = 0;   //# of hits in a chained lookup
UINT32 g_chained_lookup_miss_counter = 0;   //# of misses in a chained lookup

UINT32 g_chained_lookup_external_call_counter = 0;   //# of hits in an external chained lookup
UINT32 g_chained_lookup_external_miss_counter = 0;   //# of misses in an external chained lookup

UINT32 g_chained_entry_promoted = 0;    //# of times a cache entry is promoted to the start of the chain

UINT32 g_bucket_space = 0;              //# of bytes in caches and tables, not including the stubs themselves
UINT32 g_bucket_space_dead = 0;         //# of bytes of abandoned buckets not yet recycled

#endif // !DACCESS_COMPILE

// This is the number of times a successful chain lookup will occur before the
// entry is promoted to the front of the chain. This is declared as extern because
// the default value (CALL_STUB_CACHE_INITIAL_SUCCESS_COUNT) is defined in the header.
#ifdef TARGET_ARM64
extern "C" size_t g_dispatch_cache_chain_success_counter;
#else
extern size_t g_dispatch_cache_chain_success_counter;
#endif

#define DECLARE_DATA
#include "virtualcallstub.h"
#undef DECLARE_DATA
#include "profilepriv.h"
#include "contractimpl.h"
#include "dynamicinterfacecastable.h"

SPTR_IMPL_INIT(VirtualCallStubManagerManager, VirtualCallStubManagerManager, g_pManager, NULL);

#ifndef DACCESS_COMPILE

#ifdef STUB_LOGGING
UINT32 STUB_MISS_COUNT_VALUE = 100;
UINT32 STUB_COLLIDE_WRITE_PCT = 100;
UINT32 STUB_COLLIDE_MONO_PCT  =   0;
#endif // STUB_LOGGING

FastTable::NumCallStubs_t FastTable::NumCallStubs;

FastTable* BucketTable::dead = NULL;    //linked list of the abandoned buckets

DispatchCache *g_resolveCache = NULL;    //cache of dispatch stubs for in line lookup by resolve stubs.

size_t g_dispatch_cache_chain_success_counter = CALL_STUB_CACHE_INITIAL_SUCCESS_COUNT;

#ifdef STUB_LOGGING
UINT32 g_resetCacheCounter;
UINT32 g_resetCacheIncr;
UINT32 g_dumpLogCounter;
UINT32 g_dumpLogIncr;
#endif // STUB_LOGGING

//@TODO: use the existing logging mechanisms.  for now we write to a file.
HANDLE g_hStubLogFile;

void VirtualCallStubManager::StartupLogging()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        FORBID_FAULT;
    }
    CONTRACTL_END

    GCX_PREEMP();

    EX_TRY
    {
        FAULT_NOT_FATAL(); // We handle filecreation problems locally
        SString str;
        str.Printf("StubLog_%d.log", GetCurrentProcessId());
        g_hStubLogFile = WszCreateFile (str.GetUnicode(),
                                        GENERIC_WRITE,
                                        0,
                                        0,
                                        CREATE_ALWAYS,
                                        FILE_ATTRIBUTE_NORMAL,
                                        0);
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions)

    if (g_hStubLogFile == INVALID_HANDLE_VALUE) {
        g_hStubLogFile = NULL;
    }
}

#define OUTPUT_FORMAT_INT "\t%-30s %d\r\n"
#define OUTPUT_FORMAT_SIZE "\t%-30s %zu\r\n"
#define OUTPUT_FORMAT_PCT "\t%-30s %#5.2f%%\r\n"
#define OUTPUT_FORMAT_INT_PCT "\t%-30s %5d (%#5.2f%%)\r\n"

void VirtualCallStubManager::LoggingDump()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        FORBID_FAULT;
    }
    CONTRACTL_END

    VirtualCallStubManagerIterator it =
        VirtualCallStubManagerManager::GlobalManager()->IterateVirtualCallStubManagers();

    while (it.Next())
    {
        it.Current()->LogStats();
    }

    g_resolveCache->LogStats();

    // Temp space to use for formatting the output.
    static const int FMT_STR_SIZE = 160;
    char szPrintStr[FMT_STR_SIZE];
    DWORD dwWriteByte;

    if(g_hStubLogFile)
    {
#ifdef STUB_LOGGING
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), "\r\nstub tuning parameters\r\n");
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);

        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), "\t%-30s %3d  (0x%02x)\r\n", "STUB_MISS_COUNT_VALUE",
                STUB_MISS_COUNT_VALUE, STUB_MISS_COUNT_VALUE);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), "\t%-30s %3d%% (0x%02x)\r\n", "STUB_COLLIDE_WRITE_PCT",
                STUB_COLLIDE_WRITE_PCT, STUB_COLLIDE_WRITE_PCT);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), "\t%-30s %3d%% (0x%02x)\r\n", "STUB_COLLIDE_MONO_PCT",
                STUB_COLLIDE_MONO_PCT, STUB_COLLIDE_MONO_PCT);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), "\t%-30s %3d%% (0x%02x)\r\n", "DumpLogCounter",
                g_dumpLogCounter, g_dumpLogCounter);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), "\t%-30s %3d%% (0x%02x)\r\n", "DumpLogIncr",
                g_dumpLogCounter, g_dumpLogIncr);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), "\t%-30s %3d%% (0x%02x)\r\n", "ResetCacheCounter",
                g_resetCacheCounter, g_resetCacheCounter);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), "\t%-30s %3d%% (0x%02x)\r\n", "ResetCacheIncr",
                g_resetCacheCounter, g_resetCacheIncr);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
#endif // STUB_LOGGING

        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), "\r\nsite data\r\n");
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);

        //output counters
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_INT, "site_counter", g_site_counter);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_INT, "site_write", g_site_write);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_INT, "site_write_mono", g_site_write_mono);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_INT, "site_write_poly", g_site_write_poly);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);

        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), "\r\n%-30s %d\r\n", "reclaim_counter", g_reclaim_counter);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);

        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), "\r\nstub data\r\n");
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);

        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_INT, "stub_lookup_counter", g_stub_lookup_counter);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_INT, "stub_mono_counter", g_stub_mono_counter);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_INT, "stub_poly_counter", g_stub_poly_counter);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_INT, "stub_vtable_counter", g_stub_vtable_counter);
        WriteFile(g_hStubLogFile, szPrintStr, (DWORD)strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_INT, "stub_space", g_stub_space);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);

#ifdef STUB_LOGGING

        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), "\r\nlookup stub data\r\n");
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);

        UINT32 total_calls = g_mono_call_counter + g_poly_call_counter;

        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_INT, "lookup_call_counter", g_call_lookup_counter);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);

        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), "\r\n%-30s %d\r\n", "total stub dispatch calls", total_calls);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);

        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), "\r\n%-30s %#5.2f%%\r\n", "mono stub data",
                100.0 * double(g_mono_call_counter)/double(total_calls));
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);

        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_INT, "mono_call_counter", g_mono_call_counter);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_INT, "mono_miss_counter", g_mono_miss_counter);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_PCT, "miss percent",
                100.0 * double(g_mono_miss_counter)/double(g_mono_call_counter));
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);

        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), "\r\n%-30s %#5.2f%%\r\n", "poly stub data",
                100.0 * double(g_poly_call_counter)/double(total_calls));
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);

        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_INT, "poly_call_counter", g_poly_call_counter);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_INT, "poly_miss_counter", g_poly_miss_counter);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_PCT, "miss percent",
                100.0 * double(g_poly_miss_counter)/double(g_poly_call_counter));
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
#endif // STUB_LOGGING

#ifdef CHAIN_LOOKUP
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), "\r\nchain lookup data\r\n");
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);

#ifdef STUB_LOGGING
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_INT, "chained_lookup_call_counter", g_chained_lookup_call_counter);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_INT, "chained_lookup_miss_counter", g_chained_lookup_miss_counter);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_PCT, "miss percent",
                100.0 * double(g_chained_lookup_miss_counter)/double(g_chained_lookup_call_counter));
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);

        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_INT, "chained_lookup_external_call_counter", g_chained_lookup_external_call_counter);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_INT, "chained_lookup_external_miss_counter", g_chained_lookup_external_miss_counter);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_PCT, "miss percent",
                100.0 * double(g_chained_lookup_external_miss_counter)/double(g_chained_lookup_external_call_counter));
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
#endif // STUB_LOGGING
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_INT, "chained_entry_promoted", g_chained_entry_promoted);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
#endif // CHAIN_LOOKUP

#ifdef STUB_LOGGING
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), "\r\n%-30s %#5.2f%%\r\n", "worker (slow resolver) data",
                100.0 * double(g_worker_call)/double(total_calls));
#else // !STUB_LOGGING
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), "\r\nworker (slow resolver) data\r\n");
#endif // !STUB_LOGGING
                WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);

        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_INT, "worker_call", g_worker_call);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_INT, "worker_call_no_patch", g_worker_call_no_patch);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_INT, "external_call", g_external_call);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_INT, "external_call_no_patch", g_external_call_no_patch);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_INT, "worker_collide_to_mono", g_worker_collide_to_mono);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);

        UINT32 total_inserts = g_insert_cache_external
                             + g_insert_cache_shared
                             + g_insert_cache_dispatch
                             + g_insert_cache_resolve;

        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), "\r\n%-30s %d\r\n", "insert cache data", total_inserts);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);

        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_INT_PCT, "insert_cache_external", g_insert_cache_external,
                100.0 * double(g_insert_cache_external)/double(total_inserts));
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_INT_PCT, "insert_cache_shared", g_insert_cache_shared,
                100.0 * double(g_insert_cache_shared)/double(total_inserts));
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_INT_PCT, "insert_cache_dispatch", g_insert_cache_dispatch,
                100.0 * double(g_insert_cache_dispatch)/double(total_inserts));
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_INT_PCT, "insert_cache_resolve", g_insert_cache_resolve,
                100.0 * double(g_insert_cache_resolve)/double(total_inserts));
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_INT_PCT, "insert_cache_hit", g_insert_cache_hit,
                100.0 * double(g_insert_cache_hit)/double(total_inserts));
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_INT_PCT, "insert_cache_miss", g_insert_cache_miss,
                100.0 * double(g_insert_cache_miss)/double(total_inserts));
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_INT_PCT, "insert_cache_collide", g_insert_cache_collide,
                100.0 * double(g_insert_cache_collide)/double(total_inserts));
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_INT_PCT, "insert_cache_write", g_insert_cache_write,
                100.0 * double(g_insert_cache_write)/double(total_inserts));
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);

        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), "\r\ncache data\r\n");
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);

        size_t total, used;
        g_resolveCache->GetLoadFactor(&total, &used);

        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_SIZE, "cache_entry_used", used);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_INT, "cache_entry_counter", g_cache_entry_counter);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_INT, "cache_entry_space", g_cache_entry_space);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);

        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), "\r\nstub hash table data\r\n");
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);

        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_INT, "bucket_space", g_bucket_space);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_INT, "bucket_space_dead", g_bucket_space_dead);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);

        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), "\r\ncache_load:\t%zu used, %zu total, utilization %#5.2f%%\r\n",
                used, total, 100.0 * double(used) / double(total));
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);

#ifdef STUB_LOGGING
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), "\r\ncache entry write counts\r\n");
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        DispatchCache::CacheEntryData *rgCacheData = g_resolveCache->cacheData;
        for (UINT16 i = 0; i < CALL_STUB_CACHE_SIZE; i++)
        {
            sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), " %4d", rgCacheData[i]);
            WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
            if (i % 16 == 15)
            {
                sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), "\r\n");
                WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
            }
        }
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), "\r\n");
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
#endif // STUB_LOGGING

#if 0
        for (unsigned i = 0; i < ContractImplMap::max_delta_count; i++)
        {
            if (ContractImplMap::deltasDescs[i] != 0)
            {
                sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), "deltasDescs[%d]\t%d\r\n", i, ContractImplMap::deltasDescs[i]);
                WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
            }
        }
        for (unsigned i = 0; i < ContractImplMap::max_delta_count; i++)
        {
            if (ContractImplMap::deltasSlots[i] != 0)
            {
                sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), "deltasSlots[%d]\t%d\r\n", i, ContractImplMap::deltasSlots[i]);
                WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
            }
        }
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), "cout of maps:\t%d\r\n", ContractImplMap::countMaps);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), "count of interfaces:\t%d\r\n", ContractImplMap::countInterfaces);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), "count of deltas:\t%d\r\n", ContractImplMap::countDelta);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), "total delta for descs:\t%d\r\n", ContractImplMap::totalDeltaDescs);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), "total delta for slots:\t%d\r\n", ContractImplMap::totalDeltaSlots);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);

#endif // 0
    }
}

void VirtualCallStubManager::FinishLogging()
{
    LoggingDump();

    if(g_hStubLogFile)
    {
        CloseHandle(g_hStubLogFile);
    }
    g_hStubLogFile = NULL;
}

void VirtualCallStubManager::ResetCache()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        FORBID_FAULT;
    }
    CONTRACTL_END

    g_resolveCache->LogStats();

    g_insert_cache_external = 0;
    g_insert_cache_shared = 0;
    g_insert_cache_dispatch = 0;
    g_insert_cache_resolve = 0;
    g_insert_cache_hit = 0;
    g_insert_cache_miss = 0;
    g_insert_cache_collide = 0;
    g_insert_cache_write = 0;

    // Go through each cache entry and if the cache element there is in
    // the cache entry heap of the manager being deleted, then we just
    // set the cache entry to empty.
    DispatchCache::Iterator it(g_resolveCache);
    while (it.IsValid())
    {
        it.UnlinkEntry();
    }

}

void VirtualCallStubManager::Init(BaseDomain *pDomain, LoaderAllocator *pLoaderAllocator)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(CheckPointer(pDomain));
        INJECT_FAULT(COMPlusThrowOM(););
    } CONTRACTL_END;

    // Record the parent domain
    parentDomain        = pDomain;
    m_loaderAllocator   = pLoaderAllocator;

    //
    // Init critical sections
    //

    m_indCellLock.Init(CrstVSDIndirectionCellLock, CRST_UNSAFE_ANYMODE);

    //
    // Now allocate all BucketTables
    //

    NewHolder<BucketTable> resolvers_holder(new BucketTable(CALL_STUB_MIN_BUCKETS));
    NewHolder<BucketTable> dispatchers_holder(new BucketTable(CALL_STUB_MIN_BUCKETS*2));
    NewHolder<BucketTable> lookups_holder(new BucketTable(CALL_STUB_MIN_BUCKETS));
    NewHolder<BucketTable> vtableCallers_holder(new BucketTable(CALL_STUB_MIN_BUCKETS));
    NewHolder<BucketTable> cache_entries_holder(new BucketTable(CALL_STUB_MIN_BUCKETS));

    //
    // Now allocate our LoaderHeaps
    //

    //
    // First do some calculation to determine how many pages that we
    // will need to commit and reserve for each out our loader heaps
    //
    DWORD indcell_heap_reserve_size;
    DWORD indcell_heap_commit_size;
    DWORD cache_entry_heap_reserve_size;
    DWORD cache_entry_heap_commit_size;
    DWORD lookup_heap_reserve_size;
    DWORD lookup_heap_commit_size;
    DWORD dispatch_heap_reserve_size;
    DWORD dispatch_heap_commit_size;
    DWORD resolve_heap_reserve_size;
    DWORD resolve_heap_commit_size;
    DWORD vtable_heap_reserve_size;
    DWORD vtable_heap_commit_size;

    //
    // Setup an expected number of items to commit and reserve
    //
    // The commit number is not that important as we always commit at least one page worth of items
    // The reserve number should be high enough to cover a typical lare application,
    // in order to minimize the fragmentation of our rangelists
    //

    indcell_heap_commit_size     = 16;        indcell_heap_reserve_size      = 2000;
    cache_entry_heap_commit_size = 16;        cache_entry_heap_reserve_size  =  800;

    lookup_heap_commit_size      = 24;        lookup_heap_reserve_size       =  250;
    dispatch_heap_commit_size    = 24;        dispatch_heap_reserve_size     =  600;
    resolve_heap_commit_size     = 24;        resolve_heap_reserve_size      =  300;
    vtable_heap_commit_size      = 24;        vtable_heap_reserve_size       =  600;

#ifdef HOST_64BIT
    // If we're on 64-bit, there's a ton of address space, so reserve more space to
    // try to avoid getting into the situation where the resolve heap is more than
    // a rel32 jump away from the dispatch heap, since this will cause us to produce
    // larger dispatch stubs on AMD64.
    dispatch_heap_reserve_size      *= 10;
    resolve_heap_reserve_size       *= 10;
#endif

    //
    // Convert the number of items into a size in bytes to commit and reserve
    //
    indcell_heap_reserve_size       *= sizeof(void *);
    indcell_heap_commit_size        *= sizeof(void *);

    cache_entry_heap_reserve_size   *= sizeof(ResolveCacheElem);
    cache_entry_heap_commit_size    *= sizeof(ResolveCacheElem);

    lookup_heap_reserve_size        *= sizeof(LookupHolder);
    lookup_heap_commit_size         *= sizeof(LookupHolder);

    DWORD dispatchHolderSize        = sizeof(DispatchHolder);
#ifdef TARGET_AMD64
    dispatchHolderSize               = static_cast<DWORD>(DispatchHolder::GetHolderSize(DispatchStub::e_TYPE_SHORT));
#endif

    dispatch_heap_reserve_size      *= dispatchHolderSize;
    dispatch_heap_commit_size       *= dispatchHolderSize;

    resolve_heap_reserve_size       *= sizeof(ResolveHolder);
    resolve_heap_commit_size        *= sizeof(ResolveHolder);

    vtable_heap_reserve_size       *= static_cast<DWORD>(VTableCallHolder::GetHolderSize(0));
    vtable_heap_commit_size        *= static_cast<DWORD>(VTableCallHolder::GetHolderSize(0));

    //
    // Align up all of the commit and reserve sizes
    //
    indcell_heap_reserve_size        = (DWORD) ALIGN_UP(indcell_heap_reserve_size,     GetOsPageSize());
    indcell_heap_commit_size         = (DWORD) ALIGN_UP(indcell_heap_commit_size,      GetOsPageSize());

    cache_entry_heap_reserve_size    = (DWORD) ALIGN_UP(cache_entry_heap_reserve_size, GetOsPageSize());
    cache_entry_heap_commit_size     = (DWORD) ALIGN_UP(cache_entry_heap_commit_size,  GetOsPageSize());

    lookup_heap_reserve_size         = (DWORD) ALIGN_UP(lookup_heap_reserve_size,      GetOsPageSize());
    lookup_heap_commit_size          = (DWORD) ALIGN_UP(lookup_heap_commit_size,       GetOsPageSize());

    dispatch_heap_reserve_size       = (DWORD) ALIGN_UP(dispatch_heap_reserve_size,    GetOsPageSize());
    dispatch_heap_commit_size        = (DWORD) ALIGN_UP(dispatch_heap_commit_size,     GetOsPageSize());

    resolve_heap_reserve_size        = (DWORD) ALIGN_UP(resolve_heap_reserve_size,     GetOsPageSize());
    resolve_heap_commit_size         = (DWORD) ALIGN_UP(resolve_heap_commit_size,      GetOsPageSize());

    vtable_heap_reserve_size         = (DWORD) ALIGN_UP(vtable_heap_reserve_size,      GetOsPageSize());
    vtable_heap_commit_size          = (DWORD) ALIGN_UP(vtable_heap_commit_size,       GetOsPageSize());

    BYTE * initReservedMem = NULL;

    if (!m_loaderAllocator->IsCollectible())
    {
        DWORD dwTotalReserveMemSizeCalc  = indcell_heap_reserve_size     +
                                           cache_entry_heap_reserve_size +
                                           lookup_heap_reserve_size      +
                                           dispatch_heap_reserve_size    +
                                           resolve_heap_reserve_size     +
                                           vtable_heap_reserve_size;

        DWORD dwTotalReserveMemSize = (DWORD) ALIGN_UP(dwTotalReserveMemSizeCalc, VIRTUAL_ALLOC_RESERVE_GRANULARITY);

        // If there's wasted reserved memory, we hand this out to the heaps to avoid waste.
        {
            DWORD dwWastedReserveMemSize = dwTotalReserveMemSize - dwTotalReserveMemSizeCalc;
            if (dwWastedReserveMemSize != 0)
            {
                DWORD cWastedPages = dwWastedReserveMemSize / GetOsPageSize();
                DWORD cPagesPerHeap = cWastedPages / 6;
                DWORD cPagesRemainder = cWastedPages % 6; // We'll throw this at the resolve heap

                indcell_heap_reserve_size += cPagesPerHeap * GetOsPageSize();
                cache_entry_heap_reserve_size += cPagesPerHeap * GetOsPageSize();
                lookup_heap_reserve_size += cPagesPerHeap * GetOsPageSize();
                dispatch_heap_reserve_size += cPagesPerHeap * GetOsPageSize();
                vtable_heap_reserve_size += cPagesPerHeap * GetOsPageSize();
                resolve_heap_reserve_size += cPagesPerHeap * GetOsPageSize();
                resolve_heap_reserve_size += cPagesRemainder * GetOsPageSize();
            }

            CONSISTENCY_CHECK((indcell_heap_reserve_size     +
                               cache_entry_heap_reserve_size +
                               lookup_heap_reserve_size      +
                               dispatch_heap_reserve_size    +
                               resolve_heap_reserve_size     +
                               vtable_heap_reserve_size)    ==
                              dwTotalReserveMemSize);
        }

        initReservedMem = (BYTE*)ExecutableAllocator::Instance()->Reserve(dwTotalReserveMemSize);

        m_initialReservedMemForHeaps = (BYTE *) initReservedMem;

        if (initReservedMem == NULL)
            COMPlusThrowOM();
    }
    else
    {
        indcell_heap_reserve_size        = GetOsPageSize();
        indcell_heap_commit_size         = GetOsPageSize();

        cache_entry_heap_reserve_size    = GetOsPageSize();
        cache_entry_heap_commit_size     = GetOsPageSize();

        lookup_heap_reserve_size         = GetOsPageSize();
        lookup_heap_commit_size          = GetOsPageSize();

        dispatch_heap_reserve_size       = GetOsPageSize();
        dispatch_heap_commit_size        = GetOsPageSize();

        resolve_heap_reserve_size        = GetOsPageSize();
        resolve_heap_commit_size         = GetOsPageSize();

        // Heap for the collectible case is carefully tuned to sum up to 16 pages. Today, we only use the
        // vtable jump stubs in the R2R scenario, which is unlikely to be loaded in the collectible context,
        // so we'll keep the heap numbers at zero for now. If we ever use vtable stubs in the collectible
        // scenario, we'll just allocate the memory on demand.
        vtable_heap_reserve_size         = 0;
        vtable_heap_commit_size          = 0;

#ifdef _DEBUG
        DWORD dwTotalReserveMemSizeCalc  = indcell_heap_reserve_size     +
                                           cache_entry_heap_reserve_size +
                                           lookup_heap_reserve_size      +
                                           dispatch_heap_reserve_size    +
                                           resolve_heap_reserve_size     +
                                           vtable_heap_reserve_size;
#endif

        DWORD dwActualVSDSize = 0;

        initReservedMem = pLoaderAllocator->GetVSDHeapInitialBlock(&dwActualVSDSize);
        _ASSERTE(dwActualVSDSize == dwTotalReserveMemSizeCalc);

        m_initialReservedMemForHeaps = (BYTE *) initReservedMem;

        if (initReservedMem == NULL)
            COMPlusThrowOM();
    }

    // Hot  memory, Writable, No-Execute, infrequent writes
    NewHolder<LoaderHeap> indcell_heap_holder(
                               new LoaderHeap(indcell_heap_reserve_size, indcell_heap_commit_size,
                                              initReservedMem, indcell_heap_reserve_size,
                                              NULL, UnlockedLoaderHeap::HeapKind::Data));

    initReservedMem += indcell_heap_reserve_size;

    // Hot  memory, Writable, No-Execute, infrequent writes
    NewHolder<LoaderHeap> cache_entry_heap_holder(
                               new LoaderHeap(cache_entry_heap_reserve_size, cache_entry_heap_commit_size,
                                              initReservedMem, cache_entry_heap_reserve_size,
                                              &cache_entry_rangeList, UnlockedLoaderHeap::HeapKind::Data));

    initReservedMem += cache_entry_heap_reserve_size;

    // Warm memory, Writable, Execute, write exactly once
    NewHolder<LoaderHeap> lookup_heap_holder(
                               new LoaderHeap(lookup_heap_reserve_size, lookup_heap_commit_size,
                                              initReservedMem, lookup_heap_reserve_size,
                                              &lookup_rangeList, UnlockedLoaderHeap::HeapKind::Executable));

    initReservedMem += lookup_heap_reserve_size;

    // Hot  memory, Writable, Execute, write exactly once
    NewHolder<LoaderHeap> dispatch_heap_holder(
                               new LoaderHeap(dispatch_heap_reserve_size, dispatch_heap_commit_size,
                                              initReservedMem, dispatch_heap_reserve_size,
                                              &dispatch_rangeList, UnlockedLoaderHeap::HeapKind::Executable));

    initReservedMem += dispatch_heap_reserve_size;

    // Hot  memory, Writable, Execute, write exactly once
    NewHolder<LoaderHeap> resolve_heap_holder(
                               new LoaderHeap(resolve_heap_reserve_size, resolve_heap_commit_size,
                                              initReservedMem, resolve_heap_reserve_size,
                                              &resolve_rangeList, UnlockedLoaderHeap::HeapKind::Executable));

    initReservedMem += resolve_heap_reserve_size;

    // Hot  memory, Writable, Execute, write exactly once
    NewHolder<LoaderHeap> vtable_heap_holder(
                               new LoaderHeap(vtable_heap_reserve_size, vtable_heap_commit_size,
                                              initReservedMem, vtable_heap_reserve_size,
                                              &vtable_rangeList, UnlockedLoaderHeap::HeapKind::Executable));

    initReservedMem += vtable_heap_reserve_size;

    // Allocate the initial counter block
    NewHolder<counter_block> m_counters_holder(new counter_block);

    //
    // On success of every allocation, assign the objects and suppress the release
    //

    indcell_heap     = indcell_heap_holder;     indcell_heap_holder.SuppressRelease();
    lookup_heap      = lookup_heap_holder;      lookup_heap_holder.SuppressRelease();
    dispatch_heap    = dispatch_heap_holder;    dispatch_heap_holder.SuppressRelease();
    resolve_heap     = resolve_heap_holder;     resolve_heap_holder.SuppressRelease();
    vtable_heap      = vtable_heap_holder;      vtable_heap_holder.SuppressRelease();
    cache_entry_heap = cache_entry_heap_holder; cache_entry_heap_holder.SuppressRelease();

    resolvers        = resolvers_holder;        resolvers_holder.SuppressRelease();
    dispatchers      = dispatchers_holder;      dispatchers_holder.SuppressRelease();
    lookups          = lookups_holder;          lookups_holder.SuppressRelease();
    vtableCallers    = vtableCallers_holder;    vtableCallers_holder.SuppressRelease();
    cache_entries    = cache_entries_holder;    cache_entries_holder.SuppressRelease();

    m_counters       = m_counters_holder;       m_counters_holder.SuppressRelease();

    // Create the initial failure counter block
    m_counters->next = NULL;
    m_counters->used = 0;
    m_cur_counter_block = m_counters;

    m_cur_counter_block_for_reclaim = m_counters;
    m_cur_counter_block_for_reclaim_index = 0;

    // Keep track of all of our managers
    VirtualCallStubManagerManager::GlobalManager()->AddStubManager(this);
}

void VirtualCallStubManager::Uninit()
{
    WRAPPER_NO_CONTRACT;

    if (m_loaderAllocator->IsCollectible())
    {
        parentDomain->GetCollectibleVSDRanges()->RemoveRanges(this);
    }

    // Keep track of all our managers
    VirtualCallStubManagerManager::GlobalManager()->RemoveStubManager(this);
}

VirtualCallStubManager::~VirtualCallStubManager()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
    } CONTRACTL_END;

    LogStats();

    // Go through each cache entry and if the cache element there is in
    // the cache entry heap of the manager being deleted, then we just
    // set the cache entry to empty.
    DispatchCache::Iterator it(g_resolveCache);
    while (it.IsValid())
    {
        // Using UnlinkEntry performs an implicit call to Next (see comment for UnlinkEntry).
        // Thus, we need to avoid calling Next when we delete an entry so
        // that we don't accidentally skip entries.
        while (it.IsValid() && cache_entry_rangeList.IsInRange((TADDR)it.Entry()))
        {
            it.UnlinkEntry();
        }
        it.Next();
    }

    if (indcell_heap)     { delete indcell_heap;     indcell_heap     = NULL;}
    if (lookup_heap)      { delete lookup_heap;      lookup_heap      = NULL;}
    if (dispatch_heap)    { delete dispatch_heap;    dispatch_heap    = NULL;}
    if (resolve_heap)     { delete resolve_heap;     resolve_heap     = NULL;}
    if (vtable_heap)      { delete vtable_heap;      vtable_heap      = NULL;}
    if (cache_entry_heap) { delete cache_entry_heap; cache_entry_heap = NULL;}

    if (resolvers)        { delete resolvers;        resolvers        = NULL;}
    if (dispatchers)      { delete dispatchers;      dispatchers      = NULL;}
    if (lookups)          { delete lookups;          lookups          = NULL;}
    if (vtableCallers)    { delete vtableCallers;    vtableCallers    = NULL;}
    if (cache_entries)    { delete cache_entries;    cache_entries    = NULL;}

    // Now get rid of the memory taken by the counter_blocks
    while (m_counters != NULL)
    {
        counter_block *del = m_counters;
        m_counters = m_counters->next;
        delete del;
    }

    // This was the block reserved by Init for the heaps.
    // For the collectible case, the VSD logic does not allocate the memory.
    if (m_initialReservedMemForHeaps && !m_loaderAllocator->IsCollectible())
        ClrVirtualFree (m_initialReservedMemForHeaps, 0, MEM_RELEASE);

    // Free critical section
    m_indCellLock.Destroy();
}

// Initialize static structures, and start up logging if necessary
void VirtualCallStubManager::InitStatic()
{
    STANDARD_VM_CONTRACT;

#ifdef STUB_LOGGING
    // Note if you change these values using environment variables then you must use hex values :-(
    STUB_MISS_COUNT_VALUE  = (INT32) CLRConfig::GetConfigValue(CLRConfig::INTERNAL_VirtualCallStubMissCount);
    STUB_COLLIDE_WRITE_PCT = (INT32) CLRConfig::GetConfigValue(CLRConfig::INTERNAL_VirtualCallStubCollideWritePct);
    STUB_COLLIDE_MONO_PCT  = (INT32) CLRConfig::GetConfigValue(CLRConfig::INTERNAL_VirtualCallStubCollideMonoPct);
    g_dumpLogCounter       = (INT32) CLRConfig::GetConfigValue(CLRConfig::INTERNAL_VirtualCallStubDumpLogCounter);
    g_dumpLogIncr          = (INT32) CLRConfig::GetConfigValue(CLRConfig::INTERNAL_VirtualCallStubDumpLogIncr);
    g_resetCacheCounter    = (INT32) CLRConfig::GetConfigValue(CLRConfig::INTERNAL_VirtualCallStubResetCacheCounter);
    g_resetCacheIncr       = (INT32) CLRConfig::GetConfigValue(CLRConfig::INTERNAL_VirtualCallStubResetCacheIncr);
#endif // STUB_LOGGING

#ifndef STUB_DISPATCH_PORTABLE
    DispatchHolder::InitializeStatic();
    ResolveHolder::InitializeStatic();
#endif // !STUB_DISPATCH_PORTABLE
    LookupHolder::InitializeStatic();

    g_resolveCache = new DispatchCache();

    if(CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_VirtualCallStubLogging))
        StartupLogging();

    VirtualCallStubManagerManager::InitStatic();
}

// Static shutdown code.
// At the moment, this doesn't do anything more than log statistics.
void VirtualCallStubManager::UninitStatic()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        FORBID_FAULT;
    }
    CONTRACTL_END

    if (g_hStubLogFile != NULL)
    {
        VirtualCallStubManagerIterator it =
            VirtualCallStubManagerManager::GlobalManager()->IterateVirtualCallStubManagers();
        while (it.Next())
        {
            it.Current()->LogStats();
        }

        g_resolveCache->LogStats();

        FinishLogging();
    }
}

/* reclaim/rearrange any structures that can only be done during a gc sync point
i.e. need to be serialized and non-concurrant. */
void VirtualCallStubManager::ReclaimAll()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    /* @todo: if/when app domain unloading is supported,
    and when we have app domain specific stub heaps, we can complete the unloading
    of an app domain stub heap at this point, and make any patches to existing stubs that are
    not being unload so that they nolonger refer to any of the unloaded app domains code or types
    */

    //reclaim space of abandoned buckets
    BucketTable::Reclaim();

    VirtualCallStubManagerIterator it =
        VirtualCallStubManagerManager::GlobalManager()->IterateVirtualCallStubManagers();
    while (it.Next())
    {
        it.Current()->Reclaim();
    }

    g_reclaim_counter++;
}

/* reclaim/rearrange any structures that can only be done during a gc sync point
i.e. need to be serialized and non-concurrant. */
void VirtualCallStubManager::Reclaim()
{
    LIMITED_METHOD_CONTRACT;

    UINT32 limit = min(counter_block::MAX_COUNTER_ENTRIES,
                                  m_cur_counter_block_for_reclaim->used);
    limit = min(m_cur_counter_block_for_reclaim_index + 16,  limit);

    for (UINT32 i = m_cur_counter_block_for_reclaim_index; i < limit; i++)
    {
        m_cur_counter_block_for_reclaim->block[i] += (STUB_MISS_COUNT_VALUE/10)+1;
    }

    // Increment the index by the number we processed
    m_cur_counter_block_for_reclaim_index = limit;

    // If we ran to the end of the block, go to the next
    if (m_cur_counter_block_for_reclaim_index == m_cur_counter_block->used)
    {
        m_cur_counter_block_for_reclaim = m_cur_counter_block_for_reclaim->next;
        m_cur_counter_block_for_reclaim_index = 0;

        // If this was the last block in the chain, go back to the beginning
        if (m_cur_counter_block_for_reclaim == NULL)
            m_cur_counter_block_for_reclaim = m_counters;
    }
}

#endif // !DACCESS_COMPILE

//----------------------------------------------------------------------------
/* static */
VirtualCallStubManager *VirtualCallStubManager::FindStubManager(PCODE stubAddress,  StubKind* wbStubKind, BOOL usePredictStubKind)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
    } CONTRACTL_END

#ifndef DACCESS_COMPILE
    VirtualCallStubManager *pCur;
    StubKind kind;

    //
    // See if we are managed by the current domain
    //
    AppDomain *pDomain = GetThread()->GetDomain();
    pCur = pDomain->GetLoaderAllocator()->GetVirtualCallStubManager();
    // For the following call stack:
    // SimpleRWLock::TryEnterRead
    // SimpleRWLock::EnterRead
    // LockedRangeList::IsInRangeWorker
    // VirtualCallStubManager::isDispatchingStub
    //
    kind = pCur->getStubKind(stubAddress, usePredictStubKind);
    if (kind != SK_UNKNOWN)
    {
        if (wbStubKind)
            *wbStubKind = kind;
        return pCur;
    }

    //
    // See if we are managed by a collectible loader allocator
    //
    if (pDomain->GetCollectibleVSDRanges()->IsInRange(stubAddress, reinterpret_cast<TADDR *>(&pCur)))
    {
        _ASSERTE(pCur != NULL);

        kind = pCur->getStubKind(stubAddress, usePredictStubKind);
        if (kind != SK_UNKNOWN)
        {
            if (wbStubKind)
                *wbStubKind = kind;
            return pCur;
        }
    }

    if (wbStubKind)
        *wbStubKind = SK_UNKNOWN;

#else // DACCESS_COMPILE
    _ASSERTE(!"DACCESS Not implemented.");
#endif // DACCESS_COMPILE

    return NULL;
}

/* for use by debugger.
*/
BOOL VirtualCallStubManager::CheckIsStub_Internal(PCODE stubStartAddress)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;
    SUPPORTS_DAC;

    BOOL fIsOwner = isStub(stubStartAddress);

    return fIsOwner;
}

/* for use by debugger.
*/

extern "C" void STDCALL StubDispatchFixupPatchLabel();

BOOL VirtualCallStubManager::DoTraceStub(PCODE stubStartAddress, TraceDestination *trace)
{
    LIMITED_METHOD_CONTRACT;

    LOG((LF_CORDB, LL_EVERYTHING, "VirtualCallStubManager::DoTraceStub called\n"));

    _ASSERTE(CheckIsStub_Internal(stubStartAddress));

    // @workaround: Well, we really need the context to figure out where we're going, so
    // we'll do a TRACE_MGR_PUSH so that TraceManager gets called and we can use
    // the provided context to figure out where we're going.
    trace->InitForManagerPush(stubStartAddress, this);
    return TRUE;
}

//----------------------------------------------------------------------------
BOOL VirtualCallStubManager::TraceManager(Thread *thread,
                              TraceDestination *trace,
                              T_CONTEXT *pContext,
                              BYTE **pRetAddr)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END

    TADDR pStub = GetIP(pContext);

    // The return address should be on the top of the stack
    *pRetAddr = (BYTE *)StubManagerHelpers::GetReturnAddress(pContext);

    // Get the token from the stub
    CONSISTENCY_CHECK(isStub(pStub));
    DispatchToken token(GetTokenFromStub(pStub));

    // Get the this object from ECX
    Object *pObj = StubManagerHelpers::GetThisPtr(pContext);

    // Call common trace code.
    return (TraceResolver(pObj, token, trace));
}

#ifndef DACCESS_COMPILE

PCODE VirtualCallStubManager::GetCallStub(TypeHandle ownerType, MethodDesc *pMD)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMD));
        PRECONDITION(!pMD->IsInterface() || ownerType.GetMethodTable()->HasSameTypeDefAs(pMD->GetMethodTable()));
        INJECT_FAULT(COMPlusThrowOM(););
    } CONTRACTL_END;

    return GetCallStub(ownerType, pMD->GetSlot());
}

//find or create a stub
PCODE VirtualCallStubManager::GetCallStub(TypeHandle ownerType, DWORD slot)
{
    CONTRACT (PCODE) {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
        POSTCONDITION(RETVAL != NULL);
    } CONTRACT_END;

    GCX_COOP(); // This is necessary for BucketTable synchronization

    MethodTable * pMT = ownerType.GetMethodTable();

    DispatchToken token;
    if (pMT->IsInterface())
        token = pMT->GetLoaderAllocator()->GetDispatchToken(pMT->GetTypeID(), slot);
    else
        token = DispatchToken::CreateDispatchToken(slot);

    //get a stub from lookups, make if necessary
    PCODE stub = CALL_STUB_EMPTY_ENTRY;
    PCODE addrOfResolver = GetEEFuncEntryPoint(ResolveWorkerAsmStub);

    LookupEntry entryL;
    Prober probeL(&entryL);
    if (lookups->SetUpProber(token.To_SIZE_T(), 0, &probeL))
    {
        if ((stub = (PCODE)(lookups->Find(&probeL))) == CALL_STUB_EMPTY_ENTRY)
        {
            LookupHolder *pLookupHolder = GenerateLookupStub(addrOfResolver, token.To_SIZE_T());
            stub = (PCODE) (lookups->Add((size_t)(pLookupHolder->stub()->entryPoint()), &probeL));
        }
    }

    _ASSERTE(stub != CALL_STUB_EMPTY_ENTRY);
    stats.site_counter++;

    RETURN (stub);
}

PCODE VirtualCallStubManager::GetVTableCallStub(DWORD slot)
{
    CONTRACT(PCODE) {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
        POSTCONDITION(RETVAL != NULL);
    } CONTRACT_END;

    GCX_COOP(); // This is necessary for BucketTable synchronization

    PCODE stub = CALL_STUB_EMPTY_ENTRY;

    VTableCallEntry entry;
    Prober probe(&entry);
    if (vtableCallers->SetUpProber(DispatchToken::CreateDispatchToken(slot).To_SIZE_T(), 0, &probe))
    {
        if ((stub = (PCODE)(vtableCallers->Find(&probe))) == CALL_STUB_EMPTY_ENTRY)
        {
            VTableCallHolder *pHolder = GenerateVTableCallStub(slot);
            stub = (PCODE)(vtableCallers->Add((size_t)(pHolder->stub()->entryPoint()), &probe));
        }
    }

    _ASSERTE(stub != CALL_STUB_EMPTY_ENTRY);
    RETURN(stub);
}

VTableCallHolder* VirtualCallStubManager::GenerateVTableCallStub(DWORD slot)
{
    CONTRACT(VTableCallHolder*) {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
        POSTCONDITION(RETVAL != NULL);
    } CONTRACT_END;

    //allocate from the requisite heap and copy the template over it.
    size_t vtableHolderSize = VTableCallHolder::GetHolderSize(slot);
    VTableCallHolder * pHolder = (VTableCallHolder*)(void*)vtable_heap->AllocAlignedMem(vtableHolderSize, CODE_SIZE_ALIGN);
    ExecutableWriterHolder<VTableCallHolder> vtableWriterHolder(pHolder, vtableHolderSize);
    vtableWriterHolder.GetRW()->Initialize(slot);

    ClrFlushInstructionCache(pHolder->stub(), pHolder->stub()->size());

    AddToCollectibleVSDRangeList(pHolder);

    //incr our counters
    stats.stub_vtable_counter++;
    stats.stub_space += (UINT32)pHolder->stub()->size();
    LOG((LF_STUBS, LL_INFO10000, "GenerateVTableCallStub for slot " FMT_ADDR "at" FMT_ADDR "\n",
        DBG_ADDR(slot), DBG_ADDR(pHolder->stub())));

#ifdef FEATURE_PERFMAP
    PerfMap::LogStubs(__FUNCTION__, "GenerateVTableCallStub", (PCODE)pHolder->stub(), pHolder->stub()->size());
#endif

    RETURN(pHolder);
}

//+----------------------------------------------------------------------------
//
//  Method:     VirtualCallStubManager::GenerateStubIndirection
//
//  Synopsis:   This method allocates an indirection cell for use by the virtual stub dispatch (currently
//              only implemented for interface calls).
//              For normal methods: the indirection cell allocated will never be freed until app domain unload
//              For dynamic methods: we recycle the indirection cells when a dynamic method is collected. To
//              do that we keep all the recycled indirection cells in a linked list: m_RecycledIndCellList. When
//              the dynamic method needs an indirection cell it allocates one from m_RecycledIndCellList. Each
//              dynamic method keeps track of all the indirection cells it uses and add them back to
//              m_RecycledIndCellList when it is finalized.
//
//+----------------------------------------------------------------------------
BYTE *VirtualCallStubManager::GenerateStubIndirection(PCODE target, BOOL fUseRecycledCell /* = FALSE*/ )
{
    CONTRACT (BYTE*) {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
        PRECONDITION(target != NULL);
        POSTCONDITION(CheckPointer(RETVAL));
    } CONTRACT_END;

    _ASSERTE(isStub(target));

    CrstHolder lh(&m_indCellLock);

    // The indirection cell to hold the pointer to the stub
    BYTE * ret              = NULL;
    UINT32 cellsPerBlock    = INDCELLS_PER_BLOCK;

    // First try the recycled indirection cell list for Dynamic methods
    if (fUseRecycledCell)
        ret = GetOneRecycledIndCell();

    // Try the free indirection cell list
    if (!ret)
        ret = GetOneFreeIndCell();

    // Allocate from loader heap
    if (!ret)
    {
        // Free list is empty, allocate a block of indcells from indcell_heap and insert it into the free list.
        BYTE ** pBlock = (BYTE **) (void *) indcell_heap->AllocMem(S_SIZE_T(cellsPerBlock) * S_SIZE_T(sizeof(BYTE *)));

        // return the first cell in the block and add the rest to the free list
        ret = (BYTE *)pBlock;

        // link all the cells together
        // we don't need to null terminate the linked list, InsertIntoFreeIndCellList will do it.
        for (UINT32 i = 1; i < cellsPerBlock - 1; ++i)
        {
            pBlock[i] = (BYTE *)&(pBlock[i+1]);
        }

        // insert the list into the free indcell list.
        InsertIntoFreeIndCellList((BYTE *)&pBlock[1], (BYTE*)&pBlock[cellsPerBlock - 1]);
    }

    *((PCODE *)ret) = target;
    RETURN ret;
}

ResolveCacheElem *VirtualCallStubManager::GetResolveCacheElem(void *pMT,
                                                              size_t token,
                                                              void *target)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END

    //get an cache entry elem, or make one if necessary
    ResolveCacheElem* elem = NULL;
    ResolveCacheEntry entryRC;
    Prober probeRC(&entryRC);
    if (cache_entries->SetUpProber(token, (size_t) pMT, &probeRC))
    {
        elem = (ResolveCacheElem*) (cache_entries->Find(&probeRC));
        if (elem  == CALL_STUB_EMPTY_ENTRY)
        {
            bool reenteredCooperativeGCMode = false;
            elem = GenerateResolveCacheElem(target, pMT, token, &reenteredCooperativeGCMode);
            if (reenteredCooperativeGCMode)
            {
                // The prober may have been invalidated by reentering cooperative GC mode, reset it
                BOOL success = cache_entries->SetUpProber(token, (size_t)pMT, &probeRC);
                _ASSERTE(success);
            }
            elem = (ResolveCacheElem*) (cache_entries->Add((size_t) elem, &probeRC));
        }
    }
    _ASSERTE(elem && (elem != CALL_STUB_EMPTY_ENTRY));
    return elem;
}

#endif // !DACCESS_COMPILE

size_t VirtualCallStubManager::GetTokenFromStub(PCODE stub)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
    }
    CONTRACTL_END

    _ASSERTE(stub != NULL);
    StubKind                  stubKind = SK_UNKNOWN;
    VirtualCallStubManager *  pMgr     = FindStubManager(stub, &stubKind);

    return GetTokenFromStubQuick(pMgr, stub, stubKind);
}

size_t VirtualCallStubManager::GetTokenFromStubQuick(VirtualCallStubManager * pMgr, PCODE stub, StubKind kind)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
    }
    CONTRACTL_END

    _ASSERTE(pMgr != NULL);
    _ASSERTE(stub != NULL);
    _ASSERTE(kind != SK_UNKNOWN);

#ifndef DACCESS_COMPILE

    if (kind == SK_DISPATCH)
    {
        _ASSERTE(pMgr->isDispatchingStub(stub));
        DispatchStub  * dispatchStub  = (DispatchStub *) PCODEToPINSTR(stub);
        ResolveHolder * resolveHolder = ResolveHolder::FromFailEntry(dispatchStub->failTarget());
        _ASSERTE(pMgr->isResolvingStub(resolveHolder->stub()->resolveEntryPoint()));
        return resolveHolder->stub()->token();
    }
    else if (kind == SK_RESOLVE)
    {
        _ASSERTE(pMgr->isResolvingStub(stub));
        ResolveHolder * resolveHolder = ResolveHolder::FromResolveEntry(stub);
        return resolveHolder->stub()->token();
    }
    else if (kind == SK_LOOKUP)
    {
        _ASSERTE(pMgr->isLookupStub(stub));
        LookupHolder  * lookupHolder  = LookupHolder::FromLookupEntry(stub);
        return lookupHolder->stub()->token();
    }
    else if (kind == SK_VTABLECALL)
    {
        _ASSERTE(pMgr->isVTableCallStub(stub));
        VTableCallStub * vtableStub = (VTableCallStub *)PCODEToPINSTR(stub);
        return vtableStub->token();
    }

    _ASSERTE(!"Should not get here.");

#else // DACCESS_COMPILE

    DacNotImpl();

#endif // DACCESS_COMPILE

    return 0;
}

#ifndef DACCESS_COMPILE

#ifdef CHAIN_LOOKUP
ResolveCacheElem* __fastcall VirtualCallStubManager::PromoteChainEntry(ResolveCacheElem *pElem)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        PRECONDITION(CheckPointer(pElem));
    } CONTRACTL_END;

    g_resolveCache->PromoteChainEntry(pElem);
    return pElem;
}
#endif // CHAIN_LOOKUP

/* Resolve to a method and return its address or NULL if there is none.
   Our return value is the target address that control should continue to.  Our caller will
   enter the target address as if a direct call with the original stack frame had been made from
   the actual call site.  Hence our strategy is to either return a target address
   of the actual method implementation, or the prestub if we cannot find the actual implementation.
   If we are returning a real method address, we may patch the original call site to point to a
   dispatching stub before returning.  Note, if we encounter a method that hasn't been jitted
   yet, we will return the prestub, which should cause it to be jitted and we will
   be able to build the dispatching stub on a later call thru the call site.  If we encounter
   any other kind of problem, rather than throwing an exception, we will also return the
   prestub, unless we are unable to find the method at all, in which case we return NULL.
   */
PCODE VSD_ResolveWorker(TransitionBlock * pTransitionBlock,
                        TADDR siteAddrForRegisterIndirect,
                        size_t token
#ifndef TARGET_X86
                        , UINT_PTR flags
#endif
                        )
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
        PRECONDITION(CheckPointer(pTransitionBlock));
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    MAKE_CURRENT_THREAD_AVAILABLE();

#ifdef _DEBUG
    Thread::ObjectRefFlush(CURRENT_THREAD);
#endif

    FrameWithCookie<StubDispatchFrame> frame(pTransitionBlock);
    StubDispatchFrame * pSDFrame = &frame;

    PCODE returnAddress = pSDFrame->GetUnadjustedReturnAddress();

    StubCallSite callSite(siteAddrForRegisterIndirect, returnAddress);

    OBJECTREF *protectedObj = pSDFrame->GetThisPtr();
    _ASSERTE(protectedObj != NULL);
    OBJECTREF pObj = *protectedObj;

    PCODE target = NULL;

    if (pObj == NULL) {
        pSDFrame->SetForNullReferenceException();
        pSDFrame->Push(CURRENT_THREAD);
        INSTALL_MANAGED_EXCEPTION_DISPATCHER;
        INSTALL_UNWIND_AND_CONTINUE_HANDLER;
        COMPlusThrow(kNullReferenceException);
        UNINSTALL_UNWIND_AND_CONTINUE_HANDLER;
        UNINSTALL_MANAGED_EXCEPTION_DISPATCHER;
        _ASSERTE(!"Throw returned");
    }

#ifndef TARGET_X86
    if (flags & SDF_ResolvePromoteChain)
    {
        ResolveCacheElem * pElem =  (ResolveCacheElem *)token;
        g_resolveCache->PromoteChainEntry(pElem);
        target = (PCODE) pElem->target;

        // Have we failed the dispatch stub too many times?
        if (flags & SDF_ResolveBackPatch)
        {
            PCODE stubAddr = callSite.GetSiteTarget();
            VirtualCallStubManager * pMgr = VirtualCallStubManager::FindStubManager(stubAddr);
            pMgr->BackPatchWorker(&callSite);
        }

        return target;
    }
#endif

    pSDFrame->SetCallSite(NULL, (TADDR)callSite.GetIndirectCell());

    DispatchToken representativeToken(token);
    MethodTable * pRepresentativeMT = pObj->GetMethodTable();
    if (representativeToken.IsTypedToken())
    {
        pRepresentativeMT = CURRENT_THREAD->GetDomain()->LookupType(representativeToken.GetTypeID());
        CONSISTENCY_CHECK(CheckPointer(pRepresentativeMT));
    }

    pSDFrame->SetRepresentativeSlot(pRepresentativeMT, representativeToken.GetSlotNumber());
    pSDFrame->Push(CURRENT_THREAD);
    INSTALL_MANAGED_EXCEPTION_DISPATCHER;
    INSTALL_UNWIND_AND_CONTINUE_HANDLER;

    // For Virtual Delegates the m_siteAddr is a field of a managed object
    // Thus we have to report it as an interior pointer,
    // so that it is updated during a gc
    GCPROTECT_BEGININTERIOR( *(callSite.GetIndirectCellAddress()) );

    GCStress<vsd_on_resolve>::MaybeTriggerAndProtect(pObj);

    PCODE callSiteTarget = callSite.GetSiteTarget();
    CONSISTENCY_CHECK(callSiteTarget != NULL);

    VirtualCallStubManager::StubKind stubKind = VirtualCallStubManager::SK_UNKNOWN;
    VirtualCallStubManager *pMgr = VirtualCallStubManager::FindStubManager(callSiteTarget, &stubKind);
    PREFIX_ASSUME(pMgr != NULL);

#ifndef TARGET_X86
    // Have we failed the dispatch stub too many times?
    if (flags & SDF_ResolveBackPatch)
    {
        pMgr->BackPatchWorker(&callSite);
    }
#endif

    target = pMgr->ResolveWorker(&callSite, protectedObj, representativeToken, stubKind);

#if _DEBUG
    if (pSDFrame->GetGCRefMap() != NULL)
    {
        GCX_PREEMP();
        _ASSERTE(CheckGCRefMapEqual(pSDFrame->GetGCRefMap(), pSDFrame->GetFunction(), true));
    }
#endif // _DEBUG

    GCPROTECT_END();

    UNINSTALL_UNWIND_AND_CONTINUE_HANDLER;
    UNINSTALL_MANAGED_EXCEPTION_DISPATCHER;
    pSDFrame->Pop(CURRENT_THREAD);

    return target;
}

void VirtualCallStubManager::BackPatchWorkerStatic(PCODE returnAddress, TADDR siteAddrForRegisterIndirect)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        ENTRY_POINT;
        PRECONDITION(returnAddress != NULL);
    } CONTRACTL_END

    BEGIN_ENTRYPOINT_VOIDRET;

    StubCallSite callSite(siteAddrForRegisterIndirect, returnAddress);

    PCODE callSiteTarget = callSite.GetSiteTarget();
    CONSISTENCY_CHECK(callSiteTarget != NULL);

    VirtualCallStubManager *pMgr = VirtualCallStubManager::FindStubManager(callSiteTarget);
    PREFIX_ASSUME(pMgr != NULL);

    pMgr->BackPatchWorker(&callSite);

    END_ENTRYPOINT_VOIDRET;
}

#if defined(TARGET_X86) && defined(TARGET_UNIX)
void BackPatchWorkerStaticStub(PCODE returnAddr, TADDR siteAddrForRegisterIndirect)
{
    VirtualCallStubManager::BackPatchWorkerStatic(returnAddr, siteAddrForRegisterIndirect);
}
#endif

PCODE VirtualCallStubManager::ResolveWorker(StubCallSite* pCallSite,
                                            OBJECTREF *protectedObj,
                                            DispatchToken token,
                                            StubKind stubKind)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM(););
        PRECONDITION(protectedObj != NULL);
        PRECONDITION(*protectedObj != NULL);
        PRECONDITION(IsProtectedByGCFrame(protectedObj));
    } CONTRACTL_END;

    MethodTable* objectType = (*protectedObj)->GetMethodTable();
    CONSISTENCY_CHECK(CheckPointer(objectType));

#ifdef STUB_LOGGING
    if (g_dumpLogCounter != 0)
    {
        UINT32 total_calls = g_mono_call_counter + g_poly_call_counter;

        if (total_calls > g_dumpLogCounter)
        {
            VirtualCallStubManager::LoggingDump();
            if (g_dumpLogIncr == 0)
                g_dumpLogCounter = 0;
            else
                g_dumpLogCounter += g_dumpLogIncr;
        }
    }

    if (g_resetCacheCounter != 0)
    {
        UINT32 total_calls = g_mono_call_counter + g_poly_call_counter;

        if (total_calls > g_resetCacheCounter)
        {
            VirtualCallStubManager::ResetCache();
            if (g_resetCacheIncr == 0)
                g_resetCacheCounter = 0;
            else
                g_resetCacheCounter += g_resetCacheIncr;
        }
    }
#endif // STUB_LOGGING

    //////////////////////////////////////////////////////////////
    // Get the managers associated with the callee

    VirtualCallStubManager *pCalleeMgr = NULL;  // Only set if the caller is shared, NULL otherwise

    BOOL bCallToShorterLivedTarget = FALSE;

    // We care about the following cases:
    // Call from any site -> collectible target
    if (objectType->GetLoaderAllocator()->IsCollectible())
    {
        // The callee's manager
        pCalleeMgr = objectType->GetLoaderAllocator()->GetVirtualCallStubManager();
        if (pCalleeMgr != this)
        {
            bCallToShorterLivedTarget = TRUE;
        }
        else
        {
            pCalleeMgr = NULL;
        }
    }

    stats.worker_call++;

    LOG((LF_STUBS, LL_INFO100000, "ResolveWorker from %sStub, token" FMT_ADDR "object's MT" FMT_ADDR  "ind-cell" FMT_ADDR "call-site" FMT_ADDR "%s\n",
         (stubKind == SK_DISPATCH) ? "Dispatch" : (stubKind == SK_RESOLVE) ? "Resolve" : (stubKind == SK_LOOKUP) ? "Lookup" : "Unknown",
         DBG_ADDR(token.To_SIZE_T()), DBG_ADDR(objectType), DBG_ADDR(pCallSite->GetIndirectCell()), DBG_ADDR(pCallSite->GetReturnAddress()),
         bCallToShorterLivedTarget ? "bCallToShorterLivedTarget" : "" ));

    PCODE stub = CALL_STUB_EMPTY_ENTRY;
    PCODE target = NULL;
    BOOL patch = FALSE;

    // This code can throw an OOM, but we do not want to fail in this case because
    // we must always successfully determine the target of a virtual call so that
    // CERs can work (there are a couple of exceptions to this involving generics).
    // Since the code below is just trying to see if a stub representing the current
    // type and token exist, it is not strictly necessary in determining the target.
    // We will treat the case of an OOM the same as the case of not finding an entry
    // in the hash tables and will continue on to the slow resolve case, which is
    // guaranteed not to fail outside of a couple of generics-specific cases.
    EX_TRY
    {
        /////////////////////////////////////////////////////////////////////////////
        // First see if we can find a dispatcher stub for this token and type. If a
        // match is found, use the target stored in the entry.
        {
            DispatchEntry entryD;
            Prober probeD(&entryD);
            if (dispatchers->SetUpProber(token.To_SIZE_T(), (size_t) objectType, &probeD))
            {
                stub = (PCODE) dispatchers->Find(&probeD);
                if (stub != CALL_STUB_EMPTY_ENTRY)
                {
                    target = (PCODE)entryD.Target();
                    patch = TRUE;
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////////////
        // Second see if we can find a ResolveCacheElem for this token and type.
        // If a match is found, use the target stored in the entry.
        if (target == NULL)
        {
            ResolveCacheElem * elem = NULL;
            ResolveCacheEntry entryRC;
            Prober probeRC(&entryRC);
            if (cache_entries->SetUpProber(token.To_SIZE_T(), (size_t) objectType, &probeRC))
            {
                elem = (ResolveCacheElem *)(cache_entries->Find(&probeRC));
                if (elem  != CALL_STUB_EMPTY_ENTRY)
                {
                    target = (PCODE)entryRC.Target();
                    patch  = TRUE;
                }
            }
        }
    }
    EX_CATCH
    {
    }
    EX_END_CATCH (SwallowAllExceptions);

    /////////////////////////////////////////////////////////////////////////////////////
    // If we failed to find a target in either the resolver or cache entry hash tables,
    // we need to perform a full resolution of the token and type.
    //@TODO: Would be nice to add assertion code to ensure we only ever call Resolver once per <token,type>.
    if (target == NULL)
    {
        CONSISTENCY_CHECK(stub == CALL_STUB_EMPTY_ENTRY);
        patch = Resolver(objectType, token, protectedObj, &target, TRUE /* throwOnConflict */);

#if defined(_DEBUG)
        if (!objectType->IsComObjectType()
            && !objectType->IsICastable()
            && !objectType->IsIDynamicInterfaceCastable())
        {
            CONSISTENCY_CHECK(!MethodTable::GetMethodDescForSlotAddress(target)->IsGenericMethodDefinition());
        }
#endif // _DEBUG
    }

    CONSISTENCY_CHECK(target != NULL);

    // Now that we've successfully determined the target, we will wrap the remaining logic in a giant
    // TRY/CATCH statement because it is there purely to emit stubs and cache entries. In the event
    // that emitting stub or cache entries throws an exception (for example, because of OOM), we should
    // not fail to perform the required dispatch. This is all because the basic assumption of
    // Constrained Execution Regions (CERs) is that all virtual method calls can be made without
    // failure.
    //
    // NOTE: The THROWS contract for this method does not change, because there are still a few special
    // cases involving generics that can throw when trying to determine the target method. These cases
    // are exceptional and will be documented as unsupported for CERs.
    //
    // NOTE: We do not try to keep track of the memory that has been allocated throughout this process
    // just so we can revert the memory should things fail. This is because we add the elements to the
    // hash tables and can be reused later on. Additionally, the hash tables are unlocked so we could
    // never remove the elements anyway.
    EX_TRY
    {
        // If we're the shared domain, we can't burn a dispatch stub to the target
        // if that target is outside the shared domain (through virtuals
        // originating in the shared domain but overridden by a non-shared type and
        // called on a collection, like HashTable would call GetHashCode on an
        // arbitrary object in its colletion). Dispatch stubs would be hard to clean,
        // but resolve stubs are easy to clean because we just clean the cache.
        //@TODO: Figure out how to track these indirection cells so that in the
        //@TODO: future we can create dispatch stubs for this case.
        BOOL bCreateDispatchStub = !bCallToShorterLivedTarget;

        DispatchCache::InsertKind insertKind = DispatchCache::IK_NONE;

        if (target != NULL)
        {
            if (patch)
            {
                // NOTE: This means that we are sharing dispatch stubs among callsites. If we decide we don't want
                // to do this in the future, just remove this condition
                if (stub == CALL_STUB_EMPTY_ENTRY)
                {
                    //we have a target but not the dispatcher stub, lets build it
                    //First we need a failure target (the resolver stub)
                    ResolveHolder *pResolveHolder = NULL;
                    ResolveEntry entryR;
                    Prober probeR(&entryR);
                    PCODE pBackPatchFcn;
                    PCODE pResolverFcn;

#ifdef TARGET_X86
                    // Only X86 implementation needs a BackPatch function
                    pBackPatchFcn = (PCODE) GetEEFuncEntryPoint(BackPatchWorkerAsmStub);
#else // !TARGET_X86
                    pBackPatchFcn = NULL;
#endif // !TARGET_X86

#ifdef CHAIN_LOOKUP
                    pResolverFcn  = (PCODE) GetEEFuncEntryPoint(ResolveWorkerChainLookupAsmStub);
#else // CHAIN_LOOKUP
                    // Use the slow resolver
                    pResolverFcn = (PCODE) GetEEFuncEntryPoint(ResolveWorkerAsmStub);
#endif

                    // First see if we've already created a resolve stub for this token
                    if (resolvers->SetUpProber(token.To_SIZE_T(), 0, &probeR))
                    {
                        // Find the right resolver, make it if necessary
                        PCODE addrOfResolver = (PCODE)(resolvers->Find(&probeR));
                        if (addrOfResolver == CALL_STUB_EMPTY_ENTRY)
                        {
#if defined(TARGET_X86) && !defined(UNIX_X86_ABI)
                            MethodDesc* pMD = VirtualCallStubManager::GetRepresentativeMethodDescFromToken(token, objectType);
                            size_t stackArgumentsSize;
                            {
                                ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE();
                                stackArgumentsSize = pMD->SizeOfArgStack();
                            }
#endif // TARGET_X86 && !UNIX_X86_ABI

                            pResolveHolder = GenerateResolveStub(pResolverFcn,
                                                             pBackPatchFcn,
                                                             token.To_SIZE_T()
#if defined(TARGET_X86) && !defined(UNIX_X86_ABI)
                                                             , stackArgumentsSize
#endif
                                                             );

                            // Add the resolve entrypoint into the cache.
                            //@TODO: Can we store a pointer to the holder rather than the entrypoint?
                            resolvers->Add((size_t)(pResolveHolder->stub()->resolveEntryPoint()), &probeR);
                        }
                        else
                        {
                            pResolveHolder = ResolveHolder::FromResolveEntry(addrOfResolver);
                        }
                        CONSISTENCY_CHECK(CheckPointer(pResolveHolder));
                        stub = pResolveHolder->stub()->resolveEntryPoint();
                        CONSISTENCY_CHECK(stub != NULL);
                    }

                    // Only create a dispatch stub if:
                    //  1. We successfully created or found a resolve stub.
                    //  2. We are not blocked from creating a dispatch stub.
                    //  3. The call site is currently wired to a lookup stub. If the call site is wired
                    //     to anything else, then we're never going to use the dispatch stub so there's
                    //     no use in creating it.
                    if (pResolveHolder != NULL && stubKind == SK_LOOKUP)
                    {
                        DispatchEntry entryD;
                        Prober probeD(&entryD);
                        if (bCreateDispatchStub &&
                            dispatchers->SetUpProber(token.To_SIZE_T(), (size_t) objectType, &probeD))
                        {
                            // We are allowed to create a reusable dispatch stub for all assemblies
                            // this allows us to optimize the call interception case the same way
                            DispatchHolder *pDispatchHolder = NULL;
                            PCODE addrOfDispatch = (PCODE)(dispatchers->Find(&probeD));
                            if (addrOfDispatch == CALL_STUB_EMPTY_ENTRY)
                            {
                                PCODE addrOfFail = pResolveHolder->stub()->failEntryPoint();
                                bool reenteredCooperativeGCMode = false;
                                pDispatchHolder = GenerateDispatchStub(
                                    target, addrOfFail, objectType, token.To_SIZE_T(), &reenteredCooperativeGCMode);
                                if (reenteredCooperativeGCMode)
                                {
                                    // The prober may have been invalidated by reentering cooperative GC mode, reset it
                                    BOOL success = dispatchers->SetUpProber(token.To_SIZE_T(), (size_t)objectType, &probeD);
                                    _ASSERTE(success);
                                }
                                dispatchers->Add((size_t)(pDispatchHolder->stub()->entryPoint()), &probeD);
                            }
                            else
                            {
                                pDispatchHolder = DispatchHolder::FromDispatchEntry(addrOfDispatch);
                            }

                            // Now assign the entrypoint to stub
                            CONSISTENCY_CHECK(CheckPointer(pDispatchHolder));
                            stub = pDispatchHolder->stub()->entryPoint();
                            CONSISTENCY_CHECK(stub != NULL);
                        }
                        else
                        {
                            insertKind = DispatchCache::IK_SHARED;
                        }
                    }
                }
            }
            else
            {
                stats.worker_call_no_patch++;
            }
        }

        // When we get here, target is where to go to
        // and patch is TRUE, telling us that we may have to back patch the call site with stub
        if (stub != CALL_STUB_EMPTY_ENTRY)
        {
            _ASSERTE(patch);

            // If we go here and have a dispatching stub in hand, it probably means
            // that the cache used by the resolve stubs (g_resolveCache) does not have this stub,
            // so insert it.
            //
            // We only insert into the cache if we have a ResolveStub or we have a DispatchStub
            // that missed, since we want to keep the resolve cache empty of unused entries.
            // If later the dispatch stub fails (because of another type at the call site),
            // we'll insert the new value into the cache for the next time.
            // Note that if we decide to skip creating a DispatchStub beacuise we are calling
            // from a shared to unshared domain the we also will insert into the cache.

            if (insertKind == DispatchCache::IK_NONE)
            {
                if (stubKind == SK_DISPATCH)
                {
                    insertKind = DispatchCache::IK_DISPATCH;
                }
                else if (stubKind == SK_RESOLVE)
                {
                    insertKind = DispatchCache::IK_RESOLVE;
                }
            }

            if (insertKind != DispatchCache::IK_NONE)
            {
                // Because the TransparentProxy MT is process-global, we cannot cache targets for
                // unshared interfaces because there is the possibility of caching a
                // <token, TPMT, target> entry where target is in AD1, and then matching against
                // this entry from AD2 which happens to be using the same token, perhaps for a
                // completely different interface.
            }

            if (insertKind != DispatchCache::IK_NONE)
            {
                VirtualCallStubManager * pMgrForCacheElem = this;

                // If we're calling from shared to unshared, make sure the cache element is
                // allocated in the unshared manager so that when the unshared code unloads
                // the cache element is unloaded.
                if (bCallToShorterLivedTarget)
                {
                    _ASSERTE(pCalleeMgr != NULL);
                    pMgrForCacheElem = pCalleeMgr;
                }

                // Find or create a new ResolveCacheElem
                ResolveCacheElem *e = pMgrForCacheElem->GetResolveCacheElem(objectType, token.To_SIZE_T(), (void *)target);

                // Try to insert this entry into the resolver cache table
                // When we get a collision we may decide not to insert this element
                // and Insert will return FALSE if we decided not to add the entry
#ifdef STUB_LOGGING
                BOOL didInsert =
#endif
                    g_resolveCache->Insert(e, insertKind);

#ifdef STUB_LOGGING
                if ((STUB_COLLIDE_MONO_PCT > 0) && !didInsert && (stubKind == SK_RESOLVE))
                {
                    // If we decided not to perform the insert and we came in with a resolve stub
                    // then we currently have a polymorphic callsite, So we flip a coin to decide
                    // whether to convert this callsite back into a dispatch stub (monomorphic callsite)

                    if (!bCallToShorterLivedTarget && bCreateDispatchStub)
                    {
                        // We are allowed to create a reusable dispatch stub for all assemblies
                        // this allows us to optimize the call interception case the same way

                        UINT32 coin = UINT32(GetRandomInt(100));

                        if (coin < STUB_COLLIDE_MONO_PCT)
                        {
                            DispatchEntry entryD;
                            Prober probeD(&entryD);
                            if (dispatchers->SetUpProber(token.To_SIZE_T(), (size_t) objectType, &probeD))
                            {
                                DispatchHolder *pDispatchHolder = NULL;
                                PCODE addrOfDispatch = (PCODE)(dispatchers->Find(&probeD));
                                if (addrOfDispatch == CALL_STUB_EMPTY_ENTRY)
                                {
                                    // It is possible that we never created this monomorphic dispatch stub
                                    // so we may have to create it now
                                    ResolveHolder* pResolveHolder = ResolveHolder::FromResolveEntry(pCallSite->GetSiteTarget());
                                    PCODE addrOfFail = pResolveHolder->stub()->failEntryPoint();
                                    bool reenteredCooperativeGCMode = false;
                                    pDispatchHolder = GenerateDispatchStub(
                                        target, addrOfFail, objectType, token.To_SIZE_T(), &reenteredCooperativeGCMode);
                                    if (reenteredCooperativeGCMode)
                                    {
                                        // The prober may have been invalidated by reentering cooperative GC mode, reset it
                                        BOOL success = dispatchers->SetUpProber(token.To_SIZE_T(), (size_t)objectType, &probeD);
                                        _ASSERTE(success);
                                    }
                                    dispatchers->Add((size_t)(pDispatchHolder->stub()->entryPoint()), &probeD);
                                }
                                else
                                {
                                    pDispatchHolder = DispatchHolder::FromDispatchEntry(addrOfDispatch);
                                }

                                // increment the of times we changed a cache collision into a mono stub
                                stats.worker_collide_to_mono++;

                                // Now assign the entrypoint to stub
                                CONSISTENCY_CHECK(pDispatchHolder != NULL);
                                stub = pDispatchHolder->stub()->entryPoint();
                                CONSISTENCY_CHECK(stub != NULL);
                            }
                        }
                    }
                }
#endif // STUB_LOGGING
            }

            if (stubKind == SK_LOOKUP)
            {
                BackPatchSite(pCallSite, (PCODE)stub);
            }
        }
    }
    EX_CATCH
    {
    }
    EX_END_CATCH (SwallowAllExceptions);

    // Target can be NULL only if we can't resolve to an address
    _ASSERTE(target != NULL);

    return target;
}

/*
Resolve the token in the context of the method table, and set the target to point to
the address that we should go to to get to the implementation.  Return a boolean indicating
whether or not this is a permenent choice or a temporary choice.  For example, if the code has
not been jitted yet, return FALSE and set the target to the prestub.  If the target is set to NULL,
it means that the token is not resolvable.
*/
BOOL
VirtualCallStubManager::Resolver(
    MethodTable * pMT,
    DispatchToken token,
    OBJECTREF   * protectedObj, // this one can actually be NULL, consider using pMT is you don't need the object itself
    PCODE *       ppTarget,
    BOOL          throwOnConflict)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(CheckPointer(pMT));
        PRECONDITION(TypeHandle(pMT).CheckFullyLoaded());
    } CONTRACTL_END;

#ifdef _DEBUG
    MethodTable * dbg_pTokenMT = pMT;
    MethodDesc *  dbg_pTokenMD = NULL;
    if (token.IsTypedToken())
    {
        dbg_pTokenMT = GetThread()->GetDomain()->LookupType(token.GetTypeID());
        dbg_pTokenMD = dbg_pTokenMT->FindDispatchSlot(TYPE_ID_THIS_CLASS, token.GetSlotNumber(), throwOnConflict).GetMethodDesc();
    }
#endif // _DEBUG

    // NOTE: CERs are not hardened against transparent proxy types,
    // so no need to worry about throwing an exception from here.

    LOG((LF_LOADER, LL_INFO10000, "SD: VCSM::Resolver: (start) looking up %s method in %s\n",
         token.IsThisToken() ? "this" : "interface",
         pMT->GetClass()->GetDebugClassName()));

    MethodDesc * pMD = NULL;
    BOOL fShouldPatch = FALSE;
    DispatchSlot implSlot(pMT->FindDispatchSlot(token.GetTypeID(), token.GetSlotNumber(), throwOnConflict));

    // If we found a target, then just figure out if we're allowed to create a stub around
    // this target and backpatch the callsite.
    if (!implSlot.IsNull())
    {
#if defined(LOGGING) || defined(_DEBUG)
        {
            pMD = implSlot.GetMethodDesc();
            if (pMD != NULL)
            {
                // Make sure we aren't crossing app domain boundaries
                CONSISTENCY_CHECK(GetAppDomain()->CheckValidModule(pMD->GetModule()));
#ifdef LOGGING
                WORD slot = pMD->GetSlot();
                BOOL fIsOverriddenMethod =
                    (pMT->GetNumParentVirtuals() <= slot && slot < pMT->GetNumVirtuals());
                LOG((LF_LOADER, LL_INFO10000, "SD: VCSM::Resolver: (end) looked up %s %s method %s::%s\n",
                     fIsOverriddenMethod ? "overridden" : "newslot",
                     token.IsThisToken() ? "this" : "interface",
                     pMT->GetClass()->GetDebugClassName(),
                     pMD->GetName()));
#endif // LOGGING
            }
        }
#endif // defined(LOGGING) || defined(_DEBUG)

        BOOL fSlotCallsPrestub = DoesSlotCallPrestub(implSlot.GetTarget());
        if (!fSlotCallsPrestub)
        {
            // Skip fixup precode jump for better perf
            PCODE pDirectTarget = Precode::TryToSkipFixupPrecode(implSlot.GetTarget());
            if (pDirectTarget != NULL)
                implSlot = DispatchSlot(pDirectTarget);

            // Only patch to a target if it's not going to call the prestub.
            fShouldPatch = TRUE;
        }
        else
        {
            // Getting the MethodDesc is very expensive,
            // so only call this when we are calling the prestub
            pMD = implSlot.GetMethodDesc();

            if (pMD == NULL)
            {
                // pMD can be NULL when another thread raced in and patched the Method Entry Point
                // so that it no longer points at the prestub
                // In such a case DoesSlotCallPrestub will now return FALSE
                CONSISTENCY_CHECK(!DoesSlotCallPrestub(implSlot.GetTarget()));
                fSlotCallsPrestub = FALSE;
            }

            if (!fSlotCallsPrestub)
            {
                // Only patch to a target if it's not going to call the prestub.
                fShouldPatch = TRUE;
            }
            else
            {
                CONSISTENCY_CHECK(CheckPointer(pMD));
                if (pMD->IsGenericMethodDefinition())
                {
                    //@GENERICS: Currently, generic virtual methods are called only through JIT_VirtualFunctionPointer
                    //           and so we could never have a virtual call stub at a call site for a generic virtual.
                    //           As such, we're assuming the only callers to Resolver are calls to GetTarget caused
                    //           indirectly by JIT_VirtualFunctionPointer. So, we're return TRUE for patching so that
                    //           we can cache the result in GetTarget and we don't have to perform the full resolve
                    //           every time. If the way we call generic virtual methods changes, this will also need
                    //           to change.
                    fShouldPatch = TRUE;
                }
            }
        }
    }
#ifdef FEATURE_COMINTEROP
    else if (pMT->IsComObjectType() && IsInterfaceToken(token))
    {
        MethodTable * pItfMT = GetTypeFromToken(token);
        implSlot = pItfMT->FindDispatchSlot(TYPE_ID_THIS_CLASS, token.GetSlotNumber(), throwOnConflict);

        _ASSERTE(!pItfMT->HasInstantiation());

        fShouldPatch = TRUE;
    }
#endif // FEATURE_COMINTEROP
#ifdef FEATURE_ICASTABLE
    else if (pMT->IsICastable() && protectedObj != NULL && *protectedObj != NULL)
    {
        GCStress<cfg_any>::MaybeTrigger();

        // In case of ICastable, instead of trying to find method implementation in the real object type
        // we call pObj.GetValueInternal() and call Resolver() again with whatever type it returns.
        // It allows objects that implement ICastable to mimic behavior of other types.
        MethodTable * pTokenMT = GetTypeFromToken(token);

        // Make call to ICastableHelpers.GetImplType(this, interfaceTypeObj)
        PREPARE_NONVIRTUAL_CALLSITE(METHOD__ICASTABLEHELPERS__GETIMPLTYPE);

        OBJECTREF tokenManagedType = pTokenMT->GetManagedClassObject(); //GC triggers

        DECLARE_ARGHOLDER_ARRAY(args, 2);
        args[ARGNUM_0] = OBJECTREF_TO_ARGHOLDER(*protectedObj);
        args[ARGNUM_1] = OBJECTREF_TO_ARGHOLDER(tokenManagedType);

        OBJECTREF impTypeObj = NULL;
        CALL_MANAGED_METHOD_RETREF(impTypeObj, OBJECTREF, args);

        INDEBUG(tokenManagedType = NULL); //tokenManagedType wasn't protected during the call
        if (impTypeObj == NULL) // GetImplType returns default(RuntimeTypeHandle)
        {
            COMPlusThrow(kEntryPointNotFoundException);
        }

        ReflectClassBaseObject* resultTypeObj = ((ReflectClassBaseObject*)OBJECTREFToObject(impTypeObj));
        TypeHandle resultTypeHnd = resultTypeObj->GetType();
        MethodTable *pResultMT = resultTypeHnd.GetMethodTable();

        return Resolver(pResultMT, token, protectedObj, ppTarget, throwOnConflict);
    }
#endif // FEATURE_ICASTABLE
    else if (pMT->IsIDynamicInterfaceCastable()
        && protectedObj != NULL
        && *protectedObj != NULL
        && IsInterfaceToken(token))
    {
        MethodTable *pTokenMT = GetTypeFromToken(token);

        OBJECTREF implTypeRef = DynamicInterfaceCastable::GetInterfaceImplementation(protectedObj, TypeHandle(pTokenMT));
        _ASSERTE(implTypeRef != NULL);

        ReflectClassBaseObject *implTypeObj = ((ReflectClassBaseObject *)OBJECTREFToObject(implTypeRef));
        TypeHandle implTypeHandle = implTypeObj->GetType();
        return Resolver(implTypeHandle.GetMethodTable(), token, protectedObj, ppTarget, throwOnConflict);
    }

    if (implSlot.IsNull())
    {
        MethodTable * pTokenMT = NULL;
        MethodDesc *  pTokenMD = NULL;
        if (token.IsTypedToken())
        {
            pTokenMT = GetThread()->GetDomain()->LookupType(token.GetTypeID());
            pTokenMD = pTokenMT->FindDispatchSlot(TYPE_ID_THIS_CLASS, token.GetSlotNumber(), throwOnConflict).GetMethodDesc();
        }

#ifdef FEATURE_COMINTEROP
        if ((pTokenMT != NULL) && (pTokenMT->GetClass()->IsEquivalentType()))
        {
            SString methodName;
            DefineFullyQualifiedNameForClassW();
            pTokenMD->GetFullMethodInfo(methodName);

            COMPlusThrowHR(COR_E_MISSINGMETHOD, COR_E_MISSINGMETHOD, GetFullyQualifiedNameForClassNestedAwareW(pMT), methodName.GetUnicode());
        }
        else
#endif // FEATURE_COMINTEROP
        if (!throwOnConflict)
        {
            // Assume we got null because there was a default interface method conflict
            *ppTarget = NULL;
            return FALSE;
        }
        else
        {
            // Method not found. In the castable object scenario where the method is being resolved on an interface itself,
            // this can happen if the user tried to call a method without a default implementation. Outside of that case,
            // this should never happen for anything but equivalent types
            CONSISTENCY_CHECK((!implSlot.IsNull() || pMT->IsInterface()) && "Valid method implementation was not found.");
            COMPlusThrow(kEntryPointNotFoundException);
        }
    }

    *ppTarget = implSlot.GetTarget();

    return fShouldPatch;
} // VirtualCallStubManager::Resolver

#endif // !DACCESS_COMPILE

//----------------------------------------------------------------------------
// Given a contract, return true if the contract represents a slot on the target.
BOOL VirtualCallStubManager::IsClassToken(DispatchToken token)
{
    CONTRACT (BOOL) {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACT_END;
    RETURN (token.IsThisToken());
}

//----------------------------------------------------------------------------
// Given a contract, return true if the contract represents an interface, false if just a slot.
BOOL VirtualCallStubManager::IsInterfaceToken(DispatchToken token)
{
    CONTRACT (BOOL) {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACT_END;
    BOOL ret = token.IsTypedToken();
    // For now, only interfaces have typed dispatch tokens.
    CONSISTENCY_CHECK(!ret || CheckPointer(GetThread()->GetDomain()->LookupType(token.GetTypeID())));
    CONSISTENCY_CHECK(!ret || GetThread()->GetDomain()->LookupType(token.GetTypeID())->IsInterface());
    RETURN (ret);
}

#ifndef DACCESS_COMPILE

//----------------------------------------------------------------------------
MethodDesc *
VirtualCallStubManager::GetRepresentativeMethodDescFromToken(
    DispatchToken token,
    MethodTable * pMT)
{
    CONTRACT (MethodDesc *) {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pMT));
        POSTCONDITION(CheckPointer(RETVAL));
    } CONTRACT_END;

    // This is called when trying to create a HelperMethodFrame, which means there are
    // potentially managed references on the stack that are not yet protected.
    GCX_FORBID();

    if (token.IsTypedToken())
    {
        pMT = GetThread()->GetDomain()->LookupType(token.GetTypeID());
        CONSISTENCY_CHECK(CheckPointer(pMT));
        token = DispatchToken::CreateDispatchToken(token.GetSlotNumber());
    }
    CONSISTENCY_CHECK(token.IsThisToken());
    RETURN (pMT->GetMethodDescForSlot(token.GetSlotNumber()));
}

//----------------------------------------------------------------------------
MethodTable *VirtualCallStubManager::GetTypeFromToken(DispatchToken token)
{
    CONTRACTL {
        NOTHROW;
        WRAPPER(GC_TRIGGERS);
    } CONTRACTL_END;
    MethodTable *pMT = GetThread()->GetDomain()->LookupType(token.GetTypeID());
    _ASSERTE(pMT != NULL);
    _ASSERTE(pMT->LookupTypeID() == token.GetTypeID());
    return pMT;
}

#endif // !DACCESS_COMPILE

//----------------------------------------------------------------------------
MethodDesc *VirtualCallStubManager::GetInterfaceMethodDescFromToken(DispatchToken token)
{
    CONTRACTL {
        NOTHROW;
        WRAPPER(GC_TRIGGERS);
        PRECONDITION(IsInterfaceToken(token));
    } CONTRACTL_END;

#ifndef DACCESS_COMPILE

    MethodTable * pMT = GetTypeFromToken(token);
    PREFIX_ASSUME(pMT != NULL);
    CONSISTENCY_CHECK(CheckPointer(pMT));
    return pMT->GetMethodDescForSlot(token.GetSlotNumber());

#else // DACCESS_COMPILE

    DacNotImpl();
    return NULL;

#endif // DACCESS_COMPILE
}

#ifndef DACCESS_COMPILE

//----------------------------------------------------------------------------
// This will check to see if a match is in the cache.
// Returns the target on success, otherwise NULL.
PCODE VirtualCallStubManager::CacheLookup(size_t token, UINT16 tokenHash, MethodTable *pMT)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pMT));
    } CONTRACTL_END

    // Now look in the cache for a match
    ResolveCacheElem *pElem = g_resolveCache->Lookup(token, tokenHash, pMT);

    // If the element matches, return the target - we're done!
    return (PCODE)(pElem != NULL ? pElem->target : NULL);
}


//----------------------------------------------------------------------------
/* static */
PCODE
VirtualCallStubManager::GetTarget(
    DispatchToken token,
    MethodTable * pMT,
    BOOL throwOnConflict)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
        PRECONDITION(CheckPointer(pMT));
    } CONTRACTL_END

    g_external_call++;

    if (token.IsThisToken())
    {
        return pMT->GetRestoredSlot(token.GetSlotNumber());
    }

    GCX_COOP(); // This is necessary for BucketTable synchronization

    PCODE target = NULL;

#ifndef STUB_DISPATCH_PORTABLE
    target = CacheLookup(token.To_SIZE_T(), DispatchCache::INVALID_HASH, pMT);
    if (target != NULL)
        return target;
#endif // !STUB_DISPATCH_PORTABLE

    // No match, now do full resolve
    BOOL fPatch;

    // TODO: passing NULL as protectedObj here can lead to incorrect behavior for ICastable objects
    // We need to review if this is the case and refactor this code if we want ICastable to become officially supported
    fPatch = Resolver(pMT, token, NULL, &target, throwOnConflict);
    _ASSERTE(!throwOnConflict || target != NULL);

#ifndef STUB_DISPATCH_PORTABLE
    if (fPatch)
    {
        ResolveCacheElem *pCacheElem = pMT->GetLoaderAllocator()->GetVirtualCallStubManager()->
            GetResolveCacheElem(pMT, token.To_SIZE_T(), (BYTE *)target);

        if (pCacheElem)
        {
            if (!g_resolveCache->Insert(pCacheElem, DispatchCache::IK_EXTERNAL))
            {
                // We decided not to perform the insert
            }
        }
    }
    else
    {
        g_external_call_no_patch++;
    }
#endif // !STUB_DISPATCH_PORTABLE

    return target;
}

#endif // !DACCESS_COMPILE

//----------------------------------------------------------------------------
/*
Resolve the token in the context of the method table, and set the target to point to
the address that we should go to to get to the implementation.  Return a boolean indicating
whether or not this is a permenent choice or a temporary choice.  For example, if the code has
not been jitted yet, return FALSE and set the target to the prestub.  If the target is set to NULL,
it means that the token is not resolvable.
*/
BOOL
VirtualCallStubManager::TraceResolver(
    Object *           pObj,
    DispatchToken      token,
    TraceDestination * trace)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(CheckPointer(pObj, NULL_OK));
        INJECT_FAULT(COMPlusThrowOM(););
    } CONTRACTL_END

    // If someone is trying to step into a stub dispatch call on a null object,
    // just say that we can't trace this call and we'll just end up throwing
    // a null ref exception.
    if (pObj == NULL)
    {
        return FALSE;
    }

    MethodTable *pMT = pObj->GetMethodTable();
    CONSISTENCY_CHECK(CheckPointer(pMT));

    DispatchSlot slot(pMT->FindDispatchSlot(token.GetTypeID(), token.GetSlotNumber(), FALSE /* throwOnConflict */));
    if (slot.IsNull() && IsInterfaceToken(token) && pMT->IsComObjectType())
    {
        MethodDesc * pItfMD = GetInterfaceMethodDescFromToken(token);
        CONSISTENCY_CHECK(pItfMD->GetMethodTable()->GetSlot(pItfMD->GetSlot()) == pItfMD->GetMethodEntryPoint());

        // Look up the slot on the interface itself.
        slot = pItfMD->GetMethodTable()->FindDispatchSlot(TYPE_ID_THIS_CLASS, pItfMD->GetSlot(), FALSE /* throwOnConflict */);
    }

    // The dispatch slot's target may change due to code versioning shortly after it was retrieved above for the trace. This
    // will result in the debugger getting some version of the code or the prestub, but not necessarily the exact code pointer
    // that winds up getting executed. The debugger has code that handles this ambiguity by placing a breakpoint at the start of
    // all native code versions, even if they aren't the one that was reported by this trace, see
    // DebuggerController::PatchTrace() under case TRACE_MANAGED. This alleviates the StubManager from having to prevent the
    // race that occurs here.
    //
    // If the dispatch slot is null, we assume it's because of a diamond case in default interface method dispatch.
    return slot.IsNull() ? FALSE : (StubManager::TraceStub(slot.GetTarget(), trace));
}

#ifndef DACCESS_COMPILE

//----------------------------------------------------------------------------
/* Change the call site.  It is failing the expected MT test in the dispatcher stub
too often.
*/
void VirtualCallStubManager::BackPatchWorker(StubCallSite* pCallSite)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
    } CONTRACTL_END

    PCODE callSiteTarget = pCallSite->GetSiteTarget();

    if (isDispatchingStub(callSiteTarget))
    {
        DispatchHolder * dispatchHolder = DispatchHolder::FromDispatchEntry(callSiteTarget);
        DispatchStub *   dispatchStub   = dispatchHolder->stub();

        //yes, patch it to point to the resolve stub
        //We can ignore the races now since we now know that the call site does go thru our
        //stub mechanisms, hence no matter who wins the race, we are correct.
        //We find the correct resolve stub by following the failure path in the dispatcher stub itself
        PCODE failEntry    = dispatchStub->failTarget();
        ResolveStub* resolveStub  = ResolveHolder::FromFailEntry(failEntry)->stub();
        PCODE resolveEntry = resolveStub->resolveEntryPoint();
        BackPatchSite(pCallSite, resolveEntry);

        LOG((LF_STUBS, LL_INFO10000, "BackPatchWorker call-site" FMT_ADDR "dispatchStub" FMT_ADDR "\n",
             DBG_ADDR(pCallSite->GetReturnAddress()), DBG_ADDR(dispatchHolder->stub())));

        //Add back the default miss count to the counter being used by this resolve stub
        //Since resolve stub are shared among many dispatch stubs each dispatch stub
        //that fails decrements the shared counter and the dispatch stub that trips the
        //counter gets converted into a polymorphic site
        INT32* counter = resolveStub->pCounter();
        *counter += STUB_MISS_COUNT_VALUE;
    }
}

//----------------------------------------------------------------------------
/* consider changing the call site to point to stub, if appropriate do it
*/
void VirtualCallStubManager::BackPatchSite(StubCallSite* pCallSite, PCODE stub)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        PRECONDITION(stub != NULL);
        PRECONDITION(CheckPointer(pCallSite));
        PRECONDITION(pCallSite->GetSiteTarget() != NULL);
    } CONTRACTL_END

    PCODE patch = stub;

    // This will take care of the prejit case and find the actual patch site
    PCODE prior = pCallSite->GetSiteTarget();

    //is this really going to change anything, if not don't do it.
    if (prior == patch)
        return;

    //we only want to do the following transitions for right now:
    //  prior           new
    //  lookup          dispatching or resolving
    //  dispatching     resolving
    if (isResolvingStub(prior))
        return;

    if(isDispatchingStub(stub))
    {
        if(isDispatchingStub(prior))
        {
            return;
        }
        else
        {
            stats.site_write_mono++;
        }
    }
    else
    {
        stats.site_write_poly++;
    }

    //patch the call site
    pCallSite->SetSiteTarget(patch);

    stats.site_write++;
}

//----------------------------------------------------------------------------
void StubCallSite::SetSiteTarget(PCODE newTarget)
{
    WRAPPER_NO_CONTRACT;
    PTR_PCODE pCell = GetIndirectCell();
    *pCell = newTarget;
}

//----------------------------------------------------------------------------
/* Generate a dispatcher stub, pMTExpected is the method table to burn in the stub, and the two addrOf's
are the addresses the stub is to transfer to depending on the test with pMTExpected
*/
DispatchHolder *VirtualCallStubManager::GenerateDispatchStub(PCODE            addrOfCode,
                                                             PCODE            addrOfFail,
                                                             void *           pMTExpected,
                                                             size_t           dispatchToken,
                                                             bool *           pMayHaveReenteredCooperativeGCMode)
{
    CONTRACT (DispatchHolder*) {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
        PRECONDITION(addrOfCode != NULL);
        PRECONDITION(addrOfFail != NULL);
        PRECONDITION(CheckPointer(pMTExpected));
        PRECONDITION(pMayHaveReenteredCooperativeGCMode != nullptr);
        PRECONDITION(!*pMayHaveReenteredCooperativeGCMode);
        POSTCONDITION(CheckPointer(RETVAL));
    } CONTRACT_END;

    size_t dispatchHolderSize = sizeof(DispatchHolder);

#ifdef TARGET_AMD64
    // See comment around m_fShouldAllocateLongJumpDispatchStubs for explanation.
    if (m_fShouldAllocateLongJumpDispatchStubs
        INDEBUG(|| g_pConfig->ShouldGenerateLongJumpDispatchStub()))
    {
        RETURN GenerateDispatchStubLong(addrOfCode,
                                        addrOfFail,
                                        pMTExpected,
                                        dispatchToken,
                                        pMayHaveReenteredCooperativeGCMode);
    }

    dispatchHolderSize = DispatchHolder::GetHolderSize(DispatchStub::e_TYPE_SHORT);
#endif

    //allocate from the requisite heap and copy the template over it.
    DispatchHolder * holder = (DispatchHolder*) (void*)
        dispatch_heap->AllocAlignedMem(dispatchHolderSize, CODE_SIZE_ALIGN);

#ifdef TARGET_AMD64
    if (!DispatchHolder::CanShortJumpDispatchStubReachFailTarget(addrOfFail, (LPCBYTE)holder))
    {
        m_fShouldAllocateLongJumpDispatchStubs = TRUE;
        RETURN GenerateDispatchStub(addrOfCode, addrOfFail, pMTExpected, dispatchToken, pMayHaveReenteredCooperativeGCMode);
    }
#endif

    ExecutableWriterHolder<DispatchHolder> dispatchWriterHolder(holder, dispatchHolderSize);
    dispatchWriterHolder.GetRW()->Initialize(holder, addrOfCode,
                       addrOfFail,
                       (size_t)pMTExpected
#ifdef TARGET_AMD64
                       , DispatchStub::e_TYPE_SHORT
#endif
                       );

#ifdef FEATURE_CODE_VERSIONING
    MethodDesc *pMD = MethodTable::GetMethodDescForSlotAddress(addrOfCode);
    if (pMD->IsVersionableWithVtableSlotBackpatch())
    {
        EntryPointSlots::SlotType slotType;
        TADDR slot = holder->stub()->implTargetSlot(&slotType);
        pMD->RecordAndBackpatchEntryPointSlot(m_loaderAllocator, slot, slotType);

        // RecordAndBackpatchEntryPointSlot() may exit and reenter cooperative GC mode
        *pMayHaveReenteredCooperativeGCMode = true;
    }
#endif

    ClrFlushInstructionCache(holder->stub(), holder->stub()->size());

    AddToCollectibleVSDRangeList(holder);

    //incr our counters
    stats.stub_mono_counter++;
    stats.stub_space += (UINT32)dispatchHolderSize;
    LOG((LF_STUBS, LL_INFO10000, "GenerateDispatchStub for token" FMT_ADDR "and pMT" FMT_ADDR "at" FMT_ADDR "\n",
                                 DBG_ADDR(dispatchToken), DBG_ADDR(pMTExpected), DBG_ADDR(holder->stub())));

#ifdef FEATURE_PERFMAP
    PerfMap::LogStubs(__FUNCTION__, "GenerateDispatchStub", (PCODE)holder->stub(), holder->stub()->size());
#endif

    RETURN (holder);
}

#ifdef TARGET_AMD64
//----------------------------------------------------------------------------
/* Generate a dispatcher stub, pMTExpected is the method table to burn in the stub, and the two addrOf's
are the addresses the stub is to transfer to depending on the test with pMTExpected
*/
DispatchHolder *VirtualCallStubManager::GenerateDispatchStubLong(PCODE            addrOfCode,
                                                                 PCODE            addrOfFail,
                                                                 void *           pMTExpected,
                                                                 size_t           dispatchToken,
                                                                 bool *           pMayHaveReenteredCooperativeGCMode)
{
    CONTRACT (DispatchHolder*) {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
        PRECONDITION(addrOfCode != NULL);
        PRECONDITION(addrOfFail != NULL);
        PRECONDITION(CheckPointer(pMTExpected));
        PRECONDITION(pMayHaveReenteredCooperativeGCMode != nullptr);
        PRECONDITION(!*pMayHaveReenteredCooperativeGCMode);
        POSTCONDITION(CheckPointer(RETVAL));
    } CONTRACT_END;

    //allocate from the requisite heap and copy the template over it.
    size_t dispatchHolderSize = DispatchHolder::GetHolderSize(DispatchStub::e_TYPE_LONG);
    DispatchHolder * holder = (DispatchHolder*) (void*)dispatch_heap->AllocAlignedMem(dispatchHolderSize, CODE_SIZE_ALIGN);
    ExecutableWriterHolder<DispatchHolder> dispatchWriterHolder(holder, dispatchHolderSize);

    dispatchWriterHolder.GetRW()->Initialize(holder, addrOfCode,
                       addrOfFail,
                       (size_t)pMTExpected,
                       DispatchStub::e_TYPE_LONG);

#ifdef FEATURE_CODE_VERSIONING
    MethodDesc *pMD = MethodTable::GetMethodDescForSlotAddress(addrOfCode);
    if (pMD->IsVersionableWithVtableSlotBackpatch())
    {
        EntryPointSlots::SlotType slotType;
        TADDR slot = holder->stub()->implTargetSlot(&slotType);
        pMD->RecordAndBackpatchEntryPointSlot(m_loaderAllocator, slot, slotType);

        // RecordAndBackpatchEntryPointSlot() may exit and reenter cooperative GC mode
        *pMayHaveReenteredCooperativeGCMode = true;
    }
#endif

    ClrFlushInstructionCache(holder->stub(), holder->stub()->size());

    AddToCollectibleVSDRangeList(holder);

    //incr our counters
    stats.stub_mono_counter++;
    stats.stub_space += static_cast<UINT32>(DispatchHolder::GetHolderSize(DispatchStub::e_TYPE_LONG));
    LOG((LF_STUBS, LL_INFO10000, "GenerateDispatchStub for token" FMT_ADDR "and pMT" FMT_ADDR "at" FMT_ADDR "\n",
                                 DBG_ADDR(dispatchToken), DBG_ADDR(pMTExpected), DBG_ADDR(holder->stub())));

#ifdef FEATURE_PERFMAP
    PerfMap::LogStubs(__FUNCTION__, "GenerateDispatchStub", (PCODE)holder->stub(), holder->stub()->size());
#endif

    RETURN (holder);
}
#endif

//----------------------------------------------------------------------------
/* Generate a resolve stub for the given dispatchToken.
addrOfResolver is where to go if the inline cache check misses
addrOfPatcher is who to call if the fail piece is being called too often by dispacher stubs
*/
ResolveHolder *VirtualCallStubManager::GenerateResolveStub(PCODE            addrOfResolver,
                                                           PCODE            addrOfPatcher,
                                                           size_t           dispatchToken
#if defined(TARGET_X86) && !defined(UNIX_X86_ABI)
                                                           , size_t         stackArgumentsSize
#endif
                                                           )
{
    CONTRACT (ResolveHolder*) {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
        PRECONDITION(addrOfResolver != NULL);
#if defined(TARGET_X86)
        PRECONDITION(addrOfPatcher != NULL);
#endif
        POSTCONDITION(CheckPointer(RETVAL));
    } CONTRACT_END;

    _ASSERTE(addrOfResolver);

    //get a counter for the fail piece

    UINT32         counter_index = counter_block::MAX_COUNTER_ENTRIES;
    counter_block *cur_block     = NULL;

    while (true)
    {
        cur_block = VolatileLoad(&m_cur_counter_block);

        if ((cur_block != NULL) && (cur_block->used < counter_block::MAX_COUNTER_ENTRIES))
        {
            counter_index = InterlockedIncrement((LONG*)&cur_block->used) - 1;
            if (counter_index < counter_block::MAX_COUNTER_ENTRIES)
            {
                // Typical case we allocate the next free counter in the block
                break;
            }
        }

        // Otherwise we have to create a new counter_block to serve as the head of m_cur_counter_block list

        // Create the new block in the main heap
        counter_block *pNew = new counter_block;

        // Initialize the new block
        pNew->next = cur_block;
        pNew->used = 0;

        // Try to link in the new block
        if (InterlockedCompareExchangeT(&m_cur_counter_block, pNew, cur_block) != cur_block)
        {
            // Lost a race to add pNew as new head
            delete pNew;
        }
    }

    CONSISTENCY_CHECK(counter_index < counter_block::MAX_COUNTER_ENTRIES);
    CONSISTENCY_CHECK(CheckPointer(cur_block));

    // Initialize the default miss counter for this resolve stub
    INT32* counterAddr = &(cur_block->block[counter_index]);
    *counterAddr = STUB_MISS_COUNT_VALUE;

    //allocate from the requisite heap and copy the templates for each piece over it.
    ResolveHolder * holder = (ResolveHolder*) (void*)
        resolve_heap->AllocAlignedMem(sizeof(ResolveHolder), CODE_SIZE_ALIGN);
    ExecutableWriterHolder<ResolveHolder> resolveWriterHolder(holder, sizeof(ResolveHolder));

    resolveWriterHolder.GetRW()->Initialize(holder,
                       addrOfResolver, addrOfPatcher,
                       dispatchToken, DispatchCache::HashToken(dispatchToken),
                       g_resolveCache->GetCacheBaseAddr(), counterAddr
#if defined(TARGET_X86) && !defined(UNIX_X86_ABI)
                       , stackArgumentsSize
#endif
                       );
    ClrFlushInstructionCache(holder->stub(), holder->stub()->size());

    AddToCollectibleVSDRangeList(holder);

    //incr our counters
    stats.stub_poly_counter++;
    stats.stub_space += sizeof(ResolveHolder)+sizeof(size_t);
    LOG((LF_STUBS, LL_INFO10000, "GenerateResolveStub  for token" FMT_ADDR "at" FMT_ADDR "\n",
                                 DBG_ADDR(dispatchToken), DBG_ADDR(holder->stub())));

#ifdef FEATURE_PERFMAP
    PerfMap::LogStubs(__FUNCTION__, "GenerateResolveStub", (PCODE)holder->stub(), holder->stub()->size());
#endif

    RETURN (holder);
}

//----------------------------------------------------------------------------
/* Generate a lookup stub for the given dispatchToken.  addrOfResolver is where the stub always transfers control
*/
LookupHolder *VirtualCallStubManager::GenerateLookupStub(PCODE addrOfResolver, size_t dispatchToken)
{
    CONTRACT (LookupHolder*) {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
        PRECONDITION(addrOfResolver != NULL);
        POSTCONDITION(CheckPointer(RETVAL));
    } CONTRACT_END;

    //allocate from the requisite heap and copy the template over it.
    LookupHolder * holder     = (LookupHolder*) (void*) lookup_heap->AllocAlignedMem(sizeof(LookupHolder), CODE_SIZE_ALIGN);
    ExecutableWriterHolder<LookupHolder> lookupWriterHolder(holder, sizeof(LookupHolder));

    lookupWriterHolder.GetRW()->Initialize(holder, addrOfResolver, dispatchToken);
    ClrFlushInstructionCache(holder->stub(), holder->stub()->size());

    AddToCollectibleVSDRangeList(holder);

    //incr our counters
    stats.stub_lookup_counter++;
    stats.stub_space += sizeof(LookupHolder);
    LOG((LF_STUBS, LL_INFO10000, "GenerateLookupStub   for token" FMT_ADDR "at" FMT_ADDR "\n",
                                 DBG_ADDR(dispatchToken), DBG_ADDR(holder->stub())));

#ifdef FEATURE_PERFMAP
    PerfMap::LogStubs(__FUNCTION__, "GenerateLookupStub", (PCODE)holder->stub(), holder->stub()->size());
#endif

    RETURN (holder);
}

//----------------------------------------------------------------------------
/* Generate a cache entry
*/
ResolveCacheElem *VirtualCallStubManager::GenerateResolveCacheElem(void *addrOfCode,
                                                                   void *pMTExpected,
                                                                   size_t token,
                                                                   bool *pMayHaveReenteredCooperativeGCMode)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
        PRECONDITION(pMayHaveReenteredCooperativeGCMode != nullptr);
        PRECONDITION(!*pMayHaveReenteredCooperativeGCMode);
    }
    CONTRACTL_END

    CONSISTENCY_CHECK(CheckPointer(pMTExpected));

    //allocate from the requisite heap and set the appropriate fields
    ResolveCacheElem *e = (ResolveCacheElem*) (void*)
        cache_entry_heap->AllocAlignedMem(sizeof(ResolveCacheElem), CODE_SIZE_ALIGN);

    e->pMT    = pMTExpected;
    e->token  = token;
    e->target = addrOfCode;

    e->pNext  = NULL;

#ifdef FEATURE_CODE_VERSIONING
    MethodDesc *pMD = MethodTable::GetMethodDescForSlotAddress((PCODE)addrOfCode);
    if (pMD->IsVersionableWithVtableSlotBackpatch())
    {
        pMD->RecordAndBackpatchEntryPointSlot(
            m_loaderAllocator,
            (TADDR)&e->target,
            EntryPointSlots::SlotType_Normal);

        // RecordAndBackpatchEntryPointSlot() may exit and reenter cooperative GC mode
        *pMayHaveReenteredCooperativeGCMode = true;
    }
#endif

    //incr our counters
    stats.cache_entry_counter++;
    stats.cache_entry_space += sizeof(ResolveCacheElem);

    return e;
}

//------------------------------------------------------------------
// Adds the stub manager to our linked list of virtual stub managers
// and adds to the global list.
//------------------------------------------------------------------
void VirtualCallStubManagerManager::AddStubManager(VirtualCallStubManager *pMgr)
{
    WRAPPER_NO_CONTRACT;

    SimpleWriteLockHolder lh(&m_RWLock);

    pMgr->m_pNext = m_pManagers;
    m_pManagers = pMgr;

    STRESS_LOG2(LF_CORDB | LF_CLASSLOADER, LL_INFO100,
        "VirtualCallStubManagerManager::AddStubManager - 0x%p (vptr 0x%p)\n", pMgr, (*(PVOID*)pMgr));
}

//------------------------------------------------------------------
// Removes the stub manager from our linked list of virtual stub
// managers and fromthe global list.
//------------------------------------------------------------------
void VirtualCallStubManagerManager::RemoveStubManager(VirtualCallStubManager *pMgr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    SimpleWriteLockHolder lh(&m_RWLock);

    // Remove this manager from our list.
    for (VirtualCallStubManager **pCur = &m_pManagers;
         *pCur != NULL;
         pCur = &((*pCur)->m_pNext))
    {
        if (*pCur == pMgr)
            *pCur = (*pCur)->m_pNext;
    }

    // Make sure we don't have a residual pointer left over.
    m_pCacheElem = NULL;

    STRESS_LOG1(LF_CORDB | LF_CLASSLOADER, LL_INFO100,
        "VirtualCallStubManagerManager::RemoveStubManager - 0x%p\n", pMgr);
}

//------------------------------------------------------------------
// Logs stub usage statistics
//------------------------------------------------------------------
void VirtualCallStubManager::LogStats()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    // Our Init routine assignes all fields atomically so testing one field should suffice to
    // test whehter the Init succeeded.
    if (!resolvers)
    {
        return;
    }

    // Temp space to use for formatting the output.
    static const int FMT_STR_SIZE = 160;
    char szPrintStr[FMT_STR_SIZE];
    DWORD dwWriteByte;

    if (g_hStubLogFile && (stats.site_write != 0))
    {
        //output counters
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_INT, "site_counter", stats.site_counter);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_INT, "site_write", stats.site_write);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_INT, "site_write_mono", stats.site_write_mono);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_INT, "site_write_poly", stats.site_write_poly);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);

        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), "\r\nstub data\r\n");
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);

        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_INT, "stub_lookup_counter", stats.stub_lookup_counter);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_INT, "stub_mono_counter", stats.stub_mono_counter);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_INT, "stub_poly_counter", stats.stub_poly_counter);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_INT, "stub_space", stats.stub_space);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);

        size_t total, used;
        g_resolveCache->GetLoadFactor(&total, &used);

        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_SIZE, "cache_entry_used", used);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_INT, "cache_entry_counter", stats.cache_entry_counter);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), OUTPUT_FORMAT_INT, "cache_entry_space", stats.cache_entry_space);
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);

        sprintf_s(szPrintStr, ARRAY_SIZE(szPrintStr), "\r\ncache_load:\t%zu used, %zu total, utilization %#5.2f%%\r\n",
                used, total, 100.0 * double(used) / double(total));
        WriteFile (g_hStubLogFile, szPrintStr, (DWORD) strlen(szPrintStr), &dwWriteByte, NULL);
    }

    resolvers->LogStats();
    dispatchers->LogStats();
    lookups->LogStats();
    vtableCallers->LogStats();
    cache_entries->LogStats();

    g_site_counter += stats.site_counter;
    g_stub_lookup_counter += stats.stub_lookup_counter;
    g_stub_poly_counter += stats.stub_poly_counter;
    g_stub_mono_counter += stats.stub_mono_counter;
    g_stub_vtable_counter += stats.stub_vtable_counter;
    g_site_write += stats.site_write;
    g_site_write_poly += stats.site_write_poly;
    g_site_write_mono += stats.site_write_mono;
    g_worker_call += stats.worker_call;
    g_worker_call_no_patch += stats.worker_call_no_patch;
    g_worker_collide_to_mono += stats.worker_collide_to_mono;
    g_stub_space += stats.stub_space;
    g_cache_entry_counter += stats.cache_entry_counter;
    g_cache_entry_space += stats.cache_entry_space;

    stats.site_counter = 0;
    stats.stub_lookup_counter = 0;
    stats.stub_poly_counter = 0;
    stats.stub_mono_counter = 0;
    stats.stub_vtable_counter = 0;
    stats.site_write = 0;
    stats.site_write_poly = 0;
    stats.site_write_mono = 0;
    stats.worker_call = 0;
    stats.worker_call_no_patch = 0;
    stats.worker_collide_to_mono = 0;
    stats.stub_space = 0;
    stats.cache_entry_counter = 0;
    stats.cache_entry_space = 0;
}

void Prober::InitProber(size_t key1, size_t key2, size_t* table)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
    } CONTRACTL_END

    _ASSERTE(table);

    keyA = key1;
    keyB = key2;
    base = &table[CALL_STUB_FIRST_INDEX];
    mask = table[CALL_STUB_MASK_INDEX];
    FormHash();
}

size_t Prober::Find()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
    } CONTRACTL_END

    size_t entry;
    //if this prober has already visited every slot, there is nothing more to look at.
    //note, this means that if a prober is going to be reused, the FormHash() function
    //needs to be called to reset it.
    if (NoMore())
        return CALL_STUB_EMPTY_ENTRY;
    do
    {
        entry = Read();

        //if we hit an empty entry, it means it cannot be in the table
        if(entry==CALL_STUB_EMPTY_ENTRY)
        {
            return CALL_STUB_EMPTY_ENTRY;
        }

        //we have a real entry, see if it is the one we want using our comparer
        comparer->SetContents(entry);
        if (comparer->Equals(keyA, keyB))
        {
            return entry;
        }
    } while(Next()); //Next() returns false when we have visited every slot
    return CALL_STUB_EMPTY_ENTRY;
}

size_t Prober::Add(size_t newEntry)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
    } CONTRACTL_END

    size_t entry;
    //if we have visited every slot then there is no room in the table to add this new entry
    if (NoMore())
        return CALL_STUB_EMPTY_ENTRY;

    do
    {
        entry = Read();
        if (entry==CALL_STUB_EMPTY_ENTRY)
        {
            //it's not in the table and we have the correct empty slot in hand
            //in which to add it.
            //try and grab it, if we succeed we break out to add the entry
            //if we fail, it means a racer swoped in a wrote in
            //this slot, so we will just keep looking
            if (GrabEntry(newEntry))
            {
                break;
            }

            // We didn't grab this entry, so keep trying.
            continue;
        }
        //check if this entry is already in the table, if so we are done
        comparer->SetContents(entry);
        if (comparer->Equals(keyA, keyB))
        {
            return entry;
        }
    } while(Next()); //Next() returns false when we have visited every slot

    //if we have visited every slot then there is no room in the table to add this new entry
    if (NoMore())
        return CALL_STUB_EMPTY_ENTRY;

    CONSISTENCY_CHECK(Read() == newEntry);
    return newEntry;
}

/*Atomically grab an entry, if it is empty, so we can write in it.
@TODO: It is not clear if this routine is actually necessary and/or if the
interlocked compare exchange is necessary as opposed to just a read write with racing allowed.
If we didn't have it, all that would happen is potentially more duplicates or
dropped entries, and we are supposed to run correctly even if they
happen.  So in a sense this is a perf optimization, whose value has
not been measured, i.e. it might be faster without it.
*/
BOOL Prober::GrabEntry(size_t entryValue)
{
    LIMITED_METHOD_CONTRACT;

    return InterlockedCompareExchangeT(&base[index],
        entryValue, static_cast<size_t>(CALL_STUB_EMPTY_ENTRY)) == CALL_STUB_EMPTY_ENTRY;
}

inline void FastTable::IncrementCount()
{
    LIMITED_METHOD_CONTRACT;

    // This MUST be an interlocked increment, since BucketTable::GetMoreSpace relies on
    // the return value of FastTable::isFull to tell it whether or not to continue with
    // trying to allocate a new FastTable. If two threads race and try to increment this
    // at the same time and one increment is lost, then the size will be inaccurate and
    // BucketTable::GetMoreSpace will never succeed, resulting in an infinite loop trying
    // to add a new entry.
    InterlockedIncrement((LONG *)&contents[CALL_STUB_COUNT_INDEX]);
}

size_t FastTable::Add(size_t entry, Prober* probe)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
    } CONTRACTL_END

    size_t result = probe->Add(entry);
    if (result == entry) IncrementCount();
    return result;
}

size_t FastTable::Find(Prober* probe)
{
    WRAPPER_NO_CONTRACT;

    return probe->Find();
}

/*Increase the size of the bucket referenced by the prober p and copy the existing members into it.
Since duplicates and lost entries are okay, we can build the larger table
and then try to swap it in.  If it turns out that somebody else is racing us,
the worst that will happen is we drop a few entries on the floor, which is okay.
If by chance we swap out a table that somebody else is inserting an entry into, that
is okay too, just another dropped entry.  If we detect dups, we just drop them on
the floor. */
BOOL BucketTable::GetMoreSpace(const Prober* p)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE; // This is necessary for synchronization with BucketTable::Reclaim
        INJECT_FAULT(COMPlusThrowOM(););
    } CONTRACTL_END;

    //get ahold of the current bucket
    Prober probe(p->comparer);
    size_t index = ComputeBucketIndex(p->keyA, p->keyB);

    FastTable* oldBucket = (FastTable*) Read(index);

    if (!oldBucket->isFull())
    {
        return TRUE;
    }
    //make a larger bucket
    size_t numEntries;
    if (oldBucket->tableSize() == CALL_STUB_MIN_ENTRIES)
    {
        numEntries = CALL_STUB_SECONDARY_ENTRIES;
    }
    else
    {
        numEntries = oldBucket->tableSize()*CALL_STUB_GROWTH_FACTOR;
    }

    FastTable* newBucket = FastTable::MakeTable(numEntries);

    //copy via insertion from the old to the new bucket
    size_t* limit = &oldBucket->contents[(oldBucket->tableSize())+CALL_STUB_FIRST_INDEX];
    size_t* e;
    for (e = &oldBucket->contents[CALL_STUB_FIRST_INDEX]; e<limit; e++)
    {
        size_t moved = *e;
        if (moved == CALL_STUB_EMPTY_ENTRY)
        {
            continue;
        }
        probe.comparer->SetContents(moved);
        probe.InitProber(probe.comparer->KeyA(), probe.comparer->KeyB(), &newBucket->contents[0]);
        //if the new bucket fills up, give up (this should never happen I think)
        if (newBucket->Add(moved, &probe) == CALL_STUB_EMPTY_ENTRY)
        {
            _ASSERTE(!"This should never happen");
            return FALSE;
        }
    }

    // Doing an interlocked exchange here ensures that if someone has raced and beaten us to
    // replacing the entry, then we will just put the new bucket we just created in the
    // dead list instead of risking a race condition which would put a duplicate of the old
    // bucket in the dead list (and even possibly cause a cyclic list).
    if (InterlockedCompareExchangeT(reinterpret_cast<FastTable * volatile *>(&buckets[index]), newBucket, oldBucket) != oldBucket)
        oldBucket = newBucket;

    // Link the old onto the "to be reclaimed" list.
    // Use the dead link field of the abandoned buckets to form the list
    FastTable* list;
    do {
        list = VolatileLoad(&dead);
        oldBucket->contents[CALL_STUB_DEAD_LINK] = (size_t) list;
    } while (InterlockedCompareExchangeT(&dead, oldBucket, list) != list);

#ifdef _DEBUG
    {
        // Validate correctness of the list
        FastTable *curr = oldBucket;
        while (curr)
        {
            FastTable *next = (FastTable *) curr->contents[CALL_STUB_DEAD_LINK];
            size_t i = 0;
            while (next)
            {
                next = (FastTable *) next->contents[CALL_STUB_DEAD_LINK];
                _ASSERTE(curr != next); // Make sure we don't have duplicates
                _ASSERTE(i++ < SIZE_T_MAX/4); // This just makes sure we don't have a cycle
            }
            curr = next;
        }
    }
#endif // _DEBUG

    //update our counters
    stats.bucket_space_dead += UINT32((oldBucket->tableSize()+CALL_STUB_FIRST_INDEX)*sizeof(void*));
    stats.bucket_space      -= UINT32((oldBucket->tableSize()+CALL_STUB_FIRST_INDEX)*sizeof(void*));
    stats.bucket_space      += UINT32((newBucket->tableSize()+CALL_STUB_FIRST_INDEX)*sizeof(void*));
    return TRUE;
}

void BucketTable::Reclaim()
{

    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
    }
    CONTRACTL_END

    //reclaim the dead (abandoned) buckets on the dead list
    //       The key issue is to not reclaim the list if any thread is in a stub or
    //       if any thread is accessing (read or write) the cache tables.  So we will declare
    //       those points to be non-gc safe points, and reclaim when the gc syncs the threads
    //@TODO: add an assert to ensure we are at a gc safe point
    FastTable* list = dead;

    //see if there is anything to do.
    //We ignore the race, since we will just pick them up on the next go around
    if (list == NULL) return;

    //Try and grab the list exclusively, if we fail, it means that either somebody
    //else grabbed it, or something go added.  In either case we just give up and assume
    //we will catch it on the next go around.
    //we use an interlock here in case we are called during shutdown not at a gc safe point
    //in which case the race is between several threads wanting to reclaim.
    //We are assuming that we are assuming the actually having to do anything is rare
    //so that the interlocked overhead is acceptable.  If this is not true, then
    //we need to examine exactly how and when we may be called during shutdown.
    if (InterlockedCompareExchangeT(&dead, NULL, list) != list)
        return;

#ifdef _DEBUG
    // Validate correctness of the list
    FastTable *curr = list;
    while (curr)
    {
        FastTable *next = (FastTable *) curr->contents[CALL_STUB_DEAD_LINK];
        size_t i = 0;
        while (next)
        {
            next = (FastTable *) next->contents[CALL_STUB_DEAD_LINK];
            _ASSERTE(curr != next); // Make sure we don't have duplicates
            _ASSERTE(i++ < SIZE_T_MAX/4); // This just makes sure we don't have a cycle
        }
        curr = next;
    }
#endif // _DEBUG

    //we now have the list off by ourself, so we can just walk and cleanup
    while (list)
    {
        size_t next = list->contents[CALL_STUB_DEAD_LINK];
        delete list;
        list = (FastTable*) next;
    }
}

//
// When using SetUpProber the proper values to use for keyA, keyB are:
//
//                 KeyA              KeyB
//-------------------------------------------------------
// lookups         token     the stub calling convention
// dispatchers     token     the expected MT
// resolver        token     the stub calling convention
// cache_entries   token     the expected method table
// vtableCallers   token     unused (zero)
//
BOOL BucketTable::SetUpProber(size_t keyA, size_t keyB, Prober *prober)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE; // This is necessary for synchronization with BucketTable::Reclaim
        INJECT_FAULT(COMPlusThrowOM(););
    } CONTRACTL_END;

    // The buckets[index] table starts off initialized to all CALL_STUB_EMPTY_ENTRY
    // and we should write each buckets[index] exactly once. However in a multi-proc
    // scenario each processor could see old memory values that would cause us to
    // leak memory.
    //
    // Since this is a fairly hot code path and it is very rare for buckets[index]
    // to be CALL_STUB_EMPTY_ENTRY, we can first try a non-volatile read and then
    // if it looks like we need to create a new FastTable we double check by doing
    // a volatile read.
    //
    // Note that BucketTable::GetMoreSpace also updates buckets[index] when the FastTable
    // grows to 90% full.  (CALL_STUB_LOAD_FACTOR is 90%)

    size_t index = ComputeBucketIndex(keyA, keyB);
    size_t bucket = buckets[index];  // non-volatile read
    if (bucket==CALL_STUB_EMPTY_ENTRY)
    {
        bucket = Read(index);        // volatile read
    }

    if (bucket==CALL_STUB_EMPTY_ENTRY)
    {
        FastTable* newBucket = FastTable::MakeTable(CALL_STUB_MIN_ENTRIES);

        // Doing an interlocked exchange here ensures that if someone has raced and beaten us to
        // replacing the entry, then we will free the new bucket we just created.
        bucket = InterlockedCompareExchangeT(&buckets[index], reinterpret_cast<size_t>(newBucket), static_cast<size_t>(CALL_STUB_EMPTY_ENTRY));
        if (bucket == CALL_STUB_EMPTY_ENTRY)
        {
            // We successfully wrote newBucket into buckets[index], overwritting the CALL_STUB_EMPTY_ENTRY value
            stats.bucket_space += UINT32((newBucket->tableSize()+CALL_STUB_FIRST_INDEX)*sizeof(void*));
            bucket = (size_t) newBucket;
        }
        else
        {
            // Someone else wrote buckets[index] before us
            // and bucket contains the value that they wrote
            // We must free the memory that we allocated
            // and we will use the value that someone else wrote
            delete newBucket;
            newBucket = (FastTable*) bucket;
        }
    }

    return ((FastTable*)(bucket))->SetUpProber(keyA, keyB, prober);
}

size_t BucketTable::Add(size_t entry, Prober* probe)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE; // This is necessary for synchronization with BucketTable::Reclaim
        INJECT_FAULT(COMPlusThrowOM(););
    } CONTRACTL_END

    FastTable* table = (FastTable*)(probe->items());
    size_t result = table->Add(entry,probe);
    if (result != CALL_STUB_EMPTY_ENTRY)
    {
        return result;
    }
    //we must have missed count(s) and the table is now full, so lets
    //grow and retry (this should be rare)
    if (!GetMoreSpace(probe)) return CALL_STUB_EMPTY_ENTRY;
    if (!SetUpProber(probe->keyA, probe->keyB, probe)) return CALL_STUB_EMPTY_ENTRY;
    return Add(entry, probe);  //recurse in for the retry to write the entry
}

void BucketTable::LogStats()
{
    LIMITED_METHOD_CONTRACT;

    // Update stats
    g_bucket_space += stats.bucket_space;
    g_bucket_space_dead += stats.bucket_space_dead;

    stats.bucket_space      = 0;
    stats.bucket_space_dead = 0;
}

DispatchCache::DispatchCache()
#ifdef CHAIN_LOOKUP
    : m_writeLock(CrstStubDispatchCache, CRST_UNSAFE_ANYMODE)
#endif
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END

    //initialize the cache to be empty, i.e. all slots point to the empty entry
    ResolveCacheElem* e = new ResolveCacheElem();
    e->pMT = (void *) (-1); //force all method tables to be misses
    e->pNext = NULL; // null terminate the chain for the empty entry
    empty = e;
    for (int i = 0;i<CALL_STUB_CACHE_SIZE;i++)
        ClearCacheEntry(i);

    // Initialize statistics
    memset(&stats, 0, sizeof(stats));
#ifdef STUB_LOGGING
    memset(&cacheData, 0, sizeof(cacheData));
#endif
}

ResolveCacheElem* DispatchCache::Lookup(size_t token, UINT16 tokenHash, void* mt)
{
    WRAPPER_NO_CONTRACT;
    if (tokenHash == INVALID_HASH)
        tokenHash = HashToken(token);
    UINT16 idx = HashMT(tokenHash, mt);
    ResolveCacheElem *pCurElem = GetCacheEntry(idx);

#if defined(STUB_LOGGING) && defined(CHAIN_LOOKUP)
    BOOL chainedLookup = FALSE;
#endif
    // No need to conditionlize on CHAIN_LOOKUP, since this loop
    // will only run once when CHAIN_LOOKUP is undefined, since
    // there will only ever be one element in a bucket (chain of 1).
    while (pCurElem != empty) {
        if (pCurElem->Equals(token, mt)) {
            return pCurElem;
        }
#if defined(STUB_LOGGING) && defined(CHAIN_LOOKUP)
        // Only want to inc the counter once per chain search.
        if (pCurElem == GetCacheEntry(idx)) {
            chainedLookup = TRUE;
            g_chained_lookup_external_call_counter++;
        }
#endif // defined(STUB_LOGGING) && defined(CHAIN_LOOKUP)
        pCurElem = pCurElem->Next();
    }
#if defined(STUB_LOGGING) && defined(CHAIN_LOOKUP)
    if (chainedLookup) {
        g_chained_lookup_external_miss_counter++;
    }
#endif // defined(STUB_LOGGING) && defined(CHAIN_LOOKUP)
    return NULL; /* with chain lookup disabled this returns NULL */
}

// returns true if we wrote the resolver cache entry with the new elem
//    also returns true if the cache entry already contained elem (the miss case)
//
BOOL DispatchCache::Insert(ResolveCacheElem* elem, InsertKind insertKind)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        FORBID_FAULT;
        PRECONDITION(insertKind != IK_NONE);
    } CONTRACTL_END;

#ifdef CHAIN_LOOKUP
    CrstHolder lh(&m_writeLock);
#endif

    // Figure out what bucket this element belongs in
    UINT16 tokHash = HashToken(elem->token);
    UINT16 hash    = HashMT(tokHash, elem->pMT);
    UINT16 idx     = hash;
    BOOL   write   = FALSE;
    BOOL   miss    = FALSE;
    BOOL   hit     = FALSE;
    BOOL   collide = FALSE;

#ifdef _DEBUG
    elem->debug_hash = tokHash;
    elem->debug_index = idx;
#endif // _DEBUG

        ResolveCacheElem* cell = GetCacheEntry(idx);

#ifdef CHAIN_LOOKUP
    // There is the possibility of a race where two threads will
    // try to generate a ResolveCacheElem for the same tuple, and
    // the first thread will get the lock and insert the element
    // and the second thread coming in should detect this and not
    // re-add the element, since it is already likely at the start
    // of the list, and would result in the element looping to
    // itself.
    if (Lookup(elem->token, tokHash, elem->pMT))
#else // !CHAIN_LOOKUP
        if (cell == elem)
#endif // !CHAIN_LOOKUP
        {
            miss  = TRUE;
            write = FALSE;
        }
    else
    {
        if (cell == empty)
        {
            hit   = TRUE;
            write = TRUE;
    }
    }
    CONSISTENCY_CHECK(!(hit && miss));

    // If we didn't have a miss or a hit then we had a collision with
    // a non-empty entry in our resolver cache
    if (!hit && !miss)
    {
        collide = TRUE;

#ifdef CHAIN_LOOKUP
        // Always insert the entry into the chain
        write = TRUE;
#else // !CHAIN_LOOKUP

        if (STUB_COLLIDE_WRITE_PCT < 100)
        {
            UINT32 coin = UINT32(GetRandomInt(100));

            write = (coin < STUB_COLLIDE_WRITE_PCT);
        }
        else
        {
            write = TRUE;
        }

#endif // !CHAIN_LOOKUP
    }

    if (write)
    {
#ifdef CHAIN_LOOKUP
        // We create a list with the last pNext pointing at empty
        elem->pNext = cell;
#else // !CHAIN_LOOKUP
        elem->pNext = empty;
#endif // !CHAIN_LOOKUP
        SetCacheEntry(idx, elem);
        stats.insert_cache_write++;
    }

    LOG((LF_STUBS, LL_INFO1000, "%8s Insert(token" FMT_ADDR "MethodTable" FMT_ADDR ") at [%03x] %7s %5s \n",
         (insertKind == IK_DISPATCH) ? "Dispatch" : (insertKind == IK_RESOLVE) ? "Resolve" : "External",
         DBG_ADDR(elem->token), DBG_ADDR(elem->pMT), hash,
         hit ? "HIT" : miss ? "MISS" : "COLLIDE", write ? "WRITE" : "KEEP"));

    if (insertKind == IK_DISPATCH)
        stats.insert_cache_dispatch++;
    else if (insertKind == IK_RESOLVE)
        stats.insert_cache_resolve++;
    else if (insertKind == IK_SHARED)
        stats.insert_cache_shared++;
    else if (insertKind == IK_EXTERNAL)
        stats.insert_cache_external++;

    if (hit)
        stats.insert_cache_hit++;
    else if (miss)
        stats.insert_cache_miss++;
    else if (collide)
        stats.insert_cache_collide++;

    return write || miss;
}

#ifdef CHAIN_LOOKUP
void DispatchCache::PromoteChainEntry(ResolveCacheElem* elem)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
    } CONTRACTL_END;

    CrstHolder lh(&m_writeLock);
    g_chained_entry_promoted++;

    // Figure out what bucket this element belongs in
    UINT16 tokHash = HashToken(elem->token);
    UINT16 hash    = HashMT(tokHash, elem->pMT);
    UINT16 idx     = hash;

    ResolveCacheElem *curElem = GetCacheEntry(idx);

    // If someone raced in and promoted this element before us,
    // then we can just return. Furthermore, it would be an
    // error if we performed the below code, since we'd end up
    // with a self-referential element and an infinite loop.
    if (curElem == elem)
    {
        return;
    }

    // Now loop through the chain to find the element that is
    // point to the element we're promoting so we can remove
    // it from the chain.
    while (curElem->Next() != elem)
    {
        curElem = curElem->pNext;
        CONSISTENCY_CHECK(curElem != NULL);
    }

    // Remove the element from the chain
    CONSISTENCY_CHECK(curElem->pNext == elem);
    curElem->pNext = elem->pNext;

    // Set the promoted entry to the head of the list.
    elem->pNext = GetCacheEntry(idx);
    SetCacheEntry(idx, elem);
}
#endif // CHAIN_LOOKUP

void DispatchCache::LogStats()
{
    LIMITED_METHOD_CONTRACT;

    g_insert_cache_external += stats.insert_cache_external;
    g_insert_cache_shared   += stats.insert_cache_shared;
    g_insert_cache_dispatch += stats.insert_cache_dispatch;
    g_insert_cache_resolve  += stats.insert_cache_resolve;
    g_insert_cache_hit      += stats.insert_cache_hit;
    g_insert_cache_miss     += stats.insert_cache_miss;
    g_insert_cache_collide  += stats.insert_cache_collide;
    g_insert_cache_write    += stats.insert_cache_write;

    stats.insert_cache_external = 0;
    stats.insert_cache_shared = 0;
    stats.insert_cache_dispatch = 0;
    stats.insert_cache_resolve = 0;
    stats.insert_cache_hit = 0;
    stats.insert_cache_miss = 0;
    stats.insert_cache_collide = 0;
    stats.insert_cache_write = 0;
}

/* The following tablse have bits that have the following properties:
   1. Each entry has 12-bits with 5,6 or 7 one bits and 5,6 or 7 zero bits.
   2. For every bit we try to have half one bits and half zero bits
   3. Adjacent entries when xor-ed should have 5,6 or 7 bits that are different
*/
#ifdef HOST_64BIT
static const UINT16 tokenHashBits[64] =
#else // !HOST_64BIT
static const UINT16 tokenHashBits[32] =
#endif // !HOST_64BIT
{
    0xcd5, 0x8b9, 0x875, 0x439,
    0xbf0, 0x38d, 0xa5b, 0x6a7,
    0x78a, 0x9c8, 0xee2, 0x3d3,
    0xd94, 0x54e, 0x698, 0xa6a,
    0x753, 0x932, 0x4b7, 0x155,
    0x3a7, 0x9c8, 0x4e9, 0xe0b,
    0xf05, 0x994, 0x472, 0x626,
    0x15c, 0x3a8, 0x56e, 0xe2d,

#ifdef HOST_64BIT
    0xe3c, 0xbe2, 0x58e, 0x0f3,
    0x54d, 0x70f, 0xf88, 0xe2b,
    0x353, 0x153, 0x4a5, 0x943,
    0xaf2, 0x88f, 0x72e, 0x978,
    0xa13, 0xa0b, 0xc3c, 0xb72,
    0x0f7, 0x49a, 0xdd0, 0x366,
    0xd84, 0xba5, 0x4c5, 0x6bc,
    0x8ec, 0x0b9, 0x617, 0x85c,
#endif // HOST_64BIT
};

/*static*/ UINT16 DispatchCache::HashToken(size_t token)
{
    LIMITED_METHOD_CONTRACT;

    UINT16 hash  = 0;
    int    index = 0;

    // Note if you change the number of bits in CALL_STUB_CACHE_NUM_BITS
    // then we have to recompute the hash function
    // Though making the number of bits smaller should still be OK
    static_assert_no_msg(CALL_STUB_CACHE_NUM_BITS <= 12);

    while (token)
    {
        if (token & 1)
            hash ^= tokenHashBits[index];

        index++;
        token >>= 1;
    }
    _ASSERTE((hash & ~CALL_STUB_CACHE_MASK) == 0);
    return hash;
}

/////////////////////////////////////////////////////////////////////////////////////////////
DispatchCache::Iterator::Iterator(DispatchCache *pCache) : m_pCache(pCache), m_curBucket(-1)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pCache));
    } CONTRACTL_END;

    // Move to the first valid entry
    NextValidBucket();
}

/////////////////////////////////////////////////////////////////////////////////////////////
void DispatchCache::Iterator::Next()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    if (!IsValid()) {
        return;
    }

    // Move to the next element in the chain
    m_ppCurElem = &((*m_ppCurElem)->pNext);

    // If the next element was the empty sentinel entry, move to the next valid bucket.
    if (*m_ppCurElem == m_pCache->empty) {
        NextValidBucket();
    }
}

/////////////////////////////////////////////////////////////////////////////////////////////
// This doesn't actually delete the entry, it just unlinks it from the chain.
// Returns the unlinked entry.
ResolveCacheElem *DispatchCache::Iterator::UnlinkEntry()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        CONSISTENCY_CHECK(IsValid());
    } CONTRACTL_END;
    ResolveCacheElem *pUnlinkedEntry = *m_ppCurElem;
    *m_ppCurElem = (*m_ppCurElem)->pNext;
    pUnlinkedEntry->pNext = m_pCache->empty;
    // If unlinking this entry took us to the end of this bucket, need to move to the next.
    if (*m_ppCurElem == m_pCache->empty) {
        NextValidBucket();
    }
    return pUnlinkedEntry;
}

/////////////////////////////////////////////////////////////////////////////////////////////
void DispatchCache::Iterator::NextValidBucket()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        CONSISTENCY_CHECK(IsValid());
    } CONTRACTL_END;

    // Move to the next bucket that contains a cache entry
    do {
        NextBucket();
    } while (IsValid() && *m_ppCurElem == m_pCache->empty);
}

#endif // !DACCESS_COMPILE

/////////////////////////////////////////////////////////////////////////////////////////////
VirtualCallStubManager *VirtualCallStubManagerManager::FindVirtualCallStubManager(PCODE stubAddress)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    SUPPORTS_DAC;

#ifndef DACCESS_COMPILE
    // Check the cached element
    {
        VirtualCallStubManager *pMgr = m_pCacheElem;
        if (pMgr != NULL && pMgr->CheckIsStub_Internal(stubAddress))
        {
            return pMgr;
        }
    }

    // Check the current and shared domains.
    {
        Thread *pThread = GetThreadNULLOk();
        if (pThread != NULL)
        {
            // Check the current domain
            {
                BaseDomain *pDom = pThread->GetDomain();
                VirtualCallStubManager *pMgr = pDom->GetLoaderAllocator()->GetVirtualCallStubManager();
                if (pMgr->CheckIsStub_Internal(stubAddress))
                {
                    m_pCacheElem = pMgr;
                    return pMgr;
                }
            }
        }
    }
#endif

    // If both previous attempts fail, run through the list. This is likely
    // because the thread is a debugger thread running outside of the domain
    // that owns the target stub.
    {
        VirtualCallStubManagerIterator it =
            VirtualCallStubManagerManager::GlobalManager()->IterateVirtualCallStubManagers();

        while (it.Next())
        {
            if (it.Current()->CheckIsStub_Internal(stubAddress))
            {
#ifndef DACCESS_COMPILE
                m_pCacheElem = it.Current();
#endif
                return it.Current();
            }
        }
    }

    // No VirtualCallStubManager owns this address.
    return NULL;
}

static VirtualCallStubManager * const IT_START = (VirtualCallStubManager *)(-1);

/////////////////////////////////////////////////////////////////////////////////////////////
// Move to the next element. Iterators are created at
// start-1, so must call Next before using Current
BOOL VirtualCallStubManagerIterator::Next()
{
    LIMITED_METHOD_DAC_CONTRACT;

    if (m_fIsStart)
    {
        m_fIsStart = FALSE;
    }
    else if (m_pCurMgr != NULL)
    {
        m_pCurMgr = m_pCurMgr->m_pNext;
    }

    return (m_pCurMgr != NULL);
}

/////////////////////////////////////////////////////////////////////////////////////////////
// Get the current contents of the iterator
VirtualCallStubManager *VirtualCallStubManagerIterator::Current()
{
    LIMITED_METHOD_DAC_CONTRACT;
    CONSISTENCY_CHECK(!m_fIsStart);
    CONSISTENCY_CHECK(CheckPointer(m_pCurMgr));

    return m_pCurMgr;
}

#ifndef DACCESS_COMPILE
/////////////////////////////////////////////////////////////////////////////////////////////
VirtualCallStubManagerManager::VirtualCallStubManagerManager()
    : m_pManagers(NULL),
      m_pCacheElem(NULL),
      m_RWLock(COOPERATIVE_OR_PREEMPTIVE, LOCK_TYPE_DEFAULT)
{
    LIMITED_METHOD_CONTRACT;
}

/////////////////////////////////////////////////////////////////////////////////////////////
/* static */
void VirtualCallStubManagerManager::InitStatic()
{
    STANDARD_VM_CONTRACT;

    CONSISTENCY_CHECK(g_pManager == NULL);
    g_pManager = new VirtualCallStubManagerManager();
}
#endif

/////////////////////////////////////////////////////////////////////////////////////////////
VirtualCallStubManagerIterator VirtualCallStubManagerManager::IterateVirtualCallStubManagers()
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    VirtualCallStubManagerIterator it(VirtualCallStubManagerManager::GlobalManager());
    return it;
}

/////////////////////////////////////////////////////////////////////////////////////////////
BOOL VirtualCallStubManagerManager::CheckIsStub_Internal(
                    PCODE stubStartAddress)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    VirtualCallStubManager *pMgr = FindVirtualCallStubManager(stubStartAddress);
    return (pMgr != NULL);
}

/////////////////////////////////////////////////////////////////////////////////////////////
BOOL VirtualCallStubManagerManager::DoTraceStub(
                    PCODE stubStartAddress,
                    TraceDestination *trace)
{
    WRAPPER_NO_CONTRACT;

    // Find the owning manager. We should succeed, since presumably someone already
    // called CheckIsStub on us to find out that we own the address, and already
    // called TraceManager to initiate a trace.
    VirtualCallStubManager *pMgr = FindVirtualCallStubManager(stubStartAddress);
    CONSISTENCY_CHECK(CheckPointer(pMgr));

    return pMgr->DoTraceStub(stubStartAddress, trace);
}

#ifndef DACCESS_COMPILE
/////////////////////////////////////////////////////////////////////////////////////////////
MethodDesc *VirtualCallStubManagerManager::Entry2MethodDesc(
                    PCODE stubStartAddress,
                    MethodTable *pMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END

    if (pMT == NULL)
        return NULL;

    VirtualCallStubManager::StubKind sk;

    // Find the owning manager.
    VirtualCallStubManager *pMgr = VirtualCallStubManager::FindStubManager(stubStartAddress,  &sk);
    if (pMgr == NULL)
        return NULL;

    // Do the full resolve
    DispatchToken token(VirtualCallStubManager::GetTokenFromStubQuick(pMgr, stubStartAddress, sk));

    PCODE target = NULL;
    // TODO: passing NULL as protectedObj here can lead to incorrect behavior for ICastable objects
    // We need to review if this is the case and refactor this code if we want ICastable to become officially supported
    VirtualCallStubManager::Resolver(pMT, token, NULL, &target, TRUE /* throwOnConflict */);

    return pMT->GetMethodDescForSlotAddress(target);
}
#endif

#ifdef DACCESS_COMPILE
void VirtualCallStubManagerManager::DoEnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;
    WRAPPER_NO_CONTRACT;
    VirtualCallStubManagerIterator it = IterateVirtualCallStubManagers();
    while (it.Next())
    {
        it.Current()->DoEnumMemoryRegions(flags);
    }
}
#endif

//----------------------------------------------------------------------------
BOOL VirtualCallStubManagerManager::TraceManager(
                    Thread *thread, TraceDestination *trace,
                    T_CONTEXT *pContext, BYTE **pRetAddr)
{
    WRAPPER_NO_CONTRACT;

    // Find the owning manager. We should succeed, since presumably someone already
    // called CheckIsStub on us to find out that we own the address.
    VirtualCallStubManager *pMgr = FindVirtualCallStubManager(GetIP(pContext));
    CONSISTENCY_CHECK(CheckPointer(pMgr));

    // Forward the call to the appropriate manager.
    return pMgr->TraceManager(thread, trace, pContext, pRetAddr);
}
