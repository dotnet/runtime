/**
 * \file
 *
 * Author:
 *	Mono Project (http://www.mono-project.com)
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Rodrigo Kumpera
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <config.h>

#include <mono/metadata/object-internals.h>
#include <mono/metadata/dynamic-image-internals.h>
#include <mono/metadata/verify.h>
#include <mono/metadata/verify-internals.h>
#include <mono/metadata/opcodes.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/reflection.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/mono-endian.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/class-init.h>
#include <mono/metadata/security-manager.h>
#include <mono/metadata/security-core-clr.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/mono-basic-block.h>
#include <mono/metadata/attrdefs.h>
#include <mono/utils/mono-counters.h>
#include <mono/utils/monobitset.h>
#include <mono/utils/mono-error-internals.h>
#include <string.h>
#include <ctype.h>

static MiniVerifierMode verifier_mode = MONO_VERIFIER_MODE_OFF;
static gboolean verify_all = FALSE;

/*
 * Set the desired level of checks for the verfier.
 * 
 */
void
mono_verifier_set_mode (MiniVerifierMode mode)
{
	verifier_mode = mode;
}

void
mono_verifier_enable_verify_all ()
{
	verify_all = TRUE;
}

/*
 * Returns TURE if @type is VAR or MVAR
 */
static gboolean
mono_type_is_generic_argument (MonoType *type)
{
	return type->type == MONO_TYPE_VAR || type->type == MONO_TYPE_MVAR;
}

/*A side note here. We don't need to check if arguments are broken since this
is only need to be done by the runtime before realizing the type.
*/
static gboolean
is_valid_generic_instantiation (MonoGenericContainer *gc, MonoGenericContext *context, MonoGenericInst *ginst)
{
	ERROR_DECL (error);
	int i;

	if (ginst->type_argc != gc->type_argc)
		return FALSE;

	for (i = 0; i < gc->type_argc; ++i) {
		MonoGenericParamInfo *param_info = mono_generic_container_get_param_info (gc, i);
		MonoClass *paramClass;
		MonoClass **constraints;
		MonoType *param_type = ginst->type_argv [i];

		/*it's not our job to validate type variables*/
		if (mono_type_is_generic_argument (param_type))
			continue;

		paramClass = mono_class_from_mono_type_internal (param_type);


		/* A GTD can't be a generic argument.
		 *
		 * Due to how types are encoded we must check for the case of a genericinst MonoType and GTD MonoClass.
		 * This happens in cases such as: class Foo<T>  { void X() { new Bar<T> (); } }
		 *
		 * Open instantiations can have GTDs as this happens when one type is instantiated with others params
		 * and the former has an expansion into the later. For example:
		 * class B<K> {}
		 * class A<T>: B<K> {}
		 * The type A <K> has a parent B<K>, that is inflated into the GTD B<>.
		 * Since A<K> is open, thus not instantiatable, this is valid.
		 */
		if (mono_class_is_gtd (paramClass) && param_type->type != MONO_TYPE_GENERICINST && !ginst->is_open)
			return FALSE;

		/*it's not safe to call mono_class_init_internal from here*/
		if (mono_class_is_ginst (paramClass) && !m_class_is_inited (paramClass)) {
			if (!mono_verifier_class_is_valid_generic_instantiation (paramClass))
				return FALSE;
		}

		if (!param_info->constraints && !(param_info->flags & GENERIC_PARAMETER_ATTRIBUTE_SPECIAL_CONSTRAINTS_MASK))
			continue;

		if ((param_info->flags & GENERIC_PARAMETER_ATTRIBUTE_VALUE_TYPE_CONSTRAINT) && (!m_class_is_valuetype (paramClass) || mono_class_is_nullable (paramClass)))
			return FALSE;

		if ((param_info->flags & GENERIC_PARAMETER_ATTRIBUTE_REFERENCE_TYPE_CONSTRAINT) && m_class_is_valuetype (paramClass))
			return FALSE;

		if ((param_info->flags & GENERIC_PARAMETER_ATTRIBUTE_CONSTRUCTOR_CONSTRAINT) && !m_class_is_valuetype (paramClass) && !mono_class_has_default_constructor (paramClass, TRUE))
			return FALSE;

		if (!param_info->constraints)
			continue;

		for (constraints = param_info->constraints; *constraints; ++constraints) {
			MonoClass *ctr = *constraints;
			MonoType *inflated;

			inflated = mono_class_inflate_generic_type_checked (m_class_get_byval_arg (ctr), context, error);
			if (!is_ok (error)) {
				mono_error_cleanup (error);
				return FALSE;
			}
			ctr = mono_class_from_mono_type_internal (inflated);
			mono_metadata_free_type (inflated);

			/*FIXME maybe we need the same this as verifier_class_is_assignable_from*/
			if (!mono_class_is_assignable_from_slow (ctr, paramClass))
				return FALSE;
		}
	}
	return TRUE;
}

gboolean
mono_verifier_class_is_valid_generic_instantiation (MonoClass *klass)
{
	MonoGenericClass *gklass = mono_class_get_generic_class (klass);
	MonoGenericInst *ginst = gklass->context.class_inst;
	MonoGenericContainer *gc = mono_class_get_generic_container (gklass->container_class);
	return is_valid_generic_instantiation (gc, &gklass->context, ginst);
}

gboolean
mono_verifier_is_method_valid_generic_instantiation (MonoMethod *method)
{
	if (!method->is_inflated)
		return TRUE;
	MonoMethodInflated *gmethod = (MonoMethodInflated *)method;
	MonoGenericInst *ginst = gmethod->context.method_inst;
	MonoGenericContainer *gc = mono_method_get_generic_container (gmethod->declaring);
	if (!gc) /*non-generic inflated method - it's part of a generic type  */
		return TRUE;
	return is_valid_generic_instantiation (gc, &gmethod->context, ginst);
}

#ifndef DISABLE_VERIFIER
/*
 * Pull the list of opcodes
 */
#define OPDEF(a,b,c,d,e,f,g,h,i,j) \
	a = i,

enum {
#include "mono/cil/opcode.def"
	LAST = 0xff
};
#undef OPDEF

#ifdef MONO_VERIFIER_DEBUG
#define VERIFIER_DEBUG(code) do { code } while (0)
#else
#define VERIFIER_DEBUG(code)
#endif

//////////////////////////////////////////////////////////////////
#define IS_STRICT_MODE(ctx) (((ctx)->level & MONO_VERIFY_NON_STRICT) == 0)
#define IS_FAIL_FAST_MODE(ctx) (((ctx)->level & MONO_VERIFY_FAIL_FAST) == MONO_VERIFY_FAIL_FAST)
#define IS_SKIP_VISIBILITY(ctx) (((ctx)->level & MONO_VERIFY_SKIP_VISIBILITY) == MONO_VERIFY_SKIP_VISIBILITY)
#define IS_REPORT_ALL_ERRORS(ctx) (((ctx)->level & MONO_VERIFY_REPORT_ALL_ERRORS) == MONO_VERIFY_REPORT_ALL_ERRORS)
#define CLEAR_PREFIX(ctx, prefix) do { (ctx)->prefix_set &= ~(prefix); } while (0)
#define ADD_VERIFY_INFO(__ctx, __msg, __status, __exception)	\
	do {	\
		MonoVerifyInfoExtended *vinfo = g_new (MonoVerifyInfoExtended, 1);	\
		vinfo->info.status = __status;	\
		vinfo->info.message = ( __msg );	\
		vinfo->exception_type = (__exception);	\
		(__ctx)->list = g_slist_prepend ((__ctx)->list, vinfo);	\
	} while (0)

//TODO support MONO_VERIFY_REPORT_ALL_ERRORS
#define ADD_VERIFY_ERROR(__ctx, __msg)	\
	do {	\
		ADD_VERIFY_INFO(__ctx, __msg, MONO_VERIFY_ERROR, MONO_EXCEPTION_INVALID_PROGRAM); \
		(__ctx)->valid = 0; \
	} while (0)

#define CODE_NOT_VERIFIABLE(__ctx, __msg) \
	do {	\
		if ((__ctx)->verifiable || IS_REPORT_ALL_ERRORS (__ctx)) { \
			ADD_VERIFY_INFO(__ctx, __msg, MONO_VERIFY_NOT_VERIFIABLE, MONO_EXCEPTION_UNVERIFIABLE_IL); \
			(__ctx)->verifiable = 0; \
			if (IS_FAIL_FAST_MODE (__ctx)) \
				(__ctx)->valid = 0; \
		} \
	} while (0)

#define ADD_VERIFY_ERROR2(__ctx, __msg, __exception)	\
	do {	\
		ADD_VERIFY_INFO(__ctx, __msg, MONO_VERIFY_ERROR, __exception); \
		(__ctx)->valid = 0; \
	} while (0)

#define CODE_NOT_VERIFIABLE2(__ctx, __msg, __exception) \
	do {	\
		if ((__ctx)->verifiable || IS_REPORT_ALL_ERRORS (__ctx)) { \
			ADD_VERIFY_INFO(__ctx, __msg, MONO_VERIFY_NOT_VERIFIABLE, __exception); \
			(__ctx)->verifiable = 0; \
			if (IS_FAIL_FAST_MODE (__ctx)) \
				(__ctx)->valid = 0; \
		} \
	} while (0)

#define CHECK_ADD4_OVERFLOW_UN(a, b) ((guint32)(0xFFFFFFFFU) - (guint32)(b) < (guint32)(a))
#define CHECK_ADD8_OVERFLOW_UN(a, b) ((guint64)(0xFFFFFFFFFFFFFFFFUL) - (guint64)(b) < (guint64)(a))

#if SIZEOF_VOID_P == 4
#define CHECK_ADDP_OVERFLOW_UN(a,b) CHECK_ADD4_OVERFLOW_UN(a, b)
#else
#define CHECK_ADDP_OVERFLOW_UN(a,b) CHECK_ADD8_OVERFLOW_UN(a, b)
#endif

#define ADDP_IS_GREATER_OR_OVF(a, b, c) (((a) + (b) > (c)) || CHECK_ADDP_OVERFLOW_UN (a, b))
#define ADD_IS_GREATER_OR_OVF(a, b, c) (((a) + (b) > (c)) || CHECK_ADD4_OVERFLOW_UN (a, b))

/*Flags to be used with ILCodeDesc::flags */
enum {
	/*Instruction has not been processed.*/
	IL_CODE_FLAG_NOT_PROCESSED  = 0,
	/*Instruction was decoded by mono_method_verify loop.*/
	IL_CODE_FLAG_SEEN = 1,
	/*Instruction was target of a branch or is at a protected block boundary.*/
	IL_CODE_FLAG_WAS_TARGET = 2,
	/*Used by stack_init to avoid double initialize each entry.*/
	IL_CODE_FLAG_STACK_INITED = 4,
	/*Used by merge_stacks to decide if it should just copy the eval stack.*/
	IL_CODE_STACK_MERGED = 8,
	/*This instruction is part of the delegate construction sequence, it cannot be target of a branch.*/
	IL_CODE_DELEGATE_SEQUENCE = 0x10,
	/*This is a delegate created from a ldftn to a non final virtual method*/
	IL_CODE_LDFTN_DELEGATE_NONFINAL_VIRTUAL = 0x20,
	/*This is a call to a non final virtual method*/
	IL_CODE_CALL_NONFINAL_VIRTUAL = 0x40,
};

typedef enum {
	RESULT_VALID,
	RESULT_UNVERIFIABLE,
	RESULT_INVALID
} verify_result_t;

typedef struct {
	MonoType *type;
	int stype;
	MonoMethod *method;
} ILStackDesc;


typedef struct {
	ILStackDesc *stack;
	guint16 size, max_size;
	guint16 flags;
} ILCodeDesc;

typedef struct {
	int max_args;
	int max_stack;
	int verifiable;
	int valid;
	int level;

	int code_size;
	ILCodeDesc *code;
	ILCodeDesc eval;

	MonoType **params;
	GSList *list;
	/*Allocated fnptr MonoType that should be freed by us.*/
	GSList *funptrs;
	/*Type dup'ed exception types from catch blocks.*/
	GSList *exception_types;

	int num_locals;
	MonoType **locals;
	char *locals_verification_state;

	/*TODO get rid of target here, need_merge in mono_method_verify and hoist the merging code in the branching code*/
	int target;

	guint32 ip_offset;
	MonoMethodSignature *signature;
	MonoMethodHeader *header;

	MonoGenericContext *generic_context;
	MonoImage *image;
	MonoMethod *method;

	/*This flag helps solving a corner case of delegate verification in that you cannot have a "starg 0" 
	 *on a method that creates a delegate for a non-final virtual method using ldftn*/
	gboolean has_this_store;

	/*This flag is used to control if the contructor of the parent class has been called.
	 *If the this pointer is pushed on the eval stack and it's a reference type constructor and
	 * super_ctor_called is false, the uninitialized flag is set on the pushed value.
	 * 
	 * Poping an uninitialized this ptr from the eval stack is an unverifiable operation unless
	 * the safe variant is used. Only a few opcodes can use it : dup, pop, ldfld, stfld and call to a constructor.
	 */
	gboolean super_ctor_called;

	guint32 prefix_set;
	gboolean has_flags;
	MonoType *constrained_type;
} VerifyContext;

static void
merge_stacks (VerifyContext *ctx, ILCodeDesc *from, ILCodeDesc *to, gboolean start, gboolean external);

static int
get_stack_type (MonoType *type);

static gboolean
mono_delegate_signature_equal (MonoMethodSignature *delegate_sig, MonoMethodSignature *method_sig, gboolean is_static_ldftn);

static gboolean
mono_class_is_valid_generic_instantiation (VerifyContext *ctx, MonoClass *klass);

static gboolean
mono_method_is_valid_generic_instantiation (VerifyContext *ctx, MonoMethod *method);

static MonoGenericParam*
verifier_get_generic_param_from_type (VerifyContext *ctx, MonoType *type);

static gboolean
verifier_class_is_assignable_from (MonoClass *target, MonoClass *candidate);
//////////////////////////////////////////////////////////////////



enum {
	TYPE_INV = 0, /* leave at 0. */
	TYPE_I4  = 1,
	TYPE_I8  = 2,
	TYPE_NATIVE_INT = 3,
	TYPE_R8  = 4,
	/* Used by operator tables to resolve pointer types (managed & unmanaged) and by unmanaged pointer types*/
	TYPE_PTR  = 5,
	/* value types and classes */
	TYPE_COMPLEX = 6,
	/* Number of types, used to define the size of the tables*/
	TYPE_MAX = 6,

	/* Used by tables to signal that a result is not verifiable*/
	NON_VERIFIABLE_RESULT = 0x80,

	/*Mask used to extract just the type, excluding flags */
	TYPE_MASK = 0x0F,

	/* The stack type is a managed pointer, unmask the value to res */
	POINTER_MASK = 0x100,
	
	/*Stack type with the pointer mask*/
	RAW_TYPE_MASK = 0x10F,

	/* Controlled Mutability Manager Pointer */
	CMMP_MASK = 0x200,

	/* The stack type is a null literal*/
	NULL_LITERAL_MASK = 0x400,
	
	/**Used by ldarg.0 and family to let delegate verification happens.*/
	THIS_POINTER_MASK = 0x800,

	/**Signals that this is a boxed value type*/
	BOXED_MASK = 0x1000,

	/*This is an unitialized this ref*/
	UNINIT_THIS_MASK = 0x2000,

	/* This is a safe to return byref */
	SAFE_BYREF_MASK = 0x4000,
};

static const char* const
type_names [TYPE_MAX + 1] = {
	"Invalid",
	"Int32",
	"Int64",
	"Native Int",
	"Float64",
	"Native Pointer",
	"Complex"	
};

enum {
	PREFIX_UNALIGNED = 1,
	PREFIX_VOLATILE  = 2,
	PREFIX_TAIL      = 4,
	PREFIX_CONSTRAINED = 8,
	PREFIX_READONLY = 16
};
//////////////////////////////////////////////////////////////////

#ifdef ENABLE_VERIFIER_STATS

#define _MEM_ALLOC(amt) do { allocated_memory += (amt); working_set += (amt); } while (0)
#define _MEM_FREE(amt) do { working_set -= (amt); } while (0)

static int allocated_memory;
static int working_set;
static int max_allocated_memory;
static int max_working_set;
static int total_allocated_memory;

static void
finish_collect_stats (void)
{
	max_allocated_memory = MAX (max_allocated_memory, allocated_memory);
	max_working_set = MAX (max_working_set, working_set);
	total_allocated_memory += allocated_memory;
	allocated_memory = working_set = 0;
}

static void
init_verifier_stats (void)
{
	static gboolean inited;
	if (!inited) {
		inited = TRUE;
		mono_counters_register ("Maximum memory allocated during verification", MONO_COUNTER_METADATA | MONO_COUNTER_INT, &max_allocated_memory);
		mono_counters_register ("Maximum memory used during verification", MONO_COUNTER_METADATA | MONO_COUNTER_INT, &max_working_set);
		mono_counters_register ("Total memory allocated for verification", MONO_COUNTER_METADATA | MONO_COUNTER_INT, &total_allocated_memory);
	}
}

#else

#define _MEM_ALLOC(amt) do {} while (0)
#define _MEM_FREE(amt) do { } while (0)

#define finish_collect_stats()
#define init_verifier_stats()

#endif


//////////////////////////////////////////////////////////////////

/*
 * Verify if @token refers to a valid row on int's table.
 */
static gboolean
token_bounds_check (MonoImage *image, guint32 token)
{
	if (image_is_dynamic (image))
		return mono_dynamic_image_is_valid_token ((MonoDynamicImage*)image, token);
	return image->tables [mono_metadata_token_table (token)].rows >= mono_metadata_token_index (token) && mono_metadata_token_index (token) > 0;
}

static MonoType *
mono_type_create_fnptr_from_mono_method (VerifyContext *ctx, MonoMethod *method)
{
	MonoType *res = g_new0 (MonoType, 1);
	_MEM_ALLOC (sizeof (MonoType));

	//FIXME use mono_method_get_signature_full
	res->data.method = mono_method_signature_internal (method);
	res->type = MONO_TYPE_FNPTR;
	ctx->funptrs = g_slist_prepend (ctx->funptrs, res);
	return res;
}

/*
 * mono_type_is_enum_type:
 * 
 * Returns: TRUE if @type is an enum type. 
 */
static gboolean
mono_type_is_enum_type (MonoType *type)
{
	if (type->type == MONO_TYPE_VALUETYPE && m_class_is_enumtype (type->data.klass))
		return TRUE;
	if (type->type == MONO_TYPE_GENERICINST && m_class_is_enumtype (type->data.generic_class->container_class))
		return TRUE;
	return FALSE;
}

/*
 * mono_type_is_value_type:
 * 
 * Returns: TRUE if @type is named after @namespace.@name.
 * 
 */
static gboolean
mono_type_is_value_type (MonoType *type, const char *namespace_, const char *name)
{
	return type->type == MONO_TYPE_VALUETYPE &&
		!strcmp (namespace_, m_class_get_name_space (type->data.klass)) &&
		!strcmp (name, m_class_get_name (type->data.klass));
}

/*
 * mono_type_get_underlying_type_any:
 * 
 * This functions is just like mono_type_get_underlying_type but it doesn't care if the type is byref.
 * 
 * Returns the underlying type of @type regardless if it is byref or not.
 */
static MonoType*
mono_type_get_underlying_type_any (MonoType *type)
{
	if (type->type == MONO_TYPE_VALUETYPE && m_class_is_enumtype (type->data.klass))
		return mono_class_enum_basetype_internal (type->data.klass);
	if (type->type == MONO_TYPE_GENERICINST && m_class_is_enumtype (type->data.generic_class->container_class))
		return mono_class_enum_basetype_internal (type->data.generic_class->container_class);
	return type;
}

static G_GNUC_UNUSED const char*
mono_type_get_stack_name (MonoType *type)
{
	return type_names [get_stack_type (type) & TYPE_MASK];
}

/*
 * Verify if @type is valid for the given @ctx verification context.
 * this function checks for VAR and MVAR types that are invalid under the current verifier,
 */
static gboolean
mono_type_is_valid_type_in_context_full (MonoType *type, MonoGenericContext *context, gboolean check_gtd)
{
	int i;
	MonoGenericInst *inst;

	switch (type->type) {
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR:
		if (!context)
			return FALSE;
		inst = type->type == MONO_TYPE_VAR ? context->class_inst : context->method_inst;
		if (!inst || mono_type_get_generic_param_num (type) >= inst->type_argc)
			return FALSE;
		break;
	case MONO_TYPE_SZARRAY:
		return mono_type_is_valid_type_in_context_full (m_class_get_byval_arg (type->data.klass), context, check_gtd);
	case MONO_TYPE_ARRAY:
		return mono_type_is_valid_type_in_context_full (m_class_get_byval_arg (type->data.array->eklass), context, check_gtd);
	case MONO_TYPE_PTR:
		return mono_type_is_valid_type_in_context_full (type->data.type, context, check_gtd);
	case MONO_TYPE_GENERICINST:
		inst = type->data.generic_class->context.class_inst;
		if (!inst->is_open)
			break;
		for (i = 0; i < inst->type_argc; ++i)
			if (!mono_type_is_valid_type_in_context_full (inst->type_argv [i], context, check_gtd))
				return FALSE;
		break;
	case MONO_TYPE_CLASS:
	case MONO_TYPE_VALUETYPE: {
		MonoClass *klass = type->data.klass;
		MonoType *klass_byval_arg = m_class_get_byval_arg (klass);
		/*
		 * It's possible to encode generic'sh types in such a way that they disguise themselves as class or valuetype.
		 * Fixing the type decoding is really tricky since under some cases this behavior is needed, for example, to
		 * have a 'class' type pointing to a 'genericinst' class.
		 *
		 * For the runtime these non canonical (weird) encodings work fine, the worst they can cause is some
		 * reflection oddities which are harmless  - to security at least.
		 */
		if (klass_byval_arg->type != type->type)
			return mono_type_is_valid_type_in_context_full (klass_byval_arg, context, check_gtd);

		if (check_gtd && mono_class_is_gtd (klass))
			return FALSE;
		break;
	}
	default:
		break;
	}
	return TRUE;
}

static gboolean
mono_type_is_valid_type_in_context (MonoType *type, MonoGenericContext *context)
{
	return mono_type_is_valid_type_in_context_full (type, context, FALSE);
}

/*This function returns NULL if the type is not instantiatable*/
static MonoType*
verifier_inflate_type (VerifyContext *ctx, MonoType *type, MonoGenericContext *context)
{
	ERROR_DECL (error);
	MonoType *result;

	result = mono_class_inflate_generic_type_checked (type, context, error);
	if (!is_ok (error)) {
		mono_error_cleanup (error);
		return NULL;
	}
	return result;
}

/**
 * mono_generic_param_is_constraint_compatible:
 *
 * \returns TRUE if \p candidate is constraint compatible with \p target.
 * 
 * This means that \p candidate constraints are a super set of \p target constaints
 */
static gboolean
mono_generic_param_is_constraint_compatible (VerifyContext *ctx, MonoGenericParam *target, MonoGenericParam *candidate, MonoClass *candidate_param_class, MonoGenericContext *context)
{
	MonoGenericParamInfo *tinfo = mono_generic_param_info (target);
	MonoGenericParamInfo *cinfo = mono_generic_param_info (candidate);
	MonoClass **candidate_class;
	gboolean class_constraint_satisfied = FALSE;
	gboolean valuetype_constraint_satisfied = FALSE;

	int tmask = tinfo->flags & GENERIC_PARAMETER_ATTRIBUTE_SPECIAL_CONSTRAINTS_MASK;
	int cmask = cinfo->flags & GENERIC_PARAMETER_ATTRIBUTE_SPECIAL_CONSTRAINTS_MASK;

	if (cinfo->constraints) {
		for (candidate_class = cinfo->constraints; *candidate_class; ++candidate_class) {
			MonoClass *cc;
			MonoType *inflated = verifier_inflate_type (ctx, m_class_get_byval_arg (*candidate_class), ctx->generic_context);
			if (!inflated)
				return FALSE;
			cc = mono_class_from_mono_type_internal (inflated);
			mono_metadata_free_type (inflated);

			if (mono_type_is_reference (m_class_get_byval_arg (cc)) && !MONO_CLASS_IS_INTERFACE_INTERNAL (cc))
				class_constraint_satisfied = TRUE;
			else if (!mono_type_is_reference (m_class_get_byval_arg (cc)) && !MONO_CLASS_IS_INTERFACE_INTERNAL (cc))
				valuetype_constraint_satisfied = TRUE;
		}
	}
	class_constraint_satisfied |= (cmask & GENERIC_PARAMETER_ATTRIBUTE_REFERENCE_TYPE_CONSTRAINT) != 0;
	valuetype_constraint_satisfied |= (cmask & GENERIC_PARAMETER_ATTRIBUTE_VALUE_TYPE_CONSTRAINT) != 0;

	if ((tmask & GENERIC_PARAMETER_ATTRIBUTE_REFERENCE_TYPE_CONSTRAINT) && !class_constraint_satisfied)
		return FALSE;
	if ((tmask & GENERIC_PARAMETER_ATTRIBUTE_VALUE_TYPE_CONSTRAINT) && !valuetype_constraint_satisfied)
		return FALSE;
	if ((tmask & GENERIC_PARAMETER_ATTRIBUTE_CONSTRUCTOR_CONSTRAINT) && !((cmask & GENERIC_PARAMETER_ATTRIBUTE_CONSTRUCTOR_CONSTRAINT) ||
		valuetype_constraint_satisfied)) {
		return FALSE;
	}


	if (tinfo->constraints) {
		MonoClass **target_class;
		for (target_class = tinfo->constraints; *target_class; ++target_class) {
			MonoClass *tc;
			MonoType *inflated = verifier_inflate_type (ctx, m_class_get_byval_arg (*target_class), context);
			if (!inflated)
				return FALSE;
			tc = mono_class_from_mono_type_internal (inflated);
			mono_metadata_free_type (inflated);

			/*
			 * A constraint from @target might inflate into @candidate itself and in that case we don't need
			 * check it's constraints since it satisfy the constraint by itself.
			 */
			if (mono_metadata_type_equal (m_class_get_byval_arg (tc), m_class_get_byval_arg (candidate_param_class)))
				continue;

			if (!cinfo->constraints)
				return FALSE;

			for (candidate_class = cinfo->constraints; *candidate_class; ++candidate_class) {
				MonoClass *cc;
				inflated = verifier_inflate_type (ctx, m_class_get_byval_arg (*candidate_class), ctx->generic_context);
				if (!inflated)
					return FALSE;
				cc = mono_class_from_mono_type_internal (inflated);
				mono_metadata_free_type (inflated);

				if (verifier_class_is_assignable_from (tc, cc))
					break;

				/*
				 * This happens when we have the following:
				 *
				 * Bar<K> where K : IFace
				 * Foo<T, U> where T : U where U : IFace
				 * 	...
				 * 	Bar<T> <- T here satisfy K constraint transitively through to U's constraint
				 *
				 */
				if (mono_type_is_generic_argument (m_class_get_byval_arg (cc))) {
					MonoGenericParam *other_candidate = verifier_get_generic_param_from_type (ctx, m_class_get_byval_arg (cc));

					if (mono_generic_param_is_constraint_compatible (ctx, target, other_candidate, cc, context)) {
						break;
					}
				}
			}
			if (!*candidate_class)
				return FALSE;
		}
	}
	return TRUE;
}

static MonoGenericParam*
verifier_get_generic_param_from_type (VerifyContext *ctx, MonoType *type)
{
	MonoGenericContainer *gc;
	MonoMethod *method = ctx->method;
	int num;

	num = mono_type_get_generic_param_num (type);

	if (type->type == MONO_TYPE_VAR) {
		MonoClass *gtd = method->klass;
		if (mono_class_is_ginst (gtd))
			gtd = mono_class_get_generic_class (gtd)->container_class;
		gc = mono_class_try_get_generic_container (gtd);
	} else { //MVAR
		MonoMethod *gmd = method;
		if (method->is_inflated)
			gmd = ((MonoMethodInflated*)method)->declaring;
		gc = mono_method_get_generic_container (gmd);
	}
	if (!gc)
		return NULL;
	return mono_generic_container_get_param (gc, num);
}



/*
 * Verify if @type is valid for the given @ctx verification context.
 * this function checks for VAR and MVAR types that are invalid under the current verifier,
 * This means that it either 
 */
static gboolean
is_valid_type_in_context (VerifyContext *ctx, MonoType *type)
{
	return mono_type_is_valid_type_in_context (type, ctx->generic_context);
}

static gboolean
is_valid_generic_instantiation_in_context (VerifyContext *ctx, MonoGenericInst *ginst, gboolean check_gtd)
{
	int i;
	for (i = 0; i < ginst->type_argc; ++i) {
		MonoType *type = ginst->type_argv [i];
			
		if (!mono_type_is_valid_type_in_context_full (type, ctx->generic_context, TRUE))
			return FALSE;
	}
	return TRUE;
}

static gboolean
generic_arguments_respect_constraints (VerifyContext *ctx, MonoGenericContainer *gc, MonoGenericContext *context, MonoGenericInst *ginst)
{
	int i;
	for (i = 0; i < ginst->type_argc; ++i) {
		MonoType *type = ginst->type_argv [i];
		MonoGenericParam *target = mono_generic_container_get_param (gc, i);
		MonoGenericParam *candidate;
		MonoClass *candidate_class;

		if (!mono_type_is_generic_argument (type))
			continue;

		if (!is_valid_type_in_context (ctx, type))
			return FALSE;

		candidate = verifier_get_generic_param_from_type (ctx, type);
		candidate_class = mono_class_from_mono_type_internal (type);

		if (!mono_generic_param_is_constraint_compatible (ctx, target, candidate, candidate_class, context))
			return FALSE;
	}
	return TRUE;
}

static gboolean
mono_method_repect_method_constraints (VerifyContext *ctx, MonoMethod *method)
{
	MonoMethodInflated *gmethod = (MonoMethodInflated *)method;
	MonoGenericInst *ginst = gmethod->context.method_inst;
	MonoGenericContainer *gc = mono_method_get_generic_container (gmethod->declaring);
	return !gc || generic_arguments_respect_constraints (ctx, gc, &gmethod->context, ginst);
}

