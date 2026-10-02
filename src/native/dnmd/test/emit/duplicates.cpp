// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "emit.hpp"
#include <metadataemithelper.h>

#include <array>
#include <vector>

namespace
{
    constexpr uint32_t ReflectionEmitChecks =
        MDDupDefault | MDDupTypeDef | MDDupModuleRef | MDDupExportedType |
        MDDupAssemblyRef | MDDupPermission | MDDupFile;

    VARIANT DuplicateOption(uint32_t flags)
    {
        VARIANT option{};
        V_VT(&option) = VT_UI4;
        V_UI4(&option) = flags;
        return option;
    }

    void CreateDispenser(uint32_t checks, minipal::com_ptr<IMetaDataDispenserEx>& dispenser)
    {
        ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenserEx, (void**)&dispenser));
        VARIANT option = DuplicateOption(checks);
        ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataCheckDuplicatesFor, &option));
    }

    void DefineScope(IMetaDataDispenserEx* dispenser, minipal::com_ptr<IMetaDataEmit>& emit)
    {
        ASSERT_EQ(S_OK, dispenser->DefineScope(CLSID_CorMetaDataRuntime, 0, IID_IMetaDataEmit, (IUnknown**)&emit));
    }

    ASSEMBLYMETADATA AssemblyVersion()
    {
        ASSEMBLYMETADATA metadata{};
        metadata.usMajorVersion = 1;
        metadata.usMinorVersion = 2;
        metadata.usBuildNumber = 3;
        metadata.usRevisionNumber = 4;
        metadata.szLocale = const_cast<LPWSTR>(W("en-us"));
        metadata.cbLocale = 5;
        return metadata;
    }
}

TEST(CheckDuplicates, OptionValidatesVariantAndSupportedBits)
{
    minipal::com_ptr<IMetaDataDispenserEx> dispenser;
    ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenserEx, (void**)&dispenser));

    VARIANT option{};
    ASSERT_EQ(S_OK, dispenser->GetOption(MetaDataCheckDuplicatesFor, &option));
    EXPECT_EQ(VT_UI4, V_VT(&option));
    EXPECT_EQ(MDDupDefault, V_UI4(&option));

    option = DuplicateOption(ReflectionEmitChecks);
    ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataCheckDuplicatesFor, &option));
    V_VT(&option) = VT_EMPTY;
    ASSERT_EQ(S_OK, dispenser->GetOption(MetaDataCheckDuplicatesFor, &option));
    EXPECT_EQ(VT_UI4, V_VT(&option));
    EXPECT_EQ(ReflectionEmitChecks, V_UI4(&option));

    V_VT(&option) = VT_I4;
    EXPECT_EQ(E_INVALIDARG, dispenser->SetOption(MetaDataCheckDuplicatesFor, &option));
    for (uint32_t unsupported : { uint32_t(MDDupEvent), uint32_t(MDDupAssembly), uint32_t(MDDupAll) })
    {
        option = DuplicateOption(unsupported);
        EXPECT_EQ(E_INVALIDARG, dispenser->SetOption(MetaDataCheckDuplicatesFor, &option));
    }
    EXPECT_EQ(E_INVALIDARG, dispenser->SetOption(MetaDataCheckDuplicatesFor, nullptr));
    EXPECT_EQ(E_INVALIDARG, dispenser->GetOption(MetaDataCheckDuplicatesFor, nullptr));
    EXPECT_EQ(E_INVALIDARG, dispenser->SetOption(IID_IMetaDataImport, &option));
    EXPECT_EQ(E_INVALIDARG, dispenser->GetOption(IID_IMetaDataImport, &option));

    ASSERT_EQ(S_OK, dispenser->GetOption(MetaDataCheckDuplicatesFor, &option));
    EXPECT_EQ(ReflectionEmitChecks, V_UI4(&option));
    option = DuplicateOption(MDNoDupChecks);
    EXPECT_EQ(S_OK, dispenser->SetOption(MetaDataCheckDuplicatesFor, &option));
    EXPECT_EQ(S_OK, dispenser->GetOption(MetaDataCheckDuplicatesFor, &option));
    EXPECT_EQ(MDNoDupChecks, V_UI4(&option));
}

