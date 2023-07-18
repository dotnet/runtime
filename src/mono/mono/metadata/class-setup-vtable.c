/**
 * \file MonoClass vtable setup
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Copyright 2012 Xamarin Inc (http://www.xamarin.com)
 * Copyright 2018 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <config.h>
#include <mono/metadata/class-init.h>
#include <mono/metadata/class-init-internals.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/marshal.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/utils/unlocked.h>
#ifdef MONO_CLASS_DEF_PRIVATE
/* Class initialization gets to see the fields of MonoClass */
#define REALLY_INCLUDE_CLASS_DEF 1
#include <mono/metadata/class-private-definition.h>
#undef REALLY_INCLUDE_CLASS_DEF
#endif

#define FEATURE_COVARIANT_RETURNS

static void mono_class_setup_vtable_full (MonoClass *klass, GList *in_setup);

static void
print_implemented_interfaces (MonoClass *klass)
{
	char *name;
	ERROR_DECL (error);
	GPtrArray *ifaces = NULL;
	int ancestor_level = 0;

	name = mono_type_get_full_name (klass);
	printf ("Packed interface table for class %s has size %d\n", name, klass->interface_offsets_count);
	g_free (name);

	for (guint16 i = 0; i < klass->interface_offsets_count; i++) {
		char *ic_name = mono_type_get_full_name (klass->interfaces_packed [i]);
		printf ("  [%03hu][UUID %03d][SLOT %03d][SIZE  %03d] interface %s\n", i,
				klass->interfaces_packed [i]->interface_id,
				klass->interface_offsets_packed [i],
				mono_class_get_method_count (klass->interfaces_packed [i]),
				ic_name);
		g_free (ic_name);
	}
	printf ("Interface flags: ");
	for (guint32 i = 0; i <= klass->max_interface_id; i++)
		if (MONO_CLASS_IMPLEMENTS_INTERFACE (klass, i))
			printf ("(%d,T)", i);
		else
			printf ("(%d,F)", i);
	printf ("\n");
	printf ("Dump interface flags:");
#ifdef COMPRESSED_INTERFACE_BITMAP
	{
		const uint8_t* p = klass->interface_bitmap;
		guint32 i = klass->max_interface_id;
		while (i > 0) {
			printf (" %d x 00 %02X", p [0], p [1]);
			i -= p [0] * 8;
			i -= 8;
		}
	}
#else
	for (guint32 i = 0; i < ((((klass->max_interface_id + 1) >> 3)) + (((klass->max_interface_id + 1) & 7)? 1 :0)); i++)
		printf (" %02X", klass->interface_bitmap [i]);
#endif
	printf ("\n");
	while (klass != NULL) {
		printf ("[LEVEL %d] Implemented interfaces by class %s:\n", ancestor_level, klass->name);
		ifaces = mono_class_get_implemented_interfaces (klass, error);
		if (!is_ok (error)) {
			printf ("  Type failed due to %s\n", mono_error_get_message (error));
			mono_error_cleanup (error);
		} else if (ifaces) {
			for (guint i = 0; i < ifaces->len; i++) {
				MonoClass *ic = (MonoClass *)g_ptr_array_index (ifaces, i);
				printf ("  [UIID %d] interface %s\n", ic->interface_id, ic->name);
				printf ("  [%03d][UUID %03d][SLOT %03d][SIZE  %03d] interface %s.%s\n", i,
						ic->interface_id,
						mono_class_interface_offset (klass, ic),
						mono_class_get_method_count (ic),
						ic->name_space,
						ic->name );
			}
			g_ptr_array_free (ifaces, TRUE);
		}
		ancestor_level ++;
		klass = klass->parent;
	}
}

static mono_bool
set_interface_and_offset (int num_ifaces, MonoClass **interfaces_full, int *interface_offsets_full, MonoClass *ic, int offset, mono_bool force_set)
{
	int i;
	for (i = 0; i < num_ifaces; ++i) {
		if (interfaces_full [i] && interfaces_full [i]->interface_id == ic->interface_id) {
			if (!force_set)
				return TRUE;
			interface_offsets_full [i] = offset;
			return FALSE;
		}
		if (interfaces_full [i])
			continue;
		interfaces_full [i] = ic;
		interface_offsets_full [i] = offset;
		break;
	}
	return FALSE;
}

/**
 * mono_class_setup_invalidate_interface_offsets:
 *
 * Sets a field in the MonoClass to make mono_class_setup_interface_offsets_internal publish its results when called.
 *
 * This is a hack used by sre to compute the interface offsets
 */
void
mono_class_setup_invalidate_interface_offsets (MonoClass *klass)
{
	g_assert (MONO_CLASS_IS_INTERFACE_INTERNAL (klass));
	g_assert (!mono_class_is_ginst (klass));
	klass->interface_offsets_packed = NULL;
}

/**
 * mono_class_setup_interface_offsets_internal:
 *
 * Do not call this function outside of class creation.
 *
 * Return -1 on failure and set klass->has_failure and store a MonoErrorBoxed with the details.
 * LOCKING: Acquires the loader lock.
 */
int
mono_class_setup_interface_offsets_internal (MonoClass *klass, int cur_slot, int setup_itf_offsets_flags)
{
	ERROR_DECL (error);
	MonoClass *k, *ic;
	int num_ifaces;
	guint32 max_iid;
	MonoClass **interfaces_full = NULL;
	int *interface_offsets_full = NULL;
	GPtrArray *ifaces;
	GPtrArray **ifaces_array = NULL;
	int interface_offsets_count;
	max_iid = 0;
	num_ifaces = interface_offsets_count = 0;
	gboolean overwrite = (setup_itf_offsets_flags & MONO_SETUP_ITF_OFFSETS_OVERWRITE) != 0;
	gboolean bitmap_only = (setup_itf_offsets_flags & MONO_SETUP_ITF_OFFSETS_BITMAP_ONLY) != 0;

	mono_loader_lock ();

	mono_class_setup_supertypes (klass);

	if (mono_class_is_ginst (klass)) {
		MonoClass *gklass = mono_class_get_generic_class (klass)->container_class;

		interface_offsets_count = num_ifaces = gklass->interface_offsets_count;
		interfaces_full = (MonoClass **)g_malloc (sizeof (MonoClass*) * num_ifaces);
		interface_offsets_full = (int *)g_malloc (sizeof (int) * num_ifaces);

		cur_slot = 0;
		for (int i = 0; i < num_ifaces; ++i) {
			MonoClass *gklass_ic = gklass->interfaces_packed [i];
			MonoClass *inflated = mono_class_inflate_generic_class_checked (gklass_ic, mono_class_get_context(klass), error);
			if (!is_ok (error)) {
				char *name = mono_type_get_full_name (gklass_ic);
				mono_class_set_type_load_failure (klass, "Error calculating interface offset of %s", name);
				g_free (name);
				cur_slot = -1;
				goto end;
			}

			mono_class_setup_interface_id_nolock (inflated);

			interfaces_full [i] = inflated;
			if (!bitmap_only)
				interface_offsets_full [i] = gklass->interface_offsets_packed [i];

			int count = mono_class_setup_count_virtual_methods (inflated);
			if (count == -1) {
				char *name = mono_type_get_full_name (inflated);
				mono_class_set_type_load_failure (klass, "Error calculating interface offset of %s", name);
				g_free (name);
				cur_slot = -1;
				goto end;
			}

			cur_slot = MAX (cur_slot, interface_offsets_full [i] + count);
			max_iid = MAX (max_iid, inflated->interface_id);
		}

		goto publish;
	}
	/* compute maximum number of slots and maximum interface id */
	max_iid = 0;
	num_ifaces = 0; /* this can include duplicated ones */
	ifaces_array = g_new0 (GPtrArray *, klass->idepth);
	for (guint16 j = 0; j < klass->idepth; j++) {
		k = klass->supertypes [j];
		g_assert (k);
		num_ifaces += k->interface_count;
		for (guint16 i = 0; i < k->interface_count; i++) {
			ic = k->interfaces [i];

			/* A gparam does not have any interface_id set. */
			if (! mono_class_is_gparam (ic))
				mono_class_setup_interface_id_nolock (ic);

			if (max_iid < ic->interface_id)
				max_iid = ic->interface_id;
		}
		ifaces = mono_class_get_implemented_interfaces (k, error);
		if (!is_ok (error)) {
			char *name = mono_type_get_full_name (k);
			mono_class_set_type_load_failure (klass, "Error getting the interfaces of %s due to %s", name, mono_error_get_message (error));
			g_free (name);
			mono_error_cleanup (error);
			cur_slot = -1;
			goto end;
		}
		if (ifaces) {
			num_ifaces += ifaces->len;
			for (guint i = 0; i < ifaces->len; ++i) {
				ic = (MonoClass *)g_ptr_array_index (ifaces, i);
				if (max_iid < ic->interface_id)
					max_iid = ic->interface_id;
			}
			ifaces_array [j] = ifaces;
		}
	}

	if (MONO_CLASS_IS_INTERFACE_INTERNAL (klass)) {
		num_ifaces++;
		if (max_iid < klass->interface_id)
			max_iid = klass->interface_id;
	}

	/* compute vtable offset for interfaces */
	interfaces_full = (MonoClass **)g_malloc0 (sizeof (MonoClass*) * num_ifaces);
	interface_offsets_full = (int *)g_malloc (sizeof (int) * num_ifaces);

	for (int i = 0; i < num_ifaces; i++)
		interface_offsets_full [i] = -1;

	/* skip the current class */
	for (guint16 j = 0; j < klass->idepth - 1; j++) {
		k = klass->supertypes [j];
		ifaces = ifaces_array [j];

		if (ifaces) {
			for (guint i = 0; i < ifaces->len; ++i) {
				int io = -1;
				ic = (MonoClass *)g_ptr_array_index (ifaces, i);

				/*Force the sharing of interface offsets between parent and subtypes.*/
				if (!bitmap_only) {
					io = mono_class_interface_offset (k, ic);
					g_assertf (io >= 0, "class %s parent %s has no offset for iface %s",
						   mono_type_get_full_name (klass),
						   mono_type_get_full_name (k),
						   mono_type_get_full_name (ic));
				} else {
					/* if we don't care about offsets, just use a fake one */
					io = 0;
				}

				set_interface_and_offset (num_ifaces, interfaces_full, interface_offsets_full, ic, io, TRUE);
			}
		}
	}

	g_assert (klass == klass->supertypes [klass->idepth - 1]);
	ifaces = ifaces_array [klass->idepth - 1];
	if (ifaces) {
		for (guint i = 0; i < ifaces->len; ++i) {
			int count;
			ic = (MonoClass *)g_ptr_array_index (ifaces, i);
			if (set_interface_and_offset (num_ifaces, interfaces_full, interface_offsets_full, ic, cur_slot, FALSE))
				continue;
			count = mono_class_setup_count_virtual_methods (ic);
			if (count == -1) {
				char *name = mono_type_get_full_name (ic);
				mono_class_set_type_load_failure (klass, "Error calculating interface offset of %s", name);
				g_free (name);
				cur_slot = -1;
				goto end;
			}
			cur_slot += count;
		}
	}

	if (MONO_CLASS_IS_INTERFACE_INTERNAL (klass))
		set_interface_and_offset (num_ifaces, interfaces_full, interface_offsets_full, klass, cur_slot, TRUE);

	interface_offsets_count = 0;
	for (int i = 0; i < num_ifaces; i++) {
		if (interface_offsets_full [i] != -1)
			interface_offsets_count ++;
	}

publish:
	/* Publish the data */
	klass->max_interface_id = max_iid;
	/*
	 * We might get called multiple times:
	 * - mono_class_init_internal ()
	 * - mono_class_setup_vtable ().
	 * - mono_class_setup_interface_offsets ().
	 * mono_class_setup_interface_offsets () passes 0 as CUR_SLOT, so the computed interface offsets will be invalid. This
	 * means we have to overwrite those when called from other places (#4440).
	 */
	if (klass->interface_offsets_packed) {
		if (!overwrite)
			g_assert (klass->interface_offsets_count == interface_offsets_count);
	} else {
		uint8_t *bitmap;
		int bsize;
		klass->interface_offsets_count = GINT_TO_UINT16 (interface_offsets_count);
		klass->interfaces_packed = (MonoClass **)mono_class_alloc (klass, sizeof (MonoClass*) * interface_offsets_count);
		if (!bitmap_only) {
			klass->interface_offsets_packed = (guint16 *)mono_class_alloc (klass, sizeof (guint16) * interface_offsets_count);
		}
		bsize = (sizeof (guint8) * ((max_iid + 1) >> 3)) + (((max_iid + 1) & 7)? 1 :0);
#ifdef COMPRESSED_INTERFACE_BITMAP
		bitmap = g_malloc0 (bsize);
#else
		bitmap = (uint8_t *)mono_class_alloc0 (klass, bsize);
#endif
		for (int i = 0; i < interface_offsets_count; i++) {
			guint32 id = interfaces_full [i]->interface_id;
			bitmap [id >> 3] |= (1 << (id & 7));
			klass->interfaces_packed [i] = interfaces_full [i];
			if (!bitmap_only) {
				klass->interface_offsets_packed [i] = GINT_TO_UINT16 (interface_offsets_full [i]);
			}
		}
		if (!klass->interface_bitmap) {
#ifdef COMPRESSED_INTERFACE_BITMAP
			int i = mono_compress_bitmap (NULL, bitmap, bsize);
			klass->interface_bitmap = mono_class_alloc0 (klass, i);
			mono_compress_bitmap (klass->interface_bitmap, bitmap, bsize);
			g_free (bitmap);
#else
			klass->interface_bitmap = bitmap;
#endif
		}
	}
end:
	mono_loader_unlock ();

	g_free (interfaces_full);
	g_free (interface_offsets_full);
	if (ifaces_array) {
		for (guint16 i = 0; i < klass->idepth; i++) {
			ifaces = ifaces_array [i];
			if (ifaces)
				g_ptr_array_free (ifaces, TRUE);
		}
		g_free (ifaces_array);
	}

	//printf ("JUST DONE: ");
	//print_implemented_interfaces (klass);

 	return cur_slot;
}

