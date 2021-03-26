// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//////////////////////////////////////////////////////////////////////////////

// clang-format off
#if !defined(TARGET_ARM64)
#error Unexpected target type
#endif

#ifdef DEFINE_ID_OPS
//////////////////////////////////////////////////////////////////////////////

#undef DEFINE_ID_OPS

enum ID_OPS
{
    ID_OP_NONE, // no additional arguments
    ID_OP_SCNS, // small const  operand (21-bits or less, no reloc)
    ID_OP_JMP,  // local jump
    ID_OP_CALL, // method call
    ID_OP_SPEC, // special handling required
};

//////////////////////////////////////////////////////////////////////////////
#else // !DEFINE_ID_OPS
//////////////////////////////////////////////////////////////////////////////

#ifndef IF_DEF
#error Must define IF_DEF macro before including this file
#endif

//////////////////////////////////////////////////////////////////////////////
//
// enum insFormat   instruction            enum ID_OPS
//                  scheduling
//                  (unused)
//////////////////////////////////////////////////////////////////////////////

IF_DEF(NONE, IS_NONE, NONE) //

IF_DEF(LABEL, IS_NONE, JMP)    // label
IF_DEF(LARGEJMP, IS_NONE, JMP) // large conditional branch pseudo-op (cond branch + uncond branch)
IF_DEF(LARGEADR, IS_NONE, JMP) // large address pseudo-op (adrp + add)
IF_DEF(LARGELDC, IS_NONE, JMP) // large constant pseudo-op (adrp + ldr)

/////////////////////////////////////////////////////////////////////////////////////////////////////////

IF_DEF(EN9, IS_NONE, NONE)  // Instruction has 9 possible encoding types
IF_DEF(EN6A, IS_NONE, NONE) // Instruction has 6 possible encoding types, type A
IF_DEF(EN6B, IS_NONE, NONE) // Instruction has 6 possible encoding types, type B
IF_DEF(EN5A, IS_NONE, NONE) // Instruction has 5 possible encoding types, type A
IF_DEF(EN5B, IS_NONE, NONE) // Instruction has 5 possible encoding types, type B
IF_DEF(EN5C, IS_NONE, NONE) // Instruction has 5 possible encoding types, type C
IF_DEF(EN4A, IS_NONE, NONE) // Instruction has 4 possible encoding types, type A
IF_DEF(EN4B, IS_NONE, NONE) // Instruction has 4 possible encoding types, type B
IF_DEF(EN4C, IS_NONE, NONE) // Instruction has 4 possible encoding types, type C
IF_DEF(EN4D, IS_NONE, NONE) // Instruction has 4 possible encoding types, type D
IF_DEF(EN4E, IS_NONE, NONE) // Instruction has 4 possible encoding types, type E
IF_DEF(EN4F, IS_NONE, NONE) // Instruction has 4 possible encoding types, type F
IF_DEF(EN4G, IS_NONE, NONE) // Instruction has 4 possible encoding types, type G
IF_DEF(EN4H, IS_NONE, NONE) // Instruction has 4 possible encoding types, type H
IF_DEF(EN4I, IS_NONE, NONE) // Instruction has 4 possible encoding types, type I
IF_DEF(EN4J, IS_NONE, NONE) // Instruction has 4 possible encoding types, type J
IF_DEF(EN4K, IS_NONE, NONE) // Instruction has 4 possible encoding types, type K
IF_DEF(EN3A, IS_NONE, NONE) // Instruction has 3 possible encoding types, type A
IF_DEF(EN3B, IS_NONE, NONE) // Instruction has 3 possible encoding types, type B
IF_DEF(EN3C, IS_NONE, NONE) // Instruction has 3 possible encoding types, type C
IF_DEF(EN3D, IS_NONE, NONE) // Instruction has 3 possible encoding types, type D
IF_DEF(EN3E, IS_NONE, NONE) // Instruction has 3 possible encoding types, type E
IF_DEF(EN3F, IS_NONE, NONE) // Instruction has 3 possible encoding types, type F
IF_DEF(EN3G, IS_NONE, NONE) // Instruction has 3 possible encoding types, type G
IF_DEF(EN3H, IS_NONE, NONE) // Instruction has 3 possible encoding types, type H
IF_DEF(EN3I, IS_NONE, NONE) // Instruction has 3 possible encoding types, type I
IF_DEF(EN3J, IS_NONE, NONE) // Instruction has 3 possible encoding types, type J
IF_DEF(EN2A, IS_NONE, NONE) // Instruction has 2 possible encoding types, type A
IF_DEF(EN2B, IS_NONE, NONE) // Instruction has 2 possible encoding types, type B
IF_DEF(EN2C, IS_NONE, NONE) // Instruction has 2 possible encoding types, type C
IF_DEF(EN2D, IS_NONE, NONE) // Instruction has 2 possible encoding types, type D
IF_DEF(EN2E, IS_NONE, NONE) // Instruction has 2 possible encoding types, type E
IF_DEF(EN2F, IS_NONE, NONE) // Instruction has 2 possible encoding types, type F
IF_DEF(EN2G, IS_NONE, NONE) // Instruction has 2 possible encoding types, type G
IF_DEF(EN2H, IS_NONE, NONE) // Instruction has 2 possible encoding types, type H
IF_DEF(EN2I, IS_NONE, NONE) // Instruction has 2 possible encoding types, type I
IF_DEF(EN2J, IS_NONE, NONE) // Instruction has 2 possible encoding types, type J
IF_DEF(EN2K, IS_NONE, NONE) // Instruction has 2 possible encoding types, type K
IF_DEF(EN2L, IS_NONE, NONE) // Instruction has 2 possible encoding types, type L
IF_DEF(EN2M, IS_NONE, NONE) // Instruction has 2 possible encoding types, type M
IF_DEF(EN2N, IS_NONE, NONE) // Instruction has 2 possible encoding types, type N
IF_DEF(EN2O, IS_NONE, NONE) // Instruction has 2 possible encoding types, type O
IF_DEF(EN2P, IS_NONE, NONE) // Instruction has 2 possible encoding types, type P
IF_DEF(EN2Q, IS_NONE, NONE) // Instruction has 2 possible encoding types, type Q

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//
// Key for insFormat names:
//
// Above (Specifies multiple encodings)
//
//   EN#? ::  (count of the number of encodings)
//            (? is a unique letter A,B,C...)
//
// Below  (Specifies an exact instruction encoding)
//
//       -- the first two characters are
//
//   DI  :: Data Processing - Immediate
//   DR  :: Data Processing - Register
//   DV  :: Data Processing - Vector Register
//   LS  :: Loads and Stores
//   BI  :: Branches - Immediate
//   BR  :: Branches - Register
//   SN  :: System - No Registers or Immediates
//   SI  :: System - Immediate
//   SR  :: System - Register
//
//   _   :: a separator char '_'
//
//       -- the next two characters are
//
//   #   :: number of registers in the encoding
//   ?   :: A unique letter A,B,C,...
//       -- optional third character
//   I   :: by element immediate
//
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

