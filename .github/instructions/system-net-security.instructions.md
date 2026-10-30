---
applyTo: "src/libraries/System.Net.Security/**"
---

# System.Net.Security — Folder-Specific Guidance

## SslStream Lifecycle

- SslStream wraps an inner stream — disposal must flush pending data, send TLS close_notify, and dispose the inner stream if owned
- AuthenticateAsClientAsync/AuthenticateAsServerAsync must not be called more than once; guard against re-authentication on an already-authenticated stream
- SslStream read/write operations must handle TLS record boundaries — a single TLS record may span multiple inner stream reads
- Renegotiation (TLS 1.2) and post-handshake auth (TLS 1.3) have different flows — do not conflate them

## Certificate Validation

- The default certificate validation callback must enforce chain trust and hostname matching — never bypass validation silently
- Custom RemoteCertificateValidationCallback must receive the full chain and SslPolicyErrors so callers can make informed decisions
- Certificate selection (LocalCertificateSelectionCallback) must handle cases where no matching certificate is available
- Client certificate authentication requires that the private key is accessible — handle CryptographicException gracefully

## TLS Version and Cipher Handling

- Default to the highest mutually supported TLS version — do not hardcode a specific version
- SslProtocols.None means "let the OS decide" and is the preferred default
- ALPN negotiation results must be checked after handshake — a mismatched protocol should fail early, not produce corrupt data
- When testing TLS behavior, cover minimum version enforcement and disabled protocol rejection

## Cross-Platform PAL (SChannel / OpenSSL / Apple Security)

- SChannel (Windows), OpenSSL (Linux), and Apple Security (macOS) have different API surfaces — changes to the managed layer must be tested on all three
- OpenSSL context caching (SslContexts) must handle concurrent access and expiration correctly
- Certificate store access differs per platform — Windows uses the system store, Linux uses file-based stores, macOS uses Keychain
- Native TLS errors must be mapped to AuthenticationException with platform-specific inner exceptions preserved

## Credential and Session Caching

- TLS session resumption state must not leak across security boundaries (different hosts or certificate requirements)
- CredentialCache and credential lifecycle must be managed carefully — stale credentials cause silent authentication failures
- Do not cache negotiated security parameters beyond the lifetime of the owning SslStream

## Performance

- Avoid allocating during the TLS read/write hot path — pre-allocate TLS record buffers
- Minimize managed-to-native transitions during bulk data transfer by batching reads/writes at the TLS record level
- Handshake allocations are acceptable — optimize the steady-state data transfer path

## Security Defaults

- Never log private keys, session tickets, or pre-master secrets in any trace or diagnostic output
- Certificate pinning or validation bypass must require explicit opt-in — secure defaults are non-negotiable
- Ensure downgrade attacks (forcing weaker TLS versions) are prevented by the default configuration
