// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdlib.h>
#include "pal_locale_internal.h"
#include "pal_collation.h"

#import <Foundation/Foundation.h>

#if defined(TARGET_OSX) || defined(TARGET_MACCATALYST) || defined(TARGET_IOS) || defined(TARGET_TVOS)

// Enum that corresponds to C# CompareOptions
typedef enum
{
    None = 0,
    IgnoreCase = 1,
    IgnoreNonSpace = 2,
    IgnoreWidth = 16,
    StringSort = 536870912,
    
} CompareOptions;

static NSStringCompareOptions ConvertFromCompareOptionsToNSStringCompareOptions(int32_t comparisonOptions)
{
    switch(comparisonOptions)
    {
        case None:
        case StringSort:
            return NSLiteralSearch;
        case IgnoreCase:
            return NSCaseInsensitiveSearch;
        case IgnoreNonSpace:
            return NSDiacriticInsensitiveSearch;
        case (IgnoreNonSpace | IgnoreCase):
            return NSCaseInsensitiveSearch | NSDiacriticInsensitiveSearch;
        case IgnoreWidth:
            return NSWidthInsensitiveSearch;
        case (IgnoreWidth | IgnoreCase):
            return NSWidthInsensitiveSearch | NSCaseInsensitiveSearch;
        case (IgnoreWidth | IgnoreNonSpace):
            return NSWidthInsensitiveSearch | NSDiacriticInsensitiveSearch;
        case (IgnoreWidth | IgnoreNonSpace | IgnoreCase):
            return NSWidthInsensitiveSearch | NSDiacriticInsensitiveSearch | NSCaseInsensitiveSearch;
        default:
            return 0;
    }
}

#endif

/*
Function:
CompareString
*/
int32_t GlobalizationNative_CompareStringNative(const char* localeName, int32_t lNameLength, const char* lpStr1, int32_t cwStr1Length, 
                                                const char* lpStr2, int32_t cwStr2Length, int32_t comparisonOptions)
{    
    NSLocale *currentLocale;
    if(localeName == NULL || lNameLength == 0)
    {
        currentLocale = [NSLocale systemLocale];
    }
    else
    {
        NSString *locName = [NSString stringWithCharacters: (const unichar *)localeName length: lNameLength];
        currentLocale = [[NSLocale alloc] initWithLocaleIdentifier:locName];
    }

    NSString *firstString = [NSString stringWithCharacters: (const unichar *)lpStr1 length: cwStr1Length];
    NSString *secondString = [NSString stringWithCharacters: (const unichar *)lpStr2 length: cwStr2Length];
    NSRange string1Range = NSMakeRange(0, cwStr1Length);
    NSStringCompareOptions options = ConvertFromCompareOptionsToNSStringCompareOptions(comparisonOptions);
    
    // in case mapping is not found
    if(options == 0)
        return -2;
        
    return [firstString compare:secondString
                        options:options
                        range:string1Range
                        locale:currentLocale];
}
