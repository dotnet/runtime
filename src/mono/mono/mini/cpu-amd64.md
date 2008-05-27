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
#	i  integer register
#	b  base register (used in address references)
#	f  floating point register
#	a  EAX register
#   d  EDX register
#	l  long reg (forced eax:edx)
#   s  ECX register
#   c  register which can be used as a byte register (RAX..RDX)
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
#	c  clobbers caller-save registers
#	1  clobbers the first source register
#	a  EAX is clobbered
#   d  EDX is clobbered
#	x  both the source operands are clobbered (xchg)
#   m  sets an XMM reg
#
# flags:spec        describe if the instruction uses or sets the flags (unused)
#
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
break: len:2
jmp: len:120
br: len:6
label: len:0

long_add: dest:i src1:i src2:i len:3 clob:1
long_sub: dest:i src1:i src2:i len:3 clob:1
long_mul: dest:i src1:i src2:i len:4 clob:1
long_div: dest:a src1:a src2:i len:16 clob:d
long_div_un: dest:a src1:a src2:i len:16 clob:d
long_rem: dest:d src1:a src2:i len:16 clob:a
long_rem_un: dest:d src1:a src2:i len:16 clob:a
long_and: dest:i src1:i src2:i len:3 clob:1
long_or: dest:i src1:i src2:i len:3 clob:1
long_xor: dest:i src1:i src2:i len:3 clob:1
long_shl: dest:i src1:i src2:s clob:1 len:3
long_shr: dest:i src1:i src2:s clob:1 len:3
long_shr_un: dest:i src1:i src2:s clob:1 len:3
long_neg: dest:i src1:i len:3 clob:1
long_not: dest:i src1:i len:3 clob:1
long_conv_to_i1: dest:i src1:i len:4
long_conv_to_i2: dest:i src1:i len:4
long_conv_to_i4: dest:i src1:i len:3
long_conv_to_i8: dest:i src1:i len:3
long_conv_to_r4: dest:f src1:i len:9
long_conv_to_r8: dest:f src1:i len:9
long_conv_to_u4: dest:i src1:i len:3
long_conv_to_u8: dest:i src1:i len:3
long_conv_to_r_un: dest:f src1:i len:64
long_conv_to_ovf_i4_un: dest:i src1:i len:16
long_conv_to_ovf_u4: dest:i src1:i len:15
long_conv_to_u2: dest:i src1:i len:4
long_conv_to_u1: dest:i src1:i len:4
zext_i4: dest:i src1:i len:4

long_mul_imm: dest:i src1:i clob:1 len:12
long_min: dest:i src1:i src2:i len:16 clob:1
long_max: dest:i src1:i src2:i len:16 clob:1

