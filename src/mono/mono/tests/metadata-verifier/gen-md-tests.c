#include <ctype.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <glib.h>

#include <mono/metadata/image.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/metadata-internals.h>

#if 1
#define DEBUG_PARSER(stmt) do { stmt; } while (0)
#else
#define DEBUG_PARSER(stmt)
#endif

#if 0
#define DEBUG_SCANNER(stmt) do { stmt; } while (0)
#define SCANNER_DEBUG 1
#else
#define DEBUG_SCANNER(stmt)
#endif

/*
Grammar:

tokens:
	comment ::= '#.*<eol>
	identifier ::= ([a-z] | [A-Z]) ([a-z] | [A-Z] | [0-9] | [_-.])* 
	hexa_digit = [0-9] | [a-f] | [A-F]
	number ::= hexadecimal | decimal
	hexadecimal ::= (+-)?('0' [xX])? hexa_digit+
	decimal ::= 0 [0-9]+
	eol ::= <eol>
	punctuation ::= [{}]

program:
	test_case*

test_case:
	identifier '{' assembly_directive test_entry* '}'

assembly_directive:
 'assembly' identifier  

test_entry:
	validity patch (',' patch)*

validity:
	'valid' | 'invalid' | 'badrt'

patch:
	selector effect

selector:
	'offset' expression

effect:
	('set-byte' | 'set-ushort' | 'set-uint' | 'set-bit' | 'or-byte' | 'or-ushort' | 'or-uint' | 'truncate' ) expression

expression:
	atom ([+-] atom)*

atom:
	number | variable | function_call

function_call:
	fun_name '(' arg_list ')'

fun_name:
	read.byte |
	read.ushort |
	read.uint |
	translate.rva |
	translate.rva.ind |
	stream-header |
	table-row |
	blob.i

arg_list:
	expression |
	expression ',' arg_list

variable:
	file-size |
	pe-header |
	pe-optional-header |
	pe-signature |
	section-table |
	cli-header |
	cli-metadata |
	tables-header

TODO For the sake of a simple implementation, tokens are space delimited.
*/

enum {
	TOKEN_INVALID,
	TOKEN_ID,
	TOKEN_NUM,
	TOKEN_PUNC,
	TOKEN_EOF,
};

enum {
	OK,
	INVALID_TOKEN_TYPE,
	INVALID_PUNC_TEXT,
	INVALID_ID_TEXT,
	INVALID_VALIDITY_TEST,
	INVALID_NUMBER,
	INVALID_FILE_NAME,
	INVALID_SELECTOR,
	INVALID_EFFECT,
	INVALID_EXPRESSION,
	INVALID_VARIABLE_NAME,
	INVALID_FUNCTION_NAME,
	INVALID_ARG_COUNT,
	INVALID_RVA,
	INVALID_BAD_FILE
};

enum {
	TEST_TYPE_VALID,
	TEST_TYPE_INVALID,
	TEST_TYPE_BADRT
};

enum {
	SELECTOR_ABS_OFFSET,
};

enum {
	EFFECT_SET_BYTE,
	EFFECT_SET_USHORT,
	EFFECT_SET_UINT,
	EFFECT_SET_TRUNC,
	EFFECT_SET_BIT,
	EFFECT_OR_BYTE,
	EFFECT_OR_USHORT,
	EFFECT_OR_UINT,
};

enum {
	EXPRESSION_CONSTANT,
	EXPRESSION_VARIABLE,
	EXPRESSION_ADD,
	EXPRESSION_SUB,
	EXPRESSION_FUNC
};

typedef struct _expression expression_t;

typedef struct {
	int type;
	int start, end; /*stream range text is in [start, end[*/
	int line;
} token_t;

typedef struct {
	char *input;
	int idx, size, line;
	token_t current;
} scanner_t;


struct _expression {
	int type;
	union {
		gint32 constant;
		char *name;
		struct {
			expression_t *left;
			expression_t *right;
		} bin;
		struct {
			char *name;
			GSList *args;
		} func;
	} data;
};


typedef struct {
	int type;
	expression_t *expression;
} patch_selector_t;


