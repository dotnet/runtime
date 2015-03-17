#ifndef __MONO_SUPPORT_S390X_H__
#define __MONO_SUPPORT_S390X_H__

typedef struct __FACLIST__ {
	uint8_t	n3:1;		// 000 - N3 instructions
	uint8_t	zi:1;		// 001 - z/Arch installed
	uint8_t	za:1;		// 002 - z/Arch active
	uint8_t	date:1;		// 003 - DAT-enhancement
	uint8_t idtes:1;	// 004 - IDTE-segment tables
	uint8_t	idter:1;	// 005 - IDTE-region tables
	uint8_t	asnlx:1;	// 006 - ASN-LX reuse
	uint8_t	stfle:1;	// 007 - STFLE
	uint8_t	edat1:1;	// 008 - EDAT 1
	uint8_t	srs:1;		// 009 - Sense-Running-Status
	uint8_t	csske:1;	// 010 - Conditional SSKE
	uint8_t	ctf:1;		// 011 - Configuration-topology
	uint8_t ibm01:1;	// 012 - Assigned to IBM
	uint8_t	ipter:1;	// 013 - IPTE-range
	uint8_t	nqks:1;		// 014 - Nonquiescing key-setting
	uint8_t	ibm02:1;	// 015 - Assigned to IBM
	uint8_t etf2:1;		// 016 - Extended translation 2
	uint8_t	msa:1;		// 017 - Message security assist 1
	uint8_t	ld:1;		// 018 - Long displacement
	uint8_t	ldh:1;		// 019 - Long displacement high perf
	uint8_t	mas:1;		// 020 - HFP multiply-add-subtract
	uint8_t	eif:1;		// 021 - Extended immediate
	uint8_t	etf3:1;		// 022 - Extended translation 3
	uint8_t	hux:1;		// 023 - HFP unnormalized extension
	uint8_t	etf2e:1;	// 024 - Extended translation enhanced 2
	uint8_t	stckf:1;	// 025 - Store clock fast
	uint8_t	pe:1;		// 026 - Parsing enhancement
	uint8_t	mvcos:1;	// 027 - Move with optional specs
	uint8_t	tods:1;		// 028 - TOD steering
	uint8_t x000:1;		// 029 - Undefined
	uint8_t	etf3e:1;	// 030 - ETF3 enhancement
	uint8_t	ecput:1;	// 031 - Extract CPU time
	uint8_t	csst:1;		// 032 - Compare swap and store
	uint8_t	csst2:1;	// 033 - Compare swap and store 2
	uint8_t	gie:1;		// 034 - General instructions extension
	uint8_t	ee:1;		// 035 - Execute extensions
	uint8_t	em:1;		// 036 - Enhanced monitor
	uint8_t	fpe:1;		// 037 - Floating point extension
	uint8_t	x001:1;		// 038 - Undefined
	uint8_t	ibm03:1;	// 039 - Assigned to IBM
	uint8_t	spp:1;		// 040 - Set program parameters
	uint8_t	fpse:1;		// 041 - FP support enhancement
	uint8_t	dfp:1;		// 042 - DFP
	uint8_t	dfph:1;		// 043 - DFP high performance
	uint8_t	pfpo:1;		// 044 - PFPO instruction
	uint8_t	multi:1;	// 045 - Multiple inc load/store on CC 1
	uint8_t	ibm04:1;	// 046 - Assigned to IBM
	uint8_t cmpsce:1;	// 047 - CMPSC enhancement
	uint8_t	dfpzc:1;	// 048 - DFP zoned conversion
	uint8_t	misc:1;		// 049 - Multiple inc load and trap
	uint8_t	ctx:1;		// 050 - Constrained transactional-execution
	uint8_t	ltlb:1;		// 051 - Local TLB clearing
	uint8_t	ia:1;		// 052 - Interlocked access
	uint8_t	lsoc2:1;	// 053 - Load/store on CC 2
	uint8_t	x002:1;		// 054 - Undefined
	uint8_t	ibm05:1;	// 055 - Assigned to IBM
	uint8_t	x003:1;		// 056 - Undefined
	uint8_t	msa5:1;		// 057 - Message security assist 5
	uint8_t	x004:1;		// 058 - Undefined
	uint8_t	x005:1;		// 059 - Undefined
	uint8_t	x006:1;		// 060 - Undefined
	uint8_t	x007:1;		// 061 - Undefined
	uint8_t	ibm06:1;	// 062 - Assigned to IBM
	uint8_t	x008:1;		// 063 - Undefined
	uint8_t	x009:1;		// 064 - Undefined
	uint8_t	ibm07:1;	// 065 - Assigned to IBM
	uint8_t	rrbm:1;		// 066 - Reset reference bits multiple
	uint8_t	cmc:1;		// 067 - CPU measurement counter
	uint8_t	cms:1;		// 068 - CPU Measurement sampling
	uint8_t	ibm08:1;	// 069 - Assigned to IBM
	uint8_t	ibm09:1;	// 070 - Assigned to IBM
	uint8_t	ibm10:1;	// 071 - Assigned to IBM
	uint8_t	ibm11:1;	// 072 - Assigned to IBM
	uint8_t	txe:1;		// 073 - Transactional execution
	uint8_t	sthy:1;		// 074 - Store hypervisor information
	uint8_t	aefsi:1;	// 075 - Access exception fetch/store indication
	uint8_t	msa3:1;		// 076 - Message security assist 3
	uint8_t	msa4:1;		// 077 - Message security assist 4
	uint8_t	edat2:1;	// 078 - Enhanced DAT 2
	uint8_t	x010:1;		// 079 - Undefined
	uint8_t dfppc:1;	// 080 - DFP packed conversion
	uint8_t x011:7; 	// 081-87 - Undefined
	uint8_t x012[5];	// 088-127 - Undefined
	uint8_t ibm12:1;	// 128 - Assigned to IBM
	uint8_t	vec:1;		// 129 - Vector facility
	uint8_t	x013:6;		// 130-135 - Undefined
	uint8_t x014:6;		// 136-141 - Undefined
	uint8_t	sccm:1;		// 142 - Store CPU counter multiple
	uint8_t ibm13:1;	// 143 - Assigned to IBM
	uint8_t x015[14];	// 144-256 Undefined
} __attribute__ ((packed)) __attribute__ ((aligned(8))) facilities_t;

