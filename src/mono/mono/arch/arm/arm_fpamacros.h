/* Macros for FPA ops, auto-generated from template */


/* dyadic */

/* -- ADF -- */


/* Fd := Rn ADF Rm */
#define ARM_FPA_ADFD_COND(p, rd, rn, rm, cond) \
	ARM_EMIT((p), ARM_DEF_FPA_CPDO_DYADIC(cond,ARM_FPA_ADF,rd,rn,rm,ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_DOUBLE))
#define ARM_FPA_ADFD(p, rd, rn, rm) \
	ARM_FPA_ADFD_COND(p, rd, rn, rm, ARMCOND_AL)

#define ARM_FPA_ADFS_COND(p, rd, rn, rm, cond) \
	ARM_EMIT((p), ARM_DEF_FPA_CPDO_DYADIC(cond,ARM_FPA_ADF,rd,rn,rm,ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_SINGLE))
#define ARM_FPA_ADFS(p, rd, rn, rm) \
	ARM_FPA_ADFS_COND(p, rd, rn, rm, ARMCOND_AL)


/* -- MUF -- */


/* Fd := Rn MUF Rm */
#define ARM_FPA_MUFD_COND(p, rd, rn, rm, cond) \
	ARM_EMIT((p), ARM_DEF_FPA_CPDO_DYADIC(cond,ARM_FPA_MUF,rd,rn,rm,ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_DOUBLE))
#define ARM_FPA_MUFD(p, rd, rn, rm) \
	ARM_FPA_MUFD_COND(p, rd, rn, rm, ARMCOND_AL)

#define ARM_FPA_MUFS_COND(p, rd, rn, rm, cond) \
	ARM_EMIT((p), ARM_DEF_FPA_CPDO_DYADIC(cond,ARM_FPA_MUF,rd,rn,rm,ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_SINGLE))
#define ARM_FPA_MUFS(p, rd, rn, rm) \
	ARM_FPA_MUFS_COND(p, rd, rn, rm, ARMCOND_AL)


/* -- SUF -- */


/* Fd := Rn SUF Rm */
#define ARM_FPA_SUFD_COND(p, rd, rn, rm, cond) \
	ARM_EMIT((p), ARM_DEF_FPA_CPDO_DYADIC(cond,ARM_FPA_SUF,rd,rn,rm,ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_DOUBLE))
#define ARM_FPA_SUFD(p, rd, rn, rm) \
	ARM_FPA_SUFD_COND(p, rd, rn, rm, ARMCOND_AL)

#define ARM_FPA_SUFS_COND(p, rd, rn, rm, cond) \
	ARM_EMIT((p), ARM_DEF_FPA_CPDO_DYADIC(cond,ARM_FPA_SUF,rd,rn,rm,ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_SINGLE))
#define ARM_FPA_SUFS(p, rd, rn, rm) \
	ARM_FPA_SUFS_COND(p, rd, rn, rm, ARMCOND_AL)


/* -- RSF -- */


/* Fd := Rn RSF Rm */
#define ARM_FPA_RSFD_COND(p, rd, rn, rm, cond) \
	ARM_EMIT((p), ARM_DEF_FPA_CPDO_DYADIC(cond,ARM_FPA_RSF,rd,rn,rm,ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_DOUBLE))
#define ARM_FPA_RSFD(p, rd, rn, rm) \
	ARM_FPA_RSFD_COND(p, rd, rn, rm, ARMCOND_AL)

#define ARM_FPA_RSFS_COND(p, rd, rn, rm, cond) \
	ARM_EMIT((p), ARM_DEF_FPA_CPDO_DYADIC(cond,ARM_FPA_RSF,rd,rn,rm,ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_SINGLE))
#define ARM_FPA_RSFS(p, rd, rn, rm) \
	ARM_FPA_RSFS_COND(p, rd, rn, rm, ARMCOND_AL)


/* -- DVF -- */


/* Fd := Rn DVF Rm */
#define ARM_FPA_DVFD_COND(p, rd, rn, rm, cond) \
	ARM_EMIT((p), ARM_DEF_FPA_CPDO_DYADIC(cond,ARM_FPA_DVF,rd,rn,rm,ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_DOUBLE))
#define ARM_FPA_DVFD(p, rd, rn, rm) \
	ARM_FPA_DVFD_COND(p, rd, rn, rm, ARMCOND_AL)

