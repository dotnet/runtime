/**
 * \file
 * Binary protocol of internal activity, to aid debugging.
 *
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
 * Copyright (C) 2012 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef __MONO_SGENPROTOCOL_H__
#define __MONO_SGENPROTOCOL_H__

#include "sgen-gc.h"

#ifndef DISABLE_SGEN_BINARY_PROTOCOL

#define PROTOCOL_HEADER_CHECK 0xde7ec7ab1ec0de
/*
 * The version needs to be bumped every time we introduce breaking changes (like
 * adding new protocol entries or various format changes). The latest protocol grepper
 * should be able to handle all the previous versions, while an old grepper will
 * be able to tell if it cannot handle the format.
 */
#define PROTOCOL_HEADER_VERSION 2

/* Special indices returned by MATCH_INDEX. */
#define BINARY_PROTOCOL_NO_MATCH (-1)
#define BINARY_PROTOCOL_MATCH (-2)

#define PROTOCOL_ID(method) method ## _id
#define PROTOCOL_STRUCT(method) method ## _struct
#define CLIENT_PROTOCOL_NAME(method) sgen_client_ ## method

#ifndef TYPE_INT
#define TYPE_INT int
#endif
#ifndef TYPE_LONGLONG
#define TYPE_LONGLONG long long
#endif
#ifndef TYPE_SIZE
#define TYPE_SIZE size_t
#endif
#ifndef TYPE_POINTER
#define TYPE_POINTER gpointer
#endif
#ifndef TYPE_BOOL
#define TYPE_BOOL gboolean
#endif

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

#define DEFAULT_PRINT()
#define CUSTOM_PRINT(_)

#define IS_ALWAYS_MATCH(_)
#define MATCH_INDEX(_)
#define IS_VTABLE_MATCH(_)

#define END_PROTOCOL_ENTRY
#define END_PROTOCOL_ENTRY_FLUSH
#define END_PROTOCOL_ENTRY_HEAVY

#include "sgen-protocol-def.h"
};

/* We pack all protocol structs by default unless specified otherwise */
#ifndef PROTOCOL_STRUCT_ATTR

#define PROTOCOL_PACK_STRUCTS

#if defined(__GNUC__)
#define PROTOCOL_STRUCT_ATTR __attribute__ ((__packed__))
#else
#define PROTOCOL_STRUCT_ATTR
#endif

#endif

#define BEGIN_PROTOCOL_ENTRY0(method)
#define BEGIN_PROTOCOL_ENTRY1(method,t1,f1) \
	typedef struct PROTOCOL_STRUCT_ATTR { \
		t1 f1; \
	} PROTOCOL_STRUCT(method);
#define BEGIN_PROTOCOL_ENTRY2(method,t1,f1,t2,f2) \
	typedef struct PROTOCOL_STRUCT_ATTR { \
		t1 f1; \
		t2 f2; \
	} PROTOCOL_STRUCT(method);
#define BEGIN_PROTOCOL_ENTRY3(method,t1,f1,t2,f2,t3,f3) \
	typedef struct PROTOCOL_STRUCT_ATTR { \
		t1 f1; \
		t2 f2; \
		t3 f3; \
	} PROTOCOL_STRUCT(method);
#define BEGIN_PROTOCOL_ENTRY4(method,t1,f1,t2,f2,t3,f3,t4,f4) \
	typedef struct PROTOCOL_STRUCT_ATTR { \
		t1 f1; \
		t2 f2; \
		t3 f3; \
		t4 f4; \
	} PROTOCOL_STRUCT(method);
#define BEGIN_PROTOCOL_ENTRY5(method,t1,f1,t2,f2,t3,f3,t4,f4,t5,f5) \
	typedef struct PROTOCOL_STRUCT_ATTR { \
		t1 f1; \
		t2 f2; \
		t3 f3; \
		t4 f4; \
		t5 f5; \
	} PROTOCOL_STRUCT(method);
