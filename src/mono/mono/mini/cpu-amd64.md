# -*- mode:text; -*-
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
#  d  EDX register
#	l  long reg (forced eax:edx)
#  s  ECX register
#  c  register which can be used as a byte register (RAX..RDX)
#  A - first arg reg (rdi/rcx)
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
tailcall: len:255 clob:c
tailcall_reg: src1:b len:255 clob:c
tailcall_membase: src1:b len:255 clob:c

# tailcall_parameter models the size of moving one parameter,
# so that the required size of a branch around a tailcall can
# be accurately estimated; something like:
# void f1(volatile long *a)
# {
# a[large] = a[another large]
# }
#
# If the offsets fit in 32bits, then len:14:
#	48 8b 87 e0 04 00 00 	movq	1248(%rdi), %rax
#	48 89 87 00 08 00 00 	movq	%rax, 2048(%rdi)
#
# else 64bits:
#	48 b8 e0 fc b3 c4 04 00 00 00 	movabsq	$20479999200, %rax
#	48 8b 04 07 	movq	(%rdi,%rax), %rax
#	48 b9 00 00 b4 c4 04 00 00 00 	movabsq	$20480000000, %rcx
#	48 89 04 0f 	movq	%rax, (%rdi,%rcx)
#
# Frame size is artificially limited to 1GB in mono_arch_tailcall_supported.
# This is presently redundant with tailcall len:255, as the limit of
# near branches is [-128, +127], after which the limit is
# [-2GB, +2GB-1]
# FIXME A fixed size sequence to move parameters would moot this.
tailcall_parameter: len:14

br: len:6
label: len:0
seq_point: len:46 clob:c
il_seq_point: len:0

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
long_conv_to_r4: dest:f src1:i len:15
long_conv_to_r8: dest:f src1:i len:9
long_conv_to_u4: dest:i src1:i len:3
long_conv_to_u8: dest:i src1:i len:3
long_conv_to_r_un: dest:f src1:i len:64
long_conv_to_ovf_i4_un: dest:i src1:i len:16
long_conv_to_ovf_u4: dest:i src1:i len:15
long_conv_to_u2: dest:i src1:i len:4
long_conv_to_u1: dest:i src1:i len:4
zext_i4: dest:i src1:i len:4

long_mul_imm: dest:i src1:i clob:1 len:16
long_min: dest:i src1:i src2:i len:16 clob:1
long_min_un: dest:i src1:i src2:i len:16 clob:1
long_max: dest:i src1:i src2:i len:16 clob:1
long_max_un: dest:i src1:i src2:i len:16 clob:1

throw: src1:i len:24
rethrow: src1:i len:24
start_handler: len:16
endfinally: len:9
endfilter: src1:a len:9
get_ex_obj: dest:a len:16

ckfinite: dest:f src1:f len:43
ceq: dest:c len:8
cgt: dest:c len:8
cgt_un: dest:c len:8
clt: dest:c len:8
clt_un: dest:c len:8
localloc: dest:i src1:i len:120
compare: src1:i src2:i len:3
lcompare: src1:i src2:i len:3
icompare: src1:i src2:i len:3
compare_imm: src1:i len:13
icompare_imm: src1:i len:8
fcompare: src1:f src2:f clob:a len:13
rcompare: src1:f src2:f clob:a len:13
arglist: src1:b len:11
check_this: src1:b len:5
call: dest:a clob:c len:32
voidcall: clob:c len:32
voidcall_reg: src1:i clob:c len:32
voidcall_membase: src1:b clob:c len:32
fcall: dest:f len:64 clob:c
fcall_reg: dest:f src1:i len:64 clob:c
fcall_membase: dest:f src1:b len:64 clob:c
rcall: dest:f len:64 clob:c
rcall_reg: dest:f src1:i len:64 clob:c
rcall_membase: dest:f src1:b len:64 clob:c
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
r4const: dest:f len:17
r8const: dest:f len:12
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
loadu4_mem: dest:i len:10
amd64_loadi8_memindex: dest:i src1:i src2:i len:10
move: dest:i src1:i len:3
add_imm: dest:i src1:i len:8 clob:1
sub_imm: dest:i src1:i len:8 clob:1
mul_imm: dest:i src1:i len:12
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
float_cneq: dest:i src1:f src2:f len:42
float_cge: dest:i src1:f src2:f len:35
float_cle: dest:i src1:f src2:f len:35
float_ceq_membase: dest:i src1:f src2:b len:35
float_cgt_membase: dest:i src1:f src2:b len:35
float_cgt_un_membase: dest:i src1:f src2:b len:48
float_clt_membase: dest:i src1:f src2:b len:35
float_clt_un_membase: dest:i src1:f src2:b len:42
float_conv_to_u: dest:i src1:f len:46

