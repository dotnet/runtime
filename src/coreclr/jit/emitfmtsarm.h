// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//////////////////////////////////////////////////////////////////////////////

// clang-format off
#if !defined(TARGET_ARM)
  #error Unexpected target type
#endif

#ifdef  DEFINE_ID_OPS
//////////////////////////////////////////////////////////////////////////////

#undef  DEFINE_ID_OPS

enum    ID_OPS
{
    ID_OP_NONE,                             // no additional arguments
    ID_OP_SCNS,                             // small const  operand (21-bits or less, no reloc)
    ID_OP_JMP,                              // local jump
    ID_OP_LBL,                              // label operand
    ID_OP_CALL,                             // direct method call
    ID_OP_SPEC,                             // special handling required
};

//////////////////////////////////////////////////////////////////////////////
#else // !DEFINE_ID_OPS
//////////////////////////////////////////////////////////////////////////////

#ifndef IF_DEF
#error  Must define IF_DEF macro before including this file
#endif

//////////////////////////////////////////////////////////////////////////////
//
// enum insFormat   instruction            enum ID_OPS
//                  scheduling
//                  (unused)
//////////////////////////////////////////////////////////////////////////////

IF_DEF(NONE,        IS_NONE,               NONE)     //

IF_DEF(LABEL,       IS_NONE,               JMP )     // label
//IF_DEF(SWR_LABEL,   IS_NONE,               LBL )     // write label to stack
//IF_DEF(METHOD,      IS_NONE,               CALL)     // method
//IF_DEF(CNS,         IS_NONE,               SCNS)     // const

IF_DEF(LARGEJMP,    IS_NONE,               JMP)      // large conditional branch pseudo-op

/////////////////////////////////////////////////////////////////////////////////////////////////////////

IF_DEF(EN9,         IS_NONE,               NONE)     // Instruction has 9 possible encoding types
IF_DEF(EN8,         IS_NONE,               NONE)     // Instruction has 8 possible encoding types
IF_DEF(EN6A,        IS_NONE,               NONE)     // Instruction has 6 possible encoding types, type A
IF_DEF(EN6B,        IS_NONE,               NONE)     // Instruction has 6 possible encoding types, type B
IF_DEF(EN5A,        IS_NONE,               NONE)     // Instruction has 5 possible encoding types, type A
IF_DEF(EN5B,        IS_NONE,               NONE)     // Instruction has 5 possible encoding types, type B
IF_DEF(EN4A,        IS_NONE,               NONE)     // Instruction has 4 possible encoding types, type A
IF_DEF(EN4B,        IS_NONE,               NONE)     // Instruction has 4 possible encoding types, type B
IF_DEF(EN4C,        IS_NONE,               NONE)     // Instruction has 4 possible encoding types, type C
IF_DEF(EN3A,        IS_NONE,               NONE)     // Instruction has 3 possible encoding types, type A
IF_DEF(EN3B,        IS_NONE,               NONE)     // Instruction has 3 possible encoding types, type B
IF_DEF(EN3C,        IS_NONE,               NONE)     // Instruction has 3 possible encoding types, type C
IF_DEF(EN3D,        IS_NONE,               NONE)     // Instruction has 3 possible encoding types, type D
IF_DEF(EN3E,        IS_NONE,               NONE)     // Instruction has 3 possible encoding types, type E
IF_DEF(EN2A,        IS_NONE,               NONE)     // Instruction has 2 possible encoding types, type A
IF_DEF(EN2B,        IS_NONE,               NONE)     // Instruction has 2 possible encoding types, type B
IF_DEF(EN2C,        IS_NONE,               NONE)     // Instruction has 2 possible encoding types, type C
IF_DEF(EN2D,        IS_NONE,               NONE)     // Instruction has 2 possible encoding types, type D
IF_DEF(EN2E,        IS_NONE,               NONE)     // Instruction has 2 possible encoding types, type E
IF_DEF(EN2F,        IS_NONE,               NONE)     // Instruction has 2 possible encoding types, type F
IF_DEF(EN2G,        IS_NONE,               NONE)     // Instruction has 2 possible encoding types, type G

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

