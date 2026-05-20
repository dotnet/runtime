//===----------------------------------------------------------------------===//
//
// Part of the LLVM Project, under the Apache License v2.0 with LLVM Exceptions.
// See https://llvm.org/LICENSE.txt for license information.
// SPDX-License-Identifier: Apache-2.0 WITH LLVM-exception
//
//
//  Implements ARM zero-cost C++ exceptions
//
//===----------------------------------------------------------------------===//

#include "Unwind-EHABI.h"

#if defined(_LIBUNWIND_ARM_EHABI)

#include <inttypes.h>
#include <stdbool.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#include "config.h"
#include "libunwind.h"
#include "libunwind_ext.h"
#include "unwind.h"

namespace {

// Strange order: take words in order, but inside word, take from most to least
// signinficant byte.
uint8_t getByte(const uint32_t* data, size_t offset) {
  const uint8_t* byteData = reinterpret_cast<const uint8_t*>(data);
#if __BYTE_ORDER__ == __ORDER_LITTLE_ENDIAN__
  return byteData[(offset & ~(size_t)0x03) + (3 - (offset & (size_t)0x03))];
#elif __BYTE_ORDER__ == __ORDER_BIG_ENDIAN__
  return byteData[offset];
#else
#error "Unable to determine endianess"
#endif
}

struct Descriptor {
  // See # 9.2
  typedef enum {
    SU16 = 0, // Short descriptor, 16-bit entries
    LU16 = 1, // Long descriptor,  16-bit entries
    LU32 = 3, // Long descriptor,  32-bit entries
    RESERVED0 =  4, RESERVED1 =  5, RESERVED2  = 6,  RESERVED3  =  7,
    RESERVED4 =  8, RESERVED5 =  9, RESERVED6  = 10, RESERVED7  = 11,
    RESERVED8 = 12, RESERVED9 = 13, RESERVED10 = 14, RESERVED11 = 15
  } Format;
};


// Generates mask discriminator for _Unwind_VRS_Pop, e.g. for _UVRSC_CORE /
// _UVRSD_UINT32.
uint32_t RegisterMask(uint8_t start, uint8_t count_minus_one) {
  return ((1U << (count_minus_one + 1)) - 1) << start;
}

// Generates mask discriminator for _Unwind_VRS_Pop, e.g. for _UVRSC_VFP /
// _UVRSD_DOUBLE.
uint32_t RegisterRange(uint8_t start, uint8_t count_minus_one) {
  return ((uint32_t)start << 16) | ((uint32_t)count_minus_one + 1);
}

} // end anonymous namespace

/**
 * Decodes an EHT entry.
 *
 * @param data Pointer to EHT.
 * @param[out] off Offset from return value (in bytes) to begin interpretation.
 * @param[out] len Number of bytes in unwind code.
 * @return Pointer to beginning of unwind code.
 */
extern "C" const uint32_t*
decode_eht_entry(const uint32_t* data, size_t* off, size_t* len) {
  if ((*data & 0x80000000) == 0) {
    // 6.2: Generic Model
    //
    // EHT entry is a prel31 pointing to the PR, followed by data understood
    // only by the personality routine. Fortunately, all existing assembler
    // implementations, including GNU assembler, LLVM integrated assembler,
    // and ARM assembler, assume that the unwind opcodes come after the
    // personality routine address.
    *off = 1; // First byte is size data.
    *len = (((data[1] >> 24) & 0xff) + 1) * 4;
    data++; // Skip the first word, which is the prel31 offset.
  } else {
    // 6.3: ARM Compact Model
    //
    // EHT entries here correspond to the __aeabi_unwind_cpp_pr[012] PRs indeed
    // by format:
    Descriptor::Format format =
        static_cast<Descriptor::Format>((*data & 0x0f000000) >> 24);
    switch (format) {
      case Descriptor::SU16:
        *len = 4;
        *off = 1;
        break;
      case Descriptor::LU16:
      case Descriptor::LU32:
        *len = 4 + 4 * ((*data & 0x00ff0000) >> 16);
        *off = 2;
        break;
      default:
        return nullptr;
    }
  }
  return data;
}

