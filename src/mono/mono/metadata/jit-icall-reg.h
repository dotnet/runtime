/**
 * \file
 *   Enum for JIT icalls: MonoJitICallId MONO_JIT_ICALL_mono_foo, etc.
 *   Static storage for JIT icall info: mono_get_jit_icall_info().
 *
 *   mono_find_jit_icall_info (MonoJitICallId)
 *     Convert enum to pointer.
 *
 *   mono_find_jit_icall_info ((MonoJitICallId)int)
 *     Convert int to pointer.
 *
 *  mono_jit_icall_info_id (MonoJitICallInfo*)
 *     Convert pointer to enum.
 *
 *   mono_jit_icall_info_index (MonoJitICallInfo*)
 *     Convert pointer to int.
 *
 *   &mono_get_icall_info ()->name
 *     Convert name to pointer.
 *
 *   MONO_JIT_ICALL_ ## name
 *     Convert name to enum.
 *
 *   All conversions are just a few instructions.
 *
 * Author:
 *   Jay Krell (jaykrell@microsoft.com)
 *
 * Copyright 2019 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

// No include guard needed.

// Changes within MONO_JIT_ICALLS require revising MONO_AOT_FILE_VERSION.
#define MONO_JIT_ICALLS \
	\
MONO_JIT_ICALL (ZeroIsReserved)	\
	\
/* These must be ordered like MonoTrampolineType. */	\
MONO_JIT_ICALL (generic_trampoline_jit)	\
MONO_JIT_ICALL (generic_trampoline_jump)	\
MONO_JIT_ICALL (generic_trampoline_rgctx_lazy_fetch)	\
MONO_JIT_ICALL (generic_trampoline_aot)	\
MONO_JIT_ICALL (generic_trampoline_aot_plt)	\
MONO_JIT_ICALL (generic_trampoline_delegate)	\
MONO_JIT_ICALL (generic_trampoline_vcall)	\
	\
/* These must be ordered like MonoTlsKey (alphabetical). */ \
MONO_JIT_ICALL (mono_tls_get_domain_extern) \
MONO_JIT_ICALL (mono_tls_get_jit_tls_extern) \
MONO_JIT_ICALL (mono_tls_get_lmf_addr_extern) \
MONO_JIT_ICALL (mono_tls_get_sgen_thread_info_extern) \
MONO_JIT_ICALL (mono_tls_get_thread_extern) \
	\
