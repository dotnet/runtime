# Security design doc for `System.StringComparer`

## Summary

The _Ordinal_ and _OrdinalIgnoreCase_ properties on `System.StringComparer` are guaranteed safe when provided with adversarial input. These singletons can be safely passed to the `Dictionary<string, ...>` and `HashSet<string>` ctors, and those collection instances will be resilient against hostile input. However, direct use of `StringComparer` instances outside the context of these specific collection types carries special security consideration, especially when data which is dependent on the comparers has been persisted. Developers should be aware of this and should take appropriate defensive measures when consuming the comparer types directly.

## Outline

This document discusses:

- Typical consumption patterns of the `StringComparer` type
- How the type's dependencies impact its security promises
- Security considerations governing usage of each of the comparer's APIs
- Security considerations when combining this type with inbox collection types
- Security considerations when transmitting or persisting processed data
- How modifications to the implementation might impact its security posture

## Introduction and scope

The `System.StringComparer` type is used for determining the sort order of two strings, determining whether two strings are equal, and computing a hash code of a string key for use in a bucketing keyed collection.

Each comparer instance captures specific logic for determining equality and sort order. This logic could be based on the numeric values of individual characters within the string (an "ordinal comparison") or on the rules of a human language (a "linguistic comparison"). Variations might include toggling case sensitivity, ignoring diacritics, treating ligatures as equivalent to their underlying sequence of graphemes, ignoring the character width of katakana, and so on.

This document covers the `StringComparer` type on .NET Framework 4.x and on .NET 6+ for a certain set of supported operating systems. Where relevant, differences between runtime and OSes are highlighted.

Analysis of custom `StringComparer`-derived types beyond those implemented within the runtime are out of scope of this document.

### Audience

