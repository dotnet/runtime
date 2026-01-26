// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Common defines set based on the target-specific ones in target<target>.h files.
//

// clang-format off
#ifndef CNT_CALLEE_SAVED_FOR_CSE
#define CNT_CALLEE_SAVED_FOR_CSE CNT_CALLEE_SAVED
#endif
#ifndef CNT_CALLEE_TRASH_FOR_CSE
#define CNT_CALLEE_TRASH_FOR_CSE CNT_CALLEE_TRASH
#endif
#ifndef CNT_CALLEE_ENREG_FOR_CSE
#define CNT_CALLEE_ENREG_FOR_CSE CNT_CALLEE_ENREG
#endif

#ifndef CNT_CALLEE_SAVED_FLOAT_FOR_CSE
#define CNT_CALLEE_SAVED_FLOAT_FOR_CSE CNT_CALLEE_SAVED_FLOAT
#endif
#ifndef CNT_CALLEE_TRASH_FLOAT_FOR_CSE
#define CNT_CALLEE_TRASH_FLOAT_FOR_CSE CNT_CALLEE_TRASH_FLOAT
#endif
#ifndef CNT_CALLEE_ENREG_FLOAT_FOR_CSE
#define CNT_CALLEE_ENREG_FLOAT_FOR_CSE CNT_CALLEE_ENREG_FLOAT
#endif

#ifndef CNT_CALLEE_SAVED_MASK_FOR_CSE
#define CNT_CALLEE_SAVED_MASK_FOR_CSE CNT_CALLEE_SAVED_MASK
#endif
#ifndef CNT_CALLEE_TRASH_MASK_FOR_CSE
#define CNT_CALLEE_TRASH_MASK_FOR_CSE CNT_CALLEE_TRASH_MASK
#endif
#ifndef CNT_CALLEE_ENREG_MASK_FOR_CSE
#define CNT_CALLEE_ENREG_MASK_FOR_CSE CNT_CALLEE_ENREG_MASK
#endif
// clang-format on
