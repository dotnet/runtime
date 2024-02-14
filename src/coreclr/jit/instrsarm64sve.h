// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************
 *  Arm64 instructions for JIT compiler
 *
 *          id       -- the enum name for the instruction
 *          nm       -- textual name (for assembly dipslay)
 *          info     -- miscellaneous instruction info (load/store/compare/ASIMD right shift)
 *          fmt      -- encoding format used by this instruction
 *           e1       -- encoding 1
 *           e2       -- encoding 2
 *           e3       -- encoding 3
 *           e4       -- encoding 4
 *           e5       -- encoding 5
 *           e6       -- encoding 6
 *           e7       -- encoding 7
 *           e8       -- encoding 8
 *           e9       -- encoding 9
 *           e10      -- encoding 10
 *           e11      -- encoding 11
 *           e12      -- encoding 12
 *           e13      -- encoding 13
 *****************************************************************************/
#if !defined(TARGET_ARM64)
#error Unexpected target type
#endif

#ifndef INST1
#error INST1 must be defined before including this file.
#endif
#ifndef INST2
#error INST2 must be defined before including this file.
#endif
#ifndef INST3
#error INST3 must be defined before including this file.
#endif
#ifndef INST4
#error INST4 must be defined before including this file.
#endif
#ifndef INST5
#error INST5 must be defined before including this file.
#endif
#ifndef INST6
#error INST6 must be defined before including this file.
#endif
#ifndef INST7
#error INST7 must be defined before including this file.
#endif
#ifndef INST8
#error INST8 must be defined before including this file.
#endif
#ifndef INST9
#error INST9 must be defined before including this file.
#endif
#ifndef INST11
#error INST11 must be defined before including this file.
#endif
#ifndef INST13
#error INST13 must be defined before including this file.
#endif

/*****************************************************************************/
/*               The following is ARM64-specific                             */
/*****************************************************************************/

// If you're adding a new instruction:
// You need not only to fill in one of these macros describing the instruction, but also:
//   * If the instruction writes to more than one destination register, update the function
//     emitInsMayWriteMultipleRegs in emitArm64.cpp.

// clang-format off
INST13(invalid,          "INVALID",               0,                       IF_NONE,                          BAD_CODE,         BAD_CODE,         BAD_CODE,         BAD_CODE,         BAD_CODE,         BAD_CODE,         BAD_CODE,         BAD_CODE,         BAD_CODE,         BAD_CODE,         BAD_CODE,         BAD_CODE,         BAD_CODE)


//     enum              name                     info                                              SVE_AU_3A        SVE_BT_1A        SVE_BV_2A        SVE_BV_2A_J      SVE_BW_2A        SVE_CB_2A        SVE_CP_3A        SVE_CQ_3A        SVE_CW_4A        SVE_CZ_4A        SVE_CZ_4A_K      SVE_CZ_4A_L      SVE_EB_1A        
INST13(mov,              "mov",                   0,                       IF_SVE_13A,              0x04603000,      0x05C00000,      0x05100000,      0x05104000,      0x05202000,      0x05203800,      0x05208000,      0x0528A000,      0x0520C000,      0x25004000,      0x25004210,      0x25804000,      0x2538C000       )
    // MOV     <Zd>.D, <Zn>.D                                                            SVE_AU_3A           00000100011mmmmm 001100nnnnnddddd     0460 3000   
    // MOV     <Zd>.<T>, #<const>                                                        SVE_BT_1A           00000101110000ii iiiiiiiiiiiddddd     05C0 0000   
    // MOV     <Zd>.<T>, <Pg>/Z, #<imm>{, <shift>}                                       SVE_BV_2A           00000101xx01gggg 00hiiiiiiiiddddd     0510 0000   
    // MOV     <Zd>.<T>, <Pg>/M, #<imm>{, <shift>}                                       SVE_BV_2A_J         00000101xx01gggg 01hiiiiiiiiddddd     0510 4000   
    // MOV     <Zd>.<T>, <Zn>.<T>[<imm>]                                                 SVE_BW_2A           00000101ii1xxxxx 001000nnnnnddddd     0520 2000   
    // MOV     <Zd>.<T>, <R><n|SP>                                                       SVE_CB_2A           00000101xx100000 001110nnnnnddddd     0520 3800   
    // MOV     <Zd>.<T>, <Pg>/M, <V><n>                                                  SVE_CP_3A           00000101xx100000 100gggnnnnnddddd     0520 8000   
    // MOV     <Zd>.<T>, <Pg>/M, <R><n|SP>                                               SVE_CQ_3A           00000101xx101000 101gggnnnnnddddd     0528 A000   
    // MOV     <Zd>.<T>, <Pv>/M, <Zn>.<T>                                                SVE_CW_4A           00000101xx1mmmmm 11VVVVnnnnnddddd     0520 C000   
    // MOV     <Pd>.B, <Pg>/Z, <Pn>.B                                                    SVE_CZ_4A           001001010000MMMM 01gggg0NNNN0DDDD     2500 4000   
    // MOV     <Pd>.B, <Pg>/M, <Pn>.B                                                    SVE_CZ_4A_K         001001010000MMMM 01gggg1NNNN1DDDD     2500 4210   
    // MOV     <Pd>.B, <Pn>.B                                                            SVE_CZ_4A_L         001001011000MMMM 01gggg0NNNN0DDDD     2580 4000   
    // MOV     <Zd>.<T>, #<imm>{, <shift>}                                               SVE_EB_1A           00100101xx111000 11hiiiiiiiiddddd     2538 C000   


//     enum              name                     info                                              SVE_JD_4B        SVE_JD_4C        SVE_JI_3A_A      SVE_JJ_4A        SVE_JJ_4A_B      SVE_JJ_4A_C      SVE_JJ_4A_D      SVE_JJ_4B        SVE_JJ_4B_E      SVE_JN_3B        SVE_JN_3C        
INST11(st1w,             "st1w",                  0,                       IF_SVE_11A,              0xE5404000,      0xE5004000,      0xE540A000,      0xE5608000,      0xE5208000,      0xE5008000,      0xE5408000,      0xE520A000,      0xE500A000,      0xE540E000,      0xE500E000       )
    // ST1W    {<Zt>.<T>}, <Pg>, [<Xn|SP>, <Xm>, LSL #2]                                 SVE_JD_4B           1110010101xmmmmm 010gggnnnnnttttt     E540 4000   
    // ST1W    {<Zt>.Q }, <Pg>, [<Xn|SP>, <Xm>, LSL #2]                                  SVE_JD_4C           11100101000mmmmm 010gggnnnnnttttt     E500 4000   
    // ST1W    {<Zt>.D }, <Pg>, [<Zn>.D{, #<imm>}]                                       SVE_JI_3A_A         11100101010iiiii 101gggnnnnnttttt     E540 A000   
    // ST1W    {<Zt>.S }, <Pg>, [<Xn|SP>, <Zm>.S, <mod> #2]                              SVE_JJ_4A           11100101011mmmmm 1h0gggnnnnnttttt     E560 8000   
    // ST1W    {<Zt>.D }, <Pg>, [<Xn|SP>, <Zm>.D, <mod> #2]                              SVE_JJ_4A_B         11100101001mmmmm 1h0gggnnnnnttttt     E520 8000   
    // ST1W    {<Zt>.D }, <Pg>, [<Xn|SP>, <Zm>.D, <mod>]                                 SVE_JJ_4A_C         11100101000mmmmm 1h0gggnnnnnttttt     E500 8000   
    // ST1W    {<Zt>.S }, <Pg>, [<Xn|SP>, <Zm>.S, <mod>]                                 SVE_JJ_4A_D         11100101010mmmmm 1h0gggnnnnnttttt     E540 8000   
    // ST1W    {<Zt>.D }, <Pg>, [<Xn|SP>, <Zm>.D, LSL #2]                                SVE_JJ_4B           11100101001mmmmm 101gggnnnnnttttt     E520 A000   
    // ST1W    {<Zt>.D }, <Pg>, [<Xn|SP>, <Zm>.D]                                        SVE_JJ_4B_E         11100101000mmmmm 101gggnnnnnttttt     E500 A000   
    // ST1W    {<Zt>.<T>}, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]                             SVE_JN_3B           1110010101x0iiii 111gggnnnnnttttt     E540 E000   
    // ST1W    {<Zt>.Q }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]                              SVE_JN_3C           111001010000iiii 111gggnnnnnttttt     E500 E000   


//    enum               name                     info                                              SVE_HW_4A        SVE_HW_4A_A      SVE_HW_4A_B      SVE_HW_4A_C      SVE_HW_4B        SVE_HW_4B_D      SVE_HX_3A_E      SVE_IJ_3A_F      SVE_IK_4A_G      
INST9(ld1sh,             "ld1sh",                 0,                       IF_SVE_9A,               0x84A00000,      0xC4A00000,      0xC4800000,      0x84800000,      0xC4E08000,      0xC4C08000,      0x84A08000,      0xA500A000,      0xA5004000       )
    // LD1SH   {<Zt>.S }, <Pg>/Z, [<Xn|SP>, <Zm>.S, <mod> #1]                            SVE_HW_4A           100001001h1mmmmm 000gggnnnnnttttt     84A0 0000   
    // LD1SH   {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, <mod> #1]                            SVE_HW_4A_A         110001001h1mmmmm 000gggnnnnnttttt     C4A0 0000   
    // LD1SH   {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, <mod>]                               SVE_HW_4A_B         110001001h0mmmmm 000gggnnnnnttttt     C480 0000   
    // LD1SH   {<Zt>.S }, <Pg>/Z, [<Xn|SP>, <Zm>.S, <mod>]                               SVE_HW_4A_C         100001001h0mmmmm 000gggnnnnnttttt     8480 0000   
    // LD1SH   {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, LSL #1]                              SVE_HW_4B           11000100111mmmmm 100gggnnnnnttttt     C4E0 8000   
    // LD1SH   {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D]                                      SVE_HW_4B_D         11000100110mmmmm 100gggnnnnnttttt     C4C0 8000   
    // LD1SH   {<Zt>.D }, <Pg>/Z, [<Zn>.D{, #<imm>}]                                     SVE_HX_3A_E         10000100101iiiii 100gggnnnnnttttt     84A0 8000   
    // LD1SH   {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]                            SVE_IJ_3A_F         101001010000iiii 101gggnnnnnttttt     A500 A000   
    // LD1SH   {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #1]                                SVE_IK_4A_G         10100101000mmmmm 010gggnnnnnttttt     A500 4000   


//    enum               name                     info                                              SVE_HW_4A        SVE_HW_4A_A      SVE_HW_4A_B      SVE_HW_4A_C      SVE_HW_4B        SVE_HW_4B_D      SVE_HX_3A_E      SVE_IJ_3A_G      SVE_IK_4A_I      
INST9(ld1h,              "ld1h",                  0,                       IF_SVE_9B,               0x84A04000,      0xC4A04000,      0xC4804000,      0x84804000,      0xC4E0C000,      0xC4C0C000,      0x84A0C000,      0xA480A000,      0xA4804000       )
    // LD1H    {<Zt>.S }, <Pg>/Z, [<Xn|SP>, <Zm>.S, <mod> #1]                            SVE_HW_4A           100001001h1mmmmm 010gggnnnnnttttt     84A0 4000   
    // LD1H    {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, <mod> #1]                            SVE_HW_4A_A         110001001h1mmmmm 010gggnnnnnttttt     C4A0 4000   
    // LD1H    {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, <mod>]                               SVE_HW_4A_B         110001001h0mmmmm 010gggnnnnnttttt     C480 4000   
    // LD1H    {<Zt>.S }, <Pg>/Z, [<Xn|SP>, <Zm>.S, <mod>]                               SVE_HW_4A_C         100001001h0mmmmm 010gggnnnnnttttt     8480 4000   
    // LD1H    {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, LSL #1]                              SVE_HW_4B           11000100111mmmmm 110gggnnnnnttttt     C4E0 C000   
    // LD1H    {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D]                                      SVE_HW_4B_D         11000100110mmmmm 110gggnnnnnttttt     C4C0 C000   
    // LD1H    {<Zt>.D }, <Pg>/Z, [<Zn>.D{, #<imm>}]                                     SVE_HX_3A_E         10000100101iiiii 110gggnnnnnttttt     84A0 C000   
    // LD1H    {<Zt>.X }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]                            SVE_IJ_3A_G         101001001000iiii 101gggnnnnnttttt     A480 A000   
    // LD1H    {<Zt>.X }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #1]                                SVE_IK_4A_I         10100100100mmmmm 010gggnnnnnttttt     A480 4000   


//    enum               name                     info                                              SVE_HW_4A        SVE_HW_4A_A      SVE_HW_4A_B      SVE_HW_4A_C      SVE_HW_4B        SVE_HW_4B_D      SVE_HX_3A_E      SVE_IH_3A_F      SVE_II_4A_H      
INST9(ld1w,              "ld1w",                  0,                       IF_SVE_9C,               0x85204000,      0xC5204000,      0xC5004000,      0x85004000,      0xC560C000,      0xC540C000,      0x8520C000,      0xA5002000,      0xA5000000       )
    // LD1W    {<Zt>.S }, <Pg>/Z, [<Xn|SP>, <Zm>.S, <mod> #2]                            SVE_HW_4A           100001010h1mmmmm 010gggnnnnnttttt     8520 4000   
    // LD1W    {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, <mod> #2]                            SVE_HW_4A_A         110001010h1mmmmm 010gggnnnnnttttt     C520 4000   
    // LD1W    {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, <mod>]                               SVE_HW_4A_B         110001010h0mmmmm 010gggnnnnnttttt     C500 4000   
    // LD1W    {<Zt>.S }, <Pg>/Z, [<Xn|SP>, <Zm>.S, <mod>]                               SVE_HW_4A_C         100001010h0mmmmm 010gggnnnnnttttt     8500 4000   
    // LD1W    {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, LSL #2]                              SVE_HW_4B           11000101011mmmmm 110gggnnnnnttttt     C560 C000   
    // LD1W    {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D]                                      SVE_HW_4B_D         11000101010mmmmm 110gggnnnnnttttt     C540 C000   
    // LD1W    {<Zt>.D }, <Pg>/Z, [<Zn>.D{, #<imm>}]                                     SVE_HX_3A_E         10000101001iiiii 110gggnnnnnttttt     8520 C000   
    // LD1W    {<Zt>.X }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]                            SVE_IH_3A_F         101001010000iiii 001gggnnnnnttttt     A500 2000   
    // LD1W    {<Zt>.X }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #2]                                SVE_II_4A_H         10100101000mmmmm 000gggnnnnnttttt     A500 0000   


//    enum               name                     info                                              SVE_IH_3A        SVE_IH_3A_A      SVE_II_4A        SVE_II_4A_B      SVE_IU_4A        SVE_IU_4A_C      SVE_IU_4B        SVE_IU_4B_D      SVE_IV_3A        
INST9(ld1d,              "ld1d",                  0,                       IF_SVE_9D,               0xA5E0A000,      0xA5902000,      0xA5E04000,      0xA5808000,      0xC5A04000,      0xC5804000,      0xC5E0C000,      0xC5C0C000,      0xC5A0C000       )
    // LD1D    {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]                            SVE_IH_3A           101001011110iiii 101gggnnnnnttttt     A5E0 A000   
    // LD1D    {<Zt>.Q }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]                            SVE_IH_3A_A         101001011001iiii 001gggnnnnnttttt     A590 2000   
    // LD1D    {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #3]                                SVE_II_4A           10100101111mmmmm 010gggnnnnnttttt     A5E0 4000   
    // LD1D    {<Zt>.Q }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #3]                                SVE_II_4A_B         10100101100mmmmm 100gggnnnnnttttt     A580 8000   
    // LD1D    {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, <mod> #3]                            SVE_IU_4A           110001011h1mmmmm 010gggnnnnnttttt     C5A0 4000   
    // LD1D    {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, <mod>]                               SVE_IU_4A_C         110001011h0mmmmm 010gggnnnnnttttt     C580 4000   
    // LD1D    {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, LSL #3]                              SVE_IU_4B           11000101111mmmmm 110gggnnnnnttttt     C5E0 C000   
    // LD1D    {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D]                                      SVE_IU_4B_D         11000101110mmmmm 110gggnnnnnttttt     C5C0 C000   
    // LD1D    {<Zt>.D }, <Pg>/Z, [<Zn>.D{, #<imm>}]                                     SVE_IV_3A           11000101101iiiii 110gggnnnnnttttt     C5A0 C000   


//    enum               name                     info                                              SVE_JD_4A        SVE_JI_3A_A      SVE_JJ_4A        SVE_JJ_4A_B      SVE_JJ_4A_C      SVE_JJ_4A_D      SVE_JJ_4B        SVE_JJ_4B_E      SVE_JN_3A        
INST9(st1h,              "st1h",                  0,                       IF_SVE_9E,               0xE4804000,      0xE4C0A000,      0xE4E08000,      0xE4A08000,      0xE4808000,      0xE4C08000,      0xE4A0A000,      0xE480A000,      0xE480E000       )
    // ST1H    {<Zt>.<T>}, <Pg>, [<Xn|SP>, <Xm>, LSL #1]                                 SVE_JD_4A           111001001xxmmmmm 010gggnnnnnttttt     E480 4000   
    // ST1H    {<Zt>.D }, <Pg>, [<Zn>.D{, #<imm>}]                                       SVE_JI_3A_A         11100100110iiiii 101gggnnnnnttttt     E4C0 A000   
    // ST1H    {<Zt>.S }, <Pg>, [<Xn|SP>, <Zm>.S, <mod> #1]                              SVE_JJ_4A           11100100111mmmmm 1h0gggnnnnnttttt     E4E0 8000   
    // ST1H    {<Zt>.D }, <Pg>, [<Xn|SP>, <Zm>.D, <mod> #1]                              SVE_JJ_4A_B         11100100101mmmmm 1h0gggnnnnnttttt     E4A0 8000   
    // ST1H    {<Zt>.D }, <Pg>, [<Xn|SP>, <Zm>.D, <mod>]                                 SVE_JJ_4A_C         11100100100mmmmm 1h0gggnnnnnttttt     E480 8000   
    // ST1H    {<Zt>.S }, <Pg>, [<Xn|SP>, <Zm>.S, <mod>]                                 SVE_JJ_4A_D         11100100110mmmmm 1h0gggnnnnnttttt     E4C0 8000   
    // ST1H    {<Zt>.D }, <Pg>, [<Xn|SP>, <Zm>.D, LSL #1]                                SVE_JJ_4B           11100100101mmmmm 101gggnnnnnttttt     E4A0 A000   
    // ST1H    {<Zt>.D }, <Pg>, [<Xn|SP>, <Zm>.D]                                        SVE_JJ_4B_E         11100100100mmmmm 101gggnnnnnttttt     E480 A000   
    // ST1H    {<Zt>.<T>}, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]                             SVE_JN_3A           111001001xx0iiii 111gggnnnnnttttt     E480 E000   


//    enum               name                     info                                              SVE_JD_4C        SVE_JD_4C_A      SVE_JJ_4A        SVE_JJ_4A_B      SVE_JJ_4B        SVE_JJ_4B_C      SVE_JL_3A        SVE_JN_3C        SVE_JN_3C_D      
INST9(st1d,              "st1d",                  0,                       IF_SVE_9F,               0xE5E04000,      0xE5C04000,      0xE5A08000,      0xE5808000,      0xE5A0A000,      0xE580A000,      0xE5C0A000,      0xE5E0E000,      0xE5C0E000       )
    // ST1D    {<Zt>.D }, <Pg>, [<Xn|SP>, <Xm>, LSL #3]                                  SVE_JD_4C           11100101111mmmmm 010gggnnnnnttttt     E5E0 4000   
    // ST1D    {<Zt>.Q }, <Pg>, [<Xn|SP>, <Xm>, LSL #3]                                  SVE_JD_4C_A         11100101110mmmmm 010gggnnnnnttttt     E5C0 4000   
    // ST1D    {<Zt>.D }, <Pg>, [<Xn|SP>, <Zm>.D, <mod> #3]                              SVE_JJ_4A           11100101101mmmmm 1h0gggnnnnnttttt     E5A0 8000   
    // ST1D    {<Zt>.D }, <Pg>, [<Xn|SP>, <Zm>.D, <mod>]                                 SVE_JJ_4A_B         11100101100mmmmm 1h0gggnnnnnttttt     E580 8000   
    // ST1D    {<Zt>.D }, <Pg>, [<Xn|SP>, <Zm>.D, LSL #3]                                SVE_JJ_4B           11100101101mmmmm 101gggnnnnnttttt     E5A0 A000   
    // ST1D    {<Zt>.D }, <Pg>, [<Xn|SP>, <Zm>.D]                                        SVE_JJ_4B_C         11100101100mmmmm 101gggnnnnnttttt     E580 A000   
    // ST1D    {<Zt>.D }, <Pg>, [<Zn>.D{, #<imm>}]                                       SVE_JL_3A           11100101110iiiii 101gggnnnnnttttt     E5C0 A000   
    // ST1D    {<Zt>.D }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]                              SVE_JN_3C           111001011110iiii 111gggnnnnnttttt     E5E0 E000   
    // ST1D    {<Zt>.Q }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]                              SVE_JN_3C_D         111001011100iiii 111gggnnnnnttttt     E5C0 E000   


//    enum               name                     info                                              SVE_CE_2A        SVE_CE_2B        SVE_CE_2C        SVE_CE_2D        SVE_CF_2A        SVE_CF_2B        SVE_CF_2C        SVE_CF_2D        
INST8(pmov,              "pmov",                  0,                       IF_SVE_8A,               0x052A3800,      0x05A83800,      0x052C3800,      0x05683800,      0x052B3800,      0x05A93800,      0x052D3800,      0x05693800       )
    // PMOV    <Pd>.B, <Zn>                                                              SVE_CE_2A           0000010100101010 001110nnnnn0DDDD     052A 3800   
    // PMOV    <Pd>.D, <Zn>[<imm>]                                                       SVE_CE_2B           000001011i101ii0 001110nnnnn0DDDD     05A8 3800   
    // PMOV    <Pd>.H, <Zn>[<imm>]                                                       SVE_CE_2C           00000101001011i0 001110nnnnn0DDDD     052C 3800   
    // PMOV    <Pd>.S, <Zn>[<imm>]                                                       SVE_CE_2D           0000010101101ii0 001110nnnnn0DDDD     0568 3800   
    // PMOV    <Zd>, <Pn>.B                                                              SVE_CF_2A           0000010100101011 0011100NNNNddddd     052B 3800   
    // PMOV    <Zd>[<imm>], <Pn>.D                                                       SVE_CF_2B           000001011i101ii1 0011100NNNNddddd     05A9 3800   
    // PMOV    <Zd>[<imm>], <Pn>.H                                                       SVE_CF_2C           00000101001011i1 0011100NNNNddddd     052D 3800   
    // PMOV    <Zd>[<imm>], <Pn>.S                                                       SVE_CF_2D           0000010101101ii1 0011100NNNNddddd     0569 3800   


//    enum               name                     info                                              SVE_HW_4A        SVE_HW_4A_A      SVE_HW_4A_B      SVE_HW_4A_C      SVE_HW_4B        SVE_HW_4B_D      SVE_HX_3A_E      SVE_IG_4A_F      
INST8(ldff1sh,           "ldff1sh",               0,                       IF_SVE_8B,               0x84A02000,      0xC4A02000,      0xC4802000,      0x84802000,      0xC4E0A000,      0xC4C0A000,      0x84A0A000,      0xA5006000       )
    // LDFF1SH {<Zt>.S }, <Pg>/Z, [<Xn|SP>, <Zm>.S, <mod> #1]                            SVE_HW_4A           100001001h1mmmmm 001gggnnnnnttttt     84A0 2000   
    // LDFF1SH {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, <mod> #1]                            SVE_HW_4A_A         110001001h1mmmmm 001gggnnnnnttttt     C4A0 2000   
    // LDFF1SH {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, <mod>]                               SVE_HW_4A_B         110001001h0mmmmm 001gggnnnnnttttt     C480 2000   
    // LDFF1SH {<Zt>.S }, <Pg>/Z, [<Xn|SP>, <Zm>.S, <mod>]                               SVE_HW_4A_C         100001001h0mmmmm 001gggnnnnnttttt     8480 2000   
    // LDFF1SH {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, LSL #1]                              SVE_HW_4B           11000100111mmmmm 101gggnnnnnttttt     C4E0 A000   
    // LDFF1SH {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D]                                      SVE_HW_4B_D         11000100110mmmmm 101gggnnnnnttttt     C4C0 A000   
    // LDFF1SH {<Zt>.D }, <Pg>/Z, [<Zn>.D{, #<imm>}]                                     SVE_HX_3A_E         10000100101iiiii 101gggnnnnnttttt     84A0 A000   
    // LDFF1SH {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, <Xm>, LSL #1}]                              SVE_IG_4A_F         10100101000mmmmm 011gggnnnnnttttt     A500 6000   

INST8(ldff1w,            "ldff1w",                0,                       IF_SVE_8B,               0x85206000,      0xC5206000,      0xC5006000,      0x85006000,      0xC560E000,      0xC540E000,      0x8520E000,      0xA5406000       )
    // LDFF1W  {<Zt>.S }, <Pg>/Z, [<Xn|SP>, <Zm>.S, <mod> #2]                            SVE_HW_4A           100001010h1mmmmm 011gggnnnnnttttt     8520 6000   
    // LDFF1W  {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, <mod> #2]                            SVE_HW_4A_A         110001010h1mmmmm 011gggnnnnnttttt     C520 6000   
    // LDFF1W  {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, <mod>]                               SVE_HW_4A_B         110001010h0mmmmm 011gggnnnnnttttt     C500 6000   
    // LDFF1W  {<Zt>.S }, <Pg>/Z, [<Xn|SP>, <Zm>.S, <mod>]                               SVE_HW_4A_C         100001010h0mmmmm 011gggnnnnnttttt     8500 6000   
    // LDFF1W  {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, LSL #2]                              SVE_HW_4B           11000101011mmmmm 111gggnnnnnttttt     C560 E000   
    // LDFF1W  {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D]                                      SVE_HW_4B_D         11000101010mmmmm 111gggnnnnnttttt     C540 E000   
    // LDFF1W  {<Zt>.D }, <Pg>/Z, [<Zn>.D{, #<imm>}]                                     SVE_HX_3A_E         10000101001iiiii 111gggnnnnnttttt     8520 E000   
    // LDFF1W  {<Zt>.S }, <Pg>/Z, [<Xn|SP>{, <Xm>, LSL #2}]                              SVE_IG_4A_F         10100101010mmmmm 011gggnnnnnttttt     A540 6000   


//    enum               name                     info                                              SVE_HW_4A        SVE_HW_4A_A      SVE_HW_4A_B      SVE_HW_4A_C      SVE_HW_4B        SVE_HW_4B_D      SVE_HX_3A_E      SVE_IG_4A_G      
INST8(ldff1h,            "ldff1h",                0,                       IF_SVE_8C,               0x84A06000,      0xC4A06000,      0xC4806000,      0x84806000,      0xC4E0E000,      0xC4C0E000,      0x84A0E000,      0xA4806000       )
    // LDFF1H  {<Zt>.S }, <Pg>/Z, [<Xn|SP>, <Zm>.S, <mod> #1]                            SVE_HW_4A           100001001h1mmmmm 011gggnnnnnttttt     84A0 6000   
    // LDFF1H  {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, <mod> #1]                            SVE_HW_4A_A         110001001h1mmmmm 011gggnnnnnttttt     C4A0 6000   
    // LDFF1H  {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, <mod>]                               SVE_HW_4A_B         110001001h0mmmmm 011gggnnnnnttttt     C480 6000   
    // LDFF1H  {<Zt>.S }, <Pg>/Z, [<Xn|SP>, <Zm>.S, <mod>]                               SVE_HW_4A_C         100001001h0mmmmm 011gggnnnnnttttt     8480 6000   
    // LDFF1H  {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, LSL #1]                              SVE_HW_4B           11000100111mmmmm 111gggnnnnnttttt     C4E0 E000   
    // LDFF1H  {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D]                                      SVE_HW_4B_D         11000100110mmmmm 111gggnnnnnttttt     C4C0 E000   
    // LDFF1H  {<Zt>.D }, <Pg>/Z, [<Zn>.D{, #<imm>}]                                     SVE_HX_3A_E         10000100101iiiii 111gggnnnnnttttt     84A0 E000   
    // LDFF1H  {<Zt>.X }, <Pg>/Z, [<Xn|SP>{, <Xm>, LSL #1}]                              SVE_IG_4A_G         10100100100mmmmm 011gggnnnnnttttt     A480 6000   


//    enum               name                     info                                              SVE_IJ_3A        SVE_IK_4A        SVE_IU_4A        SVE_IU_4A_A      SVE_IU_4B        SVE_IU_4B_B      SVE_IV_3A        
INST7(ld1sw,             "ld1sw",                 0,                       IF_SVE_7A,               0xA480A000,      0xA4804000,      0xC5200000,      0xC5000000,      0xC5608000,      0xC5408000,      0xC5208000       )
    // LD1SW   {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]                            SVE_IJ_3A           101001001000iiii 101gggnnnnnttttt     A480 A000   
    // LD1SW   {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #2]                                SVE_IK_4A           10100100100mmmmm 010gggnnnnnttttt     A480 4000   
    // LD1SW   {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, <mod> #2]                            SVE_IU_4A           110001010h1mmmmm 000gggnnnnnttttt     C520 0000   
    // LD1SW   {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, <mod>]                               SVE_IU_4A_A         110001010h0mmmmm 000gggnnnnnttttt     C500 0000   
    // LD1SW   {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, LSL #2]                              SVE_IU_4B           11000101011mmmmm 100gggnnnnnttttt     C560 8000   
    // LD1SW   {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D]                                      SVE_IU_4B_B         11000101010mmmmm 100gggnnnnnttttt     C540 8000   
    // LD1SW   {<Zt>.D }, <Pg>/Z, [<Zn>.D{, #<imm>}]                                     SVE_IV_3A           11000101001iiiii 100gggnnnnnttttt     C520 8000   


//    enum               name                     info                                              SVE_AE_3A        SVE_BD_3A        SVE_EE_1A        SVE_FD_3A        SVE_FD_3B        SVE_FD_3C        
INST6(mul,               "mul",                   0,                       IF_SVE_6A,               0x04100000,      0x04206000,      0x2530C000,      0x4420F800,      0x44A0F800,      0x44E0F800       )
    // MUL     <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_AE_3A           00000100xx010000 000gggmmmmmddddd     0410 0000   
    // MUL     <Zd>.<T>, <Zn>.<T>, <Zm>.<T>                                              SVE_BD_3A           00000100xx1mmmmm 011000nnnnnddddd     0420 6000   
    // MUL     <Zdn>.<T>, <Zdn>.<T>, #<imm>                                              SVE_EE_1A           00100101xx110000 110iiiiiiiiddddd     2530 C000   
    // MUL     <Zd>.H, <Zn>.H, <Zm>.H[<imm>]                                             SVE_FD_3A           010001000i1iimmm 111110nnnnnddddd     4420 F800   
    // MUL     <Zd>.S, <Zn>.S, <Zm>.S[<imm>]                                             SVE_FD_3B           01000100101iimmm 111110nnnnnddddd     44A0 F800   
    // MUL     <Zd>.D, <Zn>.D, <Zm>.D[<imm>]                                             SVE_FD_3C           01000100111immmm 111110nnnnnddddd     44E0 F800   


//    enum               name                     info                                              SVE_GY_3A        SVE_GY_3B        SVE_GY_3B_D      SVE_HA_3A        SVE_HA_3A_E      SVE_HA_3A_F      
INST6(fdot,              "fdot",                  0,                       IF_SVE_6B,               0x64204400,      0x64204000,      0x64604400,      0x64208000,      0x64208400,      0x64608400       )
    // FDOT    <Zda>.H, <Zn>.B, <Zm>.B[<imm>]                                            SVE_GY_3A           01100100001iimmm 0100i1nnnnnddddd     6420 4400   
    // FDOT    <Zda>.S, <Zn>.H, <Zm>.H[<imm>]                                            SVE_GY_3B           01100100001iimmm 010000nnnnnddddd     6420 4000   
    // FDOT    <Zda>.S, <Zn>.B, <Zm>.B[<imm>]                                            SVE_GY_3B_D         01100100011iimmm 010001nnnnnddddd     6460 4400   
    // FDOT    <Zda>.S, <Zn>.H, <Zm>.H                                                   SVE_HA_3A           01100100001mmmmm 100000nnnnnddddd     6420 8000   
    // FDOT    <Zda>.H, <Zn>.B, <Zm>.B                                                   SVE_HA_3A_E         01100100001mmmmm 100001nnnnnddddd     6420 8400   
    // FDOT    <Zda>.S, <Zn>.B, <Zm>.B                                                   SVE_HA_3A_F         01100100011mmmmm 100001nnnnnddddd     6460 8400   


//    enum               name                     info                                              SVE_HW_4A        SVE_HW_4A_A      SVE_HW_4B        SVE_HX_3A_B      SVE_IJ_3A_D      SVE_IK_4A_F      
INST6(ld1sb,             "ld1sb",                 0,                       IF_SVE_6C,               0xC4000000,      0x84000000,      0xC4408000,      0x84208000,      0xA580A000,      0xA5804000       )
    // LD1SB   {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, <mod>]                               SVE_HW_4A           110001000h0mmmmm 000gggnnnnnttttt     C400 0000   
    // LD1SB   {<Zt>.S }, <Pg>/Z, [<Xn|SP>, <Zm>.S, <mod>]                               SVE_HW_4A_A         100001000h0mmmmm 000gggnnnnnttttt     8400 0000   
    // LD1SB   {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D]                                      SVE_HW_4B           11000100010mmmmm 100gggnnnnnttttt     C440 8000   
    // LD1SB   {<Zt>.D }, <Pg>/Z, [<Zn>.D{, #<imm>}]                                     SVE_HX_3A_B         10000100001iiiii 100gggnnnnnttttt     8420 8000   
    // LD1SB   {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]                            SVE_IJ_3A_D         101001011000iiii 101gggnnnnnttttt     A580 A000   
    // LD1SB   {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Xm>]                                        SVE_IK_4A_F         10100101100mmmmm 010gggnnnnnttttt     A580 4000   


//    enum               name                     info                                              SVE_HW_4A        SVE_HW_4A_A      SVE_HW_4B        SVE_HX_3A_B      SVE_IJ_3A_E      SVE_IK_4A_H      
INST6(ld1b,              "ld1b",                  0,                       IF_SVE_6D,               0xC4004000,      0x84004000,      0xC440C000,      0x8420C000,      0xA400A000,      0xA4004000       )
    // LD1B    {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, <mod>]                               SVE_HW_4A           110001000h0mmmmm 010gggnnnnnttttt     C400 4000   
    // LD1B    {<Zt>.S }, <Pg>/Z, [<Xn|SP>, <Zm>.S, <mod>]                               SVE_HW_4A_A         100001000h0mmmmm 010gggnnnnnttttt     8400 4000   
    // LD1B    {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D]                                      SVE_HW_4B           11000100010mmmmm 110gggnnnnnttttt     C440 C000   
    // LD1B    {<Zt>.D }, <Pg>/Z, [<Zn>.D{, #<imm>}]                                     SVE_HX_3A_B         10000100001iiiii 110gggnnnnnttttt     8420 C000   
    // LD1B    {<Zt>.B }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]                            SVE_IJ_3A_E         101001000000iiii 101gggnnnnnttttt     A400 A000   
    // LD1B    {<Zt>.B }, <Pg>/Z, [<Xn|SP>, <Xm>]                                        SVE_IK_4A_H         10100100000mmmmm 010gggnnnnnttttt     A400 4000   


//    enum               name                     info                                              SVE_HY_3A        SVE_HY_3A_A      SVE_HY_3B        SVE_HZ_2A_B      SVE_IA_2A        SVE_IB_3A        
INST6(prfb,              "prfb",                  0,                       IF_SVE_6E,               0x84200000,      0xC4200000,      0xC4608000,      0x8400E000,      0x85C00000,      0x8400C000       )
    // PRFB    <prfop>, <Pg>, [<Xn|SP>, <Zm>.S, <mod>]                                   SVE_HY_3A           100001000h1mmmmm 000gggnnnnn0oooo     8420 0000   
    // PRFB    <prfop>, <Pg>, [<Xn|SP>, <Zm>.D, <mod>]                                   SVE_HY_3A_A         110001000h1mmmmm 000gggnnnnn0oooo     C420 0000   
    // PRFB    <prfop>, <Pg>, [<Xn|SP>, <Zm>.D]                                          SVE_HY_3B           11000100011mmmmm 100gggnnnnn0oooo     C460 8000   
    // PRFB    <prfop>, <Pg>, [<Zn>.S{, #<imm>}]                                         SVE_HZ_2A_B         10000100000iiiii 111gggnnnnn0oooo     8400 E000   
    // PRFB    <prfop>, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]                                SVE_IA_2A           1000010111iiiiii 000gggnnnnn0oooo     85C0 0000   
    // PRFB    <prfop>, <Pg>, [<Xn|SP>, <Xm>]                                            SVE_IB_3A           10000100000mmmmm 110gggnnnnn0oooo     8400 C000   

