#include <config.h>
#include <mono/utils/mono-compiler.h>

#if ENABLE_NETCORE

#include "mini.h"
#include "mini-runtime.h"
#include <mono/metadata/assembly.h>
#include <mono/metadata/assembly-internals.h>
#include <mono/metadata/environment.h>
#include <mono/metadata/loader-internals.h>
#include <mono/mini/monovm.h>
#include <mono/utils/mono-logger-internals.h>

#ifndef STDAPICALLTYPE
#define STDAPICALLTYPE
#endif

#if defined(_MSC_VER) && defined(HOST_WIN32) && defined(HOST_X86)
// Ensure that the exported symbols are not decorated and that only one set is exported
#pragma comment(linker, "/export:coreclr_initialize=_coreclr_initialize@28")
#pragma comment(linker, "/export:coreclr_execute_assembly=_coreclr_execute_assembly@24")
#pragma comment(linker, "/export:coreclr_shutdown_2=_coreclr_shutdown_2@12")
#pragma comment(linker, "/export:coreclr_create_delegate=_coreclr_create_delegate@24")
#undef MONO_API
#define MONO_API MONO_EXTERN_C
#endif

MONO_API int STDAPICALLTYPE coreclr_initialize (const char* exePath, const char* appDomainFriendlyName,
	int propertyCount, const char** propertyKeys, const char** propertyValues,
	void** hostHandle, unsigned int* domainId);

MONO_API int STDAPICALLTYPE coreclr_execute_assembly (void* hostHandle, unsigned int domainId,
	int argc, const char** argv,
	const char* managedAssemblyPath, unsigned int* exitCode);

MONO_API int STDAPICALLTYPE coreclr_shutdown_2 (void* hostHandle, unsigned int domainId, int* latchedExitCode);

MONO_API int STDAPICALLTYPE coreclr_create_delegate (void* hostHandle, unsigned int domainId,
	const char* entryPointAssemblyName, const char* entryPointTypeName, const char* entryPointMethodName,
	void** delegate);

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
int STDAPICALLTYPE coreclr_initialize (const char* exePath, const char* appDomainFriendlyName,
	int propertyCount, const char** propertyKeys, const char** propertyValues,
	void** hostHandle, unsigned int* domainId)
{
	return monovm_initialize (propertyCount, propertyKeys, propertyValues);
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
int STDAPICALLTYPE coreclr_execute_assembly (void* hostHandle, unsigned int domainId,
	int argc, const char** argv,
	const char* managedAssemblyPath, unsigned int* exitCode)
{
	return monovm_execute_assembly (argc, argv, managedAssemblyPath, exitCode);
}

//
// Shutdown CoreCLR. It unloads the app domain and stops the CoreCLR host.
//
// Parameters:
//  hostHandle              - Handle of the host
//  domainId                - Id of the domain 
//  latchedExitCode         - Latched exit code after domain unloaded
//
// Returns:
//  HRESULT indicating status of the operation. S_OK if the assembly was successfully executed
//
int STDAPICALLTYPE coreclr_shutdown_2 (void* hostHandle, unsigned int domainId, int* latchedExitCode)
{
	return monovm_shutdown (latchedExitCode);
}

//
// Create a native callable delegate for a managed method.
//
// Parameters:
//  hostHandle              - Handle of the host
//  domainId                - Id of the domain 
//  entryPointAssemblyName  - Name of the assembly which holds the custom entry point
//  entryPointTypeName      - Name of the type which holds the custom entry point
//  entryPointMethodName    - Name of the method which is the custom entry point
//  delegate                - Output parameter, the function stores a pointer to the delegate at the specified address
//
// Returns:
//  HRESULT indicating status of the operation. S_OK if the assembly was successfully executed
//
int STDAPICALLTYPE coreclr_create_delegate (void* hostHandle, unsigned int domainId,
	const char* entryPointAssemblyName, const char* entryPointTypeName, const char* entryPointMethodName,
	void** delegate)
{
	g_error ("Not implemented");
	return 0;
}
#else

MONO_EMPTY_SOURCE_FILE (main_core);
#endif // ENABLE_NETCORE
