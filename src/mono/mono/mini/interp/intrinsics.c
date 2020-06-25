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

