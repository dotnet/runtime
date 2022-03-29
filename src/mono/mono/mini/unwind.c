/**
 * \file
 * Stack Unwinding Interface
 *
 * Authors:
 *   Zoltan Varga (vargaz@gmail.com)
 *
 * (C) 2008 Novell, Inc.
 */

#include "mini.h"
#include "mini-unwind.h"

#include <mono/utils/mono-counters.h>
#include <mono/utils/freebsd-dwarf.h>
#include <mono/utils/hazard-pointer.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/metadata/threads-types.h>
#include <mono/metadata/mono-endian.h>

typedef enum {
	LOC_SAME,
	LOC_OFFSET
} LocType;

typedef struct {
	LocType loc_type;
	int offset;
} Loc;

typedef struct {
	guint32 len;
	guint8 *info;
} MonoUnwindInfo;

static mono_mutex_t unwind_mutex;

static MonoUnwindInfo *cached_info;
static int cached_info_next, cached_info_size;
static GSList *cached_info_list;
static GHashTable *cached_info_ht;
/* Statistics */
static int unwind_info_size;

#define unwind_lock() mono_os_mutex_lock (&unwind_mutex)
#define unwind_unlock() mono_os_mutex_unlock (&unwind_mutex)

#ifdef TARGET_AMD64
static int map_hw_reg_to_dwarf_reg [] = { 0, 2, 1, 3, 7, 6, 4, 5, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
#define NUM_DWARF_REGS AMD64_NREG
#define DWARF_DATA_ALIGN (-8)
#define DWARF_PC_REG (mono_hw_reg_to_dwarf_reg (AMD64_RIP))
#elif defined(TARGET_ARM)
// http://infocenter.arm.com/help/topic/com.arm.doc.ihi0040a/IHI0040A_aadwarf.pdf
/* Assign d8..d15 to hregs 16..24 (dwarf regs 264..271) */
static int map_hw_reg_to_dwarf_reg [] = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 264, 265, 266, 267, 268, 269, 270, 271 };
#define NUM_DWARF_REGS 272
#define DWARF_DATA_ALIGN (-4)
#define DWARF_PC_REG (mono_hw_reg_to_dwarf_reg (ARMREG_LR))
#define IS_DOUBLE_REG(dwarf_reg) (((dwarf_reg) >= 264) && ((dwarf_reg) <= 271))
#elif defined(TARGET_ARM64)
#define NUM_DWARF_REGS 96
#define DWARF_DATA_ALIGN (-8)
/* LR */
#define DWARF_PC_REG 30
static int map_hw_reg_to_dwarf_reg [] = {
	0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
	16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31,
	/* v8..v15 */
	72, 73, 74, 75, 76, 77, 78, 79,
};
#elif defined (TARGET_X86)
/*
 * ebp and esp are swapped:
 * http://lists.cs.uiuc.edu/pipermail/lldb-dev/2014-January/003101.html
 */
static int map_hw_reg_to_dwarf_reg [] = { 0, 1, 2, 3, 5, 4, 6, 7, 8 };
/* + 1 is for IP */
#define NUM_DWARF_REGS (X86_NREG + 1)
#define DWARF_DATA_ALIGN (-4)
#define DWARF_PC_REG (mono_hw_reg_to_dwarf_reg (X86_NREG))
#elif defined (TARGET_POWERPC)
// http://refspecs.linuxfoundation.org/ELF/ppc64/PPC-elf64abi-1.9.html
static int map_hw_reg_to_dwarf_reg [ppc_lr + 1] = { 0, 1, 2, 3, 4, 5, 6, 7, 8,
										  9, 10, 11, 12, 13, 14, 15, 16,
										  17, 18, 19, 20, 21, 22, 23, 24,
										  25, 26, 27, 28, 29, 30, 31 };
#define DWARF_DATA_ALIGN (-(gint32)sizeof (target_mgreg_t))
#if _CALL_ELF == 2
#define DWARF_PC_REG 65
#else
#define DWARF_PC_REG 108
#endif
#define NUM_DWARF_REGS (DWARF_PC_REG + 1)
#elif defined (TARGET_S390X)
/*
 * 0-15 = GR0-15
 * 16-31 = FP0-15 (f0, f2, f4, f6, f1, f3, f5, f7, f8, f10, f12, f14, f9, f11, f13, f15)
 */
static int map_hw_reg_to_dwarf_reg [] = {  0,  1,  2,  3,  4,  5,  6,  7,
					   8,  9, 10, 11, 12, 13, 14, 15,
					  16, 20, 17, 21, 18, 22, 19, 23,
					  24, 28, 25, 29, 26, 30, 27, 31};

#define NUM_DWARF_REGS 32
#define DWARF_DATA_ALIGN (-8)
#define DWARF_PC_REG (mono_hw_reg_to_dwarf_reg (14))
#elif defined(TARGET_RISCV)

/*
 * These values have not currently been formalized in the RISC-V psABI. See
 * instead gcc/config/riscv/riscv.h in the GCC source tree.
 */

#define NUM_DWARF_REGS (RISCV_N_GREGS + RISCV_N_FREGS)
#define DWARF_DATA_ALIGN (-4)
#define DWARF_PC_REG (mono_hw_reg_to_dwarf_reg (RISCV_RA))

static int map_hw_reg_to_dwarf_reg [NUM_DWARF_REGS] = {
	// x0..x31
	0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
	16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31,
	// f0..f31
	32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47,
	48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63,
};

#else
static int map_hw_reg_to_dwarf_reg [16];
#define NUM_DWARF_REGS 16
#define DWARF_DATA_ALIGN 1
#define DWARF_PC_REG -1
#endif

