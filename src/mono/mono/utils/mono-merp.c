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

#ifdef TARGET_OSX
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
	const char *bundleIDArg; // App Bundle ID (required for bucketization)
	const char *versionArg; // App Version (required for bucketization)

	MerpArch archArg; // Arch, MacOS only, bails out if not found also required for bucketization. (required)
	MERPExcType exceptionArg; // Exception type (refer to merpcommon.h and mach/exception_types.h for more info (optional)

	const char *serviceNameArg; // This is the Bootstrap service name that MERP GUI will create to receive mach_task_self on a port created. Bails out if MERP GUI fails to receive mach_task_self from the crashed app. (Required for crash log generation)
	const char *servicePathArg;

	const char *moduleName;
	const char *moduleVersion;
	size_t moduleOffset;

	const char *osVersion; 
	int uiLidArg; // Application LCID 

	const char systemModel [100];
	const char *systemManufacturer;

	MonoStackHash hashes;
} MERPStruct;

typedef struct {
	gboolean enable_merp;

	const char *appBundleID;
	const char *appSignature; 
	const char *appVersion;
	const char *merpGUIPath; 
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
			// Exception type is optional
			return "";
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

	// FIXME: There are no other such signal
	// strings passed to mono_handle_native_crash at the
	// time of writing this
	g_error ("Merp doesn't know how to handle %s\n", signal);
}

static void
mono_encode_merp (GString *output, MERPStruct *merp)
{
	// Provided by icall
	g_string_append_printf (output, "ApplicationBundleID: %s\n", merp->bundleIDArg);
	g_string_append_printf (output, "ApplicationVersion: %s\n", merp->versionArg);

	g_string_append_printf (output, "ApplicationBitness: %s\n", get_merp_bitness (merp->archArg));

	// Provided by icall
	g_string_append_printf (output, "ApplicationName: %s\n", merp->serviceNameArg);

	// When embedded, sometimes this really doesn't make sense.
	// FIXME: On OSX, could figure this out for just VS4Mac.
	g_string_append_printf (output, "ApplicationPath: %s\n", merp->servicePathArg ? merp->servicePathArg : "missing");

	// Provided by icall
	g_string_append_printf (output, "BlameModuleName: %s\n", merp->moduleName);
	g_string_append_printf (output, "BlameModuleVersion: %s\n", merp->moduleVersion);
	g_string_append_printf (output, "BlameModuleOffset: 0x%x\n", merp->moduleOffset);

	g_string_append_printf (output, "ExceptionType: %s\n", get_merp_exctype (merp->exceptionArg));

	g_string_append_printf (output, "StackChecksum: 0x%x\n", merp->hashes.offset_free_hash);
	g_string_append_printf (output, "StackHash: 0x%x\n", merp->hashes.offset_rich_hash);

	// Provided by icall
	g_string_append_printf (output, "OSVersion: %s\n", merp->osVersion);
	g_string_append_printf (output, "LanguageID: 0x%x\n", merp->uiLidArg);
	g_string_append_printf (output, "SystemManufacturer: %s\n", merp->systemManufacturer);
	g_string_append_printf (output, "SystemModel: %s\n", merp->systemModel);
}

static void
write_file (const char *payload, const char *fileName)
{
	FILE *outfile = fopen (fileName, "w");
	if (!outfile)
		g_error ("Could not create file %s\n", fileName);
	fprintf (outfile, "%s\n", payload);
	fclose (outfile);
}

static void
connect_to_merp (const char *serviceName, mach_port_t *merp_port)
{
	// // Create process to launch merp gui application
	const char *argvOpen[] = {"/usr/bin/open", "-a", config.merpGUIPath, NULL};
	int status = posix_spawn(NULL, "/usr/bin/open", NULL, NULL, (char *const*)(argvOpen), NULL);

	// // FIXME error handling
	g_assert (status == 0);

}