INST6(prfd,              "prfd",                  0,                       IF_SVE_6E,               0x84206000,      0xC4206000,      0xC460E000,      0x8580E000,      0x85C06000,      0x8580C000       )
    // PRFD    <prfop>, <Pg>, [<Xn|SP>, <Zm>.S, <mod> #3]                                SVE_HY_3A           100001000h1mmmmm 011gggnnnnn0oooo     8420 6000   
    // PRFD    <prfop>, <Pg>, [<Xn|SP>, <Zm>.D, <mod> #3]                                SVE_HY_3A_A         110001000h1mmmmm 011gggnnnnn0oooo     C420 6000   
    // PRFD    <prfop>, <Pg>, [<Xn|SP>, <Zm>.D, LSL #3]                                  SVE_HY_3B           11000100011mmmmm 111gggnnnnn0oooo     C460 E000   
    // PRFD    <prfop>, <Pg>, [<Zn>.S{, #<imm>}]                                         SVE_HZ_2A_B         10000101100iiiii 111gggnnnnn0oooo     8580 E000   
    // PRFD    <prfop>, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]                                SVE_IA_2A           1000010111iiiiii 011gggnnnnn0oooo     85C0 6000   
    // PRFD    <prfop>, <Pg>, [<Xn|SP>, <Xm>, LSL #3]                                    SVE_IB_3A           10000101100mmmmm 110gggnnnnn0oooo     8580 C000   

INST6(prfh,              "prfh",                  0,                       IF_SVE_6E,               0x84202000,      0xC4202000,      0xC460A000,      0x8480E000,      0x85C02000,      0x8480C000       )
    // PRFH    <prfop>, <Pg>, [<Xn|SP>, <Zm>.S, <mod> #1]                                SVE_HY_3A           100001000h1mmmmm 001gggnnnnn0oooo     8420 2000   
    // PRFH    <prfop>, <Pg>, [<Xn|SP>, <Zm>.D, <mod> #1]                                SVE_HY_3A_A         110001000h1mmmmm 001gggnnnnn0oooo     C420 2000   
    // PRFH    <prfop>, <Pg>, [<Xn|SP>, <Zm>.D, LSL #1]                                  SVE_HY_3B           11000100011mmmmm 101gggnnnnn0oooo     C460 A000   
    // PRFH    <prfop>, <Pg>, [<Zn>.S{, #<imm>}]                                         SVE_HZ_2A_B         10000100100iiiii 111gggnnnnn0oooo     8480 E000   
    // PRFH    <prfop>, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]                                SVE_IA_2A           1000010111iiiiii 001gggnnnnn0oooo     85C0 2000   
    // PRFH    <prfop>, <Pg>, [<Xn|SP>, <Xm>, LSL #1]                                    SVE_IB_3A           10000100100mmmmm 110gggnnnnn0oooo     8480 C000   

INST6(prfw,              "prfw",                  0,                       IF_SVE_6E,               0x84204000,      0xC4204000,      0xC460C000,      0x8500E000,      0x85C04000,      0x8500C000       )
    // PRFW    <prfop>, <Pg>, [<Xn|SP>, <Zm>.S, <mod> #2]                                SVE_HY_3A           100001000h1mmmmm 010gggnnnnn0oooo     8420 4000   
    // PRFW    <prfop>, <Pg>, [<Xn|SP>, <Zm>.D, <mod> #2]                                SVE_HY_3A_A         110001000h1mmmmm 010gggnnnnn0oooo     C420 4000   
    // PRFW    <prfop>, <Pg>, [<Xn|SP>, <Zm>.D, LSL #2]                                  SVE_HY_3B           11000100011mmmmm 110gggnnnnn0oooo     C460 C000   
    // PRFW    <prfop>, <Pg>, [<Zn>.S{, #<imm>}]                                         SVE_HZ_2A_B         10000101000iiiii 111gggnnnnn0oooo     8500 E000   
    // PRFW    <prfop>, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]                                SVE_IA_2A           1000010111iiiiii 010gggnnnnn0oooo     85C0 4000   
    // PRFW    <prfop>, <Pg>, [<Xn|SP>, <Xm>, LSL #2]                                    SVE_IB_3A           10000101000mmmmm 110gggnnnnn0oooo     8500 C000   


//    enum               name                     info                                              SVE_IG_4A        SVE_IU_4A        SVE_IU_4A_A      SVE_IU_4B        SVE_IU_4B_B      SVE_IV_3A        
INST6(ldff1d,            "ldff1d",                0,                       IF_SVE_6F,               0xA5E06000,      0xC5A06000,      0xC5806000,      0xC5E0E000,      0xC5C0E000,      0xC5A0E000       )
    // LDFF1D  {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, <Xm>, LSL #3}]                              SVE_IG_4A           10100101111mmmmm 011gggnnnnnttttt     A5E0 6000   
    // LDFF1D  {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, <mod> #3]                            SVE_IU_4A           110001011h1mmmmm 011gggnnnnnttttt     C5A0 6000   
    // LDFF1D  {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, <mod>]                               SVE_IU_4A_A         110001011h0mmmmm 011gggnnnnnttttt     C580 6000   
    // LDFF1D  {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, LSL #3]                              SVE_IU_4B           11000101111mmmmm 111gggnnnnnttttt     C5E0 E000   
    // LDFF1D  {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D]                                      SVE_IU_4B_B         11000101110mmmmm 111gggnnnnnttttt     C5C0 E000   
    // LDFF1D  {<Zt>.D }, <Pg>/Z, [<Zn>.D{, #<imm>}]                                     SVE_IV_3A           11000101101iiiii 111gggnnnnnttttt     C5A0 E000   

INST6(ldff1sw,           "ldff1sw",               0,                       IF_SVE_6F,               0xA4806000,      0xC5202000,      0xC5002000,      0xC560A000,      0xC540A000,      0xC520A000       )
    // LDFF1SW {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, <Xm>, LSL #2}]                              SVE_IG_4A           10100100100mmmmm 011gggnnnnnttttt     A480 6000   
    // LDFF1SW {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, <mod> #2]                            SVE_IU_4A           110001010h1mmmmm 001gggnnnnnttttt     C520 2000   
    // LDFF1SW {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, <mod>]                               SVE_IU_4A_A         110001010h0mmmmm 001gggnnnnnttttt     C500 2000   
    // LDFF1SW {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, LSL #2]                              SVE_IU_4B           11000101011mmmmm 101gggnnnnnttttt     C560 A000   
    // LDFF1SW {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D]                                      SVE_IU_4B_B         11000101010mmmmm 101gggnnnnnttttt     C540 A000   
    // LDFF1SW {<Zt>.D }, <Pg>/Z, [<Zn>.D{, #<imm>}]                                     SVE_IV_3A           11000101001iiiii 101gggnnnnnttttt     C520 A000   


//    enum               name                     info                                              SVE_JD_4A        SVE_JI_3A_A      SVE_JK_4A        SVE_JK_4A_B      SVE_JK_4B        SVE_JN_3A        
INST6(st1b,              "st1b",                  0,                       IF_SVE_6G,               0xE4004000,      0xE440A000,      0xE4008000,      0xE4408000,      0xE400A000,      0xE400E000       )
    // ST1B    {<Zt>.<T>}, <Pg>, [<Xn|SP>, <Xm>]                                         SVE_JD_4A           111001000xxmmmmm 010gggnnnnnttttt     E400 4000   
    // ST1B    {<Zt>.D }, <Pg>, [<Zn>.D{, #<imm>}]                                       SVE_JI_3A_A         11100100010iiiii 101gggnnnnnttttt     E440 A000   
    // ST1B    {<Zt>.D }, <Pg>, [<Xn|SP>, <Zm>.D, <mod>]                                 SVE_JK_4A           11100100000mmmmm 1h0gggnnnnnttttt     E400 8000   
    // ST1B    {<Zt>.S }, <Pg>, [<Xn|SP>, <Zm>.S, <mod>]                                 SVE_JK_4A_B         11100100010mmmmm 1h0gggnnnnnttttt     E440 8000   
    // ST1B    {<Zt>.D }, <Pg>, [<Xn|SP>, <Zm>.D]                                        SVE_JK_4B           11100100000mmmmm 101gggnnnnnttttt     E400 A000   
    // ST1B    {<Zt>.<T>}, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]                             SVE_JN_3A           111001000xx0iiii 111gggnnnnnttttt     E400 E000   


//    enum               name                     info                                              SVE_AM_2A        SVE_AN_3A        SVE_AO_3A        SVE_BF_2A        SVE_BG_3A        
INST5(asr,               "asr",                   RSH,                     IF_SVE_5A,               0x04008000,      0x04108000,      0x04188000,      0x04209000,      0x04208000       )
    // ASR     <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, #<const>                                    SVE_AM_2A           00000100xx000000 100gggxxiiiddddd     0400 8000   
    // ASR     <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_AN_3A           00000100xx010000 100gggmmmmmddddd     0410 8000   
    // ASR     <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.D                                      SVE_AO_3A           00000100xx011000 100gggmmmmmddddd     0418 8000   
    // ASR     <Zd>.<T>, <Zn>.<T>, #<const>                                              SVE_BF_2A           00000100xx1xxiii 100100nnnnnddddd     0420 9000   
    // ASR     <Zd>.<T>, <Zn>.<T>, <Zm>.D                                                SVE_BG_3A           00000100xx1mmmmm 100000nnnnnddddd     0420 8000   

INST5(lsl,               "lsl",                   0,                       IF_SVE_5A,               0x04038000,      0x04138000,      0x041B8000,      0x04209C00,      0x04208C00       )
    // LSL     <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, #<const>                                    SVE_AM_2A           00000100xx000011 100gggxxiiiddddd     0403 8000   
    // LSL     <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_AN_3A           00000100xx010011 100gggmmmmmddddd     0413 8000   
    // LSL     <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.D                                      SVE_AO_3A           00000100xx011011 100gggmmmmmddddd     041B 8000   
    // LSL     <Zd>.<T>, <Zn>.<T>, #<const>                                              SVE_BF_2A           00000100xx1xxiii 100111nnnnnddddd     0420 9C00   
    // LSL     <Zd>.<T>, <Zn>.<T>, <Zm>.D                                                SVE_BG_3A           00000100xx1mmmmm 100011nnnnnddddd     0420 8C00   

INST5(lsr,               "lsr",                   RSH,                     IF_SVE_5A,               0x04018000,      0x04118000,      0x04198000,      0x04209400,      0x04208400       )
    // LSR     <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, #<const>                                    SVE_AM_2A           00000100xx000001 100gggxxiiiddddd     0401 8000   
    // LSR     <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_AN_3A           00000100xx010001 100gggmmmmmddddd     0411 8000   
    // LSR     <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.D                                      SVE_AO_3A           00000100xx011001 100gggmmmmmddddd     0419 8000   
    // LSR     <Zd>.<T>, <Zn>.<T>, #<const>                                              SVE_BF_2A           00000100xx1xxiii 100101nnnnnddddd     0420 9400   
    // LSR     <Zd>.<T>, <Zn>.<T>, <Zm>.D                                                SVE_BG_3A           00000100xx1mmmmm 100001nnnnnddddd     0420 8400   


//    enum               name                     info                                              SVE_GX_3A        SVE_GX_3B        SVE_HK_3A        SVE_HL_3A        SVE_HM_2A        
INST5(fmul,              "fmul",                  0,                       IF_SVE_5B,               0x64A02000,      0x64E02000,      0x65000800,      0x65028000,      0x651A8000       )
    // FMUL    <Zd>.S, <Zn>.S, <Zm>.S[<imm>]                                             SVE_GX_3A           01100100101iimmm 001000nnnnnddddd     64A0 2000   
    // FMUL    <Zd>.D, <Zn>.D, <Zm>.D[<imm>]                                             SVE_GX_3B           01100100111immmm 001000nnnnnddddd     64E0 2000   
    // FMUL    <Zd>.<T>, <Zn>.<T>, <Zm>.<T>                                              SVE_HK_3A           01100101xx0mmmmm 000010nnnnnddddd     6500 0800   
    // FMUL    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_HL_3A           01100101xx000010 100gggmmmmmddddd     6502 8000   
    // FMUL    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <const>                                     SVE_HM_2A           01100101xx011010 100ggg0000iddddd     651A 8000   


//    enum               name                     info                                              SVE_EF_3A        SVE_EG_3A        SVE_EH_3A        SVE_EY_3A        SVE_EY_3B        
INST5(sdot,              "sdot",                  0,                       IF_SVE_5C,               0x4400C800,      0x4480C800,      0x44000000,      0x44A00000,      0x44E00000       )
    // SDOT    <Zda>.S, <Zn>.H, <Zm>.H                                                   SVE_EF_3A           01000100000mmmmm 110010nnnnnddddd     4400 C800   
    // SDOT    <Zda>.S, <Zn>.H, <Zm>.H[<imm>]                                            SVE_EG_3A           01000100100iimmm 110010nnnnnddddd     4480 C800   
    // SDOT    <Zda>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                           SVE_EH_3A           01000100xx0mmmmm 000000nnnnnddddd     4400 0000   
    // SDOT    <Zda>.S, <Zn>.B, <Zm>.B[<imm>]                                            SVE_EY_3A           01000100101iimmm 000000nnnnnddddd     44A0 0000   
    // SDOT    <Zda>.D, <Zn>.H, <Zm>.H[<imm>]                                            SVE_EY_3B           01000100111immmm 000000nnnnnddddd     44E0 0000   

INST5(udot,              "udot",                  0,                       IF_SVE_5C,               0x4400CC00,      0x4480CC00,      0x44000400,      0x44A00400,      0x44E00400       )
    // UDOT    <Zda>.S, <Zn>.H, <Zm>.H                                                   SVE_EF_3A           01000100000mmmmm 110011nnnnnddddd     4400 CC00   
    // UDOT    <Zda>.S, <Zn>.H, <Zm>.H[<imm>]                                            SVE_EG_3A           01000100100iimmm 110011nnnnnddddd     4480 CC00   
    // UDOT    <Zda>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                           SVE_EH_3A           01000100xx0mmmmm 000001nnnnnddddd     4400 0400   
    // UDOT    <Zda>.S, <Zn>.B, <Zm>.B[<imm>]                                            SVE_EY_3A           01000100101iimmm 000001nnnnnddddd     44A0 0400   
    // UDOT    <Zda>.D, <Zn>.H, <Zm>.H[<imm>]                                            SVE_EY_3B           01000100111immmm 000001nnnnnddddd     44E0 0400   


//    enum               name                     info                                              SVE_HW_4A        SVE_HW_4A_A      SVE_HW_4B        SVE_HX_3A_B      SVE_IG_4A_D      
INST5(ldff1sb,           "ldff1sb",               0,                       IF_SVE_5D,               0xC4002000,      0x84002000,      0xC440A000,      0x8420A000,      0xA5806000       )
    // LDFF1SB {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, <mod>]                               SVE_HW_4A           110001000h0mmmmm 001gggnnnnnttttt     C400 2000   
    // LDFF1SB {<Zt>.S }, <Pg>/Z, [<Xn|SP>, <Zm>.S, <mod>]                               SVE_HW_4A_A         100001000h0mmmmm 001gggnnnnnttttt     8400 2000   
    // LDFF1SB {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D]                                      SVE_HW_4B           11000100010mmmmm 101gggnnnnnttttt     C440 A000   
    // LDFF1SB {<Zt>.D }, <Pg>/Z, [<Zn>.D{, #<imm>}]                                     SVE_HX_3A_B         10000100001iiiii 101gggnnnnnttttt     8420 A000   
    // LDFF1SB {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, <Xm>}]                                      SVE_IG_4A_D         10100101100mmmmm 011gggnnnnnttttt     A580 6000   


//    enum               name                     info                                              SVE_HW_4A        SVE_HW_4A_A      SVE_HW_4B        SVE_HX_3A_B      SVE_IG_4A_E      
INST5(ldff1b,            "ldff1b",                0,                       IF_SVE_5E,               0xC4006000,      0x84006000,      0xC440E000,      0x8420E000,      0xA4006000       )
    // LDFF1B  {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, <mod>]                               SVE_HW_4A           110001000h0mmmmm 011gggnnnnnttttt     C400 6000   
    // LDFF1B  {<Zt>.S }, <Pg>/Z, [<Xn|SP>, <Zm>.S, <mod>]                               SVE_HW_4A_A         100001000h0mmmmm 011gggnnnnnttttt     8400 6000   
    // LDFF1B  {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D]                                      SVE_HW_4B           11000100010mmmmm 111gggnnnnnttttt     C440 E000   
    // LDFF1B  {<Zt>.D }, <Pg>/Z, [<Zn>.D{, #<imm>}]                                     SVE_HX_3A_B         10000100001iiiii 111gggnnnnnttttt     8420 E000   
    // LDFF1B  {<Zt>.B }, <Pg>/Z, [<Xn|SP>{, <Xm>}]                                      SVE_IG_4A_E         10100100000mmmmm 011gggnnnnnttttt     A400 6000   


//    enum               name                     info                                              SVE_AA_3A        SVE_AU_3A        SVE_BS_1A        SVE_CZ_4A        
INST4(and,               "and",                   0,                       IF_SVE_4A,               0x041A0000,      0x04203000,      0x05800000,      0x25004000       )
    // AND     <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_AA_3A           00000100xx011010 000gggmmmmmddddd     041A 0000   
    // AND     <Zd>.D, <Zn>.D, <Zm>.D                                                    SVE_AU_3A           00000100001mmmmm 001100nnnnnddddd     0420 3000   
    // AND     <Zdn>.<T>, <Zdn>.<T>, #<const>                                            SVE_BS_1A           00000101100000ii iiiiiiiiiiiddddd     0580 0000   
    // AND     <Pd>.B, <Pg>/Z, <Pn>.B, <Pm>.B                                            SVE_CZ_4A           001001010000MMMM 01gggg0NNNN0DDDD     2500 4000   

INST4(bic,               "bic",                   0,                       IF_SVE_4A,               0x041B0000,      0x04E03000,      0x05800000,      0x25004010       )
    // BIC     <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_AA_3A           00000100xx011011 000gggmmmmmddddd     041B 0000   
    // BIC     <Zd>.D, <Zn>.D, <Zm>.D                                                    SVE_AU_3A           00000100111mmmmm 001100nnnnnddddd     04E0 3000   
    // BIC     <Zdn>.<T>, <Zdn>.<T>, #<const>                                            SVE_BS_1A           00000101100000ii iiiiiiiiiiiddddd     0580 0000   
    // BIC     <Pd>.B, <Pg>/Z, <Pn>.B, <Pm>.B                                            SVE_CZ_4A           001001010000MMMM 01gggg0NNNN1DDDD     2500 4010   

INST4(eor,               "eor",                   0,                       IF_SVE_4A,               0x04190000,      0x04A03000,      0x05400000,      0x25004200       )
    // EOR     <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_AA_3A           00000100xx011001 000gggmmmmmddddd     0419 0000   
    // EOR     <Zd>.D, <Zn>.D, <Zm>.D                                                    SVE_AU_3A           00000100101mmmmm 001100nnnnnddddd     04A0 3000   
    // EOR     <Zdn>.<T>, <Zdn>.<T>, #<const>                                            SVE_BS_1A           00000101010000ii iiiiiiiiiiiddddd     0540 0000   
    // EOR     <Pd>.B, <Pg>/Z, <Pn>.B, <Pm>.B                                            SVE_CZ_4A           001001010000MMMM 01gggg1NNNN0DDDD     2500 4200   

INST4(orr,               "orr",                   0,                       IF_SVE_4A,               0x04180000,      0x04603000,      0x05000000,      0x25804000       )
    // ORR     <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_AA_3A           00000100xx011000 000gggmmmmmddddd     0418 0000   
    // ORR     <Zd>.D, <Zn>.D, <Zm>.D                                                    SVE_AU_3A           00000100011mmmmm 001100nnnnnddddd     0460 3000   
    // ORR     <Zdn>.<T>, <Zdn>.<T>, #<const>                                            SVE_BS_1A           00000101000000ii iiiiiiiiiiiddddd     0500 0000   
    // ORR     <Pd>.B, <Pg>/Z, <Pn>.B, <Pm>.B                                            SVE_CZ_4A           001001011000MMMM 01gggg0NNNN0DDDD     2580 4000   


//    enum               name                     info                                              SVE_BU_2A        SVE_BV_2B        SVE_EA_1A        SVE_EB_1B        
INST4(fmov,              "fmov",                  0,                       IF_SVE_4B,               0x0510C000,      0x05104000,      0x2539C000,      0x2538C000       )
    // FMOV    <Zd>.<T>, <Pg>/M, #<const>                                                SVE_BU_2A           00000101xx01gggg 110iiiiiiiiddddd     0510 C000   
    // FMOV    <Zd>.<T>, <Pg>/M, #0.0                                                    SVE_BV_2B           00000101xx01gggg 01000000000ddddd     0510 4000   
    // FMOV    <Zd>.<T>, #<const>                                                        SVE_EA_1A           00100101xx111001 110iiiiiiiiddddd     2539 C000   
    // FMOV    <Zd>.<T>, #0.0                                                            SVE_EB_1B           00100101xx111000 11000000000ddddd     2538 C000   


//    enum               name                     info                                              SVE_HS_3A        SVE_HS_3A_H      SVE_HS_3A_I      SVE_HS_3A_J      
INST4(scvtf,             "scvtf",                 0,                       IF_SVE_4C,               0x6594A000,      0x65D0A000,      0x65D4A000,      0x65D6A000       )
    // SCVTF   <Zd>.S, <Pg>/M, <Zn>.S                                                    SVE_HS_3A           0110010110010100 101gggnnnnnddddd     6594 A000   
    // SCVTF   <Zd>.D, <Pg>/M, <Zn>.S                                                    SVE_HS_3A_H         0110010111010000 101gggnnnnnddddd     65D0 A000   
    // SCVTF   <Zd>.S, <Pg>/M, <Zn>.D                                                    SVE_HS_3A_I         0110010111010100 101gggnnnnnddddd     65D4 A000   
    // SCVTF   <Zd>.D, <Pg>/M, <Zn>.D                                                    SVE_HS_3A_J         0110010111010110 101gggnnnnnddddd     65D6 A000   

INST4(ucvtf,             "ucvtf",                 0,                       IF_SVE_4C,               0x6595A000,      0x65D1A000,      0x65D5A000,      0x65D7A000       )
    // UCVTF   <Zd>.S, <Pg>/M, <Zn>.S                                                    SVE_HS_3A           0110010110010101 101gggnnnnnddddd     6595 A000   
    // UCVTF   <Zd>.D, <Pg>/M, <Zn>.S                                                    SVE_HS_3A_H         0110010111010001 101gggnnnnnddddd     65D1 A000   
    // UCVTF   <Zd>.S, <Pg>/M, <Zn>.D                                                    SVE_HS_3A_I         0110010111010101 101gggnnnnnddddd     65D5 A000   
    // UCVTF   <Zd>.D, <Pg>/M, <Zn>.D                                                    SVE_HS_3A_J         0110010111010111 101gggnnnnnddddd     65D7 A000   


//    enum               name                     info                                              SVE_HP_3B        SVE_HP_3B_H      SVE_HP_3B_I      SVE_HP_3B_J      
INST4(fcvtzs,            "fcvtzs",                0,                       IF_SVE_4D,               0x659CA000,      0x65DCA000,      0x65D8A000,      0x65DEA000       )
    // FCVTZS  <Zd>.S, <Pg>/M, <Zn>.S                                                    SVE_HP_3B           0110010110011100 101gggnnnnnddddd     659C A000   
    // FCVTZS  <Zd>.D, <Pg>/M, <Zn>.S                                                    SVE_HP_3B_H         0110010111011100 101gggnnnnnddddd     65DC A000   
    // FCVTZS  <Zd>.S, <Pg>/M, <Zn>.D                                                    SVE_HP_3B_I         0110010111011000 101gggnnnnnddddd     65D8 A000   
    // FCVTZS  <Zd>.D, <Pg>/M, <Zn>.D                                                    SVE_HP_3B_J         0110010111011110 101gggnnnnnddddd     65DE A000   

INST4(fcvtzu,            "fcvtzu",                0,                       IF_SVE_4D,               0x659DA000,      0x65DDA000,      0x65D9A000,      0x65DFA000       )
    // FCVTZU  <Zd>.S, <Pg>/M, <Zn>.S                                                    SVE_HP_3B           0110010110011101 101gggnnnnnddddd     659D A000   
    // FCVTZU  <Zd>.D, <Pg>/M, <Zn>.S                                                    SVE_HP_3B_H         0110010111011101 101gggnnnnnddddd     65DD A000   
    // FCVTZU  <Zd>.S, <Pg>/M, <Zn>.D                                                    SVE_HP_3B_I         0110010111011001 101gggnnnnnddddd     65D9 A000   
    // FCVTZU  <Zd>.D, <Pg>/M, <Zn>.D                                                    SVE_HP_3B_J         0110010111011111 101gggnnnnnddddd     65DF A000   


//    enum               name                     info                                              SVE_BE_3A        SVE_FI_3A        SVE_FI_3B        SVE_FI_3C        
INST4(sqdmulh,           "sqdmulh",               0,                       IF_SVE_4E,               0x04207000,      0x4420F000,      0x44A0F000,      0x44E0F000       )
    // SQDMULH <Zd>.<T>, <Zn>.<T>, <Zm>.<T>                                              SVE_BE_3A           00000100xx1mmmmm 011100nnnnnddddd     0420 7000   
    // SQDMULH <Zd>.H, <Zn>.H, <Zm>.H[<imm>]                                             SVE_FI_3A           010001000i1iimmm 111100nnnnnddddd     4420 F000   
    // SQDMULH <Zd>.S, <Zn>.S, <Zm>.S[<imm>]                                             SVE_FI_3B           01000100101iimmm 111100nnnnnddddd     44A0 F000   
    // SQDMULH <Zd>.D, <Zn>.D, <Zm>.D[<imm>]                                             SVE_FI_3C           01000100111immmm 111100nnnnnddddd     44E0 F000   

INST4(sqrdmulh,          "sqrdmulh",              0,                       IF_SVE_4E,               0x04207400,      0x4420F400,      0x44A0F400,      0x44E0F400       )
    // SQRDMULH <Zd>.<T>, <Zn>.<T>, <Zm>.<T>                                             SVE_BE_3A           00000100xx1mmmmm 011101nnnnnddddd     0420 7400   
    // SQRDMULH <Zd>.H, <Zn>.H, <Zm>.H[<imm>]                                            SVE_FI_3A           010001000i1iimmm 111101nnnnnddddd     4420 F400   
    // SQRDMULH <Zd>.S, <Zn>.S, <Zm>.S[<imm>]                                            SVE_FI_3B           01000100101iimmm 111101nnnnnddddd     44A0 F400   
    // SQRDMULH <Zd>.D, <Zn>.D, <Zm>.D[<imm>]                                            SVE_FI_3C           01000100111immmm 111101nnnnnddddd     44E0 F400   


//    enum               name                     info                                              SVE_EM_3A        SVE_FK_3A        SVE_FK_3B        SVE_FK_3C        
INST4(sqrdmlah,          "sqrdmlah",              0,                       IF_SVE_4F,               0x44007000,      0x44201000,      0x44A01000,      0x44E01000       )
    // SQRDMLAH <Zda>.<T>, <Zn>.<T>, <Zm>.<T>                                            SVE_EM_3A           01000100xx0mmmmm 011100nnnnnddddd     4400 7000   
    // SQRDMLAH <Zda>.H, <Zn>.H, <Zm>.H[<imm>]                                           SVE_FK_3A           010001000i1iimmm 000100nnnnnddddd     4420 1000   
    // SQRDMLAH <Zda>.S, <Zn>.S, <Zm>.S[<imm>]                                           SVE_FK_3B           01000100101iimmm 000100nnnnnddddd     44A0 1000   
    // SQRDMLAH <Zda>.D, <Zn>.D, <Zm>.D[<imm>]                                           SVE_FK_3C           01000100111immmm 000100nnnnnddddd     44E0 1000   

INST4(sqrdmlsh,          "sqrdmlsh",              0,                       IF_SVE_4F,               0x44007400,      0x44201400,      0x44A01400,      0x44E01400       )
    // SQRDMLSH <Zda>.<T>, <Zn>.<T>, <Zm>.<T>                                            SVE_EM_3A           01000100xx0mmmmm 011101nnnnnddddd     4400 7400   
    // SQRDMLSH <Zda>.H, <Zn>.H, <Zm>.H[<imm>]                                           SVE_FK_3A           010001000i1iimmm 000101nnnnnddddd     4420 1400   
    // SQRDMLSH <Zda>.S, <Zn>.S, <Zm>.S[<imm>]                                           SVE_FK_3B           01000100101iimmm 000101nnnnnddddd     44A0 1400   
    // SQRDMLSH <Zda>.D, <Zn>.D, <Zm>.D[<imm>]                                           SVE_FK_3C           01000100111immmm 000101nnnnnddddd     44E0 1400   


//    enum               name                     info                                              SVE_AR_4A        SVE_FF_3A        SVE_FF_3B        SVE_FF_3C        
INST4(mla,               "mla",                   0,                       IF_SVE_4G,               0x04004000,      0x44200800,      0x44A00800,      0x44E00800       )
    // MLA     <Zda>.<T>, <Pg>/M, <Zn>.<T>, <Zm>.<T>                                     SVE_AR_4A           00000100xx0mmmmm 010gggnnnnnddddd     0400 4000   
    // MLA     <Zda>.H, <Zn>.H, <Zm>.H[<imm>]                                            SVE_FF_3A           010001000i1iimmm 000010nnnnnddddd     4420 0800   
    // MLA     <Zda>.S, <Zn>.S, <Zm>.S[<imm>]                                            SVE_FF_3B           01000100101iimmm 000010nnnnnddddd     44A0 0800   
    // MLA     <Zda>.D, <Zn>.D, <Zm>.D[<imm>]                                            SVE_FF_3C           01000100111immmm 000010nnnnnddddd     44E0 0800   

INST4(mls,               "mls",                   0,                       IF_SVE_4G,               0x04006000,      0x44200C00,      0x44A00C00,      0x44E00C00       )
    // MLS     <Zda>.<T>, <Pg>/M, <Zn>.<T>, <Zm>.<T>                                     SVE_AR_4A           00000100xx0mmmmm 011gggnnnnnddddd     0400 6000   
    // MLS     <Zda>.H, <Zn>.H, <Zm>.H[<imm>]                                            SVE_FF_3A           010001000i1iimmm 000011nnnnnddddd     4420 0C00   
    // MLS     <Zda>.S, <Zn>.S, <Zm>.S[<imm>]                                            SVE_FF_3B           01000100101iimmm 000011nnnnnddddd     44A0 0C00   
    // MLS     <Zda>.D, <Zn>.D, <Zm>.D[<imm>]                                            SVE_FF_3C           01000100111immmm 000011nnnnnddddd     44E0 0C00   


//    enum               name                     info                                              SVE_GM_3A        SVE_GN_3A        SVE_GZ_3A        SVE_HB_3A        
INST4(fmlalb,            "fmlalb",                0,                       IF_SVE_4H,               0x64205000,      0x64A08800,      0x64A04000,      0x64A08000       )
    // FMLALB  <Zda>.H, <Zn>.B, <Zm>.B[<imm>]                                            SVE_GM_3A           01100100001iimmm 0101iinnnnnddddd     6420 5000   
    // FMLALB  <Zda>.H, <Zn>.B, <Zm>.B                                                   SVE_GN_3A           01100100101mmmmm 100010nnnnnddddd     64A0 8800   
    // FMLALB  <Zda>.S, <Zn>.H, <Zm>.H[<imm>]                                            SVE_GZ_3A           01100100101iimmm 0100i0nnnnnddddd     64A0 4000   
    // FMLALB  <Zda>.S, <Zn>.H, <Zm>.H                                                   SVE_HB_3A           01100100101mmmmm 100000nnnnnddddd     64A0 8000   

INST4(fmlalt,            "fmlalt",                0,                       IF_SVE_4H,               0x64A05000,      0x64A09800,      0x64A04400,      0x64A08400       )
    // FMLALT  <Zda>.H, <Zn>.B, <Zm>.B[<imm>]                                            SVE_GM_3A           01100100101iimmm 0101iinnnnnddddd     64A0 5000   
    // FMLALT  <Zda>.H, <Zn>.B, <Zm>.B                                                   SVE_GN_3A           01100100101mmmmm 100110nnnnnddddd     64A0 9800   
    // FMLALT  <Zda>.S, <Zn>.H, <Zm>.H[<imm>]                                            SVE_GZ_3A           01100100101iimmm 0100i1nnnnnddddd     64A0 4400   
    // FMLALT  <Zda>.S, <Zn>.H, <Zm>.H                                                   SVE_HB_3A           01100100101mmmmm 100001nnnnnddddd     64A0 8400   


//    enum               name                     info                                              SVE_AX_1A        SVE_AY_2A        SVE_AZ_2A        SVE_BA_3A        
INST4(index,             "index",                 0,                       IF_SVE_4I,               0x04204000,      0x04204800,      0x04204400,      0x04204C00       )
    // INDEX   <Zd>.<T>, #<imm1>, #<imm2>                                                SVE_AX_1A           00000100xx1iiiii 010000iiiiiddddd     0420 4000   
    // INDEX   <Zd>.<T>, #<imm>, <R><m>                                                  SVE_AY_2A           00000100xx1mmmmm 010010iiiiiddddd     0420 4800   
    // INDEX   <Zd>.<T>, <R><n>, #<imm>                                                  SVE_AZ_2A           00000100xx1iiiii 010001nnnnnddddd     0420 4400   
    // INDEX   <Zd>.<T>, <R><n>, <R><m>                                                  SVE_BA_3A           00000100xx1mmmmm 010011nnnnnddddd     0420 4C00   


//    enum               name                     info                                              SVE_BV_2A        SVE_BV_2A_A      SVE_CP_3A        SVE_CQ_3A        
INST4(cpy,               "cpy",                   0,                       IF_SVE_4J,               0x05100000,      0x05104000,      0x05208000,      0x0528A000       )
    // CPY     <Zd>.<T>, <Pg>/Z, #<imm>{, <shift>}                                       SVE_BV_2A           00000101xx01gggg 00hiiiiiiiiddddd     0510 0000   
    // CPY     <Zd>.<T>, <Pg>/M, #<imm>{, <shift>}                                       SVE_BV_2A_A         00000101xx01gggg 01hiiiiiiiiddddd     0510 4000   
    // CPY     <Zd>.<T>, <Pg>/M, <V><n>                                                  SVE_CP_3A           00000101xx100000 100gggnnnnnddddd     0520 8000   
    // CPY     <Zd>.<T>, <Pg>/M, <R><n|SP>                                               SVE_CQ_3A           00000101xx101000 101gggnnnnnddddd     0528 A000   


//    enum               name                     info                                              SVE_IF_4A        SVE_IF_4A_A      SVE_IM_3A        SVE_IN_4A        
INST4(ldnt1b,            "ldnt1b",                0,                       IF_SVE_4K,               0x8400A000,      0xC400C000,      0xA400E000,      0xA400C000       )
    // LDNT1B  {<Zt>.S }, <Pg>/Z, [<Zn>.S{, <Xm>}]                                       SVE_IF_4A           10000100000mmmmm 101gggnnnnnttttt     8400 A000   
    // LDNT1B  {<Zt>.D }, <Pg>/Z, [<Zn>.D{, <Xm>}]                                       SVE_IF_4A_A         11000100000mmmmm 110gggnnnnnttttt     C400 C000   
    // LDNT1B  {<Zt>.B }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]                            SVE_IM_3A           101001000000iiii 111gggnnnnnttttt     A400 E000   
    // LDNT1B  {<Zt>.B }, <Pg>/Z, [<Xn|SP>, <Xm>]                                        SVE_IN_4A           10100100000mmmmm 110gggnnnnnttttt     A400 C000   

INST4(ldnt1h,            "ldnt1h",                0,                       IF_SVE_4K,               0x8480A000,      0xC480C000,      0xA480E000,      0xA480C000       )
    // LDNT1H  {<Zt>.S }, <Pg>/Z, [<Zn>.S{, <Xm>}]                                       SVE_IF_4A           10000100100mmmmm 101gggnnnnnttttt     8480 A000   
    // LDNT1H  {<Zt>.D }, <Pg>/Z, [<Zn>.D{, <Xm>}]                                       SVE_IF_4A_A         11000100100mmmmm 110gggnnnnnttttt     C480 C000   
    // LDNT1H  {<Zt>.H }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]                            SVE_IM_3A           101001001000iiii 111gggnnnnnttttt     A480 E000   
    // LDNT1H  {<Zt>.H }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #1]                                SVE_IN_4A           10100100100mmmmm 110gggnnnnnttttt     A480 C000   

