// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************
 *  Arm Thumb1/Thumb2 instructions for JIT compiler
 *
 *          id      -- the enum name for the instruction
 *          nm      -- textual name (for assembly dipslay)
 *          fp      -- floating point instruction
 *          ld/st/cmp   -- load/store/compare instruction
 *          fmt     -- encoding format used by this instruction
 *          e1      -- encoding 1
 *          e2      -- encoding 2
 *          e3      -- encoding 3
 *          e4      -- encoding 4
 *          e5      -- encoding 5
 *          e6      -- encoding 6
 *          e7      -- encoding 7
 *          e8      -- encoding 8
 *          e9      -- encoding 9
 *
******************************************************************************/

#if !defined(TARGET_ARM)
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
// No INST7
// #ifndef INST7
// #error  INST7 must be defined before including this file.
// #endif
#ifndef INST8
#error INST8 must be defined before including this file.
#endif
#ifndef INST9
#error INST9 must be defined before including this file.
#endif

/*****************************************************************************/
/*               The following is ARM-specific                               */
/*****************************************************************************/

// If you're adding a new instruction:
// You need not only to fill in one of these macros describing the instruction, but also:
//   * If the instruction writes to more than one destination register, update the function
//     emitInsMayWriteMultipleRegs in emitArm.cpp.

// clang-format off
INST9(invalid, "INVALID", 0, 0, IF_NONE,   BAD_CODE,  BAD_CODE,    BAD_CODE,     BAD_CODE,   BAD_CODE,     BAD_CODE,      BAD_CODE, BAD_CODE,   BAD_CODE)

//    enum     name      FP LD/ST         Rdn,Rm     Rd,Rn,Rm     Rdn,i8        Rd,Rn,i3    Rd,Rn,+i8<<i4 Rd,Rn,Rm{,sh}  SP,i9     Rd,SP,i10   Rd,PC,i10
//                                          T1_D0     T1_H         T1_J0         T1_G        T2_L0         T2_C0          T1_F      T1_J2       T1_J3
INST9(add,     "add",    0, 0, IF_EN9,    0x4400,    0x1800,      0x3000,       0x1C00,     0xF1000000,   0xEB000000,    0xB000,   0xA800,     0xA000)
                                   //  add     Rdn,Rm            T1_D0     01000100Dmmmmddd                    4400        high
                                   //  adds    Rd,Rn,Rm          T1_H      0001100mmmnnnddd                    1800        low
                                   //  adds    Rdn,i8            T1_J0     00110dddiiiiiiii                    3000        low     imm(0-255)
                                   //  adds    Rd,Rn,i3          T1_G      0001110iiinnnddd                    1C00        low     imm(0-7)
                                   //  add{s}  Rd,Rn,Rm{,sh}     T2_C0     11101011000Snnnn 0iiiddddiishmmmm   EB00 0000
                                   //  add{s}  Rd,Rn,+i8<<i4     T2_L0     11110i01000Snnnn 0iiiddddiiiiiiii   F100 0000           imm(i8<<i4) *pref
                                   //  add     SP,i9             T1_F      101100000iiiiiii                    B000        SP      imm(0-508)
                                   //  add     Rd,SP,i10         T1_J2     10101dddiiiiiiii                    A800        low     imm(0-1020)
                                   //  add     Rd,PC,i10         T1_J3     10100dddiiiiiiii                    A000        low     imm(0-1020)
INST9(sub,     "sub",    0, 0, IF_EN9,    BAD_CODE,  0x1A00,      0x3800,       0x1E00,     0xF1A00000,   0xEBA00000,    0xB080,   BAD_CODE,   BAD_CODE)
                                   //  subs    Rd,Rn,Rm          T1_H      0001101mmmnnnddd                    1A00        low
                                   //  subs    Rdn,i8            T1_J0     00111dddiiiiiiii                    3800        low     imm(0-255)
                                   //  subs    Rd,Rn,i3          T1_G      0001111iiinnnddd                    1E00        low     imm(0-7)
                                   //  sub{s}  Rd,Rn,+i8<<i4     T2_L0     11110i01101Snnnn 0iiiddddiiiiiiii   F1A0 0000           imm(i8<<i4) *pref
                                   //  sub{s}  Rd,Rn,Rm{,sh}     T2_C0     11101011101Snnnn 0iiiddddiishmmmm   EBA0 0000
                                   //  sub     SP,i9             T1_F      101100001iiiiiii                    B080        SP     imm(0-508)   <

//    enum     name      FP LD/ST         Rt,[Rn+Rm] Rt,[Rn+i7]   Rt,[Rn+Rm,sh] Rt,[Rn+=i8] Rt,[Rn+i12]   Rt,[PC+-i12]   Rd,[SP+i10] Rd,[PC+i10]
//                                         T1_H       T1_C         T2_E0         T2_H0       T2_K1         T2_K4          T1_J2        T1_J3
INST8(ldr,     "ldr",    0,LD, IF_EN8,    0x5800,    0x6800,      0xF8500000,   0xF8500800, 0xF8D00000,   0xF85F0000,    0x9800,     0x4800)
                                   //  ldr     Rt,[Rn+Rm]        T1_H      0101100mmmnnnttt                    5800        low
                                   //  ldr     Rt,[Rn+i7]        T1_C      01101iiiiinnnttt                    6800        low     imm(0-124)
                                   //  ldr     Rt,[Rn+Rm{,sh}]   T2_E0     111110000101nnnn tttt000000shmmmm   F850 0000           sh=(0,1,2,3)
                                   //  ldr     Rt,[Rn],+-i8{!}   T2_H0     111110000101nnnn tttt1PUWiiiiiiii   F850 0800           imm(0-255)
                                   //  ldr     Rt,[Rn+i12]       T2_K1     111110001101nnnn ttttiiiiiiiiiiii   F8D0 0000           imm(0-4095)
                                   //  ldr     Rt,[PC+-i12]      T2_K4     11111000U1011111 ttttiiiiiiiiiiii   F85F 0000           imm(+-4095)
                                   //  ldr     Rt,[SP+i10]       T1_J2     10011tttiiiiiiii                    9800        low     imm(0-1020)
                                   //  ldr     Rt,[PC+i10]       T1_J3     01001tttiiiiiiii                    4800        low     imm(0-1020)

//    enum     name      FP LD/ST           Rt,[Rn+Rm] Rt,[Rn+i7]   Rt,[Rn+Rm,sh] Rt,[Rn+=i8] Rt,[Rn+i12]   Rt,[PC+-i12] or Rt,[SP+-i10]
//                                           T1_H       T1_C         T2_E0         T2_H0       T2_K1         T2_K4       or  T1_J2
INST6(str,     "str",    0,ST, IF_EN6B,   0x5000,    0x6000,      0xF8400000,   0xF8400800, 0xF8C00000,   0x9000)
                                   //  str     Rt,[Rn+Rm]        T1_H      0101000mmmnnnttt                    5000        low
                                   //  str     Rt,[Rn+i7]        T1_C      01100iiiiinnnttt                    6000        low     imm(0-124)
                                   //  str     Rt,[Rn+Rm{,sh}]   T2_E0     111110000100nnnn tttt000000shmmmm   F840 0000           sh=(0,1,2,3)
                                   //  str     Rt,[Rn],+-i8{!}   T2_H0     111110000100nnnn tttt1PUWiiiiiiii   F840 0800           imm(0-255)
                                   //  str     Rt,[Rn+i12]       T2_K1     111110001100nnnn ttttiiiiiiiiiiii   F8C0 0000           imm(0-4095)
                                   //  str     Rt,[SP+-i10]      T1_J2     10010tttiiiiiiii                    9000        low     imm(0-1020)
