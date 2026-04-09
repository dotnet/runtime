/**
 * \file
 */

#include "mono/utils/mono-bitutils.h"

#ifdef _MSC_VER
#include <intrin.h>
#endif

#define LOW32(x) ((guint32)(x & 0xffffffff))
#define HIGH32(x) ((guint32)((x >> 32) & 0xffffffff))

int mono_lzcnt32 (guint32 x)
{
#ifdef _MSC_VER
	unsigned long index = 0;
	if ( _BitScanReverse( &index, x ) )
		return 31 - (int)index;
	else
		return 32;
#else
	return __builtin_clz(x);
#endif
}

int mono_lzcnt64 (guint64 x)
{
#if defined(_MSC_VER) && (defined(_M_ARM64) || defined(_M_ARM64EC) || defined (_M_X64))
	unsigned long index = 0;
	if ( _BitScanReverse64( &index, x ) )
		return 63 - (int)index;
	else
		return 64;
#elif defined(_MSC_VER)
  guint32 high = HIGH32 (x);
  if (x == 0)
    return 64;
  else if (high == 0)
    return mono_lzcnt32 (LOW32 (x)) + 32;
  else
    return mono_lzcnt32 (high);
#else
	return __builtin_clzll(x);
#endif
}

int mono_tzcnt32 (guint32 x)
{
#ifdef _MSC_VER
	unsigned long index = 0;
	if ( _BitScanForward( &index, x ) )
		return (int)index;
	else
		return 32;
#else
	return __builtin_ctz(x);
#endif
}

int mono_tzcnt64 (guint64 x)
{
#if defined(_MSC_VER) && (defined(_M_ARM64) || defined(_M_ARM64EC) || defined (_M_X64))
	unsigned long index = 0;
	if ( _BitScanForward64( &index, x ) )
		return (int)index;
	else
		return 64;
#elif defined(_MSC_VER)
  guint32 low = LOW32 (x);
  if (x == 0)
    return 64;
  else if (low == 0)
    return mono_tzcnt32 (HIGH32 (x)) + 32;
  else
    return mono_tzcnt32 (low);
#else
	return __builtin_ctzll(x);
#endif
}
