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
%token  TERM
%token <ivalue> INTEGER

%type   <tree>          tree
%type   <text>          optcost
%type   <text>          optcode

%%

decls   : /* empty */ 
	| START IDENT { start_nonterm ($2); } decls
	| TERM  tlist decls
	| IDENT ':' tree optcost optcode { create_rule ($1, $3, $5, $4); } decls 
	;

optcode : /* empty */ { $$ = NULL }
	| CODE 
	;

tlist	: /* empty */
	| tlist IDENT { create_term ($2, -1);}
	| tlist IDENT '=' INTEGER { create_term ($2, $4); }
	;

tree	: IDENT { $$ = create_tree ($1, NULL, NULL); }
	| IDENT '(' tree ')' { $$ = create_tree ($1, $3, NULL); }
	| IDENT '(' tree ',' tree ')' { $$ = create_tree ($1, $3, $5); }
	;

optcost : /* empty */ {$$ = "0"; }
	| STRING
	| INTEGER { $$ = g_strdup_printf ("%d", $1); }
	;

%%

static char *
strndup (const char *s, int n)
{
  char *ns = malloc (n + 1);
  strncpy (ns, s, n);
  ns [n + 1] = '\0';
  return ns;
}

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

static char
nextchar ()
{
  static int state = 0;
  int next_state ;
  gboolean ll;

    if (!*next) {
      next = input;
      *next = 0;
      do {
	if (!fgets (input, sizeof (input), inputfd))
	  return 0;

	ll = (input [0] == '%' && input [1] == '%' && isspace (input [2]));
	next_state = state;

	switch (state) {
	case 0:
	  if (ll) {
	    next_state = 1;
	  } else 
	    fputs (input, stdout);
	  break;
	case 1:
	  if (ll)
	    next_state = 2;
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
  fputs (input, stdout);
  while (fgets (input, sizeof (input), inputfd))
    fputs (input, stdout);
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
	    } while ((c = isdigit (*next++)));

	    yylval.ivalue = num;
	    return INTEGER;
    }

    if (isalpha (c)) {
      char *n = next;
      int l;

      //if (!strncmp (next - 1, "cost", 4) && isspace (next[3])) {
      //next += 4;
      //return COST;
      //}

      while (isalpha (*n) || isdigit (*n) || (*n == '_')) 
	      n++;

      l = n - next + 1;
      yylval.text = strndup (next - 1, l);
      next += l - 1;

      return IDENT;
    }
    
    if (c == '"') {
      int i = 0;
      static char buf [100000];
 
      while ((c = *next++) != '"' && c)
	buf [i++] = c;
      
      buf [--i] = '\0';
      yylval.text = strdup (buf);

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
	}
      }
      buf [--i] = '\0';
      yylval.text = strdup (buf);

      return CODE;
    }
    
    return c;
  
  } while (1);
}

