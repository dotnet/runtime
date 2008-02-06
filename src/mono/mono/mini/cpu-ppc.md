# powerpc cpu description file
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
memory_barrier: len:4
nop: len:4
break: len:4
jmp: len:92
call: dest:a clob:c len:16
br: len:4
beq: len:8
bge: len:8
bgt: len:8
ble: len:8
blt: len:8
bne.un: len:8
bge.un: len:8
bgt.un: len:8
ble.un: len:8
blt.un: len:8
add: dest:i src1:i src2:i len:4
sub: dest:i src1:i src2:i len:4
mul: dest:i src1:i src2:i len:4
div: dest:i src1:i src2:i len:40
div.un: dest:i src1:i src2:i len:16
rem: dest:i src1:i src2:i len:48
rem.un: dest:i src1:i src2:i len:24
and: dest:i src1:i src2:i len:4
or: dest:i src1:i src2:i len:4
xor: dest:i src1:i src2:i len:4
shl: dest:i src1:i src2:i len:4
shr: dest:i src1:i src2:i len:4
shr.un: dest:i src1:i src2:i len:4
neg: dest:i src1:i len:4
not: dest:i src1:i len:4
conv.i1: dest:i src1:i len:4
conv.i2: dest:i src1:i len:4
conv.i4: dest:i src1:i len:4
conv.r4: dest:f src1:i len:36
conv.r8: dest:f src1:i len:36
conv.u4: dest:i src1:i
conv.r.un: dest:f src1:i len:32
throw: src1:i len:20
rethrow: src1:i len:20
ckfinite: src1:f
ppc_check_finite: src1:i len:16
conv.u2: dest:i src1:i len:4
conv.u1: dest:i src1:i len:4
conv.i: dest:i src1:i len:4
add.ovf: dest:i src1:i src2:i len:16
add.ovf.un: dest:i src1:i src2:i len:16
mul.ovf: dest:i src1:i src2:i len:16
# this opcode is handled specially in the code generator
mul.ovf.un: dest:i src1:i src2:i len:16
sub.ovf: dest:i src1:i src2:i len:16
sub.ovf.un: dest:i src1:i src2:i len:16
add_ovf_carry: dest:i src1:i src2:i len:16
sub_ovf_carry: dest:i src1:i src2:i len:16
add_ovf_un_carry: dest:i src1:i src2:i len:16
sub_ovf_un_carry: dest:i src1:i src2:i len:16
start_handler: len:16
endfinally: len:12
conv.u: dest:i src1:i len:4
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
outarg: src1:i len:1
outarg_imm: len:5
setret: dest:a src1:i len:4
setlret: src1:i src2:i len:12
checkthis: src1:b len:4
voidcall: len:16 clob:c
voidcall_reg: src1:i len:8 clob:c
voidcall_membase: src1:b len:12 clob:c
fcall: dest:g len:16 clob:c
fcall_reg: dest:g src1:i len:8 clob:c
fcall_membase: dest:g src1:b len:12 clob:c
lcall: dest:l len:16 clob:c
lcall_reg: dest:l src1:i len:8 clob:c
lcall_membase: dest:l src1:b len:12 clob:c
vcall: len:16 clob:c
vcall_reg: src1:i len:8 clob:c
vcall_membase: src1:b len:12 clob:c
call_reg: dest:a src1:i len:8 clob:c
call_membase: dest:a src1:b len:12 clob:c
iconst: dest:i len:12
r4const: dest:f len:12
r8const: dest:f len:12
label: len:0
store_membase_reg: dest:b src1:i len:4
storei1_membase_reg: dest:b src1:i len:4
storei2_membase_reg: dest:b src1:i len:4
storei4_membase_reg: dest:b src1:i len:4
storer4_membase_reg: dest:b src1:f len:8
storer8_membase_reg: dest:b src1:f len:4
load_membase: dest:i src1:b len:4
loadi1_membase: dest:i src1:b len:8
loadu1_membase: dest:i src1:b len:4
loadi2_membase: dest:i src1:b len:4
loadu2_membase: dest:i src1:b len:4
loadi4_membase: dest:i src1:b len:4
loadu4_membase: dest:i src1:b len:4
loadr4_membase: dest:f src1:b len:4
loadr8_membase: dest:f src1:b len:4
load_memindex: dest:i src1:b src2:i len:4
loadi1_memindex: dest:i src1:b src2:i len:8
loadu1_memindex: dest:i src1:b src2:i len:4
loadi2_memindex: dest:i src1:b src2:i len:4
loadu2_memindex: dest:i src1:b src2:i len:4
loadi4_memindex: dest:i src1:b src2:i len:4
loadu4_memindex: dest:i src1:b src2:i len:4
loadr4_memindex: dest:f src1:b src2:i len:4
loadr8_memindex: dest:f src1:b src2:i len:4
store_memindex: dest:b src1:i src2:i len:4
storei1_memindex: dest:b src1:i src2:i len:4
storei2_memindex: dest:b src1:i src2:i len:4
storei4_memindex: dest:b src1:i src2:i len:4
storer4_memindex: dest:b src1:i src2:i len:4
storer8_memindex: dest:b src1:i src2:i len:4
loadu4_mem: dest:i len:8
move: dest:i src1:i len:4
fmove: dest:f src1:f len:4
add_imm: dest:i src1:i len:4
sub_imm: dest:i src1:i len:4
mul_imm: dest:i src1:i len:4
# there is no actual support for division or reminder by immediate
# we simulate them, though (but we need to change the burg rules 
# to allocate a symbolic reg for src2)
div_imm: dest:i src1:i src2:i len:20
div_un_imm: dest:i src1:i src2:i len:12
rem_imm: dest:i src1:i src2:i len:28
rem_un_imm: dest:i src1:i src2:i len:16
and_imm: dest:i src1:i len:4
or_imm: dest:i src1:i len:4
xor_imm: dest:i src1:i len:4
shl_imm: dest:i src1:i len:4
shr_imm: dest:i src1:i len:4
shr_un_imm: dest:i src1:i len:4
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
long_conv_to_ovf_i: dest:i src1:i src2:i len:32
long_mul_ovf: 
long_conv_to_r_un: dest:f src1:i src2:i len:37 
float_beq: len:8
float_bne_un: len:8
float_blt: len:8
float_blt_un: len:8
float_bgt: len:8
float_bgt_un: len:8
float_bge: len:8
float_bge_un: len:8
float_ble: len:8
float_ble_un: len:8
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
float_conv_to_r4: dest:f src1:f len:4
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
call_handler: len:12
endfilter: src1:i len:16
aot_const: dest:i len:8
sqrt: dest:f src1:f len:4
adc: dest:i src1:i src2:i len:4
addcc: dest:i src1:i src2:i len:4
subcc: dest:i src1:i src2:i len:4
addcc_imm: dest:i src1:i len:4
sbb: dest:i src1:i src2:i len:4
br_reg: src1:i len:8
ppc_subfic: dest:i src1:i len:4
ppc_subfze: dest:i src1:i len:4
bigmul: len:12 dest:l src1:i src2:i
bigmul_un: len:12 dest:l src1:i src2:i
tls_get: len:8 dest:i
