// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


/*
 * GCCOMMON.CPP
 *
 * Code common to both SVR and WKS gcs
 */

#include "common.h"

#include "gcenv.h"
#include "gc.h"

IGCHeapInternal* g_theGCHeap;
IGCHandleManager* g_theGCHandleManager;

#ifdef BUILD_AS_STANDALONE
IGCToCLR* g_theGCToCLR;
VersionInfo g_runtimeSupportedVersion;
bool g_oldMethodTableFlags;
#endif // BUILD_AS_STANDALONE

#ifdef GC_CONFIG_DRIVEN
size_t gc_global_mechanisms[MAX_GLOBAL_GC_MECHANISMS_COUNT];
#endif //GC_CONFIG_DRIVEN

#ifndef DACCESS_COMPILE

#ifdef WRITE_BARRIER_CHECK
uint8_t* g_GCShadow;
uint8_t* g_GCShadowEnd;
uint8_t* g_shadow_lowest_address = NULL;
#endif

uint32_t* g_gc_card_table;

VOLATILE(int32_t) g_fSuspensionPending = 0;

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
uint32_t* g_gc_card_bundle_table;
#endif

uint8_t* g_gc_lowest_address  = 0;
uint8_t* g_gc_highest_address = 0;
GCHeapType g_gc_heap_type = GC_HEAP_INVALID;
uint32_t g_max_generation = max_generation;
MethodTable* g_gc_pFreeObjectMethodTable = nullptr;
uint32_t g_num_processors = 0;

#ifdef GC_CONFIG_DRIVEN
void record_global_mechanism (int mech_index)
{
    (gc_global_mechanisms[mech_index])++;
}
#endif //GC_CONFIG_DRIVEN

#ifdef WRITE_BARRIER_CHECK

#define INVALIDGCVALUE (void *)((size_t)0xcccccccd)

    // called by the write barrier to update the shadow heap
void updateGCShadow(Object** ptr, Object* val)
{
    Object** shadow = (Object**) &g_GCShadow[((uint8_t*) ptr - g_lowest_address)];
    if ((uint8_t*) shadow < g_GCShadowEnd)
    {
        *shadow = val;

        // Ensure that the write to the shadow heap occurs before the read from
        // the GC heap so that race conditions are caught by INVALIDGCVALUE.
        MemoryBarrier();

        if(*ptr!=val)
            *shadow = (Object *) INVALIDGCVALUE;
    }
}

#endif // WRITE_BARRIER_CHECK


struct changed_seg
{
    uint8_t           * start;
    uint8_t           * end;
    size_t              gc_index;
    bgc_state           bgc;
    changed_seg_state   changed;
};


const int max_saved_changed_segs = 128;

changed_seg saved_changed_segs[max_saved_changed_segs];
uint64_t saved_changed_segs_count = ~0ull;

void record_changed_seg (uint8_t* start, uint8_t* end,
                         size_t current_gc_index,
                         bgc_state current_bgc_state,
                         changed_seg_state changed_state)
{
#if defined(MULTIPLE_HEAPS) && defined(USE_REGIONS)
    uint64_t segs_count = Interlocked::Increment(&saved_changed_segs_count);
#else
    uint64_t segs_count = ++saved_changed_segs_count;
#endif //MULTIPLE_HEAPS && USE_REGIONS

    static_assert((max_saved_changed_segs & (max_saved_changed_segs - 1)) == 0, "Size must be a power of two");
    unsigned int segs_index = segs_count & (max_saved_changed_segs - 1);

    saved_changed_segs[segs_index].start = start;
    saved_changed_segs[segs_index].end = end;
    saved_changed_segs[segs_index].gc_index = current_gc_index;
    saved_changed_segs[segs_index].bgc = current_bgc_state;
    saved_changed_segs[segs_index].changed = changed_state;
}

#if defined(TRACE_GC) || defined(GC_CONFIG_DRIVEN)
FILE* CreateLogFile(const GCConfigStringHolder& temp_logfile_name, bool is_config)
{
    FILE* logFile;

    if (!temp_logfile_name.Get())
    {
        return nullptr;
    }

    char logfile_name[MAX_LONGPATH+1];
    //uint32_t pid = GCToOSInterface::GetCurrentProcessId();
    const char* suffix = is_config ? ".config.log" : ".log";
    //_snprintf_s(logfile_name, MAX_LONGPATH+1, _TRUNCATE, "%s.%d%s", temp_logfile_name.Get(), pid, suffix);
    _snprintf_s(logfile_name, MAX_LONGPATH+1, _TRUNCATE, "%s%s", temp_logfile_name.Get(), suffix);
    logFile = fopen(logfile_name, "wb");
    return logFile;
}
#endif //TRACE_GC || GC_CONFIG_DRIVEN

