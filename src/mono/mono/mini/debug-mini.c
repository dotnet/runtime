/*
 * debug-mini.c: Mini-specific debugging stuff.
 *
 * Author:
 *   Martin Baulig (martin@ximian.com)
 *
 * (C) 2003 Ximian, Inc.
 */

#include "mini.h"
#include "jit.h"
#include <mono/metadata/verify.h>
#include <mono/metadata/mono-config.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/appdomain.h>
/* mono-debug-debugger.h needs config.h to work... */
#include "config.h"
#include <mono/metadata/mono-debug-debugger.h>

#ifdef HAVE_VALGRIND_H
#include <valgrind/valgrind.h>
#endif

static inline void
record_line_number (MonoDebugMethodJitInfo *jit, guint32 address, guint32 offset)
{
	MonoDebugLineNumberEntry lne;

	lne.native_offset = address;
	lne.il_offset = offset;

	g_array_append_val (jit->line_numbers, lne);
}

typedef struct
{
	MonoDebugMethodJitInfo *jit;
	guint32 has_line_numbers;
	guint32 breakpoint_id;
} MiniDebugMethodInfo;

void
mono_debug_init_method (MonoCompile *cfg, MonoBasicBlock *start_block, guint32 breakpoint_id)
{
	MonoMethod *method = cfg->method;
	MiniDebugMethodInfo *info;

	if (mono_debug_format == MONO_DEBUG_FORMAT_NONE)
		return;

	if ((method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) ||
	    (method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) ||
	    (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) ||
	    (method->flags & METHOD_ATTRIBUTE_ABSTRACT))
		return;
	if ((method->wrapper_type != MONO_WRAPPER_NONE) &&
	    (method->wrapper_type != MONO_WRAPPER_MANAGED_TO_NATIVE))
		return;

	info = g_new0 (MiniDebugMethodInfo, 1);
	info->breakpoint_id = breakpoint_id;

	cfg->debug_info = info;
}

void
mono_debug_open_method (MonoCompile *cfg)
{
	MiniDebugMethodInfo *info;
	MonoDebugMethodJitInfo *jit;
	MonoMethodHeader *header;

	info = (MiniDebugMethodInfo *) cfg->debug_info;
	if (!info)
		return;

	mono_class_init (cfg->method->klass);

	header = mono_method_get_header (cfg->method);
	g_assert (header);
	
	info->jit = jit = g_new0 (MonoDebugMethodJitInfo, 1);
	jit->line_numbers = g_array_new (FALSE, TRUE, sizeof (MonoDebugLineNumberEntry));
	jit->num_locals = header->num_locals;
	jit->locals = g_new0 (MonoDebugVarInfo, jit->num_locals);
}

static void
write_variable (MonoInst *inst, MonoDebugVarInfo *var)
{
	if (inst->opcode == OP_REGVAR)
		var->index = inst->dreg | MONO_DEBUG_VAR_ADDRESS_MODE_REGISTER;
	else {
		/* the debug interface needs fixing to allow 0(%base) address */
		var->index = inst->inst_basereg | MONO_DEBUG_VAR_ADDRESS_MODE_REGOFFSET;
		var->offset = inst->inst_offset;
	}
}

/*
 * mono_debug_add_vg_method:
 *
 *  Register symbol information for the method with valgrind
 */
