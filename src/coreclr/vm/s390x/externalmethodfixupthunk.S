// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "unixasmmacros.inc"
#include "asmconstants.h"

#ifdef FEATURE_READYTORUN

NESTED_ENTRY DelayLoad_MethodCall, _TEXT, NoHandler
// NOT YET IMPLEMENTED
.long 0x21

PATCH_LABEL ExternalMethodFixupPatchLabel

NESTED_END DelayLoad_MethodCall, _TEXT

//============================================================================================

.macro DYNAMICHELPER frameFlags, suffix

NESTED_ENTRY DelayLoad_Helper\suffix, _TEXT, NoHandler
// NOT YET IMPLEMENTED
.long 0x22

NESTED_END DelayLoad_Helper\suffix, _TEXT

    .endm

DYNAMICHELPER DynamicHelperFrameFlags_Default
DYNAMICHELPER DynamicHelperFrameFlags_ObjectArg, _Obj
DYNAMICHELPER (DynamicHelperFrameFlags_ObjectArg | DynamicHelperFrameFlags_ObjectArg2), _ObjObj

#endif // FEATURE_READYTORUN
