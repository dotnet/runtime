// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************/
/*                         OutString.cpp                         */
/*****************************************************************/
/* A simple, lightweight, character output stream, with very few
   external dependancies (like sprintf ... ) */

/*
   Date :  2/1/99 				*/
/*****************************************************************/

#include "stdafx.h"
#include "outstring.h"


/*****************************************************************/
//  print out 'count' instances of the character 'c'
OutString& OutString::pad(size_t count, char c) {
	if (cur+count > end)
		Realloc(count);
	memset(cur, c, count);
	cur = cur + count;
	return(*this);
}

/*****************************************************************/
// prints out a decimal representation
OutString& OutString::operator<<(double d) {

	if (d == 0.0) {
		*this << "0.0";
		return *this;
	}

	if (d < 0) {
		d = -d;
		*this << '-';
	}

		// compute the exponent
	int exponent = 0;
	while (d > 10.0)  {
		d /= 10;
		exponent++;
		if (exponent > 500) {		// avoids a possible infinite loop
            *this << "INF";
			return *this;
		}
	}
	while (d < 1.0)  {
		d *= 10;
		--exponent;
		if (exponent < -500) {		// avoids a possible infinite loop
			*this << "0.0";
			return *this;
		}
	}

	// we now have a normalized d (between 1 and 10)
	double delta = .5E-10;
	d += delta;		// round to the precision we are displaying

	unsigned trailingZeros = 0;
	for(unsigned i = 0; i < 10; i++) {
		int digit = (int) d;
		d = (d - digit) * 10;		// ISSUE: does roundoff ever bite us here?

		if (digit == 0)		// defer printing traiing zeros
			trailingZeros++;
		else {
			if (trailingZeros > 0) {
				this->pad(trailingZeros, '0');
				trailingZeros = 0;
			}
		*this << (char) ('0' + digit);
		}
		if (i == 0)
			*this << '.';

	}
	if (exponent != 0) {
		*this << 'E';
		*this << exponent;
	}
	return(*this);
}

/*****************************************************************/
// prints out a decimal representation
OutString& OutString::dec(int i, size_t minWidth) {
	char buff[12];			// big enough for any number (10 digits, - sign, null term)
	char* ptr = &buff[11];
	*ptr = 0;

	unsigned val = i;
	if (i < 0)
		val = -i;	// note this happens to also work for minint!

	for(;;) {
		if (val < 10) {
			*--ptr = (char)('0' + val);
			break;
			}
		*--ptr = (char)('0' + (val % 10));
		val = val / 10;
		}

	if (i < 0)
		*--ptr = '-';

	size_t len = &buff[11] - ptr; 	// length of string
	if (len < minWidth)
		pad(minWidth-len, ' ');

	*this << ptr;
	return(*this);
}

/*****************************************************************/
OutString& OutString::hex(unsigned __int64 i, int minWidth, unsigned flags) {

	unsigned hi = unsigned(i >> 32);
	unsigned low = unsigned(i);

	if (hi != 0) {
		minWidth -= 8;
		hex(hi, minWidth, flags);		// print upper bits
		flags = zeroFill;
		minWidth = 8;
	}
	return hex(low, minWidth, flags);	// print lower bits
}

/*****************************************************************/
OutString& OutString::hex(unsigned i, int minWidth, unsigned flags) {
	char buff[12];			// big enough for any number
	char* ptr = &buff[11];
	*ptr = 0;

    static const char digits[] = "0123456789ABCDEF";

	for(;;) {
		if (i < 16) {
			*--ptr = digits[i];
			break;
			}
		*--ptr = digits[(i % 16)];
		i = i / 16;
		}

	size_t len = &buff[11] - ptr; 			// length of string
	if (flags & put0x) {
        if (flags & zeroFill)
		    *this << "0x";
        else
            *--ptr = 'x', *--ptr = '0';
		len += 2;
		}

	if (len < (size_t)minWidth)
		pad(minWidth-len, (flags & zeroFill) ? '0' : ' ');

	*this << ptr;
	return(*this);
}

/*****************************************************************/
void OutString::Realloc(size_t neededSpace)  {
    size_t oldSize = cur-start;
	size_t newSize = (oldSize + neededSpace) * 3 / 2 + 32;
	char* oldBuff = start;
	start = new char[newSize+1];
	memcpy(start, oldBuff, oldSize);
	cur = &start[oldSize];
	end = &start[newSize];
	delete [] oldBuff;
}

