/**
 * \file
 * Security Manager
 *
 * Author:
 *	Sebastien Pouliot  <sebastien@ximian.com>
 *
 * Copyright (C) 2004-2005 Novell, Inc (http://www.novell.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef _MONO_METADATA_SECURITY_MANAGER_H_
#define _MONO_METADATA_SECURITY_MANAGER_H_

#include <string.h>

#include "object.h"
#include "metadata-internals.h"
#include "domain-internals.h"
#include "tokentype.h"
#include "threads.h"
#include "marshal.h"
#include "image.h"
#include "reflection.h"
#include "tabledefs.h"
#include <mono/metadata/icalls.h>

/* Definitions */

#define MONO_ECMA_KEY_LENGTH			16
#define MONO_PUBLIC_KEY_HEADER_LENGTH		32
#define MONO_MINIMUM_PUBLIC_KEY_LENGTH		48
#define MONO_DEFAULT_PUBLIC_KEY_LENGTH		128

#define MONO_PUBLIC_KEY_BIT_SIZE(x)		((x - MONO_PUBLIC_KEY_HEADER_LENGTH) << 3)

typedef enum {
	MONO_SECURITY_MODE_NONE,
	MONO_SECURITY_MODE_CORE_CLR,
} MonoSecurityMode;

#endif /* _MONO_METADATA_SECURITY_MANAGER_H_ */
