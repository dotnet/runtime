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
#	x  XMM reg (XMM0 - X007)
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
# Templates can be defined by using the 'template' keyword instead of an opcode name.
# The template name is assigned from a (required) 'name' specifier.
# To apply a template to an opcode, just use the template:template_name specifier: any value
# defined by the template can be overridden by adding more specifiers after the template.
#
# See the code in mini-x86.c for more details on how the specifiers are used.
#
break: len:1
call: dest:a clob:c len:17
tailcall: len:255 clob:c
tailcall_membase: src1:b len:255 clob:c # FIXME len
tailcall_reg: src1:b len:255 clob:c # FIXME len

# tailcall_parameter models the size of moving one parameter,
# so that the required size of a branch around a tailcall can
# be accurately estimated; something like:
# void f1(volatile long *a)
# {
# a[large] = a[another large]
# }
#
# This is like amd64 but without the rex bytes.
#
# Frame size is artificially limited to 1GB in mono_arch_tailcall_supported.
# This is presently redundant with tailcall len:255, as the limit of
# near branches is [-128, +127], after which the limit is
# [-2GB, +2GB-1]

# FIXME A fixed size sequence to move parameters would moot this.
tailcall_parameter: len:12

br: len:5
seq_point: len:26 clob:c
il_seq_point: len:0

int_beq: len:6
int_bge: len:6
int_bgt: len:6
int_ble: len:6
int_blt: len:6
int_bne_un: len:6
int_bge_un: len:6
int_bgt_un: len:6
int_ble_un: len:6
int_blt_un: len:6
label: len:0

#template: name:ibalu

int_add: dest:i src1:i src2:i clob:1 len:2
int_sub: dest:i src1:i src2:i clob:1 len:2
int_mul: dest:i src1:i src2:i clob:1 len:3
int_div: dest:a src1:a src2:i len:15 clob:d
int_div_un: dest:a src1:a src2:i len:15 clob:d
int_rem: dest:d src1:a src2:i len:15 clob:a
int_rem_un: dest:d src1:a src2:i len:15 clob:a
int_and: dest:i src1:i src2:i clob:1 len:2
int_or: dest:i src1:i src2:i clob:1 len:2
int_xor: dest:i src1:i src2:i clob:1 len:2
int_shl: dest:i src1:i src2:s clob:1 len:2
int_shr: dest:i src1:i src2:s clob:1 len:2
int_shr_un: dest:i src1:i src2:s clob:1 len:2
int_min: dest:i src1:i src2:i len:16 clob:1
int_min_un: dest:i src1:i src2:i len:16 clob:1
int_max: dest:i src1:i src2:i len:16 clob:1
int_max_un: dest:i src1:i src2:i len:16 clob:1

int_neg: dest:i src1:i len:2 clob:1
int_not: dest:i src1:i len:2 clob:1
int_conv_to_i1: dest:i src1:y len:3
int_conv_to_i2: dest:i src1:i len:3
int_conv_to_i4: dest:i src1:i len:2
int_conv_to_r4: dest:f src1:i len:13
int_conv_to_r8: dest:f src1:i len:7
int_conv_to_u4: dest:i src1:i
int_conv_to_u2: dest:i src1:i len:3
int_conv_to_u1: dest:i src1:y len:3
int_conv_to_i: dest:i src1:i len:3
int_mul_ovf: dest:i src1:i src2:i clob:1 len:9
int_mul_ovf_un: dest:i src1:i src2:i len:16

throw: src1:i len:13
rethrow: src1:i len:13
start_handler: len:16
endfinally: len:16
endfilter: src1:a len:16
get_ex_obj: dest:a len:16

