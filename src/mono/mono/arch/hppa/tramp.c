/*
    Copyright (c) 2003 Bernie Solomon <bernard@ugsolutions.com>
    
    Permission is hereby granted, free of charge, to any person obtaining
    a copy of this software and associated documentation files (the
    "Software"), to deal in the Software without restriction, including
    without limitation the rights to use, copy, modify, merge, publish,
    distribute, sublicense, and/or sell copies of the Software, and to
    permit persons to whom the Software is furnished to do so, subject to
    the following conditions:
    
    The above copyright notice and this permission notice shall be
    included in all copies or substantial portions of the Software.
    
    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
    NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
    LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
    OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
    WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.


    Trampoline generation for HPPA - currently (Oct 9th 2003) only
    supports 64 bits - and the HP compiler.
*/
#ifndef __linux__

#include "mono/interpreter/interp.h"
#include "mono/metadata/appdomain.h"
#include "mono/metadata/tabledefs.h"
#include "hppa-codegen.h"

#if SIZEOF_VOID_P != 8
#error "HPPA code only currently supports 64bit pointers"
#endif

// debugging flag which dumps code generated 
static int debug_asm = 0;


static void flush_cache(void *address, int length)
{
#ifdef __GNUC__
#error "currently only supports the HP C compiler"
#else
	int cache_line_size = 16;
	ulong_t end = (ulong_t)address + length;
	register ulong_t sid;
	register ulong_t offset = (ulong_t) address;
	register ulong_t r0 = 0;

	_asm("LDSID", 0, offset, sid);
	_asm("MTSP", sid, 0);
	_asm("FDC", r0, 0, offset);
	offset = (offset + (cache_line_size - 1)) & ~(cache_line_size - 1);
	while (offset < end) {
		(void)_asm("FDC", r0, 0, offset);
		offset += cache_line_size;
	}
	_asm("SYNC");
	offset = (ulong_t) address;
	_asm("FIC", r0, 0, offset);
	offset = (offset + (cache_line_size - 1)) & ~(cache_line_size - 1);
	while (offset < end) {
		(void)_asm("FIC", r0, 0, offset);
		offset += cache_line_size;
	}
	_asm("SYNC");
	// sync needs at least 7 instructions after it... this is what is used for NOP
	_asm("OR", 0, 0, 0);
	_asm("OR", 0, 0, 0);
	_asm("OR", 0, 0, 0);
	_asm("OR", 0, 0, 0);
	_asm("OR", 0, 0, 0);
	_asm("OR", 0, 0, 0);
	_asm("OR", 0, 0, 0);
#endif
}

static void disassemble (guint32 *code, int n_instrs)
{
	const char *tmp_file = "/tmp/mono_adb.in";
	FILE *fp = fopen(tmp_file, "w");
	int i;
	for (i = 0; i < n_instrs; i++)
		fprintf(fp, "0x%08x=i\n", code[i]);
	fprintf(fp, "$q\n");
	fclose(fp);
	system("adb64 </tmp/mono_adb.in");
        unlink(tmp_file);
}

#define ADD_INST(code, pc, gen_exp) \
	do { if ((code) == NULL) (pc)++; else { gen_exp; pc++; } } while (0)

/*
 * void func (void (*callme)(), void *retval, void *this_obj, stackval *arguments);
 */

