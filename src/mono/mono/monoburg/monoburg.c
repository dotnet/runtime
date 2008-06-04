/*
 * monoburg.c: an iburg like code generator generator
 *
 * Author:
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#include "monoburg.h"

extern void yyparse (void);

static GHashTable *term_hash;
static GList *term_list;
static GHashTable *nonterm_hash;
static GList *nonterm_list;
static GList *rule_list;
static GList *prefix_list;

FILE *inputfd;
FILE *outputfd;
GHashTable *definedvars;
static FILE *deffd;
static FILE *cfd;

static int dag_mode = 0;
static int predefined_terms = 0;
static int default_cost = 0;

static void output (char *fmt, ...) 
{
	va_list ap;

	va_start(ap, fmt);
	vfprintf (outputfd, fmt, ap);
	va_end (ap);
}

Rule*
make_rule (char *id, Tree *tree)
{
	Rule *rule = g_new0 (Rule, 1);
	rule->lhs = nonterm (id);
	rule->tree = tree;

	return rule;
}

void
rule_add (Rule *rule, char *code, char *cost, char *cfunc)
{
	if (!cfunc && !cost)
		cost = g_strdup_printf ("%d", default_cost);

	rule_list = g_list_append (rule_list, rule);
	rule->cost = g_strdup (cost);
	rule->cfunc = g_strdup (cfunc);
	rule->code = g_strdup (code);

	if (cfunc) {
		if (cost)
			yyerror ("duplicated costs (constant costs and cost function)");
		else {
			if (dag_mode)
				rule->cost = g_strdup_printf ("mono_burg_cost_%d (p, data)",
							      g_list_length (rule_list));
			else
				rule->cost = g_strdup_printf ("mono_burg_cost_%d (tree, data)",
							      g_list_length (rule_list));
		}
	}

	rule->lhs->rules = g_list_append (rule->lhs->rules, rule);

	if (rule->tree->op)
		rule->tree->op->rules = g_list_append (rule->tree->op->rules, rule);
	else 
		rule->tree->nonterm->chain = g_list_append (rule->tree->nonterm->chain, rule);
}

void     
create_rule (char *id, Tree *tree, char *code, char *cost, char *cfunc)
{
	Rule *rule = make_rule (id, tree);

	rule_add (rule, code, cost, cfunc);
}

Tree *
create_tree (char *id, Tree *left, Tree *right)
{
	int arity = (left != NULL) + (right != NULL);
	Term *term = NULL; 
	Tree *tree = g_new0 (Tree, 1);

	if (term_hash)
		term = g_hash_table_lookup (term_hash, id);

	/* try if id has termprefix */
	if (!term) {
		GList *pl;
		for (pl = prefix_list; pl; pl = pl->next) {
			char *pfx = (char *)pl->data;
			if (!strncmp (pfx, id, strlen (pfx))) {
				term = create_term (id, -1);
				break;
			}
		}

	}

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
create_term_prefix (char *id)
{
	if (!predefined_terms)
		yyerror ("%termprefix is only available with -p option");

	prefix_list = g_list_prepend (prefix_list, g_strdup (id));
}

