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
#include "mono/interpreter/interp.h"
#include "mono/metadata/appdomain.h"
#include "mono/metadata/tabledefs.h"

#if SIZEOF_VOID_P != 8
#error "HPPA code only currently supports 64bit pointers"
#endif

// debugging flag which dumps code generated 
static int debug_asm = 0;

#define NOP 0x08000240

#define LDB(disp, base, dest, neg) (0x40000000 | (((disp) & 0x1fff) << 1) | ((base) << 21) | ((dest) << 16) | neg)
#define STB(src, disp, base, neg) (0x60000000 | (((disp) & 0x1fff) << 1) | ((base) << 21) | ((src) << 16) | neg)

#define LDH(disp, base, dest, neg) (0x44000000 | (((disp) & 0x1fff) << 1) | ((base) << 21) | ((dest) << 16) | neg)
#define STH(src, disp, base, neg) (0x64000000 | (((disp) & 0x1fff) << 1) | ((base) << 21) | ((src) << 16) | neg)

#define LDW(disp, base, dest, neg) (0x48000000 | (((disp) & 0x1fff) << 1) | ((base) << 21) | ((dest) << 16) | neg)
#define STW(src, disp, base, neg) (0x68000000 | (((disp) & 0x1fff) << 1) | ((base) << 21) | ((src) << 16) | neg)

#define COPY(src, dest) 	  (0x34000000 | ((src) << 21) | ((dest) << 16))
#define LDD(im10a, base, dest, m, a, neg) (0x50000000 | (((im10a) & 0x3ff) << 4) | ((base) << 21) | ((dest) << 16) | neg | (m ? 0x8 : 0) | (a ? 0x4 : 0))
#define STD(src, im10a, base, m , a, neg) (0x70000000 | (((im10a) & 0x3ff) << 4) | ((base) << 21) | ((src) << 16) | neg | (m ? 0x8 : 0) | (a ? 0x4 : 0))

#define FLDD(im10a, base, dest, m, a, neg) (0x50000002 | (((im10a) & 0x3ff) << 4) | ((base) << 21) | ((dest) << 16) | neg | (m ? 0x8 : 0) | (a ? 0x4 : 0))
#define FSTD(src, im10a, base, m , a, neg) (0x70000002 | (((im10a) & 0x3ff) << 4) | ((base) << 21) | ((src) << 16) | neg | (m ? 0x8 : 0) | (a ? 0x4 : 0))

#define FLDW(im11a, base, dest, r, neg) (0x5c000000 | (((im11a) & 0x7ff) << 3) | ((base) << 21) | ((dest) << 16) | neg | ((r) ? 0x2 : 0))
#define FSTW(src, im11a, base, r, neg) (0x7c000000 | (((im11a) & 0x7ff) << 3) | ((base) << 21) | ((src) << 16) | neg | ((r) ? 0x2 : 0))

/* only works on right half SP registers */
#define FCNV(src, ssng, dest, dsng) (0x38000200 | ((src) << 21) | ((ssng) ? 0x80 : 0x800) | (dest) | ((dsng) ? 0x40 : 0x2000))

#define LDIL(im21, dest) (0x20000000 | im21 | ((dest) << 21))

#define LDO(off, base, dest, neg) (0x34000000 | (((off) & 0x1fff)) << 1 | ((base) << 21) | ((dest) << 16) | neg)

#define EXTRDU(src, pos, len, dest) (0xd8000000 | ((src) << 21) | ((dest) << 16) | ((pos) > 32 ? 0x800 : 0) | (((pos) & 31) << 5) | ((len) > 32 ? 0x1000 : 0) | (32 - (len & 31))) 

#define BVE(reg, link) (0xE8001000 | ((link ? 7 : 6) << 13) | ((reg) << 21))

static unsigned int gen_copy(int src, int dest)
{
	if (debug_asm)
		fprintf(stderr, "COPY %d,%d\n", src, dest);
	return COPY(src, dest);
}