#define NUM_HW_REGS (sizeof (map_hw_reg_to_dwarf_reg) / sizeof (int))

#ifndef IS_DOUBLE_REG
#define IS_DOUBLE_REG(dwarf_reg) (dwarf_reg ? 0 : 0)
#endif

static gboolean dwarf_reg_to_hw_reg_inited;
static gboolean hw_reg_to_dwarf_reg_inited;

static int map_dwarf_reg_to_hw_reg [NUM_DWARF_REGS];

static void
init_hw_reg_map (void)
{
#ifdef TARGET_POWERPC
	map_hw_reg_to_dwarf_reg [ppc_lr] = DWARF_PC_REG;
#endif
	mono_memory_barrier ();
	hw_reg_to_dwarf_reg_inited = TRUE;
}

/*
 * mono_hw_reg_to_dwarf_reg:
 *
 *   Map the hardware register number REG to the register number used by DWARF.
 */
int
mono_hw_reg_to_dwarf_reg (int reg)
{
	if (!hw_reg_to_dwarf_reg_inited)
		init_hw_reg_map ();

MONO_DISABLE_WARNING(4127) /* conditional expression is constant */
	if (NUM_HW_REGS == 0) {
		g_assert_not_reached ();
		return -1;
	}
MONO_RESTORE_WARNING

	return map_hw_reg_to_dwarf_reg [reg];
}

static void
init_dwarf_reg_map (void)
{
	g_assert (NUM_HW_REGS > 0);
	for (int i = 0; i < NUM_HW_REGS; ++i) {
		map_dwarf_reg_to_hw_reg [mono_hw_reg_to_dwarf_reg (i)] = i;
	}

	mono_memory_barrier ();
	dwarf_reg_to_hw_reg_inited = TRUE;
}

int
mono_dwarf_reg_to_hw_reg (int reg)
{
	if (!dwarf_reg_to_hw_reg_inited)
		init_dwarf_reg_map ();

	return map_dwarf_reg_to_hw_reg [reg];
}

static G_GNUC_UNUSED void
encode_uleb128 (guint32 value, guint8 *buf, guint8 **endbuf)
{
	guint8 *p = buf;

	do {
		guint8 b = value & 0x7f;
		value >>= 7;
		if (value != 0) /* more bytes to come */
			b |= 0x80;
		*p ++ = b;
	} while (value);

	*endbuf = p;
}

static G_GNUC_UNUSED void
encode_sleb128 (gint32 value, guint8 *buf, guint8 **endbuf)
{
	gboolean more = 1;
	gboolean negative = (value < 0);
	guint32 size = 32;
	guint8 byte;
	guint8 *p = buf;

	while (more) {
		byte = value & 0x7f;
		value >>= 7;
		/* the following is unnecessary if the
		 * implementation of >>= uses an arithmetic rather
		 * than logical shift for a signed left operand
		 */
		if (negative)
			/* sign extend */
			value |= - (1 <<(size - 7));
		/* sign bit of byte is second high order bit (0x40) */
		if ((value == 0 && !(byte & 0x40)) ||
			(value == -1 && (byte & 0x40)))
			more = 0;
		else
			byte |= 0x80;
		*p ++= byte;
	}

	*endbuf = p;
}

static guint32
decode_uleb128 (guint8 *buf, guint8 **endbuf)
{
	guint8 *p = buf;
	guint32 res = 0;
	int shift = 0;

	while (TRUE) {
		guint8 b = *p;
		p ++;

		res = res | (((int)(b & 0x7f)) << shift);
		if (!(b & 0x80))
			break;
		shift += 7;
	}

	*endbuf = p;

	return res;
}

static gint32
decode_sleb128 (guint8 *buf, guint8 **endbuf)
{
	guint8 *p = buf;
	gint32 res = 0;
	int shift = 0;

	guint8 b;

	do {
		b = *p;
		p ++;

		res = res | (((int)(b & 0x7f)) << shift);
		shift += 7;
	} while (b & 0x80);

	if (shift < 32 && (b & 0x40))
		res |= - (1 << shift);

	*endbuf = p;

	return res;
}