#define ARM_FPA_DVFS_COND(p, rd, rn, rm, cond) \
	ARM_EMIT((p), ARM_DEF_FPA_CPDO_DYADIC(cond,ARM_FPA_DVF,rd,rn,rm,ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_SINGLE))
#define ARM_FPA_DVFS(p, rd, rn, rm) \
	ARM_FPA_DVFS_COND(p, rd, rn, rm, ARMCOND_AL)


/* -- RDF -- */


/* Fd := Rn RDF Rm */
#define ARM_FPA_RDFD_COND(p, rd, rn, rm, cond) \
	ARM_EMIT((p), ARM_DEF_FPA_CPDO_DYADIC(cond,ARM_FPA_RDF,rd,rn,rm,ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_DOUBLE))
#define ARM_FPA_RDFD(p, rd, rn, rm) \
	ARM_FPA_RDFD_COND(p, rd, rn, rm, ARMCOND_AL)

#define ARM_FPA_RDFS_COND(p, rd, rn, rm, cond) \
	ARM_EMIT((p), ARM_DEF_FPA_CPDO_DYADIC(cond,ARM_FPA_RDF,rd,rn,rm,ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_SINGLE))
#define ARM_FPA_RDFS(p, rd, rn, rm) \
	ARM_FPA_RDFS_COND(p, rd, rn, rm, ARMCOND_AL)


/* -- POW -- */


/* Fd := Rn POW Rm */
#define ARM_FPA_POWD_COND(p, rd, rn, rm, cond) \
	ARM_EMIT((p), ARM_DEF_FPA_CPDO_DYADIC(cond,ARM_FPA_POW,rd,rn,rm,ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_DOUBLE))
#define ARM_FPA_POWD(p, rd, rn, rm) \
	ARM_FPA_POWD_COND(p, rd, rn, rm, ARMCOND_AL)

#define ARM_FPA_POWS_COND(p, rd, rn, rm, cond) \
	ARM_EMIT((p), ARM_DEF_FPA_CPDO_DYADIC(cond,ARM_FPA_POW,rd,rn,rm,ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_SINGLE))
#define ARM_FPA_POWS(p, rd, rn, rm) \
	ARM_FPA_POWS_COND(p, rd, rn, rm, ARMCOND_AL)


/* -- RPW -- */


/* Fd := Rn RPW Rm */
#define ARM_FPA_RPWD_COND(p, rd, rn, rm, cond) \
	ARM_EMIT((p), ARM_DEF_FPA_CPDO_DYADIC(cond,ARM_FPA_RPW,rd,rn,rm,ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_DOUBLE))
#define ARM_FPA_RPWD(p, rd, rn, rm) \
	ARM_FPA_RPWD_COND(p, rd, rn, rm, ARMCOND_AL)

#define ARM_FPA_RPWS_COND(p, rd, rn, rm, cond) \
	ARM_EMIT((p), ARM_DEF_FPA_CPDO_DYADIC(cond,ARM_FPA_RPW,rd,rn,rm,ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_SINGLE))
#define ARM_FPA_RPWS(p, rd, rn, rm) \
	ARM_FPA_RPWS_COND(p, rd, rn, rm, ARMCOND_AL)


/* -- RMF -- */


/* Fd := Rn RMF Rm */
#define ARM_FPA_RMFD_COND(p, rd, rn, rm, cond) \
	ARM_EMIT((p), ARM_DEF_FPA_CPDO_DYADIC(cond,ARM_FPA_RMF,rd,rn,rm,ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_DOUBLE))
#define ARM_FPA_RMFD(p, rd, rn, rm) \
	ARM_FPA_RMFD_COND(p, rd, rn, rm, ARMCOND_AL)

#define ARM_FPA_RMFS_COND(p, rd, rn, rm, cond) \
	ARM_EMIT((p), ARM_DEF_FPA_CPDO_DYADIC(cond,ARM_FPA_RMF,rd,rn,rm,ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_SINGLE))
#define ARM_FPA_RMFS(p, rd, rn, rm) \
	ARM_FPA_RMFS_COND(p, rd, rn, rm, ARMCOND_AL)


/* -- FML -- */


/* Fd := Rn FML Rm */
#define ARM_FPA_FMLD_COND(p, rd, rn, rm, cond) \
	ARM_EMIT((p), ARM_DEF_FPA_CPDO_DYADIC(cond,ARM_FPA_FML,rd,rn,rm,ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_DOUBLE))
