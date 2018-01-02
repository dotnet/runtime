# .NET Core Globalization Invariant Mode
 
Author: [Tarek Mahmoud Sayed](https://github.com/tarekgh)

The globalization invariant mode - new in .NET Core 2.0 - enables you to remove application dependencies on globalization data and [globalization behavior](https://docs.microsoft.com/en-us/dotnet/standard/globalization-localization/). This mode is an opt-in feature that provides more flexibility if you care more about reducing dependencies and the size of distribution than globalization functionality or globalization-correctness.

The drawback of running in the invariant mode is applications will get poor globalization support. This new option is only recommended for developers that understand globalization and the impact of its absence.

The following scenarios are affected when the invariant mode is enabled. Their invariant mode behavior is defined in this document.

- Cultures and culture data
- String casing
- String sorting and searching
- Sort keys
- String Normalization
- Internationalized Domain Names (IDN) support
- Time Zone display name on Linux

## Background
 
Globalization rules and the data that represents those rules frequently change, often due to country-specific policy changes (for example, changes in currency symbol, sorting behavior or time zones). Developers expect globalization behavior to always be current and for their applications to adapt to new data over time. In order to keep up with those changes, .NET Core (and the .NET Framework, too) depends on the underlying OS to keep up with these changes.

Relying on the underlying OS for globalization data has the following benefits:

* .NET apps have the same globalization behavior on a given OS as native apps (assuming they also rely on the OS).
* .NET apps do not have to carry this data.
* The .NET team doesn't have to maintain this data themselves (it's very expensive to do this!).

Globalization support has the following potential challenges for applications:

* Different behavior across OSes (and potentially OS versions).
* Installing/carrying the [ICU](http://icu-project.org) package on Linux (~28 MB).

Note: On Linux, .NET Core relies on globalization data from ICU. For example, [.NET Core Linux Docker images](https://github.com/dotnet/dotnet-docker/blob/master/2.0/runtime-deps/stretch/Dockerfile) install this component. Globalization data is available on Windows and macOS as part of their base installs.
  
## Cultures and culture data
 
When enabling the invariant mode, all cultures behave like the invariant culture. The invariant culture has the following characteristics: 
 
* Culture names (English, native display, ISO, language names) will return invariant names. For instance, when requesting culture native name, you will get "Invariant Language (Invariant Country)".
* All cultures LCID will have value 0x1000 (which means Custom Locale ID). The exception is the invariant cultures which will still have 0x7F.
* All culture parents will be invariant. In other word, there will not be any neutral cultures by default but the apps can still create a culture like "en".
* The application can still create any culture (e.g. "en-US") but all the culture data will still be driven from the Invariant culture. Also, the culture name used to create the culture should conform to [BCP 47 specs](https://tools.ietf.org/html/bcp47).
* All Date/Time formatting and parsing will use fixed date and time patterns. For example, the short date will be "MM/dd/yyyy" regardless of the culture used. Applications having old formatted date/time strings may not be able to parse such strings without using ParseExact.
* Numbers will always be formatted as the invariant culture. For example, decimal point will always be formatted as ".". Number strings previously formatted with cultures that have different symbols will fail parsing.
* All cultures will have currency symbol as "¤"
* Culture enumeration will always return a list with one culture which is the invariant culture.
 
## String casing
 
String casing (ToUpper and ToLower) will be performed for the ASCII range only. Requests to case code points outside that range will not be performed, however no exception will be thrown. In other words, casing will only be performed for character range ['a'..'z'].
 
Turkish I casing will not be supported when using Turkish cultures.
 
## String sorting and searching

String operations like [Compare](https://docs.microsoft.com/dotnet/api/?term=string.compare), [IndexOf](https://docs.microsoft.com/dotnet/api/?term=string.indexof) and [LastIndexOf](https://docs.microsoft.com/dotnet/api/?term=string.lastindexof) are always performed as [ordinal](https://en.wikipedia.org/wiki/Ordinal_number) and not linguistic operations regardless of the string comparing options passed to the APIs.
 
The [ignore case](https://docs.microsoft.com/dotnet/api/system.globalization.compareoptions.ignorecase) string sorting option is supported but only for the ASCII range as mentioned previously.
 
For example, the following comparison will resolve to being unequal:

* 'i', compared to
* Turkish I '\u0130', given
* Turkish culture, using 
* CompareOptions.Ignorecase

However, the following comparison will resolve to being equal:

* 'i', compared to
* 'I', using 
* CompareOptions.Ignorecase
 
It is worth noticing that all other [sort comparison options](https://docs.microsoft.com/dotnet/api/system.globalization.compareoptions) (for example, ignore symbols, ignore space, Katakana, Hiragana) will have no effect in the invariant mode (they are ignored).
 
## Sort keys
 
Sort keys are used mostly when indexing some data (for example, database indexing). When generating sort keys of 2 strings and comparing the sort keys the results should hold the exact same results as if comparing the original 2 strings. In the invariant mode, sort keys will be generated according to ordinal comparison while respecting ignore casing options.
 
## String normalization
 
String normalization normalizes a string into some form (for example, composed, decomposed forms). Normalization data is required to perform these operations, which isn't available in invariant mode. In this mode, all strings are considered as already normalized, per the following behavior: 

* If the app requested to normalize any string, the original string is returned without modification. 
* If the app asked if any string is normalized, the return value will always be `true`.
 
## Internationalized Domain Names (IDN) support

[Internationalized Domain Names](https://en.wikipedia.org/wiki/Internationalized_domain_name) require globalization data to perform conversion to ASCII or Unicode forms, which isn't available in the invariant mode. In this mode, IDN functionality has the following behavior:

* IDN support doesn't conform to the latest standard.
* IDN support will be incorrect if the input IDN string is not normalized since normalization is not supported in invariant mode.
* Some basic IDN strings will still produce correct values.
 
## Time zone display name in Linux
 
When running on Linux, ICU is used to get the time zone display name. In invariant mode, the standard time zone names are returned instead.
 
## Enabling the invariant mode
 
Applications can enable the invariant mode by either of the following:

1. in project file:

    ```xml
    <ItemGroup>
      <RuntimeHostConfigurationOption Include="System.Globalization.Invariant" Value="true" />
    </ItemGroup>
    ```

2. in `runtimeconfig.json` file:

    ```json
    {
        "runtimeOptions": {
            "configProperties": {
                "System.Globalization.Invariant": true
            }
        }
    }
    ```
  
3. setting environment variable value `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT` to `true` or `1`.

Note: value set in project file or `runtimeconfig.json` has higher priority than the environment variable.

## APP behavior with and without the invariant config switch
 
- If the invariant config switch is not set or it is set false
  - The framework will depend on the OS for the globalization support.
  - On Linux, if the ICU package is not installed, the application will fail to start.
- If the invariant config switch is defined and set to true
  - The invariant mode will be enabled and the app will get the behavior described in this document
  - Globalization data will not be used, even if available.