void
mono_print_unwind_info (guint8 *unwind_info, int unwind_info_len)
{
	guint8 *p;
	int pos, reg, offset, cfa_reg, cfa_offset;

	p = unwind_info;
	pos = 0;
	while (p < unwind_info + unwind_info_len) {
		int op = *p & 0xc0;

		switch (op) {
		case DW_CFA_advance_loc:
			pos += *p & 0x3f;
			printf ("CFA: [%x] advance loc\n",pos);
			p ++;
			break;
		case DW_CFA_offset:
			reg = *p & 0x3f;
			p ++;
			offset = decode_uleb128 (p, &p) * DWARF_DATA_ALIGN;
			if (reg == DWARF_PC_REG)
				printf ("CFA: [%x] offset: %s at cfa-0x%x\n", pos, "pc", -offset);
			else
				printf ("CFA: [%x] offset: %s at cfa-0x%x\n", pos, mono_arch_regname (mono_dwarf_reg_to_hw_reg (reg)), -offset);
			break;
		case 0: {
			int ext_op = *p;
			p ++;
			switch (ext_op) {
			case DW_CFA_def_cfa:
				cfa_reg = decode_uleb128 (p, &p);
				cfa_offset = decode_uleb128 (p, &p);
				printf ("CFA: [%x] def_cfa: %s+0x%x\n", pos, mono_arch_regname (mono_dwarf_reg_to_hw_reg (cfa_reg)), cfa_offset);
				break;
			case DW_CFA_def_cfa_offset:
				cfa_offset = decode_uleb128 (p, &p);
				printf ("CFA: [%x] def_cfa_offset: 0x%x\n", pos, cfa_offset);
				break;
			case DW_CFA_def_cfa_register:
				cfa_reg = decode_uleb128 (p, &p);
				printf ("CFA: [%x] def_cfa_reg: %s\n", pos, mono_arch_regname (mono_dwarf_reg_to_hw_reg (cfa_reg)));
				break;
			case DW_CFA_offset_extended_sf:
				reg = decode_uleb128 (p, &p);
				offset = decode_sleb128 (p, &p) * DWARF_DATA_ALIGN;
				printf ("CFA: [%x] offset_extended_sf: %s at cfa-0x%x\n", pos, mono_arch_regname (mono_dwarf_reg_to_hw_reg (reg)), -offset);
				break;
			case DW_CFA_offset_extended:
				reg = decode_uleb128 (p, &p);
				offset = decode_uleb128 (p, &p) * DWARF_DATA_ALIGN;
				printf ("CFA: [%x] offset_extended: %s at cfa-0x%x\n", pos, mono_arch_regname (mono_dwarf_reg_to_hw_reg (reg)), -offset);
				break;
			case DW_CFA_same_value:
				reg = decode_uleb128 (p, &p);
				printf ("CFA: [%x] same_value: %s\n", pos, mono_arch_regname (mono_dwarf_reg_to_hw_reg (reg)));
				break;
			case DW_CFA_remember_state:
				printf ("CFA: [%x] remember_state\n", pos);
				break;
			case DW_CFA_restore_state:
				printf ("CFA: [%x] restore_state\n", pos);
				break;
			case DW_CFA_mono_advance_loc:
				printf ("CFA: [%x] mono_advance_loc\n", pos);
				break;
			case DW_CFA_advance_loc1:
				printf ("CFA: [%x] advance_loc1\n", pos);
				pos += *p;
				p += 1;
				break;
			case DW_CFA_advance_loc2:
				printf ("CFA: [%x] advance_loc2\n", pos);
				pos += read16 (p);
				p += 2;
				break;
			case DW_CFA_advance_loc4:
				printf ("CFA: [%x] advance_loc4\n", pos);
				pos += read32 (p);
				p += 4;
				break;
			default:
				g_assert_not_reached ();
			}
			break;
		}
		default:
			g_assert_not_reached ();
		}
	}
}

/*
 * mono_unwind_ops_encode_full:
 *
 *   Encode the unwind ops in UNWIND_OPS into the compact DWARF encoding.
 * Return a pointer to malloc'ed memory.
 * If ENABLE_EXTENSIONS is FALSE, avoid encoding the mono extension
 * opcode (DW_CFA_mono_advance_loc).
 */
guint8*
mono_unwind_ops_encode_full (GSList *unwind_ops, guint32 *out_len, gboolean enable_extensions)
{
	MonoUnwindOp *op;
	int loc = 0;
	guint8 buf [4096];
	guint8 *p, *res;

	p = buf;

	for (GSList *l = unwind_ops; l; l = l->next) {
		int reg;

		op = (MonoUnwindOp *)l->data;

		/* Convert the register from the hw encoding to the dwarf encoding */
		reg = mono_hw_reg_to_dwarf_reg (op->reg);

		if (op->op == DW_CFA_mono_advance_loc) {
			/* This advances loc to its location */
			loc = op->when;
		}

		/* Emit an advance_loc if neccesary */
		while (op->when > loc) {
			if (op->when - loc >= 65536) {
				*p ++ = DW_CFA_advance_loc4;
				guint32 v = (guint32)(op->when - loc);
				memcpy (p, &v, 4);
				g_assert (read32 (p) == GUINT32_TO_LE((guint32)(op->when - loc)));
				p += 4;
				loc = op->when;
			} else if (op->when - loc >= 256) {
				*p ++ = DW_CFA_advance_loc2;
				guint16 v = (guint16)(op->when - loc);
				memcpy (p, &v, 2);
				g_assert (read16 (p) == GUINT16_TO_LE((guint32)(op->when - loc)));
				p += 2;
				loc = op->when;
			} else if (op->when - loc >= 32) {
				*p ++ = DW_CFA_advance_loc1;
				*(guint8*)p = (guint8)(op->when - loc);
				p += 1;
				loc = op->when;
			} else if (op->when - loc < 32) {
				*p ++ = DW_CFA_advance_loc | (op->when - loc);
				loc = op->when;
			} else {
				*p ++ = DW_CFA_advance_loc | (30);
				loc += 30;
			}
		}

		switch (op->op) {
		case DW_CFA_def_cfa:
			*p ++ = op->op;
			encode_uleb128 (reg, p, &p);
			encode_uleb128 (op->val, p, &p);
			break;
		case DW_CFA_def_cfa_offset:
			*p ++ = op->op;
			encode_uleb128 (op->val, p, &p);
			break;
		case DW_CFA_def_cfa_register:
			*p ++ = op->op;
			encode_uleb128 (reg, p, &p);
			break;
		case DW_CFA_same_value:
			*p ++ = op->op;
			encode_uleb128 (reg, p, &p);
			break;
		case DW_CFA_offset:
			if (reg > 63) {
				*p ++ = DW_CFA_offset_extended_sf;
				encode_uleb128 (reg, p, &p);
				encode_sleb128 (op->val / DWARF_DATA_ALIGN, p, &p);
			} else {
				*p ++ = DW_CFA_offset | reg;
				encode_uleb128 (op->val / DWARF_DATA_ALIGN, p, &p);
			}
			break;
		case DW_CFA_remember_state:
		case DW_CFA_restore_state:
			*p ++ = op->op;
			break;
		case DW_CFA_mono_advance_loc:
			if (!enable_extensions)
				break;
			/* Only one location is supported */
			g_assert (op->val == 0);
			*p ++ = op->op;
			break;
#if defined(TARGET_WIN32) && defined(TARGET_AMD64)
		case DW_CFA_mono_sp_alloc_info_win64:
		case DW_CFA_mono_fp_alloc_info_win64:
			// Drop Windows specific unwind op's. These op's are currently
			// only used when registering unwind info with Windows OS unwinder.
			break;
#endif
		default:
			g_assert_not_reached ();
			break;
		}
	}

	g_assert (p - buf < 4096);
	*out_len = p - buf;
	res = (guint8 *)g_malloc (p - buf);
	memcpy (res, buf, p - buf);
	return res;
}

