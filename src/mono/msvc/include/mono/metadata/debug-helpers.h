/**
 * \file
 */

#ifndef __MONO_DEBUG_HELPERS_H__
#define __MONO_DEBUG_HELPERS_H__

#include <mono/metadata/class.h>

MONO_BEGIN_DECLS

typedef struct MonoDisHelper MonoDisHelper;

typedef char* (*MonoDisIndenter) (MonoDisHelper *dh, MonoMethod *method, uint32_t ip_offset);
typedef char* (*MonoDisTokener)  (MonoDisHelper *dh, MonoMethod *method, uint32_t token);

struct MonoDisHelper {
	const char *newline;
	const char *label_format;
	const char *label_target;
	MonoDisIndenter indenter;
	MonoDisTokener  tokener;
	void* user_data;
};

MONO_API char* mono_disasm_code_one (MonoDisHelper *dh, MonoMethod *method, const mono_byte *ip, const mono_byte** endp);
MONO_API char* mono_disasm_code     (MonoDisHelper *dh, MonoMethod *method, const mono_byte *ip, const mono_byte* end);

typedef struct MonoMethodDesc MonoMethodDesc;

MONO_API char*           mono_type_full_name (MonoType *type);

MONO_API char*           mono_signature_get_desc (MonoMethodSignature *sig, mono_bool include_namespace);

MONO_API char*           mono_context_get_desc (MonoGenericContext *context);

MONO_API MonoMethodDesc* mono_method_desc_new (const char *name, mono_bool include_namespace);
MONO_API MonoMethodDesc* mono_method_desc_from_method (MonoMethod *method);
MONO_API void            mono_method_desc_free (MonoMethodDesc *desc);
MONO_API mono_bool       mono_method_desc_match (MonoMethodDesc *desc, MonoMethod *method);
MONO_API mono_bool       mono_method_desc_is_full (MonoMethodDesc *desc);
MONO_API mono_bool       mono_method_desc_full_match (MonoMethodDesc *desc, MonoMethod *method);
MONO_API MonoMethod*     mono_method_desc_search_in_class (MonoMethodDesc *desc, MonoClass *klass);
MONO_API MonoMethod*     mono_method_desc_search_in_image (MonoMethodDesc *desc, MonoImage *image);

MONO_API char*           mono_method_full_name (MonoMethod *method, mono_bool signature);
MONO_API char*           mono_method_get_reflection_name (MonoMethod *method);

MONO_API char*           mono_field_full_name (MonoClassField *field);

MONO_END_DECLS

#endif /* __MONO_DEBUG_HELPERS_H__ */