IF_DEF(BI_0A, IS_NONE, JMP)  // BI_0A   ......iiiiiiiiii iiiiiiiiiiiiiiii               simm26:00   b
IF_DEF(BI_0B, IS_NONE, JMP)  // BI_0B   ......iiiiiiiiii iiiiiiiiiii.....               simm19:00   b<cond>
IF_DEF(BI_0C, IS_NONE, CALL) // BI_0C   ......iiiiiiiiii iiiiiiiiiiiiiiii               simm26:00   bl
IF_DEF(BI_1A, IS_NONE, JMP)  // BI_1A   X.......iiiiiiii iiiiiiiiiiittttt      Rt       simm19:00   cbz cbnz
IF_DEF(BI_1B, IS_NONE, JMP)  // BI_1B   B.......bbbbbiii iiiiiiiiiiittttt      Rt imm6  simm14:00   tbz tbnz
IF_DEF(BR_1A, IS_NONE, CALL) // BR_1A   ................ ......nnnnn.....         Rn                ret
IF_DEF(BR_1B, IS_NONE, CALL) // BR_1B   ................ ......nnnnn.....         Rn                br blr

IF_DEF(LS_1A, IS_NONE, JMP)  // LS_1A   XX...V..iiiiiiii iiiiiiiiiiittttt      Rt    PC imm(1MB)
IF_DEF(LS_2A, IS_NONE, NONE) // LS_2A   .X.......X...... ......nnnnnttttt      Rt Rn
IF_DEF(LS_2B, IS_NONE, NONE) // LS_2B   .X.......Xiiiiii iiiiiinnnnnttttt      Rt Rn    imm(0-4095)
IF_DEF(LS_2C, IS_NONE, NONE) // LS_2C   .X.......X.iiiii iiiiP.nnnnnttttt      Rt Rn    imm(-256..+255) pre/post inc
IF_DEF(LS_2D, IS_NONE, NONE) // LS_2D   .Q.............. ....ssnnnnnttttt      Vt Rn    Load/Store multiple structures       base register
                             //                                                         Load single structure and replicate  base register
