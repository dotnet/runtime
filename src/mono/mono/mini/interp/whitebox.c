#include <config.h>
#include <stdio.h>
#include "transform.h"
#include "mintops.h"
#include <mono/metadata/assembly-internals.h>
#include <mono/metadata/mono-config.h>
#include <mono/metadata/gc-internals.h>

/* return value of "0" equals success */
typedef int (*TestVerifier)(TransformData *td);

typedef struct
{
	const char *test_name;
	/* function pointer to result verifier */
	TestVerifier verify_td;
} TestItem;

static MonoMemPool *mp = NULL;
static GList *test_list = NULL;
static const char *verbose_method_name = NULL;

static void
print_td (TransformData *td)
{
	if (!td->verbose_level)
		return;

	mono_interp_print_td_code (td);
}

static int
expect (InterpInst **ins, InterpInst **current, guint16 opcode)
{
	g_assert (ins);

	if (!*ins)
		return 1;

	while ((*ins)->opcode == MINT_NOP)
		*ins = (*ins)->next;

	if ((*ins)->opcode == opcode) {
		if (current)
			*current = *ins;

		*ins = (*ins)->next;
		return 0;
	}
	return 2;
}

static int
verify_cprop_add_consts (TransformData *td)
{
	mono_test_interp_cprop (td);
	print_td (td);

	InterpInst *ins = td->first_ins, *current;
	if (expect (&ins, &current, MINT_LDC_I4))
		return 1;
	if (READ32 (&current->data [0]) != 0x4466)
		return 2;
	if (expect (&ins, NULL, MINT_RET))
		return 3;

	return 0;
}

static int
verify_cprop_ldloc_stloc (TransformData *td)
{
	mono_test_interp_cprop (td);
	print_td (td);

	InterpInst *ins = td->first_ins;
	if (expect (&ins, NULL, MINT_INITLOCALS))
		return 1;
	if (expect (&ins, NULL, MINT_CALL))
		return 2;
	if (expect (&ins, NULL, MINT_ADD_I4))
		return 5;
	if (expect (&ins, NULL, MINT_RET))
		return 6;

	return 0;
}

static void
new_test (const char *name, TestVerifier verifier)
{
	TestItem *ti = g_malloc (sizeof (TestItem));
	ti->test_name = name;
	ti->verify_td = verifier;

	test_list = g_list_append_mempool (mp, test_list, ti);
}

static MonoImage *
load_assembly (const char *path, MonoDomain *root_domain)
{
	MonoAssemblyOpenRequest req;
	mono_assembly_request_prepare_open (&req, mono_alc_get_default ());
	MonoAssembly *ass = mono_assembly_request_open (path, &req, NULL);
	if (!ass)
		g_error ("failed to load assembly: %s", path);
	return mono_assembly_get_image_internal (ass);
}

static MonoMethod *
lookup_method_from_image (MonoImage *image, const char *name)
{
	for (int i = 0; i < mono_image_get_table_rows (image, MONO_TABLE_METHOD); i++) {
		ERROR_DECL (error);
		MonoMethod *method = mono_get_method_checked (image, MONO_TOKEN_METHOD_DEF | (i + 1), NULL, NULL, error);
		if (strcmp (method->name, name) == 0) {
			mono_class_init_internal (method->klass);
			return method;
		}
	}
	g_error ("method \"%s\" does not exist in assembly \"%s\\n", name, image->assembly_name);
}

static int
determine_verbose_level (TransformData *td)
{
	if (!verbose_method_name)
		return 0;

	if (!strcmp ("ALL", verbose_method_name))
		return 4;

	MonoMethod *method = td->method;
	const char *name = verbose_method_name;

	if ((strchr (name, '.') > name) || strchr (name, ':')) {
		MonoMethodDesc *desc = mono_method_desc_new (name, TRUE);
		int match = mono_method_desc_full_match (desc, method);
		mono_method_desc_free (desc);
		if (match)
			return 4;
	} else if (!strcmp (method->name, name))
		return 4;

	return 0;
}

static TransformData *
transform_method (MonoDomain *domain, MonoImage *image, TestItem *ti)
{
	ERROR_DECL (error);
	MonoMethod *method = lookup_method_from_image (image, ti->test_name);
	MonoMethodHeader *header = mono_method_get_header_checked (method, error);;;
	MonoMethodSignature *signature = mono_method_signature_internal (method);

	InterpMethod *rtm = g_new0 (InterpMethod, 1);
	rtm->method = method;
	rtm->domain = domain;
	/* TODO: init more fields of `rtm` */

	TransformData *td = g_new0 (TransformData, 1);
	td->method = method;
	td->verbose_level = determine_verbose_level (td);
	td->mempool = mp;
	td->rtm = rtm;
	td->clause_indexes = (int*)g_malloc (header->code_size * sizeof (int));
	td->data_items = NULL;
	td->data_hash = g_hash_table_new (NULL, NULL);
	/* TODO: init more fields of `td` */

	mono_test_interp_method_compute_offsets (td, rtm, signature, header);

	td->stack = (StackInfo*)g_malloc0 ((header->max_stack + 1) * sizeof (td->stack [0]));
	td->stack_capacity = header->max_stack + 1;
	td->sp = td->stack;
	td->max_stack_height = 0;

	mono_test_interp_generate_code (td, method, header, NULL, error);

	mono_metadata_free_mh (header);
	return td;
}

int
main (int argc, char* argv[])
{
	if (argc < 2)
		g_error ("need to pass whitebox assembly");

	int test_failed = 0, test_success = 0;
	char *whitebox_assembly = argv [1];
	mp = mono_mempool_new ();

	/* test list */
	new_test ("test_cprop_add_consts", verify_cprop_add_consts);
	new_test ("test_cprop_ldloc_stloc", verify_cprop_ldloc_stloc);

	/* init mono runtime */
	g_set_prgname (argv [0]);
	mono_set_rootdir ();
	mono_config_parse (NULL);
	MonoDomain *root_domain = mini_init ("whitebox", NULL);
	mono_gc_set_stack_end (&root_domain);

	verbose_method_name = g_getenv ("MONO_VERBOSE_METHOD");

	g_print ("interp opt white box testing suite with %s, running %d tests\n", whitebox_assembly, g_list_length (test_list));

	MonoImage *image = load_assembly (whitebox_assembly, root_domain);

	for (GList *iter = test_list; iter; iter = iter->next) {
		TestItem *ti = (TestItem *) iter->data;
		TransformData *td = transform_method (root_domain, image, ti);
		int result = ti->verify_td (td);
		g_print ("test \"%s\": %d\n", ti->test_name, result);
		free (td);

		if (result)
			test_failed++;
		else
			test_success++;
	}

	g_print ("\nSUMMARY: %d / %d passed\n", test_success, test_success + test_failed);

	/* TODO: shut runtime down, release resources, etc. */
	return test_failed;
}
