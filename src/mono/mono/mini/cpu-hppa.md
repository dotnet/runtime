# hppa cpu description file
# this file is read by genmdesc to pruduce a table with all the relevant information
# about the cpu instructions that may be used by the register allocator, the scheduler
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
#	a  r28 register (output from calls)
#	b  base register (used in address references)
#	f  floating point register
#   L  register pair
#   o  %r0
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
# See the code in mini-hppa.c for more details on how the specifiers are used.
#
relaxed_nop: len:0
label: len:0
break: len:64
jmp: len:64
br: len:16
beq: len:8
bge: len:8
bgt: len:8
ble: len:8
blt: len:8
bne.un: len:64
bge.un: len:64
bgt.un: len:64
ble.un: len:64
blt.un: len:64
switch: src1:i len:40
add: dest:i src1:i src2:i len:64
sub: dest:i src1:i src2:i len:4
mul: dest:i src1:i src2:i len:4
div: dest:i src1:i src2:i len:64
div.un: dest:i src1:i src2:i len:8
rem: dest:d src1:i src2:i len:64
rem.un: dest:d src1:i src2:i len:64
and: dest:i src1:i src2:i len:4
or: dest:i src1:i src2:i len:4
xor: dest:i src1:i src2:i len:4
shl: dest:i src1:i src2:i clob:1 len:16
shr: dest:i src1:i src2:i clob:1 len:16
shr.un: dest:i src1:i src2:i clob:1 len:16
neg: dest:i src1:i len:4
not: dest:i src1:i len:4
conv.i1: dest:i src1:i len:8
conv.i2: dest:i src1:i len:8
conv.i4: dest:i src1:i len:4
conv.i8: dest:i src1:i len:4
conv.r4: dest:f src1:i len:64
conv.r8: dest:f src1:i len:64
conv.u4: dest:i src1:i len:4
conv.u8: dest:i src1:i len:4
throw: src1:i len:64
rethrow: src1:i len:64
conv.ovf.u4: dest:i src1:i len:64
ckfinite: dest:f src1:f len:40
conv.u2: dest:i src1:i len:8
conv.u1: dest:i src1:i len:4
conv.i: dest:i src1:i len:4
mul.ovf: dest:i src1:i src2:i len:64
mul.ovf.un: dest:i src1:i src2:i len:64
start_handler: len:64
endfinally: len:64
conv.u: dest:i src1:i len:4
arglist: src1:i
ceq: dest:i len:64
cgt: dest:i len:64
cgt.un: dest:i len:64
clt: dest:i len:64
clt.un: dest:i len:64
localloc: dest:i src1:i len:64
compare: src1:i src2:i len:4
icompare: src1:i src2:i len:4
compare_imm: src1:i len:64
icompare_imm: src1:i len:64
fcompare: src1:f src2:f len:64
lcompare: src1:i src2:i len:4
setfret: dest:f src1:f len:8
setlret: dest:a src1:i len:8
checkthis: src1:b len:4
oparglist: src1:i len:64
call: dest:a clob:c len:32
call_reg: dest:a src1:i len:64 clob:c
call_membase: dest:a src1:b len:64 clob:c
voidcall: len:64 clob:c
voidcall_reg: src1:i len:64 clob:c
voidcall_membase: src1:b len:64 clob:c
fcall: dest:f len:64 clob:c
fcall_reg: dest:f src1:i len:64 clob:c
fcall_membase: dest:f src1:b len:64 clob:c
lcall: dest:L len:42 clob:c
lcall_reg: dest:L src1:i len:64 clob:c
lcall_membase: dest:L src1:b len:64 clob:c
vcall: len:40 clob:c
vcall_reg: src1:i len:64 clob:c
vcall_membase: src1:b len:64 clob:c
iconst: dest:i len:64
i8const: dest:i len:64
r4const: dest:f len:64
r8const: dest:f len:64
store_membase_imm: dest:b len:64
store_membase_reg: dest:b src1:i len:64
storei1_membase_imm: dest:b len:64
storei1_membase_reg: dest:b src1:i len:64
storei2_membase_imm: dest:b len:64
storei2_membase_reg: dest:b src1:i len:64
storei4_membase_imm: dest:b len:64
storei4_membase_reg: dest:b src1:i len:64
storei8_membase_imm: dest:b len:64 len:64
storei8_membase_reg: dest:b src1:i len:64
storer4_membase_reg: dest:b src1:f len:64
storer8_membase_reg: dest:b src1:f len:64
load_membase: dest:i src1:b len:64
loadi1_membase: dest:i src1:b len:64
loadu1_membase: dest:i src1:b len:64
loadi2_membase: dest:i src1:b len:64
loadu2_membase: dest:i src1:b len:64
loadi4_membase: dest:i src1:b len:64
loadu4_membase: dest:i src1:b len:64
loadi8_membase: dest:i src1:b len:64
loadr4_membase: dest:f src1:b len:64
loadr8_membase: dest:f src1:b len:64
loadu4_mem: dest:i len:8
move: dest:i src1:i len:4
add_imm: dest:i src1:i len:64
addcc_imm: dest:i src1:i len:64
sub_imm: dest:i src1:i len:64
subcc_imm: dest:i src1:i len:64
mul_imm: dest:i src1:i len:64
div_imm: dest:a src1:i src2:i len:64
div_un_imm: dest:a src1:i src2:i len:64
rem_imm: dest:d src1:i src2:i len:64
rem_un_imm: dest:d src1:i src2:i len:64
and_imm: dest:i src1:i len:64
or_imm: dest:i src1:i len:64
xor_imm: dest:i src1:i len:64
shl_imm: dest:i src1:i clob:1 len:20
shr_imm: dest:i src1:i clob:1 len:20
shr_un_imm: dest:i src1:i clob:1 len:20
hppa_cond_exc_eq: src1:i src2:i len:64
hppa_cond_exc_ge: src1:i src2:i len:64
hppa_cond_exc_gt: src1:i src2:i len:64
hppa_cond_exc_le: src1:i src2:i len:64
hppa_cond_exc_lt: src1:i src2:i len:64
hppa_cond_exc_ne_un: src1:i src2:i len:64
hppa_cond_exc_ge_un: src1:i src2:i len:64
hppa_cond_exc_gt_un: src1:i src2:i len:64
hppa_cond_exc_le_un: src1:i src2:i len:64
hppa_cond_exc_lt_un: src1:i src2:i len:64
hppa_cond_exc_ov: src1:i src2:i len:64
hppa_cond_exc_no: src1:i src2:i len:64
hppa_cond_exc_c: src1:i src2:i len:64
hppa_cond_exc_nc: src1:i src2:i len:64
long_shl: dest:i src1:i src2:i clob:1 len:64
long_shr: dest:i src1:i src2:i clob:1 len:64
long_shr_un: dest:i src1:i src2:i clob:1 len:64
long_conv_to_ovf_i: dest:i src1:i src2:i len:48
long_mul_ovf: 
long_conv_to_r_un: dest:f src1:i src2:i len:64 
long_shr_imm: dest:i src1:i clob:1 len:64
long_shr_un_imm: dest:i src1:i clob:1 len:64
long_shl_imm: dest:i src1:i clob:1 len:64
float_beq: src1:f src2:f len:32
float_bne_un: src1:f src2:f len:32
float_blt: src1:f src2:f len:32
float_blt_un: src1:f src2:f len:32
float_bgt: src1:f src2:f len:32
float_bgt_un: src1:f src2:f len:32
float_bge: src1:f src2:f len:32
float_bge_un: src1:f src2:f len:32
float_ble: src1:f src2:f len:32
float_ble_un: src1:f src2:f len:32
float_add: dest:f src1:f src2:f len:4
float_sub: dest:f src1:f src2:f len:4
float_mul: dest:f src1:f src2:f len:4
float_div: dest:f src1:f src2:f len:4
float_div_un: dest:f src1:f src2:f len:4
float_rem: dest:f src1:f src2:f len:64
float_rem_un: dest:f src1:f src2:f len:64
float_neg: dest:f src1:f len:4
float_not: dest:f src1:f len:4
float_conv_to_i1: dest:i src1:f len:40
float_conv_to_i2: dest:i src1:f len:40
float_conv_to_i4: dest:i src1:f len:40
float_conv_to_i8: dest:L src1:f len:40
float_conv_to_r4: dest:f src1:f len:8
float_conv_to_u4: dest:i src1:f len:40
float_conv_to_u8: dest:L src1:f len:40
float_conv_to_u2: dest:i src1:f len:40
float_conv_to_u1: dest:i src1:f len:40
float_conv_to_i: dest:i src1:f len:40
float_ceq: dest:i src1:f src2:f len:64
float_cgt: dest:i src1:f src2:f len:64
float_cgt_un: dest:i src1:f src2:f len:64
float_clt: dest:i src1:f src2:f len:64
float_clt_un: dest:i src1:f src2:f len:64
float_conv_to_u: dest:i src1:f len:64
call_handler: len:64 clob:c
endfilter: src1:i len:64
aot_const: dest:i len:64
adc: dest:i src1:i src2:i len:4
addcc: dest:i src1:i src2:i len:4
subcc: dest:i src1:i src2:i len:4
adc_imm: dest:i src1:i len:64
sbb: dest:i src1:i src2:i len:4
sbb_imm: dest:i src1:i len:64
br_reg: src1:i len:8
bigmul: len:2 dest:L src1:a src2:i
bigmul_un: len:2 dest:L src1:a src2:i
fmove: dest:f src1:f len:8