ckfinite: dest:f src1:f len:32
ceq: dest:y len:6
cgt: dest:y len:6
cgt_un: dest:y len:6
clt: dest:y len:6
clt_un: dest:y len:6
localloc: dest:i src1:i len:120
compare: src1:i src2:i len:2
compare_imm: src1:i len:6
fcompare: src1:f src2:f clob:a len:9
arglist: src1:b len:10
check_this: src1:b len:3
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
store_membase_imm: dest:b len:11
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
storer8_membase_reg: dest:b src1:f len:7
load_membase: dest:i src1:b len:7
loadi1_membase: dest:y src1:b len:7
loadu1_membase: dest:y src1:b len:7
loadi2_membase: dest:i src1:b len:7
loadu2_membase: dest:i src1:b len:7
loadi4_membase: dest:i src1:b len:7
loadu4_membase: dest:i src1:b len:7
loadi8_membase: dest:i src1:b
loadr4_membase: dest:f src1:b len:7
loadr8_membase: dest:f src1:b len:7
loadu4_mem: dest:i len:9
move: dest:i src1:i len:2
addcc_imm: dest:i src1:i len:6 clob:1
add_imm: dest:i src1:i len:6 clob:1
subcc_imm: dest:i src1:i len:6 clob:1
sub_imm: dest:i src1:i len:6 clob:1
mul_imm: dest:i src1:i len:9
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
long_shr_imm: dest:L src1:L clob:1 len:10
long_shr_un_imm: dest:L src1:L clob:1 len:10
long_shl_imm: dest:L src1:L clob:1 len:10
float_beq: len:12
float_bne_un: len:18
float_blt: len:12
float_blt_un: len:20
float_bgt: len:12
float_bgt_un: len:20
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
float_cneq: dest:y src1:f src2:f len:25
float_cge: dest:y src1:f src2:f len:37
float_cle: dest:y src1:f src2:f len:37
float_conv_to_u: dest:i src1:f len:36
call_handler: len:11 clob:c
aotconst: dest:i len:5
load_gotaddr: dest:i len:64
got_entry: dest:i src1:b len:7
gc_safe_point: clob:c src1:i len:20
x86_test_null: src1:i len:2
x86_compare_membase_reg: src1:b src2:i len:7
x86_compare_membase_imm: src1:b len:11
x86_compare_membase8_imm: src1:b len:8
x86_compare_mem_imm: len:11
x86_compare_reg_membase: src1:i src2:b len:7
x86_inc_reg: dest:i src1:i clob:1 len:1
x86_inc_membase: src1:b len:7
x86_dec_reg: dest:i src1:i clob:1 len:1
x86_dec_membase: src1:b len:7
x86_add_membase_imm: src1:b len:11
x86_sub_membase_imm: src1:b len:11
x86_and_membase_imm: src1:b len:11
x86_or_membase_imm: src1:b len:11
x86_xor_membase_imm: src1:b len:11
x86_push: src1:i len:1
x86_push_imm: len:5
x86_push_membase: src1:b len:7
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

x86_add_reg_membase: dest:i src1:i src2:b clob:1 len:11
x86_sub_reg_membase: dest:i src1:i src2:b clob:1 len:11
x86_mul_reg_membase: dest:i src1:i src2:b clob:1 len:13

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
round: dest:f src1:f len:2
bigmul: len:2 dest:l src1:a src2:i
bigmul_un: len:2 dest:l src1:a src2:i
sext_i1: dest:i src1:y len:3
sext_i2: dest:i src1:y len:3
tls_get: dest:i len:32
tls_set: src1:i len:20
atomic_add_i4: src1:b src2:i dest:i len:16
atomic_exchange_i4: src1:b src2:i dest:a len:24
atomic_cas_i4: src1:b src2:i src3:a dest:a len:24
memory_barrier: len:16
atomic_load_i1: dest:y src1:b len:7
atomic_load_u1: dest:y src1:b len:7
atomic_load_i2: dest:i src1:b len:7
atomic_load_u2: dest:i src1:b len:7
atomic_load_i4: dest:i src1:b len:7
atomic_load_u4: dest:i src1:b len:7
atomic_load_r4: dest:f src1:b len:10
atomic_load_r8: dest:f src1:b len:10
atomic_store_i1: dest:b src1:y len:10
atomic_store_u1: dest:b src1:y len:10
atomic_store_i2: dest:b src1:i len:10
atomic_store_u2: dest:b src1:i len:10
atomic_store_i4: dest:b src1:i len:10
atomic_store_u4: dest:b src1:i len:10
atomic_store_r4: dest:b src1:f len:10
atomic_store_r8: dest:b src1:f len:10

card_table_wbarrier: src1:a src2:i clob:d len:34

relaxed_nop: len:2
hard_nop: len:1

# Linear IR opcodes
nop: len:0
dummy_use: src1:i len:0
dummy_iconst: dest:i len:0
dummy_r8const: dest:f len:0
dummy_r4const: dest:f len:0
not_reached: len:0
not_null: src1:i len:0

jump_table: dest:i len:5

int_adc: dest:i src1:i src2:i len:2 clob:1
int_addcc: dest:i src1:i src2:i len:2 clob:1
int_subcc: dest:i src1:i src2:i len:2 clob:1
int_sbb: dest:i src1:i src2:i len:2 clob:1

