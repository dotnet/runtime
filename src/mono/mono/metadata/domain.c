/**
 * \file
 * MonoDomain functions
 *
 * Author:
 *	Dietmar Maurer (dietmar@ximian.com)
 *	Patrik Torstensson
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Copyright 2011-2012 Xamarin, Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#include <glib.h>
#include <string.h>
#include <sys/stat.h>

#include <mono/metadata/gc-internals.h>

#include <mono/utils/atomic.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/utils/mono-membar.h>
#include <mono/utils/mono-counters.h>
#include <mono/utils/hazard-pointer.h>
#include <mono/utils/mono-tls.h>
#include <mono/utils/mono-mmap.h>
#include <mono/utils/mono-threads.h>
#include <mono/metadata/object.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/domain-internals.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/class-init.h>
#include <mono/metadata/debug-internals.h>
#include <mono/metadata/assembly-internals.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/mono-config.h>
#include <mono/metadata/mono-hash-internals.h>
#include <mono/metadata/threads-types.h>
#include <mono/metadata/runtime.h>
#include <mono/metadata/w32event.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/coree.h>
#include <mono/metadata/jit-info.h>
#include <mono/utils/w32subset.h>
#include "external-only.h"
#include <mono/utils/mono-tls-inline.h>

/*
 * There is only one domain, but some code uses the domain TLS
 * variable to check whenever a thread is attached to the runtime etc.,
 * so keep this for now.
 */
#define GET_APPDOMAIN    mono_tls_get_domain
#define SET_APPDOMAIN(x) do { \
	MonoThreadInfo *info; \
	mono_tls_set_domain (x); \
	info = mono_thread_info_current (); \
	if (info) \
		mono_thread_info_tls_set (info, TLS_KEY_DOMAIN, (x));	\
} while (FALSE)

static MonoImage *exe_image;
static MonoDomain *mono_root_domain;

/* AppConfigInfo: Information about runtime versions supported by an
 * aplication.
 */
typedef struct {
	GSList *supported_runtimes;
	char *required_runtime;
	int configuration_count;
	int startup_count;
} AppConfigInfo;

static const MonoRuntimeInfo *current_runtime = NULL;

#define NOT_AVAIL {0xffffU,0xffffU,0xffffU,0xffffU}

/* This is the list of runtime versions supported by this JIT.
 */
static const MonoRuntimeInfo supported_runtimes[] = {
	{"v4.0.30319","4.5", { {4,0,0,0}, {10,0,0,0}, {4,0,0,0}, {4,0,0,0}, {4,0,0,0} } },
	{"mobile",    "2.1", { {2,0,5,0}, {10,0,0,0}, {2,0,5,0}, {2,0,5,0}, {4,0,0,0} } },
	{"moonlight", "2.1", { {2,0,5,0}, { 9,0,0,0}, {3,5,0,0}, {3,0,0,0}, NOT_AVAIL } },
};

#undef NOT_AVAIL


/* The stable runtime version */
#define DEFAULT_RUNTIME_VERSION "v4.0.30319"

static GSList*
get_runtimes_from_exe (const char *exe_file, MonoImage **exe_image);

static const MonoRuntimeInfo*
get_runtime_by_version (const char *version);

static MonoDomain *
create_root_domain (void)
{
	MonoDomain *domain;
	gsize domain_gc_bitmap [sizeof(MonoDomain)/4/32 + 1];
	MonoGCDescriptor domain_gc_desc;

	unsigned int i, bit = 0;
	memset (domain_gc_bitmap, 0, sizeof (domain_gc_bitmap));
	for (i = G_STRUCT_OFFSET (MonoDomain, MONO_DOMAIN_FIRST_OBJECT); i <= G_STRUCT_OFFSET (MonoDomain, MONO_DOMAIN_LAST_OBJECT); i += sizeof (gpointer)) {
		bit = i / sizeof (gpointer);
		domain_gc_bitmap [bit / 32] |= (gsize) 1 << (bit % 32);
	}
	domain_gc_desc = mono_gc_make_descr_from_bitmap ((gsize*)domain_gc_bitmap, bit + 1);

	if (!mono_gc_is_moving ())
		domain = (MonoDomain *)mono_gc_alloc_fixed (sizeof (MonoDomain), MONO_GC_DESCRIPTOR_NULL, MONO_ROOT_SOURCE_DOMAIN, NULL, "Domain Structure");
	else
		domain = (MonoDomain *)mono_gc_alloc_fixed (sizeof (MonoDomain), domain_gc_desc, MONO_ROOT_SOURCE_DOMAIN, NULL, "Domain Structure");

	MONO_PROFILER_RAISE (domain_loading, (domain));

#ifndef DISABLE_PERFCOUNTERS
	mono_atomic_inc_i32 (&mono_perfcounters->loader_appdomains);
	mono_atomic_inc_i32 (&mono_perfcounters->loader_total_appdomains);
#endif

	MONO_PROFILER_RAISE (domain_loaded, (domain));

	return domain;
}

