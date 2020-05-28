/**
 * \file
 * gsharedvt support code for arm
 *
 * Authors:
 *   Zoltan Varga <vargaz@gmail.com>
 *
 * Copyright 2013 Xamarin, Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <config.h>
#include <glib.h>

#include <mono/metadata/abi-details.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/profiler-private.h>
#include <mono/arch/arm/arm-codegen.h>
#include <mono/arch/arm/arm-vfp-codegen.h>

#include "mini.h"
#include "mini-arm.h"

#ifdef MONO_ARCH_GSHAREDVT_SUPPORTED

/*
 * GSHAREDVT
 */

gboolean
mono_arch_gsharedvt_sig_supported (MonoMethodSignature *sig)
{
	/*
	if (sig->ret && is_variable_size (sig->ret))
		return FALSE;
	*/
	return TRUE;
}

static void
add_to_map (GPtrArray *map, int src, int dst)
{
	g_ptr_array_add (map, GUINT_TO_POINTER (src));
	g_ptr_array_add (map, GUINT_TO_POINTER (dst));
}

static int
map_reg (int reg)
{
	return reg;
}

static int
map_stack_slot (int slot)
{
	return slot + 4;
}

static int
get_arg_slots (ArgInfo *ainfo, int **out_slots)
{
	int sreg = ainfo->reg;
	int sslot = ainfo->offset / 4;
	int *src = NULL;
	int i, nsrc;

	switch (ainfo->storage) {
	case RegTypeGeneral:
		nsrc = 1;
		src = g_malloc (nsrc * sizeof (int));
		src [0] = map_reg (sreg);
		break;
	case RegTypeIRegPair:
		nsrc = 2;
		src = g_malloc (nsrc * sizeof (int));
		src [0] = map_reg (sreg);
		src [1] = map_reg (sreg + 1);
		break;
	case RegTypeStructByVal:
		nsrc = ainfo->struct_size / 4;
		src = g_malloc (nsrc * sizeof (int));
		g_assert (ainfo->size <= nsrc);
		for (i = 0; i < ainfo->size; ++i)
			src [i] = map_reg (sreg + i);
		for (i = ainfo->size; i < nsrc; ++i)
			src [i] = map_stack_slot (sslot + (i - ainfo->size));
		break;
	case RegTypeBase:
		nsrc = ainfo->size / 4;
		src = g_malloc (nsrc * sizeof (int));
		for (i = 0; i < nsrc; ++i)
			src [i] = map_stack_slot (sslot + i);
		break;
	case RegTypeBaseGen:
		nsrc = 2;
		src = g_malloc (nsrc * sizeof (int));
		src [0] = map_reg (ARMREG_R3);
		src [1] = map_stack_slot (sslot);
		break;
	default:
		g_assert_not_reached ();
		break;
	}

	*out_slots = src;
	return nsrc;
}

/*
 * mono_arch_get_gsharedvt_call_info:
 *
 *   See mini-x86.c for documentation.
 */