throw: src1:i len:18
rethrow: src1:i len:18
start_handler: len:9
endfinally: len:9
endfilter: src1:a len:9
ckfinite: dest:f src1:f len:43
ceq: dest:c len:8
cgt: dest:c len:8
cgt.un: dest:c len:8
clt: dest:c len:8
clt.un: dest:c len:8
localloc: dest:i src1:i len:84
compare: src1:i src2:i len:3
lcompare: src1:i src2:i len:3
icompare: src1:i src2:i len:3
compare_imm: src1:i len:13
icompare_imm: src1:i len:8
fcompare: src1:f src2:f clob:a len:13
oparglist: src1:b len:11
outarg: src1:i len:2
setret: dest:a src1:i len:3
setlret: dest:i src1:i src2:i len:5
checkthis: src1:b len:5
call: dest:a clob:c len:32
voidcall: clob:c len:32
voidcall_reg: src1:i clob:c len:32
voidcall_membase: src1:b clob:c len:32
fcall: dest:f len:64 clob:c
fcall_reg: dest:f src1:i len:64 clob:c
fcall_membase: dest:f src1:b len:64 clob:c
lcall: dest:a len:64 clob:c
lcall_reg: dest:a src1:i len:64 clob:c
lcall_membase: dest:a src1:b len:64 clob:c
vcall: len:64 clob:c
vcall_reg: src1:i len:64 clob:c
vcall_membase: src1:b len:64 clob:c
call_reg: dest:a src1:i len:32 clob:c
call_membase: dest:a src1:b len:32 clob:c
iconst: dest:i len:10
i8const: dest:i len:10
r4const: dest:f len:14
r8const: dest:f len:9
store_membase_imm: dest:b len:15
store_membase_reg: dest:b src1:i len:9
storei8_membase_reg: dest:b src1:i len:9
storei1_membase_imm: dest:b len:11
storei1_membase_reg: dest:b src1:c len:9
storei2_membase_imm: dest:b len:13
storei2_membase_reg: dest:b src1:i len:9
storei4_membase_imm: dest:b len:13
storei4_membase_reg: dest:b src1:i len:9
storei8_membase_imm: dest:b len:18
storer4_membase_reg: dest:b src1:f len:15
storer8_membase_reg: dest:b src1:f len:10
load_membase: dest:i src1:b len:8
loadi1_membase: dest:c src1:b len:9
loadu1_membase: dest:c src1:b len:9
loadi2_membase: dest:i src1:b len:9
loadu2_membase: dest:i src1:b len:9
loadi4_membase: dest:i src1:b len:9
loadu4_membase: dest:i src1:b len:9
loadi8_membase: dest:i src1:b len:18
loadr4_membase: dest:f src1:b len:16
loadr8_membase: dest:f src1:b len:16
loadr8_spill_membase: src1:b len:9
loadu4_mem: dest:i len:10
amd64_loadi8_memindex: dest:i src1:i src2:i len:10
move: dest:i src1:i len:3
add_imm: dest:i src1:i len:8 clob:1
sub_imm: dest:i src1:i len:8 clob:1
mul_imm: dest:i src1:i len:11
# there is no actual support for division or reminder by immediate
# we simulate them, though (but we need to change the burg rules 
# to allocate a symbolic reg for src2)
div_imm: dest:a src1:i src2:i len:16 clob:d
div_un_imm: dest:a src1:i src2:i len:16 clob:d
rem_imm: dest:d src1:i src2:i len:16 clob:a
rem_un_imm: dest:d src1:i src2:i len:16 clob:a
and_imm: dest:i src1:i len:8 clob:1
or_imm: dest:i src1:i len:8 clob:1
xor_imm: dest:i src1:i len:8 clob:1
shl_imm: dest:i src1:i len:8 clob:1
shr_imm: dest:i src1:i len:8 clob:1
shr_un_imm: dest:i src1:i len:8 clob:1
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
cond_exc_ov: len:8
cond_exc_no: len:8
cond_exc_c: len:8
cond_exc_nc: len:8
cond_exc_iov: len:8
cond_exc_ic: len:8

long_conv_to_ovf_i: dest:i src1:i src2:i len:40
long_mul_ovf: dest:i src1:i src2:i clob:1 len:16
long_mul_ovf_un: dest:i src1:i src2:i len:22
long_shr_imm: dest:i src1:i clob:1 len:11
long_shr_un_imm: dest:i src1:i clob:1 len:11
long_shl_imm: dest:i src1:i clob:1 len:11

long_beq: len:8
long_bge: len:8
long_bgt: len:8
long_ble: len:8
long_blt: len:8
long_bne_un: len:8
long_bge_un: len:8
long_bgt_un: len:8
long_ble_un: len:8
long_blt_un: len:8

