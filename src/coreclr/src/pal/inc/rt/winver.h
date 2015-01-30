//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//

//
// ===========================================================================
// File: winver.h
// 
// ===========================================================================
// winver.h for PAL
// Included in .rc files.

#define VS_VERSION_INFO     1
#define VS_FFI_FILEFLAGSMASK    0x0000003FL

#define VS_FF_DEBUG             0x00000001L
#define VS_FF_PRERELEASE        0x00000002L
#define VS_FF_PATCHED           0x00000004L
#define VS_FF_PRIVATEBUILD      0x00000008L
#define VS_FF_INFOINFERRED      0x00000010L
#define VS_FF_SPECIALBUILD      0x00000020L

#define VFT_UNKNOWN             0x00000000L
#define VFT_APP                 0x00000001L
#define VFT_DLL                 0x00000002L

#define VFT2_UNKNOWN            0x00000000L

#define VOS__WINDOWS32          0x00000004L