INST4(ldnt1w,            "ldnt1w",                0,                       IF_SVE_4K,               0x8500A000,      0xC500C000,      0xA500E000,      0xA500C000       )
    // LDNT1W  {<Zt>.S }, <Pg>/Z, [<Zn>.S{, <Xm>}]                                       SVE_IF_4A           10000101000mmmmm 101gggnnnnnttttt     8500 A000   
    // LDNT1W  {<Zt>.D }, <Pg>/Z, [<Zn>.D{, <Xm>}]                                       SVE_IF_4A_A         11000101000mmmmm 110gggnnnnnttttt     C500 C000   
    // LDNT1W  {<Zt>.S }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]                            SVE_IM_3A           101001010000iiii 111gggnnnnnttttt     A500 E000   
    // LDNT1W  {<Zt>.S }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #2]                                SVE_IN_4A           10100101000mmmmm 110gggnnnnnttttt     A500 C000   


//    enum               name                     info                                              SVE_IZ_4A        SVE_IZ_4A_A      SVE_JB_4A        SVE_JM_3A        
INST4(stnt1b,            "stnt1b",                0,                       IF_SVE_4L,               0xE4402000,      0xE4002000,      0xE4006000,      0xE410E000       )
    // STNT1B  {<Zt>.S }, <Pg>, [<Zn>.S{, <Xm>}]                                         SVE_IZ_4A           11100100010mmmmm 001gggnnnnnttttt     E440 2000   
    // STNT1B  {<Zt>.D }, <Pg>, [<Zn>.D{, <Xm>}]                                         SVE_IZ_4A_A         11100100000mmmmm 001gggnnnnnttttt     E400 2000   
    // STNT1B  {<Zt>.B }, <Pg>, [<Xn|SP>, <Xm>]                                          SVE_JB_4A           11100100000mmmmm 011gggnnnnnttttt     E400 6000   
    // STNT1B  {<Zt>.B }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]                              SVE_JM_3A           111001000001iiii 111gggnnnnnttttt     E410 E000   

INST4(stnt1h,            "stnt1h",                0,                       IF_SVE_4L,               0xE4C02000,      0xE4802000,      0xE4806000,      0xE490E000       )
    // STNT1H  {<Zt>.S }, <Pg>, [<Zn>.S{, <Xm>}]                                         SVE_IZ_4A           11100100110mmmmm 001gggnnnnnttttt     E4C0 2000   
    // STNT1H  {<Zt>.D }, <Pg>, [<Zn>.D{, <Xm>}]                                         SVE_IZ_4A_A         11100100100mmmmm 001gggnnnnnttttt     E480 2000   
    // STNT1H  {<Zt>.H }, <Pg>, [<Xn|SP>, <Xm>, LSL #1]                                  SVE_JB_4A           11100100100mmmmm 011gggnnnnnttttt     E480 6000   
    // STNT1H  {<Zt>.H }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]                              SVE_JM_3A           111001001001iiii 111gggnnnnnttttt     E490 E000   

INST4(stnt1w,            "stnt1w",                0,                       IF_SVE_4L,               0xE5402000,      0xE5002000,      0xE5006000,      0xE510E000       )
    // STNT1W  {<Zt>.S }, <Pg>, [<Zn>.S{, <Xm>}]                                         SVE_IZ_4A           11100101010mmmmm 001gggnnnnnttttt     E540 2000   
    // STNT1W  {<Zt>.D }, <Pg>, [<Zn>.D{, <Xm>}]                                         SVE_IZ_4A_A         11100101000mmmmm 001gggnnnnnttttt     E500 2000   
    // STNT1W  {<Zt>.S }, <Pg>, [<Xn|SP>, <Xm>, LSL #2]                                  SVE_JB_4A           11100101000mmmmm 011gggnnnnnttttt     E500 6000   
    // STNT1W  {<Zt>.S }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]                              SVE_JM_3A           111001010001iiii 111gggnnnnnttttt     E510 E000   


//    enum               name                     info                                              SVE_AB_3A        SVE_AT_3A        SVE_EC_1A        
INST3(add,               "add",                   0,                       IF_SVE_3A,               0x04000000,      0x04200000,      0x2520C000       )
    // ADD     <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_AB_3A           00000100xx000000 000gggmmmmmddddd     0400 0000   
    // ADD     <Zd>.<T>, <Zn>.<T>, <Zm>.<T>                                              SVE_AT_3A           00000100xx1mmmmm 000000nnnnnddddd     0420 0000   
    // ADD     <Zdn>.<T>, <Zdn>.<T>, #<imm>{, <shift>}                                   SVE_EC_1A           00100101xx100000 11hiiiiiiiiddddd     2520 C000   

INST3(sub,               "sub",                   0,                       IF_SVE_3A,               0x04010000,      0x04200400,      0x2521C000       )
    // SUB     <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_AB_3A           00000100xx000001 000gggmmmmmddddd     0401 0000   
    // SUB     <Zd>.<T>, <Zn>.<T>, <Zm>.<T>                                              SVE_AT_3A           00000100xx1mmmmm 000001nnnnnddddd     0420 0400   
    // SUB     <Zdn>.<T>, <Zdn>.<T>, #<imm>{, <shift>}                                   SVE_EC_1A           00100101xx100001 11hiiiiiiiiddddd     2521 C000   


//    enum               name                     info                                              SVE_BH_3A        SVE_BH_3B        SVE_BH_3B_A      
INST3(adr,               "adr",                   0,                       IF_SVE_3B,               0x04A0A000,      0x0420A000,      0x0460A000       )
    // ADR     <Zd>.<T>, [<Zn>.<T>, <Zm>.<T>{, <mod><amount>}]                           SVE_BH_3A           000001001x1mmmmm 1010hhnnnnnddddd     04A0 A000   
    // ADR     <Zd>.D, [<Zn>.D, <Zm>.D, SXTW{<amount>}]                                  SVE_BH_3B           00000100001mmmmm 1010hhnnnnnddddd     0420 A000   
    // ADR     <Zd>.D, [<Zn>.D, <Zm>.D, UXTW{<amount>}]                                  SVE_BH_3B_A         00000100011mmmmm 1010hhnnnnnddddd     0460 A000   


//    enum               name                     info                                              SVE_BW_2A        SVE_CB_2A        SVE_EB_1A        
INST3(dup,               "dup",                   0,                       IF_SVE_3C,               0x05202000,      0x05203800,      0x2538C000       )
    // DUP     <Zd>.<T>, <Zn>.<T>[<imm>]                                                 SVE_BW_2A           00000101ii1xxxxx 001000nnnnnddddd     0520 2000   
    // DUP     <Zd>.<T>, <R><n|SP>                                                       SVE_CB_2A           00000101xx100000 001110nnnnnddddd     0520 3800   
    // DUP     <Zd>.<T>, #<imm>{, <shift>}                                               SVE_EB_1A           00100101xx111000 11hiiiiiiiiddddd     2538 C000   


//    enum               name                     info                                              SVE_BR_3A        SVE_BR_3B        SVE_CI_3A        
INST3(trn1,              "trn1",                  0,                       IF_SVE_3D,               0x05207000,      0x05A01800,      0x05205000       )
    // TRN1    <Zd>.<T>, <Zn>.<T>, <Zm>.<T>                                              SVE_BR_3A           00000101xx1mmmmm 011100nnnnnddddd     0520 7000   
    // TRN1    <Zd>.Q, <Zn>.Q, <Zm>.Q                                                    SVE_BR_3B           00000101101mmmmm 000110nnnnnddddd     05A0 1800   
    // TRN1    <Pd>.<T>, <Pn>.<T>, <Pm>.<T>                                              SVE_CI_3A           00000101xx10MMMM 0101000NNNN0DDDD     0520 5000   

INST3(trn2,              "trn2",                  0,                       IF_SVE_3D,               0x05207400,      0x05A01C00,      0x05205400       )
    // TRN2    <Zd>.<T>, <Zn>.<T>, <Zm>.<T>                                              SVE_BR_3A           00000101xx1mmmmm 011101nnnnnddddd     0520 7400   
    // TRN2    <Zd>.Q, <Zn>.Q, <Zm>.Q                                                    SVE_BR_3B           00000101101mmmmm 000111nnnnnddddd     05A0 1C00   
    // TRN2    <Pd>.<T>, <Pn>.<T>, <Pm>.<T>                                              SVE_CI_3A           00000101xx10MMMM 0101010NNNN0DDDD     0520 5400   

INST3(uzp1,              "uzp1",                  0,                       IF_SVE_3D,               0x05206800,      0x05A00800,      0x05204800       )
    // UZP1    <Zd>.<T>, <Zn>.<T>, <Zm>.<T>                                              SVE_BR_3A           00000101xx1mmmmm 011010nnnnnddddd     0520 6800   
    // UZP1    <Zd>.Q, <Zn>.Q, <Zm>.Q                                                    SVE_BR_3B           00000101101mmmmm 000010nnnnnddddd     05A0 0800   
    // UZP1    <Pd>.<T>, <Pn>.<T>, <Pm>.<T>                                              SVE_CI_3A           00000101xx10MMMM 0100100NNNN0DDDD     0520 4800   

INST3(uzp2,              "uzp2",                  0,                       IF_SVE_3D,               0x05206C00,      0x05A00C00,      0x05204C00       )
    // UZP2    <Zd>.<T>, <Zn>.<T>, <Zm>.<T>                                              SVE_BR_3A           00000101xx1mmmmm 011011nnnnnddddd     0520 6C00   
    // UZP2    <Zd>.Q, <Zn>.Q, <Zm>.Q                                                    SVE_BR_3B           00000101101mmmmm 000011nnnnnddddd     05A0 0C00   
    // UZP2    <Pd>.<T>, <Pn>.<T>, <Pm>.<T>                                              SVE_CI_3A           00000101xx10MMMM 0100110NNNN0DDDD     0520 4C00   

INST3(zip1,              "zip1",                  0,                       IF_SVE_3D,               0x05206000,      0x05A00000,      0x05204000       )
    // ZIP1    <Zd>.<T>, <Zn>.<T>, <Zm>.<T>                                              SVE_BR_3A           00000101xx1mmmmm 011000nnnnnddddd     0520 6000   
    // ZIP1    <Zd>.Q, <Zn>.Q, <Zm>.Q                                                    SVE_BR_3B           00000101101mmmmm 000000nnnnnddddd     05A0 0000   
    // ZIP1    <Pd>.<T>, <Pn>.<T>, <Pm>.<T>                                              SVE_CI_3A           00000101xx10MMMM 0100000NNNN0DDDD     0520 4000   

INST3(zip2,              "zip2",                  0,                       IF_SVE_3D,               0x05206400,      0x05A00400,      0x05204400       )
    // ZIP2    <Zd>.<T>, <Zn>.<T>, <Zm>.<T>                                              SVE_BR_3A           00000101xx1mmmmm 011001nnnnnddddd     0520 6400   
    // ZIP2    <Zd>.Q, <Zn>.Q, <Zm>.Q                                                    SVE_BR_3B           00000101101mmmmm 000001nnnnnddddd     05A0 0400   
    // ZIP2    <Pd>.<T>, <Pn>.<T>, <Pm>.<T>                                              SVE_CI_3A           00000101xx10MMMM 0100010NNNN0DDDD     0520 4400   


//    enum               name                     info                                              SVE_AT_3A        SVE_EC_1A        SVE_ET_3A        
INST3(sqadd,             "sqadd",                 0,                       IF_SVE_3E,               0x04201000,      0x2524C000,      0x44188000       )
    // SQADD   <Zd>.<T>, <Zn>.<T>, <Zm>.<T>                                              SVE_AT_3A           00000100xx1mmmmm 000100nnnnnddddd     0420 1000   
    // SQADD   <Zdn>.<T>, <Zdn>.<T>, #<imm>{, <shift>}                                   SVE_EC_1A           00100101xx100100 11hiiiiiiiiddddd     2524 C000   
    // SQADD   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_ET_3A           01000100xx011000 100gggmmmmmddddd     4418 8000   

INST3(sqsub,             "sqsub",                 0,                       IF_SVE_3E,               0x04201800,      0x2526C000,      0x441A8000       )
    // SQSUB   <Zd>.<T>, <Zn>.<T>, <Zm>.<T>                                              SVE_AT_3A           00000100xx1mmmmm 000110nnnnnddddd     0420 1800   
    // SQSUB   <Zdn>.<T>, <Zdn>.<T>, #<imm>{, <shift>}                                   SVE_EC_1A           00100101xx100110 11hiiiiiiiiddddd     2526 C000   
    // SQSUB   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_ET_3A           01000100xx011010 100gggmmmmmddddd     441A 8000   

INST3(uqadd,             "uqadd",                 0,                       IF_SVE_3E,               0x04201400,      0x2525C000,      0x44198000       )
    // UQADD   <Zd>.<T>, <Zn>.<T>, <Zm>.<T>                                              SVE_AT_3A           00000100xx1mmmmm 000101nnnnnddddd     0420 1400   
    // UQADD   <Zdn>.<T>, <Zdn>.<T>, #<imm>{, <shift>}                                   SVE_EC_1A           00100101xx100101 11hiiiiiiiiddddd     2525 C000   
    // UQADD   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_ET_3A           01000100xx011001 100gggmmmmmddddd     4419 8000   

INST3(uqsub,             "uqsub",                 0,                       IF_SVE_3E,               0x04201C00,      0x2527C000,      0x441B8000       )
    // UQSUB   <Zd>.<T>, <Zn>.<T>, <Zm>.<T>                                              SVE_AT_3A           00000100xx1mmmmm 000111nnnnnddddd     0420 1C00   
    // UQSUB   <Zdn>.<T>, <Zdn>.<T>, #<imm>{, <shift>}                                   SVE_EC_1A           00100101xx100111 11hiiiiiiiiddddd     2527 C000   
    // UQSUB   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_ET_3A           01000100xx011011 100gggmmmmmddddd     441B 8000   


//    enum               name                     info                                              SVE_GU_3A        SVE_GU_3B        SVE_HU_4A        
INST3(fmla,              "fmla",                  0,                       IF_SVE_3F,               0x64A00000,      0x64E00000,      0x65200000       )
    // FMLA    <Zda>.S, <Zn>.S, <Zm>.S[<imm>]                                            SVE_GU_3A           01100100101iimmm 000000nnnnnddddd     64A0 0000   
    // FMLA    <Zda>.D, <Zn>.D, <Zm>.D[<imm>]                                            SVE_GU_3B           01100100111immmm 000000nnnnnddddd     64E0 0000   
    // FMLA    <Zda>.<T>, <Pg>/M, <Zn>.<T>, <Zm>.<T>                                     SVE_HU_4A           01100101xx1mmmmm 000gggnnnnnddddd     6520 0000   

INST3(fmls,              "fmls",                  0,                       IF_SVE_3F,               0x64A00400,      0x64E00400,      0x65202000       )
    // FMLS    <Zda>.S, <Zn>.S, <Zm>.S[<imm>]                                            SVE_GU_3A           01100100101iimmm 000001nnnnnddddd     64A0 0400   
    // FMLS    <Zda>.D, <Zn>.D, <Zm>.D[<imm>]                                            SVE_GU_3B           01100100111immmm 000001nnnnnddddd     64E0 0400   
    // FMLS    <Zda>.<T>, <Pg>/M, <Zn>.<T>, <Zm>.<T>                                     SVE_HU_4A           01100101xx1mmmmm 001gggnnnnnddddd     6520 2000   


//    enum               name                     info                                              SVE_GH_3A        SVE_GH_3B        SVE_GH_3B_B      
INST3(luti4,             "luti4",                 0,                       IF_SVE_3G,               0x4560A400,      0x4520B400,      0x4520BC00       )
    // LUTI4   <Zd>.B, {<Zn>.B }, <Zm>[<index>]                                          SVE_GH_3A           01000101i11mmmmm 101001nnnnnddddd     4560 A400   
    // LUTI4   <Zd>.H, {<Zn1>.H, <Zn2>.H }, <Zm>[<index>]                                SVE_GH_3B           01000101ii1mmmmm 101101nnnnnddddd     4520 B400   
    // LUTI4   <Zd>.H, {<Zn>.H }, <Zm>[<index>]                                          SVE_GH_3B_B         01000101ii1mmmmm 101111nnnnnddddd     4520 BC00   


//    enum               name                     info                                              SVE_HK_3A        SVE_HL_3A        SVE_HM_2A        
INST3(fadd,              "fadd",                  0,                       IF_SVE_3H,               0x65000000,      0x65008000,      0x65188000       )
    // FADD    <Zd>.<T>, <Zn>.<T>, <Zm>.<T>                                              SVE_HK_3A           01100101xx0mmmmm 000000nnnnnddddd     6500 0000   
    // FADD    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_HL_3A           01100101xx000000 100gggmmmmmddddd     6500 8000   
    // FADD    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <const>                                     SVE_HM_2A           01100101xx011000 100ggg0000iddddd     6518 8000   

INST3(fsub,              "fsub",                  0,                       IF_SVE_3H,               0x65000400,      0x65018000,      0x65198000       )
    // FSUB    <Zd>.<T>, <Zn>.<T>, <Zm>.<T>                                              SVE_HK_3A           01100101xx0mmmmm 000001nnnnnddddd     6500 0400   
    // FSUB    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_HL_3A           01100101xx000001 100gggmmmmmddddd     6501 8000   
    // FSUB    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <const>                                     SVE_HM_2A           01100101xx011001 100ggg0000iddddd     6519 8000   


//    enum               name                     info                                              SVE_CM_3A        SVE_CN_3A        SVE_CO_3A        
INST3(clasta,            "clasta",                0,                       IF_SVE_3I,               0x05288000,      0x052A8000,      0x0530A000       )
    // CLASTA  <Zdn>.<T>, <Pg>, <Zdn>.<T>, <Zm>.<T>                                      SVE_CM_3A           00000101xx101000 100gggmmmmmddddd     0528 8000   
    // CLASTA  <V><dn>, <Pg>, <V><dn>, <Zm>.<T>                                          SVE_CN_3A           00000101xx101010 100gggmmmmmddddd     052A 8000   
    // CLASTA  <R><dn>, <Pg>, <R><dn>, <Zm>.<T>                                          SVE_CO_3A           00000101xx110000 101gggmmmmmddddd     0530 A000   

INST3(clastb,            "clastb",                0,                       IF_SVE_3I,               0x05298000,      0x052B8000,      0x0531A000       )
    // CLASTB  <Zdn>.<T>, <Pg>, <Zdn>.<T>, <Zm>.<T>                                      SVE_CM_3A           00000101xx101001 100gggmmmmmddddd     0529 8000   
    // CLASTB  <V><dn>, <Pg>, <V><dn>, <Zm>.<T>                                          SVE_CN_3A           00000101xx101011 100gggmmmmmddddd     052B 8000   
    // CLASTB  <R><dn>, <Pg>, <R><dn>, <Zm>.<T>                                          SVE_CO_3A           00000101xx110001 101gggmmmmmddddd     0531 A000   


//    enum               name                     info                                              SVE_CX_4A        SVE_CX_4A_A      SVE_CY_3A        
INST3(cmpeq,             "cmpeq",                 0,                       IF_SVE_3J,               0x2400A000,      0x24002000,      0x25008000       )
    // CMPEQ   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.<T>                                      SVE_CX_4A           00100100xx0mmmmm 101gggnnnnn0DDDD     2400 A000   
    // CMPEQ   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.D                                        SVE_CX_4A_A         00100100xx0mmmmm 001gggnnnnn0DDDD     2400 2000   
    // CMPEQ   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, #<imm>                                        SVE_CY_3A           00100101xx0iiiii 100gggnnnnn0DDDD     2500 8000   

INST3(cmpge,             "cmpge",                 0,                       IF_SVE_3J,               0x24008000,      0x24004000,      0x25000000       )
    // CMPGE   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.<T>                                      SVE_CX_4A           00100100xx0mmmmm 100gggnnnnn0DDDD     2400 8000   
    // CMPGE   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.D                                        SVE_CX_4A_A         00100100xx0mmmmm 010gggnnnnn0DDDD     2400 4000   
    // CMPGE   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, #<imm>                                        SVE_CY_3A           00100101xx0iiiii 000gggnnnnn0DDDD     2500 0000   

INST3(cmpgt,             "cmpgt",                 0,                       IF_SVE_3J,               0x24008010,      0x24004010,      0x25000010       )
    // CMPGT   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.<T>                                      SVE_CX_4A           00100100xx0mmmmm 100gggnnnnn1DDDD     2400 8010   
    // CMPGT   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.D                                        SVE_CX_4A_A         00100100xx0mmmmm 010gggnnnnn1DDDD     2400 4010   
    // CMPGT   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, #<imm>                                        SVE_CY_3A           00100101xx0iiiii 000gggnnnnn1DDDD     2500 0010   

INST3(cmple,             "cmple",                 0,                       IF_SVE_3J,               0x24008000,      0x24006010,      0x25002010       )
    // CMPLE   <Pd>.<T>, <Pg>/Z, <Zm>.<T>, <Zn>.<T>                                      SVE_CX_4A           00100100xx0mmmmm 100gggnnnnn0DDDD     2400 8000   
    // CMPLE   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.D                                        SVE_CX_4A_A         00100100xx0mmmmm 011gggnnnnn1DDDD     2400 6010   
    // CMPLE   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, #<imm>                                        SVE_CY_3A           00100101xx0iiiii 001gggnnnnn1DDDD     2500 2010   

INST3(cmplt,             "cmplt",                 0,                       IF_SVE_3J,               0x24008010,      0x24006000,      0x25002000       )
    // CMPLT   <Pd>.<T>, <Pg>/Z, <Zm>.<T>, <Zn>.<T>                                      SVE_CX_4A           00100100xx0mmmmm 100gggnnnnn1DDDD     2400 8010   
    // CMPLT   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.D                                        SVE_CX_4A_A         00100100xx0mmmmm 011gggnnnnn0DDDD     2400 6000   
    // CMPLT   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, #<imm>                                        SVE_CY_3A           00100101xx0iiiii 001gggnnnnn0DDDD     2500 2000   

INST3(cmpne,             "cmpne",                 0,                       IF_SVE_3J,               0x2400A010,      0x24002010,      0x25008010       )
    // CMPNE   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.<T>                                      SVE_CX_4A           00100100xx0mmmmm 101gggnnnnn1DDDD     2400 A010   
    // CMPNE   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.D                                        SVE_CX_4A_A         00100100xx0mmmmm 001gggnnnnn1DDDD     2400 2010   
    // CMPNE   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, #<imm>                                        SVE_CY_3A           00100101xx0iiiii 100gggnnnnn1DDDD     2500 8010   


//    enum               name                     info                                              SVE_CX_4A        SVE_CX_4A_A      SVE_CY_3B        
INST3(cmphi,             "cmphi",                 0,                       IF_SVE_3K,               0x24000010,      0x2400C010,      0x24200010       )
    // CMPHI   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.<T>                                      SVE_CX_4A           00100100xx0mmmmm 000gggnnnnn1DDDD     2400 0010   
    // CMPHI   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.D                                        SVE_CX_4A_A         00100100xx0mmmmm 110gggnnnnn1DDDD     2400 C010   
    // CMPHI   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, #<imm>                                        SVE_CY_3B           00100100xx1iiiii ii0gggnnnnn1DDDD     2420 0010   

INST3(cmphs,             "cmphs",                 0,                       IF_SVE_3K,               0x24000000,      0x2400C000,      0x24200000       )
    // CMPHS   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.<T>                                      SVE_CX_4A           00100100xx0mmmmm 000gggnnnnn0DDDD     2400 0000   
    // CMPHS   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.D                                        SVE_CX_4A_A         00100100xx0mmmmm 110gggnnnnn0DDDD     2400 C000   
    // CMPHS   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, #<imm>                                        SVE_CY_3B           00100100xx1iiiii ii0gggnnnnn0DDDD     2420 0000   

INST3(cmplo,             "cmplo",                 0,                       IF_SVE_3K,               0x24000010,      0x2400E000,      0x24202000       )
    // CMPLO   <Pd>.<T>, <Pg>/Z, <Zm>.<T>, <Zn>.<T>                                      SVE_CX_4A           00100100xx0mmmmm 000gggnnnnn1DDDD     2400 0010   
    // CMPLO   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.D                                        SVE_CX_4A_A         00100100xx0mmmmm 111gggnnnnn0DDDD     2400 E000   
    // CMPLO   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, #<imm>                                        SVE_CY_3B           00100100xx1iiiii ii1gggnnnnn0DDDD     2420 2000   

INST3(cmpls,             "cmpls",                 0,                       IF_SVE_3K,               0x24000000,      0x2400E010,      0x24202010       )
    // CMPLS   <Pd>.<T>, <Pg>/Z, <Zm>.<T>, <Zn>.<T>                                      SVE_CX_4A           00100100xx0mmmmm 000gggnnnnn0DDDD     2400 0000   
    // CMPLS   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.D                                        SVE_CX_4A_A         00100100xx0mmmmm 111gggnnnnn1DDDD     2400 E010   
    // CMPLS   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, #<imm>                                        SVE_CY_3B           00100100xx1iiiii ii1gggnnnnn1DDDD     2420 2010   


//    enum               name                     info                                              SVE_DT_3A        SVE_DX_3A        SVE_DY_3A        
INST3(whilege,           "whilege",               0,                       IF_SVE_3L,               0x25200000,      0x25205010,      0x25204010       )
    // WHILEGE <Pd>.<T>, <R><n>, <R><m>                                                  SVE_DT_3A           00100101xx1mmmmm 000X00nnnnn0DDDD     2520 0000   
    // WHILEGE {<Pd1>.<T>, <Pd2>.<T>}, <Xn>, <Xm>                                        SVE_DX_3A           00100101xx1mmmmm 010100nnnnn1DDD0     2520 5010   
    // WHILEGE <PNd>.<T>, <Xn>, <Xm>, <vl>                                               SVE_DY_3A           00100101xx1mmmmm 01l000nnnnn10DDD     2520 4010   

INST3(whilegt,           "whilegt",               0,                       IF_SVE_3L,               0x25200010,      0x25205011,      0x25204018       )
    // WHILEGT <Pd>.<T>, <R><n>, <R><m>                                                  SVE_DT_3A           00100101xx1mmmmm 000X00nnnnn1DDDD     2520 0010   
    // WHILEGT {<Pd1>.<T>, <Pd2>.<T>}, <Xn>, <Xm>                                        SVE_DX_3A           00100101xx1mmmmm 010100nnnnn1DDD1     2520 5011   
    // WHILEGT <PNd>.<T>, <Xn>, <Xm>, <vl>                                               SVE_DY_3A           00100101xx1mmmmm 01l000nnnnn11DDD     2520 4018   

INST3(whilehi,           "whilehi",               0,                       IF_SVE_3L,               0x25200810,      0x25205811,      0x25204818       )
    // WHILEHI <Pd>.<T>, <R><n>, <R><m>                                                  SVE_DT_3A           00100101xx1mmmmm 000X10nnnnn1DDDD     2520 0810   
    // WHILEHI {<Pd1>.<T>, <Pd2>.<T>}, <Xn>, <Xm>                                        SVE_DX_3A           00100101xx1mmmmm 010110nnnnn1DDD1     2520 5811   
    // WHILEHI <PNd>.<T>, <Xn>, <Xm>, <vl>                                               SVE_DY_3A           00100101xx1mmmmm 01l010nnnnn11DDD     2520 4818   

INST3(whilehs,           "whilehs",               0,                       IF_SVE_3L,               0x25200800,      0x25205810,      0x25204810       )
    // WHILEHS <Pd>.<T>, <R><n>, <R><m>                                                  SVE_DT_3A           00100101xx1mmmmm 000X10nnnnn0DDDD     2520 0800   
    // WHILEHS {<Pd1>.<T>, <Pd2>.<T>}, <Xn>, <Xm>                                        SVE_DX_3A           00100101xx1mmmmm 010110nnnnn1DDD0     2520 5810   
    // WHILEHS <PNd>.<T>, <Xn>, <Xm>, <vl>                                               SVE_DY_3A           00100101xx1mmmmm 01l010nnnnn10DDD     2520 4810   

INST3(whilele,           "whilele",               0,                       IF_SVE_3L,               0x25200410,      0x25205411,      0x25204418       )
    // WHILELE <Pd>.<T>, <R><n>, <R><m>                                                  SVE_DT_3A           00100101xx1mmmmm 000X01nnnnn1DDDD     2520 0410   
    // WHILELE {<Pd1>.<T>, <Pd2>.<T>}, <Xn>, <Xm>                                        SVE_DX_3A           00100101xx1mmmmm 010101nnnnn1DDD1     2520 5411   
    // WHILELE <PNd>.<T>, <Xn>, <Xm>, <vl>                                               SVE_DY_3A           00100101xx1mmmmm 01l001nnnnn11DDD     2520 4418   

INST3(whilelo,           "whilelo",               0,                       IF_SVE_3L,               0x25200C00,      0x25205C10,      0x25204C10       )
    // WHILELO <Pd>.<T>, <R><n>, <R><m>                                                  SVE_DT_3A           00100101xx1mmmmm 000X11nnnnn0DDDD     2520 0C00   
    // WHILELO {<Pd1>.<T>, <Pd2>.<T>}, <Xn>, <Xm>                                        SVE_DX_3A           00100101xx1mmmmm 010111nnnnn1DDD0     2520 5C10   
    // WHILELO <PNd>.<T>, <Xn>, <Xm>, <vl>                                               SVE_DY_3A           00100101xx1mmmmm 01l011nnnnn10DDD     2520 4C10   

INST3(whilels,           "whilels",               0,                       IF_SVE_3L,               0x25200C10,      0x25205C11,      0x25204C18       )
    // WHILELS <Pd>.<T>, <R><n>, <R><m>                                                  SVE_DT_3A           00100101xx1mmmmm 000X11nnnnn1DDDD     2520 0C10   
    // WHILELS {<Pd1>.<T>, <Pd2>.<T>}, <Xn>, <Xm>                                        SVE_DX_3A           00100101xx1mmmmm 010111nnnnn1DDD1     2520 5C11   
    // WHILELS <PNd>.<T>, <Xn>, <Xm>, <vl>                                               SVE_DY_3A           00100101xx1mmmmm 01l011nnnnn11DDD     2520 4C18   

INST3(whilelt,           "whilelt",               0,                       IF_SVE_3L,               0x25200400,      0x25205410,      0x25204410       )
    // WHILELT <Pd>.<T>, <R><n>, <R><m>                                                  SVE_DT_3A           00100101xx1mmmmm 000X01nnnnn0DDDD     2520 0400   
    // WHILELT {<Pd1>.<T>, <Pd2>.<T>}, <Xn>, <Xm>                                        SVE_DX_3A           00100101xx1mmmmm 010101nnnnn1DDD0     2520 5410   
    // WHILELT <PNd>.<T>, <Xn>, <Xm>, <vl>                                               SVE_DY_3A           00100101xx1mmmmm 01l001nnnnn10DDD     2520 4410   


//    enum               name                     info                                              SVE_EJ_3A        SVE_FA_3A        SVE_FA_3B        
INST3(cdot,              "cdot",                  0,                       IF_SVE_3M,               0x44001000,      0x44A04000,      0x44E04000       )
    // CDOT    <Zda>.<T>, <Zn>.<Tb>, <Zm>.<Tb>, <const>                                  SVE_EJ_3A           01000100xx0mmmmm 0001rrnnnnnddddd     4400 1000   
    // CDOT    <Zda>.S, <Zn>.B, <Zm>.B[<imm>], <const>                                   SVE_FA_3A           01000100101iimmm 0100rrnnnnnddddd     44A0 4000   
    // CDOT    <Zda>.D, <Zn>.H, <Zm>.H[<imm>], <const>                                   SVE_FA_3B           01000100111immmm 0100rrnnnnnddddd     44E0 4000   


//    enum               name                     info                                              SVE_EK_3A        SVE_FB_3A        SVE_FB_3B        
INST3(cmla,              "cmla",                  0,                       IF_SVE_3N,               0x44002000,      0x44A06000,      0x44E06000       )
    // CMLA    <Zda>.<T>, <Zn>.<T>, <Zm>.<T>, <const>                                    SVE_EK_3A           01000100xx0mmmmm 0010rrnnnnnddddd     4400 2000   
    // CMLA    <Zda>.H, <Zn>.H, <Zm>.H[<imm>], <const>                                   SVE_FB_3A           01000100101iimmm 0110rrnnnnnddddd     44A0 6000   
    // CMLA    <Zda>.S, <Zn>.S, <Zm>.S[<imm>], <const>                                   SVE_FB_3B           01000100111immmm 0110rrnnnnnddddd     44E0 6000   


//    enum               name                     info                                              SVE_EK_3A        SVE_FC_3A        SVE_FC_3B        
INST3(sqrdcmlah,         "sqrdcmlah",             0,                       IF_SVE_3O,               0x44003000,      0x44A07000,      0x44E07000       )
    // SQRDCMLAH <Zda>.<T>, <Zn>.<T>, <Zm>.<T>, <const>                                  SVE_EK_3A           01000100xx0mmmmm 0011rrnnnnnddddd     4400 3000   
    // SQRDCMLAH <Zda>.H, <Zn>.H, <Zm>.H[<imm>], <const>                                 SVE_FC_3A           01000100101iimmm 0111rrnnnnnddddd     44A0 7000   
    // SQRDCMLAH <Zda>.S, <Zn>.S, <Zm>.S[<imm>], <const>                                 SVE_FC_3B           01000100111immmm 0111rrnnnnnddddd     44E0 7000   


//    enum               name                     info                                              SVE_EL_3A        SVE_FG_3A        SVE_FG_3B        
INST3(smlalb,            "smlalb",                0,                       IF_SVE_3P,               0x44004000,      0x44A08000,      0x44E08000       )
    // SMLALB  <Zda>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                           SVE_EL_3A           01000100xx0mmmmm 010000nnnnnddddd     4400 4000   
    // SMLALB  <Zda>.S, <Zn>.H, <Zm>.H[<imm>]                                            SVE_FG_3A           01000100101iimmm 1000i0nnnnnddddd     44A0 8000   
    // SMLALB  <Zda>.D, <Zn>.S, <Zm>.S[<imm>]                                            SVE_FG_3B           01000100111immmm 1000i0nnnnnddddd     44E0 8000   

INST3(smlalt,            "smlalt",                0,                       IF_SVE_3P,               0x44004400,      0x44A08400,      0x44E08400       )
    // SMLALT  <Zda>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                           SVE_EL_3A           01000100xx0mmmmm 010001nnnnnddddd     4400 4400   
    // SMLALT  <Zda>.S, <Zn>.H, <Zm>.H[<imm>]                                            SVE_FG_3A           01000100101iimmm 1000i1nnnnnddddd     44A0 8400   
    // SMLALT  <Zda>.D, <Zn>.S, <Zm>.S[<imm>]                                            SVE_FG_3B           01000100111immmm 1000i1nnnnnddddd     44E0 8400   

INST3(smlslb,            "smlslb",                0,                       IF_SVE_3P,               0x44005000,      0x44A0A000,      0x44E0A000       )
    // SMLSLB  <Zda>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                           SVE_EL_3A           01000100xx0mmmmm 010100nnnnnddddd     4400 5000   
    // SMLSLB  <Zda>.S, <Zn>.H, <Zm>.H[<imm>]                                            SVE_FG_3A           01000100101iimmm 1010i0nnnnnddddd     44A0 A000   
    // SMLSLB  <Zda>.D, <Zn>.S, <Zm>.S[<imm>]                                            SVE_FG_3B           01000100111immmm 1010i0nnnnnddddd     44E0 A000   

INST3(smlslt,            "smlslt",                0,                       IF_SVE_3P,               0x44005400,      0x44A0A400,      0x44E0A400       )
    // SMLSLT  <Zda>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                           SVE_EL_3A           01000100xx0mmmmm 010101nnnnnddddd     4400 5400   
    // SMLSLT  <Zda>.S, <Zn>.H, <Zm>.H[<imm>]                                            SVE_FG_3A           01000100101iimmm 1010i1nnnnnddddd     44A0 A400   
    // SMLSLT  <Zda>.D, <Zn>.S, <Zm>.S[<imm>]                                            SVE_FG_3B           01000100111immmm 1010i1nnnnnddddd     44E0 A400   

INST3(umlalb,            "umlalb",                0,                       IF_SVE_3P,               0x44004800,      0x44A09000,      0x44E09000       )
    // UMLALB  <Zda>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                           SVE_EL_3A           01000100xx0mmmmm 010010nnnnnddddd     4400 4800   
    // UMLALB  <Zda>.S, <Zn>.H, <Zm>.H[<imm>]                                            SVE_FG_3A           01000100101iimmm 1001i0nnnnnddddd     44A0 9000   
    // UMLALB  <Zda>.D, <Zn>.S, <Zm>.S[<imm>]                                            SVE_FG_3B           01000100111immmm 1001i0nnnnnddddd     44E0 9000   

