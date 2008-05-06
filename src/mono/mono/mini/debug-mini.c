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
#include "config.h"
#include <mono/metadata/verify.h>
#include <mono/metadata/mono-config.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/threads-types.h>

#define _IN_THE_MONO_DEBUGGER
#include <mono/metadata/mono-debug-debugger.h>
#include "debug-mini.h"

#ifdef HAVE_VALGRIND_H
#include <valgrind/valgrind.h>
#endif

#ifdef MONO_DEBUGGER_SUPPORTED
#include <libgc/include/libgc-mono-debugger.h>
#endif

typedef struct {
	guint32 index;
	MonoMethodDesc *desc;
} MiniDebugBreakpointInfo;

typedef struct
{
	MonoDebugMethodJitInfo *jit;
	GArray *line_numbers;
	guint32 has_line_numbers;
	guint32 breakpoint_id;
} MiniDebugMethodInfo;

struct _MonoDebuggerThreadInfo {
	guint64 tid;
	guint64 lmf_addr;
	guint64 end_stack;

	guint64 extended_notifications;

	/* Next pointer. */
	MonoDebuggerThreadInfo *next;

	/*
	 * The stack bounds are only used when reading a core file.
	 */
	guint64 stack_start;
	guint64 signal_stack_start;
	guint32 stack_size;
	guint32 signal_stack_size;

	/*
	 * The debugger doesn't access anything beyond this point.
	 */
	MonoJitTlsData *jit_tls;
	MonoThread *thread;
};

MonoDebuggerThreadInfo *mono_debugger_thread_table = NULL;

static inline void
record_line_number (MiniDebugMethodInfo *info, guint32 address, guint32 offset)
{
	MonoDebugLineNumberEntry lne;

	lne.native_offset = address;
	lne.il_offset = offset;

	g_array_append_val (info->line_numbers, lne);
}

static void
mono_debug_free_method_jit_info (MonoDebugMethodJitInfo *jit)
{
	g_free (jit->line_numbers);
	g_free (jit->this_var);
	g_free (jit->params);
	g_free (jit->locals);
	g_free (jit);
}

void
mono_debug_init_method (MonoCompile *cfg, MonoBasicBlock *start_block, guint32 breakpoint_id)
{
	MiniDebugMethodInfo *info;

	if (mono_debug_format == MONO_DEBUG_FORMAT_NONE)
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
	info->line_numbers = g_array_new (FALSE, TRUE, sizeof (MonoDebugLineNumberEntry));
	jit->num_locals = header->num_locals;
	jit->locals = g_new0 (MonoDebugVarInfo, jit->num_locals);
}

