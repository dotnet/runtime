; Assembly listing for method Program:DoubleToLong(double):long (FullOpts)
; Emitting BLENDED_CODE for X64 with AVX512 - Windows
; FullOpts code
; optimized code
; rsp based frame
; partially interruptible
; No PGO data

G_M000_IG01:                ;; offset=0x0000
       C5F877               vzeroupper 
 
G_M000_IG02:                ;; offset=0x0003
       62F3FD0855053200000000 vfixupimmsd xmm0, xmm0, xmmword ptr [reloc @RWD00], 0
       C5F9C20D390000000D   vcmppd   xmm1, xmm0, xmmword ptr [reloc @RWD16], 13
       C5F8101541000000     vmovups  xmm2, xmmword ptr [reloc @RWD32]
       C4E1FB2CC0           vcvttsd2si  rax, xmm0
       62F2FD087CC0         vpbroadcastq  xmm0, rax
       62F3ED0825C8CA       vpternlogq xmm1, xmm2, xmm0, -54
       C4E1F97EC8           vmovd    rax, xmm1
 
G_M000_IG03:                ;; offset=0x0036
       C3                   ret      
 
RWD00  	dq	0000000000000088h, 0000000000000000h
RWD16  	dq	43E0000000000000h, 43E0000000000000h
RWD32  	dq	7FFFFFFFFFFFFFFFh, 7FFFFFFFFFFFFFFFh

; Total bytes of code 55