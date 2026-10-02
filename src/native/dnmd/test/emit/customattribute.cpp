// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "emit.hpp"
#include <corerror.h>
#include <dnmd.hpp>
#include <mdinternalemit.h>
#include <cstring>
#include <string>
#include <utility>
#include <vector>

namespace
{
    using Bytes = std::vector<uint8_t>;
    using LogEntry = std::pair<mdToken, uint32_t>;

    constexpr uint8_t EnumConstructor = 0xff;
    Bytes const EmptyAttribute{ 1, 0, 0, 0 };
    Bytes const MethodSignature{ IMAGE_CEE_CS_CALLCONV_DEFAULT, 0, ELEMENT_TYPE_VOID };
    Bytes const FieldSignature{ IMAGE_CEE_CS_CALLCONV_FIELD, ELEMENT_TYPE_I4 };

    void AppendUInt16(Bytes& bytes, uint16_t value)
    {
        bytes.push_back(static_cast<uint8_t>(value));
        bytes.push_back(static_cast<uint8_t>(value >> 8));
    }

    void AppendUInt32(Bytes& bytes, uint32_t value)
    {
        for (unsigned i = 0; i < 4; ++i)
            bytes.push_back(static_cast<uint8_t>(value >> (8 * i)));
    }

    void AppendString(Bytes& bytes, std::string const& text)
    {
        uint32_t size = static_cast<uint32_t>(text.length());
        if (size <= 0x7f)
            bytes.push_back(static_cast<uint8_t>(size));
        else if (size <= 0x3fff)
        {
            bytes.push_back(static_cast<uint8_t>(0x80 | (size >> 8)));
            bytes.push_back(static_cast<uint8_t>(size));
        }
        else
        {
            bytes.push_back(static_cast<uint8_t>(0xc0 | (size >> 24)));
            bytes.push_back(static_cast<uint8_t>(size >> 16));
            bytes.push_back(static_cast<uint8_t>(size >> 8));
            bytes.push_back(static_cast<uint8_t>(size));
        }
        bytes.insert(bytes.end(), text.begin(), text.end());
    }

    Bytes AttributeWithString(std::string const& text)
    {
        Bytes result{ 1, 0 };
        AppendString(result, text);
        AppendUInt16(result, 0);
        return result;
    }

    Bytes AttributeWithUInt16(uint16_t number)
    {
        Bytes result{ 1, 0 };
        AppendUInt16(result, number);
        AppendUInt16(result, 0);
        return result;
    }

    Bytes AttributeWithUInt32(uint32_t number)
    {
        Bytes result{ 1, 0 };
        AppendUInt32(result, number);
        AppendUInt16(result, 0);
        return result;
    }

    void AppendNamedBool(Bytes& bytes, char const* name, bool value)
    {
        bytes.push_back(SERIALIZATION_TYPE_FIELD);
        bytes.push_back(SERIALIZATION_TYPE_BOOLEAN);
        AppendString(bytes, name);
        bytes.push_back(value ? 1 : 0);
    }

    void AppendNamedInt(Bytes& bytes, char const* name, uint32_t value)
    {
        bytes.push_back(SERIALIZATION_TYPE_FIELD);
        bytes.push_back(SERIALIZATION_TYPE_I4);
        AppendString(bytes, name);
        AppendUInt32(bytes, value);
    }

    void AppendNamedInt16(Bytes& bytes, char const* name, uint16_t value)
    {
        bytes.push_back(SERIALIZATION_TYPE_FIELD);
        bytes.push_back(SERIALIZATION_TYPE_I2);
        AppendString(bytes, name);
        AppendUInt16(bytes, value);
    }

    void AppendNamedString(Bytes& bytes, char const* name, std::string const& value,
                           uint8_t type = SERIALIZATION_TYPE_STRING)
    {
        bytes.push_back(SERIALIZATION_TYPE_FIELD);
        bytes.push_back(type);
        AppendString(bytes, name);
        AppendString(bytes, value);
    }

    void AppendNamedEnum(Bytes& bytes, char const* name, char const* enumName, uint32_t value)
    {
        bytes.push_back(SERIALIZATION_TYPE_FIELD);
        bytes.push_back(SERIALIZATION_TYPE_ENUM);
        AppendString(bytes, enumName);
        AppendString(bytes, name);
        AppendUInt32(bytes, value);
    }

    mdMemberRef DefineCtor(IMetaDataEmit* emit, WCHAR const* attributeName,
                           std::initializer_list<uint8_t> argumentTypes = {})
    {
        mdTypeRef type;
        HRESULT hr = emit->DefineTypeRefByName(mdTokenNil, attributeName, &type);
        EXPECT_TRUE(SUCCEEDED(hr));
        if (FAILED(hr))
            return mdMemberRefNil;
        Bytes signature{ IMAGE_CEE_CS_CALLCONV_HASTHIS, static_cast<uint8_t>(argumentTypes.size()), ELEMENT_TYPE_VOID };
        for (uint8_t argumentType : argumentTypes)
        {
            if (argumentType == EnumConstructor)
            {
                signature.push_back(ELEMENT_TYPE_VALUETYPE);
                signature.push_back(0x05);
            }
            else
                signature.push_back(argumentType);
        }
        mdMemberRef constructor;
        hr = emit->DefineMemberRef(type, W(".ctor"), signature.data(),
                                   static_cast<ULONG>(signature.size()), &constructor);
        EXPECT_TRUE(SUCCEEDED(hr));
        return SUCCEEDED(hr) ? constructor : mdMemberRefNil;
    }

    mdTypeDef DefineType(IMetaDataEmit* emit)
    {
        mdTypeDef type;
        HRESULT hr = emit->DefineTypeDef(W("Example.Type"), tdPublic, mdTypeDefNil, nullptr, &type);
        EXPECT_EQ(S_OK, hr);
        return SUCCEEDED(hr) ? type : mdTypeDefNil;
    }

    mdMethodDef DefineMethod(IMetaDataEmit* emit, mdTypeDef type, WCHAR const* name = W("Run"),
                             DWORD implFlags = 0)
    {
        mdMethodDef method;
        HRESULT hr = emit->DefineMethod(type, name, mdPublic, MethodSignature.data(),
                                        static_cast<ULONG>(MethodSignature.size()), 0, implFlags, &method);
        EXPECT_EQ(S_OK, hr);
        return SUCCEEDED(hr) ? method : mdMethodDefNil;
    }

    mdFieldDef DefineField(IMetaDataEmit* emit, mdTypeDef type)
    {
        mdFieldDef field;
        HRESULT hr = emit->DefineField(type, W("Value"), fdPublic, FieldSignature.data(),
                                        static_cast<ULONG>(FieldSignature.size()), ELEMENT_TYPE_VOID,
                                        nullptr, 0, &field);
        EXPECT_EQ(S_OK, hr);
        return SUCCEEDED(hr) ? field : mdFieldDefNil;
    }

    mdParamDef DefineParam(IMetaDataEmit* emit, mdMethodDef method, ULONG sequence)
    {
        mdParamDef param;
        HRESULT hr = emit->DefineParam(method, sequence, W("arg"), 0, ELEMENT_TYPE_VOID,
                                       nullptr, 0, &param);
        EXPECT_EQ(S_OK, hr);
        return SUCCEEDED(hr) ? param : mdParamDefNil;
    }

    HRESULT Apply(IMetaDataEmit* emit, mdToken owner, mdToken ctor, Bytes const& blob,
                  mdCustomAttribute* token = nullptr)
    {
        return emit->DefineCustomAttribute(owner, ctor, blob.data(),
                                           static_cast<ULONG>(blob.size()), token);
    }

    Bytes Save(IMetaDataEmit* emit)
    {
        DWORD size = 0;
        EXPECT_EQ(S_OK, emit->GetSaveSize(cssAccurate, &size));
        Bytes image(size);
        EXPECT_EQ(S_OK, emit->SaveToMemory(image.data(), size));
        return image;
    }

    uint32_t ReadColumn(IMetaDataEmit* emit, mdToken token, col_index_t column)
    {
        Bytes image = Save(emit);
        mdhandle_t raw = nullptr;
        EXPECT_TRUE(md_create_handle(image.data(), image.size(), &raw));
        if (raw == nullptr)
            return 0;
        mdhandle_ptr handle{ raw };
        mdcursor_t row;
        EXPECT_TRUE(md_token_to_cursor(handle.get(), token, &row));
        uint32_t value = 0;
        EXPECT_TRUE(md_get_column_value_as_constant(row, column, &value));
        return value;
    }

