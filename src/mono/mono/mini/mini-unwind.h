/**
 * \file
 * Stack Unwinding Interface
 *
 * Authors:
 *   Zoltan Varga (vargaz@gmail.com)
 *
 * (C) 2007 Novell, Inc.
 */

#ifndef __MONO_UNWIND_H__
#define __MONO_UNWIND_H__

#include "mini.h"

/* This is the same as host_mgreg_t, except on 32 bit bit platforms with callee saved fp regs */
#ifndef mono_unwind_reg_t
#define mono_unwind_reg_t host_mgreg_t
#endif

/*
 * This is a platform-independent interface for unwinding through stack frames 
 * based on the Dwarf unwinding interface.
 * See http://dwarfstd.org/Dwarf3.pdf, section "Call Frame Information".
 */

/*
 * CFA = Canonical Frame Address. By convention, this is the value of the stack pointer
 * prior to the execution of the call instruction in the caller. I.e. on x86, it is
 * esp + 4 on entry to a function. The value of the CFA does not change during execution
 * of a function. There are two kinds of unwind directives:
 * - those that describe how to compute the CFA at a given pc offset inside a function
 * - those that describe where a given register is saved relative to the CFA.
 */

/* Unwind ops */

/* The low 6 bits contain additional information */
#define DW_CFA_advance_loc        0x40
#define DW_CFA_offset             0x80
#define DW_CFA_restore            0xc0

#define DW_CFA_nop              0x00
#define DW_CFA_set_loc          0x01
#define DW_CFA_advance_loc1     0x02
#define DW_CFA_advance_loc2     0x03
#define DW_CFA_advance_loc4     0x04
#define DW_CFA_offset_extended  0x05
#define DW_CFA_restore_extended 0x06
#define DW_CFA_undefined        0x07
#define DW_CFA_same_value       0x08
#define DW_CFA_register         0x09
#define DW_CFA_remember_state   0x0a
#define DW_CFA_restore_state    0x0b
#define DW_CFA_def_cfa          0x0c
#define DW_CFA_def_cfa_register 0x0d
#define DW_CFA_def_cfa_offset   0x0e
#define DW_CFA_def_cfa_expression 0x0f
#define DW_CFA_expression       0x10
#define DW_CFA_offset_extended_sf 0x11
#define DW_CFA_def_cfa_sf       0x12
#define DW_CFA_def_cfa_offset_sf 0x13
#define DW_CFA_val_offset        0x14
#define DW_CFA_val_offset_sf     0x15
#define DW_CFA_val_expression    0x16
#define DW_CFA_lo_user           0x1c
#define DW_CFA_hi_user           0x3f

/*
 * Mono extension, advance loc to a location stored outside the unwind info.
 * This is required to make the unwind descriptors sharable, since otherwise each one would contain
 * an advance_loc with a different offset just before the unwind ops for the epilog.
 */
#define DW_CFA_mono_advance_loc DW_CFA_lo_user

/*
 * Mono extension, Windows x64 unwind ABI needs some more details around sp alloc size and fp offset.
 */
#if defined(TARGET_WIN32) && defined(TARGET_AMD64)
#define DW_CFA_mono_sp_alloc_info_win64 (DW_CFA_lo_user + 1)
#define DW_CFA_mono_fp_alloc_info_win64 (DW_CFA_lo_user + 2)
#endif

/* Represents one unwind instruction */
typedef struct {
	guint8 op; /* One of DW_CFA_... */
	guint16 reg; /* register number in the hardware encoding */
	gint32 val; /* arbitrary value */
	guint32 when; /* The offset _after_ the cpu instruction this unwind op belongs to */
} MonoUnwindOp;

/* 
 * Macros for emitting MonoUnwindOp structures.
 * These should be called _after_ emitting the cpu instruction the unwind op
 * belongs to.
 */

