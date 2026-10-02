// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "emit.hpp"
#include <corerror.h>
#include <metadata.h>
#include <atomic>
#include <future>
#include <thread>
#include <vector>

#if defined(DNMD_ENABLE_LOADED_MODULES_CACHE)
static void EnableThreadSafeScopes(IMetaDataDispenserEx* dispenser)
{
    VARIANT option{};
    V_VT(&option) = VT_UI4;
    V_UI4(&option) = MDThreadSafetyOn;
    ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataThreadSafetyOptions, &option));
}
#endif // DNMD_ENABLE_LOADED_MODULES_CACHE

TEST(TypeRef, ValidScopeAndDottedName)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdTypeRef typeRef;
    WSTR_string name = W("System.Object");
    ASSERT_EQ(S_OK, emit->DefineTypeRefByName(TokenFromRid(1, mdtModule), name.c_str(), &typeRef));
    ASSERT_EQ(1, RidFromToken(typeRef));
    ASSERT_EQ(mdtTypeRef, TypeFromToken(typeRef));

    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));
    mdToken resolutionScope;
    WSTR_string readName;
    readName.resize(name.capacity() + 1);
    ULONG readNameLength;
    ASSERT_EQ(S_OK, import->GetTypeRefProps(typeRef, &resolutionScope, &readName[0], (ULONG) readName.size(), &readNameLength));
    EXPECT_EQ(TokenFromRid(1, mdtModule), resolutionScope);
    EXPECT_EQ(readNameLength, name.size() + 1);
    EXPECT_EQ(name, readName.substr(0, readNameLength - 1));
}

TEST(TypeRef, InvalidScope)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdTypeRef typeRef;
    ASSERT_EQ(E_FAIL, emit->DefineTypeRefByName(TokenFromRid(1, mdtTypeDef), W("System.Object"), &typeRef));
}

TEST(TypeRef, ValidScopeAndNonDottedName)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdTypeRef typeRef;
    WSTR_string name = W("Bar");
    ASSERT_EQ(S_OK, emit->DefineTypeRefByName(TokenFromRid(1, mdtModule), name.c_str(), &typeRef));
    ASSERT_EQ(1, RidFromToken(typeRef));
    ASSERT_EQ(mdtTypeRef, TypeFromToken(typeRef));

    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));
    mdToken resolutionScope;
    WSTR_string readName;
    readName.resize(name.capacity() + 1);
    ULONG readNameLength;
    ASSERT_EQ(S_OK, import->GetTypeRefProps(typeRef, &resolutionScope, &readName[0], (ULONG) readName.size(), &readNameLength));
    EXPECT_EQ(TokenFromRid(1, mdtModule), resolutionScope);
    EXPECT_EQ(readNameLength, name.size() + 1);
    EXPECT_EQ(name, readName.substr(0, readNameLength - 1));
}

TEST(TypeRef, StringHeapGrowsToFourByteIndices)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));

    mdTypeRef first, large, last;
    mdToken scope = TokenFromRid(1, mdtModule);
    WSTR_string largeName(65500, static_cast<WCHAR>('x'));
    ASSERT_EQ(S_OK, emit->DefineTypeRefByName(scope, W("BeforeGrowth"), &first));
    ASSERT_EQ(S_OK, emit->DefineTypeRefByName(scope, largeName.c_str(), &large));
    ASSERT_EQ(S_OK, emit->DefineTypeRefByName(scope, W("AfterGrowth"), &last));

    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));
    WSTR_string name(largeName.size() + 1, static_cast<WCHAR>(0));
    ULONG nameLength;
    mdToken resolutionScope;
    ASSERT_EQ(S_OK, import->GetTypeRefProps(first, &resolutionScope, name.data(),
        (ULONG)name.size(), &nameLength));
    EXPECT_EQ(W("BeforeGrowth"), name.substr(0, nameLength - 1));
    ASSERT_EQ(S_OK, import->GetTypeRefProps(large, &resolutionScope, name.data(),
        (ULONG)name.size(), &nameLength));
    EXPECT_EQ(largeName, name.substr(0, nameLength - 1));
    ASSERT_EQ(S_OK, import->GetTypeRefProps(last, &resolutionScope, name.data(),
        (ULONG)name.size(), &nameLength));
    EXPECT_EQ(W("AfterGrowth"), name.substr(0, nameLength - 1));
    EXPECT_EQ(scope, resolutionScope);

    DWORD size;
    ASSERT_EQ(S_OK, emit->GetSaveSize(cssAccurate, &size));
    std::vector<uint8_t> serialized(size);
    ASSERT_EQ(S_OK, emit->SaveToMemory(serialized.data(), size));

    minipal::com_ptr<IMetaDataDispenser> dispenser;
    ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenser, (void**)&dispenser));
    minipal::com_ptr<IMetaDataImport> reopened;
    ASSERT_EQ(S_OK, dispenser->OpenScopeOnMemory(serialized.data(), size, ofReadOnly | ofCopyMemory,
        IID_IMetaDataImport, (IUnknown**)&reopened));
    ASSERT_EQ(S_OK, reopened->GetTypeRefProps(last, &resolutionScope, name.data(),
        (ULONG)name.size(), &nameLength));
    EXPECT_EQ(W("AfterGrowth"), name.substr(0, nameLength - 1));
}

