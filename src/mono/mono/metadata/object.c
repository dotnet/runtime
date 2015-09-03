/*
 * object.c: Object creation for the Mono runtime
 *
 * Author:
 *   Miguel de Icaza (miguel@ximian.com)
 *   Paolo Molaro (lupus@ximian.com)
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2011 Novell, Inc (http://www.novell.com)
 * Copyright 2001 Xamarin Inc (http://www.xamarin.com)
 */
#include <config.h>
#ifdef HAVE_ALLOCA_H
#include <alloca.h>
#endif
#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <mono/metadata/mono-endian.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/object.h>
#include <mono/metadata/gc-internal.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/domain-internals.h>
#include "mono/metadata/metadata-internals.h"
#include "mono/metadata/class-internals.h"
#include <mono/metadata/assembly.h>
#include <mono/metadata/marshal.h>
#include "mono/metadata/debug-helpers.h"
#include "mono/metadata/marshal.h"
#include <mono/metadata/threads.h>
#include <mono/metadata/threads-types.h>
#include <mono/metadata/environment.h>
#include "mono/metadata/profiler-private.h"
#include "mono/metadata/security-manager.h"
#include "mono/metadata/mono-debug-debugger.h"
#include <mono/metadata/gc-internal.h>
#include <mono/metadata/verify-internals.h>
#include <mono/utils/strenc.h>
#include <mono/utils/mono-counters.h>
#include <mono/utils/mono-error-internals.h>
#include <mono/utils/mono-memory-model.h>
#include <mono/utils/checked-build.h>
#include "cominterop.h"

static void
get_default_field_value (MonoDomain* domain, MonoClassField *field, void *value);

static MonoString*
mono_ldstr_metadata_sig (MonoDomain *domain, const char* sig);

static void
free_main_args (void);

static char *
mono_string_to_utf8_internal (MonoMemPool *mp, MonoImage *image, MonoString *s, gboolean ignore_error, MonoError *error);


#define ldstr_lock() mono_mutex_lock (&ldstr_section)
#define ldstr_unlock() mono_mutex_unlock (&ldstr_section)
static mono_mutex_t ldstr_section;

void
mono_runtime_object_init (MonoObject *this)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoMethod *method = NULL;
	MonoClass *klass = this->vtable->klass;

	method = mono_class_get_method_from_name (klass, ".ctor", 0);
	if (!method)
		g_error ("Could not lookup zero argument constructor for class %s", mono_type_get_full_name (klass));

	if (method->klass->valuetype)
		this = mono_object_unbox (this);
	mono_runtime_invoke (method, this, NULL, NULL);
}

/* The pseudo algorithm for type initialization from the spec
Note it doesn't say anything about domains - only threads.

2. If the type is initialized you are done.
2.1. If the type is not yet initialized, try to take an 
     initialization lock.  
2.2. If successful, record this thread as responsible for 
     initializing the type and proceed to step 2.3.
2.2.1. If not, see whether this thread or any thread 
     waiting for this thread to complete already holds the lock.
2.2.2. If so, return since blocking would create a deadlock.  This thread 
     will now see an incompletely initialized state for the type, 
     but no deadlock will arise.
2.2.3  If not, block until the type is initialized then return.
2.3 Initialize the parent type and then all interfaces implemented 
    by this type.
2.4 Execute the type initialization code for this type.
2.5 Mark the type as initialized, release the initialization lock, 
    awaken any threads waiting for this type to be initialized, 
    and return.

*/

typedef struct
{
	guint32 initializing_tid;
	guint32 waiting_count;
	gboolean done;
	mono_mutex_t initialization_section;
} TypeInitializationLock;

/* for locking access to type_initialization_hash and blocked_thread_hash */
static mono_mutex_t type_initialization_section;

static inline void
mono_type_initialization_lock (void)
{
	/* The critical sections protected by this lock in mono_runtime_class_init_full () can block */
	MONO_PREPARE_BLOCKING;
	mono_mutex_lock (&type_initialization_section);
	MONO_FINISH_BLOCKING;
}

static inline void
mono_type_initialization_unlock (void)
{
	mono_mutex_unlock (&type_initialization_section);
}

static void
mono_type_init_lock (TypeInitializationLock *lock)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	MONO_TRY_BLOCKING;
	mono_mutex_lock (&lock->initialization_section);
	MONO_FINISH_TRY_BLOCKING;
}

static void
mono_type_init_unlock (TypeInitializationLock *lock)
{
	mono_mutex_unlock (&lock->initialization_section);
}

/* from vtable to lock */
static GHashTable *type_initialization_hash;

/* from thread id to thread id being waited on */
static GHashTable *blocked_thread_hash;

/* Main thread */
static MonoThread *main_thread;

/* Functions supplied by the runtime */
static MonoRuntimeCallbacks callbacks;

/**
 * mono_thread_set_main:
 * @thread: thread to set as the main thread
 *
 * This function can be used to instruct the runtime to treat @thread
 * as the main thread, ie, the thread that would normally execute the Main()
 * method. This basically means that at the end of @thread, the runtime will
 * wait for the existing foreground threads to quit and other such details.
 */
void
mono_thread_set_main (MonoThread *thread)
{
	MONO_REQ_GC_UNSAFE_MODE;

	static gboolean registered = FALSE;

	if (!registered) {
		MONO_GC_REGISTER_ROOT_SINGLE (main_thread, MONO_ROOT_SOURCE_THREADING, "main thread object");
		registered = TRUE;
	}

	main_thread = thread;
}

MonoThread*
mono_thread_get_main (void)
{
	MONO_REQ_GC_UNSAFE_MODE;

	return main_thread;
}

void
mono_type_initialization_init (void)
{
	mono_mutex_init_recursive (&type_initialization_section);
	type_initialization_hash = g_hash_table_new (NULL, NULL);
	blocked_thread_hash = g_hash_table_new (NULL, NULL);
	mono_mutex_init_recursive (&ldstr_section);
}

void
mono_type_initialization_cleanup (void)
{
#if 0
	/* This is causing race conditions with
	 * mono_release_type_locks
	 */
	mono_mutex_destroy (&type_initialization_section);
	g_hash_table_destroy (type_initialization_hash);
	type_initialization_hash = NULL;
#endif
	mono_mutex_destroy (&ldstr_section);
	g_hash_table_destroy (blocked_thread_hash);
	blocked_thread_hash = NULL;

	free_main_args ();
}

/**
 * get_type_init_exception_for_vtable:
 *
 *   Return the stored type initialization exception for VTABLE.
 */
static MonoException*
get_type_init_exception_for_vtable (MonoVTable *vtable)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoDomain *domain = vtable->domain;
	MonoClass *klass = vtable->klass;
	MonoException *ex;
	gchar *full_name;

	if (!vtable->init_failed)
		g_error ("Trying to get the init exception for a non-failed vtable of class %s", mono_type_get_full_name (klass));
	
	/* 
	 * If the initializing thread was rudely aborted, the exception is not stored
	 * in the hash.
	 */
	ex = NULL;
	mono_domain_lock (domain);
	if (domain->type_init_exception_hash)
		ex = mono_g_hash_table_lookup (domain->type_init_exception_hash, klass);
	mono_domain_unlock (domain);

	if (!ex) {
		if (klass->name_space && *klass->name_space)
			full_name = g_strdup_printf ("%s.%s", klass->name_space, klass->name);
		else
			full_name = g_strdup (klass->name);
		ex = mono_get_exception_type_initialization (full_name, NULL);
		g_free (full_name);
	}

	return ex;
}
/*
 * mono_runtime_class_init:
 * @vtable: vtable that needs to be initialized
 *
 * This routine calls the class constructor for @vtable.
 */
void
mono_runtime_class_init (MonoVTable *vtable)
{
	MONO_REQ_GC_UNSAFE_MODE;

	mono_runtime_class_init_full (vtable, TRUE);
}

/*
 * mono_runtime_class_init_full:
 * @vtable that neeeds to be initialized
 * @raise_exception is TRUE, exceptions are raised intead of returned 
 * 
 */
MonoException *
mono_runtime_class_init_full (MonoVTable *vtable, gboolean raise_exception)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoException *exc;
	MonoException *exc_to_throw;
	MonoMethod *method = NULL;
	MonoClass *klass;
	gchar *full_name;
	MonoDomain *domain = vtable->domain;
	TypeInitializationLock *lock;
	guint32 tid;
	int do_initialization = 0;
	MonoDomain *last_domain = NULL;

	if (vtable->initialized)
		return NULL;

	exc = NULL;
	klass = vtable->klass;

	if (!klass->image->checked_module_cctor) {
		mono_image_check_for_module_cctor (klass->image);
		if (klass->image->has_module_cctor) {
			MonoError error;
			MonoClass *module_klass;
			MonoVTable *module_vtable;

			module_klass = mono_class_get_checked (klass->image, MONO_TOKEN_TYPE_DEF | 1, &error);
			if (!module_klass) {
				exc = mono_error_convert_to_exception (&error);
				if (raise_exception)
					mono_raise_exception (exc);
				return exc; 
			}
				
			module_vtable = mono_class_vtable_full (vtable->domain, module_klass, raise_exception);
			if (!module_vtable)
				return NULL;
			exc = mono_runtime_class_init_full (module_vtable, raise_exception);
			if (exc)
				return exc;
		}
	}
	method = mono_class_get_cctor (klass);
	if (!method) {
		vtable->initialized = 1;
		return NULL;
	}

	tid = GetCurrentThreadId ();

	mono_type_initialization_lock ();
	/* double check... */
	if (vtable->initialized) {
		mono_type_initialization_unlock ();
		return NULL;
	}
	if (vtable->init_failed) {
		mono_type_initialization_unlock ();

		/* The type initialization already failed once, rethrow the same exception */
		if (raise_exception)
			mono_raise_exception (get_type_init_exception_for_vtable (vtable));
		return get_type_init_exception_for_vtable (vtable);
	}
	lock = g_hash_table_lookup (type_initialization_hash, vtable);
	if (lock == NULL) {
		/* This thread will get to do the initialization */
		if (mono_domain_get () != domain) {
			/* Transfer into the target domain */
			last_domain = mono_domain_get ();
			if (!mono_domain_set (domain, FALSE)) {
				vtable->initialized = 1;
				mono_type_initialization_unlock ();
				if (raise_exception)
					mono_raise_exception (mono_get_exception_appdomain_unloaded ());
				return mono_get_exception_appdomain_unloaded ();
			}
		}
		lock = g_malloc (sizeof(TypeInitializationLock));
		mono_mutex_init_recursive (&lock->initialization_section);
		lock->initializing_tid = tid;
		lock->waiting_count = 1;
		lock->done = FALSE;
		/* grab the vtable lock while this thread still owns type_initialization_section */
		/* This is why type_initialization_lock needs to enter blocking mode */
		mono_type_init_lock (lock);
		g_hash_table_insert (type_initialization_hash, vtable, lock);
		do_initialization = 1;
	} else {
		gpointer blocked;
		TypeInitializationLock *pending_lock;

		if (lock->initializing_tid == tid || lock->done) {
			mono_type_initialization_unlock ();
			return NULL;
		}
		/* see if the thread doing the initialization is already blocked on this thread */
		blocked = GUINT_TO_POINTER (lock->initializing_tid);
		while ((pending_lock = (TypeInitializationLock*) g_hash_table_lookup (blocked_thread_hash, blocked))) {
			if (pending_lock->initializing_tid == tid) {
				if (!pending_lock->done) {
					mono_type_initialization_unlock ();
					return NULL;
				} else {
					/* the thread doing the initialization is blocked on this thread,
					   but on a lock that has already been freed. It just hasn't got
					   time to awake */
					break;
				}
			}
			blocked = GUINT_TO_POINTER (pending_lock->initializing_tid);
		}
		++lock->waiting_count;
		/* record the fact that we are waiting on the initializing thread */
		g_hash_table_insert (blocked_thread_hash, GUINT_TO_POINTER (tid), lock);
	}
	mono_type_initialization_unlock ();

	if (do_initialization) {
		mono_runtime_invoke (method, NULL, NULL, (MonoObject **) &exc);

		/* If the initialization failed, mark the class as unusable. */
		/* Avoid infinite loops */
		if (!(exc == NULL ||
			  (klass->image == mono_defaults.corlib &&
			   !strcmp (klass->name_space, "System") &&
			   !strcmp (klass->name, "TypeInitializationException")))) {
			vtable->init_failed = 1;

			if (klass->name_space && *klass->name_space)
				full_name = g_strdup_printf ("%s.%s", klass->name_space, klass->name);
			else
				full_name = g_strdup (klass->name);
			exc_to_throw = mono_get_exception_type_initialization (full_name, exc);
			g_free (full_name);

			/*
			 * Store the exception object so it could be thrown on subsequent
			 * accesses.
			 */
			mono_domain_lock (domain);
			if (!domain->type_init_exception_hash)
				domain->type_init_exception_hash = mono_g_hash_table_new_type (mono_aligned_addr_hash, NULL, MONO_HASH_VALUE_GC, MONO_ROOT_SOURCE_DOMAIN, "type initialization exceptions table");
			mono_g_hash_table_insert (domain->type_init_exception_hash, klass, exc_to_throw);
			mono_domain_unlock (domain);
		}

		if (last_domain)
			mono_domain_set (last_domain, TRUE);
		lock->done = TRUE;
		mono_type_init_unlock (lock);
	} else {
		/* this just blocks until the initializing thread is done */
		mono_type_init_lock (lock);
		mono_type_init_unlock (lock);
	}

	mono_type_initialization_lock ();
	if (lock->initializing_tid != tid)
		g_hash_table_remove (blocked_thread_hash, GUINT_TO_POINTER (tid));
	--lock->waiting_count;
	if (lock->waiting_count == 0) {
		mono_mutex_destroy (&lock->initialization_section);
		g_hash_table_remove (type_initialization_hash, vtable);
		g_free (lock);
	}
	mono_memory_barrier ();
	if (!vtable->init_failed)
		vtable->initialized = 1;
	mono_type_initialization_unlock ();

	if (vtable->init_failed) {
		/* Either we were the initializing thread or we waited for the initialization */
		if (raise_exception)
			mono_raise_exception (get_type_init_exception_for_vtable (vtable));
		return get_type_init_exception_for_vtable (vtable);
	}
	return NULL;
}

static
gboolean release_type_locks (gpointer key, gpointer value, gpointer user)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	MonoVTable *vtable = (MonoVTable*)key;

	TypeInitializationLock *lock = (TypeInitializationLock*) value;
	if (lock->initializing_tid == GPOINTER_TO_UINT (user) && !lock->done) {
		lock->done = TRUE;
		/* 
		 * Have to set this since it cannot be set by the normal code in 
		 * mono_runtime_class_init (). In this case, the exception object is not stored,
		 * and get_type_init_exception_for_class () needs to be aware of this.
		 */
		vtable->init_failed = 1;
		mono_type_init_unlock (lock);
		--lock->waiting_count;
		if (lock->waiting_count == 0) {
			mono_mutex_destroy (&lock->initialization_section);
			g_free (lock);
			return TRUE;
		}
	}
	return FALSE;
}

void
mono_release_type_locks (MonoInternalThread *thread)
{
	MONO_REQ_GC_UNSAFE_MODE;

	mono_type_initialization_lock ();
	g_hash_table_foreach_remove (type_initialization_hash, release_type_locks, (gpointer)(gsize)(thread->tid));
	mono_type_initialization_unlock ();
}

static gpointer
default_trampoline (MonoMethod *method)
{
	return method;
}

static gpointer
default_jump_trampoline (MonoDomain *domain, MonoMethod *method, gboolean add_sync_wrapper)
{
	g_assert_not_reached ();

	return NULL;
}

#ifndef DISABLE_REMOTING

static gpointer
default_remoting_trampoline (MonoDomain *domain, MonoMethod *method, MonoRemotingTarget target)
{
	g_error ("remoting not installed");
	return NULL;
}

static MonoRemotingTrampoline arch_create_remoting_trampoline = default_remoting_trampoline;
#endif

static gpointer
default_delegate_trampoline (MonoDomain *domain, MonoClass *klass)
{
	g_assert_not_reached ();
	return NULL;
}

static MonoTrampoline arch_create_jit_trampoline = default_trampoline;
static MonoJumpTrampoline arch_create_jump_trampoline = default_jump_trampoline;
static MonoDelegateTrampoline arch_create_delegate_trampoline = default_delegate_trampoline;
static MonoImtThunkBuilder imt_thunk_builder;
#if (MONO_IMT_SIZE > 32)
#error "MONO_IMT_SIZE cannot be larger than 32"
#endif

void
mono_install_callbacks (MonoRuntimeCallbacks *cbs)
{
	memcpy (&callbacks, cbs, sizeof (*cbs));
}

MonoRuntimeCallbacks*
mono_get_runtime_callbacks (void)
{
	return &callbacks;
}

void
mono_install_trampoline (MonoTrampoline func) 
{
	arch_create_jit_trampoline = func? func: default_trampoline;
}

void
mono_install_jump_trampoline (MonoJumpTrampoline func) 
{
	arch_create_jump_trampoline = func? func: default_jump_trampoline;
}

#ifndef DISABLE_REMOTING
void
mono_install_remoting_trampoline (MonoRemotingTrampoline func) 
{
	arch_create_remoting_trampoline = func? func: default_remoting_trampoline;
}
#endif

void
mono_install_delegate_trampoline (MonoDelegateTrampoline func) 
{
	arch_create_delegate_trampoline = func? func: default_delegate_trampoline;
}

void
mono_install_imt_thunk_builder (MonoImtThunkBuilder func) {
	imt_thunk_builder = func;
}

static MonoCompileFunc default_mono_compile_method = NULL;

/**
 * mono_install_compile_method:
 * @func: function to install
 *
 * This is a VM internal routine
 */
void        
mono_install_compile_method (MonoCompileFunc func)
{
	default_mono_compile_method = func;
}

/**
 * mono_compile_method:
 * @method: The method to compile.
 *
 * This JIT-compiles the method, and returns the pointer to the native code
 * produced.
 */
gpointer 
mono_compile_method (MonoMethod *method)
{
	MONO_REQ_GC_NEUTRAL_MODE

	if (!default_mono_compile_method) {
		g_error ("compile method called on uninitialized runtime");
		return NULL;
	}
	return default_mono_compile_method (method);
}

gpointer
mono_runtime_create_jump_trampoline (MonoDomain *domain, MonoMethod *method, gboolean add_sync_wrapper)
{
	MONO_REQ_GC_NEUTRAL_MODE

	return arch_create_jump_trampoline (domain, method, add_sync_wrapper);
}

gpointer
mono_runtime_create_delegate_trampoline (MonoClass *klass)
{
	MONO_REQ_GC_NEUTRAL_MODE

	return arch_create_delegate_trampoline (mono_domain_get (), klass);
}

static MonoFreeMethodFunc default_mono_free_method = NULL;

/**
 * mono_install_free_method:
 * @func: pointer to the MonoFreeMethodFunc used to release a method
 *
 * This is an internal VM routine, it is used for the engines to
 * register a handler to release the resources associated with a method.
 *
 * Methods are freed when no more references to the delegate that holds
 * them are left.
 */
void
mono_install_free_method (MonoFreeMethodFunc func)
{
	default_mono_free_method = func;
}

/**
 * mono_runtime_free_method:
 * @domain; domain where the method is hosted
 * @method: method to release
 *
 * This routine is invoked to free the resources associated with
 * a method that has been JIT compiled.  This is used to discard
 * methods that were used only temporarily (for example, used in marshalling)
 *
 */
void
mono_runtime_free_method (MonoDomain *domain, MonoMethod *method)
{
	MONO_REQ_GC_NEUTRAL_MODE

	if (default_mono_free_method != NULL)
		default_mono_free_method (domain, method);

	mono_method_clear_object (domain, method);

	mono_free_method (method);
}

/*
 * The vtables in the root appdomain are assumed to be reachable by other 
 * roots, and we don't use typed allocation in the other domains.
 */

/* The sync block is no longer a GC pointer */
#define GC_HEADER_BITMAP (0)

#define BITMAP_EL_SIZE (sizeof (gsize) * 8)

static gsize*
compute_class_bitmap (MonoClass *class, gsize *bitmap, int size, int offset, int *max_set, gboolean static_fields)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	MonoClassField *field;
	MonoClass *p;
	guint32 pos;
	int max_size;

	if (static_fields)
		max_size = mono_class_data_size (class) / sizeof (gpointer);
	else
		max_size = class->instance_size / sizeof (gpointer);
	if (max_size > size) {
		g_assert (offset <= 0);
		bitmap = g_malloc0 ((max_size + BITMAP_EL_SIZE - 1) / BITMAP_EL_SIZE * sizeof (gsize));
		size = max_size;
	}

#ifdef HAVE_SGEN_GC
	/*An Ephemeron cannot be marked by sgen*/
	if (!static_fields && class->image == mono_defaults.corlib && !strcmp ("Ephemeron", class->name)) {
		*max_set = 0;
		memset (bitmap, 0, size / 8);
		return bitmap;
	}
#endif

	for (p = class; p != NULL; p = p->parent) {
		gpointer iter = NULL;
		while ((field = mono_class_get_fields (p, &iter))) {
			MonoType *type;

			if (static_fields) {
				if (!(field->type->attrs & (FIELD_ATTRIBUTE_STATIC | FIELD_ATTRIBUTE_HAS_FIELD_RVA)))
					continue;
				if (field->type->attrs & FIELD_ATTRIBUTE_LITERAL)
					continue;
			} else {
				if (field->type->attrs & (FIELD_ATTRIBUTE_STATIC | FIELD_ATTRIBUTE_HAS_FIELD_RVA))
					continue;
			}
			/* FIXME: should not happen, flag as type load error */
			if (field->type->byref)
				break;

			// Special static fields do not have a domain-level static slot
			if (static_fields && mono_class_field_is_special_static (field))
				continue;

			pos = field->offset / sizeof (gpointer);
			pos += offset;

			type = mono_type_get_underlying_type (field->type);
			switch (type->type) {
			case MONO_TYPE_I:
			case MONO_TYPE_PTR:
			case MONO_TYPE_FNPTR:
				break;
			/* only UIntPtr is allowed to be GC-tracked and only in mscorlib */
			case MONO_TYPE_U:
#ifdef HAVE_SGEN_GC
				break;
#else
				if (class->image != mono_defaults.corlib)
					break;
#endif
			case MONO_TYPE_STRING:
			case MONO_TYPE_SZARRAY:
			case MONO_TYPE_CLASS:
			case MONO_TYPE_OBJECT:
			case MONO_TYPE_ARRAY:
				g_assert ((field->offset % sizeof(gpointer)) == 0);

				g_assert (pos < size || pos <= max_size);
				bitmap [pos / BITMAP_EL_SIZE] |= ((gsize)1) << (pos % BITMAP_EL_SIZE);
				*max_set = MAX (*max_set, pos);
				break;
			case MONO_TYPE_GENERICINST:
				if (!mono_type_generic_inst_is_valuetype (type)) {
					g_assert ((field->offset % sizeof(gpointer)) == 0);

					bitmap [pos / BITMAP_EL_SIZE] |= ((gsize)1) << (pos % BITMAP_EL_SIZE);
					*max_set = MAX (*max_set, pos);
					break;
				} else {
					/* fall through */
				}
			case MONO_TYPE_VALUETYPE: {
				MonoClass *fclass = mono_class_from_mono_type (field->type);
				if (fclass->has_references) {
					/* remove the object header */
					compute_class_bitmap (fclass, bitmap, size, pos - (sizeof (MonoObject) / sizeof (gpointer)), max_set, FALSE);
				}
				break;
			}
			case MONO_TYPE_I1:
			case MONO_TYPE_U1:
			case MONO_TYPE_I2:
			case MONO_TYPE_U2:
			case MONO_TYPE_I4:
			case MONO_TYPE_U4:
			case MONO_TYPE_I8:
			case MONO_TYPE_U8:
			case MONO_TYPE_R4:
			case MONO_TYPE_R8:
			case MONO_TYPE_BOOLEAN:
			case MONO_TYPE_CHAR:
				break;
			default:
				g_error ("compute_class_bitmap: Invalid type %x for field %s:%s\n", type->type, mono_type_get_full_name (field->parent), field->name);
				break;
			}
		}
		if (static_fields)
			break;
	}
	return bitmap;
}

/**
 * mono_class_compute_bitmap:
 *
 * Mono internal function to compute a bitmap of reference fields in a class.
 */
gsize*
mono_class_compute_bitmap (MonoClass *class, gsize *bitmap, int size, int offset, int *max_set, gboolean static_fields)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	return compute_class_bitmap (class, bitmap, size, offset, max_set, static_fields);
}

#if 0
/* 
 * similar to the above, but sets the bits in the bitmap for any non-ref field
 * and ignores static fields
 */
static gsize*
compute_class_non_ref_bitmap (MonoClass *class, gsize *bitmap, int size, int offset)
{
	MonoClassField *field;
	MonoClass *p;
	guint32 pos, pos2;
	int max_size;

	max_size = class->instance_size / sizeof (gpointer);
	if (max_size >= size) {
		bitmap = g_malloc0 (sizeof (gsize) * ((max_size) + 1));
	}

	for (p = class; p != NULL; p = p->parent) {
		gpointer iter = NULL;
		while ((field = mono_class_get_fields (p, &iter))) {
			MonoType *type;

			if (field->type->attrs & (FIELD_ATTRIBUTE_STATIC | FIELD_ATTRIBUTE_HAS_FIELD_RVA))
				continue;
			/* FIXME: should not happen, flag as type load error */
			if (field->type->byref)
				break;

			pos = field->offset / sizeof (gpointer);
			pos += offset;

			type = mono_type_get_underlying_type (field->type);
			switch (type->type) {
#if SIZEOF_VOID_P == 8
			case MONO_TYPE_I:
			case MONO_TYPE_U:
			case MONO_TYPE_PTR:
			case MONO_TYPE_FNPTR:
#endif
			case MONO_TYPE_I8:
			case MONO_TYPE_U8:
			case MONO_TYPE_R8:
				if ((((field->offset + 7) / sizeof (gpointer)) + offset) != pos) {
					pos2 = ((field->offset + 7) / sizeof (gpointer)) + offset;
					bitmap [pos2 / BITMAP_EL_SIZE] |= ((gsize)1) << (pos2 % BITMAP_EL_SIZE);
				}
				/* fall through */
#if SIZEOF_VOID_P == 4
			case MONO_TYPE_I:
			case MONO_TYPE_U:
			case MONO_TYPE_PTR:
			case MONO_TYPE_FNPTR:
#endif
			case MONO_TYPE_I4:
			case MONO_TYPE_U4:
			case MONO_TYPE_R4:
				if ((((field->offset + 3) / sizeof (gpointer)) + offset) != pos) {
					pos2 = ((field->offset + 3) / sizeof (gpointer)) + offset;
					bitmap [pos2 / BITMAP_EL_SIZE] |= ((gsize)1) << (pos2 % BITMAP_EL_SIZE);
				}
				/* fall through */
			case MONO_TYPE_CHAR:
			case MONO_TYPE_I2:
			case MONO_TYPE_U2:
				if ((((field->offset + 1) / sizeof (gpointer)) + offset) != pos) {
					pos2 = ((field->offset + 1) / sizeof (gpointer)) + offset;
					bitmap [pos2 / BITMAP_EL_SIZE] |= ((gsize)1) << (pos2 % BITMAP_EL_SIZE);
				}
				/* fall through */
			case MONO_TYPE_BOOLEAN:
			case MONO_TYPE_I1:
			case MONO_TYPE_U1:
				bitmap [pos / BITMAP_EL_SIZE] |= ((gsize)1) << (pos % BITMAP_EL_SIZE);
				break;
			case MONO_TYPE_STRING:
			case MONO_TYPE_SZARRAY:
			case MONO_TYPE_CLASS:
			case MONO_TYPE_OBJECT:
			case MONO_TYPE_ARRAY:
				break;
			case MONO_TYPE_GENERICINST:
				if (!mono_type_generic_inst_is_valuetype (type)) {
					break;
				} else {
					/* fall through */
				}
			case MONO_TYPE_VALUETYPE: {
				MonoClass *fclass = mono_class_from_mono_type (field->type);
				/* remove the object header */
				compute_class_non_ref_bitmap (fclass, bitmap, size, pos - (sizeof (MonoObject) / sizeof (gpointer)));
				break;
			}
			default:
				g_assert_not_reached ();
				break;
			}
		}
	}
	return bitmap;
}

