// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    misc/perftrace.c

Abstract:
    Implementation of PAL Performance trace utilities.



--*/

/* PAL headers */



#ifdef PAL_PERF

/* PAL Headers */
#include "pal/palinternal.h"
#include "pal/perftrace.h"
#include "pal/dbgmsg.h"
#include "pal/cruntime.h"
#include "pal/misc.h"

/* Standard headers */
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <errno.h>
#include <pthread.h> /* for pthread_self */
#include <dirent.h>
#include <unistd.h>

SET_DEFAULT_DEBUG_CHANNEL(MISC);


#define PAL_PERF_MAX_LOGLINE     0x400  /* 1K */
#define PAL_PERF_MAX_INPUT       0x1000 /* 4k for single line of input file */
#define PAL_PERF_MAX_FUNCTION_NAME 128 /* any one want a function name longer than 127 bytes? */
#define PAL_PERF_PROFILE_BUFFER_SIZE 0x400000  /* 4M    */
#define PAL_PERF_BUFFER_FULL  (PAL_PERF_PROFILE_BUFFER_SIZE - PAL_PERF_MAX_LOGLINE ) /* (Buffer size - 1K) */

typedef struct _pal_perf_api_info
{
    ULONGLONG       entries;        /* number of PERF_ENTRY calls for an API function */
    ULONGLONG       counter;        /* number of PERF_EXIT calls for an API function */
    ULONGLONG       min_duration;   /* Minimum duration in CPU clock ticks in an API function */
    ULONGLONG       max_duration;   /* Maximum duration in CPU clock ticks in an API function */
    ULONGLONG       sum_duration;   /* Sum of duration*/
    double          sum_of_square_duration; /* Sum of square of durations */
    DWORD           *histograms;    /* An array to store the histogram of an API execution cpu ticks. */
} pal_perf_api_info;


typedef struct _pal_perf_thread_info
{
    DWORD               threadId;
    pal_perf_api_info * api_table;
    char *              pal_write_buf;
    DWORD               buf_offset;
    BOOL                profile_enabled;
    ULONGLONG           start_ticks;
    ULONGLONG           total_duration;
} pal_perf_thread_info;

typedef struct _pal_thread_list_node
{
    pal_perf_thread_info * thread_info;
    struct _pal_thread_list_node * next;

} pal_thread_list_node;

typedef struct _pal_perf_program_info
{
    char    command_line[PAL_PERF_MAX_LOGLINE];
    char    exe_path[PAL_PERF_MAX_LOGLINE];
    char    hostname[PAL_PERF_MAX_FUNCTION_NAME];
    double  cpu_clock_frequency;
    ULONGLONG start_ticks;
    ULONGLONG elapsed_time; /* Duration in CPU clock ticks of the program */
    ULONGLONG total_duration; /* Total CPU clock ticks of all the threads */
    ULONGLONG pal_duration; /* Total CPU clock ticks spent inside PAL */

    pid_t   process_id;
    char    start_time[32]; /*  must be at least 26 characters */
} pal_perf_program_info;

static ULONGLONG PERFGetTicks();
static double PERFComputeStandardDeviation(pal_perf_api_info *api);
static void PERFPrintProgramHeaderInfo(FILE * hFile, BOOL completedExecution);
static BOOL PERFInitProgramInfo(LPWSTR command_line, LPWSTR exe_path);
static BOOL PERFReadSetting( );
static void PERFLogFileName(PathCharString * destFileString, const char *fileName, const char *suffix, int max_length);
static void PERFlushAllLogs();
static int PERFWriteCounters(pal_perf_api_info * table);
static BOOL PERFFlushLog(pal_perf_thread_info * local_buffer, BOOL output_header);
static void PERFUpdateApiInfo(pal_perf_api_info *api, ULONGLONG duration);
static char * PERFIsValidPath( const char * path );
static char * PERFIsValidFile( const char * path, const char * file);

typedef char PAL_API_NAME[PAL_PERF_MAX_FUNCTION_NAME];

static PAL_API_NAME API_list[PAL_API_NUMBER] ;
static pal_perf_program_info program_info;

static pthread_key_t PERF_tlsTableKey=0 ;

static pal_thread_list_node * process_pal_thread_list=NULL;
static BOOL pal_profile_on=FALSE;
static BOOL pal_perf_enabled=FALSE;
static char * pal_function_map=NULL;
static char * perf_default_path=NULL;
static char * traced_apis_file=NULL;
static char * enabledapis_path=NULL;
static char * profile_log_path=NULL;
static char * profile_summary_log_name=NULL;
static char * profile_time_log_name=NULL;
static BOOL summary_only=FALSE;
static BOOL nested_tracing=FALSE;
static BOOL calibrate=FALSE;

/* If report_only_called_apis is TRUE,
   those PAL APIs with no function entry or exit
   will not be shown in the PAL perf summary file. */
static BOOL report_only_called_apis=FALSE;

/* If the wait_for_startup is TRUE, process profiling
   will not start until the application
   has called PAL_EnableProcessProfile(). */
static BOOL wait_for_startup=FALSE;

/* The size of a PAL API execution CPU ticks histogram, i.e.,
   Number of categories of frequency distrubution of PAL API
   execution CPU ticks.*/
