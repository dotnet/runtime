/**
 * \file
 * Support for interop with the Microsoft Error Reporting tool
 *
 * Author:
 *   Alexander Kyte (alkyte@microsoft.com)
 *
 * (C) 2018 Microsoft, Inc.
 *
 */

#include <config.h>
#include <glib.h>

#if defined(TARGET_OSX) && !defined(DISABLE_CRASH_REPORTING)
#include "mono-merp.h"

#include <unistd.h>
#include <spawn.h>

// OSX OS stuff now, for merpGUI interop
#include <mach/mach.h>
#include <mach/task_info.h>
#include <mach/mach_types.h>
#include <mach/mach_traps.h>
#include <servers/bootstrap.h>

#include <metadata/locales.h>
#include <mini/jit.h>

#if defined(HAVE_SYS_UTSNAME_H)
#include <sys/utsname.h>
#endif

// To get the apple machine model
#include <sys/param.h>
#include <sys/sysctl.h>
#include <fcntl.h>

#include <mono/utils/json.h>
#include <mono/utils/mono-state.h>
#include <utils/mono-threads-debug.h>

static const char *
os_version_string (void)
{
#ifdef HAVE_SYS_UTSNAME_H
	struct utsname name;

	memset (&name, 0, sizeof (name)); // WSL does not always nul terminate.

	if (uname (&name) >= 0)
		return g_strdup_printf ("%s", name.release);
#endif
	return "";
}

// To get the path of the running process
#include <libproc.h>

typedef enum {
	MerpArchInvalid = 0,

	MerpArchx86_64 = 1,
	MerpArchx86 = 2,
	MerpArchPPC = 3,
	MerpArchPPC64 = 4
} MerpArch;

typedef enum
{
	MERP_EXC_NONE = 0,

	MERP_EXC_FORCE_QUIT = 1,
	MERP_EXC_SIGSEGV = 2,
	MERP_EXC_SIGABRT = 3,
	MERP_EXC_SIGSYS  = 4,
	MERP_EXC_SIGILL = 5,
	MERP_EXC_SIGBUS = 6,
	MERP_EXC_SIGFPE = 7 ,
	MERP_EXC_SIGTRAP = 8,
	MERP_EXC_SIGKILL = 9,
	MERP_EXC_HANG  = 10
} MERPExcType;

typedef struct {
	const char *merpFilePath;
	const char *crashLogPath;
	const char *werXmlPath;

	const char *bundleIDArg; // App Bundle ID (required for bucketization)
	const char *versionArg; // App Version (required for bucketization)

	MerpArch archArg; // Arch, MacOS only, bails out if not found also required for bucketization. (required)
	MERPExcType exceptionArg; // Exception type (refer to merpcommon.h and mach/exception_types.h for more info (optional)

	const char *serviceNameArg; // This is the Bootstrap service name that MERP GUI will create to receive mach_task_self on a port created. Bails out if MERP GUI fails to receive mach_task_self from the crashed app. (Required for crash log generation)
	const char *servicePathArg; // The path to the executable, used to relaunch the crashed app.

	const char *moduleName;
	const char *moduleVersion;
	size_t moduleOffset;

	const char *osVersion; 
	int uiLidArg; // MONO_LOCALE_INVARIANT 0x007F

	char systemModel [100];
	const char *systemManufacturer;

	const char *eventType;

	MonoStackHash hashes;
} MERPStruct;

typedef struct {
	gboolean enable_merp;

	const char *appBundleID;
	const char *appPath;
	const char *appSignature; 
	const char *appVersion;
	const char *merpGUIPath; 
	const char *eventType;
	const char *merpFilePath;
	const char *crashLogPath;
	const char *werXmlPath;
	const char *moduleVersion;

	gboolean log;
} MerpOptions;

static MerpOptions config;

static const char *
get_merp_bitness (MerpArch arch)
{
	switch (arch) {
		case MerpArchx86_64:
			return "x64";
		case MerpArchx86:
			return "x32";
		default:
			g_assert_not_reached ();
	}
}

static MerpArch
get_merp_arch (void)
{
#ifdef TARGET_X86
	return MerpArchx86;
#elif defined(TARGET_AMD64)
	return MerpArchx86_64;
#elif defined(TARGET_POWERPC)
	return MerpArchPPC;
#elif defined(TARGET_POWERPC64)
	return MerpArchPPC64;
#else
	g_assert_not_reached ();
#endif
}

