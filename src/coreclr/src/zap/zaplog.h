//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


/*
 * Hook IfFailThrow calls to do some logging when exceptions are thrown.
 *
 */

#ifndef __ZAPLOG_H__
#define __ZAPLOG_H__

#undef IfFailThrow
#define IfFailThrow(x)                                    \
    do {                                                  \
        HRESULT hrMacro = x;                              \
        if (FAILED(hrMacro)) {                            \
            /* don't embed file names in retail to save space and avoid IP */   \
            /* a findstr /n will allow you to locate it in a pinch */           \
            ThrowAndLog(hrMacro, INDEBUG_COMMA(#x) INDEBUG_COMMA(__FILE__) __LINE__); \
        }                                                 \
    } while(FALSE)

inline void ThrowAndLog(HRESULT hr, INDEBUG_COMMA(__in_z const char * szMsg) INDEBUG_COMMA(__in_z const char * szFile) int lineNum)
{
    WRAPPER_NO_CONTRACT;

    // Log failures when StressLog is on
    static ConfigDWORD g_iStressLog;  
    BOOL bLog = g_iStressLog.val_DontUse_(CLRConfig::UNSUPPORTED_StressLog, 0);
    if (bLog)
    {
#ifdef _DEBUG
        GetSvcLogger()->Printf("IfFailThrow about to throw in file %s line %d, msg = %s, hr: 0x%X\n", szFile, lineNum, szMsg, hr);
#else
        GetSvcLogger()->Printf("IfFailThrow about to throw on line %d.  hr: 0x%X\n", lineNum, hr);
#endif
    }

    ThrowHR(hr);
}
        
#endif // __ZAPLOG_H__