gpointer
mono_arch_get_gsharedvt_call_info (gpointer addr, MonoMethodSignature *normal_sig, MonoMethodSignature *gsharedvt_sig, gboolean gsharedvt_in, gint32 vcall_offset, gboolean calli)
{
	GSharedVtCallInfo *info;
	CallInfo *caller_cinfo, *callee_cinfo;
	MonoMethodSignature *caller_sig, *callee_sig;
	int aindex, i;
	gboolean var_ret = FALSE;
	gboolean have_fregs = FALSE;
	CallInfo *cinfo, *gcinfo;
	MonoMethodSignature *sig, *gsig;
	GPtrArray *map;

	if (gsharedvt_in) {
		caller_sig = normal_sig;
		callee_sig = gsharedvt_sig;
		caller_cinfo = mono_arch_get_call_info (NULL, caller_sig);
		callee_cinfo = mono_arch_get_call_info (NULL, callee_sig);
	} else {
		callee_sig = normal_sig;
		callee_cinfo = mono_arch_get_call_info (NULL, callee_sig);
		caller_sig = gsharedvt_sig;
		caller_cinfo = mono_arch_get_call_info (NULL, caller_sig);
	}

	/*
	 * If GSHAREDVT_IN is true, this means we are transitioning from normal to gsharedvt code. The caller uses the
	 * normal call signature, while the callee uses the gsharedvt signature.
	 * If GSHAREDVT_IN is false, its the other way around.
	 */

	/* sig/cinfo describes the normal call, while gsig/gcinfo describes the gsharedvt call */
	if (gsharedvt_in) {
		sig = caller_sig;
		gsig = callee_sig;
		cinfo = caller_cinfo;
		gcinfo = callee_cinfo;
	} else {
		sig = callee_sig;
		gsig = caller_sig;
		cinfo = callee_cinfo;
		gcinfo = caller_cinfo;
	}

	if (gcinfo->ret.storage == RegTypeStructByAddr && gsig->ret && mini_is_gsharedvt_type (gsig->ret)) {
		/*
		 * The return type is gsharedvt
		 */
		var_ret = TRUE;
	}

	/*
	 * The stack looks like this:
	 * <arguments>
	 * <ret addr>
	 * <saved ebp>
	 * <call area>
	 * We have to map the stack slots in <arguments> to the stack slots in <call area>.
	 * The argument registers are mapped to slot 0..3, stack slot 0 is mapped to slot 4, etc.
	 */
	map = g_ptr_array_new ();

	if (cinfo->ret.storage == RegTypeStructByAddr) {
		/*
		 * Map ret arg.
		 * This handles the case when the method returns a normal vtype, and when it returns a type arg, and its instantiated
		 * with a vtype.
		 */
		g_assert (caller_cinfo->ret.storage == RegTypeStructByAddr);
		g_assert (callee_cinfo->ret.storage == RegTypeStructByAddr);
		add_to_map (map, map_reg (caller_cinfo->ret.reg), map_reg (callee_cinfo->ret.reg));
	}

	for (aindex = 0; aindex < cinfo->nargs; ++aindex) {
		ArgInfo *ainfo = &caller_cinfo->args [aindex];
		ArgInfo *ainfo2 = &callee_cinfo->args [aindex];
		int *src = NULL, *dst = NULL;
		int nsrc, ndst, nslots, src_slot, arg_marshal;

		if (ainfo->storage == RegTypeFP || ainfo2->storage == RegTypeFP) {
			have_fregs = TRUE;
			continue;
		}

		/*
		 * The src descriptor looks like this:
		 * - 4 bits src slot
		 * - 12 bits number of slots
		 * - 8 bits marshal type (GSHAREDVT_ARG_...)
		 */

		arg_marshal = GSHAREDVT_ARG_NONE;

		if (ainfo->storage == RegTypeGSharedVtInReg || ainfo->storage == RegTypeGSharedVtOnStack) {
			/* Pass the value whose address is received in a reg by value */
			g_assert (ainfo2->storage != RegTypeGSharedVtInReg);
			ndst = get_arg_slots (ainfo2, &dst);
			nsrc = 1;
			src = g_new0 (int, 1);
			if (ainfo->storage == RegTypeGSharedVtInReg)
				src_slot = map_reg (ainfo->reg);
			else
				src_slot = map_stack_slot (ainfo->offset / sizeof (target_mgreg_t));
			g_assert (ndst < 256);
			g_assert (src_slot < 256);
			src [0] = (ndst << 8) | src_slot;

			if (ainfo2->storage == RegTypeGeneral && ainfo2->size != 0 && ainfo2->size != sizeof (target_mgreg_t)) {
				/* Have to load less than 4 bytes */
				switch (ainfo2->size) {
				case 1:
					arg_marshal = ainfo2->is_signed ? GSHAREDVT_ARG_BYREF_TO_BYVAL_I1 : GSHAREDVT_ARG_BYREF_TO_BYVAL_U1;
					break;
				case 2:
					arg_marshal = ainfo2->is_signed ? GSHAREDVT_ARG_BYREF_TO_BYVAL_I2 : GSHAREDVT_ARG_BYREF_TO_BYVAL_U2;
					break;
				default:
					g_assert_not_reached ();
					break;
				}
			} else {
				arg_marshal = GSHAREDVT_ARG_BYREF_TO_BYVAL;
			}
		} else {
			nsrc = get_arg_slots (ainfo, &src);
		}
		if (ainfo2->storage == RegTypeGSharedVtInReg) {
			/* Pass the address of the first src slot in a reg */
			arg_marshal = GSHAREDVT_ARG_BYVAL_TO_BYREF;
			ndst = 1;
			dst = g_new0 (int, 1);
			dst [0] = map_reg (ainfo2->reg);
		} else if (ainfo2->storage == RegTypeGSharedVtOnStack) {
			/* Pass the address of the first src slot in a stack slot */
			arg_marshal = GSHAREDVT_ARG_BYVAL_TO_BYREF;
			ndst = 1;
			dst = g_new0 (int, 1);
			dst [0] = map_stack_slot (ainfo2->offset / sizeof (target_mgreg_t));
		} else {
			ndst = get_arg_slots (ainfo2, &dst);
		}
		if (nsrc)
			src [0] |= (arg_marshal << 24);
		nslots = MIN (nsrc, ndst);

		for (i = 0; i < nslots; ++i)
			add_to_map (map, src [i], dst [i]);

		g_free (src);
		g_free (dst);
	}

	info = mono_domain_alloc0 (mono_domain_get (), sizeof (GSharedVtCallInfo) + (map->len * sizeof (int)));
	info->addr = addr;
	info->stack_usage = callee_cinfo->stack_usage;
	info->ret_marshal = GSHAREDVT_RET_NONE;
	info->gsharedvt_in = gsharedvt_in ? 1 : 0;
	info->vret_slot = -1;
	info->calli = calli;
	if (var_ret) {
		g_assert (gcinfo->ret.storage == RegTypeStructByAddr);
		info->vret_arg_reg = gcinfo->ret.reg;
	} else {
		info->vret_arg_reg = -1;
	}
	info->vcall_offset = vcall_offset;
	info->map_count = map->len / 2;
	for (i = 0; i < map->len; ++i)
		info->map [i] = GPOINTER_TO_UINT (g_ptr_array_index (map, i));
	g_ptr_array_free (map, TRUE);

	/* Compute return value marshalling */
	if (var_ret) {
		switch (cinfo->ret.storage) {
		case RegTypeGeneral:
			if (gsharedvt_in && !sig->ret->byref && sig->ret->type == MONO_TYPE_I1)
				info->ret_marshal = GSHAREDVT_RET_I1;
			else if (gsharedvt_in && !sig->ret->byref && (sig->ret->type == MONO_TYPE_U1 || sig->ret->type == MONO_TYPE_BOOLEAN))
				info->ret_marshal = GSHAREDVT_RET_U1;
			else if (gsharedvt_in && !sig->ret->byref && sig->ret->type == MONO_TYPE_I2)
				info->ret_marshal = GSHAREDVT_RET_I2;
			else if (gsharedvt_in && !sig->ret->byref && (sig->ret->type == MONO_TYPE_U2 || sig->ret->type == MONO_TYPE_CHAR))
				info->ret_marshal = GSHAREDVT_RET_U2;
			else
				info->ret_marshal = GSHAREDVT_RET_IREG;
			break;
		case RegTypeIRegPair:
			info->ret_marshal = GSHAREDVT_RET_IREGS;
			break;
		case RegTypeFP:
			if (mono_arm_is_hard_float ()) {
				if (cinfo->ret.size == 4)
					info->ret_marshal = GSHAREDVT_RET_VFP_R4;
				else
					info->ret_marshal = GSHAREDVT_RET_VFP_R8;
			} else {
				if (cinfo->ret.size == sizeof (target_mgreg_t))
					info->ret_marshal = GSHAREDVT_RET_IREG;
				else
					info->ret_marshal = GSHAREDVT_RET_IREGS;
			}
			break;
		case RegTypeStructByAddr:
			info->ret_marshal = GSHAREDVT_RET_NONE;
			break;
		default:
			g_assert_not_reached ();
		}
	}

	if (gsharedvt_in && var_ret && caller_cinfo->ret.storage != RegTypeStructByAddr) {
		/* Allocate stack space for the return value */
		info->vret_slot = map_stack_slot (info->stack_usage / sizeof (target_mgreg_t));
		info->stack_usage += mono_type_stack_size_internal (normal_sig->ret, NULL, FALSE) + sizeof (target_mgreg_t);
	}

	info->stack_usage = ALIGN_TO (info->stack_usage, MONO_ARCH_FRAME_ALIGNMENT);
	info->caller_cinfo = caller_cinfo;
	info->callee_cinfo = callee_cinfo;
	info->have_fregs = have_fregs;

	return info;
}

#endif
