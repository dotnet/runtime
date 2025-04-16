// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// This file contains icalls used in jitted interpreter traces and wrappers,
//  along with infrastructure to support code generration

#ifndef __USE_ISOC99
#define __USE_ISOC99
#endif
#include "config.h"

void jiterp_preserve_module (void);

// NOTE: All code in this file needs to be guarded with HOST_BROWSER, since
//  we don't run non-wasm tests for changes to this file!

#if HOST_BROWSER

#if 0
#define jiterp_assert(b) g_assert(b)
#else
#define jiterp_assert(b)
#endif

#include <assert.h>
#include <stdatomic.h>
#include <emscripten.h>

#include <string.h>
#include <stdlib.h>
#include <math.h>

#ifndef DISABLE_THREADS
#include <pthread.h>
#endif

#include <mono/metadata/mono-config.h>
#include <mono/utils/mono-threads.h>
#include <mono/metadata/gc-internals.h>
#include <mono/metadata/class-abi-details.h>

#include "interp.h"
#include "interp-internals.h"
#include "mintops.h"
#include "transform.h"
#include "interp-intrins.h"
#include "tiering.h"

#include <mono/utils/mono-math.h>
#include <mono/mini/mini.h>
#include <mono/mini/mini-runtime.h>
#include <mono/mini/aot-runtime.h>
#include <mono/mini/llvm-runtime.h>
#include <mono/mini/llvmonly-runtime.h>
#include <mono/utils/options.h>
#include <mono/utils/atomic.h>
#include <mono/utils/mono-tls.h>

#include "jiterpreter.h"

// We disable this diagnostic because EMSCRIPTEN_KEEPALIVE makes it a false alarm, the keepalive
//  functions are being used externally. Having a bunch of prototypes is pointless since these
//  functions are not consumed by C anywhere else
#pragma clang diagnostic ignored "-Wmissing-prototypes"