/* Set cfa to reg+offset */
#define mono_emit_unwind_op_def_cfa(cfg,ip,reg,offset) do { mono_emit_unwind_op (cfg, (ip) - (cfg)->native_code, DW_CFA_def_cfa, (reg), (offset)); (cfg)->cur_cfa_reg = (reg); (cfg)->cur_cfa_offset = (offset); } while (0)
/* Set cfa to reg+existing offset */
#define mono_emit_unwind_op_def_cfa_reg(cfg,ip,reg) do { mono_emit_unwind_op (cfg, (ip) - (cfg)->native_code, DW_CFA_def_cfa_register, (reg), (0)); (cfg)->cur_cfa_reg = (reg); } while (0)
/* Set cfa to existing reg+offset */
#define mono_emit_unwind_op_def_cfa_offset(cfg,ip,offset) do { mono_emit_unwind_op (cfg, (ip) - (cfg)->native_code, DW_CFA_def_cfa_offset, (0), (offset)); (cfg)->cur_cfa_offset = (offset); } while (0)
/* Reg is the same as it was on enter to the function */
#define mono_emit_unwind_op_same_value(cfg,ip,reg) mono_emit_unwind_op (cfg, (ip) - (cfg)->native_code, DW_CFA_same_value, (reg), 0)
/* Reg is saved at cfa+offset */
#define mono_emit_unwind_op_offset(cfg,ip,reg,offset) mono_emit_unwind_op (cfg, (ip) - (cfg)->native_code, DW_CFA_offset, (reg), (offset))
/* Save the unwind state into an implicit stack */
#define mono_emit_unwind_op_remember_state(cfg,ip) mono_emit_unwind_op (cfg, (ip) - (cfg)->native_code, DW_CFA_remember_state, 0, 0)
/* Restore the unwind state from the state stack */
#define mono_emit_unwind_op_restore_state(cfg,ip) mono_emit_unwind_op (cfg, (ip) - (cfg)->native_code, DW_CFA_restore_state, 0, 0)
/*
 * Mark the current location as a location stored outside the unwind info, which will be passed
 * explicitly to mono_unwind_frame () in the MARK_LOCATIONS argument. This allows the unwind info
 * to be shared among multiple methods.
 */
#define mono_emit_unwind_op_mark_loc(cfg,ip,n) mono_emit_unwind_op (cfg, (ip) - (cfg)->native_code, DW_CFA_mono_advance_loc, 0, (n))

#if defined(TARGET_WIN32) && defined(TARGET_AMD64)
#define mono_emit_unwind_op_sp_alloc(cfg,ip,size) mono_emit_unwind_op (cfg, (ip) - (cfg)->native_code, DW_CFA_mono_sp_alloc_info_win64, 0, (size))
#define mono_emit_unwind_op_fp_alloc(cfg,ip,reg,size) mono_emit_unwind_op (cfg, (ip) - (cfg)->native_code, DW_CFA_mono_fp_alloc_info_win64, (reg), (size))
#else
#define mono_emit_unwind_op_sp_alloc(cfg,ip,size)
#define mono_emit_unwind_op_fp_alloc(cfg,ip,reg,size)
#endif

/* Similar macros usable when a cfg is not available, like for trampolines */
#define mono_add_unwind_op_def_cfa(op_list,code,buf,reg,offset) do { (op_list) = g_slist_append ((op_list), mono_create_unwind_op ((code) - (buf), DW_CFA_def_cfa, (reg), (offset))); } while (0)
#define mono_add_unwind_op_def_cfa_reg(op_list,code,buf,reg) do { (op_list) = g_slist_append ((op_list), mono_create_unwind_op ((code) - (buf), DW_CFA_def_cfa_register, (reg), (0))); } while (0)
#define mono_add_unwind_op_def_cfa_offset(op_list,code,buf,offset) do { (op_list) = g_slist_append ((op_list), mono_create_unwind_op ((code) - (buf), DW_CFA_def_cfa_offset, 0, (offset))); } while (0)
#define mono_add_unwind_op_same_value(op_list,code,buf,reg) do { (op_list) = g_slist_append ((op_list), mono_create_unwind_op ((code) - (buf), DW_CFA_same_value, (reg), 0)); } while (0)
#define mono_add_unwind_op_offset(op_list,code,buf,reg,offset) do { (op_list) = g_slist_append ((op_list), mono_create_unwind_op ((code) - (buf), DW_CFA_offset, (reg), (offset))); } while (0)

