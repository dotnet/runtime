// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "emit.hpp"
#include <dnmd.hpp>
#include <metadata.h>
#include <metadataemithelper.h>
#include <mdinternalemit.h>
#include <minipal/rwlock.h>
#include <chrono>
#include <cstring>
#include <future>
#include <utility>
#include <vector>

namespace
{
    using LogEntry = std::pair<mdToken, uint32_t>;

    mdToken RecordToken(mdtable_id_t table, uint32_t rid)
    {
        return 0x80000000u | (static_cast<uint32_t>(table) << 24) | rid;
    }

    void ReadENCLog(IMetaDataEmit2* emit, std::vector<LogEntry>& entries, DWORD* savedSize = nullptr)
    {
        DWORD size = 0;
        ASSERT_EQ(S_OK, emit->GetSaveSize(cssAccurate, &size));
        std::vector<uint8_t> image(size);
        ASSERT_EQ(S_OK, emit->SaveToMemory(image.data(), size));
        if (savedSize != nullptr)
            *savedSize = size;

        mdhandle_t rawHandle = nullptr;
        ASSERT_TRUE(md_create_handle(image.data(), image.size(), &rawHandle));
        mdhandle_ptr handle{ rawHandle };
        mdcursor_t row;
        uint32_t count;
        if (!md_create_cursor(handle.get(), mdtid_ENCLog, &row, &count))
            return;

        for (uint32_t i = 0; i < count; ++i)
        {
            mdToken token;
            uint32_t operation;
            ASSERT_TRUE(md_get_column_value_as_constant(row, mdtENCLog_Token, &token));
            ASSERT_TRUE(md_get_column_value_as_constant(row, mdtENCLog_Op, &operation));
            entries.emplace_back(token, operation);
            if (i + 1 < count)
                ASSERT_TRUE(md_cursor_next(&row));
        }
    }

    void SaveImage(IMetaDataEmit2* emit, std::vector<uint8_t>& image)
    {
        DWORD size = 0;
        ASSERT_EQ(S_OK, emit->GetSaveSize(cssAccurate, &size));
        image.resize(size);
        ASSERT_EQ(S_OK, emit->SaveToMemory(image.data(), size));
    }

    void AppendUInt16(std::vector<uint8_t>& image, uint16_t value)
    {
        image.push_back(static_cast<uint8_t>(value));
        image.push_back(static_cast<uint8_t>(value >> 8));
    }

    void AppendUInt32(std::vector<uint8_t>& image, uint32_t value)
    {
        for (unsigned i = 0; i < 4; ++i)
            image.push_back(static_cast<uint8_t>(value >> (i * 8)));
    }

    void WriteUInt32(std::vector<uint8_t>& image, size_t offset, uint32_t value)
    {
        for (unsigned i = 0; i < 4; ++i)
            image[offset + i] = static_cast<uint8_t>(value >> (i * 8));
    }

    bool CreateMinimalTypeRefDelta(GUID const& baseMvid, std::vector<uint8_t>& delta,
                                   bool invalidSecondLog = false, bool withENCMap = false,
                                   GUID const* previousEncId = nullptr, GUID const* nextEncId = nullptr,
                                   char const* typeName = "Added")
    {
        // A minimal EnC delta has an uncompressed tables stream and a #JTD marker.
        std::vector<uint8_t> seed;
        AppendUInt32(seed, 0x424a5342);
        AppendUInt16(seed, 1);
        AppendUInt16(seed, 1);
        AppendUInt32(seed, 0);
        AppendUInt32(seed, 12);
        constexpr char version[] = "v4.0.30319";
        seed.insert(seed.end(), version, version + sizeof(version));
        while (seed.size() % 4 != 0)
            seed.push_back(0);
        AppendUInt16(seed, 0);
        AppendUInt16(seed, 2);

        size_t jtdOffset = seed.size();
        AppendUInt32(seed, 0);
        AppendUInt32(seed, 0);
        seed.insert(seed.end(), { '#', 'J', 'T', 'D', 0, 0, 0, 0 });
        size_t tablesOffset = seed.size();
        AppendUInt32(seed, 0);
        AppendUInt32(seed, 38);
        seed.insert(seed.end(), { '#', '-', 0, 0 });
        WriteUInt32(seed, jtdOffset, static_cast<uint32_t>(seed.size()));
        WriteUInt32(seed, tablesOffset, static_cast<uint32_t>(seed.size()));

        AppendUInt32(seed, 0);
        seed.insert(seed.end(), { 2, 0, 0, 1 });
        AppendUInt32(seed, 1);
        AppendUInt32(seed, 0);
        AppendUInt32(seed, 0);
        AppendUInt32(seed, 0);
        AppendUInt32(seed, 1);
        for (unsigned i = 0; i < 5; ++i)
            AppendUInt16(seed, 0);

        mdhandle_t raw = nullptr;
        if (!md_create_handle(seed.data(), seed.size(), &raw))
            return false;
        mdhandle_ptr handle{ raw };
        mdcursor_t module;
        if (!md_token_to_cursor(handle.get(), TokenFromRid(1, mdtModule), &module))
            return false;

        mdguid_t mvid;
        static_assert(sizeof(mvid) == sizeof(baseMvid));
        std::memcpy(&mvid, &baseMvid, sizeof(mvid));
        mdguid_t newEncId{ 0xadc561fe, 0x9740, 0x4578, { 0x8c, 0xc2, 0xd1, 0x93, 0x56, 0xde, 0x33, 0xaf } };
        if (nextEncId != nullptr)
            std::memcpy(&newEncId, nextEncId, sizeof(newEncId));
        if (!md_set_column_value_as_guid(module, mdtModule_Mvid, mvid))
            return false;
        if (previousEncId != nullptr)
        {
            mdguid_t baseEncId;
            std::memcpy(&baseEncId, previousEncId, sizeof(baseEncId));
            if (!md_set_column_value_as_guid(module, mdtModule_EncBaseId, baseEncId))
                return false;
        }
        if (!md_set_column_value_as_guid(module, mdtModule_EncId, newEncId))
            return false;

        {
            md_added_row_t typeRef{ mdcursor_t{} };
            if (!md_append_row(handle.get(), mdtid_TypeRef, &typeRef)
                || !md_set_column_value_as_token(typeRef, mdtTypeRef_ResolutionScope, TokenFromRid(1, mdtModule))
                || !md_set_column_value_as_utf8(typeRef, mdtTypeRef_TypeNamespace, "Example")
                || !md_set_column_value_as_utf8(typeRef, mdtTypeRef_TypeName, typeName))
                return false;
        }
        {
            md_added_row_t log{ mdcursor_t{} };
            if (!md_append_row(handle.get(), mdtid_ENCLog, &log)
                || !md_set_column_value_as_constant(log, mdtENCLog_Token, TokenFromRid(1, mdtTypeRef))
                || !md_set_column_value_as_constant(log, mdtENCLog_Op, 0))
                return false;
        }
        if (invalidSecondLog)
        {
            for (char const* name : { "Unlogged", "Skipped" })
            {
                md_added_row_t typeRef{ mdcursor_t{} };
                if (!md_append_row(handle.get(), mdtid_TypeRef, &typeRef)
                    || !md_set_column_value_as_token(typeRef, mdtTypeRef_ResolutionScope, TokenFromRid(1, mdtModule))
                    || !md_set_column_value_as_utf8(typeRef, mdtTypeRef_TypeNamespace, "Example")
                    || !md_set_column_value_as_utf8(typeRef, mdtTypeRef_TypeName, name))
                    return false;
            }
            md_added_row_t log{ mdcursor_t{} };
            if (!md_append_row(handle.get(), mdtid_ENCLog, &log)
                || !md_set_column_value_as_constant(log, mdtENCLog_Token, TokenFromRid(3, mdtTypeRef))
                || !md_set_column_value_as_constant(log, mdtENCLog_Op, 0))
                return false;
        }
        if (withENCMap)
        {
            md_added_row_t map{ mdcursor_t{} };
            if (!md_append_row(handle.get(), mdtid_ENCMap, &map)
                || !md_set_column_value_as_constant(map, mdtENCMap_Token, TokenFromRid(1, mdtTypeRef)))
                return false;
        }

        size_t size = 0;
        (void)md_write_to_buffer(handle.get(), nullptr, &size);
        if (size == 0)
            return false;
        delta.resize(size);
        return md_write_to_buffer(handle.get(), delta.data(), &size);
    }