# R4 opcodes
r4_conv_to_i1: dest:i src1:f len:32
r4_conv_to_u1: dest:i src1:f len:32
r4_conv_to_i2: dest:i src1:f len:32
r4_conv_to_u2: dest:i src1:f len:32
r4_conv_to_i4: dest:i src1:f len:16
r4_conv_to_u4: dest:i src1:f len:32
r4_conv_to_i8: dest:i src1:f len:32
r4_conv_to_i: dest:i src1:f len:32
r4_conv_to_r8: dest:f src1:f len:17
r4_conv_to_r4: dest:f src1:f len:17
r4_add: dest:f src1:f src2:f clob:1 len:5
r4_sub: dest:f src1:f src2:f clob:1 len:5
r4_mul: dest:f src1:f src2:f clob:1 len:5
r4_div: dest:f src1:f src2:f clob:1 len:5
r4_neg: dest:f src1:f clob:1 len:23
r4_ceq: dest:i src1:f src2:f len:35
r4_cgt: dest:i src1:f src2:f len:35
r4_cgt_un: dest:i src1:f src2:f len:48
r4_clt: dest:i src1:f src2:f len:35
r4_clt_un: dest:i src1:f src2:f len:42
r4_cneq: dest:i src1:f src2:f len:42
r4_cge: dest:i src1:f src2:f len:35
r4_cle: dest:i src1:f src2:f len:35

fmove: dest:f src1:f len:8
rmove: dest:f src1:f len:8
move_f_to_i4: dest:i src1:f len:16
move_i4_to_f: dest:f src1:i len:16
move_f_to_i8: dest:i src1:f len:5
move_i8_to_f: dest:f src1:i len:5
call_handler: len:14 clob:c
aotconst: dest:i len:10
gc_safe_point: clob:c src1:i len:40
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
amd64_lea_membase: dest:i src1:i len:11
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
tls_get: dest:i len:32
tls_set: src1:i len:16
atomic_add_i4: src1:b src2:i dest:i len:32
atomic_add_i8: src1:b src2:i dest:i len:32
atomic_exchange_i4: src1:b src2:i dest:i len:12
atomic_exchange_i8: src1:b src2:i dest:i len:12
atomic_cas_i4: src1:b src2:i src3:a dest:a len:24
atomic_cas_i8: src1:b src2:i src3:a dest:a len:24
memory_barrier: len:3
atomic_load_i1: dest:c src1:b len:9
atomic_load_u1: dest:c src1:b len:9
atomic_load_i2: dest:i src1:b len:9
atomic_load_u2: dest:i src1:b len:9
atomic_load_i4: dest:i src1:b len:9
atomic_load_u4: dest:i src1:b len:9
atomic_load_i8: dest:i src1:b len:9
atomic_load_u8: dest:i src1:b len:9
atomic_load_r4: dest:f src1:b len:16
atomic_load_r8: dest:f src1:b len:16
atomic_store_i1: dest:b src1:c len:12
atomic_store_u1: dest:b src1:c len:12
atomic_store_i2: dest:b src1:i len:12
atomic_store_u2: dest:b src1:i len:12
atomic_store_i4: dest:b src1:i len:12
atomic_store_u4: dest:b src1:i len:12
atomic_store_i8: dest:b src1:i len:12
atomic_store_u8: dest:b src1:i len:12
atomic_store_r4: dest:b src1:f len:18
atomic_store_r8: dest:b src1:f len:13
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
sext_i1: dest:i src1:i len:4
sext_i2: dest:i src1:i len:4
sext_i4: dest:i src1:i len:8

