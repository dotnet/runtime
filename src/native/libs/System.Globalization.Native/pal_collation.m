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

typedef enum
{
    ERROR_INDEX_NOT_FOUND = -1,
    ERROR_COMPARISON_OPTIONS_NOT_FOUND = -2,
    ERROR_MIXED_COMPOSITION_NOT_FOUND = -3,
} ErrorCodes;

static NSLocale* GetCurrentLocale(const uint16_t* localeName, int32_t lNameLength)
{
    NSLocale *currentLocale;
    if(localeName == NULL || lNameLength == 0)
    {
        currentLocale = [NSLocale systemLocale];
    }
    else
    {
        NSString *locName = [NSString stringWithCharacters: localeName length: lNameLength];
        currentLocale = [NSLocale localeWithLocaleIdentifier:locName];
    }
    return currentLocale;
}

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
int32_t GlobalizationNative_CompareStringNative(const uint16_t* localeName, int32_t lNameLength, const uint16_t* lpSource, int32_t cwSourceLength, 
                                                const uint16_t* lpTarget, int32_t cwTargetLength, int32_t comparisonOptions)
{    
    NSLocale *currentLocale = GetCurrentLocale(localeName, lNameLength);
    NSString *sourceString = [NSString stringWithCharacters: lpSource length: cwSourceLength];
    NSString *sourceStrPrecomposed = sourceString.precomposedStringWithCanonicalMapping;
    NSString *targetString = [NSString stringWithCharacters: lpTarget length: cwTargetLength];
    NSString *targetStrPrecomposed = targetString.precomposedStringWithCanonicalMapping;

    NSRange comparisonRange = NSMakeRange(0, sourceStrPrecomposed.length);
    NSStringCompareOptions options = ConvertFromCompareOptionsToNSStringCompareOptions(comparisonOptions);
    
    // in case mapping is not found
    if (options == 0)
        return ERROR_COMPARISON_OPTIONS_NOT_FOUND;

    return [sourceStrPrecomposed compare:targetStrPrecomposed
                              options:options
                              range:comparisonRange
                              locale:currentLocale];
}

static NSString* RemoveWeightlessCharacters(NSString* source)
{
    NSError *error = nil;
    NSRegularExpression *regex = [NSRegularExpression regularExpressionWithPattern:@"[\u200B-\u200D\uFEFF\0]" options:NSRegularExpressionCaseInsensitive error:&error];

    if (error != nil)
        return source;

    NSString *modifiedString = [regex stringByReplacingMatchesInString:source options:0 range:NSMakeRange(0, [source length]) withTemplate:@""];

    return modifiedString;
}

static int32_t IsIndexFound(int32_t fromBeginning, int32_t foundLocation, int32_t newLocation)
{
    // last index
    if (!fromBeginning && foundLocation > newLocation)
        return 1;
    // first index
    if (fromBeginning && foundLocation > 0 && foundLocation < newLocation)
        return 1;
    return 0;
}

/*
Function: IndexOf
Find detailed explanation how this function works in https://github.com/dotnet/runtime/blob/main/docs/design/features/globalization-hybrid-mode.md
*/
Range GlobalizationNative_IndexOfNative(const uint16_t* localeName, int32_t lNameLength, const uint16_t* lpTarget, int32_t cwTargetLength,
                                        const uint16_t* lpSource, int32_t cwSourceLength, int32_t comparisonOptions, int32_t fromBeginning)
{
    assert(cwTargetLength >= 0);
    Range result = {ERROR_INDEX_NOT_FOUND, 0};
    NSStringCompareOptions options = ConvertFromCompareOptionsToNSStringCompareOptions(comparisonOptions);
    
    // in case mapping is not found
    if (options == 0)
    {
        result.location = ERROR_COMPARISON_OPTIONS_NOT_FOUND;
        return result;
    }
    
    NSString *searchString = [NSString stringWithCharacters: lpTarget length: cwTargetLength];
    NSString *searchStrCleaned = RemoveWeightlessCharacters(searchString);
    NSString *sourceString = [NSString stringWithCharacters: lpSource length: cwSourceLength];
    NSString *sourceStrCleaned = RemoveWeightlessCharacters(sourceString);

    if (sourceStrCleaned.length == 0 || searchStrCleaned.length == 0)
    {
       result.location = fromBeginning ? 0 : sourceString.length;
       return result;
    }

    NSLocale *currentLocale = GetCurrentLocale(localeName, lNameLength);
    NSString *searchStrPrecomposed = searchStrCleaned.precomposedStringWithCanonicalMapping;
    NSString *sourceStrPrecomposed = sourceStrCleaned.precomposedStringWithCanonicalMapping;

    // last index
    if (!fromBeginning)
        options |= NSBackwardsSearch;

    // check if there is a possible match and return -1 if not
    // doesn't matter which normalization form is used here
    NSRange rangeOfReceiverToSearch = NSMakeRange(0, sourceStrPrecomposed.length);
    NSRange containsRange = [sourceStrPrecomposed rangeOfString:searchStrPrecomposed
                                                  options:options
                                                  range:rangeOfReceiverToSearch
                                                  locale:currentLocale];

    if (containsRange.location == NSNotFound)
        return result;

    // in case search string is inside source string but we can't find the index return -3
    result.location = ERROR_MIXED_COMPOSITION_NOT_FOUND;
    // sourceString and searchString possibly have the same composition of characters
    rangeOfReceiverToSearch = NSMakeRange(0, sourceStrCleaned.length);
    NSRange nsRange = [sourceStrCleaned rangeOfString:searchStrCleaned
                                         options:options
                                         range:rangeOfReceiverToSearch
                                         locale:currentLocale];

    if (nsRange.location != NSNotFound)
    {   
        result.location = nsRange.location;
        result.length = nsRange.length;
        // in case of CompareOptions.IgnoreCase if letters have different representations in source and search strings
        // and case insensitive search appears more than one time in source string take last index for LastIndexOf and first index for IndexOf
        // e.g. new CultureInfo().CompareInfo.LastIndexOf("Is \u0055\u0308 or \u0075\u0308 the same as \u00DC or \u00FC?", "U\u0308", 25,18, CompareOptions.IgnoreCase);
        // should return 24 but here it will be 9
        if (!(comparisonOptions & IgnoreCase))
            return result;
    }

    // check if sourceString has precomposed form of characters and searchString has decomposed form of characters
    // convert searchString to a precomposed form
    NSRange precomposedRange = [sourceStrCleaned rangeOfString:searchStrPrecomposed
                                                 options:options
                                                 range:rangeOfReceiverToSearch
                                                 locale:currentLocale];

    if (precomposedRange.location != NSNotFound)
    {
        // in case of CompareOptions.IgnoreCase if letters have different representations in source and search strings
        // and search appears more than one time in source string take last index for LastIndexOf and first index for IndexOf
        // e.g. new CultureInfo().CompareInfo.LastIndexOf("Is \u0055\u0308 or \u0075\u0308 the same as \u00DC or \u00FC?", "U\u0308", 25,18, CompareOptions.IgnoreCase);
        // this will return 24
        if ((comparisonOptions & IgnoreCase) && IsIndexFound(fromBeginning, (int32_t)result.location, (int32_t)precomposedRange.location))
            return result;

        result.location = precomposedRange.location;
        result.length = precomposedRange.length;
        if (!(comparisonOptions & IgnoreCase))
           return result;
    }

    // check if sourceString has decomposed form of characters and searchString has precomposed form of characters
    // convert searchString to a decomposed form
    NSString *searchStrDecomposed = searchStrCleaned.decomposedStringWithCanonicalMapping;
    NSRange decomposedRange = [sourceStrCleaned rangeOfString:searchStrDecomposed
                                                options:options
                                                range:rangeOfReceiverToSearch
                                                locale:currentLocale];

    if (decomposedRange.location != NSNotFound)
    {
        if ((comparisonOptions & IgnoreCase) && IsIndexFound(fromBeginning, (int32_t)result.location, (int32_t)decomposedRange.location))
            return result;

        result.location = decomposedRange.location;
        result.length = decomposedRange.length;                    
        return result;
    }

    return result;
}