typedef struct {
	int type;
	expression_t *expression;
} patch_effect_t;

typedef struct {
	patch_selector_t *selector;
	patch_effect_t *effect;
} test_patch_t;

typedef struct {
	char *name;
	char *assembly;
	int count;

	char *assembly_data;
	int assembly_size;
	MonoImage *image;
	int init;
} test_set_t;

typedef struct {
	int validity;
	GSList *patches; /*of test_patch_t*/
	char *data;
	int data_size;
	test_set_t *test_set;
} test_entry_t;



/*******************************************************************************************************/
static guint32 expression_eval (expression_t *exp, test_entry_t *entry);
static expression_t* parse_expression (scanner_t *scanner);

static const char*
test_validity_name (int validity)
{
	switch (validity) {
	case TEST_TYPE_VALID:
		return "valid";
	case TEST_TYPE_INVALID:
		return "invalid";
	case TEST_TYPE_BADRT:
		return "badrt";
	default:
		printf ("Invalid test type %d\n", validity);
		exit (INVALID_VALIDITY_TEST);
	}
}

static char*
read_whole_file_and_close (const char *name, int *file_size)
{
	FILE *file = fopen (name, "ro");
	char *res;
	int fsize;

	if (!file) {
		printf ("Could not open file %s\n", name);
		exit (INVALID_FILE_NAME);
	}

	fseek (file, 0, SEEK_END);
	fsize = ftell (file);
	fseek (file, 0, SEEK_SET);

	res = g_malloc (fsize + 1);

	fread (res, fsize, 1, file);
	fclose (file);
	*file_size = fsize;
	return res;
}

static void
init_test_set (test_set_t *test_set)
{
	MonoImageOpenStatus status;
	if (test_set->init)
		return;
	test_set->assembly_data = read_whole_file_and_close (test_set->assembly, &test_set->assembly_size);
	test_set->image = mono_image_open_from_data_internal (mono_alc_get_default (), test_set->assembly_data, test_set->assembly_size, FALSE, &status, FALSE, NULL, NULL);
	if (!test_set->image || status != MONO_IMAGE_OK) {
		printf ("Could not parse image %s\n", test_set->assembly);
		exit (INVALID_BAD_FILE);
	}
	
	test_set->init = 1;
}

static char*
make_test_name (test_entry_t *entry, test_set_t *test_set)
{
	return g_strdup_printf ("%s-%s-%d.exe", test_validity_name (entry->validity), test_set->name, test_set->count++);
}

#define READ_VAR(KIND, PTR) GUINT32_FROM_LE((guint32)*((KIND*)(PTR)))
#define SET_VAR(KIND, PTR, VAL) do { *((KIND*)(PTR)) = GUINT32_TO_LE ((KIND)VAL); }  while (0)

#define READ_BIT(PTR,OFF) ((((guint8*)(PTR))[(OFF / 8)] & (1 << ((OFF) % 8))) != 0)
#define SET_BIT(PTR,OFF) do { ((guint8*)(PTR))[(OFF / 8)] |= (1 << ((OFF) % 8)); } while (0)

static guint32 
get_pe_header (test_entry_t *entry)
{
	return READ_VAR (guint32, entry->data + 0x3c) + 4;
}

static guint32
translate_rva (test_entry_t *entry, guint32 rva)
{
	guint32 pe_header = get_pe_header (entry);
	guint32 sectionCount = READ_VAR (guint16, entry->data + pe_header + 2);
	guint32 idx = pe_header + 244;

	while (sectionCount-- > 0) {
		guint32 size = READ_VAR (guint32, entry->data + idx + 8);
		guint32 base = READ_VAR (guint32, entry->data + idx + 12);
		guint32 offset = READ_VAR (guint32, entry->data + idx + 20);

		if (rva >= base && rva <= base + size)
			return (rva - base) + offset;
		idx += 40;
	}
	printf ("Could not translate RVA %x\n", rva);
	exit (INVALID_RVA);
}

