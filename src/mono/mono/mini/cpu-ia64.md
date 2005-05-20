# ia64 cpu description file
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
#
nop:
break: len:48
jmp: len:48
br.s:
brfalse.s:
brtrue.s:
br: len:48
brfalse:
brtrue:
beq: len:48
bge: len:48
bgt: len:48
ble: len:48
blt: len:48
bne.un: len:48
bge.un: len:48
bgt.un: len:48
ble.un: len:48
blt.un: len:48
switch:
ldind.i1: dest:i len:48
ldind.u1: dest:i len:48
ldind.i2: dest:i len:48
ldind.u2: dest:i len:48
ldind.i4: dest:i len:48
ldind.u4: dest:i len:48
ldind.i8:
ldind.i: dest:i len:48
ldind.r4:
ldind.r8:
ldind.ref: dest:i len:48
stind.ref: src1:b src2:i
stind.i1: src1:b src2:i
stind.i2: src1:b src2:i
stind.i4: src1:b src2:i
stind.i8:
stind.r4: dest:f src1:b
stind.r8: dest:f src1:b
add: dest:i src1:i src2:i len:48 clob:1
sub: dest:i src1:i src2:i len:48 clob:1
mul: dest:i src1:i src2:i len:48 clob:1
div: dest:a src1:a src2:i len:48 clob:d
div.un: dest:a src1:a src2:i len:48 clob:d
rem: dest:d src1:a src2:i len:48 clob:a
rem.un: dest:d src1:a src2:i len:48 clob:a
and: dest:i src1:i src2:i len:48 clob:1
or: dest:i src1:i src2:i len:48 clob:1
xor: dest:i src1:i src2:i len:48 clob:1
shl: dest:i src1:i src2:s clob:1 len:48
shr: dest:i src1:i src2:s clob:1 len:48
shr.un: dest:i src1:i src2:s clob:1 len:48
neg: dest:i src1:i len:48 clob:1
not: dest:i src1:i len:48 clob:1
conv.i1: dest:i src1:i len:48
conv.i2: dest:i src1:i len:48
conv.i4: dest:i src1:i len:48
conv.i8: dest:i src1:i len:48
conv.r4: dest:f src1:i len:48
conv.r8: dest:f src1:i len:48
conv.u4: dest:i src1:i len:48
conv.u8: dest:i src1:i len:48
callvirt:
cpobj:
ldobj:
ldstr:
castclass:
isinst:
conv.r.un: dest:f src1:i len:48
unbox:
throw: src1:i len:48
op_rethrow: src1:i len:48
ldfld:
ldflda:
stfld:
ldsfld:
ldsflda:
stsfld:
stobj:
conv.ovf.i1.un:
conv.ovf.i2.un:
conv.ovf.i4.un: dest:i src1:i len:48
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
conv.ovf.u4: dest:i src1:i len:48
conv.ovf.i8:
conv.ovf.u8:
refanyval:
ckfinite: dest:f src1:f len:48
mkrefany:
ldtoken:
conv.u2: dest:i src1:i len:48
conv.u1: dest:i src1:i len:48
conv.i: dest:i src1:i len:48
conv.ovf.i:
conv.ovf.u:
add.ovf:
add.ovf.un:
mul.ovf: dest:i src1:i src2:i clob:1 len:48
# this opcode is handled specially in the code generator
mul.ovf.un: dest:i src1:i src2:i len:48
sub.ovf:
sub.ovf.un:
endfinally:
leave:
leave.s:
stind.i:
conv.u: dest:i src1:i len:48
prefix7:
prefix6:
prefix5:
prefix4:
prefix3:
prefix2:
prefix1:
prefixref:
arglist:
ceq: dest:c len:48
cgt: dest:c len:48
cgt.un: dest:c len:48
clt: dest:c len:48
clt.un: dest:c len:48
ldftn:
ldvirtftn:
ldarg:
ldarga:
starg:
ldloc:
ldloca:
stloc:
localloc: dest:i src1:i len:48
endfilter:
unaligned.:
volatile.:
tail.:
initobj:
cpblk:
initblk:
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
compare: src1:i src2:i len:48
lcompare: src1:i src2:i len:48
icompare: src1:i src2:i len:48
compare_imm: src1:i len:48
icompare_imm: src1:i len:48
fcompare: src1:f src2:f clob:a len:48
local:
arg:
oparglist: src1:b len:48
outarg: src1:i len:48
outarg_imm: len:48
retarg:
setret: dest:a src1:i len:48
setlret: dest:i src1:i src2:i len:48
checkthis: src1:b len:48
call: dest:a clob:c len:48
ret: len:48
voidcall: clob:c len:48
voidcall_reg: src1:i clob:c len:48
voidcall_membase: src1:b clob:c len:48
fcall: dest:f len:48 clob:c
fcall_reg: dest:f src1:i len:48 clob:c
fcall_membase: dest:f src1:b len:48 clob:c
lcall: dest:a len:48 clob:c
lcall_reg: dest:a src1:i len:48 clob:c
lcall_membase: dest:a src1:b len:48 clob:c
vcall: len:48 clob:c
vcall_reg: src1:i len:48 clob:c
vcall_membase: src1:b len:48 clob:c
call_reg: dest:a src1:i len:48 clob:c
call_membase: dest:a src1:b len:48 clob:c
trap:
iconst: dest:i len:48
i8const: dest:i len:48
r4const: dest:f len:48
r8const: dest:f len:48
regvar:
reg:
regoffset:
label:
store_membase_imm: dest:b len:48
store_membase_reg: dest:b src1:i len:48
storei8_membase_reg: dest:b src1:i len:48
storei1_membase_imm: dest:b len:48
storei1_membase_reg: dest:b src1:c len:48
storei2_membase_imm: dest:b len:48
storei2_membase_reg: dest:b src1:i len:48
storei4_membase_imm: dest:b len:48
storei4_membase_reg: dest:b src1:i len:48
storei8_membase_imm: dest:b len:48
storer4_membase_reg: dest:b src1:f len:48
storer8_membase_reg: dest:b src1:f len:48
load_membase: dest:i src1:b len:48
loadi1_membase: dest:c src1:b len:48
loadu1_membase: dest:c src1:b len:48
loadi2_membase: dest:i src1:b len:48
loadu2_membase: dest:i src1:b len:48
loadi4_membase: dest:i src1:b len:48
loadu4_membase: dest:i src1:b len:48
loadi8_membase: dest:i src1:b len:48
loadr4_membase: dest:f src1:b len:48
loadr8_membase: dest:f src1:b len:48
loadr8_spill_membase: src1:b len:48
loadu4_mem: dest:i len:48
move: dest:i src1:i len:48
setreg: dest:i src1:i len:48
add_imm: dest:i src1:i len:48 clob:1
sub_imm: dest:i src1:i len:48 clob:1
mul_imm: dest:i src1:i len:48
# there is no actual support for division or reminder by immediate
# we simulate them, though (but we need to change the burg rules 
# to allocate a symbolic reg for src2)
div_imm: dest:a src1:i src2:i len:48 clob:d
div_un_imm: dest:a src1:i src2:i len:48 clob:d
rem_imm: dest:d src1:i src2:i len:48 clob:a
rem_un_imm: dest:d src1:i src2:i len:48 clob:a
and_imm: dest:i src1:i len:48 clob:1
or_imm: dest:i src1:i len:48 clob:1
xor_imm: dest:i src1:i len:48 clob:1
shl_imm: dest:i src1:i len:48 clob:1
shr_imm: dest:i src1:i len:48 clob:1
shr_un_imm: dest:i src1:i len:48 clob:1
cond_exc_eq: len:48
cond_exc_ne_un: len:48
cond_exc_lt: len:48
cond_exc_lt_un: len:48
cond_exc_gt: len:48
cond_exc_gt_un: len:48
cond_exc_ge: len:48
cond_exc_ge_un: len:48
cond_exc_le: len:48
cond_exc_le_un: len:48
cond_exc_ov: len:48
cond_exc_no: len:48
cond_exc_c: len:48
cond_exc_nc: len:48
cond_exc_iov: len:48
cond_exc_ic: len:48
long_add:
long_sub:
long_mul: dest:i src1:i src2:i clob:1 len:48
long_mul_imm: dest:i src1:i src2:i clob:1 len:48
long_div: dest:a src1:a src2:i len:48 clob:d
long_div_un: dest:a src1:a src2:i len:48 clob:d
long_rem: dest:d src1:a src2:i len:48 clob:a
long_rem_un: dest:d src1:a src2:i len:48 clob:a
long_and:
long_or:
long_xor:
long_shl: dest:i src1:i src2:s clob:1 len:48
long_shr: dest:i src1:i src2:s clob:1 len:48
long_shr_un: dest:i src1:i src2:s clob:1 len:48
long_neg:
long_not:
long_conv_to_i1:
long_conv_to_i2:
long_conv_to_i4:
long_conv_to_i8:
long_conv_to_r4: dest:f src1:i len:48
long_conv_to_r8: dest:f src1:i len:48
long_conv_to_u4:
long_conv_to_u8:
long_conv_to_u2:
long_conv_to_u1:
long_conv_to_i:
long_conv_to_ovf_i: dest:i src1:i src2:i len:48
long_conv_to_ovf_u:
long_add_ovf:
long_add_ovf_un:
long_mul_ovf: dest:i src1:i src2:i clob:1 len:48
long_mul_ovf_un: dest:i src1:i src2:i len:48
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
long_conv_to_r_un: dest:f src1:i src2:i len:48 
long_conv_to_u:
long_shr_imm: dest:i src1:i clob:1 len:48
long_shr_un_imm: dest:i src1:i clob:1 len:48
long_shl_imm: dest:i src1:i clob:1 len:48
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
float_beq: len:48
float_bne_un: len:48
float_blt: len:48
float_blt_un: len:48
float_bgt: len:48
float_btg_un: len:48
float_bge: len:48
float_bge_un: len:48
float_ble: len:48
float_ble_un: len:48
float_add: dest:f src1:f src2:f len:48
float_sub: dest:f src1:f src2:f len:48
float_mul: dest:f src1:f src2:f len:48
float_div: dest:f src1:f src2:f len:48
float_div_un: dest:f src1:f src2:f len:48
float_rem: dest:f src1:f src2:f len:48
float_rem_un: dest:f src1:f src2:f len:48
float_neg: dest:f src1:f len:48
float_not: dest:f src1:f len:48
float_conv_to_i1: dest:i src1:f len:48
float_conv_to_i2: dest:i src1:f len:48
float_conv_to_i4: dest:i src1:f len:48
float_conv_to_i8: dest:i src1:f len:48
float_conv_to_r4:
float_conv_to_r8:
float_conv_to_u4: dest:i src1:f len:48
float_conv_to_u8: dest:i src1:f len:48
float_conv_to_u2: dest:i src1:f len:48
float_conv_to_u1: dest:i src1:f len:48
float_conv_to_i: dest:i src1:f len:48
float_conv_to_ovf_i: dest:a src1:f len:48
float_conv_to_ovd_u: dest:a src1:f len:48
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
float_ceq: dest:i src1:f src2:f len:48
float_cgt: dest:i src1:f src2:f len:48
float_cgt_un: dest:i src1:f src2:f len:48
float_clt: dest:i src1:f src2:f len:48
float_clt_un: dest:i src1:f src2:f len:48
float_ceq_membase: dest:i src1:f src2:b len:48
float_cgt_membase: dest:i src1:f src2:b len:48
float_cgt_un_membase: dest:i src1:f src2:b len:48
float_clt_membase: dest:i src1:f src2:b len:48
float_clt_un_membase: dest:i src1:f src2:b len:48
float_conv_to_u: dest:i src1:f len:48
fmove: dest:f src1:f len:48
call_handler: len:48
aot_const: dest:i len:48
tls_get: dest:i len:48
atomic_add_i4: src1:b src2:i dest:i len:48
atomic_add_new_i4: src1:b src2:i dest:i len:48
atomic_exchange_i4: src1:b src2:i dest:i len:48
atomic_add_i8: src1:b src2:i dest:i len:48
atomic_add_new_i8: src1:b src2:i dest:i len:48
atomic_exchange_i8: src1:b src2:i dest:i len:48
adc: dest:i src1:i src2:i len:48 clob:1
addcc: dest:i src1:i src2:i len:48 clob:1
subcc: dest:i src1:i src2:i len:48 clob:1
adc_imm: dest:i src1:i len:48 clob:1
sbb: dest:i src1:i src2:i len:48 clob:1
sbb_imm: dest:i src1:i len:48 clob:1
br_reg: src1:i len:48
sin: dest:f src1:f len:48
cos: dest:f src1:f len:48
abs: dest:f src1:f len:48
tan: dest:f src1:f len:48
atan: dest:f src1:f len:48
sqrt: dest:f src1:f len:48
op_bigmul: len:48 dest:i src1:a src2:i
op_bigmul_un: len:48 dest:i src1:a src2:i
sext_i1: dest:i src1:i len:48
sext_i2: dest:i src1:i len:48