TEST(CheckDuplicates, TypeDefUsesNameNamespaceAndEnclosingType)
{
    minipal::com_ptr<IMetaDataDispenserEx> dispenser;
    ASSERT_NO_FATAL_FAILURE(CreateDispenser(MDDupTypeDef, dispenser));
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(DefineScope(dispenser.p, emit));

    mdTypeDef outer1, outer2, duplicate, differentNamespace;
    ASSERT_EQ(S_OK, emit->DefineTypeDef(W("N.Outer"), tdPublic, mdTypeDefNil, nullptr, &outer1));
    ASSERT_EQ(META_S_DUPLICATE, emit->DefineTypeDef(W("N.Outer"), tdAbstract, mdTypeDefNil, nullptr, &duplicate));
    EXPECT_EQ(outer1, duplicate);
    ASSERT_EQ(S_OK, emit->DefineTypeDef(W("Other.Outer"), tdPublic, mdTypeDefNil, nullptr, &differentNamespace));
    ASSERT_EQ(S_OK, emit->DefineTypeDef(W("N.Outer2"), tdPublic, mdTypeDefNil, nullptr, &outer2));

    mdTypeDef nested1, nested2, topLevel;
    ASSERT_EQ(S_OK, emit->DefineNestedType(W("N.Inner"), tdNestedPublic, mdTypeDefNil,
        nullptr, outer1, &nested1));
    ASSERT_EQ(META_S_DUPLICATE, emit->DefineNestedType(W("N.Inner"), tdNestedPublic, mdTypeDefNil,
        nullptr, outer1, &duplicate));
    EXPECT_EQ(nested1, duplicate);
    ASSERT_EQ(S_OK, emit->DefineNestedType(W("N.Inner"), tdNestedPublic, mdTypeDefNil,
        nullptr, outer2, &nested2));
    EXPECT_NE(nested1, nested2);
    ASSERT_EQ(S_OK, emit->DefineTypeDef(W("N.Inner"), tdPublic, mdTypeDefNil, nullptr, &topLevel));
    EXPECT_NE(nested1, topLevel);
    ASSERT_EQ(META_S_DUPLICATE, emit->DefineTypeDef(W("N.Inner"), tdPublic, mdTypeDefNil,
        nullptr, &duplicate));
    EXPECT_EQ(topLevel, duplicate);
    EXPECT_EQ(3u, RidFromToken(differentNamespace));

    mdModuleRef module1, module2;
    ASSERT_EQ(S_OK, emit->DefineModuleRef(W("UncheckedModule"), &module1));
    ASSERT_EQ(S_OK, emit->DefineModuleRef(W("UncheckedModule"), &module2));
    EXPECT_NE(module1, module2);
}

TEST(CheckDuplicates, TypeRefAndModuleRefUseTheirOwnKeys)
{
    minipal::com_ptr<IMetaDataDispenserEx> dispenser;
    ASSERT_NO_FATAL_FAILURE(CreateDispenser(MDDupTypeRef | MDDupModuleRef, dispenser));
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(DefineScope(dispenser.p, emit));

    mdModuleRef module, duplicateModule, otherModule;
    ASSERT_EQ(S_OK, emit->DefineModuleRef(W("Module"), &module));
    ASSERT_EQ(META_S_DUPLICATE, emit->DefineModuleRef(W("Module"), &duplicateModule));
    EXPECT_EQ(module, duplicateModule);
    ASSERT_EQ(S_OK, emit->DefineModuleRef(W("module"), &otherModule));
    EXPECT_NE(module, otherModule);

    mdTypeRef first, duplicate, otherScope, otherNamespace;
    ASSERT_EQ(S_OK, emit->DefineTypeRefByName(module, W("N.Type"), &first));
    ASSERT_EQ(META_S_DUPLICATE, emit->DefineTypeRefByName(module, W("N.Type"), &duplicate));
    EXPECT_EQ(first, duplicate);
    ASSERT_EQ(S_OK, emit->DefineTypeRefByName(otherModule, W("N.Type"), &otherScope));
    ASSERT_EQ(S_OK, emit->DefineTypeRefByName(module, W("Other.Type"), &otherNamespace));
    EXPECT_NE(first, otherScope);
    EXPECT_NE(first, otherNamespace);
}

