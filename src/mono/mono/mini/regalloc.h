
typedef size_t regmask_t;

enum {
	MONO_REG_FREE,
	MONO_REG_FREEABLE,
	MONO_REG_MOVEABLE,
	MONO_REG_BUSY,
	MONO_REG_RESERVED
};

enum {
	MONO_REG_INT,
	MONO_REG_DOUBLE
};

typedef struct {
	/* symbolic registers */
	int next_vireg;
	int next_vfreg;

	/* hard registers */
	int num_iregs;
	int num_fregs;

	regmask_t ifree_mask;
	regmask_t ffree_mask;

	/* symbolic -> hard register assignment */
	/* 
	 * If the register is spilled, then this contains -spill - 1, where 'spill'
	 * is the index of the spill variable.
	 */
	int *iassign;
	int *fassign;

	/* hard -> symbolic */
	int isymbolic [MONO_MAX_IREGS];
	int fsymbolic [MONO_MAX_FREGS];

	int max_ireg;
	int ispills;
} MonoRegState;

#define mono_regstate_next_int(rs)   ((rs)->next_vireg++)
#define mono_regstate_next_float(rs) ((rs)->next_vfreg++)


MonoRegState* mono_regstate_new (void);

void          mono_regstate_free      (MonoRegState *rs);
void          mono_regstate_reset     (MonoRegState *rs);
void          mono_regstate_assign    (MonoRegState *rs);
int           mono_regstate_alloc_int   (MonoRegState *rs, regmask_t allow);
void          mono_regstate_free_int  (MonoRegState *rs, int reg);
int           mono_regstate_alloc_float (MonoRegState *rs, regmask_t allow);
void          mono_regstate_free_float  (MonoRegState *rs, int reg);
inline int    mono_regstate_next_long (MonoRegState *rs);

