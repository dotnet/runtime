/*
 * monod.c: Mono daemon for running services based
 *          on System.ServiceProcess.
 *
 * Author:
 *   Joerg Rosenkranz (joergr@voelcker.com)
 *
 * (C) 2005 Voelcker Informatik AG
 */


#include <mono/mini/jit.h>
#include <mono/metadata/environment.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/debug-helpers.h>

#include <mono/io-layer/uglify.h>
#include <mono/io-layer/wait.h>
#include <mono/io-layer/events.h>

#include <locale.h>
#include <stdio.h>
#include <fcntl.h>
#include <signal.h>
#include <unistd.h>
#include <syslog.h>

/* #define DEBUG_OUTPUT */

#define DEFAULT_LOCK_PREFIX    "/tmp/"
#define DEFAULT_LOCK_EXTENSION ".lock"

/* Exit codes */
#define EXIT_SUCCESS                  0
#define EXIT_USAGE                    1
#define EXIT_COULDNOTLOADASSEMBLY     2
#define EXIT_COULDNOTGETCLASS         3
#define EXIT_COULDNOTCREATEOBJECT     4
#define EXIT_COULDNOTCREATEMETHODDESC 5
#define EXIT_COULDNOTGETMETHOD        6
#define EXIT_FORKERROR                7
#define EXIT_CANNOTOPENLOCK           8
#define EXIT_NOSVCOBJECT              9

#define EXIT_NOTIMPLEMENTED        1024

typedef struct 
{
	MonoDomain *domain;
	const char *file;
	const char *servicename;
} MainThreadArgs;

/*
 * Global variables.
 */
gpointer		svc_sig_event;
int				svc_sig;

MonoObject* get_svc_object(const char* name)
{
	MonoImage *image;
	MonoArray *svc_array;
	MonoClass *svcbase_class;
	MonoClassField *field;
	
	/*
	 * Get array of registered services from static member of ServiceBase.
	 * This property is only set when ServiceBase.Run was called in the 
	 * service's Main.
	 *
	 * System.ServiceProcess should already be loaded.
	 */
	image = mono_image_loaded ("System.ServiceProcess");
	if (!image)  {
		syslog (LOG_ERR, "Could not get image System.ServiceProcess.\n");
		return NULL;
	}

	svcbase_class = mono_class_from_name (image, "System.ServiceProcess", "ServiceBase");
	if (!svcbase_class) {
		syslog (LOG_ERR, "Could not get class System.ServiceProcess.ServiceBase.\n");
		return NULL;
	}

	field = mono_class_get_field_from_name (svcbase_class, "RegisteredServices");
	if (!field) {
		syslog (LOG_ERR, "Could not get field ServiceBase.RegisteredServices.\n");
		return NULL;
	}

	svc_array = (MonoArray*) mono_field_get_value_object (mono_domain_get (), field, NULL);
	if (!svc_array || mono_array_length (svc_array) == 0) {
		syslog (LOG_ERR, "No service object registered by Main.\n");
		return NULL;
	}

	if (name && *name) {
		/*
		 * There was a name provided
		 * -> Search for the right service object
		 */
		
		syslog (LOG_ERR, "Searching service object by name not yet implemented.");
		exit (EXIT_NOTIMPLEMENTED);
	} 

	/* Default: Return the first service */
	return mono_array_get (svc_array, MonoObject*, 0);
}

void invoke_service_method (MonoClass *svc_class, MonoObject *svc_object, const char *name)
{
	void * params [] = {NULL};
	MonoMethodDesc 	*method_desc;
	MonoMethod		*method;

	if (svc_class && svc_object) {

		/* Get method from class */
		method_desc = mono_method_desc_new (name, FALSE);
		if (! method_desc) {
			syslog (LOG_ERR, "Could not create method description for %s.\n", name);
			exit (EXIT_COULDNOTCREATEMETHODDESC);
		}
		
		method = mono_method_desc_search_in_class (method_desc, svc_class);
		if (!method) {
			syslog (LOG_ERR, "Could not get method %s.\n", name);
			exit (EXIT_COULDNOTGETMETHOD);
		}
		
		mono_method_desc_free (method_desc);
			
		/* Run method. */
		mono_runtime_invoke (method, svc_object, params, NULL);
	}
}

MonoObject* get_service_property (MonoClass *svc_class, MonoObject *svc_object, const char *name)
{
	MonoProperty *prop;
	
	if (!svc_class || !svc_object) {
		return NULL;
	}
	
	prop = mono_class_get_property_from_name (svc_class, name);
	if (!prop) {
		return NULL;
	}
	
	return mono_property_get_value (prop, svc_object, NULL, NULL);
}