/**
 * mono_init_internal:
 *
 * Creates the initial application domain and initializes the mono_defaults
 * structure.
 * This function is guaranteed to not run any IL code.
 * If exe_filename is not NULL, the method will determine the required runtime
 * from the exe configuration file or the version PE field.
 * If runtime_version is not NULL, that runtime version will be used.
 * Either exe_filename or runtime_version must be provided.
 *
 * Returns: the initial domain.
 */
static MonoDomain *
mono_init_internal (const char *filename, const char *exe_filename, const char *runtime_version)
{
	static MonoDomain *domain = NULL;
	MonoAssembly *ass = NULL;
	MonoImageOpenStatus status = MONO_IMAGE_OK;
	GSList *runtimes = NULL;

	if (domain)
		g_assert_not_reached ();

#if defined(HOST_WIN32) && HAVE_API_SUPPORT_WIN32_SET_ERROR_MODE
	/* Avoid system error message boxes. */
	SetErrorMode (SEM_FAILCRITICALERRORS | SEM_NOOPENFILEERRORBOX);
#endif

#ifndef HOST_WIN32
	mono_w32handle_init ();
#endif

	mono_w32event_init ();

#ifndef DISABLE_PERFCOUNTERS
	mono_perfcounters_init ();
#endif
	mono_counters_init ();

	mono_counters_register ("Max HashTable Chain Length", MONO_COUNTER_INT|MONO_COUNTER_METADATA, &mono_g_hash_table_max_chain_length);

	mono_gc_base_init ();
	mono_thread_info_attach ();

	mono_metadata_init ();
	mono_images_init ();
	mono_assemblies_init ();
	mono_classes_init ();
	mono_loader_init ();
	mono_reflection_init ();
	mono_runtime_init_tls ();
	mono_icall_init ();

	domain = create_root_domain ();
	mono_root_domain = domain;

	mono_alcs_init ();
	mono_jit_info_tables_init ();

	SET_APPDOMAIN (domain);

	/* Get a list of runtimes supported by the exe */
	if (exe_filename != NULL) {
		/*
		 * This function will load the exe file as a MonoImage. We need to close it, but
		 * that would mean it would be reloaded later. So instead, we save it to
		 * exe_image, and close it during shutdown.
		 */
		runtimes = get_runtimes_from_exe (exe_filename, &exe_image);
#ifdef HOST_WIN32
		if (!exe_image) {
			exe_image = mono_assembly_open_from_bundle (mono_alc_get_default (), exe_filename, NULL, NULL);
			if (!exe_image)
				exe_image = mono_image_open (exe_filename, NULL);
		}
		mono_fixup_exe_image (exe_image);
#endif
	} else if (runtime_version != NULL) {
		const MonoRuntimeInfo* rt = get_runtime_by_version (runtime_version);
		if (rt != NULL)
			runtimes = g_slist_prepend (runtimes, (gpointer)rt);
	}

	if (runtimes == NULL) {
		const MonoRuntimeInfo *default_runtime = get_runtime_by_version (DEFAULT_RUNTIME_VERSION);
		g_assert (default_runtime);
		runtimes = g_slist_prepend (runtimes, (gpointer)default_runtime);
		if (runtime_version != NULL)
			g_print ("WARNING: The requested runtime version \"%s\" is unavailable.\n", runtime_version);
		else
			g_print ("WARNING: The runtime version supported by this application is unavailable.\n");
		g_print ("Using default runtime: %s\n", default_runtime->runtime_version);
	}

	/* The selected runtime will be the first one for which there is a mscrolib.dll */
	GSList *tmp = runtimes;
	while (tmp != NULL) {
		current_runtime = (MonoRuntimeInfo*)tmp->data;
		g_assert (current_runtime);
		ass = mono_assembly_load_corlib (&status);
		if (status != MONO_IMAGE_OK && status != MONO_IMAGE_ERROR_ERRNO)
			break;
		tmp = tmp->next;
	}

	g_slist_free (runtimes);

	if ((status != MONO_IMAGE_OK) || (ass == NULL)) {
		switch (status){
		case MONO_IMAGE_ERROR_ERRNO: {
			char *corlib_file = g_build_filename (mono_assembly_getrootdir (), "mono", current_runtime->framework_version, "mscorlib.dll", (const char*)NULL);
			g_print ("The assembly mscorlib.dll was not found or could not be loaded.\n");
			g_print ("It should have been installed in the `%s' directory.\n", corlib_file);
			g_free (corlib_file);
			break;
		}
		case MONO_IMAGE_IMAGE_INVALID:
			g_print ("The file %s/mscorlib.dll is an invalid CIL image\n",
				 mono_assembly_getrootdir ());
			break;
		case MONO_IMAGE_MISSING_ASSEMBLYREF:
			g_print ("Missing assembly reference in %s/mscorlib.dll\n",
				 mono_assembly_getrootdir ());
			break;
		case MONO_IMAGE_OK:
			/* to suppress compiler warning */
			break;
		}

		exit (1);
	}
	mono_defaults.corlib = mono_assembly_get_image_internal (ass);

	mono_defaults.object_class = mono_class_load_from_name (
                mono_defaults.corlib, "System", "Object");

	mono_defaults.void_class = mono_class_load_from_name (
                mono_defaults.corlib, "System", "Void");

	mono_defaults.boolean_class = mono_class_load_from_name (
                mono_defaults.corlib, "System", "Boolean");

	mono_defaults.byte_class = mono_class_load_from_name (
                mono_defaults.corlib, "System", "Byte");

	mono_defaults.sbyte_class = mono_class_load_from_name (
                mono_defaults.corlib, "System", "SByte");

	mono_defaults.int16_class = mono_class_load_from_name (
                mono_defaults.corlib, "System", "Int16");

	mono_defaults.uint16_class = mono_class_load_from_name (
                mono_defaults.corlib, "System", "UInt16");

	mono_defaults.int32_class = mono_class_load_from_name (
                mono_defaults.corlib, "System", "Int32");

	mono_defaults.uint32_class = mono_class_load_from_name (
                mono_defaults.corlib, "System", "UInt32");

	mono_defaults.uint_class = mono_class_load_from_name (
                mono_defaults.corlib, "System", "UIntPtr");

	mono_defaults.int_class = mono_class_load_from_name (
                mono_defaults.corlib, "System", "IntPtr");

	mono_defaults.int64_class = mono_class_load_from_name (
                mono_defaults.corlib, "System", "Int64");

	mono_defaults.uint64_class = mono_class_load_from_name (
                mono_defaults.corlib, "System", "UInt64");

	mono_defaults.single_class = mono_class_load_from_name (
                mono_defaults.corlib, "System", "Single");

	mono_defaults.double_class = mono_class_load_from_name (
                mono_defaults.corlib, "System", "Double");

	mono_defaults.char_class = mono_class_load_from_name (
                mono_defaults.corlib, "System", "Char");

	mono_defaults.string_class = mono_class_load_from_name (
                mono_defaults.corlib, "System", "String");

	mono_defaults.enum_class = mono_class_load_from_name (
                mono_defaults.corlib, "System", "Enum");

	mono_defaults.array_class = mono_class_load_from_name (
                mono_defaults.corlib, "System", "Array");

	mono_defaults.delegate_class = mono_class_load_from_name (
		mono_defaults.corlib, "System", "Delegate");

	mono_defaults.multicastdelegate_class = mono_class_load_from_name (
		mono_defaults.corlib, "System", "MulticastDelegate");

	mono_defaults.typehandle_class = mono_class_load_from_name (
                mono_defaults.corlib, "System", "RuntimeTypeHandle");

	mono_defaults.methodhandle_class = mono_class_load_from_name (
                mono_defaults.corlib, "System", "RuntimeMethodHandle");

	mono_defaults.fieldhandle_class = mono_class_load_from_name (
                mono_defaults.corlib, "System", "RuntimeFieldHandle");

	mono_defaults.systemtype_class = mono_class_load_from_name (
                mono_defaults.corlib, "System", "Type");

	mono_defaults.runtimetype_class = mono_class_load_from_name (
                mono_defaults.corlib, "System", "RuntimeType");

	mono_defaults.exception_class = mono_class_load_from_name (
                mono_defaults.corlib, "System", "Exception");

	mono_defaults.thread_class = mono_class_load_from_name (
                mono_defaults.corlib, "System.Threading", "Thread");

	/* There is only one thread class */
	mono_defaults.internal_thread_class = mono_defaults.thread_class;

#if defined(HOST_DARWIN)
	mono_defaults.autoreleasepool_class = mono_class_try_load_from_name (
                mono_defaults.corlib, "System.Threading", "AutoreleasePool");
#else
	mono_defaults.autoreleasepool_class = NULL;
#endif

	mono_defaults.field_info_class = mono_class_load_from_name (
		mono_defaults.corlib, "System.Reflection", "FieldInfo");

	mono_defaults.method_info_class = mono_class_load_from_name (
		mono_defaults.corlib, "System.Reflection", "MethodInfo");

	mono_defaults.stack_frame_class = mono_class_load_from_name (
	        mono_defaults.corlib, "System.Diagnostics", "MonoStackFrame");

	mono_defaults.marshal_class = mono_class_load_from_name (
	        mono_defaults.corlib, "System.Runtime.InteropServices", "Marshal");

	mono_defaults.typed_reference_class = mono_class_load_from_name (
	        mono_defaults.corlib, "System", "TypedReference");

	mono_defaults.argumenthandle_class = mono_class_load_from_name (
	        mono_defaults.corlib, "System", "RuntimeArgumentHandle");

	mono_defaults.monitor_class = mono_class_load_from_name (
	        mono_defaults.corlib, "System.Threading", "Monitor");
	/*
	Not using GENERATE_TRY_GET_CLASS_WITH_CACHE_DECL as this type is heavily checked by sgen when computing finalization.
	*/
	mono_defaults.critical_finalizer_object = mono_class_try_load_from_name (mono_defaults.corlib,
			"System.Runtime.ConstrainedExecution", "CriticalFinalizerObject");

	mono_assembly_load_friends (ass);

	mono_defaults.attribute_class = mono_class_load_from_name (
		mono_defaults.corlib, "System", "Attribute");

	mono_class_init_internal (mono_defaults.array_class);
	mono_defaults.generic_nullable_class = mono_class_load_from_name (
		mono_defaults.corlib, "System", "Nullable`1");
	mono_defaults.generic_ilist_class = mono_class_try_load_from_name (
	        mono_defaults.corlib, "System.Collections.Generic", "IList`1");
	mono_defaults.generic_ireadonlylist_class = mono_class_try_load_from_name (
	        mono_defaults.corlib, "System.Collections.Generic", "IReadOnlyList`1");
	mono_defaults.generic_ienumerator_class = mono_class_try_load_from_name (
	        mono_defaults.corlib, "System.Collections.Generic", "IEnumerator`1");

	mono_defaults.alc_class = mono_class_get_assembly_load_context_class ();
	mono_defaults.appcontext_class = mono_class_try_load_from_name (mono_defaults.corlib, "System", "AppContext");

	domain->friendly_name = g_path_get_basename (filename);

	MONO_PROFILER_RAISE (domain_name, (domain, domain->friendly_name));

	return domain;
}

