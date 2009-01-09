#include <stdio.h>
#include <stdlib.h>
#include <memory.h>
#include <glib.h>

#if 1
#define DEBUG(stmt) do { stmt; } while (0)
#endif
/*
Grammar:

tokens:
	comment ::= '#.*<eol>
	identifier ::= ([a-z] | [A-Z]) ([a-z] | [A-Z] | [0-9] | [_-.])* 
	hexa_digit = [0-9] | [a-f] | [A-F]
	number ::= [0-9] ([0-9] hexa_digit)* | ('0' [xX] hexa_digit+)
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
	'valid' | 'invalid'

patch:
	selector effect

selector:
	'offset' number

effect:
	'set-byte' number

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
	INVALID_NUMBER
};

enum {
	TEST_TYPE_VALID,
	TEST_TYPE_INVALID
};

enum {
	SELECTOR_ABS_OFFSET,
};

enum {
	EFFECT_SET_BYTE,
};

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

typedef struct {
	int type;
	union {
		long offset;
	} data;
} patch_selector_t;

typedef struct {
	int type;
	union {
		long value;
	} data;
} patch_effect_t;

typedef struct {
	patch_selector_t *selector;
	patch_effect_t *effect;
} test_patch_t;

typedef struct {
	int validity;
	GSList *patches; /*of test_patch_t*/
} test_entry_t;

typedef struct {
	char *name;
	char *assembly;
	int count;
} test_set_t;

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
ispunct (int c)
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
	char *str = malloc (len + 1);
	memcpy (str, scanner->input + token->start, len);
	str [len] = 0;
	return str;
}

static void
dump_token (scanner_t *scanner, token_t *token)
{
	char *str = token_text_dup (scanner, token);
	
	printf ("token '%s' of type '%s' at line %d\n", str, token_type_name (token->type), token->line);
	free (str);
}

static void
next_token (scanner_t *scanner)
{
	int start, end, type;
	char c;
	skip_spaces (scanner);
	start = scanner->idx;
	while (!is_eof (scanner) && !isspace (CUR_CHAR)) {
		++scanner->idx;
	}
	end = scanner->idx;

	c = scanner->input [start];
	if (start >= scanner->size)
		type = TOKEN_EOF;
	else if (isdigit (c))
		type = TOKEN_NUM;
	else if (ispunct (c))
		type = TOKEN_PUNC;
	else
		type = TOKEN_ID;
	scanner->current.start = start;
	scanner->current.end = end;
	scanner->current.type = type;
	scanner->current.line = scanner->line;

	DEBUG (dump_token (scanner, &scanner->current));
}

static scanner_t*
scanner_new (FILE *file)
{
	long fsize;
	scanner_t *res;

	fseek (file, 0, SEEK_END);
	fsize = ftell (file);
	fseek (file, 0, SEEK_SET);

	res = malloc (sizeof (scanner_t));
	memset (res, 0, sizeof (scanner_t));
	res->input = malloc (fsize + 1);

	fread (res->input, fsize, 1, file);
	fclose (file);
	res->input [fsize] = 0;
	res->size = fsize;
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
	*res = strtol (text, &end, 16);
	ok = *end;
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

#define EXPECT_TOKEN(TYPE) do { \
	if (scanner_get_type (scanner) != TYPE) { \
		printf ("Expected %s but got %s at line %d for rule %s\n", token_type_name (TYPE), token_type_name (scanner_get_type (scanner)), scanner_get_line (scanner), __FUNCTION__);	\
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

/*******************************************************************************************************/

static void
patch_free (test_patch_t *patch)
{
	free (patch->selector);
	free (patch->effect);
	free (patch);
}

/*******************************************************************************************************/

static patch_selector_t*
parse_selector (scanner_t *scanner)
{
	patch_selector_t *selector;
	long off;

	CONSUME_SPECIFIC_IDENTIFIER ("offset");
	CONSUME_NUMBER (off);

	selector = malloc (sizeof (patch_selector_t));
	selector->type = SELECTOR_ABS_OFFSET;
	selector->data.offset = off;
	return selector;
}

static patch_effect_t*
parse_effect (scanner_t *scanner)
{
	patch_effect_t *effect;
	long value;

	CONSUME_SPECIFIC_IDENTIFIER ("set-byte");
	CONSUME_NUMBER (value);

	effect = malloc (sizeof (patch_effect_t));
	effect->type = EFFECT_SET_BYTE;
	effect->data.value = value;
	return effect;
}

static test_patch_t*
parse_patch (scanner_t *scanner)
{
	test_patch_t *patch;

	patch = malloc (sizeof (test_patch_t));
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
	else {
		printf ("Expected either 'valid' or 'invalid' but got '%s' at the begining of a test entry at line %d\n", name, scanner_get_line (scanner));
		exit (INVALID_VALIDITY_TEST);
	}

	free (name);
	return validity;
}

static void
parse_test_entry (scanner_t *scanner, test_set_t *test_set)
{
	GSList *tmp;
	test_entry_t entry;
	int res;
	memset (&entry, 0, sizeof (test_entry_t));
	
	entry.validity = parse_validity (scanner);

	do {
		entry.patches = g_slist_append (entry.patches, parse_patch (scanner));
	} while (match_current_type_and_text (scanner, TOKEN_PUNC, ","));

	//TODO consume the test_entry here

	for (tmp = entry.patches; tmp; tmp = tmp->next)
		patch_free (tmp->data);
	g_slist_free (entry.patches);
}

static void
parse_test (scanner_t *scanner)
{
	test_set_t set = { 0 };

	CONSUME_IDENTIFIER (set.name);
	CONSUME_SPECIFIC_PUNCT ("{");
	CONSUME_SPECIFIC_IDENTIFIER ("assembly");
	CONSUME_IDENTIFIER (set.assembly);

	DEBUG (printf ("\tRULE %s using assembly %s\n", set.name, set.assembly));

	while (!match_current_type (scanner, TOKEN_EOF) && !match_current_type_and_text (scanner, TOKEN_PUNC, "}"))
		parse_test_entry (scanner, &set);

	CONSUME_SPECIFIC_PUNCT ("}");

	free (set.name);
	free (set.assembly);
}


static void
parse_program (scanner_t *scanner)
{
	while (!match_current_type (scanner, TOKEN_EOF))
		parse_test (scanner);
}


static int
digest_file (FILE *file)
{
	int ret;
	scanner_t *scanner = scanner_new (file); 
	parse_program (scanner);
	scanner_free (scanner);
	return ret;
}

int
main (int argc, char **argv)
{
	if (argc != 2) {
		printf ("usage: gen-md.test file_to_process\n");
		return 1;
	}

	FILE *f = fopen (argv [1], "ro");
	if (!f) {
		printf ("could not open file %s\n", argv [1]);
		return 2;
	}

	return digest_file (f);
}

