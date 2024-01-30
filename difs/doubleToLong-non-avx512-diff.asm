; Assembly listing for method Program:DoubleToLong(double):long (FullOpts)
; Emitting BLENDED_CODE for generic X64 - Windows
; FullOpts code
; optimized code
; rsp based frame
; partially interruptible
; No PGO data

G_M000_IG01:                ;; offset=0x0000
       4883EC28             sub      rsp, 40
 
G_M000_IG02:                ;; offset=0x0004
       E8E771C95F           call     CORINFO_HELP_DBL2LNG
       90                   nop      
 
G_M000_IG03:                ;; offset=0x000A
       4883C428             add      rsp, 40
       C3                   ret      
 
; Total bytes of code 15