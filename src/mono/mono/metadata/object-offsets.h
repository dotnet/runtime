
/**
\file
This is a parameterized header. It's supposed/ok to be included multiple times.

Input defines: (those to be defined by the includer file)

Required:
DECL_OFFSET(struct,field)
DECL_OFFSET2(struct,field,offset)
DECL_ALIGN2(name,alignment)

Optional:
USE_CROSS_COMPILE_OFFSETS - if defined, force the cross compiler offsets to be used, otherwise
	they will only be used if MONO_CROSS_COMPILE is defined
DISABLE_METADATA_OFFSETS - Disable the definition of offsets for structures defined in metadata/.
DISABLE_JIT_OFFSETS - Disable the definition of offsets for structures defined in mini/.

The last two are needed because metadata shouldn't include JIT offsets since the structures
are not defined, while the JIT shouldn't include metadata offsets, since some of them
are GC specific, and the JIT needs to remain GC agnostic.

Output defines:

HAS_CROSS_COMPILER_OFFSETS - if set, it means we found some cross offsets, it doesnt mean we'll use it.
USED_CROSS_COMPILER_OFFSETS - if set, it means we used the cross offsets

Environment defines (from config.h and CFLAGS):

MONO_GENERATING_OFFSETS - Set by an offsets generating tool to disable the usage of any (possibly non-existing) generated header.
MONO_OFFSETS_FILE - Name of the header file containing the offsets to be used.

*/


#undef HAS_CROSS_COMPILER_OFFSETS
#undef USED_CROSS_COMPILER_OFFSETS

#if !defined (MONO_GENERATING_OFFSETS) && defined (MONO_OFFSETS_FILE)
#include MONO_OFFSETS_FILE
#endif

#ifndef USED_CROSS_COMPILER_OFFSETS

DECL_SIZE(gint8)
DECL_SIZE(gint16)
DECL_SIZE(gint32)
DECL_SIZE(gint64)
DECL_SIZE(float)
DECL_SIZE(double)
DECL_SIZE(gpointer)

// Offsets for structures defined in metadata/
#ifndef DISABLE_METADATA_OFFSETS
DECL_OFFSET(MonoObject, vtable)
DECL_OFFSET(MonoObject, synchronisation)

DECL_OFFSET(MonoClass, interface_bitmap)
DECL_OFFSET(MonoClass, _byval_arg)
DECL_OFFSET(MonoClass, cast_class)
DECL_OFFSET(MonoClass, element_class)
DECL_OFFSET(MonoClass, idepth)
DECL_OFFSET(MonoClass, instance_size)
DECL_OFFSET(MonoClass, interface_id)
DECL_OFFSET(MonoClass, max_interface_id)
DECL_OFFSET(MonoClass, parent)
DECL_OFFSET(MonoClass, rank)
DECL_OFFSET(MonoClass, sizes)
DECL_OFFSET(MonoClass, supertypes)
DECL_OFFSET(MonoClass, class_kind)

DECL_OFFSET(MonoVTable, klass)
DECL_OFFSET(MonoVTable, max_interface_id)
DECL_OFFSET(MonoVTable, interface_bitmap)
DECL_OFFSET(MonoVTable, vtable)
DECL_OFFSET(MonoVTable, rank)
DECL_OFFSET(MonoVTable, initialized)
DECL_OFFSET(MonoVTable, flags)
DECL_OFFSET(MonoVTable, type)
DECL_OFFSET(MonoVTable, runtime_generic_context)

DECL_OFFSET(MonoDomain, stack_overflow_ex)

DECL_OFFSET(MonoDelegate, target)
DECL_OFFSET(MonoDelegate, method_ptr)
DECL_OFFSET(MonoDelegate, invoke_impl)
DECL_OFFSET(MonoDelegate, method)
DECL_OFFSET(MonoDelegate, method_code)
DECL_OFFSET(MonoDelegate, method_is_virtual)
DECL_OFFSET(MonoDelegate, extra_arg)

DECL_OFFSET(MonoInternalThread, tid)
DECL_OFFSET(MonoInternalThread, small_id)
DECL_OFFSET(MonoInternalThread, static_data)
DECL_OFFSET(MonoInternalThread, last)

DECL_OFFSET(MonoMulticastDelegate, delegates)

DECL_OFFSET(MonoTransparentProxy, rp)
DECL_OFFSET(MonoTransparentProxy, remote_class)
DECL_OFFSET(MonoTransparentProxy, custom_type_info)

DECL_OFFSET(MonoRealProxy, target_domain_id)
DECL_OFFSET(MonoRealProxy, context)
DECL_OFFSET(MonoRealProxy, unwrapped_server)

DECL_OFFSET(MonoRemoteClass, proxy_class)

DECL_OFFSET(MonoArray, vector)
DECL_OFFSET(MonoArray, max_length)
DECL_OFFSET(MonoArray, bounds)

DECL_OFFSET(MonoArrayBounds, lower_bound)
DECL_OFFSET(MonoArrayBounds, length)

DECL_OFFSET(MonoSafeHandle, handle)

