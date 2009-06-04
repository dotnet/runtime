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

#if 0

#include <mono/metadata/gc-internal.h>
#include <mono/utils/mono-counters.h>

#define ALIGN_TO(val,align) ((((guint64)val) + ((align) - 1)) & ~((align) - 1))

#if 0
#define DEBUG(s) do { s; } while (0)
#else
#define DEBUG(s)
#endif

#if 0
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
	/* The offset of the local variable area in the stack frame relative to the frame pointer */
	int locals_offset;
	/* The size of the locals area. Can't use gc_refs->size as it includes padding */
	int locals_size;
	/* 
	 * If this is set, then the frame contains references which we can't
	 * process precisely.
	 */
	guint8 pin;
	/* A bitmap indicating which stack slots contain a GC ref */
	/* If no stack slots contain GC refs, then this is NULL */
	MonoBitSet *gc_refs;
	/* A pair of low pc offset-high pc offset for each 1 bit in gc_refs */
	guint32 live_ranges [MONO_ZERO_LEN_ARRAY];
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

	tls->lmf = mono_get_lmf ();
	if (sigctx) {
		mono_arch_sigctx_to_monoctx (sigctx, &tls->ctx);
		tls->has_context = TRUE;
	} else {
		tls->has_context = FALSE;
	}
	tls->jit_tls = TlsGetValue (mono_jit_tls_id);
}

static void
thread_mark_func (gpointer user_data, guint8 *stack_start, guint8 *stack_end, gboolean precise)
{
	TlsData *tls = user_data;
	MonoJitInfo *ji, res;
	MonoContext ctx, new_ctx;
	MonoLMF *lmf = tls->lmf;
	guint8 *stack_limit;
	gboolean last = TRUE, managed;
	GCMap *map;
	guint8* fp, *locals_start, *locals_end;
	int i, pc_offset;

	if (mono_thread_current () == NULL) {
		if (!precise)
			mono_gc_conservatively_scan_area (stack_start, stack_end);			
		return;
	}

	/* FIXME: sgen-gc.c calls this multiple times for each major collection from pin_from_roots */

	/* FIXME: Use real gc descriptors instead of bitmaps */

	/* This is one past the last address which we have scanned */
	stack_limit = stack_start;

	//DEBUG (printf ("*** %s stack marking %p-%p ***\n", precise ? "Precise" : "Conservative", stack_start, stack_end));

	if (!tls->has_context) {
		memset (&new_ctx, 0, sizeof (ctx));

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

			DEBUG (char *fname = mono_method_full_name (ji->method, TRUE); printf ("Mark: %s offset: 0x%x limit: %p fp: %p locals: %p-%p%s\n", fname, pc_offset, stack_limit, fp, locals_start, locals_end, map->pin ? ", conservative" : ""); g_free (fname));

			/* 
			 * FIXME: Add a function to mark using a bitmap, to avoid doing a 
			 * call for each object.
			 */

			/* Pinning needs to be done first, then the precise scan later */

			if (!precise) {
				g_assert (locals_start >= stack_limit);

				if (locals_start > stack_limit) {
					/* This scans the previously skipped frames as well */
					if (!precise) {
						DEBUG (printf ("\tConservative scan of %p-%p.\n", stack_limit, locals_start));
						mono_gc_conservatively_scan_area (stack_limit, locals_start);
					}
				}

				if (map->pin) {
					DEBUG (printf ("\tConservative scan of %p-%p.\n", locals_start, locals_end));
					mono_gc_conservatively_scan_area (locals_start, locals_end);
				}

				stack_limit = locals_end;
			} else {
				if (!map->pin && map->gc_refs) {
					int loffset = 0;

					for (i = 0; i < mono_bitset_size (map->gc_refs); ++i) {
						if (mono_bitset_test_fast (map->gc_refs, i)) {
							MonoObject **ptr = (MonoObject**)(locals_start + (i * sizeof (gpointer)));
							MonoObject *obj = *ptr;

							if (pc_offset >= map->live_ranges [loffset] && pc_offset < map->live_ranges [loffset + 1]) {
								if (obj) {
									*ptr = mono_gc_scan_object (obj);
									DEBUG (printf ("\tObjref at %p + 0x%x: %p -> %p.\n", locals_start, (int)(i * sizeof (gpointer)), obj, *ptr));
								} else {
									DEBUG (printf ("\tObjref at %p: %p.\n", ptr, obj));
								}
							} else {
								DEBUG (printf ("\tDead Objref at %p.\n", ptr));
							}

							loffset += 2;
						}
					}
				}
			}
		}

		if (stack_limit < stack_end && !precise) {
			DEBUG (printf ("\tConservative scan of %p-%p.\n", stack_limit, stack_end));
			mono_gc_conservatively_scan_area (stack_limit, stack_end);
		}
	} else {
		// FIXME:
		if (!precise) {
			DEBUG (printf ("\tConservative scan of %p-%p.\n", stack_start, stack_end));
			mono_gc_conservatively_scan_area (stack_start, stack_end);
		}
	}

	//mono_gc_conservatively_scan_area (stack_start, stack_end);
}

