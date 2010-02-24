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

char* mono_disasm_code_one (MonoDisHelper *dh, MonoMethod *method, const mono_byte *ip, const mono_byte** endp);
char* mono_disasm_code     (MonoDisHelper *dh, MonoMethod *method, const mono_byte *ip, const mono_byte* end);

typedef struct MonoMethodDesc MonoMethodDesc;

char*           mono_type_full_name (MonoType *type);

char*           mono_signature_get_desc (MonoMethodSignature *sig, mono_bool include_namespace);

char*           mono_context_get_desc (MonoGenericContext *context);

MonoMethodDesc* mono_method_desc_new (const char *name, mono_bool include_namespace);
MonoMethodDesc* mono_method_desc_from_method (MonoMethod *method);
void            mono_method_desc_free (MonoMethodDesc *desc);
mono_bool       mono_method_desc_match (MonoMethodDesc *desc, MonoMethod *method);
mono_bool       mono_method_desc_full_match (MonoMethodDesc *desc, MonoMethod *method);
MonoMethod*     mono_method_desc_search_in_class (MonoMethodDesc *desc, MonoClass *klass);
MonoMethod*     mono_method_desc_search_in_image (MonoMethodDesc *desc, MonoImage *image);

char*           mono_method_full_name (MonoMethod *method, mono_bool signature);

char*           mono_field_full_name (MonoClassField *field);

MONO_END_DECLS

#endif /* __MONO_DEBUG_HELPERS_H__ */

