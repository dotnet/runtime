#include <config.h>
#include <stdlib.h>
#include <string.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/rawbuffer.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/debug-symfile.h>

#include <fcntl.h>
#include <unistd.h>

#ifdef HAVE_ELF_H
#include <elf.h>
#endif

/* Keep in sync with Mono.CSharp.Debugger.MonoDwarfFileWriter */
#define MRT_none			0x00
#define MRT_target_address_size		0x01
#define MRT_il_offset			0x02
#define MRT_method_start_address	0x03
#define MRT_method_end_address		0x04
#define MRT_local_variable		0x05
#define MRT_method_parameter		0x06
#define MRT_type_sizeof			0x07
#define MRT_type_field_offset		0x08
#define MRT_mono_string_sizeof		0x09
#define MRT_mono_string_offset		0x0a
#define MRT_mono_array_sizeof		0x0b
#define MRT_mono_array_offset		0x0c
#define MRT_mono_array_bounds_sizeof	0x0d
#define MRT_mono_array_bounds_offset	0x0e
#define MRT_variable_start_scope	0x0f
#define MRT_variable_end_scope		0x10
#define MRT_mono_string_fieldsize	0x11
#define MRT_mono_array_fieldsize	0x12
#define MRT_type_field_fieldsize	0x13

#define MRI_string_offset_length	0x00
#define MRI_string_offset_chars		0x01

#define MRI_array_offset_bounds		0x00
#define MRI_array_offset_max_length	0x01
#define MRI_array_offset_vector		0x02

#define MRI_array_bounds_offset_lower	0x00
#define MRI_array_bounds_offset_length	0x01

#define MRS_debug_info			0x01
#define MRS_debug_abbrev		0x02
#define MRS_debug_line			0x03
#define MRS_mono_reloc_table		0x04

#define DW_OP_const4s			0x0d
#define DW_OP_plus			0x22
#define DW_OP_reg0			0x50
#define DW_OP_breg0			0x70
#define DW_OP_fbreg			0x91
#define DW_OP_piece			0x93
#define DW_OP_nop			0x96

#ifdef HAVE_ELF_H

