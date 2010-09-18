/*
 * mini-gc.c: GC interface for the mono JIT
 *
 * Author:
 *   Zoltan Varga (vargaz@gmail.com)
 *
 * Copyright 2009 Novell, Inc (http://www.novell.com)
 */

#include "config.h"
#include "mini-gc.h"
#include <mono/metadata/gc-internal.h>

//#if 0
#ifdef HAVE_SGEN_GC

#include <mono/metadata/gc-internal.h>
#include <mono/utils/mono-counters.h>

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
	 * The width of the bitmaps in bytes. This is not equal to the bitmap width at runtime,
	 * since it includes columns which are 0.
	 */
	int bitmap_width;
	/* 
	 * A bitmap whose width equals nslots, and whose height equals ncallsites.
	 * The bitmap contains a 1 if the corresponding stack slot has type SLOT_REF at the
	 * given callsite.
	 */
	guint8 *ref_bitmap;
	/* Same for SLOT_PIN */
	guint8 *pin_bitmap;

	/*
	 * Similar bitmaps for registers. These have width MONO_MAX_IREGS in bits.
	 * FIXME: Only store callee saved regs.
	 */
	int reg_bitmap_width;
	guint8 *reg_ref_bitmap;
	guint8 *reg_pin_bitmap;
} MonoCompileGC;

#define ALIGN_TO(val,align) ((((guint64)val) + ((align) - 1)) & ~((align) - 1))

#if 0
#define DEBUG(s) do { s; } while (0)
#else
#define DEBUG(s)
#endif

#if 1
#define DEBUG_GC_MAP(s) do { s; fflush (stdout); } while (0)
#else
#define DEBUG_GC_MAP(s)
#endif

#define GC_BITS_PER_WORD (sizeof (gsize) * 8)

/*
 * Per-thread data kept by this module. This is stored in the GC and passed to us as
 * parameters, instead of being stored in a TLS variable, since during a collection,
 * only the collection thread is active.
 */
typedef struct {
	MonoLMF *lmf;
	MonoContext ctx;
	gboolean has_context;
	MonoJitTlsData *jit_tls;
} TlsData;

/* 
 * Contains information needed to mark a stack frame.
 * FIXME: Optimize the memory usage.
 */
typedef struct {
	/* The frame pointer register */
	int frame_reg;
	/*
	 * The offsets of the GC tracked area inside the stack frame relative to the frame pointer.
	 * This includes memory which is NOREF thus doesn't need GC maps.
	 */
	int start_offset;
	int end_offset;
	/*
	 * The offset relative to frame_offset where the the memory described by the GC maps begin.
	 */
	int map_offset;
	/* The number of stack slots in the map */
	int nslots;
	/* The number of registers in the map */
	int nregs;
	/* Thw width of the stack bitmap in bytes */
	int bitmap_width;
	/* Thw width of the register bitmap in bytes */
	int reg_bitmap_width;
	guint has_pin_slots : 1;
	guint has_ref_slots : 1;
	guint has_ref_regs : 1;
	guint has_pin_regs : 1;
	/* 
	 * A bitmap whose width is equal to bitmap_width, and whose
	 * height is equal to ncallsites.
	 * The bitmap contains a 1 if the corresponding stack slot has type SLOT_REF at the
	 * given callsite.
	 * FIXME: Compress this.
	 * FIXME: Embed this after the structure.
	 */
	guint8 *ref_bitmap;
	/*
	 * Same for SLOT_PIN. It is possible that the same bit is set in both bitmaps at
     * different callsites, if the slot starts out as PIN, and later changes to REF.
	 */
	guint8 *pin_bitmap;

	/*
	 * Corresponding bitmaps for registers
	 * These have width MONO_MAX_IREGS in bits.
	 * FIXME: Merge these with the normal bitmaps, i.e. reserve the first x slots for them ?
	 */
	guint8 *reg_pin_bitmap;
	guint8 *reg_ref_bitmap;

	/* The registers used by the method */
	guint64 used_regs;

	/*
	 * A bit array marking slots which contain refs.
	 * This is used only for debugging.
	 */
	guint8 *ref_slots;

	/* Callsite offsets */
	int *callsite_offsets;
	int ncallsites;
} GCMap;

/* Statistics */
static guint32 gc_maps_size;

static gpointer
thread_attach_func (void)
{
	return g_new0 (TlsData, 1);
}

static void
thread_suspend_func (gpointer user_data, void *sigctx)
{
	TlsData *tls = user_data;

	if (!tls)
		/* Happens during startup */
		return;

	tls->lmf = mono_get_lmf ();
	if (sigctx) {
		mono_arch_sigctx_to_monoctx (sigctx, &tls->ctx);
		tls->has_context = TRUE;
	} else {
		tls->has_context = FALSE;
	}
	tls->jit_tls = TlsGetValue (mono_jit_tls_id);
}

static int precise_frame_count [2], precise_frame_limit = -1;
static gboolean precise_frame_limit_inited;

/* Stats */
typedef struct {
	int scanned_stacks;
	int scanned;
	int scanned_precisely;
	int scanned_conservatively;
	int scanned_registers;

	int all_slots;
	int noref_slots;
	int ref_slots;
	int pin_slots;
} JITGCStats;

static JITGCStats stats;

#define DEAD_REF ((gpointer)(gssize)0x2a2a2a2a2a2a2a2aULL)

static inline void
set_bit (guint8 *bitmap, int width, int y, int x)
{
	bitmap [(width * y) + (x / 8)] |= (1 << (x % 8));
}

static inline void
clear_bit (guint8 *bitmap, int width, int y, int x)
{
	bitmap [(width * y) + (x / 8)] &= ~(1 << (x % 8));
}