    bool AddMemberChangesToDelta(std::vector<uint8_t>& delta, bool create, bool includeExtras = false)
    {
        mdhandle_t raw = nullptr;
        if (!md_create_handle(delta.data(), delta.size(), &raw))
            return false;
        mdhandle_ptr handle{ raw };

        mdcursor_t parent{};
        for (unsigned i = 0; i < 2; ++i)
        {
            md_added_row_t type{ mdcursor_t{} };
            if (!md_append_row(handle.get(), mdtid_TypeDef, &type)
                || !md_set_column_value_as_utf8(type, mdtTypeDef_TypeName, i == 0 ? "<Module>" : "EditedType")
                || !md_set_column_value_as_utf8(type, mdtTypeDef_TypeNamespace, "")
                || !md_set_column_value_as_constant(type, mdtTypeDef_Flags, i == 0 ? 0 : tdPublic | tdAbstract)
                || !md_set_column_value_as_token(type, mdtTypeDef_Extends, mdTypeDefNil))
                return false;
            if (i == 1)
                parent = type;
        }

        BYTE methodSig[] = { IMAGE_CEE_CS_CALLCONV_DEFAULT, 0, ELEMENT_TYPE_VOID };
        BYTE fieldSig[] = { IMAGE_CEE_CS_CALLCONV_FIELD, ELEMENT_TYPE_I4 };
        mdcursor_t methodCursor{};
        {
            md_added_row_t method{ mdcursor_t{} };
            if (!md_add_new_row_to_list(parent, mdtTypeDef_MethodList, &method)
                || !md_set_column_value_as_utf8(method, mdtMethodDef_Name, "AddedMethod")
                || !md_set_column_value_as_blob(method, mdtMethodDef_Signature, methodSig, sizeof(methodSig))
                || !md_set_column_value_as_constant(method, mdtMethodDef_Flags, mdPublic | mdStatic)
                || !md_set_column_value_as_constant(method, mdtMethodDef_Rva, 77))
                return false;
            methodCursor = method;
        }
        {
            md_added_row_t field{ mdcursor_t{} };
            if (!md_add_new_row_to_list(parent, mdtTypeDef_FieldList, &field)
                || !md_set_column_value_as_utf8(field, mdtField_Name, "AddedField")
                || !md_set_column_value_as_blob(field, mdtField_Signature, fieldSig, sizeof(fieldSig))
                || !md_set_column_value_as_constant(field, mdtField_Flags, fdPublic | fdStatic))
                return false;
        }

        auto addLog = [&](mdToken token, uint32_t operation)
        {
            md_added_row_t row{ mdcursor_t{} };
            return md_append_row(handle.get(), mdtid_ENCLog, &row)
                && md_set_column_value_as_constant(row, mdtENCLog_Token, token)
                && md_set_column_value_as_constant(row, mdtENCLog_Op, operation);
        };
        mdToken type = TokenFromRid(2, mdtTypeDef);
        mdToken method = TokenFromRid(1, mdtMethodDef);
        mdToken field = TokenFromRid(1, mdtFieldDef);
        if (!addLog(type, 0)
            || (create && !addLog(type, 1))
            || !addLog(method, 0)
            || (create && !addLog(type, 2))
            || !addLog(field, 0))
            return false;

        if (includeExtras)
        {
            {
                md_added_row_t param{ mdcursor_t{} };
                if (!md_add_new_row_to_sorted_list(methodCursor, mdtMethodDef_ParamList,
                        mdtParam_Sequence, 1, &param)
                    || !md_set_column_value_as_utf8(param, mdtParam_Name, "Argument")
                    || !md_set_column_value_as_constant(param, mdtParam_Flags, pdIn))
                    return false;
            }
            if (!addLog(method, 3) || !addLog(TokenFromRid(1, mdtParamDef), 0))
                return false;

            mdcursor_t eventMap;
            {
                md_added_row_t map{ mdcursor_t{} };
                if (!md_append_row(handle.get(), mdtid_EventMap, &map)
                    || !md_set_column_value_as_token(map, mdtEventMap_Parent, type))
                    return false;
                eventMap = map;
            }
            {
                md_added_row_t eventRow{ mdcursor_t{} };
                if (!md_add_new_row_to_list(eventMap, mdtEventMap_EventList, &eventRow)
                    || !md_set_column_value_as_utf8(eventRow, mdtEvent_Name, "Changed")
                    || !md_set_column_value_as_token(eventRow, mdtEvent_EventType, TokenFromRid(1, mdtTypeRef)))
                    return false;
            }
            mdToken event = TokenFromRid(1, mdtEvent);
            if (!addLog(RecordToken(mdtid_EventMap, 1), 0)
                || !addLog(RecordToken(mdtid_EventMap, 1), 5)
                || !addLog(event, 0))
                return false;

            {
                md_added_row_t semantics{ mdcursor_t{} };
                if (!md_append_row(handle.get(), mdtid_MethodSemantics, &semantics)
                    || !md_set_column_value_as_token(semantics, mdtMethodSemantics_Association, event)
                    || !md_set_column_value_as_constant(semantics, mdtMethodSemantics_Semantics, msAddOn)
                    || !md_set_column_value_as_token(semantics, mdtMethodSemantics_Method, method))
                    return false;
            }
            if (!addLog(RecordToken(mdtid_MethodSemantics, 1), 0))
                return false;

            mdcursor_t propertyMap;
            {
                md_added_row_t map{ mdcursor_t{} };
                if (!md_append_row(handle.get(), mdtid_PropertyMap, &map)
                    || !md_set_column_value_as_token(map, mdtPropertyMap_Parent, type))
                    return false;
                propertyMap = map;
            }
            {
                md_added_row_t property{ mdcursor_t{} };
                BYTE signature[] = { IMAGE_CEE_CS_CALLCONV_PROPERTY, 0, ELEMENT_TYPE_I4 };
                if (!md_add_new_row_to_list(propertyMap, mdtPropertyMap_PropertyList, &property)
                    || !md_set_column_value_as_utf8(property, mdtProperty_Name, "Count")
                    || !md_set_column_value_as_blob(property, mdtProperty_Type, signature, sizeof(signature)))
                    return false;
            }
            if (!addLog(RecordToken(mdtid_PropertyMap, 1), 0)
                || !addLog(RecordToken(mdtid_PropertyMap, 1), 4)
                || !addLog(TokenFromRid(1, mdtProperty), 0))
                return false;
        }

        size_t size = 0;
        (void)md_write_to_buffer(handle.get(), nullptr, &size);
        if (size == 0)
            return false;
        std::vector<uint8_t> newDelta(size);
        if (!md_write_to_buffer(handle.get(), newDelta.data(), &size))
            return false;
        handle.reset();
        delta.swap(newDelta);
        return true;
    }

    bool AppendTypeRefsToDelta(std::vector<uint8_t>& delta, uint32_t lastRid, bool duplicateLast = false)
    {
        mdhandle_t raw = nullptr;
        if (!md_create_handle(delta.data(), delta.size(), &raw))
            return false;
        mdhandle_ptr handle{ raw };

        for (uint32_t rid = 2; rid <= lastRid; ++rid)
        {
            {
                md_added_row_t typeRef{ mdcursor_t{} };
                if (!md_append_row(handle.get(), mdtid_TypeRef, &typeRef)
                    || !md_set_column_value_as_token(typeRef, mdtTypeRef_ResolutionScope, TokenFromRid(1, mdtModule))
                    || !md_set_column_value_as_utf8(typeRef, mdtTypeRef_TypeName, "More")
                    || !md_set_column_value_as_utf8(typeRef, mdtTypeRef_TypeNamespace, "Example"))
                    return false;
            }
            md_added_row_t log{ mdcursor_t{} };
            if (!md_append_row(handle.get(), mdtid_ENCLog, &log)
                || !md_set_column_value_as_constant(log, mdtENCLog_Token, TokenFromRid(rid, mdtTypeRef))
                || !md_set_column_value_as_constant(log, mdtENCLog_Op, 0))
                return false;
        }
        if (duplicateLast)
        {
            md_added_row_t log{ mdcursor_t{} };
            if (!md_append_row(handle.get(), mdtid_ENCLog, &log)
                || !md_set_column_value_as_constant(log, mdtENCLog_Token, TokenFromRid(lastRid, mdtTypeRef))
                || !md_set_column_value_as_constant(log, mdtENCLog_Op, 0))
                return false;
        }

        size_t size = 0;
        (void)md_write_to_buffer(handle.get(), nullptr, &size);
        if (size == 0)
            return false;
        std::vector<uint8_t> updated(size);
        if (!md_write_to_buffer(handle.get(), updated.data(), &size))
            return false;
        handle.reset();
        delta.swap(updated);
        return true;
    }
}

TEST(UpdateMode, DispenserValidatesAndReportsMode)
{
    minipal::com_ptr<IMetaDataDispenserEx> dispenser;
    ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenserEx, (void**)&dispenser));

    VARIANT option{};
    ASSERT_EQ(S_OK, dispenser->GetOption(MetaDataSetUpdate, &option));
    EXPECT_EQ(VT_UI4, V_VT(&option));
    EXPECT_EQ(MDUpdateFull, V_UI4(&option));

    V_UI4(&option) = MDUpdateExtension;
    ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataSetUpdate, &option));
    V_UI4(&option) = MDUpdateFull;
    ASSERT_EQ(S_OK, dispenser->GetOption(MetaDataSetUpdate, &option));
    EXPECT_EQ(MDUpdateExtension, V_UI4(&option));

    V_VT(&option) = VT_I4;
    EXPECT_EQ(E_INVALIDARG, dispenser->SetOption(MetaDataSetUpdate, &option));
    V_VT(&option) = VT_UI4;
    V_UI4(&option) = MDUpdateENC;
    ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataSetUpdate, &option));
    ASSERT_EQ(S_OK, dispenser->GetOption(MetaDataSetUpdate, &option));
    EXPECT_EQ(MDUpdateENC, V_UI4(&option));
    V_UI4(&option) = MDUpdateExtension;
    ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataSetUpdate, &option));
    V_UI4(&option) = MDUpdateDelta;
    EXPECT_EQ(E_INVALIDARG, dispenser->SetOption(MetaDataSetUpdate, &option));
    V_UI4(&option) = MDUpdateIncremental;
    EXPECT_EQ(E_INVALIDARG, dispenser->SetOption(MetaDataSetUpdate, &option));
    EXPECT_EQ(E_INVALIDARG, dispenser->SetOption(MetaDataSetUpdate, nullptr));
    EXPECT_EQ(E_INVALIDARG, dispenser->GetOption(MetaDataSetUpdate, nullptr));
    ASSERT_EQ(S_OK, dispenser->GetOption(MetaDataSetUpdate, &option));
    EXPECT_EQ(MDUpdateExtension, V_UI4(&option));
}

TEST(UpdateMode, ScopesCaptureModeAndExposeInternalEmitter)
{
    minipal::com_ptr<IMetaDataDispenserEx> dispenser;
    ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenserEx, (void**)&dispenser));
    VARIANT option{};
    V_VT(&option) = VT_UI4;
    V_UI4(&option) = MDUpdateExtension;
    ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataSetUpdate, &option));
    V_UI4(&option) = MDThreadSafetyOn;
    ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataThreadSafetyOptions, &option));

    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_EQ(S_OK, dispenser->DefineScope(CLSID_CorMetaDataRuntime, 0,
        IID_IMetaDataEmit, (IUnknown**)&emit));
    minipal::com_ptr<IMDInternalEmit> internal;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMDInternalEmit, (void**)&internal));

    ULONG previous = UINT32_MAX;
    ASSERT_EQ(S_OK, internal->SetMDUpdateMode(MDUpdateFull, &previous));
    EXPECT_EQ(MDUpdateExtension, previous);
    ASSERT_EQ(S_OK, internal->SetMDUpdateMode(MDUpdateExtension, &previous));
    EXPECT_EQ(MDUpdateFull, previous);
    EXPECT_EQ(S_OK, internal->SetMDUpdateMode(MDUpdateENC, &previous));
    EXPECT_EQ(MDUpdateExtension, previous);
    EXPECT_EQ(S_OK, internal->SetMDUpdateMode(MDUpdateFull, &previous));
    EXPECT_EQ(MDUpdateENC, previous);

    GUID expected = { 0x9be71e5c, 0xae85, 0x4ebd, { 0x81, 0x02, 0x11, 0xe7, 0x33, 0xa2, 0x15, 0x6c } };
    ASSERT_EQ(S_OK, internal->ChangeMvid(expected));
    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));
    GUID actual{};
    ASSERT_EQ(S_OK, import->GetScopeProps(nullptr, 0, nullptr, &actual));
    EXPECT_EQ(0, std::memcmp(&expected, &actual, sizeof(expected)));

    V_UI4(&option) = MDUpdateFull;
    ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataSetUpdate, &option));
    minipal::com_ptr<IMetaDataEmit> other;
    ASSERT_EQ(S_OK, dispenser->DefineScope(CLSID_CorMetaDataRuntime, 0,
        IID_IMetaDataEmit, (IUnknown**)&other));
    minipal::com_ptr<IMDInternalEmit> otherInternal;
    ASSERT_EQ(S_OK, other->QueryInterface(IID_IMDInternalEmit, (void**)&otherInternal));
    ASSERT_EQ(S_OK, otherInternal->SetMDUpdateMode(MDUpdateExtension, &previous));
    EXPECT_EQ(MDUpdateFull, previous);
    ASSERT_EQ(S_OK, internal->SetMDUpdateMode(MDUpdateFull, &previous));
    EXPECT_EQ(MDUpdateFull, previous);
}

