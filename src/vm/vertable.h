// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*
 * Types for pop stack/push stack
 * ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
 * 1  I1/U1
 * 2  I2/U2
 * 4  I4/U4
 * 8  I8/U8
 * r  R4
 * d  R8
 * o  objref (can be an array or null)
 * [  single dimensional array of (prefix)
 * &  byref/managed ptr (prefix)
 *
 * Pop only
 * ~~~~~~~~
 * R  real number
 * N  number -any integer or real number
 * Q  number or unmanaged pointer
 * X  number, unmanaged pointer, managed pointer, or objref [Obsolete]
 * Y  integer (I1..I4), unmanaged pointer, managed pointer, or objref
 * I  Integral type (1, 2, 4, or 8 byte, or platform-independent integer type)
 * A  Anything
 *
 * CE "ceq" semantics - pop 2 arguments, do type checking as if for "ceq" instruction:
 *                      Integer     Real     ManagedPtr     UnmanagedPtr   Objref
 *       Integer           y               
 *       Real                        y
 *       ManagedPtr                             y                             y
 *       UnmanagedPtr                                            y
 *       Objref                                 y                             y
 *
 * CG "cgt" semantics - pop 2 arguments, do type checking as if for "cgt" instruction:
 *                      Integer     Real     ManagedPtr     UnmanagedPtr   Objref
 *       Integer           y               
 *       Real                        y
 *       ManagedPtr                                                           y
 *       UnmanagedPtr                                            
 *       Objref                                 y                             
 * 
 * =  Pop another item off the stack, and it must be the same type (int,real,objref,etc.) as the 
 *    last item popped (note, System.Int32 <-> I4 etc. are allowed).  Other value 
 *    classes are NOT allowed.
 *
 * i  (deprecated) Platform independent size value, but NOT an objref (I4/R4/ptr on 32-bit, I8/R8/ptr on 64-bit)
 * p  (deprecated) Platform independent size value OR objref 
 * *  (deprecated) anything

 * Push only
 * ~~~~~~~~~
 * n  null objref (valid for push only)
 * -  Rewind the stack to undo the last pop (you cannot have trashed that location, though)
 *
 * Usage: <pop stack> : <operand checks> <push stack> <branches> <!>
 *
 * Order is important!  Operand checks come after pop stack and before push stack.
 * For example, to check the operand being a valid local variable number (only), do ":L"
 *
 * If there is a "!" at the end, it means the instruction is either invalid, not supported, or that
 * there is a case statement to handle the instruction.  If no case statement exists, the verifier
 * will fail verification of the method.
 *
 * ! can be used to perform some operand checks and/or stack pops/pushes, while still allowing specific
 * behavior; e.g. verifying that the inline operand is a valid local variable number.
 *
 * <operand checks>
 * ~~~~~~~~~~~~~~~~
 * #d Overwrite inline operand with digit "d" (must be in 0...9 range)
 * L  Check that the operand is a valid local variable number.
 * A  Check that the operand is a valid argument number.
 *
 * <branches>
 * ~~~~~~~~~~
 * b1 - one byte conditional branch
 * b4 - four byte conditional branch
 * u1 - one byte unconditional branch
 * u4 - four byte unconditional branch
 * l1 - one byte leave
 * l4 - one byte leave
 *
 */

VEROPCODE(CEE_NOP,                      ":")
VEROPCODE(CEE_BREAK,                    ":")
VEROPCODE(CEE_LDARG_0,                  ":#0A!")
VEROPCODE(CEE_LDARG_1,                  ":#1A!")
VEROPCODE(CEE_LDARG_2,                  ":#2A!")
VEROPCODE(CEE_LDARG_3,                  ":#3A!")
VEROPCODE(CEE_LDLOC_0,                  ":#0L!")
VEROPCODE(CEE_LDLOC_1,                  ":#1L!")
VEROPCODE(CEE_LDLOC_2,                  ":#2L!")
VEROPCODE(CEE_LDLOC_3,                  ":#3L!")
VEROPCODE(CEE_STLOC_0,                  ":#0L!")
VEROPCODE(CEE_STLOC_1,                  ":#1L!")
VEROPCODE(CEE_STLOC_2,                  ":#2L!")
VEROPCODE(CEE_STLOC_3,                  ":#3L!")
VEROPCODE(CEE_LDARG_S,                  ":A!")
VEROPCODE(CEE_LDARGA_S,                 ":A!")
VEROPCODE(CEE_STARG_S,                  ":A!")
VEROPCODE(CEE_LDLOC_S,                  ":L!")
VEROPCODE(CEE_LDLOCA_S,                 ":L!")
VEROPCODE(CEE_STLOC_S,                  ":L!")
VEROPCODE(CEE_LDNULL,                   ":n")
VEROPCODE(CEE_LDC_I4_M1,                ":4")
VEROPCODE(CEE_LDC_I4_0,                 ":4")
VEROPCODE(CEE_LDC_I4_1,                 ":4")
VEROPCODE(CEE_LDC_I4_2,                 ":4")
VEROPCODE(CEE_LDC_I4_3,                 ":4")
VEROPCODE(CEE_LDC_I4_4,                 ":4")
VEROPCODE(CEE_LDC_I4_5,                 ":4")
VEROPCODE(CEE_LDC_I4_6,                 ":4")
VEROPCODE(CEE_LDC_I4_7,                 ":4")
VEROPCODE(CEE_LDC_I4_8,                 ":4")
VEROPCODE(CEE_LDC_I4_S,                 ":4")
VEROPCODE(CEE_LDC_I4,                   ":4")
VEROPCODE(CEE_LDC_I8,                   ":8")
VEROPCODE(CEE_LDC_R4,                   ":r")
VEROPCODE(CEE_LDC_R8,                   ":d")
VEROPCODE(CEE_UNUSED49,                 "!") 
VEROPCODE(CEE_DUP,                      "!")
VEROPCODE(CEE_POP,                      "A:")
VEROPCODE(CEE_JMP,                      "!")            // Unverifiable !
VEROPCODE(CEE_CALL,                     "!")
VEROPCODE(CEE_CALLI,                    "!")
VEROPCODE(CEE_RET,                      "!")
VEROPCODE(CEE_BR_S,                     ":u1")
VEROPCODE(CEE_BRFALSE_S,                "Y:b1")
VEROPCODE(CEE_BRTRUE_S,                 "Y:b1")
VEROPCODE(CEE_BEQ_S,                    "CE:b1")
VEROPCODE(CEE_BGE_S,                    "CG:b1")
VEROPCODE(CEE_BGT_S,                    "CG:b1")
VEROPCODE(CEE_BLE_S,                    "CG:b1")
VEROPCODE(CEE_BLT_S,                    "CG:b1")
VEROPCODE(CEE_BNE_UN_S,                 "CE:b1")
VEROPCODE(CEE_BGE_UN_S,                 "CG:b1")
VEROPCODE(CEE_BGT_UN_S,                 "CG:b1")
VEROPCODE(CEE_BLE_UN_S,                 "CG:b1")
VEROPCODE(CEE_BLT_UN_S,                 "CG:b1")
VEROPCODE(CEE_BR,                       ":u4")
VEROPCODE(CEE_BRFALSE,                  "Y:b4")
VEROPCODE(CEE_BRTRUE,                   "Y:b4")
VEROPCODE(CEE_BEQ,                      "CE:b4")
VEROPCODE(CEE_BGE,                      "CG:b4")
VEROPCODE(CEE_BGT,                      "CG:b4")
VEROPCODE(CEE_BLE,                      "CG:b4")
VEROPCODE(CEE_BLT,                      "CG:b4")
VEROPCODE(CEE_BNE_UN,                   "CE:b4")
VEROPCODE(CEE_BGE_UN,                   "CG:b4")
VEROPCODE(CEE_BGT_UN,                   "CG:b4")
VEROPCODE(CEE_BLE_UN,                   "CG:b4")
VEROPCODE(CEE_BLT_UN,                   "CG:b4")
VEROPCODE(CEE_SWITCH,                   "!")
VEROPCODE(CEE_LDIND_I1,                 "&1:4")
VEROPCODE(CEE_LDIND_U1,                 "&1:4")
VEROPCODE(CEE_LDIND_I2,                 "&2:4")
VEROPCODE(CEE_LDIND_U2,                 "&2:4")
VEROPCODE(CEE_LDIND_I4,                 "&4:4")
VEROPCODE(CEE_LDIND_U4,                 "&4:4")
VEROPCODE(CEE_LDIND_I8,                 "&8:8")
VEROPCODE(CEE_LDIND_I,                  "&i:i") // <TODO> not correct on 64 bit</TODO>
VEROPCODE(CEE_LDIND_R4,                 "&r:r")
VEROPCODE(CEE_LDIND_R8,                 "&d:d")
VEROPCODE(CEE_LDIND_REF,                "!")
VEROPCODE(CEE_STIND_REF,                "!")
VEROPCODE(CEE_STIND_I1,                 "4&1:")
VEROPCODE(CEE_STIND_I2,                 "4&2:")
VEROPCODE(CEE_STIND_I4,                 "4&4:")
VEROPCODE(CEE_STIND_I8,                 "8&8:")
VEROPCODE(CEE_STIND_R4,                 "r&r:")
VEROPCODE(CEE_STIND_R8,                 "d&d:")
VEROPCODE(CEE_ADD,                      "N=:-")
VEROPCODE(CEE_SUB,                      "N=:-")
VEROPCODE(CEE_MUL,                      "N=:-")
VEROPCODE(CEE_DIV,                      "N=:-")
VEROPCODE(CEE_DIV_UN,                   "I=:-")
VEROPCODE(CEE_REM,                      "N=:-")
VEROPCODE(CEE_REM_UN,                   "I=:-")
VEROPCODE(CEE_AND,                      "I=:-")
VEROPCODE(CEE_OR,                       "I=:-")
VEROPCODE(CEE_XOR,                      "I=:-")
VEROPCODE(CEE_SHL,                      "4I:-")
VEROPCODE(CEE_SHR,                      "4I:-")
VEROPCODE(CEE_SHR_UN,                   "4I:-")
VEROPCODE(CEE_NEG,                      "N:-")
VEROPCODE(CEE_NOT,                      "I:-")
VEROPCODE(CEE_CONV_I1,                  "Q:4")
VEROPCODE(CEE_CONV_I2,                  "Q:4")
VEROPCODE(CEE_CONV_I4,                  "Q:4")
VEROPCODE(CEE_CONV_I8,                  "Q:8")
VEROPCODE(CEE_CONV_R4,                  "N:r")
VEROPCODE(CEE_CONV_R8,                  "N:d")
VEROPCODE(CEE_CONV_U4,                  "Q:4")
VEROPCODE(CEE_CONV_U8,                  "Q:8")
VEROPCODE(CEE_CALLVIRT,                 "!")
VEROPCODE(CEE_CPOBJ,                    "!")
VEROPCODE(CEE_LDOBJ,                    "!")
VEROPCODE(CEE_LDSTR,                    "!")
VEROPCODE(CEE_NEWOBJ,                   "!")
VEROPCODE(CEE_CASTCLASS,                "!")
VEROPCODE(CEE_ISINST,                   "!")
VEROPCODE(CEE_CONV_R_UN,                "Q:r")
VEROPCODE(CEE_UNUSED58,                 "!")
VEROPCODE(CEE_UNUSED1,                  "!")
VEROPCODE(CEE_UNBOX,                    "!")
VEROPCODE(CEE_THROW,                    "!")
VEROPCODE(CEE_LDFLD,                    "!")
VEROPCODE(CEE_LDFLDA,                   "!")
VEROPCODE(CEE_STFLD,                    "!")
VEROPCODE(CEE_LDSFLD,                   "!")
VEROPCODE(CEE_LDSFLDA,                  "!")
VEROPCODE(CEE_STSFLD,                   "!")
VEROPCODE(CEE_STOBJ,                    "!")
VEROPCODE(CEE_CONV_OVF_I1_UN,           "Q:4")
VEROPCODE(CEE_CONV_OVF_I2_UN,           "Q:4")
VEROPCODE(CEE_CONV_OVF_I4_UN,           "Q:4")
VEROPCODE(CEE_CONV_OVF_I8_UN,           "Q:8")
VEROPCODE(CEE_CONV_OVF_U1_UN,           "Q:4")
VEROPCODE(CEE_CONV_OVF_U2_UN,           "Q:4")
VEROPCODE(CEE_CONV_OVF_U4_UN,           "Q:4")
VEROPCODE(CEE_CONV_OVF_U8_UN,           "Q:8")
VEROPCODE(CEE_CONV_OVF_I_UN,            "Q:i")
VEROPCODE(CEE_CONV_OVF_U_UN,            "Q:i")
VEROPCODE(CEE_BOX,                      "!")
VEROPCODE(CEE_NEWARR,                   "!")
VEROPCODE(CEE_LDLEN,                    "[*:4")
VEROPCODE(CEE_LDELEMA,                  "!")
VEROPCODE(CEE_LDELEM_I1,                "4[1:4")
VEROPCODE(CEE_LDELEM_U1,                "4[1:4")
VEROPCODE(CEE_LDELEM_I2,                "4[2:4")
VEROPCODE(CEE_LDELEM_U2,                "4[2:4")
VEROPCODE(CEE_LDELEM_I4,                "4[4:4")
VEROPCODE(CEE_LDELEM_U4,                "4[4:4")
VEROPCODE(CEE_LDELEM_I8,                "4[8:8")
VEROPCODE(CEE_LDELEM_I,                 "4[i:i")
VEROPCODE(CEE_LDELEM_R4,                "4[r:r")
VEROPCODE(CEE_LDELEM_R8,                "4[d:d")
VEROPCODE(CEE_LDELEM_REF,               "!")
VEROPCODE(CEE_STELEM_I,                 "i4[i:")
VEROPCODE(CEE_STELEM_I1,                "44[1:")
VEROPCODE(CEE_STELEM_I2,                "44[2:")
VEROPCODE(CEE_STELEM_I4,                "44[4:")
VEROPCODE(CEE_STELEM_I8,                "84[8:")
VEROPCODE(CEE_STELEM_R4,                "r4[r:")
VEROPCODE(CEE_STELEM_R8,                "d4[d:")
VEROPCODE(CEE_STELEM_REF,               "!")
VEROPCODE(CEE_LDELEM,               "!")
VEROPCODE(CEE_STELEM,               "!")
VEROPCODE(CEE_UNBOX_ANY,                "!")
VEROPCODE(CEE_UNUSED5,                  "!")
VEROPCODE(CEE_UNUSED6,                  "!")
VEROPCODE(CEE_UNUSED7,                  "!")
VEROPCODE(CEE_UNUSED8,                  "!")
VEROPCODE(CEE_UNUSED9,                  "!")
VEROPCODE(CEE_UNUSED10,                 "!")
VEROPCODE(CEE_UNUSED11,                 "!")
VEROPCODE(CEE_UNUSED12,                 "!")
VEROPCODE(CEE_UNUSED13,                 "!")
VEROPCODE(CEE_UNUSED14,                 "!")
VEROPCODE(CEE_UNUSED15,                 "!")
VEROPCODE(CEE_UNUSED16,                 "!")
VEROPCODE(CEE_UNUSED17,                 "!")
VEROPCODE(CEE_CONV_OVF_I1,              "Q:4")
VEROPCODE(CEE_CONV_OVF_U1,              "Q:4")
VEROPCODE(CEE_CONV_OVF_I2,              "Q:4")
VEROPCODE(CEE_CONV_OVF_U2,              "Q:4")
VEROPCODE(CEE_CONV_OVF_I4,              "Q:4")
VEROPCODE(CEE_CONV_OVF_U4,              "Q:4")
VEROPCODE(CEE_CONV_OVF_I8,              "Q:8")
VEROPCODE(CEE_CONV_OVF_U8,              "Q:8")
VEROPCODE(CEE_UNUSED50,                 "!")
VEROPCODE(CEE_UNUSED18,                 "!")
VEROPCODE(CEE_UNUSED19,                 "!")
VEROPCODE(CEE_UNUSED20,                 "!")
VEROPCODE(CEE_UNUSED21,                 "!")
VEROPCODE(CEE_UNUSED22,                 "!")
VEROPCODE(CEE_UNUSED23,                 "!")
VEROPCODE(CEE_REFANYVAL,                "!")
VEROPCODE(CEE_CKFINITE,                 "R:-")
VEROPCODE(CEE_UNUSED24,                 "!")
VEROPCODE(CEE_UNUSED25,                 "!")
VEROPCODE(CEE_MKREFANY,                 "!")
VEROPCODE(CEE_UNUSED59,                 "!")
VEROPCODE(CEE_UNUSED60,                 "!")
VEROPCODE(CEE_UNUSED61,                 "!")
VEROPCODE(CEE_UNUSED62,                 "!")
VEROPCODE(CEE_UNUSED63,                 "!")
VEROPCODE(CEE_UNUSED64,                 "!")
VEROPCODE(CEE_UNUSED65,                 "!")
VEROPCODE(CEE_UNUSED66,                 "!")
VEROPCODE(CEE_UNUSED67,                 "!")
VEROPCODE(CEE_LDTOKEN,                  "!")
VEROPCODE(CEE_CONV_U2,                  "Q:4")
VEROPCODE(CEE_CONV_U1,                  "Q:4")
VEROPCODE(CEE_CONV_I,                   "Q:i")
VEROPCODE(CEE_CONV_OVF_I,               "Q:i")
VEROPCODE(CEE_CONV_OVF_U,               "Q:i")
VEROPCODE(CEE_ADD_OVF,                  "I=:-")
VEROPCODE(CEE_ADD_OVF_UN,               "I=:-")
VEROPCODE(CEE_MUL_OVF,                  "I=:-")
VEROPCODE(CEE_MUL_OVF_UN,               "I=:-")
VEROPCODE(CEE_SUB_OVF,                  "I=:-")
VEROPCODE(CEE_SUB_OVF_UN,               "I=:-")
VEROPCODE(CEE_ENDFINALLY,               "!")
VEROPCODE(CEE_LEAVE,                    ":l4")
VEROPCODE(CEE_LEAVE_S,                  ":l1")
VEROPCODE(CEE_STIND_I,                  "i&i:") // <TODO> : 64 bit</TODO>
VEROPCODE(CEE_CONV_U,                   "Q:i")
VEROPCODE(CEE_UNUSED26,                 "!")
VEROPCODE(CEE_UNUSED27,                 "!")
VEROPCODE(CEE_UNUSED28,                 "!")
VEROPCODE(CEE_UNUSED29,                 "!")
VEROPCODE(CEE_UNUSED30,                 "!")
VEROPCODE(CEE_UNUSED31,                 "!")
VEROPCODE(CEE_UNUSED32,                 "!")
VEROPCODE(CEE_UNUSED33,                 "!")
VEROPCODE(CEE_UNUSED34,                 "!")
VEROPCODE(CEE_UNUSED35,                 "!")
VEROPCODE(CEE_UNUSED36,                 "!")
VEROPCODE(CEE_UNUSED37,                 "!")
VEROPCODE(CEE_UNUSED38,                 "!")
VEROPCODE(CEE_UNUSED39,                 "!")
VEROPCODE(CEE_UNUSED40,                 "!")
VEROPCODE(CEE_UNUSED41,                 "!")
VEROPCODE(CEE_UNUSED42,                 "!")
VEROPCODE(CEE_UNUSED43,                 "!")
VEROPCODE(CEE_UNUSED44,                 "!")
VEROPCODE(CEE_UNUSED45,                 "!")
VEROPCODE(CEE_UNUSED46,                 "!")
VEROPCODE(CEE_UNUSED47,                 "!")
VEROPCODE(CEE_UNUSED48,                 "!")
VEROPCODE(CEE_PREFIX7,                  "!")
VEROPCODE(CEE_PREFIX6,                  "!")
VEROPCODE(CEE_PREFIX5,                  "!")
VEROPCODE(CEE_PREFIX4,                  "!")
VEROPCODE(CEE_PREFIX3,                  "!")
VEROPCODE(CEE_PREFIX2,                  "!")
VEROPCODE(CEE_PREFIX1,                  "!")
VEROPCODE(CEE_PREFIXREF,                "!")
VEROPCODE(CEE_ARGLIST,                  "!")
VEROPCODE(CEE_CEQ,                      "CE:4")
VEROPCODE(CEE_CGT,                      "CG:4")
VEROPCODE(CEE_CGT_UN,                   "CE:4")
VEROPCODE(CEE_CLT,                      "CG:4")
VEROPCODE(CEE_CLT_UN,                   "CG:4")
VEROPCODE(CEE_LDFTN,                    "!")
VEROPCODE(CEE_LDVIRTFTN,                "!")
VEROPCODE(CEE_UNUSED56,                 "!") 
VEROPCODE(CEE_LDARG,                    ":A!")
VEROPCODE(CEE_LDARGA,                   ":A!")
VEROPCODE(CEE_STARG,                    ":A!")
VEROPCODE(CEE_LDLOC,                    ":L!")
VEROPCODE(CEE_LDLOCA,                   ":L!")
VEROPCODE(CEE_STLOC,                    ":L!")
VEROPCODE(CEE_LOCALLOC,                 "i:i!")     // Unverifiable !
VEROPCODE(CEE_UNUSED57,                 "!")
VEROPCODE(CEE_ENDFILTER,                "4:!")
VEROPCODE(CEE_UNALIGNED,                ":")
VEROPCODE(CEE_VOLATILE,                 ":")
VEROPCODE(CEE_TAILCALL,                 ":")
VEROPCODE(CEE_INITOBJ,                  "!")
VEROPCODE(CEE_CONSTRAINED,               ":")
VEROPCODE(CEE_CPBLK,                    "ii4:!")    // Unverifiable !
VEROPCODE(CEE_INITBLK,                  "i44:!")    // Unverifiable !
VEROPCODE(CEE_UNUSED69,                 "!")
VEROPCODE(CEE_RETHROW,                  "!")
VEROPCODE(CEE_UNUSED51,                 "!")
VEROPCODE(CEE_SIZEOF,                   "!")
VEROPCODE(CEE_REFANYTYPE,               "!")
VEROPCODE(CEE_READONLY,                 ":")
VEROPCODE(CEE_UNUSED53,                "!")
VEROPCODE(CEE_UNUSED54,                 "!")
VEROPCODE(CEE_UNUSED55,                 "!")
VEROPCODE(CEE_UNUSED70,                 "!")
VEROPCODE(CEE_ILLEGAL,                  "!")
VEROPCODE(CEE_MACRO_END,                "!")
VEROPCODE(CEE_COUNT,            		"!")