    uint32_t CountRows(IMetaDataEmit* emit, mdtable_id_t table)
    {
        Bytes image = Save(emit);
        mdhandle_t raw = nullptr;
        EXPECT_TRUE(md_create_handle(image.data(), image.size(), &raw));
        if (raw == nullptr)
            return 0;
        mdhandle_ptr handle{ raw };
        mdcursor_t row;
        uint32_t count = 0;
        (void)md_create_cursor(handle.get(), table, &row, &count);
        return count;
    }

    uint32_t ReadLayout(IMetaDataEmit* emit, mdtable_id_t table, col_index_t column)
    {
        Bytes image = Save(emit);
        mdhandle_t raw = nullptr;
        EXPECT_TRUE(md_create_handle(image.data(), image.size(), &raw));
        if (raw == nullptr)
            return 0;
        mdhandle_ptr handle{ raw };
        mdcursor_t row;
        uint32_t count = 0;
        EXPECT_TRUE(md_create_cursor(handle.get(), table, &row, &count));
        uint32_t value = 0;
        EXPECT_TRUE(md_get_column_value_as_constant(row, column, &value));
        return value;
    }

    std::vector<LogEntry> ReadLog(IMetaDataEmit* emit)
    {
        Bytes image = Save(emit);
        mdhandle_t raw = nullptr;
        EXPECT_TRUE(md_create_handle(image.data(), image.size(), &raw));
        if (raw == nullptr)
            return {};
        mdhandle_ptr handle{ raw };
        mdcursor_t row;
        uint32_t count;
        std::vector<LogEntry> result;
        if (!md_create_cursor(handle.get(), mdtid_ENCLog, &row, &count))
            return result;
        for (uint32_t i = 0; i < count; ++i)
        {
            uint32_t token, operation;
            EXPECT_TRUE(md_get_column_value_as_constant(row, mdtENCLog_Token, &token));
            EXPECT_TRUE(md_get_column_value_as_constant(row, mdtENCLog_Op, &operation));
            result.emplace_back(token, operation);
            if (i + 1 < count)
                EXPECT_TRUE(md_cursor_next(&row));
        }
        return result;
    }

    mdToken RowToken(mdtable_id_t table, uint32_t rid)
    {
        return 0x80000000u | (static_cast<uint32_t>(table) << 24) | rid;
    }

    void AssertMarshal(IMetaDataImport* import, mdToken owner, Bytes const& expected)
    {
        PCCOR_SIGNATURE native = nullptr;
        ULONG length = 0;
        ASSERT_EQ(S_OK, import->GetFieldMarshal(owner, &native, &length));
        ASSERT_EQ(expected.size(), length);
        EXPECT_EQ(0, std::memcmp(expected.data(), native, length));
    }
}

TEST(CustomAttribute, MetadataFlagsAndDroppedRows)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdTypeDef type = DefineType(emit.p);
    mdMethodDef method = DefineMethod(emit.p, type);
    mdFieldDef field = DefineField(emit.p, type);
    mdParamDef param = DefineParam(emit.p, method, 1);

    struct FlagCase
    {
        WCHAR const* name;
        mdToken owner;
        col_index_t column;
        uint32_t bit;
    };
    FlagCase cases[] =
    {
        { W("System.Runtime.InteropServices.ComImportAttribute"), type, mdtTypeDef_Flags, tdImport },
        { W("System.SerializableAttribute"), type, mdtTypeDef_Flags, tdSerializable },
        { W("System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeImportAttribute"), type, mdtTypeDef_Flags, tdWindowsRuntime },
        { W("System.NonSerializedAttribute"), field, mdtField_Flags, fdNotSerialized },
        { W("System.Runtime.InteropServices.InAttribute"), param, mdtParam_Flags, pdIn },
        { W("System.Runtime.InteropServices.OutAttribute"), param, mdtParam_Flags, pdOut },
        { W("System.Runtime.InteropServices.OptionalAttribute"), param, mdtParam_Flags, pdOptional },
        { W("System.Runtime.InteropServices.PreserveSigAttribute"), method, mdtMethodDef_ImplFlags, miPreserveSig },
    };
    for (FlagCase const& item : cases)
    {
        mdMemberRef ctor = DefineCtor(emit.p, item.name);
        mdCustomAttribute customAttribute = 0xffffffff;
        ASSERT_EQ(S_OK, Apply(emit.p, item.owner, ctor, EmptyAttribute, &customAttribute));
        EXPECT_EQ(mdCustomAttributeNil, customAttribute);
        EXPECT_NE(0u, ReadColumn(emit.p, item.owner, item.column) & item.bit);
    }
    EXPECT_EQ(0u, CountRows(emit.p, mdtid_CustomAttribute));
}

TEST(CustomAttribute, ArgumentlessPseudoAttributesAcceptLegacyEmptyBlob)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdTypeDef type = DefineType(emit.p);
    mdMemberRef serializable = DefineCtor(emit.p, W("System.SerializableAttribute"));
    EXPECT_EQ(S_OK, Apply(emit.p, type, serializable, Bytes{}));
    EXPECT_EQ(S_OK, Apply(emit.p, type, serializable, Bytes{ 0, 0 }));
    EXPECT_NE(0u, ReadColumn(emit.p, type, mdtTypeDef_Flags) & tdSerializable);
    EXPECT_EQ(0u, CountRows(emit.p, mdtid_CustomAttribute));
}

TEST(CustomAttribute, RecognizesConstructorDefinedOnTypeDef)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdTypeDef attributeType;
    ASSERT_EQ(S_OK, emit->DefineTypeDef(W("System.SerializableAttribute"), tdPublic,
                                        mdTypeDefNil, nullptr, &attributeType));
    mdMethodDef constructor = DefineMethod(emit.p, attributeType, W(".ctor"));
    mdTypeDef owner = DefineType(emit.p);
    EXPECT_EQ(S_OK, Apply(emit.p, owner, constructor, EmptyAttribute));
    EXPECT_NE(0u, ReadColumn(emit.p, owner, mdtTypeDef_Flags) & tdSerializable);
    EXPECT_EQ(0u, CountRows(emit.p, mdtid_CustomAttribute));
}

TEST(CustomAttribute, RecognitionCacheIsInvalidatedWhenAttributeTypeIsDeleted)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdTypeDef attributeType;
    ASSERT_EQ(S_OK, emit->DefineTypeDef(W("System.SerializableAttribute"), tdPublic,
                                        mdTypeDefNil, nullptr, &attributeType));
    mdMethodDef constructor = DefineMethod(emit.p, attributeType, W(".ctor"));
    mdTypeDef owner = DefineType(emit.p);
    ASSERT_EQ(S_OK, Apply(emit.p, owner, constructor, EmptyAttribute));
    ASSERT_EQ(S_OK, emit->DeleteToken(attributeType));
    mdCustomAttribute token = mdCustomAttributeNil;
    ASSERT_EQ(S_OK, Apply(emit.p, owner, constructor, EmptyAttribute, &token));
    EXPECT_EQ(mdtCustomAttribute, TypeFromToken(token));
    EXPECT_EQ(1u, CountRows(emit.p, mdtid_CustomAttribute));
}

TEST(CustomAttribute, RecognitionCacheFollowsMemberRefParentChanges)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdTypeDef owner = DefineType(emit.p);
    mdMemberRef ctor = DefineCtor(emit.p, W("System.SerializableAttribute"));
    mdTypeRef known, other;
    ASSERT_TRUE(SUCCEEDED(emit->DefineTypeRefByName(mdTokenNil, W("System.SerializableAttribute"), &known)));
    ASSERT_EQ(S_OK, emit->DefineTypeRefByName(mdTokenNil, W("Example.OtherAttribute"), &other));
    ASSERT_EQ(S_OK, Apply(emit.p, owner, ctor, EmptyAttribute));
    ASSERT_EQ(S_OK, emit->SetParent(ctor, other));
    mdCustomAttribute retained = mdCustomAttributeNil;
    ASSERT_EQ(S_OK, Apply(emit.p, owner, ctor, EmptyAttribute, &retained));
    EXPECT_EQ(mdtCustomAttribute, TypeFromToken(retained));
    ASSERT_EQ(S_OK, emit->SetParent(ctor, known));
    ASSERT_EQ(S_OK, Apply(emit.p, owner, ctor, EmptyAttribute));
    EXPECT_EQ(1u, CountRows(emit.p, mdtid_CustomAttribute));
}

