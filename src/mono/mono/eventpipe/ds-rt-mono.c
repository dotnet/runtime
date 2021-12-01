#include <config.h>

#ifdef ENABLE_PERFTRACING
#include <eventpipe/ds-rt-config.h>
#include <eventpipe/ds-types.h>
#include <eventpipe/ds-rt.h>

#ifdef __APPLE__
#define APPLICATION_CONTAINER_BASE_PATH_SUFFIX "/Library/Group Containers/"

// Not much to go with, but Max semaphore length on Mac is 31 characters. In a sandbox, the semaphore name
// must be prefixed with an application group ID. This will be 10 characters for developer ID and extra 2
// characters for group name. For example ABCDEFGHIJ.MS. We still need some characters left
// for the actual semaphore names.
#define MAX_APPLICATION_GROUP_ID_LENGTH 13
#endif // __APPLE__

#if defined (__linux__) && !defined (HAVE_PROCFS_STAT)
#define HAVE_PROCFS_STAT
#endif

/*
 * Forward declares of all static functions.
 */

#ifndef HOST_WIN32
static
bool
ipc_get_process_id_disambiguation_key (
	uint32_t process_id,
	uint64_t *key);
#endif /* !HOST_WIN32 */

#ifndef HOST_WIN32

#include <unistd.h>
#include <sys/types.h>
#include <sys/time.h>
#include <pwd.h>

#ifdef __APPLE__
#include <sys/sysctl.h>
#endif

#ifdef __NetBSD__
#include <sys/cdefs.h>
#include <sys/param.h>
#include <sys/sysctl.h>
#include <kvm.h>
#endif

#ifdef __FreeBSD__
#include <sys/sysctl.h>
#include <sys/user.h>
#endif

/*
Get a numeric value that can be used to disambiguate between processes with the same PID,
provided that one of them is still running. The numeric value can mean different things
on different platforms, so it should not be used for any other purpose. Under the hood,
it is implemented based on the creation time of the process.
*/
static
bool
ipc_get_process_id_disambiguation_key (
	uint32_t process_id,
	uint64_t *key)
{
	if (!key) {
		EP_ASSERT (!"key argument cannot be null!");
		return false;
	}

	*key = 0;

#if defined (__APPLE__) || defined (__FreeBSD__)
	// On OS X, we return the process start time expressed in Unix time (the number of seconds
	// since the start of the Unix epoch).
	struct kinfo_proc info = {};
	size_t size = sizeof (info);
	int mib [4] = { CTL_KERN, KERN_PROC, KERN_PROC_PID, process_id };

	const int result_sysctl = sysctl (mib, sizeof(mib)/sizeof(*mib), &info, &size, NULL, 0);
	if (result_sysctl == 0) {
#if defined (__APPLE__)
		struct timeval proc_starttime = info.kp_proc.p_starttime;
#else // __FreeBSD__
		struct timeval proc_starttime = info.ki_start;
#endif
		long seconds_since_epoch = proc_starttime.tv_sec;
		*key = seconds_since_epoch;
		return true;
	} else {
		EP_ASSERT (!"Failed to get start time of a process.");
		return false;
	}
#elif defined (__NetBSD__)
	// On NetBSD, we return the process start time expressed in Unix time (the number of seconds
	// since the start of the Unix epoch).
	kvm_t *kd;
	int cnt;
	struct kinfo_proc2 *info;

	kd = kvm_open (NULL, NULL, NULL, KVM_NO_FILES, "kvm_open");
	if (!kd) {
		EP_ASSERT (!"Failed to get start time of a process.");
		return false;
	}

	info = kvm_getproc2 (kd, KERN_PROC_PID, process_id, sizeof (struct kinfo_proc2), &cnt);
	if (!info || cnt < 1) {
		kvm_close (kd);
		EP_ASSERT (!"Failed to get start time of a process.");
		return false;
	}

	kvm_close (kd);

	long seconds_since_epoch = info->p_ustart_sec;
	*key = seconds_since_epoch;

	return true;
#elif defined (HAVE_PROCFS_STAT)
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

	char *line = NULL;
	size_t line_len = 0;
	if (getline (&line, &line_len, stat_file) == -1)
	{
		EP_ASSERT (!"Failed to get start time of a process, getline failed.");
		return false;
	}

	unsigned long long start_time;

	// According to `man proc`, the second field in the stat file is the filename of the executable,
	// in parentheses. Tokenizing the stat file using spaces as separators breaks when that name
	// has spaces in it, so we start using sscanf_s after skipping everything up to and including the
	// last closing paren and the space after it.
	char *scan_start_position = strrchr (line, ')');
	if (!scan_start_position || scan_start_position [1] == '\0') {
		EP_ASSERT (!"Failed to parse stat file contents with strrchr.");
		return false;
	}

	scan_start_position += 2;

	// All the format specifiers for the fields in the stat file are provided by 'man proc'.
	int result_sscanf = sscanf (scan_start_position,
		"%*c %*d %*d %*d %*d %*d %*u %*u %*u %*u %*u %*u %*u %*d %*d %*d %*d %*d %*d %llu \n",
		&start_time);

	if (result_sscanf != 1) {
		EP_ASSERT (!"Failed to parse stat file contents with sscanf.");
		return false;
	}

	free (line);
	fclose (stat_file);

	*key = (uint64_t)start_time;
	return true;
#else
	// If we don't have /proc, we just return false.
	DS_LOG_WARNING_0 ("ipc_get_process_id_disambiguation_key was called but is not implemented on this platform!");
	return false;
#endif
}