/*
 * Setup interface offsets for interfaces.
 * Initializes:
 * - klass->max_interface_id
 * - klass->interface_offsets_count
 * - klass->interfaces_packed
 * - klass->interface_offsets_packed
 * - klass->interface_bitmap
 *
 * This function can fail @class.
 *
 */
void
mono_class_setup_interface_offsets (MonoClass *klass)
{
	/* NOTE: This function is only correct for interfaces.
	 *
	 * It assumes that klass's interfaces can be assigned offsets starting
	 * from 0. That assumption is incorrect for classes and valuetypes.
	 */
	g_assert (MONO_CLASS_IS_INTERFACE_INTERNAL (klass) && !mono_class_is_ginst (klass));
	mono_class_setup_interface_offsets_internal (klass, 0, 0);
}


#define DEBUG_INTERFACE_VTABLE_CODE 0
#define TRACE_INTERFACE_VTABLE_CODE 0
#define VERIFY_INTERFACE_VTABLE_CODE 0
#define VTABLE_SELECTOR (1)

#if (TRACE_INTERFACE_VTABLE_CODE|DEBUG_INTERFACE_VTABLE_CODE)
#define DEBUG_INTERFACE_VTABLE(stmt) do {\
	if (!(VTABLE_SELECTOR)) break; \
	stmt;\
} while (0)
#else
#define DEBUG_INTERFACE_VTABLE(stmt)
#endif

#if TRACE_INTERFACE_VTABLE_CODE
#define TRACE_INTERFACE_VTABLE(stmt) do {\
	if (!(VTABLE_SELECTOR)) break; \
	stmt;\
} while (0)
#else
#define TRACE_INTERFACE_VTABLE(stmt)
#endif

#if VERIFY_INTERFACE_VTABLE_CODE
#define VERIFY_INTERFACE_VTABLE(stmt) do {\
	if (!(VTABLE_SELECTOR)) break; \
	stmt;\
} while (0)
#else
#define VERIFY_INTERFACE_VTABLE(stmt)
#endif


#if (TRACE_INTERFACE_VTABLE_CODE|DEBUG_INTERFACE_VTABLE_CODE)
static char*
mono_signature_get_full_desc (MonoMethodSignature *sig, gboolean include_namespace)
{
	int i;
	char *result;
	GString *res = g_string_new ("");

	g_string_append_c (res, '(');
	for (i = 0; i < sig->param_count; ++i) {
		if (i > 0)
			g_string_append_c (res, ',');
		mono_type_get_desc (res, sig->params [i], include_namespace);
	}
	g_string_append (res, ")=>");
	if (sig->ret != NULL) {
		mono_type_get_desc (res, sig->ret, include_namespace);
	} else {
		g_string_append (res, "NULL");
	}
	result = res->str;
	g_string_free (res, FALSE);
	return result;
}
static void
print_method_signatures (MonoMethod *im, MonoMethod *cm) {
	char *im_sig = mono_signature_get_full_desc (mono_method_signature_internal (im), TRUE);
	char *cm_sig = mono_signature_get_full_desc (mono_method_signature_internal (cm), TRUE);
	printf ("(IM \"%s\", CM \"%s\")", im_sig, cm_sig);
	g_free (im_sig);
	g_free (cm_sig);

}

#endif

static gboolean
is_wcf_hack_disabled (void)
{
	static char disabled;
	if (!disabled)
		disabled = g_hasenv ("MONO_DISABLE_WCF_HACK") ? 1 : 2;
	return disabled == 1;
}

enum MonoInterfaceMethodOverrideFlags {
	MONO_ITF_OVERRIDE_REQUIRE_NEWSLOT = 0x01,
	MONO_ITF_OVERRIDE_EXPLICITLY_IMPLEMENTED = 0x02,
	MONO_ITF_OVERRIDE_SLOT_EMPTY = 0x04,
	MONO_ITF_OVERRIDE_VARIANT_ITF = 0x08,
};

static gboolean
signature_is_subsumed (MonoMethod *impl_method, MonoMethod *decl_method, MonoError *error);

/*
 * Returns TRUE if the signature of \c decl is assignable from the signature of \c impl.  That is, if \c sig_impl is more
 * specific than \c sig_decl.
 */
static gboolean
signature_assignable_from (MonoMethod *decl, MonoMethod *impl)
{
	ERROR_DECL (error);
	/* FIXME: the "signature_is_subsumed" check for covariant returns is not good enough:
	 * 1. It doesn't check that the arguments are contravariant.
	 * 2. it's too general: we don't want  Foo SomeMethod() to be subsumed by Bar SomeMethod() unless
	 *    at least of Foo or Bar was a generic parameter.
	 */
	if (signature_is_subsumed (impl, decl, error))
		return TRUE;
	mono_error_cleanup (error);
	return FALSE;
}

static gboolean
check_interface_method_override (MonoClass *klass, MonoMethod *im, MonoMethod *cm, int flags)
{
	gboolean require_newslot = (flags & MONO_ITF_OVERRIDE_REQUIRE_NEWSLOT) != 0;
	gboolean interface_is_explicitly_implemented_by_class = (flags & MONO_ITF_OVERRIDE_EXPLICITLY_IMPLEMENTED) != 0;
	gboolean slot_is_empty = (flags & MONO_ITF_OVERRIDE_SLOT_EMPTY) != 0;
	gboolean variant_itf = (flags & MONO_ITF_OVERRIDE_VARIANT_ITF) != 0;
	MonoMethodSignature *cmsig, *imsig;
	if (strcmp (im->name, cm->name) == 0) {
		if ((cm->flags & METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK) != METHOD_ATTRIBUTE_PUBLIC) {
			TRACE_INTERFACE_VTABLE (printf ("[PUBLIC CHECK FAILED]"));
			return FALSE;
		}
		if (! slot_is_empty) {
			if (require_newslot) {
				if (! interface_is_explicitly_implemented_by_class) {
					TRACE_INTERFACE_VTABLE (printf ("[NOT EXPLICIT IMPLEMENTATION IN FULL SLOT REFUSED]"));
					return FALSE;
				}
				if (! (cm->flags & METHOD_ATTRIBUTE_NEW_SLOT)) {
					TRACE_INTERFACE_VTABLE (printf ("[NEWSLOT CHECK FAILED]"));
					return FALSE;
				}
			} else {
				TRACE_INTERFACE_VTABLE (printf ("[FULL SLOT REFUSED]"));
			}
		}
		cmsig = mono_method_signature_internal (cm);
		imsig = mono_method_signature_internal (im);
		if (!cmsig || !imsig) {
			mono_class_set_type_load_failure (klass, "Could not resolve the signature of a virtual method");
			return FALSE;
		}

		/* if there's a variant interface, the method could be using the generic param in a
		 * variant position compared to the interface sig
		 *
		 * public interface IFactory<out T> { T Get(); }
		 * public class Foo {}
		 * public class Bar : Foo {}
		 * public class FooFactory : IFactory<Foo> { public Foo Get() => new Foo(); }
		 * public class BarFactory : FooFactory, IFactory<Bar> { public new Bar Get() => new Bar(); }
		 *
		 * In this case, there's an explicit newslot but we want to match up 'Bar BarFactory:Get ()'
		 * with 'Foo IFactory<Foo>:Get ()'.
		 */
		if (! mono_metadata_signature_equal (cmsig, imsig) && !(variant_itf && signature_assignable_from (im, cm))) {
			TRACE_INTERFACE_VTABLE (printf ("[SIGNATURE CHECK FAILED  "));
			TRACE_INTERFACE_VTABLE (print_method_signatures (im, cm));
			TRACE_INTERFACE_VTABLE (printf ("]"));
			return FALSE;
		}

		TRACE_INTERFACE_VTABLE (printf ("[NAME CHECK OK]"));
		if (is_wcf_hack_disabled () && !mono_method_can_access_method_full (cm, im, NULL)) {
			char *body_name = mono_method_full_name (cm, TRUE);
			char *decl_name = mono_method_full_name (im, TRUE);
			mono_class_set_type_load_failure (klass, "Method %s overrides method '%s' which is not accessible", body_name, decl_name);
			g_free (body_name);
			g_free (decl_name);
			return FALSE;
		}

		return TRUE;
	} else {
		MonoClass *ic = im->klass;
		const char *ic_name_space = ic->name_space;
		const char *ic_name = ic->name;
		char *subname;

		if (! require_newslot) {
			TRACE_INTERFACE_VTABLE (printf ("[INJECTED METHOD REFUSED]"));
			return FALSE;
		}
		if (cm->klass->rank == 0) {
			TRACE_INTERFACE_VTABLE (printf ("[RANK CHECK FAILED]"));
			return FALSE;
		}
		cmsig = mono_method_signature_internal (cm);
		imsig = mono_method_signature_internal (im);
		if (!cmsig || !imsig) {
			mono_class_set_type_load_failure (klass, "Could not resolve the signature of a virtual method");
			return FALSE;
		}

		if (! mono_metadata_signature_equal (cmsig, imsig)) {
			TRACE_INTERFACE_VTABLE (printf ("[(INJECTED) SIGNATURE CHECK FAILED  "));
			TRACE_INTERFACE_VTABLE (print_method_signatures (im, cm));
			TRACE_INTERFACE_VTABLE (printf ("]"));
			return FALSE;
		}
		if (mono_class_get_image (ic) != mono_defaults.corlib) {
			TRACE_INTERFACE_VTABLE (printf ("[INTERFACE CORLIB CHECK FAILED]"));
			return FALSE;
		}
		if ((ic_name_space == NULL) || (strcmp (ic_name_space, "System.Collections.Generic") != 0)) {
			TRACE_INTERFACE_VTABLE (printf ("[INTERFACE NAMESPACE CHECK FAILED]"));
			return FALSE;
		}
		if ((ic_name == NULL) || ((strcmp (ic_name, "IEnumerable`1") != 0) && (strcmp (ic_name, "ICollection`1") != 0) && (strcmp (ic_name, "IList`1") != 0) && (strcmp (ic_name, "IReadOnlyList`1") != 0) && (strcmp (ic_name, "IReadOnlyCollection`1") != 0))) {
			TRACE_INTERFACE_VTABLE (printf ("[INTERFACE NAME CHECK FAILED]"));
			return FALSE;
		}

		subname = (char*)strstr (cm->name, ic_name_space);
		if (subname != cm->name) {
			TRACE_INTERFACE_VTABLE (printf ("[ACTUAL NAMESPACE CHECK FAILED]"));
			return FALSE;
		}
		subname += strlen (ic_name_space);
		if (subname [0] != '.') {
			TRACE_INTERFACE_VTABLE (printf ("[FIRST DOT CHECK FAILED]"));
			return FALSE;
		}
		subname ++;
		if (strstr (subname, ic_name) != subname) {
			TRACE_INTERFACE_VTABLE (printf ("[ACTUAL CLASS NAME CHECK FAILED]"));
			return FALSE;
		}
		subname += strlen (ic_name);
		if (subname [0] != '.') {
			TRACE_INTERFACE_VTABLE (printf ("[SECOND DOT CHECK FAILED]"));
			return FALSE;
		}
		subname ++;
		if (strcmp (subname, im->name) != 0) {
			TRACE_INTERFACE_VTABLE (printf ("[METHOD NAME CHECK FAILED]"));
			return FALSE;
		}

		TRACE_INTERFACE_VTABLE (printf ("[INJECTED INTERFACE CHECK OK]"));
		if (is_wcf_hack_disabled () && !mono_method_can_access_method_full (cm, im, NULL)) {
			char *body_name = mono_method_full_name (cm, TRUE);
			char *decl_name = mono_method_full_name (im, TRUE);
			mono_class_set_type_load_failure (klass, "Method %s overrides method '%s' which is not accessible", body_name, decl_name);
			g_free (body_name);
			g_free (decl_name);
			return FALSE;
		}

		return TRUE;
	}
}

