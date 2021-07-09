/**
 * \file
 * Copyright (C) 2016 Xamarin Inc
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

/* inspired by BinaryGraphPrinter.java of Graal */

#include "mini.h"

#if !defined(DISABLE_LOGGING) && !defined(DISABLE_JIT) && !defined(HOST_WIN32)

#include <glib.h>
#include <mono/metadata/class-internals.h>

#include <sys/socket.h>
#include <sys/types.h>
#include <netinet/in.h>
#include <netdb.h>
#include <string.h>
#include <stdlib.h>
#include <unistd.h>
#include <errno.h>
#include <arpa/inet.h>

#if 0
#define CFG_DEBUG
#endif

#ifdef CFG_DEBUG
#define cfg_debug(...) g_debug(__VA_ARGS__)
#else
#define cfg_debug(...) do {} while (0)
#endif

static ConstantPoolEntry*
create_cp_entry (MonoCompile *cfg, void *data, pool_type pt)
{
	ConstantPoolEntry *entry = (ConstantPoolEntry *) mono_mempool_alloc0 (cfg->mempool, sizeof (ConstantPoolEntry));
	entry->pt = pt;
	entry->data = data;
	return entry;
}

static void write_pool (MonoCompile *cfg, ConstantPoolEntry *entry);

static int
create_socket (const char *hostname, const int port)
{
    int sockfd = 0;
    struct sockaddr_in serv_addr;

    if ((sockfd = socket (AF_INET, SOCK_STREAM, 0)) < 0) {
		g_warning ("cfg_dump: could not create socket");
        return -1;
    }

    serv_addr.sin_family = AF_INET;
    serv_addr.sin_port = htons (port);
    serv_addr.sin_addr.s_addr = inet_addr (hostname);

    if (connect (sockfd, (struct sockaddr *)&serv_addr, sizeof(serv_addr)) < 0) {
        g_warning ("cfg_dump: Connect Failed: %s", strerror (errno));
        return -2;
    }

	return sockfd;
}

static void
write_byte (MonoCompile *cfg, unsigned char b)
{
	write (cfg->gdump_ctx->fd, &b, 1);
}

static void
write_short (MonoCompile *cfg, short s)
{
	short swap = htons (s);
	write (cfg->gdump_ctx->fd, &swap, 2);
}

static void
write_int (MonoCompile *cfg, int v)
{
	int swap = htonl (v);
	write (cfg->gdump_ctx->fd, &swap, 4);
}

static void
write_string (MonoCompile *cfg, const char *str)
{
	const size_t len = g_strnlen (str, 0x2000);
	write_int (cfg, (int) len);

	gunichar2 *u = u8to16 (str);
	for (int i = 0; i < len; i++)
		write_short (cfg, u[i]);
}

