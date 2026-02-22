## .NET 11.0.0 (42.42.42.42424), X64 RyuJIT AVX2
```assembly
; VectorBenchmarks.SingleVec128()
       push      rbx
       sub       rsp,40
       mov       rbx,rdx
       vmovups   xmm0,[rcx+8]
       vmovaps   [rsp+30],xmm0
       vmovups   xmm0,[rcx+18]
       vmovaps   [rsp+20],xmm0
       lea       rdx,[rsp+30]
       lea       r8,[rsp+20]
       mov       rcx,rbx
       call      qword ptr [7FFF63755BC0]; VectorBenchmarks.Add128(System.Runtime.Intrinsics.Vector128`1<Single>, System.Runtime.Intrinsics.Vector128`1<Single>)
       mov       rax,rbx
       add       rsp,40
       pop       rbx
       ret
; Total bytes of code 58
```
```assembly
; VectorBenchmarks.Add128(System.Runtime.Intrinsics.Vector128`1<Single>, System.Runtime.Intrinsics.Vector128`1<Single>)
       vmovups   xmm0,[rdx]
       vaddps    xmm0,xmm0,[r8]
       vmovups   [rcx],xmm0
       mov       rax,rcx
       ret
; Total bytes of code 17
```

## .NET 11.0.0 (42.42.42.42424), X64 RyuJIT AVX2
```assembly
; VectorBenchmarks.SingleVec128()
       vmovups   xmm2,[rcx+18]
       vmovups   xmm1,[rcx+8]
       mov       rcx,rdx
       jmp       qword ptr [7FFF63755BC0]; VectorBenchmarks.Add128(System.Runtime.Intrinsics.Vector128`1<Single>, System.Runtime.Intrinsics.Vector128`1<Single>)
; Total bytes of code 19
```
```assembly
; VectorBenchmarks.Add128(System.Runtime.Intrinsics.Vector128`1<Single>, System.Runtime.Intrinsics.Vector128`1<Single>)
       vaddps    xmm0,xmm1,xmm2
       vmovups   [rcx],xmm0
       mov       rax,rcx
       ret
; Total bytes of code 12
```

## .NET 11.0.0 (42.42.42.42424), X64 RyuJIT AVX2
```assembly
; VectorBenchmarks.FourVec128()
       push      rbx
       sub       rsp,70
       mov       rbx,rdx
       vmovups   xmm0,[rcx+8]
       vmovaps   [rsp+60],xmm0
       vmovups   xmm0,[rcx+18]
       vmovaps   [rsp+50],xmm0
       vmovups   xmm0,[rcx+28]
       vmovaps   [rsp+40],xmm0
       vmovups   xmm0,[rcx+38]
       vmovaps   [rsp+30],xmm0
       lea       rdx,[rsp+60]
       lea       r8,[rsp+50]
       lea       r9,[rsp+40]
       lea       rcx,[rsp+30]
       mov       [rsp+20],rcx
       mov       rcx,rbx
       call      qword ptr [7FFF63785848]; VectorBenchmarks.Add128_4(System.Runtime.Intrinsics.Vector128`1<Single>, System.Runtime.Intrinsics.Vector128`1<Single>, System.Runtime.Intrinsics.Vector128`1<Single>, System.Runtime.Intrinsics.Vector128`1<Single>)
       mov       rax,rbx
       add       rsp,70
       pop       rbx
       ret
; Total bytes of code 95
```
```assembly
; VectorBenchmarks.Add128_4(System.Runtime.Intrinsics.Vector128`1<Single>, System.Runtime.Intrinsics.Vector128`1<Single>, System.Runtime.Intrinsics.Vector128`1<Single>, System.Runtime.Intrinsics.Vector128`1<Single>)
       mov       rax,[rsp+28]
       vmovups   xmm0,[rdx]
       vaddps    xmm0,xmm0,[r8]
       vaddps    xmm0,xmm0,[r9]
       vaddps    xmm0,xmm0,[rax]
       vmovups   [rcx],xmm0
       mov       rax,rcx
       ret
; Total bytes of code 31
```