#if (TRACE_INTERFACE_VTABLE_CODE|DEBUG_INTERFACE_VTABLE_CODE)
static void
foreach_override (gpointer key, gpointer value, gpointer user_data)
{
	MonoMethod *method = key;
	MonoMethod *override = value;

	char *method_name = mono_method_get_full_name (method);
	char *override_name = mono_method_get_full_name (override);
	printf ("  Method '%s' has override '%s'\n", method_name, override_name);
	g_free (method_name);
	g_free (override_name);
}

static void
print_overrides (GHashTable *override_map, const char *message)
{
	if (override_map) {
		printf ("Override map \"%s\" START:\n", message);
		g_hash_table_foreach (override_map, foreach_override, NULL);
		printf ("Override map \"%s\" END.\n", message);
	} else {
		printf ("Override map \"%s\" EMPTY.\n", message);
	}
}

static void
print_vtable_full (MonoClass *klass, MonoMethod** vtable, int size, int first_non_interface_slot, const char *message, gboolean print_interfaces)
{
	char *full_name = mono_type_full_name (m_class_get_byval_arg (klass));
	int i;
	int parent_size;

	printf ("*** Vtable for class '%s' at \"%s\" (size %d)\n", full_name, message, size);

	if (print_interfaces) {
		print_implemented_interfaces (klass);
		printf ("* Interfaces for class '%s' done.\nStarting vtable (size %d):\n", full_name, size);
	}

	if (klass->parent) {
		parent_size = klass->parent->vtable_size;
	} else {
		parent_size = 0;
	}
	for (i = 0; i < size; ++i) {
		MonoMethod *cm = vtable [i];
		char *cm_name = cm ? mono_method_full_name (cm, TRUE) : g_strdup ("nil");
		char newness = (i < parent_size) ? 'O' : ((i < first_non_interface_slot) ? 'I' : 'N');

		printf ("  [%c][%03d][INDEX %03d] %s [%p]\n", newness, i, cm ? cm->slot : - 1, cm_name, cm);
		g_free (cm_name);
	}

	g_free (full_name);
}
#endif

#if VERIFY_INTERFACE_VTABLE_CODE
static int
mono_method_try_get_vtable_index (MonoMethod *method)
{
	if (method->is_inflated && (method->flags & METHOD_ATTRIBUTE_VIRTUAL)) {
		MonoMethodInflated *imethod = (MonoMethodInflated*)method;
		if (imethod->declaring->is_generic)
			return imethod->declaring->slot;
	}
	return method->slot;
}

static void
mono_class_verify_vtable (MonoClass *klass)
{
	int i, count;
	char *full_name = mono_type_full_name (m_class_get_byval_arg (klass));

	printf ("*** Verifying VTable of class '%s' \n", full_name);
	g_free (full_name);
	full_name = NULL;

	if (!klass->methods)
		return;

	count = mono_class_get_method_count (klass);
	for (i = 0; i < count; ++i) {
		MonoMethod *cm = klass->methods [i];
		int slot;

		if (!(cm->flags & METHOD_ATTRIBUTE_VIRTUAL))
			continue;

		g_free (full_name);
		full_name = mono_method_full_name (cm, TRUE);

		slot = mono_method_try_get_vtable_index (cm);
		if (slot >= 0) {
			if (slot >= klass->vtable_size) {
				printf ("\tInvalid method %s at index %d with vtable of length %d\n", full_name, slot, klass->vtable_size);
				continue;
			}

			if (slot >= 0 && klass->vtable [slot] != cm && (klass->vtable [slot])) {
				char *other_name = klass->vtable [slot] ? mono_method_full_name (klass->vtable [slot], TRUE) : g_strdup ("[null value]");
				printf ("\tMethod %s has slot %d but vtable has %s on it\n", full_name, slot, other_name);
				g_free (other_name);
			}
		} else
			printf ("\tVirtual method %s does n't have an assigned slot\n", full_name);
	}
	g_free (full_name);
}
#endif

static MonoMethod*
mono_method_get_method_definition (MonoMethod *method)
{
	while (method->is_inflated)
		method = ((MonoMethodInflated*)method)->declaring;
	return method;
}

static gboolean
verify_class_overrides (MonoClass *klass, MonoMethod **overrides, int onum)
{
	int i;

	for (i = 0; i < onum; ++i) {
		MonoMethod *decl = overrides [i * 2];
		MonoMethod *body = overrides [i * 2 + 1];

		if (mono_class_get_generic_type_definition (body->klass) != mono_class_get_generic_type_definition (klass)) {
			mono_class_set_type_load_failure (klass, "Method belongs to a different class than the declared one");
			return FALSE;
		}

		if (m_method_is_static (decl) != m_method_is_static (body)) {
			mono_class_set_type_load_failure (klass, "Static method can't override a non-static method and vice versa.");
			return FALSE;
		}

		if (!m_method_is_virtual (body) && !m_method_is_static (body)) {
			mono_class_set_type_load_failure (klass, "Method must be virtual to override a base type");
			return FALSE;
		}

		if (!m_method_is_virtual (decl)) {
			mono_class_set_type_load_failure (klass, "Cannot override a non virtual method in a base type");
			return FALSE;
		}

		if (!mono_class_is_assignable_from_slow (decl->klass, klass)) {
			mono_class_set_type_load_failure (klass, "Method overrides a class or interface that is not extended or implemented by this type");
			return FALSE;
		}

		body = mono_method_get_method_definition (body);
		decl = mono_method_get_method_definition (decl);

		if (is_wcf_hack_disabled () && !mono_method_can_access_method_full (body, decl, NULL)) {
			char *body_name = mono_method_full_name (body, TRUE);
			char *decl_name = mono_method_full_name (decl, TRUE);
			mono_class_set_type_load_failure (klass, "Method %s overrides method '%s' which is not accessible", body_name, decl_name);
			g_free (body_name);
			g_free (decl_name);
			return FALSE;
		}
	}
	return TRUE;
}

/*Checks if @klass has @parent as one of it's parents type gtd
 *
 * For example:
 * 	Foo<T>
 *	Bar<T> : Foo<Bar<Bar<T>>>
 *
 */
static gboolean
mono_class_has_gtd_parent (MonoClass *klass, MonoClass *parent)
{
	klass = mono_class_get_generic_type_definition (klass);
	parent = mono_class_get_generic_type_definition (parent);
	mono_class_setup_supertypes (klass);
	mono_class_setup_supertypes (parent);

	return klass->idepth >= parent->idepth &&
		mono_class_get_generic_type_definition (klass->supertypes [parent->idepth - 1]) == parent;
}

gboolean
mono_class_check_vtable_constraints (MonoClass *klass, GList *in_setup)
{
	MonoGenericInst *ginst;

	if (!mono_class_is_ginst (klass)) {
		mono_class_setup_vtable_full (klass, in_setup);
		return !mono_class_has_failure (klass);
	}

	mono_class_setup_vtable_full (mono_class_get_generic_type_definition (klass), in_setup);
	if (mono_class_set_type_load_failure_causedby_class (klass, mono_class_get_generic_class (klass)->container_class, "Failed to load generic definition vtable"))
		return FALSE;

	ginst = mono_class_get_generic_class (klass)->context.class_inst;
	for (guint i = 0; i < ginst->type_argc; ++i) {
		MonoClass *arg;
		if (ginst->type_argv [i]->type != MONO_TYPE_GENERICINST)
			continue;
		arg = mono_class_from_mono_type_internal (ginst->type_argv [i]);
		/*Those 2 will be checked by mono_class_setup_vtable itself*/
		if (mono_class_has_gtd_parent (klass, arg) || mono_class_has_gtd_parent (arg, klass))
			continue;
		if (!mono_class_check_vtable_constraints (arg, in_setup)) {
			mono_class_set_type_load_failure (klass, "Failed to load generic parameter %d", i);
			return FALSE;
		}
	}
	return TRUE;
}

