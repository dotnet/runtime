#ifndef DO_API_NO_RETURN
#define DO_API_NO_RETURN(a, b, c) DO_API(a,b,c)
#endif

#ifndef DO_API_OPTIONAL
#define DO_API_OPTIONAL(a, b, c) DO_API(a,b,c)
#endif

typedef UNUSED_SYMBOL void(*MonoUnityExceptionFunc) (MonoObject* exc);

// If you add functions to this file you also need to expose them in MonoBundle.exp
// Otherwise they wont be exported in the web plugin!
DO_API(gboolean, mono_unity_class_has_failure, (MonoClass * klass))
DO_API(void, mono_thread_suspend_all_other_threads, ())
DO_API(void, mono_thread_pool_cleanup, ())
DO_API(void, mono_threads_set_shutting_down, ())
DO_API(void, mono_runtime_set_shutting_down, ())
DO_API(gboolean, mono_runtime_is_shutting_down, ())
DO_API(gboolean, mono_domain_finalize, (MonoDomain * domain, int timeout))
DO_API(void, mono_runtime_cleanup, (MonoDomain * domain))
DO_API(MonoMethod*, mono_object_get_virtual_method, (MonoObject * obj, MonoMethod * method))

DO_API(void, mono_add_internal_call, (const char *name, gconstpointer method))
DO_API(void, mono_unity_jit_cleanup, (MonoDomain * domain))
DO_API(MonoDomain*, mono_jit_init_version, (const char *file, const char* runtime_version))
DO_API(void*, mono_jit_info_get_code_start, (void* jit))
DO_API(int, mono_jit_info_get_code_size, (void* jit))
DO_API(MonoClass *, mono_class_from_name, (MonoImage * image, const char* name_space, const char *name))
DO_API(MonoClass *, mono_class_from_name_case, (MonoImage * image, const char* name_space, const char *name))
DO_API(MonoAssembly *, mono_domain_assembly_open, (MonoDomain * domain, const char *name))
DO_API(MonoDomain *, mono_domain_create_appdomain, (const char *domainname, const char* configfile))
DO_API(void, mono_domain_unload, (MonoDomain * domain))
#if UNITY_EDITOR
DO_API(void, mono_unity_domain_unload, (MonoDomain * domain, MonoUnityExceptionFunc callback))
#endif
DO_API(gboolean, mono_unity_class_is_open_constructed_type, (MonoClass * klass))
DO_API(MonoException*, mono_unity_error_convert_to_exception, (MonoError * error))
DO_API(MonoObject*, mono_object_new, (MonoDomain * domain, MonoClass * klass))
DO_API(void, mono_runtime_object_init, (MonoObject * this_obj))
DO_API(MonoObject*, mono_runtime_invoke, (MonoMethod * method, void *obj, void **params, MonoException **exc))
DO_API(void, mono_field_set_value, (MonoObject * obj, MonoClassField * field, void *value))
DO_API(void, mono_field_get_value, (MonoObject * obj, MonoClassField * field, void *value))
DO_API(int, mono_field_get_offset, (MonoClassField * field))
DO_API(MonoClassField*, mono_class_get_fields, (MonoClass * klass, gpointer * iter))
DO_API(MonoClass*, mono_class_get_nested_types, (MonoClass * klass, gpointer * iter))
DO_API(MonoMethod*, mono_class_get_methods, (MonoClass * klass, gpointer * iter))
DO_API(int, mono_class_get_userdata_offset, ())
DO_API(void*, mono_class_get_userdata, (MonoClass * klass))
DO_API(void, mono_class_set_userdata, (MonoClass * klass, void* userdata))
DO_API(MonoDomain*, mono_domain_get, ())
DO_API(MonoDomain*, mono_get_root_domain, ())
DO_API(gint32, mono_domain_get_id, (MonoDomain * domain))
DO_API(void, mono_assembly_foreach, (GFunc func, gpointer user_data))
DO_API(void, mono_image_close, (MonoImage * image))
DO_API(const char*, mono_image_get_name, (MonoImage * image))
DO_API(MonoClass*, mono_get_object_class, ())
#if PLATFORM_WIN || PLATFORM_OSX || PLATFORM_ANDROID || PLATFORM_LINUX
DO_API(void, mono_set_signal_chaining, (gboolean))
#endif

DO_API(void, mono_unity_runtime_set_main_args, (int, const char* argv[]))
DO_API(void, mono_dllmap_insert, (MonoImage * assembly, const char *dll, const char *func, const char *tdll, const char *tfunc))

#if USE_MONO_AOT
DO_API(void*, mono_aot_get_method, (MonoDomain * domain, MonoMethod * method))
#endif

DO_API(void, mono_gc_wbarrier_set_field, (MonoObject * obj, gpointer field_ptr, MonoObject * value))

// Type-safe way of looking up methods based on method signatures
DO_API(MonoObject*, mono_runtime_invoke_array, (MonoMethod * method, void *obj, MonoArray * params, MonoException **exc))
DO_API(char*, mono_array_addr_with_size, (MonoArray * array, int size, uintptr_t idx));
#define mono_array_addr(array, type, index) ((type*)(void*) mono_array_addr_with_size (array, sizeof (type), index))

