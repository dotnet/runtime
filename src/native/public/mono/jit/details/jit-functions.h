// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// This file does not have ifdef guards, it is meant to be included multiple times with different definitions of MONO_API_FUNCTION
#ifndef MONO_API_FUNCTION
#error "MONO_API_FUNCTION(ret,name,args) macro not defined before including function declaration header"
#endif

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoDomain *, mono_jit_init, (const char *root_domain_name))

/**
 * This function is deprecated, use mono_jit_init instead. Ignores runtime_version parameter.
 */
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoDomain *, mono_jit_init_version, (const char *root_domain_name, const char *runtime_version))

MONO_API_FUNCTION(MonoDomain *, mono_jit_init_version_for_test_only, (const char *root_domain_name, const char *runtime_version))

MONO_API_FUNCTION(int, mono_jit_exec, (MonoDomain *domain, MonoAssembly *assembly, int argc, char *argv[]))
MONO_API_FUNCTION(void, mono_jit_cleanup, (MonoDomain *domain))

MONO_API_FUNCTION(mono_bool, mono_jit_set_trace_options, (const char* options))

MONO_API_FUNCTION(void, mono_set_signal_chaining, (mono_bool chain_signals))

MONO_API_FUNCTION(void, mono_set_crash_chaining, (mono_bool chain_signals))

/**
 * This function is deprecated, use mono_jit_set_aot_mode instead.
 */
MONO_API_FUNCTION(void, mono_jit_set_aot_only, (mono_bool aot_only))

MONO_API_FUNCTION(void, mono_jit_set_aot_mode, (MonoAotMode mode))

/*
 * Returns whether the runtime was invoked for the purpose of AOT-compiling an
 * assembly, i.e. no managed code will run.
 */
MONO_API_FUNCTION(mono_bool, mono_jit_aot_compiling, (void))

MONO_API_FUNCTION(void, mono_set_break_policy, (MonoBreakPolicyFunc policy_callback))

MONO_API_FUNCTION(void, mono_jit_parse_options, (int argc, char * argv[]))

MONO_API_FUNCTION(char*, mono_get_runtime_build_info, (void))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_set_use_llvm, (mono_bool use_llvm))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_aot_register_module, (void **aot_info))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoDomain*, mono_jit_thread_attach, (MonoDomain *domain))