/*
 * mono_class_setup_vtable:
 *
 *   Creates the generic vtable of CLASS.
 * Initializes the following fields in MonoClass:
 * - vtable
 * - vtable_size
 * Plus all the fields initialized by setup_interface_offsets ().
 * If there is an error during vtable construction, klass->has_failure
 * is set and details are stored in a MonoErrorBoxed.
 *
 * LOCKING: Acquires the loader lock.
 */
void
mono_class_setup_vtable (MonoClass *klass)
{
	mono_class_setup_vtable_full (klass, NULL);
}

static void
mono_class_setup_vtable_full (MonoClass *klass, GList *in_setup)
{
	ERROR_DECL (error);
	MonoMethod **overrides = NULL;
	MonoGenericContext *context;
	guint32 type_token;
	int onum = 0;

	if (klass->vtable)
		return;

	if (MONO_CLASS_IS_INTERFACE_INTERNAL (klass)) {
		/* This sets method->slot for all methods if this is an interface */
		mono_class_setup_methods (klass);
		return;
	}

	if (mono_class_has_failure (klass))
		return;

	if (g_list_find (in_setup, klass))
		return;

	mono_loader_lock ();

	if (klass->vtable) {
		mono_loader_unlock ();
		return;
	}

	UnlockedIncrement (&mono_stats.generic_vtable_count);
	in_setup = g_list_prepend (in_setup, klass);

	if (mono_class_is_ginst (klass)) {
		if (!mono_class_check_vtable_constraints (klass, in_setup)) {
			mono_loader_unlock ();
			g_list_remove (in_setup, klass);
			return;
		}

		context = mono_class_get_context (klass);
		type_token = mono_class_get_generic_class (klass)->container_class->type_token;
	} else {
		MonoGenericContainer *container = mono_class_try_get_generic_container (klass); //FIXME is this a case of a try?
		context = container ? &container->context : NULL;
		type_token = klass->type_token;
	}

	if (image_is_dynamic (klass->image)) {
		/* Generic instances can have zero method overrides without causing any harm.
		 * This is true since we don't do layout all over again for them, we simply inflate
		 * the layout of the parent.
		 */
		mono_reflection_get_dynamic_overrides (klass, &overrides, &onum, error);
		if (!is_ok (error)) {
			mono_class_set_type_load_failure (klass, "Could not load list of method overrides due to %s", mono_error_get_message (error));
			goto done;
		}
	} else {
		/* The following call fails if there are missing methods in the type */
		/* FIXME it's probably a good idea to avoid this for generic instances. */
		mono_class_get_overrides_full (klass->image, type_token, &overrides, &onum, context, error);
		if (!is_ok (error)) {
			mono_class_set_type_load_failure (klass, "Could not load list of method overrides due to %s", mono_error_get_message (error));
			goto done;
		}
	}

	mono_class_setup_vtable_general (klass, overrides, onum, in_setup);

done:
	g_free (overrides);
	mono_error_cleanup (error);

	mono_loader_unlock ();
	g_list_remove (in_setup, klass);

	return;
}

gboolean
mono_class_setup_need_stelemref_method (MonoClass *klass)
{
	return klass->rank == 1 && MONO_TYPE_IS_REFERENCE (m_class_get_byval_arg (m_class_get_element_class (klass)));
}

static int
apply_override (MonoClass *klass, MonoClass *override_class, MonoMethod **vtable, MonoMethod *decl, MonoMethod *override,
				GHashTable **override_map, GHashTable **override_class_map, GHashTable **conflict_map)
{
	int dslot;
	dslot = mono_method_get_vtable_slot (decl);
	if (dslot == -1) {
		mono_class_set_type_load_failure (klass, "");
		return FALSE;
	}

	dslot += mono_class_interface_offset (klass, decl->klass);

	//check if the override comes from an interface and the overrided method is from a class, if this is the case it shouldn't be changed
	if (vtable [dslot] && vtable [dslot]->klass && MONO_CLASS_IS_INTERFACE_INTERNAL (override->klass) && !MONO_CLASS_IS_INTERFACE_INTERNAL (vtable [dslot]->klass))
		return TRUE;

	vtable [dslot] = override;
	if (!MONO_CLASS_IS_INTERFACE_INTERNAL (override->klass)) {
		/*
		 * If override from an interface, then it is an override of a default interface method,
		 * don't override its slot.
		 */
		vtable [dslot]->slot = dslot;
	}

	if (!*override_map) {
		*override_map = g_hash_table_new (mono_aligned_addr_hash, NULL);
		*override_class_map = g_hash_table_new (mono_aligned_addr_hash, NULL);
	}
	GHashTable *map = *override_map;
	GHashTable *class_map = *override_class_map;

	MonoMethod *prev_override = (MonoMethod*)g_hash_table_lookup (map, decl);
	MonoClass *prev_override_class = (MonoClass*)g_hash_table_lookup (class_map, decl);

	g_assert (override_class == override->klass);

	g_hash_table_insert (map, decl, override);
	g_hash_table_insert (class_map, decl, override_class);

	/* Collect potentially conflicting overrides which are introduced by default interface methods */
	if (prev_override) {
		g_assert (prev_override->klass == prev_override_class);

		if (!*conflict_map)
			*conflict_map = g_hash_table_new (mono_aligned_addr_hash, NULL);
		GHashTable *cmap = *conflict_map;
		GSList *entries = (GSList*)g_hash_table_lookup (cmap, decl);
		if (!(decl->flags & METHOD_ATTRIBUTE_ABSTRACT))
			entries = g_slist_prepend (entries, decl);
		entries = g_slist_prepend (entries, prev_override);
		entries = g_slist_prepend (entries, override);

		g_hash_table_insert (cmap, decl, entries);
	}

	return TRUE;
}

static void
handle_dim_conflicts (MonoMethod **vtable, MonoClass *klass, GHashTable *conflict_map)
{
	GHashTableIter iter;
	MonoMethod *decl;
	GSList *entries, *l, *l2;
	GSList *dim_conflicts = NULL;

	g_hash_table_iter_init (&iter, conflict_map);
	while (g_hash_table_iter_next (&iter, (gpointer*)&decl, (gpointer*)&entries)) {
		/*
		 * Iterate over the candidate methods, remove ones whose class is less concrete than the
		 * class of another one.
		 */
		/* This is O(n^2), but that shouldn't be a problem in practice */
		for (l = entries; l; l = l->next) {
			for (l2 = entries; l2; l2 = l2->next) {
				MonoMethod *m1 = (MonoMethod*)l->data;
				MonoMethod *m2 = (MonoMethod*)l2->data;
				if (!m1 || !m2 || m1 == m2)
					continue;
				if (mono_class_is_assignable_from_internal (m1->klass, m2->klass))
					l->data = NULL;
				else if (mono_class_is_assignable_from_internal (m2->klass, m1->klass))
					l2->data = NULL;
			}
		}
		int nentries = 0;
		MonoMethod *impl = NULL;
		for (l = entries; l; l = l->next) {
			if (l->data && l->data != impl) {
				nentries ++;
				impl = (MonoMethod*)l->data;
			}
		}
		if (nentries > 1) {
			/* If more than one method is left, we have a conflict */
			if (decl->is_inflated)
				decl = ((MonoMethodInflated*)decl)->declaring;
			dim_conflicts = g_slist_prepend (dim_conflicts, decl);
			/*
			  for (l = entries; l; l = l->next) {
			  if (l->data)
			  printf ("%s %s %s\n", mono_class_full_name (klass), mono_method_full_name (decl, TRUE), mono_method_full_name (l->data, TRUE));
			  }
			*/
		} else {
			/*
			 * Use the implementing method computed above instead of the already
			 * computed one, which depends on interface ordering.
			 */
			int ic_offset = mono_class_interface_offset (klass, decl->klass);
			int im_slot = ic_offset + decl->slot;
			vtable [im_slot] = impl;
		}
		g_slist_free (entries);
	}
	if (dim_conflicts) {
		mono_loader_lock ();
		klass->has_dim_conflicts = 1;
		mono_loader_unlock ();

		/*
		 * Exceptions are thrown at method call time and only for the methods which have
		 * conflicts, so just save them in the class.
		 */

		/* Make a copy of the list from the class mempool */
		GSList *conflicts = (GSList*)mono_class_alloc0 (klass, g_slist_length (dim_conflicts) * sizeof (GSList));
		int i = 0;
		for (l = dim_conflicts; l; l = l->next) {
			conflicts [i].data = l->data;
			conflicts [i].next = &conflicts [i + 1];
			i ++;
		}
		conflicts [i - 1].next = NULL;

		mono_class_set_dim_conflicts (klass, conflicts);
		g_slist_free (dim_conflicts);
	}
}

static void
print_unimplemented_interface_method_info (MonoClass *klass, MonoClass *ic, MonoMethod *im, int im_slot, MonoMethod **overrides, int onum)
{
	int index, mcount;
	char *method_signature;
	char *type_name;

	for (index = 0; index < onum; ++index) {
		mono_trace_warning (MONO_TRACE_TYPE, " at slot %d: %s (%d) overrides %s (%d)", im_slot, overrides [index*2+1]->name,
			 overrides [index*2+1]->slot, overrides [index*2]->name, overrides [index*2]->slot);
	}
	method_signature = mono_signature_get_desc (mono_method_signature_internal (im), FALSE);
	type_name = mono_type_full_name (m_class_get_byval_arg (klass));
	mono_trace_warning (MONO_TRACE_TYPE, "no implementation for interface method %s::%s(%s) in class %s",
			    mono_type_get_name (m_class_get_byval_arg (ic)), im->name, method_signature, type_name);
	g_free (method_signature);
	g_free (type_name);
	mono_class_setup_methods (klass);
	if (mono_class_has_failure (klass)) {
		char *name = mono_type_get_full_name (klass);
		mono_trace_warning (MONO_TRACE_TYPE, "CLASS %s failed to resolve methods", name);
		g_free (name);
		return;
	}
	mcount = mono_class_get_method_count (klass);
	for (index = 0; index < mcount; ++index) {
		MonoMethod *cm = klass->methods [index];
		method_signature = mono_signature_get_desc (mono_method_signature_internal (cm), TRUE);

		mono_trace_warning (MONO_TRACE_TYPE, "METHOD %s(%s)", cm->name, method_signature);
		g_free (method_signature);
	}
}

/*
 * mono_class_get_virtual_methods:
 *
 *   Iterate over the virtual methods of KLASS.
 *
 * LOCKING: Assumes the loader lock is held (because of the klass->methods check).
 */
