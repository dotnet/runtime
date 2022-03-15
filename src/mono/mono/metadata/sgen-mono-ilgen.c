/**
 * \file
 * Copyright 2018 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include "config.h"
#ifdef HAVE_SGEN_GC

#include "metadata/method-builder.h"
#include "metadata/method-builder-ilgen.h"
#include "metadata/method-builder-ilgen-internals.h"
#include "sgen/sgen-gc.h"
#include "sgen/sgen-protocol.h"
#include "metadata/monitor.h"
#include "sgen/sgen-layout-stats.h"
#include "sgen/sgen-client.h"
#include "sgen/sgen-cardtable.h"
#include "sgen/sgen-pinning.h"
#include "sgen/sgen-workers.h"
#include "metadata/class-init.h"
#include "metadata/marshal.h"
#include "metadata/abi-details.h"
#include "metadata/class-abi-details.h"
#include <mono/metadata/mono-gc.h>
#include "metadata/runtime.h"
#include "metadata/sgen-bridge-internals.h"
#include "metadata/sgen-mono.h"
#include "metadata/sgen-mono-ilgen.h"
#include "metadata/gc-internals.h"
#include "metadata/handle.h"
#include "utils/mono-memory-model.h"
#include "utils/mono-logger-internals.h"
#include "utils/mono-threads-coop.h"
#include "utils/mono-threads.h"
#include "metadata/w32handle.h"
#include "icall-decl.h"

#define OPDEF(a,b,c,d,e,f,g,h,i,j) \
	a = i,

enum {
#include "mono/cil/opcode.def"
	CEE_LAST
};

#undef OPDEF

#ifdef MANAGED_ALLOCATION
// Cache the SgenThreadInfo pointer in a local 'var'.
// This is the only live producer of CEE_MONO_TLS.
#define EMIT_TLS_ACCESS_VAR(mb, var) \
	do { \
		var = mono_mb_add_local ((mb), mono_get_int_type ());	\
		mono_mb_emit_byte ((mb), MONO_CUSTOM_PREFIX); \
		mono_mb_emit_byte ((mb), CEE_MONO_TLS); \
		mono_mb_emit_i4 ((mb), TLS_KEY_SGEN_THREAD_INFO); \
		mono_mb_emit_stloc ((mb), (var)); \
	} while (0)

#define EMIT_TLS_ACCESS_IN_CRITICAL_REGION_ADDR(mb, var) \
	do { \
		mono_mb_emit_ldloc ((mb), (var)); \
		mono_mb_emit_icon ((mb), MONO_STRUCT_OFFSET (SgenClientThreadInfo, in_critical_region)); \
		mono_mb_emit_byte ((mb), CEE_ADD); \
	} while (0)

#define EMIT_TLS_ACCESS_NEXT_ADDR(mb, var)	do {	\
	mono_mb_emit_ldloc ((mb), (var));		\
	mono_mb_emit_icon ((mb), MONO_STRUCT_OFFSET (SgenThreadInfo, tlab_next));	\
	mono_mb_emit_byte ((mb), CEE_ADD);		\
	} while (0)

#define EMIT_TLS_ACCESS_TEMP_END(mb, var)	do {	\
	mono_mb_emit_ldloc ((mb), (var));		\
	mono_mb_emit_icon ((mb), MONO_STRUCT_OFFSET (SgenThreadInfo, tlab_temp_end));	\
	mono_mb_emit_byte ((mb), CEE_ADD);		\
	mono_mb_emit_no_nullcheck ((mb));			\
	mono_mb_emit_byte ((mb), CEE_LDIND_I);		\
	} while (0)
#endif

static void
emit_nursery_check (MonoMethodBuilder *mb, int *nursery_check_return_labels, gboolean is_concurrent)
{
	int shifted_nursery_start = mono_mb_add_local (mb, mono_get_int_type ());

	memset (nursery_check_return_labels, 0, sizeof (int) * 2);
	// if (ptr_in_nursery (ptr)) return;
	/*
	 * Masking out the bits might be faster, but we would have to use 64 bit
	 * immediates, which might be slower.
	 */
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_LDPTR_NURSERY_START);
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_LDPTR_NURSERY_BITS);
	mono_mb_emit_byte (mb, CEE_SHR_UN);
	mono_mb_emit_stloc (mb, shifted_nursery_start);

	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_LDPTR_NURSERY_BITS);
	mono_mb_emit_byte (mb, CEE_SHR_UN);
	mono_mb_emit_ldloc (mb, shifted_nursery_start);
	nursery_check_return_labels [0] = mono_mb_emit_branch (mb, CEE_BEQ);

	if (!is_concurrent) {
		// if (!ptr_in_nursery (*ptr)) return;
		mono_mb_emit_ldarg (mb, 0);
		mono_mb_emit_no_nullcheck (mb);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_byte (mb, CEE_MONO_LDPTR_NURSERY_BITS);
		mono_mb_emit_byte (mb, CEE_SHR_UN);
		mono_mb_emit_ldloc (mb, shifted_nursery_start);
		nursery_check_return_labels [1] = mono_mb_emit_branch (mb, CEE_BNE_UN);
	}
}

