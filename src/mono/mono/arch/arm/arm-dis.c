/*
 * Copyright (c) 2002 Sergey Chaban <serge@wildwestsoftware.com>
 */


#include <stdarg.h>

#include "arm-dis.h"
#include "arm-codegen.h"


static ARMDis* gdisasm = NULL;

static int use_reg_alias = 1;

const static char* cond[] = {
	"eq", "ne", "cs", "cc", "mi", "pl", "vs", "vc",
	"hi", "ls", "ge", "lt", "gt", "le", "", "nv"
};

const static char* ops[] = {
	"and", "eor", "sub", "rsb", "add", "adc", "sbc", "rsc",
	"tst", "teq", "cmp", "cmn", "orr", "mov", "bic", "mvn"
};

const static char* shift_types[] = {"lsl", "lsr", "asr", "ror"};

const static char* mul_ops[] = {
	"mul", "mla", "?", "?", "umull", "umlal", "smull", "smlal"
};

const static char* reg_alias[] = {
	"a1", "a2", "a3", "a4",
	"r4", "r5", "r6", "r7", "r8", "r9", "r10",
	"fp", "ip", "sp", "lr", "pc"
};

const static char* msr_fld[] = {"f", "c", "x", "?", "s"};


/* private functions prototypes (to keep compiler happy) */
void chk_out(ARMDis* dis);
void dump_reg(ARMDis* dis, int reg);
void dump_creg(ARMDis* dis, int creg);
void dump_reglist(ARMDis* dis, int reg_list);
void init_gdisasm(void);

void dump_br(ARMDis* dis, ARMInstr i);
void dump_cdp(ARMDis* dis, ARMInstr i);
void dump_cdt(ARMDis* dis, ARMInstr i);
void dump_crt(ARMDis* dis, ARMInstr i);
void dump_dpi(ARMDis* dis, ARMInstr i);
void dump_hxfer(ARMDis* dis, ARMInstr i);
void dump_mrs(ARMDis* dis, ARMInstr i);
void dump_mrt(ARMDis* dis, ARMInstr i);
void dump_msr(ARMDis* dis, ARMInstr i);
void dump_mul(ARMDis* dis, ARMInstr i);
void dump_swi(ARMDis* dis, ARMInstr i);
void dump_swp(ARMDis* dis, ARMInstr i);
void dump_wxfer(ARMDis* dis, ARMInstr i);
void dump_clz(ARMDis* dis, ARMInstr i);


/*
void out(ARMDis* dis, const char* format, ...) {
	va_list arglist;
	va_start(arglist, format);
	fprintf(dis->dis_out, format, arglist);
	va_end(arglist);
}
*/


void chk_out(ARMDis* dis) {
	if (dis != NULL && dis->dis_out == NULL) dis->dis_out = stdout;
}


void armdis_set_output(ARMDis* dis, FILE* f) {
	if (dis != NULL) {
		dis->dis_out = f;
		chk_out(dis);
	}
}

FILE* armdis_get_output(ARMDis* dis) {
	return (dis != NULL ? dis->dis_out : NULL);
}




void dump_reg(ARMDis* dis, int reg) {
	reg &= 0xF;
	if (!use_reg_alias || (reg > 3 && reg < 11)) {
		fprintf(dis->dis_out, "r%d", reg);
	} else {
		fprintf(dis->dis_out, reg_alias[reg]);
	}
}

void dump_creg(ARMDis* dis, int creg) {
	if (dis != NULL) {
		creg &= 0xF;
		fprintf(dis->dis_out, "c%d", creg);
	}
}

void dump_reglist(ARMDis* dis, int reg_list) {
	int i = 0, j, n = 0;
	int m1 = 1, m2, rn;
	while (i < 16) {
		if ((reg_list & m1) != 0) {
			if (n != 0) fprintf(dis->dis_out, ", ");
			n++;
			dump_reg(dis, i);
			for (j = i+1, rn = 0, m2 = m1<<1; j < 16; ++j, m2<<=1) {
				if ((reg_list & m2) != 0) ++rn;
				else break;
			}
			i+=rn;
			if (rn > 1) {
				fprintf(dis->dis_out, "-");
				dump_reg(dis, i);
			} else if (rn == 1) {
				fprintf(dis->dis_out, ", ");
				dump_reg(dis, i);
			}
			m1<<=(rn+1);
			i++;
		} else {
			++i;
			m1<<=1;
		}
	}
}


