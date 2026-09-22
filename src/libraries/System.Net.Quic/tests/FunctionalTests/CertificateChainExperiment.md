# Loopback certificate-chain experiment

This is a diagnostic experiment for [#133645](https://github.com/dotnet/runtime/issues/133645), not a proposed timeout increase or a verified fix.

The recorded Windows/Schannel failures did not complete connection establishment. Both a managed handshake deadline and native `ConnectionIdle` were observed. In the recorded MsQuic revision, the native idle timer also uses `HandshakeIdleTimeoutMs` before connection establishment. The inactivity message does not establish an established-connection idle-timeout failure.

Missing explicitly supplied issuers and enabled AIA downloads are a possible source of handshake delay, not evidence that a download occurred. Why IPv4 with the mismatching DNS name fails while the other three rows pass is still unknown.

## Arms and invariants

`ConnectWithCertificateForLoopbackIP_ChainProvisioningExperiment` runs these arms:

| Arm | Change from the original setup |
|---|---|
| `Original` | None, apart from diagnostics |
| `Server` | Construct an offline server certificate context with the generated issuer chain before connecting |
| `Client` | Supply the issuer chain through client `ExtraStore`, disable certificate downloads, explicitly retain `NoCheck` |
| `Both` | Combine the two interventions |

All four IP/name combinations retain `TargetHost="localhost"`, both loopback IP SANs, the subject/issuer/name-mismatch assertions, and the accepting validation callback. The generated root is not trusted. The ten-second handshake deadline is unchanged.

The mismatching certificate's DNS SAN is `badhost`, but it still contains both IP SANs. Clearing `TargetHost` would remove the intended DNS mismatch, not fix the timeout. On Windows, the generated `badhost` and `localhost` certificates also select different key algorithms; compare the IPv6 mismatch control before attributing failure to either name validation or the algorithm.

On Windows, constructing the server certificate context can populate the Intermediate CA store, which can affect subsequent client chain building. The `Server` arm is not a perfectly isolated server-only treatment. Use fresh generated PKIs, record responder activity, and do not clear shared machine certificate stores or caches.

## Running

Follow the repository's normal libraries baseline build instructions first. On Windows use a short checkout path, for example an unused subst drive. Run from the repository root:

```powershell
$project = 'src\libraries\System.Net.Quic\tests\FunctionalTests\System.Net.Quic.Functional.Tests.csproj'
$env:TRACE_REVOCATION_RESPONSE = '1'
$env:DOTNET_TEST_QUIC_CERTIFICATE_ARM = 'Client'
$env:DOTNET_TEST_QUIC_CERTIFICATE_CASE = 'IPv4Mismatch'
.\dotnet.cmd build $project /t:Test /p:TestScope=outerloop /p:XunitMethodName=System.Net.Quic.Tests.MsQuicTests.ConnectWithCertificateForLoopbackIP_ChainProvisioningExperiment
```

Arm values are `Original`, `Server`, `Client`, and `Both`. Case values are `IPv4Mismatch`, `IPv6Mismatch`, `IPv4Match`, and `IPv6Match`. Unset selectors run all arms/cases; invalid values fail rather than silently selecting a different experiment.

Use separate invocations for individual rows and permute those invocations to investigate ordering. A fresh process is not a cold OS certificate cache. First match the recorded Windows 11 build 26100 x64 / Schannel / MsQuic `2.5.10.154561281` (`9ff06b71fd4b4d5258361598ada5b24cbc1beb20`) environment, then compare ordinary and tiered JIT-stress 1/2 runs. The suite prints the loaded native version and backend.

Run `LoopbackCertificateChain_BuildExperiment` separately to measure `X509Chain.Build` with original versus explicitly supplied offline issuers. It uses the same certificate-generation seed as the handshake test, but a new PKI. Do not run this probe before a supposedly cold handshake and assume it has no cache effects. This probe does not measure the chain build inside QUIC.

The experiment records elapsed setup/connect/callback/disposal phases, callback count, certificate algorithm, responder URI, and connection identities. The existing networking event listener records QUIC/security/HTTP-listener events with timestamps. `TRACE_REVOCATION_RESPONSE` records AIA `/cert/` requests separately from CRL and OCSP requests in the console; preserve both console output and the xUnit XML, including passing output.

Diagnostics affect scheduling. Compare with the original `ConnectWithCertificateForLoopbackIP_IndicatesExpectedError` test, which retains its uninstrumented configuration.

## Interpretation and remaining controls

Do not call an arm a fix merely because it passes locally. Correlate the connection identities and responder URI, and compare standalone chain-build latency with the handshake phase timings. Callback entry is *after* chain building, not a measurement of its start. CAPI2/Schannel or additional targeted instrumentation is needed to locate a natural stall within chain building versus native credential setup or managed scheduling.

Before selecting a fix:

- Require observed AIA activity/chain latency in the original when claiming discovery caused the stall. If the chain build is short and there are no requests, reject that explanation.
- In an isolated diagnostic run, delay only AIA responses using the responder's existing delay hooks. Separately delay options/validation scheduling. Offline provisioning must not hide a genuine non-certificate handshake timeout.
- Keep the exact name-mismatch assertions; inverted expected-name and explicit certificate-rejection controls must still fail.
- Run all four variants in separate processes and varied order. Choose the smallest supported intervention, combining arms only when the evidence warrants it.
- Retain timeout/cancellation/rejection coverage and inspect unobserved exceptions. A suite-level late certificate-validation completion on a disposed native handle has been observed, but it is not identity-correlated to this failing test and is not fixed by this experiment.

Remove the experiment selectors after testing:

```powershell
Remove-Item Env:DOTNET_TEST_QUIC_CERTIFICATE_ARM -ErrorAction SilentlyContinue
Remove-Item Env:DOTNET_TEST_QUIC_CERTIFICATE_CASE -ErrorAction SilentlyContinue
Remove-Item Env:TRACE_REVOCATION_RESPONSE -ErrorAction SilentlyContinue
```