static void
emit_nursery_check_ilgen (MonoMethodBuilder *mb, gboolean is_concurrent)
{
#ifdef MANAGED_WBARRIER
	int i, nursery_check_labels [2];
	emit_nursery_check (mb, nursery_check_labels, is_concurrent);
	/*
	addr = sgen_cardtable + ((address >> CARD_BITS) & CARD_MASK)
	*addr = 1;

	sgen_cardtable:
		LDC_PTR sgen_cardtable

	address >> CARD_BITS
		LDARG_0
		LDC_I4 CARD_BITS
		SHR_UN
	if (SGEN_HAVE_OVERLAPPING_CARDS) {
		LDC_PTR card_table_mask
		AND
	}
	AND
	ldc_i4_1
	stind_i1
	*/
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_LDPTR_CARD_TABLE);
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_icon (mb, CARD_BITS);
	mono_mb_emit_byte (mb, CEE_SHR_UN);
	mono_mb_emit_byte (mb, CEE_CONV_I);
#ifdef SGEN_TARGET_HAVE_OVERLAPPING_CARDS
#if TARGET_SIZEOF_VOID_P == 8
	mono_mb_emit_icon8 (mb, CARD_MASK);
#else
	mono_mb_emit_icon (mb, CARD_MASK);
#endif
	mono_mb_emit_byte (mb, CEE_CONV_I);
	mono_mb_emit_byte (mb, CEE_AND);
#endif
	mono_mb_emit_byte (mb, CEE_ADD);
	mono_mb_emit_icon (mb, 1);
	mono_mb_emit_byte (mb, CEE_STIND_I1);

	// return;
	for (i = 0; i < 2; ++i) {
		if (nursery_check_labels [i])
			mono_mb_patch_branch (mb, nursery_check_labels [i]);
	}
	mono_mb_emit_byte (mb, CEE_RET);
#else
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_icall (mb, mono_gc_wbarrier_generic_nostore_internal);
	mono_mb_emit_byte (mb, CEE_RET);
#endif
}