TEST(CheckDuplicates, DefaultReferenceAndSignatureChecks)
{
    minipal::com_ptr<IMetaDataDispenserEx> dispenser;
    ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenserEx, (void**)&dispenser));
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(DefineScope(dispenser.p, emit));
    minipal::com_ptr<IMetaDataEmit2> emit2;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataEmit2, (void**)&emit2));

    mdTypeDef type1, type2;
    ASSERT_EQ(S_OK, emit->DefineTypeDef(W("UncheckedType"), tdPublic, mdTypeDefNil, nullptr, &type1));
    ASSERT_EQ(S_OK, emit->DefineTypeDef(W("UncheckedType"), tdPublic, mdTypeDefNil, nullptr, &type2));
    EXPECT_NE(type1, type2);

    mdTypeRef parent, duplicateTypeRef;
    ASSERT_EQ(S_OK, emit->DefineTypeRefByName(TokenFromRid(1, mdtModule), W("N.Type"), &parent));
    ASSERT_EQ(META_S_DUPLICATE, emit->DefineTypeRefByName(TokenFromRid(1, mdtModule),
        W("N.Type"), &duplicateTypeRef));
    EXPECT_EQ(parent, duplicateTypeRef);

    const std::array<uint8_t, 2> fieldSig{ IMAGE_CEE_CS_CALLCONV_FIELD, ELEMENT_TYPE_I4 };
    const std::array<uint8_t, 2> otherSig{ IMAGE_CEE_CS_CALLCONV_FIELD, ELEMENT_TYPE_I8 };
    mdMemberRef member, duplicateMember, otherMember;
    ASSERT_EQ(S_OK, emit->DefineMemberRef(parent, W("Field"), fieldSig.data(),
        (ULONG)fieldSig.size(), &member));
    ASSERT_EQ(META_S_DUPLICATE, emit->DefineMemberRef(parent, W("Field"), fieldSig.data(),
        (ULONG)fieldSig.size(), &duplicateMember));
    EXPECT_EQ(member, duplicateMember);
    ASSERT_EQ(S_OK, emit->DefineMemberRef(parent, W("Field"), otherSig.data(),
        (ULONG)otherSig.size(), &otherMember));
    EXPECT_NE(member, otherMember);
    ASSERT_EQ(S_OK, emit->DefineMemberRef(TokenFromRid(1, mdtTypeDef), W("Field"),
        fieldSig.data(), (ULONG)fieldSig.size(), &otherMember));
    EXPECT_NE(member, otherMember);

    mdSignature sig, duplicateSig, differentSig;
    ASSERT_EQ(S_OK, emit->GetTokenFromSig(fieldSig.data(), (ULONG)fieldSig.size(), &sig));
    ASSERT_EQ(META_S_DUPLICATE, emit->GetTokenFromSig(fieldSig.data(), (ULONG)fieldSig.size(), &duplicateSig));
    EXPECT_EQ(sig, duplicateSig);
    ASSERT_EQ(S_OK, emit->GetTokenFromSig(otherSig.data(), (ULONG)otherSig.size(), &differentSig));
    EXPECT_NE(sig, differentSig);

    mdTypeSpec spec, duplicateSpec, differentSpec;
    ASSERT_EQ(S_OK, emit->GetTokenFromTypeSpec(fieldSig.data(), (ULONG)fieldSig.size(), &spec));
    ASSERT_EQ(S_OK, emit->GetTokenFromTypeSpec(fieldSig.data(), (ULONG)fieldSig.size(), &duplicateSpec));
    EXPECT_EQ(spec, duplicateSpec);
    ASSERT_EQ(S_OK, emit->GetTokenFromTypeSpec(otherSig.data(), (ULONG)otherSig.size(), &differentSpec));
    EXPECT_EQ(RidFromToken(spec) + 1, RidFromToken(differentSpec));

    const std::array<uint8_t, 3> methodSig{ IMAGE_CEE_CS_CALLCONV_DEFAULT, 0, ELEMENT_TYPE_VOID };
    const std::array<uint8_t, 3> instantiation{ IMAGE_CEE_CS_CALLCONV_GENERICINST, 1, ELEMENT_TYPE_I4 };
    const std::array<uint8_t, 3> otherInstantiation{ IMAGE_CEE_CS_CALLCONV_GENERICINST, 1, ELEMENT_TYPE_I8 };
    mdMethodDef method, otherMethod;
    ASSERT_EQ(S_OK, emit->DefineMethod(TokenFromRid(1, mdtTypeDef), W("Method"), 0,
        methodSig.data(), (ULONG)methodSig.size(), 0, 0, &method));
    ASSERT_EQ(S_OK, emit->DefineMethod(TokenFromRid(1, mdtTypeDef), W("OtherMethod"), 0,
        methodSig.data(), (ULONG)methodSig.size(), 0, 0, &otherMethod));

    mdMethodSpec methodSpec, duplicateMethodSpec, otherMethodSpec;
    ASSERT_EQ(S_OK, emit2->DefineMethodSpec(method, instantiation.data(),
        (ULONG)instantiation.size(), &methodSpec));
    ASSERT_EQ(META_S_DUPLICATE, emit2->DefineMethodSpec(method, instantiation.data(),
        (ULONG)instantiation.size(), &duplicateMethodSpec));
    EXPECT_EQ(methodSpec, duplicateMethodSpec);
    ASSERT_EQ(S_OK, emit2->DefineMethodSpec(method, otherInstantiation.data(),
        (ULONG)otherInstantiation.size(), &otherMethodSpec));
    EXPECT_NE(methodSpec, otherMethodSpec);
    ASSERT_EQ(S_OK, emit2->DefineMethodSpec(otherMethod, instantiation.data(),
        (ULONG)instantiation.size(), &otherMethodSpec));
    EXPECT_NE(methodSpec, otherMethodSpec);
}

