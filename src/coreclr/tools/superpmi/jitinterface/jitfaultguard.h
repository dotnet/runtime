// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Recovers from the hardware exceptions of the JIT (access violations, divisions by zero, illegal instructions)
// elsewhere than on Windows, where the structured exception handling of the callers does it. A fault whose
// instruction is in the code of a JIT that CallCatchingJitFaults runs on the thread abandons the call, which returns
// the signal; any other signal goes to the handler that was installed before (for example, that of the runtime).

#ifndef JITFAULTGUARD_H
#define JITFAULTGUARD_H

#if (defined(__linux__) && (defined(__x86_64__) || defined(__aarch64__) || defined(__arm__) || defined(__riscv) || defined(__loongarch64))) || \
    (defined(__APPLE__) && (defined(__x86_64__) || defined(__aarch64__)))
#define JIT_FAULT_GUARD 1

#include <setjmp.h>
#include <signal.h>
#include <stdint.h>
#include <string.h>
#include <atomic>
#include <mutex>

#ifdef __APPLE__
// The faults reach the handlers as signals (SIGSEGV or SIGBUS for bad accesses) after the Mach exception handlers,
// such as those of the CoreCLR runtime in which the collector runs, forward the exceptions that they don't handle.
#include <dlfcn.h>
#include <mach-o/loader.h>
#else
#include <link.h>
#include <ucontext.h>
#endif

namespace JitFaultGuard
{
    static const int s_signals[] = {SIGSEGV, SIGBUS, SIGILL, SIGFPE};
    static const int SignalCount = sizeof(s_signals) / sizeof(s_signals[0]);

    // The executable segments of the JITs, which are only added to.
    struct CodeRange
    {
        uintptr_t start;
        uintptr_t end;
    };
    static const int MaxCodeRanges = 32;
    static CodeRange s_codeRanges[MaxCodeRanges];
    static std::atomic<int> s_codeRangeCount;
    static std::mutex s_lock;

    static struct sigaction s_previousActions[SignalCount];

    // The JIT call that CallCatchingJitFaults runs on the thread.
    struct Scope
    {
        sigjmp_buf jmp;
        volatile int signal;
        volatile int code;
    };
    static thread_local Scope* t_scope;

    static uintptr_t GetInstructionPointer(void* context)
    {
#if defined(__APPLE__)
        const mcontext_t mc = static_cast<ucontext_t*>(context)->uc_mcontext;
#if defined(__x86_64__)
        return (uintptr_t)mc->__ss.__rip;
#else
        return (uintptr_t)__darwin_arm_thread_state64_get_pc(mc->__ss);
#endif
#else
        const mcontext_t& mc = static_cast<ucontext_t*>(context)->uc_mcontext;
#if defined(__x86_64__)
        return (uintptr_t)mc.gregs[REG_RIP];
#elif defined(__aarch64__)
        return (uintptr_t)mc.pc;
#elif defined(__arm__)
        return (uintptr_t)mc.arm_pc;
#elif defined(__riscv)
        return (uintptr_t)mc.__gregs[REG_PC];
#elif defined(__loongarch64)
        return (uintptr_t)mc.__pc;
#endif
#endif
    }

    static bool IsInJit(uintptr_t ip)
    {
        int count = s_codeRangeCount.load(std::memory_order_acquire);
        for (int i = 0; i < count; i++)
        {
            if (ip >= s_codeRanges[i].start && ip < s_codeRanges[i].end)
                return true;
        }
        return false;
    }

    static void ChainToPreviousHandler(int signal, siginfo_t* info, void* context)
    {
        const struct sigaction* previous = nullptr;
        for (int i = 0; i < SignalCount; i++)
        {
            if (s_signals[i] == signal)
                previous = &s_previousActions[i];
        }

        if ((previous->sa_flags & SA_SIGINFO) != 0)
        {
            previous->sa_sigaction(signal, info, context);
        }
        else if (previous->sa_handler != SIG_DFL && previous->sa_handler != SIG_IGN)
        {
            previous->sa_handler(signal);
        }
        else
        {
            // The fault happens again when the handler returns, with the default action; a signal that was sent
            // rather than caused by an instruction is sent again.
            struct sigaction action = {};
            action.sa_handler = SIG_DFL;
            sigaction(signal, &action, nullptr);
            if (info->si_code <= 0)
                raise(signal);
        }
    }

    static void Handler(int signal, siginfo_t* info, void* context)
    {
        Scope* scope = IsInJit(GetInstructionPointer(context)) ? t_scope : nullptr;
        if (scope != nullptr)
        {
            scope->signal = signal;
            scope->code = info->si_code;
            siglongjmp(scope->jmp, 1);
        }

        ChainToPreviousHandler(signal, info, context);
    }

