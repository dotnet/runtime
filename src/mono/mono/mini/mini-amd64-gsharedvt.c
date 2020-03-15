/**
 * \file
 * libcorkscrew-based native unwinder
 *
 * Authors:
 *   Zoltan Varga <vargaz@gmail.com>
 *   Rodrigo Kumpera <kumpera@gmail.com>
 *   Andi McClure <andi.mcclure@xamarin.com>
 *   Johan Lorensson <johan.lorensson@xamarin.com>
 *
 * Copyright 2015 Xamarin, Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <config.h>
#include <glib.h>

#include <mono/metadata/abi-details.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/gc-internals.h>
#include <mono/arch/amd64/amd64-codegen.h>

#include <mono/utils/memcheck.h>

#include "mini.h"
#include "mini-amd64.h"
#include "mini-amd64-gsharedvt.h"
#include "debugger-agent.h"

#if defined (MONO_ARCH_GSHAREDVT_SUPPORTED)

gboolean
mono_arch_gsharedvt_sig_supported (MonoMethodSignature *sig)
{
	return FALSE;
}

static const char*
storage_name (ArgStorage st)
{
	switch (st) {
	case ArgInIReg: return "ArgInIReg";
	case ArgInFloatSSEReg: return "ArgInFloatSSEReg";
	case ArgInDoubleSSEReg: return "ArgInDoubleSSEReg";
	case ArgOnStack: return "ArgOnStack";
	case ArgValuetypeInReg: return "ArgValuetypeInReg";
	case ArgValuetypeAddrInIReg: return "ArgValuetypeAddrInIReg";
	case ArgValuetypeAddrOnStack: return "ArgValuetypeAddrOnStack";
	case ArgGSharedVtInReg: return "ArgGSharedVtInReg";
	case ArgGSharedVtOnStack: return "ArgGSharedVtOnStack";
	case ArgNone: return "ArgNone";
	default: return "unknown";
	}
}

#ifdef DEBUG_AMD64_GSHAREDVT
static char *
arg_info_desc (ArgInfo *info)
{
	GString *str = g_string_new ("");

	g_string_append_printf (str, "offset %d reg %s storage %s nregs %d", info->offset, mono_arch_regname (info->reg), storage_name (info->storage), info->nregs);
	if (info->storage == ArgValuetypeInReg)
		g_string_append_printf (str, " {(%s %s), (%s %s)", 
			storage_name (info->pair_storage [0]),
			mono_arch_regname (info->pair_regs [0]),
			storage_name (info->pair_storage [1]),
			mono_arch_regname (info->pair_regs [1]));

	return g_string_free (str, FALSE);
}
#endif

static void
add_to_map (GPtrArray *map, int src, int dst)
{
	g_ptr_array_add (map, GUINT_TO_POINTER (src));
	g_ptr_array_add (map, GUINT_TO_POINTER (dst));
}

/*
 * Slot mapping:
 *
 * System V:
 * 0..5  - rdi, rsi, rdx, rcx, r8, r9
 * 6..13 - xmm0..xmm7
 * 14..  - stack slots
 *
 * Windows:
 * 0..3 - rcx, rdx, r8, r9
 * 4..7 - xmm0..xmm3
 * 8..  - stack slots
 *
 */
static int
map_reg (int reg)
{
	int i = 0;
	for (i = 0; i < PARAM_REGS; ++i) {
		if (param_regs [i] == reg)
			return i;
	}
	g_error ("Invalid argument register number %d", reg);
	return -1;
}

static int
map_freg (int reg)
{
	return reg + PARAM_REGS;
}

static int
map_stack_slot (int slot)
{
	return slot + PARAM_REGS + FLOAT_PARAM_REGS;
}

/*
Format for the source descriptor:


Format for the destination descriptor:
	bits 0:15  - source register
	bits 16:23 - return marshal
	bits 24:32 - slot count
*/
#define SRC_DESCRIPTOR_MARSHAL_SHIFT 16
#define SRC_DESCRIPTOR_MARSHAL_MASK 0x0ff