#if UNITY_EDITOR
DO_API(MonoMethodDesc*, mono_method_desc_new, (const char *name, gboolean include_namespace))
DO_API(MonoMethod*, mono_method_desc_search_in_class, (MonoMethodDesc * desc, MonoClass * klass))
DO_API(void, mono_method_desc_free, (MonoMethodDesc * desc))
DO_API(gboolean, mono_type_generic_inst_is_valuetype, (MonoType*))
#endif
DO_API(char*, mono_type_get_name_full, (MonoType * type, MonoTypeNameFormat format))

#if PLATFORM_WIN
DO_API(gunichar2*, mono_string_to_utf16, (MonoString * string_obj))
#endif

DO_API(const char*, mono_field_get_name, (MonoClassField * field))
DO_API(MonoClass*, mono_field_get_parent, (MonoClassField * field))
DO_API(MonoType*, mono_field_get_type, (MonoClassField * field))
DO_API(gboolean, mono_type_is_byref, (MonoType * type))
DO_API(guint32, mono_type_get_attrs, (MonoType * type))
DO_API(int, mono_type_get_type, (MonoType * type))
DO_API(const char*, mono_method_get_name, (MonoMethod * method))
DO_API(char*, mono_method_full_name, (MonoMethod * method, gboolean signature))
DO_API(MonoImage*, mono_assembly_get_image, (MonoAssembly * assembly))
DO_API(MonoClass*, mono_method_get_class, (MonoMethod * method))
DO_API(MonoClass*, mono_object_get_class, (MonoObject * obj))
DO_API(MonoClass*, mono_class_get, (MonoImage * image, guint32 type_token))
DO_API(MonoObject*, mono_object_isinst, (MonoObject * obj, MonoClass * klass))
DO_API(gboolean, mono_class_is_valuetype, (MonoClass * klass))
DO_API(gboolean, mono_class_is_blittable, (MonoClass * klass))
DO_API(guint32, mono_signature_get_param_count, (MonoMethodSignature * sig))
DO_API(char*, mono_string_to_utf8, (MonoString * string_obj))
DO_API(MonoString*, mono_unity_string_empty_wrapper, ())
DO_API(MonoString*, mono_string_new_wrapper, (const char* text))
DO_API(MonoString*, mono_string_new_len, (MonoDomain * domain, const char *text, guint32 length))
DO_API(MonoString*, mono_string_new_utf16, (MonoDomain * domain, const guint16 * text, gint32 length))
DO_API(MonoString*, mono_string_from_utf16, (const gunichar2 * text))
DO_API(MonoClass*, mono_class_get_parent, (MonoClass * klass))
DO_API(const char*, mono_class_get_namespace, (MonoClass * klass))
DO_API(gboolean, mono_class_is_subclass_of, (MonoClass * klass, MonoClass * klassc, gboolean check_interfaces))
DO_API(const char*, mono_class_get_name, (MonoClass * klass))
DO_API(char*, mono_type_get_name, (MonoType * type))
DO_API(MonoClass*, mono_type_get_class, (MonoType * type))
DO_API(gboolean, mono_metadata_type_equal, (MonoType * t1, MonoType * t2))
DO_API(void, mono_metadata_decode_row, (const MonoTableInfo * t, int idx, guint32 * res, int res_size))
DO_API(MonoException *, mono_exception_from_name_msg, (MonoImage * image, const char *name_space, const char *name, const char *msg))
DO_API(MonoException *, mono_exception_from_name_two_strings, (MonoImage * image, const char *name_space, const char *name, const char *msg1, const char *msg2))
DO_API(MonoException *, mono_get_exception_argument_null, (const char *arg))
DO_API_NO_RETURN(void, mono_raise_exception, (MonoException * ex))
DO_API(MonoClass*, mono_get_exception_class, ())
DO_API(MonoClass*, mono_get_array_class, ())
DO_API(MonoClass*, mono_get_string_class, ())
DO_API(MonoClass*, mono_get_boolean_class, ())
DO_API(MonoClass*, mono_get_byte_class, ())
DO_API(MonoClass*, mono_get_char_class, ())
DO_API(MonoClass*, mono_get_int16_class, ())
DO_API(MonoClass*, mono_get_int32_class, ())
DO_API(MonoClass*, mono_get_int64_class, ())
DO_API(MonoClass*, mono_get_single_class, ())
DO_API(MonoClass*, mono_get_double_class, ())
DO_API(MonoArray*, mono_array_new, (MonoDomain * domain, MonoClass * eclass, guint32 n))
DO_API(MonoArray*, mono_unity_array_new_2d, (MonoDomain * domain, MonoClass * eclass, size_t size0, size_t size1))
DO_API(MonoArray*, mono_unity_array_new_3d, (MonoDomain * domain, MonoClass * eclass, size_t size0, size_t size1, size_t size2))

DO_API(MonoClass *, mono_array_class_get, (MonoClass * eclass, guint32 rank))

