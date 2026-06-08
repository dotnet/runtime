// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if defined(TARGET_POWERPC64)

#define ppc_emit32(c,x) do { *((uint32_t *) (c)) = (uint32_t) (x); (c) = ((uint8_t *)(c) + sizeof (uint32_t));} while (0)

// Branch instructions
#define ppc_blr(c)         ppc_emit32 (c, 0x4e800020)
#define ppc_blrl(c)        ppc_emit32 (c, 0x4e800021)
#define ppc_b(c,li)        ppc_emit32 (c, (18 << 26) | ((li) << 2))
#define ppc_bl(c,li)       ppc_emit32 (c, (18 << 26) | ((li) << 2) | 1)
#define ppc_bcx(c,BO,BI,BD,AA,LK) ppc_emit32(c, (16 << 26) | ((BO) << 21 )| ((BI) << 16) | (BD << 2) | ((AA) << 1) | LK)
#define ppc_bc(c,BO,BI,BD) ppc_bcx(c,BO,BI,BD,0,0)
#define ppc_bcctrx(c,BO,BI,LK) ppc_emit32(c, (19 << 26) | (BO << 21 )| (BI << 16) | (0 << 11) | (528 << 1) | LK)
#define ppc_bcctr(c,BO,BI) ppc_bcctrx(c,BO,BI,0)
#define ppc_bcctrl(c,BO,BI) ppc_bcctrx(c,BO,BI,1)

// Branch condition codes
#define PPC_BR_FALSE  4
#define PPC_BR_TRUE   12
#define PPC_BR_LT     0
#define PPC_BR_GT     1
#define PPC_BR_EQ     2

// Special purpose register instructions
#define ppc_mfspr(c,D,spr) ppc_emit32 (c, (31 << 26) | ((D) << 21) | ((spr) << 11) | (339 << 1))
#define ppc_mflr(c,D)      ppc_mfspr  (c, D, 256)
#define ppc_mtspr(c,spr,S) ppc_emit32 (c, (31 << 26) | ((S) << 21) | ((spr) << 11) | (467 << 1))
#define ppc_mtlr(c,S)      ppc_mtspr  (c, 256, S)
#define ppc_mtctr(c,S)     ppc_mtspr  (c, 288, S)

// Logical instructions
#define ppc_or(c,a,s,b)    ppc_emit32 (c, (31 << 26) | ((s) << 21) | ((a) << 16) | ((b) << 11) | 888)
#define ppc_mr(c,a,s)      ppc_or     (c, a, s, s)
#define ppc_ori(c,S,A,ui)  ppc_emit32 (c, (24 << 26) | ((S) << 21) | ((A) << 16) | (uint16_t)(ui))
#define ppc_oris(c,S,A,ui) ppc_emit32 (c, (25 << 26) | ((S) << 21) | ((A) << 16) | (uint16_t)(ui))
#define ppc_xori(c,A,S,ui) ppc_emit32 (c, (26 << 26) | ((S) << 21) | ((A) << 16) | (uint16_t)(ui))
#define ppc_andi(c,A,S,ui) ppc_emit32 (c, (28 << 26) | ((S) << 21) | ((A) << 16) | (uint16_t)(ui))

// Memory barrier instructions
#define ppc_hwsync(c)      ppc_emit32 (c, (31 << 26) | (0 << 21) | (0 << 16) | (0 << 11) | (598 << 1) | 0)  // hwsync is sync with L=0
#define ppc_lwsync(c)      ppc_emit32 (c, (31 << 26) | (1 << 21) | (0 << 16) | (0 << 11) | (598 << 1) | 0)  // lwsync is sync with L=1
#define ppc_isync(c)       ppc_emit32 (c, (19 << 26) | (0 << 21) | (0 << 16) | (0 << 11) | (150 << 1) | 0)  // instruction synchronize
#define ppc_nop(c)         ppc_ori    (c, 0, 0, 0)

// Arithmetic instructions
#define ppc_addi(c,D,A,i)  ppc_emit32 (c, (14 << 26) | ((D) << 21) | ((A) << 16) | (uint16_t)(i))
#define ppc_addis(c,D,A,i) ppc_emit32 (c, (15 << 26) | ((D) << 21) | ((A) << 16) | (uint16_t)(i))
#define ppc_li(c,D,v)      ppc_addi   (c, D, 0, (uint16_t)(v))
#define ppc_lis(c,D,v)     ppc_addis  (c, D, 0, (uint16_t)(v))

