# Public .NET APIs #

The contents of this directory include headers for various native APIs exposed
by the .NET runtimes.

These include:

- The Mono Embedding API
- ...

## The Mono Embedding API ##

The headers in this directory constitute the `mono-2.0` runtime embedding API.  They represent a
contract between the runtime and clients.  Care must be taken to ensure that API and ABI
compatibility is not broken.  For example: adding or removing function arguments, changing the
return type, changing the layout of defined non-opaque structs, changing the calling convention, etc
may potentially break existing clients and should not be undertaken lightly.

Adding functions constitutes a contract between the runtime and potential future clients.  Adding a
new public API function incurs a maintenance cost.  Care should be taken.

### Unstable embedding API ###

The headers `mono-private-unstable.h` in each subdirectory are an exception to the above guarantees.
Functions added to these headers represent "work in progress" and may break API semantics or ABI
compatibility.  Clients using these functions are generally tightly coupled to the development of
the runtime and take on responsibility for any breaking changes.