INST6(ldrb,    "ldrb",   0,LD, IF_EN6A,   0x5C00,    0x7800,      0xF8100000,   0xF8100800, 0xF8900000,   0xF81F0000)
                                   //  ldrb    Rt,[Rn+Rm]        T1_H      0101110mmmnnnttt                    5C00        low
                                   //  ldrb    Rt,[Rn+i5]        T1_C      01111iiiiinnnttt                    7800        low     imm(0-31)
                                   //  ldrb    Rt,[Rn+Rm{,sh}]   T2_E0     111110000001nnnn tttt000000shmmmm   F810 0000           sh=(0,1,2,3)
                                   //  ldrb    Rt,[Rn],+-i8{!}   T2_H0     111110000001nnnn tttt1PUWiiiiiiii   F810 0800           imm(0-255)
                                   //  ldrb    Rt,[Rn+i12]       T2_K1     111110001001nnnn ttttiiiiiiiiiiii   F890 0000           imm(0-4095)
                                   //  ldrb    Rt,[PC+i12]       T2_K4     11111000U0011111 ttttiiiiiiiiiiii   F81F 0000           imm(+-4095)
INST6(strb,    "strb",   0,ST, IF_EN6B,   0x5400,    0x7000,      0xF8000000,   0xF8000800, 0xF8800000,   BAD_CODE)
                                   //  strb    Rt,[Rn+Rm]        T1_H      0101010mmmnnnttt                    5400        low
                                   //  strb    Rt,[Rn+i5]        T1_C      01110iiiiinnnttt                    7000        low     imm(0-31)
                                   //  strb    Rt,[Rn+Rm{,sh}]   T2_E0     111110000000nnnn tttt000000shmmmm   F800 0000           sh=(0,1,2,3)
                                   //  strb    Rt,[Rn],+-i8{!}   T2_H0     111110000000nnnn tttt1PUWiiiiiiii   F800 0800           imm(0-255)
                                   //  strb    Rt,[Rn+i12]       T2_K1     111110001000nnnn ttttiiiiiiiiiiii   F880 0000           imm(0-4095)
INST6(ldrh,    "ldrh",   0,LD, IF_EN6A,   0x5A00,    0x8800,      0xF8300000,   0xF8300800, 0xF8B00000,   0xF83F0000)
                                   //  ldrh    Rt,[Rn+Rm]        T1_H      0101101mmmnnnttt                    5A00        low
                                   //  ldrh    Rt,[Rn+i6]        T1_C      10001iiiiinnnttt                    8800        low     imm(0-62)
                                   //  ldrh    Rt,[Rn+Rm{,sh}]   T2_E0     111110000011nnnn tttt000000shmmmm   F830 0000           sh=(0,1,2,3)
                                   //  ldrh    Rt,[Rn],+-i8{!}   T2_H0     111110000011nnnn tttt1PUWiiiiiiii   F830 0800           imm(0-255)
                                   //  ldrh    Rt,[Rn+i12]       T2_K1     111110001011nnnn ttttiiiiiiiiiiii   F8B0 0000           imm(0-4095)
                                   //  ldrh    Rt,[PC+i12]       T2_K4     11111000U0111111 ttttiiiiiiiiiiii   F83F 0000           imm(+-4095)
INST6(strh,    "strh",   0,ST, IF_EN6B,   0x5200,    0x8000,      0xF8200000,   0xF8200800, 0xF8a00000,   BAD_CODE)
                                   //  strh    Rt,[Rn+Rm]        T1_H      0101001mmmnnnttt                    5200        low
                                   //  strh    Rt,[Rn+i6]        T1_C      10000iiiiinnnttt                    8000        low     imm(0-62)
                                   //  strh    Rt,[Rn+Rm{,sh}]   T2_E0     111110000010nnnn tttt000000shmmmm   F820 0000           sh=(0,1,2,3)
                                   //  strh    Rt,[Rn],+-i8{!}   T2_H0     111110000010nnnn tttt1PUWiiiiiiii   F820 0800           imm(0-255)
                                   //  strh    Rt,[Rn+i12]       T2_K1     111110001010nnnn ttttiiiiiiiiiiii   F8A0 0000           imm(0-4095)
INST6(ldrsb,   "ldrsb",  0,LD, IF_EN6A,   0x5600,    BAD_CODE,    0xF9100000,   0xF9100800, 0xF9900000,   0xF91F0000)
                                   //  ldrsb   Rt,[Rn+Rm]        T1_H      0101011mmmnnnttt                    5600        low
                                   //  ldrsb   Rt,[Rn+Rm{,sh}]   T2_E0     111110010001nnnn tttt000000shmmmm   F910 0000           sh=(0,1,2,3)
                                   //  ldrsb   Rt,[Rn],+-i8{!}   T2_H0     111110010001nnnn tttt1PUWiiiiiiii   F910 0800           imm(0-255)
                                   //  ldrsb   Rt,[Rn+i12]       T2_K1     111110011001nnnn ttttiiiiiiiiiiii   F990 0000           imm(0-4095)
                                   //  ldrsb   Rt,[PC+i12]       T2_K4     11111001U0011111 ttttiiiiiiiiiiii   F91F 0000           imm(+-4095)
INST6(ldrsh,   "ldrsh",  0,LD, IF_EN6A,   0x5E00,    BAD_CODE,    0xF9300000,   0xF9300800, 0xF9B00000,   0xF93F0000)
                                   //  ldrsh   Rt,[Rn+Rm]        T1_H      0101111mmmnnnttt                    5E00        low
                                   //  ldrsh   Rt,[Rn+Rm{,sh}]   T2_E0     111110010011nnnn tttt000000shmmmm   F930 0000           sh=(0,1,2,3)
                                   //  ldrsh   Rt,[Rn],+-i8{!}   T2_H0     111110010011nnnn tttt1PUWiiiiiiii   F930 0800           imm(0-255)
                                   //  ldrsh   Rt,[Rn+i12]       T2_K1     111110011011nnnn ttttiiiiiiiiiiii   F9B0 0000           imm(0-4095)
                                   //  ldrsh   Rt,[PC+i12]       T2_K4     11111001U0111111 ttttiiiiiiiiiiii   F93F 0000           imm(+-4095)

//    enum     name      FP LD/ST          Rd, Rm     Rd,Rm        Rd,i8       Rd,+i8<<i4   S / Rn,Rm{,sh}
//                                          T1_E       T1_D0        T1_J0       T2_L1/L2     T2_C3/C8
INST5(mov,     "mov",    0, 0, IF_EN5A,   0x0000,    0x4600,      0x2000,      0xF04F0000,  0xEA5F0000)
                                   //  movs    Rd,Rm             T1_E      0000000000mmmddd                    0000        low
                                   //  mov     Rd,Rm             T1_D0     01000110Dmmmmddd                    4600        high
                                   //  movs    Rd,i8             T1_J0     00100dddiiiiiiii                    2000        low     imm(0-255)
                                   //  mov{s}  Rd,+i8<<i4        T2_L1     11110i00010S1111 0iiiddddiiiiiiii   F04F 0000           imm(i8<<i4)
                                   //  mov{s}  Rd,Rm             T2_C3     1110101001011111 0000dddd0000mmmm   EA5F 0000