static void 
mono_debug_add_vg_method (MonoMethod *method, MonoDebugMethodJitInfo *jit)
{
#ifdef VALGRIND_ADD_LINE_INFO
	MonoMethodHeader *header;
	int i;
	char *filename = NULL;
	guint32 address, line_number;
	const char *full_name;
	guint32 *addresses;
	guint32 *lines;

	if (!RUNNING_ON_VALGRIND)
		return;

	header = mono_method_get_header (method);

	full_name = mono_method_full_name (method, TRUE);

	addresses = g_new0 (guint32, header->code_size + 1);
	lines = g_new0 (guint32, header->code_size + 1);

	/* 
	 * Very simple code to convert the addr->offset mappings that mono has
	 * into [addr-addr] ->line number mappings.
	 */

	/* Create offset->line number mapping */
	for (i = 0; i < header->code_size; ++i) {
		char *fname;

		fname = mono_debug_source_location_from_il_offset (method, i, &lines [i]);
		if (!filename)
			filename = fname;
	}

	/* Create address->offset mapping */
	for (i = 0; i < jit->line_numbers->len; ++i) {
		MonoDebugLineNumberEntry *lne = &g_array_index (jit->line_numbers, MonoDebugLineNumberEntry, i);

		g_assert (lne->offset <= header->code_size);

		if ((addresses [lne->offset] == 0) || (lne->address < addresses [lne->offset]))
			addresses [lne->offset] = lne->address;
	}
	/* Fill out missing addresses */
	address = 0;
	for (i = 0; i < header->code_size; ++i) {
		if (addresses [i] == 0)
			addresses [i] = address;
		else
			address = addresses [i];
	}
	
	address = 0;
	line_number = 0;
	i = 0;
	while (i < header->code_size) {
		if (lines [i] == line_number)
			i ++;
		else {
			if (line_number > 0) {
				//g_assert (addresses [i] - 1 >= address);
				
				if (addresses [i] - 1 >= address) {
					VALGRIND_ADD_LINE_INFO (jit->code_start + address, jit->code_start + addresses [i] - 1, filename, line_number);
					//printf ("[%d-%d] -> %d.\n", address, addresses [i] - 1, line_number);
				}
			}
			address = addresses [i];
			line_number = lines [i];
		}
	}

	if (line_number > 0) {
		VALGRIND_ADD_LINE_INFO (jit->code_start + address, jit->code_start + jit->code_size - 1, filename, line_number);
		//printf ("[%d-%d] -> %d.\n", address, jit->code_size - 1, line_number);
	}

	VALGRIND_ADD_SYMBOL (jit->code_start, jit->code_size, full_name);

	g_free (addresses);
	g_free (lines);
#endif /* VALGRIND_ADD_LINE_INFO */
}

void
mono_debug_close_method (MonoCompile *cfg)
{
	MiniDebugMethodInfo *info;
	MonoDebugMethodJitInfo *jit;
	MonoMethodHeader *header;
	MonoMethod *method;
	int i;

	info = (MiniDebugMethodInfo *) cfg->debug_info;
	if (!info || !info->jit)
		return;

	method = cfg->method;
	header = mono_method_get_header (method);

	jit = info->jit;
	jit->code_start = cfg->native_code;
	jit->epilogue_begin = cfg->epilog_begin;
	jit->code_size = cfg->code_len;

	record_line_number (jit, jit->epilogue_begin, header->code_size);

	jit->num_params = method->signature->param_count;
	jit->params = g_new0 (MonoDebugVarInfo, jit->num_params);

	for (i = 0; i < jit->num_locals; i++)
		write_variable (cfg->varinfo [cfg->locals_start + i], &jit->locals [i]);

	if (method->signature->hasthis) {
		jit->this_var = g_new0 (MonoDebugVarInfo, 1);
		write_variable (cfg->varinfo [0], jit->this_var);
	}

	for (i = 0; i < jit->num_params; i++)
		write_variable (cfg->varinfo [i + method->signature->hasthis], &jit->params [i]);

	mono_debug_add_method (method, jit, cfg->domain);

	mono_debug_add_vg_method (method, jit);

	if (info->breakpoint_id)
		mono_debugger_breakpoint_callback (method, info->breakpoint_id);
}

void
mono_debug_record_line_number (MonoCompile *cfg, MonoInst *ins, guint32 address)
{
	MiniDebugMethodInfo *info;
	MonoMethodHeader *header;
	guint32 offset;

	info = (MiniDebugMethodInfo *) cfg->debug_info;
	if (!info || !info->jit || !ins->cil_code)
		return;

	header = mono_method_get_header (cfg->method);
	g_assert (header);

	if ((ins->cil_code < header->code) ||
	    (ins->cil_code > header->code + header->code_size))
		return;

	offset = ins->cil_code - header->code;
	if (!info->has_line_numbers) {
		info->jit->prologue_end = address;
		info->has_line_numbers = TRUE;
	}

	record_line_number (info->jit, address, offset);
}