static const char *
get_merp_exctype (MERPExcType exc)
{
	switch (exc) {
		case MERP_EXC_FORCE_QUIT:
			return "0x10000000";
		case MERP_EXC_SIGSEGV:
			return "0x20000000";
		case MERP_EXC_SIGABRT:
			return "0x30000000";
		case MERP_EXC_SIGSYS:
			return "0x40000000";
		case MERP_EXC_SIGILL:
			return "0x50000000";
		case MERP_EXC_SIGBUS:
			return "0x60000000";
		case MERP_EXC_SIGFPE:
			return "0x70000000";
		case MERP_EXC_SIGTRAP:
			return "0x03000000";
		case MERP_EXC_SIGKILL:
			return "0x04000000";
		case MERP_EXC_HANG: 
			return "0x02000000";
		case MERP_EXC_NONE:
			// Exception type documented as optional, not optional
			g_assert_not_reached ();
		default:
			g_assert_not_reached ();
	}
}

static MERPExcType
parse_exception_type (const char *signal)
{
	if (!strcmp (signal, "SIGSEGV"))
		return MERP_EXC_SIGSEGV;

	if (!strcmp (signal, "SIGFPE"))
		return MERP_EXC_SIGFPE;

	if (!strcmp (signal, "SIGILL"))
		return MERP_EXC_SIGILL;

	if (!strcmp (signal, "SIGABRT"))
		return MERP_EXC_SIGABRT;

	// Force quit == hang?
	// We need a default for this
	if (!strcmp (signal, "SIGTERM"))
		return MERP_EXC_HANG;

	// FIXME: There are no other such signal
	// strings passed to mono_handle_native_crash at the
	// time of writing this
	g_error ("Merp doesn't know how to handle %s\n", signal);
}

static int merp_file_permissions = S_IWUSR | S_IRUSR | S_IRGRP | S_IROTH;

static gboolean
mono_merp_write_params (MERPStruct *merp)
{
	int handle = g_open (merp->merpFilePath, O_TRUNC | O_WRONLY | O_CREAT, merp_file_permissions);
	g_assertf (handle != -1, "Could not open MERP file at %s", merp->merpFilePath);

	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "ApplicationBundleId: %s\n", merp->bundleIDArg);
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "ApplicationVersion: %s\n", merp->versionArg);
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "ApplicationBitness: %s\n", get_merp_bitness (merp->archArg));

	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "ApplicationName: %s\n", merp->serviceNameArg);
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "ApplicationPath: %s\n", merp->servicePathArg);
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "BlameModuleName: %s\n", merp->moduleName);
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "BlameModuleVersion: %s\n", merp->moduleVersion);
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "BlameModuleOffset: 0x%llx\n", (unsigned long long)merp->moduleOffset);
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "ExceptionType: %s\n", get_merp_exctype (merp->exceptionArg));
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "StackChecksum: 0x%llx\n", merp->hashes.offset_free_hash);
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "StackHash: 0x%llx\n", merp->hashes.offset_rich_hash);

	// Provided by icall
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "OSVersion: %s\n", merp->osVersion);
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "LanguageID: 0x%x\n", merp->uiLidArg);
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "SystemManufacturer: %s\n", merp->systemManufacturer);
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "SystemModel: %s\n", merp->systemModel);
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "EventType: %s\n", merp->eventType);

	close (handle);
	return TRUE;
}

static gboolean
mono_merp_send (MERPStruct *merp)
{
	gboolean invoke_success = FALSE;

#if defined(HAVE_EXECV) && defined(HAVE_FORK)
	pid_t pid = (pid_t) fork ();

	// Only one we define on OSX
	if (pid == 0) {
		const char *open_path = "/usr/bin/open";
		const char *argvOpen[] = {open_path, "-a", config.merpGUIPath, NULL};
		execv (open_path, (char**)argvOpen);
		exit (-1);
	} else {
		int status;
		waitpid (pid, &status, 0);
		gboolean exit_success = FALSE;
		int exit_status = FALSE;

		while (TRUE) {
			if (waitpid(pid, &status, WUNTRACED | WCONTINUED) == -1)
				break;

			if (WIFEXITED(status)) {
				exit_status = WEXITSTATUS(status);
				exit_success = TRUE;
				invoke_success = exit_status == TRUE;
				break;
			} else if (WIFSIGNALED(status)) {
				break;
			}
		}
	}

	// // Create process to launch merp gui application
#endif

	return invoke_success;
}

static void
get_apple_model (char *buffer, size_t max_length) 
{
	size_t sz = 0;

	// Get the number of bytes to copy
	sysctlbyname("hw.model", NULL, &sz, NULL, 0);

	if (sz > max_length) {
		buffer[0] = '\0';
		return;
	}

	sysctlbyname("hw.model", buffer, &sz, NULL, 0);
}

