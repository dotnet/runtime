# x86-class cpu description file
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
#	b  base register (used in address references)
#	f  floating point register
#	a  EAX register
#       d  EDX register
#	l  long reg (forced eax:edx)
#		L  long reg (dynamic)
#
# len:number         describe the maximun length in bytes of the instruction
# 		     number is a positive integer.  If the length is not specified
#                    it defaults to zero.   But lengths are only checked if the given opcode 
#                    is encountered during compilation. Some opcodes, like CONV_U4 are 
#                    transformed into other opcodes in the brg files, so they do not show up 
#                    during code generation.
#
# cost:number        describe how many cycles are needed to complete the instruction (unused)
#
# clob:spec          describe if the instruction clobbers registers or has special needs
#
# spec can be one of the following characters:
#	c  clobbers caller-save registers
#	1  clobbers the first source register
#	a  EAX is clobbered
#   d  EAX and EDX are clobbered
#	s  the src2 operand needs to be in ECX (shift opcodes)
#	x  both the source operands are clobbered (xchg)
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
nop:
break: len:2
jmp: len:42
br.s:
brfalse.s:
brtrue.s:
br: len:6
brfalse:
brtrue:
beq: len:7
bge: len:7
bgt: len:7
ble: len:7
blt: len:7
bne.un: len:7
bge.un: len:7
bgt.un: len:7
ble.un: len:7
blt.un: len:7
switch:
ldind.i1: dest:i len:7
ldind.u1: dest:i len:7
ldind.i2: dest:i len:7
ldind.u2: dest:i len:7
ldind.i4: dest:i len:9
ldind.u4: dest:i len:7
ldind.i8:
ldind.i: dest:i len:7
ldind.r4:
ldind.r8:
ldind.ref: dest:i len:7
stind.ref: src1:b src2:i
stind.i1: src1:b src2:i
stind.i2: src1:b src2:i
stind.i4: src1:b src2:i
stind.i8:
stind.r4: dest:f src1:b
stind.r8: dest:f src1:b
add: dest:i src1:i src2:i len:3 clob:1
sub: dest:i src1:i src2:i len:3 clob:1
mul: dest:i src1:i src2:i len:4 clob:1
div: dest:a src1:i src2:i len:16 clob:d
div.un: dest:a src1:i src2:i len:16 clob:d
rem: dest:d src1:i src2:i len:16 clob:d
rem.un: dest:d src1:i src2:i len:16 clob:d
and: dest:i src1:i src2:i len:3 clob:1
or: dest:i src1:i src2:i len:3 clob:1
xor: dest:i src1:i src2:i len:3 clob:1
shl: dest:i src1:i src2:i clob:s len:3
shr: dest:i src1:i src2:i clob:s len:3
shr.un: dest:i src1:i src2:i clob:s len:3
neg: dest:i src1:i len:3 clob:1
not: dest:i src1:i len:3 clob:1
conv.i1: dest:i src1:i len:4
conv.i2: dest:i src1:i len:4
conv.i4: dest:i src1:i len:3
conv.i8: dest:i src1:i len:3
conv.r4: dest:f src1:i len:9
conv.r8: dest:f src1:i len:9
conv.u4: dest:i src1:i len:3
conv.u8: dest:i src1:i len:3
callvirt:
cpobj:
ldobj:
ldstr:
newobj:
castclass:
isinst:
conv.r.un: dest:f src1:i len:8
unbox:
throw: src1:i len:17
ldfld:
ldflda:
stfld:
ldsfld:
ldsflda:
stsfld:
stobj:
conv.ovf.i1.un:
conv.ovf.i2.un:
conv.ovf.i4.un:
conv.ovf.i8.un:
conv.ovf.u1.un:
conv.ovf.u2.un:
conv.ovf.u4.un: 
conv.ovf.u8.un:
conv.ovf.i.un:
conv.ovf.u.un:
box:
newarr:
ldlen:
ldelema:
ldelem.i1:
ldelem.u1:
ldelem.i2:
ldelem.u2:
ldelem.i4:
ldelem.u4:
ldelem.i8:
ldelem.i:
ldelem.r4:
ldelem.r8:
ldelem.ref:
stelem.i:
stelem.i1:
stelem.i2:
stelem.i4:
stelem.i8:
stelem.r4:
stelem.r8:
stelem.ref:
conv.ovf.i1:
conv.ovf.u1:
conv.ovf.i2:
conv.ovf.u2:
conv.ovf.i4:
conv.ovf.u4: dest:i src1:i len:4
conv.ovf.i8:
conv.ovf.u8:
refanyval:
ckfinite: dest:f src1:f len:32
mkrefany:
ldtoken:
conv.u2: dest:i src1:i len:4
conv.u1: dest:i src1:i len:4
conv.i: dest:i src1:i len:4
conv.ovf.i:
conv.ovf.u:
add.ovf:
add.ovf.un:
mul.ovf: dest:i src1:i src2:i clob:1 len:10
# this opcode is handled specially in the code generator
mul.ovf.un: dest:i src1:i src2:i len:17
sub.ovf:
sub.ovf.un:
endfinally:
leave:
leave.s:
stind.i:
conv.u: dest:i src1:i len:4
prefix7:
prefix6:
prefix5:
prefix4:
prefix3:
prefix2:
prefix1:
prefixref:
arglist:
ceq: dest:i len:8
cgt: dest:i len:8
cgt.un: dest:i len:8
clt: dest:i len:8
clt.un: dest:i len:8
ldftn:
ldvirtftn:
ldarg:
ldarga:
starg:
ldloc:
ldloca:
stloc:
localloc: dest:i src1:i len:74
endfilter:
unaligned.:
volatile.:
tail.:
initobj:
cpblk:
initblk:
rethrow:
sizeof:
refanytype:
illegal:
endmac:
mono_objaddr:
mono_ldptr:
mono_vtaddr:
mono_newobj:
mono_retobj:
load:
ldaddr:
store:
phi:
rename:
compare: src1:i src2:i len:3
lcompare: src1:i src2:i len:3
icompare: src1:i src2:i len:3
compare_imm: src1:i len:13
icompare_imm: src1:i len:7
fcompare: src1:f src2:f clob:a len:12
local:
arg:
oparglist: src1:b len:11
outarg: src1:i len:2
outarg_imm: len:6
retarg:
setret: dest:a src1:i len:3
setlret: dest:i src1:i src2:i len:5
checkthis: src1:b len:5
call: dest:a clob:c len:64
ret: len:2
voidcall: clob:c len:64
voidcall_reg: src1:i clob:c len:64
voidcall_membase: src1:b clob:c len:64
fcall: dest:f len:64 clob:c
fcall_reg: dest:f src1:i len:64 clob:c
fcall_membase: dest:f src1:b len:64 clob:c
lcall: dest:i len:64 clob:c
lcall_reg: dest:i src1:i len:64 clob:c
lcall_membase: dest:i src1:b len:64 clob:c
vcall: len:64 clob:c
vcall_reg: src1:i len:64 clob:c
vcall_membase: src1:b len:64 clob:c
call_reg: dest:a src1:i len:64 clob:c
call_membase: dest:a src1:b len:64 clob:c
trap:
iconst: dest:i len:10
i8const: dest:i len:17
r4const: dest:f len:7
r8const: dest:f len:7
regvar:
reg:
regoffset:
label:
store_membase_imm: dest:b len:15
store_membase_reg: dest:b src1:i len:8
storei8_membase_reg: dest:b src1:i len:8
storei1_membase_imm: dest:b len:11
storei1_membase_reg: dest:b src1:i len:8
storei2_membase_imm: dest:b len:12
storei2_membase_reg: dest:b src1:i len:8
storei4_membase_imm: dest:b len:11
storei4_membase_reg: dest:b src1:i len:8
storei8_membase_imm: dest:b len:17
storer4_membase_reg: dest:b src1:f len:8
storer8_membase_reg: dest:b src1:f len:7
load_membase: dest:i src1:b len:14
loadi1_membase: dest:i src1:b len:9
loadu1_membase: dest:i src1:b len:9
loadi2_membase: dest:i src1:b len:9
loadu2_membase: dest:i src1:b len:9
loadi4_membase: dest:i src1:b len:9
loadu4_membase: dest:i src1:b len:9
loadi8_membase: dest:i src1:b len:17
loadr4_membase: dest:f src1:b len:7
loadr8_membase: dest:f src1:b len:7
loadr8_spill_membase: src1:b len:9
loadu4_mem: dest:i len:10
move: dest:i src1:i len:4
setreg: dest:i src1:i len:4
add_imm: dest:i src1:i len:7 clob:1
sub_imm: dest:i src1:i len:7 clob:1
mul_imm: dest:i src1:i len:7
# there is no actual support for division or reminder by immediate
# we simulate them, though (but we need to change the burg rules 
# to allocate a symbolic reg for src2)
div_imm: dest:a src1:i src2:i len:16 clob:d
div_un_imm: dest:a src1:i src2:i len:16 clob:d
rem_imm: dest:d src1:i src2:i len:16 clob:d
rem_un_imm: dest:d src1:i src2:i len:16 clob:d
and_imm: dest:i src1:i len:8 clob:1
or_imm: dest:i src1:i len:8 clob:1
xor_imm: dest:i src1:i len:8 clob:1
shl_imm: dest:i src1:i len:8 clob:1
shr_imm: dest:i src1:i len:8 clob:1
shr_un_imm: dest:i src1:i len:8 clob:1
cond_exc_eq: len:7
cond_exc_ne_un: len:7
cond_exc_lt: len:7
cond_exc_lt_un: len:7
cond_exc_gt: len:7
cond_exc_gt_un: len:7
cond_exc_ge: len:7
cond_exc_ge_un: len:7
cond_exc_le: len:7
cond_exc_le_un: len:7
cond_exc_ov: len:7
cond_exc_no: len:7
cond_exc_c: len:7
cond_exc_nc: len:7
cond_exc_iov: len:7
cond_exc_ic: len:7
long_add:
long_sub:
long_mul:
long_div:
long_div_un:
long_rem:
long_rem_un:
long_and:
long_or:
long_xor:
long_shl: dest:i src1:i src2:i clob:s len:31
long_shr: dest:i src1:i src2:i clob:s len:32
long_shr_un: dest:i src1:i src2:i clob:s len:32
long_neg:
long_not:
long_conv_to_i1:
long_conv_to_i2:
long_conv_to_i4:
long_conv_to_i8:
long_conv_to_r4: dest:f src1:i len:8
long_conv_to_r8: dest:f src1:i len:8
long_conv_to_u4:
long_conv_to_u8:
long_conv_to_u2:
long_conv_to_u1:
long_conv_to_i:
long_conv_to_ovf_i: dest:i src1:i src2:i len:40
long_conv_to_ovf_u:
long_add_ovf:
long_add_ovf_un:
long_mul_ovf: 
long_mul_ovf_un:
long_sub_ovf:
long_sub_ovf_un:
long_conv_to_ovf_i1_un:
long_conv_to_ovf_i2_un:
long_conv_to_ovf_i4_un:
long_conv_to_ovf_i8_un:
long_conv_to_ovf_u1_un:
long_conv_to_ovf_u2_un:
long_conv_to_ovf_u4_un:
long_conv_to_ovf_u8_un:
long_conv_to_ovf_i_un:
long_conv_to_ovf_u_un:
long_conv_to_ovf_i1:
long_conv_to_ovf_u1:
long_conv_to_ovf_i2:
long_conv_to_ovf_u2:
long_conv_to_ovf_i4:
long_conv_to_ovf_u4:
long_conv_to_ovf_i8:
long_conv_to_ovf_u8:
long_ceq:
long_cgt:
long_cgt_un:
long_clt:
long_clt_un:
long_conv_to_r_un: dest:f src1:i src2:i len:47 
long_conv_to_u:
long_shr_imm: dest:i src1:i len:11
long_shr_un_imm: dest:i src1:i len:11
long_shl_imm: dest:i src1:i len:11
long_add_imm:
long_sub_imm:
long_beq:
long_bne_un:
long_blt:
long_blt_un:
long_bgt:
long_btg_un:
long_bge:
long_bge_un:
long_ble:
long_ble_un:
float_beq: len:13
float_bne_un: len:18
float_blt: len:13
float_blt_un: len:30
float_bgt: len:13
float_btg_un: len:30
float_bge: len:32
float_bge_un: len:13
float_ble: len:32
float_ble_un: len:13
float_add: dest:f src1:f src2:f len:3
float_sub: dest:f src1:f src2:f len:3
float_mul: dest:f src1:f src2:f len:3
float_div: dest:f src1:f src2:f len:3
float_div_un: dest:f src1:f src2:f len:3
float_rem: dest:f src1:f src2:f len:19
float_rem_un: dest:f src1:f src2:f len:19
float_neg: dest:f src1:f len:3
float_not: dest:f src1:f len:3
float_conv_to_i1: dest:i src1:f len:49
float_conv_to_i2: dest:i src1:f len:49
float_conv_to_i4: dest:i src1:f len:49
float_conv_to_i8: dest:i src1:f len:49
float_conv_to_r4:
float_conv_to_r8:
float_conv_to_u4: dest:i src1:f len:49
float_conv_to_u8: dest:i src1:f len:49
float_conv_to_u2: dest:i src1:f len:49
float_conv_to_u1: dest:i src1:f len:49
float_conv_to_i: dest:i src1:f len:49
float_conv_to_ovf_i: dest:a src1:f len:40
float_conv_to_ovd_u: dest:a src1:f len:40
float_add_ovf:
float_add_ovf_un:
float_mul_ovf: 
float_mul_ovf_un:
float_sub_ovf:
float_sub_ovf_un:
float_conv_to_ovf_i1_un:
float_conv_to_ovf_i2_un:
float_conv_to_ovf_i4_un:
float_conv_to_ovf_i8_un:
float_conv_to_ovf_u1_un:
float_conv_to_ovf_u2_un:
float_conv_to_ovf_u4_un:
float_conv_to_ovf_u8_un:
float_conv_to_ovf_i_un:
float_conv_to_ovf_u_un:
float_conv_to_ovf_i1:
float_conv_to_ovf_u1:
float_conv_to_ovf_i2:
float_conv_to_ovf_u2:
float_conv_to_ovf_i4:
float_conv_to_ovf_u4:
float_conv_to_ovf_i8:
float_conv_to_ovf_u8:
float_ceq: dest:i src1:f src2:f len:35
float_cgt: dest:i src1:f src2:f len:35
float_cgt_un: dest:i src1:f src2:f len:47
float_clt: dest:i src1:f src2:f len:35
float_clt_un: dest:i src1:f src2:f len:42
float_conv_to_u: dest:i src1:f len:46
call_handler: len:14
aot_const: dest:i len:10
x86_test_null: src1:i len:5
x86_compare_membase_reg: src1:b src2:i len:7
x86_compare_membase_imm: src1:b len:12
x86_compare_reg_membase: src1:i src2:b len:7
x86_inc_reg: dest:i src1:i clob:1 len:3
x86_inc_membase: src1:b len:7
x86_dec_reg: dest:i src1:i clob:1 len:3
x86_dec_membase: src1:b len:7
x86_add_membase_imm: src1:b len:12
x86_sub_membase_imm: src1:b len:12
x86_push: src1:i len:3
x86_push_imm: len:6
x86_push_membase: src1:b len:7
x86_push_obj: src1:b len:40
x86_lea: dest:i src1:i src2:i len:8
x86_lea_membase: dest:i src1:i len:11
x86_xchg: src1:i src2:i clob:x len:2
x86_fpop: src1:f len:3
x86_fp_load_i8: dest:f src1:b len:8
x86_fp_load_i4: dest:f src1:b len:8
x86_seteq_membase: src1:b len:8
x86_add_membase: dest:i src1:i src2:b clob:1 len:12
x86_sub_membase: dest:i src1:i src2:b clob:1 len:12
x86_mul_membase: dest:i src1:i src2:b clob:1 len:14
amd64_icompare_membase_reg: src1:b src2:i len:7
amd64_icompare_membase_imm: src1:b len:12
amd64_icompare_reg_membase: src1:i src2:b len:7
amd64_set_xmmreg_r4: src1:f len:14
amd64_set_xmmreg_r8: src1:f len:14
adc: dest:i src1:i src2:i len:3 clob:1
addcc: dest:i src1:i src2:i len:3 clob:1
subcc: dest:i src1:i src2:i len:3 clob:1
adc_imm: dest:i src1:i len:7 clob:1
sbb: dest:i src1:i src2:i len:3 clob:1
sbb_imm: dest:i src1:i len:7 clob:1
br_reg: src1:i len:3
sin: dest:f src1:f len:7
cos: dest:f src1:f len:7
abs: dest:f src1:f len:3
tan: dest:f src1:f len:59
atan: dest:f src1:f len:9
sqrt: dest:f src1:f len:3
op_bigmul: len:3 dest:i src1:a src2:i
op_bigmul_un: len:3 dest:i src1:a src2:i
sext_i1: dest:i src1:i len:4
sext_i2: dest:i src1:i len:4

