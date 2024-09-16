#include "emit.hpp"

TEST(FieldRva, Define)
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
    uint32_t rva = 0x424242;
    ASSERT_EQ(S_OK, emit->SetFieldRVA(field, rva));

    // Read the field marshal signature
    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));

    DWORD readRva = 0;
    DWORD implFlags = 0;
    ASSERT_EQ(S_OK, import->GetRVA(field, & readRva, & implFlags));

    EXPECT_EQ(rva, readRva);
    EXPECT_EQ(0, implFlags);
}