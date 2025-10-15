// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++





--*/

#ifndef __PAL_ASSERT_H__
#define __PAL_ASSERT_H__

#ifdef  __cplusplus
extern "C" {
#endif

#ifndef _ASSERTE
#if defined(_DEBUG)
#define _ASSERTE(e) do {                                        \
        if (!(e)) {                                             \
            fprintf (stderr,                                    \
                     "ASSERT FAILED\n"                          \
                     "\tExpression: %s\n"                       \
                     "\tLocation:   line %d in %s\n"            \
                     "\tFunction:   %s\n"                       \
                     "\tProcess:    %d\n",                      \
                     #e, __LINE__, __FILE__, __FUNCTION__,      \
                     GetCurrentProcessId());                    \
            DebugBreak();                                       \
        }                                                       \
    }while (0)
#else // !DEBUG
#define _ASSERTE(e) ((void)0)
#endif
#endif // _ASSERTE

#ifndef assert
#define assert(e) _ASSERTE(e)
#endif  // assert

#ifdef  __cplusplus
}
#endif

#endif // __PAL_ASSERT_H__
