/* Macros for VFP ops, auto-generated from template */


/* dyadic */

/* -- ADD -- */


/* Fd := Fn ADD Fm */
#define ARM_VFP_ADDD_COND(p, rd, rn, rm, cond) \
	ARM_EMIT((p), ARM_DEF_VFP_DYADIC(cond,ARM_VFP_COPROC_DOUBLE,ARM_VFP_ADD,rd,rn,rm))
#define ARM_VFP_ADDD(p, rd, rn, rm) \
	ARM_VFP_ADDD_COND(p, rd, rn, rm, ARMCOND_AL)

#define ARM_VFP_ADDS_COND(p, rd, rn, rm, cond) \
	ARM_EMIT((p), ARM_DEF_VFP_DYADIC(cond,ARM_VFP_COPROC_SINGLE,ARM_VFP_ADD,rd,rn,rm))
#define ARM_VFP_ADDS(p, rd, rn, rm) \
	ARM_VFP_ADDS_COND(p, rd, rn, rm, ARMCOND_AL)


/* -- SUB -- */


/* Fd := Fn SUB Fm */
#define ARM_VFP_SUBD_COND(p, rd, rn, rm, cond) \
	ARM_EMIT((p), ARM_DEF_VFP_DYADIC(cond,ARM_VFP_COPROC_DOUBLE,ARM_VFP_SUB,rd,rn,rm))
#define ARM_VFP_SUBD(p, rd, rn, rm) \
	ARM_VFP_SUBD_COND(p, rd, rn, rm, ARMCOND_AL)

#define ARM_VFP_SUBS_COND(p, rd, rn, rm, cond) \
	ARM_EMIT((p), ARM_DEF_VFP_DYADIC(cond,ARM_VFP_COPROC_SINGLE,ARM_VFP_SUB,rd,rn,rm))
#define ARM_VFP_SUBS(p, rd, rn, rm) \
	ARM_VFP_SUBS_COND(p, rd, rn, rm, ARMCOND_AL)


/* -- MUL -- */


/* Fd := Fn MUL Fm */
#define ARM_VFP_MULD_COND(p, rd, rn, rm, cond) \
	ARM_EMIT((p), ARM_DEF_VFP_DYADIC(cond,ARM_VFP_COPROC_DOUBLE,ARM_VFP_MUL,rd,rn,rm))
#define ARM_VFP_MULD(p, rd, rn, rm) \
	ARM_VFP_MULD_COND(p, rd, rn, rm, ARMCOND_AL)

#define ARM_VFP_MULS_COND(p, rd, rn, rm, cond) \
	ARM_EMIT((p), ARM_DEF_VFP_DYADIC(cond,ARM_VFP_COPROC_SINGLE,ARM_VFP_MUL,rd,rn,rm))
#define ARM_VFP_MULS(p, rd, rn, rm) \
	ARM_VFP_MULS_COND(p, rd, rn, rm, ARMCOND_AL)


/* -- NMUL -- */


/* Fd := Fn NMUL Fm */
#define ARM_VFP_NMULD_COND(p, rd, rn, rm, cond) \
	ARM_EMIT((p), ARM_DEF_VFP_DYADIC(cond,ARM_VFP_COPROC_DOUBLE,ARM_VFP_NMUL,rd,rn,rm))
#define ARM_VFP_NMULD(p, rd, rn, rm) \
	ARM_VFP_NMULD_COND(p, rd, rn, rm, ARMCOND_AL)

#define ARM_VFP_NMULS_COND(p, rd, rn, rm, cond) \
	ARM_EMIT((p), ARM_DEF_VFP_DYADIC(cond,ARM_VFP_COPROC_SINGLE,ARM_VFP_NMUL,rd,rn,rm))
#define ARM_VFP_NMULS(p, rd, rn, rm) \
	ARM_VFP_NMULS_COND(p, rd, rn, rm, ARMCOND_AL)


