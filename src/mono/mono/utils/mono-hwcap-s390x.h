#ifndef __MONO_UTILS_HWCAP_S390X_H__
#define __MONO_UTILS_HWCAP_S390X_H__

#include "mono/utils/mono-hwcap.h"

typedef struct
{
	char	n3:1;		// N3 instructions present
	char	zArch:1;	// z/Architecture mode installed
	char	zAct:1;		// z/Architecture mode active
	char	date:1;		// DAT enhancement facility
	char	idte1:1;	// IDTE present (PST)
	char	idte2:1;	// IDTE present (REG)
	char	asnlx:1;	// ASN and LX reuse facility
	char	stfle:1;	// STFLE installed
	char	zDATe:1;	// Enhanced DAT in z mode
	char	srstat:1;	// Sense running status facility
	char	cSSKE:1;	// Conditional SSKE facility
	char	topo:1;		// Configuration topology facility
	char	rv1:1;		// Reserved
	char	xTrans2:1;	// Extended translation facility 2
	char	msgSec:1;	// Message security facility
	char	longDsp:1;	// Long displacement facility
	char	hiPerfLD:1;	// High performance long displacement facility
	char	hfpMAS:1;	// HFP multiply-and-add/subtrace facility
	char	xImm:1;		// Extended immediate facility
	char	xTrans3:1;	// Extended translation facility 3
	char	hfpUnX:1;	// HFP unnormalized extension facility
	char	etf2:1;		// ETF2-enhancement facility
	char	stckf:1;	// Store-clock-fast facility
	char	parse:1;	// Parsing enhancement facility
	char	mvcos:1;	// MVCOS facility
	char	todSteer:1;	// TOD-clock steering facility
	char	etf3:1;		// ETF3-enhancement facility
	char	xCPUtm:1;	// Extract CPU time facility
	char	csst:1;		// Compare-swap-and-store facility
	char	csst2:1;	// Compare-swap-and-store facility 2
	char	giX:1;		// General instructions extension facility
	char	exX:1;		// Execute extensions facility
	char	em:1;		// Enhanced monitor
	char	rv2:1;		// Reserved
	char	spp:1;		// Set program parameters
	char	fps:1;		// Floating point support enhancement
	char	dfp:1;		// Decimal floating point facility
	char	hiDFP:1;	// High Performance DFP facility
	char	pfpo:1;		// PFPO instruction facility
	char    doclpkia:1;	// DO/Fast BCR/CL/PK/IA
	char    rv3:1;		// Reserved
	char	cmpsce:1;	// CMPSC enhancement
	char	dfpzc:1;	// DFP zoned-conversion
	char	eh:1;		// Execution hint
	char	lt:1;		// Load and trap
	char	mi:1;		// Miscellaneous instruction enhancements
	char	pa:1;		// Processor assist
	char	cx:1;		// Constrained transactional execution
	char	ltlb:1;		// Local TLB clearing
	char	ia2:1;		// Interlocked access 2
	char	rv4:1;		// Reserved;
	char	rv5:1;		// Reserved;
	char	rv6:1;		// Reserved;
	char	rv7:1;		// Reserved;
	char	rv8:1;		// Reserved;
	char	rv9:1;		// Reserved;
	char	rva:1;		// Reserved;
	char	rvb:1;		// Reserved;
	char	rvc:1;		// Reserved;
	char	rvd:1;		// Reserved;
	char	rve:1;		// Reserved;
	char	rvf:1;		// Reserved;
	char	rvg:1;		// Reserved;
	char	rb:1;		// RRB multiple
	char	cmc:1;		// CPU measurement counter
	char	cms:1;		// CPU measurement sampling
	char	rvh:4;		// Reserved
	char	tx:1;		// Transactional execution
	char	rvi:1;		// Reserved
	char	axsi:1;		// Access exception/store indication
	char	m3:1;		// Message security extension 3
	char	m4:1;		// Message security extension 4
	char	ed2:1;		// Enhanced DAT 2
	int64_t end[0];		// End on a double word
} __attribute__((aligned(8))) facilityList_t;
	
extern facilityList_t facs;

#endif /* __MONO_UTILS_HWCAP_S390X_H__ */