#define SLOT_COUNT_SHIFT 24
#define SLOT_COUNT_MASK 0xff
#define SLOT_BYTE_SIZE 8

static int
get_arg_slots (ArgInfo *ainfo, int **out_slots, gboolean is_source_argument)
{
	int sreg = ainfo->reg;
	int sslot = ainfo->offset / 8;
	int *src = NULL;
	int i, nsrc;

	switch (ainfo->storage) {
	case ArgInIReg:
		nsrc = 1;
		src = g_malloc (nsrc * sizeof (int));
		src [0] = map_reg (sreg);
		break;
	case ArgValuetypeInReg:
		nsrc = ainfo->nregs;
		src = g_malloc (nsrc * sizeof (int));
		for (i = 0; i < ainfo->nregs; ++i)
			src [i] = map_reg (ainfo->pair_regs [i]);
		break;
	case ArgOnStack:
		nsrc = ainfo->arg_size / SLOT_BYTE_SIZE;
		src = g_malloc (nsrc * sizeof (int));
		// is_source_argument adds 2 because we're skipping over the old BBP and the return address
		// XXX this is a very fragile setup as changes in alignment for the caller reg array can cause the magic number be 3
		for (i = 0; i < nsrc; ++i)
			src [i] = map_stack_slot (sslot + i + (is_source_argument ? 2 : 0));
		break;
	case ArgInDoubleSSEReg:
	case ArgInFloatSSEReg:
		nsrc = 1;
		src = g_malloc (nsrc * sizeof (int));
		src [0] = map_freg (sreg);
		break;
	case ArgValuetypeAddrInIReg:
		nsrc = 1;
		src = g_malloc (nsrc * sizeof (int));
		src [0] = map_reg (ainfo->pair_regs [0]);
		break;
	case ArgValuetypeAddrOnStack:
		nsrc = 1;
		src = g_malloc (nsrc * sizeof (int));
		// is_source_argument adds 2 because we're skipping over the old BBP and the return address
		// XXX this is a very fragile setup as changes in alignment for the caller reg array can cause the magic number be 3
		src [0] = map_stack_slot (sslot + (is_source_argument ? 2 : 0));
		break;
	default:
		NOT_IMPLEMENTED;
		break;
	}

	*out_slots = src;
	return nsrc;
}

// Once src is known, operate on the dst
static void
handle_marshal_when_src_gsharedvt (ArgInfo *dst_info, int *arg_marshal, int *arg_slots)
{
	switch (dst_info->storage) {
		case ArgInIReg:
		case ArgInDoubleSSEReg:
		case ArgInFloatSSEReg:
			*arg_marshal = GSHAREDVT_ARG_BYREF_TO_BYVAL;
			*arg_slots = 1;
			break;
		case ArgOnStack:
			*arg_marshal = GSHAREDVT_ARG_BYREF_TO_BYVAL;
			g_assert (dst_info->arg_size % SLOT_BYTE_SIZE == 0); // Assert quadword aligned
			*arg_slots = dst_info->arg_size / SLOT_BYTE_SIZE;
			break;
		case ArgValuetypeInReg:
			*arg_marshal = GSHAREDVT_ARG_BYREF_TO_BYVAL;
			*arg_slots = dst_info->nregs;
			break;
		case ArgValuetypeAddrInIReg:
		case ArgValuetypeAddrOnStack:
			*arg_marshal = GSHAREDVT_ARG_NONE;
			*arg_slots = dst_info->nregs;
			break;
		default:
			NOT_IMPLEMENTED; // Inappropriate value: if dst and src are gsharedvt at once, we shouldn't be here
			break;
	}
}