static void service_main (gpointer user_data)
{
	MainThreadArgs 	*main_args = (MainThreadArgs *) user_data;
	MonoAssembly 	*assembly;
	MonoClass 		*svc_class;
	MonoObject		*svc_object;
	gboolean		bEnd;
	const char*		servicedisplay = main_args->file;
	MonoString		*s;
	char**	args;
	
	/*
	 * Get service assembly.
	 */
	assembly = mono_domain_assembly_open (main_args->domain,
					      main_args->file);
	if (!assembly) {
		syslog (LOG_ERR, "Could not load assembly %s.\n", main_args->file);
		exit (EXIT_COULDNOTLOADASSEMBLY);
	}
	
	/* 
	 * Run Main to get list of services.
	 */
	args = (char**) malloc (2 * sizeof(char*));
	args[0] = (char*) main_args->file;
	args[1] = NULL;
	mono_jit_exec (main_args->domain, assembly, 1, args);
	
	/*
	 * Get service object
	 */
	svc_object = get_svc_object (main_args->servicename);
	if (!svc_object) {
		syslog (LOG_ERR, "Could not get service object. Maybe not registered in Main.");
		exit (EXIT_NOSVCOBJECT);
	}
	
	svc_class = mono_object_get_class (svc_object);
	if (!svc_class) {
		syslog (LOG_ERR, "Could not get class from object.\n");
		exit (EXIT_COULDNOTGETCLASS);
	}

	s = (MonoString*) get_service_property (svc_class, svc_object, "ServiceName");
	if (s) {
		servicedisplay = mono_string_to_utf8 (s);
	}
	
	syslog (LOG_INFO, "Starting service %s...\n", servicedisplay);
	invoke_service_method (svc_class, svc_object, "ServiceBase:OnStart");
	
	bEnd = FALSE;
	while (!bEnd) {
		if (WaitForSingleObject (svc_sig_event, INFINITE) == WAIT_FAILED) {
			syslog (LOG_ERR, "Waiting for handle failed.\n");
			bEnd = TRUE;
		} else {
			if (svc_sig != 0) {
				switch (svc_sig) {
					case SIGTERM:
						syslog (LOG_INFO, "Stopping service %s...\n", servicedisplay);
						invoke_service_method (svc_class, svc_object, "ServiceBase:OnStop");
						bEnd = TRUE;
						break;
					
					case SIGUSR1:
						syslog (LOG_INFO, "Pausing service %s...\n", servicedisplay);
						invoke_service_method (svc_class, svc_object, "ServiceBase:OnPause");
						break;
					
					case SIGUSR2:
						syslog (LOG_INFO, "Continuing service %s...\n", servicedisplay);
						invoke_service_method (svc_class, svc_object, "ServiceBase:OnContinue");
						break;
				}
				
				svc_sig = 0;
			}
		}
	}
	
	CloseHandle (svc_sig_event);
}

void signal_handler (int sig)
{
	svc_sig = sig;
	
	/* Signal the wait event of our main loop */
	SetEvent (svc_sig_event);
}

void run_service (const char *file, const char *svcname)
{
	MonoDomain *domain;
	MainThreadArgs main_args;
	
	svc_sig = 0;
	
	/* Create AutoResetEvent to wait for signals in main loop */
	svc_sig_event = CreateEvent (NULL, FALSE, FALSE, NULL);
	
	/* Install signal handler control events */
	signal(SIGTERM, signal_handler);
	signal(SIGUSR1, signal_handler);
	signal(SIGUSR2, signal_handler);
	
	/*
	 * mono_jit_init() creates a domain: each assembly is
	 * loaded and run in a MonoDomain.
	 */
	domain = mono_jit_init (file);

	/* Parse default config files */
	mono_config_parse (NULL);
	
	main_args.domain = domain;
	main_args.file = file;
	main_args.servicename = svcname;
	
	mono_runtime_exec_managed_code (domain, service_main, &main_args);
	
	mono_jit_cleanup (domain);
}