float_beq: len:13
float_bne_un: len:18
float_blt: len:13
float_blt_un: len:30
float_bgt: len:13
float_bgt_un: len:30
float_bge: len:32
float_bge_un: len:13
float_ble: len:32
float_ble_un: len:13
float_add: dest:f src1:f src2:f clob:1 len:5
float_sub: dest:f src1:f src2:f clob:1 len:5
float_mul: dest:f src1:f src2:f clob:1 len:5
float_div: dest:f src1:f src2:f clob:1 len:5
float_div_un: dest:f src1:f src2:f clob:1 len:5
float_rem: dest:f src1:f src2:f clob:1 len:19
float_rem_un: dest:f src1:f src2:f clob:1 len:19
float_neg: dest:f src1:f clob:1 len:23
float_not: dest:f src1:f clob:1 len:3
float_conv_to_i1: dest:i src1:f len:49
float_conv_to_i2: dest:i src1:f len:49
float_conv_to_i4: dest:i src1:f len:49
float_conv_to_i8: dest:i src1:f len:49
float_conv_to_u4: dest:i src1:f len:49
float_conv_to_u8: dest:i src1:f len:49
float_conv_to_u2: dest:i src1:f len:49
float_conv_to_u1: dest:i src1:f len:49
float_conv_to_i: dest:i src1:f len:49
float_conv_to_ovf_i: dest:a src1:f len:40
float_conv_to_ovd_u: dest:a src1:f len:40
float_mul_ovf: 
float_ceq: dest:i src1:f src2:f len:35
float_cgt: dest:i src1:f src2:f len:35
float_cgt_un: dest:i src1:f src2:f len:48
float_clt: dest:i src1:f src2:f len:35
float_clt_un: dest:i src1:f src2:f len:42
float_ceq_membase: dest:i src1:f src2:b len:35
float_cgt_membase: dest:i src1:f src2:b len:35
float_cgt_un_membase: dest:i src1:f src2:b len:48
float_clt_membase: dest:i src1:f src2:b len:35
float_clt_un_membase: dest:i src1:f src2:b len:42
float_conv_to_u: dest:i src1:f len:46
fmove: dest:f src1:f len:8
call_handler: len:14
aot_const: dest:i len:10
x86_test_null: src1:i len:5
x86_compare_membase_reg: src1:b src2:i len:9
x86_compare_membase_imm: src1:b len:13
x86_compare_reg_membase: src1:i src2:b len:8
x86_inc_reg: dest:i src1:i clob:1 len:3
x86_inc_membase: src1:b len:8
x86_dec_reg: dest:i src1:i clob:1 len:3
x86_dec_membase: src1:b len:8
x86_add_membase_imm: src1:b len:13
x86_sub_membase_imm: src1:b len:13
x86_push: src1:i len:3
x86_push_imm: len:6
x86_push_membase: src1:b len:8
x86_push_obj: src1:b len:40
x86_lea: dest:i src1:i src2:i len:8
x86_lea_membase: dest:i src1:i len:11
x86_xchg: src1:i src2:i clob:x len:2
x86_fpop: src1:f len:3
x86_seteq_membase: src1:b len:9

x86_add_reg_membase: dest:i src1:i src2:b clob:1 len:13
x86_sub_reg_membase: dest:i src1:i src2:b clob:1 len:13
x86_mul_reg_membase: dest:i src1:i src2:b clob:1 len:13
x86_and_reg_membase: dest:i src1:i src2:b clob:1 len:13
x86_or_reg_membase: dest:i src1:i src2:b clob:1 len:13
x86_xor_reg_membase: dest:i src1:i src2:b clob:1 len:13