static guint32
get_cli_header (test_entry_t *entry)
{
	guint32 offset = get_pe_header (entry) + 20; /*pe-optional-header*/
	offset += 208; /*cli header entry offset in the pe-optional-header*/
	return translate_rva (entry, READ_VAR (guint32, entry->data + offset));
}

static guint32
get_cli_metadata_root (test_entry_t *entry)
{
	guint32 offset = get_cli_header (entry);
	offset += 8; /*metadata rva offset*/
	return translate_rva (entry, READ_VAR (guint32, entry->data + offset));
}

static guint32
pad4 (guint32 offset)
{
	if (offset % 4)
		offset += 4 - (offset % 4);
	return offset;
}

static guint32
get_metadata_stream_header (test_entry_t *entry, guint32 idx)
{
	guint32 offset;

	offset = get_cli_metadata_root (entry);
	offset = pad4 (offset + 16 + READ_VAR (guint32, entry->data + offset + 12));

	offset += 4;

	while (idx--) {
		int i;

		offset += 8;
		for (i = 0; i < 32; ++i) {
			if (!READ_VAR (guint8, entry->data + offset++))
				break;
		}
		offset = pad4 (offset);
	}
	return offset;	
}

static guint32
lookup_var (test_entry_t *entry, const char *name)
{
	if (!strcmp ("file-size", name))
		return entry->data_size;
	if (!strcmp ("pe-signature", name))
		return get_pe_header (entry) - 4;
	if (!strcmp ("pe-header", name))
		return get_pe_header (entry); 
	if (!strcmp ("pe-optional-header", name))
		return get_pe_header (entry) + 20; 
	if (!strcmp ("section-table", name))
		return get_pe_header (entry) + 244; 
	if (!strcmp ("cli-header", name))
		return get_cli_header (entry);
	if (!strcmp ("cli-metadata", name)) 
		return get_cli_metadata_root (entry);
	if (!strcmp ("tables-header", name)) {
		guint32 metadata_root = get_cli_metadata_root (entry);
		guint32 tilde_stream = get_metadata_stream_header (entry, 0);
		guint32 offset = READ_VAR (guint32, entry->data + tilde_stream);
		return metadata_root + offset;
	}

	printf ("Unknown variable in expression %s\n", name);
	exit (INVALID_VARIABLE_NAME);
}

static guint32
call_func (test_entry_t *entry, const char *name, GSList *args)
{
	if (!strcmp ("read.byte", name)) {
		guint32 offset;
		if (g_slist_length (args) != 1) {
			printf ("Invalid number of args to read.ushort %d\n", g_slist_length (args));
			exit (INVALID_ARG_COUNT);
		}
		offset = expression_eval (args->data, entry);
		return READ_VAR (guint8, entry->data + offset);
	}
	if (!strcmp ("read.ushort", name)) {
		guint32 offset;
		if (g_slist_length (args) != 1) {
			printf ("Invalid number of args to read.ushort %d\n", g_slist_length (args));
			exit (INVALID_ARG_COUNT);
		}
		offset = expression_eval (args->data, entry);
		return READ_VAR (guint16, entry->data + offset);
	}
	if (!strcmp ("read.uint", name)) {
		guint32 offset;
		if (g_slist_length (args) != 1) {
			printf ("Invalid number of args to read.uint %d\n", g_slist_length (args));
			exit (INVALID_ARG_COUNT);
		}
		offset = expression_eval (args->data, entry);
		return READ_VAR (guint32, entry->data + offset);
	}
	if (!strcmp ("translate.rva", name)) {
		guint32 rva;
		if (g_slist_length (args) != 1) {
			printf ("Invalid number of args to translate.rva %d\n", g_slist_length (args));
			exit (INVALID_ARG_COUNT);
		}
		rva = expression_eval (args->data, entry);
		return translate_rva (entry, rva);
	}
	if (!strcmp ("translate.rva.ind", name)) {
		guint32 rva;
		if (g_slist_length (args) != 1) {
			printf ("Invalid number of args to translate.rva.ind %d\n", g_slist_length (args));
			exit (INVALID_ARG_COUNT);
		}
		rva = expression_eval (args->data, entry);
		rva = READ_VAR (guint32, entry->data + rva);
		return translate_rva (entry, rva);
	}
	if (!strcmp ("stream-header", name)) {
		guint32 idx;
		if (g_slist_length (args) != 1) {
			printf ("Invalid number of args to stream-header %d\n", g_slist_length (args));
			exit (INVALID_ARG_COUNT);
		}
		idx = expression_eval (args->data, entry);
		return get_metadata_stream_header (entry, idx);
	}
	if (!strcmp ("table-row", name)) {
		const char *data;
		guint32 table, row;
		const MonoTableInfo *info;
		if (g_slist_length (args) != 2) {
			printf ("Invalid number of args to table-row %d\n", g_slist_length (args));
			exit (INVALID_ARG_COUNT);
		}
		table = expression_eval (args->data, entry);
		row = expression_eval (args->next->data, entry);
		info = mono_image_get_table_info (entry->test_set->image, table);
		data = info->base + row * info->row_size;
		return data - entry->test_set->assembly_data;
	}
	if (!strcmp ("blob.i", name)) {
		guint32 offset, base;
		MonoStreamHeader blob = entry->test_set->image->heap_blob;
		if (g_slist_length (args) != 1) {
			printf ("Invalid number of args to blob %d\n", g_slist_length (args));
			exit (INVALID_ARG_COUNT);
		}
		base = blob.data - entry->test_set->image->raw_data;
		offset = expression_eval (args->data, entry);
		offset = READ_VAR (guint16, entry->data + offset);
		return base + offset;
	}

	printf ("Unknown function %s\n", name);
	exit (INVALID_FUNCTION_NAME);

}

