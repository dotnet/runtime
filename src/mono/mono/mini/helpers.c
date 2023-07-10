/**
 * \file
 * Assorted routines
 *
 * (C) 2003 Ximian, Inc.
 */

#include <config.h>

#include "mini.h"
#include <ctype.h>
#include <mono/metadata/opcodes.h>

#ifndef HOST_WIN32
#include <unistd.h>
#endif

#ifndef DISABLE_JIT

#ifndef DISABLE_LOGGING

#ifdef MINI_OP
#undef MINI_OP
#endif
#ifdef MINI_OP3
#undef MINI_OP3
#endif

// This, instead of an array of pointers, to optimize away a pointer and a relocation per string.
#define MSGSTRFIELD(line) MSGSTRFIELD1(line)
#define MSGSTRFIELD1(line) str##line
static const struct msgstr_t {
#define MINI_OP(a,b,dest,src1,src2) char MSGSTRFIELD(__LINE__) [sizeof (b)];
#define MINI_OP3(a,b,dest,src1,src2,src3) char MSGSTRFIELD(__LINE__) [sizeof (b)];
#include "mini-ops.h"
#undef MINI_OP
#undef MINI_OP3
} opstr = {
#define MINI_OP(a,b,dest,src1,src2) b,
#define MINI_OP3(a,b,dest,src1,src2,src3) b,
#include "mini-ops.h"
#undef MINI_OP
#undef MINI_OP3
};
static const gint16 opidx [] = {
#define MINI_OP(a,b,dest,src1,src2)       offsetof (struct msgstr_t, MSGSTRFIELD(__LINE__)),
#define MINI_OP3(a,b,dest,src1,src2,src3) offsetof (struct msgstr_t, MSGSTRFIELD(__LINE__)),
#include "mini-ops.h"
#undef MINI_OP
#undef MINI_OP3
};

#endif /* DISABLE_LOGGING */

#if defined(__i386__) || defined(_M_IX86) || defined(__x86_64__) || defined(_M_X64)
#if !defined(TARGET_ARM64) && !defined(__APPLE__)
#define emit_debug_info  TRUE
#else
#define emit_debug_info  FALSE
#endif
#else
#define emit_debug_info  FALSE
#endif

/*This enables us to use the right tooling when building the cross compiler for iOS.*/
#if defined (__APPLE__) && defined (TARGET_ARM) && (defined(__i386__) || defined(__x86_64__))

//#define ARCH_PREFIX "/Developer/Platforms/iPhoneOS.platform/Developer/usr/bin/"

#endif

#ifdef TARGET_RISCV64
#define ARCH_PREFIX "riscv64-linux-gnu-"
#else
#define ARCH_PREFIX ""
#endif
//#define ARCH_PREFIX "powerpc64-linux-gnu-"

const char*
mono_inst_name (int op) {
#ifndef DISABLE_LOGGING
	if (op >= OP_LOAD && op <= OP_LAST)
		return (const char*)&opstr + opidx [op - OP_LOAD];
	if (op < OP_LOAD)
		return mono_opcode_name (op);
	g_error ("unknown opcode name for %d", op);
	return NULL;
#else
	g_error ("unknown opcode name for %d", op);
	g_assert_not_reached ();
#endif
}

void
mono_blockset_print (MonoCompile *cfg, MonoBitSet *set, const char *name, guint idom)
{
#ifndef DISABLE_LOGGING
	guint i;

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
 * \param cfg compilation context
 * \param code a pointer to the code
 * \param size the code size in bytes
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
#ifdef HOST_WIN32
	const char *tmp = g_get_tmp_dir ();
#endif
	char *as_file;
	char *o_file;
	int unused G_GNUC_UNUSED;

#ifdef HOST_WIN32
	as_file = g_strdup_printf ("%s/test.s", tmp);

	if (!(ofd = fopen (as_file, "w")))
		g_assert_not_reached ();
#else
	i = g_file_open_tmp (NULL, &as_file, NULL);
	ofd = fdopen (i, "w");
	g_assert (ofd);
#endif

	for (i = 0; id [i]; ++i) {
		if (i == 0 && isdigit (id [i]))
			fprintf (ofd, "_");
		else if (!isalnum (id [i]))
			fprintf (ofd, "_");
		else
			fprintf (ofd, "%c", id [i]);
	}
	fprintf (ofd, ":\n");

MONO_DISABLE_WARNING(4127) /* conditional expression is constant */

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
		if (emit_debug_info && cfg != NULL) {
			bb_num = GPOINTER_TO_INT (g_hash_table_lookup (offset_to_bb_hash, GINT_TO_POINTER (i)));
			if (bb_num) {
				fprintf (ofd, "\n.stabd 68,0,%d\n", bb_num - 1);
				cindex = 0;
			}
		}
		if (cindex == 0) {
			fprintf (ofd, "\n.byte %u", (unsigned int) code [i]);
		} else {
			fprintf (ofd, ",%u", (unsigned int) code [i]);
		}
		cindex++;
		if (cindex == 64)
			cindex = 0;
	}
	fprintf (ofd, "\n");
	fclose (ofd);