// Rotate and shift instructions
#define ppc_rldicr(c,A,S,n,b) ppc_emit32(c, (30 << 26) | ((S) << 21) | ((A) << 16) | (((n) & 0x1f) << 11) | (((b) & 0x1f) << 6) | ((((n) & 0x20) >> 5) << 1) | ((((b) & 0x20) >> 5) << 5) | (1 << 2))
#define ppc_rldicl(c,A,S,n,b) ppc_emit32(c, (30 << 26) | ((S) << 21) | ((A) << 16) | (((n) & 0x1f) << 11) | (((b) & 0x1f) << 6) | ((((n) & 0x20) >> 5) << 1) | ((((b) & 0x20) >> 5) << 5) | (0 << 2))
#define ppc_sldi(c,A,S,n)  ppc_rldicr(c, A, S, n, 63 - (n))
#define ppc_srdi(c,A,S,n)  ppc_rldicl(c, A, S, 64 - (n), (n))

// Register-based shift instructions (X-form)
#define ppc_sld(c,A,S,B)   ppc_emit32(c, (31 << 26) | ((S) << 21) | ((A) << 16) | ((B) << 11) | (27 << 1) | 0)
#define ppc_srd(c,A,S,B)   ppc_emit32(c, (31 << 26) | ((S) << 21) | ((A) << 16) | ((B) << 11) | (539 << 1) | 0)
#define ppc_srad(c,A,S,B)  ppc_emit32(c, (31 << 26) | ((S) << 21) | ((A) << 16) | ((B) << 11) | (794 << 1) | 0)
#define ppc_slw(c,A,S,B)   ppc_emit32(c, (31 << 26) | ((S) << 21) | ((A) << 16) | ((B) << 11) | (24 << 1) | 0)
#define ppc_srw(c,A,S,B)   ppc_emit32(c, (31 << 26) | ((S) << 21) | ((A) << 16) | ((B) << 11) | (536 << 1) | 0)
#define ppc_sraw(c,A,S,B)  ppc_emit32(c, (31 << 26) | ((S) << 21) | ((A) << 16) | ((B) << 11) | (792 << 1) | 0)
// Additional immediate shifts
#define ppc_sradi(c,A,S,n) ppc_emit32(c, (31 << 26) | ((S) << 21) | ((A) << 16) | (((n) & 0x1f) << 11) | (((n) & 0x20) >> 4) | (413 << 1) | 0)
#define ppc_slwi(c,A,S,n)  ppc_emit32(c, (21 << 26) | ((S) << 21) | ((A) << 16) | ((n) << 11) | (0 << 6) | ((31-(n)) << 1) | 0)
#define ppc_srwi(c,A,S,n)  ppc_emit32(c, (21 << 26) | ((S) << 21) | ((A) << 16) | ((32-(n)) << 11) | ((n) << 6) | (31 << 1) | 0)
#define ppc_srawi(c,A,S,n) ppc_emit32(c, (31 << 26) | ((S) << 21) | ((A) << 16) | ((n) << 11) | (824 << 1) | 0)

// Compare instructions
#define ppc_cmp(c,cfrD,L,A,B)   ppc_emit32(c, (31 << 26) | ((cfrD) << 23) | (0 << 22) | ((L) << 21) | ((A) << 16) | ((B) << 11) | (0 << 1) | 0)
#define ppc_cmpi(c,cfrD,L,A,B)  ppc_emit32(c, (11 << 26) | (cfrD << 23) | (0 << 22) | (L << 21) | (A << 16) | (uint16_t)(B))
#define ppc_cmpw(c,cfrD,A,B)    ppc_cmp(c, (cfrD), 0, (A), (B))
#define ppc_cmpd(c,cfrD,A,B)    ppc_cmp(c, (cfrD), 1, (A), (B))
#define ppc_cmpwi(c,cfrD,A,B)   ppc_cmpi(c, (cfrD), 0, (A), (B))
#define ppc_cmpdi(c,cfrD,A,B)   ppc_cmpi(c, (cfrD), 1, (A), (B))

