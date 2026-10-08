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

## ARM32 target images are not supported

Managed ilasm does not support generating ARM32 (AArch32) machine images. The native `/ARM`
switch is rejected.

## Pseudo custom attributes are not lowered by default

Managed ilasm leaves custom attributes unchanged by default. Use `--pseudoattributes`
to lower recognized pseudo custom attributes into metadata flags and auxiliary tables.
This opt-in switch has no legacy `/` or single-dash spelling.