static MonoMethod*
mono_class_get_virtual_methods (MonoClass* klass, gpointer *iter)
{
	// FIXME move state to caller
	gboolean static_iter = FALSE;

	if (!iter)
		return NULL;

	/*
	 * If the lowest bit of the iterator is 1, this is an iterator for static metadata,
	 * and the upper bits contain an index. Otherwise, the iterator is a pointer into
	 * klass->methods.
	 */
	if ((gsize)(*iter) & 1)
		static_iter = TRUE;
	/* Use the static metadata only if klass->methods is not yet initialized */
	if (!static_iter && !(klass->methods || !MONO_CLASS_HAS_STATIC_METADATA (klass)))
		static_iter = TRUE;

	if (!static_iter) {
		MonoMethod** methodptr;

		if (!*iter) {
			mono_class_setup_methods (klass);
			/*
			 * We can't fail lookup of methods otherwise the runtime will burst in flames on all sort of places.
			 * FIXME we should better report this error to the caller
			 */
			if (!klass->methods)
				return NULL;
			/* start from the first */
			methodptr = &klass->methods [0];
		} else {
			methodptr = (MonoMethod **)*iter;
			methodptr++;
		}
		if (*iter)
			g_assert ((guint64)(*iter) > 0x100);
		int mcount = mono_class_get_method_count (klass);
		while (methodptr < &klass->methods [mcount]) {
			if (*methodptr && ((*methodptr)->flags & METHOD_ATTRIBUTE_VIRTUAL))
				break;
			methodptr ++;
		}
		if (methodptr < &klass->methods [mcount]) {
			*iter = methodptr;
			return *methodptr;
		} else {
			return NULL;
		}
	} else {
		/* Search directly in metadata to avoid calling setup_methods () */
		MonoMethod *res = NULL;
		int i, start_index;

		if (!*iter) {
			start_index = 0;
		} else {
			start_index = GPOINTER_TO_UINT (*iter) >> 1;
		}

		int first_idx = mono_class_get_first_method_idx (klass);
		int mcount = mono_class_get_method_count (klass);
		for (i = start_index; i < mcount; ++i) {
			guint32 flags;

			/* first_idx points into the methodptr table */
			flags = mono_metadata_decode_table_row_col (klass->image, MONO_TABLE_METHOD, first_idx + i, MONO_METHOD_FLAGS);

			if (flags & METHOD_ATTRIBUTE_VIRTUAL)
				break;
		}

		if (i < mcount) {
			ERROR_DECL (error);
			res = mono_get_method_checked (klass->image, MONO_TOKEN_METHOD_DEF | (first_idx + i + 1), klass, NULL, error);
			mono_error_cleanup (error); /* FIXME don't swallow the error */

			/* Add 1 here so the if (*iter) check fails */
			*iter  = GUINT_TO_POINTER (((i + 1) << 1) | 1);
			return res;
		} else {
			return NULL;
		}
	}
}

static void
print_vtable_layout_result (MonoClass *klass, MonoMethod **vtable, int cur_slot)
{
	int icount = 0;

	print_implemented_interfaces (klass);

	for (guint32 i = 0; i <= klass->max_interface_id; i++)
		if (MONO_CLASS_IMPLEMENTS_INTERFACE (klass, i))
			icount++;

	printf ("VTable %s (vtable entries = %d, interfaces = %d)\n", mono_type_full_name (m_class_get_byval_arg (klass)),
		klass->vtable_size, icount);

	for (int i = 0; i < cur_slot; ++i) {
		MonoMethod *cm;

		cm = vtable [i];
		if (cm) {
			printf ("  slot assigned: %03d, slot index: %03d %s\n", i, cm->slot,
				mono_method_get_full_name (cm));
		} else {
			printf ("  slot assigned: %03d, <null>\n", i);
		}
	}


	if (icount) {
		printf ("Interfaces %s.%s (max_iid = %d)\n", klass->name_space,
			klass->name, klass->max_interface_id);

		for (guint16 i = 0; i < klass->interface_count; i++) {
			MonoClass *ic = klass->interfaces [i];
			printf ("  slot offset: %03d, method count: %03d, iid: %03d %s\n",
				mono_class_interface_offset (klass, ic),
				mono_class_setup_count_virtual_methods (ic), ic->interface_id, mono_type_full_name (m_class_get_byval_arg (ic)));
		}

		for (MonoClass *k = klass->parent; k ; k = k->parent) {
			for (guint16 i = 0; i < k->interface_count; i++) {
				MonoClass *ic = k->interfaces [i];
				printf ("  parent slot offset: %03d, method count: %03d, iid: %03d %s\n",
					mono_class_interface_offset (klass, ic),
					mono_class_setup_count_virtual_methods (ic), ic->interface_id, mono_type_full_name (m_class_get_byval_arg (ic)));
			}
		}
	}
}

/*
 * LOCKING: this is supposed to be called with the loader lock held.
 */
static int
setup_class_vtsize (MonoClass *klass, GList *in_setup, int *cur_slot, int *stelemref_slot, MonoError *error)
{
	GPtrArray *ifaces = NULL;
	int max_vtsize = 0;
	ifaces = mono_class_get_implemented_interfaces (klass, error);
	if (!is_ok (error)) {
		char *name = mono_type_get_full_name (klass);
		mono_class_set_type_load_failure (klass, "Could not resolve %s interfaces due to %s", name, mono_error_get_message (error));
		g_free (name);
		mono_error_cleanup (error);
		return -1;
	} else if (ifaces) {
		for (guint i = 0; i < ifaces->len; i++) {
			MonoClass *ic = (MonoClass *)g_ptr_array_index (ifaces, i);
			max_vtsize += mono_class_get_method_count (ic);
		}
		g_ptr_array_free (ifaces, TRUE);
		ifaces = NULL;
	}

	if (klass->parent) {
		mono_class_init_internal (klass->parent);
		mono_class_setup_vtable_full (klass->parent, in_setup);

		if (mono_class_set_type_load_failure_causedby_class (klass, klass->parent, "Parent class failed to load"))
			return -1;

		max_vtsize += klass->parent->vtable_size;
		*cur_slot = klass->parent->vtable_size;
	}

	max_vtsize += mono_class_get_method_count (klass);

	/*Array have a slot for stelemref*/
	if (mono_class_setup_need_stelemref_method (klass)) {
		*stelemref_slot = *cur_slot;
		++max_vtsize;
		++*cur_slot;
	}
	return max_vtsize;
}

/*
 * LOCKING: this is supposed to be called with the loader lock held.
 */
static void
mono_class_setup_vtable_ginst (MonoClass *klass, GList *in_setup)
{
	ERROR_DECL (error);
	int i;
	MonoClass *gklass = mono_class_get_generic_class (klass)->container_class;
	MonoMethod **tmp;

	mono_class_setup_vtable_full (gklass, in_setup);
	if (mono_class_set_type_load_failure_causedby_class (klass, gklass, "Could not load generic definition"))
		return;

	tmp = (MonoMethod **)mono_class_alloc0 (klass, sizeof (gpointer) * gklass->vtable_size);
	klass->vtable_size = gklass->vtable_size;
	for (i = 0; i < gklass->vtable_size; ++i)
		if (gklass->vtable [i]) {
			MonoMethod *inflated = mono_class_inflate_generic_method_full_checked (gklass->vtable [i], klass, mono_class_get_context (klass), error);
			if (!is_ok (error))	{
				char *name = mono_type_get_full_name (klass);
				mono_class_set_type_load_failure (klass, "VTable setup of type %s failed due to: %s", name, mono_error_get_message (error));
				mono_error_cleanup (error);
				g_free (name);
				return;
			}
			tmp [i] = inflated;
			tmp [i]->slot = gklass->vtable [i]->slot;
		}
	mono_memory_barrier ();
	klass->vtable = tmp;

	mono_loader_lock ();
	klass->has_dim_conflicts = gklass->has_dim_conflicts;
	mono_loader_unlock ();

	/* Have to set method->slot for abstract virtual methods */
	if (klass->methods && gklass->methods) {
		int mcount = mono_class_get_method_count (klass);
		for (i = 0; i < mcount; ++i)
			if (klass->methods [i]->slot == -1)
				klass->methods [i]->slot = gklass->methods [i]->slot;
	}

	if (mono_print_vtable)
		print_vtable_layout_result (klass, klass->vtable, gklass->vtable_size);

}

#ifdef FEATURE_COVARIANT_RETURNS
/*
 * vtable_slot_has_preserve_base_overrides_attribute:
 *
 * Needs to walk up the class hierarchy looking for the methods in this slot to
 * see if any are tagged with PreserveBaseOverrideAttribute.
 */
static gboolean
vtable_slot_has_preserve_base_overrides_attribute (MonoClass *klass, int slot, MonoClass **out_klass)
{
	/*
	 * FIXME: it's slow to do this loop every time.  A faster way would be
	 * to hang a boolean in the image of the parent class if it or any of
	 * its ancestors have the attribute.
	 */

	for (; klass; klass = klass->parent) {
		if (slot >= klass->vtable_size)
			break;
		MonoMethod *method = klass->vtable [slot];

		/* FIXME: for abstract classes, do we put abstract methods here? */
		if (method && mono_class_setup_method_has_preserve_base_overrides_attribute (method)) {
			if (out_klass)
				*out_klass = klass;
			return TRUE;
		}
	}
	return FALSE;
}

/**
 * is_ok_for_covariant_ret:
 *
 * Returns TRUE if the given pair of types are a valid return type for the covariant returns feature.
 *
 * This is the CanCastTo relation from ECMA (impl type can be cast to the decl
 * type), except that we don't allow a valuetype to be cast to one of its
 * implemented interfaces, and we don't allow T to Nullable<T>.
 */
static gboolean
is_ok_for_covariant_ret (MonoType *type_impl, MonoType *type_decl)
{
	if (m_type_is_byref (type_impl) ^ m_type_is_byref (type_decl))
		return FALSE;

	if (m_type_is_byref (type_impl)) {
		return mono_byref_type_is_assignable_from (type_decl, type_impl, TRUE);
	}

	MonoClass *class_impl = mono_class_from_mono_type_internal (type_impl);

	/* method declared to return an interface, impl returns a value type that implements the interface */
	if (m_class_is_valuetype (class_impl) && mono_type_is_reference (type_decl))
		return FALSE;


	TRACE_INTERFACE_VTABLE (do {
			char *decl_str = mono_type_full_name (type_decl);
			char *impl_str = mono_type_full_name (type_impl);
			printf ("Checking if %s is assignable from %s", decl_str, impl_str);
			g_free (decl_str);
			g_free (impl_str);
		} while (0));

	MonoClass *class_decl = mono_class_from_mono_type_internal (type_decl);

	/* Also disallow overriding a Nullable<T> return with an impl that
	 * returns T */
	if (mono_class_is_nullable (class_decl) &&
	    mono_class_get_nullable_param_internal (class_decl) == class_impl)
		return FALSE;


	ERROR_DECL (local_error);
	gboolean result = FALSE;
	mono_class_signature_is_assignable_from (class_decl, class_impl, &result, local_error);
	mono_error_cleanup (local_error);
	return result;
}

/**
 * signature_is_subsumed:
 * \param impl_method method that implements the override. defined in \p klass
 * \param decl_method method that is being overridden.
 *
 * Check that \p impl_method has a signature that is subsumed by the signature of \p decl_method.  That is,
 *  that the argument types all match and that the return type of \p impl_method is castable to the return type of \p decl_method, except that
 *  both must not be valuetypes or interfaces.
 *
 * Returns \c TRUE if \p impl_method is subsumed by \p decl_method, or FALSE otherwise.  On error sets \p error and returns \c FALSE
 *
 * Note that \p decl_method may not be the explicitly overridden method as specified by an .override in the class where \p impl_method is defined,
 * but rather some method on an ancestor class that is itself a previous override.
 *
 * class C {
 *   public virtual C Foo ();
 * }
 * class B : C {
 *   public virtual newslot B Foo () {
 *      .PreserveBaseOverrideAttribute;
 *      .override C C::Foo ();
 *   }
 * }
 * class A : B {
 *   public virtual newslot C Foo () {
 *     .override C C::Foo (); // type load error - it should be a subclass of B because of B's override of C::Foo
 *   }
 * }
 */
