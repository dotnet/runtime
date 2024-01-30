; Assembly listing for method Program:DoubleToLong128(System.Runtime.Intrinsics.Vector128`1[double]):System.Runtime.Intrinsics.Vector128`1[long] (FullOpts)
; Emitting BLENDED_CODE for X64 with AVX512 - Windows
; FullOpts code
; optimized code
; rsp based frame
; partially interruptible
; No PGO data
; 0 inlinees with PGO data; 8 single block inlinees; 4 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       4883EC38             sub      rsp, 56
       C5F877               vzeroupper 
 
G_M000_IG02:                ;; offset=0x0007
       488B02               mov      rax, qword ptr [rdx]
       4889442428           mov      qword ptr [rsp+0x28], rax
       C4E1FB2C442428       vcvttsd2si rax, qword ptr [rsp+0x28]
       4889442430           mov      qword ptr [rsp+0x30], rax
       488B442430           mov      rax, qword ptr [rsp+0x30]
       488B5208             mov      rdx, qword ptr [rdx+0x08]
       4889542418           mov      qword ptr [rsp+0x18], rdx
       C4E1FB2C542418       vcvttsd2si rdx, qword ptr [rsp+0x18]
       4889542420           mov      qword ptr [rsp+0x20], rdx
       488B542420           mov      rdx, qword ptr [rsp+0x20]
       48890424             mov      qword ptr [rsp], rax
       4889542408           mov      qword ptr [rsp+0x08], rdx
       C5F8280424           vmovaps  xmm0, xmmword ptr [rsp]
       C5F81101             vmovups  xmmword ptr [rcx], xmm0
       488BC1               mov      rax, rcx
 
G_M000_IG03:                ;; offset=0x004F
       4883C438             add      rsp, 56
       C3                   ret      
 
; Total bytes of code 84