INST5(cmp,     "cmp",    0,CMP,IF_EN5B,   0x4280,    0x4500,      0x2800,      0xF1B00F00,  0xEBB00F00)
                                   //  cmp     Rn,Rm             T1_E      0100001010mmmnnn                    4280        low
                                   //  cmp     Rn,Rm             T1_D0     01000101Nmmmmnnn                    4500        high
                                   //  cmp     Rn,i8             T1_J0     00101nnniiiiiiii                    2800        low     imm(0-255)
                                   //  cmp     Rn,+i8<<i4        T2_L2     11110i011011nnnn 0iii1111iiiiiiii   F1B0 0F00           imm(i8<<i4)
                                   //  cmp     Rn,Rm{,sh}        T2_C8     111010111011nnnn 0iii1111iishmmmm   EBB0 0F00

//    enum     name      FP LD/ST          Rdn, Rn    Rd,Rn,i5     Rd,Rn,Rm     Rd,Rn,i5
//                                          T1_E       T2_C         T2_C4        T2_C2
INST4(lsl,     "lsl",    0, 0, IF_EN4A,   0x4080,    0x0000,      0xFA00F000,  0xEA4F0000)
                                   //  lsls    Rdn,Rm            T1_E      0100000010mmmddd                    4080        low
                                   //  lsls    Rd,Rm,i5          T1_C      00000iiiiimmmddd                    0000        low     imm(0-31)
                                   //  lsl{s}  Rd,Rn,Rm          T2_C4     11111010000Snnnn 1111dddd0000mmmm   FA00 F000
                                   //  lsl{s}  Rd,Rm,i5          T2_C2     11101010010S1111 0iiiddddii00mmmm   EA4F 0000           imm(0-31)
INST4(lsr,     "lsr",    0, 0, IF_EN4A,   0x40C0,    0x0800,      0xFA20F000,  0xEA4F0010)
                                   //  lsrs    Rdn,Rm            T1_E      0100000011mmmddd                    40C0        low
                                   //  lsrs    Rd,Rm,i5          T1_C      00001iiiiimmmddd                    0800        low     imm(0-31)
                                   //  lsr{s}  Rd,Rn,Rm          T2_C4     11111010001Snnnn 1111dddd0000mmmm   FA20 F000
                                   //  lsr{s}  Rd,Rm,i5          T2_C2     11101010010S1111 0iiiddddii01mmmm   EA4F 0010           imm(0-31)
INST4(asr,     "asr",    0, 0, IF_EN4A,   0x4100,    0x1000,      0xFA40F000,  0xEA4F0020)
                                   //  asrs    Rdn,Rm            T1_E      0100000100mmmddd                    4100        low     shift by Rm
                                   //  asrs    Rd,Rm,i5          T1_C      00010iiiiimmmddd                    1000        low     imm(0-31)
                                   //  asr{s}  Rd,Rn,Rm          T2_C4     11111010010Snnnn 1111dddd0000mmmm   FA40 F000
                                   //  asr{s}  Rd,Rm,i5          T2_C2     11101010010S1111 0iiiddddii10mmmm   EA4F 0020           imm(0-31)
INST4(ror,     "ror",    0, 0, IF_EN4A,   0x41C0,    BAD_CODE,    0xFA60F000,  0xEA4F0030)
                                   //  rors    Rdn,Rm            T1_E      0100000111mmmddd                    41C0        low
                                   //  ror{s}  Rd,Rn,Rm          T2_C4     11111010011Snnnn 1111dddd0000mmmm   FA60 F000
                                   //  ror{s}  Rd,Rm,i5          T2_C2     11101010010S1111 0iiiddddii11mmmm   EA4F 0030           imm(0-31)

//    enum     name      FP LD/ST          Rdn, Rn    Rd,Rn,i5     Rd,Rn,Rm     Rd,Rn,i5
//                                          T2_K2       T2_H2       T2_C7        T2_K3
INST4(pld,     "pld",    0,LD, IF_EN4B,  0xF890F000, 0xF810FC00,  0xF810F000,  0xF81FF000)                               // Cache Prefetch Data for Read
                                   //  pld     [Rn+i12]          T2_K2     111110001001nnnn 1111iiiiiiiiiiii   F890 F000           imm(0-4095)
                                   //  pld     [Rn-i8]           T2_H2     111110000001nnnn 11111100iiiiiiii   F810 FC00           imm(0-255)
                                   //  pld     [Rn+Rm{,sh}]      T2_C7     111110000001nnnn 1111000000shmmmm   F810 F000           sh=(0,1,2,3)
                                   //  pld     [PC+-i12]         T2_K3     11111001U0011111 1111iiiiiiiiiiii   F81F F000           imm(+-4095)
INST4(pldw,    "pldw",   0,LD, IF_EN4B,  0xF8B0F000, 0xF830FC00,  0xF830F000,  BAD_CODE)                                 // Cache Prefetch Data for Write
                                   //  pldw    [Rn+i12]          T2_K2     111110001011nnnn 1111iiiiiiiiiiii   F8B0 F000           imm(0-4095)
                                   //  pldw    [Rn-i8]           T2_H2     111110000011nnnn 11111100iiiiiiii   F830 FC00           imm(0-255)
                                   //  pldw    [Rn+Rm{,sh}]      T2_C7     111110000011nnnn 1111000000shmmmm   F830 F000           sh=(0,1,2,3)
#ifdef FEATURE_PLI_INSTRUCTION
// NOTE: The PLI instruction had an errata in early Krait implementations, so even though it's unlikely we would ever generate it, it is
// #ifdef'ed out to prevent its use.
INST4(pli,     "pli",    0,LD, IF_EN4B,  0xF990F000, 0xF910FC00,  0xF910F000,  0xF91FF000)                               // Cache Prefetch Instructions for Execute
                                   //  pli     [Rn+i12]          T2_K2     111110011001nnnn 1111iiiiiiiiiiii   F990 F000           imm(0-4095)
                                   //  pli     [Rn-i8]           T2_H2     111110010001nnnn 11111100iiiiiiii   F910 FC00           imm(0-255)
                                   //  pli     [Rn+Rm{,sh}]      T2_C7     111110010001nnnn 1111000000shmmmm   F910 F000           sh=(0,1,2,3)
                                   //  pli     [PC+-i12]         T2_K3     11111001U0011111 1111iiiiiiiiiiii   F91F F000           imm(+-4095)
#endif // FEATURE_PLI_INSTRUCTION

//    enum     name      FP LD/ST         Rd,i16     Rd,i16     Rd,i16     Rd,i16
//                                        T2_N       T2_N1      T2_N2      T2_N3
INST4(movt,    "movt",   0, 0, IF_EN4C,   0xF2C00000,0xF2C00000,0xF2C00000,0xF2C00000)
                                           //  Rd,i16            T2_N      11110i101100iiii 0iiiddddiiiiiiii   F2C0 0000           imm(0-65535)
                                           //  Rd,i16            T2_N1     11110i101100iiii 0iiiddddiiiiiiii   F2C0 0000           imm(0-65535)
                                           //  Rd,i16            T2_N2     11110i101100iiii 0iiiddddiiiiiiii   F2C0 0000           imm(0-65535)
                                           //  Rd,i16            T2_N3     11110i101100iiii 0iiiddddiiiiiiii   F2C0 0000           imm(0-65535)
