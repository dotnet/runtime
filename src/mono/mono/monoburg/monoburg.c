/*
 * monoburg.c: an iburg like code generator generator
 *
 * Author:
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <stdio.h>

#include "monoburg.h"

extern void yyparse (void);

static GHashTable *term_hash;
static GList *term_list;
static GHashTable *nonterm_hash;
static GList *nonterm_list;
static GList *rule_list;

FILE *inputfd;
FILE *outputfd;
static FILE *deffd;

static void output (char *fmt, ...) 
{
	va_list ap;

	va_start(ap, fmt);
	vfprintf (outputfd, fmt, ap);
	va_end (ap);
}

void     
create_rule (char *id, Tree *tree, char *code, char *cost)
{
	Rule *rule = g_new0 (Rule, 1);

	rule->lhs = nonterm (id);
	rule->tree = tree;
	rule_list = g_list_append (rule_list, rule);
	rule->cost = cost;
	rule->code = code;

	rule->lhs->rules = g_list_append (rule->lhs->rules, rule);

	if (tree->op)
		tree->op->rules = g_list_append (tree->op->rules, rule);
	else 
		tree->nonterm->chain = g_list_append (tree->nonterm->chain, rule);
}

Tree *
create_tree (char *id, Tree *left, Tree *right)
{
	int arity = (left != NULL) + (right != NULL);
	Term *term = g_hash_table_lookup (term_hash, id);
	Tree *tree = g_new0 (Tree, 1);
	
	if (term) {
		if (term->arity == -1)
			term->arity = arity;

		if (term->arity != arity)
			yyerror ("changed arity of terminal %s from %d to %d",
				 id, term->arity, arity);

		tree->op = term;
		tree->left = left;
		tree->right = right;
	} else {
		tree->nonterm = nonterm (id);
	}

	return tree;
}

static void
check_term_num (char *key, Term *value, int num)
{
	if (num != -1 && value->number == num)
		yyerror ("duplicate terminal id \"%s\"", key);
}
 
void  
create_term (char *id, int num)
{
	Term *term;

	if (nonterm_list)
		yyerror ("terminal definition after nonterminal definition");

	if (num < -1)
		yyerror ("invalid terminal number %d", num);

	if (!term_hash) 
		term_hash = g_hash_table_new (g_str_hash , g_str_equal);

	g_hash_table_foreach (term_hash, (GHFunc) check_term_num, (gpointer) num);

	term = g_new0 (Term, 1);

	term->name = id;
	term->number = num;
	term->arity = -1;

	term_list = g_list_append (term_list, term);

	g_hash_table_insert (term_hash, term->name, term);
}

NonTerm *
nonterm (char *id)
{
	NonTerm *nterm;

	if (!nonterm_hash) 
		nonterm_hash = g_hash_table_new (g_str_hash , g_str_equal);

	if ((nterm = g_hash_table_lookup (nonterm_hash, id)))
		return nterm;

	nterm = g_new0 (NonTerm, 1);

	nterm->name = id;
	nonterm_list = g_list_append (nonterm_list, nterm);
	nterm->number = g_list_length (nonterm_list);
	
	g_hash_table_insert (nonterm_hash, nterm->name, nterm);

	return nterm;
}

void
start_nonterm (char *id)
{
	static gboolean start_def;
	
	if (start_def)
		yyerror ("start symbol redeclared");
	
	start_def = TRUE;
	nonterm (id); 
}

static void
emit_tree_string (Tree *tree)
{
	if (tree->op) {
		output ("%s", tree->op->name);
		if (tree->op->arity) {
			output ("(");
			emit_tree_string (tree->left);
			if (tree->right) {
				output (", ");
				emit_tree_string (tree->right);
			}
			output (")");
		}
	} else 
		output ("%s", tree->nonterm->name);
}

static void
emit_rule_string (Rule *rule, char *fill)
{
	output ("%s/* ", fill);
	
	output ("%s: ", rule->lhs->name);

	emit_tree_string (rule->tree);

	output (" */\n");
}

static int
next_term_num ()
{
	GList *l = term_list;
	int i = 1;

	while (l) {
		Term *t = (Term *)l->data;
		if (t->number == i) {
			l = term_list;
			i++;
		} else
			l = l->next;
	}
	return i;
}

static int
term_compare_func (Term *t1, Term *t2)
{
	return t1->number - t2->number;
}

