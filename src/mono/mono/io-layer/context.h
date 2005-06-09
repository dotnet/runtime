/*
 * context.h:  Processor-specific register contexts
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef _WAPI_CONTEXT_H_
#define _WAPI_CONTEXT_H_

#include <glib.h>

#include "mono/io-layer/wapi.h"

/* This part is x86-specific.  MSDN states that CONTEXT is defined
 * also for MIPS, Alpha and PPC processors.
 */

#define SIZE_OF_80387_REGISTERS 80

#define CONTEXT_i386 0x00010000
#define CONTEXT_i486 0x00010000

#define CONTEXT_CONTROL			(CONTEXT_i386 | 0x00000001L)
#define CONTEXT_INTEGER			(CONTEXT_i386 | 0x00000002L)
#define CONTEXT_SEGMENTS		(CONTEXT_i386 | 0x00000004L)
#define CONTEXT_FLOATING_POINT		(CONTEXT_i386 | 0x00000008L)
#define CONTEXT_DEBUG_REGISTERS		(CONTEXT_i386 | 0x00000010L)
#define CONTEXT_EXTENDED_REGISTERS	(CONTEXT_i386 | 0x00000020L)

#define CONTEXT_FULL (CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_SEGMENTS)

#define MAXIMUM_SUPPORTED_EXTENSION 512

typedef struct 
{
	guint32 ControlWord;
	guint32 StatusWord;
	guint32 TagWord;
	guint32 ErrorOffset;
	guint32 ErrorSelector;
	guint32 DataOffset;
	guint32 DataSelector;
	guint8 RegisterArea[SIZE_OF_80387_REGISTERS];
	guint32 Cr0NpxState;
} WapiFloatingSaveArea;

typedef struct 
{
	guint32 ContextFlags;
	guint32 Dr0;
	guint32 Dr1;
	guint32 Dr2;
	guint32 Dr3;
	guint32 Dr6;
	guint32 Dr7;
	
	WapiFloatingSaveArea FloatSave;
	
	guint32 SegGs;
	guint32 SegFs;
	guint32 SegEs;
	guint32 SegDs;
	
	guint32 Edi;
	guint32 Esi;
	guint32 Ebx;
	guint32 Edx;
	guint32 Ecx;
	guint32 Eax;
	
	guint32 Ebp;
	guint32 Eip;
	guint32 SegCs;
	guint32 EFlags;
	guint32 Esp;
	guint32 SegSs;
	
	guint8 ExtendedRegisters[MAXIMUM_SUPPORTED_EXTENSION];
} WapiContext;

G_BEGIN_DECLS

extern gboolean GetThreadContext(gpointer handle, WapiContext *context);

G_END_DECLS

#endif /* _WAPI_COMPEX_H_ */
