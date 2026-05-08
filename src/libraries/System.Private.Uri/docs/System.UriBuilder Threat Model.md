# System.UriBuilder

Applies to .NET 11 and newer.
The validation/escaping performed by earlier .NET versions (including Framework) is less strict or non-existent.

```c#
public class UriBuilder
{
    public UriBuilder();
    public UriBuilder(string uri);
    public UriBuilder(string? schemeName, string? hostName);
    public UriBuilder(string? scheme, string? host, int portNumber);
    public UriBuilder(string? scheme, string? host, int port, string? pathValue);
    public UriBuilder(string? scheme, string? host, int port, string? path, string? extraValue);
    public UriBuilder(Uri uri);

    public string Scheme { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
    public string Host { get; set; }
    public int Port { get; set; }
    public string Path { get; set; }
    public string Query { get; set; }
    public string Fragment { get; set; }

    public Uri Uri { get; }
    public override string ToString();
}
```

`UriBuilder` is a glorified `StringBuilder`, a helper type that makes it easier to construct and modify Uri instances.

Example use:
```c#
public Uri ReplacePort(Uri uri, int newPort) =>
    new UriBuilder(uri) { Port = newPort }.Uri;
```

Its primary function w.r.t security is performing minimal input encoding to prevent most components from leaking into others. E.g. setting the UserName should not result in the Host being overwritten.
We do this on a best effort basis.

## Constructors

Unless the component is specified as an argument, `UriBuilder` assumes some defaults: `http` scheme, `localhost` host, no port, and empty path/query/fragment.

Argument validation is the same as when properties are set individually.
If an existing `Uri` instance is specified, properties are populated from that instance.

The `extraValue` argument (when non-empty) must start with a `?` or `#`, or an exception is thrown. The value is split based on these delimiters.

## Properties

`UriBuilder` exposes settable properties for every component.

The order in which properties are set does **not** matter. Property setters may perform some input validation. Whatever validation is performed is specific to the given component and does not take other components into account.  
Notably this means that the `Scheme` does not affect how other parts of the Uri are validated.

If a property setter does not throw, that does **not** guarantee that the final constructed `Uri` will be valid. The full validation is only performed when constructing the final `Uri` instance.

### Scheme

The input is validated via `Uri.CheckSchemeName`. If it's not a valid scheme, `UriBuilder` will look for the first `:` and attempt to interpret the prefix as a scheme.

```c#
var builder = new UriBuilder();
builder.Scheme = "https://example.com";
Console.WriteLine(builder.ToString()); // https://localhost/
```

The lowercase form is stored.

### UserName and Password

The input is stored as-is.
`UriBuilder.ToString` will perform some escaping of these inputs to prevent them from escaping into the authority, but a username may still leak into the password.

### Host

Non-empty values are checked for presence of any characters that may result in the value escaping past the host component (`:/\?#@[]`).

If a value contains a `:`, it is assumed to be an IPv6 address. Leading and trailing brackets are added if missing. The contents of the IPv6 address are validated not to contain any of the previously mentioned characters.

The setter does not guarantee that the value is actually a valid host / IP address.

### Port

Validated to be in the `[-1, 65535]` range.
If set to `-1`, the port is omitted from the `ToString`.

### Path

When set to null/empty, it's set to a single slash.
Non-empty values are partially escaped (can't escape into the Query/Fragment).

### Query and Fragment

When not empty, a `?`/`#` is prepended if the delimiter does not already exist.

## ToString

`ToString` is responsible for concatenating all of the individual components.
It does **not** construct the `Uri`, and does **not** check whether such a `Uri` can be constructed with the returned string.

If the `Password` is set but `UserName` is not, an exception is thrown.

If a non-empty scheme is set, it is used to determine whether the Uri may contain an authority. This is determined based on information built into Uri for well-known schemes, or by flags reported by a custom registered parser.

> [!WARNING]
> If the Scheme is set to empty/null, other components will no longer behave as expected once the `Uri` is constructed as the parser will treat the start of the next component as the possible scheme.

The UserName and Password are appended with the `/\?#@` characters percent escaped.

Other components are appended as-is.

## Uri

When the `Uri` property is accessed, the returned instance is constructed as `new Uri(ToString())`.

All properties are repopulated from the `Uri` instance and may change.

```c#
var builder = new UriBuilder
{
    UserName = "hello:world", Password = "secret", Port = -1
};

Console.WriteLine(builder.UserName); // hello:world
Console.WriteLine(builder.Password); // secret
Console.WriteLine(builder.Port);     // -1

_ = builder.Uri; // Properties have changed

Console.WriteLine(builder.UserName); // hello
Console.WriteLine(builder.Password); // world:secret
Console.WriteLine(builder.Port);     // 80
```

## Thread safety

Unlike `Uri`, `UriBuilder` is clearly mutable, and therefore **not** safe to use concurrently.
