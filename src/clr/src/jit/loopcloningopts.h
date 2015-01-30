//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

//
/*****************************************************************************/

#ifndef LC_OPT
#error  Define LC_OPT before including this file.
#endif

// Types of Loop Cloning based optimizations.
LC_OPT(LcMdArray)
LC_OPT(LcJaggedArray)

#undef LC_OPT