/**
 * mono_class_insecure_overlapping:
 * check if a class with explicit layout has references and non-references
 * fields overlapping.
 *
 * Returns: TRUE if it is insecure to load the type.
 */
gboolean
mono_class_insecure_overlapping (MonoClass *klass)
{
	int max_set = 0;
	gsize *bitmap;
	gsize default_bitmap [4] = {0};
	gsize *nrbitmap;
	gsize default_nrbitmap [4] = {0};
	int i, insecure = FALSE;
		return FALSE;

	bitmap = compute_class_bitmap (klass, default_bitmap, sizeof (default_bitmap) * 8, 0, &max_set, FALSE);
	nrbitmap = compute_class_non_ref_bitmap (klass, default_nrbitmap, sizeof (default_nrbitmap) * 8, 0);

	for (i = 0; i <= max_set; i += sizeof (bitmap [0]) * 8) {
		int idx = i % (sizeof (bitmap [0]) * 8);
		if (bitmap [idx] & nrbitmap [idx]) {
			insecure = TRUE;
			break;
		}
	}
	if (bitmap != default_bitmap)
		g_free (bitmap);
	if (nrbitmap != default_nrbitmap)
		g_free (nrbitmap);
	if (insecure) {
		g_print ("class %s.%s in assembly %s has overlapping references\n", klass->name_space, klass->name, klass->image->name);
		return FALSE;
	}
	return insecure;
}
#endif

MonoString*
mono_string_alloc (int length)
{
	MONO_REQ_GC_UNSAFE_MODE;
	return mono_string_new_size (mono_domain_get (), length);
}

void
mono_class_compute_gc_descriptor (MonoClass *class)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	int max_set = 0;
	gsize *bitmap;
	gsize default_bitmap [4] = {0};
	static gboolean gcj_inited = FALSE;

	if (!gcj_inited) {
		mono_loader_lock ();

		mono_register_jit_icall (mono_object_new_fast, "mono_object_new_fast", mono_create_icall_signature ("object ptr"), FALSE);
		mono_register_jit_icall (mono_string_alloc, "mono_string_alloc", mono_create_icall_signature ("object int"), FALSE);

		gcj_inited = TRUE;
		mono_loader_unlock ();
	}

	if (!class->inited)
		mono_class_init (class);

	if (class->gc_descr_inited)
		return;

	class->gc_descr_inited = TRUE;
	class->gc_descr = MONO_GC_DESCRIPTOR_NULL;

	bitmap = default_bitmap;
	if (class == mono_defaults.string_class) {
		class->gc_descr = mono_gc_make_descr_for_string (bitmap, 2);
	} else if (class->rank) {
		mono_class_compute_gc_descriptor (class->element_class);
		if (MONO_TYPE_IS_REFERENCE (&class->element_class->byval_arg)) {
			gsize abm = 1;
			class->gc_descr = mono_gc_make_descr_for_array (class->byval_arg.type == MONO_TYPE_SZARRAY, &abm, 1, sizeof (gpointer));
			/*printf ("new array descriptor: 0x%x for %s.%s\n", class->gc_descr,
				class->name_space, class->name);*/
		} else {
			/* remove the object header */
			bitmap = compute_class_bitmap (class->element_class, default_bitmap, sizeof (default_bitmap) * 8, - (int)(sizeof (MonoObject) / sizeof (gpointer)), &max_set, FALSE);
			class->gc_descr = mono_gc_make_descr_for_array (class->byval_arg.type == MONO_TYPE_SZARRAY, bitmap, mono_array_element_size (class) / sizeof (gpointer), mono_array_element_size (class));
			/*printf ("new vt array descriptor: 0x%x for %s.%s\n", class->gc_descr,
				class->name_space, class->name);*/
			if (bitmap != default_bitmap)
				g_free (bitmap);
		}
	} else {
		/*static int count = 0;
		if (count++ > 58)
			return;*/
		bitmap = compute_class_bitmap (class, default_bitmap, sizeof (default_bitmap) * 8, 0, &max_set, FALSE);
		class->gc_descr = mono_gc_make_descr_for_object (bitmap, max_set + 1, class->instance_size);
		/*
		if (class->gc_descr == MONO_GC_DESCRIPTOR_NULL)
			g_print ("disabling typed alloc (%d) for %s.%s\n", max_set, class->name_space, class->name);
		*/
		/*printf ("new descriptor: %p 0x%x for %s.%s\n", class->gc_descr, bitmap [0], class->name_space, class->name);*/
		if (bitmap != default_bitmap)
			g_free (bitmap);
	}
}

/**
 * field_is_special_static:
 * @fklass: The MonoClass to look up.
 * @field: The MonoClassField describing the field.
 *
 * Returns: SPECIAL_STATIC_THREAD if the field is thread static, SPECIAL_STATIC_CONTEXT if it is context static,
 * SPECIAL_STATIC_NONE otherwise.
 */
static gint32
field_is_special_static (MonoClass *fklass, MonoClassField *field)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	MonoCustomAttrInfo *ainfo;
	int i;
	ainfo = mono_custom_attrs_from_field (fklass, field);
	if (!ainfo)
		return FALSE;
	for (i = 0; i < ainfo->num_attrs; ++i) {
		MonoClass *klass = ainfo->attrs [i].ctor->klass;
		if (klass->image == mono_defaults.corlib) {
			if (strcmp (klass->name, "ThreadStaticAttribute") == 0) {
				mono_custom_attrs_free (ainfo);
				return SPECIAL_STATIC_THREAD;
			}
			else if (strcmp (klass->name, "ContextStaticAttribute") == 0) {
				mono_custom_attrs_free (ainfo);
				return SPECIAL_STATIC_CONTEXT;
			}
		}
	}
	mono_custom_attrs_free (ainfo);
	return SPECIAL_STATIC_NONE;
}

#define rot(x,k) (((x)<<(k)) | ((x)>>(32-(k))))
#define mix(a,b,c) { \
	a -= c;  a ^= rot(c, 4);  c += b; \
	b -= a;  b ^= rot(a, 6);  a += c; \
	c -= b;  c ^= rot(b, 8);  b += a; \
	a -= c;  a ^= rot(c,16);  c += b; \
	b -= a;  b ^= rot(a,19);  a += c; \
	c -= b;  c ^= rot(b, 4);  b += a; \
}
#define final(a,b,c) { \
	c ^= b; c -= rot(b,14); \
	a ^= c; a -= rot(c,11); \
	b ^= a; b -= rot(a,25); \
	c ^= b; c -= rot(b,16); \
	a ^= c; a -= rot(c,4);  \
	b ^= a; b -= rot(a,14); \
	c ^= b; c -= rot(b,24); \
}

/*
 * mono_method_get_imt_slot:
 *
 *   The IMT slot is embedded into AOTed code, so this must return the same value
 * for the same method across all executions. This means:
 * - pointers shouldn't be used as hash values.
 * - mono_metadata_str_hash () should be used for hashing strings.
 */
guint32
mono_method_get_imt_slot (MonoMethod *method)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	MonoMethodSignature *sig;
	int hashes_count;
	guint32 *hashes_start, *hashes;
	guint32 a, b, c;
	int i;

	/* This can be used to stress tests the collision code */
	//return 0;

	/*
	 * We do this to simplify generic sharing.  It will hurt
	 * performance in cases where a class implements two different
	 * instantiations of the same generic interface.
	 * The code in build_imt_slots () depends on this.
	 */
	if (method->is_inflated)
		method = ((MonoMethodInflated*)method)->declaring;

	sig = mono_method_signature (method);
	hashes_count = sig->param_count + 4;
	hashes_start = malloc (hashes_count * sizeof (guint32));
	hashes = hashes_start;

	if (! MONO_CLASS_IS_INTERFACE (method->klass)) {
		g_error ("mono_method_get_imt_slot: %s.%s.%s is not an interface MonoMethod",
				method->klass->name_space, method->klass->name, method->name);
	}
	
	/* Initialize hashes */
	hashes [0] = mono_metadata_str_hash (method->klass->name);
	hashes [1] = mono_metadata_str_hash (method->klass->name_space);
	hashes [2] = mono_metadata_str_hash (method->name);
	hashes [3] = mono_metadata_type_hash (sig->ret);
	for (i = 0; i < sig->param_count; i++) {
		hashes [4 + i] = mono_metadata_type_hash (sig->params [i]);
	}

	/* Setup internal state */
	a = b = c = 0xdeadbeef + (((guint32)hashes_count)<<2);

	/* Handle most of the hashes */
	while (hashes_count > 3) {
		a += hashes [0];
		b += hashes [1];
		c += hashes [2];
		mix (a,b,c);
		hashes_count -= 3;
		hashes += 3;
	}

	/* Handle the last 3 hashes (all the case statements fall through) */
	switch (hashes_count) { 
	case 3 : c += hashes [2];
	case 2 : b += hashes [1];
	case 1 : a += hashes [0];
		final (a,b,c);
	case 0: /* nothing left to add */
		break;
	}
	
	free (hashes_start);
	/* Report the result */
	return c % MONO_IMT_SIZE;
}
#undef rot
#undef mix
#undef final

#define DEBUG_IMT 0

static void
add_imt_builder_entry (MonoImtBuilderEntry **imt_builder, MonoMethod *method, guint32 *imt_collisions_bitmap, int vtable_slot, int slot_num) {
	MONO_REQ_GC_NEUTRAL_MODE;

	guint32 imt_slot = mono_method_get_imt_slot (method);
	MonoImtBuilderEntry *entry;

	if (slot_num >= 0 && imt_slot != slot_num) {
		/* we build just a single imt slot and this is not it */
		return;
	}

	entry = g_malloc0 (sizeof (MonoImtBuilderEntry));
	entry->key = method;
	entry->value.vtable_slot = vtable_slot;
	entry->next = imt_builder [imt_slot];
	if (imt_builder [imt_slot] != NULL) {
		entry->children = imt_builder [imt_slot]->children + 1;
		if (entry->children == 1) {
			mono_stats.imt_slots_with_collisions++;
			*imt_collisions_bitmap |= (1 << imt_slot);
		}
	} else {
		entry->children = 0;
		mono_stats.imt_used_slots++;
	}
	imt_builder [imt_slot] = entry;
#if DEBUG_IMT
	{
	char *method_name = mono_method_full_name (method, TRUE);
	printf ("Added IMT slot for method (%p) %s: imt_slot = %d, vtable_slot = %d, colliding with other %d entries\n",
			method, method_name, imt_slot, vtable_slot, entry->children);
	g_free (method_name);
	}
#endif
}

#if DEBUG_IMT
static void
print_imt_entry (const char* message, MonoImtBuilderEntry *e, int num) {
	if (e != NULL) {
		MonoMethod *method = e->key;
		printf ("  * %s [%d]: (%p) '%s.%s.%s'\n",
				message,
				num,
				method,
				method->klass->name_space,
				method->klass->name,
				method->name);
	} else {
		printf ("  * %s: NULL\n", message);
	}
}
#endif

static int
compare_imt_builder_entries (const void *p1, const void *p2) {
	MonoImtBuilderEntry *e1 = *(MonoImtBuilderEntry**) p1;
	MonoImtBuilderEntry *e2 = *(MonoImtBuilderEntry**) p2;
	
	return (e1->key < e2->key) ? -1 : ((e1->key > e2->key) ? 1 : 0);
}

static int
imt_emit_ir (MonoImtBuilderEntry **sorted_array, int start, int end, GPtrArray *out_array)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	int count = end - start;
	int chunk_start = out_array->len;
	if (count < 4) {
		int i;
		for (i = start; i < end; ++i) {
			MonoIMTCheckItem *item = g_new0 (MonoIMTCheckItem, 1);
			item->key = sorted_array [i]->key;
			item->value = sorted_array [i]->value;
			item->has_target_code = sorted_array [i]->has_target_code;
			item->is_equals = TRUE;
			if (i < end - 1)
				item->check_target_idx = out_array->len + 1;
			else
				item->check_target_idx = 0;
			g_ptr_array_add (out_array, item);
		}
	} else {
		int middle = start + count / 2;
		MonoIMTCheckItem *item = g_new0 (MonoIMTCheckItem, 1);

		item->key = sorted_array [middle]->key;
		item->is_equals = FALSE;
		g_ptr_array_add (out_array, item);
		imt_emit_ir (sorted_array, start, middle, out_array);
		item->check_target_idx = imt_emit_ir (sorted_array, middle, end, out_array);
	}
	return chunk_start;
}

static GPtrArray*
imt_sort_slot_entries (MonoImtBuilderEntry *entries) {
	MONO_REQ_GC_NEUTRAL_MODE;

	int number_of_entries = entries->children + 1;
	MonoImtBuilderEntry **sorted_array = malloc (sizeof (MonoImtBuilderEntry*) * number_of_entries);
	GPtrArray *result = g_ptr_array_new ();
	MonoImtBuilderEntry *current_entry;
	int i;
	
	for (current_entry = entries, i = 0; current_entry != NULL; current_entry = current_entry->next, i++) {
		sorted_array [i] = current_entry;
	}
	qsort (sorted_array, number_of_entries, sizeof (MonoImtBuilderEntry*), compare_imt_builder_entries);

	/*for (i = 0; i < number_of_entries; i++) {
		print_imt_entry (" sorted array:", sorted_array [i], i);
	}*/

	imt_emit_ir (sorted_array, 0, number_of_entries, result);

	free (sorted_array);
	return result;
}

static gpointer
initialize_imt_slot (MonoVTable *vtable, MonoDomain *domain, MonoImtBuilderEntry *imt_builder_entry, gpointer fail_tramp)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	if (imt_builder_entry != NULL) {
		if (imt_builder_entry->children == 0 && !fail_tramp) {
			/* No collision, return the vtable slot contents */
			return vtable->vtable [imt_builder_entry->value.vtable_slot];
		} else {
			/* Collision, build the thunk */
			GPtrArray *imt_ir = imt_sort_slot_entries (imt_builder_entry);
			gpointer result;
			int i;
			result = imt_thunk_builder (vtable, domain,
				(MonoIMTCheckItem**)imt_ir->pdata, imt_ir->len, fail_tramp);
			for (i = 0; i < imt_ir->len; ++i)
				g_free (g_ptr_array_index (imt_ir, i));
			g_ptr_array_free (imt_ir, TRUE);
			return result;
		}
	} else {
		if (fail_tramp)
			return fail_tramp;
		else
			/* Empty slot */
			return NULL;
	}
}

static MonoImtBuilderEntry*
get_generic_virtual_entries (MonoDomain *domain, gpointer *vtable_slot);

/*
 * LOCKING: requires the loader and domain locks.
 *
*/
static void
build_imt_slots (MonoClass *klass, MonoVTable *vt, MonoDomain *domain, gpointer* imt, GSList *extra_interfaces, int slot_num)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	int i;
	GSList *list_item;
	guint32 imt_collisions_bitmap = 0;
	MonoImtBuilderEntry **imt_builder = calloc (MONO_IMT_SIZE, sizeof (MonoImtBuilderEntry*));
	int method_count = 0;
	gboolean record_method_count_for_max_collisions = FALSE;
	gboolean has_generic_virtual = FALSE, has_variant_iface = FALSE;

#if DEBUG_IMT
	printf ("Building IMT for class %s.%s slot %d\n", klass->name_space, klass->name, slot_num);
#endif
	for (i = 0; i < klass->interface_offsets_count; ++i) {
		MonoClass *iface = klass->interfaces_packed [i];
		int interface_offset = klass->interface_offsets_packed [i];
		int method_slot_in_interface, vt_slot;

		if (mono_class_has_variant_generic_params (iface))
			has_variant_iface = TRUE;

		mono_class_setup_methods (iface);
		vt_slot = interface_offset;
		for (method_slot_in_interface = 0; method_slot_in_interface < iface->method.count; method_slot_in_interface++) {
			MonoMethod *method;

			if (slot_num >= 0 && iface->is_inflated) {
				/*
				 * The imt slot of the method is the same as for its declaring method,
				 * see the comment in mono_method_get_imt_slot (), so we can
				 * avoid inflating methods which will be discarded by 
				 * add_imt_builder_entry anyway.
				 */
				method = mono_class_get_method_by_index (iface->generic_class->container_class, method_slot_in_interface);
				if (mono_method_get_imt_slot (method) != slot_num) {
					vt_slot ++;
					continue;
				}
			}
			method = mono_class_get_method_by_index (iface, method_slot_in_interface);
			if (method->is_generic) {
				has_generic_virtual = TRUE;
				vt_slot ++;
				continue;
			}

			if (!(method->flags & METHOD_ATTRIBUTE_STATIC)) {
				add_imt_builder_entry (imt_builder, method, &imt_collisions_bitmap, vt_slot, slot_num);
				vt_slot ++;
			}
		}
	}
	if (extra_interfaces) {
		int interface_offset = klass->vtable_size;

		for (list_item = extra_interfaces; list_item != NULL; list_item=list_item->next) {
			MonoClass* iface = list_item->data;
			int method_slot_in_interface;
			for (method_slot_in_interface = 0; method_slot_in_interface < iface->method.count; method_slot_in_interface++) {
				MonoMethod *method = mono_class_get_method_by_index (iface, method_slot_in_interface);

				if (method->is_generic)
					has_generic_virtual = TRUE;
				add_imt_builder_entry (imt_builder, method, &imt_collisions_bitmap, interface_offset + method_slot_in_interface, slot_num);
			}
			interface_offset += iface->method.count;
		}
	}
	for (i = 0; i < MONO_IMT_SIZE; ++i) {
		/* overwrite the imt slot only if we're building all the entries or if 
		 * we're building this specific one
		 */
		if (slot_num < 0 || i == slot_num) {
			MonoImtBuilderEntry *entries = get_generic_virtual_entries (domain, &imt [i]);

			if (entries) {
				if (imt_builder [i]) {
					MonoImtBuilderEntry *entry;

					/* Link entries with imt_builder [i] */
					for (entry = entries; entry->next; entry = entry->next) {
#if DEBUG_IMT
						MonoMethod *method = (MonoMethod*)entry->key;
						char *method_name = mono_method_full_name (method, TRUE);
						printf ("Added extra entry for method (%p) %s: imt_slot = %d\n", method, method_name, i);
						g_free (method_name);
#endif
					}
					entry->next = imt_builder [i];
					entries->children += imt_builder [i]->children + 1;
				}
				imt_builder [i] = entries;
			}

			if (has_generic_virtual || has_variant_iface) {
				/*
				 * There might be collisions later when the the thunk is expanded.
				 */
				imt_collisions_bitmap |= (1 << i);

				/* 
				 * The IMT thunk might be called with an instance of one of the 
				 * generic virtual methods, so has to fallback to the IMT trampoline.
				 */
				imt [i] = initialize_imt_slot (vt, domain, imt_builder [i], callbacks.get_imt_trampoline (i));
			} else {
				imt [i] = initialize_imt_slot (vt, domain, imt_builder [i], NULL);
			}
#if DEBUG_IMT
			printf ("initialize_imt_slot[%d]: %p methods %d\n", i, imt [i], imt_builder [i]->children + 1);
#endif
		}

		if (imt_builder [i] != NULL) {
			int methods_in_slot = imt_builder [i]->children + 1;
			if (methods_in_slot > mono_stats.imt_max_collisions_in_slot) {
				mono_stats.imt_max_collisions_in_slot = methods_in_slot;
				record_method_count_for_max_collisions = TRUE;
			}
			method_count += methods_in_slot;
		}
	}
	
	mono_stats.imt_number_of_methods += method_count;
	if (record_method_count_for_max_collisions) {
		mono_stats.imt_method_count_when_max_collisions = method_count;
	}
	
	for (i = 0; i < MONO_IMT_SIZE; i++) {
		MonoImtBuilderEntry* entry = imt_builder [i];
		while (entry != NULL) {
			MonoImtBuilderEntry* next = entry->next;
			g_free (entry);
			entry = next;
		}
	}
	free (imt_builder);
	/* we OR the bitmap since we may build just a single imt slot at a time */
	vt->imt_collisions_bitmap |= imt_collisions_bitmap;
}

static void
build_imt (MonoClass *klass, MonoVTable *vt, MonoDomain *domain, gpointer* imt, GSList *extra_interfaces) {
	MONO_REQ_GC_NEUTRAL_MODE;

	build_imt_slots (klass, vt, domain, imt, extra_interfaces, -1);
}

/**
 * mono_vtable_build_imt_slot:
 * @vtable: virtual object table struct
 * @imt_slot: slot in the IMT table
 *
 * Fill the given @imt_slot in the IMT table of @vtable with
 * a trampoline or a thunk for the case of collisions.
 * This is part of the internal mono API.
 *
 * LOCKING: Take the domain lock.
 */
void
mono_vtable_build_imt_slot (MonoVTable* vtable, int imt_slot)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	gpointer *imt = (gpointer*)vtable;
	imt -= MONO_IMT_SIZE;
	g_assert (imt_slot >= 0 && imt_slot < MONO_IMT_SIZE);

	/* no support for extra interfaces: the proxy objects will need
	 * to build the complete IMT
	 * Update and heck needs to ahppen inside the proper domain lock, as all
	 * the changes made to a MonoVTable.
	 */
	mono_loader_lock (); /*FIXME build_imt_slots requires the loader lock.*/
	mono_domain_lock (vtable->domain);
	/* we change the slot only if it wasn't changed from the generic imt trampoline already */
	if (imt [imt_slot] == callbacks.get_imt_trampoline (imt_slot))
		build_imt_slots (vtable->klass, vtable, vtable->domain, imt, NULL, imt_slot);
	mono_domain_unlock (vtable->domain);
	mono_loader_unlock ();
}


/*
 * The first two free list entries both belong to the wait list: The
 * first entry is the pointer to the head of the list and the second
 * entry points to the last element.  That way appending and removing
 * the first element are both O(1) operations.
 */
#ifdef MONO_SMALL_CONFIG
#define NUM_FREE_LISTS		6
#else
#define NUM_FREE_LISTS		12
#endif
#define FIRST_FREE_LIST_SIZE	64
#define MAX_WAIT_LENGTH 	50
#define THUNK_THRESHOLD		10

/*
 * LOCKING: The domain lock must be held.
 */
static void
init_thunk_free_lists (MonoDomain *domain)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	if (domain->thunk_free_lists)
		return;
	domain->thunk_free_lists = mono_domain_alloc0 (domain, sizeof (gpointer) * NUM_FREE_LISTS);
}

static int
list_index_for_size (int item_size)
{
	int i = 2;
	int size = FIRST_FREE_LIST_SIZE;

	while (item_size > size && i < NUM_FREE_LISTS - 1) {
		i++;
		size <<= 1;
	}

	return i;
}

/**
 * mono_method_alloc_generic_virtual_thunk:
 * @domain: a domain
 * @size: size in bytes
 *
 * Allocs size bytes to be used for the code of a generic virtual
 * thunk.  It's either allocated from the domain's code manager or
 * reused from a previously invalidated piece.
 *
 * LOCKING: The domain lock must be held.
 */
gpointer
mono_method_alloc_generic_virtual_thunk (MonoDomain *domain, int size)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	static gboolean inited = FALSE;
	static int generic_virtual_thunks_size = 0;

	guint32 *p;
	int i;
	MonoThunkFreeList **l;

	init_thunk_free_lists (domain);

	size += sizeof (guint32);
	if (size < sizeof (MonoThunkFreeList))
		size = sizeof (MonoThunkFreeList);

	i = list_index_for_size (size);
	for (l = &domain->thunk_free_lists [i]; *l; l = &(*l)->next) {
		if ((*l)->size >= size) {
			MonoThunkFreeList *item = *l;
			*l = item->next;
			return ((guint32*)item) + 1;
		}
	}

	/* no suitable item found - search lists of larger sizes */
	while (++i < NUM_FREE_LISTS) {
		MonoThunkFreeList *item = domain->thunk_free_lists [i];
		if (!item)
			continue;
		g_assert (item->size > size);
		domain->thunk_free_lists [i] = item->next;
		return ((guint32*)item) + 1;
	}

	/* still nothing found - allocate it */
	if (!inited) {
		mono_counters_register ("Generic virtual thunk bytes",
				MONO_COUNTER_GENERICS | MONO_COUNTER_INT, &generic_virtual_thunks_size);
		inited = TRUE;
	}
	generic_virtual_thunks_size += size;

	p = mono_domain_code_reserve (domain, size);
	*p = size;

	mono_domain_lock (domain);
	if (!domain->generic_virtual_thunks)
		domain->generic_virtual_thunks = g_hash_table_new (NULL, NULL);
	g_hash_table_insert (domain->generic_virtual_thunks, p, p);
	mono_domain_unlock (domain);

	return p + 1;
}

/*
 * LOCKING: The domain lock must be held.
 */
static void
invalidate_generic_virtual_thunk (MonoDomain *domain, gpointer code)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	guint32 *p = code;
	MonoThunkFreeList *l = (MonoThunkFreeList*)(p - 1);
	gboolean found = FALSE;

	mono_domain_lock (domain);
	if (!domain->generic_virtual_thunks)
		domain->generic_virtual_thunks = g_hash_table_new (NULL, NULL);
	if (g_hash_table_lookup (domain->generic_virtual_thunks, l))
		found = TRUE;
	mono_domain_unlock (domain);

	if (!found)
		/* Not allocated by mono_method_alloc_generic_virtual_thunk (), i.e. AOT */
		return;
	init_thunk_free_lists (domain);

	while (domain->thunk_free_lists [0] && domain->thunk_free_lists [0]->length >= MAX_WAIT_LENGTH) {
		MonoThunkFreeList *item = domain->thunk_free_lists [0];
		int length = item->length;
		int i;

		/* unlink the first item from the wait list */
		domain->thunk_free_lists [0] = item->next;
		domain->thunk_free_lists [0]->length = length - 1;

		i = list_index_for_size (item->size);

		/* put it in the free list */
		item->next = domain->thunk_free_lists [i];
		domain->thunk_free_lists [i] = item;
	}

	l->next = NULL;
	if (domain->thunk_free_lists [1]) {
		domain->thunk_free_lists [1] = domain->thunk_free_lists [1]->next = l;
		domain->thunk_free_lists [0]->length++;
	} else {
		g_assert (!domain->thunk_free_lists [0]);

		domain->thunk_free_lists [0] = domain->thunk_free_lists [1] = l;
		domain->thunk_free_lists [0]->length = 1;
	}
}

