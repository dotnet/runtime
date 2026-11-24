# TLS & Security Rules

Checks for `System.Net.Security` (`SslStream`, `NegotiateAuthentication`), X.509 handling in networking, the SChannel/OpenSSL/Apple/Android TLS PALs, and credential or key handling anywhere in networking code.

---

## Certificates & Trust

| Check | What to look for |
|-------|-----------------|
| **Never mutate or drop the caller's chain policy** | Code that overwrites or mutates a user-supplied `X509ChainPolicy` (`VerificationTime`, `ApplicationPolicy`, trust mode, `CustomTrustStore`, revocation, download settings), gates `.Clone()` on an optional condition such as `intermediates.Count > 0` (leaving leaf-only sessions sharing/mutating the caller's policy), assigns `chain.ChainPolicy = ...` *after* adding intermediates to `ExtraStore` (wiping them), builds a partial clone that misses fields, or ignores custom-root trust on one platform. Always `.Clone()` caller-supplied policy unconditionally per session/handshake, populate `ExtraStore` after `chain.ChainPolicy` assignment, filter custom root stores to self-signed roots where the platform does not, and dispose `ChainElements`/`ExtraStore` certificates you created. (`#71961`, `#125049`, `#69908`) |
| **Hostname matching follows SAN rules** | Target-host matching that compares IP literals against `dNSName` SAN entries, falls back to the subject CN when a SAN extension exists, or differs between OpenSSL, SChannel, and Apple. Match IP targets only against `iPAddress` SANs, DNS targets only against `dNSName`, ignore CN when SANs are present, and test the full CN/DNS/IP matrix on every PAL. (`#135573`) |

---

## Native TLS PALs

| Check | What to look for |
|-------|-----------------|
| **Drain the OpenSSL error queue around every call** | Calling `SSL_read`/`SSL_write`/`SSL_do_handshake` or other OpenSSL APIs without clearing the thread-local error queue first, or reading only one error and leaving the rest. A stale entry from an earlier call makes a later `SSL_get_error` report a bogus failure on the same thread. `ERR_clear_error()` before the call, drain with `ERR_get_error()` after a failure (logging what was dropped), and gate newer APIs such as `ERR_new` on a runtime version check. (`#64256`, `#65435`, `#67030`) |
| **Release Apple and Android objects with the matching API** | Apple Network.framework/Security objects released with the wrong function (`nw_release` for `nw_*`, `sec_release` for `sec_*` protocol objects, `CFRelease` for CF/`SecTrust` types), released when obtained under the Get rule, or not released when obtained under Create/Copy; Android JNI local refs and arrays leaked or copied unnecessarily. Follow each object's ownership rule exactly and pair every acquire with one release. (`#117016`, `#117428`, `#104726`) |
| **Cache keys include every security-relevant setting** | TLS context, session, or certificate caches keyed only on certificate or host, ignoring `CipherSuitesPolicy`, `EncryptionPolicy`, protocols, ALPN, SNI, or callback identity; evicting shared entries while in use; or OS-specific `#if` blocks leaking out of `SslStreamPal`. A cached context negotiated for one configuration is reused for another. Key on all options that affect negotiation, refcount shared entries, and keep platform code behind the PAL boundary. (`#102656`, `#99371`, `#101684`) |

---

## SslStream & Authentication

| Check | What to look for |
|-------|-----------------|
| **Never lose buffered TLS bytes** | A canceled or interrupted read that has consumed part of a TLS record and then discards it, frame-size parsing that assumes a whole header arrived, unbounded growth of the internal buffer, or throwing `AuthenticationException` vs `IOException` inconsistently. The next read decrypts garbage or hangs. Keep partially read records in the stream's buffer across cancellations, bound buffer growth, and keep the exception contract (`AuthenticationException` for handshake, `IOException` for transport). (`#134260`, `#64322`, `#83480`) |
| **Truncated TLS frames, callback rethrows, SNI `invalid` flag, and `NegotiateAuthentication` QoS** | Forwarding 0-byte or truncated TLS handshake/alert records (`frameSize <= handshakeTypeOffset`) into OS PALs instead of throwing `IOException` directly across all roles/states; wrapping user certificate callback exceptions in `AuthenticationException` instead of flushing generated TLS alerts and rethrowing via `ExceptionDispatchInfo.Throw`; setting `TlsFrameHelper.TryGetFrameInfo` `out bool invalid = true` on semantically ignored extensions (`>255`-byte or invalid-IDN SNI) instead of leaving `invalid = false` with `TargetName = null`; or allowing `NegotiateAuthentication` to succeed when `RequireMutualAuthentication` (or required protection) was requested and the negotiated package (such as NTLM) has `!IsMutuallyAuthenticated` instead of failing with `NegotiateAuthenticationStatusCode.SecurityQosFailed`. |
| **TLS session state transitions are explicit** | `TlsSession`/`TlsContext` code that reuses an options object after mutation, re-runs certificate validation, continues after a `Closed` status, or picks a certificate from a one-element collection differently than from a single certificate. Snapshot options at context creation, track validation state once, and make terminal states final. (`#130366`, `#133159`, `#80182`) |
| **Negotiate, NTLM, and GSSAPI edge cases** | SPN parsing that mishandles `user@realm`, fallback to `UnknownCredentials` or managed NTLM when explicit credentials were given, `\|\|` vs `&&` mistakes when checking username and password, dynamic decoding of fields that must be read linearly, or GSSAPI handles not released on Unix. Decide fallbacks from the actual credentials, parse protocol messages strictly in order, and release every GSS buffer and name. (`#131195`, `#91160`, `#66879`) |
| **Credential and key hygiene** | New uses of SHA-1 or non-PFS cipher suites, key logging or secret-export features without a "dangerous" name or opt-in, key material left in managed arrays or native memory after use, or "clearing" that does not actually free or zero. Use approved algorithms, gate secret export behind explicit opt-in, zero secrets with `CryptographicOperations.ZeroMemory` before freeing, and strip auth headers when a request crosses a trust boundary. (`#102656`, `#83001`, `#100665`) |