static void
emit_header ()
{
	GList *l;

	output ("#include <glib.h>\n");
	output ("\n");

	output ("#ifndef MBTREE_TYPE\n#error MBTREE_TYPE undefined\n#endif\n");
	output ("#ifndef MBTREE_OP\n#define MBTREE_OP(t) ((t)->op)\n#endif\n");
	output ("#ifndef MBTREE_LEFT\n#define MBTREE_LEFT(t) ((t)->left)\n#endif\n");
	output ("#ifndef MBTREE_RIGHT\n#define MBTREE_RIGHT(t) ((t)->right)\n#endif\n");
	output ("#ifndef MBTREE_STATE\n#define MBTREE_STATE(t) ((t)->state)\n#endif\n");

	for (l = term_list; l; l = l->next) {
		Term *t = (Term *)l->data;
		if (t->number == -1)
			t->number = next_term_num ();
	}
	term_list = g_list_sort (term_list, (GCompareFunc)term_compare_func);

	for (l = term_list; l; l = l->next) {
		Term *t = (Term *)l->data;
		if (t->number == -1)
			t->number = next_term_num ();
		
		output ("#define MB_TERM_%s\t %d\n", t->name, t->number);

	}
	output ("\n");

}

static void
emit_nonterm ()
{
	GList *l;

	for (l = nonterm_list; l; l = l->next) {
		NonTerm *n = (NonTerm *)l->data;
		output ("#define MB_NTERM_%s\t%d\n", n->name, n->number);
	}
	output ("#define MB_MAX_NTERMS\t%d\n", g_list_length (nonterm_list));
	output ("\n");
}

static void
emit_state ()
{
	GList *l;
	int i, j;

	output ("typedef struct _MBState MBState;\n");
	output ("struct _MBState {\n");
	output ("\tint\t\t op;\n");
	output ("\tMBState\t\t*left, *right;\n");
	output ("\tguint16\t\tcost[%d];\n", g_list_length (nonterm_list) + 1);

	for (l = nonterm_list; l; l = l->next) {
		NonTerm *n = (NonTerm *)l->data;
		g_assert (g_list_length (n->rules) < 256);
		i = g_list_length (n->rules);
		j = 1;
		while (i >>= 1)
			j++;
		output ("\tunsigned int\t rule_%s:%d;\n",  n->name, j); 
	}
	output ("};\n\n");
}

static void
emit_decoders ()
{
	GList *l;
	GList *rl;

	for (l = nonterm_list; l; l = l->next) {
		NonTerm *n = (NonTerm *)l->data;
		output ("int mono_burg_decode_%s[] = {\n", n->name);
		output ("\t0,\n");
		for (rl = n->rules; rl; rl = rl->next) {
			Rule *rule = (Rule *)rl->data;
			output ("\t%d,\n", g_list_index (rule_list, rule) + 1);
		}
		
		output ("};\n\n");
	}
}

static void
emit_tree_match (char *st, Tree *t)
{
	char *tn;

	output ("\t\t\t%sop == %d /* %s */", st, t->op->number, t->op->name);
	
	if (t->left && t->left->op) {
		tn = g_strconcat (st, "left->", NULL);
		output (" &&\n");
		emit_tree_match (tn, t->left);
		g_free (tn);
	}

	if (t->right && t->right->op) {
		tn = g_strconcat (st, "right->", NULL);
		output (" &&\n");
		emit_tree_match (tn, t->right);
		g_free (tn);
	}
}

static void
emit_rule_match (Rule *rule)
{
	Tree *t = rule->tree; 

	if ((t->left && t->left->op) || 
	    (t->right && t->right->op)) {	
		output ("\t\tif (\n");
		emit_tree_match ("p->", t);
		output ("\n\t\t)\n");
	}
}

static void
emit_costs (char *st, Tree *t)
{
	char *tn;

	if (t->op) {

		if (t->left) {
			tn = g_strconcat (st, "left->", NULL);
			emit_costs (tn, t->left);
			g_free (tn);
		}

		if (t->right) {
			tn = g_strconcat (st, "right->", NULL);
			emit_costs (tn, t->right);
		}
	} else
		output ("%scost[MB_NTERM_%s] + ", st, t->nonterm->name);
}

static void
emit_cond_assign (Rule *rule, char *cost, char *fill)
{
	char *rc;

	if (cost)
		rc = g_strconcat ("c + ", cost, NULL);
	else
		rc = g_strdup ("c");


	output ("%sif (%s < p->cost[MB_NTERM_%s]) {\n", fill, rc, rule->lhs->name);

	output ("%s\tp->cost[MB_NTERM_%s] = %s;\n", fill, rule->lhs->name, rc);

	output ("%s\tp->rule_%s = %d;\n", fill, rule->lhs->name, 
		g_list_index (rule->lhs->rules, rule) + 1);

	if (rule->lhs->chain)
		output ("%s\tclosure_%s (p, %s);\n", fill, rule->lhs->name, rc); 

	output ("%s}\n", fill);

	g_free (rc);
	
}

