# mips cpu description file
# this file is read by genmdesc to pruduce a table with all the relevant
# information about the cpu instructions that may be used by the regsiter
# allocator, the scheduler and other parts of the arch-dependent part of mini.
#
# An opcode name is followed by a colon and optional specifiers.
# A specifier has a name, a colon and a value.
# Specifiers are separated by white space.
# Here is a description of the specifiers valid for this file and their
# possible values.
#
# dest:register       describes the destination register of an instruction
# src1:register       describes the first source register of an instruction
# src2:register       describes the second source register of an instruction
#
# register may have the following values:
#	i  integer register
#	l  integer register pair
#	v  v0 register (output from calls)
#	V  v0/v1 register pair (output from calls)
#       a  at register
#	b  base register (used in address references)
#	f  floating point register (pair - always)
#	g  floating point register return pair (f0/f1)
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
memory_barrier: len:4
nop: len:4
relaxed_nop: len:4
break: len:16
call: dest:v clob:c len:20
br: len:16
switch: src1:i len:40
seq_point: len:24
il_seq_point: len:0

int_conv_to_r_un: dest:f src1:i len:32
throw: src1:i len:24
rethrow: src1:i len:24
ckfinite: dest:f src1:f len:52
start_handler: len:16
endfinally: len:12
ceq: dest:i len:16
cgt: dest:i len:16
cgt_un: dest:i len:16
clt: dest:i len:16
clt_un: dest:i len:16
localloc: dest:i src1:i len:60
compare: src1:i src2:i len:20
compare_imm: src1:i len:20
fcompare: src1:f src2:f len:12
arglist: src1:i len:12
setlret: src1:i src2:i len:12
check_this: src1:b len:4

voidcall: len:20 clob:c
voidcall_reg: src1:i len:20 clob:c
voidcall_membase: src1:b len:20 clob:c

fcall: dest:g len:20 clob:c
fcall_reg: dest:g src1:i len:20 clob:c
fcall_membase: dest:g src1:b len:20 clob:c

lcall: dest:V len:28 clob:c
lcall_reg: dest:V src1:i len:28 clob:c
lcall_membase: dest:V src1:b len:28 clob:c

call_reg: dest:v src1:i len:20 clob:c
call_membase: dest:v src1:b len:20 clob:c

vcall: len:16 clob:c
vcall_reg: src1:i len:20 clob:c
vcall_membase: src1:b len:20 clob:c

vcall2: len:16 clob:c
vcall2_reg: src1:i len:20 clob:c
vcall2_membase: src1:b len:20 clob:c

jump_table: dest:i len:8

iconst: dest:i len:12
i8const: dest:l len:24
r4const: dest:f len:20
r8const: dest:f len:28
label: len:0
store_membase_imm: dest:b len:20
store_membase_reg: dest:b src1:i len:20
storei1_membase_imm: dest:b len:20
storei1_membase_reg: dest:b src1:i len:20
storei2_membase_imm: dest:b len:20
storei2_membase_reg: dest:b src1:i len:20
storei4_membase_imm: dest:b len:20
storei4_membase_reg: dest:b src1:i len:20
storei8_membase_imm: dest:b
storei8_membase_reg: dest:b src1:i len:20
storer4_membase_reg: dest:b src1:f len:20
storer8_membase_reg: dest:b src1:f len:20
load_membase: dest:i src1:b len:20
loadi1_membase: dest:i src1:b len:20
loadu1_membase: dest:i src1:b len:20
loadi2_membase: dest:i src1:b len:20
loadu2_membase: dest:i src1:b len:20
loadi4_membase: dest:i src1:b len:20
loadu4_membase: dest:i src1:b len:20
loadi8_membase: dest:i src1:b len:20
loadr4_membase: dest:f src1:b len:20
loadr8_membase: dest:f src1:b len:20
load_memindex: dest:i src1:b src2:i len:4
loadi1_memindex: dest:i src1:b src2:i len:12
loadu1_memindex: dest:i src1:b src2:i len:12
loadi2_memindex: dest:i src1:b src2:i len:12
loadu2_memindex: dest:i src1:b src2:i len:12
loadi4_memindex: dest:i src1:b src2:i len:12
loadu4_memindex: dest:i src1:b src2:i len:12
loadr4_memindex: dest:f src1:b src2:i len:12
loadr8_memindex: dest:f src1:b src2:i len:12
store_memindex: dest:b src1:i src2:i len:12
storei1_memindex: dest:b src1:i src2:i len:12
storei2_memindex: dest:b src1:i src2:i len:12
storei4_memindex: dest:b src1:i src2:i len:12
storer4_memindex: dest:b src1:f src2:i len:12
storer8_memindex: dest:b src1:f src2:i len:12
loadu4_mem: dest:i len:8
move: dest:i src1:i len:4
fmove: dest:f src1:f len:8
move_f_to_i4: dest:i src1:f len:4
move_i4_to_f: dest:f src1:i len:4
add_imm: dest:i src1:i len:12
sub_imm: dest:i src1:i len:12
mul_imm: dest:i src1:i len:20
# there is no actual support for division or reminder by immediate
# we simulate them, though (but we need to change the burg rules
# to allocate a symbolic reg for src2)
div_imm: dest:i src1:i src2:i len:20
div_un_imm: dest:i src1:i src2:i len:12
rem_imm: dest:i src1:i src2:i len:28
rem_un_imm: dest:i src1:i src2:i len:16
and_imm: dest:i src1:i len:12
or_imm: dest:i src1:i len:12
xor_imm: dest:i src1:i len:12
shl_imm: dest:i src1:i len:8
shr_imm: dest:i src1:i len:8
shr_un_imm: dest:i src1:i len:8