/**
 * mono_init:
 *
 * Creates the initial application domain and initializes the mono_defaults
 * structure.
 *
 * This function is guaranteed to not run any IL code.
 * The runtime is initialized using the default runtime version.
 *
 * Returns: the initial domain.
 */
MonoDomain *
mono_init (const char *domain_name)
{
	return mono_init_internal (domain_name, NULL, DEFAULT_RUNTIME_VERSION);
}

/**
 * mono_init_from_assembly:
 * \param domain_name name to give to the initial domain
 * \param filename filename to load on startup
 *
 * Used by the runtime, users should use mono_jit_init instead.
 *
 * Creates the initial application domain and initializes the mono_defaults
 * structure.
 * This function is guaranteed to not run any IL code.
 * The runtime is initialized using the runtime version required by the
 * provided executable. The version is determined by looking at the exe
 * configuration file and the version PE field)
 *
 * \returns the initial domain.
 */
MonoDomain *
mono_init_from_assembly (const char *domain_name, const char *filename)
{
	return mono_init_internal (domain_name, filename, NULL);
}

/**
 * mono_init_version:
 *
 * Used by the runtime, users should use \c mono_jit_init instead.
 *
 * Creates the initial application domain and initializes the \c mono_defaults
 * structure.
 *
 * This function is guaranteed to not run any IL code.
 * The runtime is initialized using the provided rutime version.
 *
 * \returns the initial domain.
 */