DECL_OFFSET(MonoHandleRef, handle)

DECL_OFFSET(MonoComInteropProxy, com_object)

DECL_OFFSET(MonoString, length)
DECL_OFFSET(MonoString, chars)

DECL_OFFSET(MonoException, message)
DECL_OFFSET(MonoException, caught_in_unmanaged)

DECL_OFFSET(MonoTypedRef, type)
DECL_OFFSET(MonoTypedRef, klass)
DECL_OFFSET(MonoTypedRef, value)

//Internal structs
DECL_OFFSET(MonoThreadsSync, status)
DECL_OFFSET(MonoThreadsSync, nest)

DECL_OFFSET(MonoProfilerCallContext, method)
DECL_OFFSET(MonoProfilerCallContext, return_value)
DECL_OFFSET(MonoProfilerCallContext, args)

#ifdef HAVE_SGEN_GC
DECL_OFFSET(SgenClientThreadInfo, in_critical_region)
DECL_OFFSET(SgenThreadInfo, tlab_next)
DECL_OFFSET(SgenThreadInfo, tlab_temp_end)
#endif

#endif //DISABLE METADATA OFFSETS

// Offsets for structures defined in mini/
#ifndef DISABLE_JIT_OFFSETS
DECL_SIZE(MonoMethodRuntimeGenericContext)
DECL_SIZE(MonoLMF)
DECL_SIZE(MonoLMFExt)
DECL_SIZE(MonoTypedRef)
DECL_SIZE(CallContext)
DECL_SIZE(MonoContext)

DECL_OFFSET(MonoLMF, previous_lmf)
DECL_OFFSET(MonoLMFExt, kind)
DECL_OFFSET(MonoLMFExt, il_state)

DECL_OFFSET(MonoMethodILState, method)
DECL_OFFSET(MonoMethodILState, il_offset)
DECL_OFFSET(MonoMethodILState, data)

DECL_OFFSET(MonoMethodRuntimeGenericContext, class_vtable)
DECL_OFFSET(MonoMethodRuntimeGenericContext, entries)
DECL_OFFSET(MonoMethodRuntimeGenericContext, infos)

DECL_OFFSET(MonoJitTlsData, lmf)
DECL_OFFSET(MonoJitTlsData, class_cast_from)
DECL_OFFSET(MonoJitTlsData, class_cast_to)
#ifdef TARGET_WIN32
DECL_OFFSET(MonoJitTlsData, stack_restore_ctx)
#endif

DECL_OFFSET(MonoGSharedVtMethodRuntimeInfo, locals_size)
DECL_OFFSET(MonoGSharedVtMethodRuntimeInfo, entries) //XXX more to fix here

DECL_OFFSET(MonoDelegateTrampInfo, method)
DECL_OFFSET(MonoDelegateTrampInfo, invoke_impl)
DECL_OFFSET(MonoDelegateTrampInfo, method_ptr)

// Architecture-specific offsets
// -----------------------------

#if defined(TARGET_WASM)
DECL_OFFSET(MonoContext, wasm_ip)
DECL_OFFSET(MonoContext, wasm_bp)
DECL_OFFSET(MonoContext, wasm_sp)
DECL_OFFSET(MonoContext, llvm_exc_reg)

DECL_OFFSET(MonoLMF, lmf_addr)
DECL_OFFSET(MonoLMF, method)

#elif defined(TARGET_X86)
DECL_OFFSET(MonoContext, eax)
DECL_OFFSET(MonoContext, ebx)
DECL_OFFSET(MonoContext, ecx)
DECL_OFFSET(MonoContext, edx)
DECL_OFFSET(MonoContext, edi)
DECL_OFFSET(MonoContext, esi)
DECL_OFFSET(MonoContext, esp)
DECL_OFFSET(MonoContext, ebp)
DECL_OFFSET(MonoContext, eip)

DECL_OFFSET(MonoLMF, method)
DECL_OFFSET(MonoLMF, lmf_addr)
DECL_OFFSET(MonoLMF, esp)
DECL_OFFSET(MonoLMF, ebx)
DECL_OFFSET(MonoLMF, edi)
DECL_OFFSET(MonoLMF, esi)
DECL_OFFSET(MonoLMF, ebp)
DECL_OFFSET(MonoLMF, eip)
#elif defined(TARGET_AMD64)
DECL_OFFSET(MonoContext, gregs)
DECL_OFFSET(MonoContext, fregs)

DECL_OFFSET(MonoLMF, rsp)
DECL_OFFSET(MonoLMF, rbp)

DECL_OFFSET(DynCallArgs, res)
DECL_OFFSET(DynCallArgs, fregs)
DECL_OFFSET(DynCallArgs, has_fp)
DECL_OFFSET(DynCallArgs, nstack_args)
DECL_OFFSET(DynCallArgs, regs)