#if defined(TRACE_GC) && defined(SIMPLE_DPRINTF)
BOOL   gc_log_on = TRUE;
FILE* gc_log = NULL;
size_t gc_log_file_size = 0;

size_t gc_buffer_index = 0;
size_t max_gc_buffers = 0;

static CLRCriticalSection gc_log_lock;

// we keep this much in a buffer and only flush when the buffer is full
#define gc_log_buffer_size (1024*1024)
uint8_t* gc_log_buffer = 0;
size_t gc_log_buffer_offset = 0;

HRESULT initialize_log_file()
{
    if (GCConfig::GetLogEnabled())
    {
        gc_log = CreateLogFile(GCConfig::GetLogFile(), false);

        if (gc_log == NULL)
        {
            GCToEEInterface::LogErrorToHost("Cannot create log file");
            return E_FAIL;
        }

        // GCLogFileSize in MBs.
        gc_log_file_size = static_cast<size_t>(GCConfig::GetLogFileSize());

        if (gc_log_file_size <= 0 || gc_log_file_size > 500)
        {
            GCToEEInterface::LogErrorToHost("Invalid log file size (valid size needs to be larger than 0 and smaller than 500)");
            fclose (gc_log);
            return E_FAIL;
        }

        gc_log_lock.Initialize();
        gc_log_buffer = new (nothrow) uint8_t [gc_log_buffer_size];
        if (!gc_log_buffer)
        {
            fclose(gc_log);
            return E_OUTOFMEMORY;
        }

        memset (gc_log_buffer, '*', gc_log_buffer_size);

        max_gc_buffers = gc_log_file_size * 1024 * 1024 / gc_log_buffer_size;
    }

    return S_OK;
}

void flush_gc_log (bool close)
{
    if (gc_log_on && (gc_log != NULL))
    {
        fwrite(gc_log_buffer, gc_log_buffer_offset, 1, gc_log);
        fflush(gc_log);
        if (close)
        {
            fclose(gc_log);
            gc_log_on = false;
            gc_log = NULL;
        }
        gc_log_buffer_offset = 0;
    }
}

void log_va_msg(const char *fmt, va_list args)
{
    gc_log_lock.Enter();

    const int BUFFERSIZE = 4096;
    static char rgchBuffer[BUFFERSIZE];
    char *  pBuffer  = &rgchBuffer[0];

    pBuffer[0] = '\n';
    int buffer_start = 1;
    int pid_len = sprintf_s (&pBuffer[buffer_start], BUFFERSIZE - buffer_start,
        "[%5d]", (uint32_t)GCToOSInterface::GetCurrentThreadIdForLogging());
    buffer_start += pid_len;
    memset(&pBuffer[buffer_start], '-', BUFFERSIZE - buffer_start);
    int msg_len = _vsnprintf_s (&pBuffer[buffer_start], BUFFERSIZE - buffer_start, _TRUNCATE, fmt, args);
    if (msg_len == -1)
    {
        msg_len = BUFFERSIZE - buffer_start;
    }

    msg_len += buffer_start;

    if ((gc_log_buffer_offset + msg_len) > (gc_log_buffer_size - 12))
    {
        char index_str[8];
        memset (index_str, '-', 8);
        sprintf_s (index_str, ARRAY_SIZE(index_str), "%d", (int)gc_buffer_index);
        gc_log_buffer[gc_log_buffer_offset] = '\n';
        memcpy (gc_log_buffer + (gc_log_buffer_offset + 1), index_str, 8);

        gc_buffer_index++;
        if (gc_buffer_index > max_gc_buffers)
        {
            fseek (gc_log, 0, SEEK_SET);
            gc_buffer_index = 0;
        }
        fwrite(gc_log_buffer, gc_log_buffer_size, 1, gc_log);
        fflush(gc_log);
        memset (gc_log_buffer, '*', gc_log_buffer_size);
        gc_log_buffer_offset = 0;
    }

    memcpy (gc_log_buffer + gc_log_buffer_offset, pBuffer, msg_len);
    gc_log_buffer_offset += msg_len;

    gc_log_lock.Leave();
}

void GCLog (const char *fmt, ... )
{
    if (gc_log_on && (gc_log != NULL))
    {
        va_list     args;
        va_start(args, fmt);
        log_va_msg (fmt, args);
        va_end(args);
    }
}
#endif //TRACE_GC && SIMPLE_DPRINTF

#endif // !DACCESS_COMPILE
