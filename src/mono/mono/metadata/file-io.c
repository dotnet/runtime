/*
 * file-io.c: File IO internal calls
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>

#include <mono/metadata/object.h>
#include <mono/io-layer/io-layer.h>
#include <mono/metadata/file-io.h>
#include <mono/metadata/exception.h>

#define DEBUG

static guint32 convert_mode(MonoFileMode mono_mode)
{
	guint32 mode;

	switch(mono_mode) {
	case FileMode_CreateNew:
		mode=CREATE_NEW;
		break;
	case FileMode_Create:
		mode=CREATE_ALWAYS;
		break;
	case FileMode_Open:
		mode=OPEN_EXISTING;
		break;
	case FileMode_OpenOrCreate:
		mode=OPEN_ALWAYS;
		break;
	case FileMode_Truncate:
		mode=TRUNCATE_EXISTING;
		break;
	case FileMode_Append:
		mode=OPEN_ALWAYS;
		break;
	default:
		g_warning("System.IO.FileMode has unknown value 0x%x",
			  mono_mode);
		/* Safe fallback */
		mode=OPEN_EXISTING;
	}
	
	return(mode);
}

static guint32 convert_access(MonoFileAccess mono_access)
{
	guint32 access;
	
	switch(mono_access) {
	case FileAccess_Read:
		access=GENERIC_READ;
		break;
	case FileAccess_Write:
		access=GENERIC_WRITE;
		break;
	case FileAccess_ReadWrite:
		access=GENERIC_READ|GENERIC_WRITE;
		break;
	default:
		g_warning("System.IO.FileAccess has unknown value 0x%x",
			  mono_access);
		/* Safe fallback */
		access=GENERIC_READ;
	}
	
	return(access);
}

static guint32 convert_share(MonoFileShare mono_share)
{
	guint32 share;
	
	switch(mono_share) {
	case FileShare_None:
		share=0;
		break;
	case FileShare_Read:
		share=FILE_SHARE_READ;
		break;
	case FileShare_Write:
		share=FILE_SHARE_WRITE;
		break;
	case FileShare_ReadWrite:
		share=FILE_SHARE_READ|FILE_SHARE_WRITE;
		break;
	default:
		g_warning("System.IO.FileShare has unknown value 0x%x",
			  mono_share);
		/* Safe fallback */
		share=0;
	}
	
	return(share);
}

static guint32 convert_stdhandle(guint32 fd)
{
	guint32 stdhandle;
	
	switch(fd) {
	case 0:
		stdhandle=STD_INPUT_HANDLE;
		break;
	case 1:
		stdhandle=STD_OUTPUT_HANDLE;
		break;
	case 2:
		stdhandle=STD_ERROR_HANDLE;
		break;
	default:
		g_warning("unknown standard file descriptor %d", fd);
		stdhandle=STD_INPUT_HANDLE;
	}
	
	return(stdhandle);
}

static guint32 convert_seekorigin(MonoSeekOrigin origin)
{
	guint32 w32origin;
	
	switch(origin) {
	case SeekOrigin_Begin:
		w32origin=FILE_BEGIN;
		break;
	case SeekOrigin_Current:
		w32origin=FILE_CURRENT;
		break;
	case SeekOrigin_End:
		w32origin=FILE_END;
		break;
	default:
		g_warning("System.IO.SeekOrigin has unknown value 0x%x",
			  origin);
		/* Safe fallback */
		w32origin=FILE_CURRENT;
	}
	
	return(w32origin);
}

static MonoException *get_io_exception(const guchar *msg)
{
	static MonoException *ex = NULL;

	if(ex==NULL) {
		ex=(MonoException *)mono_exception_from_name(
			mono_defaults.corlib, "System.IO", "IOException");
	}

	ex->message=mono_string_new(msg);
	
	return(ex);
}

/* fd must be one of stdin (value 0), stdout (1) or stderr (2).  These
 * values must be hardcoded in corlib.
 */
HANDLE ves_icall_System_PAL_OpSys_GetStdHandle(MonoObject *this,
					       gint32 fd)
{
	HANDLE handle;
	
	if(fd!=0 && fd!=1 && fd!=2) {
		mono_raise_exception(
			get_io_exception("Invalid file descriptor"));
	}
	
	handle=GetStdHandle(convert_stdhandle(fd));
	
	return(handle);
}

