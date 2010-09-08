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

/*
 * The code below does not work yet, and probably needs to be thrown out if we move
 * to GC safe points.
 */

//#if 0
#ifdef HAVE_SGEN_GC

#include <mono/metadata/gc-internal.h>
#include <mono/utils/mono-counters.h>

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

typedef enum {
	/* Stack slot doesn't contain a reference */
	SLOT_NOREF = 0,
	/* Stack slot contains a reference */
	SLOT_REF = 1,
	/* No info, slot needs to be scanned conservatively */
	SLOT_PIN = 2
} StackSlotType;

/* 
 * Contains information needed to mark a stack frame.
 * FIXME: Optimize the memory usage.
 */
typedef struct {
	/* The frame pointer register */
	int frame_reg;
	/* The offset of the local variable area in the stack frame relative to the frame pointer */
	int locals_offset;
	/* The size of the locals area. Can't use nslots as it includes padding */
	int locals_size;
	/* The number of stack slots */
	int nslots;
	/* The width of the liveness bitmap in bytes */
	int bitmap_width;
	/* 
	 * The gc map itself.
	 */
	StackSlotType *slots;
	/* 
	 * A bitmap whose width is equal to the number of SLOT_REF values in gc_refs, and whose
	 * height is equal to the number of possible PC offsets.
	 * This needs to be compressed later.
	 */
	guint8 bitmap [MONO_ZERO_LEN_ARRAY];
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

static int scanned_stacks_stat;
static int scanned_stat, scanned_precisely_stat, scanned_conservatively_stat;

#define DEAD_REF ((gpointer)(gssize)0x2a2a2a2a2a2a2a2aULL)

static void
thread_mark_func (gpointer user_data, guint8 *stack_start, guint8 *stack_end, gboolean precise)
{
	TlsData *tls = user_data;
	MonoJitInfo *ji, res;
	MonoContext ctx, new_ctx;
	MonoLMF *lmf;
	guint8 *stack_limit;
	gboolean last = TRUE, managed;
	GCMap *map;
	guint8* fp, *locals_start, *locals_end;
	int i, pc_offset;
	int scanned = 0, scanned_precisely, scanned_conservatively;

	/* tls == NULL can happen during startup */
	if (mono_thread_internal_current () == NULL || !tls) {
		if (!precise) {
			mono_gc_conservatively_scan_area (stack_start, stack_end);
			scanned_stacks_stat += stack_end - stack_start;
		}
		return;
	}

	lmf = tls->lmf;

	/* Number of bytes scanned based on GC map data */
	scanned = 0;
	/* Number of bytes scanned precisely based on GC map data */
	scanned_precisely = 0;
	/* Number of bytes scanned conservatively based on GC map data */
	scanned_conservatively = 0;

	/* FIXME: sgen-gc.c calls this multiple times for each major collection from pin_from_roots */

	/* FIXME: Use real gc descriptors instead of bitmaps */

	/* This is one past the last address which we have scanned */
	stack_limit = stack_start;

	DEBUG (printf ("*** %s stack marking %p-%p ***\n", precise ? "Precise" : "Conservative", stack_start, stack_end));

	if (!tls->has_context)
		memset (&new_ctx, 0, sizeof (ctx));
	else
		memcpy (&new_ctx, &tls->ctx, sizeof (MonoContext));

	while (TRUE) {
		memcpy (&ctx, &new_ctx, sizeof (ctx));

		g_assert ((guint64)stack_limit % sizeof (gpointer) == 0);

		// FIXME: This doesn't work with appdomain transitions
		ji = mono_find_jit_info (mono_domain_get (), tls->jit_tls, &res, NULL,
								 &ctx, &new_ctx, NULL, &lmf, NULL, &managed);
		if (ji == (gpointer)-1)
			break;

		/* The last frame can be in any state so mark conservatively */
		if (last) {
			last = FALSE;
			continue;
		}

		/* These frames are returned by mono_find_jit_info () two times */
		if (!managed)
			continue;

		/* Scan the frame of this method */

		/*
		 * A frame contains the following:
		 * - saved registers
		 * - saved args
		 * - locals
		 * - spill area
		 * - localloc-ed memory
		 * Currently, only the locals are scanned precisely.
		 */

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

		locals_start = fp + map->locals_offset;
		locals_end = locals_start + map->locals_size;

		pc_offset = (guint8*)MONO_CONTEXT_GET_IP (&ctx) - (guint8*)ji->code_start;
		g_assert (pc_offset >= 0);

		DEBUG (char *fname = mono_method_full_name (ji->method, TRUE); printf ("Mark(%d): %s+0x%x (%p) limit=%p fp=%p locals=%p-%p (%d)\n", precise, fname, pc_offset, (gpointer)MONO_CONTEXT_GET_IP (&ctx), stack_limit, fp, locals_start, locals_end, (int)(locals_end - locals_start)); g_free (fname));

		/* 
		 * FIXME: Add a function to mark using a bitmap, to avoid doing a 
		 * call for each object.
		 */

		scanned += locals_end - locals_start;

		/* Pinning needs to be done first, then the precise scan later */

		if (!precise) {
			g_assert (locals_start >= stack_limit);

			if (locals_start > stack_limit) {
				/* This scans the previously skipped frames as well */
				DEBUG (printf ("\tscan area %p-%p.\n", stack_limit, locals_start));
				mono_gc_conservatively_scan_area (stack_limit, locals_start);
			}

			if (map->slots) {
				guint8 *p;

				p = locals_start;
				for (i = 0; i < map->nslots; ++i) {
					if (map->slots [i] == SLOT_PIN) {
						DEBUG (printf ("\tscan slot %s0x%x(fp)=%p.\n", (guint8*)p > (guint8*)fp ? "" : "-", ABS ((int)((gssize)p - (gssize)fp)), p));
						mono_gc_conservatively_scan_area (p, p + sizeof (gpointer));
						scanned_conservatively += sizeof (gpointer);
					}
					p += sizeof (gpointer);
				}
			}

			stack_limit = locals_end;
		} else {
			if (map->slots) {
				int loffset = 0;
				guint8 *bitmap = &map->bitmap [(map->bitmap_width * pc_offset)];
				gboolean live;

				for (i = 0; i < map->nslots; ++i) {
					if (map->slots [i] == SLOT_REF) {
						MonoObject **ptr = (MonoObject**)(locals_start + (i * sizeof (gpointer)));
						MonoObject *obj = *ptr;

						live = bitmap [loffset / 8] & (1 << (loffset % 8));

						if (live) {
							if (obj) {
								DEBUG (printf ("\tref %s0x%x(fp)=%p: %p ->", (guint8*)ptr >= (guint8*)fp ? "" : "-", ABS ((int)((gssize)ptr - (gssize)fp)), ptr, obj));
								*ptr = mono_gc_scan_object (obj);
								DEBUG (printf (" %p.\n", *ptr));
							} else {
								DEBUG (printf ("\tref %s0x%x(fp)=%p: %p.\n", (guint8*)ptr >= (guint8*)fp ? "" : "-", ABS ((int)((gssize)ptr - (gssize)fp)), ptr, obj));
							}
						} else {
							DEBUG (printf ("\tref %s0x%x(fp)=%p: dead (%p)\n", (guint8*)ptr >= (guint8*)fp ? "" : "-", ABS ((int)((gssize)ptr - (gssize)fp)), ptr, obj));
							/*
							 * Fail fast if the live range is incorrect, and
							 * the JITted code tries to access this object
							 */
							*ptr = DEAD_REF;
						}

						loffset ++;
						scanned_precisely += sizeof (gpointer);
					} else if (map->slots [i] == SLOT_NOREF) {
						scanned_precisely += sizeof (gpointer);
					}
				}
			}
		}
	}

	if (stack_limit < stack_end && !precise) {
		DEBUG (printf ("\tscan area %p-%p.\n", stack_limit, stack_end));
		mono_gc_conservatively_scan_area (stack_limit, stack_end);
	}

	DEBUG (printf ("Marked %d bytes, p=%d,c=%d out of %d.\n", scanned, scanned_precisely, scanned_conservatively, (int)(stack_end - stack_start)));

	scanned_stacks_stat += stack_end - stack_start;
	scanned_stat += scanned;
	scanned_precisely_stat += scanned_precisely;
	scanned_conservatively_stat += scanned - scanned_precisely;

	//mono_gc_conservatively_scan_area (stack_start, stack_end);
}

#define set_slot(slots, nslots, pos, val) do {	\
		g_assert ((pos) < (nslots));		   \
		(slots) [(pos)] = (val);			   \
	} while (0)