static gboolean
get_sections_elf32 (MonoDebugSymbolFile *symfile, gboolean emit_warnings)
{
	Elf32_Ehdr *header;
	Elf32_Shdr *section, *strtab_section;
	const char *strtab;
	int i;

	header = (Elf32_Ehdr *)symfile->raw_contents;
	if (header->e_version != EV_CURRENT) {
		if (emit_warnings)
			g_warning ("Symbol file %s has unknown ELF version %d",
				   symfile->file_name, header->e_version);
		return FALSE;
	}

	if (header->e_machine != EM_386) {
		if (emit_warnings)
			g_warning ("ELF file %s is for unknown architecture %d",
				   symfile->file_name, header->e_machine);
		return FALSE;
	}

	if (header->e_shentsize != sizeof (*section)) {
		if (emit_warnings)
			g_warning ("ELF file %s has unknown section header size "
				   "(expected %d, got %d)", symfile->file_name,
				   sizeof (*section), header->e_shentsize);
		return FALSE;
	}

	symfile->section_offsets = g_new0 (MonoDebugSymbolFileSection, MONO_DEBUG_SYMBOL_SECTION_MAX);

	section = (Elf32_Shdr *)(symfile->raw_contents + header->e_shoff);
	strtab_section = section + header->e_shstrndx;
	strtab = symfile->raw_contents + strtab_section->sh_offset;

	for (i = 0; i < header->e_shnum; i++, section++) {
		const gchar *name = strtab + section->sh_name;

		if (!strcmp (name, ".debug_info")) {
			MonoDebugSymbolFileSection *sfs;

			sfs = &symfile->section_offsets [MONO_DEBUG_SYMBOL_SECTION_DEBUG_INFO];
			sfs->type = MONO_DEBUG_SYMBOL_SECTION_DEBUG_INFO;
			sfs->file_offset = section->sh_offset;
			sfs->size = section->sh_size;
		} else if (!strcmp (name, ".debug_line")) {
			MonoDebugSymbolFileSection *sfs;

			sfs = &symfile->section_offsets [MONO_DEBUG_SYMBOL_SECTION_DEBUG_LINE];
			sfs->type = MONO_DEBUG_SYMBOL_SECTION_DEBUG_LINE;
			sfs->file_offset = section->sh_offset;
			sfs->size = section->sh_size;
		} else if (!strcmp (name, ".debug_abbrev")) {
			MonoDebugSymbolFileSection *sfs;

			sfs = &symfile->section_offsets [MONO_DEBUG_SYMBOL_SECTION_DEBUG_ABBREV];
			sfs->type = MONO_DEBUG_SYMBOL_SECTION_DEBUG_ABBREV;
			sfs->file_offset = section->sh_offset;
			sfs->size = section->sh_size;
		} else if (!strcmp (name, ".mono_reloc_table")) {
			MonoDebugSymbolFileSection *sfs;

			sfs = &symfile->section_offsets [MONO_DEBUG_SYMBOL_SECTION_MONO_RELOC_TABLE];
			sfs->type = MONO_DEBUG_SYMBOL_SECTION_MONO_RELOC_TABLE;
			sfs->file_offset = section->sh_offset;
			sfs->size = section->sh_size;
		} else if (!strcmp (name, ".mono_line_numbers")) {
			MonoDebugSymbolFileSection *sfs;

			sfs = &symfile->section_offsets [MONO_DEBUG_SYMBOL_SECTION_MONO_LINE_NUMBERS];
			sfs->type = MONO_DEBUG_SYMBOL_SECTION_MONO_LINE_NUMBERS;
			sfs->file_offset = section->sh_offset;
			sfs->size = section->sh_size;
		}
	}

	return TRUE;
}

#endif /* HAVE_ELF_H */

static gboolean
get_sections (MonoDebugSymbolFile *symfile, gboolean emit_warnings)
{
#ifdef HAVE_ELF_H
#ifdef __FreeBSD__
	static const char ELFMAG[] = { ELFMAG0, ELFMAG1, ELFMAG2, ELFMAG3, 0 };
#endif
	if (!strncmp (symfile->raw_contents, ELFMAG, strlen (ELFMAG)))
		return get_sections_elf32 (symfile, emit_warnings);
#endif

	if (emit_warnings)
		g_warning ("Symbol file %s has unknown file format", symfile->file_name);

	return FALSE;
}

static void
read_line_numbers (MonoDebugSymbolFile *symfile)
{
	const char *ptr, *start, *end;
	int version;
	long section_size;

	if (!symfile->section_offsets [MONO_DEBUG_SYMBOL_SECTION_MONO_LINE_NUMBERS].file_offset)
		return;

	ptr = start = symfile->raw_contents +
		symfile->section_offsets [MONO_DEBUG_SYMBOL_SECTION_MONO_LINE_NUMBERS].file_offset;

	version = *((guint16 *) ptr)++;
	if (version != MONO_DEBUG_SYMBOL_FILE_VERSION) {
		g_warning ("Symbol file %s has incorrect line number table version "
			   "(expected %d, got %d)", symfile->file_name,
			   MONO_DEBUG_SYMBOL_FILE_VERSION, version);
		return;
	}

	section_size = *((guint32 *) ptr)++;
	end = ptr + section_size;

	symfile->line_number_table = g_hash_table_new_full (g_direct_hash, g_direct_equal,
							    NULL, (GDestroyNotify) g_free);

	while (ptr < end) {
		MonoDebugLineNumberBlock *lnb;
		guint32 token, source_offset;
		MonoMethod *method;

		token = * ((guint32 *) ptr)++;
		method = mono_get_method (symfile->image, token, NULL);
		if (!method)
			continue;

		lnb = g_new0 (MonoDebugLineNumberBlock, 1);
		lnb->token = token;
		source_offset = * ((guint32 *) ptr)++;
		lnb->source_file = (const char *) start + source_offset;
		lnb->start_line = * ((guint32 *) ptr)++;
		lnb->file_offset = * ((guint32 *) ptr)++;

		g_hash_table_insert (symfile->line_number_table, method, lnb);
	}
}

