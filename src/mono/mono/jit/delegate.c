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
#include "mono/io-layer/wapi.h"
#include "mono/io-layer/uglify.h"

#include "jit.h"
#include "codegen.h"

/* FIXME:
 * - worker threads need to be initialized correctly.
 * - worker threads should be domain specific
 */

typedef struct {
	MonoMethod *begin_method;
	MonoMethod *end_method;
	int         frame_size;
	gpointer    stack_frame;
	HANDLE      wait_semaphore;
	gpointer    code;
	gpointer    cb_code;
	gpointer    cb_target;
	int         inside_cb;
	MonoObject *state;
	guint32     res_eax;
	guint32     res_edx;
	double      res_freg;
	MonoObject *exc;
} ASyncCall;

static guint32 async_invoke_thread (void);

static GList *async_call_queue = NULL;
static CRITICAL_SECTION delegate_section;
static HANDLE delegate_semaphore = NULL;
static int stop_worker = 0;

void
mono_delegate_ctor (MonoDelegate *this, MonoObject *target, gpointer addr)
{
	MonoDomain *domain = mono_domain_get ();
	MonoClass *class;
	MonoJitInfo *ji;

	g_assert (this);
	g_assert (addr);

	class = this->object.vtable->klass;

	if ((ji = mono_jit_info_table_find (domain, addr))) {
		this->method_info = mono_method_get_object (domain, ji->method);
	}
	
	this->target = target;
	this->method_ptr = addr;

}

static gpointer
arch_get_async_invoke ()
{
	static guint8 *start = NULL, *code;

	/* async_invoke (MonoAsyncResult *ar) */

	if (start)
		return start;

	start = code = g_malloc (512);

	/* save caller saved regs */
	x86_push_reg (code, X86_EBX);
	x86_push_reg (code, X86_EDI);
	x86_push_reg (code, X86_ESI);

	/* load MonoAsyncResult into ESI */
	x86_mov_reg_membase (code, X86_ESI, X86_ESP, 16, 4);
	/* load ASyncCall into EBX */
	x86_mov_reg_membase (code, X86_EBX, X86_ESI, G_STRUCT_OFFSET (MonoAsyncResult, data), 4);
	/* load frame_size into EDI */
	x86_mov_reg_membase (code, X86_EDI, X86_EBX, G_STRUCT_OFFSET (ASyncCall, frame_size), 4);
	/* allocate stack frame */
	x86_alu_reg_reg (code, X86_SUB, X86_ESP, X86_EDI);
	
	/* memcopy activation frame to the stack */
	x86_push_reg (code, X86_EDI);
	x86_push_membase (code, X86_EBX, G_STRUCT_OFFSET (ASyncCall, stack_frame));
	x86_lea_membase (code, X86_ECX, X86_ESP, 8);
	x86_push_reg (code, X86_ECX);
	x86_call_code (code, memcpy);
	x86_alu_reg_imm (code, X86_ADD, X86_ESP, 12);

	/* call delegate invoke */
	x86_call_membase (code, X86_EBX, G_STRUCT_OFFSET (ASyncCall, code));
	x86_alu_reg_reg (code, X86_ADD, X86_ESP, X86_EDI);
	
	/* save results */
	x86_mov_membase_reg (code, X86_EBX, G_STRUCT_OFFSET (ASyncCall, res_eax), X86_EAX, 4);
	x86_mov_membase_reg (code, X86_EBX, G_STRUCT_OFFSET (ASyncCall, res_edx), X86_EDX, 4);
	x86_fst_membase (code, X86_EBX, G_STRUCT_OFFSET (ASyncCall, res_freg), TRUE, FALSE);

	/* set inside_cb to 1 */
	x86_mov_membase_imm (code, X86_EBX, G_STRUCT_OFFSET (ASyncCall, inside_cb), 1, 4);
	/* set completed to 1 */
	x86_mov_membase_imm (code, X86_ESI, G_STRUCT_OFFSET (MonoAsyncResult, completed), 
			     1, sizeof (MonoBoolean));

	/* notify listeners */
	x86_push_imm (code, 0);
	x86_push_imm (code, 1);
	x86_push_membase (code, X86_EBX, G_STRUCT_OFFSET (ASyncCall, wait_semaphore));
	x86_call_code (code, ReleaseSemaphore);
	x86_alu_reg_imm (code, X86_ADD, X86_ESP, 12);

	/* call async callback */
	/* push pointer to AsyncResult */
	x86_push_reg (code, X86_ESI);
	x86_push_membase (code, X86_EBX, G_STRUCT_OFFSET (ASyncCall, cb_target));
	x86_call_membase (code, X86_EBX, G_STRUCT_OFFSET (ASyncCall, cb_code));
	x86_alu_reg_imm (code, X86_ADD, X86_ESP, 8);
	
	/* restore caller saved regs */
	x86_pop_reg (code, X86_ESI);
	x86_pop_reg (code, X86_EDI);
	x86_pop_reg (code, X86_EBX);

	x86_ret (code);
	
	return start;
}

