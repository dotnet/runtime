// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++

Module Name:

    mono-cgroup.c

Abstract:
    Read the memory limit for the current process

    Adapted from runtime src/coreclr/gc/unix/cgroup.cpp 
    - commit 28ec20194010c2a3d06f2217998cfcb8e8b8fb5e
--*/
#ifdef __FreeBSD__
#define _WITH_GETLINE
#endif

#include <config.h>
#include <mono/utils/mono-compiler.h>

#if HAVE_CGROUP_SUPPORT

#include <unistd.h>
#include <stdlib.h>
#include <stdint.h>
#include <glib.h>
#include <stdio.h>
#include <string.h>
#include <sys/resource.h>
#if defined(__APPLE__) || defined(__FreeBSD__)
#include <sys/param.h>
#include <sys/mount.h>
#else
#include <sys/vfs.h>
#endif
#include <errno.h>
#include <limits.h>

#include <utils/mono-logger-internals.h>

#ifndef SIZE_T_MAX
# define SIZE_T_MAX (~(size_t)0)
#endif

#define CGROUP2_SUPER_MAGIC 0x63677270

#define PROC_MOUNTINFO_FILENAME "/proc/self/mountinfo"
#define PROC_CGROUP_FILENAME "/proc/self/cgroup"
#define PROC_STATM_FILENAME "/proc/self/statm"
#define CGROUP1_MEMORY_LIMIT_FILENAME "/memory.limit_in_bytes"
#define CGROUP2_MEMORY_LIMIT_FILENAME "/memory.max"
#define CGROUP_MEMORY_STAT_FILENAME "/memory.stat"
#define CGROUP1_MEMORY_USAGE_FILENAME "/memory.usage_in_bytes"
#define CGROUP2_MEMORY_USAGE_FILENAME "/memory.current"
#define CGROUP1_MEMORY_STAT_INACTIVE_FIELD "total_inactive_file "
#define CGROUP2_MEMORY_STAT_INACTIVE_FIELD "inactive_file "
#define CGROUP1_CFS_QUOTA_FILENAME "/cpu.cfs_quota_us"
#define CGROUP1_CFS_PERIOD_FILENAME "/cpu.cfs_period_us"
#define CGROUP2_CPU_MAX_FILENAME "/cpu.max"

static void initialize(void);
static gboolean readMemoryValueFromFile(const char *, size_t *);
static gboolean getPhysicalMemoryLimit(size_t *);
static gboolean getPhysicalMemoryUsage(size_t *);
static int findCGroupVersion(void);
static gboolean isCGroup1MemorySubsystem(const char *);
static gboolean isCGroup1CpuSubsystem(const char *);
static char *findCGroupPath(gboolean (*is_subsystem)(const char *));
static void findHierarchyMount(gboolean (*is_subsystem)(const char *), char **, char **);
static char *findCGroupPathForSubsystem(gboolean (*is_subsystem)(const char *));
static gboolean getCGroupMemoryLimit(size_t *, const char *);
static gboolean getCGroupMemoryUsage(size_t *, const char *, const char *);
static size_t getPhysicalMemoryTotal(size_t);
static long long readCpuCGroupValue(const char *);
static void computeCpuLimit(long long, long long, guint32 *);

size_t mono_get_restricted_memory_limit(void);
gboolean mono_get_memory_used(size_t *);
size_t mono_get_memory_avail(void);
gboolean mono_get_cpu_limit(guint *);
static gboolean readLongLongValueFromFile(const char *, long long *);

// the cgroup version number or 0 to indicate cgroups are not found or not enabled
static int s_cgroup_version = -1;

static char *s_memory_cgroup_path = NULL;
static char *s_cpu_cgroup_path = NULL;

static long pageSize;

/**
 * @brief Initialize variables used by the calculation routines.
 *
 */
static void 
initialize(void)
{
	s_cgroup_version = findCGroupVersion ();
	s_memory_cgroup_path = findCGroupPath (s_cgroup_version == 1 ? &isCGroup1MemorySubsystem : NULL);
	s_cpu_cgroup_path = findCGroupPath (s_cgroup_version == 1 ? &isCGroup1CpuSubsystem : NULL);

	if (s_cgroup_version == 0) 
		return;

	pageSize = sysconf (_SC_PAGE_SIZE);
}