static void
emit_label_func ()
{
	GList *l;
	int i;

	output ("static MBState *\n");
	output ("mono_burg_label_priv (MBTREE_TYPE *tree) {\n");

	output ("\tint arity;\n");
	output ("\tint c;\n");
	output ("\tMBState *p, *left, *right;\n\n");

	output ("\tswitch (mono_burg_arity [MBTREE_OP(tree)]) {\n");
	output ("\tcase 0:\n");
	output ("\t\tleft = NULL;\n");
	output ("\t\tright = NULL;\n");
	output ("\t\tbreak;\n");
	output ("\tcase 1:\n");
	output ("\t\tleft = mono_burg_label_priv (MBTREE_LEFT(tree));\n");
	output ("\t\tright = NULL;\n");
	output ("\t\tbreak;\n");
	output ("\tcase 2:\n");
	output ("\t\tleft = mono_burg_label_priv (MBTREE_LEFT(tree));\n");
	output ("\t\tright = mono_burg_label_priv (MBTREE_RIGHT(tree));\n");
	output ("\t}\n\n");

	output ("\tarity = (left != NULL) + (right != NULL);\n");
	output ("\tg_assert (arity == mono_burg_arity [MBTREE_OP(tree)]);\n\n");

	output ("\tp = g_new0 (MBState, 1);\n");
	output ("\tp->op = MBTREE_OP(tree);\n");
	output ("\tp->left = left;\n");
	output ("\tp->right = right;\n");
	
	for (l = nonterm_list, i = 0; l; l = l->next) {
		output ("\tp->cost [%d] = 32767;\n", ++i);
	}
	output ("\n");

	output ("\tswitch (MBTREE_OP(tree)) {\n");
	for (l = term_list; l; l = l->next) {
		Term *t = (Term *)l->data;
		GList *rl;
		output ("\tcase %d: /* %s */\n", t->number, t->name);

		for (rl = t->rules; rl; rl = rl->next) {
			Rule *rule = (Rule *)rl->data; 
			Tree *t = rule->tree;

			emit_rule_string (rule, "\t\t");

			emit_rule_match (rule);
			
			output ("\t\t{\n");

			output ("\t\t\tc = ");
			
			emit_costs ("", t);
	
			output ("%s;\n", rule->cost);

			emit_cond_assign (rule, NULL, "\t\t\t");

			output ("\t\t}\n");
		}

		output ("\t\tbreak;\n");
	}
	
	output ("\tdefault:\n");
	output ("\t\tg_error (\"unknown operator\");\n");
	output ("\t}\n\n");

	output ("\tMBTREE_STATE(tree) = p;\n");

	output ("\treturn p;\n");

	output ("}\n\n");

	output ("MBState *\n");
	output ("mono_burg_label (MBTREE_TYPE *tree)\n{\n");
	output ("\tMBState *p = mono_burg_label_priv (tree);\n");
	output ("\treturn p->rule_%s ? p : NULL;\n", ((NonTerm *)nonterm_list->data)->name);
	output ("}\n\n");
}

static char *
compute_kids (char *ts, Tree *tree, int *n)
{
	char *res;

	if (tree->nonterm) {
		return g_strdup_printf ("\t\tkids[%d] = %s;\n", (*n)++, ts);
	} else if (tree->op && tree->op->arity) {
		char *res2 = NULL;

		res = compute_kids (g_strdup_printf ("MBTREE_LEFT(%s)", ts), 
				    tree->left, n);
		if (tree->op->arity == 2)
			res2 = compute_kids (g_strdup_printf ("MBTREE_RIGHT(%s)", ts), 
					     tree->right, n);
		return g_strconcat (res, res2, NULL);
	}
	return "";
}