INST4(movw,    "movw",   0, 0, IF_EN4C,   0xF2400000,0xF2400000,0xF2400000,0xF2400000)
                                           //  Rd,+i16           T2_N      11110i100100iiii 0iiiddddiiiiiiii   F240 0000           imm(0-65535)
                                           //  Rd,+i16           T2_N1     11110i100100iiii 0iiiddddiiiiiiii   F240 0000           imm(0-65535)
                                           //  Rd,+i16           T2_N2     11110i100100iiii 0iiiddddiiiiiiii   F240 0000           imm(0-65535)
                                           //  Rd,+i16           T2_N3     11110i100100iiii 0iiiddddiiiiiiii   F240 0000           imm(0-65535)

//    enum     name      FP LD/ST          Rdn, Rm    Rd,Rn,Rm,sh  Rd,Rn,i12
//                                          T1_E       T2_C0        T2_L0
INST3(and,     "and",    0, 0, IF_EN3A,   0x4000,    0xEA000000,   0xF0000000)
                                   //  ands    Rdn,Rm            T1_E      0100000000mmmddd                    4000        low
                                   //  and{s}  Rd,Rn,Rm{,sh}     T2_C0     11101010000Snnnn 0iiiddddiishmmmm   EA00 0000
                                   //  and{s}  Rd,Rn,i12         T2_L0     11110i00000Snnnn 0iiiddddiiiiiiii   F000 0000           imm(i8<<i4)
INST3(eor,     "eor",    0, 0, IF_EN3A,   0x4040,    0xEA800000,   0xF0800000)
                                   //  eors    Rd,Rm             T1_E      0100000001mmmddd                    4040        low
                                   //  eor{s}  Rd,Rn,Rm{,sh}     T2_C0     11101010100Snnnn 0iiiddddiishmmmm   EA80 0000
                                   //  eor{s}  Rd,Rn,i12         T2_L0     11110i00100Snnnn 0iiiddddiiiiiiii   F080 0000           imm(i8<<i4)
INST3(orr,     "orr",    0, 0, IF_EN3A,   0x4300,    0xEA400000,   0xF0400000)
                                   //  orrs    Rdn,Rm            T1_E      0100001100mmmddd                    4300        low
                                   //  orr{s}  Rd,Rn,Rm{,sh}     T2_C0     11101010010Snnnn 0iiiddddiishmmmm   EA40 0000
                                   //  orr{s}  Rd,Rn,i12         T2_L0     11110i00010Snnnn 0iiiddddiiiiiiii   F040 0000           imm(i8<<i4)
INST3(orn,     "orn",    0, 0, IF_EN3A,   BAD_CODE,  0xEA600000,   0xF0600000)
                                   //  orn{s}  Rd,Rn,Rm{,sh}     T2_C0     11101010011Snnnn 0iiiddddiishmmmm   EA60 0000
                                   //  orn{s}  Rd,Rn,i12         T2_L0     11110i00011Snnnn 0iiiddddiiiiiiii   F060 0000           imm(i8<<i4)
INST3(bic,     "bic",    0, 0, IF_EN3A,   0x4380,    0xEA200000,   0xF0200000)
                                   //  bics    Rdn,Rm            T1_E      0100001110mmmddd                    4380        low
                                   //  bic{s}  Rd,Rn,Rm{,sh}     T2_C0     11101010001Snnnn 0iiiddddiishmmmm   EA20 0000
                                   //  bic{s}  Rd,Rn,i12         T2_L0     11110i00001Snnnn 0iiiddddiiiiiiii   F020 0000           imm(i8<<i4)
INST3(adc,     "adc",    0, 0, IF_EN3A,   0x4140,    0xEB400000,   0xF1400000)
                                   //  adcs    Rdn,Rn            T1_E      0100000101mmmddd                    4140        low
                                   //  adcs    Rd,Rn,Rm{,sh}     T2_C0     11101011010Snnnn 0iiiddddiishmmmm   EB40 0000
                                   //  adcs    Rd,Rn,i12         T2_L0     11110i01010Snnnn 0iiiddddiiiiiiii   F140 0000           imm(0-4095)
INST3(sbc,     "sbc",    0, 0, IF_EN3A,   0x4180,    0xEB600000,   0xF1600000)
                                   //  sbcs    Rd,Rm             T1_E      0100000110mmmddd                    4180        low
                                   //  sbc{s}  Rd,Rn,Rm{,sh}     T2_C0     11101011011Snnnn 0iiiddddiishmmmm   EB60 0000
                                   //  sbc{s}  Rd,Rn,+i8<<i4     T2_L0     11110i01011Snnnn 0iiiddddiiiiiiii   F160 0000           imm(i8<<i4)
INST3(rsb,     "rsb",    0, 0, IF_EN3A,   0x4240,    0xEBC00000,   0xF1C00000)
                                   //  rsbs    Rd,Rn,#0          T1_E      0100001001nnnddd                    4240        low     (Note: x86 NEG instr)
                                   //  rsb{s}  Rd,Rn,Rm{,sh}     T2_C0     11101011110Snnnn 0iiiddddiishmmmm   EBC0 0000
                                   //  rsb{s}  Rd,Rn,+i8<<i4     T2_L0     11110i01110Snnnn 0iiiddddiiiiiiii   F1C0 0000           imm(i8<<i4)

//    enum     name      FP LD/ST          Rn,Rm     Rn,Rm,sh      Rn,i12
//                                          T1_E       T2_C8        T2_L2
INST3(tst,     "tst",    0,CMP,IF_EN3B,   0x4200,    0xEA100F00,   0xF0100F00)
                                   //  tst     Rn,Rm             T1_E      0100001000mmmnnn                    4200        low
                                   //  tst     Rn,Rm{,sh}        T2_C8     111010100001nnnn 0iii1111iishmmmm   EA10 0F00
                                   //  tst     Rn,+i8<<i4        T2_L2     11110i000001nnnn 0iii1111iiiiiiii   F010 0F00           imm(i8<<i4)
INST3(teq,     "teq",    0,CMP,IF_EN3B,   BAD_CODE,  0xEA900F00,   0xF0900F00)
                                   //  teq     Rn,Rm{,sh}        T2_C8     111010101001nnnn 0iii1111iishmmmm   EA90 0F00
                                   //  teq     Rn,+i8<<i4        T2_L2     11110i001001nnnn 0iii1111iiiiiiii   F090 0F00           imm(i8<<i4)
INST3(cmn,     "cmn",    0,CMP,IF_EN3B,   0x42C0,    0xEB100F00,   0xF1100F00)
                                   //  cmn     Rn,Rn             T1_E      0100001011mmmnnn                    42C0        low
                                   //  cmn     Rn,Rm{,sh}        T2_C8     111010110001nnnn 0iii1111iishmmmm   EB10 0F00
                                   //  cmn     Rn,+i8<<i4        T2_L2     11110i010001nnnn 0iii1111iiiiiiii   F110 0F00           imm(i8<<i4)