TEST(TypeRef, ForwardResolutionScopeSurvivesAppend)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));

    mdTypeRef nested, enclosing;
    ASSERT_EQ(S_OK, emit->DefineTypeRefByName(TokenFromRid(2, mdtTypeRef), W("Nested"), &nested));
    ASSERT_EQ(S_OK, emit->DefineTypeRefByName(TokenFromRid(1, mdtModule), W("Enclosing"), &enclosing));

    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));
    mdToken resolutionScope;
    WCHAR name[16];
    ULONG nameLength;
    ASSERT_EQ(S_OK, import->GetTypeRefProps(nested, &resolutionScope, name, 16, &nameLength));
    EXPECT_EQ(enclosing, resolutionScope);
}

TEST(TypeRef, WidenReferencedCodedIndices)
{
    minipal::com_ptr<IMetaDataDispenserEx> dispenser;
    ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenserEx, (void**)&dispenser));
    VARIANT option{};
    V_VT(&option) = VT_UI4;
    V_UI4(&option) = MDNoDupChecks;
    ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataCheckDuplicatesFor, &option));

    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_EQ(S_OK, dispenser->DefineScope(CLSID_CorMetaDataRuntime, 0,
        IID_IMetaDataEmit, (IUnknown**)&emit));
    mdToken module = TokenFromRid(1, mdtModule);
    mdTypeRef original;
    ASSERT_EQ(S_OK, emit->DefineTypeRefByName(module, W("Original"), &original));
    mdTypeDef type;
    ASSERT_EQ(S_OK, emit->DefineTypeDef(W("Derived"), tdPublic, original, nullptr, &type));

    mdTypeRef last = original;
    for (int i = 1; i < 16'384; ++i)
        ASSERT_EQ(S_OK, emit->DefineTypeRefByName(module, W("Filler"), &last));
    ASSERT_EQ(16'384u, RidFromToken(last));

    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));
    WCHAR name[16];
    ULONG nameLength;
    DWORD flags;
    mdToken extends;
    ASSERT_EQ(S_OK, import->GetTypeDefProps(type, name, 16, &nameLength, &flags, &extends));
    EXPECT_EQ(original, extends);
    ASSERT_EQ(S_OK, emit->SetTypeDefProps(type, UINT32_MAX, last, nullptr));
    ASSERT_EQ(S_OK, import->GetTypeDefProps(type, name, 16, &nameLength, &flags, &extends));
    EXPECT_EQ(last, extends);

    DWORD size;
    ASSERT_EQ(S_OK, emit->GetSaveSize(cssAccurate, &size));
    std::vector<uint8_t> data(size);
    ASSERT_EQ(S_OK, emit->SaveToMemory(data.data(), size));
    minipal::com_ptr<IMetaDataImport> reopened;
    ASSERT_EQ(S_OK, dispenser->OpenScopeOnMemory(data.data(), size, ofReadOnly | ofCopyMemory,
        IID_IMetaDataImport, (IUnknown**)&reopened));
    ASSERT_EQ(S_OK, reopened->GetTypeDefProps(type, name, 16, &nameLength, &flags, &extends));
    EXPECT_EQ(last, extends);
}