TEST(UpdateMode, ENCRecordsCoreEditsAndResetsWithoutChangingTokens)
{
    minipal::com_ptr<IMetaDataDispenserEx> dispenser;
    ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenserEx, (void**)&dispenser));
    VARIANT option{};
    V_VT(&option) = VT_UI4;
    V_UI4(&option) = MDUpdateENC;
    ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataSetUpdate, &option));

    minipal::com_ptr<IMetaDataEmit2> emit;
    ASSERT_EQ(S_OK, dispenser->DefineScope(CLSID_CorMetaDataRuntime, 0,
        IID_IMetaDataEmit2, (IUnknown**)&emit));
    BYTE methodSig[] = { IMAGE_CEE_CS_CALLCONV_DEFAULT, 0, ELEMENT_TYPE_VOID };
    BYTE fieldSig[] = { IMAGE_CEE_CS_CALLCONV_FIELD, ELEMENT_TYPE_I4 };
    mdTypeDef type;
    mdTypeRef reference;
    mdMethodDef method;
    mdFieldDef field;
    ASSERT_EQ(S_OK, emit->DefineTypeDef(W("MyType"), tdPublic, mdTypeDefNil, nullptr, &type));
    ASSERT_EQ(S_OK, emit->DefineTypeRefByName(TokenFromRid(1, mdtModule), W("MyBase"), &reference));
    ASSERT_EQ(S_OK, emit->DefineMethod(type, W("Run"), mdPublic, methodSig, sizeof(methodSig),
        0, 0, &method));
    ASSERT_EQ(S_OK, emit->DefineField(type, W("Value"), fdPublic, fieldSig, sizeof(fieldSig),
        ELEMENT_TYPE_VOID, nullptr, 0, &field));
    ASSERT_EQ(S_OK, emit->SetTypeDefProps(type, tdPublic, reference, nullptr));
    ASSERT_EQ(S_OK, emit->SetMethodProps(method, mdPublic, 42, 0));
    ASSERT_EQ(S_OK, emit->SetFieldProps(field, fdPublic | fdStatic,
        ELEMENT_TYPE_VOID, nullptr, 0));

    std::vector<LogEntry> log;
    ASSERT_NO_FATAL_FAILURE(ReadENCLog(emit.p, log));
    EXPECT_EQ((std::vector<LogEntry>{
        { type, 0 }, { reference, 0 }, { type, 1 }, { method, 0 },
        { type, 2 }, { field, 0 }, { type, 0 }, { method, 0 }, { field, 0 }
    }), log);

    DWORD beforeReset = 0;
    ASSERT_EQ(S_OK, emit->GetSaveSize(cssAccurate, &beforeReset));
    ASSERT_EQ(S_OK, emit->ResetENCLog());
    log.clear();
    DWORD afterReset = 0;
    ASSERT_NO_FATAL_FAILURE(ReadENCLog(emit.p, log, &afterReset));
    EXPECT_TRUE(log.empty());
    EXPECT_LT(afterReset, beforeReset);

    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));
    WCHAR name[32]{};
    ULONG nameLength;
    DWORD flags;
    mdToken base;
    ASSERT_EQ(S_OK, import->GetTypeDefProps(type, name, 32, &nameLength, &flags, &base));
    EXPECT_EQ(reference, base);
    EXPECT_EQ(WSTR_string(W("MyType")), WSTR_string(name));

    ASSERT_EQ(S_OK, emit->SetRVA(method, 48));
    log.clear();
    ASSERT_NO_FATAL_FAILURE(ReadENCLog(emit.p, log));
    EXPECT_EQ((std::vector<LogEntry>{ { method, 0 } }), log);
}

TEST(UpdateMode, FullAndExtensionDoNotRecordENCLog)
{
    for (ULONG mode : { MDUpdateFull, MDUpdateExtension })
    {
        minipal::com_ptr<IMetaDataDispenserEx> dispenser;
        ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenserEx, (void**)&dispenser));
        VARIANT option{};
        V_VT(&option) = VT_UI4;
        V_UI4(&option) = mode;
        ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataSetUpdate, &option));

        minipal::com_ptr<IMetaDataEmit2> emit;
        ASSERT_EQ(S_OK, dispenser->DefineScope(CLSID_CorMetaDataRuntime, 0,
            IID_IMetaDataEmit2, (IUnknown**)&emit));
        mdTypeDef type;
        ASSERT_EQ(S_OK, emit->DefineTypeDef(W("Unlogged"), tdPublic, mdTypeDefNil, nullptr, &type));
        std::vector<LogEntry> log;
        ASSERT_NO_FATAL_FAILURE(ReadENCLog(emit.p, log));
        EXPECT_TRUE(log.empty());
        EXPECT_EQ(META_E_NOT_IN_ENC_MODE, emit->ResetENCLog());
    }
}

TEST(UpdateMode, LogsRelatedRowsAndCreationOperations)
{
    minipal::com_ptr<IMetaDataDispenserEx> dispenser;
    ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenserEx, (void**)&dispenser));
    VARIANT option{};
    V_VT(&option) = VT_UI4;
    V_UI4(&option) = MDUpdateENC;
    ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataSetUpdate, &option));

    minipal::com_ptr<IMetaDataEmit2> emit;
    ASSERT_EQ(S_OK, dispenser->DefineScope(CLSID_CorMetaDataRuntime, 0,
        IID_IMetaDataEmit2, (IUnknown**)&emit));
    minipal::com_ptr<IMetaDataEmitHelper> helper;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataEmitHelper, (void**)&helper));
    mdTypeDef type, nested;
    mdTypeRef reference;
    mdMethodDef method;
    mdFieldDef field;
    mdEvent eventToken, secondEvent;
    mdProperty property;
    BYTE methodSig[] = { IMAGE_CEE_CS_CALLCONV_DEFAULT, 0, ELEMENT_TYPE_VOID };
    BYTE fieldSig[] = { IMAGE_CEE_CS_CALLCONV_FIELD, ELEMENT_TYPE_I4 };
    BYTE propertySig[] = { IMAGE_CEE_CS_CALLCONV_PROPERTY, 0, ELEMENT_TYPE_I4 };
    ASSERT_EQ(S_OK, emit->DefineTypeDef(W("Container"), tdPublic, mdTypeDefNil, nullptr, &type));
    ASSERT_EQ(S_OK, emit->DefineTypeRefByName(TokenFromRid(1, mdtModule), W("Handler"), &reference));
    mdToken implementations[] = { reference, mdTokenNil };
    ASSERT_EQ(S_OK, emit->SetTypeDefProps(type, UINT32_MAX, UINT32_MAX, implementations));
    ASSERT_EQ(S_OK, emit->DefineNestedType(W("Nested"), tdNestedPublic, mdTypeDefNil,
        nullptr, type, &nested));
    ASSERT_EQ(S_OK, emit->DefineMethod(type, W("Fire"), mdPublic,
        methodSig, sizeof(methodSig), 0, 0, &method));
    ASSERT_EQ(S_OK, emit->DefineField(type, W("State"), fdPublic,
        fieldSig, sizeof(fieldSig), ELEMENT_TYPE_VOID, nullptr, 0, &field));
    ASSERT_EQ(S_OK, emit->SetClassLayout(type, 4, nullptr, 16));
    ASSERT_EQ(S_OK, helper->SetFieldLayoutHelper(field, 8));
    ASSERT_EQ(S_OK, helper->SetFieldLayoutHelper(field, 12));
    ASSERT_EQ(S_OK, helper->DefineEventHelper(type, W("First"), 0, reference, &eventToken));
    ASSERT_EQ(S_OK, helper->DefineMethodSemanticsHelper(eventToken, msAddOn, method));
    ASSERT_EQ(S_OK, helper->DefineEventHelper(type, W("Second"), 0, reference, &secondEvent));
    ASSERT_EQ(S_OK, emit->DefineProperty(type, W("Value"), 0, propertySig, sizeof(propertySig),
        ELEMENT_TYPE_VOID, nullptr, 0, mdMethodDefNil, mdMethodDefNil, nullptr, &property));
    ASSERT_EQ(S_OK, helper->SetResolutionScopeHelper(reference, TokenFromRid(1, mdtModule)));
    ASSERT_EQ(S_OK, emit->DeleteClassLayout(type));

    std::vector<LogEntry> log;
    ASSERT_NO_FATAL_FAILURE(ReadENCLog(emit.p, log));
    EXPECT_EQ((std::vector<LogEntry>{
        { type, 0 }, { reference, 0 }, { TokenFromRid(1, mdtInterfaceImpl), 0 },
        { type, 0 }, { nested, 0 }, { RecordToken(mdtid_NestedClass, 1), 0 },
        { type, 1 }, { method, 0 }, { type, 2 }, { field, 0 },
        { RecordToken(mdtid_ClassLayout, 1), 0 },
        { RecordToken(mdtid_FieldLayout, 1), 0 },
        { RecordToken(mdtid_FieldLayout, 1), 0 },
        { RecordToken(mdtid_EventMap, 1), 0 },
        { RecordToken(mdtid_EventMap, 1), 5 }, { eventToken, 0 },
        { RecordToken(mdtid_MethodSemantics, 1), 0 },
        { RecordToken(mdtid_EventMap, 1), 5 }, { secondEvent, 0 },
        { RecordToken(mdtid_PropertyMap, 1), 0 },
        { RecordToken(mdtid_PropertyMap, 1), 4 }, { property, 0 },
        { reference, 0 }, { RecordToken(mdtid_ClassLayout, 1), 0 },
        { RecordToken(mdtid_FieldLayout, 1), 0 }
    }), log);
}