laddcc: dest:i src1:i src2:i len:3 clob:1
lsubcc: dest:i src1:i src2:i len:3 clob:1

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
int_rem_un_imm: dest:d src1:i clob:a len:32
int_and_imm: dest:i src1:i clob:1 len:8
int_or_imm: dest:i src1:i clob:1 len:8
int_xor_imm: dest:i src1:i clob:1 len:8
int_shl_imm: dest:i src1:i clob:1 len:8
int_shr_imm: dest:i src1:i clob:1 len:8
int_shr_un_imm: dest:i src1:i clob:1 len:8
int_min: dest:i src1:i src2:i len:16 clob:1
int_max: dest:i src1:i src2:i len:16 clob:1
int_min_un: dest:i src1:i src2:i len:16 clob:1
int_max_un: dest:i src1:i src2:i len:16 clob:1

int_neg: dest:i src1:i clob:1 len:4
int_not: dest:i src1:i clob:1 len:4
int_conv_to_r4: dest:f src1:i len:15
int_conv_to_r8: dest:f src1:i len:9
int_ceq: dest:c len:8
int_cgt: dest:c len:8
int_cgt_un: dest:c len:8
int_clt: dest:c len:8
int_clt_un: dest:c len:8

int_cneq: dest:c len:8
int_cge: dest:c len:8
int_cle: dest:c len:8
int_cge_un: dest:c len:8
int_cle_un: dest:c len:8

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

card_table_wbarrier: src1:a src2:i clob:d len:56

relaxed_nop: len:2
hard_nop: len:1

# Linear IR opcodes
nop: len:0
dummy_use: src1:i len:0
dummy_iconst: dest:i len:0
dummy_i8const: dest:i len:0
dummy_r8const: dest:f len:0
dummy_r4const: dest:f len:0
not_reached: len:0
not_null: src1:i len:0

long_ceq: dest:c len:64
long_cgt: dest:c len:64
long_cgt_un: dest:c len:64
long_clt: dest:c len:64
long_clt_un: dest:c len:64

int_conv_to_i1: dest:i src1:i len:4
int_conv_to_i2: dest:i src1:i len:4
int_conv_to_i4: dest:i src1:i len:3
int_conv_to_i8: dest:i src1:i len:3
int_conv_to_u4: dest:i src1:i len:3
int_conv_to_u8: dest:i src1:i len:3

int_conv_to_u: dest:i src1:i len:4
int_conv_to_u2: dest:i src1:i len:4
int_conv_to_u1: dest:i src1:i len:4
int_conv_to_i: dest:i src1:i len:4

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
cond_exc_ino: len:8
cond_exc_inc: len:8

x86_compare_membase8_imm: src1:b len:9

jump_table: dest:i len:18

cmov_ieq: dest:i src1:i src2:i len:16 clob:1
cmov_ige: dest:i src1:i src2:i len:16 clob:1
cmov_igt: dest:i src1:i src2:i len:16 clob:1
cmov_ile: dest:i src1:i src2:i len:16 clob:1
cmov_ilt: dest:i src1:i src2:i len:16 clob:1
cmov_ine_un: dest:i src1:i src2:i len:16 clob:1
cmov_ige_un: dest:i src1:i src2:i len:16 clob:1
cmov_igt_un: dest:i src1:i src2:i len:16 clob:1
cmov_ile_un: dest:i src1:i src2:i len:16 clob:1
cmov_ilt_un: dest:i src1:i src2:i len:16 clob:1

cmov_leq: dest:i src1:i src2:i len:16 clob:1
cmov_lge: dest:i src1:i src2:i len:16 clob:1
cmov_lgt: dest:i src1:i src2:i len:16 clob:1
cmov_lle: dest:i src1:i src2:i len:16 clob:1
cmov_llt: dest:i src1:i src2:i len:16 clob:1
cmov_lne_un: dest:i src1:i src2:i len:16 clob:1
cmov_lge_un: dest:i src1:i src2:i len:16 clob:1
cmov_lgt_un: dest:i src1:i src2:i len:16 clob:1
cmov_lle_un: dest:i src1:i src2:i len:16 clob:1
cmov_llt_un: dest:i src1:i src2:i len:16 clob:1

long_add_imm: dest:i src1:i clob:1 len:12
long_sub_imm: dest:i src1:i clob:1 len:12
long_and_imm: dest:i src1:i clob:1 len:12
long_or_imm: dest:i src1:i clob:1 len:12
long_xor_imm: dest:i src1:i clob:1 len:12

