#ifndef _MONONET_METADATA_ASSEMBLY_H_ 
#define _MONONET_METADATA_ASSEMBLY_H_

#include <mono/metadata/image.h>

MONO_BEGIN_DECLS

void          mono_assemblies_init     (void);
void          mono_assemblies_cleanup  (void);
MonoAssembly *mono_assembly_open       (const char *filename,
				       	MonoImageOpenStatus *status);
MonoAssembly *mono_assembly_open_full (const char *filename,
				       	MonoImageOpenStatus *status,
					mono_bool refonly);
MonoAssembly* mono_assembly_load       (MonoAssemblyName *aname, 
                                       	const char       *basedir, 
				     	MonoImageOpenStatus *status);
MonoAssembly* mono_assembly_load_full (MonoAssemblyName *aname, 
                                       	const char       *basedir, 
				     	MonoImageOpenStatus *status,
					mono_bool refonly);
MonoAssembly* mono_assembly_load_from  (MonoImage *image, const char *fname,
					MonoImageOpenStatus *status);
MonoAssembly* mono_assembly_load_from_full  (MonoImage *image, const char *fname,
					MonoImageOpenStatus *status,
					mono_bool refonly);

MonoAssembly* mono_assembly_load_with_partial_name (const char *name, MonoImageOpenStatus *status);

MonoAssembly* mono_assembly_loaded     (MonoAssemblyName *aname);
MonoAssembly* mono_assembly_loaded_full (MonoAssemblyName *aname, mono_bool refonly);
void          mono_assembly_get_assemblyref (MonoImage *image, int index, MonoAssemblyName *aname);
void          mono_assembly_load_reference (MonoImage *image, int index);
void          mono_assembly_load_references (MonoImage *image, MonoImageOpenStatus *status);
MonoImage*    mono_assembly_load_module (MonoAssembly *assembly, uint32_t idx);
void          mono_assembly_close      (MonoAssembly *assembly);
void          mono_assembly_setrootdir (const char *root_dir);
MONO_CONST_RETURN char *mono_assembly_getrootdir (void);
void	      mono_assembly_foreach    (MonoFunc func, void* user_data);
void          mono_assembly_set_main   (MonoAssembly *assembly);
MonoAssembly *mono_assembly_get_main   (void);
MonoImage    *mono_assembly_get_image  (MonoAssembly *assembly);
mono_bool      mono_assembly_fill_assembly_name (MonoImage *image, MonoAssemblyName *aname);
mono_bool      mono_assembly_names_equal (MonoAssemblyName *l, MonoAssemblyName *r);
char*         mono_stringify_assembly_name (MonoAssemblyName *aname);

/* Installs a function which is called each time a new assembly is loaded. */
typedef void  (*MonoAssemblyLoadFunc)         (MonoAssembly *assembly, void* user_data);
void          mono_install_assembly_load_hook (MonoAssemblyLoadFunc func, void* user_data);

/* 
 * Installs a new function which is used to search the list of loaded 
 * assemblies for a given assembly name.
 */
typedef MonoAssembly *(*MonoAssemblySearchFunc)         (MonoAssemblyName *aname, void* user_data);
void          mono_install_assembly_search_hook (MonoAssemblySearchFunc func, void* user_data);
void 	      mono_install_assembly_refonly_search_hook (MonoAssemblySearchFunc func, void* user_data);

MonoAssembly* mono_assembly_invoke_search_hook (MonoAssemblyName *aname);

/*
 * Installs a new search function which is used as a last resort when loading 
 * an assembly fails. This could invoke AssemblyResolve events.
 */
void          
mono_install_assembly_postload_search_hook (MonoAssemblySearchFunc func, void* user_data);

void          
mono_install_assembly_postload_refonly_search_hook (MonoAssemblySearchFunc func, void* user_data);


/* Installs a function which is called before a new assembly is loaded
 * The hook are invoked from last hooked to first. If any of them returns
 * a non-null value, that will be the value returned in mono_assembly_load */
typedef MonoAssembly * (*MonoAssemblyPreLoadFunc) (MonoAssemblyName *aname,
						   char **assemblies_path,
						   void* user_data);

void          mono_install_assembly_preload_hook (MonoAssemblyPreLoadFunc func,
						  void* user_data);
void          mono_install_assembly_refonly_preload_hook (MonoAssemblyPreLoadFunc func,
						  void* user_data);

void          mono_assembly_invoke_load_hook (MonoAssembly *ass);

MonoAssemblyName* mono_assembly_name_new             (const char *name);
const char*       mono_assembly_name_get_name        (MonoAssemblyName *aname);
const char*       mono_assembly_name_get_culture     (MonoAssemblyName *aname);
uint16_t          mono_assembly_name_get_version     (MonoAssemblyName *aname,
						      uint16_t *minor, uint16_t *build, uint16_t *revision);
mono_byte*        mono_assembly_name_get_pubkeytoken (MonoAssemblyName *aname);
void              mono_assembly_name_free            (MonoAssemblyName *aname);

typedef struct {
	const char *name;
	const unsigned char *data;
	const unsigned int size;
} MonoBundledAssembly;

void          mono_register_bundled_assemblies (const MonoBundledAssembly **assemblies);
void          mono_register_config_for_assembly (const char* assembly_name, const char* config_xml);
void          mono_register_symfile_for_assembly (const char* assembly_name, const mono_byte *raw_contents, int size);
void	      mono_register_machine_config (const char *config_xml);

void          mono_set_rootdir (void);
void          mono_set_dirs (const char *assembly_dir, const char *config_dir);
void          mono_set_assemblies_path (const char* path);
MONO_END_DECLS

#endif