// Load instructions
#define ppc_lbz(c,D,d,A)   ppc_emit32 (c, (34 << 26) | ((D) << 21) | ((A) << 16) | (uint16_t)(d))
#define ppc_lhz(c,D,d,A)   ppc_emit32 (c, (40 << 26) | ((D) << 21) | ((A) << 16) | (uint16_t)(d))
#define ppc_lha(c,D,d,A)   ppc_emit32 (c, (42 << 26) | ((D) << 21) | ((A) << 16) | (uint16_t)(d))
#define ppc_lwz(c,D,d,A)   ppc_emit32 (c, (32 << 26) | ((D) << 21) | ((A) << 16) | (uint16_t)(d))
#define ppc_lwa(c,D,ds,A)  ppc_emit32 (c, (58 << 26) | ((D) << 21) | ((A) << 16) | ((ds) & 0xfffc) | 2)
#define ppc_ld(c,D,ds,A)   ppc_emit32 (c, (58 << 26) | ((D) << 21) | ((A) << 16) | ((uint32_t)(ds) & 0xfffc) | 0)
#define ppc_lfs(c,D,d,A)   ppc_emit32 (c, (48 << 26) | ((D) << 21) | ((A) << 16) | (uint16_t)(d))
#define ppc_lfd(c,D,d,A)   ppc_emit32 (c, (50 << 26) | ((D) << 21) | ((A) << 16) | (uint16_t)(d))

// Store instructions
#define ppc_stb(c,S,d,A)   ppc_emit32 (c, (38 << 26) | ((S) << 21) | ((A) << 16) | (uint16_t)(d))
#define ppc_sth(c,S,d,A)   ppc_emit32 (c, (44 << 26) | ((S) << 21) | ((A) << 16) | (uint16_t)(d))
#define ppc_stw(c,S,d,A)   ppc_emit32 (c, (36 << 26) | ((S) << 21) | ((A) << 16) | (uint16_t)(d))
#define ppc_std(c,S,ds,A)  ppc_emit32 (c, (62 << 26) | ((S) << 21) | ((A) << 16) | ((uint32_t)(ds) & 0xfffc) | 0)
#define ppc_stdu(c,S,ds,A) ppc_emit32 (c, (62 << 26) | ((S) << 21) | ((A) << 16) | ((uint32_t)(ds) & 0xfffc) | 1)
#define ppc_stfs(c,S,d,a)  ppc_emit32 (c, (52 << 26) | ((S) << 21) | ((a) << 16) | (uint16_t)(d))
#define ppc_stfd(c,S,d,a)  ppc_emit32 (c, (54 << 26) | ((S) << 21) | ((a) << 16) | (uint16_t)(d))

// Indexed Load/Store Instructions (X-form) - Phase 4A: Array Support
// Format: opcode(6) | D/S(5) | A(5) | B(5) | XO(10) | Rc(1)
// Load Indexed - loads from address (rA + rB)
#define ppc_lbzx(c,D,A,B)   ppc_emit32(c, (31 << 26) | ((D) << 21) | ((A) << 16) | ((B) << 11) | (87 << 1) | 0)
#define ppc_lhzx(c,D,A,B)   ppc_emit32(c, (31 << 26) | ((D) << 21) | ((A) << 16) | ((B) << 11) | (279 << 1) | 0)
#define ppc_lhax(c,D,A,B)   ppc_emit32(c, (31 << 26) | ((D) << 21) | ((A) << 16) | ((B) << 11) | (343 << 1) | 0)
#define ppc_lwzx(c,D,A,B)   ppc_emit32(c, (31 << 26) | ((D) << 21) | ((A) << 16) | ((B) << 11) | (23 << 1) | 0)
#define ppc_lwax(c,D,A,B)   ppc_emit32(c, (31 << 26) | ((D) << 21) | ((A) << 16) | ((B) << 11) | (341 << 1) | 0)
#define ppc_ldx(c,D,A,B)    ppc_emit32(c, (31 << 26) | ((D) << 21) | ((A) << 16) | ((B) << 11) | (21 << 1) | 0)
#define ppc_lfsx(c,D,A,B)   ppc_emit32(c, (31 << 26) | ((D) << 21) | ((A) << 16) | ((B) << 11) | (535 << 1) | 0)
#define ppc_lfdx(c,D,A,B)   ppc_emit32(c, (31 << 26) | ((D) << 21) | ((A) << 16) | ((B) << 11) | (599 << 1) | 0)

