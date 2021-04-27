/**
 * \file
 * GC interface for the mono JIT
 *
 * Author:
 *   Zoltan Varga (vargaz@gmail.com)
 *
 * Copyright 2009 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "config.h"
#include "mini-gc.h"
#include "mini-runtime.h"
#include <mono/metadata/gc-internals.h>

static gboolean
get_provenance (StackFrameInfo *frame, MonoContext *ctx, gpointer data)
{
	MonoJitInfo *ji = frame->ji;
	MonoMethod *method;
	if (!ji)
		return FALSE;
	method = jinfo_get_method (ji);
	if (method->wrapper_type != MONO_WRAPPER_NONE)
		return FALSE;
	*(gpointer *)data = method;
	return TRUE;
}

static gpointer
get_provenance_func (void)
{
	gpointer provenance = NULL;
	mono_walk_stack (get_provenance, MONO_UNWIND_DEFAULT, (gpointer)&provenance);
	return provenance;
}

#if 0
//#if defined(MONO_ARCH_GC_MAPS_SUPPORTED)

#include <mono/metadata/sgen-conf.h>
#include <mono/metadata/gc-internals.h>
#include <mono/utils/mono-counters.h>
#include <mono/utils/unlocked.h>

//#define SIZEOF_SLOT ((int)sizeof (host_mgreg_t))
//#define SIZEOF_SLOT ((int)sizeof (target_mgreg_t))

#define GC_BITS_PER_WORD (sizeof (mword) * 8)

/* Contains state needed by the GC Map construction code */
typedef struct {
	/*
	 * This contains information about stack slots initialized in the prolog, encoded using
	 * (slot_index << 16) | slot_type. The slot_index is relative to the CFA, i.e. 0
	 * means cfa+0, 1 means cfa-4/8, etc.
	 */
	GSList *stack_slots_from_cfa;
	/* Same for stack slots relative to the frame pointer */
	GSList *stack_slots_from_fp;

	/* Number of slots in the map */
	int nslots;
	/* The number of registers in the map */
	int nregs;
	/* Min and Max offsets of the stack frame relative to fp */
	int min_offset, max_offset;
	/* Same for the locals area */
	int locals_min_offset, locals_max_offset;

	/* The call sites where this frame can be stopped during GC */
	GCCallSite **callsites;
	/* The number of call sites */
	int ncallsites;

	/*
	 * The width of the stack bitmaps in bytes. This is not equal to the bitmap width at
     * runtime, since it includes columns which are 0.
	 */
	int stack_bitmap_width;
	/* 
	 * A bitmap whose width equals nslots, and whose height equals ncallsites.
	 * The bitmap contains a 1 if the corresponding stack slot has type SLOT_REF at the
	 * given callsite.
	 */
	guint8 *stack_ref_bitmap;
	/* Same for SLOT_PIN */
	guint8 *stack_pin_bitmap;

	/*
	 * Similar bitmaps for registers. These have width MONO_MAX_IREGS in bits.
	 */
	int reg_bitmap_width;
	guint8 *reg_ref_bitmap;
	guint8 *reg_pin_bitmap;
} MonoCompileGC;

#undef DEBUG

#if 0
/* We don't support debug levels, its all-or-nothing */
#define DEBUG(s) do { s; fflush (logfile); } while (0)
#define DEBUG_ENABLED 1
#else
#define DEBUG(s)
#endif

#ifdef DEBUG_ENABLED
//#if 1
#define DEBUG_PRECISE(s) do { s; } while (0)
#define DEBUG_PRECISE_ENABLED
#else
#define DEBUG_PRECISE(s)
#endif

/*
 * Contains information collected during the conservative stack marking pass,
 * used during the precise pass. This helps to avoid doing a stack walk twice, which
 * is expensive.
 */
typedef struct {
	guint8 *bitmap;
	int nslots;
    int frame_start_offset;
	int nreg_locations;
	/* Relative to stack_start */
	int reg_locations [MONO_MAX_IREGS];
#ifdef DEBUG_PRECISE_ENABLED
	MonoJitInfo *ji;
	gpointer fp;
	int regs [MONO_MAX_IREGS];
#endif
} FrameInfo;

/* Max number of frames stored in the TLS data */
#define MAX_FRAMES 50

/*
 * Per-thread data kept by this module. This is stored in the GC and passed to us as
 * parameters, instead of being stored in a TLS variable, since during a collection,
 * only the collection thread is active.
 */
typedef struct {
	MonoThreadUnwindState unwind_state;
	MonoThreadInfo *info;
	/* For debugging */
	host_mgreg_t tid;
	gpointer ref_to_track;
	/* Number of frames collected during the !precise pass */
	int nframes;
	FrameInfo frames [MAX_FRAMES];
} TlsData;

/* These are constant so don't store them in the GC Maps */
/* Number of registers stored in gc maps */
#define NREGS MONO_MAX_IREGS

/* 
 * The GC Map itself.
 * Contains information needed to mark a stack frame.
 * This is a transient structure, created from a compressed representation on-demand.
 */
typedef struct {
	/*
	 * The offsets of the GC tracked area inside the stack frame relative to the frame pointer.
	 * This includes memory which is NOREF thus doesn't need GC maps.
	 */
	int start_offset;
	int end_offset;
	/*
	 * The offset relative to frame_offset where the the memory described by the GC maps
	 * begins.
	 */
	int map_offset;
	/* The number of stack slots in the map */
	int nslots;
	/* The frame pointer register */
	guint8 frame_reg;
	/* The size of each callsite table entry */
	guint8 callsite_entry_size;
	guint has_pin_slots : 1;
	guint has_ref_slots : 1;
	guint has_ref_regs : 1;
	guint has_pin_regs : 1;

	/* The offsets below are into an external bitmaps array */

	/* 
	 * A bitmap whose width is equal to bitmap_width, and whose height is equal to ncallsites.
	 * The bitmap contains a 1 if the corresponding stack slot has type SLOT_REF at the
	 * given callsite.
	 */
	guint32 stack_ref_bitmap_offset;
	/*
	 * Same for SLOT_PIN. It is possible that the same bit is set in both bitmaps at
     * different callsites, if the slot starts out as PIN, and later changes to REF.
	 */
	guint32 stack_pin_bitmap_offset;

	/*
	 * Corresponding bitmaps for registers
	 * These have width equal to the number of bits set in reg_ref_mask/reg_pin_mask.
	 * FIXME: Merge these with the normal bitmaps, i.e. reserve the first x slots for them ?
	 */
	guint32 reg_pin_bitmap_offset;
	guint32 reg_ref_bitmap_offset;

	guint32 used_int_regs, reg_ref_mask, reg_pin_mask;

	/* The number of bits set in the two masks above */
	guint8 nref_regs, npin_regs;

	/*
	 * A bit array marking slots which contain refs.
	 * This is used only for debugging.
	 */
	//guint8 *ref_slots;

	/* Callsite offsets */
	/* These can take up a lot of space, so encode them compactly */
	union {
		guint8 *offsets8;
		guint16 *offsets16;
		guint32 *offsets32;
	} callsites;
	int ncallsites;
} GCMap;

/*
 * A compressed version of GCMap. This is what gets stored in MonoJitInfo.
 */
typedef struct {
	//guint8 *ref_slots;
	//guint8 encoded_size;

	/*
	 * The arrays below are embedded after the struct.
	 * Their address needs to be computed.
	 */

	/* The fixed fields of the GCMap encoded using LEB128 */
	guint8 encoded [MONO_ZERO_LEN_ARRAY];

	/* An array of ncallsites entries, each entry is callsite_entry_size bytes long */
	guint8 callsites [MONO_ZERO_LEN_ARRAY];

	/* The GC bitmaps */
	guint8 bitmaps [MONO_ZERO_LEN_ARRAY];
} GCEncodedMap;

static int precise_frame_count [2], precise_frame_limit = -1;
static gboolean precise_frame_limit_inited;

/* Stats */
typedef struct {
	gint32 scanned_stacks;
	gint32 scanned;
	gint32 scanned_precisely;
	gint32 scanned_conservatively;
	gint32 scanned_registers;
	gint32 scanned_native;
	gint32 scanned_other;
	
	gint32 all_slots;
	gint32 noref_slots;
	gint32 ref_slots;
	gint32 pin_slots;

	gint32 gc_maps_size;
	gint32 gc_callsites_size;
	gint32 gc_callsites8_size;
	gint32 gc_callsites16_size;
	gint32 gc_callsites32_size;
	gint32 gc_bitmaps_size;
	gint32 gc_map_struct_size;
	gint32 tlsdata_size;
} JITGCStats;

static JITGCStats stats;

static FILE *logfile;

static gboolean enable_gc_maps_for_aot;

void
mini_gc_enable_gc_maps_for_aot (void)
{
	enable_gc_maps_for_aot = TRUE;
}

// FIXME: Move these to a shared place

static void
encode_uleb128 (guint32 value, guint8 *buf, guint8 **endbuf)
{
	guint8 *p = buf;

	do {
		guint8 b = value & 0x7f;
		value >>= 7;
		if (value != 0) /* more bytes to come */
			b |= 0x80;
		*p ++ = b;
	} while (value);

	*endbuf = p;
}

static G_GNUC_UNUSED void
encode_sleb128 (gint32 value, guint8 *buf, guint8 **endbuf)
{
	gboolean more = 1;
	gboolean negative = (value < 0);
	guint32 size = 32;
	guint8 byte;
	guint8 *p = buf;

	while (more) {
		byte = value & 0x7f;
		value >>= 7;
		/* the following is unnecessary if the
		 * implementation of >>= uses an arithmetic rather
		 * than logical shift for a signed left operand
		 */
		if (negative)
			/* sign extend */
			value |= - (1 <<(size - 7));
		/* sign bit of byte is second high order bit (0x40) */
		if ((value == 0 && !(byte & 0x40)) ||
			(value == -1 && (byte & 0x40)))
			more = 0;
		else
			byte |= 0x80;
		*p ++= byte;
	}

	*endbuf = p;
}

static guint32
decode_uleb128 (guint8 *buf, guint8 **endbuf)
{
	guint8 *p = buf;
	guint32 res = 0;
	int shift = 0;

	while (TRUE) {
		guint8 b = *p;
		p ++;

		res = res | (((int)(b & 0x7f)) << shift);
		if (!(b & 0x80))
			break;
		shift += 7;
	}

	*endbuf = p;

	return res;
}

static gint32
decode_sleb128 (guint8 *buf, guint8 **endbuf)
{
	guint8 *p = buf;
	gint32 res = 0;
	int shift = 0;

	while (TRUE) {
		guint8 b = *p;
		p ++;

		res = res | (((int)(b & 0x7f)) << shift);
		shift += 7;
		if (!(b & 0x80)) {
			if (shift < 32 && (b & 0x40))
				res |= - (1 << shift);
			break;
		}
	}

	*endbuf = p;

	return res;
}

static int
encode_frame_reg (int frame_reg)
{
#ifdef TARGET_AMD64
	if (frame_reg == AMD64_RSP)
		return 0;
	else if (frame_reg == AMD64_RBP)
		return 1;
#elif defined(TARGET_X86)
	if (frame_reg == X86_EBP)
		return 0;
	else if (frame_reg == X86_ESP)
		return 1;
#elif defined(TARGET_ARM)
	if (frame_reg == ARMREG_SP)
		return 0;
	else if (frame_reg == ARMREG_FP)
		return 1;
#elif defined(TARGET_S390X)
	if (frame_reg == S390_SP)
		return 0;
	else if (frame_reg == S390_FP)
		return 1;
#elif defined (TARGET_RISCV)
	if (frame_reg == RISCV_SP)
		return 0;
	else if (frame_reg == RISCV_FP)
		return 1;
#else
	NOT_IMPLEMENTED;
#endif
	g_assert_not_reached ();
	return -1;
}

static int
decode_frame_reg (int encoded)
{
#ifdef TARGET_AMD64
	if (encoded == 0)
		return AMD64_RSP;
	else if (encoded == 1)
		return AMD64_RBP;
#elif defined(TARGET_X86)
	if (encoded == 0)
		return X86_EBP;
	else if (encoded == 1)
		return X86_ESP;
#elif defined(TARGET_ARM)
	if (encoded == 0)
		return ARMREG_SP;
	else if (encoded == 1)
		return ARMREG_FP;
#elif defined(TARGET_S390X)
	if (encoded == 0)
		return S390_SP;
	else if (encoded == 1)
		return S390_FP;
#elif defined (TARGET_RISCV)
	if (encoded == 0)
		return RISCV_SP;
	else if (encoded == 1)
		return RISCV_FP;
#else
	NOT_IMPLEMENTED;
#endif
	g_assert_not_reached ();
	return -1;
}

