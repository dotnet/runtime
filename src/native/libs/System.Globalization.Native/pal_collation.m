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

static NSLocale* GetCurrentLocale(const uint16_t* localeName,int32_t lNameLength)
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
    NSString *sourceStrComposed = sourceString.precomposedStringWithCanonicalMapping;
    NSString *targetString = [NSString stringWithCharacters: lpTarget length: cwTargetLength];
    NSString *targetStrComposed = targetString.precomposedStringWithCanonicalMapping;

    NSRange comparisonRange = NSMakeRange(0, sourceStrComposed.length);
    NSStringCompareOptions options = ConvertFromCompareOptionsToNSStringCompareOptions(comparisonOptions);
    
    // in case mapping is not found
    if (options == 0)
        return -2;

    return [sourceStrComposed compare:targetStrComposed
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

// Remove weightless characters and normalize string with form C
static NSString* ComposeString(NSString* source)
{
    return RemoveWeightlessCharacters(source.precomposedStringWithCanonicalMapping);
}

/*
Function: IndexOf
Find detailed explanation how this function works in https://github.com/dotnet/runtime/blob/main/docs/design/features/globalization-hybrid-mode.md
*/
Range GlobalizationNative_IndexOfNative(const uint16_t* localeName, int32_t lNameLength, const uint16_t* lpTarget, int32_t cwTargetLength,
                                        const uint16_t* lpSource, int32_t cwSourceLength, int32_t comparisonOptions, int32_t fromBeginning)
{
    assert(cwTargetLength >= 0);
    Range result = {-1, 0};

    NSLocale *currentLocale = GetCurrentLocale(localeName, lNameLength);
    NSString *searchString = [NSString stringWithCharacters: lpTarget length: cwTargetLength];
    NSString *searchStrComposed = RemoveWeightlessCharacters(searchString);
    NSString *searchStrPrecomposed = searchStrComposed.precomposedStringWithCanonicalMapping;
    NSString *sourceString = [NSString stringWithCharacters: lpSource length: cwSourceLength];
    NSString *sourceStrComposed = RemoveWeightlessCharacters(sourceString);
    NSString *sourceStrPrecomposed = sourceStrComposed.precomposedStringWithCanonicalMapping;

    if (sourceStrComposed.length == 0 || searchStrComposed.length == 0)
    {
       result.location = fromBeginning ? 0 : sourceString.length;
       return result;
    }
    NSRange rangeOfReceiverToSearch = NSMakeRange(0, sourceStrComposed.length);
    NSStringCompareOptions options = ConvertFromCompareOptionsToNSStringCompareOptions(comparisonOptions);
    
    // in case mapping is not found
    if (options == 0)
        return result;
    // last index
    if (!fromBeginning)
        options |= NSBackwardsSearch;

    // check if source contains search string
    rangeOfReceiverToSearch = NSMakeRange(0, sourceStrPrecomposed.length);
    NSRange containsRange = [sourceStrPrecomposed rangeOfString:searchStrPrecomposed
                                                  options:options
                                                  range:rangeOfReceiverToSearch
                                                  locale:currentLocale];

    if (containsRange.location == NSNotFound)
    {
        result.location = -1;
        return result;
    }

    // localizedStandardRangeOfString is performing a case and diacritic insensitive, locale-aware search and finding first occurance.
    if ((comparisonOptions & IgnoreCase) && lNameLength == 0 && fromBeginning)
    {      
        NSRange localizedStandartRange = [sourceStrComposed localizedStandardRangeOfString:searchStrComposed];
        if (localizedStandartRange.location != NSNotFound)
        {
            result.location = localizedStandartRange.location;
            result.length = localizedStandartRange.length;                    
            return result;
        }       
    }
   
    NSRange nsRange = [sourceStrComposed rangeOfString:searchStrComposed
                                         options:options
                                         range:rangeOfReceiverToSearch
                                         locale:currentLocale];

    if (nsRange.location != NSNotFound)
    {   
        result.location = nsRange.location;
        result.length = nsRange.length;
        // in case of last index and CompareOptions.IgnoreCase 
        // if letters have different representations in source and search strings
        // and case insensitive search appears more than one time in source string take last index
        // e.g. new CultureInfo().CompareInfo.LastIndexOf("Is \u0055\u0308 or \u0075\u0308 the same as \u00DC or \u00FC?", "U\u0308", 25,18, CompareOptions.IgnoreCase);
        // should return 24 but here it will be 9
        if(fromBeginning || !(comparisonOptions & IgnoreCase))
            return result;
    }
    
    rangeOfReceiverToSearch = NSMakeRange(0, sourceStrComposed.length);
    // Normalize search string with Form C
    NSRange preComposedRange = [sourceStrComposed rangeOfString:searchStrPrecomposed
                                                  options:options
                                                  range:rangeOfReceiverToSearch
                                                  locale:currentLocale];

    if (preComposedRange.location != NSNotFound)
    {
        // in case of last index and CompareOptions.IgnoreCase 
        // if letters have different representations in source and search strings
        // and search appears more than one time in source string take last index
        // e.g. new CultureInfo().CompareInfo.LastIndexOf("Is \u0055\u0308 or \u0075\u0308 the same as \u00DC or \u00FC?", "U\u0308", 25,18, CompareOptions.IgnoreCase);
        // this will return 24 
        if ((int32_t)result.location > (int32_t)preComposedRange.location && !fromBeginning && (comparisonOptions & IgnoreCase))
            return result;
        result.location = preComposedRange.location;
        result.length = preComposedRange.length;
    }
    else
    {
        // Normalize search string with Form D
        NSString *searchStrDecomposed = searchStrComposed.decomposedStringWithCanonicalMapping;
        NSRange deComposedRange = [sourceStrComposed rangeOfString:searchStrDecomposed
                                                     options:options
                                                     range:rangeOfReceiverToSearch
                                                     locale:currentLocale];

        if (deComposedRange.location != NSNotFound)
        {
            result.location = deComposedRange.location;
            result.length = deComposedRange.length;                    
            return result;
        }
    }
    
    return result;
}

/*
 Return value is a "Win32 BOOL" (1 = true, 0 = false)
 */
int32_t GlobalizationNative_StartsWithNative(const uint16_t* localeName, int32_t lNameLength, const uint16_t* lpPrefix, int32_t cwPrefixLength, 
                                             const uint16_t* lpSource, int32_t cwSourceLength, int32_t comparisonOptions)
                        
{
    NSLocale *currentLocale = GetCurrentLocale(localeName, lNameLength);
    NSString *prefixString = [NSString stringWithCharacters: lpPrefix length: cwPrefixLength];
    NSString *prefixStrComposed = ComposeString(prefixString);
    NSString *sourceString = [NSString stringWithCharacters: lpSource length: cwSourceLength];
    NSString *sourceStrComposed = ComposeString(sourceString);

    NSRange sourceRange = NSMakeRange(0, prefixStrComposed.length > sourceStrComposed.length ? sourceStrComposed.length : prefixStrComposed.length);
    NSStringCompareOptions options = ConvertFromCompareOptionsToNSStringCompareOptions(comparisonOptions);
    
    // in case mapping is not found
    if (options == 0)
        return -2;
        
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
    NSLocale *currentLocale = GetCurrentLocale(localeName, lNameLength);
    NSString *suffixString = [NSString stringWithCharacters: lpSuffix length: cwSuffixLength];
    NSString *suffixStrComposed = ComposeString(suffixString);
    NSString *sourceString = [NSString stringWithCharacters: lpSource length: cwSourceLength];
    NSString *sourceStrComposed = ComposeString(sourceString);

    int32_t startIndex = suffixStrComposed.length > sourceStrComposed.length ? 0 : sourceStrComposed.length - suffixStrComposed.length;
    NSRange sourceRange = NSMakeRange(startIndex, sourceStrComposed.length - startIndex);
    NSStringCompareOptions options = ConvertFromCompareOptionsToNSStringCompareOptions(comparisonOptions);
    
    // in case mapping is not found
    if (options == 0)
        return -2;
        
    int32_t result = [sourceStrComposed compare:suffixStrComposed
                                        options:options
                                        range:sourceRange
                                        locale:currentLocale];
    return result == NSOrderedSame ? 1 : 0;
}

#endif