INST3(umlalt,            "umlalt",                0,                       IF_SVE_3P,               0x44004C00,      0x44A09400,      0x44E09400       )
    // UMLALT  <Zda>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                           SVE_EL_3A           01000100xx0mmmmm 010011nnnnnddddd     4400 4C00   
    // UMLALT  <Zda>.S, <Zn>.H, <Zm>.H[<imm>]                                            SVE_FG_3A           01000100101iimmm 1001i1nnnnnddddd     44A0 9400   
    // UMLALT  <Zda>.D, <Zn>.S, <Zm>.S[<imm>]                                            SVE_FG_3B           01000100111immmm 1001i1nnnnnddddd     44E0 9400   

INST3(umlslb,            "umlslb",                0,                       IF_SVE_3P,               0x44005800,      0x44A0B000,      0x44E0B000       )
    // UMLSLB  <Zda>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                           SVE_EL_3A           01000100xx0mmmmm 010110nnnnnddddd     4400 5800   
    // UMLSLB  <Zda>.S, <Zn>.H, <Zm>.H[<imm>]                                            SVE_FG_3A           01000100101iimmm 1011i0nnnnnddddd     44A0 B000   
    // UMLSLB  <Zda>.D, <Zn>.S, <Zm>.S[<imm>]                                            SVE_FG_3B           01000100111immmm 1011i0nnnnnddddd     44E0 B000   

INST3(umlslt,            "umlslt",                0,                       IF_SVE_3P,               0x44005C00,      0x44A0B400,      0x44E0B400       )
    // UMLSLT  <Zda>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                           SVE_EL_3A           01000100xx0mmmmm 010111nnnnnddddd     4400 5C00   
    // UMLSLT  <Zda>.S, <Zn>.H, <Zm>.H[<imm>]                                            SVE_FG_3A           01000100101iimmm 1011i1nnnnnddddd     44A0 B400   
    // UMLSLT  <Zda>.D, <Zn>.S, <Zm>.S[<imm>]                                            SVE_FG_3B           01000100111immmm 1011i1nnnnnddddd     44E0 B400   


//    enum               name                     info                                              SVE_EO_3A        SVE_FJ_3A        SVE_FJ_3B        
INST3(sqdmlalb,          "sqdmlalb",              0,                       IF_SVE_3Q,               0x44006000,      0x44A02000,      0x44E02000       )
    // SQDMLALB <Zda>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                          SVE_EO_3A           01000100xx0mmmmm 011000nnnnnddddd     4400 6000   
    // SQDMLALB <Zda>.S, <Zn>.H, <Zm>.H[<imm>]                                           SVE_FJ_3A           01000100101iimmm 0010i0nnnnnddddd     44A0 2000   
    // SQDMLALB <Zda>.D, <Zn>.S, <Zm>.S[<imm>]                                           SVE_FJ_3B           01000100111immmm 0010i0nnnnnddddd     44E0 2000   

INST3(sqdmlalt,          "sqdmlalt",              0,                       IF_SVE_3Q,               0x44006400,      0x44A02400,      0x44E02400       )
    // SQDMLALT <Zda>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                          SVE_EO_3A           01000100xx0mmmmm 011001nnnnnddddd     4400 6400   
    // SQDMLALT <Zda>.S, <Zn>.H, <Zm>.H[<imm>]                                           SVE_FJ_3A           01000100101iimmm 0010i1nnnnnddddd     44A0 2400   
    // SQDMLALT <Zda>.D, <Zn>.S, <Zm>.S[<imm>]                                           SVE_FJ_3B           01000100111immmm 0010i1nnnnnddddd     44E0 2400   

INST3(sqdmlslb,          "sqdmlslb",              0,                       IF_SVE_3Q,               0x44006800,      0x44A03000,      0x44E03000       )
    // SQDMLSLB <Zda>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                          SVE_EO_3A           01000100xx0mmmmm 011010nnnnnddddd     4400 6800   
    // SQDMLSLB <Zda>.S, <Zn>.H, <Zm>.H[<imm>]                                           SVE_FJ_3A           01000100101iimmm 0011i0nnnnnddddd     44A0 3000   
    // SQDMLSLB <Zda>.D, <Zn>.S, <Zm>.S[<imm>]                                           SVE_FJ_3B           01000100111immmm 0011i0nnnnnddddd     44E0 3000   

INST3(sqdmlslt,          "sqdmlslt",              0,                       IF_SVE_3Q,               0x44006C00,      0x44A03400,      0x44E03400       )
    // SQDMLSLT <Zda>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                          SVE_EO_3A           01000100xx0mmmmm 011011nnnnnddddd     4400 6C00   
    // SQDMLSLT <Zda>.S, <Zn>.H, <Zm>.H[<imm>]                                           SVE_FJ_3A           01000100101iimmm 0011i1nnnnnddddd     44A0 3400   
    // SQDMLSLT <Zda>.D, <Zn>.S, <Zm>.S[<imm>]                                           SVE_FJ_3B           01000100111immmm 0011i1nnnnnddddd     44E0 3400   


//    enum               name                     info                                              SVE_FE_3A        SVE_FE_3B        SVE_FN_3A        
INST3(smullb,            "smullb",                0,                       IF_SVE_3R,               0x44A0C000,      0x44E0C000,      0x45007000       )
    // SMULLB  <Zd>.S, <Zn>.H, <Zm>.H[<imm>]                                             SVE_FE_3A           01000100101iimmm 1100i0nnnnnddddd     44A0 C000   
    // SMULLB  <Zd>.D, <Zn>.S, <Zm>.S[<imm>]                                             SVE_FE_3B           01000100111immmm 1100i0nnnnnddddd     44E0 C000   
    // SMULLB  <Zd>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                            SVE_FN_3A           01000101xx0mmmmm 011100nnnnnddddd     4500 7000   

INST3(smullt,            "smullt",                0,                       IF_SVE_3R,               0x44A0C400,      0x44E0C400,      0x45007400       )
    // SMULLT  <Zd>.S, <Zn>.H, <Zm>.H[<imm>]                                             SVE_FE_3A           01000100101iimmm 1100i1nnnnnddddd     44A0 C400   
    // SMULLT  <Zd>.D, <Zn>.S, <Zm>.S[<imm>]                                             SVE_FE_3B           01000100111immmm 1100i1nnnnnddddd     44E0 C400   
    // SMULLT  <Zd>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                            SVE_FN_3A           01000101xx0mmmmm 011101nnnnnddddd     4500 7400   

INST3(umullb,            "umullb",                0,                       IF_SVE_3R,               0x44A0D000,      0x44E0D000,      0x45007800       )
    // UMULLB  <Zd>.S, <Zn>.H, <Zm>.H[<imm>]                                             SVE_FE_3A           01000100101iimmm 1101i0nnnnnddddd     44A0 D000   
    // UMULLB  <Zd>.D, <Zn>.S, <Zm>.S[<imm>]                                             SVE_FE_3B           01000100111immmm 1101i0nnnnnddddd     44E0 D000   
    // UMULLB  <Zd>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                            SVE_FN_3A           01000101xx0mmmmm 011110nnnnnddddd     4500 7800   

INST3(umullt,            "umullt",                0,                       IF_SVE_3R,               0x44A0D400,      0x44E0D400,      0x45007C00       )
    // UMULLT  <Zd>.S, <Zn>.H, <Zm>.H[<imm>]                                             SVE_FE_3A           01000100101iimmm 1101i1nnnnnddddd     44A0 D400   
    // UMULLT  <Zd>.D, <Zn>.S, <Zm>.S[<imm>]                                             SVE_FE_3B           01000100111immmm 1101i1nnnnnddddd     44E0 D400   
    // UMULLT  <Zd>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                            SVE_FN_3A           01000101xx0mmmmm 011111nnnnnddddd     4500 7C00   


//    enum               name                     info                                              SVE_FH_3A        SVE_FH_3B        SVE_FN_3A        
INST3(sqdmullb,          "sqdmullb",              0,                       IF_SVE_3S,               0x44A0E000,      0x44E0E000,      0x45006000       )
    // SQDMULLB <Zd>.S, <Zn>.H, <Zm>.H[<imm>]                                            SVE_FH_3A           01000100101iimmm 1110i0nnnnnddddd     44A0 E000   
    // SQDMULLB <Zd>.D, <Zn>.S, <Zm>.S[<imm>]                                            SVE_FH_3B           01000100111immmm 1110i0nnnnnddddd     44E0 E000   
    // SQDMULLB <Zd>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                           SVE_FN_3A           01000101xx0mmmmm 011000nnnnnddddd     4500 6000   

INST3(sqdmullt,          "sqdmullt",              0,                       IF_SVE_3S,               0x44A0E400,      0x44E0E400,      0x45006400       )
    // SQDMULLT <Zd>.S, <Zn>.H, <Zm>.H[<imm>]                                            SVE_FH_3A           01000100101iimmm 1110i1nnnnnddddd     44A0 E400   
    // SQDMULLT <Zd>.D, <Zn>.S, <Zm>.S[<imm>]                                            SVE_FH_3B           01000100111immmm 1110i1nnnnnddddd     44E0 E400   
    // SQDMULLT <Zd>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                           SVE_FN_3A           01000101xx0mmmmm 011001nnnnnddddd     4500 6400   


//    enum               name                     info                                              SVE_GX_3C        SVE_HK_3B        SVE_HL_3B        
INST3(bfmul,             "bfmul",                 0,                       IF_SVE_3T,               0x64202800,      0x65000800,      0x65028000       )
    // BFMUL   <Zd>.H, <Zn>.H, <Zm>.H[<imm>]                                             SVE_GX_3C           011001000i1iimmm 001010nnnnnddddd     6420 2800   
    // BFMUL   <Zd>.H, <Zn>.H, <Zm>.H                                                    SVE_HK_3B           01100101000mmmmm 000010nnnnnddddd     6500 0800   
    // BFMUL   <Zdn>.H, <Pg>/M, <Zdn>.H, <Zm>.H                                          SVE_HL_3B           0110010100000010 100gggmmmmmddddd     6502 8000   


//    enum               name                     info                                              SVE_IM_3A        SVE_IN_4A        SVE_IX_4A        
INST3(ldnt1d,            "ldnt1d",                0,                       IF_SVE_3U,               0xA580E000,      0xA580C000,      0xC580C000       )
    // LDNT1D  {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]                            SVE_IM_3A           101001011000iiii 111gggnnnnnttttt     A580 E000   
    // LDNT1D  {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #3]                                SVE_IN_4A           10100101100mmmmm 110gggnnnnnttttt     A580 C000   
    // LDNT1D  {<Zt>.D }, <Pg>/Z, [<Zn>.D{, <Xm>}]                                       SVE_IX_4A           11000101100mmmmm 110gggnnnnnttttt     C580 C000   


//    enum               name                     info                                              SVE_JA_4A        SVE_JB_4A        SVE_JM_3A        
INST3(stnt1d,            "stnt1d",                0,                       IF_SVE_3V,               0xE5802000,      0xE5806000,      0xE590E000       )
    // STNT1D  {<Zt>.D }, <Pg>, [<Zn>.D{, <Xm>}]                                         SVE_JA_4A           11100101100mmmmm 001gggnnnnnttttt     E580 2000   
    // STNT1D  {<Zt>.D }, <Pg>, [<Xn|SP>, <Xm>, LSL #3]                                  SVE_JB_4A           11100101100mmmmm 011gggnnnnnttttt     E580 6000   
    // STNT1D  {<Zt>.D }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]                              SVE_JM_3A           111001011001iiii 111gggnnnnnttttt     E590 E000   


//    enum               name                     info                                              SVE_ID_2A        SVE_IE_2A                   
INST2(ldr,               "ldr",                   0,                       IF_SVE_2AA,              0x85800000,      0x85804000                  )
    // LDR     <Pt>, [<Xn|SP>{, #<imm>, MUL VL}]                                         SVE_ID_2A           1000010110iiiiii 000iiinnnnn0TTTT     8580 0000   
    // LDR     <Zt>, [<Xn|SP>{, #<imm>, MUL VL}]                                         SVE_IE_2A           1000010110iiiiii 010iiinnnnnttttt     8580 4000   


//    enum               name                     info                                              SVE_JG_2A        SVE_JH_2A                   
INST2(str,               "str",                   0,                       IF_SVE_2AB,              0xE5800000,      0xE5804000                  )
    // STR     <Pt>, [<Xn|SP>{, #<imm>, MUL VL}]                                         SVE_JG_2A           1110010110iiiiii 000iiinnnnn0TTTT     E580 0000   
    // STR     <Zt>, [<Xn|SP>{, #<imm>, MUL VL}]                                         SVE_JH_2A           1110010110iiiiii 010iiinnnnnttttt     E580 4000   


//    enum               name                     info                                              SVE_AD_3A        SVE_ED_1A                   
INST2(smax,              "smax",                  0,                       IF_SVE_2AC,              0x04080000,      0x2528C000                  )
    // SMAX    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_AD_3A           00000100xx001000 000gggmmmmmddddd     0408 0000   
    // SMAX    <Zdn>.<T>, <Zdn>.<T>, #<imm>                                              SVE_ED_1A           00100101xx101000 110iiiiiiiiddddd     2528 C000   

INST2(smin,              "smin",                  0,                       IF_SVE_2AC,              0x040A0000,      0x252AC000                  )
    // SMIN    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_AD_3A           00000100xx001010 000gggmmmmmddddd     040A 0000   
    // SMIN    <Zdn>.<T>, <Zdn>.<T>, #<imm>                                              SVE_ED_1A           00100101xx101010 110iiiiiiiiddddd     252A C000   

INST2(umax,              "umax",                  0,                       IF_SVE_2AC,              0x04090000,      0x2529C000                  )
    // UMAX    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_AD_3A           00000100xx001001 000gggmmmmmddddd     0409 0000   
    // UMAX    <Zdn>.<T>, <Zdn>.<T>, #<imm>                                              SVE_ED_1A           00100101xx101001 110iiiiiiiiddddd     2529 C000   

INST2(umin,              "umin",                  0,                       IF_SVE_2AC,              0x040B0000,      0x252BC000                  )
    // UMIN    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_AD_3A           00000100xx001011 000gggmmmmmddddd     040B 0000   
    // UMIN    <Zdn>.<T>, <Zdn>.<T>, #<imm>                                              SVE_ED_1A           00100101xx101011 110iiiiiiiiddddd     252B C000   


//    enum               name                     info                                              SVE_AB_3B        SVE_AT_3B                   
INST2(addpt,             "addpt",                 0,                       IF_SVE_2AD,              0x04C40000,      0x04E00800                  )
    // ADDPT   <Zdn>.D, <Pg>/M, <Zdn>.D, <Zm>.D                                          SVE_AB_3B           0000010011000100 000gggmmmmmddddd     04C4 0000   
    // ADDPT   <Zd>.D, <Zn>.D, <Zm>.D                                                    SVE_AT_3B           00000100111mmmmm 000010nnnnnddddd     04E0 0800   

INST2(subpt,             "subpt",                 0,                       IF_SVE_2AD,              0x04C50000,      0x04E00C00                  )
    // SUBPT   <Zdn>.D, <Pg>/M, <Zdn>.D, <Zm>.D                                          SVE_AB_3B           0000010011000101 000gggmmmmmddddd     04C5 0000   
    // SUBPT   <Zd>.D, <Zn>.D, <Zm>.D                                                    SVE_AT_3B           00000100111mmmmm 000011nnnnnddddd     04E0 0C00   


//    enum               name                     info                                              SVE_CG_2A        SVE_CJ_2A                   
INST2(rev,               "rev",                   0,                       IF_SVE_2AE,              0x05383800,      0x05344000                  )
    // REV     <Zd>.<T>, <Zn>.<T>                                                        SVE_CG_2A           00000101xx111000 001110nnnnnddddd     0538 3800   
    // REV     <Pd>.<T>, <Pn>.<T>                                                        SVE_CJ_2A           00000101xx110100 0100000NNNN0DDDD     0534 4000   


//    enum               name                     info                                              SVE_AE_3A        SVE_BD_3A                   
INST2(smulh,             "smulh",                 0,                       IF_SVE_2AF,              0x04120000,      0x04206800                  )
    // SMULH   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_AE_3A           00000100xx010010 000gggmmmmmddddd     0412 0000   
    // SMULH   <Zd>.<T>, <Zn>.<T>, <Zm>.<T>                                              SVE_BD_3A           00000100xx1mmmmm 011010nnnnnddddd     0420 6800   

INST2(umulh,             "umulh",                 0,                       IF_SVE_2AF,              0x04130000,      0x04206C00                  )
    // UMULH   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_AE_3A           00000100xx010011 000gggmmmmmddddd     0413 0000   
    // UMULH   <Zd>.<T>, <Zn>.<T>, <Zm>.<T>                                              SVE_BD_3A           00000100xx1mmmmm 011011nnnnnddddd     0420 6C00   


//    enum               name                     info                                              SVE_BS_1A        SVE_CZ_4A                   
INST2(orn,               "orn",                   0,                       IF_SVE_2AG,              0x05000000,      0x25804010                  )
    // ORN     <Zdn>.<T>, <Zdn>.<T>, #<const>                                            SVE_BS_1A           00000101000000ii iiiiiiiiiiiddddd     0500 0000   
    // ORN     <Pd>.B, <Pg>/Z, <Pn>.B, <Pm>.B                                            SVE_CZ_4A           001001011000MMMM 01gggg0NNNN1DDDD     2580 4010   


//    enum               name                     info                                              SVE_BQ_2A        SVE_BQ_2B                   
INST2(ext,               "ext",                   0,                       IF_SVE_2AH,              0x05600000,      0x05200000                  )
    // EXT     <Zd>.B, {<Zn1>.B, <Zn2>.B }, #<imm>                                       SVE_BQ_2A           00000101011iiiii 000iiinnnnnddddd     0560 0000   
    // EXT     <Zdn>.B, <Zdn>.B, <Zm>.B, #<imm>                                          SVE_BQ_2B           00000101001iiiii 000iiimmmmmddddd     0520 0000   


//    enum               name                     info                                              SVE_AM_2A        SVE_EU_3A                   
INST2(sqshl,             "sqshl",                 0,                       IF_SVE_2AI,              0x04068000,      0x44088000                  )
    // SQSHL   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, #<const>                                    SVE_AM_2A           00000100xx000110 100gggxxiiiddddd     0406 8000   
    // SQSHL   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_EU_3A           01000100xx001000 100gggmmmmmddddd     4408 8000   

INST2(uqshl,             "uqshl",                 0,                       IF_SVE_2AI,              0x04078000,      0x44098000                  )
    // UQSHL   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, #<const>                                    SVE_AM_2A           00000100xx000111 100gggxxiiiddddd     0407 8000   
    // UQSHL   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_EU_3A           01000100xx001001 100gggmmmmmddddd     4409 8000   


//    enum               name                     info                                              SVE_HI_3A        SVE_HT_4A                   
INST2(fcmeq,             "fcmeq",                 0,                       IF_SVE_2AJ,              0x65122000,      0x65006000                  )
    // FCMEQ   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, #0.0                                          SVE_HI_3A           01100101xx010010 001gggnnnnn0DDDD     6512 2000   
    // FCMEQ   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.<T>                                      SVE_HT_4A           01100101xx0mmmmm 011gggnnnnn0DDDD     6500 6000   

INST2(fcmge,             "fcmge",                 0,                       IF_SVE_2AJ,              0x65102000,      0x65004000                  )
    // FCMGE   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, #0.0                                          SVE_HI_3A           01100101xx010000 001gggnnnnn0DDDD     6510 2000   
    // FCMGE   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.<T>                                      SVE_HT_4A           01100101xx0mmmmm 010gggnnnnn0DDDD     6500 4000   

INST2(fcmgt,             "fcmgt",                 0,                       IF_SVE_2AJ,              0x65102010,      0x65004010                  )
    // FCMGT   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, #0.0                                          SVE_HI_3A           01100101xx010000 001gggnnnnn1DDDD     6510 2010   
    // FCMGT   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.<T>                                      SVE_HT_4A           01100101xx0mmmmm 010gggnnnnn1DDDD     6500 4010   

INST2(fcmle,             "fcmle",                 0,                       IF_SVE_2AJ,              0x65112010,      0x65004000                  )
    // FCMLE   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, #0.0                                          SVE_HI_3A           01100101xx010001 001gggnnnnn1DDDD     6511 2010   
    // FCMLE   <Pd>.<T>, <Pg>/Z, <Zm>.<T>, <Zn>.<T>                                      SVE_HT_4A           01100101xx0mmmmm 010gggnnnnn0DDDD     6500 4000   

INST2(fcmlt,             "fcmlt",                 0,                       IF_SVE_2AJ,              0x65112000,      0x65004010                  )
    // FCMLT   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, #0.0                                          SVE_HI_3A           01100101xx010001 001gggnnnnn0DDDD     6511 2000   
    // FCMLT   <Pd>.<T>, <Pg>/Z, <Zm>.<T>, <Zn>.<T>                                      SVE_HT_4A           01100101xx0mmmmm 010gggnnnnn1DDDD     6500 4010   

INST2(fcmne,             "fcmne",                 0,                       IF_SVE_2AJ,              0x65132000,      0x65006010                  )
    // FCMNE   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, #0.0                                          SVE_HI_3A           01100101xx010011 001gggnnnnn0DDDD     6513 2000   
    // FCMNE   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.<T>                                      SVE_HT_4A           01100101xx0mmmmm 011gggnnnnn1DDDD     6500 6010   


//    enum               name                     info                                              SVE_BZ_3A        SVE_BZ_3A_A                 
INST2(tbl,               "tbl",                   0,                       IF_SVE_2AK,              0x05203000,      0x05202800                  )
    // TBL     <Zd>.<T>, {<Zn>.<T>}, <Zm>.<T>                                            SVE_BZ_3A           00000101xx1mmmmm 001100nnnnnddddd     0520 3000   
    // TBL     <Zd>.<T>, {<Zn1>.<T>, <Zn2>.<T>}, <Zm>.<T>                                SVE_BZ_3A_A         00000101xx1mmmmm 001010nnnnnddddd     0520 2800   


//    enum               name                     info                                              SVE_GG_3A        SVE_GG_3B                   
INST2(luti2,             "luti2",                 0,                       IF_SVE_2AL,              0x4520B000,      0x4520A800                  )
    // LUTI2   <Zd>.B, {<Zn>.B }, <Zm>[<index>]                                          SVE_GG_3A           01000101ii1mmmmm 101100nnnnnddddd     4520 B000   
    // LUTI2   <Zd>.H, {<Zn>.H }, <Zm>[<index>]                                          SVE_GG_3B           01000101ii1mmmmm 101i10nnnnnddddd     4520 A800   


//    enum               name                     info                                              SVE_HL_3A        SVE_HM_2A                   
INST2(fmax,              "fmax",                  0,                       IF_SVE_2AM,              0x65068000,      0x651E8000                  )
    // FMAX    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_HL_3A           01100101xx000110 100gggmmmmmddddd     6506 8000   
    // FMAX    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <const>                                     SVE_HM_2A           01100101xx011110 100ggg0000iddddd     651E 8000   

INST2(fmaxnm,            "fmaxnm",                0,                       IF_SVE_2AM,              0x65048000,      0x651C8000                  )
    // FMAXNM  <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_HL_3A           01100101xx000100 100gggmmmmmddddd     6504 8000   
    // FMAXNM  <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <const>                                     SVE_HM_2A           01100101xx011100 100ggg0000iddddd     651C 8000   

INST2(fmin,              "fmin",                  0,                       IF_SVE_2AM,              0x65078000,      0x651F8000                  )
    // FMIN    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_HL_3A           01100101xx000111 100gggmmmmmddddd     6507 8000   
    // FMIN    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <const>                                     SVE_HM_2A           01100101xx011111 100ggg0000iddddd     651F 8000   

INST2(fminnm,            "fminnm",                0,                       IF_SVE_2AM,              0x65058000,      0x651D8000                  )
    // FMINNM  <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_HL_3A           01100101xx000101 100gggmmmmmddddd     6505 8000   
    // FMINNM  <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <const>                                     SVE_HM_2A           01100101xx011101 100ggg0000iddddd     651D 8000   

INST2(fsubr,             "fsubr",                 0,                       IF_SVE_2AM,              0x65038000,      0x651B8000                  )
    // FSUBR   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_HL_3A           01100101xx000011 100gggmmmmmddddd     6503 8000   
    // FSUBR   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <const>                                     SVE_HM_2A           01100101xx011011 100ggg0000iddddd     651B 8000   


//    enum               name                     info                                              SVE_EI_3A        SVE_EZ_3A                   
INST2(usdot,             "usdot",                 0,                       IF_SVE_2AN,              0x44807800,      0x44A01800                  )
    // USDOT   <Zda>.S, <Zn>.B, <Zm>.B                                                   SVE_EI_3A           01000100100mmmmm 011110nnnnnddddd     4480 7800   
    // USDOT   <Zda>.S, <Zn>.B, <Zm>.B[<imm>]                                            SVE_EZ_3A           01000100101iimmm 000110nnnnnddddd     44A0 1800   


//    enum               name                     info                                              SVE_GT_4A        SVE_GV_3A                   
INST2(fcmla,             "fcmla",                 0,                       IF_SVE_2AO,              0x64000000,      0x64E01000                  )
    // FCMLA   <Zda>.<T>, <Pg>/M, <Zn>.<T>, <Zm>.<T>, <const>                            SVE_GT_4A           01100100xx0mmmmm 0rrgggnnnnnddddd     6400 0000   
    // FCMLA   <Zda>.S, <Zn>.S, <Zm>.S[<imm>], <const>                                   SVE_GV_3A           01100100111immmm 0001rrnnnnnddddd     64E0 1000   


//    enum               name                     info                                              SVE_GY_3B        SVE_HA_3A                   
INST2(bfdot,             "bfdot",                 0,                       IF_SVE_2AP,              0x64604000,      0x64608000                  )
    // BFDOT   <Zda>.S, <Zn>.H, <Zm>.H[<imm>]                                            SVE_GY_3B           01100100011iimmm 010000nnnnnddddd     6460 4000   
    // BFDOT   <Zda>.S, <Zn>.H, <Zm>.H                                                   SVE_HA_3A           01100100011mmmmm 100000nnnnnddddd     6460 8000   


//    enum               name                     info                                              SVE_GO_3A        SVE_HC_3A                   
INST2(fmlallbb,          "fmlallbb",              0,                       IF_SVE_2AQ,              0x64208800,      0x6420C000                  )
    // FMLALLBB <Zda>.S, <Zn>.B, <Zm>.B                                                  SVE_GO_3A           01100100001mmmmm 100010nnnnnddddd     6420 8800   
    // FMLALLBB <Zda>.S, <Zn>.B, <Zm>.B[<imm>]                                           SVE_HC_3A           01100100001iimmm 1100iinnnnnddddd     6420 C000   

INST2(fmlallbt,          "fmlallbt",              0,                       IF_SVE_2AQ,              0x64209800,      0x6460C000                  )
    // FMLALLBT <Zda>.S, <Zn>.B, <Zm>.B                                                  SVE_GO_3A           01100100001mmmmm 100110nnnnnddddd     6420 9800   
    // FMLALLBT <Zda>.S, <Zn>.B, <Zm>.B[<imm>]                                           SVE_HC_3A           01100100011iimmm 1100iinnnnnddddd     6460 C000   

INST2(fmlalltb,          "fmlalltb",              0,                       IF_SVE_2AQ,              0x6420A800,      0x64A0C000                  )
    // FMLALLTB <Zda>.S, <Zn>.B, <Zm>.B                                                  SVE_GO_3A           01100100001mmmmm 101010nnnnnddddd     6420 A800   
    // FMLALLTB <Zda>.S, <Zn>.B, <Zm>.B[<imm>]                                           SVE_HC_3A           01100100101iimmm 1100iinnnnnddddd     64A0 C000   

INST2(fmlalltt,          "fmlalltt",              0,                       IF_SVE_2AQ,              0x6420B800,      0x64E0C000                  )
    // FMLALLTT <Zda>.S, <Zn>.B, <Zm>.B                                                  SVE_GO_3A           01100100001mmmmm 101110nnnnnddddd     6420 B800   
    // FMLALLTT <Zda>.S, <Zn>.B, <Zm>.B[<imm>]                                           SVE_HC_3A           01100100111iimmm 1100iinnnnnddddd     64E0 C000   


//    enum               name                     info                                              SVE_AP_3A        SVE_CZ_4A                   
INST2(not,               "not",                   0,                       IF_SVE_2AR,              0x041EA000,      0x25004200                  )
    // NOT     <Zd>.<T>, <Pg>/M, <Zn>.<T>                                                SVE_AP_3A           00000100xx011110 101gggnnnnnddddd     041E A000   
    // NOT     <Pd>.B, <Pg>/Z, <Pn>.B                                                    SVE_CZ_4A           001001010000MMMM 01gggg1NNNN0DDDD     2500 4200   


//    enum               name                     info                                              SVE_HO_3A        SVE_HO_3A_B                 
INST2(fcvt,              "fcvt",                  0,                       IF_SVE_2AS,              0x65CBA000,      0x65CAA000                  )
    // FCVT    <Zd>.D, <Pg>/M, <Zn>.S                                                    SVE_HO_3A           0110010111001011 101gggnnnnnddddd     65CB A000   
    // FCVT    <Zd>.S, <Pg>/M, <Zn>.D                                                    SVE_HO_3A_B         0110010111001010 101gggnnnnnddddd     65CA A000   


//    enum               name                     info                                              SVE_AB_3A        SVE_EC_1A                   
INST2(subr,              "subr",                  0,                       IF_SVE_2AT,              0x04030000,      0x2523C000                  )
    // SUBR    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_AB_3A           00000100xx000011 000gggmmmmmddddd     0403 0000   
    // SUBR    <Zdn>.<T>, <Zdn>.<T>, #<imm>{, <shift>}                                   SVE_EC_1A           00100101xx100011 11hiiiiiiiiddddd     2523 C000   


//    enum               name                     info                                              SVE_AH_3A        SVE_BI_2A                   
INST2(movprfx,           "movprfx",               0,                       IF_SVE_2AU,              0x04102000,      0x0420BC00                  )
    // MOVPRFX <Zd>.<T>, <Pg>/<ZM>, <Zn>.<T>                                             SVE_AH_3A           00000100xx01000M 001gggnnnnnddddd     0410 2000   
    // MOVPRFX <Zd>, <Zn>                                                                SVE_BI_2A           0000010000100000 101111nnnnnddddd     0420 BC00   


//    enum               name                     info                                              SVE_BM_1A        SVE_BN_1A                   
INST2(decd,              "decd",                  0,                       IF_SVE_2AV,              0x04F0E400,      0x04F0C400                  )
    // DECD    <Xdn>{, <pattern>{, MUL #<imm>}}                                          SVE_BM_1A           000001001111iiii 111001pppppddddd     04F0 E400   
    // DECD    <Zdn>.D{, <pattern>{, MUL #<imm>}}                                        SVE_BN_1A           000001001111iiii 110001pppppddddd     04F0 C400   

INST2(dech,              "dech",                  0,                       IF_SVE_2AV,              0x0470E400,      0x0470C400                  )
    // DECH    <Xdn>{, <pattern>{, MUL #<imm>}}                                          SVE_BM_1A           000001000111iiii 111001pppppddddd     0470 E400   
    // DECH    <Zdn>.H{, <pattern>{, MUL #<imm>}}                                        SVE_BN_1A           000001000111iiii 110001pppppddddd     0470 C400   

INST2(decw,              "decw",                  0,                       IF_SVE_2AV,              0x04B0E400,      0x04B0C400                  )
    // DECW    <Xdn>{, <pattern>{, MUL #<imm>}}                                          SVE_BM_1A           000001001011iiii 111001pppppddddd     04B0 E400   
    // DECW    <Zdn>.S{, <pattern>{, MUL #<imm>}}                                        SVE_BN_1A           000001001011iiii 110001pppppddddd     04B0 C400   

INST2(incd,              "incd",                  0,                       IF_SVE_2AV,              0x04F0E000,      0x04F0C000                  )
    // INCD    <Xdn>{, <pattern>{, MUL #<imm>}}                                          SVE_BM_1A           000001001111iiii 111000pppppddddd     04F0 E000   
    // INCD    <Zdn>.D{, <pattern>{, MUL #<imm>}}                                        SVE_BN_1A           000001001111iiii 110000pppppddddd     04F0 C000   

INST2(inch,              "inch",                  0,                       IF_SVE_2AV,              0x0470E000,      0x0470C000                  )
    // INCH    <Xdn>{, <pattern>{, MUL #<imm>}}                                          SVE_BM_1A           000001000111iiii 111000pppppddddd     0470 E000   
    // INCH    <Zdn>.H{, <pattern>{, MUL #<imm>}}                                        SVE_BN_1A           000001000111iiii 110000pppppddddd     0470 C000   

INST2(incw,              "incw",                  0,                       IF_SVE_2AV,              0x04B0E000,      0x04B0C000                  )
    // INCW    <Xdn>{, <pattern>{, MUL #<imm>}}                                          SVE_BM_1A           000001001011iiii 111000pppppddddd     04B0 E000   
    // INCW    <Zdn>.S{, <pattern>{, MUL #<imm>}}                                        SVE_BN_1A           000001001011iiii 110000pppppddddd     04B0 C000   


//    enum               name                     info                                              SVE_BO_1A        SVE_BP_1A                   
INST2(sqdecd,            "sqdecd",                0,                       IF_SVE_2AW,              0x04E0F800,      0x04E0C800                  )
    // SQDECD  <Xdn>, <Wdn>{, <pattern>{, MUL #<imm>}}                                   SVE_BO_1A           00000100111Xiiii 111110pppppddddd     04E0 F800   
    // SQDECD  <Zdn>.D{, <pattern>{, MUL #<imm>}}                                        SVE_BP_1A           000001001110iiii 110010pppppddddd     04E0 C800   

INST2(sqdech,            "sqdech",                0,                       IF_SVE_2AW,              0x0460F800,      0x0460C800                  )
    // SQDECH  <Xdn>, <Wdn>{, <pattern>{, MUL #<imm>}}                                   SVE_BO_1A           00000100011Xiiii 111110pppppddddd     0460 F800   
    // SQDECH  <Zdn>.H{, <pattern>{, MUL #<imm>}}                                        SVE_BP_1A           000001000110iiii 110010pppppddddd     0460 C800   

INST2(sqdecw,            "sqdecw",                0,                       IF_SVE_2AW,              0x04A0F800,      0x04A0C800                  )
    // SQDECW  <Xdn>, <Wdn>{, <pattern>{, MUL #<imm>}}                                   SVE_BO_1A           00000100101Xiiii 111110pppppddddd     04A0 F800   
    // SQDECW  <Zdn>.S{, <pattern>{, MUL #<imm>}}                                        SVE_BP_1A           000001001010iiii 110010pppppddddd     04A0 C800   

INST2(sqincd,            "sqincd",                0,                       IF_SVE_2AW,              0x04E0F000,      0x04E0C000                  )
    // SQINCD  <Xdn>, <Wdn>{, <pattern>{, MUL #<imm>}}                                   SVE_BO_1A           00000100111Xiiii 111100pppppddddd     04E0 F000   
    // SQINCD  <Zdn>.D{, <pattern>{, MUL #<imm>}}                                        SVE_BP_1A           000001001110iiii 110000pppppddddd     04E0 C000   

INST2(sqinch,            "sqinch",                0,                       IF_SVE_2AW,              0x0460F000,      0x0460C000                  )
    // SQINCH  <Xdn>, <Wdn>{, <pattern>{, MUL #<imm>}}                                   SVE_BO_1A           00000100011Xiiii 111100pppppddddd     0460 F000   
    // SQINCH  <Zdn>.H{, <pattern>{, MUL #<imm>}}                                        SVE_BP_1A           000001000110iiii 110000pppppddddd     0460 C000   

INST2(sqincw,            "sqincw",                0,                       IF_SVE_2AW,              0x04A0F000,      0x04A0C000                  )
    // SQINCW  <Xdn>, <Wdn>{, <pattern>{, MUL #<imm>}}                                   SVE_BO_1A           00000100101Xiiii 111100pppppddddd     04A0 F000   
    // SQINCW  <Zdn>.S{, <pattern>{, MUL #<imm>}}                                        SVE_BP_1A           000001001010iiii 110000pppppddddd     04A0 C000   