TEST(CheckDuplicates, PermissionAndFileIgnorePayloadButUseParentAndAction)
{
    minipal::com_ptr<IMetaDataDispenserEx> dispenser;
    ASSERT_NO_FATAL_FAILURE(CreateDispenser(MDDupPermission | MDDupFile, dispenser));
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(DefineScope(dispenser.p, emit));
    minipal::com_ptr<IMetaDataAssemblyEmit> assemblyEmit;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataAssemblyEmit, (void**)&assemblyEmit));

    mdTypeDef type1, type2;
    ASSERT_EQ(S_OK, emit->DefineTypeDef(W("Parent"), tdPublic, mdTypeDefNil, nullptr, &type1));
    ASSERT_EQ(S_OK, emit->DefineTypeDef(W("OtherParent"), tdPublic, mdTypeDefNil, nullptr, &type2));

    uint8_t firstBlob = 1, otherBlob = 2;
    mdPermission first, duplicate, other;
    ASSERT_EQ(S_OK, emit->DefinePermissionSet(type1, dclDemand, &firstBlob, 1, &first));
    ASSERT_EQ(META_S_DUPLICATE, emit->DefinePermissionSet(type1, dclDemand, &otherBlob, 1, &duplicate));
    EXPECT_EQ(first, duplicate);
    ASSERT_EQ(S_OK, emit->DefinePermissionSet(type1, dclAssert, &firstBlob, 1, &other));
    EXPECT_NE(first, other);
    ASSERT_EQ(S_OK, emit->DefinePermissionSet(type2, dclDemand, &firstBlob, 1, &other));
    EXPECT_NE(first, other);

    mdFile file, duplicateFile, otherFile;
    ASSERT_EQ(S_OK, assemblyEmit->DefineFile(W("File"), &firstBlob, 1, 0, &file));
    ASSERT_EQ(META_S_DUPLICATE, assemblyEmit->DefineFile(W("File"), &otherBlob, 1, ffContainsNoMetaData, &duplicateFile));
    EXPECT_EQ(file, duplicateFile);
    ASSERT_EQ(S_OK, assemblyEmit->DefineFile(W("OtherFile"), &firstBlob, 1, 0, &otherFile));
    EXPECT_NE(file, otherFile);
}

TEST(CheckDuplicates, AssemblyRefUsesIdentityAndEquivalentKeyForms)
{
    minipal::com_ptr<IMetaDataDispenserEx> dispenser;
    ASSERT_NO_FATAL_FAILURE(CreateDispenser(MDDupAssemblyRef, dispenser));
    minipal::com_ptr<IMetaDataAssemblyEmit> emit;
    ASSERT_EQ(S_OK, dispenser->DefineScope(CLSID_CorMetaDataRuntime, 0,
        IID_IMetaDataAssemblyEmit, (IUnknown**)&emit));

    ASSEMBLYMETADATA version = AssemblyVersion();
    uint8_t hash = 42;
    mdAssemblyRef first, duplicate, other;
    ASSERT_EQ(S_OK, emit->DefineAssemblyRef(nullptr, 0, W("Reference"), &version, nullptr, 0, 0, &first));
    ASSERT_EQ(META_S_DUPLICATE, emit->DefineAssemblyRef(nullptr, 0, W("Reference"),
        &version, &hash, 1, afRetargetable, &duplicate));
    EXPECT_EQ(first, duplicate);

    ASSEMBLYMETADATA otherVersion = version;
    otherVersion.usBuildNumber++;
    ASSERT_EQ(S_OK, emit->DefineAssemblyRef(nullptr, 0, W("Reference"), &otherVersion, nullptr, 0, 0, &other));
    EXPECT_NE(first, other);
    ASSEMBLYMETADATA otherCulture = version;
    otherCulture.szLocale = const_cast<LPWSTR>(W("fr"));
    otherCulture.cbLocale = 2;
    ASSERT_EQ(S_OK, emit->DefineAssemblyRef(nullptr, 0, W("Reference"), &otherCulture, nullptr, 0, 0, &other));
    EXPECT_NE(first, other);

    ASSERT_EQ(S_OK, emit->DefineAssemblyRef(nullptr, 0, W("mscorlib"), &version, nullptr, 0, 0, &first));
    ASSERT_EQ(META_S_DUPLICATE, emit->DefineAssemblyRef(nullptr, 0, W("mscorlib"),
        &otherVersion, nullptr, 0, 0, &duplicate));
    EXPECT_EQ(first, duplicate);

    ASSEMBLYMETADATA neutralCulture = version;
    neutralCulture.szLocale = nullptr;
    neutralCulture.cbLocale = 0;
    ASSERT_EQ(S_OK, emit->DefineAssemblyRef(nullptr, 0, W("Neutral"),
        &neutralCulture, nullptr, 0, 0, &first));
    neutralCulture.szLocale = const_cast<LPWSTR>(W(""));
    ASSERT_EQ(META_S_DUPLICATE, emit->DefineAssemblyRef(nullptr, 0, W("Neutral"),
        &neutralCulture, nullptr, 0, 0, &duplicate));
    EXPECT_EQ(first, duplicate);

    constexpr uint8_t ecmaKey[] = { 0, 0, 0, 0, 0, 0, 0, 0, 4, 0, 0, 0, 0, 0, 0, 0 };
    constexpr uint8_t ecmaToken[] = { 0xb7, 0x7a, 0x5c, 0x56, 0x19, 0x34, 0xe0, 0x89 };
    ASSERT_EQ(S_OK, emit->DefineAssemblyRef(ecmaKey, sizeof(ecmaKey), W("Keyed"),
        &version, nullptr, 0, afPublicKey, &first));
    ASSERT_EQ(META_S_DUPLICATE, emit->DefineAssemblyRef(ecmaToken, sizeof(ecmaToken),
        W("Keyed"), &version, nullptr, 0, 0, &duplicate));
    EXPECT_EQ(first, duplicate);
    ASSERT_EQ(S_OK, emit->DefineAssemblyRef(ecmaToken, sizeof(ecmaToken),
        W("OtherKeyed"), &version, nullptr, 0, 0, &other));
    ASSERT_EQ(META_S_DUPLICATE, emit->DefineAssemblyRef(ecmaKey, sizeof(ecmaKey),
        W("OtherKeyed"), &version, nullptr, 0, afPublicKey, &duplicate));
    EXPECT_EQ(other, duplicate);

    constexpr uint8_t fullKey[] = { 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 6 };
    constexpr uint8_t hashedToken[] = { 0xee, 0x63, 0x6b, 0xad, 0xc1, 0xb2, 0x96, 0x8d };
    ASSERT_EQ(S_OK, emit->DefineAssemblyRef(fullKey, sizeof(fullKey), W("Hashed"),
        &version, nullptr, 0, afPublicKey, &first));
    ASSERT_EQ(META_S_DUPLICATE, emit->DefineAssemblyRef(hashedToken, sizeof(hashedToken),
        W("Hashed"), &version, nullptr, 0, 0, &duplicate));
    EXPECT_EQ(first, duplicate);
}