#define BEGIN_PROTOCOL_ENTRY6(method,t1,f1,t2,f2,t3,f3,t4,f4,t5,f5,t6,f6) \
	typedef struct PROTOCOL_STRUCT_ATTR { \
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

#define DEFAULT_PRINT()
#define CUSTOM_PRINT(_)

#define IS_ALWAYS_MATCH(_)
#define MATCH_INDEX(_)
#define IS_VTABLE_MATCH(_)

#define END_PROTOCOL_ENTRY
#define END_PROTOCOL_ENTRY_FLUSH
#define END_PROTOCOL_ENTRY_HEAVY

#if defined(_MSC_VER) && defined(PROTOCOL_PACK_STRUCTS)
#pragma pack(push)
#pragma pack(1)
#endif
#include "sgen-protocol-def.h"
#if defined(_MSC_VER) && defined(PROTOCOL_PACK_STRUCTS)
#pragma pack(pop)
#undef PROTOCOL_PACK_STRUCTS
#endif

/* missing: finalizers, roots, non-store wbarriers */

void sgen_binary_protocol_init (const char *filename, long long limit);
gboolean sgen_binary_protocol_is_enabled (void);

gboolean sgen_binary_protocol_flush_buffers (gboolean force);

#define BEGIN_PROTOCOL_ENTRY0(method) \
	void sgen_ ## method (void);
#define BEGIN_PROTOCOL_ENTRY1(method,t1,f1) \
	void sgen_ ## method (t1 f1);
#define BEGIN_PROTOCOL_ENTRY2(method,t1,f1,t2,f2) \
	void sgen_ ## method (t1 f1, t2 f2);
#define BEGIN_PROTOCOL_ENTRY3(method,t1,f1,t2,f2,t3,f3) \
	void sgen_ ## method (t1 f1, t2 f2, t3 f3);
#define BEGIN_PROTOCOL_ENTRY4(method,t1,f1,t2,f2,t3,f3,t4,f4) \
	void sgen_ ## method (t1 f1, t2 f2, t3 f3, t4 f4);
#define BEGIN_PROTOCOL_ENTRY5(method,t1,f1,t2,f2,t3,f3,t4,f4,t5,f5) \
	void sgen_ ## method (t1 f1, t2 f2, t3 f3, t4 f4, t5 f5);
#define BEGIN_PROTOCOL_ENTRY6(method,t1,f1,t2,f2,t3,f3,t4,f4,t5,f5,t6,f6) \
	void sgen_ ##  method (t1 f1, t2 f2, t3 f3, t4 f4, t5 f5, t6 f6);

#ifdef SGEN_HEAVY_BINARY_PROTOCOL
#define sgen_binary_protocol_is_heavy_enabled()	sgen_binary_protocol_is_enabled ()

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
#define sgen_binary_protocol_is_heavy_enabled()	FALSE

#define BEGIN_PROTOCOL_ENTRY_HEAVY0(method) \
	static inline void sgen_ ## method (void) {}
#define BEGIN_PROTOCOL_ENTRY_HEAVY1(method,t1,f1) \
	static inline void sgen_ ## method (t1 f1) {}
#define BEGIN_PROTOCOL_ENTRY_HEAVY2(method,t1,f1,t2,f2) \
	static inline void sgen_ ## method (t1 f1, t2 f2) {}
#define BEGIN_PROTOCOL_ENTRY_HEAVY3(method,t1,f1,t2,f2,t3,f3) \
	static inline void sgen_ ## method (t1 f1, t2 f2, t3 f3) {}
#define BEGIN_PROTOCOL_ENTRY_HEAVY4(method,t1,f1,t2,f2,t3,f3,t4,f4) \
	static inline void sgen_ ## method (t1 f1, t2 f2, t3 f3, t4 f4) {}
#define BEGIN_PROTOCOL_ENTRY_HEAVY5(method,t1,f1,t2,f2,t3,f3,t4,f4,t5,f5) \
	static inline void sgen_ ## method (t1 f1, t2 f2, t3 f3, t4 f4, t5 f5) {}
#define BEGIN_PROTOCOL_ENTRY_HEAVY6(method,t1,f1,t2,f2,t3,f3,t4,f4,t5,f5,t6,f6) \
	static inline void sgen_ ## method (t1 f1, t2 f2, t3 f3, t4 f4, t5 f5, t6 f6) {}
#endif

#define DEFAULT_PRINT()
#define CUSTOM_PRINT(_)

#define IS_ALWAYS_MATCH(_)
#define MATCH_INDEX(_)
#define IS_VTABLE_MATCH(_)

#define END_PROTOCOL_ENTRY
#define END_PROTOCOL_ENTRY_FLUSH
#define END_PROTOCOL_ENTRY_HEAVY

#include "sgen-protocol-def.h"

#undef TYPE_INT
#undef TYPE_LONGLONG
#undef TYPE_SIZE
#undef TYPE_POINTER
#undef TYPE_BOOL

#else

#ifndef TYPE_INT
#define TYPE_INT int
#endif
#ifndef TYPE_LONGLONG
#define TYPE_LONGLONG long long
#endif
#ifndef TYPE_SIZE
#define TYPE_SIZE size_t
#endif
#ifndef TYPE_POINTER
#define TYPE_POINTER gpointer
#endif
#ifndef TYPE_BOOL
#define TYPE_BOOL gboolean
#endif

#define BEGIN_PROTOCOL_ENTRY0(method) \
	static inline void sgen_ ## method (void) {}
#define BEGIN_PROTOCOL_ENTRY1(method,t1,f1) \
	static inline void sgen_ ## method (t1 f1) {}
#define BEGIN_PROTOCOL_ENTRY2(method,t1,f1,t2,f2) \
	static inline void sgen_ ## method (t1 f1, t2 f2) {}
#define BEGIN_PROTOCOL_ENTRY3(method,t1,f1,t2,f2,t3,f3) \
	static inline void sgen_ ## method (t1 f1, t2 f2, t3 f3) {}
#define BEGIN_PROTOCOL_ENTRY4(method,t1,f1,t2,f2,t3,f3,t4,f4) \
	static inline void sgen_ ## method (t1 f1, t2 f2, t3 f3, t4 f4) {}
#define BEGIN_PROTOCOL_ENTRY5(method,t1,f1,t2,f2,t3,f3,t4,f4,t5,f5) \
	static inline void sgen_ ## method (t1 f1, t2 f2, t3 f3, t4 f4, t5 f5) {}
#define BEGIN_PROTOCOL_ENTRY6(method,t1,f1,t2,f2,t3,f3,t4,f4,t5,f5,t6,f6) \
	static inline void sgen_ ## method (t1 f1, t2 f2, t3 f3, t4 f4, t5 f5, t6 f6) {}
#define BEGIN_PROTOCOL_ENTRY_HEAVY0(method) \
	static inline void sgen_ ## method (void) {}
#define BEGIN_PROTOCOL_ENTRY_HEAVY1(method,t1,f1) \
	static inline void sgen_ ## method (t1 f1) {}
#define BEGIN_PROTOCOL_ENTRY_HEAVY2(method,t1,f1,t2,f2) \
	static inline void sgen_ ## method (t1 f1, t2 f2) {}
#define BEGIN_PROTOCOL_ENTRY_HEAVY3(method,t1,f1,t2,f2,t3,f3) \
	static inline void sgen_ ## method (t1 f1, t2 f2, t3 f3) {}
#define BEGIN_PROTOCOL_ENTRY_HEAVY4(method,t1,f1,t2,f2,t3,f3,t4,f4) \
	static inline void sgen_ ## method (t1 f1, t2 f2, t3 f3, t4 f4) {}
#define BEGIN_PROTOCOL_ENTRY_HEAVY5(method,t1,f1,t2,f2,t3,f3,t4,f4,t5,f5) \
	static inline void sgen_ ## method (t1 f1, t2 f2, t3 f3, t4 f4, t5 f5) {}
#define BEGIN_PROTOCOL_ENTRY_HEAVY6(method,t1,f1,t2,f2,t3,f3,t4,f4,t5,f5,t6,f6) \
	static inline void sgen_ ## method (t1 f1, t2 f2, t3 f3, t4 f4, t5 f5, t6 f6) {}
#define DEFAULT_PRINT()
#define CUSTOM_PRINT(_)

#define IS_ALWAYS_MATCH(_)
#define MATCH_INDEX(_)
#define IS_VTABLE_MATCH(_)

#define END_PROTOCOL_ENTRY
#define END_PROTOCOL_ENTRY_FLUSH
#define END_PROTOCOL_ENTRY_HEAVY

#include "sgen-protocol-def.h"

static inline void sgen_binary_protocol_init (const char *filename, long long limit) {}
static inline gboolean sgen_binary_protocol_is_enabled (void) { return FALSE; }
static inline gboolean sgen_binary_protocol_flush_buffers (gboolean force) { return FALSE; }
static inline gboolean sgen_binary_protocol_is_heavy_enabled () { return FALSE; }

#endif

#endif