INST2(uqdecd,            "uqdecd",                0,                       IF_SVE_2AW,              0x04E0FC00,      0x04E0CC00                  )
    // UQDECD  <Wdn>{, <pattern>{, MUL #<imm>}}                                          SVE_BO_1A           00000100111Xiiii 111111pppppddddd     04E0 FC00   
    // UQDECD  <Zdn>.D{, <pattern>{, MUL #<imm>}}                                        SVE_BP_1A           000001001110iiii 110011pppppddddd     04E0 CC00   

INST2(uqdech,            "uqdech",                0,                       IF_SVE_2AW,              0x0460FC00,      0x0460CC00                  )
    // UQDECH  <Wdn>{, <pattern>{, MUL #<imm>}}                                          SVE_BO_1A           00000100011Xiiii 111111pppppddddd     0460 FC00   
    // UQDECH  <Zdn>.H{, <pattern>{, MUL #<imm>}}                                        SVE_BP_1A           000001000110iiii 110011pppppddddd     0460 CC00   

INST2(uqdecw,            "uqdecw",                0,                       IF_SVE_2AW,              0x04A0FC00,      0x04A0CC00                  )
    // UQDECW  <Wdn>{, <pattern>{, MUL #<imm>}}                                          SVE_BO_1A           00000100101Xiiii 111111pppppddddd     04A0 FC00   
    // UQDECW  <Zdn>.S{, <pattern>{, MUL #<imm>}}                                        SVE_BP_1A           000001001010iiii 110011pppppddddd     04A0 CC00   

INST2(uqincd,            "uqincd",                0,                       IF_SVE_2AW,              0x04E0F400,      0x04E0C400                  )
    // UQINCD  <Wdn>{, <pattern>{, MUL #<imm>}}                                          SVE_BO_1A           00000100111Xiiii 111101pppppddddd     04E0 F400   
    // UQINCD  <Zdn>.D{, <pattern>{, MUL #<imm>}}                                        SVE_BP_1A           000001001110iiii 110001pppppddddd     04E0 C400   

INST2(uqinch,            "uqinch",                0,                       IF_SVE_2AW,              0x0460F400,      0x0460C400                  )
    // UQINCH  <Wdn>{, <pattern>{, MUL #<imm>}}                                          SVE_BO_1A           00000100011Xiiii 111101pppppddddd     0460 F400   
    // UQINCH  <Zdn>.H{, <pattern>{, MUL #<imm>}}                                        SVE_BP_1A           000001000110iiii 110001pppppddddd     0460 C400   

INST2(uqincw,            "uqincw",                0,                       IF_SVE_2AW,              0x04A0F400,      0x04A0C400                  )
    // UQINCW  <Wdn>{, <pattern>{, MUL #<imm>}}                                          SVE_BO_1A           00000100101Xiiii 111101pppppddddd     04A0 F400   
    // UQINCW  <Zdn>.S{, <pattern>{, MUL #<imm>}}                                        SVE_BP_1A           000001001010iiii 110001pppppddddd     04A0 C400   


//    enum               name                     info                                              SVE_CC_2A        SVE_CD_2A                   
INST2(insr,              "insr",                  0,                       IF_SVE_2AX,              0x05343800,      0x05243800                  )
    // INSR    <Zdn>.<T>, <V><m>                                                         SVE_CC_2A           00000101xx110100 001110mmmmmddddd     0534 3800   
    // INSR    <Zdn>.<T>, <R><m>                                                         SVE_CD_2A           00000101xx100100 001110mmmmmddddd     0524 3800   


//    enum               name                     info                                              SVE_CR_3A        SVE_CS_3A                   
INST2(lasta,             "lasta",                 0,                       IF_SVE_2AY,              0x05228000,      0x0520A000                  )
    // LASTA   <V><d>, <Pg>, <Zn>.<T>                                                    SVE_CR_3A           00000101xx100010 100gggnnnnnddddd     0522 8000   
    // LASTA   <R><d>, <Pg>, <Zn>.<T>                                                    SVE_CS_3A           00000101xx100000 101gggnnnnnddddd     0520 A000   

INST2(lastb,             "lastb",                 0,                       IF_SVE_2AY,              0x05238000,      0x0521A000                  )
    // LASTB   <V><d>, <Pg>, <Zn>.<T>                                                    SVE_CR_3A           00000101xx100011 100gggnnnnnddddd     0523 8000   
    // LASTB   <R><d>, <Pg>, <Zn>.<T>                                                    SVE_CS_3A           00000101xx100001 101gggnnnnnddddd     0521 A000   


//    enum               name                     info                                              SVE_CV_3A        SVE_CV_3B                   
INST2(splice,            "splice",                0,                       IF_SVE_2AZ,              0x052D8000,      0x052C8000                  )
    // SPLICE  <Zd>.<T>, <Pv>, {<Zn1>.<T>, <Zn2>.<T>}                                    SVE_CV_3A           00000101xx101101 100VVVnnnnnddddd     052D 8000   
    // SPLICE  <Zdn>.<T>, <Pv>, <Zdn>.<T>, <Zm>.<T>                                      SVE_CV_3B           00000101xx101100 100VVVmmmmmddddd     052C 8000   


//    enum               name                     info                                              SVE_CW_4A        SVE_CZ_4A                   
INST2(sel,               "sel",                   0,                       IF_SVE_2BA,              0x0520C000,      0x25004210                  )
    // SEL     <Zd>.<T>, <Pv>, <Zn>.<T>, <Zm>.<T>                                        SVE_CW_4A           00000101xx1mmmmm 11VVVVnnnnnddddd     0520 C000   
    // SEL     <Pd>.B, <Pg>, <Pn>.B, <Pm>.B                                              SVE_CZ_4A           001001010000MMMM 01gggg1NNNN1DDDD     2500 4210   


//    enum               name                     info                                              SVE_CZ_4A        SVE_CZ_4A_A                 
INST2(movs,              "movs",                  0,                       IF_SVE_2BB,              0x25404000,      0x25C04000                  )
    // MOVS    <Pd>.B, <Pg>/Z, <Pn>.B                                                    SVE_CZ_4A           001001010100MMMM 01gggg0NNNN0DDDD     2540 4000   
    // MOVS    <Pd>.B, <Pn>.B                                                            SVE_CZ_4A_A         001001011100MMMM 01gggg0NNNN0DDDD     25C0 4000   


//    enum               name                     info                                              SVE_DE_1A        SVE_DZ_1A                   
INST2(ptrue,             "ptrue",                 0,                       IF_SVE_2BC,              0x2518E000,      0x25207810                  )
    // PTRUE   <Pd>.<T>{, <pattern>}                                                     SVE_DE_1A           00100101xx011000 111000ppppp0DDDD     2518 E000   
    // PTRUE   <PNd>.<T>                                                                 SVE_DZ_1A           00100101xx100000 0111100000010DDD     2520 7810   


//    enum               name                     info                                              SVE_DG_2A        SVE_DH_1A                   
INST2(rdffr,             "rdffr",                 0,                       IF_SVE_2BD,              0x2518F000,      0x2519F000                  )
    // RDFFR   <Pd>.B, <Pg>/Z                                                            SVE_DG_2A           0010010100011000 1111000gggg0DDDD     2518 F000   
    // RDFFR   <Pd>.B                                                                    SVE_DH_1A           0010010100011001 111100000000DDDD     2519 F000   


//    enum               name                     info                                              SVE_DK_3A        SVE_DL_2A                   
INST2(cntp,              "cntp",                  0,                       IF_SVE_2BE,              0x25208000,      0x25208200                  )
    // CNTP    <Xd>, <Pg>, <Pn>.<T>                                                      SVE_DK_3A           00100101xx100000 10gggg0NNNNddddd     2520 8000   
    // CNTP    <Xd>, <PNn>.<T>, <vl>                                                     SVE_DL_2A           00100101xx100000 10000l1NNNNddddd     2520 8200   


//    enum               name                     info                                              SVE_DM_2A        SVE_DN_2A                   
INST2(decp,              "decp",                  0,                       IF_SVE_2BF,              0x252D8800,      0x252D8000                  )
    // DECP    <Xdn>, <Pm>.<T>                                                           SVE_DM_2A           00100101xx101101 1000100MMMMddddd     252D 8800   
    // DECP    <Zdn>.<T>, <Pm>.<T>                                                       SVE_DN_2A           00100101xx101101 1000000MMMMddddd     252D 8000   

INST2(incp,              "incp",                  0,                       IF_SVE_2BF,              0x252C8800,      0x252C8000                  )
    // INCP    <Xdn>, <Pm>.<T>                                                           SVE_DM_2A           00100101xx101100 1000100MMMMddddd     252C 8800   
    // INCP    <Zdn>.<T>, <Pm>.<T>                                                       SVE_DN_2A           00100101xx101100 1000000MMMMddddd     252C 8000   


//    enum               name                     info                                              SVE_DO_2A        SVE_DP_2A                   
INST2(sqdecp,            "sqdecp",                0,                       IF_SVE_2BG,              0x252A8800,      0x252A8000                  )
    // SQDECP  <Xdn>, <Pm>.<T>, <Wdn>                                                    SVE_DO_2A           00100101xx101010 10001X0MMMMddddd     252A 8800   
    // SQDECP  <Zdn>.<T>, <Pm>.<T>                                                       SVE_DP_2A           00100101xx101010 1000000MMMMddddd     252A 8000   

INST2(sqincp,            "sqincp",                0,                       IF_SVE_2BG,              0x25288800,      0x25288000                  )
    // SQINCP  <Xdn>, <Pm>.<T>, <Wdn>                                                    SVE_DO_2A           00100101xx101000 10001X0MMMMddddd     2528 8800   
    // SQINCP  <Zdn>.<T>, <Pm>.<T>                                                       SVE_DP_2A           00100101xx101000 1000000MMMMddddd     2528 8000   

INST2(uqdecp,            "uqdecp",                0,                       IF_SVE_2BG,              0x252B8800,      0x252B8000                  )
    // UQDECP  <Wdn>, <Pm>.<T>                                                           SVE_DO_2A           00100101xx101011 10001X0MMMMddddd     252B 8800   
    // UQDECP  <Zdn>.<T>, <Pm>.<T>                                                       SVE_DP_2A           00100101xx101011 1000000MMMMddddd     252B 8000   

INST2(uqincp,            "uqincp",                0,                       IF_SVE_2BG,              0x25298800,      0x25298000                  )
    // UQINCP  <Wdn>, <Pm>.<T>                                                           SVE_DO_2A           00100101xx101001 10001X0MMMMddddd     2529 8800   
    // UQINCP  <Zdn>.<T>, <Pm>.<T>                                                       SVE_DP_2A           00100101xx101001 1000000MMMMddddd     2529 8000   


//    enum               name                     info                                              SVE_DW_2A        SVE_DW_2B                   
INST2(pext,              "pext",                  0,                       IF_SVE_2BH,              0x25207010,      0x25207410                  )
    // PEXT    <Pd>.<T>, <PNn>[<imm>]                                                    SVE_DW_2A           00100101xx100000 011100iiNNN1DDDD     2520 7010   
    // PEXT    {<Pd1>.<T>, <Pd2>.<T>}, <PNn>[<imm>]                                      SVE_DW_2B           00100101xx100000 0111010iNNN1DDDD     2520 7410   


//    enum               name                     info                                              SVE_FN_3A        SVE_FN_3B                   
INST2(pmullb,            "pmullb",                0,                       IF_SVE_2BI,              0x45006800,      0x45006800                  )
    // PMULLB  <Zd>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                            SVE_FN_3A           01000101xx0mmmmm 011010nnnnnddddd     4500 6800   
    // PMULLB  <Zd>.Q, <Zn>.D, <Zm>.D                                                    SVE_FN_3B           01000101000mmmmm 011010nnnnnddddd     4500 6800   

INST2(pmullt,            "pmullt",                0,                       IF_SVE_2BI,              0x45006C00,      0x45006C00                  )
    // PMULLT  <Zd>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                            SVE_FN_3A           01000101xx0mmmmm 011011nnnnnddddd     4500 6C00   
    // PMULLT  <Zd>.Q, <Zn>.D, <Zm>.D                                                    SVE_FN_3B           01000101000mmmmm 011011nnnnnddddd     4500 6C00   


//    enum               name                     info                                              SVE_GQ_3A        SVE_HG_2A                   
INST2(fcvtnt,            "fcvtnt",                0,                       IF_SVE_2BJ,              0x64CAA000,      0x650A3C00                  )
    // FCVTNT  <Zd>.S, <Pg>/M, <Zn>.D                                                    SVE_GQ_3A           0110010011001010 101gggnnnnnddddd     64CA A000   
    // FCVTNT  <Zd>.B, {<Zn1>.S-<Zn2>.S }                                                SVE_HG_2A           0110010100001010 001111nnnn0ddddd     650A 3C00   


//    enum               name                     info                                              SVE_GU_3C        SVE_HU_4B                   
INST2(bfmla,             "bfmla",                 0,                       IF_SVE_2BK,              0x64200800,      0x65200000                  )
    // BFMLA   <Zda>.H, <Zn>.H, <Zm>.H[<imm>]                                            SVE_GU_3C           011001000i1iimmm 000010nnnnnddddd     6420 0800   
    // BFMLA   <Zda>.H, <Pg>/M, <Zn>.H, <Zm>.H                                           SVE_HU_4B           01100101001mmmmm 000gggnnnnnddddd     6520 0000   

INST2(bfmls,             "bfmls",                 0,                       IF_SVE_2BK,              0x64200C00,      0x65202000                  )
    // BFMLS   <Zda>.H, <Zn>.H, <Zm>.H[<imm>]                                            SVE_GU_3C           011001000i1iimmm 000011nnnnnddddd     6420 0C00   
    // BFMLS   <Zda>.H, <Pg>/M, <Zn>.H, <Zm>.H                                           SVE_HU_4B           01100101001mmmmm 001gggnnnnnddddd     6520 2000   


//    enum               name                     info                                              SVE_GZ_3A        SVE_HB_3A                   
INST2(bfmlalb,           "bfmlalb",               0,                       IF_SVE_2BL,              0x64E04000,      0x64E08000                  )
    // BFMLALB <Zda>.S, <Zn>.H, <Zm>.H[<imm>]                                            SVE_GZ_3A           01100100111iimmm 0100i0nnnnnddddd     64E0 4000   
    // BFMLALB <Zda>.S, <Zn>.H, <Zm>.H                                                   SVE_HB_3A           01100100111mmmmm 100000nnnnnddddd     64E0 8000   

INST2(bfmlalt,           "bfmlalt",               0,                       IF_SVE_2BL,              0x64E04400,      0x64E08400                  )
    // BFMLALT <Zda>.S, <Zn>.H, <Zm>.H[<imm>]                                            SVE_GZ_3A           01100100111iimmm 0100i1nnnnnddddd     64E0 4400   
    // BFMLALT <Zda>.S, <Zn>.H, <Zm>.H                                                   SVE_HB_3A           01100100111mmmmm 100001nnnnnddddd     64E0 8400   

INST2(bfmlslb,           "bfmlslb",               0,                       IF_SVE_2BL,              0x64E06000,      0x64E0A000                  )
    // BFMLSLB <Zda>.S, <Zn>.H, <Zm>.H[<imm>]                                            SVE_GZ_3A           01100100111iimmm 0110i0nnnnnddddd     64E0 6000   
    // BFMLSLB <Zda>.S, <Zn>.H, <Zm>.H                                                   SVE_HB_3A           01100100111mmmmm 101000nnnnnddddd     64E0 A000   

INST2(bfmlslt,           "bfmlslt",               0,                       IF_SVE_2BL,              0x64E06400,      0x64E0A400                  )
    // BFMLSLT <Zda>.S, <Zn>.H, <Zm>.H[<imm>]                                            SVE_GZ_3A           01100100111iimmm 0110i1nnnnnddddd     64E0 6400   
    // BFMLSLT <Zda>.S, <Zn>.H, <Zm>.H                                                   SVE_HB_3A           01100100111mmmmm 101001nnnnnddddd     64E0 A400   

INST2(fmlslb,            "fmlslb",                0,                       IF_SVE_2BL,              0x64A06000,      0x64A0A000                  )
    // FMLSLB  <Zda>.S, <Zn>.H, <Zm>.H[<imm>]                                            SVE_GZ_3A           01100100101iimmm 0110i0nnnnnddddd     64A0 6000   
    // FMLSLB  <Zda>.S, <Zn>.H, <Zm>.H                                                   SVE_HB_3A           01100100101mmmmm 101000nnnnnddddd     64A0 A000   

INST2(fmlslt,            "fmlslt",                0,                       IF_SVE_2BL,              0x64A06400,      0x64A0A400                  )
    // FMLSLT  <Zda>.S, <Zn>.H, <Zm>.H[<imm>]                                            SVE_GZ_3A           01100100101iimmm 0110i1nnnnnddddd     64A0 6400   
    // FMLSLT  <Zda>.S, <Zn>.H, <Zm>.H                                                   SVE_HB_3A           01100100101mmmmm 101001nnnnnddddd     64A0 A400   


//    enum               name                     info                                              SVE_HK_3B        SVE_HL_3B                   
INST2(bfadd,             "bfadd",                 0,                       IF_SVE_2BM,              0x65000000,      0x65008000                  )
    // BFADD   <Zd>.H, <Zn>.H, <Zm>.H                                                    SVE_HK_3B           01100101000mmmmm 000000nnnnnddddd     6500 0000   
    // BFADD   <Zdn>.H, <Pg>/M, <Zdn>.H, <Zm>.H                                          SVE_HL_3B           0110010100000000 100gggmmmmmddddd     6500 8000   

INST2(bfsub,             "bfsub",                 0,                       IF_SVE_2BM,              0x65000400,      0x65018000                  )
    // BFSUB   <Zd>.H, <Zn>.H, <Zm>.H                                                    SVE_HK_3B           01100101000mmmmm 000001nnnnnddddd     6500 0400   
    // BFSUB   <Zdn>.H, <Pg>/M, <Zdn>.H, <Zm>.H                                          SVE_HL_3B           0110010100000001 100gggmmmmmddddd     6501 8000   


//    enum               name                     info                                              SVE_IF_4A        SVE_IF_4A_A                 
INST2(ldnt1sb,           "ldnt1sb",               0,                       IF_SVE_2BN,              0x84008000,      0xC4008000                  )
    // LDNT1SB {<Zt>.S }, <Pg>/Z, [<Zn>.S{, <Xm>}]                                       SVE_IF_4A           10000100000mmmmm 100gggnnnnnttttt     8400 8000   
    // LDNT1SB {<Zt>.D }, <Pg>/Z, [<Zn>.D{, <Xm>}]                                       SVE_IF_4A_A         11000100000mmmmm 100gggnnnnnttttt     C400 8000   

INST2(ldnt1sh,           "ldnt1sh",               0,                       IF_SVE_2BN,              0x84808000,      0xC4808000                  )
    // LDNT1SH {<Zt>.S }, <Pg>/Z, [<Zn>.S{, <Xm>}]                                       SVE_IF_4A           10000100100mmmmm 100gggnnnnnttttt     8480 8000   
    // LDNT1SH {<Zt>.D }, <Pg>/Z, [<Zn>.D{, <Xm>}]                                       SVE_IF_4A_A         11000100100mmmmm 100gggnnnnnttttt     C480 8000   


//    enum               name                     info                                              SVE_IO_3A        SVE_IP_4A                   
INST2(ld1rob,            "ld1rob",                0,                       IF_SVE_2BO,              0xA4202000,      0xA4200000                  )
    // LD1ROB  {<Zt>.B }, <Pg>/Z, [<Xn|SP>{, #<imm>}]                                    SVE_IO_3A           101001000010iiii 001gggnnnnnttttt     A420 2000   
    // LD1ROB  {<Zt>.B }, <Pg>/Z, [<Xn|SP>, <Xm>]                                        SVE_IP_4A           10100100001mmmmm 000gggnnnnnttttt     A420 0000   

INST2(ld1rod,            "ld1rod",                0,                       IF_SVE_2BO,              0xA5A02000,      0xA5A00000                  )
    // LD1ROD  {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>}]                                    SVE_IO_3A           101001011010iiii 001gggnnnnnttttt     A5A0 2000   
    // LD1ROD  {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #3]                                SVE_IP_4A           10100101101mmmmm 000gggnnnnnttttt     A5A0 0000   

INST2(ld1roh,            "ld1roh",                0,                       IF_SVE_2BO,              0xA4A02000,      0xA4A00000                  )
    // LD1ROH  {<Zt>.H }, <Pg>/Z, [<Xn|SP>{, #<imm>}]                                    SVE_IO_3A           101001001010iiii 001gggnnnnnttttt     A4A0 2000   
    // LD1ROH  {<Zt>.H }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #1]                                SVE_IP_4A           10100100101mmmmm 000gggnnnnnttttt     A4A0 0000   

INST2(ld1row,            "ld1row",                0,                       IF_SVE_2BO,              0xA5202000,      0xA5200000                  )
    // LD1ROW  {<Zt>.S }, <Pg>/Z, [<Xn|SP>{, #<imm>}]                                    SVE_IO_3A           101001010010iiii 001gggnnnnnttttt     A520 2000   
    // LD1ROW  {<Zt>.S }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #2]                                SVE_IP_4A           10100101001mmmmm 000gggnnnnnttttt     A520 0000   

INST2(ld1rqb,            "ld1rqb",                0,                       IF_SVE_2BO,              0xA4002000,      0xA4000000                  )
    // LD1RQB  {<Zt>.B }, <Pg>/Z, [<Xn|SP>{, #<imm>}]                                    SVE_IO_3A           101001000000iiii 001gggnnnnnttttt     A400 2000   
    // LD1RQB  {<Zt>.B }, <Pg>/Z, [<Xn|SP>, <Xm>]                                        SVE_IP_4A           10100100000mmmmm 000gggnnnnnttttt     A400 0000   

INST2(ld1rqd,            "ld1rqd",                0,                       IF_SVE_2BO,              0xA5802000,      0xA5800000                  )
    // LD1RQD  {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>}]                                    SVE_IO_3A           101001011000iiii 001gggnnnnnttttt     A580 2000   
    // LD1RQD  {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #3]                                SVE_IP_4A           10100101100mmmmm 000gggnnnnnttttt     A580 0000   

INST2(ld1rqh,            "ld1rqh",                0,                       IF_SVE_2BO,              0xA4802000,      0xA4800000                  )
    // LD1RQH  {<Zt>.H }, <Pg>/Z, [<Xn|SP>{, #<imm>}]                                    SVE_IO_3A           101001001000iiii 001gggnnnnnttttt     A480 2000   
    // LD1RQH  {<Zt>.H }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #1]                                SVE_IP_4A           10100100100mmmmm 000gggnnnnnttttt     A480 0000   

INST2(ld1rqw,            "ld1rqw",                0,                       IF_SVE_2BO,              0xA5002000,      0xA5000000                  )
    // LD1RQW  {<Zt>.S }, <Pg>/Z, [<Xn|SP>{, #<imm>}]                                    SVE_IO_3A           101001010000iiii 001gggnnnnnttttt     A500 2000   
    // LD1RQW  {<Zt>.S }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #2]                                SVE_IP_4A           10100101000mmmmm 000gggnnnnnttttt     A500 0000   


//    enum               name                     info                                              SVE_IQ_3A        SVE_IR_4A                   
INST2(ld2q,              "ld2q",                  0,                       IF_SVE_2BP,              0xA490E000,      0xA4A08000                  )
    // LD2Q    {<Zt1>.Q, <Zt2>.Q }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]                  SVE_IQ_3A           101001001001iiii 111gggnnnnnttttt     A490 E000   
    // LD2Q    {<Zt1>.Q, <Zt2>.Q }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #4]                      SVE_IR_4A           10100100101mmmmm 100gggnnnnnttttt     A4A0 8000   

INST2(ld3q,              "ld3q",                  0,                       IF_SVE_2BP,              0xA510E000,      0xA5208000                  )
    // LD3Q    {<Zt1>.Q, <Zt2>.Q, <Zt3>.Q }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]         SVE_IQ_3A           101001010001iiii 111gggnnnnnttttt     A510 E000   
    // LD3Q    {<Zt1>.Q, <Zt2>.Q, <Zt3>.Q }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #4]             SVE_IR_4A           10100101001mmmmm 100gggnnnnnttttt     A520 8000   

INST2(ld4q,              "ld4q",                  0,                       IF_SVE_2BP,              0xA590E000,      0xA5A08000                  )
    // LD4Q    {<Zt1>.Q, <Zt2>.Q, <Zt3>.Q, <Zt4>.Q }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]SVE_IQ_3A           101001011001iiii 111gggnnnnnttttt     A590 E000   
    // LD4Q    {<Zt1>.Q, <Zt2>.Q, <Zt3>.Q, <Zt4>.Q }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #4]    SVE_IR_4A           10100101101mmmmm 100gggnnnnnttttt     A5A0 8000   


//    enum               name                     info                                              SVE_IS_3A        SVE_IT_4A                   
INST2(ld2b,              "ld2b",                  0,                       IF_SVE_2BQ,              0xA420E000,      0xA420C000                  )
    // LD2B    {<Zt1>.B, <Zt2>.B }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]                  SVE_IS_3A           101001000010iiii 111gggnnnnnttttt     A420 E000   
    // LD2B    {<Zt1>.B, <Zt2>.B }, <Pg>/Z, [<Xn|SP>, <Xm>]                              SVE_IT_4A           10100100001mmmmm 110gggnnnnnttttt     A420 C000   

INST2(ld2d,              "ld2d",                  0,                       IF_SVE_2BQ,              0xA5A0E000,      0xA5A0C000                  )
    // LD2D    {<Zt1>.D, <Zt2>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]                  SVE_IS_3A           101001011010iiii 111gggnnnnnttttt     A5A0 E000   
    // LD2D    {<Zt1>.D, <Zt2>.D }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #3]                      SVE_IT_4A           10100101101mmmmm 110gggnnnnnttttt     A5A0 C000   

INST2(ld2h,              "ld2h",                  0,                       IF_SVE_2BQ,              0xA4A0E000,      0xA4A0C000                  )
    // LD2H    {<Zt1>.H, <Zt2>.H }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]                  SVE_IS_3A           101001001010iiii 111gggnnnnnttttt     A4A0 E000   
    // LD2H    {<Zt1>.H, <Zt2>.H }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #1]                      SVE_IT_4A           10100100101mmmmm 110gggnnnnnttttt     A4A0 C000   

INST2(ld2w,              "ld2w",                  0,                       IF_SVE_2BQ,              0xA520E000,      0xA520C000                  )
    // LD2W    {<Zt1>.S, <Zt2>.S }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]                  SVE_IS_3A           101001010010iiii 111gggnnnnnttttt     A520 E000   
    // LD2W    {<Zt1>.S, <Zt2>.S }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #2]                      SVE_IT_4A           10100101001mmmmm 110gggnnnnnttttt     A520 C000   

INST2(ld3b,              "ld3b",                  0,                       IF_SVE_2BQ,              0xA440E000,      0xA440C000                  )
    // LD3B    {<Zt1>.B, <Zt2>.B, <Zt3>.B }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]         SVE_IS_3A           101001000100iiii 111gggnnnnnttttt     A440 E000   
    // LD3B    {<Zt1>.B, <Zt2>.B, <Zt3>.B }, <Pg>/Z, [<Xn|SP>, <Xm>]                     SVE_IT_4A           10100100010mmmmm 110gggnnnnnttttt     A440 C000   

INST2(ld3d,              "ld3d",                  0,                       IF_SVE_2BQ,              0xA5C0E000,      0xA5C0C000                  )
    // LD3D    {<Zt1>.D, <Zt2>.D, <Zt3>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]         SVE_IS_3A           101001011100iiii 111gggnnnnnttttt     A5C0 E000   
    // LD3D    {<Zt1>.D, <Zt2>.D, <Zt3>.D }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #3]             SVE_IT_4A           10100101110mmmmm 110gggnnnnnttttt     A5C0 C000   

INST2(ld3h,              "ld3h",                  0,                       IF_SVE_2BQ,              0xA4C0E000,      0xA4C0C000                  )
    // LD3H    {<Zt1>.H, <Zt2>.H, <Zt3>.H }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]         SVE_IS_3A           101001001100iiii 111gggnnnnnttttt     A4C0 E000   
    // LD3H    {<Zt1>.H, <Zt2>.H, <Zt3>.H }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #1]             SVE_IT_4A           10100100110mmmmm 110gggnnnnnttttt     A4C0 C000   

INST2(ld3w,              "ld3w",                  0,                       IF_SVE_2BQ,              0xA540E000,      0xA540C000                  )
    // LD3W    {<Zt1>.S, <Zt2>.S, <Zt3>.S }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]         SVE_IS_3A           101001010100iiii 111gggnnnnnttttt     A540 E000   
    // LD3W    {<Zt1>.S, <Zt2>.S, <Zt3>.S }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #2]             SVE_IT_4A           10100101010mmmmm 110gggnnnnnttttt     A540 C000   

INST2(ld4b,              "ld4b",                  0,                       IF_SVE_2BQ,              0xA460E000,      0xA460C000                  )
    // LD4B    {<Zt1>.B, <Zt2>.B, <Zt3>.B, <Zt4>.B }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]SVE_IS_3A           101001000110iiii 111gggnnnnnttttt     A460 E000   
    // LD4B    {<Zt1>.B, <Zt2>.B, <Zt3>.B, <Zt4>.B }, <Pg>/Z, [<Xn|SP>, <Xm>]            SVE_IT_4A           10100100011mmmmm 110gggnnnnnttttt     A460 C000   

INST2(ld4d,              "ld4d",                  0,                       IF_SVE_2BQ,              0xA5E0E000,      0xA5E0C000                  )
    // LD4D    {<Zt1>.D, <Zt2>.D, <Zt3>.D, <Zt4>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]SVE_IS_3A           101001011110iiii 111gggnnnnnttttt     A5E0 E000   
    // LD4D    {<Zt1>.D, <Zt2>.D, <Zt3>.D, <Zt4>.D }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #3]    SVE_IT_4A           10100101111mmmmm 110gggnnnnnttttt     A5E0 C000   

INST2(ld4h,              "ld4h",                  0,                       IF_SVE_2BQ,              0xA4E0E000,      0xA4E0C000                  )
    // LD4H    {<Zt1>.H, <Zt2>.H, <Zt3>.H, <Zt4>.H }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]SVE_IS_3A           101001001110iiii 111gggnnnnnttttt     A4E0 E000   
    // LD4H    {<Zt1>.H, <Zt2>.H, <Zt3>.H, <Zt4>.H }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #1]    SVE_IT_4A           10100100111mmmmm 110gggnnnnnttttt     A4E0 C000   

INST2(ld4w,              "ld4w",                  0,                       IF_SVE_2BQ,              0xA560E000,      0xA560C000                  )
    // LD4W    {<Zt1>.S, <Zt2>.S, <Zt3>.S, <Zt4>.S }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]SVE_IS_3A           101001010110iiii 111gggnnnnnttttt     A560 E000   
    // LD4W    {<Zt1>.S, <Zt2>.S, <Zt3>.S, <Zt4>.S }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #2]    SVE_IT_4A           10100101011mmmmm 110gggnnnnnttttt     A560 C000   


//    enum               name                     info                                              SVE_JC_4A        SVE_JO_3A                   
INST2(st2b,              "st2b",                  0,                       IF_SVE_2BR,              0xE4206000,      0xE430E000                  )
    // ST2B    {<Zt1>.B, <Zt2>.B }, <Pg>, [<Xn|SP>, <Xm>]                                SVE_JC_4A           11100100001mmmmm 011gggnnnnnttttt     E420 6000   
    // ST2B    {<Zt1>.B, <Zt2>.B }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]                    SVE_JO_3A           111001000011iiii 111gggnnnnnttttt     E430 E000   

INST2(st2d,              "st2d",                  0,                       IF_SVE_2BR,              0xE5A06000,      0xE5B0E000                  )
    // ST2D    {<Zt1>.D, <Zt2>.D }, <Pg>, [<Xn|SP>, <Xm>, LSL #3]                        SVE_JC_4A           11100101101mmmmm 011gggnnnnnttttt     E5A0 6000   
    // ST2D    {<Zt1>.D, <Zt2>.D }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]                    SVE_JO_3A           111001011011iiii 111gggnnnnnttttt     E5B0 E000   

INST2(st2h,              "st2h",                  0,                       IF_SVE_2BR,              0xE4A06000,      0xE4B0E000                  )
    // ST2H    {<Zt1>.H, <Zt2>.H }, <Pg>, [<Xn|SP>, <Xm>, LSL #1]                        SVE_JC_4A           11100100101mmmmm 011gggnnnnnttttt     E4A0 6000   
    // ST2H    {<Zt1>.H, <Zt2>.H }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]                    SVE_JO_3A           111001001011iiii 111gggnnnnnttttt     E4B0 E000   

INST2(st2w,              "st2w",                  0,                       IF_SVE_2BR,              0xE5206000,      0xE530E000                  )
    // ST2W    {<Zt1>.S, <Zt2>.S }, <Pg>, [<Xn|SP>, <Xm>, LSL #2]                        SVE_JC_4A           11100101001mmmmm 011gggnnnnnttttt     E520 6000   
    // ST2W    {<Zt1>.S, <Zt2>.S }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]                    SVE_JO_3A           111001010011iiii 111gggnnnnnttttt     E530 E000   

INST2(st3b,              "st3b",                  0,                       IF_SVE_2BR,              0xE4406000,      0xE450E000                  )
    // ST3B    {<Zt1>.B, <Zt2>.B, <Zt3>.B }, <Pg>, [<Xn|SP>, <Xm>]                       SVE_JC_4A           11100100010mmmmm 011gggnnnnnttttt     E440 6000   
    // ST3B    {<Zt1>.B, <Zt2>.B, <Zt3>.B }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]           SVE_JO_3A           111001000101iiii 111gggnnnnnttttt     E450 E000   

INST2(st3d,              "st3d",                  0,                       IF_SVE_2BR,              0xE5C06000,      0xE5D0E000                  )
    // ST3D    {<Zt1>.D, <Zt2>.D, <Zt3>.D }, <Pg>, [<Xn|SP>, <Xm>, LSL #3]               SVE_JC_4A           11100101110mmmmm 011gggnnnnnttttt     E5C0 6000   
    // ST3D    {<Zt1>.D, <Zt2>.D, <Zt3>.D }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]           SVE_JO_3A           111001011101iiii 111gggnnnnnttttt     E5D0 E000   

INST2(st3h,              "st3h",                  0,                       IF_SVE_2BR,              0xE4C06000,      0xE4D0E000                  )
    // ST3H    {<Zt1>.H, <Zt2>.H, <Zt3>.H }, <Pg>, [<Xn|SP>, <Xm>, LSL #1]               SVE_JC_4A           11100100110mmmmm 011gggnnnnnttttt     E4C0 6000   
    // ST3H    {<Zt1>.H, <Zt2>.H, <Zt3>.H }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]           SVE_JO_3A           111001001101iiii 111gggnnnnnttttt     E4D0 E000   

INST2(st3w,              "st3w",                  0,                       IF_SVE_2BR,              0xE5406000,      0xE550E000                  )
    // ST3W    {<Zt1>.S, <Zt2>.S, <Zt3>.S }, <Pg>, [<Xn|SP>, <Xm>, LSL #2]               SVE_JC_4A           11100101010mmmmm 011gggnnnnnttttt     E540 6000   
    // ST3W    {<Zt1>.S, <Zt2>.S, <Zt3>.S }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]           SVE_JO_3A           111001010101iiii 111gggnnnnnttttt     E550 E000   

INST2(st4b,              "st4b",                  0,                       IF_SVE_2BR,              0xE4606000,      0xE470E000                  )
    // ST4B    {<Zt1>.B, <Zt2>.B, <Zt3>.B, <Zt4>.B }, <Pg>, [<Xn|SP>, <Xm>]              SVE_JC_4A           11100100011mmmmm 011gggnnnnnttttt     E460 6000   
    // ST4B    {<Zt1>.B, <Zt2>.B, <Zt3>.B, <Zt4>.B }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]  SVE_JO_3A           111001000111iiii 111gggnnnnnttttt     E470 E000   

INST2(st4d,              "st4d",                  0,                       IF_SVE_2BR,              0xE5E06000,      0xE5F0E000                  )
    // ST4D    {<Zt1>.D, <Zt2>.D, <Zt3>.D, <Zt4>.D }, <Pg>, [<Xn|SP>, <Xm>, LSL #3]      SVE_JC_4A           11100101111mmmmm 011gggnnnnnttttt     E5E0 6000   
    // ST4D    {<Zt1>.D, <Zt2>.D, <Zt3>.D, <Zt4>.D }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]  SVE_JO_3A           111001011111iiii 111gggnnnnnttttt     E5F0 E000   

