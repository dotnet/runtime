# Copyright 2003-2011 Novell, Inc (http://www.novell.com)
# Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
# arm cpu description file
# this file is read by genmdesc to pruduce a table with all the relevant information
# about the cpu instructions that may be used by the regsiter allocator, the scheduler
# and other parts of the arch-dependent part of mini.
#
# An opcode name is followed by a colon and optional specifiers.
# A specifier has a name, a colon and a value. Specifiers are separated by white space.
# Here is a description of the specifiers valid for this file and their possible values.
#
# dest:register       describes the destination register of an instruction
# src1:register       describes the first source register of an instruction
# src2:register       describes the second source register of an instruction
#
# register may have the following values:
#	i  integer register
#	a  r0 register (first argument/result reg)
#	b  base register (used in address references)
#	f  floating point register
#	g  floating point register returned in r0:r1 for soft-float mode
#
# len:number         describe the maximun length in bytes of the instruction
# number is a positive integer
#
# cost:number        describe how many cycles are needed to complete the instruction (unused)
#
# clob:spec          describe if the instruction clobbers registers or has special needs
#
# spec can be one of the following characters:
#	c  clobbers caller-save registers
#	r  'reserves' the destination register until a later instruction unreserves it
#          used mostly to set output registers in function calls
#
# flags:spec        describe if the instruction uses or sets the flags (unused)
#
# spec can be one of the following chars:
# 	s  sets the flags
#       u  uses the flags
#       m  uses and modifies the flags
#
# res:spec          describe what units are used in the processor (unused)
#
# delay:            describe delay slots (unused)
#
# the required specifiers are: len, clob (if registers are clobbered), the registers
# specifiers if the registers are actually used, flags (when scheduling is implemented).
#
# See the code in mini-x86.c for more details on how the specifiers are used.
#
nop: len:4
relaxed_nop: len:4
break: len:4
br: len:16
switch: src1:i len:12
# See the comment in resume_from_signal_handler, we can't copy the fp regs from sigctx to MonoContext on linux,
# since the corresponding sigctx structures are not well defined.
seq_point: len:52 clob:c
il_seq_point: len:0

throw: src1:i len:24
rethrow: src1:i len:20
start_handler: len:20
endfinally: len:32
call_handler: len:16 clob:c
endfilter: src1:i len:16
get_ex_obj: dest:i len:16

ckfinite: dest:f src1:f len:112
ceq: dest:i len:12
cgt: dest:i len:12
cgt_un: dest:i len:12
clt: dest:i len:12
clt_un: dest:i len:12
localloc: dest:i src1:i len:60
compare: src1:i src2:i len:4
compare_imm: src1:i len:12
fcompare: src1:f src2:f len:12
rcompare: src1:f src2:f len:12
arglist: src1:i len:12
setlret: src1:i src2:i len:12
check_this: src1:b len:4
call: dest:a clob:c len:20
call_reg: dest:a src1:i len:8 clob:c
call_membase: dest:a src1:b len:30 clob:c
voidcall: len:20 clob:c
voidcall_reg: src1:i len:8 clob:c
voidcall_membase: src1:b len:24 clob:c
fcall: dest:g len:28 clob:c
fcall_reg: dest:g src1:i len:16 clob:c
fcall_membase: dest:g src1:b len:30 clob:c
rcall: dest:g len:28 clob:c
rcall_reg: dest:g src1:i len:16 clob:c
rcall_membase: dest:g src1:b len:30 clob:c
lcall: dest:l len:20 clob:c
lcall_reg: dest:l src1:i len:8 clob:c
lcall_membase: dest:l src1:b len:24 clob:c
vcall: len:64 clob:c
vcall_reg: src1:i len:64 clob:c
vcall_membase: src1:b len:70 clob:c

tailcall: len:255 clob:c # FIXME len
tailcall_membase: src1:b len:255 clob:c # FIXME len
tailcall_reg: src1:b len:255 clob:c # FIXME len

# tailcall_parameter models the size of moving one parameter,
# so that the required size of a branch around a tailcall can
# be accurately estimated; something like:
# void f1(volatile long *a)
# {
# a[large] = a[another large]
# }
#
# In current implementation with 4K limit this is typically
# two full instructions, howevever raising the limit some
# can lead two instructions and two thumb instructions.
# FIXME A fixed size sequence to move parameters would moot this.
tailcall_parameter: len:12