static inline void
encode_value (gint32 value, char *buf, char **endbuf)
{
	char *p = buf;

	//printf ("ENCODE: %d 0x%x.\n", value, value);

	/* 
	 * Same encoding as the one used in the metadata, extended to handle values
	 * greater than 0x1fffffff.
	 */
	if ((value >= 0) && (value <= 127))
		*p++ = value;
	else if ((value >= 0) && (value <= 16384)) {
		p [0] = 0x80 | (value >> 8);
		p [1] = value & 0xff;
		p += 2;
	} else if ((value >= 0) && (value <= 0x1fffffff)) {
		p [0] = (value >> 24) | 0xc0;
		p [1] = (value >> 16) & 0xff;
		p [2] = (value >> 8) & 0xff;
		p [3] = value & 0xff;
		p += 4;
	}
	else {
		p [0] = 0xff;
		p [1] = (value >> 24) & 0xff;
		p [2] = (value >> 16) & 0xff;
		p [3] = (value >> 8) & 0xff;
		p [4] = value & 0xff;
		p += 5;
	}
	if (endbuf)
		*endbuf = p;
}

static inline gint32
decode_value (char *_ptr, char **rptr)
{
	unsigned char *ptr = (unsigned char *) _ptr;
	unsigned char b = *ptr;
	gint32 len;
	
	if ((b & 0x80) == 0){
		len = b;
		++ptr;
	} else if ((b & 0x40) == 0){
		len = ((b & 0x3f) << 8 | ptr [1]);
		ptr += 2;
	} else if (b != 0xff) {
		len = ((b & 0x1f) << 24) |
			(ptr [1] << 16) |
			(ptr [2] << 8) |
			ptr [3];
		ptr += 4;
	}
	else {
		len = (ptr [1] << 24) | (ptr [2] << 16) | (ptr [3] << 8) | ptr [4];
		ptr += 5;
	}
	if (rptr)
		*rptr = ptr;

	//printf ("DECODE: %d.\n", len);
	return len;
}

static void
serialize_variable (MonoDebugVarInfo *var, char *p, char **endbuf)
{
	guint32 flags = var->index & MONO_DEBUG_VAR_ADDRESS_MODE_FLAGS;

	switch (flags) {
	case MONO_DEBUG_VAR_ADDRESS_MODE_REGISTER:
		encode_value (var->index, p, &p);
		break;
	case MONO_DEBUG_VAR_ADDRESS_MODE_REGOFFSET:
		encode_value (var->index, p, &p);
		encode_value (var->offset, p, &p);
		break;
	default:
		g_assert_not_reached ();
	}
	*endbuf = p;
}

void
mono_debug_serialize_debug_info (MonoCompile *cfg, 
								 guint8 **out_buf, guint32 *buf_len)
{
	MiniDebugMethodInfo *info;
	MonoDebugMethodJitInfo *jit;
	guint32 size, prev_offset, prev_native_offset;
	char *buf;
	char *p;
	int i;

	info = (MiniDebugMethodInfo *) cfg->debug_info;
	if (!info || !info->jit) {
		*buf_len = 0;
		return;
	}
	jit = info->jit;

	size = ((jit->num_params + jit->num_locals + 1) * 10) + (jit->line_numbers->len * 10) + 64;
	p = buf = g_malloc (size);
	encode_value (jit->epilogue_begin, p, &p);
    encode_value (jit->prologue_end, p, &p);
	encode_value (jit->code_size, p, &p);

	for (i = 0; i < jit->num_params; ++i)
		serialize_variable (&jit->params [i], p, &p);

	if (cfg->method->signature->hasthis)
		serialize_variable (jit->this_var, p, &p);

	for (i = 0; i < jit->num_locals; i++)
		serialize_variable (&jit->locals [i], p, &p);

	encode_value (jit->line_numbers->len, p, &p);

	prev_offset = 0;
	prev_native_offset = 0;
	for (i = 0; i < jit->line_numbers->len; ++i) {
		/* Sometimes, the offset values are not in increasing order */
		MonoDebugLineNumberEntry *lne = &g_array_index (jit->line_numbers, 
														MonoDebugLineNumberEntry,
														i);
		encode_value (lne->il_offset - prev_offset, p, &p);
		encode_value (lne->native_offset - prev_native_offset, p, &p);
		prev_offset = lne->il_offset;
		prev_native_offset = lne->native_offset;
	}

	g_assert (p - buf < size);

	*out_buf = buf;
	*buf_len = p - buf;
}

