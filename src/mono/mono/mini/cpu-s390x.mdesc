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
add_imm: dest:i src1:i len:24
add_ovf_carry: dest:i src1:1 src2:i len:28
add_ovf_un_carry: dest:i src1:1 src2:i len:12
addcc: dest:i src1:i src2:i len:12
and_imm: dest:i src1:i len:24
aotconst: dest:i len:8
atomic_add_i4: src1:b src2:i dest:i len:28
atomic_add_i8: src1:b src2:i dest:i len:30
atomic_exchange_i4: src1:b src2:i dest:i len:18
atomic_exchange_i8: src1:b src2:i dest:i len:24
br: len:6
br_reg: src1:i len:8
break: len:22
call: dest:o clob:c len:26
call_handler: len:12 clob:c
call_membase: dest:o src1:b len:12 clob:c
call_reg: dest:o src1:i len:8 clob:c
ceq: dest:i len:12
cgt_un: dest:i len:12
cgt: dest:i len:12
check_this: src1:b len:16
ckfinite: dest:f src1:f len:22
clt_un: dest:i len:12
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
div_imm: dest:i src1:i len:24
div_un_imm: dest:i src1:i len:24
endfinally: len:8
fcall: dest:g len:26 clob:c
fcall_membase: dest:g src1:b len:14 clob:c
fcall_reg: dest:g src1:i len:10 clob:c
fcompare: src1:f src2:f len:14
rcompare: src1:f src2:f len:14
float_add: dest:f src1:f src2:f len:8

float_beq: len:10
float_bge: len:10
float_bge_un: len:8
float_bgt: len:10
float_ble: len:10
float_ble_un: len:8
float_blt: len:10
float_blt_un: len:8
float_bne_un: len:8
float_bgt_un: len:8

float_ceq: dest:i src1:f src2:f len:16
float_cgt: dest:i src1:f src2:f len:16
float_cgt_un: dest:i src1:f src2:f len:16
float_clt: dest:i src1:f src2:f len:16
float_clt_un: dest:i src1:f src2:f len:16
float_cneq: dest:y src1:f src2:f len:16
float_cge: dest:y src1:f src2:f len:16
float_cle: dest:y src1:f src2:f len:16

float_conv_to_i1: dest:i src1:f len:50
float_conv_to_i2: dest:i src1:f len:50
float_conv_to_i4: dest:i src1:f len:50
float_conv_to_i8: dest:l src1:f len:50
float_conv_to_r4: dest:f src1:f len:8
float_conv_to_u1: dest:i src1:f len:72
float_conv_to_u2: dest:i src1:f len:72
float_conv_to_u4: dest:i src1:f len:72
float_conv_to_u8: dest:i src1:f len:72
float_div: dest:f src1:f src2:f len:24
float_div_un: dest:f src1:f src2:f len:30
float_mul: dest:f src1:f src2:f len:8
float_neg: dest:f src1:f len:8
float_not: dest:f src1:f len:8
float_rem: dest:f src1:f src2:f len:24
float_rem_un: dest:f src1:f src2:f len:30
float_sub: dest:f src1:f src2:f len:24

# R4 opcodes
r4_conv_to_i1: dest:i src1:f len:32
r4_conv_to_u1: dest:i src1:f len:32
r4_conv_to_i2: dest:i src1:f len:32
r4_conv_to_u2: dest:i src1:f len:32
r4_conv_to_i4: dest:i src1:f len:16
r4_conv_to_u4: dest:i src1:f len:32
r4_conv_to_i8: dest:i src1:f len:32
r4_conv_to_r8: dest:f src1:f len:17
r4_conv_to_u8: dest:i src1:f len:17
r4_conv_to_r4: dest:f src1:f len:17
r4_add: dest:f src1:f src2:f clob:1 len:8
r4_sub: dest:f src1:f src2:f clob:1 len:20
r4_mul: dest:f src1:f src2:f clob:1 len:8
r4_div: dest:f src1:f src2:f clob:1 len:20
r4_rem: dest:f src1:f src2:f clob:1 len:24
r4_neg: dest:f src1:f clob:1 len:23
r4_ceq: dest:i src1:f src2:f len:35
r4_cgt: dest:i src1:f src2:f len:35
r4_cgt_un: dest:i src1:f src2:f len:48
r4_clt: dest:i src1:f src2:f len:35
r4_clt_un: dest:i src1:f src2:f len:42
r4_cneq: dest:i src1:f src2:f len:42
r4_cge: dest:i src1:f src2:f len:35
r4_cle: dest:i src1:f src2:f len:35
rmove: dest:f src1:f len:4

