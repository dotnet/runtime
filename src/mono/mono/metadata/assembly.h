#ifndef _MONONET_METADATA_ASSEMBLY_H_ 
#define _MONONET_METADATA_ASSEMBLY_H_

#include <glib.h>

#include <mono/metadata/image.h>

#define CORLIB_NAME "corlib.dll"

void          mono_assemblies_init     (void);
MonoAssembly *mono_assembly_open       (const char *filename,
				       	MonoImageOpenStatus *status);
MonoAssembly* mono_assembly_load       (MonoAssemblyName *aname, 
                                       	const char       *basedir, 
				     	MonoImageOpenStatus *status);
MonoImage*    mono_assembly_load_module (MonoAssembly *assembly, guint32 idx);
void          mono_assembly_close      (MonoAssembly *assembly);
void          mono_assembly_setrootdir (const char *root_dir);
void	      mono_assembly_foreach    (GFunc func, gpointer user_data);
void          mono_assembly_set_main   (MonoAssembly *assembly);
MonoAssembly *mono_assembly_get_main   (void);

/* Installs a function which is called each time a new assembly is loaded. */
typedef void  (*MonoAssemblyLoadFunc)         (MonoAssembly *assembly, gpointer user_data);
void          mono_install_assembly_load_hook (MonoAssemblyLoadFunc func, gpointer user_data);

/* Installs a function which is called before a new assembly is loaded
 * The hook are invoked from last hooked to first. If any of them returns
 * a non-null value, that will be the value returned in mono_assembly_load */
typedef MonoAssembly * (*MonoAssemblyPreLoadFunc) (MonoAssemblyName *aname,
						   gchar **assemblies_path,
						   gpointer user_data);

void          mono_install_assembly_preload_hook (MonoAssemblyPreLoadFunc func,
						  gpointer user_data);

void          mono_assembly_invoke_load_hook (MonoAssembly *ass);

#endif