bool
ds_rt_mono_transport_get_default_name (
	ep_char8_t *name,
	uint32_t name_len,
	const ep_char8_t *prefix,
	int32_t id,
	const ep_char8_t *group_id,
	const ep_char8_t *suffix)
{
	EP_ASSERT (name != NULL);
	EP_ASSERT (name > 0);

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
	if (!ipc_get_process_id_disambiguation_key (id, &disambiguation_key))
		EP_ASSERT (disambiguation_key == 0);
#ifdef __APPLE__
	if (group_id) {
		// Verify the length of the group id
		size_t group_id_len = strlen (group_id);
		if (group_id_len > MAX_APPLICATION_GROUP_ID_LENGTH) {
			DS_LOG_ERROR_0 ("The length of group_id is larger than MAX_APPLICATION_GROUP_ID_LENGTH");
			ep_raise_error ();
		}

		// In sandbox, all IPC files (locks, pipes) should be written to the application group
		// container. The path returned by GetTempPathA will be unique for each process and cannot
		// be used for IPC between two different processes
		const char *home_dir = getpwuid (getuid ())->pw_dir;
		size_t home_dir_len = strlen (home_dir);

		// Verify the size of the path won't exceed maximum allowed size
		if ((home_dir_len + strlen (APPLICATION_CONTAINER_BASE_PATH_SUFFIX) + group_id_len + 1) >= name_len) {
			DS_LOG_ERROR_0 ("Application container folder path is larger than name_len");
			ep_raise_error ();
		}

		format_result = snprintf (format_buffer, name_len, "%s%s%s/", home_dir, APPLICATION_CONTAINER_BASE_PATH_SUFFIX, group_id);
		if (format_result <= 0 || (uint32_t)format_result > name_len) {
			DS_LOG_ERROR_0 ("format_buffer to small");
			ep_raise_error ();
		}

	} else
#endif // __APPLE__
	{
		// Get a temp file location
		format_result = ep_rt_temp_path_get (format_buffer, name_len);
		if (format_result == 0) {
			DS_LOG_ERROR_0 ("ep_rt_temp_path_get failed");
			ep_raise_error ();
		}

		EP_ASSERT (format_result <= name_len);
	}

	format_result = snprintf(name, name_len, "%s%s-%d-%llu-%s", format_buffer, prefix, id, (unsigned long long)disambiguation_key, suffix);
	if (format_result <= 0 || (uint32_t)format_result > name_len) {
		DS_LOG_ERROR_0 ("name buffer to small");
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
}
#else /* !HOST_WIN32 */
bool
ds_rt_mono_transport_get_default_name (
	ep_char8_t *name,
	uint32_t name_len,
	const ep_char8_t *prefix,
	int32_t id,
	const ep_char8_t *group_id,
	const ep_char8_t *suffix)
{
	// Currently not used on Windows.
	g_assert_not_reached ();
}
#endif /* !HOST_WIN32 */
#endif /* ENABLE_PERFTRACING */

MONO_EMPTY_SOURCE_FILE(diagnostics_rt_mono);