IF_DEF(LS_2E, IS_NONE, NONE) // LS_2E   .Q.............. ....ssnnnnnttttt      Vt Rn    Load/Store multiple structures       post-indexed by an immediate
                             //                                                         Load single structure and replicate  post-indexed by an immediate
IF_DEF(LS_2F, IS_NONE, NONE) // LS_2F   .Q.............. xx.Sssnnnnnttttt      Vt[] Rn  Load/Store single structure          base register
IF_DEF(LS_2G, IS_NONE, NONE) // LS_2G   .Q.............. xx.Sssnnnnnttttt      Vt[] Rn  Load/Store single structure          post-indexed by an immediate
IF_DEF(LS_3A, IS_NONE, NONE) // LS_3A   .X.......X.mmmmm xxxS..nnnnnttttt      Rt Rn Rm ext(Rm) LSL {}
IF_DEF(LS_3B, IS_NONE, NONE) // LS_3B   X............... .aaaaannnnnddddd      Rd Ra Rn
IF_DEF(LS_3C, IS_NONE, NONE) // LS_3C   X.........iiiiii iaaaaannnnnddddd      Rd Ra Rn imm(im7,sh)
IF_DEF(LS_3D, IS_NONE, NONE) // LS_3D   .X.......X.mmmmm ......nnnnnttttt      Wm Rt Rn
IF_DEF(LS_3E, IS_NONE, NONE) // LS_3E   .X.........mmmmm ......nnnnnttttt      Rm Rt Rn ARMv8.1 LSE Atomics
IF_DEF(LS_3F, IS_NONE, NONE) // LS_3F   .Q.........mmmmm ....ssnnnnnttttt      Vt Rn Rm   Load/Store multiple structures       post-indexed by a register
                             //                                                           Load single structure and replicate  post-indexed by a register
IF_DEF(LS_3G, IS_NONE, NONE) // LS_3G   .Q.........mmmmm ...Sssnnnnnttttt      Vt[] Rn Rm Load/Store single structure          post-indexed by a register

IF_DEF(DI_1A, IS_NONE, NONE) // DI_1A   X.......shiiiiii iiiiiinnnnn.....         Rn    imm(i12,sh)
IF_DEF(DI_1B, IS_NONE, NONE) // DI_1B   X........hwiiiii iiiiiiiiiiiddddd      Rd       imm(i16,hw)
IF_DEF(DI_1C, IS_NONE, NONE) // DI_1C   X........Nrrrrrr ssssssnnnnn.....         Rn    imm(N,r,s)
IF_DEF(DI_1D, IS_NONE, NONE) // DI_1D   X........Nrrrrrr ssssss.....ddddd      Rd       imm(N,r,s)
IF_DEF(DI_1E, IS_NONE, JMP)  // DI_1E   .ii.....iiiiiiii iiiiiiiiiiiddddd      Rd       simm21
IF_DEF(DI_1F, IS_NONE, NONE) // DI_1F   X..........iiiii cccc..nnnnn.nzcv      Rn imm5  nzcv cond

IF_DEF(DI_2A, IS_NONE, NONE) // DI_2A   X.......shiiiiii iiiiiinnnnnddddd      Rd Rn    imm(i12,sh)
IF_DEF(DI_2B, IS_NONE, NONE) // DI_2B   X.........Xnnnnn ssssssnnnnnddddd      Rd Rn    imm(0-63)
IF_DEF(DI_2C, IS_NONE, NONE) // DI_2C   X........Nrrrrrr ssssssnnnnnddddd      Rd Rn    imm(N,r,s)
IF_DEF(DI_2D, IS_NONE, NONE) // DI_2D   X........Nrrrrrr ssssssnnnnnddddd      Rd Rn    imr, imms   (N,r,s)

IF_DEF(DR_1D, IS_NONE, NONE) // DR_1D   X............... cccc.......ddddd      Rd       cond