/*
 Return value is a "Win32 BOOL" (1 = true, 0 = false)
 */
int32_t GlobalizationNative_StartsWithNative(const uint16_t* localeName, int32_t lNameLength, const uint16_t* lpPrefix, int32_t cwPrefixLength, 
                                             const uint16_t* lpSource, int32_t cwSourceLength, int32_t comparisonOptions)          
{
    NSStringCompareOptions options = ConvertFromCompareOptionsToNSStringCompareOptions(comparisonOptions);
    
    // in case mapping is not found
    if (options == 0)
        return ERROR_COMPARISON_OPTIONS_NOT_FOUND;

    NSLocale *currentLocale = GetCurrentLocale(localeName, lNameLength);
    NSString *prefixString = [NSString stringWithCharacters: lpPrefix length: cwPrefixLength];
    NSString *prefixStrComposed = RemoveWeightlessCharacters(prefixString.precomposedStringWithCanonicalMapping);
    NSString *sourceString = [NSString stringWithCharacters: lpSource length: cwSourceLength];
    NSString *sourceStrComposed = RemoveWeightlessCharacters(sourceString.precomposedStringWithCanonicalMapping);

    NSRange sourceRange = NSMakeRange(0, prefixStrComposed.length > sourceStrComposed.length ? sourceStrComposed.length : prefixStrComposed.length);
        
    int32_t result = [sourceStrComposed compare:prefixStrComposed
                                        options:options
                                        range:sourceRange
                                        locale:currentLocale];
    return result == NSOrderedSame ? 1 : 0;
}

/*
 Return value is a "Win32 BOOL" (1 = true, 0 = false)
 */
int32_t GlobalizationNative_EndsWithNative(const uint16_t* localeName, int32_t lNameLength, const uint16_t* lpSuffix, int32_t cwSuffixLength,
                                           const uint16_t* lpSource, int32_t cwSourceLength, int32_t comparisonOptions)                
{
    NSStringCompareOptions options = ConvertFromCompareOptionsToNSStringCompareOptions(comparisonOptions);
    
    // in case mapping is not found
    if (options == 0)
        return ERROR_COMPARISON_OPTIONS_NOT_FOUND;

    NSLocale *currentLocale = GetCurrentLocale(localeName, lNameLength);
    NSString *suffixString = [NSString stringWithCharacters: lpSuffix length: cwSuffixLength];
    NSString *suffixStrComposed = RemoveWeightlessCharacters(suffixString.precomposedStringWithCanonicalMapping);
    NSString *sourceString = [NSString stringWithCharacters: lpSource length: cwSourceLength];
    NSString *sourceStrComposed = RemoveWeightlessCharacters(sourceString.precomposedStringWithCanonicalMapping);
    int32_t startIndex = suffixStrComposed.length > sourceStrComposed.length ? 0 : sourceStrComposed.length - suffixStrComposed.length;
    NSRange sourceRange = NSMakeRange(startIndex, sourceStrComposed.length - startIndex);
     
    int32_t result = [sourceStrComposed compare:suffixStrComposed
                                        options:options
                                        range:sourceRange
                                        locale:currentLocale];
    return result == NSOrderedSame ? 1 : 0;
}

#endif
