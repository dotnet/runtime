# sparc32 cpu description file
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
#   L  register pair (same as 'i' on v9)
#   l  %o0:%o1 register pair (same as 'i' on v9)
#   o  %o0
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
# See the code in mini-sparc32.c for more details on how the specifiers are used.
#
label: len:0
break: len:64
br: len:8

throw: src1:i len:64
rethrow: src1:i len:64
start_handler: len:64
endfinally: len:64
endfilter: src1:i len:64

ckfinite: dest:f src1:f len:40
ceq: dest:i len:64
cgt: dest:i len:64
cgt_un: dest:i len:64
clt: dest:i len:64
clt_un: dest:i len:64
localloc: dest:i src1:i len:64
localloc_imm: dest:i len:64
compare: src1:i src2:i len:4
icompare: src1:i src2:i len:4
compare_imm: src1:i len:64
icompare_imm: src1:i len:64
fcompare: src1:f src2:f len:64
lcompare: src1:i src2:i len:4
setfret: dest:f src1:f len:8
check_this: src1:b len:4
arglist: src1:i len:64
call: dest:o clob:c len:40
call_reg: dest:o src1:i len:64 clob:c
call_membase: dest:o src1:b len:64 clob:c
voidcall: len:64 clob:c
voidcall_reg: src1:i len:64 clob:c
voidcall_membase: src1:b len:64 clob:c
fcall: dest:f len:64 clob:c
fcall_reg: dest:f src1:i len:64 clob:c
fcall_membase: dest:f src1:b len:64 clob:c
lcall: dest:l len:42 clob:c
lcall_reg: dest:l src1:i len:64 clob:c
lcall_membase: dest:l src1:b len:64 clob:c
vcall: len:40 clob:c
vcall_reg: src1:i len:64 clob:c
vcall_membase: src1:b len:64 clob:c
iconst: dest:i len:64
i8const: dest:i len:64
r4const: dest:f len:64
r8const: dest:f len:64
store_membase_imm: dest:b len:64
store_membase_reg: dest:b src1:i len:64
storei1_membase_imm: dest:b len:64
storei1_membase_reg: dest:b src1:i len:64
storei2_membase_imm: dest:b len:64
storei2_membase_reg: dest:b src1:i len:64
storei4_membase_imm: dest:b len:64
storei4_membase_reg: dest:b src1:i len:64
storei8_membase_imm: dest:b len:64 len:64
storei8_membase_reg: dest:b src1:i len:64
storer4_membase_reg: dest:b src1:f len:64
storer8_membase_reg: dest:b src1:f len:64
load_membase: dest:i src1:b len:64
loadi1_membase: dest:i src1:b len:64
loadu1_membase: dest:i src1:b len:64
loadi2_membase: dest:i src1:b len:64
loadu2_membase: dest:i src1:b len:64
loadi4_membase: dest:i src1:b len:64
loadu4_membase: dest:i src1:b len:64
loadi8_membase: dest:i src1:b len:64
loadr4_membase: dest:f src1:b len:64
loadr8_membase: dest:f src1:b len:64
loadu4_mem: dest:i len:8
move: dest:i src1:i len:4
add_imm: dest:i src1:i len:64
addcc_imm: dest:i src1:i len:64
sub_imm: dest:i src1:i len:64
subcc_imm: dest:i src1:i len:64
mul_imm: dest:i src1:i len:64
div_imm: dest:a src1:i src2:i len:64
div_un_imm: dest:a src1:i src2:i len:64
rem_imm: dest:d src1:i src2:i len:64
rem_un_imm: dest:d src1:i src2:i len:64
and_imm: dest:i src1:i len:64
or_imm: dest:i src1:i len:64
xor_imm: dest:i src1:i len:64
shl_imm: dest:i src1:i len:64
shr_imm: dest:i src1:i len:64
shr_un_imm: dest:i src1:i len:64
cond_exc_eq: len:64
cond_exc_ne_un: len:64
cond_exc_lt: len:64
cond_exc_lt_un: len:64
cond_exc_gt: len:64
cond_exc_gt_un: len:64
cond_exc_ge: len:64
cond_exc_ge_un: len:64
cond_exc_le: len:64
cond_exc_le_un: len:64
cond_exc_ov: len:64
cond_exc_no: len:64
cond_exc_c: len:64
cond_exc_nc: len:64
float_beq: len:8
float_bne_un: len:64
float_blt: len:8
float_blt_un: len:64
float_bgt: len:8
float_bgt_un: len:64
float_bge: len:64
float_bge_un: len:64
float_ble: len:64
float_ble_un: len:64
float_add: dest:f src1:f src2:f len:4
float_sub: dest:f src1:f src2:f len:4
float_mul: dest:f src1:f src2:f len:4
float_div: dest:f src1:f src2:f len:4
float_div_un: dest:f src1:f src2:f len:4
float_rem: dest:f src1:f src2:f len:64
float_rem_un: dest:f src1:f src2:f len:64
float_neg: dest:f src1:f len:4
float_not: dest:f src1:f len:4
float_conv_to_i1: dest:i src1:f len:40
float_conv_to_i2: dest:i src1:f len:40
float_conv_to_i4: dest:i src1:f len:40
float_conv_to_i8: dest:L src1:f len:40
float_conv_to_r4: dest:f src1:f len:8
float_conv_to_u4: dest:i src1:f len:40
float_conv_to_u8: dest:L src1:f len:40
float_conv_to_u2: dest:i src1:f len:40
float_conv_to_u1: dest:i src1:f len:40
float_conv_to_i: dest:i src1:f len:40
float_ceq: dest:i src1:f src2:f len:64
float_cgt: dest:i src1:f src2:f len:64
float_cgt_un: dest:i src1:f src2:f len:64
float_clt: dest:i src1:f src2:f len:64
float_clt_un: dest:i src1:f src2:f len:64
float_conv_to_u: dest:i src1:f len:64
call_handler: len:64 clob:c
aotconst: dest:i len:64
adc: dest:i src1:i src2:i len:4
addcc: dest:i src1:i src2:i len:4
subcc: dest:i src1:i src2:i len:4
adc_imm: dest:i src1:i len:64
sbb: dest:i src1:i src2:i len:4
sbb_imm: dest:i src1:i len:64
br_reg: src1:i len:8
bigmul: len:2 dest:L src1:a src2:i
bigmul_un: len:2 dest:L src1:a src2:i
fmove: dest:f src1:f len:8