static gboolean
mono_class_repect_method_constraints (VerifyContext *ctx, MonoClass *klass)
{
	MonoGenericClass *gklass = mono_class_get_generic_class (klass);
	MonoGenericInst *ginst = gklass->context.class_inst;
	MonoGenericContainer *gc = mono_class_get_generic_container (gklass->container_class);
	return !gc || generic_arguments_respect_constraints (ctx, gc, &gklass->context, ginst);
}

static gboolean
mono_method_is_valid_generic_instantiation (VerifyContext *ctx, MonoMethod *method)
{
	MonoMethodInflated *gmethod = (MonoMethodInflated *)method;
	MonoGenericInst *ginst = gmethod->context.method_inst;
	MonoGenericContainer *gc = mono_method_get_generic_container (gmethod->declaring);
	if (!gc) /*non-generic inflated method - it's part of a generic type  */
		return TRUE;
	if (ctx && !is_valid_generic_instantiation_in_context (ctx, ginst, TRUE))
		return FALSE;
	return is_valid_generic_instantiation (gc, &gmethod->context, ginst);
}

static gboolean
mono_class_is_valid_generic_instantiation (VerifyContext *ctx, MonoClass *klass)
{
	MonoGenericClass *gklass = mono_class_get_generic_class (klass);
	MonoGenericInst *ginst = gklass->context.class_inst;
	MonoGenericContainer *gc = mono_class_get_generic_container (gklass->container_class);
	if (ctx && !is_valid_generic_instantiation_in_context (ctx, ginst, TRUE))
		return FALSE;
	return is_valid_generic_instantiation (gc, &gklass->context, ginst);
}

static gboolean
mono_type_is_valid_in_context (VerifyContext *ctx, MonoType *type)
{
	MonoClass *klass;

	if (type == NULL) {
		ADD_VERIFY_ERROR2 (ctx, g_strdup_printf ("Invalid null type at 0x%04x", ctx->ip_offset), MONO_EXCEPTION_BAD_IMAGE);
		return FALSE;
	}

	if (!is_valid_type_in_context (ctx, type)) {
		char *str = mono_type_full_name (type);
		ADD_VERIFY_ERROR2 (ctx, g_strdup_printf ("Invalid generic type (%s%s) (argument out of range or %s is not generic) at 0x%04x",
			str [0] == '!' ? "" : type->type == MONO_TYPE_VAR ? "!" : "!!",
			str,
			type->type == MONO_TYPE_VAR ? "class" : "method",
			ctx->ip_offset),
			MONO_EXCEPTION_BAD_IMAGE);		
		g_free (str);
		return FALSE;
	}

	klass = mono_class_from_mono_type_internal (type);
	mono_class_init_internal (klass);
	if (mono_class_has_failure (klass)) {
		if (mono_class_is_ginst (klass) && !mono_class_is_valid_generic_instantiation (NULL, klass))
			ADD_VERIFY_ERROR2 (ctx, g_strdup_printf ("Invalid generic instantiation of type %s.%s at 0x%04x", m_class_get_name_space (klass), m_class_get_name (klass), ctx->ip_offset), MONO_EXCEPTION_TYPE_LOAD);
		else
			ADD_VERIFY_ERROR2 (ctx, g_strdup_printf ("Could not load type %s.%s at 0x%04x", m_class_get_name_space (klass), m_class_get_name (klass), ctx->ip_offset), MONO_EXCEPTION_TYPE_LOAD);
		return FALSE;
	}

	if (mono_class_is_ginst (klass) && mono_class_has_failure (mono_class_get_generic_class (klass)->container_class)) {
		ADD_VERIFY_ERROR2 (ctx, g_strdup_printf ("Could not load type %s.%s at 0x%04x", m_class_get_name_space (klass), m_class_get_name (klass), ctx->ip_offset), MONO_EXCEPTION_TYPE_LOAD);
		return FALSE;
	}

	if (!mono_class_is_ginst (klass))
		return TRUE;

	if (!mono_class_is_valid_generic_instantiation (ctx, klass)) {
		ADD_VERIFY_ERROR2 (ctx, g_strdup_printf ("Invalid generic type instantiation of type %s.%s at 0x%04x", m_class_get_name_space (klass), m_class_get_name (klass), ctx->ip_offset), MONO_EXCEPTION_TYPE_LOAD);
		return FALSE;
	}

	if (!mono_class_repect_method_constraints (ctx, klass)) {
		ADD_VERIFY_ERROR2 (ctx, g_strdup_printf ("Invalid generic type instantiation of type %s.%s (generic args don't respect target's constraints) at 0x%04x", m_class_get_name_space (klass), m_class_get_name (klass), ctx->ip_offset), MONO_EXCEPTION_TYPE_LOAD);
		return FALSE;
	}

	return TRUE;
}

static verify_result_t
mono_method_is_valid_in_context (VerifyContext *ctx, MonoMethod *method)
{
	if (!mono_type_is_valid_in_context (ctx, m_class_get_byval_arg (method->klass)))
		return RESULT_INVALID;

	if (!method->is_inflated)
		return RESULT_VALID;

	if (!mono_method_is_valid_generic_instantiation (ctx, method)) {
		ADD_VERIFY_ERROR2 (ctx, g_strdup_printf ("Invalid generic method instantiation of method %s.%s::%s at 0x%04x", m_class_get_name_space (method->klass), m_class_get_name (method->klass), method->name, ctx->ip_offset), MONO_EXCEPTION_UNVERIFIABLE_IL);
		return RESULT_INVALID;
	}

	if (!mono_method_repect_method_constraints (ctx, method)) {
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid generic method instantiation of method %s.%s::%s (generic args don't respect target's constraints) at 0x%04x", m_class_get_name_space (method->klass), m_class_get_name (method->klass), method->name, ctx->ip_offset));
		return RESULT_UNVERIFIABLE;
	}
	return RESULT_VALID;
}

	
static MonoClassField*
verifier_load_field (VerifyContext *ctx, int token, MonoClass **out_klass, const char *opcode) {
	ERROR_DECL (error);
	MonoClassField *field;
	MonoClass *klass = NULL;

	if (ctx->method->wrapper_type != MONO_WRAPPER_NONE) {
		field = (MonoClassField *)mono_method_get_wrapper_data (ctx->method, (guint32)token);
		klass = field ? field->parent : NULL;
	} else {
		if (!IS_FIELD_DEF_OR_REF (token) || !token_bounds_check (ctx->image, token)) {
			ADD_VERIFY_ERROR2 (ctx, g_strdup_printf ("Invalid field token 0x%08x for %s at 0x%04x", token, opcode, ctx->ip_offset), MONO_EXCEPTION_BAD_IMAGE);
			return NULL;
		}

		field = mono_field_from_token_checked (ctx->image, token, &klass, ctx->generic_context, error);
		mono_error_cleanup (error); /*FIXME don't swallow the error */
	}

	if (!field || !field->parent || !klass) {
		ADD_VERIFY_ERROR2 (ctx, g_strdup_printf ("Cannot load field from token 0x%08x for %s at 0x%04x", token, opcode, ctx->ip_offset), MONO_EXCEPTION_BAD_IMAGE);
		return NULL;
	}

	if (!mono_type_is_valid_in_context (ctx, m_class_get_byval_arg (klass)))
		return NULL;

	if (mono_field_get_flags (field) & FIELD_ATTRIBUTE_LITERAL) {
		char *type_name = mono_type_get_full_name (field->parent);
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Cannot reference literal field %s::%s at 0x%04x", type_name, field->name, ctx->ip_offset));
		g_free (type_name);
		return NULL;
	}

	*out_klass = klass;
	return field;
}

static MonoMethod*
verifier_load_method (VerifyContext *ctx, int token, const char *opcode) {
	MonoMethod* method;


	if (ctx->method->wrapper_type != MONO_WRAPPER_NONE) {
		method = (MonoMethod *)mono_method_get_wrapper_data (ctx->method, (guint32)token);
	} else {
		ERROR_DECL (error);
		if (!IS_METHOD_DEF_OR_REF_OR_SPEC (token) || !token_bounds_check (ctx->image, token)) {
			ADD_VERIFY_ERROR2 (ctx, g_strdup_printf ("Invalid method token 0x%08x for %s at 0x%04x", token, opcode, ctx->ip_offset), MONO_EXCEPTION_BAD_IMAGE);
			return NULL;
		}

		method = mono_get_method_checked (ctx->image, token, NULL, ctx->generic_context, error);
		mono_error_cleanup (error); /* FIXME don't swallow this error */
	}

	if (!method) {
		ADD_VERIFY_ERROR2 (ctx, g_strdup_printf ("Cannot load method from token 0x%08x for %s at 0x%04x", token, opcode, ctx->ip_offset), MONO_EXCEPTION_BAD_IMAGE);
		return NULL;
	}
	
	if (mono_method_is_valid_in_context (ctx, method) == RESULT_INVALID)
		return NULL;

	return method;
}

static MonoType*
verifier_load_type (VerifyContext *ctx, int token, const char *opcode) {
	MonoType* type;
	
	if (ctx->method->wrapper_type != MONO_WRAPPER_NONE) {
		MonoClass *klass = (MonoClass *)mono_method_get_wrapper_data (ctx->method, (guint32)token);
		type = klass ? m_class_get_byval_arg (klass) : NULL;
	} else {
		ERROR_DECL (error);
		if (!IS_TYPE_DEF_OR_REF_OR_SPEC (token) || !token_bounds_check (ctx->image, token)) {
			ADD_VERIFY_ERROR2 (ctx, g_strdup_printf ("Invalid type token 0x%08x at 0x%04x", token, ctx->ip_offset), MONO_EXCEPTION_BAD_IMAGE);
			return NULL;
		}
		type = mono_type_get_checked (ctx->image, token, ctx->generic_context, error);
		mono_error_cleanup (error); /*FIXME don't swallow the error */
	}

	if (!type) {
		ADD_VERIFY_ERROR2 (ctx, g_strdup_printf ("Cannot load type from token 0x%08x for %s at 0x%04x", token, opcode, ctx->ip_offset), MONO_EXCEPTION_BAD_IMAGE);
		return NULL;
	}

	if (!mono_type_is_valid_in_context (ctx, type))
		return NULL;

	return type;
}


/* stack_slot_get_type:
 * 
 * Returns the stack type of @value. This value includes POINTER_MASK.
 * 
 * Use this function to checks that account for a managed pointer.
 */
static gint32
stack_slot_get_type (ILStackDesc *value)
{
	return value->stype & RAW_TYPE_MASK;
}

/* stack_slot_get_underlying_type:
 * 
 * Returns the stack type of @value. This value does not include POINTER_MASK.
 * 
 * Use this function is cases where the fact that the value could be a managed pointer is
 * irrelevant. For example, field load doesn't care about this fact of type on stack.
 */
static gint32
stack_slot_get_underlying_type (ILStackDesc *value)
{
	return value->stype & TYPE_MASK;
}

/* stack_slot_is_managed_pointer:
 * 
 * Returns TRUE is @value is a managed pointer.
 */
static gboolean
stack_slot_is_managed_pointer (ILStackDesc *value)
{
	return (value->stype & POINTER_MASK) == POINTER_MASK;
}

/* stack_slot_is_managed_mutability_pointer:
 * 
 * Returns TRUE is @value is a managed mutability pointer.
 */
static G_GNUC_UNUSED gboolean
stack_slot_is_managed_mutability_pointer (ILStackDesc *value)
{
	return (value->stype & CMMP_MASK) == CMMP_MASK;
}

/* stack_slot_is_null_literal:
 * 
 * Returns TRUE is @value is the null literal.
 */
static gboolean
stack_slot_is_null_literal (ILStackDesc *value)
{
	return (value->stype & NULL_LITERAL_MASK) == NULL_LITERAL_MASK;
}


/* stack_slot_is_this_pointer:
 * 
 * Returns TRUE is @value is the this literal
 */
static gboolean
stack_slot_is_this_pointer (ILStackDesc *value)
{
	return (value->stype & THIS_POINTER_MASK) == THIS_POINTER_MASK;
}

/* stack_slot_is_boxed_value:
 * 
 * Returns TRUE is @value is a boxed value
 */
static gboolean
stack_slot_is_boxed_value (ILStackDesc *value)
{
	return (value->stype & BOXED_MASK) == BOXED_MASK;
}

/* stack_slot_is_safe_byref:
 *
 * Returns TRUE is @value is a safe byref
 */
static gboolean
stack_slot_is_safe_byref (ILStackDesc *value)
{
	return (value->stype & SAFE_BYREF_MASK) == SAFE_BYREF_MASK;
}

static const char *
stack_slot_get_name (ILStackDesc *value)
{
	return type_names [value->stype & TYPE_MASK];
}

enum {
	SAFE_BYREF_LOCAL = 1,
	UNSAFE_BYREF_LOCAL = 2
};
static gboolean
local_is_safe_byref (VerifyContext *ctx, unsigned int arg)
{
	return ctx->locals_verification_state [arg] == SAFE_BYREF_LOCAL;
}

static gboolean
local_is_unsafe_byref (VerifyContext *ctx, unsigned int arg)
{
	return ctx->locals_verification_state [arg] == UNSAFE_BYREF_LOCAL;
}

#define APPEND_WITH_PREDICATE(PRED,NAME) do {\
	if (PRED (value)) { \
		if (!first) \
			g_string_append (str, ", "); \
		g_string_append (str, NAME); \
		first = FALSE; \
	} } while (0)

static char*
stack_slot_stack_type_full_name (ILStackDesc *value)
{
	GString *str = g_string_new ("");
	char *result;
	gboolean has_pred = FALSE, first = TRUE;

	if ((value->stype & TYPE_MASK) != value->stype) {
		g_string_append(str, "[");
		APPEND_WITH_PREDICATE (stack_slot_is_this_pointer, "this");
		APPEND_WITH_PREDICATE (stack_slot_is_boxed_value, "boxed");
		APPEND_WITH_PREDICATE (stack_slot_is_null_literal, "null");
		APPEND_WITH_PREDICATE (stack_slot_is_managed_mutability_pointer, "cmmp");
		APPEND_WITH_PREDICATE (stack_slot_is_managed_pointer, "mp");
		APPEND_WITH_PREDICATE (stack_slot_is_safe_byref, "safe-byref");
		has_pred = TRUE;
	}

	if (mono_type_is_generic_argument (value->type) && !stack_slot_is_boxed_value (value)) {
		if (!has_pred)
			g_string_append(str, "[");
		if (!first)
			g_string_append (str, ", ");
		g_string_append (str, "unboxed");
		has_pred = TRUE;
	}

	if (has_pred)
		g_string_append(str, "] ");

	g_string_append (str, stack_slot_get_name (value));
	result = str->str;
	g_string_free (str, FALSE);
	return result;
}

static char*
stack_slot_full_name (ILStackDesc *value)
{
	char *type_name = mono_type_full_name (value->type);
	char *stack_name = stack_slot_stack_type_full_name (value);
	char *res = g_strdup_printf ("%s (%s)", type_name, stack_name);
	g_free (type_name);
	g_free (stack_name);
	return res;
}

//////////////////////////////////////////////////////////////////

/**
 * mono_free_verify_list:
 */
void
mono_free_verify_list (GSList *list)
{
	MonoVerifyInfoExtended *info;
	GSList *tmp;

	for (tmp = list; tmp; tmp = tmp->next) {
		info = (MonoVerifyInfoExtended *)tmp->data;
		g_free (info->info.message);
		g_free (info);
	}
	g_slist_free (list);
}

#define ADD_ERROR(list,msg)	\
	do {	\
		MonoVerifyInfoExtended *vinfo = g_new (MonoVerifyInfoExtended, 1);	\
		vinfo->info.status = MONO_VERIFY_ERROR;	\
		vinfo->info.message = (msg);	\
		(list) = g_slist_prepend ((list), vinfo);	\
	} while (0)

#define ADD_WARN(list,code,msg)	\
	do {	\
		MonoVerifyInfoExtended *vinfo = g_new (MonoVerifyInfoExtended, 1);	\
		vinfo->info.status = (code);	\
		vinfo->info.message = (msg);	\
		(list) = g_slist_prepend ((list), vinfo);	\
	} while (0)

#define ADD_INVALID(list,msg)	\
	do {	\
		MonoVerifyInfoExtended *vinfo = g_new (MonoVerifyInfoExtended, 1);	\
		vinfo->status = MONO_VERIFY_ERROR;	\
		vinfo->message = (msg);	\
		(list) = g_slist_prepend ((list), vinfo);	\
		/*G_BREAKPOINT ();*/	\
		goto invalid_cil;	\
	} while (0)

#define CHECK_STACK_UNDERFLOW(num)	\
	do {	\
		if (cur_stack < (num))	\
			ADD_INVALID (list, g_strdup_printf ("Stack underflow at 0x%04x (%d items instead of %d)", ip_offset, cur_stack, (num)));	\
	} while (0)

#define CHECK_STACK_OVERFLOW()	\
	do {	\
		if (cur_stack >= max_stack)	\
			ADD_INVALID (list, g_strdup_printf ("Maxstack exceeded at 0x%04x", ip_offset));	\
	} while (0)


static int
in_any_block (MonoMethodHeader *header, guint offset)
{
	int i;
	MonoExceptionClause *clause;

	for (i = 0; i < header->num_clauses; ++i) {
		clause = &header->clauses [i];
		if (MONO_OFFSET_IN_CLAUSE (clause, offset))
			return 1;
		if (MONO_OFFSET_IN_HANDLER (clause, offset))
			return 1;
		if (MONO_OFFSET_IN_FILTER (clause, offset))
			return 1;
	}
	return 0;
}

/*
 * in_any_exception_block:
 * 
 * Returns TRUE is @offset is part of any exception clause (filter, handler, catch, finally or fault).
 */
static gboolean
in_any_exception_block (MonoMethodHeader *header, guint offset)
{
	int i;
	MonoExceptionClause *clause;

	for (i = 0; i < header->num_clauses; ++i) {
		clause = &header->clauses [i];
		if (MONO_OFFSET_IN_HANDLER (clause, offset))
			return TRUE;
		if (MONO_OFFSET_IN_FILTER (clause, offset))
			return TRUE;
	}
	return FALSE;
}

/*
 * is_valid_branch_instruction:
 *
 * Verify if it's valid to perform a branch from @offset to @target.
 * This should be used with br and brtrue/false.
 * It returns 0 if valid, 1 for unverifiable and 2 for invalid.
 * The major difference from other similiar functions is that branching into a
 * finally/fault block is invalid instead of just unverifiable.  
 */
static int
is_valid_branch_instruction (MonoMethodHeader *header, guint offset, guint target)
{
	int i;
	MonoExceptionClause *clause;

	for (i = 0; i < header->num_clauses; ++i) {
		clause = &header->clauses [i];
		/*branching into a finally block is invalid*/
		if ((clause->flags == MONO_EXCEPTION_CLAUSE_FINALLY || clause->flags == MONO_EXCEPTION_CLAUSE_FAULT) &&
			!MONO_OFFSET_IN_HANDLER (clause, offset) &&
			MONO_OFFSET_IN_HANDLER (clause, target))
			return 2;

		if (clause->try_offset != target && (MONO_OFFSET_IN_CLAUSE (clause, offset) ^ MONO_OFFSET_IN_CLAUSE (clause, target)))
			return 1;
		if (MONO_OFFSET_IN_HANDLER (clause, offset) ^ MONO_OFFSET_IN_HANDLER (clause, target))
			return 1;
		if (MONO_OFFSET_IN_FILTER (clause, offset) ^ MONO_OFFSET_IN_FILTER (clause, target))
			return 1;
	}
	return 0;
}

/*
 * is_valid_cmp_branch_instruction:
 * 
 * Verify if it's valid to perform a branch from @offset to @target.
 * This should be used with binary comparison branching instruction, like beq, bge and similars.
 * It returns 0 if valid, 1 for unverifiable and 2 for invalid.
 * 
 * The major differences from other similar functions are that most errors lead to invalid
 * code and only branching out of finally, filter or fault clauses is unverifiable. 
 */
static int
is_valid_cmp_branch_instruction (MonoMethodHeader *header, guint offset, guint target)
{
	int i;
	MonoExceptionClause *clause;

	for (i = 0; i < header->num_clauses; ++i) {
		clause = &header->clauses [i];
		/*branching out of a handler or finally*/
		if (clause->flags != MONO_EXCEPTION_CLAUSE_NONE &&
			MONO_OFFSET_IN_HANDLER (clause, offset) &&
			!MONO_OFFSET_IN_HANDLER (clause, target))
			return 1;

		if (clause->try_offset != target && (MONO_OFFSET_IN_CLAUSE (clause, offset) ^ MONO_OFFSET_IN_CLAUSE (clause, target)))
			return 2;
		if (MONO_OFFSET_IN_HANDLER (clause, offset) ^ MONO_OFFSET_IN_HANDLER (clause, target))
			return 2;
		if (MONO_OFFSET_IN_FILTER (clause, offset) ^ MONO_OFFSET_IN_FILTER (clause, target))
			return 2;
	}
	return 0;
}

/*
 * A leave can't escape a finally block 
 */
static int
is_correct_leave (MonoMethodHeader *header, guint offset, guint target)
{
	int i;
	MonoExceptionClause *clause;

	for (i = 0; i < header->num_clauses; ++i) {
		clause = &header->clauses [i];
		if (clause->flags == MONO_EXCEPTION_CLAUSE_FINALLY && MONO_OFFSET_IN_HANDLER (clause, offset) && !MONO_OFFSET_IN_HANDLER (clause, target))
			return 0;
		if (MONO_OFFSET_IN_FILTER (clause, offset))
			return 0;
	}
	return 1;
}

/*
 * A rethrow can't happen outside of a catch handler.
 */
static int
is_correct_rethrow (MonoMethodHeader *header, guint offset)
{
	int i;
	MonoExceptionClause *clause;

	for (i = 0; i < header->num_clauses; ++i) {
		clause = &header->clauses [i];
		if (MONO_OFFSET_IN_HANDLER (clause, offset))
			return 1;
	}
	return 0;
}

/*
 * An endfinally can't happen outside of a finally/fault handler.
 */
static int
is_correct_endfinally (MonoMethodHeader *header, guint offset)
{
	int i;
	MonoExceptionClause *clause;

	for (i = 0; i < header->num_clauses; ++i) {
		clause = &header->clauses [i];
		if (MONO_OFFSET_IN_HANDLER (clause, offset) && (clause->flags == MONO_EXCEPTION_CLAUSE_FAULT || clause->flags == MONO_EXCEPTION_CLAUSE_FINALLY))
			return 1;
	}
	return 0;
}


/*
 * An endfilter can only happens inside a filter clause.
 * In non-strict mode filter is allowed inside the handler clause too
 */
static MonoExceptionClause *
is_correct_endfilter (VerifyContext *ctx, guint offset)
{
	int i;
	MonoExceptionClause *clause;

	for (i = 0; i < ctx->header->num_clauses; ++i) {
		clause = &ctx->header->clauses [i];
		if (clause->flags != MONO_EXCEPTION_CLAUSE_FILTER)
			continue;
		if (MONO_OFFSET_IN_FILTER (clause, offset))
			return clause;
		if (!IS_STRICT_MODE (ctx) && MONO_OFFSET_IN_HANDLER (clause, offset))
			return clause;
	}
	return NULL;
}


/*
 * Non-strict endfilter can happens inside a try block or any handler block
 */
static int
is_unverifiable_endfilter (VerifyContext *ctx, guint offset)
{
	int i;
	MonoExceptionClause *clause;

	for (i = 0; i < ctx->header->num_clauses; ++i) {
		clause = &ctx->header->clauses [i];
		if (MONO_OFFSET_IN_CLAUSE (clause, offset))
			return 1;
	}
	return 0;
}

static gboolean
is_valid_bool_arg (ILStackDesc *arg)
{
	if (stack_slot_is_managed_pointer (arg) || stack_slot_is_boxed_value (arg) || stack_slot_is_null_literal (arg))
		return TRUE;


	switch (stack_slot_get_underlying_type (arg)) {
	case TYPE_I4:
	case TYPE_I8:
	case TYPE_NATIVE_INT:
	case TYPE_PTR:
		return TRUE;
	case TYPE_COMPLEX:
		g_assert (arg->type);
		switch (arg->type->type) {
		case MONO_TYPE_CLASS:
		case MONO_TYPE_STRING:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_FNPTR:
		case MONO_TYPE_PTR:
			return TRUE;
		case MONO_TYPE_GENERICINST:
			/*We need to check if the container class
			 * of the generic type is a valuetype, iow:
			 * is it a "class Foo<T>" or a "struct Foo<T>"?
			 */
			return !m_class_is_valuetype (arg->type->data.generic_class->container_class);
		default:
			return FALSE;
		}
	default:
		return FALSE;
	}
}


/*Type manipulation helper*/

/*Returns the byref version of the supplied MonoType*/
static MonoType*
mono_type_get_type_byref (MonoType *type)
{
	if (type->byref)
		return type;
	return m_class_get_this_arg (mono_class_from_mono_type_internal (type));
}


/*Returns the byval version of the supplied MonoType*/
static MonoType*
mono_type_get_type_byval (MonoType *type)
{
	if (!type->byref)
		return type;
	return m_class_get_byval_arg (mono_class_from_mono_type_internal (type));
}

static MonoType*
mono_type_from_stack_slot (ILStackDesc *slot)
{
	if (stack_slot_is_managed_pointer (slot))
		return mono_type_get_type_byref (slot->type);
	return slot->type;
}

/*Stack manipulation code*/

static void
ensure_stack_size (ILCodeDesc *stack, int required)
{
	int new_size = 8;
	ILStackDesc *tmp;

	if (required < stack->max_size)
		return;

	/* We don't have to worry about the exponential growth since stack_copy prune unused space */
	new_size = MAX (8, MAX (required, stack->max_size * 2));

	g_assert (new_size >= stack->size);
	g_assert (new_size >= required);

	tmp = g_new0 (ILStackDesc, new_size);
	_MEM_ALLOC (sizeof (ILStackDesc) * new_size);

	if (stack->stack) {
		if (stack->size)
			memcpy (tmp, stack->stack, stack->size * sizeof (ILStackDesc));
		g_free (stack->stack);
		_MEM_FREE (sizeof (ILStackDesc) * stack->max_size);
	}

	stack->stack = tmp;
	stack->max_size = new_size;
}

static void
stack_init (VerifyContext *ctx, ILCodeDesc *state) 
{
	if (state->flags & IL_CODE_FLAG_STACK_INITED)
		return;
	state->size = state->max_size = 0;
	state->flags |= IL_CODE_FLAG_STACK_INITED;
}

static void
stack_copy (ILCodeDesc *to, ILCodeDesc *from)
{
	ensure_stack_size (to, from->size);
	to->size = from->size;

	/*stack copy happens at merge points, which have small stacks*/
	if (from->size)
		memcpy (to->stack, from->stack, sizeof (ILStackDesc) * from->size);
}

static void
copy_stack_value (ILStackDesc *to, ILStackDesc *from)
{
	to->stype = from->stype;
	to->type = from->type;
	to->method = from->method;
}

static int
check_underflow (VerifyContext *ctx, int size)
{
	if (ctx->eval.size < size) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Stack underflow, required %d, but have %d at 0x%04x", size, ctx->eval.size, ctx->ip_offset));
		return 0;
	}
	return 1;
}

static int
check_overflow (VerifyContext *ctx)
{
	if (ctx->eval.size >= ctx->max_stack) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Method doesn't have stack-depth %d at 0x%04x", ctx->eval.size + 1, ctx->ip_offset));
		return 0;
	}
	return 1;
}

/*This reject out PTR, FNPTR and TYPEDBYREF*/
static gboolean
check_unmanaged_pointer (VerifyContext *ctx, ILStackDesc *value)
{
	if (stack_slot_get_type (value) == TYPE_PTR) {
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Unmanaged pointer is not a verifiable type at 0x%04x", ctx->ip_offset));
		return 0;
	}
	return 1;
}

/*TODO verify if MONO_TYPE_TYPEDBYREF is not allowed here as well.*/
static gboolean
check_unverifiable_type (VerifyContext *ctx, MonoType *type)
{
	if (type->type == MONO_TYPE_PTR || type->type == MONO_TYPE_FNPTR) {
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Unmanaged pointer is not a verifiable type at 0x%04x", ctx->ip_offset));
		return 0;
	}
	return 1;
}

static ILStackDesc *
stack_push (VerifyContext *ctx)
{
	g_assert (ctx->eval.size < ctx->max_stack);
	g_assert (ctx->eval.size <= ctx->eval.max_size);

	ensure_stack_size (&ctx->eval, ctx->eval.size + 1);

	return & ctx->eval.stack [ctx->eval.size++];
}

static ILStackDesc *
stack_push_val (VerifyContext *ctx, int stype, MonoType *type)
{
	ILStackDesc *top = stack_push (ctx);
	top->stype = stype;
	top->type = type;
	return top;
}

static ILStackDesc *
stack_pop (VerifyContext *ctx)
{
	ILStackDesc *ret;
	g_assert (ctx->eval.size > 0);	
	ret = ctx->eval.stack + --ctx->eval.size;
	if ((ret->stype & UNINIT_THIS_MASK) == UNINIT_THIS_MASK)
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Found use of uninitialized 'this ptr' ref at 0x%04x", ctx->ip_offset));
	return ret;
}

/* This function allows to safely pop an unititialized this ptr from
 * the eval stack without marking the method as unverifiable. 
 */
static ILStackDesc *
stack_pop_safe (VerifyContext *ctx)
{
	g_assert (ctx->eval.size > 0);
	return ctx->eval.stack + --ctx->eval.size;
}

/*Positive number distance from stack top. [0] is stack top, [1] is the one below*/
static ILStackDesc*
stack_peek (VerifyContext *ctx, int distance)
{
	g_assert (ctx->eval.size - distance > 0);
	return ctx->eval.stack + (ctx->eval.size - 1 - distance);
}