guint8*
mono_unwind_ops_encode (GSList *unwind_ops, guint32 *out_len)
{
	return mono_unwind_ops_encode_full (unwind_ops, out_len, TRUE);
}

#if 0
#define UNW_DEBUG(stmt) do { stmt; } while (0)
#else
#define UNW_DEBUG(stmt) do { } while (0)
#endif

static G_GNUC_UNUSED void
print_dwarf_state (int cfa_reg, int cfa_offset, int ip, int nregs, Loc *locations, guint8 *reg_saved)
{
	printf ("\t%x: cfa=r%d+%d ", ip, cfa_reg, cfa_offset);

	for (int i = 0; i < nregs; ++i)
		if (reg_saved [i] && locations [i].loc_type == LOC_OFFSET)
			printf ("r%d@%d(cfa) ", i, locations [i].offset);
	printf ("\n");
}

typedef struct {
	Loc locations [NUM_HW_REGS];
	guint8 reg_saved [NUM_HW_REGS];
	int cfa_reg, cfa_offset;
} UnwindState;

/*
 * Given the state of the current frame as stored in REGS, execute the unwind
 * operations in unwind_info until the location counter reaches POS. The result is
 * stored back into REGS. OUT_CFA will receive the value of the CFA.
 * If SAVE_LOCATIONS is non-NULL, it should point to an array of size SAVE_LOCATIONS_LEN.
 * On return, the nth entry will point to the address of the stack slot where register
 * N was saved, or NULL, if it was not saved by this frame.
 * MARK_LOCATIONS should contain the locations marked by mono_emit_unwind_op_mark_loc (), if any.
 * This function is signal safe.
 *
 * It returns FALSE on failure
 */