static void
deserialize_variable (MonoDebugVarInfo *var, char *p, char **endbuf)
{
	guint32 flags;

	var->index = decode_value (p, &p);

	flags = var->index & MONO_DEBUG_VAR_ADDRESS_MODE_FLAGS;

	switch (flags) {
	case MONO_DEBUG_VAR_ADDRESS_MODE_REGISTER:
		break;
	case MONO_DEBUG_VAR_ADDRESS_MODE_REGOFFSET:
		var->offset = decode_value (p, &p);
		break;
	default:
		g_assert_not_reached ();
	}
	*endbuf = p;
}

static MonoDebugMethodJitInfo *
deserialize_debug_info (MonoMethod *method,
						guint8 *code_start, 
						guint8 *buf, guint32 buf_len)
{
	MonoMethodHeader *header;
	MonoDebugMethodJitInfo *jit;
	gint32 offset, native_offset, prev_offset, prev_native_offset, len;
	char *p;
	int i;

	header = mono_method_get_header (method);
	g_assert (header);

	jit = g_new0 (MonoDebugMethodJitInfo, 1);
	jit->code_start = code_start;
	jit->line_numbers = g_array_new (FALSE, TRUE, sizeof (MonoDebugLineNumberEntry));
	jit->num_locals = header->num_locals;
	jit->locals = g_new0 (MonoDebugVarInfo, jit->num_locals);
	jit->num_params = method->signature->param_count;
	jit->params = g_new0 (MonoDebugVarInfo, jit->num_params);

	p = buf;
	jit->epilogue_begin = decode_value (p, &p);
	jit->prologue_end = decode_value (p, &p);
	jit->code_size = decode_value (p, &p);

	for (i = 0; i < jit->num_params; ++i)
		deserialize_variable (&jit->params [i], p, &p);

	if (method->signature->hasthis) {
		jit->this_var = g_new0 (MonoDebugVarInfo, 1);
		deserialize_variable (jit->this_var, p, &p);
	}

	for (i = 0; i < jit->num_locals; i++)
		deserialize_variable (&jit->locals [i], p, &p);

	len = decode_value (p, &p);

	prev_offset = 0;
	prev_native_offset = 0;
	for (i = 0; i < len; ++i) {
		offset = prev_offset + decode_value (p, &p);
		native_offset = prev_native_offset + decode_value (p, &p);
		record_line_number (jit, native_offset, offset);
		prev_offset = offset;
		prev_native_offset = native_offset;
	}

	return jit;
}

void
mono_debug_add_aot_method (MonoDomain *domain,
						   MonoMethod *method, guint8 *code_start, 
						   guint8 *debug_info, guint32 debug_info_len)
{
	MonoDebugMethodJitInfo *jit;

	if (mono_debug_format == MONO_DEBUG_FORMAT_NONE)
		return;

	if ((method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) ||
	    (method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) ||
	    (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) ||
	    (method->flags & METHOD_ATTRIBUTE_ABSTRACT) ||
	    (method->wrapper_type != MONO_WRAPPER_NONE))
		return;

	if (debug_info_len == 0)
		return;

	jit = deserialize_debug_info (method, code_start,
								  debug_info,
								  debug_info_len);

	mono_debug_add_method (method, jit, domain);

	mono_debug_add_vg_method (method, jit);
}

MonoDomain *
mono_init_debugger (const char *file, const char *opt_flags)
{
	MonoDomain *domain;
	const guchar *error;
	int opt;

	g_set_prgname (file);

	opt = mono_parse_default_optimizations (opt_flags);
	opt |= MONO_OPT_SHARED;

	mono_set_defaults (0, opt);

	domain = mono_jit_init (file);

	mono_config_parse (NULL);

	error = mono_check_corlib_version ();
	if (error) {
		fprintf (stderr, "Corlib not in sync with this runtime: %s\n", error);
		fprintf (stderr, "Download a newer corlib or a newer runtime at http://www.go-mono.com/daily.\n");
		exit (1);
	}

	return domain;
}

void
mono_debug_add_icall_wrapper (MonoMethod *method, MonoJitICallInfo* callinfo)
{
	if (mono_debug_format == MONO_DEBUG_FORMAT_NONE)
		return;

	mono_debug_add_wrapper (method, callinfo->func, mono_get_root_domain ());
}
