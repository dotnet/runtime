/*
 * Intrinsics for libraries methods that are heavily used in interpreter relevant
 * scenarios and where compiling these methods with the interpreter would have
 * heavy performance impact.
 */

#include "interp-intrins.h"

#include <mono/metadata/object-internals.h>
#include <mono/metadata/gc-internals.h>

static guint32
rotate_left (guint32 value, int offset)
{
        return (value << offset) | (value >> (32 - offset));
}

void
interp_intrins_marvin_block (guint32 *pp0, guint32 *pp1)
{
	// Marvin.Block
	guint32 p0 = *pp0;
	guint32 p1 = *pp1;

	p1 ^= p0;
	p0 = rotate_left (p0, 20);

	p0 += p1;
	p1 = rotate_left (p1, 9);

	p1 ^= p0;
	p0 = rotate_left (p0, 27);

	p0 += p1;
	p1 = rotate_left (p1, 19);

	*pp0 = p0;
	*pp1 = p1;
}

guint32
interp_intrins_ascii_chars_to_uppercase (guint32 value)
{
	// Utf16Utility.ConvertAllAsciiCharsInUInt32ToUppercase
	guint32 lowerIndicator = value + 0x00800080 - 0x00610061;
	guint32 upperIndicator = value + 0x00800080 - 0x007B007B;
	guint32 combinedIndicator = (lowerIndicator ^ upperIndicator);
	guint32 mask = (combinedIndicator & 0x00800080) >> 2;

	return value ^ mask;
}

int
interp_intrins_ordinal_ignore_case_ascii (guint32 valueA, guint32 valueB)
{
	// Utf16Utility.UInt32OrdinalIgnoreCaseAscii
	guint32 differentBits = (valueA ^ valueB) << 2;
	guint32 indicator = valueA + 0x00050005;
	indicator |= 0x00A000A0;
	indicator += 0x001A001A;
	indicator |= 0xFF7FFF7F;
	return (differentBits & indicator) == 0;
}

int
interp_intrins_64ordinal_ignore_case_ascii (guint64 valueA, guint64 valueB)
{
	// Utf16Utility.UInt64OrdinalIgnoreCaseAscii
	guint64 differentBits = (valueA ^ valueB) << 2;
	guint64 indicator = valueA + 0x0005000500050005l;
	indicator |= 0x00A000A000A000A0l;
	indicator += 0x001A001A001A001Al;
	indicator |= 0xFF7FFF7FFF7FFF7Fl;
	return (differentBits & indicator) == 0;
}

static int
interp_intrins_count_digits (guint32 value)
{
	int digits = 1;
	if (value >= 100000) {
		value /= 100000;
		digits += 5;
	}
	if (value < 10) {
		// no-op
	} else if (value < 100) {
		digits++;
	} else if (value < 1000) {
		digits += 2;
	} else if (value < 10000) {
		digits += 3;
	} else {
		digits += 4;
	}
	return digits;
}

static guint32
interp_intrins_math_divrem (guint32 a, guint32 b, guint32 *result)
{
	guint32 div = a / b;
	*result = a - (div * b);
	return div;
}

MonoString*
interp_intrins_u32_to_decstr (guint32 value, MonoArray *cache, MonoVTable *vtable)
{
	// Number.UInt32ToDecStr
	int bufferLength = interp_intrins_count_digits (value);

	if (bufferLength == 1)
		return mono_array_get_fast (cache, MonoString*, value);

	int size = (G_STRUCT_OFFSET (MonoString, chars) + (((size_t)bufferLength + 1) * 2));
	MonoString* result = mono_gc_alloc_string (vtable, size, bufferLength);
	mono_unichar2 *buffer = &result->chars [0];
	mono_unichar2 *p = buffer + bufferLength;
	do {
		guint32 remainder;
		value = interp_intrins_math_divrem (value, 10, &remainder);
		*(--p) = (mono_unichar2)(remainder + '0');
	} while (value != 0);
	return result;
}

mono_u
interp_intrins_widen_ascii_to_utf16 (guint8 *pAsciiBuffer, mono_unichar2 *pUtf16Buffer, mono_u elementCount)
{
	// ASCIIUtility.WidenAsciiToUtf16
	mono_u currentOffset = 0;

	while (currentOffset < elementCount) {
		guint16 asciiData = pAsciiBuffer [currentOffset];
		if ((asciiData & 0x80) != 0)
			return currentOffset;

		pUtf16Buffer [currentOffset] = (mono_unichar2)asciiData;
		currentOffset++;
	}
	return currentOffset;
}
