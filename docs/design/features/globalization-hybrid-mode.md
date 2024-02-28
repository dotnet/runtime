# Hybrid Globalization

Originally, internalization data is loaded from ICU data files. In `HybridGlobalization` mode we are leveraging the platform-native internationalization APIs, where it is possible, to allow for loading smaller ICU data files. We still need to rely on ICU files because for a bunch of globalization data no API equivalent is available. For some existing equivalents, the behavior does not fully match the original. The differences you can expect after switching on the mode are listed in this document. Expected size savings can be found under each platform section below.

Hybrid has lower priority than Invariant. To switch on the mode set the property in the build file:
```
<HybridGlobalization>true</HybridGlobalization>
```

## Behavioral differences

Hybrid mode does not use ICU data for some functions connected with globalization but relies on functions native to the platform. Because native APIs do not fully cover all the functionalities we currently support and because ICU data can be excluded from the ICU datafile only in batches defined by ICU filters, not all functions will work the same way or not all will be supported. To see what to expect after switching on `HybridGlobalization`, read the following paragraphs.

### WASM

For WebAssembly in Browser we are using Web API instead of some ICU data. Ideally, we would use `System.Runtime.InteropServices.JavaScript` to call JS code from inside of C# but we cannot reference any assemblies from inside of `System.Private.CoreLib`. That is why we are using iCalls instead. The host support depends on used Web API functions support - see **dependencies** in each section.

Hybrid has higher priority than sharding or custom modes, described in globalization-icu-wasm.md.

**HashCode**

Affected public APIs:
- System.Globalization.CompareInfo.GetHashCode

For invariant culture all `CompareOptions` are available.

For non-invariant cultures following `CompareOptions` are available:
- `CompareOption.None`
- `CompareOption.IgnoreCase`

The remaining combinations for non-invariant cultures throw `PlatformNotSupportedException`.

**SortKey**

Affected public APIs:
- System.Globalization.CompareInfo.GetSortKey
- System.Globalization.CompareInfo.GetSortKeyLength

For invariant culture all `CompareOptions` are available.

For non-invariant cultures `PlatformNotSupportedException` is thrown.

Indirectly affected APIs (the list might not be complete):
- Microsoft.VisualBasic.Collection.Add
- System.Collections.Hashtable.Add
- System.Collections.Hashtable.GetHash
- System.Collections.CaseInsensitiveHashCodeProvider.GetHashCode
- System.Collections.Specialized.NameObjectCollectionBase.BaseAdd
- System.Collections.Specialized.NameValueCollection.Add
- System.Collections.Specialized.NameObjectCollectionBase.BaseGet
- System.Collections.Specialized.NameValueCollection.Get
- System.Collections.Specialized.NameObjectCollectionBase.BaseRemove
- System.Collections.Specialized.NameValueCollection.Remove
- System.Collections.Specialized.OrderedDictionary.Add
- System.Collections.Specialized.NameObjectCollectionBase.BaseSet
- System.Collections.Specialized.NameValueCollection.Set
- System.Data.DataColumnCollection.Add
- System.Collections.Generic.HashSet
- System.Collections.Generic.Dictionary
- System.Net.Mail.MailAddress.GetHashCode
- System.Xml.Xsl.XslCompiledTransform.Transform

**Case change**

Affected public APIs:
- System.Globalization.TextInfo.ToLower,
- System.Globalization.TextInfo.ToUpper,
- System.Globalization.TextInfo.ToTitleCase.

Case change with invariant culture uses `toUpperCase` / `toLoweCase` functions that do not guarantee a full match with the original invariant culture.
Hybrid case change, same as ICU-based, does not support code points expansion e.g. "straße" -> "STRAßE".

- Final sigma behavior correction:

ICU-based case change does not respect final-sigma rule, but hybrid does, so "ΒΌΛΟΣ" -> "βόλος", not "βόλοσ".

Dependencies:
- [String.prototype.toUpperCase()](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/String/toUpperCase)
- [String.prototype.toLoweCase()](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/String/toLowerCase)
- [String.prototype.toLocaleUpperCase()](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/String/toLocaleUpperCase)
- [String.prototype.toLocaleLoweCase()](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/String/toLocaleLowerCase)

