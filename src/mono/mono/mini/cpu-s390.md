# S/390 64-bit cpu description file
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

nop: len:4
relaxed_nop: len:4

adc: dest:i src1:i src2:i len:6
add_ovf_carry: dest:i src1:1 src2:i len:28
add_ovf_un_carry: dest:i src1:1 src2:i len:28
addcc: dest:i src1:i src2:i len:6
aot_const: dest:i len:8
atomic_add_i4: src1:b src2:i dest:i len:20
atomic_exchange_i4: src1:b src2:i dest:i len:20
atomic_add_new_i4: src1:b src2:i dest:i len:24
br: len:6
br_reg: src1:i len:8
break: len:6
call: dest:o len:6 clob:c
call_handler: len:12 clob:c
call_membase: dest:o src1:b len:12 clob:c
call_reg: dest:o src1:i len:8 clob:c
ceq: dest:i len:12
cgt.un: dest:i len:12
cgt: dest:i len:12
checkthis: src1:b len:4
ckfinite: dest:f src1:f len:22
clt.un: dest:i len:12
clt: dest:i len:12
compare: src1:i src2:i len:4
compare_imm: src1:i len:14
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
endfinally: len: 20
fcall: dest:g len:10 clob:c
fcall_membase: dest:g src1:b len:14 clob:c
fcall_reg: dest:g src1:i len:10 clob:c
fcompare: src1:f src2:f len:14
float_add: dest:f src1:f src2:f len:6
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
float_conv_to_i1: dest:i src1:f len:50
float_conv_to_i2: dest:i src1:f len:50
float_conv_to_i4: dest:i src1:f len:50
float_conv_to_i8: dest:l src1:f len:50
float_conv_to_i: dest:i src1:f len:52
float_conv_to_r4: dest:f src1:f len:4
float_conv_to_u1: dest:i src1:f len:62
float_conv_to_u2: dest:i src1:f len:62
float_conv_to_u4: dest:i src1:f len:62
float_conv_to_u8: dest:l src1:f len:62
float_conv_to_u: dest:i src1:f len:36
float_div: dest:f src1:f src2:f len:6
float_div_un: dest:f src1:f src2:f len:6
float_mul: dest:f src1:f src2:f len:6
float_neg: dest:f src1:f len:6
float_not: dest:f src1:f len:6
float_rem: dest:f src1:f src2:f len:16
float_rem_un: dest:f src1:f src2:f len:16
float_sub: dest:f src1:f src2:f len:6
fmove: dest:f src1:f len:4
iconst: dest:i len:16
jmp: len:56
label: len:0
lcall: dest:L len:8 clob:c
lcall_membase: dest:L src1:b len:12 clob:c
lcall_reg: dest:L src1:i len:8 clob:c
load_membase: dest:i src1:b len:18
loadi1_membase: dest:i src1:b len:40
loadi2_membase: dest:i src1:b len:24
loadi4_membase: dest:i src1:b len:18
loadi8_membase: dest:i src1:b
loadr4_membase: dest:f src1:b len:20
loadr8_membase: dest:f src1:b len:18
loadu1_membase: dest:i src1:b len:26
loadu2_membase: dest:i src1:b len:26
loadu4_mem: dest:i len:8
loadu4_membase: dest:i src1:b len:18
localloc: dest:i src1:i len:72
long_add: len: 18 dest:l src1:l src2:i clob:1
long_add_ovf_un: len:22 dest:l src1:l src2:i clob:1
long_add_ovf: len:28 dest:l src1:l src2:i clob:1
long_conv_to_ovf_i: dest:i src1:i src2:i len:44
long_conv_to_r_un: dest:f src1:i src2:i len:37 
long_conv_to_r4: dest:f src1:i len:4
long_conv_to_r8: dest:f src1:i len:4
long_mul_ovf: len: 18
long_mul_ovf_un: len: 18 
long_sub: len: 18 dest:l src1:l src2:i clob:1
long_sub_ovf_un: len:22 dest:l src1:l src2:i clob:1
long_sub_ovf: len:36 dest:l src1:l src2:i clob:1
memory_barrier: len: 10
move: dest:i src1:i len:4
bigmul: len:2 dest:l src1:a src2:i
bigmul_un: len:2 dest:l src1:a src2:i
endfilter: src1:i len:12
rethrow: src1:i len:8
oparglist: src1:i len:20
r4const: dest:f len:22
r8const: dest:f len:18
s390_bkchain: len:16 dest:i src1:i
s390_move: len:48 dest:b src1:b
s390_setf4ret: dest:f src1:f len:4
tls_get: dest:i len:44
sbb: dest:i src1:i src2:i len:8
setlret: src1:i src2:i len:12
sqrt: dest:f src1:f len:4
start_handler: len:18
store_membase_imm: dest:b len:32
store_membase_reg: dest:b src1:i len:18
storei1_membase_imm: dest:b len:32
storei1_membase_reg: dest:b src1:i len:18
storei2_membase_imm: dest:b len:32
storei2_membase_reg: dest:b src1:i len:18
storei4_membase_imm: dest:b len:32
storei4_membase_reg: dest:b src1:i len:18
storei8_membase_imm: dest:b 
storei8_membase_reg: dest:b src1:i 
storer4_membase_reg: dest:b src1:f len:22
storer8_membase_reg: dest:b src1:f len:22
sub_ovf_carry: dest:i src1:1 src2:i len:28
sub_ovf_un_carry: dest:i src1:1 src2:i len:28
subcc: dest:i src1:i src2:i len:6
throw: src1:i len:8
vcall: len:8 clob:c
vcall_membase: src1:b len:12 clob:c
vcall_reg: src1:i len:8 clob:c
voidcall: len:8 clob:c
voidcall_membase: src1:b len:12 clob:c
voidcall_reg: src1:i len:8 clob:c