lcompare_imm: src1:i len:13

amd64_compare_membase_reg: src1:b src2:i len:9
amd64_compare_membase_imm: src1:b len:14
amd64_compare_reg_membase: src1:i src2:b len:9

amd64_add_reg_membase: dest:i src1:i src2:b clob:1 len:14
amd64_sub_reg_membase: dest:i src1:i src2:b clob:1 len:14
amd64_and_reg_membase: dest:i src1:i src2:b clob:1 len:14
amd64_or_reg_membase: dest:i src1:i src2:b clob:1 len:14
amd64_xor_reg_membase: dest:i src1:i src2:b clob:1 len:14

amd64_add_membase_imm: src1:b len:16
amd64_sub_membase_imm: src1:b len:16
amd64_and_membase_imm: src1:b len:13
amd64_or_membase_imm: src1:b len:13
amd64_xor_membase_imm: src1:b len:13

x86_and_membase_imm: src1:b len:12
x86_or_membase_imm: src1:b len:12
x86_xor_membase_imm: src1:b len:12

x86_add_membase_reg: src1:b src2:i len:12
x86_sub_membase_reg: src1:b src2:i len:12
x86_and_membase_reg: src1:b src2:i len:12
x86_or_membase_reg: src1:b src2:i len:12
x86_xor_membase_reg: src1:b src2:i len:12
x86_mul_membase_reg: src1:b src2:i len:14

amd64_add_membase_reg: src1:b src2:i len:13
amd64_sub_membase_reg: src1:b src2:i len:13
amd64_and_membase_reg: src1:b src2:i len:13
amd64_or_membase_reg: src1:b src2:i len:13
amd64_xor_membase_reg: src1:b src2:i len:13
amd64_mul_membase_reg: src1:b src2:i len:15

float_conv_to_r4: dest:f src1:f len:17

vcall2: len:64 clob:c
vcall2_reg: src1:i len:64 clob:c
vcall2_membase: src1:b len:64 clob:c

dyn_call: src1:i src2:i len:192 clob:c

localloc_imm: dest:i len:120

load_mem: dest:i len:16
loadi8_mem: dest:i len:16
loadi4_mem: dest:i len:16
loadu1_mem: dest:i len:16
loadu2_mem: dest:i len:16


#SIMD

addps: dest:x src1:x src2:x len:4 clob:1
divps: dest:x src1:x src2:x len:4 clob:1
mulps: dest:x src1:x src2:x len:4 clob:1
subps: dest:x src1:x src2:x len:4 clob:1
maxps: dest:x src1:x src2:x len:4 clob:1
minps: dest:x src1:x src2:x len:4 clob:1
compps: dest:x src1:x src2:x len:5 clob:1
andps: dest:x src1:x src2:x len:4 clob:1
andnps: dest:x src1:x src2:x len:4 clob:1
orps: dest:x src1:x src2:x len:4 clob:1
xorps: dest:x src1:x src2:x len:4 clob:1

haddps: dest:x src1:x src2:x len:5 clob:1
hsubps: dest:x src1:x src2:x len:5 clob:1
addsubps: dest:x src1:x src2:x len:5 clob:1
dupps_low: dest:x src1:x len:5
dupps_high: dest:x src1:x len:5

addpd: dest:x src1:x src2:x len:5 clob:1
divpd: dest:x src1:x src2:x len:5 clob:1
mulpd: dest:x src1:x src2:x len:5 clob:1
subpd: dest:x src1:x src2:x len:5 clob:1
maxpd: dest:x src1:x src2:x len:5 clob:1
minpd: dest:x src1:x src2:x len:5 clob:1
comppd: dest:x src1:x src2:x len:6 clob:1
andpd: dest:x src1:x src2:x len:5 clob:1
andnpd: dest:x src1:x src2:x len:5 clob:1
orpd: dest:x src1:x src2:x len:5 clob:1
xorpd: dest:x src1:x src2:x len:5 clob:1
sqrtpd: dest:x src1:x len:5 clob:1

haddpd: dest:x src1:x src2:x len:6 clob:1
hsubpd: dest:x src1:x src2:x len:6 clob:1
addsubpd: dest:x src1:x src2:x len:6 clob:1
duppd: dest:x src1:x len:6

