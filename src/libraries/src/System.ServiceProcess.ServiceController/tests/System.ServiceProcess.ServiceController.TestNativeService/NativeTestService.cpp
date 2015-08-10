// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// This is a simple Win32 service which does nothing except respond to control
// events by settings its status appropriately. It takes its own service and
// service display name as parameters to allow many tests to spin up their own
// instances of this service in parallel.
//
// Because this will eventually run on OneCore on systems which may not have
// sc.exe, this also provides a means to create, start, stop, and delete this
// service via the command line:
//
//   - When run like this:
//
//       System.ServiceProcess.ServiceController.TestNativeService.exe "TestService" "Test Service" create
//
//     This creates a service named "TestService" with display name
//     "Test Service", and starts the service. This also creates additional
//     instances of this service which depend on "TestService" for the purposes
//     of testing.
//
//   - When run like this:
//
//       System.ServiceProcess.ServiceController.TestNativeService.exe "TestService" "Test Service" delete
//
//     This attempts to stop "TestService" and delete it and its dependent
//     services.
//
//   - When run like this:
//
//       System.ServiceProcess.ServiceController.TestNativeService.exe "TestService" "Test Service"
//
//     This executable assumes it is being run as a service, and simply waits
//     for and responds to control events.
//

#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif

#include <windows.h>
#include <stdio.h>
#include <tchar.h>
#include <wchar.h>
#include <string>

#define DEPENDENT_SERVICES 3

// The path to this executable
TCHAR                   gModulePath[MAX_PATH];

// Log file handle
HANDLE                  ghLogFile;
std::wstring            gLogFilePath;

// Main Test Service State
SERVICE_STATUS          gServiceStatus;
SERVICE_STATUS_HANDLE   gServiceStatusHandle;
HANDLE                  ghServiceStopEvent;
LPTSTR                  gServiceName;
LPTSTR                  gServiceDisplayName;

// Dependent Service State
std::wstring            gDependentServiceNames[DEPENDENT_SERVICES];
std::wstring            gDependentServiceDisplayNames[DEPENDENT_SERVICES];

// Service Management Methods
VOID GenerateDependentServiceNames();
BOOL CreateTestServices();
SC_HANDLE CreateTestService(SC_HANDLE, LPCTSTR, LPCTSTR, LPCTSTR, int, LPCTSTR dependencies = NULL);
BOOL DeleteTestServices();
BOOL DeleteTestService(SC_HANDLE, LPCTSTR);

// Service Methods
VOID WINAPI ServiceMain(DWORD, LPTSTR*);
VOID WINAPI ServiceCtrlHandler(DWORD);
VOID ServiceReportStatus(DWORD, DWORD, DWORD);
VOID ServiceInit(DWORD, LPTSTR*);
BOOL InitModulePath();
VOID CreateLogFile();
DWORD DeleteLogFile();
VOID LogMessage(LPCTSTR format, ...);

int _tmain(int argc, _TCHAR* argv [])
{
	if (argc < 3 || argc > 4)
	{
		puts("usage: System.ServiceProcess.ServiceController.TestNativeService.exe <ServiceName> <DisplayName> [create|delete]");
		return 1;
	}

	gServiceName = argv[1];
	gServiceDisplayName = argv[2];

	if (argc == 3)
	{
		// When run with just a service name, just run as a service
		SERVICE_TABLE_ENTRY DispatchTable [] =
		{
			{ gServiceName, (LPSERVICE_MAIN_FUNCTION) ServiceMain },
			{ NULL, NULL }
		};

		// This call returns when the service has stopped. 
		// The process should simply terminate when the call returns.
		if (!StartServiceCtrlDispatcher(DispatchTable))
		{
			LogMessage(L"error: StartServiceCtrlDispatcher failed (%d)\n", GetLastError());
		}
	}
	else if (argc == 4)
	{
		if (!InitModulePath())
		{
			return -1;
		}

		GenerateDependentServiceNames();

		std::wstring action = argv[3];
		if (action == L"create")
		{
			if (!CreateTestServices())
			{
				wprintf(L"error: Creating the test services failed\n");
				DeleteTestServices();
				return -1;
			}
		}
		else if (action == L"delete")
		{
			if (!DeleteTestServices())
			{
				wprintf(L"error: Deleting the test services failed\n");
				return -1;
			}
		}
		else
		{
			wprintf(L"error: Invalid action '%s'\n", action.c_str());
			return -1;
		}
	}

	return 0;
}