MONO_JIT_ICALL (__emul_fadd)	\
MONO_JIT_ICALL (__emul_fcmp_ceq)	\
MONO_JIT_ICALL (__emul_fcmp_cgt)	\
MONO_JIT_ICALL (__emul_fcmp_cgt_un)	\
MONO_JIT_ICALL (__emul_fcmp_clt)	\
MONO_JIT_ICALL (__emul_fcmp_clt_un)	\
MONO_JIT_ICALL (__emul_fcmp_eq)	\
MONO_JIT_ICALL (__emul_fcmp_ge)	\
MONO_JIT_ICALL (__emul_fcmp_ge_un)	\
MONO_JIT_ICALL (__emul_fcmp_gt)	\
MONO_JIT_ICALL (__emul_fcmp_gt_un)	\
MONO_JIT_ICALL (__emul_fcmp_le)	\
MONO_JIT_ICALL (__emul_fcmp_le_un)	\
MONO_JIT_ICALL (__emul_fcmp_lt)	\
MONO_JIT_ICALL (__emul_fcmp_lt_un)	\
MONO_JIT_ICALL (__emul_fcmp_ne_un)	\
MONO_JIT_ICALL (__emul_fconv_to_i)	\
MONO_JIT_ICALL (__emul_fconv_to_i1)	\
MONO_JIT_ICALL (__emul_fconv_to_i2)	\
MONO_JIT_ICALL (__emul_fconv_to_i4)	\
MONO_JIT_ICALL (__emul_fconv_to_i8)	\
MONO_JIT_ICALL (__emul_fconv_to_ovf_i8)	\
MONO_JIT_ICALL (__emul_fconv_to_ovf_u8)	\
MONO_JIT_ICALL (__emul_fconv_to_r4)	\
MONO_JIT_ICALL (__emul_fconv_to_u)	\
MONO_JIT_ICALL (__emul_fconv_to_u1)	\
MONO_JIT_ICALL (__emul_fconv_to_u2)	\
MONO_JIT_ICALL (__emul_fconv_to_u4)	\
MONO_JIT_ICALL (__emul_fconv_to_u8)	\
MONO_JIT_ICALL (__emul_fdiv)	\
MONO_JIT_ICALL (__emul_fmul)	\
MONO_JIT_ICALL (__emul_fneg)	\
MONO_JIT_ICALL (__emul_frem)	\
MONO_JIT_ICALL (__emul_fsub)	\
MONO_JIT_ICALL (__emul_iconv_to_r_un) \
MONO_JIT_ICALL (__emul_iconv_to_r4) \
MONO_JIT_ICALL (__emul_iconv_to_r8) \
MONO_JIT_ICALL (__emul_lconv_to_r4) \
MONO_JIT_ICALL (__emul_lconv_to_r8) \
MONO_JIT_ICALL (__emul_lconv_to_r8_un) \
MONO_JIT_ICALL (__emul_ldiv) \
MONO_JIT_ICALL (__emul_ldiv_un) \
MONO_JIT_ICALL (__emul_lmul) \
MONO_JIT_ICALL (__emul_lmul_ovf) \
MONO_JIT_ICALL (__emul_lmul_ovf_un) \
MONO_JIT_ICALL (__emul_lmul_ovf_un_oom) \
MONO_JIT_ICALL (__emul_lrem) \
MONO_JIT_ICALL (__emul_lrem_un) \
MONO_JIT_ICALL (__emul_lshl) \
MONO_JIT_ICALL (__emul_lshr) \
MONO_JIT_ICALL (__emul_lshr_un) \
MONO_JIT_ICALL (__emul_op_idiv) \
MONO_JIT_ICALL (__emul_op_idiv_un) \
MONO_JIT_ICALL (__emul_op_imul) \
MONO_JIT_ICALL (__emul_op_imul_ovf) \
MONO_JIT_ICALL (__emul_op_imul_ovf_un) \
MONO_JIT_ICALL (__emul_op_imul_ovf_un_oom) \
MONO_JIT_ICALL (__emul_op_irem) \
MONO_JIT_ICALL (__emul_op_irem_un) \
MONO_JIT_ICALL (__emul_rconv_to_i8) \
MONO_JIT_ICALL (__emul_rconv_to_ovf_i8) \
MONO_JIT_ICALL (__emul_rconv_to_ovf_u8) \
MONO_JIT_ICALL (__emul_rconv_to_u4)	\
MONO_JIT_ICALL (__emul_rconv_to_u8) \
MONO_JIT_ICALL (__emul_rrem) \
MONO_JIT_ICALL (cominterop_get_ccw) \
MONO_JIT_ICALL (cominterop_get_ccw_object) \
MONO_JIT_ICALL (cominterop_get_function_pointer) \
MONO_JIT_ICALL (cominterop_get_interface) \
MONO_JIT_ICALL (cominterop_get_method_interface) \
MONO_JIT_ICALL (cominterop_object_is_rcw) \
MONO_JIT_ICALL (cominterop_restore_domain) \
MONO_JIT_ICALL (cominterop_set_ccw_object_domain) \
MONO_JIT_ICALL (cominterop_type_from_handle) \
MONO_JIT_ICALL (g_free) \
MONO_JIT_ICALL (interp_to_native_trampoline)	\
MONO_JIT_ICALL (mini_llvm_init_method) \
MONO_JIT_ICALL (mini_llvmonly_init_delegate) \
MONO_JIT_ICALL (mini_llvmonly_init_delegate_virtual) \
MONO_JIT_ICALL (mini_llvmonly_init_vtable_slot) \
MONO_JIT_ICALL (mini_llvmonly_resolve_generic_virtual_call) \
MONO_JIT_ICALL (mini_llvmonly_resolve_generic_virtual_iface_call) \
MONO_JIT_ICALL (mini_llvmonly_resolve_iface_call_gsharedvt) \
MONO_JIT_ICALL (mini_llvmonly_resolve_vcall_gsharedvt) \
MONO_JIT_ICALL (mini_llvmonly_resolve_vcall_gsharedvt_fast) \
MONO_JIT_ICALL (mini_llvmonly_throw_nullref_exception) \
MONO_JIT_ICALL (mini_llvmonly_throw_aot_failed_exception) \
MONO_JIT_ICALL (mini_llvmonly_interp_entry_gsharedvt) \
MONO_JIT_ICALL (mini_llvmonly_throw_exception) \
MONO_JIT_ICALL (mini_llvmonly_rethrow_exception) \
MONO_JIT_ICALL (mini_llvmonly_throw_corlib_exception) \
MONO_JIT_ICALL (mini_llvmonly_resume_exception_il_state) \
MONO_JIT_ICALL (mono_amd64_resume_unwind)	\
MONO_JIT_ICALL (mono_amd64_start_gsharedvt_call)	\
MONO_JIT_ICALL (mono_amd64_throw_corlib_exception)	\
MONO_JIT_ICALL (mono_amd64_throw_exception)	\
MONO_JIT_ICALL (mono_arch_rethrow_exception) \
MONO_JIT_ICALL (mono_arch_throw_corlib_exception) \
MONO_JIT_ICALL (mono_arch_throw_exception) \
MONO_JIT_ICALL (mono_arm_resume_unwind)	\
MONO_JIT_ICALL (mono_arm_start_gsharedvt_call)	\
MONO_JIT_ICALL (mono_arm_throw_exception)	\
MONO_JIT_ICALL (mono_arm_throw_exception_by_token)	\
MONO_JIT_ICALL (mono_arm_unaligned_stack)	\
MONO_JIT_ICALL (mono_array_new_1) \
MONO_JIT_ICALL (mono_array_new_2) \
MONO_JIT_ICALL (mono_array_new_3) \
MONO_JIT_ICALL (mono_array_new_4) \
MONO_JIT_ICALL (mono_array_new_n_icall) \
MONO_JIT_ICALL (mono_array_to_byte_byvalarray) \
MONO_JIT_ICALL (mono_array_to_lparray) \
MONO_JIT_ICALL (mono_array_to_savearray) \
MONO_JIT_ICALL (mono_break) \
MONO_JIT_ICALL (mono_byvalarray_to_byte_array) \
MONO_JIT_ICALL (mono_chkstk_win64) \
MONO_JIT_ICALL (mono_ckfinite) \
MONO_JIT_ICALL (mono_class_interface_match) \
MONO_JIT_ICALL (mono_class_static_field_address) \
MONO_JIT_ICALL (mono_compile_method_icall) \
MONO_JIT_ICALL (mono_context_get_icall) \
MONO_JIT_ICALL (mono_context_set_icall) \
MONO_JIT_ICALL (mono_create_corlib_exception_0) \
MONO_JIT_ICALL (mono_create_corlib_exception_1) \
MONO_JIT_ICALL (mono_create_corlib_exception_2) \
MONO_JIT_ICALL (mono_debug_personality) \
MONO_JIT_ICALL (mono_debugger_agent_breakpoint_from_context) \
MONO_JIT_ICALL (mono_debugger_agent_single_step_from_context) \
MONO_JIT_ICALL (mono_debugger_agent_user_break) \
MONO_JIT_ICALL (mono_delegate_begin_invoke) \
MONO_JIT_ICALL (mono_delegate_end_invoke) \
MONO_JIT_ICALL (mono_delegate_to_ftnptr) \
MONO_JIT_ICALL (mono_domain_get) \
MONO_JIT_ICALL (mono_dummy_jit_icall) \
MONO_JIT_ICALL (mono_dummy_jit_icall_val) \
MONO_JIT_ICALL (mono_exception_from_token) \
MONO_JIT_ICALL (mono_fill_class_rgctx) \
MONO_JIT_ICALL (mono_fill_method_rgctx) \
MONO_JIT_ICALL (mono_fload_r4) \
MONO_JIT_ICALL (mono_fload_r4_arg) \
MONO_JIT_ICALL (mono_free_bstr) \
MONO_JIT_ICALL (mono_free_lparray) \
MONO_JIT_ICALL (mono_fstore_r4) \
MONO_JIT_ICALL (mono_ftnptr_to_delegate) \
MONO_JIT_ICALL (mono_gc_alloc_obj) \
MONO_JIT_ICALL (mono_gc_alloc_string) \
MONO_JIT_ICALL (mono_gc_alloc_vector) \
MONO_JIT_ICALL (mono_gc_wbarrier_generic_nostore_internal) \
MONO_JIT_ICALL (mono_gc_wbarrier_range_copy) \
MONO_JIT_ICALL (mono_gchandle_get_target_internal) \
MONO_JIT_ICALL (mono_generic_class_init) \
MONO_JIT_ICALL (mono_get_assembly_object) \
MONO_JIT_ICALL (mono_get_method_object) \
MONO_JIT_ICALL (mono_get_native_calli_wrapper) \
MONO_JIT_ICALL (mono_get_special_static_data) \
MONO_JIT_ICALL (mono_gsharedvt_constrained_call) \
MONO_JIT_ICALL (mono_gsharedvt_value_copy) \
MONO_JIT_ICALL (mono_helper_compile_generic_method) \
MONO_JIT_ICALL (mono_helper_ldstr) \
MONO_JIT_ICALL (mono_helper_ldstr_mscorlib) \
MONO_JIT_ICALL (mono_helper_newobj_mscorlib) \
MONO_JIT_ICALL (mono_helper_stelem_ref_check) \
MONO_JIT_ICALL (mono_init_vtable_slot) \
MONO_JIT_ICALL (mono_interp_entry_from_trampoline) \
MONO_JIT_ICALL (mono_interp_to_native_trampoline) \
MONO_JIT_ICALL (mono_isfinite_double) \
MONO_JIT_ICALL (mono_ldftn) \
MONO_JIT_ICALL (mono_ldtoken_wrapper) \
MONO_JIT_ICALL (mono_ldtoken_wrapper_generic_shared) \
MONO_JIT_ICALL (mono_ldvirtfn) \
MONO_JIT_ICALL (mono_ldvirtfn_gshared) \
MONO_JIT_ICALL (mono_llvm_resume_unwind_trampoline) \
MONO_JIT_ICALL (mono_llvm_rethrow_exception_trampoline) \
MONO_JIT_ICALL (mono_llvm_set_unhandled_exception_handler) \
MONO_JIT_ICALL (mono_llvm_throw_corlib_exception_abs_trampoline) \
MONO_JIT_ICALL (mono_llvm_throw_corlib_exception_trampoline) \
MONO_JIT_ICALL (mono_llvm_throw_exception_trampoline) \
MONO_JIT_ICALL (mono_llvmonly_init_delegate) \
MONO_JIT_ICALL (mono_llvmonly_init_delegate_virtual) \
MONO_JIT_ICALL (mono_marshal_asany) \
MONO_JIT_ICALL (mono_marshal_check_domain_image) \
MONO_JIT_ICALL (mono_marshal_clear_last_error) \
MONO_JIT_ICALL (mono_marshal_free) \
MONO_JIT_ICALL (mono_marshal_free_array) \
MONO_JIT_ICALL (mono_marshal_free_asany) \
MONO_JIT_ICALL (mono_marshal_get_type_object) \
MONO_JIT_ICALL (mono_marshal_isinst_with_cache) \
MONO_JIT_ICALL (mono_marshal_safearray_begin) \
MONO_JIT_ICALL (mono_marshal_safearray_create) \
MONO_JIT_ICALL (mono_marshal_safearray_end) \
MONO_JIT_ICALL (mono_marshal_safearray_free_indices) \
MONO_JIT_ICALL (mono_marshal_safearray_get_value) \
MONO_JIT_ICALL (mono_marshal_safearray_next) \
MONO_JIT_ICALL (mono_marshal_safearray_set_value) \
MONO_JIT_ICALL (mono_marshal_set_domain_by_id) \
MONO_JIT_ICALL (mono_marshal_set_last_error) \
MONO_JIT_ICALL (mono_marshal_set_last_error_windows) \
MONO_JIT_ICALL (mono_marshal_string_to_utf16) \
MONO_JIT_ICALL (mono_marshal_string_to_utf16_copy) \
MONO_JIT_ICALL (mono_monitor_enter_fast) \
MONO_JIT_ICALL (mono_monitor_enter_internal) \
MONO_JIT_ICALL (mono_monitor_enter_v4_fast) \
MONO_JIT_ICALL (mono_monitor_enter_v4_internal) \
MONO_JIT_ICALL (mono_object_castclass_unbox) \
MONO_JIT_ICALL (mono_object_castclass_with_cache) \
MONO_JIT_ICALL (mono_object_isinst_icall) \
MONO_JIT_ICALL (mono_object_isinst_with_cache) \
MONO_JIT_ICALL (mono_ppc_throw_exception)	\
MONO_JIT_ICALL (mono_profiler_raise_exception_clause) \
MONO_JIT_ICALL (mono_profiler_raise_gc_allocation) \
MONO_JIT_ICALL (mono_profiler_raise_method_enter) \
MONO_JIT_ICALL (mono_profiler_raise_method_leave) \
MONO_JIT_ICALL (mono_profiler_raise_method_tail_call) \
MONO_JIT_ICALL (mono_resolve_generic_virtual_call) \
MONO_JIT_ICALL (mono_resolve_generic_virtual_iface_call) \
MONO_JIT_ICALL (mono_resolve_iface_call_gsharedvt) \
MONO_JIT_ICALL (mono_resolve_vcall_gsharedvt) \
MONO_JIT_ICALL (mono_resume_unwind) \
MONO_JIT_ICALL (mono_rethrow_preserve_exception) \
MONO_JIT_ICALL (mono_string_builder_to_utf16) \
MONO_JIT_ICALL (mono_string_builder_to_utf8) \
MONO_JIT_ICALL (mono_string_from_ansibstr) \
MONO_JIT_ICALL (mono_string_from_bstr_icall) \
MONO_JIT_ICALL (mono_string_from_byvalstr) \
MONO_JIT_ICALL (mono_string_from_byvalwstr) \
MONO_JIT_ICALL (mono_string_from_tbstr) \
MONO_JIT_ICALL (mono_string_new_len_wrapper) \
MONO_JIT_ICALL (mono_string_new_wrapper_internal) \
MONO_JIT_ICALL (mono_string_to_ansibstr) \
MONO_JIT_ICALL (mono_string_to_bstr) \
MONO_JIT_ICALL (mono_string_to_byvalstr) \
MONO_JIT_ICALL (mono_string_to_byvalwstr) \
MONO_JIT_ICALL (mono_string_to_tbstr) \
MONO_JIT_ICALL (mono_string_to_utf16_internal) \
MONO_JIT_ICALL (mono_string_to_utf8str) \
MONO_JIT_ICALL (mono_string_utf16_to_builder) \
MONO_JIT_ICALL (mono_string_utf16_to_builder2) \
MONO_JIT_ICALL (mono_string_utf8_to_builder) \
MONO_JIT_ICALL (mono_string_utf8_to_builder2) \
MONO_JIT_ICALL (mono_struct_delete_old) \
MONO_JIT_ICALL (mono_thread_force_interruption_checkpoint_noraise) \
MONO_JIT_ICALL (mono_thread_get_undeniable_exception) \
MONO_JIT_ICALL (mono_thread_interruption_checkpoint) \
MONO_JIT_ICALL (mono_threads_attach_coop) \
MONO_JIT_ICALL (mono_threads_detach_coop) \
MONO_JIT_ICALL (mono_threads_enter_gc_safe_region_unbalanced) \
MONO_JIT_ICALL (mono_threads_enter_gc_unsafe_region_unbalanced) \
MONO_JIT_ICALL (mono_threads_exit_gc_safe_region_unbalanced) \
MONO_JIT_ICALL (mono_threads_exit_gc_unsafe_region_unbalanced) \
MONO_JIT_ICALL (mono_threads_state_poll) \
MONO_JIT_ICALL (mono_throw_exception) \
MONO_JIT_ICALL (mono_throw_method_access) \
MONO_JIT_ICALL (mono_throw_ambiguous_implementation) \
MONO_JIT_ICALL (mono_throw_bad_image) \
MONO_JIT_ICALL (mono_throw_not_supported) \
MONO_JIT_ICALL (mono_throw_platform_not_supported) \
MONO_JIT_ICALL (mono_throw_invalid_program) \
MONO_JIT_ICALL (mono_trace_enter_method) \
MONO_JIT_ICALL (mono_trace_leave_method) \
MONO_JIT_ICALL (mono_trace_tail_method) \
MONO_JIT_ICALL (mono_upgrade_remote_class_wrapper) \
MONO_JIT_ICALL (mono_value_copy_internal) \
MONO_JIT_ICALL (mono_x86_start_gsharedvt_call)	\
MONO_JIT_ICALL (mono_x86_throw_corlib_exception)	\
MONO_JIT_ICALL (mono_x86_throw_exception)	\
MONO_JIT_ICALL (mini_init_method_rgctx)		\
MONO_JIT_ICALL (native_to_interp_trampoline)	\
MONO_JIT_ICALL (personality) \
MONO_JIT_ICALL (pthread_getspecific) \
MONO_JIT_ICALL (rgctx_fetch_trampoline_general)	\
MONO_JIT_ICALL (sdb_breakpoint_trampoline)	\
MONO_JIT_ICALL (sdb_single_step_trampoline)	\
MONO_JIT_ICALL (type_from_handle) \
MONO_JIT_ICALL (ves_icall_array_new_specific) \
MONO_JIT_ICALL (ves_icall_marshal_alloc) \
MONO_JIT_ICALL (ves_icall_mono_delegate_ctor) \
MONO_JIT_ICALL (ves_icall_mono_delegate_ctor_interp) \
MONO_JIT_ICALL (ves_icall_mono_string_from_utf16) \
MONO_JIT_ICALL (ves_icall_mono_string_to_utf8) \
MONO_JIT_ICALL (ves_icall_object_new) \
MONO_JIT_ICALL (ves_icall_object_new_specific) \
MONO_JIT_ICALL (ves_icall_runtime_class_init) \
MONO_JIT_ICALL (ves_icall_string_alloc) \
MONO_JIT_ICALL (ves_icall_string_new_wrapper) \
MONO_JIT_ICALL (ves_icall_thread_finish_async_abort) \
MONO_JIT_ICALL (mono_marshal_lookup_pinvoke) \
	\
