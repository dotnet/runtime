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

#include "jit.h"
#include "codegen.h"

gpointer 
arch_begin_invoke (MonoMethod *method, gpointer ret_ip, MonoObject *delegate)
{
	MonoMethodMessage *msg;
	MonoDelegate *async_callback;
	MonoObject *state;
	MonoMethod *im;
	
	im = mono_get_delegate_invoke (method->klass);
	msg = arch_method_call_message_new (method, &delegate, im, &async_callback, &state);

	return mono_thread_pool_add (delegate, msg, async_callback, state);
}

void
arch_end_invoke (MonoMethod *method, gpointer first_arg, ...)
{
	MonoDomain *domain = mono_domain_get ();
	MonoAsyncResult *ares;
	MonoMethodSignature *sig = method->signature;
	MonoMethodMessage *msg;
	MonoObject *res, *exc;
	MonoArray *out_args;

	g_assert (method);

	msg = arch_method_call_message_new (method, &first_arg, NULL, NULL, NULL);

	ares = mono_array_get (msg->args, gpointer, sig->param_count - 1);
	g_assert (ares);

	res = mono_thread_pool_finish (ares, &out_args, &exc);

	if (exc) {
		char *strace = mono_string_to_utf8 (((MonoException*)exc)->stack_trace);
		char  *tmp;
		tmp = g_strdup_printf ("%s\nException Rethrown at:\n", strace);
		g_free (strace);	
		((MonoException*)exc)->stack_trace = mono_string_new (domain, tmp);
		g_free (tmp);
		mono_raise_exception ((MonoException*)exc);
	}

	/* restore return value */
	if (method->signature->ret->type != MONO_TYPE_VOID) {
		g_assert (res);
		arch_method_return_message_restore (method, &first_arg, res, out_args);
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