gpointer 
arch_begin_invoke (MonoMethod *method, gpointer ret_ip, MonoObject *this, ...)
{
	MonoDomain *domain = mono_domain_get ();
	MonoMethodSignature *csig = method->signature;
	MonoAsyncResult *ares;
	MonoDelegate *async_callback;
	MonoClass *klass;
	MonoMethod *im;
	ASyncCall *ac;
	int i, align, arg_size = 4;

	if (csig->ret->type == MONO_TYPE_VALUETYPE) {
		g_assert (!csig->ret->byref);
		arg_size += sizeof (gpointer);
	}

	if (csig->param_count) {
		for (i = 0; i < csig->param_count; ++i)
			arg_size += mono_type_stack_size (csig->params [i], &align);
	}

	ac = g_new0 (ASyncCall, 1);
	ac->begin_method = method;
	ac->stack_frame = g_memdup (&this, arg_size);
	ac->frame_size = arg_size;
	ac->state = *((MonoObject **)(((char *)&this) + arg_size - sizeof (gpointer)));
	ac->wait_semaphore = CreateSemaphore (NULL, 0, 0x7fffffff, NULL);

	async_callback = *((MonoDelegate **)(((char *)&this) + arg_size - sizeof (gpointer) * 2));
	
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
		ac->cb_code = arch_compile_method (im);
		ac->cb_target = async_callback;
	}

	klass = this->vtable->klass;
	im = NULL;

	for (i = 0; i < klass->method.count; ++i) {
		if (klass->methods [i]->name[0] == 'I' && 
		    !strcmp ("Invoke", klass->methods [i]->name) &&
		    klass->methods [i]->signature->param_count == (csig->param_count - 2)) {
			im = klass->methods [i];
		}
		if (klass->methods [i]->name[0] == 'E' && 
		    !strcmp ("EndInvoke", klass->methods [i]->name))
			ac->end_method = klass->methods [i];
	}

	g_assert (ac->end_method);
	g_assert (im);

	ac->code = arch_compile_method (im);

	ares = mono_async_result_new (domain, ac->wait_semaphore, ac->state, ac);

	ares->async_delegate = this;

	EnterCriticalSection (&delegate_section);	
	async_call_queue = g_list_append (async_call_queue, ares); 
	LeaveCriticalSection (&delegate_section);

	ReleaseSemaphore (delegate_semaphore, 1, NULL);

	return ares;
}

