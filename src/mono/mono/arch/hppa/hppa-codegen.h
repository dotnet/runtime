typedef enum {
	hppa_r0 = 0,
	hppa_r1,
	hppa_r2,
	hppa_rp = hppa_r2,
	hppa_r3,
	hppa_r4,
	hppa_r5,
	hppa_r6,
	hppa_r7,
	hppa_r8,
	hppa_r9,
	hppa_r10,
	hppa_r11,
	hppa_r12,
	hppa_r13,
	hppa_r14,
	hppa_r15,
	hppa_r16,
	hppa_r17,
	hppa_r18,
	hppa_r19,
	hppa_r20,
	hppa_r21,
	hppa_r22,
	hppa_r23,
	hppa_r24,
	hppa_r25,
	hppa_r26,
	hppa_r27,
	hppa_r28,
	hppa_r29,
	hppa_ap = hppa_r29,
	hppa_r30,
	hppa_sp = hppa_r30,
	hppa_r31
} HPPAIntRegister;

#define hppa_nop(p); \
	do { \
		*(p) = 0x08000240; \
		p++; \
	} while (0)

#define hppa_ldb(p, disp, base, dest); \
	do { \
	        int neg = (disp) < 0; \
		*(p) = (0x40000000 | (((disp) & 0x1fff) << 1) | ((base) << 21) | ((dest) << 16) | neg); \
		p++; \
	} while (0)

#define hppa_stb(p, src, disp, base) \
	do { \
	        int neg = (disp) < 0; \
		*(p) = (0x60000000 | (((disp) & 0x1fff) << 1) | ((base) << 21) | ((src) << 16) | neg); \
		p++; \
	} while (0)

#define hppa_ldh(p, disp, base, dest) \
	do { \
	        int neg = (disp) < 0; \
		g_assert(((disp) & 1) == 0); \
		*(p) = (0x44000000 | (((disp) & 0x1fff) << 1) | ((base) << 21) | ((dest) << 16) | neg); \
		p++; \
	} while (0)

#define hppa_sth(p, src, disp, base) \
	do { \
	        int neg = (disp) < 0; \
		g_assert(((disp) & 1) == 0); \
		*(p) = (0x64000000 | (((disp) & 0x1fff) << 1) | ((base) << 21) | ((src) << 16) | neg); \
		p++; \
	} while (0)

#define hppa_ldw(p, disp, base, dest) \
	do { \
	        int neg = (disp) < 0; \
		g_assert(((disp) & 3) == 0); \
		*(p) = (0x48000000 | (((disp) & 0x1fff) << 1) | ((base) << 21) | ((dest) << 16) | neg); \
		p++; \
	} while (0)

#define hppa_stw(p, src, disp, base) \
	do { \
	        int neg = (disp) < 0; \
		g_assert(((disp) & 3) == 0); \
		*(p) = (0x68000000 | (((disp) & 0x1fff) << 1) | ((base) << 21) | ((src) << 16) | neg); \
		p++; \
	} while (0)

#define hppa_copy(p, src, dest) \
	do { \
		*(p) = (0x34000000 | ((src) << 21) | ((dest) << 16)); \
		p++; \
	} while (0)

#define hppa_ldd_with_flags(p, disp, base, dest, m, a) \
	do { \
	        int neg = (disp) < 0; \
		int im10a = (disp) >> 3; \
		g_assert(((disp) & 7) == 0); \
		*(p) = (0x50000000 | (((im10a) & 0x3ff) << 4) | ((base) << 21) | ((dest) << 16) | neg | (m ? 0x8 : 0) | (a ? 0x4 : 0)); \
		p++; \
	} while (0)

#define hppa_ldd(p, disp, base, dest) \
	hppa_ldd_with_flags(p, disp, base, dest, 0, 0)

#define hppa_ldd_mb(p, disp, base, dest) \
	hppa_ldd_with_flags(p, disp, base, dest, 1, 1)

#define hppa_std_with_flags(p, src, disp, base, m, a); \
	do { \
	        int neg = (disp) < 0; \
		int im10a = (disp) >> 3; \
		g_assert(((disp) & 7) == 0); \
		*(p) = (0x70000000 | (((im10a) & 0x3ff) << 4) | ((base) << 21) | ((src) << 16) | neg | (m ? 0x8 : 0) | (a ? 0x4 : 0)); \
		p++; \
	} while (0)

