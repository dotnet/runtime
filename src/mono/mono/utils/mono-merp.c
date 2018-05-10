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
	pid_t pidArg; // Process ID of crashed app (required for crash log generation)
	MerpArch archArg; // Arch, MacOS only, bails out if not found also required for bucketization. (required)
	uintptr_t capabilitiesArg; // App capabilities (optional) i.e. recover files, etc.
	uintptr_t threadArg; // Stack Pointer of crashing thread. (required for crash log generation) Used for identifying crashing thread on crawl back when generating crashreport.txt
	uintptr_t timeArg; // Total runtime (optional)

	MERPExcType exceptionArg; // Exception type (refer to merpcommon.h and mach/exception_types.h for more info (optional)

	const char *bundleIDArg; // App Bundle ID (required for bucketization)
	const char *signatureArg; // App Bundle Signature (required) 

	const char *versionArg; // App Version (required for bucketization)
	const char *devRegionArg; // App region (optional)
	const char *uiLidArg; // Application LCID aka Language ID (optional for bucketization)

	const char *serviceNameArg; // This is the Bootstrap service name that MERP GUI will create to receive mach_task_self on a port created. Bails out if MERP GUI fails to receive mach_task_self from the crashed app. (Required for crash log generation)

	const char *appLoggerName; // App loger name (optional)
	const char *appLogSessionUUID; // App Session ID (optional but very useful)
	gboolean isOfficeApplication; // Is office application (1 = True 0 = false). Needs to be 1 to continue. (Required by design)
	gboolean isObjCException; // Is objectiveC Exception. (optional for crash log generation)
	size_t memVirt; // Virtual memory (pagination) at the time of crash (optional)
	size_t memRes; // Physical memory allocated at the time of the crash (optional)
	const char *treSessionID; // Rules engine session ID (optional)
	const char *exceptionCodeArg; // Exception code (optional)
	const char *exceptionAddressArg; // Exception address (optional)
	gboolean isAppRegisteredWithMAU; // Was this app installed with Microsoft Autoupdate (1 = True 0 = false)
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