static DWORD pal_perf_histogram_size = 0;

/* The step size in CPU ticks of each category of the
   PAL API execution CPU ticks histogram.*/
static DWORD pal_perf_histogram_step = 100;

static const char PAL_PERF_TRACING[]="PAL_PERF_TRACING";
static const char PAL_DEFAULT_PATH[]="PAL_PERF_DEFAULT_PATH";
static const char PAL_PERF_TRACEDAPIS_PATH[]="PAL_PERF_TRACEDAPIS_FILE";
static const char PAL_PERF_LOG_PATH[]="PAL_PERF_LOG_PATH";
static const char PAL_PERF_SUMMARY_LOG_NAME[]="PAL_PERF_SUMMARY_LOG_NAME";
static const char PAL_PERF_TIME_LOG_NAME[]="PAL_PERF_TIME_LOG_NAME";
static const char PAL_PERF_ENABLED_APIS_PATH[]="PAL_PERF_ENABLEDAPIS_FILE";
static const char PAL_SUMMARY_FLAG[]="PAL_PERF_SUMMARY_ONLY";
static const char PAL_PERF_NESTED_TRACING[]="PAL_PERF_NESTED_TRACING";
static const char PAL_PERF_CALIBRATE[]="PAL_PERF_CALIBRATE";
static const char PAL_PERF_REPORT_ONLY_CALLED_APIS[]="PAL_PERF_REPORT_ONLY_CALLED_APIS";
static const char PAL_PERF_WAIT_FOR_STARTUP[]="PAL_PERF_WAIT_FOR_STARTUP";
static const char PAL_PERF_HISTOGRAM_SIZE[]="PAL_PERF_HISTOGRAM_SIZE";
static const char PAL_PERF_HISTOGRAM_STEP[]="PAL_PERF_HISTOGRAM_STEP";
static const char traced_apis_filename[]="PerfTracedAPIs.txt";
static const char perf_enabled_filename[]="AllPerfEnabledAPIs.txt";
static const char PATH_SEPARATOR[] = "/";



#define LLFORMAT "%llu"

static
ULONGLONG
PERFGetTicks(){
#ifdef HOST_X86 // for BSD and Windows.
    unsigned long a, d;
  #ifdef _MSC_VER
  __asm{
            rdtsc
            mov a, eax
            mov d, edx
       }
  #else
  #undef volatile
  asm volatile("rdtsc":"=a" (a), "=d" (d));
  #define volatile DoNotUseVolatileKeyword
  #endif
  return ((ULONGLONG)((unsigned int)(d)) << 32) | (unsigned int)(a);
#else
  return 0; // on non-BSD and non-Windows, we'll return 0 for now.
#endif // HOST_X86
}

static
double
PERFComputeStandardDeviation(pal_perf_api_info *api)
{
    double n;
    double sum_of_variance;
    if (api->counter <= 1)
        return 0.0;
    n = (double) api->counter;
    // Calculates standard deviation based on the entire population given as arguments.
    // Same as stdevp in Excel.
    sum_of_variance = (n*api->sum_of_square_duration) - (api->sum_duration*api->sum_duration);
    if (sum_of_variance <= 0.0)
        return 0.0;
    return sqrt(sum_of_variance/(n*n));
}


static
void
PERFPrintProgramHeaderInfo(FILE * hFile, BOOL completedExecution)
{
    ULONGLONG etime = 0;
    ULONGLONG ttime = 0;
    ULONGLONG ptime = 0;
    if (completedExecution) {
       etime = program_info.elapsed_time;
       ttime = program_info.total_duration;
       ptime = program_info.pal_duration;
    }
    fprintf(hFile,"#LOG\tversion=1.00\n");

    fprintf(hFile, "#MACHINE\thostname=%s\tcpu_clock_frequency=%g\n", program_info.hostname,
        program_info.cpu_clock_frequency);
    fprintf(hFile, "#PROCESS\tprocess_id=%d\ttotal_latency=" LLFORMAT "\tthread_times=" LLFORMAT "\tpal_time=" LLFORMAT "\texe_path=%s\tcommand_line=%s\tstart_time=%s",
        program_info.process_id, etime, ttime, ptime,
        program_info.exe_path,program_info.command_line,program_info.start_time);
}

static
BOOL
PERFInitProgramInfo(LPWSTR command_line, LPWSTR exe_path)
{
    ULONGLONG start_tick;
    struct timeval tv;

    if (WideCharToMultiByte(CP_ACP, 0, command_line, -1,
                        program_info.command_line, PAL_PERF_MAX_LOGLINE-1, NULL, NULL) == 0)
        return FALSE;
    if (WideCharToMultiByte(CP_ACP, 0, exe_path, -1,
                        program_info.exe_path, PAL_PERF_MAX_LOGLINE-1, NULL, NULL) == 0)
        return FALSE;

    gethostname(program_info.hostname, PAL_PERF_MAX_FUNCTION_NAME);
    program_info.process_id = getpid();

    gettimeofday(&tv, NULL);
    ctime_r(&tv.tv_sec, program_info.start_time);

    // estimate the cpu clock cycles
    start_tick = PERFGetTicks();
    if (start_tick != 0)
    {
        sleep(1);
        program_info.cpu_clock_frequency = (double) (PERFGetTicks() - start_tick);
    }
    else
    {
        program_info.cpu_clock_frequency = 0.0;
    }

    program_info.start_ticks = 0;
    program_info.elapsed_time = 0;
    program_info.total_duration = 0;
    program_info.pal_duration = 0;

    return TRUE;
}