iconst: dest:i len:16
r4const: dest:f len:24
r8const: dest:f len:20
label: len:0
store_membase_imm: dest:b len:20
store_membase_reg: dest:b src1:i len:20
storei1_membase_imm: dest:b len:20
storei1_membase_reg: dest:b src1:i len:12
storei2_membase_imm: dest:b len:20
storei2_membase_reg: dest:b src1:i len:12
storei4_membase_imm: dest:b len:20
storei4_membase_reg: dest:b src1:i len:20
storei8_membase_imm: dest:b
storei8_membase_reg: dest:b src1:i
storer4_membase_reg: dest:b src1:f len:60
storer8_membase_reg: dest:b src1:f len:24
store_memindex: dest:b src1:i src2:i len:4
storei1_memindex: dest:b src1:i src2:i len:4
storei2_memindex: dest:b src1:i src2:i len:4
storei4_memindex: dest:b src1:i src2:i len:4
load_membase: dest:i src1:b len:20
loadi1_membase: dest:i src1:b len:4
loadu1_membase: dest:i src1:b len:4
loadi2_membase: dest:i src1:b len:4
loadu2_membase: dest:i src1:b len:4
loadi4_membase: dest:i src1:b len:4
loadu4_membase: dest:i src1:b len:4
loadi8_membase: dest:i src1:b
loadr4_membase: dest:f src1:b len:56
loadr8_membase: dest:f src1:b len:24
load_memindex: dest:i src1:b src2:i len:4
loadi1_memindex: dest:i src1:b src2:i len:4
loadu1_memindex: dest:i src1:b src2:i len:4
loadi2_memindex: dest:i src1:b src2:i len:4
loadu2_memindex: dest:i src1:b src2:i len:4
loadi4_memindex: dest:i src1:b src2:i len:4
loadu4_memindex: dest:i src1:b src2:i len:4
loadu4_mem: dest:i len:8
move: dest:i src1:i len:4
fmove: dest:f src1:f len:4
move_f_to_i4: dest:i src1:f len:28
move_i4_to_f: dest:f src1:i len:8
add_imm: dest:i src1:i len:12
sub_imm: dest:i src1:i len:12
mul_imm: dest:i src1:i len:12
and_imm: dest:i src1:i len:12
or_imm: dest:i src1:i len:12
xor_imm: dest:i src1:i len:12
shl_imm: dest:i src1:i len:8
shr_imm: dest:i src1:i len:8
shr_un_imm: dest:i src1:i len:8
cond_exc_eq: len:8
cond_exc_ne_un: len:8
cond_exc_lt: len:8
cond_exc_lt_un: len:8
cond_exc_gt: len:8
cond_exc_gt_un: len:8
cond_exc_ge: len:8
cond_exc_ge_un: len:8
cond_exc_le: len:8
cond_exc_le_un: len:8
cond_exc_ov: len:12
cond_exc_no: len:8
cond_exc_c: len:12
cond_exc_nc: len:8
#float_beq: src1:f src2:f len:20
#float_bne_un: src1:f src2:f len:20
#float_blt: src1:f src2:f len:20
#float_blt_un: src1:f src2:f len:20
#float_bgt: src1:f src2:f len:20
#float_bgt_un: src1:f src2:f len:20
#float_bge: src1:f src2:f len:20
#float_bge_un: src1:f src2:f len:20
#float_ble: src1:f src2:f len:20
#float_ble_un: src1:f src2:f len:20
float_add: dest:f src1:f src2:f len:4
float_sub: dest:f src1:f src2:f len:4
float_mul: dest:f src1:f src2:f len:4
float_div: dest:f src1:f src2:f len:4
float_div_un: dest:f src1:f src2:f len:4
float_rem: dest:f src1:f src2:f len:16
float_rem_un: dest:f src1:f src2:f len:16
float_neg: dest:f src1:f len:4
float_not: dest:f src1:f len:4
float_conv_to_i1: dest:i src1:f len:88
float_conv_to_i2: dest:i src1:f len:88
float_conv_to_i4: dest:i src1:f len:88
float_conv_to_i8: dest:l src1:f len:88
float_conv_to_r4: dest:f src1:f len:8
float_conv_to_u4: dest:i src1:f len:88
float_conv_to_u8: dest:l src1:f len:88
float_conv_to_u2: dest:i src1:f len:88
float_conv_to_u1: dest:i src1:f len:88
float_conv_to_i: dest:i src1:f len:40
float_ceq: dest:i src1:f src2:f len:16
float_cgt: dest:i src1:f src2:f len:16
float_cgt_un: dest:i src1:f src2:f len:20
float_clt: dest:i src1:f src2:f len:16
float_clt_un: dest:i src1:f src2:f len:20
float_cneq: dest:y src1:f src2:f len:20
float_cge: dest:y src1:f src2:f len:20
float_cle: dest:y src1:f src2:f len:20
float_conv_to_u: dest:i src1:f len:36