IF_DEF(T1_A,        IS_NONE,               NONE)     // T1_A    ................
IF_DEF(T1_B,        IS_NONE,               NONE)     // T1_B    ........cccc....                                           cond
IF_DEF(T1_C,        IS_NONE,               NONE)     // T1_C    .....iiiiimmmddd                       R1  R2              imm5
IF_DEF(T1_D0,       IS_NONE,               NONE)     // T1_D0   ........Dmmmmddd                       R1* R2*
IF_DEF(T1_D1,       IS_NONE,               SPEC)     // T1_D1   .........mmmm...                       R1*
IF_DEF(T1_D2,       IS_NONE,               SPEC)     // T1_D2   .........mmmm...                               R3*
IF_DEF(T1_E,        IS_NONE,               NONE)     // T1_E    ..........mmmddd                       R1  R2
IF_DEF(T1_F,        IS_NONE,               NONE)     // T1_F    .........iiiiiii                       SP                  imm7
IF_DEF(T1_G,        IS_NONE,               NONE)     // T1_G    .......iiinnnddd                       R1  R2              imm3
IF_DEF(T1_H,        IS_NONE,               NONE)     // T1_H    .......mmmnnnddd                       R1  R2  R3
IF_DEF(T1_I,        IS_NONE,               JMP )     // T1_I    ......i.iiiiinnn                       R1                  imm6
IF_DEF(T1_J0,       IS_NONE,               NONE)     // T1_J    .....dddiiiiiiii                       R1                  imm8
IF_DEF(T1_J1,       IS_NONE,               NONE)     // T1_J    .....dddiiiiiiii                       R1                  <regmask8>
IF_DEF(T1_J2,       IS_NONE,               NONE)     // T1_J    .....dddiiiiiiii                       R1  SP              imm8
IF_DEF(T1_J3,       IS_NONE,               LBL )     // T1_J    .....dddiiiiiiii                       R1  PC              imm8
IF_DEF(T1_K,        IS_NONE,               JMP )     // T1_K    ....cccciiiiiiii                       Branch              imm8, cond4
IF_DEF(T1_L0,       IS_NONE,               NONE)     // T1_L0   ........iiiiiiii                                           imm8
IF_DEF(T1_L1,       IS_NONE,               NONE)     // T1_L1   ........rrrrrrrr                                           <regmask8>
IF_DEF(T1_M,        IS_NONE,               JMP )     // T1_M    .....iiiiiiiiiii                       Branch              imm11


