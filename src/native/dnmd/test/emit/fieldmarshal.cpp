#include "emit.hpp"

#include <gmock/gmock.h>

TEST(FieldMarshal, DefineAndDelete)
{
    // Define a field
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdFieldDef field;
    mdTypeDef type;
    ASSERT_EQ(S_OK, emit->DefineTypeDef(W("Type"), tdPublic, TokenFromRid(1, mdtTypeDef), nullptr, &type));
    std::array<uint8_t, 4> signature = { IMAGE_CEE_CS_CALLCONV_FIELD, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I4 };
    ASSERT_EQ(S_OK, emit->DefineField(type, W("Field"), fdPublic, signature.data(), (ULONG)signature.size(), 0, nullptr, 0, &field));

    // Define the field marshal signature
    std::array<uint8_t, 1> marshalSignature = { NATIVE_TYPE_I4 };
    ASSERT_EQ(S_OK, emit->SetFieldMarshal(field, marshalSignature.data(), (ULONG)marshalSignature.size()));

    // Read the field marshal signature
    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));

    ULONG readMarshalLength;
    PCCOR_SIGNATURE readMarshal;
    ASSERT_EQ(S_OK, import->GetFieldMarshal(field, & readMarshal, & readMarshalLength));

    EXPECT_EQ(marshalSignature.size(), readMarshalLength);
    EXPECT_THAT(std::vector<uint8_t>(readMarshal, readMarshal + readMarshalLength), testing::ContainerEq(std::vector<uint8_t>(marshalSignature.begin(), marshalSignature.end())));

    // Delete the field marshal entry
    ASSERT_EQ(S_OK, emit->DeleteFieldMarshal(field));

    // Verify we can't find the field marshal entry after deletion
    ASSERT_EQ(CLDB_E_RECORD_NOTFOUND, import->GetFieldMarshal(field, & readMarshal, & readMarshalLength));
}