void
arch_end_invoke (MonoObject *this, gpointer handle, ...)
{
	void (*async_invoke) (MonoAsyncResult *ar) = arch_get_async_invoke ();
	MonoAsyncResult *ares = (MonoAsyncResult *)handle;
	ASyncCall *ac = (ASyncCall *)ares->data;
	MonoMethodSignature *sig;
	MonoDomain *domain = this->vtable->domain;
	int type;
	void *resp;
	GList *l;
	int res_eax, res_edx;
	double res_freg;

	/* check if we call EndInvoke twice */
	if (!ares->data) {
		MonoException *e;
		e = mono_exception_from_name (mono_defaults.corlib, "System", 
					      "InvalidOperationException");
		mono_raise_exception (e);
	}

	ares->endinvoke_called = 1;

	EnterCriticalSection (&delegate_section);	
	if ((l = g_list_find (async_call_queue, handle))) {
		async_call_queue = g_list_remove_link (async_call_queue, l);
		async_invoke (ares);
	}		
	LeaveCriticalSection (&delegate_section);

	if (ac->exc) {
		char *strace = mono_string_to_utf8 (((MonoException*)ac->exc)->stack_trace);
		char  *tmp;
		tmp = g_strdup_printf ("%s\nException Rethrown at:\n", strace);
		g_free (strace);	
		((MonoException*)ac->exc)->stack_trace = mono_string_new (domain, tmp);
		g_free (tmp);
		mono_raise_exception (ac->exc);
	}

	sig = ac->end_method->signature;

	/* save return value */
	res_eax = ac->res_eax;
	res_edx = ac->res_edx;
	res_freg = ac->res_freg;

	/* free resources */
	/* fixme: this triggers a strange SEGV somethimes with tests/delegate1.cs */
	//g_free (ac->stack_frame);
	//g_free (ac);
	ares->data = NULL;

	/* restore return value */

	if (sig->ret->byref) {
		resp = &res_eax;
		asm ("movl (%0),%%eax" : : "r" (resp) : "eax");
		return;
	}

	type = sig->ret->type;
handle_enum:
	switch (type) {
	case MONO_TYPE_VOID:
		/* nothing to do */
		break;
	case MONO_TYPE_U1:
	case MONO_TYPE_I1:
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_U2:
	case MONO_TYPE_I2:
	case MONO_TYPE_CHAR:
#if SIZEOF_VOID_P == 4
	case MONO_TYPE_U:
	case MONO_TYPE_I:
#endif
	case MONO_TYPE_U4:
	case MONO_TYPE_I4:
	case MONO_TYPE_STRING:
	case MONO_TYPE_CLASS: 
		resp = &res_eax;
		asm ("movl (%0),%%eax" : : "r" (resp) : "eax");
		break;
#if SIZEOF_VOID_P == 8
	case MONO_TYPE_U:
	case MONO_TYPE_I:
#endif
	case MONO_TYPE_U8:
	case MONO_TYPE_I8:
		resp = &res_eax;
		asm ("movl (%0),%%eax" : : "r" (resp) : "eax");
		resp = &res_edx;
		asm ("movl (%0),%%edx" : : "r" (resp) : "edx");
		break;
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
		resp = &res_freg;
		asm ("fldl (%0)" : : "r" (resp) : "st", "st(1)" );
		break;
	case MONO_TYPE_VALUETYPE:
		if (sig->ret->data.klass->enumtype) {
			type = sig->ret->data.klass->enum_basetype->type;
			goto handle_enum;
		} else {
			/* do nothing */
		}
		break;
	default:
		g_error ("type 0x%x not handled in endinvoke", sig->ret->type);

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

	code = addr = g_malloc (64 + arg_size);
	
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
	
	g_assert ((code - addr) < (64 + arg_size));

	if (size)
		*size = code - addr;

	return addr;
}

static void
async_invoke_abort (MonoObject *obj)
{
	MonoDomain *domain = obj->vtable->domain;
	MonoAsyncResult *ares = TlsGetValue (async_result_id);
	ASyncCall *ac = (ASyncCall *)ares->data;

	ares->completed = 1;

	if (!ac->exc)
		ac->exc = obj;

	/* we need to call the callback if not already called */
	if (!ac->inside_cb) {
		void (*async_cb) (gpointer target, MonoAsyncResult *ares);
		ac->inside_cb = 1;
		async_cb = ac->cb_code; 
		async_cb (ac->cb_target, ares);
	}

	/* signal that we finished processing */
	ReleaseSemaphore (ac->wait_semaphore, 1, NULL);

	/* start a new worker */
	mono_thread_create (domain, async_invoke_thread);
	/* exit current one */
	ExitThread (0);
}

static guint32
async_invoke_thread ()
{
	MonoDomain *domain;
	void (*async_invoke) (MonoAsyncResult *ar) = arch_get_async_invoke ();
	static int workers = 1;

	TlsSetValue (exc_cleanup_id, async_invoke_abort);

	for (;;) {
		MonoAsyncResult *ar;
		gboolean new_worker = FALSE;

		WaitForSingleObject (delegate_semaphore, INFINITE);
		
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

		TlsSetValue (async_result_id, ar);

		async_invoke (ar);
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
}