/**
 *
 * @brief Read a value from a specified /sys/fs/cgroup/memory file
 *
 * @param[in] filename - name of file containing value
 * @param[out] val - pointer to the result area
 * @returns True or False depending if value was found
 *
 */
static gboolean 
readMemoryValueFromFile(const char* filename, size_t* val)
{
	gboolean result = FALSE;
	char *line = NULL;
	size_t lineLen = 0;
	char *endptr = NULL;
	FILE *file = NULL;

	if (val != NULL) {
		file = fopen (filename, "r");
		if (file != NULL) {
			if (getline (&line, &lineLen, file) != -1) {
				errno = 0;
				*val = strtoull (line, &endptr, 0);
				result = TRUE;
			}
		}
	}

	if (file)
		fclose (file);
	free (line);
	return result;
}

/**
 *
 * @brief Interrogate the cgroup memory values to determine if there's
 * a limit on physical memory.
 *
 * @param[out] val - pointer to the result area
 * @returns True or False depending if a limit was found
 *
 */
static gboolean 
getPhysicalMemoryLimit(size_t *val)
{
	if (s_cgroup_version == 0)
		return FALSE;
	else if (s_cgroup_version == 1)
		return getCGroupMemoryLimit (val, CGROUP1_MEMORY_LIMIT_FILENAME);
	else if (s_cgroup_version == 2)
		return getCGroupMemoryLimit (val, CGROUP2_MEMORY_LIMIT_FILENAME);
	else {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_CONFIG, 
			    "Unknown cgroup version.");
		return FALSE;
	}
}

/**
 *
 * @brief Interrogate the cgroup memory values to determine how much 
 * memory is in use.
 *
 * @param[out] val - pointer to the result area
 * @returns True or False depending if a usage value was found
 *
 */
static gboolean 
getPhysicalMemoryUsage(size_t *val)
{
	if (s_cgroup_version == 0)
		return FALSE;
	else if (s_cgroup_version == 1)
		return getCGroupMemoryUsage (val, CGROUP1_MEMORY_USAGE_FILENAME, CGROUP1_MEMORY_STAT_INACTIVE_FIELD);
	else if (s_cgroup_version == 2)
		return getCGroupMemoryUsage (val, CGROUP2_MEMORY_USAGE_FILENAME, CGROUP2_MEMORY_STAT_INACTIVE_FIELD);
	else {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_CONFIG, 
			    "Unknown cgroup version.");
		return FALSE;
	}
}

/**
 *
 * @brief Inspect the /sys/fs/cgroup hierachy to determine what version of 
 * group we are using
 *
 * @returns cgroup version
 *
 */
static int 
findCGroupVersion(void)
{
	// It is possible to have both cgroup v1 and v2 enabled on a system.
	// Most non-bleeding-edge Linux distributions fall in this group. We
	// look at the file system type of /sys/fs/cgroup to determine which
	// one is the default. For more details, see:
	// https://systemd.io/CGROUP_DELEGATION/#three-different-tree-setups-
	// We dont care about the difference between the "legacy" and "hybrid"
	// modes because both of those involve cgroup v1 controllers managing
	// resources.


	struct statfs stats;
	int result = statfs ("/sys/fs/cgroup", &stats);
	if (result != 0)
		return 0;

	if (stats.f_type == CGROUP2_SUPER_MAGIC) {
		return 2;
	} else {
		// Assume that if /sys/fs/cgroup exists and the file system type is not cgroup2fs,
		// it is cgroup v1. Typically the file system type is tmpfs, but other values have
		// been seen in the wild.
		return 1;
	}
}

/**
 *
 * @brief Check if we've found the memory component of /sys/fs/cgroup
 *
 * @param[in] strTok - Token for comparison
 * @returns True if token matches "memory"
 *
 */
static gboolean 
isCGroup1MemorySubsystem(const char *strTok)
{
	return strcmp ("memory", strTok) == 0;
}

/**
 *
 * @brief Check if we've found the CPU component of /sys/fs/cgroup
 *
 * @param[in] strTok - Token for comparison
 * @returns True if token matches "cpu"
 *
 */