typedef struct _GenericVirtualCase {
	MonoMethod *method;
	gpointer code;
	int count;
	struct _GenericVirtualCase *next;
} GenericVirtualCase;

/*
 * get_generic_virtual_entries:
 *
 *   Return IMT entries for the generic virtual method instances and
 *   variant interface methods for vtable slot
 * VTABLE_SLOT.
 */ 
static MonoImtBuilderEntry*
get_generic_virtual_entries (MonoDomain *domain, gpointer *vtable_slot)
{
	MONO_REQ_GC_NEUTRAL_MODE;

  	GenericVirtualCase *list;
 	MonoImtBuilderEntry *entries;
  
 	mono_domain_lock (domain);
 	if (!domain->generic_virtual_cases)
 		domain->generic_virtual_cases = g_hash_table_new (mono_aligned_addr_hash, NULL);
 
 	list = g_hash_table_lookup (domain->generic_virtual_cases, vtable_slot);
 
 	entries = NULL;
 	for (; list; list = list->next) {
 		MonoImtBuilderEntry *entry;
 
 		if (list->count < THUNK_THRESHOLD)
 			continue;
 
 		entry = g_new0 (MonoImtBuilderEntry, 1);
 		entry->key = list->method;
 		entry->value.target_code = mono_get_addr_from_ftnptr (list->code);
 		entry->has_target_code = 1;
 		if (entries)
 			entry->children = entries->children + 1;
 		entry->next = entries;
 		entries = entry;
 	}
 
 	mono_domain_unlock (domain);
 
 	/* FIXME: Leaking memory ? */
 	return entries;
}

/**
 * mono_method_add_generic_virtual_invocation:
 * @domain: a domain
 * @vtable_slot: pointer to the vtable slot
 * @method: the inflated generic virtual method
 * @code: the method's code
 *
 * Registers a call via unmanaged code to a generic virtual method
 * instantiation or variant interface method.  If the number of calls reaches a threshold
 * (THUNK_THRESHOLD), the method is added to the vtable slot's generic
 * virtual method thunk.
 */
void
mono_method_add_generic_virtual_invocation (MonoDomain *domain, MonoVTable *vtable,
											gpointer *vtable_slot,
											MonoMethod *method, gpointer code)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	static gboolean inited = FALSE;
	static int num_added = 0;

	GenericVirtualCase *gvc, *list;
	MonoImtBuilderEntry *entries;
	int i;
	GPtrArray *sorted;

	mono_domain_lock (domain);
	if (!domain->generic_virtual_cases)
		domain->generic_virtual_cases = g_hash_table_new (mono_aligned_addr_hash, NULL);

	/* Check whether the case was already added */
	list = g_hash_table_lookup (domain->generic_virtual_cases, vtable_slot);
	gvc = list;
	while (gvc) {
		if (gvc->method == method)
			break;
		gvc = gvc->next;
	}

	/* If not found, make a new one */
	if (!gvc) {
		gvc = mono_domain_alloc (domain, sizeof (GenericVirtualCase));
		gvc->method = method;
		gvc->code = code;
		gvc->count = 0;
		gvc->next = g_hash_table_lookup (domain->generic_virtual_cases, vtable_slot);

		g_hash_table_insert (domain->generic_virtual_cases, vtable_slot, gvc);

		if (!inited) {
			mono_counters_register ("Generic virtual cases", MONO_COUNTER_GENERICS | MONO_COUNTER_INT, &num_added);
			inited = TRUE;
		}
		num_added++;
	}

	if (++gvc->count == THUNK_THRESHOLD) {
		gpointer *old_thunk = *vtable_slot;
		gpointer vtable_trampoline = NULL;
		gpointer imt_trampoline = NULL;

		if ((gpointer)vtable_slot < (gpointer)vtable) {
			int displacement = (gpointer*)vtable_slot - (gpointer*)vtable;
			int imt_slot = MONO_IMT_SIZE + displacement;

			/* Force the rebuild of the thunk at the next call */
			imt_trampoline = callbacks.get_imt_trampoline (imt_slot);
			*vtable_slot = imt_trampoline;
		} else {
			vtable_trampoline = callbacks.get_vtable_trampoline ? callbacks.get_vtable_trampoline ((gpointer*)vtable_slot - (gpointer*)vtable->vtable) : NULL;

			entries = get_generic_virtual_entries (domain, vtable_slot);

			sorted = imt_sort_slot_entries (entries);

			*vtable_slot = imt_thunk_builder (NULL, domain, (MonoIMTCheckItem**)sorted->pdata, sorted->len,
											  vtable_trampoline);

			while (entries) {
				MonoImtBuilderEntry *next = entries->next;
				g_free (entries);
				entries = next;
			}

			for (i = 0; i < sorted->len; ++i)
				g_free (g_ptr_array_index (sorted, i));
			g_ptr_array_free (sorted, TRUE);
		}

#ifndef __native_client__
		/* We don't re-use any thunks as there is a lot of overhead */
		/* to deleting and re-using code in Native Client.          */
		if (old_thunk != vtable_trampoline && old_thunk != imt_trampoline)
			invalidate_generic_virtual_thunk (domain, old_thunk);
#endif
	}

	mono_domain_unlock (domain);
}

static MonoVTable *mono_class_create_runtime_vtable (MonoDomain *domain, MonoClass *class, gboolean raise_on_error);

/**
 * mono_class_vtable:
 * @domain: the application domain
 * @class: the class to initialize
 *
 * VTables are domain specific because we create domain specific code, and 
 * they contain the domain specific static class data.
 * On failure, NULL is returned, and class->exception_type is set.
 */
MonoVTable *
mono_class_vtable (MonoDomain *domain, MonoClass *class)
{
	return mono_class_vtable_full (domain, class, FALSE);
}

/**
 * mono_class_vtable_full:
 * @domain: the application domain
 * @class: the class to initialize
 * @raise_on_error if an exception should be raised on failure or not
 *
 * VTables are domain specific because we create domain specific code, and 
 * they contain the domain specific static class data.
 */
MonoVTable *
mono_class_vtable_full (MonoDomain *domain, MonoClass *class, gboolean raise_on_error)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoClassRuntimeInfo *runtime_info;

	g_assert (class);

	if (class->exception_type) {
		if (raise_on_error)
			mono_raise_exception (mono_class_get_exception_for_failure (class));
		return NULL;
	}

	/* this check can be inlined in jitted code, too */
	runtime_info = class->runtime_info;
	if (runtime_info && runtime_info->max_domain >= domain->domain_id && runtime_info->domain_vtables [domain->domain_id])
		return runtime_info->domain_vtables [domain->domain_id];
	return mono_class_create_runtime_vtable (domain, class, raise_on_error);
}

/**
 * mono_class_try_get_vtable:
 * @domain: the application domain
 * @class: the class to initialize
 *
 * This function tries to get the associated vtable from @class if
 * it was already created.
 */
MonoVTable *
mono_class_try_get_vtable (MonoDomain *domain, MonoClass *class)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	MonoClassRuntimeInfo *runtime_info;

	g_assert (class);

	runtime_info = class->runtime_info;
	if (runtime_info && runtime_info->max_domain >= domain->domain_id && runtime_info->domain_vtables [domain->domain_id])
		return runtime_info->domain_vtables [domain->domain_id];
	return NULL;
}

static gpointer*
alloc_vtable (MonoDomain *domain, size_t vtable_size, size_t imt_table_bytes)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	size_t alloc_offset;

	/*
	 * We want the pointer to the MonoVTable aligned to 8 bytes because SGen uses three
	 * address bits.  The IMT has an odd number of entries, however, so on 32 bits the
	 * alignment will be off.  In that case we allocate 4 more bytes and skip over them.
	 */
	if (sizeof (gpointer) == 4 && (imt_table_bytes & 7)) {
		g_assert ((imt_table_bytes & 7) == 4);
		vtable_size += 4;
		alloc_offset = 4;
	} else {
		alloc_offset = 0;
	}

	return (gpointer*) ((char*)mono_domain_alloc0 (domain, vtable_size) + alloc_offset);
}

static MonoVTable *
mono_class_create_runtime_vtable (MonoDomain *domain, MonoClass *class, gboolean raise_on_error)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoVTable *vt;
	MonoClassRuntimeInfo *runtime_info, *old_info;
	MonoClassField *field;
	char *t;
	int i, vtable_slots;
	size_t imt_table_bytes;
	int gc_bits;
	guint32 vtable_size, class_size;
	gpointer iter;
	gpointer *interface_offsets;

	mono_loader_lock (); /*FIXME mono_class_init acquires it*/
	mono_domain_lock (domain);
	runtime_info = class->runtime_info;
	if (runtime_info && runtime_info->max_domain >= domain->domain_id && runtime_info->domain_vtables [domain->domain_id]) {
		mono_domain_unlock (domain);
		mono_loader_unlock ();
		return runtime_info->domain_vtables [domain->domain_id];
	}
	if (!class->inited || class->exception_type) {
		if (!mono_class_init (class) || class->exception_type) {
			mono_domain_unlock (domain);
			mono_loader_unlock ();
			if (raise_on_error)
				mono_raise_exception (mono_class_get_exception_for_failure (class));
			return NULL;
		}
	}

	/* Array types require that their element type be valid*/
	if (class->byval_arg.type == MONO_TYPE_ARRAY || class->byval_arg.type == MONO_TYPE_SZARRAY) {
		MonoClass *element_class = class->element_class;
		if (!element_class->inited)
			mono_class_init (element_class);

		/*mono_class_init can leave the vtable layout to be lazily done and we can't afford this here*/
		if (element_class->exception_type == MONO_EXCEPTION_NONE && !element_class->vtable_size)
			mono_class_setup_vtable (element_class);
		
		if (element_class->exception_type != MONO_EXCEPTION_NONE) {
			/*Can happen if element_class only got bad after mono_class_setup_vtable*/
			if (class->exception_type == MONO_EXCEPTION_NONE)
				mono_class_set_failure (class, MONO_EXCEPTION_TYPE_LOAD, NULL);
			mono_domain_unlock (domain);
			mono_loader_unlock ();
			if (raise_on_error)
				mono_raise_exception (mono_class_get_exception_for_failure (class));
			return NULL;
		}
	}

	/* 
	 * For some classes, mono_class_init () already computed class->vtable_size, and 
	 * that is all that is needed because of the vtable trampolines.
	 */
	if (!class->vtable_size)
		mono_class_setup_vtable (class);

	if (class->generic_class && !class->vtable)
		mono_class_check_vtable_constraints (class, NULL);

	/* Initialize klass->has_finalize */
	mono_class_has_finalizer (class);

	if (class->exception_type) {
		mono_domain_unlock (domain);
		mono_loader_unlock ();
		if (raise_on_error)
			mono_raise_exception (mono_class_get_exception_for_failure (class));
		return NULL;
	}

	vtable_slots = class->vtable_size;
	/* we add an additional vtable slot to store the pointer to static field data only when needed */
	class_size = mono_class_data_size (class);
	if (class_size)
		vtable_slots++;

	if (class->interface_offsets_count) {
		imt_table_bytes = sizeof (gpointer) * (MONO_IMT_SIZE);
		mono_stats.imt_number_of_tables++;
		mono_stats.imt_tables_size += imt_table_bytes;
	} else {
		imt_table_bytes = 0;
	}

	vtable_size = imt_table_bytes + MONO_SIZEOF_VTABLE + vtable_slots * sizeof (gpointer);

	mono_stats.used_class_count++;
	mono_stats.class_vtable_size += vtable_size;

	interface_offsets = alloc_vtable (domain, vtable_size, imt_table_bytes);
	vt = (MonoVTable*) ((char*)interface_offsets + imt_table_bytes);
	g_assert (!((gsize)vt & 7));

	vt->klass = class;
	vt->rank = class->rank;
	vt->domain = domain;

	mono_class_compute_gc_descriptor (class);
		/*
		 * We can't use typed allocation in the non-root domains, since the
		 * collector needs the GC descriptor stored in the vtable even after
		 * the mempool containing the vtable is destroyed when the domain is
		 * unloaded. An alternative might be to allocate vtables in the GC
		 * heap, but this does not seem to work (it leads to crashes inside
		 * libgc). If that approach is tried, two gc descriptors need to be
		 * allocated for each class: one for the root domain, and one for all
		 * other domains. The second descriptor should contain a bit for the
		 * vtable field in MonoObject, since we can no longer assume the 
		 * vtable is reachable by other roots after the appdomain is unloaded.
		 */
#ifdef HAVE_BOEHM_GC
	if (domain != mono_get_root_domain () && !mono_dont_free_domains)
		vt->gc_descr = MONO_GC_DESCRIPTOR_NULL;
	else
#endif
		vt->gc_descr = class->gc_descr;

	gc_bits = mono_gc_get_vtable_bits (class);
	g_assert (!(gc_bits & ~((1 << MONO_VTABLE_AVAILABLE_GC_BITS) - 1)));

	vt->gc_bits = gc_bits;

	if (class_size) {
		/* we store the static field pointer at the end of the vtable: vt->vtable [class->vtable_size] */
		if (class->has_static_refs) {
			MonoGCDescriptor statics_gc_descr;
			int max_set = 0;
			gsize default_bitmap [4] = {0};
			gsize *bitmap;

			bitmap = compute_class_bitmap (class, default_bitmap, sizeof (default_bitmap) * 8, 0, &max_set, TRUE);
			/*g_print ("bitmap 0x%x for %s.%s (size: %d)\n", bitmap [0], class->name_space, class->name, class_size);*/
			statics_gc_descr = mono_gc_make_descr_from_bitmap (bitmap, max_set + 1);
			vt->vtable [class->vtable_size] = mono_gc_alloc_fixed (class_size, statics_gc_descr, MONO_ROOT_SOURCE_STATIC, "managed static variables");
			mono_domain_add_class_static_data (domain, class, vt->vtable [class->vtable_size], NULL);
			if (bitmap != default_bitmap)
				g_free (bitmap);
		} else {
			vt->vtable [class->vtable_size] = mono_domain_alloc0 (domain, class_size);
		}
		vt->has_static_fields = TRUE;
		mono_stats.class_static_data_size += class_size;
	}

	iter = NULL;
	while ((field = mono_class_get_fields (class, &iter))) {
		if (!(field->type->attrs & FIELD_ATTRIBUTE_STATIC))
			continue;
		if (mono_field_is_deleted (field))
			continue;
		if (!(field->type->attrs & FIELD_ATTRIBUTE_LITERAL)) {
			gint32 special_static = class->no_special_static_fields ? SPECIAL_STATIC_NONE : field_is_special_static (class, field);
			if (special_static != SPECIAL_STATIC_NONE) {
				guint32 size, offset;
				gint32 align;
				gsize default_bitmap [4] = {0};
				gsize *bitmap;
				int max_set = 0;
				int numbits;
				MonoClass *fclass;
				if (mono_type_is_reference (field->type)) {
					default_bitmap [0] = 1;
					numbits = 1;
					bitmap = default_bitmap;
				} else if (mono_type_is_struct (field->type)) {
					fclass = mono_class_from_mono_type (field->type);
					bitmap = compute_class_bitmap (fclass, default_bitmap, sizeof (default_bitmap) * 8, - (int)(sizeof (MonoObject) / sizeof (gpointer)), &max_set, FALSE);
					numbits = max_set + 1;
				} else {
					default_bitmap [0] = 0;
					numbits = 0;
					bitmap = default_bitmap;
				}
				size = mono_type_size (field->type, &align);
				offset = mono_alloc_special_static_data (special_static, size, align, (uintptr_t*)bitmap, numbits);
				if (!domain->special_static_fields)
					domain->special_static_fields = g_hash_table_new (NULL, NULL);
				g_hash_table_insert (domain->special_static_fields, field, GUINT_TO_POINTER (offset));
				if (bitmap != default_bitmap)
					g_free (bitmap);
				/* 
				 * This marks the field as special static to speed up the
				 * checks in mono_field_static_get/set_value ().
				 */
				field->offset = -1;
				continue;
			}
		}
		if ((field->type->attrs & FIELD_ATTRIBUTE_HAS_FIELD_RVA)) {
			MonoClass *fklass = mono_class_from_mono_type (field->type);
			const char *data = mono_field_get_data (field);

			g_assert (!(field->type->attrs & FIELD_ATTRIBUTE_HAS_DEFAULT));
			t = (char*)mono_vtable_get_static_field_data (vt) + field->offset;
			/* some fields don't really have rva, they are just zeroed (bss? bug #343083) */
			if (!data)
				continue;
			if (fklass->valuetype) {
				memcpy (t, data, mono_class_value_size (fklass, NULL));
			} else {
				/* it's a pointer type: add check */
				g_assert ((fklass->byval_arg.type == MONO_TYPE_PTR) || (fklass->byval_arg.type == MONO_TYPE_FNPTR));
				*t = *(char *)data;
			}
			continue;
		}		
	}

	vt->max_interface_id = class->max_interface_id;
	vt->interface_bitmap = class->interface_bitmap;
	
	//printf ("Initializing VT for class %s (interface_offsets_count = %d)\n",
	//		class->name, class->interface_offsets_count);

	/* Initialize vtable */
	if (callbacks.get_vtable_trampoline) {
		// This also covers the AOT case
		for (i = 0; i < class->vtable_size; ++i) {
			vt->vtable [i] = callbacks.get_vtable_trampoline (i);
		}
	} else {
		mono_class_setup_vtable (class);

		for (i = 0; i < class->vtable_size; ++i) {
			MonoMethod *cm;

			if ((cm = class->vtable [i]))
				vt->vtable [i] = arch_create_jit_trampoline (cm);
		}
	}

	if (imt_table_bytes) {
		/* Now that the vtable is full, we can actually fill up the IMT */
			for (i = 0; i < MONO_IMT_SIZE; ++i)
				interface_offsets [i] = callbacks.get_imt_trampoline (i);
	}

	/*
	 * FIXME: Is it ok to allocate while holding the domain/loader locks ? If not, we can release them, allocate, then
	 * re-acquire them and check if another thread has created the vtable in the meantime.
	 */
	/* Special case System.MonoType to avoid infinite recursion */
	if (class != mono_defaults.monotype_class) {
		/*FIXME check for OOM*/
		vt->type = mono_type_get_object (domain, &class->byval_arg);
		if (mono_object_get_class (vt->type) != mono_defaults.monotype_class)
			/* This is unregistered in
			   unregister_vtable_reflection_type() in
			   domain.c. */
			MONO_GC_REGISTER_ROOT_IF_MOVING(vt->type, MONO_ROOT_SOURCE_REFLECTION, "vtable reflection type");
	}

	mono_vtable_set_is_remote (vt, mono_class_is_contextbound (class));

	/*  class_vtable_array keeps an array of created vtables
	 */
	g_ptr_array_add (domain->class_vtable_array, vt);
	/* class->runtime_info is protected by the loader lock, both when
	 * it it enlarged and when it is stored info.
	 */

	/*
	 * Store the vtable in class->runtime_info.
	 * class->runtime_info is accessed without locking, so this do this last after the vtable has been constructed.
	 */
	mono_memory_barrier ();

	old_info = class->runtime_info;
	if (old_info && old_info->max_domain >= domain->domain_id) {
		/* someone already created a large enough runtime info */
		old_info->domain_vtables [domain->domain_id] = vt;
	} else {
		int new_size = domain->domain_id;
		if (old_info)
			new_size = MAX (new_size, old_info->max_domain);
		new_size++;
		/* make the new size a power of two */
		i = 2;
		while (new_size > i)
			i <<= 1;
		new_size = i;
		/* this is a bounded memory retention issue: may want to 
		 * handle it differently when we'll have a rcu-like system.
		 */
		runtime_info = mono_image_alloc0 (class->image, MONO_SIZEOF_CLASS_RUNTIME_INFO + new_size * sizeof (gpointer));
		runtime_info->max_domain = new_size - 1;
		/* copy the stuff from the older info */
		if (old_info) {
			memcpy (runtime_info->domain_vtables, old_info->domain_vtables, (old_info->max_domain + 1) * sizeof (gpointer));
		}
		runtime_info->domain_vtables [domain->domain_id] = vt;
		/* keep this last*/
		mono_memory_barrier ();
		class->runtime_info = runtime_info;
	}

	if (class == mono_defaults.monotype_class) {
		/*FIXME check for OOM*/
		vt->type = mono_type_get_object (domain, &class->byval_arg);
		if (mono_object_get_class (vt->type) != mono_defaults.monotype_class)
			/* This is unregistered in
			   unregister_vtable_reflection_type() in
			   domain.c. */
			MONO_GC_REGISTER_ROOT_IF_MOVING(vt->type, MONO_ROOT_SOURCE_REFLECTION, "vtable reflection type");
	}

	mono_domain_unlock (domain);
	mono_loader_unlock ();

	/* make sure the parent is initialized */
	/*FIXME shouldn't this fail the current type?*/
	if (class->parent)
		mono_class_vtable_full (domain, class->parent, raise_on_error);

	return vt;
}

#ifndef DISABLE_REMOTING
/**
 * mono_class_proxy_vtable:
 * @domain: the application domain
 * @remove_class: the remote class
 *
 * Creates a vtable for transparent proxies. It is basically
 * a copy of the real vtable of the class wrapped in @remote_class,
 * but all function pointers invoke the remoting functions, and
 * vtable->klass points to the transparent proxy class, and not to @class.
 */
static MonoVTable *
mono_class_proxy_vtable (MonoDomain *domain, MonoRemoteClass *remote_class, MonoRemotingTarget target_type)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoError error;
	MonoVTable *vt, *pvt;
	int i, j, vtsize, max_interface_id, extra_interface_vtsize = 0;
	MonoClass *k;
	GSList *extra_interfaces = NULL;
	MonoClass *class = remote_class->proxy_class;
	gpointer *interface_offsets;
	uint8_t *bitmap;
	int bsize;
	size_t imt_table_bytes;
	
#ifdef COMPRESSED_INTERFACE_BITMAP
	int bcsize;
#endif

	vt = mono_class_vtable (domain, class);
	g_assert (vt); /*FIXME property handle failure*/
	max_interface_id = vt->max_interface_id;
	
	/* Calculate vtable space for extra interfaces */
	for (j = 0; j < remote_class->interface_count; j++) {
		MonoClass* iclass = remote_class->interfaces[j];
		GPtrArray *ifaces;
		int method_count;

		/*FIXME test for interfaces with variant generic arguments*/
		if (MONO_CLASS_IMPLEMENTS_INTERFACE (class, iclass->interface_id))
			continue;	/* interface implemented by the class */
		if (g_slist_find (extra_interfaces, iclass))
			continue;
			
		extra_interfaces = g_slist_prepend (extra_interfaces, iclass);
		
		method_count = mono_class_num_methods (iclass);
	
		ifaces = mono_class_get_implemented_interfaces (iclass, &error);
		g_assert (mono_error_ok (&error)); /*FIXME do proper error handling*/
		if (ifaces) {
			for (i = 0; i < ifaces->len; ++i) {
				MonoClass *ic = g_ptr_array_index (ifaces, i);
				/*FIXME test for interfaces with variant generic arguments*/
				if (MONO_CLASS_IMPLEMENTS_INTERFACE (class, ic->interface_id))
					continue;	/* interface implemented by the class */
				if (g_slist_find (extra_interfaces, ic))
					continue;
				extra_interfaces = g_slist_prepend (extra_interfaces, ic);
				method_count += mono_class_num_methods (ic);
			}
			g_ptr_array_free (ifaces, TRUE);
		}

		extra_interface_vtsize += method_count * sizeof (gpointer);
		if (iclass->max_interface_id > max_interface_id) max_interface_id = iclass->max_interface_id;
	}

	imt_table_bytes = sizeof (gpointer) * MONO_IMT_SIZE;
	mono_stats.imt_number_of_tables++;
	mono_stats.imt_tables_size += imt_table_bytes;

	vtsize = imt_table_bytes + MONO_SIZEOF_VTABLE + class->vtable_size * sizeof (gpointer);

	mono_stats.class_vtable_size += vtsize + extra_interface_vtsize;

	interface_offsets = alloc_vtable (domain, vtsize + extra_interface_vtsize, imt_table_bytes);
	pvt = (MonoVTable*) ((char*)interface_offsets + imt_table_bytes);
	g_assert (!((gsize)pvt & 7));

	memcpy (pvt, vt, MONO_SIZEOF_VTABLE + class->vtable_size * sizeof (gpointer));

	pvt->klass = mono_defaults.transparent_proxy_class;
	/* we need to keep the GC descriptor for a transparent proxy or we confuse the precise GC */
	pvt->gc_descr = mono_defaults.transparent_proxy_class->gc_descr;

	/* initialize vtable */
	mono_class_setup_vtable (class);
	for (i = 0; i < class->vtable_size; ++i) {
		MonoMethod *cm;
		    
		if ((cm = class->vtable [i]))
			pvt->vtable [i] = arch_create_remoting_trampoline (domain, cm, target_type);
		else
			pvt->vtable [i] = NULL;
	}

	if (class->flags & TYPE_ATTRIBUTE_ABSTRACT) {
		/* create trampolines for abstract methods */
		for (k = class; k; k = k->parent) {
			MonoMethod* m;
			gpointer iter = NULL;
			while ((m = mono_class_get_methods (k, &iter)))
				if (!pvt->vtable [m->slot])
					pvt->vtable [m->slot] = arch_create_remoting_trampoline (domain, m, target_type);
		}
	}

	pvt->max_interface_id = max_interface_id;
	bsize = sizeof (guint8) * (max_interface_id/8 + 1 );
#ifdef COMPRESSED_INTERFACE_BITMAP
	bitmap = g_malloc0 (bsize);
#else
	bitmap = mono_domain_alloc0 (domain, bsize);
#endif

	for (i = 0; i < class->interface_offsets_count; ++i) {
		int interface_id = class->interfaces_packed [i]->interface_id;
		bitmap [interface_id >> 3] |= (1 << (interface_id & 7));
	}

	if (extra_interfaces) {
		int slot = class->vtable_size;
		MonoClass* interf;
		gpointer iter;
		MonoMethod* cm;
		GSList *list_item;

		/* Create trampolines for the methods of the interfaces */
		for (list_item = extra_interfaces; list_item != NULL; list_item=list_item->next) {
			interf = list_item->data;
			
			bitmap [interf->interface_id >> 3] |= (1 << (interf->interface_id & 7));

			iter = NULL;
			j = 0;
			while ((cm = mono_class_get_methods (interf, &iter)))
				pvt->vtable [slot + j++] = arch_create_remoting_trampoline (domain, cm, target_type);
			
			slot += mono_class_num_methods (interf);
		}
	}

	/* Now that the vtable is full, we can actually fill up the IMT */
	build_imt (class, pvt, domain, interface_offsets, extra_interfaces);
	if (extra_interfaces) {
		g_slist_free (extra_interfaces);
	}

#ifdef COMPRESSED_INTERFACE_BITMAP
	bcsize = mono_compress_bitmap (NULL, bitmap, bsize);
	pvt->interface_bitmap = mono_domain_alloc0 (domain, bcsize);
	mono_compress_bitmap (pvt->interface_bitmap, bitmap, bsize);
	g_free (bitmap);
#else
	pvt->interface_bitmap = bitmap;
#endif
	return pvt;
}

#endif /* DISABLE_REMOTING */

/**
 * mono_class_field_is_special_static:
 *
 *   Returns whether @field is a thread/context static field.
 */
