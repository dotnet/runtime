// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#if defined(SILVERLIGHT)
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

