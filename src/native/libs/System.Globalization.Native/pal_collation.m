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
int32_t GlobalizationNative_CompareStringNative(const uint16_t* localeName, int32_t lNameLength, const uint16_t* lpSource, int32_t cwSourceLength, 
                                                const uint16_t* lpTarget, int32_t cwTargetLength, int32_t comparisonOptions)
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

NSString* ComposeString(NSString* str)
{
    NSString* source = str.precomposedStringWithCanonicalMapping;
    // Below we are removing weightless characters from the string to get ICU behavior.
    NSString* zarb = @"\u200d";
    NSString* nullChar = @"\0";
    // Remove zero width joiner
    NSString* result = [source stringByReplacingOccurrencesOfString:zarb withString:@""];
    // Remove null characters
    result = [result stringByReplacingOccurrencesOfString:nullChar withString:@""];

    return result;
}

/*
Function:
IndexOf
*/
Range GlobalizationNative_IndexOfNative(const uint16_t* localeName, int32_t lNameLength, const uint16_t* lpTarget, int32_t cwTargetLength,
                                        const uint16_t* lpSource, int32_t cwSourceLength, int32_t comparisonOptions, int32_t fromBeginning)
{
    assert(cwTargetLength >= 0);
    Range result = {-2, 0};

    NSLocale *currentLocale;
    if (localeName == NULL || lNameLength == 0)
    {
        currentLocale = [NSLocale systemLocale];
    }
    else
    {
        NSString *locName = [NSString stringWithCharacters: localeName length: lNameLength];
        currentLocale = [[NSLocale alloc] initWithLocaleIdentifier:locName];
    }

    NSString *searchString = [NSString stringWithCharacters: lpTarget length: cwTargetLength];
    NSString *searchStrComposed = ComposeString(searchString);
    NSString *sourceString = [NSString stringWithCharacters: lpSource length: cwSourceLength];
    NSString *sourceStrComposed = ComposeString(sourceString);

    if (searchStrComposed.length == 0)
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
    
    NSRange nsRange = [sourceStrComposed rangeOfString:searchStrComposed
                                         options:options
                                         range:rangeOfReceiverToSearch
                                         locale:currentLocale];
    
    if (nsRange.location != NSNotFound)
    {   
        result.location = nsRange.location;
        result.length = nsRange.length;
    }
    else
    {
        result.location = -1;
    }
    
    return result;
}

/*
 Return value is a "Win32 BOOL" (1 = true, 0 = false)
 */
int32_t GlobalizationNative_StartsWithNative(const uint16_t* localeName, int32_t lNameLength, const uint16_t* lpPrefix, int32_t cwPrefixLength, 
                                             const uint16_t* lpSource, int32_t cwSourceLength, int32_t comparisonOptions)
                        
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