gboolean
mono_unwind_frame (guint8 *unwind_info, guint32 unwind_info_len,
				   guint8 *start_ip, guint8 *end_ip, guint8 *ip, guint8 **mark_locations,
				   mono_unwind_reg_t *regs, int nregs,
				   host_mgreg_t **save_locations, int save_locations_len,
				   guint8 **out_cfa)
{
	Loc locations [NUM_HW_REGS];
	guint8 reg_saved [NUM_HW_REGS];
	int pos, reg, hwreg, cfa_reg = -1, cfa_offset = 0, offset;
	guint8 *p;
	guint8 *cfa_val;
	UnwindState state_stack [1];
	int state_stack_pos;

	memset (reg_saved, 0, sizeof (reg_saved));
	state_stack [0].cfa_reg = -1;
	state_stack [0].cfa_offset = 0;

	p = unwind_info;
	pos = 0;
	cfa_reg = -1;
	cfa_offset = -1;
	state_stack_pos = 0;
	while (pos <= ip - start_ip && p < unwind_info + unwind_info_len) {
		int op = *p & 0xc0;

		switch (op) {
		case DW_CFA_advance_loc:
			UNW_DEBUG (print_dwarf_state (cfa_reg, cfa_offset, pos, nregs, locations));
			pos += *p & 0x3f;
			p ++;
			break;
		case DW_CFA_offset:
			reg = *p & 0x3f;
			p ++;
			if (reg >= NUM_DWARF_REGS) {
				/* Register we don't care about, like a caller save reg in a cold cconv */
				decode_uleb128 (p, &p);
				break;
			}
			hwreg = mono_dwarf_reg_to_hw_reg (reg);
			reg_saved [hwreg] = TRUE;
			locations [hwreg].loc_type = LOC_OFFSET;
			locations [hwreg].offset = decode_uleb128 (p, &p) * DWARF_DATA_ALIGN;
			break;
		case 0: {
			int ext_op = *p;
			p ++;
			switch (ext_op) {
			case DW_CFA_def_cfa:
				cfa_reg = decode_uleb128 (p, &p);
				cfa_offset = decode_uleb128 (p, &p);
				break;
			case DW_CFA_def_cfa_offset:
				cfa_offset = decode_uleb128 (p, &p);
				break;
			case DW_CFA_def_cfa_register:
				cfa_reg = decode_uleb128 (p, &p);
				break;
			case DW_CFA_offset_extended_sf:
				reg = decode_uleb128 (p, &p);
				offset = decode_sleb128 (p, &p);
				if (reg >= NUM_DWARF_REGS)
					break;
				hwreg = mono_dwarf_reg_to_hw_reg (reg);
				reg_saved [hwreg] = TRUE;
				locations [hwreg].loc_type = LOC_OFFSET;
				locations [hwreg].offset = offset * DWARF_DATA_ALIGN;
				break;
			case DW_CFA_offset_extended:
				reg = decode_uleb128 (p, &p);
				offset = decode_uleb128 (p, &p);
				if (reg >= NUM_DWARF_REGS)
					break;
				hwreg = mono_dwarf_reg_to_hw_reg (reg);
				reg_saved [hwreg] = TRUE;
				locations [hwreg].loc_type = LOC_OFFSET;
				locations [hwreg].offset = offset * DWARF_DATA_ALIGN;
				break;
			case DW_CFA_same_value:
				reg = decode_uleb128 (p, &p);
				if (reg >= NUM_DWARF_REGS)
					break;
				hwreg = mono_dwarf_reg_to_hw_reg (reg);
				locations [hwreg].loc_type = LOC_SAME;
				break;
			case DW_CFA_advance_loc1:
				pos += *p;
				p += 1;
				break;
			case DW_CFA_advance_loc2:
				pos += read16 (p);
				p += 2;
				break;
			case DW_CFA_advance_loc4:
				pos += read32 (p);
				p += 4;
				break;
			case DW_CFA_remember_state:
				if (state_stack_pos != 0) {
					mono_runtime_printf_err ("Unwind failure. Assertion at %s %d\n.", __FILE__, __LINE__);
					return FALSE;
				}
				memcpy (&state_stack [0].locations, &locations, sizeof (locations));
				memcpy (&state_stack [0].reg_saved, &reg_saved, sizeof (reg_saved));
				state_stack [0].cfa_reg = cfa_reg;
				state_stack [0].cfa_offset = cfa_offset;
				state_stack_pos ++;
				break;
			case DW_CFA_restore_state:
				if (state_stack_pos != 1) {
					mono_runtime_printf_err ("Unwind failure. Assertion at %s %d\n.", __FILE__, __LINE__);
					return FALSE;
				}
				state_stack_pos --;
				memcpy (&locations, &state_stack [0].locations, sizeof (locations));
				memcpy (&reg_saved, &state_stack [0].reg_saved, sizeof (reg_saved));
				cfa_reg = state_stack [0].cfa_reg;
				cfa_offset = state_stack [0].cfa_offset;
				break;
			case DW_CFA_mono_advance_loc:
				if (!mark_locations [0]) {
					mono_runtime_printf_err ("Unwind failure. Assertion at %s %d\n.", __FILE__, __LINE__);
					return FALSE;
				}
				pos = mark_locations [0] - start_ip;
				break;
			default:
				mono_runtime_printf_err ("Unwind failure. Illegal value for switch statement, assertion at %s %d\n.", __FILE__, __LINE__);
				return FALSE;
			}
			break;
		}
		default:
			mono_runtime_printf_err ("Unwind failure. Illegal value for switch statement, assertion at %s %d\n.", __FILE__, __LINE__);
			return FALSE;
		}
	}

	if (save_locations)
		memset (save_locations, 0, save_locations_len * sizeof (host_mgreg_t*));

	if (cfa_reg == -1) {
		mono_runtime_printf_err ("Unset cfa_reg in method %s. Memory around ip (%p):", mono_get_method_from_ip (ip), ip);
		mono_dump_mem (ip - 0x10, 0x40);
		return FALSE;
	}
	cfa_val = (guint8*)regs [mono_dwarf_reg_to_hw_reg (cfa_reg)] + cfa_offset;
	for (hwreg = 0; hwreg < NUM_HW_REGS; ++hwreg) {
		if (reg_saved [hwreg] && locations [hwreg].loc_type == LOC_OFFSET) {
			int dwarfreg = mono_hw_reg_to_dwarf_reg (hwreg);
			if (hwreg >= nregs) {
				mono_runtime_printf_err ("Unwind failure. Assertion at %s %d\n.", __FILE__, __LINE__);
				return FALSE;
			}
			if (IS_DOUBLE_REG (dwarfreg))
				regs [hwreg] = *(guint64*)(cfa_val + locations [hwreg].offset);
			else
				regs [hwreg] = *(host_mgreg_t*)(cfa_val + locations [hwreg].offset);
			if (save_locations && hwreg < save_locations_len)
				save_locations [hwreg] = (host_mgreg_t*)(cfa_val + locations [hwreg].offset);
		}
	}

	*out_cfa = cfa_val;

	// Success
	return TRUE;
}

void
mono_unwind_init (void)
{
	mono_os_mutex_init_recursive (&unwind_mutex);

	mono_counters_register ("Unwind info size", MONO_COUNTER_JIT | MONO_COUNTER_INT, &unwind_info_size);
}

static guint
cached_info_hash(gconstpointer key)
{
	guint i, a;
	const guint8 *info = cached_info [GPOINTER_TO_UINT (key)].info;
	const guint len = cached_info [GPOINTER_TO_UINT (key)].len;

	for (i = a = 0; i != len; ++i)
		a ^= (((guint)info [i]) << (i & 0xf));

	return a;
}

static gboolean
cached_info_eq(gconstpointer a, gconstpointer b)
{
	const guint32 lena = cached_info [GPOINTER_TO_UINT (a)].len;
	const guint32 lenb = cached_info [GPOINTER_TO_UINT (b)].len;
	if (lena == lenb) {
		const guint8 *infoa = cached_info [GPOINTER_TO_UINT (a)].info;
		const guint8 *infob = cached_info [GPOINTER_TO_UINT (b)].info;
		if (memcmp (infoa, infob, lena) == 0)
			return TRUE;
	}

	return FALSE;
}

/*
 * mono_cache_unwind_info
 *
 *   Save UNWIND_INFO in the unwind info cache and return an id which can be passed
 * to mono_get_cached_unwind_info to get a cached copy of the info.
 * A copy is made of the unwind info.
 * This function is useful for two reasons:
 * - many methods have the same unwind info
 * - MonoJitInfo->unwind_info is an int so it can't store the pointer to the unwind info
 */