static guint32
expression_eval (expression_t *exp, test_entry_t *entry)
{
	switch (exp->type) {
	case EXPRESSION_CONSTANT:
		return exp->data.constant;
	case EXPRESSION_VARIABLE:
		return lookup_var (entry, exp->data.name);
	case EXPRESSION_ADD:
		return expression_eval (exp->data.bin.left, entry) + expression_eval (exp->data.bin.right, entry);
	case EXPRESSION_SUB:
		return expression_eval (exp->data.bin.left, entry) - expression_eval (exp->data.bin.right, entry);
	case EXPRESSION_FUNC:
		return call_func (entry, exp->data.func.name, exp->data.func.args);
	default:
		printf ("Invalid expression type %d\n", exp->type);
		exit (INVALID_EXPRESSION);
	}	return 0;
}

static guint32
apply_selector (patch_selector_t *selector, test_entry_t *entry)
{
	guint32 value = 0;
	if (selector->expression)
		value = expression_eval (selector->expression, entry);
	switch (selector->type) {
	case SELECTOR_ABS_OFFSET:
		DEBUG_PARSER (printf("\tabsolute offset selector [%04x]\n", value));
		return value;
	default:
		printf ("Invalid selector type %d\n", selector->type);
		exit (INVALID_SELECTOR);
	}
}