DO_API(gint32, mono_class_array_element_size, (MonoClass * ac))
DO_API(MonoObject*, mono_type_get_object, (MonoDomain * domain, MonoType * type))
DO_API(gboolean, mono_class_is_generic, (MonoClass * klass))
DO_API(gboolean, mono_class_is_inflated, (MonoClass * klass))

DO_API(gboolean, unity_mono_method_is_generic, (MonoMethod * method))
DO_API(gboolean, unity_mono_method_is_inflated, (MonoMethod * method))

DO_API(MonoThread *, mono_thread_attach, (MonoDomain * domain))

DO_API(void, mono_thread_detach, (MonoThread * thread))
DO_API(gboolean, mono_thread_has_sufficient_execution_stack, (void))

#if USE_MONO_DOMAINS
DO_API(void, mono_unity_thread_fast_attach, (MonoDomain * domain))
DO_API(void, mono_unity_thread_fast_detach, ())
#endif

DO_API(MonoThread *, mono_thread_exit, ())

DO_API(MonoThread *, mono_thread_current, (void))
DO_API(void, mono_thread_set_main, (MonoThread * thread))
DO_API(void, mono_set_find_plugin_callback, (gconstpointer method))

DO_API(void, mono_runtime_unhandled_exception_policy_set, (MonoRuntimeUnhandledExceptionPolicy policy))

DO_API(MonoClass*, mono_class_get_nesting_type, (MonoClass * klass))
DO_API(MonoVTable*, mono_class_vtable, (MonoDomain * domain, MonoClass * klass))
DO_API(MonoReflectionMethod*, mono_method_get_object, (MonoDomain * domain, MonoMethod * method, MonoClass * refclass))
DO_API(MonoReflectionField*, mono_field_get_object, (MonoDomain * domain, MonoClass * klass, MonoClassField * field))
DO_API(MonoClassField* , mono_field_from_token, (MonoImage * image, uint32_t token, MonoClass** retklass, MonoGenericContext * context))
DO_API(MonoClassField*, mono_unity_field_from_token_checked, (MonoImage * image, guint32 token, MonoClass** retklass, MonoGenericContext * context, MonoError * error))

DO_API(MonoMethodSignature*, mono_method_signature, (MonoMethod * method))
DO_API(MonoMethodSignature*, mono_method_signature_checked_slow, (MonoMethod * method, MonoError * error))
DO_API(MonoType*, mono_signature_get_params, (MonoMethodSignature * sig, gpointer * iter))
DO_API(MonoType*, mono_signature_get_return_type, (MonoMethodSignature * sig))
DO_API(MonoType*, mono_class_get_type, (MonoClass * klass))

DO_API(void, mono_debug_init, (int format))

DO_API(gboolean, mono_is_debugger_attached, (void))

DO_API(void, mono_debug_open_image_from_memory, (MonoImage * image, const char *raw_contents, int size))
DO_API(guint32, mono_field_get_flags, (MonoClassField * field))
DO_API(MonoImage*, mono_image_open_from_data_full, (const void *data, guint32 data_len, gboolean need_copy, int *status, gboolean ref_only))
DO_API(const char*, mono_image_strerror, (int status))
DO_API(MonoImage*, mono_image_open_from_data_with_name, (char *data, guint32 data_len, gboolean need_copy, int *status, gboolean refonly, const char *name))
DO_API(MonoAssembly *, mono_assembly_load_from, (MonoImage * image, const char*fname, int *status))
DO_API(gboolean, mono_assembly_fill_assembly_name, (MonoImage * image, MonoAssemblyName * aname))
DO_API(char*, mono_stringify_assembly_name, (MonoAssemblyName * aname))
DO_API(int, mono_assembly_name_parse, (const char* name, MonoAssemblyName * assembly))
DO_API(void, mono_assembly_name_free, (MonoAssemblyName * assembly))
DO_API(MonoAssembly*, mono_assembly_loaded, (MonoAssemblyName * aname))
DO_API(const MonoTableInfo*, mono_image_get_table_info, (MonoImage * image, int table_id))
DO_API(int, mono_image_get_table_rows, (MonoImage * image, int table_id))
DO_API(MonoClass*, mono_unity_class_get, (MonoImage * image, guint32 type_token))
DO_API(gboolean, mono_metadata_signature_equal, (MonoMethodSignature * sig1, MonoMethodSignature * sig2))

DO_API(MonoObject *, mono_value_box, (MonoDomain * domain, MonoClass * klass, gpointer val))
DO_API(MonoImage*, mono_class_get_image, (MonoClass * klass))
DO_API(char, mono_signature_is_instance, (MonoMethodSignature * signature))
DO_API(MonoMethod*, mono_method_get_last_managed, ())
DO_API(MonoClass*, mono_get_enum_class, ())
DO_API(MonoType*, mono_class_get_byref_type, (MonoClass * klass))

DO_API(void, mono_field_static_get_value, (MonoVTable * vt, MonoClassField * field, void *value))
DO_API(void, mono_unity_set_embeddinghostname, (const char* name))
DO_API(void, mono_set_assemblies_path_null_separated, (const char* name))

