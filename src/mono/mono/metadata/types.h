#ifndef _MONO_METADATA_TYPES_H_
#define _MONO_METADATA_TYPES_H_

typedef struct _MonoImage MonoImage;

enum MonoImageOpenStatus {
	MONO_IMAGE_OK,
	MONO_IMAGE_ERROR_ERRNO,
	MONO_IMAGE_MISSING_ASSEMBLYREF,
	MONO_IMAGE_IMAGE_INVALID
};

typedef struct _MonoAssembly MonoAssembly;

typedef char * (*MonoAssemblyResolverFn)(const char *name);

#endif
