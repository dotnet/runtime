// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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

// Replaces previous uses of C_ASSERT and COMPILE_TIME_ASSERT
#define static_assert_no_msg( cond ) static_assert( cond, #cond )

#endif // __STATIC_ASSERT_H__