TEST(UpdateMode, UpdatingInterfacesLogsExistingAndReplacementRows)
{
    minipal::com_ptr<IMetaDataDispenserEx> dispenser;
    ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenserEx, (void**)&dispenser));
    VARIANT option{};
    V_VT(&option) = VT_UI4;
    V_UI4(&option) = MDUpdateENC;
    ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataSetUpdate, &option));
    minipal::com_ptr<IMetaDataEmit2> emit;
    ASSERT_EQ(S_OK, dispenser->DefineScope(CLSID_CorMetaDataRuntime, 0,
        IID_IMetaDataEmit2, (IUnknown**)&emit));
    mdTypeDef type;
    mdTypeRef first, second, replacement;
    ASSERT_EQ(S_OK, emit->DefineTypeDef(W("Implementation"), tdPublic, mdTypeDefNil, nullptr, &type));
    ASSERT_EQ(S_OK, emit->DefineTypeRefByName(TokenFromRid(1, mdtModule), W("First"), &first));
    ASSERT_EQ(S_OK, emit->DefineTypeRefByName(TokenFromRid(1, mdtModule), W("Second"), &second));
    ASSERT_EQ(S_OK, emit->DefineTypeRefByName(TokenFromRid(1, mdtModule), W("Replacement"), &replacement));
    mdToken implementations[] = { first, second, mdTokenNil };
    mdToken replacements[] = { replacement, mdTokenNil };
    ASSERT_EQ(S_OK, emit->SetTypeDefProps(type, UINT32_MAX, UINT32_MAX, implementations));
    ASSERT_EQ(S_OK, emit->SetTypeDefProps(type, UINT32_MAX, UINT32_MAX, replacements));

    std::vector<LogEntry> log;
    ASSERT_NO_FATAL_FAILURE(ReadENCLog(emit.p, log));
    EXPECT_EQ((std::vector<LogEntry>{
        { type, 0 }, { first, 0 }, { second, 0 }, { replacement, 0 },
        { TokenFromRid(1, mdtInterfaceImpl), 0 },
        { TokenFromRid(2, mdtInterfaceImpl), 0 }, { type, 0 },
        { TokenFromRid(1, mdtInterfaceImpl), 0 },
        { TokenFromRid(2, mdtInterfaceImpl), 0 },
        { TokenFromRid(3, mdtInterfaceImpl), 0 }, { type, 0 }
    }), log);

    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));
    HCORENUM enumerator = nullptr;
    mdInterfaceImpl tokens[3]{};
    ULONG count;
    ASSERT_EQ(S_OK, import->EnumInterfaceImpls(&enumerator, type, tokens, 3, &count));
    EXPECT_EQ(1u, count);
    EXPECT_EQ(TokenFromRid(3, mdtInterfaceImpl), tokens[0]);
    import->CloseEnum(enumerator);
}

TEST(UpdateMode, LogsFieldRVAFieldMarshalAndPinvokeEdits)
{
    minipal::com_ptr<IMetaDataDispenserEx> dispenser;
    ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenserEx, (void**)&dispenser));
    VARIANT option{};
    V_VT(&option) = VT_UI4;
    V_UI4(&option) = MDUpdateENC;
    ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataSetUpdate, &option));
    minipal::com_ptr<IMetaDataEmit2> emit;
    ASSERT_EQ(S_OK, dispenser->DefineScope(CLSID_CorMetaDataRuntime, 0,
        IID_IMetaDataEmit2, (IUnknown**)&emit));
    mdTypeDef type;
    mdFieldDef field;
    mdModuleRef module;
    BYTE fieldSig[] = { IMAGE_CEE_CS_CALLCONV_FIELD, ELEMENT_TYPE_I4 };
    BYTE nativeType[] = { 0x07 };
    ASSERT_EQ(S_OK, emit->DefineTypeDef(W("Storage"), tdPublic, mdTypeDefNil, nullptr, &type));
    ASSERT_EQ(S_OK, emit->DefineField(type, W("Value"), fdPublic,
        fieldSig, sizeof(fieldSig), ELEMENT_TYPE_VOID, nullptr, 0, &field));
    ASSERT_EQ(S_OK, emit->SetFieldRVA(field, 16));
    ASSERT_EQ(S_OK, emit->SetFieldRVA(field, 32));
    ASSERT_EQ(S_OK, emit->SetFieldMarshal(field, nativeType, sizeof(nativeType)));
    ASSERT_EQ(S_OK, emit->DeleteFieldMarshal(field));
    ASSERT_EQ(S_OK, emit->DefineModuleRef(W("native"), &module));
    ASSERT_EQ(S_OK, emit->DefinePinvokeMap(field, 0, W("first"), module));
    ASSERT_EQ(S_OK, emit->SetPinvokeMap(field, 0, W("second"), module));
    ASSERT_EQ(S_OK, emit->DefinePinvokeMap(field, 0, W("third"), module));
    ASSERT_EQ(S_OK, emit->DeletePinvokeMap(field));

    std::vector<LogEntry> log;
    ASSERT_NO_FATAL_FAILURE(ReadENCLog(emit.p, log));
    EXPECT_EQ((std::vector<LogEntry>{
        { type, 0 }, { type, 2 }, { field, 0 },
        { field, 0 }, { RecordToken(mdtid_FieldRva, 1), 0 },
        { field, 0 }, { RecordToken(mdtid_FieldRva, 1), 0 },
        { field, 0 }, { RecordToken(mdtid_FieldMarshal, 1), 0 },
        { RecordToken(mdtid_FieldMarshal, 1), 0 }, { field, 0 },
        { module, 0 }, { field, 0 }, { RecordToken(mdtid_ImplMap, 1), 0 },
        { RecordToken(mdtid_ImplMap, 1), 0 },
        { field, 0 }, { RecordToken(mdtid_ImplMap, 1), 0 },
        { RecordToken(mdtid_ImplMap, 1), 0 }, { field, 0 }
    }), log);

    std::vector<uint8_t> image;
    ASSERT_NO_FATAL_FAILURE(SaveImage(emit.p, image));
    mdhandle_t raw = nullptr;
    ASSERT_TRUE(md_create_handle(image.data(), image.size(), &raw));
    mdhandle_ptr handle{ raw };
    mdcursor_t row;
    uint32_t count;
    ASSERT_TRUE(md_create_cursor(handle.get(), mdtid_FieldRva, &row, &count));
    EXPECT_EQ(1u, count);
    uint32_t rva;
    ASSERT_TRUE(md_get_column_value_as_constant(row, mdtFieldRva_Rva, &rva));
    EXPECT_EQ(32u, rva);
    ASSERT_TRUE(md_create_cursor(handle.get(), mdtid_FieldMarshal, &row, &count));
    EXPECT_EQ(1u, count);
    ASSERT_TRUE(md_create_cursor(handle.get(), mdtid_ImplMap, &row, &count));
    EXPECT_EQ(1u, count);
}

TEST(UpdateMode, ResetENCLogPreservesLaterTables)
{
    minipal::com_ptr<IMetaDataDispenserEx> dispenser;
    ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenserEx, (void**)&dispenser));
    VARIANT option{};
    V_VT(&option) = VT_UI4;
    V_UI4(&option) = MDUpdateENC;
    ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataSetUpdate, &option));
    minipal::com_ptr<IMetaDataEmit2> emit;
    ASSERT_EQ(S_OK, dispenser->DefineScope(CLSID_CorMetaDataRuntime, 0,
        IID_IMetaDataEmit2, (IUnknown**)&emit));
    minipal::com_ptr<IMetaDataAssemblyEmit> assemblyEmit;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataAssemblyEmit, (void**)&assemblyEmit));

    ASSEMBLYMETADATA assemblyMetadata{};
    WCHAR locale[] = W("");
    assemblyMetadata.szLocale = locale;
    mdAssembly assembly;
    ASSERT_EQ(S_OK, assemblyEmit->DefineAssembly(nullptr, 0, 0, W("EnCAssembly"),
        &assemblyMetadata, 0, &assembly));
    mdAssemblyRef reference;
    ASSERT_EQ(S_OK, assemblyEmit->DefineAssemblyRef(nullptr, 0, W("Dependency"),
        &assemblyMetadata, nullptr, 0, 0, &reference));
    std::vector<LogEntry> log;
    ASSERT_NO_FATAL_FAILURE(ReadENCLog(emit.p, log));
    EXPECT_EQ((std::vector<LogEntry>{ { assembly, 0 }, { reference, 0 } }), log);

    std::vector<uint8_t> serialized;
    ASSERT_NO_FATAL_FAILURE(SaveImage(emit.p, serialized));
    minipal::com_ptr<IMetaDataEmit2> reopened;
    ASSERT_EQ(S_OK, dispenser->OpenScopeOnMemory(serialized.data(), (ULONG)serialized.size(),
        ofRead | ofCopyMemory, IID_IMetaDataEmit2, (IUnknown**)&reopened));
    ASSERT_EQ(S_OK, reopened->ResetENCLog());
    std::vector<LogEntry> reopenedLog;
    ASSERT_NO_FATAL_FAILURE(ReadENCLog(reopened.p, reopenedLog));
    EXPECT_TRUE(reopenedLog.empty());
    std::vector<uint8_t> reopenedImage;
    ASSERT_NO_FATAL_FAILURE(SaveImage(reopened.p, reopenedImage));
    mdhandle_t reopenedRaw = nullptr;
    ASSERT_TRUE(md_create_handle(reopenedImage.data(), reopenedImage.size(), &reopenedRaw));
    mdhandle_ptr reopenedHandle{ reopenedRaw };
    mdcursor_t reopenedAssembly;
    ASSERT_TRUE(md_token_to_cursor(reopenedHandle.get(), assembly, &reopenedAssembly));
    char const* reopenedName = nullptr;
    ASSERT_TRUE(md_get_column_value_as_utf8(reopenedAssembly, mdtAssembly_Name, &reopenedName));
    EXPECT_STREQ("EnCAssembly", reopenedName);

    ASSERT_EQ(S_OK, emit->ResetENCLog());
    ASSERT_EQ(S_OK, emit->ResetENCLog());
    log.clear();
    ASSERT_NO_FATAL_FAILURE(ReadENCLog(emit.p, log));
    EXPECT_TRUE(log.empty());

    std::vector<uint8_t> image;
    ASSERT_NO_FATAL_FAILURE(SaveImage(emit.p, image));
    mdhandle_t raw = nullptr;
    ASSERT_TRUE(md_create_handle(image.data(), image.size(), &raw));
    mdhandle_ptr handle{ raw };
    EXPECT_TRUE(md_validate(handle.get()));
    mdcursor_t row;
    ASSERT_TRUE(md_token_to_cursor(handle.get(), assembly, &row));
    char const* name = nullptr;
    ASSERT_TRUE(md_get_column_value_as_utf8(row, mdtAssembly_Name, &name));
    EXPECT_STREQ("EnCAssembly", name);
    ASSERT_TRUE(md_token_to_cursor(handle.get(), reference, &row));
    ASSERT_TRUE(md_get_column_value_as_utf8(row, mdtAssemblyRef_Name, &name));
    EXPECT_STREQ("Dependency", name);
}