gboolean
mono_class_field_is_special_static (MonoClassField *field)
{
	MONO_REQ_GC_NEUTRAL_MODE

	if (!(field->type->attrs & FIELD_ATTRIBUTE_STATIC))
		return FALSE;
	if (mono_field_is_deleted (field))
		return FALSE;
	if (!(field->type->attrs & FIELD_ATTRIBUTE_LITERAL)) {
		if (field->offset == -1)
			return TRUE;
		if (field_is_special_static (field->parent, field) != SPECIAL_STATIC_NONE)
			return TRUE;
	}
	return FALSE;
}

/**
 * mono_class_field_get_special_static_type:
 * @field: The MonoClassField describing the field.
 *
 * Returns: SPECIAL_STATIC_THREAD if the field is thread static, SPECIAL_STATIC_CONTEXT if it is context static,
 * SPECIAL_STATIC_NONE otherwise.
 */
guint32
mono_class_field_get_special_static_type (MonoClassField *field)
{
	MONO_REQ_GC_NEUTRAL_MODE

	if (!(field->type->attrs & FIELD_ATTRIBUTE_STATIC))
		return SPECIAL_STATIC_NONE;
	if (mono_field_is_deleted (field))
		return SPECIAL_STATIC_NONE;
	if (!(field->type->attrs & FIELD_ATTRIBUTE_LITERAL))
		return field_is_special_static (field->parent, field);
	return SPECIAL_STATIC_NONE;
}

/**
 * mono_class_has_special_static_fields:
 * 
 *   Returns whenever @klass has any thread/context static fields.
 */
gboolean
mono_class_has_special_static_fields (MonoClass *klass)
{
	MONO_REQ_GC_NEUTRAL_MODE

	MonoClassField *field;
	gpointer iter;

	iter = NULL;
	while ((field = mono_class_get_fields (klass, &iter))) {
		g_assert (field->parent == klass);
		if (mono_class_field_is_special_static (field))
			return TRUE;
	}

	return FALSE;
}

#ifndef DISABLE_REMOTING
/**
 * create_remote_class_key:
 * Creates an array of pointers that can be used as a hash key for a remote class.
 * The first element of the array is the number of pointers.
 */
static gpointer*
create_remote_class_key (MonoRemoteClass *remote_class, MonoClass *extra_class)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	gpointer *key;
	int i, j;
	
	if (remote_class == NULL) {
		if (extra_class->flags & TYPE_ATTRIBUTE_INTERFACE) {
			key = g_malloc (sizeof(gpointer) * 3);
			key [0] = GINT_TO_POINTER (2);
			key [1] = mono_defaults.marshalbyrefobject_class;
			key [2] = extra_class;
		} else {
			key = g_malloc (sizeof(gpointer) * 2);
			key [0] = GINT_TO_POINTER (1);
			key [1] = extra_class;
		}
	} else {
		if (extra_class != NULL && (extra_class->flags & TYPE_ATTRIBUTE_INTERFACE)) {
			key = g_malloc (sizeof(gpointer) * (remote_class->interface_count + 3));
			key [0] = GINT_TO_POINTER (remote_class->interface_count + 2);
			key [1] = remote_class->proxy_class;

			// Keep the list of interfaces sorted
			for (i = 0, j = 2; i < remote_class->interface_count; i++, j++) {
				if (extra_class && remote_class->interfaces [i] > extra_class) {
					key [j++] = extra_class;
					extra_class = NULL;
				}
				key [j] = remote_class->interfaces [i];
			}
			if (extra_class)
				key [j] = extra_class;
		} else {
			// Replace the old class. The interface list is the same
			key = g_malloc (sizeof(gpointer) * (remote_class->interface_count + 2));
			key [0] = GINT_TO_POINTER (remote_class->interface_count + 1);
			key [1] = extra_class != NULL ? extra_class : remote_class->proxy_class;
			for (i = 0; i < remote_class->interface_count; i++)
				key [2 + i] = remote_class->interfaces [i];
		}
	}
	
	return key;
}

/**
 * copy_remote_class_key:
 *
 *   Make a copy of KEY in the domain and return the copy.
 */
static gpointer*
copy_remote_class_key (MonoDomain *domain, gpointer *key)
{
	MONO_REQ_GC_NEUTRAL_MODE

	int key_size = (GPOINTER_TO_UINT (key [0]) + 1) * sizeof (gpointer);
	gpointer *mp_key = mono_domain_alloc (domain, key_size);

	memcpy (mp_key, key, key_size);

	return mp_key;
}

/**
 * mono_remote_class:
 * @domain: the application domain
 * @class_name: name of the remote class
 *
 * Creates and initializes a MonoRemoteClass object for a remote type. 
 *
 * Can raise an exception on failure. 
 */
MonoRemoteClass*
mono_remote_class (MonoDomain *domain, MonoString *class_name, MonoClass *proxy_class)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoError error;
	MonoRemoteClass *rc;
	gpointer* key, *mp_key;
	char *name;
	
	key = create_remote_class_key (NULL, proxy_class);
	
	mono_domain_lock (domain);
	rc = g_hash_table_lookup (domain->proxy_vtable_hash, key);

	if (rc) {
		g_free (key);
		mono_domain_unlock (domain);
		return rc;
	}

	name = mono_string_to_utf8_mp (domain->mp, class_name, &error);
	if (!mono_error_ok (&error)) {
		g_free (key);
		mono_domain_unlock (domain);
		mono_error_raise_exception (&error);
	}

	mp_key = copy_remote_class_key (domain, key);
	g_free (key);
	key = mp_key;

	if (proxy_class->flags & TYPE_ATTRIBUTE_INTERFACE) {
		rc = mono_domain_alloc (domain, MONO_SIZEOF_REMOTE_CLASS + sizeof(MonoClass*));
		rc->interface_count = 1;
		rc->interfaces [0] = proxy_class;
		rc->proxy_class = mono_defaults.marshalbyrefobject_class;
	} else {
		rc = mono_domain_alloc (domain, MONO_SIZEOF_REMOTE_CLASS);
		rc->interface_count = 0;
		rc->proxy_class = proxy_class;
	}
	
	rc->default_vtable = NULL;
	rc->xdomain_vtable = NULL;
	rc->proxy_class_name = name;
#ifndef DISABLE_PERFCOUNTERS
	mono_perfcounters->loader_bytes += mono_string_length (class_name) + 1;
#endif

	g_hash_table_insert (domain->proxy_vtable_hash, key, rc);

	mono_domain_unlock (domain);
	return rc;
}

/**
 * clone_remote_class:
 * Creates a copy of the remote_class, adding the provided class or interface
 */
static MonoRemoteClass*
clone_remote_class (MonoDomain *domain, MonoRemoteClass* remote_class, MonoClass *extra_class)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	MonoRemoteClass *rc;
	gpointer* key, *mp_key;
	
	key = create_remote_class_key (remote_class, extra_class);
	rc = g_hash_table_lookup (domain->proxy_vtable_hash, key);
	if (rc != NULL) {
		g_free (key);
		return rc;
	}

	mp_key = copy_remote_class_key (domain, key);
	g_free (key);
	key = mp_key;

	if (extra_class->flags & TYPE_ATTRIBUTE_INTERFACE) {
		int i,j;
		rc = mono_domain_alloc (domain, MONO_SIZEOF_REMOTE_CLASS + sizeof(MonoClass*) * (remote_class->interface_count + 1));
		rc->proxy_class = remote_class->proxy_class;
		rc->interface_count = remote_class->interface_count + 1;
		
		// Keep the list of interfaces sorted, since the hash key of
		// the remote class depends on this
		for (i = 0, j = 0; i < remote_class->interface_count; i++, j++) {
			if (remote_class->interfaces [i] > extra_class && i == j)
				rc->interfaces [j++] = extra_class;
			rc->interfaces [j] = remote_class->interfaces [i];
		}
		if (i == j)
			rc->interfaces [j] = extra_class;
	} else {
		// Replace the old class. The interface array is the same
		rc = mono_domain_alloc (domain, MONO_SIZEOF_REMOTE_CLASS + sizeof(MonoClass*) * remote_class->interface_count);
		rc->proxy_class = extra_class;
		rc->interface_count = remote_class->interface_count;
		if (rc->interface_count > 0)
			memcpy (rc->interfaces, remote_class->interfaces, rc->interface_count * sizeof (MonoClass*));
	}
	
	rc->default_vtable = NULL;
	rc->xdomain_vtable = NULL;
	rc->proxy_class_name = remote_class->proxy_class_name;

	g_hash_table_insert (domain->proxy_vtable_hash, key, rc);

	return rc;
}

gpointer
mono_remote_class_vtable (MonoDomain *domain, MonoRemoteClass *remote_class, MonoRealProxy *rp)
{
	MONO_REQ_GC_UNSAFE_MODE;

	mono_loader_lock (); /*FIXME mono_class_from_mono_type and mono_class_proxy_vtable take it*/
	mono_domain_lock (domain);
	if (rp->target_domain_id != -1) {
		if (remote_class->xdomain_vtable == NULL)
			remote_class->xdomain_vtable = mono_class_proxy_vtable (domain, remote_class, MONO_REMOTING_TARGET_APPDOMAIN);
		mono_domain_unlock (domain);
		mono_loader_unlock ();
		return remote_class->xdomain_vtable;
	}
	if (remote_class->default_vtable == NULL) {
		MonoType *type;
		MonoClass *klass;
		type = ((MonoReflectionType *)rp->class_to_proxy)->type;
		klass = mono_class_from_mono_type (type);
#ifndef DISABLE_COM
		if ((mono_class_is_com_object (klass) || (mono_class_get_com_object_class () && klass == mono_class_get_com_object_class ())) && !mono_vtable_is_remote (mono_class_vtable (mono_domain_get (), klass)))
			remote_class->default_vtable = mono_class_proxy_vtable (domain, remote_class, MONO_REMOTING_TARGET_COMINTEROP);
		else
#endif
			remote_class->default_vtable = mono_class_proxy_vtable (domain, remote_class, MONO_REMOTING_TARGET_UNKNOWN);
	}
	
	mono_domain_unlock (domain);
	mono_loader_unlock ();
	return remote_class->default_vtable;
}

/**
 * mono_upgrade_remote_class:
 * @domain: the application domain
 * @tproxy: the proxy whose remote class has to be upgraded.
 * @klass: class to which the remote class can be casted.
 *
 * Updates the vtable of the remote class by adding the necessary method slots
 * and interface offsets so it can be safely casted to klass. klass can be a
 * class or an interface.
 */
void
mono_upgrade_remote_class (MonoDomain *domain, MonoObject *proxy_object, MonoClass *klass)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoTransparentProxy *tproxy;
	MonoRemoteClass *remote_class;
	gboolean redo_vtable;

	mono_loader_lock (); /*FIXME mono_remote_class_vtable requires it.*/
	mono_domain_lock (domain);

	tproxy = (MonoTransparentProxy*) proxy_object;
	remote_class = tproxy->remote_class;
	
	if (klass->flags & TYPE_ATTRIBUTE_INTERFACE) {
		int i;
		redo_vtable = TRUE;
		for (i = 0; i < remote_class->interface_count && redo_vtable; i++)
			if (remote_class->interfaces [i] == klass)
				redo_vtable = FALSE;
	}
	else {
		redo_vtable = (remote_class->proxy_class != klass);
	}

	if (redo_vtable) {
		tproxy->remote_class = clone_remote_class (domain, remote_class, klass);
		proxy_object->vtable = mono_remote_class_vtable (domain, tproxy->remote_class, tproxy->rp);
	}
	
	mono_domain_unlock (domain);
	mono_loader_unlock ();
}
#endif /* DISABLE_REMOTING */


/**
 * mono_object_get_virtual_method:
 * @obj: object to operate on.
 * @method: method 
 *
 * Retrieves the MonoMethod that would be called on obj if obj is passed as
 * the instance of a callvirt of method.
 */
MonoMethod*
mono_object_get_virtual_method (MonoObject *obj, MonoMethod *method)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoClass *klass;
	MonoMethod **vtable;
	gboolean is_proxy = FALSE;
	MonoMethod *res = NULL;

	klass = mono_object_class (obj);
#ifndef DISABLE_REMOTING
	if (klass == mono_defaults.transparent_proxy_class) {
		klass = ((MonoTransparentProxy *)obj)->remote_class->proxy_class;
		is_proxy = TRUE;
	}
#endif

	if (!is_proxy && ((method->flags & METHOD_ATTRIBUTE_FINAL) || !(method->flags & METHOD_ATTRIBUTE_VIRTUAL)))
			return method;

	mono_class_setup_vtable (klass);
	vtable = klass->vtable;

	if (method->slot == -1) {
		/* method->slot might not be set for instances of generic methods */
		if (method->is_inflated) {
			g_assert (((MonoMethodInflated*)method)->declaring->slot != -1);
			method->slot = ((MonoMethodInflated*)method)->declaring->slot; 
		} else {
			if (!is_proxy)
				g_assert_not_reached ();
		}
	}

	/* check method->slot is a valid index: perform isinstance? */
	if (method->slot != -1) {
		if (method->klass->flags & TYPE_ATTRIBUTE_INTERFACE) {
			if (!is_proxy) {
				gboolean variance_used = FALSE;
				int iface_offset = mono_class_interface_offset_with_variance (klass, method->klass, &variance_used);
				g_assert (iface_offset > 0);
				res = vtable [iface_offset + method->slot];
			}
		} else {
			res = vtable [method->slot];
		}
    }

#ifndef DISABLE_REMOTING
	if (is_proxy) {
		/* It may be an interface, abstract class method or generic method */
		if (!res || mono_method_signature (res)->generic_param_count)
			res = method;

		/* generic methods demand invoke_with_check */
		if (mono_method_signature (res)->generic_param_count)
			res = mono_marshal_get_remoting_invoke_with_check (res);
		else {
#ifndef DISABLE_COM
			if (klass == mono_class_get_com_object_class () || mono_class_is_com_object (klass))
				res = mono_cominterop_get_invoke (res);
			else
#endif
				res = mono_marshal_get_remoting_invoke (res);
		}
	} else
#endif
	{
		if (method->is_inflated) {
			MonoError error;
			/* Have to inflate the result */
			res = mono_class_inflate_generic_method_checked (res, &((MonoMethodInflated*)method)->context, &error);
			g_assert (mono_error_ok (&error)); /* FIXME don't swallow the error */
		}
	}

	g_assert (res);
	
	return res;
}

static MonoObject*
dummy_mono_runtime_invoke (MonoMethod *method, void *obj, void **params, MonoObject **exc)
{
	g_error ("runtime invoke called on uninitialized runtime");
	return NULL;
}

static MonoInvokeFunc default_mono_runtime_invoke = dummy_mono_runtime_invoke;

/**
 * mono_runtime_invoke:
 * @method: method to invoke
 * @obJ: object instance
 * @params: arguments to the method
 * @exc: exception information.
 *
 * Invokes the method represented by @method on the object @obj.
 *
 * obj is the 'this' pointer, it should be NULL for static
 * methods, a MonoObject* for object instances and a pointer to
 * the value type for value types.
 *
 * The params array contains the arguments to the method with the
 * same convention: MonoObject* pointers for object instances and
 * pointers to the value type otherwise. 
 * 
 * From unmanaged code you'll usually use the
 * mono_runtime_invoke() variant.
 *
 * Note that this function doesn't handle virtual methods for
 * you, it will exec the exact method you pass: we still need to
 * expose a function to lookup the derived class implementation
 * of a virtual method (there are examples of this in the code,
 * though).
 * 
 * You can pass NULL as the exc argument if you don't want to
 * catch exceptions, otherwise, *exc will be set to the exception
 * thrown, if any.  if an exception is thrown, you can't use the
 * MonoObject* result from the function.
 * 
 * If the method returns a value type, it is boxed in an object
 * reference.
 */
MonoObject*
mono_runtime_invoke (MonoMethod *method, void *obj, void **params, MonoObject **exc)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoObject *result;

	if (mono_runtime_get_no_exec ())
		g_warning ("Invoking method '%s' when running in no-exec mode.\n", mono_method_full_name (method, TRUE));

	if (mono_profiler_get_events () & MONO_PROFILE_METHOD_EVENTS)
		mono_profiler_method_start_invoke (method);

	result = default_mono_runtime_invoke (method, obj, params, exc);

	if (mono_profiler_get_events () & MONO_PROFILE_METHOD_EVENTS)
		mono_profiler_method_end_invoke (method);

	return result;
}

/**
 * mono_method_get_unmanaged_thunk:
 * @method: method to generate a thunk for.
 *
 * Returns an unmanaged->managed thunk that can be used to call
 * a managed method directly from C.
 *
 * The thunk's C signature closely matches the managed signature:
 *
 * C#: public bool Equals (object obj);
 * C:  typedef MonoBoolean (*Equals)(MonoObject*,
 *             MonoObject*, MonoException**);
 *
 * The 1st ("this") parameter must not be used with static methods:
 *
 * C#: public static bool ReferenceEquals (object a, object b);
 * C:  typedef MonoBoolean (*ReferenceEquals)(MonoObject*, MonoObject*,
 *             MonoException**);
 *
 * The last argument must be a non-null pointer of a MonoException* pointer.
 * It has "out" semantics. After invoking the thunk, *ex will be NULL if no
 * exception has been thrown in managed code. Otherwise it will point
 * to the MonoException* caught by the thunk. In this case, the result of
 * the thunk is undefined:
 *
 * MonoMethod *method = ... // MonoMethod* of System.Object.Equals
 * MonoException *ex = NULL;
 * Equals func = mono_method_get_unmanaged_thunk (method);
 * MonoBoolean res = func (thisObj, objToCompare, &ex);
 * if (ex) {
 *    // handle exception
 * }
 *
 * The calling convention of the thunk matches the platform's default
 * convention. This means that under Windows, C declarations must
 * contain the __stdcall attribute:
 *
 * C:  typedef MonoBoolean (__stdcall *Equals)(MonoObject*,
 *             MonoObject*, MonoException**);
 *
 * LIMITATIONS
 *
 * Value type arguments and return values are treated as they were objects:
 *
 * C#: public static Rectangle Intersect (Rectangle a, Rectangle b);
 * C:  typedef MonoObject* (*Intersect)(MonoObject*, MonoObject*, MonoException**);
 *
 * Arguments must be properly boxed upon trunk's invocation, while return
 * values must be unboxed.
 */
gpointer
mono_method_get_unmanaged_thunk (MonoMethod *method)
{
	MONO_REQ_GC_NEUTRAL_MODE;
	MONO_REQ_API_ENTRYPOINT;

	gpointer res;

	MONO_PREPARE_RESET_BLOCKING
	method = mono_marshal_get_thunk_invoke_wrapper (method);
	res = mono_compile_method (method);
	MONO_FINISH_RESET_BLOCKING

	return res;
}

void
mono_copy_value (MonoType *type, void *dest, void *value, int deref_pointer)
{
	MONO_REQ_GC_UNSAFE_MODE;

	int t;
	if (type->byref) {
		/* object fields cannot be byref, so we don't need a
		   wbarrier here */
		gpointer *p = (gpointer*)dest;
		*p = value;
		return;
	}
	t = type->type;
handle_enum:
	switch (t) {
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_I1:
	case MONO_TYPE_U1: {
		guint8 *p = (guint8*)dest;
		*p = value ? *(guint8*)value : 0;
		return;
	}
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR: {
		guint16 *p = (guint16*)dest;
		*p = value ? *(guint16*)value : 0;
		return;
	}
#if SIZEOF_VOID_P == 4
	case MONO_TYPE_I:
	case MONO_TYPE_U:
#endif
	case MONO_TYPE_I4:
	case MONO_TYPE_U4: {
		gint32 *p = (gint32*)dest;
		*p = value ? *(gint32*)value : 0;
		return;
	}
#if SIZEOF_VOID_P == 8
	case MONO_TYPE_I:
	case MONO_TYPE_U:
#endif
	case MONO_TYPE_I8:
	case MONO_TYPE_U8: {
		gint64 *p = (gint64*)dest;
		*p = value ? *(gint64*)value : 0;
		return;
	}
	case MONO_TYPE_R4: {
		float *p = (float*)dest;
		*p = value ? *(float*)value : 0;
		return;
	}
	case MONO_TYPE_R8: {
		double *p = (double*)dest;
		*p = value ? *(double*)value : 0;
		return;
	}
	case MONO_TYPE_STRING:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_ARRAY:
		mono_gc_wbarrier_generic_store (dest, deref_pointer? *(gpointer*)value: value);
		return;
	case MONO_TYPE_FNPTR:
	case MONO_TYPE_PTR: {
		gpointer *p = (gpointer*)dest;
		*p = deref_pointer? *(gpointer*)value: value;
		return;
	}
	case MONO_TYPE_VALUETYPE:
		/* note that 't' and 'type->type' can be different */
		if (type->type == MONO_TYPE_VALUETYPE && type->data.klass->enumtype) {
			t = mono_class_enum_basetype (type->data.klass)->type;
			goto handle_enum;
		} else {
			MonoClass *class = mono_class_from_mono_type (type);
			int size = mono_class_value_size (class, NULL);
			if (value == NULL)
				mono_gc_bzero_atomic (dest, size);
			else
				mono_gc_wbarrier_value_copy (dest, value, 1, class);
		}
		return;
	case MONO_TYPE_GENERICINST:
		t = type->data.generic_class->container_class->byval_arg.type;
		goto handle_enum;
	default:
		g_error ("got type %x", type->type);
	}
}

/**
 * mono_field_set_value:
 * @obj: Instance object
 * @field: MonoClassField describing the field to set
 * @value: The value to be set
 *
 * Sets the value of the field described by @field in the object instance @obj
 * to the value passed in @value.   This method should only be used for instance
 * fields.   For static fields, use mono_field_static_set_value.
 *
 * The value must be on the native format of the field type. 
 */
void
mono_field_set_value (MonoObject *obj, MonoClassField *field, void *value)
{
	MONO_REQ_GC_UNSAFE_MODE;

	void *dest;

	g_return_if_fail (!(field->type->attrs & FIELD_ATTRIBUTE_STATIC));

	dest = (char*)obj + field->offset;
	mono_copy_value (field->type, dest, value, FALSE);
}

/**
 * mono_field_static_set_value:
 * @field: MonoClassField describing the field to set
 * @value: The value to be set
 *
 * Sets the value of the static field described by @field
 * to the value passed in @value.
 *
 * The value must be on the native format of the field type. 
 */
void
mono_field_static_set_value (MonoVTable *vt, MonoClassField *field, void *value)
{
	MONO_REQ_GC_UNSAFE_MODE;

	void *dest;

	g_return_if_fail (field->type->attrs & FIELD_ATTRIBUTE_STATIC);
	/* you cant set a constant! */
	g_return_if_fail (!(field->type->attrs & FIELD_ATTRIBUTE_LITERAL));

	if (field->offset == -1) {
		/* Special static */
		gpointer addr;

		mono_domain_lock (vt->domain);
		addr = g_hash_table_lookup (vt->domain->special_static_fields, field);
		mono_domain_unlock (vt->domain);
		dest = mono_get_special_static_data (GPOINTER_TO_UINT (addr));
	} else {
		dest = (char*)mono_vtable_get_static_field_data (vt) + field->offset;
	}
	mono_copy_value (field->type, dest, value, FALSE);
}

/**
 * mono_vtable_get_static_field_data:
 *
 * Internal use function: return a pointer to the memory holding the static fields
 * for a class or NULL if there are no static fields.
 * This is exported only for use by the debugger.
 */
void *
mono_vtable_get_static_field_data (MonoVTable *vt)
{
	MONO_REQ_GC_NEUTRAL_MODE

	if (!vt->has_static_fields)
		return NULL;
	return vt->vtable [vt->klass->vtable_size];
}

static guint8*
mono_field_get_addr (MonoObject *obj, MonoVTable *vt, MonoClassField *field)
{
	MONO_REQ_GC_UNSAFE_MODE;

	guint8 *src;

	if (field->type->attrs & FIELD_ATTRIBUTE_STATIC) {
		if (field->offset == -1) {
			/* Special static */
			gpointer addr;

			mono_domain_lock (vt->domain);
			addr = g_hash_table_lookup (vt->domain->special_static_fields, field);
			mono_domain_unlock (vt->domain);
			src = mono_get_special_static_data (GPOINTER_TO_UINT (addr));
		} else {
			src = (guint8*)mono_vtable_get_static_field_data (vt) + field->offset;
		}
	} else {
		src = (guint8*)obj + field->offset;
	}

	return src;
}

/**
 * mono_field_get_value:
 * @obj: Object instance
 * @field: MonoClassField describing the field to fetch information from
 * @value: pointer to the location where the value will be stored
 *
 * Use this routine to get the value of the field @field in the object
 * passed.
 *
 * The pointer provided by value must be of the field type, for reference
 * types this is a MonoObject*, for value types its the actual pointer to
 * the value type.
 *
 * For example:
 *     int i;
 *     mono_field_get_value (obj, int_field, &i);
 */
void
mono_field_get_value (MonoObject *obj, MonoClassField *field, void *value)
{
	MONO_REQ_GC_UNSAFE_MODE;

	void *src;

	g_assert (obj);

	g_return_if_fail (!(field->type->attrs & FIELD_ATTRIBUTE_STATIC));

	src = (char*)obj + field->offset;
	mono_copy_value (field->type, value, src, TRUE);
}

/**
 * mono_field_get_value_object:
 * @domain: domain where the object will be created (if boxing)
 * @field: MonoClassField describing the field to fetch information from
 * @obj: The object instance for the field.
 *
 * Returns: a new MonoObject with the value from the given field.  If the
 * field represents a value type, the value is boxed.
 *
 */
MonoObject *
mono_field_get_value_object (MonoDomain *domain, MonoClassField *field, MonoObject *obj)
{	
	MONO_REQ_GC_UNSAFE_MODE;

	MonoObject *o;
	MonoClass *klass;
	MonoVTable *vtable = NULL;
	gchar *v;
	gboolean is_static = FALSE;
	gboolean is_ref = FALSE;
	gboolean is_literal = FALSE;
	gboolean is_ptr = FALSE;
	MonoError error;
	MonoType *type = mono_field_get_type_checked (field, &error);

	if (!mono_error_ok (&error))
		mono_error_raise_exception (&error);

	switch (type->type) {
	case MONO_TYPE_STRING:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_SZARRAY:
		is_ref = TRUE;
		break;
	case MONO_TYPE_U1:
	case MONO_TYPE_I1:
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_U2:
	case MONO_TYPE_I2:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_U:
	case MONO_TYPE_I:
	case MONO_TYPE_U4:
	case MONO_TYPE_I4:
	case MONO_TYPE_R4:
	case MONO_TYPE_U8:
	case MONO_TYPE_I8:
	case MONO_TYPE_R8:
	case MONO_TYPE_VALUETYPE:
		is_ref = type->byref;
		break;
	case MONO_TYPE_GENERICINST:
		is_ref = !mono_type_generic_inst_is_valuetype (type);
		break;
	case MONO_TYPE_PTR:
		is_ptr = TRUE;
		break;
	default:
		g_error ("type 0x%x not handled in "
			 "mono_field_get_value_object", type->type);
		return NULL;
	}

	if (type->attrs & FIELD_ATTRIBUTE_LITERAL)
		is_literal = TRUE;

	if (type->attrs & FIELD_ATTRIBUTE_STATIC) {
		is_static = TRUE;

		if (!is_literal) {
			vtable = mono_class_vtable_full (domain, field->parent, TRUE);
			if (!vtable->initialized)
				mono_runtime_class_init (vtable);
		}
	} else {
		g_assert (obj);
	}
	
	if (is_ref) {
		if (is_literal) {
			get_default_field_value (domain, field, &o);
		} else if (is_static) {
			mono_field_static_get_value (vtable, field, &o);
		} else {
			mono_field_get_value (obj, field, &o);
		}
		return o;
	}

	if (is_ptr) {
		static MonoMethod *m;
		gpointer args [2];
		gpointer *ptr;
		gpointer v;

		if (!m) {
			MonoClass *ptr_klass = mono_class_from_name_cached (mono_defaults.corlib, "System.Reflection", "Pointer");
			m = mono_class_get_method_from_name_flags (ptr_klass, "Box", 2, METHOD_ATTRIBUTE_STATIC);
			g_assert (m);
		}

		v = &ptr;
		if (is_literal) {
			get_default_field_value (domain, field, v);
		} else if (is_static) {
			mono_field_static_get_value (vtable, field, v);
		} else {
			mono_field_get_value (obj, field, v);
		}

		/* MONO_TYPE_PTR is passed by value to runtime_invoke () */
		args [0] = ptr ? *ptr : NULL;
		args [1] = mono_type_get_object (mono_domain_get (), type);

		return mono_runtime_invoke (m, NULL, args, NULL);
	}

	/* boxed value type */
	klass = mono_class_from_mono_type (type);

	if (mono_class_is_nullable (klass))
		return mono_nullable_box (mono_field_get_addr (obj, vtable, field), klass);

	o = mono_object_new (domain, klass);
	v = ((gchar *) o) + sizeof (MonoObject);

	if (is_literal) {
		get_default_field_value (domain, field, v);
	} else if (is_static) {
		mono_field_static_get_value (vtable, field, v);
	} else {
		mono_field_get_value (obj, field, v);
	}

	return o;
}