fmove: dest:f src1:f len:4
move_f_to_i4: dest:i src1:f len:14
move_i4_to_f: dest:f src1:i len:14
move_f_to_i8: dest:i src1:f len:4
move_i8_to_f: dest:f src1:i len:8
i8const: dest:i len:20
icompare: src1:i src2:i len:4
icompare_imm: src1:i len:18
iconst: dest:i len:40
label: len:0
lcall: dest:o len:22 clob:c
lcall_membase: dest:o src1:b len:12 clob:c
lcall_reg: dest:o src1:i len:8 clob:c
lcompare: src1:i src2:i len:4
load_membase: dest:i src1:b len:30
loadi1_membase: dest:i src1:b len:40
loadi2_membase: dest:i src1:b len:30
loadi4_membase: dest:i src1:b len:30
loadi8_membase: dest:i src1:b len:30
loadr4_membase: dest:f src1:b len:28
loadr8_membase: dest:f src1:b len:28
loadu1_membase: dest:i src1:b len:30
loadu2_membase: dest:i src1:b len:30
loadu4_mem: dest:i len:8
loadu4_membase: dest:i src1:b len:30
localloc: dest:i src1:i len:180
memory_barrier: len:10
move: dest:i src1:i len:4
mul_imm: dest:i src1:i len:24
nop: len:4
popcnt32: dest:i src1:i len:38
popcnt64: dest:i src1:i len:34
relaxed_nop: len:4
arglist: src1:i len:28
bigmul: len:2 dest:i src1:a src2:i
bigmul_un: len:2 dest:i src1:a src2:i
endfilter: src1:i len:28
rethrow: src1:i len:26
or_imm: dest:i src1:i len:24
r4const: dest:f len:26
r8const: dest:f len:24
rem_imm: dest:i src1:i len:24
rcall: dest:f len:26 clob:c
rcall_reg: dest:f src1:i len:8 clob:c
rcall_membase: dest:f src1:b len:12 clob:c
rem_un_imm: dest:i src1:i len:24
s390_bkchain: len:8 dest:i src1:i
s390_move: len:48 src2:b src1:b
s390_setf4ret: dest:f src1:f len:4
sbb: dest:i src1:i src2:i len:6
sbb_imm: dest:i src1:i len:14
seq_point: len:64
il_seq_point: len:0
sext_i4: dest:i src1:i len:4
zext_i4: dest:i src1:i len:4
shl_imm: dest:i src1:i len:10
shr_imm: dest:i src1:i len:10
shr_un_imm: dest:i src1:i len:10
abs: dest:f src1:f len:4
absf: dest:f src1:f len:4
ceil: dest:f src1:f len:4
ceilf: dest:f src1:f len:4
floor: dest:f src1:f len:4
floorf: dest:f src1:f len:4
round: dest:f src1:f len:4
sqrt: dest:f src1:f len:4
sqrtf: dest:f src1:f len:4
trunc: dest:f src1:f len:4
truncf: dest:f src1:f len:4
fcopysign: dest:f src1:f src2:f len:4
start_handler: len:26
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
sub_imm: dest:i src1:i len:18
sub_ovf_carry: dest:i src1:1 src2:i len:28
sub_ovf_un_carry: dest:i src1:1 src2:i len:12
subcc: dest:i src1:i src2:i len:12
tailcall: len:32 clob:c
tailcall_reg: src1:b len:32 clob:c
tailcall_membase: src1:b len:32 clob:c

