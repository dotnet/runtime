// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***
*cruntime.h - definitions specific to the target operating system and hardware
*

*
*Purpose:
*       This header file contains widely used definitions specific to the
*       host operating system and hardware. It is included by every C source
*       and most every other header file.
*
*       [Internal]
*
****/

#if _MSC_VER > 1000
#pragma once
#endif  /* _MSC_VER > 1000 */

#ifndef _INC_CRUNTIME
#define _INC_CRUNTIME

#ifndef _CRTBLD
/*
 * This is an internal C runtime header file. It is used when building
 * the C runtimes only. It is not to be used as a public header file.
 */
#error ERROR: Use of C runtime library internal header file.
#endif  /* _CRTBLD */

#if defined (_SYSCRT) && defined (BIT64)
#define _USE_OLD_STDCPP 1
#endif  /* defined (_SYSCRT) && defined (BIT64) */

#if !defined (UNALIGNED)
#if defined (_M_AMD64)
#define UNALIGNED __unaligned
#else  /* defined (_M_AMD64) */
#define UNALIGNED
#endif  /* defined (_M_AMD64) */
#endif  /* !defined (UNALIGNED) */

#ifdef _M_IX86
/*
 * 386/486
 */
#define REG1    register
#define REG2    register
#define REG3    register
#define REG4
#define REG5
#define REG6
#define REG7
#define REG8
#define REG9

#elif defined (_M_AMD64)
/*
 * AMD64
 */
#define REG1    register
#define REG2    register
#define REG3    register
#define REG4    register
#define REG5    register
#define REG6    register
#define REG7    register
#define REG8    register
#define REG9    register

#else  /* defined (_M_AMD64) */

#pragma message ("Machine register set not defined")

/*
 * Unknown machine
 */

#define REG1
#define REG2
#define REG3
#define REG4
#define REG5
#define REG6
#define REG7
#define REG8
#define REG9

#endif  /* defined (_M_AMD64) */

/*
 * Are the macro definitions below still needed in this file?
 */

#endif  /* _INC_CRUNTIME */
