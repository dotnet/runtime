// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// clang-format off

/*****************************************************************************/
/*****************************************************************************/
#ifndef REGDEF
#error  Must define REGDEF macro before including this file
#endif
#ifndef REGALIAS
#define REGALIAS(alias, realname)
#endif

/*
REGDEF(name, rnum,   mask, sname) */

// The interpreter has no registers; every value lives on the interpreter stack.
// This must be last!
REGDEF(STK,     0, 0x0000, "STK")

/*****************************************************************************/
#undef  REGDEF
#undef  REGALIAS
/*****************************************************************************/

// clang-format on