static ILStackDesc *
stack_push_stack_val (VerifyContext *ctx, ILStackDesc *value)
{
	ILStackDesc *top = stack_push (ctx);
	copy_stack_value (top, value);
	return top;
}

/* Returns the MonoType associated with the token, or NULL if it is invalid.
 * 
 * A boxable type can be either a reference or value type, but cannot be a byref type or an unmanaged pointer   
 * */
static MonoType*
get_boxable_mono_type (VerifyContext* ctx, int token, const char *opcode)
{
	MonoType *type;
	MonoClass *klass;

	if (!(type = verifier_load_type (ctx, token, opcode)))
		return NULL;

	if (type->byref && type->type != MONO_TYPE_TYPEDBYREF) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Invalid use of byref type for %s at 0x%04x", opcode, ctx->ip_offset));
		return NULL;
	}

	if (type->type == MONO_TYPE_VOID) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Invalid use of void type for %s at 0x%04x", opcode, ctx->ip_offset));
		return NULL;
	}

	if (type->type == MONO_TYPE_TYPEDBYREF)
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid use of typedbyref for %s at 0x%04x", opcode, ctx->ip_offset));

	if (!(klass = mono_class_from_mono_type_internal (type)))
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Could not retrieve type token for %s at 0x%04x", opcode, ctx->ip_offset));

	if (mono_class_is_gtd (klass) && type->type != MONO_TYPE_GENERICINST)
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Cannot use the generic type definition in a boxable type position for %s at 0x%04x", opcode, ctx->ip_offset));	

	check_unverifiable_type (ctx, type);
	return type;
}


/*operation result tables */

static const unsigned char bin_op_table [TYPE_MAX][TYPE_MAX] = {
	{TYPE_I4, TYPE_INV, TYPE_NATIVE_INT, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_I8, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_NATIVE_INT, TYPE_INV, TYPE_NATIVE_INT, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_R8, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
};

static const unsigned char add_table [TYPE_MAX][TYPE_MAX] = {
	{TYPE_I4, TYPE_INV, TYPE_NATIVE_INT, TYPE_INV, TYPE_PTR | NON_VERIFIABLE_RESULT, TYPE_INV},
	{TYPE_INV, TYPE_I8, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_NATIVE_INT, TYPE_INV, TYPE_NATIVE_INT, TYPE_INV, TYPE_PTR | NON_VERIFIABLE_RESULT, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_R8, TYPE_INV, TYPE_INV},
	{TYPE_PTR | NON_VERIFIABLE_RESULT, TYPE_INV, TYPE_PTR | NON_VERIFIABLE_RESULT, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
};

static const unsigned char sub_table [TYPE_MAX][TYPE_MAX] = {
	{TYPE_I4, TYPE_INV, TYPE_NATIVE_INT, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_I8, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_NATIVE_INT, TYPE_INV, TYPE_NATIVE_INT, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_R8, TYPE_INV, TYPE_INV},
	{TYPE_PTR | NON_VERIFIABLE_RESULT, TYPE_INV, TYPE_PTR | NON_VERIFIABLE_RESULT, TYPE_INV, TYPE_NATIVE_INT | NON_VERIFIABLE_RESULT, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
};

static const unsigned char int_bin_op_table [TYPE_MAX][TYPE_MAX] = {
	{TYPE_I4, TYPE_INV, TYPE_NATIVE_INT, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_I8, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_NATIVE_INT, TYPE_INV, TYPE_NATIVE_INT, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
};

static const unsigned char shift_op_table [TYPE_MAX][TYPE_MAX] = {
	{TYPE_I4, TYPE_INV, TYPE_I4, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_I8, TYPE_INV, TYPE_I8, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_NATIVE_INT, TYPE_INV, TYPE_NATIVE_INT, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
};

static const unsigned char cmp_br_op [TYPE_MAX][TYPE_MAX] = {
	{TYPE_I4, TYPE_INV, TYPE_I4, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_I4, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_I4, TYPE_INV, TYPE_I4, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_I4, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_I4, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
};

static const unsigned char cmp_br_eq_op [TYPE_MAX][TYPE_MAX] = {
	{TYPE_I4, TYPE_INV, TYPE_I4, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_I4, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_I4, TYPE_INV, TYPE_I4, TYPE_INV, TYPE_I4 | NON_VERIFIABLE_RESULT, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_I4, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_I4 | NON_VERIFIABLE_RESULT, TYPE_INV, TYPE_I4, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_I4},
};

static const unsigned char add_ovf_un_table [TYPE_MAX][TYPE_MAX] = {
	{TYPE_I4, TYPE_INV, TYPE_NATIVE_INT, TYPE_INV, TYPE_PTR | NON_VERIFIABLE_RESULT, TYPE_INV},
	{TYPE_INV, TYPE_I8, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_NATIVE_INT, TYPE_INV, TYPE_NATIVE_INT, TYPE_INV, TYPE_PTR | NON_VERIFIABLE_RESULT, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_PTR | NON_VERIFIABLE_RESULT, TYPE_INV, TYPE_PTR | NON_VERIFIABLE_RESULT, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
};

static const unsigned char sub_ovf_un_table [TYPE_MAX][TYPE_MAX] = {
	{TYPE_I4, TYPE_INV, TYPE_NATIVE_INT, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_I8, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_NATIVE_INT, TYPE_INV, TYPE_NATIVE_INT, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_PTR | NON_VERIFIABLE_RESULT, TYPE_INV, TYPE_PTR | NON_VERIFIABLE_RESULT, TYPE_INV, TYPE_NATIVE_INT | NON_VERIFIABLE_RESULT, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
};

static const unsigned char bin_ovf_table [TYPE_MAX][TYPE_MAX] = {
	{TYPE_I4, TYPE_INV, TYPE_NATIVE_INT, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_I8, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_NATIVE_INT, TYPE_INV, TYPE_NATIVE_INT, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
	{TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV, TYPE_INV},
};

#ifdef MONO_VERIFIER_DEBUG

/*debug helpers */
static void
dump_stack_value (ILStackDesc *value)
{
	printf ("[(%x)(%x)", value->type->type, value->stype);

	if (stack_slot_is_this_pointer (value))
		printf ("[this] ");

	if (stack_slot_is_boxed_value (value))
		printf ("[boxed] ");

	if (stack_slot_is_null_literal (value))
		printf ("[null] ");

	if (stack_slot_is_managed_mutability_pointer (value))
		printf ("Controled Mutability MP: ");

	if (stack_slot_is_managed_pointer (value))
		printf ("Managed Pointer to: ");

	if (stack_slot_is_safe_byref (value))
		printf ("Safe ByRef to: ");

	switch (stack_slot_get_underlying_type (value)) {
		case TYPE_INV:
			printf ("invalid type]"); 
			return;
		case TYPE_I4:
			printf ("int32]"); 
			return;
		case TYPE_I8:
			printf ("int64]"); 
			return;
		case TYPE_NATIVE_INT:
			printf ("native int]"); 
			return;
		case TYPE_R8:
			printf ("float64]"); 
			return;
		case TYPE_PTR:
			printf ("unmanaged pointer]"); 
			return;
		case TYPE_COMPLEX:
			switch (value->type->type) {
			case MONO_TYPE_CLASS:
			case MONO_TYPE_VALUETYPE:
				printf ("complex] (%s)", value->type->data.klass->name);
				return;
			case MONO_TYPE_STRING:
				printf ("complex] (string)");
				return;
			case MONO_TYPE_OBJECT:
				printf ("complex] (object)");
				return;
			case MONO_TYPE_SZARRAY:
				printf ("complex] (%s [])", value->type->data.klass->name);
				return;
			case MONO_TYPE_ARRAY:
				printf ("complex] (%s [%d %d %d])",
					value->type->data.array->eklass->name,
					value->type->data.array->rank,
					value->type->data.array->numsizes,
					value->type->data.array->numlobounds);
				return;
			case MONO_TYPE_GENERICINST:
				printf ("complex] (inst of %s )", value->type->data.generic_class->container_class->name);
				return;
			case MONO_TYPE_VAR:
				printf ("complex] (type generic param !%d - %s) ", value->type->data.generic_param->num, mono_generic_param_name (value->type->data.generic_param));
				return;
			case MONO_TYPE_MVAR:
				printf ("complex] (method generic param !!%d - %s) ", value->type->data.generic_param->num, mono_generic_param_name (value->type->data.generic_param));
				return;
			default: {
				//should be a boxed value 
				char * name = mono_type_full_name (value->type);
				printf ("complex] %s", name);
				g_free (name);
				return;
				}
			}
		default:
			printf ("unknown stack %x type]\n", value->stype);
			g_assert_not_reached ();
	}
}

static void
dump_stack_state (ILCodeDesc *state) 
{
	int i;

	printf ("(%d) ", state->size);
	for (i = 0; i < state->size; ++i)
		dump_stack_value (state->stack + i);
	printf ("\n");
}
#endif

/**
 * is_array_type_compatible:
 *
 * Returns TRUE if candidate array type can be assigned to target.
 *
 * Both parameters MUST be of type MONO_TYPE_ARRAY (target->type == MONO_TYPE_ARRAY)
 */
static gboolean
is_array_type_compatible (MonoType *target, MonoType *candidate)
{
	MonoArrayType *left = target->data.array;
	MonoArrayType *right = candidate->data.array;

	g_assert (target->type == MONO_TYPE_ARRAY);
	g_assert (candidate->type == MONO_TYPE_ARRAY);

	if (left->rank != right->rank)
		return FALSE;

	return verifier_class_is_assignable_from (left->eklass, right->eklass);
}

static int
get_stack_type (MonoType *type)
{
	int mask = 0;
	int type_kind = type->type;
	if (type->byref)
		mask = POINTER_MASK;
	/*TODO handle CMMP_MASK */

handle_enum:
	switch (type_kind) {
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
		return TYPE_I4 | mask;

	case MONO_TYPE_I:
	case MONO_TYPE_U:
		return TYPE_NATIVE_INT | mask;

	/* FIXME: the spec says that you cannot have a pointer to method pointer, do we need to check this here? */ 
	case MONO_TYPE_FNPTR:
	case MONO_TYPE_PTR:
	case MONO_TYPE_TYPEDBYREF:
		return TYPE_PTR | mask;

	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR:

	case MONO_TYPE_CLASS:
	case MONO_TYPE_STRING:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:
		return TYPE_COMPLEX | mask;

	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		return TYPE_I8 | mask;

	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
		return TYPE_R8 | mask;

	case MONO_TYPE_GENERICINST:
	case MONO_TYPE_VALUETYPE:
		if (mono_type_is_enum_type (type)) {
			type = mono_type_get_underlying_type_any (type);
			if (!type)
				return FALSE;
			type_kind = type->type;
			goto handle_enum;
		} else {
			return TYPE_COMPLEX | mask;
		}

	default:
		return TYPE_INV;
	}
}

/* convert MonoType to ILStackDesc format (stype) */
static gboolean
set_stack_value (VerifyContext *ctx, ILStackDesc *stack, MonoType *type, int take_addr)
{
	int mask = 0;
	int type_kind = type->type;

	if (type->byref || take_addr)
		mask = POINTER_MASK;
	/* TODO handle CMMP_MASK */

handle_enum:
	stack->type = type;

	switch (type_kind) {
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
		stack->stype = TYPE_I4 | mask;
		break;
	case MONO_TYPE_I:
	case MONO_TYPE_U:
		stack->stype = TYPE_NATIVE_INT | mask;
		break;

	/*FIXME: Do we need to check if it's a pointer to the method pointer? The spec says it' illegal to have that.*/
	case MONO_TYPE_FNPTR:
	case MONO_TYPE_PTR:
	case MONO_TYPE_TYPEDBYREF:
		stack->stype = TYPE_PTR | mask;
		break;

	case MONO_TYPE_CLASS:
	case MONO_TYPE_STRING:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:

	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR: 
		stack->stype = TYPE_COMPLEX | mask;
		break;
		
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		stack->stype = TYPE_I8 | mask;
		break;
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
		stack->stype = TYPE_R8 | mask;
		break;
	case MONO_TYPE_GENERICINST:
	case MONO_TYPE_VALUETYPE:
		if (mono_type_is_enum_type (type)) {
			MonoType *utype = mono_type_get_underlying_type_any (type);
			if (!utype) {
				ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Could not resolve underlying type of %x at %d", type->type, ctx->ip_offset));
				return FALSE;
			}
			type = utype;
			type_kind = type->type;
			goto handle_enum;
		} else {
			stack->stype = TYPE_COMPLEX | mask;
			break;
		}
	default:
		VERIFIER_DEBUG ( printf ("unknown type 0x%02x in eval stack type\n", type->type); );
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Illegal value set on stack 0x%02x at %d", type->type, ctx->ip_offset));
		return FALSE;
	}
	return TRUE;
}

/* 
 * init_stack_with_value_at_exception_boundary:
 * 
 * Initialize the stack and push a given type.
 * The instruction is marked as been on the exception boundary.
 */
static void
init_stack_with_value_at_exception_boundary (VerifyContext *ctx, ILCodeDesc *code, MonoClass *klass)
{
	ERROR_DECL (error);
	MonoType *type = mono_class_inflate_generic_type_checked (m_class_get_byval_arg (klass), ctx->generic_context, error);

	if (!is_ok (error)) {
		char *name = mono_type_get_full_name (klass);
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Invalid class %s used for exception", name));
		g_free (name);
		mono_error_cleanup (error);
		return;
	}

	if (!ctx->max_stack) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Stack overflow at 0x%04x", ctx->ip_offset));
		return;
	}

	stack_init (ctx, code);
	ensure_stack_size (code, 1);
	set_stack_value (ctx, code->stack, type, FALSE);
	ctx->exception_types = g_slist_prepend (ctx->exception_types, type);
	code->size = 1;
	code->flags |= IL_CODE_FLAG_WAS_TARGET;
	if (mono_type_is_generic_argument (type))
		code->stack->stype |= BOXED_MASK;
}
/* Class lazy loading functions */
static GENERATE_GET_CLASS_WITH_CACHE (ienumerable, "System.Collections.Generic", "IEnumerable`1")
static GENERATE_GET_CLASS_WITH_CACHE (icollection, "System.Collections.Generic", "ICollection`1")
static GENERATE_GET_CLASS_WITH_CACHE (ireadonly_list, "System.Collections.Generic", "IReadOnlyList`1")
static GENERATE_GET_CLASS_WITH_CACHE (ireadonly_collection, "System.Collections.Generic", "IReadOnlyCollection`1")


static MonoClass*
get_ienumerable_class (void)
{
	return mono_class_get_ienumerable_class ();
}

static MonoClass*
get_icollection_class (void)
{
	return mono_class_get_icollection_class ();
}

static MonoClass*
get_ireadonlylist_class (void)
{
	return mono_class_get_ireadonly_list_class ();
}

static MonoClass*
get_ireadonlycollection_class (void)
{
	return mono_class_get_ireadonly_collection_class ();
}

static MonoClass*
inflate_class_one_arg (MonoClass *gtype, MonoClass *arg0)
{
	MonoType *args [1];
	args [0] = m_class_get_byval_arg (arg0);

	return mono_class_bind_generic_parameters (gtype, 1, args, FALSE);
}

static gboolean
verifier_inflate_and_check_compat (MonoClass *target, MonoClass *gtd, MonoClass *arg)
{
	MonoClass *tmp;
	if (!(tmp = inflate_class_one_arg (gtd, arg)))
		return FALSE;
	if (mono_class_is_variant_compatible (target, tmp, TRUE))
		return TRUE;
	return FALSE;
}

static gboolean
verifier_class_is_assignable_from (MonoClass *target, MonoClass *candidate)
{
	MonoClass *iface_gtd;

	if (target == candidate)
		return TRUE;

	if (mono_class_has_variant_generic_params (target)) {
		if (MONO_CLASS_IS_INTERFACE_INTERNAL (target)) {
			if (MONO_CLASS_IS_INTERFACE_INTERNAL (candidate) && mono_class_is_variant_compatible (target, candidate, TRUE))
				return TRUE;

			if (m_class_get_rank (candidate) == 1) {
				MonoClass *candidate_element_class = m_class_get_element_class (candidate);
				if (verifier_inflate_and_check_compat (target, mono_defaults.generic_ilist_class, candidate_element_class))
					return TRUE;
				if (verifier_inflate_and_check_compat (target, get_icollection_class (), candidate_element_class))
					return TRUE;
				if (verifier_inflate_and_check_compat (target, get_ienumerable_class (), candidate_element_class))
					return TRUE;
				if (verifier_inflate_and_check_compat (target, get_ireadonlylist_class (), candidate_element_class))
					return TRUE;
				if (verifier_inflate_and_check_compat (target, get_ireadonlycollection_class (), candidate_element_class))
					return TRUE;
			} else {
				ERROR_DECL (error);
				int i;
				while (candidate && candidate != mono_defaults.object_class) {
					mono_class_setup_interfaces (candidate, error);
					if (!is_ok (error)) {
						mono_error_cleanup (error);
						return FALSE;
					}

					/*klass is a generic variant interface, We need to extract from oklass a list of ifaces which are viable candidates.*/
					guint16 candidate_interface_offsets_count = m_class_get_interface_offsets_count (candidate);
					MonoClass **candidate_interfaces_packed = m_class_get_interfaces_packed (candidate);
					for (i = 0; i < candidate_interface_offsets_count; ++i) {
						MonoClass *iface = candidate_interfaces_packed [i];
						if (mono_class_is_variant_compatible (target, iface, TRUE))
							return TRUE;
					}

					guint16 candidate_interface_count = m_class_get_interface_count (candidate);
					MonoClass **candidate_interfaces = m_class_get_interfaces (candidate);
					for (i = 0; i < candidate_interface_count; ++i) {
						MonoClass *iface = candidate_interfaces [i];
						if (mono_class_is_variant_compatible (target, iface, TRUE))
							return TRUE;
					}
					candidate = m_class_get_parent (candidate);
				}
			}
		} else if (m_class_is_delegate (target)) {
			if (mono_class_is_variant_compatible (target, candidate, TRUE))
				return TRUE;
		}
		return FALSE;
	}

	if (mono_class_is_assignable_from_internal (target, candidate))
		return TRUE;

	if (!MONO_CLASS_IS_INTERFACE_INTERNAL (target) || !mono_class_is_ginst (target) || m_class_get_rank (candidate) != 1)
		return FALSE;

	iface_gtd = mono_class_get_generic_class (target)->container_class;
	if (iface_gtd != mono_defaults.generic_ilist_class && iface_gtd != get_icollection_class () && iface_gtd != get_ienumerable_class ())
		return FALSE;

	target = mono_class_from_mono_type_internal (mono_class_get_generic_class (target)->context.class_inst->type_argv [0]);
	candidate = m_class_get_element_class (candidate);

	return TRUE;
}

/*Verify if type 'candidate' can be stored in type 'target'.
 * 
 * If strict, check for the underlying type and not the verification stack types
 */
static gboolean
verify_type_compatibility_full (VerifyContext *ctx, MonoType *target, MonoType *candidate, gboolean strict)
{
#define IS_ONE_OF3(T, A, B, C) (T == A || T == B || T == C)
#define IS_ONE_OF2(T, A, B) (T == A || T == B)

	MonoType *original_candidate = candidate;
	VERIFIER_DEBUG ( printf ("checking type compatibility %s x %s strict %d\n", mono_type_full_name (target), mono_type_full_name (candidate), strict); );

 	/*only one is byref */
	if (candidate->byref ^ target->byref) {
		/* converting from native int to byref*/
		if (get_stack_type (candidate) == TYPE_NATIVE_INT && target->byref) {
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("using byref native int at 0x%04x", ctx->ip_offset));
			return TRUE;
		}
		return FALSE;
	}
	strict |= target->byref;
	/*From now on we don't care about byref anymore, so it's ok to discard it here*/
	candidate = mono_type_get_underlying_type_any (candidate);

handle_enum:
	switch (target->type) {
	case MONO_TYPE_VOID:
		return candidate->type == MONO_TYPE_VOID;
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
		if (strict)
			return IS_ONE_OF3 (candidate->type, MONO_TYPE_I1, MONO_TYPE_U1, MONO_TYPE_BOOLEAN);
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
		if (strict)
			return IS_ONE_OF3 (candidate->type, MONO_TYPE_I2, MONO_TYPE_U2, MONO_TYPE_CHAR);
	case MONO_TYPE_I4:
	case MONO_TYPE_U4: {
		gboolean is_native_int = IS_ONE_OF2 (candidate->type, MONO_TYPE_I, MONO_TYPE_U);
		gboolean is_int4 = IS_ONE_OF2 (candidate->type, MONO_TYPE_I4, MONO_TYPE_U4);
		if (strict)
			return is_native_int || is_int4;
		return is_native_int || get_stack_type (candidate) == TYPE_I4;
	}

	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		return IS_ONE_OF2 (candidate->type, MONO_TYPE_I8, MONO_TYPE_U8);

	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
		if (strict)
			return candidate->type == target->type;
		return IS_ONE_OF2 (candidate->type, MONO_TYPE_R4, MONO_TYPE_R8);

	case MONO_TYPE_I:
	case MONO_TYPE_U: {
		gboolean is_native_int = IS_ONE_OF2 (candidate->type, MONO_TYPE_I, MONO_TYPE_U);
		gboolean is_int4 = IS_ONE_OF2 (candidate->type, MONO_TYPE_I4, MONO_TYPE_U4);
		if (strict)
			return is_native_int || is_int4;
		return is_native_int || get_stack_type (candidate) == TYPE_I4;
	}

	case MONO_TYPE_PTR:
		if (candidate->type != MONO_TYPE_PTR)
			return FALSE;
		/* check the underlying type */
		return verify_type_compatibility_full (ctx, target->data.type, candidate->data.type, TRUE);

	case MONO_TYPE_FNPTR: {
		MonoMethodSignature *left, *right;
		if (candidate->type != MONO_TYPE_FNPTR)
			return FALSE;

		left = mono_type_get_signature_internal (target);
		right = mono_type_get_signature_internal (candidate);
		return mono_metadata_signature_equal (left, right) && left->call_convention == right->call_convention;
	}

	case MONO_TYPE_GENERICINST: {
		MonoClass *target_klass;
		MonoClass *candidate_klass;
		if (mono_type_is_enum_type (target)) {
			target = mono_type_get_underlying_type_any (target);
			if (!target)
				return FALSE;
			goto handle_enum;
		}
		/*
		 * VAR / MVAR compatibility must be checked by verify_stack_type_compatibility
		 * to take boxing status into account.
		 */
		if (mono_type_is_generic_argument (original_candidate))
			return FALSE;

		target_klass = mono_class_from_mono_type_internal (target);
		candidate_klass = mono_class_from_mono_type_internal (candidate);
		if (mono_class_is_nullable (target_klass)) {
			if (!mono_class_is_nullable (candidate_klass))
				return FALSE;
			return target_klass == candidate_klass;
		}
		return verifier_class_is_assignable_from (target_klass, candidate_klass);
	}

	case MONO_TYPE_STRING:
		return candidate->type == MONO_TYPE_STRING;

	case MONO_TYPE_CLASS:
		/*
		 * VAR / MVAR compatibility must be checked by verify_stack_type_compatibility
		 * to take boxing status into account.
		 */
		if (mono_type_is_generic_argument (original_candidate))
			return FALSE;

		if (candidate->type == MONO_TYPE_VALUETYPE)
			return FALSE;

		/* If candidate is an enum it should return true for System.Enum and supertypes.
		 * That's why here we use the original type and not the underlying type.
		 */ 
		return verifier_class_is_assignable_from (target->data.klass, mono_class_from_mono_type_internal (original_candidate));

	case MONO_TYPE_OBJECT:
		return MONO_TYPE_IS_REFERENCE (candidate);

	case MONO_TYPE_SZARRAY: {
		MonoClass *left;
		MonoClass *right;
		if (candidate->type != MONO_TYPE_SZARRAY)
			return FALSE;

		left = mono_class_from_mono_type_internal (target);
		right = mono_class_from_mono_type_internal (candidate);

		return verifier_class_is_assignable_from (left, right);
	}

	case MONO_TYPE_ARRAY:
		if (candidate->type != MONO_TYPE_ARRAY)
			return FALSE;
		return is_array_type_compatible (target, candidate);

	case MONO_TYPE_TYPEDBYREF:
		return candidate->type == MONO_TYPE_TYPEDBYREF;

	case MONO_TYPE_VALUETYPE: {
		MonoClass *target_klass;
		MonoClass *candidate_klass;

		if (candidate->type == MONO_TYPE_CLASS)
			return FALSE;

		target_klass = mono_class_from_mono_type_internal (target);
		candidate_klass = mono_class_from_mono_type_internal (candidate);
		if (target_klass == candidate_klass)
			return TRUE;
		if (mono_type_is_enum_type (target)) {
			target = mono_type_get_underlying_type_any (target);
			if (!target)
				return FALSE;
			goto handle_enum;
		}
		return FALSE;
	}

	case MONO_TYPE_VAR:
		if (candidate->type != MONO_TYPE_VAR)
			return FALSE;
		return mono_type_get_generic_param_num (candidate) == mono_type_get_generic_param_num (target);

	case MONO_TYPE_MVAR:
		if (candidate->type != MONO_TYPE_MVAR)
			return FALSE;
		return mono_type_get_generic_param_num (candidate) == mono_type_get_generic_param_num (target);

	default:
		VERIFIER_DEBUG ( printf ("unknown store type %d\n", target->type); );
		g_assert_not_reached ();
		return FALSE;
	}
	return 1;
#undef IS_ONE_OF3
#undef IS_ONE_OF2
}

static gboolean
verify_type_compatibility (VerifyContext *ctx, MonoType *target, MonoType *candidate)
{
	return verify_type_compatibility_full (ctx, target, candidate, FALSE);
}

/*
 * Returns the generic param bound to the context been verified.
 * 
 */
static MonoGenericParam*
get_generic_param (VerifyContext *ctx, MonoType *param) 
{
	guint16 param_num = mono_type_get_generic_param_num (param);
	if (param->type == MONO_TYPE_VAR) {
		if (!ctx->generic_context->class_inst || ctx->generic_context->class_inst->type_argc <= param_num) {
			ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Invalid generic type argument %d", param_num));
			return NULL;
		}
		return ctx->generic_context->class_inst->type_argv [param_num]->data.generic_param;
	}
	
	/*param must be a MVAR */
	if (!ctx->generic_context->method_inst || ctx->generic_context->method_inst->type_argc <= param_num) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Invalid generic method argument %d", param_num));
		return NULL;
	}
	return ctx->generic_context->method_inst->type_argv [param_num]->data.generic_param;
	
}

static gboolean
recursive_boxed_constraint_type_check (VerifyContext *ctx, MonoType *type, MonoClass *constraint_class, int recursion_level)
{
	MonoType *constraint_type = m_class_get_byval_arg (constraint_class);
	if (recursion_level <= 0)
		return FALSE;

	if (verify_type_compatibility_full (ctx, type, mono_type_get_type_byval (constraint_type), FALSE))
		return TRUE;

	if (mono_type_is_generic_argument (constraint_type)) {
		MonoGenericParam *param = get_generic_param (ctx, constraint_type);
		MonoClass **klass;
		if (!param)
			return FALSE;
		for (klass = mono_generic_param_info (param)->constraints; klass && *klass; ++klass) {
			if (recursive_boxed_constraint_type_check (ctx, type, *klass, recursion_level - 1))
				return TRUE;
		}
	}
	return FALSE;
}

/** 
 * is_compatible_boxed_valuetype:
 * 
 * Returns: TRUE if @candidate / @stack is a valid boxed valuetype. 
 * 
 * @type The source type. It it tested to be of the proper type.    
 * @candidate type of the boxed valuetype.
 * @stack stack slot of the boxed valuetype, separate from @candidade since one could be changed before calling this function
 * @strict if TRUE candidate must be boxed compatible to the target type
 * 
 */
static gboolean
is_compatible_boxed_valuetype (VerifyContext *ctx, MonoType *type, MonoType *candidate, ILStackDesc *stack, gboolean strict)
{
	if (!stack_slot_is_boxed_value (stack))
		return FALSE;
	if (type->byref || candidate->byref)
		return FALSE;

	if (mono_type_is_generic_argument (candidate)) {
		MonoGenericParam *param = get_generic_param (ctx, candidate);
		MonoClass **klass;
		if (!param)
			return FALSE;

		for (klass = mono_generic_param_info (param)->constraints; klass && *klass; ++klass) {
			/*256 should be enough since there can't be more than 255 generic arguments.*/
			if (recursive_boxed_constraint_type_check (ctx, type, *klass, 256))
				return TRUE;
		}
	}

	if (mono_type_is_generic_argument (type))
		return FALSE;

	if (!strict)
		return TRUE;

	return MONO_TYPE_IS_REFERENCE (type) && verifier_class_is_assignable_from (mono_class_from_mono_type_internal (type), mono_class_from_mono_type_internal (candidate));
}

static int
verify_stack_type_compatibility_full (VerifyContext *ctx, MonoType *type, ILStackDesc *stack, gboolean drop_byref, gboolean valuetype_must_be_boxed)
{
	MonoType *candidate = mono_type_from_stack_slot (stack);
	if (MONO_TYPE_IS_REFERENCE (type) && !type->byref && stack_slot_is_null_literal (stack))
		return TRUE;

	if (is_compatible_boxed_valuetype (ctx, type, candidate, stack, TRUE))
		return TRUE;

	if (valuetype_must_be_boxed && !stack_slot_is_boxed_value (stack) && !MONO_TYPE_IS_REFERENCE (candidate))
		return FALSE;

	if (!valuetype_must_be_boxed && stack_slot_is_boxed_value (stack))
		return FALSE;

	if (drop_byref)
		return verify_type_compatibility_full (ctx, type, mono_type_get_type_byval (candidate), FALSE);

	/* Handle how Roslyn emit fixed statements by encoding it as byref */
	if (type->byref && candidate->byref && (type->type == MONO_TYPE_I) && !mono_type_is_reference (candidate)) {
		if (!IS_STRICT_MODE (ctx))
			return TRUE;
	}

	return verify_type_compatibility_full (ctx, type, candidate, FALSE);
}

static int
verify_stack_type_compatibility (VerifyContext *ctx, MonoType *type, ILStackDesc *stack)
{
	return verify_stack_type_compatibility_full (ctx, type, stack, FALSE, FALSE);
}

static gboolean
mono_delegate_type_equal (MonoType *target, MonoType *candidate)
{
	if (candidate->byref ^ target->byref)
		return FALSE;

	switch (target->type) {
	case MONO_TYPE_VOID:
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_STRING:
	case MONO_TYPE_TYPEDBYREF:
		return candidate->type == target->type;

	case MONO_TYPE_PTR:
		if (candidate->type != MONO_TYPE_PTR)
			return FALSE;
		return mono_delegate_type_equal (target->data.type, candidate->data.type);

	case MONO_TYPE_FNPTR:
		if (candidate->type != MONO_TYPE_FNPTR)
			return FALSE;
		return mono_delegate_signature_equal (mono_type_get_signature_internal (target), mono_type_get_signature_internal (candidate), FALSE);

	case MONO_TYPE_GENERICINST: {
		MonoClass *target_klass;
		MonoClass *candidate_klass;
		target_klass = mono_class_from_mono_type_internal (target);
		candidate_klass = mono_class_from_mono_type_internal (candidate);
		/*FIXME handle nullables and enum*/
		return verifier_class_is_assignable_from (target_klass, candidate_klass);
	}
	case MONO_TYPE_OBJECT:
		return MONO_TYPE_IS_REFERENCE (candidate);

	case MONO_TYPE_CLASS:
		return verifier_class_is_assignable_from(target->data.klass, mono_class_from_mono_type_internal (candidate));

	case MONO_TYPE_SZARRAY:
		if (candidate->type != MONO_TYPE_SZARRAY)
			return FALSE;
		return verifier_class_is_assignable_from (m_class_get_element_class (mono_class_from_mono_type_internal (target)), m_class_get_element_class (mono_class_from_mono_type_internal (candidate)));

	case MONO_TYPE_ARRAY:
		if (candidate->type != MONO_TYPE_ARRAY)
			return FALSE;
		return is_array_type_compatible (target, candidate);

	case MONO_TYPE_VALUETYPE:
		/*FIXME handle nullables and enum*/
		return mono_class_from_mono_type_internal (candidate) == mono_class_from_mono_type_internal (target);

	case MONO_TYPE_VAR:
		return candidate->type == MONO_TYPE_VAR && mono_type_get_generic_param_num (target) == mono_type_get_generic_param_num (candidate);
		return FALSE;

	case MONO_TYPE_MVAR:
		return candidate->type == MONO_TYPE_MVAR && mono_type_get_generic_param_num (target) == mono_type_get_generic_param_num (candidate);
		return FALSE;

	default:
		VERIFIER_DEBUG ( printf ("Unknown type %d. Implement me!\n", target->type); );
		g_assert_not_reached ();
		return FALSE;
	}
}

static gboolean
mono_delegate_param_equal (MonoType *delegate, MonoType *method)
{
	if (mono_metadata_type_equal_full (delegate, method, TRUE))
		return TRUE;

	return mono_delegate_type_equal (method, delegate);
}

static gboolean
mono_delegate_ret_equal (MonoType *delegate, MonoType *method)
{
	if (mono_metadata_type_equal_full (delegate, method, TRUE))
		return TRUE;

	return mono_delegate_type_equal (delegate, method);
}

/*
 * mono_delegate_signature_equal:
 * 
 * Compare two signatures in the way expected by delegates.
 * 
 * This function only exists due to the fact that it should ignore the 'has_this' part of the signature.
 *
 * FIXME can this function be eliminated and proper metadata functionality be used?
 */
static gboolean
mono_delegate_signature_equal (MonoMethodSignature *delegate_sig, MonoMethodSignature *method_sig, gboolean is_static_ldftn)
{
	int i;
	int method_offset = is_static_ldftn ? 1 : 0;

	if (delegate_sig->param_count + method_offset != method_sig->param_count) 
		return FALSE;

	if (delegate_sig->call_convention != method_sig->call_convention)
		return FALSE;

	for (i = 0; i < delegate_sig->param_count; i++) { 
		MonoType *p1 = delegate_sig->params [i];
		MonoType *p2 = method_sig->params [i + method_offset];

		if (!mono_delegate_param_equal (p1, p2))
			return FALSE;
	}

	if (!mono_delegate_ret_equal (delegate_sig->ret, method_sig->ret))
		return FALSE;

	return TRUE;
}

gboolean
mono_verifier_is_signature_compatible (MonoMethodSignature *target, MonoMethodSignature *candidate)
{
	return mono_delegate_signature_equal (target, candidate, FALSE);
}

/* 
 * verify_ldftn_delegate:
 * 
 * Verify properties of ldftn based delegates.
 */
static void
verify_ldftn_delegate (VerifyContext *ctx, MonoClass *delegate, ILStackDesc *value, ILStackDesc *funptr)
{
	MonoMethod *method = funptr->method;

	/*ldftn non-final virtuals only allowed if method is not static,
	 * the object is a this arg (comes from a ldarg.0), and there is no starg.0.
	 * This rules doesn't apply if the object on stack is a boxed valuetype.
	 */
	if ((method->flags & METHOD_ATTRIBUTE_VIRTUAL) && !(method->flags & METHOD_ATTRIBUTE_FINAL) && !mono_class_is_sealed (method->klass) && !stack_slot_is_boxed_value (value)) {
		/*A stdarg 0 must not happen, we fail here only in fail fast mode to avoid double error reports*/
		if (IS_FAIL_FAST_MODE (ctx) && ctx->has_this_store)
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid ldftn with virtual function in method with stdarg 0 at  0x%04x", ctx->ip_offset));

		/*current method must not be static*/
		if (ctx->method->flags & METHOD_ATTRIBUTE_STATIC)
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid ldftn with virtual function at 0x%04x", ctx->ip_offset));

		/*value is the this pointer, loaded using ldarg.0 */
		if (!stack_slot_is_this_pointer (value))
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid object argument, it is not the this pointer, to ldftn with virtual method at  0x%04x", ctx->ip_offset));

		ctx->code [ctx->ip_offset].flags |= IL_CODE_LDFTN_DELEGATE_NONFINAL_VIRTUAL;
	}
}

/*
 * verify_delegate_compatibility:
 * 
 * Verify delegate creation sequence.
 * 
 */
static void
verify_delegate_compatibility (VerifyContext *ctx, MonoClass *delegate, ILStackDesc *value, ILStackDesc *funptr)
{
#define IS_VALID_OPCODE(offset, opcode) (ip [ip_offset - offset] == opcode && (ctx->code [ip_offset - offset].flags & IL_CODE_FLAG_SEEN))
#define IS_LOAD_FUN_PTR(kind) (IS_VALID_OPCODE (6, CEE_PREFIX1) && ip [ip_offset - 5] == kind)

	MonoMethod *invoke, *method;
	const guint8 *ip = ctx->header->code;
	guint32 ip_offset = ctx->ip_offset;
	gboolean is_static_ldftn = FALSE, is_first_arg_bound = FALSE;
	
	if (stack_slot_get_type (funptr) != TYPE_PTR || !funptr->method) {
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid function pointer parameter for delegate constructor at 0x%04x", ctx->ip_offset));
		return;
	}
	
	invoke = mono_get_delegate_invoke_internal (delegate);
	method = funptr->method;

	if (!method || !mono_method_signature_internal (method)) {
		char *name = mono_type_get_full_name (delegate);
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Invalid method on stack to create delegate %s construction at 0x%04x", name, ctx->ip_offset));
		g_free (name);
		return;
	}

	if (!invoke || !mono_method_signature_internal (invoke)) {
		char *name = mono_type_get_full_name (delegate);
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Delegate type %s with bad Invoke method at 0x%04x", name, ctx->ip_offset));
		g_free (name);
		return;
	}

	is_static_ldftn = (ip_offset > 5 && IS_LOAD_FUN_PTR (CEE_LDFTN)) && method->flags & METHOD_ATTRIBUTE_STATIC;

	if (is_static_ldftn)
		is_first_arg_bound = mono_method_signature_internal (invoke)->param_count + 1 ==  mono_method_signature_internal (method)->param_count;

	if (!mono_delegate_signature_equal (mono_method_signature_internal (invoke), mono_method_signature_internal (method), is_first_arg_bound)) {
		char *fun_sig = mono_signature_get_desc (mono_method_signature_internal (method), FALSE);
		char *invoke_sig = mono_signature_get_desc (mono_method_signature_internal (invoke), FALSE);
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Function pointer signature '%s' doesn't match delegate's signature '%s' at 0x%04x", fun_sig, invoke_sig, ctx->ip_offset));
		g_free (fun_sig);
		g_free (invoke_sig);
	}

	/* 
	 * Delegate code sequences:
	 * [-6] ldftn token
	 * newobj ...
	 * 
	 * 
	 * [-7] dup
	 * [-6] ldvirtftn token
	 * newobj ...
	 * 
	 * ldftn sequence:*/
	if (ip_offset > 5 && IS_LOAD_FUN_PTR (CEE_LDFTN)) {
		verify_ldftn_delegate (ctx, delegate, value, funptr);
	} else if (ip_offset > 6 && IS_VALID_OPCODE (7, CEE_DUP) && IS_LOAD_FUN_PTR (CEE_LDVIRTFTN)) {
		ctx->code [ip_offset - 6].flags |= IL_CODE_DELEGATE_SEQUENCE;	
	}else {
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid code sequence for delegate creation at 0x%04x", ctx->ip_offset));
	}
	ctx->code [ip_offset].flags |= IL_CODE_DELEGATE_SEQUENCE;

	//general tests
	if (is_first_arg_bound) {
		if (mono_method_signature_internal (method)->param_count == 0 || !verify_stack_type_compatibility_full (ctx, mono_method_signature_internal (method)->params [0], value, FALSE, TRUE))
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("This object not compatible with function pointer for delegate creation at 0x%04x", ctx->ip_offset));
	} else {
		if (method->flags & METHOD_ATTRIBUTE_STATIC) {
			if (!stack_slot_is_null_literal (value))
				CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Non-null this args used with static function for delegate creation at 0x%04x", ctx->ip_offset));
		} else {
			if (!verify_stack_type_compatibility_full (ctx, m_class_get_byval_arg (method->klass), value, FALSE, TRUE) && !stack_slot_is_null_literal (value))
				CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("This object not compatible with function pointer for delegate creation at 0x%04x", ctx->ip_offset));
		}
	}

	if (stack_slot_get_type (value) != TYPE_COMPLEX)
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid first parameter for delegate creation at 0x%04x", ctx->ip_offset));

#undef IS_VALID_OPCODE
#undef IS_LOAD_FUN_PTR
}

static gboolean
is_this_arg_of_struct_instance_method (unsigned int arg, VerifyContext *ctx)
{
	if (arg != 0)
		return FALSE;
	if (ctx->method->flags & METHOD_ATTRIBUTE_STATIC)
		return FALSE;
	if (!m_class_is_valuetype (ctx->method->klass))
		return FALSE;
	return TRUE;
}

/* implement the opcode checks*/
static void
push_arg (VerifyContext *ctx, unsigned int arg, int take_addr) 
{
	ILStackDesc *top;

	if (arg >= ctx->max_args) {
		if (take_addr) 
			ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Method doesn't have argument %d", arg + 1));
		else {
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Method doesn't have argument %d", arg + 1));
			if (check_overflow (ctx)) //FIXME: what sane value could we ever push?
				stack_push_val (ctx, TYPE_I4, mono_get_int32_type ());
		}
	} else if (check_overflow (ctx)) {
		/*We must let the value be pushed, otherwise we would get an underflow error*/
		check_unverifiable_type (ctx, ctx->params [arg]);
		if (ctx->params [arg]->byref && take_addr)
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("ByRef of ByRef at 0x%04x", ctx->ip_offset));
		top = stack_push (ctx);
		if (!set_stack_value (ctx, top, ctx->params [arg], take_addr))
			return;

		if (arg == 0 && !(ctx->method->flags & METHOD_ATTRIBUTE_STATIC)) {
			if (take_addr)
				ctx->has_this_store = TRUE;
			else
				top->stype |= THIS_POINTER_MASK;
			if (mono_method_is_constructor (ctx->method) && !ctx->super_ctor_called && !m_class_is_valuetype (ctx->method->klass))
				top->stype |= UNINIT_THIS_MASK;
		}
		if (!take_addr && ctx->params [arg]->byref && !is_this_arg_of_struct_instance_method (arg, ctx))
			top->stype |= SAFE_BYREF_MASK;
	} 
}

static void
push_local (VerifyContext *ctx, guint32 arg, int take_addr) 
{
	if (arg >= ctx->num_locals) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Method doesn't have local %d", arg + 1));
	} else if (check_overflow (ctx)) {
		/*We must let the value be pushed, otherwise we would get an underflow error*/
		check_unverifiable_type (ctx, ctx->locals [arg]);
		if (ctx->locals [arg]->byref && take_addr)
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("ByRef of ByRef at 0x%04x", ctx->ip_offset));

		ILStackDesc *value = stack_push (ctx);
		set_stack_value (ctx, value, ctx->locals [arg], take_addr);
		if (local_is_safe_byref (ctx, arg))
			value->stype |= SAFE_BYREF_MASK;
	}
}

static void
store_arg (VerifyContext *ctx, guint32 arg)
{
	ILStackDesc *value;

	if (arg >= ctx->max_args) {
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Method doesn't have argument %d at 0x%04x", arg + 1, ctx->ip_offset));
		if (check_underflow (ctx, 1))
			stack_pop (ctx);
		return;
	}

	if (check_underflow (ctx, 1)) {
		value = stack_pop (ctx);
		if (!verify_stack_type_compatibility (ctx, ctx->params [arg], value)) {
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Incompatible type %s in argument store at 0x%04x", stack_slot_get_name (value), ctx->ip_offset));
		}
	}
	if (arg == 0 && !(ctx->method->flags & METHOD_ATTRIBUTE_STATIC))
		ctx->has_this_store = 1;
}

static void
store_local (VerifyContext *ctx, guint32 arg)
{
	ILStackDesc *value;
	if (arg >= ctx->num_locals) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Method doesn't have local var %d at 0x%04x", arg + 1, ctx->ip_offset));
		return;
	}

	/*TODO verify definite assigment */		
	if (!check_underflow (ctx, 1))
		return;

	value = stack_pop (ctx);
	if (ctx->locals [arg]->byref) {
		if (stack_slot_is_managed_mutability_pointer (value))
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Cannot use a readonly managed reference when storing on a local variable at 0x%04x", ctx->ip_offset));

		if (local_is_safe_byref (ctx, arg) && !stack_slot_is_safe_byref (value))
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Cannot store an unsafe ret byref to a local that was previously stored a save ret byref value at 0x%04x", ctx->ip_offset));

		if (stack_slot_is_safe_byref (value) && !local_is_unsafe_byref (ctx, arg))
			ctx->locals_verification_state [arg] |= SAFE_BYREF_LOCAL;

		if (!stack_slot_is_safe_byref (value))
			ctx->locals_verification_state [arg] |= UNSAFE_BYREF_LOCAL;

	}
	if (!verify_stack_type_compatibility (ctx, ctx->locals [arg], value)) {
		char *expected = mono_type_full_name (ctx->locals [arg]);
		char *found = stack_slot_full_name (value);
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Incompatible type '%s' on stack cannot be stored to local %d with type '%s' at 0x%04x",
				found,
				arg,
				expected,
				ctx->ip_offset));
		g_free (expected);
		g_free (found);	
	}
}

