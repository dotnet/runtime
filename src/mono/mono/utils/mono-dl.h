#ifndef __MONO_UTILS_DL_H__
#define __MONO_UTILS_DL_H__

enum {
	MONO_DL_LAZY  = 1,
	MONO_DL_LOCAL = 2,
	MONO_DL_MASK  = 3
};

typedef struct _MonoDl MonoDl;

MonoDl*     mono_dl_open       (const char *name, int flags, char **error_msg);
char*       mono_dl_symbol     (MonoDl *module, const char *name, void **symbol);
void        mono_dl_close      (MonoDl *module);

char*       mono_dl_build_path (const char *directory, const char *name, void **iter);

#endif /* __MONO_UTILS_DL_H__ */

