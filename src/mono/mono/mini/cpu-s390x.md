# S/390 cpu description file
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

adc: dest:i src1:i src2:i len:6
adc_imm: dest:i src1:i len:14
add.ovf.un: len:18 dest:i src1:i src2:i
add.ovf: len: 28 dest:i src1:i src2:i
add: dest:i src1:i src2:i len:12
add_imm: dest:i src1:i len:24
add_ovf_carry: dest:i src1:1 src2:i len:28
add_ovf_un_carry: dest:i src1:1 src2:i len:12
addcc: dest:i src1:i src2:i len:12
and: dest:i src1:i src2:i len:8
and_imm: dest:i src1:i len:24
aot_const: dest:i len:8
atomic_add_i4: src1:b src2:i dest:i len:28
atomic_add_i8: src1:b src2:i dest:i len:30
atomic_add_new_i4: src1:b src2:i dest:i len:28
atomic_add_new_i8: src1:b src2:i dest:i len:30
atomic_exchange_i4: src1:b src2:i dest:i len:18
atomic_exchange_i8: src1:b src2:i dest:i len:24
beq: len:8
bge.un: len:8
bge: len:8
bgt.un: len:8
bgt: len:8
ble.un: len:8
ble: len:8
blt.un: len:8
blt: len:8
bne.un: len:8
br: len:6
br_reg: src1:i len:8
break: len:22
call: dest:o clob:c len:26
call_handler: len:12 
call_membase: dest:o src1:b len:12 clob:c
call_reg: dest:o src1:i len:8 clob:c
ceq: dest:i len:12
cgt.un: dest:i len:12
cgt: dest:i len:12
checkthis: src1:b len:10
ckfinite: dest:f src1:f len:22
clt.un: dest:i len:12
clt: dest:i len:12
compare: src1:i src2:i len:4
compare_imm: src1:i len:20
cond_exc_c: len:8
cond_exc_eq: len:8
cond_exc_ge: len:8
cond_exc_ge_un: len:8
cond_exc_gt: len:8
cond_exc_gt_un: len:8
cond_exc_le: len:8
cond_exc_le_un: len:8
cond_exc_lt: len:8
cond_exc_lt_un: len:8
cond_exc_nc: len:8
cond_exc_ne_un: len:8
cond_exc_no: len:8
cond_exc_ov: len:8
conv.i1: dest:i src1:i len:30
conv.i2: dest:i src1:i len:36
conv.i4: dest:i src1:i len:4
conv.i8: dest:i src1:i len:4
conv.i: dest:i src1:i len:4
conv.ovf.i4.un: dest:i src1:i len:50
conv.ovf.u4: dest:i src1:i len:48
conv.ovf.u8.un: dest:i src1:i len:4
conv.ovf.u8: 
conv.r.un: dest:f src1:i len:32
conv.r4: dest:f src1:i len:4
conv.r8: dest:f src1:i len:4
conv.u1: dest:i src1:i len:14
conv.u2: dest:i src1:i len:24
conv.u4: dest:i src1:i len:4
conv.u8: dest:i src1:i len:4
conv.u:  dest:i src1:i len:4
div.un: dest:a src1:i src2:i len:12
div: dest:a src1:i src2:i len:10
div_imm: dest:i src1:i src2:i len:24
div_un_imm: dest:i src1:i src2:i len:24
endfinally: len:28
fcall: dest:g len:26 clob:c
fcall_membase: dest:g src1:b len:14 clob:c
fcall_reg: dest:g src1:i len:10 clob:c
fcompare: src1:f src2:f len:14
float_add: dest:f src1:f src2:f len:6
float_beq: len:10
float_bge: len:10
float_bge_un: len:8
float_bgt: len:10
float_ble: len:10
float_ble_un: len:8
float_blt: len:10
float_blt_un: len:8
float_bne_un: len:8
float_btg_un: len:8
float_ceq: dest:i src1:f src2:f len:16
float_cgt: dest:i src1:f src2:f len:16
float_cgt_un: dest:i src1:f src2:f len:16
float_clt: dest:i src1:f src2:f len:16
float_clt_un: dest:i src1:f src2:f len:16
float_conv_to_i1: dest:i src1:f len:50
float_conv_to_i2: dest:i src1:f len:50
float_conv_to_i4: dest:i src1:f len:50
float_conv_to_i8: dest:l src1:f len:50
float_conv_to_i: dest:i src1:f len:52
float_conv_to_r4: dest:f src1:f len:4
float_conv_to_u1: dest:i src1:f len:66
float_conv_to_u2: dest:i src1:f len:66
float_conv_to_u4: dest:i src1:f len:66
float_conv_to_u8: dest:i src1:f len:66
float_conv_to_u: dest:i src1:f len:36
float_div: dest:f src1:f src2:f len:6
float_div_un: dest:f src1:f src2:f len:6
float_mul: dest:f src1:f src2:f len:6
float_neg: dest:f src1:f len:6
float_not: dest:f src1:f len:6
float_rem: dest:f src1:f src2:f len:16
float_rem_un: dest:f src1:f src2:f len:16
float_sub: dest:f src1:f src2:f len:6
fmove: dest:f src1:f len:4
i8const: dest:i len:20
icompare: src1:i src2:i len:4
icompare_imm: src1:i src2:i len:14
iconst: dest:i len:40
int_adc: dest:i src1:i src2:i len:12
int_adc_imm: dest:i src1:i len:14
int_addcc: dest:i src1:i src2:i len:12
int_add: dest:i src1:i src2:i len:12
int_add_imm: dest:i src1:i len:20
int_and: dest:i src1:i src2:i len:12
int_and_imm: dest:i src1:i len:24
int_beq: len:8
int_bge: len:8
int_bge_un: len:8
int_bgt: len:8
int_bgt_un: len:8
int_ble: len:8
int_ble_un: len:8
int_blt: len:8
int_blt_un: len:8
int_bne_un: len:8
int_ceq: dest:i len:12
int_cgt: dest:i len:12
int_cgt_un: dest:i len:12
int_clt: dest:i len:12
int_clt_un: dest:i len:12
int_div: dest:a src1:i src2:i len:16
int_div_imm: dest:a src1:i len:24
int_div_un: dest:a src1:i src2:i len:16
int_div_un_imm: dest:a src1:i len:24
int_mul: dest:i src1:i src2:i len:16
int_mul_imm: dest:i src1:i len:24
int_mul_ovf: dest:i src1:i src2:i len:44
int_mul_ovf_un: dest:i src1:i src2:i len:22
int_neg: dest:i src1:i len:12
int_not: dest:i src1:i len:12
int_or: dest:i src1:i src2:i len:12
int_or_imm: dest:i src1:i len:24
int_rem: dest:d src1:i src2:i len:16
int_rem_imm: dest:d src1:i len:24
int_rem_un: dest:d src1:i src2:i len:16
int_rem_un_imm: dest:d src1:i len:24
int_sbb: dest:i src1:i src2:i len:6
int_sbb_imm: dest:i src1:i len:14
int_shl: dest:i src1:i src2:i clob:s len:10
int_shl_imm: dest:i src1:i len:10
int_shr: dest:i src1:i src2:i clob:s len:10
int_shr_imm: dest:i src1:i len:10
int_shr_un: dest:i src1:i src2:i clob:s len:10
int_shr_un_imm: dest:i src1:i len:10
int_subcc: dest:i src1:i src2:i len:12
int_sub: dest:i src1:i src2:i len:12
int_sub_imm: dest:i src1:i len:18
int_xor: dest:i src1:i src2:i len:12
int_xor_imm: dest:i src1:i len:24
jmp: len:40
label: len:0
lcall: dest:o len:22 clob:c
lcall_membase: dest:o src1:b len:12 clob:c
lcall_reg: dest:o src1:i len:8 clob:c
lcompare: src1:i src2:i len:4
ldind.i1: dest:i len:8
ldind.i2: dest:i len:8
ldind.i4: dest:i len:8
ldind.i: dest:i len:8
ldind.ref: dest:i len:8
ldind.u1: dest:i len:8
ldind.u2: dest:i len:8
ldind.u4: dest:i len:8
load_membase: dest:i src1:b len:26
loadi1_membase: dest:i src1:b len:40
loadi2_membase: dest:i src1:b len:26
loadi4_membase: dest:i src1:b len:26
loadi8_membase: dest:i src1:b len:26
loadr4_membase: dest:f src1:b len:28
loadr8_membase: dest:f src1:b len:28
loadu1_membase: dest:i src1:b len:26
loadu2_membase: dest:i src1:b len:26
loadu4_mem: dest:i len:8
loadu4_membase: dest:i src1:b len:26
localloc: dest:i src1:i len:106
long_add_ovf: len:14 dest:i src1:i src2:i
long_add_ovf_un: len:14 dest:i src1:i src2:i
long_conv_to_ovf_i: dest:i src1:i src2:i len:44
long_conv_to_r4: dest:f src1:i len:16
long_conv_to_r8: dest:f src1:i len:16
long_conv_to_r_un: dest:f src1:i src2:i len:37 
long_div: dest:i src1:i src2:i len:12
long_div_un: dest:i src1:i src2:i len:16
long_mul: dest:i src1:i src2:i len:12
long_mul_imm: dest:i src1:i src2:i len:20
long_mul_ovf: dest:i src1:i src2:i len:56 
long_mul_ovf_un: dest:i src1:i src2:i len:64 
long_neg: dest:i src1:i len:6
long_not: dest:i src1:i len:6
long_or: dest:i src1:i src2:i len:6
long_rem: dest:i src1:i src2:i len:12
long_rem_un: dest:i src1:i src2:i len:16
long_shl: dest:i src1:i src2:i len:14
long_shl_imm: dest:i src1:i len:14
long_shr_un: dest:i src1:i src2:i len:14
long_shr: dest:i src1:i src2:i len:14
long_shr_imm: dest:i src1:i len:14
long_shr_un_imm: dest:i src1:i len:14
long_sub: dest:i src1:i src2:i len:12
long_sub_imm: dest:i src1:i len:16
long_sub_ovf: len:14 dest:i src1:i src2:i
long_sub_ovf_un: len:14 dest:i src1:i src2:i
long_xor: dest:i src1:i len:6
memory_barrier: len: 10
move: dest:i src1:i len:4
mul.ovf.un: dest:i src1:i src2:i len:20
mul.ovf: dest:i src1:i src2:i len:42
mul: dest:i src1:i src2:i len:6
mul_imm: dest:i src1:i len:24
neg: dest:i src1:i len:6
nop: len:4
not: dest:i src1:i len:12
oparglist: src1:i len:28
bigmul: len:2 dest:i src1:a src2:i
bigmul_un: len:2 dest:i src1:a src2:i
endfilter: src1:i len:28
rethrow: src1:i len:26
or: dest:i src1:i src2:i len:8
or_imm: dest:i src1:i len:24
outarg: src1:i len:1
outarg_imm: len:5
r4const: dest:f len:26
r8const: dest:f len:24
rem.un: dest:d src1:i src2:i len:12
rem: dest:d src1:i src2:i len:10
rem_imm: dest:i src1:i src2:i len:24
rem_un_imm: dest:i src1:i src2:i len:24
s390_bkchain: len: 8 dest:i src1:i
s390_move: len:48 dest:b src1:b
s390_setf4ret: dest:f src1:f len:4
sbb: dest:i src1:i src2:i len:6
sbb_imm: dest:i src1:i len:14
setfreg: dest:f src1:f len:4
setreg: dest:i src1:i len:4
setregimm: dest:i len:20
setret: dest:a src1:i len:4
sext_i4: dest:i src1:i len:4
shl: dest:i src1:i src2:i clob:s len:6
shl_imm: dest:i src1:i len:8
shr.un: dest:i src1:i src2:i clob:s len:6
shr: dest:i src1:i src2:i clob:s len:6
shr_imm: dest:i src1:i len:10
shr_un_imm: dest:i src1:i len:8
sqrt: dest:f src1:f len:4
start_handler: len:26
stind.i1: src1:b src2:i
stind.i2: src1:b src2:i
stind.i4: src1:b src2:i
stind.r4: src1:b src2:f
stind.r8: src1:b src2:f
stind.ref: src1:b src2:i
store_membase_imm: dest:b len:46
store_membase_reg: dest:b src1:i len:26
storei1_membase_imm: dest:b len:46
storei1_membase_reg: dest:b src1:i len:26
storei2_membase_imm: dest:b len:46
storei2_membase_reg: dest:b src1:i len:26
storei4_membase_imm: dest:b len:46
storei4_membase_reg: dest:b src1:i len:26
storei8_membase_imm: dest:b len:46
storei8_membase_reg: dest:b src1:i len:26 
storer4_membase_reg: dest:b src1:f len:28
storer8_membase_reg: dest:b src1:f len:24
sub.ovf.un: len:16 dest:i src1:i src2:i 
sub.ovf: len:28 dest:i src1:i src2:i
sub: dest:i src1:i src2:i len:12
sub_imm: dest:i src1:i len:18
sub_ovf_carry: dest:i src1:1 src2:i len:28
sub_ovf_un_carry: dest:i src1:1 src2:i len:12
subcc: dest:i src1:i src2:i len:12
throw: src1:i len:26
vcall: len:22 clob:c
vcall_membase: src1:b len:12 clob:c
vcall_reg: src1:i len:8 clob:c
voidcall: len:22 clob:c
voidcall_membase: src1:b len:12 clob:c
voidcall_reg: src1:i len:8 clob:c
xor: dest:i src1:i src2:i len:8
xor_imm: dest:i src1:i len:20