/* -- DIV -- */


/* Fd := Fn DIV Fm */
#define ARM_VFP_DIVD_COND(p, rd, rn, rm, cond) \
	ARM_EMIT((p), ARM_DEF_VFP_DYADIC(cond,ARM_VFP_COPROC_DOUBLE,ARM_VFP_DIV,rd,rn,rm))
#define ARM_VFP_DIVD(p, rd, rn, rm) \
	ARM_VFP_DIVD_COND(p, rd, rn, rm, ARMCOND_AL)

#define ARM_VFP_DIVS_COND(p, rd, rn, rm, cond) \
	ARM_EMIT((p), ARM_DEF_VFP_DYADIC(cond,ARM_VFP_COPROC_SINGLE,ARM_VFP_DIV,rd,rn,rm))
#define ARM_VFP_DIVS(p, rd, rn, rm) \
	ARM_VFP_DIVS_COND(p, rd, rn, rm, ARMCOND_AL)



/* monadic */

/* -- CPY -- */


/* Fd := CPY Fm */

#define ARM_CPYD_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_VFP_MONADIC((cond),ARM_VFP_COPROC_DOUBLE,ARM_VFP_CPY,(dreg),(sreg)))
#define ARM_CPYD(p,dreg,sreg)      ARM_CPYD_COND(p,dreg,sreg,ARMCOND_AL)

#define ARM_CPYS_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_VFP_MONADIC((cond),ARM_VFP_COPROC_SINGLE,ARM_VFP_CPY,(dreg),(sreg)))
#define ARM_CPYS(p,dreg,sreg)      ARM_CPYS_COND(p,dreg,sreg,ARMCOND_AL)


/* -- ABS -- */


/* Fd := ABS Fm */

#define ARM_ABSD_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_VFP_MONADIC((cond),ARM_VFP_COPROC_DOUBLE,ARM_VFP_ABS,(dreg),(sreg)))
#define ARM_ABSD(p,dreg,sreg)      ARM_ABSD_COND(p,dreg,sreg,ARMCOND_AL)

#define ARM_ABSS_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_VFP_MONADIC((cond),ARM_VFP_COPROC_SINGLE,ARM_VFP_ABS,(dreg),(sreg)))
#define ARM_ABSS(p,dreg,sreg)      ARM_ABSS_COND(p,dreg,sreg,ARMCOND_AL)


/* -- NEG -- */


/* Fd := NEG Fm */

#define ARM_NEGD_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_VFP_MONADIC((cond),ARM_VFP_COPROC_DOUBLE,ARM_VFP_NEG,(dreg),(sreg)))
#define ARM_NEGD(p,dreg,sreg)      ARM_NEGD_COND(p,dreg,sreg,ARMCOND_AL)

#define ARM_NEGS_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_VFP_MONADIC((cond),ARM_VFP_COPROC_SINGLE,ARM_VFP_NEG,(dreg),(sreg)))
#define ARM_NEGS(p,dreg,sreg)      ARM_NEGS_COND(p,dreg,sreg,ARMCOND_AL)


/* -- SQRT -- */


/* Fd := SQRT Fm */

#define ARM_SQRTD_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_VFP_MONADIC((cond),ARM_VFP_COPROC_DOUBLE,ARM_VFP_SQRT,(dreg),(sreg)))
#define ARM_SQRTD(p,dreg,sreg)      ARM_SQRTD_COND(p,dreg,sreg,ARMCOND_AL)

#define ARM_SQRTS_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_VFP_MONADIC((cond),ARM_VFP_COPROC_SINGLE,ARM_VFP_SQRT,(dreg),(sreg)))
#define ARM_SQRTS(p,dreg,sreg)      ARM_SQRTS_COND(p,dreg,sreg,ARMCOND_AL)


/* -- CMP -- */


/* Fd := CMP Fm */