#ifdef TARGET_AMD64
#ifdef HOST_WIN32
static int callee_saved_regs [] = { AMD64_RBP, AMD64_RBX, AMD64_R12, AMD64_R13, AMD64_R14, AMD64_R15, AMD64_RDI, AMD64_RSI };
#else
static int callee_saved_regs [] = { AMD64_RBP, AMD64_RBX, AMD64_R12, AMD64_R13, AMD64_R14, AMD64_R15 };
#endif
#elif defined(TARGET_X86)
static int callee_saved_regs [] = { X86_EBX, X86_ESI, X86_EDI };
#elif defined(TARGET_ARM)
static int callee_saved_regs [] = { ARMREG_V1, ARMREG_V2, ARMREG_V3, ARMREG_V4, ARMREG_V5, ARMREG_V7, ARMREG_FP };
#elif defined(TARGET_ARM64)
// FIXME:
static int callee_saved_regs [] = { };
#elif defined(TARGET_S390X)
static int callee_saved_regs [] = { s390_r6, s390_r7, s390_r8, s390_r9, s390_r10, s390_r11, s390_r12, s390_r13, s390_r14 };
#elif defined(TARGET_POWERPC64) && _CALL_ELF == 2
static int callee_saved_regs [] = {
  ppc_r13, ppc_r14, ppc_r15, ppc_r16,
  ppc_r17, ppc_r18, ppc_r19, ppc_r20,
  ppc_r21, ppc_r22, ppc_r23, ppc_r24,
  ppc_r25, ppc_r26, ppc_r27, ppc_r28,
  ppc_r29, ppc_r30, ppc_r31 };
#elif defined(TARGET_POWERPC)
static int callee_saved_regs [] = { ppc_r6, ppc_r7, ppc_r8, ppc_r9, ppc_r10, ppc_r11, ppc_r12, ppc_r13, ppc_r14 };
#elif defined (TARGET_RISCV)
static int callee_saved_regs [] = {
	RISCV_S0, RISCV_S1, RISCV_S2, RISCV_S3, RISCV_S4, RISCV_S5,
	RISCV_S6, RISCV_S7, RISCV_S8, RISCV_S9, RISCV_S10, RISCV_S11,
};
#endif

static guint32
encode_regmask (guint32 regmask)
{
	int i;
	guint32 res;

	res = 0;
	for (i = 0; i < sizeof (callee_saved_regs) / sizeof (int); ++i) {
		if (regmask & (1 << callee_saved_regs [i])) {
			res |= (1 << i);
			regmask -= (1 << callee_saved_regs [i]);
		}
	}
	g_assert (regmask == 0);
	return res;
}

static guint32
decode_regmask (guint32 regmask)
{
	int i;
	guint32 res;

	res = 0;
	for (i = 0; i < sizeof (callee_saved_regs) / sizeof (int); ++i)
		if (regmask & (1 << i))
			res |= (1 << callee_saved_regs [i]);
	return res;
}

/*
 * encode_gc_map:
 *
 *   Encode the fixed fields of MAP into a buffer pointed to by BUF.
 */
static void
encode_gc_map (GCMap *map, guint8 *buf, guint8 **endbuf)
{
	guint32 flags, freg;

	encode_sleb128 (map->start_offset / SIZEOF_SLOT, buf, &buf);
	encode_sleb128 (map->end_offset / SIZEOF_SLOT, buf, &buf);
	encode_sleb128 (map->map_offset / SIZEOF_SLOT, buf, &buf);
	encode_uleb128 (map->nslots, buf, &buf);
	g_assert (map->callsite_entry_size <= 4);
	freg = encode_frame_reg (map->frame_reg);
	g_assert (freg < 2);
	flags = (map->has_ref_slots ? 1 : 0) | (map->has_pin_slots ? 2 : 0) | (map->has_ref_regs ? 4 : 0) | (map->has_pin_regs ? 8 : 0) | ((map->callsite_entry_size - 1) << 4) | (freg << 6);
	encode_uleb128 (flags, buf, &buf);
	encode_uleb128 (encode_regmask (map->used_int_regs), buf, &buf);
	if (map->has_ref_regs)
		encode_uleb128 (encode_regmask (map->reg_ref_mask), buf, &buf);
	if (map->has_pin_regs)
		encode_uleb128 (encode_regmask (map->reg_pin_mask), buf, &buf);
	encode_uleb128 (map->ncallsites, buf, &buf);

	*endbuf = buf;
}	

/*
 * decode_gc_map:
 *
 *   Decode the encoded GC map representation in BUF and store the result into MAP.
 */
static void
decode_gc_map (guint8 *buf, GCMap *map, guint8 **endbuf)
{
	guint32 flags;
	int stack_bitmap_size, reg_ref_bitmap_size, reg_pin_bitmap_size, offset, freg;
	int i, n;

	map->start_offset = decode_sleb128 (buf, &buf) * SIZEOF_SLOT;
	map->end_offset = decode_sleb128 (buf, &buf) * SIZEOF_SLOT;
	map->map_offset = decode_sleb128 (buf, &buf) * SIZEOF_SLOT;
	map->nslots = decode_uleb128 (buf, &buf);
	flags = decode_uleb128 (buf, &buf);
	map->has_ref_slots = (flags & 1) ? 1 : 0;
	map->has_pin_slots = (flags & 2) ? 1 : 0;
	map->has_ref_regs = (flags & 4) ? 1 : 0;
	map->has_pin_regs = (flags & 8) ? 1 : 0;
	map->callsite_entry_size = ((flags >> 4) & 0x3) + 1;
	freg = flags >> 6;
	map->frame_reg = decode_frame_reg (freg);
	map->used_int_regs = decode_regmask (decode_uleb128 (buf, &buf));
	if (map->has_ref_regs) {
		map->reg_ref_mask = decode_regmask (decode_uleb128 (buf, &buf));
		n = 0;
		for (i = 0; i < NREGS; ++i)
			if (map->reg_ref_mask & (1 << i))
				n ++;
		map->nref_regs = n;
	}
	if (map->has_pin_regs) {
		map->reg_pin_mask = decode_regmask (decode_uleb128 (buf, &buf));
		n = 0;
		for (i = 0; i < NREGS; ++i)
			if (map->reg_pin_mask & (1 << i))
				n ++;
		map->npin_regs = n;
	}
	map->ncallsites = decode_uleb128 (buf, &buf);

	stack_bitmap_size = (ALIGN_TO (map->nslots, 8) / 8) * map->ncallsites;
	reg_ref_bitmap_size = (ALIGN_TO (map->nref_regs, 8) / 8) * map->ncallsites;
	reg_pin_bitmap_size = (ALIGN_TO (map->npin_regs, 8) / 8) * map->ncallsites;
	offset = 0;
	map->stack_ref_bitmap_offset = offset;
	if (map->has_ref_slots)
		offset += stack_bitmap_size;
	map->stack_pin_bitmap_offset = offset;
	if (map->has_pin_slots)
		offset += stack_bitmap_size;
	map->reg_ref_bitmap_offset = offset;
	if (map->has_ref_regs)
		offset += reg_ref_bitmap_size;
	map->reg_pin_bitmap_offset = offset;
	if (map->has_pin_regs)
		offset += reg_pin_bitmap_size;

	*endbuf = buf;
}

static gpointer
thread_attach_func (void)
{
	TlsData *tls;

	tls = g_new0 (TlsData, 1);
	tls->tid = mono_native_thread_id_get ();
	tls->info = mono_thread_info_current ();
	UnlockedAdd (&stats.tlsdata_size, sizeof (TlsData));

	return tls;
}

static void
thread_detach_func (gpointer user_data)
{
	TlsData *tls = user_data;

	g_free (tls);
}

static void
thread_suspend_func (gpointer user_data, void *sigctx, MonoContext *ctx)
{
	TlsData *tls = user_data;

	if (!tls) {
		/* Happens during startup */
		return;
	}

	if (tls->tid != mono_native_thread_id_get ()) {
		/* Happens on osx because threads are not suspended using signals */
#ifndef TARGET_WIN32
		gboolean res;
#endif

		g_assert (tls->info);
#ifdef TARGET_WIN32
		return;
#else
		res = mono_thread_state_init_from_handle (&tls->unwind_state, tls->info, NULL);
#endif
	} else {
		tls->unwind_state.unwind_data [MONO_UNWIND_DATA_LMF] = mono_get_lmf ();
		if (sigctx) {
			mono_sigctx_to_monoctx (sigctx, &tls->unwind_state.ctx);
			tls->unwind_state.valid = TRUE;
		} else if (ctx) {
			memcpy (&tls->unwind_state.ctx, ctx, sizeof (MonoContext));
			tls->unwind_state.valid = TRUE;
		} else {
			tls->unwind_state.valid = FALSE;
		}
		tls->unwind_state.unwind_data [MONO_UNWIND_DATA_JIT_TLS] = mono_tls_get_jit_tls ();
		tls->unwind_state.unwind_data [MONO_UNWIND_DATA_DOMAIN] = mono_domain_get ();
	}

	if (!tls->unwind_state.unwind_data [MONO_UNWIND_DATA_DOMAIN]) {
		/* Happens during startup */
		tls->unwind_state.valid = FALSE;
		return;
	}
}

#define DEAD_REF ((gpointer)(gssize)0x2a2a2a2a2a2a2a2aULL)

static void
set_bit (guint8 *bitmap, int width, int y, int x)
{
	bitmap [(width * y) + (x / 8)] |= (1 << (x % 8));
}

static void
clear_bit (guint8 *bitmap, int width, int y, int x)
{
	bitmap [(width * y) + (x / 8)] &= ~(1 << (x % 8));
}

static int
get_bit (guint8 *bitmap, int width, int y, int x)
{
	return bitmap [(width * y) + (x / 8)] & (1 << (x % 8));
}

static const char*
slot_type_to_string (GCSlotType type)
{
	switch (type) {
	case SLOT_REF:
		return "ref";
	case SLOT_NOREF:
		return "noref";
	case SLOT_PIN:
		return "pin";
	default:
		g_assert_not_reached ();
		return NULL;
	}
}

static host_mgreg_t
get_frame_pointer (MonoContext *ctx, int frame_reg)
{
#if defined(TARGET_AMD64)
		if (frame_reg == AMD64_RSP)
			return ctx->rsp;
		else if (frame_reg == AMD64_RBP)
			return ctx->rbp;
#elif defined(TARGET_X86)
		if (frame_reg == X86_ESP)
			return ctx->esp;
		else if (frame_reg == X86_EBP)
			return ctx->ebp;
#elif defined(TARGET_ARM)
		if (frame_reg == ARMREG_SP)
			return (host_mgreg_t)MONO_CONTEXT_GET_SP (ctx);
		else if (frame_reg == ARMREG_FP)
			return (host_mgreg_t)MONO_CONTEXT_GET_BP (ctx);
#elif defined(TARGET_S390X)
		if (frame_reg == S390_SP)
			return (host_mgreg_t)MONO_CONTEXT_GET_SP (ctx);
		else if (frame_reg == S390_FP)
			return (host_mgreg_t)MONO_CONTEXT_GET_BP (ctx);
#elif defined (TARGET_RISCV)
		if (frame_reg == RISCV_SP)
			return MONO_CONTEXT_GET_SP (ctx);
		else if (frame_reg == RISCV_FP)
			return MONO_CONTEXT_GET_BP (ctx);
#endif
		g_assert_not_reached ();
		return 0;
}

/*
 * conservatively_pass:
 *
 *   Mark a thread stack conservatively and collect information needed by the precise pass.
 */
