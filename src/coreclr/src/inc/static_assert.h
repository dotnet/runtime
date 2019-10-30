// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ---------------------------------------------------------------------------
// static_assert.h
//
// Static assertion infrastructure
// ---------------------------------------------------------------------------

//--------------------------------------------------------------------------------
// static_assert represents a check which should be made at compile time.  It
// can only be done on a constant expression. 
//--------------------------------------------------------------------------------

#ifndef __STATIC_ASSERT_H__
#define __STATIC_ASSERT_H__

// static_assert( cond, msg ) is now a compiler-supported intrinsic in Dev10 C++ compiler.
// Replaces previous uses of STATIC_ASSERT_MSG and COMPILE_TIME_ASSERT_MSG.

// Replaces previous uses of CPP_ASSERT
#define static_assert_n( n, cond ) static_assert( cond, #cond )

// Replaces previous uses of C_ASSERT and COMPILE_TIME_ASSERT
#define static_assert_no_msg( cond ) static_assert( cond, #cond )

#endif // __STATIC_ASSERT_H__

