#ifndef __MONO_SGENGC_H__
#define __MONO_SGENGC_H__

#ifdef __i386__
#define ARCH_NUM_REGS 8
#define ARCH_STORE_REGS(ptr)	\
	__asm__ __volatile__(	\
		"mov %%ecx, 0x00(%0)\n"	\
		"mov %%edx, 0x04(%0)\n"	\
		"mov %%ebx, 0x08(%0)\n"	\
		"mov %%edi, 0x0c(%0)\n"	\
		"mov %%esi, 0x10(%0)\n"	\
		"mov %%ebp, 0x14(%0)\n"	\
		: "=&a" (ptr)	\
		: "0" (cur_thread_regs)	\
		: "memory"	\
	)

#elif defined(__x86_64__)
#define ARCH_NUM_REGS 16
#define ARCH_STORE_REGS(ptr)	\
	__asm__ __volatile__(	\
		"movq %%rcx, 0x00(%0)\n"	\
		"movq %%rdx, 0x08(%0)\n"	\
		"movq %%rbx, 0x10(%0)\n"	\
		"movq %%rdi, 0x18(%0)\n"	\
		"movq %%rsi, 0x20(%0)\n"	\
		"movq %%rbp, 0x28(%0)\n"	\
		"movq %%r8, 0x30(%0)\n"	\
		"movq %%r9, 0x38(%0)\n"	\
		"movq %%r10, 0x40(%0)\n"	\
		"movq %%r11, 0x48(%0)\n"	\
		"movq %%r12, 0x50(%0)\n"	\
		"movq %%r13, 0x58(%0)\n"	\
		"movq %%r14, 0x60(%0)\n"	\
		"movq %%r15, 0x68(%0)\n"	\
		: "=&a" (ptr)	\
		: "0" (cur_thread_regs)	\
		: "memory"	\
	)

#endif

#endif /* __MONO_SGENGC_H__ */

