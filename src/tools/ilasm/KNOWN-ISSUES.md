# Managed IL Assembler - Known Issues

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