static void
emit_kids ()
{
	GList *l, *nl;
	int i, j, c, n, *si;
	char **sa;

	output ("int\n");
	output ("mono_burg_rule (MBState *state, int goal)\n{\n");

	output ("\tg_return_val_if_fail (state != NULL, 0);\n"); 
	output ("\tg_return_val_if_fail (goal > 0, 0);\n\n");

	output ("\tswitch (goal) {\n");

	for (nl = nonterm_list; nl; nl = nl->next) {
		NonTerm *n = (NonTerm *)nl->data;
		output ("\tcase MB_NTERM_%s:\n", n->name);
		output ("\t\treturn mono_burg_decode_%s [state->rule_%s];\n",
			n->name, n->name);
	}

	output ("\tdefault: g_assert_not_reached ();\n");
	output ("\t}\n");
	output ("\treturn 0;\n");
	output ("}\n\n");


	output ("MBTREE_TYPE **\n");
	output ("mono_burg_kids (MBTREE_TYPE *tree, int rulenr, MBTREE_TYPE *kids [])\n{\n");
	output ("\tg_return_val_if_fail (tree != NULL, NULL);\n");
	output ("\tg_return_val_if_fail (MBTREE_STATE(tree) != NULL, NULL);\n");
	output ("\tg_return_val_if_fail (kids != NULL, NULL);\n\n");

	output ("\tswitch (rulenr) {\n");

	n = g_list_length (rule_list);
	sa = g_new0 (char *, n);
	si = g_new0 (int, n);

	/* compress the case statement */
	for (l = rule_list, i = 0, c = 0; l; l = l->next) {
		Rule *rule = (Rule *)l->data;
		int kn = 0;
		char *k;

		k = compute_kids ("tree", rule->tree, &kn);
		for (j = 0; j < c; j++)
			if (!strcmp (sa [j], k))
				break;

		si [i++] = j;
		if (j == c)
			sa [c++] = k;
	}

	for (i = 0; i < c; i++) {
		for (l = rule_list, j = 0; l; l = l->next, j++)
			if (i == si [j])
				output ("\tcase %d:\n", j + 1);
		output ("%s", sa [i]);
		output ("\t\tbreak;\n");
	}

	output ("\tdefault:\n\t\tg_assert_not_reached ();\n");
	output ("\t}\n");
	output ("\treturn kids;\n");
	output ("}\n\n");

}

static void
emit_emitter_func ()
{
	GList *l;
	int i;

	for (l =  rule_list, i = 0; l; l = l->next) {
		Rule *rule = (Rule *)l->data;
		
		if (rule->code) {
			output ("static void ");

			emit_rule_string (rule, "");

			output ("mono_burg_emit_%d (MBTREE_TYPE *tree)\n", i);
			output ("{\n");
			output ("%s\n", rule->code);
			output ("}\n\n");
		}
		i++;
	}

	output ("MBEmitFunc mono_burg_func [] = {\n");
	output ("\tNULL,\n");
	for (l =  rule_list, i = 0; l; l = l->next) {
		Rule *rule = (Rule *)l->data;
		if (rule->code)
			output ("\tmono_burg_emit_%d,\n", i);
		else
			output ("\tNULL,\n");
		i++;
	}
	output ("};\n\n");
}

static void
emit_closure ()
{
	GList *l, *rl;

	for (l = nonterm_list; l; l = l->next) {
		NonTerm *n = (NonTerm *)l->data;
		
		if (n->chain)
			output ("static void closure_%s (MBState *p, int c);\n", n->name);
	}

	output ("\n");

	for (l = nonterm_list; l; l = l->next) {
		NonTerm *n = (NonTerm *)l->data;
		
		if (n->chain) {
			output ("static void\n");
			output ("closure_%s (MBState *p, int c)\n{\n", n->name);
			for (rl = n->chain; rl; rl = rl->next) {
				Rule *rule = (Rule *)rl->data;
				
				emit_rule_string (rule, "\t");
				emit_cond_assign (rule, rule->cost, "\t");
			}
			output ("}\n\n");
		}
	}
}

static char *
compute_nonterms (Tree *tree)
{
	if (!tree)
		return "";

	if (tree->nonterm) {
		return g_strdup_printf ("MB_NTERM_%s, ", tree->nonterm->name);
	} else {
		return g_strconcat (compute_nonterms (tree->left),
				    compute_nonterms (tree->right), NULL);
	} 
}