static
void
PERFCalibrationFunction()
{
    PERF_ENTRY(CalibrationFunction);
    PERF_EXIT(CalibrationFunction);
}

void
PERFCalibrate(const char* msg)
{
    ULONGLONG start_tick, cal_ticks;
    int i=0;
    int cal_length=100000;

    if (calibrate) {
       start_tick = PERFGetTicks();
       for(i=0; i<cal_length; i++)
       {
          PERFCalibrationFunction();
       }
       cal_ticks = PERFGetTicks() - start_tick;
       printf("%s: %g\n", msg, (double)(cal_ticks/cal_length));
    }
}

BOOL
PERFInitialize(LPWSTR command_line, LPWSTR exe_path)
{
    BOOL bRead;
    BOOL ret = TRUE;

    // Check if PAL Perf should be disabled
    char *pal_perf_tracing_env = MiscGetenv(PAL_PERF_TRACING);
    if ( pal_perf_tracing_env == NULL || strlen(pal_perf_tracing_env) == 0)
    {
        pal_perf_enabled = FALSE;
        return TRUE;
    }
    else
    {
        pal_perf_enabled = TRUE;
    }
    if (!PERFInitProgramInfo(command_line, exe_path))
        return FALSE;

    pal_profile_on = FALSE;  // turn it off until we setup everything.
    // allocate the TLS index for  structures
    if( pthread_key_create(&PERF_tlsTableKey , NULL) != 0 )
       ret = FALSE;

    if( ret == TRUE )
    {
        pal_function_map = (char*)PAL_malloc(PAL_API_NUMBER);
        if(pal_function_map != NULL)
        {
            bRead = PERFReadSetting( );  // we don't quit even we failed to read the file.
            ret = TRUE;
        }
        /* free the index in TLS */
        else
        {

            pthread_key_delete(PERF_tlsTableKey );
            ret = FALSE;
        }
    }

    PERFCalibrate("Overhead when profiling is disabled process-wide");

    return ret;
}


void PERFTerminate(  )
{
    static LONG pal_perf_terminated = FALSE;

    if (!pal_perf_enabled || wait_for_startup)
        return;

    // make sure PERFTerminate is called only once
    if (InterlockedCompareExchange(&pal_perf_terminated, TRUE, FALSE))
        return;

    PERFlushAllLogs();
    pthread_key_delete(PERF_tlsTableKey );
    PAL_free(pal_function_map);
}


BOOL PERFAllocThreadInfo(  )
{
    pal_perf_api_info * apiTable = NULL;
    pal_thread_list_node * node = NULL;
    pal_perf_thread_info * local_info = NULL;
    char * log_buf = NULL;
    int i;
    BOOL ret = TRUE;

    if (!pal_perf_enabled)
        return TRUE;

    /*  The memory allocated per thread for PAL perf tracing is never freed until PAL_Terminate
        is called in the current implementation. If the test program keeps creating new threads,
        memory resources could be exhausted. If this ever becomes a problem, the memory allocated
        per thread should be freed when a thread exits. */

    node = ( pal_thread_list_node * )PAL_malloc(sizeof(pal_thread_list_node));
    if(node == NULL)
    {
        ret = FALSE;
        goto PERFAllocThreadInfoExit;
    }

    local_info = (pal_perf_thread_info *)PAL_malloc(sizeof(pal_perf_thread_info));
    if (local_info == NULL)
    {
        ret = FALSE;
        goto PERFAllocThreadInfoExit;
    }

    apiTable = (pal_perf_api_info *)PAL_malloc( PAL_API_NUMBER *  sizeof(pal_perf_api_info));
    if (apiTable == NULL)
    {
        ret = FALSE;
        goto PERFAllocThreadInfoExit;
    }

    node->thread_info = local_info;
    local_info->api_table=apiTable;
    local_info->threadId = THREADSilentGetCurrentThreadId();

    for (i = 0; i < PAL_API_NUMBER; i++)
    {
        apiTable[i].entries = 0;
        apiTable[i].counter = 0;
        apiTable[i].min_duration = _UI64_MAX;
        apiTable[i].max_duration = 0;
        apiTable[i].sum_duration = 0;
        apiTable[i].sum_of_square_duration = 0.0;
        if (pal_perf_histogram_size > 0)
        {
            apiTable[i].histograms = (DWORD *)PAL_malloc(pal_perf_histogram_size*sizeof(DWORD));
            if (apiTable[i].histograms == NULL)
            {
                ret = FALSE;
                goto PERFAllocThreadInfoExit;
            }
            memset(apiTable[i].histograms, 0, pal_perf_histogram_size*sizeof(DWORD));
        }
        else
        {
            apiTable[i].histograms = NULL;
        }
    }

    log_buf = (char * )PAL_malloc( PAL_PERF_PROFILE_BUFFER_SIZE );

    if(log_buf == NULL)
    {
        ret = FALSE;
        goto PERFAllocThreadInfoExit;
    }

    local_info->pal_write_buf=log_buf;
    local_info->buf_offset = 0;
    local_info->profile_enabled = FALSE;
    local_info->total_duration = 0;
    local_info->start_ticks = 0;
    memset(log_buf, 0, PAL_PERF_PROFILE_BUFFER_SIZE);

    if (pthread_setspecific(PERF_tlsTableKey, local_info) != 0)
       ret = FALSE;

PERFAllocThreadInfoExit:
    if (ret == TRUE)
    {
        node->next = process_pal_thread_list;
        process_pal_thread_list = node;
        PERFFlushLog(local_info, TRUE);
    }
    else
    {
        if (node != NULL)
        {
            PAL_free(node);
        }
        if (local_info != NULL)
        {
            PAL_free(local_info);
        }
        if (apiTable != NULL)
        {
            for (i = 0; i < PAL_API_NUMBER; i++)
            {
                if (apiTable[i].histograms != NULL)
                {
                    PAL_free(apiTable[i].histograms);
                }
            }
            PAL_free(apiTable);
        }
        if (log_buf != NULL)
        {
            PAL_free(log_buf);
        }
    }
    return ret;
}