static gboolean
signature_is_subsumed (MonoMethod *impl_method, MonoMethod *decl_method, MonoError *error)
{
	MonoMethodSignature *impl_sig = mono_method_signature_internal (impl_method);
	MonoMethodSignature *decl_sig = mono_method_signature_internal (decl_method);

	if (mono_metadata_signature_equal (impl_sig, decl_sig))
		return TRUE;

	if (!mono_metadata_signature_equal_no_ret (impl_sig, decl_sig))
		return FALSE;

	MonoType *impl_ret = impl_sig->ret;
	MonoType *decl_ret_0 = decl_sig->ret;

	/*
	 * Comparing return types of generic methods:
	 *
	 * Suppose decl is a generic method RetTy0`2[!!0,!!1] Method<T1,T2> (DeclArgTys...)
	 * we need to check that:
	 *
	 * 1. impl is a generic method with the same number of type arguments
	 *    ImplRetTy Method<S1,S2> (ImplArgTys...)
         *    (because the number of generic arguments isn't allowed to change)
	 * 2. Inflate RetTy0`2[T1,T2] with S1,S2 for T1,T2 to get RetTy0`2[S1,S2]
	 * 3. Compare if ImplRetTy is assignable to RetTy0`2[S1,S2]
	 *
	 * (For example the decl method might return IReadOnlyDictionary<K,V>
	 *  Foo<K,V>() and the impl method might be SortedList<A,B> Foo<A,B>(),
	 *  so we want to check that SortedList<A,B> is assignable to
	 *  IReadOnlyDictionary<A,B>.  If we naively check SortedList<A,B>
	 *  against IReadOnlyDictionary<K,V> it will fail because the
	 *  implementation of the assignable relation won't consider K and A
	 *  equal)
	 *
	 * It's possible that actually mono_metadata_signature_equal will work
	 * (because it does a signature check which considers !!0 equal to !!0
	 * even if they come from different generic containers), so if the
	 * return types are identical, we won't even get here, but for the
	 * return type assignable checking, we need to inflate RetTy0 properly.
	 */

	/* We only inflate the return type, not the entire method signature,
	 * because the signature_equal_no_ret check above can compare
	 * corresponding positional type parameters even if they come from
	 * different generic methods.  So since the arguments aren't allowed to
	 * vary at all, we know they were already identical and we only need to
	 * compare return types.
	 */

	/* both have to be non-generic, or both generic */
	if ((impl_method->is_generic || decl_method->is_generic) &&
	    !!impl_method->is_generic != !!decl_method->is_generic)
		return FALSE;

	MonoType *decl_ret;
	MonoType *alloc_decl_ret = NULL;
	if (!impl_method->is_generic) {
		decl_ret = decl_ret_0;
	} else {
		g_assert (decl_method->is_generic);

		MonoGenericContainer *impl_container = mono_method_get_generic_container (impl_method);
		MonoGenericContainer *decl_container = mono_method_get_generic_container (decl_method);

		g_assert (decl_container != NULL);
		g_assert (impl_container != NULL);

		if (impl_container->type_argc != decl_container->type_argc)
			return FALSE;

		/* inflate decl's return type with the type parameters of impl */
		alloc_decl_ret = mono_class_inflate_generic_type_checked (decl_ret_0, &impl_container->context, error);
		return_val_if_nok (error, FALSE);
		decl_ret = alloc_decl_ret;
	}

	gboolean result = is_ok_for_covariant_ret (impl_ret, decl_ret);
	if (alloc_decl_ret)
		mono_metadata_free_type (alloc_decl_ret);
	return result;
}

static gboolean
check_signature_covariant (MonoClass *klass, MonoMethod *impl, MonoMethod *decl)
{
	TRACE_INTERFACE_VTABLE (printf (" checking covariant signature compatibility on behalf of %s: '%s' overriding '%s'\n", mono_type_full_name (m_class_get_byval_arg (klass)), mono_method_full_name (impl, 1), mono_method_full_name (decl, 1)));
	ERROR_DECL (local_error);
	gboolean subsumed = signature_is_subsumed (impl, decl, local_error);
	if (!is_ok (local_error) || !subsumed) {
		const gboolean print_sig = TRUE;
		const gboolean print_ret_type = TRUE;
		char *decl_method_name = mono_method_get_name_full (decl, print_sig, print_ret_type, MONO_TYPE_NAME_FORMAT_IL);
		char *impl_method_name = mono_method_get_name_full (impl, print_sig, print_ret_type, MONO_TYPE_NAME_FORMAT_IL);
		const char *msg;
		if (!is_ok (local_error)) {
			msg = mono_error_get_message (local_error);
		} else {
			msg = "but with an incompatible signature";
		}
		mono_class_set_type_load_failure (klass, "Method '%s' overrides method '%s', %s",
						  impl_method_name, decl_method_name, msg);
		mono_error_cleanup (local_error);
		g_free (decl_method_name);
		g_free (impl_method_name);
		return FALSE;
	}
	return TRUE;
}

/**
 * check_vtable_covariant_override_impls:
 * \param klass the class
 * \param vtable the provisional vtable
 * \param vtable_size the number of slots in the vtable
 *
 * Given a provisional vtable for a class, check that any methods that come from \p klass that have the \c
 * MonoMethodDefInfrequentBits:is_covariant_override_impl bit have the most specific signature of any method in that
 * slot in the ancestor classes.  This checks that if the current class is overriding a method, it is doing so with the
 * most specific signature.
 *
 * Because classes can override methods either explicitly using an .override directive or implicitly by matching the
 * signature of some virtual method of some ancestor class, you could get into a situation where a class incorrectly
 * overrides a method with a less specific signature.
 *
 * For example:
 *
 * class A {
 *    public virtual A Foo ();
 * }
 * class B : A {
 *    public override B Foo (); // covariant return override
 * }
 * class C : B {
 *   public override A Foo (); // incorrect
 * }
 */
static gboolean
check_vtable_covariant_override_impls (MonoClass *klass, MonoMethod **vtable, int vtable_size)
{
	MonoClass *parent_class = klass->parent;
	if (!parent_class)
		return TRUE;

	/* we only need to check the slots that the parent class has, too. Everything else is new. */
	for (int slot = 0; slot < parent_class->vtable_size; ++slot) {
		MonoMethod *impl = vtable[slot];
		if (!impl || !mono_method_get_is_covariant_override_impl (impl) || impl->klass != klass)
			continue;
		MonoMethod *last_checked_prev_override = NULL;
		for (MonoClass *cur_class = parent_class; cur_class ; cur_class = cur_class->parent) {
			if (slot >= cur_class->vtable_size)
				break;
			MonoMethod *prev_impl = cur_class->vtable[slot];

			// if the current class re-abstracted the method, it may not be there.
			if (!prev_impl)
				continue;

			if (prev_impl != last_checked_prev_override) {
				/*
				 * the new impl should be subsumed by the prior one, ie this
				 * newest leaf class provides the most specific implementation
				 * of the method of any of its ancestor classes.
				 */
				/*
				 * but as we go up the inheritance hierarchy only check if the
				 * prev_impl method is changing.  If it's the same one in the
				 * slot the whole time, don't bother checking it over and
				 * over.
				 */
				/* FIXME: do we need to check all the way up? can we just check the most derived one? */
				if (!check_signature_covariant (klass, impl, prev_impl))
					return FALSE;
				last_checked_prev_override = prev_impl;
			}
		}
	}
	return TRUE;
}
#endif /* FEATURE_COVARIANT_RETURNS */

/*
 * LOCKING: this is supposed to be called with the loader lock held.
 */
