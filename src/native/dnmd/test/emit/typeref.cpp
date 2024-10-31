#include "emit.hpp"

TEST(TypeRef, ValidScopeAndDottedName)
{
    dncp::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdTypeRef typeRef;
    WSTR_string name = W("System.Object");
    ASSERT_EQ(S_OK, emit->DefineTypeRefByName(TokenFromRid(1, mdtModule), name.c_str(), &typeRef));
    ASSERT_EQ(1, RidFromToken(typeRef));
    ASSERT_EQ(mdtTypeRef, TypeFromToken(typeRef));

    dncp::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));
    mdToken resolutionScope;
    WSTR_string readName;
    readName.resize(name.capacity() + 1);
    ULONG readNameLength;
    ASSERT_EQ(S_OK, import->GetTypeRefProps(typeRef, &resolutionScope, readName.data(), (ULONG) readName.size(), &readNameLength));
    EXPECT_EQ(TokenFromRid(1, mdtModule), resolutionScope);
    EXPECT_EQ(readNameLength, name.size() + 1);
    EXPECT_EQ(name, readName.substr(0, readNameLength - 1));
}

TEST(TypeRef, InvalidScope)
{
    dncp::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdTypeRef typeRef;
    ASSERT_EQ(E_FAIL, emit->DefineTypeRefByName(TokenFromRid(1, mdtTypeDef), W("System.Object"), &typeRef));
}

TEST(TypeRef, ValidScopeAndNonDottedName)
{
    dncp::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdTypeRef typeRef;
    WSTR_string name = W("Bar");
    ASSERT_EQ(S_OK, emit->DefineTypeRefByName(TokenFromRid(1, mdtModule), name.c_str(), &typeRef));
    ASSERT_EQ(1, RidFromToken(typeRef));
    ASSERT_EQ(mdtTypeRef, TypeFromToken(typeRef));

    dncp::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));
    mdToken resolutionScope;
    WSTR_string readName;
    readName.resize(name.capacity() + 1);
    ULONG readNameLength;
    ASSERT_EQ(S_OK, import->GetTypeRefProps(typeRef, &resolutionScope, readName.data(), (ULONG) readName.size(), &readNameLength));
    EXPECT_EQ(TokenFromRid(1, mdtModule), resolutionScope);
    EXPECT_EQ(readNameLength, name.size() + 1);
    EXPECT_EQ(name, readName.substr(0, readNameLength - 1));
}