static void
emit_managed_allocator_ilgen (MonoMethodBuilder *mb, gboolean slowpath, gboolean profiler, int atype)
{
#ifdef MANAGED_ALLOCATION
	int p_var, size_var, real_size_var, thread_var G_GNUC_UNUSED;
	int tlab_next_addr_var, new_next_var;
	guint32 fastpath_branch, max_size_branch, no_oom_branch;

	if (slowpath) {
		switch (atype) {
		case ATYPE_NORMAL:
		case ATYPE_SMALL:
			mono_mb_emit_ldarg (mb, 0);
			mono_mb_emit_icall (mb, ves_icall_object_new_specific);
			break;
		case ATYPE_VECTOR:
			mono_mb_emit_ldarg (mb, 0);
			mono_mb_emit_ldarg (mb, 1);
			mono_mb_emit_icall (mb, ves_icall_array_new_specific);
			break;
		case ATYPE_STRING:
			mono_mb_emit_ldarg (mb, 1);
			mono_mb_emit_icall (mb, ves_icall_string_alloc);
			break;
		default:
			g_assert_not_reached ();
		}

		goto done;
	}

	MonoType *int_type;
	int_type = mono_get_int_type ();
	/*
	 * Tls access might call foreign code or code without jinfo. This can
	 * only happen if we are outside of the critical region.
	 */
	EMIT_TLS_ACCESS_VAR (mb, thread_var);

	size_var = mono_mb_add_local (mb, int_type);
	if (atype == ATYPE_SMALL) {
		/* size_var = size_arg */
		mono_mb_emit_ldarg (mb, 1);
		mono_mb_emit_stloc (mb, size_var);
	} else if (atype == ATYPE_NORMAL) {
		/* size = vtable->klass->instance_size; */
		mono_mb_emit_ldarg (mb, 0);
		mono_mb_emit_icon (mb, MONO_STRUCT_OFFSET (MonoVTable, klass));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_no_nullcheck (mb);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_icon (mb, m_class_offsetof_instance_size ());
		mono_mb_emit_byte (mb, CEE_ADD);
		/* FIXME: assert instance_size stays a 4 byte integer */
		mono_mb_emit_no_nullcheck (mb);
		mono_mb_emit_byte (mb, CEE_LDIND_U4);
		mono_mb_emit_byte (mb, CEE_CONV_I);
		mono_mb_emit_stloc (mb, size_var);
	} else if (atype == ATYPE_VECTOR) {
		int pos, pos_error;

		/*
		 * n > MONO_ARRAY_MAX_INDEX => OutOfMemoryException
		 * n < 0                    => OverflowException
		 *
		 * We can do an unsigned comparison to catch both cases, then in the error
		 * case compare signed to distinguish between them.
		 */
		mono_mb_emit_ldarg (mb, 1);
		mono_mb_emit_icon (mb, MONO_ARRAY_MAX_INDEX);
		mono_mb_emit_byte (mb, CEE_CONV_U);
		pos = mono_mb_emit_short_branch (mb, CEE_BLE_UN_S);

		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_byte (mb, CEE_MONO_NOT_TAKEN);
		mono_mb_emit_ldarg (mb, 1);
		mono_mb_emit_icon (mb, 0);
		pos_error = mono_mb_emit_short_branch (mb, CEE_BLT_S);
		mono_mb_emit_exception (mb, "OutOfMemoryException", NULL);
		mono_mb_patch_short_branch (mb, pos_error);
		mono_mb_emit_exception (mb, "OverflowException", NULL);

		mono_mb_patch_short_branch (mb, pos);

		/* vtable->klass->sizes.element_size */
		mono_mb_emit_ldarg (mb, 0);
		mono_mb_emit_icon (mb, MONO_STRUCT_OFFSET (MonoVTable, klass));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_no_nullcheck (mb);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_icon (mb, m_class_offsetof_sizes ());
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_no_nullcheck (mb);
		mono_mb_emit_byte (mb, CEE_LDIND_U4);
		mono_mb_emit_byte (mb, CEE_CONV_I);

		/* * n */
		mono_mb_emit_ldarg (mb, 1);
		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_op (mb, CEE_MONO_REMAP_OVF_EXC, (gpointer)"OutOfMemoryException");
		mono_mb_emit_byte (mb, CEE_MUL_OVF_UN);
		/* + sizeof (MonoArray) */
		mono_mb_emit_icon (mb, MONO_SIZEOF_MONO_ARRAY);
		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_op (mb, CEE_MONO_REMAP_OVF_EXC, (gpointer)"OutOfMemoryException");
		mono_mb_emit_byte (mb, CEE_ADD_OVF_UN);
		mono_mb_emit_stloc (mb, size_var);
	} else if (atype == ATYPE_STRING) {
		/*
		 * a string allocator method takes the args: (vtable, len)
		 *
		 * bytes = offsetof (MonoString, chars) + ((len + 1) * 2)
		 *
		 * condition:
		 *
		 * bytes <= SIZE_MAX - (SGEN_ALLOC_ALIGN - 1)
		 *
		 * therefore:
		 *
		 * offsetof (MonoString, chars) + ((len + 1) * 2) <= INT32_MAX - (SGEN_ALLOC_ALIGN - 1)
		 * len <= (SIZE_MAX - (SGEN_ALLOC_ALIGN - 1) - offsetof (MonoString, chars)) / 2 - 1
		 *
		 * On 64-bit platforms SIZE_MAX is so big that the 32-bit string length can
		 * never reach the maximum size.
		 */
#if TARGET_SIZEOF_VOID_P == 4
		int pos;

		mono_mb_emit_ldarg (mb, 1);
		mono_mb_emit_icon (mb, (INT32_MAX - (SGEN_ALLOC_ALIGN - 1) - MONO_STRUCT_OFFSET (MonoString, chars)) / 2 - 1);
		pos = mono_mb_emit_short_branch (mb, MONO_CEE_BLE_UN_S);

		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_byte (mb, CEE_MONO_NOT_TAKEN);
		mono_mb_emit_exception (mb, "OutOfMemoryException", NULL);
		mono_mb_patch_short_branch (mb, pos);
#endif

		mono_mb_emit_ldarg (mb, 1);
		mono_mb_emit_byte (mb, CEE_CONV_I);
		mono_mb_emit_icon (mb, 1);
		mono_mb_emit_byte (mb, MONO_CEE_SHL);
		//WE manually fold the above + 2 here
		mono_mb_emit_icon (mb, MONO_STRUCT_OFFSET (MonoString, chars) + 2);
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_stloc (mb, size_var);
	} else {
		g_assert_not_reached ();
	}

#ifdef MANAGED_ALLOCATOR_CAN_USE_CRITICAL_REGION
	EMIT_TLS_ACCESS_IN_CRITICAL_REGION_ADDR (mb, thread_var);
	mono_mb_emit_byte (mb, CEE_LDC_I4_1);
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_ATOMIC_STORE_I4);
	mono_mb_emit_i4 (mb, MONO_MEMORY_BARRIER_NONE);
