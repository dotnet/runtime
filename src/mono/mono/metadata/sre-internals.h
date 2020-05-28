/**
 * \file
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_METADATA_SRE_INTERNALS_H__
#define __MONO_METADATA_SRE_INTERNALS_H__

#include <mono/metadata/object-internals.h>

/* Keep in sync with System.Reflection.Emit.AssemblyBuilderAccess */
enum MonoAssemblyBuilderAccess {
	MonoAssemblyBuilderAccess_Run = 1,                /* 0b0001 */
	MonoAssemblyBuilderAccess_Save = 2,               /* 0b0010 */
	MonoAssemblyBuilderAccess_RunAndSave = 3,         /* Run | Save */
	MonoAssemblyBuilderAccess_ReflectionOnly = 6,     /* Refonly | Save */
	MonoAssemblyBuilderAccess_RunAndCollect = 9,      /* Collect | Run */
};

typedef struct _ArrayMethod ArrayMethod;

typedef struct {
	guint32 owner;
	MonoReflectionGenericParam *gparam;
} GenericParamTableEntry;

typedef struct {
	MonoReflectionILGen *ilgen;
	MonoReflectionType *rtype;
	MonoArray *parameters;
	MonoArray *generic_params;
	MonoGenericContainer *generic_container;
	MonoArray *pinfo;
	MonoArray *opt_types;
	guint32 attrs;
	guint32 iattrs;
	guint32 call_conv;
	guint32 *table_idx; /* note: it's a pointer */
	MonoArray *code;
	MonoObject *type;
	MonoString *name;
	MonoBoolean init_locals;
	MonoBoolean skip_visibility;
	MonoArray *return_modreq;
	MonoArray *return_modopt;
	MonoArray *param_modreq;
	MonoArray *param_modopt;
	MonoArray *permissions;
	MonoMethod *mhandle;
	guint32 nrefs;
	gpointer *refs;
	/* for PInvoke */
	int charset, extra_flags, native_cc;
	MonoString *dll, *dllentry;
} ReflectionMethodBuilder; /* FIXME raw pointers to managed objects */

void
mono_reflection_emit_init (void);

void
mono_reflection_dynimage_basic_init (MonoReflectionAssemblyBuilder *assemblyb, MonoError *error);

gpointer
mono_image_g_malloc0 (MonoImage *image, guint size);

#define mono_image_g_malloc0(image, size) (g_cast (mono_image_g_malloc0 ((image), (size))))

gboolean
mono_is_sre_type_builder (MonoClass *klass);

gboolean
mono_is_sre_generic_instance (MonoClass *klass);

gboolean
mono_is_sre_method_on_tb_inst (MonoClass *klass);

gboolean
mono_is_sre_ctor_builder (MonoClass *klass);

gboolean
mono_is_sre_ctor_on_tb_inst (MonoClass *klass);

gboolean
mono_is_sr_mono_cmethod (MonoClass *klass);

gboolean
mono_is_sr_mono_property (MonoClass *klass);

MonoType*
mono_reflection_type_get_handle (MonoReflectionType *ref, MonoError *error);

gpointer
mono_reflection_resolve_object (MonoImage *image, MonoObject *obj, MonoClass **handle_class, MonoGenericContext *context, MonoError *error);

gpointer
mono_reflection_resolve_object_handle (MonoImage *image, MonoObjectHandle obj, MonoClass **handle_class, MonoGenericContext *context, MonoError *error);

MonoType* mono_type_array_get_and_resolve (MonoArrayHandle array, int idx, MonoError* error);

void
mono_sre_array_method_free (ArrayMethod *am);

void
mono_sre_generic_param_table_entry_free (GenericParamTableEntry *entry);

gboolean
mono_reflection_methodbuilder_from_method_builder (ReflectionMethodBuilder *rmb, MonoReflectionMethodBuilder *mb,
						   MonoError *error);
gboolean
mono_reflection_methodbuilder_from_ctor_builder (ReflectionMethodBuilder *rmb, MonoReflectionCtorBuilder *mb,
						 MonoError *error);
							    
guint32
mono_reflection_resolution_scope_from_image (MonoDynamicImage *assembly, MonoImage *image);

guint32 mono_reflection_method_count_clauses (MonoReflectionILGen *ilgen);


/* sre-encode */

guint32
mono_dynimage_encode_field_signature (MonoDynamicImage *assembly, MonoReflectionFieldBuilder *fb, MonoError *error);

guint32
mono_dynimage_encode_constant (MonoDynamicImage *assembly, MonoObject *val, MonoTypeEnum *ret_type);

guint32
mono_dynimage_encode_locals (MonoDynamicImage *assembly, MonoReflectionILGen *ilgen, MonoError *error);

guint32
mono_dynimage_encode_fieldref_signature (MonoDynamicImage *assembly, MonoImage *field_image, MonoType *type);

guint32
mono_dynimage_encode_method_signature (MonoDynamicImage *assembly, MonoMethodSignature *sig);

guint32
mono_dynimage_encode_method_builder_signature (MonoDynamicImage *assembly, ReflectionMethodBuilder *mb,
					       MonoError *error);

guint32
mono_dynimage_encode_generic_method_sig (MonoDynamicImage *assembly, MonoGenericContext *context);

guint32
mono_dynimage_encode_typedef_or_ref_full (MonoDynamicImage *assembly, MonoType *type, gboolean try_typespec);

guint32
mono_dynimage_encode_reflection_sighelper (MonoDynamicImage *assembly, MonoReflectionSigHelperHandle helper,
					   MonoError *error);

/* sre-encode, without DISABLE_REFLECTION_EMIT_SAVE (o.w. g_assert_not_reached ()) */

guint32
mono_dynimage_save_encode_marshal_blob (MonoDynamicImage *assembly, MonoReflectionMarshal *minfo, MonoError *error);

guint32
mono_dynimage_save_encode_property_signature (MonoDynamicImage *assembly, MonoReflectionPropertyBuilder *fb, MonoError *error);

guint32
mono_image_get_methodref_token (MonoDynamicImage *assembly, MonoMethod *method, gboolean create_typespec);

#endif  /* __MONO_METADATA_SRE_INTERNALS_H__ */