TEST(UpdateMode, ChangingModeOnlyRecordsENCOperations)
{
    minipal::com_ptr<IMetaDataEmit2> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdTypeDef type;
    ASSERT_EQ(S_OK, emit->DefineTypeDef(W("Unlogged"), tdPublic, mdTypeDefNil, nullptr, &type));
    minipal::com_ptr<IMDInternalEmit> internal;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMDInternalEmit, (void**)&internal));
    ULONG previous = UINT32_MAX;
    ASSERT_EQ(S_OK, internal->SetMDUpdateMode(MDUpdateENC, &previous));
    EXPECT_EQ(MDUpdateFull, previous);
    mdTypeRef logged, unlogged;
    ASSERT_EQ(S_OK, emit->DefineTypeRefByName(TokenFromRid(1, mdtModule), W("Logged"), &logged));
    ASSERT_EQ(S_OK, internal->SetMDUpdateMode(MDUpdateFull, &previous));
    EXPECT_EQ(MDUpdateENC, previous);
    ASSERT_EQ(S_OK, emit->DefineTypeRefByName(TokenFromRid(1, mdtModule), W("Unlogged"), &unlogged));
    std::vector<LogEntry> log;
    ASSERT_NO_FATAL_FAILURE(ReadENCLog(emit.p, log));
    EXPECT_EQ((std::vector<LogEntry>{ { logged, 0 } }), log);
    EXPECT_EQ(META_E_NOT_IN_ENC_MODE, emit->ResetENCLog());
    ASSERT_EQ(S_OK, internal->SetMDUpdateMode(MDUpdateENC, &previous));
    ASSERT_EQ(S_OK, emit->ResetENCLog());
    log.clear();
    ASSERT_NO_FATAL_FAILURE(ReadENCLog(emit.p, log));
    EXPECT_TRUE(log.empty());
}

TEST(UpdateMode, DeltaGenerationIsNotImplemented)
{
    minipal::com_ptr<IMetaDataDispenserEx> dispenser;
    ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenserEx, (void**)&dispenser));
    VARIANT option{};
    V_VT(&option) = VT_UI4;
    V_UI4(&option) = MDUpdateENC;
    ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataSetUpdate, &option));
    minipal::com_ptr<IMetaDataEmit2> emit;
    ASSERT_EQ(S_OK, dispenser->DefineScope(CLSID_CorMetaDataRuntime, 0,
        IID_IMetaDataEmit2, (IUnknown**)&emit));

    DWORD size = UINT32_MAX;
    EXPECT_EQ(E_NOTIMPL, emit->GetDeltaSaveSize(cssAccurate, &size));
    EXPECT_EQ(UINT32_MAX, size);
    EXPECT_EQ(E_NOTIMPL, emit->SaveDeltaToMemory(nullptr, 0));
    EXPECT_EQ(E_NOTIMPL, emit->SaveDeltaToStream(nullptr, 0));
    EXPECT_EQ(E_NOTIMPL, emit->SaveDelta(nullptr, 0));
}

TEST(UpdateMode, AppliesNonRemappingDeltaAndReplacesENCLog)
{
    for (bool threadSafe : { false, true })
    {
        minipal::com_ptr<IMetaDataDispenserEx> dispenser;
        ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenserEx, (void**)&dispenser));
        VARIANT option{};
        V_VT(&option) = VT_UI4;
        V_UI4(&option) = MDUpdateENC;
        ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataSetUpdate, &option));
        if (threadSafe)
        {
            V_UI4(&option) = MDThreadSafetyOn;
            ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataThreadSafetyOptions, &option));
        }

        minipal::com_ptr<IMetaDataEmit2> emit;
        ASSERT_EQ(S_OK, dispenser->DefineScope(CLSID_CorMetaDataRuntime, 0,
            IID_IMetaDataEmit2, (IUnknown**)&emit));
        mdTypeDef existing;
        ASSERT_EQ(S_OK, emit->DefineTypeDef(W("Existing"), tdPublic, mdTypeDefNil, nullptr, &existing));

        minipal::com_ptr<IMetaDataImport2> import;
        ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport2, (void**)&import));
        minipal::com_ptr<IUnknown> identityBefore, identityAfter;
        ASSERT_EQ(S_OK, emit->QueryInterface(IID_IUnknown, (void**)&identityBefore));
        GUID mvid{};
        ASSERT_EQ(S_OK, import->GetScopeProps(nullptr, 0, nullptr, &mvid));

        std::vector<uint8_t> delta;
        ASSERT_TRUE(CreateMinimalTypeRefDelta(mvid, delta));
        minipal::com_ptr<IMetaDataImport2> deltaImport;
        ASSERT_EQ(S_OK, dispenser->OpenScopeOnMemory(delta.data(), (ULONG)delta.size(),
            (threadSafe ? ofRead : ofReadOnly) | ofCopyMemory,
            IID_IMetaDataImport2, (IUnknown**)&deltaImport));

        ASSERT_EQ(S_OK, emit->ApplyEditAndContinue(deltaImport.p));
        ASSERT_EQ(S_OK, import->QueryInterface(IID_IUnknown, (void**)&identityAfter));
        EXPECT_EQ(identityBefore.p, identityAfter.p);
        mdTypeDef found;
        ASSERT_EQ(S_OK, import->FindTypeDefByName(W("Existing"), mdTokenNil, &found));
        EXPECT_EQ(existing, found);
        WCHAR name[32]{};
        ULONG nameLength;
        mdToken resolutionScope;
        ASSERT_EQ(S_OK, import->GetTypeRefProps(TokenFromRid(1, mdtTypeRef),
            &resolutionScope, name, 32, &nameLength));
        EXPECT_EQ(TokenFromRid(1, mdtModule), resolutionScope);
        EXPECT_EQ(WSTR_string(W("Example.Added")), WSTR_string(name));

        std::vector<LogEntry> log;
        ASSERT_NO_FATAL_FAILURE(ReadENCLog(emit.p, log));
        EXPECT_EQ((std::vector<LogEntry>{ { TokenFromRid(1, mdtTypeRef), 0 } }), log);
    }
}

TEST(UpdateMode, AppliesMemberCreationsAndUpdatesWithoutTokenRemapping)
{
    for (bool create : { true, false })
    {
        minipal::com_ptr<IMetaDataDispenserEx> dispenser;
        ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenserEx, (void**)&dispenser));
        VARIANT option{};
        V_VT(&option) = VT_UI4;
        V_UI4(&option) = MDUpdateENC;
        ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataSetUpdate, &option));
        minipal::com_ptr<IMetaDataEmit2> emit;
        ASSERT_EQ(S_OK, dispenser->DefineScope(CLSID_CorMetaDataRuntime, 0,
            IID_IMetaDataEmit2, (IUnknown**)&emit));
        mdTypeDef type;
        ASSERT_EQ(S_OK, emit->DefineTypeDef(W("Existing"), tdPublic, mdTypeDefNil, nullptr, &type));

        BYTE oldMethodSig[] = { IMAGE_CEE_CS_CALLCONV_DEFAULT, 0, ELEMENT_TYPE_I4 };
        BYTE oldFieldSig[] = { IMAGE_CEE_CS_CALLCONV_FIELD, ELEMENT_TYPE_I8 };
        if (!create)
        {
            mdMethodDef oldMethod;
            mdFieldDef oldField;
            ASSERT_EQ(S_OK, emit->DefineMethod(type, W("OldMethod"), mdPublic,
                oldMethodSig, sizeof(oldMethodSig), 0, 0, &oldMethod));
            ASSERT_EQ(TokenFromRid(1, mdtMethodDef), oldMethod);
            ASSERT_EQ(S_OK, emit->DefineField(type, W("OldField"), fdPublic,
                oldFieldSig, sizeof(oldFieldSig), ELEMENT_TYPE_VOID, nullptr, 0, &oldField));
            ASSERT_EQ(TokenFromRid(1, mdtFieldDef), oldField);
        }
        minipal::com_ptr<IMetaDataImport2> import;
        ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport2, (void**)&import));
        GUID mvid{};
        ASSERT_EQ(S_OK, import->GetScopeProps(nullptr, 0, nullptr, &mvid));
        std::vector<uint8_t> delta;
        ASSERT_TRUE(CreateMinimalTypeRefDelta(mvid, delta));
        ASSERT_TRUE(AddMemberChangesToDelta(delta, create));
        minipal::com_ptr<IMetaDataImport2> deltaImport;
        ASSERT_EQ(S_OK, dispenser->OpenScopeOnMemory(delta.data(), (ULONG)delta.size(),
            ofReadOnly | ofCopyMemory, IID_IMetaDataImport2, (IUnknown**)&deltaImport));
        ASSERT_EQ(S_OK, emit->ApplyEditAndContinue(deltaImport.p));

        mdTypeDef parent;
        WCHAR name[32]{};
        ULONG nameLength, sigLength, rva;
        DWORD flags, implFlags;
        PCCOR_SIGNATURE signature;
        mdToken extends;
        ASSERT_EQ(S_OK, import->GetTypeDefProps(type, name, 32, &nameLength, &flags, &extends));
        EXPECT_EQ(WSTR_string(W("EditedType")), WSTR_string(name));
        EXPECT_EQ(tdPublic | tdAbstract, flags);
        ASSERT_EQ(S_OK, import->GetMethodProps(TokenFromRid(1, mdtMethodDef), &parent, name, 32,
            &nameLength, &flags, &signature, &sigLength, &rva, &implFlags));
        EXPECT_EQ(type, parent);
        EXPECT_EQ(WSTR_string(W("AddedMethod")), WSTR_string(name));
        EXPECT_EQ(mdPublic | mdStatic, flags);
        EXPECT_EQ(77u, rva);
        BYTE methodSig[] = { IMAGE_CEE_CS_CALLCONV_DEFAULT, 0, ELEMENT_TYPE_VOID };
        ASSERT_EQ(sizeof(methodSig), sigLength);
        EXPECT_EQ(0, std::memcmp(methodSig, signature, sigLength));

        DWORD constantType;
        UVCP_CONSTANT value;
        ULONG valueLength;
        ASSERT_EQ(S_OK, import->GetFieldProps(TokenFromRid(1, mdtFieldDef), &parent, name, 32,
            &nameLength, &flags, &signature, &sigLength, &constantType, &value, &valueLength));
        EXPECT_EQ(type, parent);
        EXPECT_EQ(WSTR_string(W("AddedField")), WSTR_string(name));
        EXPECT_EQ(fdPublic | fdStatic, flags);
        BYTE fieldSig[] = { IMAGE_CEE_CS_CALLCONV_FIELD, ELEMENT_TYPE_I4 };
        ASSERT_EQ(sizeof(fieldSig), sigLength);
        EXPECT_EQ(0, std::memcmp(fieldSig, signature, sigLength));

        HCORENUM methods = nullptr;
        mdMethodDef methodTokens[2]{};
        ULONG count = 0;
        ASSERT_EQ(S_OK, import->EnumMethods(&methods, type, methodTokens, 2, &count));
        EXPECT_EQ(1u, count);
        EXPECT_EQ(TokenFromRid(1, mdtMethodDef), methodTokens[0]);
        import->CloseEnum(methods);
        HCORENUM fields = nullptr;
        mdFieldDef fieldTokens[2]{};
        ASSERT_EQ(S_OK, import->EnumFields(&fields, type, fieldTokens, 2, &count));
        EXPECT_EQ(1u, count);
        EXPECT_EQ(TokenFromRid(1, mdtFieldDef), fieldTokens[0]);
        import->CloseEnum(fields);

        std::vector<LogEntry> log;
        ASSERT_NO_FATAL_FAILURE(ReadENCLog(emit.p, log));
        if (create)
        {
            EXPECT_EQ((std::vector<LogEntry>{
                { TokenFromRid(1, mdtTypeRef), 0 }, { type, 0 }, { type, 1 },
                { TokenFromRid(1, mdtMethodDef), 0 }, { type, 2 },
                { TokenFromRid(1, mdtFieldDef), 0 }
            }), log);
        }
        else
        {
            EXPECT_EQ((std::vector<LogEntry>{
                { TokenFromRid(1, mdtTypeRef), 0 }, { type, 0 },
                { TokenFromRid(1, mdtMethodDef), 0 },
                { TokenFromRid(1, mdtFieldDef), 0 }
            }), log);
        }
    }
}