static void
add_pool_entry (MonoCompile *cfg, ConstantPoolEntry *entry)
{
	int *cp_id= (int *) mono_mempool_alloc0 (cfg->mempool, sizeof (int));
	*cp_id = cfg->gdump_ctx->next_cp_id;
	g_hash_table_insert (cfg->gdump_ctx->constant_pool, entry, cp_id);
	write_byte (cfg, POOL_NEW);
	write_short (cfg, cfg->gdump_ctx->next_cp_id++);
	switch (entry->pt) {
		case PT_STRING:
			write_byte (cfg, POOL_STRING);
			write_string (cfg, (char *) entry->data);
			break;
		case PT_METHOD: {
			MonoMethod *method = (MonoMethod *) entry->data;
			write_byte (cfg, POOL_METHOD);
			write_pool (cfg, create_cp_entry (cfg, (void *) method->klass, PT_KLASS));
			write_pool (cfg, create_cp_entry (cfg, (void *) method->name, PT_STRING));
			write_pool (cfg, create_cp_entry (cfg, (void *) method->signature, PT_SIGNATURE));
			write_int (cfg, (int) method->flags);
			write_int (cfg, -1); // don't transmit bytecode.
			break;
		}
		case PT_KLASS: {
			MonoClass *klass = (MonoClass *) entry->data;
			write_byte (cfg, POOL_KLASS);
			write_string (cfg, m_class_get_name (klass));
			write_byte (cfg, KLASS);
			break;
		}
		case PT_SIGNATURE: {
			write_byte (cfg, POOL_SIGNATURE);
			MonoMethodSignature *sig = (MonoMethodSignature *) entry->data;
			write_short (cfg, sig->param_count);
			for (int i = 0; i < sig->param_count; i++) {
				GString *sbuf = g_string_new (NULL);
				mono_type_get_desc (sbuf, sig->params [i], TRUE);
				write_pool (cfg, create_cp_entry (cfg, (void *) sbuf->str, PT_STRING));
				g_string_free (sbuf, TRUE);
			}
			GString *sbuf = g_string_new (NULL);
			mono_type_get_desc (sbuf, sig->ret, TRUE);
			write_pool (cfg, create_cp_entry (cfg, (void *) sbuf->str, PT_STRING));
			g_string_free (sbuf, TRUE);
			break;
		}
		case PT_OPTYPE: {
			MonoInst *insn = (MonoInst *) entry->data;
			write_byte (cfg, POOL_NODE_CLASS);

			write_string (cfg, mono_inst_name (insn->opcode));
			GString *insndesc = mono_print_ins_index_strbuf (-1, insn);
			const int len = g_strnlen (insndesc->str, 0x2000);
#define CUTOFF 40
			if (len > CUTOFF) {
				insndesc->str[CUTOFF] = '\0';
				insndesc->str[CUTOFF - 1] = '.';
				insndesc->str[CUTOFF - 2] = '.';
			}
			write_string (cfg, insndesc->str);
			if (len > CUTOFF)
				insndesc->str[CUTOFF] = ' ';
			g_string_free (insndesc, TRUE);

			// one predecessor
			write_short (cfg, 1);
			write_byte (cfg, 0);
			write_pool (cfg, create_cp_entry (cfg, (void *) "predecessor", PT_STRING));
			write_pool (cfg, create_cp_entry (cfg, (void *) NULL, PT_INPUTTYPE));

			// make NUM_SUCCESSOR successor edges, not everyone will be used.
#define NUM_SUCCESSOR 5
			write_short (cfg, NUM_SUCCESSOR);
			for (int i = 0; i < NUM_SUCCESSOR; i++) {
				char *str = g_strdup ("successor1");
				str[9] = '0' + i;
				write_byte (cfg, 0);
				write_pool (cfg, create_cp_entry (cfg, (void *) str, PT_STRING));
			}

			break;
		}
		case PT_INPUTTYPE: {
			write_byte (cfg, POOL_ENUM);
			write_pool (cfg, create_cp_entry (cfg, (void *) NULL, PT_ENUMKLASS));
			write_int (cfg, 0);
			break;
		}
		case PT_ENUMKLASS: {
			write_byte (cfg, POOL_KLASS);
			write_string (cfg, "InputType");
			write_byte (cfg, ENUM_KLASS);
			write_int (cfg, 1);
			write_pool (cfg, create_cp_entry (cfg, (void *) "fixed", PT_STRING));
			break;
		}
	}
}

static void
write_pool (MonoCompile *cfg, ConstantPoolEntry *entry)
{
	if (!entry || !entry->data) {
		write_byte (cfg, POOL_NULL);
		return;
	}

	short *cp_index = (short *) g_hash_table_lookup (cfg->gdump_ctx->constant_pool, entry);
	if (cp_index == NULL)
		add_pool_entry (cfg, entry);
	else {
		switch (entry->pt) {
			case PT_STRING: write_byte (cfg, POOL_STRING); break;
			case PT_METHOD: write_byte (cfg, POOL_METHOD); break;
			case PT_ENUMKLASS: write_byte (cfg, POOL_KLASS); break;
			case PT_KLASS: write_byte (cfg, POOL_KLASS); break;
			case PT_SIGNATURE: write_byte (cfg, POOL_SIGNATURE); break;
			case PT_OPTYPE: write_byte (cfg, POOL_NODE_CLASS); break;
			case PT_INPUTTYPE: write_byte (cfg, POOL_ENUM); break;
		}
		write_short (cfg, *cp_index);
	}
}

