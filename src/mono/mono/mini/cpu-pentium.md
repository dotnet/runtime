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
#	d  EDX register
#   s  ECX register
#	l  long reg (forced eax:edx)
#	L  long reg (dynamic)
#	y  the reg needs to be one of EAX,EBX,ECX,EDX (sete opcodes)
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
#   d  EDX is clobbered
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
break: len:1
jmp: len:32
call: dest:a clob:c len:17
ret: len:1
br: len:5
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
label:
ldind.i1: dest:i len:6
ldind.u1: dest:i len:6
ldind.i2: dest:i len:6
ldind.u2: dest:i len:6
ldind.i4: dest:i len:6
ldind.u4: dest:i len:6
ldind.i: dest:i len:6
ldind.ref: dest:i len:6
stind.ref: src1:b src2:i
stind.i1: src1:b src2:i
stind.i2: src1:b src2:i
stind.i4: src1:b src2:i
stind.r4: dest:f src1:b
stind.r8: dest:f src1:b
add: dest:i src1:i src2:i len:2 clob:1
sub: dest:i src1:i src2:i len:2 clob:1
mul: dest:i src1:i src2:i len:3 clob:1
div: dest:a src1:a src2:i len:15 clob:d
div.un: dest:a src1:a src2:i len:15 clob:d
rem: dest:d src1:a src2:i len:15 clob:a
rem.un: dest:d src1:a src2:i len:15 clob:a
and: dest:i src1:i src2:i len:2 clob:1
or: dest:i src1:i src2:i len:2 clob:1
xor: dest:i src1:i src2:i len:2 clob:1
shl: dest:i src1:i src2:s clob:1 len:2
shr: dest:i src1:i src2:s clob:1 len:2
shr.un: dest:i src1:i src2:s clob:1 len:2
neg: dest:i src1:i len:2 clob:1
not: dest:i src1:i len:2 clob:1
conv.i1: dest:i src1:y len:3
conv.i2: dest:i src1:i len:3
conv.i4: dest:i src1:i len:2
conv.r4: dest:f src1:i len:7
conv.r8: dest:f src1:i len:7
conv.u4: dest:i src1:i
conv.u2: dest:i src1:i len:3
conv.u1: dest:i src1:y len:3
conv.i: dest:i src1:i len:3
throw: src1:i len:13
op_rethrow: src1:i len:13
ckfinite: dest:f src1:f len:22
mul.ovf: dest:i src1:i src2:i clob:1 len:9
# this opcode is handled specially in the code generator
mul.ovf.un: dest:i src1:i src2:i len:16
conv.u: dest:i src1:i len:3
ceq: dest:y len:6
cgt: dest:y len:6
cgt.un: dest:y len:6
clt: dest:y len:6
clt.un: dest:y len:6
cne: dest:y len:6
localloc: dest:i src1:i len:120
compare: src1:i src2:i len:2
compare_imm: src1:i len:6
fcompare: src1:f src2:f clob:a len:9
oparglist: src1:b len:10
outarg: src1:i len:1
outarg_imm: len:5
setret: dest:a src1:i len:2
setlret: dest:l src1:i src2:i len:4
checkthis: src1:b len:2
voidcall: len:17 clob:c
voidcall_reg: src1:i len:11 clob:c
voidcall_membase: src1:b len:16 clob:c
fcall: dest:f len:17 clob:c
fcall_reg: dest:f src1:i len:11 clob:c
fcall_membase: dest:f src1:b len:16 clob:c
lcall: dest:l len:17 clob:c
lcall_reg: dest:l src1:i len:11 clob:c
lcall_membase: dest:l src1:b len:16 clob:c
vcall: len:17 clob:c
vcall_reg: src1:i len:11 clob:c
vcall_membase: src1:b len:16 clob:c
call_reg: dest:a src1:i len:11 clob:c
call_membase: dest:a src1:b len:16 clob:c
iconst: dest:i len:5
r4const: dest:f len:15
r8const: dest:f len:16
store_membase_imm: dest:b len:10
store_membase_reg: dest:b src1:i len:7
storei1_membase_imm: dest:b len:10
storei1_membase_reg: dest:b src1:y len:7
storei2_membase_imm: dest:b len:11
storei2_membase_reg: dest:b src1:i len:7
storei4_membase_imm: dest:b len:10
storei4_membase_reg: dest:b src1:i len:7
storei8_membase_imm: dest:b 
storei8_membase_reg: dest:b src1:i 
storer4_membase_reg: dest:b src1:f len:7
storer8_membase_reg: dest:b src1:f len:6
load_membase: dest:i src1:b len:6
loadi1_membase: dest:y src1:b len:7
loadu1_membase: dest:y src1:b len:7
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
addcc_imm: dest:i src1:i len:6 clob:1
add_imm: dest:i src1:i len:6 clob:1
subcc_imm: dest:i src1:i len:6 clob:1
sub_imm: dest:i src1:i len:6 clob:1
mul_imm: dest:i src1:i len:9
# there is no actual support for division or reminder by immediate
# we simulate them, though (but we need to change the burg rules 
# to allocate a symbolic reg for src2)
div_imm: dest:a src1:a src2:i len:15 clob:d
div_un_imm: dest:a src1:a src2:i len:15 clob:d
rem_imm: dest:d src1:a src2:i len:15 clob:a
rem_un_imm: dest:d src1:a src2:i len:15 clob:a
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
long_shl: dest:L src1:L src2:s clob:1 len:21
long_shr: dest:L src1:L src2:s clob:1 len:22
long_shr_un: dest:L src1:L src2:s clob:1 len:22
long_conv_to_ovf_i: dest:i src1:i src2:i len:30
long_mul_ovf: 
long_conv_to_r_un: dest:f src1:i src2:i len:37 
long_shr_imm: dest:L src1:L clob:1 len:10
long_shr_un_imm: dest:L src1:L clob:1 len:10
long_shl_imm: dest:L src1:L clob:1 len:10
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
float_conv_to_i1: dest:y src1:f len:39
float_conv_to_i2: dest:y src1:f len:39
float_conv_to_i4: dest:i src1:f len:39
float_conv_to_i8: dest:L src1:f len:39
float_conv_to_u4: dest:i src1:f len:39
float_conv_to_u8: dest:L src1:f len:39
float_conv_to_u2: dest:y src1:f len:39
float_conv_to_u1: dest:y src1:f len:39
float_conv_to_i: dest:i src1:f len:39
float_conv_to_ovf_i: dest:a src1:f len:30
float_conv_to_ovd_u: dest:a src1:f len:30
float_mul_ovf: 
float_ceq: dest:y src1:f src2:f len:25
float_cgt: dest:y src1:f src2:f len:25
float_cgt_un: dest:y src1:f src2:f len:37
float_clt: dest:y src1:f src2:f len:25
float_clt_un: dest:y src1:f src2:f len:32
float_conv_to_u: dest:i src1:f len:36
call_handler: len:10
aot_const: dest:i len:5
load_gotaddr: dest:i len:64
got_entry: dest:i src1:b len:7
x86_test_null: src1:i len:2
x86_compare_membase_reg: src1:b src2:i len:6
x86_compare_membase_imm: src1:b len:11
x86_compare_membase8_imm: src1:b len:8
x86_compare_mem_imm: len:11
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
x86_push_got_entry: src1:b len:7
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
tls_get: dest:i len:20
atomic_add_i4: src1:b src2:i dest:i len:16
atomic_add_new_i4: src1:b src2:i dest:i len:16
atomic_exchange_i4: src1:b src2:i dest:i len:24
memory_barrier: len:16