static MonoClass *
mono_debug_class_get (MonoDebugSymbolFile *symfile, guint32 type_token)
{
	MonoClass *klass;

	if ((klass = g_hash_table_lookup (symfile->image->class_cache, GUINT_TO_POINTER (type_token))))
		return klass;

	return NULL;
}

MonoDebugSymbolFile *
mono_debug_open_symbol_file (MonoImage *image, const char *filename, gboolean emit_warnings)
{
	MonoDebugSymbolFile *symfile;
	off_t file_size;
	void *ptr;
	int fd;

	fd = open (filename, O_RDWR);
	if (fd == -1) {
		if (emit_warnings)
			g_warning ("Can't open symbol file: %s", filename);
		return NULL;
	}

	file_size = lseek (fd, 0, SEEK_END);
	lseek (fd, 0, SEEK_SET);

	if (file_size == (off_t) -1) {
		if (emit_warnings)
			g_warning ("Can't get size of symbol file: %s", filename);
		return NULL;
	}

	ptr = mono_raw_buffer_load (fd, 1, 0, file_size);
	if (!ptr) {
		if (emit_warnings)
			g_warning ("Can't read symbol file: %s", filename);
		return NULL;
	}

	symfile = g_new0 (MonoDebugSymbolFile, 1);
	symfile->fd = fd;
	symfile->file_name = g_strdup (filename);
	symfile->image = image;
	symfile->raw_contents = ptr;
	symfile->raw_contents_size = file_size;

	if (!get_sections (symfile, emit_warnings)) {
		mono_debug_close_symbol_file (symfile);
		return NULL;
	}

	read_line_numbers (symfile);

	return symfile;
}

void
mono_debug_close_symbol_file (MonoDebugSymbolFile *symfile)
{
	if (!symfile)
		return;

	if (symfile->raw_contents)
		mono_raw_buffer_free (symfile->raw_contents);
	if (symfile->fd)
		close (symfile->fd);

	if (symfile->line_number_table)
		g_hash_table_destroy (symfile->line_number_table);
	g_free (symfile->file_name);
	g_free (symfile->section_offsets);
	g_free (symfile);
}