/*FIXME add and sub needs special care here*/
static void
do_binop (VerifyContext *ctx, unsigned int opcode, const unsigned char table [TYPE_MAX][TYPE_MAX])
{
	ILStackDesc *a, *b, *top;
	int idxa, idxb, complexMerge = 0;
	unsigned char res;

	if (!check_underflow (ctx, 2))
		return;
	b = stack_pop (ctx);
	a = stack_pop (ctx);

	idxa = stack_slot_get_underlying_type (a);
	if (stack_slot_is_managed_pointer (a)) {
		idxa = TYPE_PTR;
		complexMerge = 1;
	}

	idxb = stack_slot_get_underlying_type (b);
	if (stack_slot_is_managed_pointer (b)) {
		idxb = TYPE_PTR;
		complexMerge = 2;
	}

	--idxa;
	--idxb;
	res = table [idxa][idxb];

	VERIFIER_DEBUG ( printf ("binop res %d\n", res); );
	VERIFIER_DEBUG ( printf ("idxa %d idxb %d\n", idxa, idxb); );

	top = stack_push (ctx);
	if (res == TYPE_INV) {
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Binary instruction applyed to ill formed stack (%s x %s)", stack_slot_get_name (a), stack_slot_get_name (b)));
		copy_stack_value (top, a);
		return;
	}

 	if (res & NON_VERIFIABLE_RESULT) {
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Binary instruction is not verifiable (%s x %s)", stack_slot_get_name (a), stack_slot_get_name (b)));

		res = res & ~NON_VERIFIABLE_RESULT;
 	}

 	if (complexMerge && res == TYPE_PTR) {
 		if (complexMerge == 1) 
 			copy_stack_value (top, a);
 		else if (complexMerge == 2)
 			copy_stack_value (top, b);
		/*
		 * There is no need to merge the type of two pointers.
		 * The only valid operation is subtraction, that returns a native
		 *  int as result and can be used with any 2 pointer kinds.
		 * This is valid acording to Patition III 1.1.4
		 */
 	} else
 		top->stype = res;
 	
}


static void
do_boolean_branch_op (VerifyContext *ctx, int delta)
{
	int target = ctx->ip_offset + delta;
	ILStackDesc *top;

	VERIFIER_DEBUG ( printf ("boolean branch offset %d delta %d target %d\n", ctx->ip_offset, delta, target); );
 
	if (target < 0 || target >= ctx->code_size) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Boolean branch target out of code at 0x%04x", ctx->ip_offset));
		return;
	}

	switch (is_valid_branch_instruction (ctx->header, ctx->ip_offset, target)) {
	case 1:
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Branch target escapes out of exception block at 0x%04x", ctx->ip_offset));
		break;
	case 2:
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Branch target escapes out of exception block at 0x%04x", ctx->ip_offset));
		return;
	}

	ctx->target = target;

	if (!check_underflow (ctx, 1))
		return;

	top = stack_pop (ctx);
	if (!is_valid_bool_arg (top))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Argument type %s not valid for brtrue/brfalse at 0x%04x", stack_slot_get_name (top), ctx->ip_offset));

	check_unmanaged_pointer (ctx, top);
}

static gboolean
stack_slot_is_complex_type_not_reference_type (ILStackDesc *slot)
{
	return stack_slot_get_type (slot) == TYPE_COMPLEX && !MONO_TYPE_IS_REFERENCE (slot->type) && !stack_slot_is_boxed_value (slot);
}

static gboolean
stack_slot_is_reference_value (ILStackDesc *slot)
{
	return stack_slot_get_type (slot) == TYPE_COMPLEX && (MONO_TYPE_IS_REFERENCE (slot->type) || stack_slot_is_boxed_value (slot));
}

static void
do_branch_op (VerifyContext *ctx, signed int delta, const unsigned char table [TYPE_MAX][TYPE_MAX])
{
	ILStackDesc *a, *b;
	int idxa, idxb;
	unsigned char res;
	int target = ctx->ip_offset + delta;

	VERIFIER_DEBUG ( printf ("branch offset %d delta %d target %d\n", ctx->ip_offset, delta, target); );
 
	if (target < 0 || target >= ctx->code_size) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Branch target out of code at 0x%04x", ctx->ip_offset));
		return;
	}

	switch (is_valid_cmp_branch_instruction (ctx->header, ctx->ip_offset, target)) {
	case 1: /*FIXME use constants and not magic numbers.*/
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Branch target escapes out of exception block at 0x%04x", ctx->ip_offset));
		break;
	case 2:
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Branch target escapes out of exception block at 0x%04x", ctx->ip_offset));
		return;
	}

	ctx->target = target;

	if (!check_underflow (ctx, 2))
		return;

	b = stack_pop (ctx);
	a = stack_pop (ctx);

	idxa = stack_slot_get_underlying_type (a);
	if (stack_slot_is_managed_pointer (a))
		idxa = TYPE_PTR;

	idxb = stack_slot_get_underlying_type (b);
	if (stack_slot_is_managed_pointer (b))
		idxb = TYPE_PTR;

	if (stack_slot_is_complex_type_not_reference_type (a) || stack_slot_is_complex_type_not_reference_type (b)) {
		res = TYPE_INV;
	} else {
		--idxa;
		--idxb;
		res = table [idxa][idxb];
	}

	VERIFIER_DEBUG ( printf ("branch res %d\n", res); );
	VERIFIER_DEBUG ( printf ("idxa %d idxb %d\n", idxa, idxb); );

	if (res == TYPE_INV) {
		CODE_NOT_VERIFIABLE (ctx,
			g_strdup_printf ("Compare and Branch instruction applyed to ill formed stack (%s x %s) at 0x%04x", stack_slot_get_name (a), stack_slot_get_name (b), ctx->ip_offset));
	} else if (res & NON_VERIFIABLE_RESULT) {
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Compare and Branch instruction is not verifiable (%s x %s) at 0x%04x", stack_slot_get_name (a), stack_slot_get_name (b), ctx->ip_offset)); 
 		res = res & ~NON_VERIFIABLE_RESULT;
 	}
}

static void
do_cmp_op (VerifyContext *ctx, const unsigned char table [TYPE_MAX][TYPE_MAX], guint32 opcode)
{
	ILStackDesc *a, *b;
	int idxa, idxb;
	unsigned char res;

	if (!check_underflow (ctx, 2))
		return;
	b = stack_pop (ctx);
	a = stack_pop (ctx);

	if (opcode == CEE_CGT_UN) {
		if ((stack_slot_is_reference_value (a) && stack_slot_is_null_literal (b)) ||
			(stack_slot_is_reference_value (b) && stack_slot_is_null_literal (a))) {
			stack_push_val (ctx, TYPE_I4, mono_get_int32_type ());
			return;
		}
	}

	idxa = stack_slot_get_underlying_type (a);
	if (stack_slot_is_managed_pointer (a))
		idxa = TYPE_PTR;

	idxb = stack_slot_get_underlying_type (b);
	if (stack_slot_is_managed_pointer (b)) 
		idxb = TYPE_PTR;

	if (stack_slot_is_complex_type_not_reference_type (a) || stack_slot_is_complex_type_not_reference_type (b)) {
		res = TYPE_INV;
	} else {
		--idxa;
		--idxb;
		res = table [idxa][idxb];
	}

	if(res == TYPE_INV) {
		char *left_type = stack_slot_full_name (a);
		char *right_type = stack_slot_full_name (b);
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf("Compare instruction applyed to ill formed stack (%s x %s) at 0x%04x", left_type, right_type, ctx->ip_offset));
		g_free (left_type);
		g_free (right_type);
	} else if (res & NON_VERIFIABLE_RESULT) {
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Compare instruction is not verifiable (%s x %s) at 0x%04x", stack_slot_get_name (a), stack_slot_get_name (b), ctx->ip_offset)); 
 		res = res & ~NON_VERIFIABLE_RESULT;
 	}
 	stack_push_val (ctx, TYPE_I4, mono_get_int32_type ());
}

static void
do_ret (VerifyContext *ctx)
{
	MonoType *ret = ctx->signature->ret;
	VERIFIER_DEBUG ( printf ("checking ret\n"); );
	if (ret->type != MONO_TYPE_VOID) {
		ILStackDesc *top;
		if (!check_underflow (ctx, 1))
			return;

		top = stack_pop(ctx);

		if (!verify_stack_type_compatibility (ctx, ctx->signature->ret, top)) {
			char *ret_type = mono_type_full_name (ctx->signature->ret);
			char *stack_type = stack_slot_full_name (top);
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Incompatible return value on stack with method signature, expected '%s' but got '%s' at 0x%04x", ret_type, stack_type, ctx->ip_offset));
			g_free (stack_type);
			g_free (ret_type);
			return;
		}

		if (ret->byref && !stack_slot_is_safe_byref (top))
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Method returns byref and return value is not a safe-to-return-byref at 0x%04x", ctx->ip_offset));

		if (ret->type == MONO_TYPE_TYPEDBYREF || mono_type_is_value_type (ret, "System", "ArgIterator") || mono_type_is_value_type (ret, "System", "RuntimeArgumentHandle"))
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Method returns byref, TypedReference, ArgIterator or RuntimeArgumentHandle at 0x%04x", ctx->ip_offset));
	}

	if (ctx->eval.size > 0) {
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Stack not empty (%d) after ret at 0x%04x", ctx->eval.size, ctx->ip_offset));
	} 
	if (in_any_block (ctx->header, ctx->ip_offset))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("ret cannot escape exception blocks at 0x%04x", ctx->ip_offset));
}

/*
 * FIXME we need to fix the case of a non-virtual instance method defined in the parent but call using a token pointing to a subclass.
 * 	This is illegal but mono_get_method_full decoded it.
 * TODO handle calling .ctor outside one or calling the .ctor for other class but super  
 */
static void
do_invoke_method (VerifyContext *ctx, int method_token, gboolean virtual_)
{
	ERROR_DECL (error);
	int param_count, i;
	MonoMethodSignature *sig;
	ILStackDesc *value;
	MonoMethod *method;
	gboolean virt_check_this = FALSE;
	gboolean constrained = ctx->prefix_set & PREFIX_CONSTRAINED;

	if (!(method = verifier_load_method (ctx, method_token, virtual_ ? "callvirt" : "call")))
		return;

	if (virtual_) {
		CLEAR_PREFIX (ctx, PREFIX_CONSTRAINED);

		if (m_class_is_valuetype (method->klass)) // && !constrained ???
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Cannot use callvirtual with valuetype method at 0x%04x", ctx->ip_offset));

		if ((method->flags & METHOD_ATTRIBUTE_STATIC))
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Cannot use callvirtual with static method at 0x%04x", ctx->ip_offset));

	} else {
		if (method->flags & METHOD_ATTRIBUTE_ABSTRACT) 
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Cannot use call with an abstract method at 0x%04x", ctx->ip_offset));
		
		if ((method->flags & METHOD_ATTRIBUTE_VIRTUAL) && !(method->flags & METHOD_ATTRIBUTE_FINAL) && !mono_class_is_sealed (method->klass)) {
			virt_check_this = TRUE;
			ctx->code [ctx->ip_offset].flags |= IL_CODE_CALL_NONFINAL_VIRTUAL;
		}
	}

	if (!(sig = mono_method_get_signature_checked (method, ctx->image, method_token, ctx->generic_context, error))) {
		mono_error_cleanup (error);
		sig = mono_method_get_signature_checked (method, ctx->image, method_token, NULL, error);
	}

	if (!sig) {
		char *name = mono_type_get_full_name (method->klass);
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Could not resolve signature of %s:%s at 0x%04x due to: %s", name, method->name, ctx->ip_offset, mono_error_get_message (error)));
		mono_error_cleanup (error);
		g_free (name);
		return;
	}

	param_count = sig->param_count + sig->hasthis;
	if (!check_underflow (ctx, param_count))
		return;

	gboolean is_safe_byref_call = TRUE;

	for (i = sig->param_count - 1; i >= 0; --i) {
		VERIFIER_DEBUG ( printf ("verifying argument %d\n", i); );
		value = stack_pop (ctx);
		if (!verify_stack_type_compatibility (ctx, sig->params[i], value)) {
			char *stack_name = stack_slot_full_name (value);
			char *sig_name = mono_type_full_name (sig->params [i]);
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Incompatible parameter with function signature: Calling method with signature (%s) but for argument %d there is a (%s) on stack at 0x%04x", sig_name, i, stack_name, ctx->ip_offset));
			g_free (stack_name);
			g_free (sig_name);
		}

		if (stack_slot_is_managed_mutability_pointer (value))
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Cannot use a readonly pointer as argument of %s at 0x%04x", virtual_ ? "callvirt" : "call",  ctx->ip_offset));

		if ((ctx->prefix_set & PREFIX_TAIL) && stack_slot_is_managed_pointer (value)) {
			ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Cannot  pass a byref argument to a tail %s at 0x%04x", virtual_ ? "callvirt" : "call",  ctx->ip_offset));
			return;
		}
		if (stack_slot_is_managed_pointer (value) && !stack_slot_is_safe_byref (value))
			is_safe_byref_call = FALSE;
	}

	if (sig->hasthis) {
		MonoType *type = m_class_get_byval_arg (method->klass);
		ILStackDesc copy;

		if (mono_method_is_constructor (method) && !m_class_is_valuetype (method->klass)) {
			if (IS_STRICT_MODE (ctx) && !mono_method_is_constructor (ctx->method))
				CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Cannot call a constructor outside one at 0x%04x", ctx->ip_offset));
			if (IS_STRICT_MODE (ctx) && method->klass != m_class_get_parent (ctx->method->klass) && method->klass != ctx->method->klass)
				CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Cannot call a constructor of a type different from this or super at 0x%04x", ctx->ip_offset));

			ctx->super_ctor_called = TRUE;
			value = stack_pop_safe (ctx);
			if (IS_STRICT_MODE (ctx) && (value->stype & THIS_POINTER_MASK) != THIS_POINTER_MASK)
				CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid 'this ptr' argument for constructor at 0x%04x", ctx->ip_offset));
			if (!(value->stype & UNINIT_THIS_MASK))
				CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Calling the base constructor on an initialized this pointer at 0x%04x", ctx->ip_offset));
		} else {
			value = stack_pop (ctx);
		}
			
		copy_stack_value (&copy, value);
		//TODO we should extract this to a 'drop_byref_argument' and use everywhere
		//Other parts of the code suffer from the same issue of 
		copy.type = mono_type_get_type_byval (copy.type);
		copy.stype &= ~POINTER_MASK;

		if (virt_check_this && !stack_slot_is_this_pointer (value) && !(m_class_is_valuetype (method->klass) || stack_slot_is_boxed_value (value)))
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Cannot use the call opcode with a non-final virtual method on an object different than the 'this' pointer at 0x%04x", ctx->ip_offset));

		if (constrained && virtual_) {
			if (!stack_slot_is_managed_pointer (value))
				CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Object is not a managed pointer for a constrained call at 0x%04x", ctx->ip_offset));
			if (!mono_metadata_type_equal_full (mono_type_get_type_byval (value->type), mono_type_get_underlying_type (ctx->constrained_type), TRUE))
				CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Object not compatible with constrained type at 0x%04x", ctx->ip_offset));
			copy.stype |= BOXED_MASK;
			copy.type = ctx->constrained_type;
		} else {
			if (stack_slot_is_managed_pointer (value) && !m_class_is_valuetype (mono_class_from_mono_type_internal (value->type)))
				CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Cannot call a reference type using a managed pointer to the this arg at 0x%04x", ctx->ip_offset));
	
			if (!virtual_ && m_class_is_valuetype (mono_class_from_mono_type_internal (value->type)) && !m_class_is_valuetype (method->klass) && !stack_slot_is_boxed_value (value))
				CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Cannot call a valuetype baseclass at 0x%04x", ctx->ip_offset));
	
			if (virtual_ && m_class_is_valuetype (mono_class_from_mono_type_internal (value->type)) && !stack_slot_is_boxed_value (value))
				CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Cannot use a valuetype with callvirt at 0x%04x", ctx->ip_offset));
	
			if (m_class_is_valuetype (method->klass) && (stack_slot_is_boxed_value (value) || !stack_slot_is_managed_pointer (value)))
				CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Cannot use a boxed or literal valuetype to call a valuetype method at 0x%04x", ctx->ip_offset));
		}
		if (!verify_stack_type_compatibility (ctx, type, &copy)) {
			char *expected = mono_type_full_name (type);
			char *effective = stack_slot_full_name (&copy);
			char *method_name = mono_method_full_name (method, TRUE);
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Incompatible this argument on stack with method signature expected '%s' but got '%s' for a call to '%s' at 0x%04x",
					expected, effective, method_name, ctx->ip_offset));
			g_free (method_name);
			g_free (effective);
			g_free (expected);
		}

		if (!IS_SKIP_VISIBILITY (ctx) && !mono_method_can_access_method_full (ctx->method, method, mono_class_from_mono_type_internal (value->type))) {
			char *name = mono_method_full_name (method, TRUE);
			CODE_NOT_VERIFIABLE2 (ctx, g_strdup_printf ("Method %s is not accessible at 0x%04x", name, ctx->ip_offset), MONO_EXCEPTION_METHOD_ACCESS);
			g_free (name);
		}

	} else if (!IS_SKIP_VISIBILITY (ctx) && !mono_method_can_access_method_full (ctx->method, method, NULL)) {
		char *name = mono_method_full_name (method, TRUE);
		CODE_NOT_VERIFIABLE2 (ctx, g_strdup_printf ("Method %s is not accessible at 0x%04x", name, ctx->ip_offset), MONO_EXCEPTION_METHOD_ACCESS);
		g_free (name);
	}

	if (sig->ret->type != MONO_TYPE_VOID) {
		if (!mono_type_is_valid_in_context (ctx, sig->ret))
			return;

		if (check_overflow (ctx)) {
			value = stack_push (ctx);
			set_stack_value (ctx, value, sig->ret, FALSE);
			if ((ctx->prefix_set & PREFIX_READONLY) && m_class_get_rank (method->klass) && !strcmp (method->name, "Address")) {
				ctx->prefix_set &= ~PREFIX_READONLY;
				value->stype |= CMMP_MASK;
			}
			if (sig->ret->byref && is_safe_byref_call)
				value->stype |= SAFE_BYREF_MASK;
		}
	}

	if ((ctx->prefix_set & PREFIX_TAIL)) {
		if (!mono_delegate_ret_equal (mono_method_signature_internal (ctx->method)->ret, sig->ret))
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Tail call with incompatible return type at 0x%04x", ctx->ip_offset));
		if (ctx->header->code [ctx->ip_offset + 5] != CEE_RET)
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Tail call not followed by ret at 0x%04x", ctx->ip_offset));
	}

}