static void
mini_gc_init_gc_map (MonoCompile *cfg)
{
	if (COMPILE_LLVM (cfg))
		return;

	cfg->compute_gc_maps = TRUE;
}

void
mini_gc_create_gc_map (MonoCompile *cfg)
{
	GCMap *map;
	int i, nslots, alloc_size, loffset, min_offset, max_offset;
	StackSlotType *slots = NULL;
	gboolean norefs = FALSE;
	GSList **live_intervals;
	int bitmap_width, bitmap_size;

	/*
	 * Since we currently don't use GC safe points, we need to create GC maps which
	 * are precise at every instruction within a method. The live ranges calculated by
	 * the liveness pass are not usable for this, since they contain abstract positions, not
	 * pc offsets. The live ranges calculated by mono_spill_global_vars () are not usable
	 * either, since they can't model holes. Instead of these, we implement our own
	 * liveness analysis which is precise, and works with PC offsets. It calculates live
	 * intervals, which are unions of live ranges.
	 * FIXME:
	 * - arguments (these are not scanned precisely currently).
	 * - it would simplify things if we extended live ranges to the end of basic blocks
	 * instead of computing them precisely.
	 * - maybe mark loads+stores as needing GC tracking, instead of using DEF/USE
	 * instructions ?
	 */

	if (!(cfg->comp_done & MONO_COMP_LIVENESS))
		/* Without liveness info, the live ranges are not precise enough */
		return;

	mono_analyze_liveness_gc (cfg);

#ifdef TARGET_AMD64
	min_offset = ALIGN_TO (cfg->locals_min_stack_offset, sizeof (gpointer));
	max_offset = cfg->locals_max_stack_offset;
#else
	/* min/max stack offset needs to be computed in mono_arch_allocate_vars () */
	NOT_IMPLEMENTED;
#endif

	for (i = cfg->locals_start; i < cfg->num_varinfo; i++) {
		MonoInst *ins = cfg->varinfo [i];
		MonoType *t = ins->inst_vtype;

		if ((MONO_TYPE_ISSTRUCT (t) && ins->klass->has_references))
			break;
		if (MONO_TYPE_ISSTRUCT (t))
			break;
		if (t->byref || t->type == MONO_TYPE_PTR)
			break;
		if (ins && ins->opcode == OP_REGOFFSET && MONO_TYPE_IS_REFERENCE (ins->inst_vtype))
			break;
	}

	if (i == cfg->num_varinfo)
		norefs = TRUE;

	if (cfg->verbose_level > 1)
		printf ("GC Map for %s: 0x%x-0x%x\n", mono_method_full_name (cfg->method, TRUE), min_offset, max_offset);

	nslots = (max_offset - min_offset) / sizeof (gpointer);
	if (!norefs) {
		alloc_size = nslots * sizeof (StackSlotType);
		slots = mono_domain_alloc0 (cfg->domain, alloc_size);
		for (i = 0; i < nslots; ++i)
			slots [i] = SLOT_NOREF;
		gc_maps_size += alloc_size;
	}
	live_intervals = g_new0 (GSList*, nslots);
	loffset = 0;

	for (i = cfg->locals_start; i < cfg->num_varinfo; i++) {
		MonoInst *ins = cfg->varinfo [i];
		MonoType *t = ins->inst_vtype;
		MonoMethodVar *vmv;
		guint32 pos;

		if (norefs)
			continue;

		vmv = MONO_VARINFO (cfg, i);

		if (ins->opcode != OP_REGOFFSET)
			continue;

		if (ins->inst_offset % sizeof (gpointer) != 0)
			continue;

		pos = (ins->inst_offset - min_offset) / sizeof (gpointer);

		if ((MONO_TYPE_ISSTRUCT (t) && !ins->klass->has_references))
			continue;

		if ((MONO_TYPE_ISSTRUCT (t) && ins->klass->has_references)) {
			int numbits, j;
			gsize *bitmap;
			gboolean pin;

			if (ins->klass->generic_container || mono_class_is_open_constructed_type (t)) {
				/* FIXME: Generic sharing */
				pin = TRUE;
			} else {
				mono_class_compute_gc_descriptor (ins->klass);

				bitmap = mono_gc_get_bitmap_for_descr (ins->klass->gc_descr, &numbits);

				if (bitmap) {
					for (j = 0; j < numbits; ++j) {
						if (bitmap [j / GC_BITS_PER_WORD] & ((gsize)1 << (j % GC_BITS_PER_WORD))) {
							/* The descriptor is for the boxed object */
							set_slot (slots, nslots, (pos + j - (sizeof (MonoObject) / sizeof (gpointer))), SLOT_REF);
						}
					}
					g_free (bitmap);

					if (cfg->verbose_level > 1)
						printf ("\tvtype at fp+0x%x: %s -> 0x%x\n", (int)ins->inst_offset, mono_type_full_name (ins->inst_vtype), (int)ins->inst_offset);

					// FIXME: These have no live range
					pin = TRUE;
				} else {
					// FIXME:
					pin = TRUE;
				}
			}

			if (ins->backend.is_pinvoke)
				pin = TRUE;

			if (pin) {
				int size;

				if (ins->backend.is_pinvoke)
					size = mono_class_native_size (ins->klass, NULL);
				else
					size = mono_class_value_size (ins->klass, NULL);
				for (j = 0; j < size / sizeof (gpointer); ++j)
					set_slot (slots, nslots, pos + j, SLOT_PIN);
			}
			continue;
		}

		if (ins->inst_offset < min_offset || ins->inst_offset >= max_offset)
			/* Vret addr etc. */
			continue;

		if (t->byref || t->type == MONO_TYPE_PTR || t->type == MONO_TYPE_I || t->type == MONO_TYPE_U) {
			set_slot (slots, nslots, pos, SLOT_PIN);
			continue;
		}

		if (MONO_TYPE_IS_REFERENCE (ins->inst_vtype)) {
			if (vmv && !vmv->gc_interval) {
				set_slot (slots, nslots, pos, SLOT_PIN);
				continue;
			}

			if (ins->flags & (MONO_INST_VOLATILE | MONO_INST_INDIRECT)) {
				set_slot (slots, nslots, pos, SLOT_PIN);
				continue;
			}

			set_slot (slots, nslots, pos, SLOT_REF);

			live_intervals [pos] = g_slist_prepend_mempool (cfg->mempool, live_intervals [pos], vmv->gc_interval);

			if (cfg->verbose_level > 1) {
				printf ("\tref at %s0x%x(fp) (slot=%d): %s ", ins->inst_offset < 0 ? "-" : "", (ins->inst_offset < 0) ? -(int)ins->inst_offset : (int)ins->inst_offset, pos, mono_type_full_name (ins->inst_vtype));
				mono_linterval_print (vmv->gc_interval);
				printf ("\n");
			}
		}
	}

	loffset = 0;
	if (slots) {
		for (i = 0; i < nslots; ++i) {
			if (slots [i] == SLOT_REF)
				loffset ++;
		}
	}

	bitmap_width = ALIGN_TO (loffset, 8) / 8;
	bitmap_size = bitmap_width * cfg->code_len;
	alloc_size = sizeof (GCMap) + (norefs ? 0 : bitmap_size);
	map = mono_domain_alloc0 (cfg->domain, alloc_size);
	gc_maps_size += alloc_size;

	map->frame_reg = cfg->frame_reg;
	map->locals_offset = min_offset;
	map->locals_size = ALIGN_TO (max_offset - min_offset, sizeof (gpointer));
	map->nslots = nslots;
	map->slots = slots;
	map->bitmap_width = bitmap_width;

	/* Create liveness bitmap */
	loffset = 0;
	if (slots) {
		for (i = 0; i < nslots; ++i) {
			if (map->slots [i] == SLOT_REF) {
				MonoLiveInterval *iv;
				GSList *l;
				MonoLiveRange2 *r;
				int pc_offset;

				for (l = live_intervals [i]; l; l = l->next) {
					iv = l->data;
					for (r = iv->range; r; r = r->next) {
						for (pc_offset = r->from; pc_offset < r->to; ++pc_offset)
							map->bitmap [(map->bitmap_width * pc_offset) + loffset / 8] |= (1 << (loffset % 8));
					}
				}
				loffset ++;
			}
		}
	}

#if 1
	{
		static int precise_count;

		if (map->slots) {
			precise_count ++;
			if (getenv ("MONO_GCMAP_COUNT")) {
				if (precise_count == atoi (getenv ("MONO_GCMAP_COUNT")))
					printf ("LAST: %s\n", mono_method_full_name (cfg->method, TRUE));
				if (precise_count > atoi (getenv ("MONO_GCMAP_COUNT"))) {
					for (i = 0; i < nslots; ++i)
						map->slots [i] = SLOT_PIN;
				}
			}
		}
	}
#endif

	cfg->jit_info->gc_info = map;

	g_free (live_intervals);
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

	mono_counters_register ("Stack space scanned (all)",
							MONO_COUNTER_GC | MONO_COUNTER_INT, &scanned_stacks_stat);
	mono_counters_register ("Stack space scanned (using GC Maps)",
							MONO_COUNTER_GC | MONO_COUNTER_INT, &scanned_stat);
	mono_counters_register ("Stack space scanned (precise)",
							MONO_COUNTER_GC | MONO_COUNTER_INT, &scanned_precisely_stat);
	mono_counters_register ("Stack space scanned (conservative)",
							MONO_COUNTER_GC | MONO_COUNTER_INT, &scanned_conservatively_stat);
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
 * - only the locals are scanned precisely
 * - the computation of the GC maps is slow since it involves a liveness analysis pass
 * - the GC maps are uncompressed and take up a lot of memory.
 * - if the code is finished, less pinning will be done, causing problems because
 *   we promote all surviving objects to old-gen.
 */
