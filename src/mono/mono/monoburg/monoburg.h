#ifndef __MONO_MONOBURG_H__
#define __MONO_MONOBURG_H__

#include <glib.h>

void yyerror (char *fmt, ...);
int  yylex   (void);

extern FILE *inputfd;
extern FILE *outputfd;

typedef struct _Rule Rule;

typedef struct _Term Term;
struct _Term{
	char *name;
	int number;
	int arity;
	GList *rules; /* rules that start with this terminal */
};

typedef struct _NonTerm NonTerm;

struct _NonTerm {
	char *name;
	int number;
	GList *rules; /* rules with this nonterm on the left side */
	GList *chain;
	gboolean reached;
};

typedef struct _Tree Tree;

struct _Tree {
	Term *op;
	Tree *left;
	Tree *right;
	NonTerm *nonterm; /* used by chain rules */
};

struct _Rule {
	NonTerm *lhs;
	Tree *tree;
	char *code;
	char *cost;
	char *cfunc;
};


Tree    *create_tree    (char *id, Tree *left, Tree *right);

void     create_term    (char *id, int num);

NonTerm *nonterm        (char *id);

void     start_nonterm  (char *id);

void     create_rule    (char *id, Tree *tree, char *code, char *cost, 
			 char *cfunc);

void     yyparsetail    (void);

#endif