static void
mono_merp_send (const char *merpFile, const char *crashLog)
{
	// Write struct to magic file location
	// This registers our mach service so we can connect
	// to the merp process
	const char *home = g_get_home_dir ();
	char *merpParamPath = g_strdup_printf ("%s/Library/Group Containers/UBF8T346G9.ms/MERP.uploadparams.txt", home);
	write_file (merpFile, merpParamPath);
	g_free (merpParamPath);

	char *crashLogPath = g_strdup_printf ("%s/Library/Group Containers/UBF8T346G9.ms/lastcrashlog.txt", home);
	write_file (crashLog, crashLogPath);
	g_free (crashLogPath);

	if (config.log) {
		if (merpFile != NULL)
			fprintf (stderr, "Crashing MERP File:\n####\n%s\n####\n", merpFile);
		if (crashLog != NULL)
			fprintf (stderr, "Crashing Dump File:\n####\n%s\n####\n", crashLog);
	}

	// // Create process to launch merp gui application
	const char *argvOpen[] = {"/usr/bin/open", "-a", config.merpGUIPath, NULL};
	int status = posix_spawn(NULL, "/usr/bin/open", NULL, NULL, (char *const*)(argvOpen), NULL);

	// // FIXME error handling
	if (status == 0)
		g_error ("Could not start merp\n");

	return;
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
mono_init_merp (const intptr_t crashed_pid, const char *signal, MonoStackHash *hashes, MERPStruct *merp, const char *version)
{
	g_assert (mono_merp_enabled ());

	// If these aren't set, icall wasn't made
	// don't do merp? / don't set the variable to use merp;
	g_assert (config.appBundleID);
	g_assert (config.appVersion);
	merp->bundleIDArg = config.appSignature;
	merp->versionArg = config.appVersion;

	merp->archArg = get_merp_arch ();
	merp->exceptionArg = parse_exception_type (signal);

	merp->serviceNameArg = config.appBundleID;

	// FIXME: Not really a posix way to associated a process with a single executable
	// path? Linux gets bogged down in /proc
	merp->servicePathArg = NULL;
	if (crashed_pid) {
		size_t servicePathSize = sizeof (gchar) * 1200;
		merp->servicePathArg = g_malloc0 (servicePathSize);
		int result = proc_pidpath (crashed_pid, (void *) merp->servicePathArg, 1200);
		if (result <= 0) {
			g_free ((void *) merp->servicePathArg);
			merp->servicePathArg = NULL;
		}
	}

	merp->moduleName = "Mono Exception";
	merp->moduleVersion = version;

	merp->moduleOffset = 0;

	ERROR_DECL (error);
	merp->uiLidArg = ves_icall_System_Threading_Thread_current_lcid (error);
	mono_error_assert_ok (error);

	merp->osVersion = os_version_string ();

	// FIXME: THis is apple-only for now
	merp->systemManufacturer = "apple";
	get_apple_model ((char *) merp->systemModel, sizeof (merp->systemModel));

	merp->hashes = *hashes;
}

void
mono_merp_invoke (const intptr_t crashed_pid, const char *signal, const char *dump_file, MonoStackHash *hashes, char *version)
{
	MERPStruct merp;
	memset (&merp, 0, sizeof (merp));
	mono_init_merp (crashed_pid, signal, hashes, &merp, version);

	GString *output = g_string_new ("");
	mono_encode_merp (output, &merp);

	mono_merp_send (output->str, dump_file);

	g_string_free (output, TRUE);
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
	memset (&config, 0, sizeof (config));
}

void
mono_merp_enable (const char *appBundleID, const char *appSignature, const char *appVersion, const char *merpGUIPath)
{
	g_assert (!config.enable_merp);

	config.appBundleID = g_strdup (appBundleID);
	config.appSignature = g_strdup (appSignature);
	config.appVersion = g_strdup (appVersion);
	config.merpGUIPath = g_strdup (merpGUIPath);

	config.log = g_getenv ("MONO_MERP_VERBOSE") != NULL;

	config.enable_merp = TRUE;
}

gboolean
mono_merp_enabled (void)
{
	return config.enable_merp;
}

#endif // TARGET_OSX
