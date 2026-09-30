// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "emit.hpp"
#include <vector>

TEST(TypeRef, ValidScopeAndDottedName)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdTypeRef typeRef;
    WSTR_string name = W("System.Object");
    ASSERT_EQ(S_OK, emit->DefineTypeRefByName(TokenFromRid(1, mdtModule), name.c_str(), &typeRef));
    ASSERT_EQ(1, RidFromToken(typeRef));
    ASSERT_EQ(mdtTypeRef, TypeFromToken(typeRef));

    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));
    mdToken resolutionScope;
    WSTR_string readName;
    readName.resize(name.capacity() + 1);
    ULONG readNameLength;
    ASSERT_EQ(S_OK, import->GetTypeRefProps(typeRef, &resolutionScope, &readName[0], (ULONG) readName.size(), &readNameLength));
    EXPECT_EQ(TokenFromRid(1, mdtModule), resolutionScope);
    EXPECT_EQ(readNameLength, name.size() + 1);
    EXPECT_EQ(name, readName.substr(0, readNameLength - 1));
}

TEST(TypeRef, InvalidScope)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdTypeRef typeRef;
    ASSERT_EQ(E_FAIL, emit->DefineTypeRefByName(TokenFromRid(1, mdtTypeDef), W("System.Object"), &typeRef));
}

TEST(TypeRef, ValidScopeAndNonDottedName)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdTypeRef typeRef;
    WSTR_string name = W("Bar");
    ASSERT_EQ(S_OK, emit->DefineTypeRefByName(TokenFromRid(1, mdtModule), name.c_str(), &typeRef));
    ASSERT_EQ(1, RidFromToken(typeRef));
    ASSERT_EQ(mdtTypeRef, TypeFromToken(typeRef));

    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));
    mdToken resolutionScope;
    WSTR_string readName;
    readName.resize(name.capacity() + 1);
    ULONG readNameLength;
    ASSERT_EQ(S_OK, import->GetTypeRefProps(typeRef, &resolutionScope, &readName[0], (ULONG) readName.size(), &readNameLength));
    EXPECT_EQ(TokenFromRid(1, mdtModule), resolutionScope);
    EXPECT_EQ(readNameLength, name.size() + 1);
    EXPECT_EQ(name, readName.substr(0, readNameLength - 1));
}

TEST(TypeRef, StringHeapGrowsToFourByteIndices)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));

    mdTypeRef first, large, last;
    mdToken scope = TokenFromRid(1, mdtModule);
    WSTR_string largeName(65500, static_cast<WCHAR>('x'));
    ASSERT_EQ(S_OK, emit->DefineTypeRefByName(scope, W("BeforeGrowth"), &first));
    ASSERT_EQ(S_OK, emit->DefineTypeRefByName(scope, largeName.c_str(), &large));
    ASSERT_EQ(S_OK, emit->DefineTypeRefByName(scope, W("AfterGrowth"), &last));

    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));
    WSTR_string name(largeName.size() + 1, static_cast<WCHAR>(0));
    ULONG nameLength;
    mdToken resolutionScope;
    ASSERT_EQ(S_OK, import->GetTypeRefProps(first, &resolutionScope, name.data(),
        (ULONG)name.size(), &nameLength));
    EXPECT_EQ(W("BeforeGrowth"), name.substr(0, nameLength - 1));
    ASSERT_EQ(S_OK, import->GetTypeRefProps(large, &resolutionScope, name.data(),
        (ULONG)name.size(), &nameLength));
    EXPECT_EQ(largeName, name.substr(0, nameLength - 1));
    ASSERT_EQ(S_OK, import->GetTypeRefProps(last, &resolutionScope, name.data(),
        (ULONG)name.size(), &nameLength));
    EXPECT_EQ(W("AfterGrowth"), name.substr(0, nameLength - 1));
    EXPECT_EQ(scope, resolutionScope);

    DWORD size;
    ASSERT_EQ(S_OK, emit->GetSaveSize(cssAccurate, &size));
    std::vector<uint8_t> serialized(size);
    ASSERT_EQ(S_OK, emit->SaveToMemory(serialized.data(), size));

    minipal::com_ptr<IMetaDataDispenser> dispenser;
    ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenser, (void**)&dispenser));
    minipal::com_ptr<IMetaDataImport> reopened;
    ASSERT_EQ(S_OK, dispenser->OpenScopeOnMemory(serialized.data(), size, ofReadOnly | ofCopyMemory,
        IID_IMetaDataImport, (IUnknown**)&reopened));
    ASSERT_EQ(S_OK, reopened->GetTypeRefProps(last, &resolutionScope, name.data(),
        (ULONG)name.size(), &nameLength));
    EXPECT_EQ(W("AfterGrowth"), name.substr(0, nameLength - 1));
}