MonoDomain *
mono_init_version (const char *domain_name, const char *version)
{
	return mono_init_internal (domain_name, NULL, version);
}

void
mono_close_exe_image (void)
{
	gboolean do_close = exe_image != NULL;
	/* FIXME: shutdown hack. We mess something up and try to double-close/free it. */
	do_close = do_close && !exe_image->has_updates;
	if (do_close)
		mono_image_close (exe_image);
}

/**
 * mono_get_root_domain:
 *
 * The root AppDomain is the initial domain created by the runtime when it is
 * initialized.  Programs execute on this AppDomain, but can create new ones
 * later.   Currently there is no unmanaged API to create new AppDomains, this
 * must be done from managed code.
 *
 * Returns: the root appdomain, to obtain the current domain, use mono_domain_get ()
 */
MonoDomain*
mono_get_root_domain (void)
{
	return mono_root_domain;
}

/**
 * mono_domain_get:
 *
 * This method returns the value of the current \c MonoDomain that this thread
 * and code are running under.   To obtain the root domain use
 * \c mono_get_root_domain API.
 *
 * \returns the current domain
 */
MonoDomain *
mono_domain_get ()
{
	return GET_APPDOMAIN ();
}

void
mono_domain_unset (void)
{
	SET_APPDOMAIN (NULL);
}