static
void
PERFUpdateProgramInfo(pal_perf_thread_info* local_info)
{
    int i;

    if (!local_info) return;

    // add the elapsed time to the program's total
    if (local_info->total_duration == 0)
    {
        // this thread did not go through PERFDisableThreadProfile code
        // so compute the total elapsed time for the thread here
        local_info->total_duration = PERFGetTicks() - local_info->start_ticks;
    }
    program_info.total_duration += local_info->total_duration;

    // Add up all the time spent in PAL
    if (local_info->api_table) {
       for(i=0; i<PAL_API_NUMBER; i++) {
         program_info.pal_duration += local_info->api_table[i].sum_duration;
       }
    }
}


static
void
PERFlushAllLogs( )
{
    pal_thread_list_node * current, * node;
    pal_perf_api_info * table1, *table0;
    int i;
    node = process_pal_thread_list;
    if(node == NULL || node->thread_info == NULL || node->thread_info->api_table == NULL )   // should not come here
    {
        return ;
    }
    process_pal_thread_list = process_pal_thread_list->next;
    table0 = node->thread_info->api_table;

    PERFUpdateProgramInfo(node->thread_info);

    while(process_pal_thread_list)
    {
        current=process_pal_thread_list;
        process_pal_thread_list = process_pal_thread_list->next;
        if (current->thread_info)
        {
            if (current->thread_info->api_table)
            {
                table1 = current->thread_info->api_table;
                for(i=0;i<PAL_API_NUMBER;i++)
                {
                    DWORD j;
                    if (table1[i].counter == 0)
                    {
                        continue;
                    }
                    for (j = 0; j < pal_perf_histogram_size; j++)
                    {
                        table0[i].histograms[j] += table1[i].histograms[j];
                    }
                    table0[i].entries += table1[i].entries;
                    table0[i].counter += table1[i].counter;
                    if (table0[i].min_duration > table1[i].min_duration)
                        table0[i].min_duration = table1[i].min_duration;
                    if (table0[i].max_duration < table1[i].max_duration)
                        table0[i].max_duration = table1[i].max_duration;
                    table0[i].sum_duration += table1[i].sum_duration;
                    table0[i].sum_of_square_duration += table1[i].sum_of_square_duration;
               }
                PERFUpdateProgramInfo(current->thread_info);
                if (table1->histograms != NULL)
                {
                    PAL_free(table1->histograms);
                }
                PAL_free(table1);
            }
            PERFFlushLog(current->thread_info, FALSE);
            PAL_free(current->thread_info->pal_write_buf);
            PAL_free(current->thread_info);
        }
        PAL_free(current);
    }
    PERFWriteCounters(table0);
    if (table0->histograms != NULL)
    {
        PAL_free(table0->histograms);
    }
    PAL_free(table0);
    PERFFlushLog(node->thread_info, FALSE);
    PAL_free(node->thread_info->pal_write_buf);
    PAL_free(node->thread_info);
    PAL_free(node);
}

static
void
PERFLogFileName(PathCharString& destFileString, const char *fileName, const char *suffix)
{
    const char *dir_path;
    CPalThread* pThread = InternalGetCurrentThread();
    dir_path = (profile_log_path == NULL) ? "." : profile_log_path;

    destFileString.Append(dir_path, strlen(dir_path));
    destFileString.Append(PATH_SEPARATOR, strlen(PATH_SEPARATOR));
    if (fileName != NULL)
    {
        destFileString.Append(fileName, strlen(fileName));
    }
    else
    {
        char buffer[33];
        char* process_id     = itoa(program_info.process_id, buffer, 10);
        destFileString.Append(process_id, strlen(process_id));
        destFileString.Append("_", 1);

        char* current_thread = itoa(THREADSilentGetCurrentThreadId(),buffer, 10);
        destFileString.Append(current_thread, strlen( current_thread));
        destFileString.Append(suffix, strlen(suffix));
    }

}