TEST(TypeRef, ResolveTypeDefShortcutAndInvalidTokens)
{
    for (bool threadSafe : { false, true })
    {
        minipal::com_ptr<IMetaDataEmit> emit;
        if (threadSafe)
            ASSERT_NO_FATAL_FAILURE(CreateThreadSafeEmit(emit));
        else
            ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));

        mdTypeDef type;
        ASSERT_EQ(S_OK, emit->DefineTypeDef(W("Resolve.Shortcut"), tdPublic, mdTypeDefNil, nullptr, &type));

        minipal::com_ptr<IMetaDataImport> import;
        ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));
        minipal::com_ptr<IUnknown> identity;
        ASSERT_EQ(S_OK, import->QueryInterface(IID_IUnknown, (void**)&identity));

        mdTypeDef resolved = mdTypeDefNil;
        minipal::com_ptr<IUnknown> scope;
        ASSERT_EQ(S_OK, import->ResolveTypeRef(type, IID_IMetaDataImport2, &scope.p, &resolved));
        EXPECT_EQ(type, resolved);
        minipal::com_ptr<IUnknown> resolvedIdentity;
        ASSERT_EQ(S_OK, scope->QueryInterface(IID_IUnknown, (void**)&resolvedIdentity));
        EXPECT_EQ(identity.p, resolvedIdentity.p);
        resolvedIdentity.Release();
        scope.Release();

        IUnknown* output = identity.p;
        resolved = type;
        EXPECT_EQ(E_NOINTERFACE, import->ResolveTypeRef(type, IID_IMetaDataDispenser, &output, &resolved));
        EXPECT_EQ(nullptr, output);
        EXPECT_EQ(mdTypeDefNil, resolved);

        output = identity.p;
        resolved = type;
        EXPECT_EQ(E_INVALIDARG, import->ResolveTypeRef(mdTypeRefNil, IID_IUnknown, &output, &resolved));
        EXPECT_EQ(nullptr, output);
        EXPECT_EQ(mdTypeDefNil, resolved);

        output = identity.p;
        resolved = type;
        EXPECT_EQ(E_INVALIDARG, import->ResolveTypeRef(TokenFromRid(1, mdtMethodDef), IID_IUnknown, &output, &resolved));
        EXPECT_EQ(nullptr, output);
        EXPECT_EQ(mdTypeDefNil, resolved);

        output = identity.p;
        resolved = type;
#if defined(DNMD_ENABLE_LOADED_MODULES_CACHE)
        EXPECT_EQ(CLDB_E_RECORD_NOTFOUND, import->ResolveTypeRef(TokenFromRid(42, mdtTypeRef), IID_IUnknown, &output, &resolved));
#else // DNMD_ENABLE_LOADED_MODULES_CACHE
        EXPECT_EQ(E_NOTIMPL, import->ResolveTypeRef(TokenFromRid(42, mdtTypeRef), IID_IUnknown, &output, &resolved));
#endif // DNMD_ENABLE_LOADED_MODULES_CACHE
        EXPECT_EQ(nullptr, output);
        EXPECT_EQ(mdTypeDefNil, resolved);

#if !defined(DNMD_ENABLE_LOADED_MODULES_CACHE)
        mdTypeRef localReference;
        ASSERT_EQ(S_OK, emit->DefineTypeRefByName(TokenFromRid(1, mdtModule), W("Resolve.Shortcut"), &localReference));
        output = identity.p;
        resolved = type;
        EXPECT_EQ(E_NOTIMPL, import->ResolveTypeRef(localReference, IID_IUnknown, &output, &resolved));
        EXPECT_EQ(nullptr, output);
        EXPECT_EQ(mdTypeDefNil, resolved);