DO_API(void, mono_unity_gc_set_mode, (MonoGCMode mode));

DO_API_OPTIONAL(gint64, mono_gc_get_max_time_slice_ns, ());
DO_API_OPTIONAL(void, mono_gc_set_max_time_slice_ns, (gint64 maxTimeSlice));
DO_API_OPTIONAL(gboolean, mono_gc_is_incremental, ());
DO_API_OPTIONAL(void, mono_gc_set_incremental, (gboolean value));

DO_API(guint32, mono_gchandle_new, (MonoObject * obj, gboolean pinned))
DO_API(guint32, mono_gchandle_new_weakref, (MonoObject * obj, gboolean track_resurrection))
DO_API(MonoObject*, mono_gchandle_get_target, (guint32 gchandle))
DO_API(void, mono_gchandle_free, (guint32 gchandle))
DO_API(gboolean, mono_gchandle_is_in_domain, (guint32 gchandle, MonoDomain * domain))

DO_API(uintptr_t, mono_gchandle_new_v2, (MonoObject * obj, gboolean pinned))
DO_API(uintptr_t, mono_gchandle_new_weakref_v2, (MonoObject * obj, gboolean track_resurrection))
DO_API(MonoObject*, mono_gchandle_get_target_v2, (uintptr_t gchandle))
DO_API(void, mono_gchandle_free_v2, (uintptr_t gchandle))
DO_API(gboolean, mono_gchandle_is_in_domain_v2, (uintptr_t gchandle, MonoDomain * domain))

DO_API(MonoObject*, mono_assembly_get_object, (MonoDomain * domain, MonoAssembly * assembly))

typedef UNUSED_SYMBOL gboolean(*MonoStackWalk) (MonoMethod *method, gint32 native_offset, gint32 il_offset, gboolean managed, gpointer data);
DO_API(void, mono_stack_walk, (MonoStackWalk func, gpointer user_data));
DO_API(void, mono_stack_walk_no_il, (MonoStackWalk start, void* user_data));

DO_API(char*, mono_pmip, (void *ip));
DO_API(MonoObject*, mono_runtime_delegate_invoke, (MonoObject * delegate, void** params, MonoException** exc))

DO_API(MonoJitInfo*, mono_jit_info_table_find, (MonoDomain * domain, void* ip))

DO_API(int, mono_unity_managed_callstack, (unsigned char* buffer, int bufferSize, const MonoUnityCallstackOptions * opts));

DO_API_OPTIONAL(MonoDebugSourceLocation*, mono_debug_lookup_source_location_by_il, (MonoMethod * method, guint32 il_offset, MonoDomain * domain))
DO_API(MonoDebugSourceLocation*, mono_debug_lookup_source_location, (MonoMethod * method, guint32 address, MonoDomain * domain))
DO_API(void, mono_debug_free_source_location, (MonoDebugSourceLocation * location))
DO_API_OPTIONAL(MonoDebugMethodJitInfo*, mono_debug_find_method, (MonoMethod * method, MonoDomain * domain))
DO_API_OPTIONAL(void, mono_debug_free_method_jit_info, (MonoDebugMethodJitInfo * jit))

// We need to hook into the Boehm GC internals to perform validation of write barriers
#if ENABLE_SCRIPTING_GC_WBARRIERS && UNITY_DEVELOPER_BUILD
DO_API_OPTIONAL(void, GC_dirty_inner, (void **ptr))
DO_API_OPTIONAL(void*, GC_malloc, (size_t size))
DO_API_OPTIONAL(void*, GC_malloc_uncollectable, (size_t size))
DO_API_OPTIONAL(void*, GC_malloc_kind, (size_t size, int k))
DO_API_OPTIONAL(void*, GC_malloc_atomic, (size_t size))
DO_API_OPTIONAL(void*, GC_gcj_malloc, (size_t size, void *))
DO_API_OPTIONAL(void*, GC_free, (void*))
#endif

DO_API(MonoProperty*, mono_class_get_properties, (MonoClass * klass, gpointer * iter))
DO_API(MonoMethod*, mono_property_get_get_method, (MonoProperty * prop))
DO_API(MonoObject *, mono_object_new_alloc_specific, (MonoVTable * vtable))
DO_API(MonoObject *, mono_object_new_specific, (MonoVTable * vtable))
//DO_API(MonoDomain*, mono_object_get_domain, (MonoObject *obj))

DO_API(void, mono_gc_collect, (int generation))
DO_API_OPTIONAL(int, mono_gc_collect_a_little, ())
DO_API_OPTIONAL(void, mono_gc_start_incremental_collection, ())
DO_API(int, mono_gc_max_generation, ())

DO_API(gint64, mono_gc_get_used_size, ())
DO_API(gint64, mono_gc_get_heap_size, ())

DO_API(void, mono_gc_wbarrier_generic_store, (gpointer ptr, MonoObject * value))

DO_API(MonoAssembly*, mono_image_get_assembly, (MonoImage * image))
DO_API(MonoAssembly*, mono_assembly_open, (const char *filename, int *status))

