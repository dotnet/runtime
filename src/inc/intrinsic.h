//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
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