VOID GenerateDependentServiceNames()
{
	LPCTSTR nameSuffix = L".Dependent";

	for (int i = 0; i < DEPENDENT_SERVICES; i++)
	{
		std::wstring& name = gDependentServiceNames[i];
		name = gServiceName;
		name = name + nameSuffix;
		name += '0' + i;

		std::wstring& displayName = gDependentServiceDisplayNames[i];
		displayName = gServiceDisplayName;
		displayName += nameSuffix;
		displayName += '0' + i;
	}
}

BOOL CreateTestServices()
{
	// Get a handle to the SCM database. 

	SC_HANDLE hScManager = OpenSCManager(
		NULL,                    // local computer
		NULL,                    // ServicesActive database 
		SC_MANAGER_ALL_ACCESS);  // full access rights

	if (hScManager == NULL)
	{
		wprintf(L"error: OpenSCManager failed (%d)\n", GetLastError());
		return false;
	}

	// Create the main test service

	std::wstring serviceCommand = gModulePath;
	serviceCommand += L" \"";
	serviceCommand += gServiceName;
	serviceCommand += L"\" \"";
	serviceCommand += gServiceDisplayName;
	serviceCommand += L"\"";

	SC_HANDLE hService = CreateTestService(
		hScManager,
		gServiceName,
		gServiceDisplayName,
		serviceCommand.c_str(),
		SERVICE_DEMAND_START
		);

	if (hService == NULL)
	{
		CloseServiceHandle(hScManager);
		return false;
	}

	// Create dependent services

	std::wstring dependencies = gServiceName;
	dependencies += (TCHAR)0;

	for (int i = 0; i < DEPENDENT_SERVICES; i++)
	{
		SC_HANDLE hDependentService = CreateTestService(
			hScManager,
			gDependentServiceNames[i].c_str(),
			gDependentServiceDisplayNames[i].c_str(),
			serviceCommand.c_str(),
			SERVICE_DISABLED,
			dependencies.c_str());

		if (hDependentService == NULL)
		{
			CloseServiceHandle(hScManager);
			return false;
		}

		// Make each dependent service depend on all of the services before it
		// for the sake of testing ServiceController.DependentServices and
		// ServiceController.ServicesDepended on. We do this by inserting the name
		// of the last dependent service created into the next dependent service's
		// dependency list before the double null.

		dependencies.insert(dependencies.end() - 1, (TCHAR)0);
		dependencies.insert(dependencies.length() - 1, gDependentServiceNames[i]);
	}

	// Attempt to start the main test service

	BOOL result = StartService(hService, 0, NULL);
	if (!result)
	{
		int error = GetLastError();
		if (error == ERROR_SERVICE_ALREADY_RUNNING)
		{
			wprintf(L"warning: Service '%s' is already running\n", gServiceName);
			result = true;
		}
		else
		{
			wprintf(L"error: StartService failed (%d)\n", error);
		}
	}

	CloseServiceHandle(hService);
	CloseServiceHandle(hScManager);
	return result;
}