MonoPIFunc
mono_arch_create_trampoline (MonoMethodSignature *sig, gboolean string_ctor)
{
	int pc, save_pc;
	int param;
	void **descriptor;
	unsigned int *code = NULL;
	unsigned int *code_start = NULL;
	int arg_reg;
#define FP_ARG_REG(r) (4 + (26 - arg_reg))
	int arg_offset;
	int frame_size = 0;
	int spill_offset;
	int parameter_offset;
	int parameter_slot;
	int args_on_stack;

	if (debug_asm) {
		fprintf(stderr, "trampoline: # params %d has this %d exp this %d string %d, ret type %d\n", 
			sig->param_count, sig->hasthis, sig->explicit_this, string_ctor, sig->ret->type);
	}

	// everything takes 8 bytes unless it is a bigger struct
	for (param = 0; param < sig->param_count; param++) {
		if (sig->params[param]->byref)
			frame_size += 8;
		else {
			if (sig->params[param]->type != MONO_TYPE_VALUETYPE)
				frame_size += 8;
			else {
				if (sig->params [param]->data.klass->enumtype) 
					frame_size += 8;
				else {
					frame_size += 15; // large structs are 16 byte aligned
					frame_size &= ~15;
					frame_size += mono_class_native_size (sig->params [param]->data.klass, NULL);
					frame_size += 7;
					frame_size &= ~7;
				}
			}
		}
	}
				
	if (sig->hasthis)
		frame_size += 8;
	// 16 byte alignment
	if ((frame_size & 15) != 0)
		frame_size += 8;
	// minimum is 64 bytes
	if (frame_size < 64)
		frame_size = 64;

	if (debug_asm)
		fprintf(stderr, "outgoing frame size: %d\n", frame_size);

	frame_size += 16; // for the frame marker (called routines stuff return address etc. here)
	frame_size += 32; // spill area for r4, r5 and r27 (16 byte aligned)

	spill_offset = -frame_size;
	parameter_offset = spill_offset + 32; // spill area size is really 24
	spill_offset += 8;

	/* the rest executes twice - once to count instructions so we can
	   allocate memory in one block and once to fill it in... the count
	   should be pretty fast anyway...
	*/
generate:
	pc = 0;
        arg_reg = 26;
	arg_offset = 0;
	args_on_stack = 0;
	parameter_slot = parameter_offset;

	ADD_INST(code, pc, hppa_std(code, 2, -16, 30));  // STD	  %r2,-16(%r30)   
	ADD_INST(code, pc, hppa_std_ma(code, 3, frame_size, 30));
	ADD_INST(code, pc, hppa_std(code, 4, spill_offset, 30));
	ADD_INST(code, pc, hppa_std(code, 5, spill_offset + 8, 30));
	ADD_INST(code, pc, hppa_copy(code, 29, 3));	   // COPY	  %r29,%r3		  
	ADD_INST(code, pc, hppa_std(code, 27, spill_offset + 16, 30));
	ADD_INST(code, pc, hppa_nop(code)); 		   // NOP			  

	ADD_INST(code, pc, hppa_std(code, 26, -64, 29)); // STD	  %r26,-64(%r29)  callme
	ADD_INST(code, pc, hppa_std(code, 25, -56, 29)); // STD	  %r25,-56(%r29)  retval
	ADD_INST(code, pc, hppa_std(code, 24, -48, 29)); // STD	  %r24,-48(%r29)  this_obj
	ADD_INST(code, pc, hppa_std(code, 23, -40, 29)); // STD	  %r23,-40(%r29)  arguments

	if (sig->param_count > 0)
		ADD_INST(code, pc, hppa_copy(code, 23, 4));  // r4 is the current pointer to the stackval array of args

	if (sig->hasthis) {
		if (sig->call_convention != MONO_CALL_THISCALL) {
			ADD_INST(code, pc, hppa_copy(code, 24, arg_reg));
			--arg_reg;
			parameter_slot += 8;
		} else	{
			fprintf(stderr, "case I didn't handle\n");
		}
	}

	for (param = 0; param < sig->param_count; param++) {
		int type = sig->params[param]->type;
		if (sig->params[param]->byref) {
			if (args_on_stack) {
				ADD_INST(code, pc, hppa_ldd(code, arg_offset, 4, 5));
				ADD_INST(code, pc, hppa_std(code, 5, parameter_slot, 30));
			} else {
				ADD_INST(code, pc, hppa_ldd(code, arg_offset, 4, arg_reg));
				--arg_reg;
			}
			arg_offset += sizeof(stackval);
			parameter_slot += 8;
			continue;
		}
	typeswitch:
		switch (type) {
		case MONO_TYPE_CHAR:
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
			if (args_on_stack) {
				ADD_INST(code, pc, hppa_ldw(code, arg_offset, 4, 5));
				switch (type) {
				case MONO_TYPE_I4:
				case MONO_TYPE_U4:
					ADD_INST(code, pc, hppa_stw(code, 5, parameter_slot + 4, 30));
					break;
				case MONO_TYPE_CHAR:
				case MONO_TYPE_I2:
				case MONO_TYPE_U2:
					ADD_INST(code, pc, hppa_sth(code, 5, parameter_slot + 6, 30));
					break;
				case MONO_TYPE_BOOLEAN:
				case MONO_TYPE_I1:
				case MONO_TYPE_U1:
					ADD_INST(code, pc, hppa_stb(code, 5, parameter_slot + 7, 30));
					break;
				}
			} else {
				ADD_INST(code, pc, hppa_ldw(code, arg_offset, 4, arg_reg));
				--arg_reg;
			}
			arg_offset += sizeof(stackval);
			parameter_slot += 8;
			break;
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_STRING:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_PTR:
			if (args_on_stack) {
				ADD_INST(code, pc, hppa_ldd(code, arg_offset, 4, 5));
				ADD_INST(code, pc, hppa_std(code, 5, parameter_slot, 30));
			} else {
				ADD_INST(code, pc, hppa_ldd(code, arg_offset, 4, arg_reg));
				--arg_reg;
			}
			arg_offset += sizeof(stackval);
			parameter_slot += 8;
			break;
		case MONO_TYPE_R8:
			if (args_on_stack) {
				ADD_INST(code, pc, hppa_ldd(code, arg_offset, 4, 5));
				ADD_INST(code, pc, hppa_std(code, 5, parameter_slot, 30));
			} else {
				ADD_INST(code, pc, hppa_fldd(code, arg_offset, 4, FP_ARG_REG(arg_reg)));
				--arg_reg;
			}
			arg_offset += sizeof(stackval);
			parameter_slot += 8;
			break;
		case MONO_TYPE_R4:
			if (args_on_stack) {
				ADD_INST(code, pc, hppa_fldd(code, arg_offset, 4, 22));
				ADD_INST(code, pc, hppa_fcnv_dbl_sng(code, 22, 22));
				ADD_INST(code, pc, hppa_fstw(code, 22, parameter_slot + 4, 30));
			} else {
				ADD_INST(code, pc, hppa_fldd(code, arg_offset, 4, FP_ARG_REG(arg_reg)));
				ADD_INST(code, pc, hppa_fcnv_dbl_sng(code, FP_ARG_REG(arg_reg), FP_ARG_REG(arg_reg)));
				--arg_reg;
			}
			arg_offset += sizeof(stackval);
			parameter_slot += 8;
			break;
		case MONO_TYPE_VALUETYPE:
			if (sig->params [param]->data.klass->enumtype) {
				type = sig->params [param]->data.klass->enum_basetype->type;
				goto typeswitch;
			} else {
				int size = mono_class_native_size (sig->params [param]->data.klass, NULL);
				// assumes struct is 8 byte aligned whatever its size... (as interp.c guarantees at present)
				// copies multiple of 8 bytes which may include some trailing garbage but should be safe
				if (size <= 8) {
					if (args_on_stack) {
						ADD_INST(code, pc, hppa_ldd(code, arg_offset, 4, 5));
						ADD_INST(code, pc, hppa_ldd(code, 0, 5, 5));
						ADD_INST(code, pc, hppa_std(code, 5, parameter_slot, 30));
					} else {
						ADD_INST(code, pc, hppa_ldd(code, arg_offset, 4, arg_reg));
						ADD_INST(code, pc, hppa_ldd(code, 0, arg_reg, arg_reg));
						--arg_reg;
					}
					parameter_slot += 8;
				} else {
					int soffset = 0;
					if ((parameter_slot & 15) != 0) {
						--arg_reg;
						if (arg_reg < 19) {
							args_on_stack = 1;
						}
						parameter_slot += 8;
					}
					ADD_INST(code, pc, hppa_ldd(code, arg_offset, 4, 5));
					// might generate a lot of code for very large structs... should
					// use a loop or routine call them
					while (size > 0) {
						if (args_on_stack) {
							ADD_INST(code, pc, hppa_ldd(code, soffset, 5, 31));
							ADD_INST(code, pc, hppa_std(code, 31, parameter_slot, 30));
						} else {
							ADD_INST(code, pc, hppa_ldd(code, soffset, 5, arg_reg));
							--arg_reg;
							if (arg_reg < 19)
								args_on_stack = 1;
						}
						parameter_slot += 8;
						soffset += 8;
						size -= 8;
					}
				}
				arg_offset += sizeof(stackval);
				break;
			}
			break;
		default:
			g_error ("mono_create_trampoline: unhandled arg type %d", type);
			return NULL;
		}

		if (arg_reg < 19) {
			args_on_stack = 1;
		}
	}

	// for large return structs just pass on the buffer given to us.
	if (sig->ret->type == MONO_TYPE_VALUETYPE && sig->ret->data.klass->enumtype == 0) {
		int size = mono_class_native_size (sig->ret->data.klass, NULL);
		if (size > 16) {
			ADD_INST(code, pc, hppa_ldd(code, -56, 3, 28));
			ADD_INST(code, pc, hppa_ldd(code, 0, 28, 28));
		}
	}

	ADD_INST(code, pc, hppa_nop(code)); 		   // NOP			  
	ADD_INST(code, pc, hppa_ldd(code, -64, 29, 5));
	ADD_INST(code, pc, hppa_ldd(code, 24, 5, 27));
	ADD_INST(code, pc, hppa_ldd(code, 16, 5, 5));
	ADD_INST(code, pc, hppa_blve(code, 5));
	ADD_INST(code, pc, hppa_ldo(code, parameter_offset + 64, 30, 29));
	ADD_INST(code, pc, hppa_ldd(code, spill_offset + 16, 30, 27));
	ADD_INST(code, pc, hppa_nop(code)); 		   // NOP			  
        
	if (string_ctor) {
		ADD_INST(code, pc, hppa_ldd(code, -56, 3, 19)); // LDD	 -56(%r3),%r19	 
		ADD_INST(code, pc, hppa_std(code, 28, 0, 19));  // STD	 %r28,0(%r19)	 
	}
	else if (sig->ret->type != MONO_TYPE_VOID) {
		int type = sig->ret->type;

	rettypeswitch:
		switch (type) {
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
			ADD_INST(code, pc, hppa_ldd(code, -56, 3, 19)); // LDD	 -56(%r3),%r19	 
			ADD_INST(code, pc, hppa_stb(code, 28, 0, 19));  // STB	 %r28,0(%r19)	 
			break;
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
			ADD_INST(code, pc, hppa_ldd(code, -56, 3, 19)); // LDD	 -56(%r3),%r19	 
			ADD_INST(code, pc, hppa_stw(code, 28, 0, 19));  // STW	 %r28,0(%r19)	 
			break;
		case MONO_TYPE_CHAR:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
			ADD_INST(code, pc, hppa_ldd(code, -56, 3, 19)); // LDD	 -56(%r3),%r19	 
			ADD_INST(code, pc, hppa_sth(code, 28, 0, 19));  // STH	 %r28,0(%r19)
			break;
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_STRING:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_PTR:
			ADD_INST(code, pc, hppa_ldd(code, -56, 3, 19)); // LDD	 -56(%r3),%r19	 
			ADD_INST(code, pc, hppa_std(code, 28, 0, 19));  // STD	 %r28,0(%r19)	 
			break;
		case MONO_TYPE_R8:
			ADD_INST(code, pc, hppa_ldd(code, -56, 3, 19)); // LDD	 -56(%r3),%r19	 
			ADD_INST(code, pc, hppa_fstd(code, 4, 0, 19));  // FSTD	  %fr4,0(%r19)	  
			break;
		case MONO_TYPE_R4:
			ADD_INST(code, pc, hppa_ldd(code, -56, 3, 19)); // LDD	 -56(%r3),%r19	 
			ADD_INST(code, pc, hppa_fstw(code, 4, 0, 19));  // FSTW	  %fr4r,0(%r19)    
			break;
		case MONO_TYPE_VALUETYPE:
			if (sig->ret->data.klass->enumtype) {
				type = sig->ret->data.klass->enum_basetype->type;
				goto rettypeswitch;
			} else {
				int size = mono_class_native_size (sig->ret->data.klass, NULL);
				if (size <= 16)	{
					int reg = 28;
                                        int off = 0;
					ADD_INST(code, pc, hppa_ldd(code, -56, 3, 19));
					ADD_INST(code, pc, hppa_ldd(code, 0, 19, 19));
					if (size > 8) {
						ADD_INST(code, pc, hppa_std(code, 28, 0, 19)); 
						size -= 8;
						reg = 29;
						off += 8;
					}
					// get rest of value right aligned in the register
					ADD_INST(code, pc, hppa_extrdu(code, reg, 8 * size - 1, 8 * size, reg));
					if ((size & 1) != 0) {
						ADD_INST(code, pc, hppa_stb(code, reg, off + size - 1, 19));
						ADD_INST(code, pc, hppa_extrdu(code, reg, 55, 56, reg));
						size -= 1;
					}
					if ((size & 2) != 0) {
						ADD_INST(code, pc, hppa_sth(code, reg, off + size - 2, 19));
						ADD_INST(code, pc, hppa_extrdu(code, reg, 47, 48, reg));
						size -= 2;
					}
					if ((size & 4) != 0)
						ADD_INST(code, pc, hppa_stw(code, reg, off + size - 4, 19));
				}
				break;
			}
		default:
			g_error ("mono_create_trampoline: unhandled ret type %d", type);
			return NULL;
		}
	}

	ADD_INST(code, pc, hppa_ldd(code, -frame_size-16, 30, 2));
	ADD_INST(code, pc, hppa_ldd(code, spill_offset, 30, 4));
	ADD_INST(code, pc, hppa_ldd(code, spill_offset + 8, 30, 5));
	ADD_INST(code, pc, hppa_bve(code, 2, 0));
	ADD_INST(code, pc, hppa_ldd_mb(code, -frame_size, 30, 3));

	if (code == NULL) {
		descriptor = (void **)g_malloc(4 * sizeof(void *) + pc * sizeof(unsigned int));
		code = (unsigned int *)((char *)descriptor + 4 * sizeof(void *));
		code_start = code;
		save_pc = pc;
		goto generate;
        } else 
		g_assert(pc == save_pc);

	if (debug_asm) {
		fprintf(stderr, "generated: %d bytes\n", pc * 4);
		disassemble(code_start, pc);
	}

        // must do this so we can actually execute the code we just put in memory
	flush_cache(code_start, 4 * pc);

	descriptor[0] = 0;
	descriptor[1] = 0;
	descriptor[2] = code_start;
	descriptor[3] = 0;

	return (MonoPIFunc)descriptor;
}

