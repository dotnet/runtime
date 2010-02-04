#ifndef __MONO_METADATA_BASIC_BLOCK_H__
#define __MONO_METADATA_BASIC_BLOCK_H__

#include <glib.h>


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
mono_basic_block_split (MonoMethod *method, MonoError *error) MONO_INTERNAL;

void
mono_basic_block_free (MonoSimpleBasicBlock *bb) MONO_INTERNAL;


/*This function is here because opcodes.h is a public header*/
int
mono_opcode_value_and_size (const unsigned char **ip, const unsigned char *end, int *value) MONO_INTERNAL;

int
mono_opcode_size (const unsigned char *ip, const unsigned char *end) MONO_INTERNAL;

G_END_DECLS

#endif  /* __MONO_METADATA_BASIC_BLOCK_H__ */

