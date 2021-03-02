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
#include <mono/metadata/w32mutex.h>
#include <mono/metadata/w32semaphore.h>
#include <mono/metadata/w32event.h>
#include <mono/metadata/w32file.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/coree.h>
#include <mono/utils/mono-experiments.h>
#include <mono/utils/w32subset.h>
#include "external-only.h"
#include "mono/utils/mono-tls-inline.h"

#define GET_APPDOMAIN    mono_tls_get_domain
#define SET_APPDOMAIN(x) do { \
	MonoThreadInfo *info; \
	mono_tls_set_domain (x); \
	info = mono_thread_info_current (); \
	if (info) \
		mono_thread_info_tls_set (info, TLS_KEY_DOMAIN, (x));	\
} while (FALSE)

#define GET_APPCONTEXT() NULL
#define SET_APPCONTEXT(x)

static guint16 appdomain_list_size = 0;
static guint16 appdomain_next = 0;
static MonoDomain **appdomains_list = NULL;
static MonoImage *exe_image;

gboolean mono_dont_free_domains;

#define mono_appdomains_lock() mono_coop_mutex_lock (&appdomains_mutex)
#define mono_appdomains_unlock() mono_coop_mutex_unlock (&appdomains_mutex)
static MonoCoopMutex appdomains_mutex;

static MonoDomain *mono_root_domain = NULL;

/* some statistics */
static int max_domain_code_size = 0;
static int max_domain_code_alloc = 0;
static int total_domain_code_alloc = 0;

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

gboolean
mono_string_equal_internal (MonoString *s1, MonoString *s2)
{
	int l1 = mono_string_length_internal (s1);
	int l2 = mono_string_length_internal (s2);

	if (s1 == s2)
		return TRUE;
	if (l1 != l2)
		return FALSE;

	return memcmp (mono_string_chars_internal (s1), mono_string_chars_internal (s2), l1 * 2) == 0;
}

/**
 * mono_string_equal:
 * \param s1 First string to compare
 * \param s2 Second string to compare
 *
 * Compares two \c MonoString* instances ordinally for equality.
 *
 * \returns FALSE if the strings differ.
 */
gboolean
mono_string_equal (MonoString *s1, MonoString *s2)
{
	MONO_EXTERNAL_ONLY (gboolean, mono_string_equal_internal (s1, s2));
}

guint
mono_string_hash_internal (MonoString *s)
{
	const gunichar2 *p = mono_string_chars_internal (s);
	int i, len = mono_string_length_internal (s);
	guint h = 0;

	for (i = 0; i < len; i++) {
		h = (h << 5) - h + *p;
		p++;
	}

	return h;	
}

/**
 * mono_string_hash:
 * \param s the string to hash
 *
 * Compute the hash for a \c MonoString*
 * \returns the hash for the string.
 */
guint
mono_string_hash (MonoString *s)
{
	MONO_EXTERNAL_ONLY (guint, mono_string_hash_internal (s));
}

static gboolean
mono_ptrarray_equal (gpointer *s1, gpointer *s2)
{
	int len = GPOINTER_TO_INT (s1 [0]);
	if (len != GPOINTER_TO_INT (s2 [0]))
		return FALSE;

	return memcmp (s1 + 1, s2 + 1, len * sizeof(gpointer)) == 0; 
}

static guint
mono_ptrarray_hash (gpointer *s)
{
	int i;
	int len = GPOINTER_TO_INT (s [0]);
	guint hash = 0;
	
	for (i = 1; i < len; i++)
		hash += GPOINTER_TO_UINT (s [i]);

	return hash;	
}

//g_malloc on sgen and mono_gc_alloc_fixed on boehm
static void*
gc_alloc_fixed_non_heap_list (size_t size)
{
	if (mono_gc_is_moving ())
		return g_malloc0 (size);
	else
		return mono_gc_alloc_fixed (size, MONO_GC_DESCRIPTOR_NULL, MONO_ROOT_SOURCE_DOMAIN, NULL, "Domain List");
}