# R4 opcodes
rmove: dest:f src1:f len:4
r4_conv_to_i1: dest:i src1:f len:88
r4_conv_to_i2: dest:i src1:f len:88
r4_conv_to_i4: dest:i src1:f len:88
r4_conv_to_u1: dest:i src1:f len:88
r4_conv_to_u2: dest:i src1:f len:88
r4_conv_to_u4: dest:i src1:f len:88
r4_conv_to_r4: dest:f src1:f len:16
r4_conv_to_r8: dest:f src1:f len:16
r4_add: dest:f src1:f src2:f len:4
r4_sub: dest:f src1:f src2:f len:4
r4_mul: dest:f src1:f src2:f len:4
r4_div: dest:f src1:f src2:f len:4
r4_rem: dest:f src1:f src2:f len:16
r4_neg: dest:f src1:f len:4
r4_ceq: dest:i src1:f src2:f len:16
r4_cgt: dest:i src1:f src2:f len:16
r4_cgt_un: dest:i src1:f src2:f len:20
r4_clt: dest:i src1:f src2:f len:16
r4_clt_un: dest:i src1:f src2:f len:20
r4_cneq: dest:y src1:f src2:f len:20
r4_cge: dest:y src1:f src2:f len:20
r4_cle: dest:y src1:f src2:f len:20

setfret: src1:f len:12
aotconst: dest:i len:16
objc_get_selector: dest:i len:32
sqrt: dest:f src1:f len:4
adc: dest:i src1:i src2:i len:4
addcc: dest:i src1:i src2:i len:4
subcc: dest:i src1:i src2:i len:4
adc_imm: dest:i src1:i len:12
addcc_imm: dest:i src1:i len:12
subcc_imm: dest:i src1:i len:12
sbb: dest:i src1:i src2:i len:4
sbb_imm: dest:i src1:i len:12
br_reg: src1:i len:8
bigmul: len:8 dest:l src1:i src2:i
bigmul_un: len:8 dest:l src1:i src2:i
tls_get: len:16 dest:i
tls_set: len:16 src1:i clob:c

# 32 bit opcodes
int_add: dest:i src1:i src2:i len:4
int_sub: dest:i src1:i src2:i len:4
int_mul: dest:i src1:i src2:i len:4
int_div: dest:i src1:i src2:i len:4
int_div_un: dest:i src1:i src2:i len:4
int_rem: dest:i src1:i src2:i len:8
int_rem_un: dest:i src1:i src2:i len:8
int_and: dest:i src1:i src2:i len:4
int_or: dest:i src1:i src2:i len:4
int_xor: dest:i src1:i src2:i len:4
int_shl: dest:i src1:i src2:i len:4
int_shr: dest:i src1:i src2:i len:4
int_shr_un: dest:i src1:i src2:i len:4
int_neg: dest:i src1:i len:4
int_not: dest:i src1:i len:4
int_conv_to_i1: dest:i src1:i len:8
int_conv_to_i2: dest:i src1:i len:8
int_conv_to_i4: dest:i src1:i len:4
int_conv_to_r4: dest:f src1:i len:84
int_conv_to_r8: dest:f src1:i len:84
int_conv_to_u4: dest:i src1:i
int_conv_to_r_un: dest:f src1:i len:56
int_conv_to_u2: dest:i src1:i len:8
int_conv_to_u1: dest:i src1:i len:4
int_beq: len:16
int_bge: len:16
int_bgt: len:16
int_ble: len:16
int_blt: len:16
int_bne_un: len:16
int_bge_un: len:16
int_bgt_un: len:16
int_ble_un: len:16
int_blt_un: len:16
int_add_ovf: dest:i src1:i src2:i len:16
int_add_ovf_un: dest:i src1:i src2:i len:16
int_mul_ovf: dest:i src1:i src2:i len:16
int_mul_ovf_un: dest:i src1:i src2:i len:16
int_sub_ovf: dest:i src1:i src2:i len:16
int_sub_ovf_un: dest:i src1:i src2:i len:16
add_ovf_carry: dest:i src1:i src2:i len:16
sub_ovf_carry: dest:i src1:i src2:i len:16
add_ovf_un_carry: dest:i src1:i src2:i len:16
sub_ovf_un_carry: dest:i src1:i src2:i len:16