void
mono_domain_set_fast (MonoDomain *domain)
{
	MONO_REQ_GC_UNSAFE_MODE;

	mono_domain_set_internal_with_options (domain, TRUE);
}

void
mono_domain_set_internal_with_options (MonoDomain *domain, gboolean migrate_exception)
{
	MONO_REQ_GC_UNSAFE_MODE;
	MonoInternalThread *thread;

	if (mono_domain_get () == domain)
		return;

	SET_APPDOMAIN (domain);

	if (migrate_exception) {
		thread = mono_thread_internal_current ();
		if (!thread->abort_exc)
			return;

		g_assert (thread->abort_exc->object.vtable->domain != domain);
		MONO_OBJECT_SETREF_INTERNAL (thread, abort_exc, mono_get_exception_thread_abort ());
		g_assert (thread->abort_exc->object.vtable->domain == domain);
	}
}

// Intended only for loading the main assembly
MonoAssembly *
mono_domain_assembly_open_internal (MonoAssemblyLoadContext *alc, const char *name)
{
	MonoAssembly *ass;

	MONO_REQ_GC_UNSAFE_MODE;

	MonoAssemblyOpenRequest req;
	mono_assembly_request_prepare_open (&req, alc);
	ass = mono_assembly_request_open (name, &req, NULL);

	// On netcore, this is necessary because we check the AppContext.BaseDirectory property as part of the assembly lookup algorithm
	// AppContext.BaseDirectory can sometimes fall back to checking the location of the entry_assembly, which should be non-null
	mono_runtime_ensure_entry_assembly (ass);

	return ass;
}

/**
 * mono_get_corlib:
 * Use this function to get the \c MonoImage* for the \c mscorlib.dll assembly
 * \returns The \c MonoImage for mscorlib.dll
 */