void dump_br(ARMDis* dis, ARMInstr i) {
	fprintf(dis->dis_out, "b%s%s\t%x\t; %p -> %p",
	    (i.br.link == 1) ? "l" : "",
	    cond[i.br.cond], i.br.offset, dis->pi, (int)dis->pi + 4*2 + ((int)(i.br.offset << 8) >> 6));
}


void dump_dpi(ARMDis* dis, ARMInstr i) {
	fprintf(dis->dis_out, "%s%s", ops[i.dpi.all.opcode], cond[i.dpi.all.cond]);

	if ((i.dpi.all.opcode < ARMOP_TST || i.dpi.all.opcode > ARMOP_CMN) && (i.dpi.all.s != 0)) {
		fprintf(dis->dis_out, "s");
	}

	fprintf(dis->dis_out, "\t");

	if ((i.dpi.all.opcode < ARMOP_TST) || (i.dpi.all.opcode > ARMOP_CMN)) {
		/* for comparison operations Rd is ignored */
		dump_reg(dis, i.dpi.all.rd);
		fprintf(dis->dis_out, ", ");
	}

	if ((i.dpi.all.opcode != ARMOP_MOV) && (i.dpi.all.opcode != ARMOP_MVN)) {
		/* for MOV/MVN Rn is ignored */
		dump_reg(dis, i.dpi.all.rn);
		fprintf(dis->dis_out, ", ");
	}

	if (i.dpi.all.type == 1) {
		/* immediate */
		if (i.dpi.op2_imm.rot != 0) {
			fprintf(dis->dis_out, "#%d, %d\t; 0x%x", i.dpi.op2_imm.imm, i.dpi.op2_imm.rot << 1,
			        ARM_SCALE(i.dpi.op2_imm.imm, (i.dpi.op2_imm.rot << 1)) );
		} else {
			fprintf(dis->dis_out, "#%d\t; 0x%x", i.dpi.op2_imm.imm, i.dpi.op2_imm.imm);
		}
	} else {
		/* reg-reg */
		if (i.dpi.op2_reg.tag == 0) {
			/* op2 is reg shift by imm */
			dump_reg(dis, i.dpi.op2_reg_imm.r2.rm);
			if (i.dpi.op2_reg_imm.imm.shift != 0) {
				fprintf(dis->dis_out, " %s #%d", shift_types[i.dpi.op2_reg_imm.r2.type], i.dpi.op2_reg_imm.imm.shift);
			}
		} else {
			/* op2 is reg shift by reg */
			dump_reg(dis, i.dpi.op2_reg_reg.r2.rm);
			fprintf(dis->dis_out, " %s ", shift_types[i.dpi.op2_reg_reg.r2.type]);
			dump_reg(dis, i.dpi.op2_reg_reg.reg.rs);
		}

	}
}

void dump_wxfer(ARMDis* dis, ARMInstr i) {
	fprintf(dis->dis_out, "%s%s%s%s\t",
		(i.wxfer.all.ls == 0) ? "str" : "ldr",
		cond[i.generic.cond],
		(i.wxfer.all.b == 0) ? "" : "b",
		(i.wxfer.all.ls != 0 && i.wxfer.all.wb != 0) ? "t" : "");
	dump_reg(dis, i.wxfer.all.rd);
	fprintf(dis->dis_out, ", [");
	dump_reg(dis, i.wxfer.all.rn);
	fprintf(dis->dis_out, "%s, ", (i.wxfer.all.p == 0) ? "]" : "");

	if (i.wxfer.all.type == 0) { /* imm */
		fprintf(dis->dis_out, "#%s%d", (i.wxfer.all.u == 0) ? "-" : "", i.wxfer.all.op2_imm);
	} else {
		dump_reg(dis, i.wxfer.op2_reg_imm.r2.rm);
		if (i.wxfer.op2_reg_imm.imm.shift != 0) {
			fprintf(dis->dis_out, " %s #%d", shift_types[i.wxfer.op2_reg_imm.r2.type], i.wxfer.op2_reg_imm.imm.shift);
		}
	}

	if (i.wxfer.all.p != 0) {
		/* close pre-index instr, also check for write-back */
		fprintf(dis->dis_out, "]%s", (i.wxfer.all.wb != 0) ? "!" : "");
	}
}