static void
relocate_variable (MonoDebugVarInfo *var, void *base_ptr)
{
	if (!var) {
		* ((guint8 *) base_ptr)++ = DW_OP_nop;
		* ((guint8 *) base_ptr)++ = DW_OP_nop;
		* ((guint8 *) base_ptr)++ = DW_OP_nop;
		* ((guint8 *) base_ptr)++ = DW_OP_nop;
		* ((guint8 *) base_ptr)++ = DW_OP_nop;
		* ((guint8 *) base_ptr)++ = DW_OP_nop;
		* ((guint8 *) base_ptr)++ = DW_OP_nop;
		* ((guint8 *) base_ptr)++ = DW_OP_nop;
		return;
	}
		
	/*
	 * Update the location description for a local variable or method parameter.
	 * MCS always reserves 8 bytes for us to do this, if we don't need them all
	 * we just fill up the rest with DW_OP_nop's.
	 */

	switch (var->index & MONO_DEBUG_VAR_ADDRESS_MODE_FLAGS) {
	case MONO_DEBUG_VAR_ADDRESS_MODE_STACK:
		/*
		 * Variable is on the stack.
		 *
		 * If `index' is zero, use the normal frame register.  Otherwise, bits
		 * 0..4 of `index' contain the frame register.
		 *
		 * Both DW_OP_fbreg and DW_OP_breg0 ... DW_OP_breg31 take an ULeb128
		 * argument - since this has an variable size, we set it to zero and
		 * manually add a 4 byte constant using DW_OP_plus.
		 */
		if (!var->index)
			/* Use the normal frame register (%ebp on the i386). */
			* ((guint8 *) base_ptr)++ = DW_OP_fbreg;
		else
			/* Use a custom frame register. */
			* ((guint8 *) base_ptr)++ = DW_OP_breg0 + (var->index & 0x001f);
		* ((guint8 *) base_ptr)++ = 0;
		* ((guint8 *) base_ptr)++ = DW_OP_const4s;
		* ((gint32 *) base_ptr)++ = var->offset;
		* ((guint8 *) base_ptr)++ = DW_OP_plus;
		break;

	case MONO_DEBUG_VAR_ADDRESS_MODE_REGISTER:
		/*
		 * Variable is in the register whose number is contained in bits 0..4
		 * of `index'.
		 *
		 * We need to write exactly 8 bytes in this location description, so instead
		 * of filling up the rest with DW_OP_nop's just add the `offset' even if
		 * it's zero.
		 */
		* ((guint8 *) base_ptr)++ = DW_OP_reg0 + (var->index & 0x001f);
		* ((guint8 *) base_ptr)++ = DW_OP_nop;
		* ((guint8 *) base_ptr)++ = DW_OP_const4s;
		* ((gint32 *) base_ptr)++ = var->offset;
		* ((guint8 *) base_ptr)++ = DW_OP_plus;
		break;

	case MONO_DEBUG_VAR_ADDRESS_MODE_TWO_REGISTERS:
		/*
		 * Variable is in two registers whose numbers are in bits 0..4 and 5..9 of 
		 * the `index' field.  Don't add `offset' since we have only two bytes left,
		 * fill them up with DW_OP_nop's.
		 */
		* ((guint8 *) base_ptr)++ = DW_OP_reg0 + (var->index & 0x001f);
		* ((guint8 *) base_ptr)++ = DW_OP_piece;
		* ((guint8 *) base_ptr)++ = sizeof (int);
		* ((guint8 *) base_ptr)++ = DW_OP_reg0 + ((var->index & 0x1f0) >> 5);
		* ((guint8 *) base_ptr)++ = DW_OP_piece;
		* ((guint8 *) base_ptr)++ = sizeof (int);
		* ((guint8 *) base_ptr)++ = DW_OP_nop;
		* ((guint8 *) base_ptr)++ = DW_OP_nop;
		break;

	default:
		g_assert_not_reached ();
	}
}