static void
mono_init_merp (const intptr_t crashed_pid, const char *signal, MonoStackHash *hashes, MERPStruct *merp)
{
	g_assert (mono_merp_enabled ());

	merp->merpFilePath = config.merpFilePath;
	merp->crashLogPath = config.crashLogPath;
	merp->werXmlPath = config.werXmlPath;

	// If these aren't set, icall wasn't made
	// don't do merp? / don't set the variable to use merp;
	g_assert (config.appBundleID);
	g_assert (config.appVersion);
	merp->bundleIDArg = config.appSignature;
	merp->versionArg = config.appVersion;

	merp->archArg = get_merp_arch ();
	merp->exceptionArg = parse_exception_type (signal);

	merp->serviceNameArg = config.appBundleID;
	merp->servicePathArg = config.appPath;

	merp->moduleName = "Mono Exception";
	merp->moduleVersion = config.moduleVersion;

	merp->moduleOffset = 0;

	merp->uiLidArg = MONO_LOCALE_INVARIANT;

	merp->osVersion = os_version_string ();

	// FIXME: THis is apple-only for now
	merp->systemManufacturer = "apple";
	get_apple_model ((char *) merp->systemModel, sizeof (merp->systemModel));

	merp->eventType = config.eventType;

	merp->hashes = *hashes;
}

static gboolean
mono_merp_write_fingerprint_payload (const char *non_param_data, const MERPStruct *merp)
{
	int handle = g_open (merp->crashLogPath, O_TRUNC | O_WRONLY | O_CREAT, merp_file_permissions);
	g_assertf (handle != -1, "Could not open crash log file at %s", merp->crashLogPath);

	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "{\n");
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "\t\"payload\" : \n");
	g_write (handle, non_param_data, (guint32)strlen (non_param_data));	\
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, ",\n");

	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "\t\"parameters\" : \n{\n");
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "\t\t\"ApplicationBundleId\" : \"%s\",\n", merp->bundleIDArg);
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "\t\t\"ApplicationVersion\" : \"%s\",\n", merp->versionArg);
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "\t\t\"ApplicationBitness\" : \"%s\",\n", get_merp_bitness (merp->archArg));
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "\t\t\"ApplicationName\" : \"%s\",\n", merp->serviceNameArg);
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "\t\t\"BlameModuleName\" : \"%s\",\n", merp->moduleName);
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "\t\t\"BlameModuleVersion\" : \"%s\",\n", merp->moduleVersion);
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "\t\t\"BlameModuleOffset\" : \"0x%lx\",\n", merp->moduleOffset);
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "\t\t\"ExceptionType\" : \"%s\",\n", get_merp_exctype (merp->exceptionArg));
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "\t\t\"StackChecksum\" : \"0x%llx\",\n", merp->hashes.offset_free_hash);
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "\t\t\"StackHash\" : \"0x%llx\",\n", merp->hashes.offset_rich_hash);

	// Provided by icall
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "\t\t\"OSVersion\" : \"%s\",\n", merp->osVersion);
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "\t\t\"LanguageID\" : \"0x%x\",\n", merp->uiLidArg);
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "\t\t\"SystemManufacturer\" : \"%s\",\n", merp->systemManufacturer);
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "\t\t\"SystemModel\" : \"%s\",\n", merp->systemModel);
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "\t\t\"EventType\" : \"%s\"\n", merp->eventType);

	// End of parameters 
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "\t}\n");
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "}\n");

	// End of object
	close (handle);

	return TRUE;
}