TEST(UpdateMode, AppliesParameterEventAndPropertyCreateOperations)
{
    minipal::com_ptr<IMetaDataDispenserEx> dispenser;
    ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenserEx, (void**)&dispenser));
    VARIANT option{};
    V_VT(&option) = VT_UI4;
    V_UI4(&option) = MDUpdateENC;
    ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataSetUpdate, &option));
    minipal::com_ptr<IMetaDataEmit2> emit;
    ASSERT_EQ(S_OK, dispenser->DefineScope(CLSID_CorMetaDataRuntime, 0,
        IID_IMetaDataEmit2, (IUnknown**)&emit));
    mdTypeDef type;
    ASSERT_EQ(S_OK, emit->DefineTypeDef(W("Existing"), tdPublic, mdTypeDefNil, nullptr, &type));
    minipal::com_ptr<IMetaDataImport2> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport2, (void**)&import));
    GUID mvid{};
    ASSERT_EQ(S_OK, import->GetScopeProps(nullptr, 0, nullptr, &mvid));
    std::vector<uint8_t> delta;
    ASSERT_TRUE(CreateMinimalTypeRefDelta(mvid, delta));
    ASSERT_TRUE(AddMemberChangesToDelta(delta, true, true));
    minipal::com_ptr<IMetaDataImport2> deltaImport;
    ASSERT_EQ(S_OK, dispenser->OpenScopeOnMemory(delta.data(), (ULONG)delta.size(),
        ofReadOnly | ofCopyMemory, IID_IMetaDataImport2, (IUnknown**)&deltaImport));
    ASSERT_EQ(S_OK, emit->ApplyEditAndContinue(deltaImport.p));

    std::vector<uint8_t> image;
    ASSERT_NO_FATAL_FAILURE(SaveImage(emit.p, image));
    mdhandle_t raw = nullptr;
    ASSERT_TRUE(md_create_handle(image.data(), image.size(), &raw));
    mdhandle_ptr handle{ raw };
    EXPECT_TRUE(md_validate(handle.get()));
    mdcursor_t row;
    char const* name = nullptr;
    ASSERT_TRUE(md_token_to_cursor(handle.get(), TokenFromRid(1, mdtParamDef), &row));
    ASSERT_TRUE(md_get_column_value_as_utf8(row, mdtParam_Name, &name));
    EXPECT_STREQ("Argument", name);
    ASSERT_TRUE(md_token_to_cursor(handle.get(), TokenFromRid(1, mdtEvent), &row));
    ASSERT_TRUE(md_get_column_value_as_utf8(row, mdtEvent_Name, &name));
    EXPECT_STREQ("Changed", name);
    ASSERT_TRUE(md_token_to_cursor(handle.get(), TokenFromRid(1, mdtProperty), &row));
    ASSERT_TRUE(md_get_column_value_as_utf8(row, mdtProperty_Name, &name));
    EXPECT_STREQ("Count", name);

    HCORENUM enumeration = nullptr;
    mdParamDef parameters[2]{};
    ULONG count;
    ASSERT_EQ(S_OK, import->EnumParams(&enumeration, TokenFromRid(1, mdtMethodDef),
        parameters, 2, &count));
    EXPECT_EQ(1u, count);
    EXPECT_EQ(TokenFromRid(1, mdtParamDef), parameters[0]);
    import->CloseEnum(enumeration);
    DWORD semantics;
    ASSERT_EQ(S_OK, import->GetMethodSemantics(TokenFromRid(1, mdtMethodDef),
        TokenFromRid(1, mdtEvent), &semantics));
    EXPECT_EQ(msAddOn, semantics);

    std::vector<LogEntry> log;
    ASSERT_NO_FATAL_FAILURE(ReadENCLog(emit.p, log));
    EXPECT_EQ((std::vector<LogEntry>{
        { TokenFromRid(1, mdtTypeRef), 0 }, { type, 0 },
        { type, 1 }, { TokenFromRid(1, mdtMethodDef), 0 },
        { type, 2 }, { TokenFromRid(1, mdtFieldDef), 0 },
        { TokenFromRid(1, mdtMethodDef), 3 }, { TokenFromRid(1, mdtParamDef), 0 },
        { RecordToken(mdtid_EventMap, 1), 0 },
        { RecordToken(mdtid_EventMap, 1), 5 }, { TokenFromRid(1, mdtEvent), 0 },
        { RecordToken(mdtid_MethodSemantics, 1), 0 },
        { RecordToken(mdtid_PropertyMap, 1), 0 },
        { RecordToken(mdtid_PropertyMap, 1), 4 }, { TokenFromRid(1, mdtProperty), 0 }
    }), log);

    minipal::com_ptr<IMDInternalImportENC> internalENC;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMDInternalImportENC, (void**)&internalENC));
    HENUMInternal deltaTokens{};
    ASSERT_EQ(S_OK, internalENC->EnumDeltaTokensInit(&deltaTokens));
    EXPECT_EQ(7u, internalENC->EnumGetCount(&deltaTokens));
    std::vector<mdToken> tokens;
    mdToken token;
    while (internalENC->EnumNext(&deltaTokens, &token))
        tokens.push_back(token);
    internalENC->EnumClose(&deltaTokens);
    EXPECT_EQ((std::vector<mdToken>{
        TokenFromRid(1, mdtTypeRef), type,
        TokenFromRid(1, mdtMethodDef), TokenFromRid(1, mdtFieldDef),
        TokenFromRid(1, mdtParamDef), TokenFromRid(1, mdtEvent),
        TokenFromRid(1, mdtProperty)
    }), tokens);
}