SC_HANDLE CreateTestService(SC_HANDLE hScManager, LPCTSTR name, LPCTSTR displayName, LPCTSTR command, int startType, LPCTSTR dependencies)
{
	SC_HANDLE hService = CreateService(
		hScManager,                // SCM database 
		name,                      // name of service 
		displayName,               // service name to display 
		SERVICE_ALL_ACCESS,        // desired access 
		SERVICE_WIN32_OWN_PROCESS, // service type 
		startType,                 // start type 
		SERVICE_ERROR_NORMAL,      // error control type 
		command,                   // path to service's binary + arguments
		NULL,                      // no load ordering group 
		NULL,                      // no tag identifier 
		dependencies,              // dependencies (optional)
		NULL,                      // LocalSystem account 
		NULL);                     // no password 

	if (!hService)
	{
		BOOL result = false;
		int error = GetLastError();

		switch (error)
		{
		case ERROR_SERVICE_EXISTS:
			wprintf(L"warning: Service '%s' already exists.\n", name);

			hService = OpenService(hScManager, name, SERVICE_ALL_ACCESS);
			if (hService == NULL)
			{
				wprintf(L"error: Failed to open service '%s' (%d)\n", name, GetLastError());
				return NULL;
			}
			break;

		case ERROR_SERVICE_MARKED_FOR_DELETE:
			wprintf(L"error: Service '%s' exists and has been marked for deletion.\n", name);
			return NULL;

		default:
			wprintf(L"error: Failed to create service '%s' (%d)\n", name, error);
			return NULL;
		}
	}

	return hService;
}

BOOL DeleteTestServices()
{
	SC_HANDLE hScManager = OpenSCManager(
		NULL,                    // local computer
		NULL,                    // ServicesActive database 
		SC_MANAGER_ALL_ACCESS);  // full access rights

	if (hScManager == NULL)
	{
		wprintf(L"error: OpenSCManager failed (%d)\n", GetLastError());
		return false;
	}

	// Delete dependent services

	for (int i = 0; i < DEPENDENT_SERVICES; i++)
	{
		LPCTSTR name = gDependentServiceNames[i].c_str();

		SC_HANDLE hDependentService = OpenService(
			hScManager,
			name,
			SERVICE_ALL_ACCESS);

		if (hDependentService == NULL)
		{
			wprintf(L"warning: Failed to open service '%s' (%d)\n", name, GetLastError());
			continue;
		}

		DeleteTestService(hDependentService, name);
		CloseServiceHandle(hDependentService);
	}

	// Stop and delete the main test service

	SC_HANDLE hService = OpenService(
		hScManager,           // SCM database 
		gServiceName,         // name of service 
		SERVICE_ALL_ACCESS);  // desired access

	if (hService == NULL)
	{
		wprintf(L"error: Failed to open service '%s' (%d)\n", gServiceName, GetLastError());
		CloseServiceHandle(hScManager);
		return false;
	}

	SERVICE_CONTROL_STATUS_REASON_PARAMS reasonParams =
	{
		SERVICE_STOP_REASON_FLAG_PLANNED | SERVICE_STOP_REASON_MAJOR_NONE | SERVICE_STOP_REASON_MINOR_INSTALLATION,
		L"Stopping service for delete",
		{ 0 }
	};

	if (!ControlServiceEx(hService, SERVICE_CONTROL_STOP, SERVICE_CONTROL_STATUS_REASON_INFO, &reasonParams))
	{
		int error = GetLastError();
		if (error == ERROR_SERVICE_NOT_ACTIVE)
		{
			wprintf(L"warning: Service '%s' is already stopped\n", gServiceName);
		}
		else
		{
			wprintf(L"warning: Failed to stop service (%d). Will still delete, but recreating may fail if the service is still running.\n", error);
		}
	}

	BOOL result = DeleteTestService(hService, gServiceName);

	CloseServiceHandle(hService);
	CloseServiceHandle(hScManager);
	return result;
}

BOOL DeleteTestService(SC_HANDLE hService, LPCTSTR name)
{
	BOOL result = DeleteService(hService);

	if (!result)
	{
		wprintf(L"error: Failed to delete service '%s' (%d)\n", name, GetLastError());
	}

	return result;
}