# Linear IR opcodes
dummy_use: src1:i len:0
dummy_iconst: dest:i len:0
dummy_i8const: dest:i len:0
dummy_r8const: dest:f len:0
dummy_r4const: dest:f len:0
not_reached: len:0
not_null: src1:i len:0

# 32 bit opcodes
int_add: dest:i src1:i src2:i len:4
int_sub: dest:i src1:i src2:i len:4
int_mul: dest:i src1:i src2:i len:16
int_div: dest:i src1:i src2:i len:84
int_div_un: dest:i src1:i src2:i len:40
int_rem: dest:i src1:i src2:i len:84
int_rem_un: dest:i src1:i src2:i len:40
int_and: dest:i src1:i src2:i len:4
int_or: dest:i src1:i src2:i len:4
int_xor: dest:i src1:i src2:i len:4
int_shl: dest:i src1:i src2:i len:4
int_shr: dest:i src1:i src2:i len:4
int_shr_un: dest:i src1:i src2:i len:4
int_neg: dest:i src1:i len:4
int_not: dest:i src1:i len:4
int_conv_to_i1: dest:i src1:i len:8
int_conv_to_i2: dest:i src1:i len:8
int_conv_to_i4: dest:i src1:i len:4
int_conv_to_r4: dest:f src1:i len:36
int_conv_to_r8: dest:f src1:i len:36
int_conv_to_u4: dest:i src1:i
int_conv_to_u2: dest:i src1:i len:8
int_conv_to_u1: dest:i src1:i len:4
int_beq: len:8
int_bge: len:8
int_bgt: len:8
int_ble: len:8
int_blt: len:8
int_bne_un: len:8
int_bge_un: len:8
int_bgt_un: len:8
int_ble_un: len:8
int_blt_un: len:8
int_add_ovf: dest:i src1:i src2:i len:16
int_add_ovf_un: dest:i src1:i src2:i len:16
int_mul_ovf: dest:i src1:i src2:i len:56
int_mul_ovf_un: dest:i src1:i src2:i len:56
int_sub_ovf: dest:i src1:i src2:i len:16
int_sub_ovf_un: dest:i src1:i src2:i len:16

int_adc: dest:i src1:i src2:i len:4
int_addcc: dest:i src1:i src2:i len:4
int_subcc: dest:i src1:i src2:i len:4
int_sbb: dest:i src1:i src2:i len:4
int_adc_imm: dest:i src1:i len:12
int_sbb_imm: dest:i src1:i len:12

int_add_imm: dest:i src1:i len:12
int_sub_imm: dest:i src1:i len:12
int_mul_imm: dest:i src1:i len:12
int_div_imm: dest:i src1:i len:20
int_div_un_imm: dest:i src1:i len:12
int_rem_imm: dest:i src1:i len:28
int_rem_un_imm: dest:i src1:i len:16
int_and_imm: dest:i src1:i len:12
int_or_imm: dest:i src1:i len:12
int_xor_imm: dest:i src1:i len:12
int_shl_imm: dest:i src1:i len:8
int_shr_imm: dest:i src1:i len:8
int_shr_un_imm: dest:i src1:i len:8

int_ceq: dest:i len:16
int_cgt: dest:i len:16
int_cgt_un: dest:i len:16
int_clt: dest:i len:16
int_clt_un: dest:i len:16