INST2(st4h,              "st4h",                  0,                       IF_SVE_2BR,              0xE4E06000,      0xE4F0E000                  )
    // ST4H    {<Zt1>.H, <Zt2>.H, <Zt3>.H, <Zt4>.H }, <Pg>, [<Xn|SP>, <Xm>, LSL #1]      SVE_JC_4A           11100100111mmmmm 011gggnnnnnttttt     E4E0 6000   
    // ST4H    {<Zt1>.H, <Zt2>.H, <Zt3>.H, <Zt4>.H }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]  SVE_JO_3A           111001001111iiii 111gggnnnnnttttt     E4F0 E000   

INST2(st4w,              "st4w",                  0,                       IF_SVE_2BR,              0xE5606000,      0xE570E000                  )
    // ST4W    {<Zt1>.S, <Zt2>.S, <Zt3>.S, <Zt4>.S }, <Pg>, [<Xn|SP>, <Xm>, LSL #2]      SVE_JC_4A           11100101011mmmmm 011gggnnnnnttttt     E560 6000   
    // ST4W    {<Zt1>.S, <Zt2>.S, <Zt3>.S, <Zt4>.S }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]  SVE_JO_3A           111001010111iiii 111gggnnnnnttttt     E570 E000   


//    enum               name                     info                                              SVE_JE_3A        SVE_JF_4A                   
INST2(st2q,              "st2q",                  0,                       IF_SVE_2BS,              0xE4400000,      0xE4600000                  )
    // ST2Q    {<Zt1>.Q, <Zt2>.Q }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]                    SVE_JE_3A           111001000100iiii 000gggnnnnnttttt     E440 0000   
    // ST2Q    {<Zt1>.Q, <Zt2>.Q }, <Pg>, [<Xn|SP>, <Xm>, LSL #4]                        SVE_JF_4A           11100100011mmmmm 000gggnnnnnttttt     E460 0000   

INST2(st3q,              "st3q",                  0,                       IF_SVE_2BS,              0xE4800000,      0xE4A00000                  )
    // ST3Q    {<Zt1>.Q, <Zt2>.Q, <Zt3>.Q }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]           SVE_JE_3A           111001001000iiii 000gggnnnnnttttt     E480 0000   
    // ST3Q    {<Zt1>.Q, <Zt2>.Q, <Zt3>.Q }, <Pg>, [<Xn|SP>, <Xm>, LSL #4]               SVE_JF_4A           11100100101mmmmm 000gggnnnnnttttt     E4A0 0000   

INST2(st4q,              "st4q",                  0,                       IF_SVE_2BS,              0xE4C00000,      0xE4E00000                  )
    // ST4Q    {<Zt1>.Q, <Zt2>.Q, <Zt3>.Q, <Zt4>.Q }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]  SVE_JE_3A           111001001100iiii 000gggnnnnnttttt     E4C0 0000   
    // ST4Q    {<Zt1>.Q, <Zt2>.Q, <Zt3>.Q, <Zt4>.Q }, <Pg>, [<Xn|SP>, <Xm>, LSL #4]      SVE_JF_4A           11100100111mmmmm 000gggnnnnnttttt     E4E0 0000   


//    enum               name                     info                                              SVE_AQ_3A                                    
INST1(abs,               "abs",                   0,                       IF_SVE_AQ_3A,            0x0416A000                                   )
    // ABS     <Zd>.<T>, <Pg>/M, <Zn>.<T>                                                SVE_AQ_3A           00000100xx010110 101gggnnnnnddddd     0416 A000   

INST1(neg,               "neg",                   0,                       IF_SVE_AQ_3A,            0x0417A000                                   )
    // NEG     <Zd>.<T>, <Pg>/M, <Zn>.<T>                                                SVE_AQ_3A           00000100xx010111 101gggnnnnnddddd     0417 A000   

INST1(sxtb,              "sxtb",                  0,                       IF_SVE_AQ_3A,            0x0410A000                                   )
    // SXTB    <Zd>.<T>, <Pg>/M, <Zn>.<T>                                                SVE_AQ_3A           00000100xx010000 101gggnnnnnddddd     0410 A000   

INST1(sxth,              "sxth",                  0,                       IF_SVE_AQ_3A,            0x0412A000                                   )
    // SXTH    <Zd>.<T>, <Pg>/M, <Zn>.<T>                                                SVE_AQ_3A           00000100xx010010 101gggnnnnnddddd     0412 A000   

INST1(sxtw,              "sxtw",                  0,                       IF_SVE_AQ_3A,            0x0414A000                                   )
    // SXTW    <Zd>.D, <Pg>/M, <Zn>.D                                                    SVE_AQ_3A           00000100xx010100 101gggnnnnnddddd     0414 A000   

INST1(uxtb,              "uxtb",                  0,                       IF_SVE_AQ_3A,            0x0411A000                                   )
    // UXTB    <Zd>.<T>, <Pg>/M, <Zn>.<T>                                                SVE_AQ_3A           00000100xx010001 101gggnnnnnddddd     0411 A000   

INST1(uxth,              "uxth",                  0,                       IF_SVE_AQ_3A,            0x0413A000                                   )
    // UXTH    <Zd>.<T>, <Pg>/M, <Zn>.<T>                                                SVE_AQ_3A           00000100xx010011 101gggnnnnnddddd     0413 A000   

INST1(uxtw,              "uxtw",                  0,                       IF_SVE_AQ_3A,            0x0415A000                                   )
    // UXTW    <Zd>.D, <Pg>/M, <Zn>.D                                                    SVE_AQ_3A           00000100xx010101 101gggnnnnnddddd     0415 A000   


//    enum               name                     info                                              SVE_CZ_4A                                    
INST1(ands,              "ands",                  0,                       IF_SVE_CZ_4A,            0x25404000                                   )
    // ANDS    <Pd>.B, <Pg>/Z, <Pn>.B, <Pm>.B                                            SVE_CZ_4A           001001010100MMMM 01gggg0NNNN0DDDD     2540 4000   

INST1(bics,              "bics",                  0,                       IF_SVE_CZ_4A,            0x25404010                                   )
    // BICS    <Pd>.B, <Pg>/Z, <Pn>.B, <Pm>.B                                            SVE_CZ_4A           001001010100MMMM 01gggg0NNNN1DDDD     2540 4010   

INST1(eors,              "eors",                  0,                       IF_SVE_CZ_4A,            0x25404200                                   )
    // EORS    <Pd>.B, <Pg>/Z, <Pn>.B, <Pm>.B                                            SVE_CZ_4A           001001010100MMMM 01gggg1NNNN0DDDD     2540 4200   

INST1(nand,              "nand",                  0,                       IF_SVE_CZ_4A,            0x25804210                                   )
    // NAND    <Pd>.B, <Pg>/Z, <Pn>.B, <Pm>.B                                            SVE_CZ_4A           001001011000MMMM 01gggg1NNNN1DDDD     2580 4210   

INST1(nands,             "nands",                 0,                       IF_SVE_CZ_4A,            0x25C04210                                   )
    // NANDS   <Pd>.B, <Pg>/Z, <Pn>.B, <Pm>.B                                            SVE_CZ_4A           001001011100MMMM 01gggg1NNNN1DDDD     25C0 4210   

INST1(nor,               "nor",                   0,                       IF_SVE_CZ_4A,            0x25804200                                   )
    // NOR     <Pd>.B, <Pg>/Z, <Pn>.B, <Pm>.B                                            SVE_CZ_4A           001001011000MMMM 01gggg1NNNN0DDDD     2580 4200   

INST1(nors,              "nors",                  0,                       IF_SVE_CZ_4A,            0x25C04200                                   )
    // NORS    <Pd>.B, <Pg>/Z, <Pn>.B, <Pm>.B                                            SVE_CZ_4A           001001011100MMMM 01gggg1NNNN0DDDD     25C0 4200   

INST1(nots,              "nots",                  0,                       IF_SVE_CZ_4A,            0x25404200                                   )
    // NOTS    <Pd>.B, <Pg>/Z, <Pn>.B                                                    SVE_CZ_4A           001001010100MMMM 01gggg1NNNN0DDDD     2540 4200   

INST1(orns,              "orns",                  0,                       IF_SVE_CZ_4A,            0x25C04010                                   )
    // ORNS    <Pd>.B, <Pg>/Z, <Pn>.B, <Pm>.B                                            SVE_CZ_4A           001001011100MMMM 01gggg0NNNN1DDDD     25C0 4010   

INST1(orrs,              "orrs",                  0,                       IF_SVE_CZ_4A,            0x25C04000                                   )
    // ORRS    <Pd>.B, <Pg>/Z, <Pn>.B, <Pm>.B                                            SVE_CZ_4A           001001011100MMMM 01gggg0NNNN0DDDD     25C0 4000   


//    enum               name                     info                                              SVE_CU_3A                                    
INST1(rbit,              "rbit",                  0,                       IF_SVE_CU_3A,            0x05278000                                   )
    // RBIT    <Zd>.<T>, <Pg>/M, <Zn>.<T>                                                SVE_CU_3A           00000101xx100111 100gggnnnnnddddd     0527 8000   

INST1(revb,              "revb",                  0,                       IF_SVE_CU_3A,            0x05248000                                   )
    // REVB    <Zd>.<T>, <Pg>/M, <Zn>.<T>                                                SVE_CU_3A           00000101xx100100 100gggnnnnnddddd     0524 8000   

INST1(revh,              "revh",                  0,                       IF_SVE_CU_3A,            0x05258000                                   )
    // REVH    <Zd>.<T>, <Pg>/M, <Zn>.<T>                                                SVE_CU_3A           00000101xx100101 100gggnnnnnddddd     0525 8000   

INST1(revw,              "revw",                  0,                       IF_SVE_CU_3A,            0x05268000                                   )
    // REVW    <Zd>.D, <Pg>/M, <Zn>.D                                                    SVE_CU_3A           00000101xx100110 100gggnnnnnddddd     0526 8000   


//    enum               name                     info                                              SVE_AP_3A                                    
INST1(cls,               "cls",                   0,                       IF_SVE_AP_3A,            0x0418A000                                   )
    // CLS     <Zd>.<T>, <Pg>/M, <Zn>.<T>                                                SVE_AP_3A           00000100xx011000 101gggnnnnnddddd     0418 A000   

INST1(clz,               "clz",                   0,                       IF_SVE_AP_3A,            0x0419A000                                   )
    // CLZ     <Zd>.<T>, <Pg>/M, <Zn>.<T>                                                SVE_AP_3A           00000100xx011001 101gggnnnnnddddd     0419 A000   

INST1(cnot,              "cnot",                  0,                       IF_SVE_AP_3A,            0x041BA000                                   )
    // CNOT    <Zd>.<T>, <Pg>/M, <Zn>.<T>                                                SVE_AP_3A           00000100xx011011 101gggnnnnnddddd     041B A000   

INST1(cnt,               "cnt",                   0,                       IF_SVE_AP_3A,            0x041AA000                                   )
    // CNT     <Zd>.<T>, <Pg>/M, <Zn>.<T>                                                SVE_AP_3A           00000100xx011010 101gggnnnnnddddd     041A A000   

INST1(fabs,              "fabs",                  0,                       IF_SVE_AP_3A,            0x041CA000                                   )
    // FABS    <Zd>.<T>, <Pg>/M, <Zn>.<T>                                                SVE_AP_3A           00000100xx011100 101gggnnnnnddddd     041C A000   

INST1(fneg,              "fneg",                  0,                       IF_SVE_AP_3A,            0x041DA000                                   )
    // FNEG    <Zd>.<T>, <Pg>/M, <Zn>.<T>                                                SVE_AP_3A           00000100xx011101 101gggnnnnnddddd     041D A000   


//    enum               name                     info                                              SVE_AC_3A                                    
INST1(sdiv,              "sdiv",                  0,                       IF_SVE_AC_3A,            0x04140000                                   )
    // SDIV    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_AC_3A           00000100xx010100 000gggmmmmmddddd     0414 0000   

INST1(sdivr,             "sdivr",                 0,                       IF_SVE_AC_3A,            0x04160000                                   )
    // SDIVR   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_AC_3A           00000100xx010110 000gggmmmmmddddd     0416 0000   

INST1(udiv,              "udiv",                  0,                       IF_SVE_AC_3A,            0x04150000                                   )
    // UDIV    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_AC_3A           00000100xx010101 000gggmmmmmddddd     0415 0000   

INST1(udivr,             "udivr",                 0,                       IF_SVE_AC_3A,            0x04170000                                   )
    // UDIVR   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_AC_3A           00000100xx010111 000gggmmmmmddddd     0417 0000   


//    enum               name                     info                                              SVE_BS_1A                                    
INST1(eon,               "eon",                   0,                       IF_SVE_BS_1A,            0x05400000                                   )
    // EON     <Zdn>.<T>, <Zdn>.<T>, #<const>                                            SVE_BS_1A           00000101010000ii iiiiiiiiiiiddddd     0540 0000   


//    enum               name                     info                                              SVE_AK_3A                                    
INST1(smaxv,             "smaxv",                 0,                       IF_SVE_AK_3A,            0x04082000                                   )
    // SMAXV   <V><d>, <Pg>, <Zn>.<T>                                                    SVE_AK_3A           00000100xx001000 001gggnnnnnddddd     0408 2000   

INST1(sminv,             "sminv",                 0,                       IF_SVE_AK_3A,            0x040A2000                                   )
    // SMINV   <V><d>, <Pg>, <Zn>.<T>                                                    SVE_AK_3A           00000100xx001010 001gggnnnnnddddd     040A 2000   

INST1(umaxv,             "umaxv",                 0,                       IF_SVE_AK_3A,            0x04092000                                   )
    // UMAXV   <V><d>, <Pg>, <Zn>.<T>                                                    SVE_AK_3A           00000100xx001001 001gggnnnnnddddd     0409 2000   

INST1(uminv,             "uminv",                 0,                       IF_SVE_AK_3A,            0x040B2000                                   )
    // UMINV   <V><d>, <Pg>, <Zn>.<T>                                                    SVE_AK_3A           00000100xx001011 001gggnnnnnddddd     040B 2000   


//    enum               name                     info                                              SVE_HE_3A                                    
INST1(faddv,             "faddv",                 0,                       IF_SVE_HE_3A,            0x65002000                                   )
    // FADDV   <V><d>, <Pg>, <Zn>.<T>                                                    SVE_HE_3A           01100101xx000000 001gggnnnnnddddd     6500 2000   

INST1(fmaxnmv,           "fmaxnmv",               0,                       IF_SVE_HE_3A,            0x65042000                                   )
    // FMAXNMV <V><d>, <Pg>, <Zn>.<T>                                                    SVE_HE_3A           01100101xx000100 001gggnnnnnddddd     6504 2000   

INST1(fmaxv,             "fmaxv",                 0,                       IF_SVE_HE_3A,            0x65062000                                   )
    // FMAXV   <V><d>, <Pg>, <Zn>.<T>                                                    SVE_HE_3A           01100101xx000110 001gggnnnnnddddd     6506 2000   

INST1(fminnmv,           "fminnmv",               0,                       IF_SVE_HE_3A,            0x65052000                                   )
    // FMINNMV <V><d>, <Pg>, <Zn>.<T>                                                    SVE_HE_3A           01100101xx000101 001gggnnnnnddddd     6505 2000   

INST1(fminv,             "fminv",                 0,                       IF_SVE_HE_3A,            0x65072000                                   )
    // FMINV   <V><d>, <Pg>, <Zn>.<T>                                                    SVE_HE_3A           01100101xx000111 001gggnnnnnddddd     6507 2000   


//    enum               name                     info                                              SVE_ER_3A                                    
INST1(addp,              "addp",                  0,                       IF_SVE_ER_3A,            0x4411A000                                   )
    // ADDP    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_ER_3A           01000100xx010001 101gggmmmmmddddd     4411 A000   

INST1(smaxp,             "smaxp",                 0,                       IF_SVE_ER_3A,            0x4414A000                                   )
    // SMAXP   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_ER_3A           01000100xx010100 101gggmmmmmddddd     4414 A000   

INST1(sminp,             "sminp",                 0,                       IF_SVE_ER_3A,            0x4416A000                                   )
    // SMINP   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_ER_3A           01000100xx010110 101gggmmmmmddddd     4416 A000   

INST1(umaxp,             "umaxp",                 0,                       IF_SVE_ER_3A,            0x4415A000                                   )
    // UMAXP   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_ER_3A           01000100xx010101 101gggmmmmmddddd     4415 A000   

INST1(uminp,             "uminp",                 0,                       IF_SVE_ER_3A,            0x4417A000                                   )
    // UMINP   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_ER_3A           01000100xx010111 101gggmmmmmddddd     4417 A000   


//    enum               name                     info                                              SVE_GR_3A                                    
INST1(faddp,             "faddp",                 0,                       IF_SVE_GR_3A,            0x64108000                                   )
    // FADDP   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_GR_3A           01100100xx010000 100gggmmmmmddddd     6410 8000   

INST1(fmaxnmp,           "fmaxnmp",               0,                       IF_SVE_GR_3A,            0x64148000                                   )
    // FMAXNMP <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_GR_3A           01100100xx010100 100gggmmmmmddddd     6414 8000   

INST1(fmaxp,             "fmaxp",                 0,                       IF_SVE_GR_3A,            0x64168000                                   )
    // FMAXP   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_GR_3A           01100100xx010110 100gggmmmmmddddd     6416 8000   

INST1(fminnmp,           "fminnmp",               0,                       IF_SVE_GR_3A,            0x64158000                                   )
    // FMINNMP <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_GR_3A           01100100xx010101 100gggmmmmmddddd     6415 8000   

INST1(fminp,             "fminp",                 0,                       IF_SVE_GR_3A,            0x64178000                                   )
    // FMINP   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_GR_3A           01100100xx010111 100gggmmmmmddddd     6417 8000   


//    enum               name                     info                                              SVE_FU_2A                                    
INST1(srsra,             "srsra",                 RSH,                     IF_SVE_FU_2A,            0x4500E800                                   )
    // SRSRA   <Zda>.<T>, <Zn>.<T>, #<const>                                             SVE_FU_2A           01000101xx0xxiii 111010nnnnnddddd     4500 E800   

INST1(ssra,              "ssra",                  RSH,                     IF_SVE_FU_2A,            0x4500E000                                   )
    // SSRA    <Zda>.<T>, <Zn>.<T>, #<const>                                             SVE_FU_2A           01000101xx0xxiii 111000nnnnnddddd     4500 E000   

INST1(ursra,             "ursra",                 RSH,                     IF_SVE_FU_2A,            0x4500EC00                                   )
    // URSRA   <Zda>.<T>, <Zn>.<T>, #<const>                                             SVE_FU_2A           01000101xx0xxiii 111011nnnnnddddd     4500 EC00   

INST1(usra,              "usra",                  RSH,                     IF_SVE_FU_2A,            0x4500E400                                   )
    // USRA    <Zda>.<T>, <Zn>.<T>, #<const>                                             SVE_FU_2A           01000101xx0xxiii 111001nnnnnddddd     4500 E400   


//    enum               name                     info                                              SVE_AM_2A                                    
INST1(asrd,              "asrd",                  RSH,                     IF_SVE_AM_2A,            0x04048000                                   )
    // ASRD    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, #<const>                                    SVE_AM_2A           00000100xx000100 100gggxxiiiddddd     0404 8000   

INST1(sqshlu,            "sqshlu",                0,                       IF_SVE_AM_2A,            0x040F8000                                   )
    // SQSHLU  <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, #<const>                                    SVE_AM_2A           00000100xx001111 100gggxxiiiddddd     040F 8000   

INST1(srshr,             "srshr",                 RSH,                     IF_SVE_AM_2A,            0x040C8000                                   )
    // SRSHR   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, #<const>                                    SVE_AM_2A           00000100xx001100 100gggxxiiiddddd     040C 8000   

INST1(urshr,             "urshr",                 RSH,                     IF_SVE_AM_2A,            0x040D8000                                   )
    // URSHR   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, #<const>                                    SVE_AM_2A           00000100xx001101 100gggxxiiiddddd     040D 8000   


//    enum               name                     info                                              SVE_GA_2A                                    
INST1(sqrshrn,           "sqrshrn",               RSH,                     IF_SVE_GA_2A,            0x45B02800                                   )
    // SQRSHRN <Zd>.H, {<Zn1>.S-<Zn2>.S }, #<const>                                      SVE_GA_2A           010001011011iiii 001010nnnn0ddddd     45B0 2800   

INST1(sqrshrun,          "sqrshrun",              RSH,                     IF_SVE_GA_2A,            0x45B00800                                   )
    // SQRSHRUN <Zd>.H, {<Zn1>.S-<Zn2>.S }, #<const>                                     SVE_GA_2A           010001011011iiii 000010nnnn0ddddd     45B0 0800   

INST1(uqrshrn,           "uqrshrn",               RSH,                     IF_SVE_GA_2A,            0x45B03800                                   )
    // UQRSHRN <Zd>.H, {<Zn1>.S-<Zn2>.S }, #<const>                                      SVE_GA_2A           010001011011iiii 001110nnnn0ddddd     45B0 3800   


//    enum               name                     info                                              SVE_FT_2A                                    
INST1(sli,               "sli",                   0,                       IF_SVE_FT_2A,            0x4500F400                                   )
    // SLI     <Zd>.<T>, <Zn>.<T>, #<const>                                              SVE_FT_2A           01000101xx0xxiii 111101nnnnnddddd     4500 F400   

INST1(sri,               "sri",                   RSH,                     IF_SVE_FT_2A,            0x4500F000                                   )
    // SRI     <Zd>.<T>, <Zn>.<T>, #<const>                                              SVE_FT_2A           01000101xx0xxiii 111100nnnnnddddd     4500 F000   


//    enum               name                     info                                              SVE_EU_3A                                    
INST1(sqrshl,            "sqrshl",                0,                       IF_SVE_EU_3A,            0x440A8000                                   )
    // SQRSHL  <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_EU_3A           01000100xx001010 100gggmmmmmddddd     440A 8000   

INST1(sqrshlr,           "sqrshlr",               0,                       IF_SVE_EU_3A,            0x440E8000                                   )
    // SQRSHLR <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_EU_3A           01000100xx001110 100gggmmmmmddddd     440E 8000   

INST1(sqshlr,            "sqshlr",                0,                       IF_SVE_EU_3A,            0x440C8000                                   )
    // SQSHLR  <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_EU_3A           01000100xx001100 100gggmmmmmddddd     440C 8000   

INST1(srshl,             "srshl",                 0,                       IF_SVE_EU_3A,            0x44028000                                   )
    // SRSHL   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_EU_3A           01000100xx000010 100gggmmmmmddddd     4402 8000   

INST1(srshlr,            "srshlr",                0,                       IF_SVE_EU_3A,            0x44068000                                   )
    // SRSHLR  <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_EU_3A           01000100xx000110 100gggmmmmmddddd     4406 8000   

INST1(uqrshl,            "uqrshl",                0,                       IF_SVE_EU_3A,            0x440B8000                                   )
    // UQRSHL  <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_EU_3A           01000100xx001011 100gggmmmmmddddd     440B 8000   

INST1(uqrshlr,           "uqrshlr",               0,                       IF_SVE_EU_3A,            0x440F8000                                   )
    // UQRSHLR <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_EU_3A           01000100xx001111 100gggmmmmmddddd     440F 8000   

INST1(uqshlr,            "uqshlr",                0,                       IF_SVE_EU_3A,            0x440D8000                                   )
    // UQSHLR  <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_EU_3A           01000100xx001101 100gggmmmmmddddd     440D 8000   

INST1(urshl,             "urshl",                 0,                       IF_SVE_EU_3A,            0x44038000                                   )
    // URSHL   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_EU_3A           01000100xx000011 100gggmmmmmddddd     4403 8000   

INST1(urshlr,            "urshlr",                0,                       IF_SVE_EU_3A,            0x44078000                                   )
    // URSHLR  <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_EU_3A           01000100xx000111 100gggmmmmmddddd     4407 8000   


//    enum               name                     info                                              SVE_HL_3A                                    
INST1(fabd,              "fabd",                  0,                       IF_SVE_HL_3A,            0x65088000                                   )
    // FABD    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_HL_3A           01100101xx001000 100gggmmmmmddddd     6508 8000   

INST1(famax,             "famax",                 0,                       IF_SVE_HL_3A,            0x650E8000                                   )
    // FAMAX   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_HL_3A           01100101xx001110 100gggmmmmmddddd     650E 8000   

INST1(famin,             "famin",                 0,                       IF_SVE_HL_3A,            0x650F8000                                   )
    // FAMIN   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_HL_3A           01100101xx001111 100gggmmmmmddddd     650F 8000   

INST1(fdiv,              "fdiv",                  0,                       IF_SVE_HL_3A,            0x650D8000                                   )
    // FDIV    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_HL_3A           01100101xx001101 100gggmmmmmddddd     650D 8000   

INST1(fdivr,             "fdivr",                 0,                       IF_SVE_HL_3A,            0x650C8000                                   )
    // FDIVR   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_HL_3A           01100101xx001100 100gggmmmmmddddd     650C 8000   

INST1(fmulx,             "fmulx",                 0,                       IF_SVE_HL_3A,            0x650A8000                                   )
    // FMULX   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_HL_3A           01100101xx001010 100gggmmmmmddddd     650A 8000   

INST1(fscale,            "fscale",                0,                       IF_SVE_HL_3A,            0x65098000                                   )
    // FSCALE  <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_HL_3A           01100101xx001001 100gggmmmmmddddd     6509 8000   


//    enum               name                     info                                              SVE_HK_3A                                    
INST1(frecps,            "frecps",                0,                       IF_SVE_HK_3A,            0x65001800                                   )
    // FRECPS  <Zd>.<T>, <Zn>.<T>, <Zm>.<T>                                              SVE_HK_3A           01100101xx0mmmmm 000110nnnnnddddd     6500 1800   

INST1(frsqrts,           "frsqrts",               0,                       IF_SVE_HK_3A,            0x65001C00                                   )
    // FRSQRTS <Zd>.<T>, <Zn>.<T>, <Zm>.<T>                                              SVE_HK_3A           01100101xx0mmmmm 000111nnnnnddddd     6500 1C00   

INST1(ftsmul,            "ftsmul",                0,                       IF_SVE_HK_3A,            0x65000C00                                   )
    // FTSMUL  <Zd>.<T>, <Zn>.<T>, <Zm>.<T>                                              SVE_HK_3A           01100101xx0mmmmm 000011nnnnnddddd     6500 0C00   


//    enum               name                     info                                              SVE_HT_4A                                    
INST1(facge,             "facge",                 0,                       IF_SVE_HT_4A,            0x6500C010                                   )
    // FACGE   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.<T>                                      SVE_HT_4A           01100101xx0mmmmm 110gggnnnnn1DDDD     6500 C010   

INST1(facgt,             "facgt",                 0,                       IF_SVE_HT_4A,            0x6500E010                                   )
    // FACGT   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.<T>                                      SVE_HT_4A           01100101xx0mmmmm 111gggnnnnn1DDDD     6500 E010   

INST1(facle,             "facle",                 0,                       IF_SVE_HT_4A,            0x6500C010                                   )
    // FACLE   <Pd>.<T>, <Pg>/Z, <Zm>.<T>, <Zn>.<T>                                      SVE_HT_4A           01100101xx0mmmmm 110gggnnnnn1DDDD     6500 C010   

INST1(faclt,             "faclt",                 0,                       IF_SVE_HT_4A,            0x6500E010                                   )
    // FACLT   <Pd>.<T>, <Pg>/Z, <Zm>.<T>, <Zn>.<T>                                      SVE_HT_4A           01100101xx0mmmmm 111gggnnnnn1DDDD     6500 E010   

INST1(fcmuo,             "fcmuo",                 0,                       IF_SVE_HT_4A,            0x6500C000                                   )
    // FCMUO   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.<T>                                      SVE_HT_4A           01100101xx0mmmmm 110gggnnnnn0DDDD     6500 C000   


//    enum               name                     info                                              SVE_ET_3A                                    
INST1(sqsubr,            "sqsubr",                0,                       IF_SVE_ET_3A,            0x441E8000                                   )
    // SQSUBR  <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_ET_3A           01000100xx011110 100gggmmmmmddddd     441E 8000   

INST1(suqadd,            "suqadd",                0,                       IF_SVE_ET_3A,            0x441C8000                                   )
    // SUQADD  <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_ET_3A           01000100xx011100 100gggmmmmmddddd     441C 8000   

INST1(uqsubr,            "uqsubr",                0,                       IF_SVE_ET_3A,            0x441F8000                                   )
    // UQSUBR  <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_ET_3A           01000100xx011111 100gggmmmmmddddd     441F 8000   

INST1(usqadd,            "usqadd",                0,                       IF_SVE_ET_3A,            0x441D8000                                   )
    // USQADD  <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_ET_3A           01000100xx011101 100gggmmmmmddddd     441D 8000   


//    enum               name                     info                                              SVE_ES_3A                                    
INST1(sqabs,             "sqabs",                 0,                       IF_SVE_ES_3A,            0x4408A000                                   )
    // SQABS   <Zd>.<T>, <Pg>/M, <Zn>.<T>                                                SVE_ES_3A           01000100xx001000 101gggnnnnnddddd     4408 A000   

INST1(sqneg,             "sqneg",                 0,                       IF_SVE_ES_3A,            0x4409A000                                   )
    // SQNEG   <Zd>.<T>, <Pg>/M, <Zn>.<T>                                                SVE_ES_3A           01000100xx001001 101gggnnnnnddddd     4409 A000   

INST1(urecpe,            "urecpe",                0,                       IF_SVE_ES_3A,            0x4400A000                                   )
    // URECPE  <Zd>.S, <Pg>/M, <Zn>.S                                                    SVE_ES_3A           01000100xx000000 101gggnnnnnddddd     4400 A000   

INST1(ursqrte,           "ursqrte",               0,                       IF_SVE_ES_3A,            0x4401A000                                   )
    // URSQRTE <Zd>.S, <Pg>/M, <Zn>.S                                                    SVE_ES_3A           01000100xx000001 101gggnnnnnddddd     4401 A000   


//    enum               name                     info                                              SVE_HF_2A                                    
INST1(frecpe,            "frecpe",                0,                       IF_SVE_HF_2A,            0x650E3000                                   )
    // FRECPE  <Zd>.<T>, <Zn>.<T>                                                        SVE_HF_2A           01100101xx001110 001100nnnnnddddd     650E 3000   

INST1(frsqrte,           "frsqrte",               0,                       IF_SVE_HF_2A,            0x650F3000                                   )
    // FRSQRTE <Zd>.<T>, <Zn>.<T>                                                        SVE_HF_2A           01100101xx001111 001100nnnnnddddd     650F 3000   


//    enum               name                     info                                              SVE_HR_3A                                    
INST1(frecpx,            "frecpx",                0,                       IF_SVE_HR_3A,            0x650CA000                                   )
    // FRECPX  <Zd>.<T>, <Pg>/M, <Zn>.<T>                                                SVE_HR_3A           01100101xx001100 101gggnnnnnddddd     650C A000   

INST1(fsqrt,             "fsqrt",                 0,                       IF_SVE_HR_3A,            0x650DA000                                   )
    // FSQRT   <Zd>.<T>, <Pg>/M, <Zn>.<T>                                                SVE_HR_3A           01100101xx001101 101gggnnnnnddddd     650D A000   


//    enum               name                     info                                              SVE_BZ_3A                                    
INST1(tbx,               "tbx",                   0,                       IF_SVE_BZ_3A,            0x05202C00                                   )
    // TBX     <Zd>.<T>, <Zn>.<T>, <Zm>.<T>                                              SVE_BZ_3A           00000101xx1mmmmm 001011nnnnnddddd     0520 2C00   


//    enum               name                     info                                              SVE_EP_3A                                    
INST1(shadd,             "shadd",                 0,                       IF_SVE_EP_3A,            0x44108000                                   )
    // SHADD   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_EP_3A           01000100xx010000 100gggmmmmmddddd     4410 8000   

INST1(shsub,             "shsub",                 0,                       IF_SVE_EP_3A,            0x44128000                                   )
    // SHSUB   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_EP_3A           01000100xx010010 100gggmmmmmddddd     4412 8000   

INST1(shsubr,            "shsubr",                0,                       IF_SVE_EP_3A,            0x44168000                                   )
    // SHSUBR  <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_EP_3A           01000100xx010110 100gggmmmmmddddd     4416 8000   

INST1(srhadd,            "srhadd",                0,                       IF_SVE_EP_3A,            0x44148000                                   )
    // SRHADD  <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_EP_3A           01000100xx010100 100gggmmmmmddddd     4414 8000   

INST1(uhadd,             "uhadd",                 0,                       IF_SVE_EP_3A,            0x44118000                                   )
    // UHADD   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_EP_3A           01000100xx010001 100gggmmmmmddddd     4411 8000   

INST1(uhsub,             "uhsub",                 0,                       IF_SVE_EP_3A,            0x44138000                                   )
    // UHSUB   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_EP_3A           01000100xx010011 100gggmmmmmddddd     4413 8000   

INST1(uhsubr,            "uhsubr",                0,                       IF_SVE_EP_3A,            0x44178000                                   )
    // UHSUBR  <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_EP_3A           01000100xx010111 100gggmmmmmddddd     4417 8000   

INST1(urhadd,            "urhadd",                0,                       IF_SVE_EP_3A,            0x44158000                                   )
    // URHADD  <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_EP_3A           01000100xx010101 100gggmmmmmddddd     4415 8000   


//    enum               name                     info                                              SVE_AD_3A                                    
INST1(sabd,              "sabd",                  0,                       IF_SVE_AD_3A,            0x040C0000                                   )
    // SABD    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_AD_3A           00000100xx001100 000gggmmmmmddddd     040C 0000   

INST1(uabd,              "uabd",                  0,                       IF_SVE_AD_3A,            0x040D0000                                   )
    // UABD    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_AD_3A           00000100xx001101 000gggmmmmmddddd     040D 0000   


//    enum               name                     info                                              SVE_FW_3A                                    
INST1(saba,              "saba",                  0,                       IF_SVE_FW_3A,            0x4500F800                                   )
    // SABA    <Zda>.<T>, <Zn>.<T>, <Zm>.<T>                                             SVE_FW_3A           01000101xx0mmmmm 111110nnnnnddddd     4500 F800   

INST1(uaba,              "uaba",                  0,                       IF_SVE_FW_3A,            0x4500FC00                                   )
    // UABA    <Zda>.<T>, <Zn>.<T>, <Zm>.<T>                                             SVE_FW_3A           01000101xx0mmmmm 111111nnnnnddddd     4500 FC00   


//    enum               name                     info                                              SVE_BD_3B                                    
INST1(pmul,              "pmul",                  0,                       IF_SVE_BD_3B,            0x04206400                                   )
    // PMUL    <Zd>.B, <Zn>.B, <Zm>.B                                                    SVE_BD_3B           00000100001mmmmm 011001nnnnnddddd     0420 6400   


//    enum               name                     info                                              SVE_AV_3A                                    
INST1(bcax,              "bcax",                  0,                       IF_SVE_AV_3A,            0x04603800                                   )
    // BCAX    <Zdn>.D, <Zdn>.D, <Zm>.D, <Zk>.D                                          SVE_AV_3A           00000100011mmmmm 001110kkkkkddddd     0460 3800   

INST1(bsl,               "bsl",                   0,                       IF_SVE_AV_3A,            0x04203C00                                   )
    // BSL     <Zdn>.D, <Zdn>.D, <Zm>.D, <Zk>.D                                          SVE_AV_3A           00000100001mmmmm 001111kkkkkddddd     0420 3C00   

INST1(bsl1n,             "bsl1n",                 0,                       IF_SVE_AV_3A,            0x04603C00                                   )
    // BSL1N   <Zdn>.D, <Zdn>.D, <Zm>.D, <Zk>.D                                          SVE_AV_3A           00000100011mmmmm 001111kkkkkddddd     0460 3C00   

INST1(bsl2n,             "bsl2n",                 0,                       IF_SVE_AV_3A,            0x04A03C00                                   )
    // BSL2N   <Zdn>.D, <Zdn>.D, <Zm>.D, <Zk>.D                                          SVE_AV_3A           00000100101mmmmm 001111kkkkkddddd     04A0 3C00   

INST1(eor3,              "eor3",                  0,                       IF_SVE_AV_3A,            0x04203800                                   )
    // EOR3    <Zdn>.D, <Zdn>.D, <Zm>.D, <Zk>.D                                          SVE_AV_3A           00000100001mmmmm 001110kkkkkddddd     0420 3800   

INST1(nbsl,              "nbsl",                  0,                       IF_SVE_AV_3A,            0x04E03C00                                   )
    // NBSL    <Zdn>.D, <Zdn>.D, <Zm>.D, <Zk>.D                                          SVE_AV_3A           00000100111mmmmm 001111kkkkkddddd     04E0 3C00   


//    enum               name                     info                                              SVE_HG_2A                                    
INST1(bfcvtn,            "bfcvtn",                0,                       IF_SVE_HG_2A,            0x650A3800                                   )
    // BFCVTN  <Zd>.B, {<Zn1>.H-<Zn2>.H }                                                SVE_HG_2A           0110010100001010 001110nnnn0ddddd     650A 3800   