static void
write_variable (MonoInst *inst, MonoDebugVarInfo *var)
{
	var->type = inst->inst_vtype;

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
	MonoDebugMethodInfo *minfo;
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

	minfo = mono_debug_lookup_method (method);
	if (minfo) {
		/* Create offset->line number mapping */
		for (i = 0; i < header->code_size; ++i) {
			MonoDebugSourceLocation *location;

			location = mono_debug_symfile_lookup_location (minfo, i);
			if (!location)
				continue;

			lines [i] = location.row;
			if (!filename)
				filename = location.source_file;

			mono_debug_free_source_location (location);
		}
	}

	/* Create address->offset mapping */
	for (i = 0; i < jit->num_line_numbers; ++i) {
		MonoDebugLineNumberEntry *lne = jit->line_numbers [i];

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
	MonoMethodSignature *sig;
	MonoDebugMethodAddress *debug_info;
	MonoMethod *method;
	int i;

	info = (MiniDebugMethodInfo *) cfg->debug_info;
	if (!info || !info->jit) {
		if (info)
			g_free (info);
		return;
	}

	method = cfg->method;
	header = mono_method_get_header (method);
	sig = mono_method_signature (method);

	jit = info->jit;
	jit->code_start = cfg->native_code;
	jit->epilogue_begin = cfg->epilog_begin;
	jit->code_size = cfg->code_len;

	record_line_number (info, jit->epilogue_begin, header->code_size);

	jit->num_params = sig->param_count;
	jit->params = g_new0 (MonoDebugVarInfo, jit->num_params);

	for (i = 0; i < jit->num_locals; i++)
		write_variable (cfg->varinfo [cfg->locals_start + i], &jit->locals [i]);

	if (sig->hasthis) {
		jit->this_var = g_new0 (MonoDebugVarInfo, 1);
		write_variable (cfg->varinfo [0], jit->this_var);
	}

	for (i = 0; i < jit->num_params; i++)
		write_variable (cfg->varinfo [i + sig->hasthis], &jit->params [i]);

	jit->num_line_numbers = info->line_numbers->len;
	jit->line_numbers = g_new0 (MonoDebugLineNumberEntry, jit->num_line_numbers);

	for (i = 0; i < jit->num_line_numbers; i++)
		jit->line_numbers [i] = g_array_index (info->line_numbers, MonoDebugLineNumberEntry, i);

	debug_info = mono_debug_add_method (method, jit, cfg->domain);

	mono_debug_add_vg_method (method, jit);

	if (info->breakpoint_id)
		mono_debugger_breakpoint_callback (method, info->breakpoint_id);

	mono_debugger_check_breakpoints (method, debug_info);

	mono_debug_free_method_jit_info (jit);
	g_array_free (info->line_numbers, TRUE);
	g_free (info);
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

	record_line_number (info, address, offset);
}

void
mono_debug_open_block (MonoCompile *cfg, MonoBasicBlock *bb, guint32 address)
{
	MiniDebugMethodInfo *info;
	MonoMethodHeader *header;
	guint32 offset;

	info = (MiniDebugMethodInfo *) cfg->debug_info;
	if (!info || !info->jit || !bb->cil_code)
		return;

	header = mono_method_get_header (cfg->method);
	g_assert (header);

	if ((bb->cil_code < header->code) ||
	    (bb->cil_code > header->code + header->code_size))
		return;

	offset = bb->cil_code - header->code;
	if (!info->has_line_numbers) {
		info->jit->prologue_end = address;
		info->has_line_numbers = TRUE;
	}

	record_line_number (info, address, offset);
}

static inline void
encode_value (gint32 value, guint8 *buf, guint8 **endbuf)
{
	guint8 *p = buf;

	//printf ("ENCODE: %d 0x%x.\n", value, value);

	/* 
	 * Same encoding as the one used in the metadata, extended to handle values
	 * greater than 0x1fffffff.
	 */
	if ((value >= 0) && (value <= 127))
		*p++ = value;
	else if ((value >= 0) && (value <= 16383)) {
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
decode_value (guint8 *ptr, guint8 **rptr)
{
	guint8 b = *ptr;
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
serialize_variable (MonoDebugVarInfo *var, guint8 *p, guint8 **endbuf)
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
mono_debug_serialize_debug_info (MonoCompile *cfg, guint8 **out_buf, guint32 *buf_len)
{
	MiniDebugMethodInfo *info;
	MonoDebugMethodJitInfo *jit;
	guint32 size, prev_offset, prev_native_offset;
	guint8 *buf, *p;
	int i;

	info = (MiniDebugMethodInfo *) cfg->debug_info;
	if (!info || !info->jit) {
		*buf_len = 0;
		return;
	}
	jit = info->jit;

	size = ((jit->num_params + jit->num_locals + 1) * 10) + (jit->num_line_numbers * 10) + 64;
	p = buf = g_malloc (size);
	encode_value (jit->epilogue_begin, p, &p);
	encode_value (jit->prologue_end, p, &p);
	encode_value (jit->code_size, p, &p);

	for (i = 0; i < jit->num_params; ++i)
		serialize_variable (&jit->params [i], p, &p);

	if (mono_method_signature (cfg->method)->hasthis)
		serialize_variable (jit->this_var, p, &p);

	for (i = 0; i < jit->num_locals; i++)
		serialize_variable (&jit->locals [i], p, &p);

	encode_value (jit->num_line_numbers, p, &p);

	prev_offset = 0;
	prev_native_offset = 0;
	for (i = 0; i < jit->num_line_numbers; ++i) {
		/* Sometimes, the offset values are not in increasing order */
		MonoDebugLineNumberEntry *lne = &jit->line_numbers [i];
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
deserialize_variable (MonoDebugVarInfo *var, guint8 *p, guint8 **endbuf)
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
deserialize_debug_info (MonoMethod *method, guint8 *code_start, guint8 *buf, guint32 buf_len)
{
	MonoMethodHeader *header;
	gint32 offset, native_offset, prev_offset, prev_native_offset;
	MonoDebugMethodJitInfo *jit;
	guint8 *p;
	int i;

	header = mono_method_get_header (method);
	g_assert (header);

	jit = g_new0 (MonoDebugMethodJitInfo, 1);
	jit->code_start = code_start;
	jit->num_locals = header->num_locals;
	jit->locals = g_new0 (MonoDebugVarInfo, jit->num_locals);
	jit->num_params = mono_method_signature (method)->param_count;
	jit->params = g_new0 (MonoDebugVarInfo, jit->num_params);

	p = buf;
	jit->epilogue_begin = decode_value (p, &p);
	jit->prologue_end = decode_value (p, &p);
	jit->code_size = decode_value (p, &p);

	for (i = 0; i < jit->num_params; ++i)
		deserialize_variable (&jit->params [i], p, &p);

	if (mono_method_signature (method)->hasthis) {
		jit->this_var = g_new0 (MonoDebugVarInfo, 1);
		deserialize_variable (jit->this_var, p, &p);
	}

	for (i = 0; i < jit->num_locals; i++)
		deserialize_variable (&jit->locals [i], p, &p);

	jit->num_line_numbers = decode_value (p, &p);
	jit->line_numbers = g_new0 (MonoDebugLineNumberEntry, jit->num_line_numbers);

	prev_offset = 0;
	prev_native_offset = 0;
	for (i = 0; i < jit->num_line_numbers; ++i) {
		MonoDebugLineNumberEntry *lne = &jit->line_numbers [i];

		offset = prev_offset + decode_value (p, &p);
		native_offset = prev_native_offset + decode_value (p, &p);

		lne->native_offset = native_offset;
		lne->il_offset = offset;

		prev_offset = offset;
		prev_native_offset = native_offset;
	}

	return jit;
}

void
mono_debug_add_aot_method (MonoDomain *domain, MonoMethod *method, guint8 *code_start, 
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

	jit = deserialize_debug_info (method, code_start, debug_info, debug_info_len);

	mono_debug_add_method (method, jit, domain);

	mono_debug_add_vg_method (method, jit);

	mono_debug_free_method_jit_info (jit);
}

void
mono_debug_add_icall_wrapper (MonoMethod *method, MonoJitICallInfo* callinfo)
{
	if (mono_debug_format == MONO_DEBUG_FORMAT_NONE)
		return;

	// mono_debug_add_wrapper (method, callinfo->wrapper, callinfo->func);
}

static void
print_var_info (MonoDebugVarInfo *info, int idx, const char *name, const char *type)
{
	switch (info->index & MONO_DEBUG_VAR_ADDRESS_MODE_FLAGS) {
	case MONO_DEBUG_VAR_ADDRESS_MODE_REGISTER:
		g_print ("%s %s (%d) in register %s\n", type, name, idx, mono_arch_regname (info->index & (~MONO_DEBUG_VAR_ADDRESS_MODE_FLAGS)));
		break;
	case MONO_DEBUG_VAR_ADDRESS_MODE_REGOFFSET:
		g_print ("%s %s (%d) in memory: base register %s + %d\n", type, name, idx, mono_arch_regname (info->index & (~MONO_DEBUG_VAR_ADDRESS_MODE_FLAGS)), info->offset);
		break;
	case MONO_DEBUG_VAR_ADDRESS_MODE_TWO_REGISTERS:
	default:
		g_assert_not_reached ();
	}
}

/**
 * mono_debug_print_locals:
 *
 * Prints to stdout the information about the local variables in
 * a method (if @only_arguments is false) or about the arguments.
 * The information includes the storage info (where the variable 
 * lives, in a register or in memory).
 * The method is found by looking up what method has been emitted at
 * the instruction address @ip.
 * This is for use inside a debugger.
 */
void
mono_debug_print_vars (gpointer ip, gboolean only_arguments)
{
	MonoDomain *domain = mono_domain_get ();
	MonoJitInfo *ji = mono_jit_info_table_find (domain, ip);
	MonoDebugMethodJitInfo *jit;
	int i;

	if (!ji)
		return;

	jit = mono_debug_find_method (mono_jit_info_get_method (ji), domain);
	if (!jit)
		return;

	if (only_arguments) {
		char **names;
		names = g_new (char *, jit->num_params);
		mono_method_get_param_names (mono_jit_info_get_method (ji), (const char **) names);
		if (jit->this_var)
			print_var_info (jit->this_var, 0, "this", "Arg");
		for (i = 0; i < jit->num_params; ++i) {
			print_var_info (&jit->params [i], i, names [i]? names [i]: "unknown name", "Arg");
		}
		g_free (names);
	} else {
		for (i = 0; i < jit->num_locals; ++i) {
			print_var_info (&jit->locals [i], i, "", "Local");
		}
	}
}

/*
 * The old Debugger breakpoint interface.
 *
 * This interface is used to insert breakpoints on methods which are not yet JITed.
 * The debugging code keeps a list of all such breakpoints and automatically inserts the
 * breakpoint when the method is JITed.
 */

static GPtrArray *breakpoints = NULL;

int
mono_debugger_insert_breakpoint_full (MonoMethodDesc *desc)
{
	static int last_breakpoint_id = 0;
	MiniDebugBreakpointInfo *info;

	info = g_new0 (MiniDebugBreakpointInfo, 1);
	info->desc = desc;
	info->index = ++last_breakpoint_id;

	if (!breakpoints)
		breakpoints = g_ptr_array_new ();

	g_ptr_array_add (breakpoints, info);

	return info->index;
}

int
mono_debugger_remove_breakpoint (int breakpoint_id)
{
	int i;

	if (!breakpoints)
		return 0;

	for (i = 0; i < breakpoints->len; i++) {
		MiniDebugBreakpointInfo *info = g_ptr_array_index (breakpoints, i);

		if (info->index != breakpoint_id)
			continue;

		mono_method_desc_free (info->desc);
		g_ptr_array_remove (breakpoints, info);
		g_free (info);
		return 1;
	}

	return 0;
}

int
mono_debugger_insert_breakpoint (const gchar *method_name, gboolean include_namespace)
{
	MonoMethodDesc *desc;

	desc = mono_method_desc_new (method_name, include_namespace);
	if (!desc)
		return 0;

	return mono_debugger_insert_breakpoint_full (desc);
}

int
mono_debugger_method_has_breakpoint (MonoMethod *method)
{
	int i;

	if (!breakpoints || (method->wrapper_type != MONO_WRAPPER_NONE))
		return 0;

	for (i = 0; i < breakpoints->len; i++) {
		MiniDebugBreakpointInfo *info = g_ptr_array_index (breakpoints, i);

		if (!mono_method_desc_full_match (info->desc, method))
			continue;

		return info->index;
	}

	return 0;
}

void
mono_debugger_breakpoint_callback (MonoMethod *method, guint32 index)
{
	mono_debugger_event (MONO_DEBUGGER_EVENT_JIT_BREAKPOINT, (guint64) (gsize) method, index);
}

void
mono_debugger_thread_created (gsize tid, MonoThread *thread, MonoJitTlsData *jit_tls)
{
#ifdef MONO_DEBUGGER_SUPPORTED
	size_t stsize = 0;
	guint8 *staddr = NULL;
	MonoDebuggerThreadInfo *info;

	if (mono_debug_format == MONO_DEBUG_FORMAT_NONE)
		return;

	mono_debugger_lock ();

	mono_thread_get_stack_bounds (&staddr, &stsize);

	info = g_new0 (MonoDebuggerThreadInfo, 1);
	info->tid = tid;
	info->thread = thread;
	info->stack_start = (guint64) (gsize) staddr;
	info->signal_stack_start = (guint64) (gsize) jit_tls->signal_stack;
	info->stack_size = stsize;
	info->signal_stack_size = jit_tls->signal_stack_size;
	info->end_stack = (guint64) (gsize) GC_mono_debugger_get_stack_ptr ();
	info->lmf_addr = (guint64) (gsize) mono_get_lmf_addr ();
	info->jit_tls = jit_tls;

	info->next = mono_debugger_thread_table;
	mono_debugger_thread_table = info;

	mono_debugger_event (MONO_DEBUGGER_EVENT_THREAD_CREATED,
			     tid, (guint64) (gsize) info);

	mono_debugger_unlock ();
#endif /* MONO_DEBUGGER_SUPPORTED */
}

void
mono_debugger_thread_cleanup (MonoJitTlsData *jit_tls)
{
#ifdef MONO_DEBUGGER_SUPPORTED
	MonoDebuggerThreadInfo **ptr;

	if (mono_debug_format == MONO_DEBUG_FORMAT_NONE)
		return;

	mono_debugger_lock ();

	for (ptr = &mono_debugger_thread_table; *ptr; ptr = &(*ptr)->next) {
		MonoDebuggerThreadInfo *info = *ptr;

		if (info->jit_tls != jit_tls)
			continue;

		mono_debugger_event (MONO_DEBUGGER_EVENT_THREAD_CLEANUP,
				     info->tid, (guint64) (gsize) info);

		*ptr = info->next;
		g_free (info);
		break;
	}

	mono_debugger_unlock ();
#endif
}

void
mono_debugger_extended_notification (MonoDebuggerEvent event, guint64 data, guint64 arg)
{
#ifdef MONO_DEBUGGER_SUPPORTED
	MonoDebuggerThreadInfo **ptr;
	MonoThread *thread = mono_thread_current ();

	if (!mono_debug_using_mono_debugger ())
		return;

	mono_debugger_lock ();

	for (ptr = &mono_debugger_thread_table; *ptr; ptr = &(*ptr)->next) {
		MonoDebuggerThreadInfo *info = *ptr;

		if (info->thread != thread)
			continue;

		if ((info->extended_notifications & (int) event) == 0)
			continue;

		mono_debugger_event (event, data, arg);
	}

	mono_debugger_unlock ();
#endif
}

void
mono_debugger_trampoline_compiled (MonoMethod *method, const guint8 *code)
{
	mono_debugger_extended_notification (MONO_DEBUGGER_EVENT_TRAMPOLINE,
					     (guint64) (gsize) method, (guint64) (gsize) code);
}