TEST(CustomAttribute, KeepAttributesAndUnknownAttributes)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdTypeDef type = DefineType(emit.p);

    mdMemberRef guid = DefineCtor(emit.p, W("System.Runtime.InteropServices.GuidAttribute"),
                                  { SERIALIZATION_TYPE_STRING });
    mdCustomAttribute token = mdCustomAttributeNil;
    Bytes guidBlob = AttributeWithString("01234567-89ab-cdef-0123-456789abcdef");
    ASSERT_EQ(S_OK, Apply(emit.p, type, guid, guidBlob, &token));
    EXPECT_EQ(mdtCustomAttribute, TypeFromToken(token));
    EXPECT_EQ(1u, RidFromToken(token));

    mdMemberRef iface = DefineCtor(emit.p, W("System.Runtime.InteropServices.InterfaceTypeAttribute"),
                                   { SERIALIZATION_TYPE_U2 });
    mdMemberRef clsIface = DefineCtor(emit.p, W("System.Runtime.InteropServices.ClassInterfaceAttribute"),
                                      { SERIALIZATION_TYPE_U2 });
    EXPECT_EQ(S_OK, Apply(emit.p, type, iface, AttributeWithUInt16(3)));
    EXPECT_EQ(S_OK, Apply(emit.p, type, clsIface, AttributeWithUInt16(2)));

    mdMemberRef unknown = DefineCtor(emit.p, W("Example.UnknownAttribute"));
    EXPECT_EQ(S_OK, Apply(emit.p, type, unknown, Bytes{ 0xff }));
    EXPECT_EQ(4u, CountRows(emit.p, mdtid_CustomAttribute));

    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));
    mdToken owner, ctor;
    void const* data;
    ULONG size;
    ASSERT_EQ(S_OK, import->GetCustomAttributeProps(token, &owner, &ctor, &data, &size));
    EXPECT_EQ(type, owner);
    EXPECT_EQ(guid, ctor);
    ASSERT_EQ(guidBlob.size(), size);
    EXPECT_EQ(0, std::memcmp(guidBlob.data(), data, size));
}

TEST(CustomAttribute, InvalidBlobsTargetsAndValuesLeaveMetadataUnchanged)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdTypeDef type = DefineType(emit.p);
    mdFieldDef field = DefineField(emit.p, type);
    mdMemberRef serializable = DefineCtor(emit.p, W("System.SerializableAttribute"));
    mdMemberRef guid = DefineCtor(emit.p, W("System.Runtime.InteropServices.GuidAttribute"),
                                  { SERIALIZATION_TYPE_STRING });
    mdMemberRef iface = DefineCtor(emit.p, W("System.Runtime.InteropServices.InterfaceTypeAttribute"),
                                   { SERIALIZATION_TYPE_U2 });
    mdMemberRef clsIface = DefineCtor(emit.p, W("System.Runtime.InteropServices.ClassInterfaceAttribute"),
                                      { SERIALIZATION_TYPE_U2 });
    mdMemberRef fieldOffset = DefineCtor(emit.p, W("System.Runtime.InteropServices.FieldOffsetAttribute"),
                                         { SERIALIZATION_TYPE_U4 });
    EXPECT_EQ(META_E_CA_INVALID_TARGET, Apply(emit.p, field, serializable, Bytes{}));
    EXPECT_EQ(META_E_CA_INVALID_TARGET, Apply(emit.p, type, fieldOffset, AttributeWithUInt32(1)));
    EXPECT_EQ(META_E_CA_INVALID_BLOB, Apply(emit.p, type, guid, Bytes{ 0, 0 }));
    EXPECT_EQ(META_E_CA_INVALID_BLOB, Apply(emit.p, type, iface, Bytes{ 1, 0, 1 }));
    EXPECT_EQ(META_E_CA_INVALID_BLOB, Apply(emit.p, type, guid, Bytes{ 1, 0, 0xc0, 0xff, 0xff, 0xff }));
    EXPECT_EQ(META_E_CA_INVALID_UUID, Apply(emit.p, type, guid, AttributeWithString("not-a-guid")));
    EXPECT_EQ(META_E_CA_INVALID_UUID, Apply(emit.p, type, guid,
                                           AttributeWithString("01234567-89ab-cdef-0123-456789abcdeg")));
    EXPECT_EQ(META_E_CA_INVALID_VALUE, Apply(emit.p, type, iface, AttributeWithUInt16(4)));
    EXPECT_EQ(META_E_CA_INVALID_VALUE, Apply(emit.p, type, clsIface, AttributeWithUInt16(3)));
    EXPECT_EQ(META_E_CA_INVALID_VALUE, Apply(emit.p, field, fieldOffset, AttributeWithUInt32(0xffffffff)));
    EXPECT_EQ(0u, CountRows(emit.p, mdtid_CustomAttribute));
    EXPECT_EQ(tdPublic, ReadColumn(emit.p, type, mdtTypeDef_Flags));
    EXPECT_EQ(0u, CountRows(emit.p, mdtid_FieldLayout));
}

TEST(CustomAttribute, DllImportDefinesImplMapAndPreserveSig)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdTypeDef type = DefineType(emit.p);
    mdMethodDef method = DefineMethod(emit.p, type, W("NativeCall"), miPreserveSig);
    mdMemberRef ctor = DefineCtor(emit.p, W("System.Runtime.InteropServices.DllImportAttribute"),
                                  { SERIALIZATION_TYPE_STRING });
    Bytes attribute{ 1, 0 };
    AppendString(attribute, "example.dll");
    AppendUInt16(attribute, 8);
    AppendNamedEnum(attribute, "CallingConvention", "System.Runtime.InteropServices.CallingConvention", 2);
    AppendNamedEnum(attribute, "CharSet", "System.Runtime.InteropServices.CharSet", 3);
    AppendNamedString(attribute, "EntryPoint", "NativeEntry");
    AppendNamedBool(attribute, "ExactSpelling", true);
    AppendNamedBool(attribute, "SetLastError", true);
    AppendNamedBool(attribute, "PreserveSig", false);
    AppendNamedBool(attribute, "BestFitMapping", false);
    // The named-argument count is deliberately wrong: parsing must reject the blob.
    EXPECT_EQ(META_E_CA_INVALID_BLOB, Apply(emit.p, method, ctor, attribute));
    EXPECT_EQ(0u, CountRows(emit.p, mdtid_ImplMap));
    attribute = AttributeWithString("example.dll");
    attribute.resize(attribute.size() - 2);
    AppendUInt16(attribute, 8);
    AppendNamedEnum(attribute, "CallingConvention", "System.Runtime.InteropServices.CallingConvention", 2);
    AppendNamedEnum(attribute, "CharSet", "System.Runtime.InteropServices.CharSet, System.Runtime.InteropServices", 3);
    AppendNamedString(attribute, "EntryPoint", "NativeEntry");
    AppendNamedBool(attribute, "ExactSpelling", true);
    AppendNamedBool(attribute, "SetLastError", true);
    AppendNamedBool(attribute, "PreserveSig", false);
    AppendNamedBool(attribute, "BestFitMapping", false);
    AppendNamedBool(attribute, "ThrowOnUnmappableChar", true);

    mdCustomAttribute token = 0xffffffff;
    ASSERT_EQ(S_OK, Apply(emit.p, method, ctor, attribute, &token));
    EXPECT_EQ(mdCustomAttributeNil, token);
    EXPECT_EQ(0u, CountRows(emit.p, mdtid_CustomAttribute));
    EXPECT_EQ(1u, CountRows(emit.p, mdtid_ModuleRef));
    EXPECT_EQ(1u, CountRows(emit.p, mdtid_ImplMap));
    EXPECT_NE(0u, ReadColumn(emit.p, method, mdtMethodDef_Flags) & mdPinvokeImpl);
    EXPECT_EQ(0u, ReadColumn(emit.p, method, mdtMethodDef_ImplFlags) & miPreserveSig);

    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));
    DWORD flags;
    WCHAR entry[32]{};
    ULONG length;
    mdModuleRef module;
    ASSERT_EQ(S_OK, import->GetPinvokeMap(method, &flags, entry, 32, &length, &module));
    EXPECT_EQ(WSTR_string(W("NativeEntry")), WSTR_string(entry));
    EXPECT_EQ(pmCallConvCdecl | pmCharSetUnicode | pmNoMangle | pmSupportsLastError |
              pmBestFitDisabled | pmThrowOnUnmappableCharEnabled, flags);
    WCHAR moduleName[32]{};
    ASSERT_EQ(S_OK, import->GetModuleRefProps(module, moduleName, 32, &length));
    EXPECT_EQ(WSTR_string(W("example.dll")), WSTR_string(moduleName));

    mdMethodDef second = DefineMethod(emit.p, type, W("Second"));
    ASSERT_EQ(S_OK, Apply(emit.p, second, ctor, AttributeWithString("example.dll")));
    ASSERT_EQ(S_OK, import->GetPinvokeMap(second, &flags, entry, 32, &length, &module));
    EXPECT_EQ(WSTR_string(W("Second")), WSTR_string(entry));
    EXPECT_EQ(pmCallConvWinapi, flags);
    EXPECT_NE(0u, ReadColumn(emit.p, second, mdtMethodDef_ImplFlags) & miPreserveSig);
    EXPECT_EQ(1u, CountRows(emit.p, mdtid_ModuleRef));

    mdMethodDef third = DefineMethod(emit.p, type, W("LongEntryPoint"));
    Bytes longEntry = AttributeWithString("example.dll");
    longEntry.resize(longEntry.size() - 2);
    AppendUInt16(longEntry, 1);
    AppendNamedString(longEntry, "EntryPoint", std::string(130, 'x'));
    ASSERT_EQ(S_OK, Apply(emit.p, third, ctor, longEntry));
    WCHAR longName[131]{};
    ASSERT_EQ(S_OK, import->GetPinvokeMap(third, &flags, longName, 131, &length, &module));
    EXPECT_EQ(WSTR_string(130, static_cast<WCHAR>('x')), WSTR_string(longName));
    EXPECT_EQ(1u, CountRows(emit.p, mdtid_ModuleRef));
}

