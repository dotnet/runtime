/*
 * delegate.c: delegate support functions
 *
 * Authors:
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>

#include <mono/arch/x86/x86-codegen.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/threads.h>
#ifndef PLATFORM_WIN32
#include "mono/io-layer/wapi.h"
#include "mono/io-layer/uglify.h"
#endif

#include "jit.h"
#include "codegen.h"

/* FIXME:
 * - worker threads need to be initialized correctly.
 * - worker threads should be domain specific
 */

typedef struct {
	MonoMethodMessage *msg;
	HANDLE            wait_semaphore;
	MonoMethod        *cb_method;
	MonoDelegate      *cb_target;
	int                inside_cb;
	MonoObject        *state;
	MonoObject        *res;
	MonoArray         *out_args;
} ASyncCall;

static guint32 async_invoke_thread (void);

static GList *async_call_queue = NULL;
static CRITICAL_SECTION delegate_section;
static HANDLE delegate_semaphore = NULL;
static int stop_worker = 0;

/**
 * mono_delegate_ctor:
 * @this: pointer to an uninitialized delegate object
 * @target: target object
 * @addr: pointer to native code
 *
 * This is used to initialize a delegate. We also insert the method_info if
 * we find the info with mono_jit_info_table_find().
 */
void
mono_delegate_ctor (MonoDelegate *this, MonoObject *target, gpointer addr)
{
	MonoDomain *domain = mono_domain_get ();
	MonoMethod *method = NULL;
	MonoClass *class;
	MonoJitInfo *ji;

	g_assert (this);
	g_assert (addr);

	class = this->object.vtable->klass;

	if ((ji = mono_jit_info_table_find (domain, addr))) {
		method = ji->method;
		this->method_info = mono_method_get_object (domain, method);
	}

	if (target && target->vtable->klass == mono_defaults.transparent_proxy_class) {
		g_assert (method);
		this->method_ptr = arch_create_remoting_trampoline (method);
		this->target = target;
	} else {
		this->method_ptr = addr;
		this->target = target;
	}
}

static void
mono_async_invoke (MonoAsyncResult *ares, gboolean cb_only)
{
	ASyncCall *ac = (ASyncCall *)ares->data;

	if (!cb_only)
		ac->res = mono_message_invoke (ares->async_delegate, ac->msg, 
					       &ac->msg->exc, &ac->out_args);

	ac->inside_cb = 1;
	ares->completed = 1;
		
	/* notify listeners */
	ReleaseSemaphore (ac->wait_semaphore, 0x7fffffff, NULL);

	/* call async callback if cb_method != null*/
	if (ac->cb_method) {
		void *pa = &ares;
		mono_runtime_invoke (ac->cb_method, ac->cb_target, pa, NULL);
	}
}

gpointer 
arch_begin_invoke (MonoMethod *method, gpointer ret_ip, MonoObject *delegate)
{
	MonoDomain *domain = mono_domain_get ();
	MonoAsyncResult *ares;
	MonoDelegate *async_callback;
	MonoClass *klass;
	MonoMethod *im;
	ASyncCall *ac;
	int i;
	
	ac = g_new0 (ASyncCall, 1);
	ac->wait_semaphore = CreateSemaphore (NULL, 0, 0x7fffffff, NULL);
	
	klass = method->klass;
	im = NULL;

	for (i = 0; i < klass->method.count; ++i) {
		if (klass->methods [i]->name[0] == 'I' && 
		    !strcmp ("Invoke", klass->methods [i]->name) &&
		    klass->methods [i]->signature->param_count == 
		    (method->signature->param_count - 2)) {
			im = klass->methods [i];
		}
	}

	g_assert (im);

	ac->msg = arch_method_call_message_new (method, &delegate, im, &async_callback, &ac->state);

	if (async_callback) {
		klass = ((MonoObject *)async_callback)->vtable->klass;
		im = NULL;
		for (i = 0; i < klass->method.count; ++i) {
			if (klass->methods [i]->name[0] == 'I' && 
			    !strcmp ("Invoke", klass->methods [i]->name) &&
			    klass->methods [i]->signature->param_count == 1) {
				im = klass->methods [i];
				break;
			}
		}
		g_assert (im);
		ac->cb_method = im;
		ac->cb_target = async_callback;
	}

	ares = mono_async_result_new (domain, ac->wait_semaphore, ac->state, ac);
	ares->async_delegate = delegate;

	EnterCriticalSection (&delegate_section);	
	async_call_queue = g_list_append (async_call_queue, ares); 
	LeaveCriticalSection (&delegate_section);

	ReleaseSemaphore (delegate_semaphore, 1, NULL);

	return ares;
}