void *
mono_arch_create_method_pointer (MonoMethod *method)
{
	MonoMethodSignature *sig = method->signature;
	MonoJitInfo *ji;
	int i;
	int pc;
	int param;
	void **descriptor = NULL;
	void **data = NULL;
	unsigned int *code = NULL;
	unsigned int *code_start = NULL;
	int arg_reg = 26;
	int arg_offset = 0;
	int frame_size;
	int invoke_rec_offset;
	int stack_vals_offset;
	int stack_val_pos;
	int arg_val_pos;
	int spill_offset;
	int *vtoffsets;
	int t;

	if (debug_asm) {
		fprintf(stderr, "mono_create_method_pointer %s: flags %d\n", method->name, method->flags);
		fprintf(stderr, "method: # params %d has this %d exp this %d\n", sig->param_count, sig->hasthis, sig->explicit_this);
		fprintf(stderr, "ret %d\n", sig->ret->type);
		for (i = 0; i < sig->param_count; i++)
			fprintf(stderr, "%d: %d\n", i, sig->params[i]->type);
	}

	// the extra stackval is for the return val if necessary
	// the 64 is for outgoing parameters and the 16 is the frame marker.
	// the other 16 is space for struct return vals < 16 bytes
        frame_size = sizeof(MonoInvocation) + (sig->param_count + 1) * sizeof(stackval) + 16 + 64 + 16;
	frame_size += 15;
	frame_size &= ~15;
	invoke_rec_offset = -frame_size;
	vtoffsets = (int *)alloca(sig->param_count * sizeof(int));

	t = invoke_rec_offset;

	for (i = 0; i < sig->param_count; ++i)
		if (sig->params[i]->type == MONO_TYPE_VALUETYPE &&
		    !sig->params[i]->data.klass->enumtype && !sig->params[i]->byref) {
			int size = mono_class_native_size (sig->params[i]->data.klass, NULL);
			size += 7;
			size &= ~7;
			t -= size;
			frame_size += size;
			vtoffsets[i] = t;
		}

	stack_vals_offset = invoke_rec_offset + sizeof(MonoInvocation);
	stack_vals_offset += 7;
	stack_vals_offset &= ~7;
	frame_size += 32;
	frame_size += 15;
	frame_size &= ~15;
	spill_offset = -frame_size + 8;

generate:
	stack_val_pos = stack_vals_offset;
	arg_val_pos = -64;
	pc = 0;

	ADD_INST(code, pc, hppa_std(code, 2, -16, 30));
	ADD_INST(code, pc, hppa_std_ma(code, 3, frame_size, 30));
	ADD_INST(code, pc, hppa_std(code, 4, spill_offset, 30));
	ADD_INST(code, pc, hppa_copy(code, 29, 3));
	ADD_INST(code, pc, hppa_std(code, 27, spill_offset + 8, 30));
	ADD_INST(code, pc, hppa_std(code, 28, spill_offset + 16, 30));
	ADD_INST(code, pc, hppa_nop(code));

	ADD_INST(code, pc, hppa_std(code, 26, -64, 29)); // STD	  %r26,-64(%r29)
	ADD_INST(code, pc, hppa_std(code, 25, -56, 29)); // STD	  %r25,-56(%r29)
	ADD_INST(code, pc, hppa_std(code, 24, -48, 29)); // STD	  %r24,-48(%r29)
	ADD_INST(code, pc, hppa_std(code, 23, -40, 29)); // STD	  %r23,-40(%r29)
	ADD_INST(code, pc, hppa_std(code, 22, -32, 29)); // STD	  %r22,-32(%r29)
	ADD_INST(code, pc, hppa_std(code, 21, -24, 29)); // STD	  %r21,-24(%r29)
	ADD_INST(code, pc, hppa_std(code, 20, -16, 29)); // STD	  %r20,-16(%r29)
	ADD_INST(code, pc, hppa_std(code, 19, -8, 29));  // STD	  %r19,-8(%r29)

	ADD_INST(code, pc, hppa_std(code, 0, invoke_rec_offset + G_STRUCT_OFFSET (MonoInvocation, parent), 30));
	ADD_INST(code, pc, hppa_std(code, 0, invoke_rec_offset + G_STRUCT_OFFSET (MonoInvocation, ex), 30));
	ADD_INST(code, pc, hppa_std(code, 0, invoke_rec_offset + G_STRUCT_OFFSET (MonoInvocation, ex_handler), 30));
	ADD_INST(code, pc, hppa_std(code, 0, invoke_rec_offset + G_STRUCT_OFFSET (MonoInvocation, ip), 30));

	if (data != NULL)
		data[0] = method;
	ADD_INST(code, pc, hppa_ldd(code, 0, 27, 19));
	ADD_INST(code, pc, hppa_std(code, 19, invoke_rec_offset + G_STRUCT_OFFSET (MonoInvocation, method), 30));

	if (sig->hasthis) {
		if (sig->call_convention != MONO_CALL_THISCALL)	{
			ADD_INST(code, pc, hppa_std(code, arg_reg, invoke_rec_offset + G_STRUCT_OFFSET (MonoInvocation, obj), 30));
			arg_val_pos += 8;
		} else {
			fprintf(stderr, "case I didn't handle 2\n");
		}
	}

	if (data != NULL)
		data[2] = (void *)stackval_from_data;

	for (i = 0; i < sig->param_count; ++i) {
		if (data != NULL)
			data[4 + i] = sig->params[i];
		ADD_INST(code, pc, hppa_ldd(code, (4 + i) * 8, 27, 26)); // LDD	  x(%r27),%r26 == type
		ADD_INST(code, pc, hppa_ldo(code, stack_val_pos, 30, 25)); // LDD 	x(%r30),%r25 == &stackval
		if (sig->params[i]->byref) {
			ADD_INST(code, pc, hppa_ldo(code, arg_val_pos, 3, 24));
		} else {
			int type = sig->params[i]->type;
		typeswitch:
			switch (type) {
			case MONO_TYPE_I8:
			case MONO_TYPE_U8:
			case MONO_TYPE_I:
			case MONO_TYPE_U:
			case MONO_TYPE_STRING:
			case MONO_TYPE_OBJECT:
			case MONO_TYPE_CLASS:
			case MONO_TYPE_SZARRAY:
			case MONO_TYPE_PTR:
			case MONO_TYPE_R8:
				ADD_INST(code, pc, hppa_ldo(code, arg_val_pos, 3, 24));
				break;
			case MONO_TYPE_I4:
			case MONO_TYPE_U4:
				ADD_INST(code, pc, hppa_ldo(code, arg_val_pos + 4, 3, 24));
				break;
			case MONO_TYPE_CHAR:
			case MONO_TYPE_I2:
			case MONO_TYPE_U2:
				ADD_INST(code, pc, hppa_ldo(code, arg_val_pos + 6, 3, 24));
				break;
			case MONO_TYPE_I1:
			case MONO_TYPE_U1:
			case MONO_TYPE_BOOLEAN:
				ADD_INST(code, pc, hppa_ldo(code, arg_val_pos + 7, 3, 24));
				break;
			case MONO_TYPE_VALUETYPE:
				if (sig->params [i]->data.klass->enumtype) {
					type = sig->params [i]->data.klass->enum_basetype->type;
					goto typeswitch;
				} else {
					int size = mono_class_native_size (sig->params[i]->data.klass, NULL);
					if (size <= 8)
						ADD_INST(code, pc, hppa_ldo(code, arg_val_pos, 3, 24));
					else {
						arg_val_pos += 15;
						arg_val_pos &= ~15;
						ADD_INST(code, pc, hppa_ldo(code, arg_val_pos, 3, 24));
					}

					arg_val_pos += size;
					arg_val_pos += 7;
					arg_val_pos &= ~7;
					arg_val_pos -=8 ; // as it is incremented later

					ADD_INST(code, pc, hppa_ldo(code, vtoffsets[i], 30, 19));
					ADD_INST(code, pc, hppa_std(code, 19, 0, 25));
				}
				break;
			default:
				fprintf(stderr, "can not cope in create method pointer %d\n", sig->params[i]->type);
				break;
			}
		}

		ADD_INST(code, pc, hppa_ldo(code, sig->pinvoke, 0, 23)); // LDI sig->pinvoke,%r23
		ADD_INST(code, pc, hppa_ldd(code, 16, 27, 19));	// LDD	   x(%r27),%r19 == stackval_from_data
		ADD_INST(code, pc, hppa_ldd(code, 16, 19, 20));	// LDD	   16(%r19),%r20   
		ADD_INST(code, pc, hppa_ldd(code, 24, 19, 27));	// LDD	   24(%r19),%r27   
		ADD_INST(code, pc, hppa_blve(code, 20));		// BVE,L   (%r20),%r2	   
		ADD_INST(code, pc, hppa_ldo(code, -16, 30, 29));	// LDO	   -16(%r30),%r29
		ADD_INST(code, pc, hppa_ldd(code, spill_offset + 8, 30, 27));

		stack_val_pos += sizeof (stackval);
		arg_val_pos += 8;
		g_assert(stack_val_pos < -96);
	}
        
	ADD_INST(code, pc, hppa_ldo(code, stack_vals_offset, 30, 19));
	ADD_INST(code, pc, hppa_std(code, 19, invoke_rec_offset + G_STRUCT_OFFSET (MonoInvocation, stack_args), 30));
	ADD_INST(code, pc, hppa_ldo(code, stack_val_pos, 30, 19));
	ADD_INST(code, pc, hppa_std(code, 19, invoke_rec_offset + G_STRUCT_OFFSET (MonoInvocation, retval), 30));

	if (sig->ret->type == MONO_TYPE_VALUETYPE && !sig->ret->data.klass->enumtype) {
		int size = mono_class_native_size (sig->ret->data.klass, NULL);
		// for large return structs pass on the pointer given us by our caller.
		if (size > 16)
			ADD_INST(code, pc, hppa_ldd(code, spill_offset + 16, 30, 28));
		else // use space left on stack for the return value
			ADD_INST(code, pc, hppa_ldo(code, stack_val_pos + sizeof(stackval), 30, 28));
		ADD_INST(code, pc, hppa_std(code, 28, stack_val_pos, 30));
	}

	ADD_INST(code, pc, hppa_ldo(code, invoke_rec_offset, 30, 26)); // address of invocation

	if (data != NULL)
		data[1] = (void *)ves_exec_method;
	ADD_INST(code, pc, hppa_ldd(code, 8, 27, 19));	// LDD	   8(%r27),%r19
	ADD_INST(code, pc, hppa_ldd(code, 16, 19, 20));	// LDD	   16(%r19),%r20   
	ADD_INST(code, pc, hppa_ldd(code, 24, 19, 27));	// LDD	   24(%r19),%r27   
	ADD_INST(code, pc, hppa_blve(code, 20));		// BVE,L   (%r20),%r2	   
	ADD_INST(code, pc, hppa_ldo(code, -16, 30, 29));	// LDO	   -16(%r30),%r29
	ADD_INST(code, pc, hppa_ldd(code, spill_offset + 8, 30, 27));
	if (sig->ret->byref) {
		fprintf(stderr, "can'ty cope with ret byref\n");
	} else {
		int simpletype = sig->ret->type;	
	enum_retvalue:
		switch (simpletype) {
		case MONO_TYPE_VOID:
			break;
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
		case MONO_TYPE_CHAR:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
			ADD_INST(code, pc, hppa_ldw(code, stack_val_pos, 30, 28)); // LDW 	x(%r30),%r28
			break;
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_STRING:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_PTR:
			ADD_INST(code, pc, hppa_ldd(code, stack_val_pos, 30, 28)); // LDD 	x(%r30),%r28
			break;
		case MONO_TYPE_R8:
			ADD_INST(code, pc, hppa_fldd(code, stack_val_pos, 30, 4)); // FLDD	 x(%r30),%fr4
			break;
		case MONO_TYPE_VALUETYPE:
			if (sig->ret->data.klass->enumtype) {
				simpletype = sig->ret->data.klass->enum_basetype->type;
				goto enum_retvalue;
			} else {
				int size = mono_class_native_size (sig->ret->data.klass, NULL);
				if (size <= 16) {
					ADD_INST(code, pc, hppa_ldd(code, stack_val_pos, 30, 28));
					if (size > 8)
						ADD_INST(code, pc, hppa_ldd(code, 8, 28, 29)); 
					ADD_INST(code, pc, hppa_ldd(code, 0, 28, 28)); 
				}
			}
			break;
		default:
			fprintf(stderr, "can't cope with ret type %d\n", simpletype);
			return NULL;
		}
	}

	ADD_INST(code, pc, hppa_ldd(code, -frame_size-16, 30, 2));
	ADD_INST(code, pc, hppa_ldd(code, spill_offset, 30, 4));
	ADD_INST(code, pc, hppa_bve(code, 2, 0));
	ADD_INST(code, pc, hppa_ldd_mb(code, -frame_size, 30, 3));
	if (code == NULL) {
		descriptor = (void **)malloc((8 + sig->param_count) * sizeof(void *) + sizeof(unsigned int) * pc);
		data = descriptor + 4;
		code = (unsigned int *)(data + 4 + sig->param_count);
		code_start = code;
		goto generate;
	}

	if (debug_asm) {
		fprintf(stderr, "generated: %d bytes\n", pc * 4);
		disassemble(code_start, pc);
	}

        flush_cache(code_start, 4 * pc);

	descriptor[0] = 0;
	descriptor[1] = 0;
	descriptor[2] = code_start;
	descriptor[3] = data;

	ji = g_new0 (MonoJitInfo, 1);
	ji->method = method;
	ji->code_size = 4; // does this matter?
	ji->code_start = descriptor;

	mono_jit_info_table_add (mono_get_root_domain (), ji);

	return ji->code_start;
}
#endif