#define S390_SET(loc, dr, v)					\
	do {							\
		guint64 val = (guint64) v;			\
		if (s390_is_imm16(val)) {			\
			s390_lghi(loc, dr, val);		\
		} else if (s390_is_uimm16(val)) {		\
			s390_llill(loc, dr, val);		\
		} else if (s390_is_imm32(val)) {		\
			s390_lgfi(loc, dr, val);		\
		} else if (s390_is_uimm32(val)) {		\
			s390_llilf(loc, dr, val);		\
		} else {					\
			guint32 hi = (val) >> 32;		\
			guint32 lo = (val) & 0xffffffff;	\
			s390_iihf(loc, dr, hi);			\
			s390_iilf(loc, dr, lo);			\
		}						\
	} while (0)

#define S390_LONG(loc, opy, op, r, ix, br, off)				\
	if (s390_is_imm20(off)) {					\
		s390_##opy (loc, r, ix, br, off);			\
	} else {							\
		if (ix == 0) {						\
			S390_SET(loc, s390_r13, off);			\
			s390_la (loc, s390_r13, s390_r13, br, 0);	\
		} else {						\
			s390_la   (loc, s390_r13, ix, br, 0);		\
			S390_SET  (loc, s390_r0, off);			\
			s390_agr  (loc, s390_r13, s390_r0);		\
		}							\
		s390_##op (loc, r, 0, s390_r13, 0);			\
	}

#define S390_SET_MASK(loc, dr, v)				\
	do {							\
		if (s390_is_imm16 (v)) {			\
			s390_lghi (loc, dr, v);			\
		} else if (s390_is_imm32 (v)) {			\
			s390_lgfi (loc, dr, v);			\
		} else {					\
			gint64 val = (gint64) v;		\
			guint32 hi = (val) >> 32;		\
			guint32 lo = (val) & 0xffffffff;	\
			s390_iilf(loc, dr, lo);			\
			s390_iihf(loc, dr, hi);			\
		}						\
	} while (0)

#define S390_CALL_TEMPLATE(loc, r)				\
	do {							\
		s390_iihf (loc, r, 0);				\
		s390_iilf (loc, r, 0);				\
		s390_basr (loc, s390_r14, r);			\
	} while (0)

#define S390_BR_TEMPLATE(loc, r)				\
	do {							\
		s390_iihf (loc, r, 0);				\
		s390_iilf (loc, r, 0);				\
		s390_br   (loc, r);				\
	} while (0)

#define S390_LOAD_TEMPLATE(loc, r)				\
	do {							\
		s390_iihf (loc, r, 0);				\
		s390_iilf (loc, r, 0);				\
	} while (0)

#define S390_EMIT_CALL(loc, t)					\
	do {							\
		gint64 val = (gint64) t;			\
		guint32 hi = (val) >> 32;			\
		guint32 lo = (val) & 0xffffffff;		\
		uintptr_t p = (uintptr_t) loc;			\
		p += 2;						\
		*(guint32 *) p = hi;				\
		p += 6;						\
		*(guint32 *) p = lo;				\
	} while (0)

#define S390_EMIT_LOAD(loc, v)					\
	do {							\
		gint64 val = (gint64) v;			\
		guint32 hi = (val) >> 32;			\
		guint32 lo = (val) & 0xffffffff;		\
		uintptr_t p = (uintptr_t) loc;			\
		p += 2;						\
		*(guint32 *) p = hi;				\
		p += 6;						\
		*(guint32 *) p = lo;				\
	} while (0)

#endif	/* __MONO_SUPPORT_S390X_H__ */