static void
do_push_static_field (VerifyContext *ctx, int token, gboolean take_addr)
{
	MonoClassField *field;
	MonoClass *klass;
	if (!check_overflow (ctx))
		return;
	if (!take_addr)
		CLEAR_PREFIX (ctx, PREFIX_VOLATILE);

	if (!(field = verifier_load_field (ctx, token, &klass, take_addr ? "ldsflda" : "ldsfld")))
		return;

	if (!(field->type->attrs & FIELD_ATTRIBUTE_STATIC)) { 
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Cannot load non static field at 0x%04x", ctx->ip_offset));
		return;
	}
	/*taking the address of initonly field only works from the static constructor */
	if (take_addr && (field->type->attrs & FIELD_ATTRIBUTE_INIT_ONLY) &&
		!(field->parent == ctx->method->klass && (ctx->method->flags & (METHOD_ATTRIBUTE_SPECIAL_NAME | METHOD_ATTRIBUTE_STATIC)) && !strcmp (".cctor", ctx->method->name)))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Cannot take the address of a init-only field at 0x%04x", ctx->ip_offset));

	if (!IS_SKIP_VISIBILITY (ctx) && !mono_method_can_access_field_full (ctx->method, field, NULL))
		CODE_NOT_VERIFIABLE2 (ctx, g_strdup_printf ("Type at stack is not accessible at 0x%04x", ctx->ip_offset), MONO_EXCEPTION_FIELD_ACCESS);

	ILStackDesc *value = stack_push (ctx);
	set_stack_value (ctx, value, field->type, take_addr);
	if (take_addr)
		value->stype |= SAFE_BYREF_MASK;
}

static void
do_store_static_field (VerifyContext *ctx, int token) {
	MonoClassField *field;
	MonoClass *klass;
	ILStackDesc *value;
	CLEAR_PREFIX (ctx, PREFIX_VOLATILE);

	if (!check_underflow (ctx, 1))
		return;

	value = stack_pop (ctx);

	if (!(field = verifier_load_field (ctx, token, &klass, "stsfld")))
		return;

	if (!(field->type->attrs & FIELD_ATTRIBUTE_STATIC)) { 
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Cannot store non static field at 0x%04x", ctx->ip_offset));
		return;
	}

	if (field->type->type == MONO_TYPE_TYPEDBYREF) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Typedbyref field is an unverfiable type in store static field at 0x%04x", ctx->ip_offset));
		return;
	}

	if (!IS_SKIP_VISIBILITY (ctx) && !mono_method_can_access_field_full (ctx->method, field, NULL))
		CODE_NOT_VERIFIABLE2 (ctx, g_strdup_printf ("Type at stack is not accessible at 0x%04x", ctx->ip_offset), MONO_EXCEPTION_FIELD_ACCESS);

	if (!verify_stack_type_compatibility (ctx, field->type, value)) {
		char *stack_name = stack_slot_full_name (value);
		char *field_name = mono_type_full_name (field->type);
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Incompatible type in static field store expected '%s' but found '%s' at 0x%04x",
				field_name, stack_name, ctx->ip_offset));
		g_free (field_name);
		g_free (stack_name);
	}
}

static gboolean
check_is_valid_type_for_field_ops (VerifyContext *ctx, int token, ILStackDesc *obj, MonoClassField **ret_field, const char *opcode)
{
	MonoClassField *field;
	MonoClass *klass;
	gboolean is_pointer;

	/*must be a reference type, a managed pointer, an unamanaged pointer, or a valuetype*/
	if (!(field = verifier_load_field (ctx, token, &klass, opcode)))
		return FALSE;

	*ret_field = field;
	//the value on stack is going to be used as a pointer
	is_pointer = stack_slot_get_type (obj) == TYPE_PTR || (stack_slot_get_type (obj) == TYPE_NATIVE_INT && !get_stack_type (m_class_get_byval_arg (field->parent)));

	if (field->type->type == MONO_TYPE_TYPEDBYREF) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Typedbyref field is an unverfiable type at 0x%04x", ctx->ip_offset));
		return FALSE;
	}
	g_assert (obj->type);

	/*The value on the stack must be a subclass of the defining type of the field*/ 
	/* we need to check if we can load the field from the stack value*/
	if (is_pointer) {
		if (stack_slot_get_underlying_type (obj) == TYPE_NATIVE_INT)
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Native int is not a verifiable type to reference a field at 0x%04x", ctx->ip_offset));

		if (!IS_SKIP_VISIBILITY (ctx) && !mono_method_can_access_field_full (ctx->method, field, NULL))
				CODE_NOT_VERIFIABLE2 (ctx, g_strdup_printf ("Type at stack is not accessible at 0x%04x", ctx->ip_offset), MONO_EXCEPTION_FIELD_ACCESS);
	} else {
		if (!m_class_is_valuetype (field->parent) && stack_slot_is_managed_pointer (obj))
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Type at stack is a managed pointer to a reference type and is not compatible to reference the field at 0x%04x", ctx->ip_offset));

		/*a value type can be loaded from a value or a managed pointer, but not a boxed object*/
		if (m_class_is_valuetype (field->parent) && stack_slot_is_boxed_value (obj))
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Type at stack is a boxed valuetype and is not compatible to reference the field at 0x%04x", ctx->ip_offset));

		if (!stack_slot_is_null_literal (obj) && !verify_stack_type_compatibility_full (ctx, m_class_get_byval_arg (field->parent), obj, TRUE, FALSE)) {
			char *found = stack_slot_full_name (obj);
			char *expected = mono_type_full_name (m_class_get_byval_arg (field->parent));
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Expected type '%s' but found '%s' referencing the 'this' argument at 0x%04x", expected, found, ctx->ip_offset));
			g_free (found);
			g_free (expected);
		}

		if (!IS_SKIP_VISIBILITY (ctx) && !mono_method_can_access_field_full (ctx->method, field, mono_class_from_mono_type_internal (obj->type)))
			CODE_NOT_VERIFIABLE2 (ctx, g_strdup_printf ("Type at stack is not accessible at 0x%04x", ctx->ip_offset), MONO_EXCEPTION_FIELD_ACCESS);
	} 

	check_unmanaged_pointer (ctx, obj);
	return TRUE;
}

static void
do_push_field (VerifyContext *ctx, int token, gboolean take_addr)
{
	ILStackDesc *obj;
	MonoClassField *field;
	gboolean is_safe_byref = FALSE;

	if (!take_addr)
		CLEAR_PREFIX (ctx, PREFIX_UNALIGNED | PREFIX_VOLATILE);

	if (!check_underflow (ctx, 1))
		return;
	obj = stack_pop_safe (ctx);

	if (!check_is_valid_type_for_field_ops (ctx, token, obj, &field, take_addr ? "ldflda" : "ldfld"))
		return;

	if (take_addr && m_class_is_valuetype (field->parent) && !stack_slot_is_managed_pointer (obj))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Cannot take the address of a temporary value-type at 0x%04x", ctx->ip_offset));

	if (take_addr && (field->type->attrs & FIELD_ATTRIBUTE_INIT_ONLY) &&
		!(field->parent == ctx->method->klass && mono_method_is_constructor (ctx->method)))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Cannot take the address of a init-only field at 0x%04x", ctx->ip_offset));

	//must do it here cuz stack_push will return the same slot as obj above
	is_safe_byref = take_addr && (stack_slot_is_reference_value (obj) || stack_slot_is_safe_byref (obj));

	ILStackDesc *value = stack_push (ctx);
	set_stack_value (ctx, value, field->type, take_addr);

	if (is_safe_byref)
		value->stype |= SAFE_BYREF_MASK;
}

static void
do_store_field (VerifyContext *ctx, int token)
{
	ILStackDesc *value, *obj;
	MonoClassField *field;
	CLEAR_PREFIX (ctx, PREFIX_UNALIGNED | PREFIX_VOLATILE);

	if (!check_underflow (ctx, 2))
		return;

	value = stack_pop (ctx);
	obj = stack_pop_safe (ctx);

	if (!check_is_valid_type_for_field_ops (ctx, token, obj, &field, "stfld"))
		return;

	if (!verify_stack_type_compatibility (ctx, field->type, value))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Incompatible type %s in field store at 0x%04x", stack_slot_get_name (value), ctx->ip_offset));	
}

/*TODO proper handle for Nullable<T>*/
static void
do_box_value (VerifyContext *ctx, int klass_token)
{
	ILStackDesc *value;
	MonoType *type = get_boxable_mono_type (ctx, klass_token, "box");
	MonoClass *klass;	

	if (!type)
		return;

	if (!check_underflow (ctx, 1))
		return;

	value = stack_pop (ctx);
	/*box is a nop for reference types*/

	if (stack_slot_get_underlying_type (value) == TYPE_COMPLEX && MONO_TYPE_IS_REFERENCE (value->type) && MONO_TYPE_IS_REFERENCE (type)) {
		stack_push_stack_val (ctx, value)->stype |= BOXED_MASK;
		return;
	}


	if (!verify_stack_type_compatibility (ctx, type, value))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid type at stack for boxing operation at 0x%04x", ctx->ip_offset));

	klass = mono_class_from_mono_type_internal (type);
	if (mono_class_is_nullable (klass))
		type = m_class_get_byval_arg (mono_class_get_nullable_param_internal (klass));
	stack_push_val (ctx, TYPE_COMPLEX | BOXED_MASK, type);
}

static void
do_unbox_value (VerifyContext *ctx, int klass_token)
{
	ILStackDesc *value;
	MonoType *type = get_boxable_mono_type (ctx, klass_token, "unbox");

	if (!type)
		return;
 
	if (!check_underflow (ctx, 1))
		return;

	if (!m_class_is_valuetype (mono_class_from_mono_type_internal (type)))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid reference type for unbox at 0x%04x", ctx->ip_offset));

	value = stack_pop (ctx);

	/*Value should be: a boxed valuetype or a reference type*/
	if (!(stack_slot_get_type (value) == TYPE_COMPLEX &&
		(stack_slot_is_boxed_value (value) || !m_class_is_valuetype (mono_class_from_mono_type_internal (value->type)))))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid type %s at stack for unbox operation at 0x%04x", stack_slot_get_name (value), ctx->ip_offset));

	set_stack_value (ctx, value = stack_push (ctx), mono_type_get_type_byref (type), FALSE);
	value->stype |= CMMP_MASK;
}

static void
do_unbox_any (VerifyContext *ctx, int klass_token)
{
	ILStackDesc *value;
	MonoType *type = get_boxable_mono_type (ctx, klass_token, "unbox.any");

	if (!type)
		return;
 
	if (!check_underflow (ctx, 1))
		return;

	value = stack_pop (ctx);

	/*Value should be: a boxed valuetype or a reference type*/
	if (!(stack_slot_get_type (value) == TYPE_COMPLEX &&
		(stack_slot_is_boxed_value (value) || !m_class_is_valuetype (mono_class_from_mono_type_internal (value->type)))))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid type %s at stack for unbox.any operation at 0x%04x", stack_slot_get_name (value), ctx->ip_offset));
 
	set_stack_value (ctx, stack_push (ctx), type, FALSE);
}

static void
do_unary_math_op (VerifyContext *ctx, int op)
{
	ILStackDesc *value;
	if (!check_underflow (ctx, 1))
		return;
	value = stack_pop (ctx);
	switch (stack_slot_get_type (value)) {
	case TYPE_I4:
	case TYPE_I8:
	case TYPE_NATIVE_INT:
		break;
	case TYPE_R8:
		if (op == CEE_NEG)
			break;
	case TYPE_COMPLEX: /*only enums are ok*/
		if (mono_type_is_enum_type (value->type))
			break;
	default:
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid type at stack for unary not at 0x%04x", ctx->ip_offset));
	}
	stack_push_stack_val (ctx, value);
}

static void
do_conversion (VerifyContext *ctx, int kind) 
{
	ILStackDesc *value;
	if (!check_underflow (ctx, 1))
		return;
	value = stack_pop (ctx);

	switch (stack_slot_get_type (value)) {
	case TYPE_I4:
	case TYPE_I8:
	case TYPE_NATIVE_INT:
	case TYPE_R8:
		break;
	default:
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid type (%s) at stack for conversion operation. Numeric type expected at 0x%04x", stack_slot_get_name (value), ctx->ip_offset));
	}

	switch (kind) {
	case TYPE_I4:
		stack_push_val (ctx, TYPE_I4, mono_get_int32_type ());
		break;
	case TYPE_I8:
		stack_push_val (ctx,TYPE_I8, m_class_get_byval_arg (mono_defaults.int64_class));
		break;
	case TYPE_R8:
		stack_push_val (ctx, TYPE_R8, m_class_get_byval_arg (mono_defaults.double_class));
		break;
	case TYPE_NATIVE_INT:
		stack_push_val (ctx, TYPE_NATIVE_INT, mono_get_int_type ());
		break;
	default:
		g_error ("unknown type %02x in conversion", kind);

	}
}

static void
do_load_token (VerifyContext *ctx, int token) 
{
	ERROR_DECL (error);
	gpointer handle;
	MonoClass *handle_class;
	if (!check_overflow (ctx))
		return;

	if (ctx->method->wrapper_type != MONO_WRAPPER_NONE) {
		handle = mono_method_get_wrapper_data (ctx->method, token);
		handle_class = (MonoClass *)mono_method_get_wrapper_data (ctx->method, token + 1);
		if (handle_class == mono_defaults.typehandle_class)
			handle = m_class_get_byval_arg ((MonoClass*)handle);
	} else {
		switch (token & 0xff000000) {
		case MONO_TOKEN_TYPE_DEF:
		case MONO_TOKEN_TYPE_REF:
		case MONO_TOKEN_TYPE_SPEC:
		case MONO_TOKEN_FIELD_DEF:
		case MONO_TOKEN_METHOD_DEF:
		case MONO_TOKEN_METHOD_SPEC:
		case MONO_TOKEN_MEMBER_REF:
			if (!token_bounds_check (ctx->image, token)) {
				ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Table index out of range 0x%x for token %x for ldtoken at 0x%04x", mono_metadata_token_index (token), token, ctx->ip_offset));
				return;
			}
			break;
		default:
			ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Invalid table 0x%x for token 0x%x for ldtoken at 0x%04x", mono_metadata_token_table (token), token, ctx->ip_offset));
			return;
		}

		handle = mono_ldtoken_checked (ctx->image, token, &handle_class, ctx->generic_context, error);
	}

	if (!handle) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Invalid token 0x%x for ldtoken at 0x%04x due to %s", token, ctx->ip_offset, mono_error_get_message (error)));
		mono_error_cleanup (error);
		return;
	}
	if (handle_class == mono_defaults.typehandle_class) {
		mono_type_is_valid_in_context (ctx, (MonoType*)handle);
	} else if (handle_class == mono_defaults.methodhandle_class) {
		mono_method_is_valid_in_context (ctx, (MonoMethod*)handle);		
	} else if (handle_class == mono_defaults.fieldhandle_class) {
		mono_type_is_valid_in_context (ctx, m_class_get_byval_arg (((MonoClassField*)handle)->parent));				
	} else {
		ADD_VERIFY_ERROR2 (ctx, g_strdup_printf ("Invalid ldtoken type %x at 0x%04x", token, ctx->ip_offset), MONO_EXCEPTION_BAD_IMAGE);
	}
	stack_push_val (ctx, TYPE_COMPLEX, m_class_get_byval_arg (handle_class));
}

static void
do_ldobj_value (VerifyContext *ctx, int token) 
{
	ILStackDesc *value;
	MonoType *type = get_boxable_mono_type (ctx, token, "ldobj");
	CLEAR_PREFIX (ctx, PREFIX_UNALIGNED | PREFIX_VOLATILE);

	if (!type)
		return;

	if (!check_underflow (ctx, 1))
		return;

	value = stack_pop (ctx);
	if (!stack_slot_is_managed_pointer (value) 
			&& stack_slot_get_type (value) != TYPE_NATIVE_INT
			&& !(stack_slot_get_type (value) == TYPE_PTR && value->type->type != MONO_TYPE_FNPTR)) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Invalid argument %s to ldobj at 0x%04x", stack_slot_get_name (value), ctx->ip_offset));
		return;
	}

	if (stack_slot_get_type (value) == TYPE_NATIVE_INT)
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Using native pointer to ldobj at 0x%04x", ctx->ip_offset));

	/*We have a byval on the stack, but the comparison must be strict. */
	if (!verify_type_compatibility_full (ctx, type, mono_type_get_type_byval (value->type), TRUE))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid type at stack for ldojb operation at 0x%04x", ctx->ip_offset));

	set_stack_value (ctx, stack_push (ctx), type, FALSE);
}

static void
do_stobj (VerifyContext *ctx, int token) 
{
	ILStackDesc *dest, *src;
	MonoType *type = get_boxable_mono_type (ctx, token, "stobj");
	CLEAR_PREFIX (ctx, PREFIX_UNALIGNED | PREFIX_VOLATILE);

	if (!type)
		return;

	if (!check_underflow (ctx, 2))
		return;

	src = stack_pop (ctx);
	dest = stack_pop (ctx);

	if (stack_slot_is_managed_mutability_pointer (dest))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Cannot use a readonly pointer with stobj at 0x%04x", ctx->ip_offset));

	if (!stack_slot_is_managed_pointer (dest)) 
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid destination of stobj operation at 0x%04x", ctx->ip_offset));

	if (stack_slot_is_boxed_value (src) && !MONO_TYPE_IS_REFERENCE (src->type) && !MONO_TYPE_IS_REFERENCE (type))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Cannot use stobj with a boxed source value that is not a reference type at 0x%04x", ctx->ip_offset));

	if (!verify_stack_type_compatibility (ctx, type, src)) {
		char *type_name = mono_type_full_name (type);
		char *src_name = stack_slot_full_name (src);
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Token '%s' and source '%s' of stobj don't match ' at 0x%04x", type_name, src_name, ctx->ip_offset));
		g_free (type_name);
		g_free (src_name);
	}

	if (!verify_type_compatibility (ctx, mono_type_get_type_byval (dest->type), type))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Destination and token types of stobj don't match at 0x%04x", ctx->ip_offset));
}

static void
do_cpobj (VerifyContext *ctx, int token)
{
	ILStackDesc *dest, *src;
	MonoType *type = get_boxable_mono_type (ctx, token, "cpobj");
	if (!type)
		return;

	if (!check_underflow (ctx, 2))
		return;

	src = stack_pop (ctx);
	dest = stack_pop (ctx);

	if (!stack_slot_is_managed_pointer (src)) 
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid source of cpobj operation at 0x%04x", ctx->ip_offset));

	if (!stack_slot_is_managed_pointer (dest)) 
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid destination of cpobj operation at 0x%04x", ctx->ip_offset));

	if (stack_slot_is_managed_mutability_pointer (dest))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Cannot use a readonly pointer with cpobj at 0x%04x", ctx->ip_offset));

	if (!verify_type_compatibility (ctx, type, mono_type_get_type_byval (src->type)))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Token and source types of cpobj don't match at 0x%04x", ctx->ip_offset));

	if (!verify_type_compatibility (ctx, mono_type_get_type_byval (dest->type), type))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Destination and token types of cpobj don't match at 0x%04x", ctx->ip_offset));
}

static void
do_initobj (VerifyContext *ctx, int token)
{
	ILStackDesc *obj;
	MonoType *stack, *type = get_boxable_mono_type (ctx, token, "initobj");
	if (!type)
		return;

	if (!check_underflow (ctx, 1))
		return;

	obj = stack_pop (ctx);

	if (!stack_slot_is_managed_pointer (obj)) 
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid object address for initobj at 0x%04x", ctx->ip_offset));

	if (stack_slot_is_managed_mutability_pointer (obj))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Cannot use a readonly pointer with initobj at 0x%04x", ctx->ip_offset));

	stack = mono_type_get_type_byval (obj->type);
	if (MONO_TYPE_IS_REFERENCE (stack)) {
		if (!verify_type_compatibility (ctx, stack, type)) 
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Type token of initobj not compatible with value on stack at 0x%04x", ctx->ip_offset));
		else if (IS_STRICT_MODE (ctx) && !mono_metadata_type_equal (type, stack)) 
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Type token of initobj not compatible with value on stack at 0x%04x", ctx->ip_offset));
	} else if (!verify_type_compatibility (ctx, stack, type)) {
		char *expected_name = mono_type_full_name (type);
		char *stack_name = mono_type_full_name (stack);

		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Initobj %s not compatible with value on stack %s at 0x%04x", expected_name, stack_name, ctx->ip_offset));
		g_free (expected_name);
		g_free (stack_name);
	}
}

static void
do_newobj (VerifyContext *ctx, int token) 
{
	ILStackDesc *value;
	int i;
	MonoMethodSignature *sig;
	MonoMethod *method;
	gboolean is_delegate = FALSE;

	if (!(method = verifier_load_method (ctx, token, "newobj")))
		return;

	if (!mono_method_is_constructor (method)) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Method from token 0x%08x not a constructor at 0x%04x", token, ctx->ip_offset));
		return;
	}

	if (mono_class_get_flags (method->klass) & (TYPE_ATTRIBUTE_ABSTRACT | TYPE_ATTRIBUTE_INTERFACE))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Trying to instantiate an abstract or interface type at 0x%04x", ctx->ip_offset));

	if (!IS_SKIP_VISIBILITY (ctx) && !mono_method_can_access_method_full (ctx->method, method, NULL)) {
		char *from = mono_method_full_name (ctx->method, TRUE);
		char *to = mono_method_full_name (method, TRUE);
		CODE_NOT_VERIFIABLE2 (ctx, g_strdup_printf ("Constructor %s not visible from %s at 0x%04x", to, from, ctx->ip_offset), MONO_EXCEPTION_METHOD_ACCESS);
		g_free (from);
		g_free (to);
	}

	//FIXME use mono_method_get_signature_full
	sig = mono_method_signature_internal (method);
	if (!sig) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Invalid constructor signature to newobj at 0x%04x", ctx->ip_offset));
		return;
	}

	if (!sig->hasthis) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Invalid constructor signature missing hasthis at 0x%04x", ctx->ip_offset));
		return;
	}

	if (!check_underflow (ctx, sig->param_count))
		return;

	is_delegate = m_class_get_parent (method->klass) == mono_defaults.multicastdelegate_class;

	if (is_delegate) {
		ILStackDesc *funptr;
		//first arg is object, second arg is fun ptr
		if (sig->param_count != 2) {
			ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Invalid delegate constructor at 0x%04x", ctx->ip_offset));
			return;
		}
		funptr = stack_pop (ctx);
		value = stack_pop (ctx);
		verify_delegate_compatibility (ctx, method->klass, value, funptr);
	} else {
		for (i = sig->param_count - 1; i >= 0; --i) {
			VERIFIER_DEBUG ( printf ("verifying constructor argument %d\n", i); );
			value = stack_pop (ctx);
			if (!verify_stack_type_compatibility (ctx, sig->params [i], value)) {
				char *stack_name = stack_slot_full_name (value);
				char *sig_name = mono_type_full_name (sig->params [i]);
				CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Incompatible parameter value with constructor signature: %s X %s at 0x%04x", sig_name, stack_name, ctx->ip_offset));
				g_free (stack_name);
				g_free (sig_name);
			}

			if (stack_slot_is_managed_mutability_pointer (value))
				CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Cannot use a readonly pointer as argument of newobj at 0x%04x", ctx->ip_offset));
		}
	}

	if (check_overflow (ctx))
		set_stack_value (ctx, stack_push (ctx),  m_class_get_byval_arg (method->klass), FALSE);
}

static void
do_cast (VerifyContext *ctx, int token, const char *opcode) {
	ILStackDesc *value;
	MonoType *type;
	gboolean is_boxed;
	gboolean do_box;

	if (!check_underflow (ctx, 1))
		return;

	if (!(type = get_boxable_mono_type (ctx, token, opcode)))
		return;

	if (type->byref) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Invalid %s type at 0x%04x", opcode, ctx->ip_offset));
		return;
	}

	value = stack_pop (ctx);
	is_boxed = stack_slot_is_boxed_value (value);

	if (stack_slot_is_managed_pointer (value))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid value for %s at 0x%04x", opcode, ctx->ip_offset));
	else if (!MONO_TYPE_IS_REFERENCE  (value->type) && !is_boxed) {
		char *name = stack_slot_full_name (value);
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Expected a reference type on stack for %s but found %s at 0x%04x", opcode, name, ctx->ip_offset));
		g_free (name);
	}

	switch (value->type->type) {
	case MONO_TYPE_FNPTR:
	case MONO_TYPE_PTR:
	case MONO_TYPE_TYPEDBYREF: 
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid value for %s at 0x%04x", opcode, ctx->ip_offset));
	default:
		break;
	}

	do_box = is_boxed || mono_type_is_generic_argument(type) || m_class_is_valuetype (mono_class_from_mono_type_internal (type));
	stack_push_val (ctx, TYPE_COMPLEX | (do_box ? BOXED_MASK : 0), type);
}

static MonoType *
mono_type_from_opcode (int opcode) {
	switch (opcode) {
	case CEE_LDIND_I1:
	case CEE_LDIND_U1:
	case CEE_STIND_I1:
	case CEE_LDELEM_I1:
	case CEE_LDELEM_U1:
	case CEE_STELEM_I1:
		return m_class_get_byval_arg (mono_defaults.sbyte_class);

	case CEE_LDIND_I2:
	case CEE_LDIND_U2:
	case CEE_STIND_I2:
	case CEE_LDELEM_I2:
	case CEE_LDELEM_U2:
	case CEE_STELEM_I2:
		return m_class_get_byval_arg (mono_defaults.int16_class);

	case CEE_LDIND_I4:
	case CEE_LDIND_U4:
	case CEE_STIND_I4:
	case CEE_LDELEM_I4:
	case CEE_LDELEM_U4:
	case CEE_STELEM_I4:
		return mono_get_int32_type ();

	case CEE_LDIND_I8:
	case CEE_STIND_I8:
	case CEE_LDELEM_I8:
	case CEE_STELEM_I8:
		return m_class_get_byval_arg (mono_defaults.int64_class);

	case CEE_LDIND_R4:
	case CEE_STIND_R4:
	case CEE_LDELEM_R4:
	case CEE_STELEM_R4:
		return m_class_get_byval_arg (mono_defaults.single_class);

	case CEE_LDIND_R8:
	case CEE_STIND_R8:
	case CEE_LDELEM_R8:
	case CEE_STELEM_R8:
		return m_class_get_byval_arg (mono_defaults.double_class);

	case CEE_LDIND_I:
	case CEE_STIND_I:
	case CEE_LDELEM_I:
	case CEE_STELEM_I:
		return mono_get_int_type ();

	case CEE_LDIND_REF:
	case CEE_STIND_REF:
	case CEE_LDELEM_REF:
	case CEE_STELEM_REF:
		return mono_get_object_type ();

	default:
		g_error ("unknown opcode %02x in mono_type_from_opcode ", opcode);
		return NULL;
	}
}