MONO_JIT_ICALL (count) \

#define MONO_JIT_ICALL_mono_get_lmf_addr MONO_JIT_ICALL_mono_tls_get_lmf_addr_extern

typedef enum MonoJitICallId
{
#define MONO_JIT_ICALL(x) MONO_JIT_ICALL_ ## x,
MONO_JIT_ICALLS
#undef MONO_JIT_ICALL
} MonoJitICallId;

typedef union MonoJitICallInfos {
	struct {
#define MONO_JIT_ICALL(x) MonoJitICallInfo x;
MONO_JIT_ICALLS
#undef MONO_JIT_ICALL
	};
	MonoJitICallInfo array [MONO_JIT_ICALL_count];
} MonoJitICallInfos;

extern MonoJitICallInfos mono_jit_icall_info;

#define mono_get_jit_icall_info() (&mono_jit_icall_info)

// Convert MonoJitICallInfo* to an int or enum.
//
#define mono_jit_icall_info_index(x) ((x) - mono_get_jit_icall_info ()->array)
#define mono_jit_icall_info_id(x) ((MonoJitICallId)mono_jit_icall_info_index(x))

// Given an enum/id, get the MonoJitICallInfo*.
//
static inline MonoJitICallInfo*
mono_find_jit_icall_info (MonoJitICallId id)
{
	const guint index = (guint)id;

	g_assert (index < MONO_JIT_ICALL_count);
	g_static_assert (MONO_JIT_ICALL_count < 0x200); // fits in 9 bits

	return &mono_get_jit_icall_info ()->array [index];
}

#if __cplusplus
// MonoJumpInfo.jit_icall_id is gsize instead of MonoJitICallId in order
// to fully overlap pointers, and not match union reads with writes.
inline MonoJitICallInfo*
mono_find_jit_icall_info (gsize id)
{
	return mono_find_jit_icall_info ((MonoJitICallId)id);
}
#endif
