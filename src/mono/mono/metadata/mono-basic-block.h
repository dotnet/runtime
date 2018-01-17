/**
 * \file
 */

#ifndef __MONO_METADATA_BASIC_BLOCK_H__
#define __MONO_METADATA_BASIC_BLOCK_H__

#include <glib.h>
#include <mono/metadata/metadata.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-error.h>

G_BEGIN_DECLS

typedef struct _MonoSimpleBasicBlock MonoSimpleBasicBlock;

struct _MonoSimpleBasicBlock {
	MonoSimpleBasicBlock *next, *left, *right, *parent;
	GSList *out_bb;
	int start, end;
	unsigned colour   : 1;
	unsigned dead     : 1;
};

MonoSimpleBasicBlock*
mono_basic_block_split (MonoMethod *method, MonoError *error, MonoMethodHeader *header);

void
mono_basic_block_free (MonoSimpleBasicBlock *bb);


/*This function is here because opcodes.h is a public header*/
int
mono_opcode_value_and_size (const unsigned char **ip, const unsigned char *end, int *value);

int
mono_opcode_size (const unsigned char *ip, const unsigned char *end);

G_END_DECLS

#endif  /* __MONO_METADATA_BASIC_BLOCK_H__ */