DO_API(gboolean, mono_class_is_enum, (MonoClass * klass))
DO_API(MonoType*, mono_class_enum_basetype, (MonoClass * klass))
DO_API(gint32, mono_class_instance_size, (MonoClass * klass))
DO_API(guint32, mono_object_get_size, (MonoObject * obj))
DO_API(guint32, mono_class_get_type_token, (MonoClass * klass))
DO_API(const char*, mono_image_get_filename, (MonoImage * image))
DO_API(MonoAssembly*, mono_assembly_load_from_full, (MonoImage * image, const char *fname, int *status, gboolean refonly))
DO_API(MonoClass*, mono_class_get_interfaces, (MonoClass * klass, gpointer * iter))
DO_API(void, mono_assembly_close, (MonoAssembly * assembly))
DO_API(MonoProperty*, mono_class_get_property_from_name, (MonoClass * klass, const char *name))
DO_API(MonoMethod*, mono_class_get_method_from_name, (MonoClass * klass, const char *name, int param_count))
DO_API(MonoClass*, mono_class_from_mono_type, (MonoType * image))
DO_API(int, mono_class_get_rank, (MonoClass * klass));
DO_API(MonoClass*, mono_class_get_element_class, (MonoClass * klass));
DO_API(gboolean, mono_unity_class_is_interface, (MonoClass * klass))
DO_API(gboolean, mono_unity_class_is_abstract, (MonoClass * klass))
DO_API(MonoClass*, mono_unity_class_get_generic_type_definition, (MonoClass * klass))
DO_API(MonoMethod*, mono_get_method, (MonoImage * image, guint32 token, MonoClass * klass))

DO_API(int, mono_array_element_size, (MonoClass * classOfArray))

DO_API(gboolean, mono_domain_set, (MonoDomain * domain, gboolean force))
DO_API(void, mono_unity_domain_set_config, (MonoDomain * domain, const char *base_dir, const char *config_file_name))
DO_API(void, mono_thread_push_appdomain_ref, (MonoDomain * domain))
DO_API(void, mono_thread_pop_appdomain_ref, ())

DO_API(int, mono_runtime_exec_main, (MonoMethod * method, MonoArray * args, MonoObject **exc))

DO_API(MonoImage*, mono_get_corlib, ())
DO_API(MonoImage*, mono_image_loaded, (const char *name))
DO_API(MonoClassField*, mono_class_get_field_from_name, (MonoClass * klass, const char *name))
DO_API(guint32, mono_class_get_flags, (MonoClass * klass))

DO_API(int, mono_parse_default_optimizations, (const char* p))
DO_API(void, mono_set_defaults, (int verbose_level, guint32 opts))
DO_API(void, mono_config_parse, (const char *filename))
DO_API(void, mono_set_dirs, (const char *assembly_dir, const char *config_dir))

#if UNITY_EDITOR
DO_API(void, mono_set_break_policy, (MonoBreakPolicyFunc policy_callback))
#endif

DO_API(void, mono_set_ignore_version_and_key_when_finding_assemblies_already_loaded, (gboolean value))
DO_API(void, mono_verifier_set_mode, (MiniVerifierMode mode))
DO_API(void, mono_jit_parse_options, (int argc, char * argv[]))
DO_API(gpointer, mono_object_unbox, (MonoObject * o))

DO_API(MonoObject*, mono_custom_attrs_get_attr, (MonoCustomAttrInfo * ainfo, MonoClass * attr_klass))

DO_API(MonoArray*, mono_custom_attrs_construct, (MonoCustomAttrInfo * cinfo))
DO_API(MonoArray*, mono_unity_custom_attrs_construct, (MonoCustomAttrInfo * cinfo, MonoError * error))

DO_API(gboolean, mono_custom_attrs_has_attr, (MonoCustomAttrInfo * ainfo, MonoClass * attr_klass))
DO_API(MonoCustomAttrInfo*, mono_custom_attrs_from_field, (MonoClass * klass, MonoClassField * field))
DO_API(MonoCustomAttrInfo*, mono_custom_attrs_from_method, (MonoMethod * method))
DO_API(MonoCustomAttrInfo*, mono_custom_attrs_from_property, (MonoClass * klass, MonoProperty * property))
DO_API(MonoCustomAttrInfo*, mono_custom_attrs_from_class, (MonoClass * klass))
DO_API(MonoCustomAttrInfo*, mono_custom_attrs_from_assembly, (MonoAssembly * assembly))
DO_API(MonoArray*, mono_reflection_get_custom_attrs_by_type, (MonoObject * object, MonoClass * klass))
DO_API(void, mono_custom_attrs_free, (MonoCustomAttrInfo * attr))

DO_API(void, mono_unity_set_data_dir, (const char * dir));
DO_API(MonoClass*, mono_custom_attrs_get_attrs, (MonoCustomAttrInfo * ainfo, void** iterator))

DO_API(MonoException*, mono_unity_loader_get_last_error_and_error_prepare_exception, (void))

