// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//----------------------------------------------------------------------------- 

//----------------------------------------------------------------------------- 

#ifndef __STACK_TRACE_H__
#define __STACK_TRACE_H__

HINSTANCE LoadImageHlp();
HINSTANCE LoadDbgHelp();

#include <specstrings.h>

//
//--- Constants ---------------------------------------------------------------
//

#define cchMaxAssertModuleLen 60
#define cchMaxAssertSymbolLen 257
#define cfrMaxAssertStackLevels 20
#define cchMaxAssertExprLen 257

#ifdef _WIN64

#define cchMaxAssertStackLevelStringLen \
    ((3 * 8) + cchMaxAssertModuleLen + cchMaxAssertSymbolLen + 13)
    // 3 addresses of at most 8 char, module, symbol, and the extra chars:
    // 0x<address>: <module>! <symbol> + 0x<offset>\n
    //FMT_ADDR_BARE   is defined as   "%08x`%08x" on Win64, and as 
    //"%08x" on 32 bit platforms. Hence the difference in the definitions.

#else

#define cchMaxAssertStackLevelStringLen \
    ((2 * 8) + cchMaxAssertModuleLen + cchMaxAssertSymbolLen + 12)
    // 2 addresses of at most 8 char, module, symbol, and the extra chars:
    // 0x<address>: <module>! <symbol> + 0x<offset>\n

#endif

//
//--- Prototypes --------------------------------------------------------------
//

/****************************************************************************
* MagicDeinit *
*-------------*
*   Description:  
*       Cleans up for the symbol loading code. Should be called before
*       exiting in order to free the dynamically loaded imagehlp.dll
******************************************************************** robch */
void MagicDeinit(void);

/****************************************************************************
* GetStringFromStackLevels *
*--------------------------*
*   Description:  
*       Retrieves a string from the stack frame. If more than one frame, they
*       are separated by newlines. Each fram appears in this format:
*
*           0x<address>: <module>! <symbol> + 0x<offset>
******************************************************************** robch */
void GetStringFromStackLevels(UINT ifrStart, UINT cfrTotal, __out_ecount(cchMaxAssertStackLevelStringLen * cfrTotal) CHAR *pszString, struct _CONTEXT * pContext = NULL);

/****************************************************************************
* GetStringFromAddr *
*-------------------*
*   Description:  
*       Builds a string from an address in the format:
*
*           0x<address>: <module>! <symbol> + 0x<offset>
******************************************************************** robch */
void GetStringFromAddr(DWORD_PTR dwAddr, __out_ecount(cchMaxAssertStackLevelStringLen) LPSTR szString);

#if defined(_TARGET_X86_) && !defined(FEATURE_PAL)
/****************************************************************************
* ClrCaptureContext *
*-------------------*
*   Description:  
*       Exactly the contents of RtlCaptureContext for Win7 - Win2K doesn't
*       support this, so we need it for CoreCLR 4, if we require Win2K support
****************************************************************************/
extern "C" void __stdcall ClrCaptureContext(__out PCONTEXT ctx);
#else // _TARGET_X86_ && !FEATURE_PAL
#define ClrCaptureContext RtlCaptureContext
#endif // _TARGET_X86_ && !FEATURE_PAL


#endif