int
mono_get_constant_value_from_blob (MonoDomain* domain, MonoTypeEnum type, const char *blob, void *value)
{
	MONO_REQ_GC_UNSAFE_MODE;

	int retval = 0;
	const char *p = blob;
	mono_metadata_decode_blob_size (p, &p);

	switch (type) {
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_U1:
	case MONO_TYPE_I1:
		*(guint8 *) value = *p;
		break;
	case MONO_TYPE_CHAR:
	case MONO_TYPE_U2:
	case MONO_TYPE_I2:
		*(guint16*) value = read16 (p);
		break;
	case MONO_TYPE_U4:
	case MONO_TYPE_I4:
		*(guint32*) value = read32 (p);
		break;
	case MONO_TYPE_U8:
	case MONO_TYPE_I8:
		*(guint64*) value = read64 (p);
		break;
	case MONO_TYPE_R4:
		readr4 (p, (float*) value);
		break;
	case MONO_TYPE_R8:
		readr8 (p, (double*) value);
		break;
	case MONO_TYPE_STRING:
		*(gpointer*) value = mono_ldstr_metadata_sig (domain, blob);
		break;
	case MONO_TYPE_CLASS:
		*(gpointer*) value = NULL;
		break;
	default:
		retval = -1;
		g_warning ("type 0x%02x should not be in constant table", type);
	}
	return retval;
}

static void
get_default_field_value (MonoDomain* domain, MonoClassField *field, void *value)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	MonoTypeEnum def_type;
	const char* data;
	
	data = mono_class_get_field_default_value (field, &def_type);
	mono_get_constant_value_from_blob (domain, def_type, data, value);
}

void
mono_field_static_get_value_for_thread (MonoInternalThread *thread, MonoVTable *vt, MonoClassField *field, void *value)
{
	MONO_REQ_GC_UNSAFE_MODE;

	void *src;

	g_return_if_fail (field->type->attrs & FIELD_ATTRIBUTE_STATIC);
	
	if (field->type->attrs & FIELD_ATTRIBUTE_LITERAL) {
		get_default_field_value (vt->domain, field, value);
		return;
	}

	if (field->offset == -1) {
		/* Special static */
		gpointer addr = g_hash_table_lookup (vt->domain->special_static_fields, field);
		src = mono_get_special_static_data_for_thread (thread, GPOINTER_TO_UINT (addr));
	} else {
		src = (char*)mono_vtable_get_static_field_data (vt) + field->offset;
	}
	mono_copy_value (field->type, value, src, TRUE);
}

/**
 * mono_field_static_get_value:
 * @vt: vtable to the object
 * @field: MonoClassField describing the field to fetch information from
 * @value: where the value is returned
 *
 * Use this routine to get the value of the static field @field value.
 *
 * The pointer provided by value must be of the field type, for reference
 * types this is a MonoObject*, for value types its the actual pointer to
 * the value type.
 *
 * For example:
 *     int i;
 *     mono_field_static_get_value (vt, int_field, &i);
 */
void
mono_field_static_get_value (MonoVTable *vt, MonoClassField *field, void *value)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	mono_field_static_get_value_for_thread (mono_thread_internal_current (), vt, field, value);
}

/**
 * mono_property_set_value:
 * @prop: MonoProperty to set
 * @obj: instance object on which to act
 * @params: parameters to pass to the propery
 * @exc: optional exception
 *
 * Invokes the property's set method with the given arguments on the
 * object instance obj (or NULL for static properties). 
 * 
 * You can pass NULL as the exc argument if you don't want to
 * catch exceptions, otherwise, *exc will be set to the exception
 * thrown, if any.  if an exception is thrown, you can't use the
 * MonoObject* result from the function.
 */
void
mono_property_set_value (MonoProperty *prop, void *obj, void **params, MonoObject **exc)
{
	MONO_REQ_GC_UNSAFE_MODE;

	default_mono_runtime_invoke (prop->set, obj, params, exc);
}

/**
 * mono_property_get_value:
 * @prop: MonoProperty to fetch
 * @obj: instance object on which to act
 * @params: parameters to pass to the propery
 * @exc: optional exception
 *
 * Invokes the property's get method with the given arguments on the
 * object instance obj (or NULL for static properties). 
 * 
 * You can pass NULL as the exc argument if you don't want to
 * catch exceptions, otherwise, *exc will be set to the exception
 * thrown, if any.  if an exception is thrown, you can't use the
 * MonoObject* result from the function.
 *
 * Returns: the value from invoking the get method on the property.
 */
MonoObject*
mono_property_get_value (MonoProperty *prop, void *obj, void **params, MonoObject **exc)
{
	MONO_REQ_GC_UNSAFE_MODE;

	return default_mono_runtime_invoke (prop->get, obj, params, exc);
}

/*
 * mono_nullable_init:
 * @buf: The nullable structure to initialize.
 * @value: the value to initialize from
 * @klass: the type for the object
 *
 * Initialize the nullable structure pointed to by @buf from @value which
 * should be a boxed value type.   The size of @buf should be able to hold
 * as much data as the @klass->instance_size (which is the number of bytes
 * that will be copies).
 *
 * Since Nullables have variable structure, we can not define a C
 * structure for them.
 */
void
mono_nullable_init (guint8 *buf, MonoObject *value, MonoClass *klass)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoClass *param_class = klass->cast_class;

	mono_class_setup_fields_locking (klass);
	g_assert (klass->fields_inited);
				
	g_assert (mono_class_from_mono_type (klass->fields [0].type) == param_class);
	g_assert (mono_class_from_mono_type (klass->fields [1].type) == mono_defaults.boolean_class);

	*(guint8*)(buf + klass->fields [1].offset - sizeof (MonoObject)) = value ? 1 : 0;
	if (value) {
		if (param_class->has_references)
			mono_gc_wbarrier_value_copy (buf + klass->fields [0].offset - sizeof (MonoObject), mono_object_unbox (value), 1, param_class);
		else
			mono_gc_memmove_atomic (buf + klass->fields [0].offset - sizeof (MonoObject), mono_object_unbox (value), mono_class_value_size (param_class, NULL));
	} else {
		mono_gc_bzero_atomic (buf + klass->fields [0].offset - sizeof (MonoObject), mono_class_value_size (param_class, NULL));
	}
}

/**
 * mono_nullable_box:
 * @buf: The buffer representing the data to be boxed
 * @klass: the type to box it as.
 *
 * Creates a boxed vtype or NULL from the Nullable structure pointed to by
 * @buf.
 */
MonoObject*
mono_nullable_box (guint8 *buf, MonoClass *klass)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoClass *param_class = klass->cast_class;

	mono_class_setup_fields_locking (klass);
	g_assert (klass->fields_inited);

	g_assert (mono_class_from_mono_type (klass->fields [0].type) == param_class);
	g_assert (mono_class_from_mono_type (klass->fields [1].type) == mono_defaults.boolean_class);

	if (*(guint8*)(buf + klass->fields [1].offset - sizeof (MonoObject))) {
		MonoObject *o = mono_object_new (mono_domain_get (), param_class);
		if (param_class->has_references)
			mono_gc_wbarrier_value_copy (mono_object_unbox (o), buf + klass->fields [0].offset - sizeof (MonoObject), 1, param_class);
		else
			mono_gc_memmove_atomic (mono_object_unbox (o), buf + klass->fields [0].offset - sizeof (MonoObject), mono_class_value_size (param_class, NULL));
		return o;
	}
	else
		return NULL;
}

/**
 * mono_get_delegate_invoke:
 * @klass: The delegate class
 *
 * Returns: the MonoMethod for the "Invoke" method in the delegate klass or NULL if @klass is a broken delegate type
 */
MonoMethod *
mono_get_delegate_invoke (MonoClass *klass)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	MonoMethod *im;

	/* This is called at runtime, so avoid the slower search in metadata */
	mono_class_setup_methods (klass);
	if (klass->exception_type)
		return NULL;
	im = mono_class_get_method_from_name (klass, "Invoke", -1);
	return im;
}

/**
 * mono_get_delegate_begin_invoke:
 * @klass: The delegate class
 *
 * Returns: the MonoMethod for the "BeginInvoke" method in the delegate klass or NULL if @klass is a broken delegate type
 */
MonoMethod *
mono_get_delegate_begin_invoke (MonoClass *klass)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	MonoMethod *im;

	/* This is called at runtime, so avoid the slower search in metadata */
	mono_class_setup_methods (klass);
	if (klass->exception_type)
		return NULL;
	im = mono_class_get_method_from_name (klass, "BeginInvoke", -1);
	return im;
}

/**
 * mono_get_delegate_end_invoke:
 * @klass: The delegate class
 *
 * Returns: the MonoMethod for the "EndInvoke" method in the delegate klass or NULL if @klass is a broken delegate type
 */
MonoMethod *
mono_get_delegate_end_invoke (MonoClass *klass)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	MonoMethod *im;

	/* This is called at runtime, so avoid the slower search in metadata */
	mono_class_setup_methods (klass);
	if (klass->exception_type)
		return NULL;
	im = mono_class_get_method_from_name (klass, "EndInvoke", -1);
	return im;
}

/**
 * mono_runtime_delegate_invoke:
 * @delegate: pointer to a delegate object.
 * @params: parameters for the delegate.
 * @exc: Pointer to the exception result.
 *
 * Invokes the delegate method @delegate with the parameters provided.
 *
 * You can pass NULL as the exc argument if you don't want to
 * catch exceptions, otherwise, *exc will be set to the exception
 * thrown, if any.  if an exception is thrown, you can't use the
 * MonoObject* result from the function.
 */
MonoObject*
mono_runtime_delegate_invoke (MonoObject *delegate, void **params, MonoObject **exc)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoMethod *im;
	MonoClass *klass = delegate->vtable->klass;

	im = mono_get_delegate_invoke (klass);
	if (!im)
		g_error ("Could not lookup delegate invoke method for delegate %s", mono_type_get_full_name (klass));

	return mono_runtime_invoke (im, delegate, params, exc);
}

static char **main_args = NULL;
static int num_main_args = 0;

/**
 * mono_runtime_get_main_args:
 *
 * Returns: a MonoArray with the arguments passed to the main program
 */
MonoArray*
mono_runtime_get_main_args (void)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoArray *res;
	int i;
	MonoDomain *domain = mono_domain_get ();

	res = (MonoArray*)mono_array_new (domain, mono_defaults.string_class, num_main_args);

	for (i = 0; i < num_main_args; ++i)
		mono_array_setref (res, i, mono_string_new (domain, main_args [i]));

	return res;
}

static void
free_main_args (void)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	int i;

	for (i = 0; i < num_main_args; ++i)
		g_free (main_args [i]);
	g_free (main_args);
	num_main_args = 0;
	main_args = NULL;
}

/**
 * mono_runtime_set_main_args:
 * @argc: number of arguments from the command line
 * @argv: array of strings from the command line
 *
 * Set the command line arguments from an embedding application that doesn't otherwise call
 * mono_runtime_run_main ().
 */
int
mono_runtime_set_main_args (int argc, char* argv[])
{
	MONO_REQ_GC_NEUTRAL_MODE;

	int i;

	free_main_args ();
	main_args = g_new0 (char*, argc);
	num_main_args = argc;

	for (i = 0; i < argc; ++i) {
		gchar *utf8_arg;

		utf8_arg = mono_utf8_from_external (argv[i]);
		if (utf8_arg == NULL) {
			g_print ("\nCannot determine the text encoding for argument %d (%s).\n", i, argv [i]);
			g_print ("Please add the correct encoding to MONO_EXTERNAL_ENCODINGS and try again.\n");
			exit (-1);
		}

		main_args [i] = utf8_arg;
	}

	return 0;
}

/**
 * mono_runtime_run_main:
 * @method: the method to start the application with (usually Main)
 * @argc: number of arguments from the command line
 * @argv: array of strings from the command line
 * @exc: excetption results
 *
 * Execute a standard Main() method (argc/argv contains the
 * executable name). This method also sets the command line argument value
 * needed by System.Environment.
 *
 * 
 */
int
mono_runtime_run_main (MonoMethod *method, int argc, char* argv[],
		       MonoObject **exc)
{
	MONO_REQ_GC_UNSAFE_MODE;

	int i;
	MonoArray *args = NULL;
	MonoDomain *domain = mono_domain_get ();
	gchar *utf8_fullpath;
	MonoMethodSignature *sig;

	g_assert (method != NULL);
	
	mono_thread_set_main (mono_thread_current ());

	main_args = g_new0 (char*, argc);
	num_main_args = argc;

	if (!g_path_is_absolute (argv [0])) {
		gchar *basename = g_path_get_basename (argv [0]);
		gchar *fullpath = g_build_filename (method->klass->image->assembly->basedir,
						    basename,
						    NULL);

		utf8_fullpath = mono_utf8_from_external (fullpath);
		if(utf8_fullpath == NULL) {
			/* Printing the arg text will cause glib to
			 * whinge about "Invalid UTF-8", but at least
			 * its relevant, and shows the problem text
			 * string.
			 */
			g_print ("\nCannot determine the text encoding for the assembly location: %s\n", fullpath);
			g_print ("Please add the correct encoding to MONO_EXTERNAL_ENCODINGS and try again.\n");
			exit (-1);
		}

		g_free (fullpath);
		g_free (basename);
	} else {
		utf8_fullpath = mono_utf8_from_external (argv[0]);
		if(utf8_fullpath == NULL) {
			g_print ("\nCannot determine the text encoding for the assembly location: %s\n", argv[0]);
			g_print ("Please add the correct encoding to MONO_EXTERNAL_ENCODINGS and try again.\n");
			exit (-1);
		}
	}

	main_args [0] = utf8_fullpath;

	for (i = 1; i < argc; ++i) {
		gchar *utf8_arg;

		utf8_arg=mono_utf8_from_external (argv[i]);
		if(utf8_arg==NULL) {
			/* Ditto the comment about Invalid UTF-8 here */
			g_print ("\nCannot determine the text encoding for argument %d (%s).\n", i, argv[i]);
			g_print ("Please add the correct encoding to MONO_EXTERNAL_ENCODINGS and try again.\n");
			exit (-1);
		}

		main_args [i] = utf8_arg;
	}
	argc--;
	argv++;

	sig = mono_method_signature (method);
	if (!sig) {
		g_print ("Unable to load Main method.\n");
		exit (-1);
	}

	if (sig->param_count) {
		args = (MonoArray*)mono_array_new (domain, mono_defaults.string_class, argc);
		for (i = 0; i < argc; ++i) {
			/* The encodings should all work, given that
			 * we've checked all these args for the
			 * main_args array.
			 */
			gchar *str = mono_utf8_from_external (argv [i]);
			MonoString *arg = mono_string_new (domain, str);
			mono_array_setref (args, i, arg);
			g_free (str);
		}
	} else {
		args = (MonoArray*)mono_array_new (domain, mono_defaults.string_class, 0);
	}
	
	mono_assembly_set_main (method->klass->image->assembly);

	return mono_runtime_exec_main (method, args, exc);
}

static MonoObject*
serialize_object (MonoObject *obj, gboolean *failure, MonoObject **exc)
{
	static MonoMethod *serialize_method;

	void *params [1];
	MonoObject *array;

	if (!serialize_method) {
		MonoClass *klass = mono_class_from_name (mono_defaults.corlib, "System.Runtime.Remoting", "RemotingServices");
		serialize_method = mono_class_get_method_from_name (klass, "SerializeCallData", -1);
	}

	if (!serialize_method) {
		*failure = TRUE;
		return NULL;
	}

	g_assert (!mono_class_is_marshalbyref (mono_object_class (obj)));

	params [0] = obj;
	*exc = NULL;
	array = mono_runtime_invoke (serialize_method, NULL, params, exc);
	if (*exc)
		*failure = TRUE;

	return array;
}

static MonoObject*
deserialize_object (MonoObject *obj, gboolean *failure, MonoObject **exc)
{
	MONO_REQ_GC_UNSAFE_MODE;

	static MonoMethod *deserialize_method;

	void *params [1];
	MonoObject *result;

	if (!deserialize_method) {
		MonoClass *klass = mono_class_from_name (mono_defaults.corlib, "System.Runtime.Remoting", "RemotingServices");
		deserialize_method = mono_class_get_method_from_name (klass, "DeserializeCallData", -1);
	}
	if (!deserialize_method) {
		*failure = TRUE;
		return NULL;
	}

	params [0] = obj;
	*exc = NULL;
	result = mono_runtime_invoke (deserialize_method, NULL, params, exc);
	if (*exc)
		*failure = TRUE;

	return result;
}

#ifndef DISABLE_REMOTING
static MonoObject*
make_transparent_proxy (MonoObject *obj, gboolean *failure, MonoObject **exc)
{
	MONO_REQ_GC_UNSAFE_MODE;

	static MonoMethod *get_proxy_method;

	MonoDomain *domain = mono_domain_get ();
	MonoRealProxy *real_proxy;
	MonoReflectionType *reflection_type;
	MonoTransparentProxy *transparent_proxy;

	if (!get_proxy_method)
		get_proxy_method = mono_class_get_method_from_name (mono_defaults.real_proxy_class, "GetTransparentProxy", 0);

	g_assert (mono_class_is_marshalbyref (obj->vtable->klass));

	real_proxy = (MonoRealProxy*) mono_object_new (domain, mono_defaults.real_proxy_class);
	reflection_type = mono_type_get_object (domain, &obj->vtable->klass->byval_arg);

	MONO_OBJECT_SETREF (real_proxy, class_to_proxy, reflection_type);
	MONO_OBJECT_SETREF (real_proxy, unwrapped_server, obj);

	*exc = NULL;
	transparent_proxy = (MonoTransparentProxy*) mono_runtime_invoke (get_proxy_method, real_proxy, NULL, exc);
	if (*exc)
		*failure = TRUE;

	return (MonoObject*) transparent_proxy;
}
#endif /* DISABLE_REMOTING */

/**
 * mono_object_xdomain_representation
 * @obj: an object
 * @target_domain: a domain
 * @exc: pointer to a MonoObject*
 *
 * Creates a representation of obj in the domain target_domain.  This
 * is either a copy of obj arrived through via serialization and
 * deserialization or a proxy, depending on whether the object is
 * serializable or marshal by ref.  obj must not be in target_domain.
 *
 * If the object cannot be represented in target_domain, NULL is
 * returned and *exc is set to an appropriate exception.
 */
MonoObject*
mono_object_xdomain_representation (MonoObject *obj, MonoDomain *target_domain, MonoObject **exc)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoObject *deserialized = NULL;
	gboolean failure = FALSE;

	*exc = NULL;

#ifndef DISABLE_REMOTING
	if (mono_class_is_marshalbyref (mono_object_class (obj))) {
		deserialized = make_transparent_proxy (obj, &failure, exc);
	} 
	else
#endif
	{
		MonoDomain *domain = mono_domain_get ();
		MonoObject *serialized;

		mono_domain_set_internal_with_options (mono_object_domain (obj), FALSE);
		serialized = serialize_object (obj, &failure, exc);
		mono_domain_set_internal_with_options (target_domain, FALSE);
		if (!failure)
			deserialized = deserialize_object (serialized, &failure, exc);
		if (domain != target_domain)
			mono_domain_set_internal_with_options (domain, FALSE);
	}

	return deserialized;
}

/* Used in call_unhandled_exception_delegate */
static MonoObject *
create_unhandled_exception_eventargs (MonoObject *exc)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoClass *klass;
	gpointer args [2];
	MonoMethod *method = NULL;
	MonoBoolean is_terminating = TRUE;
	MonoObject *obj;

	klass = mono_class_from_name (mono_defaults.corlib, "System", "UnhandledExceptionEventArgs");
	g_assert (klass);

	mono_class_init (klass);

	/* UnhandledExceptionEventArgs only has 1 public ctor with 2 args */
	method = mono_class_get_method_from_name_flags (klass, ".ctor", 2, METHOD_ATTRIBUTE_PUBLIC);
	g_assert (method);

	args [0] = exc;
	args [1] = &is_terminating;

	obj = mono_object_new (mono_domain_get (), klass);
	mono_runtime_invoke (method, obj, args, NULL);

	return obj;
}

/* Used in mono_unhandled_exception */
static void
call_unhandled_exception_delegate (MonoDomain *domain, MonoObject *delegate, MonoObject *exc) {
	MONO_REQ_GC_UNSAFE_MODE;

	MonoObject *e = NULL;
	gpointer pa [2];
	MonoDomain *current_domain = mono_domain_get ();

	if (domain != current_domain)
		mono_domain_set_internal_with_options (domain, FALSE);

	g_assert (domain == mono_object_domain (domain->domain));

	if (mono_object_domain (exc) != domain) {
		MonoObject *serialization_exc;

		exc = mono_object_xdomain_representation (exc, domain, &serialization_exc);
		if (!exc) {
			if (serialization_exc) {
				MonoObject *dummy;
				exc = mono_object_xdomain_representation (serialization_exc, domain, &dummy);
				g_assert (exc);
			} else {
				exc = (MonoObject*) mono_exception_from_name_msg (mono_get_corlib (),
						"System.Runtime.Serialization", "SerializationException",
						"Could not serialize unhandled exception.");
			}
		}
	}
	g_assert (mono_object_domain (exc) == domain);

	pa [0] = domain->domain;
	pa [1] = create_unhandled_exception_eventargs (exc);
	mono_runtime_delegate_invoke (delegate, pa, &e);

	if (domain != current_domain)
		mono_domain_set_internal_with_options (current_domain, FALSE);

	if (e) {
		MonoError error;
		gchar *msg = mono_string_to_utf8_checked (((MonoException *) e)->message, &error);
		if (!mono_error_ok (&error)) {
			g_warning ("Exception inside UnhandledException handler with invalid message (Invalid characters)\n");
			mono_error_cleanup (&error);
		} else {
			g_warning ("exception inside UnhandledException handler: %s\n", msg);
			g_free (msg);
		}
	}
}

static MonoRuntimeUnhandledExceptionPolicy runtime_unhandled_exception_policy = MONO_UNHANDLED_POLICY_CURRENT;

/**
 * mono_runtime_unhandled_exception_policy_set:
 * @policy: the new policy
 * 
 * This is a VM internal routine.
 *
 * Sets the runtime policy for handling unhandled exceptions.
 */
void
mono_runtime_unhandled_exception_policy_set (MonoRuntimeUnhandledExceptionPolicy policy) {
	runtime_unhandled_exception_policy = policy;
}

/**
 * mono_runtime_unhandled_exception_policy_get:
 *
 * This is a VM internal routine.
 *
 * Gets the runtime policy for handling unhandled exceptions.
 */
MonoRuntimeUnhandledExceptionPolicy
mono_runtime_unhandled_exception_policy_get (void) {
	return runtime_unhandled_exception_policy;
}

/**
 * mono_unhandled_exception:
 * @exc: exception thrown
 *
 * This is a VM internal routine.
 *
 * We call this function when we detect an unhandled exception
 * in the default domain.
 *
 * It invokes the * UnhandledException event in AppDomain or prints
 * a warning to the console 
 */
void
mono_unhandled_exception (MonoObject *exc)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoDomain *current_domain = mono_domain_get ();
	MonoDomain *root_domain = mono_get_root_domain ();
	MonoClassField *field;
	MonoObject *current_appdomain_delegate;
	MonoObject *root_appdomain_delegate;

	field=mono_class_get_field_from_name(mono_defaults.appdomain_class, 
					     "UnhandledException");
	g_assert (field);

	if (exc->vtable->klass != mono_defaults.threadabortexception_class) {
		gboolean abort_process = (main_thread && (mono_thread_internal_current () == main_thread->internal_thread)) ||
				(mono_runtime_unhandled_exception_policy_get () == MONO_UNHANDLED_POLICY_CURRENT);
		root_appdomain_delegate = *(MonoObject **)(((char *)root_domain->domain) + field->offset);
		if (current_domain != root_domain) {
			current_appdomain_delegate = *(MonoObject **)(((char *)current_domain->domain) + field->offset);
		} else {
			current_appdomain_delegate = NULL;
		}

		/* set exitcode only if we will abort the process */
		if ((current_appdomain_delegate == NULL) && (root_appdomain_delegate == NULL)) {
			if (abort_process)
				mono_environment_exitcode_set (1);
			mono_print_unhandled_exception (exc);
		} else {
			if (root_appdomain_delegate) {
				call_unhandled_exception_delegate (root_domain, root_appdomain_delegate, exc);
			}
			if (current_appdomain_delegate) {
				call_unhandled_exception_delegate (current_domain, current_appdomain_delegate, exc);
			}
		}
	}
}

/**
 * mono_runtime_exec_managed_code:
 * @domain: Application domain
 * @main_func: function to invoke from the execution thread
 * @main_args: parameter to the main_func
 *
 * Launch a new thread to execute a function
 *
 * main_func is called back from the thread with main_args as the
 * parameter.  The callback function is expected to start Main()
 * eventually.  This function then waits for all managed threads to
 * finish.
 * It is not necesseray anymore to execute managed code in a subthread,
 * so this function should not be used anymore by default: just
 * execute the code and then call mono_thread_manage ().
 */
void
mono_runtime_exec_managed_code (MonoDomain *domain,
				MonoMainThreadFunc main_func,
				gpointer main_args)
{
	mono_thread_create (domain, main_func, main_args);

	mono_thread_manage ();
}

/*
 * Execute a standard Main() method (args doesn't contain the
 * executable name).
 */