TEST(CheckDuplicates, ExportedTypeDistinguishesNestedFromTopLevel)
{
    minipal::com_ptr<IMetaDataDispenserEx> dispenser;
    ASSERT_NO_FATAL_FAILURE(CreateDispenser(MDDupExportedType, dispenser));
    minipal::com_ptr<IMetaDataAssemblyEmit> emit;
    ASSERT_EQ(S_OK, dispenser->DefineScope(CLSID_CorMetaDataRuntime, 0,
        IID_IMetaDataAssemblyEmit, (IUnknown**)&emit));

    mdFile file1, file2;
    ASSERT_EQ(S_OK, emit->DefineFile(W("File1"), nullptr, 0, 0, &file1));
    ASSERT_EQ(S_OK, emit->DefineFile(W("File2"), nullptr, 0, 0, &file2));

    mdExportedType outer1, outer2, first, duplicate, other;
    ASSERT_EQ(S_OK, emit->DefineExportedType(W("N.Outer1"), file1, mdTypeDefNil,
        tdPublic, &outer1));
    ASSERT_EQ(S_OK, emit->DefineExportedType(W("N.Outer2"), file2, mdTypeDefNil,
        tdPublic, &outer2));
    ASSERT_EQ(S_OK, emit->DefineExportedType(W("N.Inner"), outer1, mdTypeDefNil,
        tdNestedPublic, &first));
    ASSERT_EQ(META_S_DUPLICATE, emit->DefineExportedType(W("N.Inner"), outer1,
        mdTypeDefNil, tdNestedPublic, &duplicate));
    EXPECT_EQ(first, duplicate);
    ASSERT_EQ(S_OK, emit->DefineExportedType(W("N.Inner"), outer2,
        mdTypeDefNil, tdNestedPublic, &other));
    EXPECT_NE(first, other);
    ASSERT_EQ(S_OK, emit->DefineExportedType(W("N.Inner"), file1,
        mdTypeDefNil, tdPublic, &first));
    ASSERT_EQ(META_S_DUPLICATE, emit->DefineExportedType(W("N.Inner"), file2,
        mdTypeDefNil, tdPublic, &duplicate));
    EXPECT_EQ(first, duplicate);
    ASSERT_EQ(S_OK, emit->DefineExportedType(W("Other.Inner"), file1,
        mdTypeDefNil, tdPublic, &other));
    EXPECT_NE(first, other);
}