static void
do_load_indirect (VerifyContext *ctx, int opcode)
{
	ILStackDesc *value;
	CLEAR_PREFIX (ctx, PREFIX_UNALIGNED | PREFIX_VOLATILE);

	if (!check_underflow (ctx, 1))
		return;
	
	value = stack_pop (ctx);
	if (!stack_slot_is_managed_pointer (value)) {
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Load indirect not using a manager pointer at 0x%04x", ctx->ip_offset));
		set_stack_value (ctx, stack_push (ctx), mono_type_from_opcode (opcode), FALSE);
		return;
	}

	if (opcode == CEE_LDIND_REF) {
		if (stack_slot_get_underlying_type (value) != TYPE_COMPLEX || m_class_is_valuetype (mono_class_from_mono_type_internal (value->type)))
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid type at stack for ldind_ref expected object byref operation at 0x%04x", ctx->ip_offset));
		set_stack_value (ctx, stack_push (ctx), mono_type_get_type_byval (value->type), FALSE);
	} else {
		if (!verify_type_compatibility_full (ctx, mono_type_from_opcode (opcode), mono_type_get_type_byval (value->type), TRUE))
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid type at stack for ldind 0x%x operation at 0x%04x", opcode, ctx->ip_offset));
		set_stack_value (ctx, stack_push (ctx), mono_type_from_opcode (opcode), FALSE);
	}
}

static void
do_store_indirect (VerifyContext *ctx, int opcode)
{
	ILStackDesc *addr, *val;
	CLEAR_PREFIX (ctx, PREFIX_UNALIGNED | PREFIX_VOLATILE);

	if (!check_underflow (ctx, 2))
		return;

	val = stack_pop (ctx);
	addr = stack_pop (ctx);	

	check_unmanaged_pointer (ctx, addr);

	if (!stack_slot_is_managed_pointer (addr) && stack_slot_get_type (addr) != TYPE_PTR) {
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid non-pointer argument to stind at 0x%04x", ctx->ip_offset));
		return;
	}

	if (stack_slot_is_managed_mutability_pointer (addr)) {
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Cannot use a readonly pointer with stind at 0x%04x", ctx->ip_offset));
		return;
	}

	if (!verify_type_compatibility_full (ctx, mono_type_from_opcode (opcode), mono_type_get_type_byval (addr->type), TRUE))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid addr type at stack for stind 0x%x operation at 0x%04x", opcode, ctx->ip_offset));

	if (!verify_stack_type_compatibility (ctx, mono_type_from_opcode (opcode), val))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid value type at stack for stind 0x%x operation at 0x%04x", opcode, ctx->ip_offset));
}

static void
do_newarr (VerifyContext *ctx, int token) 
{
	ILStackDesc *value;
	MonoType *type = get_boxable_mono_type (ctx, token, "newarr");

	if (!type)
		return;

	if (!check_underflow (ctx, 1))
		return;

	value = stack_pop (ctx);
	if (stack_slot_get_type (value) != TYPE_I4 && stack_slot_get_type (value) != TYPE_NATIVE_INT)
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Array size type on stack (%s) is not a verifiable type at 0x%04x", stack_slot_get_name (value), ctx->ip_offset));

	set_stack_value (ctx, stack_push (ctx), m_class_get_byval_arg (mono_class_create_array (mono_class_from_mono_type_internal (type), 1)), FALSE);
}

/*FIXME handle arrays that are not 0-indexed*/
static void
do_ldlen (VerifyContext *ctx)
{
	ILStackDesc *value;

	if (!check_underflow (ctx, 1))
		return;

	value = stack_pop (ctx);

	if (stack_slot_get_type (value) != TYPE_COMPLEX || value->type->type != MONO_TYPE_SZARRAY)
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid array type for ldlen at 0x%04x", ctx->ip_offset));

	stack_push_val (ctx, TYPE_NATIVE_INT, mono_get_int_type ());
}

/*FIXME handle arrays that are not 0-indexed*/
/*FIXME handle readonly prefix and CMMP*/
static void
do_ldelema (VerifyContext *ctx, int klass_token)
{
	ILStackDesc *index, *array, *res;
	MonoType *type = get_boxable_mono_type (ctx, klass_token, "ldelema");
	gboolean valid; 

	if (!type)
		return;

	if (!check_underflow (ctx, 2))
		return;

	index = stack_pop (ctx);
	array = stack_pop (ctx);

	if (stack_slot_get_type (index) != TYPE_I4 && stack_slot_get_type (index) != TYPE_NATIVE_INT)
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Index type(%s) for ldelema is not an int or a native int at 0x%04x", stack_slot_get_name (index), ctx->ip_offset));

	if (!stack_slot_is_null_literal (array)) {
		if (stack_slot_get_type (array) != TYPE_COMPLEX || array->type->type != MONO_TYPE_SZARRAY)
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid array type(%s) for ldelema at 0x%04x", stack_slot_get_name (array), ctx->ip_offset));
		else {
			if (get_stack_type (type) == TYPE_I4 || get_stack_type (type) == TYPE_NATIVE_INT) {
				valid = verify_type_compatibility_full (ctx, type, m_class_get_byval_arg (array->type->data.klass), TRUE);
			} else {
				valid = mono_metadata_type_equal (type, m_class_get_byval_arg (array->type->data.klass));
			}
			if (!valid)
				CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid array type on stack for ldelema at 0x%04x", ctx->ip_offset));
		}
	}

	res = stack_push (ctx);
	set_stack_value (ctx, res, type, TRUE);
	if (ctx->prefix_set & PREFIX_READONLY) {
		ctx->prefix_set &= ~PREFIX_READONLY;
		res->stype |= CMMP_MASK;
	}

	res->stype |= SAFE_BYREF_MASK;
}

/*
 * FIXME handle arrays that are not 0-indexed
 * FIXME handle readonly prefix and CMMP
 */
static void
do_ldelem (VerifyContext *ctx, int opcode, int token)
{
#define IS_ONE_OF2(T, A, B) (T == A || T == B)
	ILStackDesc *index, *array;
	MonoType *type;
	if (!check_underflow (ctx, 2))
		return;

	if (opcode == CEE_LDELEM) {
		if (!(type = verifier_load_type (ctx, token, "ldelem.any"))) {
			ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Type (0x%08x) not found at 0x%04x", token, ctx->ip_offset));
			return;
		}
	} else {
		type = mono_type_from_opcode (opcode);
	}

	index = stack_pop (ctx);
	array = stack_pop (ctx);

	if (stack_slot_get_type (index) != TYPE_I4 && stack_slot_get_type (index) != TYPE_NATIVE_INT)
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Index type(%s) for ldelem.X is not an int or a native int at 0x%04x", stack_slot_get_name (index), ctx->ip_offset));

	if (!stack_slot_is_null_literal (array)) {
		if (stack_slot_get_type (array) != TYPE_COMPLEX || array->type->type != MONO_TYPE_SZARRAY)
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid array type(%s) for ldelem.X at 0x%04x", stack_slot_get_name (array), ctx->ip_offset));
		else {
			if (opcode == CEE_LDELEM_REF) {
				if (m_class_is_valuetype (array->type->data.klass))
					CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid array type is not a reference type for ldelem.ref 0x%04x", ctx->ip_offset));
				type = m_class_get_byval_arg (array->type->data.klass);
			} else {
				MonoType *candidate = m_class_get_byval_arg (array->type->data.klass);
				if (IS_STRICT_MODE (ctx)) {
					MonoType *underlying_type = mono_type_get_underlying_type_any (type);
					MonoType *underlying_candidate = mono_type_get_underlying_type_any (candidate);
					if ((IS_ONE_OF2 (underlying_type->type, MONO_TYPE_I4, MONO_TYPE_U4) && IS_ONE_OF2 (underlying_candidate->type, MONO_TYPE_I, MONO_TYPE_U)) ||
						(IS_ONE_OF2 (underlying_candidate->type, MONO_TYPE_I4, MONO_TYPE_U4) && IS_ONE_OF2 (underlying_type->type, MONO_TYPE_I, MONO_TYPE_U)))
						CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid array type on stack for ldelem.X at 0x%04x", ctx->ip_offset));
				}
				if (!verify_type_compatibility_full (ctx, type, candidate, TRUE))
					CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid array type on stack for ldelem.X at 0x%04x", ctx->ip_offset));
			}
		}
	}

	set_stack_value (ctx, stack_push (ctx), type, FALSE);
#undef IS_ONE_OF2
}

/*
 * FIXME handle arrays that are not 0-indexed
 */
static void
do_stelem (VerifyContext *ctx, int opcode, int token)
{
	ILStackDesc *index, *array, *value;
	MonoType *type;
	if (!check_underflow (ctx, 3))
		return;

	if (opcode == CEE_STELEM) {
		if (!(type = verifier_load_type (ctx, token, "stelem.any"))) {
			ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Type (0x%08x) not found at 0x%04x", token, ctx->ip_offset));
			return;
		}
	} else {
		type = mono_type_from_opcode (opcode);
	}
	
	value = stack_pop (ctx);
	index = stack_pop (ctx);
	array = stack_pop (ctx);

	if (stack_slot_get_type (index) != TYPE_I4 && stack_slot_get_type (index) != TYPE_NATIVE_INT)
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Index type(%s) for stdelem.X is not an int or a native int at 0x%04x", stack_slot_get_name (index), ctx->ip_offset));

	if (!stack_slot_is_null_literal (array)) {
		if (stack_slot_get_type (array) != TYPE_COMPLEX || array->type->type != MONO_TYPE_SZARRAY) {
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid array type(%s) for stelem.X at 0x%04x", stack_slot_get_name (array), ctx->ip_offset));
		} else {
			if (opcode == CEE_STELEM_REF) {
				if (m_class_is_valuetype (array->type->data.klass))
					CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid array type is not a reference type for stelem.ref 0x%04x", ctx->ip_offset));
			} else if (!verify_type_compatibility_full (ctx, m_class_get_byval_arg (array->type->data.klass), type, TRUE)) {
					CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid array type on stack for stdelem.X at 0x%04x", ctx->ip_offset));
			}
		}
	}
	if (opcode == CEE_STELEM_REF) {
		if (!stack_slot_is_boxed_value (value) && m_class_is_valuetype (mono_class_from_mono_type_internal (value->type)))
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid value is not a reference type for stelem.ref 0x%04x", ctx->ip_offset));
	} else if (opcode != CEE_STELEM_REF) {
		if (!verify_stack_type_compatibility (ctx, type, value))
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid value on stack for stdelem.X at 0x%04x", ctx->ip_offset));

		if (stack_slot_is_boxed_value (value) && !MONO_TYPE_IS_REFERENCE (value->type) && !MONO_TYPE_IS_REFERENCE (type))
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Cannot use stobj with a boxed source value that is not a reference type at 0x%04x", ctx->ip_offset));

	}
}

static void
do_throw (VerifyContext *ctx)
{
	ILStackDesc *exception;
	if (!check_underflow (ctx, 1))
		return;
	exception = stack_pop (ctx);

	if (!stack_slot_is_null_literal (exception) && !(stack_slot_get_type (exception) == TYPE_COMPLEX && !m_class_is_valuetype (mono_class_from_mono_type_internal (exception->type))))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid type on stack for throw, expected reference type at 0x%04x", ctx->ip_offset));

	if (mono_type_is_generic_argument (exception->type) && !stack_slot_is_boxed_value (exception)) {
		char *name = mono_type_full_name (exception->type);
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid type on stack for throw, expected reference type but found unboxed %s  at 0x%04x ", name, ctx->ip_offset));
		g_free (name);
	}
	/*The stack is left empty after a throw*/
	ctx->eval.size = 0;
}


static void
do_endfilter (VerifyContext *ctx)
{
	MonoExceptionClause *clause;

	if (IS_STRICT_MODE (ctx)) {
		if (ctx->eval.size != 1)
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Stack size must have one item for endfilter at 0x%04x", ctx->ip_offset));

		if (ctx->eval.size >= 1 && stack_slot_get_type (stack_pop (ctx)) != TYPE_I4)
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Stack item type is not an int32 for endfilter at 0x%04x", ctx->ip_offset));
	}

	if ((clause = is_correct_endfilter (ctx, ctx->ip_offset))) {
		if (IS_STRICT_MODE (ctx)) {
			if (ctx->ip_offset != clause->handler_offset - 2)
				ADD_VERIFY_ERROR (ctx, g_strdup_printf ("endfilter is not the last instruction of the filter clause at 0x%04x", ctx->ip_offset));			
		} else {
			if ((ctx->ip_offset != clause->handler_offset - 2) && !MONO_OFFSET_IN_HANDLER (clause, ctx->ip_offset))
				ADD_VERIFY_ERROR (ctx, g_strdup_printf ("endfilter is not the last instruction of the filter clause at 0x%04x", ctx->ip_offset));
		}
	} else {
		if (IS_STRICT_MODE (ctx) && !is_unverifiable_endfilter (ctx, ctx->ip_offset))
			ADD_VERIFY_ERROR (ctx, g_strdup_printf ("endfilter outside filter clause at 0x%04x", ctx->ip_offset));
		else
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("endfilter outside filter clause at 0x%04x", ctx->ip_offset));
	}

	ctx->eval.size = 0;
}

static void
do_leave (VerifyContext *ctx, int delta)
{
	int target = ((gint32)ctx->ip_offset) + delta;
	if (target >= ctx->code_size || target < 0)
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Branch target out of code at 0x%04x", ctx->ip_offset));

	if (!is_correct_leave (ctx->header, ctx->ip_offset, target))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Leave not allowed in finally block at 0x%04x", ctx->ip_offset));
	ctx->eval.size = 0;
	ctx->target = target;
}

/* 
 * do_static_branch:
 * 
 * Verify br and br.s opcodes.
 */
static void
do_static_branch (VerifyContext *ctx, int delta)
{
	int target = ctx->ip_offset + delta;
	if (target < 0 || target >= ctx->code_size) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("branch target out of code at 0x%04x", ctx->ip_offset));
		return;
	}

	switch (is_valid_branch_instruction (ctx->header, ctx->ip_offset, target)) {
	case 1:
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Branch target escapes out of exception block at 0x%04x", ctx->ip_offset));
		break;
	case 2:
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Branch target escapes out of exception block at 0x%04x", ctx->ip_offset));
		break;
	}

	ctx->target = target;
}

static void
do_switch (VerifyContext *ctx, int count, const unsigned char *data)
{
	int i, base = ctx->ip_offset + 5 + count * 4;
	ILStackDesc *value;

	if (!check_underflow (ctx, 1))
		return;

	value = stack_pop (ctx);

	if (stack_slot_get_type (value) != TYPE_I4 && stack_slot_get_type (value) != TYPE_NATIVE_INT)
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid argument to switch at 0x%04x", ctx->ip_offset));

	for (i = 0; i < count; ++i) {
		int target = base + read32 (data + i * 4);

		if (target < 0 || target >= ctx->code_size) {
			ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Switch target %x out of code at 0x%04x", i, ctx->ip_offset));
			return;
		}

		switch (is_valid_branch_instruction (ctx->header, ctx->ip_offset, target)) {
		case 1:
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Switch target %x escapes out of exception block at 0x%04x", i, ctx->ip_offset));
			break;
		case 2:
			ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Switch target %x escapes out of exception block at 0x%04x", i, ctx->ip_offset));
			return;
		}
		merge_stacks (ctx, &ctx->eval, &ctx->code [target], FALSE, TRUE);
	}
}

static void
do_load_function_ptr (VerifyContext *ctx, guint32 token, gboolean virtual_)
{
	ILStackDesc *top;
	MonoMethod *method;

	if (virtual_ && !check_underflow (ctx, 1))
		return;

	if (!virtual_ && !check_overflow (ctx))
		return;

	if (ctx->method->wrapper_type != MONO_WRAPPER_NONE) {
		method = (MonoMethod *)mono_method_get_wrapper_data (ctx->method, (guint32)token);
		if (!method) {
			ADD_VERIFY_ERROR2 (ctx, g_strdup_printf ("Invalid token %x for ldftn  at 0x%04x", token, ctx->ip_offset), MONO_EXCEPTION_BAD_IMAGE);
			return;
		}
	} else {
		if (!IS_METHOD_DEF_OR_REF_OR_SPEC (token) || !token_bounds_check (ctx->image, token)) {
			ADD_VERIFY_ERROR2 (ctx, g_strdup_printf ("Invalid token %x for ldftn  at 0x%04x", token, ctx->ip_offset), MONO_EXCEPTION_BAD_IMAGE);
			return;
		}

		if (!(method = verifier_load_method (ctx, token, virtual_ ? "ldvirtfrn" : "ldftn")))
			return;
	}

	if (mono_method_is_constructor (method))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Cannot use ldftn with a constructor at 0x%04x", ctx->ip_offset));

	if (virtual_) {
		ILStackDesc *top = stack_pop (ctx);
	
		if (stack_slot_get_type (top) != TYPE_COMPLEX || top->type->type == MONO_TYPE_VALUETYPE)
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Invalid argument to ldvirtftn at 0x%04x", ctx->ip_offset));
	
		if (method->flags & METHOD_ATTRIBUTE_STATIC)
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Cannot use ldvirtftn with a constructor at 0x%04x", ctx->ip_offset));

		if (!verify_stack_type_compatibility (ctx, m_class_get_byval_arg (method->klass), top))
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Unexpected object for ldvirtftn at 0x%04x", ctx->ip_offset));
	}
	
	if (!IS_SKIP_VISIBILITY (ctx) && !mono_method_can_access_method_full (ctx->method, method, NULL))
		CODE_NOT_VERIFIABLE2 (ctx, g_strdup_printf ("Loaded method is not visible for ldftn/ldvirtftn at 0x%04x", ctx->ip_offset), MONO_EXCEPTION_METHOD_ACCESS);

	top = stack_push_val(ctx, TYPE_PTR, mono_type_create_fnptr_from_mono_method (ctx, method));
	top->method = method;
}

static void
do_sizeof (VerifyContext *ctx, int token)
{
	MonoType *type;
	
	if (!(type = verifier_load_type (ctx, token, "sizeof")))
		return;

	if (type->byref && type->type != MONO_TYPE_TYPEDBYREF) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Invalid use of byref type at 0x%04x", ctx->ip_offset));
		return;
	}

	if (type->type == MONO_TYPE_VOID) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Invalid use of void type at 0x%04x", ctx->ip_offset));
		return;
	}

	if (check_overflow (ctx))
		set_stack_value (ctx, stack_push (ctx), m_class_get_byval_arg (mono_defaults.uint32_class), FALSE);
}

/* Stack top can be of any type, the runtime doesn't care and treat everything as an int. */
static void
do_localloc (VerifyContext *ctx)
{
	if (ctx->eval.size != 1) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Stack must have only size item in localloc at 0x%04x", ctx->ip_offset));
		return;		
	}

	if (in_any_exception_block (ctx->header, ctx->ip_offset)) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Stack must have only size item in localloc at 0x%04x", ctx->ip_offset));
		return;
	}

	/*TODO verify top type*/
	/* top = */ stack_pop (ctx);

	set_stack_value (ctx, stack_push (ctx), mono_get_int_type (), FALSE);
	CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Instruction localloc in never verifiable at 0x%04x", ctx->ip_offset));
}

static void
do_ldstr (VerifyContext *ctx, guint32 token)
{
	ERROR_DECL (error);
	if (ctx->method->wrapper_type == MONO_WRAPPER_NONE && !image_is_dynamic (ctx->image)) {
		if (mono_metadata_token_code (token) != MONO_TOKEN_STRING) {
			ADD_VERIFY_ERROR2 (ctx, g_strdup_printf ("Invalid string token %x at 0x%04x", token, ctx->ip_offset), MONO_EXCEPTION_BAD_IMAGE);
			return;
		}

		if (!mono_verifier_verify_string_signature (ctx->image, mono_metadata_token_index (token), error)) {
			ADD_VERIFY_ERROR2 (ctx, g_strdup_printf ("Invalid string index %x at 0x%04x due to: %s", token, ctx->ip_offset, mono_error_get_message (error)), MONO_EXCEPTION_BAD_IMAGE);
			mono_error_cleanup (error);
			return;
		}
	}

	if (check_overflow (ctx))
		stack_push_val (ctx, TYPE_COMPLEX,  m_class_get_byval_arg (mono_defaults.string_class));
}

static void
do_refanyval (VerifyContext *ctx, int token)
{
	ILStackDesc *top;
	MonoType *type;
	if (!check_underflow (ctx, 1))
		return;

	if (!(type = get_boxable_mono_type (ctx, token, "refanyval")))
		return;

	top = stack_pop (ctx);

	if (top->stype != TYPE_PTR || top->type->type != MONO_TYPE_TYPEDBYREF)
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Expected a typedref as argument for refanyval, but found %s at 0x%04x", stack_slot_get_name (top), ctx->ip_offset));

	set_stack_value (ctx, stack_push (ctx), type, TRUE);
}

static void
do_refanytype (VerifyContext *ctx)
{
	ILStackDesc *top;

	if (!check_underflow (ctx, 1))
		return;

	top = stack_pop (ctx);

	if (top->stype != TYPE_PTR || top->type->type != MONO_TYPE_TYPEDBYREF)
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Expected a typedref as argument for refanytype, but found %s at 0x%04x", stack_slot_get_name (top), ctx->ip_offset));

	set_stack_value (ctx, stack_push (ctx), m_class_get_byval_arg (mono_defaults.typehandle_class), FALSE);

}

static void
do_mkrefany (VerifyContext *ctx, int token)
{
	ILStackDesc *top;
	MonoType *type;
	if (!check_underflow (ctx, 1))
		return;

	if (!(type = get_boxable_mono_type (ctx, token, "refanyval")))
		return;

	top = stack_pop (ctx);

	if (stack_slot_is_managed_mutability_pointer (top))
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Cannot use a readonly pointer with mkrefany at 0x%04x", ctx->ip_offset));

	if (!stack_slot_is_managed_pointer (top)) {
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Expected a managed pointer for mkrefany, but found %s at 0x%04x", stack_slot_get_name (top), ctx->ip_offset));
	}else {
		MonoType *stack_type = mono_type_get_type_byval (top->type);
		if (MONO_TYPE_IS_REFERENCE (type) && !mono_metadata_type_equal (type, stack_type))
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Type not compatible for mkrefany at 0x%04x", ctx->ip_offset));
			
		if (!MONO_TYPE_IS_REFERENCE (type) && !verify_type_compatibility_full (ctx, type, stack_type, TRUE))
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Type not compatible for mkrefany at 0x%04x", ctx->ip_offset));
	}

	set_stack_value (ctx, stack_push (ctx), m_class_get_byval_arg (mono_defaults.typed_reference_class), FALSE);
}

static void
do_ckfinite (VerifyContext *ctx)
{
	ILStackDesc *top;
	if (!check_underflow (ctx, 1))
		return;

	top = stack_pop (ctx);

	if (stack_slot_get_underlying_type (top) != TYPE_R8)
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Expected float32 or float64 on stack for ckfinit but found %s at 0x%04x", stack_slot_get_name (top), ctx->ip_offset));	
	stack_push_stack_val (ctx, top);
}
/*
 * merge_stacks:
 * Merge the stacks and perform compat checks. The merge check if types of @from are mergeable with type of @to 
 * 
 * @from holds new values for a given control path
 * @to holds the current values of a given control path
 * 
 * TODO we can eliminate the from argument as all callers pass &ctx->eval
 */
static void
merge_stacks (VerifyContext *ctx, ILCodeDesc *from, ILCodeDesc *to, gboolean start, gboolean external) 
{
	ERROR_DECL (error);
	int i, j;
	stack_init (ctx, to);

	if (start) {
		if (to->flags == IL_CODE_FLAG_NOT_PROCESSED) 
			from->size = 0;
		else
			stack_copy (&ctx->eval, to);
		goto end_verify;
	} else if (!(to->flags & IL_CODE_STACK_MERGED)) {
		stack_copy (to, &ctx->eval);
		goto end_verify;
	}
	VERIFIER_DEBUG ( printf ("performing stack merge %d x %d\n", from->size, to->size); );

	if (from->size != to->size) {
		VERIFIER_DEBUG ( printf ("different stack sizes %d x %d at 0x%04x\n", from->size, to->size, ctx->ip_offset); );
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Could not merge stacks, different sizes (%d x %d) at 0x%04x", from->size, to->size, ctx->ip_offset)); 
		goto end_verify;
	}

	//FIXME we need to preserve CMMP attributes
	//FIXME we must take null literals into consideration.
	for (i = 0; i < from->size; ++i) {
		ILStackDesc *new_slot = from->stack + i;
		ILStackDesc *old_slot = to->stack + i;
		MonoType *new_type = mono_type_from_stack_slot (new_slot);
		MonoType *old_type = mono_type_from_stack_slot (old_slot);
		MonoClass *old_class = mono_class_from_mono_type_internal (old_type);
		MonoClass *new_class = mono_class_from_mono_type_internal (new_type);
		MonoClass *match_class = NULL;

		// check for safe byref before the next steps override new_slot
		if (stack_slot_is_safe_byref (old_slot) ^ stack_slot_is_safe_byref (new_slot)) {
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Cannot merge stack at depth %d byref types are safe byref incompatible at %0x04x ", i, ctx->ip_offset));
			goto end_verify;
		}

		// S := T then U = S (new value is compatible with current value, keep current)
		if (verify_stack_type_compatibility (ctx, old_type, new_slot)) {
			copy_stack_value (new_slot, old_slot);
			continue;
		}

		// T := S then U = T (old value is compatible with current value, use new)
		if (verify_stack_type_compatibility (ctx, new_type, old_slot)) {
			copy_stack_value (old_slot, new_slot);
			continue;
		}

		/*Both slots are the same boxed valuetype. Simply copy it.*/
		if (stack_slot_is_boxed_value (old_slot) && 
			stack_slot_is_boxed_value (new_slot) &&
			mono_metadata_type_equal (old_type, new_type)) {
			copy_stack_value (new_slot, old_slot);
			continue;
		}

		if (mono_type_is_generic_argument (old_type) || mono_type_is_generic_argument (new_type)) {
			char *old_name = stack_slot_full_name (old_slot); 
			char *new_name = stack_slot_full_name (new_slot);
			CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Could not merge stack at depth %d, types not compatible: %s X %s at 0x%04x", i, old_name, new_name, ctx->ip_offset));
			g_free (old_name);
			g_free (new_name);
			goto end_verify;			
		} 

		//both are reference types, use closest common super type
		if (!m_class_is_valuetype (mono_class_from_mono_type_internal (old_type))
			&& !m_class_is_valuetype (mono_class_from_mono_type_internal (new_type))
			&& !stack_slot_is_managed_pointer (old_slot)
			&& !stack_slot_is_managed_pointer (new_slot)) {

			mono_class_setup_supertypes (old_class);
			mono_class_setup_supertypes (new_class);

			MonoClass **old_class_supertypes = m_class_get_supertypes (old_class);
			MonoClass **new_class_supertypes = m_class_get_supertypes (new_class);
			for (j = MIN (m_class_get_idepth (old_class), m_class_get_idepth (new_class)) - 1; j > 0; --j) {
				if (mono_metadata_type_equal (m_class_get_byval_arg (old_class_supertypes [j]), m_class_get_byval_arg (new_class_supertypes [j]))) {
					match_class = old_class_supertypes [j];
					goto match_found;
				}
			}

			mono_class_setup_interfaces (old_class, error);
			if (!is_ok (error)) {
				CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Cannot merge stacks due to a TypeLoadException %s at 0x%04x", mono_error_get_message (error), ctx->ip_offset));
				mono_error_cleanup (error);
				goto end_verify;
			}
			mono_class_setup_interfaces (new_class, error);
			if (!is_ok (error)) {
				CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Cannot merge stacks due to a TypeLoadException %s at 0x%04x", mono_error_get_message (error), ctx->ip_offset));
				mono_error_cleanup (error);
				goto end_verify;
			}

			/* if old class is an interface that new class implements */
			if (mono_class_is_interface (old_class)) {
				if (verifier_class_is_assignable_from (old_class, new_class)) {
					match_class = old_class;
					goto match_found;	
				}
				MonoClass **old_class_interfaces = m_class_get_interfaces (old_class);
				guint16 old_class_interface_count = m_class_get_interface_count (old_class);
				for (j = 0; j < old_class_interface_count; ++j) {
					if (verifier_class_is_assignable_from (old_class_interfaces [j], new_class)) {
						match_class = old_class_interfaces [j];
						goto match_found;	
					}
				}
			}

			if (mono_class_is_interface (new_class)) {
				if (verifier_class_is_assignable_from (new_class, old_class)) {
					match_class = new_class;
					goto match_found;	
				}
				MonoClass **new_class_interfaces = m_class_get_interfaces (new_class);
				guint16 new_class_interface_count = m_class_get_interface_count (new_class);
				for (j = 0; j < new_class_interface_count; ++j) {
					if (verifier_class_is_assignable_from (new_class_interfaces [j], old_class)) {
						match_class = new_class_interfaces [j];
						goto match_found;	
					}
				}
			}

			//No decent super type found, use object
			match_class = mono_defaults.object_class;
			goto match_found;
		} else if (is_compatible_boxed_valuetype (ctx,old_type, new_type, new_slot, FALSE) || is_compatible_boxed_valuetype (ctx, new_type, old_type, old_slot, FALSE)) {
			match_class = mono_defaults.object_class;
			goto match_found;
		}

		{
		char *old_name = stack_slot_full_name (old_slot); 
		char *new_name = stack_slot_full_name (new_slot);
		CODE_NOT_VERIFIABLE (ctx, g_strdup_printf ("Could not merge stack at depth %d, types not compatible: %s X %s at 0x%04x", i, old_name, new_name, ctx->ip_offset)); 
		g_free (old_name);
		g_free (new_name);
		}
		set_stack_value (ctx, old_slot, m_class_get_byval_arg (new_class), stack_slot_is_managed_pointer (old_slot));
		goto end_verify;

match_found:
		g_assert (match_class);
		set_stack_value (ctx, old_slot, m_class_get_byval_arg (match_class), stack_slot_is_managed_pointer (old_slot));
		set_stack_value (ctx, new_slot, m_class_get_byval_arg (match_class), stack_slot_is_managed_pointer (old_slot));
		continue;
	}

end_verify:
	if (external)
		to->flags |= IL_CODE_FLAG_WAS_TARGET;
	to->flags |= IL_CODE_STACK_MERGED;
}