**String comparison**

Affected public APIs:
- System.Globalization.CompareInfo.Compare,
- System.String.Compare,
- System.String.Equals.
Indirectly affected APIs (the list might not be complete):
- Microsoft.VisualBasic.Strings.InStrRev
- Microsoft.VisualBasic.Strings.Replace
- Microsoft.VisualBasic.Strings.InStr
- Microsoft.VisualBasic.Strings.Split
- Microsoft.VisualBasic.Strings.StrComp
- Microsoft.VisualBasic.CompilerServices.LikeOperator.LikeObject
- Microsoft.VisualBasic.CompilerServices.LikeOperator.LikeString
- Microsoft.VisualBasic.CompilerServices.ObjectType.ObjTst
- Microsoft.VisualBasic.CompilerServices.ObjectType.LikeObj
- Microsoft.VisualBasic.CompilerServices.StringType.StrLike
- Microsoft.VisualBasic.CompilerServices.Operators.ConditionalCompareObjectEqual
- Microsoft.VisualBasic.CompilerServices.StringType.StrLike
- Microsoft.VisualBasic.CompilerServices.StringType.StrLikeText
- Microsoft.VisualBasic.CompilerServices.StringType.StrCmp
- System.Data.DataSet.ReadXml
- System.Data.DataTableCollection.Add

Dependencies:
- [String.prototype.localeCompare()](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/String/localeCompare)