#endif // !DNMD_ENABLE_LOADED_MODULES_CACHE

        resolved = type;
        EXPECT_EQ(E_POINTER, import->ResolveTypeRef(type, IID_IUnknown, nullptr, &resolved));
        EXPECT_EQ(mdTypeDefNil, resolved);
        output = identity.p;
        EXPECT_EQ(E_POINTER, import->ResolveTypeRef(type, IID_IUnknown, &output, nullptr));
        EXPECT_EQ(nullptr, output);
    }
}

#if defined(DNMD_ENABLE_LOADED_MODULES_CACHE)
TEST(TypeRef, ResolveSameAndCrossScopeNestedTypes)
{
    minipal::com_ptr<IMetaDataDispenserEx> destinationDispenser, sourceDispenser;
    ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenserEx, (void**)&destinationDispenser));
    ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenserEx, (void**)&sourceDispenser));
    ASSERT_NO_FATAL_FAILURE(EnableThreadSafeScopes(destinationDispenser.p));
    ASSERT_NO_FATAL_FAILURE(EnableThreadSafeScopes(sourceDispenser.p));

    minipal::com_ptr<IMetaDataEmit> destination, source;
    ASSERT_EQ(S_OK, destinationDispenser->DefineScope(CLSID_CorMetaDataRuntime, 0,
        IID_IMetaDataEmit, (IUnknown**)&destination));
    ASSERT_EQ(S_OK, sourceDispenser->DefineScope(CLSID_CorMetaDataRuntime, 0,
        IID_IMetaDataEmit, (IUnknown**)&source));

    mdTypeDef wrongOuter, wrongMiddle, wrongLeaf, outer, middle, leaf;
    ASSERT_EQ(S_OK, destination->DefineTypeDef(W("Resolve.WrongOuter"), tdPublic, mdTypeDefNil, nullptr, &wrongOuter));
    ASSERT_EQ(S_OK, destination->DefineNestedType(W("Resolve.Middle"), tdNestedPublic, mdTypeDefNil, nullptr, wrongOuter, &wrongMiddle));
    ASSERT_EQ(S_OK, destination->DefineNestedType(W("Resolve.Leaf"), tdNestedPublic, mdTypeDefNil, nullptr, wrongMiddle, &wrongLeaf));
    ASSERT_EQ(S_OK, destination->DefineTypeDef(W("Resolve.Outer"), tdPublic, mdTypeDefNil, nullptr, &outer));
    ASSERT_EQ(S_OK, destination->DefineNestedType(W("Resolve.Middle"), tdNestedPublic, mdTypeDefNil, nullptr, outer, &middle));
    ASSERT_EQ(S_OK, destination->DefineNestedType(W("Resolve.Leaf"), tdNestedPublic, mdTypeDefNil, nullptr, middle, &leaf));
    EXPECT_NE(wrongLeaf, leaf);

    minipal::com_ptr<IMetaDataAssemblyEmit> assemblyEmit;
    ASSERT_EQ(S_OK, source->QueryInterface(IID_IMetaDataAssemblyEmit, (void**)&assemblyEmit));
    ASSEMBLYMETADATA assemblyMetadata{};
    mdAssemblyRef assemblyRef;
    ASSERT_EQ(S_OK, assemblyEmit->DefineAssemblyRef(nullptr, 0, W("Resolve.Assembly"),
        &assemblyMetadata, nullptr, 0, 0, &assemblyRef));

    mdTypeRef outerRef, middleRef, leafRef;
    ASSERT_EQ(S_OK, source->DefineTypeRefByName(assemblyRef, W("Resolve.Outer"), &outerRef));
    ASSERT_EQ(S_OK, source->DefineTypeRefByName(outerRef, W("Resolve.Middle"), &middleRef));
    ASSERT_EQ(S_OK, source->DefineTypeRefByName(middleRef, W("Resolve.Leaf"), &leafRef));

    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, source->QueryInterface(IID_IMetaDataImport, (void**)&import));
    minipal::com_ptr<IUnknown> destinationIdentity, sourceIdentity;
    ASSERT_EQ(S_OK, destination->QueryInterface(IID_IUnknown, (void**)&destinationIdentity));
    ASSERT_EQ(S_OK, source->QueryInterface(IID_IUnknown, (void**)&sourceIdentity));

    minipal::com_ptr<IMetaDataAssemblyImport> resolvedScope;
    mdTypeDef resolved = mdTypeDefNil;
    ASSERT_EQ(S_OK, import->ResolveTypeRef(leafRef, IID_IMetaDataAssemblyImport,
        (IUnknown**)&resolvedScope, &resolved));
    EXPECT_EQ(leaf, resolved);
    minipal::com_ptr<IUnknown> resolvedIdentity;
    ASSERT_EQ(S_OK, resolvedScope->QueryInterface(IID_IUnknown, (void**)&resolvedIdentity));
    EXPECT_EQ(destinationIdentity.p, resolvedIdentity.p);

    IUnknown* unsupportedScope = destinationIdentity.p;
    resolved = leaf;
    EXPECT_EQ(E_NOINTERFACE, import->ResolveTypeRef(leafRef, IID_IMetaDataDispenser, &unsupportedScope, &resolved));
    EXPECT_EQ(nullptr, unsupportedScope);
    EXPECT_EQ(mdTypeDefNil, resolved);

    mdTypeDef local;
    mdTypeRef localRef;
    ASSERT_EQ(S_OK, source->DefineTypeDef(W("Resolve.Local"), tdPublic, mdTypeDefNil, nullptr, &local));
    ASSERT_EQ(S_OK, source->DefineTypeRefByName(TokenFromRid(1, mdtModule), W("Resolve.Local"), &localRef));
    minipal::com_ptr<IUnknown> localScope;
    ASSERT_EQ(S_OK, import->ResolveTypeRef(localRef, IID_IUnknown, &localScope.p, &resolved));
    EXPECT_EQ(local, resolved);
    EXPECT_EQ(sourceIdentity.p, localScope.p);
}