DECL_OFFSET(MonoLMFTramp, ctx)
DECL_OFFSET(MonoLMFTramp, lmf_addr)
#elif defined(TARGET_ARM)
DECL_OFFSET(MonoLMF, sp)
DECL_OFFSET(MonoLMF, fp)
DECL_OFFSET(MonoLMF, ip)
DECL_OFFSET(MonoLMF, iregs)
DECL_OFFSET(MonoLMF, fregs)
DECL_OFFSET(DynCallArgs, fpregs)
DECL_OFFSET(DynCallArgs, has_fpregs)
DECL_OFFSET(DynCallArgs, regs)
DECL_OFFSET(DynCallArgs, n_stackargs)
DECL_OFFSET(SeqPointInfo, ss_tramp_addr)
#elif defined(TARGET_ARM64)
DECL_OFFSET(MonoLMF, pc)
DECL_OFFSET(MonoLMF, gregs)
DECL_OFFSET(DynCallArgs, regs)
DECL_OFFSET(DynCallArgs, fpregs)
DECL_OFFSET(DynCallArgs, n_stackargs)
DECL_OFFSET(DynCallArgs, n_fpargs)
DECL_OFFSET(DynCallArgs, n_fpret)
#elif defined(TARGET_S390X)
DECL_OFFSET(MonoLMF, pregs)
DECL_OFFSET(MonoLMF, lmf_addr)
DECL_OFFSET(MonoLMF, method)
DECL_OFFSET(MonoLMF, ebp)
DECL_OFFSET(MonoLMF, eip)
DECL_OFFSET(MonoLMF, gregs)
DECL_OFFSET(MonoLMF, fregs)
#elif defined(TARGET_RISCV)
DECL_OFFSET(MonoContext, gregs)
DECL_OFFSET(MonoContext, fregs)
#endif

// Shared architecture offfsets
// ----------------------------

#if defined(TARGET_ARM) || defined(TARGET_ARM64)
DECL_OFFSET (MonoContext, pc)
DECL_OFFSET (MonoContext, regs)
DECL_OFFSET (MonoContext, fregs)

DECL_OFFSET(MonoLMF, lmf_addr)

DECL_OFFSET(DynCallArgs, res)
DECL_OFFSET(DynCallArgs, res2)
#endif

#if defined(TARGET_ARM)
DECL_OFFSET(MonoLMF, method)
DECL_OFFSET(GSharedVtCallInfo, stack_usage)
DECL_OFFSET(GSharedVtCallInfo, vret_arg_reg)
DECL_OFFSET(GSharedVtCallInfo, ret_marshal)
DECL_OFFSET(GSharedVtCallInfo, vret_slot)
DECL_OFFSET(GSharedVtCallInfo, gsharedvt_in)

DECL_OFFSET(SeqPointInfo, ss_trigger_page)
#endif

#if defined(TARGET_ARM64)
DECL_OFFSET (MonoContext, has_fregs)

DECL_OFFSET(GSharedVtCallInfo, stack_usage)
DECL_OFFSET(GSharedVtCallInfo, gsharedvt_in)
DECL_OFFSET(GSharedVtCallInfo, ret_marshal)
DECL_OFFSET(GSharedVtCallInfo, vret_slot)
#endif

#if defined(TARGET_AMD64) || defined(TARGET_ARM64)
DECL_OFFSET(SeqPointInfo, ss_tramp_addr)
#endif

#if defined(TARGET_AMD64) || defined(TARGET_ARM) || defined(TARGET_ARM64)
DECL_OFFSET(SeqPointInfo, bp_addrs)

DECL_OFFSET(CallContext, gregs)
DECL_OFFSET(CallContext, fregs)
DECL_OFFSET(CallContext, stack_size)
DECL_OFFSET(CallContext, stack)
#endif

#if defined(TARGET_X86)
DECL_OFFSET(CallContext, eax)
DECL_OFFSET(CallContext, edx)
DECL_OFFSET(CallContext, fret)
DECL_OFFSET(CallContext, stack_size)
DECL_OFFSET(CallContext, stack)
#endif

#if defined(TARGET_X86)
DECL_OFFSET(GSharedVtCallInfo, stack_usage)
DECL_OFFSET(GSharedVtCallInfo, vret_slot)
DECL_OFFSET(GSharedVtCallInfo, vret_arg_slot)
DECL_OFFSET(GSharedVtCallInfo, ret_marshal)
DECL_OFFSET(GSharedVtCallInfo, gsharedvt_in)
#endif

#if defined(TARGET_AMD64)
DECL_OFFSET(GSharedVtCallInfo, ret_marshal)
DECL_OFFSET(GSharedVtCallInfo, vret_arg_reg)
DECL_OFFSET(GSharedVtCallInfo, vret_slot)
DECL_OFFSET(GSharedVtCallInfo, stack_usage)
DECL_OFFSET(GSharedVtCallInfo, gsharedvt_in)
#endif

DECL_OFFSET(MonoFtnDesc, arg)
DECL_OFFSET(MonoFtnDesc, addr)

#endif //DISABLE_JIT_OFFSETS

#endif //USED_CROSS_COMPILER_OFFSETS

#undef DECL_OFFSET
#undef DECL_OFFSET2
#undef DECL_ALIGN2
#undef DECL_SIZE
#undef DECL_SIZE2
#undef USE_CROSS_COMPILE_OFFSETS