#define ARM_FPA_FMLD(p, rd, rn, rm) \
	ARM_FPA_FMLD_COND(p, rd, rn, rm, ARMCOND_AL)

#define ARM_FPA_FMLS_COND(p, rd, rn, rm, cond) \
	ARM_EMIT((p), ARM_DEF_FPA_CPDO_DYADIC(cond,ARM_FPA_FML,rd,rn,rm,ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_SINGLE))
#define ARM_FPA_FMLS(p, rd, rn, rm) \
	ARM_FPA_FMLS_COND(p, rd, rn, rm, ARMCOND_AL)


/* -- FDV -- */


/* Fd := Rn FDV Rm */
#define ARM_FPA_FDVD_COND(p, rd, rn, rm, cond) \
	ARM_EMIT((p), ARM_DEF_FPA_CPDO_DYADIC(cond,ARM_FPA_FDV,rd,rn,rm,ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_DOUBLE))
#define ARM_FPA_FDVD(p, rd, rn, rm) \
	ARM_FPA_FDVD_COND(p, rd, rn, rm, ARMCOND_AL)

#define ARM_FPA_FDVS_COND(p, rd, rn, rm, cond) \
	ARM_EMIT((p), ARM_DEF_FPA_CPDO_DYADIC(cond,ARM_FPA_FDV,rd,rn,rm,ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_SINGLE))
#define ARM_FPA_FDVS(p, rd, rn, rm) \
	ARM_FPA_FDVS_COND(p, rd, rn, rm, ARMCOND_AL)


/* -- FRD -- */


/* Fd := Rn FRD Rm */
#define ARM_FPA_FRDD_COND(p, rd, rn, rm, cond) \
	ARM_EMIT((p), ARM_DEF_FPA_CPDO_DYADIC(cond,ARM_FPA_FRD,rd,rn,rm,ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_DOUBLE))
#define ARM_FPA_FRDD(p, rd, rn, rm) \
	ARM_FPA_FRDD_COND(p, rd, rn, rm, ARMCOND_AL)

#define ARM_FPA_FRDS_COND(p, rd, rn, rm, cond) \
	ARM_EMIT((p), ARM_DEF_FPA_CPDO_DYADIC(cond,ARM_FPA_FRD,rd,rn,rm,ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_SINGLE))
#define ARM_FPA_FRDS(p, rd, rn, rm) \
	ARM_FPA_FRDS_COND(p, rd, rn, rm, ARMCOND_AL)


/* -- POL -- */


/* Fd := Rn POL Rm */
#define ARM_FPA_POLD_COND(p, rd, rn, rm, cond) \
	ARM_EMIT((p), ARM_DEF_FPA_CPDO_DYADIC(cond,ARM_FPA_POL,rd,rn,rm,ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_DOUBLE))
#define ARM_FPA_POLD(p, rd, rn, rm) \
	ARM_FPA_POLD_COND(p, rd, rn, rm, ARMCOND_AL)

#define ARM_FPA_POLS_COND(p, rd, rn, rm, cond) \
	ARM_EMIT((p), ARM_DEF_FPA_CPDO_DYADIC(cond,ARM_FPA_POL,rd,rn,rm,ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_SINGLE))
#define ARM_FPA_POLS(p, rd, rn, rm) \
	ARM_FPA_POLS_COND(p, rd, rn, rm, ARMCOND_AL)



/* monadic */

/* -- MVF -- */


/* Fd := MVF Rm */

#define ARM_MVFD_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_FPA_CPDO_MONADIC((cond),ARM_FPA_MVF,(dreg),(sreg),ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_DOUBLE))
#define ARM_MVFD(p,dreg,sreg)      ARM_MVFD_COND(p,dreg,sreg,ARMCOND_AL)

#define ARM_MVFS_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_FPA_CPDO_MONADIC((cond),ARM_FPA_MVF,(dreg),(sreg),ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_SINGLE))
#define ARM_MVFS(p,dreg,sreg)      ARM_MVFS_COND(p,dreg,sreg,ARMCOND_AL)


/* -- MNF -- */


/* Fd := MNF Rm */

#define ARM_MNFD_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_FPA_CPDO_MONADIC((cond),ARM_FPA_MNF,(dreg),(sreg),ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_DOUBLE))
#define ARM_MNFD(p,dreg,sreg)      ARM_MNFD_COND(p,dreg,sreg,ARMCOND_AL)

