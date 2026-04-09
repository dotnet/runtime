// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*********************************************************************
 **                                                                 **
 ** CorExcep.h - lists the exception codes used by the CLR.         **
 **                                                                 **
 *********************************************************************/


#ifndef __COREXCEP_H__
#define __COREXCEP_H__

// All CLR exceptions are expressed as a RaiseException with this exception
// code.  If you change this value, you must also change
// Exception.cs's EXCEPTION_COMPLUS value.

#define EXCEPTION_MSVC    0xe06d7363    // 0xe0000000 | 'msc'

#define EXCEPTION_COMPLUS 0xe0434352    // 0xe0000000 | 'CCR'

#define EXCEPTION_HIJACK  0xe0434f4e    // 0xe0000000 | 'COM'+1

#if defined(_DEBUG)
#define EXCEPTION_INTERNAL_ASSERT 0xe0584d4e // 0xe0000000 | 'XMN'
                                        // An internal Assert will raise this exception when the config
                                        // value "RaiseExceptionOnAssert" si specified. This is used in
                                        // stress to facilitate failure triaging.
#endif

#endif // __COREXCEP_H__