//
// Purpose: 
//   Entry point for the service
//
// Parameters:
//   dwArgc - Number of arguments in the lpszArgv array
//   lpszArgv - Array of strings. The first string is the name of
//     the service and subsequent strings are passed by the process
//     that called the StartService function to start the service.
// 
// Return value:
//   None.
//
VOID WINAPI ServiceMain(DWORD dwArgc, LPTSTR* lpszArgv)
{
	// Register the handler function for the service

	gServiceStatusHandle = RegisterServiceCtrlHandler(gServiceName, ServiceCtrlHandler);

	if (!gServiceStatusHandle)
	{
		LogMessage(L"error: RegisterServiceCtrlHandler failed (%d)\n", GetLastError());
		return;
	}

	// These SERVICE_STATUS members remain as set here

	gServiceStatus.dwServiceType = SERVICE_WIN32_OWN_PROCESS;
	gServiceStatus.dwServiceSpecificExitCode = 0;

	// Report initial status to the SCM

	ServiceReportStatus(SERVICE_START_PENDING, NO_ERROR, 3000);

	// Perform service-specific initialization and work.

	ServiceInit(dwArgc, lpszArgv);
}

//
// Purpose: 
//   The service code
//
// Parameters:
//   dwArgc - Number of arguments in the lpszArgv array
//   lpszArgv - Array of strings. The first string is the name of
//     the service and subsequent strings are passed by the process
//     that called the StartService function to start the service.
// 
// Return value:
//   None
//
VOID ServiceInit(DWORD dwArgc, LPTSTR* lpszArgv)
{
	// Create an event. The control handler function, ServiceCtrlHandler,
	// signals this event when it receives the stop control code.

	ghServiceStopEvent = CreateEvent(
		NULL,    // default security attributes
		TRUE,    // manual reset event
		FALSE,   // not signaled
		NULL);   // no name

	if (ghServiceStopEvent == NULL)
	{
		ServiceReportStatus(SERVICE_STOPPED, NO_ERROR, 0);
		return;
	}

	InitModulePath();
	CreateLogFile();

	// Write the service arguments to the registry key:
	// HKEY_USERS\.DEFAULT\dotnetTests\ServiceController\<ServiceName>\ServiceArguments
	// to verify that they were correctly passed through.

	std::wstring keyPath = L".DEFAULT\\dotnetTests\\ServiceController\\";
	keyPath += gServiceName;

	HKEY hKey;
	LONG result = RegCreateKeyEx(
		HKEY_USERS,
		keyPath.c_str(),
		0,
		NULL,
		REG_OPTION_VOLATILE,
		KEY_ALL_ACCESS,
		NULL,
		&hKey,
		NULL);

	if (result != ERROR_SUCCESS)
	{
		LogMessage(L"warning: failed to open or create registry key 'HKEY_USERS\\%s' (%d)\n", keyPath.c_str(), result);
	}
	else
	{
		// Join the arguments array, separating each argument with a comma

		std::wstring argsString;
		DWORD i = 1;

		for (; i < dwArgc - 1; i++)
		{
			argsString += lpszArgv[i];
			argsString += L',';
		}

		if (i < dwArgc)
		{
			argsString += lpszArgv[i];
		}

		// Write the result to the value "ServiceArguments"

		LPCTSTR valueName = L"ServiceArguments";
		result = RegSetValueEx(
			hKey,
			valueName,
			0,
			REG_SZ,
			(const BYTE*) argsString.c_str(),
			(DWORD) ((argsString.length() + 1) * sizeof(wchar_t)));

		if (result != ERROR_SUCCESS)
		{
			LogMessage(L"warning: failed to set value '%s' = '%s' in registry key 'HKEY_USERS\\%s' (%d)\n", valueName, argsString.c_str(), keyPath.c_str(), result);
		}

		RegCloseKey(hKey);
	}

	// Report running status when initialization is complete.

	ServiceReportStatus(SERVICE_RUNNING, NO_ERROR, 0);

	while (1)
	{
		// Check whether to stop the service.

		WaitForSingleObject(ghServiceStopEvent, INFINITE);

		// We're stopping, delete the log file
		DWORD error = DeleteLogFile();

		ServiceReportStatus(SERVICE_STOPPED, error, 0);
		return;
	}
}

