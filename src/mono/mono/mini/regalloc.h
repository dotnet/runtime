
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

/* make this arch-dependent */
#define MONO_MAX_IREGS 8
#define MONO_MAX_FREGS 7

typedef struct {
	/* symbolic registers */
	int next_vireg;
	int next_vfreg;

	/* hard registers */
	int num_iregs;
	int num_fregs;

	guint32 ifree_mask;
	guint32 ffree_mask;

	/* symbolic -> hard register assignment */
	char *iassign;
	char *fassign;

	/* hard -> symbolic */
	int isymbolic [MONO_MAX_IREGS];
	int fsymbolic [MONO_MAX_FREGS];

	int ispills;
} MonoRegState;

#define mono_regstate_next_int(rs)   ((rs)->next_vireg++)
#define mono_regstate_next_float(rs) ((rs)->next_vfreg++)


MonoRegState* mono_regstate_new (void);

void          mono_regstate_free      (MonoRegState *rs);
void          mono_regstate_reset     (MonoRegState *rs);
void          mono_regstate_assign    (MonoRegState *rs);
int           mono_regstate_alloc_int (MonoRegState *rs, guint32 allow);
void          mono_regstate_free_int  (MonoRegState *rs, int reg);
inline int    mono_regstate_next_long (MonoRegState *rs);

