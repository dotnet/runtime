// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef NIBBLEMAPMACROS_H_
#define NIBBLEMAPMACROS_H_

///////////////////////////////////////////////////////////////////////
////   some mmgr stuff for JIT, especially for jit code blocks
///////////////////////////////////////////////////////////////////////
//
// In order to quickly find the start of a jit code block
// we keep track of all those positions via a map.
// Each entry in this map represents 32 byte (a bucket) of the code heap.
// We make the assumption that no two code-blocks can start in
// the same 32byte bucket;
// Additionally we assume that every code header is DWORD aligned.
// Because we cannot guarantee that jitblocks always start at
// multiples of 32 bytes we cannot use a simple bitmap; instead we
// use a nibble (4 bit) per bucket and encode the offset of the header
// inside the bucket (in DWORDS). In order to make initialization
// easier we add one to the real offset, a nibble-value of zero
// means that there is no header start in the resp. bucket.
// In order to speed up "backwards scanning" we start numbering
// nibbles inside a DWORD from the highest bits (28..31). Because
// of that we can scan backwards inside the DWORD with right shifts.

#if defined(HOST_64BIT)
// TODO: bump up the windows CODE_ALIGN to 16 and iron out any nibble map bugs that exist.
// TODO: there is something wrong with USE_INDIRECT_CODEHEADER with CODE_ALIGN=16
# define CODE_ALIGN             4
# define LOG2_CODE_ALIGN        2
#else
# define CODE_ALIGN             sizeof(DWORD)                                // 4 byte boundry
# define LOG2_CODE_ALIGN        2
#endif
#define NIBBLE_MASK             0xf
#define NIBBLE_SIZE             4                                            // 4 bits
#define LOG2_NIBBLE_SIZE        2
#define NIBBLES_PER_DWORD       ((8*sizeof(DWORD)) >> LOG2_NIBBLE_SIZE)      // 8 (4-bit) nibbles per dword
#define NIBBLES_PER_DWORD_MASK  (NIBBLES_PER_DWORD - 1)                      // 7
#define LOG2_NIBBLES_PER_DWORD  3
#define BYTES_PER_BUCKET        (NIBBLES_PER_DWORD * CODE_ALIGN)             // 32 bytes per bucket
#define LOG2_BYTES_PER_BUCKET   (LOG2_CODE_ALIGN + LOG2_NIBBLES_PER_DWORD)   //  5 bits per bucket
#define MASK_BYTES_PER_BUCKET   (BYTES_PER_BUCKET - 1)                       // 31
#define HIGHEST_NIBBLE_BIT      (32 - NIBBLE_SIZE)                           // 28 (i.e 32 - 4)
#define HIGHEST_NIBBLE_MASK     (NIBBLE_MASK << HIGHEST_NIBBLE_BIT)          // 0xf0000000

#define ADDR2POS(x)                      ((x) >> LOG2_BYTES_PER_BUCKET)
#define ADDR2OFFS(x)            (DWORD)  ((((x) & MASK_BYTES_PER_BUCKET) >> LOG2_CODE_ALIGN) + 1)
#define POSOFF2ADDR(pos, of)    (size_t) (((pos) << LOG2_BYTES_PER_BUCKET) + (((of) - 1) << LOG2_CODE_ALIGN))
#define HEAP2MAPSIZE(x)                  (((x) / (BYTES_PER_BUCKET * NIBBLES_PER_DWORD)) * CODE_ALIGN)
#define POS2SHIFTCOUNT(x)       (DWORD)  (HIGHEST_NIBBLE_BIT - (((x) & NIBBLES_PER_DWORD_MASK) << LOG2_NIBBLE_SIZE))
#define POS2MASK(x)             (DWORD) ~(HIGHEST_NIBBLE_MASK >> (((x) & NIBBLES_PER_DWORD_MASK) << LOG2_NIBBLE_SIZE))

#endif  // NIBBLEMAPMACROS_H_