static gboolean
isCGroup1CpuSubsystem(const char *strTok)
{
	return strcmp ("cpu", strTok) == 0;
}

/**
 *
 * @brief Navigate the /sys/fs/cgroup to try and find the correct cgroup path
 *
 * @param[in] is_subsystem - Function used to compare tokens
 * @returns Path to cgroup
 *
 */
static char *
findCGroupPath(gboolean (*is_subsystem)(const char *))
{
	char *cgroup_path = NULL;
	char *hierarchy_mount = NULL;
	char *hierarchy_root = NULL;
	char *cgroup_path_relative_to_mount = NULL;
	size_t common_path_prefix_len;

	findHierarchyMount (is_subsystem, &hierarchy_mount, &hierarchy_root);
	if (hierarchy_mount != NULL && hierarchy_root != NULL) {

		cgroup_path_relative_to_mount = findCGroupPathForSubsystem (is_subsystem);
		if (cgroup_path_relative_to_mount != NULL) {

			cgroup_path = (char*)malloc (strlen (hierarchy_mount) + strlen (cgroup_path_relative_to_mount) + 1);
			if (cgroup_path != NULL) {

				strcpy (cgroup_path, hierarchy_mount);
				// For a host cgroup, we need to append the relative path.
				// The root and cgroup path can share a common prefix of the path that should not be appended.
				// Example 1 (docker):
				// hierarchy_mount:               /sys/fs/cgroup/cpu
				// hierarchy_root:                /docker/87ee2de57e51bc75175a4d2e81b71d162811b179d549d6601ed70b58cad83578
				// cgroup_path_relative_to_mount: /docker/87ee2de57e51bc75175a4d2e81b71d162811b179d549d6601ed70b58cad83578/my_named_cgroup
				// append do the cgroup_path:     /my_named_cgroup
				// final cgroup_path:             /sys/fs/cgroup/cpu/my_named_cgroup
				//
				// Example 2 (out of docker)
				// hierarchy_mount:               /sys/fs/cgroup/cpu
				// hierarchy_root:                /
				// cgroup_path_relative_to_mount: /my_named_cgroup
				// append do the cgroup_path:     /my_named_cgroup
				// final cgroup_path:             /sys/fs/cgroup/cpu/my_named_cgroup
				common_path_prefix_len = strlen (hierarchy_root);
				if ((common_path_prefix_len == 1) || 
				    (strncmp (hierarchy_root, cgroup_path_relative_to_mount, common_path_prefix_len) != 0))
					common_path_prefix_len = 0;

				g_assert((cgroup_path_relative_to_mount[common_path_prefix_len] == '/') || 
				         (cgroup_path_relative_to_mount[common_path_prefix_len] == '\0'));

				strcat (cgroup_path, cgroup_path_relative_to_mount + common_path_prefix_len);
			}
		}
	}

	free (hierarchy_mount);
	free (hierarchy_root);
	free (cgroup_path_relative_to_mount);
	return cgroup_path;
}

/**
 *
 * @brief Check the /proc filesystem to determine the root and mount 
 * path of /sys/fs/cgroup data
 *
 * @param[in] is_subsystem - Comparison function
 * @param[out] pmountpath -
 * @param[out] pmountroot -
 *
 */