TEST(TypeRef, ResolveReadOnlyScope)
{
    minipal::com_ptr<IMetaDataDispenser> dispenser;
    ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenser, (void**)&dispenser));
    minipal::com_ptr<IMetaDataEmit> destination;
    ASSERT_EQ(S_OK, dispenser->DefineScope(CLSID_CorMetaDataRuntime, 0,
        IID_IMetaDataEmit, (IUnknown**)&destination));
    mdTypeDef type;
    ASSERT_EQ(S_OK, destination->DefineTypeDef(W("ReadOnlyResolvable"), tdPublic, mdTypeDefNil, nullptr, &type));

    DWORD size;
    ASSERT_EQ(S_OK, destination->GetSaveSize(cssAccurate, &size));
    std::vector<uint8_t> serialized(size);
    ASSERT_EQ(S_OK, destination->SaveToMemory(serialized.data(), size));
    destination.Release();

    minipal::com_ptr<IMetaDataImport> reopened;
    ASSERT_EQ(S_OK, dispenser->OpenScopeOnMemory(serialized.data(), size, ofReadOnly | ofCopyMemory,
        IID_IMetaDataImport, (IUnknown**)&reopened));
    minipal::com_ptr<IMetaDataEmit> source;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(source));
    mdTypeRef reference;
    ASSERT_EQ(S_OK, source->DefineTypeRefByName(mdTokenNil, W("ReadOnlyResolvable"), &reference));

    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, source->QueryInterface(IID_IMetaDataImport, (void**)&import));
    minipal::com_ptr<IUnknown> resolvedScope, reopenedIdentity;
    ASSERT_EQ(S_OK, reopened->QueryInterface(IID_IUnknown, (void**)&reopenedIdentity));
    mdTypeDef resolved = mdTypeDefNil;
    ASSERT_EQ(S_OK, import->ResolveTypeRef(reference, IID_IUnknown, &resolvedScope.p, &resolved));
    EXPECT_EQ(type, resolved);
    EXPECT_EQ(reopenedIdentity.p, resolvedScope.p);
}