amd64_test_null: src1:i len:5
amd64_icompare_membase_reg: src1:b src2:i len:8
amd64_icompare_membase_imm: src1:b len:13
amd64_icompare_reg_membase: src1:i src2:b len:8
amd64_set_xmmreg_r4: dest:f src1:f len:14 clob:m
amd64_set_xmmreg_r8: dest:f src1:f len:14 clob:m
amd64_save_sp_to_lmf: len:16
tls_get: dest:i len:16
atomic_add_i4: src1:b src2:i dest:i len:32
atomic_add_new_i4: src1:b src2:i dest:i len:32
atomic_exchange_i4: src1:b src2:i dest:a len:32
atomic_add_i8: src1:b src2:i dest:i len:32
atomic_add_new_i8: src1:b src2:i dest:i len:32
atomic_exchange_i8: src1:b src2:i dest:a len:32
atomic_cas_imm_i4: src1:b src2:i dest:a len:32
atomic_cas_imm_i8: src1:b src2:i dest:a len:32
memory_barrier: len:16
adc: dest:i src1:i src2:i len:3 clob:1
addcc: dest:i src1:i src2:i len:3 clob:1
subcc: dest:i src1:i src2:i len:3 clob:1
adc_imm: dest:i src1:i len:8 clob:1
sbb: dest:i src1:i src2:i len:3 clob:1
sbb_imm: dest:i src1:i len:8 clob:1
br_reg: src1:i len:3
sin: dest:f src1:f len:32
cos: dest:f src1:f len:32
abs: dest:f src1:f clob:1 len:32
tan: dest:f src1:f len:59
atan: dest:f src1:f len:9
sqrt: dest:f src1:f len:32
bigmul: len:3 dest:i src1:a src2:i
bigmul_un: len:3 dest:i src1:a src2:i
sext_i1: dest:i src1:i len:4
sext_i2: dest:i src1:i len:4
sext_i4: dest:i src1:i len:8

# 32 bit opcodes
int_add: dest:i src1:i src2:i clob:1 len:4
int_sub: dest:i src1:i src2:i clob:1 len:4
int_mul: dest:i src1:i src2:i clob:1 len:4
int_mul_ovf: dest:i src1:i src2:i clob:1 len:32
int_mul_ovf_un: dest:i src1:i src2:i clob:1 len:32
int_div: dest:a src1:a src2:i clob:d len:32
int_div_un: dest:a src1:a src2:i clob:d len:32
int_rem: dest:d src1:a src2:i clob:a len:32
int_rem_un: dest:d src1:a src2:i clob:a len:32
int_and: dest:i src1:i src2:i clob:1 len:4
int_or: dest:i src1:i src2:i clob:1 len:4
int_xor: dest:i src1:i src2:i clob:1 len:4
int_shl: dest:i src1:i src2:s clob:1 len:4
int_shr: dest:i src1:i src2:s clob:1 len:4
int_shr_un: dest:i src1:i src2:s clob:1 len:4
int_adc: dest:i src1:i src2:i clob:1 len:4
int_adc_imm: dest:i src1:i clob:1 len:8
int_sbb: dest:i src1:i src2:i clob:1 len:4
int_sbb_imm: dest:i src1:i clob:1 len:8
int_addcc: dest:i src1:i src2:i clob:1 len:16
int_subcc: dest:i src1:i src2:i clob:1 len:16
int_add_imm: dest:i src1:i clob:1 len:8
int_sub_imm: dest:i src1:i clob:1 len:8
int_mul_imm: dest:i src1:i clob:1 len:32
int_div_imm: dest:a src1:i clob:d len:32
int_div_un_imm: dest:a src1:i clob:d len:32
int_rem_imm: dest:d src1:i clob:a len:32
int_rem_un_imm: dest:d src1:i clob:a len:32
int_and_imm: dest:i src1:i clob:1 len:8
int_or_imm: dest:i src1:i clob:1 len:8
int_xor_imm: dest:i src1:i clob:1 len:8
int_shl_imm: dest:i src1:i clob:1 len:8
int_shr_imm: dest:i src1:i clob:1 len:8
int_shr_un_imm: dest:i src1:i clob:1 len:8
int_min: dest:i src1:i src2:i len:16 clob:1
int_max: dest:i src1:i src2:i len:16 clob:1

int_neg: dest:i src1:i clob:1 len:4
int_not: dest:i src1:i clob:1 len:4
int_conv_to_r4: dest:f src1:i len:9
int_conv_to_r8: dest:f src1:i len:9
int_ceq: dest:c len:8
int_cgt: dest:c len:8
int_cgt_un: dest:c len:8
int_clt: dest:c len:8
int_clt_un: dest:c len:8
int_beq: len:8
int_bne_un: len:8
int_blt: len:8
int_blt_un: len:8
int_bgt: len:8
int_bgt_un: len:8
int_bge: len:8
int_bge_un: len:8
int_ble: len:8
int_ble_un: len:8

