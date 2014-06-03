ifndef X64

.386
.model flat, c

endif

.code

ifdef X64

PUBLIC mono_context_get_current

mono_context_get_current PROC
;rdi has the ctx ptr
	mov [rcx + 00h], rax
	mov [rcx + 08h], rbx
	mov [rcx + 10h], rcx
	mov [rcx + 18h], rdx
	mov [rcx + 20h], rbp
	mov [rcx + 28h], rsp
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

	lea rax, __mono_current_ip
__mono_current_ip:
	mov [rcx + 80h], rax
	ret

mono_context_get_current endP

endif

end