# 32 bit opcodes
int_add: dest:i src1:i src2:i len:6
int_sub: dest:i src1:i src2:i len:6
int_mul: dest:i src1:i src2:i len:6
int_div: dest:a src1:i src2:i len:10
int_div_un: dest:a src1:i src2:i len:12 
int_and: dest:i src1:i src2:i len:6
int_or: dest:i src1:i src2:i len:4
int_xor: dest:i src1:i src2:i len:4
int_rem: dest:d src1:i src2:i len:10
int_rem_un: dest:d src1:i src2:i len:12
int_shl: dest:i src1:i src2:i clob:s len:8
int_shr: dest:i src1:i src2:i clob:s len:8
int_shr_un: dest:i src1:i src2:i clob:s len:8
int_add_ovf: len: 24 dest:i src1:i src2:i
int_add_ovf_un: len: 10 dest:i src1:i src2:i
int_sub_ovf: len:24 dest:i src1:i src2:i
int_sub_ovf_un: len:10 dest:i src1:i src2:i 
int_mul_ovf: dest:i src1:i src2:i len:42
int_mul_ovf_un: dest:i src1:i src2:i len:20

int_neg: dest:i src1:i len:4
int_not: dest:i src1:i len:8
int_conv_to_i1: dest:i src1:i len:16
int_conv_to_i2: dest:i src1:i len:16
int_conv_to_i4: dest:i src1:i len:2
int_conv_to_r4: dest:f src1:i len:4
int_conv_to_r8: dest:f src1:i len:4
int_conv_to_u1: dest:i src1:i len:8
int_conv_to_u2: dest:i src1:i len:16
int_conv_to_u4: dest:i src1:i

int_conv_to_r_un: dest:f src1:i len:30

int_beq: len:8
int_bge_un: len:8
int_bge: len:8
int_bgt_un: len:8
int_bgt: len:8
int_ble_un: len:8
int_ble: len:8
int_blt_un: len:8
int_blt: len:8
int_bne_un: len:8

mul_imm: dest:i src1:i len:20
adc_imm: dest:i src1:i len:18
add_imm: dest:i src1:i len:18
addcc_imm: dest:i src1:i len:18
and_imm: dest:i src1:i len:16
div_imm: dest:i src1:i len:24
div_un_imm: dest:i src1:i len:24
or_imm: dest:i src1:i len:16
rem_imm: dest:i src1:i len:24
rem_un_imm: dest:i src1:i len:24
sbb_imm: dest:i src1:i len:18
shl_imm: dest:i src1:i len:8
shr_imm: dest:i src1:i len:8
shr_un_imm: dest:i src1:i len:8
sub_imm: dest:i src1:i len:18
subcc_imm: dest:i src1:i len:18
xor_imm: dest:i src1:i len:16

# Linear IR opcodes
dummy_use: src1:i len:0
dummy_store: len:0
not_reached: len:0
not_null: src1:i len:0

jump_table: dest:i len:16

icompare: src1:i src2:i len:4
icompare_imm: src1:i len:14

int_ceq: dest:i len:12
int_cgt_un: dest:i len:12
int_cgt: dest:i len:12
int_clt_un: dest:i len:12
int_clt: dest:i len:12

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

int_add_imm: dest:i src1:i len:18
int_sub_imm: dest:i src1:i len:18
int_mul_imm: dest:i src1:i len:20
int_div_imm: dest:i src1:i len:24
int_div_un_imm: dest:i src1:i len:24
int_rem_imm: dest:i src1:i len:24
int_rem_un_imm: dest:i src1:i len:24
int_and_imm: dest:i src1:i len:16
int_or_imm: dest:i src1:i len:16
int_xor_imm: dest:i src1:i len:16
int_adc_imm: dest:i src1:i len:18
int_sbb_imm: dest:i src1:i len:18
int_shl_imm: dest:i src1:i len:8
int_shr_imm: dest:i src1:i len:8
int_shr_un_imm: dest:i src1:i len:8

int_adc: dest:i src1:i src2:i len:6
int_sbb: dest:i src1:i src2:i len:8
int_addcc: dest:i src1:i src2:i len:6
int_subcc: dest:i src1:i src2:i len:6

long_conv_to_ovf_i4_2: dest:i src1:i src2:i len:44

vcall2: len:8 clob:c
vcall2_membase: src1:b len:12 clob:c
vcall2_reg: src1:i len:8 clob:c

s390_long_add: dest:l src1:i src2:i len:18
s390_long_add_ovf: dest:l src1:i src2:i len:32
s390_long_add_ovf_un: dest:l src1:i src2:i len:32
s390_long_sub: dest:l src1:i src2:i len:18
s390_long_sub_ovf: dest:l src1:i src2:i len:32
s390_long_sub_ovf_un: dest:l src1:i src2:i len:32
s390_long_neg: dest:l src1:i src2:i len:18

s390_int_add_ovf: len:24 dest:i src1:i src2:i
s390_int_add_ovf_un: len:10 dest:i src1:i src2:i 
s390_int_sub_ovf: len:24 dest:i src1:i src2:i
s390_int_sub_ovf_un: len:10 dest:i src1:i src2:i 