# 32 bit opcodes
int_add: dest:i src1:i src2:i len:64
int_sub: dest:i src1:i src2:i len:64
int_mul: dest:i src1:i src2:i len:64
int_div: dest:i src1:i src2:i len:64
int_div_un: dest:i src1:i src2:i len:64
int_rem: dest:i src1:i src2:i len:64
int_rem_un: dest:i src1:i src2:i len:64
int_and: dest:i src1:i src2:i len:64
int_or: dest:i src1:i src2:i len:64
int_xor: dest:i src1:i src2:i len:64
int_shl: dest:i src1:i src2:i len:64
int_shr: dest:i src1:i src2:i len:64
int_shr_un: dest:i src1:i src2:i len:64
int_adc: dest:i src1:i src2:i len:64
int_adc_imm: dest:i src1:i len:64
int_sbb: dest:i src1:i src2:i len:64
int_sbb_imm: dest:i src1:i len:64
int_addcc: dest:i src1:i src2:i len:64
int_subcc: dest:i src1:i src2:i len:64
int_add_imm: dest:i src1:i len:64
int_sub_imm: dest:i src1:i len:64
int_mul_imm: dest:i src1:i len:64
int_div_imm: dest:i src1:i len:64
int_div_un_imm: dest:i src1:i len:64
int_rem_imm: dest:i src1:i len:64
int_rem_un_imm: dest:i src1:i len:64
int_and_imm: dest:i src1:i len:64
int_or_imm: dest:i src1:i len:64
int_xor_imm: dest:i src1:i len:64
int_shl_imm: dest:i src1:i len:64
int_shr_imm: dest:i src1:i len:64
int_shr_un_imm: dest:i src1:i len:64
int_mul_ovf: dest:i src1:i src2:i len:64
int_mul_ovf_un: dest:i src1:i src2:i len:64
int_conv_to_i1: dest:i src1:i len:8
int_conv_to_i2: dest:i src1:i len:8
int_conv_to_i4: dest:i src1:i len:4
int_conv_to_i8: dest:i src1:i len:4
int_conv_to_r4: dest:f src1:i len:64
int_conv_to_r8: dest:f src1:i len:64
int_conv_to_u4: dest:i src1:i len:4
int_conv_to_u8: dest:i src1:i len:4
int_conv_to_u2: dest:i src1:i len:8
int_conv_to_u1: dest:i src1:i len:4
int_conv_to_i: dest:i src1:i len:4
int_neg: dest:i src1:i len:64
int_not: dest:i src1:i len:64
int_ceq: dest:i len:64
int_cgt: dest:i len:64
int_cgt_un: dest:i len:64
int_clt: dest:i len:64
int_clt_un: dest:i len:64
int_beq: len:8
int_bge: len:8
int_bgt: len:8
int_ble: len:8
int_blt: len:8
int_bne_un: len:64
int_bge_un: len:64
int_bgt_un: len:64
int_ble_un: len:64
int_blt_un: len:64

