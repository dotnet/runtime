// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* ------------------------------------------------------------------------- *
 * DbgMeta.h - header file for debugger metadata routines
 * ------------------------------------------------------------------------- */

#ifndef _DbgMeta_h_
#define _DbgMeta_h_

#include <cor.h>

/* ------------------------------------------------------------------------- *
 * Structs to support line numbers and variables
 * ------------------------------------------------------------------------- */

class DebuggerLexicalScope;

//
// DebuggerVarInfo
//
// Holds basic information about local variables, method arguments,
// and class static and instance variables.
//
struct DebuggerVarInfo
{
    LPCSTR                 name;
    PCCOR_SIGNATURE        sig;
    unsigned int          varNumber;  // placement info for IL code
    DebuggerLexicalScope*  scope;      // containing scope

    DebuggerVarInfo() : name(NULL), sig(NULL), varNumber(0),
                        scope(NULL) {}
};

#endif /* _DbgMeta_h_ */