MONO_RESTORE_WARNING

#ifdef __APPLE__
#ifdef __ppc64__
#define DIS_CMD "otool64 -v -t"
#else
#define DIS_CMD "otool -v -t"
#endif
#else
#if defined(TARGET_X86)
#define DIS_CMD "objdump -l -d"
#elif defined(TARGET_AMD64)
  #if defined(HOST_WIN32)
  #define DIS_CMD "x86_64-w64-mingw32-objdump.exe -M x86-64 -d"
  #else
  #define DIS_CMD "objdump -l -d"
  #endif
#else
#define DIS_CMD "objdump -d"
#endif
#endif

#if defined (TARGET_X86)
#  if defined(__APPLE__)
#    define AS_CMD "as -arch i386"
#  else
#    define AS_CMD "as -gstabs"
#  endif
#elif defined (TARGET_AMD64)
#  if defined (__APPLE__)
#    define AS_CMD "as -arch x86_64"
#  else
#    define AS_CMD "as -gstabs"
#  endif
#elif defined (TARGET_ARM)
#  if defined (__APPLE__)
#    define AS_CMD "as -arch arm"
#  else
#    define AS_CMD "as -gstabs"
#  endif
#elif defined (TARGET_ARM64)
#  if defined (__APPLE__)
#    define AS_CMD "clang -c -arch arm64 -g -x assembler"
#  else
#    define AS_CMD "as -gstabs"
#  endif
#elif defined(__ppc64__)
#define AS_CMD "as -arch ppc64"
#elif defined(__powerpc64__)
#define AS_CMD "as -mppc64"
#elif defined (TARGET_RISCV64)
#define AS_CMD "as -march=rv64ima"
#elif defined (TARGET_RISCV32)
#define AS_CMD "as -march=rv32ima"
#else
#define AS_CMD "as"
#endif

#ifdef HOST_WIN32
	o_file = g_strdup_printf ("%s/test.o", tmp);
#else
	i = g_file_open_tmp (NULL, &o_file, NULL);
	close (i);
#endif

#ifdef HAVE_SYSTEM
	char *cmd = g_strdup_printf (ARCH_PREFIX AS_CMD " %s -o %s", as_file, o_file);
	unused = system (cmd);
	g_free (cmd);
	char *objdump_args = g_getenv ("MONO_OBJDUMP_ARGS");
	if (!objdump_args)
		objdump_args = g_strdup ("");

	fflush (stdout);

#if (defined(__arm__) || defined(__aarch64__)) && !defined(TARGET_OSX)
	/*
	 * The arm assembler inserts ELF directives instructing objdump to display
	 * everything as data.
	 */
	cmd = g_strdup_printf (ARCH_PREFIX "strip -s %s", o_file);
	unused = system (cmd);
	g_free (cmd);
#endif

	cmd = g_strdup_printf (ARCH_PREFIX DIS_CMD " %s %s", objdump_args, o_file);
	unused = system (cmd);
	g_free (cmd);
	g_free (objdump_args);
#else
	g_assert_not_reached ();
#endif /* HAVE_SYSTEM */

#ifndef HOST_WIN32
	unlink (o_file);
	unlink (as_file);
#endif
	g_free (o_file);
	g_free (as_file);
#endif
}

#else /* DISABLE_JIT */

void
mono_blockset_print (MonoCompile *cfg, MonoBitSet *set, const char *name, guint idom)
{
}

#endif /* DISABLE_JIT */
