
/*
 * domain.c: MonoDomain functions
 *
 * Author:
 *	Dietmar Maurer (dietmar@ximian.com)
 *	Patrik Torstensson
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>
#include <string.h>
#include <sys/stat.h>

#include <mono/os/gc_wrapper.h>

#include <mono/utils/mono-compiler.h>
#include <mono/metadata/object.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/domain-internals.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/cil-coff.h>
#include <mono/metadata/rawbuffer.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/gc-internal.h>
#include <mono/metadata/appdomain.h>
#include <metadata/threads.h>

/* #define DEBUG_DOMAIN_UNLOAD */

/* we need to use both the Tls* functions and __thread because
 * some archs may generate faster jit code with one meachanism
 * or the other (we used to do it because tls slots were GC-tracked,
 * but we can't depend on this).
 */
static guint32 appdomain_thread_id = -1;
 
#ifdef HAVE_KW_THREAD
static __thread MonoDomain * tls_appdomain MONO_TLS_FAST;
#define GET_APPDOMAIN() tls_appdomain
#define SET_APPDOMAIN(x) do { \
	tls_appdomain = x; \
	TlsSetValue (appdomain_thread_id, x); \
} while (FALSE)

#else

#define GET_APPDOMAIN() ((MonoDomain *)TlsGetValue (appdomain_thread_id))
#define SET_APPDOMAIN(x) TlsSetValue (appdomain_thread_id, x);

#endif

#define GET_APPCONTEXT() (mono_thread_current ()->current_appcontext)
#define SET_APPCONTEXT(x) MONO_OBJECT_SETREF (mono_thread_current (), current_appcontext, (x))

static guint16 appdomain_list_size = 0;
static guint16 appdomain_next = 0;
static MonoDomain **appdomains_list = NULL;

#define mono_appdomains_lock() EnterCriticalSection (&appdomains_mutex)
#define mono_appdomains_unlock() LeaveCriticalSection (&appdomains_mutex)
static CRITICAL_SECTION appdomains_mutex;

static MonoDomain *mono_root_domain = NULL;

/* AppConfigInfo: Information about runtime versions supported by an 
 * aplication.
 */
typedef struct {
	GSList *supported_runtimes;
	char *required_runtime;
	int configuration_count;
	int startup_count;
} AppConfigInfo;

/*
 * AotModuleInfo: Contains information about AOT modules.
 */
typedef struct {
	MonoImage *image;
	gpointer start, end;
} AotModuleInfo;

static const MonoRuntimeInfo *current_runtime = NULL;

static MonoJitInfoFindInAot jit_info_find_in_aot_func = NULL;

/*
 * Contains information about AOT loaded code.
 */
static MonoJitInfoTable *aot_modules = NULL;

/* This is the list of runtime versions supported by this JIT.
 */
static const MonoRuntimeInfo supported_runtimes[] = {
	{"v1.0.3705", "1.0", { {1,0,5000,0}, {7,0,5000,0} }	},
	{"v1.1.4322", "1.0", { {1,0,5000,0}, {7,0,5000,0} }	},
	{"v2.0.50215","2.0", { {2,0,0,0},    {8,0,0,0} }	},
	{"v2.0.50727","2.0", { {2,0,0,0},    {8,0,0,0} }	}
};


/* The stable runtime version */
#define DEFAULT_RUNTIME_VERSION "v1.1.4322"

static void
get_runtimes_from_exe (const char *exe_file, const MonoRuntimeInfo** runtimes);

static const MonoRuntimeInfo*
get_runtime_by_version (const char *version);

static MonoImage*
mono_jit_info_find_aot_module (guint8* addr);

guint32
mono_domain_get_tls_key (void)
{
	return appdomain_thread_id;
}

gint32
mono_domain_get_tls_offset (void)
{
	int offset = -1;
	MONO_THREAD_VAR_OFFSET (tls_appdomain, offset);
/*	__asm ("jmp 1f; .section writetext, \"awx\"; 1: movl $tls_appdomain@ntpoff, %0; jmp 2f; .previous; 2:" 
		: "=r" (offset));*/
	return offset;
}

static MonoJitInfoTable *
mono_jit_info_table_new (void)
{
	return g_array_new (FALSE, FALSE, sizeof (gpointer));
}

static void
mono_jit_info_table_free (MonoJitInfoTable *table)
{
	g_array_free (table, TRUE);
}

static int
mono_jit_info_table_index (MonoJitInfoTable *table, char *addr)
{
	int left = 0, right = table->len;

	while (left < right) {
		int pos = (left + right) / 2;
		MonoJitInfo *ji = g_array_index (table, gpointer, pos);
		char *start = ji->code_start;
		char *end = start + ji->code_size;

		if (addr < start)
			right = pos;
		else if (addr >= end) 
			left = pos + 1;
		else
			return pos;
	}

	return left;
}