#define ARM_MNFS_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_FPA_CPDO_MONADIC((cond),ARM_FPA_MNF,(dreg),(sreg),ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_SINGLE))
#define ARM_MNFS(p,dreg,sreg)      ARM_MNFS_COND(p,dreg,sreg,ARMCOND_AL)


/* -- ABS -- */


/* Fd := ABS Rm */

#define ARM_ABSD_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_FPA_CPDO_MONADIC((cond),ARM_FPA_ABS,(dreg),(sreg),ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_DOUBLE))
#define ARM_ABSD(p,dreg,sreg)      ARM_ABSD_COND(p,dreg,sreg,ARMCOND_AL)

#define ARM_ABSS_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_FPA_CPDO_MONADIC((cond),ARM_FPA_ABS,(dreg),(sreg),ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_SINGLE))
#define ARM_ABSS(p,dreg,sreg)      ARM_ABSS_COND(p,dreg,sreg,ARMCOND_AL)


/* -- RND -- */


/* Fd := RND Rm */

#define ARM_RNDD_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_FPA_CPDO_MONADIC((cond),ARM_FPA_RND,(dreg),(sreg),ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_DOUBLE))
#define ARM_RNDD(p,dreg,sreg)      ARM_RNDD_COND(p,dreg,sreg,ARMCOND_AL)

#define ARM_RNDS_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_FPA_CPDO_MONADIC((cond),ARM_FPA_RND,(dreg),(sreg),ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_SINGLE))
#define ARM_RNDS(p,dreg,sreg)      ARM_RNDS_COND(p,dreg,sreg,ARMCOND_AL)


/* -- SQT -- */


/* Fd := SQT Rm */

#define ARM_SQTD_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_FPA_CPDO_MONADIC((cond),ARM_FPA_SQT,(dreg),(sreg),ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_DOUBLE))
#define ARM_SQTD(p,dreg,sreg)      ARM_SQTD_COND(p,dreg,sreg,ARMCOND_AL)

#define ARM_SQTS_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_FPA_CPDO_MONADIC((cond),ARM_FPA_SQT,(dreg),(sreg),ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_SINGLE))
#define ARM_SQTS(p,dreg,sreg)      ARM_SQTS_COND(p,dreg,sreg,ARMCOND_AL)


/* -- LOG -- */


/* Fd := LOG Rm */

#define ARM_LOGD_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_FPA_CPDO_MONADIC((cond),ARM_FPA_LOG,(dreg),(sreg),ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_DOUBLE))
#define ARM_LOGD(p,dreg,sreg)      ARM_LOGD_COND(p,dreg,sreg,ARMCOND_AL)

#define ARM_LOGS_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_FPA_CPDO_MONADIC((cond),ARM_FPA_LOG,(dreg),(sreg),ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_SINGLE))
#define ARM_LOGS(p,dreg,sreg)      ARM_LOGS_COND(p,dreg,sreg,ARMCOND_AL)


/* -- EXP -- */


/* Fd := EXP Rm */

#define ARM_EXPD_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_FPA_CPDO_MONADIC((cond),ARM_FPA_EXP,(dreg),(sreg),ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_DOUBLE))
#define ARM_EXPD(p,dreg,sreg)      ARM_EXPD_COND(p,dreg,sreg,ARMCOND_AL)

#define ARM_EXPS_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_FPA_CPDO_MONADIC((cond),ARM_FPA_EXP,(dreg),(sreg),ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_SINGLE))
#define ARM_EXPS(p,dreg,sreg)      ARM_EXPS_COND(p,dreg,sreg,ARMCOND_AL)


/* -- SIN -- */


/* Fd := SIN Rm */

#define ARM_SIND_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_FPA_CPDO_MONADIC((cond),ARM_FPA_SIN,(dreg),(sreg),ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_DOUBLE))
#define ARM_SIND(p,dreg,sreg)      ARM_SIND_COND(p,dreg,sreg,ARMCOND_AL)

#define ARM_SINS_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_FPA_CPDO_MONADIC((cond),ARM_FPA_SIN,(dreg),(sreg),ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_SINGLE))
#define ARM_SINS(p,dreg,sreg)      ARM_SINS_COND(p,dreg,sreg,ARMCOND_AL)


/* -- COS -- */


/* Fd := COS Rm */

#define ARM_COSD_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_FPA_CPDO_MONADIC((cond),ARM_FPA_COS,(dreg),(sreg),ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_DOUBLE))
#define ARM_COSD(p,dreg,sreg)      ARM_COSD_COND(p,dreg,sreg,ARMCOND_AL)