static inline int
get_bit (guint8 *bitmap, int width, int y, int x)
{
	return bitmap [(width * y) + (x / 8)] & (1 << (x % 8));
}

static const char*
slot_type_to_string (StackSlotType type)
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

/*
 * thread_mark_func:
 *
 *   This is called by the GC twice to mark a thread stack. PRECISE is FALSE at the first
 * call, and TRUE at the second. USER_DATA points to a TlsData
 * structure filled up by thread_suspend_func. 
 */
static void
thread_mark_func (gpointer user_data, guint8 *stack_start, guint8 *stack_end, gboolean precise)
{
	TlsData *tls = user_data;
	MonoJitInfo *ji;
	MonoContext ctx, new_ctx;
	MonoLMF *lmf;
	guint8 *stack_limit;
	gboolean last = TRUE;
	GCMap *map;
	guint8* fp, *frame_start, *frame_end;
	int i, pc_offset, cindex;
	int scanned = 0, scanned_precisely, scanned_conservatively, scanned_registers;
	gboolean res;
	StackFrameInfo frame;
	mgreg_t *reg_locations [MONO_MAX_IREGS];
	mgreg_t *new_reg_locations [MONO_MAX_IREGS];

	/* tls == NULL can happen during startup */
	if (mono_thread_internal_current () == NULL || !tls) {
		if (!precise) {
			mono_gc_conservatively_scan_area (stack_start, stack_end);
			stats.scanned_stacks += stack_end - stack_start;
		}
		return;
	}

	lmf = tls->lmf;
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

	DEBUG (printf ("*** %s stack marking %p-%p ***\n", precise ? "Precise" : "Conservative", stack_start, stack_end));

	if (!tls->has_context)
		memset (&new_ctx, 0, sizeof (ctx));
	else
		memcpy (&new_ctx, &tls->ctx, sizeof (MonoContext));

	memset (reg_locations, 0, sizeof (reg_locations));
	memset (new_reg_locations, 0, sizeof (new_reg_locations));

	while (TRUE) {
		memcpy (&ctx, &new_ctx, sizeof (ctx));

		for (i = 0; i < MONO_MAX_IREGS; ++i) {
			if (new_reg_locations [i]) {
				/*
				 * If the current frame saves the register, it means it might modify its
				 * value, thus the old location might not contain the same value, so
				 * we have to mark it conservatively.
				 * FIXME: This happens very often, due to:
				 * - outside the live intervals of the variables allocated to a register,
				 * we have to treat the register as PIN, since we don't know whenever it
				 * has the same value as in the caller, or a new dead value.
				 */
				if (!precise && reg_locations [i]) {
					DEBUG (printf ("\tscan saved reg %s location %p.\n", mono_arch_regname (i), reg_locations [i]));
					mono_gc_conservatively_scan_area (reg_locations [i], reg_locations [i] + sizeof (mgreg_t));
					scanned_registers += sizeof (mgreg_t);
				}

				reg_locations [i] = new_reg_locations [i];

				if (!precise) {
					DEBUG (printf ("\treg %s is at location %p.\n", mono_arch_regname (i), reg_locations [i]));
				}
			}
		}

		g_assert ((guint64)stack_limit % sizeof (mgreg_t) == 0);

#ifdef MONO_ARCH_HAVE_FIND_JIT_INFO_EXT
		res = mono_find_jit_info_ext (frame.domain ? frame.domain : mono_domain_get (), tls->jit_tls, NULL, &ctx, &new_ctx, NULL, &lmf, new_reg_locations, &frame);
		if (!res)
			break;
#else
		break;
#endif

		/* The last frame can be in any state so mark conservatively */
		if (last) {
			last = FALSE;
			continue;
		}

		/* These frames are returned by mono_find_jit_info () two times */
		if (!frame.managed)
			continue;

		/* All the other frames are at a call site */

		/* Scan the frame of this method */

		/*
		 * A frame contains the following:
		 * - saved registers
		 * - saved args
		 * - locals
		 * - spill area
		 * - localloc-ed memory
		 */

		ji = frame.ji;
		map = ji->gc_info;

		if (!map) {
			DEBUG (char *fname = mono_method_full_name (ji->method, TRUE); printf ("Mark(%d): No GC map for %s\n", precise, fname); g_free (fname));

			continue;
		}

		/*
		 * Debugging aid to control the number of frames scanned precisely
		 */
		if (!precise_frame_limit_inited) {
			if (getenv ("MONO_PRECISE_COUNT"))
				precise_frame_limit = atoi (getenv ("MONO_PRECISE_COUNT"));
			precise_frame_limit_inited = TRUE;
		}
				
		if (precise_frame_limit != -1) {
			if (precise_frame_count [precise] == precise_frame_limit)
				printf ("LAST PRECISE FRAME: %s\n", mono_method_full_name (ji->method, TRUE));
			if (precise_frame_count [precise] > precise_frame_limit)
				continue;
		}
		precise_frame_count [precise] ++;

#ifdef __x86_64__
		if (map->frame_reg == AMD64_RSP)
			fp = (guint8*)ctx.rsp;
		else if (map->frame_reg == AMD64_RBP)
			fp = (guint8*)ctx.rbp;
		else
			g_assert_not_reached ();
#else
		fp = NULL;
		g_assert_not_reached ();
#endif

		frame_start = fp + map->start_offset + map->map_offset;
		frame_end = fp + map->end_offset;

		pc_offset = (guint8*)MONO_CONTEXT_GET_IP (&ctx) - (guint8*)ji->code_start;
		g_assert (pc_offset >= 0);

		DEBUG (char *fname = mono_method_full_name (ji->method, TRUE); printf ("Mark(%d): %s+0x%x (%p) limit=%p fp=%p frame=%p-%p (%d)\n", precise, fname, pc_offset, (gpointer)MONO_CONTEXT_GET_IP (&ctx), stack_limit, fp, frame_start, frame_end, (int)(frame_end - frame_start)); g_free (fname));

		/* Find the callsite index */
		// FIXME: Use a binary search
		for (i = 0; i < map->ncallsites; ++i)
			/* ip points inside the call instruction */
			if (map->callsite_offsets [i] == pc_offset + 1)
				break;
		if (i == map->ncallsites) {
			printf ("Unable to find ip offset 0x%x in callsite list of %s.\n", pc_offset + 1, mono_method_full_name (ji->method, TRUE));
			g_assert_not_reached ();
		}
		cindex = i;

		/* 
		 * FIXME: Add a function to mark using a bitmap, to avoid doing a 
		 * call for each object.
		 */

		/* Pinning needs to be done first, then the precise scan later */

		if (!precise) {
			g_assert (frame_start >= stack_limit);

			if (frame_start > stack_limit) {
				/* This scans the previously skipped frames as well */
				DEBUG (printf ("\tscan area %p-%p.\n", stack_limit, frame_start));
				mono_gc_conservatively_scan_area (stack_limit, frame_start);
			}

			/* Mark stack slots */
			if (map->has_pin_slots) {
				guint8 *pin_bitmap = &map->pin_bitmap [(map->bitmap_width * cindex)];
				guint8 *p;
				gboolean pinned;

				p = frame_start;
				for (i = 0; i < map->nslots; ++i) {
					pinned = pin_bitmap [i / 8] & (1 << (i % 8));
					if (pinned) {
						DEBUG (printf ("\tscan slot %s0x%x(fp)=%p.\n", (guint8*)p > (guint8*)fp ? "" : "-", ABS ((int)((gssize)p - (gssize)fp)), p));
						mono_gc_conservatively_scan_area (p, p + sizeof (mgreg_t));
						scanned_conservatively += sizeof (mgreg_t);
					} else {
						scanned_precisely += sizeof (mgreg_t);
					}
					p += sizeof (mgreg_t);
				}
			} else {
				scanned_precisely += (map->nslots * sizeof (mgreg_t));
			}

			/* The area outside of start-end is NOREF */
			scanned_precisely += (map->end_offset - map->start_offset) - (map->nslots * sizeof (mgreg_t));

			/* Mark registers */
			if (map->has_pin_regs) {
				guint8 *pin_bitmap = &map->reg_pin_bitmap [(map->reg_bitmap_width * cindex)];
				for (i = 0; i < map->nregs; ++i) {
					if (!(map->used_regs & (1 << i)))
						continue;

					if (!reg_locations [i])
						continue;

					if (!(pin_bitmap [i / 8] & (1 << (i % 8)))) {
						/*
						 * The method uses this register, and we have precise info for it.
						 * This means the location will be scanned precisely.
						 * Tell the code at the beginning of the loop that this location is
						 * processed.
						 */
						DEBUG (printf ("\treg %s at location %p is precise.\n", mono_arch_regname (i), reg_locations [i]));
						reg_locations [i] = NULL;
					} else {
						DEBUG (printf ("\treg %s at location %p is pinning.\n", mono_arch_regname (i), reg_locations [i]));
					}
				}
			}

			scanned += map->end_offset - map->start_offset;

			g_assert (scanned == scanned_precisely + scanned_conservatively);

			stack_limit = frame_end;
		} else {
			/* Mark stack slots */
			if (map->has_ref_slots) {
				guint8 *ref_bitmap = &map->ref_bitmap [(map->bitmap_width * cindex)];
				gboolean live;

				for (i = 0; i < map->nslots; ++i) {
					MonoObject **ptr = (MonoObject**)(frame_start + (i * sizeof (mgreg_t)));

					live = ref_bitmap [i / 8] & (1 << (i % 8));

					if (live) {
						MonoObject *obj = *ptr;
						if (obj) {
							DEBUG (printf ("\tref %s0x%x(fp)=%p: %p ->", (guint8*)ptr >= (guint8*)fp ? "" : "-", ABS ((int)((gssize)ptr - (gssize)fp)), ptr, obj));
							*ptr = mono_gc_scan_object (obj);
							DEBUG (printf (" %p.\n", *ptr));
						} else {
							DEBUG (printf ("\tref %s0x%x(fp)=%p: %p.\n", (guint8*)ptr >= (guint8*)fp ? "" : "-", ABS ((int)((gssize)ptr - (gssize)fp)), ptr, obj));
						}
					} else {
						if (map->ref_slots [i / 8] & (1 << (i % 8))) {
							DEBUG (printf ("\tref %s0x%x(fp)=%p: dead (%p)\n", (guint8*)ptr >= (guint8*)fp ? "" : "-", ABS ((int)((gssize)ptr - (gssize)fp)), ptr, *ptr));
							/*
							 * Fail fast if the live range is incorrect, and
							 * the JITted code tries to access this object
							 */
							*ptr = DEAD_REF;
						}
					}
				}
			}

			/* Mark registers */

			/*
			 * Registers are different from stack slots, they have no address where they
			 * are stored. Instead, some frame below this frame in the stack saves them
			 * in its prolog to the stack. We can mark this location precisely.
			 */
			if (map->has_ref_regs) {
				guint8 *ref_bitmap = &map->reg_ref_bitmap [(map->reg_bitmap_width * cindex)];
				for (i = 0; i < map->nregs; ++i) {
					if (!(map->used_regs & (1 << i)))
						continue;

					if (!reg_locations [i])
						continue;

					if (ref_bitmap [i / 8] & (1 << (i % 8))) {
						/*
						 * reg_locations [i] contains the address of the stack slot where
						 * i was last saved, so mark that slot.
						 */
						MonoObject **ptr = (MonoObject**)reg_locations [i];
						MonoObject *obj = *ptr;

						if (obj) {
							DEBUG (printf ("\treg %s saved at %p: %p ->", mono_arch_regname (i), reg_locations [i], obj));
							*ptr = mono_gc_scan_object (obj);
							DEBUG (printf (" %p.\n", *ptr));
						} else {
							DEBUG (printf ("\treg %s saved at %p: %p", mono_arch_regname (i), reg_locations [i], obj));
						}
					}
				}
			}	
		}
	}

	if (!precise) {
		/* Scan the remaining register save locations */
		for (i = 0; i < MONO_MAX_IREGS; ++i) {
			if (reg_locations [i]) {
				DEBUG (printf ("\tscan saved reg location %p.\n", reg_locations [i]));
				mono_gc_conservatively_scan_area (reg_locations [i], reg_locations [i] + sizeof (mgreg_t));
				scanned_registers += sizeof (mgreg_t);
			}
			// FIXME: Is this needed ?
			if (new_reg_locations [i]) {
				DEBUG (printf ("\tscan saved reg location %p.\n", new_reg_locations [i]));
				mono_gc_conservatively_scan_area (new_reg_locations [i], new_reg_locations [i] + sizeof (mgreg_t));
				scanned_registers += sizeof (mgreg_t);
			}
		}
	}

	if (stack_limit < stack_end && !precise) {
		DEBUG (printf ("\tscan area %p-%p.\n", stack_limit, stack_end));
		mono_gc_conservatively_scan_area (stack_limit, stack_end);
	}

	DEBUG (printf ("Marked %d bytes, p=%d,c=%d out of %d.\n", scanned, scanned_precisely, scanned_conservatively, (int)(stack_end - stack_start)));

	if (!precise) {
		stats.scanned_stacks += stack_end - stack_start;
		stats.scanned += scanned;
		stats.scanned_precisely += scanned_precisely;
		stats.scanned_conservatively += scanned_conservatively;
		stats.scanned_registers += scanned_registers;
	}

	//mono_gc_conservatively_scan_area (stack_start, stack_end);
}