void dump_hxfer(ARMDis* dis, ARMInstr i) {
	fprintf(dis->dis_out, "%s%s%s%s\t",
		(i.hxfer.ls == 0) ? "str" : "ldr",
		cond[i.generic.cond],
		(i.hxfer.s != 0) ? "s" : "",
		(i.hxfer.h != 0) ? "h" : "b");
	dump_reg(dis, i.hxfer.rd);
	fprintf(dis->dis_out, ", [");
	dump_reg(dis, i.hxfer.rn);
	fprintf(dis->dis_out, "%s, ", (i.hxfer.p == 0) ? "]" : "");

	if (i.hxfer.type != 0) { /* imm */
		fprintf(dis->dis_out, "#%s%d", (i.hxfer.u == 0) ? "-" : "", (i.hxfer.imm_hi << 4) | i.hxfer.rm);
	} else {
		dump_reg(dis, i.hxfer.rm);
	}

	if (i.hxfer.p != 0) {
		/* close pre-index instr, also check for write-back */
		fprintf(dis->dis_out, "]%s", (i.hxfer.wb != 0) ? "!" : "");
	}
}


void dump_mrt(ARMDis* dis, ARMInstr i) {
	fprintf(dis->dis_out, "%s%s%s%s\t", (i.mrt.ls == 0) ? "stm" : "ldm", cond[i.mrt.cond],
	        (i.mrt.u == 0) ? "d" : "i", (i.mrt.p == 0) ? "a" : "b");
	dump_reg(dis, i.mrt.rn);
	fprintf(dis->dis_out, "%s, {", (i.mrt.wb != 0) ? "!" : "");
	dump_reglist(dis, i.mrt.reg_list);
	fprintf(dis->dis_out, "}");
}


void dump_swp(ARMDis* dis, ARMInstr i) {
	fprintf(dis->dis_out, "swp%s%s ", cond[i.swp.cond], (i.swp.b != 0) ? "b" : "");
	dump_reg(dis, i.swp.rd);
	fprintf(dis->dis_out, ", ");
	dump_reg(dis, i.swp.rm);
	fprintf(dis->dis_out, ", [");
	dump_reg(dis, i.swp.rn);
	fprintf(dis->dis_out, "]");
}


void dump_mul(ARMDis* dis, ARMInstr i) {
	fprintf(dis->dis_out, "%s%s%s\t", mul_ops[i.mul.opcode], cond[i.mul.cond], (i.mul.s != 0) ? "s" : "");
	switch (i.mul.opcode) {
	case ARMOP_MUL:
		dump_reg(dis, i.mul.rd);
		fprintf(dis->dis_out, ", ");
		dump_reg(dis, i.mul.rm);
		fprintf(dis->dis_out, ", ");
		dump_reg(dis, i.mul.rs);
		break;
	case ARMOP_MLA:
		dump_reg(dis, i.mul.rd);
		fprintf(dis->dis_out, ", ");
		dump_reg(dis, i.mul.rm);
		fprintf(dis->dis_out, ", ");
		dump_reg(dis, i.mul.rs);
		fprintf(dis->dis_out, ", ");
		dump_reg(dis, i.mul.rn);
		break;
	case ARMOP_UMULL:
	case ARMOP_UMLAL:
	case ARMOP_SMULL:
	case ARMOP_SMLAL:
		dump_reg(dis, i.mul.rd);
		fprintf(dis->dis_out, ", ");
		dump_reg(dis, i.mul.rn);
		fprintf(dis->dis_out, ", ");
		dump_reg(dis, i.mul.rm);
		fprintf(dis->dis_out, ", ");
		dump_reg(dis, i.mul.rs);
		break;
	default:
		fprintf(dis->dis_out, "DCD 0x%x\t; <unknown>", i.raw);
		break;
	}
}