TEST(CustomAttribute, DllImportRepairsExistingImplMapMethodFlag)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdTypeDef type = DefineType(emit.p);
    mdMethodDef method = DefineMethod(emit.p, type);
    mdModuleRef module;
    ASSERT_EQ(S_OK, emit->DefineModuleRef(W("native.dll"), &module));
    ASSERT_EQ(S_OK, emit->DefinePinvokeMap(method, pmCallConvWinapi, W("Run"), module));
    ASSERT_EQ(S_OK, emit->SetMethodProps(method, mdPublic, UINT32_MAX, UINT32_MAX));
    EXPECT_EQ(0u, ReadColumn(emit.p, method, mdtMethodDef_Flags) & mdPinvokeImpl);

    mdMemberRef ctor = DefineCtor(emit.p, W("System.Runtime.InteropServices.DllImportAttribute"),
                                  { SERIALIZATION_TYPE_STRING });
    ASSERT_EQ(S_OK, Apply(emit.p, method, ctor, AttributeWithString("native.dll")));
    EXPECT_NE(0u, ReadColumn(emit.p, method, mdtMethodDef_Flags) & mdPinvokeImpl);
    EXPECT_EQ(1u, CountRows(emit.p, mdtid_ImplMap));
    EXPECT_EQ(1u, CountRows(emit.p, mdtid_ModuleRef));
}

TEST(CustomAttribute, DllImportRejectsMalformedBlobsWithoutSideEffects)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdTypeDef type = DefineType(emit.p);
    mdMethodDef method = DefineMethod(emit.p, type);
    mdMemberRef ctor = DefineCtor(emit.p, W("System.Runtime.InteropServices.DllImportAttribute"),
                                  { SERIALIZATION_TYPE_STRING });
    EXPECT_EQ(META_E_CA_INVALID_TARGET, Apply(emit.p, type, ctor, Bytes{}));
    EXPECT_EQ(META_E_CA_INVALID_VALUE, Apply(emit.p, method, ctor, AttributeWithString("")));
    EXPECT_EQ(META_E_CA_INVALID_BLOB, Apply(emit.p, method, ctor, Bytes{ 1, 0, 0xc0, 0xff, 0xff, 0xff }));

    Bytes invalidEnum = AttributeWithString("native.dll");
    invalidEnum.resize(invalidEnum.size() - 2);
    AppendUInt16(invalidEnum, 1);
    AppendNamedEnum(invalidEnum, "CallingConvention", "System.Runtime.InteropServices.CallingConvention", 42);
    EXPECT_EQ(META_E_CA_INVALID_VALUE, Apply(emit.p, method, ctor, invalidEnum));

    Bytes unknown = AttributeWithString("native.dll");
    unknown.resize(unknown.size() - 2);
    AppendUInt16(unknown, 1);
    AppendNamedBool(unknown, "UnknownField", true);
    EXPECT_EQ(META_E_CA_UNKNOWN_ARGUMENT, Apply(emit.p, method, ctor, unknown));

    Bytes repeated = AttributeWithString("native.dll");
    repeated.resize(repeated.size() - 2);
    AppendUInt16(repeated, 2);
    AppendNamedBool(repeated, "SetLastError", true);
    AppendNamedBool(repeated, "SetLastError", false);
    EXPECT_EQ(META_E_CA_REPEATED_ARG, Apply(emit.p, method, ctor, repeated));

    Bytes badTag = AttributeWithString("native.dll");
    badTag.resize(badTag.size() - 2);
    AppendUInt16(badTag, 1);
    badTag.push_back(0xff);
    EXPECT_EQ(META_E_CA_INVALID_ARGTYPE, Apply(emit.p, method, ctor, badTag));
    EXPECT_EQ(0u, CountRows(emit.p, mdtid_ImplMap));
    EXPECT_EQ(0u, CountRows(emit.p, mdtid_ModuleRef));
    EXPECT_EQ(0u, CountRows(emit.p, mdtid_CustomAttribute));
    EXPECT_EQ(mdPublic, ReadColumn(emit.p, method, mdtMethodDef_Flags));
}

TEST(CustomAttribute, StructLayoutAndFieldOffsetCreateLayoutRows)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdTypeDef type = DefineType(emit.p);
    mdFieldDef field = DefineField(emit.p, type);
    mdMemberRef layout = DefineCtor(emit.p, W("System.Runtime.InteropServices.StructLayoutAttribute"),
                                    { SERIALIZATION_TYPE_I2 });
    mdMemberRef offset = DefineCtor(emit.p, W("System.Runtime.InteropServices.FieldOffsetAttribute"),
                                    { SERIALIZATION_TYPE_I4 });

    Bytes extended{ 1, 0 };
    AppendUInt16(extended, 1);
    AppendUInt16(extended, 3);
    AppendNamedInt(extended, "Pack", 8);
    AppendNamedInt(extended, "Size", 32);
    AppendNamedEnum(extended, "CharSet", "System.Runtime.InteropServices.CharSet", 3);
    ASSERT_EQ(S_OK, Apply(emit.p, type, layout, extended));
    EXPECT_EQ(tdExtendedLayout, ReadColumn(emit.p, type, mdtTypeDef_Flags) & tdLayoutMask);
    EXPECT_EQ(tdUnicodeClass, ReadColumn(emit.p, type, mdtTypeDef_Flags) & tdStringFormatMask);
    EXPECT_EQ(8u, ReadLayout(emit.p, mdtid_ClassLayout, mdtClassLayout_PackingSize));
    EXPECT_EQ(32u, ReadLayout(emit.p, mdtid_ClassLayout, mdtClassLayout_ClassSize));
    ASSERT_EQ(S_OK, Apply(emit.p, field, offset, AttributeWithUInt32(12)));
    EXPECT_EQ(12u, ReadLayout(emit.p, mdtid_FieldLayout, mdtFieldLayout_Offset));

    mdMemberRef layoutEnum = DefineCtor(emit.p, W("System.Runtime.InteropServices.StructLayoutAttribute"),
                                        { EnumConstructor });
    Bytes update = AttributeWithUInt32(2);
    update.resize(update.size() - 2);
    AppendUInt16(update, 1);
    AppendNamedInt(update, "Size", 48);
    ASSERT_EQ(S_OK, Apply(emit.p, type, layoutEnum, update));
    EXPECT_EQ(tdExplicitLayout, ReadColumn(emit.p, type, mdtTypeDef_Flags) & tdLayoutMask);
    EXPECT_EQ(tdUnicodeClass, ReadColumn(emit.p, type, mdtTypeDef_Flags) & tdStringFormatMask);
    EXPECT_EQ(8u, ReadLayout(emit.p, mdtid_ClassLayout, mdtClassLayout_PackingSize));
    EXPECT_EQ(48u, ReadLayout(emit.p, mdtid_ClassLayout, mdtClassLayout_ClassSize));
    EXPECT_EQ(1u, CountRows(emit.p, mdtid_ClassLayout));
    EXPECT_EQ(1u, CountRows(emit.p, mdtid_FieldLayout));
    EXPECT_EQ(0u, CountRows(emit.p, mdtid_CustomAttribute));
}