static gboolean
mono_write_wer_template (MERPStruct *merp)
{
	// Note about missing ProcessInformation block: we have no PID that makes sense
	// and when mono is embedded and used to run functions without an entry point,
	// there is no image that would make any semantic sense to send either. 
	// It's a nuanced problem, each way we can run mono would need a separate fix.

	int handle = g_open (merp->werXmlPath, O_WRONLY | O_CREAT | O_TRUNC, merp_file_permissions);
	g_assertf (handle != -1, "Could not open WER XML file at %s", merp->werXmlPath);

	// Provided by icall
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "<?xml version=\"1.0\" encoding=\"UTF-16\"?>\n");
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "<WERReportMetadata>\n");
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "<ProblemSignatures>\n");
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "<EventType>%s</EventType>\n", merp->eventType);

	int i=0;
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "<Parameter%d>%s</Parameter%d>\n", i, merp->bundleIDArg, i);
	i++;
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "<Parameter%d>%s</Parameter%d>\n", i, merp->versionArg, i);
	i++;
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "<Parameter%d>%s</Parameter%d>\n", i, get_merp_bitness (merp->archArg), i);
	i++;
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "<Parameter%d>%s</Parameter%d>\n", i, merp->serviceNameArg, i);
	i++;
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "<Parameter%d>%s</Parameter%d>\n", i, merp->moduleName, i);
	i++;
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "<Parameter%d>%s</Parameter%d>\n", i, merp->moduleVersion, i);
	i++;
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "<Parameter%d>0x%zx</Parameter%d>\n", i, merp->moduleOffset, i);
	i++;
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "<Parameter%d>%s</Parameter%d>\n", i, get_merp_exctype (merp->exceptionArg), i);
	i++;
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "<Parameter%d>0x%llx</Parameter%d>\n", i, merp->hashes.offset_free_hash, i);
	i++;
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "<Parameter%d>0x%llx</Parameter%d>\n", i, merp->hashes.offset_rich_hash, i);
	i++;
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "<Parameter%d>%s</Parameter%d>\n", i, merp->osVersion, i);
	i++;
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "<Parameter%d>0x%x</Parameter%d>\n", i, merp->uiLidArg, i);
	i++;
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "<Parameter%d>%s</Parameter%d>\n", i, merp->systemManufacturer, i);
	i++;
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "<Parameter%d>%s</Parameter%d>\n", i, merp->systemModel, i);
	i++;

	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "</ProblemSignatures>\n");
	MOSTLY_ASYNC_SAFE_FPRINTF(handle, "</WERReportMetadata>\n");

	close (handle);

	return TRUE;
}

// Returns success
gboolean
mono_merp_invoke (const intptr_t crashed_pid, const char *signal, const char *non_param_data, MonoStackHash *hashes)
{
	MERPStruct merp;
	memset (&merp, 0, sizeof (merp));

	mono_summarize_timeline_phase_log (MonoSummaryMerpWriter);

	mono_init_merp (crashed_pid, signal, hashes, &merp);
	if (!mono_merp_write_params (&merp))
		return FALSE;

	if (!mono_merp_write_fingerprint_payload (non_param_data, &merp))
		return FALSE;

	if (!mono_write_wer_template (&merp))
		return FALSE;

	// Start program
	mono_summarize_timeline_phase_log (MonoSummaryMerpInvoke);
	gboolean success = mono_merp_send (&merp);

	if (success)
		mono_summarize_timeline_phase_log (MonoSummaryCleanup);

	return success;
}

void
mono_merp_disable (void)
{
	if (!config.enable_merp)
		return;

	g_free ((char*)config.appBundleID); // cast away const
	g_free ((char*)config.appSignature);
	g_free ((char*)config.appVersion);
	g_free ((char*)config.merpGUIPath);
	g_free ((char*)config.eventType);
	g_free ((char*)config.appPath); 
	g_free ((char*)config.moduleVersion);
	memset (&config, 0, sizeof (config));
}

void
mono_merp_enable (const char *appBundleID, const char *appSignature, const char *appVersion, const char *merpGUIPath, const char *eventType, const char *appPath, const char *configDir)
{
	g_assert (!config.enable_merp);

	char *prefix = NULL;

	if (!configDir) {
		const char *home = g_get_home_dir ();
		prefix = g_strdup_printf ("%s/Library/Group Containers/UBF8T346G9.ms/", home);
	} else {
		prefix = g_strdup (configDir);
	}
	config.merpFilePath = g_strdup_printf ("%s%s", prefix, "MERP.uploadparams.txt");
	config.crashLogPath = g_strdup_printf ("%s%s", prefix, "lastcrashlog.txt");
	config.werXmlPath = g_strdup_printf ("%s%s", prefix, "CustomLogsMetadata.xml");
	g_free (prefix);

	config.moduleVersion = mono_get_runtime_callbacks ()->get_runtime_build_info ();

	config.appBundleID = g_strdup (appBundleID);
	config.appSignature = g_strdup (appSignature);
	config.appVersion = g_strdup (appVersion);
	config.merpGUIPath = g_strdup (merpGUIPath);
	config.eventType = g_strdup (eventType);
	config.appPath = g_strdup (appPath);

	config.log = g_getenv ("MONO_MERP_VERBOSE") != NULL;

	config.enable_merp = TRUE;
}

gboolean
mono_merp_enabled (void)
{
	return config.enable_merp;
}

#endif // TARGET_OSX