INST1(fcvtn,             "fcvtn",                 NRW,                     IF_SVE_HG_2A,            0x650A3000                                   )
    // FCVTN   <Zd>.B, {<Zn1>.H-<Zn2>.H }                                                SVE_HG_2A           0110010100001010 001100nnnn0ddddd     650A 3000   

INST1(fcvtnb,            "fcvtnb",                0,                       IF_SVE_HG_2A,            0x650A3400                                   )
    // FCVTNB  <Zd>.B, {<Zn1>.S-<Zn2>.S }                                                SVE_HG_2A           0110010100001010 001101nnnn0ddddd     650A 3400   


//    enum               name                     info                                              SVE_GP_3A                                    
INST1(fcadd,             "fcadd",                 0,                       IF_SVE_GP_3A,            0x64008000                                   )
    // FCADD   <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>, <const>                           SVE_GP_3A           01100100xx00000r 100gggmmmmmddddd     6400 8000   


//    enum               name                     info                                              SVE_FO_3A                                    
INST1(smmla,             "smmla",                 0,                       IF_SVE_FO_3A,            0x45009800                                   )
    // SMMLA   <Zda>.S, <Zn>.B, <Zm>.B                                                   SVE_FO_3A           01000101000mmmmm 100110nnnnnddddd     4500 9800   

INST1(ummla,             "ummla",                 0,                       IF_SVE_FO_3A,            0x45C09800                                   )
    // UMMLA   <Zda>.S, <Zn>.B, <Zm>.B                                                   SVE_FO_3A           01000101110mmmmm 100110nnnnnddddd     45C0 9800   

INST1(usmmla,            "usmmla",                0,                       IF_SVE_FO_3A,            0x45809800                                   )
    // USMMLA  <Zda>.S, <Zn>.B, <Zm>.B                                                   SVE_FO_3A           01000101100mmmmm 100110nnnnnddddd     4580 9800   


//    enum               name                     info                                              SVE_HD_3A                                    
INST1(bfmmla,            "bfmmla",                0,                       IF_SVE_HD_3A,            0x6460E400                                   )
    // BFMMLA  <Zda>.S, <Zn>.H, <Zm>.H                                                   SVE_HD_3A           01100100011mmmmm 111001nnnnnddddd     6460 E400   


//    enum               name                     info                                              SVE_EQ_3A                                    
INST1(sadalp,            "sadalp",                LNG,                     IF_SVE_EQ_3A,            0x4404A000                                   )
    // SADALP  <Zda>.<T>, <Pg>/M, <Zn>.<Tb>                                              SVE_EQ_3A           01000100xx000100 101gggnnnnnddddd     4404 A000   

INST1(uadalp,            "uadalp",                LNG,                     IF_SVE_EQ_3A,            0x4405A000                                   )
    // UADALP  <Zda>.<T>, <Pg>/M, <Zn>.<Tb>                                              SVE_EQ_3A           01000100xx000101 101gggnnnnnddddd     4405 A000   


//    enum               name                     info                                              SVE_HQ_3A                                    
INST1(frinta,            "frinta",                0,                       IF_SVE_HQ_3A,            0x6504A000                                   )
    // FRINTA  <Zd>.<T>, <Pg>/M, <Zn>.<T>                                                SVE_HQ_3A           01100101xx000100 101gggnnnnnddddd     6504 A000   

INST1(frinti,            "frinti",                0,                       IF_SVE_HQ_3A,            0x6507A000                                   )
    // FRINTI  <Zd>.<T>, <Pg>/M, <Zn>.<T>                                                SVE_HQ_3A           01100101xx000111 101gggnnnnnddddd     6507 A000   

INST1(frintm,            "frintm",                0,                       IF_SVE_HQ_3A,            0x6502A000                                   )
    // FRINTM  <Zd>.<T>, <Pg>/M, <Zn>.<T>                                                SVE_HQ_3A           01100101xx000010 101gggnnnnnddddd     6502 A000   

INST1(frintn,            "frintn",                0,                       IF_SVE_HQ_3A,            0x6500A000                                   )
    // FRINTN  <Zd>.<T>, <Pg>/M, <Zn>.<T>                                                SVE_HQ_3A           01100101xx000000 101gggnnnnnddddd     6500 A000   

INST1(frintp,            "frintp",                0,                       IF_SVE_HQ_3A,            0x6501A000                                   )
    // FRINTP  <Zd>.<T>, <Pg>/M, <Zn>.<T>                                                SVE_HQ_3A           01100101xx000001 101gggnnnnnddddd     6501 A000   

INST1(frintx,            "frintx",                0,                       IF_SVE_HQ_3A,            0x6506A000                                   )
    // FRINTX  <Zd>.<T>, <Pg>/M, <Zn>.<T>                                                SVE_HQ_3A           01100101xx000110 101gggnnnnnddddd     6506 A000   

INST1(frintz,            "frintz",                0,                       IF_SVE_HQ_3A,            0x6503A000                                   )
    // FRINTZ  <Zd>.<T>, <Pg>/M, <Zn>.<T>                                                SVE_HQ_3A           01100101xx000011 101gggnnnnnddddd     6503 A000   


//    enum               name                     info                                              SVE_EZ_3A                                    
INST1(sudot,             "sudot",                 0,                       IF_SVE_EZ_3A,            0x44A01C00                                   )
    // SUDOT   <Zda>.S, <Zn>.B, <Zm>.B[<imm>]                                            SVE_EZ_3A           01000100101iimmm 000111nnnnnddddd     44A0 1C00   


//    enum               name                     info                                              SVE_GK_2A                                    
INST1(aesd,              "aesd",                  0,                       IF_SVE_GK_2A,            0x4522E400                                   )
    // AESD    <Zdn>.B, <Zdn>.B, <Zm>.B                                                  SVE_GK_2A           0100010100100010 111001mmmmmddddd     4522 E400   

INST1(aese,              "aese",                  0,                       IF_SVE_GK_2A,            0x4522E000                                   )
    // AESE    <Zdn>.B, <Zdn>.B, <Zm>.B                                                  SVE_GK_2A           0100010100100010 111000mmmmmddddd     4522 E000   

INST1(sm4e,              "sm4e",                  0,                       IF_SVE_GK_2A,            0x4523E000                                   )
    // SM4E    <Zdn>.S, <Zdn>.S, <Zm>.S                                                  SVE_GK_2A           0100010100100011 111000mmmmmddddd     4523 E000   


//    enum               name                     info                                              SVE_GL_1A                                    
INST1(aesimc,            "aesimc",                0,                       IF_SVE_GL_1A,            0x4520E400                                   )
    // AESIMC  <Zdn>.B, <Zdn>.B                                                          SVE_GL_1A           0100010100100000 11100100000ddddd     4520 E400   

INST1(aesmc,             "aesmc",                 0,                       IF_SVE_GL_1A,            0x4520E000                                   )
    // AESMC   <Zdn>.B, <Zdn>.B                                                          SVE_GL_1A           0100010100100000 11100000000ddddd     4520 E000   


//    enum               name                     info                                              SVE_GJ_3A                                    
INST1(rax1,              "rax1",                  0,                       IF_SVE_GJ_3A,            0x4520F400                                   )
    // RAX1    <Zd>.D, <Zn>.D, <Zm>.D                                                    SVE_GJ_3A           01000101001mmmmm 111101nnnnnddddd     4520 F400   

INST1(sm4ekey,           "sm4ekey",               0,                       IF_SVE_GJ_3A,            0x4520F000                                   )
    // SM4EKEY <Zd>.S, <Zn>.S, <Zm>.S                                                    SVE_GJ_3A           01000101001mmmmm 111100nnnnnddddd     4520 F000   


//    enum               name                     info                                              SVE_AW_2A                                    
INST1(xar,               "xar",                   0,                       IF_SVE_AW_2A,            0x04203400                                   )
    // XAR     <Zdn>.<T>, <Zdn>.<T>, <Zm>.<T>, #<const>                                  SVE_AW_2A           00000100xx1xxiii 001101mmmmmddddd     0420 3400   


//    enum               name                     info                                              SVE_HO_3A                                    
INST1(bfcvt,             "bfcvt",                 0,                       IF_SVE_HO_3A,            0x658AA000                                   )
    // BFCVT   <Zd>.H, <Pg>/M, <Zn>.S                                                    SVE_HO_3A           0110010110001010 101gggnnnnnddddd     658A A000   

INST1(fcvtx,             "fcvtx",                 0,                       IF_SVE_HO_3A,            0x650AA000                                   )
    // FCVTX   <Zd>.S, <Pg>/M, <Zn>.D                                                    SVE_HO_3A           0110010100001010 101gggnnnnnddddd     650A A000   


//    enum               name                     info                                              SVE_AF_3A                                    
INST1(andv,              "andv",                  0,                       IF_SVE_AF_3A,            0x041A2000                                   )
    // ANDV    <V><d>, <Pg>, <Zn>.<T>                                                    SVE_AF_3A           00000100xx011010 001gggnnnnnddddd     041A 2000   

INST1(eorv,              "eorv",                  0,                       IF_SVE_AF_3A,            0x04192000                                   )
    // EORV    <V><d>, <Pg>, <Zn>.<T>                                                    SVE_AF_3A           00000100xx011001 001gggnnnnnddddd     0419 2000   

INST1(orv,               "orv",                   0,                       IF_SVE_AF_3A,            0x04182000                                   )
    // ORV     <V><d>, <Pg>, <Zn>.<T>                                                    SVE_AF_3A           00000100xx011000 001gggnnnnnddddd     0418 2000   


//    enum               name                     info                                              SVE_AG_3A                                    
INST1(andqv,             "andqv",                 0,                       IF_SVE_AG_3A,            0x041E2000                                   )
    // ANDQV   <Vd>.<T>, <Pg>, <Zn>.<Tb>                                                 SVE_AG_3A           00000100xx011110 001gggnnnnnddddd     041E 2000   

INST1(eorqv,             "eorqv",                 0,                       IF_SVE_AG_3A,            0x041D2000                                   )
    // EORQV   <Vd>.<T>, <Pg>, <Zn>.<Tb>                                                 SVE_AG_3A           00000100xx011101 001gggnnnnnddddd     041D 2000   

INST1(orqv,              "orqv",                  0,                       IF_SVE_AG_3A,            0x041C2000                                   )
    // ORQV    <Vd>.<T>, <Pg>, <Zn>.<Tb>                                                 SVE_AG_3A           00000100xx011100 001gggnnnnnddddd     041C 2000   


//    enum               name                     info                                              SVE_AI_3A                                    
INST1(saddv,             "saddv",                 0,                       IF_SVE_AI_3A,            0x04002000                                   )
    // SADDV   <Dd>, <Pg>, <Zn>.<T>                                                      SVE_AI_3A           00000100xx000000 001gggnnnnnddddd     0400 2000   

INST1(uaddv,             "uaddv",                 0,                       IF_SVE_AI_3A,            0x04012000                                   )
    // UADDV   <Dd>, <Pg>, <Zn>.<T>                                                      SVE_AI_3A           00000100xx000001 001gggnnnnnddddd     0401 2000   


//    enum               name                     info                                              SVE_AJ_3A                                    
INST1(addqv,             "addqv",                 0,                       IF_SVE_AJ_3A,            0x04052000                                   )
    // ADDQV   <Vd>.<T>, <Pg>, <Zn>.<Tb>                                                 SVE_AJ_3A           00000100xx000101 001gggnnnnnddddd     0405 2000   


//    enum               name                     info                                              SVE_AL_3A                                    
INST1(smaxqv,            "smaxqv",                0,                       IF_SVE_AL_3A,            0x040C2000                                   )
    // SMAXQV  <Vd>.<T>, <Pg>, <Zn>.<Tb>                                                 SVE_AL_3A           00000100xx001100 001gggnnnnnddddd     040C 2000   

INST1(sminqv,            "sminqv",                0,                       IF_SVE_AL_3A,            0x040E2000                                   )
    // SMINQV  <Vd>.<T>, <Pg>, <Zn>.<Tb>                                                 SVE_AL_3A           00000100xx001110 001gggnnnnnddddd     040E 2000   

INST1(umaxqv,            "umaxqv",                0,                       IF_SVE_AL_3A,            0x040D2000                                   )
    // UMAXQV  <Vd>.<T>, <Pg>, <Zn>.<Tb>                                                 SVE_AL_3A           00000100xx001101 001gggnnnnnddddd     040D 2000   

INST1(uminqv,            "uminqv",                0,                       IF_SVE_AL_3A,            0x040F2000                                   )
    // UMINQV  <Vd>.<T>, <Pg>, <Zn>.<Tb>                                                 SVE_AL_3A           00000100xx001111 001gggnnnnnddddd     040F 2000   


//    enum               name                     info                                              SVE_AN_3A                                    
INST1(asrr,              "asrr",                  RSH,                     IF_SVE_AN_3A,            0x04148000                                   )
    // ASRR    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_AN_3A           00000100xx010100 100gggmmmmmddddd     0414 8000   

INST1(lslr,              "lslr",                  RSH,                     IF_SVE_AN_3A,            0x04178000                                   )
    // LSLR    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_AN_3A           00000100xx010111 100gggmmmmmddddd     0417 8000   

INST1(lsrr,              "lsrr",                  RSH,                     IF_SVE_AN_3A,            0x04158000                                   )
    // LSRR    <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>                                    SVE_AN_3A           00000100xx010101 100gggmmmmmddddd     0415 8000   


//    enum               name                     info                                              SVE_AS_4A                                    
INST1(mad,               "mad",                   0,                       IF_SVE_AS_4A,            0x0400C000                                   )
    // MAD     <Zdn>.<T>, <Pg>/M, <Zm>.<T>, <Za>.<T>                                     SVE_AS_4A           00000100xx0mmmmm 110gggaaaaaddddd     0400 C000   

INST1(msb,               "msb",                   0,                       IF_SVE_AS_4A,            0x0400E000                                   )
    // MSB     <Zdn>.<T>, <Pg>/M, <Zm>.<T>, <Za>.<T>                                     SVE_AS_4A           00000100xx0mmmmm 111gggaaaaaddddd     0400 E000   


//    enum               name                     info                                              SVE_BB_2A                                    
INST1(addpl,             "addpl",                 0,                       IF_SVE_BB_2A,            0x04605000                                   )
    // ADDPL   <Xd|SP>, <Xn|SP>, #<imm>                                                  SVE_BB_2A           00000100011nnnnn 01010iiiiiiddddd     0460 5000   

INST1(addvl,             "addvl",                 0,                       IF_SVE_BB_2A,            0x04205000                                   )
    // ADDVL   <Xd|SP>, <Xn|SP>, #<imm>                                                  SVE_BB_2A           00000100001nnnnn 01010iiiiiiddddd     0420 5000   


//    enum               name                     info                                              SVE_BC_1A                                    
INST1(rdvl,              "rdvl",                  0,                       IF_SVE_BC_1A,            0x04BF5000                                   )
    // RDVL    <Xd>, #<imm>                                                              SVE_BC_1A           0000010010111111 01010iiiiiiddddd     04BF 5000   


//    enum               name                     info                                              SVE_BJ_2A                                    
INST1(fexpa,             "fexpa",                 0,                       IF_SVE_BJ_2A,            0x0420B800                                   )
    // FEXPA   <Zd>.<T>, <Zn>.<T>                                                        SVE_BJ_2A           00000100xx100000 101110nnnnnddddd     0420 B800   


//    enum               name                     info                                              SVE_BK_3A                                    
INST1(ftssel,            "ftssel",                0,                       IF_SVE_BK_3A,            0x0420B000                                   )
    // FTSSEL  <Zd>.<T>, <Zn>.<T>, <Zm>.<T>                                              SVE_BK_3A           00000100xx1mmmmm 101100nnnnnddddd     0420 B000   


//    enum               name                     info                                              SVE_BL_1A                                    
INST1(cntb,              "cntb",                  0,                       IF_SVE_BL_1A,            0x0420E000                                   )
    // CNTB    <Xd>{, <pattern>{, MUL #<imm>}}                                           SVE_BL_1A           000001000010iiii 111000pppppddddd     0420 E000   

INST1(cntd,              "cntd",                  0,                       IF_SVE_BL_1A,            0x04E0E000                                   )
    // CNTD    <Xd>{, <pattern>{, MUL #<imm>}}                                           SVE_BL_1A           000001001110iiii 111000pppppddddd     04E0 E000   

INST1(cnth,              "cnth",                  0,                       IF_SVE_BL_1A,            0x0460E000                                   )
    // CNTH    <Xd>{, <pattern>{, MUL #<imm>}}                                           SVE_BL_1A           000001000110iiii 111000pppppddddd     0460 E000   

INST1(cntw,              "cntw",                  0,                       IF_SVE_BL_1A,            0x04A0E000                                   )
    // CNTW    <Xd>{, <pattern>{, MUL #<imm>}}                                           SVE_BL_1A           000001001010iiii 111000pppppddddd     04A0 E000   


//    enum               name                     info                                              SVE_BM_1A                                    
INST1(decb,              "decb",                  0,                       IF_SVE_BM_1A,            0x0430E400                                   )
    // DECB    <Xdn>{, <pattern>{, MUL #<imm>}}                                          SVE_BM_1A           000001000011iiii 111001pppppddddd     0430 E400   

INST1(incb,              "incb",                  0,                       IF_SVE_BM_1A,            0x0430E000                                   )
    // INCB    <Xdn>{, <pattern>{, MUL #<imm>}}                                          SVE_BM_1A           000001000011iiii 111000pppppddddd     0430 E000   


//    enum               name                     info                                              SVE_BO_1A                                    
INST1(sqdecb,            "sqdecb",                0,                       IF_SVE_BO_1A,            0x0420F800                                   )
    // SQDECB  <Xdn>, <Wdn>{, <pattern>{, MUL #<imm>}}                                   SVE_BO_1A           00000100001Xiiii 111110pppppddddd     0420 F800   

INST1(sqincb,            "sqincb",                0,                       IF_SVE_BO_1A,            0x0420F000                                   )
    // SQINCB  <Xdn>, <Wdn>{, <pattern>{, MUL #<imm>}}                                   SVE_BO_1A           00000100001Xiiii 111100pppppddddd     0420 F000   

INST1(uqdecb,            "uqdecb",                0,                       IF_SVE_BO_1A,            0x0420FC00                                   )
    // UQDECB  <Wdn>{, <pattern>{, MUL #<imm>}}                                          SVE_BO_1A           00000100001Xiiii 111111pppppddddd     0420 FC00   

INST1(uqincb,            "uqincb",                0,                       IF_SVE_BO_1A,            0x0420F400                                   )
    // UQINCB  <Wdn>{, <pattern>{, MUL #<imm>}}                                          SVE_BO_1A           00000100001Xiiii 111101pppppddddd     0420 F400   


//    enum               name                     info                                              SVE_BT_1A                                    
INST1(dupm,              "dupm",                  0,                       IF_SVE_BT_1A,            0x05C00000                                   )
    // DUPM    <Zd>.<T>, #<const>                                                        SVE_BT_1A           00000101110000ii iiiiiiiiiiiddddd     05C0 0000   


//    enum               name                     info                                              SVE_BU_2A                                    
INST1(fcpy,              "fcpy",                  0,                       IF_SVE_BU_2A,            0x0510C000                                   )
    // FCPY    <Zd>.<T>, <Pg>/M, #<const>                                                SVE_BU_2A           00000101xx01gggg 110iiiiiiiiddddd     0510 C000   


//    enum               name                     info                                              SVE_BX_2A                                    
INST1(dupq,              "dupq",                  0,                       IF_SVE_BX_2A,            0x05202400                                   )
    // DUPQ    <Zd>.<T>, <Zn>.<T>[<imm>]                                                 SVE_BX_2A           00000101001ixxxx 001001nnnnnddddd     0520 2400   


//    enum               name                     info                                              SVE_BY_2A                                    
INST1(extq,              "extq",                  0,                       IF_SVE_BY_2A,            0x05602400                                   )
    // EXTQ    <Zdn>.B, <Zdn>.B, <Zm>.B, #<imm>                                          SVE_BY_2A           000001010110iiii 001001mmmmmddddd     0560 2400   


//    enum               name                     info                                              SVE_CA_3A                                    
INST1(tbxq,              "tbxq",                  0,                       IF_SVE_CA_3A,            0x05203400                                   )
    // TBXQ    <Zd>.<T>, <Zn>.<T>, <Zm>.<T>                                              SVE_CA_3A           00000101xx1mmmmm 001101nnnnnddddd     0520 3400   


//    enum               name                     info                                              SVE_CH_2A                                    
INST1(sunpkhi,           "sunpkhi",               0,                       IF_SVE_CH_2A,            0x05313800                                   )
    // SUNPKHI <Zd>.<T>, <Zn>.<Tb>                                                       SVE_CH_2A           00000101xx110001 001110nnnnnddddd     0531 3800   

INST1(sunpklo,           "sunpklo",               0,                       IF_SVE_CH_2A,            0x05303800                                   )
    // SUNPKLO <Zd>.<T>, <Zn>.<Tb>                                                       SVE_CH_2A           00000101xx110000 001110nnnnnddddd     0530 3800   

INST1(uunpkhi,           "uunpkhi",               0,                       IF_SVE_CH_2A,            0x05333800                                   )
    // UUNPKHI <Zd>.<T>, <Zn>.<Tb>                                                       SVE_CH_2A           00000101xx110011 001110nnnnnddddd     0533 3800   

INST1(uunpklo,           "uunpklo",               0,                       IF_SVE_CH_2A,            0x05323800                                   )
    // UUNPKLO <Zd>.<T>, <Zn>.<Tb>                                                       SVE_CH_2A           00000101xx110010 001110nnnnnddddd     0532 3800   


//    enum               name                     info                                              SVE_CK_2A                                    
INST1(punpkhi,           "punpkhi",               0,                       IF_SVE_CK_2A,            0x05314000                                   )
    // PUNPKHI <Pd>.H, <Pn>.B                                                            SVE_CK_2A           0000010100110001 0100000NNNN0DDDD     0531 4000   

INST1(punpklo,           "punpklo",               0,                       IF_SVE_CK_2A,            0x05304000                                   )
    // PUNPKLO <Pd>.H, <Pn>.B                                                            SVE_CK_2A           0000010100110000 0100000NNNN0DDDD     0530 4000   


//    enum               name                     info                                              SVE_CL_3A                                    
INST1(compact,           "compact",               0,                       IF_SVE_CL_3A,            0x05218000                                   )
    // COMPACT <Zd>.<T>, <Pg>, <Zn>.<T>                                                  SVE_CL_3A           00000101xx100001 100gggnnnnnddddd     0521 8000   


//    enum               name                     info                                              SVE_CT_3A                                    
INST1(revd,              "revd",                  0,                       IF_SVE_CT_3A,            0x052E8000                                   )
    // REVD    <Zd>.Q, <Pg>/M, <Zn>.Q                                                    SVE_CT_3A           0000010100101110 100gggnnnnnddddd     052E 8000   


//    enum               name                     info                                              SVE_DA_4A                                    
INST1(brkpa,             "brkpa",                 0,                       IF_SVE_DA_4A,            0x2500C000                                   )
    // BRKPA   <Pd>.B, <Pg>/Z, <Pn>.B, <Pm>.B                                            SVE_DA_4A           001001010000MMMM 11gggg0NNNN0DDDD     2500 C000   

INST1(brkpas,            "brkpas",                0,                       IF_SVE_DA_4A,            0x2540C000                                   )
    // BRKPAS  <Pd>.B, <Pg>/Z, <Pn>.B, <Pm>.B                                            SVE_DA_4A           001001010100MMMM 11gggg0NNNN0DDDD     2540 C000   

INST1(brkpb,             "brkpb",                 0,                       IF_SVE_DA_4A,            0x2500C010                                   )
    // BRKPB   <Pd>.B, <Pg>/Z, <Pn>.B, <Pm>.B                                            SVE_DA_4A           001001010000MMMM 11gggg0NNNN1DDDD     2500 C010   

INST1(brkpbs,            "brkpbs",                0,                       IF_SVE_DA_4A,            0x2540C010                                   )
    // BRKPBS  <Pd>.B, <Pg>/Z, <Pn>.B, <Pm>.B                                            SVE_DA_4A           001001010100MMMM 11gggg0NNNN1DDDD     2540 C010   


//    enum               name                     info                                              SVE_DB_3A                                    
INST1(brka,              "brka",                  0,                       IF_SVE_DB_3A,            0x25104000                                   )
    // BRKA    <Pd>.B, <Pg>/<ZM>, <Pn>.B                                                 SVE_DB_3A           0010010100010000 01gggg0NNNNMDDDD     2510 4000   

INST1(brkb,              "brkb",                  0,                       IF_SVE_DB_3A,            0x25904000                                   )
    // BRKB    <Pd>.B, <Pg>/<ZM>, <Pn>.B                                                 SVE_DB_3A           0010010110010000 01gggg0NNNNMDDDD     2590 4000   


//    enum               name                     info                                              SVE_DB_3B                                    
INST1(brkas,             "brkas",                 0,                       IF_SVE_DB_3B,            0x25504000                                   )
    // BRKAS   <Pd>.B, <Pg>/Z, <Pn>.B                                                    SVE_DB_3B           0010010101010000 01gggg0NNNN0DDDD     2550 4000   

INST1(brkbs,             "brkbs",                 0,                       IF_SVE_DB_3B,            0x25D04000                                   )
    // BRKBS   <Pd>.B, <Pg>/Z, <Pn>.B                                                    SVE_DB_3B           0010010111010000 01gggg0NNNN0DDDD     25D0 4000   


//    enum               name                     info                                              SVE_DC_3A                                    
INST1(brkn,              "brkn",                  0,                       IF_SVE_DC_3A,            0x25184000                                   )
    // BRKN    <Pdm>.B, <Pg>/Z, <Pn>.B, <Pdm>.B                                          SVE_DC_3A           0010010100011000 01gggg0NNNN0MMMM     2518 4000   

INST1(brkns,             "brkns",                 0,                       IF_SVE_DC_3A,            0x25584000                                   )
    // BRKNS   <Pdm>.B, <Pg>/Z, <Pn>.B, <Pdm>.B                                          SVE_DC_3A           0010010101011000 01gggg0NNNN0MMMM     2558 4000   


//    enum               name                     info                                              SVE_DD_2A                                    
INST1(pfirst,            "pfirst",                0,                       IF_SVE_DD_2A,            0x2558C000                                   )
    // PFIRST  <Pdn>.B, <Pg>, <Pdn>.B                                                    SVE_DD_2A           0010010101011000 1100000gggg0DDDD     2558 C000   


//    enum               name                     info                                              SVE_DE_1A                                    
INST1(ptrues,            "ptrues",                0,                       IF_SVE_DE_1A,            0x2519E000                                   )
    // PTRUES  <Pd>.<T>{, <pattern>}                                                     SVE_DE_1A           00100101xx011001 111000ppppp0DDDD     2519 E000   


//    enum               name                     info                                              SVE_DF_2A                                    
INST1(pnext,             "pnext",                 0,                       IF_SVE_DF_2A,            0x2519C400                                   )
    // PNEXT   <Pdn>.<T>, <Pv>, <Pdn>.<T>                                                SVE_DF_2A           00100101xx011001 1100010VVVV0DDDD     2519 C400   


//    enum               name                     info                                              SVE_DG_2A                                    
INST1(rdffrs,            "rdffrs",                0,                       IF_SVE_DG_2A,            0x2558F000                                   )
    // RDFFRS  <Pd>.B, <Pg>/Z                                                            SVE_DG_2A           0010010101011000 1111000gggg0DDDD     2558 F000   


//    enum               name                     info                                              SVE_DI_2A                                    
INST1(ptest,             "ptest",                 0,                       IF_SVE_DI_2A,            0x2550C000                                   )
    // PTEST   <Pg>, <Pn>.B                                                              SVE_DI_2A           0010010101010000 11gggg0NNNN00000     2550 C000   


//    enum               name                     info                                              SVE_DJ_1A                                    
INST1(pfalse,            "pfalse",                0,                       IF_SVE_DJ_1A,            0x2518E400                                   )
    // PFALSE  <Pd>.B                                                                    SVE_DJ_1A           0010010100011000 111001000000DDDD     2518 E400   


//    enum               name                     info                                              SVE_DQ_0A                                    
INST1(setffr,            "setffr",                0,                       IF_SVE_DQ_0A,            0x252C9000                                   )
    // SETFFR                                                                            SVE_DQ_0A           0010010100101100 1001000000000000     252C 9000   


//    enum               name                     info                                              SVE_DR_1A                                    
INST1(wrffr,             "wrffr",                 0,                       IF_SVE_DR_1A,            0x25289000                                   )
    // WRFFR   <Pn>.B                                                                    SVE_DR_1A           0010010100101000 1001000NNNN00000     2528 9000   


//    enum               name                     info                                              SVE_DS_2A                                    
INST1(ctermeq,           "ctermeq",               0,                       IF_SVE_DS_2A,            0x25A02000                                   )
    // CTERMEQ <R><n>, <R><m>                                                            SVE_DS_2A           001001011x1mmmmm 001000nnnnn00000     25A0 2000   

INST1(ctermne,           "ctermne",               0,                       IF_SVE_DS_2A,            0x25A02010                                   )
    // CTERMNE <R><n>, <R><m>                                                            SVE_DS_2A           001001011x1mmmmm 001000nnnnn10000     25A0 2010   


//    enum               name                     info                                              SVE_DU_3A                                    
INST1(whilerw,           "whilerw",               0,                       IF_SVE_DU_3A,            0x25203010                                   )
    // WHILERW <Pd>.<T>, <Xn>, <Xm>                                                      SVE_DU_3A           00100101xx1mmmmm 001100nnnnn1DDDD     2520 3010   

INST1(whilewr,           "whilewr",               0,                       IF_SVE_DU_3A,            0x25203000                                   )
    // WHILEWR <Pd>.<T>, <Xn>, <Xm>                                                      SVE_DU_3A           00100101xx1mmmmm 001100nnnnn0DDDD     2520 3000   


//    enum               name                     info                                              SVE_DV_4A                                    
INST1(psel,              "psel",                  0,                       IF_SVE_DV_4A,            0x25204000                                   )
    // PSEL    <Pd>, <Pn>, <Pm>.<T>[<Wv>, <imm>]                                         SVE_DV_4A           00100101ix1xxxvv 01NNNN0MMMM0DDDD     2520 4000   


//    enum               name                     info                                              SVE_EA_1A                                    
INST1(fdup,              "fdup",                  0,                       IF_SVE_EA_1A,            0x2539C000                                   )
    // FDUP    <Zd>.<T>, #<const>                                                        SVE_EA_1A           00100101xx111001 110iiiiiiiiddddd     2539 C000   


//    enum               name                     info                                              SVE_EN_3A                                    
INST1(sqdmlalbt,         "sqdmlalbt",             0,                       IF_SVE_EN_3A,            0x44000800                                   )
    // SQDMLALBT <Zda>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                         SVE_EN_3A           01000100xx0mmmmm 000010nnnnnddddd     4400 0800   

INST1(sqdmlslbt,         "sqdmlslbt",             0,                       IF_SVE_EN_3A,            0x44000C00                                   )
    // SQDMLSLBT <Zda>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                         SVE_EN_3A           01000100xx0mmmmm 000011nnnnnddddd     4400 0C00   


//    enum               name                     info                                              SVE_EV_3A                                    
INST1(sclamp,            "sclamp",                0,                       IF_SVE_EV_3A,            0x4400C000                                   )
    // SCLAMP  <Zd>.<T>, <Zn>.<T>, <Zm>.<T>                                              SVE_EV_3A           01000100xx0mmmmm 110000nnnnnddddd     4400 C000   

INST1(uclamp,            "uclamp",                0,                       IF_SVE_EV_3A,            0x4400C400                                   )
    // UCLAMP  <Zd>.<T>, <Zn>.<T>, <Zm>.<T>                                              SVE_EV_3A           01000100xx0mmmmm 110001nnnnnddddd     4400 C400   


//    enum               name                     info                                              SVE_EW_3A                                    
INST1(mlapt,             "mlapt",                 0,                       IF_SVE_EW_3A,            0x44C0D000                                   )
    // MLAPT   <Zda>.D, <Zn>.D, <Zm>.D                                                   SVE_EW_3A           01000100110mmmmm 110100nnnnnddddd     44C0 D000   


//    enum               name                     info                                              SVE_EW_3B                                    
INST1(madpt,             "madpt",                 0,                       IF_SVE_EW_3B,            0x44C0D800                                   )
    // MADPT   <Zdn>.D, <Zm>.D, <Za>.D                                                   SVE_EW_3B           01000100110mmmmm 110110aaaaaddddd     44C0 D800   


//    enum               name                     info                                              SVE_EX_3A                                    
INST1(tblq,              "tblq",                  0,                       IF_SVE_EX_3A,            0x4400F800                                   )
    // TBLQ    <Zd>.<T>, {<Zn>.<T>}, <Zm>.<T>                                            SVE_EX_3A           01000100xx0mmmmm 111110nnnnnddddd     4400 F800   

INST1(uzpq1,             "uzpq1",                 0,                       IF_SVE_EX_3A,            0x4400E800                                   )
    // UZPQ1   <Zd>.<T>, <Zn>.<T>, <Zm>.<T>                                              SVE_EX_3A           01000100xx0mmmmm 111010nnnnnddddd     4400 E800   

INST1(uzpq2,             "uzpq2",                 0,                       IF_SVE_EX_3A,            0x4400EC00                                   )
    // UZPQ2   <Zd>.<T>, <Zn>.<T>, <Zm>.<T>                                              SVE_EX_3A           01000100xx0mmmmm 111011nnnnnddddd     4400 EC00   

INST1(zipq1,             "zipq1",                 0,                       IF_SVE_EX_3A,            0x4400E000                                   )
    // ZIPQ1   <Zd>.<T>, <Zn>.<T>, <Zm>.<T>                                              SVE_EX_3A           01000100xx0mmmmm 111000nnnnnddddd     4400 E000   

INST1(zipq2,             "zipq2",                 0,                       IF_SVE_EX_3A,            0x4400E400                                   )
    // ZIPQ2   <Zd>.<T>, <Zn>.<T>, <Zm>.<T>                                              SVE_EX_3A           01000100xx0mmmmm 111001nnnnnddddd     4400 E400   


//    enum               name                     info                                              SVE_FL_3A                                    
INST1(sabdlb,            "sabdlb",                0,                       IF_SVE_FL_3A,            0x45003000                                   )
    // SABDLB  <Zd>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                            SVE_FL_3A           01000101xx0mmmmm 001100nnnnnddddd     4500 3000   

INST1(sabdlt,            "sabdlt",                0,                       IF_SVE_FL_3A,            0x45003400                                   )
    // SABDLT  <Zd>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                            SVE_FL_3A           01000101xx0mmmmm 001101nnnnnddddd     4500 3400   

INST1(saddlb,            "saddlb",                0,                       IF_SVE_FL_3A,            0x45000000                                   )
    // SADDLB  <Zd>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                            SVE_FL_3A           01000101xx0mmmmm 000000nnnnnddddd     4500 0000   

INST1(saddlt,            "saddlt",                0,                       IF_SVE_FL_3A,            0x45000400                                   )
    // SADDLT  <Zd>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                            SVE_FL_3A           01000101xx0mmmmm 000001nnnnnddddd     4500 0400   

INST1(ssublb,            "ssublb",                0,                       IF_SVE_FL_3A,            0x45001000                                   )
    // SSUBLB  <Zd>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                            SVE_FL_3A           01000101xx0mmmmm 000100nnnnnddddd     4500 1000   

INST1(ssublt,            "ssublt",                0,                       IF_SVE_FL_3A,            0x45001400                                   )
    // SSUBLT  <Zd>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                            SVE_FL_3A           01000101xx0mmmmm 000101nnnnnddddd     4500 1400   

INST1(uabdlb,            "uabdlb",                0,                       IF_SVE_FL_3A,            0x45003800                                   )
    // UABDLB  <Zd>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                            SVE_FL_3A           01000101xx0mmmmm 001110nnnnnddddd     4500 3800   

INST1(uabdlt,            "uabdlt",                0,                       IF_SVE_FL_3A,            0x45003C00                                   )
    // UABDLT  <Zd>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                            SVE_FL_3A           01000101xx0mmmmm 001111nnnnnddddd     4500 3C00   

INST1(uaddlb,            "uaddlb",                0,                       IF_SVE_FL_3A,            0x45000800                                   )
    // UADDLB  <Zd>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                            SVE_FL_3A           01000101xx0mmmmm 000010nnnnnddddd     4500 0800   

INST1(uaddlt,            "uaddlt",                0,                       IF_SVE_FL_3A,            0x45000C00                                   )
    // UADDLT  <Zd>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                            SVE_FL_3A           01000101xx0mmmmm 000011nnnnnddddd     4500 0C00   