int
mono_runtime_exec_main (MonoMethod *method, MonoArray *args, MonoObject **exc)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoDomain *domain;
	gpointer pa [1];
	int rval;
	MonoCustomAttrInfo* cinfo;
	gboolean has_stathread_attribute;
	MonoInternalThread* thread = mono_thread_internal_current ();

	g_assert (args);

	pa [0] = args;

	domain = mono_object_domain (args);
	if (!domain->entry_assembly) {
		gchar *str;
		MonoAssembly *assembly;

		assembly = method->klass->image->assembly;
		domain->entry_assembly = assembly;
		/* Domains created from another domain already have application_base and configuration_file set */
		if (domain->setup->application_base == NULL) {
			MONO_OBJECT_SETREF (domain->setup, application_base, mono_string_new (domain, assembly->basedir));
		}

		if (domain->setup->configuration_file == NULL) {
			str = g_strconcat (assembly->image->name, ".config", NULL);
			MONO_OBJECT_SETREF (domain->setup, configuration_file, mono_string_new (domain, str));
			g_free (str);
			mono_set_private_bin_path_from_config (domain);
		}
	}

	cinfo = mono_custom_attrs_from_method (method);
	if (cinfo) {
		static MonoClass *stathread_attribute = NULL;
		if (!stathread_attribute)
			stathread_attribute = mono_class_from_name (mono_defaults.corlib, "System", "STAThreadAttribute");
		has_stathread_attribute = mono_custom_attrs_has_attr (cinfo, stathread_attribute);
		if (!cinfo->cached)
			mono_custom_attrs_free (cinfo);
	} else {
		has_stathread_attribute = FALSE;
 	}
	if (has_stathread_attribute) {
		thread->apartment_state = ThreadApartmentState_STA;
	} else {
		thread->apartment_state = ThreadApartmentState_MTA;
	}
	mono_thread_init_apartment_state ();

	/* FIXME: check signature of method */
	if (mono_method_signature (method)->ret->type == MONO_TYPE_I4) {
		MonoObject *res;
		res = mono_runtime_invoke (method, NULL, pa, exc);
		if (!exc || !*exc)
			rval = *(guint32 *)((char *)res + sizeof (MonoObject));
		else
			rval = -1;

		mono_environment_exitcode_set (rval);
	} else {
		mono_runtime_invoke (method, NULL, pa, exc);
		if (!exc || !*exc)
			rval = 0;
		else {
			/* If the return type of Main is void, only
			 * set the exitcode if an exception was thrown
			 * (we don't want to blow away an
			 * explicitly-set exit code)
			 */
			rval = -1;
			mono_environment_exitcode_set (rval);
		}
	}

	return rval;
}

/**
 * mono_install_runtime_invoke:
 * @func: Function to install
 *
 * This is a VM internal routine
 */
void
mono_install_runtime_invoke (MonoInvokeFunc func)
{
	default_mono_runtime_invoke = func ? func: dummy_mono_runtime_invoke;
}


/**
 * mono_runtime_invoke_array:
 * @method: method to invoke
 * @obJ: object instance
 * @params: arguments to the method
 * @exc: exception information.
 *
 * Invokes the method represented by @method on the object @obj.
 *
 * obj is the 'this' pointer, it should be NULL for static
 * methods, a MonoObject* for object instances and a pointer to
 * the value type for value types.
 *
 * The params array contains the arguments to the method with the
 * same convention: MonoObject* pointers for object instances and
 * pointers to the value type otherwise. The _invoke_array
 * variant takes a C# object[] as the params argument (MonoArray
 * *params): in this case the value types are boxed inside the
 * respective reference representation.
 * 
 * From unmanaged code you'll usually use the
 * mono_runtime_invoke() variant.
 *
 * Note that this function doesn't handle virtual methods for
 * you, it will exec the exact method you pass: we still need to
 * expose a function to lookup the derived class implementation
 * of a virtual method (there are examples of this in the code,
 * though).
 * 
 * You can pass NULL as the exc argument if you don't want to
 * catch exceptions, otherwise, *exc will be set to the exception
 * thrown, if any.  if an exception is thrown, you can't use the
 * MonoObject* result from the function.
 * 
 * If the method returns a value type, it is boxed in an object
 * reference.
 */
MonoObject*
mono_runtime_invoke_array (MonoMethod *method, void *obj, MonoArray *params,
			   MonoObject **exc)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoMethodSignature *sig = mono_method_signature (method);
	gpointer *pa = NULL;
	MonoObject *res;
	int i;
	gboolean has_byref_nullables = FALSE;

	if (NULL != params) {
		pa = alloca (sizeof (gpointer) * mono_array_length (params));
		for (i = 0; i < mono_array_length (params); i++) {
			MonoType *t = sig->params [i];

		again:
			switch (t->type) {
			case MONO_TYPE_U1:
			case MONO_TYPE_I1:
			case MONO_TYPE_BOOLEAN:
			case MONO_TYPE_U2:
			case MONO_TYPE_I2:
			case MONO_TYPE_CHAR:
			case MONO_TYPE_U:
			case MONO_TYPE_I:
			case MONO_TYPE_U4:
			case MONO_TYPE_I4:
			case MONO_TYPE_U8:
			case MONO_TYPE_I8:
			case MONO_TYPE_R4:
			case MONO_TYPE_R8:
			case MONO_TYPE_VALUETYPE:
				if (t->type == MONO_TYPE_VALUETYPE && mono_class_is_nullable (mono_class_from_mono_type (sig->params [i]))) {
					/* The runtime invoke wrapper needs the original boxed vtype, it does handle byref values as well. */
					pa [i] = mono_array_get (params, MonoObject*, i);
					if (t->byref)
						has_byref_nullables = TRUE;
				} else {
					/* MS seems to create the objects if a null is passed in */
					if (!mono_array_get (params, MonoObject*, i))
						mono_array_setref (params, i, mono_object_new (mono_domain_get (), mono_class_from_mono_type (sig->params [i]))); 

					if (t->byref) {
						/*
						 * We can't pass the unboxed vtype byref to the callee, since
						 * that would mean the callee would be able to modify boxed
						 * primitive types. So we (and MS) make a copy of the boxed
						 * object, pass that to the callee, and replace the original
						 * boxed object in the arg array with the copy.
						 */
						MonoObject *orig = mono_array_get (params, MonoObject*, i);
						MonoObject *copy = mono_value_box (mono_domain_get (), orig->vtable->klass, mono_object_unbox (orig));
						mono_array_setref (params, i, copy);
					}
						
					pa [i] = mono_object_unbox (mono_array_get (params, MonoObject*, i));
				}
				break;
			case MONO_TYPE_STRING:
			case MONO_TYPE_OBJECT:
			case MONO_TYPE_CLASS:
			case MONO_TYPE_ARRAY:
			case MONO_TYPE_SZARRAY:
				if (t->byref)
					pa [i] = mono_array_addr (params, MonoObject*, i);
					// FIXME: I need to check this code path
				else
					pa [i] = mono_array_get (params, MonoObject*, i);
				break;
			case MONO_TYPE_GENERICINST:
				if (t->byref)
					t = &t->data.generic_class->container_class->this_arg;
				else
					t = &t->data.generic_class->container_class->byval_arg;
				goto again;
			case MONO_TYPE_PTR: {
				MonoObject *arg;

				/* The argument should be an IntPtr */
				arg = mono_array_get (params, MonoObject*, i);
				if (arg == NULL) {
					pa [i] = NULL;
				} else {
					g_assert (arg->vtable->klass == mono_defaults.int_class);
					pa [i] = ((MonoIntPtr*)arg)->m_value;
				}
				break;
			}
			default:
				g_error ("type 0x%x not handled in mono_runtime_invoke_array", sig->params [i]->type);
			}
		}
	}

	if (!strcmp (method->name, ".ctor") && method->klass != mono_defaults.string_class) {
		void *o = obj;

		if (mono_class_is_nullable (method->klass)) {
			/* Need to create a boxed vtype instead */
			g_assert (!obj);

			if (!params)
				return NULL;
			else
				return mono_value_box (mono_domain_get (), method->klass->cast_class, pa [0]);
		}

		if (!obj) {
			obj = mono_object_new (mono_domain_get (), method->klass);
			g_assert (obj); /*maybe we should raise a TLE instead?*/
#ifndef DISABLE_REMOTING
			if (mono_object_class(obj) == mono_defaults.transparent_proxy_class) {
				method = mono_marshal_get_remoting_invoke (method->slot == -1 ? method : method->klass->vtable [method->slot]);
			}
#endif
			if (method->klass->valuetype)
				o = mono_object_unbox (obj);
			else
				o = obj;
		} else if (method->klass->valuetype) {
			obj = mono_value_box (mono_domain_get (), method->klass, obj);
		}

		mono_runtime_invoke (method, o, pa, exc);
		return obj;
	} else {
		if (mono_class_is_nullable (method->klass)) {
			MonoObject *nullable;

			/* Convert the unboxed vtype into a Nullable structure */
			nullable = mono_object_new (mono_domain_get (), method->klass);

			mono_nullable_init (mono_object_unbox (nullable), mono_value_box (mono_domain_get (), method->klass->cast_class, obj), method->klass);
			obj = mono_object_unbox (nullable);
		}

		/* obj must be already unboxed if needed */
		res = mono_runtime_invoke (method, obj, pa, exc);

		if (sig->ret->type == MONO_TYPE_PTR) {
			MonoClass *pointer_class;
			static MonoMethod *box_method;
			void *box_args [2];
			MonoObject *box_exc;

			/* 
			 * The runtime-invoke wrapper returns a boxed IntPtr, need to 
			 * convert it to a Pointer object.
			 */
			pointer_class = mono_class_from_name_cached (mono_defaults.corlib, "System.Reflection", "Pointer");
			if (!box_method)
				box_method = mono_class_get_method_from_name (pointer_class, "Box", -1);

			g_assert (res->vtable->klass == mono_defaults.int_class);
			box_args [0] = ((MonoIntPtr*)res)->m_value;
			box_args [1] = mono_type_get_object (mono_domain_get (), sig->ret);
			res = mono_runtime_invoke (box_method, NULL, box_args, &box_exc);
			g_assert (!box_exc);
		}

		if (has_byref_nullables) {
			/* 
			 * The runtime invoke wrapper already converted byref nullables back,
			 * and stored them in pa, we just need to copy them back to the
			 * managed array.
			 */
			for (i = 0; i < mono_array_length (params); i++) {
				MonoType *t = sig->params [i];

				if (t->byref && t->type == MONO_TYPE_GENERICINST && mono_class_is_nullable (mono_class_from_mono_type (t)))
					mono_array_setref (params, i, pa [i]);
			}
		}

		return res;
	}
}

static void
arith_overflow (void)
{
	MONO_REQ_GC_UNSAFE_MODE;

	mono_raise_exception (mono_get_exception_overflow ());
}

/**
 * mono_object_new:
 * @klass: the class of the object that we want to create
 *
 * Returns: a newly created object whose definition is
 * looked up using @klass.   This will not invoke any constructors, 
 * so the consumer of this routine has to invoke any constructors on
 * its own to initialize the object.
 * 
 * It returns NULL on failure.
 */
MonoObject *
mono_object_new (MonoDomain *domain, MonoClass *klass)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoVTable *vtable;

	vtable = mono_class_vtable (domain, klass);
	if (!vtable)
		return NULL;
	return mono_object_new_specific (vtable);
}

/**
 * mono_object_new_pinned:
 *
 *   Same as mono_object_new, but the returned object will be pinned.
 * For SGEN, these objects will only be freed at appdomain unload.
 */
MonoObject *
mono_object_new_pinned (MonoDomain *domain, MonoClass *klass)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoVTable *vtable;

	vtable = mono_class_vtable (domain, klass);
	if (!vtable)
		return NULL;

#ifdef HAVE_SGEN_GC
	return mono_gc_alloc_pinned_obj (vtable, mono_class_instance_size (klass));
#else
	return mono_object_new_specific (vtable);
#endif
}

/**
 * mono_object_new_specific:
 * @vtable: the vtable of the object that we want to create
 *
 * Returns: A newly created object with class and domain specified
 * by @vtable
 */
MonoObject *
mono_object_new_specific (MonoVTable *vtable)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoObject *o;

	/* check for is_com_object for COM Interop */
	if (mono_vtable_is_remote (vtable) || mono_class_is_com_object (vtable->klass))
	{
		gpointer pa [1];
		MonoMethod *im = vtable->domain->create_proxy_for_type_method;

		if (im == NULL) {
			MonoClass *klass = mono_class_from_name (mono_defaults.corlib, "System.Runtime.Remoting.Activation", "ActivationServices");

			if (!klass->inited)
				mono_class_init (klass);

			im = mono_class_get_method_from_name (klass, "CreateProxyForType", 1);
			if (!im)
				mono_raise_exception (mono_get_exception_not_supported ("Linked away."));
			vtable->domain->create_proxy_for_type_method = im;
		}
	
		pa [0] = mono_type_get_object (mono_domain_get (), &vtable->klass->byval_arg);

		o = mono_runtime_invoke (im, NULL, pa, NULL);		
		if (o != NULL) return o;
	}

	return mono_object_new_alloc_specific (vtable);
}

MonoObject *
mono_object_new_alloc_specific (MonoVTable *vtable)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoObject *o = mono_gc_alloc_obj (vtable, vtable->klass->instance_size);

	if (G_UNLIKELY (vtable->klass->has_finalize))
		mono_object_register_finalizer (o);

	return o;
}

MonoObject*
mono_object_new_fast (MonoVTable *vtable)
{
	MONO_REQ_GC_UNSAFE_MODE;

	return mono_gc_alloc_obj (vtable, vtable->klass->instance_size);
}

/**
 * mono_class_get_allocation_ftn:
 * @vtable: vtable
 * @for_box: the object will be used for boxing
 * @pass_size_in_words: 
 *
 * Return the allocation function appropriate for the given class.
 */

void*
mono_class_get_allocation_ftn (MonoVTable *vtable, gboolean for_box, gboolean *pass_size_in_words)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	*pass_size_in_words = FALSE;

	if (mono_class_has_finalizer (vtable->klass) || mono_class_is_marshalbyref (vtable->klass) || (mono_profiler_get_events () & MONO_PROFILE_ALLOCATIONS))
		return mono_object_new_specific;

	if (vtable->gc_descr != MONO_GC_DESCRIPTOR_NULL) {

		return mono_object_new_fast;

		/* 
		 * FIXME: This is actually slower than mono_object_new_fast, because
		 * of the overhead of parameter passing.
		 */
		/*
		*pass_size_in_words = TRUE;
#ifdef GC_REDIRECT_TO_LOCAL
		return GC_local_gcj_fast_malloc;
#else
		return GC_gcj_fast_malloc;
#endif
		*/
	}

	return mono_object_new_specific;
}

/**
 * mono_object_new_from_token:
 * @image: Context where the type_token is hosted
 * @token: a token of the type that we want to create
 *
 * Returns: A newly created object whose definition is
 * looked up using @token in the @image image
 */
MonoObject *
mono_object_new_from_token  (MonoDomain *domain, MonoImage *image, guint32 token)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoError error;
	MonoClass *class;

	class = mono_class_get_checked (image, token, &error);
	g_assert (mono_error_ok (&error)); /* FIXME don't swallow the error */

	return mono_object_new (domain, class);
}


/**
 * mono_object_clone:
 * @obj: the object to clone
 *
 * Returns: A newly created object who is a shallow copy of @obj
 */
MonoObject *
mono_object_clone (MonoObject *obj)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoObject *o;
	int size = obj->vtable->klass->instance_size;

	if (obj->vtable->klass->rank)
		return (MonoObject*)mono_array_clone ((MonoArray*)obj);

	o = mono_gc_alloc_obj (obj->vtable, size);

	/* If the object doesn't contain references this will do a simple memmove. */
	mono_gc_wbarrier_object_copy (o, obj);

	if (obj->vtable->klass->has_finalize)
		mono_object_register_finalizer (o);
	return o;
}

/**
 * mono_array_full_copy:
 * @src: source array to copy
 * @dest: destination array
 *
 * Copies the content of one array to another with exactly the same type and size.
 */
void
mono_array_full_copy (MonoArray *src, MonoArray *dest)
{
	MONO_REQ_GC_UNSAFE_MODE;

	uintptr_t size;
	MonoClass *klass = src->obj.vtable->klass;

	g_assert (klass == dest->obj.vtable->klass);

	size = mono_array_length (src);
	g_assert (size == mono_array_length (dest));
	size *= mono_array_element_size (klass);
#ifdef HAVE_SGEN_GC
	if (klass->element_class->valuetype) {
		if (klass->element_class->has_references)
			mono_value_copy_array (dest, 0, mono_array_addr_with_size_fast (src, 0, 0), mono_array_length (src));
		else
			mono_gc_memmove_atomic (&dest->vector, &src->vector, size);
	} else {
		mono_array_memcpy_refs (dest, 0, src, 0, mono_array_length (src));
	}
#else
	mono_gc_memmove_atomic (&dest->vector, &src->vector, size);
#endif
}

/**
 * mono_array_clone_in_domain:
 * @domain: the domain in which the array will be cloned into
 * @array: the array to clone
 *
 * This routine returns a copy of the array that is hosted on the
 * specified MonoDomain.
 */
MonoArray*
mono_array_clone_in_domain (MonoDomain *domain, MonoArray *array)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoArray *o;
	uintptr_t size, i;
	uintptr_t *sizes;
	MonoClass *klass = array->obj.vtable->klass;

	if (array->bounds == NULL) {
		size = mono_array_length (array);
		o = mono_array_new_full (domain, klass, &size, NULL);

		size *= mono_array_element_size (klass);
#ifdef HAVE_SGEN_GC
		if (klass->element_class->valuetype) {
			if (klass->element_class->has_references)
				mono_value_copy_array (o, 0, mono_array_addr_with_size_fast (array, 0, 0), mono_array_length (array));
			else
				mono_gc_memmove_atomic (&o->vector, &array->vector, size);
		} else {
			mono_array_memcpy_refs (o, 0, array, 0, mono_array_length (array));
		}
#else
		mono_gc_memmove_atomic (&o->vector, &array->vector, size);
#endif
		return o;
	}
	
	sizes = alloca (klass->rank * sizeof(intptr_t) * 2);
	size = mono_array_element_size (klass);
	for (i = 0; i < klass->rank; ++i) {
		sizes [i] = array->bounds [i].length;
		size *= array->bounds [i].length;
		sizes [i + klass->rank] = array->bounds [i].lower_bound;
	}
	o = mono_array_new_full (domain, klass, sizes, (intptr_t*)sizes + klass->rank);
#ifdef HAVE_SGEN_GC
	if (klass->element_class->valuetype) {
		if (klass->element_class->has_references)
			mono_value_copy_array (o, 0, mono_array_addr_with_size_fast (array, 0, 0), mono_array_length (array));
		else
			mono_gc_memmove_atomic (&o->vector, &array->vector, size);
	} else {
		mono_array_memcpy_refs (o, 0, array, 0, mono_array_length (array));
	}
#else
	mono_gc_memmove_atomic (&o->vector, &array->vector, size);
#endif

	return o;
}

/**
 * mono_array_clone:
 * @array: the array to clone
 *
 * Returns: A newly created array who is a shallow copy of @array
 */
MonoArray*
mono_array_clone (MonoArray *array)
{
	MONO_REQ_GC_UNSAFE_MODE;

	return mono_array_clone_in_domain (((MonoObject *)array)->vtable->domain, array);
}

/* helper macros to check for overflow when calculating the size of arrays */
#ifdef MONO_BIG_ARRAYS
#define MYGUINT64_MAX 0x0000FFFFFFFFFFFFUL
#define MYGUINT_MAX MYGUINT64_MAX
#define CHECK_ADD_OVERFLOW_UN(a,b) \
	    (G_UNLIKELY ((guint64)(MYGUINT64_MAX) - (guint64)(b) < (guint64)(a)))
#define CHECK_MUL_OVERFLOW_UN(a,b) \
	    (G_UNLIKELY (((guint64)(a) > 0) && ((guint64)(b) > 0) &&	\
					 ((guint64)(b) > ((MYGUINT64_MAX) / (guint64)(a)))))
#else
#define MYGUINT32_MAX 4294967295U
#define MYGUINT_MAX MYGUINT32_MAX
#define CHECK_ADD_OVERFLOW_UN(a,b) \
	    (G_UNLIKELY ((guint32)(MYGUINT32_MAX) - (guint32)(b) < (guint32)(a)))
#define CHECK_MUL_OVERFLOW_UN(a,b) \
	    (G_UNLIKELY (((guint32)(a) > 0) && ((guint32)(b) > 0) &&			\
					 ((guint32)(b) > ((MYGUINT32_MAX) / (guint32)(a)))))
#endif

gboolean
mono_array_calc_byte_len (MonoClass *class, uintptr_t len, uintptr_t *res)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	uintptr_t byte_len;

	byte_len = mono_array_element_size (class);
	if (CHECK_MUL_OVERFLOW_UN (byte_len, len))
		return FALSE;
	byte_len *= len;
	if (CHECK_ADD_OVERFLOW_UN (byte_len, sizeof (MonoArray)))
		return FALSE;
	byte_len += sizeof (MonoArray);

	*res = byte_len;

	return TRUE;
}

/**
 * mono_array_new_full:
 * @domain: domain where the object is created
 * @array_class: array class
 * @lengths: lengths for each dimension in the array
 * @lower_bounds: lower bounds for each dimension in the array (may be NULL)
 *
 * This routine creates a new array objects with the given dimensions,
 * lower bounds and type.
 */
MonoArray*
mono_array_new_full (MonoDomain *domain, MonoClass *array_class, uintptr_t *lengths, intptr_t *lower_bounds)
{
	MONO_REQ_GC_UNSAFE_MODE;

	uintptr_t byte_len = 0, len, bounds_size;
	MonoObject *o;
	MonoArray *array;
	MonoArrayBounds *bounds;
	MonoVTable *vtable;
	int i;

	if (!array_class->inited)
		mono_class_init (array_class);

	len = 1;

	/* A single dimensional array with a 0 lower bound is the same as an szarray */
	if (array_class->rank == 1 && ((array_class->byval_arg.type == MONO_TYPE_SZARRAY) || (lower_bounds && lower_bounds [0] == 0))) {
		len = lengths [0];
		if (len > MONO_ARRAY_MAX_INDEX)//MONO_ARRAY_MAX_INDEX
			arith_overflow ();
		bounds_size = 0;
	} else {
		bounds_size = sizeof (MonoArrayBounds) * array_class->rank;

		for (i = 0; i < array_class->rank; ++i) {
			if (lengths [i] > MONO_ARRAY_MAX_INDEX) //MONO_ARRAY_MAX_INDEX
				arith_overflow ();
			if (CHECK_MUL_OVERFLOW_UN (len, lengths [i]))
				mono_gc_out_of_memory (MONO_ARRAY_MAX_SIZE);
			len *= lengths [i];
		}
	}

	if (!mono_array_calc_byte_len (array_class, len, &byte_len))
		mono_gc_out_of_memory (MONO_ARRAY_MAX_SIZE);

	if (bounds_size) {
		/* align */
		if (CHECK_ADD_OVERFLOW_UN (byte_len, 3))
			mono_gc_out_of_memory (MONO_ARRAY_MAX_SIZE);
		byte_len = (byte_len + 3) & ~3;
		if (CHECK_ADD_OVERFLOW_UN (byte_len, bounds_size))
			mono_gc_out_of_memory (MONO_ARRAY_MAX_SIZE);
		byte_len += bounds_size;
	}
	/* 
	 * Following three lines almost taken from mono_object_new ():
	 * they need to be kept in sync.
	 */
	vtable = mono_class_vtable_full (domain, array_class, TRUE);
	if (bounds_size)
		o = mono_gc_alloc_array (vtable, byte_len, len, bounds_size);
	else
		o = mono_gc_alloc_vector (vtable, byte_len, len);
	array = (MonoArray*)o;

	bounds = array->bounds;

	if (bounds_size) {
		for (i = 0; i < array_class->rank; ++i) {
			bounds [i].length = lengths [i];
			if (lower_bounds)
				bounds [i].lower_bound = lower_bounds [i];
		}
	}

	return array;
}

/**
 * mono_array_new:
 * @domain: domain where the object is created
 * @eclass: element class
 * @n: number of array elements
 *
 * This routine creates a new szarray with @n elements of type @eclass.
 */
MonoArray *
mono_array_new (MonoDomain *domain, MonoClass *eclass, uintptr_t n)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoClass *ac;

	ac = mono_array_class_get (eclass, 1);
	g_assert (ac);

	return mono_array_new_specific (mono_class_vtable_full (domain, ac, TRUE), n);
}

/**
 * mono_array_new_specific:
 * @vtable: a vtable in the appropriate domain for an initialized class
 * @n: number of array elements
 *
 * This routine is a fast alternative to mono_array_new() for code which
 * can be sure about the domain it operates in.
 */
MonoArray *
mono_array_new_specific (MonoVTable *vtable, uintptr_t n)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoObject *o;
	MonoArray *ao;
	uintptr_t byte_len;

	if (G_UNLIKELY (n > MONO_ARRAY_MAX_INDEX)) {
		arith_overflow ();
		return NULL;
	}

	if (!mono_array_calc_byte_len (vtable->klass, n, &byte_len)) {
		mono_gc_out_of_memory (MONO_ARRAY_MAX_SIZE);
		return NULL;
	}
	o = mono_gc_alloc_vector (vtable, byte_len, n);
	ao = (MonoArray*)o;

	return ao;
}

/**
 * mono_string_new_utf16:
 * @text: a pointer to an utf16 string
 * @len: the length of the string
 *
 * Returns: A newly created string object which contains @text.
 */
MonoString *
mono_string_new_utf16 (MonoDomain *domain, const guint16 *text, gint32 len)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoString *s;
	
	s = mono_string_new_size (domain, len);
	g_assert (s != NULL);

	memcpy (mono_string_chars (s), text, len * 2);

	return s;
}

/**
 * mono_string_new_utf32:
 * @text: a pointer to an utf32 string
 * @len: the length of the string
 *
 * Returns: A newly created string object which contains @text.
 */
MonoString *
mono_string_new_utf32 (MonoDomain *domain, const mono_unichar4 *text, gint32 len)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoString *s;
	mono_unichar2 *utf16_output = NULL;
	gint32 utf16_len = 0;
	GError *error = NULL;
	glong items_written;
	
	utf16_output = g_ucs4_to_utf16 (text, len, NULL, &items_written, &error);
	
	if (error)
		g_error_free (error);

	while (utf16_output [utf16_len]) utf16_len++;
	
	s = mono_string_new_size (domain, utf16_len);
	g_assert (s != NULL);

	memcpy (mono_string_chars (s), utf16_output, utf16_len * 2);

	g_free (utf16_output);
	
	return s;
}

/**
 * mono_string_new_size:
 * @text: a pointer to an utf16 string
 * @len: the length of the string
 *
 * Returns: A newly created string object of @len
 */
MonoString *
mono_string_new_size (MonoDomain *domain, gint32 len)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoString *s;
	MonoVTable *vtable;
	size_t size;

	/* check for overflow */
	if (len < 0 || len > ((SIZE_MAX - G_STRUCT_OFFSET (MonoString, chars) - 8) / 2))
		mono_gc_out_of_memory (-1);

	size = (G_STRUCT_OFFSET (MonoString, chars) + (((size_t)len + 1) * 2));
	g_assert (size > 0);

	vtable = mono_class_vtable (domain, mono_defaults.string_class);
	g_assert (vtable);

	s = mono_gc_alloc_string (vtable, size, len);

	return s;
}

/**
 * mono_string_new_len:
 * @text: a pointer to an utf8 string
 * @length: number of bytes in @text to consider
 *
 * Returns: A newly created string object which contains @text.
 */