MonoJitInfo *
mono_jit_info_table_find (MonoDomain *domain, char *addr)
{
	MonoJitInfoTable *table = domain->jit_info_table;
	MonoJitInfo *ji;
	guint left = 0, right;

	mono_domain_lock (domain);

	right = table->len;
	while (left < right) {
		guint pos = (left + right) / 2;
		ji = g_array_index (table, gpointer, pos);

		if (addr < (char*)ji->code_start)
			right = pos;
		else if (addr >= (char*)ji->code_start + ji->code_size) 
			left = pos + 1;
		else {
			mono_domain_unlock (domain);
			return ji;
		}
	}
	mono_domain_unlock (domain);

	/* maybe it is shared code, so we also search in the root domain */
	ji = NULL;
	if (domain != mono_root_domain)
		ji = mono_jit_info_table_find (mono_root_domain, addr);

	if (ji == NULL) {
		/* Maybe its an AOT module */
		MonoImage *image = mono_jit_info_find_aot_module (addr);
		if (image)
			ji = jit_info_find_in_aot_func (domain, image, addr);
	}
	
	return ji;
}

void
mono_jit_info_table_add (MonoDomain *domain, MonoJitInfo *ji)
{
	MonoJitInfoTable *table = domain->jit_info_table;
	gpointer start = ji->code_start;
	int pos;

	mono_domain_lock (domain);
	pos = mono_jit_info_table_index (table, start);

	g_array_insert_val (table, pos, ji);
	mono_domain_unlock (domain);
}

void
mono_jit_info_table_remove (MonoDomain *domain, MonoJitInfo *ji)
{
	MonoJitInfoTable *table = domain->jit_info_table;
	gpointer start = ji->code_start;
	int pos;

	mono_domain_lock (domain);
	pos = mono_jit_info_table_index (table, start);
	if (g_array_index (table, gpointer, pos) != ji) {
		MonoJitInfo *ji2 = g_array_index (table, gpointer, pos);
		g_assert (ji == ji2);
	}
	g_assert (g_array_index (table, gpointer, pos) == ji);

	g_array_remove_index (table, pos);
	mono_domain_unlock (domain);
}	

static int
aot_info_table_index (MonoJitInfoTable *table, char *addr)
{
	int left = 0, right = table->len;

	while (left < right) {
		int pos = (left + right) / 2;
		AotModuleInfo *ainfo = g_array_index (table, gpointer, pos);
		char *start = ainfo->start;
		char *end = ainfo->end;

		if (addr < start)
			right = pos;
		else if (addr >= end) 
			left = pos + 1;
		else
			return pos;
	}

	return left;
}

void
mono_jit_info_add_aot_module (MonoImage *image, gpointer start, gpointer end)
{
	AotModuleInfo *ainfo = g_new0 (AotModuleInfo, 1);
	int pos;

	ainfo->image = image;
	ainfo->start = start;
	ainfo->end = end;

	mono_appdomains_lock ();

	if (!aot_modules)
		aot_modules = mono_jit_info_table_new ();

	pos = aot_info_table_index (aot_modules, start);

	g_array_insert_val (aot_modules, pos, ainfo);

	mono_appdomains_unlock ();
}

static MonoImage*
mono_jit_info_find_aot_module (guint8* addr)
{
	guint left = 0, right;

	if (!aot_modules)
		return NULL;

	mono_appdomains_lock ();

	right = aot_modules->len;
	while (left < right) {
		guint pos = (left + right) / 2;
		AotModuleInfo *ai = g_array_index (aot_modules, gpointer, pos);

		if (addr < (guint8*)ai->start)
			right = pos;
		else if (addr >= (guint8*)ai->end)
			left = pos + 1;
		else {
			mono_appdomains_unlock ();
			return ai->image;
		}
	}

	mono_appdomains_unlock ();

	return NULL;
}

void
mono_install_jit_info_find_in_aot (MonoJitInfoFindInAot func)
{
	jit_info_find_in_aot_func = func;
}

gpointer
mono_jit_info_get_code_start (MonoJitInfo* ji)
{
	return ji->code_start;
}

int
mono_jit_info_get_code_size (MonoJitInfo* ji)
{
	return ji->code_size;
}

MonoMethod*
mono_jit_info_get_method (MonoJitInfo* ji)
{
	return ji->method;
}

gboolean
mono_string_equal (MonoString *s1, MonoString *s2)
{
	int l1 = mono_string_length (s1);
	int l2 = mono_string_length (s2);

	if (s1 == s2)
		return TRUE;
	if (l1 != l2)
		return FALSE;

	return memcmp (mono_string_chars (s1), mono_string_chars (s2), l1 * 2) == 0; 
}

