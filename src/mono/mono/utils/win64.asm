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

	ret

mono_context_get_current endP

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