static void 
findHierarchyMount(gboolean (*is_subsystem)(const char *), char** pmountpath, char** pmountroot)
{
	char *line = NULL;
	size_t lineLen = 0, maxLineLen = 0;
	char *filesystemType = NULL;
	char *options = NULL;
	char *mountpath = NULL;
	char *mountroot = NULL;

	FILE *mountinfofile = fopen (PROC_MOUNTINFO_FILENAME, "r");
	if (mountinfofile == NULL) 
		goto done;

	while (getline (&line, &lineLen, mountinfofile) != -1) {
		if (filesystemType == NULL || lineLen > maxLineLen) {
			free (filesystemType);
			filesystemType = NULL;
			free (options);
			options = NULL;
			filesystemType = (char*)malloc (lineLen+1);
			if (filesystemType == NULL)
				goto done;
			options = (char*)malloc (lineLen+1);
			if (options == NULL)
				goto done;
			maxLineLen = lineLen;
		}

		char *separatorChar = strstr (line, " - ");

		// See man page of proc to get format for /proc/self/mountinfo file
		int sscanfRet = sscanf (separatorChar,
					   " - %s %*s %s",
					   filesystemType,
					   options);
		if (sscanfRet != 2) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_CONFIG, "Failed to parse mount info file contents with sscanf.");
			goto done;
		}

		if (strncmp(filesystemType, "cgroup", 6) == 0) {
			gboolean isSubsystemMatch = is_subsystem == NULL;
			if (!isSubsystemMatch) {
				char *context = NULL;
				char *strTok = strtok_r (options, ",", &context);
				while (!isSubsystemMatch && strTok != NULL)
				{
					isSubsystemMatch = is_subsystem (strTok);
					strTok = strtok_r (NULL, ",", &context);
				}
			}
			if (isSubsystemMatch) {
				mountpath = (char*)malloc (lineLen+1);
				if (mountpath == NULL)
					goto done;
				mountroot = (char*)malloc (lineLen+1);
				if (mountroot == NULL)
					goto done;

				sscanfRet = sscanf (line,
						   "%*s %*s %*s %s %s ",
						   mountroot,
						   mountpath);
				if (sscanfRet != 2)
					mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_CONFIG, 
						    "Failed to parse mount info file contents with sscanf.");

				// assign the output arguments and clear the locals so we don't free them.
				*pmountpath = mountpath;
				*pmountroot = mountroot;
				mountpath = mountroot = NULL;
			}
		}
	}
done:
	free (mountpath);
	free (mountroot);
	free (filesystemType);
	free (options);
	free (line);
	if (mountinfofile)
		fclose (mountinfofile);
}

/**
 *
 * @brief
 * Check the /proc filesystem to determine the root and mount path 
 * of /sys/fs/cgroup data
 *
 * @param[in] is_subsystem - Comparison function
 * @returns cgroup path for the memory subsystem
 *
 */
static char * 
findCGroupPathForSubsystem(gboolean (*is_subsystem)(const char *))
{
	char *line = NULL;
	size_t lineLen = 0;
	size_t maxLineLen = 0;
	char *subsystem_list = NULL;
	char *cgroup_path = NULL;
	gboolean result = FALSE;

	FILE *cgroupfile = fopen (PROC_CGROUP_FILENAME, "r");
	if (cgroupfile == NULL)
		goto done;

	while (!result && getline (&line, &lineLen, cgroupfile) != -1) {
		if (subsystem_list == NULL || lineLen > maxLineLen) {
			free (subsystem_list);
			subsystem_list = NULL;
			free (cgroup_path);
			cgroup_path = NULL;
			subsystem_list = (char*)malloc (lineLen+1);
			if (subsystem_list == NULL)
				goto done;
			cgroup_path = (char*)malloc (lineLen+1);
			if (cgroup_path == NULL)
				goto done;
			maxLineLen = lineLen;
		}

		if (s_cgroup_version == 1) {
			// See man page of proc to get format for /proc/self/cgroup file
			int sscanfRet = sscanf (line,
						"%*[^:]:%[^:]:%s",
						subsystem_list,
						cgroup_path);
			if (sscanfRet != 2) {
				mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_CONFIG, 
					    "Failed to parse cgroup info file contents with sscanf.");
				goto done;
			}

			char* context = NULL;
			char* strTok = strtok_r (subsystem_list, ",", &context);
			while (strTok != NULL) {
				if (is_subsystem (strTok)) {
					result = TRUE;
					break;
				}
				strTok = strtok_r (NULL, ",", &context);
			}
		} else if (s_cgroup_version == 2) {
			// See https://www.kernel.org/doc/Documentation/cgroup-v2.txt
			// Look for a "0::/some/path"
			int sscanfRet = sscanf (line,
						"0::%s",
						cgroup_path);
			if (sscanfRet == 1)
			{
				result = TRUE;
			}
		} else {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_CONFIG, 
				    "Unknown cgroup version in mountinfo.");
			goto done;
		}
	}
done:
	free (subsystem_list);
	if (!result) {
		free (cgroup_path);
		cgroup_path = NULL;
	}
	free (line);
	if (cgroupfile)
		fclose (cgroupfile);
	return cgroup_path;
}