void
mono_cfg_dump_begin_group (MonoCompile *cfg)
{
	if (cfg->gdump_ctx == NULL)
		return;
	write_byte (cfg, BEGIN_GROUP);
	char *title = (char *) mono_mempool_alloc0 (cfg->mempool, 0x2000);
	sprintf (title, "%s::%s", m_class_get_name (cfg->method->klass), cfg->method->name);
	write_pool (cfg, create_cp_entry (cfg, (void *) title, PT_STRING));
	write_pool (cfg, create_cp_entry (cfg, (void *) cfg->method->name, PT_STRING));
	write_pool (cfg, create_cp_entry (cfg, (void *) cfg->method, PT_METHOD));
	write_int (cfg, 0); // TODO: real bytecode index.
}

void
mono_cfg_dump_close_group (MonoCompile *cfg)
{
	if (cfg->gdump_ctx == NULL)
		return;
	write_byte (cfg, CLOSE_GROUP);
	cfg->gdump_ctx = NULL;
}

static int
label_instructions (MonoCompile *cfg)
{
	MonoBasicBlock *bb;
	int instruction_count = 0;

	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		cfg_debug ("bb: %d (in: %d, out: %d)", bb->block_num, bb->in_count, bb->out_count);
		MonoInst *insn;
		for (insn = bb->code; insn; insn = insn->next) {
			instruction_count++;
			void *id = g_hash_table_lookup (cfg->gdump_ctx->insn2id, insn);
			if (id != NULL) // already in the table.
				continue;
			int *new_id = (int *) mono_mempool_alloc0 (cfg->mempool, sizeof (int));
			*new_id = cfg->gdump_ctx->next_insn_id++;
			g_hash_table_insert (cfg->gdump_ctx->insn2id, insn, new_id);
#ifdef CFG_DEBUG
			GString *insndesc = mono_print_ins_index_strbuf (-1, insn);
			cfg_debug ("> insn%002d: %s", *new_id, insndesc->str);
			g_string_free (insndesc, TRUE);
#endif
		}
	}
	return instruction_count;
}

static void
write_instructions (MonoCompile *cfg, int instruction_count)
{
	MonoBasicBlock *bb;
	write_int (cfg, instruction_count);
	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		MonoInst *insn;
		cfg_debug ("== bb: %d (in: %d, out: %d) ==", bb->block_num, bb->in_count, bb->out_count);
		for (insn = bb->code; insn; insn = insn->next) {
			int i;
			int *id = (int *) g_hash_table_lookup (cfg->gdump_ctx->insn2id, insn);
			g_assert (id);
			write_int (cfg, *id);

			// hardcoded node class: only one input and NUM_SUCCESSOR successors
			write_pool (cfg, create_cp_entry (cfg, (void *) insn, PT_OPTYPE));
			write_byte (cfg, cfg->bb_entry->code != insn);

			// properties
			write_short (cfg, 2);

			// property #1
			GString *insndesc = mono_print_ins_index_strbuf (-1, insn);
			cfg_debug ("dumping node [%2d]: %s", *id, insndesc->str);
			write_pool (cfg, create_cp_entry (cfg, (void *) "fullname", PT_STRING));
			write_byte (cfg, PROPERTY_POOL);
			write_pool (cfg, create_cp_entry (cfg, (void *) insndesc->str, PT_STRING));
			g_string_free (insndesc, TRUE);

			// property #2
			write_pool (cfg, create_cp_entry (cfg, (void *) "category", PT_STRING));
			write_byte (cfg, PROPERTY_POOL);
			if (bb->in_count > 1 && bb->code == insn)
				write_pool (cfg, create_cp_entry (cfg, (void *) "merge", PT_STRING));
			else if (bb->code == insn)
				write_pool (cfg, create_cp_entry (cfg, (void *) "begin", PT_STRING));
			else if (MONO_IS_COND_BRANCH_OP (insn))
				write_pool (cfg, create_cp_entry (cfg, (void *) "controlSplit", PT_STRING));
			else if (MONO_IS_PHI (insn))
				write_pool (cfg, create_cp_entry (cfg, (void *) "phi", PT_STRING));
			else if (!MONO_INS_HAS_NO_SIDE_EFFECT (insn))
				write_pool (cfg, create_cp_entry (cfg, (void *) "state", PT_STRING));
			else
				write_pool (cfg, create_cp_entry (cfg, (void *) "fixed", PT_STRING));
			// end of properties
			write_int (cfg, -1); // never set predecessor.

			int *next_id;
			if (insn->next != NULL) {
				next_id = (int *) g_hash_table_lookup (cfg->gdump_ctx->insn2id, insn->next);
				g_assert (next_id);
				cfg_debug ("\tsuccessor' : [%2d]", *next_id);
				write_int (cfg, *next_id);
				for (i = 1; i < NUM_SUCCESSOR; i++)
					write_int (cfg, -1);
			} else {
				g_assert (bb->out_count < NUM_SUCCESSOR);
				for (i = 0; (i < bb->out_count) && (i < NUM_SUCCESSOR); i++) {
					if (bb->out_bb[i]->code == NULL)
						write_int (cfg, -1);
					else {
						next_id = (int *) g_hash_table_lookup (cfg->gdump_ctx->insn2id, bb->out_bb[i]->code);
						if (next_id)
							cfg_debug ("\tsuccessor'': [%2d]", *next_id);
						write_int (cfg, next_id ? *next_id : -1);
					}
				}
				for (; i < NUM_SUCCESSOR; i++)
					write_int (cfg, -1);
			}
		}
	}
}

