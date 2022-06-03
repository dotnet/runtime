// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/******************************************************************
*                                                                 *
*  intsafe.h -- This module defines helper functions to prevent   *
*               integer overflow issues.                          *
*                                                                 *
*                                                                 *
******************************************************************/
#ifndef _INTSAFE_H_INCLUDED_
#define _INTSAFE_H_INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif

#include <specstrings.h>    // for IN, etc.

#define INTSAFE_E_ARITHMETIC_OVERFLOW       ((HRESULT)0x80070216L)  // 0x216 = 534 = ERROR_ARITHMETIC_OVERFLOW

#ifndef LOWORD
#define LOWORD(l)       ((WORD)(((DWORD_PTR)(l)) & 0xffff))
#endif

#ifndef HIWORD
#define HIWORD(l)       ((WORD)(((DWORD_PTR)(l)) >> 16))
#endif

#define HIDWORD(_qw)    ((ULONG)((_qw) >> 32))
#define LODWORD(_qw)    ((ULONG)(_qw))

//
// We make some assumptions about the sizes of various types. Let's be
// explicit about those assumptions and check them.
//
C_ASSERT(sizeof(unsigned short) == 2);
C_ASSERT(sizeof(unsigned int) == 4);
C_ASSERT(sizeof(ULONG) == 4);

#endif // _INTSAFE_H_INCLUDED_
