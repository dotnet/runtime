// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//
// ===========================================================================
// File: shlwapi.h
//
// Header for ported shlwapi stuff
// ===========================================================================

#ifndef SHLWAPIP_H_INCLUDED
#define SHLWAPIP_H_INCLUDED

#define ARRAYSIZE(x)    (sizeof(x)/sizeof(x[0]))
#define SIZECHARS(sz)   (sizeof(sz)/sizeof(sz[0]))

#define SIZEOF(x)       sizeof(x)
#define PRIVATE
#define PUBLIC
#ifndef ASSERT
#define ASSERT          _ASSERTE
#endif
#define AssertMsg(f,m)  _ASSERTE(f)
#define RIP(f)          _ASSERTE(f)
#define RIPMSG(f,m)     _ASSERTE(f)

#define IS_VALID_READ_BUFFER(p, t, n)   (p != NULL)
#define IS_VALID_WRITE_BUFFER(p, t, n)  (p != NULL)

#define IS_VALID_READ_PTR(p, t)         IS_VALID_READ_BUFFER(p, t, 1)
#define IS_VALID_WRITE_PTR(p, t)        IS_VALID_WRITE_BUFFER(p, t, 1)

#define IS_VALID_STRING_PTR(p, c)       (p != NULL)
#define IS_VALID_STRING_PTRW(p, c)      (p != NULL)

#endif  // ! SHLWAPIP_H_INCLUDED