static unsigned int gen_ldb(int disp, int base, int dest)
{
	int neg = disp < 0;
	if (debug_asm)
		fprintf(stderr, "LDB %d(%d),%d\n", disp, base, dest);
	return LDB(disp, base, dest, neg);
}

static unsigned int gen_stb(int src, int disp, int base)
{
	int neg = disp < 0;
	if (debug_asm)
		fprintf(stderr, "STB %d,%d(%d)\n", src, disp, base);
	return STB(src, disp, base, neg);
}

static unsigned int gen_ldh(int disp, int base, int dest)
{
	int neg = disp < 0;
	if (debug_asm)
		fprintf(stderr, "LDH %d(%d),%d\n", disp, base, dest);
	g_assert((disp & 1) == 0);
	return LDH(disp, base, dest, neg);
}

static unsigned int gen_sth(int src, int disp, int base)
{
	int neg = disp < 0;
	if (debug_asm)
		fprintf(stderr, "STH %d,%d(%d)\n", src, disp, base);
	g_assert((disp & 1) == 0);
	return STH(src, disp, base, neg);
}

static unsigned int gen_ldw(int disp, int base, int dest)
{
	int neg = disp < 0;
	if (debug_asm)
		fprintf(stderr, "LDW %d(%d),%d\n", disp, base, dest);
	g_assert((disp & 3) == 0);
	return LDW(disp, base, dest, neg);
}

static unsigned int gen_stw(int src, int disp, int base)
{
	int neg = disp < 0;
	if (debug_asm)
		fprintf(stderr, "STW %d,%d(%d)\n", src, disp, base);
	g_assert((disp & 3) == 0);
	return STW(src, disp, base, neg);
}

static unsigned int gen_ldd(int disp, int base, int dest)
{
	int neg = disp < 0;
	if (debug_asm)
		fprintf(stderr, "LDD %d(%d),%d\n", disp, base, dest);
	g_assert((disp & 7) == 0);
	return LDD(disp >> 3, base, dest, 0, 0, neg);
}

static unsigned int gen_lddmb(int disp, int base, int dest)
{
	int neg = disp < 0;
	if (debug_asm)
		fprintf(stderr, "LDD,MB %d(%d),%d\n", disp, base, dest);
	g_assert((disp & 7) == 0);
	return LDD(disp >> 3, base, dest, 1, 1, neg);
}

static unsigned int gen_std(int src, int disp, int base)
{
	int neg = disp < 0;
	g_assert((disp & 7) == 0);
	if (debug_asm)
		fprintf(stderr, "STD %d,%d(%d)\n", src, disp, base);
	return STD(src, disp >> 3, base, 0, 0, neg);
}

static unsigned int gen_fldd(int disp, int base, int dest)
{
	int neg = disp < 0;
	if (debug_asm)
		fprintf(stderr, "FLDD %d(%d),%d\n", disp, base, dest);
	g_assert((disp & 7) == 0);
	return FLDD(disp >> 3, base, dest, 0, 0, neg);
}

static unsigned int gen_fstd(int src, int disp, int base)
{
	int neg = disp < 0;
	g_assert((disp & 7) == 0);
	if (debug_asm)
		fprintf(stderr, "FSTD %d,%d(%d)\n", src, disp, base);
	return FSTD(src, disp >> 3, base, 0, 0, neg);
}

static unsigned int gen_fldw(int disp, int base, int dest)
{
	int neg = disp < 0;
	if (debug_asm)
		fprintf(stderr, "FLDW %d(%d),%dr\n", disp, base, dest);
	g_assert((disp & 3) == 0);
	return FLDW(disp >> 2, base, dest, 1, neg);
}

static unsigned int gen_fstw(int src, int disp, int base)
{
	int neg = disp < 0;
	g_assert((disp & 3) == 0);
	if (debug_asm)
		fprintf(stderr, "FSTW %dr,%d(%d)\n", src, disp, base);
	return FSTW(src, disp >> 2, base, 1, neg);
}