TEST(CustomAttribute, StructLayoutRejectsInvalidValuesBeforeChanges)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdTypeDef type = DefineType(emit.p);
    mdMemberRef layout = DefineCtor(emit.p, W("System.Runtime.InteropServices.StructLayoutAttribute"),
                                    { SERIALIZATION_TYPE_I4 });
    EXPECT_EQ(META_E_CA_INVALID_VALUE, Apply(emit.p, type, layout, AttributeWithUInt32(4)));

    Bytes invalidPack = AttributeWithUInt32(2);
    invalidPack.resize(invalidPack.size() - 2);
    AppendUInt16(invalidPack, 1);
    AppendNamedInt(invalidPack, "Pack", 3);
    EXPECT_EQ(META_E_CA_INVALID_VALUE, Apply(emit.p, type, layout, invalidPack));

    Bytes invalidSize = AttributeWithUInt32(2);
    invalidSize.resize(invalidSize.size() - 2);
    AppendUInt16(invalidSize, 1);
    AppendNamedInt(invalidSize, "Size", 0xffffffff);
    EXPECT_EQ(META_E_CA_INVALID_VALUE, Apply(emit.p, type, layout, invalidSize));

    Bytes invalidCharSet = AttributeWithUInt32(2);
    invalidCharSet.resize(invalidCharSet.size() - 2);
    AppendUInt16(invalidCharSet, 1);
    AppendNamedEnum(invalidCharSet, "CharSet", "System.Runtime.InteropServices.CharSet", 1);
    EXPECT_EQ(META_E_CA_INVALID_VALUE, Apply(emit.p, type, layout, invalidCharSet));
    EXPECT_EQ(tdPublic, ReadColumn(emit.p, type, mdtTypeDef_Flags));
    EXPECT_EQ(0u, CountRows(emit.p, mdtid_ClassLayout));
}

TEST(CustomAttribute, MethodImplOverloadsAndNamedCodeType)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdTypeDef type = DefineType(emit.p);
    mdMethodDef method = DefineMethod(emit.p, type, W("Run"), miForwardRef);
    mdMemberRef shortCtor = DefineCtor(emit.p, W("System.Runtime.CompilerServices.MethodImplAttribute"),
                                       { SERIALIZATION_TYPE_I2 });
    ASSERT_EQ(S_OK, Apply(emit.p, method, shortCtor, AttributeWithUInt16(miNoInlining)));
    EXPECT_EQ(miForwardRef | miNoInlining, ReadColumn(emit.p, method, mdtMethodDef_ImplFlags));

    mdMemberRef enumCtor = DefineCtor(emit.p, W("System.Runtime.CompilerServices.MethodImplAttribute"),
                                      { EnumConstructor });
    Bytes aggressive = AttributeWithUInt32(miAggressiveInlining);
    aggressive.resize(aggressive.size() - 2);
    AppendUInt16(aggressive, 1);
    AppendNamedEnum(aggressive, "MethodCodeType", "System.Runtime.CompilerServices.MethodCodeType", miNative);
    ASSERT_EQ(S_OK, Apply(emit.p, method, enumCtor, aggressive));
    EXPECT_EQ(miForwardRef | miNoInlining | miAggressiveInlining | miNative,
              ReadColumn(emit.p, method, mdtMethodDef_ImplFlags));

    mdMemberRef noArgCtor = DefineCtor(emit.p, W("System.Runtime.CompilerServices.MethodImplAttribute"));
    Bytes resetCodeType{ 1, 0, 1, 0 };
    AppendNamedEnum(resetCodeType, "MethodCodeType", "System.Runtime.CompilerServices.MethodCodeType", miIL);
    ASSERT_EQ(S_OK, Apply(emit.p, method, noArgCtor, resetCodeType));
    EXPECT_EQ(miForwardRef | miNoInlining | miAggressiveInlining,
              ReadColumn(emit.p, method, mdtMethodDef_ImplFlags));
    EXPECT_EQ(META_E_CA_INVALID_VALUE, Apply(emit.p, method, enumCtor, AttributeWithUInt32(miNative)));
    EXPECT_EQ(META_E_CA_INVALID_VALUE, Apply(emit.p, method, enumCtor, AttributeWithUInt32(0xffffffff)));
    EXPECT_EQ(0u, CountRows(emit.p, mdtid_CustomAttribute));
}

TEST(CustomAttribute, MarshalAsEncodesFieldAndParameterNativeTypes)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdTypeDef type = DefineType(emit.p);
    mdMethodDef method = DefineMethod(emit.p, type);
    mdFieldDef field = DefineField(emit.p, type);
    mdParamDef param = DefineParam(emit.p, method, 1);
    mdMemberRef shortCtor = DefineCtor(emit.p, W("System.Runtime.InteropServices.MarshalAsAttribute"),
                                       { SERIALIZATION_TYPE_I2 });
    mdMemberRef enumCtor = DefineCtor(emit.p, W("System.Runtime.InteropServices.MarshalAsAttribute"),
                                      { EnumConstructor });
    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));

    ASSERT_EQ(S_OK, Apply(emit.p, field, shortCtor, AttributeWithUInt16(NATIVE_TYPE_I4)));
    AssertMarshal(import.p, field, Bytes{ NATIVE_TYPE_I4 });
    EXPECT_NE(0u, ReadColumn(emit.p, field, mdtField_Flags) & fdHasFieldMarshal);
    ASSERT_EQ(S_OK, Apply(emit.p, param, enumCtor, AttributeWithUInt32(NATIVE_TYPE_LPWSTR)));
    AssertMarshal(import.p, param, Bytes{ NATIVE_TYPE_LPWSTR });
    EXPECT_NE(0u, ReadColumn(emit.p, param, mdtParam_Flags) & pdHasFieldMarshal);

    Bytes fixedArray = AttributeWithUInt32(NATIVE_TYPE_FIXEDARRAY);
    fixedArray.resize(fixedArray.size() - 2);
    AppendUInt16(fixedArray, 2);
    AppendNamedInt(fixedArray, "SizeConst", 3);
    AppendNamedEnum(fixedArray, "ArraySubType", "System.Runtime.InteropServices.UnmanagedType", NATIVE_TYPE_I2);
    ASSERT_EQ(S_OK, Apply(emit.p, field, enumCtor, fixedArray));
    AssertMarshal(import.p, field, Bytes{ NATIVE_TYPE_FIXEDARRAY, 3, NATIVE_TYPE_I2 });

    Bytes fixedString = AttributeWithUInt32(NATIVE_TYPE_FIXEDSYSSTRING);
    fixedString.resize(fixedString.size() - 2);
    AppendUInt16(fixedString, 1);
    AppendNamedInt(fixedString, "SizeConst", 8);
    ASSERT_EQ(S_OK, Apply(emit.p, field, enumCtor, fixedString));
    AssertMarshal(import.p, field, Bytes{ NATIVE_TYPE_FIXEDSYSSTRING, 8 });

    Bytes array = AttributeWithUInt32(NATIVE_TYPE_ARRAY);
    array.resize(array.size() - 2);
    AppendUInt16(array, 2);
    AppendNamedInt16(array, "SizeParamIndex", 2);
    AppendNamedInt(array, "SizeConst", 5);
    ASSERT_EQ(S_OK, Apply(emit.p, param, enumCtor, array));
    AssertMarshal(import.p, param, Bytes{ NATIVE_TYPE_ARRAY, NATIVE_TYPE_MAX, 2, 5, ntaSizeParamIndexSpecified });

    Bytes iid = AttributeWithUInt32(NATIVE_TYPE_IUNKNOWN);
    iid.resize(iid.size() - 2);
    AppendUInt16(iid, 1);
    AppendNamedInt(iid, "IidParameterIndex", 4);
    ASSERT_EQ(S_OK, Apply(emit.p, param, enumCtor, iid));
    AssertMarshal(import.p, param, Bytes{ NATIVE_TYPE_IUNKNOWN, 4 });
    ASSERT_EQ(S_OK, Apply(emit.p, param, enumCtor, AttributeWithUInt32(NATIVE_TYPE_BYVALSTR)));
    AssertMarshal(import.p, param, Bytes{ NATIVE_TYPE_BYVALSTR });
    EXPECT_EQ(2u, CountRows(emit.p, mdtid_FieldMarshal));
    EXPECT_EQ(0u, CountRows(emit.p, mdtid_CustomAttribute));
}

