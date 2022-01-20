# Ready to run PerfMap format

Traditionally in .NET symbols have been described using PDBs. These are used to map IL to source lines for code that the JIT will compile. The JIT usually emits the data that can then map from IL to a native address for symbolication purposes.

Ready to run, however, avoids this IL to native code translation at runtime. For this reason, tools that emit R2R images often need to emit auxiliary artifacts to facilitate the mapping between source and native addresses. The Ready to Run PerfMap format describes such one map - where any method in the source code is associated with a region within the R2R image. That way any region from such image that gets executed can be linked back to a method at the source level. This facilitates tasks like stack symbolication for performance oriented investigations, although it is not appropriate to aid in tasks such as debugging at the source line level.

## Version 1

R2R PerfMaps of version 1 are usually found in files with the extension `.ni.r2rmap`. It's a plain text UTF-8 format where each entry is on a separate line. Each entry is composed of a triplet: an offset relative to the beginning of the image, a length, and a name. The file is laid out in the following as follows.

### Header

The header leads the file and is composed by special entries. Each entry contains a 4 byte integer token in place of an RVA signifying the type of information in the entry, a length that is always 0, and the entry data. The entries are emitted in the following order.

| Token      | Description                                                           |
|:-----------|-----------------------------------------------------------------------|
| 0xFFFFFFFF | A 16 byte sequence representing a signature to correlate the perfmap with the r2r image. |
| 0xFFFFFFFE | The version of the perfmap being emitted as a unsigned 4 byte integer. |
| 0xFFFFFFFD | An unsigned 4 byte unsigned integer representing the OS the image targets. See [enumerables section](#enumerables-used-in-headers)  |
| 0xFFFFFFFC | An unsigned 4 byte unsigned integer representing the architecture the image targets. See [enumerables section](#enumerables-used-in-headers) |
| 0xFFFFFFFB | An unsigned 4 byte unsigned integer representing the ABI of the image. See [enumerables section](#enumerables-used-in-headers) |

These entries contain information about the compilation that can be useful to tools and identifiers that can be used to correlate a perfmap with an image as described in ["Ready to Run format - debug directory entries"](./readytorun-format.md#additions-to-the-debug-directory).


### Content

Each entry is a triplet - the relative address of a method with respect to the image start as an unsigned 4 byte integer, the number of bytes used by the native code represented by an unsigned 2 byte integer, and the name of the method. There's one entry per line after the header, and a method can appear more than once since if may have gone through cold/hot path splitting.

## Enumerables used in headers.

```
PerfMapArchitectureToken
    Unknown = 0,
    ARM     = 1,
    ARM64   = 2,
    X64     = 3,
    X86     = 4,
```

```
PerfMapOSToken
    Unknown     = 0,
    Windows     = 1,
    Linux       = 2,
    OSX         = 3,
    FreeBSD     = 4,
    NetBSD      = 5,
    SunOS       = 6,
```

```
PerfMapAbiToken
    Unknown = 0,
    Default = 1,
    Armel = 2,
```