static void
conservative_pass (TlsData *tls, guint8 *stack_start, guint8 *stack_end)
{
	MonoJitInfo *ji;
	MonoMethod *method;
	MonoContext ctx, new_ctx;
	MonoLMF *lmf;
	guint8 *stack_limit;
	gboolean last = TRUE;
	GCMap *map;
	GCMap map_tmp;
	GCEncodedMap *emap;
	guint8* fp, *p, *real_frame_start, *frame_start, *frame_end;
	int i, pc_offset, cindex, bitmap_width;
	int scanned = 0, scanned_precisely, scanned_conservatively, scanned_registers;
	gboolean res;
	StackFrameInfo frame;
	host_mgreg_t *reg_locations [MONO_MAX_IREGS];
	host_mgreg_t *new_reg_locations [MONO_MAX_IREGS];
	guint8 *bitmaps;
	FrameInfo *fi;
	guint32 precise_regmask;

	if (tls) {
		tls->nframes = 0;
		tls->ref_to_track = NULL;
	}

	/* tls == NULL can happen during startup */
	if (mono_thread_internal_current () == NULL || !tls) {
		mono_gc_conservatively_scan_area (stack_start, stack_end);
		UnlockedAdd (&stats.scanned_stacks, stack_end - stack_start);
		return;
	}

	lmf = tls->unwind_state.unwind_data [MONO_UNWIND_DATA_LMF];
	frame.domain = NULL;

	/* Number of bytes scanned based on GC map data */
	scanned = 0;
	/* Number of bytes scanned precisely based on GC map data */
	scanned_precisely = 0;
	/* Number of bytes scanned conservatively based on GC map data */
	scanned_conservatively = 0;
	/* Number of bytes scanned conservatively in register save areas */
	scanned_registers = 0;

	/* This is one past the last address which we have scanned */
	stack_limit = stack_start;

	if (!tls->unwind_state.valid)
		memset (&new_ctx, 0, sizeof (ctx));
	else
		memcpy (&new_ctx, &tls->unwind_state.ctx, sizeof (MonoContext));

	memset (reg_locations, 0, sizeof (reg_locations));
	memset (new_reg_locations, 0, sizeof (new_reg_locations));

	while (TRUE) {
		if (!tls->unwind_state.valid)
			break;

		memcpy (&ctx, &new_ctx, sizeof (ctx));

		for (i = 0; i < MONO_MAX_IREGS; ++i) {
			if (new_reg_locations [i]) {
				/*
				 * If the current frame saves the register, it means it might modify its
				 * value, thus the old location might not contain the same value, so
				 * we have to mark it conservatively.
				 */
				if (reg_locations [i]) {
					DEBUG (fprintf (logfile, "\tscan saved reg %s location %p.\n", mono_arch_regname (i), reg_locations [i]));
					mono_gc_conservatively_scan_area (reg_locations [i], (char*)reg_locations [i] + SIZEOF_SLOT);
					scanned_registers += SIZEOF_SLOT;
				}

				reg_locations [i] = new_reg_locations [i];

				DEBUG (fprintf (logfile, "\treg %s is now at location %p.\n", mono_arch_regname (i), reg_locations [i]));
			}
		}

		g_assert ((gsize)stack_limit % SIZEOF_SLOT == 0);

		res = mono_find_jit_info_ext (tls->unwind_state.unwind_data [MONO_UNWIND_DATA_JIT_TLS], NULL, &ctx, &new_ctx, NULL, &lmf, new_reg_locations, &frame);
		if (!res)
			break;

		ji = frame.ji;

		// FIXME: For skipped frames, scan the param area of the parent frame conservatively ?
		// FIXME: trampolines

		if (frame.type == FRAME_TYPE_MANAGED_TO_NATIVE) {
			/*
			 * These frames are problematic for several reasons:
			 * - they are unwound through an LMF, and we have no precise register tracking for those.
			 * - the LMF might not contain a precise ip, so we can't compute the call site.
			 * - the LMF only unwinds to the wrapper frame, so we get these methods twice.
			 */
			DEBUG (fprintf (logfile, "Mark(0): <Managed-to-native transition>\n"));
			for (i = 0; i < MONO_MAX_IREGS; ++i) {
				if (reg_locations [i]) {
					DEBUG (fprintf (logfile, "\tscan saved reg %s location %p.\n", mono_arch_regname (i), reg_locations [i]));
					mono_gc_conservatively_scan_area (reg_locations [i], (char*)reg_locations [i] + SIZEOF_SLOT);
					scanned_registers += SIZEOF_SLOT;
				}
				reg_locations [i] = NULL;
				new_reg_locations [i] = NULL;
			}
			ctx = new_ctx;
			continue;
		}

		if (ji)
			method = jinfo_get_method (ji);
		else
			method = NULL;

		/* The last frame can be in any state so mark conservatively */
		if (last) {
			if (ji) {
				DEBUG (char *fname = mono_method_full_name (method, TRUE); fprintf (logfile, "Mark(0): %s+0x%x (%p)\n", fname, pc_offset, (gpointer)MONO_CONTEXT_GET_IP (&ctx)); g_free (fname));
			}
			DEBUG (fprintf (logfile, "\t <Last frame>\n"));
			last = FALSE;
			/*
			 * new_reg_locations is not precise when a method is interrupted during its epilog, so clear it.
			 */
			for (i = 0; i < MONO_MAX_IREGS; ++i) {
				if (reg_locations [i]) {
					DEBUG (fprintf (logfile, "\tscan saved reg %s location %p.\n", mono_arch_regname (i), reg_locations [i]));
					mono_gc_conservatively_scan_area (reg_locations [i], (char*)reg_locations [i] + SIZEOF_SLOT);
					scanned_registers += SIZEOF_SLOT;
				}
				if (new_reg_locations [i]) {
					DEBUG (fprintf (logfile, "\tscan saved reg %s location %p.\n", mono_arch_regname (i), new_reg_locations [i]));
					mono_gc_conservatively_scan_area (new_reg_locations [i], (char*)new_reg_locations [i] + SIZEOF_SLOT);
					scanned_registers += SIZEOF_SLOT;
				}
				reg_locations [i] = NULL;
				new_reg_locations [i] = NULL;
			}
			continue;
		}

		pc_offset = (guint8*)MONO_CONTEXT_GET_IP (&ctx) - (guint8*)ji->code_start;

		/* These frames are very problematic */
		if (method->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE) {
			DEBUG (char *fname = mono_method_full_name (method, TRUE); fprintf (logfile, "Mark(0): %s+0x%x (%p)\n", fname, pc_offset, (gpointer)MONO_CONTEXT_GET_IP (&ctx)); g_free (fname));
			DEBUG (fprintf (logfile, "\tSkip.\n"));
			continue;
		}

		/* All the other frames are at a call site */

		if (tls->nframes == MAX_FRAMES) {
			/* 
			 * Can't save information since the array is full. So scan the rest of the
			 * stack conservatively.
			 */
			DEBUG (fprintf (logfile, "Mark (0): Frame stack full.\n"));
			break;
		}

		/* Scan the frame of this method */

		/*
		 * A frame contains the following:
		 * - saved registers
		 * - saved args
		 * - locals
		 * - spill area
		 * - localloc-ed memory
		 */
		g_assert (pc_offset >= 0);

		emap = ji->gc_info;

		if (!emap) {
			DEBUG (char *fname = mono_method_full_name (jinfo_get_method (ji), TRUE); fprintf (logfile, "Mark(0): %s+0x%x (%p)\n", fname, pc_offset, (gpointer)MONO_CONTEXT_GET_IP (&ctx)); g_free (fname));
			DEBUG (fprintf (logfile, "\tNo GC Map.\n"));
			continue;
		}

		/* The embedded callsite table requires this */
		g_assert (((gsize)emap % 4) == 0);

		/*
		 * Debugging aid to control the number of frames scanned precisely
		 */
		if (!precise_frame_limit_inited) {
			char *mono_precise_count = g_getenv ("MONO_PRECISE_COUNT");
			if (mono_precise_count) {
				precise_frame_limit = atoi (mono_precise_count);
				g_free (mono_precise_count);
			}
			precise_frame_limit_inited = TRUE;
		}
				
		if (precise_frame_limit != -1) {
			if (precise_frame_count [FALSE] == precise_frame_limit)
				printf ("LAST PRECISE FRAME: %s\n", mono_method_full_name (method, TRUE));
			if (precise_frame_count [FALSE] > precise_frame_limit)
				continue;
		}
		precise_frame_count [FALSE] ++;

		/* Decode the encoded GC map */
		map = &map_tmp;
		memset (map, 0, sizeof (GCMap));
		decode_gc_map (&emap->encoded [0], map, &p);
		p = (guint8*)ALIGN_TO (p, map->callsite_entry_size);
		map->callsites.offsets8 = p;
		p += map->callsite_entry_size * map->ncallsites;
		bitmaps = p;

		fp = (guint8*)get_frame_pointer (&ctx, map->frame_reg);

		real_frame_start = fp + map->start_offset;
		frame_start = fp + map->start_offset + map->map_offset;
		frame_end = fp + map->end_offset;

		DEBUG (char *fname = mono_method_full_name (jinfo_get_method (ji), TRUE); fprintf (logfile, "Mark(0): %s+0x%x (%p) limit=%p fp=%p frame=%p-%p (%d)\n", fname, pc_offset, (gpointer)MONO_CONTEXT_GET_IP (&ctx), stack_limit, fp, frame_start, frame_end, (int)(frame_end - frame_start)); g_free (fname));

		/* Find the callsite index */
		if (map->callsite_entry_size == 1) {
			for (i = 0; i < map->ncallsites; ++i)
				/* ip points inside the call instruction */
				if (map->callsites.offsets8 [i] == pc_offset + 1)
					break;
		} else if (map->callsite_entry_size == 2) {
			// FIXME: Use a binary search
			for (i = 0; i < map->ncallsites; ++i)
				/* ip points inside the call instruction */
				if (map->callsites.offsets16 [i] == pc_offset + 1)
					break;
		} else {
			// FIXME: Use a binary search
			for (i = 0; i < map->ncallsites; ++i)
				/* ip points inside the call instruction */
				if (map->callsites.offsets32 [i] == pc_offset + 1)
					break;
		}
		if (i == map->ncallsites) {
			printf ("Unable to find ip offset 0x%x in callsite list of %s.\n", pc_offset + 1, mono_method_full_name (method, TRUE));
			g_assert_not_reached ();
		}
		cindex = i;

		/* 
		 * This is not neccessary true on x86 because frames have a different size at each
		 * call site.
		 */
		//g_assert (real_frame_start >= stack_limit);

		if (real_frame_start > stack_limit) {
			/* This scans the previously skipped frames as well */
			DEBUG (fprintf (logfile, "\tscan area %p-%p (%d).\n", stack_limit, real_frame_start, (int)(real_frame_start - stack_limit)));
			mono_gc_conservatively_scan_area (stack_limit, real_frame_start);
			UnlockedAdd (&stats.scanned_other, real_frame_start - stack_limit);
		}

		/* Mark stack slots */
		if (map->has_pin_slots) {
			int bitmap_width = ALIGN_TO (map->nslots, 8) / 8;
			guint8 *pin_bitmap = &bitmaps [map->stack_pin_bitmap_offset + (bitmap_width * cindex)];
			guint8 *p;
			gboolean pinned;

			p = frame_start;
			for (i = 0; i < map->nslots; ++i) {
				pinned = pin_bitmap [i / 8] & (1 << (i % 8));
				if (pinned) {
					DEBUG (fprintf (logfile, "\tscan slot %s0x%x(fp)=%p.\n", (guint8*)p > (guint8*)fp ? "" : "-", ABS ((int)((gssize)p - (gssize)fp)), p));
					mono_gc_conservatively_scan_area (p, p + SIZEOF_SLOT);
					scanned_conservatively += SIZEOF_SLOT;
				} else {
					scanned_precisely += SIZEOF_SLOT;
				}
				p += SIZEOF_SLOT;
			}
		} else {
			scanned_precisely += (map->nslots * SIZEOF_SLOT);
		}

		/* The area outside of start-end is NOREF */
		scanned_precisely += (map->end_offset - map->start_offset) - (map->nslots * SIZEOF_SLOT);

		/* Mark registers */
		precise_regmask = map->used_int_regs | (1 << map->frame_reg);
		if (map->has_pin_regs) {
			int bitmap_width = ALIGN_TO (map->npin_regs, 8) / 8;
			guint8 *pin_bitmap = &bitmaps [map->reg_pin_bitmap_offset + (bitmap_width * cindex)];
			int bindex = 0;
			for (i = 0; i < NREGS; ++i) {
				if (!(map->used_int_regs & (1 << i)))
					continue;
				
				if (!(map->reg_pin_mask & (1 << i)))
					continue;

				if (pin_bitmap [bindex / 8] & (1 << (bindex % 8))) {
					DEBUG (fprintf (logfile, "\treg %s saved at 0x%p is pinning.\n", mono_arch_regname (i), reg_locations [i]));
					precise_regmask &= ~(1 << i);
				}
				bindex ++;
			}
		}

		scanned += map->end_offset - map->start_offset;

		g_assert (scanned == scanned_precisely + scanned_conservatively);

		stack_limit = frame_end;

		/* Save information for the precise pass */
		fi = &tls->frames [tls->nframes];
		fi->nslots = map->nslots;
		bitmap_width = ALIGN_TO (map->nslots, 8) / 8;
		if (map->has_ref_slots)
			fi->bitmap = &bitmaps [map->stack_ref_bitmap_offset + (bitmap_width * cindex)];
		else
			fi->bitmap = NULL;
		fi->frame_start_offset = frame_start - stack_start;
		fi->nreg_locations = 0;
		DEBUG_PRECISE (fi->ji = ji);
		DEBUG_PRECISE (fi->fp = fp);

		if (map->has_ref_regs) {
			int bitmap_width = ALIGN_TO (map->nref_regs, 8) / 8;
			guint8 *ref_bitmap = &bitmaps [map->reg_ref_bitmap_offset + (bitmap_width * cindex)];
			int bindex = 0;
			for (i = 0; i < NREGS; ++i) {
				if (!(map->reg_ref_mask & (1 << i)))
					continue;

				if (reg_locations [i] && (ref_bitmap [bindex / 8] & (1 << (bindex % 8)))) {
					DEBUG_PRECISE (fi->regs [fi->nreg_locations] = i);
					DEBUG (fprintf (logfile, "\treg %s saved at 0x%p is ref.\n", mono_arch_regname (i), reg_locations [i]));
					fi->reg_locations [fi->nreg_locations] = (guint8*)reg_locations [i] - stack_start;
					fi->nreg_locations ++;
				}
				bindex ++;
			}
		}

		/*
		 * Clear locations of precisely tracked registers.
		 */
		if (precise_regmask) {
			for (i = 0; i < NREGS; ++i) {
				if (precise_regmask & (1 << i)) {
					/*
					 * The method uses this register, and we have precise info for it.
					 * This means the location will be scanned precisely.
					 * Tell the code at the beginning of the loop that this location is
					 * processed.
					 */
					if (reg_locations [i])
						DEBUG (fprintf (logfile, "\treg %s at location %p (==%p) is precise.\n", mono_arch_regname (i), reg_locations [i], (gpointer)*reg_locations [i]));
					reg_locations [i] = NULL;
				}
			}
		}

		tls->nframes ++;
	}

	/* Scan the remaining register save locations */
	for (i = 0; i < MONO_MAX_IREGS; ++i) {
		if (reg_locations [i]) {
			DEBUG (fprintf (logfile, "\tscan saved reg location %p.\n", reg_locations [i]));
			mono_gc_conservatively_scan_area (reg_locations [i], (char*)reg_locations [i] + SIZEOF_SLOT);
			scanned_registers += SIZEOF_SLOT;
		}
		if (new_reg_locations [i]) {
			DEBUG (fprintf (logfile, "\tscan saved reg location %p.\n", new_reg_locations [i]));
			mono_gc_conservatively_scan_area (new_reg_locations [i], (char*)new_reg_locations [i] + SIZEOF_SLOT);
			scanned_registers += SIZEOF_SLOT;
		}
	}

	if (stack_limit < stack_end) {
		DEBUG (fprintf (logfile, "\tscan remaining stack %p-%p (%d).\n", stack_limit, stack_end, (int)(stack_end - stack_limit)));
		mono_gc_conservatively_scan_area (stack_limit, stack_end);
		UnlockedAdd (&stats.scanned_native, stack_end - stack_limit);
	}

	DEBUG (fprintf (logfile, "Marked %d bytes, p=%d,c=%d out of %d.\n", scanned, scanned_precisely, scanned_conservatively, (int)(stack_end - stack_start)));

	UnlockedAdd (&stats.scanned_stacks, stack_end - stack_start);
	UnlockedAdd (&stats.scanned, scanned);
	UnlockedAdd (&stats.scanned_precisely, scanned_precisely);
	UnlockedAdd (&stats.scanned_conservatively, scanned_conservatively);
	UnlockedAdd (&stats.scanned_registers, scanned_registers);

	//mono_gc_conservatively_scan_area (stack_start, stack_end);
}