TEST(TypeRef, ResolveConvertedReadOnlyScope)
{
    minipal::com_ptr<IMetaDataDispenser> dispenser;
    ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenser, (void**)&dispenser));
    minipal::com_ptr<IMetaDataEmit> original;
    ASSERT_EQ(S_OK, dispenser->DefineScope(CLSID_CorMetaDataRuntime, 0,
        IID_IMetaDataEmit, (IUnknown**)&original));
    DWORD size;
    ASSERT_EQ(S_OK, original->GetSaveSize(cssAccurate, &size));
    std::vector<uint8_t> image(size);
    ASSERT_EQ(S_OK, original->SaveToMemory(image.data(), size));
    IMetaDataEmit* originalReference = original.Detach();
    EXPECT_EQ(0u, originalReference->Release());

    minipal::com_ptr<IMDInternalImport> readOnly;
    ASSERT_EQ(S_OK, dispenser->OpenScopeOnMemory(image.data(), size, ofReadOnly | ofCopyMemory,
        IID_IMDInternalImport, (IUnknown**)&readOnly));
    minipal::com_ptr<IMetaDataEmit> readOnlyEmit;
    EXPECT_EQ(E_NOINTERFACE, readOnly->QueryInterface(IID_IMetaDataEmit, (void**)&readOnlyEmit));
    IMDInternalImport* converted = nullptr;
    ASSERT_EQ(S_OK, ConvertDNMDInternalImport(readOnly.p, &converted));
    minipal::com_ptr<IMDInternalImport> writable;
    writable.Attach(converted);

    minipal::com_ptr<IUnknown> readOnlyIdentity, writableIdentity;
    ASSERT_EQ(S_OK, readOnly->QueryInterface(IID_IUnknown, (void**)&readOnlyIdentity));
    ASSERT_EQ(S_OK, writable->QueryInterface(IID_IUnknown, (void**)&writableIdentity));
    EXPECT_NE(readOnlyIdentity.p, writableIdentity.p);

    minipal::com_ptr<IMetaDataEmit> writableEmit;
    ASSERT_EQ(S_OK, writable->QueryInterface(IID_IMetaDataEmit, (void**)&writableEmit));
    mdTypeDef added;
    ASSERT_EQ(S_OK, writableEmit->DefineTypeDef(W("Resolve.ConvertedOnly"), tdPublic,
        mdTypeDefNil, nullptr, &added));
    mdTypeDef missing = mdTypeDefNil;
    EXPECT_EQ(CLDB_E_RECORD_NOTFOUND, readOnly->FindTypeDef("Resolve", "ConvertedOnly", mdTokenNil, &missing));

    minipal::com_ptr<IMetaDataEmit> source;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(source));
    mdTypeRef reference;
    ASSERT_EQ(S_OK, source->DefineTypeRefByName(mdTokenNil, W("Resolve.ConvertedOnly"), &reference));
    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, source->QueryInterface(IID_IMetaDataImport, (void**)&import));
    minipal::com_ptr<IUnknown> resolvedScope;
    mdTypeDef resolved = mdTypeDefNil;
    ASSERT_EQ(S_OK, import->ResolveTypeRef(reference, IID_IUnknown, &resolvedScope.p, &resolved));
    EXPECT_EQ(added, resolved);
    EXPECT_EQ(writableIdentity.p, resolvedScope.p);

    resolvedScope.Release();
    writableEmit.Release();
    writableIdentity.Release();
    IMDInternalImport* writableReference = writable.Detach();
    EXPECT_EQ(0u, writableReference->Release());

    IUnknown* output = source.p;
    resolved = added;
    EXPECT_EQ(META_E_CANNOTRESOLVETYPEREF, import->ResolveTypeRef(reference, IID_IUnknown, &output, &resolved));
    EXPECT_EQ(nullptr, output);
    EXPECT_EQ(mdTypeDefNil, resolved);

    readOnlyIdentity.Release();
    IMDInternalImport* readOnlyReference = readOnly.Detach();
    EXPECT_EQ(0u, readOnlyReference->Release());
    import.Release();
    IMetaDataEmit* sourceReference = source.Detach();
    EXPECT_EQ(0u, sourceReference->Release());
    IMetaDataDispenser* dispenserReference = dispenser.Detach();
    EXPECT_EQ(0u, dispenserReference->Release());
}