static void
emit_vardefs ()
{
	GList *l;
	int i, j, c, n, *si;
	char **sa;

	output ("guint8 mono_burg_arity [] = {\n"); 
	for (l = term_list, i = 0; l; l = l->next) {
		Term *t = (Term *)l->data;

		while (i < t->number) {
			output ("\t0,\n");
			i++;
		}
		
		output ("\t%d, /* %s */\n", t->arity, t->name);

		i++;
	}
	output ("};\n\n");

	output ("char *mono_burg_term_string [] = {\n");
	output ("\tNULL,\n");
	for (l = term_list, i = 0; l; l = l->next) {
		Term *t = (Term *)l->data;
		output ("\t\"%s\",\n", t->name);
	}
	output ("};\n\n");

	output ("char *mono_burg_rule_string [] = {\n");
	output ("\tNULL,\n");
	for (l = rule_list, i = 0; l; l = l->next) {
		Rule *rule = (Rule *)l->data;
		output ("\t\"%s: ", rule->lhs->name);
		emit_tree_string (rule->tree);
		output ("\",\n");
	}
	output ("};\n\n");

	n = g_list_length (rule_list);
	sa = g_new0 (char *, n);
	si = g_new0 (int, n);

	/* compress the _nts array */
	for (l = rule_list, i = 0, c = 0; l; l = l->next) {
		Rule *rule = (Rule *)l->data;
		char *s = compute_nonterms (rule->tree);

		for (j = 0; j < c; j++)
			if (!strcmp (sa [j], s))
				break;

		si [i++] = j;
		if (j == c) {
			output ("static guint16 mono_burg_nts_%d [] = { %s0 };\n", c, s);
			sa [c++] = s;
		}
	}	
	output ("\n");

	output ("guint16 *mono_burg_nts [] = {\n");
	output ("\t0,\n");
	for (l = rule_list, i = 0; l; l = l->next) {
		Rule *rule = (Rule *)l->data;
		output ("\tmono_burg_nts_%d, ", si [i++]);
		emit_rule_string (rule, "");
	}
	output ("};\n\n");
}

static void
emit_prototypes ()
{
	output ("typedef void (*MBEmitFunc) (MBTREE_TYPE *tree);\n\n");

	output ("extern char *mono_burg_term_string [];\n");
	output ("extern char *mono_burg_rule_string [];\n");
	output ("extern guint16 *mono_burg_nts [];\n");
	output ("extern MBEmitFunc mono_burg_func [];\n");

	output ("MBState *mono_burg_label (MBTREE_TYPE *tree);\n");
	output ("int mono_burg_rule (MBState *state, int goal);\n");
	output ("MBTREE_TYPE **mono_burg_kids (MBTREE_TYPE *tree, int rulenr, MBTREE_TYPE *kids []);\n");

	output ("\n");
}

static void check_reach (NonTerm *n);

static void
mark_reached (Tree *tree)
{
	if (tree->nonterm && !tree->nonterm->reached)
		check_reach (tree->nonterm);
	if (tree->left)
		mark_reached (tree->left);
	if (tree->right)
		mark_reached (tree->right);
}

static void
check_reach (NonTerm *n)
{
	GList *l;

	n->reached = 1;
	for (l = n->rules; l; l = l->next) {
		Rule *rule = (Rule *)l->data;
		mark_reached (rule->tree);
	}
}

static void
check_result ()
{
	GList *l;

	for (l = term_list; l; l = l->next) {
		Term *term = (Term *)l->data;
		if (term->arity == -1)
			g_warning ("unused terminal \"%s\"",term->name);
	} 

	check_reach (((NonTerm *)nonterm_list->data));

	for (l = nonterm_list; l; l = l->next) {
		NonTerm *n = (NonTerm *)l->data;
		if (!n->reached)
			g_error ("unreachable nonterm \"%s\"", n->name);
	}
}

static void
usage ()
{
	fprintf (stderr,
		 "Usage is: monoburg [-d file] [file] \n");
	exit (1);
}

int
main (int argc, char *argv [])
{
	char *deffile = NULL;
	char *infile = NULL;
	int i;

	for (i = 1; i < argc; i++){
		if (argv [i][0] == '-'){
			if (argv [i][1] == 'h') {
				usage ();
			} else if (argv [i][1] == 'd') {
				deffile = argv [++i];
			} else {
				usage ();
			}
		} else {
			if (infile)
				usage ();
			else
				infile = argv [i];
		}
	}

	if (deffile) {
		if (!(deffd = fopen (deffile, "w"))) {
			perror ("cant open header output file");
			exit (-1);
		}
		outputfd = deffd;
		output ("#ifndef _MONO_BURG_DEFS_\n");
		output ("#define _MONO_BURG_DEFS_\n\n");
	} else 
		outputfd = stdout;


	if (infile) {
		if (!(inputfd = fopen (infile, "r"))) {
			perror ("cant open input file");
			exit (-1);
		}
	} else {
		inputfd = stdin;
	}

	yyparse ();

	check_result ();

	if (!nonterm_list)
		g_error ("no start symbol found");

	emit_header ();
	emit_nonterm ();
	emit_state ();
	emit_prototypes ();

	if (deffd) {
		output ("#endif /* _MONO_BURG_DEFS_ */\n");
		fclose (deffd);
		outputfd = stdout;

		output ("#include \"%s\"\n\n", deffile);
	}
	
	emit_vardefs ();
	emit_emitter_func ();
	emit_decoders ();

	emit_closure ();
	emit_label_func ();

	emit_kids ();

	yyparsetail ();

	if (infile)
		fclose (inputfd);


	return 0;
}
