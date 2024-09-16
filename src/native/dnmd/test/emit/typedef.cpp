#include "emit.hpp"

TEST(TypeDef, Define)
{
    WSTR_string name = W("Foo");
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdTypeDef typeDef;
    mdToken implements = mdTokenNil;

    ASSERT_EQ(S_OK, emit->DefineTypeDef(name.c_str(), 0, mdTypeDefNil, &implements, &typeDef));

    // The first type is the <Module> type,
    // so the second type is the one we just defined.
    ASSERT_EQ(2, RidFromToken(typeDef));
    ASSERT_EQ(mdtTypeDef, TypeFromToken(typeDef));

    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));

    WSTR_string readName;
    readName.resize(name.capacity() + 1);
    ULONG readNameLength;
    DWORD typeDefFlags;
    mdToken extends;
    ASSERT_EQ(S_OK, import->GetTypeDefProps(typeDef, &readName[0], (ULONG)readName.capacity(), &readNameLength, &typeDefFlags, &extends));
    EXPECT_EQ(name, readName.substr(0, readNameLength - 1));
    EXPECT_EQ(0, typeDefFlags);
    EXPECT_EQ(mdTypeRefNil, extends);

    HCORENUM hEnum = nullptr;
    mdFieldDef field;
    ULONG count;
    EXPECT_EQ(S_FALSE, import->EnumFields(&hEnum, typeDef, &field, 1, &count));
    import->CloseEnum(hEnum);

    hEnum = nullptr;
    mdMethodDef method;
    EXPECT_EQ(S_FALSE, import->EnumMethods(&hEnum, typeDef, &method, 1, &count));
    import->CloseEnum(hEnum);
}

TEST(TypeDef, DefineWithInterfaces)
{
    WSTR_string name = W("Foo");
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdTypeDef typeDef;
    mdToken implements[] = { TokenFromRid(1, mdtTypeRef),  mdTokenNil };

    ASSERT_EQ(S_OK, emit->DefineTypeDef(name.c_str(), 0, mdTypeDefNil, implements, &typeDef));

    // The first type is the <Module> type,
    // so the second type is the one we just defined.
    ASSERT_EQ(2, RidFromToken(typeDef));
    ASSERT_EQ(mdtTypeDef, TypeFromToken(typeDef));

    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));

    HCORENUM hEnum = nullptr;
    mdInterfaceImpl interfaceImpls[2] = {};
    ULONG count;
    ASSERT_EQ(S_OK, import->EnumInterfaceImpls(&hEnum, typeDef, interfaceImpls, 2, &count));
    ASSERT_EQ(1, count);
    EXPECT_EQ(TokenFromRid(1, mdtInterfaceImpl), interfaceImpls[0]);
    EXPECT_EQ(0, interfaceImpls[1]); // The second element should not be touched.
    import->CloseEnum(hEnum);

    mdTypeDef classType;
    mdToken interfaceType;
    ASSERT_EQ(S_OK, import->GetInterfaceImplProps(interfaceImpls[0], &classType, &interfaceType));
    EXPECT_EQ(typeDef, classType);
    EXPECT_EQ(TokenFromRid(1, mdtTypeRef), interfaceType);

}

TEST(TypeDef, DefineWithBase)
{
    WSTR_string name = W("Foo");
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdTypeDef typeDef;
    mdToken base = TokenFromRid(1, mdtTypeRef);
    mdToken implements = mdTokenNil;

    ASSERT_EQ(S_OK, emit->DefineTypeDef(name.c_str(), 0, base, &implements, &typeDef));

    // The first type is the <Module> type,
    // so the second type is the one we just defined.
    ASSERT_EQ(2, RidFromToken(typeDef));
    ASSERT_EQ(mdtTypeDef, TypeFromToken(typeDef));

    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));

    WSTR_string readName;
    readName.resize(name.capacity() + 1);
    ULONG readNameLength;
    DWORD typeDefFlags;
    mdToken extends;
    ASSERT_EQ(S_OK, import->GetTypeDefProps(typeDef, &readName[0], (ULONG)readName.capacity(), &readNameLength, &typeDefFlags, &extends));
    EXPECT_EQ(name, readName.substr(0, readNameLength - 1));
    EXPECT_EQ(0, typeDefFlags);
    EXPECT_EQ(base, extends);
}

