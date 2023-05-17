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
    
} CompareOptions;

static NSStringCompareOptions ConvertFromCompareOptionsToNSStringCompareOptions(int32_t comparisonOptions)
{
    NSStringCompareOptions options = 0;
    if (comparisonOptions == None)
       options = NSLiteralSearch;

    else if (comparisonOptions == IgnoreCase)
        options = NSCaseInsensitiveSearch;

    else if (comparisonOptions == IgnoreNonSpace)
        options = NSDiacriticInsensitiveSearch;

    else if (comparisonOptions == (IgnoreNonSpace | IgnoreCase))
        options = NSCaseInsensitiveSearch | NSDiacriticInsensitiveSearch;

    else if (comparisonOptions == IgnoreWidth)
        options = NSWidthInsensitiveSearch;

    else if (comparisonOptions == (IgnoreWidth | IgnoreCase))
        options = NSWidthInsensitiveSearch | NSCaseInsensitiveSearch;

    else if (comparisonOptions == (IgnoreWidth | IgnoreNonSpace))
        options = NSWidthInsensitiveSearch | NSDiacriticInsensitiveSearch;

    else if (comparisonOptions == (IgnoreWidth | IgnoreNonSpace | IgnoreCase))
        options = NSWidthInsensitiveSearch | NSDiacriticInsensitiveSearch | NSCaseInsensitiveSearch;

    return options;
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