/*
 * precise_pass:
 *
 *   Mark a thread stack precisely based on information saved during the conservative
 * pass.
 */
static void
precise_pass (TlsData *tls, guint8 *stack_start, guint8 *stack_end, void *gc_data)
{
	int findex, i;
	FrameInfo *fi;
	guint8 *frame_start;

	if (!tls)
		return;

	if (!tls->unwind_state.valid)
		return;

	for (findex = 0; findex < tls->nframes; findex ++) {
		/* Load information saved by the !precise pass */
		fi = &tls->frames [findex];
		frame_start = stack_start + fi->frame_start_offset;

		DEBUG (char *fname = mono_method_full_name (jinfo_get_method (fi->ji), TRUE); fprintf (logfile, "Mark(1): %s\n", fname); g_free (fname));

		/* 
		 * FIXME: Add a function to mark using a bitmap, to avoid doing a 
		 * call for each object.
		 */

		/* Mark stack slots */
		if (fi->bitmap) {
			guint8 *ref_bitmap = fi->bitmap;
			gboolean live;

			for (i = 0; i < fi->nslots; ++i) {
				MonoObject **ptr = (MonoObject**)(frame_start + (i * SIZEOF_SLOT));

				live = ref_bitmap [i / 8] & (1 << (i % 8));

				if (live) {
					MonoObject *obj = *ptr;
					if (obj) {
						DEBUG (fprintf (logfile, "\tref %s0x%x(fp)=%p: %p ->", (guint8*)ptr >= (guint8*)fi->fp ? "" : "-", ABS ((int)((gssize)ptr - (gssize)fi->fp)), ptr, obj));
						*ptr = mono_gc_scan_object (obj, gc_data);
						DEBUG (fprintf (logfile, " %p.\n", *ptr));
					} else {
						DEBUG (fprintf (logfile, "\tref %s0x%x(fp)=%p: %p.\n", (guint8*)ptr >= (guint8*)fi->fp ? "" : "-", ABS ((int)((gssize)ptr - (gssize)fi->fp)), ptr, obj));
					}
				} else {
#if 0
					/*
					 * This is disabled because the pointer takes up a lot of space.
					 * Stack slots might be shared between ref and non-ref variables ?
					 */
					if (map->ref_slots [i / 8] & (1 << (i % 8))) {
						DEBUG (fprintf (logfile, "\tref %s0x%x(fp)=%p: dead (%p)\n", (guint8*)ptr >= (guint8*)fi->fp ? "" : "-", ABS ((int)((gssize)ptr - (gssize)fi->fp)), ptr, *ptr));
						/*
						 * Fail fast if the live range is incorrect, and
						 * the JITted code tries to access this object
						 */
						*ptr = DEAD_REF;
					}
#endif
				}
			}
		}

		/* Mark registers */

		/*
		 * Registers are different from stack slots, they have no address where they
		 * are stored. Instead, some frame below this frame in the stack saves them
		 * in its prolog to the stack. We can mark this location precisely.
		 */
		for (i = 0; i < fi->nreg_locations; ++i) {
			/*
			 * reg_locations [i] contains the address of the stack slot where
			 * a reg was last saved, so mark that slot.
			 */
			MonoObject **ptr = (MonoObject**)((guint8*)stack_start + fi->reg_locations [i]);
			MonoObject *obj = *ptr;

			if (obj) {
				DEBUG (fprintf (logfile, "\treg %s saved at %p: %p ->", mono_arch_regname (fi->regs [i]), ptr, obj));
				*ptr = mono_gc_scan_object (obj, gc_data);
				DEBUG (fprintf (logfile, " %p.\n", *ptr));
			} else {
				DEBUG (fprintf (logfile, "\treg %s saved at %p: %p\n", mono_arch_regname (fi->regs [i]), ptr, obj));
			}
		}	
	}

	/*
	 * Debugging aid to check for missed refs.
	 */
	if (tls->ref_to_track) {
		gpointer *p;

		for (p = (gpointer*)stack_start; p < (gpointer*)stack_end; ++p)
			if (*p == tls->ref_to_track)
				printf ("REF AT %p.\n", p);
	}
}

/*
 * thread_mark_func:
 *
 *   This is called by the GC twice to mark a thread stack. PRECISE is FALSE at the first
 * call, and TRUE at the second. USER_DATA points to a TlsData
 * structure filled up by thread_suspend_func. 
 */
static void
thread_mark_func (gpointer user_data, guint8 *stack_start, guint8 *stack_end, gboolean precise, void *gc_data)
{
	TlsData *tls = user_data;

	DEBUG (fprintf (logfile, "****************************************\n"));
	DEBUG (fprintf (logfile, "*** %s stack marking for thread %p (%p-%p) ***\n", precise ? "Precise" : "Conservative", tls ? GUINT_TO_POINTER (tls->tid) : NULL, stack_start, stack_end));
	DEBUG (fprintf (logfile, "****************************************\n"));

	if (!precise)
		conservative_pass (tls, stack_start, stack_end);
	else
		precise_pass (tls, stack_start, stack_end, gc_data);
}

#ifndef DISABLE_JIT

static void
mini_gc_init_gc_map (MonoCompile *cfg)
{
	if (COMPILE_LLVM (cfg))
		return;

	if (!mono_gc_is_moving ())
		return;

	if (cfg->compile_aot) {
		if (!enable_gc_maps_for_aot)
			return;
	} else if (!mono_gc_precise_stack_mark_enabled ())
		return;

#if 1
	/* Debugging support */
	{
		static int precise_count;

		precise_count ++;
		char *mono_gcmap_count = g_getenv ("MONO_GCMAP_COUNT");
		if (mono_gcmap_count) {
			int count = atoi (mono_gcmap_count);
			g_free (mono_gcmap_count);
			if (precise_count == count)
				printf ("LAST: %s\n", mono_method_full_name (cfg->method, TRUE));
			if (precise_count > count)
				return;
		}
	}
#endif

	cfg->compute_gc_maps = TRUE;

	cfg->gc_info = mono_mempool_alloc0 (cfg->mempool, sizeof (MonoCompileGC));
}

/*
 * mini_gc_set_slot_type_from_fp:
 *
 *   Set the GC slot type of the stack slot identified by SLOT_OFFSET, which should be
 * relative to the frame pointer. By default, all stack slots are type PIN, so there is no
 * need to call this function for those slots.
 */
void
mini_gc_set_slot_type_from_fp (MonoCompile *cfg, int slot_offset, GCSlotType type)
{
	MonoCompileGC *gcfg = (MonoCompileGC*)cfg->gc_info;

	if (!cfg->compute_gc_maps)
		return;

	g_assert (slot_offset % SIZEOF_SLOT == 0);

	gcfg->stack_slots_from_fp = g_slist_prepend_mempool (cfg->mempool, gcfg->stack_slots_from_fp, GINT_TO_POINTER (((slot_offset) << 16) | type));
}

/*
 * mini_gc_set_slot_type_from_cfa:
 *
 *   Set the GC slot type of the stack slot identified by SLOT_OFFSET, which should be
 * relative to the DWARF CFA value. This should be called from mono_arch_emit_prolog ().
 * If type is STACK_REF, the slot is assumed to be live from the end of the prolog until
 * the end of the method. By default, all stack slots are type PIN, so there is no need to
 * call this function for those slots.
 */
void
mini_gc_set_slot_type_from_cfa (MonoCompile *cfg, int slot_offset, GCSlotType type)
{
	MonoCompileGC *gcfg = (MonoCompileGC*)cfg->gc_info;
	int slot = - (slot_offset / SIZEOF_SLOT);

	if (!cfg->compute_gc_maps)
		return;

	g_assert (slot_offset <= 0);
	g_assert (slot_offset % SIZEOF_SLOT == 0);

	gcfg->stack_slots_from_cfa = g_slist_prepend_mempool (cfg->mempool, gcfg->stack_slots_from_cfa, GUINT_TO_POINTER (((slot) << 16) | type));
}

static int
fp_offset_to_slot (MonoCompile *cfg, int offset)
{
	MonoCompileGC *gcfg = cfg->gc_info;

	return (offset - gcfg->min_offset) / SIZEOF_SLOT;
}

static int
slot_to_fp_offset (MonoCompile *cfg, int slot)
{
	MonoCompileGC *gcfg = cfg->gc_info;

	return (slot * SIZEOF_SLOT) + gcfg->min_offset;
}

static MONO_ALWAYS_INLINE void
set_slot (MonoCompileGC *gcfg, int slot, int callsite_index, GCSlotType type)
{
	g_assert (slot >= 0 && slot < gcfg->nslots);

	if (type == SLOT_PIN) {
		clear_bit (gcfg->stack_ref_bitmap, gcfg->stack_bitmap_width, slot, callsite_index);
		set_bit (gcfg->stack_pin_bitmap, gcfg->stack_bitmap_width, slot, callsite_index);
	} else if (type == SLOT_REF) {
		set_bit (gcfg->stack_ref_bitmap, gcfg->stack_bitmap_width, slot, callsite_index);
		clear_bit (gcfg->stack_pin_bitmap, gcfg->stack_bitmap_width, slot, callsite_index);
	} else if (type == SLOT_NOREF) {
		clear_bit (gcfg->stack_ref_bitmap, gcfg->stack_bitmap_width, slot, callsite_index);
		clear_bit (gcfg->stack_pin_bitmap, gcfg->stack_bitmap_width, slot, callsite_index);
	}
}

