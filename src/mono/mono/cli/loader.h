#ifndef _MONO_CLI_CLI_H_
#define _MONO_CLI_CLI_H_ 1

typedef struct {
	MonoMetaMethodHeader *header;
	MonoMethodSignature  *signature;
	guint32 name; /* index in string heap */
	/* add flags, info from param table  ... */
} MonoMethod;


#endif