// Store Indexed - stores to address (rA + rB)
#define ppc_stbx(c,S,A,B)   ppc_emit32(c, (31 << 26) | ((S) << 21) | ((A) << 16) | ((B) << 11) | (215 << 1) | 0)
#define ppc_sthx(c,S,A,B)   ppc_emit32(c, (31 << 26) | ((S) << 21) | ((A) << 16) | ((B) << 11) | (407 << 1) | 0)
#define ppc_stwx(c,S,A,B)   ppc_emit32(c, (31 << 26) | ((S) << 21) | ((A) << 16) | ((B) << 11) | (151 << 1) | 0)
#define ppc_stdx(c,S,A,B)   ppc_emit32(c, (31 << 26) | ((S) << 21) | ((A) << 16) | ((B) << 11) | (149 << 1) | 0)
#define ppc_stfsx(c,S,A,B)  ppc_emit32(c, (31 << 26) | ((S) << 21) | ((A) << 16) | ((B) << 11) | (663 << 1) | 0)
#define ppc_stfdx(c,S,A,B)  ppc_emit32(c, (31 << 26) | ((S) << 21) | ((A) << 16) | ((B) << 11) | (727 << 1) | 0)


// Floating-point arithmetic instructions (A-form)
// Format: opcode(6) | fD(5) | fA(5) | fB(5) | 0(5) | XO(5) | Rc(1)
#define ppc_fadds(c,D,A,B) ppc_emit32 (c, (59 << 26) | ((D) << 21) | ((A) << 16) | ((B) << 11) | (21 << 1) | 0)
#define ppc_fadd(c,D,A,B)  ppc_emit32 (c, (63 << 26) | ((D) << 21) | ((A) << 16) | ((B) << 11) | (21 << 1) | 0)
#define ppc_fsubs(c,D,A,B) ppc_emit32 (c, (59 << 26) | ((D) << 21) | ((A) << 16) | ((B) << 11) | (20 << 1) | 0)
#define ppc_fsub(c,D,A,B)  ppc_emit32 (c, (63 << 26) | ((D) << 21) | ((A) << 16) | ((B) << 11) | (20 << 1) | 0)
#define ppc_fmuls(c,D,A,C) ppc_emit32 (c, (59 << 26) | ((D) << 21) | ((A) << 16) | ((C) << 6) | (25 << 1) | 0)
#define ppc_fmul(c,D,A,C)  ppc_emit32 (c, (63 << 26) | ((D) << 21) | ((A) << 16) | ((C) << 6) | (25 << 1) | 0)
#define ppc_fdivs(c,D,A,B) ppc_emit32 (c, (59 << 26) | ((D) << 21) | ((A) << 16) | ((B) << 11) | (18 << 1) | 0)
#define ppc_fdiv(c,D,A,B)  ppc_emit32 (c, (63 << 26) | ((D) << 21) | ((A) << 16) | ((B) << 11) | (18 << 1) | 0)

// Floating-point move instruction (X-form)
// Format: opcode(6) | fD(5) | 0(5) | fB(5) | XO(10) | Rc(1)
#define ppc_fmr(c,D,B)     ppc_emit32 (c, (63 << 26) | ((D) << 21) | (0 << 16) | ((B) << 11) | (72 << 1) | 0)

// Floating-point round to single precision (X-form)
// Format: opcode(6) | fD(5) | 0(5) | fB(5) | XO(10) | Rc(1)
#define ppc_frsp(c,D,B)    ppc_emit32 (c, (63 << 26) | ((D) << 21) | (0 << 16) | ((B) << 11) | (12 << 1) | 0)

// Floating-point convert from integer doubleword (X-form)
// Format: opcode(6) | fD(5) | 0(5) | fB(5) | XO(10) | Rc(1)
#define ppc_fcfid(c,D,B)   ppc_emit32 (c, (63 << 26) | ((D) << 21) | (0 << 16) | ((B) << 11) | (846 << 1) | 0)
#define ppc_fcfids(c,D,B)  ppc_emit32 (c, (59 << 26) | ((D) << 21) | (0 << 16) | ((B) << 11) | (846 << 1) | 0)
#define ppc_fcfidu(c,D,B)  ppc_emit32 (c, (63 << 26) | ((D) << 21) | (0 << 16) | ((B) << 11) | (974 << 1) | 0)
#define ppc_fcfidus(c,D,B) ppc_emit32 (c, (59 << 26) | ((D) << 21) | (0 << 16) | ((B) << 11) | (974 << 1) | 0)

