// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// clang-format off
#ifndef JMP_SMALL
#error Must define JMP_SMALL macro before including this file
#endif

#if defined(TARGET_XARCH)

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
JMP_SMALL(jp    , jnp   , jp     )
JMP_SMALL(jnp   , jp    , jnp    )
JMP_SMALL(jl    , jge   , jl     )
JMP_SMALL(jge   , jl    , jge    )
JMP_SMALL(jle   , jg    , jle    )
JMP_SMALL(jg    , jle   , jg     )

#elif defined(TARGET_ARMARCH)

//       jump   reverse instruction condcode
JMP_SMALL(jmp   , jmp   , b      )  // AL always
JMP_SMALL(eq    , ne    , beq    )  // EQ
JMP_SMALL(ne    , eq    , bne    )  // NE
JMP_SMALL(hs    , lo    , bhs    )  // HS also CS
JMP_SMALL(lo    , hs    , blo    )  // LO also CC
JMP_SMALL(mi    , pl    , bmi    )  // MI
JMP_SMALL(pl    , mi    , bpl    )  // PL
JMP_SMALL(vs    , vc    , bvs    )  // VS
JMP_SMALL(vc    , vs    , bvc    )  // VC
JMP_SMALL(hi    , ls    , bhi    )  // HI
JMP_SMALL(ls    , hi    , bls    )  // LS
JMP_SMALL(ge    , lt    , bge    )  // GE
JMP_SMALL(lt    , ge    , blt    )  // LT
JMP_SMALL(gt    , le    , bgt    )  // GT
JMP_SMALL(le    , gt    , ble    )  // LE

#elif defined(TARGET_LOONGARCH64)

// TODO-LOONGARCH64: adding other condition branches.
JMP_SMALL(jmp   , jmp   , b      )
JMP_SMALL(eq    , ne    , beq    )  // EQ
JMP_SMALL(ne    , eq    , bne    )  // NE

#else
  #error Unsupported or unset target architecture
#endif // target type

/*****************************************************************************/
#undef JMP_SMALL
/*****************************************************************************/

// clang-format on