MonoString*
mono_string_new_len (MonoDomain *domain, const char *text, guint length)
{
	MONO_REQ_GC_UNSAFE_MODE;

	GError *error = NULL;
	MonoString *o = NULL;
	guint16 *ut;
	glong items_written;

	ut = eg_utf8_to_utf16_with_nuls (text, length, NULL, &items_written, &error);

	if (!error)
		o = mono_string_new_utf16 (domain, ut, items_written);
	else 
		g_error_free (error);

	g_free (ut);

	return o;
}

/**
 * mono_string_new:
 * @text: a pointer to an utf8 string
 *
 * Returns: A newly created string object which contains @text.
 */
MonoString*
mono_string_new (MonoDomain *domain, const char *text)
{
	MONO_REQ_GC_UNSAFE_MODE;

    GError *error = NULL;
    MonoString *o = NULL;
    guint16 *ut;
    glong items_written;
    int l;

    l = strlen (text);
   
    ut = g_utf8_to_utf16 (text, l, NULL, &items_written, &error);

    if (!error)
        o = mono_string_new_utf16 (domain, ut, items_written);
    else
        g_error_free (error);

    g_free (ut);
/*FIXME g_utf8_get_char, g_utf8_next_char and g_utf8_validate are not part of eglib.*/
#if 0
	gunichar2 *str;
	const gchar *end;
	int len;
	MonoString *o = NULL;

	if (!g_utf8_validate (text, -1, &end))
		return NULL;

	len = g_utf8_strlen (text, -1);
	o = mono_string_new_size (domain, len);
	str = mono_string_chars (o);

	while (text < end) {
		*str++ = g_utf8_get_char (text);
		text = g_utf8_next_char (text);
	}
#endif
	return o;
}

/**
 * mono_string_new_wrapper:
 * @text: pointer to utf8 characters.
 *
 * Helper function to create a string object from @text in the current domain.
 */
MonoString*
mono_string_new_wrapper (const char *text)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoDomain *domain = mono_domain_get ();

	if (text)
		return mono_string_new (domain, text);

	return NULL;
}

/**
 * mono_value_box:
 * @class: the class of the value
 * @value: a pointer to the unboxed data
 *
 * Returns: A newly created object which contains @value.
 */
MonoObject *
mono_value_box (MonoDomain *domain, MonoClass *class, gpointer value)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoObject *res;
	int size;
	MonoVTable *vtable;

	g_assert (class->valuetype);
	if (mono_class_is_nullable (class))
		return mono_nullable_box (value, class);

	vtable = mono_class_vtable (domain, class);
	if (!vtable)
		return NULL;
	size = mono_class_instance_size (class);
	res = mono_object_new_alloc_specific (vtable);

	size = size - sizeof (MonoObject);

#ifdef HAVE_SGEN_GC
	g_assert (size == mono_class_value_size (class, NULL));
	mono_gc_wbarrier_value_copy ((char *)res + sizeof (MonoObject), value, 1, class);
#else
#if NO_UNALIGNED_ACCESS
	mono_gc_memmove_atomic ((char *)res + sizeof (MonoObject), value, size);
#else
	switch (size) {
	case 1:
		*((guint8 *) res + sizeof (MonoObject)) = *(guint8 *) value;
		break;
	case 2:
		*(guint16 *)((guint8 *) res + sizeof (MonoObject)) = *(guint16 *) value;
		break;
	case 4:
		*(guint32 *)((guint8 *) res + sizeof (MonoObject)) = *(guint32 *) value;
		break;
	case 8:
		*(guint64 *)((guint8 *) res + sizeof (MonoObject)) = *(guint64 *) value;
		break;
	default:
		mono_gc_memmove_atomic ((char *)res + sizeof (MonoObject), value, size);
	}
#endif
#endif
	if (class->has_finalize)
		mono_object_register_finalizer (res);
	return res;
}

/*
 * mono_value_copy:
 * @dest: destination pointer
 * @src: source pointer
 * @klass: a valuetype class
 *
 * Copy a valuetype from @src to @dest. This function must be used
 * when @klass contains references fields.
 */
void
mono_value_copy (gpointer dest, gpointer src, MonoClass *klass)
{
	MONO_REQ_GC_UNSAFE_MODE;

	mono_gc_wbarrier_value_copy (dest, src, 1, klass);
}

/*
 * mono_value_copy_array:
 * @dest: destination array
 * @dest_idx: index in the @dest array
 * @src: source pointer
 * @count: number of items
 *
 * Copy @count valuetype items from @src to the array @dest at index @dest_idx. 
 * This function must be used when @klass contains references fields.
 * Overlap is handled.
 */
void
mono_value_copy_array (MonoArray *dest, int dest_idx, gpointer src, int count)
{
	MONO_REQ_GC_UNSAFE_MODE;

	int size = mono_array_element_size (dest->obj.vtable->klass);
	char *d = mono_array_addr_with_size_fast (dest, size, dest_idx);
	g_assert (size == mono_class_value_size (mono_object_class (dest)->element_class, NULL));
	mono_gc_wbarrier_value_copy (d, src, count, mono_object_class (dest)->element_class);
}

/**
 * mono_object_get_domain:
 * @obj: object to query
 * 
 * Returns: the MonoDomain where the object is hosted
 */
MonoDomain*
mono_object_get_domain (MonoObject *obj)
{
	MONO_REQ_GC_UNSAFE_MODE;

	return mono_object_domain (obj);
}

/**
 * mono_object_get_class:
 * @obj: object to query
 * 
 * Returns: the MonOClass of the object.
 */
MonoClass*
mono_object_get_class (MonoObject *obj)
{
	MONO_REQ_GC_UNSAFE_MODE;

	return mono_object_class (obj);
}
/**
 * mono_object_get_size:
 * @o: object to query
 * 
 * Returns: the size, in bytes, of @o
 */
guint
mono_object_get_size (MonoObject* o)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoClass* klass = mono_object_class (o);
	if (klass == mono_defaults.string_class) {
		return sizeof (MonoString) + 2 * mono_string_length ((MonoString*) o) + 2;
	} else if (o->vtable->rank) {
		MonoArray *array = (MonoArray*)o;
		size_t size = sizeof (MonoArray) + mono_array_element_size (klass) * mono_array_length (array);
		if (array->bounds) {
			size += 3;
			size &= ~3;
			size += sizeof (MonoArrayBounds) * o->vtable->rank;
		}
		return size;
	} else {
		return mono_class_instance_size (klass);
	}
}

/**
 * mono_object_unbox:
 * @obj: object to unbox
 * 
 * Returns: a pointer to the start of the valuetype boxed in this
 * object.
 *
 * This method will assert if the object passed is not a valuetype.
 */
gpointer
mono_object_unbox (MonoObject *obj)
{
	MONO_REQ_GC_UNSAFE_MODE;

	/* add assert for valuetypes? */
	g_assert (obj->vtable->klass->valuetype);
	return ((char*)obj) + sizeof (MonoObject);
}

/**
 * mono_object_isinst:
 * @obj: an object
 * @klass: a pointer to a class 
 *
 * Returns: @obj if @obj is derived from @klass
 */
MonoObject *
mono_object_isinst (MonoObject *obj, MonoClass *klass)
{
	MONO_REQ_GC_UNSAFE_MODE;

	if (!klass->inited)
		mono_class_init (klass);

	if (mono_class_is_marshalbyref (klass) || (klass->flags & TYPE_ATTRIBUTE_INTERFACE))
		return mono_object_isinst_mbyref (obj, klass);

	if (!obj)
		return NULL;

	return mono_class_is_assignable_from (klass, obj->vtable->klass) ? obj : NULL;
}

MonoObject *
mono_object_isinst_mbyref (MonoObject *obj, MonoClass *klass)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoVTable *vt;

	if (!obj)
		return NULL;

	vt = obj->vtable;
	
	if (klass->flags & TYPE_ATTRIBUTE_INTERFACE) {
		if (MONO_VTABLE_IMPLEMENTS_INTERFACE (vt, klass->interface_id)) {
			return obj;
		}

		/*If the above check fails we are in the slow path of possibly raising an exception. So it's ok to it this way.*/
		if (mono_class_has_variant_generic_params (klass) && mono_class_is_assignable_from (klass, obj->vtable->klass))
			return obj;
	} else {
		MonoClass *oklass = vt->klass;
		if (mono_class_is_transparent_proxy (oklass))
			oklass = ((MonoTransparentProxy *)obj)->remote_class->proxy_class;

		mono_class_setup_supertypes (klass);	
		if ((oklass->idepth >= klass->idepth) && (oklass->supertypes [klass->idepth - 1] == klass))
			return obj;
	}
#ifndef DISABLE_REMOTING
	if (vt->klass == mono_defaults.transparent_proxy_class && ((MonoTransparentProxy *)obj)->custom_type_info) 
	{
		MonoDomain *domain = mono_domain_get ();
		MonoObject *res;
		MonoObject *rp = (MonoObject *)((MonoTransparentProxy *)obj)->rp;
		MonoClass *rpklass = mono_defaults.iremotingtypeinfo_class;
		MonoMethod *im = NULL;
		gpointer pa [2];

		im = mono_class_get_method_from_name (rpklass, "CanCastTo", -1);
		if (!im)
			mono_raise_exception (mono_get_exception_not_supported ("Linked away."));
		im = mono_object_get_virtual_method (rp, im);
		g_assert (im);
	
		pa [0] = mono_type_get_object (domain, &klass->byval_arg);
		pa [1] = obj;

		res = mono_runtime_invoke (im, rp, pa, NULL);
	
		if (*(MonoBoolean *) mono_object_unbox(res)) {
			/* Update the vtable of the remote type, so it can safely cast to this new type */
			mono_upgrade_remote_class (domain, obj, klass);
			return obj;
		}
	}
#endif /* DISABLE_REMOTING */
	return NULL;
}

/**
 * mono_object_castclass_mbyref:
 * @obj: an object
 * @klass: a pointer to a class 
 *
 * Returns: @obj if @obj is derived from @klass, throws an exception otherwise
 */
MonoObject *
mono_object_castclass_mbyref (MonoObject *obj, MonoClass *klass)
{
	MONO_REQ_GC_UNSAFE_MODE;

	if (!obj) return NULL;
	if (mono_object_isinst_mbyref (obj, klass)) return obj;
		
	mono_raise_exception (mono_exception_from_name (mono_defaults.corlib,
							"System",
							"InvalidCastException"));
	return NULL;
}

typedef struct {
	MonoDomain *orig_domain;
	MonoString *ins;
	MonoString *res;
} LDStrInfo;

static void
str_lookup (MonoDomain *domain, gpointer user_data)
{
	MONO_REQ_GC_UNSAFE_MODE;

	LDStrInfo *info = user_data;
	if (info->res || domain == info->orig_domain)
		return;
	info->res = mono_g_hash_table_lookup (domain->ldstr_table, info->ins);
}

#ifdef HAVE_SGEN_GC

static MonoString*
mono_string_get_pinned (MonoString *str)
{
	MONO_REQ_GC_UNSAFE_MODE;

	int size;
	MonoString *news;
	size = sizeof (MonoString) + 2 * (mono_string_length (str) + 1);
	news = mono_gc_alloc_pinned_obj (((MonoObject*)str)->vtable, size);
	if (news) {
		memcpy (mono_string_chars (news), mono_string_chars (str), mono_string_length (str) * 2);
		news->length = mono_string_length (str);
	}
	return news;
}

#else
#define mono_string_get_pinned(str) (str)
#endif

static MonoString*
mono_string_is_interned_lookup (MonoString *str, int insert)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoGHashTable *ldstr_table;
	MonoString *s, *res;
	MonoDomain *domain;
	
	domain = ((MonoObject *)str)->vtable->domain;
	ldstr_table = domain->ldstr_table;
	ldstr_lock ();
	res = mono_g_hash_table_lookup (ldstr_table, str);
	if (res) {
		ldstr_unlock ();
		return res;
	}
	if (insert) {
		/* Allocate outside the lock */
		ldstr_unlock ();
		s = mono_string_get_pinned (str);
		if (s) {
			ldstr_lock ();
			res = mono_g_hash_table_lookup (ldstr_table, str);
			if (res) {
				ldstr_unlock ();
				return res;
			}
			mono_g_hash_table_insert (ldstr_table, s, s);
			ldstr_unlock ();
		}
		return s;
	} else {
		LDStrInfo ldstr_info;
		ldstr_info.orig_domain = domain;
		ldstr_info.ins = str;
		ldstr_info.res = NULL;

		mono_domain_foreach (str_lookup, &ldstr_info);
		if (ldstr_info.res) {
			/* 
			 * the string was already interned in some other domain:
			 * intern it in the current one as well.
			 */
			mono_g_hash_table_insert (ldstr_table, str, str);
			ldstr_unlock ();
			return str;
		}
	}
	ldstr_unlock ();
	return NULL;
}

/**
 * mono_string_is_interned:
 * @o: String to probe
 *
 * Returns whether the string has been interned.
 */
MonoString*
mono_string_is_interned (MonoString *o)
{
	MONO_REQ_GC_UNSAFE_MODE;

	return mono_string_is_interned_lookup (o, FALSE);
}

/**
 * mono_string_intern:
 * @o: String to intern
 *
 * Interns the string passed.  
 * Returns: The interned string.
 */
MonoString*
mono_string_intern (MonoString *str)
{
	MONO_REQ_GC_UNSAFE_MODE;

	return mono_string_is_interned_lookup (str, TRUE);
}

/**
 * mono_ldstr:
 * @domain: the domain where the string will be used.
 * @image: a metadata context
 * @idx: index into the user string table.
 * 
 * Implementation for the ldstr opcode.
 * Returns: a loaded string from the @image/@idx combination.
 */
MonoString*
mono_ldstr (MonoDomain *domain, MonoImage *image, guint32 idx)
{
	MONO_REQ_GC_UNSAFE_MODE;

	if (image->dynamic) {
		MonoString *str = mono_lookup_dynamic_token (image, MONO_TOKEN_STRING | idx, NULL);
		return str;
	} else {
		if (!mono_verifier_verify_string_signature (image, idx, NULL))
			return NULL; /*FIXME we should probably be raising an exception here*/
		return mono_ldstr_metadata_sig (domain, mono_metadata_user_string (image, idx));
	}
}

/**
 * mono_ldstr_metadata_sig
 * @domain: the domain for the string
 * @sig: the signature of a metadata string
 *
 * Returns: a MonoString for a string stored in the metadata
 */
static MonoString*
mono_ldstr_metadata_sig (MonoDomain *domain, const char* sig)
{
	MONO_REQ_GC_UNSAFE_MODE;

	const char *str = sig;
	MonoString *o, *interned;
	size_t len2;

	len2 = mono_metadata_decode_blob_size (str, &str);
	len2 >>= 1;

	o = mono_string_new_utf16 (domain, (guint16*)str, len2);
#if G_BYTE_ORDER != G_LITTLE_ENDIAN
	{
		int i;
		guint16 *p2 = (guint16*)mono_string_chars (o);
		for (i = 0; i < len2; ++i) {
			*p2 = GUINT16_FROM_LE (*p2);
			++p2;
		}
	}
#endif
	ldstr_lock ();
	interned = mono_g_hash_table_lookup (domain->ldstr_table, o);
	ldstr_unlock ();
	if (interned)
		return interned; /* o will get garbage collected */

	o = mono_string_get_pinned (o);
	if (o) {
		ldstr_lock ();
		interned = mono_g_hash_table_lookup (domain->ldstr_table, o);
		if (!interned) {
			mono_g_hash_table_insert (domain->ldstr_table, o, o);
			interned = o;
		}
		ldstr_unlock ();
	}

	return interned;
}

/**
 * mono_string_to_utf8:
 * @s: a System.String
 *
 * Returns the UTF8 representation for @s.
 * The resulting buffer needs to be freed with mono_free().
 *
 * @deprecated Use mono_string_to_utf8_checked to avoid having an exception arbritraly raised.
 */
char *
mono_string_to_utf8 (MonoString *s)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoError error;
	char *result = mono_string_to_utf8_checked (s, &error);
	
	if (!mono_error_ok (&error))
		mono_error_raise_exception (&error);
	return result;
}

/**
 * mono_string_to_utf8_checked:
 * @s: a System.String
 * @error: a MonoError.
 * 
 * Converts a MonoString to its UTF8 representation. May fail; check 
 * @error to determine whether the conversion was successful.
 * The resulting buffer should be freed with mono_free().
 */
char *
mono_string_to_utf8_checked (MonoString *s, MonoError *error)
{
	MONO_REQ_GC_UNSAFE_MODE;

	long written = 0;
	char *as;
	GError *gerror = NULL;

	mono_error_init (error);

	if (s == NULL)
		return NULL;

	if (!s->length)
		return g_strdup ("");

	as = g_utf16_to_utf8 (mono_string_chars (s), s->length, NULL, &written, &gerror);
	if (gerror) {
		mono_error_set_argument (error, "string", "%s", gerror->message);
		g_error_free (gerror);
		return NULL;
	}
	/* g_utf16_to_utf8  may not be able to complete the convertion (e.g. NULL values were found, #335488) */
	if (s->length > written) {
		/* allocate the total length and copy the part of the string that has been converted */
		char *as2 = g_malloc0 (s->length);
		memcpy (as2, as, written);
		g_free (as);
		as = as2;
	}

	return as;
}

/**
 * mono_string_to_utf8_ignore:
 * @s: a MonoString
 *
 * Converts a MonoString to its UTF8 representation. Will ignore
 * invalid surrogate pairs.
 * The resulting buffer should be freed with mono_free().
 * 
 */
char *
mono_string_to_utf8_ignore (MonoString *s)
{
	MONO_REQ_GC_UNSAFE_MODE;

	long written = 0;
	char *as;

	if (s == NULL)
		return NULL;

	if (!s->length)
		return g_strdup ("");

	as = g_utf16_to_utf8 (mono_string_chars (s), s->length, NULL, &written, NULL);

	/* g_utf16_to_utf8  may not be able to complete the convertion (e.g. NULL values were found, #335488) */
	if (s->length > written) {
		/* allocate the total length and copy the part of the string that has been converted */
		char *as2 = g_malloc0 (s->length);
		memcpy (as2, as, written);
		g_free (as);
		as = as2;
	}

	return as;
}

/**
 * mono_string_to_utf8_image_ignore:
 * @s: a System.String
 *
 * Same as mono_string_to_utf8_ignore, but allocate the string from the image mempool.
 */
char *
mono_string_to_utf8_image_ignore (MonoImage *image, MonoString *s)
{
	MONO_REQ_GC_UNSAFE_MODE;

	return mono_string_to_utf8_internal (NULL, image, s, TRUE, NULL);
}

/**
 * mono_string_to_utf8_mp_ignore:
 * @s: a System.String
 *
 * Same as mono_string_to_utf8_ignore, but allocate the string from a mempool.
 */
char *
mono_string_to_utf8_mp_ignore (MonoMemPool *mp, MonoString *s)
{
	MONO_REQ_GC_UNSAFE_MODE;

	return mono_string_to_utf8_internal (mp, NULL, s, TRUE, NULL);
}


/**
 * mono_string_to_utf16:
 * @s: a MonoString
 *
 * Return an null-terminated array of the utf-16 chars
 * contained in @s. The result must be freed with g_free().
 * This is a temporary helper until our string implementation
 * is reworked to always include the null terminating char.
 */
mono_unichar2*
mono_string_to_utf16 (MonoString *s)
{
	MONO_REQ_GC_UNSAFE_MODE;

	char *as;

	if (s == NULL)
		return NULL;

	as = g_malloc ((s->length * 2) + 2);
	as [(s->length * 2)] = '\0';
	as [(s->length * 2) + 1] = '\0';

	if (!s->length) {
		return (gunichar2 *)(as);
	}
	
	memcpy (as, mono_string_chars(s), s->length * 2);
	return (gunichar2 *)(as);
}

/**
 * mono_string_to_utf32:
 * @s: a MonoString
 *
 * Return an null-terminated array of the UTF-32 (UCS-4) chars
 * contained in @s. The result must be freed with g_free().
 */
mono_unichar4*
mono_string_to_utf32 (MonoString *s)
{
	MONO_REQ_GC_UNSAFE_MODE;

	mono_unichar4 *utf32_output = NULL; 
	GError *error = NULL;
	glong items_written;
	
	if (s == NULL)
		return NULL;
		
	utf32_output = g_utf16_to_ucs4 (s->chars, s->length, NULL, &items_written, &error);
	
	if (error)
		g_error_free (error);

	return utf32_output;
}

/**
 * mono_string_from_utf16:
 * @data: the UTF16 string (LPWSTR) to convert
 *
 * Converts a NULL terminated UTF16 string (LPWSTR) to a MonoString.
 *
 * Returns: a MonoString.
 */
MonoString *
mono_string_from_utf16 (gunichar2 *data)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoDomain *domain = mono_domain_get ();
	int len = 0;

	if (!data)
		return NULL;

	while (data [len]) len++;

	return mono_string_new_utf16 (domain, data, len);
}

/**
 * mono_string_from_utf32:
 * @data: the UTF32 string (LPWSTR) to convert
 *
 * Converts a UTF32 (UCS-4)to a MonoString.
 *
 * Returns: a MonoString.
 */
MonoString *
mono_string_from_utf32 (mono_unichar4 *data)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoString* result = NULL;
	mono_unichar2 *utf16_output = NULL;
	GError *error = NULL;
	glong items_written;
	int len = 0;

	if (!data)
		return NULL;

	while (data [len]) len++;

	utf16_output = g_ucs4_to_utf16 (data, len, NULL, &items_written, &error);

	if (error)
		g_error_free (error);

	result = mono_string_from_utf16 (utf16_output);
	g_free (utf16_output);
	return result;
}

static char *
mono_string_to_utf8_internal (MonoMemPool *mp, MonoImage *image, MonoString *s, gboolean ignore_error, MonoError *error)
{
	MONO_REQ_GC_UNSAFE_MODE;

	char *r;
	char *mp_s;
	int len;

	if (ignore_error) {
		r = mono_string_to_utf8_ignore (s);
	} else {
		r = mono_string_to_utf8_checked (s, error);
		if (!mono_error_ok (error))
			return NULL;
	}

	if (!mp && !image)
		return r;

	len = strlen (r) + 1;
	if (mp)
		mp_s = mono_mempool_alloc (mp, len);
	else
		mp_s = mono_image_alloc (image, len);

	memcpy (mp_s, r, len);

	g_free (r);

	return mp_s;
}

/**
 * mono_string_to_utf8_image:
 * @s: a System.String
 *
 * Same as mono_string_to_utf8, but allocate the string from the image mempool.
 */
char *
mono_string_to_utf8_image (MonoImage *image, MonoString *s, MonoError *error)
{
	MONO_REQ_GC_UNSAFE_MODE;

	return mono_string_to_utf8_internal (NULL, image, s, FALSE, error);
}

/**
 * mono_string_to_utf8_mp:
 * @s: a System.String
 *
 * Same as mono_string_to_utf8, but allocate the string from a mempool.
 */
char *
mono_string_to_utf8_mp (MonoMemPool *mp, MonoString *s, MonoError *error)
{
	MONO_REQ_GC_UNSAFE_MODE;

	return mono_string_to_utf8_internal (mp, NULL, s, FALSE, error);
}


static MonoRuntimeExceptionHandlingCallbacks eh_callbacks;

void
mono_install_eh_callbacks (MonoRuntimeExceptionHandlingCallbacks *cbs)
{
	eh_callbacks = *cbs;
}

MonoRuntimeExceptionHandlingCallbacks *
mono_get_eh_callbacks (void)
{
	return &eh_callbacks;
}

/**
 * mono_raise_exception:
 * @ex: exception object
 *
 * Signal the runtime that the exception @ex has been raised in unmanaged code.
 */
void
mono_raise_exception (MonoException *ex) 
{
	MONO_REQ_GC_UNSAFE_MODE;

	/*
	 * NOTE: Do NOT annotate this function with G_GNUC_NORETURN, since
	 * that will cause gcc to omit the function epilog, causing problems when
	 * the JIT tries to walk the stack, since the return address on the stack
	 * will point into the next function in the executable, not this one.
	 */	
	eh_callbacks.mono_raise_exception (ex);
}

void
mono_raise_exception_with_context (MonoException *ex, MonoContext *ctx) 
{
	MONO_REQ_GC_UNSAFE_MODE;

	eh_callbacks.mono_raise_exception_with_ctx (ex, ctx);
}

/**
 * mono_wait_handle_new:
 * @domain: Domain where the object will be created
 * @handle: Handle for the wait handle
 *
 * Returns: A new MonoWaitHandle created in the given domain for the given handle
 */
MonoWaitHandle *
mono_wait_handle_new (MonoDomain *domain, HANDLE handle)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoWaitHandle *res;
	gpointer params [1];
	static MonoMethod *handle_set;

	res = (MonoWaitHandle *)mono_object_new (domain, mono_defaults.manualresetevent_class);

	/* Even though this method is virtual, it's safe to invoke directly, since the object type matches.  */
	if (!handle_set)
		handle_set = mono_class_get_property_from_name (mono_defaults.manualresetevent_class, "Handle")->set;

	params [0] = &handle;
	mono_runtime_invoke (handle_set, res, params, NULL);

	return res;
}

HANDLE
mono_wait_handle_get_handle (MonoWaitHandle *handle)
{
	MONO_REQ_GC_UNSAFE_MODE;

	static MonoClassField *f_os_handle;
	static MonoClassField *f_safe_handle;

	if (!f_os_handle && !f_safe_handle) {
		f_os_handle = mono_class_get_field_from_name (mono_defaults.manualresetevent_class, "os_handle");
		f_safe_handle = mono_class_get_field_from_name (mono_defaults.manualresetevent_class, "safe_wait_handle");
	}

	if (f_os_handle) {
		HANDLE retval;
		mono_field_get_value ((MonoObject*)handle, f_os_handle, &retval);
		return retval;
	} else {
		MonoSafeHandle *sh;
		mono_field_get_value ((MonoObject*)handle, f_safe_handle, &sh);
		return sh->handle;
	}
}


static MonoObject*
mono_runtime_capture_context (MonoDomain *domain)
{
	MONO_REQ_GC_UNSAFE_MODE;

	RuntimeInvokeFunction runtime_invoke;

	if (!domain->capture_context_runtime_invoke || !domain->capture_context_method) {
		MonoMethod *method = mono_get_context_capture_method ();
		MonoMethod *wrapper;
		if (!method)
			return NULL;
		wrapper = mono_marshal_get_runtime_invoke (method, FALSE);
		domain->capture_context_runtime_invoke = mono_compile_method (wrapper);
		domain->capture_context_method = mono_compile_method (method);
	}

	runtime_invoke = domain->capture_context_runtime_invoke;

	return runtime_invoke (NULL, NULL, NULL, domain->capture_context_method);
}
/**
 * mono_async_result_new:
 * @domain:domain where the object will be created.
 * @handle: wait handle.
 * @state: state to pass to AsyncResult
 * @data: C closure data.
 *
 * Creates a new MonoAsyncResult (AsyncResult C# class) in the given domain.
 * If the handle is not null, the handle is initialized to a MonOWaitHandle.
 *
 */