TEST(CustomAttribute, MarshalAsSafeArrayAndCustomMarshaler)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdTypeDef type = DefineType(emit.p);
    mdFieldDef field = DefineField(emit.p, type);
    mdMemberRef ctor = DefineCtor(emit.p, W("System.Runtime.InteropServices.MarshalAsAttribute"),
                                  { EnumConstructor });
    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));
    Bytes safeArray = AttributeWithUInt32(NATIVE_TYPE_SAFEARRAY);
    safeArray.resize(safeArray.size() - 2);
    AppendUInt16(safeArray, 2);
    AppendNamedEnum(safeArray, "SafeArraySubType", "System.Runtime.InteropServices.VarEnum", 36);
    AppendNamedString(safeArray, "SafeArrayUserDefinedSubType", "Demo", SERIALIZATION_TYPE_TYPE);
    ASSERT_EQ(S_OK, Apply(emit.p, field, ctor, safeArray));
    AssertMarshal(import.p, field, Bytes{ NATIVE_TYPE_SAFEARRAY, 36, 4, 'D', 'e', 'm', 'o' });

    Bytes custom = AttributeWithUInt32(NATIVE_TYPE_CUSTOMMARSHALER);
    custom.resize(custom.size() - 2);
    AppendUInt16(custom, 2);
    AppendNamedString(custom, "MarshalType", "Kind");
    AppendNamedString(custom, "MarshalCookie", "id");
    ASSERT_EQ(S_OK, Apply(emit.p, field, ctor, custom));
    AssertMarshal(import.p, field, Bytes{ NATIVE_TYPE_CUSTOMMARSHALER, 0, 0, 4, 'K', 'i', 'n', 'd', 2, 'i', 'd' });

    Bytes typeRef = AttributeWithUInt32(NATIVE_TYPE_CUSTOMMARSHALER);
    typeRef.resize(typeRef.size() - 2);
    AppendUInt16(typeRef, 1);
    AppendNamedString(typeRef, "MarshalTypeRef", "Demo", SERIALIZATION_TYPE_TYPE);
    ASSERT_EQ(S_OK, Apply(emit.p, field, ctor, typeRef));
    AssertMarshal(import.p, field, Bytes{ NATIVE_TYPE_CUSTOMMARSHALER, 0, 0, 4, 'D', 'e', 'm', 'o', 0 });
    EXPECT_EQ(1u, CountRows(emit.p, mdtid_FieldMarshal));
}

TEST(CustomAttribute, MarshalAsPropertyTargetsAccessorParameters)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdTypeDef type = DefineType(emit.p);
    Bytes getterSignature{ IMAGE_CEE_CS_CALLCONV_DEFAULT, 0, ELEMENT_TYPE_I4 };
    Bytes setterSignature{ IMAGE_CEE_CS_CALLCONV_DEFAULT, 1, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I4 };
    mdMethodDef getter, setter;
    ASSERT_EQ(S_OK, emit->DefineMethod(type, W("get_Number"), mdPublic, getterSignature.data(),
                                       static_cast<ULONG>(getterSignature.size()), 0, 0, &getter));
    mdParamDef returnParam = DefineParam(emit.p, getter, 0);
    ASSERT_EQ(S_OK, emit->DefineMethod(type, W("set_Number"), mdPublic, setterSignature.data(),
                                       static_cast<ULONG>(setterSignature.size()), 0, 0, &setter));
    mdParamDef valueParam = DefineParam(emit.p, setter, 1);
    Bytes propertySignature{ IMAGE_CEE_CS_CALLCONV_PROPERTY, 0, ELEMENT_TYPE_I4 };
    mdProperty property;
    ASSERT_EQ(S_OK, emit->DefineProperty(type, W("Number"), 0, propertySignature.data(),
                                          static_cast<ULONG>(propertySignature.size()), ELEMENT_TYPE_VOID,
                                          nullptr, 0, setter, getter, nullptr, &property));
    mdMemberRef ctor = DefineCtor(emit.p, W("System.Runtime.InteropServices.MarshalAsAttribute"),
                                  { SERIALIZATION_TYPE_I2 });
    ASSERT_EQ(S_OK, Apply(emit.p, property, ctor, AttributeWithUInt16(NATIVE_TYPE_LPWSTR)));
    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));
    AssertMarshal(import.p, returnParam, Bytes{ NATIVE_TYPE_LPWSTR });
    AssertMarshal(import.p, valueParam, Bytes{ NATIVE_TYPE_LPWSTR });
    EXPECT_EQ(2u, CountRows(emit.p, mdtid_FieldMarshal));
}

TEST(CustomAttribute, MarshalAsPropertyWithMissingReturnParameterStillMarshalsSetter)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdTypeDef type = DefineType(emit.p);
    Bytes getterSignature{ IMAGE_CEE_CS_CALLCONV_DEFAULT, 0, ELEMENT_TYPE_I4 };
    Bytes setterSignature{ IMAGE_CEE_CS_CALLCONV_DEFAULT, 1, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I4 };
    mdMethodDef getter, setter;
    ASSERT_EQ(S_OK, emit->DefineMethod(type, W("get_Number"), mdPublic, getterSignature.data(),
                                       static_cast<ULONG>(getterSignature.size()), 0, 0, &getter));
    ASSERT_EQ(S_OK, emit->DefineMethod(type, W("set_Number"), mdPublic, setterSignature.data(),
                                       static_cast<ULONG>(setterSignature.size()), 0, 0, &setter));
    mdParamDef valueParam = DefineParam(emit.p, setter, 1);
    Bytes propertySignature{ IMAGE_CEE_CS_CALLCONV_PROPERTY, 0, ELEMENT_TYPE_I4 };
    mdProperty property;
    ASSERT_EQ(S_OK, emit->DefineProperty(type, W("Number"), 0, propertySignature.data(),
                                          static_cast<ULONG>(propertySignature.size()), ELEMENT_TYPE_VOID,
                                          nullptr, 0, setter, getter, nullptr, &property));
    mdMemberRef ctor = DefineCtor(emit.p, W("System.Runtime.InteropServices.MarshalAsAttribute"),
                                  { SERIALIZATION_TYPE_I2 });
    ASSERT_EQ(S_OK, Apply(emit.p, property, ctor, AttributeWithUInt16(NATIVE_TYPE_I4)));
    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));
    AssertMarshal(import.p, valueParam, Bytes{ NATIVE_TYPE_I4 });
    EXPECT_EQ(1u, CountRows(emit.p, mdtid_FieldMarshal));
}

