# Managed IL Assembler - Known Issues

## TLS RVA statics

Thread-local storage (TLS) RVA static fields (`.data tls`) are not
supported by the managed ilasm. The native ilasm emits a TLS directory
entry in the PE header for these, which the managed ilasm's PE builder
does not currently implement.

## Win32 resources

Embedding Win32 resources (in either `.obj` or `.res` format) is not
supported by managed ilasm.

## -MSV is not supported

Overriding the metadata stream version with `-MSV` is not supported by
managed ilasm.

## -OPTIMIZE is a no-op

Currently managed ilasm does not do any optimizations of the IL written by the user.

## -FOLD is a no-op

Currently managed ilasm does not fold identical IL bodies from different methods into the same blob.
