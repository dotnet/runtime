# Test: System.Text.Tests.EncodingTests.GetEncoding_FromProvider_ByCodePage_WithDisallowedEncoding_Throws

## Test Suite
System.Runtime.Tests

## Failure Type
crash

## Exception Type
RuntimeError: memory access out of bounds

## Stack Trace
```
RuntimeError: memory access out of bounds
    at dotnet.native.wasm.StgBlobPool::AddBlob(MetaData::DataBlob const*, unsigned int*)
    at dotnet.native.wasm.MetaData::BlobHeapRW::AddBlob(MetaData::DataBlob, unsigned int*)
    at dotnet.native.wasm.CMiniMdRW::PutBlob(unsigned int, unsigned int, void*, void const*, unsigned int)
    at dotnet.native.wasm.ImportHelper::CreateAssemblyRefFromAssemblyRef(...)
    at dotnet.native.wasm.ImportHelper::ImportTypeRef(...)
    at dotnet.native.wasm.ImportHelper::MergeUpdateTokenInFieldSig(...)
    at dotnet.native.wasm.ImportHelper::MergeUpdateTokenInSig(...)
    at dotnet.native.wasm.TranslateSigHelper(...)
    at dotnet.native.wasm.MDInternalRO::TranslateSigWithScope(...)
    at dotnet.native.wasm.ModuleBuilder_GetMemberRefOfMethodInfo(...)
    ... (interpreter frames)
```

## Notes
- Platform: Browser/WASM + CoreCLR
- Category: interpreter (Reflection.Emit / dynamic code generation)
- This test appears to use Reflection.Emit (ModuleBuilder) which has known limitations on WASM
- The crash occurred in native WASM code related to metadata manipulation
- This crashes the entire test runner, preventing remaining tests from executing
- **Blocking:** This test must be disabled to continue running the test suite
