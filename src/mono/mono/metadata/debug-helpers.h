#ifndef __MONO_DEBUG_HELPERS_H__
#define __MONO_DEBUG_HELPERS_H__

#include <glib.h>
#include <mono/metadata/class.h>

G_BEGIN_DECLS

typedef struct MonoDisHelper MonoDisHelper;

typedef char* (*MonoDisIndenter) (MonoDisHelper *dh, MonoMethod *method, guint32 ip_offset);
typedef char* (*MonoDisTokener)  (MonoDisHelper *dh, MonoMethod *method, guint32 token);

struct MonoDisHelper {
	const char *newline;
	const char *label_format;
	const char *label_target;
	MonoDisIndenter indenter;
	MonoDisTokener  tokener;
	gpointer user_data;
};

char* mono_disasm_code_one (MonoDisHelper *dh, MonoMethod *method, const guchar *ip, const guchar** endp);
char* mono_disasm_code     (MonoDisHelper *dh, MonoMethod *method, const guchar *ip, const guchar* end);

typedef struct MonoMethodDesc MonoMethodDesc;

void            mono_type_get_desc (GString *res, MonoType *type, gboolean include_namespace);
char*           mono_type_full_name (MonoType *type);

char*           mono_signature_get_desc (MonoMethodSignature *sig, gboolean include_namespace);

char*           mono_context_get_desc (MonoGenericContext *context);

MonoMethodDesc* mono_method_desc_new (const char *name, gboolean include_namespace);
MonoMethodDesc* mono_method_desc_from_method (MonoMethod *method);
void            mono_method_desc_free (MonoMethodDesc *desc);
gboolean        mono_method_desc_match (MonoMethodDesc *desc, MonoMethod *method);
gboolean        mono_method_desc_full_match (MonoMethodDesc *desc, MonoMethod *method);
MonoMethod*     mono_method_desc_search_in_class (MonoMethodDesc *desc, MonoClass *klass);
MonoMethod*     mono_method_desc_search_in_image (MonoMethodDesc *desc, MonoImage *image);

char*           mono_method_full_name (MonoMethod *method, gboolean signature);

char*           mono_field_full_name (MonoClassField *field);

G_END_DECLS

#endif /* __MONO_DEBUG_HELPERS_H__ */

