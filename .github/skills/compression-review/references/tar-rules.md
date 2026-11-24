# Tar Review Rules

Rules for `TarReader`, `TarWriter`, `TarEntry` and its format subclasses, `TarFile`, and the tar Unix PAL helpers in `src/libraries/System.Formats.Tar`.

---

## Header Encoding

| Check | What to look for |
|-------|-----------------|
| **PAX records are byte-framed** | Each extended record is `"<len> key=value\n"` where `<len>` is the UTF-8 byte count of the whole record including its own digits, the space and the trailing newline; compute it in bytes (not `string.Length`) and iterate because adding a digit can grow the length. Values may contain embedded newlines, so parse by length, never by splitting lines; keys must not contain `=` or `\n` and must be rejected when they do. (`#74281`) |
| **PAX attributes stay in sync with header fields** | Public properties (`Name`, `LinkName`, `ModificationTime`, uid/gid, size) and their `path`/`linkpath`/`mtime`/`size` extended attributes must agree on read and write: setters update or remove the key, conflicting user-supplied keys throw instead of silently winning, and global extended attributes are applied in archive order. Treat every overflowable field the same way (`AddOrRemove`), including `linkpath`. (`#75237`, `#123990`, `#76404`) |
| **Field width, octal and base-256 invariants** | Ustar/V7 numeric fields are NUL/space-terminated octal of fixed width; values that don't fit must go to PAX attributes or GNU base-256 (high bit set), and setters must throw for formats that can't represent them rather than truncate. Parse leniently only for leading/trailing NUL/space, not arbitrary garbage; reject negative sizes. Don't invent defaults (GNU atime/ctime, `UnixEpoch` special cases) that some tools reject. (`#101172`, `#74358`, `#115211`) |
| **Unsigned 32-bit POSIX `uid_t`/`gid_t` mapping** | Rejecting unsigned octal, GNU base-256, or PAX `uid`/`gid` values in `int.MaxValue + 1 .. uint.MaxValue` (`2147483648..4294967295`) with `OverflowException`, or writing negative `TarEntry.Uid`/`Gid` `int` values as signed negative PAX decimals instead of reinterpreting via `unchecked((int)u32)` on read and `unchecked((uint)i32)` on write (GNU base-256 `0x80` high-bit or unsigned PAX decimal). |
| **Long names split on UTF-8 boundaries** | Ustar `name` (100 bytes) and `prefix` (155 bytes) are filled from encoded bytes, split only at a `/` and never mid-character; slice destinations to the encoded byte count. V7 fails on overflow while PAX/GNU store the full path in an extended record or `LongPath` entry, so the truncated legacy field must not be the source of truth on read. (`#75023`, `#75902`) |
| **Checksum covers the written header** | The checksum is the unsigned byte sum of the 512-byte header with the checksum field treated as eight spaces; compute it over the bytes actually written (zeroed buffer, sliced fields), identically in sync and async paths, and verify it on read so a corrupt or non-tar stream fails early instead of yielding a bogus entry. (`#75902`, `#118577`, `#114940`) |

---

## Reading

| Check | What to look for |
|-------|-----------------|
| **Advance past unconsumed data and padding** | Before reading the next header, `TarReader` must skip the remaining bytes of the previous entry's data stream (`SubReadStream`, including when the caller never read or only partly read it) plus the zero padding to the next 512-byte boundary, on both seekable and unseekable streams. Bound the entry stream by its declared size, fail early when the archive is truncated, and never read past a block into the next entry's data (e.g. sparse maps). (`#131794`, `#125283`) |

---

## Extraction Safety

| Check | What to look for |
|-------|-----------------|
| **Link targets and paths stay inside the destination** | Sanitize the stored entry path and resolve it against the destination; hard links must resolve inside the destination and may not be absolute, and symlinks whose target escapes must not be followed by later entries. Overwrite with `FileMode.Create`, respect `umask` when applying `UnixFileMode`, and define `overwrite: false` behavior for pre-existing directories. The destination itself is trusted. (`#85378`, `#77338`, `#74002`) |