#endif

	if (sgen_nursery_canaries_enabled ()) {
		real_size_var = mono_mb_add_local (mb, int_type);
		mono_mb_emit_ldloc (mb, size_var);
		mono_mb_emit_stloc(mb, real_size_var);
	}
	else
		real_size_var = size_var;

	/* size += ALLOC_ALIGN - 1; */
	mono_mb_emit_ldloc (mb, size_var);
	mono_mb_emit_icon (mb, SGEN_ALLOC_ALIGN - 1);
	mono_mb_emit_byte (mb, CEE_ADD);
	/* size &= ~(ALLOC_ALIGN - 1); */
	mono_mb_emit_icon (mb, ~(SGEN_ALLOC_ALIGN - 1));
	mono_mb_emit_byte (mb, CEE_AND);
	mono_mb_emit_stloc (mb, size_var);

	/* if (size > MAX_SMALL_OBJ_SIZE) goto slowpath */
	if (atype != ATYPE_SMALL) {
		mono_mb_emit_ldloc (mb, size_var);
		mono_mb_emit_icon (mb, SGEN_MAX_SMALL_OBJ_SIZE);
		max_size_branch = mono_mb_emit_short_branch (mb, MONO_CEE_BGT_UN_S);
	}

	/*
	 * We need to modify tlab_next, but the JIT only supports reading, so we read
	 * another tls var holding its address instead.
	 */

	/* tlab_next_addr (local) = tlab_next_addr (TLS var) */
	tlab_next_addr_var = mono_mb_add_local (mb, int_type);
	EMIT_TLS_ACCESS_NEXT_ADDR (mb, thread_var);
	mono_mb_emit_stloc (mb, tlab_next_addr_var);

	/* p = (void**)tlab_next; */
	p_var = mono_mb_add_local (mb, int_type);
	mono_mb_emit_ldloc (mb, tlab_next_addr_var);
	mono_mb_emit_no_nullcheck (mb);
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_stloc (mb, p_var);

	/* new_next = (char*)p + size; */
	new_next_var = mono_mb_add_local (mb, int_type);
	mono_mb_emit_ldloc (mb, p_var);
	mono_mb_emit_ldloc (mb, size_var);
	mono_mb_emit_byte (mb, CEE_CONV_I);
	mono_mb_emit_byte (mb, CEE_ADD);

	if (sgen_nursery_canaries_enabled ()) {
			mono_mb_emit_icon (mb, CANARY_SIZE);
			mono_mb_emit_byte (mb, CEE_ADD);
	}
	mono_mb_emit_stloc (mb, new_next_var);

	/* if (G_LIKELY (new_next < tlab_temp_end)) */
	mono_mb_emit_ldloc (mb, new_next_var);
	EMIT_TLS_ACCESS_TEMP_END (mb, thread_var);
	fastpath_branch = mono_mb_emit_short_branch (mb, MONO_CEE_BLT_UN_S);

	/* Slowpath */
	if (atype != ATYPE_SMALL)
		mono_mb_patch_short_branch (mb, max_size_branch);

	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_NOT_TAKEN);
	/*
	 * We are no longer in a critical section. We need to do this before calling
	 * to unmanaged land in order to avoid stw deadlocks since unmanaged code
	 * might take locks.
	 */
#ifdef MANAGED_ALLOCATOR_CAN_USE_CRITICAL_REGION
	EMIT_TLS_ACCESS_IN_CRITICAL_REGION_ADDR (mb, thread_var);
	mono_mb_emit_byte (mb, CEE_LDC_I4_0);
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_ATOMIC_STORE_I4);
	mono_mb_emit_i4 (mb, MONO_MEMORY_BARRIER_NONE);