void daemonize (char * lockfile, char * rundirectory, char * logname)
{
	int i, lfp;
	char str [10];
	
	/* fork to create daemon process */
	i = fork ();
	
	if (i<0) 
		exit (EXIT_FORKERROR); /* fork error */
	
	if (i>0) 
		exit (EXIT_SUCCESS); /* parent exits */
	
	/* child (daemon) continues */
	setsid (); /* obtain a new process group */
	
#ifndef DEBUG_OUTPUT
	/* suppress stdout and stderr */
	for (i = getdtablesize(); i >= 0 ; --i) 
		close (i); /* close all descriptors */
	
	i = open ("/dev/null", O_RDWR); 
	dup (i); 
	dup (i); /* handle standard I/O */
#endif
	
	/* open syslog handle */
	openlog (logname, LOG_PID, LOG_DAEMON);
	
	if (rundirectory && *rundirectory) {
		syslog (LOG_INFO, "Running in directory %s", rundirectory);
		chdir (rundirectory); /* change running directory */
	}
	
	/* create and lock lock file */
	lfp = open (lockfile, O_RDWR|O_CREAT, 0640);
	
	if (lfp<0)  {
		syslog (LOG_ERR, "Cannot open lock file.\n");
		exit (EXIT_CANNOTOPENLOCK); /* can not open */
	}
	
	if (lockf(lfp, F_TLOCK,0)<0)  {
		syslog (LOG_ERR, "Daemon is already running.\n");
		exit (EXIT_SUCCESS); /* can not lock */
	}
	
	/* first instance continues */
	sprintf (str,"%d\n", getpid());
	write(lfp, str, strlen (str)); /* record pid to lockfile */
	
	signal (SIGCHLD, SIG_IGN); /* ignore child */
	signal (SIGTSTP, SIG_IGN); /* ignore tty signals */
	signal (SIGTTOU, SIG_IGN);
	signal (SIGTTIN, SIG_IGN);
}

int main (int argc, char* argv[]) 
{
	char * 	lockfile;
	char * 	directory;
	char *	name;
	char *	assembly;
	char *	logname;
	gboolean		bShowUsage;
	int				i;
	char *	param;
	
	setlocale (LC_ALL, "");
	g_log_set_always_fatal (G_LOG_LEVEL_ERROR);
	g_log_set_fatal_mask (G_LOG_DOMAIN, G_LOG_LEVEL_ERROR);
	
	/* set default values */
	lockfile = NULL;
	directory = NULL;
	name = NULL;
	logname = NULL;
	assembly = NULL;
	bShowUsage = FALSE;
	
	/* parse command line arguments */
	for (i = 1; !bShowUsage && i < argc - 1; i++) {
		param = argv [i];
		
		if (strlen (param) < 3 || param [0] != '-' || param [2] != ':') {
			bShowUsage = TRUE;
		}
		else {
			switch (param [1]) {
				case 'd':
				case 'D':
					directory = &param [3];
					break;
				
				case 'l':
				case 'L':
					lockfile = &param [3];
					break;
				
				case 'n':
				case 'N':
					name = &param [3];
					break;
				
				case 'm':
				case 'M':
					logname = &param [3];
					break;
				
				default:
					bShowUsage = TRUE;
			}
		}
	}
	
	if (argc > 1 && *argv [argc - 1] != '-') {
		assembly = argv [argc - 1];
	} else {
		fprintf (stderr, "Assembly name is missing.\n");
		bShowUsage = TRUE;
	}
	
	if (bShowUsage) {
		fprintf (stdout,
			"Usage is: monod [options] service\n"
			"\n"
			"    -d:<directory>         Working directory\n"
			"    -l:<lock file>         Lock file (default is /tmp/<service>.log)\n"
			"    -m:<syslog name>       Name to show in syslog\n"
			"    -n:<service name>      Name of service to start (default is first defined)\n"
			"\n"
			"Controlling the service:\n"
			"\n"
			"    kill -USR1 `cat <lock file>`    Pausing service\n"
			"    kill -USR2 `cat <lock file>`    Continuing service\n"
			"    kill `cat <lock file>`          Ending service\n"
			"\n");
		return EXIT_USAGE;
	}
	
	if ( !lockfile ) {
		/* Build default lock file name */
		lockfile = (char*) malloc (strlen (DEFAULT_LOCK_PREFIX) + strlen (assembly) + strlen (DEFAULT_LOCK_EXTENSION));
		strcpy (lockfile, DEFAULT_LOCK_PREFIX);
		strcat (lockfile, assembly);
		strcat (lockfile, DEFAULT_LOCK_EXTENSION);
	}
	
	if ( !logname ) {
		logname = assembly;
	}
	
	/* Run as daemon */
	daemonize (lockfile, directory, logname);
	
	/* Start the service */
	run_service (assembly, name);
	
	return EXIT_SUCCESS;
}

