#if defined(HOST_X86) || defined(HOST_AMD64)
.intel_syntax noprefix
#endif

#include "unixasmmacros.inc"

#if defined(HOST_ARM)
.syntax unified
.thumb
#endif

#include "libredefines.inc"
