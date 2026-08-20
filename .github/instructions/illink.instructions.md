---
applyTo: "src/tools/illink/test/**,src/coreclr/tools/ILTrim.Tests/**,src/coreclr/tools/aot/ILCompiler.Trimming.Tests/**"
---

# Shared trimming tests

- Changes to shared test cases must pass in all applicable tools: ILLink, ILC, the ILLink
  analyzer, and ILTrim.
- For unsupported ILTrim cases, follow the existing expected-failure conventions without
  regressing behavior that ILTrim already supports.
