# Hybrid Globalization

Originally, internalization data is loaded from ICU data files. In `HybridGlobalization` mode we are leveraging the platform-native internationalization APIs, where it is possible, to allow for loading smaller ICU data files. We still need to rely on ICU files because for a bunch of globalization data no API equivalent is available. For some existing equivalents, the behavior does not fully match the original. The differences you can expect after switching on the mode are listed in this document. Expected size savings can be found under each platform section below.

Hybrid has lower priority than Invariant. To switch on the mode set the property in the build file:
```
<HybridGlobalization>true</HybridGlobalization>
```

## Behavioral differences

Hybrid mode does not use ICU data for some functions connected with globalization but relies on functions native to the platform. Because native APIs do not fully cover all the functionalities we currently support and because ICU data can be excluded from the ICU datafile only in batches defined by ICU filters, not all functions will work the same way or not all will be supported. To see what to expect after switching on `HybridGlobalization`, read the following paragraphs.

### Apple platforms

For Apple platforms (iOS/tvOS/maccatalyst) we are using native apis instead of ICU data.

## String comparison

Affected public APIs:
- CompareInfo.Compare,
- String.Compare,
- String.Equals.

Mapped to Apple Native API `compare:options:range:locale:`(https://developer.apple.com/documentation/foundation/nsstring/1414561-compare?language=objc)
This implementation uses normalization techniques such as `precomposedStringWithCanonicalMapping`,
which can result in behavior differences compared to other platforms.
Specifically, the use of precomposed strings and additional locale-based string folding can affect the results of comparisons.
Due to these differences, the exact result of string compariso on Apple platforms may differ.

The number of `CompareOptions` and `NSStringCompareOptions` combinations are limited. Originally supported combinations can be found [here for CompareOptions](https://learn.microsoft.com/dotnet/api/system.globalization.compareoptions) and [here for NSStringCompareOptions](https://developer.apple.com/documentation/foundation/nsstringcompareoptions).

- `IgnoreSymbols` is supported by filtering ignorable symbols on the managed side before invoking the native API.

- `IgnoreKanaType` is implemented using [`kCFStringTransformHiraganaKatakana`](https://developer.apple.com/documentation/corefoundation/kcfstringtransformhiraganakatakana?language=objc) then comparing strings.


- `None`:

   `CompareOptions.None` is mapped to `NSStringCompareOptions.NSLiteralSearch`

   There are some behaviour changes. Below are examples of such cases.

   | **character 1** | **character 2** | **CompareOptions** | **hybrid globalization** | **icu** |                       **comments**                      |
   |:---------------:|:---------------:|--------------------|:------------------------:|:-------:|:-------------------------------------------------------:|
   |   `\u3042` あ   |   `\u30A1` ァ   |   None  |             1            |    -1   |     hiragana and katakana characters are ordered differently compared to ICU    |
   |   `\u304D\u3083` きゃ  |   `\u30AD\u30E3` キャ |     None     |             1            |    -1   | hiragana and katakana characters are ordered differently compared to ICU  |
   |   `\u304D\u3083` きゃ  |   `\u30AD\u3083` キゃ  |     None     |             1           |    -1   |  hiragana and katakana characters are ordered differently compared to ICU  |
   |   `\u3070\u3073\uFF8C\uFF9E\uFF8D\uFF9E\u307C` ばびﾌﾞﾍﾞぼ  |   `\u30D0\u30D3\u3076\u30D9\uFF8E\uFF9E` バビぶベﾎﾞ  |     None     |   1  |  -1  | hiragana and katakana characters are ordered differently compared to ICU   |
   |   `\u3060` だ  |   `\u30C0` ダ  |     None     |   1  |  -1  |   hiragana and katakana characters are ordered differently compared to ICU |

- `StringSort` :

   `CompareOptions.StringSort` is mapped to `NSStringCompareOptions.NSLiteralSearch` .ICU's default is to use "StringSort", i.e. nonalphanumeric symbols come before alphanumeric. That is how works also `NSLiteralSearch`.

- `IgnoreCase`:

   `CompareOptions.IgnoreCase` is mapped to `NSStringCompareOptions.NSCaseInsensitiveSearch | NSStringCompareOptions.NSLiteralSearch`

   There are some behaviour changes. Below are examples of such cases.

   | **character 1** | **character 2** | **CompareOptions** | **hybrid globalization** | **icu** |                       **comments**                      |
   |:---------------:|:---------------:|--------------------|:------------------------:|:-------:|:-------------------------------------------------------:|
   |   `\u3060` だ |   `\u30C0` ダ  |     IgnoreCase     |   1  |  -1  |  hiragana and katakana characters are ordered differently compared to ICU  |

- `IgnoreNonSpace`:

   `CompareOptions.IgnoreNonSpace` is mapped to `NSStringCompareOptions.NSDiacriticInsensitiveSearch | NSStringCompareOptions.NSLiteralSearch`

- `IgnoreWidth`:

   `CompareOptions.IgnoreWidth` is mapped to `NSStringCompareOptions.NSWidthInsensitiveSearch | NSStringCompareOptions.NSLiteralSearch`

## String starts with / ends with

Affected public APIs:
- CompareInfo.IsPrefix
- CompareInfo.IsSuffix
- String.StartsWith
- String.EndsWith

Mapped to Apple Native API `compare:options:range:locale:`(https://developer.apple.com/documentation/foundation/nsstring/1414561-compare?language=objc)
Apple Native API does not expose locale-sensitive endsWith/startsWith function. As a workaround, both strings get normalized and weightless characters are removed. Resulting strings are cut to the same length and comparison is performed. As we are normalizing strings to be able to cut them, we cannot calculate the match length on the original strings. Methods that calculate this information throw PlatformNotSupported exception:

- [CompareInfo.IsPrefix](https://learn.microsoft.com/dotnet/api/system.globalization.compareinfo.isprefix)
- [CompareInfo.IsSuffix](https://learn.microsoft.com/dotnet/api/system.globalization.compareinfo.issuffix)

- `IgnoreSymbols`

   Supported by filtering ignorable symbols on the managed side prior to comparison using native API.

## String indexing

Affected public APIs:
- CompareInfo.IndexOf
- CompareInfo.LastIndexOf
- String.IndexOf
- String.LastIndexOf

Methods that calculate `matchLength` throw PlatformNotSupported exception:
[CompareInfo.IndexOf](https://learn.microsoft.com/en-us/dotnet/api/system.globalization.compareinfo.indexof?view=system-globalization-compareinfo-indexof(system-readonlyspan((system-char))-system-readonlyspan((system-char))-system-globalization-compareoptions-system-int32@))

[CompareInfo.LastIndexOf](https://learn.microsoft.com/en-us/dotnet/api/system.globalization.compareinfo.lastindexof?view=system-globalization-compareinfo-lastindexof(system-readonlyspan((system-char))-system-readonlyspan((system-char))-system-globalization-compareoptions-system-int32@))

Mapped to Apple Native API `rangeOfString:options:range:locale:`(https://developer.apple.com/documentation/foundation/nsstring/1417348-rangeofstring?language=objc)

In `rangeOfString:options:range:locale:` objects are compared by checking the Unicode canonical equivalence of their code point sequences.
In cases where search string contains diacritics and has different normalization form than in source string result can be incorrect.

Characters in general are represented by unicode code points, and some characters can be represented in a single code point or by combining multiple characters (like diacritics/diaeresis). Normalization Form C will look to compress characters to their single code point format if they were originally represented as a sequence of multiple code points. Normalization Form D does the opposite and expands characters into their multiple code point formats if possible.

`NSString` `rangeOfString:options:range:locale:` uses canonical equivalence to find the position of the `searchString` within the `sourceString`, however, it does not automatically handle comparison of precomposed (single code point representation) or decomposed (most code points representation). Because the `searchString` and `sourceString` can be of differing formats, to properly find the index, we need to ensure that the searchString is in the same form as the sourceString by checking the `rangeOfString:options:range:locale:` using every single normalization form.

Here are the covered cases with diacritics:
  1. Search string contains diacritic and has same normalization form as in source string.
  2. Search string contains diacritic but with source string they have same letters with different char lengths but substring is normalized in source.

     a. search string `normalizing to form C` is substring of source string. example: search string: `U\u0308` source string:  `Source is \u00DC` => matchLength is 1

     b. search string `normalizing to form D` is substring of source string. example: search string: `\u00FC` source string: `Source is \u0075\u0308` => matchLength is 2

Not covered case:
      Source string's intended substring match containing characters of mixed composition forms cannot be matched by 2. because partial precomposition/decomposition is not performed. example: search string: `U\u0308 and \u00FC` (Ü and ü) source string: `Source is \u00DC and \u0075\u0308` (Source is Ü and ü)
      as it is visible from example normalizaing search string to form C or D will not help to find substring in source string.

- `IgnoreSymbols`

   Supported by filtering ignorable symbols on the managed side prior to comparison using native API.

- Some letters consist of more than one grapheme.

   Apple Native Api does not guarantee that string will be segmented by letters but by graphemes. E.g. in `cs-CZ` and `sk-SK` "ch" is 1 letter, 2 graphemes. The following code with `HybridGlobalization` switched off returns -1 (not found) while with `HybridGlobalization` switched on, it returns 1.

   ``` C#
   new CultureInfo("sk-SK").CompareInfo.IndexOf("ch", "h"); // -1 or 1
   ```

- Some graphemes have multi-grapheme equivalents.
   E.g. in `de-DE` ß (%u00DF) is one letter and one grapheme and "ss" is one letter and is recognized as two graphemes. Apple Native API's equivalent of `IgnoreNonSpace` treats them as the same letter when comparing. Similar case: ǳ (%u01F3) and dz.

   Using `IgnoreNonSpace` for these two with `HybridGlobalization` off, also returns 0 (they are equal). However, the workaround used in `HybridGlobalization` will compare them grapheme-by-grapheme and will return -1.

   ``` C#
   new CultureInfo("de-DE").CompareInfo.IndexOf("strasse", "stra\u00DFe", 0, CompareOptions.IgnoreNonSpace); // 0 or -1
   ```


## SortKey

Affected public APIs:
- CompareInfo.GetSortKey
- CompareInfo.GetSortKeyLength
- CompareInfo.GetHashCode

Implemeneted using [stringByFoldingWithOptions:locale:](https://developer.apple.com/documentation/foundation/nsstring/1413779-stringbyfoldingwithoptions)

Note: This implementation does not construct SortKeys like ICU ucol_getSortKey does, and might not adhere to the specifications specifications of SortKey such as SortKeys from different collators not being comparable and merging sortkeys.


## Case change

Affected public APIs:
- TextInfo.ToLower,
- TextInfo.ToUpper

Below function are used from apple native functions:
- [uppercaseString](https://developer.apple.com/documentation/foundation/nsstring/1409855-uppercasestring)
- [lowercaseString](https://developer.apple.com/documentation/foundation/nsstring/1408467-lowercasestring)
- [uppercaseStringWithLocale](https://developer.apple.com/documentation/foundation/nsstring/1413316-uppercasestringwithlocale?language=objc)
- [lowercaseStringWithLocale](https://developer.apple.com/documentation/foundation/nsstring/1417298-lowercasestringwithlocale?language=objc)

## Calandars

Affected public APIs:
- DateTimeFormatInfo.AbbreviatedDayNames
- DateTimeFormatInfo.GetAbbreviatedDayName()
- DateTimeFormatInfo.AbbreviatedMonthGenitiveNames
- DateTimeFormatInfo.AbbreviatedMonthNames
- DateTimeFormatInfo.GetAbbreviatedMonthName()
- DateTimeFormatInfo.AMDesignator
- DateTimeFormatInfo.CalendarWeekRule
- DateTimeFormatInfo.DayNames
- DateTimeFormatInfo.GetDayName()
- DateTimeFormatInfo.GetEraName()
- DateTimeFormatInfo.FirstDayOfWeek
- DateTimeFormatInfo.FullDateTimePattern
- DateTimeFormatInfo.LongDatePattern
- DateTimeFormatInfo.LongTimePattern
- DateTimeFormatInfo.MonthDayPattern
- DateTimeFormatInfo.MonthGenitiveNames
- DateTimeFormatInfo.MonthNames
- DateTimeFormatInfo.GetMonthName()
- DateTimeFormatInfo.NativeCalendarName
- DateTimeFormatInfo.PMDesignator
- DateTimeFormatInfo.ShortDatePattern
- DateTimeFormatInfo.ShortestDayNames
- DateTimeFormatInfo.GetShortestDayName()
- DateTimeFormatInfo.ShortTimePattern
- DateTimeFormatInfo.YearMonthPattern

Apple Native API does not have an equivalent for abbreviated era name and will return empty string
- DateTimeFormatInfo.GetAbbreviatedEraName()
