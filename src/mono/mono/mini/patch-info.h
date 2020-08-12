PATCH_INFO(BB, "bb")
PATCH_INFO(ABS, "abs")
PATCH_INFO(LABEL, "label")
PATCH_INFO(METHOD, "method")
PATCH_INFO(METHOD_JUMP, "method_jump")
PATCH_INFO(METHODCONST, "methodconst")
// Either the address of a C function implementing a JIT icall, or a wrapper around it
PATCH_INFO(JIT_ICALL_ID, "jit_icall_id") // replaced MONO_PATCH_INFO_JIT_ICALL, using enum instead of string
PATCH_INFO(SWITCH, "switch")
PATCH_INFO(EXC, "exc")
PATCH_INFO(EXC_NAME, "exc_name")
PATCH_INFO(CLASS, "class")
PATCH_INFO(IMAGE, "image")
PATCH_INFO(FIELD, "field")
PATCH_INFO(VTABLE, "vtable")
PATCH_INFO(SFLDA, "sflda")
PATCH_INFO(LDSTR, "ldstr")
PATCH_INFO(LDTOKEN, "ldtoken")
PATCH_INFO(TYPE_FROM_HANDLE, "type_from_handle")
PATCH_INFO(R4, "r4")
PATCH_INFO(R8, "r8")
PATCH_INFO(IP, "ip")
PATCH_INFO(IID, "iid")
PATCH_INFO(ADJUSTED_IID, "adjusted_iid")
PATCH_INFO(BB_OVF, "bb_ovf")
PATCH_INFO(EXC_OVF, "exc_ovf")
PATCH_INFO(GOT_OFFSET, "got_offset")
PATCH_INFO(DECLSEC, "declsec")
PATCH_INFO(RVA, "rva")
PATCH_INFO(DELEGATE_TRAMPOLINE, "delegate_trampoline")
PATCH_INFO(ICALL_ADDR, "icall_addr")
/* The address of a C function implementing a JIT icall */
PATCH_INFO(JIT_ICALL_ADDR, "jit_icall_addr")
PATCH_INFO(INTERRUPTION_REQUEST_FLAG, "interruption_request_flag")
PATCH_INFO(METHOD_RGCTX, "method_rgctx")
PATCH_INFO(RGCTX_FETCH, "rgctx_fetch")
PATCH_INFO(RGCTX_SLOT_INDEX, "rgctx_slot_index")
PATCH_INFO(MSCORLIB_GOT_ADDR, "mscorlib_got_addr")
PATCH_INFO(SEQ_POINT_INFO, "seq_point_info")
PATCH_INFO(GC_CARD_TABLE_ADDR, "gc_card_table_addr")
PATCH_INFO(CASTCLASS_CACHE, "castclass_cache")
PATCH_INFO(SIGNATURE, "signature")
PATCH_INFO(GSHAREDVT_CALL, "gsharedvt_call")
PATCH_INFO(GSHAREDVT_METHOD, "gsharedvt_method")
PATCH_INFO(OBJC_SELECTOR_REF, "objc_selector_ref")
PATCH_INFO(METHOD_CODE_SLOT, "method_code_slot")
PATCH_INFO(LDSTR_LIT, "ldstr_lit")
PATCH_INFO(GC_NURSERY_START, "gc_nursery_start")
PATCH_INFO(VIRT_METHOD, "virt_method")
PATCH_INFO(GC_SAFE_POINT_FLAG, "gc_safe_point_flag")
PATCH_INFO(NONE, "none")
PATCH_INFO(AOT_MODULE, "aot_module")
PATCH_INFO(AOT_JIT_INFO, "aot_jit_info")
PATCH_INFO(GC_NURSERY_BITS, "gc_nursery_bits")
PATCH_INFO(GSHAREDVT_IN_WRAPPER, "gsharedvt_in_wrapper")
PATCH_INFO(ICALL_ADDR_CALL, "icall_addr_call")
/*
 * The address of a C function implementing a JIT icall.
 * Same as JIT_ICALL_ADDR, but not treated as a call.
 */
PATCH_INFO(JIT_ICALL_ADDR_NOCALL, "jit_icall_addr_nocall")
PATCH_INFO(PROFILER_ALLOCATION_COUNT, "profiler_allocation_count")
PATCH_INFO(PROFILER_CLAUSE_COUNT, "profiler_clause_count")
/*
 * A MonoFtnDesc for calling amethod.
 * This either points to native code or to an interp entry
 * function.
 */
PATCH_INFO(METHOD_FTNDESC, "method_ftndesc")

PATCH_INFO(SPECIFIC_TRAMPOLINE_LAZY_FETCH_ADDR, "specific_trampoline_lazy_fetch_addr")

/* mscorlib_amodule->info.specific_trampolines */
PATCH_INFO(SPECIFIC_TRAMPOLINES, "specific_trampolines")
/* Address of got slot block in mscorlib_amodule->got belonging to specific trampolines */
PATCH_INFO(SPECIFIC_TRAMPOLINES_GOT_SLOTS_BASE, "specific_trampolines_got_slots_base")

/*
 * PATCH_INFO_R4/R8 is handled by mono_arch_emit_exceptions () by emitting the data
 * into the text segment after the method body. These patches emit the data
 * elsewhere.
 */
PATCH_INFO(R8_GOT, "r8_got")
PATCH_INFO(R4_GOT, "r4_got")

/* MonoMethod* -> the address of a memory slot which is used to cache the pinvoke address */
PATCH_INFO(METHOD_PINVOKE_ADDR_CACHE, "pinvoke_addr_cache")
