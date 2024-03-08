## .NET runtime data stream

The .NET runtime data stream is mechanism for the runtime to encode information about itself in a way that is accessible to diagnostic tools. This enables de-coupling of the tooling (for example, the DAC) from the details of a specific version of the runtime.

Data Streams consist of three concepts.

1. A collection of type descriptions.

2. A collection of instances (i.e., pointers) of types described in (1).

3. A collection of value blobs.

4. A versioning scheme that permits evolution of (1), (2), and (3).

The data streams model begins with a header that captures the minimum needed data&mdash;`data_stream_context_t`. The header contains a mechanism, `magic` field, for confirming the memory is what we expect _and_ also serves to indicate the endianness of the target process. The endianness of the target is important for scenarios where the reader is running on another machine. The header also contains data stream versioning and statically allocated stream count.

### Streams and Blocks (ver.1)

The stream, `data_stream_t`, is an opaque data structure to the user and is an implementation detail. The only indication of changes to it are captured in the version value contained on the header. The current design will be described and is considered version "1".

A stream is a singly linked list of uniform sized blocks that are allocated on demand. The stream itself is a small type used to hold the head of the list of blocks, the `curr` pointer, and a pointer to the data stream header, `cxt`.

The core of the stream is the block, `data_block_t`. This data structure is a contiguous allocation with (4) pointer slots&mdash;`begin`, `end`, `pos` and `prev`. The `begin`, `end` and `prev` pointers are set on allocation and never change. The internal block allocation scheme is commonly called a "bump allocator". The `pos` pointer represents the current spot in the range between `begin` and `end`. Blocks are filled in reverse order (`end` to `begin`) to ensure reading of a stream is always performed in reverse chronological order.

Both `pos` value on the `data_block_t` and `curr` on the `data_stream_t` are both updated atomically and expected to be lock-free.

Within each block an entry data structure, `stream_entry_t`, is used to quickly and safely add new entries. An entry consists of a field, `offset_next`, to hold the relative offset from the current entry to the next. This offset concept makes reading easy since once the entire block is read from the target no further memory reads are needed to walk the block.

The simplicity of the streams and blocks makes reading from another process simple.

#### Types (ver.1)

The collection of types are all recorded in the first stream in the `data_stream_context_t` type's `streams` field.

Types are expressed with minimal data to efficiently version and read from a target process. Type definitions start with an identifying tuple&mdash;`type` (numeric ID), `version` and `name`. The tuple's design facilitates creation of a map look-up on the reader side and a way to evolve the definition safely on the target side.

The layout of a type is expressed by the size, in bytes, and a collection of relevant field offsets and their type, `field_offset_t`. Field offset count is computed by reading in two pointer sized values and then dividing the remaining space by the size of the `field_offset_t` data structure. Both of these components are needed to satisfy the evolution and reading efficiency goals. The size allows the reader to read an entire type in one operation and the field offsets need not be exhaustive if they provide no utility on the reader side.

An example of the current memory layout of a type entry is below.

```
| type_details_t*  | # Pointer in target process
| size_t           | # Total size, in bytes, of the type
| field_offset_t 1 | # First field offset
| ...              |
| field_offset_t N | # Last field offset
```

#### Instances (ver.1)

All streams, other than the first stream in the `data_stream_context_t` type's `streams` field, which is used for types, contain instances.

Instances are defined as a numeric ID and a valid pointer in the target process. The numeric ID is expected to exist in one of the type identifier tuples defined above.

An example of the current memory layout of an instance entry is below.

```
| uint16_t | # Type numeric ID
| intptr_t | # Target process memory
```

### Target usage

Consumption of data streams should start with a mechanism for defining the type identity tuple that can be shared between the target and reader.

A `data_stream_context_t` instance should be allocated, statically or dynamically, in a manner where its address is discoverable by the reader process. The `data_stream_context_t` instance must be initialized and static stream count and block sizes defined. There must be at least a single stream size for use in the type definitions. It is expected that the type's stream has a block size that is sufficient to hold all type defintions without an additional allocation.

Type versions or names are not used directly by the target process. The target process records these values as an indication for the reader only.

**NOTE** Registration of types should be done prior to any recording of instances. It is assumed that all sharable types are known statically.

After type registration is performed, streams can be aquired by components in the target process and typed instances inserted into the stream. Adding an instance to a stream is considered thread safe. The typing of an instance should be done via a numeric ID.

### Reader usage

The reader should first define a series of type names that it is able to interpret and consume. These type names should match the names defined by the target. These names could be used to map types in the target process with their numeric ID and version. The reader is not expected to have any hardcoded numeric type IDs as these are subject to change between target versions.

The reader is expected to be resiliant in recieving an unknown version of a type and gracefully interpret it. Two examples of graceful interpretation are describing it as `"Unknown"` or printing its memory address in the target process.

After acquiring the target process's `data_stream_context_t` address, it should be validated and endianness computed.

The first time the data stream is read, the target processes types are enumerated in reserve chronological order (Last In/First Out) and this data may be used throughout the lifetime of the target process. During the enumeration of types the following can be done:

* Creation of a fast mapping from name to numeric ID.

* Creation of look-up map to type details (e.g., field offsets).

* Validation of supported type versions.

After type enumeration is complete, instance streams can be enumerated and interpreted. The size contained within the type description allows the reader to read in the entire type and then use field offsets to poke into that memory. The reading in of the entire data type helps with efficiency and acts as a versioning resiliance mechanism. Adding new fields to a type, without changing the version, need not represent a breaking change.

### Design FAQs

---

**Q1** Why are streams allowed to grow?

**A1** Consider the case where a data structure in the target process has a specific use case but the reader has either stricter or looser requirements. An example would be a thread pool used in the target process. This structure would ideally only be concerned with current threads in the target process, exited threads having been removed. However, the reader process likely has a need for knowing when a thread instance has exited to update its own internal state. A possible solution is to fully query the thread pool data structure each time. However, if instead entries for created and deleted threads are recorded in a stream, the reader only needs to know the delta as opposed to querying the thread pool each time. The logic follows for any data structure that contains objects with transient lifetimes.

---

**Q2** Why are the contents of a stream immutable?

**A2** Having streams that are mutable means the reader _must_ always re-read the full stream to validate for updates. If the contents of a stream are instead immutable _and_ in reverse chronological order (LIFO), then entries for "deleted" or "invalidated" data are possible, which enables readers to consume deltas and reduce cross-process inspection.

---