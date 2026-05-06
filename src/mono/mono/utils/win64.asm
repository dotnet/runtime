ifdef RAX
else

.386
.model flat, c

endif

.code

ifdef RAX

PUBLIC mono_context_get_current

mono_context_get_current PROC
;rcx has the ctx ptr
	mov [rcx + 00h], rax
	mov [rcx + 08h], rcx
	mov [rcx + 10h], rdx
	mov [rcx + 18h], rbx
	mov [rcx + 28h], rbp
	mov [rcx + 30h], rsi
	mov [rcx + 38h], rdi
	mov [rcx + 40h], r8
	mov [rcx + 48h], r9
	mov [rcx + 50h], r10
	mov [rcx + 58h], r11
	mov [rcx + 60h], r12
	mov [rcx + 68h], r13
	mov [rcx + 70h], r14
	mov [rcx + 78h], r15

	lea rax, [rsp+8]
	mov [rcx + 20h], rax

	mov rax, qword ptr [rsp]
	mov [rcx + 80h], rax

	movaps xmmword ptr [rcx + 90h], xmm0
	movaps xmmword ptr [rcx + 0A0h], xmm1
	movaps xmmword ptr [rcx + 0B0h], xmm2
	movaps xmmword ptr [rcx + 0C0h], xmm3
	movaps xmmword ptr [rcx + 0D0h], xmm4
	movaps xmmword ptr [rcx + 0E0h], xmm5
	movaps xmmword ptr [rcx + 0F0h], xmm6
	movaps xmmword ptr [rcx + 100h], xmm7
	movaps xmmword ptr [rcx + 110h], xmm8
	movaps xmmword ptr [rcx + 120h], xmm9
	movaps xmmword ptr [rcx + 130h], xmm10
	movaps xmmword ptr [rcx + 140h], xmm11
	movaps xmmword ptr [rcx + 150h], xmm12
	movaps xmmword ptr [rcx + 160h], xmm13
	movaps xmmword ptr [rcx + 170h], xmm14
	movaps xmmword ptr [rcx + 180h], xmm15

	ret

mono_context_get_current endP

PUBLIC mono_context_get_current_avx

mono_context_get_current_avx PROC
;rcx has the ctx ptr
	mov [rcx + 00h], rax
	mov [rcx + 08h], rcx
	mov [rcx + 10h], rdx
	mov [rcx + 18h], rbx
	mov [rcx + 28h], rbp
	mov [rcx + 30h], rsi
	mov [rcx + 38h], rdi
	mov [rcx + 40h], r8
	mov [rcx + 48h], r9
	mov [rcx + 50h], r10
	mov [rcx + 58h], r11
	mov [rcx + 60h], r12
	mov [rcx + 68h], r13
	mov [rcx + 70h], r14
	mov [rcx + 78h], r15

	lea rax, [rsp+8]
	mov [rcx + 20h], rax

	mov rax, qword ptr [rsp]
	mov [rcx + 80h], rax

	movaps xmmword ptr [rcx + 90h], xmm0
	movaps xmmword ptr [rcx + 0A0h], xmm1
	movaps xmmword ptr [rcx + 0B0h], xmm2
	movaps xmmword ptr [rcx + 0C0h], xmm3
	movaps xmmword ptr [rcx + 0D0h], xmm4
	movaps xmmword ptr [rcx + 0E0h], xmm5
	movaps xmmword ptr [rcx + 0F0h], xmm6
	movaps xmmword ptr [rcx + 100h], xmm7
	movaps xmmword ptr [rcx + 110h], xmm8
	movaps xmmword ptr [rcx + 120h], xmm9
	movaps xmmword ptr [rcx + 130h], xmm10
	movaps xmmword ptr [rcx + 140h], xmm11
	movaps xmmword ptr [rcx + 150h], xmm12
	movaps xmmword ptr [rcx + 160h], xmm13
	movaps xmmword ptr [rcx + 170h], xmm14
	movaps xmmword ptr [rcx + 180h], xmm15

	vextractf128 xmmword ptr [rcx + 190h],ymm0,1
	vextractf128 xmmword ptr [rcx + 1A0h],ymm1,1
	vextractf128 xmmword ptr [rcx + 1B0h],ymm2,1
	vextractf128 xmmword ptr [rcx + 1C0h],ymm3,1
	vextractf128 xmmword ptr [rcx + 1D0h],ymm4,1
	vextractf128 xmmword ptr [rcx + 1E0h],ymm5,1
	vextractf128 xmmword ptr [rcx + 1F0h],ymm6,1
	vextractf128 xmmword ptr [rcx + 200h],ymm7,1
	vextractf128 xmmword ptr [rcx + 210h],ymm8,1
	vextractf128 xmmword ptr [rcx + 220h],ymm9,1
	vextractf128 xmmword ptr [rcx + 230h],ymm10,1
	vextractf128 xmmword ptr [rcx + 240h],ymm11,1
	vextractf128 xmmword ptr [rcx + 250h],ymm12,1
	vextractf128 xmmword ptr [rcx + 260h],ymm13,1
	vextractf128 xmmword ptr [rcx + 270h],ymm14,1
	vextractf128 xmmword ptr [rcx + 280h],ymm15,1

	ret

mono_context_get_current_avx endP

; Implementation of __builtin_unwind_init under MSVC, dumping
; nonvolatile registers into MonoBuiltinUnwindInfo *.

copy_stack_data_internal_win32_wrapper PROC PUBLIC
;rcx MonoThreadInfo *
;rdx MonoStackData *
;r8 MonoBuiltinUnwindInfo *
;r9 CopyStackDataFunc

	movaps xmmword ptr [r8 + 00h], xmm6
	movaps xmmword ptr [r8 + 10h], xmm7
	movaps xmmword ptr [r8 + 20h], xmm8
	movaps xmmword ptr [r8 + 30h], xmm9
	movaps xmmword ptr [r8 + 40h], xmm10
	movaps xmmword ptr [r8 + 50h], xmm11
	movaps xmmword ptr [r8 + 60h], xmm12
	movaps xmmword ptr [r8 + 70h], xmm13
	movaps xmmword ptr [r8 + 80h], xmm14
	movaps xmmword ptr [r8 + 90h], xmm15

	mov qword ptr [r8 + 0A0h], rbx
	mov qword ptr [r8 + 0A8h], rsi
	mov qword ptr [r8 + 0B0h], rdi
	mov qword ptr [r8 + 0B8h], r12
	mov qword ptr [r8 + 0C0h], r13
	mov qword ptr [r8 + 0C8h], r14
	mov qword ptr [r8 + 0D0h], r15
	mov qword ptr [r8 + 0D8h], rbp

	; tailcall, all parameters passed through to CopyStackDataFunc.
	jmp r9

copy_stack_data_internal_win32_wrapper endP

endif

end
