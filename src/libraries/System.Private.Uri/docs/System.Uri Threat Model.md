# System.Uri

This threat model applies to .NET Framework and .NET 8.0 or newer. The document tries to call out the version when some behavior changed whenever possible.  
The document was written with the implementation on modern .NET in mind, so some details may be inaccurate for .NET Framework.

`Uri` is used in practically every .NET application and very often deals with user-influenced inputs.  
We warrant that its parsing is hardened against and may be used with hostile inputs, but the developer still needs to take care to use it properly.

## Parsing

Uri parses inputs in multiple phases, as lazily as possible. There are three phases internally:
- As part of the constructor / `TryCreate` call, the minimum amount of validation is performed to decide whether an exception should be thrown.
  - This involves deciding the scheme and validating the user info/host/port.
  - The starting offset of the Path is stored on the instance.
  - The path/query/fragment are not parsed at this time, and any arbitrary set of characters will be considered valid.
- When accessing a property such as `Host`, the implementation allocates a larger internal object to store some cached fields, and offsets into every component.
  - The section up to the path is parsed again. Offsets of the Scheme/User info/Host are stored.
- When accessing components that extend beyond the authority (path/query/fragment), the rest of the input is parsed.

Throughout parsing, Uri will internally store flags about any observations it makes, e.g. was any given component already in its canonical form (will we have to modify it when properties are accessed), whether the input contained escaped dots or slashes, will the path have to be compressed, etc.  
It relies on these observations to guide later parsing / component reconstruction.

Leading and trailing whitespace is ignored (implementation-defined as `' ', '\n', '\r', '\t'`).

### Relative Uris

The constructor and `TryCreate` helpers allow the developer to specify which kind of Uri they are prepared to deal with by specifying the `UriKind.Absolute / Relative / RelativeOrAbsolute`.

If the input scheme can't be parsed and the `UriKind` allows relative Uris, a relative instance is returned.  
If the input scheme _is_ valid, it's possible that a relative instance will not be returned, even if `UriKind.Relative` is specified. This is one of the rare types of validation performed on relative Uris. E.g. `"http://host"` can't be relative, but `"http:host"` can be.

Other than the scheme, any arbitrary set of characters may be considered a valid relative Uri. `IsWellFormedOriginalString` may be used to check if the input was already escaped.

A user can query whether a `Uri` instance is relative or not by checking the `IsAbsoluteUri` property.  
Only a small subset of operations are supported on a relative Uri: `IsWellFormedOriginalString`, `ToString`, `GetHashCode`, `GetComponents` with `UriComponents.SerializationInfoString`. Other operations (e.g. querying the `Host`) throw an `InvalidOperationException`.

Such instances are rarely interacted with directly. Instead they are passed along until they can be combined into an absolute Uri via `new Uri(Uri baseUri, Uri relativeUri)` or `TryCreate(Uri? baseUri, Uri? relativeUri, out Uri? result)`.

### Scheme

```
scheme = ALPHA *( ALPHA / DIGIT / "+" / "-" / "." )
URI    = scheme ":" ...
```

The scheme is always parsed first as it affects all future parsing decisions.
Its length is currently limited at 1024 characters.

