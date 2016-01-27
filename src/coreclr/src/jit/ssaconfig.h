// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ==++==
//

//

//
// ==--==

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                                  SSA                                      XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#pragma once


#ifdef DEBUG
    #define DBG_SSA_JITDUMP(...) if (GetTlsCompiler()->verboseSsa) JitDump(__VA_ARGS__)
#else
    #define DBG_SSA_JITDUMP(...)
#endif

// DBG_SSA_JITDUMP prints only if DEBUG, DEBUG_SSA, and tlsCompiler->verbose are all set.

namespace SsaConfig
{
    // FIRST ssa num is given to the first definition of a variable which can either be:
    // 1. A regular definition in the program.
    // 2. Or initialization by compInitMem.
    static const int FIRST_SSA_NUM = 2;

    // UNINIT ssa num is given to variables whose definitions were never encountered:
    // 1. Neither by SsaBuilder
    // 2. Nor were they initialized using compInitMem.
    static const int UNINIT_SSA_NUM = 1;

    // Sentinel value to indicate variable not touched by SSA.
    static const int RESERVED_SSA_NUM = 0;

} // end of namespace SsaConfig
