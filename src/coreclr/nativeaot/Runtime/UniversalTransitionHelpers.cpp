// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include "common.h"
#include "CommonTypes.h"
#include "CommonMacros.h"
#include "PalRedhawkCommon.h"
#include "PalRedhawk.h"

#ifdef _DEBUG
#define TRASH_SAVED_ARGUMENT_REGISTERS
#endif

#ifdef TRASH_SAVED_ARGUMENT_REGISTERS

//
// Define tables of predictable distinguished values that RhpUniversalTransition can use to
// trash argument registers after they have been saved into the transition frame.
//
// Trashing these registers is a testability aid that makes it easier to detect bugs where
// the transition frame content is not correctly propagated to the eventual callee.
//
// In the absence of trashing, such bugs can become undetectable if the code that
// dispatches the call happens to never touch the impacted argument register (e.g., xmm3 on
// amd64 or q5 on arm64). In such a case, the original enregistered argument will flow
// unmodified into the eventual callee, obscuring the fact that the dispatcher failed to
// propagate the transition frame copy of this register.
//
// These tables are manually aligned as a conservative safeguard to ensure that the
// consumers can use arbitrary access widths without ever needing to worry about alignment.
// The comments in each table show the %d/%f renderings of each 32-bit value, plus the
// %I64d/%f rendering of the combined 64-bit value of each aligned pair of 32-bit values.
//

#define TRASH_VALUE_ALIGNMENT 16

EXTERN_C
DECLSPEC_ALIGN(TRASH_VALUE_ALIGNMENT)
const uint32_t RhpIntegerTrashValues[] = {
 // Lo32         Hi32               Lo32       Hi32        Hi32:Lo32
 // -----------  -----------        ---------  ---------   ------------------
    0x07801001U, 0x07802002U,   // (125833217, 125837314) (540467148372316161)
    0x07803003U, 0x07804004U,   // (125841411, 125845508) (540502341334347779)
    0x07805005U, 0x07806006U,   // (125849605, 125853702) (540537534296379397)
    0x07807007U, 0x07808008U,   // (125857799, 125861896) (540572727258411015)
    0x07809009U, 0x0780a00aU,   // (125865993, 125870090) (540607920220442633)
    0x0780b00bU, 0x0780c00cU,   // (125874187, 125878284) (540643113182474251)
    0x0780d00dU, 0x0780e00eU,   // (125882381, 125886478) (540678306144505869)
    0x0780f00fU, 0x07810010U,   // (125890575, 125894672) (540713499106537487)
};

EXTERN_C
DECLSPEC_ALIGN(TRASH_VALUE_ALIGNMENT)
const uint32_t RhpFpTrashValues[] = {
 // Lo32         Hi32               Lo32                 Hi32                  Hi32:Lo32
 // -----------  -----------        -------------------  -------------------   -------------------
    0x42001001U, 0x42002002U,   // (32.0156288146972660, 32.0312576293945310) (8657061952.00781440)
    0x42003003U, 0x42004004U,   // (32.0468864440917970, 32.0625152587890630) (8724187200.02344320)
    0x42005005U, 0x42006006U,   // (32.0781440734863280, 32.0937728881835940) (8791312448.03907200)
    0x42007007U, 0x42008008U,   // (32.1094017028808590, 32.1250305175781250) (8858437696.05470090)
    0x42009009U, 0x4200a00aU,   // (32.1406593322753910, 32.1562881469726560) (8925562944.07032970)
    0x4200b00bU, 0x4200c00cU,   // (32.1719169616699220, 32.1875457763671880) (8992688192.08595850)
    0x4200d00dU, 0x4200e00eU,   // (32.2031745910644530, 32.2188034057617190) (9059813440.10158730)
    0x4200f00fU, 0x42010010U,   // (32.2344322204589840, 32.2500610351562500) (9126938688.11721610)
};

#endif // TRASH_SAVED_ARGUMENT_REGISTERS

