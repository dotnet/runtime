// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#ifndef JMP_SMALL
#error Must define JMP_SMALL macro before including this file
#endif

#if defined(_TARGET_XARCH_)

//       jump   reverse instruction
JMP_SMALL(jmp   , jmp   , jmp    )
JMP_SMALL(jo    , jno   , jo     )
JMP_SMALL(jno   , jo    , jno    )
JMP_SMALL(jb    , jae   , jb     )
JMP_SMALL(jae   , jb    , jae    )
JMP_SMALL(je    , jne   , je     )
JMP_SMALL(jne   , je    , jne    )
JMP_SMALL(jbe   , ja    , jbe    )
JMP_SMALL(ja    , jbe   , ja     )
JMP_SMALL(js    , jns   , js     )
JMP_SMALL(jns   , js    , jns    )
JMP_SMALL(jpe   , jpo   , jpe    )
JMP_SMALL(jpo   , jpe   , jpo    )
JMP_SMALL(jl    , jge   , jl     )
JMP_SMALL(jge   , jl    , jge    )
JMP_SMALL(jle   , jg    , jle    )
JMP_SMALL(jg    , jle   , jg     )

#elif defined(_TARGET_ARM_)

//       jump   reverse instruction condcode
JMP_SMALL(jmp   , jmp   , b         , 15    )  // illegal condcode
JMP_SMALL(jo    , jno   , bvs       , 6     )  // VS
JMP_SMALL(jno   , jo    , bvc       , 7     )  // VC
JMP_SMALL(jb    , jae   , blo       , 3     )  // LO also CC
JMP_SMALL(jae   , jb    , bhs       , 2     )  // HS also CS
JMP_SMALL(je    , jne   , beq       , 0     )  // EQ
JMP_SMALL(jne   , je    , bne       , 1     )  // NE
JMP_SMALL(jbe   , ja    , bls       , 9     )  // LS
JMP_SMALL(ja    , jbe   , bhi       , 8     )  // HI
JMP_SMALL(js    , jns   , bmi       , 4     )  // MI
JMP_SMALL(jns   , js    , bpl       , 5     )  // PL
JMP_SMALL(jl    , jge   , blt       , 11    )  // LT
JMP_SMALL(jge   , jl    , bge       , 10    )  // GE
JMP_SMALL(jle   , jg    , ble       , 13    )  // LE
JMP_SMALL(jg    , jle   , bgt       , 12    )  // GT

#elif defined(_TARGET_ARM64_)

//       jump   reverse instruction condcode
JMP_SMALL(jmp   , jmp   , b         , 15    )  // illegal condcode
JMP_SMALL(jo    , jno   , bvs       , 6     )  // VS
JMP_SMALL(jno   , jo    , bvc       , 7     )  // VC
JMP_SMALL(jb    , jae   , blo       , 3     )  // LO also CC
JMP_SMALL(jae   , jb    , bhs       , 2     )  // HS also CS
JMP_SMALL(je    , jne   , beq       , 0     )  // EQ
JMP_SMALL(jne   , je    , bne       , 1     )  // NE
JMP_SMALL(jbe   , ja    , bls       , 9     )  // LS
JMP_SMALL(ja    , jbe   , bhi       , 8     )  // HI
JMP_SMALL(js    , jns   , bmi       , 4     )  // MI
JMP_SMALL(jns   , js    , bpl       , 5     )  // PL
JMP_SMALL(jl    , jge   , blt       , 11    )  // LT
JMP_SMALL(jge   , jl    , bge       , 10    )  // GE
JMP_SMALL(jle   , jg    , ble       , 13    )  // LE
JMP_SMALL(jg    , jle   , bgt       , 12    )  // GT

#else
  #error Unsupported or unset target architecture
#endif // target type

/*****************************************************************************/
#undef JMP_SMALL
/*****************************************************************************/