gint32 ves_icall_System_PAL_OpSys_ReadFile(MonoObject *this, HANDLE handle, MonoArray *buffer, gint32 offset, gint32 count)
{
	gboolean ret;
	guint32 bytesread;
	guchar *buf;
	gint32 alen;
	
	alen=mono_array_length(buffer);
	if(offset+count>alen) {
		return(0);
	}
	
	buf=mono_array_addr(buffer, guchar, offset);
	
	ret=ReadFile(handle, buf, count, &bytesread, NULL);
	
	return(bytesread);
}

gint32 ves_icall_System_PAL_OpSys_WriteFile(MonoObject *this, HANDLE handle,  MonoArray *buffer, gint32 offset, gint32 count)
{
	gboolean ret;
	guint32 byteswritten;
	guchar *buf;
	gint32 alen;
	
	alen=mono_array_length(buffer);
	if(offset+count>alen) {
		return(0);
	}
	
	buf=mono_array_addr(buffer, guchar, offset);
	
	ret=WriteFile(handle, buf, count, &byteswritten, NULL);
	
	return(byteswritten);
}

gint32 ves_icall_System_PAL_OpSys_SetLengthFile(MonoObject *this, HANDLE handle, gint64 length)
{
	/* FIXME: Should this put the file pointer back to where it
	 * was before we started setting the length? The spec doesnt
	 * say, as usual
	 */

	gboolean ret;
	gint32 lenlo, lenhi, retlo;
	
	lenlo=length & 0xFFFFFFFF;
	lenhi=length >> 32;

	retlo=SetFilePointer(handle, lenlo, &lenhi, FILE_BEGIN);
	ret=SetEndOfFile(handle);
	
	if(ret==FALSE) {
		mono_raise_exception(get_io_exception("IO Exception"));
	}
	
	return(0);
}

HANDLE ves_icall_System_PAL_OpSys_OpenFile(MonoObject *this, MonoString *path, gint32 mode, gint32 access, gint32 share)
{
	HANDLE handle;
	
	handle=CreateFile(mono_string_chars(path), convert_access(access),
			  convert_share(share), NULL, convert_mode(mode),
			  FILE_ATTRIBUTE_NORMAL, NULL);
	
	return(handle);
}

void ves_icall_System_PAL_OpSys_CloseFile(MonoObject *this, HANDLE handle)
{
	CloseHandle(handle);
}

gint64 ves_icall_System_PAL_OpSys_SeekFile(MonoObject *this, HANDLE handle,
					   gint64 offset, gint32 origin)
{
	gint64 ret;
	gint32 offsetlo, offsethi, retlo;
	
	offsetlo=offset & 0xFFFFFFFF;
	offsethi=offset >> 32;

	retlo=SetFilePointer(handle, offset, &offsethi,
			     convert_seekorigin(origin));
	
	ret=((gint64)offsethi << 32) + offsetlo;

	return(ret);
}

void ves_icall_System_PAL_OpSys_DeleteFile(MonoObject *this, MonoString *path)
{
	DeleteFile(mono_string_chars(path));
}

gboolean ves_icall_System_PAL_OpSys_ExistsFile(MonoObject *this, MonoString *path)
{
	return(FALSE);
}

gboolean ves_icall_System_PAL_OpSys_GetFileTime(HANDLE handle, gint64 *createtime, gint64 *lastaccess, gint64 *lastwrite)
{
	gboolean ret;
	FILETIME cr, ac, wr;
	
	ret=GetFileTime(handle, &cr, &ac, &wr);
	if(ret==TRUE) {
		/* The FILETIME struct holds two unsigned 32 bit
		 * values for the low and high bytes, but the .net
		 * file time insists on being signed :(
		 */
		*createtime=((gint64)cr.dwHighDateTime << 32) +
			cr.dwLowDateTime;
		*lastaccess=((gint64)ac.dwHighDateTime << 32) +
			ac.dwLowDateTime;
		*lastwrite=((gint64)wr.dwHighDateTime << 32) +
			wr.dwLowDateTime;
	}
	
	return(ret);
}

gboolean ves_icall_System_PAL_OpSys_SetFileTime(HANDLE handle, gint64 createtime, gint64 lastaccess, gint64 lastwrite)
{
	gboolean ret;
	FILETIME cr, ac, wr;
	
	cr.dwLowDateTime= createtime & 0xFFFFFFFF;
	cr.dwHighDateTime= createtime >> 32;
	
	ac.dwLowDateTime= lastaccess & 0xFFFFFFFF;
	ac.dwHighDateTime= lastaccess >> 32;
	
	wr.dwLowDateTime= lastwrite & 0xFFFFFFFF;
	wr.dwHighDateTime= lastwrite >> 32;

	ret=SetFileTime(handle, &cr, &ac, &wr);
	
	return(ret);
}