#endif

	/* FIXME: mono_gc_alloc_obj takes a 'size_t' as an argument, not an int32 */
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldloc (mb, real_size_var);
	if (atype == ATYPE_NORMAL || atype == ATYPE_SMALL) {
		mono_mb_emit_icall (mb, mono_gc_alloc_obj);
	} else if (atype == ATYPE_VECTOR) {
		mono_mb_emit_ldarg (mb, 1);
		mono_mb_emit_icall (mb, mono_gc_alloc_vector);
	} else if (atype == ATYPE_STRING) {
		mono_mb_emit_ldarg (mb, 1);
		mono_mb_emit_icall (mb, mono_gc_alloc_string);
	} else {
		g_assert_not_reached ();
	}

	/* if (ret == NULL) throw OOM; */
	mono_mb_emit_byte (mb, CEE_DUP);
	no_oom_branch = mono_mb_emit_branch (mb, CEE_BRTRUE);
	mono_mb_emit_exception (mb, "OutOfMemoryException", NULL);

	mono_mb_patch_branch (mb, no_oom_branch);
	mono_mb_emit_byte (mb, CEE_RET);

	/* Fastpath */
	mono_mb_patch_short_branch (mb, fastpath_branch);

	/* FIXME: Memory barrier */

	/* tlab_next = new_next */
	mono_mb_emit_ldloc (mb, tlab_next_addr_var);
	mono_mb_emit_ldloc (mb, new_next_var);
	mono_mb_emit_byte (mb, CEE_STIND_I);

	/* *p = vtable; */
	mono_mb_emit_ldloc (mb, p_var);
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_byte (mb, CEE_STIND_I);

	/* mark object end with nursery word */
	if (sgen_nursery_canaries_enabled ()) {
			mono_mb_emit_ldloc (mb, p_var);
			mono_mb_emit_ldloc (mb, real_size_var);
			mono_mb_emit_byte (mb, MONO_CEE_ADD);
			mono_mb_emit_icon8 (mb, (mword) CANARY_STRING);
			mono_mb_emit_icon (mb, CANARY_SIZE);
			mono_mb_emit_byte (mb, MONO_CEE_PREFIX1);
			mono_mb_emit_byte (mb, CEE_CPBLK);
	}

	if (atype == ATYPE_VECTOR) {
		/* arr->max_length = max_length; */
		mono_mb_emit_ldloc (mb, p_var);
		mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoArray, max_length));
		mono_mb_emit_ldarg (mb, 1);
		mono_mb_emit_byte (mb, CEE_STIND_I4);
	} else 	if (atype == ATYPE_STRING) {
		/* need to set length and clear the last char */
		/* s->length = len; */
		mono_mb_emit_ldloc (mb, p_var);
		mono_mb_emit_icon (mb, MONO_STRUCT_OFFSET (MonoString, length));
		mono_mb_emit_byte (mb, MONO_CEE_ADD);
		mono_mb_emit_ldarg (mb, 1);
		mono_mb_emit_byte (mb, MONO_CEE_STIND_I4);
	}

#ifdef MANAGED_ALLOCATOR_CAN_USE_CRITICAL_REGION
	EMIT_TLS_ACCESS_IN_CRITICAL_REGION_ADDR (mb, thread_var);
	mono_mb_emit_byte (mb, CEE_LDC_I4_0);
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_ATOMIC_STORE_I4);
#else
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_MEMORY_BARRIER);
#endif
	/*
	We must make sure both vtable and max_length are globaly visible before returning to managed land.
	*/
	mono_mb_emit_i4 (mb, MONO_MEMORY_BARRIER_REL);

	/* return p */
	mono_mb_emit_ldloc (mb, p_var);

 done:

	/*
	 * It's important that we do this outside of the critical region as we
	 * will be invoking arbitrary code.
	 */
	if (profiler) {
		/*
		 * if (G_UNLIKELY (*&mono_profiler_state.gc_allocation_count)) {
		 * 	mono_profiler_raise_gc_allocation (p);
		 * }
		 */

		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_byte (mb, CEE_MONO_LDPTR_PROFILER_ALLOCATION_COUNT);
		mono_mb_emit_no_nullcheck (mb);
		mono_mb_emit_byte (mb, CEE_LDIND_U4);

		int prof_br = mono_mb_emit_short_branch (mb, CEE_BRFALSE_S);

		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_byte (mb, CEE_MONO_NOT_TAKEN);
		mono_mb_emit_byte (mb, CEE_DUP);
		mono_mb_emit_icall (mb, mono_profiler_raise_gc_allocation);

		mono_mb_patch_short_branch (mb, prof_br);
	}

	mono_mb_emit_byte (mb, CEE_RET);
	mb->init_locals = FALSE;
#else
	g_assert_not_reached ();
#endif /* MANAGED_ALLOCATION */
}

void
mono_sgen_mono_ilgen_init (void)
{
	MonoSgenMonoCallbacks cb;
	cb.version = MONO_SGEN_MONO_CALLBACKS_VERSION;
	cb.emit_nursery_check = emit_nursery_check_ilgen;
	cb.emit_managed_allocator = emit_managed_allocator_ilgen;
	mono_install_sgen_mono_callbacks (&cb);
}
#endif

