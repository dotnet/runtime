/*
 * messages.c:  Error message handling
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2008 Novell, Inc.
 */

#include <config.h>
#include <glib.h>
#include <stdarg.h>
#include <string.h>

#include <mono/io-layer/wapi.h>
#include <mono/io-layer/wapi-private.h>
#include <mono/io-layer/misc-private.h>
#include <mono/io-layer/messages.h>

#undef DEBUG

static const gchar *message_string (guint32 id);

static guint32 unicode_chars (const gunichar2 *str)
{
	guint32 len = 0;
	
	do {
		if (str[len] == '\0') {
			return(len);
		}
		len++;
	} while(1);
}

guint32 FormatMessage (guint32 flags, gconstpointer source, guint32 messageid,
		       guint32 languageid, gunichar2 *buf, guint32 size, ...)
{
	/*va_list ap;*/
	guint32 strlen, cpy;
	gunichar2 *str;
	gboolean freestr = FALSE;
	
	if ((flags & FORMAT_MESSAGE_FROM_HMODULE) ||
	    (flags & FORMAT_MESSAGE_ARGUMENT_ARRAY) ||
	    !(flags & FORMAT_MESSAGE_IGNORE_INSERTS)) {
		g_warning ("%s: Unsupported flags passed: %d", __func__,
			   flags);
		SetLastError (ERROR_NOT_SUPPORTED);
		return(0);
	}

	if ((flags & FORMAT_MESSAGE_MAX_WIDTH_MASK) != 0) {
		g_warning ("%s: Message width mask (%d) not supported",
			   __func__, (flags & FORMAT_MESSAGE_MAX_WIDTH_MASK));
	}
	
	if (languageid != 0) {
		g_warning ("%s: Locale 0x%x not supported, returning language neutral string", __func__, languageid);
	}
	
	/* We're only supporting IGNORE_INSERTS, so we don't need to
	 * use va_start (ap, size) and va_end (ap)
	 */

	if (flags & FORMAT_MESSAGE_FROM_STRING) {
		str = (gunichar2 *)source;
	} else if (flags & FORMAT_MESSAGE_FROM_SYSTEM) {
		str = g_utf8_to_utf16 (message_string (messageid), -1, NULL,
				       NULL, NULL);
		freestr = TRUE;
	}

	strlen = unicode_chars (str);

	if (flags & FORMAT_MESSAGE_ALLOCATE_BUFFER) {
		*(gpointer *)buf = (gunichar2 *)g_new0 (gunichar2, strlen + 2 < size?size:strlen + 2);
	}

	if (strlen >= size) {
		cpy = size - 1;
	} else {
		cpy = strlen;
	}
	memcpy (buf, str, cpy * 2);
	buf[cpy] = '\0';

	if (freestr) {
		g_free (str);
	}
	
	return(strlen);
}