#define ARM_COSS_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_FPA_CPDO_MONADIC((cond),ARM_FPA_COS,(dreg),(sreg),ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_SINGLE))
#define ARM_COSS(p,dreg,sreg)      ARM_COSS_COND(p,dreg,sreg,ARMCOND_AL)


/* -- TAN -- */


/* Fd := TAN Rm */

#define ARM_TAND_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_FPA_CPDO_MONADIC((cond),ARM_FPA_TAN,(dreg),(sreg),ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_DOUBLE))
#define ARM_TAND(p,dreg,sreg)      ARM_TAND_COND(p,dreg,sreg,ARMCOND_AL)

#define ARM_TANS_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_FPA_CPDO_MONADIC((cond),ARM_FPA_TAN,(dreg),(sreg),ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_SINGLE))
#define ARM_TANS(p,dreg,sreg)      ARM_TANS_COND(p,dreg,sreg,ARMCOND_AL)


/* -- ASN -- */


/* Fd := ASN Rm */

#define ARM_ASND_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_FPA_CPDO_MONADIC((cond),ARM_FPA_ASN,(dreg),(sreg),ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_DOUBLE))
#define ARM_ASND(p,dreg,sreg)      ARM_ASND_COND(p,dreg,sreg,ARMCOND_AL)

#define ARM_ASNS_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_FPA_CPDO_MONADIC((cond),ARM_FPA_ASN,(dreg),(sreg),ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_SINGLE))
#define ARM_ASNS(p,dreg,sreg)      ARM_ASNS_COND(p,dreg,sreg,ARMCOND_AL)


/* -- ACS -- */


/* Fd := ACS Rm */

#define ARM_ACSD_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_FPA_CPDO_MONADIC((cond),ARM_FPA_ACS,(dreg),(sreg),ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_DOUBLE))
#define ARM_ACSD(p,dreg,sreg)      ARM_ACSD_COND(p,dreg,sreg,ARMCOND_AL)

#define ARM_ACSS_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_FPA_CPDO_MONADIC((cond),ARM_FPA_ACS,(dreg),(sreg),ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_SINGLE))
#define ARM_ACSS(p,dreg,sreg)      ARM_ACSS_COND(p,dreg,sreg,ARMCOND_AL)


/* -- ATN -- */


/* Fd := ATN Rm */

#define ARM_ATND_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_FPA_CPDO_MONADIC((cond),ARM_FPA_ATN,(dreg),(sreg),ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_DOUBLE))
#define ARM_ATND(p,dreg,sreg)      ARM_ATND_COND(p,dreg,sreg,ARMCOND_AL)

#define ARM_ATNS_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_FPA_CPDO_MONADIC((cond),ARM_FPA_ATN,(dreg),(sreg),ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_SINGLE))
#define ARM_ATNS(p,dreg,sreg)      ARM_ATNS_COND(p,dreg,sreg,ARMCOND_AL)


/* -- URD -- */


/* Fd := URD Rm */

#define ARM_URDD_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_FPA_CPDO_MONADIC((cond),ARM_FPA_URD,(dreg),(sreg),ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_DOUBLE))
#define ARM_URDD(p,dreg,sreg)      ARM_URDD_COND(p,dreg,sreg,ARMCOND_AL)

#define ARM_URDS_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_FPA_CPDO_MONADIC((cond),ARM_FPA_URD,(dreg),(sreg),ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_SINGLE))
#define ARM_URDS(p,dreg,sreg)      ARM_URDS_COND(p,dreg,sreg,ARMCOND_AL)


/* -- NRM -- */


/* Fd := NRM Rm */

#define ARM_NRMD_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_FPA_CPDO_MONADIC((cond),ARM_FPA_NRM,(dreg),(sreg),ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_DOUBLE))
#define ARM_NRMD(p,dreg,sreg)      ARM_NRMD_COND(p,dreg,sreg,ARMCOND_AL)

#define ARM_NRMS_COND(p,dreg,sreg,cond) \
        ARM_EMIT((p), ARM_DEF_FPA_CPDO_MONADIC((cond),ARM_FPA_NRM,(dreg),(sreg),ARM_FPA_ROUND_NEAREST,ARM_FPA_ROUND_SINGLE))
#define ARM_NRMS(p,dreg,sreg)      ARM_NRMS_COND(p,dreg,sreg,ARMCOND_AL)






/* end generated */

