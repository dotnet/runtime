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
break: len:1
ldarg.0:
ldarg.1:
ldarg.2:
ldarg.3:
ldloc.0:
ldloc.1:
ldloc.2:
ldloc.3:
stloc.0:
stloc.1:
stloc.2:
stloc.3:
ldarg.s:
ldarga.s:
starg.s:
ldloc.s:
ldloca.s:
stloc.s:
ldnull:
ldc.i4.m1:
ldc.i4.0:
ldc.i4.1:
ldc.i4.2:
ldc.i4.3:
ldc.i4.4:
ldc.i4.5:
ldc.i4.6:
ldc.i4.7:
ldc.i4.8:
ldc.i4.s:
ldc.i4:
ldc.i8:
ldc.r4:
ldc.r8:
dup:
pop:
jmp: len:32
call: dest:a clob:c len:11
calli:
ret: len:1
br.s:
brfalse.s:
brtrue.s:
beq.s:
bge.s:
bgt.s:
ble.s:
blt.s:
bne.un.s:
bge.un.s:
bgt.un.s:
ble.un.s:
blt.un.s:
br: len:5
brfalse:
brtrue:
beq: len:6
bge: len:6
bgt: len:6
ble: len:6
blt: len:6
bne.un: len:6
bge.un: len:6
bgt.un: len:6
ble.un: len:6
blt.un: len:6
switch:
ldind.i1: dest:i len:6
ldind.u1: dest:i len:6
ldind.i2: dest:i len:6
ldind.u2: dest:i len:6
ldind.i4: dest:i len:6
ldind.u4: dest:i len:6
ldind.i8:
ldind.i: dest:i len:6
ldind.r4:
ldind.r8:
ldind.ref: dest:i len:6
stind.ref: src1:b src2:i
stind.i1: src1:b src2:i
stind.i2: src1:b src2:i
stind.i4: src1:b src2:i
stind.i8:
stind.r4: dest:f src1:b
stind.r8: dest:f src1:b
add: dest:i src1:i src2:i len:2 clob:1
sub: dest:i src1:i src2:i len:2 clob:1
mul: dest:i src1:i src2:i len:3 clob:1
div: dest:a src1:i src2:i len:15 clob:d
div.un: dest:a src1:i src2:i len:15 clob:d
rem: dest:d src1:i src2:i len:15 clob:d
rem.un: dest:d src1:i src2:i len:15 clob:d
and: dest:i src1:i src2:i len:2 clob:1
or: dest:i src1:i src2:i len:2 clob:1
xor: dest:i src1:i src2:i len:2 clob:1
shl: dest:i src1:i src2:i clob:s len:2
shr: dest:i src1:i src2:i clob:s len:2
shr.un: dest:i src1:i src2:i clob:s len:2
neg: dest:i src1:i len:2 clob:1
not: dest:i src1:i len:2 clob:1
conv.i1: dest:i src1:i len:3
conv.i2: dest:i src1:i len:3
conv.i4: dest:i src1:i len:2
conv.i8:
conv.r4: dest:f src1:i len:7
conv.r8: dest:f src1:i len:7
conv.u4: dest:i src1:i
conv.u8:
callvirt:
cpobj:
ldobj:
ldstr:
newobj:
castclass:
isinst:
conv.r.un:
unbox:
throw: src1:i len:6
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
conv.ovf.u4:
conv.ovf.i8:
conv.ovf.u8:
refanyval:
ckfinite: dest:f src1:f len:22
mkrefany:
ldtoken:
conv.u2: dest:i src1:i len:3
conv.u1: dest:i src1:i len:3
conv.i: dest:i src1:i len:3
conv.ovf.i:
conv.ovf.u:
add.ovf:
add.ovf.un:
mul.ovf: dest:i src1:i src2:i clob:1 len:9
# this opcode is handled specially in the code generator
mul.ovf.un: dest:i src1:i src2:i len:16
sub.ovf:
sub.ovf.un:
endfinally:
leave:
leave.s:
stind.i:
conv.u: dest:i src1:i len:3
prefix7:
prefix6:
prefix5:
prefix4:
prefix3:
prefix2:
prefix1:
prefixref:
arglist:
ceq: dest:i len:6
cgt: dest:i len:6
cgt.un: dest:i len:6
clt: dest:i len:6
clt.un: dest:i len:6
cne: dest:i len:6
ldftn:
ldvirtftn:
ldarg:
ldarga:
starg:
ldloc:
ldloca:
stloc:
localloc: dest:i src1:i len:64
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
compare: src1:i src2:i len:2
compare_imm: src1:i len:6
fcompare: src1:f src2:f clob:a len:9
lcompare:
local:
arg:
oparglist: src1:b len:10
outarg: src1:i len:1
outarg_imm: len:5
retarg:
setret: dest:a src1:i len:2
setlret: dest:l src1:i src2:i len:4
checkthis: src1:b len:2
voidcall: len:11 clob:c
voidcall_reg: src1:i len:11 clob:c
voidcall_membase: src1:b len:16 clob:c
fcall: dest:f len:11 clob:c
fcall_reg: dest:f src1:i len:11 clob:c
fcall_membase: dest:f src1:b len:16 clob:c
lcall: dest:l len:11 clob:c
lcall_reg: dest:l src1:i len:11 clob:c
lcall_membase: dest:l src1:b len:16 clob:c
vcall: len:11 clob:c
vcall_reg: src1:i len:11 clob:c
vcall_membase: src1:b len:16 clob:c
call_reg: dest:a src1:i len:11 clob:c
call_membase: dest:a src1:b len:16 clob:c
trap:
iconst: dest:i len:5
i8const:
r4const: dest:f len:6
r8const: dest:f len:6
regvar:
reg:
regoffset:
label:
store_membase_imm: dest:b len:10
store_membase_reg: dest:b src1:i len:7
storei1_membase_imm: dest:b len:10
storei1_membase_reg: dest:b src1:i len:7
storei2_membase_imm: dest:b len:11
storei2_membase_reg: dest:b src1:i len:7
storei4_membase_imm: dest:b len:10
storei4_membase_reg: dest:b src1:i len:7
storei8_membase_imm: dest:b 
storei8_membase_reg: dest:b src1:i 
storer4_membase_reg: dest:b src1:f len:7
storer8_membase_reg: dest:b src1:f len:6
load_membase: dest:i src1:b len:6
loadi1_membase: dest:i src1:b len:7
loadu1_membase: dest:i src1:b len:7
loadi2_membase: dest:i src1:b len:7
loadu2_membase: dest:i src1:b len:7
loadi4_membase: dest:i src1:b len:6
loadu4_membase: dest:i src1:b len:6
loadi8_membase: dest:i src1:b
loadr4_membase: dest:f src1:b len:6
loadr8_membase: dest:f src1:b len:6
loadr8_spill_membase: src1:b len:8
loadu4_mem: dest:i len:9
move: dest:i src1:i len:2
add_imm: dest:i src1:i len:6 clob:1
sub_imm: dest:i src1:i len:6 clob:1
mul_imm: dest:i src1:i len:6
# there is no actual support for division or reminder by immediate
# we simulate them, though (but we need to change the burg rules 
# to allocate a symbolic reg for src2)
div_imm: dest:a src1:i src2:i len:15 clob:d
div_un_imm: dest:a src1:i src2:i len:15 clob:d
rem_imm: dest:d src1:i src2:i len:15 clob:d
rem_un_imm: dest:d src1:i src2:i len:15 clob:d
and_imm: dest:i src1:i len:6 clob:1
or_imm: dest:i src1:i len:6 clob:1
xor_imm: dest:i src1:i len:6 clob:1
shl_imm: dest:i src1:i len:6 clob:1
shr_imm: dest:i src1:i len:6 clob:1
shr_un_imm: dest:i src1:i len:6 clob:1
cond_exc_eq: len:6
cond_exc_ne_un: len:6
cond_exc_lt: len:6
cond_exc_lt_un: len:6
cond_exc_gt: len:6
cond_exc_gt_un: len:6
cond_exc_ge: len:6
cond_exc_ge_un: len:6
cond_exc_le: len:6
cond_exc_le_un: len:6
cond_exc_ov: len:6
cond_exc_no: len:6
cond_exc_c: len:6
cond_exc_nc: len:6
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
long_shl: dest:L src1:L src2:i clob:s len:21
long_shr: dest:L src1:L src2:i clob:s len:22
long_shr_un: dest:L src1:L src2:i clob:s len:22
long_neg:
long_not:
long_conv_to_i1:
long_conv_to_i2:
long_conv_to_i4:
long_conv_to_i8:
long_conv_to_r4:
long_conv_to_r8:
long_conv_to_u4:
long_conv_to_u8:
long_conv_to_u2:
long_conv_to_u1:
long_conv_to_i:
long_conv_to_ovf_i: dest:i src1:i src2:i len:30
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
long_conv_to_r_un: dest:f src1:i src2:i len:37 
long_conv_to_u:
long_shr_imm: dest:L src1:L len:10
long_shr_un_imm: dest:L src1:L len:10
long_shl_imm: dest:L src1:L len:10
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
float_beq: len:12
float_bne_un: len:18
float_blt: len:12
float_blt_un: len:20
float_bgt: len:12
float_btg_un: len:20
float_bge: len:22
float_bge_un: len:12
float_ble: len:22
float_ble_un: len:12
float_add: dest:f src1:f src2:f len:2
float_sub: dest:f src1:f src2:f len:2
float_mul: dest:f src1:f src2:f len:2
float_div: dest:f src1:f src2:f len:2
float_div_un: dest:f src1:f src2:f len:2
float_rem: dest:f src1:f src2:f len:17
float_rem_un: dest:f src1:f src2:f len:17
float_neg: dest:f src1:f len:2
float_not: dest:f src1:f len:2
float_conv_to_i1: dest:i src1:f len:39
float_conv_to_i2: dest:i src1:f len:39
float_conv_to_i4: dest:i src1:f len:39
float_conv_to_i8: dest:L src1:f len:39
float_conv_to_r4:
float_conv_to_r8:
float_conv_to_u4: dest:i src1:f len:39
float_conv_to_u8: dest:L src1:f len:39
float_conv_to_u2: dest:i src1:f len:39
float_conv_to_u1: dest:i src1:f len:39
float_conv_to_i: dest:i src1:f len:39
float_conv_to_ovf_i: dest:a src1:f len:30
float_conv_to_ovd_u: dest:a src1:f len:30
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
float_ceq: dest:i src1:f src2:f len:25
float_cgt: dest:i src1:f src2:f len:25
float_cgt_un: dest:i src1:f src2:f len:37
float_clt: dest:i src1:f src2:f len:25
float_clt_un: dest:i src1:f src2:f len:32
float_conv_to_u: dest:i src1:f len:36
call_handler: len:10
aot_const: dest:i len:5
x86_test_null: src1:i len:2
x86_compare_membase_reg: src1:b src2:i len:6
x86_compare_membase_imm: src1:b len:11
x86_compare_membase8_imm: src1:b len:8
x86_compare_reg_membase: src1:i src2:b len:6
x86_inc_reg: dest:i src1:i clob:1 len:1
x86_inc_membase: src1:b len:6
x86_dec_reg: dest:i src1:i clob:1 len:1
x86_dec_membase: src1:b len:6
x86_add_membase_imm: src1:b len:11
x86_sub_membase_imm: src1:b len:11
x86_push: src1:i len:1
x86_push_imm: len:5
x86_push_membase: src1:b len:6
x86_push_obj: src1:b len:30
x86_lea: dest:i src1:i src2:i len:7
x86_lea_membase: dest:i src1:i len:10
x86_xchg: src1:i src2:i clob:x len:1
x86_fpop: src1:f len:2
x86_fp_load_i8: dest:f src1:b len:7
x86_fp_load_i4: dest:f src1:b len:7
x86_seteq_membase: src1:b len:7
x86_setne_membase: src1:b len:7
x86_add_membase: dest:i src1:i src2:b clob:1 len:11
x86_sub_membase: dest:i src1:i src2:b clob:1 len:11
x86_mul_membase: dest:i src1:i src2:b clob:1 len:13
adc: dest:i src1:i src2:i len:2 clob:1
addcc: dest:i src1:i src2:i len:2 clob:1
subcc: dest:i src1:i src2:i len:2 clob:1
adc_imm: dest:i src1:i len:6 clob:1
sbb: dest:i src1:i src2:i len:2 clob:1
sbb_imm: dest:i src1:i len:6 clob:1
br_reg: src1:i len:2
sin: dest:f src1:f len:6
cos: dest:f src1:f len:6
abs: dest:f src1:f len:2
tan: dest:f src1:f len:49
atan: dest:f src1:f len:8
sqrt: dest:f src1:f len:2
op_bigmul: len:2 dest:l src1:a src2:i
op_bigmul_un: len:2 dest:l src1:a src2:i
sext_i1: dest:i src1:i len:3
sext_i2: dest:i src1:i len:3
x86_tls_get: dest:a len:20