//    enum     name      FP LD/ST          Rd,Rm     Rd,Rm,sh      Rd,Rn,i12
//                                          T1_E       T2_C1        T2_L1
INST3(mvn,     "mvn",    0, 0, IF_EN3C,   0x43C0,    0xEA6F0000,   0xF06F0000)
                                   //  mvns    Rd,Rm             T1_E      0100001111mmmddd                    43C0        low
                                   //  mvn{s}  Rd,Rm{,sh}        T2_C1     11101010011S1111 0iiiddddiishmmmm   EA6F 0000
                                   //  mvn{s}  Rd,+i8<<i4        T2_L1     11110i00011S1111 0iiiddddiiiiiiii   F06F 0000           imm(i8<<i4)

//    enum     name      FP LD/ST          SP,reg8     rT          reg,reg16
//                                          T1_L1      T2_E2        T2_I1
INST3(push,    "push",   0, 0, IF_EN3D,   0xB400,    0xF84D0D04,   0xE92D0000)
                                   //  push    {LR,}<reglist8>   T1_L1     1011010Mrrrrrrrr                    B400        low
                                   //  push    rT                T2_E2     1111100001001101 tttt110100000100   F84D 0D04
                                   //  push    <reglist16>       T2_I1     1110100100101101 0M0rrrrrrrrrrrrr   E92D 0000
INST3(pop,     "pop",    0, 0, IF_EN3D,   0xBC00,    0xF85D0B04,   0xE8BD0000)
                                   //  pop     {PC,}<reglist8>   T1_L1     1011110Prrrrrrrr                    BC00        low
                                   //  pop     rT                T2_E2     1111100001011101 tttt101100000100   F85D 0B04
                                   //  pop     <reglist16>       T2_I1     1110100010111101 PM0rrrrrrrrrrrrr   E8BD 0000

//    enum     name      FP LD/ST         PC+-imm11 PC+-imm24   PC+-imm24
//                                          T1_M      T2_J2       T2_J3
INST3(b,       "b",      0, 0, IF_EN3E,   0xE000,   0xF0009000, 0xF0009000)
                                   //  b       PC+-i11            T1_M      11100iiiiiiiiiii                    E000                imm(-2048..2046)
                                   //  b       PC+-i24            T2_J2     11110Siiiiiiiiii 10j1jiiiiiiiiiii   F000 9000           imm(-16777216..16777214) (intra-procedure offset)
                                   //  b       PC+-i24            T2_J3     11110Siiiiiiiiii 10j1jiiiiiiiiiii   F000 9000           imm(-16777216..16777214) (inter-procedure offset)


//    enum     name      FP LD/ST         PC+-imm8  PC+-imm20
//                                          T1_K      T2_J1
INST2(beq,     "beq",    0, 0, IF_EN2A,   0xD000,    0xF0008000)
                                   //  beq     PC+-i8             T1_K      11010000iiiiiiii                    D000                imm(-256..254)
                                   //  beq     PC+-i20            T2_J1     11110S0000iiiiii 10j0jiiiiiiiiiii   F000 8000           imm(-1048576..1048574)
INST2(bne,     "bne",    0, 0, IF_EN2A,   0xD100,    0xF0408000)
                                   //  bne     PC+-i8             T1_K      11010001iiiiiiii                    D000                imm(-256..254)
                                   //  bne     PC+-i20            T2_J1     11110S0001iiiiii 10j0jiiiiiiiiiii   F000 8000           imm(-1048576..1048574)
INST2(bhs,     "bhs",    0, 0, IF_EN2A,   0xD200,    0xF0808000)
                                   //  bhs     PC+-i8             T1_K      11010010iiiiiiii                    D000                imm(-256..254)
                                   //  bhs     PC+-i20            T2_J1     11110S0010iiiiii 10j0jiiiiiiiiiii   F000 8000           imm(-1048576..1048574)
INST2(blo,     "blo",    0, 0, IF_EN2A,   0xD300,    0xF0C08000)
                                   //  blo     PC+-i8             T1_K      11010011iiiiiiii                    D000                imm(-256..254)
                                   //  blo     PC+-i20            T2_J1     11110S0011iiiiii 10j0jiiiiiiiiiii   F000 8000           imm(-1048576..1048574)
INST2(bmi,     "bmi",    0, 0, IF_EN2A,   0xD400,    0xF1008000)
                                   //  bmi     PC+-i8             T1_K      11010100iiiiiiii                    D000                imm(-256..254)
                                   //  bmi     PC+-i20            T2_J1     11110S0100iiiiii 10j0jiiiiiiiiiii   F000 8000           imm(-1048576..1048574)
INST2(bpl,     "bpl",    0, 0, IF_EN2A,   0xD500,    0xF1408000)
                                   //  bpl     PC+-i8             T1_K      11010101iiiiiiii                    D000                imm(-256..254)
                                   //  bpl     PC+-i20            T2_J1     11110S0101iiiiii 10j0jiiiiiiiiiii   F000 8000           imm(-1048576..1048574)
INST2(bvs,     "bvs",    0, 0, IF_EN2A,   0xD600,    0xF1808000)
                                   //  bvs     PC+-i8             T1_K      11010110iiiiiiii                    D000                imm(-256..254)
                                   //  bvs     PC+-i20            T2_J1     11110S0110iiiiii 10j0jiiiiiiiiiii   F000 8000           imm(-1048576..1048574)
INST2(bvc,     "bvc",    0, 0, IF_EN2A,   0xD700,    0xF1C08000)
                                   //  bvc     PC+-i8             T1_K      11010111iiiiiiii                    D000                imm(-256..254)
                                   //  bvc     PC+-i20            T2_J1     11110S0111iiiiii 10j0jiiiiiiiiiii   F000 8000           imm(-1048576..1048574)
INST2(bhi,     "bhi",    0, 0, IF_EN2A,   0xD800,    0xF2008000)
                                   //  bhi     PC+-i8             T1_K      11011000iiiiiiii                    D000                imm(-256..254)
                                   //  bhi     PC+-i20            T2_J1     11110S1000iiiiii 10j0jiiiiiiiiiii   F000 8000           imm(-1048576..1048574)
INST2(bls,     "bls",    0, 0, IF_EN2A,   0xD900,    0xF2408000)
                                   //  bls     PC+-i8             T1_K      11011001iiiiiiii                    D000                imm(-256..254)
                                   //  bls     PC+-i20            T2_J1     11110S1001iiiiii 10j0jiiiiiiiiiii   F000 8000           imm(-1048576..1048574)
INST2(bge,     "bge",    0, 0, IF_EN2A,   0xDA00,    0xF2808000)
                                   //  bge     PC+-i8             T1_K      11011010iiiiiiii                    D000                imm(-256..254)
                                   //  bge     PC+-i20            T2_J1     11110S1010iiiiii 10j0jiiiiiiiiiii   F000 8000           imm(-1048576..1048574)
INST2(blt,     "blt",    0, 0, IF_EN2A,   0xDB00,    0xF2C08000)
                                   //  blt     PC+-i8             T1_K      11011011iiiiiiii                    D000                imm(-256..254)
                                   //  blt     PC+-i20            T2_J1     11110S1011iiiiii 10j0jiiiiiiiiiii   F000 8000           imm(-1048576..1048574)