static void
gc_free_fixed_non_heap_list (void *ptr)
{
	if (mono_gc_is_moving ())
		g_free (ptr);
	else
		mono_gc_free_fixed (ptr);
}
/*
 * Allocate an id for domain and set domain->domain_id.
 * LOCKING: must be called while holding appdomains_mutex.
 * We try to assign low numbers to the domain, so it can be used
 * as an index in data tables to lookup domain-specific info
 * with minimal memory overhead. We also try not to reuse the
 * same id too quickly (to help debugging).
 */
static int
domain_id_alloc (MonoDomain *domain)
{
	int id = -1, i;
	if (!appdomains_list) {
		appdomain_list_size = 2;
		appdomains_list = (MonoDomain **)gc_alloc_fixed_non_heap_list (appdomain_list_size * sizeof (void*));

	}
	for (i = appdomain_next; i < appdomain_list_size; ++i) {
		if (!appdomains_list [i]) {
			id = i;
			break;
		}
	}
	if (id == -1) {
		for (i = 0; i < appdomain_next; ++i) {
			if (!appdomains_list [i]) {
				id = i;
				break;
			}
		}
	}
	if (id == -1) {
		MonoDomain **new_list;
		int new_size = appdomain_list_size * 2;
		if (new_size >= (1 << 16))
			g_assert_not_reached ();
		id = appdomain_list_size;
		new_list = (MonoDomain **)gc_alloc_fixed_non_heap_list (new_size * sizeof (void*));
		memcpy (new_list, appdomains_list, appdomain_list_size * sizeof (void*));
		gc_free_fixed_non_heap_list (appdomains_list);
		appdomains_list = new_list;
		appdomain_list_size = new_size;
	}
	domain->domain_id = id;
	appdomains_list [id] = domain;
	appdomain_next++;
	if (appdomain_next > appdomain_list_size)
		appdomain_next = 0;
	return id;
}

static gsize domain_gc_bitmap [sizeof(MonoDomain)/4/32 + 1];
static MonoGCDescriptor domain_gc_desc = MONO_GC_DESCRIPTOR_NULL;

/**
 * mono_domain_create:
 *
 * Creates a new application domain, the unmanaged representation
 * of the actual domain.
 *
 * Application domains provide an isolation facilty for assemblies.   You
 * can load assemblies and execute code in them that will not be visible
 * to other application domains. This is a runtime-based virtualization
 * technology.
 *
 * It is possible to unload domains, which unloads the assemblies and
 * data that was allocated in that domain.
 *
 * When a domain is created a mempool is allocated for domain-specific
 * structures, along a dedicated code manager to hold code that is
 * associated with the domain.
 *
 * \returns New initialized \c MonoDomain, with no configuration or assemblies
 * loaded into it.
 */
MonoDomain *
mono_domain_create (void)
{
	MonoDomain *domain;
  
	mono_appdomains_lock ();
  
	if (!domain_gc_desc) {
		unsigned int i, bit = 0;
		for (i = G_STRUCT_OFFSET (MonoDomain, MONO_DOMAIN_FIRST_OBJECT); i < G_STRUCT_OFFSET (MonoDomain, MONO_DOMAIN_FIRST_GC_TRACKED); i += sizeof (gpointer)) {
			bit = i / sizeof (gpointer);
			domain_gc_bitmap [bit / 32] |= (gsize) 1 << (bit % 32);
		}
		domain_gc_desc = mono_gc_make_descr_from_bitmap ((gsize*)domain_gc_bitmap, bit + 1);
	}
	mono_appdomains_unlock ();

	if (!mono_gc_is_moving ())
		domain = (MonoDomain *)mono_gc_alloc_fixed (sizeof (MonoDomain), MONO_GC_DESCRIPTOR_NULL, MONO_ROOT_SOURCE_DOMAIN, NULL, "Domain Structure");
	else
		domain = (MonoDomain *)mono_gc_alloc_fixed (sizeof (MonoDomain), domain_gc_desc, MONO_ROOT_SOURCE_DOMAIN, NULL, "Domain Structure");

	domain->domain = NULL;
	domain->friendly_name = NULL;
	domain->search_path = NULL;

	MONO_PROFILER_RAISE (domain_loading, (domain));

	domain->env = mono_g_hash_table_new_type_internal ((GHashFunc)mono_string_hash_internal, (GCompareFunc)mono_string_equal_internal, MONO_HASH_KEY_VALUE_GC, MONO_ROOT_SOURCE_DOMAIN, domain, "Domain Environment Variable Table");
	domain->domain_assemblies = NULL;
	mono_jit_code_hash_init (&domain->jit_code_hash);
	domain->ldstr_table = mono_g_hash_table_new_type_internal ((GHashFunc)mono_string_hash_internal, (GCompareFunc)mono_string_equal_internal, MONO_HASH_KEY_VALUE_GC, MONO_ROOT_SOURCE_DOMAIN, domain, "Domain String Pool Table");
	domain->num_jit_info_table_duplicates = 0;
	domain->jit_info_table = mono_jit_info_table_new (domain);
	domain->jit_info_free_queue = NULL;
	domain->finalizable_objects_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);
	domain->ftnptrs_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);

	mono_coop_mutex_init_recursive (&domain->lock);

	mono_coop_mutex_init_recursive (&domain->assemblies_lock);
	mono_os_mutex_init_recursive (&domain->jit_code_hash_lock);
	mono_os_mutex_init_recursive (&domain->finalizable_objects_hash_lock);

	mono_coop_mutex_init (&domain->alcs_lock);

	mono_appdomains_lock ();
	domain_id_alloc (domain);
	mono_appdomains_unlock ();