guint
mono_string_hash (MonoString *s)
{
	const guint16 *p = mono_string_chars (s);
	int i, len = mono_string_length (s);
	guint h = 0;

	for (i = 0; i < len; i++) {
		h = (h << 5) - h + *p;
		p++;
	}

	return h;	
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
		appdomains_list = mono_gc_alloc_fixed (appdomain_list_size * sizeof (void*), NULL);
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
		new_list = mono_gc_alloc_fixed (new_size * sizeof (void*), NULL);
		memcpy (new_list, appdomains_list, appdomain_list_size * sizeof (void*));
		mono_gc_free_fixed (appdomains_list);
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

MonoDomain *
mono_domain_create (void)
{
	MonoDomain *domain;

	domain = mono_gc_alloc_fixed (sizeof (MonoDomain), NULL);
	domain->domain = NULL;
	domain->setup = NULL;
	domain->friendly_name = NULL;
	domain->search_path = NULL;

	domain->mp = mono_mempool_new ();
	domain->code_mp = mono_code_manager_new ();
	domain->env = mono_g_hash_table_new ((GHashFunc)mono_string_hash, (GCompareFunc)mono_string_equal);
	domain->domain_assemblies = NULL;
	domain->class_vtable_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);
	domain->proxy_vtable_hash = mono_g_hash_table_new ((GHashFunc)mono_ptrarray_hash, (GCompareFunc)mono_ptrarray_equal);
	domain->static_data_hash = mono_g_hash_table_new (mono_aligned_addr_hash, NULL);
	domain->jit_code_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);
	domain->ldstr_table = mono_g_hash_table_new ((GHashFunc)mono_string_hash, (GCompareFunc)mono_string_equal);
	domain->jit_info_table = mono_jit_info_table_new ();
	domain->class_init_trampoline_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);
	domain->jump_trampoline_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);
	domain->finalizable_objects_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);
	domain->jit_trampoline_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);
	domain->delegate_trampoline_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);

	InitializeCriticalSection (&domain->lock);
	InitializeCriticalSection (&domain->assemblies_lock);

	mono_appdomains_lock ();
	domain_id_alloc (domain);
	mono_appdomains_unlock ();

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
	const MonoRuntimeInfo* runtimes [G_N_ELEMENTS (supported_runtimes) + 1];
	int n;

	if (domain)
		g_assert_not_reached ();

	MONO_GC_PRE_INIT ();

	appdomain_thread_id = TlsAlloc ();

	InitializeCriticalSection (&appdomains_mutex);

	mono_metadata_init ();
	mono_raw_buffer_init ();
	mono_images_init ();
	mono_assemblies_init ();
	mono_loader_init ();

	/* FIXME: When should we release this memory? */
	MONO_GC_REGISTER_ROOT (appdomains_list);

	domain = mono_domain_create ();
	mono_root_domain = domain;

	SET_APPDOMAIN (domain);
	
	/* Get a list of runtimes supported by the exe */
	if (exe_filename != NULL) {
		get_runtimes_from_exe (exe_filename, runtimes);
	} else if (runtime_version != NULL) {
		runtimes [0] = get_runtime_by_version (runtime_version);
		runtimes [1] = NULL;
	}

	if (runtimes [0] == NULL) {
		const MonoRuntimeInfo *default_runtime = get_runtime_by_version (DEFAULT_RUNTIME_VERSION);
		runtimes [0] = default_runtime;
		runtimes [1] = NULL;
		g_print ("WARNING: The runtime version supported by this application is unavailable.\n");
		g_print ("Using default runtime: %s\n", default_runtime->runtime_version);
	}

	/* The selected runtime will be the first one for which there is a mscrolib.dll */
	for (n = 0; runtimes [n] != NULL && ass == NULL; n++) {
		current_runtime = runtimes [n];
		ass = mono_assembly_load_corlib (current_runtime, &status);
		if (status != MONO_IMAGE_OK && status != MONO_IMAGE_ERROR_ERRNO)
			break;
	}

	if ((status != MONO_IMAGE_OK) || (ass == NULL)) {
		switch (status){
		case MONO_IMAGE_ERROR_ERRNO: {
			char *corlib_file = g_build_filename (mono_assembly_getrootdir (), "mono", current_runtime->framework_version, "mscorlib.dll", NULL);
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
	mono_defaults.corlib = mono_assembly_get_image (ass);

	mono_defaults.object_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Object");
	g_assert (mono_defaults.object_class != 0);

	mono_defaults.void_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Void");
	g_assert (mono_defaults.void_class != 0);

	mono_defaults.boolean_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Boolean");
	g_assert (mono_defaults.boolean_class != 0);

	mono_defaults.byte_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Byte");
	g_assert (mono_defaults.byte_class != 0);

	mono_defaults.sbyte_class = mono_class_from_name (
                mono_defaults.corlib, "System", "SByte");
	g_assert (mono_defaults.sbyte_class != 0);

	mono_defaults.int16_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Int16");
	g_assert (mono_defaults.int16_class != 0);

	mono_defaults.uint16_class = mono_class_from_name (
                mono_defaults.corlib, "System", "UInt16");
	g_assert (mono_defaults.uint16_class != 0);

	mono_defaults.int32_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Int32");
	g_assert (mono_defaults.int32_class != 0);

	mono_defaults.uint32_class = mono_class_from_name (
                mono_defaults.corlib, "System", "UInt32");
	g_assert (mono_defaults.uint32_class != 0);

	mono_defaults.uint_class = mono_class_from_name (
                mono_defaults.corlib, "System", "UIntPtr");
	g_assert (mono_defaults.uint_class != 0);

	mono_defaults.int_class = mono_class_from_name (
                mono_defaults.corlib, "System", "IntPtr");
	g_assert (mono_defaults.int_class != 0);

	mono_defaults.int64_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Int64");
	g_assert (mono_defaults.int64_class != 0);

	mono_defaults.uint64_class = mono_class_from_name (
                mono_defaults.corlib, "System", "UInt64");
	g_assert (mono_defaults.uint64_class != 0);

	mono_defaults.single_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Single");
	g_assert (mono_defaults.single_class != 0);

	mono_defaults.double_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Double");
	g_assert (mono_defaults.double_class != 0);

	mono_defaults.char_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Char");
	g_assert (mono_defaults.char_class != 0);

	mono_defaults.string_class = mono_class_from_name (
                mono_defaults.corlib, "System", "String");
	g_assert (mono_defaults.string_class != 0);

	mono_defaults.enum_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Enum");
	g_assert (mono_defaults.enum_class != 0);

	mono_defaults.array_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Array");
	g_assert (mono_defaults.array_class != 0);

	mono_defaults.delegate_class = mono_class_from_name (
		mono_defaults.corlib, "System", "Delegate");
	g_assert (mono_defaults.delegate_class != 0 );

	mono_defaults.multicastdelegate_class = mono_class_from_name (
		mono_defaults.corlib, "System", "MulticastDelegate");
	g_assert (mono_defaults.multicastdelegate_class != 0 );

	mono_defaults.asyncresult_class = mono_class_from_name (
		mono_defaults.corlib, "System.Runtime.Remoting.Messaging", 
		"AsyncResult");
	g_assert (mono_defaults.asyncresult_class != 0 );

	mono_defaults.waithandle_class = mono_class_from_name (
		mono_defaults.corlib, "System.Threading", "WaitHandle");
	g_assert (mono_defaults.waithandle_class != 0 );

	mono_defaults.typehandle_class = mono_class_from_name (
                mono_defaults.corlib, "System", "RuntimeTypeHandle");
	g_assert (mono_defaults.typehandle_class != 0);

	mono_defaults.methodhandle_class = mono_class_from_name (
                mono_defaults.corlib, "System", "RuntimeMethodHandle");
	g_assert (mono_defaults.methodhandle_class != 0);

	mono_defaults.fieldhandle_class = mono_class_from_name (
                mono_defaults.corlib, "System", "RuntimeFieldHandle");
	g_assert (mono_defaults.fieldhandle_class != 0);

	mono_defaults.systemtype_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Type");
	g_assert (mono_defaults.systemtype_class != 0);

	mono_defaults.monotype_class = mono_class_from_name (
                mono_defaults.corlib, "System", "MonoType");
	g_assert (mono_defaults.monotype_class != 0);

	mono_defaults.exception_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Exception");
	g_assert (mono_defaults.exception_class != 0);

	mono_defaults.threadabortexception_class = mono_class_from_name (
                mono_defaults.corlib, "System.Threading", "ThreadAbortException");
	g_assert (mono_defaults.threadabortexception_class != 0);

	mono_defaults.thread_class = mono_class_from_name (
                mono_defaults.corlib, "System.Threading", "Thread");
	g_assert (mono_defaults.thread_class != 0);

	mono_defaults.appdomain_class = mono_class_from_name (
                mono_defaults.corlib, "System", "AppDomain");
	g_assert (mono_defaults.appdomain_class != 0);

	mono_defaults.transparent_proxy_class = mono_class_from_name (
                mono_defaults.corlib, "System.Runtime.Remoting.Proxies", "TransparentProxy");
	g_assert (mono_defaults.transparent_proxy_class != 0);

	mono_defaults.real_proxy_class = mono_class_from_name (
                mono_defaults.corlib, "System.Runtime.Remoting.Proxies", "RealProxy");
	g_assert (mono_defaults.real_proxy_class != 0);

	mono_defaults.mono_method_message_class = mono_class_from_name (
                mono_defaults.corlib, "System.Runtime.Remoting.Messaging", "MonoMethodMessage");
	g_assert (mono_defaults.mono_method_message_class != 0);

	mono_defaults.field_info_class = mono_class_from_name (
		mono_defaults.corlib, "System.Reflection", "FieldInfo");
	g_assert (mono_defaults.field_info_class != 0);

	mono_defaults.method_info_class = mono_class_from_name (
		mono_defaults.corlib, "System.Reflection", "MethodInfo");
	g_assert (mono_defaults.method_info_class != 0);

	mono_defaults.stringbuilder_class = mono_class_from_name (
		mono_defaults.corlib, "System.Text", "StringBuilder");
	g_assert (mono_defaults.stringbuilder_class != 0);

	mono_defaults.math_class = mono_class_from_name (
	        mono_defaults.corlib, "System", "Math");
	g_assert (mono_defaults.math_class != 0);

	mono_defaults.stack_frame_class = mono_class_from_name (
	        mono_defaults.corlib, "System.Diagnostics", "StackFrame");
	g_assert (mono_defaults.stack_frame_class != 0);

	mono_defaults.stack_trace_class = mono_class_from_name (
	        mono_defaults.corlib, "System.Diagnostics", "StackTrace");
	g_assert (mono_defaults.stack_trace_class != 0);

	mono_defaults.marshal_class = mono_class_from_name (
	        mono_defaults.corlib, "System.Runtime.InteropServices", "Marshal");
	g_assert (mono_defaults.marshal_class != 0);

	mono_defaults.iserializeable_class = mono_class_from_name (
	        mono_defaults.corlib, "System.Runtime.Serialization", "ISerializable");
	g_assert (mono_defaults.iserializeable_class != 0);

	mono_defaults.serializationinfo_class = mono_class_from_name (
	        mono_defaults.corlib, "System.Runtime.Serialization", "SerializationInfo");
	g_assert (mono_defaults.serializationinfo_class != 0);

	mono_defaults.streamingcontext_class = mono_class_from_name (
	        mono_defaults.corlib, "System.Runtime.Serialization", "StreamingContext");
	g_assert (mono_defaults.streamingcontext_class != 0);

	mono_defaults.typed_reference_class =  mono_class_from_name (
	        mono_defaults.corlib, "System", "TypedReference");
	g_assert (mono_defaults.typed_reference_class != 0);

	mono_defaults.argumenthandle_class =  mono_class_from_name (
	        mono_defaults.corlib, "System", "RuntimeArgumentHandle");
	g_assert (mono_defaults.argumenthandle_class != 0);

	mono_defaults.marshalbyrefobject_class =  mono_class_from_name (
	        mono_defaults.corlib, "System", "MarshalByRefObject");
	g_assert (mono_defaults.marshalbyrefobject_class != 0);

	mono_defaults.monitor_class =  mono_class_from_name (
	        mono_defaults.corlib, "System.Threading", "Monitor");
	g_assert (mono_defaults.monitor_class != 0);

	mono_defaults.iremotingtypeinfo_class = mono_class_from_name (
	        mono_defaults.corlib, "System.Runtime.Remoting", "IRemotingTypeInfo");
	g_assert (mono_defaults.iremotingtypeinfo_class != 0);

	mono_defaults.runtimesecurityframe_class = mono_class_from_name (
	        mono_defaults.corlib, "System.Security", "RuntimeSecurityFrame");

	mono_defaults.executioncontext_class = mono_class_from_name (
	        mono_defaults.corlib, "System.Threading", "ExecutionContext");

	mono_defaults.internals_visible_class = mono_class_from_name (
	        mono_defaults.corlib, "System.Runtime.CompilerServices", "InternalsVisibleToAttribute");

	/*
	 * Note that mono_defaults.generic_*_class is only non-NULL if we're
	 * using the 2.0 corlib.
	 */
	mono_class_init (mono_defaults.array_class);
	mono_defaults.generic_array_class = mono_class_from_name (
	        mono_defaults.corlib, "System", "Array/InternalArray`1");
	mono_defaults.generic_nullable_class = mono_class_from_name (
		mono_defaults.corlib, "System", "Nullable`1");

	domain->friendly_name = g_path_get_basename (filename);

	return domain;
}

/**
 * mono_init:
 * 
 * Creates the initial application domain and initializes the mono_defaults
 * structure.
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
 * 
 * Creates the initial application domain and initializes the mono_defaults
 * structure.
 * This function is guaranteed to not run any IL code.
 * The runtime is initialized using the runtime version required by the
 * provided executable. The version is determined by looking at the exe 
 * configuration file and the version PE field)
 *
 * Returns: the initial domain.
 */
MonoDomain *
mono_init_from_assembly (const char *domain_name, const char *filename)
{
	return mono_init_internal (domain_name, filename, NULL);
}

/**
 * mono_init_version:
 * 
 * Creates the initial application domain and initializes the mono_defaults
 * structure.
 * This function is guaranteed to not run any IL code.
 * The runtime is initialized using the provided rutime version.
 *
 * Returns: the initial domain.
 */
MonoDomain *
mono_init_version (const char *domain_name, const char *version)
{
	return mono_init_internal (domain_name, NULL, version);
}

MonoDomain*
mono_get_root_domain (void)
{
	return mono_root_domain;
}

/**
 * mono_domain_get:
 *
 * Returns: the current domain.
 */
MonoDomain *
mono_domain_get ()
{
	return GET_APPDOMAIN ();
}

/**
 * mono_domain_set_internal:
 * @domain: the new domain
 *
 * Sets the current domain to @domain.
 */
void
mono_domain_set_internal (MonoDomain *domain)
{
	SET_APPDOMAIN (domain);
	SET_APPCONTEXT (domain->default_context);
}

void
mono_domain_foreach (MonoDomainFunc func, gpointer user_data)
{
	int i, size;
	MonoDomain **copy;

	/*
	 * Create a copy of the data to avoid calling the user callback
	 * inside the lock because that could lead to deadlocks.
	 * We can do this because this function is not perf. critical.
	 */
	mono_appdomains_lock ();
	size = appdomain_list_size;
	copy = mono_gc_alloc_fixed (appdomain_list_size * sizeof (void*), NULL);
	memcpy (copy, appdomains_list, appdomain_list_size * sizeof (void*));
	mono_appdomains_unlock ();

	for (i = 0; i < size; ++i) {
		if (copy [i])
			func (copy [i], user_data);
	}

	mono_gc_free_fixed (copy);
}

/**
 * mono_domain_assembly_open:
 * @domain: the application domain
 * @name: file name of the assembly
 *
 * fixme: maybe we should integrate this with mono_assembly_open ??
 */
MonoAssembly *
mono_domain_assembly_open (MonoDomain *domain, const char *name)
{
	MonoAssembly *ass;
	GSList *tmp;

	mono_domain_assemblies_lock (domain);
	for (tmp = domain->domain_assemblies; tmp; tmp = tmp->next) {
		ass = tmp->data;
		if (strcmp (name, ass->aname.name) == 0) {
			mono_domain_assemblies_unlock (domain);
			return ass;
		}
	}
	mono_domain_assemblies_unlock (domain);

	if (!(ass = mono_assembly_open (name, NULL)))
		return NULL;

	return ass;
}

static void
dynamic_method_info_free (gpointer key, gpointer value, gpointer user_data)
{
	MonoJitDynamicMethodInfo *di = value;
	mono_code_manager_destroy (di->code_mp);
	g_free (di);
}

static void
delete_jump_list (gpointer key, gpointer value, gpointer user_data)
{
	g_slist_free (value);
}

void
mono_domain_free (MonoDomain *domain, gboolean force)
{
	GSList *tmp;
	if ((domain == mono_root_domain) && !force) {
		g_warning ("cant unload root domain");
		return;
	}

	mono_appdomains_lock ();
	appdomains_list [domain->domain_id] = NULL;
	mono_appdomains_unlock ();

	/* FIXME: free delegate_hash_table when it's used */
	if (domain->search_path) {
		g_strfreev (domain->search_path);
		domain->search_path = NULL;
	}
	domain->create_proxy_for_type_method = NULL;
	domain->private_invoke_method = NULL;
	domain->default_context = NULL;
	domain->out_of_memory_ex = NULL;
	domain->null_reference_ex = NULL;
	domain->stack_overflow_ex = NULL;
	domain->entry_assembly = NULL;
	g_free (domain->friendly_name);
	domain->friendly_name = NULL;
	for (tmp = domain->domain_assemblies; tmp; tmp = tmp->next) {
		MonoAssembly *ass = tmp->data;
		/*g_print ("Unloading domain %p, assembly %s, refcount: %d\n", domain, ass->aname.name, ass->ref_count);*/
		mono_assembly_close (ass);
	}
	g_slist_free (domain->domain_assemblies);
	domain->domain_assemblies = NULL;

	mono_g_hash_table_destroy (domain->env);
	domain->env = NULL;
	g_hash_table_destroy (domain->class_vtable_hash);
	domain->class_vtable_hash = NULL;
	mono_g_hash_table_destroy (domain->proxy_vtable_hash);
	domain->proxy_vtable_hash = NULL;
	mono_g_hash_table_destroy (domain->static_data_hash);
	domain->static_data_hash = NULL;
	g_hash_table_destroy (domain->jit_code_hash);
	domain->jit_code_hash = NULL;
	if (domain->dynamic_code_hash) {
		g_hash_table_foreach (domain->dynamic_code_hash, dynamic_method_info_free, NULL);
		g_hash_table_destroy (domain->dynamic_code_hash);
		domain->dynamic_code_hash = NULL;
	}
	mono_g_hash_table_destroy (domain->ldstr_table);
	domain->ldstr_table = NULL;
	mono_jit_info_table_free (domain->jit_info_table);
	domain->jit_info_table = NULL;
#ifdef DEBUG_DOMAIN_UNLOAD
	mono_mempool_invalidate (domain->mp);
	mono_code_manager_invalidate (domain->code_mp);
#else
	mono_mempool_destroy (domain->mp);
	domain->mp = NULL;
	mono_code_manager_destroy (domain->code_mp);
	domain->code_mp = NULL;
#endif	
	if (domain->jump_target_hash) {
		g_hash_table_foreach (domain->jump_target_hash, delete_jump_list, NULL);
		g_hash_table_destroy (domain->jump_target_hash);
		domain->jump_target_hash = NULL;
	}
	if (domain->type_hash) {
		mono_g_hash_table_destroy (domain->type_hash);
		domain->type_hash = NULL;
	}
	if (domain->refobject_hash) {
		mono_g_hash_table_destroy (domain->refobject_hash);
		domain->refobject_hash = NULL;
	}
	g_hash_table_destroy (domain->class_init_trampoline_hash);
	domain->class_init_trampoline_hash = NULL;
	g_hash_table_destroy (domain->jump_trampoline_hash);
	domain->jump_trampoline_hash = NULL;
	g_hash_table_destroy (domain->finalizable_objects_hash);
	domain->finalizable_objects_hash = NULL;
	g_hash_table_destroy (domain->jit_trampoline_hash);
	domain->jit_trampoline_hash = NULL;
	g_hash_table_destroy (domain->delegate_trampoline_hash);
	domain->delegate_trampoline_hash = NULL;
	if (domain->special_static_fields) {
		g_hash_table_destroy (domain->special_static_fields);
		domain->special_static_fields = NULL;
	}
	DeleteCriticalSection (&domain->assemblies_lock);
	DeleteCriticalSection (&domain->lock);
	domain->setup = NULL;

	/* FIXME: anything else required ? */

	mono_gc_free_fixed (domain);

	if ((domain == mono_root_domain))
		mono_root_domain = NULL;
}

/**
 * mono_domain_get_id:
 *
 * Returns: the a domain for a specific domain id.
 */
MonoDomain * 
mono_domain_get_by_id (gint32 domainid) 
{
	MonoDomain * domain;

	mono_appdomains_lock ();
	if (domainid < appdomain_list_size)
		domain = appdomains_list [domainid];
	else
		domain = NULL;
	mono_appdomains_unlock ();

	return domain;
}

gint32
mono_domain_get_id (MonoDomain *domain)
{
	return domain->domain_id;
}

void 
mono_context_set (MonoAppContext * new_context)
{
	SET_APPCONTEXT (new_context);
}

MonoAppContext * 
mono_context_get (void)
{
	return GET_APPCONTEXT ();
}

MonoImage*
mono_get_corlib (void)
{
	return mono_defaults.corlib;
}

MonoClass*
mono_get_object_class (void)
{
	return mono_defaults.object_class;
}

MonoClass*
mono_get_byte_class (void)
{
	return mono_defaults.byte_class;
}

MonoClass*
mono_get_void_class (void)
{
	return mono_defaults.void_class;
}

MonoClass*
mono_get_boolean_class (void)
{
	return mono_defaults.boolean_class;
}

MonoClass*
mono_get_sbyte_class (void)
{
	return mono_defaults.sbyte_class;
}

MonoClass*
mono_get_int16_class (void)
{
	return mono_defaults.int16_class;
}

MonoClass*
mono_get_uint16_class (void)
{
	return mono_defaults.uint16_class;
}

MonoClass*
mono_get_int32_class (void)
{
	return mono_defaults.int32_class;
}

MonoClass*
mono_get_uint32_class (void)
{
	return mono_defaults.uint32_class;
}

MonoClass*
mono_get_intptr_class (void)
{
	return mono_defaults.int_class;
}

MonoClass*
mono_get_uintptr_class (void)
{
	return mono_defaults.uint_class;
}

MonoClass*
mono_get_int64_class (void)
{
	return mono_defaults.int64_class;
}

MonoClass*
mono_get_uint64_class (void)
{
	return mono_defaults.uint64_class;
}

MonoClass*
mono_get_single_class (void)
{
	return mono_defaults.single_class;
}

MonoClass*
mono_get_double_class (void)
{
	return mono_defaults.double_class;
}

MonoClass*
mono_get_char_class (void)
{
	return mono_defaults.char_class;
}

MonoClass*
mono_get_string_class (void)
{
	return mono_defaults.string_class;
}

MonoClass*
mono_get_enum_class (void)
{
	return mono_defaults.enum_class;
}

MonoClass*
mono_get_array_class (void)
{
	return mono_defaults.array_class;
}

MonoClass*
mono_get_thread_class (void)
{
	return mono_defaults.thread_class;
}

MonoClass*
mono_get_exception_class (void)
{
	return mono_defaults.exception_class;
}


static char* get_attribute_value (const gchar **attribute_names, 
					const gchar **attribute_values, 
					const char *att_name)
{
	int n;
	for (n=0; attribute_names[n] != NULL; n++) {
		if (strcmp (attribute_names[n], att_name) == 0)
			return g_strdup (attribute_values[n]);
	}
	return NULL;
}

static void start_element (GMarkupParseContext *context, 
                           const gchar         *element_name,
			   const gchar        **attribute_names,
			   const gchar        **attribute_values,
			   gpointer             user_data,
			   GError             **error)
{
	AppConfigInfo* app_config = (AppConfigInfo*) user_data;
	
	if (strcmp (element_name, "configuration") == 0) {
		app_config->configuration_count++;
		return;
	}
	if (strcmp (element_name, "startup") == 0) {
		app_config->startup_count++;
		return;
	}
	
	if (app_config->configuration_count != 1 || app_config->startup_count != 1)
		return;
	
	if (strcmp (element_name, "requiredRuntime") == 0) {
		app_config->required_runtime = get_attribute_value (attribute_names, attribute_values, "version");
	} else if (strcmp (element_name, "supportedRuntime") == 0) {
		char *version = get_attribute_value (attribute_names, attribute_values, "version");
		app_config->supported_runtimes = g_slist_append (app_config->supported_runtimes, version);
	}
}

static void end_element   (GMarkupParseContext *context,
                           const gchar         *element_name,
			   gpointer             user_data,
			   GError             **error)
{
	AppConfigInfo* app_config = (AppConfigInfo*) user_data;
	
	if (strcmp (element_name, "configuration") == 0) {
		app_config->configuration_count--;
	} else if (strcmp (element_name, "startup") == 0) {
		app_config->startup_count--;
	}
}

static const GMarkupParser 
mono_parser = {
	start_element,
	end_element,
	NULL,
	NULL,
	NULL
};

static AppConfigInfo *
app_config_parse (const char *filename)
{
	AppConfigInfo *app_config;
	GMarkupParseContext *context;
	char *text;
	gsize len;
	
	struct stat buf;
	if (stat (filename, &buf) != 0)
		return NULL;
	
	app_config = g_new0 (AppConfigInfo, 1);

	if (!g_file_get_contents (filename, &text, &len, NULL))
		return NULL;

	context = g_markup_parse_context_new (&mono_parser, 0, app_config, NULL);
	if (g_markup_parse_context_parse (context, text, len, NULL)) {
		g_markup_parse_context_end_parse (context, NULL);
	}
	g_markup_parse_context_free (context);
	g_free (text);
	return app_config;
}

static void 
app_config_free (AppConfigInfo* app_config)
{
	char *rt;
	GSList *list = app_config->supported_runtimes;
	while (list != NULL) {
		rt = (char*)list->data;
		g_free (rt);
		list = g_slist_next (list);
	}
	g_slist_free (app_config->supported_runtimes);
	g_free (app_config->required_runtime);
	g_free (app_config);
}


static const MonoRuntimeInfo*
get_runtime_by_version (const char *version)
{
	int n;
	int max = G_N_ELEMENTS (supported_runtimes);
	
	for (n=0; n<max; n++) {
		if (strcmp (version, supported_runtimes[n].runtime_version) == 0)
			return &supported_runtimes[n];
	}
	return NULL;
}

static void
get_runtimes_from_exe (const char *exe_file, const MonoRuntimeInfo** runtimes)
{
	AppConfigInfo* app_config;
	char *version;
	char *config_name;
	const MonoRuntimeInfo* runtime = NULL;
	MonoImage *image = NULL;
	
	config_name = g_strconcat (exe_file, ".config", NULL);
	app_config = app_config_parse (config_name);
	g_free (config_name);
	
	if (app_config != NULL) {
		/* Check supportedRuntime elements, if none is supported, fail.
		 * If there are no such elements, look for a requiredRuntime element.
		 */
		if (app_config->supported_runtimes != NULL) {
			int n = 0;
			GSList *list = app_config->supported_runtimes;
			while (list != NULL) {
				version = (char*) list->data;
				runtime = get_runtime_by_version (version);
				if (runtime != NULL)
					runtimes [n++] = runtime;
				list = g_slist_next (list);
			}
			runtimes [n] = NULL;
			app_config_free (app_config);
			return;
		}
		
		/* Check the requiredRuntime element. This is for 1.0 apps only. */
		if (app_config->required_runtime != NULL) {
			runtimes [0] = get_runtime_by_version (app_config->required_runtime);
			runtimes [1] = NULL;
			app_config_free (app_config);
			return;
		}
		app_config_free (app_config);
	}
	
	/* Look for a runtime with the exact version */
	image = mono_image_open (exe_file, NULL);
	if (image == NULL) {
		/* The image is wrong or the file was not found. In this case return
		 * a default runtime and leave to the initialization method the work of
		 * reporting the error.
		 */
		runtimes [0] = get_runtime_by_version (DEFAULT_RUNTIME_VERSION);
		runtimes [1] = NULL;
		return;
	}

	runtimes [0] = get_runtime_by_version (image->version);
	runtimes [1] = NULL;
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

gchar *
mono_debugger_check_runtime_version (const char *filename)
{
	const MonoRuntimeInfo* runtimes [G_N_ELEMENTS (supported_runtimes) + 1];
	const MonoRuntimeInfo *rinfo;

	get_runtimes_from_exe (filename, runtimes);
	rinfo = runtimes [0];

	if (!rinfo)
		return g_strdup_printf ("Cannot get runtime version from assembly `%s'", filename);

	if (rinfo != current_runtime)
		return g_strdup_printf ("The Mono Debugger is currently using the `%s' runtime, but "
					"the assembly `%s' requires version `%s'", current_runtime->runtime_version,
					filename, rinfo->runtime_version);

	return NULL;
}
