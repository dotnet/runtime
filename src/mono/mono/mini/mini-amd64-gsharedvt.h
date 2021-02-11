/**
 * \file
 * libcorkscrew-based native unwinder
 *
 * Authors:
 *   Zoltan Varga <vargaz@gmail.com>
 *   Rodrigo Kumpera <kumpera@gmail.com>
 *   Andi McClure <andi.mcclure@xamarin.com>
 *   Johan Lorensson <johan.lorensson@xamarin.com>
 *
 * Copyright 2015 Xamarin, Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef MINI_AMD64_GSHAREDVT_H
#define MINI_AMD64_GSHAREDVT_H

typedef enum {
	GSHAREDVT_ARG_NONE = 0,
	GSHAREDVT_ARG_BYVAL_TO_BYREF,
	GSHAREDVT_ARG_BYREF_TO_BYVAL,
	GSHAREDVT_ARG_BYREF_TO_BYVAL_I1,
	GSHAREDVT_ARG_BYREF_TO_BYVAL_U1,
	GSHAREDVT_ARG_BYREF_TO_BYVAL_I2,
	GSHAREDVT_ARG_BYREF_TO_BYVAL_U2,
	GSHAREDVT_ARG_BYREF_TO_BYVAL_I4,
	GSHAREDVT_ARG_BYREF_TO_BYVAL_U4
} GSharedVtArgMarshal;

typedef enum {
	GSHAREDVT_RET_NONE = 0,
	GSHAREDVT_RET_I1,      // 1 byte integer
	GSHAREDVT_RET_U1,      // 1 byte unsigned
	GSHAREDVT_RET_I2,      // 2 byte integer
	GSHAREDVT_RET_U2,      // 2 byte unsigned
	GSHAREDVT_RET_I4,      // 4 byte integer
	GSHAREDVT_RET_U4,      // 4 byte unsigned
	GSHAREDVT_RET_I8,      // 8 byte integer
	GSHAREDVT_RET_IREGS_1, // Load in first return register
	GSHAREDVT_RET_R8,     // Double
	GSHAREDVT_RET_NUM,
} GSharedVtRetMarshal;

#ifdef DEBUG_AMD64_GSHAREDVT
#define DEBUG_AMD64_GSHAREDVT_PRINT printf
#else
#define DEBUG_AMD64_GSHAREDVT_PRINT(...)
#endif

#endif /* MINI_AMD64_GSHAREDVT_H */