int_add_imm: dest:i src1:i len:6 clob:1
int_sub_imm: dest:i src1:i len:6 clob:1
int_mul_imm: dest:i src1:i len:9
int_div_imm: dest:a src1:a len:15 clob:d
int_div_un_imm: dest:a src1:a len:15 clob:d
int_rem_imm: dest:a src1:a len:15 clob:d
int_rem_un_imm: dest:d src1:a len:15 clob:a
int_and_imm: dest:i src1:i len:6 clob:1
int_or_imm: dest:i src1:i len:6 clob:1
int_xor_imm: dest:i src1:i len:6 clob:1
int_shl_imm: dest:i src1:i len:6 clob:1
int_shr_imm: dest:i src1:i len:6 clob:1
int_shr_un_imm: dest:i src1:i len:6 clob:1

int_conv_to_r_un: dest:f src1:i len:32

int_ceq: dest:y len:6
int_cgt: dest:y len:6
int_cgt_un: dest:y len:6
int_clt: dest:y len:6
int_clt_un: dest:y len:6

int_cneq: dest:y len:6
int_cge: dest:y len:6
int_cle: dest:y len:6
int_cge_un: dest:y len:6
int_cle_un: dest:y len:6

cond_exc_ieq: len:6
cond_exc_ine_un: len:6
cond_exc_ilt: len:6
cond_exc_ilt_un: len:6
cond_exc_igt: len:6
cond_exc_igt_un: len:6
cond_exc_ige: len:6
cond_exc_ige_un: len:6
cond_exc_ile: len:6
cond_exc_ile_un: len:6
cond_exc_iov: len:6
cond_exc_ino: len:6
cond_exc_ic: len:6
cond_exc_inc: len:6

icompare: src1:i src2:i len:2
icompare_imm: src1:i len:6

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

long_conv_to_ovf_i4_2: dest:i src1:i src2:i len:30
long_conv_to_r8_2: dest:f src1:i src2:i len:14
long_conv_to_r4_2: dest:f src1:i src2:i len:14
long_conv_to_r_un_2: dest:f src1:i src2:i len:40

fmove: dest:f src1:f
move_f_to_i4: dest:i src1:f len:17
move_i4_to_f: dest:f src1:i len:17
float_conv_to_r4: dest:f src1:f  len:12

load_mem: dest:i len:9
loadi4_mem: dest:i len:9
loadu1_mem: dest:i len:9
loadu2_mem: dest:i len:9

vcall2: len:17 clob:c
vcall2_reg: src1:i len:11 clob:c
vcall2_membase: src1:b len:16 clob:c

localloc_imm: dest:i len:120

x86_add_membase_reg: src1:b src2:i len:11
x86_sub_membase_reg: src1:b src2:i len:11
x86_and_membase_reg: src1:b src2:i len:11
x86_or_membase_reg: src1:b src2:i len:11
x86_xor_membase_reg: src1:b src2:i len:11
x86_mul_membase_reg: src1:b src2:i len:13

x86_and_reg_membase: dest:i src1:i src2:b clob:1 len:6
x86_or_reg_membase: dest:i src1:i src2:b clob:1 len:6
x86_xor_reg_membase: dest:i src1:i src2:b clob:1 len:6

x86_fxch: len:2

addps: dest:x src1:x src2:x len:3 clob:1
divps: dest:x src1:x src2:x len:3 clob:1
mulps: dest:x src1:x src2:x len:3 clob:1
subps: dest:x src1:x src2:x len:3 clob:1
maxps: dest:x src1:x src2:x len:3 clob:1
minps: dest:x src1:x src2:x len:3 clob:1
compps: dest:x src1:x src2:x len:4 clob:1
andps: dest:x src1:x src2:x len:3 clob:1
andnps: dest:x src1:x src2:x len:3 clob:1
orps: dest:x src1:x src2:x len:3 clob:1
xorps: dest:x src1:x src2:x len:3 clob:1

haddps: dest:x src1:x src2:x len:4 clob:1
hsubps: dest:x src1:x src2:x len:4 clob:1
addsubps: dest:x src1:x src2:x len:4 clob:1
dupps_low: dest:x src1:x len:4
dupps_high: dest:x src1:x len:4