#define ARM_CMPD_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_VFP_MONADIC((cond),ARM_VFP_COPROC_DOUBLE,ARM_VFP_CMP,(dreg),(sreg)))
#define ARM_CMPD(p,dreg,sreg)      ARM_CMPD_COND(p,dreg,sreg,ARMCOND_AL)

#define ARM_CMPS_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_VFP_MONADIC((cond),ARM_VFP_COPROC_SINGLE,ARM_VFP_CMP,(dreg),(sreg)))
#define ARM_CMPS(p,dreg,sreg)      ARM_CMPS_COND(p,dreg,sreg,ARMCOND_AL)


/* -- CMPE -- */


/* Fd := CMPE Fm */

#define ARM_CMPED_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_VFP_MONADIC((cond),ARM_VFP_COPROC_DOUBLE,ARM_VFP_CMPE,(dreg),(sreg)))
#define ARM_CMPED(p,dreg,sreg)      ARM_CMPED_COND(p,dreg,sreg,ARMCOND_AL)

#define ARM_CMPES_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_VFP_MONADIC((cond),ARM_VFP_COPROC_SINGLE,ARM_VFP_CMPE,(dreg),(sreg)))
#define ARM_CMPES(p,dreg,sreg)      ARM_CMPES_COND(p,dreg,sreg,ARMCOND_AL)


/* -- CMPZ -- */


/* Fd := CMPZ Fm */

#define ARM_CMPZD_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_VFP_MONADIC((cond),ARM_VFP_COPROC_DOUBLE,ARM_VFP_CMPZ,(dreg),(sreg)))
#define ARM_CMPZD(p,dreg,sreg)      ARM_CMPZD_COND(p,dreg,sreg,ARMCOND_AL)

#define ARM_CMPZS_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_VFP_MONADIC((cond),ARM_VFP_COPROC_SINGLE,ARM_VFP_CMPZ,(dreg),(sreg)))
#define ARM_CMPZS(p,dreg,sreg)      ARM_CMPZS_COND(p,dreg,sreg,ARMCOND_AL)


/* -- CMPEZ -- */


/* Fd := CMPEZ Fm */

#define ARM_CMPEZD_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_VFP_MONADIC((cond),ARM_VFP_COPROC_DOUBLE,ARM_VFP_CMPEZ,(dreg),(sreg)))
#define ARM_CMPEZD(p,dreg,sreg)      ARM_CMPEZD_COND(p,dreg,sreg,ARMCOND_AL)

#define ARM_CMPEZS_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_VFP_MONADIC((cond),ARM_VFP_COPROC_SINGLE,ARM_VFP_CMPEZ,(dreg),(sreg)))
#define ARM_CMPEZS(p,dreg,sreg)      ARM_CMPEZS_COND(p,dreg,sreg,ARMCOND_AL)


/* -- CVT -- */


/* Fd := CVT Fm */

#define ARM_CVTD_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_VFP_MONADIC((cond),ARM_VFP_COPROC_DOUBLE,ARM_VFP_CVT,(dreg),(sreg)))
#define ARM_CVTD(p,dreg,sreg)      ARM_CVTD_COND(p,dreg,sreg,ARMCOND_AL)

#define ARM_CVTS_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_VFP_MONADIC((cond),ARM_VFP_COPROC_SINGLE,ARM_VFP_CVT,(dreg),(sreg)))
#define ARM_CVTS(p,dreg,sreg)      ARM_CVTS_COND(p,dreg,sreg,ARMCOND_AL)


/* -- UITO -- */


/* Fd := UITO Fm */

#define ARM_UITOD_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_VFP_MONADIC((cond),ARM_VFP_COPROC_DOUBLE,ARM_VFP_UITO,(dreg),(sreg)))
#define ARM_UITOD(p,dreg,sreg)      ARM_UITOD_COND(p,dreg,sreg,ARMCOND_AL)

