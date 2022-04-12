# powerpc cpu description file
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
memory_barrier: len:4
nop: len:4
relaxed_nop: len:4
break: len:32
seq_point: len:24
il_seq_point: len:0
tailcall: len:120 clob:c

# PowerPC outputs a nice fixed size memcpy loop for larger stack_usage, so 0.
tailcall_parameter: len:0

call: dest:a clob:c len:16
br: len:4
throw: src1:i len:20
rethrow: src1:i len:20
ckfinite: dest:f src1:f
ppc_check_finite: src1:i len:16
add_ovf_carry: dest:i src1:i src2:i len:16
sub_ovf_carry: dest:i src1:i src2:i len:16
add_ovf_un_carry: dest:i src1:i src2:i len:16
sub_ovf_un_carry: dest:i src1:i src2:i len:16
start_handler: len:32
endfinally: len:28
ceq: dest:i len:12
cgt: dest:i len:12
cgt_un: dest:i len:12
clt: dest:i len:12
clt_un: dest:i len:12
localloc: dest:i src1:i len:60
compare: src1:i src2:i len:4
compare_imm: src1:i len:12
fcompare: src1:f src2:f len:12
arglist: src1:i len:12
setlret: src1:i src2:i len:12
check_this: src1:b len:4
voidcall: len:16 clob:c
voidcall_reg: src1:i len:16 clob:c
voidcall_membase: src1:b len:16 clob:c
fcall: dest:g len:16 clob:c
fcall_reg: dest:g src1:i len:16 clob:c
fcall_membase: dest:g src1:b len:16 clob:c
lcall: dest:l len:16 clob:c
lcall_reg: dest:l src1:i len:16 clob:c
lcall_membase: dest:l src1:b len:16 clob:c
vcall: len:16 clob:c
vcall_reg: src1:i len:16 clob:c
vcall_membase: src1:b len:16 clob:c
call_reg: dest:a src1:i len:16 clob:c
call_membase: dest:a src1:b len:16 clob:c
iconst: dest:i len:8
r4const: dest:f len:12
r8const: dest:f len:24
label: len:0
store_membase_reg: dest:b src1:i len:12
storei1_membase_reg: dest:b src1:i len:12
storei2_membase_reg: dest:b src1:i len:12
storei4_membase_reg: dest:b src1:i len:12
storer4_membase_reg: dest:b src1:f len:16
storer8_membase_reg: dest:b src1:f len:12
load_membase: dest:i src1:b len:12
loadi1_membase: dest:i src1:b len:16
loadu1_membase: dest:i src1:b len:12
loadi2_membase: dest:i src1:b len:12
loadu2_membase: dest:i src1:b len:12
loadi4_membase: dest:i src1:b len:12
loadu4_membase: dest:i src1:b len:12
loadr4_membase: dest:f src1:b len:12
loadr8_membase: dest:f src1:b len:12
load_memindex: dest:i src1:b src2:i len:4
loadi1_memindex: dest:i src1:b src2:i len:8
loadu1_memindex: dest:i src1:b src2:i len:4
loadi2_memindex: dest:i src1:b src2:i len:4
loadu2_memindex: dest:i src1:b src2:i len:4
loadi4_memindex: dest:i src1:b src2:i len:4
loadu4_memindex: dest:i src1:b src2:i len:4
loadr4_memindex: dest:f src1:b src2:i len:4
loadr8_memindex: dest:f src1:b src2:i len:4
store_memindex: dest:b src1:i src2:i len:4
storei1_memindex: dest:b src1:i src2:i len:4
storei2_memindex: dest:b src1:i src2:i len:4
storei4_memindex: dest:b src1:i src2:i len:4
storer4_memindex: dest:b src1:i src2:i len:8
storer8_memindex: dest:b src1:i src2:i len:4
loadu4_mem: dest:i len:8
move: dest:i src1:i len:4
fmove: dest:f src1:f len:4
move_f_to_i4: dest:i src1:f len:8
move_i4_to_f: dest:f src1:i len:8
add_imm: dest:i src1:i len:4
sub_imm: dest:i src1:i len:4
mul_imm: dest:i src1:i len:4
# there is no actual support for division or reminder by immediate
# we simulate them, though (but we need to change the burg rules
# to allocate a symbolic reg for src2)
div_imm: dest:i src1:i src2:i len:20
div_un_imm: dest:i src1:i src2:i len:12
rem_imm: dest:i src1:i src2:i len:28
rem_un_imm: dest:i src1:i src2:i len:16
and_imm: dest:i src1:i len:4
or_imm: dest:i src1:i len:4
xor_imm: dest:i src1:i len:4
shl_imm: dest:i src1:i len:4
shr_imm: dest:i src1:i len:4
shr_un_imm: dest:i src1:i len:4
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
cond_exc_ov: len:12
cond_exc_no: len:8
cond_exc_c: len:12
cond_exc_nc: len:8
long_conv_to_ovf_i: dest:i src1:i src2:i len:32
long_mul_ovf:
long_conv_to_r_un: dest:f src1:i src2:i len:37
float_beq: len:8
float_bne_un: len:8
float_blt: len:8
float_blt_un: len:8
float_bgt: len:8
float_bgt_un: len:8
float_bge: len:8
float_bge_un: len:8
float_ble: len:8
float_ble_un: len:8
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
float_conv_to_r4: dest:f src1:f len:4
float_conv_to_u4: dest:i src1:f len:40
float_conv_to_u8: dest:l src1:f len:40
float_conv_to_u2: dest:i src1:f len:40
float_conv_to_u1: dest:i src1:f len:40
float_ceq: dest:i src1:f src2:f len:16
float_cgt: dest:i src1:f src2:f len:16
float_cgt_un: dest:i src1:f src2:f len:20
float_clt: dest:i src1:f src2:f len:16
float_clt_un: dest:i src1:f src2:f len:20
float_cneq: dest:i src1:f src2:f len:16
float_cge: dest:i src1:f src2:f len:16
float_cle: dest:i src1:f src2:f len:16
call_handler: len:12 clob:c
endfilter: src1:i len:32
aotconst: dest:i len:8
load_gotaddr: dest:i len:32
got_entry: dest:i src1:b len:32
abs: dest:f src1:f len:4
sqrt: dest:f src1:f len:4
sqrtf: dest:f src1:f len:4
round: dest:f src1:f len:4
ppc_trunc: dest:f src1:f len:4
ppc_ceil: dest:f src1:f len:4
ppc_floor: dest:f src1:f len:4
adc: dest:i src1:i src2:i len:4
addcc: dest:i src1:i src2:i len:4
subcc: dest:i src1:i src2:i len:4
addcc_imm: dest:i src1:i len:4
sbb: dest:i src1:i src2:i len:4
br_reg: src1:i len:8
ppc_subfic: dest:i src1:i len:4
ppc_subfze: dest:i src1:i len:4
bigmul: len:12 dest:l src1:i src2:i
bigmul_un: len:12 dest:l src1:i src2:i