//
// Purpose: 
//   Sets the current service status and reports it to the SCM.
//
// Parameters:
//   dwCurrentState - The current state (see SERVICE_STATUS)
//   dwWin32ExitCode - The system error code
//   dwWaitHint - Estimated time for pending operation, 
//     in milliseconds
// 
// Return value:
//   None
//
VOID ServiceReportStatus(DWORD dwCurrentState, DWORD dwWin32ExitCode, DWORD dwWaitHint)
{
	static DWORD dwCheckPoint = 1;

	// Fill in the SERVICE_STATUS structure.

	gServiceStatus.dwCurrentState = dwCurrentState;
	gServiceStatus.dwWin32ExitCode = dwWin32ExitCode;
	gServiceStatus.dwWaitHint = dwWaitHint;

	if (dwCurrentState == SERVICE_START_PENDING)
		gServiceStatus.dwControlsAccepted = 0;
	else gServiceStatus.dwControlsAccepted = SERVICE_ACCEPT_STOP | SERVICE_ACCEPT_PAUSE_CONTINUE;

	if ((dwCurrentState == SERVICE_RUNNING) ||
		(dwCurrentState == SERVICE_STOPPED))
		gServiceStatus.dwCheckPoint = 0;
	else gServiceStatus.dwCheckPoint = dwCheckPoint++;

	// Report the status of the service to the SCM.

	SetServiceStatus(gServiceStatusHandle, &gServiceStatus);
}

//
// Purpose: 
//   Called by SCM whenever a control code is sent to the service
//   using the ControlService function.
//
// Parameters:
//   dwCtrl - control code
// 
// Return value:
//   None
//
VOID WINAPI ServiceCtrlHandler(DWORD dwCtrl)
{
	// Handle the requested control code. 

	switch (dwCtrl)
	{
	case SERVICE_CONTROL_STOP:
		ServiceReportStatus(SERVICE_STOP_PENDING, NO_ERROR, 0);

		// Signal the service to stop.

		SetEvent(ghServiceStopEvent);
		ServiceReportStatus(gServiceStatus.dwCurrentState, NO_ERROR, 0);
		break;

	case SERVICE_CONTROL_PAUSE:
		ServiceReportStatus(SERVICE_PAUSED, NO_ERROR, 0);
		break;

	case SERVICE_CONTROL_CONTINUE:
		ServiceReportStatus(SERVICE_RUNNING, NO_ERROR, 0);
		break;

	case SERVICE_CONTROL_INTERROGATE:
		break;

	default:
		break;
	}
}

BOOL InitModulePath()
{
	if (!GetModuleFileName(NULL, gModulePath, MAX_PATH))
	{
		wprintf(L"error: Failed to get module file name (%d)\n", GetLastError());
		return FALSE;
	}

	return TRUE;
}

VOID CreateLogFile()
{
	gLogFilePath = gModulePath;
	gLogFilePath += L'.';
	gLogFilePath += gServiceName;
	gLogFilePath += L".txt";

	ghLogFile = CreateFile(
		gLogFilePath.c_str(),
		GENERIC_WRITE,
		FILE_SHARE_READ,
		NULL,
		CREATE_ALWAYS,
		FILE_ATTRIBUTE_NORMAL,
		NULL);

	if (ghLogFile == INVALID_HANDLE_VALUE)
	{
		wprintf(L"warning: Failed to create log file '%s'\n", gLogFilePath.c_str());
	}
}

DWORD DeleteLogFile()
{
	CloseHandle(ghLogFile);

	if (!DeleteFile(gLogFilePath.c_str()))
	{
		DWORD error = GetLastError();
		LogMessage(L"warning: Failed to delete log file '%s' (%d)\n", gLogFilePath.c_str(), error);
		return error;
	}

	return NO_ERROR;
}

VOID LogMessage(LPCTSTR format, ...)
{
	TCHAR buffer[256];
	va_list args;
	va_start(args, format);

	int numChars = _vstprintf_s(buffer, 256, format, args);

	BOOL result = WriteFile(
		ghLogFile,
		buffer,
		numChars * sizeof(TCHAR),
		NULL,
		NULL);

	if (!result)
	{
		wprintf(L"warning: Failed to write to the log file (%d): %s", GetLastError(), buffer);
	}

	va_end(args);
}