static void
set_slot_everywhere (MonoCompileGC *gcfg, int slot, GCSlotType type)
{
	int width, pos;
	guint8 *ref_bitmap, *pin_bitmap;

	/*
	int cindex;

	for (cindex = 0; cindex < gcfg->ncallsites; ++cindex)
		set_slot (gcfg, slot, cindex, type);
	*/
	ref_bitmap = gcfg->stack_ref_bitmap;
	pin_bitmap = gcfg->stack_pin_bitmap;
	width = gcfg->stack_bitmap_width;
	pos = width * slot;

	if (type == SLOT_PIN) {
		memset (ref_bitmap + pos, 0, width);
		memset (pin_bitmap + pos, 0xff, width);
	} else if (type == SLOT_REF) {
		memset (ref_bitmap + pos, 0xff, width);
		memset (pin_bitmap + pos, 0, width);
	} else if (type == SLOT_NOREF) {
		memset (ref_bitmap + pos, 0, width);
		memset (pin_bitmap + pos, 0, width);
	}
}

static void
set_slot_in_range (MonoCompileGC *gcfg, int slot, int from, int to, GCSlotType type)
{
	int cindex;

	for (cindex = 0; cindex < gcfg->ncallsites; ++cindex) {
		int callsite_offset = gcfg->callsites [cindex]->pc_offset;
		if (callsite_offset >= from && callsite_offset < to)
			set_slot (gcfg, slot, cindex, type);
	}
}

static void
set_reg_slot (MonoCompileGC *gcfg, int slot, int callsite_index, GCSlotType type)
{
	g_assert (slot >= 0 && slot < gcfg->nregs);

	if (type == SLOT_PIN) {
		clear_bit (gcfg->reg_ref_bitmap, gcfg->reg_bitmap_width, slot, callsite_index);
		set_bit (gcfg->reg_pin_bitmap, gcfg->reg_bitmap_width, slot, callsite_index);
	} else if (type == SLOT_REF) {
		set_bit (gcfg->reg_ref_bitmap, gcfg->reg_bitmap_width, slot, callsite_index);
		clear_bit (gcfg->reg_pin_bitmap, gcfg->reg_bitmap_width, slot, callsite_index);
	} else if (type == SLOT_NOREF) {
		clear_bit (gcfg->reg_ref_bitmap, gcfg->reg_bitmap_width, slot, callsite_index);
		clear_bit (gcfg->reg_pin_bitmap, gcfg->reg_bitmap_width, slot, callsite_index);
	}
}

static void
set_reg_slot_everywhere (MonoCompileGC *gcfg, int slot, GCSlotType type)
{
	int cindex;

	for (cindex = 0; cindex < gcfg->ncallsites; ++cindex)
		set_reg_slot (gcfg, slot, cindex, type);
}

static void
set_reg_slot_in_range (MonoCompileGC *gcfg, int slot, int from, int to, GCSlotType type)
{
	int cindex;

	for (cindex = 0; cindex < gcfg->ncallsites; ++cindex) {
		int callsite_offset = gcfg->callsites [cindex]->pc_offset;
		if (callsite_offset >= from && callsite_offset < to)
			set_reg_slot (gcfg, slot, cindex, type);
	}
}

static void
process_spill_slots (MonoCompile *cfg)
{
	MonoCompileGC *gcfg = cfg->gc_info;
	MonoBasicBlock *bb;
	GSList *l;
	int i;

	/* Mark all ref/pin spill slots as NOREF by default outside of their live range */
	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		for (l = bb->spill_slot_defs; l; l = l->next) {
			MonoInst *def = l->data;
			int spill_slot = def->inst_c0;
			int bank = def->inst_c1;
			int offset = cfg->spill_info [bank][spill_slot].offset;
			int slot = fp_offset_to_slot (cfg, offset);

			if (bank == MONO_REG_INT_MP || bank == MONO_REG_INT_REF)
				set_slot_everywhere (gcfg, slot, SLOT_NOREF);
		}
	}

	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		for (l = bb->spill_slot_defs; l; l = l->next) {
			MonoInst *def = l->data;
			int spill_slot = def->inst_c0;
			int bank = def->inst_c1;
			int offset = cfg->spill_info [bank][spill_slot].offset;
			int slot = fp_offset_to_slot (cfg, offset);
			GCSlotType type;

			if (bank == MONO_REG_INT_MP)
				type = SLOT_PIN;
			else
				type = SLOT_REF;

			/*
			 * Extend the live interval for the GC tracked spill slots
			 * defined in this bblock.
			 * FIXME: This is not needed.
			 */
			set_slot_in_range (gcfg, slot, def->backend.pc_offset, bb->native_offset + bb->native_length, type);

			if (cfg->verbose_level > 1)
				printf ("\t%s spill slot at %s0x%x(fp) (slot = %d)\n", slot_type_to_string (type), offset >= 0 ? "" : "-", ABS (offset), slot);
		}
	}

	/* Set fp spill slots to NOREF */
	for (i = 0; i < cfg->spill_info_len [MONO_REG_DOUBLE]; ++i) {
		int offset = cfg->spill_info [MONO_REG_DOUBLE][i].offset;
		int slot;

		if (offset == -1)
			continue;

		slot = fp_offset_to_slot (cfg, offset);

		set_slot_everywhere (gcfg, slot, SLOT_NOREF);
		/* FIXME: 32 bit */
		if (cfg->verbose_level > 1)
			printf ("\tfp spill slot at %s0x%x(fp) (slot = %d)\n", offset >= 0 ? "" : "-", ABS (offset), slot);
	}

	/* Set int spill slots to NOREF */
	for (i = 0; i < cfg->spill_info_len [MONO_REG_INT]; ++i) {
		int offset = cfg->spill_info [MONO_REG_INT][i].offset;
		int slot;

		if (offset == -1)
			continue;

		slot = fp_offset_to_slot (cfg, offset);

		set_slot_everywhere (gcfg, slot, SLOT_NOREF);
		if (cfg->verbose_level > 1)
			printf ("\tint spill slot at %s0x%x(fp) (slot = %d)\n", offset >= 0 ? "" : "-", ABS (offset), slot);
	}
}

/*
 * process_other_slots:
 *
 *   Process stack slots registered using mini_gc_set_slot_type_... ().
 */
static void
process_other_slots (MonoCompile *cfg)
{
	MonoCompileGC *gcfg = cfg->gc_info;
	GSList *l;

	/* Relative to the CFA */
	for (l = gcfg->stack_slots_from_cfa; l; l = l->next) {
		guint data = GPOINTER_TO_UINT (l->data);
		int cfa_slot = data >> 16;
		GCSlotType type = data & 0xff;
		int slot;
		
		/*
		 * Map the cfa relative slot to an fp relative slot.
		 * slot_addr == cfa - <cfa_slot>*4/8
		 * fp + cfa_offset == cfa
		 * -> slot_addr == fp + (cfa_offset - <cfa_slot>*4/8)
		 */
		slot = (cfg->cfa_offset / SIZEOF_SLOT) - cfa_slot - (gcfg->min_offset / SIZEOF_SLOT);

		set_slot_everywhere (gcfg, slot, type);

		if (cfg->verbose_level > 1) {
			int fp_offset = slot_to_fp_offset (cfg, slot);
			if (type == SLOT_NOREF)
				printf ("\tnoref slot at %s0x%x(fp) (slot = %d) (cfa - 0x%x)\n", fp_offset >= 0 ? "" : "-", ABS (fp_offset), slot, (int)(cfa_slot * SIZEOF_SLOT));
		}
	}

	/* Relative to the FP */
	for (l = gcfg->stack_slots_from_fp; l; l = l->next) {
		gint data = GPOINTER_TO_INT (l->data);
		int offset = data >> 16;
		GCSlotType type = data & 0xff;
		int slot;
		
		slot = fp_offset_to_slot (cfg, offset);

		set_slot_everywhere (gcfg, slot, type);

		/* Liveness for these slots is handled by process_spill_slots () */

		if (cfg->verbose_level > 1) {
			if (type == SLOT_REF)
				printf ("\tref slot at fp+0x%x (slot = %d)\n", offset, slot);
			else if (type == SLOT_NOREF)
				printf ("\tnoref slot at 0x%x(fp) (slot = %d)\n", offset, slot);
		}
	}
}

static gsize*
get_vtype_bitmap (MonoType *t, int *numbits)
{
	MonoClass *klass = mono_class_from_mono_type_internal (t);

	if (klass->generic_container || mono_class_is_open_constructed_type (t)) {
		/* FIXME: Generic sharing */
		return NULL;
	} else {
		mono_class_compute_gc_descriptor (klass);

		return mono_gc_get_bitmap_for_descr (klass->gc_descr, numbits);
	}
}

static const char*
get_offset_sign (int offset)
{
	return offset < 0 ? "-" : "+";
}

static int
get_offset_val (int offset)
{
	return offset < 0 ? (- offset) : offset;
}