/**
 *
 * @brief Extract memory limit from specified /sys/fs/cgroup/memory file
 *
 * @param[out] val - Memory limit
 * @param[in] filename - name of file from which to extract limit
 * @returns True if value found
 *
 */
static gboolean 
getCGroupMemoryLimit(size_t *val, const char *filename)
{
	if (s_memory_cgroup_path == NULL)
		return FALSE;

	char* mem_limit_filename = NULL;
	if (asprintf (&mem_limit_filename, "%s%s", s_memory_cgroup_path, filename) < 0)
		return FALSE;

	gboolean result = readMemoryValueFromFile (mem_limit_filename, val);
	free (mem_limit_filename);
	return result;
}

/**
 *
 * @brief Extract memory usage from /sys/fs/cgroup/memory.stat file
 *
 * @param[out] val - Memory limit
 * @returns True if value found
 *
 */
static gboolean 
getCGroupMemoryUsage(size_t *val, const char *filename, const char *inactiveFileFieldName)
{
	/* 
	 * Use the same way to calculate memory load as popular container tools (Docker, Kubernetes, Containerd etc.)
	 * For cgroup v1: value of 'memory.usage_in_bytes' minus 'total_inactive_file' value of 'memory.stat'
	 * For cgroup v2: value of 'memory.current' minus 'inactive_file' value of 'memory.stat'
	 */

	char *mem_usage_filename = NULL;
	if (asprintf (&mem_usage_filename, "%s%s", s_memory_cgroup_path, filename) < 0)
		return FALSE;

	size_t temp = 0;
	size_t usage = 0;

	gboolean result = readMemoryValueFromFile (mem_usage_filename, &temp);
	if (result) {
		if (temp > SIZE_T_MAX)
			usage = SIZE_T_MAX;
		else
			usage = temp;
	}

	free (mem_usage_filename);

	if (!result)
		return result;

	if (s_memory_cgroup_path == NULL)
		return FALSE;

	char *stat_filename = NULL;
	if (asprintf (&stat_filename, "%s%s", s_memory_cgroup_path, CGROUP_MEMORY_STAT_FILENAME) < 0)
		return FALSE;

	FILE *stat_file = fopen (stat_filename, "r");
	free (stat_filename);
	if (stat_file == NULL)
		return FALSE;

	char *line = NULL;
	size_t lineLen = 0;
	gboolean foundInactiveFileValue = FALSE;
	char *endptr;

	size_t inactiveFileFieldNameLength = strlen (inactiveFileFieldName);

	while (getline (&line, &lineLen, stat_file) != -1) {
		if (strncmp (line, inactiveFileFieldName, inactiveFileFieldNameLength) == 0) {
			errno = 0;
			const char *startptr = line + inactiveFileFieldNameLength;
			size_t inactiveFileValue = strtoll (startptr, &endptr, 10);
			if (endptr != startptr && errno == 0) {
				foundInactiveFileValue = TRUE;
				*val = usage - inactiveFileValue;
			}
			break;
		}
	}

	fclose (stat_file);
	free (line);

	return foundInactiveFileValue;
}

/**
 *
 * @brief Determine if there are any limits on memory and return the value
 *
 * @returns Physical memory limit
 *
 * Zero represents no limit.
 */
size_t 
mono_get_restricted_memory_limit(void)
{
	size_t physical_memory_limit = 0;

	if (s_cgroup_version == -1)
		initialize();

	if (s_cgroup_version == 0)
		return 0;

	if (!getPhysicalMemoryLimit (&physical_memory_limit))
		return 0;

	// If there's no memory limit specified on the container this
	// actually returns 0x7FFFFFFFFFFFF000 (2^63-1 rounded down to
	// 4k which is a common page size). So we know we are not
	// running in a memory restricted environment.
	if (physical_memory_limit > 0x7FFFFFFF00000000)
		return 0;

	return (getPhysicalMemoryTotal (physical_memory_limit));
}

/**
 *
 * @brief Check the input limit against any system limits or actual memory on system
 *
 * @param[in] physical_memory_limit - The max memory on the system
 * @returns Physical memory total
 *
 */
