#include "mini.h"
#include "mini-runtime.h"

MONO_API int coreclr_initialize (const char* exePath, const char* appDomainFriendlyName,
	int propertyCount, const char** propertyKeys, const char** propertyValues,
	void** hostHandle, unsigned int* domainId);

MONO_API int coreclr_execute_assembly (void* hostHandle, unsigned int domainId,
	int argc, const char** argv,
	const char* managedAssemblyPath, unsigned int* exitCode);

MONO_API int coreclr_shutdown_2 (void* hostHandle, unsigned int domainId, int* latchedExitCode);

//
// Initialize the CoreCLR. Creates and starts CoreCLR host and creates an app domain
//
// Parameters:
//  exePath                 - Absolute path of the executable that invoked the ExecuteAssembly
//  appDomainFriendlyName   - Friendly name of the app domain that will be created to execute the assembly
//  propertyCount           - Number of properties (elements of the following two arguments)
//  propertyKeys            - Keys of properties of the app domain
//  propertyValues          - Values of properties of the app domain
//  hostHandle              - Output parameter, handle of the created host
//  domainId                - Output parameter, id of the created app domain 
//
// Returns:
//  HRESULT indicating status of the operation. S_OK if the assembly was successfully executed
//
int coreclr_initialize (const char* exePath, const char* appDomainFriendlyName,
	int propertyCount, const char** propertyKeys, const char** propertyValues,
	void** hostHandle, unsigned int* domainId)
{
	// TODO: TRUSTED_PLATFORM_ASSEMBLIES is the property key for managed assemblies mapping
	return 0;
}


//
// Execute a managed assembly with given arguments
//
// Parameters:
//  hostHandle              - Handle of the host
//  domainId                - Id of the domain 
//  argc                    - Number of arguments passed to the executed assembly
//  argv                    - Array of arguments passed to the executed assembly
//  managedAssemblyPath     - Path of the managed assembly to execute (or NULL if using a custom entrypoint).
//  exitCode                - Exit code returned by the executed assembly
//
// Returns:
//  HRESULT indicating status of the operation. S_OK if the assembly was successfully executed
//
int coreclr_execute_assembly (void* hostHandle, unsigned int domainId,
	int argc, const char** argv,
	const char* managedAssemblyPath, unsigned int* exitCode)
{
	if (exitCode == NULL)
	{
		return -1;
	}

	//
	// Make room for program name and executable assembly
	//
	int mono_argc = argc + 2;

	char **mono_argv = (char **) malloc (sizeof (char *) * (mono_argc + 1 /* null terminated */));
	const char **ptr = (const char **) mono_argv;
	
	*ptr++ = NULL;

	// executable assembly
	*ptr++ = (char*) managedAssemblyPath;

	// the rest
	for (int i = 0; i < argc; ++i)
		*ptr++ = argv [i];

	*ptr = NULL;

	*exitCode = mono_main (mono_argc, mono_argv);

	return 0;
}

//
// Shutdown CoreCLR. It unloads the app domain and stops the CoreCLR host.
//
// Parameters:
//  hostHandle              - Handle of the host
//  domainId                - Id of the domain 
//
// Returns:
//  HRESULT indicating status of the operation. S_OK if the assembly was successfully executed
//
int coreclr_shutdown_2 (void* hostHandle, unsigned int domainId, int* latchedExitCode)
{
	return 0;
}
