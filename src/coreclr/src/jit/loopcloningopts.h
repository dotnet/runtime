// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
/*****************************************************************************/

#ifndef LC_OPT
#error  Define LC_OPT before including this file.
#endif

// Types of Loop Cloning based optimizations.
LC_OPT(LcMdArray)
LC_OPT(LcJaggedArray)

#undef LC_OPT