static const gchar *message_string (guint32 id)
{
	switch(id) {
	case ERROR_SUCCESS:
		return("Success");
		break;
	case ERROR_INVALID_FUNCTION:
		return("Invalid function");
		break;
	case ERROR_FILE_NOT_FOUND:
		return("Cannot find the specified file");
		break;
	case ERROR_PATH_NOT_FOUND:
		return("Cannot find the specified file");
		break;
	case ERROR_TOO_MANY_OPEN_FILES:
		return("Too many open files");
		break;
	case ERROR_ACCESS_DENIED:
		return("Access denied");
		break;
	case ERROR_INVALID_HANDLE:
		return("Invalid handle");
		break;
	case ERROR_ARENA_TRASHED:
		return("Arena trashed");
		break;
	case ERROR_NOT_ENOUGH_MEMORY:
		return("Not enough memory");
		break;
	case ERROR_INVALID_BLOCK:
		return("Invalid block");
		break;
	case ERROR_BAD_ENVIRONMENT:
		return("Bad environment");
		break;
	case ERROR_BAD_FORMAT:
		return("Bad format");
		break;
	case ERROR_INVALID_ACCESS:
		return("Invalid access");
		break;
	case ERROR_INVALID_DATA:
		return("Invalid data");
		break;
	case ERROR_OUTOFMEMORY:
		return("Out of memory");
		break;
	case ERROR_INVALID_DRIVE:
		return("Invalid drive");
		break;
	case ERROR_CURRENT_DIRECTORY:
		return("Current directory");
		break;
	case ERROR_NOT_SAME_DEVICE:
		return("Not same device");
		break;
	case ERROR_NO_MORE_FILES:
		return("No more files");
		break;
	case ERROR_WRITE_PROTECT:
		return("Write protect");
		break;
	case ERROR_BAD_UNIT:
		return("Bad unit");
		break;
	case ERROR_NOT_READY:
		return("Not ready");
		break;
	case ERROR_BAD_COMMAND:
		return("Bad command");
		break;
	case ERROR_CRC:
		return("CRC");
		break;
	case ERROR_BAD_LENGTH:
		return("Bad length");
		break;
	case ERROR_SEEK:
		return("Seek");
		break;
	case ERROR_NOT_DOS_DISK:
		return("Not DOS disk");
		break;
	case ERROR_SECTOR_NOT_FOUND:
		return("Sector not found");
		break;
	case ERROR_OUT_OF_PAPER:
		return("Out of paper");
		break;
	case ERROR_WRITE_FAULT:
		return("Write fault");
		break;
	case ERROR_READ_FAULT:
		return("Read fault");
		break;
	case ERROR_GEN_FAILURE:
		return("General failure");
		break;
	case ERROR_SHARING_VIOLATION:
		return("Sharing violation");
		break;
	case ERROR_LOCK_VIOLATION:
		return("Lock violation");
		break;
	case ERROR_WRONG_DISK:
		return("Wrong disk");
		break;
	case ERROR_SHARING_BUFFER_EXCEEDED:
		return("Sharing buffer exceeded");
		break;
	case ERROR_HANDLE_EOF:
		return("Handle EOF");
		break;
	case ERROR_HANDLE_DISK_FULL:
		return("Handle disk full");
		break;
	case ERROR_NOT_SUPPORTED:
		return("Operation not supported");
		break;
	case ERROR_REM_NOT_LIST:
		return("Rem not list");
		break;
	case ERROR_DUP_NAME:
		return("Duplicate name");
		break;
	case ERROR_BAD_NETPATH:
		return("Bad netpath");
		break;
	case ERROR_NETWORK_BUSY:
		return("Network busy");
		break;
	case ERROR_DEV_NOT_EXIST:
		return("Device does not exist");
		break;
	case ERROR_TOO_MANY_CMDS:
		return("Too many commands");
		break;
	case ERROR_ADAP_HDW_ERR:
		return("ADAP HDW error");
		break;
	case ERROR_BAD_NET_RESP:
		return("Bad net response");
		break;
	case ERROR_UNEXP_NET_ERR:
		return("Unexpected net error");
		break;
	case ERROR_BAD_REM_ADAP:
		return("Bad rem adap");
		break;
	case ERROR_PRINTQ_FULL:
		return("Print queue full");
		break;
	case ERROR_NO_SPOOL_SPACE:
		return("No spool space");
		break;
	case ERROR_PRINT_CANCELLED:
		return("Print cancelled");
		break;
	case ERROR_NETNAME_DELETED:
		return("Netname deleted");
		break;
	case ERROR_NETWORK_ACCESS_DENIED:
		return("Network access denied");
		break;
	case ERROR_BAD_DEV_TYPE:
		return("Bad device type");
		break;
	case ERROR_BAD_NET_NAME:
		return("Bad net name");
		break;
	case ERROR_TOO_MANY_NAMES:
		return("Too many names");
		break;
	case ERROR_TOO_MANY_SESS:
		return("Too many sessions");
		break;
	case ERROR_SHARING_PAUSED:
		return("Sharing paused");
		break;
	case ERROR_REQ_NOT_ACCEP:
		return("Req not accep");
		break;
	case ERROR_REDIR_PAUSED:
		return("Redir paused");
		break;
	case ERROR_FILE_EXISTS:
		return("File exists");
		break;
	case ERROR_CANNOT_MAKE:
		return("Cannot make");
		break;
	case ERROR_FAIL_I24:
		return("Fail i24");
		break;
	case ERROR_OUT_OF_STRUCTURES:
		return("Out of structures");
		break;
	case ERROR_ALREADY_ASSIGNED:
		return("Already assigned");
		break;
	case ERROR_INVALID_PASSWORD:
		return("Invalid password");
		break;
	case ERROR_INVALID_PARAMETER:
		return("Invalid parameter");
		break;
	case ERROR_NET_WRITE_FAULT:
		return("Net write fault");
		break;
	case ERROR_NO_PROC_SLOTS:
		return("No proc slots");
		break;
	case ERROR_TOO_MANY_SEMAPHORES:
		return("Too many semaphores");
		break;
	case ERROR_EXCL_SEM_ALREADY_OWNED:
		return("Exclusive semaphore already owned");
		break;
	case ERROR_SEM_IS_SET:
		return("Semaphore is set");
		break;
	case ERROR_TOO_MANY_SEM_REQUESTS:
		return("Too many semaphore requests");
		break;
	case ERROR_INVALID_AT_INTERRUPT_TIME:
		return("Invalid at interrupt time");
		break;
	case ERROR_SEM_OWNER_DIED:
		return("Semaphore owner died");
		break;
	case ERROR_SEM_USER_LIMIT:
		return("Semaphore user limit");
		break;
	case ERROR_DISK_CHANGE:
		return("Disk change");
		break;
	case ERROR_DRIVE_LOCKED:
		return("Drive locked");
		break;
	case ERROR_BROKEN_PIPE:
		return("Broken pipe");
		break;
	case ERROR_OPEN_FAILED:
		return("Open failed");
		break;
	case ERROR_BUFFER_OVERFLOW:
		return("Buffer overflow");
		break;
	case ERROR_DISK_FULL:
		return("Disk full");
		break;
	case ERROR_NO_MORE_SEARCH_HANDLES:
		return("No more search handles");
		break;
	case ERROR_INVALID_TARGET_HANDLE:
		return("Invalid target handle");
		break;
	case ERROR_INVALID_CATEGORY:
		return("Invalid category");
		break;
	case ERROR_INVALID_VERIFY_SWITCH:
		return("Invalid verify switch");
		break;
	case ERROR_BAD_DRIVER_LEVEL:
		return("Bad driver level");
		break;
	case ERROR_CALL_NOT_IMPLEMENTED:
		return("Call not implemented");
		break;
	case ERROR_SEM_TIMEOUT:
		return("Semaphore timeout");
		break;
	case ERROR_INSUFFICIENT_BUFFER:
		return("Insufficient buffer");
		break;
	case ERROR_INVALID_NAME:
		return("Invalid name");
		break;
	case ERROR_INVALID_LEVEL:
		return("Invalid level");
		break;
	case ERROR_NO_VOLUME_LABEL:
		return("No volume label");
		break;
	case ERROR_MOD_NOT_FOUND:
		return("Module not found");
		break;
	case ERROR_PROC_NOT_FOUND:
		return("Process not found");
		break;
	case ERROR_WAIT_NO_CHILDREN:
		return("Wait no children");
		break;
	case ERROR_CHILD_NOT_COMPLETE:
		return("Child not complete");
		break;
	case ERROR_DIRECT_ACCESS_HANDLE:
		return("Direct access handle");
		break;
	case ERROR_NEGATIVE_SEEK:
		return("Negative seek");
		break;
	case ERROR_SEEK_ON_DEVICE:
		return("Seek on device");
		break;
	case ERROR_IS_JOIN_TARGET:
		return("Is join target");
		break;
	case ERROR_IS_JOINED:
		return("Is joined");
		break;
	case ERROR_IS_SUBSTED:
		return("Is substed");
		break;
	case ERROR_NOT_JOINED:
		return("Not joined");
		break;
	case ERROR_NOT_SUBSTED:
		return("Not substed");
		break;
	case ERROR_JOIN_TO_JOIN:
		return("Join to join");
		break;
	case ERROR_SUBST_TO_SUBST:
		return("Subst to subst");
		break;
	case ERROR_JOIN_TO_SUBST:
		return("Join to subst");
		break;
	case ERROR_SUBST_TO_JOIN:
		return("Subst to join");
		break;
	case ERROR_BUSY_DRIVE:
		return("Busy drive");
		break;
	case ERROR_SAME_DRIVE:
		return("Same drive");
		break;
	case ERROR_DIR_NOT_ROOT:
		return("Directory not root");
		break;
	case ERROR_DIR_NOT_EMPTY:
		return("Directory not empty");
		break;
	case ERROR_IS_SUBST_PATH:
		return("Is subst path");
		break;
	case ERROR_IS_JOIN_PATH:
		return("Is join path");
		break;
	case ERROR_PATH_BUSY:
		return("Path busy");
		break;
	case ERROR_IS_SUBST_TARGET:
		return("Is subst target");
		break;
	case ERROR_SYSTEM_TRACE:
		return("System trace");
		break;
	case ERROR_INVALID_EVENT_COUNT:
		return("Invalid event count");
		break;
	case ERROR_TOO_MANY_MUXWAITERS:
		return("Too many muxwaiters");
		break;
	case ERROR_INVALID_LIST_FORMAT:
		return("Invalid list format");
		break;
	case ERROR_LABEL_TOO_LONG:
		return("Label too long");
		break;
	case ERROR_TOO_MANY_TCBS:
		return("Too many TCBs");
		break;
	case ERROR_SIGNAL_REFUSED:
		return("Signal refused");
		break;
	case ERROR_DISCARDED:
		return("Discarded");
		break;
	case ERROR_NOT_LOCKED:
		return("Not locked");
		break;
	case ERROR_BAD_THREADID_ADDR:
		return("Bad thread ID addr");
		break;
	case ERROR_BAD_ARGUMENTS:
		return("Bad arguments");
		break;
	case ERROR_BAD_PATHNAME:
		return("Bad pathname");
		break;
	case ERROR_SIGNAL_PENDING:
		return("Signal pending");
		break;
	case ERROR_MAX_THRDS_REACHED:
		return("Max thrds reached");
		break;
	case ERROR_LOCK_FAILED:
		return("Lock failed");
		break;
	case ERROR_BUSY:
		return("Busy");
		break;
	case ERROR_CANCEL_VIOLATION:
		return("Cancel violation");
		break;
	case ERROR_ATOMIC_LOCKS_NOT_SUPPORTED:
		return("Atomic locks not supported");
		break;
	case ERROR_INVALID_SEGMENT_NUMBER:
		return("Invalid segment number");
		break;
	case ERROR_INVALID_ORDINAL:
		return("Invalid ordinal");
		break;
	case ERROR_ALREADY_EXISTS:
		return("Already exists");
		break;
	case ERROR_INVALID_FLAG_NUMBER:
		return("Invalid flag number");
		break;
	case ERROR_SEM_NOT_FOUND:
		return("Sem not found");
		break;
	case ERROR_INVALID_STARTING_CODESEG:
		return("Invalid starting codeseg");
		break;
	case ERROR_INVALID_STACKSEG:
		return("Invalid stackseg");
		break;
	case ERROR_INVALID_MODULETYPE:
		return("Invalid moduletype");
		break;
	case ERROR_INVALID_EXE_SIGNATURE:
		return("Invalid exe signature");
		break;
	case ERROR_EXE_MARKED_INVALID:
		return("Exe marked invalid");
		break;
	case ERROR_BAD_EXE_FORMAT:
		return("Bad exe format");
		break;
	case ERROR_ITERATED_DATA_EXCEEDS_64k:
		return("Iterated data exceeds 64k (and that should be enough for anybody!)");
		break;
	case ERROR_INVALID_MINALLOCSIZE:
		return("Invalid minallocsize");
		break;
	case ERROR_DYNLINK_FROM_INVALID_RING:
		return("Dynlink from invalid ring");
		break;
	case ERROR_IOPL_NOT_ENABLED:
		return("IOPL not enabled");
		break;
	case ERROR_INVALID_SEGDPL:
		return("Invalid segdpl");
		break;
	case ERROR_AUTODATASEG_EXCEEDS_64k:
		return("Autodataseg exceeds 64k");
		break;
	case ERROR_RING2SEG_MUST_BE_MOVABLE:
		return("Ring2seg must be movable");
		break;
	case ERROR_RELOC_CHAIN_XEEDS_SEGLIM:
		return("Reloc chain exceeds seglim");
		break;
	case ERROR_INFLOOP_IN_RELOC_CHAIN:
		return("Infloop in reloc chain");
		break;
	case ERROR_ENVVAR_NOT_FOUND:
		return("Env var not found");
		break;
	case ERROR_NO_SIGNAL_SENT:
		return("No signal sent");
		break;
	case ERROR_FILENAME_EXCED_RANGE:
		return("Filename exceeds range");
		break;
	case ERROR_RING2_STACK_IN_USE:
		return("Ring2 stack in use");
		break;
	case ERROR_META_EXPANSION_TOO_LONG:
		return("Meta expansion too long");
		break;
	case ERROR_INVALID_SIGNAL_NUMBER:
		return("Invalid signal number");
		break;
	case ERROR_THREAD_1_INACTIVE:
		return("Thread 1 inactive");
		break;
	case ERROR_LOCKED:
		return("Locked");
		break;
	case ERROR_TOO_MANY_MODULES:
		return("Too many modules");
		break;
	case ERROR_NESTING_NOT_ALLOWED:
		return("Nesting not allowed");
		break;
	case ERROR_EXE_MACHINE_TYPE_MISMATCH:
		return("Exe machine type mismatch");
		break;
	case ERROR_BAD_PIPE:
		return("Bad pipe");
		break;
	case ERROR_PIPE_BUSY:
		return("Pipe busy");
		break;
	case ERROR_NO_DATA:
		return("No data");
		break;
	case ERROR_PIPE_NOT_CONNECTED:
		return("Pipe not connected");
		break;
	case ERROR_MORE_DATA:
		return("More data");
		break;
	case ERROR_VC_DISCONNECTED:
		return("VC disconnected");
		break;
	case ERROR_INVALID_EA_NAME:
		return("Invalid EA name");
		break;
	case ERROR_EA_LIST_INCONSISTENT:
		return("EA list inconsistent");
		break;
	case WAIT_TIMEOUT:
		return("Wait timeout");
		break;
	case ERROR_NO_MORE_ITEMS:
		return("No more items");
		break;
	case ERROR_CANNOT_COPY:
		return("Cannot copy");
		break;
	case ERROR_DIRECTORY:
		return("Is a directory");
		break;
	case ERROR_EAS_DIDNT_FIT:
		return("EAS didnt fit");
		break;
	case ERROR_EA_FILE_CORRUPT:
		return("EA file corrupt");
		break;
	case ERROR_EA_TABLE_FULL:
		return("EA table full");
		break;
	case ERROR_INVALID_EA_HANDLE:
		return("Invalid EA handle");
		break;
	case ERROR_EAS_NOT_SUPPORTED:
		return("EAs not supported");
		break;
	case ERROR_NOT_OWNER:
		return("Not owner");
		break;
	case ERROR_TOO_MANY_POSTS:
		return("Too many posts");
		break;
	case ERROR_PARTIAL_COPY:
		return("Partial copy");
		break;
	case ERROR_OPLOCK_NOT_GRANTED:
		return("Oplock not granted");
		break;
	case ERROR_INVALID_OPLOCK_PROTOCOL:
		return("Invalid oplock protocol");
		break;
	case ERROR_DISK_TOO_FRAGMENTED:
		return("Disk too fragmented");
		break;
	case ERROR_DELETE_PENDING:
		return("Delete pending");
		break;
	case ERROR_MR_MID_NOT_FOUND:
		return("Mr Mid not found");
		break;
	case ERROR_INVALID_ADDRESS:
		return("Invalid address");
		break;
	case ERROR_ARITHMETIC_OVERFLOW:
		return("Arithmetic overflow");
		break;
	case ERROR_PIPE_CONNECTED:
		return("Pipe connected");
		break;
	case ERROR_PIPE_LISTENING:
		return("Pipe listening");
		break;
	case ERROR_EA_ACCESS_DENIED:
		return("EA access denied");
		break;
	case ERROR_OPERATION_ABORTED:
		return("Operation aborted");
		break;
	case ERROR_IO_INCOMPLETE:
		return("IO incomplete");
		break;
	case ERROR_IO_PENDING:
		return("IO pending");
		break;
	case ERROR_NOACCESS:
		return("No access");
		break;
	case ERROR_SWAPERROR:
		return("Swap error");
		break;
	case ERROR_STACK_OVERFLOW:
		return("Stack overflow");
		break;
	case ERROR_INVALID_MESSAGE:
		return("Invalid message");
		break;
	case ERROR_CAN_NOT_COMPLETE:
		return("Can not complete");
		break;
	case ERROR_INVALID_FLAGS:
		return("Invalid flags");
		break;
	case ERROR_UNRECOGNIZED_VOLUME:
		return("Unrecognised volume");
		break;
	case ERROR_FILE_INVALID:
		return("File invalid");
		break;
	case ERROR_FULLSCREEN_MODE:
		return("Full screen mode");
		break;
	case ERROR_NO_TOKEN:
		return("No token");
		break;
	case ERROR_BADDB:
		return("Bad DB");
		break;
	case ERROR_BADKEY:
		return("Bad key");
		break;
	case ERROR_CANTOPEN:
		return("Can't open");
		break;
	case ERROR_CANTREAD:
		return("Can't read");
		break;
	case ERROR_CANTWRITE:
		return("Can't write");
		break;
	case ERROR_REGISTRY_RECOVERED:
		return("Registry recovered");
		break;
	case ERROR_REGISTRY_CORRUPT:
		return("Registry corrupt");
		break;
	case ERROR_REGISTRY_IO_FAILED:
		return("Registry IO failed");
		break;
	case ERROR_NOT_REGISTRY_FILE:
		return("Not registry file");
		break;
	case ERROR_KEY_DELETED:
		return("Key deleted");
		break;
	case ERROR_NO_LOG_SPACE:
		return("No log space");
		break;
	case ERROR_KEY_HAS_CHILDREN:
		return("Key has children");
		break;
	case ERROR_CHILD_MUST_BE_VOLATILE:
		return("Child must be volatile");
		break;
	case ERROR_NOTIFY_ENUM_DIR:
		return("Notify enum dir");
		break;
	case ERROR_DEPENDENT_SERVICES_RUNNING:
		return("Dependent services running");
		break;
	case ERROR_INVALID_SERVICE_CONTROL:
		return("Invalid service control");
		break;
	case ERROR_SERVICE_REQUEST_TIMEOUT:
		return("Service request timeout");
		break;
	case ERROR_SERVICE_NO_THREAD:
		return("Service no thread");
		break;
	case ERROR_SERVICE_DATABASE_LOCKED:
		return("Service database locked");
		break;
	case ERROR_SERVICE_ALREADY_RUNNING:
		return("Service already running");
		break;
	case ERROR_INVALID_SERVICE_ACCOUNT:
		return("Invalid service account");
		break;
	case ERROR_SERVICE_DISABLED:
		return("Service disabled");
		break;
	case ERROR_CIRCULAR_DEPENDENCY:
		return("Circular dependency");
		break;
	case ERROR_SERVICE_DOES_NOT_EXIST:
		return("Service does not exist");
		break;
	case ERROR_SERVICE_CANNOT_ACCEPT_CTRL:
		return("Service cannot accept ctrl");
		break;
	case ERROR_SERVICE_NOT_ACTIVE:
		return("Service not active");
		break;
	case ERROR_FAILED_SERVICE_CONTROLLER_CONNECT:
		return("Failed service controller connect");
		break;
	case ERROR_EXCEPTION_IN_SERVICE:
		return("Exception in service");
		break;
	case ERROR_DATABASE_DOES_NOT_EXIST:
		return("Database does not exist");
		break;
	case ERROR_SERVICE_SPECIFIC_ERROR:
		return("Service specific error");
		break;
	case ERROR_PROCESS_ABORTED:
		return("Process aborted");
		break;
	case ERROR_SERVICE_DEPENDENCY_FAIL:
		return("Service dependency fail");
		break;
	case ERROR_SERVICE_LOGON_FAILED:
		return("Service logon failed");
		break;
	case ERROR_SERVICE_START_HANG:
		return("Service start hang");
		break;
	case ERROR_INVALID_SERVICE_LOCK:
		return("Invalid service lock");
		break;
	case ERROR_SERVICE_MARKED_FOR_DELETE:
		return("Service marked for delete");
		break;
	case ERROR_SERVICE_EXISTS:
		return("Service exists");
		break;
	case ERROR_ALREADY_RUNNING_LKG:
		return("Already running lkg");
		break;
	case ERROR_SERVICE_DEPENDENCY_DELETED:
		return("Service dependency deleted");
		break;
	case ERROR_BOOT_ALREADY_ACCEPTED:
		return("Boot already accepted");
		break;
	case ERROR_SERVICE_NEVER_STARTED:
		return("Service never started");
		break;
	case ERROR_DUPLICATE_SERVICE_NAME:
		return("Duplicate service name");
		break;
	case ERROR_DIFFERENT_SERVICE_ACCOUNT:
		return("Different service account");
		break;
	case ERROR_CANNOT_DETECT_DRIVER_FAILURE:
		return("Cannot detect driver failure");
		break;
	case ERROR_CANNOT_DETECT_PROCESS_ABORT:
		return("Cannot detect process abort");
		break;
	case ERROR_NO_RECOVERY_PROGRAM:
		return("No recovery program");
		break;
	case ERROR_SERVICE_NOT_IN_EXE:
		return("Service not in exe");
		break;
	case ERROR_NOT_SAFEBOOT_SERVICE:
		return("Not safeboot service");
		break;
	case ERROR_END_OF_MEDIA:
		return("End of media");
		break;
	case ERROR_FILEMARK_DETECTED:
		return("Filemark detected");
		break;
	case ERROR_BEGINNING_OF_MEDIA:
		return("Beginning of media");
		break;
	case ERROR_SETMARK_DETECTED:
		return("Setmark detected");
		break;
	case ERROR_NO_DATA_DETECTED:
		return("No data detected");
		break;
	case ERROR_PARTITION_FAILURE:
		return("Partition failure");
		break;
	case ERROR_INVALID_BLOCK_LENGTH:
		return("Invalid block length");
		break;
	case ERROR_DEVICE_NOT_PARTITIONED:
		return("Device not partitioned");
		break;
	case ERROR_UNABLE_TO_LOCK_MEDIA:
		return("Unable to lock media");
		break;
	case ERROR_UNABLE_TO_UNLOAD_MEDIA:
		return("Unable to unload media");
		break;
	case ERROR_MEDIA_CHANGED:
		return("Media changed");
		break;
	case ERROR_BUS_RESET:
		return("Bus reset");
		break;
	case ERROR_NO_MEDIA_IN_DRIVE:
		return("No media in drive");
		break;
	case ERROR_NO_UNICODE_TRANSLATION:
		return("No unicode translation");
		break;
	case ERROR_DLL_INIT_FAILED:
		return("DLL init failed");
		break;
	case ERROR_SHUTDOWN_IN_PROGRESS:
		return("Shutdown in progress");
		break;
	case ERROR_NO_SHUTDOWN_IN_PROGRESS:
		return("No shutdown in progress");
		break;
	case ERROR_IO_DEVICE:
		return("IO device");
		break;
	case ERROR_SERIAL_NO_DEVICE:
		return("Serial IO device");
		break;
	case ERROR_IRQ_BUSY:
		return("IRQ busy");
		break;
	case ERROR_MORE_WRITES:
		return("More writes");
		break;
	case ERROR_COUNTER_TIMEOUT:
		return("Counter timeout");
		break;
	case ERROR_FLOPPY_ID_MARK_NOT_FOUND:
		return("Floppy ID mark not found");
		break;
	case ERROR_FLOPPY_WRONG_CYLINDER:
		return("Floppy wrong cylinder");
		break;
	case ERROR_FLOPPY_UNKNOWN_ERROR:
		return("Floppy unknown error");
		break;
	case ERROR_FLOPPY_BAD_REGISTERS:
		return("Floppy bad registers");
		break;
	case ERROR_DISK_RECALIBRATE_FAILED:
		return("Disk recalibrate failed");
		break;
	case ERROR_DISK_OPERATION_FAILED:
		return("Disk operation failed");
		break;
	case ERROR_DISK_RESET_FAILED:
		return("Disk reset failed");
		break;
	case ERROR_EOM_OVERFLOW:
		return("EOM overflow");
		break;
	case ERROR_NOT_ENOUGH_SERVER_MEMORY:
		return("Not enough server memory");
		break;
	case ERROR_POSSIBLE_DEADLOCK:
		return("Possible deadlock");
		break;
	case ERROR_MAPPED_ALIGNMENT:
		return("Mapped alignment");
		break;
	case ERROR_SET_POWER_STATE_VETOED:
		return("Set power state vetoed");
		break;
	case ERROR_SET_POWER_STATE_FAILED:
		return("Set power state failed");
		break;
	case ERROR_TOO_MANY_LINKS:
		return("Too many links");
		break;
	case ERROR_OLD_WIN_VERSION:
		return("Old win version");
		break;
	case ERROR_APP_WRONG_OS:
		return("App wrong OS");
		break;
	case ERROR_SINGLE_INSTANCE_APP:
		return("Single instance app");
		break;
	case ERROR_RMODE_APP:
		return("Rmode app");
		break;
	case ERROR_INVALID_DLL:
		return("Invalid DLL");
		break;
	case ERROR_NO_ASSOCIATION:
		return("No association");
		break;
	case ERROR_DDE_FAIL:
		return("DDE fail");
		break;
	case ERROR_DLL_NOT_FOUND:
		return("DLL not found");
		break;
	case ERROR_NO_MORE_USER_HANDLES:
		return("No more user handles");
		break;
	case ERROR_MESSAGE_SYNC_ONLY:
		return("Message sync only");
		break;
	case ERROR_SOURCE_ELEMENT_EMPTY:
		return("Source element empty");
		break;
	case ERROR_DESTINATION_ELEMENT_FULL:
		return("Destination element full");
		break;
	case ERROR_ILLEGAL_ELEMENT_ADDRESS:
		return("Illegal element address");
		break;
	case ERROR_MAGAZINE_NOT_PRESENT:
		return("Magazine not present");
		break;
	case ERROR_DEVICE_REINITIALIZATION_NEEDED:
		return("Device reinitialization needed");
		break;
	case ERROR_DEVICE_REQUIRES_CLEANING:
		return("Device requires cleaning");
		break;
	case ERROR_DEVICE_DOOR_OPEN:
		return("Device door open");
		break;
	case ERROR_DEVICE_NOT_CONNECTED:
		return("Device not connected");
		break;
	case ERROR_NOT_FOUND:
		return("Not found");
		break;
	case ERROR_NO_MATCH:
		return("No match");
		break;
	case ERROR_SET_NOT_FOUND:
		return("Set not found");
		break;
	case ERROR_POINT_NOT_FOUND:
		return("Point not found");
		break;
	case ERROR_NO_TRACKING_SERVICE:
		return("No tracking service");
		break;
	case ERROR_NO_VOLUME_ID:
		return("No volume ID");
		break;
	case ERROR_UNABLE_TO_REMOVE_REPLACED:
		return("Unable to remove replaced");
		break;
	case ERROR_UNABLE_TO_MOVE_REPLACEMENT:
		return("Unable to move replacement");
		break;
	case ERROR_UNABLE_TO_MOVE_REPLACEMENT_2:
		return("Unable to move replacement 2");
		break;
	case ERROR_JOURNAL_DELETE_IN_PROGRESS:
		return("Journal delete in progress");
		break;
	case ERROR_JOURNAL_NOT_ACTIVE:
		return("Journal not active");
		break;
	case ERROR_POTENTIAL_FILE_FOUND:
		return("Potential file found");
		break;
	case ERROR_JOURNAL_ENTRY_DELETED:
		return("Journal entry deleted");
		break;
	case ERROR_BAD_DEVICE:
		return("Bad device");
		break;
	case ERROR_CONNECTION_UNAVAIL:
		return("Connection unavail");
		break;
	case ERROR_DEVICE_ALREADY_REMEMBERED:
		return("Device already remembered");
		break;
	case ERROR_NO_NET_OR_BAD_PATH:
		return("No net or bad path");
		break;
	case ERROR_BAD_PROVIDER:
		return("Bad provider");
		break;
	case ERROR_CANNOT_OPEN_PROFILE:
		return("Cannot open profile");
		break;
	case ERROR_BAD_PROFILE:
		return("Bad profile");
		break;
	case ERROR_NOT_CONTAINER:
		return("Not container");
		break;
	case ERROR_EXTENDED_ERROR:
		return("Extended error");
		break;
	case ERROR_INVALID_GROUPNAME:
		return("Invalid group name");
		break;
	case ERROR_INVALID_COMPUTERNAME:
		return("Invalid computer name");
		break;
	case ERROR_INVALID_EVENTNAME:
		return("Invalid event name");
		break;
	case ERROR_INVALID_DOMAINNAME:
		return("Invalid domain name");
		break;
	case ERROR_INVALID_SERVICENAME:
		return("Invalid service name");
		break;
	case ERROR_INVALID_NETNAME:
		return("Invalid net name");
		break;
	case ERROR_INVALID_SHARENAME:
		return("Invalid share name");
		break;
	case ERROR_INVALID_PASSWORDNAME:
		return("Invalid password name");
		break;
	case ERROR_INVALID_MESSAGENAME:
		return("Invalid message name");
		break;
	case ERROR_INVALID_MESSAGEDEST:
		return("Invalid message dest");
		break;
	case ERROR_SESSION_CREDENTIAL_CONFLICT:
		return("Session credential conflict");
		break;
	case ERROR_REMOTE_SESSION_LIMIT_EXCEEDED:
		return("Remote session limit exceeded");
		break;
	case ERROR_DUP_DOMAINNAME:
		return("Dup domain name");
		break;
	case ERROR_NO_NETWORK:
		return("No network");
		break;
	case ERROR_CANCELLED:
		return("Cancelled");
		break;
	case ERROR_USER_MAPPED_FILE:
		return("User mapped file");
		break;
	case ERROR_CONNECTION_REFUSED:
		return("Connection refused");
		break;
	case ERROR_GRACEFUL_DISCONNECT:
		return("Graceful disconnect");
		break;
	case ERROR_ADDRESS_ALREADY_ASSOCIATED:
		return("Address already associated");
		break;
	case ERROR_ADDRESS_NOT_ASSOCIATED:
		return("Address not associated");
		break;
	case ERROR_CONNECTION_INVALID:
		return("Connected invalid");
		break;
	case ERROR_CONNECTION_ACTIVE:
		return("Connection active");
		break;
	case ERROR_NETWORK_UNREACHABLE:
		return("Network unreachable");
		break;
	case ERROR_HOST_UNREACHABLE:
		return("Host unreachable");
		break;
	case ERROR_PROTOCOL_UNREACHABLE:
		return("Protocol unreachable");
		break;
	case ERROR_PORT_UNREACHABLE:
		return("Port unreachable");
		break;
	case ERROR_REQUEST_ABORTED:
		return("Request aborted");
		break;
	case ERROR_CONNECTION_ABORTED:
		return("Connection aborted");
		break;
	case ERROR_RETRY:
		return("Retry");
		break;
	case ERROR_CONNECTION_COUNT_LIMIT:
		return("Connection count limit");
		break;
	case ERROR_LOGIN_TIME_RESTRICTION:
		return("Login time restriction");
		break;
	case ERROR_LOGIN_WKSTA_RESTRICTION:
		return("Login wksta restriction");
		break;
	case ERROR_INCORRECT_ADDRESS:
		return("Incorrect address");
		break;
	case ERROR_ALREADY_REGISTERED:
		return("Already registered");
		break;
	case ERROR_SERVICE_NOT_FOUND:
		return("Service not found");
		break;
	case ERROR_NOT_AUTHENTICATED:
		return("Not authenticated");
		break;
	case ERROR_NOT_LOGGED_ON:
		return("Not logged on");
		break;
	case ERROR_CONTINUE:
		return("Continue");
		break;
	case ERROR_ALREADY_INITIALIZED:
		return("Already initialised");
		break;
	case ERROR_NO_MORE_DEVICES:
		return("No more devices");
		break;
	case ERROR_NO_SUCH_SITE:
		return("No such site");
		break;
	case ERROR_DOMAIN_CONTROLLER_EXISTS:
		return("Domain controller exists");
		break;
	case ERROR_ONLY_IF_CONNECTED:
		return("Only if connected");
		break;
	case ERROR_OVERRIDE_NOCHANGES:
		return("Override no changes");
		break;
	case ERROR_BAD_USER_PROFILE:
		return("Bad user profile");
		break;
	case ERROR_NOT_SUPPORTED_ON_SBS:
		return("Not supported on SBS");
		break;
	case ERROR_SERVER_SHUTDOWN_IN_PROGRESS:
		return("Server shutdown in progress");
		break;
	case ERROR_HOST_DOWN:
		return("Host down");
		break;
	case ERROR_NON_ACCOUNT_SID:
		return("Non account sid");
		break;
	case ERROR_NON_DOMAIN_SID:
		return("Non domain sid");
		break;
	case ERROR_APPHELP_BLOCK:
		return("Apphelp block");
		break;
	case ERROR_ACCESS_DISABLED_BY_POLICY:
		return("Access disabled by policy");
		break;
	case ERROR_REG_NAT_CONSUMPTION:
		return("Reg nat consumption");
		break;
	case ERROR_CSCSHARE_OFFLINE:
		return("CSC share offline");
		break;
	case ERROR_PKINIT_FAILURE:
		return("PK init failure");
		break;
	case ERROR_SMARTCARD_SUBSYSTEM_FAILURE:
		return("Smartcard subsystem failure");
		break;
	case ERROR_DOWNGRADE_DETECTED:
		return("Downgrade detected");
		break;
	case SEC_E_SMARTCARD_CERT_REVOKED:
		return("Smartcard cert revoked");
		break;
	case SEC_E_ISSUING_CA_UNTRUSTED:
		return("Issuing CA untrusted");
		break;
	case SEC_E_REVOCATION_OFFLINE_C:
		return("Revocation offline");
		break;
	case SEC_E_PKINIT_CLIENT_FAILUR:
		return("PK init client failure");
		break;
	case SEC_E_SMARTCARD_CERT_EXPIRED:
		return("Smartcard cert expired");
		break;
	case ERROR_MACHINE_LOCKED:
		return("Machine locked");
		break;
	case ERROR_CALLBACK_SUPPLIED_INVALID_DATA:
		return("Callback supplied invalid data");
		break;
	case ERROR_SYNC_FOREGROUND_REFRESH_REQUIRED:
		return("Sync foreground refresh required");
		break;
	case ERROR_DRIVER_BLOCKED:
		return("Driver blocked");
		break;
	case ERROR_INVALID_IMPORT_OF_NON_DLL:
		return("Invalid import of non DLL");
		break;
	case ERROR_NOT_ALL_ASSIGNED:
		return("Not all assigned");
		break;
	case ERROR_SOME_NOT_MAPPED:
		return("Some not mapped");
		break;
	case ERROR_NO_QUOTAS_FOR_ACCOUNT:
		return("No quotas for account");
		break;
	case ERROR_LOCAL_USER_SESSION_KEY:
		return("Local user session key");
		break;
	case ERROR_NULL_LM_PASSWORD:
		return("Null LM password");
		break;
	case ERROR_UNKNOWN_REVISION:
		return("Unknown revision");
		break;
	case ERROR_REVISION_MISMATCH:
		return("Revision mismatch");
		break;
	case ERROR_INVALID_OWNER:
		return("Invalid owner");
		break;
	case ERROR_INVALID_PRIMARY_GROUP:
		return("Invalid primary group");
		break;
	case ERROR_NO_IMPERSONATION_TOKEN:
		return("No impersonation token");
		break;
	case ERROR_CANT_DISABLE_MANDATORY:
		return("Can't disable mandatory");
		break;
	case ERROR_NO_LOGON_SERVERS:
		return("No logon servers");
		break;
	case ERROR_NO_SUCH_LOGON_SESSION:
		return("No such logon session");
		break;
	case ERROR_NO_SUCH_PRIVILEGE:
		return("No such privilege");
		break;
	case ERROR_PRIVILEGE_NOT_HELD:
		return("Privilege not held");
		break;
	case ERROR_INVALID_ACCOUNT_NAME:
		return("Invalid account name");
		break;
	case ERROR_USER_EXISTS:
		return("User exists");
		break;
	case ERROR_NO_SUCH_USER:
		return("No such user");
		break;
	case ERROR_GROUP_EXISTS:
		return("Group exists");
		break;
	case ERROR_NO_SUCH_GROUP:
		return("No such group");
		break;
	case ERROR_MEMBER_IN_GROUP:
		return("Member in group");
		break;
	case ERROR_MEMBER_NOT_IN_GROUP:
		return("Member not in group");
		break;
	case ERROR_LAST_ADMIN:
		return("Last admin");
		break;
	case ERROR_WRONG_PASSWORD:
		return("Wrong password");
		break;
	case ERROR_ILL_FORMED_PASSWORD:
		return("Ill formed password");
		break;
	case ERROR_PASSWORD_RESTRICTION:
		return("Password restriction");
		break;
	case ERROR_LOGON_FAILURE:
		return("Logon failure");
		break;
	case ERROR_ACCOUNT_RESTRICTION:
		return("Account restriction");
		break;
	case ERROR_INVALID_LOGON_HOURS:
		return("Invalid logon hours");
		break;
	case ERROR_INVALID_WORKSTATION:
		return("Invalid workstation");
		break;
	case ERROR_PASSWORD_EXPIRED:
		return("Password expired");
		break;
	case ERROR_ACCOUNT_DISABLED:
		return("Account disabled");
		break;
	case ERROR_NONE_MAPPED:
		return("None mapped");
		break;
	case ERROR_TOO_MANY_LUIDS_REQUESTED:
		return("Too many LUIDs requested");
		break;
	case ERROR_LUIDS_EXHAUSTED:
		return("LUIDs exhausted");
		break;
	case ERROR_INVALID_SUB_AUTHORITY:
		return("Invalid sub authority");
		break;
	case ERROR_INVALID_ACL:
		return("Invalid ACL");
		break;
	case ERROR_INVALID_SID:
		return("Invalid SID");
		break;
	case ERROR_INVALID_SECURITY_DESCR:
		return("Invalid security descr");
		break;
	case ERROR_BAD_INHERITANCE_ACL:
		return("Bad inheritance ACL");
		break;
	case ERROR_SERVER_DISABLED:
		return("Server disabled");
		break;
	case ERROR_SERVER_NOT_DISABLED:
		return("Server not disabled");
		break;
	case ERROR_INVALID_ID_AUTHORITY:
		return("Invalid ID authority");
		break;
	case ERROR_ALLOTTED_SPACE_EXCEEDED:
		return("Allotted space exceeded");
		break;
	case ERROR_INVALID_GROUP_ATTRIBUTES:
		return("Invalid group attributes");
		break;
	case ERROR_BAD_IMPERSONATION_LEVEL:
		return("Bad impersonation level");
		break;
	case ERROR_CANT_OPEN_ANONYMOUS:
		return("Can't open anonymous");
		break;
	case ERROR_BAD_VALIDATION_CLASS:
		return("Bad validation class");
		break;
	case ERROR_BAD_TOKEN_TYPE:
		return("Bad token type");
		break;
	case ERROR_NO_SECURITY_ON_OBJECT:
		return("No security on object");
		break;
	case ERROR_CANT_ACCESS_DOMAIN_INFO:
		return("Can't access domain info");
		break;
	case ERROR_INVALID_SERVER_STATE:
		return("Invalid server state");
		break;
	case ERROR_INVALID_DOMAIN_STATE:
		return("Invalid domain state");
		break;
	case ERROR_INVALID_DOMAIN_ROLE:
		return("Invalid domain role");
		break;
	case ERROR_NO_SUCH_DOMAIN:
		return("No such domain");
		break;
	case ERROR_DOMAIN_EXISTS:
		return("Domain exists");
		break;
	case ERROR_DOMAIN_LIMIT_EXCEEDED:
		return("Domain limit exceeded");
		break;
	case ERROR_INTERNAL_DB_CORRUPTION:
		return("Internal DB corruption");
		break;
	case ERROR_INTERNAL_ERROR:
		return("Internal error");
		break;
	case ERROR_GENERIC_NOT_MAPPED:
		return("Generic not mapped");
		break;
	case ERROR_BAD_DESCRIPTOR_FORMAT:
		return("Bad descriptor format");
		break;
	case ERROR_NOT_LOGON_PROCESS:
		return("Not logon process");
		break;
	case ERROR_LOGON_SESSION_EXISTS:
		return("Logon session exists");
		break;
	case ERROR_NO_SUCH_PACKAGE:
		return("No such package");
		break;
	case ERROR_BAD_LOGON_SESSION_STATE:
		return("Bad logon session state");
		break;
	case ERROR_LOGON_SESSION_COLLISION:
		return("Logon session collision");
		break;
	case ERROR_INVALID_LOGON_TYPE:
		return("Invalid logon type");
		break;
	case ERROR_CANNOT_IMPERSONATE:
		return("Cannot impersonate");
		break;
	case ERROR_RXACT_INVALID_STATE:
		return("Rxact invalid state");
		break;
	case ERROR_RXACT_COMMIT_FAILURE:
		return("Rxact commit failure");
		break;
	case ERROR_SPECIAL_ACCOUNT:
		return("Special account");
		break;
	case ERROR_SPECIAL_GROUP:
		return("Special group");
		break;
	case ERROR_SPECIAL_USER:
		return("Special user");
		break;
	case ERROR_MEMBERS_PRIMARY_GROUP:
		return("Members primary group");
		break;
	case ERROR_TOKEN_ALREADY_IN_USE:
		return("Token already in use");
		break;
	case ERROR_NO_SUCH_ALIAS:
		return("No such alias");
		break;
	case ERROR_MEMBER_NOT_IN_ALIAS:
		return("Member not in alias");
		break;
	case ERROR_MEMBER_IN_ALIAS:
		return("Member in alias");
		break;
	case ERROR_ALIAS_EXISTS:
		return("Alias exists");
		break;
	case ERROR_LOGON_NOT_GRANTED:
		return("Logon not granted");
		break;
	case ERROR_TOO_MANY_SECRETS:
		return("Too many secrets");
		break;
	case ERROR_SECRET_TOO_LONG:
		return("Secret too long");
		break;
	case ERROR_INTERNAL_DB_ERROR:
		return("Internal DB error");
		break;
	case ERROR_TOO_MANY_CONTEXT_IDS:
		return("Too many context IDs");
		break;
	case ERROR_LOGON_TYPE_NOT_GRANTED:
		return("Logon type not granted");
		break;
	case ERROR_NT_CROSS_ENCRYPTION_REQUIRED:
		return("NT cross encryption required");
		break;
	case ERROR_NO_SUCH_MEMBER:
		return("No such member");
		break;
	case ERROR_INVALID_MEMBER:
		return("Invalid member");
		break;
	case ERROR_TOO_MANY_SIDS:
		return("Too many SIDs");
		break;
	case ERROR_LM_CROSS_ENCRYPTION_REQUIRED:
		return("LM cross encryption required");
		break;
	case ERROR_NO_INHERITANCE:
		return("No inheritance");
		break;
	case ERROR_FILE_CORRUPT:
		return("File corrupt");
		break;
	case ERROR_DISK_CORRUPT:
		return("Disk corrupt");
		break;
	case ERROR_NO_USER_SESSION_KEY:
		return("No user session key");
		break;
	case ERROR_LICENSE_QUOTA_EXCEEDED:
		return("Licence quota exceeded");
		break;
	case ERROR_WRONG_TARGET_NAME:
		return("Wrong target name");
		break;
	case ERROR_MUTUAL_AUTH_FAILED:
		return("Mutual auth failed");
		break;
	case ERROR_TIME_SKEW:
		return("Time skew");
		break;
	case ERROR_CURRENT_DOMAIN_NOT_ALLOWED:
		return("Current domain not allowed");
		break;
	case ERROR_INVALID_WINDOW_HANDLE:
		return("Invalid window handle");
		break;
	case ERROR_INVALID_MENU_HANDLE:
		return("Invalid menu handle");
		break;
	case ERROR_INVALID_CURSOR_HANDLE:
		return("Invalid cursor handle");
		break;
	case ERROR_INVALID_ACCEL_HANDLE:
		return("Invalid accel handle");
		break;
	case ERROR_INVALID_HOOK_HANDLE:
		return("Invalid hook handle");
		break;
	case ERROR_INVALID_DWP_HANDLE:
		return("Invalid DWP handle");
		break;
	case ERROR_TLW_WITH_WSCHILD:
		return("TLW with wschild");
		break;
	case ERROR_CANNOT_FIND_WND_CLASS:
		return("Cannot find WND class");
		break;
	case ERROR_WINDOW_OF_OTHER_THREAD:
		return("Window of other thread");
		break;
	case ERROR_HOTKEY_ALREADY_REGISTERED:
		return("Hotkey already registered");
		break;
	case ERROR_CLASS_ALREADY_EXISTS:
		return("Class already exists");
		break;
	case ERROR_CLASS_DOES_NOT_EXIST:
		return("Class does not exist");
		break;
	case ERROR_CLASS_HAS_WINDOWS:
		return("Class has windows");
		break;
	case ERROR_INVALID_INDEX:
		return("Invalid index");
		break;
	case ERROR_INVALID_ICON_HANDLE:
		return("Invalid icon handle");
		break;
	case ERROR_PRIVATE_DIALOG_INDEX:
		return("Private dialog index");
		break;
	case ERROR_LISTBOX_ID_NOT_FOUND:
		return("Listbox ID not found");
		break;
	case ERROR_NO_WILDCARD_CHARACTERS:
		return("No wildcard characters");
		break;
	case ERROR_CLIPBOARD_NOT_OPEN:
		return("Clipboard not open");
		break;
	case ERROR_HOTKEY_NOT_REGISTERED:
		return("Hotkey not registered");
		break;
	case ERROR_WINDOW_NOT_DIALOG:
		return("Window not dialog");
		break;
	case ERROR_CONTROL_ID_NOT_FOUND:
		return("Control ID not found");
		break;
	case ERROR_INVALID_COMBOBOX_MESSAGE:
		return("Invalid combobox message");
		break;
	case ERROR_WINDOW_NOT_COMBOBOX:
		return("Window not combobox");
		break;
	case ERROR_INVALID_EDIT_HEIGHT:
		return("Invalid edit height");
		break;
	case ERROR_DC_NOT_FOUND:
		return("DC not found");
		break;
	case ERROR_INVALID_HOOK_FILTER:
		return("Invalid hook filter");
		break;
	case ERROR_INVALID_FILTER_PROC:
		return("Invalid filter proc");
		break;
	case ERROR_HOOK_NEEDS_HMOD:
		return("Hook needs HMOD");
		break;
	case ERROR_GLOBAL_ONLY_HOOK:
		return("Global only hook");
		break;
	case ERROR_JOURNAL_HOOK_SET:
		return("Journal hook set");
		break;
	case ERROR_HOOK_NOT_INSTALLED:
		return("Hook not installed");
		break;
	case ERROR_INVALID_LB_MESSAGE:
		return("Invalid LB message");
		break;
	case ERROR_SETCOUNT_ON_BAD_LB:
		return("Setcount on bad LB");
		break;
	case ERROR_LB_WITHOUT_TABSTOPS:
		return("LB without tabstops");
		break;
	case ERROR_DESTROY_OBJECT_OF_OTHER_THREAD:
		return("Destroy object of other thread");
		break;
	case ERROR_CHILD_WINDOW_MENU:
		return("Child window menu");
		break;
	case ERROR_NO_SYSTEM_MENU:
		return("No system menu");
		break;
	case ERROR_INVALID_MSGBOX_STYLE:
		return("Invalid msgbox style");
		break;
	case ERROR_INVALID_SPI_VALUE:
		return("Invalid SPI value");
		break;
	case ERROR_SCREEN_ALREADY_LOCKED:
		return("Screen already locked");
		break;
	case ERROR_HWNDS_HAVE_DIFF_PARENT:
		return("HWNDs have different parent");
		break;
	case ERROR_NOT_CHILD_WINDOW:
		return("Not child window");
		break;
	case ERROR_INVALID_GW_COMMAND:
		return("Invalid GW command");
		break;
	case ERROR_INVALID_THREAD_ID:
		return("Invalid thread ID");
		break;
	case ERROR_NON_MDICHILD_WINDOW:
		return("Non MDI child window");
		break;
	case ERROR_POPUP_ALREADY_ACTIVE:
		return("Popup already active");
		break;
	case ERROR_NO_SCROLLBARS:
		return("No scrollbars");
		break;
	case ERROR_INVALID_SCROLLBAR_RANGE:
		return("Invalid scrollbar range");
		break;
	case ERROR_INVALID_SHOWWIN_COMMAND:
		return("Invalid showwin command");
		break;
	case ERROR_NO_SYSTEM_RESOURCES:
		return("No system resources");
		break;
	case ERROR_NONPAGED_SYSTEM_RESOURCES:
		return("Nonpaged system resources");
		break;
	case ERROR_PAGED_SYSTEM_RESOURCES:
		return("Paged system resources");
		break;
	case ERROR_WORKING_SET_QUOTA:
		return("Working set quota");
		break;
	case ERROR_PAGEFILE_QUOTA:
		return("Pagefile quota");
		break;
	case ERROR_COMMITMENT_LIMIT:
		return("Commitment limit");
		break;
	case ERROR_MENU_ITEM_NOT_FOUND:
		return("Menu item not found");
		break;
	case ERROR_INVALID_KEYBOARD_HANDLE:
		return("Invalid keyboard handle");
		break;
	case ERROR_HOOK_TYPE_NOT_ALLOWED:
		return("Hook type not allowed");
		break;
	case ERROR_REQUIRES_INTERACTIVE_WINDOWSTATION:
		return("Requires interactive windowstation");
		break;
	case ERROR_TIMEOUT:
		return("Timeout");
		break;
	case ERROR_INVALID_MONITOR_HANDLE:
		return("Invalid monitor handle");
		break;
	case ERROR_EVENTLOG_FILE_CORRUPT:
		return("Eventlog file corrupt");
		break;
	case ERROR_EVENTLOG_CANT_START:
		return("Eventlog can't start");
		break;
	case ERROR_LOG_FILE_FULL:
		return("Log file full");
		break;
	case ERROR_EVENTLOG_FILE_CHANGED:
		return("Eventlog file changed");
		break;
	case ERROR_INSTALL_SERVICE_FAILURE:
		return("Install service failure");
		break;
	case ERROR_INSTALL_USEREXIT:
		return("Install userexit");
		break;
	case ERROR_INSTALL_FAILURE:
		return("Install failure");
		break;
	case ERROR_INSTALL_SUSPEND:
		return("Install suspend");
		break;
	case ERROR_UNKNOWN_PRODUCT:
		return("Unknown product");
		break;
	case ERROR_UNKNOWN_FEATURE:
		return("Unknown feature");
		break;
	case ERROR_UNKNOWN_COMPONENT:
		return("Unknown component");
		break;
	case ERROR_UNKNOWN_PROPERTY:
		return("Unknown property");
		break;
	case ERROR_INVALID_HANDLE_STATE:
		return("Invalid handle state");
		break;
	case ERROR_BAD_CONFIGURATION:
		return("Bad configuration");
		break;
	case ERROR_INDEX_ABSENT:
		return("Index absent");
		break;
	case ERROR_INSTALL_SOURCE_ABSENT:
		return("Install source absent");
		break;
	case ERROR_INSTALL_PACKAGE_VERSION:
		return("Install package version");
		break;
	case ERROR_PRODUCT_UNINSTALLED:
		return("Product uninstalled");
		break;
	case ERROR_BAD_QUERY_SYNTAX:
		return("Bad query syntax");
		break;
	case ERROR_INVALID_FIELD:
		return("Invalid field");
		break;
	case ERROR_DEVICE_REMOVED:
		return("Device removed");
		break;
	case ERROR_INSTALL_ALREADY_RUNNING:
		return("Install already running");
		break;
	case ERROR_INSTALL_PACKAGE_OPEN_FAILED:
		return("Install package open failed");
		break;
	case ERROR_INSTALL_PACKAGE_INVALID:
		return("Install package invalid");
		break;
	case ERROR_INSTALL_UI_FAILURE:
		return("Install UI failure");
		break;
	case ERROR_INSTALL_LOG_FAILURE:
		return("Install log failure");
		break;
	case ERROR_INSTALL_LANGUAGE_UNSUPPORTED:
		return("Install language unsupported");
		break;
	case ERROR_INSTALL_TRANSFORM_FAILURE:
		return("Install transform failure");
		break;
	case ERROR_INSTALL_PACKAGE_REJECTED:
		return("Install package rejected");
		break;
	case ERROR_FUNCTION_NOT_CALLED:
		return("Function not called");
		break;
	case ERROR_FUNCTION_FAILED:
		return("Function failed");
		break;
	case ERROR_INVALID_TABLE:
		return("Invalid table");
		break;
	case ERROR_DATATYPE_MISMATCH:
		return("Datatype mismatch");
		break;
	case ERROR_UNSUPPORTED_TYPE:
		return("Unsupported type");
		break;
	case ERROR_CREATE_FAILED:
		return("Create failed");
		break;
	case ERROR_INSTALL_TEMP_UNWRITABLE:
		return("Install temp unwritable");
		break;
	case ERROR_INSTALL_PLATFORM_UNSUPPORTED:
		return("Install platform unsupported");
		break;
	case ERROR_INSTALL_NOTUSED:
		return("Install notused");
		break;
	case ERROR_PATCH_PACKAGE_OPEN_FAILED:
		return("Patch package open failed");
		break;
	case ERROR_PATCH_PACKAGE_INVALID:
		return("Patch package invalid");
		break;
	case ERROR_PATCH_PACKAGE_UNSUPPORTED:
		return("Patch package unsupported");
		break;
	case ERROR_PRODUCT_VERSION:
		return("Product version");
		break;
	case ERROR_INVALID_COMMAND_LINE:
		return("Invalid command line");
		break;
	case ERROR_INSTALL_REMOTE_DISALLOWED:
		return("Install remote disallowed");
		break;
	case ERROR_SUCCESS_REBOOT_INITIATED:
		return("Success reboot initiated");
		break;
	case ERROR_PATCH_TARGET_NOT_FOUND:
		return("Patch target not found");
		break;
	case ERROR_PATCH_PACKAGE_REJECTED:
		return("Patch package rejected");
		break;
	case ERROR_INSTALL_TRANSFORM_REJECTED:
		return("Install transform rejected");
		break;
	case RPC_S_INVALID_STRING_BINDING:
		return("RPC S Invalid string binding");
		break;
	case RPC_S_WRONG_KIND_OF_BINDING:
		return("RPC S Wrong kind of binding");
		break;
	case RPC_S_INVALID_BINDING:
		return("RPC S Invalid binding");
		break;
	case RPC_S_PROTSEQ_NOT_SUPPORTED:
		return("RPC S Protseq not supported");
		break;
	case RPC_S_INVALID_RPC_PROTSEQ:
		return("RPC S Invalid RPC protseq");
		break;
	case RPC_S_INVALID_STRING_UUID:
		return("RPC S Invalid string UUID");
		break;
	case RPC_S_INVALID_ENDPOINT_FORMAT:
		return("RPC S Invalid endpoint format");
		break;
	case RPC_S_INVALID_NET_ADDR:
		return("RPC S Invalid net addr");
		break;
	case RPC_S_NO_ENDPOINT_FOUND:
		return("RPC S No endpoint found");
		break;
	case RPC_S_INVALID_TIMEOUT:
		return("RPC S Invalid timeout");
		break;
	case RPC_S_OBJECT_NOT_FOUND:
		return("RPC S Object not found");
		break;
	case RPC_S_ALREADY_REGISTERED:
		return("RPC S Already registered");
		break;
	case RPC_S_TYPE_ALREADY_REGISTERED:
		return("RPC S Type already registered");
		break;
	case RPC_S_ALREADY_LISTENING:
		return("RPC S Already listening");
		break;
	case RPC_S_NO_PROTSEQS_REGISTERED:
		return("RPC S Not protseqs registered");
		break;
	case RPC_S_NOT_LISTENING:
		return("RPC S Not listening");
		break;
	case RPC_S_UNKNOWN_MGR_TYPE:
		return("RPC S Unknown mgr type");
		break;
	case RPC_S_UNKNOWN_IF:
		return("RPC S Unknown IF");
		break;
	case RPC_S_NO_BINDINGS:
		return("RPC S No bindings");
		break;
	case RPC_S_NO_PROTSEQS:
		return("RPC S Not protseqs");
		break;
	case RPC_S_CANT_CREATE_ENDPOINT:
		return("RPC S Can't create endpoint");
		break;
	case RPC_S_OUT_OF_RESOURCES:
		return("RPC S Out of resources");
		break;
	case RPC_S_SERVER_UNAVAILABLE:
		return("RPC S Server unavailable");
		break;
	case RPC_S_SERVER_TOO_BUSY:
		return("RPC S Server too busy");
		break;
	case RPC_S_INVALID_NETWORK_OPTIONS:
		return("RPC S Invalid network options");
		break;
	case RPC_S_NO_CALL_ACTIVE:
		return("RPC S No call active");
		break;
	case RPC_S_CALL_FAILED:
		return("RPC S Call failed");
		break;
	case RPC_S_CALL_FAILED_DNE:
		return("RPC S Call failed DNE");
		break;
	case RPC_S_PROTOCOL_ERROR:
		return("RPC S Protocol error");
		break;
	case RPC_S_UNSUPPORTED_TRANS_SYN:
		return("RPC S Unsupported trans syn");
		break;
	case RPC_S_UNSUPPORTED_TYPE:
		return("RPC S Unsupported type");
		break;
	case RPC_S_INVALID_TAG:
		return("RPC S Invalid tag");
		break;
	case RPC_S_INVALID_BOUND:
		return("RPC S Invalid bound");
		break;
	case RPC_S_NO_ENTRY_NAME:
		return("RPC S No entry name");
		break;
	case RPC_S_INVALID_NAME_SYNTAX:
		return("RPC S Invalid name syntax");
		break;
	case RPC_S_UNSUPPORTED_NAME_SYNTAX:
		return("RPC S Unsupported name syntax");
		break;
	case RPC_S_UUID_NO_ADDRESS:
		return("RPC S UUID no address");
		break;
	case RPC_S_DUPLICATE_ENDPOINT:
		return("RPC S Duplicate endpoint");
		break;
	case RPC_S_UNKNOWN_AUTHN_TYPE:
		return("RPC S Unknown authn type");
		break;
	case RPC_S_MAX_CALLS_TOO_SMALL:
		return("RPC S Max calls too small");
		break;
	case RPC_S_STRING_TOO_LONG:
		return("RPC S String too long");
		break;
	case RPC_S_PROTSEQ_NOT_FOUND:
		return("RPC S Protseq not found");
		break;
	case RPC_S_PROCNUM_OUT_OF_RANGE:
		return("RPC S Procnum out of range");
		break;
	case RPC_S_BINDING_HAS_NO_AUTH:
		return("RPC S Binding has no auth");
		break;
	case RPC_S_UNKNOWN_AUTHN_SERVICE:
		return("RPC S Unknown authn service");
		break;
	case RPC_S_UNKNOWN_AUTHN_LEVEL:
		return("RPC S Unknown authn level");
		break;
	case RPC_S_INVALID_AUTH_IDENTITY:
		return("RPC S Invalid auth identity");
		break;
	case RPC_S_UNKNOWN_AUTHZ_SERVICE:
		return("RPC S Unknown authz service");
		break;
	case EPT_S_INVALID_ENTRY:
		return("EPT S Invalid entry");
		break;
	case EPT_S_CANT_PERFORM_OP:
		return("EPT S Can't perform op");
		break;
	case EPT_S_NOT_REGISTERED:
		return("EPT S Not registered");
		break;
	case RPC_S_NOTHING_TO_EXPORT:
		return("RPC S Nothing to export");
		break;
	case RPC_S_INCOMPLETE_NAME:
		return("RPC S Incomplete name");
		break;
	case RPC_S_INVALID_VERS_OPTION:
		return("RPC S Invalid vers option");
		break;
	case RPC_S_NO_MORE_MEMBERS:
		return("RPC S No more members");
		break;
	case RPC_S_NOT_ALL_OBJS_UNEXPORTED:
		return("RPC S Not all objs unexported");
		break;
	case RPC_S_INTERFACE_NOT_FOUND:
		return("RPC S Interface not found");
		break;
	case RPC_S_ENTRY_ALREADY_EXISTS:
		return("RPC S Entry already exists");
		break;
	case RPC_S_ENTRY_NOT_FOUND:
		return("RPC S Entry not found");
		break;
	case RPC_S_NAME_SERVICE_UNAVAILABLE:
		return("RPC S Name service unavailable");
		break;
	case RPC_S_INVALID_NAF_ID:
		return("RPC S Invalid naf ID");
		break;
	case RPC_S_CANNOT_SUPPORT:
		return("RPC S Cannot support");
		break;
	case RPC_S_NO_CONTEXT_AVAILABLE:
		return("RPC S No context available");
		break;
	case RPC_S_INTERNAL_ERROR:
		return("RPC S Internal error");
		break;
	case RPC_S_ZERO_DIVIDE:
		return("RPC S Zero divide");
		break;
	case RPC_S_ADDRESS_ERROR:
		return("RPC S Address error");
		break;
	case RPC_S_FP_DIV_ZERO:
		return("RPC S FP div zero");
		break;
	case RPC_S_FP_UNDERFLOW:
		return("RPC S FP Underflow");
		break;
	case RPC_S_FP_OVERFLOW:
		return("RPC S Overflow");
		break;
	case RPC_X_NO_MORE_ENTRIES:
		return("RPC X No more entries");
		break;
	case RPC_X_SS_CHAR_TRANS_OPEN_FAIL:
		return("RPC X SS char trans open fail");
		break;
	case RPC_X_SS_CHAR_TRANS_SHORT_FILE:
		return("RPC X SS char trans short file");
		break;
	case RPC_X_SS_IN_NULL_CONTEXT:
		return("RPC S SS in null context");
		break;
	case RPC_X_SS_CONTEXT_DAMAGED:
		return("RPC X SS context damaged");
		break;
	case RPC_X_SS_HANDLES_MISMATCH:
		return("RPC X SS handles mismatch");
		break;
	case RPC_X_SS_CANNOT_GET_CALL_HANDLE:
		return("RPC X SS cannot get call handle");
		break;
	case RPC_X_NULL_REF_POINTER:
		return("RPC X Null ref pointer");
		break;
	case RPC_X_ENUM_VALUE_OUT_OF_RANGE:
		return("RPC X enum value out of range");
		break;
	case RPC_X_BYTE_COUNT_TOO_SMALL:
		return("RPC X byte count too small");
		break;
	case RPC_X_BAD_STUB_DATA:
		return("RPC X bad stub data");
		break;
	case ERROR_INVALID_USER_BUFFER:
		return("Invalid user buffer");
		break;
	case ERROR_UNRECOGNIZED_MEDIA:
		return("Unrecognised media");
		break;
	case ERROR_NO_TRUST_LSA_SECRET:
		return("No trust lsa secret");
		break;
	case ERROR_NO_TRUST_SAM_ACCOUNT:
		return("No trust sam account");
		break;
	case ERROR_TRUSTED_DOMAIN_FAILURE:
		return("Trusted domain failure");
		break;
	case ERROR_TRUSTED_RELATIONSHIP_FAILURE:
		return("Trusted relationship failure");
		break;
	case ERROR_TRUST_FAILURE:
		return("Trust failure");
		break;
	case RPC_S_CALL_IN_PROGRESS:
		return("RPC S call in progress");
		break;
	case ERROR_NETLOGON_NOT_STARTED:
		return("Error netlogon not started");
		break;
	case ERROR_ACCOUNT_EXPIRED:
		return("Account expired");
		break;
	case ERROR_REDIRECTOR_HAS_OPEN_HANDLES:
		return("Redirector has open handles");
		break;
	case ERROR_PRINTER_DRIVER_ALREADY_INSTALLED:
		return("Printer driver already installed");
		break;
	case ERROR_UNKNOWN_PORT:
		return("Unknown port");
		break;
	case ERROR_UNKNOWN_PRINTER_DRIVER:
		return("Unknown printer driver");
		break;
	case ERROR_UNKNOWN_PRINTPROCESSOR:
		return("Unknown printprocessor");
		break;
	case ERROR_INVALID_SEPARATOR_FILE:
		return("Invalid separator file");
		break;
	case ERROR_INVALID_PRIORITY:
		return("Invalid priority");
		break;
	case ERROR_INVALID_PRINTER_NAME:
		return("Invalid printer name");
		break;
	case ERROR_PRINTER_ALREADY_EXISTS:
		return("Printer already exists");
		break;
	case ERROR_INVALID_PRINTER_COMMAND:
		return("Invalid printer command");
		break;
	case ERROR_INVALID_DATATYPE:
		return("Invalid datatype");
		break;
	case ERROR_INVALID_ENVIRONMENT:
		return("Invalid environment");
		break;
	case RPC_S_NO_MORE_BINDINGS:
		return("RPC S no more bindings");
		break;
	case ERROR_NOLOGON_INTERDOMAIN_TRUST_ACCOUNT:
		return("Nologon interdomain trust account");
		break;
	case ERROR_NOLOGON_WORKSTATION_TRUST_ACCOUNT:
		return("Nologon workstation trust account");
		break;
	case ERROR_NOLOGON_SERVER_TRUST_ACCOUNT:
		return("Nologon server trust account");
		break;
	case ERROR_DOMAIN_TRUST_INCONSISTENT:
		return("Domain trust inconsistent");
		break;
	case ERROR_SERVER_HAS_OPEN_HANDLES:
		return("Server has open handles");
		break;
	case ERROR_RESOURCE_DATA_NOT_FOUND:
		return("Resource data not found");
		break;
	case ERROR_RESOURCE_TYPE_NOT_FOUND:
		return("Resource type not found");
		break;
	case ERROR_RESOURCE_NAME_NOT_FOUND:
		return("Resource name not found");
		break;
	case ERROR_RESOURCE_LANG_NOT_FOUND:
		return("Resource lang not found");
		break;
	case ERROR_NOT_ENOUGH_QUOTA:
		return("Not enough quota");
		break;
	case RPC_S_NO_INTERFACES:
		return("RPC S no interfaces");
		break;
	case RPC_S_CALL_CANCELLED:
		return("RPC S Call cancelled");
		break;
	case RPC_S_BINDING_INCOMPLETE:
		return("RPC S Binding incomplete");
		break;
	case RPC_S_COMM_FAILURE:
		return("RPC S Comm failure");
		break;
	case RPC_S_UNSUPPORTED_AUTHN_LEVEL:
		return("RPC S Unsupported authn level");
		break;
	case RPC_S_NO_PRINC_NAME:
		return("RPC S No princ name");
		break;
	case RPC_S_NOT_RPC_ERROR:
		return("RPC S Not RPC error");
		break;
	case RPC_S_UUID_LOCAL_ONLY:
		return("RPC U UUID local only");
		break;
	case RPC_S_SEC_PKG_ERROR:
		return("RPC S Sec pkg error");
		break;
	case RPC_S_NOT_CANCELLED:
		return("RPC S Not cancelled");
		break;
	case RPC_X_INVALID_ES_ACTION:
		return("RPC X Invalid ES action");
		break;
	case RPC_X_WRONG_ES_VERSION:
		return("RPC X Wrong ES version");
		break;
	case RPC_X_WRONG_STUB_VERSION:
		return("RPC X Wrong stub version");
		break;
	case RPC_X_INVALID_PIPE_OBJECT:
		return("RPC X Invalid pipe object");
		break;
	case RPC_X_WRONG_PIPE_ORDER:
		return("RPC X Wrong pipe order");
		break;
	case RPC_X_WRONG_PIPE_VERSION:
		return("RPC X Wrong pipe version");
		break;
	case RPC_S_GROUP_MEMBER_NOT_FOUND:
		return("RPC S group member not found");
		break;
	case EPT_S_CANT_CREATE:
		return("EPT S Can't create");
		break;
	case RPC_S_INVALID_OBJECT:
		return("RPC S Invalid object");
		break;
	case ERROR_INVALID_TIME:
		return("Invalid time");
		break;
	case ERROR_INVALID_FORM_NAME:
		return("Invalid form name");
		break;
	case ERROR_INVALID_FORM_SIZE:
		return("Invalid form size");
		break;
	case ERROR_ALREADY_WAITING:
		return("Already waiting");
		break;
	case ERROR_PRINTER_DELETED:
		return("Printer deleted");
		break;
	case ERROR_INVALID_PRINTER_STATE:
		return("Invalid printer state");
		break;
	case ERROR_PASSWORD_MUST_CHANGE:
		return("Password must change");
		break;
	case ERROR_DOMAIN_CONTROLLER_NOT_FOUND:
		return("Domain controller not found");
		break;
	case ERROR_ACCOUNT_LOCKED_OUT:
		return("Account locked out");
		break;
	case OR_INVALID_OXID:
		return("OR Invalid OXID");
		break;
	case OR_INVALID_OID:
		return("OR Invalid OID");
		break;
	case OR_INVALID_SET:
		return("OR Invalid set");
		break;
	case RPC_S_SEND_INCOMPLETE:
		return("RPC S Send incomplete");
		break;
	case RPC_S_INVALID_ASYNC_HANDLE:
		return("RPC S Invalid async handle");
		break;
	case RPC_S_INVALID_ASYNC_CALL:
		return("RPC S Invalid async call");
		break;
	case RPC_X_PIPE_CLOSED:
		return("RPC X Pipe closed");
		break;
	case RPC_X_PIPE_DISCIPLINE_ERROR:
		return("RPC X Pipe discipline error");
		break;
	case RPC_X_PIPE_EMPTY:
		return("RPC X Pipe empty");
		break;
	case ERROR_NO_SITENAME:
		return("No sitename");
		break;
	case ERROR_CANT_ACCESS_FILE:
		return("Can't access file");
		break;
	case ERROR_CANT_RESOLVE_FILENAME:
		return("Can't resolve filename");
		break;
	case RPC_S_ENTRY_TYPE_MISMATCH:
		return("RPC S Entry type mismatch");
		break;
	case RPC_S_NOT_ALL_OBJS_EXPORTED:
		return("RPC S Not all objs exported");
		break;
	case RPC_S_INTERFACE_NOT_EXPORTED:
		return("RPC S Interface not exported");
		break;
	case RPC_S_PROFILE_NOT_ADDED:
		return("RPC S Profile not added");
		break;
	case RPC_S_PRF_ELT_NOT_ADDED:
		return("RPC S PRF ELT not added");
		break;
	case RPC_S_PRF_ELT_NOT_REMOVED:
		return("RPC S PRF ELT not removed");
		break;
	case RPC_S_GRP_ELT_NOT_ADDED:
		return("RPC S GRP ELT not added");
		break;
	case RPC_S_GRP_ELT_NOT_REMOVED:
		return("RPC S GRP ELT not removed");
		break;
	case ERROR_KM_DRIVER_BLOCKED:
		return("KM driver blocked");
		break;
	case ERROR_CONTEXT_EXPIRED:
		return("Context expired");
		break;
	case ERROR_INVALID_PIXEL_FORMAT:
		return("Invalid pixel format");
		break;
	case ERROR_BAD_DRIVER:
		return("Bad driver");
		break;
	case ERROR_INVALID_WINDOW_STYLE:
		return("Invalid window style");
		break;
	case ERROR_METAFILE_NOT_SUPPORTED:
		return("Metafile not supported");
		break;
	case ERROR_TRANSFORM_NOT_SUPPORTED:
		return("Transform not supported");
		break;
	case ERROR_CLIPPING_NOT_SUPPORTED:
		return("Clipping not supported");
		break;
	case ERROR_INVALID_CMM:
		return("Invalid CMM");
		break;
	case ERROR_INVALID_PROFILE:
		return("Invalid profile");
		break;
	case ERROR_TAG_NOT_FOUND:
		return("Tag not found");
		break;
	case ERROR_TAG_NOT_PRESENT:
		return("Tag not present");
		break;
	case ERROR_DUPLICATE_TAG:
		return("Duplicate tag");
		break;
	case ERROR_PROFILE_NOT_ASSOCIATED_WITH_DEVICE:
		return("Profile not associated with device");
		break;
	case ERROR_PROFILE_NOT_FOUND:
		return("Profile not found");
		break;
	case ERROR_INVALID_COLORSPACE:
		return("Invalid colorspace");
		break;
	case ERROR_ICM_NOT_ENABLED:
		return("ICM not enabled");
		break;
	case ERROR_DELETING_ICM_XFORM:
		return("Deleting ICM xform");
		break;
	case ERROR_INVALID_TRANSFORM:
		return("Invalid transform");
		break;
	case ERROR_COLORSPACE_MISMATCH:
		return("Colorspace mismatch");
		break;
	case ERROR_INVALID_COLORINDEX:
		return("Invalid colorindex");
		break;
	case ERROR_CONNECTED_OTHER_PASSWORD:
		return("Connected other password");
		break;
	case ERROR_CONNECTED_OTHER_PASSWORD_DEFAULT:
		return("Connected other password default");
		break;
	case ERROR_BAD_USERNAME:
		return("Bad username");
		break;
	case ERROR_NOT_CONNECTED:
		return("Not connected");
		break;
	case ERROR_OPEN_FILES:
		return("Open files");
		break;
	case ERROR_ACTIVE_CONNECTIONS:
		return("Active connections");
		break;
	case ERROR_DEVICE_IN_USE:
		return("Device in use");
		break;
	case ERROR_UNKNOWN_PRINT_MONITOR:
		return("Unknown print monitor");
		break;
	case ERROR_PRINTER_DRIVER_IN_USE:
		return("Printer driver in use");
		break;
	case ERROR_SPOOL_FILE_NOT_FOUND:
		return("Spool file not found");
		break;
	case ERROR_SPL_NO_STARTDOC:
		return("SPL no startdoc");
		break;
	case ERROR_SPL_NO_ADDJOB:
		return("SPL no addjob");
		break;
	case ERROR_PRINT_PROCESSOR_ALREADY_INSTALLED:
		return("Print processor already installed");
		break;
	case ERROR_PRINT_MONITOR_ALREADY_INSTALLED:
		return("Print monitor already installed");
		break;
	case ERROR_INVALID_PRINT_MONITOR:
		return("Invalid print monitor");
		break;
	case ERROR_PRINT_MONITOR_IN_USE:
		return("Print monitor in use");
		break;
	case ERROR_PRINTER_HAS_JOBS_QUEUED:
		return("Printer has jobs queued");
		break;
	case ERROR_SUCCESS_REBOOT_REQUIRED:
		return("Success reboot required");
		break;
	case ERROR_SUCCESS_RESTART_REQUIRED:
		return("Success restart required");
		break;
	case ERROR_PRINTER_NOT_FOUND:
		return("Printer not found");
		break;
	case ERROR_PRINTER_DRIVER_WARNED:
		return("Printer driver warned");
		break;
	case ERROR_PRINTER_DRIVER_BLOCKED:
		return("Printer driver blocked");
		break;
	case ERROR_WINS_INTERNAL:
		return("Wins internal");
		break;
	case ERROR_CAN_NOT_DEL_LOCAL_WINS:
		return("Can not del local wins");
		break;
	case ERROR_STATIC_INIT:
		return("Static init");
		break;
	case ERROR_INC_BACKUP:
		return("Inc backup");
		break;
	case ERROR_FULL_BACKUP:
		return("Full backup");
		break;
	case ERROR_REC_NON_EXISTENT:
		return("Rec not existent");
		break;
	case ERROR_RPL_NOT_ALLOWED:
		return("RPL not allowed");
		break;
	case ERROR_DHCP_ADDRESS_CONFLICT:
		return("DHCP address conflict");
		break;
	case ERROR_WMI_GUID_NOT_FOUND:
		return("WMU GUID not found");
		break;
	case ERROR_WMI_INSTANCE_NOT_FOUND:
		return("WMI instance not found");
		break;
	case ERROR_WMI_ITEMID_NOT_FOUND:
		return("WMI ItemID not found");
		break;
	case ERROR_WMI_TRY_AGAIN:
		return("WMI try again");
		break;
	case ERROR_WMI_DP_NOT_FOUND:
		return("WMI DP not found");
		break;
	case ERROR_WMI_UNRESOLVED_INSTANCE_REF:
		return("WMI unresolved instance ref");
		break;
	case ERROR_WMI_ALREADY_ENABLED:
		return("WMU already enabled");
		break;
	case ERROR_WMI_GUID_DISCONNECTED:
		return("WMU GUID disconnected");
		break;
	case ERROR_WMI_SERVER_UNAVAILABLE:
		return("WMI server unavailable");
		break;
	case ERROR_WMI_DP_FAILED:
		return("WMI DP failed");
		break;
	case ERROR_WMI_INVALID_MOF:
		return("WMI invalid MOF");
		break;
	case ERROR_WMI_INVALID_REGINFO:
		return("WMI invalid reginfo");
		break;
	case ERROR_WMI_ALREADY_DISABLED:
		return("WMI already disabled");
		break;
	case ERROR_WMI_READ_ONLY:
		return("WMI read only");
		break;
	case ERROR_WMI_SET_FAILURE:
		return("WMI set failure");
		break;
	case ERROR_INVALID_MEDIA:
		return("Invalid media");
		break;
	case ERROR_INVALID_LIBRARY:
		return("Invalid library");
		break;
	case ERROR_INVALID_MEDIA_POOL:
		return("Invalid media pool");
		break;
	case ERROR_DRIVE_MEDIA_MISMATCH:
		return("Drive media mismatch");
		break;
	case ERROR_MEDIA_OFFLINE:
		return("Media offline");
		break;
	case ERROR_LIBRARY_OFFLINE:
		return("Library offline");
		break;
	case ERROR_EMPTY:
		return("Empty");
		break;
	case ERROR_NOT_EMPTY:
		return("Not empty");
		break;
	case ERROR_MEDIA_UNAVAILABLE:
		return("Media unavailable");
		break;
	case ERROR_RESOURCE_DISABLED:
		return("Resource disabled");
		break;
	case ERROR_INVALID_CLEANER:
		return("Invalid cleaner");
		break;
	case ERROR_UNABLE_TO_CLEAN:
		return("Unable to clean");
		break;
	case ERROR_OBJECT_NOT_FOUND:
		return("Object not found");
		break;
	case ERROR_DATABASE_FAILURE:
		return("Database failure");
		break;
	case ERROR_DATABASE_FULL:
		return("Database full");
		break;
	case ERROR_MEDIA_INCOMPATIBLE:
		return("Media incompatible");
		break;
	case ERROR_RESOURCE_NOT_PRESENT:
		return("Resource not present");
		break;
	case ERROR_INVALID_OPERATION:
		return("Invalid operation");
		break;
	case ERROR_MEDIA_NOT_AVAILABLE:
		return("Media not available");
		break;
	case ERROR_DEVICE_NOT_AVAILABLE:
		return("Device not available");
		break;
	case ERROR_REQUEST_REFUSED:
		return("Request refused");
		break;
	case ERROR_INVALID_DRIVE_OBJECT:
		return("Invalid drive object");
		break;
	case ERROR_LIBRARY_FULL:
		return("Library full");
		break;
	case ERROR_MEDIUM_NOT_ACCESSIBLE:
		return("Medium not accessible");
		break;
	case ERROR_UNABLE_TO_LOAD_MEDIUM:
		return("Unable to load medium");
		break;
	case ERROR_UNABLE_TO_INVENTORY_DRIVE:
		return("Unable to inventory drive");
		break;
	case ERROR_UNABLE_TO_INVENTORY_SLOT:
		return("Unable to inventory slot");
		break;
	case ERROR_UNABLE_TO_INVENTORY_TRANSPORT:
		return("Unable to inventory transport");
		break;
	case ERROR_TRANSPORT_FULL:
		return("Transport full");
		break;
	case ERROR_CONTROLLING_IEPORT:
		return("Controlling ieport");
		break;
	case ERROR_UNABLE_TO_EJECT_MOUNTED_MEDIA:
		return("Unable to eject mounted media");
		break;
	case ERROR_CLEANER_SLOT_SET:
		return("Cleaner slot set");
		break;
	case ERROR_CLEANER_SLOT_NOT_SET:
		return("Cleaner slot not set");
		break;
	case ERROR_CLEANER_CARTRIDGE_SPENT:
		return("Cleaner cartridge spent");
		break;
	case ERROR_UNEXPECTED_OMID:
		return("Unexpected omid");
		break;
	case ERROR_CANT_DELETE_LAST_ITEM:
		return("Can't delete last item");
		break;
	case ERROR_MESSAGE_EXCEEDS_MAX_SIZE:
		return("Message exceeds max size");
		break;
	case ERROR_VOLUME_CONTAINS_SYS_FILES:
		return("Volume contains sys files");
		break;
	case ERROR_INDIGENOUS_TYPE:
		return("Indigenous type");
		break;
	case ERROR_NO_SUPPORTING_DRIVES:
		return("No supporting drives");
		break;
	case ERROR_CLEANER_CARTRIDGE_INSTALLED:
		return("Cleaner cartridge installed");
		break;
	case ERROR_FILE_OFFLINE:
		return("Fill offline");
		break;
	case ERROR_REMOTE_STORAGE_NOT_ACTIVE:
		return("Remote storage not active");
		break;
	case ERROR_REMOTE_STORAGE_MEDIA_ERROR:
		return("Remote storage media error");
		break;
	case ERROR_NOT_A_REPARSE_POINT:
		return("Not a reparse point");
		break;
	case ERROR_REPARSE_ATTRIBUTE_CONFLICT:
		return("Reparse attribute conflict");
		break;
	case ERROR_INVALID_REPARSE_DATA:
		return("Invalid reparse data");
		break;
	case ERROR_REPARSE_TAG_INVALID:
		return("Reparse tag invalid");
		break;
	case ERROR_REPARSE_TAG_MISMATCH:
		return("Reparse tag mismatch");
		break;
	case ERROR_VOLUME_NOT_SIS_ENABLED:
		return("Volume not sis enabled");
		break;
	case ERROR_DEPENDENT_RESOURCE_EXISTS:
		return("Dependent resource exists");
		break;
	case ERROR_DEPENDENCY_NOT_FOUND:
		return("Dependency not found");
		break;
	case ERROR_DEPENDENCY_ALREADY_EXISTS:
		return("Dependency already exists");
		break;
	case ERROR_RESOURCE_NOT_ONLINE:
		return("Resource not online");
		break;
	case ERROR_HOST_NODE_NOT_AVAILABLE:
		return("Host node not available");
		break;
	case ERROR_RESOURCE_NOT_AVAILABLE:
		return("Resource not available");
		break;
	case ERROR_RESOURCE_NOT_FOUND:
		return("Resource not found");
		break;
	case ERROR_SHUTDOWN_CLUSTER:
		return("Shutdown cluster");
		break;
	case ERROR_CANT_EVICT_ACTIVE_NODE:
		return("Can't evict active node");
		break;
	case ERROR_OBJECT_ALREADY_EXISTS:
		return("Object already exists");
		break;
	case ERROR_OBJECT_IN_LIST:
		return("Object in list");
		break;
	case ERROR_GROUP_NOT_AVAILABLE:
		return("Group not available");
		break;
	case ERROR_GROUP_NOT_FOUND:
		return("Group not found");
		break;
	case ERROR_GROUP_NOT_ONLINE:
		return("Group not online");
		break;
	case ERROR_HOST_NODE_NOT_RESOURCE_OWNER:
		return("Host node not resource owner");
		break;
	case ERROR_HOST_NODE_NOT_GROUP_OWNER:
		return("Host node not group owner");
		break;
	case ERROR_RESMON_CREATE_FAILED:
		return("Resmon create failed");
		break;
	case ERROR_RESMON_ONLINE_FAILED:
		return("Resmon online failed");
		break;
	case ERROR_RESOURCE_ONLINE:
		return("Resource online");
		break;
	case ERROR_QUORUM_RESOURCE:
		return("Quorum resource");
		break;
	case ERROR_NOT_QUORUM_CAPABLE:
		return("Not quorum capable");
		break;
	case ERROR_CLUSTER_SHUTTING_DOWN:
		return("Cluster shutting down");
		break;
	case ERROR_INVALID_STATE:
		return("Invalid state");
		break;
	case ERROR_RESOURCE_PROPERTIES_STORED:
		return("Resource properties stored");
		break;
	case ERROR_NOT_QUORUM_CLASS:
		return("Not quorum class");
		break;
	case ERROR_CORE_RESOURCE:
		return("Core resource");
		break;
	case ERROR_QUORUM_RESOURCE_ONLINE_FAILED:
		return("Quorum resource online failed");
		break;
	case ERROR_QUORUMLOG_OPEN_FAILED:
		return("Quorumlog open failed");
		break;
	case ERROR_CLUSTERLOG_CORRUPT:
		return("Clusterlog corrupt");
		break;
	case ERROR_CLUSTERLOG_RECORD_EXCEEDS_MAXSIZE:
		return("Clusterlog record exceeds maxsize");
		break;
	case ERROR_CLUSTERLOG_EXCEEDS_MAXSIZE:
		return("Clusterlog exceeds maxsize");
		break;
	case ERROR_CLUSTERLOG_CHKPOINT_NOT_FOUND:
		return("Clusterlog chkpoint not found");
		break;
	case ERROR_CLUSTERLOG_NOT_ENOUGH_SPACE:
		return("Clusterlog not enough space");
		break;
	case ERROR_QUORUM_OWNER_ALIVE:
		return("Quorum owner alive");
		break;
	case ERROR_NETWORK_NOT_AVAILABLE:
		return("Network not available");
		break;
	case ERROR_NODE_NOT_AVAILABLE:
		return("Node not available");
		break;
	case ERROR_ALL_NODES_NOT_AVAILABLE:
		return("All nodes not available");
		break;
	case ERROR_RESOURCE_FAILED:
		return("Resource failed");
		break;
	case ERROR_CLUSTER_INVALID_NODE:
		return("Cluster invalid node");
		break;
	case ERROR_CLUSTER_NODE_EXISTS:
		return("Cluster node exists");
		break;
	case ERROR_CLUSTER_JOIN_IN_PROGRESS:
		return("Cluster join in progress");
		break;
	case ERROR_CLUSTER_NODE_NOT_FOUND:
		return("Cluster node not found");
		break;
	case ERROR_CLUSTER_LOCAL_NODE_NOT_FOUND:
		return("Cluster local node not found");
		break;
	case ERROR_CLUSTER_NETWORK_EXISTS:
		return("Cluster network exists");
		break;
	case ERROR_CLUSTER_NETWORK_NOT_FOUND:
		return("Cluster network not found");
		break;
	case ERROR_CLUSTER_NETINTERFACE_EXISTS:
		return("Cluster netinterface exists");
		break;
	case ERROR_CLUSTER_NETINTERFACE_NOT_FOUND:
		return("Cluster netinterface not found");
		break;
	case ERROR_CLUSTER_INVALID_REQUEST:
		return("Cluster invalid request");
		break;
	case ERROR_CLUSTER_INVALID_NETWORK_PROVIDER:
		return("Cluster invalid network provider");
		break;
	case ERROR_CLUSTER_NODE_DOWN:
		return("Cluster node down");
		break;
	case ERROR_CLUSTER_NODE_UNREACHABLE:
		return("Cluster node unreachable");
		break;
	case ERROR_CLUSTER_NODE_NOT_MEMBER:
		return("Cluster node not member");
		break;
	case ERROR_CLUSTER_JOIN_NOT_IN_PROGRESS:
		return("Cluster join not in progress");
		break;
	case ERROR_CLUSTER_INVALID_NETWORK:
		return("Cluster invalid network");
		break;
	case ERROR_CLUSTER_NODE_UP:
		return("Cluster node up");
		break;
	case ERROR_CLUSTER_IPADDR_IN_USE:
		return("Cluster ipaddr in use");
		break;
	case ERROR_CLUSTER_NODE_NOT_PAUSED:
		return("Cluster node not paused");
		break;
	case ERROR_CLUSTER_NO_SECURITY_CONTEXT:
		return("Cluster no security context");
		break;
	case ERROR_CLUSTER_NETWORK_NOT_INTERNAL:
		return("Cluster network not internal");
		break;
	case ERROR_CLUSTER_NODE_ALREADY_UP:
		return("Cluster node already up");
		break;
	case ERROR_CLUSTER_NODE_ALREADY_DOWN:
		return("Cluster node already down");
		break;
	case ERROR_CLUSTER_NETWORK_ALREADY_ONLINE:
		return("Cluster network already online");
		break;
	case ERROR_CLUSTER_NETWORK_ALREADY_OFFLINE:
		return("Cluster network already offline");
		break;
	case ERROR_CLUSTER_NODE_ALREADY_MEMBER:
		return("Cluster node already member");
		break;
	case ERROR_CLUSTER_LAST_INTERNAL_NETWORK:
		return("Cluster last internal network");
		break;
	case ERROR_CLUSTER_NETWORK_HAS_DEPENDENTS:
		return("Cluster network has dependents");
		break;
	case ERROR_INVALID_OPERATION_ON_QUORUM:
		return("Invalid operation on quorum");
		break;
	case ERROR_DEPENDENCY_NOT_ALLOWED:
		return("Dependency not allowed");
		break;
	case ERROR_CLUSTER_NODE_PAUSED:
		return("Cluster node paused");
		break;
	case ERROR_NODE_CANT_HOST_RESOURCE:
		return("Node can't host resource");
		break;
	case ERROR_CLUSTER_NODE_NOT_READY:
		return("Cluster node not ready");
		break;
	case ERROR_CLUSTER_NODE_SHUTTING_DOWN:
		return("Cluster node shutting down");
		break;
	case ERROR_CLUSTER_JOIN_ABORTED:
		return("Cluster join aborted");
		break;
	case ERROR_CLUSTER_INCOMPATIBLE_VERSIONS:
		return("Cluster incompatible versions");
		break;
	case ERROR_CLUSTER_MAXNUM_OF_RESOURCES_EXCEEDED:
		return("Cluster maxnum of resources exceeded");
		break;
	case ERROR_CLUSTER_SYSTEM_CONFIG_CHANGED:
		return("Cluster system config changed");
		break;
	case ERROR_CLUSTER_RESOURCE_TYPE_NOT_FOUND:
		return("Cluster resource type not found");
		break;
	case ERROR_CLUSTER_RESTYPE_NOT_SUPPORTED:
		return("Cluster restype not supported");
		break;
	case ERROR_CLUSTER_RESNAME_NOT_FOUND:
		return("Cluster resname not found");
		break;
	case ERROR_CLUSTER_NO_RPC_PACKAGES_REGISTERED:
		return("Cluster no RPC packages registered");
		break;
	case ERROR_CLUSTER_OWNER_NOT_IN_PREFLIST:
		return("Cluster owner not in preflist");
		break;
	case ERROR_CLUSTER_DATABASE_SEQMISMATCH:
		return("Cluster database seqmismatch");
		break;
	case ERROR_RESMON_INVALID_STATE:
		return("Resmon invalid state");
		break;
	case ERROR_CLUSTER_GUM_NOT_LOCKER:
		return("Cluster gum not locker");
		break;
	case ERROR_QUORUM_DISK_NOT_FOUND:
		return("Quorum disk not found");
		break;
	case ERROR_DATABASE_BACKUP_CORRUPT:
		return("Database backup corrupt");
		break;
	case ERROR_CLUSTER_NODE_ALREADY_HAS_DFS_ROOT:
		return("Cluster node already has DFS root");
		break;
	case ERROR_RESOURCE_PROPERTY_UNCHANGEABLE:
		return("Resource property unchangeable");
		break;
	case ERROR_CLUSTER_MEMBERSHIP_INVALID_STATE:
		return("Cluster membership invalid state");
		break;
	case ERROR_CLUSTER_QUORUMLOG_NOT_FOUND:
		return("Cluster quorumlog not found");
		break;
	case ERROR_CLUSTER_MEMBERSHIP_HALT:
		return("Cluster membership halt");
		break;
	case ERROR_CLUSTER_INSTANCE_ID_MISMATCH:
		return("Cluster instance ID mismatch");
		break;
	case ERROR_CLUSTER_NETWORK_NOT_FOUND_FOR_IP:
		return("Cluster network not found for IP");
		break;
	case ERROR_CLUSTER_PROPERTY_DATA_TYPE_MISMATCH:
		return("Cluster property data type mismatch");
		break;
	case ERROR_CLUSTER_EVICT_WITHOUT_CLEANUP:
		return("Cluster evict without cleanup");
		break;
	case ERROR_CLUSTER_PARAMETER_MISMATCH:
		return("Cluster parameter mismatch");
		break;
	case ERROR_NODE_CANNOT_BE_CLUSTERED:
		return("Node cannot be clustered");
		break;
	case ERROR_CLUSTER_WRONG_OS_VERSION:
		return("Cluster wrong OS version");
		break;
	case ERROR_CLUSTER_CANT_CREATE_DUP_CLUSTER_NAME:
		return("Cluster can't create dup cluster name");
		break;
	case ERROR_ENCRYPTION_FAILED:
		return("Encryption failed");
		break;
	case ERROR_DECRYPTION_FAILED:
		return("Decryption failed");
		break;
	case ERROR_FILE_ENCRYPTED:
		return("File encrypted");
		break;
	case ERROR_NO_RECOVERY_POLICY:
		return("No recovery policy");
		break;
	case ERROR_NO_EFS:
		return("No EFS");
		break;
	case ERROR_WRONG_EFS:
		return("Wrong EFS");
		break;
	case ERROR_NO_USER_KEYS:
		return("No user keys");
		break;
	case ERROR_FILE_NOT_ENCRYPTED:
		return("File not encryped");
		break;
	case ERROR_NOT_EXPORT_FORMAT:
		return("Not export format");
		break;
	case ERROR_FILE_READ_ONLY:
		return("File read only");
		break;
	case ERROR_DIR_EFS_DISALLOWED:
		return("Dir EFS disallowed");
		break;
	case ERROR_EFS_SERVER_NOT_TRUSTED:
		return("EFS server not trusted");
		break;
	case ERROR_BAD_RECOVERY_POLICY:
		return("Bad recovery policy");
		break;
	case ERROR_EFS_ALG_BLOB_TOO_BIG:
		return("ETS alg blob too big");
		break;
	case ERROR_VOLUME_NOT_SUPPORT_EFS:
		return("Volume not support EFS");
		break;
	case ERROR_EFS_DISABLED:
		return("EFS disabled");
		break;
	case ERROR_EFS_VERSION_NOT_SUPPORT:
		return("EFS version not support");
		break;
	case ERROR_NO_BROWSER_SERVERS_FOUND:
		return("No browser servers found");
		break;
	case SCHED_E_SERVICE_NOT_LOCALSYSTEM:
		return("Sched E service not localsystem");
		break;
	case ERROR_CTX_WINSTATION_NAME_INVALID:
		return("Ctx winstation name invalid");
		break;
	case ERROR_CTX_INVALID_PD:
		return("Ctx invalid PD");
		break;
	case ERROR_CTX_PD_NOT_FOUND:
		return("Ctx PD not found");
		break;
	case ERROR_CTX_WD_NOT_FOUND:
		return("Ctx WD not found");
		break;
	case ERROR_CTX_CANNOT_MAKE_EVENTLOG_ENTRY:
		return("Ctx cannot make eventlog entry");
		break;
	case ERROR_CTX_SERVICE_NAME_COLLISION:
		return("Ctx service name collision");
		break;
	case ERROR_CTX_CLOSE_PENDING:
		return("Ctx close pending");
		break;
	case ERROR_CTX_NO_OUTBUF:
		return("Ctx no outbuf");
		break;
	case ERROR_CTX_MODEM_INF_NOT_FOUND:
		return("Ctx modem inf not found");
		break;
	case ERROR_CTX_INVALID_MODEMNAME:
		return("Ctx invalid modemname");
		break;
	case ERROR_CTX_MODEM_RESPONSE_ERROR:
		return("Ctx modem response error");
		break;
	case ERROR_CTX_MODEM_RESPONSE_TIMEOUT:
		return("Ctx modem response timeout");
		break;
	case ERROR_CTX_MODEM_RESPONSE_NO_CARRIER:
		return("Ctx modem response no carrier");
		break;
	case ERROR_CTX_MODEM_RESPONSE_NO_DIALTONE:
		return("Ctx modem response no dial tone");
		break;
	case ERROR_CTX_MODEM_RESPONSE_BUSY:
		return("Ctx modem response busy");
		break;
	case ERROR_CTX_MODEM_RESPONSE_VOICE:
		return("Ctx modem response voice");
		break;
	case ERROR_CTX_TD_ERROR:
		return("Ctx TD error");
		break;
	case ERROR_CTX_WINSTATION_NOT_FOUND:
		return("Ctx winstation not found");
		break;
	case ERROR_CTX_WINSTATION_ALREADY_EXISTS:
		return("Ctx winstation already exists");
		break;
	case ERROR_CTX_WINSTATION_BUSY:
		return("Ctx winstation busy");
		break;
	case ERROR_CTX_BAD_VIDEO_MODE:
		return("Ctx bad video mode");
		break;
	case ERROR_CTX_GRAPHICS_INVALID:
		return("Ctx graphics invalid");
		break;
	case ERROR_CTX_LOGON_DISABLED:
		return("Ctx logon disabled");
		break;
	case ERROR_CTX_NOT_CONSOLE:
		return("Ctx not console");
		break;
	case ERROR_CTX_CLIENT_QUERY_TIMEOUT:
		return("Ctx client query timeout");
		break;
	case ERROR_CTX_CONSOLE_DISCONNECT:
		return("Ctx console disconnect");
		break;
	case ERROR_CTX_CONSOLE_CONNECT:
		return("Ctx console connect");
		break;
	case ERROR_CTX_SHADOW_DENIED:
		return("Ctx shadow denied");
		break;
	case ERROR_CTX_WINSTATION_ACCESS_DENIED:
		return("Ctx winstation access denied");
		break;
	case ERROR_CTX_INVALID_WD:
		return("Ctx invalid WD");
		break;
	case ERROR_CTX_SHADOW_INVALID:
		return("Ctx shadow invalid");
		break;
	case ERROR_CTX_SHADOW_DISABLED:
		return("Ctx shadow disabled");
		break;
	case ERROR_CTX_CLIENT_LICENSE_IN_USE:
		return("Ctx client licence in use");
		break;
	case ERROR_CTX_CLIENT_LICENSE_NOT_SET:
		return("Ctx client licence not set");
		break;
	case ERROR_CTX_LICENSE_NOT_AVAILABLE:
		return("Ctx licence not available");
		break;
	case ERROR_CTX_LICENSE_CLIENT_INVALID:
		return("Ctx licence client invalid");
		break;
	case ERROR_CTX_LICENSE_EXPIRED:
		return("Ctx licence expired");
		break;
	case ERROR_CTX_SHADOW_NOT_RUNNING:
		return("Ctx shadow not running");
		break;
	case ERROR_CTX_SHADOW_ENDED_BY_MODE_CHANGE:
		return("Ctx shadow ended by mode change");
		break;
	case FRS_ERR_INVALID_API_SEQUENCE:
		return("FRS err invalid API sequence");
		break;
	case FRS_ERR_STARTING_SERVICE:
		return("FRS err starting service");
		break;
	case FRS_ERR_STOPPING_SERVICE:
		return("FRS err stopping service");
		break;
	case FRS_ERR_INTERNAL_API:
		return("FRS err internal API");
		break;
	case FRS_ERR_INTERNAL:
		return("FRS err internal");
		break;
	case FRS_ERR_SERVICE_COMM:
		return("FRS err service comm");
		break;
	case FRS_ERR_INSUFFICIENT_PRIV:
		return("FRS err insufficient priv");
		break;
	case FRS_ERR_AUTHENTICATION:
		return("FRS err authentication");
		break;
	case FRS_ERR_PARENT_INSUFFICIENT_PRIV:
		return("FRS err parent insufficient priv");
		break;
	case FRS_ERR_PARENT_AUTHENTICATION:
		return("FRS err parent authentication");
		break;
	case FRS_ERR_CHILD_TO_PARENT_COMM:
		return("FRS err child to parent comm");
		break;
	case FRS_ERR_PARENT_TO_CHILD_COMM:
		return("FRS err parent to child comm");
		break;
	case FRS_ERR_SYSVOL_POPULATE:
		return("FRS err sysvol populate");
		break;
	case FRS_ERR_SYSVOL_POPULATE_TIMEOUT:
		return("FRS err sysvol populate timeout");
		break;
	case FRS_ERR_SYSVOL_IS_BUSY:
		return("FRS err sysvol is busy");
		break;
	case FRS_ERR_SYSVOL_DEMOTE:
		return("FRS err sysvol demote");
		break;
	case FRS_ERR_INVALID_SERVICE_PARAMETER:
		return("FRS err invalid service parameter");
		break;
	case ERROR_DS_NOT_INSTALLED:
		return("DS not installed");
		break;
	case ERROR_DS_MEMBERSHIP_EVALUATED_LOCALLY:
		return("DS membership evaluated locally");
		break;
	case ERROR_DS_NO_ATTRIBUTE_OR_VALUE:
		return("DS no attribute or value");
		break;
	case ERROR_DS_INVALID_ATTRIBUTE_SYNTAX:
		return("DS invalid attribute syntax");
		break;
	case ERROR_DS_ATTRIBUTE_TYPE_UNDEFINED:
		return("DS attribute type undefined");
		break;
	case ERROR_DS_ATTRIBUTE_OR_VALUE_EXISTS:
		return("DS attribute or value exists");
		break;
	case ERROR_DS_BUSY:
		return("DS busy");
		break;
	case ERROR_DS_UNAVAILABLE:
		return("DS unavailable");
		break;
	case ERROR_DS_NO_RIDS_ALLOCATED:
		return("DS no rids allocated");
		break;
	case ERROR_DS_NO_MORE_RIDS:
		return("DS no more rids");
		break;
	case ERROR_DS_INCORRECT_ROLE_OWNER:
		return("DS incorrect role owner");
		break;
	case ERROR_DS_RIDMGR_INIT_ERROR:
		return("DS ridmgr init error");
		break;
	case ERROR_DS_OBJ_CLASS_VIOLATION:
		return("DS obj class violation");
		break;
	case ERROR_DS_CANT_ON_NON_LEAF:
		return("DS can't on non leaf");
		break;
	case ERROR_DS_CANT_ON_RDN:
		return("DS can't on rnd");
		break;
	case ERROR_DS_CANT_MOD_OBJ_CLASS:
		return("DS can't mod obj class");
		break;
	case ERROR_DS_CROSS_DOM_MOVE_ERROR:
		return("DS cross dom move error");
		break;
	case ERROR_DS_GC_NOT_AVAILABLE:
		return("DS GC not available");
		break;
	case ERROR_SHARED_POLICY:
		return("Shared policy");
		break;
	case ERROR_POLICY_OBJECT_NOT_FOUND:
		return("Policy object not found");
		break;
	case ERROR_POLICY_ONLY_IN_DS:
		return("Policy only in DS");
		break;
	case ERROR_PROMOTION_ACTIVE:
		return("Promotion active");
		break;
	case ERROR_NO_PROMOTION_ACTIVE:
		return("No promotion active");
		break;
	case ERROR_DS_OPERATIONS_ERROR:
		return("DS operations error");
		break;
	case ERROR_DS_PROTOCOL_ERROR:
		return("DS protocol error");
		break;
	case ERROR_DS_TIMELIMIT_EXCEEDED:
		return("DS timelimit exceeded");
		break;
	case ERROR_DS_SIZELIMIT_EXCEEDED:
		return("DS sizelimit exceeded");
		break;
	case ERROR_DS_ADMIN_LIMIT_EXCEEDED:
		return("DS admin limit exceeded");
		break;
	case ERROR_DS_COMPARE_FALSE:
		return("DS compare false");
		break;
	case ERROR_DS_COMPARE_TRUE:
		return("DS compare true");
		break;
	case ERROR_DS_AUTH_METHOD_NOT_SUPPORTED:
		return("DS auth method not supported");
		break;
	case ERROR_DS_STRONG_AUTH_REQUIRED:
		return("DS strong auth required");
		break;
	case ERROR_DS_INAPPROPRIATE_AUTH:
		return("DS inappropriate auth");
		break;
	case ERROR_DS_AUTH_UNKNOWN:
		return("DS auth unknown");
		break;
	case ERROR_DS_REFERRAL:
		return("DS referral");
		break;
	case ERROR_DS_UNAVAILABLE_CRIT_EXTENSION:
		return("DS unavailable crit extension");
		break;
	case ERROR_DS_CONFIDENTIALITY_REQUIRED:
		return("DS confidentiality required");
		break;
	case ERROR_DS_INAPPROPRIATE_MATCHING:
		return("DS inappropriate matching");
		break;
	case ERROR_DS_CONSTRAINT_VIOLATION:
		return("DS constraint violation");
		break;
	case ERROR_DS_NO_SUCH_OBJECT:
		return("DS no such object");
		break;
	case ERROR_DS_ALIAS_PROBLEM:
		return("DS alias problem");
		break;
	case ERROR_DS_INVALID_DN_SYNTAX:
		return("DS invalid dn syntax");
		break;
	case ERROR_DS_IS_LEAF:
		return("DS is leaf");
		break;
	case ERROR_DS_ALIAS_DEREF_PROBLEM:
		return("DS alias deref problem");
		break;
	case ERROR_DS_UNWILLING_TO_PERFORM:
		return("DS unwilling to perform");
		break;
	case ERROR_DS_LOOP_DETECT:
		return("DS loop detect");
		break;
	case ERROR_DS_NAMING_VIOLATION:
		return("DS naming violation");
		break;
	case ERROR_DS_OBJECT_RESULTS_TOO_LARGE:
		return("DS object results too large");
		break;
	case ERROR_DS_AFFECTS_MULTIPLE_DSAS:
		return("DS affects multiple dsas");
		break;
	case ERROR_DS_SERVER_DOWN:
		return("DS server down");
		break;
	case ERROR_DS_LOCAL_ERROR:
		return("DS local error");
		break;
	case ERROR_DS_ENCODING_ERROR:
		return("DS encoding error");
		break;
	case ERROR_DS_DECODING_ERROR:
		return("DS decoding error");
		break;
	case ERROR_DS_FILTER_UNKNOWN:
		return("DS filter unknown");
		break;
	case ERROR_DS_PARAM_ERROR:
		return("DS param error");
		break;
	case ERROR_DS_NOT_SUPPORTED:
		return("DS not supported");
		break;
	case ERROR_DS_NO_RESULTS_RETURNED:
		return("DS no results returned");
		break;
	case ERROR_DS_CONTROL_NOT_FOUND:
		return("DS control not found");
		break;
	case ERROR_DS_CLIENT_LOOP:
		return("DS client loop");
		break;
	case ERROR_DS_REFERRAL_LIMIT_EXCEEDED:
		return("DS referral limit exceeded");
		break;
	case ERROR_DS_SORT_CONTROL_MISSING:
		return("DS sort control missing");
		break;
	case ERROR_DS_OFFSET_RANGE_ERROR:
		return("DS offset range error");
		break;
	case ERROR_DS_ROOT_MUST_BE_NC:
		return("DS root must be nc");
		break;
	case ERROR_DS_ADD_REPLICA_INHIBITED:
		return("DS and replica inhibited");
		break;
	case ERROR_DS_ATT_NOT_DEF_IN_SCHEMA:
		return("DS att not def in schema");
		break;
	case ERROR_DS_MAX_OBJ_SIZE_EXCEEDED:
		return("DS max obj size exceeded");
		break;
	case ERROR_DS_OBJ_STRING_NAME_EXISTS:
		return("DS obj string name exists");
		break;
	case ERROR_DS_NO_RDN_DEFINED_IN_SCHEMA:
		return("DS no rdn defined in schema");
		break;
	case ERROR_DS_RDN_DOESNT_MATCH_SCHEMA:
		return("DS rdn doesn't match schema");
		break;
	case ERROR_DS_NO_REQUESTED_ATTS_FOUND:
		return("DS no requested atts found");
		break;
	case ERROR_DS_USER_BUFFER_TO_SMALL:
		return("DS user buffer too small");
		break;
	case ERROR_DS_ATT_IS_NOT_ON_OBJ:
		return("DS att is not on obj");
		break;
	case ERROR_DS_ILLEGAL_MOD_OPERATION:
		return("DS illegal mod operation");
		break;
	case ERROR_DS_OBJ_TOO_LARGE:
		return("DS obj too large");
		break;
	case ERROR_DS_BAD_INSTANCE_TYPE:
		return("DS bad instance type");
		break;
	case ERROR_DS_MASTERDSA_REQUIRED:
		return("DS masterdsa required");
		break;
	case ERROR_DS_OBJECT_CLASS_REQUIRED:
		return("DS object class required");
		break;
	case ERROR_DS_MISSING_REQUIRED_ATT:
		return("DS missing required att");
		break;
	case ERROR_DS_ATT_NOT_DEF_FOR_CLASS:
		return("DS att not def for class");
		break;
	case ERROR_DS_ATT_ALREADY_EXISTS:
		return("DS att already exists");
		break;
	case ERROR_DS_CANT_ADD_ATT_VALUES:
		return("DS can't add att values");
		break;
	case ERROR_DS_SINGLE_VALUE_CONSTRAINT:
		return("DS single value constraint");
		break;
	case ERROR_DS_RANGE_CONSTRAINT:
		return("DS range constraint");
		break;
	case ERROR_DS_ATT_VAL_ALREADY_EXISTS:
		return("DS att val already exists");
		break;
	case ERROR_DS_CANT_REM_MISSING_ATT:
		return("DS can't rem missing att");
		break;
	case ERROR_DS_CANT_REM_MISSING_ATT_VAL:
		return("DS can't rem missing att val");
		break;
	case ERROR_DS_ROOT_CANT_BE_SUBREF:
		return("DS root can't be subref");
		break;
	case ERROR_DS_NO_CHAINING:
		return("DS no chaining");
		break;
	case ERROR_DS_NO_CHAINED_EVAL:
		return("DS no chained eval");
		break;
	case ERROR_DS_NO_PARENT_OBJECT:
		return("DS no parent object");
		break;
	case ERROR_DS_PARENT_IS_AN_ALIAS:
		return("DS parent is an alias");
		break;
	case ERROR_DS_CANT_MIX_MASTER_AND_REPS:
		return("DS can't mix master and reps");
		break;
	case ERROR_DS_CHILDREN_EXIST:
		return("DS children exist");
		break;
	case ERROR_DS_OBJ_NOT_FOUND:
		return("DS obj not found");
		break;
	case ERROR_DS_ALIASED_OBJ_MISSING:
		return("DS aliased obj missing");
		break;
	case ERROR_DS_BAD_NAME_SYNTAX:
		return("DS bad name syntax");
		break;
	case ERROR_DS_ALIAS_POINTS_TO_ALIAS:
		return("DS alias points to alias");
		break;
	case ERROR_DS_CANT_DEREF_ALIAS:
		return("DS can't redef alias");
		break;
	case ERROR_DS_OUT_OF_SCOPE:
		return("DS out of scope");
		break;
	case ERROR_DS_OBJECT_BEING_REMOVED:
		return("DS object being removed");
		break;
	case ERROR_DS_CANT_DELETE_DSA_OBJ:
		return("DS can't delete dsa obj");
		break;
	case ERROR_DS_GENERIC_ERROR:
		return("DS generic error");
		break;
	case ERROR_DS_DSA_MUST_BE_INT_MASTER:
		return("DS dsa must be int master");
		break;
	case ERROR_DS_CLASS_NOT_DSA:
		return("DS class not dsa");
		break;
	case ERROR_DS_INSUFF_ACCESS_RIGHTS:
		return("DS insuff access rights");
		break;
	case ERROR_DS_ILLEGAL_SUPERIOR:
		return("DS illegal superior");
		break;
	case ERROR_DS_ATTRIBUTE_OWNED_BY_SAM:
		return("DS attribute owned by sam");
		break;
	case ERROR_DS_NAME_TOO_MANY_PARTS:
		return("DS name too many parts");
		break;
	case ERROR_DS_NAME_TOO_LONG:
		return("DS name too long");
		break;
	case ERROR_DS_NAME_VALUE_TOO_LONG:
		return("DS name value too long");
		break;
	case ERROR_DS_NAME_UNPARSEABLE:
		return("DS name unparseable");
		break;
	case ERROR_DS_NAME_TYPE_UNKNOWN:
		return("DS name type unknown");
		break;
	case ERROR_DS_NOT_AN_OBJECT:
		return("DS not an object");
		break;
	case ERROR_DS_SEC_DESC_TOO_SHORT:
		return("DS sec desc too short");
		break;
	case ERROR_DS_SEC_DESC_INVALID:
		return("DS sec desc invalid");
		break;
	case ERROR_DS_NO_DELETED_NAME:
		return("DS no deleted name");
		break;
	case ERROR_DS_SUBREF_MUST_HAVE_PARENT:
		return("DS subref must have parent");
		break;
	case ERROR_DS_NCNAME_MUST_BE_NC:
		return("DS ncname must be nc");
		break;
	case ERROR_DS_CANT_ADD_SYSTEM_ONLY:
		return("DS can't add system only");
		break;
	case ERROR_DS_CLASS_MUST_BE_CONCRETE:
		return("DS class must be concrete");
		break;
	case ERROR_DS_INVALID_DMD:
		return("DS invalid dmd");
		break;
	case ERROR_DS_OBJ_GUID_EXISTS:
		return("DS obj GUID exists");
		break;
	case ERROR_DS_NOT_ON_BACKLINK:
		return("DS not on backlink");
		break;
	case ERROR_DS_NO_CROSSREF_FOR_NC:
		return("DS no crossref for nc");
		break;
	case ERROR_DS_SHUTTING_DOWN:
		return("DS shutting down");
		break;
	case ERROR_DS_UNKNOWN_OPERATION:
		return("DS unknown operation");
		break;
	case ERROR_DS_INVALID_ROLE_OWNER:
		return("DS invalid role owner");
		break;
	case ERROR_DS_COULDNT_CONTACT_FSMO:
		return("DS couldn't contact fsmo");
		break;
	case ERROR_DS_CROSS_NC_DN_RENAME:
		return("DS cross nc dn rename");
		break;
	case ERROR_DS_CANT_MOD_SYSTEM_ONLY:
		return("DS can't mod system only");
		break;
	case ERROR_DS_REPLICATOR_ONLY:
		return("DS replicator only");
		break;
	case ERROR_DS_OBJ_CLASS_NOT_DEFINED:
		return("DS obj class not defined");
		break;
	case ERROR_DS_OBJ_CLASS_NOT_SUBCLASS:
		return("DS obj class not subclass");
		break;
	case ERROR_DS_NAME_REFERENCE_INVALID:
		return("DS name reference invalid");
		break;
	case ERROR_DS_CROSS_REF_EXISTS:
		return("DS cross ref exists");
		break;
	case ERROR_DS_CANT_DEL_MASTER_CROSSREF:
		return("DS can't del master crossref");
		break;
	case ERROR_DS_SUBTREE_NOTIFY_NOT_NC_HEAD:
		return("DS subtree notify not nc head");
		break;
	case ERROR_DS_NOTIFY_FILTER_TOO_COMPLEX:
		return("DS notify filter too complex");
		break;
	case ERROR_DS_DUP_RDN:
		return("DS dup rdn");
		break;
	case ERROR_DS_DUP_OID:
		return("DS dup oid");
		break;
	case ERROR_DS_DUP_MAPI_ID:
		return("DS dup mapi ID");
		break;
	case ERROR_DS_DUP_SCHEMA_ID_GUID:
		return("DS dup schema ID GUID");
		break;
	case ERROR_DS_DUP_LDAP_DISPLAY_NAME:
		return("DS dup LDAP display name");
		break;
	case ERROR_DS_SEMANTIC_ATT_TEST:
		return("DS semantic att test");
		break;
	case ERROR_DS_SYNTAX_MISMATCH:
		return("DS syntax mismatch");
		break;
	case ERROR_DS_EXISTS_IN_MUST_HAVE:
		return("DS exists in must have");
		break;
	case ERROR_DS_EXISTS_IN_MAY_HAVE:
		return("DS exists in may have");
		break;
	case ERROR_DS_NONEXISTENT_MAY_HAVE:
		return("DS nonexistent may have");
		break;
	case ERROR_DS_NONEXISTENT_MUST_HAVE:
		return("DS nonexistent must have");
		break;
	case ERROR_DS_AUX_CLS_TEST_FAIL:
		return("DS aux cls test fail");
		break;
	case ERROR_DS_NONEXISTENT_POSS_SUP:
		return("DS nonexistent poss sup");
		break;
	case ERROR_DS_SUB_CLS_TEST_FAIL:
		return("DS sub cls test fail");
		break;
	case ERROR_DS_BAD_RDN_ATT_ID_SYNTAX:
		return("DS bad rdn att ID syntax");
		break;
	case ERROR_DS_EXISTS_IN_AUX_CLS:
		return("DS exists in aux cls");
		break;
	case ERROR_DS_EXISTS_IN_SUB_CLS:
		return("DS exists in sub cls");
		break;
	case ERROR_DS_EXISTS_IN_POSS_SUP:
		return("DS exists in poss sup");
		break;
	case ERROR_DS_RECALCSCHEMA_FAILED:
		return("DS recalcschema failed");
		break;
	case ERROR_DS_TREE_DELETE_NOT_FINISHED:
		return("DS tree delete not finished");
		break;
	case ERROR_DS_CANT_DELETE:
		return("DS can't delete");
		break;
	case ERROR_DS_ATT_SCHEMA_REQ_ID:
		return("DS att schema req ID");
		break;
	case ERROR_DS_BAD_ATT_SCHEMA_SYNTAX:
		return("DS bad att schema syntax");
		break;
	case ERROR_DS_CANT_CACHE_ATT:
		return("DS can't cache att");
		break;
	case ERROR_DS_CANT_CACHE_CLASS:
		return("DS can't cache class");
		break;
	case ERROR_DS_CANT_REMOVE_ATT_CACHE:
		return("DS can't remove att cache");
		break;
	case ERROR_DS_CANT_REMOVE_CLASS_CACHE:
		return("DS can't remove class cache");
		break;
	case ERROR_DS_CANT_RETRIEVE_DN:
		return("DS can't retrieve DN");
		break;
	case ERROR_DS_MISSING_SUPREF:
		return("DS missing supref");
		break;
	case ERROR_DS_CANT_RETRIEVE_INSTANCE:
		return("DS can't retrieve instance");
		break;
	case ERROR_DS_CODE_INCONSISTENCY:
		return("DS code inconsistency");
		break;
	case ERROR_DS_DATABASE_ERROR:
		return("DS database error");
		break;
	case ERROR_DS_GOVERNSID_MISSING:
		return("DS governsid missing");
		break;
	case ERROR_DS_MISSING_EXPECTED_ATT:
		return("DS missing expected att");
		break;
	case ERROR_DS_NCNAME_MISSING_CR_REF:
		return("DS ncname missing cr ref");
		break;
	case ERROR_DS_SECURITY_CHECKING_ERROR:
		return("DS security checking error");
		break;
	case ERROR_DS_SCHEMA_NOT_LOADED:
		return("DS schema not loaded");
		break;
	case ERROR_DS_SCHEMA_ALLOC_FAILED:
		return("DS schema alloc failed");
		break;
	case ERROR_DS_ATT_SCHEMA_REQ_SYNTAX:
		return("DS att schema req syntax");
		break;
	case ERROR_DS_GCVERIFY_ERROR:
		return("DS gcverify error");
		break;
	case ERROR_DS_DRA_SCHEMA_MISMATCH:
		return("DS dra schema mismatch");
		break;
	case ERROR_DS_CANT_FIND_DSA_OBJ:
		return("DS can't find dsa obj");
		break;
	case ERROR_DS_CANT_FIND_EXPECTED_NC:
		return("DS can't find expected nc");
		break;
	case ERROR_DS_CANT_FIND_NC_IN_CACHE:
		return("DS can't find nc in cache");
		break;
	case ERROR_DS_CANT_RETRIEVE_CHILD:
		return("DS can't retrieve child");
		break;
	case ERROR_DS_SECURITY_ILLEGAL_MODIFY:
		return("DS security illegal modify");
		break;
	case ERROR_DS_CANT_REPLACE_HIDDEN_REC:
		return("DS can't replace hidden rec");
		break;
	case ERROR_DS_BAD_HIERARCHY_FILE:
		return("DS bad hierarchy file");
		break;
	case ERROR_DS_BUILD_HIERARCHY_TABLE_FAILED:
		return("DS build hierarchy table failed");
		break;
	case ERROR_DS_CONFIG_PARAM_MISSING:
		return("DS config param missing");
		break;
	case ERROR_DS_COUNTING_AB_INDICES_FAILED:
		return("DS counting ab indices failed");
		break;
	case ERROR_DS_HIERARCHY_TABLE_MALLOC_FAILED:
		return("DS hierarchy table malloc failed");
		break;
	case ERROR_DS_INTERNAL_FAILURE:
		return("DS internal failure");
		break;
	case ERROR_DS_UNKNOWN_ERROR:
		return("DS unknown error");
		break;
	case ERROR_DS_ROOT_REQUIRES_CLASS_TOP:
		return("DS root requires class top");
		break;
	case ERROR_DS_REFUSING_FSMO_ROLES:
		return("DS refusing fmso roles");
		break;
	case ERROR_DS_MISSING_FSMO_SETTINGS:
		return("DS missing fmso settings");
		break;
	case ERROR_DS_UNABLE_TO_SURRENDER_ROLES:
		return("DS unable to surrender roles");
		break;
	case ERROR_DS_DRA_GENERIC:
		return("DS dra generic");
		break;
	case ERROR_DS_DRA_INVALID_PARAMETER:
		return("DS dra invalid parameter");
		break;
	case ERROR_DS_DRA_BUSY:
		return("DS dra busy");
		break;
	case ERROR_DS_DRA_BAD_DN:
		return("DS dra bad dn");
		break;
	case ERROR_DS_DRA_BAD_NC:
		return("DS dra bad nc");
		break;
	case ERROR_DS_DRA_DN_EXISTS:
		return("DS dra dn exists");
		break;
	case ERROR_DS_DRA_INTERNAL_ERROR:
		return("DS dra internal error");
		break;
	case ERROR_DS_DRA_INCONSISTENT_DIT:
		return("DS dra inconsistent dit");
		break;
	case ERROR_DS_DRA_CONNECTION_FAILED:
		return("DS dra connection failed");
		break;
	case ERROR_DS_DRA_BAD_INSTANCE_TYPE:
		return("DS dra bad instance type");
		break;
	case ERROR_DS_DRA_OUT_OF_MEM:
		return("DS dra out of mem");
		break;
	case ERROR_DS_DRA_MAIL_PROBLEM:
		return("DS dra mail problem");
		break;
	case ERROR_DS_DRA_REF_ALREADY_EXISTS:
		return("DS dra ref already exists");
		break;
	case ERROR_DS_DRA_REF_NOT_FOUND:
		return("DS dra ref not found");
		break;
	case ERROR_DS_DRA_OBJ_IS_REP_SOURCE:
		return("DS dra obj is rep source");
		break;
	case ERROR_DS_DRA_DB_ERROR:
		return("DS dra db error");
		break;
	case ERROR_DS_DRA_NO_REPLICA:
		return("DS dra no replica");
		break;
	case ERROR_DS_DRA_ACCESS_DENIED:
		return("DS dra access denied");
		break;
	case ERROR_DS_DRA_NOT_SUPPORTED:
		return("DS dra not supported");
		break;
	case ERROR_DS_DRA_RPC_CANCELLED:
		return("DS dra RPC cancelled");
		break;
	case ERROR_DS_DRA_SOURCE_DISABLED:
		return("DS dra source disabled");
		break;
	case ERROR_DS_DRA_SINK_DISABLED:
		return("DS dra sink disabled");
		break;
	case ERROR_DS_DRA_NAME_COLLISION:
		return("DS dra name collision");
		break;
	case ERROR_DS_DRA_SOURCE_REINSTALLED:
		return("DS dra source reinstalled");
		break;
	case ERROR_DS_DRA_MISSING_PARENT:
		return("DS dra missing parent");
		break;
	case ERROR_DS_DRA_PREEMPTED:
		return("DS dra preempted");
		break;
	case ERROR_DS_DRA_ABANDON_SYNC:
		return("DS dra abandon sync");
		break;
	case ERROR_DS_DRA_SHUTDOWN:
		return("DS dra shutdown");
		break;
	case ERROR_DS_DRA_INCOMPATIBLE_PARTIAL_SET:
		return("DS dra incompatible partial set");
		break;
	case ERROR_DS_DRA_SOURCE_IS_PARTIAL_REPLICA:
		return("DS dra source is partial replica");
		break;
	case ERROR_DS_DRA_EXTN_CONNECTION_FAILED:
		return("DS dra extn connection failed");
		break;
	case ERROR_DS_INSTALL_SCHEMA_MISMATCH:
		return("DS install schema mismatch");
		break;
	case ERROR_DS_DUP_LINK_ID:
		return("DS dup link ID");
		break;
	case ERROR_DS_NAME_ERROR_RESOLVING:
		return("DS name error resolving");
		break;
	case ERROR_DS_NAME_ERROR_NOT_FOUND:
		return("DS name error not found");
		break;
	case ERROR_DS_NAME_ERROR_NOT_UNIQUE:
		return("DS name error not unique");
		break;
	case ERROR_DS_NAME_ERROR_NO_MAPPING:
		return("DS name error no mapping");
		break;
	case ERROR_DS_NAME_ERROR_DOMAIN_ONLY:
		return("DS name error domain only");
		break;
	case ERROR_DS_NAME_ERROR_NO_SYNTACTICAL_MAPPING:
		return("DS name error no syntactical mapping");
		break;
	case ERROR_DS_CONSTRUCTED_ATT_MOD:
		return("DS constructed att mod");
		break;
	case ERROR_DS_WRONG_OM_OBJ_CLASS:
		return("DS wrong om obj class");
		break;
	case ERROR_DS_DRA_REPL_PENDING:
		return("DS dra repl pending");
		break;
	case ERROR_DS_DS_REQUIRED:
		return("DS ds required");
		break;
	case ERROR_DS_INVALID_LDAP_DISPLAY_NAME:
		return("DS invalid LDAP display name");
		break;
	case ERROR_DS_NON_BASE_SEARCH:
		return("DS non base search");
		break;
	case ERROR_DS_CANT_RETRIEVE_ATTS:
		return("DS can't retrieve atts");
		break;
	case ERROR_DS_BACKLINK_WITHOUT_LINK:
		return("DS backlink without link");
		break;
	case ERROR_DS_EPOCH_MISMATCH:
		return("DS epoch mismatch");
		break;
	case ERROR_DS_SRC_NAME_MISMATCH:
		return("DS src name mismatch");
		break;
	case ERROR_DS_SRC_AND_DST_NC_IDENTICAL:
		return("DS src and dst nc identical");
		break;
	case ERROR_DS_DST_NC_MISMATCH:
		return("DS dst nc mismatch");
		break;
	case ERROR_DS_NOT_AUTHORITIVE_FOR_DST_NC:
		return("DS not authoritive for dst nc");
		break;
	case ERROR_DS_SRC_GUID_MISMATCH:
		return("DS src GUID mismatch");
		break;
	case ERROR_DS_CANT_MOVE_DELETED_OBJECT:
		return("DS can't move deleted object");
		break;
	case ERROR_DS_PDC_OPERATION_IN_PROGRESS:
		return("DS pdc operation in progress");
		break;
	case ERROR_DS_CROSS_DOMAIN_CLEANUP_REQD:
		return("DS cross domain cleanup reqd");
		break;
	case ERROR_DS_ILLEGAL_XDOM_MOVE_OPERATION:
		return("DS illegal xdom move operation");
		break;
	case ERROR_DS_CANT_WITH_ACCT_GROUP_MEMBERSHPS:
		return("DS can't with acct group membershps");
		break;
	case ERROR_DS_NC_MUST_HAVE_NC_PARENT:
		return("DS nc must have nc parent");
		break;
	case ERROR_DS_DST_DOMAIN_NOT_NATIVE:
		return("DS dst domain not native");
		break;
	case ERROR_DS_MISSING_INFRASTRUCTURE_CONTAINER:
		return("DS missing infrastructure container");
		break;
	case ERROR_DS_CANT_MOVE_ACCOUNT_GROUP:
		return("DS can't move account group");
		break;
	case ERROR_DS_CANT_MOVE_RESOURCE_GROUP:
		return("DS can't move resource group");
		break;
	case ERROR_DS_INVALID_SEARCH_FLAG:
		return("DS invalid search flag");
		break;
	case ERROR_DS_NO_TREE_DELETE_ABOVE_NC:
		return("DS no tree delete above nc");
		break;
	case ERROR_DS_COULDNT_LOCK_TREE_FOR_DELETE:
		return("DS couldn't lock tree for delete");
		break;
	case ERROR_DS_COULDNT_IDENTIFY_OBJECTS_FOR_TREE_DELETE:
		return("DS couldn't identify objects for tree delete");
		break;
	case ERROR_DS_SAM_INIT_FAILURE:
		return("DS sam init failure");
		break;
	case ERROR_DS_SENSITIVE_GROUP_VIOLATION:
		return("DS sensitive group violation");
		break;
	case ERROR_DS_CANT_MOD_PRIMARYGROUPID:
		return("DS can't mod primarygroupid");
		break;
	case ERROR_DS_ILLEGAL_BASE_SCHEMA_MOD:
		return("DS illegal base schema mod");
		break;
	case ERROR_DS_NONSAFE_SCHEMA_CHANGE:
		return("DS nonsafe schema change");
		break;
	case ERROR_DS_SCHEMA_UPDATE_DISALLOWED:
		return("DS schema update disallowed");
		break;
	case ERROR_DS_CANT_CREATE_UNDER_SCHEMA:
		return("DS can't create under schema");
		break;
	case ERROR_DS_INSTALL_NO_SRC_SCH_VERSION:
		return("DS install no src sch version");
		break;
	case ERROR_DS_INSTALL_NO_SCH_VERSION_IN_INIFILE:
		return("DS install no sch version in inifile");
		break;
	case ERROR_DS_INVALID_GROUP_TYPE:
		return("DS invalid group type");
		break;
	case ERROR_DS_NO_NEST_GLOBALGROUP_IN_MIXEDDOMAIN:
		return("DS no nest globalgroup in mixeddomain");
		break;
	case ERROR_DS_NO_NEST_LOCALGROUP_IN_MIXEDDOMAIN:
		return("DS no nest localgroup in mixeddomain");
		break;
	case ERROR_DS_GLOBAL_CANT_HAVE_LOCAL_MEMBER:
		return("DS global can't have local member");
		break;
	case ERROR_DS_GLOBAL_CANT_HAVE_UNIVERSAL_MEMBER:
		return("DS global can't have universal member");
		break;
	case ERROR_DS_UNIVERSAL_CANT_HAVE_LOCAL_MEMBER:
		return("DS universal can't have local member");
		break;
	case ERROR_DS_GLOBAL_CANT_HAVE_CROSSDOMAIN_MEMBER:
		return("DS global can't have crossdomain member");
		break;
	case ERROR_DS_LOCAL_CANT_HAVE_CROSSDOMAIN_LOCAL_MEMBER:
		return("DS local can't have crossdomain local member");
		break;
	case ERROR_DS_HAVE_PRIMARY_MEMBERS:
		return("DS have primary members");
		break;
	case ERROR_DS_STRING_SD_CONVERSION_FAILED:
		return("DS string sd conversion failed");
		break;
	case ERROR_DS_NAMING_MASTER_GC:
		return("DS naming master gc");
		break;
	case ERROR_DS_LOOKUP_FAILURE:
		return("DS lookup failure");
		break;
	case ERROR_DS_COULDNT_UPDATE_SPNS:
		return("DS couldn't update spns");
		break;
	case ERROR_DS_CANT_RETRIEVE_SD:
		return("DS can't retrieve sd");
		break;
	case ERROR_DS_KEY_NOT_UNIQUE:
		return("DS key not unique");
		break;
	case ERROR_DS_WRONG_LINKED_ATT_SYNTAX:
		return("DS wrong linked att syntax");
		break;
	case ERROR_DS_SAM_NEED_BOOTKEY_PASSWORD:
		return("DS sam need bootkey password");
		break;
	case ERROR_DS_SAM_NEED_BOOTKEY_FLOPPY:
		return("DS sam need bootkey floppy");
		break;
	case ERROR_DS_CANT_START:
		return("DS can't start");
		break;
	case ERROR_DS_INIT_FAILURE:
		return("DS init failure");
		break;
	case ERROR_DS_NO_PKT_PRIVACY_ON_CONNECTION:
		return("DS no pkt privacy on connection");
		break;
	case ERROR_DS_SOURCE_DOMAIN_IN_FOREST:
		return("DS source domain in forest");
		break;
	case ERROR_DS_DESTINATION_DOMAIN_NOT_IN_FOREST:
		return("DS destination domain not in forest");
		break;
	case ERROR_DS_DESTINATION_AUDITING_NOT_ENABLED:
		return("DS destination auditing not enabled");
		break;
	case ERROR_DS_CANT_FIND_DC_FOR_SRC_DOMAIN:
		return("DS can't find dc for src domain");
		break;
	case ERROR_DS_SRC_OBJ_NOT_GROUP_OR_USER:
		return("DS src obj not group or user");
		break;
	case ERROR_DS_SRC_SID_EXISTS_IN_FOREST:
		return("DS src sid exists in forest");
		break;
	case ERROR_DS_SRC_AND_DST_OBJECT_CLASS_MISMATCH:
		return("DS src and dst object class mismatch");
		break;
	case ERROR_SAM_INIT_FAILURE:
		return("Sam init failure");
		break;
	case ERROR_DS_DRA_SCHEMA_INFO_SHIP:
		return("DS dra schema info ship");
		break;
	case ERROR_DS_DRA_SCHEMA_CONFLICT:
		return("DS dra schema conflict");
		break;
	case ERROR_DS_DRA_EARLIER_SCHEMA_CONLICT:
		return("DS dra earlier schema conflict");
		break;
	case ERROR_DS_DRA_OBJ_NC_MISMATCH:
		return("DS dra obj nc mismatch");
		break;
	case ERROR_DS_NC_STILL_HAS_DSAS:
		return("DS nc still has dsas");
		break;
	case ERROR_DS_GC_REQUIRED:
		return("DS gc required");
		break;
	case ERROR_DS_LOCAL_MEMBER_OF_LOCAL_ONLY:
		return("DS local member of local only");
		break;
	case ERROR_DS_NO_FPO_IN_UNIVERSAL_GROUPS:
		return("DS no fpo in universal groups");
		break;
	case ERROR_DS_CANT_ADD_TO_GC:
		return("DS can't add to gc");
		break;
	case ERROR_DS_NO_CHECKPOINT_WITH_PDC:
		return("DS no checkpoint with pdc");
		break;
	case ERROR_DS_SOURCE_AUDITING_NOT_ENABLED:
		return("DS source auditing not enabled");
		break;
	case ERROR_DS_CANT_CREATE_IN_NONDOMAIN_NC:
		return("DS can't create in nondomain nc");
		break;
	case ERROR_DS_INVALID_NAME_FOR_SPN:
		return("DS invalid name for spn");
		break;
	case ERROR_DS_FILTER_USES_CONTRUCTED_ATTRS:
		return("DS filter uses constructed attrs");
		break;
	case ERROR_DS_UNICODEPWD_NOT_IN_QUOTES:
		return("DS unicodepwd not in quotes");
		break;
	case ERROR_DS_MACHINE_ACCOUNT_QUOTA_EXCEEDED:
		return("DS machine account quota exceeded");
		break;
	case ERROR_DS_MUST_BE_RUN_ON_DST_DC:
		return("DS must be run on dst dc");
		break;
	case ERROR_DS_SRC_DC_MUST_BE_SP4_OR_GREATER:
		return("DS src dc must be sp4 or greater");
		break;
	case ERROR_DS_CANT_TREE_DELETE_CRITICAL_OBJ:
		return("DS can't tree delete critical obj");
		break;
	case ERROR_DS_INIT_FAILURE_CONSOLE:
		return("DS init failure console");
		break;
	case ERROR_DS_SAM_INIT_FAILURE_CONSOLE:
		return("DS sam init failure console");
		break;
	case ERROR_DS_FOREST_VERSION_TOO_HIGH:
		return("DS forest version too high");
		break;
	case ERROR_DS_DOMAIN_VERSION_TOO_HIGH:
		return("DS domain version too high");
		break;
	case ERROR_DS_FOREST_VERSION_TOO_LOW:
		return("DS forest version too low");
		break;
	case ERROR_DS_DOMAIN_VERSION_TOO_LOW:
		return("DS domain version too low");
		break;
	case ERROR_DS_INCOMPATIBLE_VERSION:
		return("DS incompatible version");
		break;
	case ERROR_DS_LOW_DSA_VERSION:
		return("DS low dsa version");
		break;
	case ERROR_DS_NO_BEHAVIOR_VERSION_IN_MIXEDDOMAIN:
		return("DS no behaviour version in mixeddomain");
		break;
	case ERROR_DS_NOT_SUPPORTED_SORT_ORDER:
		return("DS not supported sort order");
		break;
	case ERROR_DS_NAME_NOT_UNIQUE:
		return("DS name not unique");
		break;
	case ERROR_DS_MACHINE_ACCOUNT_CREATED_PRENT4:
		return("DS machine account created prent4");
		break;
	case ERROR_DS_OUT_OF_VERSION_STORE:
		return("DS out of version store");
		break;
	case ERROR_DS_INCOMPATIBLE_CONTROLS_USED:
		return("DS incompatible controls used");
		break;
	case ERROR_DS_NO_REF_DOMAIN:
		return("DS no ref domain");
		break;
	case ERROR_DS_RESERVED_LINK_ID:
		return("DS reserved link ID");
		break;
	case ERROR_DS_LINK_ID_NOT_AVAILABLE:
		return("DS link ID not available");
		break;
	case ERROR_DS_AG_CANT_HAVE_UNIVERSAL_MEMBER:
		return("DS ag can't have universal member");
		break;
	case ERROR_DS_MODIFYDN_DISALLOWED_BY_INSTANCE_TYPE:
		return("DS modifydn disallowed by instance type");
		break;
	case ERROR_DS_NO_OBJECT_MOVE_IN_SCHEMA_NC:
		return("DS no object move in schema nc");
		break;
	case ERROR_DS_MODIFYDN_DISALLOWED_BY_FLAG:
		return("DS modifydn disallowed by flag");
		break;
	case ERROR_DS_MODIFYDN_WRONG_GRANDPARENT:
		return("DS modifydn wrong grandparent");
		break;
	case ERROR_DS_NAME_ERROR_TRUST_REFERRAL:
		return("DS name error trust referral");
		break;
	case ERROR_NOT_SUPPORTED_ON_STANDARD_SERVER:
		return("Not supported on standard server");
		break;
	case ERROR_DS_CANT_ACCESS_REMOTE_PART_OF_AD:
		return("DS can't access remote part of ad");
		break;
	case ERROR_DS_CR_IMPOSSIBLE_TO_VALIDATE:
		return("DS cr impossible to validate");
		break;
	case ERROR_DS_THREAD_LIMIT_EXCEEDED:
		return("DS thread limit exceeded");
		break;
	case ERROR_DS_NOT_CLOSEST:
		return("DS not closest");
		break;
	case ERROR_DS_CANT_DERIVE_SPN_WITHOUT_SERVER_REF:
		return("DS can't derive spn without server ref");
		break;
	case ERROR_DS_SINGLE_USER_MODE_FAILED:
		return("DS single user mode failed");
		break;
	case ERROR_DS_NTDSCRIPT_SYNTAX_ERROR:
		return("DS ntdscript syntax error");
		break;
	case ERROR_DS_NTDSCRIPT_PROCESS_ERROR:
		return("DS ntdscript process error");
		break;
	case ERROR_DS_DIFFERENT_REPL_EPOCHS:
		return("DS different repl epochs");
		break;
	case ERROR_DS_DRS_EXTENSIONS_CHANGED:
		return("DS drs extensions changed");
		break;
	case ERROR_DS_REPLICA_SET_CHANGE_NOT_ALLOWED_ON_DISABLED_CR:
		return("DS replica set change not allowed on disabled cr");
		break;
	case ERROR_DS_NO_MSDS_INTID:
		return("DS no msds intid");
		break;
	case ERROR_DS_DUP_MSDS_INTID:
		return("DS dup msds intid");
		break;
	case ERROR_DS_EXISTS_IN_RDNATTID:
		return("DS exists in rdnattid");
		break;
	case ERROR_DS_AUTHORIZATION_FAILED:
		return("DS authorisation failed");
		break;
	case ERROR_DS_INVALID_SCRIPT:
		return("DS invalid script");
		break;
	case ERROR_DS_REMOTE_CROSSREF_OP_FAILED:
		return("DS remote crossref op failed");
		break;
	case DNS_ERROR_RCODE_FORMAT_ERROR:
		return("DNS error rcode format error");
		break;
	case DNS_ERROR_RCODE_SERVER_FAILURE:
		return("DNS error rcode server failure");
		break;
	case DNS_ERROR_RCODE_NAME_ERROR:
		return("DNS error rcode name error");
		break;
	case DNS_ERROR_RCODE_NOT_IMPLEMENTED:
		return("DNS error rcode not implemented");
		break;
	case DNS_ERROR_RCODE_REFUSED:
		return("DNS error rcode refused");
		break;
	case DNS_ERROR_RCODE_YXDOMAIN:
		return("DNS error rcode yxdomain");
		break;
	case DNS_ERROR_RCODE_YXRRSET:
		return("DNS error rcode yxrrset");
		break;
	case DNS_ERROR_RCODE_NXRRSET:
		return("DNS error rcode nxrrset");
		break;
	case DNS_ERROR_RCODE_NOTAUTH:
		return("DNS error rcode notauth");
		break;
	case DNS_ERROR_RCODE_NOTZONE:
		return("DNS error rcode notzone");
		break;
	case DNS_ERROR_RCODE_BADSIG:
		return("DNS error rcode badsig");
		break;
	case DNS_ERROR_RCODE_BADKEY:
		return("DNS error rcode badkey");
		break;
	case DNS_ERROR_RCODE_BADTIME:
		return("DNS error rcode badtime");
		break;
	case DNS_INFO_NO_RECORDS:
		return("DNS info no records");
		break;
	case DNS_ERROR_BAD_PACKET:
		return("DNS error bad packet");
		break;
	case DNS_ERROR_NO_PACKET:
		return("DNS error no packet");
		break;
	case DNS_ERROR_RCODE:
		return("DNS error rcode");
		break;
	case DNS_ERROR_UNSECURE_PACKET:
		return("DNS error unsecure packet");
		break;
	case DNS_ERROR_INVALID_TYPE:
		return("DNS error invalid type");
		break;
	case DNS_ERROR_INVALID_IP_ADDRESS:
		return("DNS error invalid IP address");
		break;
	case DNS_ERROR_INVALID_PROPERTY:
		return("DNS error invalid property");
		break;
	case DNS_ERROR_TRY_AGAIN_LATER:
		return("DNS error try again later");
		break;
	case DNS_ERROR_NOT_UNIQUE:
		return("DNS error not unique");
		break;
	case DNS_ERROR_NON_RFC_NAME:
		return("DNS error non RFC name");
		break;
	case DNS_STATUS_FQDN:
		return("DNS status FQDN");
		break;
	case DNS_STATUS_DOTTED_NAME:
		return("DNS status dotted name");
		break;
	case DNS_STATUS_SINGLE_PART_NAME:
		return("DNS status single part name");
		break;
	case DNS_ERROR_INVALID_NAME_CHAR:
		return("DNS error invalid name char");
		break;
	case DNS_ERROR_NUMERIC_NAME:
		return("DNS error numeric name");
		break;
	case DNS_ERROR_NOT_ALLOWED_ON_ROOT_SERVER:
		return("DNS error not allowed on root server");
		break;
	case DNS_ERROR_ZONE_DOES_NOT_EXIST:
		return("DNS error zone does not exist");
		break;
	case DNS_ERROR_NO_ZONE_INFO:
		return("DNS error not zone info");
		break;
	case DNS_ERROR_INVALID_ZONE_OPERATION:
		return("DNS error invalid zone operation");
		break;
	case DNS_ERROR_ZONE_CONFIGURATION_ERROR:
		return("DNS error zone configuration error");
		break;
	case DNS_ERROR_ZONE_HAS_NO_SOA_RECORD:
		return("DNS error zone has not SOA record");
		break;
	case DNS_ERROR_ZONE_HAS_NO_NS_RECORDS:
		return("DNS error zone has no NS records");
		break;
	case DNS_ERROR_ZONE_LOCKED:
		return("DNS error zone locked");
		break;
	case DNS_ERROR_ZONE_CREATION_FAILED:
		return("DNS error zone creation failed");
		break;
	case DNS_ERROR_ZONE_ALREADY_EXISTS:
		return("DNS error zone already exists");
		break;
	case DNS_ERROR_AUTOZONE_ALREADY_EXISTS:
		return("DNS error autozone already exists");
		break;
	case DNS_ERROR_INVALID_ZONE_TYPE:
		return("DNS error invalid zone type");
		break;
	case DNS_ERROR_SECONDARY_REQUIRES_MASTER_IP:
		return("DNS error secondary requires master IP");
		break;
	case DNS_ERROR_ZONE_NOT_SECONDARY:
		return("DNS error zone not secondary");
		break;
	case DNS_ERROR_NEED_SECONDARY_ADDRESSES:
		return("DNS error need secondary addresses");
		break;
	case DNS_ERROR_WINS_INIT_FAILED:
		return("DNS error wins init failed");
		break;
	case DNS_ERROR_NEED_WINS_SERVERS:
		return("DNS error need wins servers");
		break;
	case DNS_ERROR_NBSTAT_INIT_FAILED:
		return("DNS error nbstat init failed");
		break;
	case DNS_ERROR_SOA_DELETE_INVALID:
		return("DNS error SOA delete invalid");
		break;
	case DNS_ERROR_FORWARDER_ALREADY_EXISTS:
		return("DNS error forwarder already exists");
		break;
	case DNS_ERROR_ZONE_REQUIRES_MASTER_IP:
		return("DNS error zone requires master IP");
		break;
	case DNS_ERROR_ZONE_IS_SHUTDOWN:
		return("DNS error zone is shutdown");
		break;
	case DNS_ERROR_PRIMARY_REQUIRES_DATAFILE:
		return("DNS error primary requires datafile");
		break;
	case DNS_ERROR_INVALID_DATAFILE_NAME:
		return("DNS error invalid datafile name");
		break;
	case DNS_ERROR_DATAFILE_OPEN_FAILURE:
		return("DNS error datafile open failure");
		break;
	case DNS_ERROR_FILE_WRITEBACK_FAILED:
		return("DNS error file writeback failed");
		break;
	case DNS_ERROR_DATAFILE_PARSING:
		return("DNS error datafile parsing");
		break;
	case DNS_ERROR_RECORD_DOES_NOT_EXIST:
		return("DNS error record does not exist");
		break;
	case DNS_ERROR_RECORD_FORMAT:
		return("DNS error record format");
		break;
	case DNS_ERROR_NODE_CREATION_FAILED:
		return("DNS error node creation failed");
		break;
	case DNS_ERROR_UNKNOWN_RECORD_TYPE:
		return("DNS error unknown record type");
		break;
	case DNS_ERROR_RECORD_TIMED_OUT:
		return("DNS error record timed out");
		break;
	case DNS_ERROR_NAME_NOT_IN_ZONE:
		return("DNS error name not in zone");
		break;
	case DNS_ERROR_CNAME_LOOP:
		return("DNS error CNAME loop");
		break;
	case DNS_ERROR_NODE_IS_CNAME:
		return("DNS error node is CNAME");
		break;
	case DNS_ERROR_CNAME_COLLISION:
		return("DNS error CNAME collision");
		break;
	case DNS_ERROR_RECORD_ONLY_AT_ZONE_ROOT:
		return("DNS error record only at zone root");
		break;
	case DNS_ERROR_RECORD_ALREADY_EXISTS:
		return("DNS error record already exists");
		break;
	case DNS_ERROR_SECONDARY_DATA:
		return("DNS error secondary data");
		break;
	case DNS_ERROR_NO_CREATE_CACHE_DATA:
		return("DNS error no create cache data");
		break;
	case DNS_ERROR_NAME_DOES_NOT_EXIST:
		return("DNS error name does not exist");
		break;
	case DNS_WARNING_PTR_CREATE_FAILED:
		return("DNS warning PTR create failed");
		break;
	case DNS_WARNING_DOMAIN_UNDELETED:
		return("DNS warning domain undeleted");
		break;
	case DNS_ERROR_DS_UNAVAILABLE:
		return("DNS error ds unavailable");
		break;
	case DNS_ERROR_DS_ZONE_ALREADY_EXISTS:
		return("DNS error ds zone already exists");
		break;
	case DNS_ERROR_NO_BOOTFILE_IF_DS_ZONE:
		return("DNS error no bootfile if ds zone");
		break;
	case DNS_INFO_AXFR_COMPLETE:
		return("DNS info AXFR complete");
		break;
	case DNS_ERROR_AXFR:
		return("DNS error AXFR");
		break;
	case DNS_INFO_ADDED_LOCAL_WINS:
		return("DNS info added local wins");
		break;
	case DNS_STATUS_CONTINUE_NEEDED:
		return("DNS status continue needed");
		break;
	case DNS_ERROR_NO_TCPIP:
		return("DNS error no TCPIP");
		break;
	case DNS_ERROR_NO_DNS_SERVERS:
		return("DNS error no DNS servers");
		break;
	case DNS_ERROR_DP_DOES_NOT_EXIST:
		return("DNS error dp does not exist");
		break;
	case DNS_ERROR_DP_ALREADY_EXISTS:
		return("DNS error dp already exists");
		break;
	case DNS_ERROR_DP_NOT_ENLISTED:
		return("DNS error dp not enlisted");
		break;
	case DNS_ERROR_DP_ALREADY_ENLISTED:
		return("DNS error dp already enlisted");
		break;
	case WSAEINTR:
		return("interrupted");
		break;
	case WSAEBADF:
		return("Bad file number");
		break;
	case WSAEACCES:
		return("Access denied");
		break;
	case WSAEFAULT:
		return("Bad address");
		break;
	case WSAEINVAL:
		return("Invalid arguments");
		break;
	case WSAEMFILE:
		return("Too many open files");
		break;
	case WSAEWOULDBLOCK:
		return("Operation on non-blocking socket would block");
		break;
	case WSAEINPROGRESS:
		return("Operation in progress");
		break;
	case WSAEALREADY:
		return("Operation already in progress");
		break;
	case WSAENOTSOCK:
		return("The descriptor is not a socket");
		break;
	case WSAEDESTADDRREQ:
		return("Destination address required");
		break;
	case WSAEMSGSIZE:
		return("Message too long");
		break;
	case WSAEPROTOTYPE:
		return("Protocol wrong type for socket");
		break;
	case WSAENOPROTOOPT:
		return("Protocol option not supported");
		break;
	case WSAEPROTONOSUPPORT:
		return("Protocol not supported");
		break;
	case WSAESOCKTNOSUPPORT:
		return("Socket not supported");
		break;
	case WSAEOPNOTSUPP:
		return("Operation not supported");
		break;
	case WSAEPFNOSUPPORT:
		return("Protocol family not supported");
		break;
	case WSAEAFNOSUPPORT:
		return("An address incompatible with the requested protocol was used");
		break;
	case WSAEADDRINUSE:
		return("Address already in use");
		break;
	case WSAEADDRNOTAVAIL:
		return("The requested address is not valid in this context");
		break;
	case WSAENETDOWN:
		return("Network subsystem is down");
		break;
	case WSAENETUNREACH:
		return("Network is unreachable");
		break;
	case WSAENETRESET:
		return("Connection broken, keep-alive detected a problem");
		break;
	case WSAECONNABORTED:
		return("An established connection was aborted in your host machine.");
		break;
	case WSAECONNRESET:
		return("Connection reset by peer");
		break;
	case WSAENOBUFS:
		return("Not enough buffer space is available");
		break;
	case WSAEISCONN:
		return("Socket is already connected");
		break;
	case WSAENOTCONN:
		return("The socket is not connected");
		break;
	case WSAESHUTDOWN:
		return("The socket has been shut down");
		break;
	case WSAETOOMANYREFS:
		return("Too many references: cannot splice");
		break;
	case WSAETIMEDOUT:
		return("Connection timed out");
		break;
	case WSAECONNREFUSED:
		return("Connection refused");
		break;
	case WSAELOOP:
		return("Too many symbolic links encountered");
		break;
	case WSAENAMETOOLONG:
		return("File name too long");
		break;
	case WSAEHOSTDOWN:
		return("Host is down");
		break;
	case WSAEHOSTUNREACH:
		return("No route to host");
		break;
	case WSAENOTEMPTY:
		return("Directory not empty");
		break;
	case WSAEPROCLIM:
		return("EPROCLIM");
		break;
	case WSAEUSERS:
		return("Too many users");
		break;
	case WSAEDQUOT:
		return("Quota exceeded");
		break;
	case WSAESTALE:
		return("Stale NFS file handle");
		break;
	case WSAEREMOTE:
		return("Object is remote");
		break;
	case WSASYSNOTREADY:
		return("SYSNOTREADY");
		break;
	case WSAVERNOTSUPPORTED:
		return("VERNOTSUPPORTED");
		break;
	case WSANOTINITIALISED:
		return("Winsock not initialised");
		break;
	case WSAEDISCON:
		return("EDISCON");
		break;
	case WSAENOMORE:
		return("ENOMORE");
		break;
	case WSAECANCELLED:
		return("Operation canceled");
		break;
	case WSAEINVALIDPROCTABLE:
		return("EINVALIDPROCTABLE");
		break;
	case WSAEINVALIDPROVIDER:
		return("EINVALIDPROVIDER");
		break;
	case WSAEPROVIDERFAILEDINIT:
		return("EPROVIDERFAILEDINIT");
		break;
	case WSASYSCALLFAILURE:
		return("System call failed");
		break;
	case WSASERVICE_NOT_FOUND:
		return("SERVICE_NOT_FOUND");
		break;
	case WSATYPE_NOT_FOUND:
		return("TYPE_NOT_FOUND");
		break;
	case WSA_E_NO_MORE:
		return("E_NO_MORE");
		break;
	case WSA_E_CANCELLED:
		return("E_CANCELLED");
		break;
	case WSAEREFUSED:
		return("EREFUSED");
		break;
	case WSAHOST_NOT_FOUND:
		return("No such host is known");
		break;
	case WSATRY_AGAIN:
		return("A temporary error occurred on an authoritative name server.  Try again later.");
		break;
	case WSANO_RECOVERY:
		return("No recovery");
		break;
	case WSANO_DATA:
		return("No data");
		break;
	case WSA_QOS_RECEIVERS:
		return("QOS receivers");
		break;
	case WSA_QOS_SENDERS:
		return("QOS senders");
		break;
	case WSA_QOS_NO_SENDERS:
		return("QOS no senders");
		break;
	case WSA_QOS_NO_RECEIVERS:
		return("QOS no receivers");
		break;
	case WSA_QOS_REQUEST_CONFIRMED:
		return("QOS request confirmed");
		break;
	case WSA_QOS_ADMISSION_FAILURE:
		return("QOS admission failure");
		break;
	case WSA_QOS_POLICY_FAILURE:
		return("QOS policy failure");
		break;
	case WSA_QOS_BAD_STYLE:
		return("QOS bad style");
		break;
	case WSA_QOS_BAD_OBJECT:
		return("QOS bad object");
		break;
	case WSA_QOS_TRAFFIC_CTRL_ERROR:
		return("QOS traffic ctrl error");
		break;
	case WSA_QOS_GENERIC_ERROR:
		return("QOS generic error");
		break;
	case WSA_QOS_ESERVICETYPE:
		return("QOS eservicetype");
		break;
	case WSA_QOS_EFLOWSPEC:
		return("QOS eflowspec");
		break;
	case WSA_QOS_EPROVSPECBUF:
		return("QOS eprovspecbuf");
		break;
	case WSA_QOS_EFILTERSTYLE:
		return("QOS efilterstyle");
		break;
	case WSA_QOS_EFILTERTYPE:
		return("QOS efiltertype");
		break;
	case WSA_QOS_EFILTERCOUNT:
		return("QOS efiltercount");
		break;
	case WSA_QOS_EOBJLENGTH:
		return("QOS eobjlength");
		break;
	case WSA_QOS_EFLOWCOUNT:
		return("QOS eflowcount");
		break;
	case WSA_QOS_EUNKNOWNPSOBJ:
		return("QOS eunknownpsobj");
		break;
	case WSA_QOS_EPOLICYOBJ:
		return("QOS epolicyobj");
		break;
	case WSA_QOS_EFLOWDESC:
		return("QOS eflowdesc");
		break;
	case WSA_QOS_EPSFLOWSPEC:
		return("QOS epsflowspec");
		break;
	case WSA_QOS_EPSFILTERSPEC:
		return("QOS epsfilterspec");
		break;
	case WSA_QOS_ESDMODEOBJ:
		return("QOS esdmodeobj");
		break;
	case WSA_QOS_ESHAPERATEOBJ:
		return("QOS eshaperateobj");
		break;
	case WSA_QOS_RESERVED_PETYPE:
		return("QOS reserved petype");
		break;
	case ERROR_IPSEC_QM_POLICY_EXISTS:
		return("IPSEC qm policy exists");
		break;
	case ERROR_IPSEC_QM_POLICY_NOT_FOUND:
		return("IPSEC qm policy not found");
		break;
	case ERROR_IPSEC_QM_POLICY_IN_USE:
		return("IPSEC qm policy in use");
		break;
	case ERROR_IPSEC_MM_POLICY_EXISTS:
		return("IPSEC mm policy exists");
		break;
	case ERROR_IPSEC_MM_POLICY_NOT_FOUND:
		return("IPSEC mm policy not found");
		break;
	case ERROR_IPSEC_MM_POLICY_IN_USE:
		return("IPSEC mm policy in use");
		break;
	case ERROR_IPSEC_MM_FILTER_EXISTS:
		return("IPSEC mm filter exists");
		break;
	case ERROR_IPSEC_MM_FILTER_NOT_FOUND:
		return("IPSEC mm filter not found");
		break;
	case ERROR_IPSEC_TRANSPORT_FILTER_EXISTS:
		return("IPSEC transport filter exists");
		break;
	case ERROR_IPSEC_TRANSPORT_FILTER_NOT_FOUND:
		return("IPSEC transport filter not found");
		break;
	case ERROR_IPSEC_MM_AUTH_EXISTS:
		return("IPSEC mm auth exists");
		break;
	case ERROR_IPSEC_MM_AUTH_NOT_FOUND:
		return("IPSEC mm auth not found");
		break;
	case ERROR_IPSEC_MM_AUTH_IN_USE:
		return("IPSEC mm auth in use");
		break;
	case ERROR_IPSEC_DEFAULT_MM_POLICY_NOT_FOUND:
		return("IPSEC default mm policy not found");
		break;
	case ERROR_IPSEC_DEFAULT_MM_AUTH_NOT_FOUND:
		return("IPSEC default mm auth not found");
		break;
	case ERROR_IPSEC_DEFAULT_QM_POLICY_NOT_FOUND:
		return("IPSEC default qm policy not found");
		break;
	case ERROR_IPSEC_TUNNEL_FILTER_EXISTS:
		return("IPSEC tunnel filter exists");
		break;
	case ERROR_IPSEC_TUNNEL_FILTER_NOT_FOUND:
		return("IPSEC tunnel filter not found");
		break;
	case ERROR_IPSEC_MM_FILTER_PENDING_DELETION:
		return("IPSEC mm filter pending deletion");
		break;
	case ERROR_IPSEC_TRANSPORT_FILTER_PENDING_DELETION:
		return("IPSEC transport filter pending deletion");
		break;
	case ERROR_IPSEC_TUNNEL_FILTER_PENDING_DELETION:
		return("IPSEC tunnel filter pending deletion");
		break;
	case ERROR_IPSEC_MM_POLICY_PENDING_DELETION:
		return("IPSEC mm policy pending deletion");
		break;
	case ERROR_IPSEC_MM_AUTH_PENDING_DELETION:
		return("IPSEC mm auth pending deletion");
		break;
	case ERROR_IPSEC_QM_POLICY_PENDING_DELETION:
		return("IPSEC qm policy pending deletion");
		break;
	case ERROR_IPSEC_IKE_AUTH_FAIL:
		return("IPSEC IKE auth fail");
		break;
	case ERROR_IPSEC_IKE_ATTRIB_FAIL:
		return("IPSEC IKE attrib fail");
		break;
	case ERROR_IPSEC_IKE_NEGOTIATION_PENDING:
		return("IPSEC IKE negotiation pending");
		break;
	case ERROR_IPSEC_IKE_GENERAL_PROCESSING_ERROR:
		return("IPSEC IKE general processing error");
		break;
	case ERROR_IPSEC_IKE_TIMED_OUT:
		return("IPSEC IKE timed out");
		break;
	case ERROR_IPSEC_IKE_NO_CERT:
		return("IPSEC IKE no cert");
		break;
	case ERROR_IPSEC_IKE_SA_DELETED:
		return("IPSEC IKE sa deleted");
		break;
	case ERROR_IPSEC_IKE_SA_REAPED:
		return("IPSEC IKE sa reaped");
		break;
	case ERROR_IPSEC_IKE_MM_ACQUIRE_DROP:
		return("IPSEC IKE mm acquire drop");
		break;
	case ERROR_IPSEC_IKE_QM_ACQUIRE_DROP:
		return("IPSEC IKE qm acquire drop");
		break;
	case ERROR_IPSEC_IKE_QUEUE_DROP_MM:
		return("IPSEC IKE queue drop mm");
		break;
	case ERROR_IPSEC_IKE_QUEUE_DROP_NO_MM:
		return("IPSEC IKE queue drop no mm");
		break;
	case ERROR_IPSEC_IKE_DROP_NO_RESPONSE:
		return("IPSEC IKE drop no response");
		break;
	case ERROR_IPSEC_IKE_MM_DELAY_DROP:
		return("IPSEC IKE mm delay drop");
		break;
	case ERROR_IPSEC_IKE_QM_DELAY_DROP:
		return("IPSEC IKE qm delay drop");
		break;
	case ERROR_IPSEC_IKE_ERROR:
		return("IPSEC IKE error");
		break;
	case ERROR_IPSEC_IKE_CRL_FAILED:
		return("IPSEC IKE crl failed");
		break;
	case ERROR_IPSEC_IKE_INVALID_KEY_USAGE:
		return("IPSEC IKE invalid key usage");
		break;
	case ERROR_IPSEC_IKE_INVALID_CERT_TYPE:
		return("IPSEC IKE invalid cert type");
		break;
	case ERROR_IPSEC_IKE_NO_PRIVATE_KEY:
		return("IPSEC IKE no private key");
		break;
	case ERROR_IPSEC_IKE_DH_FAIL:
		return("IPSEC IKE dh fail");
		break;
	case ERROR_IPSEC_IKE_INVALID_HEADER:
		return("IPSEC IKE invalid header");
		break;
	case ERROR_IPSEC_IKE_NO_POLICY:
		return("IPSEC IKE no policy");
		break;
	case ERROR_IPSEC_IKE_INVALID_SIGNATURE:
		return("IPSEC IKE invalid signature");
		break;
	case ERROR_IPSEC_IKE_KERBEROS_ERROR:
		return("IPSEC IKE kerberos error");
		break;
	case ERROR_IPSEC_IKE_NO_PUBLIC_KEY:
		return("IPSEC IKE no public key");
		break;
	case ERROR_IPSEC_IKE_PROCESS_ERR:
		return("IPSEC IKE process err");
		break;
	case ERROR_IPSEC_IKE_PROCESS_ERR_SA:
		return("IPSEC IKE process err sa");
		break;
	case ERROR_IPSEC_IKE_PROCESS_ERR_PROP:
		return("IPSEC IKE process err prop");
		break;
	case ERROR_IPSEC_IKE_PROCESS_ERR_TRANS:
		return("IPSEC IKE process err trans");
		break;
	case ERROR_IPSEC_IKE_PROCESS_ERR_KE:
		return("IPSEC IKE process err ke");
		break;
	case ERROR_IPSEC_IKE_PROCESS_ERR_ID:
		return("IPSEC IKE process err ID");
		break;
	case ERROR_IPSEC_IKE_PROCESS_ERR_CERT:
		return("IPSEC IKE process err cert");
		break;
	case ERROR_IPSEC_IKE_PROCESS_ERR_CERT_REQ:
		return("IPSEC IKE process err cert req");
		break;
	case ERROR_IPSEC_IKE_PROCESS_ERR_HASH:
		return("IPSEC IKE process err hash");
		break;
	case ERROR_IPSEC_IKE_PROCESS_ERR_SIG:
		return("IPSEC IKE process err sig");
		break;
	case ERROR_IPSEC_IKE_PROCESS_ERR_NONCE:
		return("IPSEC IKE process err nonce");
		break;
	case ERROR_IPSEC_IKE_PROCESS_ERR_NOTIFY:
		return("IPSEC IKE process err notify");
		break;
	case ERROR_IPSEC_IKE_PROCESS_ERR_DELETE:
		return("IPSEC IKE process err delete");
		break;
	case ERROR_IPSEC_IKE_PROCESS_ERR_VENDOR:
		return("IPSEC IKE process err vendor");
		break;
	case ERROR_IPSEC_IKE_INVALID_PAYLOAD:
		return("IPSEC IKE invalid payload");
		break;
	case ERROR_IPSEC_IKE_LOAD_SOFT_SA:
		return("IPSEC IKE load soft sa");
		break;
	case ERROR_IPSEC_IKE_SOFT_SA_TORN_DOWN:
		return("IPSEC IKE soft sa torn down");
		break;
	case ERROR_IPSEC_IKE_INVALID_COOKIE:
		return("IPSEC IKE invalid cookie");
		break;
	case ERROR_IPSEC_IKE_NO_PEER_CERT:
		return("IPSEC IKE no peer cert");
		break;
	case ERROR_IPSEC_IKE_PEER_CRL_FAILED:
		return("IPSEC IKE peer CRL failed");
		break;
	case ERROR_IPSEC_IKE_POLICY_CHANGE:
		return("IPSEC IKE policy change");
		break;
	case ERROR_IPSEC_IKE_NO_MM_POLICY:
		return("IPSEC IKE no mm policy");
		break;
	case ERROR_IPSEC_IKE_NOTCBPRIV:
		return("IPSEC IKE notcbpriv");
		break;
	case ERROR_IPSEC_IKE_SECLOADFAIL:
		return("IPSEC IKE secloadfail");
		break;
	case ERROR_IPSEC_IKE_FAILSSPINIT:
		return("IPSEC IKE failsspinit");
		break;
	case ERROR_IPSEC_IKE_FAILQUERYSSP:
		return("IPSEC IKE failqueryssp");
		break;
	case ERROR_IPSEC_IKE_SRVACQFAIL:
		return("IPSEC IKE srvacqfail");
		break;
	case ERROR_IPSEC_IKE_SRVQUERYCRED:
		return("IPSEC IKE srvquerycred");
		break;
	case ERROR_IPSEC_IKE_GETSPIFAIL:
		return("IPSEC IKE getspifail");
		break;
	case ERROR_IPSEC_IKE_INVALID_FILTER:
		return("IPSEC IKE invalid filter");
		break;
	case ERROR_IPSEC_IKE_OUT_OF_MEMORY:
		return("IPSEC IKE out of memory");
		break;
	case ERROR_IPSEC_IKE_ADD_UPDATE_KEY_FAILED:
		return("IPSEC IKE add update key failed");
		break;
	case ERROR_IPSEC_IKE_INVALID_POLICY:
		return("IPSEC IKE invalid policy");
		break;
	case ERROR_IPSEC_IKE_UNKNOWN_DOI:
		return("IPSEC IKE unknown doi");
		break;
	case ERROR_IPSEC_IKE_INVALID_SITUATION:
		return("IPSEC IKE invalid situation");
		break;
	case ERROR_IPSEC_IKE_DH_FAILURE:
		return("IPSEC IKE dh failure");
		break;
	case ERROR_IPSEC_IKE_INVALID_GROUP:
		return("IPSEC IKE invalid group");
		break;
	case ERROR_IPSEC_IKE_ENCRYPT:
		return("IPSEC IKE encrypt");
		break;
	case ERROR_IPSEC_IKE_DECRYPT:
		return("IPSEC IKE decrypt");
		break;
	case ERROR_IPSEC_IKE_POLICY_MATCH:
		return("IPSEC IKE policy match");
		break;
	case ERROR_IPSEC_IKE_UNSUPPORTED_ID:
		return("IPSEC IKE unsupported ID");
		break;
	case ERROR_IPSEC_IKE_INVALID_HASH:
		return("IPSEC IKE invalid hash");
		break;
	case ERROR_IPSEC_IKE_INVALID_HASH_ALG:
		return("IPSEC IKE invalid hash alg");
		break;
	case ERROR_IPSEC_IKE_INVALID_HASH_SIZE:
		return("IPSEC IKE invalid hash size");
		break;
	case ERROR_IPSEC_IKE_INVALID_ENCRYPT_ALG:
		return("IPSEC IKE invalid encrypt alg");
		break;
	case ERROR_IPSEC_IKE_INVALID_AUTH_ALG:
		return("IPSEC IKE invalid auth alg");
		break;
	case ERROR_IPSEC_IKE_INVALID_SIG:
		return("IPSEC IKE invalid sig");
		break;
	case ERROR_IPSEC_IKE_LOAD_FAILED:
		return("IPSEC IKE load failed");
		break;
	case ERROR_IPSEC_IKE_RPC_DELETE:
		return("IPSEC IKE rpc delete");
		break;
	case ERROR_IPSEC_IKE_BENIGN_REINIT:
		return("IPSEC IKE benign reinit");
		break;
	case ERROR_IPSEC_IKE_INVALID_RESPONDER_LIFETIME_NOTIFY:
		return("IPSEC IKE invalid responder lifetime notify");
		break;
	case ERROR_IPSEC_IKE_INVALID_CERT_KEYLEN:
		return("IPSEC IKE invalid cert keylen");
		break;
	case ERROR_IPSEC_IKE_MM_LIMIT:
		return("IPSEC IKE mm limit");
		break;
	case ERROR_IPSEC_IKE_NEGOTIATION_DISABLED:
		return("IPSEC IKE negotiation disabled");
		break;
	case ERROR_IPSEC_IKE_NEG_STATUS_END:
		return("IPSEC IKE neg status end");
		break;
	case ERROR_SXS_SECTION_NOT_FOUND:
		return("Sxs section not found");
		break;
	case ERROR_SXS_CANT_GEN_ACTCTX:
		return("Sxs can't gen actctx");
		break;
	case ERROR_SXS_INVALID_ACTCTXDATA_FORMAT:
		return("Sxs invalid actctxdata format");
		break;
	case ERROR_SXS_ASSEMBLY_NOT_FOUND:
		return("Sxs assembly not found");
		break;
	case ERROR_SXS_MANIFEST_FORMAT_ERROR:
		return("Sxs manifest format error");
		break;
	case ERROR_SXS_MANIFEST_PARSE_ERROR:
		return("Sxs manifest parse error");
		break;
	case ERROR_SXS_ACTIVATION_CONTEXT_DISABLED:
		return("Sxs activation context disabled");
		break;
	case ERROR_SXS_KEY_NOT_FOUND:
		return("Sxs key not found");
		break;
	case ERROR_SXS_VERSION_CONFLICT:
		return("Sxs version conflict");
		break;
	case ERROR_SXS_WRONG_SECTION_TYPE:
		return("Sxs wrong section type");
		break;
	case ERROR_SXS_THREAD_QUERIES_DISABLED:
		return("Sxs thread queries disabled");
		break;
	case ERROR_SXS_PROCESS_DEFAULT_ALREADY_SET:
		return("Sxs process default already set");
		break;
	case ERROR_SXS_UNKNOWN_ENCODING_GROUP:
		return("Sxs unknown encoding group");
		break;
	case ERROR_SXS_UNKNOWN_ENCODING:
		return("Sxs unknown encoding");
		break;
	case ERROR_SXS_INVALID_XML_NAMESPACE_URI:
		return("Sxs invalid XML namespace URI");
		break;
	case ERROR_SXS_ROOT_MANIFEST_DEPENDENCY_NOT_INSTALLED:
		return("Sxs root manifest dependency not installed");
		break;
	case ERROR_SXS_LEAF_MANIFEST_DEPENDENCY_NOT_INSTALLED:
		return("Sxs leaf manifest dependency not installed");
		break;
	case ERROR_SXS_INVALID_ASSEMBLY_IDENTITY_ATTRIBUTE:
		return("Sxs invalid assembly indentity attribute");
		break;
	case ERROR_SXS_MANIFEST_MISSING_REQUIRED_DEFAULT_NAMESPACE:
		return("Sxs manifest missing required default namespace");
		break;
	case ERROR_SXS_MANIFEST_INVALID_REQUIRED_DEFAULT_NAMESPACE:
		return("Sxs manifest invalid required default namespace");
		break;
	case ERROR_SXS_PRIVATE_MANIFEST_CROSS_PATH_WITH_REPARSE_POINT:
		return("Sxs private manifest cross path with reparse point");
		break;
	case ERROR_SXS_DUPLICATE_DLL_NAME:
		return("Sxs duplicate dll name");
		break;
	case ERROR_SXS_DUPLICATE_WINDOWCLASS_NAME:
		return("Sxs duplicate windowclass name");
		break;
	case ERROR_SXS_DUPLICATE_CLSID:
		return("Sxs duplicate clsid");
		break;
	case ERROR_SXS_DUPLICATE_IID:
		return("Sxs duplicate iid");
		break;
	case ERROR_SXS_DUPLICATE_TLBID:
		return("Sxs duplicate tlbid");
		break;
	case ERROR_SXS_DUPLICATE_PROGID:
		return("Sxs duplicate progid");
		break;
	case ERROR_SXS_DUPLICATE_ASSEMBLY_NAME:
		return("Sxs duplicate assembly name");
		break;
	case ERROR_SXS_FILE_HASH_MISMATCH:
		return("Sxs file hash mismatch");
		break;
	case ERROR_SXS_POLICY_PARSE_ERROR:
		return("Sxs policy parse error");
		break;
	case ERROR_SXS_XML_E_MISSINGQUOTE:
		return("Sxs XML e missingquote");
		break;
	case ERROR_SXS_XML_E_COMMENTSYNTAX:
		return("Sxs XML e commentsyntax");
		break;
	case ERROR_SXS_XML_E_BADSTARTNAMECHAR:
		return("Sxs XML e badstartnamechar");
		break;
	case ERROR_SXS_XML_E_BADNAMECHAR:
		return("Sxs XML e badnamechar");
		break;
	case ERROR_SXS_XML_E_BADCHARINSTRING:
		return("Sxs XML e badcharinstring");
		break;
	case ERROR_SXS_XML_E_XMLDECLSYNTAX:
		return("Sxs XML e xmldeclsyntax");
		break;
	case ERROR_SXS_XML_E_BADCHARDATA:
		return("Sxs XML e badchardata");
		break;
	case ERROR_SXS_XML_E_MISSINGWHITESPACE:
		return("Sxs XML e missingwhitespace");
		break;
	case ERROR_SXS_XML_E_EXPECTINGTAGEND:
		return("Sxs XML e expectingtagend");
		break;
	case ERROR_SXS_XML_E_MISSINGSEMICOLON:
		return("Sxs XML e missingsemicolon");
		break;
	case ERROR_SXS_XML_E_UNBALANCEDPAREN:
		return("Sxs XML e unbalancedparen");
		break;
	case ERROR_SXS_XML_E_INTERNALERROR:
		return("Sxs XML e internalerror");
		break;
	case ERROR_SXS_XML_E_UNEXPECTED_WHITESPACE:
		return("Sxs XML e unexpected whitespace");
		break;
	case ERROR_SXS_XML_E_INCOMPLETE_ENCODING:
		return("Sxs XML e incomplete encoding");
		break;
	case ERROR_SXS_XML_E_MISSING_PAREN:
		return("Sxs XML e missing paren");
		break;
	case ERROR_SXS_XML_E_EXPECTINGCLOSEQUOTE:
		return("Sxs XML e expectingclosequote");
		break;
	case ERROR_SXS_XML_E_MULTIPLE_COLONS:
		return("Sxs XML e multiple colons");
		break;
	case ERROR_SXS_XML_E_INVALID_DECIMAL:
		return("Sxs XML e invalid decimal");
		break;
	case ERROR_SXS_XML_E_INVALID_HEXIDECIMAL:
		return("Sxs XML e invalid hexidecimal");
		break;
	case ERROR_SXS_XML_E_INVALID_UNICODE:
		return("Sxs XML e invalid unicode");
		break;
	case ERROR_SXS_XML_E_WHITESPACEORQUESTIONMARK:
		return("Sxs XML e whitespaceorquestionmark");
		break;
	case ERROR_SXS_XML_E_UNEXPECTEDENDTAG:
		return("Sxs XML e unexpectedendtag");
		break;
	case ERROR_SXS_XML_E_UNCLOSEDTAG:
		return("Sxs XML e unclosedtag");
		break;
	case ERROR_SXS_XML_E_DUPLICATEATTRIBUTE:
		return("Sxs XML e duplicateattribute");
		break;
	case ERROR_SXS_XML_E_MULTIPLEROOTS:
		return("Sxs XML e multipleroots");
		break;
	case ERROR_SXS_XML_E_INVALIDATROOTLEVEL:
		return("Sxs XML e invalidatrootlevel");
		break;
	case ERROR_SXS_XML_E_BADXMLDECL:
		return("Sxs XML e badxmldecl");
		break;
	case ERROR_SXS_XML_E_MISSINGROOT:
		return("Sxs XML e missingroot");
		break;
	case ERROR_SXS_XML_E_UNEXPECTEDEOF:
		return("Sxs XML e unexpectedeof");
		break;
	case ERROR_SXS_XML_E_BADPEREFINSUBSET:
		return("Sxs XML e badperefinsubset");
		break;
	case ERROR_SXS_XML_E_UNCLOSEDSTARTTAG:
		return("Sxs XML e unclosedstarttag");
		break;
	case ERROR_SXS_XML_E_UNCLOSEDENDTAG:
		return("Sxs XML e unclosedendtag");
		break;
	case ERROR_SXS_XML_E_UNCLOSEDSTRING:
		return("Sxs XML e unclosedstring");
		break;
	case ERROR_SXS_XML_E_UNCLOSEDCOMMENT:
		return("Sxs XML e unclosedcomment");
		break;
	case ERROR_SXS_XML_E_UNCLOSEDDECL:
		return("Sxs XML e uncloseddecl");
		break;
	case ERROR_SXS_XML_E_UNCLOSEDCDATA:
		return("Sxs XML e unclosedcdata");
		break;
	case ERROR_SXS_XML_E_RESERVEDNAMESPACE:
		return("Sxs XML e reservednamespace");
		break;
	case ERROR_SXS_XML_E_INVALIDENCODING:
		return("Sxs XML e invalidencoding");
		break;
	case ERROR_SXS_XML_E_INVALIDSWITCH:
		return("Sxs XML e invalidswitch");
		break;
	case ERROR_SXS_XML_E_BADXMLCASE:
		return("Sxs XML e badxmlcase");
		break;
	case ERROR_SXS_XML_E_INVALID_STANDALONE:
		return("Sxs XML e invalid standalone");
		break;
	case ERROR_SXS_XML_E_UNEXPECTED_STANDALONE:
		return("Sxs XML e unexpected standalone");
		break;
	case ERROR_SXS_XML_E_INVALID_VERSION:
		return("Sxs XML e invalid version");
		break;
	case ERROR_SXS_XML_E_MISSINGEQUALS:
		return("Sxs XML e missingequals");
		break;
	case ERROR_SXS_PROTECTION_RECOVERY_FAILED:
		return("Sxs protection recovery failed");
		break;
	case ERROR_SXS_PROTECTION_PUBLIC_KEY_TOO_SHORT:
		return("Sxs protection public key too short");
		break;
	case ERROR_SXS_PROTECTION_CATALOG_NOT_VALID:
		return("Sxs protection catalog not valid");
		break;
	case ERROR_SXS_UNTRANSLATABLE_HRESULT:
		return("Sxs untranslatable hresult");
		break;
	case ERROR_SXS_PROTECTION_CATALOG_FILE_MISSING:
		return("Sxs protection catalog file missing");
		break;
	case ERROR_SXS_MISSING_ASSEMBLY_IDENTITY_ATTRIBUTE:
		return("Sxs missing assembly identity attribute");
		break;
	case ERROR_SXS_INVALID_ASSEMBLY_IDENTITY_ATTRIBUTE_NAME:
		return("Sxs invalid assembly identity attribute name");
		break;
	default:
		return("Unknown error");
	}
}

