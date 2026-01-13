# RuntimeJsonPruner

A tool to prune `runtime.json` to align with the RID simplification design and host RID algorithm.

## Purpose

This tool simplifies the RID graph in `src/libraries/Microsoft.NETCore.Platforms/src/runtime.json` by removing distro-specific and versioned RIDs while preserving portable RIDs and those used by the host algorithm.

See the [RID Simplification Design](https://github.com/dotnet/designs/blob/c2712085c21a17bc7db46a9d51f0e8e0299834d2/accepted/2022/simplify-rid-model.md) for background.

## What It Does

### Keeps:
- All portable RIDs: `win`, `unix`, `linux`, `osx`
- All Windows RIDs: `win-*`, `win.*` (for backward compatibility)
- Host algorithm RIDs:
  - Windows: `win-x86`, `win-x64`, `win-arm64`
  - macOS: `osx-x64`, `osx-arm64`
  - Linux glibc: `linux-x86`, `linux-x64`, `linux-arm`, `linux-arm64`
  - Linux musl: `linux-musl-x64`, `linux-musl-arm64`, etc.
  - Linux bionic: `linux-bionic-x86`, `linux-bionic-x64`, `linux-bionic-arm`, `linux-bionic-arm64`
- Portable Linux architectures: `linux-s390x`, `linux-ppc64le`, `linux-riscv64`, `linux-loongarch64`
- Unversioned non-Linux Unix RIDs: `freebsd`, `freebsd-x64`, `illumos`, `illumos-x64`, etc.

### Removes:
- All Alpine RIDs: `alpine`, `alpine-*`, `alpine.*`
- All Linux distro-specific RIDs: `ubuntu`, `debian`, `centos`, `rhel`, `fedora`, `sles`, etc.
- Versioned Apple RIDs: `osx.10.15-x64`, `ios.15-arm64`, etc.
- Versioned non-Linux Unix RIDs: `freebsd.12-x64`, `illumos.11-x64`, etc.

### Post-processing:
- Cleans up `#import` arrays to remove references to deleted RIDs

## Usage

```bash
# From the repo root (default path)
dotnet run --project src/tools/RuntimeJsonPruner/src/RuntimeJsonPruner.csproj

# With a custom path
dotnet run --project src/tools/RuntimeJsonPruner/src/RuntimeJsonPruner.csproj -- path/to/runtime.json
```

## Validation

To verify the pruning was done correctly:

```bash
bash src/tools/RuntimeJsonPruner/validate-runtime-json.sh
```

This checks that:
- No Alpine RIDs remain
- No Linux distro-specific RIDs remain
- No versioned OSX or FreeBSD RIDs remain
- All required host RIDs are present

## Results

Running the tool reduces `runtime.json` from ~800 RIDs to ~260 RIDs, removing:
- 112 Alpine RIDs
- 101 Ubuntu RIDs
- 54 Fedora RIDs
- 36 Debian RIDs
- 33 RHEL RIDs
- 30 versioned OSX RIDs
- And other distro-specific and versioned RIDs

This is the first step toward aligning the static RID graph with the algorithmic RID model used by the host.
