%{
/*
 * monoburg.y: yacc input grammer
 *
 * Author:
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#include <unistd.h>
#include <ctype.h>
#include <assert.h>
#include <stdarg.h>

#include "monoburg.h"
  
static int yylineno = 0;
static int yylinepos = 0;

%}

%union {
  char *text;
  int   ivalue;
  Tree  *tree;
}

%token <text> IDENT
%token <text> CODE
%token <text> STRING
%token  START
%token  COST
%token  TERM
%token  TERMPREFIX
%token <ivalue> INTEGER

%type   <tree>          tree
%type   <text>          optcost
%type   <text>          optcfunc
%type   <text>          optcode

%%

decls   : /* empty */ 
	| START IDENT { start_nonterm ($2); } decls
	| TERM  tlist decls
	| TERMPREFIX plist decls
	| IDENT ':' tree optcost optcode optcfunc { create_rule ($1, $3, $5, $4, $6); } decls 
	;

optcode : /* empty */ { $$ = NULL; }
	| CODE 
	;

plist	: /* empty */
	| plist IDENT { create_term_prefix ($2);}
	;

tlist	: /* empty */
	| tlist IDENT { create_term ($2, -1);}
	| tlist IDENT '=' INTEGER { create_term ($2, $4); }
	;

tree	: IDENT { $$ = create_tree ($1, NULL, NULL); }
	| IDENT '(' tree ')' { $$ = create_tree ($1, $3, NULL); }
	| IDENT '(' tree ',' tree ')' { $$ = create_tree ($1, $3, $5); }
	;

optcost : /* empty */ {$$ = NULL; }
	| STRING
	| INTEGER { $$ = g_strdup_printf ("%d", $1); }
	;

optcfunc : /*empty */ { $$ = NULL; }
	 | COST CODE { $$ = $2; }
	 ;
%%

static char input[2048];
static char *next = input;

void 
yyerror (char *fmt, ...)
{
  va_list ap;

  va_start(ap, fmt);

  fprintf (stderr, "line %d(%d): ", yylineno, yylinepos);
  vfprintf (stderr, fmt, ap);
  fprintf(stderr, "\n");

  va_end (ap);

  exit (-1);
}

static int state = 0;

void
reset_parser ()
{
  state = 0;
}

struct pplist {
  struct pplist *next;
  gboolean ignore;
};

static struct pplist *pp = NULL;

static char*
getvar (const char *input)
{
    char *var = g_strchug (g_strdup (input));
    char *ptr;

    for (ptr = var; *ptr && *ptr != '\n'; ++ptr) {
        if (g_ascii_isspace (*ptr)) {
            break;
        }
    }
    *ptr = '\0';

    return var;
}

static void
push_if (char *input, gboolean flip)
{
  struct pplist *new_pp = g_new (struct pplist, 1);
  char *var = getvar (input);

  new_pp->ignore = (g_hash_table_lookup (definedvars, var) == NULL) ^ flip;
  new_pp->next = pp;

  new_pp->ignore |= (pp ? pp->ignore : 0);
  pp = new_pp;
  g_free (var);
}

static void
flip_if ()
{
  if (!pp)
      yyerror ("%%else without %%if");

  pp->ignore = !pp->ignore | (pp->next ? pp->next->ignore : 0);
}

static void
pop_if ()
{
  struct pplist *prev_pp = pp;

  if (!pp)
      yyerror ("%%endif without %%if");

  pp = pp->next;
  g_free (prev_pp);
}

static char
nextchar ()
{
  int next_state ;
  gboolean ll;

    if (!*next) {
      next = input;
      *next = 0;
      do {
	if (!fgets (input, sizeof (input), inputfd))
	  return 0;

	ll = (input [0] == '%' && input [1] == '%');
	next_state = state;

        if (state == 1) {
          if (!ll && input [0] == '%') {
            if (!strncmp (&input [1], "ifdef", 5)) {
              push_if (&input [6], FALSE);
              ll = TRUE;
              continue;
            }
            else if (!strncmp (&input [1], "ifndef", 6)) {
              push_if (&input [7], TRUE);
              ll = TRUE;
              continue;
            }
            else if (!strncmp (&input [1], "else", 4)) {
              flip_if ();
              ll = TRUE;
              continue;
            }
            else if (!strncmp (&input [1], "endif", 5)) {
              pop_if ();
              ll = TRUE;
              continue;
            }
          }
          if (pp && pp->ignore) {
            ll = TRUE;
            continue;
          }
        }

	switch (state) {
	case 0:
	  if (ll) {
	    next_state = 1;
	  } else 
	    fputs (input, outputfd);
	  break;
	case 1:
	  if (ll) {
	    next_state = 2;
	    *next = 0;
	  }
	  break;
	default:
	  return 0;
	}
	ll = state != 1 || input[0] == '#';
	state = next_state;
	yylineno++;
      } while (next_state == 2 || ll);
    } 

    return *next++;
}

void
yyparsetail (void)
{
  fputs (input, outputfd);
  while (fgets (input, sizeof (input), inputfd))
    fputs (input, outputfd);
}

int 
yylex (void) 
{
  char c;

  do {

    if (!(c = nextchar ()))
      return 0;

    yylinepos = next - input + 1;

    if (isspace (c))
      continue;

    if (c == '%') {
      if (!strncmp (next, "start", 5) && isspace (next[5])) {
	next += 5;
	return START;
      }

      if (!strncmp (next, "termprefix", 10) && isspace (next[10])) {
	next += 10;
	return TERMPREFIX;
      }

      if (!strncmp (next, "term", 4) && isspace (next[4])) {
	next += 4;
	return TERM;
      }
      return c;
    }

    if (isdigit (c)) {
	    int num = 0;

	    do {
		    num = 10*num + (c - '0');
	    } while (isdigit (c = (*next++)));

	    yylval.ivalue = num;
	    return INTEGER;
    }

    if (isalpha (c)) {
      char *n = next;
      int l;

      if (!strncmp (next - 1, "cost", 4) && isspace (next[3])) {
	next += 4;
	return COST;
      }

      while (isalpha (*n) || isdigit (*n) || *n == '_') 
	      n++;

      l = n - next + 1;
      yylval.text = g_strndup (next - 1, l);
      next = n;
      return IDENT;
    }
    
    if (c == '"') {
      int i = 0;
      static char buf [100000];
 
      while ((c = *next++) != '"' && c)
	buf [i++] = c;
      
      buf [i] = '\0';
      yylval.text = g_strdup (buf);

      return STRING;
    }

    if (c == '{') {
      int i = 0, d = 1;
      static char buf [100000];
 
      while (d && (c = nextchar ())) {
	buf [i++] = c;
	assert (i < sizeof (buf));

	switch (c) {
	case '{': d++; break;
	case '}': d--; break;
	default:
		break;
	}
      }
      buf [--i] = '\0';
      yylval.text = g_strdup (buf);

      return CODE;
    }
    
    return c;
  
  } while (1);
}