# Tailcall parameters are moved with one instruction per 256 bytes,
# of stacked parameters. Zero and six are the most common
# totals. Division is not possible. Allocate an instruction per parameter.
tailcall_parameter: len:6

throw: src1:i len:26
tls_get: dest:1 len:32
tls_set: src1:1 len:32
vcall: len:22 clob:c
vcall_membase: src1:b len:12 clob:c
vcall_reg: src1:i len:8 clob:c
voidcall: len:22 clob:c
voidcall_membase: src1:b len:12 clob:c
voidcall_reg: src1:i len:8 clob:c
xor_imm: dest:i src1:i len:20

# 32 bit opcodes
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

int_cneq: dest:i len:12
int_cge: dest:i len:12
int_cle: dest:i len:12
int_cge_un: dest:i len:12
int_cle_un: dest:i len:12

int_div: dest:a src1:i src2:i len:16
int_div_imm: dest:a src1:i len:24
int_div_un: dest:a src1:i src2:i len:16
int_div_un_imm: dest:a src1:i len:24
int_mul: dest:i src1:i src2:i len:16
int_mul_imm: dest:i src1:i len:24
int_mul_ovf: dest:i src1:i src2:i len:44
int_mul_ovf_un: dest:i src1:i src2:i len:22
int_add_ovf: dest:i src1:i src2:i len:32
int_add_ovf_un: dest:i src1:i src2:i len:32
int_sub_ovf: dest:i src1:i src2:i len:32
int_sub_ovf_un: dest:i src1:i src2:i len:32
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
int_shl: dest:i src1:i src2:i clob:s len:12
int_shl_imm: dest:i src1:i len:10
int_shr: dest:i src1:i src2:i clob:s len:12
int_shr_imm: dest:i src1:i len:10
int_shr_un: dest:i src1:i src2:i clob:s len:12
int_shr_un_imm: dest:i src1:i len:10
int_subcc: dest:i src1:i src2:i len:12
int_sub: dest:i src1:i src2:i len:12
int_sub_imm: dest:i src1:i len:20
int_xor: dest:i src1:i src2:i len:12
int_xor_imm: dest:i src1:i len:24
int_conv_to_r4: dest:f src1:i len:16
int_conv_to_r8: dest:f src1:i len:16

# 64 bit opcodes
long_add: dest:i src1:i src2:i len:12
long_sub: dest:i src1:i src2:i len:12
long_add_ovf: dest:i src1:i src2:i len:32
long_add_ovf_un: dest:i src1:i src2:i len:32
long_div: dest:i src1:i src2:i len:12
long_div_un: dest:i src1:i src2:i len:16
long_mul: dest:i src1:i src2:i len:12
long_mul_imm: dest:i src1:i len:20
long_mul_ovf: dest:i src1:i src2:i len:56
long_mul_ovf_un: dest:i src1:i src2:i len:64
long_and: dest:i src1:i src2:i len:8
long_or: dest:i src1:i src2:i len:8
long_xor: dest:i src1:i src2:i len:8
long_neg: dest:i src1:i len:6
long_not: dest:i src1:i len:12
long_rem: dest:i src1:i src2:i len:12
long_rem_imm: dest:i src1:i len:12
long_rem_un: dest:i src1:i src2:i len:16
long_shl: dest:i src1:i src2:i len:14
long_shl_imm: dest:i src1:i len:14
long_shr_un: dest:i src1:i src2:i len:14
long_shr: dest:i src1:i src2:i len:14
long_shr_imm: dest:i src1:i len:14
long_shr_un_imm: dest:i src1:i len:14
long_sub_imm: dest:i src1:i len:16
long_sub_ovf: dest:i src1:i src2:i len:16
long_sub_ovf_un: dest:i src1:i src2:i len:28