This document focuses on security-related aspects of `StringComparer`'s implementation and typical usage by consumers. It supplements the type's [reference documentation](https://learn.microsoft.com/dotnet/api/system.stringcomparer), which covers a broader range of non-security topics.

This document is intended for:

- **Maintainers of `StringComparer`**, who need to understand and preserve the type's behavioral guarantees.
- **Consumers of `StringComparer`**, who depend on these guarantees.

## Assumptions and dependencies

The `StringComparer` type is dependent on `System.Globalization.CultureInfo` and `System.Globalization.CompareInfo` for some of its internal logic. A full security design doc for the globalization types has not been published; however, relevant globalization behaviors are called out in this document where appropriate.

The `CompareInfo` type is itself dependent on [NLS](https://learn.microsoft.com/windows/win32/intl/national-language-support) or [ICU](https://icu.unicode.org/), depending on runtime and operating system. NLS is used by .NET Framework 4.x and can optionally be enabled in .NET 6+ apps on Windows. ICU is used by default on .NET 6+ across all operating systems where it is available.

Environments where `CompareInfo` utilizes neither NLS nor ICU are not covered by this document.

## API usage and security considerations

In this section, given an arbitrary-length UTF-16 / UCS-2 input string $s$, let $\mathit{len}(s)$ represent the length in `char`s of the string.

For equality and comparison methods which take two arguments, let $s_1$ and $s_2$ represent the two input strings to the method.

In this section, the phrase "ordinal comparer" encompasses both _Ordinal_ and _OrdinalIgnoreCase_ comparers. A "linguistic comparer" is any non-ordinal comparer, including culture-agnostic comparers like _InvariantCulture_ and _InvariantCultureIgnoreCase_.

### Comparison (`Compare`) and equality (`Equals`)

#### Ordinal comparers

Ordinal comparison methods are guaranteed to be resilient against adversarial input. Adversarial input cannot cause denial of service, buffer overrun, or any other misbehavior within the equality method itself. If the internal method logic would fail (e.g., the input strings are too long), an exception will be thrown gracefully.

The guaranteed worst-case runtime of comparison is $O\left(\min \left(\mathit{len}(s_1), \mathit{len}(s_2)\right)\right)$. Additionally, if the two input strings first differ at character index $i$ as measured from the start of the string, the guaranteed worst-case runtime of comparison is $O(i)$.

Equality checking _may_ provide further optimizations, such as early-exiting if two input strings are known to be of different lengths. These optimizations are not guaranteed (and in fact cannot be guaranteed for variable-length character sets like UTF-8). Callers must not rely on such optimizations.

For case-insensitive comparisons, the return value of `Compare` and `Equals` are not guaranteed to be consistent across NLS and ICU. For example, `StringComparer.OrdinalIgnoreCase.Equals("\U00010CD4", "\U00010C94")` returns _true_ on recent versions of ICU but _false_ on recent versions of NLS.

Additionally, OS and .NET runtime updates may update the version of NLS or ICU in use. When this happens, strings that previously compared as not equal under a case-insensitive comparer may begin to compare as equal. This can occur if one of the input strings contains a code point which was not assigned in an earlier version of NLS / ICU but which is now assigned after a system or runtime update. For example, since the code points `U+10595` and `U+105BC` were not assigned until Unicode 14.0, when running under ICU mode, the .NET 6 runtime (which embeds the Unicode 13.0 casing tables) would evaluate the statement `StringComparer.OrdinalIgnoreCase.Equals("\U00010595", "\U000105BC")` to be _false_, while the .NET 8 runtime (which embeds the Unicode 15.0 casing tables) would evaluate that same statement to be _true_.

> **Tip**
>
> Applications which rely on case-insensitive equality comparisons for security, such as comparing usernames, should restrict the set of allowed characters to avoid ICU / NLS updates causing conflicts. [UTS \#39](https://www.unicode.org/reports/tr39/) discusses this in more detail.
>
> .NET also provides the [`CompareInfo.IsSortable`](https://learn.microsoft.com/dotnet/api/system.globalization.compareinfo.issortable) method, which can be used to determine whether the input data consists solely of assigned characters known to the version of NLS in use by the runtime. The `IsSortable` method does not have the same semantic on ICU and should not be used to determine whether the underlying ICU library believes that an input consists solely of assigned characters.

Services utilizing components written in a variety of languages or distributed across heterogeneous operating environments should be aware of the possibility of **confusion attacks**. Case-insensitive comparisons in .NET are performed by first converting each input to uppercase, then comparing the converted inputs. In NLS, this is performed by the Win32 [`CompareStringOrdinal`](https://learn.microsoft.com/windows/win32/api/stringapiset/nf-stringapiset-comparestringordinal) API. In ICU, .NET queries the underlying ICU library's casing tables. If running in Invariant globalization mode, .NET queries its own local copy of the ICU casing tables. This allows .NET to mimic legacy `CompareStringOrdinal` behavior when running on ICU.

This means that .NET's view of whether two strings are case-insensitive equal may differ from other languages' view, even when provided with the same inputs. For example, [Java maps strings to uppercase then back to lowercase.](https://docs.oracle.com/en/java/javase/11/docs/api/java.base/java/lang/String.html#equalsIgnoreCase(java.lang.String)) [Go uses Unicode case folding.](https://pkg.go.dev/strings#EqualFold) If an adversary can trick two different subsystems in the same service into disagreeing about whether two strings are case-insensitive equal, they could subvert the service's business logic, perhaps even elevating their own privilege.

Consider for example the input strings `"Administrator"` and `"Adminiſtrator"`. (The latter input string contains `U+017F LATIN SMALL LETTER LONG S`.) A case-insensitive equality comparison between these two strings depends on the programming language being used.

```cs
// .NET Framework and .NET 6+ - prints FALSE
Console.WriteLine(StringComparer.OrdinalIgnoreCase.Equals("Administrator", "Adminiſtrator"));
```
```go
// go - prints TRUE
fmt.Println(strings.EqualFold("Administrator", "Adminiſtrator"))
```
```java
// Java - prints TRUE
System.out.println("Administrator".equalsIgnoreCase("Adminiſtrator"));
```

#### Linguistic comparers

Linguistic comparison methods _are not_ guaranteed to be resilient to adversarial input.

Neither ICU nor NLS publishes guarantees about the behavior of the comparison routines in the face of adversarial input. While the developers of these libraries are responsive to issues like buffer overruns and OOM failures, no published claims are made against algorithmic complexity attacks.

It has been observed in practice that ICU and NLS's linguistic comparers operate in $O\left(\mathit{len}(s_1) + \mathit{len}(s_2)\right)$ time. However, this is not a published guarantee. If the underlying algorithms are subject to backtracking, the complexity could balloon to $O\left(\mathit{len}(s_1) \cdot \mathit{len}(s_2)\right)$ or some other undesirable limit.

Under a linguistic comparer, if the two input strings first differ at character index $i$ as measured from the start of the string, no claim is made that the worst-case runtime of linguistic comparison is $O(i)$. (Unicode Technical Standard \#10 even [mentions how tricky this is](https://www.unicode.org/reports/tr10/tr10-49.html#Incremental_Comparison).)

The result of comparison and equality operations depends on the version of ICU or NLS in use. As OSes improve the quality of their globalization data, the rules governing how equality is determined may change. Applications may see the result of comparison and equality operations change when switching between ICU and NLS, when updating the underlying OS, or when updating the underlying version of ICU. Such changes can be observed even if `CompareInfo.IsSortable` returns _true_ on both input strings.

> **Caution**
>
> Developers _should not_ use linguistic comparers when processing security-sensitive identifiers like form fields and usernames. Ordinal comparers are better suited to this task. See [Best practices for comparing strings in .NET](https://learn.microsoft.com/dotnet/standard/base-types/best-practices-strings) for further discussion.

### Hash code calculation (`StringComparer.GetHashCode`)

#### Ordinal comparers

Ordinal comparers' hash code calculation routines are guaranteed to be resilient against adversarial input. See the comparison section above for a discussion on guarantees.

`StringComparer` explicitly disclaims that a hash code generated from adversarial input is fit for consumption by the caller. Even though the calculation routine itself is resistant to adversarial input, the resulting hash code may have collisions or other patterns which make it unfit for direct consumption.

Some .NET runtimes utilize Marvin32 as an implementation detail in hash code calculation, where the seed is randomly chosen at app startup. Though Marvin32 appears to be a robust algorithm, it would not be appropriate to rely on the algorithm for hash flooding resistance in this context. The inability to roll the seed increases the risk that side channels or disclosure of the hash code outputs to adversaries could provide enough information for them to reconstruct the seed or otherwise generate collisions.

> **Note**
>
> Built-in collection types like `Dictionary<string, ...>` and `HashSet<string>` special-case the caller providing `StringComparer.Ordinal` and `StringComparer.OrdinalIgnoreCase` as the collection ctor's _comparer_ parameter. In these cases, the collection instance uses a special implementation of hash code calculation which is resilient to hash flooding attacks.

The guaranteed complexity of hash code calculation is $O\left(\mathit{len}(s)\right)$. The implementation of the hash code calculation differs between runtimes, and it may even differ between invocations of the same application on the same machine. Hash code values are meaningless outside the application which generates them, and they should not be transmitted outside the application or otherwise persisted.

Different `StringComparer` instances may return different hash code calculations for the same input string. For example, `StringComparer.Ordinal.GetHashCode(s)` and `StringComparer.OrdinalIgnoreCase.GetHashCode(s)` may return different values, even when provided the same input string $s$.

#### Linguistic comparers

Linguistic comparers' hash code calculation methods _are not_ guaranteed to be resilient to adversarial input.

The `GetHashCode` method relies on the ICU and NLS concepts of a "sort key", and neither ICU nor NLS publishes guarantees about the behavior of their sort key calculation routines in the face of adversarial input. While the developers of these libraries are responsive to issues like buffer overruns, no published claims are made against algorithmic complexity attacks.

As with ordinal comparers, hash codes produced by linguistic comparers are not guaranteed stable across application invocations. They should not be transmitted or persisted. Hash codes resulting from adversarial input are not guaranteed fit for direct consumption.

It has been observed in practice that ICU and NLS's sort key calculation routines operate in $O\left(\mathit{len}(s)\right)$ time. However, this is not a published guarantee. If the underlying algorithms are subject to backtracking, the complexity could balloon to $O\left(\mathit{len}(s)^2\right)$.

> **Note**
>
> Built-in collection types like `Dictionary<string, ...>` and `HashSet<string>` special-case built-in linguistic `StringComparer` instances. In these cases, the collection uses a special implementation of hash code calculation which is resilient to hash flooding attacks. However, the custom hash code calculation routine would still be subject to any catastrophic backtracking or other vulnerabilities present in the underlying ICU / NLS sort key production APIs.

## Relationship to other inbox comparers

### `EqualityComparer<string>.Default`

This is the default comparer used by collection types like `Dictionary<string, ...>` and `HashSet<string>` if no explicit comparer has been provided to the collection's ctor.

The comparer returned by `EqualityComparer<string>.Default` is functionally equivalent to `StringComparer.Ordinal`. Both the `Equals` and `GetHashCode` behaviors are as described above under the "ordinal" section.

### `Comparer<string>.Default`

This is the default comparer used by collection types like `SortedDictionary<string, ...>` and `SortedSet<string>` if no explicit comparer has been provided to the collection's ctor. It is also the default comparer used by utility methods like `Array.Sort<string>(string[])`.

The comparer returned by `Comparer<string>.Default` is mostly equivalent to `StringComparer.CurrentCulture`. The `Compare` behavior is as described above under the "linguistic" section.

The key difference between the two is when the current culture object is captured. When the `StringComparer.CurrentCulture` property is queried, the `CultureInfo.CurrentCulture` thread local value is captured immediately, and the returned `StringComparer` instance is locked to that culture object. The returned `StringComparer` instance will continue to utilize the captured culture object even if the active thread's culture changes.

The `Comparer<string>.Default` property getter, on the other hand, returns a special comparer object that is not locked to any specific culture. It instead queries the thread's current culture object on each call to `Compare`.

```cs
using System;
using System.Collections.Generic;
using System.Globalization;

CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
var comparer1 = StringComparer.CurrentCulture;
CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
var result1 = comparer1.Compare("string1", "string2"); // uses en-US rules

CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
var comparer2 = Comparer<String>.Default;
CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
var result2 = comparer2.Compare("string1", "string2"); // uses fr-FR rules
```

### Security considerations for collection types

Because collection types might use either `EqualityComparer<string>.Default` or `Comparer<string>.Default` when no explicit comparer has been provided, and because these two comparers have different comparison logic, callers should take caution to read the documentation for the collection type they're consuming and confirm that the default behavior is appropriate for their scenario.

This is particularly important when the keys might be provided by an adversary. The adversary might craft keys to force entries to be overwritten within the dictionary rather than added to the dictionary. Overwriting a trusted entry with an adversarial value could subvert the app's logic. The example below shows how two different components in a web application - one using `Dictionary<string, ...>`, the other using `SortedDictionary<string, ...>` - might observe an identical query string yet draw different conclusions about what values are present in the payload. This discrepancy could be leveraged in a confusion attack.

```cs
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.WebUtilities;

const string QueryString = "?fruit=banana&fruit%e2%80%8d=orange"; // [E2 80 8D] = U+200D ZERO-WIDTH JOINER

// Parse the query string and write the values to a Dictionary<string, ...> with default ctor
var normalDict = new Dictionary<string, string>();
foreach (var kvp in new QueryStringEnumerable(QueryString))
{
    normalDict[kvp.DecodeName().ToString()] = kvp.DecodeValue().ToString();
}
Console.WriteLine($"normalDict['fruit'] = {normalDict["fruit"]}"); // "banana"

// Parse the query string and write the values to a SortedDictionary<string, ...> with default ctor
var sortedDict = new SortedDictionary<string, string>();
foreach (var kvp in new QueryStringEnumerable(QueryString))
{
    sortedDict[kvp.DecodeName().ToString()] = kvp.DecodeValue().ToString();
}
Console.WriteLine($"sortedDict['fruit'] = {sortedDict["fruit"]}"); // "orange"
```

> **Tip**
>
> If unsure as to the collection's default behavior, pass an explicit `StringComparer` object (preferably _Ordinal_ or _OrdinalIgnoreCase_) to the collection's ctor to remove any ambiguity.
>
> Additionally, consider using the collection type's _Add_ method instead of the indexer setter. For most collection types, this will cause an exception to be thrown if the caller attempts to add duplicate entries.

## Security considerations for persisted storage and data transmission

The _API usage and security considerations_ section above discusses how equality or sort comparisons of two strings may differ based on operating system or runtime version. This presents opportunities for adversaries to abuse this behavior.

Consider a web service which accepts an array of strings from the client. The service sorts this array using an _OrdinalIgnoreCase_ or a linguistic comparer, then persists this sorted array to storage. Some other component of the web service loads this entry from the database and - knowing the writer already sorted the data before persisting it - performs an operation which assumes the data is properly sorted.

Since different components within the application might be executing on different runtimes or within different environments, those components might disagree on what "properly sorted" means. If operating over malicious input, this provides a potential opportunity for an adversary to manipulate control flow within the target service.

The example below demonstrates this potential. Assume the application processing the initial request and persisting the sorted data to the database is a .NET Framework 4.x application.

```cs
/* called from .NET Framework 4.x */

using System;
using System.Linq;
using Microsoft.AspNetCore.WebUtilities;

// read a string from the request (hardcoded here for demonstration)
string userInput = "?keywords=target&keywords=tar%C6%84get&keywords=tar%C6%86get&keywords=zulu"; 
var keywords = QueryHelpers.ParseQuery(userInput); // from package Microsoft.AspNetCore.WebUtilities

Array.Sort(keywords); // uses Comparer<string>.Default
WriteToDatabase(keywords); // order: [ "tar\u0184get", "tar\u0186get", "target", "zulu" ]
```

Now assume the application loading from the database is a .NET 8 application running in default ICU mode.

```cs
/* called from .NET 8 */

using System;

// read the previously sorted array from the database (hardcoded here for demonstration)
string[] sortedKeywords = ["tar\u0184get", "tar\u0186get", "target", "zulu"];
Console.WriteLine($"Array.BinarySearch(..., 'target') = {Array.BinarySearch(sortedKeywords, "target")}"); // prints '-1'
```

When the data is persisted to the database, it's sorted according to the rules of the application which performs the database write, _not the rules of the application which performs the database read._ If the adversary knows the reading application utilizes `Array.BinarySearch` in control flow logic, and if they are able to construct a payload which violates the binary search invariants, they might be able to coerce the reading application into invoking a logic branch which should not have executed. In the example above, they can coerce the `BinarySearch` method into [returning a negative value](https://learn.microsoft.com/dotnet/api/system.array.binarysearch), indicating the string `"target"` is not in the input array.

> **Tip**
>
> If data's provenance is potentially adversarial, applications should consider re-validating the well-formedness and structure of the data before operating on it. Even if some other trusted component has independently validated the data, it might not be safe to assume that the trusted component's previous validation perfectly matches the validation assumed by the current component.

## Future improvements and considerations

### Improving hash flooding resistance through API modifications

`StringComparer`'s lack of guarantee regarding the fitness of adversarial-influenced hash codes could make it difficult for developers to create their own secure keyed collection types. This protection logic is currently an implementation detail of `Dictionary<string, ...>` and other inbox collection types.

To facilitate a healthy third-party ecosystem of secure keyed collection types, it may be worthwhile to investigate allowing `StringComparer` creation utilizing per-instance randomization rather than a single global randomization seed. The existing `StringComparer.FromComparison` and `StringComparer.Create` factory methods could easily be changed to support this. However, it would not address that most developers currently use the static singleton properties like _Ordinal_ and _OrdinalIgnoreCase_. This could require refactoring of a significant portion of code throughout the .NET ecosystem.

The _Future improvements and considerations_ section of [the `System.HashCode` security design doc](System.HashCode.md) discusses potential options here in more detail.