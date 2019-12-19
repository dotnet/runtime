#if defined(_X86_) || defined(_AMD64_)
.intel_syntax noprefix
#endif

#include "unixasmmacros.inc"

#if defined(_ARM_)
.syntax unified
CLR_ARM_ASM_COMPILATION_MODE
#endif

#include "palredefines.inc"
