; Assembly listing for method Program:FloatToUint(float):uint (FullOpts)
; Emitting BLENDED_CODE for X64 with AVX512 - Windows
; FullOpts code
; optimized code
; rsp based frame
; partially interruptible
; No PGO data

G_M000_IG01:                ;; offset=0x0000
       C5F877               vzeroupper 
 
G_M000_IG02:                ;; offset=0x0003
       62F37D0855051200000000 vfixupimmss xmm0, xmm0, xmmword ptr [reloc @RWD00], 0
       62F17E0878C0         vcvttss2usi  eax, xmm0
 
G_M000_IG03:                ;; offset=0x0014
       C3                   ret      
 
RWD00  	dq	0000000008000088h, 0000000000000000h

; Total bytes of code 21