#ifndef DISABLE_PERFCOUNTERS
	mono_atomic_inc_i32 (&mono_perfcounters->loader_appdomains);
	mono_atomic_inc_i32 (&mono_perfcounters->loader_total_appdomains);
#endif

	mono_alc_create_default (domain);

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
	mono_w32handle_namespace_init ();
#endif

	mono_w32mutex_init ();
	mono_w32semaphore_init ();
	mono_w32event_init ();
	mono_w32file_init ();

#ifndef DISABLE_PERFCOUNTERS
	mono_perfcounters_init ();
#endif
	mono_counters_init ();

	mono_counters_register ("Max native code in a domain", MONO_COUNTER_INT|MONO_COUNTER_JIT, &max_domain_code_size);
	mono_counters_register ("Max code space allocated in a domain", MONO_COUNTER_INT|MONO_COUNTER_JIT, &max_domain_code_alloc);
	mono_counters_register ("Total code space allocated", MONO_COUNTER_INT|MONO_COUNTER_JIT, &total_domain_code_alloc);

	mono_counters_register ("Max HashTable Chain Length", MONO_COUNTER_INT|MONO_COUNTER_METADATA, &mono_g_hash_table_max_chain_length);

	mono_gc_base_init ();
	mono_thread_info_attach ();

	mono_coop_mutex_init_recursive (&appdomains_mutex);

	mono_metadata_init ();
	mono_images_init ();
	mono_assemblies_init ();
	mono_classes_init ();
	mono_loader_init ();
	mono_reflection_init ();
	mono_runtime_init_tls ();
	mono_icall_init ();

	domain = mono_domain_create ();
	mono_root_domain = domain;

	SET_APPDOMAIN (domain);

#if defined(ENABLE_EXPERIMENT_null)
	if (mono_experiment_enabled (MONO_EXPERIMENT_null))
		g_warning ("null experiment enabled");
#endif

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
			exe_image = mono_assembly_open_from_bundle (mono_domain_default_alc (domain), exe_filename, NULL, NULL);
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

/**
 * mono_cleanup:
 *
 * Cleans up all metadata modules. 
 */
void
mono_cleanup (void)
{
	mono_close_exe_image ();

	mono_thread_info_cleanup ();

	mono_defaults.corlib = NULL;

	mono_loader_cleanup ();
	mono_classes_cleanup ();
	mono_assemblies_cleanup ();
	mono_debug_cleanup ();
	mono_images_cleanup ();
	mono_metadata_cleanup ();

	mono_coop_mutex_destroy (&appdomains_mutex);

	mono_w32file_cleanup ();
}

