#include "emit.hpp"

TEST(ModuleRef, Define)
{
    WSTR_string name = W("Foo");
    dncp::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdModuleRef moduleRef;

    ASSERT_EQ(S_OK, emit->DefineModuleRef(name.c_str(), &moduleRef));

    ASSERT_EQ(1, RidFromToken(moduleRef));
    ASSERT_EQ(mdtModuleRef, TypeFromToken(moduleRef));

    dncp::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));

    WSTR_string readName;
    readName.resize(name.capacity() + 1);
    ULONG readNameLength;
    ASSERT_EQ(S_OK, import->GetModuleRefProps(moduleRef, readName.data(), (ULONG)readName.capacity(), &readNameLength));
    EXPECT_EQ(name, readName.substr(0, readNameLength - 1));
}