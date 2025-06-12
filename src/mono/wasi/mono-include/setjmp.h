// setjmp.h is provided by WASI SDK, complains that:
//    Setjmp/longjmp support requires Exception handling support, which is [not yet standardized](https://github.com/WebAssembly/proposals?tab=readme-ov-file#phase-3---implementation-phase-cg--wg).
//    To enable it, compile with `-mllvm -wasm-enable-sjlj` and use an engine that implements the Exception handling proposal.
// many Mono source files want to import it, so we need this here
// We don't call any of its symbols at runtime so it can be left empty
