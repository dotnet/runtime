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
#include "loader.h"
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

#ifndef ENABLE_ILGEN
static MonoMethodBuilder *
new_base_noilgen (MonoClass *klass, MonoWrapperType type)
{
	MonoMethodBuilder *mb;
	MonoMethod *m;

	g_assert (klass != NULL);

	mb = g_new0 (MonoMethodBuilder, 1);

	mb->method = m = (MonoMethod *)g_new0 (MonoMethodWrapper, 1);

	m->klass = klass;
	m->inline_info = 1;
	m->wrapper_type = type;

	/* placeholder for the wrapper always at index 1 */
	mono_mb_add_data (mb, NULL);

	return mb;
}

static void
free_noilgen (MonoMethodBuilder *mb)
{
	g_free (mb->method);
	if (!mb->no_dup_name)
		g_free (mb->name);
	g_free (mb);
}

static MonoMethod *
create_method_noilgen (MonoMethodBuilder *mb, MonoMethodSignature *signature, int max_stack)
{
	MonoMethodWrapper *mw;
	MonoImage *image;
	MonoMethod *method;
	GList *l;
	int i;

	g_assert (mb != NULL);

	image = m_class_get_image (mb->method->klass);

	{
		/* Realloc the method info into a mempool */

		method = (MonoMethod *)mono_image_alloc0 (image, sizeof (MonoMethodWrapper));
		memcpy (method, mb->method, sizeof (MonoMethodWrapper));
		mw = (MonoMethodWrapper*) method;

		if (mb->no_dup_name)
			method->name = mb->name;
		else
			method->name = mono_image_strdup (image, mb->name);
	}

	method->signature = signature;
	if (!signature->hasthis)
		method->flags |= METHOD_ATTRIBUTE_STATIC;

	i = g_list_length ((GList *)mw->method_data);
	if (i) {
		GList *tmp;
		void **data;
		l = g_list_reverse ((GList *)mw->method_data);
		if (method_is_dynamic (method))
			data = (void **)g_malloc (sizeof (gpointer) * (i + 1));
		else
			data = (void **)mono_image_alloc (image, sizeof (gpointer) * (i + 1));
		/* store the size in the first element */
		data [0] = GUINT_TO_POINTER (i);
		i = 1;
		for (tmp = l; tmp; tmp = tmp->next) {
			data [i++] = tmp->data;
		}
		g_list_free (l);

		mw->method_data = data;
	}

	return method;
}
#endif

void
mono_install_method_builder_callbacks (MonoMethodBuilderCallbacks *cb)
{
	g_assert (!cb_inited);
	g_assert (cb->version == MONO_METHOD_BUILDER_CALLBACKS_VERSION);
	memcpy (&mb_cb, cb, sizeof (MonoMethodBuilderCallbacks));
	cb_inited = TRUE;
}

#ifndef ENABLE_ILGEN
static void
install_noilgen (void)
{
	MonoMethodBuilderCallbacks cb;
	cb.version = MONO_METHOD_BUILDER_CALLBACKS_VERSION;
	cb.new_base = new_base_noilgen;
	cb.free = free_noilgen;
	cb.create_method = create_method_noilgen;
	mono_install_method_builder_callbacks (&cb);
}
#endif

static MonoMethodBuilderCallbacks *
get_mb_cb (void)
{
	if (G_UNLIKELY (!cb_inited)) {
#ifdef ENABLE_ILGEN
		mono_method_builder_ilgen_init ();
#else
		install_noilgen ();
#endif
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