#define hppa_std(p, disp, base, dest) \
	hppa_std_with_flags(p, disp, base, dest, 0, 0)

#define hppa_std_ma(p, disp, base, dest) \
	hppa_std_with_flags(p, disp, base, dest, 1, 0)

#define hppa_fldd_with_flags(p, disp, base, dest, m, a) \
	do { \
		int neg = (disp) < 0; \
		int im10a = (disp) >> 2; \
		*(p) = (0x50000002 | (((im10a) & 0x3ff) << 4) | ((base) << 21) | ((dest) << 16) | neg | (m ? 0x8 : 0) | (a ? 0x4 : 0)); \
		p++; \
	} while (0)

#define hppa_fldd(p, disp, base, dest) \
	hppa_fldd_with_flags(p, disp, base, dest, 0, 0)

#define hppa_fstd_with_flags(p, src, disp, base, m, a) \
	do { \
		int neg = (disp) < 0; \
		int im10a = (disp) >> 2; \
		*(p) = (0x70000002 | (((im10a) & 0x3ff) << 4) | ((base) << 21) | ((src) << 16) | neg | (m ? 0x8 : 0) | (a ? 0x4 : 0)); \
		p++; \
	} while (0)

#define hppa_fstd(p, disp, base, dest) \
	hppa_fstd_with_flags(p, disp, base, dest, 0, 0)


#define hppa_fldw_with_flags(p, im11a, base, dest, r) \
	do { \
		int neg = (disp) < 0; \
		int im11a = (disp) >> 2; \
		*(p) = (0x5c000000 | (((im11a) & 0x7ff) << 3) | ((base) << 21) | ((dest) << 16) | neg | ((r) ? 0x2 : 0)); \
		p++; \
	} while (0)

#define hppa_fldw(p, disp, base, dest) \
	hppa_fldw_with_flags(p, disp, base, dest, 1)

#define hppa_fstw_with_flags(p, src, disp, base, r) \
	do { \
		int neg = (disp) < 0; \
		int im11a = (disp) >> 2; \
		*(p) = (0x7c000000 | (((im11a) & 0x7ff) << 3) | ((base) << 21) | ((src) << 16) | neg | ((r) ? 0x2 : 0)); \
		p++; \
	} while (0)

#define hppa_fstw(p, src, disp, base) \
	hppa_fstw_with_flags(p, src, disp, base, 1)

/* only works on right half SP registers */
#define hppa_fcnv(p, src, ssng, dest, dsng) \
	do { \
		*(p) = (0x38000200 | ((src) << 21) | ((ssng) ? 0x80 : 0x800) | (dest) | ((dsng) ? 0x40 : 0x2000)); \
		p++; \
	} while (0)

#define hppa_fcnv_sng_dbl(p, src, dest) \
	hppa_fcnv(p, src, 1, dest, 0)

#define hppa_fcnv_dbl_sng(p, src, dest) \
	hppa_fcnv(p, src, 0, dest, 1)

#define hppa_ldil(p, val, dest) \
	do { \
		unsigned int t = (val >> 11) & 0x1fffff; \
		unsigned int im21 = ((t & 0x7c) << 14) | ((t & 0x180) << 7) | ((t & 0x3) << 12) | ((t & 0xffe00) >> 8) | ((t & 0x100000) >> 20); \
		*(p) = (0x20000000 | im21 | ((dest) << 21)); \
		p++; \
	} while (0)

#define hppa_ldo(p, off, base, dest) \
	do { \
		int neg = (off) < 0; \
		*(p) = (0x34000000 | (((off) & 0x1fff)) << 1 | ((base) << 21) | ((dest) << 16) | neg); \
		p++; \
	} while (0)

#define hppa_extrdu(p, src, pos, len, dest) \
	do { \
		*(p) = (0xd8000000 | ((src) << 21) | ((dest) << 16) | ((pos) > 32 ? 0x800 : 0) | (((pos) & 31) << 5) | ((len) > 32 ? 0x1000 : 0) | (32 - (len & 31))); \
		p++; \
	} while (0)

#define hppa_bve(p, reg, link) \
	do { \
		*(p) = (0xE8001000 | ((link ? 7 : 6) << 13) | ((reg) << 21)); \
		p++; \
	} while (0)

#define hppa_blve(p, reg) \
	hppa_bve(p, reg, 1)