# 32 bit opcodes
# FIXME: fix sizes
int_add: dest:i src1:i src2:i clob:1 len:64
int_sub: dest:i src1:i src2:i clob:1 len:64
int_mul: dest:i src1:i src2:i clob:1 len:64
int_mul_ovf: dest:i src1:i src2:i clob:1 len:64
int_mul_ovf_un: dest:i src1:i src2:i clob:1 len:64
int_div: dest:a src1:i src2:i clob:d len:64
int_div_un: dest:a src1:i src2:i clob:d len:64
int_rem: dest:d src1:i src2:i clob:d len:64
int_rem_un: dest:d src1:i src2:i clob:d len:64
int_and: dest:i src1:i src2:i clob:1 len:64
int_or: dest:i src1:i src2:i clob:1 len:64
int_xor: dest:i src1:i src2:i clob:1 len:64
int_shl: dest:i src1:i src2:i clob:s len:64
int_shr: dest:i src1:i src2:i clob:s len:64
int_shr_un: dest:i src1:i src2:i clob:s len:64
int_adc: dest:i src1:i src2:i clob:1 len:64
int_adc_imm: dest:i src1:i clob:1 len:64
int_sbb: dest:i src1:i src2:i clob:1 len:64
int_sbb_imm: dest:i src1:i clob:1 len:64
int_addcc: dest:i src1:i src2:i clob:1 len:64
int_subcc: dest:i src1:i src2:i clob:1 len:64
int_add_imm: dest:i src1:i clob:1 len:64
int_sub_imm: dest:i src1:i clob:1 len:64
int_mul_imm: dest:i src1:i clob:1 len:64
int_div_imm: dest:a src1:i clob:d len:64
int_div_un_imm: dest:a src1:i clob:d len:64
int_rem_imm: dest:d src1:i clob:d len:64
int_rem_un_imm: dest:d src1:i clob:d len:64
int_and_imm: dest:i src1:i clob:1 len:64
int_or_imm: dest:i src1:i clob:1 len:64
int_xor_imm: dest:i src1:i clob:1 len:64
int_shl_imm: dest:i src1:i clob:1 len:64
int_shr_imm: dest:i src1:i clob:1 len:64
int_shr_un_imm: dest:i src1:i clob:1 len:64
int_neg: dest:i src1:i clob:1 len:64
int_not: dest:i src1:i clob:1 len:64
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

