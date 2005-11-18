/*
 * aliasing.h: Alias Analysis
 *
 * Author:
 *   Massimiliano Mantione (massi@ximian.com)
 *
 * (C) 2005 Novell, Inc.  http://www.novell.com
 */

#ifndef __MONO_ALIASING_H__
#define __MONO_ALIASING_H__

#include "mini.h"

#define MONO_ALIASING_INVALID_VARIABLE_INDEX (-1)

/*
 * A struct representing the element of a list of local variables.
 */
typedef struct MonoLocalVariableList {
	/* The index of the local variable */
	int variable_index;
	
	/* Next in the list */
	struct MonoLocalVariableList *next;
} MonoLocalVariableList;

/*
 * A struct representing the information about the fact that an address
 * that could be an alias is used.
 */
typedef struct MonoAliasUsageInformation {
	/* The inst where the address is used */
	MonoInst *inst;
	
	/* The possibly aliased variables. Note that if this field is null */
	/* it means that any "problematic" variable could be aliased! */
	MonoLocalVariableList *affected_variables;
	
	/* Next in the list */
	struct MonoAliasUsageInformation *next;
} MonoAliasUsageInformation;


/*
 * All the different kinds of locations for a variable's address.
 * "ANY" means the location is unknown or cannot be handled.
 */
typedef enum {
	MONO_ALIASING_TYPE_ANY,
	MONO_ALIASING_TYPE_NO_ALIAS,
	MONO_ALIASING_TYPE_LOCAL,
	MONO_ALIASING_TYPE_LOCAL_FIELD
} MonoAliasType;

typedef struct MonoAliasValue {
	MonoAliasType type;
	int variable_index;
} MonoAliasValue;

/*
 * A struct representing the aliasing information in a BB.
 */
typedef struct MonoAliasingInformationInBB {
	/* The BB to which these info are relaed. */
	MonoBasicBlock *bb;
	
	/* Info on alias usage. */
	MonoAliasUsageInformation *potential_alias_uses;
} MonoAliasingInformationInBB;

/*
 * A struct representing the aliasing information in a MonoCompile.
 */
typedef struct MonoAliasingInformation {
	/* The MonoCompile to which these info are relaed. */
	MonoCompile *cfg;
	
	/* The pool from where everything is allocated (including this struct). */
	MonoMemPool *mempool;
	
	/* Aliasing info for each BB */
	MonoAliasingInformationInBB *bb;
	
	/* The variables whose address has been taken and "lost": any pointer */
	/* with an unknown value potentially aliases these variables. */
	MonoLocalVariableList *uncontrollably_aliased_variables;
	
	/* Used to track the current inst when traversing inst trees in a BB. */
	MonoAliasUsageInformation *next_interesting_inst;
	
	/* Array containing one MonoLocalVariableList for each local variable. */
	MonoLocalVariableList *variables;
	
	/* Array containing one flag for each local variable stating if it is */
	/* already in the uncontrollably_aliased_variables list. */
	gboolean *variable_is_uncontrollably_aliased;
	
	/* This MonoLocalVariableList is a placeholder for uncontrollably_aliased_variables */
	/* while the aliasing info are still being collected. */
	MonoLocalVariableList *temporary_uncontrollably_aliased_variables;
	
	/* An array of MonoInst* containing all the arguments to the next call. */
	MonoInst **arguments;
	/* An array of MonoAliasValue containing all the aliases passed to the arguments of the next call. */
	MonoAliasValue *arguments_aliases;
	/* The total capacity of the "arguments" and "arguments_aliases" arrays. */
	int arguments_capacity;
	/* The number of used elements in the "arguments" and "arguments_aliases" arrays. */
	int number_of_arguments;
} MonoAliasingInformation;

extern MonoAliasingInformation*
mono_build_aliasing_information (MonoCompile *cfg);
extern void
mono_destroy_aliasing_information (MonoAliasingInformation *info);
extern void
mono_aliasing_initialize_code_traversal (MonoAliasingInformation *info, MonoBasicBlock *bb);
extern MonoLocalVariableList*
mono_aliasing_get_affected_variables_for_inst_traversing_code (MonoAliasingInformation *info, MonoInst *inst);
extern MonoLocalVariableList*
mono_aliasing_get_affected_variables_for_inst_in_bb (MonoAliasingInformation *info, MonoInst *inst, MonoBasicBlock *bb);
extern MonoLocalVariableList*
mono_aliasing_get_affected_variables_for_inst (MonoAliasingInformation *info, MonoInst *inst);

extern void
mono_aliasing_deadce (MonoAliasingInformation *info);

#endif /* __MONO_ALIASING_H__ */
