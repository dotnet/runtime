---
applyTo: "src/libraries/System.Formats.Tar/**"
---

# System.Formats.Tar — Folder-Specific Guidance

- Canonical specs: [POSIX.1-2024 `pax`](https://pubs.opengroup.org/onlinepubs/9799919799/utilities/pax.html) for ustar/pax (use the edition the API targets); the [GNU tar manual](https://www.gnu.org/software/tar/manual/html_node/Extensions.html) only for GNU extensions.
- POSIX `uid_t` and `gid_t` are unsigned 32-bit integers (`0..uint.MaxValue`) exposed on `TarEntry.Uid`/`Gid` as signed `int`: convert unsigned octal, GNU base-256, and PAX `uid`/`gid` values in `0..uint.MaxValue` via `unchecked((int)value)` on read, and reinterpret negative `int` values via `unchecked((uint)value)` on write (using GNU base-256 `0x80` high-bit encoding in GNU format and unsigned decimal strings in PAX extended attributes).