Term *
create_term (char *id, int num)
{
	Term *term;

	if (!predefined_terms && nonterm_list)
		yyerror ("terminal definition after nonterminal definition");

	if (num < -1)
		yyerror ("invalid terminal number %d", num);

	if (!term_hash) 
		term_hash = g_hash_table_new (g_str_hash , g_str_equal);

	g_hash_table_foreach (term_hash, (GHFunc) check_term_num, GINT_TO_POINTER (num));

	term = g_new0 (Term, 1);

	term->name = g_strdup (id);
	term->number = num;
	term->arity = -1;

	term_list = g_list_append (term_list, term);

	g_hash_table_insert (term_hash, term->name, term);

	return term;
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

	nterm->name = g_strdup (id);
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
	output ("#ifndef MBREG_TYPE\n#define MBREG_TYPE gint32\n#endif\n");
	output ("#ifndef MBCGEN_TYPE\n#define MBCGEN_TYPE int\n#endif\n");
	output ("#ifndef MBALLOC_STATE\n#define MBALLOC_STATE g_new (MBState, 1)\n#endif\n");
	output ("#ifndef MBCOST_DATA\n#define MBCOST_DATA gpointer\n#endif\n");
	output ("\n");
	output ("#define MBMAXCOST 32768\n");

	output ("\n");
	output ("#define MBCOND(x) if (!(x)) return MBMAXCOST;\n");

	output ("\n");

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

		if (predefined_terms)
			output ("#define MB_TERM_%s\t %s\n", t->name, t->name);
		else
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

	if (dag_mode) {
		output ("\tMBTREE_TYPE\t *tree;\n");
		output ("\tMBREG_TYPE\t reg1;\n");
		output ("\tMBREG_TYPE\t reg2;\n");
	}
	
	output ("\tMBState\t\t*left, *right;\n");
	output ("\tguint16\t\tcost[%d];\n", g_list_length (nonterm_list) + 1);

	for (l = nonterm_list; l; l = l->next) {
		NonTerm *n = (NonTerm *)l->data;
		g_assert (g_list_length (n->rules) < 512);
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
		output ("const short mono_burg_decode_%s[] = {\n", n->name);
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
	int not_first = strcmp (st, "p->");

	/* we can omit this check at the top level */
	if (not_first) {
		if (predefined_terms)
			output ("\t\t\t%sop == %s /* %s */", st, t->op->name, t->op->name);
		else
			output ("\t\t\t%sop == %d /* %s */", st, t->op->number, t->op->name);
	}

	if (t->left && t->left->op) {
		tn = g_strconcat (st, "left->", NULL);
		if (not_first)
			output (" &&\n");
		not_first = 1;
		emit_tree_match (tn, t->left);
		g_free (tn);
	}

	if (t->right && t->right->op) {
		tn = g_strconcat (st, "right->", NULL);
		if (not_first)
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

	if (dag_mode) {
		output ("static void\n");
		output ("mono_burg_label_priv (MBTREE_TYPE *tree, MBCOST_DATA *data, MBState *p) {\n");
	} else {
		output ("static MBState *\n");
		output ("mono_burg_label_priv (MBTREE_TYPE *tree, MBCOST_DATA *data) {\n");
	}

	output ("\tint arity;\n");
	output ("\tint c;\n");
	if (!dag_mode) 
		output ("\tMBState *p;\n");
	output ("\tMBState *left = NULL, *right = NULL;\n\n");

	output ("\tswitch (mono_burg_arity [MBTREE_OP(tree)]) {\n");
	output ("\tcase 0:\n");
	output ("\t\tbreak;\n");
	output ("\tcase 1:\n");
	if (dag_mode) {
		output ("\t\tleft = MBALLOC_STATE;\n");
		output ("\t\tmono_burg_label_priv (MBTREE_LEFT(tree), data, left);\n");		
	} else {
		output ("\t\tleft = mono_burg_label_priv (MBTREE_LEFT(tree), data);\n");
		output ("\t\tright = NULL;\n");
	}
	output ("\t\tbreak;\n");
	output ("\tcase 2:\n");
	if (dag_mode) {
		output ("\t\tleft = MBALLOC_STATE;\n");
		output ("\t\tmono_burg_label_priv (MBTREE_LEFT(tree), data, left);\n");		
		output ("\t\tright = MBALLOC_STATE;\n");
		output ("\t\tmono_burg_label_priv (MBTREE_RIGHT(tree), data, right);\n");		
	} else {
		output ("\t\tleft = mono_burg_label_priv (MBTREE_LEFT(tree), data);\n");
		output ("\t\tright = mono_burg_label_priv (MBTREE_RIGHT(tree), data);\n");
	}
	output ("\t}\n\n");

	output ("\tarity = (left != NULL) + (right != NULL);\n");
	output ("\tg_assert (arity == mono_burg_arity [MBTREE_OP(tree)]);\n\n");

	if (!dag_mode)
		output ("\tp = MBALLOC_STATE;\n");

	output ("\tmemset (p, 0, sizeof (MBState));\n");
	output ("\tp->op = MBTREE_OP(tree);\n");
	output ("\tp->left = left;\n");
	output ("\tp->right = right;\n");

	if (dag_mode)
		output ("\tp->tree = tree;\n");	
	
	for (l = nonterm_list, i = 0; l; l = l->next) {
		output ("\tp->cost [%d] = 32767;\n", ++i);
	}
	output ("\n");

	output ("\tswitch (MBTREE_OP(tree)) {\n");
	for (l = term_list; l; l = l->next) {
		Term *t = (Term *)l->data;
		GList *rl;
		
		if (predefined_terms)
			output ("\tcase %s: /* %s */\n", t->name, t->name);
		else
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
	output ("#ifdef MBGET_OP_NAME\n");
	output ("\t\tg_error (\"unknown operator: %%s\", MBGET_OP_NAME(MBTREE_OP(tree)));\n");
	output ("#else\n");
	output ("\t\tg_error (\"unknown operator: 0x%%04x\", MBTREE_OP(tree));\n");
	output ("#endif\n");
	output ("\t}\n\n");

	if (!dag_mode) {
		output ("\tMBTREE_STATE(tree) = p;\n");
		output ("\treturn p;\n");
	}

	output ("}\n\n");

	output ("MBState *\n");
	output ("mono_burg_label (MBTREE_TYPE *tree, MBCOST_DATA *data)\n{\n");
	if (dag_mode) {
		output ("\tMBState *p = MBALLOC_STATE;\n");
		output ("\tmono_burg_label_priv (tree, data, p);\n");		
	} else {
		output ("\tMBState *p = mono_burg_label_priv (tree, data);\n");
	}
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

		if (dag_mode) {
			res = compute_kids (g_strdup_printf ("%s->left", ts), 
					    tree->left, n);
			if (tree->op->arity == 2)
				res2 = compute_kids (g_strdup_printf ("%s->right", ts), 
						     tree->right, n);
		} else {
			res = compute_kids (g_strdup_printf ("MBTREE_LEFT(%s)", ts), 
					    tree->left, n);
			if (tree->op->arity == 2)
				res2 = compute_kids (g_strdup_printf ("MBTREE_RIGHT(%s)", ts), 
						     tree->right, n);
		}

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


	if (dag_mode) {
		output ("MBState **\n");
		output ("mono_burg_kids (MBState *state, int rulenr, MBState *kids [])\n{\n");
		output ("\tg_return_val_if_fail (state != NULL, NULL);\n");
		output ("\tg_return_val_if_fail (kids != NULL, NULL);\n\n");

	} else {
		output ("MBTREE_TYPE **\n");
		output ("mono_burg_kids (MBTREE_TYPE *tree, int rulenr, MBTREE_TYPE *kids [])\n{\n");
		output ("\tg_return_val_if_fail (tree != NULL, NULL);\n");
		output ("\tg_return_val_if_fail (kids != NULL, NULL);\n\n");
	}

	output ("\tswitch (rulenr) {\n");

	n = g_list_length (rule_list);
	sa = g_new0 (char *, n);
	si = g_new0 (int, n);

	/* compress the case statement */
	for (l = rule_list, i = 0, c = 0; l; l = l->next) {
		Rule *rule = (Rule *)l->data;
		int kn = 0;
		char *k;

		if (dag_mode)
			k = compute_kids ("state", rule->tree, &kn);
		else
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
	GHashTable *cache = g_hash_table_new (g_str_hash, g_str_equal);

	for (l =  rule_list, i = 0; l; l = l->next) {
		Rule *rule = (Rule *)l->data;
		
		if (rule->code) {
			GList *cases;
			if ((cases = g_hash_table_lookup (cache, rule->code))) {
				cases = g_list_append (cases, GINT_TO_POINTER (i));
			} else {
				cases = g_list_append (NULL, GINT_TO_POINTER (i));
			}
			g_hash_table_insert (cache, rule->code, cases);
		}
		i++;
	}

	output ("void mono_burg_emit (int ern, MBState *state, MBTREE_TYPE *tree, MBCGEN_TYPE *s)\n {\n");
	output ("\tswitch (ern) {");
	for (l =  rule_list, i = 0; l; l = l->next) {
		Rule *rule = (Rule *)l->data;

		if (rule->code) {
			GList *cases, *tmp;
			cases = g_hash_table_lookup (cache, rule->code);
			if (cases && i != GPOINTER_TO_INT (cases->data)) {
				i++;
				continue;
			}
			emit_rule_string (rule, "");
			for (tmp = cases; tmp; tmp = tmp->next) {
				output ("\tcase %d:\n", GPOINTER_TO_INT (tmp->data) + 1);
			}
			output ("\t{\n");
			output ("%s\n", rule->code);
			output ("\t}\n\treturn;\n");
		}
		i++;
	}
	output ("\t}\n}\n\n");
	g_hash_table_destroy (cache);
}

static void
emit_cost_func ()
{
	GList *l;
	int i;

	for (l =  rule_list, i = 0; l; l = l->next) {
		Rule *rule = (Rule *)l->data;
		
		if (rule->cfunc) {
			output ("inline static guint16\n");

			emit_rule_string (rule, "");
			
			if (dag_mode)
				output ("mono_burg_cost_%d (MBState *state, MBCOST_DATA *data)\n", i + 1);
			else
				output ("mono_burg_cost_%d (MBTREE_TYPE *tree, MBCOST_DATA *data)\n", i + 1);
			output ("{\n");
			output ("%s\n", rule->cfunc);
			output ("}\n\n");
		}
		i++;
	}
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

static int
count_nonterms (Tree *tree)
{
	if (!tree)
		return 0;

	if (tree->nonterm) {
		return 1;
	} else {
		return count_nonterms (tree->left) + count_nonterms (tree->right);
	} 
}

static void
emit_vardefs ()
{
	GList *l;
	int i, j, c, n, *si;
	char **sa;
	int *nts_offsets;
	int current_offset;

	output ("\n");
	if (predefined_terms) {
		output ("#if HAVE_ARRAY_ELEM_INIT\n");
		output ("const guint8 mono_burg_arity [MBMAX_OPCODES] = {\n"); 
		for (l = term_list, i = 0; l; l = l->next) {
			Term *t = (Term *)l->data;
			output ("\t [%s] = %d, /* %s */\n", t->name, t->arity, t->name);
		}
		output ("};\n\n");
		output ("void\nmono_burg_init (void) {\n");
		output ("}\n\n");
		output ("#else\n");
		output ("guint8 mono_burg_arity [MBMAX_OPCODES];\n"); 

		output ("void\nmono_burg_init (void)\n{\n");

		for (l = term_list, i = 0; l; l = l->next) {
			Term *t = (Term *)l->data;
			output ("\tmono_burg_arity [%s] = %d; /* %s */\n", t->name, t->arity, t->name);
		}
		output ("}\n\n");
		output ("#endif /* HAVE_ARRAY_ELEM_INIT */\n");

	} else {
		output ("const guint8 mono_burg_arity [] = {\n"); 
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

		output ("const char *const mono_burg_term_string [] = {\n");
		output ("\tNULL,\n");
		for (l = term_list, i = 0; l; l = l->next) {
			Term *t = (Term *)l->data;
			output ("\t\"%s\",\n", t->name);
		}
		output ("};\n\n");
	}

	output ("#if MONOBURG_LOG\n");
	output ("const char * const mono_burg_rule_string [] = {\n");
	output ("\tNULL,\n");
	for (l = rule_list, i = 0; l; l = l->next) {
		Rule *rule = (Rule *)l->data;
		output ("\t\"%s: ", rule->lhs->name);
		emit_tree_string (rule->tree);
		output ("\",\n");
	}
	output ("};\n");
	output ("#endif /* MONOBURG_LOG */\n\n");

	n = g_list_length (rule_list);
	sa = g_new0 (char *, n);
	si = g_new0 (int, n);
	nts_offsets = g_new0 (int, n);

	/* at offset 0 we store 0 to mean end of list */
	current_offset = 1;
	output ("const guint16 mono_burg_nts_data [] = {\n\t0,\n");
	/* compress the _nts array */
	for (l = rule_list, i = 0, c = 0; l; l = l->next) {
		Rule *rule = (Rule *)l->data;
		char *s = compute_nonterms (rule->tree);

		for (j = 0; j < c; j++)
			if (!strcmp (sa [j], s))
				break;

		si [i++] = j;
		if (j == c) {
			output ("\t%s0,\n", s);
			nts_offsets [c] = current_offset;
			sa [c++] = s;
			current_offset += count_nonterms (rule->tree) + 1;
		}
	}	
	output ("\t0\n};\n\n");

	output ("const guint8 mono_burg_nts [] = {\n");
	output ("\t0,\n");
	for (l = rule_list, i = 0; l; l = l->next) {
		Rule *rule = (Rule *)l->data;
		output ("\t%d, /* %s */ ", nts_offsets [si [i]], sa [si [i]]);
		++i;
		emit_rule_string (rule, "");
	}
	output ("};\n\n");
}

static void
emit_prototypes ()
{
	output ("extern const char * const mono_burg_term_string [];\n");
	output ("#if MONOBURG_LOG\n");
	output ("extern const char * const mono_burg_rule_string [];\n");
	output ("#endif /* MONOBURG_LOG */\n");
	output ("extern const guint16 mono_burg_nts_data [];\n");
	output ("extern const guint8 mono_burg_nts [];\n");
	output ("extern void mono_burg_emit (int ern, MBState *state, MBTREE_TYPE *tree, MBCGEN_TYPE *s);\n\n");

	output ("MBState *mono_burg_label (MBTREE_TYPE *tree, MBCOST_DATA *data);\n");
	output ("int mono_burg_rule (MBState *state, int goal);\n");

	if (dag_mode)
		output ("MBState **mono_burg_kids (MBState *state, int rulenr, MBState *kids []);\n");
	else
		output ("MBTREE_TYPE **mono_burg_kids (MBTREE_TYPE *tree, int rulenr, MBTREE_TYPE *kids []);\n");

	output ("extern void mono_burg_init (void);\n");
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
			g_warning ("unreachable nonterm \"%s\"", n->name);
	}
}

static void
usage ()
{
	fprintf (stderr,
		 "Usage is: monoburg -d file.h -s file.c [inputfile] \n");
	exit (1);
}

static void
warning_handler (const gchar *log_domain,
		 GLogLevelFlags log_level,
		 const gchar *message,
		 gpointer user_data)
{
	(void) fprintf ((FILE *) user_data, "** WARNING **: %s\n", message);
}

int
main (int argc, char *argv [])
{
	char *cfile = NULL;
	char *deffile = NULL;
	GList *infiles = NULL;
	int i;

        definedvars = g_hash_table_new (g_str_hash, g_str_equal);
	g_log_set_handler (NULL, G_LOG_LEVEL_WARNING, warning_handler, stderr);

	for (i = 1; i < argc; i++){
		if (argv [i][0] == '-'){
			if (argv [i][1] == 'h') {
				usage ();
			} else if (argv [i][1] == 'e') {
				dag_mode = 1;
			} else if (argv [i][1] == 'p') {
				predefined_terms = 1;
			} else if (argv [i][1] == 'd') {
				deffile = argv [++i];
			} else if (argv [i][1] == 's') {
				cfile = argv [++i];
			} else if (argv [i][1] == 'c') {
				default_cost = atoi (argv [++i]);
			} else if (argv [i][1] == 'D') {
                                g_hash_table_insert (definedvars, &argv [i][2],
                                                     GUINT_TO_POINTER (1));
			} else {
				usage ();
			}
		} else {
			infiles = g_list_append (infiles, argv [i]);
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


	if (infiles) {
		GList *l = infiles;
		while (l) {
			char *infile = (char *)l->data;
			if (!(inputfd = fopen (infile, "r"))) {
				perror ("cant open input file");
				exit (-1);
			}

			yyparse ();

			reset_parser ();

			l->data = inputfd;
			l = l->next;
		}
	} else {
		inputfd = stdin;
		yyparse ();
	}

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

		if (cfile == NULL)
			outputfd = stdout;
		else {
			if (!(cfd = fopen (cfile, "w"))) {
				perror ("cant open c output file");
				(void) remove (deffile);
				exit (-1);
			}

			outputfd = cfd;
		}

		output ("#include \"%s\"\n\n", deffile);
	}
	
	if (infiles) {
		GList *l = infiles;
		while (l) {
			inputfd = l->data;
			yyparsetail ();
			fclose (inputfd);
			l = l->next;
		}
	} else {
		yyparsetail ();
	}

	emit_vardefs ();
	emit_cost_func ();
	emit_emitter_func ();
	emit_decoders ();

	emit_closure ();
	emit_label_func ();

	emit_kids ();

	if (cfile)
		fclose (cfd);

	return 0;
}