addpd: dest:x src1:x src2:x len:4 clob:1
divpd: dest:x src1:x src2:x len:4 clob:1
mulpd: dest:x src1:x src2:x len:4 clob:1
subpd: dest:x src1:x src2:x len:4 clob:1
maxpd: dest:x src1:x src2:x len:4 clob:1
minpd: dest:x src1:x src2:x len:4 clob:1
comppd: dest:x src1:x src2:x len:5 clob:1
andpd: dest:x src1:x src2:x len:4 clob:1
andnpd: dest:x src1:x src2:x len:4 clob:1
orpd: dest:x src1:x src2:x len:4 clob:1
xorpd: dest:x src1:x src2:x len:4 clob:1
sqrtpd: dest:x src1:x len:4 clob:1

haddpd: dest:x src1:x src2:x len:5 clob:1
hsubpd: dest:x src1:x src2:x len:5 clob:1
addsubpd: dest:x src1:x src2:x len:5 clob:1
duppd: dest:x src1:x len:5

pand: dest:x src1:x src2:x len:4 clob:1
por: dest:x src1:x src2:x len:4 clob:1
pxor: dest:x src1:x src2:x len:4 clob:1

sqrtps: dest:x src1:x len:4
rsqrtps: dest:x src1:x len:4
rcpps: dest:x src1:x len:4

pshuflew_high: dest:x src1:x len:5
pshuflew_low: dest:x src1:x len:5
pshufled: dest:x src1:x len:5
shufps: dest:x src1:x src2:x len:4 clob:1
shufpd: dest:x src1:x src2:x len:5 clob:1

extract_mask: dest:i src1:x len:4

paddb: dest:x src1:x src2:x len:4 clob:1
paddw: dest:x src1:x src2:x len:4 clob:1
paddd: dest:x src1:x src2:x len:4 clob:1
paddq: dest:x src1:x src2:x len:4 clob:1

psubb: dest:x src1:x src2:x len:4 clob:1
psubw: dest:x src1:x src2:x len:4 clob:1
psubd: dest:x src1:x src2:x len:4 clob:1
psubq: dest:x src1:x src2:x len:4 clob:1

pmaxb_un: dest:x src1:x src2:x len:4 clob:1
pmaxw_un: dest:x src1:x src2:x len:5 clob:1
pmaxd_un: dest:x src1:x src2:x len:5 clob:1

pmaxb: dest:x src1:x src2:x len:5 clob:1
pmaxw: dest:x src1:x src2:x len:4 clob:1
pmaxd: dest:x src1:x src2:x len:5 clob:1

pavgb_un: dest:x src1:x src2:x len:4 clob:1
pavgw_un: dest:x src1:x src2:x len:4 clob:1

pminb_un: dest:x src1:x src2:x len:4 clob:1
pminw_un: dest:x src1:x src2:x len:5 clob:1
pmind_un: dest:x src1:x src2:x len:5 clob:1

pminb: dest:x src1:x src2:x len:5 clob:1
pminw: dest:x src1:x src2:x len:4 clob:1
pmind: dest:x src1:x src2:x len:5 clob:1

pcmpeqb: dest:x src1:x src2:x len:4 clob:1
pcmpeqw: dest:x src1:x src2:x len:4 clob:1
pcmpeqd: dest:x src1:x src2:x len:4 clob:1
pcmpeqq: dest:x src1:x src2:x len:5 clob:1

pcmpgtb: dest:x src1:x src2:x len:4 clob:1
pcmpgtw: dest:x src1:x src2:x len:4 clob:1
pcmpgtd: dest:x src1:x src2:x len:4 clob:1
pcmpgtq: dest:x src1:x src2:x len:5 clob:1

psum_abs_diff: dest:x src1:x src2:x len:4 clob:1

unpack_lowb: dest:x src1:x src2:x len:4 clob:1
unpack_loww: dest:x src1:x src2:x len:4 clob:1
unpack_lowd: dest:x src1:x src2:x len:4 clob:1
unpack_lowq: dest:x src1:x src2:x len:4 clob:1
unpack_lowps: dest:x src1:x src2:x len:3 clob:1
unpack_lowpd: dest:x src1:x src2:x len:4 clob:1

unpack_highb: dest:x src1:x src2:x len:4 clob:1
unpack_highw: dest:x src1:x src2:x len:4 clob:1
unpack_highd: dest:x src1:x src2:x len:4 clob:1
unpack_highq: dest:x src1:x src2:x len:4 clob:1
unpack_highps: dest:x src1:x src2:x len:3 clob:1
unpack_highpd: dest:x src1:x src2:x len:4 clob:1

