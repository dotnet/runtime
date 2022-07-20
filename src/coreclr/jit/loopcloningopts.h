// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
/*****************************************************************************/

#ifndef LC_OPT
#error Define LC_OPT before including this file.
#endif

// Types of Loop Cloning based optimizations.
LC_OPT(LcMdArray)
LC_OPT(LcJaggedArray)
LC_OPT(LcTypeTest)

#undef LC_OPT
