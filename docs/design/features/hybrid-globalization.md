# Hybrid Globalization

Description, purpose and instruction how to use.

## Behavioral differences

Hybrid mode does not use ICU data for some functions connected with globalization but relies on functions native to the platform. Because native APIs do not fully cover all the functionalities we currently support and because ICU data can be excluded from the ICU datafile only in batches defined by ICU filters, not all functions will work the same way or not all will be supported. To see what to expect after switching on `HybridGlobalization`, read the following paragraphs.

### WASM

For WebAssembly, both on Browser and WASI, we are using Web API instead of some ICU data.

**Case change**

Affected public APIs:
- TextInfo.ToLower,
- TextInfo.ToUpper,
- TextInfo.ToTitleCase.

Case change with invariant culture uses `toUpperCase` / `toLoweCase` functions that do not guarantee a full match with the original invariant culture.