IF_DEF(DR_2A, IS_NONE, NONE) // DR_2A   X..........mmmmm ......nnnnn.....         Rn Rm
IF_DEF(DR_2B, IS_NONE, NONE) // DR_2B   X.......sh.mmmmm ssssssnnnnn.....         Rn Rm {LSL,LSR,ASR} imm(0-63)
IF_DEF(DR_2C, IS_NONE, NONE) // DR_2C   X..........mmmmm xxxsssnnnnn.....         Rn Rm ext(Rm) LSL imm(0-4)
IF_DEF(DR_2D, IS_NONE, NONE) // DR_2D   X..........nnnnn cccc..nnnnnddddd      Rd Rn    cond
IF_DEF(DR_2E, IS_NONE, NONE) // DR_2E   X..........mmmmm ...........ddddd      Rd    Rm
IF_DEF(DR_2F, IS_NONE, NONE) // DR_2F   X.......sh.mmmmm ssssss.....ddddd      Rd    Rm {LSL,LSR,ASR} imm(0-63)
IF_DEF(DR_2G, IS_NONE, NONE) // DR_2G   X............... ......nnnnnddddd      Rd Rn
IF_DEF(DR_2H, IS_NONE, NONE) // DR_2H   X........X...... ......nnnnnddddd      Rd Rn
IF_DEF(DR_2I, IS_NONE, NONE) // DR_2I   X..........mmmmm cccc..nnnnn.nzcv      Rn Rm    nzcv cond

IF_DEF(DR_3A, IS_NONE, NONE) // DR_3A   X..........mmmmm ......nnnnnddddd      Rd Rn Rm
IF_DEF(DR_3B, IS_NONE, NONE) // DR_3B   X.......sh.mmmmm ssssssnnnnnddddd      Rd Rn Rm {LSL,LSR,ASR} imm(0-63)
IF_DEF(DR_3C, IS_NONE, NONE) // DR_3C   X..........mmmmm xxxsssnnnnnddddd      Rd Rn Rm ext(Rm) LSL imm(0-4)
IF_DEF(DR_3D, IS_NONE, NONE) // DR_3D   X..........mmmmm cccc..nnnnnddddd      Rd Rn Rm cond
IF_DEF(DR_3E, IS_NONE, NONE) // DR_3E   X........X.mmmmm ssssssnnnnnddddd      Rd Rn Rm imm(0-63)

IF_DEF(DR_4A, IS_NONE, NONE) // DR_4A   X..........mmmmm .aaaaannnnnddddd      Rd Rn Rm Ra

IF_DEF(DV_1A, IS_NONE, NONE) // DV_1A   .........X.iiiii iii........ddddd      Vd imm8    (fmov - immediate scalar)
IF_DEF(DV_1B, IS_NONE, NONE) // DV_1B   .QX..........iii jjjj..iiiiiddddd      Vd imm8    (fmov/movi - immediate vector)
IF_DEF(DV_1C, IS_NONE, NONE) // DV_1C   .........X...... ......nnnnn.....      Vn #0.0    (fcmp - with zero)

IF_DEF(DV_2A, IS_NONE, NONE) // DV_2A   .Q.......X...... ......nnnnnddddd      Vd Vn      (fabs, fcvtXX - vector)
IF_DEF(DV_2B, IS_NONE, NONE) // DV_2B   .Q.........iiiii ......nnnnnddddd      Rd Vn[]    (umov/smov    - to general)
IF_DEF(DV_2C, IS_NONE, NONE) // DV_2C   .Q.........iiiii ......nnnnnddddd      Vd Rn      (dup/ins - vector from general)
IF_DEF(DV_2D, IS_NONE, NONE) // DV_2D   .Q.........iiiii ......nnnnnddddd      Vd Vn[]    (dup - vector)
IF_DEF(DV_2E, IS_NONE, NONE) // DV_2E   ...........iiiii ......nnnnnddddd      Vd Vn[]    (dup - scalar)
IF_DEF(DV_2F, IS_NONE, NONE) // DV_2F   ...........iiiii .jjjj.nnnnnddddd      Vd[] Vn[]  (ins - element)
IF_DEF(DV_2G, IS_NONE, NONE) // DV_2G   .........X...... ......nnnnnddddd      Vd Vn      (fmov, fcvtXX - register)
IF_DEF(DV_2H, IS_NONE, NONE) // DV_2H   X........X...... ......nnnnnddddd      Rd Vn      (fmov, fcvtXX - to general)
IF_DEF(DV_2I, IS_NONE, NONE) // DV_2I   X........X...... ......nnnnnddddd      Vd Rn      (fmov, fcvtXX - from general)
IF_DEF(DV_2J, IS_NONE, NONE) // DV_2J   .........d...... D.....nnnnnddddd      Vd Vn      (fcvt)
IF_DEF(DV_2K, IS_NONE, NONE) // DV_2K   .........X.mmmmm ......nnnnn.....      Vn Vm      (fcmp)
IF_DEF(DV_2L, IS_NONE, NONE) // DV_2L   ........XX...... ......nnnnnddddd      Vd Vn      (abs, neg - scalar)
IF_DEF(DV_2M, IS_NONE, NONE) // DV_2M   .Q......XX...... ......nnnnnddddd      Vd Vn      (abs, neg - vector)
IF_DEF(DV_2N, IS_NONE, NONE) // DV_2N   .........iiiiiii ......nnnnnddddd      Vd Vn imm  (shift - scalar)
IF_DEF(DV_2O, IS_NONE, NONE) // DV_2O   .Q.......iiiiiii ......nnnnnddddd      Vd Vn imm  (shift - vector)
IF_DEF(DV_2P, IS_NONE, NONE) // DV_2P   ................ ......nnnnnddddd      Vd Vn      (Vd used as both source and destination)
IF_DEF(DV_2Q, IS_NONE, NONE) // DV_2Q   .........X...... ......nnnnnddddd      Sd Vn      (faddp, fmaxnmp, fmaxp, fminnmp, fminp - scalar)
IF_DEF(DV_2R, IS_NONE, NONE) // DV_2R   .Q.......X...... ......nnnnnddddd      Sd Vn      (fmaxnmv, fmaxv, fminnmv, fminv)
IF_DEF(DV_2S, IS_NONE, NONE) // DV_2S   ........XX...... ......nnnnnddddd      Sd Vn      (addp - scalar)
IF_DEF(DV_2T, IS_NONE, NONE) // DV_2T   .Q......XX...... ......nnnnnddddd      Sd Vn      (addv, saddlv, smaxv, sminv, uaddlv, umaxv, uminv)
IF_DEF(DV_2U, IS_NONE, NONE) // DV_2U   ................ ......nnnnnddddd      Sd Sn      (sha1h)