static unsigned int gen_fcnv_dbl_sng(int src, int dest)
{
	if (debug_asm)
		fprintf(stderr, "FCNV,DBL,SGL %d,%dr\n", src, dest);
	return FCNV(src, 0, dest, 1);
}

static unsigned int gen_fcnv_sng_dbl(int src, int dest)
{
	if (debug_asm)
		fprintf(stderr, "FCNV,SGL,DBL %dr,%d\n", src, dest);
	return FCNV(src, 1, dest, 0);
}

static unsigned int gen_stdma(int src, int disp, int base)
{
	int neg = disp < 0;
	g_assert((disp & 7) == 0);
	if (debug_asm)
		fprintf(stderr, "STD,MA %d,%d(%d)\n", src, disp, base);
	return STD(src, disp >> 3, base, 1, 0, neg);
}

/* load top 21 bits of val into reg */
static unsigned int gen_ldil(unsigned int val, int reg)
{
	unsigned int t = (val >> 11) & 0x1fffff;
	unsigned int im21 = ((t & 0x7c) << 14) | ((t & 0x180) << 7) | ((t & 0x3) << 12) | ((t & 0xffe00) >> 8) | ((t & 0x100000) >> 20);
	return LDIL(reg, im21);
}

static unsigned int gen_ldo(int off, int base, int reg)
{
	int neg = off < 0;
	if (debug_asm)
		fprintf(stderr, "LDO %d(%d),%d\n", off, base, reg);
	return LDO(off, base, reg, neg);
}

static unsigned int gen_nop(void)
{
	if (debug_asm)
		fprintf(stderr, "NOP\n");
	return NOP;
}

static unsigned int gen_bve(int reg, int link)
{
	if (debug_asm)
		fprintf(stderr, "BVE%s (%d)%s\n", link ? ",L" : "", reg, link ? ",2" : "");
	return BVE(reg, link);
}

static unsigned int gen_extrdu(int src, int pos, int len, int dest)
{
	if (debug_asm)
		fprintf(stderr, "EXTRD,U %d,%d,%d,%d\n", src, pos, len, dest);
	return EXTRDU(src, pos, len, dest);
}

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

#define ADD_INST(code, pc, gen_exp) ((code) == NULL ? (pc)++ : (code[(pc)++] = (gen_exp)))

/*
 * void func (void (*callme)(), void *retval, void *this_obj, stackval *arguments);
 */