# 64 bit opcodes
long_shl: dest:i src1:i src2:i len:64
long_shr: dest:i src1:i src2:i len:64
long_shr_un: dest:i src1:i src2:i len:64
long_conv_to_ovf_i: dest:i src1:i src2:i len:48
long_mul_ovf:
long_conv_to_r_un: dest:f src1:i src2:i len:64
long_shr_imm: dest:i src1:i len:64
long_shr_un_imm: dest:i src1:i len:64
long_shl_imm: dest:i src1:i len:64

memory_barrier: len:4

sparc_brz: src1:i len: 8
sparc_brlez: src1:i len: 8
sparc_brlz: src1:i len: 8
sparc_brnz: src1:i len: 8
sparc_brgz: src1:i len: 8
sparc_brgez: src1:i len: 8
sparc_cond_exc_eqz: src1:i len:64
sparc_cond_exc_nez: src1:i len:64
sparc_cond_exc_ltz: src1:i len:64
sparc_cond_exc_gtz: src1:i len:64
sparc_cond_exc_gez: src1:i len:64
sparc_cond_exc_lez: src1:i len:64

relaxed_nop: len:0

# Linear IR opcodes
nop: len:0
dummy_use: src1:i len:0
dummy_iconst: dest:i len:0
dummy_i8const: dest:i len:0
dummy_r8const: dest:f len:0
dummy_r4const: dest:f len:0
not_reached: len:0
not_null: src1:i len:0

jump_table: dest:i len:64

cond_exc_ieq: len:64
cond_exc_ine_un: len:64
cond_exc_ilt: len:64
cond_exc_ilt_un: len:64
cond_exc_igt: len:64
cond_exc_igt_un: len:64
cond_exc_ige: len:64
cond_exc_ige_un: len:64
cond_exc_ile: len:64
cond_exc_ile_un: len:64
cond_exc_iov: len:64
cond_exc_ino: len:64
cond_exc_ic: len:64
cond_exc_inc: len:64

long_conv_to_ovf_i4_2: dest:i src1:i src2:i len:48

vcall2: len:40 clob:c
vcall2_reg: src1:i len:64 clob:c
vcall2_membase: src1:b len:64 clob:c

liverange_start: len:0
liverange_end: len:0
gc_safe_point: len:0