MonoImage*
mono_get_corlib (void)
{
	return mono_defaults.corlib;
}

/**
 * mono_get_object_class:
 * Use this function to get the \c MonoClass* that the runtime is using for \c System.Object .
 * \returns The \c MonoClass* for the \c System.Object type.
 */
MonoClass*
mono_get_object_class (void)
{
	return mono_defaults.object_class;
}

/**
 * mono_get_byte_class:
 * Use this function to get the \c MonoClass* that the runtime is using for \c System.Byte .
 * \returns The \c MonoClass* for the \c System.Byte type.
 */
MonoClass*
mono_get_byte_class (void)
{
	return mono_defaults.byte_class;
}

/**
 * mono_get_void_class:
 * Use this function to get the \c MonoClass* that the runtime is using for \c System.Void .
 * \returns The \c MonoClass* for the \c System.Void type.
 */
MonoClass*
mono_get_void_class (void)
{
	return mono_defaults.void_class;
}

/**
 * mono_get_boolean_class:
 * Use this function to get the \c MonoClass* that the runtime is using for \c System.Boolean .
 * \returns The \c MonoClass* for the \c System.Boolean type.
 */
MonoClass*
mono_get_boolean_class (void)
{
	return mono_defaults.boolean_class;
}

/**
 * mono_get_sbyte_class:
 * Use this function to get the \c MonoClass* that the runtime is using for \c System.SByte.
 * \returns The \c MonoClass* for the \c System.SByte type.
 */
MonoClass*
mono_get_sbyte_class (void)
{
	return mono_defaults.sbyte_class;
}

/**
 * mono_get_int16_class:
 * Use this function to get the \c MonoClass* that the runtime is using for \c System.Int16 .
 * \returns The \c MonoClass* for the \c System.Int16 type.
 */
MonoClass*
mono_get_int16_class (void)
{
	return mono_defaults.int16_class;
}

/**
 * mono_get_uint16_class:
 * Use this function to get the \c MonoClass* that the runtime is using for \c System.UInt16 .
 * \returns The \c MonoClass* for the \c System.UInt16 type.
 */
MonoClass*
mono_get_uint16_class (void)
{
	return mono_defaults.uint16_class;
}

/**
 * mono_get_int32_class:
 * Use this function to get the \c MonoClass* that the runtime is using for \c System.Int32 .
 * \returns The \c MonoClass* for the \c System.Int32 type.
 */
MonoClass*
mono_get_int32_class (void)
{
	return mono_defaults.int32_class;
}

/**
 * mono_get_uint32_class:
 * Use this function to get the \c MonoClass* that the runtime is using for \c System.UInt32 .
 * \returns The \c MonoClass* for the \c System.UInt32 type.
 */
MonoClass*
mono_get_uint32_class (void)
{
	return mono_defaults.uint32_class;
}

/**
 * mono_get_intptr_class:
 * Use this function to get the \c MonoClass* that the runtime is using for \c System.IntPtr .
 * \returns The \c MonoClass* for the \c System.IntPtr type.
 */
MonoClass*
mono_get_intptr_class (void)
{
	return mono_defaults.int_class;
}

/**
 * mono_get_uintptr_class:
 * Use this function to get the \c MonoClass* that the runtime is using for \c System.UIntPtr .
 * \returns The \c MonoClass* for the \c System.UIntPtr type.
 */
MonoClass*
mono_get_uintptr_class (void)
{
	return mono_defaults.uint_class;
}

/**
 * mono_get_int64_class:
 * Use this function to get the \c MonoClass* that the runtime is using for \c System.Int64 .
 * \returns The \c MonoClass* for the \c System.Int64 type.
 */
MonoClass*
mono_get_int64_class (void)
{
	return mono_defaults.int64_class;
}

/**
 * mono_get_uint64_class:
 * Use this function to get the \c MonoClass* that the runtime is using for \c System.UInt64 .
 * \returns The \c MonoClass* for the \c System.UInt64 type.
 */
MonoClass*
mono_get_uint64_class (void)
{
	return mono_defaults.uint64_class;
}