static void
write_blocks (MonoCompile *cfg)
{
	int block_size = 0;
	MonoBasicBlock *bb;
	for (bb = cfg->bb_entry; bb; bb = bb->next_bb)
		block_size++;
	write_int (cfg, block_size);

	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		int insn_size = 0;
		MonoInst *insn = NULL;

		write_int (cfg, bb->block_num);

		for (insn = bb->code; insn; insn = insn->next)
			insn_size++;
		write_int (cfg, insn_size);

		for (insn = bb->code; insn; insn = insn->next) {
			int *id = (int *) g_hash_table_lookup (cfg->gdump_ctx->insn2id, insn);
			g_assert (id);
			write_int (cfg, *id);
		}

		write_int (cfg, bb->out_count);
		for (int i = 0; i < bb->out_count; i++)
			write_int (cfg, bb->out_bb[i]->block_num);
	}
}

static guint
instruction_hash (MonoInst *insn)
{
	guint res = 0;
	res  = insn->opcode << 0x00;
	res ^= insn->type   << 0x04;
	res ^= insn->flags  << 0x08;
	res ^= insn->dreg   << 0x0c;
	res ^= insn->sreg1  << 0x10;
	res ^= insn->sreg2  << 0x14;
	res ^= insn->sreg3  << 0x18;
	res ^= (gsize) insn->next;
	res ^= (gsize) insn->prev;
	res ^= (gsize) insn;
	return res;
}

static gboolean
instruction_equal (gconstpointer v1, gconstpointer v2)
{
	MonoInst *i1 = (MonoInst *) v1;
	MonoInst *i2 = (MonoInst *) v2;

	if (i1->opcode != i2->opcode || i1->type != i2->type || i1->flags != i2->flags)
		return FALSE;
	if (i1->dreg != i2->dreg || i1->sreg1 != i2->sreg1 || i1->sreg2 != i2->sreg2 || i1->sreg3 != i2->sreg3)
		return FALSE;
	if (i1->next != i2->next || i1->prev != i2->prev)
		return FALSE;
	return TRUE;
}

static guint
constant_pool_hash (ConstantPoolEntry *entry)
{
	switch (entry->pt) {
		case PT_STRING:
			return g_str_hash (entry->data);
		case PT_METHOD: {
			MonoMethod *method = (MonoMethod *) entry->data;
			return g_str_hash (method->name) ^ g_str_hash (method->klass);
		}
		case PT_KLASS:
			return g_str_hash (m_class_get_name ((MonoClass *) entry->data));
		case PT_OPTYPE:
			return instruction_hash ((MonoInst *) entry->data);
		case PT_SIGNATURE: {
			MonoMethodSignature *sig = (MonoMethodSignature *) entry->data;
			guint ret = GPOINTER_TO_UINT (sig->ret);
			for (int i = 0; i < sig->param_count; i++) {
				ret ^= GPOINTER_TO_UINT (sig->params [i]) << (i + 1);
			}
			return ret;
		}
		case PT_INPUTTYPE: // TODO: singleton.
		case PT_ENUMKLASS:
			return GPOINTER_TO_UINT (entry->data);
	}
	g_assert (FALSE);
	return FALSE;
}

