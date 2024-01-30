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
       C4E1FA2CC0           vcvttss2si  rax, xmm0
 
G_M000_IG03:                ;; offset=0x0008
       C3                   ret   