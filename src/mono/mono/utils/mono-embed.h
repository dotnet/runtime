#ifndef __MONO_EMBED_H__
#define __MONO_EMBED_H__

/* 
 * These are only used and available on embedded systems, the
 * EMBEDDED_PINVOKE configuration option must be set, and it
 * overrides any platform symbol loading functionality 
 */
typedef struct {
	const char *name;	
	void *addr;
} MonoDlMapping;

void mono_dl_register_library (const char *name, MonoDlMapping *mappings);

#endif /* __MONO_EMBED_H__ */