static gboolean
constant_pool_equal (gconstpointer v1, gconstpointer v2)
{
	ConstantPoolEntry *e1 = (ConstantPoolEntry *) v1;
	ConstantPoolEntry *e2 = (ConstantPoolEntry *) v2;
	if (e1->pt != e2->pt)
		return FALSE;

	switch (e1->pt) {
		case PT_STRING:
			return g_str_equal (e1->data, e2->data);
		case PT_OPTYPE:
			return instruction_equal (e1->data, e2->data);
		case PT_METHOD: // TODO: implement proper equal.
		case PT_KLASS:
		case PT_SIGNATURE:
			return constant_pool_hash (e1) == constant_pool_hash (e2);
		case PT_INPUTTYPE: // TODO: singleton.
		case PT_ENUMKLASS:
			return TRUE;
	}
	g_assert (FALSE);
	return FALSE;
}


static gboolean cfg_dump_method_inited = FALSE;
static const char *cfg_dump_method_name;

void mono_cfg_dump_create_context (MonoCompile *cfg)
{
	cfg->gdump_ctx = NULL;

	if (!cfg_dump_method_inited) {
		cfg_dump_method_name = g_getenv ("MONO_JIT_DUMP_METHOD");
		cfg_dump_method_inited = TRUE;
	}
	if (!cfg_dump_method_name)
		return;
	const char *name = cfg_dump_method_name;

	if ((strchr (name, '.') > name) || strchr (name, ':')) {
		MonoMethodDesc *desc = mono_method_desc_new (name, TRUE);
		gboolean failed = !mono_method_desc_full_match (desc, cfg->method);
		mono_method_desc_free (desc);
		if (failed)
			return;
	} else
		if (strcmp (cfg->method->name, name) != 0)
			return;

	g_debug ("cfg_dump: create context for \"%s::%s\"", m_class_get_name (cfg->method->klass), cfg->method->name);
	int fd = create_socket (DEFAULT_HOST, DEFAULT_PORT);
	if (fd < 0) {
		g_warning ("cfg_dump: couldn't create socket: %s::%d", DEFAULT_HOST, DEFAULT_PORT);
		return;
	}

	MonoGraphDumper *ctx = (MonoGraphDumper *) mono_mempool_alloc0 (cfg->mempool, sizeof (MonoGraphDumper));
	ctx->fd = fd;
	ctx->constant_pool = g_hash_table_new ((GHashFunc) constant_pool_hash, constant_pool_equal);
	ctx->insn2id = g_hash_table_new ((GHashFunc) instruction_hash, instruction_equal);
	ctx->next_cp_id = 1;
	ctx->next_insn_id = 0;

	cfg->gdump_ctx = ctx;
}

void
mono_cfg_dump_ir (MonoCompile *cfg, const char *phase_name)
{
	if (cfg->gdump_ctx == NULL)
		return;
	cfg_debug ("=== DUMPING PASS \"%s\" ===", phase_name);
	write_byte (cfg, BEGIN_GRAPH);
	write_pool (cfg, create_cp_entry (cfg, (void *) phase_name, PT_STRING));

	int instruction_count = label_instructions (cfg);
	write_instructions (cfg, instruction_count);
	write_blocks (cfg);
}
#else /* !defined(DISABLE_LOGGING) && !defined(DISABLE_JIT) && !defined(HOST_WIN32) */
void
mono_cfg_dump_create_context (MonoCompile *cfg)
{
}

void
mono_cfg_dump_begin_group (MonoCompile *cfg)
{
}

void
mono_cfg_dump_close_group (MonoCompile *cfg)
{
}

void
mono_cfg_dump_ir (MonoCompile *cfg, const char *phase_name)
{
}
#endif /* !defined(DISABLE_LOGGING) && !defined(DISABLE_JIT) && !defined(HOST_WIN32) */