// Floating-point convert to integer word/doubleword with round toward zero (X-form)
// Format: opcode(6) | fD(5) | 0(5) | fB(5) | XO(10) | Rc(1)
#define ppc_fctiwz(c,D,B)  ppc_emit32 (c, (63 << 26) | ((D) << 21) | (0 << 16) | ((B) << 11) | (15 << 1) | 0)
#define ppc_fctidz(c,D,B)  ppc_emit32 (c, (63 << 26) | ((D) << 21) | (0 << 16) | ((B) << 11) | (815 << 1) | 0)
#define ppc_fctiwuz(c,D,B) ppc_emit32 (c, (63 << 26) | ((D) << 21) | (0 << 16) | ((B) << 11) | (143 << 1) | 0)
#define ppc_fctiduz(c,D,B) ppc_emit32 (c, (63 << 26) | ((D) << 21) | (0 << 16) | ((B) << 11) | (943 << 1) | 0)

// Floating-point comparison instructions (X-form)
// Format: opcode(6) | crD(3) | 0(2) | fA(5) | fB(5) | XO(10) | 0(1)
// crD specifies which CR field to update (0-7), typically use 0 for CR0
#define ppc_fcmpu(c,crD,A,B)  ppc_emit32 (c, (63 << 26) | ((crD) << 23) | ((A) << 16) | ((B) << 11) | (0 << 1) | 0)
#define ppc_fcmpo(c,crD,A,B)  ppc_emit32 (c, (63 << 26) | ((crD) << 23) | ((A) << 16) | ((B) << 11) | (32 << 1) | 0)

// Sign/Zero extension instructions (X-form)
// Format: opcode(6) | rS(5) | rA(5) | rB(5) | XO(10) | Rc(1)
#define ppc_extsb(c,A,S)   ppc_emit32 (c, (31 << 26) | ((S) << 21) | ((A) << 16) | (0 << 11) | (954 << 1) | 0)
#define ppc_extsh(c,A,S)   ppc_emit32 (c, (31 << 26) | ((S) << 21) | ((A) << 16) | (0 << 11) | (922 << 1) | 0)
#define ppc_extsw(c,A,S)   ppc_emit32 (c, (31 << 26) | ((S) << 21) | ((A) << 16) | (0 << 11) | (986 << 1) | 0)

// Integer arithmetic instructions (XO-form)
// Format: opcode(6) | rD(5) | rA(5) | rB(5) | OE(1) | XO(9) | Rc(1)
#define ppc_add(c,D,A,B)   ppc_emit32 (c, (31 << 26) | ((D) << 21) | ((A) << 16) | ((B) << 11) | (0 << 10) | (266 << 1) | 0) 
#define ppc_subf(c,D,A,B)  ppc_emit32 (c, (31 << 26) | ((D) << 21) | ((A) << 16) | ((B) << 11) | (0 << 10) | (40 << 1) | 0)  
#define ppc_mulld(c,D,A,B) ppc_emit32 (c, (31 << 26) | ((D) << 21) | ((A) << 16) | ((B) << 11) | (0 << 10) | (233 << 1) | 0)
#define ppc_mullw(c,D,A,B) ppc_emit32 (c, (31 << 26) | ((D) << 21) | ((A) << 16) | ((B) << 11) | (0 << 10) | (235 << 1) | 0) 
#define ppc_divd(c,D,A,B)  ppc_emit32 (c, (31 << 26) | ((D) << 21) | ((A) << 16) | ((B) << 11) | (0 << 10) | (489 << 1) | 0) 
#define ppc_divdu(c,D,A,B) ppc_emit32 (c, (31 << 26) | ((D) << 21) | ((A) << 16) | ((B) << 11) | (0 << 10) | (457 << 1) | 0) 
#define ppc_divw(c,D,A,B)  ppc_emit32 (c, (31 << 26) | ((D) << 21) | ((A) << 16) | ((B) << 11) | (0 << 10) | (491 << 1) | 0) 
#define ppc_divwu(c,D,A,B) ppc_emit32 (c, (31 << 26) | ((D) << 21) | ((A) << 16) | ((B) << 11) | (0 << 10) | (459 << 1) | 0) 