TEST(TypeRef, ForwardResolutionScopeSurvivesAppend)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));

    mdTypeRef nested, enclosing;
    ASSERT_EQ(S_OK, emit->DefineTypeRefByName(TokenFromRid(2, mdtTypeRef), W("Nested"), &nested));
    ASSERT_EQ(S_OK, emit->DefineTypeRefByName(TokenFromRid(1, mdtModule), W("Enclosing"), &enclosing));

    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));
    mdToken resolutionScope;
    WCHAR name[16];
    ULONG nameLength;
    ASSERT_EQ(S_OK, import->GetTypeRefProps(nested, &resolutionScope, name, 16, &nameLength));
    EXPECT_EQ(enclosing, resolutionScope);
}

TEST(TypeRef, WidenReferencedCodedIndices)
{
    minipal::com_ptr<IMetaDataDispenserEx> dispenser;
    ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenserEx, (void**)&dispenser));
    VARIANT option{};
    V_VT(&option) = VT_UI4;
    V_UI4(&option) = MDNoDupChecks;
    ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataCheckDuplicatesFor, &option));

    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_EQ(S_OK, dispenser->DefineScope(CLSID_CorMetaDataRuntime, 0,
        IID_IMetaDataEmit, (IUnknown**)&emit));
    mdToken module = TokenFromRid(1, mdtModule);
    mdTypeRef original;
    ASSERT_EQ(S_OK, emit->DefineTypeRefByName(module, W("Original"), &original));
    mdTypeDef type;
    ASSERT_EQ(S_OK, emit->DefineTypeDef(W("Derived"), tdPublic, original, nullptr, &type));

    mdTypeRef last = original;
    for (int i = 1; i < 16'384; ++i)
        ASSERT_EQ(S_OK, emit->DefineTypeRefByName(module, W("Filler"), &last));
    ASSERT_EQ(16'384u, RidFromToken(last));

    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));
    WCHAR name[16];
    ULONG nameLength;
    DWORD flags;
    mdToken extends;
    ASSERT_EQ(S_OK, import->GetTypeDefProps(type, name, 16, &nameLength, &flags, &extends));
    EXPECT_EQ(original, extends);
    ASSERT_EQ(S_OK, emit->SetTypeDefProps(type, UINT32_MAX, last, nullptr));
    ASSERT_EQ(S_OK, import->GetTypeDefProps(type, name, 16, &nameLength, &flags, &extends));
    EXPECT_EQ(last, extends);

    DWORD size;
    ASSERT_EQ(S_OK, emit->GetSaveSize(cssAccurate, &size));
    std::vector<uint8_t> data(size);
    ASSERT_EQ(S_OK, emit->SaveToMemory(data.data(), size));
    minipal::com_ptr<IMetaDataImport> reopened;
    ASSERT_EQ(S_OK, dispenser->OpenScopeOnMemory(data.data(), size, ofReadOnly | ofCopyMemory,
        IID_IMetaDataImport, (IUnknown**)&reopened));
    ASSERT_EQ(S_OK, reopened->GetTypeDefProps(type, name, 16, &nameLength, &flags, &extends));
    EXPECT_EQ(last, extends);
}