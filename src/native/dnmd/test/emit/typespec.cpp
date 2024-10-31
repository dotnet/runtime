#include "emit.hpp"
#include <array>
#include <vector>
#include <gmock/gmock.h>

TEST(TypeSpec, Define)
{
    dncp::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdTypeSpec spec;
    std::array<uint8_t, 3> signature = {0x01, 0x02, 0x03};
    ASSERT_EQ(S_OK, emit->GetTokenFromTypeSpec(signature.data(), (ULONG)signature.size(), &spec));
    ASSERT_EQ(1, RidFromToken(spec));
    ASSERT_EQ(mdtTypeSpec, TypeFromToken(spec));

    dncp::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));

    PCCOR_SIGNATURE sigBlob;
    ULONG sigBlobLength;
    ASSERT_EQ(S_OK, import->GetTypeSpecFromToken(spec, &sigBlob, &sigBlobLength));
    EXPECT_THAT(std::vector(sigBlob, sigBlob + sigBlobLength), testing::ContainerEq(std::vector(signature.begin(), signature.end())));
}