int mono_jiterp_first_trace_fn_ptr = 0;

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_encode_leb64_ref (unsigned char * destination, void * source, int valueIsSigned) {	
		return 0;	
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_encode_leb52 (unsigned char * destination, double doubleValue, int valueIsSigned) {	
		return 0;
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_encode_leb_signed_boundary (unsigned char * destination, int bits, int sign) {	
		return 0;	
}

// Many of the following functions implement various opcodes or provide support for opcodes
//  so that jiterpreter traces don't have to inline dozens of wasm instructions worth of
//  complex logic - these are designed to match interp.c

// If a trace is jitted for a method that hasn't been tiered yet, we need to
//  update the interpreter entry count for the method.
EMSCRIPTEN_KEEPALIVE int
mono_jiterp_increase_entry_count (void *_imethod) {
	return 0;
}

EMSCRIPTEN_KEEPALIVE void*
mono_jiterp_object_unbox (MonoObject *obj) {
	return (void*)0;
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_type_is_byref (MonoType *type) {
	return 0;		
}

EMSCRIPTEN_KEEPALIVE void
mono_jiterp_value_copy (void *dest, void *src, MonoClass *klass) {	
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_try_newobj_inlined (MonoObject **destination, MonoVTable *vtable) {
	return 0;	
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_try_newstr (MonoString **destination, int length) {
	return 0;
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_try_newarr (MonoArray **destination, MonoVTable *vtable, int length) {
	return 0;
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_gettype_ref (
	MonoObject **destination, MonoObject **source
) {
	return 0;
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_has_parent_fast (
	MonoClass *klass, MonoClass *parent
) {
	return 0;
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_implements_interface (
	MonoVTable *vtable, MonoClass *klass
) {
	return 0;
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_is_special_interface (MonoClass *klass)
{
	return 0;
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_implements_special_interface (
	MonoObject *obj, MonoVTable *vtable, MonoClass *klass
) {
	return 0;
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_cast_v2 (
	MonoObject **destination, MonoObject *obj,
	MonoClass *klass, MintOpcode opcode
) {
	return 0;
}

EMSCRIPTEN_KEEPALIVE void
mono_jiterp_localloc (gpointer *destination, gint32 len, InterpFrame *frame)
{	
}

EMSCRIPTEN_KEEPALIVE void
mono_jiterp_ldtsflda (gpointer *destination, guint32 offset) {	
}

EMSCRIPTEN_KEEPALIVE void
mono_jiterp_box_ref (MonoVTable *vtable, MonoObject **dest, void *src, gboolean vt) {
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_conv (void *dest, void *src, int opcode) {
	return 0;
}

// keep in sync with jiterpreter-opcodes.ts
enum {
	JITERP_CNE_UN_R4 = (0xFFFF + 0),
	JITERP_CGE_UN_R4,
	JITERP_CLE_UN_R4,
	JITERP_CNE_UN_R8,
	JITERP_CGE_UN_R8,
	JITERP_CLE_UN_R8,
};

#define JITERP_RELOP(opcode, type, op, noorder) \
	case opcode: \
		{ \
			if (is_unordered) \
				return noorder; \
			else \
				return ((type)lhs op (type)rhs); \
		}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_relop_fp (double lhs, double rhs, int opcode) {
	return 0;
}

#undef JITERP_RELOP

EMSCRIPTEN_KEEPALIVE size_t
mono_jiterp_get_size_of_stackval () {
	return 0;
}

// jiterpreter-interp-entry.ts uses this information to decide whether to call
//  stackval_from_data for a given type or just do a raw value copy of N bytes
EMSCRIPTEN_KEEPALIVE int
mono_jiterp_type_get_raw_value_size (MonoType *type) {
	return 0;
}

// we use these helpers to record when a trace bails out (in countBailouts mode)
EMSCRIPTEN_KEEPALIVE void
mono_jiterp_trace_bailout (int reason)
{	
}

EMSCRIPTEN_KEEPALIVE double
mono_jiterp_get_trace_bailout_count (int reason)
{
	return 0;
}

// we use this to record how many times a trace has aborted due to a given opcode.
// this is done in C because the heuristic updates it along with typescript updating it
EMSCRIPTEN_KEEPALIVE gint32
mono_jiterp_adjust_abort_count (MintOpcode opcode, gint32 delta) {
	return 0;
}

// at the start of a jitted interp_entry wrapper, this is called to perform initial setup
//  like resolving the target for delegates and setting up the thread context
// inlining this into the wrappers would make them unnecessarily big and complex
EMSCRIPTEN_KEEPALIVE stackval *
mono_jiterp_interp_entry_prologue (JiterpEntryData *data, void *this_arg)
{
	return (void*)0;
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_get_opcode_value_table_entry (int opcode) {
	return 0;
}

void
jiterp_insert_entry_points (void *_imethod, void *_td)
{
	
}

EMSCRIPTEN_KEEPALIVE double
mono_jiterp_get_trace_hit_count (gint32 trace_index) {
	return 0;
}

MONO_NEVER_INLINE JiterpreterThunk
mono_interp_tier_prepare_jiterpreter_fast (
	void *_frame, const guint16 *ip
) {	
	return (JiterpreterThunk)(void*)0;
}

void
mono_jiterp_free_method_data (MonoMethod *method, InterpMethod *imethod)
{	
}

EMSCRIPTEN_KEEPALIVE gboolean
mono_jiterp_parse_option (const char *option)
{	
	return FALSE;
}

EMSCRIPTEN_KEEPALIVE gint32
mono_jiterp_get_options_version ()
{
	return 0;
}

EMSCRIPTEN_KEEPALIVE char *
mono_jiterp_get_options_as_json ()
{
	return (char*)0;
}

EMSCRIPTEN_KEEPALIVE gint32
mono_jiterp_get_option_as_int (const char *name)
{
	return 0;
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_object_has_component_size (MonoObject **ppObj)
{
	return 0;
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_get_hashcode (MonoObject ** ppObj)
{
	return 0;
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_try_get_hashcode (MonoObject ** ppObj)
{
	return 0;
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_get_signature_has_this (MonoMethodSignature *sig)
{
	return 0;
}

EMSCRIPTEN_KEEPALIVE MonoType *
mono_jiterp_get_signature_return_type (MonoMethodSignature *sig)
{
	return sig->ret;
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_get_signature_param_count (MonoMethodSignature *sig)
{
	return 0;
}

EMSCRIPTEN_KEEPALIVE MonoType **
mono_jiterp_get_signature_params (MonoMethodSignature *sig)
{
	return 0;
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_type_to_ldind (MonoType *type)
{
	return 0;
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_type_to_stind (MonoType *type)
{
	return 0;
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_get_array_rank (gint32 *dest, MonoObject **src)
{
	return 0;
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_get_array_element_size (gint32 *dest, MonoObject **src)
{
	return 0;
}

// Returns 1 on success so that the trace can do br_if to bypass its bailout
EMSCRIPTEN_KEEPALIVE int
mono_jiterp_set_object_field (
	uint8_t *locals, guint32 fieldOffsetBytes,
	guint32 targetLocalOffsetBytes, guint32 sourceLocalOffsetBytes
) {
	return 0;
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_debug_count ()
{
	return 0;
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_stelem_ref (
	MonoArray *o, gint32 aindex, MonoObject *ref
) {
	return 0;
}


// keep in sync with jiterpreter-enums.ts JiterpMember
enum {
	JITERP_MEMBER_VT_INITIALIZED = 0,
	JITERP_MEMBER_ARRAY_DATA,
	JITERP_MEMBER_STRING_LENGTH,
	JITERP_MEMBER_STRING_DATA,
	JITERP_MEMBER_IMETHOD,
	JITERP_MEMBER_DATA_ITEMS,
	JITERP_MEMBER_RMETHOD,
	JITERP_MEMBER_SPAN_LENGTH,
	JITERP_MEMBER_SPAN_DATA,
	JITERP_MEMBER_ARRAY_LENGTH,
	// Kept as-is but no longer implemented
	JITERP_MEMBER_BACKWARD_BRANCH_OFFSETS,
	// Ditto
	JITERP_MEMBER_BACKWARD_BRANCH_OFFSETS_COUNT,
	JITERP_MEMBER_CLAUSE_DATA_OFFSETS,
	JITERP_MEMBER_PARAMS_COUNT,
	JITERP_MEMBER_VTABLE,
	JITERP_MEMBER_VTABLE_KLASS,
	JITERP_MEMBER_CLASS_RANK,
	JITERP_MEMBER_CLASS_ELEMENT_CLASS,
	JITERP_MEMBER_BOXED_VALUE_DATA,
	JITERP_MEMBER_BACKWARD_BRANCH_TAKEN,
	JITERP_MEMBER_BAILOUT_OPCODE_COUNT,
};


// we use these helpers at JIT time to figure out where to do memory loads and stores
EMSCRIPTEN_KEEPALIVE size_t
mono_jiterp_get_member_offset (int member) {
	return 0;
}

// keep in sync with jiterpreter-enums.ts JiterpCounter
enum {
	JITERP_COUNTER_TRACE_CANDIDATES = 0,
	JITERP_COUNTER_TRACES_COMPILED,
	JITERP_COUNTER_ENTRY_WRAPPERS_COMPILED,
	JITERP_COUNTER_JIT_CALLS_COMPILED,
	JITERP_COUNTER_DIRECT_JIT_CALLS_COMPILED,
	JITERP_COUNTER_FAILURES,
	JITERP_COUNTER_BYTES_GENERATED,
	JITERP_COUNTER_NULL_CHECKS_ELIMINATED,
	JITERP_COUNTER_NULL_CHECKS_FUSED,
	JITERP_COUNTER_BACK_BRANCHES_EMITTED,
	JITERP_COUNTER_BACK_BRANCHES_NOT_EMITTED,
	JITERP_COUNTER_ELAPSED_GENERATION,
	JITERP_COUNTER_ELAPSED_COMPILATION,
	JITERP_COUNTER_SWITCH_TARGETS_OK,
	JITERP_COUNTER_SWITCH_TARGETS_FAILED,
	JITERP_COUNTER_MAX = JITERP_COUNTER_SWITCH_TARGETS_FAILED
};

#define JITERP_COUNTER_UNIT 100

static long counters[JITERP_COUNTER_MAX + 1] = {0};

static long *
mono_jiterp_get_counter_address (int counter) {
	return 0;
}

EMSCRIPTEN_KEEPALIVE double
mono_jiterp_get_counter (int counter) {
	return 0;
}

EMSCRIPTEN_KEEPALIVE double
mono_jiterp_modify_counter (int counter, double delta) {
	return 0;
}


EMSCRIPTEN_KEEPALIVE void
mono_jiterp_write_number_unaligned (void *dest, double value, int mode) {	
}

MONO_NEVER_INLINE ptrdiff_t
mono_jiterp_monitor_trace (const guint16 *ip, void *_frame, void *locals)
{
	return 0;
}

EMSCRIPTEN_KEEPALIVE gint32
mono_jiterp_get_rejected_trace_count ()
{
	return 0;
}

EMSCRIPTEN_KEEPALIVE void
mono_jiterp_boost_back_branch_target (const JiterpreterOpcode *ip) {	
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_is_imethod_var_address_taken (InterpMethod *imethod, int offset) {
	return 0;
}

EMSCRIPTEN_KEEPALIVE void
mono_jiterp_initialize_table (int type, int first_index, int last_index) {	
}

EMSCRIPTEN_KEEPALIVE int
mono_jiterp_allocate_table_entry (int type) {
	return 0;
}

int
mono_jiterp_increment_counter (volatile int *counter) {
	return 0;
}

gboolean
mono_jiterp_patch_opcode (volatile JiterpreterOpcode *ip, guint16 old_opcode, guint16 new_opcode) {
	return FALSE;
}


// Purges this item from all queues
void
mono_jiterp_tlqueue_purge_all (gpointer item) {	
}

EMSCRIPTEN_KEEPALIVE gpointer
mono_jiterp_tlqueue_next (int queue) {	
	return (gpointer)0;
}

// Adds a new item to the end of the queue and returns the new size of the queue
EMSCRIPTEN_KEEPALIVE int
mono_jiterp_tlqueue_add (int queue, gpointer item) {
	return 0;
}

EMSCRIPTEN_KEEPALIVE void
mono_jiterp_tlqueue_clear (int queue) {	
}

EMSCRIPTEN_KEEPALIVE
#else
int
mono_jiterp_is_enabled (void);
#endif // HOST_BROWSER

int
mono_jiterp_is_enabled (void) {
	return 0;
}

void
jiterp_preserve_module (void) {
}
