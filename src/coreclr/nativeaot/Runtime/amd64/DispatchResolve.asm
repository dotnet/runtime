;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

include AsmMacros.inc


EXTERN RhpCidResolve : PROC
EXTERN RhpUniversalTransitionTailCall : PROC

EXTERN g_pDispatchCache : QWORD

;; Fast version of RhpResolveInterfaceMethod
LEAF_ENTRY RhpResolveInterfaceMethodFast, _TEXT

        ;; Load the MethodTable from the object instance in rcx.
        ;; Trigger an AV if we're dispatching on a null this.
        ;; The exception handling infrastructure is aware of the fact that this is the first
        ;; instruction of RhpResolveInterfaceMethodFast and uses it to translate an AV here
        ;; to a NullReferenceException at the callsite.
        mov     r10, [rcx]

        ;; r11 currently contains the indirection cell address.
        ;; load the cached monomorphic resolved code address into rax
        mov     rax, [r11 + 8]
        test    rax, rax
        jz      RhpResolveInterfaceMethodFast_SlowPath

        ;; is this the monomorhpic MethodTable?
        cmp     qword ptr [r11], r10
        jne     RhpResolveInterfaceMethodFast_Hashtable
        jmp     rax

      RhpResolveInterfaceMethodFast_Hashtable:

push        rcx
push        r8
push        r9

mov         r8, qword ptr [g_pDispatchCache]

mov         rcx,r11
mov         r8,qword ptr [r8]  
rol         rcx,10h  
xor         rcx,r10  
mov         r9,rcx  
sar         r9,20h  
xor         r9d,ecx  
movsxd      r9,r9d  
mov         rax,9E3779B97F4A7C15h  
imul        r9,rax  
movzx       ecx,byte ptr [r8+10h]  
shr         r9,cl  
xor         ecx,ecx  

RhpResolveInterfaceMethodFast_40:
lea         eax,[r9+1]  
movsxd      rax,eax
shl         rax,5  
lea         rax,[r8+rax+10h]  
cmp         r11,qword ptr [rax+8]  
jne         RhpResolveInterfaceMethodFast_65
cmp         r10,qword ptr [rax+10h]    
jne          RhpResolveInterfaceMethodFast_65

mov         r10d,dword ptr [rax]  
and         r10d,0FFFFFFFEh  
cmp         r10d,dword ptr [rax]  
jne         RhpResolveInterfaceMethodFast_7E

mov         rax,qword ptr [rax+18h]  

pop         r9
pop         r8
pop         rcx
jmp         rax

RhpResolveInterfaceMethodFast_65:
mov         eax,dword ptr [rax]  
test        eax,eax  
je          RhpResolveInterfaceMethodFast_7E
inc         ecx  
add         r9d,ecx  
mov         eax,dword ptr [r8+8]  
add         eax,0FFFFFFFEh  
and         r9d,eax  
cmp         ecx,8  
jl          RhpResolveInterfaceMethodFast_40

RhpResolveInterfaceMethodFast_7E:
pop         r9
pop         r8
pop         rcx

      RhpResolveInterfaceMethodFast_SlowPath:
        ;; r11 contains indirection cell address
        lea     r10, RhpCidResolve
        jmp     RhpUniversalTransitionTailCall

LEAF_END RhpResolveInterfaceMethodFast, _TEXT

end
