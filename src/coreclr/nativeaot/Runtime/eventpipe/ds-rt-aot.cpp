// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <eventpipe/ep-rt-config.h>

#ifdef ENABLE_PERFTRACING
#include <eventpipe/ep-types.h>
#include <eventpipe/ep.h>
#include <eventpipe/ep-stack-contents.h>
#include <eventpipe/ep-rt.h>

#include "ds-rt-aot.h"

bool aot_ipc_get_process_id_disambiguation_key(uint32_t process_id, uint64_t *key);

bool
aot_ipc_get_process_id_disambiguation_key(
    uint32_t process_id,
    uint64_t *key)
{
    if (!key) {
        EP_ASSERT (!"key argument cannot be null!");
        return false;
    }

    *key = 0;

// Mono implementation, restricted just to Unix
#ifdef TARGET_UNIX

    // Here we read /proc/<pid>/stat file to get the start time for the process.
    // We return this value (which is expressed in jiffies since boot time).

    // Making something like: /proc/123/stat
    char stat_file_name [64];
    snprintf (stat_file_name, sizeof (stat_file_name), "/proc/%d/stat", process_id);

    FILE *stat_file = fopen (stat_file_name, "r");
    if (!stat_file) {
        EP_ASSERT (!"Failed to get start time of a process, fopen failed.");
        return false;
    }

    bool result = false;
    unsigned long long start_time = 0;
    char *scan_start_position;
    int result_sscanf;

    char *line = NULL;
    size_t line_len = 0;
    if (getline (&line, &line_len, stat_file) == -1)
    {
        EP_ASSERT (!"Failed to get start time of a process, getline failed.");
        ep_raise_error ();
    }


    // According to `man proc`, the second field in the stat file is the filename of the executable,
    // in parentheses. Tokenizing the stat file using spaces as separators breaks when that name
    // has spaces in it, so we start using sscanf_s after skipping everything up to and including the
    // last closing paren and the space after it.
    scan_start_position = strrchr (line, ')');
    if (!scan_start_position || scan_start_position [1] == '\0') {
        EP_ASSERT (!"Failed to parse stat file contents with strrchr.");
        ep_raise_error ();
    }

    scan_start_position += 2;

    // All the format specifiers for the fields in the stat file are provided by 'man proc'.
    result_sscanf = sscanf (scan_start_position,
        "%*c %*d %*d %*d %*d %*d %*u %*u %*u %*u %*u %*u %*u %*d %*d %*d %*d %*d %*d %llu \n",
        &start_time);

    if (result_sscanf != 1) {
        EP_ASSERT (!"Failed to parse stat file contents with sscanf.");
        ep_raise_error ();
    }

    free (line);
    fclose (stat_file);
    result = true;

ep_on_exit:
    *key = (uint64_t)start_time;
    return result;

ep_on_error:
    free (line);
    fclose (stat_file);
    result = false;
    ep_exit_error_handler ();

#else
    // If we don't have /proc, we just return false.
    DS_LOG_WARNING_0 ("ipc_get_process_id_disambiguation_key was called but is not implemented on this platform!");
    return false;
#endif
}

bool
ds_rt_aot_transport_get_default_name (
    ep_char8_t *name,
    int32_t name_len,
    const ep_char8_t *prefix,
    int32_t id,
    const ep_char8_t *group_id,
    const ep_char8_t *suffix)
{
    STATIC_CONTRACT_NOTHROW;
    
#ifdef TARGET_UNIX

    EP_ASSERT (name != NULL);

    bool result = false;
    int32_t format_result = 0;
    uint64_t disambiguation_key = 0;
    ep_char8_t *format_buffer = NULL;

    *name = '\0';

    format_buffer = (ep_char8_t *)malloc (name_len + 1);
    ep_raise_error_if_nok (format_buffer != NULL);

    *format_buffer = '\0';

    // If ipc_get_process_id_disambiguation_key failed for some reason, it should set the value
    // to 0. We expect that anyone else making the pipe name will also fail and thus will
    // also try to use 0 as the value.
    if (!aot_ipc_get_process_id_disambiguation_key (id, &disambiguation_key))
        EP_ASSERT (disambiguation_key == 0);
    
    // Get a temp file location
    format_result = ep_rt_temp_path_get (format_buffer, name_len);
    if (format_result == 0) {
        DS_LOG_ERROR_0 ("ep_rt_temp_path_get failed");
        ep_raise_error ();
    }

    EP_ASSERT (format_result <= name_len);

    format_result = snprintf(name, name_len, "%s%s-%d-%llu-%s", format_buffer, prefix, id, (unsigned long long)disambiguation_key, suffix);
    if (format_result <= 0 || format_result > name_len) {
        DS_LOG_ERROR_0 ("name buffer too small");
        ep_raise_error ();
    }

    result = true;

ep_on_exit:
    free (format_buffer);
    return result;

ep_on_error:
    EP_ASSERT (!result);
    name [0] = '\0';
    ep_exit_error_handler ();

#else
    return true;
#endif
}
#endif /* ENABLE_PERFTRACING */