TEST(CustomAttribute, MarshalAsInvalidArgumentsDoNotAddFieldMarshal)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdTypeDef type = DefineType(emit.p);
    mdFieldDef field = DefineField(emit.p, type);
    mdMethodDef method = DefineMethod(emit.p, type);
    mdParamDef param = DefineParam(emit.p, method, 1);
    mdMemberRef ctor = DefineCtor(emit.p, W("System.Runtime.InteropServices.MarshalAsAttribute"),
                                  { EnumConstructor });

    EXPECT_EQ(META_E_CA_FIXEDSTR_SIZE_REQUIRED,
              Apply(emit.p, field, ctor, AttributeWithUInt32(NATIVE_TYPE_FIXEDSYSSTRING)));
    EXPECT_EQ(META_E_CA_NT_FIELDONLY,
              Apply(emit.p, param, ctor, AttributeWithUInt32(NATIVE_TYPE_FIXEDARRAY)));
    EXPECT_EQ(META_E_CA_INVALID_TARGET,
              Apply(emit.p, field, ctor, AttributeWithUInt32(NATIVE_TYPE_BYVALSTR)));
    EXPECT_EQ(META_E_CA_CUSTMARSH_TYPE_REQUIRED,
              Apply(emit.p, field, ctor, AttributeWithUInt32(NATIVE_TYPE_CUSTOMMARSHALER)));
    EXPECT_EQ(META_E_CA_INVALID_TARGET,
              Apply(emit.p, method, ctor, AttributeWithUInt32(NATIVE_TYPE_I4)));

    Bytes size = AttributeWithUInt32(NATIVE_TYPE_FIXEDARRAY);
    size.resize(size.size() - 2);
    AppendUInt16(size, 1);
    AppendNamedInt(size, "SizeConst", 0xffffffff);
    EXPECT_EQ(META_E_CA_NEGATIVE_CONSTSIZE, Apply(emit.p, field, ctor, size));

    Bytes paramIndex = AttributeWithUInt32(NATIVE_TYPE_ARRAY);
    paramIndex.resize(paramIndex.size() - 2);
    AppendUInt16(paramIndex, 1);
    AppendNamedInt16(paramIndex, "SizeParamIndex", 0xffff);
    EXPECT_EQ(META_E_CA_NEGATIVE_PARAMINDEX, Apply(emit.p, param, ctor, paramIndex));

    Bytes badSafeArray = AttributeWithUInt32(NATIVE_TYPE_SAFEARRAY);
    badSafeArray.resize(badSafeArray.size() - 2);
    AppendUInt16(badSafeArray, 2);
    AppendNamedEnum(badSafeArray, "SafeArraySubType", "System.Runtime.InteropServices.VarEnum", 3);
    AppendNamedString(badSafeArray, "SafeArrayUserDefinedSubType", "Demo", SERIALIZATION_TYPE_TYPE);
    EXPECT_EQ(META_E_CA_INVALID_MARSHALAS_FIELDS, Apply(emit.p, field, ctor, badSafeArray));
    EXPECT_EQ(0u, CountRows(emit.p, mdtid_FieldMarshal));
    EXPECT_EQ(0u, ReadColumn(emit.p, field, mdtField_Flags) & fdHasFieldMarshal);
    EXPECT_EQ(0u, ReadColumn(emit.p, param, mdtParam_Flags) & pdHasFieldMarshal);
}

TEST(CustomAttribute, SpecialNameSetsEachSupportedKind)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdTypeDef type = DefineType(emit.p);
    mdMethodDef method = DefineMethod(emit.p, type);
    mdFieldDef field = DefineField(emit.p, type);
    Bytes propertySignature{ IMAGE_CEE_CS_CALLCONV_PROPERTY, 0, ELEMENT_TYPE_I4 };
    mdProperty property;
    ASSERT_EQ(S_OK, emit->DefineProperty(type, W("Count"), 0, propertySignature.data(),
                                          static_cast<ULONG>(propertySignature.size()), ELEMENT_TYPE_VOID,
                                          nullptr, 0, mdMethodDefNil, mdMethodDefNil, nullptr, &property));
    mdEvent event;
    ASSERT_EQ(S_OK, emit->DefineEvent(type, W("Changed"), 0, mdTypeDefNil,
                                      mdMethodDefNil, mdMethodDefNil, mdMethodDefNil, nullptr, &event));
    mdMemberRef ctor = DefineCtor(emit.p, W("System.Runtime.CompilerServices.SpecialNameAttribute"));
    for (mdToken token : { mdToken(type), mdToken(method), mdToken(field), mdToken(property), mdToken(event) })
        ASSERT_EQ(S_OK, Apply(emit.p, token, ctor, EmptyAttribute));
    EXPECT_NE(0u, ReadColumn(emit.p, type, mdtTypeDef_Flags) & tdSpecialName);
    EXPECT_NE(0u, ReadColumn(emit.p, method, mdtMethodDef_Flags) & mdSpecialName);
    EXPECT_NE(0u, ReadColumn(emit.p, field, mdtField_Flags) & fdSpecialName);
    EXPECT_NE(0u, ReadColumn(emit.p, property, mdtProperty_Flags) & prSpecialName);
    EXPECT_NE(0u, ReadColumn(emit.p, event, mdtEvent_EventFlags) & evSpecialName);
    EXPECT_EQ(0u, CountRows(emit.p, mdtid_CustomAttribute));
}

TEST(CustomAttribute, VersionAttributesValidateAndRemainStored)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdTypeRef owner;
    ASSERT_EQ(S_OK, emit->DefineTypeRefByName(mdTokenNil, W("Example.Type"), &owner));
    mdMemberRef typeLib = DefineCtor(emit.p, W("System.Runtime.InteropServices.TypeLibVersionAttribute"),
                                     { SERIALIZATION_TYPE_I4, SERIALIZATION_TYPE_I4 });
    mdMemberRef compatible = DefineCtor(emit.p, W("System.Runtime.InteropServices.ComCompatibleVersionAttribute"),
                                        { SERIALIZATION_TYPE_I4, SERIALIZATION_TYPE_I4,
                                          SERIALIZATION_TYPE_I4, SERIALIZATION_TYPE_I4 });
    Bytes version{ 1, 0 };
    AppendUInt32(version, 1);
    AppendUInt32(version, 2);
    AppendUInt16(version, 0);
    mdCustomAttribute retained;
    EXPECT_EQ(S_OK, Apply(emit.p, owner, typeLib, version, &retained));
    EXPECT_EQ(mdtCustomAttribute, TypeFromToken(retained));
    version[2] = 0xff;
    version[3] = 0xff;
    version[4] = 0xff;
    version[5] = 0xff;
    EXPECT_EQ(META_E_CA_INVALID_VALUE, Apply(emit.p, owner, typeLib, version));

    Bytes fourVersions{ 1, 0 };
    for (uint32_t part : { 1u, 2u, 3u, 4u })
        AppendUInt32(fourVersions, part);
    AppendUInt16(fourVersions, 0);
    EXPECT_EQ(S_OK, Apply(emit.p, owner, compatible, fourVersions));
    fourVersions[14] = 0xff;
    fourVersions[15] = 0xff;
    fourVersions[16] = 0xff;
    fourVersions[17] = 0xff;
    EXPECT_EQ(META_E_CA_INVALID_VALUE, Apply(emit.p, owner, compatible, fourVersions));
    EXPECT_EQ(2u, CountRows(emit.p, mdtid_CustomAttribute));
}

TEST(CustomAttribute, SecurityAttributesRemainOrdinary)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdTypeDef type = DefineType(emit.p);
    mdMethodDef method = DefineMethod(emit.p, type);
    mdMemberRef ctor = DefineCtor(emit.p, W("System.Security.AllowPartiallyTrustedCallersAttribute"));
    mdCustomAttribute token = mdCustomAttributeNil;
    ASSERT_EQ(S_OK, Apply(emit.p, type, ctor, EmptyAttribute, &token));
    EXPECT_EQ(mdtCustomAttribute, TypeFromToken(token));
    mdMemberRef dynamic = DefineCtor(emit.p, W("System.Security.DynamicSecurityMethodAttribute"));
    mdMemberRef unmanaged = DefineCtor(emit.p, W("System.Security.SuppressUnmanagedCodeSecurityAttribute"));
    EXPECT_EQ(S_OK, Apply(emit.p, method, dynamic, EmptyAttribute));
    EXPECT_EQ(S_OK, Apply(emit.p, type, unmanaged, EmptyAttribute));
    EXPECT_EQ(3u, CountRows(emit.p, mdtid_CustomAttribute));
    EXPECT_EQ(0u, ReadColumn(emit.p, method, mdtMethodDef_Flags) & mdRequireSecObject);
    EXPECT_EQ(0u, ReadColumn(emit.p, type, mdtTypeDef_Flags) & tdHasSecurity);
    EXPECT_EQ(tdPublic, ReadColumn(emit.p, type, mdtTypeDef_Flags));
}

