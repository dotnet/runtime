## About

<!-- A description of the package and where one can find more documentation -->

Provides types for encoding and escaping strings for use in JavaScript, HTML, and URLs.

This package is essential for protecting web applications against cross-site scripting (XSS) attacks by safely encoding text, and it offers extensive support for Unicode, allowing fine-grained control over which characters are encoded and which are left unescaped.

## Key Features

<!-- The key features of this package -->

* Safe encoders for HTML, JavaScript, and URL strings.
* Extensible to support custom encoding scenarios, including the ability to specify Unicode ranges.
* Helps prevent cross-site scripting (XSS) vulnerabilities.
* Flexible Unicode encoding with support for specifying individual or predefined ranges to cover broader sets of characters, including options to avoid escaping specific language character sets.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

### Encoding HTML, JavaScript, and URLs

```csharp
using System.Text.Encodings.Web;

string unsafeString = "<script>alert('XSS Attack!');</script>";

// HTML encode the string to safely display it on a web page.
string safeHtml = HtmlEncoder.Default.Encode(unsafeString);
Console.WriteLine(safeHtml);
// &lt;script&gt;alert(&#x27;XSS Attack!&#x27;);&lt;/script&gt;

// JavaScript encode the string to safely include it in a JavaScript context.
string safeJavaScript = JavaScriptEncoder.Default.Encode(unsafeString);
Console.WriteLine(safeJavaScript);
// \u003Cscript\u003Ealert(\u0027XSS Attack!\u0027);\u003C/script\u003E

string urlPart = "user input with spaces and & symbols";

// URL encode the string to safely include it in a URL.
string encodedUrlPart = UrlEncoder.Default.Encode(urlPart);
Console.WriteLine(encodedUrlPart);
// user%20input%20with%20spaces%20and%20%26%20symbols
```

### Custom Encoding Scenario with Specific Unicode Ranges

```csharp
using System.Text.Encodings.Web;
using System.Text.Unicode;

TextEncoderSettings customEncoderSettings = new TextEncoderSettings();
customEncoderSettings.AllowCharacters('!', '*', '-', '.', '_', '~'); // RFC 3986 unreserved characters
customEncoderSettings.AllowRange(new UnicodeRange('a', 26));
customEncoderSettings.AllowRange(new UnicodeRange('A', 26));
customEncoderSettings.AllowRange(new UnicodeRange('0', 10));

// Create a URL encoder with the custom settings
UrlEncoder customUrlEncoder = UrlEncoder.Create(customEncoderSettings);

string customUrlPart = "custom data: (@123!)";

// By default, the symbols '(', ')', and '@' are not encoded
string defaultEncoded = UrlEncoder.Default.Encode(customUrlPart);
Console.WriteLine(defaultEncoded);
// custom%20data%3A%20(@123!)

// Now, the symbols '(', ')', and '@' are also encoded
string customEncoded = customUrlEncoder.Encode(customUrlPart);
Console.WriteLine(customEncoded);
// custom%20data%3A%20%28%40123!%29
```

### Serialization with Specific Unicode Character Sets

By default Cyrillic characters are encoded as Unicode escape sequences in JSON.

```json
{
  "Date": "2019-08-01T00:00:00-07:00",
  "TemperatureCelsius": 25,
  "Summary": "\u0436\u0430\u0440\u043A\u043E"
}
```

This can be customized by providing a custom `JavaScriptEncoder` to `JsonSerializerOptions`:

```csharp
JsonSerializerOptions options = new JsonSerializerOptions
{
    Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
    WriteIndented = true
};
jsonString = JsonSerializer.Serialize(weatherForecast, options1);
```

```json
{
  "Date": "2019-08-01T00:00:00-07:00",
  "TemperatureCelsius": 25,
  "Summary": "жарко"
}
```

More information about this can be found in the [How to customize character encoding with System.Text.Json](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/character-encoding) article.

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `System.Text.Encodings.Web.HtmlEncoder`
* `System.Text.Encodings.Web.JavaScriptEncoder`
* `System.Text.Encodings.Web.UrlEncoder`
* `System.Text.Encodings.Web.TextEncoder`
* `System.Text.Encodings.Web.TextEncoderSettings`
* `System.Text.Unicode.UnicodeRange`
* `System.Text.Unicode.UnicodeRanges`

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [API documentation](https://learn.microsoft.com/dotnet/api/system.text.encodings.web)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

System.Text.Encodings.Web is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