packw: dest:x src1:x src2:x len:4 clob:1
packd: dest:x src1:x src2:x len:4 clob:1

packw_un: dest:x src1:x src2:x len:4 clob:1
packd_un: dest:x src1:x src2:x len:5 clob:1

paddb_sat: dest:x src1:x src2:x len:4 clob:1
paddb_sat_un: dest:x src1:x src2:x len:4 clob:1

paddw_sat: dest:x src1:x src2:x len:4 clob:1
paddw_sat_un: dest:x src1:x src2:x len:4 clob:1

psubb_sat: dest:x src1:x src2:x len:4 clob:1
psubb_sat_un: dest:x src1:x src2:x len:4 clob:1

psubw_sat: dest:x src1:x src2:x len:4 clob:1
psubw_sat_un: dest:x src1:x src2:x len:4 clob:1

pmulw: dest:x src1:x src2:x len:4 clob:1
pmuld: dest:x src1:x src2:x len:5 clob:1
pmulq: dest:x src1:x src2:x len:4 clob:1

pmulw_high_un: dest:x src1:x src2:x len:4 clob:1
pmulw_high: dest:x src1:x src2:x len:4 clob:1

pshrw: dest:x src1:x len:5 clob:1
pshrw_reg: dest:x src1:x src2:x len:4 clob:1

psarw: dest:x src1:x len:5 clob:1
psarw_reg: dest:x src1:x src2:x len:4 clob:1

pshlw: dest:x src1:x len:5 clob:1
pshlw_reg: dest:x src1:x src2:x len:4 clob:1

pshrd: dest:x src1:x len:5 clob:1
pshrd_reg: dest:x src1:x src2:x len:4 clob:1

psard: dest:x src1:x len:5 clob:1
psard_reg: dest:x src1:x src2:x len:4 clob:1

pshld: dest:x src1:x len:5 clob:1
pshld_reg: dest:x src1:x src2:x len:4 clob:1

pshrq: dest:x src1:x len:5 clob:1
pshrq_reg: dest:x src1:x src2:x len:4 clob:1

pshlq: dest:x src1:x len:5 clob:1
pshlq_reg: dest:x src1:x src2:x len:4 clob:1

cvtdq2pd: dest:x src1:x len:4 clob:1
cvtdq2ps: dest:x src1:x len:3 clob:1
cvtpd2dq: dest:x src1:x len:4 clob:1
cvtpd2ps: dest:x src1:x len:4 clob:1
cvtps2dq: dest:x src1:x len:4 clob:1
cvtps2pd: dest:x src1:x len:3 clob:1
cvttpd2dq: dest:x src1:x len:4 clob:1
cvttps2dq: dest:x src1:x len:4 clob:1

xmove: dest:x src1:x len:4
xzero: dest:x len:4
xones: dest:x len:4

iconv_to_x: dest:x src1:i len:4
extract_i4: dest:i src1:x len:4

extract_i2: dest:i src1:x len:10
extract_i1: dest:i src1:x len:10
extract_r8: dest:f src1:x len:8

insert_i2: dest:x src1:x src2:i len:5 clob:1

extractx_u2: dest:i src1:x len:5
insertx_u1_slow: dest:x src1:i src2:i len:16 clob:x

insertx_i4_slow: dest:x src1:x src2:i len:13 clob:x
insertx_r4_slow: dest:x src1:x src2:f len:24 clob:1
insertx_r8_slow: dest:x src1:x src2:f len:24 clob:1

loadx_membase: dest:x src1:b len:7
storex_membase: dest:b src1:x len:7
storex_membase_reg: dest:b src1:x len:7

loadx_aligned_membase: dest:x src1:b len:7
storex_aligned_membase_reg: dest:b src1:x len:7
storex_nta_membase_reg: dest:b src1:x len:7

fconv_to_r8_x: dest:x src1:f len:14
xconv_r8_to_i4: dest:y src1:x len:7

prefetch_membase: src1:b len:4

expand_i2: dest:x src1:i len:15
expand_i4: dest:x src1:i len:9
expand_r4: dest:x src1:f len:20
expand_r8: dest:x src1:f len:20

liverange_start: len:0
liverange_end: len:0
gc_liveness_def: len:0
gc_liveness_use: len:0
gc_spill_slot_liveness_def: len:0
gc_param_slot_liveness_def: len:0
get_sp: dest:i len:6
set_sp: src1:i len:6

fill_prof_call_ctx: src1:i len:128

get_last_error: dest:i len:32
