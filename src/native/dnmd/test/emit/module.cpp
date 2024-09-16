#include "emit.hpp"

TEST(Module, ModuleNameExcludesDirectoryWin32Paths)
{
    WSTR_string moduleName = W("C:\\foo\\bar\\baz.dll");
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    ASSERT_EQ(S_OK, emit->SetModuleProps(moduleName.c_str()));

    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));

    WSTR_string readModuleName;
    readModuleName.resize(moduleName.capacity() + 1);
    ULONG readModuleNameLength;

    GUID mvid;
    ASSERT_EQ(S_OK, import->GetScopeProps(&readModuleName[0], (ULONG)readModuleName.capacity(), &readModuleNameLength, & mvid));

    EXPECT_EQ(W("baz.dll"), readModuleName.substr(0, readModuleNameLength - 1));
}

TEST(Module, ModuleNameExcludesDirectoryUnixPaths)
{
    WSTR_string moduleName = W("/home/foo/bar/baz.dll");
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    ASSERT_EQ(S_OK, emit->SetModuleProps(moduleName.c_str()));

    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));

    WSTR_string readModuleName;
    readModuleName.resize(moduleName.capacity() + 1);
    ULONG readModuleNameLength;

    GUID mvid;
    ASSERT_EQ(S_OK, import->GetScopeProps(&readModuleName[0], (ULONG)readModuleName.capacity(), &readModuleNameLength, & mvid));

    EXPECT_EQ(W("baz.dll"), readModuleName.substr(0, readModuleNameLength - 1));
}

TEST(Module, ModuleNameWithoutDirectory)
{
    WSTR_string moduleName = W("baz.dll");
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    ASSERT_EQ(S_OK, emit->SetModuleProps(moduleName.c_str()));

    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));

    WSTR_string readModuleName;
    readModuleName.resize(moduleName.capacity() + 1);
    ULONG readModuleNameLength;

    GUID mvid;
    ASSERT_EQ(S_OK, import->GetScopeProps(&readModuleName[0], (ULONG)readModuleName.capacity(), &readModuleNameLength, & mvid));

    EXPECT_EQ(moduleName.length(), readModuleNameLength - 1);
    EXPECT_EQ(moduleName, readModuleName.substr(0, readModuleNameLength - 1));
}

TEST(Module, EmptyName)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    ASSERT_EQ(S_OK, emit->SetModuleProps(W("")));

    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));

    std::array<WCHAR, 10> readModuleName;
    ULONG readModuleNameLength;

    GUID mvid;
    ASSERT_EQ(S_OK, import->GetScopeProps(&readModuleName[0], (ULONG)readModuleName.size(), & readModuleNameLength, & mvid));

    EXPECT_EQ(0, readModuleNameLength);
    EXPECT_EQ('\0', readModuleName[0]);
}

TEST(Module, NullName)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    ASSERT_EQ(S_OK, emit->SetModuleProps(nullptr));
}