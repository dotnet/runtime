/**
 * \file
 */

#ifndef __MINI_ARM64_GSHAREDVT_H__
#define __MINI_ARM64_GSHAREDVT_H__

/* Argument marshallings for calls between gsharedvt and normal code */
typedef enum {
	GSHAREDVT_ARG_NONE = 0,
	GSHAREDVT_ARG_BYVAL_TO_BYREF = 1,
	GSHAREDVT_ARG_BYVAL_TO_BYREF_HFAR4 = 2,
	GSHAREDVT_ARG_BYREF_TO_BYVAL = 3,
	GSHAREDVT_ARG_BYREF_TO_BYVAL_HFAR4 = 4,
	GSHAREDVT_ARG_BYREF_TO_BYREF = 5
} GSharedVtArgMarshal;

/* For arguments passed on the stack on ios */
typedef enum {
	GSHAREDVT_ARG_SIZE_NONE = 0,
	GSHAREDVT_ARG_SIZE_I1 = 1,
	GSHAREDVT_ARG_SIZE_U1 = 2,
	GSHAREDVT_ARG_SIZE_I2 = 3,
	GSHAREDVT_ARG_SIZE_U2 = 4,
	GSHAREDVT_ARG_SIZE_I4 = 5,
	GSHAREDVT_ARG_SIZE_U4 = 6,
} GSharedVtArgSize;

/* Return value marshalling for calls between gsharedvt and normal code */
typedef enum {
	GSHAREDVT_RET_NONE = 0,
	GSHAREDVT_RET_I8 = 1,
	GSHAREDVT_RET_I1 = 2,
	GSHAREDVT_RET_U1 = 3,
	GSHAREDVT_RET_I2 = 4,
	GSHAREDVT_RET_U2 = 5,
	GSHAREDVT_RET_I4 = 6,
	GSHAREDVT_RET_U4 = 7,
	GSHAREDVT_RET_R8 = 8,
	GSHAREDVT_RET_R4 = 9,
	GSHAREDVT_RET_IREGS_1 = 10,
	GSHAREDVT_RET_IREGS_2 = 11,
	GSHAREDVT_RET_IREGS_3 = 12,
	GSHAREDVT_RET_IREGS_4 = 13,
	GSHAREDVT_RET_IREGS_5 = 14,
	GSHAREDVT_RET_IREGS_6 = 15,
	GSHAREDVT_RET_IREGS_7 = 16,
	GSHAREDVT_RET_IREGS_8 = 17,
	GSHAREDVT_RET_HFAR8_1 = 18,
	GSHAREDVT_RET_HFAR8_2 = 19,
	GSHAREDVT_RET_HFAR8_3 = 20,
	GSHAREDVT_RET_HFAR8_4 = 21,
	GSHAREDVT_RET_HFAR4_1 = 22,
	GSHAREDVT_RET_HFAR4_2 = 23,
	GSHAREDVT_RET_HFAR4_3 = 24,
	GSHAREDVT_RET_HFAR4_4 = 25,
	GSHAREDVT_RET_NUM = 26
} GSharedVtRetMarshal;

typedef struct {
	/* Method address to call */
	gpointer addr;
	/* The trampoline reads this, so keep the size explicit */
	int ret_marshal;
	/* If ret_marshal != NONE, this is the reg of the vret arg, else -1 */
	/* Equivalent of vret_arg_slot in x86 implementation. */
	int vret_arg_reg;
	/* The stack slot where the return value will be stored */
	int vret_slot;
	int stack_usage, map_count;
	/* If not -1, then make a virtual call using this vtable offset */
	int vcall_offset;
	/* If 1, make an indirect call to the address in the rgctx reg */
	int calli;
	/* Whenever this is a in or an out call */
	int gsharedvt_in;
	/* Maps stack slots/registers in the caller to the stack slots/registers in the callee */
	int map [MONO_ZERO_LEN_ARRAY];
} GSharedVtCallInfo;

/* Number of argument registers (r0..r8) */
#define NUM_GSHAREDVT_ARG_GREGS 9
#define NUM_GSHAREDVT_ARG_FREGS 8

gpointer
mono_arm_start_gsharedvt_call (GSharedVtCallInfo *info, gpointer *caller, gpointer *callee, gpointer mrgctx_reg);

#endif /* __MINI_ARM64_GSHAREDVT_H__ */