INST1(usublb,            "usublb",                0,                       IF_SVE_FL_3A,            0x45001800                                   )
    // USUBLB  <Zd>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                            SVE_FL_3A           01000101xx0mmmmm 000110nnnnnddddd     4500 1800   

INST1(usublt,            "usublt",                0,                       IF_SVE_FL_3A,            0x45001C00                                   )
    // USUBLT  <Zd>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                            SVE_FL_3A           01000101xx0mmmmm 000111nnnnnddddd     4500 1C00   


//    enum               name                     info                                              SVE_FM_3A                                    
INST1(saddwb,            "saddwb",                0,                       IF_SVE_FM_3A,            0x45004000                                   )
    // SADDWB  <Zd>.<T>, <Zn>.<T>, <Zm>.<Tb>                                             SVE_FM_3A           01000101xx0mmmmm 010000nnnnnddddd     4500 4000   

INST1(saddwt,            "saddwt",                0,                       IF_SVE_FM_3A,            0x45004400                                   )
    // SADDWT  <Zd>.<T>, <Zn>.<T>, <Zm>.<Tb>                                             SVE_FM_3A           01000101xx0mmmmm 010001nnnnnddddd     4500 4400   

INST1(ssubwb,            "ssubwb",                0,                       IF_SVE_FM_3A,            0x45005000                                   )
    // SSUBWB  <Zd>.<T>, <Zn>.<T>, <Zm>.<Tb>                                             SVE_FM_3A           01000101xx0mmmmm 010100nnnnnddddd     4500 5000   

INST1(ssubwt,            "ssubwt",                0,                       IF_SVE_FM_3A,            0x45005400                                   )
    // SSUBWT  <Zd>.<T>, <Zn>.<T>, <Zm>.<Tb>                                             SVE_FM_3A           01000101xx0mmmmm 010101nnnnnddddd     4500 5400   

INST1(uaddwb,            "uaddwb",                0,                       IF_SVE_FM_3A,            0x45004800                                   )
    // UADDWB  <Zd>.<T>, <Zn>.<T>, <Zm>.<Tb>                                             SVE_FM_3A           01000101xx0mmmmm 010010nnnnnddddd     4500 4800   

INST1(uaddwt,            "uaddwt",                0,                       IF_SVE_FM_3A,            0x45004C00                                   )
    // UADDWT  <Zd>.<T>, <Zn>.<T>, <Zm>.<Tb>                                             SVE_FM_3A           01000101xx0mmmmm 010011nnnnnddddd     4500 4C00   

INST1(usubwb,            "usubwb",                0,                       IF_SVE_FM_3A,            0x45005800                                   )
    // USUBWB  <Zd>.<T>, <Zn>.<T>, <Zm>.<Tb>                                             SVE_FM_3A           01000101xx0mmmmm 010110nnnnnddddd     4500 5800   

INST1(usubwt,            "usubwt",                0,                       IF_SVE_FM_3A,            0x45005C00                                   )
    // USUBWT  <Zd>.<T>, <Zn>.<T>, <Zm>.<Tb>                                             SVE_FM_3A           01000101xx0mmmmm 010111nnnnnddddd     4500 5C00   


//    enum               name                     info                                              SVE_FP_3A                                    
INST1(eorbt,             "eorbt",                 0,                       IF_SVE_FP_3A,            0x45009000                                   )
    // EORBT   <Zd>.<T>, <Zn>.<T>, <Zm>.<T>                                              SVE_FP_3A           01000101xx0mmmmm 100100nnnnnddddd     4500 9000   

INST1(eortb,             "eortb",                 0,                       IF_SVE_FP_3A,            0x45009400                                   )
    // EORTB   <Zd>.<T>, <Zn>.<T>, <Zm>.<T>                                              SVE_FP_3A           01000101xx0mmmmm 100101nnnnnddddd     4500 9400   


//    enum               name                     info                                              SVE_FQ_3A                                    
INST1(bdep,              "bdep",                  0,                       IF_SVE_FQ_3A,            0x4500B400                                   )
    // BDEP    <Zd>.<T>, <Zn>.<T>, <Zm>.<T>                                              SVE_FQ_3A           01000101xx0mmmmm 101101nnnnnddddd     4500 B400   

INST1(bext,              "bext",                  0,                       IF_SVE_FQ_3A,            0x4500B000                                   )
    // BEXT    <Zd>.<T>, <Zn>.<T>, <Zm>.<T>                                              SVE_FQ_3A           01000101xx0mmmmm 101100nnnnnddddd     4500 B000   

INST1(bgrp,              "bgrp",                  0,                       IF_SVE_FQ_3A,            0x4500B800                                   )
    // BGRP    <Zd>.<T>, <Zn>.<T>, <Zm>.<T>                                              SVE_FQ_3A           01000101xx0mmmmm 101110nnnnnddddd     4500 B800   


//    enum               name                     info                                              SVE_FR_2A                                    
INST1(sshllb,            "sshllb",                0,                       IF_SVE_FR_2A,            0x4500A000                                   )
    // SSHLLB  <Zd>.<T>, <Zn>.<Tb>, #<const>                                             SVE_FR_2A           010001010x0xxiii 101000nnnnnddddd     4500 A000   

INST1(sshllt,            "sshllt",                0,                       IF_SVE_FR_2A,            0x4500A400                                   )
    // SSHLLT  <Zd>.<T>, <Zn>.<Tb>, #<const>                                             SVE_FR_2A           010001010x0xxiii 101001nnnnnddddd     4500 A400   

INST1(ushllb,            "ushllb",                0,                       IF_SVE_FR_2A,            0x4500A800                                   )
    // USHLLB  <Zd>.<T>, <Zn>.<Tb>, #<const>                                             SVE_FR_2A           010001010x0xxiii 101010nnnnnddddd     4500 A800   

INST1(ushllt,            "ushllt",                0,                       IF_SVE_FR_2A,            0x4500AC00                                   )
    // USHLLT  <Zd>.<T>, <Zn>.<Tb>, #<const>                                             SVE_FR_2A           010001010x0xxiii 101011nnnnnddddd     4500 AC00   


//    enum               name                     info                                              SVE_FS_3A                                    
INST1(saddlbt,           "saddlbt",               0,                       IF_SVE_FS_3A,            0x45008000                                   )
    // SADDLBT <Zd>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                            SVE_FS_3A           01000101xx0mmmmm 100000nnnnnddddd     4500 8000   

INST1(ssublbt,           "ssublbt",               0,                       IF_SVE_FS_3A,            0x45008800                                   )
    // SSUBLBT <Zd>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                            SVE_FS_3A           01000101xx0mmmmm 100010nnnnnddddd     4500 8800   

INST1(ssubltb,           "ssubltb",               0,                       IF_SVE_FS_3A,            0x45008C00                                   )
    // SSUBLTB <Zd>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                            SVE_FS_3A           01000101xx0mmmmm 100011nnnnnddddd     4500 8C00   


//    enum               name                     info                                              SVE_FV_2A                                    
INST1(cadd,              "cadd",                  0,                       IF_SVE_FV_2A,            0x4500D800                                   )
    // CADD    <Zdn>.<T>, <Zdn>.<T>, <Zm>.<T>, <const>                                   SVE_FV_2A           01000101xx000000 11011rmmmmmddddd     4500 D800   

INST1(sqcadd,            "sqcadd",                0,                       IF_SVE_FV_2A,            0x4501D800                                   )
    // SQCADD  <Zdn>.<T>, <Zdn>.<T>, <Zm>.<T>, <const>                                   SVE_FV_2A           01000101xx000001 11011rmmmmmddddd     4501 D800   


//    enum               name                     info                                              SVE_FX_3A                                    
INST1(sabalb,            "sabalb",                0,                       IF_SVE_FX_3A,            0x4500C000                                   )
    // SABALB  <Zda>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                           SVE_FX_3A           01000101xx0mmmmm 110000nnnnnddddd     4500 C000   

INST1(sabalt,            "sabalt",                0,                       IF_SVE_FX_3A,            0x4500C400                                   )
    // SABALT  <Zda>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                           SVE_FX_3A           01000101xx0mmmmm 110001nnnnnddddd     4500 C400   

INST1(uabalb,            "uabalb",                0,                       IF_SVE_FX_3A,            0x4500C800                                   )
    // UABALB  <Zda>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                           SVE_FX_3A           01000101xx0mmmmm 110010nnnnnddddd     4500 C800   

INST1(uabalt,            "uabalt",                0,                       IF_SVE_FX_3A,            0x4500CC00                                   )
    // UABALT  <Zda>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                           SVE_FX_3A           01000101xx0mmmmm 110011nnnnnddddd     4500 CC00   


//    enum               name                     info                                              SVE_FY_3A                                    
INST1(adclb,             "adclb",                 0,                       IF_SVE_FY_3A,            0x4500D000                                   )
    // ADCLB   <Zda>.<T>, <Zn>.<T>, <Zm>.<T>                                             SVE_FY_3A           010001010x0mmmmm 110100nnnnnddddd     4500 D000   

INST1(adclt,             "adclt",                 0,                       IF_SVE_FY_3A,            0x4500D400                                   )
    // ADCLT   <Zda>.<T>, <Zn>.<T>, <Zm>.<T>                                             SVE_FY_3A           010001010x0mmmmm 110101nnnnnddddd     4500 D400   

INST1(sbclb,             "sbclb",                 0,                       IF_SVE_FY_3A,            0x4580D000                                   )
    // SBCLB   <Zda>.<T>, <Zn>.<T>, <Zm>.<T>                                             SVE_FY_3A           010001011x0mmmmm 110100nnnnnddddd     4580 D000   

INST1(sbclt,             "sbclt",                 0,                       IF_SVE_FY_3A,            0x4580D400                                   )
    // SBCLT   <Zda>.<T>, <Zn>.<T>, <Zm>.<T>                                             SVE_FY_3A           010001011x0mmmmm 110101nnnnnddddd     4580 D400   


//    enum               name                     info                                              SVE_FZ_2A                                    
INST1(sqcvtn,            "sqcvtn",                0,                       IF_SVE_FZ_2A,            0x45314000                                   )
    // SQCVTN  <Zd>.H, {<Zn1>.S-<Zn2>.S }                                                SVE_FZ_2A           0100010100110001 010000nnnn0ddddd     4531 4000   

INST1(sqcvtun,           "sqcvtun",               0,                       IF_SVE_FZ_2A,            0x45315000                                   )
    // SQCVTUN <Zd>.H, {<Zn1>.S-<Zn2>.S }                                                SVE_FZ_2A           0100010100110001 010100nnnn0ddddd     4531 5000   

INST1(uqcvtn,            "uqcvtn",                0,                       IF_SVE_FZ_2A,            0x45314800                                   )
    // UQCVTN  <Zd>.H, {<Zn1>.S-<Zn2>.S }                                                SVE_FZ_2A           0100010100110001 010010nnnn0ddddd     4531 4800   


//    enum               name                     info                                              SVE_GB_2A                                    
INST1(rshrnb,            "rshrnb",                0,                       IF_SVE_GB_2A,            0x45201800                                   )
    // RSHRNB  <Zd>.<T>, <Zn>.<Tb>, #<const>                                             SVE_GB_2A           010001010x1xxiii 000110nnnnnddddd     4520 1800   

INST1(rshrnt,            "rshrnt",                0,                       IF_SVE_GB_2A,            0x45201C00                                   )
    // RSHRNT  <Zd>.<T>, <Zn>.<Tb>, #<const>                                             SVE_GB_2A           010001010x1xxiii 000111nnnnnddddd     4520 1C00   

INST1(shrnb,             "shrnb",                 0,                       IF_SVE_GB_2A,            0x45201000                                   )
    // SHRNB   <Zd>.<T>, <Zn>.<Tb>, #<const>                                             SVE_GB_2A           010001010x1xxiii 000100nnnnnddddd     4520 1000   

INST1(shrnt,             "shrnt",                 0,                       IF_SVE_GB_2A,            0x45201400                                   )
    // SHRNT   <Zd>.<T>, <Zn>.<Tb>, #<const>                                             SVE_GB_2A           010001010x1xxiii 000101nnnnnddddd     4520 1400   

INST1(sqrshrnb,          "sqrshrnb",              0,                       IF_SVE_GB_2A,            0x45202800                                   )
    // SQRSHRNB <Zd>.<T>, <Zn>.<Tb>, #<const>                                            SVE_GB_2A           010001010x1xxiii 001010nnnnnddddd     4520 2800   

INST1(sqrshrnt,          "sqrshrnt",              0,                       IF_SVE_GB_2A,            0x45202C00                                   )
    // SQRSHRNT <Zd>.<T>, <Zn>.<Tb>, #<const>                                            SVE_GB_2A           010001010x1xxiii 001011nnnnnddddd     4520 2C00   

INST1(sqrshrunb,         "sqrshrunb",             0,                       IF_SVE_GB_2A,            0x45200800                                   )
    // SQRSHRUNB <Zd>.<T>, <Zn>.<Tb>, #<const>                                           SVE_GB_2A           010001010x1xxiii 000010nnnnnddddd     4520 0800   

INST1(sqrshrunt,         "sqrshrunt",             0,                       IF_SVE_GB_2A,            0x45200C00                                   )
    // SQRSHRUNT <Zd>.<T>, <Zn>.<Tb>, #<const>                                           SVE_GB_2A           010001010x1xxiii 000011nnnnnddddd     4520 0C00   

INST1(sqshrnb,           "sqshrnb",               0,                       IF_SVE_GB_2A,            0x45202000                                   )
    // SQSHRNB <Zd>.<T>, <Zn>.<Tb>, #<const>                                             SVE_GB_2A           010001010x1xxiii 001000nnnnnddddd     4520 2000   

INST1(sqshrnt,           "sqshrnt",               0,                       IF_SVE_GB_2A,            0x45202400                                   )
    // SQSHRNT <Zd>.<T>, <Zn>.<Tb>, #<const>                                             SVE_GB_2A           010001010x1xxiii 001001nnnnnddddd     4520 2400   

INST1(sqshrunb,          "sqshrunb",              0,                       IF_SVE_GB_2A,            0x45200000                                   )
    // SQSHRUNB <Zd>.<T>, <Zn>.<Tb>, #<const>                                            SVE_GB_2A           010001010x1xxiii 000000nnnnnddddd     4520 0000   

INST1(sqshrunt,          "sqshrunt",              0,                       IF_SVE_GB_2A,            0x45200400                                   )
    // SQSHRUNT <Zd>.<T>, <Zn>.<Tb>, #<const>                                            SVE_GB_2A           010001010x1xxiii 000001nnnnnddddd     4520 0400   

INST1(uqrshrnb,          "uqrshrnb",              0,                       IF_SVE_GB_2A,            0x45203800                                   )
    // UQRSHRNB <Zd>.<T>, <Zn>.<Tb>, #<const>                                            SVE_GB_2A           010001010x1xxiii 001110nnnnnddddd     4520 3800   

INST1(uqrshrnt,          "uqrshrnt",              0,                       IF_SVE_GB_2A,            0x45203C00                                   )
    // UQRSHRNT <Zd>.<T>, <Zn>.<Tb>, #<const>                                            SVE_GB_2A           010001010x1xxiii 001111nnnnnddddd     4520 3C00   

INST1(uqshrnb,           "uqshrnb",               0,                       IF_SVE_GB_2A,            0x45203000                                   )
    // UQSHRNB <Zd>.<T>, <Zn>.<Tb>, #<const>                                             SVE_GB_2A           010001010x1xxiii 001100nnnnnddddd     4520 3000   

INST1(uqshrnt,           "uqshrnt",               0,                       IF_SVE_GB_2A,            0x45203400                                   )
    // UQSHRNT <Zd>.<T>, <Zn>.<Tb>, #<const>                                             SVE_GB_2A           010001010x1xxiii 001101nnnnnddddd     4520 3400   


//    enum               name                     info                                              SVE_GC_3A                                    
INST1(addhnb,            "addhnb",                0,                       IF_SVE_GC_3A,            0x45206000                                   )
    // ADDHNB  <Zd>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                            SVE_GC_3A           01000101xx1mmmmm 011000nnnnnddddd     4520 6000   

INST1(addhnt,            "addhnt",                0,                       IF_SVE_GC_3A,            0x45206400                                   )
    // ADDHNT  <Zd>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                            SVE_GC_3A           01000101xx1mmmmm 011001nnnnnddddd     4520 6400   

INST1(raddhnb,           "raddhnb",               0,                       IF_SVE_GC_3A,            0x45206800                                   )
    // RADDHNB <Zd>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                            SVE_GC_3A           01000101xx1mmmmm 011010nnnnnddddd     4520 6800   

INST1(raddhnt,           "raddhnt",               0,                       IF_SVE_GC_3A,            0x45206C00                                   )
    // RADDHNT <Zd>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                            SVE_GC_3A           01000101xx1mmmmm 011011nnnnnddddd     4520 6C00   

INST1(rsubhnb,           "rsubhnb",               0,                       IF_SVE_GC_3A,            0x45207800                                   )
    // RSUBHNB <Zd>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                            SVE_GC_3A           01000101xx1mmmmm 011110nnnnnddddd     4520 7800   

INST1(rsubhnt,           "rsubhnt",               0,                       IF_SVE_GC_3A,            0x45207C00                                   )
    // RSUBHNT <Zd>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                            SVE_GC_3A           01000101xx1mmmmm 011111nnnnnddddd     4520 7C00   

INST1(subhnb,            "subhnb",                0,                       IF_SVE_GC_3A,            0x45207000                                   )
    // SUBHNB  <Zd>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                            SVE_GC_3A           01000101xx1mmmmm 011100nnnnnddddd     4520 7000   

INST1(subhnt,            "subhnt",                0,                       IF_SVE_GC_3A,            0x45207400                                   )
    // SUBHNT  <Zd>.<T>, <Zn>.<Tb>, <Zm>.<Tb>                                            SVE_GC_3A           01000101xx1mmmmm 011101nnnnnddddd     4520 7400   


//    enum               name                     info                                              SVE_GD_2A                                    
INST1(sqxtnb,            "sqxtnb",                0,                       IF_SVE_GD_2A,            0x45204000                                   )
    // SQXTNB  <Zd>.<T>, <Zn>.<Tb>                                                       SVE_GD_2A           010001010x1xx000 010000nnnnnddddd     4520 4000   

INST1(sqxtnt,            "sqxtnt",                0,                       IF_SVE_GD_2A,            0x45204400                                   )
    // SQXTNT  <Zd>.<T>, <Zn>.<Tb>                                                       SVE_GD_2A           010001010x1xx000 010001nnnnnddddd     4520 4400   

INST1(sqxtunb,           "sqxtunb",               0,                       IF_SVE_GD_2A,            0x45205000                                   )
    // SQXTUNB <Zd>.<T>, <Zn>.<Tb>                                                       SVE_GD_2A           010001010x1xx000 010100nnnnnddddd     4520 5000   

INST1(sqxtunt,           "sqxtunt",               0,                       IF_SVE_GD_2A,            0x45205400                                   )
    // SQXTUNT <Zd>.<T>, <Zn>.<Tb>                                                       SVE_GD_2A           010001010x1xx000 010101nnnnnddddd     4520 5400   

INST1(uqxtnb,            "uqxtnb",                0,                       IF_SVE_GD_2A,            0x45204800                                   )
    // UQXTNB  <Zd>.<T>, <Zn>.<Tb>                                                       SVE_GD_2A           010001010x1xx000 010010nnnnnddddd     4520 4800   

INST1(uqxtnt,            "uqxtnt",                0,                       IF_SVE_GD_2A,            0x45204C00                                   )
    // UQXTNT  <Zd>.<T>, <Zn>.<Tb>                                                       SVE_GD_2A           010001010x1xx000 010011nnnnnddddd     4520 4C00   


//    enum               name                     info                                              SVE_GE_4A                                    
INST1(match,             "match",                 0,                       IF_SVE_GE_4A,            0x45208000                                   )
    // MATCH   <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.<T>                                      SVE_GE_4A           01000101xx1mmmmm 100gggnnnnn0DDDD     4520 8000   

INST1(nmatch,            "nmatch",                0,                       IF_SVE_GE_4A,            0x45208010                                   )
    // NMATCH  <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.<T>                                      SVE_GE_4A           01000101xx1mmmmm 100gggnnnnn1DDDD     4520 8010   


//    enum               name                     info                                              SVE_GF_3A                                    
INST1(histseg,           "histseg",               0,                       IF_SVE_GF_3A,            0x4520A000                                   )
    // HISTSEG <Zd>.B, <Zn>.B, <Zm>.B                                                    SVE_GF_3A           01000101xx1mmmmm 101000nnnnnddddd     4520 A000   


//    enum               name                     info                                              SVE_GI_4A                                    
INST1(histcnt,           "histcnt",               0,                       IF_SVE_GI_4A,            0x4520C000                                   )
    // HISTCNT <Zd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.<T>                                      SVE_GI_4A           01000101xx1mmmmm 110gggnnnnnddddd     4520 C000   


//    enum               name                     info                                              SVE_GQ_3A                                    
INST1(bfcvtnt,           "bfcvtnt",               0,                       IF_SVE_GQ_3A,            0x648AA000                                   )
    // BFCVTNT <Zd>.H, <Pg>/M, <Zn>.S                                                    SVE_GQ_3A           0110010010001010 101gggnnnnnddddd     648A A000   

INST1(fcvtlt,            "fcvtlt",                0,                       IF_SVE_GQ_3A,            0x64CBA000                                   )
    // FCVTLT  <Zd>.D, <Pg>/M, <Zn>.S                                                    SVE_GQ_3A           0110010011001011 101gggnnnnnddddd     64CB A000   

INST1(fcvtxnt,           "fcvtxnt",               0,                       IF_SVE_GQ_3A,            0x640AA000                                   )
    // FCVTXNT <Zd>.S, <Pg>/M, <Zn>.D                                                    SVE_GQ_3A           0110010000001010 101gggnnnnnddddd     640A A000   


//    enum               name                     info                                              SVE_GS_3A                                    
INST1(faddqv,            "faddqv",                0,                       IF_SVE_GS_3A,            0x6410A000                                   )
    // FADDQV  <Vd>.<T>, <Pg>, <Zn>.<Tb>                                                 SVE_GS_3A           01100100xx010000 101gggnnnnnddddd     6410 A000   

INST1(fmaxnmqv,          "fmaxnmqv",              0,                       IF_SVE_GS_3A,            0x6414A000                                   )
    // FMAXNMQV <Vd>.<T>, <Pg>, <Zn>.<Tb>                                                SVE_GS_3A           01100100xx010100 101gggnnnnnddddd     6414 A000   

INST1(fmaxqv,            "fmaxqv",                0,                       IF_SVE_GS_3A,            0x6416A000                                   )
    // FMAXQV  <Vd>.<T>, <Pg>, <Zn>.<Tb>                                                 SVE_GS_3A           01100100xx010110 101gggnnnnnddddd     6416 A000   

INST1(fminnmqv,          "fminnmqv",              0,                       IF_SVE_GS_3A,            0x6415A000                                   )
    // FMINNMQV <Vd>.<T>, <Pg>, <Zn>.<Tb>                                                SVE_GS_3A           01100100xx010101 101gggnnnnnddddd     6415 A000   

INST1(fminqv,            "fminqv",                0,                       IF_SVE_GS_3A,            0x6417A000                                   )
    // FMINQV  <Vd>.<T>, <Pg>, <Zn>.<Tb>                                                 SVE_GS_3A           01100100xx010111 101gggnnnnnddddd     6417 A000   


//    enum               name                     info                                              SVE_GW_3A                                    
INST1(fclamp,            "fclamp",                0,                       IF_SVE_GW_3A,            0x64202400                                   )
    // FCLAMP  <Zd>.<T>, <Zn>.<T>, <Zm>.<T>                                              SVE_GW_3A           01100100xx1mmmmm 001001nnnnnddddd     6420 2400   


//    enum               name                     info                                              SVE_GW_3B                                    
INST1(bfclamp,           "bfclamp",               0,                       IF_SVE_GW_3B,            0x64202400                                   )
    // BFCLAMP <Zd>.H, <Zn>.H, <Zm>.H                                                    SVE_GW_3B           01100100001mmmmm 001001nnnnnddddd     6420 2400   


//    enum               name                     info                                              SVE_HD_3A_A                                  
INST1(fmmla,             "fmmla",                 0,                       IF_SVE_HD_3A_A,          0x64A0E400                                   )
    // FMMLA   <Zda>.D, <Zn>.D, <Zm>.D                                                   SVE_HD_3A_A         01100100101mmmmm 111001nnnnnddddd     64A0 E400   


//    enum               name                     info                                              SVE_HH_2A                                    
INST1(bf1cvt,            "bf1cvt",                0,                       IF_SVE_HH_2A,            0x65083800                                   )
    // BF1CVT  <Zd>.H, <Zn>.B                                                            SVE_HH_2A           0110010100001000 001110nnnnnddddd     6508 3800   

INST1(bf1cvtlt,          "bf1cvtlt",              0,                       IF_SVE_HH_2A,            0x65093800                                   )
    // BF1CVTLT <Zd>.H, <Zn>.B                                                           SVE_HH_2A           0110010100001001 001110nnnnnddddd     6509 3800   

INST1(bf2cvt,            "bf2cvt",                0,                       IF_SVE_HH_2A,            0x65083C00                                   )
    // BF2CVT  <Zd>.H, <Zn>.B                                                            SVE_HH_2A           0110010100001000 001111nnnnnddddd     6508 3C00   

INST1(bf2cvtlt,          "bf2cvtlt",              0,                       IF_SVE_HH_2A,            0x65093C00                                   )
    // BF2CVTLT <Zd>.H, <Zn>.B                                                           SVE_HH_2A           0110010100001001 001111nnnnnddddd     6509 3C00   

INST1(f1cvt,             "f1cvt",                 0,                       IF_SVE_HH_2A,            0x65083000                                   )
    // F1CVT   <Zd>.H, <Zn>.B                                                            SVE_HH_2A           0110010100001000 001100nnnnnddddd     6508 3000   

INST1(f1cvtlt,           "f1cvtlt",               0,                       IF_SVE_HH_2A,            0x65093000                                   )
    // F1CVTLT <Zd>.H, <Zn>.B                                                            SVE_HH_2A           0110010100001001 001100nnnnnddddd     6509 3000   

INST1(f2cvt,             "f2cvt",                 0,                       IF_SVE_HH_2A,            0x65083400                                   )
    // F2CVT   <Zd>.H, <Zn>.B                                                            SVE_HH_2A           0110010100001000 001101nnnnnddddd     6508 3400   

INST1(f2cvtlt,           "f2cvtlt",               0,                       IF_SVE_HH_2A,            0x65093400                                   )
    // F2CVTLT <Zd>.H, <Zn>.B                                                            SVE_HH_2A           0110010100001001 001101nnnnnddddd     6509 3400   


//    enum               name                     info                                              SVE_HJ_3A                                    
INST1(fadda,             "fadda",                 0,                       IF_SVE_HJ_3A,            0x65182000                                   )
    // FADDA   <V><dn>, <Pg>, <V><dn>, <Zm>.<T>                                          SVE_HJ_3A           01100101xx011000 001gggmmmmmddddd     6518 2000   


//    enum               name                     info                                              SVE_HL_3B                                    
INST1(bfmax,             "bfmax",                 0,                       IF_SVE_HL_3B,            0x65068000                                   )
    // BFMAX   <Zdn>.H, <Pg>/M, <Zdn>.H, <Zm>.H                                          SVE_HL_3B           0110010100000110 100gggmmmmmddddd     6506 8000   

INST1(bfmaxnm,           "bfmaxnm",               0,                       IF_SVE_HL_3B,            0x65048000                                   )
    // BFMAXNM <Zdn>.H, <Pg>/M, <Zdn>.H, <Zm>.H                                          SVE_HL_3B           0110010100000100 100gggmmmmmddddd     6504 8000   

INST1(bfmin,             "bfmin",                 0,                       IF_SVE_HL_3B,            0x65078000                                   )
    // BFMIN   <Zdn>.H, <Pg>/M, <Zdn>.H, <Zm>.H                                          SVE_HL_3B           0110010100000111 100gggmmmmmddddd     6507 8000   

INST1(bfminnm,           "bfminnm",               0,                       IF_SVE_HL_3B,            0x65058000                                   )
    // BFMINNM <Zdn>.H, <Pg>/M, <Zdn>.H, <Zm>.H                                          SVE_HL_3B           0110010100000101 100gggmmmmmddddd     6505 8000   


//    enum               name                     info                                              SVE_HN_2A                                    
INST1(ftmad,             "ftmad",                 0,                       IF_SVE_HN_2A,            0x65108000                                   )
    // FTMAD   <Zdn>.<T>, <Zdn>.<T>, <Zm>.<T>, #<imm>                                    SVE_HN_2A           01100101xx010iii 100000mmmmmddddd     6510 8000   


//    enum               name                     info                                              SVE_HP_3A                                    
INST1(flogb,             "flogb",                 0,                       IF_SVE_HP_3A,            0x6518A000                                   )
    // FLOGB   <Zd>.<T>, <Pg>/M, <Zn>.<T>                                                SVE_HP_3A           0110010100011xx0 101gggnnnnnddddd     6518 A000   


//    enum               name                     info                                              SVE_HU_4A                                    
INST1(fnmla,             "fnmla",                 0,                       IF_SVE_HU_4A,            0x65204000                                   )
    // FNMLA   <Zda>.<T>, <Pg>/M, <Zn>.<T>, <Zm>.<T>                                     SVE_HU_4A           01100101xx1mmmmm 010gggnnnnnddddd     6520 4000   

INST1(fnmls,             "fnmls",                 0,                       IF_SVE_HU_4A,            0x65206000                                   )
    // FNMLS   <Zda>.<T>, <Pg>/M, <Zn>.<T>, <Zm>.<T>                                     SVE_HU_4A           01100101xx1mmmmm 011gggnnnnnddddd     6520 6000   


//    enum               name                     info                                              SVE_HV_4A                                    
INST1(fmad,              "fmad",                  0,                       IF_SVE_HV_4A,            0x65208000                                   )
    // FMAD    <Zdn>.<T>, <Pg>/M, <Zm>.<T>, <Za>.<T>                                     SVE_HV_4A           01100101xx1aaaaa 100gggmmmmmddddd     6520 8000   

INST1(fmsb,              "fmsb",                  0,                       IF_SVE_HV_4A,            0x6520A000                                   )
    // FMSB    <Zdn>.<T>, <Pg>/M, <Zm>.<T>, <Za>.<T>                                     SVE_HV_4A           01100101xx1aaaaa 101gggmmmmmddddd     6520 A000   

INST1(fnmad,             "fnmad",                 0,                       IF_SVE_HV_4A,            0x6520C000                                   )
    // FNMAD   <Zdn>.<T>, <Pg>/M, <Zm>.<T>, <Za>.<T>                                     SVE_HV_4A           01100101xx1aaaaa 110gggmmmmmddddd     6520 C000   

INST1(fnmsb,             "fnmsb",                 0,                       IF_SVE_HV_4A,            0x6520E000                                   )
    // FNMSB   <Zdn>.<T>, <Pg>/M, <Zm>.<T>, <Za>.<T>                                     SVE_HV_4A           01100101xx1aaaaa 111gggmmmmmddddd     6520 E000   


//    enum               name                     info                                              SVE_IC_3A_C                                  
INST1(ld1rb,             "ld1rb",                 0,                       IF_SVE_IC_3A_C,          0x84408000                                   )
    // LD1RB   {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>}]                                    SVE_IC_3A_C         1000010001iiiiii 100gggnnnnnttttt     8440 8000   


//    enum               name                     info                                              SVE_IC_3A                                    
INST1(ld1rd,             "ld1rd",                 0,                       IF_SVE_IC_3A,            0x85C0E000                                   )
    // LD1RD   {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>}]                                    SVE_IC_3A           1000010111iiiiii 111gggnnnnnttttt     85C0 E000   

INST1(ld1rsw,            "ld1rsw",                0,                       IF_SVE_IC_3A,            0x84C08000                                   )
    // LD1RSW  {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>}]                                    SVE_IC_3A           1000010011iiiiii 100gggnnnnnttttt     84C0 8000   


//    enum               name                     info                                              SVE_IC_3A_B                                  
INST1(ld1rh,             "ld1rh",                 0,                       IF_SVE_IC_3A_B,          0x84C08000                                   )
    // LD1RH   {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>}]                                    SVE_IC_3A_B         1000010011iiiiii 100gggnnnnnttttt     84C0 8000   

INST1(ld1rsb,            "ld1rsb",                0,                       IF_SVE_IC_3A_B,          0x85C08000                                   )
    // LD1RSB  {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>}]                                    SVE_IC_3A_B         1000010111iiiiii 100gggnnnnnttttt     85C0 8000   


//    enum               name                     info                                              SVE_IC_3A_A                                  
INST1(ld1rsh,            "ld1rsh",                0,                       IF_SVE_IC_3A_A,          0x85408000                                   )
    // LD1RSH  {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>}]                                    SVE_IC_3A_A         1000010101iiiiii 100gggnnnnnttttt     8540 8000   

INST1(ld1rw,             "ld1rw",                 0,                       IF_SVE_IC_3A_A,          0x8540C000                                   )
    // LD1RW   {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>}]                                    SVE_IC_3A_A         1000010101iiiiii 110gggnnnnnttttt     8540 C000   


//    enum               name                     info                                              SVE_IL_3A_C                                  
INST1(ldnf1b,            "ldnf1b",                0,                       IF_SVE_IL_3A_C,          0xA410A000                                   )
    // LDNF1B  {<Zt>.B }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]                            SVE_IL_3A_C         101001000001iiii 101gggnnnnnttttt     A410 A000   


//    enum               name                     info                                              SVE_IL_3A                                    
INST1(ldnf1d,            "ldnf1d",                0,                       IF_SVE_IL_3A,            0xA5F0A000                                   )
    // LDNF1D  {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]                            SVE_IL_3A           101001011111iiii 101gggnnnnnttttt     A5F0 A000   

INST1(ldnf1sw,           "ldnf1sw",               0,                       IF_SVE_IL_3A,            0xA490A000                                   )
    // LDNF1SW {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]                            SVE_IL_3A           101001001001iiii 101gggnnnnnttttt     A490 A000   


//    enum               name                     info                                              SVE_IL_3A_B                                  
INST1(ldnf1h,            "ldnf1h",                0,                       IF_SVE_IL_3A_B,          0xA490A000                                   )
    // LDNF1H  {<Zt>.X }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]                            SVE_IL_3A_B         101001001001iiii 101gggnnnnnttttt     A490 A000   

INST1(ldnf1sb,           "ldnf1sb",               0,                       IF_SVE_IL_3A_B,          0xA590A000                                   )
    // LDNF1SB {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]                            SVE_IL_3A_B         101001011001iiii 101gggnnnnnttttt     A590 A000   


//    enum               name                     info                                              SVE_IL_3A_A                                  
INST1(ldnf1sh,           "ldnf1sh",               0,                       IF_SVE_IL_3A_A,          0xA510A000                                   )
    // LDNF1SH {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]                            SVE_IL_3A_A         101001010001iiii 101gggnnnnnttttt     A510 A000   

INST1(ldnf1w,            "ldnf1w",                0,                       IF_SVE_IL_3A_A,          0xA550A000                                   )
    // LDNF1W  {<Zt>.S }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]                            SVE_IL_3A_A         101001010101iiii 101gggnnnnnttttt     A550 A000   


//    enum               name                     info                                              SVE_IW_4A                                    
INST1(ld1q,              "ld1q",                  0,                       IF_SVE_IW_4A,            0xC400A000                                   )
    // LD1Q    {<Zt>.Q }, <Pg>/Z, [<Zn>.D{, <Xm>}]                                       SVE_IW_4A           11000100000mmmmm 101gggnnnnnttttt     C400 A000   


//    enum               name                     info                                              SVE_IX_4A                                    
INST1(ldnt1sw,           "ldnt1sw",               0,                       IF_SVE_IX_4A,            0xC5008000                                   )
    // LDNT1SW {<Zt>.D }, <Pg>/Z, [<Zn>.D{, <Xm>}]                                       SVE_IX_4A           11000101000mmmmm 100gggnnnnnttttt     C500 8000   


//    enum               name                     info                                              SVE_IY_4A                                    
INST1(st1q,              "st1q",                  0,                       IF_SVE_IY_4A,            0xE4202000                                   )
    // ST1Q    {<Zt>.Q }, <Pg>, [<Zn>.D{, <Xm>}]                                         SVE_IY_4A           11100100001mmmmm 001gggnnnnnttttt     E420 2000

// clang-format on

/*****************************************************************************/
#undef INST1
#undef INST2
#undef INST3
#undef INST4
#undef INST5
#undef INST6
#undef INST7
#undef INST8
#undef INST9
#undef INST11
#undef INST13
