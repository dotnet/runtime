/*
 * helpers.c: Assorted routines
 *
 * (C) 2003 Ximian, Inc.
 */
#include "mini.h"
#include <ctype.h>
#include <mono/metadata/opcodes.h>

#ifndef PLATFORM_WIN32
#include <unistd.h>
#endif

#ifdef MINI_OP
#undef MINI_OP
#endif

#ifdef HAVE_ARRAY_ELEM_INIT
#define MSGSTRFIELD(line) MSGSTRFIELD1(line)
#define MSGSTRFIELD1(line) str##line
static const struct msgstr_t {
#define MINI_OP(a,b) char MSGSTRFIELD(__LINE__) [sizeof (b)];
#include "mini-ops.h"
#undef MINI_OP
} opstr = {
#define MINI_OP(a,b) b,
#include "mini-ops.h"
#undef MINI_OP
};
static const gint16 opidx [] = {
#define MINI_OP(a,b) [a - OP_LOAD] = offsetof (struct msgstr_t, MSGSTRFIELD(__LINE__)),
#include "mini-ops.h"
#undef MINI_OP
};

#else

#define MINI_OP(a,b) b,
/* keep in sync with the enum in mini.h */
static const char* const
opnames[] = {
#include "mini-ops.h"
};
#undef MINI_OP

#endif

#if defined(__i386__) || defined(__x86_64__)
#define emit_debug_info  TRUE
#else
#define emit_debug_info  FALSE
#endif

const char*
mono_inst_name (int op) {
	if (op >= OP_LOAD && op <= OP_LAST)
#ifdef HAVE_ARRAY_ELEM_INIT
		return (const char*)&opstr + opidx [op - OP_LOAD];
#else
		return opnames [op - OP_LOAD];
#endif
	if (op < OP_LOAD)
		return mono_opcode_name (op);
	g_error ("unknown opcode name for %d", op);
	return NULL;
}

void
mono_blockset_print (MonoCompile *cfg, MonoBitSet *set, const char *name, guint idom) 
{
#ifndef DISABLE_LOGGING
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
#endif
}

/**
 * mono_disassemble_code:
 * @cfg: compilation context
 * @code: a pointer to the code
 * @size: the code size in bytes
 *
 * Disassemble to code to stdout.
 */
void
mono_disassemble_code (MonoCompile *cfg, guint8 *code, int size, char *id)
{
#ifndef DISABLE_LOGGING
	GHashTable *offset_to_bb_hash = NULL;
	int i, cindex, bb_num;
	FILE *ofd;
#ifdef PLATFORM_WIN32
	const char *tmp = g_get_tmp_dir ();
#endif
	const char *objdump_args = g_getenv ("MONO_OBJDUMP_ARGS");
	char *as_file;
	char *o_file;
	char *cmd;

#ifdef PLATFORM_WIN32
	as_file = g_strdup_printf ("%s/test.s", tmp);    

	if (!(ofd = fopen (as_file, "w")))
		g_assert_not_reached ();
#else	
	i = g_file_open_tmp (NULL, &as_file, NULL);
	ofd = fdopen (i, "w");
	g_assert (ofd);
#endif

	for (i = 0; id [i]; ++i) {
		if (!isalnum (id [i]))
			fprintf (ofd, "_");
		else
			fprintf (ofd, "%c", id [i]);
	}
	fprintf (ofd, ":\n");

	if (emit_debug_info && cfg != NULL) {
		MonoBasicBlock *bb;

		fprintf (ofd, ".stabs	\"\",100,0,0,.Ltext0\n");
		fprintf (ofd, ".stabs	\"<BB>\",100,0,0,.Ltext0\n");
		fprintf (ofd, ".Ltext0:\n");

		offset_to_bb_hash = g_hash_table_new (NULL, NULL);
		for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
			g_hash_table_insert (offset_to_bb_hash, GINT_TO_POINTER (bb->native_offset), GINT_TO_POINTER (bb->block_num + 1));
		}
	}

	cindex = 0;
	for (i = 0; i < size; ++i) {
		if (emit_debug_info) {
			bb_num = GPOINTER_TO_INT (g_hash_table_lookup (offset_to_bb_hash, GINT_TO_POINTER (i)));
			if (bb_num) {
				fprintf (ofd, "\n.stabd 68,0,%d\n", bb_num - 1);
				cindex = 0;
			}
		}
		if (cindex == 0) {
			fprintf (ofd, "\n.byte %d", (unsigned int) code [i]);
		} else {
			fprintf (ofd, ",%d", (unsigned int) code [i]);
		}
		cindex++;
		if (cindex == 64)
			cindex = 0;
	}
	fprintf (ofd, "\n");
	fclose (ofd);
#ifdef __APPLE__
#ifdef __ppc64__
#define DIS_CMD "otool64 -v -t"
#else
#define DIS_CMD "otool -v -t"
#endif
#else
#if defined(sparc) && !defined(__GNUC__)
#define DIS_CMD "dis"
#elif defined(__i386__) || defined(__x86_64__)
#define DIS_CMD "objdump -l -d"
#else
#define DIS_CMD "objdump -d"
#endif
#endif

#if defined(sparc)
#define AS_CMD "as -xarch=v9"
#elif defined(__i386__) || defined(__x86_64__)
#define AS_CMD "as -gstabs"
#elif defined(__mips__)
#define AS_CMD "as -mips32"
#elif defined(__ppc64__)
#define AS_CMD "as -arch ppc64"
#elif defined(__powerpc64__)
#define AS_CMD "as -mppc64"
#else
#define AS_CMD "as"
#endif

#ifdef PLATFORM_WIN32
	o_file = g_strdup_printf ("%s/test.o", tmp);
#else	
	i = g_file_open_tmp (NULL, &o_file, NULL);
	close (i);
#endif

	cmd = g_strdup_printf (AS_CMD " %s -o %s", as_file, o_file);
	system (cmd); 
	g_free (cmd);
	if (!objdump_args)
		objdump_args = "";

#ifdef __arm__
	/* 
	 * The arm assembler inserts ELF directives instructing objdump to display 
	 * everything as data.
	 */
	cmd = g_strdup_printf ("strip -x %s", o_file);
	system (cmd);
	g_free (cmd);
#endif
	
	cmd = g_strdup_printf (DIS_CMD " %s %s", objdump_args, o_file);
	system (cmd);
	g_free (cmd);
	
#ifndef PLATFORM_WIN32
	unlink (o_file);
	unlink (as_file);
#endif
	g_free (o_file);
	g_free (as_file);
#endif
}

