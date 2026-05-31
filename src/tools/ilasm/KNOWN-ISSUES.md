# Managed IL Assembler - Known Issues

## TLS RVA statics

Thread-local storage (TLS) RVA static fields (`.data tls`) are not
supported by the managed ilasm. The native ilasm emits a TLS directory
entry in the PE header for these, which the managed ilasm's PE builder
does not currently implement.


