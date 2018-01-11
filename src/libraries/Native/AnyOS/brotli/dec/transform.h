/* Copyright 2013 Google Inc. All Rights Reserved.

   Distributed under MIT license.
   See file LICENSE for detail or copy at https://opensource.org/licenses/MIT
*/

/* Transformations on dictionary words. */

#ifndef BROTLI_DEC_TRANSFORM_H_
#define BROTLI_DEC_TRANSFORM_H_

#include <brotli/types.h>
#include "./port.h"

#if defined(__cplusplus) || defined(c_plusplus)
extern "C" {
#endif

enum WordTransformType {
  kIdentity = 0,
  kOmitLast1 = 1,
  kOmitLast2 = 2,
  kOmitLast3 = 3,
  kOmitLast4 = 4,
  kOmitLast5 = 5,
  kOmitLast6 = 6,
  kOmitLast7 = 7,
  kOmitLast8 = 8,
  kOmitLast9 = 9,
  kUppercaseFirst = 10,
  kUppercaseAll = 11,
  kOmitFirst1 = 12,
  kOmitFirst2 = 13,
  kOmitFirst3 = 14,
  kOmitFirst4 = 15,
  kOmitFirst5 = 16,
  kOmitFirst6 = 17,
  kOmitFirst7 = 18,
  kOmitFirst8 = 19,
  kOmitFirst9 = 20
};

typedef struct {
  const uint8_t prefix_id;
  const uint8_t transform;
  const uint8_t suffix_id;
} Transform;

static const char kPrefixSuffix[208] =
    "\0 \0, \0 of the \0 of \0s \0.\0 and \0 in \0\"\0 to \0\">\0\n\0. \0]\0"
    " for \0 a \0 that \0\'\0 with \0 from \0 by \0(\0. The \0 on \0 as \0"
    " is \0ing \0\n\t\0:\0ed \0=\"\0 at \0ly \0,\0=\'\0.com/\0. This \0"
    " not \0er \0al \0ful \0ive \0less \0est \0ize \0\xc2\xa0\0ous ";

enum {
  /* EMPTY = ""
     SP = " "
     DQUOT = "\""
     SQUOT = "'"
     CLOSEBR = "]"
     OPEN = "("
     SLASH = "/"
     NBSP = non-breaking space "\0xc2\xa0"
  */
  kPFix_EMPTY = 0,
  kPFix_SP = 1,
  kPFix_COMMASP = 3,
  kPFix_SPofSPtheSP = 6,
  kPFix_SPtheSP = 9,
  kPFix_eSP = 12,
  kPFix_SPofSP = 15,
  kPFix_sSP = 20,
  kPFix_DOT = 23,
  kPFix_SPandSP = 25,
  kPFix_SPinSP = 31,
  kPFix_DQUOT = 36,
  kPFix_SPtoSP = 38,
  kPFix_DQUOTGT = 43,
  kPFix_NEWLINE = 46,
  kPFix_DOTSP = 48,
  kPFix_CLOSEBR = 51,
  kPFix_SPforSP = 53,
  kPFix_SPaSP = 59,
  kPFix_SPthatSP = 63,
  kPFix_SQUOT = 70,
  kPFix_SPwithSP = 72,
  kPFix_SPfromSP = 79,
  kPFix_SPbySP = 86,
  kPFix_OPEN = 91,
  kPFix_DOTSPTheSP = 93,
  kPFix_SPonSP = 100,
  kPFix_SPasSP = 105,
  kPFix_SPisSP = 110,
  kPFix_ingSP = 115,
  kPFix_NEWLINETAB = 120,
  kPFix_COLON = 123,
  kPFix_edSP = 125,
  kPFix_EQDQUOT = 129,
  kPFix_SPatSP = 132,
  kPFix_lySP = 137,
  kPFix_COMMA = 141,
  kPFix_EQSQUOT = 143,
  kPFix_DOTcomSLASH = 146,
  kPFix_DOTSPThisSP = 152,
  kPFix_SPnotSP = 160,
  kPFix_erSP = 166,
  kPFix_alSP = 170,
  kPFix_fulSP = 174,
  kPFix_iveSP = 179,
  kPFix_lessSP = 184,
  kPFix_estSP = 190,
  kPFix_izeSP = 195,
  kPFix_NBSP = 200,
  kPFix_ousSP = 203
};

static const Transform kTransforms[] = {
  { kPFix_EMPTY, kIdentity, kPFix_EMPTY },
  { kPFix_EMPTY, kIdentity, kPFix_SP },
  { kPFix_SP, kIdentity, kPFix_SP },
  { kPFix_EMPTY, kOmitFirst1, kPFix_EMPTY },
  { kPFix_EMPTY, kUppercaseFirst, kPFix_SP },
  { kPFix_EMPTY, kIdentity, kPFix_SPtheSP },
  { kPFix_SP, kIdentity, kPFix_EMPTY },
  { kPFix_sSP, kIdentity, kPFix_SP },
  { kPFix_EMPTY, kIdentity, kPFix_SPofSP },
  { kPFix_EMPTY, kUppercaseFirst, kPFix_EMPTY },
  { kPFix_EMPTY, kIdentity, kPFix_SPandSP },
  { kPFix_EMPTY, kOmitFirst2, kPFix_EMPTY },
  { kPFix_EMPTY, kOmitLast1, kPFix_EMPTY },
  { kPFix_COMMASP, kIdentity, kPFix_SP },
  { kPFix_EMPTY, kIdentity, kPFix_COMMASP },
  { kPFix_SP, kUppercaseFirst, kPFix_SP },
  { kPFix_EMPTY, kIdentity, kPFix_SPinSP },
  { kPFix_EMPTY, kIdentity, kPFix_SPtoSP },
  { kPFix_eSP, kIdentity, kPFix_SP },
  { kPFix_EMPTY, kIdentity, kPFix_DQUOT },
  { kPFix_EMPTY, kIdentity, kPFix_DOT },
  { kPFix_EMPTY, kIdentity, kPFix_DQUOTGT },
  { kPFix_EMPTY, kIdentity, kPFix_NEWLINE },
  { kPFix_EMPTY, kOmitLast3, kPFix_EMPTY },
  { kPFix_EMPTY, kIdentity, kPFix_CLOSEBR },
  { kPFix_EMPTY, kIdentity, kPFix_SPforSP },
  { kPFix_EMPTY, kOmitFirst3, kPFix_EMPTY },
  { kPFix_EMPTY, kOmitLast2, kPFix_EMPTY },
  { kPFix_EMPTY, kIdentity, kPFix_SPaSP },
  { kPFix_EMPTY, kIdentity, kPFix_SPthatSP },
  { kPFix_SP, kUppercaseFirst, kPFix_EMPTY },
  { kPFix_EMPTY, kIdentity, kPFix_DOTSP },
  { kPFix_DOT, kIdentity, kPFix_EMPTY },
  { kPFix_SP, kIdentity, kPFix_COMMASP },
  { kPFix_EMPTY, kOmitFirst4, kPFix_EMPTY },
  { kPFix_EMPTY, kIdentity, kPFix_SPwithSP },
  { kPFix_EMPTY, kIdentity, kPFix_SQUOT },
  { kPFix_EMPTY, kIdentity, kPFix_SPfromSP },
  { kPFix_EMPTY, kIdentity, kPFix_SPbySP },
  { kPFix_EMPTY, kOmitFirst5, kPFix_EMPTY },
  { kPFix_EMPTY, kOmitFirst6, kPFix_EMPTY },
  { kPFix_SPtheSP, kIdentity, kPFix_EMPTY },
  { kPFix_EMPTY, kOmitLast4, kPFix_EMPTY },
  { kPFix_EMPTY, kIdentity, kPFix_DOTSPTheSP },
  { kPFix_EMPTY, kUppercaseAll, kPFix_EMPTY },
  { kPFix_EMPTY, kIdentity, kPFix_SPonSP },
  { kPFix_EMPTY, kIdentity, kPFix_SPasSP },
  { kPFix_EMPTY, kIdentity, kPFix_SPisSP },
  { kPFix_EMPTY, kOmitLast7, kPFix_EMPTY },
  { kPFix_EMPTY, kOmitLast1, kPFix_ingSP },
  { kPFix_EMPTY, kIdentity, kPFix_NEWLINETAB },
  { kPFix_EMPTY, kIdentity, kPFix_COLON },
  { kPFix_SP, kIdentity, kPFix_DOTSP },
  { kPFix_EMPTY, kIdentity, kPFix_edSP },
  { kPFix_EMPTY, kOmitFirst9, kPFix_EMPTY },
  { kPFix_EMPTY, kOmitFirst7, kPFix_EMPTY },
  { kPFix_EMPTY, kOmitLast6, kPFix_EMPTY },
  { kPFix_EMPTY, kIdentity, kPFix_OPEN },
  { kPFix_EMPTY, kUppercaseFirst, kPFix_COMMASP },
  { kPFix_EMPTY, kOmitLast8, kPFix_EMPTY },
  { kPFix_EMPTY, kIdentity, kPFix_SPatSP },
  { kPFix_EMPTY, kIdentity, kPFix_lySP },
  { kPFix_SPtheSP, kIdentity, kPFix_SPofSP },
  { kPFix_EMPTY, kOmitLast5, kPFix_EMPTY },
  { kPFix_EMPTY, kOmitLast9, kPFix_EMPTY },
  { kPFix_SP, kUppercaseFirst, kPFix_COMMASP },
  { kPFix_EMPTY, kUppercaseFirst, kPFix_DQUOT },
  { kPFix_DOT, kIdentity, kPFix_OPEN },
  { kPFix_EMPTY, kUppercaseAll, kPFix_SP },
  { kPFix_EMPTY, kUppercaseFirst, kPFix_DQUOTGT },
  { kPFix_EMPTY, kIdentity, kPFix_EQDQUOT },
  { kPFix_SP, kIdentity, kPFix_DOT },
  { kPFix_DOTcomSLASH, kIdentity, kPFix_EMPTY },
  { kPFix_SPtheSP, kIdentity, kPFix_SPofSPtheSP },
  { kPFix_EMPTY, kUppercaseFirst, kPFix_SQUOT },
  { kPFix_EMPTY, kIdentity, kPFix_DOTSPThisSP },
  { kPFix_EMPTY, kIdentity, kPFix_COMMA },
  { kPFix_DOT, kIdentity, kPFix_SP },
  { kPFix_EMPTY, kUppercaseFirst, kPFix_OPEN },
  { kPFix_EMPTY, kUppercaseFirst, kPFix_DOT },
  { kPFix_EMPTY, kIdentity, kPFix_SPnotSP },
  { kPFix_SP, kIdentity, kPFix_EQDQUOT },
  { kPFix_EMPTY, kIdentity, kPFix_erSP },
  { kPFix_SP, kUppercaseAll, kPFix_SP },
  { kPFix_EMPTY, kIdentity, kPFix_alSP },
  { kPFix_SP, kUppercaseAll, kPFix_EMPTY },
  { kPFix_EMPTY, kIdentity, kPFix_EQSQUOT },
  { kPFix_EMPTY, kUppercaseAll, kPFix_DQUOT },
  { kPFix_EMPTY, kUppercaseFirst, kPFix_DOTSP },
  { kPFix_SP, kIdentity, kPFix_OPEN },
  { kPFix_EMPTY, kIdentity, kPFix_fulSP },
  { kPFix_SP, kUppercaseFirst, kPFix_DOTSP },
  { kPFix_EMPTY, kIdentity, kPFix_iveSP },
  { kPFix_EMPTY, kIdentity, kPFix_lessSP },
  { kPFix_EMPTY, kUppercaseAll, kPFix_SQUOT },
  { kPFix_EMPTY, kIdentity, kPFix_estSP },
  { kPFix_SP, kUppercaseFirst, kPFix_DOT },
  { kPFix_EMPTY, kUppercaseAll, kPFix_DQUOTGT },
  { kPFix_SP, kIdentity, kPFix_EQSQUOT },
  { kPFix_EMPTY, kUppercaseFirst, kPFix_COMMA },
  { kPFix_EMPTY, kIdentity, kPFix_izeSP },
  { kPFix_EMPTY, kUppercaseAll, kPFix_DOT },
  { kPFix_NBSP, kIdentity, kPFix_EMPTY },
  { kPFix_SP, kIdentity, kPFix_COMMA },
  { kPFix_EMPTY, kUppercaseFirst, kPFix_EQDQUOT },
  { kPFix_EMPTY, kUppercaseAll, kPFix_EQDQUOT },
  { kPFix_EMPTY, kIdentity, kPFix_ousSP },
  { kPFix_EMPTY, kUppercaseAll, kPFix_COMMASP },
  { kPFix_EMPTY, kUppercaseFirst, kPFix_EQSQUOT },
  { kPFix_SP, kUppercaseFirst, kPFix_COMMA },
  { kPFix_SP, kUppercaseAll, kPFix_EQDQUOT },
  { kPFix_SP, kUppercaseAll, kPFix_COMMASP },
  { kPFix_EMPTY, kUppercaseAll, kPFix_COMMA },
  { kPFix_EMPTY, kUppercaseAll, kPFix_OPEN },
  { kPFix_EMPTY, kUppercaseAll, kPFix_DOTSP },
  { kPFix_SP, kUppercaseAll, kPFix_DOT },
  { kPFix_EMPTY, kUppercaseAll, kPFix_EQSQUOT },
  { kPFix_SP, kUppercaseAll, kPFix_DOTSP },
  { kPFix_SP, kUppercaseFirst, kPFix_EQDQUOT },
  { kPFix_SP, kUppercaseAll, kPFix_EQSQUOT },
  { kPFix_SP, kUppercaseFirst, kPFix_EQSQUOT },
};

static const int kNumTransforms = sizeof(kTransforms) / sizeof(kTransforms[0]);

static int ToUpperCase(uint8_t* p) {
  if (p[0] < 0xc0) {
    if (p[0] >= 'a' && p[0] <= 'z') {
      p[0] ^= 32;
    }
    return 1;
  }
  /* An overly simplified uppercasing model for UTF-8. */
  if (p[0] < 0xe0) {
    p[1] ^= 32;
    return 2;
  }
  /* An arbitrary transform for three byte characters. */
  p[2] ^= 5;
  return 3;
}

static BROTLI_NOINLINE int TransformDictionaryWord(
    uint8_t* dst, const uint8_t* word, int len, int transform) {
  int idx = 0;
  {
    const char* prefix = &kPrefixSuffix[kTransforms[transform].prefix_id];
    while (*prefix) { dst[idx++] = (uint8_t)*prefix++; }
  }
  {
    const int t = kTransforms[transform].transform;
    int i = 0;
    int skip = t - (kOmitFirst1 - 1);
    if (skip > 0) {
      word += skip;
      len -= skip;
    } else if (t <= kOmitLast9) {
      len -= t;
    }
    while (i < len) { dst[idx++] = word[i++]; }
    if (t == kUppercaseFirst) {
      ToUpperCase(&dst[idx - len]);
    } else if (t == kUppercaseAll) {
      uint8_t* uppercase = &dst[idx - len];
      while (len > 0) {
        int step = ToUpperCase(uppercase);
        uppercase += step;
        len -= step;
      }
    }
  }
  {
    const char* suffix = &kPrefixSuffix[kTransforms[transform].suffix_id];
    while (*suffix) { dst[idx++] = (uint8_t)*suffix++; }
    return idx;
  }
}

#if defined(__cplusplus) || defined(c_plusplus)
}  /* extern "C" */
#endif

#endif  /* BROTLI_DEC_TRANSFORM_H_ */
