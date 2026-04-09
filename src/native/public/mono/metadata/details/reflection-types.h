// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
#ifndef _MONO_REFLECTION_TYPES_H
#define _MONO_REFLECTION_TYPES_H

#include <mono/utils/details/mono-publib-types.h>
#include <mono/metadata/details/object-types.h>

MONO_BEGIN_DECLS

typedef struct MonoTypeNameParse MonoTypeNameParse;

typedef struct {
	MonoMethod *ctor;
	uint32_t     data_size;
	const mono_byte* data;
} MonoCustomAttrEntry;

typedef struct {
	int num_attrs;
	int cached;
	MonoImage *image;
	MonoCustomAttrEntry attrs [MONO_ZERO_LEN_ARRAY];
} MonoCustomAttrInfo;

#define MONO_SIZEOF_CUSTOM_ATTR_INFO (offsetof (MonoCustomAttrInfo, attrs))

/*
 * Information which isn't in the MonoMethod structure is stored here for
 * dynamic methods.
 */
typedef struct {
	char **param_names;
	MonoMarshalSpec **param_marshall;
	MonoCustomAttrInfo **param_cattr;
	uint8_t** param_defaults;
	uint32_t *param_default_types;
	char *dllentry, *dll;
} MonoReflectionMethodAux;

typedef enum {
	ResolveTokenError_OutOfRange,
	ResolveTokenError_BadTable,
	ResolveTokenError_Other
} MonoResolveTokenError;

#define MONO_DECLSEC_ACTION_MIN		0x1
#define MONO_DECLSEC_ACTION_MAX		0x12

enum {
	MONO_DECLSEC_FLAG_REQUEST 			= 0x00000001,
	MONO_DECLSEC_FLAG_DEMAND			= 0x00000002,
	MONO_DECLSEC_FLAG_ASSERT			= 0x00000004,
	MONO_DECLSEC_FLAG_DENY				= 0x00000008,
	MONO_DECLSEC_FLAG_PERMITONLY			= 0x00000010,
	MONO_DECLSEC_FLAG_LINKDEMAND			= 0x00000020,
	MONO_DECLSEC_FLAG_INHERITANCEDEMAND		= 0x00000040,
	MONO_DECLSEC_FLAG_REQUEST_MINIMUM		= 0x00000080,
	MONO_DECLSEC_FLAG_REQUEST_OPTIONAL		= 0x00000100,
	MONO_DECLSEC_FLAG_REQUEST_REFUSE		= 0x00000200,
	MONO_DECLSEC_FLAG_PREJIT_GRANT			= 0x00000400,
	MONO_DECLSEC_FLAG_PREJIT_DENY			= 0x00000800,
	MONO_DECLSEC_FLAG_NONCAS_DEMAND			= 0x00001000,
	MONO_DECLSEC_FLAG_NONCAS_LINKDEMAND		= 0x00002000,
	MONO_DECLSEC_FLAG_NONCAS_INHERITANCEDEMAND	= 0x00004000,
	MONO_DECLSEC_FLAG_LINKDEMAND_CHOICE		= 0x00008000,
	MONO_DECLSEC_FLAG_INHERITANCEDEMAND_CHOICE	= 0x00010000,
	MONO_DECLSEC_FLAG_DEMAND_CHOICE			= 0x00020000
};

/* this structure MUST be kept in synch with RuntimeDeclSecurityEntry
 * located in /mcs/class/corlib/System.Security/SecurityFrame.cs */
typedef struct {
	char *blob;				/* pointer to metadata blob */
	uint32_t size;				/* size of the metadata blob */
	uint32_t index;
} MonoDeclSecurityEntry;

typedef struct {
	MonoDeclSecurityEntry demand;
	MonoDeclSecurityEntry noncasdemand;
	MonoDeclSecurityEntry demandchoice;
} MonoDeclSecurityActions;

MONO_END_DECLS

#endif /* _MONO_REFLECTION_TYPES_H */
