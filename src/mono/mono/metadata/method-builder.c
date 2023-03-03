/**
 * \file
 * Functions for creating IL methods at runtime.
 *
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *
 * Copyright 2002-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "config.h"
#include <mono/metadata/loader.h>
#include "mono/metadata/abi-details.h"
#include "mono/metadata/method-builder.h"
#include "mono/metadata/method-builder-internals.h"
#include "mono/metadata/method-builder-ilgen.h"
#include "mono/metadata/tabledefs.h"
#include "mono/metadata/exception.h"
#include "mono/metadata/appdomain.h"
#include "mono/metadata/debug-helpers.h"
#include "mono/metadata/metadata-internals.h"
#include "mono/metadata/domain-internals.h"
#include <string.h>
#include <errno.h>

/* #define DEBUG_RUNTIME_CODE */

#define OPDEF(a,b,c,d,e,f,g,h,i,j) \
	a = i,

enum {
#include "mono/cil/opcode.def"
	LAST = 0xff
};
#undef OPDEF

#ifdef DEBUG_RUNTIME_CODE
static char*
indenter (MonoDisHelper *dh, MonoMethod *method, guint32 ip_offset)
{
	return g_strdup (" ");
}

static MonoDisHelper marshal_dh = {
	"\n",
	"IL_%04x: ",
	"IL_%04x",
	indenter,
	NULL,
	NULL
};
#endif

static MonoMethodBuilderCallbacks mb_cb;
static gboolean cb_inited = FALSE;

void
mono_install_method_builder_callbacks (MonoMethodBuilderCallbacks *cb)
{
	g_assert (!cb_inited);
	g_assert (cb->version == MONO_METHOD_BUILDER_CALLBACKS_VERSION);
	memcpy (&mb_cb, cb, sizeof (MonoMethodBuilderCallbacks));
	cb_inited = TRUE;
}

static MonoMethodBuilderCallbacks *
get_mb_cb (void)
{
	if (G_UNLIKELY (!cb_inited)) {
		mono_method_builder_ilgen_init ();
	}
	return &mb_cb;
}

MonoMethodBuilder *
mono_mb_new_no_dup_name (MonoClass *klass, const char *name, MonoWrapperType type)
{
	MonoMethodBuilder *mb = get_mb_cb ()->new_base (klass, type);
	mb->name = (char*)name;
	mb->no_dup_name = TRUE;
	return mb;
}

/**
 * mono_mb_new:
 */
MonoMethodBuilder *
mono_mb_new (MonoClass *klass, const char *name, MonoWrapperType type)
{
	MonoMethodBuilder *mb = get_mb_cb ()->new_base (klass, type);
	mb->name = g_strdup (name);
	return mb;
}

/**
 * mono_mb_free:
 */
void
mono_mb_free (MonoMethodBuilder *mb)
{
	get_mb_cb ()->free (mb);
}

/**
 * mono_mb_create_method:
 * Create a \c MonoMethod from this method builder.
 * \returns the newly created method.
 */
MonoMethod *
mono_mb_create_method (MonoMethodBuilder *mb, MonoMethodSignature *signature, int max_stack)
{
	return get_mb_cb ()->create_method (mb, signature, max_stack);
}

/**
 * mono_mb_add_data:
 */
guint32
mono_mb_add_data (MonoMethodBuilder *mb, gpointer data)
{
	MonoMethodWrapper *mw;

	g_assert (mb != NULL);

	mw = (MonoMethodWrapper *)mb->method;

	/* one O(n) is enough */
	mw->method_data = g_list_prepend ((GList *)mw->method_data, data);

	return g_list_length ((GList *)mw->method_data);
}