# 32 bit opcodes
# FIXME: fix sizes
int_add: dest:i src1:i src2:i clob:1 len:48
int_sub: dest:i src1:i src2:i clob:1 len:48
int_mul: dest:i src1:i src2:i clob:1 len:48
int_mul_ovf: dest:i src1:i src2:i clob:1 len:48
int_mul_ovf_un: dest:i src1:i src2:i clob:1 len:48
int_div: dest:a src1:a src2:i clob:d len:48
int_div_un: dest:a src1:a src2:i clob:d len:48
int_rem: dest:d src1:a src2:i clob:a len:48
int_rem_un: dest:d src1:a src2:i clob:a len:48
int_and: dest:i src1:i src2:i clob:1 len:48
int_or: dest:i src1:i src2:i clob:1 len:48
int_xor: dest:i src1:i src2:i clob:1 len:48
int_shl: dest:i src1:i src2:s clob:1 len:48
int_shr: dest:i src1:i src2:s clob:1 len:48
int_shr_un: dest:i src1:i src2:s clob:1 len:48
int_adc: dest:i src1:i src2:i clob:1 len:48
int_adc_imm: dest:i src1:i clob:1 len:48
int_sbb: dest:i src1:i src2:i clob:1 len:48
int_sbb_imm: dest:i src1:i clob:1 len:48
int_addcc: dest:i src1:i src2:i clob:1 len:96
int_subcc: dest:i src1:i src2:i clob:1 len:48
int_add_imm: dest:i src1:i clob:1 len:48
int_sub_imm: dest:i src1:i clob:1 len:48
int_mul_imm: dest:i src1:i clob:1 len:48
int_div_imm: dest:a src1:i clob:d len:48
int_div_un_imm: dest:a src1:i clob:d len:48
int_rem_imm: dest:d src1:i clob:a len:48
int_rem_un_imm: dest:d src1:i clob:a len:48
int_and_imm: dest:i src1:i clob:1 len:48
int_or_imm: dest:i src1:i clob:1 len:48
int_xor_imm: dest:i src1:i clob:1 len:48
int_shl_imm: dest:i src1:i clob:1 len:48
int_shr_imm: dest:i src1:i clob:1 len:48
int_shr_un_imm: dest:i src1:i clob:1 len:48
int_neg: dest:i src1:i clob:1 len:48
int_not: dest:i src1:i clob:1 len:48
int_ceq: dest:c len:48
int_cgt: dest:c len:48
int_cgt_un: dest:c len:48
int_clt: dest:c len:48
int_clt_un: dest:c len:48
int_beq: len:48
int_bne_un: len:48
int_blt: len:48
int_blt_un: len:48
int_bgt: len:48
int_bgt_un: len:48
int_bge: len:48
int_bge_un: len:48
int_ble: len:48
int_ble_un: len:48

ia64_cmp4_eq: src1:i src2:i len:48
ia64_cmp4_ne: src1:i src2:i len:48
ia64_cmp4_le: src1:i src2:i len:48
ia64_cmp4_lt: src1:i src2:i len:48
ia64_cmp4_ge: src1:i src2:i len:48
ia64_cmp4_gt: src1:i src2:i len:48
ia64_cmp4_lt_un: src1:i src2:i len:48
ia64_cmp4_gt_un: src1:i src2:i len:48
ia64_br_cond: len:48
ia64_cond_exc: len:48
