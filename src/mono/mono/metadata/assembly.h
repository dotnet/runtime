#ifndef _MONONET_METADATA_ASSEMBLY_H_ 
#define _MONONET_METADATA_ASSEMBLY_H_

#include <glib.h>

#include <mono/metadata/image.h>

#define CORLIB_NAME "corlib.dll"

MonoAssembly *mono_assembly_open       (const char *filename,
				       	MonoImageOpenStatus *status);
MonoAssembly* mono_assembly_load       (MonoAssemblyName *aname, 
                                       	const char       *basedir, 
				     	MonoImageOpenStatus *status);
void          mono_assembly_close      (MonoAssembly *assembly);
void          mono_assembly_setrootdir (const char *root_dir);
void	      mono_assembly_foreach    (GFunc func, gpointer user_data);
void          mono_assembly_set_main   (MonoAssembly *assembly);
MonoAssembly *mono_assembly_get_main   (void);

/* Installs a function which is called each time a new assembly is loaded. */
typedef void  (*MonoOpenAssemblyFunc)         (MonoAssembly *assembly);
void          mono_install_open_assembly_hook (MonoOpenAssemblyFunc func);

#endif