static
int
PERFWriteCounters( pal_perf_api_info * table )
{
    PathCharString fileName;
    pal_perf_api_info * off;
    PERF_FILE * hFile;
    int i;

    off = table;

    PERFLogFileName(fileName, profile_summary_log_name, "_perf_summary.log");
    hFile = fopen(fileName, "a+");
    if(hFile != NULL)
    {
        PERFPrintProgramHeaderInfo(hFile, TRUE);
        fprintf(hFile,"#api_name\tapi_id\tperf_entries\tperf_exits\tsum_of_latency\tmin_latency\tmax_latency\tstd_dev_latency\tsum_of_square_latency\n");
        for(i=0;i<PAL_API_NUMBER;i++)
        {
            double dev;
            ULONGLONG min_duration;

            min_duration = (off->min_duration == _UI64_MAX) ? 0 : off->min_duration;
            if (off->counter >= 1)
            {
                dev = PERFComputeStandardDeviation(off);
            }
            else
            {
                dev = 0.0;
            }

            if (off->counter > 0 || !report_only_called_apis)
            {
                fprintf(hFile,"%s\t%d\t" LLFORMAT "\t" LLFORMAT "\t" LLFORMAT "\t" LLFORMAT "\t" LLFORMAT "\t%g\t%g\n",
                    API_list[i], i, off->entries, off->counter,off->sum_duration,
                    min_duration, off->max_duration, dev, off->sum_of_square_duration);
            }

            off++;
        }
    }
    else
    {
        return -1;
    }
    fclose(hFile);

    if (pal_perf_histogram_size > 0)
    {
        off = table;
        PERFLogFileName(fileName, profile_summary_log_name, "_perf_summary.hist");
        hFile = fopen(fileName, "a+");

        if (hFile != NULL)
        {
            DWORD j;
            fprintf(hFile,"#api_name\tapi_id");
            for (j = 0; j < pal_perf_histogram_size; j++)
            {
                fprintf(hFile, "\t%d", j*pal_perf_histogram_step);
            }
            fprintf(hFile, "\n");

            for(i = 0; i < PAL_API_NUMBER; i++)
            {
                if (off->counter > 0)
                {
                    fprintf(hFile,"%s\t%d", API_list[i], i);

                    for (j = 0; j < pal_perf_histogram_size; j++)
                    {
                        fprintf(hFile, "\t%d", off->histograms[j]);
                    }

                    fprintf(hFile, "\n");
                }

                off++;
            }
        }
        else
        {
            return -1;
        }
        fclose(hFile);
    }

    return 0;
}