guint32
mono_cache_unwind_info (guint8 *unwind_info, guint32 unwind_info_len)
{
	gpointer orig_key;
	guint32 i;
	unwind_lock ();

	if (!cached_info_ht)
		cached_info_ht = g_hash_table_new (cached_info_hash, cached_info_eq);

	if (cached_info_next >= cached_info_size) {
		MonoUnwindInfo *new_table;
		int new_cached_info_size = cached_info_size ? cached_info_size * 2 : 16;

		/* ensure no integer overflow */
		g_assert (new_cached_info_size > cached_info_size);

		/*
		 * Avoid freeing the old table so mono_get_cached_unwind_info ()
		 * doesn't need locks/hazard pointers.
		 */
		new_table = g_new0 (MonoUnwindInfo, new_cached_info_size );

		/* include array allocations into statistics of memory totally consumed by unwind info */
		unwind_info_size += sizeof (MonoUnwindInfo) * new_cached_info_size ;

		if (cached_info_size)
			memcpy (new_table, cached_info, sizeof (MonoUnwindInfo) * cached_info_size);

		mono_memory_barrier ();

		cached_info_list = g_slist_prepend (cached_info_list, cached_info);

		cached_info = new_table;

		cached_info_size = new_cached_info_size;
	}

	i = cached_info_next;

	/* construct temporary element at array's edge without allocated info copy - it will be used for hashtable lookup */
	cached_info [i].len = unwind_info_len;
	cached_info [i].info = unwind_info;

	if (!g_hash_table_lookup_extended (cached_info_ht, GUINT_TO_POINTER (i), &orig_key, NULL) ) {
		/* hashtable lookup didnt find match - now need to really add new element with allocated copy of unwind info */
		cached_info [i].info = g_new (guint8, unwind_info_len);
		memcpy (cached_info [i].info, unwind_info, unwind_info_len);

		/* include allocated memory in stats, note that hashtable allocates struct of 3 pointers per each entry */
		unwind_info_size += sizeof (void *) * 3 + unwind_info_len;
		g_hash_table_insert_replace (cached_info_ht, GUINT_TO_POINTER (i), NULL, TRUE);

		cached_info_next = i + 1;

	} else {
		i = GPOINTER_TO_UINT (orig_key);
	}

	unwind_unlock ();

	return i;
}

/*
 * This function is signal safe.
 */
guint8*
mono_get_cached_unwind_info (guint32 index, guint32 *unwind_info_len)
{
	MonoUnwindInfo *info;
	guint8 *data;

	/*
	 * This doesn't need any locks/hazard pointers,
	 * since new tables are copies of the old ones.
	 */
	info = &cached_info [index];

	*unwind_info_len = info->len;
	data = info->info;

	return data;
}

/*
 * mono_unwind_get_dwarf_data_align:
 *
 *   Return the data alignment used by the encoded unwind information.
 */
int
mono_unwind_get_dwarf_data_align (void)
{
	return DWARF_DATA_ALIGN;
}

/*
 * mono_unwind_get_dwarf_pc_reg:
 *
 *   Return the dwarf register number of the register holding the ip of the
 * previous frame.
 */
int
mono_unwind_get_dwarf_pc_reg (void)
{
	return DWARF_PC_REG;
}

static void
decode_cie_op (guint8 *p, guint8 **endp)
{
	int op = *p & 0xc0;

	switch (op) {
	case DW_CFA_advance_loc:
		p ++;
		break;
	case DW_CFA_offset:
		p ++;
		decode_uleb128 (p, &p);
		break;
	case 0: {
		int ext_op = *p;
		p ++;
		switch (ext_op) {
		case DW_CFA_def_cfa:
			decode_uleb128 (p, &p);
			decode_uleb128 (p, &p);
			break;
		case DW_CFA_def_cfa_offset:
			decode_uleb128 (p, &p);
			break;
		case DW_CFA_def_cfa_register:
			decode_uleb128 (p, &p);
			break;
		case DW_CFA_advance_loc4:
			p += 4;
			break;
		case DW_CFA_offset_extended_sf:
			decode_uleb128 (p, &p);
			decode_uleb128 (p, &p);
			break;
		default:
			g_assert_not_reached ();
		}
		break;
	}
	default:
		g_assert_not_reached ();
	}

	*endp = p;
}

static gint64
read_encoded_val (guint32 encoding, guint8 *p, guint8 **endp)
{
	gint64 res;

	switch (encoding & 0xf) {
	case DW_EH_PE_sdata8:
		res = *(gint64*)p;
		p += 8;
		break;
	case DW_EH_PE_sdata4:
		res = *(gint32*)p;
		p += 4;
		break;
	default:
		g_assert_not_reached ();
	}

	*endp = p;
	return res;
}

/*
 * decode_lsda:
 *
 *   Decode the Mono specific Language Specific Data Area generated by LLVM.
 * This function is async safe.
 */
