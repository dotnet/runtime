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

/*
 * The GC type of a stack slot.
 * This can change through the method as follows:
 * - a SLOT_REF can become SLOT_NOREF and vice-versa when it becomes live/dead.
 * - a SLOT_PIN can become SLOT_REF after it has been definitely assigned.
 */
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
	/* The number of stack slots */
	int nslots;
	/* Thw width of the bitmap in bytes */
	int bitmap_width;
	guint has_pin_slots : 1;
	guint has_ref_slots : 1;
	/* 
	 * A bitmap whose width is equal to bitmap_width, and whose
	 * height is equal to the number of possible PC offsets.
	 * The bitmap contains a 1 if the corresponding stack slot has type SLOT_REF at the
	 * given pc offset.
	 * FIXME: Compress this.
	 * FIXME: Embed this after the structure.
	 */
	guint8 *ref_bitmap;
	/*
	 * Same for SLOT_PIN. It is possible that the same bit is set in both bitmaps at different
	 * pc offsets, if the slot starts out as PIN, and later changes to REF.
	 */
	guint8 *pin_bitmap;
	/*
	 * A bit array marking slots which contain refs.
	 * This is used only for debugging.
	 */
	guint8 *ref_slots;
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

	int all_slots;
	int noref_slots;
	int ref_slots;
	int pin_slots;
} JITGCStats;

static JITGCStats stats;