static void
apply_effect (patch_effect_t *effect, test_entry_t *entry, guint32 offset)
{
	gint32 value = 0;
	char *ptr = entry->data + offset;
	if (effect->expression)
		value = expression_eval (effect->expression, entry);

	switch (effect->type) {
	case EFFECT_SET_BYTE:
		DEBUG_PARSER (printf("\tset-byte effect old value [%x] new value [%x]\n", READ_VAR (guint8, ptr), value));
		SET_VAR (guint8, ptr, value);
		break;
	case EFFECT_SET_USHORT:
		DEBUG_PARSER (printf("\tset-ushort effect old value [%x] new value [%x]\n", READ_VAR (guint16, ptr), value));
		SET_VAR (guint16, ptr, value);
		break;
	case EFFECT_SET_UINT:
		DEBUG_PARSER (printf("\tset-uint effect old value [%x] new value [%x]\n", READ_VAR (guint32, ptr), value));
		SET_VAR (guint32, ptr, value);
		break;
	case EFFECT_SET_TRUNC:
		DEBUG_PARSER (printf("\ttrunc effect [%d]\n", offset));
		entry->data_size = offset;
		break;
	case EFFECT_SET_BIT:
		DEBUG_PARSER (printf("\tset-bit effect bit %d old value [%x]\n", value, READ_BIT (ptr, value)));
		SET_BIT (ptr, value);
		break;
	case EFFECT_OR_BYTE:
		DEBUG_PARSER (printf("\tor-byte effect old value [%x] new value [%x]\n", READ_VAR (guint8, ptr), value));
		SET_VAR (guint8, ptr, READ_VAR (guint8, ptr) | value);
		break;
	case EFFECT_OR_USHORT:
		DEBUG_PARSER (printf("\tor-ushort effect old value [%x] new value [%x]\n", READ_VAR (guint16, ptr), value));
		SET_VAR (guint16, ptr, READ_VAR (guint16, ptr) | value);
		break;
	case EFFECT_OR_UINT:
		DEBUG_PARSER (printf("\tor-uint effect old value [%x] new value [%x]\n", READ_VAR (guint32, ptr), value));
		SET_VAR (guint32, ptr, READ_VAR (guint32, ptr) | value);
		break;
	default:
		printf ("Invalid effect type %d\n", effect->type);
		exit (INVALID_EFFECT);
	}
}

static void
apply_patch (test_entry_t *entry, test_patch_t *patch)
{
	guint32 offset = apply_selector (patch->selector, entry);
	apply_effect (patch->effect, entry, offset);
}

static void
process_test_entry (test_set_t *test_set, test_entry_t *entry)
{
	GSList *tmp;
	char *file_name;
	FILE *f;

	init_test_set (test_set);
	entry->data = g_memdup (test_set->assembly_data, test_set->assembly_size);
	entry->data_size = test_set->assembly_size;
	entry->test_set = test_set;

	DEBUG_PARSER (printf("(%d)%s\n", test_set->count, test_validity_name (entry->validity)));
	for (tmp = entry->patches; tmp; tmp = tmp->next)
		apply_patch (entry, tmp->data);

	file_name = make_test_name (entry, test_set);

	f = fopen (file_name, "wo");
	fwrite (entry->data, entry->data_size, 1, f);
	fclose (f);

	g_free (file_name);
} 	

/*******************************************************************************************************/

static void
patch_free (test_patch_t *patch)
{
	free (patch->selector);
	free (patch->effect);
	free (patch);
}

static void
test_set_free (test_set_t *set)
{
	free (set->name);
	free (set->assembly);
	free (set->assembly_data);
	if (set->image)
		mono_image_close (set->image);
}

static void
test_entry_free (test_entry_t *entry)
{
	GSList *tmp;

	free (entry->data);
	for (tmp = entry->patches; tmp; tmp = tmp->next)
		patch_free (tmp->data);
	g_slist_free (entry->patches);
}


/*******************************************************************************************************/
static const char*
token_type_name (int type)
{
	switch (type) {
	case TOKEN_INVALID:
		return "invalid";
	case TOKEN_ID:
		return "identifier";
	case TOKEN_NUM:
		return "number";
	case TOKEN_PUNC:
		return "punctuation";
	case TOKEN_EOF:
		return "end of file";
	}
	return "unknown token type";
}

#define CUR_CHAR (scanner->input [scanner->idx])

static int
is_eof (scanner_t *scanner)
{
	return scanner->idx >= scanner->size;
}

static int
ispunct_char (int c)
{
	return c == '{' || c == '}' || c == ',';
}

static void
skip_spaces (scanner_t *scanner)
{
start:
	while (!is_eof (scanner) && isspace (CUR_CHAR)) {
		if (CUR_CHAR == '\n')
			++scanner->line;
		++scanner->idx;
	}
	if (CUR_CHAR == '#') {
		while (!is_eof (scanner) && CUR_CHAR != '\n') {
			++scanner->idx;
		}
		goto start;
	}
}

static char*
token_text_dup (scanner_t *scanner, token_t *token)
{
	int len = token->end - token->start;
	
	char *str = g_memdup (scanner->input + token->start, len + 1);
	str [len] = 0;
	return str;
}

