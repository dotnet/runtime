---
applyTo: "src/libraries/System.Net.Primitives/**,src/libraries/System.Net.NameResolution/**,src/libraries/System.Net.NetworkInformation/**,src/libraries/System.Net.Ping/**,src/libraries/System.Net.WebSockets/**,src/libraries/System.Net.WebSockets.Client/**,src/libraries/System.Net.Mail/**,src/libraries/System.Net.Requests/**"
---

# System.Net Common Libraries — Folder-Specific Guidance

## Cross-Platform PAL

- Each library may have Windows, Unix, and macOS implementations — changes to one PAL must be evaluated for all platforms
- Use platform-abstraction layers (e.g., SocketPal, NetworkInterfacePal) rather than #if directives in shared code
- Platform-specific files follow the naming convention `*.Windows.cs`, `*.Unix.cs`, `*.OSX.cs` — place new platform code in the correct file

## DNS and Name Resolution

- DNS resolution results may be cached — ensure cache invalidation logic handles TTL and negative caching correctly
- Dns.GetHostEntryAsync must handle both forward (hostname → IP) and reverse (IP → hostname) lookups
- DNS failures must produce SocketException with the correct SocketError (HostNotFound, TryAgain, NoData)
- Test DNS behavior with both IPv4 and IPv6 addresses, including IPv6 scope IDs

## IP Address and Endpoint Handling

- IPAddress.Parse and TryParse must handle all valid formats: dotted-decimal, IPv6, IPv4-mapped IPv6, and scoped addresses
- Avoid allocating when parsing well-formed addresses on hot paths — use Span-based TryFormat/TryParse overloads
- IPEndPoint equality comparisons must account for scope ID differences in IPv6 addresses

## URI Handling

- URI parsing changes must preserve backward compatibility — many callers depend on current escaping and normalization behavior
- Handle internationalized domain names (IDN) correctly through Punycode encoding
- Percent-encoding must round-trip correctly — do not double-encode or decode reserved characters

## WebSocket Specifics

- WebSocket connection upgrade must validate the HTTP 101 response including Sec-WebSocket-Accept header
- WebSocket framing (mask, opcode, payload length) must follow RFC 6455 exactly
- WebSocket close handshake requires sending and receiving Close frames — track close state to avoid double-close
- Dispose must abort the underlying connection if the close handshake has not completed

## Legacy API Compatibility (HttpWebRequest, SmtpClient)

- HttpWebRequest and SmtpClient are legacy APIs — fixes should maintain behavioral compatibility, not modernize the API surface
- HttpWebRequest delegates to HttpClient internally — changes must not break this delegation layer
- SmtpClient changes must handle both synchronous Send and async SendMailAsync code paths

## Ping

- Ping uses raw sockets on Unix and ICMP APIs on Windows — test on all platforms
- Ping timeout and TTL handling differs per platform — ensure consistent behavior at the managed API level

## Diagnostics

- Use NetEventSource for tracing in all System.Net libraries — maintain consistent event IDs across libraries
- Never log personally identifiable information, email content, or credentials