pand: dest:x src1:x src2:x len:5 clob:1
pandn: dest:x src1:x src2:x len:5 clob:1
por: dest:x src1:x src2:x len:5 clob:1
pxor: dest:x src1:x src2:x len:5 clob:1

sqrtps: dest:x src1:x len:5
rsqrtps: dest:x src1:x len:5
rcpps: dest:x src1:x len:5

pshuflew_high: dest:x src1:x len:6
pshuflew_low: dest:x src1:x len:6
pshufled: dest:x src1:x len:6
shufps: dest:x src1:x src2:x len:5 clob:1
shufpd: dest:x src1:x src2:x len:6 clob:1

extract_mask: dest:i src1:x len:6

paddb: dest:x src1:x src2:x len:5 clob:1
paddw: dest:x src1:x src2:x len:5 clob:1
paddd: dest:x src1:x src2:x len:5 clob:1
paddq: dest:x src1:x src2:x len:5 clob:1

psubb: dest:x src1:x src2:x len:5 clob:1
psubw: dest:x src1:x src2:x len:5 clob:1
psubd: dest:x src1:x src2:x len:5 clob:1
psubq: dest:x src1:x src2:x len:5 clob:1

pmaxb_un: dest:x src1:x src2:x len:5 clob:1
pmaxw_un: dest:x src1:x src2:x len:6 clob:1
pmaxd_un: dest:x src1:x src2:x len:6 clob:1

pmaxb: dest:x src1:x src2:x len:6 clob:1
pmaxw: dest:x src1:x src2:x len:5 clob:1
pmaxd: dest:x src1:x src2:x len:6 clob:1

pavgb_un: dest:x src1:x src2:x len:5 clob:1
pavgw_un: dest:x src1:x src2:x len:5 clob:1

pminb_un: dest:x src1:x src2:x len:5 clob:1
pminw_un: dest:x src1:x src2:x len:6 clob:1
pmind_un: dest:x src1:x src2:x len:6 clob:1

pminb: dest:x src1:x src2:x len:6 clob:1
pminw: dest:x src1:x src2:x len:5 clob:1
pmind: dest:x src1:x src2:x len:6 clob:1

pcmpeqb: dest:x src1:x src2:x len:5 clob:1
pcmpeqw: dest:x src1:x src2:x len:5 clob:1
pcmpeqd: dest:x src1:x src2:x len:5 clob:1
pcmpeqq: dest:x src1:x src2:x len:6 clob:1

pcmpgtb: dest:x src1:x src2:x len:5 clob:1
pcmpgtw: dest:x src1:x src2:x len:5 clob:1
pcmpgtd: dest:x src1:x src2:x len:5 clob:1
pcmpgtq: dest:x src1:x src2:x len:6 clob:1

psum_abs_diff: dest:x src1:x src2:x len:5 clob:1

unpack_lowb: dest:x src1:x src2:x len:5 clob:1
unpack_loww: dest:x src1:x src2:x len:5 clob:1
unpack_lowd: dest:x src1:x src2:x len:5 clob:1
unpack_lowq: dest:x src1:x src2:x len:5 clob:1
unpack_lowps: dest:x src1:x src2:x len:5 clob:1
unpack_lowpd: dest:x src1:x src2:x len:5 clob:1

unpack_highb: dest:x src1:x src2:x len:5 clob:1
unpack_highw: dest:x src1:x src2:x len:5 clob:1
unpack_highd: dest:x src1:x src2:x len:5 clob:1
unpack_highq: dest:x src1:x src2:x len:5 clob:1
unpack_highps: dest:x src1:x src2:x len:5 clob:1
unpack_highpd: dest:x src1:x src2:x len:5 clob:1

packw: dest:x src1:x src2:x len:5 clob:1
packd: dest:x src1:x src2:x len:5 clob:1

packw_un: dest:x src1:x src2:x len:5 clob:1
packd_un: dest:x src1:x src2:x len:6 clob:1

paddb_sat: dest:x src1:x src2:x len:5 clob:1
paddb_sat_un: dest:x src1:x src2:x len:5 clob:1

paddw_sat: dest:x src1:x src2:x len:5 clob:1
paddw_sat_un: dest:x src1:x src2:x len:5 clob:1