static void
process_variables (MonoCompile *cfg)
{
	MonoCompileGC *gcfg = cfg->gc_info;
	MonoMethodSignature *sig = mono_method_signature_internal (cfg->method);
	int i, locals_min_slot, locals_max_slot, cindex;
	MonoBasicBlock *bb;
	MonoInst *tmp;
	int *pc_offsets;
	int locals_min_offset = gcfg->locals_min_offset;
	int locals_max_offset = gcfg->locals_max_offset;

	/* Slots for locals are NOREF by default */
	locals_min_slot = (locals_min_offset - gcfg->min_offset) / SIZEOF_SLOT;
	locals_max_slot = (locals_max_offset - gcfg->min_offset) / SIZEOF_SLOT;
	for (i = locals_min_slot; i < locals_max_slot; ++i) {
		set_slot_everywhere (gcfg, i, SLOT_NOREF);
	}

	/*
	 * Compute the offset where variables are initialized in the first bblock, if any.
	 */
	pc_offsets = g_new0 (int, cfg->next_vreg);

	bb = cfg->bb_entry->next_bb;
	MONO_BB_FOR_EACH_INS (bb, tmp) {
		if (tmp->opcode == OP_GC_LIVENESS_DEF) {
			int vreg = tmp->inst_c1;
			if (pc_offsets [vreg] == 0) {
				g_assert (tmp->backend.pc_offset > 0);
				pc_offsets [vreg] = tmp->backend.pc_offset;
			}
		}
	}

	/*
	 * Stack slots holding arguments are initialized in the prolog.
	 * This means we can treat them alive for the whole method.
	 */
	for (i = 0; i < cfg->num_varinfo; i++) {
		MonoInst *ins = cfg->varinfo [i];
		MonoType *t = ins->inst_vtype;
		MonoMethodVar *vmv;
		guint32 pos;
		gboolean byref, is_this = FALSE;
		gboolean is_arg = i < cfg->locals_start;

		if (ins == cfg->ret) {
			if (!(ins->opcode == OP_REGOFFSET && MONO_TYPE_ISSTRUCT (t)))
				continue;
		}

		vmv = MONO_VARINFO (cfg, i);

		/* For some reason, 'this' is byref */
		if (sig->hasthis && ins == cfg->args [0] && !cfg->method->klass->valuetype) {
			t = m_class_get_byval_arg (cfg->method->klass);
			is_this = TRUE;
		}

		byref = t->byref;

		if (ins->opcode == OP_REGVAR) {
			int hreg;
			GCSlotType slot_type;

			t = mini_get_underlying_type (t);

			hreg = ins->dreg;
			g_assert (hreg < MONO_MAX_IREGS);

			if (byref)
				slot_type = SLOT_PIN;
			else
				slot_type = mini_type_is_reference (t) ? SLOT_REF : SLOT_NOREF;

			if (slot_type == SLOT_PIN) {
				/* These have no live interval, be conservative */
				set_reg_slot_everywhere (gcfg, hreg, slot_type);
			} else {
				/*
				 * Unlike variables allocated to the stack, we generate liveness info
				 * for noref vars in registers in mono_spill_global_vars (), because
				 * knowing that a register doesn't contain a ref allows us to mark its save
				 * locations precisely.
				 */
				for (cindex = 0; cindex < gcfg->ncallsites; ++cindex)
					if (gcfg->callsites [cindex]->liveness [i / 8] & (1 << (i % 8)))
						set_reg_slot (gcfg, hreg, cindex, slot_type);
			}

			if (cfg->verbose_level > 1) {
				printf ("\t%s %sreg %s(R%d)\n", slot_type_to_string (slot_type), is_arg ? "arg " : "", mono_arch_regname (hreg), vmv->vreg);
			}

			continue;
		}

		if (ins->opcode != OP_REGOFFSET)
			continue;

		if (ins->inst_offset % SIZEOF_SLOT != 0)
			continue;

		pos = fp_offset_to_slot (cfg, ins->inst_offset);

		if (is_arg && ins->flags & MONO_INST_IS_DEAD) {
			/* These do not get stored in the prolog */
			set_slot_everywhere (gcfg, pos, SLOT_NOREF);

			if (cfg->verbose_level > 1) {
				printf ("\tdead arg at fp%s0x%x (slot = %d): %s\n", get_offset_sign (ins->inst_offset), get_offset_val (ins->inst_offset), pos, mono_type_full_name (ins->inst_vtype));
			}
			continue;
		}

		if (MONO_TYPE_ISSTRUCT (t)) {
			int numbits = 0, j;
			gsize *bitmap = NULL;
			gboolean pin = FALSE;
			int size;
			int size_in_slots;
			
			if (ins->backend.is_pinvoke)
				size = mono_class_native_size (ins->klass, NULL);
			else
				size = mono_class_value_size (ins->klass, NULL);
			size_in_slots = ALIGN_TO (size, SIZEOF_SLOT) / SIZEOF_SLOT;

			if (cfg->verbose_level > 1)
				printf ("\tvtype R%d at %s0x%x(fp)-%s0x%x(fp) (slot %d-%d): %s\n", vmv->vreg, get_offset_sign (ins->inst_offset), get_offset_val (ins->inst_offset), get_offset_sign (ins->inst_offset), get_offset_val (ins->inst_offset + (size_in_slots * SIZEOF_SLOT)), pos, pos + size_in_slots, mono_type_full_name (ins->inst_vtype));

			if (!ins->klass->has_references) {
				if (is_arg) {
					for (j = 0; j < size_in_slots; ++j)
						set_slot_everywhere (gcfg, pos + j, SLOT_NOREF);
				}
				continue;
			}

			bitmap = get_vtype_bitmap (t, &numbits);
			if (!bitmap)
				pin = TRUE;

			/*
			 * Most vtypes are marked volatile because of the LDADDR instructions,
			 * and they have no liveness information since they are decomposed
			 * before the liveness pass. We emit OP_GC_LIVENESS_DEF instructions for
			 * them during VZERO decomposition.
			 */
			if (!is_arg) {
				if (!pc_offsets [vmv->vreg])
					pin = TRUE;

				if (ins->backend.is_pinvoke)
					pin = TRUE;
			}

			if (bitmap) {
				for (cindex = 0; cindex < gcfg->ncallsites; ++cindex) {
					if (gcfg->callsites [cindex]->pc_offset > pc_offsets [vmv->vreg]) {
						for (j = 0; j < numbits; ++j) {
							if (bitmap [j / GC_BITS_PER_WORD] & ((gsize)1 << (j % GC_BITS_PER_WORD))) {
								/* The descriptor is for the boxed object */
								set_slot (gcfg, (pos + j - (MONO_ABI_SIZEOF (MonoObject) / SIZEOF_SLOT)), cindex, pin ? SLOT_PIN : SLOT_REF);
							}
						}
					}
				}

				if (cfg->verbose_level > 1) {
					for (j = 0; j < numbits; ++j) {
						if (bitmap [j / GC_BITS_PER_WORD] & ((gsize)1 << (j % GC_BITS_PER_WORD)))
							printf ("\t\t%s slot at 0x%x(fp) (slot = %d)\n", pin ? "pin" : "ref", (int)(ins->inst_offset + (j * SIZEOF_SLOT)), (int)(pos + j - (MONO_ABI_SIZEOF (MonoObject) / SIZEOF_SLOT)));
					}
				}
			} else {
				if (cfg->verbose_level > 1)
					printf ("\t\tpinned\n");
				for (j = 0; j < size_in_slots; ++j) {
					set_slot_everywhere (gcfg, pos + j, SLOT_PIN);
				}
			}

			g_free (bitmap);

			continue;
		}

		if (!is_arg && (ins->inst_offset < gcfg->min_offset || ins->inst_offset >= gcfg->max_offset))
			/* Vret addr etc. */
			continue;

		if (t->byref) {
			if (is_arg) {
				set_slot_everywhere (gcfg, pos, SLOT_PIN);
			} else {
				for (cindex = 0; cindex < gcfg->ncallsites; ++cindex)
					if (gcfg->callsites [cindex]->liveness [i / 8] & (1 << (i % 8)))
						set_slot (gcfg, pos, cindex, SLOT_PIN);
			}
			if (cfg->verbose_level > 1)
				printf ("\tbyref at %s0x%x(fp) (R%d, slot = %d): %s\n", ins->inst_offset < 0 ? "-" : "", (ins->inst_offset < 0) ? -(int)ins->inst_offset : (int)ins->inst_offset, vmv->vreg, pos, mono_type_full_name (ins->inst_vtype));
			continue;
		}

		/*
		 * This is currently disabled, but could be enabled to debug crashes.
		 */
#if 0
		if (t->type == MONO_TYPE_I) {
			/*
			 * Variables created in mono_handle_global_vregs have type I, but they
			 * could hold GC refs since the vregs they were created from might not been
			 * marked as holding a GC ref. So be conservative.
			 */
			set_slot_everywhere (gcfg, pos, SLOT_PIN);
			continue;
		}
#endif

		t = mini_get_underlying_type (t);

		if (!mini_type_is_reference (t)) {
			set_slot_everywhere (gcfg, pos, SLOT_NOREF);
			if (cfg->verbose_level > 1)
				printf ("\tnoref%s at %s0x%x(fp) (R%d, slot = %d): %s\n", (is_arg ? " arg" : ""), ins->inst_offset < 0 ? "-" : "", (ins->inst_offset < 0) ? -(int)ins->inst_offset : (int)ins->inst_offset, vmv->vreg, pos, mono_type_full_name (ins->inst_vtype));
			if (!t->byref && sizeof (host_mgreg_t) == 4 && (t->type == MONO_TYPE_I8 || t->type == MONO_TYPE_U8 || t->type == MONO_TYPE_R8)) {
				set_slot_everywhere (gcfg, pos + 1, SLOT_NOREF);
				if (cfg->verbose_level > 1)
					printf ("\tnoref at %s0x%x(fp) (R%d, slot = %d): %s\n", ins->inst_offset < 0 ? "-" : "", (ins->inst_offset < 0) ? -(int)(ins->inst_offset + 4) : (int)ins->inst_offset + 4, vmv->vreg, pos + 1, mono_type_full_name (ins->inst_vtype));
			}
			continue;
		}

		/* 'this' is marked INDIRECT for gshared methods */
		if (ins->flags & (MONO_INST_VOLATILE | MONO_INST_INDIRECT) && !is_this) {
			/*
			 * For volatile variables, treat them alive from the point they are
			 * initialized in the first bblock until the end of the method.
			 */
			if (is_arg) {
				set_slot_everywhere (gcfg, pos, SLOT_REF);
			} else if (pc_offsets [vmv->vreg]) {
				set_slot_in_range (gcfg, pos, 0, pc_offsets [vmv->vreg], SLOT_PIN);
				set_slot_in_range (gcfg, pos, pc_offsets [vmv->vreg], cfg->code_size, SLOT_REF);
			} else {
				set_slot_everywhere (gcfg, pos, SLOT_PIN);
			}
			if (cfg->verbose_level > 1)
				printf ("\tvolatile ref at %s0x%x(fp) (R%d, slot = %d): %s\n", ins->inst_offset < 0 ? "-" : "", (ins->inst_offset < 0) ? -(int)ins->inst_offset : (int)ins->inst_offset, vmv->vreg, pos, mono_type_full_name (ins->inst_vtype));
			continue;
		}

		if (is_arg) {
			/* Live for the whole method */
			set_slot_everywhere (gcfg, pos, SLOT_REF);
		} else {
			for (cindex = 0; cindex < gcfg->ncallsites; ++cindex)
				if (gcfg->callsites [cindex]->liveness [i / 8] & (1 << (i % 8)))
					set_slot (gcfg, pos, cindex, SLOT_REF);
		}

		if (cfg->verbose_level > 1) {
			printf ("\tref%s at %s0x%x(fp) (R%d, slot = %d): %s\n", (is_arg ? " arg" : ""), ins->inst_offset < 0 ? "-" : "", (ins->inst_offset < 0) ? -(int)ins->inst_offset : (int)ins->inst_offset, vmv->vreg, pos, mono_type_full_name (ins->inst_vtype));
		}
	}

	g_free (pc_offsets);
}

static int
sp_offset_to_fp_offset (MonoCompile *cfg, int sp_offset)
{
	/* 
	 * Convert a sp relative offset to a slot index. This is
	 * platform specific.
	 */
#ifdef TARGET_AMD64
	/* fp = sp + offset */
	g_assert (cfg->frame_reg == AMD64_RBP);
	return (- cfg->arch.sp_fp_offset + sp_offset);
#elif defined(TARGET_X86)
	/* The offset is computed from the sp at the start of the call sequence */
	g_assert (cfg->frame_reg == X86_EBP);
#ifdef MONO_X86_NO_PUSHES
	return (- cfg->arch.sp_fp_offset + sp_offset);
#else
	return (- cfg->arch.sp_fp_offset - sp_offset);	
#endif
#else
	NOT_IMPLEMENTED;
	return -1;
#endif
}

static void
process_param_area_slots (MonoCompile *cfg)
{
	MonoCompileGC *gcfg = cfg->gc_info;
	int cindex, i;
	gboolean *is_param;

	/*
	 * These slots are used for passing parameters during calls. They are sp relative, not
	 * fp relative, so they are harder to handle.
	 */
	if (cfg->flags & MONO_CFG_HAS_ALLOCA)
		/* The distance between fp and sp is not constant */
		return;

	is_param = mono_mempool_alloc0 (cfg->mempool, gcfg->nslots * sizeof (gboolean));

	for (cindex = 0; cindex < gcfg->ncallsites; ++cindex) {
		GCCallSite *callsite = gcfg->callsites [cindex];
		GSList *l;

		for (l = callsite->param_slots; l; l = l->next) {
			MonoInst *def = l->data;
			MonoType *t = def->inst_vtype;
			int sp_offset = def->inst_offset;
			int fp_offset = sp_offset_to_fp_offset (cfg, sp_offset);
			int slot = fp_offset_to_slot (cfg, fp_offset);
			guint32 align;
			guint32 size;

			if (MONO_TYPE_ISSTRUCT (t)) {
				size = mini_type_stack_size_full (t, &align, FALSE);
			} else {
				size = sizeof (target_mgreg_t);
			}

			for (i = 0; i < size / sizeof (target_mgreg_t); ++i) {
				g_assert (slot + i >= 0 && slot + i < gcfg->nslots);
				is_param [slot + i] = TRUE;
			}
		}
	}

	/* All param area slots are noref by default */
	for (i = 0; i < gcfg->nslots; ++i) {
		if (is_param [i])
			set_slot_everywhere (gcfg, i, SLOT_NOREF);
	}

	/*
	 * We treat param area slots as being part of the callee's frame, to be able to handle tailcalls which overwrite
	 * the argument area of the caller.
	 */
}

static void
process_finally_clauses (MonoCompile *cfg)
{
	MonoCompileGC *gcfg = cfg->gc_info;
	GCCallSite **callsites;
	int ncallsites;
	gboolean has_finally;
	int i, j, nslots, nregs;

	ncallsites = gcfg->ncallsites;
	nslots = gcfg->nslots;
	nregs = gcfg->nregs;
	callsites = gcfg->callsites;

	/*
	 * The calls to the finally clauses don't show up in the cfg. See
	 * test_0_liveness_8 ().
	 * Variables accessed inside the finally clause are already marked VOLATILE by
	 * mono_liveness_handle_exception_clauses (). Variables not accessed inside the finally clause have
	 * correct liveness outside the finally clause. So mark them PIN inside the finally clauses.
	 */
	has_finally = FALSE;
	for (i = 0; i < cfg->header->num_clauses; ++i) {
		MonoExceptionClause *clause = &cfg->header->clauses [i];

		if (clause->flags == MONO_EXCEPTION_CLAUSE_FINALLY) {
			has_finally = TRUE;
		}
	}
	if (has_finally) {
		if (cfg->verbose_level > 1)
			printf ("\tMethod has finally clauses, pessimizing live ranges.\n");
		for (j = 0; j < ncallsites; ++j) {
			MonoBasicBlock *bb = callsites [j]->bb;
			MonoExceptionClause *clause;
			gboolean is_in_finally = FALSE;

			for (i = 0; i < cfg->header->num_clauses; ++i) {
				clause = &cfg->header->clauses [i];
			   
				if (MONO_OFFSET_IN_HANDLER (clause, bb->real_offset)) {
					if (clause->flags == MONO_EXCEPTION_CLAUSE_FINALLY) {
						is_in_finally = TRUE;
						break;
					}
				}
			}

			if (is_in_finally) {
				for (i = 0; i < nslots; ++i)
					set_slot (gcfg, i, j, SLOT_PIN);
				for (i = 0; i < nregs; ++i)
					set_reg_slot (gcfg, i, j, SLOT_PIN);
			}
		}
	}
}

