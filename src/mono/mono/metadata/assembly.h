#ifndef _MONONET_METADATA_ASSEMBLY_H_ 
#define _MONONET_METADATA_ASSEMBLY_H_

typedef struct {
	FILE *f;
	void *image_info;
} MonoAssembly;

enum MonoAssemblyOpenStatus {
	MONO_ASSEMBLY_OK,
	MONO_ASSEMBLY_ERROR_ERRNO,
	MONO_ASSEMBLY_IMAGE_INVALID
};

MonoAssembly *mono_assembly_open     (const char *fname,
				      enum MonoAssemblyOpenStatus *status);
void          mono_assembly_close    (MonoAssembly *assembly);
const char   *mono_assembly_strerror (enum MonoAssemblyOpenStatus status);


int           mono_assembly_ensure_section     (MonoAssembly *assembly,
					       const char *section);
int           mono_assembly_ensure_section_idx (MonoAssembly *assembly,
					       int section);
	
#endif