cond_exc_eq: len:32
cond_exc_ne_un: len:32
cond_exc_lt: len:32
cond_exc_lt_un: len:32
cond_exc_gt: len:32
cond_exc_gt_un: len:32
cond_exc_ge: len:32
cond_exc_ge_un: len:32
cond_exc_le: len:32
cond_exc_le_un: len:32
cond_exc_ov: len:32
cond_exc_no: len:32
cond_exc_c: len:32
cond_exc_nc: len:32

cond_exc_ieq: len:32
cond_exc_ine_un: len:32
cond_exc_ilt: len:32
cond_exc_ilt_un: len:32
cond_exc_igt: len:32
cond_exc_igt_un: len:32
cond_exc_ige: len:32
cond_exc_ige_un: len:32
cond_exc_ile: len:32
cond_exc_ile_un: len:32
cond_exc_iov: len:12
cond_exc_ino: len:32
cond_exc_ic: len:12
cond_exc_inc: len:32

icompare: src1:i src2:i len:4
icompare_imm: src1:i len:12

# 64 bit opcodes
long_add: dest:i src1:i src2:i len:4
long_sub: dest:i src1:i src2:i len:4
long_mul: dest:i src1:i src2:i len:32
long_mul_imm: dest:i src1:i len:4
long_div: dest:i src1:i src2:i len:40
long_div_un: dest:i src1:i src2:i len:16
long_rem: dest:i src1:i src2:i len:48
long_rem_un: dest:i src1:i src2:i len:24
long_and: dest:i src1:i src2:i len:4
long_or: dest:i src1:i src2:i len:4
long_xor: dest:i src1:i src2:i len:4
long_shl: dest:i src1:i src2:i len:4
long_shl_imm: dest:i src1:i len:4
long_shr: dest:i src1:i src2:i len:4
long_shr_un: dest:i src1:i src2:i len:4
long_shr_imm: dest:i src1:i len:4
long_shr_un_imm: dest:i src1:i len:4
long_neg: dest:i src1:i len:4
long_not: dest:i src1:i len:4
long_conv_to_i1: dest:i src1:l len:32
long_conv_to_i2: dest:i src1:l len:32
long_conv_to_i4: dest:i src1:l len:32
long_conv_to_r4: dest:f src1:l len:32
long_conv_to_r8: dest:f src1:l len:32
long_conv_to_u4: dest:i src1:l len:32
long_conv_to_u8: dest:l src1:l len:32
long_conv_to_u2: dest:i src1:l len:32
long_conv_to_u1: dest:i src1:l len:32
long_conv_to_i:  dest:i src1:l len:32
long_conv_to_ovf_i: dest:i src1:i src2:i len:32
long_conv_to_ovf_i4_2: dest:i src1:i src2:i len:32
zext_i4: dest:i src1:i len:16
sext_i4: dest:i src1:i len:16

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
long_add_ovf: dest:i src1:i src2:i len:16
long_add_ovf_un: dest:i src1:i src2:i len:16
long_mul_ovf: dest:i src1:i src2:i len:16
long_mul_ovf_un: dest:i src1:i src2:i len:16
long_sub_ovf: dest:i src1:i src2:i len:16
long_sub_ovf_un: dest:i src1:i src2:i len:16

long_ceq: dest:i len:12
long_cgt: dest:i len:12
long_cgt_un: dest:i len:12
long_clt: dest:i len:12
long_clt_un: dest:i len:12

long_add_imm: dest:i src1:i clob:1 len:4
long_sub_imm: dest:i src1:i clob:1 len:4
long_and_imm: dest:i src1:i clob:1 len:4
long_or_imm: dest:i src1:i clob:1 len:4
long_xor_imm: dest:i src1:i clob:1 len:4

lcompare: src1:i src2:i len:4
lcompare_imm: src1:i len:12

long_conv_to_r_un: dest:f src1:i src2:i len:37

float_beq:    len:16
float_bne_un: len:16
float_blt:    len:16
float_blt_un: len:16
float_bgt:    len:16
float_bgt_un: len:16
float_bge:    len:16
float_bge_un: len:16
float_ble:    len:16
float_ble_un: len:16