long_conv_to_i1: dest:i src1:i len:12
long_conv_to_i2: dest:i src1:i len:12
long_conv_to_i4: dest:i src1:i len:4
long_conv_to_i8: dest:i src1:i len:4
long_conv_to_i: dest:i src1:i len:4
long_conv_to_ovf_i: dest:i src1:i len:44
long_conv_to_ovf_i4_un: dest:i src1:i len:50
long_conv_to_ovf_u4: dest:i src1:i len:48
long_conv_to_ovf_u8_un: dest:i src1:i len:4
long_conv_to_r4: dest:f src1:i len:16
long_conv_to_r8: dest:f src1:i len:16
long_conv_to_u1: dest:i src1:i len:16
long_conv_to_u2: dest:i src1:i len:24
long_conv_to_u4: dest:i src1:i len:4
long_conv_to_u8: dest:i src1:i len:4
long_conv_to_u:  dest:i src1:i len:4
long_conv_to_r_un: dest:f src1:i len:37

long_beq: len:8
long_bge_un: len:8
long_bge: len:8
long_bgt_un: len:8
long_bgt: len:8
long_ble_un: len:8
long_ble: len:8
long_blt_un: len:8
long_blt: len:8
long_bne_un: len:8

# Linear IR opcodes
dummy_use: src1:i len:0
dummy_iconst: dest:i len:0
dummy_i8const: dest:i len:0
dummy_r8const: dest:f len:0
dummy_r4const: dest:f len:0
not_reached: len:0
not_null: src1:i len:0

jump_table: dest:i len:24

int_conv_to_i1: dest:i src1:i len:12
int_conv_to_i2: dest:i src1:i len:12
int_conv_to_i4: dest:i src1:i len:4
int_conv_to_i: dest:i src1:i len:4
int_conv_to_u1: dest:i src1:i len:10
int_conv_to_u2: dest:i src1:i len:16
int_conv_to_u4: dest:i src1:i len:4
int_conv_to_r_un: dest:f src1:i len:37

cond_exc_ic: len:8
cond_exc_ieq: len:8
cond_exc_ige: len:8
cond_exc_ige_un: len:8
cond_exc_igt: len:8
cond_exc_igt_un: len:8
cond_exc_ile: len:8
cond_exc_ile_un: len:8
cond_exc_ilt: len:8
cond_exc_ilt_un: len:8
cond_exc_inc: len:8
cond_exc_ine_un: len:8
cond_exc_ino: len:8
cond_exc_iov: len:8

lcompare_imm: src1:i len:20

long_add_imm: dest:i src1:i len:20

long_ceq: dest:i len:12
long_cgt_un: dest:i len:12
long_cgt: dest:i len:12
long_clt_un: dest:i len:12
long_clt: dest:i len:12

vcall2: len:22 clob:c
vcall2_membase: src1:b len:12 clob:c
vcall2_reg: src1:i len:8 clob:c

s390_int_add_ovf: len:32 dest:i src1:i src2:i
s390_int_add_ovf_un: len:32 dest:i src1:i src2:i
s390_int_sub_ovf: len:32 dest:i src1:i src2:i
s390_int_sub_ovf_un: len:32 dest:i src1:i src2:i

s390_long_add_ovf: dest:i src1:i src2:i len:32
s390_long_add_ovf_un: dest:i src1:i src2:i len:32
s390_long_sub_ovf: dest:i src1:i src2:i len:32
s390_long_sub_ovf_un: dest:i src1:i src2:i len:32

liverange_start: len:0
liverange_end: len:0
gc_liveness_def: len:0
gc_liveness_use: len:0
gc_spill_slot_liveness_def: len:0
gc_param_slot_liveness_def: len:0
gc_safe_point: clob:c src1:i len:32

generic_class_init: src1:A len:32 clob:c

s390_crj: src1:i src2:i len:24
s390_crj_un: src1:i src2:i len:24
s390_cgrj: src1:i src2:i len:24
s390_cgrj_un: src1:i src2:i len:24
s390_cij: len:24
s390_cij_un: src1:i len:24
s390_cgij: len:24
s390_cgij_un: len:24