    static void AddCodeRange(uintptr_t start, uintptr_t end)
    {
        int count = s_codeRangeCount.load(std::memory_order_relaxed);
        if (count < MaxCodeRanges)
        {
            s_codeRanges[count] = {start, end};
            s_codeRangeCount.store(count + 1, std::memory_order_release);
        }
    }

#ifdef __APPLE__
    // Adds the executable segments of the image that contains 'address'.
    static void AddModuleContaining(uintptr_t address)
    {
        Dl_info info;
        if (dladdr((const void*)address, &info) == 0 || info.dli_fbase == nullptr)
            return;

        const mach_header_64* header = static_cast<const mach_header_64*>(info.dli_fbase);
        if (header->magic != MH_MAGIC_64)
            return;

        // The image is loaded at the address of its header, which starts its __TEXT segment.
        const load_command* command = reinterpret_cast<const load_command*>(header + 1);
        uintptr_t slide = 0;
        for (uint32_t i = 0; i < header->ncmds; i++)
        {
            const segment_command_64* segment = reinterpret_cast<const segment_command_64*>(command);
            if (command->cmd == LC_SEGMENT_64 && strcmp(segment->segname, SEG_TEXT) == 0)
                slide = (uintptr_t)header - (uintptr_t)segment->vmaddr;
            command = reinterpret_cast<const load_command*>(reinterpret_cast<const char*>(command) + command->cmdsize);
        }

        command = reinterpret_cast<const load_command*>(header + 1);
        for (uint32_t i = 0; i < header->ncmds; i++)
        {
            const segment_command_64* segment = reinterpret_cast<const segment_command_64*>(command);
            if (command->cmd == LC_SEGMENT_64 && (segment->initprot & VM_PROT_EXECUTE) != 0)
                AddCodeRange(slide + (uintptr_t)segment->vmaddr, slide + (uintptr_t)segment->vmaddr + (uintptr_t)segment->vmsize);
            command = reinterpret_cast<const load_command*>(reinterpret_cast<const char*>(command) + command->cmdsize);
        }
    }
#else
    struct FindModuleParam
    {
        uintptr_t address;
        bool found;
    };

    // Adds the executable segments of the module that contains 'address', if it is the one.
    static int AddModuleIfContains(dl_phdr_info* info, size_t, void* p)
    {
        FindModuleParam* param = static_cast<FindModuleParam*>(p);
        bool contains = false;
        for (int i = 0; i < info->dlpi_phnum; i++)
        {
            const ElfW(Phdr)& phdr = info->dlpi_phdr[i];
            uintptr_t start = info->dlpi_addr + phdr.p_vaddr;
            if (phdr.p_type == PT_LOAD && param->address >= start && param->address < start + phdr.p_memsz)
                contains = true;
        }
        if (!contains)
            return 0;

        for (int i = 0; i < info->dlpi_phnum; i++)
        {
            const ElfW(Phdr)& phdr = info->dlpi_phdr[i];
            if (phdr.p_type == PT_LOAD && (phdr.p_flags & PF_X) != 0)
            {
                uintptr_t start = info->dlpi_addr + phdr.p_vaddr;
                AddCodeRange(start, start + phdr.p_memsz);
            }
        }
        param->found = true;
        return 1;
    }

    // Adds the executable segments of the module that contains 'address'.
    static void AddModuleContaining(uintptr_t address)
    {
        FindModuleParam param = {address, false};
        dl_iterate_phdr(AddModuleIfContains, &param);
    }
#endif

    // Adds the JIT (the module with the code at 'jitAddress') to those whose faults are caught, and installs the
    // handlers the first time.
    static void AddJit(const void* jitAddress)
    {
        std::lock_guard<std::mutex> lock(s_lock);

        static bool s_installed = false;
        if (!s_installed)
        {
            for (int i = 0; i < SignalCount; i++)
            {
                struct sigaction action = {};
                action.sa_sigaction = Handler;
                action.sa_flags = SA_SIGINFO | SA_RESTART;
                sigemptyset(&action.sa_mask);

                // As NativeAOT's AddSignalHandler: the previous handler, which this one calls for the faults outside of
                // the JIT, runs on the kind of stack that it expects (the runtime handles the managed exceptions on the
                // thread's stack, and only the stack overflows on the alternate stack), with the signals it blocks.
                sigaction(s_signals[i], nullptr, &s_previousActions[i]);
                if ((s_previousActions[i].sa_flags & SA_ONSTACK) != 0)
                {
                    action.sa_flags |= SA_ONSTACK;
                    action.sa_mask = s_previousActions[i].sa_mask;
                }
                sigaction(s_signals[i], &action, &s_previousActions[i]);
            }
            s_installed = true;
        }

        static const void* s_jits[MaxCodeRanges];
        static int s_jitCount = 0;
        for (int i = 0; i < s_jitCount; i++)
        {
            if (s_jits[i] == jitAddress)
                return;
        }

        AddModuleContaining((uintptr_t)jitAddress);
        if (s_jitCount < MaxCodeRanges)
            s_jits[s_jitCount++] = jitAddress;
    }

    // Restores the scope of the caller when the call returns, or when an exception that isn't a fault escapes it.
    struct ScopeRestorer
    {
        Scope* previous;

        ~ScopeRestorer()
        {
            t_scope = previous;
        }
    };

    // Calls 'call', which runs the JIT, and returns 0; or the signal (and in *pCode, the si_code) of a fault in the
    // code of the JIT, which abandons the call. 'jit' is the ICorJitCompiler, whose vtable is in the JIT module.
    template <typename F>
    static int CallCatchingJitFaults(const void* jit, F& call, int* pCode)
    {
        AddJit(*static_cast<void* const*>(jit));

        Scope scope;
        scope.signal = 0;
        scope.code = 0;
        ScopeRestorer restorer = {t_scope};
        if (sigsetjmp(scope.jmp, 1) == 0)
        {
            t_scope = &scope;
            call();
            return 0;
        }

        *pCode = scope.code;
        return scope.signal;
    }

    // The code of the Windows exception that the fault corresponds to, which SuperPMI reports.
    static uint32_t ExceptionCode(int signal, int code)
    {
        switch (signal)
        {
            case SIGFPE:
                return code == FPE_INTOVF ? 0xC0000095 : 0xC0000094; // EXCEPTION_INT_OVERFLOW, EXCEPTION_INT_DIVIDE_BY_ZERO
            case SIGILL:
                return 0xC000001D; // EXCEPTION_ILLEGAL_INSTRUCTION
            default:
                return 0xC0000005; // EXCEPTION_ACCESS_VIOLATION
        }
    }
}

#endif

#endif // JITFAULTGUARD_H