#if PLATFORM_STANDALONE || UNITY_EDITOR
// DllImport fallback handling to load native libraries from custom locations
typedef UNUSED_SYMBOL void* (*MonoDlFallbackLoad) (const char *name, int flags, char **err, void *user_data);
typedef UNUSED_SYMBOL void* (*MonoDlFallbackSymbol) (void *handle, const char *name, char **err, void *user_data);
typedef UNUSED_SYMBOL void* (*MonoDlFallbackClose) (void *handle, void *user_data);

DO_API(MonoDlFallbackHandler*, mono_dl_fallback_register, (MonoDlFallbackLoad load_func, MonoDlFallbackSymbol symbol_func, MonoDlFallbackClose close_func, void *user_data))
DO_API(void, mono_dl_fallback_unregister, (MonoDlFallbackHandler * handler))

#endif

typedef UNUSED_SYMBOL void(*vprintf_func)(const char* msg, va_list args);
DO_API(void, mono_unity_set_vprintf_func, (vprintf_func func))

DO_API(void*, mono_unity_liveness_allocate_struct, (MonoClass * filter, int max_object_count, mono_register_object_callback callback, void* userdata, mono_liveness_reallocate_callback reallocate))
DO_API(void, mono_unity_liveness_finalize, (void* state))
DO_API(void, mono_unity_liveness_free_struct, (void* state))
DO_API(void, mono_unity_liveness_calculation_from_root, (MonoObject * root, void* state))
DO_API(void, mono_unity_liveness_calculation_from_statics, (void* state))

DO_API(MonoMethod*, unity_mono_reflection_method_get_method, (MonoReflectionMethod * mrf))

// Profiler
#if ENABLE_MONO
typedef UNUSED_SYMBOL void(*MonoProfileFunc) (void *prof);
typedef UNUSED_SYMBOL void(*MonoProfileGCFunc)         (void *prof, int event, int generation);
typedef UNUSED_SYMBOL  void(*MonoProfileGCResizeFunc)   (void *prof, SInt64 new_size);
typedef UNUSED_SYMBOL gboolean(*MonoProfilerCoverageFilterCallback) (void *prof, MonoMethod *method);
typedef UNUSED_SYMBOL void(*MonoProfilerCoverageCallback) (void *prof, const MonoProfilerCoverageData *data);
DO_API(void, mono_profiler_install, (void *prof, MonoProfileFunc shutdown_callback))
DO_API(void, mono_profiler_install_gc, (MonoProfileGCFunc callback, MonoProfileGCResizeFunc heap_resize_callback))
DO_API(void, mono_profiler_set_events, (int events))

DO_API_OPTIONAL(gboolean, mono_profiler_enable_coverage, ())
DO_API_OPTIONAL(void, mono_profiler_set_coverage_filter_callback, (void* handle, MonoProfilerCoverageFilterCallback cb))
DO_API_OPTIONAL(gboolean, mono_profiler_get_coverage_data, (void* handle, MonoMethod * method, MonoProfilerCoverageCallback cb))
DO_API_OPTIONAL(void, mono_profiler_reset_coverage, (MonoMethod * method))
DO_API_OPTIONAL(gboolean, mono_profiler_get_all_coverage_data, (void* handle, MonoProfilerCoverageCallback cb))
DO_API_OPTIONAL(void, mono_profiler_reset_all_coverage, ())

#if LOAD_MONO_DYNAMICALLY
DO_API_OPTIONAL(void*, mono_profiler_create, (MonoProfiler * prof))
DO_API_OPTIONAL(void, mono_profiler_load, (const char *desc))
DO_API_OPTIONAL(void, mono_set_crash_chaining, (gboolean))
#endif
#endif

#if ENABLE_MONO_MEMORY_PROFILER
typedef UNUSED_SYMBOL void(*MonoProfileMethodFunc)   (void *prof, MonoMethod *method);
typedef UNUSED_SYMBOL void(*MonoProfileObjectFunc) (void *prof, MonoObject *object);
typedef UNUSED_SYMBOL void(*MonoProfileExceptionClauseFunc) (void *prof, MonoMethod *method, int clause_type, int clause_num);
typedef UNUSED_SYMBOL void(*MonoProfileAllocFunc)      (void *prof, MonoObject* obj, MonoClass* klass);
typedef UNUSED_SYMBOL void(*MonoProfileStatCallChainFunc) (void *prof, int call_chain_depth, guchar **ip, void *context);
typedef UNUSED_SYMBOL void(*MonoProfileStatFunc)       (void *prof, guchar *ip, void *context);
typedef UNUSED_SYMBOL void(*MonoProfileJitResult)    (void *prof, MonoMethod *method, void* jinfo, int result);
typedef UNUSED_SYMBOL void(*MonoProfileThreadFunc)     (void *prof, unsigned long tid);
typedef UNUSED_SYMBOL void(*MonoProfileThreadNameFunc) (void *prof, uintptr_t tid, const char *name);
typedef UNUSED_SYMBOL void(*MonoProfileJitDoneFunc)    (void *prof, MonoMethod *method, MonoJitInfo *jinfo);
typedef UNUSED_SYMBOL void(*MonoProfileJitCodeBufferFunc) (void *prof, void *buffer, uint64_t size, MonoProfilerCodeBufferType type, const void *data);
typedef UNUSED_SYMBOL void(*MonoProfileMethodEnterLeaveFunc)   (void *prof, MonoMethod *method, void *context);
typedef UNUSED_SYMBOL void(*MonoProfileMethodTailCall)   (void *prof, MonoMethod *method, MonoMethod *target);
typedef UNUSED_SYMBOL void(*MonoProfileMethodExceptionLeave)   (void *prof, MonoMethod *method, MonoObject *exception);
typedef UNUSED_SYMBOL int(*MonoProfilerCallInstrumentationFilterCallback) (void *prof, MonoMethod *method);
typedef UNUSED_SYMBOL void(*MonoProfileDomainFunc)   (void *prof, MonoDomain *domain);

