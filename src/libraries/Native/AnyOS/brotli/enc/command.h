/* Copyright 2013 Google Inc. All Rights Reserved.

   Distributed under MIT license.
   See file LICENSE for detail or copy at https://opensource.org/licenses/MIT
*/

/* This class models a sequence of literals and a backward reference copy. */

#ifndef BROTLI_ENC_COMMAND_H_
#define BROTLI_ENC_COMMAND_H_

#include "../common/constants.h"
#include <brotli/port.h>
#include <brotli/types.h>
#include "./fast_log.h"
#include "./prefix.h"

#if defined(__cplusplus) || defined(c_plusplus)
extern "C" {
#endif

static uint32_t kInsBase[] =   { 0, 1, 2, 3, 4, 5, 6, 8, 10, 14, 18, 26, 34, 50,
    66, 98, 130, 194, 322, 578, 1090, 2114, 6210, 22594 };
static uint32_t kInsExtra[] =  { 0, 0, 0, 0, 0, 0, 1, 1,  2,  2,  3,  3,  4,  4,
    5,   5,   6,   7,   8,   9,   10,   12,   14,    24 };
static uint32_t kCopyBase[] =  { 2, 3, 4, 5, 6, 7, 8, 9, 10, 12, 14, 18, 22, 30,
    38, 54,  70, 102, 134, 198, 326,   582, 1094,  2118 };
static uint32_t kCopyExtra[] = { 0, 0, 0, 0, 0, 0, 0, 0,  1,  1,  2,  2,  3,  3,
     4,  4,   5,   5,   6,   7,   8,     9,   10,    24 };

static BROTLI_INLINE uint16_t GetInsertLengthCode(size_t insertlen) {
  if (insertlen < 6) {
    return (uint16_t)insertlen;
  } else if (insertlen < 130) {
    uint32_t nbits = Log2FloorNonZero(insertlen - 2) - 1u;
    return (uint16_t)((nbits << 1) + ((insertlen - 2) >> nbits) + 2);
  } else if (insertlen < 2114) {
    return (uint16_t)(Log2FloorNonZero(insertlen - 66) + 10);
  } else if (insertlen < 6210) {
    return 21u;
  } else if (insertlen < 22594) {
    return 22u;
  } else {
    return 23u;
  }
}

static BROTLI_INLINE uint16_t GetCopyLengthCode(size_t copylen) {
  if (copylen < 10) {
    return (uint16_t)(copylen - 2);
  } else if (copylen < 134) {
    uint32_t nbits = Log2FloorNonZero(copylen - 6) - 1u;
    return (uint16_t)((nbits << 1) + ((copylen - 6) >> nbits) + 4);
  } else if (copylen < 2118) {
    return (uint16_t)(Log2FloorNonZero(copylen - 70) + 12);
  } else {
    return 23u;
  }
}

static BROTLI_INLINE uint16_t CombineLengthCodes(
    uint16_t inscode, uint16_t copycode, BROTLI_BOOL use_last_distance) {
  uint16_t bits64 =
      (uint16_t)((copycode & 0x7u) | ((inscode & 0x7u) << 3));
  if (use_last_distance && inscode < 8 && copycode < 16) {
    return (copycode < 8) ? bits64 : (bits64 | 64);
  } else {
    /* Specification: 5 Encoding of ... (last table) */
    /* offset = 2 * index, where index is in range [0..8] */
    int offset = 2 * ((copycode >> 3) + 3 * (inscode >> 3));
    /* All values in specification are K * 64,
       where   K = [2, 3, 6, 4, 5, 8, 7, 9, 10],
           i + 1 = [1, 2, 3, 4, 5, 6, 7, 8,  9],
       K - i - 1 = [1, 1, 3, 0, 0, 2, 0, 1,  2] = D.
       All values in D require only 2 bits to encode.
       Magic constant is shifted 6 bits left, to avoid final multiplication. */
    offset = (offset << 5) + 0x40 + ((0x520D40 >> offset) & 0xC0);
    return (uint16_t)offset | bits64;
  }
}

static BROTLI_INLINE void GetLengthCode(size_t insertlen, size_t copylen,
                                        BROTLI_BOOL use_last_distance,
                                        uint16_t* code) {
  uint16_t inscode = GetInsertLengthCode(insertlen);
  uint16_t copycode = GetCopyLengthCode(copylen);
  *code = CombineLengthCodes(inscode, copycode, use_last_distance);
}

static BROTLI_INLINE uint32_t GetInsertBase(uint16_t inscode) {
  return kInsBase[inscode];
}

static BROTLI_INLINE uint32_t GetInsertExtra(uint16_t inscode) {
  return kInsExtra[inscode];
}

static BROTLI_INLINE uint32_t GetCopyBase(uint16_t copycode) {
  return kCopyBase[copycode];
}

static BROTLI_INLINE uint32_t GetCopyExtra(uint16_t copycode) {
  return kCopyExtra[copycode];
}

typedef struct Command {
  uint32_t insert_len_;
  /* Stores copy_len in low 24 bits and copy_len XOR copy_code in high 8 bit. */
  uint32_t copy_len_;
  uint32_t dist_extra_;
  uint16_t cmd_prefix_;
  uint16_t dist_prefix_;
} Command;

/* distance_code is e.g. 0 for same-as-last short code, or 16 for offset 1. */
static BROTLI_INLINE void InitCommand(Command* self, size_t insertlen,
    size_t copylen, int copylen_code_delta, size_t distance_code) {
  /* Don't rely on signed int representation, use honest casts. */
  uint32_t delta = (uint8_t)((int8_t)copylen_code_delta);
  self->insert_len_ = (uint32_t)insertlen;
  self->copy_len_ = (uint32_t)(copylen | (delta << 24));
  /* The distance prefix and extra bits are stored in this Command as if
     npostfix and ndirect were 0, they are only recomputed later after the
     clustering if needed. */
  PrefixEncodeCopyDistance(
      distance_code, 0, 0, &self->dist_prefix_, &self->dist_extra_);
  GetLengthCode(
      insertlen, (size_t)((int)copylen + copylen_code_delta),
      TO_BROTLI_BOOL(self->dist_prefix_ == 0), &self->cmd_prefix_);
}

static BROTLI_INLINE void InitInsertCommand(Command* self, size_t insertlen) {
  self->insert_len_ = (uint32_t)insertlen;
  self->copy_len_ = 4 << 24;
  self->dist_extra_ = 0;
  self->dist_prefix_ = BROTLI_NUM_DISTANCE_SHORT_CODES;
  GetLengthCode(insertlen, 4, BROTLI_FALSE, &self->cmd_prefix_);
}

static BROTLI_INLINE uint32_t CommandRestoreDistanceCode(const Command* self) {
  if (self->dist_prefix_ < BROTLI_NUM_DISTANCE_SHORT_CODES) {
    return self->dist_prefix_;
  } else {
    uint32_t nbits = self->dist_extra_ >> 24;
    uint32_t extra = self->dist_extra_ & 0xffffff;
    /* It is assumed that the distance was first encoded with NPOSTFIX = 0 and
       NDIRECT = 0, so the code itself is of this form:
         BROTLI_NUM_DISTANCE_SHORT_CODES + 2 * (nbits - 1) + prefix_bit
       Therefore, the following expression results in (2 + prefix_bit). */
    uint32_t prefix =
        self->dist_prefix_ + 4u - BROTLI_NUM_DISTANCE_SHORT_CODES - 2u * nbits;
    /* Subtract 4 for offset (Chapter 4.) and
       increase by BROTLI_NUM_DISTANCE_SHORT_CODES - 1  */
    return (prefix << nbits) + extra + BROTLI_NUM_DISTANCE_SHORT_CODES - 4u;
  }
}

static BROTLI_INLINE uint32_t CommandDistanceContext(const Command* self) {
  uint32_t r = self->cmd_prefix_ >> 6;
  uint32_t c = self->cmd_prefix_ & 7;
  if ((r == 0 || r == 2 || r == 4 || r == 7) && (c <= 2)) {
    return c;
  }
  return 3;
}

static BROTLI_INLINE uint32_t CommandCopyLen(const Command* self) {
  return self->copy_len_ & 0xFFFFFF;
}

static BROTLI_INLINE uint32_t CommandCopyLenCode(const Command* self) {
  int32_t delta = (int8_t)((uint8_t)(self->copy_len_ >> 24));
  return (uint32_t)((int32_t)(self->copy_len_ & 0xFFFFFF) + delta);
}

#if defined(__cplusplus) || defined(c_plusplus)
}  /* extern "C" */
#endif

#endif  /* BROTLI_ENC_COMMAND_H_ */