void
mono_close_exe_image (void)
{
	gboolean do_close = exe_image != NULL;
#ifdef ENABLE_METADATA_UPDATE
	/* FIXME: shutdown hack. We mess something up and try to double-close/free it. */
	do_close = do_close && !exe_image->delta_image;
#endif
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
mono_domain_set_internal_with_options (MonoDomain *domain, gboolean migrate_exception)
{
	MONO_REQ_GC_UNSAFE_MODE;
	MonoInternalThread *thread;

	if (mono_domain_get () == domain)
		return;

	SET_APPDOMAIN (domain);
	SET_APPCONTEXT (domain->default_context);

	if (migrate_exception) {
		thread = mono_thread_internal_current ();
		if (!thread->abort_exc)
			return;

		g_assert (thread->abort_exc->object.vtable->domain != domain);
		MONO_OBJECT_SETREF_INTERNAL (thread, abort_exc, mono_get_exception_thread_abort ());
		g_assert (thread->abort_exc->object.vtable->domain == domain);
	}
}

/**
 * mono_domain_foreach:
 * \param func function to invoke with the domain data
 * \param user_data user-defined pointer that is passed to the supplied \p func fo reach domain
 *
 * Use this method to safely iterate over all the loaded application
 * domains in the current runtime.   The provided \p func is invoked with a
 * pointer to the \c MonoDomain and is given the value of the \p user_data
 * parameter which can be used to pass state to your called routine.
 */
void
mono_domain_foreach (MonoDomainFunc func, gpointer user_data)
{
	MONO_ENTER_GC_UNSAFE;
	int i, size;
	MonoDomain **copy;

	/*
	 * Create a copy of the data to avoid calling the user callback
	 * inside the lock because that could lead to deadlocks.
	 * We can do this because this function is not perf. critical.
	 */
	mono_appdomains_lock ();
	size = appdomain_list_size;
	copy = (MonoDomain **)gc_alloc_fixed_non_heap_list (appdomain_list_size * sizeof (void*));
	memcpy (copy, appdomains_list, appdomain_list_size * sizeof (void*));
	mono_appdomains_unlock ();

	for (i = 0; i < size; ++i) {
		if (copy [i])
			func (copy [i], user_data);
	}

	gc_free_fixed_non_heap_list (copy);
	MONO_EXIT_GC_UNSAFE;
}

void
mono_domain_ensure_entry_assembly (MonoDomain *domain, MonoAssembly *assembly)
{
	if (!mono_runtime_get_no_exec () && !domain->entry_assembly && assembly) {

		domain->entry_assembly = assembly;
	}
}

/**
 * mono_domain_assembly_open:
 * \param domain the application domain
 * \param name file name of the assembly
 */
MonoAssembly *
mono_domain_assembly_open (MonoDomain *domain, const char *name)
{
	MonoAssembly *result;
	MONO_ENTER_GC_UNSAFE;
	result = mono_domain_assembly_open_internal (domain, mono_domain_default_alc (domain), name);
	MONO_EXIT_GC_UNSAFE;
	return result;
}

// Uses the domain on legacy mono and the ALC on current
// Intended only for loading the main assembly
MonoAssembly *
mono_domain_assembly_open_internal (MonoDomain *domain, MonoAssemblyLoadContext *alc, const char *name)
{
	MonoDomain *current;
	MonoAssembly *ass;

	MONO_REQ_GC_UNSAFE_MODE;

	MonoAssemblyOpenRequest req;
	mono_assembly_request_prepare_open (&req, MONO_ASMCTX_DEFAULT, alc);
	if (domain != mono_domain_get ()) {
		current = mono_domain_get ();

		mono_domain_set_fast (domain, FALSE);
		ass = mono_assembly_request_open (name, &req, NULL);
		mono_domain_set_fast (current, FALSE);
	} else {
		ass = mono_assembly_request_open (name, &req, NULL);
	}

	// On netcore, this is necessary because we check the AppContext.BaseDirectory property as part of the assembly lookup algorithm
	// AppContext.BaseDirectory can sometimes fall back to checking the location of the entry_assembly, which should be non-null
	mono_domain_ensure_entry_assembly (domain, ass);

	return ass;
}

/**
 * mono_domain_free:
 * \param domain the domain to release
 * \param force if TRUE, it allows the root domain to be released (used at shutdown only).
 *
 * This releases the resources associated with the specific domain.
 * This is a low-level function that is invoked by the AppDomain infrastructure
 * when necessary.
 *
 * In theory, this is dead code on netcore and thus does not need to be ALC-aware.
 */
void
mono_domain_free (MonoDomain *domain, gboolean force)
{
	g_assert_not_reached ();
}

/**
 * mono_domain_get_by_id:
 * \param domainid the ID
 * \returns the domain for a specific domain id.
 */
MonoDomain * 
mono_domain_get_by_id (gint32 domainid) 
{
	MonoDomain * domain;

	MONO_ENTER_GC_UNSAFE;
	mono_appdomains_lock ();
	if (domainid < appdomain_list_size)
		domain = appdomains_list [domainid];
	else
		domain = NULL;
	mono_appdomains_unlock ();
	MONO_EXIT_GC_UNSAFE;
	return domain;
}

/**
 * mono_domain_get_id:
 *
 * A domain ID is guaranteed to be unique for as long as the domain
 * using it is alive. It may be reused later once the domain has been
 * unloaded.
 *
 * \returns The unique ID for \p domain.
 */
gint32
mono_domain_get_id (MonoDomain *domain)
{
	return domain->domain_id;
}

/**
 * mono_domain_get_friendly_name:
 *
 * The returned string's lifetime is the same as \p domain's. Consider
 * copying it if you need to store it somewhere.
 *
 * \returns The friendly name of \p domain. Can be NULL if not yet set.
 */
const char *
mono_domain_get_friendly_name (MonoDomain *domain)
{
	return domain->friendly_name;
}

/**
 * mono_context_set:
 */
void 
mono_context_set (MonoAppContext * new_context)
{
	SET_APPCONTEXT (new_context);
}

void
mono_context_set_handle (MonoAppContextHandle new_context)
{
	SET_APPCONTEXT (MONO_HANDLE_RAW (new_context));
}

/**
 * mono_context_get:
 *
 * Returns: the current Mono Application Context.
 */
MonoAppContext * 
mono_context_get (void)
{
	return GET_APPCONTEXT ();
}

/**
 * mono_context_get_handle:
 *
 * Returns: the current Mono Application Context.
 */
MonoAppContextHandle
mono_context_get_handle (void)
{
	return MONO_HANDLE_NEW (MonoAppContext, GET_APPCONTEXT ());
}

/**
 * mono_context_get_id:
 * \param context the context to operate on.
 *
 * Context IDs are guaranteed to be unique for the duration of a Mono
 * process; they are never reused.
 *
 * \returns The unique ID for \p context.
 */
gint32
mono_context_get_id (MonoAppContext *context)
{
	return context->context_id;
}

/**
 * mono_context_get_domain_id:
 * \param context the context to operate on.
 * \returns The ID of the domain that \p context was created in.
 */
gint32
mono_context_get_domain_id (MonoAppContext *context)
{
	return context->domain_id;
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
	int vlen;

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
	image = mono_assembly_open_from_bundle (mono_domain_default_alc (mono_domain_get ()), file, NULL, NULL);

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

void
mono_domain_lock (MonoDomain *domain)
{
	mono_locks_coop_acquire (&domain->lock, DomainLock);
}

void
mono_domain_unlock (MonoDomain *domain)
{
	mono_locks_coop_release (&domain->lock, DomainLock);
}

GPtrArray*
mono_domain_get_assemblies (MonoDomain *domain)
{
	GSList *tmp;
	GPtrArray *assemblies;
	MonoAssembly *ass;

	assemblies = g_ptr_array_new ();
	mono_domain_assemblies_lock (domain);
	for (tmp = domain->domain_assemblies; tmp; tmp = tmp->next) {
		ass = (MonoAssembly *)tmp->data;
		g_ptr_array_add (assemblies, ass);
	}
	mono_domain_assemblies_unlock (domain);
	return assemblies;
}

MonoAssemblyLoadContext *
mono_domain_default_alc (MonoDomain *domain)
{
	return domain->default_alc;
}
