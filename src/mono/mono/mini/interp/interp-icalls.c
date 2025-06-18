#include "interp-icalls.h"
void
do_icall (MonoMethodSignature *sig, MintICallSig op, stackval *ret_sp, stackval *sp, gpointer ptr, gboolean save_last_error)
{
	if (save_last_error)
		mono_marshal_clear_last_error ();
	log_op(op);
	switch (op) {
	case MINT_ICALLSIG_V_V: {
		typedef void (*T)(void);		
		T func = (T)ptr;
        	func ();
		break;
	}
	case MINT_ICALLSIG_V_P: {
		typedef gpointer (*T)(void);		
		T func = (T)ptr;
		ret_sp->data.p = func ();
		break;
	}
	case MINT_ICALLSIG_P_V: {
		typedef void (*T)(gpointer);		
		T func = (T)ptr;
		func (sp [0].data.p);
		break;
	}
	case MINT_ICALLSIG_P_P: {
		typedef gpointer (*T)(gpointer);		
		T func = (T)ptr;
		ret_sp->data.p = func (sp [0].data.p);
		break;
	}
	case MINT_ICALLSIG_PP_V: {
		typedef void (*T)(gpointer,gpointer);		
		T func = (T)ptr;
		func (sp [0].data.p, sp [1].data.p);
		break;
	}
	case MINT_ICALLSIG_PP_P: {
		typedef gpointer (*T)(gpointer,gpointer);		
		T func = (T)ptr;
		ret_sp->data.p = func (sp [0].data.p, sp [1].data.p);
		break;
	}
	case MINT_ICALLSIG_PPP_V: {
		typedef void (*T)(gpointer,gpointer,gpointer);		
		T func = (T)ptr;
		func (sp [0].data.p, sp [1].data.p, sp [2].data.p);
		break;
	}
	case MINT_ICALLSIG_PPP_P: {
		typedef gpointer (*T)(gpointer,gpointer,gpointer);
		T func = (T)ptr;
		ret_sp->data.p = func (sp [0].data.p, sp [1].data.p, sp [2].data.p);
		break;
	}
	case MINT_ICALLSIG_PPPP_V: {
		typedef void (*T)(gpointer,gpointer,gpointer,gpointer);
		T func = (T)ptr;
		func (sp [0].data.p, sp [1].data.p, sp [2].data.p, sp [3].data.p);
		break;
	}
	case MINT_ICALLSIG_PPPP_P: {
		typedef gpointer (*T)(gpointer,gpointer,gpointer,gpointer);
		T func = (T)ptr;
		ret_sp->data.p = func (sp [0].data.p, sp [1].data.p, sp [2].data.p, sp [3].data.p);
		break;
	}
	case MINT_ICALLSIG_PPPPP_V: {
		typedef void (*T)(gpointer,gpointer,gpointer,gpointer,gpointer);
		T func = (T)ptr;
		func (sp [0].data.p, sp [1].data.p, sp [2].data.p, sp [3].data.p, sp [4].data.p);
		break;
	}
	case MINT_ICALLSIG_PPPPP_P: {
		typedef gpointer (*T)(gpointer,gpointer,gpointer,gpointer,gpointer);
		T func = (T)ptr;
		ret_sp->data.p = func (sp [0].data.p, sp [1].data.p, sp [2].data.p, sp [3].data.p, sp [4].data.p);
		break;
	}
	case MINT_ICALLSIG_PPPPPP_V: {
		typedef void (*T)(gpointer,gpointer,gpointer,gpointer,gpointer,gpointer);
		T func = (T)ptr;
		func (sp [0].data.p, sp [1].data.p, sp [2].data.p, sp [3].data.p, sp [4].data.p, sp [5].data.p);
		break;
	}
	case MINT_ICALLSIG_PPPPPP_P: {
		typedef gpointer (*T)(gpointer,gpointer,gpointer,gpointer,gpointer,gpointer);
		T func = (T)ptr;
		ret_sp->data.p = func (sp [0].data.p, sp [1].data.p, sp [2].data.p, sp [3].data.p, sp [4].data.p, sp [5].data.p);
		break;
	}
	default:
		g_assert_not_reached ();
	}

	if (save_last_error)
		mono_marshal_set_last_error ();

	/* convert the native representation to the stackval representation */
	if (sig)
		stackval_from_data (sig->ret, ret_sp, (char*) &ret_sp->data.p, sig->pinvoke && !sig->marshalling_disabled);
}