#define HANDLER_START(clause) ((clause)->flags == MONO_EXCEPTION_CLAUSE_FILTER ? (clause)->data.filter_offset : clause->handler_offset)
#define IS_CATCH_OR_FILTER(clause) ((clause)->flags == MONO_EXCEPTION_CLAUSE_FILTER || (clause)->flags == MONO_EXCEPTION_CLAUSE_NONE)

/**
 * is_clause_in_range :
 * 
 * Returns TRUE if either the protected block or the handler of @clause is in the @start - @end range.  
 */
static gboolean
is_clause_in_range (MonoExceptionClause *clause, guint32 start, guint32 end)
{
	if (clause->try_offset >= start && clause->try_offset < end)
		return TRUE;
	if (HANDLER_START (clause) >= start && HANDLER_START (clause) < end)
		return TRUE;
	return FALSE;
}

/**
 * is_clause_inside_range :
 * 
 * Returns TRUE if @clause lies completely inside the @start - @end range.  
 */
static gboolean
is_clause_inside_range (MonoExceptionClause *clause, guint32 start, guint32 end)
{
	if (clause->try_offset < start || (clause->try_offset + clause->try_len) > end)
		return FALSE;
	if (HANDLER_START (clause) < start || (clause->handler_offset + clause->handler_len) > end)
		return FALSE;
	return TRUE;
}

/**
 * is_clause_nested :
 * 
 * Returns TRUE if @nested is nested in @clause.   
 */
static gboolean
is_clause_nested (MonoExceptionClause *clause, MonoExceptionClause *nested)
{
	if (clause->flags == MONO_EXCEPTION_CLAUSE_FILTER && is_clause_inside_range (nested, clause->data.filter_offset, clause->handler_offset))
		return TRUE;
	return is_clause_inside_range (nested, clause->try_offset, clause->try_offset + clause->try_len) ||
	is_clause_inside_range (nested, clause->handler_offset, clause->handler_offset + clause->handler_len);
}

/* Test the relationship between 2 exception clauses. Follow  P.1 12.4.2.7 of ECMA
 * the each pair of exception must have the following properties:
 *  - one is fully nested on another (the outer must not be a filter clause) (the nested one must come earlier)
 *  - completely disjoin (none of the 3 regions of each entry overlap with the other 3)
 *  - mutual protection (protected block is EXACT the same, handlers are disjoin and all handler are catch or all handler are filter)
 */
static void
verify_clause_relationship (VerifyContext *ctx, MonoExceptionClause *clause, MonoExceptionClause *to_test)
{
	/*clause is nested*/
	if (to_test->flags == MONO_EXCEPTION_CLAUSE_FILTER && is_clause_inside_range (clause, to_test->data.filter_offset, to_test->handler_offset)) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Exception clause inside filter"));
		return;
	}

	/*wrong nesting order.*/
	if (is_clause_nested (clause, to_test)) {
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Nested exception clause appears after enclosing clause"));
		return;
	}

	/*mutual protection*/
	if (clause->try_offset == to_test->try_offset && clause->try_len == to_test->try_len) {
		/*handlers are not disjoint*/
		if (is_clause_in_range (to_test, HANDLER_START (clause), clause->handler_offset + clause->handler_len)) {
			ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Exception handlers overlap"));
			return;
		}
		/* handlers are not catch or filter */
		if (!IS_CATCH_OR_FILTER (clause) || !IS_CATCH_OR_FILTER (to_test)) {
			ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Exception clauses with shared protected block are neither catch or filter"));
			return;
		}
		/*OK*/
		return;
	}

	/*not completelly disjoint*/
	if ((is_clause_in_range (to_test, clause->try_offset, clause->try_offset + clause->try_len) ||
		is_clause_in_range (to_test, HANDLER_START (clause), clause->handler_offset + clause->handler_len)) && !is_clause_nested (to_test, clause))
		ADD_VERIFY_ERROR (ctx, g_strdup_printf ("Exception clauses overlap"));
}

#define code_bounds_check(size) \
	if (ADDP_IS_GREATER_OR_OVF (ip, size, end)) {\
		ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("Code overrun starting with 0x%x at 0x%04x", *ip, ctx.ip_offset)); \
		break; \
	} \

static gboolean
mono_opcode_is_prefix (int op)
{
	switch (op) {
	case MONO_CEE_UNALIGNED_:
	case MONO_CEE_VOLATILE_:
	case MONO_CEE_TAIL_:
	case MONO_CEE_CONSTRAINED_:
	case MONO_CEE_READONLY_:
		return TRUE;
	}
	return FALSE;
}

/*
 * FIXME: need to distinguish between valid and verifiable.
 * Need to keep track of types on the stack.
 */

/**
 * mono_method_verify:
 * Verify types for opcodes.
 */
GSList*
mono_method_verify (MonoMethod *method, int level)
{
	ERROR_DECL (error);
	const unsigned char *ip, *code_start;
	const unsigned char *end;
	MonoSimpleBasicBlock *bb = NULL, *original_bb = NULL;

	int i, n, need_merge = 0, start = 0;
	guint ip_offset = 0, prefix = 0;
	MonoGenericContext *generic_context = NULL;
	MonoImage *image;
	VerifyContext ctx;
	GSList *tmp;
	VERIFIER_DEBUG ( printf ("Verify IL for method %s %s %s\n",  method->klass->name_space,  method->klass->name, method->name); );

	init_verifier_stats ();

	if (method->iflags & (METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL | METHOD_IMPL_ATTRIBUTE_RUNTIME) ||
			(method->flags & (METHOD_ATTRIBUTE_PINVOKE_IMPL | METHOD_ATTRIBUTE_ABSTRACT))) {
		return NULL;
	}

	// Disable for now
	if (TRUE)
		return NULL;

	memset (&ctx, 0, sizeof (VerifyContext));

	//FIXME use mono_method_get_signature_full
	ctx.signature = mono_method_signature_internal (method);
	if (!ctx.signature) {
		ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("Could not decode method signature"));

		finish_collect_stats ();
		return ctx.list;
	}
	if (!method->is_generic && !mono_class_is_gtd (method->klass) && ctx.signature->has_type_parameters) {
		ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("Method and signature don't match in terms of genericity"));
		finish_collect_stats ();
		return ctx.list;
	}

	ctx.header = mono_method_get_header_checked (method, error);
	if (!ctx.header) {
		ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("Could not decode method header due to %s", mono_error_get_message (error)));
		mono_error_cleanup (error);
		finish_collect_stats ();
		return ctx.list;
	}
	ctx.method = method;
	code_start = ip = ctx.header->code;
	end = ip + ctx.header->code_size;
	ctx.image = image = m_class_get_image (method->klass);


	ctx.max_args = ctx.signature->param_count + ctx.signature->hasthis;
	ctx.max_stack = ctx.header->max_stack;
	ctx.verifiable = ctx.valid = 1;
	ctx.level = level;

	ctx.code = g_new (ILCodeDesc, ctx.header->code_size);
	ctx.code_size = ctx.header->code_size;
	_MEM_ALLOC (sizeof (ILCodeDesc) * ctx.header->code_size);

	memset(ctx.code, 0, sizeof (ILCodeDesc) * ctx.header->code_size);

	ctx.num_locals = ctx.header->num_locals;
	ctx.locals = (MonoType **)g_memdup (ctx.header->locals, sizeof (MonoType*) * ctx.header->num_locals);
	_MEM_ALLOC (sizeof (MonoType*) * ctx.header->num_locals);
	ctx.locals_verification_state = g_new0 (char, ctx.num_locals);

	if (ctx.num_locals > 0 && !ctx.header->init_locals)
		CODE_NOT_VERIFIABLE (&ctx, g_strdup_printf ("Method with locals variable but without init locals set"));

	ctx.params = g_new (MonoType*, ctx.max_args);
	_MEM_ALLOC (sizeof (MonoType*) * ctx.max_args);

	if (ctx.signature->hasthis)
		ctx.params [0] = m_class_is_valuetype (method->klass) ? m_class_get_this_arg (method->klass) : m_class_get_byval_arg (method->klass);
	memcpy (ctx.params + ctx.signature->hasthis, ctx.signature->params, sizeof (MonoType *) * ctx.signature->param_count);

	if (ctx.signature->is_inflated)
		ctx.generic_context = generic_context = mono_method_get_context (method);

	if (!generic_context && (mono_class_is_gtd (method->klass) || method->is_generic)) {
		if (method->is_generic)
			ctx.generic_context = generic_context = &(mono_method_get_generic_container (method)->context);
		else
			ctx.generic_context = generic_context = &mono_class_get_generic_container (method->klass)->context;
	}

	for (i = 0; i < ctx.num_locals; ++i) {
		MonoType *uninflated = ctx.locals [i];
		ctx.locals [i] = mono_class_inflate_generic_type_checked (ctx.locals [i], ctx.generic_context, error);
		if (!is_ok (error)) {
			char *name = mono_type_full_name (ctx.locals [i] ? ctx.locals [i] : uninflated);
			ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("Invalid local %d of type %s", i, name));
			g_free (name);
			mono_error_cleanup (error);
			/* we must not free (in cleanup) what was not yet allocated (but only copied) */
			ctx.num_locals = i;
			ctx.max_args = 0;
			goto cleanup;
		}
	}
	for (i = 0; i < ctx.max_args; ++i) {
		MonoType *uninflated = ctx.params [i];
		ctx.params [i] = mono_class_inflate_generic_type_checked (ctx.params [i], ctx.generic_context, error);
		if (!is_ok (error)) {
			char *name = mono_type_full_name (ctx.params [i] ? ctx.params [i] : uninflated);
			ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("Invalid parameter %d of type %s", i, name));
			g_free (name);
			mono_error_cleanup (error);
			/* we must not free (in cleanup) what was not yet allocated (but only copied) */
			ctx.max_args = i;
			goto cleanup;
		}
	}
	stack_init (&ctx, &ctx.eval);

	for (i = 0; i < ctx.num_locals; ++i) {
		if (!mono_type_is_valid_in_context (&ctx, ctx.locals [i]))
			break;
		if (get_stack_type (ctx.locals [i]) == TYPE_INV) {
			char *name = mono_type_full_name (ctx.locals [i]);
			ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("Invalid local %i of type %s", i, name));
			g_free (name);
			break;
		}
		
	}

	for (i = 0; i < ctx.max_args; ++i) {
		if (!mono_type_is_valid_in_context (&ctx, ctx.params [i]))
			break;

		if (get_stack_type (ctx.params [i]) == TYPE_INV) {
			char *name = mono_type_full_name (ctx.params [i]);
			ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("Invalid parameter %i of type %s", i, name));
			g_free (name);
			break;
		}
	}

	if (!ctx.valid)
		goto cleanup;

	for (i = 0; i < ctx.header->num_clauses && ctx.valid; ++i) {
		MonoExceptionClause *clause = ctx.header->clauses + i;
		VERIFIER_DEBUG (printf ("clause try %x len %x filter at %x handler at %x len %x\n", clause->try_offset, clause->try_len, clause->data.filter_offset, clause->handler_offset, clause->handler_len); );

		if (clause->try_offset > ctx.code_size || ADD_IS_GREATER_OR_OVF (clause->try_offset, clause->try_len, ctx.code_size))
			ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("try clause out of bounds at 0x%04x", clause->try_offset));

		if (clause->try_len <= 0)
			ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("try clause len <= 0 at 0x%04x", clause->try_offset));

		if (clause->handler_offset > ctx.code_size || ADD_IS_GREATER_OR_OVF (clause->handler_offset, clause->handler_len, ctx.code_size))
			ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("handler clause out of bounds at 0x%04x", clause->try_offset));

		if (clause->handler_len <= 0)
			ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("handler clause len <= 0 at 0x%04x", clause->try_offset));

		if (clause->try_offset < clause->handler_offset && ADD_IS_GREATER_OR_OVF (clause->try_offset, clause->try_len, HANDLER_START (clause)))
			ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("try block (at 0x%04x) includes handler block (at 0x%04x)", clause->try_offset, clause->handler_offset));

		if (clause->flags == MONO_EXCEPTION_CLAUSE_FILTER) {
			if (clause->data.filter_offset > ctx.code_size)
				ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("filter clause out of bounds at 0x%04x", clause->try_offset));

			if (clause->data.filter_offset >= clause->handler_offset)
				ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("filter clause must come before the handler clause at 0x%04x", clause->data.filter_offset));
		}

		for (n = i + 1; n < ctx.header->num_clauses && ctx.valid; ++n)
			verify_clause_relationship (&ctx, clause, ctx.header->clauses + n);

		if (!ctx.valid)
			break;

		ctx.code [clause->try_offset].flags |= IL_CODE_FLAG_WAS_TARGET;
		if (clause->try_offset + clause->try_len < ctx.code_size)
			ctx.code [clause->try_offset + clause->try_len].flags |= IL_CODE_FLAG_WAS_TARGET;
		if (clause->handler_offset + clause->handler_len < ctx.code_size)
			ctx.code [clause->handler_offset + clause->handler_len].flags |= IL_CODE_FLAG_WAS_TARGET;

		if (clause->flags == MONO_EXCEPTION_CLAUSE_NONE) {
			if (!clause->data.catch_class) {
				ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("Catch clause %d with invalid type", i));
				break;
			}
			if (!mono_type_is_valid_in_context (&ctx, m_class_get_byval_arg (clause->data.catch_class)))
				break;

			init_stack_with_value_at_exception_boundary (&ctx, ctx.code + clause->handler_offset, clause->data.catch_class);
		}
		else if (clause->flags == MONO_EXCEPTION_CLAUSE_FILTER) {
			init_stack_with_value_at_exception_boundary (&ctx, ctx.code + clause->data.filter_offset, mono_defaults.exception_class);
			init_stack_with_value_at_exception_boundary (&ctx, ctx.code + clause->handler_offset, mono_defaults.exception_class);	
		}
	}

	if (!ctx.valid)
		goto cleanup;

	original_bb = bb = mono_basic_block_split (method, error, ctx.header);
	if (!is_ok (error)) {
		ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("Invalid branch target: %s", mono_error_get_message (error)));
		mono_error_cleanup (error);
		goto cleanup;
	}
	g_assert (bb);

	while (ip < end && ctx.valid) {
		int op_size;
		ip_offset = (guint) (ip - code_start);
		{
			const unsigned char *ip_copy = ip;
			MonoOpcodeEnum op;

			if (ip_offset > bb->end) {
				ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("Branch or EH block at [0x%04x] targets middle instruction at 0x%04x", bb->end, ip_offset));
				goto cleanup;
			}

			if (ip_offset == bb->end)
				bb = bb->next;
	
			op_size = mono_opcode_value_and_size (&ip_copy, end, &op);
			if (op_size == -1) {
				ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("Invalid instruction %x at 0x%04x", *ip, ip_offset));
				goto cleanup;
			}

			if (ADD_IS_GREATER_OR_OVF (ip_offset, op_size, bb->end)) {
				ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("Branch or EH block targets middle of instruction at 0x%04x", ip_offset));
				goto cleanup;
			}

			/*Last Instruction*/
			if (ip_offset + op_size == bb->end && mono_opcode_is_prefix (op)) {
				ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("Branch or EH block targets between prefix '%s' and instruction at 0x%04x", mono_opcode_name (op), ip_offset));
				goto cleanup;
			}
		}

		ctx.ip_offset = ip_offset =  (guint) (ip - code_start);

		/*We need to check against fallthrou in and out of protected blocks.
		 * For fallout we check the once a protected block ends, if the start flag is not set.
		 * Likewise for fallthru in, we check if ip is the start of a protected block and start is not set
		 * TODO convert these checks to be done using flags and not this loop
		 */
		for (i = 0; i < ctx.header->num_clauses && ctx.valid; ++i) {
			MonoExceptionClause *clause = ctx.header->clauses + i;

			if ((clause->try_offset + clause->try_len == ip_offset) && start == 0) {
				CODE_NOT_VERIFIABLE (&ctx, g_strdup_printf ("fallthru off try block at 0x%04x", ip_offset));
				start = 1;
			}

			if ((clause->handler_offset + clause->handler_len == ip_offset) && start == 0) {
				if (clause->flags == MONO_EXCEPTION_CLAUSE_FILTER)
					ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("fallout of handler block at 0x%04x", ip_offset));
				else
					CODE_NOT_VERIFIABLE (&ctx, g_strdup_printf ("fallout of handler block at 0x%04x", ip_offset));
				start = 1;
			}

			if (clause->flags == MONO_EXCEPTION_CLAUSE_FILTER && clause->handler_offset == ip_offset && start == 0) {
				ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("fallout of filter block at 0x%04x", ip_offset));
				start = 1;
			}

			if (clause->handler_offset == ip_offset && start == 0) {
				CODE_NOT_VERIFIABLE (&ctx, g_strdup_printf ("fallthru handler block at 0x%04x", ip_offset));
				start = 1;
			}

			if (clause->try_offset == ip_offset && ctx.eval.size > 0 && start == 0) {
				ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("Try to enter try block with a non-empty stack at 0x%04x", ip_offset));
				start = 1;
			}
		}

		/*This must be done after fallthru detection otherwise it won't happen.*/
		if (bb->dead) {
			/*FIXME remove this once we move all bad branch checking code to use BB only*/
			ctx.code [ip_offset].flags |= IL_CODE_FLAG_SEEN;
			ip += op_size;
			continue;
		}

		if (!ctx.valid)
			break;

		if (need_merge) {
			VERIFIER_DEBUG ( printf ("extra merge needed! 0x%04x \n", ctx.target); );
			merge_stacks (&ctx, &ctx.eval, &ctx.code [ctx.target], FALSE, TRUE);
			need_merge = 0;	
		}
		merge_stacks (&ctx, &ctx.eval, &ctx.code[ip_offset], start, FALSE);
		start = 0;

		/*TODO we can fast detect a forward branch or exception block targeting code after prefix, we should fail fast*/
#ifdef MONO_VERIFIER_DEBUG
		{
			char *discode;
			discode = mono_disasm_code_one (NULL, method, ip, NULL);
			discode [strlen (discode) - 1] = 0; /* no \n */
			g_print ("[%d] %-29s (%d)\n",  ip_offset, discode, ctx.eval.size);
			g_free (discode);
		}
		dump_stack_state (&ctx.code [ip_offset]);
		dump_stack_state (&ctx.eval);