static
BOOL
PERFReadSetting(  )
{
    // this function is not safe right now.
    //more code is required to deal with corrupted input file.
    BOOL ret;
    unsigned int index;
    char line[PAL_PERF_MAX_INPUT];
    char * ptr;
    char function_name[PAL_PERF_MAX_FUNCTION_NAME];  //no function can be longer than 127 bytes.

    char * file_name_buf;
    PathCharString file_name_bufPS;
    char  * input_file_name;
    char  * summary_flag_env;
    char  * nested_tracing_env;
    char  * calibrate_env;
    char  * report_only_called_apis_env;
    char  * wait_for_startup_env;
    char  * pal_perf_histogram_size_env;
    char  * pal_perf_histogram_step_env;

    PAL_FILE * hFile;

    if((pal_function_map == NULL) || (PAL_API_NUMBER < 0) )
    {
        // should not be here.
    }

    /* do some env setting here */
    summary_flag_env = MiscGetenv(PAL_SUMMARY_FLAG);
    if (summary_flag_env == NULL || strlen(summary_flag_env) == 0)
    {
        summary_only = FALSE;
    }
    else
    {
        summary_only = TRUE;
    }
    nested_tracing_env = MiscGetenv(PAL_PERF_NESTED_TRACING);
    if (nested_tracing_env == NULL || strlen(nested_tracing_env) == 0)
    {
        nested_tracing = FALSE;
    }
    else
    {
        nested_tracing = TRUE;
    }

    calibrate_env = MiscGetenv(PAL_PERF_CALIBRATE);
    if (calibrate_env == NULL || strlen(calibrate_env) == 0)
    {
        calibrate = FALSE;
    }
    else
    {
        calibrate = TRUE;
    }

    report_only_called_apis_env = MiscGetenv(PAL_PERF_REPORT_ONLY_CALLED_APIS);
    if (report_only_called_apis_env == NULL || strlen(report_only_called_apis_env) == 0)
    {
        report_only_called_apis = FALSE;
    }
    else
    {
        report_only_called_apis = TRUE;
    }

    wait_for_startup_env = MiscGetenv(PAL_PERF_WAIT_FOR_STARTUP);
    if (wait_for_startup_env == NULL || strlen(wait_for_startup_env) == 0)
    {
        wait_for_startup = FALSE;
    }
    else
    {
        wait_for_startup = TRUE;
    }

    pal_perf_histogram_size_env = MiscGetenv(PAL_PERF_HISTOGRAM_SIZE);
    if (pal_perf_histogram_size_env != NULL && strlen(pal_perf_histogram_size_env) > 0)
    {
        long value;
        char *endptr;
        value = strtol(pal_perf_histogram_size_env, &endptr, 10);
        if (value > 0)
        {
            pal_perf_histogram_size = (DWORD) value;
        }
    }

    pal_perf_histogram_step_env = MiscGetenv(PAL_PERF_HISTOGRAM_STEP);
    if (pal_perf_histogram_step_env != NULL && strlen(pal_perf_histogram_step_env) > 0)
    {
        long value;
        char *endptr;
        value = strtol(pal_perf_histogram_step_env, &endptr, 10);
        if (value > 0)
        {
            pal_perf_histogram_step = (DWORD) value;
        }
    }

    traced_apis_file = PERFIsValidFile("", MiscGetenv(PAL_PERF_TRACEDAPIS_PATH));
    enabledapis_path = PERFIsValidFile("", MiscGetenv(PAL_PERF_ENABLED_APIS_PATH));
    profile_log_path = PERFIsValidPath(MiscGetenv(PAL_PERF_LOG_PATH));
    perf_default_path = PERFIsValidPath( MiscGetenv(PAL_DEFAULT_PATH));
    profile_summary_log_name = MiscGetenv(PAL_PERF_SUMMARY_LOG_NAME);
    if (profile_summary_log_name != NULL && strlen(profile_summary_log_name) == 0)
        profile_summary_log_name = NULL;
    profile_time_log_name = MiscGetenv(PAL_PERF_TIME_LOG_NAME);
    if (profile_time_log_name != NULL && strlen(profile_time_log_name) == 0)
        profile_time_log_name = NULL;

    if( traced_apis_file == NULL)
    {
        if(perf_default_path==NULL)
        {
            ret=FALSE;
            input_file_name = NULL;
        }
        else
        {
            if( PERFIsValidFile(perf_default_path,traced_apis_filename))
            {
                int length = strlen(perf_default_path) + strlen(PATH_SEPARATOR) + strlen(traced_apis_filename);
                file_name_buf = file_name_bufPS.OpenStringBuffer(length);
                if ((strcpy_s(file_name_buf, file_name_bufPS.GetSizeOf(), perf_default_path) != SAFECRT_SUCCESS) ||
                    (strcat_s(file_name_buf, file_name_bufPS.GetSizeOf(), PATH_SEPARATOR) != SAFECRT_SUCCESS) ||
                    (strcat_s(file_name_buf, file_name_bufPS.GetSizeOf(), traced_apis_filename) != SAFECRT_SUCCESS))
                {
                    file_name_bufPS.CloseBuffer(0);
                    ret = FALSE;
                    input_file_name = NULL;
                }
                else
                {
                    file_name_bufPS.CloseBuffer(length);
                    input_file_name = file_name_buf;
                }
            }
            else
            {
                ret = FALSE;
                input_file_name=NULL;
            }
        }
    }
    else
    {
        input_file_name=traced_apis_file;
    }

    if(input_file_name)
    {
        hFile = PAL_fopen(input_file_name, "r+");
        if ( hFile == NULL )
        {
            memset(pal_function_map, 1, PAL_API_NUMBER);
            ret = FALSE;
        }
        else
        {
            memset(pal_function_map, 0, PAL_API_NUMBER);

            PAL_fseek(hFile, 0L, SEEK_SET);

            /* Read a line of data from file: */
            while ( PAL_fgets(line, PAL_PERF_MAX_INPUT, hFile) != NULL )
            {
                if(strlen(line)==0)
                    continue;
                ptr = strchr( line, '#');
                if( ptr )
                    continue;
                sscanf_s(line, "%s %u", function_name,&index);

                if( index >= PAL_API_NUMBER)
                {
                        // some code here to deal with incorrect index.
                        // use function name to cover it.
                }
                else if(pal_function_map[index]==1)
                {
                    // some code here to deal with conflict index.
                    // use function name to cover it.
                }
                else
                {
                    pal_function_map[index]=1;
                }

            }

            PAL_fclose(hFile);
            ret = TRUE;
        }
    }
    else
    {
        memset(pal_function_map, 1, PAL_API_NUMBER);
        ret = FALSE;
    }

    if( enabledapis_path == NULL)
    {
        if(perf_default_path==NULL)
        {
            input_file_name = NULL;
        }
        else
        {
            if( PERFIsValidFile(perf_default_path,perf_enabled_filename))
            {
                if ((strcpy_s(file_name_buf, sizeof(file_name_buf), perf_default_path) != SAFECRT_SUCCESS) ||
                    (strcat_s(file_name_buf, sizeof(file_name_buf), PATH_SEPARATOR) != SAFECRT_SUCCESS) ||
                    (strcat_s(file_name_buf, sizeof(file_name_buf), perf_enabled_filename) != SAFECRT_SUCCESS))
                {
                    ret = FALSE;
                    input_file_name = NULL;
                }
                else
                {
                    input_file_name = file_name_buf;
                }
            }
            else
            {
               input_file_name=NULL;
            }
        }
    }
    else
    {
        input_file_name=enabledapis_path;
    }

    if(input_file_name == NULL)
    {
        return ret;
    }

    hFile = PAL_fopen(input_file_name, "r+");

    if ( hFile != NULL )
    {
        PAL_fseek(hFile, 0L, SEEK_SET);

        /* Read a line of data from file: */
        while (PAL_fgets(line, PAL_PERF_MAX_INPUT, hFile) != NULL)
        {
            if(strlen(line)==0)
                continue;
            ptr = strchr( line, '#');
            if( ptr )
                continue;
            sscanf_s(line, "%s %u", function_name,&index);

            if( index >= PAL_API_NUMBER)
            {
                // some code here to deal with incorrect index.
                // use function name to cover it.
                continue;
            }

            if (strcpy_s(API_list[index], sizeof(API_list[index]), function_name) != SAFECRT_SUCCESS)
            {
                ret = FALSE;
                break;
            }
        }

        PAL_fclose(hFile);
    }

    return ret;

}


