// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdlib.h>
#include "pal_locale_internal.h"
#include "pal_collation.h"
#include "pal_casing.h"

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
        return -2;

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
    Range result = {-2, 0};
    NSStringCompareOptions options = ConvertFromCompareOptionsToNSStringCompareOptions(comparisonOptions);
    
    // in case mapping is not found
    if (options == 0)
        return result;
    
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
    result.location = -3;
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
        return -2;

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
        return -2;

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

/**
 * Append a code point to a string, overwriting 1 or 2 code units.
 * The offset points to the current end of the string contents
 * and is advanced (post-increment).
 * "Safe" macro, checks for a valid code point.
 * If a surrogate pair is written, checks for sufficient space in the string.
 * If the code point is not valid or a trail surrogate does not fit,
 * then isError is set to true.
 *
 * @param s const UChar * string buffer
 * @param i string offset, must be i<capacity
 * @param capacity size of the string buffer
 * @param c code point to append
 * @param isError output UBool set to true if an error occurs, otherwise not modified
 * @stable ICU 2.4
 */
#define Append(s, i, capacity, c, isError) UPRV_BLOCK_MACRO_BEGIN { \
    if((uint32_t)(c)<=0xffff) { \
        (s)[(i)++]=(uint16_t)(c); \
    } else if((uint32_t)(c)<=0x10ffff && (i)+1<(capacity)) { \
        (s)[(i)++]=(uint16_t)(((c)>>10)+0xd7c0); \
        (s)[(i)++]=(uint16_t)(((c)&0x3ff)|0xdc00); \
    } else /* c>0x10ffff or not enough space */ { \
        (isError)=true; \
    } \
} UPRV_BLOCK_MACRO_END

/*
Function:
ChangeCaseNative

Returns upper or lower casing of a string, taking into account the specified locale.
*/
int32_t GlobalizationNative_ChangeCaseNative(const uint16_t* localeName, int32_t lNameLength,
                                                 const uint16_t* lpSrc, int32_t cwSrcLength, uint16_t* lpDst, int32_t cwDstLength, int32_t bToUpper)
{
    NSLocale *currentLocale = GetCurrentLocale(localeName, lNameLength);
    NSString *source = [NSString stringWithCharacters: lpSrc length: cwSrcLength];
    NSString *result = bToUpper ? [source uppercaseStringWithLocale:currentLocale] : [source lowercaseStringWithLocale:currentLocale];

    int32_t srcIdx = 0, dstIdx = 0, isError = false;
    uint16_t dstCodepoint;
    if (result.length > cwDstLength)
        result = source;
    while (srcIdx < result.length)
    {
        dstCodepoint = [result characterAtIndex:srcIdx];
        Append(lpDst, dstIdx, cwDstLength, dstCodepoint, isError);
        srcIdx++;
        //assert(isError == false && srcIdx == dstIdx);
    }
    return 0;
}

/*
Function:
ChangeCaseInvariantNative

Returns upper or lower casing of a string.
*/
int32_t GlobalizationNative_ChangeCaseInvariantNative(const uint16_t* lpSrc, int32_t cwSrcLength, uint16_t* lpDst, int32_t cwDstLength, int32_t bToUpper)
{
    NSString *source = [NSString stringWithCharacters: lpSrc length: cwSrcLength];
    NSString *result = bToUpper ? source.uppercaseString : source.lowercaseString;

    int32_t srcIdx = 0, dstIdx = 0, isError = false;
    uint16_t dstCodepoint;
    if (result.length > cwDstLength)
        result = source;
    while (srcIdx < cwSrcLength)
    {
        dstCodepoint = [result characterAtIndex:srcIdx];
        Append(lpDst, dstIdx, cwDstLength, dstCodepoint, isError);
        srcIdx++;
        //assert(isError == false && srcIdx == dstIdx);
    }
    return 0;
}

#endif