## .NET 11.0.0 (42.42.42.42424), X64 RyuJIT AVX2
```assembly
; VectorBenchmarks.FourVec128()
       push      rbx
       sub       rsp,30
       mov       rbx,rdx
       vmovups   xmm1,[rcx+38]
       vmovups   [rsp+20],xmm1
       vmovups   xmm1,[rcx+8]
       vmovups   xmm2,[rcx+18]
       vmovups   xmm3,[rcx+28]
       mov       rcx,rbx
       call      qword ptr [7FFF63775BC0]; VectorBenchmarks.Add128_4(System.Runtime.Intrinsics.Vector128`1<Single>, System.Runtime.Intrinsics.Vector128`1<Single>, System.Runtime.Intrinsics.Vector128`1<Single>, System.Runtime.Intrinsics.Vector128`1<Single>)
       mov       rax,rbx
       add       rsp,30
       pop       rbx
       ret
; Total bytes of code 52
```
```assembly
; VectorBenchmarks.Add128_4(System.Runtime.Intrinsics.Vector128`1<Single>, System.Runtime.Intrinsics.Vector128`1<Single>, System.Runtime.Intrinsics.Vector128`1<Single>, System.Runtime.Intrinsics.Vector128`1<Single>)
       vaddps    xmm0,xmm1,xmm2
       vaddps    xmm0,xmm0,xmm3
       vaddps    xmm0,xmm0,[rsp+28]
       vmovups   [rcx],xmm0
       mov       rax,rcx
       ret
; Total bytes of code 22
```

## .NET 11.0.0 (42.42.42.42424), X64 RyuJIT AVX2
```assembly
; VectorBenchmarks.SingleVec256()
       push      rbx
       sub       rsp,70
       mov       rbx,rdx
       vmovups   ymm0,[rcx+48]
       vmovups   [rsp+40],ymm0
       vmovups   ymm0,[rcx+68]
       vmovups   [rsp+20],ymm0
       lea       rdx,[rsp+40]
       lea       r8,[rsp+20]
       mov       rcx,rbx
       call      qword ptr [7FFF63795BC0]; VectorBenchmarks.Add256(System.Runtime.Intrinsics.Vector256`1<Single>, System.Runtime.Intrinsics.Vector256`1<Single>)
       mov       rax,rbx
       vzeroupper
       add       rsp,70
       pop       rbx
       ret
; Total bytes of code 61
```
```assembly
; VectorBenchmarks.Add256(System.Runtime.Intrinsics.Vector256`1<Single>, System.Runtime.Intrinsics.Vector256`1<Single>)
       vmovups   ymm0,[rdx]
       vaddps    ymm0,ymm0,[r8]
       vmovups   [rcx],ymm0
       mov       rax,rcx
       vzeroupper
       ret
; Total bytes of code 20
```

## .NET 11.0.0 (42.42.42.42424), X64 RyuJIT AVX2
```assembly
; VectorBenchmarks.SingleVec256()
       vmovups   ymm2,[rcx+68]
       vmovups   ymm1,[rcx+48]
       mov       rcx,rdx
       vzeroupper
       jmp       qword ptr [7FFF63765BC0]; VectorBenchmarks.Add256(System.Runtime.Intrinsics.Vector256`1<Single>, System.Runtime.Intrinsics.Vector256`1<Single>)
; Total bytes of code 22
```
```assembly
; VectorBenchmarks.Add256(System.Runtime.Intrinsics.Vector256`1<Single>, System.Runtime.Intrinsics.Vector256`1<Single>)
       vaddps    ymm0,ymm1,ymm2
       vmovups   [rcx],ymm0
       mov       rax,rcx
       vzeroupper
       ret
; Total bytes of code 15
```

## .NET 11.0.0 (42.42.42.42424), X64 RyuJIT AVX2
```assembly
; VectorBenchmarks.MixedIntVec128()
       push      rbx
       sub       rsp,50
       mov       rbx,rdx
       vmovups   xmm0,[rcx+8]
       vmovaps   [rsp+40],xmm0
       vmovups   xmm0,[rcx+18]
       vmovaps   [rsp+30],xmm0
       lea       r8,[rsp+30]
       mov       [rsp+20],r8
       lea       r8,[rsp+40]
       mov       rcx,rbx
       mov       edx,2A
       mov       r9d,63
       call      qword ptr [7FFF63765BC0]; VectorBenchmarks.MixedAdd(Int32, System.Runtime.Intrinsics.Vector128`1<Single>, Int32, System.Runtime.Intrinsics.Vector128`1<Single>)
       mov       rax,rbx
       add       rsp,50
       pop       rbx
       ret
; Total bytes of code 74
```
```assembly
; VectorBenchmarks.MixedAdd(Int32, System.Runtime.Intrinsics.Vector128`1<Single>, Int32, System.Runtime.Intrinsics.Vector128`1<Single>)
       mov       rax,[rsp+28]
       vmovups   xmm0,[r8]
       vaddps    xmm0,xmm0,[rax]
       add       edx,r9d
       vxorps    xmm1,xmm1,xmm1
       vcvtsi2ss xmm1,xmm1,edx
       vbroadcastss xmm1,xmm1
       vaddps    xmm0,xmm1,xmm0
       vmovups   [rcx],xmm0
       mov       rax,rcx
       ret
; Total bytes of code 42
```