INST2(bgt,     "bgt",    0, 0, IF_EN2A,   0xDC00,    0xF3008000)
                                   //  bgt     PC+-i8             T1_K      11011100iiiiiiii                    D000                imm(-256..254)
                                   //  bgt     PC+-i20            T2_J1     11110S1100iiiiii 10j0jiiiiiiiiiii   F000 8000           imm(-1048576..1048574)
INST2(ble,     "ble",    0, 0, IF_EN2A,   0xDD00,    0xF3408000)
                                   //  ble     PC+-i8             T1_K      11011101iiiiiiii                    D000                imm(-256..254)
                                   //  ble     PC+-i20            T2_J1     11110S1101iiiiii 10j0jiiiiiiiiiii   F000 8000           imm(-1048576..1048574)

//    enum     name      FP LD/ST           Rm          Rm
//                                          T1_D1       T1_D2
INST2(bx,      "bx",     0, 0, IF_EN2B,   0x4700,     0x4700)
                                   //  bx      Rm                T1_D1     010001110mmmm000                     4700        high
                                   //  bx      Rm                T1_D2     010001110mmmm000                     4700        high

//    enum     name      FP LD/ST           rM      PC+-imm24
//                                          T1_D2      T2_J3
INST2(blx,     "blx",    0, 0, IF_EN2C,   0x4780,   0xF000C000)
                                   //  blx     Rm                 T1_D2     010001111mmmm000                    4780        high
                                   //  blx     PC+-i24            T2_J3     11110Siiiiiiiiii 11j0jiiiiiiiiii0   F000 C000           imm(-16777216..16777214)

//    enum     name      FP LD/ST          Rn,<reg8>  Rn,<reg16>
//                                          T1_J1      T2_I0
INST2(ldm,     "ldm",    0,LD, IF_EN2D,   0xC800,   0xE8900000)
                                   //  ldm     Rn,<reglist8>     T1_J1     11001nnnrrrrrrrr                    C800        low
                                   //  ldm     Rn{!},<reglist16> T2_I0     1110100010W1nnnn rr0rrrrrrrrrrrrr   E890 0000
INST2(stm,     "stm",    0,ST, IF_EN2D,   0xC000,   0xE8800000)
                                   //  stm     Rn!,<reglist8>    T1_J1    11000nnnrrrrrrrr                    C000        low
                                   //  stm     Rn{!},<reglist16> T2_I0     1110100010W0nnnn 0r0rrrrrrrrrrrrr   E880 0000

//    enum     name      FP LD/ST          Rn,Rm      Rn,Rm,{sb}
//                                          T1_E       T2_C6
INST2(sxtb,    "sxtb",   0, 0, IF_EN2E,   0xB240,    0xFA4FF080)
                                           //  Rd,Rm             T1_E      1011001001mmmddd                    B240        low
                                           //  Rd,Rm{,sb}        T2_C6     1111101001001111 1111dddd10sbmmmm   FA4F F080
INST2(sxth,    "sxth",   0, 0, IF_EN2E,   0xB200,    0xFA0FF080)
                                           //  Rd,Rm             T1_E      1011001000mmmddd                    B200        low
                                           //  Rd,Rm{,sb}        T2_C6     1111101000001111 1111dddd10sbmmmm   FA0F F080
INST2(uxtb,    "uxtb",   0, 0, IF_EN2E,   0xB2C0,    0xFA5FF080)
                                           //  Rd,Rm             T1_E      1011001011mmmddd                    B2C0        low
                                           //  Rd,Rm{,sb}        T2_C6     1111101001011111 1111dddd10sbmmmm   FA5F F080
INST2(uxth,    "uxth",   0, 0, IF_EN2E,   0xB280,    0xFA1FF080)
                                           //  Rd,Rm             T1_E      1011001010mmmddd                    B280        low
                                           //  Rd,Rm             T2_C6     1111101000011111 1111dddd10sbmmmm   FA1F F080

//    enum     name      FP LD/ST          Rdn,Rm     Rd,Rn,Rm
//                                          T1_E       T2_C5
INST2(mul,     "mul",    0, 0, IF_EN2F,   0x4340,    0xFB00F000)
                                           //  Rd,Rm             T1_E      0100001101nnnddd                    4340        low
                                           //  Rd,Rn,Rm          T2_C5     111110110000nnnn 1111dddd0000mmmm   FB00 F000

//    enum     name      FP LD/ST          Rd,PC,i10   Rd,PC,+-i12
//                                          T1_J3       T2_M1
INST2(adr,     "adr",    0, 0, IF_EN2G,   0xA000,    0xF20F0000)
                                           //  Rd,PC+i10         T1_J3     10100dddiiiiiiii                    A000        low     imm(0-1020)
                                           //  Rd,PC+-i12        T2_M1     11110i10U0U01111 0iiiddddiiiiiiii   F20F 0000           imm(+-4095)

INST1(addw,    "addw",   0, 0, IF_T2_M0,  0xF2000000)
                                           //  Rd,Rn,i12         T2_M0     11110i100000nnnn 0iiiddddiiiiiiii   F200 0000           imm(0-4095)
INST1(bfc,     "bfc",    0, 0, IF_T2_D1,  0xF36F0000)
                                           //  Rd,#b,#w          T2_D1     1111001101101111 0iiiddddii0wwwww   F36F 0000           imm(0-31),imm(0-31)
INST1(bfi,     "bfi",    0, 0, IF_T2_D0,  0xF3600000)
                                           //  Rd,Rn,#b,#w       T2_D0     111100110110nnnn 0iiiddddii0wwwww   F360 0000           imm(0-31),imm(0-31)
INST1(bl,      "bl",     0, 0, IF_T2_J3,  0xF000D000)
                                           //  PC+-i24           T2_J3     11110Siiiiiiiiii 11j1jiiiiiiiiiii   F000 D000           imm(-16777216..16777214)
INST1(bkpt,    "bkpt",   0, 0, IF_T1_A,   0xDEFE)
                                           //                    T1_A      1101111011111110                    DEFE                // Windows uses this
                                           //  i8                T1_L0     10111110iiiiiiii                    BE00                imm(0-255)
INST1(cbnz,    "cbnz",   0, 0, IF_T1_I,   0xB900)
                                           //  Rn,PC+i7          T1_I      101110i1iiiiinnn                    B900        low     imm(0-126)
INST1(cbz,     "cbz",    0, 0, IF_T1_I,   0xB100)
                                           //  Rn,PC+i7          T1_I      101100i1iiiiinnn                    B100        low     imm(0-126)
INST1(clz,     "clz",    0, 0, IF_T2_C10, 0xFAB0F000)
                                           //  Rd,Rm             T2_C10    111110101011mmmm 1111dddd0000mmmm   FAB0 F000
INST1(dmb,     "dmb",    0, 0, IF_T2_B,   0xF3BF8F50)
                                           //  #i4               T2_B      1111001110111111 100011110101iiii   F3BF 8F50           imm(0-15)
INST1(ism,     "ism",    0, 0, IF_T2_B,   0xF3BF8F60)
                                           //  #i4               T2_B      1111001110111111 100011110110iiii   F3BF 8F60           imm(0-15)