TEST(TypeDef, NestedDefine)
{
    WSTR_string name = W("Foo");
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdTypeDef outerTypeDef, typeDef;
    mdToken base = TokenFromRid(1, mdtTypeRef);
    mdToken outerImplements = mdTokenNil;
    mdToken implements[] = { TokenFromRid(1, mdtTypeRef),  mdTokenNil };

    ASSERT_EQ(S_OK, emit->DefineTypeDef(name.c_str(), 0, mdTypeDefNil, &outerImplements, &outerTypeDef));
    ASSERT_EQ(S_OK, emit->DefineNestedType(name.c_str(), 0, base, implements, outerTypeDef, &typeDef));

    // The first type is the <Module> type,
    // the second is the outer type,
    // so the third is the nested type.
    ASSERT_EQ(3, RidFromToken(typeDef));
    ASSERT_EQ(mdtTypeDef, TypeFromToken(typeDef));

    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));

    WSTR_string readName;
    readName.resize(name.capacity() + 1);
    ULONG readNameLength;
    DWORD typeDefFlags;
    mdToken extends;
    ASSERT_EQ(S_OK, import->GetTypeDefProps(typeDef, &readName[0], (ULONG)readName.capacity(), &readNameLength, &typeDefFlags, &extends));
    EXPECT_EQ(name, readName.substr(0, readNameLength - 1));
    EXPECT_EQ(0, typeDefFlags);
    EXPECT_EQ(base, extends);

    HCORENUM hEnum = nullptr;
    mdInterfaceImpl interfaceImpls[2] = {};
    ULONG count;
    ASSERT_EQ(S_OK, import->EnumInterfaceImpls(&hEnum, typeDef, interfaceImpls, 2, &count));
    ASSERT_EQ(1, count);
    EXPECT_EQ(TokenFromRid(1, mdtInterfaceImpl), interfaceImpls[0]);
    EXPECT_EQ(0, interfaceImpls[1]); // The second element should not be touched.
    import->CloseEnum(hEnum);

    mdTypeDef classType;
    mdToken interfaceType;
    ASSERT_EQ(S_OK, import->GetInterfaceImplProps(interfaceImpls[0], &classType, &interfaceType));
    EXPECT_EQ(typeDef, classType);
    EXPECT_EQ(TokenFromRid(1, mdtTypeRef), interfaceType);

    mdTypeDef enclosing;
    ASSERT_EQ(S_OK, import->GetNestedClassProps(typeDef, &enclosing));
    EXPECT_EQ(outerTypeDef, enclosing);
}

TEST(TypeDef, SetProps)
{
    WSTR_string name = W("Foo");
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdTypeDef typeDef;
    mdToken initialImplements[] = { TokenFromRid(2, mdtTypeSpec), mdTokenNil };
    mdToken extends = TokenFromRid(1, mdtTypeRef);

    ASSERT_EQ(S_OK, emit->DefineTypeDef(name.c_str(), 0, mdTypeDefNil, initialImplements, &typeDef));

    mdToken implements[] = { TokenFromRid(4, mdtTypeRef),  mdTokenNil };
    ASSERT_EQ(S_OK, emit->SetTypeDefProps(typeDef, tdAbstract, extends, implements));

    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));

    WSTR_string readName;
    readName.resize(name.capacity() + 1);
    ULONG readNameLength;
    DWORD typeDefFlags;
    mdToken readExtends;
    ASSERT_EQ(S_OK, import->GetTypeDefProps(typeDef, &readName[0], (ULONG)readName.capacity(), &readNameLength, &typeDefFlags, &readExtends));
    EXPECT_EQ(name, readName.substr(0, readNameLength - 1));
    EXPECT_EQ(tdAbstract, typeDefFlags);
    EXPECT_EQ(extends, readExtends);

    HCORENUM hEnum = nullptr;
    mdInterfaceImpl interfaceImpls[2] = {};
    ULONG count;
    ASSERT_EQ(S_OK, import->EnumInterfaceImpls(&hEnum, typeDef, interfaceImpls, 2, &count));
    ASSERT_EQ(1, count);
    // We should have created a new entry for the new interface implementation,
    // not modified the existing one.
    EXPECT_EQ(TokenFromRid(2, mdtInterfaceImpl), interfaceImpls[0]);
    EXPECT_EQ(0, interfaceImpls[1]); // The second element should not be touched.
    import->CloseEnum(hEnum);

    mdTypeDef classType;
    mdToken interfaceType;
    ASSERT_EQ(S_OK, import->GetInterfaceImplProps(interfaceImpls[0], &classType, &interfaceType));
    EXPECT_EQ(typeDef, classType);
    EXPECT_EQ(implements[0], interfaceType);
}
