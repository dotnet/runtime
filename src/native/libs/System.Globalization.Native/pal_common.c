#include <stdint.h>
#include "pal_icushim_internal.h"
#include "pal_casing.h"

void GlobalizationNative_InitOrdinalCasingPage(int32_t pageNumber, UChar* pTarget)
{
    pageNumber <<= 8;
    for (int i = 0; i < 256; i++)
    {
        // Unfortunately, to ensure one-to-one simple mapping we have to call u_toupper on every character.
        // Using string casing ICU APIs cannot give such results even when using NULL locale to force root behavior.
        pTarget[i] = (UChar) u_toupper((UChar32)(pageNumber + i));
    }

    if (pageNumber == 0x0100)
    {
        // Disable Turkish I behavior on Ordinal operations
        pTarget[0x31] = (UChar)0x0131;  // Turkish lowercase i
        pTarget[0x7F] = (UChar)0x017F;  // // 017F;LATIN SMALL LETTER LONG S
    }
}

void GlobalizationNative_InitOrdinalLowerCasingPage(int32_t pageNumber, UChar* pTarget)
{
    pageNumber <<= 8;
    for (int i = 0; i < 256; i++)
    {
        // Unfortunately, to ensure one-to-one simple mapping we have to call u_tolower on every character.
        // Using string casing ICU APIs cannot give such results even when using NULL locale to force root behavior.
        pTarget[i] = (UChar) u_tolower((UChar32)(pageNumber + i));
    }

    // Keep a few characters unchanged because their simple lower casing would move them out of their
    // ordinal (simple) upper-casing equivalence class. Ordinal lower casing must stay consistent with
    // OrdinalIgnoreCase comparisons, i.e. a character has to remain OrdinalIgnoreCase-equal to its
    // lower-cased form, exactly like the upper-casing page keeps Turkish 'i' and long 's' unchanged.
    // The affected characters are handled per originating page, mirroring the upper-casing page above.
    switch (pageNumber)
    {
        case 0x0100:
            pTarget[0x30] = (UChar)0x0130;  // LATIN CAPITAL LETTER I WITH DOT ABOVE
            break;
        case 0x0300:
            pTarget[0xF4] = (UChar)0x03F4;  // GREEK CAPITAL THETA SYMBOL
            break;
        case 0x1E00:
            pTarget[0x9E] = (UChar)0x1E9E;  // LATIN CAPITAL LETTER SHARP S
            break;
        case 0x2100:
            pTarget[0x26] = (UChar)0x2126;  // OHM SIGN
            pTarget[0x2A] = (UChar)0x212A;  // KELVIN SIGN
            pTarget[0x2B] = (UChar)0x212B;  // ANGSTROM SIGN
            break;
    }
}
