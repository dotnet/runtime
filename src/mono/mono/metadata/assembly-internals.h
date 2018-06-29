/**
 * \file
 * Copyright 2015 Xamarin Inc
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_METADATA_ASSEMBLY_INTERNALS_H__
#define __MONO_METADATA_ASSEMBLY_INTERNALS_H__

#include <glib.h>

#include <mono/metadata/assembly.h>
#include <mono/metadata/metadata-internals.h>

/* Flag bits for mono_assembly_names_equal_flags (). */
typedef enum {
	/* Default comparison: all fields must match */
	MONO_ANAME_EQ_NONE = 0x0,
	/* Don't compare public key token */
	MONO_ANAME_EQ_IGNORE_PUBKEY = 0x1,
	/* Don't compare the versions */
	MONO_ANAME_EQ_IGNORE_VERSION = 0x2,
	/* When comparing simple names, ignore case differences */
	MONO_ANAME_EQ_IGNORE_CASE = 0x4,

	MONO_ANAME_EQ_MASK = 0x7
} MonoAssemblyNameEqFlags;

void
mono_assembly_name_free_internal (MonoAssemblyName *aname);

gboolean
mono_assembly_names_equal_flags (MonoAssemblyName *l, MonoAssemblyName *r, MonoAssemblyNameEqFlags flags);

gboolean
mono_assembly_get_assemblyref_checked (MonoImage *image, int index, MonoAssemblyName *aname, MonoError *error);

MONO_API MonoImage*    mono_assembly_load_module_checked (MonoAssembly *assembly, uint32_t idx, MonoError *error);

MonoAssembly * mono_assembly_open_a_lot (const char *filename, MonoImageOpenStatus *status, MonoAssemblyContextKind asmctx);

MonoAssembly* mono_assembly_load_full_nosearch (MonoAssemblyName *aname, 
						const char       *basedir,
						MonoAssemblyContextKind asmctx,
						MonoImageOpenStatus *status);

MonoAssembly* mono_assembly_load_with_partial_name_internal (const char *name, MonoImageOpenStatus *status);


/* If predicate returns true assembly should be loaded, if false ignore it. */
typedef gboolean (*MonoAssemblyCandidatePredicate)(MonoAssembly *, gpointer);

MonoAssembly*          mono_assembly_open_predicate (const char *filename,
						     MonoAssemblyContextKind asmctx,
						     MonoAssemblyCandidatePredicate pred,
						     gpointer user_data,
						     MonoImageOpenStatus *status);

MonoAssembly*          mono_assembly_load_from_predicate (MonoImage *image, const char *fname,
							  MonoAssemblyContextKind asmctx,
							  MonoAssemblyCandidatePredicate pred,
							  gpointer user_data,
							  MonoImageOpenStatus *status);


/* MonoAssemblyCandidatePredicate that compares the assembly name (name, version,
 * culture, public key token) of the candidate with the wanted name, if the
 * wanted name has a public key token (if not present, always return true).
 * Pass the wanted MonoAssemblyName* as the user_data.
 */
gboolean
mono_assembly_candidate_predicate_sn_same_name (MonoAssembly *candidate, gpointer wanted_name);

MonoAssembly*
mono_assembly_binding_applies_to_image (MonoImage* image, MonoImageOpenStatus *status);

MonoAssembly*
mono_assembly_load_from_assemblies_path (gchar **assemblies_path, MonoAssemblyName *aname, MonoAssemblyContextKind asmctx);

MONO_PROFILER_API MonoAssemblyName*
mono_assembly_get_name_internal (MonoAssembly *assembly);

MONO_PROFILER_API MonoImage*
mono_assembly_get_image_internal (MonoAssembly *assembly);

#endif /* __MONO_METADATA_ASSEMBLY_INTERNALS_H__ */