TEST(TypeRef, ResolveMissingTypeAndReleaseScope)
{
    for (bool threadSafe : { false, true })
    {
        minipal::com_ptr<IMetaDataEmit> source, destination;
        ASSERT_NO_FATAL_FAILURE(CreateEmit(source));
        if (threadSafe)
            ASSERT_NO_FATAL_FAILURE(CreateThreadSafeEmit(destination));
        else
            ASSERT_NO_FATAL_FAILURE(CreateEmit(destination));

        mdTypeDef type;
        ASSERT_EQ(S_OK, destination->DefineTypeDef(W("Resolve.Released"), tdPublic, mdTypeDefNil, nullptr, &type));
        mdTypeRef foundRef, missingRef;
        ASSERT_EQ(S_OK, source->DefineTypeRefByName(mdTokenNil, W("Resolve.Released"), &foundRef));
        ASSERT_EQ(S_OK, source->DefineTypeRefByName(mdTokenNil, W("Resolve.Missing"), &missingRef));
        minipal::com_ptr<IMetaDataImport> import;
        ASSERT_EQ(S_OK, source->QueryInterface(IID_IMetaDataImport, (void**)&import));

        mdTypeDef resolved = type;
        IUnknown* output = source.p;
        EXPECT_EQ(META_E_CANNOTRESOLVETYPEREF, import->ResolveTypeRef(missingRef, IID_IUnknown, &output, &resolved));
        EXPECT_EQ(nullptr, output);
        EXPECT_EQ(mdTypeDefNil, resolved);

        minipal::com_ptr<IUnknown> resolvedScope;
        ASSERT_EQ(S_OK, import->ResolveTypeRef(foundRef, IID_IUnknown, &resolvedScope.p, &resolved));
        EXPECT_EQ(type, resolved);
        resolvedScope.Release();

        IMetaDataEmit* lastReference = destination.Detach();
        EXPECT_EQ(0u, lastReference->Release());

        resolved = type;
        output = source.p;
        EXPECT_EQ(META_E_CANNOTRESOLVETYPEREF, import->ResolveTypeRef(foundRef, IID_IUnknown, &output, &resolved));
        EXPECT_EQ(nullptr, output);
        EXPECT_EQ(mdTypeDefNil, resolved);
    }
}

TEST(TypeRef, ResolveCyclicHierarchy)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdTypeRef first, second;
    ASSERT_EQ(S_OK, emit->DefineTypeRefByName(TokenFromRid(2, mdtTypeRef), W("Resolve.First"), &first));
    ASSERT_EQ(S_OK, emit->DefineTypeRefByName(first, W("Resolve.Second"), &second));
    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));

    mdTypeDef resolved = TokenFromRid(42, mdtTypeDef);
    IUnknown* output = emit.p;
    EXPECT_EQ(CLDB_E_FILE_CORRUPT, import->ResolveTypeRef(second, IID_IUnknown, &output, &resolved));
    EXPECT_EQ(nullptr, output);
    EXPECT_EQ(mdTypeDefNil, resolved);
}