The number of `CompareOptions` and `StringComparison` combinations is limited. Originally supported combinations can be found [here for CompareOptions](https://learn.microsoft.com/dotnet/api/system.globalization.compareoptions) and [here for StringComparison](https://learn.microsoft.com/dotnet/api/system.stringcomparison).


- `IgnoreWidth` is not supported because there is no equivalent in Web API. Throws `PlatformNotSupportedException`.
``` JS
let high = String.fromCharCode(65281)                                       // %uff83 = ﾃ
let low = String.fromCharCode(12486)                                        // %u30c6 = テ
high.localeCompare(low, "ja-JP", { sensitivity: "case" })                   // -1 ; case: a ≠ b, a = á, a ≠ A; expected: 0

let wide = String.fromCharCode(65345)                                       // %uFF41 = ａ
let narrow = "a"
wide.localeCompare(narrow, "en-US", { sensitivity: "accent" })              // 0; accent: a ≠ b, a ≠ á, a = A; expected: -1
```

For comparison where "accent" sensitivity is used, ignoring some type of character widths is applied and cannot be switched off (see: point about `IgnoreCase`).

- `IgnoreKanaType`:

It is always switched on for comparison with locale "ja-JP", even if this comparison option was not set explicitly.

``` JS
let hiragana = String.fromCharCode(12353)             // %u3041 = ぁ
let katakana = String.fromCharCode(12449)             // %u30A1 = ァ
let enCmp = hiragana.localeCompare(katakana, "en-US") // -1
let jaCmp = hiragana.localeCompare(katakana, "ja-JP") // 0
```

For locales different than "ja-JP" it cannot be used separately (no equivalent in Web API) - throws `PlatformNotSupportedException`.

- `None`:

No equivalent in Web API for "ja-JP" locale. See previous point about `IgnoreKanaType`. For "ja-JP" it throws `PlatformNotSupportedException`.

- `IgnoreCase`, `CurrentCultureIgnoreCase`, `InvariantCultureIgnoreCase`

For `IgnoreCase | IgnoreKanaType`, argument `sensitivity: "accent"` is used.

``` JS
let hiraganaBig = `${String.fromCharCode(12353)} A`                          // %u3041 = ぁ
let katakanaSmall = `${String.fromCharCode(12449)} a`                        // %u30A1 = ァ
hiraganaBig.localeCompare(katakanaSmall, "en-US", { sensitivity: "accent" }) // 0;  accent: a ≠ b, a ≠ á, a = A
```

Known exceptions:

| **character 1** | **character 2** | **CompareOptions** | **hybrid globalization** | **icu** |                       **comments**                      |
|:---------------:|:---------------:|--------------------|:------------------------:|:-------:|:-------------------------------------------------------:|
|        a        |   `\uFF41` ａ   |   IgnoreKanaType   |             0            |    -1   |            applies to all wide-narrow chars             |
|   `\u30DC` ボ   |    `\uFF8E` ﾎ   |     IgnoreCase     |             1            |    -1   | 1 is returned in icu when we additionally ignore width  |
|   `\u30BF` タ   |    `\uFF80` ﾀ   |     IgnoreCase     |             0            |    -1   |                                                         |


For `IgnoreCase` alone, a comparison with default option: `sensitivity: "variant"` is used after string case unification.

``` JS
let hiraganaBig = `${String.fromCharCode(12353)} A`                          // %u3041 = ぁ
let katakanaSmall = `${String.fromCharCode(12449)} a`                        // %u30A1 = ァ
let unchangedLocale = "en-US"
let unchangedStr1 = hiraganaBig.toLocaleLowerCase(unchangedLocale);
let unchangedStr2 = katakanaSmall.toLocaleLowerCase(unchangedLocale);
unchangedStr1.localeCompare(unchangedStr2, unchangedLocale)                  // -1;
let changedLocale = "ja-JP"
let changedStr1 = hiraganaBig.toLocaleLowerCase(changedLocale);
let changedStr2 = katakanaSmall.toLocaleLowerCase(changedLocale);
changedStr1.localeCompare(changedStr2, changedLocale)                        // 0;
```

From this reason, comparison with locale `ja-JP` `CompareOption` `IgnoreCase` and `StringComparison`: `CurrentCultureIgnoreCase` and `InvariantCultureIgnoreCase` behave like a combination `IgnoreCase | IgnoreKanaType` (see: previous point about `IgnoreKanaType`). For other locales the behavior is unchanged with the following known exceptions:

|                  **character 1**                 |                       **character 2**                      | **CompareOptions**                | **hybrid globalization** | **icu** |
|:------------------------------------------------:|:----------------------------------------------------------:|-----------------------------------|:------------------------:|:-------:|
| `\uFF9E`  (HALFWIDTH KATAKANA VOICED SOUND MARK) | `\u3099`   (COMBINING KATAKANA-HIRAGANA VOICED SOUND MARK) | None / IgnoreCase / IgnoreSymbols |             1            |    0    |

- `IgnoreNonSpace`

`IgnoreNonSpace` cannot be used separately without `IgnoreKanaType`. Argument `sensitivity: "case"` is used for comparison and it ignores both types of characters. Option `IgnoreNonSpace` alone throws `PlatformNotSupportedException`.

``` JS
let hiraganaAccent = `${String.fromCharCode(12353)} á`                           // %u3041 = ぁ
let katakanaNoAccent = `${String.fromCharCode(12449)} a`                         // %u30A1 = ァ
hiraganaAccent.localeCompare(katakanaNoAccent, "en-US", { sensitivity: "case" }) // 0; case:  a ≠ b, a = á, a ≠ A
```

- `IgnoreNonSpace | IgnoreCase`
Combination of `IgnoreNonSpace` and `IgnoreCase` cannot be used without `IgnoreKanaType`. Argument `sensitivity: "base"` is used for comparison and it ignores three types of characters. Combination `IgnoreNonSpace | IgnoreCase` alone throws `PlatformNotSupportedException`.

``` JS
let hiraganaBigAccent = `${String.fromCharCode(12353)} A á`                              // %u3041 = ぁ
let katakanaSmallNoAccent = `${String.fromCharCode(12449)} a a`                          // %u30A1 = ァ
hiraganaBigAccent.localeCompare(katakanaSmallNoAccent, "en-US", { sensitivity: "base" }) // 0; base: a ≠ b, a = á, a = A
```

- `IgnoreSymbols`

The subset of ignored symbols is limited to the symbols ignored by `string1.localeCompare(string2, locale, { ignorePunctuation: true })`. E.g. currency symbols, & are not ignored

``` JS
let hiraganaAccent = `${String.fromCharCode(12353)} á`                     // %u3041 = ぁ
let katakanaNoAccent = `${String.fromCharCode(12449)} a`                   // %u30A1 = ァ
hiraganaBig.localeCompare(katakanaSmall, "en-US", { sensitivity: "base" }) // 0; base: a ≠ b, a = á, a = A
```

- List of all `CompareOptions` combinations always throwing `PlatformNotSupportedException`:

`IgnoreCase`,

`IgnoreNonSpace`,

`IgnoreNonSpace | IgnoreCase`,

`IgnoreSymbols | IgnoreCase`,

`IgnoreSymbols | IgnoreNonSpace`,

`IgnoreSymbols | IgnoreNonSpace | IgnoreCase`,

`IgnoreWidth`,

`IgnoreWidth | IgnoreCase`,

`IgnoreWidth | IgnoreNonSpace`,

`IgnoreWidth | IgnoreNonSpace | IgnoreCase`,

`IgnoreWidth | IgnoreSymbols`

`IgnoreWidth | IgnoreSymbols | IgnoreCase`

`IgnoreWidth | IgnoreSymbols | IgnoreNonSpace`

`IgnoreWidth | IgnoreSymbols | IgnoreNonSpace | IgnoreCase`

`IgnoreKanaType | IgnoreWidth`

`IgnoreKanaType | IgnoreWidth | IgnoreCase`

`IgnoreKanaType | IgnoreWidth | IgnoreNonSpace`

`IgnoreKanaType | IgnoreWidth | IgnoreNonSpace | IgnoreCase`

`IgnoreKanaType | IgnoreWidth | IgnoreSymbols`

`IgnoreKanaType | IgnoreWidth | IgnoreSymbols | IgnoreCase`

`IgnoreKanaType | IgnoreWidth | IgnoreSymbols | IgnoreNonSpace`

`IgnoreKanaType | IgnoreWidth | IgnoreSymbols | IgnoreNonSpace | IgnoreCase`


**String starts with / ends with**

Affected public APIs:
- CompareInfo.IsPrefix
- CompareInfo.IsSuffix
- String.StartsWith
- String.EndsWith

Dependencies:
- [String.prototype.normalize()](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/String/normalize)
- [String.prototype.localeCompare()](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/String/localeCompare)

Web API does not expose locale-sensitive endsWith/startsWith function. As a workaround, both strings get normalized and weightless characters are removed. Resulting strings are cut to the same length and comparison is performed. This approach, beyond having the same compare option limitations as described under **String comparison**, has additional limitations connected with the workaround used. Because we are normalizing strings to be able to cut them, we cannot calculate the match length on the original strings. Methods that calculate this information throw PlatformNotSupported exception:

- [CompareInfo.IsPrefix](https://learn.microsoft.com/en-us/dotnet/api/system.globalization.compareinfo.isprefix?view=net-8.0#system-globalization-compareinfo-isprefix(system-readonlyspan((system-char))-system-readonlyspan((system-char))-system-globalization-compareoptions-system-int32@))
- [CompareInfo.IsSuffix](https://learn.microsoft.com/en-us/dotnet/api/system.globalization.compareinfo.issuffix?view=net-8.0#system-globalization-compareinfo-issuffix(system-readonlyspan((system-char))-system-readonlyspan((system-char))-system-globalization-compareoptions-system-int32@))

- `IgnoreSymbols`
Only comparisons that do not skip character types are allowed. E.g. `IgnoreSymbols` skips symbol-chars in comparison/indexing. All `CompareOptions` combinations that include `IgnoreSymbols` throw `PlatformNotSupportedException`.


**String indexing**

Affected public APIs:
- CompareInfo.IndexOf
- CompareInfo.LastIndexOf
- String.IndexOf
- String.LastIndexOf

Web API does not expose locale-sensitive indexing function. There is a discussion on adding it: https://github.com/tc39/ecma402/issues/506. In the current state, as a workaround, locale-sensitive string segmenter combined with locale-sensitive comparison is used. This approach, beyond having the same compare option limitations as described under **String comparison**, has additional limitations connected with the workaround used. Information about additional limitations:

- Support depends on [`Intl.segmenter's support`](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Intl/Segmenter#browser_compatibility).

- `IgnoreSymbols`

Only comparisons that ignore types of characters but do not skip them are allowed. E.g. `IgnoreCase` ignores type (case) of characters but `IgnoreSymbols` skips symbol-chars in comparison/indexing. All `CompareOptions` combinations that include `IgnoreSymbols` throw `PlatformNotSupportedException`.

- Some letters consist of more than one grapheme.

Using locale-sensitive segmenter `Intl.Segmenter(locale, { granularity: "grapheme" })` does not guarantee that string will be segmented by letters but by graphemes. E.g. in `cs-CZ` and `sk-SK` "ch" is 1 letter, 2 graphemes. The following code with `HybridGlobalization` switched off returns -1 (not found) while with `HybridGlobalization` switched on, it returns 1.

``` C#
new CultureInfo("sk-SK").CompareInfo.IndexOf("ch", "h"); // -1 or 1
```

- Some graphemes consist of more than one character.
E.g. `\r\n` that represents two characters in C#, is treated as one grapheme by the segmenter:

``` JS
const segmenter = new Intl.Segmenter(undefined, { granularity: "grapheme" });
Array.from(segmenter.segment("\r\n")) // {segment: '\r\n', index: 0, input: '\r\n'}
```

Because we are comparing grapheme-by-grapheme, character `\r` or character `\n` will not be found in `\r\n` string when `HybridGlobalization` is switched on.

- Some graphemes have multi-grapheme equivalents.
E.g. in `de-DE` ß (%u00DF) is one letter and one grapheme and "ss" is one letter and is recognized as two graphemes. Web API's equivalent of `IgnoreNonSpace` treats them as the same letter when comparing. Similar case: ǳ (%u01F3) and dz.
``` JS
"ß".localeCompare("ss", "de-DE", { sensitivity: "case" }); // 0
```

Using `IgnoreNonSpace` for these two with `HybridGlobalization` off, also returns 0 (they are equal). However, the workaround used in `HybridGlobalization` will compare them grapheme-by-grapheme and will return -1.

``` C#
new CultureInfo("de-DE").CompareInfo.IndexOf("strasse", "stra\u00DFe", 0, CompareOptions.IgnoreNonSpace); // 0 or -1
```

**Calandars**

Affected public APIs:
- DateTimeFormatInfo.AbbreviatedDayNames
- DateTimeFormatInfo.GetAbbreviatedDayName()
- DateTimeFormatInfo.AbbreviatedMonthGenitiveNames
- DateTimeFormatInfo.AbbreviatedMonthNames
- DateTimeFormatInfo.GetAbbreviatedMonthName()
- DateTimeFormatInfo.AMDesignator
- DateTimeFormatInfo.CalendarWeekRule
- DateTimeFormatInfo.DayNames
- DateTimeFormatInfo.GetDayName
- DateTimeFormatInfo.GetAbbreviatedEraName()
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


The Hybrid responses may differ because they use Web API functions. To better ilustrate the mechanism we provide an example for each endpoint. All exceptions cannot be listed, for reference check the response of specific version of Web API on your host.
|            **API**            |                                                         **Functions used**                                                         | **Example of difference for locale** |   **non-Hybrid**   |     **Hybrid**    |
|:-----------------------------:|:----------------------------------------------------------------------------------------------------------------------------------:|:------------------------------------:|:------------------:|:-----------------:|
|      AbbreviatedDayNames      |                                  `Date.prototype.toLocaleDateString(locale, { weekday: "short" })`                                 |                 en-CA                |        Sun.        |        Sun        |
| AbbreviatedMonthGenitiveNames |                           `Date.prototype.toLocaleDateString(locale, { month: "short", day: "numeric"})`                           |                 kn-IN                |         ಆಗ         |        ಆಗಸ್ಟ್       |
|     AbbreviatedMonthNames     |                                   `Date.prototype.toLocaleDateString(locale, { month: "short" })`                                  |                 lt-LT                |        saus.       |         01        |
|          AMDesignator         | `Date.prototype.toLocaleTimeString(locale, { hourCycle: "h12"})`; `Date.prototype.toLocaleTimeString(locale, { hourCycle: "h24"})` |              sr-Cyrl-RS              |      пре подне     |         AM        |
|        CalendarWeekRule       |                                          `Intl.Locale.prototype.getWeekInfo().minimalDay`                                          |                 none                 |          -         |         -         |
|            DayNames           |                                  `Date.prototype.toLocaleDateString(locale, { weekday: "long" })`                                  |                 none                 |          -         |         -         |
|    GetAbbreviatedEraName()    |                                   `Date.prototype.toLocaleDateString(locale, { era: "narrow" })`                                   |                 bn-IN                |       খৃষ্টাব্দ       |        খ্রিঃ       |
|          GetEraName()         |                                    `Date.prototype.toLocaleDateString(locale, { era: "short" })`                                   |                 vi-VI                |       sau CN       |         CN        |
|         FirstDayOfWeek        |                                           `Intl.Locale.prototype.getWeekInfo().firstDay`                                           |                 zn-CN                |       Sunday       |       Monday      |
|      FullDateTimePattern      |                                               `LongDatePattern` and `LongTimePattern`                                              |                   -                  |                    |                   |
|        LongDatePattern        |           `Intl.DateTimeFormat(locale, { weekday: "long", year: "numeric", month: "long", day: "numeric"}).format(date)`           |                 en-BW                | dddd, dd MMMM yyyy | dddd, d MMMM yyyy |
|        LongTimePattern        |                                       `Intl.DateTimeFormat(locale, { timeStyle: "medium" })`                                       |                 zn-CN                |      tth:mm:ss     |      HH:mm:ss     |
|        MonthDayPattern        |                            `Date.prototype.toLocaleDateString(locale, { month: "long", day: "numeric"})`                           |                 en-PH                |       d MMMM       |       MMMM d      |
|       MonthGenitiveNames      |                            `Date.prototype.toLocaleDateString(locale, { month: "long", day: "numeric"})`                           |                 ca-AD                |      de gener      |       gener       |
|           MonthNames          |                                   `Date.prototype.toLocaleDateString(locale, { month: "long" })`                                   |                 el-GR                |     Ιανουαρίου     |     Ιανουάριος    |
|       NativeCalendarName      |                                               `Intl.Locale.prototype.getCalendars()`                                               | for all locales it has English names | Gregorian Calendar |      gregory      |
|          PMDesignator         | `Date.prototype.toLocaleTimeString(locale, { hourCycle: "h12"})`; `Date.prototype.toLocaleTimeString(locale, { hourCycle: "h24"})` |                 mr-IN                |        म.उ.        |         PM        |
|        ShortDatePattern       |                                  `Date.prototype.toLocaleDateString(locale, {dateStyle: "short"})`                                 |                 en-CH                |     dd.MM.yyyy     |     dd/MM/yyyy    |
|        ShortestDayNames       |                                 `Date.prototype.toLocaleDateString(locale, { weekday: "narrow" })`                                 |                 none                 |          -         |         -         |
|        ShortTimePattern       |                                       `Intl.DateTimeFormat(locale, { timeStyle: "medium" })`                                       |                 bg-BG                |        HH:mm       |        H:mm       |
|        YearMonthPattern       |                           `Date.prototype.toLocaleDateString(locale, { year: "numeric", month: "long" })`                          |                 ar-SA                |      MMMM yyyy     |    MMMM yyyy g    |

### Apple platforms

For Apple platforms (iOS/tvOS/maccatalyst) we are using native apis instead of ICU data.

## String comparison

Affected public APIs:
- CompareInfo.Compare,
- String.Compare,
- String.Equals.

The number of `CompareOptions` and `NSStringCompareOptions` combinations are limited. Originally supported combinations can be found [here for CompareOptions](https://learn.microsoft.com/dotnet/api/system.globalization.compareoptions) and [here for NSStringCompareOptions](https://developer.apple.com/documentation/foundation/nsstringcompareoptions).

- `IgnoreSymbols` is not supported because there is no equivalent in native api. Throws `PlatformNotSupportedException`.

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

- All combinations that contain below `CompareOptions` always throw `PlatformNotSupportedException`:

   `IgnoreSymbols`

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

   As there is no IgnoreSymbols equivalent in NSStringCompareOptions all `CompareOptions` combinations that include `IgnoreSymbols` throw `PlatformNotSupportedException`

## String indexing

Affected public APIs:
- CompareInfo.IndexOf
- CompareInfo.LastIndexOf
- String.IndexOf
- String.LastIndexOf

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

   As there is no IgnoreSymbols equivalent in NSStringCompareOptions all `CompareOptions` combinations that include `IgnoreSymbols` throw `PlatformNotSupportedException`

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