TEST(CheckDuplicates, DisabledChecksAllowIdenticalRows)
{
    minipal::com_ptr<IMetaDataDispenserEx> dispenser;
    ASSERT_NO_FATAL_FAILURE(CreateDispenser(MDNoDupChecks, dispenser));
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(DefineScope(dispenser.p, emit));
    minipal::com_ptr<IMetaDataEmit2> emit2;
    minipal::com_ptr<IMetaDataAssemblyEmit> assemblyEmit;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataEmit2, (void**)&emit2));
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataAssemblyEmit, (void**)&assemblyEmit));

    mdTypeDef type1, type2;
    ASSERT_EQ(S_OK, emit->DefineTypeDef(W("Type"), tdPublic, mdTypeDefNil, nullptr, &type1));
    ASSERT_EQ(S_OK, emit->DefineTypeDef(W("Type"), tdPublic, mdTypeDefNil, nullptr, &type2));
    EXPECT_NE(type1, type2);
    mdTypeRef ref1, ref2;
    ASSERT_EQ(S_OK, emit->DefineTypeRefByName(TokenFromRid(1, mdtModule), W("Ref"), &ref1));
    ASSERT_EQ(S_OK, emit->DefineTypeRefByName(TokenFromRid(1, mdtModule), W("Ref"), &ref2));
    EXPECT_NE(ref1, ref2);
    mdModuleRef module1, module2;
    ASSERT_EQ(S_OK, emit->DefineModuleRef(W("Module"), &module1));
    ASSERT_EQ(S_OK, emit->DefineModuleRef(W("Module"), &module2));
    EXPECT_NE(module1, module2);

    const std::array<uint8_t, 2> signature{ IMAGE_CEE_CS_CALLCONV_FIELD, ELEMENT_TYPE_I4 };
    mdMemberRef member1, member2;
    ASSERT_EQ(S_OK, emit->DefineMemberRef(type1, W("Member"), signature.data(), (ULONG)signature.size(), &member1));
    ASSERT_EQ(S_OK, emit->DefineMemberRef(type1, W("Member"), signature.data(), (ULONG)signature.size(), &member2));
    EXPECT_NE(member1, member2);
    mdSignature sig1, sig2;
    ASSERT_EQ(S_OK, emit->GetTokenFromSig(signature.data(), (ULONG)signature.size(), &sig1));
    ASSERT_EQ(S_OK, emit->GetTokenFromSig(signature.data(), (ULONG)signature.size(), &sig2));
    EXPECT_NE(sig1, sig2);
    mdTypeSpec spec1, spec2;
    ASSERT_EQ(S_OK, emit->GetTokenFromTypeSpec(signature.data(), (ULONG)signature.size(), &spec1));
    ASSERT_EQ(S_OK, emit->GetTokenFromTypeSpec(signature.data(), (ULONG)signature.size(), &spec2));
    EXPECT_NE(spec1, spec2);
    mdMethodSpec inst1, inst2;
    ASSERT_EQ(S_OK, emit2->DefineMethodSpec(member1, signature.data(), (ULONG)signature.size(), &inst1));
    ASSERT_EQ(S_OK, emit2->DefineMethodSpec(member1, signature.data(), (ULONG)signature.size(), &inst2));
    EXPECT_NE(inst1, inst2);

    uint8_t payload = 1;
    mdPermission permission1, permission2;
    ASSERT_EQ(S_OK, emit->DefinePermissionSet(type1, dclDemand, &payload, 1, &permission1));
    ASSERT_EQ(S_OK, emit->DefinePermissionSet(type1, dclDemand, &payload, 1, &permission2));
    EXPECT_NE(permission1, permission2);
    mdFile file1, file2;
    ASSERT_EQ(S_OK, assemblyEmit->DefineFile(W("File"), nullptr, 0, 0, &file1));
    ASSERT_EQ(S_OK, assemblyEmit->DefineFile(W("File"), nullptr, 0, 0, &file2));
    EXPECT_NE(file1, file2);
    mdExportedType exported1, exported2;
    ASSERT_EQ(S_OK, assemblyEmit->DefineExportedType(W("Exported"), file1, mdTypeDefNil, tdPublic, &exported1));
    ASSERT_EQ(S_OK, assemblyEmit->DefineExportedType(W("Exported"), file1, mdTypeDefNil, tdPublic, &exported2));
    EXPECT_NE(exported1, exported2);
    ASSEMBLYMETADATA version = AssemblyVersion();
    mdAssemblyRef assembly1, assembly2;
    ASSERT_EQ(S_OK, assemblyEmit->DefineAssemblyRef(nullptr, 0, W("Assembly"), &version, nullptr, 0, 0, &assembly1));
    ASSERT_EQ(S_OK, assemblyEmit->DefineAssemblyRef(nullptr, 0, W("Assembly"), &version, nullptr, 0, 0, &assembly2));
    EXPECT_NE(assembly1, assembly2);
}

