// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Minimal NativeAOT-specific process reader for crash dump generation.
// Reads memory regions, threads, and registers via /proc and ptrace.
// Pure C — no C++ runtime dependency.

#ifndef PROCESS_READER_H
#define PROCESS_READER_H

#include <stdint.h>
#include <stddef.h>
#include <stdbool.h>
#include <sys/types.h>
#include <sys/user.h>
#include <elf.h>
#include <signal.h>

#ifdef __x86_64__
typedef struct user_regs_struct gp_regs_t;
typedef struct user_fpregs_struct fp_regs_t;
#elif defined(__aarch64__)
// aarch64 uses user_pt_regs and user_fpsimd_state
#include <asm/ptrace.h>
typedef struct user_pt_regs gp_regs_t;
typedef struct user_fpsimd_state fp_regs_t;
#else
#error "Unsupported architecture for NativeAOT createdump"
#endif

// Flag for private mappings (MAP_PRIVATE), stored alongside PF_R/PF_W/PF_X.
#define MR_PRIVATE 0x10

// Memory region from /proc/<pid>/maps
typedef struct
{
    uint64_t startAddress;
    uint64_t endAddress;
    uint64_t offset;
    uint32_t deviceMajor;
    uint32_t deviceMinor;
    uint64_t inode;
    uint32_t permissions; // PF_R | PF_W | PF_X | MR_PRIVATE
    char* fileName;
} MemRegion;

// Thread info
typedef struct
{
    pid_t tid;
    gp_regs_t gpRegs;
    fp_regs_t fpRegs;
    size_t fpRegsSize;
    bool isCrashThread;
} ThreadData;

// Dynamic array helpers
typedef struct
{
    MemRegion* items;
    size_t count;
    size_t capacity;
} MemRegionArray;

typedef struct
{
    ThreadData* items;
    size_t count;
    size_t capacity;
} ThreadArray;

typedef struct
{
    Elf64_auxv_t* items;
    size_t count;
    size_t capacity;
} AuxvArray;

// Process snapshot containing all data needed for dump generation
typedef struct
{
    pid_t pid;
    pid_t ppid;
    pid_t pgrp;
    uint64_t pageSize;
    char name[256];
    char exePath[4096]; // /proc/pid/exe target
    int crashSignal;
    pid_t crashThread;
    int signalCode;
    int signalErrno;
    uint64_t signalAddress;
    MemRegionArray regions;
    ThreadArray threads;
    AuxvArray auxv;
} ProcessInfo;

// Initialize/cleanup
bool ProcessInfoInit(ProcessInfo* info, pid_t pid, int crashSignal, pid_t crashThread,
                     int signalCode, int signalErrno, uint64_t signalAddress);
void ProcessInfoCleanup(ProcessInfo* info);

// Gather process data
bool ReadMemoryRegions(ProcessInfo* info);
bool EnumerateAndAttachThreads(ProcessInfo* info);
bool ReadThreadRegisters(ProcessInfo* info);
bool ReadAuxv(ProcessInfo* info);
bool ReadProcessStatus(ProcessInfo* info);
void DetachThreads(ProcessInfo* info);

#endif // PROCESS_READER_H
