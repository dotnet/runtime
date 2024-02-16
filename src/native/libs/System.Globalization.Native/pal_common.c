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