static size_t
getPhysicalMemoryTotal(size_t physical_memory_limit)
{
	struct rlimit curr_rlimit;
	size_t rlimit_soft_limit = (size_t)RLIM_INFINITY;
	if (getrlimit (RLIMIT_AS, &curr_rlimit) == 0)
		rlimit_soft_limit = curr_rlimit.rlim_cur;
	physical_memory_limit = (physical_memory_limit < rlimit_soft_limit) ?
				 physical_memory_limit : rlimit_soft_limit;

	// Ensure that limit is not greater than real memory size
	long pages = sysconf (_SC_PHYS_PAGES);
	if (pages != -1) {
		if (pageSize != -1) {
			physical_memory_limit = (physical_memory_limit < (size_t)pages * pageSize) ?
						 physical_memory_limit : (size_t)pages * pageSize;
		}
	}

	if (physical_memory_limit > ULONG_MAX) {
		// It is observed in practice when the memory is unrestricted, Linux control
		// group returns a physical limit that is bigger than the address space
		return ULONG_MAX;
	} else
		return physical_memory_limit;
}

/**
 *
 * @brief Determine the amount of memory in use
 *
 * @param[out] val - pointer to the memory usage value 
 * @returns True if we are able to determine usage
 *
 */
gboolean 
mono_get_memory_used(size_t *val)
{
	gboolean result = FALSE;
	size_t linelen;
	char *line = NULL;

	if (val == NULL)
		return FALSE;

	// Linux uses cgroup usage to trigger oom kills.
	if (getPhysicalMemoryUsage (val))
		return TRUE;

	// process resident set size.
	FILE* file = fopen (PROC_STATM_FILENAME, "r");
	if (file != NULL && getline (&line, &linelen, file) != -1) {
		char* context = NULL;
		char* strTok = strtok_r (line, " ", &context);
		strTok = strtok_r (NULL, " ", &context);

		errno = 0;
		*val = strtoull (strTok, NULL, 0);
		if (errno == 0) {
			if (pageSize != -1) {
				*val = *val * pageSize;
				result = TRUE;
			}
		}
	}

	if (file)
		fclose (file);
	free (line);
	return result;
}

/**
 *
 * @brief Determine the amount of memory available by examininig any 
 * limits and checking what memory is in use.
 *
 * @returns Amount of memory available
 *
 */
size_t
mono_get_memory_avail(void)
{
	size_t max, used, avail, sysAvail;
#ifdef _SC_AVPHYS_PAGES		// If this isn't defined then we don't get called

	max = mono_get_restricted_memory_limit ();

	if (max == 0)
		max = getPhysicalMemoryTotal (ULONG_MAX);

	if (mono_get_memory_used (&used))
		avail = max - used;
	else
		avail = max;

	sysAvail = sysconf (_SC_AVPHYS_PAGES) * pageSize;
	return (avail < sysAvail ? avail : sysAvail);
#else
	return (0);
#endif
}

/**
 *
 * @brief Return any limits on CPU use
 *
 * @returns Number of CPU usable
 *
 */
static gboolean
getCGroup1CpuLimit(guint32 *val)
{
	long long quota;
	long long period;

	quota = readCpuCGroupValue (CGROUP1_CFS_QUOTA_FILENAME);
	if (quota <= 0)
		return FALSE;

	period = readCpuCGroupValue (CGROUP1_CFS_PERIOD_FILENAME);
	if (period <= 0)
		return FALSE;

	computeCpuLimit (period, quota, val);

	return TRUE;
}

/**
 *
 * @brief Return any limits on CPU use
 *
 * @returns Number of CPU usable
 *
 */