# Linear IR opcodes
dummy_use: src1:i len:0
dummy_iconst: dest:i len:0
dummy_r8const: dest:f len:0
dummy_r4const: dest:f len:0
not_reached: len:0
not_null: src1:i len:0

# 32 bit opcodes
int_add: dest:i src1:i src2:i len:4
int_sub: dest:i src1:i src2:i len:4
int_mul: dest:i src1:i src2:i len:4
int_div: dest:i src1:i src2:i len:40
int_div_un: dest:i src1:i src2:i len:16
int_rem: dest:i src1:i src2:i len:48
int_rem_un: dest:i src1:i src2:i len:24
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
int_mul_ovf: dest:i src1:i src2:i len:16
int_mul_ovf_un: dest:i src1:i src2:i len:16
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
cond_exc_iov: len:12
cond_exc_ino: len:8
cond_exc_ic: len:12
cond_exc_inc: len:8

icompare: src1:i src2:i len:4
icompare_imm: src1:i len:12

long_conv_to_ovf_i4_2: dest:i src1:i src2:i len:32

# shouldn't use long stuff on ppc32
#long_min: dest:i src1:i src2:i len:8 clob:1
#long_min_un: dest:i src1:i src2:i len:8 clob:1
#long_max: dest:i src1:i src2:i len:8 clob:1
#long_max_un: dest:i src1:i src2:i len:8 clob:1
int_min: dest:i src1:i src2:i len:8 clob:1
int_max: dest:i src1:i src2:i len:8 clob:1
int_min_un: dest:i src1:i src2:i len:8 clob:1
int_max_un: dest:i src1:i src2:i len:8 clob:1

vcall2: len:20 clob:c
vcall2_reg: src1:i len:8 clob:c
vcall2_membase: src1:b len:16 clob:c

jump_table: dest:i len:8

atomic_add_i4: src1:b src2:i dest:i len:28
atomic_cas_i4: src1:b src2:i src3:i dest:i len:38

liverange_start: len:0
liverange_end: len:0
gc_safe_point: len:0