TEST(CustomAttribute, ThreadSafeEmitterHandlesPseudoAttributes)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateThreadSafeEmit(emit));
    mdTypeDef type = DefineType(emit.p);
    mdFieldDef field = DefineField(emit.p, type);
    mdMemberRef serializable = DefineCtor(emit.p, W("System.SerializableAttribute"));
    mdMemberRef marshalAs = DefineCtor(emit.p, W("System.Runtime.InteropServices.MarshalAsAttribute"),
                                       { SERIALIZATION_TYPE_I2 });
    mdMemberRef fieldOffset = DefineCtor(emit.p, W("System.Runtime.InteropServices.FieldOffsetAttribute"),
                                         { SERIALIZATION_TYPE_I4 });
    ASSERT_EQ(S_OK, Apply(emit.p, type, serializable, EmptyAttribute));
    ASSERT_EQ(S_OK, Apply(emit.p, field, marshalAs, AttributeWithUInt16(NATIVE_TYPE_I4)));
    ASSERT_EQ(S_OK, Apply(emit.p, field, fieldOffset, AttributeWithUInt32(16)));

    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));
    AssertMarshal(import.p, field, Bytes{ NATIVE_TYPE_I4 });
    EXPECT_NE(0u, ReadColumn(emit.p, type, mdtTypeDef_Flags) & tdSerializable);
    EXPECT_EQ(16u, ReadLayout(emit.p, mdtid_FieldLayout, mdtFieldLayout_Offset));
}

#if defined(DNMD_ENABLE_INTERNAL_INTERFACES)
TEST(CustomAttribute, ENCLogsOnlySuccessfulPseudoMutationsAndRetainedRows)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdTypeDef type = DefineType(emit.p);
    mdFieldDef field = DefineField(emit.p, type);
    mdMemberRef serializable = DefineCtor(emit.p, W("System.SerializableAttribute"));
    mdMemberRef fieldOffset = DefineCtor(emit.p, W("System.Runtime.InteropServices.FieldOffsetAttribute"),
                                         { SERIALIZATION_TYPE_I4 });
    mdMemberRef layout = DefineCtor(emit.p, W("System.Runtime.InteropServices.StructLayoutAttribute"),
                                    { SERIALIZATION_TYPE_I4 });
    mdMemberRef marshalAs = DefineCtor(emit.p, W("System.Runtime.InteropServices.MarshalAsAttribute"),
                                       { SERIALIZATION_TYPE_I2 });
    mdMemberRef guid = DefineCtor(emit.p, W("System.Runtime.InteropServices.GuidAttribute"),
                                  { SERIALIZATION_TYPE_STRING });
    mdMemberRef unknown = DefineCtor(emit.p, W("Example.UnknownAttribute"));

    minipal::com_ptr<IMDInternalEmit> internal;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMDInternalEmit, (void**)&internal));
    ULONG previous = UINT32_MAX;
    ASSERT_EQ(S_OK, internal->SetMDUpdateMode(MDUpdateENC, &previous));
    ASSERT_TRUE(ReadLog(emit.p).empty());

    ASSERT_EQ(S_OK, Apply(emit.p, type, serializable, EmptyAttribute));
    ASSERT_EQ(S_OK, Apply(emit.p, type, serializable, EmptyAttribute));
    EXPECT_EQ(META_E_CA_INVALID_VALUE,
              Apply(emit.p, field, fieldOffset, AttributeWithUInt32(0xffffffff)));
    EXPECT_EQ((std::vector<LogEntry>{ { type, 0 } }), ReadLog(emit.p));

    ASSERT_EQ(S_OK, Apply(emit.p, field, fieldOffset, AttributeWithUInt32(8)));
    Bytes pack = AttributeWithUInt32(1);
    pack.resize(pack.size() - 2);
    AppendUInt16(pack, 1);
    AppendNamedInt(pack, "Pack", 4);
    ASSERT_EQ(S_OK, Apply(emit.p, type, layout, pack));
    ASSERT_EQ(S_OK, Apply(emit.p, field, marshalAs, AttributeWithUInt16(NATIVE_TYPE_I4)));
    mdCustomAttribute kept;
    ASSERT_EQ(S_OK, Apply(emit.p, type, guid,
                          AttributeWithString("01234567-89ab-cdef-0123-456789abcdef"), &kept));
    mdCustomAttribute ordinary;
    ASSERT_EQ(S_OK, Apply(emit.p, type, unknown, Bytes{ 0xee }, &ordinary));
    EXPECT_EQ((std::vector<LogEntry>{
        { type, 0 },
        { RowToken(mdtid_FieldLayout, 1), 0 },
        { RowToken(mdtid_ClassLayout, 1), 0 }, { type, 0 },
        { field, 0 }, { RowToken(mdtid_FieldMarshal, 1), 0 },
        { kept, 0 }, { ordinary, 0 }
    }), ReadLog(emit.p));
    EXPECT_EQ(2u, CountRows(emit.p, mdtid_CustomAttribute));

    Bytes updatedPack = AttributeWithUInt32(1);
    updatedPack.resize(updatedPack.size() - 2);
    AppendUInt16(updatedPack, 1);
    AppendNamedInt(updatedPack, "Pack", 8);
    std::vector<LogEntry> expected = ReadLog(emit.p);
    expected.emplace_back(RowToken(mdtid_ClassLayout, 1), 0);
    expected.emplace_back(type, 0);
    ASSERT_EQ(S_OK, Apply(emit.p, type, layout, updatedPack));
    EXPECT_EQ(expected, ReadLog(emit.p));
    ASSERT_EQ(S_OK, Apply(emit.p, type, layout, updatedPack));
    EXPECT_EQ(expected, ReadLog(emit.p));
}

TEST(CustomAttribute, ENCLogsDllImportModuleAndMapOnlyAfterValidatingBlob)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdTypeDef type = DefineType(emit.p);
    mdMethodDef method = DefineMethod(emit.p, type);
    mdMemberRef ctor = DefineCtor(emit.p, W("System.Runtime.InteropServices.DllImportAttribute"),
                                  { SERIALIZATION_TYPE_STRING });
    minipal::com_ptr<IMDInternalEmit> internal;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMDInternalEmit, (void**)&internal));
    ULONG previous;
    ASSERT_EQ(S_OK, internal->SetMDUpdateMode(MDUpdateENC, &previous));

    Bytes invalid = AttributeWithString("native.dll");
    invalid.resize(invalid.size() - 2);
    AppendUInt16(invalid, 1);
    AppendNamedEnum(invalid, "CallingConvention", "System.Runtime.InteropServices.CallingConvention", 6);
    EXPECT_EQ(META_E_CA_INVALID_VALUE, Apply(emit.p, method, ctor, invalid));
    EXPECT_TRUE(ReadLog(emit.p).empty());
    EXPECT_EQ(0u, CountRows(emit.p, mdtid_ModuleRef));

    ASSERT_EQ(S_OK, Apply(emit.p, method, ctor, AttributeWithString("native.dll")));
    EXPECT_EQ((std::vector<LogEntry>{
        { TokenFromRid(1, mdtModuleRef), 0 }, { method, 0 },
        { RowToken(mdtid_ImplMap, 1), 0 }, { method, 0 }
    }), ReadLog(emit.p));
    ASSERT_EQ(S_OK, Apply(emit.p, method, ctor, AttributeWithString("native.dll")));
    EXPECT_EQ(4u, ReadLog(emit.p).size());
    EXPECT_EQ(1u, CountRows(emit.p, mdtid_ImplMap));

    ASSERT_EQ(S_OK, Apply(emit.p, method, ctor, AttributeWithString("other.dll")));
    EXPECT_EQ((std::vector<LogEntry>{
        { TokenFromRid(1, mdtModuleRef), 0 }, { method, 0 },
        { RowToken(mdtid_ImplMap, 1), 0 }, { method, 0 },
        { TokenFromRid(2, mdtModuleRef), 0 },
        { RowToken(mdtid_ImplMap, 1), 0 }, { method, 0 }
    }), ReadLog(emit.p));
    EXPECT_EQ(2u, CountRows(emit.p, mdtid_ModuleRef));
    EXPECT_EQ(1u, CountRows(emit.p, mdtid_ImplMap));
}
#endif // DNMD_ENABLE_INTERNAL_INTERFACES