TEST(TypeRef, ResolveWhileDestinationIsReleased)
{
    minipal::com_ptr<IMetaDataDispenserEx> dispenser;
    ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenserEx, (void**)&dispenser));
    ASSERT_NO_FATAL_FAILURE(EnableThreadSafeScopes(dispenser.p));

    minipal::com_ptr<IMetaDataEmit> source, destination;
    ASSERT_EQ(S_OK, dispenser->DefineScope(CLSID_CorMetaDataRuntime, 0, IID_IMetaDataEmit, (IUnknown**)&source));
    ASSERT_EQ(S_OK, dispenser->DefineScope(CLSID_CorMetaDataRuntime, 0, IID_IMetaDataEmit, (IUnknown**)&destination));
    mdTypeDef type;
    mdTypeRef reference;
    ASSERT_EQ(S_OK, destination->DefineTypeDef(W("Resolve.Concurrent"), tdPublic, mdTypeDefNil, nullptr, &type));
    ASSERT_EQ(S_OK, source->DefineTypeRefByName(mdTokenNil, W("Resolve.Concurrent"), &reference));

    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, source->QueryInterface(IID_IMetaDataImport, (void**)&import));
    std::promise<void> firstAttempt;
    std::future<void> firstAttemptDone = firstAttempt.get_future();
    std::atomic<bool> releaseCompleted{ false };
    std::atomic<unsigned> successes{ 0 };
    std::atomic<HRESULT> unexpected{ S_OK };
    std::thread resolver([&]
    {
        for (unsigned i = 0; i < 1000 || !releaseCompleted.load(); ++i)
        {
            minipal::com_ptr<IUnknown> scope;
            mdTypeDef resolved = mdTypeDefNil;
            HRESULT hr = import->ResolveTypeRef(reference, IID_IUnknown, &scope.p, &resolved);
            if (hr == S_OK)
            {
                if (scope.p == nullptr || resolved != type)
                    unexpected.store(E_FAIL);
                ++successes;
            }
            else if (hr != META_E_CANNOTRESOLVETYPEREF || scope.p != nullptr || resolved != mdTypeDefNil)
            {
                unexpected.store(hr == S_OK ? E_FAIL : hr);
            }
            if (i == 0)
                firstAttempt.set_value();
        }
    });

    firstAttemptDone.wait();
    destination.Release();
    releaseCompleted.store(true);
    resolver.join();

    EXPECT_EQ(S_OK, unexpected.load());
    EXPECT_GT(successes.load(), 0u);
    mdTypeDef resolved = type;
    IUnknown* output = source.p;
    EXPECT_EQ(META_E_CANNOTRESOLVETYPEREF, import->ResolveTypeRef(reference, IID_IUnknown, &output, &resolved));
    EXPECT_EQ(nullptr, output);
    EXPECT_EQ(mdTypeDefNil, resolved);
}

TEST(TypeRef, ResolveWhileDestinationIsUpdated)
{
    minipal::com_ptr<IMetaDataDispenserEx> dispenser;
    ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenserEx, (void**)&dispenser));
    ASSERT_NO_FATAL_FAILURE(EnableThreadSafeScopes(dispenser.p));

    minipal::com_ptr<IMetaDataEmit> source, destination;
    ASSERT_EQ(S_OK, dispenser->DefineScope(CLSID_CorMetaDataRuntime, 0, IID_IMetaDataEmit, (IUnknown**)&source));
    ASSERT_EQ(S_OK, dispenser->DefineScope(CLSID_CorMetaDataRuntime, 0, IID_IMetaDataEmit, (IUnknown**)&destination));
    mdTypeDef type;
    mdTypeRef reference;
    ASSERT_EQ(S_OK, destination->DefineTypeDef(W("Resolve.Updated"), tdPublic, mdTypeDefNil, nullptr, &type));
    ASSERT_EQ(S_OK, source->DefineTypeRefByName(mdTokenNil, W("Resolve.Updated"), &reference));

    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, source->QueryInterface(IID_IMetaDataImport, (void**)&import));
    std::atomic<HRESULT> failure{ S_OK };
    std::thread writer([&]
    {
        for (unsigned i = 0; i < 200; ++i)
        {
            WSTR_string name = W("Resolve.Filler");
            name.push_back(static_cast<WCHAR>('A' + i / 26));
            name.push_back(static_cast<WCHAR>('A' + i % 26));
            mdTypeDef added;
            HRESULT hr = destination->DefineTypeDef(name.c_str(), tdPublic, mdTypeDefNil, nullptr, &added);
            if (hr != S_OK)
                failure.store(hr);
        }
    });

    for (unsigned i = 0; i < 500; ++i)
    {
        minipal::com_ptr<IUnknown> resolvedScope;
        mdTypeDef resolved = mdTypeDefNil;
        HRESULT hr = import->ResolveTypeRef(reference, IID_IUnknown, &resolvedScope.p, &resolved);
        if (hr != S_OK || resolvedScope.p == nullptr || resolved != type)
            failure.store(hr == S_OK ? E_FAIL : hr);
    }
    writer.join();
    EXPECT_EQ(S_OK, failure.load());
}
#endif // DNMD_ENABLE_LOADED_MODULES_CACHE
