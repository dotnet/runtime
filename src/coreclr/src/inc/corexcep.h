// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*********************************************************************
 **                                                                 **
 ** CorExcep.h - lists the exception codes used by the CLR.         **
 **                                                                 **
 *********************************************************************/


#ifndef __COREXCEP_H__
#define __COREXCEP_H__

// All COM+ exceptions are expressed as a RaiseException with this exception
// code.  If you change this value, you must also change
// mscorlib\src\system\Exception.cs's _COMPlusExceptionCode value.

#define EXCEPTION_MSVC    0xe06d7363    // 0xe0000000 | 'msc'

#define EXCEPTION_COMPLUS 0xe0434352    // 0xe0000000 | 'CCR'

#define EXCEPTION_HIJACK  0xe0434f4e    // 0xe0000000 | 'COM'+1

#ifdef FEATURE_STACK_PROBE
#define EXCEPTION_SOFTSO  0xe053534f    // 0xe0000000 | 'SSO'
                                        // We can not throw internal C++ exception through managed frame.
                                        // At boundary, we will raise an exception with this error code
#endif

#if defined(_DEBUG)
#define EXCEPTION_INTERNAL_ASSERT 0xe0584d4e // 0xe0000000 | 'XMN'
                                        // An internal Assert will raise this exception when the config
                                        // value "RaiseExceptionOnAssert" si specified. This is used in
                                        // stress to facilitate failure triaging.
#endif

// This is the exception code to report SetupThread failure to caller of reverse pinvoke
// It is misleading to use our COM+ exception code, since this is not a managed exception.  
// In the end, we picked e0455858 (EXX).
#define EXCEPTION_EXX     0xe0455858    // 0xe0000000 | 'EXX'
#endif // __COREXCEP_H__
