/*
 * declsec.h:  Declarative Security support
 *
 * Author:
 *	Sebastien Pouliot  <sebastien@ximian.com>
 *
 * (C) 2004 Novell (http://www.novell.com)
 */

#ifndef _MONO_MINI_DECLSEC_H_
#define _MONO_MINI_DECLSEC_H_

#include <string.h>

#include "mono/metadata/class-internals.h"
#include "mono/metadata/object.h"
#include "mono/metadata/tabledefs.h"

MonoBoolean mono_method_has_declsec (MonoMethod *method);

#endif /* _MONO_MINI_DECLSEC_H_ */