MonoPIFunc
mono_create_trampoline (MonoMethodSignature *sig, gboolean string_ctor)
{
	int pc, save_pc;
	int param;
	void **descriptor;
	unsigned int *code = NULL;
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

	ADD_INST(code, pc, gen_std(2, -16, 30));  // STD	  %r2,-16(%r30)   
	ADD_INST(code, pc, gen_stdma(3, frame_size, 30));
	ADD_INST(code, pc, gen_std(4, spill_offset, 30));
	ADD_INST(code, pc, gen_std(5, spill_offset + 8, 30));
	ADD_INST(code, pc, gen_copy(29, 3));	   // COPY	  %r29,%r3		  
	ADD_INST(code, pc, gen_std(27, spill_offset + 16, 30));
	ADD_INST(code, pc, gen_nop()); 		   // NOP			  

	ADD_INST(code, pc, gen_std(26, -64, 29)); // STD	  %r26,-64(%r29)  callme
	ADD_INST(code, pc, gen_std(25, -56, 29)); // STD	  %r25,-56(%r29)  retval
	ADD_INST(code, pc, gen_std(24, -48, 29)); // STD	  %r24,-48(%r29)  this_obj
	ADD_INST(code, pc, gen_std(23, -40, 29)); // STD	  %r23,-40(%r29)  arguments

	if (sig->param_count > 0)
		ADD_INST(code, pc, gen_copy(23, 4));  // r4 is the current pointer to the stackval array of args

	if (sig->hasthis) {
		if (sig->call_convention != MONO_CALL_THISCALL) {
			ADD_INST(code, pc, gen_copy(24, arg_reg));
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
				ADD_INST(code, pc, gen_ldd(arg_offset, 4, 5));
				ADD_INST(code, pc, gen_std(5, parameter_slot, 30));
			} else {
				ADD_INST(code, pc, gen_ldd(arg_offset, 4, arg_reg));
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
				ADD_INST(code, pc, gen_ldw(arg_offset, 4, 5));
				switch (type) {
				case MONO_TYPE_I4:
				case MONO_TYPE_U4:
					ADD_INST(code, pc, gen_stw(5, parameter_slot + 4, 30));
					break;
				case MONO_TYPE_CHAR:
				case MONO_TYPE_I2:
				case MONO_TYPE_U2:
					ADD_INST(code, pc, gen_sth(5, parameter_slot + 6, 30));
					break;
				case MONO_TYPE_BOOLEAN:
				case MONO_TYPE_I1:
				case MONO_TYPE_U1:
					ADD_INST(code, pc, gen_stb(5, parameter_slot + 7, 30));
					break;
				}
			} else {
				ADD_INST(code, pc, gen_ldw(arg_offset, 4, arg_reg));
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
				ADD_INST(code, pc, gen_ldd(arg_offset, 4, 5));
				ADD_INST(code, pc, gen_std(5, parameter_slot, 30));
			} else {
				ADD_INST(code, pc, gen_ldd(arg_offset, 4, arg_reg));
				--arg_reg;
			}
			arg_offset += sizeof(stackval);
			parameter_slot += 8;
			break;
		case MONO_TYPE_R8:
			if (args_on_stack) {
				ADD_INST(code, pc, gen_ldd(arg_offset, 4, 5));
				ADD_INST(code, pc, gen_std(5, parameter_slot, 30));
			} else {
				ADD_INST(code, pc, gen_fldd(arg_offset, 4, FP_ARG_REG(arg_reg)));
				--arg_reg;
			}
			arg_offset += sizeof(stackval);
			parameter_slot += 8;
			break;
		case MONO_TYPE_R4:
			if (args_on_stack) {
				ADD_INST(code, pc, gen_fldd(arg_offset, 4, 22));
				ADD_INST(code, pc, gen_fcnv_dbl_sng(22, 22));
				ADD_INST(code, pc, gen_fstw(22, parameter_slot + 4, 30));
			} else {
				ADD_INST(code, pc, gen_fldd(arg_offset, 4, FP_ARG_REG(arg_reg)));
				ADD_INST(code, pc, gen_fcnv_dbl_sng(FP_ARG_REG(arg_reg), FP_ARG_REG(arg_reg)));
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
						ADD_INST(code, pc, gen_ldd(arg_offset, 4, 5));
						ADD_INST(code, pc, gen_ldd(0, 5, 5));
						ADD_INST(code, pc, gen_std(5, parameter_slot, 30));
					} else {
						ADD_INST(code, pc, gen_ldd(arg_offset, 4, arg_reg));
						ADD_INST(code, pc, gen_ldd(0, arg_reg, arg_reg));
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
					ADD_INST(code, pc, gen_ldd(arg_offset, 4, 5));
					// might generate a lot of code for very large structs... should
					// use a loop or routine call them
					while (size > 0) {
						if (args_on_stack) {
							ADD_INST(code, pc, gen_ldd(soffset, 5, 31));
							ADD_INST(code, pc, gen_std(31, parameter_slot, 30));
						} else {
							ADD_INST(code, pc, gen_ldd(soffset, 5, arg_reg));
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
			ADD_INST(code, pc, gen_ldd(-56, 3, 28));
			ADD_INST(code, pc, gen_ldd(0, 28, 28));
		}
	}

	ADD_INST(code, pc, gen_nop()); 		   // NOP			  
	ADD_INST(code, pc, gen_ldd(-64, 29, 5));
	ADD_INST(code, pc, gen_ldd(24, 5, 27));
	ADD_INST(code, pc, gen_ldd(16, 5, 5));
	ADD_INST(code, pc, gen_bve(5, 1));
	ADD_INST(code, pc, gen_ldo(parameter_offset + 64, 30, 29));
	ADD_INST(code, pc, gen_ldd(spill_offset + 16, 30, 27));
	ADD_INST(code, pc, gen_nop()); 		   // NOP			  
        
	if (string_ctor) {
		ADD_INST(code, pc, gen_ldd(-56, 3, 19)); // LDD	 -56(%r3),%r19	 
		ADD_INST(code, pc, gen_std(28, 0, 19));  // STD	 %r28,0(%r19)	 
	}
	else if (sig->ret->type != MONO_TYPE_VOID) {
		int type = sig->ret->type;

	rettypeswitch:
		switch (type) {
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
			ADD_INST(code, pc, gen_ldd(-56, 3, 19)); // LDD	 -56(%r3),%r19	 
			ADD_INST(code, pc, gen_stb(28, 0, 19));  // STB	 %r28,0(%r19)	 
			break;
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
			ADD_INST(code, pc, gen_ldd(-56, 3, 19)); // LDD	 -56(%r3),%r19	 
			ADD_INST(code, pc, gen_stw(28, 0, 19));  // STW	 %r28,0(%r19)	 
			break;
		case MONO_TYPE_CHAR:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
			ADD_INST(code, pc, gen_ldd(-56, 3, 19)); // LDD	 -56(%r3),%r19	 
			ADD_INST(code, pc, gen_sth(28, 0, 19));  // STH	 %r28,0(%r19)
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
			ADD_INST(code, pc, gen_ldd(-56, 3, 19)); // LDD	 -56(%r3),%r19	 
			ADD_INST(code, pc, gen_std(28, 0, 19));  // STD	 %r28,0(%r19)	 
			break;
		case MONO_TYPE_R8:
			ADD_INST(code, pc, gen_ldd(-56, 3, 19)); // LDD	 -56(%r3),%r19	 
			ADD_INST(code, pc, gen_fstd(4, 0, 19));  // FSTD	  %fr4,0(%r19)	  
			break;
		case MONO_TYPE_R4:
			ADD_INST(code, pc, gen_ldd(-56, 3, 19)); // LDD	 -56(%r3),%r19	 
			ADD_INST(code, pc, gen_fstw(4, 0, 19));  // FSTW	  %fr4r,0(%r19)    
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
					ADD_INST(code, pc, gen_ldd(-56, 3, 19));
					ADD_INST(code, pc, gen_ldd(0, 19, 19));
					if (size > 8) {
						ADD_INST(code, pc, gen_std(28, 0, 19)); 
						size -= 8;
						reg = 29;
						off += 8;
					}
					// get rest of value right aligned in the register
					ADD_INST(code, pc, gen_extrdu(reg, 8 * size - 1, 8 * size, reg));
					if ((size & 1) != 0) {
						ADD_INST(code, pc, gen_stb(reg, off + size - 1, 19));
						ADD_INST(code, pc, gen_extrdu(reg, 55, 56, reg));
						size -= 1;
					}
					if ((size & 2) != 0) {
						ADD_INST(code, pc, gen_sth(reg, off + size - 2, 19));
						ADD_INST(code, pc, gen_extrdu(reg, 47, 48, reg));
						size -= 2;
					}
					if ((size & 4) != 0)
						ADD_INST(code, pc, gen_stw(reg, off + size - 4, 19));
				}
				break;
			}
		default:
			g_error ("mono_create_trampoline: unhandled ret type %d", type);
			return NULL;
		}
	}

	ADD_INST(code, pc, gen_ldd(-frame_size-16, 30, 2));
	ADD_INST(code, pc, gen_ldd(spill_offset, 30, 4));
	ADD_INST(code, pc, gen_ldd(spill_offset + 8, 30, 5));
	ADD_INST(code, pc, gen_bve(2, 0));
	ADD_INST(code, pc, gen_lddmb(-frame_size, 30, 3));

	if (code == NULL) {
		descriptor = (void **)g_malloc(4 * sizeof(void *) + pc * sizeof(unsigned int));
		code = (unsigned int *)((char *)descriptor + 4 * sizeof(void *));
		save_pc = pc;
		goto generate;
        } else 
		g_assert(pc == save_pc);

	if (debug_asm)
		fprintf(stderr, "generated: %d bytes\n", pc * 4);

        // must do this so we can actually execute the code we just put in memory
	flush_cache(code, 4 * pc);

	descriptor[0] = 0;
	descriptor[1] = 0;
	descriptor[2] = code;
	descriptor[3] = 0;

	return (MonoPIFunc)descriptor;
}

