/*
 * ARM CodeGen
 * XScale WirelessMMX extensions
 * Copyright 2002 Wild West Software
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef __WMMX_H__
#define __WMMX_H__ 1

#if 0
#include <arm-codegen.h>
#endif

#if defined(ARM_IASM)
#	define WM_ASM(_expr) ARM_IASM(_expr)
#else
#	define WM_ASM(_expr) __emit (_expr)
#endif

#if defined(ARM_EMIT)
#	define WM_EMIT(p, i) ARM_EMIT(p, i)
#else
#	define WM_EMIT(p, i) 
#endif

enum {
	WM_CC_EQ = 0x0,
	WM_CC_NE = 0x1,
	WM_CC_CS = 0x2,
	WM_CC_HS = WM_CC_CS,
	WM_CC_CC = 0x3,
	WM_CC_LO = WM_CC_CC,
	WM_CC_MI = 0x4,
	WM_CC_PL = 0x5,
	WM_CC_VS = 0x6,
	WM_CC_VC = 0x7,
	WM_CC_HI = 0x8,
	WM_CC_LS = 0x9,
	WM_CC_GE = 0xA,
	WM_CC_LT = 0xB,
	WM_CC_GT = 0xC,
	WM_CC_LE = 0xD,
	WM_CC_AL = 0xE,
	WM_CC_NV = 0xF,
	WM_CC_SHIFT = 28
};

#if defined(ARM_DEF_COND)
#	define WM_DEF_CC(_cc) ARM_DEF_COND(_cc)
#else
#	define WM_DEF_CC(_cc) ((_cc & 0xF) << WM_CC_SHIFT)
#endif


enum {
	WM_R0	= 0x0,
	WM_R1	= 0x1,
	WM_R2	= 0x2,
	WM_R3	= 0x3,
	WM_R4	= 0x4,
	WM_R5	= 0x5,
	WM_R6	= 0x6,
	WM_R7	= 0x7,
	WM_R8	= 0x8,
	WM_R9	= 0x9,
	WM_R10	= 0xA,
	WM_R11	= 0xB,
	WM_R12	= 0xC,
	WM_R13	= 0xD,
	WM_R14	= 0xE,
	WM_R15	= 0xF,

	WM_wR0	= 0x0,
	WM_wR1	= 0x1,
	WM_wR2	= 0x2,
	WM_wR3	= 0x3,
	WM_wR4	= 0x4,
	WM_wR5	= 0x5,
	WM_wR6	= 0x6,
	WM_wR7	= 0x7,
	WM_wR8	= 0x8,
	WM_wR9	= 0x9,
	WM_wR10	= 0xA,
	WM_wR11	= 0xB,
	WM_wR12	= 0xC,
	WM_wR13	= 0xD,
	WM_wR14	= 0xE,
	WM_wR15	= 0xF
};


/*
 * Qualifiers:
 *	H - 16-bit (HalfWord) SIMD
 *	W - 32-bit (Word) SIMD
 *	D - 64-bit (Double)
 */
enum {
	WM_B = 0,
	WM_H = 1,
	WM_D = 2
};

/*
 * B.2.3 Transfers From Coprocessor Register (MRC)
 * Table B-5
 */
enum {
	WM_TMRC_OP2      = 0,
	WM_TMRC_CPNUM    = 1,

	WM_TMOVMSK_OP2   = 1,
	WM_TMOVMSK_CPNUM = 0,

	WM_TANDC_OP2     = 1,
	WM_TANDC_CPNUM   = 1,

	WM_TORC_OP2      = 2,
	WM_TORC_CPNUM    = 1,

	WM_TEXTRC_OP2    = 3,
	WM_TEXTRC_CPNUM  = 1,

	WM_TEXTRM_OP2    = 3,
	WM_TEXTRM_CPNUM  = 0
};


/*
 * TANDC<B,H,W>{Cond} R15
 * Performs AND across the fields of the SIMD PSR register (wCASF) and sends the result
 * to CPSR; can be performed after a Byte, Half-word or Word operation that sets the flags.
 * NOTE: R15 is omitted from the macro declaration;
 */
#define DEF_WM_TNADC_CC(_q, _cc) WM_DEF_CC((_cc)) + ((_q) << 0x16) + 0xE13F130

#define _WM_TNADC_CC(_q, _cc) WM_ASM(DEF_WM_TNADC_CC(_q, _cc))
#define ARM_WM_TNADC_CC(_p, _q, _cc) WM_EMIT(_p, DEF_WM_TNADC_CC(_q, _cc))

/* inline assembly */
#define _WM_TNADC(_q) _WM_TNADC_CC((_q), WM_CC_AL)
#define _WM_TNADCB() _WM_TNADC(WM_B)
#define _WM_TNADCH() _WM_TNADC(WM_H)
#define _WM_TNADCD() _WM_TNADC(WM_D)

/* codegen */
#define ARM_WM_TNADC(_p, _q) ARM_WM_TNADC_CC((_p), (_q), WM_CC_AL)
#define ARM_WM_TNADCB(_p) ARM_WM_TNADC(_p, WM_B)
#define ARM_WM_TNADCH(_p) ARM_WM_TNADC(_p, WM_H)
#define ARM_WM_TNADCD(_p) ARM_WM_TNADC(_p, WM_D)


/*
 * TBCST<B,H,W>{Cond} wRd, Rn
 * Broadcasts a value from the ARM Source reg (Rn) to every SIMD position
 * in the WMMX Destination reg (wRd).
 */
#define DEF_WM_TBCST_CC(_q, _cc, _wrd, _rn) \
	WM_DEF_CC((_cc)) + ((_q) << 6) + ((_wrd) << 16) + ((_rn) << 12) + 0xE200010

#define _WM_TBCST_CC(_q, _cc, _wrd, _rn) WM_ASM(DEF_WM_TBCST_CC(_q, _cc, _wrd, _rn))
#define ARM_WM_TBCST_CC(_p, _q, _cc, _wrd, _rn) WM_EMIT(_p, DEF_WM_TBCST_CC(_q, _cc, _wrd, _rn))

/* inline */
#define _WM_TBCST(_q, _wrd, _rn) _WM_TBCST_CC(_q, WM_CC_AL, _wrd, _rn)
#define _WM_TBCSTB(_wrd, _rn) _WM_TBCST(WM_B)
#define _WM_TBCSTH(_wrd, _rn) _WM_TBCST(WM_H)
#define _WM_TBCSTD(_wrd, _rn) _WM_TBCST(WM_D)

/* codegen */
#define ARM_WM_TBCST(_p, _q, _wrd, _rn) ARM_WM_TBCST_CC(_p, _q, WM_CC_AL, _wrd, _rn)
#define ARM_WM_TBCSTB(_p, _wrd, _rn) _WM_TBCST(_p, WM_B)
#define ARM_WM_TBCSTH(_p, _wrd, _rn) _WM_TBCST(_p, WM_H)
#define ARM_WM_TBCSTD(_p, _wrd, _rn) _WM_TBCST(_p, WM_D)


#endif /* __WMMX_H__ */