IF_DEF(DV_3A,  IS_NONE, NONE) // DV_3A  .Q......XX.mmmmm ......nnnnnddddd      Vd Vn Vm   (vector)
IF_DEF(DV_3AI, IS_NONE, NONE) // DV_3AI .Q......XXLMmmmm ....H.nnnnnddddd      Vd Vn Vm[] (vector by element)
IF_DEF(DV_3B,  IS_NONE, NONE) // DV_3B  .Q.......X.mmmmm ......nnnnnddddd      Vd Vn Vm   (vector)
IF_DEF(DV_3BI, IS_NONE, NONE) // DV_3BI .Q.......XLmmmmm ....H.nnnnnddddd      Vd Vn Vm[] (vector by element)
IF_DEF(DV_3C,  IS_NONE, NONE) // DV_3C  .Q.........mmmmm ......nnnnnddddd      Vd Vn Vm   (vector)
IF_DEF(DV_3D,  IS_NONE, NONE) // DV_3D  .........X.mmmmm ......nnnnnddddd      Vd Vn Vm   (scalar)
IF_DEF(DV_3DI, IS_NONE, NONE) // DV_3DI .........XLmmmmm ....H.nnnnnddddd      Vd Vn Vm[] (scalar by element)
IF_DEF(DV_3E,  IS_NONE, NONE) // DV_3E  ........XX.mmmmm ......nnnnnddddd      Vd Vn Vm   (scalar)
IF_DEF(DV_3EI, IS_NONE, NONE) // DV_3EI ........XXLMmmmm ....H.nnnnnddddd      Vd Vn Vm[] (scalar by element)
IF_DEF(DV_3F,  IS_NONE, NONE) // DV_3F  ...........mmmmm ......nnnnnddddd      Qd Sn Vm   (Qd used as both source and destination)
IF_DEF(DV_3G,  IS_NONE, NONE) // DV_3G   .Q.........mmmmm .iiii.nnnnnddddd     Vd Vn Vm imm (vector)

IF_DEF(DV_4A,  IS_NONE, NONE) // DV_4A  .........X.mmmmm .aaaaannnnnddddd      Vd Vn Vm Va (scalar)

IF_DEF(SN_0A, IS_NONE, NONE) // SN_0A   ................ ................
IF_DEF(SI_0A, IS_NONE, NONE) // SI_0A   ...........iiiii iiiiiiiiiii.....               imm16
IF_DEF(SI_0B, IS_NONE, NONE) // SI_0B   ................ ....bbbb........               imm4 - barrier

IF_DEF(SR_1A, IS_NONE, NONE) // SR_1A   ................ ...........ttttt      Rt       (dc zva)

IF_DEF(INVALID, IS_NONE, NONE) //

//////////////////////////////////////////////////////////////////////////////
#undef IF_DEF
//////////////////////////////////////////////////////////////////////////////

#endif // !DEFINE_ID_OPS
//////////////////////////////////////////////////////////////////////////////
// clang-format on