void dump_cdp(ARMDis* dis, ARMInstr i) {
	fprintf(dis->dis_out, "cdp%s\tp%d, %d, ", cond[i.generic.cond], i.cdp.cpn, i.cdp.op);
	dump_creg(dis, i.cdp.crd);
	fprintf(dis->dis_out, ", ");
	dump_creg(dis, i.cdp.crn);
	fprintf(dis->dis_out, ", ");
	dump_creg(dis, i.cdp.crm);

	if (i.cdp.op2 != 0) {
		fprintf(dis->dis_out, ", %d", i.cdp.op2);
	}
}


void dump_cdt(ARMDis* dis, ARMInstr i) {
	fprintf(dis->dis_out, "%s%s%s\tp%d, ", (i.cdt.ls == 0) ? "stc" : "ldc",
	        cond[i.generic.cond], (i.cdt.n != 0) ? "l" : "", i.cdt.cpn);
	dump_creg(dis, i.cdt.crd);
	fprintf(dis->dis_out, ", ");
	dump_reg(dis, i.cdt.rn);

	if (i.cdt.p == 0) {
		fprintf(dis->dis_out, "]");
	}

	if (i.cdt.offs != 0) {
		fprintf(dis->dis_out, ", #%d", i.cdt.offs);
	}

	if (i.cdt.p != 0) {
		fprintf(dis->dis_out, "]%s", (i.cdt.wb != 0) ? "!" : "");
	}
}


void dump_crt(ARMDis* dis, ARMInstr i) {
	fprintf(dis->dis_out, "%s%s\tp%d, %d, ", (i.crt.ls == 0) ? "mrc" : "mcr",
	        cond[i.generic.cond], i.crt.cpn, i.crt.op1);
	dump_reg(dis, i.crt.rd);
	fprintf(dis->dis_out, ", ");
	dump_creg(dis, i.crt.crn);
	fprintf(dis->dis_out, ", ");
	dump_creg(dis, i.crt.crm);

	if (i.crt.op2 != 0) {
		fprintf(dis->dis_out, ", %d", i.crt.op2);
	}
}


void dump_msr(ARMDis* dis, ARMInstr i) {
	fprintf(dis->dis_out, "msr%s\t%spsr_, ", cond[i.generic.cond],
	        (i.msr.all.sel == 0) ? "s" : "c");
	if (i.msr.all.type == 0) {
		/* reg */
		fprintf(dis->dis_out, "%s, ", msr_fld[i.msr.all.fld]);
		dump_reg(dis, i.msr.all.rm);
	} else {
		/* imm */
		fprintf(dis->dis_out, "f, #%d", i.msr.op2_imm.imm << i.msr.op2_imm.rot);
	}
}


void dump_mrs(ARMDis* dis, ARMInstr i) {
	fprintf(dis->dis_out, "mrs%s\t", cond[i.generic.cond]);
	dump_reg(dis, i.mrs.rd);
	fprintf(dis->dis_out, ", %spsr", (i.mrs.sel == 0) ? "s" : "c");
}


void dump_swi(ARMDis* dis, ARMInstr i) {
	fprintf(dis->dis_out, "swi%s\t%d", cond[i.generic.cond], i.swi.num);
}


void dump_clz(ARMDis* dis, ARMInstr i) {
	fprintf(dis->dis_out, "clz%s\t");
	dump_reg(dis, i.clz.rd);
	fprintf(dis->dis_out, ", ");
	dump_reg(dis, i.clz.rm);
	fprintf(dis->dis_out, "\n");
}