static gboolean 
getCGroup2CpuLimit(guint32 *val)
{
	char *filename = NULL;
	FILE *file = NULL;
	char *endptr = NULL;
	char *max_quota_string = NULL;
	char *period_string = NULL;
	char *context = NULL;
	char *line = NULL;
	size_t lineLen = 0;

	long long quota = 0;
	long long period = 0;


	gboolean result = FALSE;

	if (s_cpu_cgroup_path == NULL)
		return FALSE;

	if (asprintf (&filename, "%s%s", s_cpu_cgroup_path, CGROUP2_CPU_MAX_FILENAME) < 0)
		return FALSE;

	file = fopen (filename, "r");
	if (file == NULL)
		goto done;

	if (getline (&line, &lineLen, file) == -1)
		goto done;

	// The expected format is:
	//     $MAX $PERIOD
	// Where "$MAX" may be the string literal "max"

	max_quota_string = strtok_r (line, " ", &context);
	if (max_quota_string == NULL)
	{
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_CONFIG, 
			    "Unable to parse " CGROUP2_CPU_MAX_FILENAME " file contents.");
		goto done;
	}
	period_string = strtok_r (NULL, " ", &context);
	if (period_string == NULL)
	{
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_CONFIG, 
			    "Unable to parse " CGROUP2_CPU_MAX_FILENAME " file contents.");
		goto done;
	}

	// "max" means no cpu limit
	if (strcmp ("max", max_quota_string) == 0)
		goto done;

	errno = 0;
	quota = strtoll (max_quota_string, &endptr, 10);
	if (max_quota_string == endptr || errno != 0)
		goto done;

	period = strtoll (period_string, &endptr, 10);
	if (period_string == endptr || errno != 0)
		goto done;

	computeCpuLimit (period, quota, val);
	result = TRUE;

done:
	if (file)
		fclose (file);
	free (filename);
	free (line);

	return result;
}

/**
 *
 * @brief Compute the CPU limit based on the CGroup data
 *
 * @param[in] period
 * @param[in] quota - Limit found in sysfs
 * @param[out] Number of CPU usable
 *
 */
static void 
computeCpuLimit(long long period, long long quota, uint32_t *val)
{
	// Cannot have less than 1 CPU
	if (quota <= period) {
		*val = 1;
		return;
	}

	// Calculate cpu count based on quota and round it up
	double cpu_count = (double) quota / period  + 0.999999999;
	*val = (cpu_count < UINT32_MAX) ? (uint32_t)cpu_count : UINT32_MAX;
}

/**
 *
 * @brief Read the CGroup CPU data from sysfs
 *
 * @param[in] subsystemFileName - sysfs File containing data
 * @returns CPU CGroup value
 *
 */
static long long 
readCpuCGroupValue(const char *subsystemFilename)
{
	char *filename = NULL;
	gboolean result = FALSE;
	long long val = -1;

	if (s_cpu_cgroup_path == NULL)
		return -1;

	if (asprintf (&filename, "%s%s", s_cpu_cgroup_path, subsystemFilename) < 0)
		return -1;

	result = readLongLongValueFromFile (filename, &val);
	free (filename);
	if (!result)
		return -1;

	return val;
}

/**
 *
 * @brief Read a long long value from a file
 *
 * @param[in] fileName - sysfs File containing data
 * @param[out] val - Value read
 * @returns Success indicator
 *
 */
static gboolean 
readLongLongValueFromFile(const char *filename, long long *val)
{
	gboolean result = FALSE;
	char *line = NULL;
	size_t lineLen = 0;
	char *endptr = NULL;

	if (val == NULL)
		return FALSE;

	FILE *file = fopen (filename, "r");
	if (file == NULL)
		return FALSE;

	if (getline (&line, &lineLen, file) != -1) {
		errno = 0;
		*val = strtoll (line, &endptr, 10);
		if (line != endptr && errno == 0)
			result = TRUE;
	}

	fclose (file);
	free (line);
	return result;
}

/**
 *
 * @brief Interrogate the cgroup CPU values to determine if there's
 * a limit on CPUs
 *
 * @param[out] val - pointer to the result area
 * @returns True or False depending if a limit was found
 *
 * Interrogate the cgroup CPU values to determine if there's
 * a limit on CPUs
 */
gboolean 
mono_get_cpu_limit(guint *val)
{
	if (s_cgroup_version == -1)
		initialize();

	if (s_cgroup_version == 0)
		return FALSE;
	else if (s_cgroup_version == 1)
		return getCGroup1CpuLimit ((guint32 *)val);
	else if (s_cgroup_version == 2)
		return getCGroup2CpuLimit ((guint32 *)val);
	else {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_CONFIG, 
			    "Unknown cgroup version.");
		return FALSE;
	}
}
#else

MONO_EMPTY_SOURCE_FILE (mono_cgroup);

#endif