#endif

		switch (*ip) {
		case CEE_NOP:
		case CEE_BREAK:
			++ip;
			break;

		case CEE_LDARG_0:
		case CEE_LDARG_1:
		case CEE_LDARG_2:
		case CEE_LDARG_3:
			push_arg (&ctx, *ip - CEE_LDARG_0, FALSE);
			++ip;
			break;

		case CEE_LDARG_S:
		case CEE_LDARGA_S:
			code_bounds_check (2);
			push_arg (&ctx, ip [1],  *ip == CEE_LDARGA_S);
			ip += 2;
			break;

		case CEE_ADD_OVF_UN:
			do_binop (&ctx, *ip, add_ovf_un_table);
			++ip;
			break;

		case CEE_SUB_OVF_UN:
			do_binop (&ctx, *ip, sub_ovf_un_table);
			++ip;
			break;

		case CEE_ADD_OVF:
		case CEE_SUB_OVF:
		case CEE_MUL_OVF:
		case CEE_MUL_OVF_UN:
			do_binop (&ctx, *ip, bin_ovf_table);
			++ip;
			break;

		case CEE_ADD:
			do_binop (&ctx, *ip, add_table);
			++ip;
			break;

		case CEE_SUB:
			do_binop (&ctx, *ip, sub_table);
			++ip;
			break;

		case CEE_MUL:
		case CEE_DIV:
		case CEE_REM:
			do_binop (&ctx, *ip, bin_op_table);
			++ip;
			break;

		case CEE_AND:
		case CEE_DIV_UN:
		case CEE_OR:
		case CEE_REM_UN:
		case CEE_XOR:
			do_binop (&ctx, *ip, int_bin_op_table);
			++ip;
			break;

		case CEE_SHL:
		case CEE_SHR:
		case CEE_SHR_UN:
			do_binop (&ctx, *ip, shift_op_table);
			++ip;
			break;

		case CEE_POP:
			if (!check_underflow (&ctx, 1))
				break;
			stack_pop_safe (&ctx);
			++ip;
			break;

		case CEE_RET:
			do_ret (&ctx);
			++ip;
			start = 1;
			break;

		case CEE_LDLOC_0:
		case CEE_LDLOC_1:
		case CEE_LDLOC_2:
		case CEE_LDLOC_3:
			/*TODO support definite assignment verification? */
			push_local (&ctx, *ip - CEE_LDLOC_0, FALSE);
			++ip;
			break;

		case CEE_STLOC_0:
		case CEE_STLOC_1:
		case CEE_STLOC_2:
		case CEE_STLOC_3:
			store_local (&ctx, *ip - CEE_STLOC_0);
			++ip;
			break;

		case CEE_STLOC_S:
			code_bounds_check (2);
			store_local (&ctx, ip [1]);
			ip += 2;
			break;

		case CEE_STARG_S:
			code_bounds_check (2);
			store_arg (&ctx, ip [1]);
			ip += 2;
			break;

		case CEE_LDC_I4_M1:
		case CEE_LDC_I4_0:
		case CEE_LDC_I4_1:
		case CEE_LDC_I4_2:
		case CEE_LDC_I4_3:
		case CEE_LDC_I4_4:
		case CEE_LDC_I4_5:
		case CEE_LDC_I4_6:
		case CEE_LDC_I4_7:
		case CEE_LDC_I4_8:
			if (check_overflow (&ctx))
				stack_push_val (&ctx, TYPE_I4, mono_get_int32_type ());
			++ip;
			break;

		case CEE_LDC_I4_S:
			code_bounds_check (2);
			if (check_overflow (&ctx))
				stack_push_val (&ctx, TYPE_I4, mono_get_int32_type ());
			ip += 2;
			break;

		case CEE_LDC_I4:
			code_bounds_check (5);
			if (check_overflow (&ctx))
				stack_push_val (&ctx,TYPE_I4, mono_get_int32_type ());
			ip += 5;
			break;

		case CEE_LDC_I8:
			code_bounds_check (9);
			if (check_overflow (&ctx))
				stack_push_val (&ctx,TYPE_I8, m_class_get_byval_arg (mono_defaults.int64_class));
			ip += 9;
			break;

		case CEE_LDC_R4:
			code_bounds_check (5);
			if (check_overflow (&ctx))
				stack_push_val (&ctx, TYPE_R8, m_class_get_byval_arg (mono_defaults.double_class));
			ip += 5;
			break;

		case CEE_LDC_R8:
			code_bounds_check (9);
			if (check_overflow (&ctx))
				stack_push_val (&ctx, TYPE_R8, m_class_get_byval_arg (mono_defaults.double_class));
			ip += 9;
			break;

		case CEE_LDNULL:
			if (check_overflow (&ctx))
				stack_push_val (&ctx, TYPE_COMPLEX | NULL_LITERAL_MASK, mono_get_object_type ());
			++ip;
			break;

		case CEE_BEQ_S:
		case CEE_BNE_UN_S:
			code_bounds_check (2);
			do_branch_op (&ctx, (signed char)ip [1] + 2, cmp_br_eq_op);
			ip += 2;
			need_merge = 1;
			break;

		case CEE_BGE_S:
		case CEE_BGT_S:
		case CEE_BLE_S:
		case CEE_BLT_S:
		case CEE_BGE_UN_S:
		case CEE_BGT_UN_S:
		case CEE_BLE_UN_S:
		case CEE_BLT_UN_S:
			code_bounds_check (2);
			do_branch_op (&ctx, (signed char)ip [1] + 2, cmp_br_op);
			ip += 2;
			need_merge = 1;
			break;

		case CEE_BEQ:
		case CEE_BNE_UN:
			code_bounds_check (5);
			do_branch_op (&ctx, (gint32)read32 (ip + 1) + 5, cmp_br_eq_op);
			ip += 5;
			need_merge = 1;
			break;

		case CEE_BGE:
		case CEE_BGT:
		case CEE_BLE:
		case CEE_BLT:
		case CEE_BGE_UN:
		case CEE_BGT_UN:
		case CEE_BLE_UN:
		case CEE_BLT_UN:
			code_bounds_check (5);
			do_branch_op (&ctx, (gint32)read32 (ip + 1) + 5, cmp_br_op);
			ip += 5;
			need_merge = 1;
			break;

		case CEE_LDLOC_S:
		case CEE_LDLOCA_S:
			code_bounds_check (2);
			push_local (&ctx, ip[1], *ip == CEE_LDLOCA_S);
			ip += 2;
			break;

		case CEE_UNUSED99:
			ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("Use of the `unused' opcode"));
			++ip;
			break; 

		case CEE_DUP: {
			ILStackDesc *top;
			if (!check_underflow (&ctx, 1))
				break;
			if (!check_overflow (&ctx))
				break;
			top = stack_push (&ctx);
			copy_stack_value (top, stack_peek (&ctx, 1));
			++ip;
			break;
		}

		case CEE_JMP:
			code_bounds_check (5);
			if (ctx.eval.size)
				ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("Eval stack must be empty in jmp at 0x%04x", ip_offset));
			/* token = read32 (ip + 1); */
			if (in_any_block (ctx.header, ip_offset))
				ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("jmp cannot escape exception blocks at 0x%04x", ip_offset));

			CODE_NOT_VERIFIABLE (&ctx, g_strdup_printf ("Intruction jmp is not verifiable at 0x%04x", ctx.ip_offset));
			/*
			 * FIXME: check signature, retval, arguments etc.
			 */
			ip += 5;
			break;
		case CEE_CALL:
		case CEE_CALLVIRT:
			code_bounds_check (5);
			do_invoke_method (&ctx, read32 (ip + 1), *ip == CEE_CALLVIRT);
			ip += 5;
			break;

		case CEE_CALLI:
			code_bounds_check (5);
			/* token = read32 (ip + 1); */
			/*
			 * FIXME: check signature, retval, arguments etc.
			 * FIXME: check requirements for tail call
			 */
			CODE_NOT_VERIFIABLE (&ctx, g_strdup_printf ("Intruction calli is not verifiable at 0x%04x", ctx.ip_offset));
			ip += 5;
			break;
		case CEE_BR_S:
			code_bounds_check (2);
			do_static_branch (&ctx, (signed char)ip [1] + 2);
			need_merge = 1;
			ip += 2;
			start = 1;
			break;

		case CEE_BRFALSE_S:
		case CEE_BRTRUE_S:
			code_bounds_check (2);
			do_boolean_branch_op (&ctx, (signed char)ip [1] + 2);
			ip += 2;
			need_merge = 1;
			break;

		case CEE_BR:
			code_bounds_check (5);
			do_static_branch (&ctx, (gint32)read32 (ip + 1) + 5);
			need_merge = 1;
			ip += 5;
			start = 1;
			break;

		case CEE_BRFALSE:
		case CEE_BRTRUE:
			code_bounds_check (5);
			do_boolean_branch_op (&ctx, (gint32)read32 (ip + 1) + 5);
			ip += 5;
			need_merge = 1;
			break;

		case CEE_SWITCH: {
			guint32 entries;
			code_bounds_check (5);
			entries = read32 (ip + 1);

			if (entries > 0xFFFFFFFFU / sizeof (guint32))
				ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("Too many switch entries %x at 0x%04x", entries, ctx.ip_offset));

			ip += 5;
			code_bounds_check (sizeof (guint32) * entries);
			
			do_switch (&ctx, entries, ip);
			ip += sizeof (guint32) * entries;
			break;
		}
		case CEE_LDIND_I1:
		case CEE_LDIND_U1:
		case CEE_LDIND_I2:
		case CEE_LDIND_U2:
		case CEE_LDIND_I4:
		case CEE_LDIND_U4:
		case CEE_LDIND_I8:
		case CEE_LDIND_I:
		case CEE_LDIND_R4:
		case CEE_LDIND_R8:
		case CEE_LDIND_REF:
			do_load_indirect (&ctx, *ip);
			++ip;
			break;
			
		case CEE_STIND_REF:
		case CEE_STIND_I1:
		case CEE_STIND_I2:
		case CEE_STIND_I4:
		case CEE_STIND_I8:
		case CEE_STIND_R4:
		case CEE_STIND_R8:
		case CEE_STIND_I:
			do_store_indirect (&ctx, *ip);
			++ip;
			break;

		case CEE_NOT:
		case CEE_NEG:
			do_unary_math_op (&ctx, *ip);
			++ip;
			break;

		case CEE_CONV_I1:
		case CEE_CONV_I2:
		case CEE_CONV_I4:
		case CEE_CONV_U1:
		case CEE_CONV_U2:
		case CEE_CONV_U4:
			do_conversion (&ctx, TYPE_I4);
			++ip;
			break;			

		case CEE_CONV_I8:
		case CEE_CONV_U8:
			do_conversion (&ctx, TYPE_I8);
			++ip;
			break;			

		case CEE_CONV_R4:
		case CEE_CONV_R8:
		case CEE_CONV_R_UN:
			do_conversion (&ctx, TYPE_R8);
			++ip;
			break;			

		case CEE_CONV_I:
		case CEE_CONV_U:
			do_conversion (&ctx, TYPE_NATIVE_INT);
			++ip;
			break;

		case CEE_CPOBJ:
			code_bounds_check (5);
			do_cpobj (&ctx, read32 (ip + 1));
			ip += 5;
			break;

		case CEE_LDOBJ:
			code_bounds_check (5);
			do_ldobj_value (&ctx, read32 (ip + 1));
			ip += 5;
			break;

		case CEE_LDSTR:
			code_bounds_check (5);
			do_ldstr (&ctx, read32 (ip + 1));
			ip += 5;
			break;

		case CEE_NEWOBJ:
			code_bounds_check (5);
			do_newobj (&ctx, read32 (ip + 1));
			ip += 5;
			break;

		case CEE_CASTCLASS:
		case CEE_ISINST:
			code_bounds_check (5);
			do_cast (&ctx, read32 (ip + 1), *ip == CEE_CASTCLASS ? "castclass" : "isinst");
			ip += 5;
			break;

		case CEE_UNUSED58:
		case CEE_UNUSED1:
			ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("Use of the `unused' opcode"));
			++ip;
			break;

		case CEE_UNBOX:
			code_bounds_check (5);
			do_unbox_value (&ctx, read32 (ip + 1));
			ip += 5;
			break;

		case CEE_THROW:
			do_throw (&ctx);
			start = 1;
			++ip;
			break;

		case CEE_LDFLD:
		case CEE_LDFLDA:
			code_bounds_check (5);
			do_push_field (&ctx, read32 (ip + 1), *ip == CEE_LDFLDA);
			ip += 5;
			break;

		case CEE_LDSFLD:
		case CEE_LDSFLDA:
			code_bounds_check (5);
			do_push_static_field (&ctx, read32 (ip + 1), *ip == CEE_LDSFLDA);
			ip += 5;
			break;

		case CEE_STFLD:
			code_bounds_check (5);
			do_store_field (&ctx, read32 (ip + 1));
			ip += 5;
			break;

		case CEE_STSFLD:
			code_bounds_check (5);
			do_store_static_field (&ctx, read32 (ip + 1));
			ip += 5;
			break;

		case CEE_STOBJ:
			code_bounds_check (5);
			do_stobj (&ctx, read32 (ip + 1));
			ip += 5;
			break;

		case CEE_CONV_OVF_I1_UN:
		case CEE_CONV_OVF_I2_UN:
		case CEE_CONV_OVF_I4_UN:
		case CEE_CONV_OVF_U1_UN:
		case CEE_CONV_OVF_U2_UN:
		case CEE_CONV_OVF_U4_UN:
			do_conversion (&ctx, TYPE_I4);
			++ip;
			break;			

		case CEE_CONV_OVF_I8_UN:
		case CEE_CONV_OVF_U8_UN:
			do_conversion (&ctx, TYPE_I8);
			++ip;
			break;			

		case CEE_CONV_OVF_I_UN:
		case CEE_CONV_OVF_U_UN:
			do_conversion (&ctx, TYPE_NATIVE_INT);
			++ip;
			break;

		case CEE_BOX:
			code_bounds_check (5);
			do_box_value (&ctx, read32 (ip + 1));
			ip += 5;
			break;

		case CEE_NEWARR:
			code_bounds_check (5);
			do_newarr (&ctx, read32 (ip + 1));
			ip += 5;
			break;

		case CEE_LDLEN:
			do_ldlen (&ctx);
			++ip;
			break;

		case CEE_LDELEMA:
			code_bounds_check (5);
			do_ldelema (&ctx, read32 (ip + 1));
			ip += 5;
			break;

		case CEE_LDELEM_I1:
		case CEE_LDELEM_U1:
		case CEE_LDELEM_I2:
		case CEE_LDELEM_U2:
		case CEE_LDELEM_I4:
		case CEE_LDELEM_U4:
		case CEE_LDELEM_I8:
		case CEE_LDELEM_I:
		case CEE_LDELEM_R4:
		case CEE_LDELEM_R8:
		case CEE_LDELEM_REF:
			do_ldelem (&ctx, *ip, 0);
			++ip;
			break;

		case CEE_STELEM_I:
		case CEE_STELEM_I1:
		case CEE_STELEM_I2:
		case CEE_STELEM_I4:
		case CEE_STELEM_I8:
		case CEE_STELEM_R4:
		case CEE_STELEM_R8:
		case CEE_STELEM_REF:
			do_stelem (&ctx, *ip, 0);
			++ip;
			break;

		case CEE_LDELEM:
			code_bounds_check (5);
			do_ldelem (&ctx, *ip, read32 (ip + 1));
			ip += 5;
			break;

		case CEE_STELEM:
			code_bounds_check (5);
			do_stelem (&ctx, *ip, read32 (ip + 1));
			ip += 5;
			break;
			
		case CEE_UNBOX_ANY:
			code_bounds_check (5);
			do_unbox_any (&ctx, read32 (ip + 1));
			ip += 5;
			break;

		case CEE_CONV_OVF_I1:
		case CEE_CONV_OVF_U1:
		case CEE_CONV_OVF_I2:
		case CEE_CONV_OVF_U2:
		case CEE_CONV_OVF_I4:
		case CEE_CONV_OVF_U4:
			do_conversion (&ctx, TYPE_I4);
			++ip;
			break;

		case CEE_CONV_OVF_I8:
		case CEE_CONV_OVF_U8:
			do_conversion (&ctx, TYPE_I8);
			++ip;
			break;

		case CEE_CONV_OVF_I:
		case CEE_CONV_OVF_U:
			do_conversion (&ctx, TYPE_NATIVE_INT);
			++ip;
			break;

		case CEE_REFANYVAL:
			code_bounds_check (5);
			do_refanyval (&ctx, read32 (ip + 1));
			ip += 5;
			break;

		case CEE_CKFINITE:
			do_ckfinite (&ctx);
			++ip;
			break;

		case CEE_MKREFANY:
			code_bounds_check (5);
			do_mkrefany (&ctx,  read32 (ip + 1));
			ip += 5;
			break;

		case CEE_LDTOKEN:
			code_bounds_check (5);
			do_load_token (&ctx, read32 (ip + 1));
			ip += 5;
			break;

		case CEE_ENDFINALLY:
			if (!is_correct_endfinally (ctx.header, ip_offset))
				ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("endfinally must be used inside a finally/fault handler at 0x%04x", ctx.ip_offset));
			ctx.eval.size = 0;
			start = 1;
			++ip;
			break;

		case CEE_LEAVE:
			code_bounds_check (5);
			do_leave (&ctx, read32 (ip + 1) + 5);
			ip += 5;
			start = 1;
			need_merge = 1;
			break;

		case CEE_LEAVE_S:
			code_bounds_check (2);
			do_leave (&ctx, (signed char)ip [1] + 2);
			ip += 2;
			start = 1;
			need_merge = 1;
			break;

		case CEE_PREFIX1:
			code_bounds_check (2);
			++ip;
			switch (*ip) {
			case CEE_STLOC:
				code_bounds_check (3);
				store_local (&ctx, read16 (ip + 1));
				ip += 3;
				break;

			case CEE_CEQ:
				do_cmp_op (&ctx, cmp_br_eq_op, *ip);
				++ip;
				break;

			case CEE_CGT:
			case CEE_CGT_UN:
			case CEE_CLT:
			case CEE_CLT_UN:
				do_cmp_op (&ctx, cmp_br_op, *ip);
				++ip;
				break;

			case CEE_STARG:
				code_bounds_check (3);
				store_arg (&ctx, read16 (ip + 1) );
				ip += 3;
				break;


			case CEE_ARGLIST:
				if (!check_overflow (&ctx))
					break;
				if (ctx.signature->call_convention != MONO_CALL_VARARG)
					ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("Cannot use arglist on method without VARGARG calling convention at 0x%04x", ctx.ip_offset));
				set_stack_value (&ctx, stack_push (&ctx), m_class_get_byval_arg (mono_defaults.argumenthandle_class), FALSE);
				++ip;
				break;
	
			case CEE_LDFTN:
				code_bounds_check (5);
				do_load_function_ptr (&ctx, read32 (ip + 1), FALSE);
				ip += 5;
				break;

			case CEE_LDVIRTFTN:
				code_bounds_check (5);
				do_load_function_ptr (&ctx, read32 (ip + 1), TRUE);
				ip += 5;
				break;

			case CEE_LDARG:
			case CEE_LDARGA:
				code_bounds_check (3);
				push_arg (&ctx, read16 (ip + 1),  *ip == CEE_LDARGA);
				ip += 3;
				break;

			case CEE_LDLOC:
			case CEE_LDLOCA:
				code_bounds_check (3);
				push_local (&ctx, read16 (ip + 1), *ip == CEE_LDLOCA);
				ip += 3;
				break;

			case CEE_LOCALLOC:
				do_localloc (&ctx);
				++ip;
				break;

			case CEE_UNUSED56:
			case CEE_UNUSED57:
			case CEE_UNUSED70:
			case CEE_UNUSED:
				ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("Use of the `unused' opcode"));
				++ip;
				break;
			case CEE_ENDFILTER:
				do_endfilter (&ctx);
				start = 1;
				++ip;
				break;
			case CEE_UNALIGNED_:
				code_bounds_check (2);
				prefix |= PREFIX_UNALIGNED;
				ip += 2;
				break;
			case CEE_VOLATILE_:
				prefix |= PREFIX_VOLATILE;
				++ip;
				break;
			case CEE_TAIL_:
				prefix |= PREFIX_TAIL;
				++ip;
				if (ip < end && (*ip != CEE_CALL && *ip != CEE_CALLI && *ip != CEE_CALLVIRT))
					ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("tail prefix must be used only with call opcodes at 0x%04x", ip_offset));
				break;

			case CEE_INITOBJ:
				code_bounds_check (5);
				do_initobj (&ctx, read32 (ip + 1));
				ip += 5;
				break;

			case CEE_CONSTRAINED_:
				code_bounds_check (5);
				ctx.constrained_type = get_boxable_mono_type (&ctx, read32 (ip + 1), "constrained.");
				prefix |= PREFIX_CONSTRAINED;
				ip += 5;
				break;
	
			case CEE_READONLY_:
				prefix |= PREFIX_READONLY;
				ip++;
				break;

			case CEE_CPBLK:
				CLEAR_PREFIX (&ctx, PREFIX_UNALIGNED | PREFIX_VOLATILE);
				if (!check_underflow (&ctx, 3))
					break;
				CODE_NOT_VERIFIABLE (&ctx, g_strdup_printf ("Instruction cpblk is not verifiable at 0x%04x", ctx.ip_offset));
				ip++;
				break;
				
			case CEE_INITBLK:
				CLEAR_PREFIX (&ctx, PREFIX_UNALIGNED | PREFIX_VOLATILE);
				if (!check_underflow (&ctx, 3))
					break;
				CODE_NOT_VERIFIABLE (&ctx, g_strdup_printf ("Instruction initblk is not verifiable at 0x%04x", ctx.ip_offset));
				ip++;
				break;
				
			case CEE_NO_:
				ip += 2;
				break;
			case CEE_RETHROW:
				if (!is_correct_rethrow (ctx.header, ip_offset))
					ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("rethrow must be used inside a catch handler at 0x%04x", ctx.ip_offset));
				ctx.eval.size = 0;
				start = 1;
				++ip;
				break;

			case CEE_SIZEOF:
				code_bounds_check (5);
				do_sizeof (&ctx, read32 (ip + 1));
				ip += 5;
				break;

			case CEE_REFANYTYPE:
				do_refanytype (&ctx);
				++ip;
				break;

			default:
				ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("Invalid instruction FE %x at 0x%04x", *ip, ctx.ip_offset));
				++ip;
			}
			break;

		default:
			ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("Invalid instruction %x at 0x%04x", *ip, ctx.ip_offset));
			++ip;
		}

		/*TODO we can fast detect a forward branch or exception block targeting code after prefix, we should fail fast*/
		if (prefix) {
			if (!ctx.prefix_set) //first prefix
				ctx.code [ctx.ip_offset].flags |= IL_CODE_FLAG_SEEN;
			ctx.prefix_set |= prefix;
			ctx.has_flags = TRUE;
			prefix = 0;
		} else {
			if (!ctx.has_flags)
				ctx.code [ctx.ip_offset].flags |= IL_CODE_FLAG_SEEN;

			if (ctx.prefix_set & PREFIX_CONSTRAINED)
				ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("Invalid instruction after constrained prefix at 0x%04x", ctx.ip_offset));
			if (ctx.prefix_set & PREFIX_READONLY)
				ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("Invalid instruction after readonly prefix at 0x%04x", ctx.ip_offset));
			if (ctx.prefix_set & PREFIX_VOLATILE)
				ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("Invalid instruction after volatile prefix at 0x%04x", ctx.ip_offset));
			if (ctx.prefix_set & PREFIX_UNALIGNED)
				ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("Invalid instruction after unaligned prefix at 0x%04x", ctx.ip_offset));
			ctx.prefix_set = prefix = 0;
			ctx.has_flags = FALSE;
		}
	}
	/*
	 * if ip != end we overflowed: mark as error.
	 */
	if ((ip != end || !start) && ctx.verifiable && !ctx.list) {
		ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("Run ahead of method code at 0x%04x", ip_offset));
	}

	/*We should guard against the last decoded opcode, otherwise we might add errors that doesn't make sense.*/
	for (i = 0; i < ctx.code_size && i < ip_offset; ++i) {
		if (ctx.code [i].flags & IL_CODE_FLAG_WAS_TARGET) {
			if (!(ctx.code [i].flags & IL_CODE_FLAG_SEEN))
				ADD_VERIFY_ERROR (&ctx, g_strdup_printf ("Branch or exception block target middle of instruction at 0x%04x", i));

			if (ctx.code [i].flags & IL_CODE_DELEGATE_SEQUENCE)
				CODE_NOT_VERIFIABLE (&ctx, g_strdup_printf ("Branch to delegate code sequence at 0x%04x", i));
		}
		if ((ctx.code [i].flags & IL_CODE_LDFTN_DELEGATE_NONFINAL_VIRTUAL) && ctx.has_this_store)
			CODE_NOT_VERIFIABLE (&ctx, g_strdup_printf ("Invalid ldftn with virtual function in method with stdarg 0 at  0x%04x", i));

		if ((ctx.code [i].flags & IL_CODE_CALL_NONFINAL_VIRTUAL) && ctx.has_this_store)
			CODE_NOT_VERIFIABLE (&ctx, g_strdup_printf ("Invalid call to a non-final virtual function in method with stdarg.0 or ldarga.0 at  0x%04x", i));
	}

	if (mono_method_is_constructor (ctx.method) && !ctx.super_ctor_called && !m_class_is_valuetype (ctx.method->klass) && ctx.method->klass != mono_defaults.object_class) {
		char *method_name = mono_method_full_name (ctx.method, TRUE);
		char *type = mono_type_get_full_name (ctx.method->klass);
		if (m_class_get_parent (ctx.method->klass) && mono_class_has_failure (m_class_get_parent (ctx.method->klass)))
			CODE_NOT_VERIFIABLE (&ctx, g_strdup_printf ("Constructor %s for type %s not calling base type ctor due to a TypeLoadException on base type.", method_name, type));
		else
			CODE_NOT_VERIFIABLE (&ctx, g_strdup_printf ("Constructor %s for type %s not calling base type ctor.", method_name, type));
		g_free (method_name);
		g_free (type);
	}

cleanup:
	if (ctx.code) {
		for (i = 0; i < ctx.header->code_size; ++i) {
			if (ctx.code [i].stack)
				g_free (ctx.code [i].stack);
		}
	}

	for (tmp = ctx.funptrs; tmp; tmp = tmp->next)
		g_free (tmp->data);
	g_slist_free (ctx.funptrs);

	for (tmp = ctx.exception_types; tmp; tmp = tmp->next)
		mono_metadata_free_type ((MonoType *)tmp->data);
	g_slist_free (ctx.exception_types);

	for (i = 0; i < ctx.num_locals; ++i) {
		if (ctx.locals [i])
			mono_metadata_free_type (ctx.locals [i]);
	}
	for (i = 0; i < ctx.max_args; ++i) {
		if (ctx.params [i])
			mono_metadata_free_type (ctx.params [i]);
	}

	if (ctx.eval.stack)
		g_free (ctx.eval.stack);
	if (ctx.code)
		g_free (ctx.code);
	g_free (ctx.locals);
	g_free (ctx.locals_verification_state);
	g_free (ctx.params);
	mono_basic_block_free (original_bb);
	mono_metadata_free_mh (ctx.header);

	finish_collect_stats ();
	return ctx.list;
}

char*
mono_verify_corlib ()
{
	/* This is a public API function so cannot be removed */
	return NULL;
}

/**
 * mono_verifier_is_enabled_for_method:
 * \param method the method to probe
 * \returns TRUE if \p method needs to be verified.
 */
gboolean
mono_verifier_is_enabled_for_method (MonoMethod *method)
{
	return mono_verifier_is_enabled_for_class (method->klass) && (method->wrapper_type == MONO_WRAPPER_NONE || method->wrapper_type == MONO_WRAPPER_DYNAMIC_METHOD);
}

/**
 * mono_verifier_is_enabled_for_class:
 * \param klass The \c MonoClass to probe
 * \returns TRUE if \p klass need to be verified.
 */
gboolean
mono_verifier_is_enabled_for_class (MonoClass *klass)
{
	MonoImage *image = m_class_get_image (klass);
	return verify_all || (verifier_mode > MONO_VERIFIER_PE_ONLY && !(image->assembly && image->assembly->in_gac) && image != mono_defaults.corlib);
}

gboolean
mono_verifier_is_enabled_for_image (MonoImage *image)
{
	return verify_all || verifier_mode > MONO_VERIFIER_PE_ONLY;
}

gboolean
mono_verifier_is_enabled_for_pe_only ()
{
	return verify_all || verifier_mode == MONO_VERIFIER_PE_ONLY;
}

/*
 * Dynamic methods are not considered full trust since if the user is trusted and need to
 * generate unsafe code, make the method skip verification - this is a known good way to do it.
 */
gboolean
mono_verifier_is_method_full_trust (MonoMethod *method)
{
	return mono_verifier_is_class_full_trust (method->klass) && !method_is_dynamic (method);
}

/*
 * Returns if @klass is under full trust or not.
 * 
 * TODO This code doesn't take CAS into account.
 * 
 * Under verify_all all user code must be verifiable if no security option was set 
 * 
 */
gboolean
mono_verifier_is_class_full_trust (MonoClass *klass)
{
	MonoImage *image = m_class_get_image (klass);
	/* under CoreCLR code is trusted if it is part of the "platform" otherwise all code inside the GAC is trusted */
	gboolean trusted_location = !mono_security_core_clr_enabled () ?
		(image->assembly && image->assembly->in_gac) : mono_security_core_clr_is_platform_image (image);

	if (verify_all && verifier_mode == MONO_VERIFIER_MODE_OFF)
		return trusted_location || image == mono_defaults.corlib;
	return verifier_mode < MONO_VERIFIER_MODE_VERIFIABLE || trusted_location || image == mono_defaults.corlib;
}

GSList*
mono_method_verify_with_current_settings (MonoMethod *method, gboolean skip_visibility, gboolean is_fulltrust)
{
	return mono_method_verify (method, 
			(verifier_mode != MONO_VERIFIER_MODE_STRICT ? MONO_VERIFY_NON_STRICT: 0)
			| (!is_fulltrust && !mono_verifier_is_method_full_trust (method) ? MONO_VERIFY_FAIL_FAST : 0)
			| (skip_visibility ? MONO_VERIFY_SKIP_VISIBILITY : 0));
}

static int
get_field_end (MonoClassField *field)
{
	int align;
	int size = mono_type_size (field->type, &align);
	if (size == 0)
		size = 4; /*FIXME Is this a safe bet?*/
	return size + field->offset;
}

static gboolean
verify_class_for_overlapping_reference_fields (MonoClass *klass)
{
	int i = 0, j;
	gpointer iter = NULL;
	MonoClassField *field;
	gboolean is_fulltrust = mono_verifier_is_class_full_trust (klass);
	/*We can't skip types with !has_references since this is calculated after we have run.*/
	if (!mono_class_is_explicit_layout (klass))
		return TRUE;


	/*We must check for stuff overlapping reference fields.
	  The outer loop uses mono_class_get_fields_internal to ensure that MonoClass:fields get inited.
	*/
	while ((field = mono_class_get_fields_internal (klass, &iter))) {
		int fieldEnd = get_field_end (field);
		gboolean is_valuetype = !MONO_TYPE_IS_REFERENCE (field->type);
		++i;

		if (mono_field_is_deleted (field) || (field->type->attrs & FIELD_ATTRIBUTE_STATIC))
			continue;

		int fcount = mono_class_get_field_count (klass);
		MonoClassField *klass_fields = m_class_get_fields (klass);
		for (j = i; j < fcount; ++j) {
			MonoClassField *other = &klass_fields [j];
			int otherEnd = get_field_end (other);
			if (mono_field_is_deleted (other) || (is_valuetype && !MONO_TYPE_IS_REFERENCE (other->type)) || (other->type->attrs & FIELD_ATTRIBUTE_STATIC))
				continue;

			if (!is_valuetype && MONO_TYPE_IS_REFERENCE (other->type) && field->offset == other->offset && is_fulltrust)
				continue;

			if ((otherEnd > field->offset && otherEnd <= fieldEnd) || (other->offset >= field->offset && other->offset < fieldEnd))
				return FALSE;
		}
	}
	return TRUE;
}

static guint
field_hash (gconstpointer key)
{
	const MonoClassField *field = (const MonoClassField *)key;
	return g_str_hash (field->name) ^ mono_metadata_type_hash (field->type); /**/
}

static gboolean
field_equals (gconstpointer _a, gconstpointer _b)
{
	const MonoClassField *a = (const MonoClassField *)_a;
	const MonoClassField *b = (const MonoClassField *)_b;
	return !strcmp (a->name, b->name) && mono_metadata_type_equal (a->type, b->type);
}


static gboolean
verify_class_fields (MonoClass *klass)
{
	gpointer iter = NULL;
	MonoClassField *field;
	MonoGenericContext *context = mono_class_get_context (klass);
	GHashTable *unique_fields = g_hash_table_new_full (&field_hash, &field_equals, NULL, NULL);
	if (mono_class_is_gtd (klass))
		context = &mono_class_get_generic_container (klass)->context;

	while ((field = mono_class_get_fields_internal (klass, &iter)) != NULL) {
		if (!mono_type_is_valid_type_in_context (field->type, context)) {
			g_hash_table_destroy (unique_fields);
			return FALSE;
		}
		if (g_hash_table_lookup (unique_fields, field)) {
			g_hash_table_destroy (unique_fields);
			return FALSE;
		}
		g_hash_table_insert (unique_fields, field, field);
	}
	g_hash_table_destroy (unique_fields);
	return TRUE;
}

static gboolean
verify_interfaces (MonoClass *klass)
{
	int i;
	guint16 klass_interface_count = m_class_get_interface_count (klass);
	MonoClass **klass_interfaces = m_class_get_interfaces (klass);
	for (i = 0; i < klass_interface_count; ++i) {
		MonoClass *iface = klass_interfaces [i];
		if (!mono_class_get_flags (iface))
			return FALSE;
	}
	return TRUE;
}

static gboolean
verify_valuetype_layout_with_target (MonoClass *klass, MonoClass *target_class)
{
	int type;
	gpointer iter = NULL;
	MonoClassField *field;
	MonoClass *field_class;

	if (!m_class_is_valuetype (klass))
		return TRUE;

	type = m_class_get_byval_arg (klass)->type;
	/*primitive type fields are not properly decoded*/
	if ((type >= MONO_TYPE_BOOLEAN && type <= MONO_TYPE_R8) || (type >= MONO_TYPE_I && type <= MONO_TYPE_U))
		return TRUE;

	while ((field = mono_class_get_fields_internal (klass, &iter)) != NULL) {
		if (!field->type)
			return FALSE;

		if (field->type->attrs & (FIELD_ATTRIBUTE_STATIC | FIELD_ATTRIBUTE_HAS_FIELD_RVA))
			continue;

		field_class = mono_class_get_generic_type_definition (mono_class_from_mono_type_internal (field->type));

		if (field_class == target_class || klass == field_class || !verify_valuetype_layout_with_target (field_class, target_class))
			return FALSE;
	}

	return TRUE;
}

static gboolean
verify_valuetype_layout (MonoClass *klass)
{
	gboolean res;
	res = verify_valuetype_layout_with_target (klass, klass);
	return res;
}

static gboolean
recursive_mark_constraint_args (MonoBitSet *used_args, MonoGenericContainer *gc, MonoType *type)
{
	int idx;
	MonoClass **constraints;
	MonoGenericParamInfo *param_info;

	g_assert (mono_type_is_generic_argument (type));

	idx = mono_type_get_generic_param_num (type);
	if (mono_bitset_test_fast (used_args, idx))
		return FALSE;

	mono_bitset_set_fast (used_args, idx);
	param_info = mono_generic_container_get_param_info (gc, idx);

	if (!param_info->constraints)
		return TRUE;

	for (constraints = param_info->constraints; *constraints; ++constraints) {
		MonoClass *ctr = *constraints;
		MonoType *constraint_type = m_class_get_byval_arg (ctr);

		if (mono_type_is_generic_argument (constraint_type) && !recursive_mark_constraint_args (used_args, gc, constraint_type))
			return FALSE;
	}
	return TRUE;
}

static gboolean
verify_generic_parameters (MonoClass *klass)
{
	int i;
	MonoGenericContainer *gc = mono_class_get_generic_container (klass);
	MonoBitSet *used_args = mono_bitset_new (gc->type_argc, 0);

	for (i = 0; i < gc->type_argc; ++i) {
		MonoGenericParamInfo *param_info = mono_generic_container_get_param_info (gc, i);
		MonoClass **constraints;

		if (!param_info->constraints)
			continue;

		mono_bitset_clear_all (used_args);
		mono_bitset_set_fast (used_args, i);

		for (constraints = param_info->constraints; *constraints; ++constraints) {
			MonoClass *ctr = *constraints;
			MonoType *constraint_type = m_class_get_byval_arg (ctr);

			if (!mono_class_can_access_class (klass, ctr))
				goto fail;

			if (!mono_type_is_valid_type_in_context (constraint_type, &gc->context))
				goto fail;

			if (mono_type_is_generic_argument (constraint_type) && !recursive_mark_constraint_args (used_args, gc, constraint_type))
				goto fail;
			if (mono_class_is_ginst (ctr) && !mono_class_is_valid_generic_instantiation (NULL, ctr))
				goto fail;
		}
	}
	mono_bitset_free (used_args);
	return TRUE;

fail:
	mono_bitset_free (used_args);
	return FALSE;
}

/*
 * Check if the class is verifiable.
 * 
 * Right now there are no conditions that make a class a valid but not verifiable. Both overlapping reference
 * field and invalid generic instantiation are fatal errors.
 * 
 * This method must be safe to be called from mono_class_init_internal and all code must be carefull about that.
 * 
 */
gboolean
mono_verifier_verify_class (MonoClass *klass)
{
	MonoClass *klass_parent = m_class_get_parent (klass);
	/*Neither <Module>, object or ifaces have parent.*/
	if (!klass_parent &&
		klass != mono_defaults.object_class && 
		!MONO_CLASS_IS_INTERFACE_INTERNAL (klass) &&
		(!image_is_dynamic (m_class_get_image (klass)) && m_class_get_type_token (klass) != 0x2000001)) /*<Module> is the first type in the assembly*/
		return FALSE;
	if (m_class_get_parent (klass)) {
		if (MONO_CLASS_IS_INTERFACE_INTERNAL (klass_parent))
			return FALSE;
		if (!mono_class_is_ginst (klass) && mono_class_is_gtd (klass_parent))
			return FALSE;
		if (mono_class_is_ginst (klass_parent) && !mono_class_is_ginst (klass)) {
			MonoGenericContext *context = mono_class_get_context (klass);
			if (mono_class_is_gtd (klass))
				context = &mono_class_get_generic_container (klass)->context;
			if (!mono_type_is_valid_type_in_context (m_class_get_byval_arg (klass_parent), context))
				return FALSE;
		}
	}
	if (mono_class_is_gtd (klass) && (mono_class_is_explicit_layout (klass)))
		return FALSE;
	if (mono_class_is_gtd (klass) && !verify_generic_parameters (klass))
		return FALSE;
	if (!verify_class_for_overlapping_reference_fields (klass))
		return FALSE;
	if (mono_class_is_ginst (klass) && !mono_class_is_valid_generic_instantiation (NULL, klass))
		return FALSE;
	if (!mono_class_is_ginst (klass) && !verify_class_fields (klass))
		return FALSE;
	if (m_class_is_valuetype (klass) && !verify_valuetype_layout (klass))
		return FALSE;
	if (!verify_interfaces (klass))
		return FALSE;
	return TRUE;
}

#else

gboolean
mono_verifier_verify_class (MonoClass *klass)
{
	/* The verifier was disabled at compile time */
	return TRUE;
}

GSList*
mono_method_verify_with_current_settings (MonoMethod *method, gboolean skip_visibility, gboolean is_fulltrust)
{
	/* The verifier was disabled at compile time */
	return NULL;
}

gboolean
mono_verifier_is_class_full_trust (MonoClass *klass)
{
	/* The verifier was disabled at compile time */
	return TRUE;
}

gboolean
mono_verifier_is_method_full_trust (MonoMethod *method)
{
	/* The verifier was disabled at compile time */
	return TRUE;
}

gboolean
mono_verifier_is_enabled_for_image (MonoImage *image)
{
	/* The verifier was disabled at compile time */
	return FALSE;
}

gboolean
mono_verifier_is_enabled_for_class (MonoClass *klass)
{
	/* The verifier was disabled at compile time */
	return FALSE;
}

gboolean
mono_verifier_is_enabled_for_method (MonoMethod *method)
{
	/* The verifier was disabled at compile time */
	return FALSE;
}

GSList*
mono_method_verify (MonoMethod *method, int level)
{
	/* The verifier was disabled at compile time */
	return NULL;
}

void
mono_free_verify_list (GSList *list)
{
	/* The verifier was disabled at compile time */
	/* will always be null if verifier is disabled */
}

#endif