void
arch_end_invoke (MonoMethod *method, gpointer first_arg, ...)
{
	MonoDomain *domain = mono_domain_get ();
	MonoAsyncResult *ares;
	MonoMethodSignature *sig = method->signature;
	MonoMethodMessage *msg;
	ASyncCall *ac;
	GList *l;

	g_assert (method);

	msg = arch_method_call_message_new (method, &first_arg, NULL, NULL, NULL);

	ares = mono_array_get (msg->args, gpointer, sig->param_count - 1);
	g_assert (ares);

	ac = (ASyncCall *)ares->data;

	/* check if we call EndInvoke twice */
	if (!ares->data) {
		MonoException *e;
		e = mono_exception_from_name (mono_defaults.corlib, "System", 
					      "InvalidOperationException");
		mono_raise_exception (e);
	}

	ares->endinvoke_called = 1;

	EnterCriticalSection (&delegate_section);	
	if ((l = g_list_find (async_call_queue, ares))) {
		async_call_queue = g_list_remove_link (async_call_queue, l);
		mono_async_invoke (ares, FALSE);
	}		
	LeaveCriticalSection (&delegate_section);
	
	/* wait until we are really finished */
	WaitForSingleObject (ac->wait_semaphore, INFINITE);

	if (ac->msg->exc) {
		char *strace = mono_string_to_utf8 (((MonoException*)ac->msg->exc)->stack_trace);
		char  *tmp;
		tmp = g_strdup_printf ("%s\nException Rethrown at:\n", strace);
		g_free (strace);	
		((MonoException*)ac->msg->exc)->stack_trace = mono_string_new (domain, tmp);
		g_free (tmp);
		mono_raise_exception ((MonoException*)ac->msg->exc);
	}

	/* restore return value */
	if (method->signature->ret->type != MONO_TYPE_VOID) {
		g_assert (ac->res);
		arch_method_return_message_restore (method, &first_arg, ac->res, ac->out_args);
	}
}

gpointer
arch_get_delegate_invoke (MonoMethod *method, int *size)
{
	/*
	 *	Invoke( args .. ) {
	 *		if ( prev )
	 *			prev.Invoke();
	 *		return this.<m_target>( args );
	 *	}
	 */
	MonoMethodSignature *csig = method->signature;
	guint8 *code, *addr, *br[2], *pos[2];
	int i, arg_size, this_pos = 4;
			
	if (csig->ret->type == MONO_TYPE_VALUETYPE) {
		g_assert (!csig->ret->byref);
		this_pos = 8;
	}

	arg_size = 0;
	if (csig->param_count) {
		int align;
		
		for (i = 0; i < csig->param_count; ++i) {
			arg_size += mono_type_stack_size (csig->params [i], &align);
			g_assert (align == 4);
		}
	}

	code = addr = g_malloc (64 + arg_size * 2);

	/* load the this pointer */
	x86_mov_reg_membase (code, X86_EAX, X86_ESP, this_pos, 4);
	
	/* load prev */
	x86_mov_reg_membase (code, X86_EDX, X86_EAX, G_STRUCT_OFFSET (MonoMulticastDelegate, prev), 4);

	/* prev == 0 ? */
	x86_alu_reg_imm (code, X86_CMP, X86_EDX, 0);
	br[0] = code; x86_branch32 (code, X86_CC_EQ, 0, TRUE );
	pos[0] = code;
	
	x86_push_reg( code, X86_EAX );
	/* push args */
	for ( i = 0; i < (arg_size>>2); i++ )
		x86_push_membase( code, X86_ESP, (arg_size + this_pos + 4) );
	/* push next */
	x86_push_reg( code, X86_EDX );
	if (this_pos == 8)
		x86_push_membase (code, X86_ESP, (arg_size + 8));
	/* recurse */
	br[1] = code; x86_call_imm( code, 0 );
	pos[1] = code; x86_call_imm( br[1], addr - pos[1] );

	if (this_pos == 8)
		x86_alu_reg_imm (code, X86_ADD, X86_ESP, arg_size + 8);
	else
		x86_alu_reg_imm (code, X86_ADD, X86_ESP, arg_size + 4);
	x86_pop_reg( code, X86_EAX );
	
	/* prev == 0 */ 
	x86_branch32( br[0], X86_CC_EQ, code - pos[0], TRUE );
	
	/* load mtarget */
	x86_mov_reg_membase (code, X86_EDX, X86_EAX, G_STRUCT_OFFSET (MonoDelegate, target), 4); 
	/* mtarget == 0 ? */
	x86_alu_reg_imm (code, X86_CMP, X86_EDX, 0);
	br[0] = code; x86_branch32 (code, X86_CC_EQ, 0, TRUE);
	pos[0] = code;

	/* 
	 * virtual delegate methods: we have to
	 * replace the this pointer with the actual
	 * target
	 */
	x86_mov_membase_reg (code, X86_ESP, this_pos, X86_EDX, 4); 

	/* jump to method_ptr() */
	x86_jump_membase (code, X86_EAX, G_STRUCT_OFFSET (MonoDelegate, method_ptr));

	/* mtarget != 0 */ 
	x86_branch32( br[0], X86_CC_EQ, code - pos[0], TRUE);
	/* 
	 * static delegate methods: we have to remove
	 * the this pointer from the activation frame
	 * - I do this creating a new stack frame anx
	 * copy all arguments except the this pointer
	 */
	g_assert ((arg_size & 3) == 0);
	for (i = 0; i < (arg_size>>2); i++) {
		x86_push_membase (code, X86_ESP, (arg_size + this_pos));
	}
	
	if (this_pos == 8)
		x86_push_membase (code, X86_ESP, (arg_size + 4));
	
	x86_call_membase (code, X86_EAX, G_STRUCT_OFFSET (MonoDelegate, method_ptr));
	if (arg_size) {
		if (this_pos == 8) 
			x86_alu_reg_imm (code, X86_ADD, X86_ESP, arg_size + 4);
		else
			x86_alu_reg_imm (code, X86_ADD, X86_ESP, arg_size);
	}
	
	x86_ret (code);
	
	g_assert ((code - addr) < (64 + arg_size * 2));

	if (size)
		*size = code - addr;

	return addr;
}

