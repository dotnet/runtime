// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// clang-format off
//  memberName - enum member name
//  memberValue - enum member value
//  shortName - short name string
//  DEF_REFTYPE(memberName                , memberValue        , shortName )
    DEF_REFTYPE(RefTypeInvalid            , 0x00               , "Invl"    )
    DEF_REFTYPE(RefTypeDef                , 0x01               , "Def "    )
    DEF_REFTYPE(RefTypeUse                , 0x02               , "Use "    )
    DEF_REFTYPE(RefTypeKill               , 0x04               , "Kill"    )
    DEF_REFTYPE(RefTypeBB                 , 0x08               , "BB  "    )
    DEF_REFTYPE(RefTypeFixedReg           , 0x10               , "Fixd"    )
    DEF_REFTYPE(RefTypeExpUse             , (0x20 | RefTypeUse), "ExpU"    )
    DEF_REFTYPE(RefTypeParamDef           , (0x10 | RefTypeDef), "Parm"    )
    DEF_REFTYPE(RefTypeDummyDef           , (0x20 | RefTypeDef), "DDef"    )
    DEF_REFTYPE(RefTypeZeroInit           , (0x30 | RefTypeDef), "Zero"    )
    DEF_REFTYPE(RefTypeUpperVectorSave    , (0x40 | RefTypeDef), "UVSv"    )
    DEF_REFTYPE(RefTypeUpperVectorRestore , (0x40 | RefTypeUse), "UVRs"    )
    DEF_REFTYPE(RefTypeKillGCRefs         , 0x80               , "KlGC"    )
// clang-format on
