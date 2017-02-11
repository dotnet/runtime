/**
 * \file
 * gsharedvt support code for x86
 *
 * Authors:
 *   Zoltan Varga <vargaz@gmail.com>
 *
 * Copyright 2013 Xamarin, Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include "mini.h"

#ifdef MONO_ARCH_GSHAREDVT_SUPPORTED

#define ALIGN_TO(val,align) ((((guint64)val) + ((align) - 1)) & ~((align) - 1))

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

/*
 * mono_arch_get_gsharedvt_call_info:
 *
 *   Compute calling convention information for marshalling a call between NORMAL_SIG and GSHAREDVT_SIG.
 * If GSHAREDVT_IN is TRUE, then the caller calls using the signature NORMAL_SIG but the call is received by
 * a method with signature GSHAREDVT_SIG, otherwise its the other way around.
 */
gpointer
mono_arch_get_gsharedvt_call_info (gpointer addr, MonoMethodSignature *normal_sig, MonoMethodSignature *gsharedvt_sig, gboolean gsharedvt_in, gint32 vcall_offset, gboolean calli)
{
	GSharedVtCallInfo *info;
	CallInfo *caller_cinfo, *callee_cinfo;
	MonoMethodSignature *caller_sig, *callee_sig;
	int i, j;
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

	if (gcinfo->vtype_retaddr && gsig->ret && mini_is_gsharedvt_type (gsig->ret)) {
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
	 */
	map = g_ptr_array_new ();

	if (cinfo->vtype_retaddr) {
		/*
		 * Map ret arg.
		 * This handles the case when the method returns a normal vtype, and when it returns a type arg, and its instantiated
		 * with a vtype.
		 */		
		g_ptr_array_add (map, GUINT_TO_POINTER (caller_cinfo->vret_arg_offset / sizeof (gpointer)));
		g_ptr_array_add (map, GUINT_TO_POINTER (callee_cinfo->vret_arg_offset / sizeof (gpointer)));
	}

	for (i = 0; i < cinfo->nargs; ++i) {
		ArgInfo *ainfo = &caller_cinfo->args [i];
		ArgInfo *ainfo2 = &callee_cinfo->args [i];
		int nslots;

		switch (ainfo->storage) {
		case ArgGSharedVt:
			if (ainfo2->storage == ArgOnStack) {
				nslots = callee_cinfo->args [i].nslots;
				if (!nslots)
					nslots = 1;
				g_ptr_array_add (map, GUINT_TO_POINTER ((ainfo->offset / sizeof (gpointer)) + (1 << 16) + (nslots << 18)));
				g_ptr_array_add (map, GUINT_TO_POINTER ((ainfo2->offset / sizeof (gpointer))));
			} else {
				g_ptr_array_add (map, GUINT_TO_POINTER ((ainfo->offset / sizeof (gpointer))));
				g_ptr_array_add (map, GUINT_TO_POINTER ((ainfo2->offset / sizeof (gpointer))));
			}
			break;
		default:
			if (ainfo2->storage == ArgOnStack) {
				nslots = cinfo->args [i].nslots;
				if (!nslots)
					nslots = 1;
				for (j = 0; j < nslots; ++j) {
					g_ptr_array_add (map, GUINT_TO_POINTER ((ainfo->offset / sizeof (gpointer)) + j));
					g_ptr_array_add (map, GUINT_TO_POINTER ((ainfo2->offset / sizeof (gpointer)) + j));
				}
			} else {
				g_assert (ainfo2->storage == ArgGSharedVt);
				g_ptr_array_add (map, GUINT_TO_POINTER ((ainfo->offset / sizeof (gpointer)) + (2 << 16)));
				g_ptr_array_add (map, GUINT_TO_POINTER ((ainfo2->offset / sizeof (gpointer))));
			}
			break;
		}
	}

	info = mono_domain_alloc0 (mono_domain_get (), sizeof (GSharedVtCallInfo) + (map->len * sizeof (int)));
	info->addr = addr;
	info->stack_usage = callee_cinfo->stack_usage;
	info->ret_marshal = GSHAREDVT_RET_NONE;
	info->gsharedvt_in = gsharedvt_in ? 1 : 0;
	info->vret_slot = -1;
	info->calli = calli ? 1 : 0;
	if (var_ret)
		info->vret_arg_slot = gcinfo->vret_arg_offset / sizeof (gpointer);
	else
		info->vret_arg_slot = -1;
	info->vcall_offset = vcall_offset;
	info->map_count = map->len / 2;
	for (i = 0; i < map->len; ++i)
		info->map [i] = GPOINTER_TO_UINT (g_ptr_array_index (map, i));
	g_ptr_array_free (map, TRUE);

	/* Compute return value marshalling */
	if (var_ret) {
		switch (cinfo->ret.storage) {
		case ArgInIReg:
			if (gsharedvt_in && !sig->ret->byref && sig->ret->type == MONO_TYPE_I1)
				info->ret_marshal = GSHAREDVT_RET_I1;
			else if (gsharedvt_in && !sig->ret->byref && (sig->ret->type == MONO_TYPE_U1 || sig->ret->type == MONO_TYPE_BOOLEAN))
				info->ret_marshal = GSHAREDVT_RET_U1;
			else if (gsharedvt_in && !sig->ret->byref && sig->ret->type == MONO_TYPE_I2)
				info->ret_marshal = GSHAREDVT_RET_I2;
			else if (gsharedvt_in && !sig->ret->byref && (sig->ret->type == MONO_TYPE_U2 || sig->ret->type == MONO_TYPE_CHAR))
				info->ret_marshal = GSHAREDVT_RET_U2;
			else if (cinfo->ret.is_pair)
 				info->ret_marshal = GSHAREDVT_RET_IREGS;
			else
				info->ret_marshal = GSHAREDVT_RET_IREG;
			break;
		case ArgOnDoubleFpStack:
			info->ret_marshal = GSHAREDVT_RET_DOUBLE_FPSTACK;
			break;
		case ArgOnFloatFpStack:
			info->ret_marshal = GSHAREDVT_RET_FLOAT_FPSTACK;
			break;
		case ArgOnStack:
			/* The caller passes in a vtype ret arg as well */
			g_assert (gcinfo->vtype_retaddr);
			/* Just have to pop the arg, as done by normal methods in their epilog */
			info->ret_marshal = GSHAREDVT_RET_STACK_POP;
			break;
		default:
			g_assert_not_reached ();
		}
	} else if (gsharedvt_in && cinfo->vtype_retaddr) {
		info->ret_marshal = GSHAREDVT_RET_STACK_POP;
	}

	if (gsharedvt_in && var_ret && !caller_cinfo->vtype_retaddr) {
		/* Allocate stack space for the return value */
		info->vret_slot = info->stack_usage / sizeof (gpointer);
		// FIXME:
		info->stack_usage += sizeof (gpointer) * 3;
	}

	info->stack_usage = ALIGN_TO (info->stack_usage, MONO_ARCH_FRAME_ALIGNMENT);

	g_free (caller_cinfo);
	g_free (callee_cinfo);

	return info;
}
#endif
