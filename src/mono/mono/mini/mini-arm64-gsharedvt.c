/**
 * \file
 * gsharedvt support code for arm64
 *
 * Authors:
 *   Zoltan Varga <vargaz@gmail.com>
 *
 * Copyright 2013 Xamarin, Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include "mini.h"
#include "mini-arm64.h"
#include "mini-arm64-gsharedvt.h"
#include "aot-runtime.h"

/*
 * GSHAREDVT
 */
#ifdef MONO_ARCH_GSHAREDVT_SUPPORTED

void
mono_arm_gsharedvt_init (void)
{
}

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

/*
 * Slot mapping:
 * 0..8  - r0..r8
 * 9..16 - d0..d7
 * 17..  - stack slots
 */

static int
map_reg (int reg)
{
	return reg;
}

static int
map_freg (int reg)
{
	return reg + NUM_GSHAREDVT_ARG_GREGS;
}

static int
map_stack_slot (int slot)
{
	return slot + NUM_GSHAREDVT_ARG_GREGS + NUM_GSHAREDVT_ARG_FREGS;
}

static int
get_arg_slots (ArgInfo *ainfo, int **out_slots)
{
	int sreg = ainfo->reg;
	int sslot = ainfo->offset / 8;
	int *src = NULL;
	int i, nsrc;

	switch (ainfo->storage) {
	case ArgInIReg:
	case ArgVtypeByRef:
		nsrc = 1;
		src = g_malloc (nsrc * sizeof (int));
		src [0] = map_reg (sreg);
		break;
	case ArgVtypeByRefOnStack:
		nsrc = 1;
		src = g_malloc (nsrc * sizeof (int));
		src [0] = map_stack_slot (sslot);
		break;
	case ArgInFReg:
	case ArgInFRegR4:
		nsrc = 1;
		src = g_malloc (nsrc * sizeof (int));
		src [0] = map_freg (sreg);
		break;
	case ArgHFA:
		nsrc = ainfo->nregs;
		src = g_malloc (nsrc * sizeof (int));
		for (i = 0; i < ainfo->nregs; ++i)
			src [i] = map_freg (sreg + i);
		break;
	case ArgVtypeInIRegs:
		nsrc = ainfo->nregs;
		src = g_malloc (nsrc * sizeof (int));
		for (i = 0; i < ainfo->nregs; ++i)
			src [i] = map_reg (sreg + i);
		break;
	case ArgOnStack:
	case ArgOnStackR4:
	case ArgOnStackR8:
		nsrc = 1;
		src = g_malloc (nsrc * sizeof (int));
		src [0] = map_stack_slot (sslot);
		break;
	case ArgVtypeOnStack:
		nsrc = ainfo->size / 8;
		src = g_malloc (nsrc * sizeof (int));
		for (i = 0; i < nsrc; ++i)
			src [i] = map_stack_slot (sslot + i);
		break;
	default:
		NOT_IMPLEMENTED;
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
		caller_sig = gsharedvt_sig;
		callee_cinfo = mono_arch_get_call_info (NULL, callee_sig);
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

	if (gcinfo->ret.gsharedvt) {
		/*
		 * The return type is gsharedvt
		 */
		var_ret = TRUE;
	}

	/*
	 * The stack looks like this:
	 * <arguments>
	 * <trampoline frame>
	 * <call area>
	 * We have to map the stack slots in <arguments> to the stack slots in <call area>.
	 */
	map = g_ptr_array_new ();

	for (aindex = 0; aindex < cinfo->nargs; ++aindex) {
		ArgInfo *ainfo = &caller_cinfo->args [aindex];
		ArgInfo *ainfo2 = &callee_cinfo->args [aindex];
		int *src = NULL, *dst = NULL;
		int nsrc, ndst, nslots, src_slot, arg_marshal;

		/*
		 * The src descriptor looks like this:
		 * - 6 bits src slot
		 * - 12 bits number of slots
		 * - 4 bits marshal type (GSHAREDVT_ARG_...)
		 * - 4 bits size/sign descriptor (GSHAREDVT_ARG_SIZE)
		 * - 4 bits offset inside stack slots
		 */
		arg_marshal = GSHAREDVT_ARG_NONE;

		if (ainfo->gsharedvt) {
			/* Pass the value whose address is received in a reg by value */
			g_assert (!ainfo2->gsharedvt);
			ndst = get_arg_slots (ainfo2, &dst);
			nsrc = 1;
			src = g_new0 (int, 1);
			if (ainfo->storage == ArgVtypeByRef)
				src_slot = map_reg (ainfo->reg);
			else
				src_slot = map_stack_slot (ainfo->offset / sizeof (target_mgreg_t));
			g_assert (ndst < 256);
			g_assert (src_slot < 64);
			src [0] = (ndst << 6) | src_slot;
			if (ainfo2->storage == ArgHFA && ainfo2->esize == 4)
				arg_marshal = GSHAREDVT_ARG_BYREF_TO_BYVAL_HFAR4;
			else if (ainfo2->storage == ArgVtypeByRef || ainfo2->storage == ArgVtypeByRefOnStack)
				arg_marshal = GSHAREDVT_ARG_BYREF_TO_BYREF;
			else
				arg_marshal = GSHAREDVT_ARG_BYREF_TO_BYVAL;
		} else {
			nsrc = get_arg_slots (ainfo, &src);
		}
		if (ainfo2->storage == ArgVtypeByRef && ainfo2->gsharedvt) {
			/* Pass the address of the first src slot in a reg */
			if (ainfo->storage != ArgVtypeByRef) {
				if (ainfo->storage == ArgHFA && ainfo->esize == 4) {
					arg_marshal = GSHAREDVT_ARG_BYVAL_TO_BYREF_HFAR4;
					g_assert (src [0] < 64);
					g_assert (nsrc < 256);
					src [0] |= (nsrc << 6);
				} else {
					arg_marshal = GSHAREDVT_ARG_BYVAL_TO_BYREF;
				}
			}
			ndst = 1;
			dst = g_new0 (int, 1);
			dst [0] = map_reg (ainfo2->reg);
		} else if (ainfo2->storage == ArgVtypeByRefOnStack && ainfo2->gsharedvt) {
			/* Pass the address of the first src slot in a stack slot */
			if (ainfo->storage != ArgVtypeByRef)
				arg_marshal = GSHAREDVT_ARG_BYVAL_TO_BYREF;
			ndst = 1;
			dst = g_new0 (int, 1);
			dst [0] = map_stack_slot (ainfo2->offset / sizeof (target_mgreg_t));
		} else {
			ndst = get_arg_slots (ainfo2, &dst);
		}
		if (nsrc)
			src [0] |= (arg_marshal << 18);
		if ((ainfo->storage == ArgOnStack || ainfo->storage == ArgOnStackR4) && ainfo->slot_size != 8) {
			GSharedVtArgSize arg_size = GSHAREDVT_ARG_SIZE_NONE;

			/*
			 * On IOS, stack arguments smaller than 8 bytes can
			 * share a stack slot. Encode this information into
			 * the descriptor.
			 */
			switch (ainfo->slot_size) {
			case 1:
				arg_size = ainfo->sign ? GSHAREDVT_ARG_SIZE_I1 : GSHAREDVT_ARG_SIZE_U1;
				break;
			case 2:
				arg_size = ainfo->sign ? GSHAREDVT_ARG_SIZE_I2 : GSHAREDVT_ARG_SIZE_U2;
				break;
			case 4:
				arg_size = ainfo->sign ? GSHAREDVT_ARG_SIZE_I4 : GSHAREDVT_ARG_SIZE_U4;
				break;
			default:
				NOT_IMPLEMENTED;
				break;
			}
			/* Encode the size/sign */
			src [0] |= (arg_size << 22);
			/* Encode the offset inside the stack slot */
			src [0] |= ((ainfo->offset % 8) << 26);
			if (ainfo2->storage == ArgOnStack || ainfo2->storage == ArgOnStackR4)
				dst [0] |= ((ainfo2->offset % 8) << 26);
		} else if (ainfo2->storage == ArgOnStack && ainfo2->slot_size != 8) {
			/* The caller passes in an address, need to store it into a stack slot */

			GSharedVtArgSize arg_size = GSHAREDVT_ARG_SIZE_NONE;
			switch (ainfo2->slot_size) {
			case 1:
				arg_size = ainfo2->sign ? GSHAREDVT_ARG_SIZE_I1 : GSHAREDVT_ARG_SIZE_U1;
				break;
			case 2:
				arg_size = ainfo2->sign ? GSHAREDVT_ARG_SIZE_I2 : GSHAREDVT_ARG_SIZE_U2;
				break;
			case 4:
				arg_size = ainfo2->sign ? GSHAREDVT_ARG_SIZE_I4 : GSHAREDVT_ARG_SIZE_U4;
				break;
			default:
				NOT_IMPLEMENTED;
				break;
			}
			/* Encode the size/sign */
			src [0] |= (arg_size << 22);
			/* Encode the offset inside the stack slot */
			dst [0] |= ((ainfo2->offset % 8) << 26);
		}
		nslots = MIN (nsrc, ndst);

		for (i = 0; i < nslots; ++i)
			add_to_map (map, src [i], dst [i]);

		g_free (src);
		g_free (dst);
	}

	if (cinfo->ret.storage == ArgVtypeByRef) {
		/* Both the caller and the callee pass the vtype ret address in r8 */
		g_assert (cinfo->ret.storage == gcinfo->ret.storage);
		add_to_map (map, map_reg (ARMREG_R8), map_reg (ARMREG_R8));
	}

	info = mono_domain_alloc0 (mono_domain_get (), sizeof (GSharedVtCallInfo) + (map->len * sizeof (int)));
	info->addr = addr;
	info->stack_usage = callee_cinfo->stack_usage;
	info->ret_marshal = GSHAREDVT_RET_NONE;
	info->gsharedvt_in = gsharedvt_in ? 1 : 0;
	info->vret_slot = -1;
	info->calli = calli;

	if (var_ret) {
		g_assert (gcinfo->ret.gsharedvt);
		info->vret_arg_reg = map_reg (ARMREG_R8);
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
		case ArgInIReg:
			if (!gsharedvt_in || sig->ret->byref) {
				info->ret_marshal = GSHAREDVT_RET_I8;
			} else {
				MonoType *rtype = mini_get_underlying_type (sig->ret);

				switch (rtype->type) {
				case MONO_TYPE_I1:
					info->ret_marshal = GSHAREDVT_RET_I1;
					break;
				case MONO_TYPE_U1:
					info->ret_marshal = GSHAREDVT_RET_U1;
					break;
				case MONO_TYPE_I2:
					info->ret_marshal = GSHAREDVT_RET_I2;
					break;
				case MONO_TYPE_U2:
					info->ret_marshal = GSHAREDVT_RET_U2;
					break;
				case MONO_TYPE_I4:
					info->ret_marshal = GSHAREDVT_RET_I4;
					break;
				case MONO_TYPE_U4:
					info->ret_marshal = GSHAREDVT_RET_U4;
					break;
				default:
					info->ret_marshal = GSHAREDVT_RET_I8;
					break;
				}
			}
			break;
		case ArgInFReg:
			info->ret_marshal = GSHAREDVT_RET_R8;
			break;
		case ArgInFRegR4:
			info->ret_marshal = GSHAREDVT_RET_R4;
			break;
		case ArgVtypeInIRegs:
			info->ret_marshal = GSHAREDVT_RET_IREGS_1 - 1 + cinfo->ret.nregs;
			break;
		case ArgHFA:
			if (cinfo->ret.esize == 4)
				info->ret_marshal = GSHAREDVT_RET_HFAR4_1 - 1 + cinfo->ret.nregs;
			else
				info->ret_marshal = GSHAREDVT_RET_HFAR8_1 - 1 + cinfo->ret.nregs;
			break;
		case ArgVtypeByRef:
			/* No conversion needed */
			break;
		default:
			g_assert_not_reached ();
		}
	}

	if (gsharedvt_in && var_ret && cinfo->ret.storage != ArgVtypeByRef) {
		/* Allocate stack space for the return value */
		info->vret_slot = map_stack_slot (info->stack_usage / sizeof (target_mgreg_t));
		info->stack_usage += mono_type_stack_size_internal (normal_sig->ret, NULL, FALSE) + sizeof (target_mgreg_t);
	}

	info->stack_usage = ALIGN_TO (info->stack_usage, MONO_ARCH_FRAME_ALIGNMENT);

	return info;
}

#else

void
mono_arm_gsharedvt_init (void)
{
}

#endif /* MONO_ARCH_GSHAREDVT_SUPPORTED */