static void
compute_frame_size (MonoCompile *cfg)
{
	int i, locals_min_offset, locals_max_offset, cfa_min_offset, cfa_max_offset;
	int min_offset, max_offset;
	MonoCompileGC *gcfg = cfg->gc_info;
	MonoMethodSignature *sig = mono_method_signature_internal (cfg->method);
	GSList *l;

	/* Compute min/max offsets from the fp */

	/* Locals */
#if defined(TARGET_AMD64) || defined(TARGET_X86) || defined(TARGET_ARM) || defined(TARGET_S390X)
	locals_min_offset = ALIGN_TO (cfg->locals_min_stack_offset, SIZEOF_SLOT);
	locals_max_offset = cfg->locals_max_stack_offset;
#else
	/* min/max stack offset needs to be computed in mono_arch_allocate_vars () */
	NOT_IMPLEMENTED;
#endif

	locals_min_offset = ALIGN_TO (locals_min_offset, SIZEOF_SLOT);
	locals_max_offset = ALIGN_TO (locals_max_offset, SIZEOF_SLOT);

	min_offset = locals_min_offset;
	max_offset = locals_max_offset;

	/* Arguments */
	for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
		MonoInst *ins = cfg->args [i];

		if (ins->opcode == OP_REGOFFSET) {
			int size, size_in_slots;
			size = mini_type_stack_size_full (ins->inst_vtype, NULL, ins->backend.is_pinvoke);
			size_in_slots = ALIGN_TO (size, SIZEOF_SLOT) / SIZEOF_SLOT;

			min_offset = MIN (min_offset, ins->inst_offset);
			max_offset = MAX ((int)max_offset, (int)(ins->inst_offset + (size_in_slots * SIZEOF_SLOT)));
		}
	}

	/* Cfa slots */
	g_assert (cfg->frame_reg == cfg->cfa_reg);
	g_assert (cfg->cfa_offset > 0);
	cfa_min_offset = 0;
	cfa_max_offset = cfg->cfa_offset;

	min_offset = MIN (min_offset, cfa_min_offset);
	max_offset = MAX (max_offset, cfa_max_offset);

	/* Fp relative slots */
	for (l = gcfg->stack_slots_from_fp; l; l = l->next) {
		gint data = GPOINTER_TO_INT (l->data);
		int offset = data >> 16;

		min_offset = MIN (min_offset, offset);
	}

	/* Spill slots */
	if (!(cfg->flags & MONO_CFG_HAS_SPILLUP)) {
		int stack_offset = ALIGN_TO (cfg->stack_offset, SIZEOF_SLOT);
		min_offset = MIN (min_offset, (-stack_offset));
	}

	/* Param area slots */
#ifdef TARGET_AMD64
	min_offset = MIN (min_offset, -cfg->arch.sp_fp_offset);
#elif defined(TARGET_X86)
#ifdef MONO_X86_NO_PUSHES
	min_offset = MIN (min_offset, -cfg->arch.sp_fp_offset);
#else
	min_offset = MIN (min_offset, - (cfg->arch.sp_fp_offset + cfg->arch.param_area_size));
#endif
#elif defined(TARGET_ARM)
	// FIXME:
#elif defined(TARGET_s390X)
	// FIXME:
#else
	NOT_IMPLEMENTED;
#endif

	gcfg->min_offset = min_offset;
	gcfg->max_offset = max_offset;
	gcfg->locals_min_offset = locals_min_offset;
	gcfg->locals_max_offset = locals_max_offset;
}

static void
init_gcfg (MonoCompile *cfg)
{
	int i, nregs, nslots;
	MonoCompileGC *gcfg = cfg->gc_info;
	GCCallSite **callsites;
	int ncallsites;
	MonoBasicBlock *bb;
	GSList *l;

	/*
	 * Collect callsites
	 */
	ncallsites = 0;
	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		ncallsites += g_slist_length (bb->gc_callsites);
	}
	callsites = mono_mempool_alloc0 (cfg->mempool, ncallsites * sizeof (GCCallSite*));
	i = 0;
	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		for (l = bb->gc_callsites; l; l = l->next)
			callsites [i++] = l->data;
	}

	/* The callsites should already be ordered by pc offset */
	for (i = 1; i < ncallsites; ++i)
		g_assert (callsites [i - 1]->pc_offset < callsites [i]->pc_offset);

	/*
	 * The stack frame looks like this:
	 *
	 * <fp + max_offset> == cfa ->  <end of previous frame>
	 *                              <other stack slots>
	 *                              <locals>
	 *                              <other stack slots>
	 * fp + min_offset          ->
	 * ...
	 * fp                       ->
	 */

	if (cfg->verbose_level > 1)
		printf ("GC Map for %s: 0x%x-0x%x\n", mono_method_full_name (cfg->method, TRUE), gcfg->min_offset, gcfg->max_offset);

	nslots = (gcfg->max_offset - gcfg->min_offset) / SIZEOF_SLOT;
	nregs = NREGS;

	gcfg->nslots = nslots;
	gcfg->nregs = nregs;
	gcfg->callsites = callsites;
	gcfg->ncallsites = ncallsites;
	gcfg->stack_bitmap_width = ALIGN_TO (ncallsites, 8) / 8;
	gcfg->reg_bitmap_width = ALIGN_TO (ncallsites, 8) / 8;
	gcfg->stack_ref_bitmap = mono_mempool_alloc0 (cfg->mempool, gcfg->stack_bitmap_width * nslots);
	gcfg->stack_pin_bitmap = mono_mempool_alloc0 (cfg->mempool, gcfg->stack_bitmap_width * nslots);
	gcfg->reg_ref_bitmap = mono_mempool_alloc0 (cfg->mempool, gcfg->reg_bitmap_width * nregs);
	gcfg->reg_pin_bitmap = mono_mempool_alloc0 (cfg->mempool, gcfg->reg_bitmap_width * nregs);

	/* All slots start out as PIN */
	memset (gcfg->stack_pin_bitmap, 0xff, gcfg->stack_bitmap_width * nregs);
	for (i = 0; i < nregs; ++i) {
		/*
		 * By default, registers are NOREF.
		 * It is possible for a callee to save them before being defined in this method,
		 * but the saved value is dead too, so it doesn't need to be marked.
		 */
		if ((cfg->used_int_regs & (1 << i)))
			set_reg_slot_everywhere (gcfg, i, SLOT_NOREF);
	}
}

static gboolean
has_bit_set (guint8 *bitmap, int width, int slot)
{
	int i;
	int pos = width * slot;

	for (i = 0; i < width; ++i) {
		if (bitmap [pos + i])
			break;
	}
	return i < width;
}

static void
create_map (MonoCompile *cfg)
{
	GCMap *map;
	int i, j, nregs, nslots, nref_regs, npin_regs, alloc_size, bitmaps_size, bitmaps_offset;
	int ntypes [16];
	int stack_bitmap_width, stack_bitmap_size, reg_ref_bitmap_width, reg_ref_bitmap_size;
	int reg_pin_bitmap_width, reg_pin_bitmap_size, bindex;
	int start, end;
	gboolean has_ref_slots, has_pin_slots, has_ref_regs, has_pin_regs;
	MonoCompileGC *gcfg = cfg->gc_info;
	GCCallSite **callsites;
	int ncallsites;
	guint8 *bitmap, *bitmaps;
	guint32 reg_ref_mask, reg_pin_mask;

	ncallsites = gcfg->ncallsites;
	nslots = gcfg->nslots;
	nregs = gcfg->nregs;
	callsites = gcfg->callsites;

	/* 
	 * Compute the real size of the bitmap i.e. ignore NOREF columns at the beginning and at
	 * the end. Also, compute whenever the map needs ref/pin bitmaps, and collect stats.
	 */
	has_ref_slots = FALSE;
	has_pin_slots = FALSE;
	start = -1;
	end = -1;
	memset (ntypes, 0, sizeof (ntypes));
	for (i = 0; i < nslots; ++i) {
		gboolean has_ref = FALSE;
		gboolean has_pin = FALSE;

		if (has_bit_set (gcfg->stack_pin_bitmap, gcfg->stack_bitmap_width, i))
			has_pin = TRUE;
		if (has_bit_set (gcfg->stack_ref_bitmap, gcfg->stack_bitmap_width, i))
			has_ref = TRUE;

		if (has_ref)
			has_ref_slots = TRUE;
		if (has_pin)
			has_pin_slots = TRUE;

		if (has_ref)
			ntypes [SLOT_REF] ++;
		else if (has_pin)
			ntypes [SLOT_PIN] ++;
		else
			ntypes [SLOT_NOREF] ++;

		if (has_ref || has_pin) {
			if (start == -1)
				start = i;
			end = i + 1;
		}
	}
	if (start == -1) {
		start = end = nslots;
	} else {
		g_assert (start != -1);
		g_assert (start < end);
	}

	has_ref_regs = FALSE;
	has_pin_regs = FALSE;
	reg_ref_mask = 0;
	reg_pin_mask = 0;
	nref_regs = 0;
	npin_regs = 0;
	for (i = 0; i < nregs; ++i) {
		gboolean has_ref = FALSE;
		gboolean has_pin = FALSE;

		if (!(cfg->used_int_regs & (1 << i)))
			continue;

		if (has_bit_set (gcfg->reg_pin_bitmap, gcfg->reg_bitmap_width, i))
			has_pin = TRUE;
		if (has_bit_set (gcfg->reg_ref_bitmap, gcfg->reg_bitmap_width, i))
			has_ref = TRUE;

		if (has_ref) {
			reg_ref_mask |= (1 << i);
			has_ref_regs = TRUE;
			nref_regs ++;
		}
		if (has_pin) {
			reg_pin_mask |= (1 << i);
			has_pin_regs = TRUE;
			npin_regs ++;
		}
	}

	if (cfg->verbose_level > 1)
		printf ("Slots: %d Start: %d End: %d Refs: %d NoRefs: %d Pin: %d Callsites: %d\n", nslots, start, end, ntypes [SLOT_REF], ntypes [SLOT_NOREF], ntypes [SLOT_PIN], ncallsites);

	/* Create the GC Map */

	/* The work bitmaps have one row for each slot, since this is how we access them during construction */
	stack_bitmap_width = ALIGN_TO (end - start, 8) / 8;
	stack_bitmap_size = stack_bitmap_width * ncallsites;
	reg_ref_bitmap_width = ALIGN_TO (nref_regs, 8) / 8;
	reg_ref_bitmap_size = reg_ref_bitmap_width * ncallsites;
	reg_pin_bitmap_width = ALIGN_TO (npin_regs, 8) / 8;
	reg_pin_bitmap_size = reg_pin_bitmap_width * ncallsites;
	bitmaps_size = (has_ref_slots ? stack_bitmap_size : 0) + (has_pin_slots ? stack_bitmap_size : 0) + (has_ref_regs ? reg_ref_bitmap_size : 0) + (has_pin_regs ? reg_pin_bitmap_size : 0);
	
	map = mono_mempool_alloc0 (cfg->mempool, sizeof (GCMap));

	map->frame_reg = cfg->frame_reg;
	map->start_offset = gcfg->min_offset;
	map->end_offset = gcfg->min_offset + (nslots * SIZEOF_SLOT);
	map->map_offset = start * SIZEOF_SLOT;
	map->nslots = end - start;
	map->has_ref_slots = has_ref_slots;
	map->has_pin_slots = has_pin_slots;
	map->has_ref_regs = has_ref_regs;
	map->has_pin_regs = has_pin_regs;
	g_assert (nregs < 32);
	map->used_int_regs = cfg->used_int_regs;
	map->reg_ref_mask = reg_ref_mask;
	map->reg_pin_mask = reg_pin_mask;
	map->nref_regs = nref_regs;
	map->npin_regs = npin_regs;

	bitmaps = mono_mempool_alloc0 (cfg->mempool, bitmaps_size);

	bitmaps_offset = 0;
	if (has_ref_slots) {
		map->stack_ref_bitmap_offset = bitmaps_offset;
		bitmaps_offset += stack_bitmap_size;

		bitmap = &bitmaps [map->stack_ref_bitmap_offset];
		for (i = 0; i < nslots; ++i) {
			for (j = 0; j < ncallsites; ++j) {
				if (get_bit (gcfg->stack_ref_bitmap, gcfg->stack_bitmap_width, i, j))
					set_bit (bitmap, stack_bitmap_width, j, i - start);
			}
		}
	}
	if (has_pin_slots) {
		map->stack_pin_bitmap_offset = bitmaps_offset;
		bitmaps_offset += stack_bitmap_size;

		bitmap = &bitmaps [map->stack_pin_bitmap_offset];
		for (i = 0; i < nslots; ++i) {
			for (j = 0; j < ncallsites; ++j) {
				if (get_bit (gcfg->stack_pin_bitmap, gcfg->stack_bitmap_width, i, j))
					set_bit (bitmap, stack_bitmap_width, j, i - start);
			}
		}
	}
	if (has_ref_regs) {
		map->reg_ref_bitmap_offset = bitmaps_offset;
		bitmaps_offset += reg_ref_bitmap_size;

		bitmap = &bitmaps [map->reg_ref_bitmap_offset];
		bindex = 0;
		for (i = 0; i < nregs; ++i) {
			if (reg_ref_mask & (1 << i)) {
				for (j = 0; j < ncallsites; ++j) {
					if (get_bit (gcfg->reg_ref_bitmap, gcfg->reg_bitmap_width, i, j))
						set_bit (bitmap, reg_ref_bitmap_width, j, bindex);
				}
				bindex ++;
			}
		}
	}
	if (has_pin_regs) {
		map->reg_pin_bitmap_offset = bitmaps_offset;
		bitmaps_offset += reg_pin_bitmap_size;

		bitmap = &bitmaps [map->reg_pin_bitmap_offset];
		bindex = 0;
		for (i = 0; i < nregs; ++i) {
			if (reg_pin_mask & (1 << i)) {
				for (j = 0; j < ncallsites; ++j) {
					if (get_bit (gcfg->reg_pin_bitmap, gcfg->reg_bitmap_width, i, j))
						set_bit (bitmap, reg_pin_bitmap_width, j, bindex);
				}
				bindex ++;
			}
		}
	}

	/* Call sites */
	map->ncallsites = ncallsites;
	if (cfg->code_len < 256)
		map->callsite_entry_size = 1;
	else if (cfg->code_len < 65536)
		map->callsite_entry_size = 2;
	else
		map->callsite_entry_size = 4;

	/* Encode the GC Map */
	{
		guint8 buf [256];
		guint8 *endbuf;
		GCEncodedMap *emap;
		int encoded_size;
		guint8 *p;

		encode_gc_map (map, buf, &endbuf);
		g_assert (endbuf - buf < 256);

		encoded_size = endbuf - buf;
		alloc_size = sizeof (GCEncodedMap) + ALIGN_TO (encoded_size, map->callsite_entry_size) + (map->callsite_entry_size * map->ncallsites) + bitmaps_size;

		emap = mono_mem_manager_alloc0 (cfg->mem_manager, alloc_size);
		//emap->ref_slots = map->ref_slots;

		/* Encoded fixed fields */
		p = &emap->encoded [0];
		//emap->encoded_size = encoded_size;
		memcpy (p, buf, encoded_size);
		p += encoded_size;

		/* Callsite table */
		p = (guint8*)ALIGN_TO ((gsize)p, map->callsite_entry_size);
		if (map->callsite_entry_size == 1) {
			guint8 *offsets = p;
			for (i = 0; i < ncallsites; ++i)
				offsets [i] = callsites [i]->pc_offset;
			UnlockedAdd (&stats.gc_callsites8_size, ncallsites * sizeof (guint8));
		} else if (map->callsite_entry_size == 2) {
			guint16 *offsets = (guint16*)p;
			for (i = 0; i < ncallsites; ++i)
				offsets [i] = callsites [i]->pc_offset;
			UnlockedAdd (&stats.gc_callsites16_size, ncallsites * sizeof (guint16));
		} else {
			guint32 *offsets = (guint32*)p;
			for (i = 0; i < ncallsites; ++i)
				offsets [i] = callsites [i]->pc_offset;
			UnlockedAdd (&stats.gc_callsites32_size, ncallsites * sizeof (guint32));
		}
		p += ncallsites * map->callsite_entry_size;

		/* Bitmaps */
		memcpy (p, bitmaps, bitmaps_size);
		p += bitmaps_size;

		g_assert ((guint8*)p - (guint8*)emap <= alloc_size);

		UnlockedAdd (&stats.gc_maps_size, alloc_size);
		UnlockedAdd (&stats.gc_callsites_size, ncallsites * map->callsite_entry_size);
		UnlockedAdd (&stats.gc_bitmaps_size, bitmaps_size);
		UnlockedAdd (&stats.gc_map_struct_size, sizeof (GCEncodedMap) + encoded_size);

		cfg->jit_info->gc_info = emap;

		cfg->gc_map = (guint8*)emap;
		cfg->gc_map_size = alloc_size;
	}

	UnlockedAdd (&stats.all_slots, nslots);
	UnlockedAdd (&stats.ref_slots, ntypes [SLOT_REF]);
	UnlockedAdd (&stats.noref_slots, ntypes [SLOT_NOREF]);
	UnlockedAdd (&stats.pin_slots, ntypes [SLOT_PIN]);
}