TEST(CheckDuplicates, ScopesCaptureOptionsIncludingWritableMemoryScopes)
{
    minipal::com_ptr<IMetaDataDispenserEx> dispenser;
    ASSERT_NO_FATAL_FAILURE(CreateDispenser(MDNoDupChecks, dispenser));
    minipal::com_ptr<IMetaDataEmit> unchecked;
    ASSERT_NO_FATAL_FAILURE(DefineScope(dispenser.p, unchecked));

    VARIANT option = DuplicateOption(MDDupTypeDef);
    ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataCheckDuplicatesFor, &option));
    VARIANT threadSafety = DuplicateOption(MDThreadSafetyOn);
    ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataThreadSafetyOptions, &threadSafety));
    minipal::com_ptr<IMetaDataEmit> checked;
    ASSERT_NO_FATAL_FAILURE(DefineScope(dispenser.p, checked));

    option = DuplicateOption(MDNoDupChecks);
    ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataCheckDuplicatesFor, &option));
    minipal::com_ptr<IMetaDataEmit> uncheckedAgain;
    ASSERT_NO_FATAL_FAILURE(DefineScope(dispenser.p, uncheckedAgain));

    mdTypeDef first, second;
    ASSERT_EQ(S_OK, unchecked->DefineTypeDef(W("Unchanged"), tdPublic, mdTypeDefNil, nullptr, &first));
    ASSERT_EQ(S_OK, unchecked->DefineTypeDef(W("Unchanged"), tdPublic, mdTypeDefNil, nullptr, &second));
    EXPECT_NE(first, second);
    ASSERT_EQ(S_OK, checked->DefineTypeDef(W("Persisted"), tdPublic, mdTypeDefNil, nullptr, &first));
    ASSERT_EQ(META_S_DUPLICATE, checked->DefineTypeDef(W("Persisted"), tdPublic, mdTypeDefNil, nullptr, &second));
    EXPECT_EQ(first, second);
    ASSERT_EQ(S_OK, uncheckedAgain->DefineTypeDef(W("Independent"), tdPublic, mdTypeDefNil, nullptr, &first));
    ASSERT_EQ(S_OK, uncheckedAgain->DefineTypeDef(W("Independent"), tdPublic, mdTypeDefNil, nullptr, &second));
    EXPECT_NE(first, second);

    DWORD saveSize;
    ASSERT_EQ(S_OK, checked->GetSaveSize(cssAccurate, &saveSize));
    std::vector<uint8_t> image(saveSize);
    ASSERT_EQ(S_OK, checked->SaveToMemory(image.data(), saveSize));

    option = DuplicateOption(MDDupTypeDef);
    ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataCheckDuplicatesFor, &option));
    minipal::com_ptr<IMetaDataEmit> openedChecked;
    ASSERT_EQ(S_OK, dispenser->OpenScopeOnMemory(image.data(), saveSize, ofCopyMemory,
        IID_IMetaDataEmit, (IUnknown**)&openedChecked));
    option = DuplicateOption(MDNoDupChecks);
    ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataCheckDuplicatesFor, &option));
    ASSERT_EQ(META_S_DUPLICATE, openedChecked->DefineTypeDef(W("Persisted"), tdPublic,
        mdTypeDefNil, nullptr, &second));
    EXPECT_EQ(TokenFromRid(2, mdtTypeDef), second);

    minipal::com_ptr<IMetaDataEmit> openedUnchecked;
    ASSERT_EQ(S_OK, dispenser->OpenScopeOnMemory(image.data(), saveSize, ofCopyMemory,
        IID_IMetaDataEmit, (IUnknown**)&openedUnchecked));
    ASSERT_EQ(S_OK, openedUnchecked->DefineTypeDef(W("Persisted"), tdPublic,
        mdTypeDefNil, nullptr, &second));
    EXPECT_EQ(TokenFromRid(3, mdtTypeDef), second);
}

