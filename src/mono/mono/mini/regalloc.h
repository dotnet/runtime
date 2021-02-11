/**
 * \file
 */

typedef size_t regmask_t;

enum {
	MONO_REG_INT,
	MONO_REG_DOUBLE,
	/* This is used only for allocating spill slots with GC tracking */
	MONO_REG_INT_REF,
	/* Ditto for managed pointers */
	MONO_REG_INT_MP,
	MONO_REG_SIMD
};

 
#ifdef MONO_ARCH_NEED_SIMD_BANK
#define MONO_NUM_REGBANKS 5
#else
#define MONO_NUM_REGBANKS 4
#endif

typedef struct {
	/* symbolic registers */
	int next_vreg;

	regmask_t ifree_mask;
	regmask_t free_mask [MONO_NUM_REGBANKS];

	/* symbolic -> hard register assignment */
	/* 
	 * If the register is spilled, then this contains -spill - 1, where 'spill'
	 * is the index of the spill variable.
	 */
	gint32 *vassign;

	/* hard -> symbolic */
	int isymbolic [MONO_MAX_IREGS];
	int fsymbolic [MONO_MAX_FREGS];
#ifdef MONO_ARCH_NEED_SIMD_BANK
	int xsymbolic [MONO_MAX_XREGS];
#endif
	/* Points to the arrays above */
	int *symbolic [MONO_NUM_REGBANKS];

	int vassign_size;
} MonoRegState;

MonoRegState* mono_regstate_new (void);

void          mono_regstate_free      (MonoRegState *rs);