// Logical/Bitwise instructions (X-form)
// Format: opcode(6) | rS(5) | rA(5) | rB(5) | XO(10) | Rc(1)
#define ppc_and(c,A,S,B)   ppc_emit32 (c, (31 << 26) | ((S) << 21) | ((A) << 16) | ((B) << 11) | (28 << 1) | 0)
#define ppc_or(c,A,S,B)    ppc_emit32 (c, (31 << 26) | ((S) << 21) | ((A) << 16) | ((B) << 11) | (444 << 1) | 0)
#define ppc_xor(c,A,S,B)   ppc_emit32 (c, (31 << 26) | ((S) << 21) | ((A) << 16) | ((B) << 11) | (316 << 1) | 0)
#define ppc_nor(c,A,S,B)   ppc_emit32 (c, (31 << 26) | ((S) << 21) | ((A) << 16) | ((B) << 11) | (124 << 1) | 0)
#define ppc_nand(c,A,S,B)  ppc_emit32 (c, (31 << 26) | ((S) << 21) | ((A) << 16) | ((B) << 11) | (476 << 1) | 0)
#define ppc_andc(c,A,S,B)  ppc_emit32 (c, (31 << 26) | ((S) << 21) | ((A) << 16) | ((B) << 11) | (60 << 1) | 0)
#define ppc_orc(c,A,S,B)   ppc_emit32 (c, (31 << 26) | ((S) << 21) | ((A) << 16) | ((B) << 11) | (412 << 1) | 0)

// Trap instruction
#define ppc_trap(c)        ppc_emit32 (c, 0x7FE00008)

// The POWERPC64 instructions are all 32 bits in size.
// we use an unsigned int to hold the encoded instructions.
// This typedef defines the type that we use to hold encoded instructions.

//TODO POWERPC64

typedef unsigned int code_t;
/************************************************************************/
/*  Private members that deal with target-dependent instr. descriptors  */
/************************************************************************/

private:
instrDesc* emitNewInstrCallDir(int              argCnt,
                               VARSET_VALARG_TP GCvars,
                               regMaskTP        gcrefRegs,
                               regMaskTP        byrefRegs,
                               emitAttr         retSize,
                               emitAttr         secondRetSize);

instrDesc* emitNewInstrCallInd(int              argCnt,
                               ssize_t          disp,
                               VARSET_VALARG_TP GCvars,
                               regMaskTP        gcrefRegs,
                               regMaskTP        byrefRegs,
                               emitAttr         retSize,
                               emitAttr         secondRetSize);

/************************************************************************/
/*   enum to allow instruction optimisation to specify register order   */
/************************************************************************/
enum RegisterOrder
{
    eRO_none = 0,
    eRO_ascending,
    eRO_descending
};

/************************************************************************/
/*               Private helpers for instruction output                 */
/************************************************************************/

private:
bool     emitInsIsCompare(instruction ins);
bool     emitInsIsLoad(instruction ins);
bool     emitInsIsStore(instruction ins);
bool     emitInsIsLoadOrStore(instruction ins);
bool     emitInsIsVectorRightShift(instruction ins);
bool     emitInsIsVectorLong(instruction ins);
bool     emitInsIsVectorNarrow(instruction ins);
bool     emitInsIsVectorWide(instruction ins);
bool     emitInsDestIsOp2(instruction ins);
emitAttr emitInsTargetRegSize(instrDesc* id);
emitAttr emitInsLoadStoreSize(instrDesc* id);
bool IsRedundantMov(instruction ins, emitAttr size, regNumber dst, regNumber src, bool canSkip);
bool IsMovInstruction(instruction ins);

public:
inline static bool isFloatReg(regNumber reg)
{
    return (reg >= REG_F0 && reg <= REG_F31);
}

inline static bool isGeneralRegister(regNumber reg)
{
    return (reg >= REG_R0 && reg <= REG_R31);
} // Excludes REG_ZR

inline static bool insOptsNone(insOpts opt)
{
    return (opt == INS_OPTS_NONE);
}