TEST(UpdateMode, ConsecutiveDeltasKeepScopeIdentityAndHeapPointers)
{
    minipal::com_ptr<IMetaDataDispenserEx> dispenser;
    ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenserEx, (void**)&dispenser));
    VARIANT option{};
    V_VT(&option) = VT_UI4;
    V_UI4(&option) = MDUpdateENC;
    ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataSetUpdate, &option));
    minipal::com_ptr<IMetaDataEmit2> emit;
    ASSERT_EQ(S_OK, dispenser->DefineScope(CLSID_CorMetaDataRuntime, 0,
        IID_IMetaDataEmit2, (IUnknown**)&emit));
    minipal::com_ptr<IMDInternalImport> internal;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMDInternalImport, (void**)&internal));
    minipal::com_ptr<IMetaDataImport2> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport2, (void**)&import));
    GUID mvid{};
    ASSERT_EQ(S_OK, import->GetScopeProps(nullptr, 0, nullptr, &mvid));

    std::vector<uint8_t> firstDelta;
    ASSERT_TRUE(CreateMinimalTypeRefDelta(mvid, firstDelta));
    minipal::com_ptr<IMetaDataImport2> firstImport;
    ASSERT_EQ(S_OK, dispenser->OpenScopeOnMemory(firstDelta.data(), (ULONG)firstDelta.size(),
        ofReadOnly | ofCopyMemory, IID_IMetaDataImport2, (IUnknown**)&firstImport));
    ASSERT_EQ(S_OK, emit->ApplyEditAndContinue(firstImport.p));
    minipal::com_ptr<IMDInternalImportENC> internalENC;
    ASSERT_EQ(S_OK, internal->QueryInterface(IID_IMDInternalImportENC, (void**)&internalENC));
    HENUMInternal firstTokens{};
    ASSERT_EQ(S_OK, internalENC->EnumDeltaTokensInit(&firstTokens));
    EXPECT_EQ(1u, internalENC->EnumGetCount(&firstTokens));

    char const* oldName = nullptr;
    char const* oldNamespace = nullptr;
    ASSERT_EQ(S_OK, internal->GetNameOfTypeRef(TokenFromRid(1, mdtTypeRef), &oldNamespace, &oldName));
    EXPECT_STREQ("Added", oldName);

    std::vector<uint8_t> firstImage;
    ASSERT_NO_FATAL_FAILURE(SaveImage(emit.p, firstImage));
    mdhandle_t raw = nullptr;
    ASSERT_TRUE(md_create_handle(firstImage.data(), firstImage.size(), &raw));
    mdhandle_ptr snapshot{ raw };
    mdcursor_t module;
    ASSERT_TRUE(md_token_to_cursor(snapshot.get(), TokenFromRid(1, mdtModule), &module));
    mdguid_t previousEncId{};
    ASSERT_TRUE(md_get_column_value_as_guid(module, mdtModule_EncId, &previousEncId));
    GUID previousGuid;
    static_assert(sizeof(previousGuid) == sizeof(previousEncId));
    std::memcpy(&previousGuid, &previousEncId, sizeof(previousGuid));
    GUID nextGuid{ 0x5ddf2f6a, 0xd795, 0x49ea, { 0x96, 0x56, 0x0d, 0xdf, 0x3e, 0x6d, 0xaa, 0x94 } };
    std::vector<uint8_t> nextDelta;
    ASSERT_TRUE(CreateMinimalTypeRefDelta(mvid, nextDelta, false, false,
        &previousGuid, &nextGuid, "Updated"));
    ASSERT_TRUE(AppendTypeRefsToDelta(nextDelta, 2, true));
    minipal::com_ptr<IMetaDataImport2> nextImport;
    ASSERT_EQ(S_OK, dispenser->OpenScopeOnMemory(nextDelta.data(), (ULONG)nextDelta.size(),
        ofReadOnly | ofCopyMemory, IID_IMetaDataImport2, (IUnknown**)&nextImport));
    ASSERT_EQ(S_OK, emit->ApplyEditAndContinue(nextImport.p));

    char const* updatedName = nullptr;
    char const* updatedNamespace = nullptr;
    ASSERT_EQ(S_OK, internal->GetNameOfTypeRef(TokenFromRid(1, mdtTypeRef),
        &updatedNamespace, &updatedName));
    EXPECT_STREQ("Updated", updatedName);
    EXPECT_STREQ("Example", updatedNamespace);
    EXPECT_STREQ("Added", oldName);
    EXPECT_STREQ("Example", oldNamespace);

    std::vector<LogEntry> log;
    ASSERT_NO_FATAL_FAILURE(ReadENCLog(emit.p, log));
    EXPECT_EQ((std::vector<LogEntry>{
        { TokenFromRid(1, mdtTypeRef), 0 },
        { TokenFromRid(2, mdtTypeRef), 0 }, { TokenFromRid(2, mdtTypeRef), 0 }
    }), log);
    HENUMInternal secondTokens{};
    ASSERT_EQ(S_OK, internalENC->EnumDeltaTokensInit(&secondTokens));
    EXPECT_EQ(3u, internalENC->EnumGetCount(&secondTokens));
    mdToken enumerated = mdTokenNil;
    EXPECT_TRUE(internalENC->EnumNext(&secondTokens, &enumerated));
    EXPECT_EQ(TokenFromRid(1, mdtTypeRef), enumerated);
    EXPECT_TRUE(internalENC->EnumNext(&secondTokens, &enumerated));
    EXPECT_EQ(TokenFromRid(2, mdtTypeRef), enumerated);
    EXPECT_TRUE(internalENC->EnumNext(&secondTokens, &enumerated));
    EXPECT_EQ(TokenFromRid(2, mdtTypeRef), enumerated);
    EXPECT_FALSE(internalENC->EnumNext(&secondTokens, &enumerated));
    internalENC->EnumClose(&secondTokens);
    EXPECT_EQ(1u, internalENC->EnumGetCount(&firstTokens));
    EXPECT_TRUE(internalENC->EnumNext(&firstTokens, &enumerated));
    EXPECT_EQ(TokenFromRid(1, mdtTypeRef), enumerated);
    EXPECT_FALSE(internalENC->EnumNext(&firstTokens, &enumerated));
    internalENC->EnumClose(&firstTokens);

    GUID rejectedId{ 0xf744f937, 0x640e, 0x42b8, { 0xb5, 0x95, 0x03, 0x58, 0x1e, 0xed, 0xc3, 0x30 } };
    std::vector<uint8_t> rejectedDelta;
    ASSERT_TRUE(CreateMinimalTypeRefDelta(mvid, rejectedDelta, false, false,
        &previousGuid, &rejectedId, "Rejected"));
    minipal::com_ptr<IMetaDataImport2> rejectedImport;
    ASSERT_EQ(S_OK, dispenser->OpenScopeOnMemory(rejectedDelta.data(), (ULONG)rejectedDelta.size(),
        ofReadOnly | ofCopyMemory, IID_IMetaDataImport2, (IUnknown**)&rejectedImport));
    EXPECT_EQ(E_INVALIDARG, emit->ApplyEditAndContinue(rejectedImport.p));
    HENUMInternal afterFailure{};
    ASSERT_EQ(S_OK, internalENC->EnumDeltaTokensInit(&afterFailure));
    EXPECT_EQ(3u, internalENC->EnumGetCount(&afterFailure));
    internalENC->EnumClose(&afterFailure);

    std::vector<uint8_t> finalImage;
    ASSERT_NO_FATAL_FAILURE(SaveImage(emit.p, finalImage));
    raw = nullptr;
    ASSERT_TRUE(md_create_handle(finalImage.data(), finalImage.size(), &raw));
    mdhandle_ptr finalHandle{ raw };
    ASSERT_TRUE(md_token_to_cursor(finalHandle.get(), TokenFromRid(1, mdtModule), &module));
    mdguid_t actualId{};
    ASSERT_TRUE(md_get_column_value_as_guid(module, mdtModule_EncId, &actualId));
    EXPECT_EQ(0, std::memcmp(&actualId, &nextGuid, sizeof(nextGuid)));
}

TEST(UpdateMode, InvalidAndRemappingDeltasDoNotModifyScope)
{
    minipal::com_ptr<IMetaDataDispenserEx> dispenser;
    ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenserEx, (void**)&dispenser));
    VARIANT option{};
    V_VT(&option) = VT_UI4;
    V_UI4(&option) = MDUpdateENC;
    ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataSetUpdate, &option));
    minipal::com_ptr<IMetaDataEmit2> emit;
    ASSERT_EQ(S_OK, dispenser->DefineScope(CLSID_CorMetaDataRuntime, 0,
        IID_IMetaDataEmit2, (IUnknown**)&emit));
    mdTypeDef existing;
    ASSERT_EQ(S_OK, emit->DefineTypeDef(W("Existing"), tdPublic, mdTypeDefNil, nullptr, &existing));
    minipal::com_ptr<IMetaDataImport2> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport2, (void**)&import));
    GUID mvid{};
    ASSERT_EQ(S_OK, import->GetScopeProps(nullptr, 0, nullptr, &mvid));
    std::vector<uint8_t> before, after;
    ASSERT_NO_FATAL_FAILURE(SaveImage(emit.p, before));

    for (bool remap : { false, true })
    {
        std::vector<uint8_t> delta;
        ASSERT_TRUE(CreateMinimalTypeRefDelta(mvid, delta, !remap, remap));
        minipal::com_ptr<IMetaDataImport2> deltaImport;
        ASSERT_EQ(S_OK, dispenser->OpenScopeOnMemory(delta.data(), (ULONG)delta.size(),
            ofReadOnly | ofCopyMemory, IID_IMetaDataImport2, (IUnknown**)&deltaImport));
        EXPECT_EQ(remap ? E_NOTIMPL : E_INVALIDARG, emit->ApplyEditAndContinue(deltaImport.p));
        ASSERT_NO_FATAL_FAILURE(SaveImage(emit.p, after));
        EXPECT_EQ(before, after);
    }

    std::vector<uint8_t> corrupt;
    ASSERT_TRUE(CreateMinimalTypeRefDelta(mvid, corrupt));
    ASSERT_TRUE(AddMemberChangesToDelta(corrupt, true));
    mdhandle_t raw = nullptr;
    ASSERT_TRUE(md_create_handle(corrupt.data(), corrupt.size(), &raw));
    mdhandle_ptr parsed{ raw };
    mdcursor_t method;
    ASSERT_TRUE(md_token_to_cursor(parsed.get(), TokenFromRid(1, mdtMethodDef), &method));
    uint8_t const* signature;
    uint32_t length;
    ASSERT_TRUE(md_get_column_value_as_blob(method, mdtMethodDef_Signature, &signature, &length));
    ASSERT_EQ(3u, length);
    size_t blobOffset = signature - corrupt.data();
    ASSERT_GT(blobOffset, 0u);
    ASSERT_GE(corrupt.size() - blobOffset, 3u);
    parsed.reset();
    corrupt[blobOffset - 1] = 0xdf;
    for (size_t i = 0; i < 3; ++i)
        corrupt[blobOffset + i] = 0xff;
    minipal::com_ptr<IMetaDataImport2> corruptImport;
    ASSERT_EQ(S_OK, dispenser->OpenScopeOnMemory(corrupt.data(), (ULONG)corrupt.size(),
        ofReadOnly | ofCopyMemory, IID_IMetaDataImport2, (IUnknown**)&corruptImport));
    EXPECT_EQ(E_INVALIDARG, emit->ApplyEditAndContinue(corruptImport.p));
    ASSERT_NO_FATAL_FAILURE(SaveImage(emit.p, after));
    EXPECT_EQ(before, after);
    EXPECT_EQ(E_INVALIDARG, emit->ApplyEditAndContinue(nullptr));
}

TEST(UpdateMode, InternalENCInterfaceHasWritableScopeIdentityAndNoLegacyDeltaOverload)
{
    for (bool threadSafe : { false, true })
    {
        minipal::com_ptr<IMetaDataDispenserEx> dispenser;
        ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenserEx, (void**)&dispenser));
        if (threadSafe)
        {
            VARIANT option{};
            V_VT(&option) = VT_UI4;
            V_UI4(&option) = MDThreadSafetyOn;
            ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataThreadSafetyOptions, &option));
        }

        minipal::com_ptr<IMDInternalImport> internal;
        ASSERT_EQ(S_OK, dispenser->DefineScope(CLSID_CorMetaDataRuntime, 0,
            IID_IMDInternalImport, (IUnknown**)&internal));
        minipal::com_ptr<IMDInternalImportENC> internalENC;
        ASSERT_EQ(S_OK, internal->QueryInterface(IID_IMDInternalImportENC, (void**)&internalENC));
        EXPECT_EQ(internal.p, static_cast<IMDInternalImport*>(internalENC.p));

        minipal::com_ptr<IUnknown> internalIdentity, encIdentity, emitIdentity;
        ASSERT_EQ(S_OK, internal->QueryInterface(IID_IUnknown, (void**)&internalIdentity));
        ASSERT_EQ(S_OK, internalENC->QueryInterface(IID_IUnknown, (void**)&encIdentity));
        EXPECT_EQ(internalIdentity.p, encIdentity.p);
        minipal::com_ptr<IMetaDataEmit2> emit;
        ASSERT_EQ(S_OK, internalENC->QueryInterface(IID_IMetaDataEmit2, (void**)&emit));
        ASSERT_EQ(S_OK, emit->QueryInterface(IID_IUnknown, (void**)&emitIdentity));
        EXPECT_EQ(internalIdentity.p, emitIdentity.p);

        EXPECT_EQ(E_NOTIMPL, internalENC->ApplyEditAndContinue(static_cast<MDInternalRW*>(nullptr)));
        EXPECT_EQ(E_INVALIDARG, internalENC->EnumDeltaTokensInit(nullptr));
        HENUMInternal empty{};
        ASSERT_EQ(S_OK, internalENC->EnumDeltaTokensInit(&empty));
        EXPECT_EQ(0u, internalENC->EnumGetCount(&empty));
        mdToken token = 0xdeadbeef;
        EXPECT_FALSE(internalENC->EnumNext(&empty, &token));
        EXPECT_EQ(0xdeadbeefu, token);
        internalENC->EnumClose(&empty);

        std::vector<uint8_t> image;
        ASSERT_NO_FATAL_FAILURE(SaveImage(emit.p, image));
        minipal::com_ptr<IMDInternalImport> readOnly;
        ASSERT_EQ(S_OK, dispenser->OpenScopeOnMemory(image.data(), (ULONG)image.size(),
            ofReadOnly | ofCopyMemory, IID_IMDInternalImport, (IUnknown**)&readOnly));
        minipal::com_ptr<IMDInternalImportENC> readOnlyENC;
        EXPECT_EQ(E_NOINTERFACE, readOnly->QueryInterface(IID_IMDInternalImportENC, (void**)&readOnlyENC));
        EXPECT_EQ(nullptr, readOnlyENC.p);

        IMDInternalImport* converted = nullptr;
        ASSERT_EQ(S_OK, ConvertDNMDInternalImport(readOnly.p, &converted));
        minipal::com_ptr<IMDInternalImport> writable;
        writable.Attach(converted);
        minipal::com_ptr<IMDInternalImportENC> convertedENC;
        ASSERT_EQ(S_OK, writable->QueryInterface(IID_IMDInternalImportENC, (void**)&convertedENC));
        minipal::com_ptr<IUnknown> convertedIdentity, convertedENCIdentity;
        ASSERT_EQ(S_OK, writable->QueryInterface(IID_IUnknown, (void**)&convertedIdentity));
        ASSERT_EQ(S_OK, convertedENC->QueryInterface(IID_IUnknown, (void**)&convertedENCIdentity));
        EXPECT_EQ(convertedIdentity.p, convertedENCIdentity.p);
    }
}