// Once dst is known, operate on the src
static void
handle_marshal_when_dst_gsharedvt (ArgInfo *src_info, int *arg_marshal)
{
	switch (src_info->storage) {
		case ArgInIReg:
		case ArgInDoubleSSEReg:
		case ArgInFloatSSEReg:
		case ArgValuetypeInReg:
		case ArgOnStack:
			*arg_marshal = GSHAREDVT_ARG_BYVAL_TO_BYREF;
			break;
		case ArgValuetypeAddrInIReg:
		case ArgValuetypeAddrOnStack:
			*arg_marshal = GSHAREDVT_ARG_NONE;
			break;
		default:
			NOT_IMPLEMENTED; // See above
			break;
	}
}

static void
handle_map_when_gsharedvt_in_reg (ArgInfo *reg_info, int *n, int **map)
{
	*n = 1;
	*map = g_new0 (int, 1);
	(*map) [0] = map_reg (reg_info->reg);
}

static void
handle_map_when_gsharedvt_on_stack (ArgInfo *reg_info, int *n, int **map, gboolean is_source_argument)
{
	*n = 1;
	*map = g_new0 (int, 1);
	int sslot = reg_info->offset / SLOT_BYTE_SIZE;
	(*map) [0] = map_stack_slot (sslot + (is_source_argument ? 2 : 0)); // see get_arg_slots
}