void armdis_decode(ARMDis* dis, void* p, int size) {
	int i;
	arminstr_t* pi = (arminstr_t*)p;
	ARMInstr instr;

	if (dis == NULL) return;

	chk_out(dis);

	size/=sizeof(arminstr_t);

	for (i=0; i<size; ++i) {
		fprintf(dis->dis_out, "%p:\t%08x\t", pi, *pi);
		dis->pi = pi;
		instr.raw = *pi++;

		if ((instr.raw & ARM_BR_MASK) == ARM_BR_TAG) {
			dump_br(dis, instr);
		} else if ((instr.raw & ARM_SWP_MASK) == ARM_SWP_TAG) {
			dump_swp(dis, instr);
		} else if ((instr.raw & ARM_MUL_MASK) == ARM_MUL_TAG) {
			dump_mul(dis, instr);
		} else if ((instr.raw & ARM_CLZ_MASK) == ARM_CLZ_TAG) {
			dump_clz(dis, instr);
		} else if ((instr.raw & ARM_WXFER_MASK) == ARM_WXFER_TAG) {
			dump_wxfer(dis, instr);
		} else if ((instr.raw & ARM_HXFER_MASK) == ARM_HXFER_TAG) {
			dump_hxfer(dis, instr);
		} else if ((instr.raw & ARM_DPI_MASK) == ARM_DPI_TAG) {
			dump_dpi(dis, instr);
		} else if ((instr.raw & ARM_MRT_MASK) == ARM_MRT_TAG) {
			dump_mrt(dis, instr);
		} else if ((instr.raw & ARM_CDP_MASK) == ARM_CDP_TAG) {
			dump_cdp(dis, instr);
		} else if ((instr.raw & ARM_CDT_MASK) == ARM_CDT_TAG) {
			dump_cdt(dis, instr);
		} else if ((instr.raw & ARM_CRT_MASK) == ARM_CRT_TAG) {
			dump_crt(dis, instr);
		} else if ((instr.raw & ARM_MSR_MASK) == ARM_MSR_TAG) {
			dump_msr(dis, instr);
		} else if ((instr.raw & ARM_MRS_MASK) == ARM_MRS_TAG) {
			dump_mrs(dis, instr);
		} else if ((instr.raw & ARM_SWI_MASK) == ARM_SWI_TAG) {
			dump_swi(dis, instr);
		} else {
			fprintf(dis->dis_out, "DCD 0x%x\t; <unknown>", instr.raw);
		}

		fprintf(dis->dis_out, "\n");
	}
}


void armdis_open(ARMDis* dis, const char* dump_name) {
	if (dis != NULL && dump_name != NULL) {
		armdis_set_output(dis, fopen(dump_name, "w"));
	}
}


void armdis_close(ARMDis* dis) {
	if (dis->dis_out != NULL && dis->dis_out != stdout && dis->dis_out != stderr) {
		fclose(dis->dis_out);
		dis->dis_out = NULL;
	}
}


void armdis_dump(ARMDis* dis, const char* dump_name, void* p, int size) {
	armdis_open(dis, dump_name);
	armdis_decode(dis, p, size);
	armdis_close(dis);
}


void armdis_init(ARMDis* dis) {
	if (dis != NULL) {
		/* set to stdout */
		armdis_set_output(dis, NULL);
	}
}




void init_gdisasm() {
	if (gdisasm == NULL) {
		gdisasm = (ARMDis*)malloc(sizeof(ARMDis));
		armdis_init(gdisasm);
	}
}

void _armdis_set_output(FILE* f) {
	init_gdisasm();
	armdis_set_output(gdisasm, f);
}

FILE* _armdis_get_output() {
	init_gdisasm();
	return armdis_get_output(gdisasm);
}

void _armdis_decode(void* p, int size) {
	init_gdisasm();
	armdis_decode(gdisasm, p, size);
}

void _armdis_open(const char* dump_name) {
	init_gdisasm();
	armdis_open(gdisasm, dump_name);
}

void _armdis_close() {
	init_gdisasm();
	armdis_close(gdisasm);
}

void _armdis_dump(const char* dump_name, void* p, int size) {
	init_gdisasm();
	armdis_dump(gdisasm, dump_name, p, size);
}

