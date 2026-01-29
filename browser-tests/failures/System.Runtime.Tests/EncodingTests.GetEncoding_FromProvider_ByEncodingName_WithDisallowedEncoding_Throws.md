# Test: System.Text.Tests.EncodingTests.GetEncoding_FromProvider_ByEncodingName_WithDisallowedEncoding_Throws

## Test Suite
System.Runtime.Tests

## Failure Type
crash

## Exception Type
RuntimeError: memory access out of bounds

## Stack Trace
```
DOTNET: Unhandled error: RuntimeError: memory access out of bounds
at dotnet.native.wasm.StgBlobPool::AddBlob(MetaData::DataBlob const*, unsigned int*)
at dotnet.native.wasm.MetaData::BlobHeapRW::AddBlob(MetaData::DataBlob, unsigned int*)
at dotnet.native.wasm.CMiniMdRW::PutBlob(unsigned int, unsigned int, void*, void const*, unsigned int)
at dotnet.native.wasm.ImportHelper::CreateAssemblyRefFromAssemblyRef(CMiniMdRW*, CMiniMdRW*, IMetaModelCommon*, unsigned int, unsigned int*)
at dotnet.native.wasm.ImportHelper::ImportTypeRef(CMiniMdRW*, CMiniMdRW*, IMetaModelCommon*, void const*, unsigned int, IMetaModelCommon*, unsigned int, unsigned int*)
at dotnet.native.wasm.ImportHelper::MergeUpdateTokenInFieldSig(CMiniMdRW*, CMiniMdRW*, IMetaModelCommon*, void const*, unsigned int, IMetaModelCommon*, unsigned char const*, MDTOKENMAP*, CQuickBytes*, unsigned int, unsigned int*, unsigned int*)
at dotnet.native.wasm.ImportHelper::MergeUpdateTokenInSig(CMiniMdRW*, CMiniMdRW*, IMetaModelCommon*, void const*, unsigned int, IMetaModelCommon*, unsigned char const*, MDTOKENMAP*, CQuickBytes*, unsigned int, unsigned int*, unsigned int*)
at dotnet.native.wasm.TranslateSigHelper(IMDInternalImport*, IMDInternalImport*, void const*, unsigned int, unsigned char const*, unsigned int, IMetaDataAssemblyEmit*, IMetaDataEmit*, CQuickBytes*, unsigned int*)
at dotnet.native.wasm.MDInternalRO::TranslateSigWithScope(IMDInternalImport*, void const*, unsigned int, unsigned char const*, unsigned int, IMetaDataAssemblyEmit*, IMetaDataEmit*, CQuickBytes*, unsigned int*)
at dotnet.native.wasm.ModuleBuilder_GetMemberRefOfMethodInfo
```

## Notes
- Platform: Browser/WASM + CoreCLR
- Category: interpreter/Reflection.Emit
- Root cause: Test uses Moq library which requires Reflection.Emit. Reflection.Emit has issues on Browser+CoreCLR (interpreter mode).
- The test `GetEncoding_FromProvider_ByCodePage_WithDisallowedEncoding_Throws` already had the same ActiveIssue attribute applied.
- Also applied to: `GetEncodings_FromProvider_DoesNotContainDisallowedEncodings`