Uri uses the scheme to look up which parser to use for the remainder of the input. See also the [custom parsers](#custom-parsers) section.

Schemes with built-in recognition (e.g. http) define a set of allowed and required properties for the Uri. E.g. does it require an authority or whether it may contain a query string.

If a scheme is not recognized by the implementation, a fallback parser is allocated and cached. The internal global cache has a capacity limit (512) and will drop all entries if the limit is exceeded.
This is purely a performance optimization. While hostile inputs can manipulate the contents of this cache and force it to resize/clear, the associated performance overhead is negligible and does not exceed the inherent algorithmic complexity associated with parsing the input in the first place.

Schemes are considered to be case-insensitive. The public `string Scheme` property of `Uri` will always return the lowercase variant.

While the implementation tries to always return the same `string` instance for a given scheme, it does not guarantee this.
```c#
// BAD
if (Uri.TryCreate(input, UriKind.Absolute, out Uri? uri) &&
    ReferenceEquals(uri.Scheme, Uri.UriSchemeWss))
```
While the above example would work for `wss` in practice, it won't for `UriSchemeData` or some other arbitrary scheme.

### User info

If an `@` is present before the next delimiter (`?`/`#`/`/`/`\`), everything between the scheme and the `@` is considered as `UserInfo`.
This may contain arbitrary characters (including other reserved characters, escaped sequences, new lines).

`Uri` parsing does not attempt to distinguish between the username/password, the entire `UserInfo` is treated as a combined blob.

Querying the `UserInfo` property will return the escaped form of the input.

```c#
new Uri("http://\r:\n:![%20@host").UserInfo
// %0D:%0A:![%20
```

### Host

Uri supports IPv4, IPv6, Dns, and "Basic" host name types.

- The strictness of `IPv4` validation depends on the scheme. `http` allows the use of more relaxed patterns (e.g. 1-3 segments instead of 4, octal base) whereas a canonical format is enforced for custom schemes
    ```c#
    new Uri("http://127.0.070/").Host           // 127.0.0.56
    new Uri("http://127.0.070/").HostNameType   // IPv4
    new Uri("custom://127.0.070/").Host         // 127.0.070
    new Uri("custom://127.0.070/").HostNameType // Dns
    ```
- `IPv6` requires that the input be wrapped in brackets `[` `]`. As of .NET 11, additional restrictions were introduced on the format:
    - The address must be followed by a valid delimiter (e.g. `"http://[::]extra"` is no longer considered valid)
    - Length prefixes are no longer allowed (e.g. the `/64` in `"http://[AB::/64]/path"`)
    - Scope ID characters must be `%` or unreserved (e.g. `"http://[::%abc?def]"` is invalid because of the `?`)
- `Dns` performs some validation on the individual labels
    ```c#
    new Uri($"http://{new string('a', 63)}.def").HostNameType // Dns
    new Uri($"http://{new string('a', 64)}.def").HostNameType // Basic
    ```
- `Basic` is reported as a fallback when not matched by `Dns`. Internally it tries to parse the host as `Unc`, which still enforces restrictions on the allowed characters (letters / digits / `-` / `_` / `.`).

A custom parser may opt-in to more relaxed host parsing via `GenericUriParserOptions.GenericAuthority`.

`Uri` provides three separate `Host` properties:
- `Host`
    - Returns the escaped input. Non-ASCII hosts are returned as-is. IPv6 hosts include the brackets but not the scope.
- `IdnHost`
    - Non-ASCII hosts are punycode encoded, IPv6 hosts don't include brackets but include the scope.
- `DnsSafeHost`
    - Behaves like `IdnHost` for IPv6 and Basic hosts, and like `Host` for Dns ones. More of a historical artifact from before `IdnHost` was added.

Which one should be used depends on the use case. Typically either Host/IdnHost is used depending on whether you want the IPv6 brackets, scope ID, or punycode encoding.

### Port

```c#
public int Port
```

Parsing allows only ASCII digits. Leading `0`s are allowed in the input, but removed when normalized. A port outside the [0, 65535] range is invalid and will be rejected during construction.

```c#
new Uri("http://host:00123").Port // 123
```

The scheme (parser) may define a default port value. If one is not known, `uri.Port` will return `-1`.
```c#
new Uri("https://host").Port // 443
new Uri("idk://host").Port // -1
```

If the default port is explicitly specified in the input, it is stripped during normalization.
```c#
new Uri("http://host:080").AbsoluteUri // http://host/
```

### Path / Query / Fragment

Any arbitrary set of characters is considered valid for the Path / Query / Fragment.

The components are split by the `?` and `#` delimiters.
In rare circumstances it is possible for the path or query to contain future delimiters (e.g. `Query` may contain `#`) if `DangerousDisablePathAndQueryCanonicalization` is used, or a custom parser specified the `GenericUriParserOptions.NoQuery / NoFragment` flag.

The RFC gives the following example:  
```
  foo://example.com:8042/over/there?name=ferret#nose
  \_/   \______________/\_________/ \_________/ \__/
   |           |            |            |        |
scheme     authority       path        query   fragment
```
`Uri` properties such as `Query`, `PathAndQuery`, `Fragment` DO include the delimiters if a component exists (`?abc` will be returned instead of `abc`).

Querying these properties will return the escaped form of the input.

## GetComponents

```c#
public string GetComponents(UriComponents components, UriFormat format)
// UriComponents.Scheme, UserInfo, Host, Port, ...
// UriFormat.UriEscaped, Unescaped, SafeUnescaped
```

`GetComponents` is a core helper for extracting a string from an arbitrary set of components.

Other than slight differences in edge-cases, public properties are mostly accelerators over this helper, sometimes with extra caching.  
E.g. `string Authority` is implemented as `GetComponents(UriComponents.Host | UriComponents.Port, UriFormat.UriEscaped)`.

- `UriFormat.UriEscaped` is the option that should almost always be used, and is what properties such as `AbsolutePath`, `Query`, `AbsoluteUri` return.
- `UriFormat.Unescaped` can be misleading as it will unescape everything, including meaningful reserved characters, and the output may be interpreted as multiple other components.
- `UriFormat.SafeUnescaped` unescapes, but avoids unescaping various delimiters and reserved characters.

Consider the following case where the encoded `@` is kept escaped as part of the user info when using `SafeUnescaped`:
```c#
string name = Uri.EscapeDataString("admin@foo? a");
var uri = new Uri($"http://{name}@host/path");

// http://admin%40foo%3F%20a@host/path
Console.WriteLine(uri.GetComponents(UriComponents.AbsoluteUri, UriFormat.UriEscaped));

// http://admin%40foo%3F a@host/path (note that the space was unescaped)
Console.WriteLine(uri.GetComponents(UriComponents.AbsoluteUri, UriFormat.SafeUnescaped));

// http://admin@foo? a@host/path (completely different meaning)
Console.WriteLine(uri.GetComponents(UriComponents.AbsoluteUri, UriFormat.Unescaped));
```

Normalization ("canonicalization") is performed on the input.
E.g. `"HTTP://host:080?foo"` will become `"http://host/?foo"`. Note the scheme casing, removed default port, and the inserted `/` for the path.

## Non-ASCII

If a non-ASCII character or an escaped non-reserved character appear anywhere in the input string, Uri will eagerly perform normalization and the whole input will be parsed during construction.

Handling of non-ASCII inputs adds notable complexity to the Uri implementation, as the internal string representation is replaced if they are present.

Escaped non-reserved characters are unescaped (e.g. `%41` => `A`).

Non-ASCII characters will be escaped or unescaped according to IRI rules (see [RFC 3987](https://datatracker.ietf.org/doc/html/rfc3987#section-2.2)).
```
// RFC 3986 - Uniform Resource Identifier (URI): Generic Syntax
query     = *( pchar / "/" / "?" )
fragment  = *( pchar / "/" / "?" )

// RFC 3987 - Internationalized Resource Identifiers (IRIs)
iquery    = *( ipchar / iprivate / "/" / "?" )
ifragment = *( ipchar / "/" / "?" )
ucschar   = %xA0-D7FF / [more ranges omitted] / %xE1000-EFFFD
iprivate  = %xE000-F8FF / %xF0000-FFFFD / %x100000-10FFFD
```
Note that characters in the private ranges may only appear in a query string.

In practice this affects the behavior of `UriFormat.SafeUnescaped`/`ToString()` for these components. If a character from the private range appears outside of the query, it will be/remain escaped.

See also the [Host](#host) section for details about non-ASCII handling there.

## File paths

Uri supports parsing file paths - DOS / UNC / Unix.
They are treated as Absolute Uris.
```c#
new Uri("file://C:/foo").LocalPath // C:\foo
new Uri("file:////C/foo").IsUnc    // True
```

It also (unfortunately) supports parsing "implicit" file paths, where the scheme is not specified.
```c#
new Uri(@"C:\foo").AbsoluteUri     // file:///C:/foo
```
Implicit file paths are also treated as Absolute Uris. The `file:` scheme is implied.

Implicit paths differ from explicit ones in that they may not contain a query/fragment or percent escaped sequences.
```c#
new Uri(@"file://C:\foo%2F?bar").LocalPath // C:\foo\
new Uri(@"C:\foo%2F?bar").LocalPath        // C:\foo%2F?bar
```

Unix file paths are particularly problematic as their support is OS-dependent.
This is the only OS-specific logic in Uri (aside from globalization differences from the underlying platform).
When supported, Unix file paths are treated as Absolute Uris, often causing confusion for developers.
```c#
new Uri("/") // Throws on Windows, works on Unix
new Uri("/", UriKind.RelativeOrAbsolute).IsAbsoluteUri // False on Windows, True on Unix
new Uri("/", UriKind.Relative) // Relative on either OS
```

To help deal with such cases there is an [approved](https://github.com/dotnet/runtime/issues/59099) (but not-yet-implemented) API that allows the user to opt-out of supporting implicit file paths.
```c#
new UriCreationOptions { AllowImplicitFilePaths = false }
```

## Combining absolute and relative Uris & IsBaseOf

Combining base and relative components behaves as [suggested by the RFC](https://datatracker.ietf.org/doc/html/rfc3986#section-5.2.2).

A potential source of confusion/risk for users is the existence of "network-path reference"s. These are relative Uris that start with two slashes, and DO contain an authority (they are relative to the scheme, not the base Uri itself).
```c#
var baseUri = new Uri("https://host/path");
var uri = new Uri(baseUri, "//host2/path2");
Console.WriteLine(uri.AbsoluteUri);       // https://host2/path2
Console.WriteLine(baseUri.IsBaseOf(uri)); // False
```
When combining a trusted base with an untrusted relative url, the `IsBaseOf` helper should be used to check whether the base was modified.
An example of this is given under ["Security considerations"](https://learn.microsoft.com/dotnet/fundamentals/runtime-libraries/system-uri#security-considerations) docs.

```c#
public Uri MakeRelativeUri(Uri uri);
```
The `MakeRelativeUri` performs the inverse - given a base and a target, constructs a relative Uri that is, when combined with the base, equal to the target.

## Path compression

Uri may remove path segments when "compressing" the input that contains [dot segments](https://datatracker.ietf.org/doc/html/rfc3986#section-5.2.4).
E.g.
```c#
new Uri("http://host/first/../second/./").AbsoluteUri
// http://host/second/
```

As of .NET 11 the implementation is based on a two-pass $O(n)$ approach of first determining the ranges (segments) to be removed, then merging together the remaining ones.

When the Uri contains an authority component, the authority is rooted and won't be removed/replaced.
```c#
new Uri("http://host/../foo:88").AbsoluteUri      // http://host/foo:88
new Uri(@"C:\..\D:").AbsoluteUri                  // file:///C:/D:
new Uri(@"\\host\share\..\..\foo:88").AbsoluteUri // file://host/foo:88
```

Note that some segments, such as the UNC share, may still get replaced. If this is a concern, the caller should validate their assumptions by using the `IsBaseOf` helper.
```c#
var baseUri = new Uri(@"\\host\share\");
var uri = new Uri(baseUri, new Uri("../test", UriKind.Relative));
Console.WriteLine(baseUri.IsBaseOf(uri)); // False
```
It is up to the caller to ensure that the base Uri is properly formatted for the `IsBaseOf` call. Consider for example that the `baseUri` in the above example was `\\host\share` instead (note the missing trailing slash). In this case the root is only `\\host\`, and `IsBaseOf` returns `True`.

If the input does not have an authority (or an implied base as with DOS-style paths), path compression is not performed.
```c#
Console.WriteLine(new Uri("custom:test/..").AbsolutePath);     // test/..
Console.WriteLine(new Uri("custom:/test/..").AbsolutePath);    // /test/..
Console.WriteLine(new Uri("custom://test/..").AbsolutePath);   // /
Console.WriteLine(new Uri("custom:///test/..").AbsolutePath);  // /
Console.WriteLine(new Uri("custom:////test/..").AbsolutePath); // //
```

## DangerousDisablePathAndQueryCanonicalization

.NET 6 added a new opt-in API that allows the caller to configure how Uris are constructed.

As of .NET 11, the only option exposed is `DangerousDisablePathAndQueryCanonicalization`, which instructs `Uri` to avoid performing any transformations on the input starting with the path.

Properties such as `AbsolutePath` normally return `string`s in an escaped form. When using this flag, they will instead return a substring of the original input as-is.

As the name suggests, this makes the API dangerous as improper use may break assumptions that other logic consuming Uris is making. It is the caller's responsibility to perform sufficient escaping of the input for their use case when using this flag. Failure to do so may lead to undefined behavior.
For example, SocketsHttpHandler has an expectation that `PathAndQuery` returns an ASCII value that is sufficiently escaped for use on the wire. Improper use may present a vector for HTTP request smuggling.

The flag allows the user to override Uri's default input transformations. E.g. Uri will compress HTTP paths such as `/../`, or unescape unreserved characters ("%41" turns into "A").
The flag can be used with YARP to proxy requests more transparently.

```c#
var options = new UriCreationOptions
{
    DangerousDisablePathAndQueryCanonicalization = true
};

var uri = new Uri("http://host/pa th ?foo=\nbar#abc", options);
Console.WriteLine(uri.AbsolutePath); // "/pa th "
Console.WriteLine(uri.Query);        // "?foo=\nbar#abc"
```

`Uri.Fragment` is always assumed to be empty when this flag is used, so the path or query may contain `#` characters. Any trailing spaces are also considered part of the path/query instead of being trimmed.

To lower the chance of accidental misuse by consumers of such Uri instances, the `uri.GetComponents` API throws when the Path or Query are requested. This is because `GetComponents` accepts a `UriFormat` argument (escaped / unescaped), which may otherwise be misleading in this case.

## Algorithmic complexity & length

Up until .NET 10, Uri inputs were limited to around 65k characters (using `ushort`s internally). There was logic with an $O(n^2)$ worst-case, but mitigated due to the length restriction.

[As of .NET 10, the length limit was lifted completely.](https://learn.microsoft.com/dotnet/core/compatibility/networking/10.0/uri-length-limits-removed) The quadratic logic has been rewritten into a linear pass.
We assume that all Uri operations are sub-quadratic on .NET 10+.

The removed length restrictions mainly apply to paths, queries, and fragments as the most practical components to carry large amounts of data. Components such as the scheme and host may still enforce some limits (e.g. the scheme is currently limited at 1024 characters). Practical limitations as you approach the length limits of `string` also apply, so you can't (nor should you) use Uri to represent a 10 GB file.

While commonly used properties such as `IdnHost` and `PathAndQuery` are cached internally, not all properties are. It is possible for the caller to introduce quadratic complexity by mistake by querying the same property N times. E.g.
```c#
// BAD
for (int i = 0; i < uri.Segments.Length; i++)
    Console.WriteLine(uri.Segments[i]);
```

## Thread safety

All members of `Uri` are documented as thread-safe and may be used concurrently from multiple threads.

The type is not immutable internally as parsing happens on demand. The implementation takes care to properly synchronize any such action, or ensure that any repeated work is idempotent.  
As far as consumers of a Uri instance are concerned, it presents as if it were fully immutable.

Some string-returning properties are cached internally on the `Uri` instance. Which (if any) is considered an implementation detail that can and does change. Uri makes no guarantees that querying a property multiple times will return the same `string` instance (especially when used concurrently), but the results will be equal.

## Custom parsers

Uri has built-in knowledge of several well-known schemes, and may alter parsing behavior based on the scheme. E.g. does it support a query, or whether we should unescape dots and slashes.

A developer can also register a custom parser for a given scheme.
The registration is global for the process.
```c#
UriParser.Register(myParser, "my-scheme", defaultPort: 123);
```

There are built-in parsers that mimic the behavior of well-known schemes
```c#
UriParser.Register(new HttpStyleUriParser(), "my-scheme", defaultPort: 123);
```
or a more generic parser with more configurable options
```c#
new GenericUriParser(
    GenericUriParserOptions.DontConvertPathBackslashes |
    GenericUriParserOptions.NoFragment)
```

Internally these make use of the existing support for configurable parsing based on the scheme that is also used for well-known schemes.

Another option that is practically never used is to register a custom implementation derived from `UriParser` which can bypasses internal parsing logic. That implementation is then responsible for creating `Uri` instances for that scheme, doing the parsing, and returning results by overriding the `GetComponents` functionality.
```c#
class MyParser : UriParser
{
    protected override void InitializeAndValidate(Uri uri, out UriFormatException parsingError) =>
        parsingError = uri.OriginalString.Contains("foo") ? new UriFormatException("Bar") : null;

    protected override string GetComponents(Uri uri, UriComponents components, UriFormat format) =>
        "foo";
}
```

If such a parser is used, it is up to its implementation to perform sufficient validation and escaping. Any assumptions or guarantees made elsewhere in this document may be void  (e.g. parsing complexity).
The custom implementation is also responsible for ensuring its logic in `GetComponents` is safe to call concurrently from multiple threads.

Only one parser can be registered for a given scheme. Registering a parser for one of the built-in schemes is not allowed.  
The set of schemes that are recognized by the implementation is not documented and may change in the future. The current set includes schemes such as http, https, wss, ftp, file, etc.

Uri exposes many static fields for scheme names (`Uri.UriSchemeHttp`, `Uri.UriSchemeData`, ...). While all schemes with built-in parser recognition are currently exposed as such public fields, the inverse does not hold - the presence of the public field does not guarantee that an internal parser exists, or that it's not possible to register a custom parser for said scheme.

Notably `Uri.UriSchemeData` does _not_ currently have any special internal recognition.

See [DataUriParserDemo.cs](https://gist.github.com/MihaZupan/928e380196973d06850484dbedb445e8) for an example of a `DataUri` type that derives from `Uri`, exposes a `ReadOnlySpan<byte> Data` property, and augments (rather than replacing) existing Uri validation.
```c#
var dataUri = new DataUri("data:image/png;base64,aGVsbG8=");
Console.WriteLine(dataUri.Data.Length);
```

## IsWellFormedUriString / IsWellFormedOriginalString

There are two "IsWellFormed" helpers, one instance and one static, where the static just forwards into the instance one.

```c#
public bool IsWellFormedOriginalString();  

public static bool IsWellFormedUriString(string? uriString, UriKind uriKind) =>
    Uri.TryCreate(uriString, uriKind, out Uri? uri) &&
    uri.IsWellFormedOriginalString();
```

Checks if the original input string was already "well-formed" in an opinionated way. As implied by `IsWellFormedUriString`'s implementation, it is a strict subset of inputs that `TryCreate` accepts.

It performs further checks like whether the input was already sufficiently escaped, that an absolute Uri was not an implicit file path, contains backslashes in the path, etc.

It is rarely necessary for this check to be performed as the `Uri` instance is completely usable without it (e.g. for use with HttpClient).
We believe most uses of it are just because it *looks* like a good thing to call, sometimes leading to stricter validation than may be necessary.

For a scheme such as `http`, the assumption is that `uri.AbsoluteUri` returns a string which is itself parseable and well formed (sans [false-negative bugs](https://github.com/dotnet/runtime/issues/72632)).

## Equality

Effectively compares the `GetComponents(Scheme | Host | Port | Path | Query, SafeUnescaped)` of the two inputs.

Note that the user info and fragment are ignored.  
If the inputs are `mailto` schemes, the user info is also compared.

Comparisons are case-sensitive, except if the inputs are UNC or DOS paths.

## Static Uri helpers

### EscapeDataString / UnescapeDataString

These are the helpers that should be used when encoding/decoding query string arguments. This ensures that those arguments cannot "escape" and affect the structure of the url (e.g. insert a fragment, modify a different query argument).
```c#
// BAD
string query = $"?userId={userId}&address={address}";

// GOOD
string query = $"?userId={Uri.EscapeDataString(userId)}&address={Uri.EscapeDataString(address)}";
```

`EscapeDataString` transforms the input by escaping every input character except for those [defined as unreserved by RFC3986](https://datatracker.ietf.org/doc/html/rfc3986#section-2.3).

```
unreserved = ALPHA / DIGIT / "-" / "." / "_" / "~"
```
Non-ASCII characters are escaped by emitting the escaped UTF-8 bytes (e.g. `🍉` turns into `%F0%9F%8D%89`).
Unpaired surrogates are replaced with an escaped replacement character (`%EF%BF%BD`), making this method not fully round-trippable for an arbitrary set of `char`s.

`UnescapeDataString` performs the inverse, decoding any valid escape sequence. Invalid sequences (e.g. invalid hex `%1G`, or invalid escaped UTF-8) are preserved as-is.
We perform a single unescaping pass, so it is possible and intentional that the output may still contain sequences that are themselves escaped (e.g. `%25C3%25BC` => `%C3%BC`, which can be unescaped again to `ü`).

Invalid UTF-8 representations (e.g. `%C0%AF` - invalid encoding of `/`) are not decoded.

All `{Try}{Un}EscapeDataString` helpers run in $O(n)$.

### EscapeUriString

Similar to `EscapeDataString`, but doesn't escape reserved characters.

```c#
public static string EscapeUriString(string stringToEscape) =>
    EscapeString(stringToEscape, noEscape: UnreservedReserved);

public static string EscapeDataString(string stringToEscape) =>
    EscapeString(stringToEscape, noEscape: Unreserved);
```

As practically every usage we could see online was developers formatting query string arguments (where you should use `EscapeDataString` instead!), and it's hard to come up with a compelling usage example where ignoring reserved characters is needed, we marked the method as [marked Obsolete](https://learn.microsoft.com/dotnet/fundamentals/syslib-diagnostics/syslib0013) in .NET 6.

### CheckSchemeName

A simple helper that validates that the input follows the syntax defined by the RFC
```
scheme = ALPHA *( ALPHA / DIGIT / "+" / "-" / "." )
```
but it does not enforce a length limit, so it's therefore possible that Uri parsing will reject some inputs that `CheckSchemeName` returned `true` for.

### CheckHostName

`CheckHostName` returns an enum (IPv4/IPv6/Dns/Unknown).
For IPv6 it will accept inputs both with or without the `[]` braces (unlike "full" parsing that requires them).
`Unknown` is returned for other hosts that don't meet Dns restrictions, but such hosts may still be accepted as part of a full Uri string.  
E.g. for the host `new string('a', 64)`, `Uri.CheckHostName(host)` will return `Unknown`, but `new Uri($"http://{host}").HostNameType` will return `Basic`.  

Due to such differences, `CheckHostName` returning `true` does not guarantee that such a host will be accepted by Uri, but it returning `false` also does not guarantee that it won't be.  
In practice that does hold for common hostnames and IPv4 addresses, so it's a suitable helper for early input validation.

### Miscellaneous helpers

`Uri` also holds several static helpers related to hex processing: `IsHexEncoding`, `IsHexDigit`, `HexEscape`, `HexUnescape`, `FromHex`.
They're simple helpers that do the obvious thing in constant time.

## Use of unsafe code

Uri used excessive amounts of unsafe code (unmanaged pointers) throughout the majority of parsing routines.
While slowly reduced over the years, most of it remained until .NET 11.

As of .NET 11, all such use of pointers has been removed.  
The implementation does still make heavy use of uninitialized memory (`stackalloc` w/ `SkipLocalsInit` & `ArrayPool`), though practically always via helpers that make misuse less likely (`ValueStringBuilder`).  
With the definition of `unsafe` as of writing this document, the library compiles with `/p:AllowUnsafeBlocks=false /p:SkipLocalsInit=false`.

## Fuzzing

[Exists](https://github.com/dotnet/runtime/blob/main/src/libraries/Fuzzing/DotnetFuzzing/Fuzzers/UriFuzzer.cs).