void
mono_debug_update_symbol_file (MonoDebugSymbolFile *symfile,
			       MonoDebugMethodInfoFunc method_info_func,
			       gpointer  user_data)
{
	const char *reloc_ptr, *reloc_start, *reloc_end;
	int version, already_relocated = 0;
	long reloc_size;

	if (!symfile->section_offsets [MONO_DEBUG_SYMBOL_SECTION_MONO_RELOC_TABLE].file_offset)
		return;

	reloc_ptr = reloc_start = symfile->raw_contents +
		symfile->section_offsets [MONO_DEBUG_SYMBOL_SECTION_MONO_RELOC_TABLE].file_offset;

	version = *((guint16 *) reloc_ptr)++;
	if (version != MONO_DEBUG_SYMBOL_FILE_VERSION) {
		g_warning ("Symbol file %s has incorrect relocation table version "
			   "(expected %d, got %d)", symfile->file_name,
			   MONO_DEBUG_SYMBOL_FILE_VERSION, version);
		return;
	}

	already_relocated = *reloc_ptr;
	*((char *) reloc_ptr)++ = 1;

	reloc_size = *((guint32 *) reloc_ptr)++;
	reloc_end = reloc_ptr + reloc_size;

	while (reloc_ptr < reloc_end) {
		int type, size, section, offset;
		const char *tmp_ptr;
		char *base_ptr;

		type = *reloc_ptr++;
		size = * ((guint32 *) reloc_ptr)++;

		tmp_ptr = reloc_ptr;
		reloc_ptr += size;

		section = *tmp_ptr++;
		offset = *((guint32 *) tmp_ptr)++;

		if (section >= MONO_DEBUG_SYMBOL_SECTION_MAX) {
			g_warning ("Symbol file %s contains a relocation entry for unknown section %d",
				   symfile->file_name, section);
			continue;
		}

		if (!symfile->section_offsets [section].file_offset) {
			g_warning ("Symbol file %s contains a relocation entry for non-existing "
				   "section %d", symfile->file_name, section);
			continue;
		}

		base_ptr = symfile->raw_contents + symfile->section_offsets [section].file_offset;
		base_ptr = base_ptr + offset;

		switch (type) {
		case MRT_target_address_size:
			* (guint8 *) base_ptr = sizeof (void *);
			break;
		case MRT_method_start_address: {
			int token = *((guint32 *) tmp_ptr)++;
			MonoDebugMethodInfo *minfo;

			minfo = method_info_func (symfile, token, user_data);

			if (!minfo) {
				* (void **) base_ptr = 0;
				continue;
			}

#if 0
			g_message ("Start of `%s' (%ld) relocated to %p", minfo->method->name,
				   token, minfo->code_start);
#endif

			* (void **) base_ptr = minfo->code_start;

			break;
		}
		case MRT_method_end_address: {
			int token = *((guint32 *) tmp_ptr)++;
			MonoDebugMethodInfo *minfo;

			minfo = method_info_func (symfile, token, user_data);

			if (!minfo) {
				* (void **) base_ptr = 0;
				continue;
			}

			* (void **) base_ptr = (char *)minfo->code_start + minfo->code_size;

			break;
		}
		case MRT_il_offset: {
			guint32 token = *((guint32 *) tmp_ptr)++;
			guint32 original = *((guint32 *) tmp_ptr)++;
			MonoDebugMethodInfo *minfo;
			guint32 address;
			int i;

			minfo = method_info_func (symfile, token, user_data);

			if (!minfo) {
				* (void **) base_ptr = 0;
				continue;
			}

			address = minfo->code_size;

			for (i = 0; i < minfo->num_il_offsets; i++) {
				MonoDebugILOffsetInfo *il = &minfo->il_offsets [i];

				if (il->offset >= original) {
					address = il->address;
					break;
				}
			}

#if 0
			g_message ("Relocating IL offset %04x in `%s' to %d (%p)",
				   original, minfo->method->name, address,
				   minfo->code_start + address);
#endif

			* (void **) base_ptr = minfo->code_start + address;

			break;
		}
		case MRT_local_variable: {
			guint32 token = *((guint32 *) tmp_ptr)++;
			guint32 original = *((guint32 *) tmp_ptr)++;
			MonoDebugMethodInfo *minfo;

			minfo = method_info_func (symfile, token, user_data);

			if (!minfo) {
				relocate_variable (NULL, base_ptr);
				continue;
			}

			if (original > minfo->num_locals) {
				g_warning ("Symbol file %s contains relocation entry for non-existing "
					   "local variable %d, but method %s only has %d local variables.",
					   symfile->file_name, original, minfo->method->name,
					   minfo->num_locals);
				g_message (G_STRLOC ": %d", token);
				G_BREAKPOINT ();
				continue;
			}

			relocate_variable (&minfo->locals [original], base_ptr);

			break;
		}
		case MRT_method_parameter: {
			guint32 token = *((guint32 *) tmp_ptr)++;
			guint32 original = *((guint32 *) tmp_ptr)++;
			MonoDebugMethodInfo *minfo;

			minfo = method_info_func (symfile, token, user_data);

			if (!minfo) {
				relocate_variable (NULL, base_ptr);
				continue;
			}

			if (minfo->method->signature->hasthis) {
				if (original == 0) {
					relocate_variable (minfo->this_var, base_ptr);
					continue;
				}

				original--;
			}

			if (original > minfo->num_params) {
				g_warning ("Symbol file %s contains relocation entry for non-existing "
					   "parameter %d, but method %s only has %d parameters.",
					   symfile->file_name, original, minfo->method->name,
					   minfo->num_params);
				continue;
			}

			relocate_variable (&minfo->params [original], base_ptr);

			break;
		}
		case MRT_type_sizeof: {
			guint32 token = *((guint32 *) tmp_ptr)++;
			MonoClass *klass = mono_debug_class_get (symfile, token);

			if (!klass)
				continue;

			mono_class_init (klass);

			if (klass->enumtype || klass->valuetype)
				* (gint8 *) base_ptr = klass->instance_size - sizeof (MonoObject);
			else
				* (gint8 *) base_ptr = klass->instance_size;

			break;
		}
		case MRT_type_field_offset: {
			guint32 token = *((guint32 *) tmp_ptr)++;
			guint32 original = *((guint32 *) tmp_ptr)++;
			MonoClass *klass = mono_debug_class_get (symfile, token);
			guint32 off;

			if (!klass)
				continue;

			mono_class_init (klass);

			if (original > klass->field.count) {
				g_warning ("Symbol file %s contains invalid field offset entry.",
					   symfile->file_name);
				g_message (G_STRLOC ": %d", token);
				/* G_BREAKPOINT (); */
				continue;
			}

			if (!klass->fields)
				continue;

			off = klass->fields [original].offset;
			if (klass->byval_arg.type == MONO_TYPE_VALUETYPE)
				off -= sizeof (MonoObject);

#if 0
			g_message ("Setting field %d of type %u to offset %d", original,
				   token, off);
#endif

			* (guint32 *) base_ptr = off;

			break;
		}
		case MRT_mono_string_sizeof:
			* (gint8 *) base_ptr = sizeof (MonoString);
			break;

		case MRT_mono_string_offset: {
			guint32 idx = *((guint32 *) tmp_ptr)++;
			MonoString string;
			guint32 off;

			switch (idx) {
			case MRI_string_offset_length:
				off = (guchar *) &string.length - (guchar *) &string;
				break;

			case MRI_string_offset_chars:
				off = (guchar *) &string.chars - (guchar *) &string;
				break;

			default:
				g_warning ("Symbol file %s contains invalid string offset entry",
					   symfile->file_name);
				continue;
			}

			* (guint32 *) base_ptr = off;

			break;
		}
		case MRT_mono_array_sizeof:
			* (gint8 *) base_ptr = sizeof (MonoArray);
			break;

		case MRT_mono_array_offset: {
			guint32 idx = *((guint32 *) tmp_ptr)++;
			MonoArray array;
			guint32 off;

			switch (idx) {
			case MRI_array_offset_bounds:
				off = (guchar *) &array.bounds - (guchar *) &array;
				break;

			case MRI_array_offset_max_length:
				off = (guchar *) &array.max_length - (guchar *) &array;
				break;

			case MRI_array_offset_vector:
				off = (guchar *) &array.vector - (guchar *) &array;
				break;

			default:
				g_warning ("Symbol file %s contains invalid array offset entry",
					   symfile->file_name);
				continue;
			}

			* (guint32 *) base_ptr = off;

			break;
		}

		case MRT_mono_array_bounds_sizeof:
			* (gint8 *) base_ptr = sizeof (MonoArrayBounds);
			break;

		case MRT_mono_array_bounds_offset: {
			guint32 idx = *((guint32 *) tmp_ptr)++;
			MonoArrayBounds bounds;
			guint32 off;

			switch (idx) {
			case MRI_array_bounds_offset_lower:
				off = (guchar *) &bounds.lower_bound - (guchar *) &bounds;
				break;

			case MRI_array_bounds_offset_length:
				off = (guchar *) &bounds.length - (guchar *) &bounds;
				break;

			default:
				g_warning ("Symbol file %s contains invalid array bounds offset entry",
					   symfile->file_name);
				continue;
			}

			* (guint32 *) base_ptr = off;

			break;
		}

		case MRT_variable_start_scope:  {
			guint32 token = *((guint32 *) tmp_ptr)++;
			guint32 original = *((guint32 *) tmp_ptr)++;
			MonoDebugMethodInfo *minfo;
			gint32 address;

			minfo = method_info_func (symfile, token, user_data);

			if (!minfo || !minfo->locals) {
				* (void **) base_ptr = 0;
				continue;
			}

			if (original > minfo->num_locals) {
				g_warning ("Symbol file %s contains relocation entry for non-existing "
					   "local variable %d, but method %s only has %d local variables.",
					   symfile->file_name, original, minfo->method->name,
					   minfo->num_locals);
				continue;
			}

			address = minfo->locals [original].begin_scope;

			* (void **) base_ptr = minfo->code_start + address;

			break;
		}

		case MRT_variable_end_scope:  {
			guint32 token = *((guint32 *) tmp_ptr)++;
			guint32 original = *((guint32 *) tmp_ptr)++;
			MonoDebugMethodInfo *minfo;
			gint32 address;

			minfo = method_info_func (symfile, token, user_data);

			if (!minfo || !minfo->locals) {
				* (void **) base_ptr = 0;
				continue;
			}

			if (original > minfo->num_locals) {
				g_warning ("Symbol file %s contains relocation entry for non-existing "
					   "local variable %d, but method %s only has %d local variables.",
					   symfile->file_name, original, minfo->method->name,
					   minfo->num_locals);
				continue;
			}

			address = minfo->locals [original].end_scope;

			* (void **) base_ptr = minfo->code_start + address;

			break;
		}

		case MRT_mono_string_fieldsize: {
			guint32 idx = *((guint32 *) tmp_ptr)++;
			MonoString string;
			guint32 fieldsize;

			switch (idx) {
			case MRI_string_offset_length:
				fieldsize = sizeof (string.length);
				break;

			default:
				g_warning ("Symbol file %s contains invalid string fieldsize entry",
					   symfile->file_name);
				continue;
			}

			* (guint32 *) base_ptr = fieldsize;

			break;
		}

		case MRT_mono_array_fieldsize: {
			guint32 idx = *((guint32 *) tmp_ptr)++;
			MonoArray array;
			guint32 fieldsize;

			switch (idx) {
			case MRI_array_offset_bounds:
				fieldsize = sizeof (array.bounds);
				break;

			case MRI_array_offset_max_length:
				fieldsize = sizeof (array.max_length);
				break;

			case MRI_array_offset_vector:
				fieldsize = sizeof (array.vector);
				break;

			default:
				g_warning ("Symbol file %s contains invalid array fieldsize entry",
					   symfile->file_name);
				continue;
			}

			* (guint32 *) base_ptr = fieldsize;

			break;
		}


		default:
			g_warning ("Symbol file %s contains unknown relocation entry %d",
				   symfile->file_name, type);
			break;
		}
	}

	mono_raw_buffer_update (symfile->raw_contents, symfile->raw_contents_size);
}

gchar *
mono_debug_find_source_location (MonoDebugSymbolFile *symfile, MonoMethod *method, guint32 offset)
{
	MonoDebugLineNumberBlock *lnb;
	const char *ptr;

	if (!symfile->line_number_table)
		return NULL;

	lnb = g_hash_table_lookup (symfile->line_number_table, method);
	if (!lnb)
		return NULL;

	ptr = symfile->raw_contents +
		symfile->section_offsets [MONO_DEBUG_SYMBOL_SECTION_MONO_LINE_NUMBERS].file_offset;

	ptr += lnb->file_offset;

	do {
		guint32 row, iloffset;

		row = * ((guint32 *) ptr)++;
		iloffset = * ((guint32 *) ptr)++;

		if (!row && !offset)
			return NULL;
		if (!row)
			continue;

		if (iloffset >= offset)
			return g_strdup_printf ("%s:%d", lnb->source_file, row);
	} while (1);

	return NULL;
}
