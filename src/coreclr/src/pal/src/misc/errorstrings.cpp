//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*++



Module Name:

    errorstrings.cpp

Abstract:

    Conversion of PAL error code to string

Revision History:



--*/

#include "pal/palinternal.h"
#include "errorstrings.h"

struct ErrorString
{
    DWORD code;
    LPCWSTR const string;
};

ErrorString palErrorStrings[] =
{
    { ERROR_SUCCESS, W("The operation completed successfully.\n") },
    { ERROR_INVALID_FUNCTION, W("Incorrect function.\n") },
    { ERROR_FILE_NOT_FOUND, W("The system cannot find the file specified.\n") },
    { ERROR_PATH_NOT_FOUND, W("The system cannot find the path specified.\n") },
    { ERROR_TOO_MANY_OPEN_FILES, W("The system cannot open the file.\n") },
    { ERROR_ACCESS_DENIED, W("Access is denied.\n") },
    { ERROR_INVALID_HANDLE, W("The handle is invalid.\n") },
    { ERROR_NOT_ENOUGH_MEMORY, W("Not enough storage is available to process this command.\n") },
    { ERROR_BAD_ENVIRONMENT, W("The environment is incorrect.\n") },
    { ERROR_BAD_FORMAT, W("An attempt was made to load a program with an incorrect format.\n") },
    { ERROR_INVALID_ACCESS, W("The access code is invalid.\n") },
    { ERROR_INVALID_DATA, W("The data is invalid.\n") },
    { ERROR_OUTOFMEMORY, W("Not enough storage is available to complete this operation.\n") },
    { ERROR_INVALID_DRIVE, W("The system cannot find the drive specified.\n") },
    { ERROR_NO_MORE_FILES, W("There are no more files.\n") },
    { ERROR_WRITE_PROTECT, W("The media is write protected.\n") },
    { ERROR_NOT_READY, W("The device is not ready.\n") },
    { ERROR_BAD_COMMAND, W("The device does not recognize the command.\n") },
    { ERROR_BAD_LENGTH, W("The program issued a command but the command length is incorrect.\n") },
    { ERROR_WRITE_FAULT, W("The system cannot write to the specified device.\n") },
    { ERROR_READ_FAULT, W("The system cannot read from the specified device.\n") },
    { ERROR_GEN_FAILURE, W("A device attached to the system is not functioning.\n") },
    { ERROR_SHARING_VIOLATION, W("The process cannot access the file because it is being used by another process.\n") },
    { ERROR_LOCK_VIOLATION, W("The process cannot access the file because another process has locked a portion of the file.\n") },
    { ERROR_SHARING_BUFFER_EXCEEDED, W("Too many files opened for sharing.\n") },
    { ERROR_HANDLE_EOF, W("Reached the end of the file.\n") },
    { ERROR_HANDLE_DISK_FULL, W("The disk is full.\n") },
    { ERROR_NOT_SUPPORTED, W("The request is not supported.\n") },
    { ERROR_DUP_NAME, W("A duplicate name exists on the network.\n") },
    { ERROR_BAD_NETPATH, W("The network path was not found.\n") },
    { ERROR_DEV_NOT_EXIST, W("The specified network resource or device is no longer available.\n") },
    { ERROR_BAD_NET_NAME, W("The network name cannot be found.\n") },
    { ERROR_FILE_EXISTS, W("The file exists.\n") },
    { ERROR_CANNOT_MAKE, W("The directory or file cannot be created.\n") },
    { ERROR_INVALID_PARAMETER, W("The parameter is incorrect.\n") },
    { ERROR_NET_WRITE_FAULT, W("A write fault occurred on the network.\n") },
    { ERROR_DRIVE_LOCKED, W("The disk is in use or locked by another process.\n") },
    { ERROR_BROKEN_PIPE, W("The pipe has been ended.\n") },
    { ERROR_OPEN_FAILED, W("The system cannot open the device or file specified.\n") },
    { ERROR_BUFFER_OVERFLOW, W("The file name is too long.\n") },
    { ERROR_DISK_FULL, W("There is not enough space on the disk.\n") },
    { ERROR_CALL_NOT_IMPLEMENTED, W("This function is not supported on this system.\n") },
    { ERROR_SEM_TIMEOUT, W("The semaphore timeout period has expired.\n") },
    { ERROR_INSUFFICIENT_BUFFER, W("The data area passed to a system call is too small.\n") },
    { ERROR_INVALID_NAME, W("The filename, directory name, or volume label syntax is incorrect.\n") },
    { ERROR_MOD_NOT_FOUND, W("The specified module could not be found.\n") },
    { ERROR_PROC_NOT_FOUND, W("The specified procedure could not be found.\n") },
    { ERROR_WAIT_NO_CHILDREN, W("There are no child processes to wait for.\n") },
    { ERROR_NEGATIVE_SEEK, W("An attempt was made to move the file pointer before the beginning of the file.\n") },
    { ERROR_SEEK_ON_DEVICE, W("The file pointer cannot be set on the specified device or file.\n") },
    { ERROR_DIR_NOT_EMPTY, W("The directory is not empty.\n") },
    { ERROR_SIGNAL_REFUSED, W("The recipient process has refused the signal.\n") },
    { ERROR_NOT_LOCKED, W("The segment is already unlocked.\n") },
    { ERROR_BAD_PATHNAME, W("The specified path is invalid.\n") },
    { ERROR_BUSY, W("The requested resource is in use.\n") },
    { ERROR_INVALID_ORDINAL, W("The operating system cannot run %1.\n") },
    { ERROR_ALREADY_EXISTS, W("Cannot create a file when that file already exists.\n") },
    { ERROR_INVALID_EXE_SIGNATURE, W("Cannot run %1 in Win32 mode.\n") },
    { ERROR_EXE_MARKED_INVALID, W("The operating system cannot run %1.\n") },
    { ERROR_BAD_EXE_FORMAT, W("%1 is not a valid Win32 application.\n") },
    { ERROR_ENVVAR_NOT_FOUND, W("The system could not find the environment option that was entered.\n") },
    { ERROR_FILENAME_EXCED_RANGE, W("The filename or extension is too long.\n") },
    { ERROR_PIPE_BUSY, W("All pipe instances are busy.\n") },
    { ERROR_NO_DATA,  W("The pipe is being closed\n")},
    { ERROR_MORE_DATA, W("More data is available.\n") },
    { ERROR_NO_MORE_ITEMS, W("No more data is available.\n") },
    { ERROR_DIRECTORY, W("The directory name is invalid.\n") },
    { ERROR_NOT_OWNER, W("Attempt to release mutex not owned by caller.\n") },
    { ERROR_PARTIAL_COPY, W("Only part of a ReadProcessMemory or WriteProcessMemory request was completed.\n") },
    { ERROR_INVALID_ADDRESS, W("Attempt to access invalid address.\n") },
    { ERROR_ARITHMETIC_OVERFLOW, W("Arithmetic result exceeded 32 bits.\n") },
    { ERROR_OPERATION_ABORTED, W("The I/O operation has been aborted because of either a thread exit or an application request.\n") },
    { ERROR_IO_INCOMPLETE, W("Overlapped I/O event is not in a signaled state.\n") },
    { ERROR_IO_PENDING, W("Overlapped I/O operation is in progress.\n") },
    { ERROR_NOACCESS, W("Invalid access to memory location.\n") },
    { ERROR_STACK_OVERFLOW, W("Recursion too deep; the stack overflowed.\n") },
    { ERROR_INVALID_FLAGS, W("Invalid flags.\n") },
    { ERROR_UNRECOGNIZED_VOLUME, W("The volume does not contain a recognized file system.\nPlease make sure that all required file system drivers are loaded and that the volume is not corrupted.\n") },
    { ERROR_FILE_INVALID, W("The volume for a file has been externally altered so that the opened file is no longer valid.\n") },
    { ERROR_PROCESS_ABORTED, W("The process terminated unexpectedly.\n") },
    { ERROR_NO_UNICODE_TRANSLATION, W("No mapping for the Unicode character exists in the target multi-byte code page.\n") },
    { ERROR_DLL_INIT_FAILED, W("A dynamic link library (DLL) initialization routine failed.\n") },
    { ERROR_IO_DEVICE, W("The request could not be performed because of an I/O device error.\n") },
    { ERROR_DISK_OPERATION_FAILED, W("While accessing the hard disk, a disk operation failed even after retries.\n") },
    { ERROR_POSSIBLE_DEADLOCK, W("A potential deadlock condition has been detected.\n") },
    { ERROR_TOO_MANY_LINKS, W("An attempt was made to create more links on a file than the file system supports.\n") },
    { ERROR_INVALID_DLL, W("One of the library files needed to run this application is damaged.\n") },
    { ERROR_DLL_NOT_FOUND, W("One of the library files needed to run this application cannot be found.\n") },
    { ERROR_NOT_FOUND, W("Element not found.\n") },
    { ERROR_CANCELLED, W("The operation was canceled by the user.\n") },
    { ERROR_NOT_AUTHENTICATED, W("The operation being requested was not performed because the user has not been authenticated.\n") },
    { ERROR_INTERNAL_ERROR, W("An internal error occurred.\n") },
    { ERROR_FILE_CORRUPT, W("The file or directory is corrupted and unreadable.\n") },
    { ERROR_DISK_CORRUPT, W("The disk structure is corrupted and unreadable.\n") },
    { ERROR_WRONG_TARGET_NAME, W("Logon Failure: The target account name is incorrect.\n") },
    { ERROR_NO_SYSTEM_RESOURCES, W("Insufficient system resources exist to complete the requested service.\n") },
    { ERROR_COMMITMENT_LIMIT, W("The paging file is too small for this operation to complete.\n") },
    { ERROR_TIMEOUT, W("This operation returned because the timeout period expired.\n") },
    { ERROR_EVENTLOG_FILE_CORRUPT, W("The event log file is corrupted.\n") },
    { ERROR_LOG_FILE_FULL, W("The event log file is full.\n") },
    { ERROR_UNSUPPORTED_TYPE, W("Data of this type is not supported.\n") },
    { RPC_S_INVALID_VERS_OPTION, W("The version option is invalid.\n") },
    { ERROR_RESOURCE_DATA_NOT_FOUND, W("The specified image file did not contain a resource section.\n") },
    { ERROR_RESOURCE_LANG_NOT_FOUND, W("The specified resource language ID cannot be found in the image file.\n") },
    { ERROR_TAG_NOT_PRESENT, W("A required tag is not present.\n") },
    { WSAEINTR, W("A blocking operation was interrupted by a call to WSACancelBlockingCall.\n") },
    { WSAEBADF, W("The file handle supplied is not valid.\n") },
    { WSAEACCES, W("An attempt was made to access a socket in a way forbidden by its access permissions.\n") },
    { WSAEFAULT, W("The system detected an invalid pointer address in attempting to use a pointer argument in a call.\n") },
    { WSAEINVAL, W("An invalid argument was supplied.\n") },
    { WSAEMFILE, W("Too many open sockets.\n") },
    { WSAEWOULDBLOCK, W("A non-blocking socket operation could not be completed immediately.\n") },
    { WSAEINPROGRESS, W("A blocking operation is currently executing.\n") },
    { WSAEALREADY, W("An operation was attempted on a non-blocking socket that already had an operation in progress.\n") },
    { WSAENOTSOCK, W("An operation was attempted on something that is not a socket.\n") },
    { WSAEDESTADDRREQ, W("A required address was omitted from an operation on a socket.\n") },
    { WSAEMSGSIZE, W("A message sent on a datagram socket was larger than the internal message buffer or some other network limit, or the buffer used to receive a datagram into was smaller than the datagram itself.\n") },
    { WSAEPROTOTYPE, W("A protocol was specified in the socket function call that does not support the semantics of the socket type requested.\n") },
    { WSAENOPROTOOPT, W("An unknown, invalid, or unsupported option or level was specified in a getsockopt or setsockopt call.\n") },
    { WSAEPROTONOSUPPORT, W("The requested protocol has not been configured into the system, or no implementation for it exists.\n") },
    { WSAESOCKTNOSUPPORT, W("The support for the specified socket type does not exist in this address family.\n") },
    { WSAEOPNOTSUPP, W("The attempted operation is not supported for the type of object referenced.\n") },
    { WSAEPFNOSUPPORT, W("The protocol family has not been configured into the system or no implementation for it exists.\n") },
    { WSAEAFNOSUPPORT, W("An address incompatible with the requested protocol was used.\n") },
    { WSAEADDRINUSE, W("Only one usage of each socket address (protocol/network address/port) is normally permitted.\n") },
    { WSAEADDRNOTAVAIL, W("The requested address is not valid in its context.\n") },
    { WSAENETDOWN, W("A socket operation encountered a dead network.\n") },
    { WSAENETUNREACH, W("A socket operation was attempted to an unreachable network.\n") },
    { WSAENETRESET, W("The connection has been broken due to keep-alive activity detecting a failure while the operation was in progress.\n") },
    { WSAECONNABORTED, W("An established connection was aborted by the software in your host machine.\n") },
    { WSAECONNRESET, W("An existing connection was forcibly closed by the remote host.\n") },
    { WSAENOBUFS, W("An operation on a socket could not be performed because the system lacked sufficient buffer space or because a queue was full.\n") },
    { WSAEISCONN, W("A connect request was made on an already connected socket.\n") },
    { WSAENOTCONN, W("A request to send or receive data was disallowed because the socket is not connected and (when sending on a datagram socket using a sendto call) no address was supplied.\n") },
    { WSAESHUTDOWN, W("A request to send or receive data was disallowed because the socket had already been shut down in that direction with a previous shutdown call.\n") },
    { WSAETOOMANYREFS, W("Too many references to some kernel object.\n") },
    { WSAETIMEDOUT, W("A connection attempt failed because the connected party did not properly respond after a period of time, or established connection failed because connected host has failed to respond.\n") },
    { WSAECONNREFUSED, W("No connection could be made because the target machine actively refused it.\n") },
    { WSAELOOP, W("Cannot translate name.\n") },
    { WSAENAMETOOLONG, W("Name component or name was too long.\n") },
    { WSAEHOSTDOWN, W("A socket operation failed because the destination host was down.\n") },
    { WSAEHOSTUNREACH, W("A socket operation was attempted to an unreachable host.\n") },
    { WSAENOTEMPTY, W("Cannot remove a directory that is not empty.\n") },
    { WSAEPROCLIM, W("A sockets implementation may have a limit on the number of applications that may use it simultaneously.\n") },
    { WSAEUSERS, W("Ran out of quota.\n") },
    { WSAEDQUOT, W("Ran out of disk quota.\n") },
    { WSAESTALE, W("File handle reference is no longer available.\n") },
    { WSAEREMOTE, W("Item is not available locally.\n") },
    { WSASYSNOTREADY, W("WSAStartup cannot function at this time because the underlying system it uses to provide network services is currently unavailable.\n") },
    { WSAVERNOTSUPPORTED, W("The sockets version requested is not supported.\n") },
    { WSANOTINITIALISED, W("Either the application has not called WSAStartup, or WSAStartup failed.\n") },
    { WSATYPE_NOT_FOUND, W("The specified class was not found.\n") },
    { WSAHOST_NOT_FOUND, W("No such host is known.\n") },
    { WSATRY_AGAIN, W("This is usually a temporary error during hostname resolution and means that the local server did not receive a response from an authoritative server.\n") },
    { WSANO_RECOVERY, W("A non-recoverable error occurred during a database lookup.\n") },
    { WSANO_DATA, W("The requested name is valid and was found in the database, but it does not have the correct associated data being resolved for.\n") }
};

int CompareErrorStrings(const void *a, const void *b)
{
    DWORD codeA = ((ErrorString *)a)->code;
    DWORD codeB = ((ErrorString *)b)->code;

    if (codeA < codeB)
    {
        return -1;
    }
    else if (codeA == codeB)
    {
        return 0;
    }
    else 
    {
        return 1;
    }
}

LPCWSTR GetPalErrorString(DWORD code)
{
    // Search the sorted set of resources for the ID we're interested in.
    ErrorString searchEntry = {code, NULL};
    ErrorString *stringEntry = (ErrorString *)bsearch(
        &searchEntry,
        palErrorStrings,
        sizeof(palErrorStrings) / sizeof(palErrorStrings[0]),
        sizeof(ErrorString),
        CompareErrorStrings);

    return (stringEntry != NULL) ? stringEntry->string : NULL;
}