IF_DEF(T2_A,        IS_NONE,               NONE)     // T2_A    ................ ................
IF_DEF(T2_B,        IS_NONE,               NONE)     // T2_B    ................ ............iiii                          imm4
IF_DEF(T2_C0,       IS_NONE,               NONE)     // T2_C0   ...........Snnnn .iiiddddiishmmmm       R1  R2  R3      S, imm5, sh
IF_DEF(T2_C1,       IS_NONE,               NONE)     // T2_C1   ...........S.... .iiiddddiishmmmm       R1  R2          S, imm5, sh
IF_DEF(T2_C2,       IS_NONE,               NONE)     // T2_C2   ...........S.... .iiiddddii..mmmm       R1  R2          S, imm5
IF_DEF(T2_C3,       IS_NONE,               NONE)     // T2_C3   ...........S.... ....dddd....mmmm       R1  R2          S
IF_DEF(T2_C4,       IS_NONE,               NONE)     // T2_C4   ...........Snnnn ....dddd....mmmm       R1  R2  R3      S
IF_DEF(T2_C5,       IS_NONE,               NONE)     // T2_C5   ............nnnn ....dddd....mmmm       R1  R2  R3
IF_DEF(T2_C6,       IS_NONE,               NONE)     // T2_C6   ................ ....dddd..iimmmm       R1  R2                   imm2
IF_DEF(T2_C7,       IS_NONE,               NONE)     // T2_C7   ............nnnn ..........shmmmm       R1  R2                   imm2
IF_DEF(T2_C8,       IS_NONE,               NONE)     // T2_C8   ............nnnn .iii....iishmmmm       R1  R2             imm5, sh
IF_DEF(T2_C9,       IS_NONE,               NONE)     // T2_C9   ............nnnn ............mmmm       R1  R2
IF_DEF(T2_C10,      IS_NONE,               NONE)     // T2_C10  ............mmmm ....dddd....mmmm       R1  R2
IF_DEF(T2_D0,       IS_NONE,               NONE)     // T2_D0   ............nnnn .iiiddddii.wwwww       R1  R2             imm5, imm5
IF_DEF(T2_D1,       IS_NONE,               NONE)     // T2_D1   ................ .iiiddddii.wwwww       R1                 imm5, imm5
IF_DEF(T2_E0,       IS_NONE,               NONE)     // T2_E0   ............nnnn tttt......shmmmm       R1  R2  R3               imm2
IF_DEF(T2_E1,       IS_NONE,               NONE)     // T2_E1   ............nnnn tttt............       R1  R2
IF_DEF(T2_E2,       IS_NONE,               NONE)     // T2_E2   ................ tttt............       R1
IF_DEF(T2_F1,       IS_NONE,               NONE)     // T2_F1   ............nnnn ttttdddd....mmmm       R1  R2  R3  R4
IF_DEF(T2_F2,       IS_NONE,               NONE)     // T2_F2   ............nnnn aaaadddd....mmmm       R1  R2  R3  R4
IF_DEF(T2_G0,       IS_NONE,               NONE)     // T2_G0   .......PU.W.nnnn ttttTTTTiiiiiiii       R1  R2  R3         imm8, PUW
IF_DEF(T2_G1,       IS_NONE,               NONE)     // T2_G1   ............nnnn ttttTTTT........       R1  R2  R3
IF_DEF(T2_H0,       IS_NONE,               NONE)     // T2_H0   ............nnnn tttt.PUWiiiiiiii       R1  R2             imm8, PUW
IF_DEF(T2_H1,       IS_NONE,               NONE)     // T2_H1   ............nnnn tttt....iiiiiiii       R1  R2             imm8
IF_DEF(T2_H2,       IS_NONE,               NONE)     // T2_H2   ............nnnn ........iiiiiiii       R1                 imm8
IF_DEF(T2_I0,       IS_NONE,               NONE)     // T2_I0   ..........W.nnnn rrrrrrrrrrrrrrrr       R1              W, imm16
IF_DEF(T2_I1,       IS_NONE,               NONE)     // T2_I1   ................ rrrrrrrrrrrrrrrr                          imm16
IF_DEF(T2_J1,       IS_NONE,               JMP )     // T2_J1   .....Scccciiiiii ..j.jiiiiiiiiiii       Branch             imm20, cond4
IF_DEF(T2_J2,       IS_NONE,               JMP )     // T2_J2   .....Siiiiiiiiii ..j.jiiiiiiiiii.       Branch             imm24
IF_DEF(T2_J3,       IS_NONE,               CALL)     // T2_J3   .....Siiiiiiiiii ..j.jiiiiiiiiii.       Call               imm24
IF_DEF(T2_K1,       IS_NONE,               NONE)     // T2_K1   ............nnnn ttttiiiiiiiiiiii       R1  R2             imm12
IF_DEF(T2_K2,       IS_NONE,               NONE)     // T2_K2   ............nnnn ....iiiiiiiiiiii       R1                 imm12
IF_DEF(T2_K3,       IS_NONE,               NONE)     // T2_K3   ........U....... ....iiiiiiiiiiii       PC              U, imm12
IF_DEF(T2_K4,       IS_NONE,               NONE)     // T2_K4   ........U....... ttttiiiiiiiiiiii       R1  PC          U, imm12
IF_DEF(T2_L0,       IS_NONE,               NONE)     // T2_L0   .....i.....Snnnn .iiiddddiiiiiiii       R1  R2          S, imm8<<imm4
IF_DEF(T2_L1,       IS_NONE,               NONE)     // T2_L1   .....i.....S.... .iiiddddiiiiiiii       R1              S, imm8<<imm4
IF_DEF(T2_L2,       IS_NONE,               NONE)     // T2_L2   .....i......nnnn .iii....iiiiiiii       R1                 imm8<<imm4
IF_DEF(T2_M0,       IS_NONE,               NONE)     // T2_M0   .....i......nnnn .iiiddddiiiiiiii       R1  R2             imm12
IF_DEF(T2_M1,       IS_NONE,               LBL )     // T2_M1   .....i.......... .iiiddddiiiiiiii       R1  PC             imm12
IF_DEF(T2_N,        IS_NONE,               NONE)     // T2_N    .....i......iiii .iiiddddiiiiiiii       R1                 imm16    ; movw/movt
IF_DEF(T2_N1,       IS_NONE,               JMP)      // T2_N1   .....i......iiii .iiiddddiiiiiiii       R1                 imm16    ; movw/movt of a code address
IF_DEF(T2_N2,       IS_NONE,               NONE)     // T2_N2   .....i......iiii .iiiddddiiiiiiii       R1                 imm16    ; movw/movt of a data address
IF_DEF(T2_N3,       IS_NONE,               NONE)     // T2_N3   .....i......iiii .iiiddddiiiiiiii       R1                 imm16    ; movw/movt (relocatable imm)
IF_DEF(T2_O1,       IS_NONE,               NONE)     // T2_O1   ............nnnn ttttTTTT....dddd       R1  R2  R3  R4
IF_DEF(T2_O2,       IS_NONE,               NONE)     // T2_O2   ............nnnn tttt........dddd       R1  R2  R3
IF_DEF(T2_O3,       IS_NONE,               NONE)     // T2_O3   ............nnnn ttttddddiiiiiiii       R1  R2  R3         imm8
IF_DEF(T2_VLDST,    IS_NONE,               NONE)     // T2_VLDST 11101101UD0Lnnnn dddd101Ziiiiiiii      D1  R2             imm(+-1020)
IF_DEF(T2_VFP2,     IS_NONE,               NONE)     // T2_VFP2  111011101D110--- dddd101Z--M0mmmm      D1  D2
IF_DEF(T2_VFP3,     IS_NONE,               NONE)     // T2_VFP3  11101110-D--nnnn dddd101ZN-M0mmmm      D1  D2  D3
IF_DEF(T2_VMOVS,    IS_NONE,               NONE)
IF_DEF(T2_VMOVD,    IS_NONE,               NONE)

IF_DEF(INVALID,     IS_NONE,               NONE)     //

//////////////////////////////////////////////////////////////////////////////
#undef IF_DEF
//////////////////////////////////////////////////////////////////////////////

#endif // !DEFINE_ID_OPS
//////////////////////////////////////////////////////////////////////////////
// clang-format on
