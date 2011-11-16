# Copyright 2003-2011 Novell, Inc (http://www.novell.com)
# Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
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
#	a  r3 register (output from calls)
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
memory_barrier: len:8 clob:a
nop: len:4
relaxed_nop: len:4
break: len:4
jmp: len:92
br: len:4
switch: src1:i len:8
seq_point: len:38

throw: src1:i len:24
rethrow: src1:i len:20
start_handler: len:20
endfinally: len:20
call_handler: len:12 clob:c
endfilter: src1:i len:16

ckfinite: dest:f src1:f len:64
ceq: dest:i len:12
cgt: dest:i len:12
cgt.un: dest:i len:12
clt: dest:i len:12
clt.un: dest:i len:12
localloc: dest:i src1:i len:60
compare: src1:i src2:i len:4
compare_imm: src1:i len:12
fcompare: src1:f src2:f len:12
oparglist: src1:i len:12
setlret: src1:i src2:i len:12
checkthis: src1:b len:4
call: dest:a clob:c len:20
call_reg: dest:a src1:i len:8 clob:c
call_membase: dest:a src1:b len:12 clob:c
voidcall: len:20 clob:c
voidcall_reg: src1:i len:8 clob:c
voidcall_membase: src1:b len:12 clob:c
fcall: dest:g len:28 clob:c
fcall_reg: dest:g src1:i len:16 clob:c
fcall_membase: dest:g src1:b len:20 clob:c
lcall: dest:l len:20 clob:c
lcall_reg: dest:l src1:i len:8 clob:c
lcall_membase: dest:l src1:b len:12 clob:c
vcall: len:20 clob:c
vcall_reg: src1:i len:8 clob:c
vcall_membase: src1:b len:12 clob:c
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
storer4_membase_reg: dest:b src1:f len:12
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
loadr4_membase: dest:f src1:b len:8
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
float_conv_to_i1: dest:i src1:f len:40
float_conv_to_i2: dest:i src1:f len:40
float_conv_to_i4: dest:i src1:f len:40
float_conv_to_i8: dest:l src1:f len:40
float_conv_to_r4: dest:f src1:f len:8
float_conv_to_u4: dest:i src1:f len:40
float_conv_to_u8: dest:l src1:f len:40
float_conv_to_u2: dest:i src1:f len:40
float_conv_to_u1: dest:i src1:f len:40
float_conv_to_i: dest:i src1:f len:40
float_ceq: dest:i src1:f src2:f len:16
float_cgt: dest:i src1:f src2:f len:16
float_cgt_un: dest:i src1:f src2:f len:20
float_clt: dest:i src1:f src2:f len:16
float_clt_un: dest:i src1:f src2:f len:20
float_conv_to_u: dest:i src1:f len:36
setfret: src1:f len:12
aot_const: dest:i len:16
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
tls_get: len:8 dest:i clob:c

# 32 bit opcodes
int_add: dest:i src1:i src2:i len:4
int_sub: dest:i src1:i src2:i len:4
int_mul: dest:i src1:i src2:i len:4
int_div: dest:i src1:i src2:i len:40
int_div_un: dest:i src1:i src2:i len:16
int_rem: dest:i src1:i src2:i len:48
int_rem_un: dest:i src1:i src2:i len:24
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
int_conv_to_r4: dest:f src1:i len:36
int_conv_to_r8: dest:f src1:i len:36
int_conv_to_u4: dest:i src1:i
int_conv_to_r_un: dest:f src1:i len:56
int_conv_to_u2: dest:i src1:i len:8
int_conv_to_u1: dest:i src1:i len:4
int_beq: len:8
int_bge: len:8
int_bgt: len:8
int_ble: len:8
int_blt: len:8
int_bne_un: len:8
int_bge_un: len:8
int_bgt_un: len:8
int_ble_un: len:8
int_blt_un: len:8
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
dummy_store: len:0
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

cond_exc_ieq: len:8
cond_exc_ine_un: len:8
cond_exc_ilt: len:8
cond_exc_ilt_un: len:8
cond_exc_igt: len:8
cond_exc_igt_un: len:8
cond_exc_ige: len:8
cond_exc_ige_un: len:8
cond_exc_ile: len:8
cond_exc_ile_un: len:8
cond_exc_iov: len:12
cond_exc_ino: len:8
cond_exc_ic: len:12
cond_exc_inc: len:8

icompare: src1:i src2:i len:4
icompare_imm: src1:i len:12

long_conv_to_ovf_i4_2: dest:i src1:i src2:i len:36

vcall2: len:20 clob:c
vcall2_reg: src1:i len:8 clob:c
vcall2_membase: src1:b len:12 clob:c
dyn_call: src1:i src2:i len:120 clob:c

# This is different from the original JIT opcodes
float_beq: len:20
float_bne_un: len:20
float_blt: len:20
float_blt_un: len:20
float_bgt: len:20
float_bgt_un: len:20
float_bge: len:20
float_bge_un: len:20
float_ble: len:20
float_ble_un: len:20

liverange_start: len:0
liverange_end: len:0
gc_liveness_def: len:0
gc_liveness_use: len:0
gc_spill_slot_liveness_def: len:0
gc_param_slot_liveness_def: len:0
