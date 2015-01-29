/*
 * sgen-protocol.h: Binary protocol of internal activity, to aid
 * debugging.
 *
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
 * Copyright (C) 2012 Xamarin Inc
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Library General Public
 * License 2.0 as published by the Free Software Foundation;
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Library General Public License for more details.
 *
 * You should have received a copy of the GNU Library General Public
 * License 2.0 along with this library; if not, write to the Free
 * Software Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
 */

#ifndef __MONO_SGENPROTOCOL_H__
#define __MONO_SGENPROTOCOL_H__

#include "sgen-gc.h"

/* Special indices returned by MATCH_INDEX. */
#define BINARY_PROTOCOL_NO_MATCH (-1)
#define BINARY_PROTOCOL_MATCH (-2)

#define PROTOCOL_ID(method) method ## _id
#define PROTOCOL_STRUCT(method) method ## _struct

#define TYPE_INT int
#define TYPE_LONGLONG long long
#define TYPE_SIZE size_t
#define TYPE_POINTER gpointer

enum {
#define BEGIN_PROTOCOL_ENTRY0(method) PROTOCOL_ID(method),
#define BEGIN_PROTOCOL_ENTRY1(method,t1,f1) PROTOCOL_ID(method),
#define BEGIN_PROTOCOL_ENTRY2(method,t1,f1,t2,f2) PROTOCOL_ID(method),
#define BEGIN_PROTOCOL_ENTRY3(method,t1,f1,t2,f2,t3,f3) PROTOCOL_ID(method),
#define BEGIN_PROTOCOL_ENTRY4(method,t1,f1,t2,f2,t3,f3,t4,f4) PROTOCOL_ID(method),
#define BEGIN_PROTOCOL_ENTRY5(method,t1,f1,t2,f2,t3,f3,t4,f4,t5,f5) PROTOCOL_ID(method),
#define BEGIN_PROTOCOL_ENTRY6(method,t1,f1,t2,f2,t3,f3,t4,f4,t5,f5,t6,f6) PROTOCOL_ID(method),
#define BEGIN_PROTOCOL_ENTRY_HEAVY0(method) PROTOCOL_ID(method),
#define BEGIN_PROTOCOL_ENTRY_HEAVY1(method,t1,f1) PROTOCOL_ID(method),
#define BEGIN_PROTOCOL_ENTRY_HEAVY2(method,t1,f1,t2,f2) PROTOCOL_ID(method),
#define BEGIN_PROTOCOL_ENTRY_HEAVY3(method,t1,f1,t2,f2,t3,f3) PROTOCOL_ID(method),
#define BEGIN_PROTOCOL_ENTRY_HEAVY4(method,t1,f1,t2,f2,t3,f3,t4,f4) PROTOCOL_ID(method),
#define BEGIN_PROTOCOL_ENTRY_HEAVY5(method,t1,f1,t2,f2,t3,f3,t4,f4,t5,f5) PROTOCOL_ID(method),
#define BEGIN_PROTOCOL_ENTRY_HEAVY6(method,t1,f1,t2,f2,t3,f3,t4,f4,t5,f5,t6,f6) PROTOCOL_ID(method),

#define FLUSH()

#define DEFAULT_PRINT()
#define CUSTOM_PRINT(_)

#define IS_ALWAYS_MATCH(_)
#define MATCH_INDEX(_)
#define IS_VTABLE_MATCH(_)

#define END_PROTOCOL_ENTRY
#define END_PROTOCOL_ENTRY_HEAVY

#include "sgen-protocol-def.h"
};

#define BEGIN_PROTOCOL_ENTRY0(method)
#define BEGIN_PROTOCOL_ENTRY1(method,t1,f1) \
	typedef struct { \
		t1 f1; \
	} PROTOCOL_STRUCT(method);
#define BEGIN_PROTOCOL_ENTRY2(method,t1,f1,t2,f2) \
	typedef struct { \
		t1 f1; \
		t2 f2; \
	} PROTOCOL_STRUCT(method);
#define BEGIN_PROTOCOL_ENTRY3(method,t1,f1,t2,f2,t3,f3) \
	typedef struct { \
		t1 f1; \
		t2 f2; \
		t3 f3; \
	} PROTOCOL_STRUCT(method);
