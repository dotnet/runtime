//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//
#if defined(SILVERLIGHT) || defined(FEATURE_CORECLR)
#if defined(FEATURE_CORESYSTEM)
#define asm_rmj              4
#define asm_rmm              0
#define asm_rup              0
#define asm_rpt              0
#else
#define asm_rmj              5
#define asm_rmm              0
#define asm_rup              5
#define asm_rpt              0
#endif
#else
#define asm_rmj              4
#define asm_rmm              0
#define asm_rup              0
#define asm_rpt              0
#endif

