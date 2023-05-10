// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdlib.h>
#include "pal_locale_internal.h"
#include "pal_collation.h"

#import <Foundation/Foundation.h>

#if defined(TARGET_OSX) || defined(TARGET_MACCATALYST) || defined(TARGET_IOS) || defined(TARGET_TVOS)

/*
Function:
CompareString
*/
int32_t GlobalizationNative_CompareStringNative(const char* localeName, const char* lpStr1, int32_t cwStr1Length, 
                                                const char* lpStr2, int32_t cwStr2Length, int32_t comparisonOptions)
{    
    NSLocale *currentLocale;
    if(localeName == NULL || strlen(localeName) == 0)
    {
        currentLocale = [NSLocale systemLocale];
    }
    else
    {
        NSString *locName = [NSString stringWithFormat:@"%s", localeName];
        currentLocale = [[NSLocale alloc] initWithLocaleIdentifier:locName];
    }

    NSString *firstString = [NSString stringWithCharacters: (const unichar *)lpStr1 length: cwStr1Length];
    NSString *secondString = [NSString stringWithCharacters: (const unichar *)lpStr2 length: cwStr2Length];
    NSRange string1Range = NSMakeRange(0, cwStr1Length);
    NSStringCompareOptions options = 0;   
    switch (comparisonOptions)
    {
        case 0:
            // 0: None
            options = NSLiteralSearch;
            break;
        case 1:
            // 1: IgnoreCase
            options = NSCaseInsensitiveSearch;
            break;
        case 2:
            // 2: IgnoreNonSpace
            options = NSDiacriticInsensitiveSearch;
            break;
        case 3:        
            // 3: IgnoreNonSpace | IgnoreCase
            options = NSCaseInsensitiveSearch | NSDiacriticInsensitiveSearch;
            break;
        case 16:
            // 16: IgnoreWidth
            options = NSWidthInsensitiveSearch;
            break;
        case 17:
            // 17: IgnoreWidth | IgnoreCase
            options = NSWidthInsensitiveSearch | NSCaseInsensitiveSearch;
            break;
        case 18:
            // 18: IgnoreWidth | IgnoreNonSpace
            options = NSWidthInsensitiveSearch | NSDiacriticInsensitiveSearch;
            break;
        case 19:
            // 19: IgnoreWidth | IgnoreNonSpace | IgnoreCase
            options = NSWidthInsensitiveSearch | NSDiacriticInsensitiveSearch | NSCaseInsensitiveSearch;
            break;
        case 4:
        case 5:
        case 6:
        case 7:
        case 8:
        case 9:
        case 10:
        case 11:
        case 12:
        case 13:
        case 14:
        case 15:
        case 20:
        case 21:
        case 22:
        case 23:
        case 24:
        case 25:
        case 26:
        case 27:
        case 28:
        case 29:
        case 30:
        case 31:
        default:
            // 4: IgnoreSymbols
            // 5: IgnoreSymbols | IgnoreCase
            // 6: IgnoreSymbols | IgnoreNonSpace
            // 7: IgnoreSymbols | IgnoreNonSpace | IgnoreCase
            // 8: IgnoreKanaType
            // 9: IgnoreKanaType | IgnoreCase
            // 10: IgnoreKanaType | IgnoreNonSpace
            // 11: IgnoreKanaType | IgnoreNonSpace | IgnoreCase
            // 12: IgnoreKanaType | IgnoreSymbols
            // 13: IgnoreKanaType | IgnoreCase | IgnoreSymbols
            // 14: IgnoreKanaType | IgnoreSymbols | IgnoreNonSpace
            // 15: IgnoreKanaType | IgnoreSymbols | IgnoreNonSpace | IgnoreCase            
            // 20: IgnoreWidth | IgnoreSymbols
            // 21: IgnoreWidth | IgnoreSymbols | IgnoreCase
            // 22: IgnoreWidth | IgnoreSymbols | IgnoreNonSpace
            // 23: IgnoreWidth | IgnoreSymbols | IgnoreNonSpace | IgnoreCase
            // 24: IgnoreKanaType | IgnoreWidth
            // 25: IgnoreKanaType | IgnoreWidth | IgnoreCase
            // 26: IgnoreKanaType | IgnoreWidth | IgnoreNonSpace
            // 27: IgnoreKanaType | IgnoreWidth | IgnoreNonSpace | IgnoreCase
            // 28: IgnoreKanaType | IgnoreWidth | IgnoreSymbols
            // 29: IgnoreKanaType | IgnoreWidth | IgnoreSymbols | IgnoreCase
            // 30: IgnoreKanaType | IgnoreWidth | IgnoreSymbols | IgnoreNonSpace
            // 31: IgnoreKanaType | IgnoreWidth | IgnoreSymbols | IgnoreNonSpace | IgnoreCase
            return -2;
    }
        
    return [firstString compare:secondString
                        options:options
                        range:string1Range
                        locale:currentLocale];
}

#endif