# 32 bit opcodes
int_add: dest:i src1:i src2:i len:64
int_sub: dest:i src1:i src2:i len:64
int_mul: dest:i src1:i src2:i len:64
int_div: dest:i src1:i src2:i len:64
int_div_un: dest:i src1:i src2:i len:64
int_rem: dest:i src1:i src2:i len:64
int_rem_un: dest:i src1:i src2:i len:64
int_and: dest:i src1:i src2:i len:64
int_or: dest:i src1:i src2:i len:64
int_xor: dest:i src1:i src2:i len:64
int_shl: dest:i src1:i src2:i clob:1 len:64
int_shr: dest:i src1:i src2:i clob:1 len:64
int_shr_un: dest:i src1:i src2:i clob:1 len:64
int_adc: dest:i src1:i src2:i len:64
int_adc_imm: dest:i src1:i len:64
int_sbb: dest:i src1:i src2:i len:64
int_sbb_imm: dest:i src1:i len:64
int_addcc: dest:i src1:i src2:i len:64
int_subcc: dest:i src1:i src2:i len:64
int_add_imm: dest:i src1:i len:64
int_sub_imm: dest:i src1:i len:64
int_mul_imm: dest:i src1:i len:64
int_div_imm: dest:i src1:i len:64
int_div_un_imm: dest:i src1:i len:64
int_rem_imm: dest:i src1:i len:64
int_rem_un_imm: dest:i src1:i len:64
int_and_imm: dest:i src1:i len:64
int_or_imm: dest:i src1:i len:64
int_xor_imm: dest:i src1:i len:64
int_shl_imm: dest:i src1:i clob:1 len:64
int_shr_imm: dest:i src1:i clob:1 len:64
int_shr_un_imm: dest:i src1:i clob:1 len:64
int_neg: dest:i src1:i len:64
int_not: dest:i src1:i len:64
int_ceq: dest:i len:64
int_cgt: dest:i len:64
int_cgt_un: dest:i len:64
int_clt: dest:i len:64
int_clt_un: dest:i len:64
int_beq: len:64
int_bne_un: len:64
int_blt: len:64
int_blt_un: len:64
int_bgt: len:64
int_bgt_un: len:64
int_bge: len:64
int_bge_un: len:64
int_ble: len:64
int_ble_un: len:64