#if SCANNER_DEBUG
static void
dump_token (scanner_t *scanner, token_t *token)
{
	char *str = token_text_dup (scanner, token);
	
	printf ("token '%s' of type '%s' at line %d\n", str, token_type_name (token->type), token->line);
	free (str);
}

#endif

static gboolean
is_special_char (char c)
{
	switch (c) {
	case ';':
	case ',':
	case '{':
	case '}':
	case '(':
	case ')':
		return TRUE;
	}
	return FALSE;
}

static void
next_token (scanner_t *scanner)
{
	int start, end, type;
	char c;
	skip_spaces (scanner);
	start = scanner->idx;
	while (!is_eof (scanner) && !isspace (CUR_CHAR)) {
		if (scanner->idx == start) {
			if (is_special_char (CUR_CHAR)) {
				++scanner->idx;
				break;
			}
		} else if (is_special_char (CUR_CHAR))
			break;
		++scanner->idx;
	}
	end = scanner->idx;

	c = scanner->input [start];
	if (start >= scanner->size)
		type = TOKEN_EOF;
	else if (isdigit (c) || c == '\'')
		type = TOKEN_NUM;
	else if (ispunct_char (c))
		type = TOKEN_PUNC;
	else
		type = TOKEN_ID;
	scanner->current.start = start;
	scanner->current.end = end;
	scanner->current.type = type;
	scanner->current.line = scanner->line;

	DEBUG_SCANNER (dump_token (scanner, &scanner->current));
}

static scanner_t*
scanner_new (const char *file_name)
{
	scanner_t *res;

	res = g_new0 (scanner_t, 1);
	res->input = read_whole_file_and_close (file_name, &res->size);

	res->line = 1;
	next_token (res);

	return res;
}

static void
scanner_free (scanner_t *scanner)
{
	free (scanner->input);
	free (scanner);
}

static token_t*
scanner_get_current_token (scanner_t *scanner)
{
	return &scanner->current;
}

static int
scanner_get_type (scanner_t *scanner)
{
	return scanner_get_current_token (scanner)->type;
}

static int
scanner_get_line (scanner_t *scanner)
{
	return scanner_get_current_token (scanner)->line;
}

static char*
scanner_text_dup (scanner_t *scanner)
{
	return token_text_dup (scanner, scanner_get_current_token (scanner));
}

static int
scanner_text_parse_number (scanner_t *scanner, long *res)
{
	char *text = scanner_text_dup (scanner);
	char *end = NULL;
	int ok;
	if (text [0] == '\'') {
		ok = strlen (text) != 3 || text [2] != '\'';
		if (!ok)
			*res = text [1];
	} else {
		*res = strtol (text, &end, 0);
		ok = *end;
	}
	free (text);

	return ok;
}

static int
match_current_type (scanner_t *scanner, int type)
{
	return scanner_get_type (scanner) == type;
}

static int
match_current_text (scanner_t *scanner, const char *text)
{
	token_t *t = scanner_get_current_token (scanner);
	return !strncmp (scanner->input + t->start, text, t->end - t->start);
}

static int
match_current_type_and_text (scanner_t *scanner, int type, const char *text)
{
	return match_current_type (scanner, type)  && match_current_text (scanner, text);
}

/*******************************************************************************************************/
#define FAIL(MSG, REASON) do { \
	printf ("%s at line %d for rule %s\n", MSG, scanner_get_line (scanner), __FUNCTION__);	\
	exit (REASON);	\
} while (0);

#define EXPECT_TOKEN(TYPE) do { \
	if (scanner_get_type (scanner) != TYPE) { \
		printf ("Expected %s but got %s '%s' at line %d for rule %s\n", token_type_name (TYPE), token_type_name (scanner_get_type (scanner)), scanner_text_dup (scanner), scanner_get_line (scanner), __FUNCTION__);	\
		exit (INVALID_TOKEN_TYPE);	\
	}	\
} while (0)

