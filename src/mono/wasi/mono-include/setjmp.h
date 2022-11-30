// setjmp.h isn't provided by WASI SDK, but many Mono source files want to import it, so we need this here
// We don't call any of its symbols at runtime so it can be left empty
