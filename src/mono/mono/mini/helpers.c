/*
 * helpers.c: Assorted routines
 *
 * (C) 2003 Ximian, Inc.
 */
#include "mini.h"
#include <ctype.h>
#include <mono/metadata/opcodes.h>

#ifdef MINI_OP
#undef MINI_OP
#endif
#define MINI_OP(a,b) b,
/* keep in sync with the enum in mini.h */
static const char* 
opnames[] = {
#include "mini-ops.h"
};
#undef MINI_OP

const char*
mono_inst_name (int op) {
	if (op >= OP_LOAD && op <= OP_LAST)
		return opnames [op - OP_LOAD];
	if (op < OP_LOAD)
		return mono_opcode_names [op];
	g_error ("unknown opcode name for %d", op);
	return NULL;
}

void
mono_blockset_print (MonoCompile *cfg, MonoBitSet *set, const char *name, guint idom) 
{
	int i;

	if (name)
		g_print ("%s:", name);
	
	mono_bitset_foreach_bit (set, i, cfg->num_bblocks) {
		if (idom == i)
			g_print (" [BB%d]", cfg->bblocks [i]->block_num);
		else
			g_print (" BB%d", cfg->bblocks [i]->block_num);
		
	}
	g_print ("\n");
}

/**
 * mono_disassemble_code:
 * @code: a pointer to the code
 * @size: the code size in bytes
 *
 * Disassemble to code to stdout.
 */
void
mono_disassemble_code (guint8 *code, int size, char *id)
{
	int i;
	FILE *ofd;
	const char *tmp = getenv("TMP");
	char *as_file;
	char *o_file;
	char *cmd;

	if (tmp == NULL)
		tmp = "/tmp";
	as_file = g_strdup_printf ("%s/test.s", tmp);    

	if (!(ofd = fopen (as_file, "w")))
		g_assert_not_reached ();

	for (i = 0; id [i]; ++i) {
		if (!isalnum (id [i]))
			fprintf (ofd, "_");
		else
			fprintf (ofd, "%c", id [i]);
	}
	fprintf (ofd, ":\n");

	for (i = 0; i < size; ++i) 
		fprintf (ofd, ".byte %d\n", (unsigned int) code [i]);

	fclose (ofd);
#ifdef __APPLE__
#define DIS_CMD "otool -V -v -t"
#else
#define DIS_CMD "objdump -d"
#endif
	o_file = g_strdup_printf ("%s/test.o", tmp);    
	cmd = g_strdup_printf ("as %s -o %s", as_file, o_file);
	system (cmd); 
	g_free (cmd);
	cmd = g_strdup_printf (DIS_CMD " %s", o_file);
	system (cmd); 
	g_free (cmd);
	g_free (o_file);
	g_free (as_file);
}

