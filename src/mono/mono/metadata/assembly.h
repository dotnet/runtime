#ifndef _MONONET_METADATA_ASSEMBLY_H_ 
#define _MONONET_METADATA_ASSEMBLY_H_

#include <glib.h>

#include <mono/metadata/image.h>

void          mono_assemblies_init     (void);
MonoAssembly *mono_assembly_open       (const char *filename,
				       	MonoImageOpenStatus *status);
MonoAssembly* mono_assembly_load       (MonoAssemblyName *aname, 
                                       	const char       *basedir, 
				     	MonoImageOpenStatus *status);
MonoAssembly* mono_assembly_load_from  (MonoImage *image, const char *fname,
					MonoImageOpenStatus *status);

MonoAssembly* mono_assembly_load_with_partial_name (const char *name, MonoImageOpenStatus *status);

MonoAssembly* mono_assembly_loaded     (MonoAssemblyName *aname);
void          mono_assembly_load_reference (MonoImage *image, int index);
void          mono_assembly_load_references (MonoImage *image, MonoImageOpenStatus *status);
MonoImage*    mono_assembly_load_module (MonoAssembly *assembly, guint32 idx);
void          mono_assembly_close      (MonoAssembly *assembly);
void          mono_assembly_setrootdir (const char *root_dir);
G_CONST_RETURN gchar *mono_assembly_getrootdir (void);
void	      mono_assembly_foreach    (GFunc func, gpointer user_data);
void          mono_assembly_set_main   (MonoAssembly *assembly);
MonoAssembly *mono_assembly_get_main   (void);
MonoImage    *mono_assembly_get_image  (MonoAssembly *assembly);
gboolean      mono_assembly_fill_assembly_name (MonoImage *image, MonoAssemblyName *aname);
gboolean      mono_assembly_names_equal (MonoAssemblyName *l, MonoAssemblyName *r);

/* Installs a function which is called each time a new assembly is loaded. */
typedef void  (*MonoAssemblyLoadFunc)         (MonoAssembly *assembly, gpointer user_data);
void          mono_install_assembly_load_hook (MonoAssemblyLoadFunc func, gpointer user_data);

/* 
 * Installs a new function which is used to search the list of loaded 
 * assemblies for a given assembly name.
 */
typedef MonoAssembly *(*MonoAssemblySearchFunc)         (MonoAssemblyName *aname, gpointer user_data);
void          mono_install_assembly_search_hook (MonoAssemblySearchFunc func, gpointer user_data);

MonoAssembly* mono_assembly_invoke_search_hook (MonoAssemblyName *aname);

/* Installs a function which is called before a new assembly is loaded
 * The hook are invoked from last hooked to first. If any of them returns
 * a non-null value, that will be the value returned in mono_assembly_load */
typedef MonoAssembly * (*MonoAssemblyPreLoadFunc) (MonoAssemblyName *aname,
						   gchar **assemblies_path,
						   gpointer user_data);

void          mono_install_assembly_preload_hook (MonoAssemblyPreLoadFunc func,
						  gpointer user_data);

void          mono_assembly_invoke_load_hook (MonoAssembly *ass);

typedef struct {
	const char *name;
	const unsigned char *data;
	const unsigned int size;
} MonoBundledAssembly;

void          mono_register_bundled_assemblies (const MonoBundledAssembly **assemblies);
#endif