void
mini_gc_create_gc_map (MonoCompile *cfg)
{
	if (!cfg->compute_gc_maps)
		return;

	/*
	 * During marking, all frames except the top frame are at a call site, and we mark the
	 * top frame conservatively. This means that we only need to compute and record
	 * GC maps for call sites.
	 */

	if (!(cfg->comp_done & MONO_COMP_LIVENESS))
		/* Without liveness info, the live ranges are not precise enough */
		return;

	mono_analyze_liveness_gc (cfg);

	compute_frame_size (cfg);

	init_gcfg (cfg);

	process_spill_slots (cfg);
	process_other_slots (cfg);
	process_param_area_slots (cfg);
	process_variables (cfg);
	process_finally_clauses (cfg);

	create_map (cfg);
}

#endif /* DISABLE_JIT */

static void
parse_debug_options (void)
{
	char **opts, **ptr;
	const char *env;

	env = g_getenv ("MONO_GCMAP_DEBUG");
	if (!env)
		return;

	opts = g_strsplit (env, ",", -1);
	for (ptr = opts; ptr && *ptr; ptr ++) {
		/* No options yet */
		fprintf (stderr, "Invalid format for the MONO_GCMAP_DEBUG env variable: '%s'\n", env);
		exit (1);
	}
	g_strfreev (opts);
	g_free (env);
}

void
mini_gc_init (void)
{
	MonoGCCallbacks cb;

	memset (&cb, 0, sizeof (cb));
	cb.thread_attach_func = thread_attach_func;
	cb.thread_detach_func = thread_detach_func;
	cb.thread_suspend_func = thread_suspend_func;
	/* Comment this out to disable precise stack marking */
	cb.thread_mark_func = thread_mark_func;
	cb.get_provenance_func = get_provenance_func;
	if (mono_use_interpreter)
		cb.interp_mark_func = mini_get_interp_callbacks ()->mark_stack;
	mono_gc_set_gc_callbacks (&cb);

	logfile = mono_gc_get_logfile ();

	parse_debug_options ();

	mono_counters_register ("GC Maps size",
							MONO_COUNTER_GC | MONO_COUNTER_INT, &stats.gc_maps_size);
	mono_counters_register ("GC Call Sites size",
							MONO_COUNTER_GC | MONO_COUNTER_INT, &stats.gc_callsites_size);
	mono_counters_register ("GC Bitmaps size",
							MONO_COUNTER_GC | MONO_COUNTER_INT, &stats.gc_bitmaps_size);
	mono_counters_register ("GC Map struct size",
							MONO_COUNTER_GC | MONO_COUNTER_INT, &stats.gc_map_struct_size);
	mono_counters_register ("GC Call Sites encoded using 8 bits",
							MONO_COUNTER_GC | MONO_COUNTER_INT, &stats.gc_callsites8_size);
	mono_counters_register ("GC Call Sites encoded using 16 bits",
							MONO_COUNTER_GC | MONO_COUNTER_INT, &stats.gc_callsites16_size);
	mono_counters_register ("GC Call Sites encoded using 32 bits",
							MONO_COUNTER_GC | MONO_COUNTER_INT, &stats.gc_callsites32_size);

	mono_counters_register ("GC Map slots (all)",
							MONO_COUNTER_GC | MONO_COUNTER_INT, &stats.all_slots);
	mono_counters_register ("GC Map slots (ref)",
							MONO_COUNTER_GC | MONO_COUNTER_INT, &stats.ref_slots);
	mono_counters_register ("GC Map slots (noref)",
							MONO_COUNTER_GC | MONO_COUNTER_INT, &stats.noref_slots);
	mono_counters_register ("GC Map slots (pin)",
							MONO_COUNTER_GC | MONO_COUNTER_INT, &stats.pin_slots);

	mono_counters_register ("GC TLS Data size",
							MONO_COUNTER_GC | MONO_COUNTER_INT, &stats.tlsdata_size);

	mono_counters_register ("Stack space scanned (all)",
							MONO_COUNTER_GC | MONO_COUNTER_INT, &stats.scanned_stacks);
	mono_counters_register ("Stack space scanned (native)",
							MONO_COUNTER_GC | MONO_COUNTER_INT, &stats.scanned_native);
	mono_counters_register ("Stack space scanned (other)",
							MONO_COUNTER_GC | MONO_COUNTER_INT, &stats.scanned_other);
	mono_counters_register ("Stack space scanned (using GC Maps)",
							MONO_COUNTER_GC | MONO_COUNTER_INT, &stats.scanned);
	mono_counters_register ("Stack space scanned (precise)",
							MONO_COUNTER_GC | MONO_COUNTER_INT, &stats.scanned_precisely);
	mono_counters_register ("Stack space scanned (pin)",
							MONO_COUNTER_GC | MONO_COUNTER_INT, &stats.scanned_conservatively);
	mono_counters_register ("Stack space scanned (pin registers)",
							MONO_COUNTER_GC | MONO_COUNTER_INT, &stats.scanned_registers);
}

#else

void
mini_gc_enable_gc_maps_for_aot (void)
{
}

void
mini_gc_init (void)
{
	MonoGCCallbacks cb;
	memset (&cb, 0, sizeof (cb));
	cb.get_provenance_func = get_provenance_func;
	if (mono_use_interpreter)
		cb.interp_mark_func = mini_get_interp_callbacks ()->mark_stack;
	mono_gc_set_gc_callbacks (&cb);
}

#ifndef DISABLE_JIT

static void
mini_gc_init_gc_map (MonoCompile *cfg)
{
}

void
mini_gc_create_gc_map (MonoCompile *cfg)
{
}

void
mini_gc_set_slot_type_from_fp (MonoCompile *cfg, int slot_offset, GCSlotType type)
{
}

void
mini_gc_set_slot_type_from_cfa (MonoCompile *cfg, int slot_offset, GCSlotType type)
{
}

#endif /* DISABLE_JIT */

#endif

#ifndef DISABLE_JIT

/*
 * mini_gc_init_cfg:
 *
 *   Set GC specific options in CFG.
 */
void
mini_gc_init_cfg (MonoCompile *cfg)
{
	if (mono_gc_is_moving ()) {
		cfg->disable_ref_noref_stack_slot_share = TRUE;
		cfg->gen_write_barriers = TRUE;
	}

	mini_gc_init_gc_map (cfg);
}

#endif /* DISABLE_JIT */

/*
 * Problems with the current code:
 * - the stack walk is slow
 * - vtypes/refs used in EH regions are treated conservatively
 * - if the code is finished, less pinning will be done, causing problems because
 *   we promote all surviving objects to old-gen.
 * - the unwind code can't handle a method stopped inside a finally region, it thinks the caller is
 *   another method, but in reality it is either the exception handling code or the CALL_HANDLER opcode.
 *   This manifests in "Unable to find ip offset x in callsite list" assertions.
 * - the unwind code also can't handle frames which are in the epilog, since the unwind info is not
 *   precise there.
 */

/*
 * Ideas for creating smaller GC maps:
 * - remove empty columns from the bitmaps. This requires adding a mask bit array for
 *   each bitmap.
 * - merge reg and stack slot bitmaps, so the unused bits at the end of the reg bitmap are
 *   not wasted.
 * - if the bitmap width is not a multiple of 8, the remaining bits are wasted.
 * - group ref and non-ref stack slots together in mono_allocate_stack_slots ().
 * - add an index for the callsite table so that each entry can be encoded as a 1 byte difference
 *   from an index entry.
 */
