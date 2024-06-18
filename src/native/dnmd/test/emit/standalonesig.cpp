#include "emit.hpp"
#include <array>
#include <vector>
#include <gmock/gmock.h>

TEST(StandaloneSig, Define)
{
    dncp::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdSignature sig;
    std::array<uint8_t, 3> signature = {0x01, 0x02, 0x03};
    ASSERT_EQ(S_OK, emit->GetTokenFromSig(signature.data(), (ULONG)signature.size(), &sig));
    ASSERT_EQ(1, RidFromToken(sig));
    ASSERT_EQ(mdtSignature, TypeFromToken(sig));

    dncp::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));

    PCCOR_SIGNATURE sigBlob;
    ULONG sigBlobLength;
    ASSERT_EQ(S_OK, import->GetSigFromToken(sig, &sigBlob, &sigBlobLength));
    EXPECT_THAT(std::vector(sigBlob, sigBlob + sigBlobLength), testing::ContainerEq(std::vector(signature.begin(), signature.end())));
}