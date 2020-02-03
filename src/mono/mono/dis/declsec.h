/*
 * declsec.h:  Support for the new declarative security attribute
 *	metadata format (2.0)
 *
 * Author:
 *	Sebastien Pouliot  <sebastien@ximian.com>
 *
 * Copyright (C) 2005 Novell, Inc (http://www.novell.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef __MONODIS_DECLSEC_H__
#define __MONODIS_DECLSEC_H__

#define MONO_DECLSEC_FORMAT_20		0x2E

#define MONO_DECLSEC_FIELD		0x53
#define MONO_DECLSEC_PROPERTY		0x54
#define MONO_DECLSEC_ENUM		0x55

#define MONO_TYPE_SYSTEM_TYPE		0x50

char* dump_declsec_entry20 (MonoImage *m, const char* p, const char *indent);

#endif /* __MONODIS_DECLSEC_H__ */