_LIBUNWIND_EXPORT _Unwind_Reason_Code
_Unwind_VRS_Interpret(_Unwind_Context *context, const uint32_t *data,
                      size_t offset, size_t len) {
  bool wrotePC = false;
  bool finish = false;
  bool hasReturnAddrAuthCode = false;
  while (offset < len && !finish) {
    uint8_t byte = getByte(data, offset++);
    if ((byte & 0x80) == 0) {
      uint32_t sp;
      _Unwind_VRS_Get(context, _UVRSC_CORE, UNW_ARM_SP, _UVRSD_UINT32, &sp);
      if (byte & 0x40)
        sp -= (((uint32_t)byte & 0x3f) << 2) + 4;
      else
        sp += ((uint32_t)byte << 2) + 4;
      _Unwind_VRS_Set(context, _UVRSC_CORE, UNW_ARM_SP, _UVRSD_UINT32, &sp, NULL);
    } else {
      switch (byte & 0xf0) {
        case 0x80: {
          if (offset >= len)
            return _URC_FAILURE;
          uint32_t registers =
              (((uint32_t)byte & 0x0f) << 12) |
              (((uint32_t)getByte(data, offset++)) << 4);
          if (!registers)
            return _URC_FAILURE;
          if (registers & (1 << 15))
            wrotePC = true;
          _Unwind_VRS_Pop(context, _UVRSC_CORE, registers, _UVRSD_UINT32);
          break;
        }
        case 0x90: {
          uint8_t reg = byte & 0x0f;
          if (reg == 13 || reg == 15)
            return _URC_FAILURE;
          uint32_t sp;
          _Unwind_VRS_Get(context, _UVRSC_CORE, UNW_ARM_R0 + reg,
                          _UVRSD_UINT32, &sp);
          _Unwind_VRS_Set(context, _UVRSC_CORE, UNW_ARM_SP, _UVRSD_UINT32,
                          &sp, NULL);
          break;
        }
        case 0xa0: {
          uint32_t registers = RegisterMask(4, byte & 0x07);
          if (byte & 0x08)
            registers |= 1 << 14;
          _Unwind_VRS_Pop(context, _UVRSC_CORE, registers, _UVRSD_UINT32);
          break;
        }
        case 0xb0: {
          switch (byte) {
            case 0xb0:
              finish = true;
              break;
            case 0xb1: {
              if (offset >= len)
                return _URC_FAILURE;
              uint8_t registers = getByte(data, offset++);
              if (registers & 0xf0 || !registers)
                return _URC_FAILURE;
              _Unwind_VRS_Pop(context, _UVRSC_CORE, registers, _UVRSD_UINT32);
              break;
            }
            case 0xb2: {
              uint32_t addend = 0;
              uint32_t shift = 0;
              // This decodes a uleb128 value.
              while (true) {
                if (offset >= len)
                  return _URC_FAILURE;
                uint32_t v = getByte(data, offset++);
                addend |= (v & 0x7f) << shift;
                if ((v & 0x80) == 0)
                  break;
                shift += 7;
              }
              uint32_t sp;
              _Unwind_VRS_Get(context, _UVRSC_CORE, UNW_ARM_SP, _UVRSD_UINT32,
                              &sp);
              sp += 0x204 + (addend << 2);
              _Unwind_VRS_Set(context, _UVRSC_CORE, UNW_ARM_SP, _UVRSD_UINT32,
                              &sp, NULL);
              break;
            }
            case 0xb3: {
              uint8_t v = getByte(data, offset++);
              _Unwind_VRS_Pop(context, _UVRSC_VFP,
                              RegisterRange(static_cast<uint8_t>(v >> 4),
                                            v & 0x0f), _UVRSD_VFPX);
              break;
            }
            case 0xb4:
              hasReturnAddrAuthCode = true;
              _Unwind_VRS_Pop(context, _UVRSC_PSEUDO,
                              0 /* Return Address Auth Code */, _UVRSD_UINT32);
              break;
            case 0xb5:
            case 0xb6:
            case 0xb7:
              return _URC_FAILURE;
            default:
              _Unwind_VRS_Pop(context, _UVRSC_VFP,
                              RegisterRange(8, byte & 0x07), _UVRSD_VFPX);
              break;
          }
          break;
        }
        case 0xc0: {
          switch (byte) {
#if defined(__ARM_WMMX)
            case 0xc0:
            case 0xc1:
            case 0xc2:
            case 0xc3:
            case 0xc4:
            case 0xc5:
              _Unwind_VRS_Pop(context, _UVRSC_WMMXD,
                              RegisterRange(10, byte & 0x7), _UVRSD_DOUBLE);
              break;
            case 0xc6: {
              uint8_t v = getByte(data, offset++);
              uint8_t start = static_cast<uint8_t>(v >> 4);
              uint8_t count_minus_one = v & 0xf;
              if (start + count_minus_one >= 16)
                return _URC_FAILURE;
              _Unwind_VRS_Pop(context, _UVRSC_WMMXD,
                              RegisterRange(start, count_minus_one),
                              _UVRSD_DOUBLE);
              break;
            }
            case 0xc7: {
              uint8_t v = getByte(data, offset++);
              if (!v || v & 0xf0)
                return _URC_FAILURE;
              _Unwind_VRS_Pop(context, _UVRSC_WMMXC, v, _UVRSD_DOUBLE);
              break;
            }
#endif
            case 0xc8:
            case 0xc9: {
              uint8_t v = getByte(data, offset++);
              uint8_t start =
                  static_cast<uint8_t>(((byte == 0xc8) ? 16 : 0) + (v >> 4));
              uint8_t count_minus_one = v & 0xf;
              if (start + count_minus_one >= 32)
                return _URC_FAILURE;
              _Unwind_VRS_Pop(context, _UVRSC_VFP,
                              RegisterRange(start, count_minus_one),
                              _UVRSD_DOUBLE);
              break;
            }
            default:
              return _URC_FAILURE;
          }
          break;
        }
        case 0xd0: {
          if (byte & 0x08)
            return _URC_FAILURE;
          _Unwind_VRS_Pop(context, _UVRSC_VFP, RegisterRange(8, byte & 0x7),
                          _UVRSD_DOUBLE);
          break;
        }
        default:
          return _URC_FAILURE;
      }
    }
  }
  if (!wrotePC) {
    uint32_t lr;
    _Unwind_VRS_Get(context, _UVRSC_CORE, UNW_ARM_LR, _UVRSD_UINT32, &lr);
#ifdef __ARM_FEATURE_PAUTH
    if (hasReturnAddrAuthCode) {
      uint32_t sp;
      uint32_t pac;
      _Unwind_VRS_Get(context, _UVRSC_CORE, UNW_ARM_SP, _UVRSD_UINT32, &sp);
      _Unwind_VRS_Get(context, _UVRSC_PSEUDO, 0, _UVRSD_UINT32, &pac);
      __asm__ __volatile__("autg %0, %1, %2" : : "r"(pac), "r"(lr), "r"(sp) :);
    }
#else
    (void)hasReturnAddrAuthCode;
#endif
    _Unwind_VRS_Set(context, _UVRSC_CORE, UNW_ARM_IP, _UVRSD_UINT32, &lr, NULL);
  }
  return _URC_CONTINUE_UNWIND;
}


#endif  // defined(_LIBUNWIND_ARM_EHABI)