/**
 * mono_get_single_class:
 * Use this function to get the \c MonoClass* that the runtime is using for \c System.Single  (32-bit floating points).
 * \returns The \c MonoClass* for the \c System.Single type.
 */
MonoClass*
mono_get_single_class (void)
{
	return mono_defaults.single_class;
}

/**
 * mono_get_double_class:
 * Use this function to get the \c MonoClass* that the runtime is using for \c System.Double  (64-bit floating points).
 * \returns The \c MonoClass* for the \c System.Double type.
 */
MonoClass*
mono_get_double_class (void)
{
	return mono_defaults.double_class;
}

/**
 * mono_get_char_class:
 * Use this function to get the \c MonoClass* that the runtime is using for \c System.Char .
 * \returns The \c MonoClass* for the \c System.Char type.
 */
MonoClass*
mono_get_char_class (void)
{
	return mono_defaults.char_class;
}

/**
 * mono_get_string_class:
 * Use this function to get the \c MonoClass* that the runtime is using for \c System.String .
 * \returns The \c MonoClass* for the \c System.String type.
 */
MonoClass*
mono_get_string_class (void)
{
	return mono_defaults.string_class;
}

/**
 * mono_get_enum_class:
 * Use this function to get the \c MonoClass* that the runtime is using for \c System.Enum .
 * \returns The \c MonoClass* for the \c System.Enum type.
 */
MonoClass*
mono_get_enum_class (void)
{
	return mono_defaults.enum_class;
}

/**
 * mono_get_array_class:
 * Use this function to get the \c MonoClass* that the runtime is using for \c System.Array .
 * \returns The \c MonoClass* for the \c System.Array type.
 */
MonoClass*
mono_get_array_class (void)
{
	return mono_defaults.array_class;
}

/**
 * mono_get_thread_class:
 * Use this function to get the \c MonoClass* that the runtime is using for \c System.Threading.Thread .
 * \returns The \c MonoClass* for the \c System.Threading.Thread type.
 */
MonoClass*
mono_get_thread_class (void)
{
	return mono_defaults.thread_class;
}

/**
 * mono_get_exception_class:
 * Use this function to get the \c MonoClass* that the runtime is using for \c System.Exception .
 * \returns The \c MonoClass* for the \c  type.
 */
MonoClass*
mono_get_exception_class (void)
{
	return mono_defaults.exception_class;
}

static const MonoRuntimeInfo*
get_runtime_by_version (const char *version)
{
	int n;
	int max = G_N_ELEMENTS (supported_runtimes);
	size_t vlen;

	if (!version)
		return NULL;

	for (n=0; n<max; n++) {
		if (strcmp (version, supported_runtimes[n].runtime_version) == 0)
			return &supported_runtimes[n];
	}

	vlen = strlen (version);
	if (vlen >= 4 && version [1] - '0' >= 4) {
		for (n=0; n<max; n++) {
			if (strncmp (version, supported_runtimes[n].runtime_version, 4) == 0)
				return &supported_runtimes[n];
		}
	}

	return NULL;
}

static GSList*
get_runtimes_from_exe (const char *file, MonoImage **out_image)
{
	const MonoRuntimeInfo* runtime = NULL;
	MonoImage *image = NULL;
	GSList *runtimes = NULL;

	/* Look for a runtime with the exact version */
	image = mono_assembly_open_from_bundle (mono_alc_get_default (), file, NULL, NULL);

	if (image == NULL)
		image = mono_image_open (file, NULL);

	if (image == NULL) {
		/* The image is wrong or the file was not found. In this case return
		 * a default runtime and leave to the initialization method the work of
		 * reporting the error.
		 */
		runtime = get_runtime_by_version (DEFAULT_RUNTIME_VERSION);
		runtimes = g_slist_prepend (runtimes, (gpointer)runtime);
		return runtimes;
	}

	*out_image = image;

	runtime = get_runtime_by_version (image->version);
	if (runtime != NULL)
		runtimes = g_slist_prepend (runtimes, (gpointer)runtime);
	return runtimes;
}

/**
 * mono_get_runtime_info:
 *
 * Returns: the version of the current runtime instance.
 */
const MonoRuntimeInfo*
mono_get_runtime_info (void)
{
	return current_runtime;
}