static
BOOL
PERFFlushLog(pal_perf_thread_info * local_info, BOOL output_header)
{
    BOOL ret = FALSE;
    PathCharString fileName;
    int nWrittenBytes = 0;
    PERF_FILE * hFile;

    if (summary_only)
        return TRUE;

    PERFLogFileName(fileName, profile_time_log_name, "_perf_time.log");

    hFile = fopen(fileName, "a+");

    if(hFile)
    {
        if (output_header)
        {
            PERFPrintProgramHeaderInfo(hFile, FALSE);
        }
        if (local_info->buf_offset > 0)
        {
            nWrittenBytes = fwrite(local_info->pal_write_buf, local_info->buf_offset, 1, hFile);
            if (nWrittenBytes < 1)
            {
                ERROR("fwrite() failed with errno == %d\n", errno);
                return ret;
            }
            local_info->buf_offset = 0;
        }
        fclose(hFile);
        ret = TRUE;
    }

    return ret;
}

void
PERFLogFunctionEntry(unsigned int pal_api_id, ULONGLONG *pal_perf_start_tick )
{
    pal_perf_thread_info * local_info=NULL;
    pal_perf_api_info * table;
    char * write_buf;
    __int32  buf_off;
    short bufused = 0;


    struct timeval tv;


    if(!pal_perf_enabled || pal_function_map==NULL || !pal_profile_on )  // haven't initialize, just quit.
        return;

    if( pal_function_map[pal_api_id] )
    {
        local_info= (pal_perf_thread_info * )pthread_getspecific(PERF_tlsTableKey);

        if (local_info==NULL  )
        {
            return;
        }
        if ( !local_info->profile_enabled ) /*  prevent recursion. */
        {
            return;
        }
        // turn on this flag before call any other functions
        local_info->profile_enabled = FALSE;
        table = local_info->api_table;
        table[pal_api_id].entries++;

        if(!summary_only)
        {
            write_buf = (local_info->pal_write_buf);
            if(local_info->buf_offset >= PAL_PERF_BUFFER_FULL)
            {
                PERFFlushLog(local_info, FALSE);
            }

            gettimeofday(&tv, NULL);

            buf_off = local_info->buf_offset;

            bufused = snprintf(&write_buf[buf_off], PAL_PERF_MAX_LOGLINE, "----> %d %lu %06u entry.\n", pal_api_id, tv.tv_sec,  tv.tv_usec );
            local_info->buf_offset += bufused;
        }
        if(nested_tracing)
            local_info->profile_enabled = TRUE;
        *pal_perf_start_tick = PERFGetTicks();
    }
    return;
}

static
void
PERFUpdateApiInfo(pal_perf_api_info *api, ULONGLONG duration)
{
    DWORD iBucket;

    api->counter++;
    if (api->min_duration > duration)
        api->min_duration = duration;
    if (api->max_duration < duration)
        api->max_duration = duration;
    api->sum_duration += duration;
    api->sum_of_square_duration += (double) duration * (double)duration;

    if (pal_perf_histogram_size > 0)
    {
        iBucket = (DWORD)(duration / pal_perf_histogram_step);
        if (iBucket >= pal_perf_histogram_size)
        {
            iBucket = pal_perf_histogram_size - 1;
        }
        api->histograms[iBucket]++;
    }

}

void
PERFLogFunctionExit(unsigned int pal_api_id, ULONGLONG *pal_perf_start_tick )
{

    pal_perf_thread_info * local_info;
    char * buf;
    short bufused = 0;
    DWORD  off;
    ULONGLONG duration = 0;
    struct timeval timev;


    if(!pal_perf_enabled || (pal_function_map == NULL) || !pal_profile_on ) // haven't initiallize yet, just quit.
        return;

    if (*pal_perf_start_tick != 0)
    {
        duration = PERFGetTicks() - *pal_perf_start_tick;
    }
    else
    {
        return; // pal_perf_start_tick == 0 indicates that we exited PERFLogFunctionEntry before getting the ticks.
    }

    if( pal_function_map[pal_api_id] )
    {
        local_info = (pal_perf_thread_info*)pthread_getspecific(PERF_tlsTableKey);

        if (NULL == local_info ){
            return;
        }
        PERFUpdateApiInfo(&local_info->api_table[pal_api_id], duration);
        *pal_perf_start_tick = 0;

        if(summary_only)
        {
            local_info->profile_enabled = TRUE;
            return;
        }

        gettimeofday(&timev, NULL);

        buf = local_info->pal_write_buf;
        if(local_info->buf_offset >= PAL_PERF_BUFFER_FULL)
        {
            PERFFlushLog(local_info, FALSE);
        }
        off = local_info->buf_offset;

        bufused = snprintf(&buf[off], PAL_PERF_MAX_LOGLINE, "<---- %d %lu %06u exit. \n", pal_api_id, timev.tv_sec,  timev.tv_usec );
        local_info->buf_offset += bufused;
        local_info->profile_enabled = TRUE;
    }
    return;
}