static void
async_invoke_abort (MonoObject *obj)
{
	MonoDomain *domain = obj->vtable->domain;
	MonoJitTlsData *jit_tls = TlsGetValue (mono_jit_tls_id);
	MonoAsyncResult *ares = jit_tls->async_result;
	ASyncCall *ac = (ASyncCall *)ares->data;

	ares->completed = 1;

	if (!ac->msg->exc)
		ac->msg->exc = obj;

	/* we need to call the callback if not already called */
	if (!ac->inside_cb) {
		ac->inside_cb = 1;
		mono_async_invoke (ares, TRUE);
	}

	/* signal that we finished processing */
	ReleaseSemaphore (ac->wait_semaphore, 0x7fffffff, NULL);

	/* start a new worker */
	mono_thread_create (domain, async_invoke_thread);
	/* exit current one */
	ExitThread (0);
}

static guint32
async_invoke_thread ()
{
	MonoDomain *domain;
	static int workers = 1;
	static HANDLE first_worker = NULL;
	MonoJitTlsData *jit_tls = TlsGetValue (mono_jit_tls_id);
      
	if (!first_worker) {
		first_worker = GetCurrentThread ();
		g_assert (first_worker);
	}

	jit_tls->abort_func = async_invoke_abort;

	for (;;) {
		MonoAsyncResult *ar;
		gboolean new_worker = FALSE;

		if (WaitForSingleObject (delegate_semaphore, 3000) == WAIT_TIMEOUT) {
			if (GetCurrentThread () != first_worker)
				ExitThread (0);
		}
		
		ar = NULL;
		EnterCriticalSection (&delegate_section);
		
		if (async_call_queue) {
			if ((g_list_length (async_call_queue) > 1) && (workers < mono_worker_threads)) {
				new_worker = TRUE;
				workers++;
			}

			ar = (MonoAsyncResult *)async_call_queue->data;
			async_call_queue = g_list_remove_link (async_call_queue, async_call_queue); 

		}

		LeaveCriticalSection (&delegate_section);

		if (stop_worker)
			ExitThread (0);

		if (!ar)
			continue;
		
		/* worker threads invokes methods in different domains,
		 * so we need to set the right domain here */
		domain = ((MonoObject *)ar)->vtable->domain;
		mono_domain_set (domain);

		if (new_worker) 
			mono_thread_create (domain, async_invoke_thread);

		jit_tls->async_result = ar;

		mono_async_invoke (ar, FALSE);
	}

	return 0;
}

void
mono_delegate_init ()
{
	MonoDomain *domain = mono_domain_get ();
	MonoObject *thread;

	delegate_semaphore = CreateSemaphore (NULL, 0, 0x7fffffff, NULL);
	g_assert (delegate_semaphore != INVALID_HANDLE_VALUE);
	InitializeCriticalSection (&delegate_section);
	thread = mono_thread_create (domain, async_invoke_thread);
	g_assert (thread != NULL);
}

void
mono_delegate_cleanup ()
{
	stop_worker = 1;

	/* signal all waiters in order to stop all workers (max. 0xffff) */
	ReleaseSemaphore (delegate_semaphore, 0xffff, NULL);
}