#define BEGIN_PROTOCOL_ENTRY4(method,t1,f1,t2,f2,t3,f3,t4,f4) \
	typedef struct { \
		t1 f1; \
		t2 f2; \
		t3 f3; \
		t4 f4; \
	} PROTOCOL_STRUCT(method);
#define BEGIN_PROTOCOL_ENTRY5(method,t1,f1,t2,f2,t3,f3,t4,f4,t5,f5) \
	typedef struct { \
		t1 f1; \
		t2 f2; \
		t3 f3; \
		t4 f4; \
		t5 f5; \
	} PROTOCOL_STRUCT(method);
#define BEGIN_PROTOCOL_ENTRY6(method,t1,f1,t2,f2,t3,f3,t4,f4,t5,f5,t6,f6) \
	typedef struct { \
		t1 f1; \
		t2 f2; \
		t3 f3; \
		t4 f4; \
		t5 f5; \
		t6 f6; \
	} PROTOCOL_STRUCT(method);

#define BEGIN_PROTOCOL_ENTRY_HEAVY0(method) \
	BEGIN_PROTOCOL_ENTRY0 (method)
#define BEGIN_PROTOCOL_ENTRY_HEAVY1(method,t1,f1) \
	BEGIN_PROTOCOL_ENTRY1 (method,t1,f1)
#define BEGIN_PROTOCOL_ENTRY_HEAVY2(method,t1,f1,t2,f2) \
	BEGIN_PROTOCOL_ENTRY2 (method,t1,f1,t2,f2)
#define BEGIN_PROTOCOL_ENTRY_HEAVY3(method,t1,f1,t2,f2,t3,f3) \
	BEGIN_PROTOCOL_ENTRY3 (method,t1,f1,t2,f2,t3,f3)
#define BEGIN_PROTOCOL_ENTRY_HEAVY4(method,t1,f1,t2,f2,t3,f3,t4,f4) \
	BEGIN_PROTOCOL_ENTRY4 (method,t1,f1,t2,f2,t3,f3,t4,f4)
#define BEGIN_PROTOCOL_ENTRY_HEAVY5(method,t1,f1,t2,f2,t3,f3,t4,f4,t5,f5) \
	BEGIN_PROTOCOL_ENTRY5 (method,t1,f1,t2,f2,t3,f3,t4,f4,t5,f5)
#define BEGIN_PROTOCOL_ENTRY_HEAVY6(method,t1,f1,t2,f2,t3,f3,t4,f4,t5,f5,t6,f6) \
	BEGIN_PROTOCOL_ENTRY6 (method,t1,f1,t2,f2,t3,f3,t4,f4,t5,f5,t6,f6)

#define FLUSH()

#define DEFAULT_PRINT()
#define CUSTOM_PRINT(_)

#define IS_ALWAYS_MATCH(_)
#define MATCH_INDEX(_)
#define IS_VTABLE_MATCH(_)

#define END_PROTOCOL_ENTRY
#define END_PROTOCOL_ENTRY_HEAVY

#include "sgen-protocol-def.h"

/* missing: finalizers, roots, non-store wbarriers */

void binary_protocol_init (const char *filename, long long limit) MONO_INTERNAL;
gboolean binary_protocol_is_enabled (void) MONO_INTERNAL;

void binary_protocol_flush_buffers (gboolean force) MONO_INTERNAL;

#define BEGIN_PROTOCOL_ENTRY0(method) \
	void method (void) MONO_INTERNAL;
#define BEGIN_PROTOCOL_ENTRY1(method,t1,f1) \
	void method (t1 f1) MONO_INTERNAL;
#define BEGIN_PROTOCOL_ENTRY2(method,t1,f1,t2,f2) \
	void method (t1 f1, t2 f2) MONO_INTERNAL;
#define BEGIN_PROTOCOL_ENTRY3(method,t1,f1,t2,f2,t3,f3) \
	void method (t1 f1, t2 f2, t3 f3) MONO_INTERNAL;
#define BEGIN_PROTOCOL_ENTRY4(method,t1,f1,t2,f2,t3,f3,t4,f4) \
	void method (t1 f1, t2 f2, t3 f3, t4 f4) MONO_INTERNAL;