TEST(UpdateMode, InternalENCEnumerationSnapshotsMoreThanOnePage)
{
    minipal::com_ptr<IMetaDataDispenserEx> dispenser;
    ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenserEx, (void**)&dispenser));
    VARIANT option{};
    V_VT(&option) = VT_UI4;
    V_UI4(&option) = MDUpdateENC;
    ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataSetUpdate, &option));
    minipal::com_ptr<IMetaDataEmit2> emit;
    ASSERT_EQ(S_OK, dispenser->DefineScope(CLSID_CorMetaDataRuntime, 0,
        IID_IMetaDataEmit2, (IUnknown**)&emit));
    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));
    GUID mvid{};
    ASSERT_EQ(S_OK, import->GetScopeProps(nullptr, 0, nullptr, &mvid));
    std::vector<uint8_t> delta;
    ASSERT_TRUE(CreateMinimalTypeRefDelta(mvid, delta));
    ASSERT_TRUE(AppendTypeRefsToDelta(delta, 20));

    IMDInternalImport* updated = nullptr;
    minipal::com_ptr<IMDInternalImport> internal;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMDInternalImport, (void**)&internal));
    ASSERT_EQ(S_OK, internal->ApplyEditAndContinue(delta.data(), (ULONG)delta.size(), &updated));
    EXPECT_EQ(internal.p, updated);
    minipal::com_ptr<IMDInternalImportENC> internalENC;
    ASSERT_EQ(S_OK, internal->QueryInterface(IID_IMDInternalImportENC, (void**)&internalENC));
    HENUMInternal enumeration{};
    ASSERT_EQ(S_OK, internalENC->EnumDeltaTokensInit(&enumeration));
    EXPECT_EQ(20u, internalENC->EnumGetCount(&enumeration));
    mdToken token = mdTokenNil;
    ASSERT_TRUE(internalENC->EnumNext(&enumeration, &token));
    EXPECT_EQ(TokenFromRid(1, mdtTypeRef), token);
    internalENC->EnumReset(&enumeration);
    for (uint32_t rid = 1; rid <= 20; ++rid)
    {
        ASSERT_TRUE(internalENC->EnumNext(&enumeration, &token));
        EXPECT_EQ(TokenFromRid(rid, mdtTypeRef), token);
    }
    EXPECT_FALSE(internalENC->EnumNext(&enumeration, &token));

    ASSERT_EQ(S_OK, emit->ResetENCLog());
    HENUMInternal afterReset{};
    ASSERT_EQ(S_OK, internalENC->EnumDeltaTokensInit(&afterReset));
    EXPECT_EQ(0u, internalENC->EnumGetCount(&afterReset));
    internalENC->EnumClose(&afterReset);
    internalENC->EnumReset(&enumeration);
    ASSERT_TRUE(internalENC->EnumNext(&enumeration, &token));
    EXPECT_EQ(TokenFromRid(1, mdtTypeRef), token);
    internalENC->EnumClose(&enumeration);
}

TEST(UpdateMode, InternalImporterAppliesDeltaInPlace)
{
    minipal::com_ptr<IMetaDataDispenserEx> dispenser;
    ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenserEx, (void**)&dispenser));
    VARIANT option{};
    V_VT(&option) = VT_UI4;
    V_UI4(&option) = MDUpdateENC;
    ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataSetUpdate, &option));
    V_UI4(&option) = MDThreadSafetyOn;
    ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataThreadSafetyOptions, &option));

    minipal::com_ptr<IMDInternalImport> internal;
    ASSERT_EQ(S_OK, dispenser->DefineScope(CLSID_CorMetaDataRuntime, 0,
        IID_IMDInternalImport, (IUnknown**)&internal));
    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, internal->QueryInterface(IID_IMetaDataImport, (void**)&import));
    GUID mvid{};
    ASSERT_EQ(S_OK, import->GetScopeProps(nullptr, 0, nullptr, &mvid));
    std::vector<uint8_t> delta;
    ASSERT_TRUE(CreateMinimalTypeRefDelta(mvid, delta));

    IMDInternalImport* updated = nullptr;
    ASSERT_EQ(S_OK, internal->ApplyEditAndContinue(delta.data(), (ULONG)delta.size(), &updated));
    EXPECT_EQ(internal.p, updated);
    EXPECT_EQ(1u, internal->GetCountWithTokenKind(mdtTypeRef));
    char const* typeName = nullptr;
    char const* typeNamespace = nullptr;
    ASSERT_EQ(S_OK, internal->GetNameOfTypeRef(TokenFromRid(1, mdtTypeRef), &typeNamespace, &typeName));
    EXPECT_STREQ("Example", typeNamespace);
    EXPECT_STREQ("Added", typeName);

    minipal::com_ptr<IMDInternalImportENC> internalENC;
    ASSERT_EQ(S_OK, internal->QueryInterface(IID_IMDInternalImportENC, (void**)&internalENC));
    HENUMInternal deltaTokens{};
    ASSERT_EQ(S_OK, internalENC->EnumDeltaTokensInit(&deltaTokens));
    EXPECT_EQ(1u, internalENC->EnumGetCount(&deltaTokens));
    mdToken token = mdTokenNil;
    EXPECT_TRUE(internalENC->EnumNext(&deltaTokens, &token));
    EXPECT_EQ(TokenFromRid(1, mdtTypeRef), token);
    EXPECT_FALSE(internalENC->EnumNext(&deltaTokens, &token));
    internalENC->EnumClose(&deltaTokens);

    EXPECT_EQ(E_INVALIDARG, internal->ApplyEditAndContinue(nullptr, 0, &updated));
    EXPECT_EQ(nullptr, updated);
}

TEST(UpdateMode, ThreadSafeApplyWaitsForCurrentReaders)
{
    minipal::com_ptr<IMetaDataDispenserEx> dispenser;
    ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenserEx, (void**)&dispenser));
    VARIANT option{};
    V_VT(&option) = VT_UI4;
    V_UI4(&option) = MDUpdateENC;
    ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataSetUpdate, &option));
    V_UI4(&option) = MDThreadSafetyOn;
    ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataThreadSafetyOptions, &option));

    minipal::com_ptr<IMDInternalImport> internal;
    ASSERT_EQ(S_OK, dispenser->DefineScope(CLSID_CorMetaDataRuntime, 0,
        IID_IMDInternalImport, (IUnknown**)&internal));
    minipal::com_ptr<IMetaDataImport2> import;
    ASSERT_EQ(S_OK, internal->QueryInterface(IID_IMetaDataImport2, (void**)&import));
    GUID mvid{};
    ASSERT_EQ(S_OK, import->GetScopeProps(nullptr, 0, nullptr, &mvid));
    std::vector<uint8_t> delta;
    ASSERT_TRUE(CreateMinimalTypeRefDelta(mvid, delta));
    minipal_rwlock* lock = internal->GetReaderWriterLock();
    ASSERT_NE(nullptr, lock);
    ASSERT_TRUE(minipal_rwlock_enter_read(lock));

    std::promise<void> started;
    std::future<void> entering = started.get_future();
    std::future<HRESULT> update = std::async(std::launch::async, [&]
    {
        started.set_value();
        IMDInternalImport* result = nullptr;
        return internal->ApplyEditAndContinue(delta.data(), (ULONG)delta.size(), &result);
    });
    bool reached = entering.wait_for(std::chrono::seconds(5)) == std::future_status::ready;
    bool blocked = reached && update.wait_for(std::chrono::milliseconds(150)) == std::future_status::timeout;
    minipal_rwlock_leave_read(lock);
    ASSERT_TRUE(reached);
    EXPECT_TRUE(blocked);
    EXPECT_EQ(S_OK, update.get());
    EXPECT_EQ(1u, internal->GetCountWithTokenKind(mdtTypeRef));

    minipal::com_ptr<IMDInternalImportENC> internalENC;
    ASSERT_EQ(S_OK, internal->QueryInterface(IID_IMDInternalImportENC, (void**)&internalENC));
    ASSERT_TRUE(minipal_rwlock_enter_write(lock));
    std::promise<void> enumStarted;
    std::future<void> enumEntering = enumStarted.get_future();
    std::future<ULONG> enumerated = std::async(std::launch::async, [&]
    {
        HENUMInternal deltaTokens{};
        enumStarted.set_value();
        if (FAILED(internalENC->EnumDeltaTokensInit(&deltaTokens)))
            return static_cast<ULONG>(UINT32_MAX);
        ULONG count = internalENC->EnumGetCount(&deltaTokens);
        internalENC->EnumClose(&deltaTokens);
        return count;
    });
    reached = enumEntering.wait_for(std::chrono::seconds(5)) == std::future_status::ready;
    blocked = reached && enumerated.wait_for(std::chrono::milliseconds(150)) == std::future_status::timeout;
    minipal_rwlock_leave_write(lock);
    ASSERT_TRUE(reached);
    EXPECT_TRUE(blocked);
    EXPECT_EQ(1u, enumerated.get());
}
