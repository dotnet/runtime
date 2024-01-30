; Assembly listing for method Program:DoubleToLong128(System.Runtime.Intrinsics.Vector128`1[double]):System.Runtime.Intrinsics.Vector128`1[long] (FullOpts)
; Emitting BLENDED_CODE for X64 with AVX512 - Windows
; FullOpts code
; optimized code
; rsp based frame
; partially interruptible
; No PGO data

G_M000_IG01:                ;; offset=0x0000
       C5F877               vzeroupper 
 
G_M000_IG02:                ;; offset=0x0003
       C5F81002             vmovups  xmm0, xmmword ptr [rdx]
       62F3FD0854052E00000000 vfixupimmpd xmm0, xmm0, xmmword ptr [reloc @RWD00], 0
       C5F9C20D350000000D   vcmppd   xmm1, xmm0, xmmword ptr [reloc @RWD16], 13
       C5F810153D000000     vmovups  xmm2, xmmword ptr [reloc @RWD32]
       62F1FD087AC0         vcvttpd2qq xmm0, xmm0
       62F3ED0825C8CA       vpternlogq xmm1, xmm2, xmm0, -54
       C5F81109             vmovups  xmmword ptr [rcx], xmm1
       488BC1               mov      rax, rcx
 
G_M000_IG03:                ;; offset=0x0037
       C3                   ret      
 
RWD00  	dq	0000000000000088h, 0000000000000088h
RWD16  	dq	43E0000000000000h, 43E0000000000000h
RWD32  	dq	7FFFFFFFFFFFFFFFh, 7FFFFFFFFFFFFFFFh

; Total bytes of code 56