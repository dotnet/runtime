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
    int32_t supportedOptions = None | IgnoreCase | IgnoreNonSpace | IgnoreWidth | StringSort;
    // To achieve an equivalent search behavior to the default in ICU,
    // NSLiteralSearch is employed as the default search option.
    NSStringCompareOptions options = NSLiteralSearch;

    if ((comparisonOptions | supportedOptions) != supportedOptions)
        return 0;

    if (comparisonOptions & IgnoreCase)
        options |= NSCaseInsensitiveSearch;

    if (comparisonOptions & IgnoreNonSpace)
        options |= NSDiacriticInsensitiveSearch;

    if (comparisonOptions & IgnoreWidth)
        options |= NSWidthInsensitiveSearch;

    return options;
}

/*
Function:
CompareString
*/
int32_t GlobalizationNative_CompareStringNative(const uint16_t* localeName, int32_t lNameLength, const uint16_t* lpStr1, int32_t cwStr1Length, 
                                                const uint16_t* lpStr2, int32_t cwStr2Length, int32_t comparisonOptions)
{    
    NSLocale *currentLocale;
    if(localeName == NULL || lNameLength == 0)
    {
        currentLocale = [NSLocale systemLocale];
    }
    else
    {
        NSString *locName = [NSString stringWithCharacters: localeName length: lNameLength];
        currentLocale = [[NSLocale alloc] initWithLocaleIdentifier:locName];
    }

    NSString *firstString = [NSString stringWithCharacters: lpStr1 length: cwStr1Length];
    NSString *secondString = [NSString stringWithCharacters: lpStr2 length: cwStr2Length];
    NSRange string1Range = NSMakeRange(0, cwStr1Length);
    NSStringCompareOptions options = ConvertFromCompareOptionsToNSStringCompareOptions(comparisonOptions);
    
    // in case mapping is not found
    if (options == 0)
        return -2;
        
    return [firstString compare:secondString
                        options:options
                        range:string1Range
                        locale:currentLocale];
}

/*
Function:
IndexOf
*/
Range GlobalizationNative_IndexOfNative(const uint16_t* localeName,
                                        int32_t lNameLength,
                                        const uint16_t* lpTarget,
                                        int32_t cwTargetLength,
                                        const uint16_t* lpSource,
                                        int32_t cwSourceLength,
                                        int32_t comparisonOptions,
                                        int32_t fromBeginning)
{
    assert(cwTargetLength > 0);
    Range result = {-2, 0};

    NSLocale *currentLocale;
    if(localeName == NULL || lNameLength == 0)
    {
        currentLocale = [NSLocale systemLocale];
    }
    else
    {
        NSString *locName = [NSString stringWithCharacters: localeName length: lNameLength];
        currentLocale = [[NSLocale alloc] initWithLocaleIdentifier:locName];
    }

    NSString *searchString = [NSString stringWithCharacters: lpTarget length: cwTargetLength];
    NSString *sourceString = [NSString stringWithCharacters: lpSource length: cwSourceLength];
    if ((int32_t)sourceString.length < 0)
       return result;
    NSRange rangeOfReceiverToSearch = NSMakeRange(0, (int32_t)sourceString.length);
    NSStringCompareOptions options = ConvertFromCompareOptionsToNSStringCompareOptions(comparisonOptions);
    
    // in case mapping is not found
    if (options == 0)
        return result;
    if (!fromBeginning)
        options |= NSBackwardsSearch;
    
    NSRange nsRange = [sourceString rangeOfString:searchString
                                    options:options
                                    range:rangeOfReceiverToSearch
                                    locale:currentLocale];
    
    if (nsRange.location != NSNotFound)
    {   
        result.location = (int32_t)nsRange.location;
        result.length = (int32_t)nsRange.length;
    }
    else
    {
        result.location = -1;
    }
    
    return result;
}

#endif