#define CONSUME_SPECIFIC_PUNCT(TEXT) do { \
	EXPECT_TOKEN (TOKEN_PUNC);	\
	if (!match_current_text (scanner, TEXT)) { \
		char *__tmp = scanner_text_dup (scanner);	\
		printf ("Expected '%s' but got '%s' at line %d for rule %s\n", TEXT, __tmp, scanner_get_line (scanner), __FUNCTION__);	\
		free (__tmp); \
		exit (INVALID_PUNC_TEXT);	\
	}	\
	next_token (scanner); \
} while (0)

#define CONSUME_IDENTIFIER(DEST) do { \
	EXPECT_TOKEN (TOKEN_ID);	\
	DEST = scanner_text_dup (scanner);	\
	next_token (scanner); \
} while (0)

#define CONSUME_SPECIFIC_IDENTIFIER(TEXT) do { \
	EXPECT_TOKEN (TOKEN_ID);	\
	if (!match_current_text (scanner, TEXT)) { \
		char *__tmp = scanner_text_dup (scanner);	\
		printf ("Expected '%s' but got '%s' at line %d for rule %s\n", TEXT, __tmp, scanner_get_line (scanner), __FUNCTION__);	\
		free (__tmp); \
		exit (INVALID_ID_TEXT);	\
	}	\
	next_token (scanner); \
} while (0)

#define CONSUME_NUMBER(DEST) do { \
	long __tmp_num;	\
	EXPECT_TOKEN (TOKEN_NUM);	\
	if (scanner_text_parse_number (scanner, &__tmp_num)) {	\
		char *__tmp = scanner_text_dup (scanner);	\
		printf ("Expected a number but got '%s' at line %d for rule %s\n", __tmp, scanner_get_line (scanner), __FUNCTION__);	\
		free (__tmp); \
		exit (INVALID_NUMBER);	\
	}	\
	DEST = __tmp_num; \
	next_token (scanner); \
} while (0)

#define LA_ID(TEXT) (scanner_get_type (scanner) == TOKEN_ID && match_current_text (scanner, TEXT))
#define LA_PUNCT(TEXT) (scanner_get_type (scanner) == TOKEN_PUNC && match_current_text (scanner, TEXT))

/*******************************************************************************************************/

static expression_t*
parse_atom (scanner_t *scanner)
{
	expression_t *atom = g_new0 (expression_t, 1);
	if (scanner_get_type (scanner) == TOKEN_NUM) {
		atom->type = EXPRESSION_CONSTANT;
		CONSUME_NUMBER (atom->data.constant);
	} else {
		char *name;
		CONSUME_IDENTIFIER (name);
		if (LA_ID ("(")) {
			atom->data.func.name = name;
			atom->type = EXPRESSION_FUNC;
			CONSUME_SPECIFIC_IDENTIFIER ("(");

			while (!LA_ID (")") && !match_current_type (scanner, TOKEN_EOF))
				atom->data.func.args = g_slist_append (atom->data.func.args, parse_expression (scanner));

			CONSUME_SPECIFIC_IDENTIFIER (")");
		} else {
			atom->data.name = name;
			atom->type = EXPRESSION_VARIABLE;
		}
	}
	return atom;
}


static expression_t*
parse_expression (scanner_t *scanner)
{
	expression_t *exp = parse_atom (scanner);

	while (LA_ID ("-") || LA_ID ("+")) {
		char *text;
		CONSUME_IDENTIFIER (text);
		expression_t *left = exp;
		exp = g_new0 (expression_t, 1);
		exp->type = !strcmp ("+", text) ? EXPRESSION_ADD: EXPRESSION_SUB;
		exp->data.bin.left = left;
		exp->data.bin.right = parse_atom (scanner);
	}
	return exp;
}


static patch_selector_t*
parse_selector (scanner_t *scanner)
{
	patch_selector_t *selector;

	CONSUME_SPECIFIC_IDENTIFIER ("offset");

	selector = g_new0 (patch_selector_t, 1);
	selector->type = SELECTOR_ABS_OFFSET;
	selector->expression = parse_expression (scanner);
	return selector;
}