void
PERFNoLatencyProfileEntry(unsigned int pal_api_id )
{
    pal_perf_thread_info * local_info=NULL;
    pal_perf_api_info * table;

    if(!pal_perf_enabled || pal_function_map==NULL || !pal_profile_on )  // haven't initialize, just quit.
        return;
    if( pal_function_map[pal_api_id] )
    {
         local_info= (pal_perf_thread_info * )pthread_getspecific(PERF_tlsTableKey);
         if (local_info==NULL  )
         {
            return;
         }
         else{
            table = local_info->api_table;
            table[pal_api_id].entries++;
        }
   }
   return;
}


void
PERFEnableThreadProfile(BOOL isInternal)
{
    pal_perf_thread_info * local_info;
    if (!pal_perf_enabled)
        return;
    if (NULL != (local_info = (pal_perf_thread_info*)pthread_getspecific(PERF_tlsTableKey)))
    {
         if (!isInternal || nested_tracing) {
            local_info->profile_enabled = TRUE;
            local_info->start_ticks = PERFGetTicks();
         }
    }
}


void
PERFDisableThreadProfile(BOOL isInternal)
{
    pal_perf_thread_info * local_info;
    if (!pal_perf_enabled)
        return;
    if (NULL != (local_info = (pal_perf_thread_info*)pthread_getspecific(PERF_tlsTableKey)))
    {
         if (!isInternal || nested_tracing) {
            local_info->profile_enabled = FALSE;
            local_info->total_duration = PERFGetTicks() - local_info->start_ticks;
         }
    }
}


void
PERFEnableProcessProfile(  )
{
    if (!pal_perf_enabled || wait_for_startup)
        return;
    pal_profile_on = TRUE;
    PERFCalibrate("Overhead when profiling is disabled temporarily for a thread");
    // record the cpu clock ticks at the beginning of the profiling.
    program_info.start_ticks = PERFGetTicks();
}


void
PERFDisableProcessProfile( )
{
    if (!pal_perf_enabled)
        return;
    pal_profile_on = FALSE;
    // compute the total program duration in cpu clock ticks.
    if (program_info.start_ticks != 0)
    {
        program_info.elapsed_time += (PERFGetTicks() - program_info.start_ticks);
        program_info.start_ticks = 0;
    }
}

BOOL
PERFIsProcessProfileEnabled(  )
{
    return pal_profile_on;
}

static
char *
PERFIsValidPath( const char * path )
{
    DIR * dir;

    if(( path==NULL) || (strlen(path)==0))
       return NULL;

    dir = opendir(path);
    if( dir!=NULL)
    {
        closedir(dir);
        return ((char *)path);
    }
    return NULL;
}

static
char *
PERFIsValidFile( const char * path, const char * file)
{
    FILE * hFile;
    char * temp;
    PathCharString tempPS;

    if(file==NULL || strlen(file)==0)
        return NULL;

	if ( strcmp(path, "") )
    {
        int length = strlen(path) + strlen(PATH_SEPARATOR) + strlen(file);
        temp = tempPS.OpenStringBuffer(length);
        if ((strcpy_s(temp, sizeof(temp), path) != SAFECRT_SUCCESS) ||
            (strcat_s(temp, sizeof(temp), PATH_SEPARATOR) != SAFECRT_SUCCESS) ||
            (strcat_s(temp, sizeof(temp), file) != SAFECRT_SUCCESS))
        {
            tempPS.CloseBuffer(0);
            return NULL;
        }

        tempPS.CloseBuffer(length);
        hFile = fopen(temp, "r");
    }
    else
	{
        hFile = fopen(file, "r");
	}

    if(hFile)
    {
	    fclose(hFile);
        return ((char *) file);
    }
	else
		return NULL;

}

PALIMPORT
VOID
PALAPI
PAL_EnableProcessProfile(VOID)
{
    wait_for_startup = FALSE;
    pal_profile_on = TRUE;
    PERFEnableProcessProfile();
}

PALIMPORT
VOID
PALAPI
PAL_DisableProcessProfile(VOID)
{
    pal_profile_on = FALSE;
    PERFDisableProcessProfile();
}

PALIMPORT
BOOL
PALAPI
PAL_IsProcessProfileEnabled(VOID)
{
    return PERFIsProcessProfileEnabled();
}

PALIMPORT
INT64
PALAPI
PAL_GetCpuTickCount(VOID)
{
    return PERFGetTicks();
}

#endif /* PAL_PERF */




