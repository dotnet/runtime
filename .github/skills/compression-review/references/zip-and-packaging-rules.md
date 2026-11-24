# ZIP and Packaging Review Rules

Rules for `ZipArchive`, `ZipArchiveEntry`, `ZipFile`/`ZipFileExtensions`, WinZip AES/ZipCrypto, and `System.IO.Packaging` in `src/libraries/System.IO.Compression*` and `src/libraries/System.IO.Packaging`.

---

## Archive Structure

| Check | What to look for |
|-------|-----------------|
| **Zip64 sentinels are per field** | Only fields that actually overflowed are written as `0xFFFFFFFF` (sizes/offset) or `0xFFFF` (disk number) and only those get values in the Zip64 extra field, in APPNOTE order (uncompressed, compressed, offset, disk). Readers must consume exactly the fields whose header value is the sentinel, validate the extra field length per requested field, and never assume a fixed 28-byte layout. Keep one "needs Zip64" predicate shared by local header, central directory and EOCD writers. (`#77181`, `#94970`) |
| **Central directory is authoritative** | Sizes, offsets, compression method and flags come from the central directory; the local header may be stale or zeroed (data descriptor). Validate CD lengths/offsets eagerly while parsing the CD, not lazily in `Open()`, and never return garbage data that only throws later. Derive an entry's extent from the next entry's/CD offset instead of re-seeking the local header. (`#127990`, `#126349`) |
| **Update mode keeps untouched entries intact** | In `ZipArchiveMode.Update`, mixed `[modified, unchanged, modified]` sequences must keep the write cursor and every untouched entry's offset consistent: unchanged entries are copied/kept byte-for-byte (never re-encoded, skipped or decompressed), offsets are recomputed after deletes, and only the first changed entry onward is rewritten. Check `ChangeState` explicitly, including metadata-only changes. (`#102704`, `#126447`) |
| **Update mode lazy dirtying on `Open()` and atomic save-time validation** | Eagerly marking an existing `ZipArchiveMode.Update` entry dirty or clearing central-directory `Length`/`CompressedLength` inside `entry.Open()` before any `Write`, `SetLength`, or metadata setter runs (breaking callers like `Package.Open` that open streams for read/write inspection and `Dispose()` without writing); losing `_originalLastModified` needed for ZipCrypto header verification when `LastWriteTime` is unchanged; or running cross-property encryption/compression checks halfway through writing output instead of validating all entries atomically before mutating the archive stream. |
| **Data descriptors and CRC validation** | Bit 3 entries have zero sizes/CRC in the local header; `ForwardRead`/non-seekable paths must bound reads with a `SubReadStream`-style wrapper, not assume Deflate (Stored/Deflate64 exist), and accept 32- and 64-bit descriptors. CRC must be checked at EOF including 0-byte entries, and a failed check must not flip a "validated" flag so a retry silently skips it; reset CRC state when seeking back to 0. (`#126646`, `#124766`) |

---

## Encryption

| Check | What to look for |
|-------|-----------------|
| **WinZip AES and ZipCrypto header handling** | AES entries use method 99 with the real method in the `0x9901` extra field; write the vendor version explicitly (AE-1 keeps CRC, AE-2 zeroes it), derive keys from the salt in the header, and in Update mode re-derive rather than reuse keys. Verify the HMAC as soon as the last ciphertext byte is read (throw from `Read`, not `Dispose`), never let a boundary exception skip it, and don't drain ciphertext on recoverable errors. ZipCrypto's 12-byte header check byte comes from CRC or, with a data descriptor, from the DOS time. (`#122093`, `#126646`) |

---

## Names, Comments, and Encoding

| Check | What to look for |
|-------|-----------------|
| **Entry name and comment encoding** | Bit 11 (UTF-8) in the general purpose flag is shared by `FullName` and `Comment`: set it if either needs UTF-8 and never clear it because only one changed. Honor a custom `entryNameEncoding` symmetrically on read and write. Truncate comments on encoded bytes at a character boundary, never mid-sequence, and keep names using `/` separators regardless of OS. (`#59442`, `#65886`, `#119457`) |
| **Packaging is a ZipArchive consumer** | `System.IO.Packaging` multi-targets `netstandard2.0`/`net462` and validates part names and flags strictly, so ZIP-level changes (UTF-8 flag, compression option bits, entry ordering) can make previously valid packages fail to open on .NET Framework or older .NET. Only set Deflate option bits when the method is Deflate, and test round-trip with the oldest supported reader. (`#102096`, `#98278`) |
| **Packaging overload defaults and exception contracts** | Validation or size limits wired into only one `Package.Open` overload or gated on `FileAccess.ReadWrite` when sibling overloads (`Package.Open(Stream)`) default to `FileAccess.Read`, or corrupt/oversized OPC XML metadata (`[Content_Types].xml`, `.rels`) letting raw `System.Xml.XmlException` escape instead of wrapping into `System.IO.FileFormatException`. Audit default parameters across all `Open` overloads and enforce the documented `FileFormatException` contract consistently. |

---

## Extraction Safety

| Check | What to look for |
|-------|-----------------|
| **Extraction stays inside the destination** | Resolve every entry path with `Path.GetFullPath` against a separator-terminated destination and reject escapes (`..`, rooted names, drive letters). Sanitize the stored entry path, not just the file name. Define behavior for `.`/empty and directory-only entries. Archive-planted symlinks must not be followed to write outside the destination; the destination itself is treated as trusted. (`#131202`, `#87534`, `#85378`) |
| **Atomic overwrite and metadata on the handle** | Overwrites use `FileMode.Create` or a temp file staged in the destination directory (same volume, so the final move is an atomic rename and never an EXDEV copy that leaks partial files from `/tmp`), deleted on failure. Apply timestamps and `UnixFileMode` through the open handle, honor `umask`, and don't widen permissions from `ExternalAttributes` beyond what the archive states. (`#123991`, `#64192`, `#67883`) |