static patch_effect_t*
parse_effect (scanner_t *scanner)
{
	patch_effect_t *effect;
	char *name;
	int type;

	CONSUME_IDENTIFIER(name);

	if (!strcmp ("set-byte", name))
		type = EFFECT_SET_BYTE; 
	else if (!strcmp ("set-ushort", name))
		type = EFFECT_SET_USHORT; 
	else if (!strcmp ("set-uint", name))
		type = EFFECT_SET_UINT; 
	else if (!strcmp ("set-bit", name))
		type = EFFECT_SET_BIT; 
	else if (!strcmp ("truncate", name))
		type = EFFECT_SET_TRUNC;
	else if (!strcmp ("or-byte", name))
		type = EFFECT_OR_BYTE;
	else if (!strcmp ("or-ushort", name))
		type = EFFECT_OR_USHORT;
	else if (!strcmp ("or-uint", name))
		type = EFFECT_OR_UINT;
	else 
		FAIL(g_strdup_printf ("Invalid effect kind, expected one of: (set-byte set-ushort set-uint set-bit or-byte or-ushort or-uint truncate) but got %s",name), INVALID_ID_TEXT);

	effect = g_new0 (patch_effect_t, 1);
	effect->type = type;
	if (type != EFFECT_SET_TRUNC)
		effect->expression = parse_expression (scanner);
	return effect;
}

static test_patch_t*
parse_patch (scanner_t *scanner)
{
	test_patch_t *patch;

	patch = g_new0 (test_patch_t, 1);
	patch->selector = parse_selector (scanner);
	patch->effect = parse_effect (scanner);
	return patch;
}

static int
parse_validity (scanner_t *scanner)
{
	char *name = NULL;
	int validity;
	CONSUME_IDENTIFIER (name);

	if (!strcmp (name, "valid"))
		validity = TEST_TYPE_VALID;
	else if (!strcmp (name, "invalid"))
		validity = TEST_TYPE_INVALID;
	else if (!strcmp (name, "badrt"))
		validity = TEST_TYPE_BADRT;
	else {
		printf ("Expected either 'valid', 'invalid' or 'badtr' but got '%s' at the beginning of a test entry at line %d\n", name, scanner_get_line (scanner));
		exit (INVALID_VALIDITY_TEST);
	}

	free (name);
	return validity;
}

static void
parse_test_entry (scanner_t *scanner, test_set_t *test_set)
{
	test_entry_t entry = { 0 };
	
	entry.validity = parse_validity (scanner);

	do {
		if (entry.patches)
			CONSUME_SPECIFIC_PUNCT (",");
		entry.patches = g_slist_append (entry.patches, parse_patch (scanner));
	} while (match_current_type_and_text (scanner, TOKEN_PUNC, ","));

	process_test_entry (test_set, &entry);

	test_entry_free (&entry);
}

static void
parse_test (scanner_t *scanner)
{
	test_set_t set = { 0 };

	CONSUME_IDENTIFIER (set.name);
	CONSUME_SPECIFIC_PUNCT ("{");
	CONSUME_SPECIFIC_IDENTIFIER ("assembly");
	CONSUME_IDENTIFIER (set.assembly);

	DEBUG_PARSER (printf ("RULE %s using assembly %s\n", set.name, set.assembly));

	while (!match_current_type (scanner, TOKEN_EOF) && !match_current_type_and_text (scanner, TOKEN_PUNC, "}"))
		parse_test_entry (scanner, &set);

	CONSUME_SPECIFIC_PUNCT ("}");

	test_set_free (&set);
}


static void
parse_program (scanner_t *scanner)
{
	while (!match_current_type (scanner, TOKEN_EOF))
		parse_test (scanner);
}


static void
digest_file (const char *file)
{
	scanner_t *scanner = scanner_new (file); 
	parse_program (scanner);
	scanner_free (scanner);
}

int
main (int argc, char **argv)
{
	if (argc != 2) {
		printf ("usage: gen-md.test file_to_process\n");
		return 1;
	}

	mono_init_version ("gen-md-test", "v2.0.50727");
	mono_marshal_init ();

	digest_file (argv [1]);
	return 0;
}

