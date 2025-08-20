// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ===========================================================================
// File: patchedcodeconstants.h
// ===========================================================================

#ifndef PATCHEDCODECONSTANTS_H
#define PATCHEDCODECONSTANTS_H

// These are fixed constants becuase MacOS doesn't allow label arithmetic in
// LDR instructions. Asserts in writebarriermanager CALC_TABLE_LOCATION ensure
// the values are correct.

#define JIT_WriteBarrier_Size					   0x3a0

#ifdef TARGET_WINDOWS
#define JIT_WriteBarrier_Table_Offset              (0x30 + JIT_WriteBarrier_Size)
#else
#define JIT_WriteBarrier_Table_Offset              (0x2c + JIT_WriteBarrier_Size)
#endif

#define JIT_WriteBarrier_Offset_CardTable          (0x0  + JIT_WriteBarrier_Table_Offset)
#define JIT_WriteBarrier_Offset_CardBundleTable    (0x8  + JIT_WriteBarrier_Table_Offset)
#define JIT_WriteBarrier_Offset_WriteWatchTable    (0x10 + JIT_WriteBarrier_Table_Offset)
#define JIT_WriteBarrier_Offset_Lower              (0x18 + JIT_WriteBarrier_Table_Offset)
#define JIT_WriteBarrier_Offset_Upper              (0x20 + JIT_WriteBarrier_Table_Offset)
#define JIT_WriteBarrier_Offset_LowestAddress      (0x28 + JIT_WriteBarrier_Table_Offset)
#define JIT_WriteBarrier_Offset_HighestAddress     (0x30 + JIT_WriteBarrier_Table_Offset)
#define JIT_WriteBarrier_Offset_RegionToGeneration (0x38 + JIT_WriteBarrier_Table_Offset)
#define JIT_WriteBarrier_Offset_RegionShr          (0x40 + JIT_WriteBarrier_Table_Offset)
#define JIT_WriteBarrier_Offset_GCShadow           (0x48 + JIT_WriteBarrier_Table_Offset)
#define JIT_WriteBarrier_Offset_GCShadowEnd        (0x50 + JIT_WriteBarrier_Table_Offset)

#endif // PATCHEDCODECONSTANTS_H