INST1(ldmdb,   "ldmdb",  0,LD, IF_T2_I0,  0xE9100000)
                                           //  Rn{!},<reglist16> T2_I0     1110100100W1nnnn rr0rrrrrrrrrrrrr   E910 0000
INST1(ldrd,    "ldrd",   0,LD, IF_T2_G0,  0xE8500000)
                                           //  Rt,RT,[Rn],+-i8{!}T2_G0     1110100PU1W1nnnn ttttTTTTiiiiiiii   E850 0000
INST1(ldrex,   "ldrex",  0,LD, IF_T2_H1,  0xE8500F00)
                                           //  Rt,[Rn+i8]        T2_H1     111010000101nnnn tttt1111iiiiiiii   E850 0F00           imm(0-1020)
INST1(ldrexb,  "ldrexb", 0,LD, IF_T2_E1,  0xE8D00F4F)
                                           //  Rt,[Rn]           T2_E1     111010001101nnnn tttt111101001111   E8D0 0F4F
INST1(ldrexd,  "ldrexd", 0,LD, IF_T2_G1,  0xE8D0007F)
                                           //  Rt,RT,[Rn]        T2_G1     111010001101nnnn ttttTTTT01111111   E8D0 007F
INST1(ldrexh,  "ldrexh", 0,LD, IF_T2_E1,  0xE8D00F5F)
                                           //  Rt,[Rn]           T2_E1     111010001101nnnn tttt111101011111   E8D0 0F5F
INST1(mla,     "mla",    0, 0, IF_T2_F2,  0xFB000000)
                                           //  Rd,Rn,Rm,Ra       T2_F2     111110110000nnnn aaaadddd0000mmmm   FB00 0000
INST1(mls,     "mls",    0, 0, IF_T2_F2,  0xFB000010)
                                           //  Rd,Rn,Rm,Ra       T2_F2     111110110000nnnn aaaadddd0001mmmm   FB00 0010
INST1(nop,     "nop",    0, 0, IF_T1_A,   0xBF00)
                                           //                    T1_A      1011111100000000                    BF00
INST1(nopw,    "nop.w",  0, 0, IF_T2_A,   0xF3AF8000)
                                           //                    T2_A      1111001110101111 1000000000000000   F3AF 8000
INST1(sbfx,    "sbfx",   0, 0, IF_T2_D0,  0xF3400000)
                                           //  Rd,Rn,#b,#w       T2_D0     111100110100nnnn 0iiiddddii0wwwww   F340 0000           imm(0-31),imm(0-31)
INST1(sdiv,    "sdiv",   0, 0, IF_T2_C5,  0xFB90F0F0)
                                           //  Rd,Rn,Rm          T2_C5     111110111001nnnn 1111dddd1111mmmm   FB90 F0F0
INST1(smlal,   "smlal",  0, 0, IF_T2_F1,  0xFBC00000)
                                           //  Rl,Rh,Rn,Rm       T2_F1     111110111100nnnn llllhhhh0000mmmm   FBC0 0000
INST1(smull,   "smull",  0, 0, IF_T2_F1,  0xFB800000)
                                           //  Rl,Rh,Rn,Rm       T2_F1     111110111000nnnn llllhhhh0000mmmm   FB80 0000
INST1(stmdb,   "stmdb",  0,ST, IF_EN2D,   0xE9000000)
                                           //  Rn{!},<reglist16> T2_I0     1110100100W0nnnn 0r0rrrrrrrrrrrrr   E900 0000
INST1(strd,    "strd",   0,ST, IF_T2_G0,  0xE8400000)
                                           //  Rt,RT,[Rn],+-i8{!}T2_G0     1110100PU1W0nnnn ttttTTTTiiiiiiii   E840 0000
INST1(strex,   "strex",  0,ST, IF_T2_H1,  0xE8400F00)
                                           //  Rt,[Rn+i8]        T2_H1     111010000100nnnn tttt1111iiiiiiii   E840 0F00           imm(0-1020)
INST1(strexb,  "strexb", 0,ST, IF_T2_E1,  0xE8C00F4F)
                                           //  Rt,[Rn]           T2_E1     111010001100nnnn tttt111101001111   E8C0 0F4F
INST1(strexd,  "strexd", 0,ST, IF_T2_G1,  0xE8C0007F)
                                           //  Rt,RT,[Rn]        T2_G1     111010001100nnnn ttttTTTT01111111   E8C0 007F
INST1(strexh,  "strexh", 0,ST, IF_T2_E1,  0xE8C00F5F)
                                           //  Rt,[Rn]           T2_E1     111010001100nnnn tttt111101011111   E8C0 0F5F
INST1(subw,    "subw",   0, 0, IF_T2_M0,  0xF2A00000)
                                           //  Rd,Rn,+i12        T2_M0     11110i101010nnnn 0iiiddddiiiiiiii   F2A0 0000           imm(0-4095)
INST1(tbb,     "tbb",    0, 0, IF_T2_C9,  0xE8D0F000)
                                           //  Rn,Rm             T2_C9     111010001101nnnn 111100000000mmmm   E8D0 F000
INST1(tbh,     "tbh",    0, 0, IF_T2_C9,  0xE8D0F010)
                                           //  Rn,Rm,LSL #1      T2_C9     111010001101nnnn 111100000001mmmm   E8D0 F010
INST1(ubfx,    "ubfx",   0, 0, IF_T2_D0,  0xF3C00000)
                                           //  Rd,Rn,#b,#w       T2_D0     111100111100nnnn 0iiiddddii0wwwww   F3C0 0000           imm(0-31),imm(0-31)
INST1(udiv,    "udiv",   0, 0, IF_T2_C5,  0xFBB0F0F0)
                                           //  Rd,Rn,Rm          T2_C5     111110111011nnnn 1111dddd1111mmmm   FBB0 F0F0
INST1(umlal,   "umlal",  0, 0, IF_T2_F1,  0xFBE00000)
                                           //  Rl,Rh,Rn,Rm       T2_F1     111110111110nnnn llllhhhh0000mmmm   FBE0 0000
INST1(umull,   "umull",  0, 0, IF_T2_F1,  0xFBA00000)
                                           //  Rl,Rh,Rn,Rm       T2_F1     111110111010nnnn llllhhhh0000mmmm   FBA0 0000

#ifdef FEATURE_ITINSTRUCTION
INST1(it,      "it",     0, 0, IF_T1_B,   0xBF08)
                                           //  cond              T1_B      10111111cond1000                    BF08                cond
INST1(itt,     "itt",    0, 0, IF_T1_B,   0xBF04)
                                           //  cond              T1_B      10111111cond0100                    BF04                cond
INST1(ite,     "ite",    0, 0, IF_T1_B,   0xBF0C)
                                           //  cond              T1_B      10111111cond1100                    BF0C                cond
INST1(ittt,    "ittt",   0, 0, IF_T1_B,   0xBF02)
                                           //  cond              T1_B      10111111cond0010                    BF02                cond
INST1(itte,    "itte",   0, 0, IF_T1_B,   0xBF06)
                                           //  cond              T1_B      10111111cond0110                    BF06                cond
INST1(itet,    "itet",   0, 0, IF_T1_B,   0xBF0A)
                                           //  cond              T1_B      10111111cond1010                    BF0A                cond