#if defined(TARGET_WIN32) && defined(TARGET_AMD64)
#define mono_add_unwind_op_sp_alloc(op_list,code,buf,size) do { (op_list) = g_slist_append ((op_list), mono_create_unwind_op ((code) - (buf), DW_CFA_mono_sp_alloc_info_win64, 0, (size))); } while (0)
#define mono_add_unwind_op_fp_alloc(op_list,code,buf,reg,size) do { (op_list) = g_slist_append ((op_list), mono_create_unwind_op ((code) - (buf), DW_CFA_mono_fp_alloc_info_win64, (reg), (size))); } while (0)
#else
#define mono_add_unwind_op_sp_alloc(op_list,code,buf,size)
#define mono_add_unwind_op_fp_alloc(op_list,code,buf,reg,size)
#endif

#define mono_free_unwind_info(op_list) do { GSList *l; for (l = op_list; l; l = l->next) g_free (l->data); g_slist_free (op_list); op_list = NULL; } while (0)

/* Pointer Encoding in the .eh_frame */
enum {
	DW_EH_PE_absptr = 0x00,
	DW_EH_PE_omit = 0xff,

	DW_EH_PE_udata4 = 0x03,
	DW_EH_PE_sdata4 = 0x0b,
	DW_EH_PE_sdata8 = 0x0c,

	DW_EH_PE_pcrel = 0x10,
	DW_EH_PE_textrel = 0x20,
	DW_EH_PE_datarel = 0x30,
	DW_EH_PE_funcrel = 0x40,
	DW_EH_PE_aligned = 0x50,

	DW_EH_PE_indirect = 0x80
};

int
mono_hw_reg_to_dwarf_reg (int reg);

int
mono_dwarf_reg_to_hw_reg (int reg);

int
mono_unwind_get_dwarf_data_align (void);

int
mono_unwind_get_dwarf_pc_reg (void);

guint8*
mono_unwind_ops_encode_full (GSList *unwind_ops, guint32 *out_len, gboolean enable_extensions);

guint8*
mono_unwind_ops_encode (GSList *unwind_ops, guint32 *out_len);

gboolean
mono_unwind_frame (guint8 *unwind_info, guint32 unwind_info_len, 
				   guint8 *start_ip, guint8 *end_ip, guint8 *ip, guint8 **mark_locations,
				   mono_unwind_reg_t *regs, int nregs,
				   host_mgreg_t **save_locations, int save_locations_len,
				   guint8 **out_cfa);

void mono_unwind_init (void);

guint32 mono_cache_unwind_info (guint8 *unwind_info, guint32 unwind_info_len);

guint8* mono_get_cached_unwind_info (guint32 index, guint32 *unwind_info_len);

guint8* mono_unwind_decode_fde (guint8 *fde, guint32 *out_len, guint32 *code_len, MonoJitExceptionInfo **ex_info, guint32 *ex_info_len, gpointer **type_info, int *this_reg, int *this_offset);

/* Data retrieved from an LLVM Mono FDE entry */
typedef struct {
	guint32 unw_info_len;
	guint32 ex_info_len;
	int type_info_len;
	int this_reg;
	int this_offset;
} MonoLLVMFDEInfo;

void
mono_unwind_decode_llvm_mono_fde (guint8 *fde, int fde_len, guint8 *cie, guint8 *code, MonoLLVMFDEInfo *res, MonoJitExceptionInfo *ei, gpointer *type_info, guint8 *unw_info);

GSList* mono_unwind_get_cie_program (void);

void mono_print_unwind_info (guint8 *unwind_info, int unwind_info_len);

#endif
