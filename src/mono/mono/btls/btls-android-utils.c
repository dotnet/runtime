// Copied from Chromium: https://src.chromium.org/svn/trunk/src/base/os_compat_android.cc

// Copyright (c) 2012 The Chromium Authors. All rights reserved.
// Use of this source code is governed by a BSD-style license that can be
// found in the LICENSE file.

#if defined(__ANDROID__)

#include <asm/unistd.h>
#include <errno.h>
#include <math.h>
#include <sys/stat.h>
#include <sys/syscall.h>

#if !defined(__LP64__)
#include <time64.h>
#endif

#if !defined(__LP64__)
// 32-bit Android has only timegm64() and not timegm().
// We replicate the behaviour of timegm() when the result overflows time_t.
time_t timegm(struct tm* const t) {
  // time_t is signed on Android.
  static const time_t kTimeMax = ~(1L << (sizeof(time_t) * CHAR_BIT - 1));
  static const time_t kTimeMin = (1L << (sizeof(time_t) * CHAR_BIT - 1));
  time64_t result = timegm64(t);
  if (result < kTimeMin || result > kTimeMax)
    return -1;
  return result;
}
#endif

#endif
