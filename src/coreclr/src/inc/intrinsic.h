// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// Intrinsic.h
//
// Force several very useful functions to be intrinsic, which means that the
// compiler will generate code inline for the functions instead of generating
// a call to the function.
//
//*****************************************************************************

#ifndef __intrinsic_h__
#define __intrinsic_h__

#ifdef _MSC_VER
#pragma intrinsic(memcmp)
#pragma intrinsic(memcpy)
#pragma intrinsic(memset)
#pragma intrinsic(strcmp)
#pragma intrinsic(strcpy)
#pragma intrinsic(strlen)
#endif  // defined(_MSC_VER)

#endif // __intrinsic_h__
