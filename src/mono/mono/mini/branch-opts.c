/*
 * branch-opts.c: Branch optimizations support 
 *
 * Authors:
 *   Patrik Torstensson (Patrik.Torstesson at gmail.com)
 *
 * (C) 2005 Ximian, Inc.  http://www.ximian.com
 */
 #include "mini.h"
 
 /*
 * Used by the arch code to replace the exception handling
 * with a direct branch. This is safe to do if the 
 * exception object isn't used, no rethrow statement and
 * no filter statement (verify).
 *
 */
MonoInst *
mono_branch_optimize_exception_target (MonoCompile *cfg, MonoBasicBlock *bb, char * exname)
{
	MonoMethod *method = cfg->method;
	MonoMethodHeader *header = mono_method_get_header (method);
	MonoExceptionClause *clause;
	MonoClass *exclass;
	int i;

	if (!(cfg->opt & MONO_OPT_EXCEPTION))
		return NULL;

	if (bb->region == -1 || !MONO_BBLOCK_IS_IN_REGION (bb, MONO_REGION_TRY))
		return NULL;

	exclass = mono_class_from_name (mono_get_corlib (), "System", exname);
	/* search for the handler */
	for (i = 0; i < header->num_clauses; ++i) {
		clause = &header->clauses [i];
		if (MONO_OFFSET_IN_CLAUSE (clause, bb->real_offset)) {
			if (exclass == clause->data.catch_class) {
				MonoBasicBlock *tbb;

				/* get the basic block for the handler and 
				 * check if the exception object is used.
				 * Flag is set during method_to_ir due to 
				 * pop-op is optmized away in codegen (burg).
				 */
				tbb = g_hash_table_lookup (cfg->bb_hash, header->code + clause->handler_offset);
				if (tbb && tbb->flags & BB_EXCEPTION_DEAD_OBJ && !(tbb->flags & BB_EXCEPTION_UNSAFE)) {
					MonoBasicBlock *targetbb = tbb;
					gboolean unsafe = FALSE;

					/* Check if this catch clause is ok to optmize by
					 * looking for the BB_EXCEPTION_UNSAFE in every BB that
					 * belongs to the same region. 
					 *
					 * UNSAFE flag is set during method_to_ir (OP_RETHROW)
					 */
					while (!unsafe && tbb->next_bb && tbb->region == tbb->next_bb->region) {
						if (tbb->next_bb->flags & BB_EXCEPTION_UNSAFE)  {
							unsafe = TRUE;
							break;
						}
						tbb = tbb->next_bb;
					}

					if (!unsafe) {
						MonoInst *jump;

						/* Create dummy inst to allow easier integration in
						 * arch dependent code (opcode ignored)
						 */
						MONO_INST_NEW (cfg, jump, CEE_BR);

						/* Allocate memory for our branch target */
						jump->inst_i1 = mono_mempool_alloc0 (cfg->mempool, sizeof (MonoInst));
						jump->inst_true_bb = targetbb;

						if (cfg->verbose_level > 2) 
							g_print ("found exception to optimize - returning branch to BB%d (%s) (instead of throw) for method %s:%s\n", targetbb->block_num, clause->data.catch_class->name, cfg->method->klass->name, cfg->method->name);

						return jump;
					} 

					return NULL;
				}
			}
		}
	}

	return NULL;
}