void *
mono_create_method_pointer (MonoMethod *method)
{
	MonoMethodSignature *sig = method->signature;
	MonoJitInfo *ji;
	int i;
	int pc;
	int param;
	void **descriptor = NULL;
	void **data = NULL;
	unsigned int *code = NULL;
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

	/*
	 * If it is a static P/Invoke method, we can just return the pointer
	 * to the method implementation.
	 */
	if (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL && method->addr) {
		ji = g_new0 (MonoJitInfo, 1);
		ji->method = method;
		ji->code_size = 1;
		ji->code_start = method->addr;

		mono_jit_info_table_add (mono_root_domain, ji);
		return method->addr;
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

	ADD_INST(code, pc, gen_std(2, -16, 30));
	ADD_INST(code, pc, gen_stdma(3, frame_size, 30));
	ADD_INST(code, pc, gen_std(4, spill_offset, 30));
	ADD_INST(code, pc, gen_copy(29, 3));
	ADD_INST(code, pc, gen_std(27, spill_offset + 8, 30));
	ADD_INST(code, pc, gen_std(28, spill_offset + 16, 30));
	ADD_INST(code, pc, gen_nop());

	ADD_INST(code, pc, gen_std(26, -64, 29)); // STD	  %r26,-64(%r29)
	ADD_INST(code, pc, gen_std(25, -56, 29)); // STD	  %r25,-56(%r29)
	ADD_INST(code, pc, gen_std(24, -48, 29)); // STD	  %r24,-48(%r29)
	ADD_INST(code, pc, gen_std(23, -40, 29)); // STD	  %r23,-40(%r29)
	ADD_INST(code, pc, gen_std(22, -32, 29)); // STD	  %r22,-32(%r29)
	ADD_INST(code, pc, gen_std(21, -24, 29)); // STD	  %r21,-24(%r29)
	ADD_INST(code, pc, gen_std(20, -16, 29)); // STD	  %r20,-16(%r29)
	ADD_INST(code, pc, gen_std(19, -8, 29));  // STD	  %r19,-8(%r29)

	ADD_INST(code, pc, gen_std(0, invoke_rec_offset + G_STRUCT_OFFSET (MonoInvocation, parent), 30));
	ADD_INST(code, pc, gen_std(0, invoke_rec_offset + G_STRUCT_OFFSET (MonoInvocation, child), 30));
	ADD_INST(code, pc, gen_std(0, invoke_rec_offset + G_STRUCT_OFFSET (MonoInvocation, ex), 30));
	ADD_INST(code, pc, gen_std(0, invoke_rec_offset + G_STRUCT_OFFSET (MonoInvocation, ex_handler), 30));
	ADD_INST(code, pc, gen_std(0, invoke_rec_offset + G_STRUCT_OFFSET (MonoInvocation, ip), 30));

	if (data != NULL)
		data[0] = method;
	ADD_INST(code, pc, gen_ldd(0, 27, 19));
	ADD_INST(code, pc, gen_std(19, invoke_rec_offset + G_STRUCT_OFFSET (MonoInvocation, method), 30));

	if (sig->hasthis) {
		if (sig->call_convention != MONO_CALL_THISCALL)	{
			ADD_INST(code, pc, gen_std(arg_reg, invoke_rec_offset + G_STRUCT_OFFSET (MonoInvocation, obj), 30));
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
		ADD_INST(code, pc, gen_ldd((4 + i) * 8, 27, 26)); // LDD	  x(%r27),%r26 == type
		ADD_INST(code, pc, gen_ldo(stack_val_pos, 30, 25)); // LDD 	x(%r30),%r25 == &stackval
		if (sig->params[i]->byref) {
			ADD_INST(code, pc, gen_ldo(arg_val_pos, 3, 24));
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
				ADD_INST(code, pc, gen_ldo(arg_val_pos, 3, 24));
				break;
			case MONO_TYPE_I4:
			case MONO_TYPE_U4:
				ADD_INST(code, pc, gen_ldo(arg_val_pos + 4, 3, 24));
				break;
			case MONO_TYPE_CHAR:
			case MONO_TYPE_I2:
			case MONO_TYPE_U2:
				ADD_INST(code, pc, gen_ldo(arg_val_pos + 6, 3, 24));
				break;
			case MONO_TYPE_I1:
			case MONO_TYPE_U1:
			case MONO_TYPE_BOOLEAN:
				ADD_INST(code, pc, gen_ldo(arg_val_pos + 7, 3, 24));
				break;
			case MONO_TYPE_VALUETYPE:
				if (sig->params [i]->data.klass->enumtype) {
					type = sig->params [i]->data.klass->enum_basetype->type;
					goto typeswitch;
				} else {
					int size = mono_class_native_size (sig->params[i]->data.klass, NULL);
					if (size <= 8)
						ADD_INST(code, pc, gen_ldo(arg_val_pos, 3, 24));
					else {
						arg_val_pos += 15;
						arg_val_pos &= ~15;
						ADD_INST(code, pc, gen_ldo(arg_val_pos, 3, 24));
					}

					arg_val_pos += size;
					arg_val_pos += 7;
					arg_val_pos &= ~7;
					arg_val_pos -=8 ; // as it is incremented later

					ADD_INST(code, pc, gen_ldo(vtoffsets[i], 30, 19));
					ADD_INST(code, pc, gen_std(19, 0, 25));
				}
				break;
			default:
				fprintf(stderr, "can not cope in create method pointer %d\n", sig->params[i]->type);
				break;
			}
		}

		ADD_INST(code, pc, gen_ldo(sig->pinvoke, 0, 23)); // LDI sig->pinvoke,%r23
		ADD_INST(code, pc, gen_ldd(16, 27, 19));	// LDD	   x(%r27),%r19 == stackval_from_data
		ADD_INST(code, pc, gen_ldd(16, 19, 20));	// LDD	   16(%r19),%r20   
		ADD_INST(code, pc, gen_ldd(24, 19, 27));	// LDD	   24(%r19),%r27   
		ADD_INST(code, pc, gen_bve(20, 1));		// BVE,L   (%r20),%r2	   
		ADD_INST(code, pc, gen_ldo(-16, 30, 29));	// LDO	   -16(%r30),%r29
		ADD_INST(code, pc, gen_ldd(spill_offset + 8, 30, 27));

		stack_val_pos += sizeof (stackval);
		arg_val_pos += 8;
		g_assert(stack_val_pos < -96);
	}
        
	ADD_INST(code, pc, gen_ldo(stack_vals_offset, 30, 19));
	ADD_INST(code, pc, gen_std(19, invoke_rec_offset + G_STRUCT_OFFSET (MonoInvocation, stack_args), 30));
	ADD_INST(code, pc, gen_ldo(stack_val_pos, 30, 19));
	ADD_INST(code, pc, gen_std(19, invoke_rec_offset + G_STRUCT_OFFSET (MonoInvocation, retval), 30));

	if (sig->ret->type == MONO_TYPE_VALUETYPE && !sig->ret->data.klass->enumtype) {
		int size = mono_class_native_size (sig->ret->data.klass, NULL);
		// for large return structs pass on the pointer given us by our caller.
		if (size > 16)
			ADD_INST(code, pc, gen_ldd(spill_offset + 16, 30, 28));
		else // use space left on stack for the return value
			ADD_INST(code, pc, gen_ldo(stack_val_pos + sizeof(stackval), 30, 28));
		ADD_INST(code, pc, gen_std(28, stack_val_pos, 30));
	}

	ADD_INST(code, pc, gen_ldo(invoke_rec_offset, 30, 26)); // address of invocation

	if (data != NULL)
		data[1] = (void *)ves_exec_method;
	ADD_INST(code, pc, gen_ldd(8, 27, 19));	// LDD	   8(%r27),%r19
	ADD_INST(code, pc, gen_ldd(16, 19, 20));	// LDD	   16(%r19),%r20   
	ADD_INST(code, pc, gen_ldd(24, 19, 27));	// LDD	   24(%r19),%r27   
	ADD_INST(code, pc, gen_bve(20, 1));		// BVE,L   (%r20),%r2	   
	ADD_INST(code, pc, gen_ldo(-16, 30, 29));	// LDO	   -16(%r30),%r29
	ADD_INST(code, pc, gen_ldd(spill_offset + 8, 30, 27));
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
			ADD_INST(code, pc, gen_ldw(stack_val_pos, 30, 28)); // LDW 	x(%r30),%r28
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
			ADD_INST(code, pc, gen_ldd(stack_val_pos, 30, 28)); // LDD 	x(%r30),%r28
			break;
		case MONO_TYPE_R8:
			ADD_INST(code, pc, gen_fldd(stack_val_pos, 30, 4)); // FLDD	 x(%r30),%fr4
			break;
		case MONO_TYPE_VALUETYPE:
			if (sig->ret->data.klass->enumtype) {
				simpletype = sig->ret->data.klass->enum_basetype->type;
				goto enum_retvalue;
			} else {
				int size = mono_class_native_size (sig->ret->data.klass, NULL);
				if (size <= 16) {
					ADD_INST(code, pc, gen_ldd(stack_val_pos, 30, 28));
					if (size > 8)
						ADD_INST(code, pc, gen_ldd(8, 28, 29)); 
					ADD_INST(code, pc, gen_ldd(0, 28, 28)); 
				}
			}
			break;
		default:
			fprintf(stderr, "can't cope with ret type %d\n", simpletype);
			return NULL;
		}
	}

	ADD_INST(code, pc, gen_ldd(-frame_size-16, 30, 2));
	ADD_INST(code, pc, gen_ldd(spill_offset, 30, 4));
	ADD_INST(code, pc, gen_bve(2, 0));
	ADD_INST(code, pc, gen_lddmb(-frame_size, 30, 3));
	if (code == NULL) {
		descriptor = (void **)malloc((8 + sig->param_count) * sizeof(void *) + sizeof(unsigned int) * pc);
		data = descriptor + 4;
		code = (unsigned int *)(data + 4 + sig->param_count);
		goto generate;
	}

        flush_cache(code, 4 * pc);

	descriptor[0] = 0;
	descriptor[1] = 0;
	descriptor[2] = code;
	descriptor[3] = data;

	ji = g_new0 (MonoJitInfo, 1);
	ji->method = method;
	ji->code_size = 4; // does this matter?
	ji->code_start = descriptor;

	mono_jit_info_table_add (mono_root_domain, ji);

	return ji->code_start;
}
