#include <config.h>
#include <stdlib.h>
#include <string.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/rawbuffer.h>
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

#define MRI_string_offset_length	0x00
#define MRI_string_offset_vector	0x01

#define MRI_array_offset_bounds		0x00
#define MRI_array_offset_max_length	0x01
#define MRI_array_offset_vector		0x02

#define MRI_array_bounds_offset_lower	0x00
#define MRI_array_bounds_offset_length	0x01

#define MRS_debug_info			0x01
#define MRS_debug_abbrev		0x02
#define MRS_debug_line			0x03
#define MRS_mono_reloc_table		0x04

#ifdef HAVE_ELF_H

static gboolean
get_sections_elf32 (MonoDebugSymbolFile *symfile, gboolean emit_warnings)
{
	Elf32_Ehdr *header;
	Elf32_Shdr *section, *strtab_section;
	const char *strtab;
	int i;

	header = symfile->raw_contents;
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

	section = (Elf32_Shdr *)((char *)symfile->raw_contents + header->e_shoff);
	strtab_section = section + header->e_shstrndx;
	strtab = (char *)symfile->raw_contents + strtab_section->sh_offset;

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
		}
	}

	return TRUE;
}

#endif /* HAVE_ELF_H */

static gboolean
get_sections (MonoDebugSymbolFile *symfile, gboolean emit_warnings)
{
#ifdef HAVE_ELF_H
	if (!strncmp (symfile->raw_contents, ELFMAG, strlen (ELFMAG)))
		return get_sections_elf32 (symfile, emit_warnings);
#endif

	if (emit_warnings)
		g_warning ("Symbol file %s has unknown file format", symfile->file_name);

	return FALSE;
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

	g_free (symfile->file_name);
	g_free (symfile->section_offsets);
	g_free (symfile);
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

	reloc_ptr = reloc_start = (char *)symfile->raw_contents +
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
		void *base_ptr;

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

		base_ptr = (char *)symfile->raw_contents + symfile->section_offsets [section].file_offset;
		base_ptr = (char *)base_ptr + offset;

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
			g_message ("Start of `%s' relocated to %p", minfo->method->name, minfo->code_start);
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

			* (void **) base_ptr = (char *)minfo->code_start + address;

			break;
		}
		case MRT_local_variable: {
			guint32 token = *((guint32 *) tmp_ptr)++;
			guint32 original = *((guint32 *) tmp_ptr)++;
			MonoDebugMethodInfo *minfo;
			gint32 address;

			minfo = method_info_func (symfile, token, user_data);

			if (!minfo) {
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

			address = minfo->local_offsets [original];

#if 0
			g_message ("Relocating local variable %d (%s) to stack offset %d",
				   original, minfo->method->name, address);
#endif

			* (gint32 *) base_ptr = address;

			break;
		}
		case MRT_method_parameter: {
			guint32 token = *((guint32 *) tmp_ptr)++;
			guint32 original = *((guint32 *) tmp_ptr)++;
			MonoDebugMethodInfo *minfo;
			gint32 address;

			minfo = method_info_func (symfile, token, user_data);

			if (!minfo) {
				* (void **) base_ptr = 0;
				continue;
			}

			if (minfo->method->signature->hasthis) {
				if (original == 0) {
					* (gint32 *) base_ptr = minfo->this_offset;
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

			address = minfo->param_offsets [original];

#if 0
			g_message ("Relocating parameter %d (%s) to stack offset %d",
				   original, minfo->method->name, address);
#endif

			* (gint32 *) base_ptr = address;

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

			case MRI_string_offset_vector:
				off = (guchar *) &string.c_str - (guchar *) &string;
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

		default:
			g_warning ("Symbol file %s contains unknown relocation entry %d",
				   symfile->file_name, type);
			break;
		}
	}

	mono_raw_buffer_update (symfile->raw_contents, symfile->raw_contents_size);
}

MonoReflectionType *
ves_icall_Debugger_MonoSymbolWriter_get_local_type_from_sig (MonoReflectionAssembly *assembly,
							     MonoArray *signature)
{
	MonoDomain *domain; 
	MonoImage *image;
	MonoType *type;
	const char *ptr;
	int len = 0;

	MONO_CHECK_ARG_NULL (assembly);
	MONO_CHECK_ARG_NULL (signature);

	domain = mono_domain_get();
	image = assembly->assembly->image;

	ptr = mono_array_addr (signature, char, 0);
	g_assert (*ptr++ == 0x07);
	len = mono_metadata_decode_value (ptr, &ptr);
	g_assert (len == 1);

	type = mono_metadata_parse_type (image, MONO_PARSE_LOCAL, 0, ptr, &ptr);

	return mono_type_get_object (domain, type);
}

MonoReflectionMethod *
ves_icall_Debugger_MonoSymbolWriter_method_from_token (MonoReflectionAssembly *assembly, guint32 token)
{
	MonoDomain *domain; 
	MonoImage *image;
	MonoMethod *method;

	MONO_CHECK_ARG_NULL (assembly);

	domain = mono_domain_get();
	image = assembly->assembly->image;

	method = mono_get_method (image, token, NULL);

	return mono_method_get_object (domain, method);
}

guint32
ves_icall_Debugger_DwarfFileWriter_get_type_token (MonoReflectionType *type)
{
	MonoClass *klass = mono_class_from_mono_type (type->type);

	mono_class_init (klass);

	return klass->type_token;
}
