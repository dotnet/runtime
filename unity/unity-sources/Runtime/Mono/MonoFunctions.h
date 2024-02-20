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

DO_API(void, mono_add_internal_call, (const char *name, gconstpointer method))

DO_API(MonoObject*, mono_runtime_invoke, (MonoMethod * method, void *obj, void **params, MonoException **exc))
DO_API(int, mono_field_get_offset, (MonoClassField * field))
DO_API(MonoMethod*, mono_class_get_methods, (MonoClass * klass, gpointer * iter))
DO_API(int, mono_class_get_userdata_offset, ())
DO_API(void*, mono_class_get_userdata, (MonoClass * klass))
DO_API(void, mono_class_set_userdata, (MonoClass * klass, void* userdata))

#if USE_MONO_AOT
DO_API(void*, mono_aot_get_method, (MonoDomain * domain, MonoMethod * method))
#endif

#if UNITY_EDITOR
DO_API(MonoMethodDesc*, mono_method_desc_new, (const char *name, gboolean include_namespace))
DO_API(MonoMethod*, mono_method_desc_search_in_class, (MonoMethodDesc * desc, MonoClass * klass))
DO_API(void, mono_method_desc_free, (MonoMethodDesc * desc))
DO_API(gboolean, mono_type_generic_inst_is_valuetype, (MonoType*))
#endif

DO_API(const char*, mono_field_get_name, (MonoClassField * field))
DO_API(MonoClass*, mono_field_get_parent, (MonoClassField * field))
DO_API(int, mono_type_get_type, (MonoType * type))
DO_API(const char*, mono_method_get_name, (MonoMethod * method))
DO_API(MonoImage*, mono_assembly_get_image, (MonoAssembly * assembly))
DO_API(MonoClass*, mono_method_get_class, (MonoMethod * method))
DO_API(guint32, mono_signature_get_param_count, (MonoMethodSignature * sig))
DO_API(MonoClass*, mono_class_get_parent, (MonoClass * klass))
DO_API(const char*, mono_class_get_namespace, (MonoClass * klass))
DO_API(const char*, mono_class_get_name, (MonoClass * klass))
DO_API(gboolean, mono_metadata_type_equal, (MonoType * t1, MonoType * t2))

DO_API(void*, unity_coreclr_create_delegate, (const char* assemblyName, const char* typeName, const char* methodName))

DO_API(gint32, mono_class_array_element_size, (MonoClass * ac))

DO_API(MonoThread *, mono_thread_attach, (MonoDomain * domain))

DO_API(void, mono_thread_detach, (MonoThread * thread))
DO_API(gboolean, mono_thread_has_sufficient_execution_stack, (void))

DO_API(MonoThread *, mono_thread_current, (void))

DO_API(MonoMethodSignature*, mono_method_signature, (MonoMethod * method))
DO_API(MonoType*, mono_signature_get_params, (MonoMethodSignature * sig, gpointer * iter))
DO_API(MonoType*, mono_signature_get_return_type, (MonoMethodSignature * sig))
DO_API(MonoType*, mono_class_get_type, (MonoClass * klass))

DO_API(int, mono_assembly_name_parse, (const char* name, MonoAssemblyName * assembly))
DO_API(int, mono_image_get_table_rows, (MonoImage * image, int table_id))

DO_API(MonoMethod*, mono_method_get_last_managed, ())

DO_API(gint32, mono_class_instance_size, (MonoClass * klass))
DO_API(guint32, mono_class_get_type_token, (MonoClass * klass))
DO_API(MonoProperty*, mono_class_get_property_from_name, (MonoClass * klass, const char *name))
DO_API(MonoClass*, mono_class_from_mono_type, (MonoType * image))

DO_API(int, mono_array_element_size, (MonoClass * classOfArray))

DO_API(MonoException*, mono_unity_loader_get_last_error_and_error_prepare_exception, (void))

#ifdef WIN32
typedef int (__cdecl *vprintf_func)(const char* msg, va_list args);
#else
typedef int (*vprintf_func)(const char* msg, va_list args);
#endif
DO_API(void, mono_unity_set_vprintf_func, (vprintf_func func))

typedef void(*MonoDataFunc) (void *data, void *userData);

DO_API(void, mono_unity_gc_handles_foreach_get_target, (MonoDataFunc callback, void* userData))
DO_API(uint32_t, mono_unity_allocation_granularity, ())
DO_API(void, mono_unity_type_get_name_full_chunked, (MonoType * type, MonoDataFunc appendCallback, void* userData))

// GLib functions
#define g_free mono_unity_g_free
DO_API(void, mono_unity_g_free, (void* p))

#if PLATFORM_OSX
DO_API(int, mono_unity_backtrace_from_context, (void* context, void* array[], int count))
#endif

#if ENABLE_MONO_MEMORY_CALLBACKS
DO_API(void, mono_unity_install_memory_callbacks, (MonoMemoryCallbacks * callbacks))
#endif

#if UNITY_EDITOR
typedef UNUSED_SYMBOL size_t(*RemapPathFunction)(const char* path, char* buffer, size_t buffer_len);
DO_API(void, mono_unity_register_path_remapper, (RemapPathFunction func))
#endif

#if LOAD_MONO_DYNAMICALLY
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
