/*
 * unwind.c: Stack Unwinding Interface
 *
 * Authors:
 *   Zoltan Varga (vargaz@gmail.com)
 *
 * (C) 2008 Novell, Inc.
 */

#include "mini.h"
#include "unwind.h"

typedef enum {
	LOC_SAME,
	LOC_OFFSET
} LocType;

typedef struct {
	LocType loc_type;
	int offset;
} Loc;

#ifdef __x86_64__
static int map_hw_reg_to_dwarf_reg [] = { 0, 2, 1, 3, 7, 6, 4, 5, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
#define NUM_REGS AMD64_NREG
#else
#define NUM_REGS 0
#endif

static gboolean dwarf_reg_to_hw_reg_inited;

static int map_dwarf_reg_to_hw_reg [NUM_REGS];

/*
 * mono_hw_reg_to_dwarf_reg:
 *
 *   Map the hardware register number REG to the register number used by DWARF.
 */
int
mono_hw_reg_to_dwarf_reg (int reg)
{
#ifdef __x86_64__
	return map_hw_reg_to_dwarf_reg [reg];
#else
	g_assert_not_reached ();
	return -1;
#endif
}

static void
init_reg_map (void)
{
	int i;

	g_assert (sizeof (map_hw_reg_to_dwarf_reg) / sizeof (int) == NUM_REGS);
	for (i = 0; i < NUM_REGS; ++i) {
		map_dwarf_reg_to_hw_reg [mono_hw_reg_to_dwarf_reg (i)] = i;
	}

	mono_memory_barrier ();
	dwarf_reg_to_hw_reg_inited = TRUE;
}

static inline int
mono_dwarf_reg_to_hw_reg (int reg)
{
	if (!dwarf_reg_to_hw_reg_inited)
		init_reg_map ();

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

static inline guint32
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

/*
 * mono_unwind_ops_encode:
 *
 *   Encode the unwind ops in UNWIND_OPS into the compact DWARF encoding.
 * Return a pointer to malloc'ed memory.
 */
guint8*
mono_unwind_ops_encode (GSList *unwind_ops, guint32 *out_len)
{
	GSList *l;
	MonoUnwindOp *op;
	int loc;
	guint8 *buf, *p, *res;

	p = buf = g_malloc0 (256);

	loc = 0;
	l = unwind_ops;
	for (; l; l = l->next) {
		int reg;

		op = l->data;

		/* Convert the register from the hw encoding to the dwarf encoding */
		reg = mono_hw_reg_to_dwarf_reg (op->reg);

		/* Emit an advance_loc if neccesary */
		if (op->when > loc) {
			g_assert (op->when - loc < 32);
			*p ++ = DW_CFA_advance_loc | (op->when - loc);
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
		case DW_CFA_offset:
			*p ++ = DW_CFA_offset | reg;
			encode_uleb128 (op->val / - 8, p, &p);
			break;
		default:
			g_assert_not_reached ();
			break;
		}

		loc = op->when;
	}
	
	g_assert (p - buf < 256);
	*out_len = p - buf;
	res = g_malloc (p - buf);
	memcpy (res, buf, p - buf);
	g_free (buf);
	return res;
}

#if 0
#define UNW_DEBUG(stmt) do { stmt; } while (0)
#else
#define UNW_DEBUG(stmt) do { } while (0)
#endif

static G_GNUC_UNUSED void
print_dwarf_state (int cfa_reg, int cfa_offset, int ip, int nregs, Loc *locations)
{
	int i;

	printf ("\t%x: cfa=r%d+%d ", ip, cfa_reg, cfa_offset);

	for (i = 0; i < nregs; ++i)
		if (locations [i].loc_type == LOC_OFFSET)
			printf ("r%d@%d(cfa) ", i, locations [i].offset);
	printf ("\n");
}

/*
 * Given the state of the current frame as stored in REGS, execute the unwind 
 * operations in unwind_info until the location counter reaches POS. The result is 
 * stored back into REGS. OUT_CFA will receive the value of the CFA.
 */
void
mono_unwind_frame (guint8 *unwind_info, guint32 unwind_info_len, 
				   int data_align_factor,
				   guint8 *start_ip, guint8 *end_ip, guint8 *ip, gssize *regs, 
				   int nregs, guint8 **out_cfa) 
{
	Loc locations [NUM_REGS];
	int i, pos, reg, cfa_reg, cfa_offset;
	guint8 *p;
	guint8 *cfa_val;

	g_assert (nregs <= NUM_REGS);

	for (i = 0; i < nregs; ++i)
		locations [i].loc_type = LOC_SAME;

	p = unwind_info;
	pos = 0;
	while (pos < ip - start_ip && p < unwind_info + unwind_info_len) {
		int op = *p & 0xc0;

		switch (op) {
		case DW_CFA_advance_loc:
			UNW_DEBUG (print_dwarf_state (cfa_reg, cfa_offset, pos, nregs, locations));
			pos += *p & 0x3f;
			p ++;
			break;
		case DW_CFA_offset:
			reg = mono_dwarf_reg_to_hw_reg (*p & 0x3f);
			p ++;
			locations [reg].loc_type = LOC_OFFSET;
			locations [reg].offset = decode_uleb128 (p, &p) * data_align_factor;
			break;
		case 0: {
			int ext_op = *p;
			p ++;
			switch (ext_op) {
			case DW_CFA_def_cfa:
				cfa_reg = mono_dwarf_reg_to_hw_reg (decode_uleb128 (p, &p));
				cfa_offset = decode_uleb128 (p, &p);
				break;
			case DW_CFA_def_cfa_offset:
				cfa_offset = decode_uleb128 (p, &p);
				break;
			case DW_CFA_def_cfa_register:
				cfa_reg = mono_dwarf_reg_to_hw_reg (decode_uleb128 (p, &p));
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

	cfa_val = (guint8*)regs [cfa_reg] + cfa_offset;
	for (i = 0; i < nregs; ++i) {
		if (locations [i].loc_type == LOC_OFFSET)
			regs [i] = *(gssize*)(cfa_val + locations [i].offset);
	}

	*out_cfa = cfa_val;

	g_free (locations);
}
