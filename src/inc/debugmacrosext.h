//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//*****************************************************************************
// DebugMacrosExt.h
//
// Simple debugging macros that take no dependencies on CLR headers.
// This header can be used from outside the CLR.
//
//*****************************************************************************

#ifndef __DebugMacrosExt_h__
#define __DebugMacrosExt_h__

#if !defined(_DEBUG_IMPL) && defined(_DEBUG) && !defined(DACCESS_COMPILE)
#define _DEBUG_IMPL 1
#endif

#ifdef _DEBUG
// A macro to execute a statement only in _DEBUG.
#define DEBUG_STMT(stmt) stmt
#define INDEBUG(x)          x
#define INDEBUG_COMMA(x)    x,
#define COMMA_INDEBUG(x)    ,x
#define NOT_DEBUG(x)
#else
#define DEBUG_STMT(stmt)
#define INDEBUG(x)
#define INDEBUG_COMMA(x)
#define COMMA_INDEBUG(x)
#define NOT_DEBUG(x)        x
#endif


#ifdef _DEBUG_IMPL
#define INDEBUGIMPL(x)          x
#define INDEBUGIMPL_COMMA(x)    x,
#define COMMA_INDEBUGIMPL(x)    ,x
#else
#define INDEBUGIMPL(x)
#define INDEBUGIMPL_COMMA(x)
#define COMMA_INDEBUGIMPL(x)
#endif


#endif