static void
decode_lsda (guint8 *lsda, guint8 *code, MonoJitExceptionInfo *ex_info, gpointer *type_info, guint32 *ex_info_len, int *this_reg, int *this_offset)
{
	guint8 *p;
	int ncall_sites, this_encoding;
	guint32 mono_magic, version;

	p = lsda;

	/* This is the modified LSDA generated by the LLVM mono branch */
	mono_magic = decode_uleb128 (p, &p);
	g_assert (mono_magic == 0x4d4fef4f);
	version = decode_uleb128 (p, &p);
	g_assert (version == 1);
	this_encoding = *p;
	p ++;
	if (this_encoding == DW_EH_PE_udata4) {
		gint32 op, reg, offset;

		/* 'this' location */
		op = *p;
		g_assert (op == DW_OP_bregx);
		p ++;
		reg = decode_uleb128 (p, &p);
		offset = decode_sleb128 (p, &p);

		*this_reg = mono_dwarf_reg_to_hw_reg (reg);
		*this_offset = offset;
	} else {
		g_assert (this_encoding == DW_EH_PE_omit);

		*this_reg = -1;
		*this_offset = -1;
	}
	ncall_sites = decode_uleb128 (p, &p);
	p = (guint8*)ALIGN_TO ((gsize)p, 4);

	if (ex_info_len)
		*ex_info_len = ncall_sites;

	for (int i = 0; i < ncall_sites; ++i) {
		int block_start_offset, block_size, landing_pad;
		guint8 *tinfo;

		block_start_offset = read32 (p);
		p += sizeof (gint32);
		block_size = read32 (p);
		p += sizeof (gint32);
		landing_pad = read32 (p);
		p += sizeof (gint32);
		tinfo = p;
		p += sizeof (gint32);

		g_assert (landing_pad);
		g_assert (((size_t)tinfo % 4) == 0);
		//printf ("X: %p %d\n", landing_pad, *(int*)tinfo);

		if (ex_info) {
			if (type_info)
				type_info [i] = tinfo;
			ex_info[i].try_start = code + block_start_offset;
			ex_info[i].try_end = code + block_start_offset + block_size;
			ex_info[i].handler_start = code + landing_pad;
		}
	}
}

/*
 * mono_unwind_decode_fde:
 *
 *   Decode a DWARF FDE entry, returning the unwind opcodes.
 * If not NULL, EX_INFO is set to a malloc-ed array of MonoJitExceptionInfo structures,
 * only try_start, try_end and handler_start is set.
 * If not NULL, TYPE_INFO is set to a malloc-ed array containing the ttype table from the
 * LSDA.
 */
guint8*
mono_unwind_decode_fde (guint8 *fde, guint32 *out_len, guint32 *code_len, MonoJitExceptionInfo **ex_info, guint32 *ex_info_len, gpointer **type_info, int *this_reg, int *this_offset)
{
	guint8 *p, *cie, *fde_current, *fde_aug = NULL, *code, *fde_cfi, *cie_cfi;
	gint32 fde_len, cie_offset, pc_begin, pc_range, aug_len;
	gint32 cie_len, cie_id, cie_version, code_align, data_align, return_reg;
	gint32 i, cie_aug_len, buf_len;
	char *cie_aug_str;
	guint8 *buf;
	gboolean has_fde_augmentation = FALSE;

	/*
	 * http://refspecs.freestandards.org/LSB_3.0.0/LSB-Core-generic/LSB-Core-generic/ehframechpt.html
	 */

	/* This is generated by JITDwarfEmitter::EmitEHFrame () */

	*type_info = NULL;
	*this_reg = -1;
	*this_offset = -1;

	/* Decode FDE */

	p = fde;
	// FIXME: Endianess ?
	fde_len = *(guint32*)p;
	g_assert (fde_len != 0xffffffff && fde_len != 0);
	p += 4;
	cie_offset = *(guint32*)p;
	cie = p - cie_offset;
	p += 4;
	fde_current = p;

	/* Decode CIE */
	p = cie;
	cie_len = *(guint32*)p;
	p += 4;
	cie_id = *(guint32*)p;
	g_assert (cie_id == 0);
	p += 4;
	cie_version = *p;
	g_assert (cie_version == 1);
	p += 1;
	cie_aug_str = (char*)p;
	p += strlen (cie_aug_str) + 1;
	code_align = decode_uleb128 (p, &p);
	data_align = decode_sleb128 (p, &p);
	return_reg = decode_uleb128 (p, &p);
	if (strstr (cie_aug_str, "z")) {
		guint8 *cie_aug;
		guint32 p_encoding;

		cie_aug_len = decode_uleb128 (p, &p);

		has_fde_augmentation = TRUE;

		cie_aug = p;
		for (i = 0; cie_aug_str [i] != '\0'; ++i) {
			switch (cie_aug_str [i]) {
			case 'z':
				break;
			case 'P':
				p_encoding = *p;
				p ++;
				read_encoded_val (p_encoding, p, &p);
				break;
			case 'L':
				g_assert ((*p == (DW_EH_PE_sdata4|DW_EH_PE_pcrel)) || (*p == (DW_EH_PE_sdata8|DW_EH_PE_pcrel)));
				p ++;
				break;
			case 'R':
				g_assert (*p == (DW_EH_PE_sdata4|DW_EH_PE_pcrel));
				p ++;
				break;
			default:
				g_assert_not_reached ();
				break;
			}
		}

		p = cie_aug;
		p += cie_aug_len;
	}
	cie_cfi = p;

	/* Continue decoding FDE */
	p = fde_current;
	/* DW_EH_PE_sdata4|DW_EH_PE_pcrel encoding */
	pc_begin = *(gint32*)p;
	code = p + pc_begin;
	p += 4;
	pc_range = *(guint32*)p;
	p += 4;
	if (has_fde_augmentation) {
		aug_len = decode_uleb128 (p, &p);
		fde_aug = p;
		p += aug_len;
	} else {
		aug_len = 0;
	}
	fde_cfi = p;

	if (code_len)
		*code_len = pc_range;

	if (ex_info) {
		*ex_info = NULL;
		*ex_info_len = 0;
	}

	/* Decode FDE augmention */
	if (aug_len) {
		gint32 lsda_offset;
		guint8 *lsda;

		/* sdata|pcrel encoding */
		if (aug_len == 4)
			lsda_offset = read32 (fde_aug);
		else if (aug_len == 8)
			lsda_offset = *(gint64*)fde_aug;
		else
			g_assert_not_reached ();
		if (lsda_offset != 0) {
			lsda = fde_aug + lsda_offset;

			/* Get the lengths first */
			guint32 len;
			decode_lsda (lsda, code, NULL, NULL, &len, this_reg, this_offset);

			if (ex_info)
				*ex_info = (MonoJitExceptionInfo *)g_malloc0 (len * sizeof (MonoJitExceptionInfo));
			if (type_info)
				*type_info = (gpointer *)g_malloc0 (len * sizeof (gpointer));

			decode_lsda (lsda, code, ex_info ? *ex_info : NULL, type_info ? *type_info : NULL, ex_info_len, this_reg, this_offset);
		}
	}

	/* Make sure the FDE uses the same constants as we do */
	g_assert (code_align == 1);
	g_assert (data_align == DWARF_DATA_ALIGN);
	g_assert (return_reg == DWARF_PC_REG);

	buf_len = (cie + cie_len + 4 - cie_cfi) + (fde + fde_len + 4 - fde_cfi);
	buf = (guint8 *)g_malloc0 (buf_len);

	i = 0;
	p = cie_cfi;
	while (p < cie + cie_len + 4) {
		if (*p == DW_CFA_nop)
			break;
		decode_cie_op (p, &p);
	}
	memcpy (buf + i, cie_cfi, p - cie_cfi);
	i += p - cie_cfi;

	p = fde_cfi;
	while (p < fde + fde_len + 4) {
		if (*p == DW_CFA_nop)
			break;
		decode_cie_op (p, &p);
	}
	memcpy (buf + i, fde_cfi, p - fde_cfi);
	i += p - fde_cfi;
	g_assert (i <= buf_len);

	*out_len = i;

	return (guint8 *)g_realloc (buf, i);
}

