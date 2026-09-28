#include "emit.hpp"

#include <array>

TEST(Import, TypeDefWithoutAssemblyManifestInSameModule)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));

    mdToken implements = mdTokenNil;
    mdTypeDef typeDef;
    ASSERT_EQ(S_OK, emit->DefineTypeDef(W("SourceType"), tdPublic, mdTypeDefNil, &implements, &typeDef));

    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));

    mdTypeRef imported = mdTypeRefNil;
    ASSERT_EQ(S_OK, emit->DefineImportType(nullptr, nullptr, 0, import.p, typeDef, nullptr, &imported));
    EXPECT_EQ(typeDef, imported);
}

TEST(Import, TypeDefWithoutAssemblyManifestInDifferentModule)
{
    minipal::com_ptr<IMetaDataEmit> source;
    minipal::com_ptr<IMetaDataEmit> target;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(source));
    ASSERT_NO_FATAL_FAILURE(CreateEmit(target));

    mdToken implements = mdTokenNil;
    mdTypeDef typeDef;
    ASSERT_EQ(S_OK, source->DefineTypeDef(W("SourceType"), tdPublic, mdTypeDefNil, &implements, &typeDef));

    minipal::com_ptr<IMetaDataImport> sourceImport;
    minipal::com_ptr<IMetaDataAssemblyEmit> targetAssemblyEmit;
    ASSERT_EQ(S_OK, source->QueryInterface(IID_IMetaDataImport, (void**)&sourceImport));
    ASSERT_EQ(S_OK, target->QueryInterface(IID_IMetaDataAssemblyEmit, (void**)&targetAssemblyEmit));

    mdTypeRef imported = mdTypeRefNil;
    EXPECT_EQ(E_UNEXPECTED, target->DefineImportType(nullptr, nullptr, 0, sourceImport.p, typeDef, targetAssemblyEmit.p, &imported));
}

TEST(Import, TypeRefWithoutAssemblyManifestInDifferentModule)
{
    minipal::com_ptr<IMetaDataEmit> source;
    minipal::com_ptr<IMetaDataEmit> target;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(source));
    ASSERT_NO_FATAL_FAILURE(CreateEmit(target));

    mdTypeRef typeRef;
    ASSERT_EQ(S_OK, source->DefineTypeRefByName(mdTokenNil, W("SourceType"), &typeRef));
    ASSERT_EQ(1u, RidFromToken(typeRef));

    minipal::com_ptr<IMetaDataImport> sourceImport;
    minipal::com_ptr<IMetaDataAssemblyEmit> targetAssemblyEmit;
    ASSERT_EQ(S_OK, source->QueryInterface(IID_IMetaDataImport, (void**)&sourceImport));
    ASSERT_EQ(S_OK, target->QueryInterface(IID_IMetaDataAssemblyEmit, (void**)&targetAssemblyEmit));

    // The TypeDefOrRef coded index for TypeRef RID 1 is 5.
    std::array<uint8_t, 3> signature = {IMAGE_CEE_CS_CALLCONV_FIELD, ELEMENT_TYPE_CLASS, 5};
    std::array<uint8_t, 16> translated{};
    ULONG translatedLength = 0;
    EXPECT_EQ(E_UNEXPECTED, target->TranslateSigWithScope(nullptr, nullptr, 0, sourceImport.p,
        signature.data(), (ULONG)signature.size(), targetAssemblyEmit.p, target.p,
        translated.data(), (ULONG)translated.size(), &translatedLength));
}
