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