/*
 * mono_unwind_decode_mono_fde:
 *
 *   Decode an FDE entry in the LLVM emitted mono EH frame.
 * If EI/TYPE_INFO/UNW_INFO are NULL, compute only the value of the scalar fields in INFO.
 * Otherwise:
 * - Fill out EX_INFO with try_start, try_end and handler_start.
 * - Fill out TYPE_INFO with the ttype table from the LSDA.
 * - Fill out UNW_INFO with the unwind info.
 * This function is async safe.
 */
void
mono_unwind_decode_llvm_mono_fde (guint8 *fde, int fde_len, guint8 *cie, guint8 *code, MonoLLVMFDEInfo *res, MonoJitExceptionInfo *ex_info, gpointer *type_info, guint8 *unw_info)
{
	guint8 *p, *fde_aug, *cie_cfi, *fde_cfi, *buf;
	int has_aug, aug_len, cie_cfi_len, fde_cfi_len;
	gint32 code_align, data_align, return_reg, pers_encoding;

	memset (res, 0, sizeof (*res));
	res->this_reg = -1;
	res->this_offset = -1;

	/* fde points to data emitted by LLVM in DwarfMonoException::EmitMonoEHFrame () */
	p = fde;
	has_aug = *p;
	p ++;
	if (has_aug) {
		aug_len = read32 (p);
		p += 4;
	} else {
		aug_len = 0;
	}
	fde_aug = p;
	p += aug_len;
	fde_cfi = p;

	if (has_aug) {
		guint8 *lsda;

		/* The LSDA is embedded directly into the FDE */
		lsda = fde_aug;

		/* Get the lengths first */
		decode_lsda (lsda, code, NULL, NULL, &res->ex_info_len, &res->this_reg, &res->this_offset);

		decode_lsda (lsda, code, ex_info, type_info, NULL, &res->this_reg, &res->this_offset);
	}

	/* Decode CIE */
	p = cie;
	code_align = decode_uleb128 (p, &p);
	data_align = decode_sleb128 (p, &p);
	return_reg = decode_uleb128 (p, &p);
	pers_encoding = *p;
	p ++;
	if (pers_encoding != DW_EH_PE_omit)
		read_encoded_val (pers_encoding, p, &p);

	cie_cfi = p;

	/* Make sure the FDE uses the same constants as we do */
	g_assert (code_align == 1);
	g_assert (data_align == DWARF_DATA_ALIGN);
	g_assert (return_reg == DWARF_PC_REG);

	/* Compute size of CIE unwind info it is DW_CFA_nop terminated */
	p = cie_cfi;
	while (*p != DW_CFA_nop) {
	    decode_cie_op (p, &p);
	}
	cie_cfi_len = p - cie_cfi;
	fde_cfi_len = (fde + fde_len - fde_cfi);

	buf = unw_info;
	if (buf) {
		memcpy (buf, cie_cfi, cie_cfi_len);
		memcpy (buf + cie_cfi_len, fde_cfi, fde_cfi_len);
	}

	res->unw_info_len = cie_cfi_len + fde_cfi_len;
}

/*
 * mono_unwind_get_cie_program:
 *
 *   Get the unwind bytecode for the DWARF CIE.
 */
GSList*
mono_unwind_get_cie_program (void)
{
#if defined(TARGET_AMD64) || defined(TARGET_X86) || defined(TARGET_POWERPC) || defined(TARGET_ARM)
	return mono_arch_get_cie_program ();
#else
	return NULL;
#endif
}
