/**
 * \file
 */

#include "mono/utils/mono-bitutils.h"

#ifdef _MSC_VER
#include <intrin.h>
#endif

int mono_lzcnt32 (guint32 x)
{
#ifdef _MSC_VER
	unsigned long index = 0;
	if ( _BitScanReverse( &index, x ) )
		return (int)index;
	else
		return 32;
#else
	return __builtin_clz(x);
#endif
}

int mono_lzcnt64 (guint64 x)
{
#ifdef _MSC_VER
	unsigned long index = 0;
	if ( _BitScanReverse64( &index, x ) )
		return (int)index;
	else
		return 64;
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
#ifdef _MSC_VER
	unsigned long index = 0;
	if ( _BitScanForward64( &index, x ) )
		return (int)index;
	else
		return 64;
#else
	return __builtin_ctzll(x);
#endif
}