void emitIns_S_R(instruction ins, emitAttr attr, regNumber ireg, int varx, int offs);

void emitIns_R_S(instruction ins, emitAttr attr, regNumber ireg, int varx, int offs);

enum EmitCallType
{
    EC_FUNC_TOKEN, // Direct call to a helper/static/nonvirtual/global method
    EC_INDIR_R,    // Indirect call via register
    EC_COUNT
};

void emitIns_Call(EmitCallType          callType,
                  CORINFO_METHOD_HANDLE methHnd,
                  INDEBUG_LDISASM_COMMA(CORINFO_SIG_INFO* sigInfo) // used to report call sites to the EE
                  void*            addr,
                  ssize_t          argSize,
                  emitAttr         retSize,
                  emitAttr         secondRetSize,
                  VARSET_VALARG_TP ptrVars,
                  regMaskTP        gcrefRegs,
                  regMaskTP        byrefRegs,
                  const DebugInfo& di,
                  regNumber        ireg,
                  regNumber        xreg,
                  unsigned         xmul,
                  ssize_t          disp,
                  bool             isJump,
                  bool             noSafePoint = false);

void emitIns_Mov(
    instruction ins, emitAttr attr, regNumber dstReg, regNumber srcReg, bool canSkip, insOpts opt = INS_OPTS_NONE);


/************************************************************************/
/*           The public entry points to output instructions             */
/************************************************************************/

public:
// Helper to check if a branch target is within range for direct bl instruction
// Returns the instruction offset if within range, 0 if out of range
int getBranchOffset(BYTE* src, void* target);

void emitIns(instruction ins);

void emitIns_I(instruction ins, emitAttr attr, ssize_t imm);

void emitInsSve_I(instruction ins, emitAttr attr, ssize_t imm);

void emitIns_R(instruction ins, emitAttr attr, regNumber reg, insOpts opt = INS_OPTS_NONE);

void emitInsSve_R(instruction ins, emitAttr attr, regNumber reg, insOpts opt = INS_OPTS_NONE);

void emitIns_R_I(instruction     ins,
                 emitAttr        attr,
                 regNumber       reg,
                 ssize_t         imm,
                 insOpts         opt  = INS_OPTS_NONE,
                 insScalableOpts sopt = INS_SCALABLE_OPTS_NONE DEBUGARG(size_t targetHandle = 0)
                     DEBUGARG(GenTreeFlags gtFlags = GTF_EMPTY));

void emitIns_R_R(instruction     ins,
		emitAttr        attr,
		regNumber       reg1,
		regNumber       reg2,
		insOpts         opt  = INS_OPTS_NONE,
		insScalableOpts sopt = INS_SCALABLE_OPTS_NONE);

void emitInsSve_R_R(instruction     ins,
		emitAttr        attr,
		regNumber       reg1,
		regNumber       reg2,
		insOpts         opt  = INS_OPTS_NONE,
		insScalableOpts sopt = INS_SCALABLE_OPTS_NONE);

void emitIns_R_R(instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, insFlags flags)
{
	emitIns_R_R(ins, attr, reg1, reg2);
}


void emitIns_R_AR(instruction ins, emitAttr attr, regNumber ireg, regNumber reg, int offs);

void emitIns_AR_R(instruction ins, emitAttr attr, regNumber ireg, regNumber reg, int offs);

void emitIns_R_R_I(instruction ins,
                   emitAttr    attr,
                   regNumber   reg1,
                   regNumber   reg2,
                   ssize_t     imm,
                   insOpts     opt = INS_OPTS_NONE);

void emitIns_R_R_R(instruction ins,
                   emitAttr    attr,
                   regNumber   reg1,
                   regNumber   reg2,
                   regNumber   reg3,
                   insOpts     opt = INS_OPTS_NONE);

bool emitIns_valid_imm_for_li(ssize_t imm);

void emitInsLoadStoreOp(instruction ins, emitAttr attr, regNumber dataReg, GenTreeIndir* indir);

void emitIns_J(instruction ins, BasicBlock* dst, int instrCount = 0);

#endif
#ifdef DEBUG
const char* emitDisInsName(code_t code, const BYTE* addr, instrDesc* id);
#endif

size_t emitInsSize(instrDesc* id);
