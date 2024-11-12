#include "emit.hpp"

TEST(Assembly, DefineNoPublicKey)
{
    minipal::com_ptr<IMetaDataAssemblyEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdAssembly assembly;

    WSTR_string name = W("AssemblyName");
    ASSEMBLYMETADATA assemblyMetadata;
    assemblyMetadata.usMajorVersion = 1;
    assemblyMetadata.usMinorVersion = 2;
    assemblyMetadata.usBuildNumber = 3;
    assemblyMetadata.usRevisionNumber = 4;
    assemblyMetadata.szLocale = const_cast<LPWSTR>(W("en-us"));
    assemblyMetadata.cbLocale = 5;
    uint32_t hashAlgId = 4;
    ASSERT_EQ(S_OK, emit->DefineAssembly(nullptr, 0, hashAlgId, name.c_str(), &assemblyMetadata, 0, &assembly));
    ASSERT_EQ(1, RidFromToken(assembly));
    ASSERT_EQ(mdtAssembly, TypeFromToken(assembly));

    minipal::com_ptr<IMetaDataAssemblyImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataAssemblyImport, (void**)&import));

    ASSEMBLYMETADATA metadata;
    std::unique_ptr<WCHAR[]> localeName = std::make_unique<WCHAR[]>(20);
    metadata.szLocale = localeName.get();
    metadata.cbLocale = 20;
    ULONG importHashAlgId;
    DWORD assemblyFlags;
    WSTR_string assemblyName;
    assemblyName.resize(name.capacity() + 1);
    ULONG assemblyNameLen;
    void const* publicKey;
    ULONG publicKeyLength;
    ASSERT_EQ(S_OK, import->GetAssemblyProps(assembly, &publicKey, &publicKeyLength, &importHashAlgId, &assemblyName[0], (ULONG)assemblyName.capacity(), &assemblyNameLen, &metadata, & assemblyFlags));
    EXPECT_EQ(0, assemblyFlags);
    EXPECT_EQ(W("AssemblyName"), assemblyName.substr(0, assemblyNameLen - 1));
    EXPECT_EQ(1, metadata.usMajorVersion);
    EXPECT_EQ(2, metadata.usMinorVersion);
    EXPECT_EQ(3, metadata.usBuildNumber);
    EXPECT_EQ(4, metadata.usRevisionNumber);

    WSTR_string locale{ metadata.szLocale };
    EXPECT_EQ(W("en-us"), locale);
    EXPECT_EQ(locale.length() + 1, metadata.cbLocale);
}
