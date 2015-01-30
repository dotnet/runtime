//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*++



Module Name:

    pal_safecrt.h

Abstract:

Wrapper for including SafeCRT for Mac build of CoreCLR



--*/

#ifndef _PAL_SAFECRT_H_
#define _PAL_SAFECRT_H_

#define _CRT_ALTERNATIVE_INLINES
#define _SAFECRT_NO_INCLUDES 1

#if !defined (_SAFECRT_USE_INLINES)
#define _SAFECRT_USE_INLINES 0
#endif

#if !defined (_SAFECRT_IMPL)
#define _SAFECRT_IMPL 0
#endif

#define _SAFECRT_SET_ERRNO 0
#define _SAFECRT_DEFINE_MBS_FUNCTIONS 0
#define _SAFECRT_DEFINE_TCS_MACROS 1
//#define _SAFECRT_INVALID_PARAMETER(message) WARN(message "\n")

#if defined (SAFECRT_IN_PAL)

#define DUMMY_memset void * __cdecl memset(void *, int, size_t);

#endif

// Include the safecrt implementation
#include "../../palrt/inc/safecrt.h"

#if defined(SAFECRT_IN_PAL)

#define DUMMY_memset

#endif

#endif // _PAL_SAFECRT_H_