#define DEAD_REF ((gpointer)(gssize)0x2a2a2a2a2a2a2a2aULL)

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
			stats.scanned_stacks += stack_end - stack_start;
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
		locals_end = locals_start + (map->nslots * sizeof (gpointer));

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

			if (map->has_pin_slots) {
				guint8 *pin_bitmap = &map->pin_bitmap [(map->bitmap_width * pc_offset)];
				guint8 *p;
				gboolean pinned;

				p = locals_start;
				for (i = 0; i < map->nslots; ++i) {
					pinned = pin_bitmap [i / 8] & (1 << (i % 8));
					if (pinned) {
						DEBUG (printf ("\tscan slot %s0x%x(fp)=%p.\n", (guint8*)p > (guint8*)fp ? "" : "-", ABS ((int)((gssize)p - (gssize)fp)), p));
						mono_gc_conservatively_scan_area (p, p + sizeof (gpointer));
						scanned_conservatively += sizeof (gpointer);
					} else {
						scanned_precisely += sizeof (gpointer);
					}
					p += sizeof (gpointer);
				}
			} else {
				scanned_precisely += map->nslots * sizeof (gpointer);
			}

			stack_limit = locals_end;
		} else {
			if (map->has_ref_slots) {
				guint8 *ref_bitmap = &map->ref_bitmap [(map->bitmap_width * pc_offset)];
				gboolean live;

				for (i = 0; i < map->nslots; ++i) {
					MonoObject **ptr = (MonoObject**)(locals_start + (i * sizeof (gpointer)));

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
							DEBUG (printf ("\tref %s0x%x(fp)=%p: dead (%p)\n", (guint8*)ptr >= (guint8*)fp ? "" : "-", ABS ((int)((gssize)ptr - (gssize)fp)), ptr, obj));
							/*
							 * Fail fast if the live range is incorrect, and
							 * the JITted code tries to access this object
							 */
							*ptr = DEAD_REF;
						}
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

	if (!precise) {
		stats.scanned_stacks += stack_end - stack_start;
		stats.scanned += scanned;
		stats.scanned_precisely += scanned_precisely;
		stats.scanned_conservatively += scanned_conservatively;
	}

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
	int i, nslots, alloc_size, min_offset, max_offset;
	StackSlotType *slots = NULL;
	GSList **live_intervals;
	int bitmap_width, bitmap_size;
	MonoBasicBlock *bb;
	MonoInst *tmp;
	int *pc_offsets;
	gboolean *starts_pinned;
	gboolean has_ref_slots, has_pin_slots;

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
	 * - group ref and no-ref slots together on the stack, to speed up marking, and
	 * to make the gc bitmaps better compressable.
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

#ifdef TARGET_AMD64
	min_offset = ALIGN_TO (cfg->locals_min_stack_offset, sizeof (gpointer));
	max_offset = cfg->locals_max_stack_offset;
#else
	/* min/max stack offset needs to be computed in mono_arch_allocate_vars () */
	NOT_IMPLEMENTED;
#endif

	if (cfg->verbose_level > 1)
		printf ("GC Map for %s: 0x%x-0x%x\n", mono_method_full_name (cfg->method, TRUE), min_offset, max_offset);

	nslots = (max_offset - min_offset) / sizeof (gpointer);
	slots = g_new0 (StackSlotType, nslots);
	for (i = 0; i < nslots; ++i)
		slots [i] = SLOT_NOREF;
	live_intervals = g_new0 (GSList*, nslots);
	starts_pinned = g_new0 (gboolean, nslots);

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
			break;
		}
	}

	for (i = cfg->locals_start; i < cfg->num_varinfo; i++) {
		MonoInst *ins = cfg->varinfo [i];
		MonoType *t = ins->inst_vtype;
		MonoMethodVar *vmv;
		guint32 pos;

		vmv = MONO_VARINFO (cfg, i);

		if (ins->opcode != OP_REGOFFSET)
			continue;

		if (ins->inst_offset % sizeof (gpointer) != 0)
			continue;

		pos = (ins->inst_offset - min_offset) / sizeof (gpointer);

		if ((MONO_TYPE_ISSTRUCT (t) && !ins->klass->has_references))
			continue;

		if ((MONO_TYPE_ISSTRUCT (t) && ins->klass->has_references)) {
			int numbits = 0, j;
			gsize *bitmap;
			gboolean pin = FALSE;
			MonoLiveInterval *interval = NULL;
			int size;

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

			if (ins->backend.is_pinvoke)
				size = mono_class_native_size (ins->klass, NULL);
			else
				size = mono_class_value_size (ins->klass, NULL);

			if (bitmap) {
				for (j = 0; j < numbits; ++j) {
					if (bitmap [j / GC_BITS_PER_WORD] & ((gsize)1 << (j % GC_BITS_PER_WORD))) {
						/* The descriptor is for the boxed object */
						set_slot (slots, nslots, (pos + j - (sizeof (MonoObject) / sizeof (gpointer))), pin ? SLOT_PIN : SLOT_REF);
					}
				}
			} else if (pin) {
				for (j = 0; j < size / sizeof (gpointer); ++j)
					set_slot (slots, nslots, pos + j, SLOT_PIN);
			}

			if (!pin) {
				for (j = 0; j < size / sizeof (gpointer); ++j) {
					live_intervals [pos + j] = g_slist_prepend_mempool (cfg->mempool, live_intervals [pos + j], interval);
					starts_pinned [pos + j] = TRUE;
				}
			}

			g_free (bitmap);

			if (cfg->verbose_level > 1) {
				printf ("\tvtype R%d at fp+0x%x-0x%x: %s ", vmv->vreg, (int)ins->inst_offset, (int)(ins->inst_offset + (size / sizeof (gpointer))), mono_type_full_name (ins->inst_vtype));
				if (interval)
					mono_linterval_print (interval);
				else
					printf ("(pinned)");
				printf ("\n");
			}

			continue;
		}

		if (ins->inst_offset < min_offset || ins->inst_offset >= max_offset)
			/* Vret addr etc. */
			continue;

		if (t->byref) {
			set_slot (slots, nslots, pos, SLOT_PIN);
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
			set_slot (slots, nslots, pos, SLOT_PIN);
			continue;
		}
#endif

		if (MONO_TYPE_IS_REFERENCE (ins->inst_vtype)) {
			if (vmv && !vmv->gc_interval) {
				set_slot (slots, nslots, pos, SLOT_PIN);
				continue;
			}

			if (ins->flags & (MONO_INST_VOLATILE | MONO_INST_INDIRECT)) {
				/*
				 * For volatile variables, treat them alive from the point they are
				 * initialized in the first bblock until the end of the method.
				 */
				if (pc_offsets [vmv->vreg]) {
					vmv->gc_interval = mono_mempool_alloc0 (cfg->mempool, sizeof (MonoLiveInterval));
					mono_linterval_add_range (cfg, vmv->gc_interval, pc_offsets [vmv->vreg], cfg->code_size);
					starts_pinned [pos] = TRUE;
				} else {
					set_slot (slots, nslots, pos, SLOT_PIN);
					continue;
				}
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

#if 1
	{
		static int precise_count;

		precise_count ++;
		if (getenv ("MONO_GCMAP_COUNT")) {
			if (precise_count == atoi (getenv ("MONO_GCMAP_COUNT")))
				printf ("LAST: %s\n", mono_method_full_name (cfg->method, TRUE));
			if (precise_count > atoi (getenv ("MONO_GCMAP_COUNT"))) {
				for (i = 0; i < nslots; ++i)
					slots [i] = SLOT_PIN;
			}
		}
	}
#endif

	/* Create the GC Map */

	has_ref_slots = FALSE;
	has_pin_slots = FALSE;
	for (i = 0; i < nslots; ++i) {
		if (slots [i] == SLOT_REF)
			has_ref_slots = TRUE;
		if (slots [i] == SLOT_PIN || (slots [i] == SLOT_REF && starts_pinned [i]))
			has_pin_slots = TRUE;
	}

	bitmap_width = ALIGN_TO (nslots, 8) / 8;
	bitmap_size = bitmap_width * cfg->code_len;
	alloc_size = sizeof (GCMap);
	map = mono_domain_alloc0 (cfg->domain, alloc_size);
	gc_maps_size += alloc_size;

	map->frame_reg = cfg->frame_reg;
	map->locals_offset = min_offset;
	map->nslots = nslots;
	map->bitmap_width = bitmap_width;
	map->has_ref_slots = has_ref_slots;
	map->has_pin_slots = has_pin_slots;
	map->ref_slots = mono_domain_alloc0 (cfg->domain, bitmap_width);

	if (has_ref_slots)
		map->ref_bitmap = mono_domain_alloc0 (cfg->domain, bitmap_size);
	if (has_pin_slots)
		map->pin_bitmap = mono_domain_alloc0 (cfg->domain, bitmap_size);

	/* Create liveness bitmaps */
	for (i = 0; i < nslots; ++i) {
		int pc_offset;

		if (slots [i] == SLOT_REF) {
			MonoLiveInterval *iv;
			GSList *l;
			MonoLiveRange2 *r;

			map->ref_slots [i / 8] |= (1 << i);

			if (starts_pinned [i]) {
				/* The slots start out as pinned until they are first defined */
				g_assert (live_intervals [i]);
				g_assert (!live_intervals [i]->next);

				iv = live_intervals [i]->data;
				for (pc_offset = 0; pc_offset < iv->range->from; ++pc_offset)
					map->pin_bitmap [(map->bitmap_width * pc_offset) + i / 8] |= (1 << (i % 8));					
			}

			for (l = live_intervals [i]; l; l = l->next) {
				iv = l->data;
				for (r = iv->range; r; r = r->next) {
					for (pc_offset = r->from; pc_offset < r->to; ++pc_offset)
						map->ref_bitmap [(map->bitmap_width * pc_offset) + i / 8] |= (1 << (i % 8));
				}
			}
		} else if (slots [i] == SLOT_PIN) {
			for (pc_offset = 0; pc_offset < cfg->code_len; ++pc_offset)
				map->pin_bitmap [(map->bitmap_width * pc_offset) + i / 8] |= (1 << (i % 8));
		}
	}

	stats.all_slots += nslots;
	for (i = 0; i < nslots; ++i) {
		if (slots [i] == SLOT_REF)
			stats.ref_slots ++;
		else if (slots [i] == SLOT_NOREF)
			stats.noref_slots ++;
		else
			stats.pin_slots ++;
	}

	cfg->jit_info->gc_info = map;

	g_free (live_intervals);
	g_free (pc_offsets);
	g_free (slots);
	g_free (starts_pinned);
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
	mono_counters_register ("Stack space scanned (conservative)",
							MONO_COUNTER_GC | MONO_COUNTER_INT, &stats.scanned_conservatively);
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
 * - vtypes/refs used in EH regions are treated conservatively
 * - the computation of the GC maps is slow since it involves a liveness analysis pass
 * - the GC maps are uncompressed and take up a lot of memory.
 * - if the code is finished, less pinning will be done, causing problems because
 *   we promote all surviving objects to old-gen.
 */
