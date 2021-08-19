// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


//
 /*vim: set foldmethod=marker: */
#include <stdafx.h>

//dummy implementation for dac
HRESULT ClrDataAccess::DumpNativeImage(CLRDATA_ADDRESS loadedBase,
    LPCWSTR name,
    IXCLRDataDisplay* display,
    IXCLRLibrarySupport* support,
    IXCLRDisassemblySupport* dis)
{
    return E_FAIL;
}

/* REVISIT_TODO Mon 10/10/2005
 * Here is where it gets bad.  There is no DAC build of gcdump, so instead
 * build it directly into the the dac.  That's what all these ugly defines
 * are all about.
 */
#ifdef __MSC_VER
#pragma warning(disable:4244)   // conversion from 'unsigned int' to 'unsigned short', possible loss of data
#pragma warning(disable:4189)   // local variable is initialized but not referenced
#endif // __MSC_VER

#undef assert
#define assert(a)
#define NOTHROW
#define GC_NOTRIGGER
#include <gcdecoder.cpp>
#undef NOTHROW
#undef GC_NOTRIGGER

#if defined _DEBUG && defined TARGET_X86
#ifdef _MSC_VER
// disable FPO for checked build
#pragma optimize("y", off)
#endif // _MSC_VER
#endif

#undef _ASSERTE
#define _ASSERTE(a) do {} while (0)
#ifdef TARGET_X86
#include <gcdump.cpp>
#endif

#undef LIMITED_METHOD_CONTRACT
#undef WRAPPER_NO_CONTRACT
#ifdef TARGET_X86
#include <i386/gcdumpx86.cpp>
#else // !TARGET_X86
#undef PREGDISPLAY
#include <gcdumpnonx86.cpp>
#endif // !TARGET_X86

#ifdef __MSC_VER
#pragma warning(default:4244)
#pragma warning(default:4189)
#endif // __MSC_VER
