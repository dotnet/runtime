# TlsHandshakeBench

Ad-hoc handshake benchmark comparing `SslStream` (baseline) against `TlsSession` in
both buffered and fd-bound modes. Runs against a real loopback TCP socket so I/O
transitions are exercised.

> This project is **temporary** — eventually it will move to `dotnet/performance`.
> It is not part of the regular libraries build (not referenced from any solution
> or test list).

## Run

The benchmark depends on the live-built `System.Net.Security` (TlsSession is not
in any shipped runtime), so it must run against the in-tree testhost rather than
the SDK's bundled runtime.

```bash
# 1) Baseline build (required once; populates a release-ish runtime layout).
./build.sh clr+libs+libs.pretest -rc release

# 2) Build the benchmark.
./dotnet.sh build -c Release \
    src/libraries/System.Net.Security/perf/TlsHandshakeBench/TlsHandshakeBench.csproj

# 3) Run with the local testhost.
artifacts/bin/testhost/net11.0-linux-Release-x64/dotnet \
    artifacts/bin/TlsHandshakeBench/Release/net11.0/TlsHandshakeBench.dll --filter '*'
```

If the release testhost layout is incomplete (missing `dotnet` host binary or
native libs), fall back to the debug testhost — relative comparisons remain
useful even though absolute numbers will be slower:

```bash
artifacts/bin/testhost/net11.0-linux-Debug-x64/dotnet \
    artifacts/bin/TlsHandshakeBench/Release/net11.0/TlsHandshakeBench.dll --filter '*'
```

The benchmark uses `InProcessEmitToolchain` so no child processes are spawned —
BenchmarkDotNet doesn't recognize the `net11.0` runtime moniker yet, and
in-process is fine for I/O-dominated workloads.

To run a single mode:

```bash
... TlsHandshakeBench.dll --filter '*TlsSession_Fd*'
```

## What's measured

Each benchmark performs one full TLS handshake over a fresh loopback TCP connection
(connect + accept + handshake + dispose). Three engines:

- **SslStream_Server** — baseline; `SslStream` on both ends.
- **TlsSession_Buffered_Server** — `TlsSession` on the server, driving I/O through
  the managed `ProcessHandshake` / `DrainPendingOutput` buffer loop.
- **TlsSession_Fd_Server** — `TlsSession.Create(ctx, socketHandle)`; OpenSSL drives
  ciphertext I/O directly via `SSL_set_fd`. (Linux/FreeBSD only.)

Parameterized over `Tls12` and `Tls13`.

`TlsContext` is allocated once in `[GlobalSetup]` and reused across iterations so
per-session cost is measured (not SSL_CTX allocation).