memory_barrier: len:4

hppa_beq: src1:i src2:i len:32
hppa_bne: src1:i src2:i len:32
hppa_blt: src1:i src2:i len:32
hppa_blt_un: src1:i src2:i len:32
hppa_ble: src1:i src2:i len:32
hppa_ble_un: src1:i src2:i len:32
hppa_bgt: src1:i src2:i len:32
hppa_bgt_un: src1:i src2:i len:32
hppa_bge: src1:i src2:i len:32
hppa_bge_un: src1:i src2:i len:32

hppa_xmpyu: dest:f src1:f src2:f len:4
hppa_add_ovf: dest:i src1:i src2:i len:24
hppa_sub_ovf: dest:i src1:i src2:i len:24
hppa_addc_ovf: dest:i src1:i src2:i len:24
hppa_subb_ovf: dest:i src1:i src2:i len:24
hppa_ceq: dest:i src1:i src2:i len:8
hppa_clt: dest:i src1:i src2:i len:8
hppa_clt_un: dest:i src1:i src2:i len:8
hppa_cgt: dest:i src1:i src2:i len:8
hppa_cgt_un: dest:i src1:i src2:i len:8


hppa_loadr4_left: dest:f src1:b len:12
hppa_loadr4_right: dest:f src1:b len:12
hppa_storer4_left: dest:b src1:f len:12
hppa_storer4_right: dest:b src1:f len:12

hppa_setf4reg: dest:f src1:f len:4