void
mini_gc_create_gc_map (MonoCompile *cfg)
{
	GCMap *map;
	int i, nslots, alloc_size, loffset, min_offset, max_offset;
	MonoBitSet *gc_refs = NULL;
	gboolean pin = FALSE, norefs = FALSE;
	guint32 *live_range_start, *live_range_end;

	min_offset = ALIGN_TO (cfg->locals_min_stack_offset, sizeof (gpointer));
	max_offset = cfg->locals_max_stack_offset;

	for (i = cfg->locals_start; i < cfg->num_varinfo; i++) {
		MonoInst *ins = cfg->varinfo [i];
		MonoType *t = ins->inst_vtype;

		if ((MONO_TYPE_ISSTRUCT (t) && ins->klass->has_references))
			break;
		if (t->byref || t->type == MONO_TYPE_PTR)
			break;
		if (ins && ins->opcode == OP_REGOFFSET && MONO_TYPE_IS_REFERENCE (ins->inst_vtype))
			break;
	}

	if (i == cfg->num_varinfo)
		norefs = TRUE;

	DEBUG_GC_MAP (printf ("GC Map for %s: 0x%x-0x%x\n", mono_method_full_name (cfg->method, TRUE), min_offset, max_offset));

	nslots = (max_offset - min_offset) / sizeof (gpointer);
	if (!norefs) {
		alloc_size = mono_bitset_alloc_size (nslots, 0);
		gc_refs = mono_bitset_mem_new (mono_domain_alloc0 (cfg->domain, alloc_size), (max_offset - min_offset) / sizeof (gpointer), 0);
		gc_maps_size += alloc_size;
	}
	live_range_start = g_new (guint32, nslots);
	live_range_end = g_new (guint32, nslots);
	loffset = 0;

	for (i = 0; i < nslots; ++i) {
		live_range_start [i] = (guint32)-1;
		live_range_end [i] = 0;
	}

	for (i = cfg->locals_start; i < cfg->num_varinfo; i++) {
		MonoInst *ins = cfg->varinfo [i];
		MonoType *t = ins->inst_vtype;
		MonoMethodVar *vmv;

		vmv = MONO_VARINFO (cfg, i);

		if ((MONO_TYPE_ISSTRUCT (t) && ins->klass->has_references)) {
			int numbits, j;
			gsize *bitmap;

			mono_class_compute_gc_descriptor (ins->klass);

			bitmap = mono_gc_get_bitmap_for_descr (ins->klass->gc_descr, &numbits);

			if (bitmap) {
				int base_bit_offset = (ins->inst_offset - min_offset) / sizeof (gpointer);
				for (j = 0; j < numbits; ++j) {
					if (bitmap [j / GC_BITS_PER_WORD] & (1 << (j % GC_BITS_PER_WORD)))
						/* The descriptor is for the boxed object */
						mono_bitset_set_fast (gc_refs, base_bit_offset + j - (sizeof (MonoObject) / sizeof (gpointer)));
				}
				g_free (bitmap);

				DEBUG_GC_MAP (printf ("\tVType: %s -> 0x%x\n", mono_type_full_name (ins->inst_vtype), (int)ins->inst_offset));

				// FIXME: These have no live range
				pin = TRUE;
			} else {
				// FIXME:
				pin = TRUE;
			}

			continue;
		}
		if (t->byref || t->type == MONO_TYPE_PTR || t->type == MONO_TYPE_I || t->type == MONO_TYPE_U)
			pin = TRUE;
		if (vmv && !vmv->live_range_start)
			pin = TRUE;
		if (pin)
			break;

		if (ins && ins->opcode == OP_REGOFFSET && MONO_TYPE_IS_REFERENCE (ins->inst_vtype)) {
			guint32 pos = (ins->inst_offset - min_offset) / sizeof (gpointer);

			g_assert (ins->inst_offset % sizeof (gpointer) == 0);
			g_assert (ins->inst_offset >= min_offset && ins->inst_offset < max_offset);
			mono_bitset_set_fast (gc_refs, pos);

			/* 
			 * If stack slots are shared, the live range will be the union of
			 * the live range of variables stored in it. This might cause some
			 * objects to outlive their live ranges.
			 */
			live_range_start [pos] = MIN (live_range_start [pos], vmv->live_range_start);
			live_range_end [pos] = MAX (live_range_end [pos], vmv->live_range_end);

			DEBUG_GC_MAP (printf ("\tRef: %s -> 0x%x [0x%x - 0x%x]\n", mono_type_full_name (ins->inst_vtype), (int)ins->inst_offset, vmv->live_range_start, vmv->live_range_end));
		}
	}

	alloc_size = sizeof (GCMap) + (norefs ? 0 : ((mono_bitset_count (gc_refs) - MONO_ZERO_LEN_ARRAY) * sizeof (guint32) * 2));
	map = mono_domain_alloc0 (cfg->domain, alloc_size);
	gc_maps_size += alloc_size;

	map->frame_reg = cfg->frame_reg;
	map->locals_offset = min_offset;
	map->locals_size = ALIGN_TO (max_offset - min_offset, sizeof (gpointer));
	map->gc_refs = gc_refs;
	map->pin = pin;
	loffset = 0;
	if (!norefs) {
		for (i = 0; i < mono_bitset_size (gc_refs); ++i) {
			if (mono_bitset_test_fast (gc_refs, i)) {
				map->live_ranges [loffset ++] = live_range_start [i];
				map->live_ranges [loffset ++] = live_range_end [i];
			}
		}
	}

#if 1
	{
		static int precise_count;

		precise_count ++;
		if (getenv ("PRECISE_COUNT")) {
			if (precise_count == atoi (getenv ("PRECISE_COUNT")))
				printf ("LAST: %s\n", mono_method_full_name (cfg->method, TRUE));
			if (precise_count > atoi (getenv ("PRECISE_COUNT")))
				map->pin = TRUE;
		}
	}
#endif

	cfg->jit_info->gc_info = map;

	g_free (live_range_start);
	g_free (live_range_end);
}

void
mini_gc_init (void)
{
	MonoGCCallbacks cb;

	memset (&cb, 0, sizeof (cb));
	cb.thread_attach_func = thread_attach_func;
	cb.thread_suspend_func = thread_suspend_func;
	/* Comment this out to disable precise stack marking */
	//cb.thread_mark_func = thread_mark_func;
	mono_gc_set_gc_callbacks (&cb);

	mono_counters_register ("GC Maps size",
							MONO_COUNTER_GC | MONO_COUNTER_INT, &gc_maps_size);
}

#else

void
mini_gc_init (void)
{
}

void
mini_gc_create_gc_map (MonoCompile *cfg)
{
}

#endif