float_add: dest:f src1:f src2:f len:4
float_sub: dest:f src1:f src2:f len:4
float_mul: dest:f src1:f src2:f len:4
float_div: dest:f src1:f src2:f len:4
float_div_un: dest:f src1:f src2:f len:4
float_rem: dest:f src1:f src2:f len:16
float_rem_un: dest:f src1:f src2:f len:16
float_neg: dest:f src1:f len:4
float_not: dest:f src1:f len:4
float_conv_to_i1: dest:i src1:f len:40
float_conv_to_i2: dest:i src1:f len:40
float_conv_to_i4: dest:i src1:f len:40
float_conv_to_i8: dest:l src1:f len:40
float_conv_to_r4: dest:f src1:f len:8
float_conv_to_u4: dest:i src1:f len:40
float_conv_to_u8: dest:l src1:f len:40
float_conv_to_u2: dest:i src1:f len:40
float_conv_to_u1: dest:i src1:f len:40
float_conv_to_i: dest:i src1:f len:40
float_ceq: dest:i src1:f src2:f len:20
float_cgt: dest:i src1:f src2:f len:20
float_cgt_un: dest:i src1:f src2:f len:20
float_clt: dest:i src1:f src2:f len:20
float_clt_un: dest:i src1:f src2:f len:20
float_conv_to_u: dest:i src1:f len:36
call_handler: len:20 clob:c
endfilter: src1:i len:16
aotconst: dest:i len:8
sqrt: dest:f src1:f len:4
adc: dest:i src1:i src2:i len:4
addcc: dest:i src1:i src2:i len:4
subcc: dest:i src1:i src2:i len:4
adc_imm: dest:i src1:i len:12
addcc_imm: dest:i src1:i len:12
subcc_imm: dest:i src1:i len:12
sbb: dest:i src1:i src2:i len:4
sbb_imm: dest:i src1:i len:12
br_reg: src1:i len:8
#ppc_subfic: dest:i src1:i len:4
#ppc_subfze: dest:i src1:i len:4
bigmul: len:52 dest:l src1:i src2:i
bigmul_un: len:52 dest:l src1:i src2:i
mips_beq: src1:i src2:i len:24
mips_bgez: src1:i len:24
mips_bgtz: src1:i len:24
mips_blez: src1:i len:24
mips_bltz: src1:i len:24
mips_bne: src1:i src2:i len:24
mips_cvtsd: dest:f src1:f len:8
mips_fbeq: src1:f src2:f len:16
mips_fbge: src1:f src2:f len:32
mips_fbge_un: src1:f src2:f len:16
mips_fbgt: src1:f src2:f len:32
mips_fbgt_un: src1:f src2:f len:16
mips_fble: src1:f src2:f len:32
mips_fble_un: src1:f src2:f len:16
mips_fblt: src1:f src2:f len:32
mips_fblt_un: src1:f src2:f len:16
mips_fbne: src1:f src2:f len:16
mips_lwc1: dest:f src1:b len:16
mips_mtc1_s: dest:f src1:i len:8
mips_mtc1_s2: dest:f src1:i src2:i len:8
mips_mfc1_s: dest:i src1:f len:8
mips_mtc1_d: dest:f src1:i len:8
mips_mfc1_d: dest:i src1:f len:8
mips_slti: dest:i src1:i len:4
mips_slt: dest:i src1:i src2:i len:4
mips_sltiu: dest:i src1:i len:4
mips_sltu: dest:i src1:i src2:i len:4
mips_cond_exc_eq: src1:i src2:i len:44
mips_cond_exc_ge: src1:i src2:i len:44
mips_cond_exc_gt: src1:i src2:i len:44
mips_cond_exc_le: src1:i src2:i len:44
mips_cond_exc_lt: src1:i src2:i len:44
mips_cond_exc_ne_un: src1:i src2:i len:44
mips_cond_exc_ge_un: src1:i src2:i len:44
mips_cond_exc_gt_un: src1:i src2:i len:44
mips_cond_exc_le_un: src1:i src2:i len:44
mips_cond_exc_lt_un: src1:i src2:i len:44
mips_cond_exc_ov: src1:i src2:i len:44
mips_cond_exc_no: src1:i src2:i len:44
mips_cond_exc_c: src1:i src2:i len:44
mips_cond_exc_nc: src1:i src2:i len:44
mips_cond_exc_ieq: src1:i src2:i len:44
mips_cond_exc_ige: src1:i src2:i len:44
mips_cond_exc_igt: src1:i src2:i len:44
mips_cond_exc_ile: src1:i src2:i len:44
mips_cond_exc_ilt: src1:i src2:i len:44
mips_cond_exc_ine_un: src1:i src2:i len:44
mips_cond_exc_ige_un: src1:i src2:i len:44
mips_cond_exc_igt_un: src1:i src2:i len:44
mips_cond_exc_ile_un: src1:i src2:i len:44
mips_cond_exc_ilt_un: src1:i src2:i len:44
mips_cond_exc_iov: src1:i src2:i len:44
mips_cond_exc_ino: src1:i src2:i len:44
mips_cond_exc_ic: src1:i src2:i len:44
mips_cond_exc_inc: src1:i src2:i len:44

liverange_start: len:0
liverange_end: len:0
gc_safe_point: len:0