gpointer
mono_arch_get_gsharedvt_call_info (gpointer addr, MonoMethodSignature *normal_sig, MonoMethodSignature *gsharedvt_sig, gboolean gsharedvt_in, gint32 vcall_offset, gboolean calli)
{
	GSharedVtCallInfo *info;
	CallInfo *caller_cinfo, *callee_cinfo;
	MonoMethodSignature *caller_sig, *callee_sig;
	int aindex, i;
	gboolean var_ret = FALSE;
	CallInfo *cinfo, *gcinfo;
	MonoMethodSignature *sig;
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

	/* sig/cinfo describes the normal call, while gcinfo describes the gsharedvt call */
	if (gsharedvt_in) {
		sig = caller_sig;
		cinfo = caller_cinfo;
		gcinfo = callee_cinfo;
	} else {
		sig = callee_sig;
		cinfo = callee_cinfo;
		gcinfo = caller_cinfo;
	}

	DEBUG_AMD64_GSHAREDVT_PRINT ("source sig: (%s) return (%s)\n", mono_signature_get_desc (caller_sig, FALSE), mono_type_full_name (mono_signature_get_return_type_internal (caller_sig))); // Leak
	DEBUG_AMD64_GSHAREDVT_PRINT ("dest sig: (%s) return (%s)\n", mono_signature_get_desc (callee_sig, FALSE), mono_type_full_name (mono_signature_get_return_type_internal (callee_sig)));

	if (gcinfo->ret.storage == ArgGsharedvtVariableInReg) {
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
		ArgInfo *src_info = &caller_cinfo->args [aindex];
		ArgInfo *dst_info = &callee_cinfo->args [aindex];
		int *src = NULL, *dst = NULL;
		int nsrc = -1, ndst = -1, nslots = 0;

		int arg_marshal = GSHAREDVT_ARG_NONE;
		int arg_slots = 0; // Size in quadwords
		DEBUG_AMD64_GSHAREDVT_PRINT ("-- arg %d in (%s) out (%s)\n", aindex, arg_info_desc (src_info), arg_info_desc (dst_info));

		switch (src_info->storage) {
		case ArgInIReg:
		case ArgInDoubleSSEReg:
		case ArgInFloatSSEReg:
		case ArgValuetypeInReg:
		case ArgOnStack:
			nsrc = get_arg_slots (src_info, &src, TRUE);
			break;
		case ArgGSharedVtInReg:
			handle_marshal_when_src_gsharedvt (dst_info, &arg_marshal, &arg_slots);
			handle_map_when_gsharedvt_in_reg (src_info, &nsrc, &src);
			break;
		case ArgGSharedVtOnStack:
			handle_marshal_when_src_gsharedvt (dst_info, &arg_marshal, &arg_slots);
			handle_map_when_gsharedvt_on_stack (src_info, &nsrc, &src, TRUE);
			break;
		case ArgValuetypeAddrInIReg:
		case ArgValuetypeAddrOnStack:
			nsrc = get_arg_slots (src_info, &src, TRUE);
			break;
		default:
			g_error ("Gsharedvt can't handle source arg type %d", (int)src_info->storage); // Inappropriate value: ArgValuetypeAddrInIReg is for returns only
		}

		switch (dst_info->storage) {
		case ArgInIReg:
		case ArgInDoubleSSEReg:
		case ArgInFloatSSEReg:
		case ArgOnStack:
		case ArgValuetypeInReg:
			ndst = get_arg_slots (dst_info, &dst, FALSE);
			break;
		case ArgGSharedVtInReg:
			handle_marshal_when_dst_gsharedvt (src_info, &arg_marshal);
			handle_map_when_gsharedvt_in_reg (dst_info, &ndst, &dst);
			break;
		case ArgGSharedVtOnStack:
			handle_marshal_when_dst_gsharedvt (src_info, &arg_marshal);
			handle_map_when_gsharedvt_on_stack (dst_info, &ndst, &dst, FALSE);
			break;
		case ArgValuetypeAddrInIReg:
		case ArgValuetypeAddrOnStack:
			ndst = get_arg_slots (dst_info, &dst, FALSE);
			break;
		default:
			g_error ("Gsharedvt can't handle dest arg type %d", (int)dst_info->storage); // See above
		}

		if (arg_marshal == GSHAREDVT_ARG_BYREF_TO_BYVAL && dst_info->byte_arg_size) {
			/* Have to load less than 4 bytes */
			switch (dst_info->byte_arg_size) {
			case 1:
				arg_marshal = dst_info->is_signed ? GSHAREDVT_ARG_BYREF_TO_BYVAL_I1 : GSHAREDVT_ARG_BYREF_TO_BYVAL_U1;
				break;
			case 2:
				arg_marshal = dst_info->is_signed ? GSHAREDVT_ARG_BYREF_TO_BYVAL_I2 : GSHAREDVT_ARG_BYREF_TO_BYVAL_U2;
				break;
			default:
				arg_marshal = dst_info->is_signed ? GSHAREDVT_ARG_BYREF_TO_BYVAL_I4 : GSHAREDVT_ARG_BYREF_TO_BYVAL_U4;
				break;
			}
		}

		if (nsrc)
			src [0] |= (arg_marshal << SRC_DESCRIPTOR_MARSHAL_SHIFT) | (arg_slots << SLOT_COUNT_SHIFT);

		/* Merge and add to the global list*/
		nslots = MIN (nsrc, ndst);
		DEBUG_AMD64_GSHAREDVT_PRINT ("nsrc %d ndst %d\n", nsrc, ndst);

		for (i = 0; i < nslots; ++i)
			add_to_map (map, src [i], dst [i]);

		g_free (src);
		g_free (dst);
	}

	DEBUG_AMD64_GSHAREDVT_PRINT ("-- return in (%s) out (%s) var_ret %d\n", arg_info_desc (&caller_cinfo->ret),  arg_info_desc (&callee_cinfo->ret), var_ret);

	if (cinfo->ret.storage == ArgValuetypeAddrInIReg) {
		/* Both the caller and the callee pass the vtype ret address in r8 (System V) and RCX or RDX (Windows) */
		g_assert (gcinfo->ret.storage == ArgValuetypeAddrInIReg || gcinfo->ret.storage == ArgGsharedvtVariableInReg);
		add_to_map (map, map_reg (caller_cinfo->ret.reg), map_reg (callee_cinfo->ret.reg));
	}

	info = mono_domain_alloc0 (mono_domain_get (), sizeof (GSharedVtCallInfo) + (map->len * sizeof (int)));
	info->addr = addr;
	info->stack_usage = callee_cinfo->stack_usage;
	info->ret_marshal = GSHAREDVT_RET_NONE;
	info->gsharedvt_in = gsharedvt_in ? 1 : 0;
	info->vret_slot = -1;
	info->calli = calli;

	if (var_ret) {
		g_assert (gcinfo->ret.storage == ArgGsharedvtVariableInReg);
		info->vret_arg_reg = map_reg (gcinfo->ret.reg);
		DEBUG_AMD64_GSHAREDVT_PRINT ("mapping vreg_arg_reg to %d in reg %s\n", info->vret_arg_reg, mono_arch_regname (gcinfo->ret.reg));
	} else {
		info->vret_arg_reg = -1;
	}

#ifdef DEBUG_AMD64_GSHAREDVT
	printf ("final map:\n");
	for (i = 0; i < map->len; i += 2) {
		printf ("\t[%d] src %x dst %x\n ", 
			i / 2,
			GPOINTER_TO_UINT (g_ptr_array_index (map, i)),
			GPOINTER_TO_UINT (g_ptr_array_index (map, i + 1)));
	}
#endif

	info->vcall_offset = vcall_offset;
	info->map_count = map->len / 2;
	for (i = 0; i < map->len; ++i)
		info->map [i] = GPOINTER_TO_UINT (g_ptr_array_index (map, i));
	g_ptr_array_free (map, TRUE);

	/* Compute return value marshalling */
	if (var_ret) {
		/* Compute return value marshalling */
		switch (cinfo->ret.storage) {
		case ArgInIReg:
			if (!gsharedvt_in || sig->ret->byref) {
				info->ret_marshal = GSHAREDVT_RET_IREGS_1;
			} else {
				MonoType *ret = sig->ret;

				ret = mini_type_get_underlying_type (ret);
				switch (ret->type) {
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
				case MONO_TYPE_I:
				case MONO_TYPE_U:
				case MONO_TYPE_PTR:
				case MONO_TYPE_FNPTR:
				case MONO_TYPE_OBJECT:
				case MONO_TYPE_U8:
				case MONO_TYPE_I8:
					info->ret_marshal = GSHAREDVT_RET_I8;
					break;
				case MONO_TYPE_GENERICINST:
					g_assert (!mono_type_generic_inst_is_valuetype (ret));
					info->ret_marshal = GSHAREDVT_RET_I8;
					break;
				default:
					g_error ("Gsharedvt can't handle dst type [%d]", (int)sig->ret->type);
				}
			}
			break;
		case ArgValuetypeInReg:
			info->ret_marshal = GSHAREDVT_RET_IREGS_1 - 1 + cinfo->ret.nregs;
			g_assert (cinfo->ret.nregs == 1); // ABI supports 2-register return but we do not implement this.
			break;
		case ArgInDoubleSSEReg:
		case ArgInFloatSSEReg:
			info->ret_marshal = GSHAREDVT_RET_R8;
			break;
		case ArgValuetypeAddrInIReg:
			break;
		default:
			g_error ("Can't marshal return of storage [%d] %s", (int)cinfo->ret.storage, storage_name (cinfo->ret.storage));
		}

		if (gsharedvt_in && cinfo->ret.storage != ArgValuetypeAddrInIReg) {
			/* Allocate stack space for the return value */
			info->vret_slot = map_stack_slot (info->stack_usage / sizeof (target_mgreg_t));
			info->stack_usage += mono_type_stack_size_internal (normal_sig->ret, NULL, FALSE) + sizeof (target_mgreg_t);
		}
		DEBUG_AMD64_GSHAREDVT_PRINT ("RET marshal is %s\n", ret_marshal_name [info->ret_marshal]);
	}

	info->stack_usage = ALIGN_TO (info->stack_usage, MONO_ARCH_FRAME_ALIGNMENT);

	g_free (callee_cinfo);
	g_free (caller_cinfo);

	DEBUG_AMD64_GSHAREDVT_PRINT ("allocated an info at %p stack usage %d\n", info, info->stack_usage);
	return info;
}

#endif