INST1(itee,    "itee",   0, 0, IF_T1_B,   0xBF0E)
                                           //  cond              T1_B      10111111cond1110                    BF0E                cond
INST1(itttt,   "itttt",  0, 0, IF_T1_B,   0xBF01)
                                           //  cond              T1_B      10111111cond0001                    BF01                cond
INST1(ittte,   "ittte",  0, 0, IF_T1_B,   0xBF01)
                                           //  cond              T1_B      10111111cond0011                    BF03                cond
INST1(ittet,   "ittet",  0, 0, IF_T1_B,   0xBF01)
                                           //  cond              T1_B      10111111cond0101                    BF05                cond
INST1(ittee,   "ittee",  0, 0, IF_T1_B,   0xBF01)
                                           //  cond              T1_B      10111111cond0111                    BF07                cond
INST1(itett,   "itett",  0, 0, IF_T1_B,   0xBF01)
                                           //  cond              T1_B      10111111cond1001                    BF09                cond
INST1(itete,   "itete",  0, 0, IF_T1_B,   0xBF01)
                                           //  cond              T1_B      10111111cond1011                    BF0B                cond
INST1(iteet,   "iteet",  0, 0, IF_T1_B,   0xBF01)
                                           //  cond              T1_B      10111111cond1101                    BF0D                cond
INST1(iteee,   "iteee",  0, 0, IF_T1_B,   0xBF01)
                                           //  cond              T1_B      10111111cond1111                    BF0F                cond
#endif // FEATURE_ITINSTRUCTION


/*****************************************************************************/
/*             Floating Point Instructions                                   */
/*****************************************************************************/
//    enum      name          FP LD/ST
                                           //  Dd,[Rn+imm8]      T2_VLDST  11101101UD0Lnnnn  dddd101Ziiiiiiii   ED00 0A00           imm(+-1020)
INST1(vstr,     "vstr",        1,ST,   IF_T2_VLDST, 0xED000A00)
INST1(vldr,     "vldr",        1,LD,   IF_T2_VLDST, 0xED100A00)
INST1(vstm,     "vstm",        1,ST,   IF_T2_VLDST, 0xEC800A00)   // A8.6.399 VSTM (to an address in ARM core register from consecutive floats)
INST1(vldm,     "vldm",        1,LD,   IF_T2_VLDST, 0xEC900A00)   // A8.6.399 VLDM (from an address in ARM core register to consecutive floats)
INST1(vpush,    "vpush",       1,ST,   IF_T2_VLDST, 0xED2D0A00)
INST1(vpop,     "vpop",        1,LD,   IF_T2_VLDST, 0xECBD0A00)

                                           //  vmrs    rT        T2_E2     1110111011110001  tttt101000010000   EEF1 0A10
INST1(vmrs,     "vmrs",        1, 0,   IF_T2_E2,    0xEEF10A10)

                                           //  Dd,Dn,Dm          T2_VFP3   11101110-D--nnnn  dddd101ZN-M0mmmm   EE30 0A00
INST1(vadd,     "vadd",        1, 0,   IF_T2_VFP3,  0xEE300A00)
INST1(vsub,     "vsub",        1, 0,   IF_T2_VFP3,  0xEE300A40)
INST1(vmul,     "vmul",        1, 0,   IF_T2_VFP3,  0xEE200A00)
INST1(vdiv,     "vdiv",        1, 0,   IF_T2_VFP3,  0xEE800A00)

                                           //  Dd,Dm             T2_VFP2   111011101D110---  dddd101zp1M0mmmm   EEB0 0A40
INST1(vmov,     "vmov",        1, 0,   IF_T2_VFP2,  0xEEB00A40)               // opc2 = '000',  zp = 00
INST1(vabs,     "vabs",        1, 0,   IF_T2_VFP2,  0xEEB00AC0)               // opc2 = '000',  zp = 01
INST1(vsqrt,    "vsqrt",       1, 0,   IF_T2_VFP2,  0xEEB10AC0)               // opc2 = '001',  zp = 01
INST1(vneg,     "vneg",        1, 0,   IF_T2_VFP2,  0xEEB10A40)               // opc2 = '001',  zp = 00
INST1(vcmp,     "vcmp",        1, CMP, IF_T2_VFP2,  0xEEB40A40)               // opc2 = '100',  zp = 00
INST1(vcmp0,    "vcmp.0",      1, CMP, IF_T2_VFP2,  0xEEB50A40)               // opc2 = '101',  zp = 00

                                           //  Dd,Dm             T2_VFP2   111011101D111---  dddd101zp1M0mmmm   EEB8 0A40
INST1(vcvt_d2i,  "vcvt.d2i",   1, 0,   IF_T2_VFP2,  0xEEBD0BC0)               // opc2 = '101',  zp = 11
INST1(vcvt_f2i,  "vcvt.f2i",   1, 0,   IF_T2_VFP2,  0xEEBD0AC0)               // opc2 = '101',  zp = 01
INST1(vcvt_d2u,  "vcvt.d2u",   1, 0,   IF_T2_VFP2,  0xEEBC0BC0)               // opc2 = '100',  zp = 11
INST1(vcvt_f2u,  "vcvt.f2u",   1, 0,   IF_T2_VFP2,  0xEEBC0AC0)               // opc2 = '100',  zp = 01

INST1(vcvt_i2f,  "vcvt.i2f",   1, 0,   IF_T2_VFP2,  0xEEB80AC0)               // opc2 = '000',  zp = 01
INST1(vcvt_i2d,  "vcvt.i2d",   1, 0,   IF_T2_VFP2,  0xEEB80BC0)               // opc2 = '000',  zp = 11
INST1(vcvt_u2f,  "vcvt.u2f",   1, 0,   IF_T2_VFP2,  0xEEB80A40)               // opc2 = '000',  zp = 00
INST1(vcvt_u2d,  "vcvt.u2d",   1, 0,   IF_T2_VFP2,  0xEEB80B40)               // opc2 = '000',  zp = 10

                                           //  Dd,Dm             T2_VFP2   111011101D110111  dddd101z11M0mmmm   EEB7 0AC0
INST1(vcvt_d2f,  "vcvt.d2f",   1, 0,   IF_T2_VFP2,  0xEEB70BC0)               // opc2 = '111'   zp = 01
INST1(vcvt_f2d,  "vcvt.f2d",   1, 0,   IF_T2_VFP2,  0xEEB70AC0)               // opc2 = '111'   zp = 11

                                           //  Dd,Dm             T2_VMOVD  111011F100D0V0000
INST1(vmov_i2d,  "vmov.i2d",   1, 0,   IF_T2_VMOVD, 0xEC400B10) // A8.6.332 VMOV from 2 int regs to a double
INST1(vmov_d2i,  "vmov.d2i",   1, 0,   IF_T2_VMOVD, 0xEC500B10) // A8.6.332 VMOV from a double to 2 int regs
INST1(vmov_i2f,  "vmov.i2f",   1, 0,   IF_T2_VMOVS, 0xEE000A10) // A8.6.330 VMOV (between ARM core register and single-precision register)
INST1(vmov_f2i,  "vmov.f2i",   1, 0,   IF_T2_VMOVS, 0xEE100A10) // A8.6.330 VMOV (between ARM core register and single-precision register)
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
/*****************************************************************************/
