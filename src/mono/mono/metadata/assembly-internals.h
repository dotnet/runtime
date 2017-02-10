/**
 * \file
 * Copyright 2015 Xamarin Inc
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_METADATA_ASSEMBLY_INTERNALS_H__
#define __MONO_METADATA_ASSEMBLY_INTERNALS_H__

#include <glib.h>

#include <mono/metadata/assembly.h>

MONO_API MonoImage*    mono_assembly_load_module_checked (MonoAssembly *assembly, uint32_t idx, MonoError *error);

MonoAssembly * mono_assembly_open_a_lot (const char *filename, MonoImageOpenStatus *status, gboolean refonly, gboolean load_from_context);

/* If predicate returns true assembly should be loaded, if false ignore it. */
typedef gboolean (*MonoAssemblyCandidatePredicate)(MonoAssembly *, gpointer);

MonoAssembly*          mono_assembly_open_predicate (const char *filename,
						     gboolean refonly,
						     gboolean load_from_context,
						     MonoAssemblyCandidatePredicate pred,
						     gpointer user_data,
						     MonoImageOpenStatus *status);

MonoAssembly*          mono_assembly_load_from_predicate (MonoImage *image, const char *fname,
							  gboolean refonly,
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

#endif /* __MONO_METADATA_ASSEMBLY_INTERNALS_H__ */