#if defined(DNMD_ENABLE_INTERNAL_INTERFACES)
TEST(CheckDuplicates, LookupIndexObservesChangedNamesAndScopes)
{
    minipal::com_ptr<IMetaDataDispenserEx> dispenser;
    ASSERT_NO_FATAL_FAILURE(CreateDispenser(ReflectionEmitChecks, dispenser));
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(DefineScope(dispenser.p, emit));
    minipal::com_ptr<IMetaDataEmitHelper> helper;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataEmitHelper, (void**)&helper));

    mdModuleRef otherScope;
    ASSERT_EQ(S_OK, emit->DefineModuleRef(W("Other"), &otherScope));
    mdTypeRef oldRef, duplicate, newRef;
    mdToken module = TokenFromRid(1, mdtModule);
    ASSERT_EQ(S_OK, emit->DefineTypeRefByName(module, W("N.Referenced"), &oldRef));
    ASSERT_EQ(META_S_DUPLICATE, emit->DefineTypeRefByName(module, W("N.Referenced"), &duplicate));
    ASSERT_EQ(oldRef, duplicate);
    ASSERT_EQ(S_OK, helper->SetResolutionScopeHelper(oldRef, otherScope));
    ASSERT_EQ(S_OK, emit->DefineTypeRefByName(module, W("N.Referenced"), &newRef));
    EXPECT_NE(oldRef, newRef);
    ASSERT_EQ(META_S_DUPLICATE, emit->DefineTypeRefByName(otherScope, W("N.Referenced"), &duplicate));
    EXPECT_EQ(oldRef, duplicate);

    mdTypeDef oldType, typeDuplicate, newType;
    ASSERT_EQ(S_OK, emit->DefineTypeDef(W("N.Type"), tdPublic, mdTypeDefNil, nullptr, &oldType));
    ASSERT_EQ(META_S_DUPLICATE, emit->DefineTypeDef(W("N.Type"), tdPublic, mdTypeDefNil, nullptr, &typeDuplicate));
    ASSERT_EQ(oldType, typeDuplicate);
    ASSERT_EQ(S_OK, emit->DeleteToken(oldType));
    ASSERT_EQ(S_OK, emit->DefineTypeDef(W("N.Type"), tdPublic, mdTypeDefNil, nullptr, &newType));
    EXPECT_NE(oldType, newType);

    minipal::com_ptr<IMetaDataAssemblyEmit> assemblyEmit;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataAssemblyEmit, (void**)&assemblyEmit));
    ASSEMBLYMETADATA version = AssemblyVersion();
    mdAssemblyRef original, renamed, fresh;
    ASSERT_EQ(S_OK, assemblyEmit->DefineAssemblyRef(nullptr, 0, W("Original"),
        &version, nullptr, 0, 0, &original));
    ASSERT_EQ(META_S_DUPLICATE, assemblyEmit->DefineAssemblyRef(nullptr, 0, W("Original"),
        &version, nullptr, 0, 0, &renamed));
    ASSERT_EQ(original, renamed);
    ASSERT_EQ(S_OK, assemblyEmit->SetAssemblyRefProps(original, nullptr, 0,
        W("Renamed"), &version, nullptr, 0, 0));
    ASSERT_EQ(S_OK, assemblyEmit->DefineAssemblyRef(nullptr, 0, W("Original"),
        &version, nullptr, 0, 0, &fresh));
    EXPECT_NE(original, fresh);
    ASSERT_EQ(META_S_DUPLICATE, assemblyEmit->DefineAssemblyRef(nullptr, 0, W("Renamed"),
        &version, nullptr, 0, 0, &renamed));
    EXPECT_EQ(original, renamed);

    mdFile file;
    ASSERT_EQ(S_OK, assemblyEmit->DefineFile(W("File"), nullptr, 0, 0, &file));
    mdExportedType exported, repeated, recreated;
    ASSERT_EQ(S_OK, assemblyEmit->DefineExportedType(W("N.Export"), file,
        mdTypeDefNil, tdPublic, &exported));
    ASSERT_EQ(META_S_DUPLICATE, assemblyEmit->DefineExportedType(W("N.Export"), file,
        mdTypeDefNil, tdPublic, &repeated));
    EXPECT_EQ(exported, repeated);
    ASSERT_EQ(S_OK, emit->DeleteToken(exported));
    ASSERT_EQ(S_OK, assemblyEmit->DefineExportedType(W("N.Export"), file,
        mdTypeDefNil, tdPublic, &recreated));
    EXPECT_NE(exported, recreated);
}
#endif // DNMD_ENABLE_INTERNAL_INTERFACES

TEST(CheckDuplicates, LookupIndexLoadsPreexistingRowsInTokenOrder)
{
    minipal::com_ptr<IMetaDataDispenserEx> uncheckedDispenser;
    ASSERT_NO_FATAL_FAILURE(CreateDispenser(MDNoDupChecks, uncheckedDispenser));
    minipal::com_ptr<IMetaDataEmit> unchecked;
    ASSERT_NO_FATAL_FAILURE(DefineScope(uncheckedDispenser.p, unchecked));

    mdToken module = TokenFromRid(1, mdtModule);
    mdTypeRef earliest, second;
    ASSERT_EQ(S_OK, unchecked->DefineTypeRefByName(module, W("Repeated"), &earliest));
    ASSERT_EQ(S_OK, unchecked->DefineTypeRefByName(module, W("Repeated"), &second));
    EXPECT_NE(earliest, second);
    DWORD size;
    ASSERT_EQ(S_OK, unchecked->GetSaveSize(cssAccurate, &size));
    std::vector<uint8_t> image(size);
    ASSERT_EQ(S_OK, unchecked->SaveToMemory(image.data(), size));

    minipal::com_ptr<IMetaDataDispenserEx> dispenser;
    ASSERT_NO_FATAL_FAILURE(CreateDispenser(MDDupTypeRef, dispenser));
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_EQ(S_OK, dispenser->OpenScopeOnMemory(image.data(), size, ofCopyMemory,
        IID_IMetaDataEmit, (IUnknown**)&emit));

    mdTypeRef found, appended;
    ASSERT_EQ(META_S_DUPLICATE, emit->DefineTypeRefByName(module, W("Repeated"), &found));
    EXPECT_EQ(earliest, found);
    ASSERT_EQ(S_OK, emit->DefineTypeRefByName(module, W("Added"), &appended));
    ASSERT_EQ(META_S_DUPLICATE, emit->DefineTypeRefByName(module, W("Added"), &found));
    EXPECT_EQ(appended, found);
}
