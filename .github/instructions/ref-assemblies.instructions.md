---
applyTo: "src/libraries/**/ref/**"
---

# Reference Assemblies (`ref/`) — Public API Contract Guidance

Reference assembly sources under `src/libraries/<Lib>/ref/` define the exact public compile-time surface and are validated automatically against `src/` by `ApiCompat` whenever `src/<Lib>.csproj` builds.

## Editing `ref/*.cs` Cleanly

- **Edit `ref/<Lib>.cs` directly by hand in alphabetical/type order** matching the file's existing style (`#nullable enable`, fully-qualified base/parameter/return types where surrounding declarations use them, `{ throw null; }` / `{ get { throw null; } set { } }`, and exact approved nullability annotations).
- **Avoid running `/t:GenerateReferenceAssemblySource` on hand-maintained nullable `ref/*.cs` files** unless specifically regenerating a full facade: GenAPI frequently rewrites unrelated lines, strips `#nullable` annotations, or drops platform attributes, causing `CS8768`/`CS8769` nullability drift. Let `ApiCompat` during `./dotnet.sh build --no-restore src/libraries/<Lib>/src/<Lib>.csproj /p:RuntimeConfiguration=Release` verify 100% contract parity between `ref/` and `src/`.
- **Cross-assembly type references:** When a new public signature in `ref/<Lib>.cs` exposes a type defined in another assembly (for example `CompressionLevel`, `ZstandardCompressionOptions`, or `SslClientAuthenticationOptions`), check `ref/<Lib>.csproj` and add `<ProjectReference Include="$(LibrariesProjectRoot)<Dependency>\ref\<Dependency>.csproj" />` if it is not already referenced.

## Platform Guard Parity (`CA1416`)

- **Check `[UnsupportedOSPlatform]` / `[SupportedOSPlatform]` on every underlying runtime type your new API wraps or delegates to** (for example `BrotliStream`/`BrotliEncoder` or `ZstandardStream` on `browser`/`wasi`, or OS-specific TLS/crypto/socket APIs). Inspect `src/libraries/<Dependency>/ref/*.cs` for platform attributes on the underlying types before finalizing your public surface.
- **Mirror matching `[UnsupportedOSPlatform]` / `[SupportedOSPlatform]` attributes on BOTH `ref/<Lib>.cs` and `src/`** (or guard platform-unsupported code paths as appropriate for the approved API design).
- **Fast inner-loop `-f <host-tfm>` + mandatory pre-review all-TFM verification:**
  - While `./build.sh clr+libs -rc release` is running, stage `ref/` and `src/` edits under `artifacts/staged/<repo-rel-path>` rather than modifying `ref/`/`src/` in-tree, then promote (`cp -R artifacts/staged/* . && rm -rf artifacts/staged`) once `./build.sh` exits `0`.
  - During iterative test runs, always build `ref/<Lib>.csproj` first (~3s), then build `src/<Lib>.csproj` **with `-f <host-tfm>`** (~4–8s, picking the most specific entry listed in `<TargetFrameworks>` of `src/<Lib>.csproj`: `<tfm>-osx`/`<tfm>-linux` → `<tfm>-unix` → `<tfm>`) so `testhost` updates rapidly:
    ```bash
    ./dotnet.sh build --no-restore src/libraries/<Lib>/ref/<Lib>.csproj /p:RuntimeConfiguration=Release
    ./dotnet.sh build --no-restore src/libraries/<Lib>/src/<Lib>.csproj /p:RuntimeConfiguration=Release -f <host-tfm>
    ```
  - Right before the author review gate (and again after any post-review edit to `ref/` or `src/`), run **one** full multi-TFM `src/<Lib>.csproj` build **without `-f`**:
    ```bash
    ./dotnet.sh build --no-restore src/libraries/<Lib>/src/<Lib>.csproj /p:RuntimeConfiguration=Release
    ```
    Building `src/<Lib>.csproj` with `-f <host-tfm>` skips `browser`/`wasi`/`windows`/`mobile` configurations during inner-loop iteration; the single pre-review no-`-f` build catches `CA1416`, trimming, and cross-OS `ApiCompat` mismatches in seconds.
