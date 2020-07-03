/*
 * Intrinsics for libraries methods that are heavily used in interpreter relevant
 * scenarios and where compiling these methods with the interpreter would have
 * heavy performance impact.
 */

#include "intrinsics.h"

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
	guint32 differentBits = valueA ^ valueB;
	guint32 lowerIndicator = valueA + 0x01000100 - 0x00410041;
	guint32 upperIndicator = (valueA | 0x00200020u) + 0x00800080 - 0x007B007B;
	guint32 combinedIndicator = lowerIndicator | upperIndicator;
	return (((combinedIndicator >> 2) | ~0x00200020) & differentBits) == 0;
}

int
interp_intrins_64ordinal_ignore_case_ascii (guint64 valueA, guint64 valueB)
{
	// Utf16Utility.UInt64OrdinalIgnoreCaseAscii
	guint64 lowerIndicator = valueA + 0x0080008000800080l - 0x0041004100410041l;
	guint64 upperIndicator = (valueA | 0x0020002000200020l) + 0x0100010001000100l - 0x007B007B007B007Bl;
	guint64 combinedIndicator = (0x0080008000800080l & lowerIndicator & upperIndicator) >> 2;
	return (valueA | combinedIndicator) == (valueB | combinedIndicator);
}