DO_API(void, mono_profiler_install_enter_leave, (MonoProfileMethodFunc enter, MonoProfileMethodFunc leave))
DO_API(void, mono_profiler_install_allocation, (MonoProfileAllocFunc callback))
DO_API(void, mono_profiler_install_jit_end, (MonoProfileJitResult jit_end))
DO_API(void, mono_profiler_install_thread, (MonoProfileThreadFunc start, MonoProfileThreadFunc end))

#if LOAD_MONO_DYNAMICALLY
DO_API_OPTIONAL(void, mono_profiler_set_thread_name_callback, (void *handle, MonoProfileThreadNameFunc callback))
DO_API_OPTIONAL(void, mono_profiler_set_gc_allocation_callback, (void *handle, MonoProfileObjectFunc callback))
DO_API_OPTIONAL(void, mono_profiler_set_gc_finalizing_callback, (void *handle, MonoProfileFunc callback))
DO_API_OPTIONAL(void, mono_profiler_set_gc_finalized_callback, (void *handle, MonoProfileFunc callback))
DO_API_OPTIONAL(void, mono_profiler_set_gc_finalizing_object_callback, (void *handle, MonoProfileObjectFunc callback))
DO_API_OPTIONAL(void, mono_profiler_set_gc_finalized_object_callback, (void *handle, MonoProfileObjectFunc callback))
DO_API_OPTIONAL(void, mono_profiler_set_jit_begin_callback, (void *handle, MonoProfileMethodFunc callback))
DO_API_OPTIONAL(void, mono_profiler_set_jit_failed_callback, (void *handle, MonoProfileMethodFunc callback))
DO_API_OPTIONAL(void, mono_profiler_set_jit_done_callback, (void *handle, MonoProfileJitDoneFunc callback))
DO_API_OPTIONAL(void, mono_profiler_set_jit_code_buffer_callback, (void *handle, MonoProfileJitCodeBufferFunc callback))
DO_API_OPTIONAL(void, mono_profiler_set_method_enter_callback, (void *handle, MonoProfileMethodEnterLeaveFunc callback))
DO_API_OPTIONAL(void, mono_profiler_set_method_leave_callback, (void *handle, MonoProfileMethodEnterLeaveFunc callback))
DO_API_OPTIONAL(void, mono_profiler_set_method_tail_call_callback, (void *handle, MonoProfileMethodTailCall callback))
DO_API_OPTIONAL(void, mono_profiler_set_method_exception_leave_callback, (void *handle, MonoProfileMethodExceptionLeave callback))
DO_API_OPTIONAL(void, mono_profiler_set_call_instrumentation_filter_callback, (void *handle, MonoProfilerCallInstrumentationFilterCallback callback))
DO_API_OPTIONAL(void, mono_profiler_set_domain_unloading_callback, (void *handle, MonoProfileDomainFunc callback))
DO_API_OPTIONAL(void, mono_profiler_set_domain_unloaded_callback, (void *handle, MonoProfileDomainFunc callback))
#endif
#endif

typedef void(*MonoDataFunc) (void *data, void *userData);
typedef void(*MonoClassFunc) (MonoClass *klass, void *userData);