#define ARM_UITOS_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_VFP_MONADIC((cond),ARM_VFP_COPROC_SINGLE,ARM_VFP_UITO,(dreg),(sreg)))
#define ARM_UITOS(p,dreg,sreg)      ARM_UITOS_COND(p,dreg,sreg,ARMCOND_AL)


/* -- SITO -- */


/* Fd := SITO Fm */

#define ARM_SITOD_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_VFP_MONADIC((cond),ARM_VFP_COPROC_DOUBLE,ARM_VFP_SITO,(dreg),(sreg)))
#define ARM_SITOD(p,dreg,sreg)      ARM_SITOD_COND(p,dreg,sreg,ARMCOND_AL)

#define ARM_SITOS_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_VFP_MONADIC((cond),ARM_VFP_COPROC_SINGLE,ARM_VFP_SITO,(dreg),(sreg)))
#define ARM_SITOS(p,dreg,sreg)      ARM_SITOS_COND(p,dreg,sreg,ARMCOND_AL)


/* -- TOUI -- */


/* Fd := TOUI Fm */

#define ARM_TOUID_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_VFP_MONADIC((cond),ARM_VFP_COPROC_DOUBLE,ARM_VFP_TOUI,(dreg),(sreg)))
#define ARM_TOUID(p,dreg,sreg)      ARM_TOUID_COND(p,dreg,sreg,ARMCOND_AL)

#define ARM_TOUIS_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_VFP_MONADIC((cond),ARM_VFP_COPROC_SINGLE,ARM_VFP_TOUI,(dreg),(sreg)))
#define ARM_TOUIS(p,dreg,sreg)      ARM_TOUIS_COND(p,dreg,sreg,ARMCOND_AL)


/* -- TOSI -- */


/* Fd := TOSI Fm */

#define ARM_TOSID_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_VFP_MONADIC((cond),ARM_VFP_COPROC_DOUBLE,ARM_VFP_TOSI,(dreg),(sreg)))
#define ARM_TOSID(p,dreg,sreg)      ARM_TOSID_COND(p,dreg,sreg,ARMCOND_AL)

#define ARM_TOSIS_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_VFP_MONADIC((cond),ARM_VFP_COPROC_SINGLE,ARM_VFP_TOSI,(dreg),(sreg)))
#define ARM_TOSIS(p,dreg,sreg)      ARM_TOSIS_COND(p,dreg,sreg,ARMCOND_AL)


/* -- TOUIZ -- */


/* Fd := TOUIZ Fm */

#define ARM_TOUIZD_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_VFP_MONADIC((cond),ARM_VFP_COPROC_DOUBLE,ARM_VFP_TOUIZ,(dreg),(sreg)))
#define ARM_TOUIZD(p,dreg,sreg)      ARM_TOUIZD_COND(p,dreg,sreg,ARMCOND_AL)

#define ARM_TOUIZS_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_VFP_MONADIC((cond),ARM_VFP_COPROC_SINGLE,ARM_VFP_TOUIZ,(dreg),(sreg)))
#define ARM_TOUIZS(p,dreg,sreg)      ARM_TOUIZS_COND(p,dreg,sreg,ARMCOND_AL)


/* -- TOSIZ -- */


/* Fd := TOSIZ Fm */

#define ARM_TOSIZD_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_VFP_MONADIC((cond),ARM_VFP_COPROC_DOUBLE,ARM_VFP_TOSIZ,(dreg),(sreg)))
#define ARM_TOSIZD(p,dreg,sreg)      ARM_TOSIZD_COND(p,dreg,sreg,ARMCOND_AL)

#define ARM_TOSIZS_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_VFP_MONADIC((cond),ARM_VFP_COPROC_SINGLE,ARM_VFP_TOSIZ,(dreg),(sreg)))
#define ARM_TOSIZS(p,dreg,sreg)      ARM_TOSIZS_COND(p,dreg,sreg,ARMCOND_AL)






/* end generated */

