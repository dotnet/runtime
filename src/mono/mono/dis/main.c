/*
 * dis.c: Sample disassembler
 *
 * Author:
 *   Miguel de Icaza (miguel@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */
#include <config.h>
#include <stdio.h>
#include <glib.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/cil-coff.h>

FILE *output;

/* True if you want to get a dump of the header data */
gboolean dump_header_data_p = FALSE;

static void
dump_header_data (MonoAssembly *ass)
{
	if (!dump_header_data_p)
		return;

	fprintf (output,
		 "// Ximian's CIL disassembler, version 1.0\n"
		 "// Copyright (C) 2001 Ximian, Inc.\n\n");
}

static void
disassemble_file (const char *file)
{
	enum MonoAssemblyOpenStatus status;
	MonoAssembly *ass;

	ass = mono_assembly_open (file, &status);
	if (ass == NULL){
		fprintf (stderr, "Error while trying to process %s\n", file);
		
	}

	dump_header_data (ass);

	mono_assembly_close (ass);
}

static void
usage (void)
{
	fprintf (stderr, "Usage is: monodis file1 ..\n");
	exit (1);
}

int
main (int argc, char *argv [])
{
	GList *input_files = NULL, *l;
	int i;

	output = stdout;
	for (i = 1; i < argc; i++){
		if (argv [i][0] == '-'){
			if (argv [i][1] == 'h')
				usage ();
			else if (argv [i][1] == 'd')
				dump_header_data_p = TRUE;
		} else
			input_files = g_list_append (input_files, argv [i]);
	}

	if (input_files == NULL)
		usage ();
	
	for (l = input_files; l; l = l->next)
		disassemble_file (l->data);
}
