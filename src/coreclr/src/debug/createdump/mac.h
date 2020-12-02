// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <mach/mach.h>
#include <mach/mach_vm.h>

#define AT_SYSINFO_EHDR    33

#if TARGET_64BIT
#define TARGET_WORDSIZE 64
#else
#define TARGET_WORDSIZE 32
#endif

#ifndef ElfW
/* We use this macro to refer to ELF types independent of the native wordsize.
   `ElfW(TYPE)' is used in place of `Elf32_TYPE' or `Elf64_TYPE'.  */
#define ElfW(type)      _ElfW (Elf, TARGET_WORDSIZE, type)
#define _ElfW(e,w,t)    _ElfW_1 (e, w, _##t)
#define _ElfW_1(e,w,t)  e##w##t
#endif

#define ELFMAG0     0x7f    /* Magic number byte 0 */
#define ELFMAG1     'E'     /* Magic number byte 1 */
#define ELFMAG2     'L'     /* Magic number byte 2 */
#define ELFMAG3     'F'     /* Magic number byte 3 */

enum {
  NT_PRSTATUS = 1,
  NT_FPREGSET,
  NT_PRPSINFO,
  NT_TASKSTRUCT,
  NT_PLATFORM,
  NT_AUXV,
  NT_FILE = 0x46494c45,
  NT_SIGINFO = 0x53494749,
  NT_PPC_VMX = 0x100,
  NT_PPC_VSX = 0x102,
  NT_PRXFPREG = 0x46e62b7f,
};

typedef struct
{
  uint64_t a_type;      /* Entry type */
  union
    {
      uint64_t a_val;   /* Integer value */
      /* We use to have pointer elements added here.  We cannot do that,
         though, since it does not work when using 32-bit definitions
         on 64-bit platforms and vice versa.  */
    } a_un;
} Elf64_auxv_t;

#define AT_NULL   0	/* end of vector */
#define AT_BASE   7	/* base address of interpreter */

/* Note header in a PT_NOTE section */
typedef struct elf32_note {
  Elf32_Word    n_namesz;    /* Name size */
  Elf32_Word    n_descsz;    /* Content size */
  Elf32_Word    n_type;        /* Content type */
} Elf32_Nhdr;

/* Note header in a PT_NOTE section */
typedef struct elf64_note {
  Elf64_Word n_namesz;    /* Name size */
  Elf64_Word n_descsz;    /* Content size */
  Elf64_Word n_type;    /* Content type */
} Elf64_Nhdr;

#if defined(TARGET_AMD64)
struct user_fpregs_struct
{
  unsigned short int    cwd;
  unsigned short int    swd;
  unsigned short int    ftw;
  unsigned short int    fop;
  unsigned long long int rip;
  unsigned long long int rdp;
  unsigned int        mxcsr;
  unsigned int        mxcr_mask;
  unsigned int        st_space[32];   /* 8*16 bytes for each FP-reg = 128 bytes */
  unsigned int        xmm_space[64];  /* 16*16 bytes for each XMM-reg = 256 bytes */
  unsigned int        padding[24];
};

struct user_regs_struct
{
  unsigned long long int r15;
  unsigned long long int r14;
  unsigned long long int r13;
  unsigned long long int r12;
  unsigned long long int rbp;
  unsigned long long int rbx;
  unsigned long long int r11;
  unsigned long long int r10;
  unsigned long long int r9;
  unsigned long long int r8;
  unsigned long long int rax;
  unsigned long long int rcx;
  unsigned long long int rdx;
  unsigned long long int rsi;
  unsigned long long int rdi;
  unsigned long long int orig_rax;
  unsigned long long int rip;
  unsigned long long int cs;
  unsigned long long int eflags;
  unsigned long long int rsp;
  unsigned long long int ss;
  unsigned long long int fs_base;
  unsigned long long int gs_base;
  unsigned long long int ds;
  unsigned long long int es;
  unsigned long long int fs;
  unsigned long long int gs;
};
#elif defined(TARGET_ARM64)
struct user_fpsimd_struct
{
  uint64_t vregs[2*32];
  uint32_t fpcr;
  uint32_t fpsr;
};

struct user_regs_struct
{
  uint64_t regs[31];
  uint64_t sp;
  uint64_t pc;
  uint32_t pstate;
};
#else
#error Unexpected architecture
#endif


typedef pid_t __pid_t;

/* Type for a general-purpose register.  */
#ifdef __x86_64__
typedef unsigned long long elf_greg_t;
#else
typedef unsigned long elf_greg_t;
#endif

/* And the whole bunch of them.  We could have used `struct
   user_regs_struct' directly in the typedef, but tradition says that
   the register set is an array, which does have some peculiar
   semantics, so leave it that way.  */
#define ELF_NGREG (sizeof (struct user_regs_struct) / sizeof(elf_greg_t))
typedef elf_greg_t elf_gregset_t[ELF_NGREG];

/* Signal info.  */
struct elf_siginfo
  {
    int si_signo;           /* Signal number.  */
    int si_code;            /* Extra code.  */
    int si_errno;           /* Errno.  */
  };

/* Definitions to generate Intel SVR4-like core files.  These mostly
   have the same names as the SVR4 types with "elf_" tacked on the
   front to prevent clashes with Linux definitions, and the typedef
   forms have been avoided.  This is mostly like the SVR4 structure,
   but more Linuxy, with things that Linux does not support and which
   GDB doesn't really use excluded.  */

struct elf_prstatus
  {
    struct elf_siginfo pr_info;     /* Info associated with signal.  */
    short int pr_cursig;            /* Current signal.  */
    unsigned long int pr_sigpend;   /* Set of pending signals.  */
    unsigned long int pr_sighold;   /* Set of held signals.  */
    __pid_t pr_pid;
    __pid_t pr_ppid;
    __pid_t pr_pgrp;
    __pid_t pr_sid;
    struct timeval pr_utime;        /* User time.  */
    struct timeval pr_stime;        /* System time.  */
    struct timeval pr_cutime;       /* Cumulative user time.  */
    struct timeval pr_cstime;       /* Cumulative system time.  */
    elf_gregset_t pr_reg;           /* GP registers.  */
    int pr_fpvalid;                 /* True if math copro being used.  */
  };


#define ELF_PRARGSZ     (80)        /* Number of chars for args.  */

struct elf_prpsinfo
  {
    char pr_state;                  /* Numeric process state.  */
    char pr_sname;                  /* Char for pr_state.  */
    char pr_zomb;                   /* Zombie.  */
    char pr_nice;                   /* Nice val.  */
    unsigned long int pr_flag;      /* Flags.  */
#if __WORDSIZE == 32
    unsigned short int pr_uid;
    unsigned short int pr_gid;
#else
    unsigned int pr_uid;
    unsigned int pr_gid;
#endif
    int pr_pid, pr_ppid, pr_pgrp, pr_sid;
    /* Lots missing */
    char pr_fname[16];              /* Filename of executable.  */
    char pr_psargs[ELF_PRARGSZ];    /* Initial part of arg list.  */
  };

/* Process status and info.  In the end we do provide typedefs for them.  */
typedef struct elf_prstatus prstatus_t;
typedef struct elf_prpsinfo prpsinfo_t;