MonoAsyncResult *
mono_async_result_new (MonoDomain *domain, HANDLE handle, MonoObject *state, gpointer data, MonoObject *object_data)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoAsyncResult *res = (MonoAsyncResult *)mono_object_new (domain, mono_defaults.asyncresult_class);
	MonoObject *context = mono_runtime_capture_context (domain);
	/* we must capture the execution context from the original thread */
	if (context) {
		MONO_OBJECT_SETREF (res, execution_context, context);
		/* note: result may be null if the flow is suppressed */
	}

	res->data = data;
	MONO_OBJECT_SETREF (res, object_data, object_data);
	MONO_OBJECT_SETREF (res, async_state, state);
	if (handle != NULL)
		MONO_OBJECT_SETREF (res, handle, (MonoObject *) mono_wait_handle_new (domain, handle));

	res->sync_completed = FALSE;
	res->completed = FALSE;

	return res;
}

MonoObject *
ves_icall_System_Runtime_Remoting_Messaging_AsyncResult_Invoke (MonoAsyncResult *ares)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoAsyncCall *ac;
	MonoObject *res;

	g_assert (ares);
	g_assert (ares->async_delegate);

	ac = (MonoAsyncCall*) ares->object_data;
	if (!ac) {
		res = mono_runtime_delegate_invoke (ares->async_delegate, (void**) &ares->async_state, NULL);
	} else {
		gpointer wait_event = NULL;

		ac->msg->exc = NULL;
		res = mono_message_invoke (ares->async_delegate, ac->msg, &ac->msg->exc, &ac->out_args);
		MONO_OBJECT_SETREF (ac, res, res);

		mono_monitor_enter ((MonoObject*) ares);
		ares->completed = 1;
		if (ares->handle)
			wait_event = mono_wait_handle_get_handle ((MonoWaitHandle*) ares->handle);
		mono_monitor_exit ((MonoObject*) ares);

		if (wait_event != NULL)
			SetEvent (wait_event);

		if (ac->cb_method) {
			/* we swallow the excepton as it is the behavior on .NET */
			MonoObject *exc = NULL;
			mono_runtime_invoke (ac->cb_method, ac->cb_target, (gpointer*) &ares, &exc);
			if (exc)
				mono_unhandled_exception (exc);
		}
	}

	return res;
}

void
mono_message_init (MonoDomain *domain,
		   MonoMethodMessage *this_obj, 
		   MonoReflectionMethod *method,
		   MonoArray *out_args)
{
	MONO_REQ_GC_UNSAFE_MODE;

	static MonoClass *object_array_klass;
	static MonoClass *byte_array_klass;
	static MonoClass *string_array_klass;
	MonoMethodSignature *sig = mono_method_signature (method->method);
	MonoString *name;
	int i, j;
	char **names;
	guint8 arg_type;

	if (!object_array_klass) {
		MonoClass *klass;

		klass = mono_array_class_get (mono_defaults.byte_class, 1);
		g_assert (klass);
		byte_array_klass = klass;

		klass = mono_array_class_get (mono_defaults.string_class, 1);
		g_assert (klass);
		string_array_klass = klass;

		klass = mono_array_class_get (mono_defaults.object_class, 1);
		g_assert (klass);

		mono_atomic_store_release (&object_array_klass, klass);
	}

	MONO_OBJECT_SETREF (this_obj, method, method);

	MONO_OBJECT_SETREF (this_obj, args, mono_array_new_specific (mono_class_vtable (domain, object_array_klass), sig->param_count));
	MONO_OBJECT_SETREF (this_obj, arg_types, mono_array_new_specific (mono_class_vtable (domain, byte_array_klass), sig->param_count));
	this_obj->async_result = NULL;
	this_obj->call_type = CallType_Sync;

	names = g_new (char *, sig->param_count);
	mono_method_get_param_names (method->method, (const char **) names);
	MONO_OBJECT_SETREF (this_obj, names, mono_array_new_specific (mono_class_vtable (domain, string_array_klass), sig->param_count));
	
	for (i = 0; i < sig->param_count; i++) {
		name = mono_string_new (domain, names [i]);
		mono_array_setref (this_obj->names, i, name);	
	}

	g_free (names);
	for (i = 0, j = 0; i < sig->param_count; i++) {
		if (sig->params [i]->byref) {
			if (out_args) {
				MonoObject* arg = mono_array_get (out_args, gpointer, j);
				mono_array_setref (this_obj->args, i, arg);
				j++;
			}
			arg_type = 2;
			if (!(sig->params [i]->attrs & PARAM_ATTRIBUTE_OUT))
				arg_type |= 1;
		} else {
			arg_type = 1;
			if (sig->params [i]->attrs & PARAM_ATTRIBUTE_OUT)
				arg_type |= 4;
		}
		mono_array_set (this_obj->arg_types, guint8, i, arg_type);
	}
}

#ifndef DISABLE_REMOTING
/**
 * mono_remoting_invoke:
 * @real_proxy: pointer to a RealProxy object
 * @msg: The MonoMethodMessage to execute
 * @exc: used to store exceptions
 * @out_args: used to store output arguments
 *
 * This is used to call RealProxy::Invoke(). RealProxy::Invoke() returns an
 * IMessage interface and it is not trivial to extract results from there. So
 * we call an helper method PrivateInvoke instead of calling
 * RealProxy::Invoke() directly.
 *
 * Returns: the result object.
 */
MonoObject *
mono_remoting_invoke (MonoObject *real_proxy, MonoMethodMessage *msg, 
		      MonoObject **exc, MonoArray **out_args)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoMethod *im = real_proxy->vtable->domain->private_invoke_method;
	gpointer pa [4];

	/*static MonoObject *(*invoke) (gpointer, gpointer, MonoObject **, MonoArray **) = NULL;*/

	if (!im) {
		im = mono_class_get_method_from_name (mono_defaults.real_proxy_class, "PrivateInvoke", 4);
		if (!im)
			mono_raise_exception (mono_get_exception_not_supported ("Linked away."));
		real_proxy->vtable->domain->private_invoke_method = im;
	}

	pa [0] = real_proxy;
	pa [1] = msg;
	pa [2] = exc;
	pa [3] = out_args;

	return mono_runtime_invoke (im, NULL, pa, exc);
}
#endif

MonoObject *
mono_message_invoke (MonoObject *target, MonoMethodMessage *msg, 
		     MonoObject **exc, MonoArray **out_args) 
{
	MONO_REQ_GC_UNSAFE_MODE;

	static MonoClass *object_array_klass;
	MonoDomain *domain; 
	MonoMethod *method;
	MonoMethodSignature *sig;
	MonoObject *ret;
	int i, j, outarg_count = 0;

#ifndef DISABLE_REMOTING
	if (target && mono_object_is_transparent_proxy (target)) {
		MonoTransparentProxy* tp = (MonoTransparentProxy *)target;
		if (mono_class_is_contextbound (tp->remote_class->proxy_class) && tp->rp->context == (MonoObject *) mono_context_get ()) {
			target = tp->rp->unwrapped_server;
		} else {
			return mono_remoting_invoke ((MonoObject *)tp->rp, msg, exc, out_args);
		}
	}
#endif

	domain = mono_domain_get (); 
	method = msg->method->method;
	sig = mono_method_signature (method);

	for (i = 0; i < sig->param_count; i++) {
		if (sig->params [i]->byref) 
			outarg_count++;
	}

	if (!object_array_klass) {
		MonoClass *klass;

		klass = mono_array_class_get (mono_defaults.object_class, 1);
		g_assert (klass);

		mono_memory_barrier ();
		object_array_klass = klass;
	}

	mono_gc_wbarrier_generic_store (out_args, (MonoObject*) mono_array_new_specific (mono_class_vtable (domain, object_array_klass), outarg_count));
	*exc = NULL;

	ret = mono_runtime_invoke_array (method, method->klass->valuetype? mono_object_unbox (target): target, msg->args, exc);

	for (i = 0, j = 0; i < sig->param_count; i++) {
		if (sig->params [i]->byref) {
			MonoObject* arg;
			arg = mono_array_get (msg->args, gpointer, i);
			mono_array_setref (*out_args, j, arg);
			j++;
		}
	}

	return ret;
}

/**
 * mono_object_to_string:
 * @obj: The object
 * @exc: Any exception thrown by ToString (). May be NULL.
 *
 * Returns: the result of calling ToString () on an object.
 */
MonoString *
mono_object_to_string (MonoObject *obj, MonoObject **exc)
{
	MONO_REQ_GC_UNSAFE_MODE;

	static MonoMethod *to_string = NULL;
	MonoMethod *method;
	void *target = obj;

	g_assert (obj);

	if (!to_string)
		to_string = mono_class_get_method_from_name_flags (mono_get_object_class (), "ToString", 0, METHOD_ATTRIBUTE_VIRTUAL | METHOD_ATTRIBUTE_PUBLIC);

	method = mono_object_get_virtual_method (obj, to_string);

	// Unbox value type if needed
	if (mono_class_is_valuetype (mono_method_get_class (method))) {
		target = mono_object_unbox (obj);
	}

	return (MonoString *) mono_runtime_invoke (method, target, NULL, exc);
}

/**
 * mono_print_unhandled_exception:
 * @exc: The exception
 *
 * Prints the unhandled exception.
 */
void
mono_print_unhandled_exception (MonoObject *exc)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoString * str;
	char *message = (char*)"";
	gboolean free_message = FALSE;
	MonoError error;

	if (exc == (MonoObject*)mono_object_domain (exc)->out_of_memory_ex) {
		message = g_strdup ("OutOfMemoryException");
		free_message = TRUE;
	} else if (exc == (MonoObject*)mono_object_domain (exc)->stack_overflow_ex) {
		message = g_strdup ("StackOverflowException"); //if we OVF, we can't expect to have stack space to JIT Exception::ToString.
		free_message = TRUE;
	} else {
		
		if (((MonoException*)exc)->native_trace_ips) {
			message = mono_exception_get_native_backtrace ((MonoException*)exc);
			free_message = TRUE;
		} else {
			MonoObject *other_exc = NULL;
			str = mono_object_to_string (exc, &other_exc);
			if (other_exc) {
				char *original_backtrace = mono_exception_get_managed_backtrace ((MonoException*)exc);
				char *nested_backtrace = mono_exception_get_managed_backtrace ((MonoException*)other_exc);
				
				message = g_strdup_printf ("Nested exception detected.\nOriginal Exception: %s\nNested exception:%s\n",
					original_backtrace, nested_backtrace);

				g_free (original_backtrace);
				g_free (nested_backtrace);
				free_message = TRUE;
			} else if (str) {
				message = mono_string_to_utf8_checked (str, &error);
				if (!mono_error_ok (&error)) {
					mono_error_cleanup (&error);
					message = (char *) "";
				} else {
					free_message = TRUE;
				}
			}
		}
	}

	/*
	 * g_printerr ("\nUnhandled Exception: %s.%s: %s\n", exc->vtable->klass->name_space, 
	 *	   exc->vtable->klass->name, message);
	 */
	g_printerr ("\nUnhandled Exception:\n%s\n", message);
	
	if (free_message)
		g_free (message);
}

/**
 * mono_delegate_ctor:
 * @this: pointer to an uninitialized delegate object
 * @target: target object
 * @addr: pointer to native code
 * @method: method
 *
 * Initialize a delegate and sets a specific method, not the one
 * associated with addr.  This is useful when sharing generic code.
 * In that case addr will most probably not be associated with the
 * correct instantiation of the method.
 */
void
mono_delegate_ctor_with_method (MonoObject *this_obj, MonoObject *target, gpointer addr, MonoMethod *method)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoDelegate *delegate = (MonoDelegate *)this_obj;

	g_assert (this_obj);
	g_assert (addr);

	g_assert (mono_class_has_parent (mono_object_class (this_obj), mono_defaults.multicastdelegate_class));

	if (method)
		delegate->method = method;

	mono_stats.delegate_creations++;

#ifndef DISABLE_REMOTING
	if (target && target->vtable->klass == mono_defaults.transparent_proxy_class) {
		g_assert (method);
		method = mono_marshal_get_remoting_invoke (method);
		delegate->method_ptr = mono_compile_method (method);
		MONO_OBJECT_SETREF (delegate, target, target);
	} else
#endif
	{
		delegate->method_ptr = addr;
		MONO_OBJECT_SETREF (delegate, target, target);
	}

	delegate->invoke_impl = arch_create_delegate_trampoline (delegate->object.vtable->domain, delegate->object.vtable->klass);
}

/**
 * mono_delegate_ctor:
 * @this: pointer to an uninitialized delegate object
 * @target: target object
 * @addr: pointer to native code
 *
 * This is used to initialize a delegate.
 */
void
mono_delegate_ctor (MonoObject *this_obj, MonoObject *target, gpointer addr)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoDomain *domain = mono_domain_get ();
	MonoJitInfo *ji;
	MonoMethod *method = NULL;

	g_assert (addr);

	ji = mono_jit_info_table_find (domain, mono_get_addr_from_ftnptr (addr));
	/* Shared code */
	if (!ji && domain != mono_get_root_domain ())
		ji = mono_jit_info_table_find (mono_get_root_domain (), mono_get_addr_from_ftnptr (addr));
	if (ji) {
		method = mono_jit_info_get_method (ji);
		g_assert (!method->klass->generic_container);
	}

	mono_delegate_ctor_with_method (this_obj, target, addr, method);
}

/**
 * mono_method_call_message_new:
 * @method: method to encapsulate
 * @params: parameters to the method
 * @invoke: optional, delegate invoke.
 * @cb: async callback delegate.
 * @state: state passed to the async callback.
 *
 * Translates arguments pointers into a MonoMethodMessage.
 */
MonoMethodMessage *
mono_method_call_message_new (MonoMethod *method, gpointer *params, MonoMethod *invoke, 
			      MonoDelegate **cb, MonoObject **state)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoDomain *domain = mono_domain_get ();
	MonoMethodSignature *sig = mono_method_signature (method);
	MonoMethodMessage *msg;
	int i, count;

	msg = (MonoMethodMessage *)mono_object_new (domain, mono_defaults.mono_method_message_class); 
	
	if (invoke) {
		mono_message_init (domain, msg, mono_method_get_object (domain, invoke, NULL), NULL);
		count =  sig->param_count - 2;
	} else {
		mono_message_init (domain, msg, mono_method_get_object (domain, method, NULL), NULL);
		count =  sig->param_count;
	}

	for (i = 0; i < count; i++) {
		gpointer vpos;
		MonoClass *class;
		MonoObject *arg;

		if (sig->params [i]->byref)
			vpos = *((gpointer *)params [i]);
		else 
			vpos = params [i];

		class = mono_class_from_mono_type (sig->params [i]);

		if (class->valuetype)
			arg = mono_value_box (domain, class, vpos);
		else 
			arg = *((MonoObject **)vpos);
		      
		mono_array_setref (msg->args, i, arg);
	}

	if (cb != NULL && state != NULL) {
		*cb = *((MonoDelegate **)params [i]);
		i++;
		*state = *((MonoObject **)params [i]);
	}

	return msg;
}

/**
 * mono_method_return_message_restore:
 *
 * Restore results from message based processing back to arguments pointers
 */
void
mono_method_return_message_restore (MonoMethod *method, gpointer *params, MonoArray *out_args)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoMethodSignature *sig = mono_method_signature (method);
	int i, j, type, size, out_len;
	
	if (out_args == NULL)
		return;
	out_len = mono_array_length (out_args);
	if (out_len == 0)
		return;

	for (i = 0, j = 0; i < sig->param_count; i++) {
		MonoType *pt = sig->params [i];

		if (pt->byref) {
			char *arg;
			if (j >= out_len)
				mono_raise_exception (mono_get_exception_execution_engine ("The proxy call returned an incorrect number of output arguments"));

			arg = mono_array_get (out_args, gpointer, j);
			type = pt->type;

			g_assert (type != MONO_TYPE_VOID);

			if (MONO_TYPE_IS_REFERENCE (pt)) {
				mono_gc_wbarrier_generic_store (*((MonoObject ***)params [i]), (MonoObject *)arg);
			} else {
				if (arg) {
					MonoClass *class = ((MonoObject*)arg)->vtable->klass;
					size = mono_class_value_size (class, NULL);
					if (class->has_references)
						mono_gc_wbarrier_value_copy (*((gpointer *)params [i]), arg + sizeof (MonoObject), 1, class);
					else
						mono_gc_memmove_atomic (*((gpointer *)params [i]), arg + sizeof (MonoObject), size);
				} else {
					size = mono_class_value_size (mono_class_from_mono_type (pt), NULL);
					mono_gc_bzero_atomic (*((gpointer *)params [i]), size);
				}
			}

			j++;
		}
	}
}

#ifndef DISABLE_REMOTING

/**
 * mono_load_remote_field:
 * @this: pointer to an object
 * @klass: klass of the object containing @field
 * @field: the field to load
 * @res: a storage to store the result
 *
 * This method is called by the runtime on attempts to load fields of
 * transparent proxy objects. @this points to such TP, @klass is the class of
 * the object containing @field. @res is a storage location which can be
 * used to store the result.
 *
 * Returns: an address pointing to the value of field.
 */
gpointer
mono_load_remote_field (MonoObject *this_obj, MonoClass *klass, MonoClassField *field, gpointer *res)
{
	MONO_REQ_GC_UNSAFE_MODE;

	static MonoMethod *getter = NULL;
	MonoDomain *domain = mono_domain_get ();
	MonoTransparentProxy *tp = (MonoTransparentProxy *) this_obj;
	MonoClass *field_class;
	MonoMethodMessage *msg;
	MonoArray *out_args;
	MonoObject *exc;
	char* full_name;

	g_assert (mono_object_is_transparent_proxy (this_obj));
	g_assert (res != NULL);

	if (mono_class_is_contextbound (tp->remote_class->proxy_class) && tp->rp->context == (MonoObject *) mono_context_get ()) {
		mono_field_get_value (tp->rp->unwrapped_server, field, res);
		return res;
	}
	
	if (!getter) {
		getter = mono_class_get_method_from_name (mono_defaults.object_class, "FieldGetter", -1);
		if (!getter)
			mono_raise_exception (mono_get_exception_not_supported ("Linked away."));
	}
	
	field_class = mono_class_from_mono_type (field->type);

	msg = (MonoMethodMessage *)mono_object_new (domain, mono_defaults.mono_method_message_class);
	out_args = mono_array_new (domain, mono_defaults.object_class, 1);
	mono_message_init (domain, msg, mono_method_get_object (domain, getter, NULL), out_args);

	full_name = mono_type_get_full_name (klass);
	mono_array_setref (msg->args, 0, mono_string_new (domain, full_name));
	mono_array_setref (msg->args, 1, mono_string_new (domain, mono_field_get_name (field)));
	g_free (full_name);

	mono_remoting_invoke ((MonoObject *)(tp->rp), msg, &exc, &out_args);

	if (exc) mono_raise_exception ((MonoException *)exc);

	if (mono_array_length (out_args) == 0)
		return NULL;

	*res = mono_array_get (out_args, MonoObject *, 0); /* FIXME: GC write abrrier for res */

	if (field_class->valuetype) {
		return ((char *)*res) + sizeof (MonoObject);
	} else
		return res;
}

/**
 * mono_load_remote_field_new:
 * @this: 
 * @klass: 
 * @field:
 *
 * Missing documentation.
 */
MonoObject *
mono_load_remote_field_new (MonoObject *this_obj, MonoClass *klass, MonoClassField *field)
{
	MONO_REQ_GC_UNSAFE_MODE;

	static MonoMethod *getter = NULL;
	MonoDomain *domain = mono_domain_get ();
	MonoTransparentProxy *tp = (MonoTransparentProxy *) this_obj;
	MonoClass *field_class;
	MonoMethodMessage *msg;
	MonoArray *out_args;
	MonoObject *exc, *res;
	char* full_name;

	g_assert (mono_object_is_transparent_proxy (this_obj));

	field_class = mono_class_from_mono_type (field->type);

	if (mono_class_is_contextbound (tp->remote_class->proxy_class) && tp->rp->context == (MonoObject *) mono_context_get ()) {
		gpointer val;
		if (field_class->valuetype) {
			res = mono_object_new (domain, field_class);
			val = ((gchar *) res) + sizeof (MonoObject);
		} else {
			val = &res;
		}
		mono_field_get_value (tp->rp->unwrapped_server, field, val);
		return res;
	}

	if (!getter) {
		getter = mono_class_get_method_from_name (mono_defaults.object_class, "FieldGetter", -1);
		if (!getter)
			mono_raise_exception (mono_get_exception_not_supported ("Linked away."));
	}
	
	msg = (MonoMethodMessage *)mono_object_new (domain, mono_defaults.mono_method_message_class);
	out_args = mono_array_new (domain, mono_defaults.object_class, 1);

	mono_message_init (domain, msg, mono_method_get_object (domain, getter, NULL), out_args);

	full_name = mono_type_get_full_name (klass);
	mono_array_setref (msg->args, 0, mono_string_new (domain, full_name));
	mono_array_setref (msg->args, 1, mono_string_new (domain, mono_field_get_name (field)));
	g_free (full_name);

	mono_remoting_invoke ((MonoObject *)(tp->rp), msg, &exc, &out_args);

	if (exc) mono_raise_exception ((MonoException *)exc);

	if (mono_array_length (out_args) == 0)
		res = NULL;
	else
		res = mono_array_get (out_args, MonoObject *, 0);

	return res;
}

/**
 * mono_store_remote_field:
 * @this_obj: pointer to an object
 * @klass: klass of the object containing @field
 * @field: the field to load
 * @val: the value/object to store
 *
 * This method is called by the runtime on attempts to store fields of
 * transparent proxy objects. @this_obj points to such TP, @klass is the class of
 * the object containing @field. @val is the new value to store in @field.
 */
void
mono_store_remote_field (MonoObject *this_obj, MonoClass *klass, MonoClassField *field, gpointer val)
{
	MONO_REQ_GC_UNSAFE_MODE;

	static MonoMethod *setter = NULL;
	MonoDomain *domain = mono_domain_get ();
	MonoTransparentProxy *tp = (MonoTransparentProxy *) this_obj;
	MonoClass *field_class;
	MonoMethodMessage *msg;
	MonoArray *out_args;
	MonoObject *exc;
	MonoObject *arg;
	char* full_name;

	g_assert (mono_object_is_transparent_proxy (this_obj));

	field_class = mono_class_from_mono_type (field->type);

	if (mono_class_is_contextbound (tp->remote_class->proxy_class) && tp->rp->context == (MonoObject *) mono_context_get ()) {
		if (field_class->valuetype) mono_field_set_value (tp->rp->unwrapped_server, field, val);
		else mono_field_set_value (tp->rp->unwrapped_server, field, *((MonoObject **)val));
		return;
	}

	if (!setter) {
		setter = mono_class_get_method_from_name (mono_defaults.object_class, "FieldSetter", -1);
		if (!setter)
			mono_raise_exception (mono_get_exception_not_supported ("Linked away."));
	}

	if (field_class->valuetype)
		arg = mono_value_box (domain, field_class, val);
	else 
		arg = *((MonoObject **)val);
		

	msg = (MonoMethodMessage *)mono_object_new (domain, mono_defaults.mono_method_message_class);
	mono_message_init (domain, msg, mono_method_get_object (domain, setter, NULL), NULL);

	full_name = mono_type_get_full_name (klass);
	mono_array_setref (msg->args, 0, mono_string_new (domain, full_name));
	mono_array_setref (msg->args, 1, mono_string_new (domain, mono_field_get_name (field)));
	mono_array_setref (msg->args, 2, arg);
	g_free (full_name);

	mono_remoting_invoke ((MonoObject *)(tp->rp), msg, &exc, &out_args);

	if (exc) mono_raise_exception ((MonoException *)exc);
}

/**
 * mono_store_remote_field_new:
 * @this_obj:
 * @klass:
 * @field:
 * @arg:
 *
 * Missing documentation
 */
void
mono_store_remote_field_new (MonoObject *this_obj, MonoClass *klass, MonoClassField *field, MonoObject *arg)
{
	MONO_REQ_GC_UNSAFE_MODE;

	static MonoMethod *setter = NULL;
	MonoDomain *domain = mono_domain_get ();
	MonoTransparentProxy *tp = (MonoTransparentProxy *) this_obj;
	MonoClass *field_class;
	MonoMethodMessage *msg;
	MonoArray *out_args;
	MonoObject *exc;
	char* full_name;

	g_assert (mono_object_is_transparent_proxy (this_obj));

	field_class = mono_class_from_mono_type (field->type);

	if (mono_class_is_contextbound (tp->remote_class->proxy_class) && tp->rp->context == (MonoObject *) mono_context_get ()) {
		if (field_class->valuetype) mono_field_set_value (tp->rp->unwrapped_server, field, ((gchar *) arg) + sizeof (MonoObject));
		else mono_field_set_value (tp->rp->unwrapped_server, field, arg);
		return;
	}

	if (!setter) {
		setter = mono_class_get_method_from_name (mono_defaults.object_class, "FieldSetter", -1);
		if (!setter)
			mono_raise_exception (mono_get_exception_not_supported ("Linked away."));
	}

	msg = (MonoMethodMessage *)mono_object_new (domain, mono_defaults.mono_method_message_class);
	mono_message_init (domain, msg, mono_method_get_object (domain, setter, NULL), NULL);

	full_name = mono_type_get_full_name (klass);
	mono_array_setref (msg->args, 0, mono_string_new (domain, full_name));
	mono_array_setref (msg->args, 1, mono_string_new (domain, mono_field_get_name (field)));
	mono_array_setref (msg->args, 2, arg);
	g_free (full_name);

	mono_remoting_invoke ((MonoObject *)(tp->rp), msg, &exc, &out_args);

	if (exc) mono_raise_exception ((MonoException *)exc);
}
#endif

/*
 * mono_create_ftnptr:
 *
 *   Given a function address, create a function descriptor for it.
 * This is only needed on some platforms.
 */
gpointer
mono_create_ftnptr (MonoDomain *domain, gpointer addr)
{
	return callbacks.create_ftnptr (domain, addr);
}

/*
 * mono_get_addr_from_ftnptr:
 *
 *   Given a pointer to a function descriptor, return the function address.
 * This is only needed on some platforms.
 */
gpointer
mono_get_addr_from_ftnptr (gpointer descr)
{
	return callbacks.get_addr_from_ftnptr (descr);
}	

/**
 * mono_string_chars:
 * @s: a MonoString
 *
 * Returns a pointer to the UCS16 characters stored in the MonoString
 */
gunichar2 *
mono_string_chars (MonoString *s)
{
	// MONO_REQ_GC_UNSAFE_MODE; //FIXME too much trouble for now

	return s->chars;
}

/**
 * mono_string_length:
 * @s: MonoString
 *
 * Returns the lenght in characters of the string
 */
int
mono_string_length (MonoString *s)
{
	MONO_REQ_GC_UNSAFE_MODE;

	return s->length;
}

/**
 * mono_array_length:
 * @array: a MonoArray*
 *
 * Returns the total number of elements in the array. This works for
 * both vectors and multidimensional arrays.
 */
uintptr_t
mono_array_length (MonoArray *array)
{
	MONO_REQ_GC_UNSAFE_MODE;

	return array->max_length;
}

/**
 * mono_array_addr_with_size:
 * @array: a MonoArray*
 * @size: size of the array elements
 * @idx: index into the array
 *
 * Returns the address of the @idx element in the array.
 */
char*
mono_array_addr_with_size (MonoArray *array, int size, uintptr_t idx)
{
	MONO_REQ_GC_UNSAFE_MODE;

	return ((char*)(array)->vector) + size * idx;
}