## .NET 11.0.0 (42.42.42.42424), X64 RyuJIT AVX2
```assembly
; VectorBenchmarks.MixedIntVec128()
       push      rbx
       sub       rsp,30
       mov       rbx,rdx
       vmovups   xmm2,[rcx+18]
       vmovups   [rsp+20],xmm2
       vmovups   xmm2,[rcx+8]
       mov       rcx,rbx
       mov       edx,2A
       mov       r9d,63
       call      qword ptr [7FFF63775BC0]; VectorBenchmarks.MixedAdd(Int32, System.Runtime.Intrinsics.Vector128`1<Single>, Int32, System.Runtime.Intrinsics.Vector128`1<Single>)
       mov       rax,rbx
       add       rsp,30
       pop       rbx
       ret
; Total bytes of code 53
```
```assembly
; VectorBenchmarks.MixedAdd(Int32, System.Runtime.Intrinsics.Vector128`1<Single>, Int32, System.Runtime.Intrinsics.Vector128`1<Single>)
       vaddps    xmm0,xmm2,[rsp+28]
       add       edx,r9d
       vxorps    xmm1,xmm1,xmm1
       vcvtsi2ss xmm1,xmm1,edx
       vbroadcastss xmm1,xmm1
       vaddps    xmm0,xmm1,xmm0
       vmovups   [rcx],xmm0
       mov       rax,rcx
       ret
; Total bytes of code 34
```

## .NET 11.0.0 (42.42.42.42424), X64 RyuJIT AVX2
```assembly
; VectorBenchmarks.ChainedVec128()
       push      rsi
       push      rbx
       sub       rsp,68
       mov       rbx,rcx
       mov       rsi,rdx
       vmovups   xmm0,[rbx+8]
       vmovaps   [rsp+30],xmm0
       vmovups   xmm0,[rbx+18]
       vmovaps   [rsp+20],xmm0
       lea       rdx,[rsp+30]
       lea       r8,[rsp+20]
       lea       rcx,[rsp+50]
       call      qword ptr [7FFF63765848]; VectorBenchmarks.Add128(System.Runtime.Intrinsics.Vector128`1<Single>, System.Runtime.Intrinsics.Vector128`1<Single>)
       vmovups   xmm0,[rbx+28]
       vmovaps   [rsp+30],xmm0
       lea       r8,[rsp+30]
       lea       rdx,[rsp+50]
       lea       rcx,[rsp+40]
       call      qword ptr [7FFF63765848]; VectorBenchmarks.Add128(System.Runtime.Intrinsics.Vector128`1<Single>, System.Runtime.Intrinsics.Vector128`1<Single>)
       vmovups   xmm0,[rbx+38]
       vmovaps   [rsp+30],xmm0
       lea       r8,[rsp+30]
       lea       rdx,[rsp+40]
       mov       rcx,rsi
       call      qword ptr [7FFF63765848]; VectorBenchmarks.Add128(System.Runtime.Intrinsics.Vector128`1<Single>, System.Runtime.Intrinsics.Vector128`1<Single>)
       mov       rax,rsi
       add       rsp,68
       pop       rbx
       pop       rsi
       ret
; Total bytes of code 127
```
```assembly
; VectorBenchmarks.Add128(System.Runtime.Intrinsics.Vector128`1<Single>, System.Runtime.Intrinsics.Vector128`1<Single>)
       vmovups   xmm0,[rdx]
       vaddps    xmm0,xmm0,[r8]
       vmovups   [rcx],xmm0
       mov       rax,rcx
       ret
; Total bytes of code 17
```