arm_rsbs_imm: dest:i src1:i len:4
arm_rsc_imm: dest:i src1:i len:4

# Linear IR opcodes
dummy_use: src1:i len:0
dummy_iconst: dest:i len:0
dummy_r8const: dest:f len:0
dummy_r4const: dest:f len:0
not_reached: len:0
not_null: src1:i len:0

int_adc: dest:i src1:i src2:i len:4
int_addcc: dest:i src1:i src2:i len:4
int_subcc: dest:i src1:i src2:i len:4
int_sbb: dest:i src1:i src2:i len:4
int_adc_imm: dest:i src1:i len:12
int_sbb_imm: dest:i src1:i len:12

int_add_imm: dest:i src1:i len:12
int_sub_imm: dest:i src1:i len:12
int_mul_imm: dest:i src1:i len:12
int_div_imm: dest:i src1:i len:20
int_div_un_imm: dest:i src1:i len:12
int_rem_imm: dest:i src1:i len:28
int_rem_un_imm: dest:i src1:i len:16
int_and_imm: dest:i src1:i len:12
int_or_imm: dest:i src1:i len:12
int_xor_imm: dest:i src1:i len:12
int_shl_imm: dest:i src1:i len:8
int_shr_imm: dest:i src1:i len:8
int_shr_un_imm: dest:i src1:i len:8

int_ceq: dest:i len:12
int_cgt: dest:i len:12
int_cgt_un: dest:i len:12
int_clt: dest:i len:12
int_clt_un: dest:i len:12

int_cneq: dest:i len:12
int_cge: dest:i len:12
int_cle: dest:i len:12
int_cge_un: dest:i len:12
int_cle_un: dest:i len:12

cond_exc_ieq: len:16
cond_exc_ine_un: len:16
cond_exc_ilt: len:16
cond_exc_ilt_un: len:16
cond_exc_igt: len:16
cond_exc_igt_un: len:16
cond_exc_ige: len:16
cond_exc_ige_un: len:16
cond_exc_ile: len:16
cond_exc_ile_un: len:16
cond_exc_iov: len:20
cond_exc_ino: len:16
cond_exc_ic: len:20
cond_exc_inc: len:16

icompare: src1:i src2:i len:4
icompare_imm: src1:i len:12

long_conv_to_ovf_i4_2: dest:i src1:i src2:i len:36

vcall2: len:64 clob:c
vcall2_reg: src1:i len:64 clob:c
vcall2_membase: src1:b len:64 clob:c
dyn_call: src1:i src2:i len:252 clob:c

# This is different from the original JIT opcodes
float_beq: len:32
float_bne_un: len:32
float_blt: len:32
float_blt_un: len:32
float_bgt: len:32
float_bgt_un: len:32
float_bge: len:32
float_bge_un: len:32
float_ble: len:32
float_ble_un: len:32

liverange_start: len:0
liverange_end: len:0
gc_liveness_def: len:0
gc_liveness_use: len:0
gc_spill_slot_liveness_def: len:0
gc_param_slot_liveness_def: len:0
gc_safe_point: clob:c src1:i len:40

atomic_add_i4: dest:i src1:i src2:i len:64
atomic_exchange_i4: dest:i src1:i src2:i len:64
atomic_cas_i4: dest:i src1:i src2:i src3:i len:64
memory_barrier: len:8 clob:a
atomic_load_i1: dest:i src1:b len:28
atomic_load_u1: dest:i src1:b len:28
atomic_load_i2: dest:i src1:b len:28
atomic_load_u2: dest:i src1:b len:28
atomic_load_i4: dest:i src1:b len:28
atomic_load_u4: dest:i src1:b len:28
atomic_load_r4: dest:f src1:b len:80
atomic_load_r8: dest:f src1:b len:32
atomic_store_i1: dest:b src1:i len:28
atomic_store_u1: dest:b src1:i len:28
atomic_store_i2: dest:b src1:i len:28
atomic_store_u2: dest:b src1:i len:28
atomic_store_i4: dest:b src1:i len:28
atomic_store_u4: dest:b src1:i len:28
atomic_store_r4: dest:b src1:f len:80
atomic_store_r8: dest:b src1:f len:32

generic_class_init: src1:a len:44 clob:c

fill_prof_call_ctx: src1:i len:128