#define BEGIN_PROTOCOL_ENTRY5(method,t1,f1,t2,f2,t3,f3,t4,f4,t5,f5) \
	void method (t1 f1, t2 f2, t3 f3, t4 f4, t5 f5) MONO_INTERNAL;
#define BEGIN_PROTOCOL_ENTRY6(method,t1,f1,t2,f2,t3,f3,t4,f4,t5,f5,t6,f6) \
	void method (t1 f1, t2 f2, t3 f3, t4 f4, t5 f5, t6 f6) MONO_INTERNAL;

#ifdef SGEN_HEAVY_BINARY_PROTOCOL
#define binary_protocol_is_heavy_enabled()	binary_protocol_is_enabled ()

#define BEGIN_PROTOCOL_ENTRY_HEAVY0(method) \
	BEGIN_PROTOCOL_ENTRY0 (method)
#define BEGIN_PROTOCOL_ENTRY_HEAVY1(method,t1,f1) \
	BEGIN_PROTOCOL_ENTRY1 (method,t1,f1)
#define BEGIN_PROTOCOL_ENTRY_HEAVY2(method,t1,f1,t2,f2) \
	BEGIN_PROTOCOL_ENTRY2 (method,t1,f1,t2,f2)
#define BEGIN_PROTOCOL_ENTRY_HEAVY3(method,t1,f1,t2,f2,t3,f3) \
	BEGIN_PROTOCOL_ENTRY3 (method,t1,f1,t2,f2,t3,f3)
#define BEGIN_PROTOCOL_ENTRY_HEAVY4(method,t1,f1,t2,f2,t3,f3,t4,f4) \
	BEGIN_PROTOCOL_ENTRY4 (method,t1,f1,t2,f2,t3,f3,t4,f4)
#define BEGIN_PROTOCOL_ENTRY_HEAVY5(method,t1,f1,t2,f2,t3,f3,t4,f4,t5,f5) \
	BEGIN_PROTOCOL_ENTRY5 (method,t1,f1,t2,f2,t3,f3,t4,f4,t5,f5)
#define BEGIN_PROTOCOL_ENTRY_HEAVY6(method,t1,f1,t2,f2,t3,f3,t4,f4,t5,f5,t6,f6) \
	BEGIN_PROTOCOL_ENTRY6 (method,t1,f1,t2,f2,t3,f3,t4,f4,t5,f5,t6,f6)
#else
#define binary_protocol_is_heavy_enabled()	FALSE

#define BEGIN_PROTOCOL_ENTRY_HEAVY0(method) \
	static inline void method (void) {}
#define BEGIN_PROTOCOL_ENTRY_HEAVY1(method,t1,f1) \
	static inline void method (t1 f1) {}
#define BEGIN_PROTOCOL_ENTRY_HEAVY2(method,t1,f1,t2,f2) \
	static inline void method (t1 f1, t2 f2) {}
#define BEGIN_PROTOCOL_ENTRY_HEAVY3(method,t1,f1,t2,f2,t3,f3) \
	static inline void method (t1 f1, t2 f2, t3 f3) {}
#define BEGIN_PROTOCOL_ENTRY_HEAVY4(method,t1,f1,t2,f2,t3,f3,t4,f4) \
	static inline void method (t1 f1, t2 f2, t3 f3, t4 f4) {}
#define BEGIN_PROTOCOL_ENTRY_HEAVY5(method,t1,f1,t2,f2,t3,f3,t4,f4,t5,f5) \
	static inline void method (t1 f1, t2 f2, t3 f3, t4 f4, t5 f5) {}
#define BEGIN_PROTOCOL_ENTRY_HEAVY6(method,t1,f1,t2,f2,t3,f3,t4,f4,t5,f5,t6,f6) \
	static inline void method (t1 f1, t2 f2, t3 f3, t4 f4, t5 f5, t6 f6) {}
#endif

#define FLUSH()

#define DEFAULT_PRINT()
#define CUSTOM_PRINT(_)

#define IS_ALWAYS_MATCH(_)
#define MATCH_INDEX(_)
#define IS_VTABLE_MATCH(_)

#define END_PROTOCOL_ENTRY
#define END_PROTOCOL_ENTRY_HEAVY

#include "sgen-protocol-def.h"

#undef TYPE_INT
#undef TYPE_LONGLONG
#undef TYPE_SIZE
#undef TYPE_POINTER

#endif