static void
append_merp_arch (GString *output, MerpArch arch)
{
	switch (arch) {
		case MerpArchx86_64:
			g_string_append_printf (output, "01000007\n");
			break;
		case MerpArchx86:
			g_string_append_printf (output, "00000007\n");
			break;
		case MerpArchPPC:
			g_string_append_printf (output, "00000012\n");
			break;
		case MerpArchPPC64:
			g_string_append_printf (output, "01000012\n");
			break;
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

static void
append_merp_exctype (GString *output, MERPExcType exc)
{
	switch (exc) {
		case MERP_EXC_FORCE_QUIT:
			g_string_append_printf (output, "10000000\n");
			break;
		case MERP_EXC_SIGSEGV:
			g_string_append_printf (output, "20000000\n");
			break;
		case MERP_EXC_SIGABRT:
			g_string_append_printf (output, "30000000\n");
			break;
		case MERP_EXC_SIGSYS:
			g_string_append_printf (output, "40000000\n");
			break;
		case MERP_EXC_SIGILL:
			g_string_append_printf (output, "50000000\n");
			break;
		case MERP_EXC_SIGBUS:
			g_string_append_printf (output, "60000000\n");
			break;
		case MERP_EXC_SIGFPE:
			g_string_append_printf (output, "70000000\n");
			break;
		case MERP_EXC_SIGTRAP:
			g_string_append_printf (output, "80000000\n");
			break;
		case MERP_EXC_SIGKILL:
			g_string_append_printf (output, "90000000\n");
			break;
		case MERP_EXC_HANG: 
			g_string_append_printf (output, "02000000\n");
			break;
		case MERP_EXC_NONE:
			// Exception type is optional
			g_string_append_printf (output, "\n");
			break;
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
print_string_or_blank (GString *output, const char *maybeString)
{
	if (maybeString)
		g_string_append_printf (output, "%s", maybeString);
	g_string_append_printf (output, "\n");
}

static void
print_pointer_param (GString *output, intptr_t ptr)
{
	// The format is arch-specific
#if defined(TARGET_AMD64)
	g_string_append_printf (output, "%016llx\n", ptr);
#elif defined(TARGET_X86)
	g_string_append_printf (output, "%08x\n", ptr);
#endif
}

static void
print_memory_fraction (GString *output, size_t mem_bytes)
{
	if (!mem_bytes) {
		g_string_append_printf (output, "\n");
		return;
	}

	mem_bytes >>= 10; // convert from bytes to kb
	float frac = (float) mem_bytes / 1024.0; // kb to mb

	g_string_append_printf (output, "%f\n", frac);
}

static void
mono_encode_merp (GString *output, MERPStruct *merp)
{
	// Format of integers seems to be 8 digits with padding
	g_assert (merp->pidArg);
	g_string_append_printf (output, "%.8x\n", merp->pidArg);

	g_assert (merp->archArg);
	append_merp_arch (output, merp->archArg);

	g_string_append_printf (output, "%.8x\n", merp->capabilitiesArg);
	print_pointer_param (output, merp->threadArg);
	print_pointer_param (output, merp->timeArg);

	append_merp_exctype (output, merp->exceptionArg);

	g_assert (merp->bundleIDArg);
	g_assert (merp->signatureArg);
	g_assert (merp->versionArg);
	g_string_append_printf (output, "%s\n%s\n%s\n", merp->bundleIDArg, merp->signatureArg, merp->versionArg);

	print_string_or_blank (output, merp->devRegionArg);
	print_string_or_blank (output, merp->uiLidArg);

	g_assert (merp->serviceNameArg);
	g_string_append_printf (output, "%s\n", merp->serviceNameArg);

	print_string_or_blank (output, merp->appLoggerName);
	print_string_or_blank (output, merp->appLogSessionUUID);

	g_string_append_printf (output, "%d\n", merp->isOfficeApplication ? 1 : 0);

	print_string_or_blank (output, merp->isObjCException ? "1" : NULL);

	print_memory_fraction (output, merp->memRes);
	print_memory_fraction (output, merp->memVirt);

	print_string_or_blank (output, merp->treSessionID);
	print_string_or_blank (output, merp->exceptionCodeArg);
	print_string_or_blank (output, merp->exceptionAddressArg);
	print_string_or_blank (output, merp->isAppRegisteredWithMAU ? "1" : "0");
}

// Darwin-only for now
static void
mono_arch_memory_info (size_t *resOut, size_t *vmOut)
{
	struct task_basic_info t_info;
	memset (&t_info, 0, sizeof (t_info));
	mach_msg_type_number_t t_info_count = TASK_BASIC_INFO_COUNT;
	task_name_t task = mach_task_self ();

	task_info(task, TASK_BASIC_INFO, (task_info_t) &t_info, &t_info_count);

	*resOut = (size_t) t_info.resident_size;
	*vmOut = (size_t) t_info.virtual_size;
}

static void
write_file (GString *str, const char *fileName)
{
	FILE *outfile = fopen (fileName, "w");
	if (!outfile)
		g_error ("Could not create file %s\n", fileName);
	fwrite (str->str, sizeof (gchar), str->len, outfile);
	fclose (outfile);
}

/*
 * This struct is the wire protocol between MERP
 * and mono
 */

typedef struct {
	mach_msg_header_t head;

	/* start of the merp-specific data */
	mach_msg_body_t msgh_body;
	mach_msg_port_descriptor_t task;
	/* end of the merp-specific data */

} MerpRequest;

static void
send_mach_message (mach_port_t *mach_port)
{
	task_name_t task = mach_task_self ();

	// Setup request
	MerpRequest req;
	memset (&req, 0, sizeof (req));
	req.head.msgh_bits = MACH_MSGH_BITS_COMPLEX | MACH_MSGH_BITS(19, 0);
	req.task.name = task;
	req.task.disposition = 19;
	req.task.type = MACH_MSG_PORT_DESCRIPTOR;

	/* msgh_size passed as argument */
	req.head.msgh_remote_port = *mach_port;
	req.head.msgh_local_port = MACH_PORT_NULL;
	req.head.msgh_id = 400;

	req.msgh_body.msgh_descriptor_count = 1;

	// Send port to merp GUI
	if (config.log)
		fprintf (stderr, "Sending message to MERP\n");
	mach_msg_return_t res = mach_msg (&req.head, MACH_SEND_MSG|MACH_MSG_OPTION_NONE, (mach_msg_size_t)sizeof(MerpRequest), 0, MACH_PORT_NULL, MACH_MSG_TIMEOUT_NONE, MACH_PORT_NULL);
	g_assert (res == KERN_SUCCESS);
	if (config.log)
		fprintf (stderr, "Successfully sent message to MERP\n");
}

static void
connect_to_merp (const char *serviceName, mach_port_t *merp_port)
{
	// // Create process to launch merp gui application
	const char *argvOpen[] = {"/usr/bin/open", "-a", config.merpGUIPath, NULL};
	int status = posix_spawn(NULL, "/usr/bin/open", NULL, NULL, (char *const*)(argvOpen), NULL);

	// // FIXME error handling
	g_assert (status == 0);

	// Register our service name with this task
	// BOOTSTRAP_UNKNOWN_SERVICE is returned while the service doesn't exist.
	// We rely on MERP to make the service with serviceName for us
	kern_return_t kernErr = bootstrap_look_up(bootstrap_port, serviceName, merp_port);
	while (TRUE) {
		for (int i = 0; BOOTSTRAP_UNKNOWN_SERVICE == kernErr && i < 5000; i++)
			kernErr = bootstrap_look_up(bootstrap_port, serviceName, merp_port);

		if (kernErr != BOOTSTRAP_UNKNOWN_SERVICE)
			break;

		if (config.log)
			fprintf (stderr, "Merp: Service not registered with name %s, resetting counter after 10s sleep\n", serviceName);
		sleep(10);
	}

	/*// FIXME error handling*/
	g_assert (KERN_SUCCESS == kernErr);
}

static void
mono_merp_send (GString *str, const char *serviceName)
{
	// Write struct to magic file location
	// This registers our mach service so we can connect
	// to the merp process
	const char *home = g_get_home_dir ();
	char *merpParamPath = g_strdup_printf ("%s/Library/Group Containers/UBF8T346G9.ms/MERP.Params.txt", home);
	write_file (str, merpParamPath);
	g_free (merpParamPath);

	mach_port_t merpPort = MACH_PORT_NULL;

	// Start merpGui application
	// Assign to merpPort the port that merpGui opens
	// Connecting to the service with name serviceName 
	// that merpGUI starts
	connect_to_merp (serviceName, &merpPort);

	// Send this mach task to MERP over the port
	send_mach_message (&merpPort);

	// After we resume from the suspend, we are done
	// We're spinning down the process shortly.
	// This thread exits, and the crashing thread's
	// waitpid on this thread ends.
	return;
}

static void
mono_init_merp (const char *serviceName, const char *signal, pid_t crashed_pid, intptr_t thread_pointer, MERPStruct *merp)
{
	g_assert (mono_merp_enabled ());

	merp->pidArg = crashed_pid;
	merp->archArg = get_merp_arch ();
	merp->capabilitiesArg = 0x0;

	merp->threadArg = thread_pointer;

	// FIXME: time the runtime?
	merp->timeArg = 0x0;

	merp->exceptionArg = parse_exception_type (signal);

	// If these aren't set, icall wasn't made
	// don't do merp? / don't set the variable to use merp;
	merp->bundleIDArg = config.appBundleID;
	merp->signatureArg = config.appSignature;
	merp->versionArg = config.appVersion;

	// FIXME: Do we want these?
	merp->devRegionArg = NULL;
	merp->uiLidArg = NULL;

	merp->serviceNameArg = serviceName;

	// FIXME: Do we want these?
	merp->appLoggerName = NULL;
	merp->appLogSessionUUID = NULL;

	// Should be set for our usage as per document
	merp->isOfficeApplication = TRUE;
	merp->isObjCException = FALSE;

	mono_arch_memory_info (&merp->memRes, &merp->memVirt);

	// No sessions right now
	merp->treSessionID = NULL;
	merp->exceptionCodeArg = NULL;
	merp->exceptionAddressArg = NULL;

	// Not certain? Maybe expose to config options
	merp->isAppRegisteredWithMAU = TRUE;
}

void
mono_merp_invoke (pid_t crashed_pid, intptr_t thread_pointer, const char *signal, const char *dump_file)
{
	if (dump_file != NULL)
		fprintf (stderr, "Crashing Dump File:\n####\n%s\n####\n", dump_file);

	// This unique service name is used to communicate with merp over mach service ports
	char *serviceName = g_strdup_printf ("com.mono.merp.%.8x", crashed_pid);

	MERPStruct merp;
	memset (&merp, 0, sizeof (merp));
	mono_init_merp (serviceName, signal, crashed_pid, thread_pointer, &merp);

	GString *output = g_string_new ("");
	mono_encode_merp (output, &merp);

	if (config.log)
		fprintf (stderr, "Results: \n(%s)\n", output->str);

	// We send the merp over the port
	mono_merp_send (output, serviceName);

	g_string_free (output, TRUE);
	g_free (serviceName);
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