psubb_sat: dest:x src1:x src2:x len:5 clob:1
psubb_sat_un: dest:x src1:x src2:x len:5 clob:1

psubw_sat: dest:x src1:x src2:x len:5 clob:1
psubw_sat_un: dest:x src1:x src2:x len:5 clob:1

pmulw: dest:x src1:x src2:x len:5 clob:1
pmuld: dest:x src1:x src2:x len:6 clob:1
pmulq: dest:x src1:x src2:x len:5 clob:1

pmulw_high_un: dest:x src1:x src2:x len:5 clob:1
pmulw_high: dest:x src1:x src2:x len:5 clob:1

pshrw: dest:x src1:x len:6 clob:1
pshrw_reg: dest:x src1:x src2:x len:5 clob:1

psarw: dest:x src1:x len:6 clob:1
psarw_reg: dest:x src1:x src2:x len:5 clob:1

pshlw: dest:x src1:x len:6 clob:1
pshlw_reg: dest:x src1:x src2:x len:5 clob:1

pshrd: dest:x src1:x len:6 clob:1
pshrd_reg: dest:x src1:x src2:x len:5 clob:1

psard: dest:x src1:x len:6 clob:1
psard_reg: dest:x src1:x src2:x len:5 clob:1

pshld: dest:x src1:x len:6 clob:1
pshld_reg: dest:x src1:x src2:x len:5 clob:1

pshrq: dest:x src1:x len:6 clob:1
pshrq_reg: dest:x src1:x src2:x len:5 clob:1

pshlq: dest:x src1:x len:6 clob:1
pshlq_reg: dest:x src1:x src2:x len:5 clob:1

cvtdq2pd: dest:x src1:x len:5 clob:1
cvtdq2ps: dest:x src1:x len:4 clob:1
cvtpd2dq: dest:x src1:x len:5 clob:1
cvtpd2ps: dest:x src1:x len:5 clob:1
cvtps2dq: dest:x src1:x len:5 clob:1
cvtps2pd: dest:x src1:x len:4 clob:1
cvttpd2dq: dest:x src1:x len:5 clob:1
cvttps2dq: dest:x src1:x len:5 clob:1

xmove: dest:x src1:x len:5
xzero: dest:x len:5
xones: dest:x len:5

iconv_to_x: dest:x src1:i len:5
extract_i4: dest:i src1:x len:5

extract_i8: dest:i src1:x len:9

extract_i2: dest:i src1:x len:13
extract_i1: dest:i src1:x len:13
extract_r8: dest:f src1:x len:5

iconv_to_r4_raw: dest:f src1:i len:10

insert_i2: dest:x src1:x src2:i len:6 clob:1

extractx_u2: dest:i src1:x len:6
insertx_u1_slow: dest:x src1:i src2:i len:18 clob:x

insertx_i4_slow: dest:x src1:x src2:i len:16 clob:x
insertx_i8_slow: dest:x src1:x src2:i len:13
insertx_r4_slow: dest:x src1:x src2:f len:24
insertx_r8_slow: dest:x src1:x src2:f len:24

loadx_membase: dest:x src1:b len:9
storex_membase: dest:b src1:x len:9
storex_membase_reg: dest:b src1:x len:9

loadx_aligned_membase: dest:x src1:b len:7
storex_aligned_membase_reg: dest:b src1:x len:7
storex_nta_membase_reg: dest:b src1:x len:7

fconv_to_r8_x: dest:x src1:f len:4
xconv_r8_to_i4: dest:y src1:x len:7

prefetch_membase: src1:b len:4

expand_i2: dest:x src1:i len:18
expand_i4: dest:x src1:i len:11
expand_i8: dest:x src1:i len:11
expand_r4: dest:x src1:f len:16
expand_r8: dest:x src1:f len:13

roundp: dest:x src1:x len:10

liverange_start: len:0
liverange_end: len:0
gc_liveness_def: len:0
gc_liveness_use: len:0
gc_spill_slot_liveness_def: len:0
gc_param_slot_liveness_def: len:0

generic_class_init: src1:A len:32 clob:c
get_last_error: dest:i len:32

fill_prof_call_ctx: src1:i len:128

lzcnt32: dest:i src1:i len:16
lzcnt64: dest:i src1:i len:16
popcnt32: dest:i src1:i len:16
popcnt64: dest:i src1:i len:16