static void
mini_gc_init_gc_map (MonoCompile *cfg)
{
	if (COMPILE_LLVM (cfg))
		return;

#if 1
	/* Debugging support */
	{
		static int precise_count;

		precise_count ++;
		if (getenv ("MONO_GCMAP_COUNT")) {
			if (precise_count == atoi (getenv ("MONO_GCMAP_COUNT")))
				printf ("LAST: %s\n", mono_method_full_name (cfg->method, TRUE));
			if (precise_count > atoi (getenv ("MONO_GCMAP_COUNT")))
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
mini_gc_set_slot_type_from_fp (MonoCompile *cfg, int slot_offset, StackSlotType type)
{
	MonoCompileGC *gcfg = (MonoCompileGC*)cfg->gc_info;

	if (!cfg->compute_gc_maps)
		return;

	g_assert (slot_offset % sizeof (mgreg_t) == 0);

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
mini_gc_set_slot_type_from_cfa (MonoCompile *cfg, int slot_offset, StackSlotType type)
{
	MonoCompileGC *gcfg = (MonoCompileGC*)cfg->gc_info;
	int slot = - (slot_offset / sizeof (mgreg_t));

	if (!cfg->compute_gc_maps)
		return;

	g_assert (slot_offset <= 0);
	g_assert (slot_offset % sizeof (mgreg_t) == 0);

	gcfg->stack_slots_from_cfa = g_slist_prepend_mempool (cfg->mempool, gcfg->stack_slots_from_cfa, GUINT_TO_POINTER (((slot) << 16) | type));
}

static inline int
fp_offset_to_slot (MonoCompile *cfg, int offset)
{
	MonoCompileGC *gcfg = cfg->gc_info;

	return (offset - gcfg->min_offset) / sizeof (mgreg_t);
}

static inline void
set_slot (MonoCompileGC *gcfg, int slot, int callsite_index, StackSlotType type)
{
	g_assert (slot >= 0 && slot < gcfg->nslots);

	if (type == SLOT_PIN) {
		clear_bit (gcfg->ref_bitmap, gcfg->bitmap_width, callsite_index, slot);
		set_bit (gcfg->pin_bitmap, gcfg->bitmap_width, callsite_index, slot);
	} else if (type == SLOT_REF) {
		set_bit (gcfg->ref_bitmap, gcfg->bitmap_width, callsite_index, slot);
		clear_bit (gcfg->pin_bitmap, gcfg->bitmap_width, callsite_index, slot);
	} else if (type == SLOT_NOREF) {
		clear_bit (gcfg->ref_bitmap, gcfg->bitmap_width, callsite_index, slot);
		clear_bit (gcfg->pin_bitmap, gcfg->bitmap_width, callsite_index, slot);
	}
}

static inline void
set_slot_everywhere (MonoCompileGC *gcfg, int slot, StackSlotType type)
{
	int cindex;

	for (cindex = 0; cindex < gcfg->ncallsites; ++cindex)
		set_slot (gcfg, slot, cindex, type);
}

static inline void
set_slot_in_range (MonoCompileGC *gcfg, int slot, int from, int to, StackSlotType type)
{
	int cindex;

	for (cindex = 0; cindex < gcfg->ncallsites; ++cindex) {
		int callsite_offset = gcfg->callsites [cindex]->pc_offset;
		if (callsite_offset >= from && callsite_offset < to)
			set_slot (gcfg, slot, cindex, type);
	}
}

static inline void
set_reg_slot (MonoCompileGC *gcfg, int slot, int callsite_index, StackSlotType type)
{
	g_assert (slot >= 0 && slot < gcfg->nregs);

	if (type == SLOT_PIN) {
		clear_bit (gcfg->reg_ref_bitmap, gcfg->reg_bitmap_width, callsite_index, slot);
		set_bit (gcfg->reg_pin_bitmap, gcfg->reg_bitmap_width, callsite_index, slot);
	} else if (type == SLOT_REF) {
		set_bit (gcfg->reg_ref_bitmap, gcfg->reg_bitmap_width, callsite_index, slot);
		clear_bit (gcfg->reg_pin_bitmap, gcfg->reg_bitmap_width, callsite_index, slot);
	} else if (type == SLOT_NOREF) {
		clear_bit (gcfg->reg_ref_bitmap, gcfg->reg_bitmap_width, callsite_index, slot);
		clear_bit (gcfg->reg_pin_bitmap, gcfg->reg_bitmap_width, callsite_index, slot);
	}
}

static inline void
set_reg_slot_everywhere (MonoCompileGC *gcfg, int slot, StackSlotType type)
{
	int cindex;

	for (cindex = 0; cindex < gcfg->ncallsites; ++cindex)
		set_reg_slot (gcfg, slot, cindex, type);
}

static inline void
set_reg_slot_in_range (MonoCompileGC *gcfg, int slot, int from, int to, StackSlotType type)
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
			StackSlotType type;

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
				printf ("\t%s spill slot at fp+0x%x (slot = %d)\n", slot_type_to_string (type), offset, slot);
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
			printf ("\tfp spill slot at fp+0x%x (slot = %d)\n", offset, slot);
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
			printf ("\tint spill slot at fp+0x%x (slot = %d)\n", offset, slot);
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
		StackSlotType type = data & 0xff;
		int slot;
		
		/*
		 * Map the cfa relative slot to an fp relative slot.
		 * slot_addr == cfa - <cfa_slot>*4/8
		 * fp + cfa_offset == cfa
		 * -> slot_addr == fp + (cfa_offset - <cfa_slot>*4/8)
		 */
		slot = (cfg->cfa_offset / sizeof (mgreg_t)) - cfa_slot - (gcfg->min_offset / sizeof (mgreg_t));

		set_slot_everywhere (gcfg, slot, type);

		if (cfg->verbose_level > 1) {
			if (type == SLOT_NOREF)
				printf ("\tnoref slot at fp+0x%x (slot = %d) (cfa - 0x%x)\n", (int)(slot * sizeof (mgreg_t)), slot, (int)(cfa_slot * sizeof (mgreg_t)));
		}
	}

	/* Relative to the FP */
	for (l = gcfg->stack_slots_from_fp; l; l = l->next) {
		gint data = GPOINTER_TO_INT (l->data);
		int offset = data >> 16;
		StackSlotType type = data & 0xff;
		int slot;
		
		slot = fp_offset_to_slot (cfg, offset);

		set_slot_everywhere (gcfg, slot, type);

		/* Liveness for these slots is handled by process_spill_slots () */

		if (cfg->verbose_level > 1) {
			if (type == SLOT_REF)
				printf ("\tref slot at fp+0x%x (slot = %d)\n", offset, slot);
		}
	}
}

static void
process_variables (MonoCompile *cfg)
{
	MonoCompileGC *gcfg = cfg->gc_info;
	MonoMethodSignature *sig = mono_method_signature (cfg->method);
	int i, locals_min_slot, locals_max_slot, cindex;
	MonoBasicBlock *bb;
	MonoInst *tmp;
	int *pc_offsets;
	int locals_min_offset = gcfg->locals_min_offset;
	int locals_max_offset = gcfg->locals_max_offset;

	/* Slots for locals are NOREF by default */
	locals_min_slot = (locals_min_offset - gcfg->min_offset) / sizeof (mgreg_t);
	locals_max_slot = (locals_max_offset - gcfg->min_offset) / sizeof (mgreg_t);
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
		gboolean byref;
		gboolean is_arg = i < cfg->locals_start;
		
		vmv = MONO_VARINFO (cfg, i);

		/* For some reason, 'this' is byref */
		if (sig->hasthis && ins == cfg->args [0] && !cfg->method->klass->valuetype)
			t = &cfg->method->klass->byval_arg;

		byref = t->byref;

		if (ins->opcode == OP_REGVAR) {
			int hreg;
			StackSlotType slot_type;

			t = mini_type_get_underlying_type (NULL, t);

			hreg = ins->dreg;
			g_assert (hreg < MONO_MAX_IREGS);

			// FIXME: Add back this check
#if 0
			if (is_arg && gcfg->reg_live_intervals [hreg]) {
				/* 
				 * FIXME: This argument shares a hreg with a local, we can't add the whole
				 * method as a live interval, since it would overlap with the locals
				 * live interval.
				 */
				continue;
			}
#endif

			if (byref)
				slot_type = SLOT_PIN;
			else
				slot_type = MONO_TYPE_IS_REFERENCE (t) ? SLOT_REF : SLOT_NOREF;

			if (is_arg) {
				/* Live for the whole method */
				set_reg_slot_everywhere (gcfg, hreg, slot_type);
			} else {
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
			}

			if (cfg->verbose_level > 1) {
				printf ("\t%s %sreg %s(R%d)\n", slot_type_to_string (slot_type), is_arg ? "arg " : "", mono_arch_regname (hreg), vmv->vreg);
				printf ("\n");
			}

			continue;
		}

		if (ins->opcode != OP_REGOFFSET)
			continue;

		if (ins->inst_offset % sizeof (mgreg_t) != 0)
			continue;

		if (is_arg && ins->inst_offset >= gcfg->max_offset)
			/* In parent frame */
			continue;

		pos = fp_offset_to_slot (cfg, ins->inst_offset);

		if (is_arg && ins->flags & MONO_INST_IS_DEAD) {
			/* These do not get stored in the prolog */
			set_slot_everywhere (gcfg, pos, SLOT_NOREF);

			if (cfg->verbose_level > 1) {
				printf ("\tdead arg at fp%s0x%x (slot=%d): %s\n", ins->inst_offset < 0 ? "-" : "+", (ins->inst_offset < 0) ? -(int)ins->inst_offset : (int)ins->inst_offset, pos, mono_type_full_name (ins->inst_vtype));
			}
			continue;
		}

		if (MONO_TYPE_ISSTRUCT (t)) {
			int numbits = 0, j;
			gsize *bitmap = NULL;
			gboolean pin = FALSE;
			MonoLiveInterval *interval = NULL;
			int size;

			if (ins->backend.is_pinvoke)
				size = mono_class_native_size (ins->klass, NULL);
			else
				size = mono_class_value_size (ins->klass, NULL);

			if (!ins->klass->has_references) {
				if (is_arg) {
					for (j = 0; j < size / sizeof (mgreg_t); ++j)
						set_slot_everywhere (gcfg, pos + j, SLOT_NOREF);
				}
				continue;
			}

			if (ins->klass->generic_container || mono_class_is_open_constructed_type (t)) {
				/* FIXME: Generic sharing */
				pin = TRUE;
			} else {
				mono_class_compute_gc_descriptor (ins->klass);

				bitmap = mono_gc_get_bitmap_for_descr (ins->klass->gc_descr, &numbits);

				if (!bitmap)
					// FIXME:
					pin = TRUE;

				/*
				 * Most vtypes are marked volatile because of the LDADDR instructions,
				 * and they have no liveness information since they are decomposed
				 * before the liveness pass. We emit OP_GC_LIVENESS_DEF instructions for
				 * them during VZERO decomposition.
				 */
				if (pc_offsets [vmv->vreg]) {
					interval = mono_mempool_alloc0 (cfg->mempool, sizeof (MonoLiveInterval));
					mono_linterval_add_range (cfg, interval, pc_offsets [vmv->vreg], cfg->code_size);
				} else {
					pin = TRUE;
				}
			}

			if (ins->backend.is_pinvoke)
				pin = TRUE;

			if (bitmap) {
				if (pc_offsets [vmv->vreg]) {
					for (cindex = 0; cindex < gcfg->ncallsites; ++cindex) {
						if (gcfg->callsites [cindex]->pc_offset > pc_offsets [vmv->vreg]) {
							for (j = 0; j < numbits; ++j) {
								if (bitmap [j / GC_BITS_PER_WORD] & ((gsize)1 << (j % GC_BITS_PER_WORD))) {
									/* The descriptor is for the boxed object */
									set_slot (gcfg, (pos + j - (sizeof (MonoObject) / sizeof (gpointer))), cindex, pin ? SLOT_PIN : SLOT_REF);
								}
							}
						}
					}
				}
			} else if (pin) {
				for (j = 0; j < size / sizeof (mgreg_t); ++j) {
					set_slot_everywhere (gcfg, pos + j, SLOT_PIN);
				}
			}

			g_free (bitmap);

			if (cfg->verbose_level > 1) {
				printf ("\tvtype R%d at fp+0x%x-0x%x: %s ", vmv->vreg, (int)ins->inst_offset, (int)(ins->inst_offset + (size / sizeof (mgreg_t))), mono_type_full_name (ins->inst_vtype));
				if (interval)
					mono_linterval_print (interval);
				else
					printf ("(pinned)");
				printf ("\n");
			}

			continue;
		}

		if (!is_arg && (ins->inst_offset < gcfg->min_offset || ins->inst_offset >= gcfg->max_offset))
			/* Vret addr etc. */
			continue;

		if (t->byref) {
			// FIXME: Not everywhere
			set_slot_everywhere (gcfg, pos, SLOT_PIN);
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

		t = mini_type_get_underlying_type (NULL, t);

		if (!MONO_TYPE_IS_REFERENCE (t)) {
			set_slot_everywhere (gcfg, pos, SLOT_NOREF);
			continue;
		}

		if (ins->flags & (MONO_INST_VOLATILE | MONO_INST_INDIRECT)) {
			/*
			 * For volatile variables, treat them alive from the point they are
			 * initialized in the first bblock until the end of the method.
			 */
			if (pc_offsets [vmv->vreg]) {
				set_slot_in_range (gcfg, pos, pc_offsets [vmv->vreg], cfg->code_size, SLOT_PIN);
			} else {
				set_slot_everywhere (gcfg, pos, SLOT_PIN);
				continue;
			}
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
			printf ("\tref at %s0x%x(fp) (slot=%d): %s\n", ins->inst_offset < 0 ? "-" : "", (ins->inst_offset < 0) ? -(int)ins->inst_offset : (int)ins->inst_offset, pos, mono_type_full_name (ins->inst_vtype));
			printf ("\n");
		}
	}

	g_free (pc_offsets);
}

static void
compute_frame_size (MonoCompile *cfg)
{
	int i, locals_min_offset, locals_max_offset, cfa_min_offset, cfa_max_offset;
	int min_offset, max_offset;
	MonoCompileGC *gcfg = cfg->gc_info;
	MonoMethodSignature *sig = mono_method_signature (cfg->method);

	/* Compute min/max offsets from the fp */

	/* Locals */
#ifdef TARGET_AMD64
	locals_min_offset = ALIGN_TO (cfg->locals_min_stack_offset, sizeof (mgreg_t));
	locals_max_offset = cfg->locals_max_stack_offset;
#else
	/* min/max stack offset needs to be computed in mono_arch_allocate_vars () */
	NOT_IMPLEMENTED;
#endif

	locals_min_offset = ALIGN_TO (locals_min_offset, sizeof (mgreg_t));
	locals_max_offset = ALIGN_TO (locals_max_offset, sizeof (mgreg_t));

	min_offset = locals_min_offset;
	max_offset = locals_max_offset;

	/* Arguments */
	for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
		MonoInst *ins = cfg->args [i];

		if (ins->opcode == OP_REGOFFSET)
			min_offset = MIN (min_offset, ins->inst_offset);
	}

	/* Cfa slots */
	g_assert (cfg->frame_reg == cfg->cfa_reg);
	g_assert (cfg->cfa_offset > 0);
	cfa_min_offset = 0;
	cfa_max_offset = cfg->cfa_offset;

	min_offset = MIN (min_offset, cfa_min_offset);
	max_offset = MAX (max_offset, cfa_max_offset);

	/* Spill slots */
	if (!(cfg->flags & MONO_CFG_HAS_SPILLUP)) {
		int stack_offset = ALIGN_TO (cfg->stack_offset, sizeof (mgreg_t));
		min_offset = MIN (min_offset, (-stack_offset));
	}

	gcfg->min_offset = min_offset;
	gcfg->max_offset = max_offset;
	gcfg->locals_min_offset = locals_min_offset;
	gcfg->locals_max_offset = locals_max_offset;
}

void
mini_gc_create_gc_map (MonoCompile *cfg)
{
	GCMap *map;
	int i, j, nregs, nslots, alloc_size;
	int ntypes [16];
	int bitmap_width, bitmap_size, reg_bitmap_width, reg_bitmap_size;
	int start, end;
	gboolean has_ref_slots, has_pin_slots, has_ref_regs, has_pin_regs;
	MonoCompileGC *gcfg = cfg->gc_info;
	GCCallSite **callsites;
	int ncallsites;
	MonoBasicBlock *bb;
	GSList *l;

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

	if (cfg->header->num_clauses)
		/*
		 * The calls to the finally clauses don't show up in the cfg. See
		 * test_0_liveness_8 ().
		 */
		return;

	mono_analyze_liveness_gc (cfg);

	compute_frame_size (cfg);

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

	nslots = (gcfg->max_offset - gcfg->min_offset) / sizeof (mgreg_t);
	nregs = MONO_MAX_IREGS;

	gcfg->nslots = nslots;
	gcfg->nregs = nregs;
	gcfg->callsites = callsites;
	gcfg->ncallsites = ncallsites;
	gcfg->bitmap_width = ALIGN_TO (nslots, 8) / 8;
	gcfg->reg_bitmap_width = ALIGN_TO (nregs, 8) / 8;
	gcfg->ref_bitmap = mono_mempool_alloc0 (cfg->mempool, gcfg->bitmap_width * ncallsites);
	gcfg->pin_bitmap = mono_mempool_alloc0 (cfg->mempool, gcfg->bitmap_width * ncallsites);
	gcfg->reg_ref_bitmap = mono_mempool_alloc0 (cfg->mempool, gcfg->reg_bitmap_width * ncallsites);
	gcfg->reg_pin_bitmap = mono_mempool_alloc0 (cfg->mempool, gcfg->reg_bitmap_width * ncallsites);

	/* All slots start out as PIN */
	memset (gcfg->pin_bitmap, 0xff, gcfg->bitmap_width * ncallsites);
	for (i = 0; i < nregs; ++i) {
		/*
		 * By default, registers are PIN.
		 * This is because we don't know their type outside their live range, since
		 * they could have the same value as in the caller, or a value set by the
		 * current method etc.
		 */
		if ((cfg->used_int_regs & (1 << i)))
			set_reg_slot_everywhere (gcfg, i, SLOT_PIN);
	}

	process_spill_slots (cfg);
	process_other_slots (cfg);
	process_variables (cfg);

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

		for (j = 0; j < ncallsites; ++j) {
			if (get_bit (gcfg->pin_bitmap, gcfg->bitmap_width, j, i))
				has_pin = TRUE;
			if (get_bit (gcfg->ref_bitmap, gcfg->bitmap_width, j, i))
				has_ref = TRUE;
		}

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
	has_pin_regs = TRUE;
	for (i = 0; i < nregs; ++i) {
		gboolean has_ref = FALSE;

		for (j = 0; j < ncallsites; ++j) {
			if (get_bit (gcfg->reg_ref_bitmap, gcfg->reg_bitmap_width, j, i))
				has_ref = TRUE;
		}

		if (has_ref)
			has_ref_regs = TRUE;
	}

	if (cfg->verbose_level > 1)
		printf ("Slots: %d Start: %d End: %d Refs: %d NoRefs: %d Pin: %d Callsites: %d\n", nslots, start, end, ntypes [SLOT_REF], ntypes [SLOT_NOREF], ntypes [SLOT_PIN], ncallsites);

	/* Create the GC Map */

	/* This is the final bitmap width */
	bitmap_width = ALIGN_TO (end - start, 8) / 8;
	bitmap_size = bitmap_width * ncallsites;
	reg_bitmap_width = ALIGN_TO (nregs, 8) / 8;
	reg_bitmap_size = reg_bitmap_width * ncallsites;
	alloc_size = sizeof (GCMap);
	map = mono_domain_alloc0 (cfg->domain, alloc_size);
	gc_maps_size += alloc_size;

	map->frame_reg = cfg->frame_reg;
	map->start_offset = gcfg->min_offset;
	map->end_offset = gcfg->min_offset + (nslots * sizeof (mgreg_t));
	map->map_offset = start * sizeof (mgreg_t);
	map->nslots = end - start;
	map->nregs = nregs;
	map->bitmap_width = bitmap_width;
	map->reg_bitmap_width = reg_bitmap_width;
	map->has_ref_slots = has_ref_slots;
	map->has_pin_slots = has_pin_slots;
	map->has_ref_regs = has_ref_regs;
	map->has_pin_regs = has_pin_regs;
	map->ref_slots = mono_domain_alloc0 (cfg->domain, bitmap_width);
	gc_maps_size += bitmap_width;

	if (has_ref_slots) {
		map->ref_bitmap = mono_domain_alloc0 (cfg->domain, bitmap_size);
		gc_maps_size += bitmap_size;

		for (i = 0; i < nslots; ++i) {
			for (j = 0; j < ncallsites; ++j) {
				if (get_bit (gcfg->ref_bitmap, gcfg->bitmap_width, j, i))
					set_bit (map->ref_bitmap, map->bitmap_width, j, i - start);
			}
		}
	}
	if (has_pin_slots) {
		map->pin_bitmap = mono_domain_alloc0 (cfg->domain, bitmap_size);
		gc_maps_size += bitmap_size;

		for (i = 0; i < nslots; ++i) {
			for (j = 0; j < ncallsites; ++j) {
				if (get_bit (gcfg->pin_bitmap, gcfg->bitmap_width, j, i))
					set_bit (map->pin_bitmap, map->bitmap_width, j, i - start);
			}
		}
	}
	if (has_ref_regs) {
		map->reg_ref_bitmap = mono_domain_alloc0 (cfg->domain, reg_bitmap_size);
		gc_maps_size += reg_bitmap_size;
		memcpy (map->reg_ref_bitmap, gcfg->reg_ref_bitmap, reg_bitmap_size);
	}
	if (has_pin_regs) {
		map->reg_pin_bitmap = mono_domain_alloc0 (cfg->domain, reg_bitmap_size);
		gc_maps_size += reg_bitmap_size;
		memcpy (map->reg_pin_bitmap, gcfg->reg_pin_bitmap, reg_bitmap_size);
	}

	map->ncallsites = ncallsites;
	map->callsite_offsets = mono_domain_alloc0 (cfg->domain, ncallsites *sizeof (int));
	g_assert (nregs < 64);
	map->used_regs = cfg->used_int_regs;
	gc_maps_size += ncallsites * sizeof (int);

	for (i = 0; i < ncallsites; ++i)
		map->callsite_offsets [i] = callsites [i]->pc_offset;

	stats.all_slots += nslots;
	stats.ref_slots += ntypes [SLOT_REF];
	stats.noref_slots += ntypes [SLOT_NOREF];
	stats.pin_slots += ntypes [SLOT_PIN];

	cfg->jit_info->gc_info = map;
}

void
mini_gc_init (void)
{
	MonoGCCallbacks cb;

	memset (&cb, 0, sizeof (cb));
	cb.thread_attach_func = thread_attach_func;
	cb.thread_suspend_func = thread_suspend_func;
	/* Comment this out to disable precise stack marking */
	cb.thread_mark_func = thread_mark_func;
	mono_gc_set_gc_callbacks (&cb);

	mono_counters_register ("GC Maps size",
							MONO_COUNTER_GC | MONO_COUNTER_INT, &gc_maps_size);

	mono_counters_register ("GC Map slots (all)",
							MONO_COUNTER_GC | MONO_COUNTER_INT, &stats.all_slots);
	mono_counters_register ("GC Map slots (ref)",
							MONO_COUNTER_GC | MONO_COUNTER_INT, &stats.ref_slots);
	mono_counters_register ("GC Map slots (noref)",
							MONO_COUNTER_GC | MONO_COUNTER_INT, &stats.noref_slots);
	mono_counters_register ("GC Map slots (pin)",
							MONO_COUNTER_GC | MONO_COUNTER_INT, &stats.pin_slots);

	mono_counters_register ("Stack space scanned (all)",
							MONO_COUNTER_GC | MONO_COUNTER_INT, &stats.scanned_stacks);
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
mini_gc_init (void)
{
}

static void
mini_gc_init_gc_map (MonoCompile *cfg)
{
}

void
mini_gc_create_gc_map (MonoCompile *cfg)
{
}

void
mini_gc_set_slot_type_from_fp (MonoCompile *cfg, int slot_offset, StackSlotType type)
{
}

void
mini_gc_set_slot_type_from_cfa (MonoCompile *cfg, int slot_offset, StackSlotType type)
{
}

#endif

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

/*
 * Problems with the current code:
 * - it makes two passes over the stack
 * - the stack walk is slow
 * - vtypes/refs used in EH regions are treated conservatively
 * - the computation of the GC maps is slow since it involves a liveness analysis pass
 * - the GC maps are uncompressed and take up a lot of memory.
 * - if the code is finished, less pinning will be done, causing problems because
 *   we promote all surviving objects to old-gen.
 */