void
mono_class_setup_vtable_general (MonoClass *klass, MonoMethod **overrides, int onum, GList *in_setup)
{
	ERROR_DECL (error);
	MonoClass *k, *ic;
	MonoMethod **vtable = NULL;
	int max_vtsize = 0, cur_slot = 0;
	GHashTable *override_map = NULL;
	GHashTable *override_class_map = NULL;
	GHashTable *conflict_map = NULL;
#if (DEBUG_INTERFACE_VTABLE_CODE|TRACE_INTERFACE_VTABLE_CODE)
	int first_non_interface_slot;
#endif
	GSList *virt_methods = NULL, *l;
	int stelemref_slot = 0;

	if (klass->vtable)
		return;

	if (overrides && !verify_class_overrides (klass, overrides, onum))
		return;

	max_vtsize = setup_class_vtsize (klass, in_setup,  &cur_slot, &stelemref_slot, error);
	if (max_vtsize == -1)
		return;

	cur_slot = mono_class_setup_interface_offsets_internal (klass, cur_slot, MONO_SETUP_ITF_OFFSETS_OVERWRITE);
	if (cur_slot == -1) /*setup_interface_offsets fails the type.*/
		return;

	DEBUG_INTERFACE_VTABLE (first_non_interface_slot = cur_slot);

	/* Optimized version for generic instances */
	if (mono_class_is_ginst (klass)) {
		mono_class_setup_vtable_ginst (klass, in_setup);
		return;
	}

	vtable = (MonoMethod **)g_malloc0 (sizeof (gpointer) * max_vtsize);

	if (klass->parent && klass->parent->vtable_size)
		memcpy (vtable,  klass->parent->vtable,  sizeof (gpointer) *  klass->parent->vtable_size);

	/*Array have a slot for stelemref*/
	if (mono_class_setup_need_stelemref_method (klass)) {
		MonoMethod *method = mono_marshal_get_virtual_stelemref (klass);
		if (!method->slot)
			method->slot = stelemref_slot;
		else
			g_assert (method->slot == stelemref_slot);

		vtable [stelemref_slot] = method;
	}

	TRACE_INTERFACE_VTABLE (print_vtable_full (klass, vtable, cur_slot, first_non_interface_slot, "AFTER INHERITING PARENT VTABLE", TRUE));

	/* Process overrides from interface default methods */
	// FIXME: Ordering between interfaces
	for (int ifindex = 0; ifindex < klass->interface_offsets_count; ifindex++) {
		ic = klass->interfaces_packed [ifindex];

		mono_class_setup_methods (ic);
		if (mono_class_has_failure (ic))
			goto fail;

		MonoMethod **iface_overrides;
		int iface_onum;
		mono_class_get_overrides_full (ic->image, ic->type_token, &iface_overrides, &iface_onum, mono_class_get_context (ic), error);
		goto_if_nok (error, fail);
		for (int i = 0; i < iface_onum; i++) {
			MonoMethod *decl = iface_overrides [i*2];
			MonoMethod *override = iface_overrides [i*2 + 1];
			if (mono_class_is_gtd (override->klass)) {
				override = mono_class_inflate_generic_method_full_checked (override, ic, mono_class_get_context (ic), error);
			} 
			// there used to be code here to inflate decl if decl->is_inflated, but in https://github.com/dotnet/runtime/pull/64102#discussion_r790019545 we
			// think that this does not correspond to any real code.
			if (!apply_override (klass, ic, vtable, decl, override, &override_map, &override_class_map, &conflict_map))
				goto fail;
		}
		g_free (iface_overrides);
	}

	/* override interface methods */
	for (int i = 0; i < onum; i++) {
		MonoMethod *decl = overrides [i*2];
		MonoMethod *override = overrides [i*2 + 1];
		if (MONO_CLASS_IS_INTERFACE_INTERNAL (decl->klass)) {
			/*
			 * We expect override methods that are part of a generic definition, to have
			 * their parent class be the actual interface/class containing the override,
			 * i.e.
			 *
			 * IFace<T> in:
			 * class Foo<T> : IFace<T>
			 *
			 * This is needed so the mono_class_is_assignable_from_internal () calls in the
			 * conflict resolution work.
			 */
			g_assert (override->klass == klass);
			if (!apply_override (klass, klass, vtable, decl, override, &override_map, &override_class_map, &conflict_map))
				goto fail;
		}
	}

	TRACE_INTERFACE_VTABLE (print_overrides (override_map, "AFTER OVERRIDING INTERFACE METHODS"));
	TRACE_INTERFACE_VTABLE (print_vtable_full (klass, vtable, cur_slot, first_non_interface_slot, "AFTER OVERRIDING INTERFACE METHODS", FALSE));

	/*
	 * Create a list of virtual methods to avoid calling
	 * mono_class_get_virtual_methods () which is slow because of the metadata
	 * optimization.
	 */
	{
		gpointer iter = NULL;
		MonoMethod *cm;

		virt_methods = NULL;
		while ((cm = mono_class_get_virtual_methods (klass, &iter))) {
			virt_methods = g_slist_prepend (virt_methods, cm);
		}
		if (mono_class_has_failure (klass))
			goto fail;
	}

	// Loop on all implemented interfaces...
	for (int i = 0; i < klass->interface_offsets_count; i++) {
		MonoClass *parent = klass->parent;
		int ic_offset;
		gboolean interface_is_explicitly_implemented_by_class;
		gboolean variant_itf = FALSE;
		int im_index;

		ic = klass->interfaces_packed [i];
		ic_offset = mono_class_interface_offset (klass, ic);

		mono_class_setup_methods (ic);
		if (mono_class_has_failure (ic))
			goto fail;

		// Check if this interface is explicitly implemented (instead of just inherited)
		if (parent != NULL) {
			int implemented_interfaces_index;
			interface_is_explicitly_implemented_by_class = FALSE;
			variant_itf = mono_class_has_variant_generic_params (ic);
			for (implemented_interfaces_index = 0; implemented_interfaces_index < klass->interface_count; implemented_interfaces_index++) {
				if (ic == klass->interfaces [implemented_interfaces_index]) {
					interface_is_explicitly_implemented_by_class = TRUE;
					break;
				}
				if (variant_itf) {
					if (mono_class_is_variant_compatible (ic, klass->interfaces [implemented_interfaces_index], FALSE)) {
						MonoClass *impl_itf;
						(void)impl_itf; // conditionally used
						impl_itf = klass->interfaces [implemented_interfaces_index];
						TRACE_INTERFACE_VTABLE (printf ("  variant interface '%s' is explicitly implemented by '%s'\n", mono_type_full_name (m_class_get_byval_arg (ic)), mono_type_full_name (m_class_get_byval_arg (impl_itf))));
						interface_is_explicitly_implemented_by_class = TRUE;
						break;
					}
				}
			}
		} else {
			interface_is_explicitly_implemented_by_class = TRUE;
		}

		// Loop on all interface methods...
		int mcount = mono_class_get_method_count (ic);
		for (im_index = 0; im_index < mcount; im_index++) {
			MonoMethod *im = ic->methods [im_index];
			int im_slot = ic_offset + im->slot;
			MonoMethod *override_im = (override_map != NULL) ? (MonoMethod *)g_hash_table_lookup (override_map, im) : NULL;

			if (!m_method_is_virtual (im))
				continue;

			TRACE_INTERFACE_VTABLE (printf ("\tchecking iface method %s\n", mono_method_full_name (im,1)));

			if (override_im == NULL || (override_im && MONO_CLASS_IS_INTERFACE_INTERNAL(override_im->klass))) {
				int cm_index;
				MonoMethod *cm;

				// First look for a suitable method among the class methods
				for (l = virt_methods; l; l = l->next) {
					cm = (MonoMethod *)l->data;
					TRACE_INTERFACE_VTABLE (printf ("    For slot %d ('%s'.'%s':'%s'), trying method '%s'.'%s':'%s'... [EXPLICIT IMPLEMENTATION = %d][SLOT IS NULL = %d]", im_slot, ic->name_space, ic->name, im->name, cm->klass->name_space, cm->klass->name, cm->name, interface_is_explicitly_implemented_by_class, (vtable [im_slot] == NULL)));
					int flags;
					flags = MONO_ITF_OVERRIDE_REQUIRE_NEWSLOT;
					if (interface_is_explicitly_implemented_by_class)
						flags |= MONO_ITF_OVERRIDE_EXPLICITLY_IMPLEMENTED;
					if (interface_is_explicitly_implemented_by_class && variant_itf)
						flags |= MONO_ITF_OVERRIDE_VARIANT_ITF;
					// if the slot is emtpy, or it's filled with a DIM, treat it as empty
					if (vtable [im_slot] == NULL || m_class_is_interface (vtable [im_slot]->klass))
						flags |= MONO_ITF_OVERRIDE_SLOT_EMPTY;
					if (check_interface_method_override (klass, im, cm, flags)) {
						TRACE_INTERFACE_VTABLE (printf ("[check ok]: ASSIGNING\n"));
						vtable [im_slot] = cm;
						/* Why do we need this? */
						if (cm->slot < 0) {
							cm->slot = im_slot;
						}
						if (conflict_map)
							g_hash_table_remove(conflict_map, im);
						break;
					}
					TRACE_INTERFACE_VTABLE (printf ("\n"));
					if (mono_class_has_failure (klass))  /*Might be set by check_interface_method_override*/
						goto fail;
				}

				// If the slot is still empty, look in all the inherited virtual methods...
				if ((vtable [im_slot] == NULL) && klass->parent != NULL) {
					// Reverse order, so that last added methods are preferred
					for (cm_index = parent->vtable_size - 1; cm_index >= 0; cm_index--) {
						cm = parent->vtable [cm_index];

						TRACE_INTERFACE_VTABLE ((cm != NULL) && printf ("    For slot %d ('%s'.'%s':'%s'), trying (ancestor) method '%s'.'%s':'%s'... ", im_slot, ic->name_space, ic->name, im->name, cm->klass->name_space, cm->klass->name, cm->name));
						if ((cm != NULL) && check_interface_method_override (klass, im, cm, MONO_ITF_OVERRIDE_SLOT_EMPTY)) {
							TRACE_INTERFACE_VTABLE (printf ("[everything ok]: ASSIGNING\n"));
							vtable [im_slot] = cm;
							/* Why do we need this? */
							if (cm->slot < 0) {
								cm->slot = im_slot;
							}
							break;
						}
						if (mono_class_has_failure (klass)) /*Might be set by check_interface_method_override*/
							goto fail;
						TRACE_INTERFACE_VTABLE ((cm != NULL) && printf ("\n"));
					}
				}

				if ((vtable [im_slot] == NULL) && klass->parent != NULL) {
					// For covariant returns we might need to lookup matching virtual methods in parent types
					// that were overriden with a method that doesn't exactly match interface method signature.
					gboolean found = FALSE;
					for (MonoClass *parent_klass = klass->parent; parent_klass != NULL && !found; parent_klass = parent_klass->parent) {
						gpointer iter = NULL;
						while ((cm = mono_class_get_virtual_methods (parent_klass, &iter))) {
							TRACE_INTERFACE_VTABLE ((cm != NULL) && printf ("    For slot %d ('%s'.'%s':'%s'), trying (ancestor) method '%s'.'%s':'%s'... ", im_slot, ic->name_space, ic->name, im->name, cm->klass->name_space, cm->klass->name, cm->name));
							if ((cm != NULL) && check_interface_method_override (klass, im, cm, MONO_ITF_OVERRIDE_SLOT_EMPTY)) {
								TRACE_INTERFACE_VTABLE (printf ("[everything ok]: ASSIGNING\n"));
								found = TRUE;
								if (vtable [cm->slot]) {
									// We match the current method was overriding it. If this method will
									// get overriden again, the interface slot will also be updated
									vtable [im_slot] = vtable [cm->slot];
								} else {
									// We add abstract method in the vtable. This method will be overriden
									// with the actual implementation once we resolve the abstract method later.
									// FIXME If klass is abstract, we can end up with abstract method in the vtable. Is this a problem ?
									vtable [im_slot] = cm;
								}
								break;
							}
						}
					}
				}

				if (vtable [im_slot] == NULL) {
					if (!(im->flags & METHOD_ATTRIBUTE_ABSTRACT)) {
						TRACE_INTERFACE_VTABLE (printf ("    Using default iface method %s.\n", mono_method_full_name (im, 1)));
						vtable [im_slot] = im;
					}
				}
			} else {
				g_assert (vtable [im_slot] == override_im);
			}
		}
	}

	// If the class is not abstract, check that all its interface slots are full.
	// The check is done here and not directly at the end of the loop above because
	// it can happen (for injected generic array interfaces) that the same slot is
	// processed multiple times (those interfaces have overlapping slots), and it
	// will not always be the first pass the one that fills the slot.
	// Now it is okay to implement a class that is not abstract and implements a interface that has an abstract method because it's reabstracted
	if (!mono_class_is_abstract (klass)) {
		for (int i = 0; i < klass->interface_offsets_count; i++) {
			int ic_offset;
			int im_index;

			ic = klass->interfaces_packed [i];
			ic_offset = mono_class_interface_offset (klass, ic);

			int mcount = mono_class_get_method_count (ic);
			for (im_index = 0; im_index < mcount; im_index++) {
				MonoMethod *im = ic->methods [im_index];
				int im_slot = ic_offset + im->slot;

				if (!m_method_is_virtual (im))
					continue;
				if (mono_method_get_is_reabstracted (im))
					continue;

				TRACE_INTERFACE_VTABLE (printf ("      [class is not abstract, checking slot %d for interface '%s'.'%s', method %s, slot check is %d]\n",
						im_slot, ic->name_space, ic->name, im->name, (vtable [im_slot] == NULL)));
				if (vtable [im_slot] == NULL) {
					print_unimplemented_interface_method_info (klass, ic, im, im_slot, overrides, onum);
					goto fail;
				}
			}
		}
	}

	TRACE_INTERFACE_VTABLE (print_vtable_full (klass, vtable, cur_slot, first_non_interface_slot, "AFTER SETTING UP INTERFACE METHODS", FALSE));
	for (l = virt_methods; l; l = l->next) {
		MonoMethod *cm = (MonoMethod *)l->data;
		/*
		 * If the method is REUSE_SLOT, we must check in the
		 * base class for a method to override.
		 */
		if (!(cm->flags & METHOD_ATTRIBUTE_NEW_SLOT)) {
			int slot = -1;
			for (k = klass->parent; k ; k = k->parent) {
				gpointer k_iter;
				MonoMethod *m1;

				k_iter = NULL;
				while ((m1 = mono_class_get_virtual_methods (k, &k_iter))) {
					MonoMethodSignature *cmsig, *m1sig;

					cmsig = mono_method_signature_internal (cm);
					m1sig = mono_method_signature_internal (m1);

					if (!cmsig || !m1sig) /* FIXME proper error message, use signature_checked? */
						goto fail;

					if (!strcmp(cm->name, m1->name) &&
					    mono_metadata_signature_equal (cmsig, m1sig)) {

						slot = mono_method_get_vtable_slot (m1);
						if (slot == -1)
							goto fail;

#ifdef FEATURE_COVARIANT_RETURNS
						if (vtable[slot] && mono_method_get_is_covariant_override_impl (vtable[slot])) {
							TRACE_INTERFACE_VTABLE (printf ("  in class %s, implicit override %s overrides %s in slot %d, which contained %s which had the covariant return bit\n", mono_type_full_name (m_class_get_byval_arg (klass)), mono_method_full_name (cm, 1), mono_method_full_name (m1, 1), slot,  mono_method_full_name (vtable[slot], 1)));
							/* Mark the current method as overriding a covariant return method; after the explicit overloads are applied, if this method is still in its slot in the vtable, we will check that it's the most specific implementation */
							mono_method_set_is_covariant_override_impl (cm);
						}
#endif /* FEATURE_COVARIANT_RETURNS */

						if (is_wcf_hack_disabled () && !mono_method_can_access_method_full (cm, m1, NULL)) {
							char *body_name = mono_method_full_name (cm, TRUE);
							char *decl_name = mono_method_full_name (m1, TRUE);
							mono_class_set_type_load_failure (klass, "Method %s overrides method '%s' which is not accessible", body_name, decl_name);
							g_free (body_name);
							g_free (decl_name);
							goto fail;
						}

						g_assert (cm->slot < max_vtsize);
						if (!override_map)
							override_map = g_hash_table_new (mono_aligned_addr_hash, NULL);
						TRACE_INTERFACE_VTABLE (printf ("adding base class override from %s [%p] to %s [%p]\n",
							mono_method_full_name (m1, 1), m1,
							mono_method_full_name (cm, 1), cm));
						g_hash_table_insert (override_map, m1, cm);
						break;
					}
				}
				if (mono_class_has_failure (k))
					goto fail;

				if (slot >= 0)
					break;
			}
			if (slot >= 0)
				cm->slot = slot;
		}

		/*Non final newslot methods must be given a non-interface vtable slot*/
		if ((cm->flags & METHOD_ATTRIBUTE_NEW_SLOT) && !(cm->flags & METHOD_ATTRIBUTE_FINAL) && cm->slot >= 0)
			cm->slot = -1;

		if (cm->slot < 0)
			cm->slot = cur_slot++;

		if (!(cm->flags & METHOD_ATTRIBUTE_ABSTRACT))
			vtable [cm->slot] = cm;
	}

	/* override non interface methods */
	for (int i = 0; i < onum; i++) {
		MonoMethod *decl = overrides [i*2];
		MonoMethod *impl = overrides [i*2 + 1];
		if (!MONO_CLASS_IS_INTERFACE_INTERNAL (decl->klass)) {
			g_assert (decl->slot != -1);
#ifdef FEATURE_COVARIANT_RETURNS
			MonoMethod *prev_impl = vtable [decl->slot];
#endif
			vtable [decl->slot] = impl;
#ifdef FEATURE_COVARIANT_RETURNS
			gboolean impl_newslot = (impl->flags & METHOD_ATTRIBUTE_NEW_SLOT) != 0;
			/* covariant returns generate an explicit override impl with a newslot flag. respect it. */
			if (!impl_newslot)
				impl->slot = decl->slot;
#endif
			if (!override_map)
				override_map = g_hash_table_new (mono_aligned_addr_hash, NULL);
			TRACE_INTERFACE_VTABLE (printf ("adding explicit override from %s [%p] to %s [%p]\n",
				mono_method_full_name (decl, 1), decl,
				mono_method_full_name (impl, 1), impl));
			g_hash_table_insert (override_map, decl, impl);

#ifdef FEATURE_COVARIANT_RETURNS
			MonoClass *impl_class = impl->klass;
			gboolean has_preserve_base_overrides =
				mono_class_setup_method_has_preserve_base_overrides_attribute (impl) ||
				(klass->parent &&
				 vtable_slot_has_preserve_base_overrides_attribute (klass->parent, decl->slot, &impl_class) &&
				 impl_class != decl->klass);

			if (impl_class == decl->klass) {
				TRACE_INTERFACE_VTABLE (printf ("preserve base overrides attribute is on slot %d is on the decl method %s; not adding any more overrides\n", decl->slot, mono_method_full_name (decl, 1)));
			}

			if (has_preserve_base_overrides) {
				g_assert (impl_class != NULL);
				g_assert (impl_class == klass || decl->slot < impl_class->vtable_size);
				TRACE_INTERFACE_VTABLE (do {
						MonoMethod *impl_with_attr = impl_class == klass ? impl : impl_class->vtable [decl->slot];
						printf ("override decl [slot %d] %s in class %s has method %s in this slot and it has the preserve base overrides attribute.  overridden by %s\n", decl->slot, mono_method_full_name (decl, 1), mono_type_full_name (m_class_get_byval_arg (impl_class)), mono_method_full_name (impl_with_attr, 1), mono_method_full_name (impl, 1));
							} while (0));
			}

			/* Historically, mono didn't do a signature equivalence check for explicit overrides, but we need one for covariant returns */
			/* If the previous impl in the slot had the covariant signature bit set, or if the signature of the proposed impl doesn't match the signature of the previous impl, check for covariance */
			if (prev_impl != NULL &&
			    (mono_method_get_is_covariant_override_impl (prev_impl) ||
			     !mono_metadata_signature_equal (mono_method_signature_internal (impl), mono_method_signature_internal (prev_impl)))) {
				mono_method_set_is_covariant_override_impl (impl);
			}

			/* if we saw the attribute or if we think we need to check impl sigs, we will need to traverse the class hierarchy. */
			if (has_preserve_base_overrides) {
				for (MonoClass *cur_class = klass->parent; cur_class ; cur_class = cur_class->parent) {
					if (decl->slot >= cur_class->vtable_size)
						break;
					prev_impl = cur_class->vtable[decl->slot];
					g_hash_table_insert (override_map, prev_impl, impl);
					TRACE_INTERFACE_VTABLE (do {
							char *full_name = mono_type_full_name (m_class_get_byval_arg (cur_class));
							printf ("  slot %d of %s was %s adding override to %s\n", decl->slot, full_name, mono_method_full_name (prev_impl, 1), mono_method_full_name (impl, 1));
							g_free (full_name);
						} while (0));
				}
			}
#endif /* FEATURE_COVARIANT_RETURNS */
		}
	}

	TRACE_INTERFACE_VTABLE (print_vtable_full (klass, vtable, cur_slot, first_non_interface_slot, "AFTER OVERRIDING NON-INTERFACE METHODS", FALSE));

#ifdef FEATURE_COVARIANT_RETURNS
	/*
	 * for each vtable slot that has one of the methods from klass that has the is_covariant_override_impl bit set,
	 * check that it is subsumed by the methods in each ancestor class.
	 *
	 * This check has to be done after both implicit and explicit non-interface method overrides are applied to the
	 * vtable.  If it's done inline earlier, we could erroneously check an implicit override that should actually be
	 * ignored (because a more specific explicit override is applied).
	 */
	if (!check_vtable_covariant_override_impls (klass, vtable, cur_slot))
		goto fail;
#endif

	/*
	 * If a method occupies more than one place in the vtable, and it is
	 * overridden, then change the other occurrences too.
	 */
	if (override_map) {
		MonoMethod *cm;

		for (int i = 0; i < max_vtsize; ++i)
			if (vtable [i]) {
				TRACE_INTERFACE_VTABLE (printf ("checking slot %d method %s[%p] for overrides\n", i, mono_method_full_name (vtable [i], 1), vtable [i]));

				cm = (MonoMethod *)g_hash_table_lookup (override_map, vtable [i]);
				if (cm)
					vtable [i] = cm;
			}

		g_hash_table_destroy (override_map);
		override_map = NULL;
	}

	if (override_class_map)
		g_hash_table_destroy (override_class_map);

	if (conflict_map) {
		handle_dim_conflicts (vtable, klass, conflict_map);
		g_hash_table_destroy (conflict_map);
	}

	g_slist_free (virt_methods);
	virt_methods = NULL;

	g_assert (cur_slot <= max_vtsize);

	/* Ensure that all vtable slots are filled with concrete methods */
	// Now it is okay to implement a class that is not abstract and implements a interface that has an abstract method because it's reabstracted
	if (!mono_class_is_abstract (klass)) {
		for (int i = 0; i < cur_slot; ++i) {
			if (vtable [i] == NULL || (vtable [i]->flags & METHOD_ATTRIBUTE_ABSTRACT)) {
				if (vtable [i] != NULL && mono_method_get_is_reabstracted (vtable [i]))
					continue;
				char *type_name = mono_type_get_full_name (klass);
				char *method_name = vtable [i] ? mono_method_full_name (vtable [i], TRUE) : g_strdup ("none");
				mono_class_set_type_load_failure (klass, "Type %s has invalid vtable method slot %d with method %s", type_name, i, method_name);
				g_free (type_name);
				g_free (method_name);

				if (mono_print_vtable)
					print_vtable_layout_result (klass, vtable, cur_slot);

				g_free (vtable);
				return;
			}
		}
	}

	if (mono_class_is_ginst (klass)) {
		MonoClass *gklass = mono_class_get_generic_class (klass)->container_class;

		mono_class_init_internal (gklass);

		klass->vtable_size = MAX (gklass->vtable_size, cur_slot);
	} else {
		/* Check that the vtable_size value computed in mono_class_init_internal () is correct */
		if (klass->vtable_size)
			g_assert (cur_slot == klass->vtable_size);
		klass->vtable_size = cur_slot;
	}

	/* Try to share the vtable with our parent. */
	if (klass->parent && (klass->parent->vtable_size == klass->vtable_size) && (memcmp (klass->parent->vtable, vtable, sizeof (gpointer) * klass->vtable_size) == 0)) {
		mono_memory_barrier ();
		klass->vtable = klass->parent->vtable;
	} else {
		MonoMethod **tmp = (MonoMethod **)mono_class_alloc0 (klass, sizeof (gpointer) * klass->vtable_size);
		memcpy (tmp, vtable,  sizeof (gpointer) * klass->vtable_size);
		mono_memory_barrier ();
		klass->vtable = tmp;
	}

	DEBUG_INTERFACE_VTABLE (print_vtable_full (klass, klass->vtable, klass->vtable_size, first_non_interface_slot, "FINALLY", FALSE));
	if (mono_print_vtable)
		print_vtable_layout_result (klass, vtable, cur_slot);

	g_free (vtable);

	VERIFY_INTERFACE_VTABLE (mono_class_verify_vtable (klass));
	return;

fail:
	{
	char *name = mono_type_get_full_name (klass);
	if (!is_ok (error))
		mono_class_set_type_load_failure (klass, "VTable setup of type %s failed due to: %s", name, mono_error_get_message (error));
	else
		mono_class_set_type_load_failure (klass, "VTable setup of type %s failed", name);
	mono_error_cleanup (error);
	g_free (name);
	if (mono_print_vtable)
		print_vtable_layout_result (klass, vtable, cur_slot);


	g_free (vtable);
	if (override_map)
		g_hash_table_destroy (override_map);
	if (virt_methods)
		g_slist_free (virt_methods);
	}
}