## .NET 11.0.0 (42.42.42.42424), X64 RyuJIT AVX2
```assembly
; VectorBenchmarks.ChainedVec128()
       push      rsi
       push      rbx
       sub       rsp,48
       mov       rbx,rcx
       mov       rsi,rdx
       vmovups   xmm1,[rbx+8]
       vmovups   xmm2,[rbx+18]
       lea       rcx,[rsp+30]
       call      qword ptr [7FFF63765848]; VectorBenchmarks.Add128(System.Runtime.Intrinsics.Vector128`1<Single>, System.Runtime.Intrinsics.Vector128`1<Single>)
       vmovups   xmm2,[rbx+28]
       lea       rcx,[rsp+20]
       vmovaps   xmm1,[rsp+30]
       call      qword ptr [7FFF63765848]; VectorBenchmarks.Add128(System.Runtime.Intrinsics.Vector128`1<Single>, System.Runtime.Intrinsics.Vector128`1<Single>)
       vmovups   xmm2,[rbx+38]
       mov       rcx,rsi
       vmovaps   xmm1,[rsp+20]
       add       rsp,48
       pop       rbx
       pop       rsi
       jmp       qword ptr [7FFF63765848]; VectorBenchmarks.Add128(System.Runtime.Intrinsics.Vector128`1<Single>, System.Runtime.Intrinsics.Vector128`1<Single>)
; Total bytes of code 81
```
```assembly
; VectorBenchmarks.Add128(System.Runtime.Intrinsics.Vector128`1<Single>, System.Runtime.Intrinsics.Vector128`1<Single>)
       vaddps    xmm0,xmm1,xmm2
       vmovups   [rcx],xmm0
       mov       rax,rcx
       ret
; Total bytes of code 12
```

## .NET 11.0.0 (42.42.42.42424), X64 RyuJIT AVX2
```assembly
; VectorBenchmarks.Identity128()
       push      rbx
       sub       rsp,30
       mov       rbx,rdx
       vmovups   xmm0,[rcx+8]
       vmovaps   [rsp+20],xmm0
       lea       rdx,[rsp+20]
       mov       rcx,rbx
       call      qword ptr [7FFF63765848]; VectorBenchmarks.ReturnIdentity128(System.Runtime.Intrinsics.Vector128`1<Single>)
       mov       rax,rbx
       add       rsp,30
       pop       rbx
       ret
; Total bytes of code 42
```
```assembly
; VectorBenchmarks.ReturnIdentity128(System.Runtime.Intrinsics.Vector128`1<Single>)
       vmovups   xmm0,[rdx]
       vmovups   [rcx],xmm0
       mov       rax,rcx
       ret
; Total bytes of code 12
```

## .NET 11.0.0 (42.42.42.42424), X64 RyuJIT AVX2
```assembly
; VectorBenchmarks.Identity128()
       vmovups   xmm1,[rcx+8]
       mov       rcx,rdx
       jmp       qword ptr [7FFF63765BC0]; VectorBenchmarks.ReturnIdentity128(System.Runtime.Intrinsics.Vector128`1<Single>)
; Total bytes of code 14
```
```assembly
; VectorBenchmarks.ReturnIdentity128(System.Runtime.Intrinsics.Vector128`1<Single>)
       vmovups   [rcx],xmm1
       mov       rax,rcx
       ret
; Total bytes of code 8
```

## .NET 11.0.0 (42.42.42.42424), X64 RyuJIT AVX2
```assembly
; VectorBenchmarks.Identity256()
       push      rbx
       sub       rsp,50
       mov       rbx,rdx
       vmovups   ymm0,[rcx+48]
       vmovups   [rsp+20],ymm0
       lea       rdx,[rsp+20]
       mov       rcx,rbx
       call      qword ptr [7FFF63755848]; VectorBenchmarks.ReturnIdentity256(System.Runtime.Intrinsics.Vector256`1<Single>)
       mov       rax,rbx
       vzeroupper
       add       rsp,50
       pop       rbx
       ret
; Total bytes of code 45
```
```assembly
; VectorBenchmarks.ReturnIdentity256(System.Runtime.Intrinsics.Vector256`1<Single>)
       vmovups   ymm0,[rdx]
       vmovups   [rcx],ymm0
       mov       rax,rcx
       vzeroupper
       ret
; Total bytes of code 15
```

## .NET 11.0.0 (42.42.42.42424), X64 RyuJIT AVX2
```assembly
; VectorBenchmarks.Identity256()
       vmovups   ymm1,[rcx+48]
       mov       rcx,rdx
       vzeroupper
       jmp       qword ptr [7FFF63765BC0]; VectorBenchmarks.ReturnIdentity256(System.Runtime.Intrinsics.Vector256`1<Single>)
; Total bytes of code 17
```
```assembly
; VectorBenchmarks.ReturnIdentity256(System.Runtime.Intrinsics.Vector256`1<Single>)
       vmovups   [rcx],ymm1
       mov       rax,rcx
       vzeroupper
       ret
; Total bytes of code 11
```

