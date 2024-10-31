#include "emit.hpp"
#include <array>
#include <vector>
#include <gmock/gmock.h>

TEST(MemberRef, Define)
{
    dncp::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdMemberRef memberRef;
    std::array<uint8_t, 3> signature = {0x01, 0x02, 0x03};
    ASSERT_EQ(S_OK, emit->DefineMemberRef(TokenFromRid(1, mdtTypeDef), W("Foo"), signature.data(), (ULONG)signature.size(), &memberRef));
    ASSERT_EQ(1, RidFromToken(memberRef));
    ASSERT_EQ(mdtMemberRef, TypeFromToken(memberRef));

    dncp::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));

    mdTypeDef type;
    WSTR_string readName;
    readName.resize(3);
    ULONG readNameLength;
    PCCOR_SIGNATURE sigBlob;
    ULONG sigBlobLength;
    ASSERT_EQ(S_OK, import->GetMemberRefProps(memberRef, &type, readName.data(), (ULONG)readName.capacity(), &readNameLength, &sigBlob, &sigBlobLength));
    EXPECT_EQ(W("Foo"), readName.substr(0, readNameLength - 1));
    EXPECT_EQ(TokenFromRid(1, mdtTypeDef), type);
    EXPECT_THAT(std::vector(sigBlob, sigBlob + sigBlobLength), testing::ContainerEq(std::vector(signature.begin(), signature.end())));
}

TEST(MemberRef, SetParent)
{
    dncp::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdMemberRef memberRef;
    std::array<uint8_t, 3> signature = {0x01, 0x02, 0x03};
    ASSERT_EQ(S_OK, emit->DefineMemberRef(TokenFromRid(1, mdtTypeDef), W("Foo"), signature.data(), (ULONG)signature.size(), &memberRef));
    ASSERT_EQ(1, RidFromToken(memberRef));
    ASSERT_EQ(mdtMemberRef, TypeFromToken(memberRef));

    dncp::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));

    mdToken parent;
    WSTR_string readName;
    readName.resize(3);
    ULONG readNameLength;
    PCCOR_SIGNATURE sigBlob;
    ULONG sigBlobLength;
    ASSERT_EQ(S_OK, import->GetMemberRefProps(memberRef, &parent, readName.data(), (ULONG)readName.capacity(), &readNameLength, &sigBlob, &sigBlobLength));
    EXPECT_EQ(W("Foo"), readName.substr(0, readNameLength - 1));
    EXPECT_EQ(TokenFromRid(1, mdtTypeDef), parent);
    EXPECT_THAT(std::vector(sigBlob, sigBlob + sigBlobLength), testing::ContainerEq(std::vector(signature.begin(), signature.end())));

    ASSERT_EQ(S_OK, emit->SetParent(memberRef, TokenFromRid(2, mdtTypeRef)));
          
    readName.resize(3);
    ASSERT_EQ(S_OK, import->GetMemberRefProps(memberRef, &parent, readName.data(), (ULONG)readName.capacity(), &readNameLength, &sigBlob, &sigBlobLength));
    EXPECT_EQ(W("Foo"), readName.substr(0, readNameLength - 1));
    EXPECT_EQ(TokenFromRid(2, mdtTypeRef), parent);
    EXPECT_THAT(std::vector(sigBlob, sigBlob + sigBlobLength), testing::ContainerEq(std::vector(signature.begin(), signature.end())));
}