DO_API(void, mono_unity_image_set_mempool_chunk_foreach, (MonoDataFunc callback, void* userdata))
DO_API(void, mono_unity_root_domain_mempool_chunk_foreach, (MonoDataFunc callback, void* userdata))
DO_API(void, mono_unity_domain_mempool_chunk_foreach, (MonoDomain * domain, MonoDataFunc callback, void* userData))
DO_API(void, mono_unity_assembly_mempool_chunk_foreach, (MonoAssembly * assembly, MonoDataFunc callback, void* userData))
DO_API(void, mono_unity_gc_heap_foreach, (MonoDataFunc callback, void* userData))
DO_API(void, mono_unity_gc_handles_foreach_get_target, (MonoDataFunc callback, void* userData))
DO_API(uint32_t, mono_unity_object_header_size, ())
DO_API(uint32_t, mono_unity_array_object_header_size, ())
DO_API(uint32_t, mono_unity_offset_of_array_length_in_array_object_header, ())
DO_API(uint32_t, mono_unity_offset_of_array_bounds_in_array_object_header, ())
DO_API(uint32_t, mono_unity_allocation_granularity, ())
DO_API(uint32_t, mono_unity_class_get_data_size, (MonoClass * klass))
DO_API(void, mono_unity_type_get_name_full_chunked, (MonoType * type, MonoDataFunc appendCallback, void* userData))
DO_API(MonoVTable*, mono_unity_class_try_get_vtable, (MonoDomain * domain, MonoClass * klass))
DO_API(gboolean, mono_unity_type_is_pointer_type, (MonoType * type))
DO_API(gboolean, mono_unity_type_is_static, (MonoType * type))
DO_API(gboolean, mono_unity_class_field_is_literal, (MonoClassField * field))
DO_API(void*, mono_unity_vtable_get_static_field_data, (MonoVTable * vTable))
DO_API(void, mono_unity_class_for_each, (MonoClassFunc callback, void* userData))
DO_API(void, mono_unity_stop_gc_world, ())
DO_API(void, mono_unity_start_gc_world, ())

DO_API(MonoManagedMemorySnapshot*, mono_unity_capture_memory_snapshot, ());
DO_API(void, mono_unity_free_captured_memory_snapshot, (MonoManagedMemorySnapshot * snapshot));

// GLib functions
#define g_free mono_unity_g_free
DO_API(void, mono_unity_g_free, (void* p))

typedef UNUSED_SYMBOL void (*MonoLogCallback) (const char *log_domain, const char *log_level, const char *message, bool fatal, void *user_data);
DO_API(void, mono_trace_set_log_handler, (MonoLogCallback callback, void *user_data))
DO_API(void, mono_trace_set_level_string, (const char *value))
DO_API(void, mono_trace_set_mask_string, (const char *value))

#if PLATFORM_OSX
DO_API(int, mono_unity_backtrace_from_context, (void* context, void* array[], int count))
#endif

#if UNITY_ANDROID
DO_API(void, mono_file_map_override, (MonoFileMapOpen open_func, MonoFileMapSize size_func, MonoFileMapFd fd_func, MonoFileMapClose close_func, MonoFileMapMap map_func, MonoFileMapUnmap unmap_func))
DO_API(void, mono_register_machine_config, (const char *config_xml))

DO_API(void, mono_sigctx_to_monoctx, (void *sigctx, MonoContext * mctx))
DO_API(void, mono_walk_stack_with_ctx, (MonoJitStackWalk func, MonoContext * start_ctx, MonoUnwindOptions options, void *user_data))
DO_API(char *, mono_debug_print_stack_frame, (MonoMethod * method, guint32 native_offset, MonoDomain * domain))
#endif

#if ENABLE_MONO_MEMORY_CALLBACKS
DO_API(void, mono_unity_install_memory_callbacks, (MonoMemoryCallbacks * callbacks))
#endif

#if UNITY_EDITOR
typedef UNUSED_SYMBOL size_t(*RemapPathFunction)(const char* path, char* buffer, size_t buffer_len);
DO_API(void, mono_unity_register_path_remapper, (RemapPathFunction func))
DO_API_OPTIONAL(void, mono_unity_set_enable_handler_block_guards, (gboolean allow))
#endif

DO_API_OPTIONAL(void, mono_unity_install_unitytls_interface, (void* callbacks))

#if ENABLE_OUT_OF_PROCESS_CRASH_HANDLER && UNITY_64 && PLATFORM_WIN && ENABLE_MONO && !PLATFORM_XBOXONE
DO_API_OPTIONAL(void*, mono_unity_lock_dynamic_function_access_tables64, (unsigned int))
DO_API_OPTIONAL(void, mono_unity_unlock_dynamic_function_access_tables64, (void))
#endif

#if LOAD_MONO_DYNAMICALLY
DO_API_OPTIONAL(void, mono_error_init, (MonoError * error))
DO_API_OPTIONAL(void, mono_error_cleanup, (MonoError * error))
DO_API_OPTIONAL(gint32, mono_error_ok, (MonoError * error))
DO_API_OPTIONAL(unsigned short, mono_error_get_error_code, (MonoError * error))
DO_API_OPTIONAL(const char*, mono_error_get_message, (MonoError * error))
#endif

#if UNITY_EDITOR
DO_API_OPTIONAL(void, mono_debugger_set_generate_debug_info, (gboolean enable))
DO_API_OPTIONAL(gboolean, mono_debugger_get_generate_debug_info, ())
DO_API_OPTIONAL(void, mono_debugger_disconnect, ())
typedef void (*MonoDebuggerAttachFunc)(gboolean attached);
DO_API_OPTIONAL(void, mono_debugger_install_attach_detach_callback, (MonoDebuggerAttachFunc func))
typedef UNUSED_SYMBOL void (*UnityLogErrorCallback) (const char* message);
DO_API(void, mono_unity_set_editor_logging_callback, (UnityLogErrorCallback callback))
#endif

#undef DO_API
#undef DO_API_NO_RETURN
#undef DO_API_OPTIONAL
