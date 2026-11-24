---
name: compression-review
description: "Guidance for writing and reviewing compression, archive, and packaging code in dotnet/runtime: Deflate/ZLib/GZip/Brotli/Zstandard streams, ZipArchive/ZipFile (Zip64, encryption, Update mode), System.Formats.Tar, System.IO.Packaging, System.IO.Compression.Native, and vendored zlib-ng/brotli/zstd. For reviews, routes to the @compression-review agent and its references/ checklists. Trigger words: compression, zip, zip64, ziparchive, deflate, gzip, brotli, zstandard, zstd, zlib, tar, pax, packaging."
---

# Writing & Reviewing Compression, Tar & Packaging Code

> **Authoring guide only — not an independent review:** Loading this skill (`skill("compression-review")`) puts authoring rules into your own context; it does **not** satisfy the [author review gate](../code-review/author-review.md). Before finishing a behavioral change, spawn the **`@compression-review`** custom agent in a **fresh read-only sub-agent** (`task` / sub-agent tool) when sub-agents are available.

Use this skill when writing or reviewing code under `src/libraries/System.IO.Compression*`,
`src/libraries/System.Formats.Tar`, `src/libraries/System.IO.Packaging`,
`src/native/libs/System.IO.Compression.Native`, or the vendored libraries under
`src/native/external/` (`zlib-ng`, `brotli`, `zstd`, their `-version.txt` files, and
`cgmanifest.json`). Area rules — upstream-first for vendored codecs, format specifications, and
test conventions — stay in `.github/instructions/compression.instructions.md`,
`.github/instructions/system-formats-tar.instructions.md`, and
`.github/instructions/tests.instructions.md`.

## Reviewing

The [`@compression-review` agent](../../agents/compression-review.agent.md) holds the review
workflow; the checks are in [`references/`](references/):

| Reference | Covers |
|-----------|--------|
| [`ai-and-repo-conventions.md`](references/ai-and-repo-conventions.md) | Diff hygiene, naming, comments, dead code, API docs, exceptions, breaking changes, AI-code pitfalls (always) |
| [`csharp-streams-and-memory-rules.md`](references/csharp-streams-and-memory-rules.md) | Stream contracts, disposal, sync/async parity, allocations, overflow, untrusted-input bounds |
| [`zip-and-packaging-rules.md`](references/zip-and-packaging-rules.md) | ZIP headers, Zip64, central directory, Update mode, encryption, extraction, Packaging |
| [`tar-rules.md`](references/tar-rules.md) | Tar formats, PAX records, reader/writer invariants, extraction |
| [`codecs-and-vendored-native-rules.md`](references/codecs-and-vendored-native-rules.md) | Deflate/Brotli/Zstandard streams, native shim, vendored libraries, CMake |
| [`testing-msbuild-and-assets-rules.md`](references/testing-msbuild-and-assets-rules.md) | Test inputs and fixtures, oracles, test structure, MSBuild/ref |

Invoke the agent as a sub-task when sub-agents are available; tell it to return findings, not
post them. The older `@extensions-reviewer` agent also lists compression dimensions; prefer this
agent for compression, tar, and packaging diffs. In `code-review` reviewer-worker mode, read the
agent file and the matching references as a checklist instead of invoking it.

## Writing compression code

Before finishing, walk the references that match your change as a self-check. The essentials:

- **Follow the bytes:** update every producer and consumer of a format field together — read
  and write paths, sync and async twins, ZIP Create/Read/Update modes, the central directory and
  local headers, tar reader and writer — and keep the format specification open.
- **Untrusted input:** treat archive fields as hostile: check sizes and offsets with
  overflow-safe arithmetic, cap allocations, and keep extraction inside the destination.
- **Streams:** don't assume a read fills the buffer; respect `CanSeek`; keep cursor invariants
  for entries you don't modify; honor `leaveOpen`.
- **Native state:** release or reset native codec state on every exit path and never leave
  native pointers into managed buffers after a call returns.
- **Vendored codecs:** search upstream before patching and prefer an exact backport, as the
  compression instruction file describes.

## Testing

Build test inputs in code first; reuse `*TestData` fixtures from
[dotnet/runtime-assets](https://github.com/dotnet/runtime-assets) rather than committing binary or
Base64 archives (see `.github/instructions/tests.instructions.md`). Compute expected bytes from
the specification, cover finished and unfinished prior operations and boundary sizes, and run
the owning test project as described in [testing libraries](/docs/workflow/testing/libraries/testing.md).
