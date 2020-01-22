/**
 * \file
 * S/390x hardware feature detection
 *
 * Authors:
 *    Alex Rønne Petersen (alexrp@xamarin.com)
 *    Elijah Taylor (elijahtaylor@google.com)
 *    Miguel de Icaza (miguel@xamarin.com)
 *    Neale Ferguson (Neale.Ferguson@SoftwareAG-usa.com)
 *    Paolo Molaro (lupus@xamarin.com)
 *    Rodrigo Kumpera (kumpera@gmail.com)
 *    Sebastien Pouliot (sebastien@xamarin.com)
 *    Zoltan Varga (vargaz@xamarin.com)
 *
 * Copyright 2003 Ximian, Inc.
 * Copyright 2003-2011 Novell, Inc
 * Copyright 2006 Broadcom
 * Copyright 2007-2008 Andreas Faerber
 * Copyright 2011-2013 Xamarin Inc
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "mono/utils/mono-hwcap.h"

#include <signal.h>

typedef struct {
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
	uint8_t	opcf:1;		// 038 - Order-preserving-compression facility
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
	uint8_t	eecf:1;		// 054 - Entropy-encoding compression facility
	uint8_t	ibm05:1;	// 055 - Assigned to IBM
	uint8_t	x003:1;		// 056 - Undefined
	uint8_t	msa5:1;		// 057 - Message security assist 5
	uint8_t	mie2:1;		// 058 - Miscellaneous execution facility 2
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
	uint8_t	iep:1; 		// 130 - Instruction Execution Protection Facility
	uint8_t	sea:1; 		// 131 - Side-effect-access Faility
	uint8_t	x013:1;		// 132 - Undefined
	uint8_t	gs:1;  		// 133 - Guarded Storage Facility
	uint8_t	vpd:1;		// 134 - Vector Packed Decimal Facility
	uint8_t	ve1:1;		// 135 - Vector Enhancements Facilityty
	uint8_t x014:2;		// 136-137 - Undefined
	uint8_t cazm:1;		// 138 - Configuration-z/Architecture-arcitectural -mode Faciliy
	uint8_t mef:1; 		// 139 - Multiple-epoch Facility ture-arcitectural -mode Faciliy
	uint8_t ibm13:2;	// 140-141 - Assigned to IBM
	uint8_t	sccm:1;		// 142 - Store CPU counter multiple
	uint8_t x015:1; 	// 143 - Assigned to IBM
	uint8_t tpei:1; 	// 144 - Test Pending External Interrption Facility
	uint8_t irbm:1; 	// 145 - Insert Reference Bits Multiple Facility
	uint8_t mse8:1; 	// 146 - Message Security Assist Extension 8
	uint8_t ibm14:1;	// 147 - Reserved for IBM use
	uint8_t x016:4; 	// 148-151 - Undefined
	uint8_t x017[2];	// 152-167 - Undefined
	uint8_t esac:1; 	// 168 - ESA/390 Compatibility Mode Facility
	uint8_t x018:7;  	// 169-175 - Undefined
	uint8_t x019[10];	// 176-256 Undefined
} __attribute__ ((__packed__)) __attribute__ ((__aligned__(8))) facilityList_t;

void
mono_hwcap_arch_init (void)
{
	facilityList_t facs;
	int lFacs = sizeof (facs) / 8;

	__asm__ __volatile__ (
		"lgfr\t0,%1\n\t"
		".insn\ts,0xb2b00000,%0\n\t"
		: "=m" (facs)
		: "r" (lFacs)
		: "0", "cc"
	);

	mono_hwcap_s390x_has_fpe  = facs.fpe;
	mono_hwcap_s390x_has_vec  = facs.vec;
	mono_hwcap_s390x_has_mlt  = facs.multi;
	mono_hwcap_s390x_has_ia   = facs.ia;
	mono_hwcap_s390x_has_gie  = facs.gie;
	mono_hwcap_s390x_has_mie2 = facs.mie2;
	mono_hwcap_s390x_has_gs   = facs.gs;
}
