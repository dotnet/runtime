#include "metadataemit.hpp"
#include "importhelpers.hpp"
#include "signatures.hpp"
#include "pal.hpp"
#include <array>
#include <cctype>
#include <limits>
#include <fstream>
#include <functional>
#include <new>
#include <stack>
#include <algorithm>
#include <utility>
#include <cstring>
#include <minipal/strings.h>

#define RETURN_IF_FAILED(exp) \
{ \
    hr = (exp); \
    if (FAILED(hr)) \
    { \
        return hr; \
    } \
}

#define MD_MODULE_TOKEN TokenFromRid(1, mdtModule)
#define MD_GLOBAL_PARENT_TOKEN TokenFromRid(1, mdtTypeDef)

namespace
{
    enum ENCOperation : uint32_t
    {
        ENCUpdate = 0,
        ENCMethodCreate = 1,
        ENCFieldCreate = 2,
        ENCParamCreate = 3,
        ENCPropertyCreate = 4,
        ENCEventCreate = 5,
    };

    HRESULT AppendENCLog(mdhandle_t metadata, mdToken token, uint32_t operation)
    {
        md_added_row_t row{ mdcursor_t{} };
        if (!md_append_row(metadata, mdtid_ENCLog, &row))
            return E_FAIL;
        if (!md_set_column_value_as_constant(row, mdtENCLog_Token, token)
            || !md_set_column_value_as_constant(row, mdtENCLog_Op, operation))
            return E_FAIL;
        return S_OK;
    }

    struct MetadataSnapshot
    {
        malloc_ptr<void> image;
        mdhandle_ptr handle;
        size_t size = 0;
    };

    bool RemainingImageBytes(MetadataSnapshot const& snapshot, void const* pointer, size_t& remaining)
    {
        uintptr_t begin = reinterpret_cast<uintptr_t>(snapshot.image.get());
        uintptr_t address = reinterpret_cast<uintptr_t>(pointer);
        if (address < begin || address - begin >= snapshot.size)
            return false;
        remaining = snapshot.size - (address - begin);
        return true;
    }

    HRESULT SerializeMetadata(mdhandle_t metadata, malloc_ptr<void>& image, size_t& size)
    {
        size = 0;
        (void)md_write_to_buffer(metadata, nullptr, &size);
        if (size == 0)
            return CLDB_E_FILE_CORRUPT;
        if (size > UINT32_MAX)
            return CLDB_E_TOO_BIG;

        malloc_ptr<void> buffer{ ::malloc(size) };
        if (buffer == nullptr)
            return E_OUTOFMEMORY;
        if (!md_write_to_buffer(metadata, static_cast<uint8_t*>(buffer.get()), &size))
            return CLDB_E_FILE_CORRUPT;

        image = std::move(buffer);
        return S_OK;
    }

    HRESULT OpenSnapshot(malloc_ptr<void> image, size_t size, MetadataSnapshot& snapshot)
    {
        mdhandle_t handle;
        if (!md_create_handle(image.get(), size, &handle))
            return CLDB_E_FILE_CORRUPT;

        snapshot.handle.reset(handle);
        snapshot.image = std::move(image);
        snapshot.size = size;
        return S_OK;
    }

    HRESULT CloneMetadata(mdhandle_t metadata, MetadataSnapshot& snapshot)
    {
        malloc_ptr<void> image;
        size_t size;
        HRESULT hr = SerializeMetadata(metadata, image, size);
        return FAILED(hr) ? hr : OpenSnapshot(std::move(image), size, snapshot);
    }

    HRESULT SnapshotDelta(IUnknown* source, IDNMDOwner* owner, MetadataSnapshot& snapshot)
    {
        if (!owner->IsReadWrite())
            return CloneMetadata(owner->MetaData(), snapshot);

        // The emitter performs the copy under the source scope's lock when it is thread-safe.
        minipal::com_ptr<IMetaDataEmit2> emitter;
        HRESULT hr = source->QueryInterface(IID_IMetaDataEmit2, (void**)&emitter);
        if (FAILED(hr))
            return hr;

        DWORD size;
        hr = emitter->GetSaveSize(cssAccurate, &size);
        if (FAILED(hr))
            return hr;
        if (size == 0)
            return CLDB_E_FILE_CORRUPT;

        malloc_ptr<void> image{ ::malloc(size) };
        if (image == nullptr)
            return E_OUTOFMEMORY;
        hr = emitter->SaveToMemory(image.get(), size);
        return FAILED(hr) ? hr : OpenSnapshot(std::move(image), size, snapshot);
    }

    uint32_t ReadUInt32(uint8_t const* data)
    {
        return uint32_t(data[0]) | (uint32_t(data[1]) << 8) |
            (uint32_t(data[2]) << 16) | (uint32_t(data[3]) << 24);
    }

    uint64_t ReadUInt64(uint8_t const* data)
    {
        return uint64_t(ReadUInt32(data)) | (uint64_t(ReadUInt32(data + 4)) << 32);
    }

    void WriteUInt32(uint8_t* data, uint32_t value)
    {
        for (size_t i = 0; i < sizeof(value); ++i)
            data[i] = static_cast<uint8_t>(value >> (i * 8));
    }

    void WriteUInt64(uint8_t* data, uint64_t value)
    {
        WriteUInt32(data, static_cast<uint32_t>(value));
        WriteUInt32(data + 4, static_cast<uint32_t>(value >> 32));
    }

    bool FindFinalTablesStream(uint8_t const* image, size_t size, size_t& streamOffset,
                               size_t& streamSize, size_t& sizeFieldOffset)
    {
        // ECMA-335 II.24.2.1-2: metadata root and stream headers.
        if (size < 20 || ReadUInt32(image) != 0x424a5342)
            return false;

        size_t versionLength = ReadUInt32(image + 12);
        if (versionLength > size - 20)
            return false;

        size_t offset = 16 + ((versionLength + 3) & ~size_t(3));
        if (offset > size - 4)
            return false;
        uint16_t streamCount = uint16_t(image[offset + 2]) | (uint16_t(image[offset + 3]) << 8);
        offset += 4;

        bool found = false;
        for (uint16_t i = 0; i < streamCount; ++i)
        {
            if (offset > size || size - offset < 12)
                return false;

            uint8_t const* name = image + offset + 8;
            uint8_t const* end = static_cast<uint8_t const*>(std::memchr(name, 0, size - offset - 8));
            if (end == nullptr)
                return false;
            size_t nameLength = end - name;
            if (nameLength > size - offset - 12)
                return false;
            size_t paddedNameLength = (nameLength + 4) & ~size_t(3);
            if (paddedNameLength > size - offset - 8)
                return false;

            if (nameLength == 2 && name[0] == '#' && (name[1] == '~' || name[1] == '-'))
            {
                streamOffset = ReadUInt32(image + offset);
                streamSize = ReadUInt32(image + offset + 4);
                sizeFieldOffset = offset + 4;
                found = streamOffset <= size && streamSize == size - streamOffset;
            }
            offset += 8 + paddedNameLength;
        }
        return found;
    }

    HRESULT ClearENCLog(MetadataSnapshot& snapshot)
    {
        mdcursor_t firstLogRow;
        uint32_t logCount;
        if (!md_create_cursor(snapshot.handle.get(), mdtid_ENCLog, &firstLogRow, &logCount))
            return S_OK;

        // Write a canonical image with the tables stream last, even for scopes opened from memory.
        mdcursor_t module;
        uint32_t generation;
        if (!md_token_to_cursor(snapshot.handle.get(), MD_MODULE_TOKEN, &module)
            || !md_get_column_value_as_constant(module, mdtModule_Generation, &generation)
            || !md_set_column_value_as_constant(module, mdtModule_Generation, generation))
            return CLDB_E_FILE_CORRUPT;

        malloc_ptr<void> image;
        size_t size;
        HRESULT hr = SerializeMetadata(snapshot.handle.get(), image, size);
        if (FAILED(hr))
            return hr;

        // DNMD has no table-clear API. Locate the first log row by changing only
        // its four-byte token in a throwaway clone, avoiding assumptions about
        // the widths of preceding metadata tables.
        uint32_t token;
        if (!md_get_column_value_as_constant(firstLogRow, mdtENCLog_Token, &token)
            || !md_set_column_value_as_constant(firstLogRow, mdtENCLog_Token, ~token))
            return CLDB_E_FILE_CORRUPT;

        malloc_ptr<void> changed;
        size_t changedSize;
        hr = SerializeMetadata(snapshot.handle.get(), changed, changedSize);
        if (FAILED(hr))
            return hr;
        if (changedSize != size)
            return CLDB_E_FILE_CORRUPT;

        uint8_t* bytes = static_cast<uint8_t*>(image.get());
        uint8_t const* changedBytes = static_cast<uint8_t const*>(changed.get());
        size_t logOffset = size;
        size_t changedCount = 0;
        for (size_t i = 0; i < size; ++i)
        {
            if (bytes[i] != changedBytes[i])
            {
                if (logOffset == size)
                    logOffset = i;
                if (i - logOffset >= sizeof(uint32_t))
                    return CLDB_E_FILE_CORRUPT;
                ++changedCount;
            }
        }
        if (changedCount != sizeof(uint32_t))
            return CLDB_E_FILE_CORRUPT;

        size_t tablesOffset, tablesSize, sizeFieldOffset;
        if (!FindFinalTablesStream(bytes, size, tablesOffset, tablesSize, sizeFieldOffset)
            || tablesSize < 24 || logOffset < tablesOffset + 24)
            return CLDB_E_FILE_CORRUPT;

        uint64_t validTables = ReadUInt64(bytes + tablesOffset + 8);
        constexpr uint64_t encLogBit = uint64_t(1) << mdtid_ENCLog;
        if ((validTables & encLogBit) == 0)
            return CLDB_E_FILE_CORRUPT;

        size_t earlierCounts = 0, totalCounts = 0;
        for (size_t i = 0; i < 64; ++i)
        {
            if ((validTables & (uint64_t(1) << i)) != 0)
            {
                ++totalCounts;
                if (i < mdtid_ENCLog)
                    ++earlierCounts;
            }
        }

        size_t countsOffset = tablesOffset + 24;
        if (totalCounts > (size - countsOffset) / sizeof(uint32_t))
            return CLDB_E_FILE_CORRUPT;
        size_t logCountOffset = countsOffset + earlierCounts * sizeof(uint32_t);
        if (logCountOffset > logOffset || logOffset - logCountOffset < sizeof(uint32_t)
            || ReadUInt32(bytes + logCountOffset) != logCount
            || logCount > (size - logOffset) / (2 * sizeof(uint32_t)))
            return CLDB_E_FILE_CORRUPT;

        size_t logBytes = size_t(logCount) * (2 * sizeof(uint32_t));
        if (logBytes > tablesSize - sizeof(uint32_t))
            return CLDB_E_FILE_CORRUPT;
        size_t removedBytes = logBytes + sizeof(uint32_t);

        std::memmove(bytes + logOffset, bytes + logOffset + logBytes, size - logOffset - logBytes);
        size -= logBytes;
        std::memmove(bytes + logCountOffset, bytes + logCountOffset + sizeof(uint32_t),
                     size - logCountOffset - sizeof(uint32_t));
        size -= sizeof(uint32_t);

        WriteUInt64(bytes + tablesOffset + 8, validTables & ~encLogBit);
        WriteUInt64(bytes + tablesOffset + 16, ReadUInt64(bytes + tablesOffset + 16) & ~encLogBit);
        WriteUInt32(bytes + sizeFieldOffset, static_cast<uint32_t>(tablesSize - removedBytes));

        mdhandle_t reopened;
        if (!md_create_handle(bytes, size, &reopened))
            return CLDB_E_FILE_CORRUPT;
        snapshot.handle.reset(reopened);
        snapshot.image = std::move(image);
        snapshot.size = size;
        return S_OK;
    }

    HRESULT RestoreHeapColumns(mdcursor_t destination, mdcursor_t source, mdtable_id_t table,
                               MetadataSnapshot const* sourceSnapshot = nullptr);

    HRESULT ValidateNonRemappingDelta(MetadataSnapshot const& snapshot)
    {
        mdhandle_t delta = snapshot.handle.get();
        mdcursor_t row;
        uint32_t count;
        if (md_create_cursor(delta, mdtid_ENCMap, &row, &count))
            return E_NOTIMPL;
        if (!md_create_cursor(delta, mdtid_ENCLog, &row, &count))
            return S_OK;

        mdtable_id_t expectedNext = mdtid_Unused;
        for (uint32_t i = 0; i < count; ++i)
        {
            uint32_t token, operation;
            if (!md_get_column_value_as_constant(row, mdtENCLog_Token, &token)
                || !md_get_column_value_as_constant(row, mdtENCLog_Op, &operation))
                return E_INVALIDARG;

            mdtable_id_t table = static_cast<mdtable_id_t>((token >> 24) & 0x7f);
            if (table < mdtid_First || table >= mdtid_End || RidFromToken(token) == 0)
                return E_INVALIDARG;
            if (expectedNext != mdtid_Unused && (operation != ENCUpdate || table != expectedNext))
                return E_INVALIDARG;
            expectedNext = mdtid_Unused;

            switch (operation)
            {
            case ENCUpdate:
                break;
            case ENCMethodCreate:
            case ENCFieldCreate:
                if (table != mdtid_TypeDef)
                    return E_INVALIDARG;
                expectedNext = operation == ENCMethodCreate ? mdtid_MethodDef : mdtid_Field;
                break;
            case ENCParamCreate:
                if (table != mdtid_MethodDef)
                    return E_INVALIDARG;
                expectedNext = mdtid_Param;
                break;
            case ENCPropertyCreate:
            case ENCEventCreate:
                if (table != (operation == ENCPropertyCreate ? mdtid_PropertyMap : mdtid_EventMap))
                    return E_INVALIDARG;
                expectedNext = operation == ENCPropertyCreate ? mdtid_Property : mdtid_Event;
                break;
            default:
                return E_INVALIDARG;
            }
            if (operation == ENCUpdate)
            {
                mdcursor_t deltaRow;
                if (!md_token_to_cursor(delta, token & 0x7fffffffu, &deltaRow))
                    return E_INVALIDARG;
                HRESULT hr = RestoreHeapColumns(deltaRow, deltaRow, table, &snapshot);
                if (FAILED(hr))
                    return hr;
            }
            if (i + 1 < count && !md_cursor_next(&row))
                return E_INVALIDARG;
        }
        return expectedNext == mdtid_Unused ? S_OK : E_INVALIDARG;
    }

    HRESULT RestoreString(mdcursor_t destination, mdcursor_t source, col_index_t column,
                          MetadataSnapshot const* sourceSnapshot)
    {
        char const* value;
        if (!md_get_column_value_as_utf8(source, column, &value))
            return E_INVALIDARG;
        if (sourceSnapshot != nullptr)
        {
            size_t remaining;
            if (RemainingImageBytes(*sourceSnapshot, value, remaining))
            {
                if (std::memchr(value, 0, remaining) == nullptr)
                    return E_INVALIDARG;
            }
            else if (value == nullptr || *value != '\0')
            {
                return E_INVALIDARG;
            }
            return S_OK;
        }
        return md_set_column_value_as_utf8(destination, column, value) ? S_OK : E_FAIL;
    }

    HRESULT RestoreBlob(mdcursor_t destination, mdcursor_t source, col_index_t column,
                        MetadataSnapshot const* sourceSnapshot)
    {
        uint8_t const* value;
        uint32_t length;
        if (!md_get_column_value_as_blob(source, column, &value, &length))
            return E_INVALIDARG;
        if (sourceSnapshot != nullptr && length != 0)
        {
            size_t remaining;
            if (!RemainingImageBytes(*sourceSnapshot, value, remaining) || length > remaining)
                return E_INVALIDARG;
        }
        if (sourceSnapshot != nullptr)
            return S_OK;
        return md_set_column_value_as_blob(destination, column, value, length) ? S_OK : E_FAIL;
    }

    HRESULT RestoreGuid(mdcursor_t destination, mdcursor_t source, col_index_t column)
    {
        mdguid_t value;
        if (!md_get_column_value_as_guid(source, column, &value))
            return E_INVALIDARG;
        mdguid_t current;
        if (md_get_column_value_as_guid(destination, column, &current)
            && std::memcmp(&value, &current, sizeof(value)) == 0)
            return S_OK;
        return md_set_column_value_as_guid(destination, column, value) ? S_OK : E_FAIL;
    }

    HRESULT RestoreHeapColumns(mdcursor_t destination, mdcursor_t source, mdtable_id_t table,
                               MetadataSnapshot const* sourceSnapshot)
    {
        HRESULT hr;
#define RESTORE_STRING(column) RETURN_IF_FAILED(RestoreString(destination, source, column, sourceSnapshot))
#define RESTORE_BLOB(column) RETURN_IF_FAILED(RestoreBlob(destination, source, column, sourceSnapshot))
#define RESTORE_GUID(column) RETURN_IF_FAILED(RestoreGuid(destination, source, column))
        switch (table)
        {
        case mdtid_Module:
            RESTORE_STRING(mdtModule_Name);
            RESTORE_GUID(mdtModule_Mvid);
            RESTORE_GUID(mdtModule_EncId);
            RESTORE_GUID(mdtModule_EncBaseId);
            break;
        case mdtid_TypeRef:
            RESTORE_STRING(mdtTypeRef_TypeName);
            RESTORE_STRING(mdtTypeRef_TypeNamespace);
            break;
        case mdtid_TypeDef:
            RESTORE_STRING(mdtTypeDef_TypeName);
            RESTORE_STRING(mdtTypeDef_TypeNamespace);
            break;
        case mdtid_Field:
            RESTORE_STRING(mdtField_Name);
            RESTORE_BLOB(mdtField_Signature);
            break;
        case mdtid_MethodDef:
            RESTORE_STRING(mdtMethodDef_Name);
            RESTORE_BLOB(mdtMethodDef_Signature);
            break;
        case mdtid_Param:
            RESTORE_STRING(mdtParam_Name);
            break;
        case mdtid_MemberRef:
            RESTORE_STRING(mdtMemberRef_Name);
            RESTORE_BLOB(mdtMemberRef_Signature);
            break;
        case mdtid_Constant:
            RESTORE_BLOB(mdtConstant_Value);
            break;
        case mdtid_CustomAttribute:
            RESTORE_BLOB(mdtCustomAttribute_Value);
            break;
        case mdtid_FieldMarshal:
            RESTORE_BLOB(mdtFieldMarshal_NativeType);
            break;
        case mdtid_DeclSecurity:
            RESTORE_BLOB(mdtDeclSecurity_PermissionSet);
            break;
        case mdtid_StandAloneSig:
            RESTORE_BLOB(mdtStandAloneSig_Signature);
            break;
        case mdtid_Event:
            RESTORE_STRING(mdtEvent_Name);
            break;
        case mdtid_Property:
            RESTORE_STRING(mdtProperty_Name);
            RESTORE_BLOB(mdtProperty_Type);
            break;
        case mdtid_ModuleRef:
            RESTORE_STRING(mdtModuleRef_Name);
            break;
        case mdtid_TypeSpec:
            RESTORE_BLOB(mdtTypeSpec_Signature);
            break;
        case mdtid_ImplMap:
            RESTORE_STRING(mdtImplMap_ImportName);
            break;
        case mdtid_Assembly:
            RESTORE_BLOB(mdtAssembly_PublicKey);
            RESTORE_STRING(mdtAssembly_Name);
            RESTORE_STRING(mdtAssembly_Culture);
            break;
        case mdtid_AssemblyRef:
            RESTORE_BLOB(mdtAssemblyRef_PublicKeyOrToken);
            RESTORE_STRING(mdtAssemblyRef_Name);
            RESTORE_STRING(mdtAssemblyRef_Culture);
            RESTORE_BLOB(mdtAssemblyRef_HashValue);
            break;
        case mdtid_File:
            RESTORE_STRING(mdtFile_Name);
            RESTORE_BLOB(mdtFile_HashValue);
            break;
        case mdtid_ExportedType:
            RESTORE_STRING(mdtExportedType_TypeName);
            RESTORE_STRING(mdtExportedType_TypeNamespace);
            break;
        case mdtid_ManifestResource:
            RESTORE_STRING(mdtManifestResource_Name);
            break;
        case mdtid_GenericParam:
            RESTORE_STRING(mdtGenericParam_Name);
            break;
        case mdtid_MethodSpec:
            RESTORE_BLOB(mdtMethodSpec_Instantiation);
            break;
        case mdtid_InterfaceImpl:
        case mdtid_ClassLayout:
        case mdtid_FieldLayout:
        case mdtid_EventMap:
        case mdtid_PropertyMap:
        case mdtid_MethodSemantics:
        case mdtid_MethodImpl:
        case mdtid_FieldRva:
        case mdtid_NestedClass:
        case mdtid_GenericParamConstraint:
            break;
        default:
            return E_NOTIMPL;
        }
#undef RESTORE_GUID
#undef RESTORE_BLOB
#undef RESTORE_STRING
        return S_OK;
    }

    HRESULT RestoreDeltaHeaps(mdhandle_t destination, mdhandle_t delta)
    {
        HRESULT hr;
        mdcursor_t module, deltaModule;
        if (!md_token_to_cursor(destination, MD_MODULE_TOKEN, &module)
            || !md_token_to_cursor(delta, MD_MODULE_TOKEN, &deltaModule))
            return E_INVALIDARG;
        RETURN_IF_FAILED(RestoreGuid(module, deltaModule, mdtModule_EncId));

        mdcursor_t log;
        uint32_t count;
        if (!md_create_cursor(delta, mdtid_ENCLog, &log, &count))
            return S_OK;

        for (uint32_t i = 0; i < count; ++i)
        {
            uint32_t token, operation;
            if (!md_get_column_value_as_constant(log, mdtENCLog_Token, &token)
                || !md_get_column_value_as_constant(log, mdtENCLog_Op, &operation))
                return E_INVALIDARG;
            if (operation == ENCUpdate)
            {
                mdToken rowToken = token & 0x7fffffffu;
                mdcursor_t updatedRow, deltaRow;
                if (!md_token_to_cursor(destination, rowToken, &updatedRow)
                    || !md_token_to_cursor(delta, rowToken, &deltaRow))
                    return E_INVALIDARG;
                RETURN_IF_FAILED(RestoreHeapColumns(updatedRow, deltaRow,
                    static_cast<mdtable_id_t>(TypeFromToken(rowToken) >> 24)));
            }
            if (i + 1 < count && !md_cursor_next(&log))
                return E_INVALIDARG;
        }
        return S_OK;
    }

    mdhandle_t MetaDataOrNull(IDNMDOwner* owner)
    {
        return owner == nullptr ? nullptr : owner->MetaData();
    }

    void SplitTypeName(
        char* typeName,
        char const** nspace,
        char const** name)
    {
        // Search for the last delimiter.
        char* pos = std::strrchr(typeName, '.');
        if (pos == nullptr)
        {
            // No namespace is indicated by an empty string.
            *nspace = "";
            *name = typeName;
        }
        else
        {
            *pos = '\0';
            *nspace = typeName;
            *name = pos + 1;
        }
    }

    size_t DuplicateHash(void const* data, size_t length)
    {
        size_t hash = sizeof(size_t) == 8 ? static_cast<size_t>(14695981039346656037ull) : 2166136261u;
        size_t prime = sizeof(size_t) == 8 ? static_cast<size_t>(1099511628211ull) : 16777619u;
        uint8_t const* bytes = static_cast<uint8_t const*>(data);
        for (size_t i = 0; i < length; ++i)
        {
            hash ^= bytes[i];
            hash *= prime;
        }
        return hash;
    }

    size_t DuplicateNameHash(char const* name)
    {
        return DuplicateHash(name, std::strlen(name));
    }

    HRESULT DuplicateRowHash(mdcursor_t row, mdtable_id_t table, size_t* hash)
    {
        col_index_t column;
        switch (table)
        {
        case mdtid_TypeDef: column = mdtTypeDef_TypeName; break;
        case mdtid_TypeRef: column = mdtTypeRef_TypeName; break;
        case mdtid_MemberRef: column = mdtMemberRef_Name; break;
        case mdtid_ModuleRef: column = mdtModuleRef_Name; break;
        case mdtid_AssemblyRef: column = mdtAssemblyRef_Name; break;
        case mdtid_File: column = mdtFile_Name; break;
        case mdtid_ExportedType: column = mdtExportedType_TypeName; break;
        case mdtid_StandAloneSig: column = mdtStandAloneSig_Signature; break;
        case mdtid_TypeSpec: column = mdtTypeSpec_Signature; break;
        case mdtid_MethodSpec: column = mdtMethodSpec_Instantiation; break;
        case mdtid_DeclSecurity:
        {
            mdToken parent;
            if (!md_get_column_value_as_token(row, mdtDeclSecurity_Parent, &parent))
                return CLDB_E_FILE_CORRUPT;
            *hash = std::hash<mdToken>{}(parent);
            return S_OK;
        }
        default:
            return E_INVALIDARG;
        }

        if (table == mdtid_StandAloneSig || table == mdtid_TypeSpec || table == mdtid_MethodSpec)
        {
            uint8_t const* data;
            uint32_t length;
            if (!md_get_column_value_as_blob(row, column, &data, &length))
                return CLDB_E_FILE_CORRUPT;
            *hash = DuplicateHash(data, length);
        }
        else
        {
            char const* name;
            if (!md_get_column_value_as_utf8(row, column, &name))
                return CLDB_E_FILE_CORRUPT;
            *hash = DuplicateNameHash(name);
        }
        return S_OK;
    }

    HRESULT MatchString(mdcursor_t row, col_index_t column, char const* expected)
    {
        char const* actual;
        if (!md_get_column_value_as_utf8(row, column, &actual))
            return CLDB_E_FILE_CORRUPT;
        return std::strcmp(actual, expected) == 0 ? S_OK : S_FALSE;
    }

    HRESULT MatchToken(mdcursor_t row, col_index_t column, mdToken expected)
    {
        mdToken actual;
        if (!md_get_column_value_as_token(row, column, &actual))
            return CLDB_E_FILE_CORRUPT;
        return actual == expected ? S_OK : S_FALSE;
    }

    HRESULT MatchConstant(mdcursor_t row, col_index_t column, uint32_t expected)
    {
        uint32_t actual;
        if (!md_get_column_value_as_constant(row, column, &actual))
            return CLDB_E_FILE_CORRUPT;
        return actual == expected ? S_OK : S_FALSE;
    }

    HRESULT MatchBlob(mdcursor_t row, col_index_t column, void const* expected, uint32_t expectedLength)
    {
        uint8_t const* actual;
        uint32_t actualLength;
        if (!md_get_column_value_as_blob(row, column, &actual, &actualLength))
            return CLDB_E_FILE_CORRUPT;
        return actualLength == expectedLength &&
            (actualLength == 0 || (expected != nullptr && std::memcmp(actual, expected, actualLength) == 0))
            ? S_OK : S_FALSE;
    }

    bool EqualsIgnoreAsciiCase(char const* left, char const* right)
    {
        while (*left != '\0' && *right != '\0')
        {
            if (std::tolower(static_cast<unsigned char>(*left)) !=
                std::tolower(static_cast<unsigned char>(*right)))
                return false;
            ++left;
            ++right;
        }
        return *left == *right;
    }

    HRESULT PublicKeyToken(uint8_t const* key, uint32_t length, std::array<uint8_t, 8>& token)
    {
        // The ECMA pseudo-key has a predefined token instead of a hashed public key.
        constexpr uint8_t ecmaKey[] = { 0, 0, 0, 0, 0, 0, 0, 0, 4, 0, 0, 0, 0, 0, 0, 0 };
        if (length == sizeof(ecmaKey) && std::memcmp(key, ecmaKey, length) == 0)
        {
            token = { 0xb7, 0x7a, 0x5c, 0x56, 0x19, 0x34, 0xe0, 0x89 };
            return S_OK;
        }

        uint32_t header[3];
        if (length < sizeof(header))
            return CORSEC_E_INVALID_PUBLICKEY;
        std::memcpy(header, key, sizeof(header));
        if (header[2] != length - sizeof(header) ||
            (header[1] != 0 && ((header[1] & (7u << 13)) != (4u << 13) ||
                                (header[1] & 511u) < 4)) ||
            (header[0] != 0 && (header[0] & (7u << 13)) != (1u << 13)) ||
            header[2] == 0 || key[sizeof(header)] != 0x06)
            return CORSEC_E_INVALID_PUBLICKEY;

        std::array<uint8_t, pal::SHA1_HASH_SIZE> hash;
        if (!pal::ComputeSha1Hash({ key, length }, hash))
            return CORSEC_E_INVALID_PUBLICKEY;
        std::reverse_copy(hash.end() - token.size(), hash.end(), token.begin());
        return S_OK;
    }

    HRESULT MatchAssemblyRefKey(mdcursor_t row, void const* key, uint32_t length, DWORD flags)
    {
        uint8_t const* existingKey;
        uint32_t existingLength;
        if (!md_get_column_value_as_blob(row, mdtAssemblyRef_PublicKeyOrToken, &existingKey, &existingLength))
            return CLDB_E_FILE_CORRUPT;
        if ((length == 0) != (existingLength == 0))
            return S_FALSE;
        if (length == 0)
            return S_OK;

        uint32_t existingFlags;
        if (!md_get_column_value_as_constant(row, mdtAssemblyRef_Flags, &existingFlags))
            return CLDB_E_FILE_CORRUPT;
        if (((flags ^ existingFlags) & afPublicKey) == 0)
            return MatchBlob(row, mdtAssemblyRef_PublicKeyOrToken, key, length);

        std::array<uint8_t, 8> token;
        if ((flags & afPublicKey) != 0)
        {
            HRESULT hr = PublicKeyToken(static_cast<uint8_t const*>(key), length, token);
            if (FAILED(hr))
                return hr;
            return existingLength == token.size() && std::memcmp(existingKey, token.data(), token.size()) == 0
                ? S_OK : S_FALSE;
        }

        HRESULT hr = PublicKeyToken(existingKey, existingLength, token);
        if (FAILED(hr))
            return hr;
        return length == token.size() && std::memcmp(key, token.data(), token.size()) == 0
            ? S_OK : S_FALSE;
    }
}

template<typename Match>
HRESULT MetadataEmit::FindExisting(mdtable_id_t table, size_t hash, Match match, mdToken* token)
{
    mdhandle_t metadata = MetaData();
    mdcursor_t row;
    uint32_t count;
    if (!md_create_cursor(metadata, table, &row, &count))
        return S_FALSE;

    try
    {
        DuplicateIndex& index = _duplicateIndexes[table];
        if (index.handle != metadata || index.indexedCount > count)
        {
            index.hashes.clear();
            index.indexedCount = 0;
            index.handle = metadata;
        }

        if (index.indexedCount != 0 && index.indexedCount < count &&
            !md_cursor_move(&row, static_cast<int32_t>(index.indexedCount)))
            return CLDB_E_FILE_CORRUPT;

        for (uint32_t i = index.indexedCount; i < count; ++i)
        {
            size_t rowHash;
            HRESULT hr = DuplicateRowHash(row, table, &rowHash);
            if (FAILED(hr))
                return hr;
            mdToken rowToken;
            if (!md_cursor_to_token(row, &rowToken))
                return CLDB_E_FILE_CORRUPT;
            index.hashes.emplace(rowHash, rowToken);
            if (i + 1 < count && !md_cursor_next(&row))
                return CLDB_E_FILE_CORRUPT;
        }
        index.indexedCount = count;

        typedef std::unordered_multimap<size_t, mdToken> Hashes;
        std::pair<Hashes::const_iterator, Hashes::const_iterator> matches = index.hashes.equal_range(hash);
        mdToken first = mdTokenNil;
        for (Hashes::const_iterator candidate = matches.first; candidate != matches.second; ++candidate)
        {
            mdcursor_t matchedRow;
            if (!md_token_to_cursor(metadata, candidate->second, &matchedRow))
                return CLDB_E_FILE_CORRUPT;
            HRESULT hr = match(matchedRow);
            if (FAILED(hr))
                return hr;
            if (hr == S_OK && (IsNilToken(first) || RidFromToken(candidate->second) < RidFromToken(first)))
                first = candidate->second;
        }
        if (!IsNilToken(first))
        {
            *token = first;
            return S_OK;
        }
        return S_FALSE;
    }
    catch (std::bad_alloc const&)
    {
        _duplicateIndexes.erase(table);
        return E_OUTOFMEMORY;
    }
}

HRESULT MetadataEmit::LogToken(mdToken token, uint32_t operation)
{
    return _md_ptr.UpdateMode() == MDUpdateENC
        ? AppendENCLog(MetaData(), token, operation)
        : S_OK;
}

HRESULT MetadataEmit::LogRow(mdcursor_t row, uint32_t operation)
{
    if (_md_ptr.UpdateMode() != MDUpdateENC)
        return S_OK;

    mdToken token;
    if (!md_cursor_to_token(row, &token))
        return CLDB_E_FILE_CORRUPT;
    return AppendENCLog(MetaData(), token | 0x80000000u, operation);
}

HRESULT MetadataEmit::SetModuleProps(
        LPCWSTR     szName)
{
    // If the name is null, we have nothing to do.
    if (szName == nullptr)
        return LogToken(MD_MODULE_TOKEN);

    pal::StringConvert<WCHAR, char> cvt(szName);
    if (!cvt.Success())
        return E_INVALIDARG;

    mdcursor_t c;
    uint32_t count;
    if (!md_create_cursor(MetaData(), mdtid_Module, &c, &count))
    {
        if (md_append_row(MetaData(), mdtid_Module, &c))
        {
            md_commit_row_add(c);
        }
        else
        {
            return E_FAIL;
        }
    }

    // Search for a file name in the provided path
    // and use that as the module name.
    char* modulePath = cvt;
    std::size_t len = std::strlen(modulePath);
    char const* start = modulePath;
    for (char const* p = modulePath + len - 1; p >= modulePath; p--)
    {
        if (*p == '\\' || *p == '/')
        {
            start = p + 1;
            break;
        }
    }

    if (!md_set_column_value_as_utf8(c, mdtModule_Name, start))
        return E_FAIL;

    return LogToken(MD_MODULE_TOKEN);
}

HRESULT MetadataEmit::Save(
        LPCWSTR     szFile,
        DWORD       dwSaveFlags)
{
    if (dwSaveFlags != 0)
        return E_INVALIDARG;

    pal::StringConvert<WCHAR, char> cvt(szFile);
    if (!cvt.Success())
        return E_INVALIDARG;

    size_t saveSize;
    md_write_to_buffer(MetaData(), nullptr, &saveSize);
    std::unique_ptr<uint8_t[]> buffer { new uint8_t[saveSize] };
    if (!md_write_to_buffer(MetaData(), buffer.get(), &saveSize))
        return E_FAIL;

    std::FILE* file = std::fopen(cvt, "wb");
    if (file == nullptr)
    {
        return E_FAIL;
    }

    size_t totalSaved = 0;
    while (totalSaved < saveSize)
    {
        totalSaved += std::fwrite(buffer.get(), sizeof(uint8_t), saveSize - totalSaved, file);
        if (ferror(file) != 0)
        {
            std::fclose(file);
            return E_FAIL;
        }
    }

    if (std::fclose(file) == EOF)
    {
        return E_FAIL;
    }

    return S_OK;
}

HRESULT MetadataEmit::SaveToStream(
        IStream     *pIStream,
        DWORD       dwSaveFlags)
{
    HRESULT hr;
    if (dwSaveFlags != 0)
        return E_INVALIDARG;

    size_t saveSize;
    md_write_to_buffer(MetaData(), nullptr, &saveSize);
    std::unique_ptr<uint8_t[]> buffer { new uint8_t[saveSize] };
    md_write_to_buffer(MetaData(), buffer.get(), &saveSize);

    size_t totalSaved = 0;
    while (totalSaved < saveSize)
    {
        ULONG numBytesToWrite = (ULONG)std::min(saveSize, (size_t)std::numeric_limits<ULONG>::max());
        RETURN_IF_FAILED(pIStream->Write((char const*)buffer.get() + totalSaved, numBytesToWrite, nullptr));
        totalSaved += numBytesToWrite;
    }

    return pIStream->Write(buffer.get(), (ULONG)saveSize, nullptr);
}

HRESULT MetadataEmit::GetSaveSize(
        CorSaveSize fSave,
        DWORD       *pdwSaveSize)
{
    // TODO: Do we want to support different save modes (as specified through dispenser options)?
    // If so, we'll need to handle that here in addition to the ::Save* methods.
    UNREFERENCED_PARAMETER(fSave);
    size_t saveSize;
    md_write_to_buffer(MetaData(), nullptr, &saveSize);
    if (saveSize > std::numeric_limits<DWORD>::max())
        return CLDB_E_TOO_BIG;
    *pdwSaveSize = (DWORD)saveSize;
    return S_OK;
}

HRESULT MetadataEmit::DefineTypeDef(
        LPCWSTR     szTypeDef,
        DWORD       dwTypeDefFlags,
        mdToken     tkExtends,
        mdToken     rtkImplements[],
        mdTypeDef   *ptd)
{
    return DefineTypeDefCore(szTypeDef, dwTypeDefFlags, tkExtends, rtkImplements, mdTypeDefNil, ptd);
}

HRESULT MetadataEmit::DefineTypeDefCore(
        LPCWSTR     szTypeDef,
        DWORD       dwTypeDefFlags,
        mdToken     tkExtends,
        mdToken     rtkImplements[],
        mdTypeDef   tdEncloser,
        mdTypeDef   *ptd)
{
    HRESULT hr;
    pal::StringConvert<WCHAR, char> cvt(szTypeDef);
    if (!cvt.Success())
        return E_INVALIDARG;

    char const* ns;
    char const* name;
    SplitTypeName(cvt, &ns, &name);

    if (CheckDuplicates(MDDupTypeDef))
    {
        hr = FindExisting(mdtid_TypeDef, DuplicateNameHash(name), [&](mdcursor_t row)
        {
            HRESULT match = MatchString(row, mdtTypeDef_TypeNamespace, ns);
            if (match != S_OK)
                return match;
            match = MatchString(row, mdtTypeDef_TypeName, name);
            if (match != S_OK)
                return match;

            mdTypeDef candidate;
            if (!md_cursor_to_token(row, &candidate))
                return CLDB_E_FILE_CORRUPT;
            mdcursor_t nestedRows{}, nestedRow{};
            uint32_t count;
            bool isNested = md_create_cursor(MetaData(), mdtid_NestedClass, &nestedRows, &count) &&
                md_find_row_from_cursor(nestedRows, mdtNestedClass_NestedClass, RidFromToken(candidate), &nestedRow);
            if (!isNested)
                return IsNilToken(tdEncloser) ? S_OK : S_FALSE;
            if (IsNilToken(tdEncloser))
                return S_FALSE;
            return MatchToken(nestedRow, mdtNestedClass_EnclosingClass, tdEncloser);
        }, ptd);
        if (hr == S_OK)
            return META_S_DUPLICATE;
        if (FAILED(hr))
            return hr;
    }

    md_added_row_t c;
    if (!md_append_row(MetaData(), mdtid_TypeDef, &c))
        return E_FAIL;

    if (!md_set_column_value_as_utf8(c, mdtTypeDef_TypeNamespace, ns))
        return E_FAIL;
    if (!md_set_column_value_as_utf8(c, mdtTypeDef_TypeName, name))
        return E_FAIL;

    // TODO: Handle reserved flags
    uint32_t flags = (uint32_t)dwTypeDefFlags;
    if (!md_set_column_value_as_constant(c, mdtTypeDef_Flags, flags))
        return E_FAIL;

    if (!md_set_column_value_as_token(c, mdtTypeDef_Extends, tkExtends))
        return E_FAIL;

    mdcursor_t fieldCursor;
    uint32_t numFields;
    if (!md_create_cursor(MetaData(), mdtid_Field, &fieldCursor, &numFields))
    {
        mdToken nilField = mdFieldDefNil;
        if (!md_set_column_value_as_token(c, mdtTypeDef_FieldList, nilField))
            return E_FAIL;
    }
    else
    {
        md_cursor_move(&fieldCursor, numFields);
        if (!md_set_column_value_as_cursor(c, mdtTypeDef_FieldList, fieldCursor))
            return E_FAIL;
    }

    mdcursor_t methodCursor;
    uint32_t numMethods;
    if (!md_create_cursor(MetaData(), mdtid_MethodDef, &methodCursor, &numMethods))
    {
        mdToken nilMethod = mdMethodDefNil;
        if (!md_set_column_value_as_token(c, mdtTypeDef_MethodList, nilMethod))
            return E_FAIL;
    }
    else
    {
        md_cursor_move(&methodCursor, numMethods);
        if (!md_set_column_value_as_cursor(c, mdtTypeDef_MethodList, methodCursor))
            return E_FAIL;
    }

    if (!md_cursor_to_token(c, ptd))
        return E_FAIL;
    RETURN_IF_FAILED(LogToken(*ptd));

    size_t i = 0;

    if (rtkImplements != nullptr)
    {
        for (mdToken currentImplementation = rtkImplements[i]; currentImplementation != mdTokenNil; currentImplementation = rtkImplements[++i])
        {
            md_added_row_t interfaceImpl;
            if (!md_append_row(MetaData(), mdtid_InterfaceImpl, &interfaceImpl))
                return E_FAIL;

            if (!md_set_column_value_as_cursor(interfaceImpl, mdtInterfaceImpl_Class, c))
                return E_FAIL;

            if (!md_set_column_value_as_token(interfaceImpl, mdtInterfaceImpl_Interface, currentImplementation))
                return E_FAIL;
            mdToken token;
            if (!md_cursor_to_token(interfaceImpl, &token))
                return CLDB_E_FILE_CORRUPT;
            RETURN_IF_FAILED(LogToken(token));
        }
    }

    if (!IsNilToken(tdEncloser))
    {
        md_added_row_t nestedClass;
        if (!md_append_row(MetaData(), mdtid_NestedClass, &nestedClass) ||
            !md_set_column_value_as_token(nestedClass, mdtNestedClass_NestedClass, *ptd) ||
            !md_set_column_value_as_token(nestedClass, mdtNestedClass_EnclosingClass, tdEncloser))
            return E_FAIL;
        RETURN_IF_FAILED(LogRow(nestedClass));
    }

    return S_OK;
}

HRESULT MetadataEmit::DefineNestedType(
        LPCWSTR     szTypeDef,
        DWORD       dwTypeDefFlags,
        mdToken     tkExtends,
        mdToken     rtkImplements[],
        mdTypeDef   tdEncloser,
        mdTypeDef   *ptd)
{
    if (TypeFromToken(tdEncloser) != mdtTypeDef || IsNilToken(tdEncloser))
        return E_INVALIDARG;

    return DefineTypeDefCore(szTypeDef, dwTypeDefFlags, tkExtends, rtkImplements, tdEncloser, ptd);
}

HRESULT MetadataEmit::SetHandler(
        IUnknown    *pUnk)
{
    // The this implementation of MetadataEmit doesn't ever remap tokens,
    // so this method (which is for registering a callback for when tokens are remapped)
    // is a no-op.
    UNREFERENCED_PARAMETER(pUnk);
    return S_OK;
}

HRESULT MetadataEmit::DefineMethod(
        mdTypeDef       td,
        LPCWSTR         szName,
        DWORD           dwMethodFlags,
        PCCOR_SIGNATURE pvSigBlob,
        ULONG           cbSigBlob,
        ULONG           ulCodeRVA,
        DWORD           dwImplFlags,
        mdMethodDef     *pmd)
{
    HRESULT hr;
    if (TypeFromToken(td) != mdtTypeDef)
        return E_INVALIDARG;

    mdcursor_t type;
    if (!md_token_to_cursor(MetaData(), td, &type))
        return CLDB_E_FILE_CORRUPT;

    md_added_row_t newMethod;
    if (!md_add_new_row_to_list(type, mdtTypeDef_MethodList, &newMethod))
        return E_FAIL;

    pal::StringConvert<WCHAR, char> cvt(szName);

    char const* name = cvt;
    if (!md_set_column_value_as_utf8(newMethod, mdtMethodDef_Name, name))
        return E_FAIL;

    uint32_t flags = dwMethodFlags;
    if (!md_set_column_value_as_constant(newMethod, mdtMethodDef_Flags, flags))
        return E_FAIL;

    uint32_t sigLength = cbSigBlob;
    if (!md_set_column_value_as_blob(newMethod, mdtMethodDef_Signature, pvSigBlob, sigLength))
        return E_FAIL;

    uint32_t implFlags = dwImplFlags;
    if (!md_set_column_value_as_constant(newMethod, mdtMethodDef_ImplFlags, implFlags))
        return E_FAIL;

    uint32_t rva = ulCodeRVA;
    if (!md_set_column_value_as_constant(newMethod, mdtMethodDef_Rva, rva))
        return E_FAIL;

    if (!md_cursor_to_token(newMethod, pmd))
        return CLDB_E_FILE_CORRUPT;

    RETURN_IF_FAILED(LogToken(td, ENCMethodCreate));
    return LogToken(*pmd);
}

HRESULT MetadataEmit::DefineMethodImpl(
        mdTypeDef   td,
        mdToken     tkBody,
        mdToken     tkDecl)
{
    md_added_row_t c;
    if (!md_append_row(MetaData(), mdtid_MethodImpl, &c))
        return E_FAIL;

    if (!md_set_column_value_as_token(c, mdtMethodImpl_Class, td))
        return E_FAIL;

    if (!md_set_column_value_as_token(c, mdtMethodImpl_MethodBody, tkBody))
        return E_FAIL;

    if (!md_set_column_value_as_token(c, mdtMethodImpl_MethodDeclaration, tkDecl))
        return E_FAIL;

    return LogRow(c);
}

HRESULT MetadataEmit::DefineTypeRefByName(
        mdToken     tkResolutionScope,
        LPCWSTR     szName,
        mdTypeRef   *ptr)
{
    pal::StringConvert<WCHAR, char> cv(szName);
    if (!cv.Success())
        return E_FAIL;

    char const* ns;
    char const* name;
    SplitTypeName(cv, &ns, &name);

    if (CheckDuplicates(MDDupTypeRef))
    {
        HRESULT hr = FindExisting(mdtid_TypeRef, DuplicateNameHash(name), [&](mdcursor_t row)
        {
            HRESULT match = MatchToken(row, mdtTypeRef_ResolutionScope, tkResolutionScope);
            if (match != S_OK)
                return match;
            match = MatchString(row, mdtTypeRef_TypeNamespace, ns);
            return match == S_OK ? MatchString(row, mdtTypeRef_TypeName, name) : match;
        }, ptr);
        if (hr == S_OK)
            return META_S_DUPLICATE;
        if (FAILED(hr))
            return hr;
    }

    md_added_row_t c;
    if (!md_append_row(MetaData(), mdtid_TypeRef, &c))
        return E_FAIL;

    if (!md_set_column_value_as_token(c, mdtTypeRef_ResolutionScope, tkResolutionScope))
        return E_FAIL;

    if (!md_set_column_value_as_utf8(c, mdtTypeRef_TypeNamespace, ns))
        return E_FAIL;
    if (!md_set_column_value_as_utf8(c, mdtTypeRef_TypeName, name))
        return E_FAIL;

    if (!md_cursor_to_token(c, ptr))
        return E_FAIL;

    return LogToken(*ptr);
}

HRESULT MetadataEmit::DefineImportType(
        IMetaDataAssemblyImport *pAssemImport,
        void const *pbHashValue,
        ULONG       cbHashValue,
        IMetaDataImport *pImport,
        mdTypeDef   tdImport,
        IMetaDataAssemblyEmit *pAssemEmit,
        mdTypeRef   *ptr)
{
    HRESULT hr;
    minipal::com_ptr<IDNMDOwner> assemImport{};

    if (pAssemImport != nullptr)
        RETURN_IF_FAILED(pAssemImport->QueryInterface(IID_IDNMDOwner, (void**)&assemImport));

    minipal::com_ptr<IDNMDOwner> assemEmit{};
    if (pAssemEmit != nullptr)
        RETURN_IF_FAILED(pAssemEmit->QueryInterface(IID_IDNMDOwner, (void**)&assemEmit));

    if (pImport == nullptr)
        return E_INVALIDARG;

    minipal::com_ptr<IDNMDOwner> import{};
    RETURN_IF_FAILED(pImport->QueryInterface(IID_IDNMDOwner, (void**)&import));

    mdcursor_t originalTypeDef;
    if (!md_token_to_cursor(import->MetaData(), tdImport, &originalTypeDef))
        return CLDB_E_FILE_CORRUPT;

    mdcursor_t importedTypeDef;
    HRESULT logStatus = S_OK;

    RETURN_IF_FAILED(ImportReferenceToTypeDef(
        originalTypeDef,
        MetaDataOrNull(assemImport.p),
        { reinterpret_cast<uint8_t const*>(pbHashValue), cbHashValue },
        MetaDataOrNull(assemEmit.p),
        MetaData(),
        false,
        [&](mdcursor_t row)
        {
            if (SUCCEEDED(logStatus) && _md_ptr.UpdateMode() == MDUpdateENC)
            {
                mdToken token;
                logStatus = md_cursor_to_token(row, &token) ? LogToken(token) : CLDB_E_FILE_CORRUPT;
            }
        },
        &importedTypeDef
    ));
    RETURN_IF_FAILED(logStatus);

    if (!md_cursor_to_token(importedTypeDef, ptr))
        return E_FAIL;

    return S_OK;
}

HRESULT MetadataEmit::DefineMemberRef(
        mdToken     tkImport,
        LPCWSTR     szName,
        PCCOR_SIGNATURE pvSigBlob,
        ULONG       cbSigBlob,
        mdMemberRef *pmr)
{
    if (IsNilToken(tkImport))
        tkImport = MD_GLOBAL_PARENT_TOKEN;

    pal::StringConvert<WCHAR, char> cvt(szName);
    if (!cvt.Success())
        return E_INVALIDARG;
    char const* name = cvt;

    if (CheckDuplicates(MDDupMemberRef))
    {
        if (cbSigBlob != 0 && pvSigBlob == nullptr)
            return E_INVALIDARG;
        HRESULT hr = FindExisting(mdtid_MemberRef, DuplicateNameHash(name), [&](mdcursor_t row)
        {
            HRESULT match = MatchToken(row, mdtMemberRef_Class, tkImport);
            if (match != S_OK)
                return match;
            match = MatchString(row, mdtMemberRef_Name, name);
            return match == S_OK ? MatchBlob(row, mdtMemberRef_Signature, pvSigBlob, cbSigBlob) : match;
        }, pmr);
        if (hr == S_OK)
            return META_S_DUPLICATE;
        if (FAILED(hr))
            return hr;
    }

    md_added_row_t c;
    if (!md_append_row(MetaData(), mdtid_MemberRef, &c))
        return E_FAIL;

    if (!md_set_column_value_as_token(c, mdtMemberRef_Class, tkImport))
        return E_FAIL;

    if (!md_set_column_value_as_utf8(c, mdtMemberRef_Name, name))
        return E_FAIL;

    uint8_t const* sig = (uint8_t const*)pvSigBlob;
    uint32_t sigLength = cbSigBlob;
    if (!md_set_column_value_as_blob(c, mdtMemberRef_Signature, sig, sigLength))
        return E_FAIL;

    if (!md_cursor_to_token(c, pmr))
        return E_FAIL;

    return LogToken(*pmr);
}

HRESULT MetadataEmit::DefineImportMember(
        IMetaDataAssemblyImport *pAssemImport,
        void const  *pbHashValue,
        ULONG       cbHashValue,
        IMetaDataImport *pImport,
        mdToken     mbMember,
        IMetaDataAssemblyEmit *pAssemEmit,
        mdToken     tkParent,
        mdMemberRef *pmr)
{
    return ::DefineImportMember(
        this,
        pAssemImport,
        pbHashValue,
        cbHashValue,
        pImport,
        mbMember,
        pAssemEmit,
        tkParent,
        pmr);
}

HRESULT MetadataEmit::AddMethodSemantic(mdcursor_t parent, CorMethodSemanticsAttr semantic, mdMethodDef method)
{
    md_added_row_t addMethodSemantic;
    if (!md_append_row(MetaData(), mdtid_MethodSemantics, &addMethodSemantic))
        return E_FAIL;

    if (!md_set_column_value_as_cursor(addMethodSemantic, mdtMethodSemantics_Association, parent))
        return E_FAIL;

    uint32_t semantics = semantic;
    if (!md_set_column_value_as_constant(addMethodSemantic, mdtMethodSemantics_Semantics, semantics))
        return E_FAIL;

    if (!md_set_column_value_as_token(addMethodSemantic, mdtMethodSemantics_Method, method))
        return E_FAIL;

    return LogRow(addMethodSemantic);
}

namespace
{
    uint32_t ParentRowKey(mdtable_id_t table, mdToken parent)
    {
        switch (table)
        {
        case mdtid_ClassLayout:
        case mdtid_EventMap:
        case mdtid_FieldLayout:
        case mdtid_FieldRva:
        case mdtid_PropertyMap:
            return RidFromToken(parent);
        default:
            return parent;
        }
    }

    HRESULT DeleteParentedToken(mdhandle_t md, mdToken parent, mdtable_id_t childTable, col_index_t parentColumn,
                               mdcursor_t* deletedRow = nullptr)
    {
        mdcursor_t c;
        uint32_t count;
        if (!md_create_cursor(md, childTable, &c, &count))
            return CLDB_E_RECORD_NOTFOUND;

        if (!md_find_row_from_cursor(c, parentColumn, ParentRowKey(childTable, parent), &c))
            return CLDB_E_RECORD_NOTFOUND;

        mdToken nilParent = TokenFromRid(0, TypeFromToken(parent));
        if (!md_set_column_value_as_token(c, parentColumn, nilParent))
            return E_FAIL;

        mdcursor_t parentCursor;
        if (!md_token_to_cursor(md, parent, &parentCursor))
            return CLDB_E_FILE_CORRUPT;
        if (deletedRow != nullptr)
            *deletedRow = c;
        return S_OK;
    }

    HRESULT RemoveFlag(mdhandle_t md, mdToken tk, col_index_t flagsColumn, uint32_t flagToRemove)
    {
        mdcursor_t c;
        if (!md_token_to_cursor(md, tk, &c))
            return CLDB_E_FILE_CORRUPT;

        uint32_t flags;
        if (!md_get_column_value_as_constant(c, flagsColumn, &flags))
            return E_FAIL;

        flags &= ~flagToRemove;
        if (!md_set_column_value_as_constant(c, flagsColumn, flags))
            return E_FAIL;

        return S_OK;
    }

    HRESULT AddFlag(mdhandle_t md, mdToken tk, col_index_t flagsColumn, uint32_t flagToAdd)
    {
        mdcursor_t c;
        if (!md_token_to_cursor(md, tk, &c))
            return CLDB_E_FILE_CORRUPT;

        uint32_t flags;
        if (!md_get_column_value_as_constant(c, flagsColumn, &flags))
            return E_FAIL;

        flags |= flagToAdd;
        if (!md_set_column_value_as_constant(c, flagsColumn, flags))
            return E_FAIL;

        return S_OK;
    }

    template<typename T>
    HRESULT FindOrCreateParentedRow(mdhandle_t md, mdToken parent, mdtable_id_t childTable, col_index_t parentCol,
                                   T const& setTableData, mdcursor_t* updatedRow = nullptr, bool* created = nullptr)
    {
        HRESULT hr;
        mdcursor_t c;
        md_added_row_t addedRow{ mdcursor_t{} };
        uint32_t count;
        bool isNew = false;
        if (!md_create_cursor(md, childTable, &c, &count)
            || !md_find_row_from_cursor(c, parentCol, ParentRowKey(childTable, parent), &c))
        {
            if (!md_append_row(md, childTable, &addedRow))
                return E_FAIL;

            if (!md_set_column_value_as_token(addedRow, parentCol, parent))
                return E_FAIL;
            c = addedRow;
            isNew = true;
        }
        if (created != nullptr)
            *created = isNew;
        RETURN_IF_FAILED(setTableData(c));

        if (updatedRow != nullptr)
            *updatedRow = c;
        return S_OK;
    }
}

HRESULT MetadataEmit::DefineEvent(
        mdTypeDef   td,
        LPCWSTR     szEvent,
        DWORD       dwEventFlags,
        mdToken     tkEventType,
        mdMethodDef mdAddOn,
        mdMethodDef mdRemoveOn,
        mdMethodDef mdFire,
        mdMethodDef rmdOtherMethods[],
        mdEvent     *pmdEvent)
{
    assert(TypeFromToken(td) == mdtTypeDef && td != mdTypeDefNil);
    assert(IsNilToken(tkEventType) || TypeFromToken(tkEventType) == mdtTypeDef ||
                TypeFromToken(tkEventType) == mdtTypeRef || TypeFromToken(tkEventType) == mdtTypeSpec);
    assert(IsNilToken(mdAddOn) || TypeFromToken(mdAddOn) == mdtMethodDef);
    assert(IsNilToken(mdRemoveOn) || TypeFromToken(mdRemoveOn) == mdtMethodDef);
    assert(IsNilToken(mdFire) || TypeFromToken(mdFire) == mdtMethodDef);
    assert(szEvent && pmdEvent);

    pal::StringConvert<WCHAR, char> cvt(szEvent);
    if (!cvt.Success())
        return E_INVALIDARG;

    char const* name = cvt;

    bool mapCreated = false;
    return FindOrCreateParentedRow(MetaData(), td, mdtid_EventMap, mdtEventMap_Parent, [=, &mapCreated](mdcursor_t c)
    {
        HRESULT hr;
        if (mapCreated)
            RETURN_IF_FAILED(LogRow(c));

        // TODO: Check for duplicates
        md_added_row_t addedEvent;
        if (!md_add_new_row_to_list(c, mdtEventMap_EventList, &addedEvent))
            return E_FAIL;

        if (!md_set_column_value_as_utf8(addedEvent, mdtEvent_Name, name))
            return E_FAIL;

        uint32_t flags = dwEventFlags;
        if (!md_set_column_value_as_constant(addedEvent, mdtEvent_EventFlags, flags))
            return E_FAIL;

        if (!md_set_column_value_as_token(addedEvent, mdtEvent_EventType, tkEventType))
            return E_FAIL;

        if (!md_cursor_to_token(addedEvent, pmdEvent))
            return E_FAIL;
        RETURN_IF_FAILED(LogRow(c, ENCEventCreate));
        RETURN_IF_FAILED(LogToken(*pmdEvent));

        if (mdAddOn != mdMethodDefNil)
        {
            RETURN_IF_FAILED(AddMethodSemantic(addedEvent, msAddOn, mdAddOn));
        }

        if (mdRemoveOn != mdMethodDefNil)
        {
            RETURN_IF_FAILED(AddMethodSemantic(addedEvent, msRemoveOn, mdRemoveOn));
        }

        if (mdFire != mdMethodDefNil)
        {
            RETURN_IF_FAILED(AddMethodSemantic(addedEvent, msFire, mdFire));
        }

        if (rmdOtherMethods != nullptr)
        {
            for (size_t i = 0; !IsNilToken(rmdOtherMethods[i]); i++)
            {
                RETURN_IF_FAILED(AddMethodSemantic(addedEvent, msOther, rmdOtherMethods[i]));
            }
        }

        return S_OK;
    }, nullptr, &mapCreated);
}

HRESULT MetadataEmit::SetClassLayout(
        mdTypeDef   td,
        DWORD       dwPackSize,
        COR_FIELD_OFFSET rFieldOffsets[],
        ULONG       ulClassSize)
{
    HRESULT hr;
    assert(TypeFromToken(td) == mdtTypeDef);

    if (rFieldOffsets != nullptr)
    {
        for (size_t i = 0; rFieldOffsets[i].ridOfField != mdFieldDefNil; ++i)
        {
            if (rFieldOffsets[i].ulOffset != UINT32_MAX)
            {
                mdToken field = TokenFromRid(rFieldOffsets[i].ridOfField, mdtFieldDef);
                uint32_t offset = rFieldOffsets[i].ulOffset;
                mdcursor_t fieldLayout;
                RETURN_IF_FAILED(FindOrCreateParentedRow(MetaData(), field, mdtid_FieldLayout, mdtFieldLayout_Field, [=](mdcursor_t c)
                {
                    if (!md_set_column_value_as_constant(c, mdtFieldLayout_Offset, offset))
                        return E_FAIL;

                    return S_OK;
                }, &fieldLayout));
                RETURN_IF_FAILED(LogRow(fieldLayout));
            }
        }
    }

    mdcursor_t classLayout;
    RETURN_IF_FAILED(FindOrCreateParentedRow(MetaData(), td, mdtid_ClassLayout, mdtClassLayout_Parent, [=](mdcursor_t c)
    {
        uint32_t packSize = (uint32_t)dwPackSize;
        if (!md_set_column_value_as_constant(c, mdtClassLayout_PackingSize, packSize))
            return E_FAIL;

        uint32_t classSize = (uint32_t)ulClassSize;
        if (!md_set_column_value_as_constant(c, mdtClassLayout_ClassSize, classSize))
            return E_FAIL;

        return S_OK;
    }, &classLayout));

    return LogRow(classLayout);
}

HRESULT MetadataEmit::DeleteClassLayout(
        mdTypeDef   td)
{
    assert(TypeFromToken(td) == mdtTypeDef);
    HRESULT hr;
    mdcursor_t c;
    uint32_t count;
    if (!md_create_cursor(MetaData(), mdtid_ClassLayout, &c, &count))
        return CLDB_E_RECORD_NOTFOUND;

    if (!md_find_row_from_cursor(c, mdtClassLayout_Parent, RidFromToken(td), &c))
        return CLDB_E_RECORD_NOTFOUND;

    RETURN_IF_FAILED(DeleteParentedToken(MetaData(), td, mdtid_ClassLayout, mdtClassLayout_Parent));
    RETURN_IF_FAILED(LogRow(c));

    // Now that we've deleted the class layout entry,
    // we need to delete the field layout entries for the fields of the type.
    mdcursor_t type;
    if (!md_token_to_cursor(MetaData(), td, &type))
        return CLDB_E_FILE_CORRUPT;

    mdcursor_t field;
    uint32_t fieldCount;
    if (!md_get_column_value_as_range(type, mdtTypeDef_FieldList, &field, &fieldCount))
        return S_OK;

    for (uint32_t i = 0; i < fieldCount; ++i, md_cursor_next(&field))
    {
        mdcursor_t resolvedField;
        if (!md_resolve_indirect_cursor(field, &resolvedField))
            return E_FAIL;

        mdToken fieldToken;
        if (!md_cursor_to_token(resolvedField, &fieldToken))
            return E_FAIL;

        mdcursor_t removedFieldLayout;
        hr = DeleteParentedToken(MetaData(), fieldToken, mdtid_FieldLayout, mdtFieldLayout_Field, &removedFieldLayout);

        // If we couldn't find the field layout entry, that's fine.
        // If we hit another error, return that error.
        if (hr == CLDB_E_RECORD_NOTFOUND)
            continue;
        RETURN_IF_FAILED(hr);
        RETURN_IF_FAILED(LogRow(removedFieldLayout));
    }

    return S_OK;
}

HRESULT MetadataEmit::SetFieldMarshal(
        mdToken     tk,
        PCCOR_SIGNATURE pvNativeType,
        ULONG       cbNativeType)
{
    HRESULT hr;
    mdcursor_t parent;
    if (!md_token_to_cursor(MetaData(), tk, &parent))
        return CLDB_E_FILE_CORRUPT;

    col_index_t col = TypeFromToken(tk) == mdtFieldDef ? mdtField_Flags : mdtParam_Flags;
    uint32_t flagToAdd = TypeFromToken(tk) == mdtFieldDef ? (uint32_t)fdHasFieldMarshal : (uint32_t)pdHasFieldMarshal;
    uint32_t flags;
    if (!md_get_column_value_as_constant(parent, col, &flags))
        return E_FAIL;

    flags |= flagToAdd;
    if (!md_set_column_value_as_constant(parent, col, flags))
        return E_FAIL;

    mdcursor_t marshal;
    RETURN_IF_FAILED(FindOrCreateParentedRow(MetaData(), tk, mdtid_FieldMarshal, mdtFieldMarshal_Parent, [=](mdcursor_t c)
    {
        uint8_t const* sig = (uint8_t const*)pvNativeType;
        uint32_t sigLength = cbNativeType;
        if (!md_set_column_value_as_blob(c, mdtFieldMarshal_NativeType, sig, sigLength))
            return E_FAIL;

        return S_OK;
    }, &marshal));

    RETURN_IF_FAILED(LogToken(tk));
    return LogRow(marshal);
}

HRESULT MetadataEmit::DeleteFieldMarshal(
        mdToken     tk)
{
    HRESULT hr;
    mdcursor_t removedMarshal;
    assert(TypeFromToken(tk) == mdtFieldDef || TypeFromToken(tk) == mdtParamDef);
    assert(!IsNilToken(tk));

    RETURN_IF_FAILED(DeleteParentedToken(
        MetaData(),
        tk,
        mdtid_FieldMarshal,
        mdtFieldMarshal_Parent,
        &removedMarshal));
    RETURN_IF_FAILED(LogRow(removedMarshal));

    RETURN_IF_FAILED(RemoveFlag(
        MetaData(),
        tk,
        TypeFromToken(tk) == mdtFieldDef ? mdtField_Flags : mdtParam_Flags,
        TypeFromToken(tk) == mdtFieldDef ? (uint32_t)fdHasFieldMarshal : (uint32_t)pdHasFieldMarshal));
    return LogToken(tk);
}

HRESULT MetadataEmit::DefinePermissionSet(
        mdToken     tk,
        DWORD       dwAction,
        void const  *pvPermission,
        ULONG       cbPermission,
        mdPermission *ppm)
{
    HRESULT hr;
    assert(TypeFromToken(tk) == mdtTypeDef || TypeFromToken(tk) == mdtMethodDef ||
             TypeFromToken(tk) == mdtAssembly);

    if (CheckDuplicates(MDDupPermission))
    {
        mdPermission existing;
        HRESULT hr = FindExisting(mdtid_DeclSecurity, std::hash<mdToken>{}(tk), [&](mdcursor_t row)
        {
            HRESULT match = MatchToken(row, mdtDeclSecurity_Parent, tk);
            return match == S_OK ? MatchConstant(row, mdtDeclSecurity_Action, dwAction) : match;
        }, &existing);
        if (hr == S_OK)
        {
            if (ppm != nullptr)
                *ppm = existing;
            return META_S_DUPLICATE;
        }
        if (FAILED(hr))
            return hr;
    }

    md_added_row_t c;
    if (!md_append_row(MetaData(), mdtid_DeclSecurity, &c))
        return E_FAIL;

    if (!md_set_column_value_as_token(c, mdtDeclSecurity_Parent, tk))
        return E_FAIL;

    if (TypeFromToken(tk) == mdtTypeDef
        || TypeFromToken(tk) == mdtMethodDef)
    {
        uint32_t flagToAdd = TypeFromToken(tk) == mdtTypeDef ? (uint32_t)tdHasSecurity : (uint32_t)mdHasSecurity;
        col_index_t flagsCol = TypeFromToken(tk) == mdtTypeDef ? mdtTypeDef_Flags : mdtMethodDef_Flags;

        mdcursor_t parent;
        if (!md_get_column_value_as_cursor(c, mdtDeclSecurity_Parent, &parent))
            return E_FAIL;

        uint32_t flags;
        if (!md_get_column_value_as_constant(parent, flagsCol, &flags))
            return E_FAIL;

        flags |= flagToAdd;

        if (!md_set_column_value_as_constant(parent, flagsCol, flags))
            return E_FAIL;
        RETURN_IF_FAILED(LogToken(tk));
    }

    uint32_t action = dwAction;
    if (!md_set_column_value_as_constant(c, mdtDeclSecurity_Action, action))
        return E_FAIL;

    uint8_t const* permission = (uint8_t const*)pvPermission;
    uint32_t permissionLength = cbPermission;
    if (!md_set_column_value_as_blob(c, mdtDeclSecurity_PermissionSet, permission, permissionLength))
        return E_FAIL;

    if (!md_cursor_to_token(c, ppm))
        return E_FAIL;

    return LogToken(*ppm);
}

HRESULT MetadataEmit::SetRVA(
        mdMethodDef md,
        ULONG       ulRVA)
{
    mdcursor_t method;
    if (!md_token_to_cursor(MetaData(), md, &method))
        return CLDB_E_FILE_CORRUPT;

    uint32_t rva = ulRVA;
    if (!md_set_column_value_as_constant(method, mdtMethodDef_Rva, rva))
        return E_FAIL;

    return LogToken(md);
}

HRESULT MetadataEmit::GetTokenFromSig(
        PCCOR_SIGNATURE pvSig,
        ULONG       cbSig,
        mdSignature *pmsig)
{
    if (CheckDuplicates(MDDupSignature))
    {
        if (cbSig != 0 && pvSig == nullptr)
            return E_INVALIDARG;
        HRESULT hr = FindExisting(mdtid_StandAloneSig, DuplicateHash(pvSig, cbSig),
            [&](mdcursor_t row) { return MatchBlob(row, mdtStandAloneSig_Signature, pvSig, cbSig); }, pmsig);
        if (hr == S_OK)
            return META_S_DUPLICATE;
        if (FAILED(hr))
            return hr;
    }

    md_added_row_t c;
    if (!md_append_row(MetaData(), mdtid_StandAloneSig, &c))
        return E_FAIL;

    uint32_t sigLength = cbSig;
    if (!md_set_column_value_as_blob(c, mdtStandAloneSig_Signature, pvSig, sigLength))
        return E_FAIL;

    if (!md_cursor_to_token(c, pmsig))
        return CLDB_E_FILE_CORRUPT;

    return LogToken(*pmsig);
}

HRESULT MetadataEmit::DefineModuleRef(
        LPCWSTR     szName,
        mdModuleRef *pmur)
{
    pal::StringConvert<WCHAR, char> cvt(szName);
    if (!cvt.Success())
        return E_INVALIDARG;
    char const* name = cvt;

    if (CheckDuplicates(MDDupModuleRef))
    {
        HRESULT hr = FindExisting(mdtid_ModuleRef, DuplicateNameHash(name),
            [&](mdcursor_t row) { return MatchString(row, mdtModuleRef_Name, name); }, pmur);
        if (hr == S_OK)
            return META_S_DUPLICATE;
        if (FAILED(hr))
            return hr;
    }

    md_added_row_t c;
    if (!md_append_row(MetaData(), mdtid_ModuleRef, &c))
        return E_FAIL;

    if (!md_set_column_value_as_utf8(c, mdtModuleRef_Name, name))
        return E_FAIL;

    if (!md_cursor_to_token(c, pmur))
        return CLDB_E_FILE_CORRUPT;

    return LogToken(*pmur);
}


HRESULT MetadataEmit::SetParent(
        mdMemberRef mr,
        mdToken     tk)
{
    mdcursor_t c;
    if (!md_token_to_cursor(MetaData(), mr, &c))
        return CLDB_E_FILE_CORRUPT;

    if (!md_set_column_value_as_token(c, mdtMemberRef_Class, tk))
        return E_FAIL;

    return LogToken(mr);
}

HRESULT MetadataEmit::GetTokenFromTypeSpec(
        PCCOR_SIGNATURE pvSig,
        ULONG       cbSig,
        mdTypeSpec *ptypespec)
{
    if (CheckDuplicates(MDDupTypeSpec))
    {
        if (cbSig != 0 && pvSig == nullptr)
            return E_INVALIDARG;
        HRESULT hr = FindExisting(mdtid_TypeSpec, DuplicateHash(pvSig, cbSig),
            [&](mdcursor_t row) { return MatchBlob(row, mdtTypeSpec_Signature, pvSig, cbSig); }, ptypespec);
        if (hr == S_OK)
            return S_OK;
        if (FAILED(hr))
            return hr;
    }

    md_added_row_t c;
    if (!md_append_row(MetaData(), mdtid_TypeSpec, &c))
        return E_FAIL;

    uint32_t sigLength = cbSig;
    if (!md_set_column_value_as_blob(c, mdtTypeSpec_Signature, pvSig, sigLength))
        return E_FAIL;

    if (!md_cursor_to_token(c, ptypespec))
        return CLDB_E_FILE_CORRUPT;

    return LogToken(*ptypespec);
}

HRESULT MetadataEmit::SaveToMemory(
        void        *pbData,
        ULONG       cbData)
{
    size_t saveSize = cbData;
    return md_write_to_buffer(MetaData(), (uint8_t*)pbData, &saveSize) ? S_OK : E_OUTOFMEMORY;
}

HRESULT MetadataEmit::DefineUserString(
        LPCWSTR szString,
        ULONG       cchString,
        mdString    *pstk)
{
    std::unique_ptr<char16_t[]> pString{ new char16_t[cchString + 1] };
    std::memcpy(pString.get(), szString, cchString * sizeof(char16_t));
    pString[cchString] = u'\0';

    mduserstringcursor_t c = md_add_userstring_to_heap(MetaData(), pString.get());

    if (c == 0)
        return E_FAIL;

    if ((c & 0xff000000) != 0)
        return META_E_STRINGSPACE_FULL;

    *pstk = TokenFromRid((mdString)c, mdtString);
    return S_OK;
}

HRESULT MetadataEmit::DeleteToken(
        mdToken     tkObj)
{
    mdcursor_t c;
    if (!md_token_to_cursor(MetaData(), tkObj, &c))
        return E_INVALIDARG;

    char const* deletedName = COR_DELETED_NAME_A;
    switch (TypeFromToken(tkObj))
    {
        case mdtTypeDef:
        {
            _duplicateIndexes.erase(mdtid_TypeDef);
            if (!md_set_column_value_as_utf8(c, mdtTypeDef_TypeName, deletedName))
                return E_FAIL;
            HRESULT hr = AddFlag(MetaData(), tkObj, mdtTypeDef_Flags, tdSpecialName | tdRTSpecialName);
            return FAILED(hr) ? hr : LogToken(tkObj);
        }
        case mdtMethodDef:
        {
            if (!md_set_column_value_as_utf8(c, mdtMethodDef_Name, deletedName))
                return E_FAIL;
            HRESULT hr = AddFlag(MetaData(), tkObj, mdtMethodDef_Flags, mdSpecialName | mdRTSpecialName);
            return FAILED(hr) ? hr : LogToken(tkObj);
        }
        case mdtFieldDef:
        {
            if (!md_set_column_value_as_utf8(c, mdtField_Name, deletedName))
                return E_FAIL;
            HRESULT hr = AddFlag(MetaData(), tkObj, mdtField_Flags, fdSpecialName | fdRTSpecialName);
            return FAILED(hr) ? hr : LogToken(tkObj);
        }
        case mdtEvent:
        {
            if (!md_set_column_value_as_utf8(c, mdtEvent_Name, deletedName))
                return E_FAIL;
            HRESULT hr = AddFlag(MetaData(), tkObj, mdtEvent_EventFlags, evSpecialName | evRTSpecialName);
            return FAILED(hr) ? hr : LogToken(tkObj);
        }
        case mdtProperty:
        {
            if (!md_set_column_value_as_utf8(c, mdtProperty_Name, deletedName))
                return E_FAIL;
            HRESULT hr = AddFlag(MetaData(), tkObj, mdtProperty_Flags, prSpecialName | prRTSpecialName);
            return FAILED(hr) ? hr : LogToken(tkObj);
        }
        case mdtExportedType:
        {
            _duplicateIndexes.erase(mdtid_ExportedType);
            if (!md_set_column_value_as_utf8(c, mdtExportedType_TypeName, deletedName))
                return E_FAIL;
            return LogToken(tkObj);
        }
        case mdtCustomAttribute:
        {
            mdToken parent;
            if (!md_get_column_value_as_token(c, mdtCustomAttribute_Parent, &parent))
                return E_FAIL;

            // Change the parent to the nil token.
            parent = TokenFromRid(mdTokenNil, TypeFromToken(parent));

            if (!md_set_column_value_as_token(c, mdtCustomAttribute_Parent, parent))
                return E_FAIL;

            return LogToken(tkObj);
        }
        case mdtGenericParam:
        {
            mdToken parent;
            if (!md_get_column_value_as_token(c, mdtGenericParam_Owner, &parent))
                return E_FAIL;

            // Change the parent to the nil token.
            parent = TokenFromRid(mdTokenNil, TypeFromToken(parent));

            if (!md_set_column_value_as_token(c, mdtGenericParam_Owner, parent))
                return E_FAIL;

            return LogToken(tkObj);
        }
        case mdtGenericParamConstraint:
        {
            mdToken parent = mdGenericParamNil;
            if (!md_set_column_value_as_token(c, mdtGenericParamConstraint_Owner, parent))
                return E_FAIL;

            return LogToken(tkObj);
        }
        case mdtPermission:
        {
            mdToken parent;
            if (!md_get_column_value_as_token(c, mdtDeclSecurity_Parent, &parent))
                return E_FAIL;

            // Change the parent to the nil token.
            mdToken originalParent = parent;
            parent = TokenFromRid(mdTokenNil, TypeFromToken(parent));

            if (!md_set_column_value_as_token(c, mdtDeclSecurity_Parent, parent))
                return E_FAIL;

            if (TypeFromToken(originalParent) == mdtAssembly)
            {
                // There is no HasSecurity flag for an assembly, so we're done.
                return LogToken(tkObj);
            }

            mdcursor_t permissions;
            uint32_t numPermissions;
            if (!md_create_cursor(MetaData(), mdtid_DeclSecurity, &permissions, &numPermissions))
                return E_FAIL;

            // If we have no more permissions for this parent, remove the HasSecurity bit.
            // Since we just need to know if there's any matching row and we don't need a range of rows,
            // we can use find_row instead of find_range.
            if (!md_find_row_from_cursor(permissions, mdtDeclSecurity_Parent, originalParent, &permissions))
            {
                HRESULT hr = RemoveFlag(
                    MetaData(),
                    originalParent,
                    TypeFromToken(originalParent) == mdtTypeDef ? mdtTypeDef_Flags : mdtMethodDef_Flags,
                    TypeFromToken(originalParent) == mdtTypeDef ? (uint32_t)tdHasSecurity : (uint32_t)mdHasSecurity);
                if (FAILED(hr))
                    return hr;
                hr = LogToken(originalParent);
                if (FAILED(hr))
                    return hr;
            }

            return LogToken(tkObj);
        }
        default:
            break;
    }
    return E_INVALIDARG;
}

HRESULT MetadataEmit::SetMethodProps(
        mdMethodDef md,
        DWORD       dwMethodFlags,
        ULONG       ulCodeRVA,
        DWORD       dwImplFlags)
{
    mdcursor_t c;
    if (!md_token_to_cursor(MetaData(), md, &c))
        return CLDB_E_FILE_CORRUPT;

    if (dwMethodFlags != std::numeric_limits<DWORD>::max())
    {
        // TODO: Strip the reserved flags from user input and preserve the existing reserved flags.
        uint32_t flags = dwMethodFlags;
        if (!md_set_column_value_as_constant(c, mdtMethodDef_Flags, flags))
            return E_FAIL;
    }

    if (ulCodeRVA != std::numeric_limits<ULONG>::max())
    {
        uint32_t rva = ulCodeRVA;
        if (!md_set_column_value_as_constant(c, mdtMethodDef_Rva, rva))
            return E_FAIL;
    }

    if (dwImplFlags != std::numeric_limits<DWORD>::max())
    {
        uint32_t implFlags = dwImplFlags;
        if (!md_set_column_value_as_constant(c, mdtMethodDef_ImplFlags, implFlags))
            return E_FAIL;
    }

    return LogToken(md);
}

HRESULT MetadataEmit::SetTypeDefProps(
        mdTypeDef   td,
        DWORD       dwTypeDefFlags,
        mdToken     tkExtends,
        mdToken     rtkImplements[])
{
    HRESULT hr;
    mdcursor_t c;
    if (!md_token_to_cursor(MetaData(), td, &c))
        return CLDB_E_FILE_CORRUPT;

    if (dwTypeDefFlags != std::numeric_limits<DWORD>::max())
    {
        // TODO: Strip the reserved flags from user input and preserve the existing reserved flags.
        uint32_t flags = dwTypeDefFlags;
        if (!md_set_column_value_as_constant(c, mdtTypeDef_Flags, flags))
            return E_FAIL;
    }

    if (tkExtends != std::numeric_limits<uint32_t>::max())
    {
        if (IsNilToken(tkExtends))
            tkExtends = mdTypeDefNil;

        if (!md_set_column_value_as_token(c, mdtTypeDef_Extends, tkExtends))
            return E_FAIL;
    }

    if (rtkImplements)
    {
        // First null-out the Class columns of the current implementations.
        // We can't delete here as we hand out tokens into this table to the caller.
        // This would be much more efficient if we could delete rows, as nulling out the parent will almost assuredly make the column
        // unsorted.
        mdcursor_t interfaceImplCursor;
        uint32_t numInterfaceImpls;
        if (md_create_cursor(MetaData(), mdtid_InterfaceImpl, &interfaceImplCursor, &numInterfaceImpls)
            && md_find_range_from_cursor(interfaceImplCursor, mdtInterfaceImpl_Class, RidFromToken(td), &interfaceImplCursor, &numInterfaceImpls) != MD_RANGE_NOT_FOUND)
        {
            for (uint32_t i = 0; i < numInterfaceImpls; ++i, md_cursor_next(&interfaceImplCursor))
            {
                mdToken parent;
                if (!md_get_column_value_as_token(interfaceImplCursor, mdtInterfaceImpl_Class, &parent))
                    return E_FAIL;

                // If getting a range was unsupported, then we're doing a whole table scan here.
                // In that case, we can't assume that we've already validated the parent.
                // Update it here.
                if (parent == td)
                {
                    mdToken newParent = mdTypeDefNil;
                    if (!md_set_column_value_as_token(interfaceImplCursor, mdtInterfaceImpl_Class, newParent))
                        return E_FAIL;
                    mdToken token;
                    if (!md_cursor_to_token(interfaceImplCursor, &token))
                        return CLDB_E_FILE_CORRUPT;
                    RETURN_IF_FAILED(LogToken(token));
                }
            }
        }

        for (size_t i = 0; !IsNilToken(rtkImplements[i]); ++i)
        {
            md_added_row_t interfaceImpl;
            if (!md_append_row(MetaData(), mdtid_InterfaceImpl, &interfaceImpl))
                return E_FAIL;

            if (!md_set_column_value_as_cursor(interfaceImpl, mdtInterfaceImpl_Class, c))
                return E_FAIL;

            if (!md_set_column_value_as_token(interfaceImpl, mdtInterfaceImpl_Interface, rtkImplements[i]))
                return E_FAIL;
            mdToken token;
            if (!md_cursor_to_token(interfaceImpl, &token))
                return CLDB_E_FILE_CORRUPT;
            RETURN_IF_FAILED(LogToken(token));
        }
    }

    return LogToken(td);
}

HRESULT MetadataEmit::RemoveSemantics(mdToken parent, CorMethodSemanticsAttr semantic)
{
    // Set all rows in the MethodSemantic table with a matching Association column of parent to the nil token of parent's table.
    HRESULT hr;
    mdcursor_t c;
    uint32_t count;
    if (!md_create_cursor(MetaData(), mdtid_MethodSemantics, &c, &count))
        return S_OK;

    md_range_result_t result = md_find_range_from_cursor(c, mdtMethodSemantics_Association, parent, &c, &count);
    if (result == MD_RANGE_NOT_FOUND)
        return S_OK;

    for (uint32_t i = 0; i < count; ++i, md_cursor_next(&c))
    {
        mdToken association;
        if (!md_get_column_value_as_token(c, mdtMethodSemantics_Association, &association))
            return E_FAIL;

        uint32_t recordSemantic;
        if (!md_get_column_value_as_constant(c, mdtMethodSemantics_Semantics, &recordSemantic))
            return E_FAIL;

        if (association == parent && recordSemantic == (uint32_t)semantic)
        {
            association = TokenFromRid(mdTokenNil, TypeFromToken(association));
            if (!md_set_column_value_as_token(c, mdtMethodSemantics_Association, association))
                return E_FAIL;
            RETURN_IF_FAILED(LogRow(c));
        }
    }

    return S_OK;
}

HRESULT MetadataEmit::SetEventProps(
        mdEvent     ev,
        DWORD       dwEventFlags,
        mdToken     tkEventType,
        mdMethodDef mdAddOn,
        mdMethodDef mdRemoveOn,
        mdMethodDef mdFire,
        mdMethodDef rmdOtherMethods[])
{
    HRESULT hr;
    mdcursor_t c;
    if (!md_token_to_cursor(MetaData(), ev, &c))
        return CLDB_E_FILE_CORRUPT;

    if (dwEventFlags != std::numeric_limits<DWORD>::max())
    {
        uint32_t eventFlags = dwEventFlags;
        if (!md_set_column_value_as_constant(c, mdtEvent_EventFlags, eventFlags))
            return E_FAIL;
    }

    if (!IsNilToken(tkEventType))
    {
        if (!md_set_column_value_as_token(c, mdtEvent_EventType, tkEventType))
            return E_FAIL;
    }

    if (!IsNilToken(mdAddOn))
    {
        RETURN_IF_FAILED(RemoveSemantics(ev, msAddOn));
        RETURN_IF_FAILED(AddMethodSemantic(c, msAddOn, mdAddOn));
    }

    if (!IsNilToken(mdRemoveOn))
    {
        RETURN_IF_FAILED(RemoveSemantics(ev, msRemoveOn));
        RETURN_IF_FAILED(AddMethodSemantic(c, msRemoveOn, mdRemoveOn));
    }

    if (!IsNilToken(mdFire))
    {
        RETURN_IF_FAILED(RemoveSemantics(ev, msFire));
        RETURN_IF_FAILED(AddMethodSemantic(c, msFire, mdFire));
    }

    if (rmdOtherMethods)
    {
        RETURN_IF_FAILED(RemoveSemantics(ev, msOther));
        for (size_t i = 0; rmdOtherMethods[i] != mdMethodDefNil; ++i)
        {
            RETURN_IF_FAILED(AddMethodSemantic(c, msOther, rmdOtherMethods[i]));
        }
    }

    return LogToken(ev);
}

HRESULT MetadataEmit::SetPermissionSetProps(
        mdToken     tk,
        DWORD       dwAction,
        void const  *pvPermission,
        ULONG       cbPermission,
        mdPermission *ppm)
{
    assert(TypeFromToken(tk) == mdtTypeDef || TypeFromToken(tk) == mdtMethodDef ||
        TypeFromToken(tk) == mdtAssembly);

    if (dwAction == UINT32_MAX || dwAction == 0 || dwAction > dclMaximumValue)
        return E_INVALIDARG;

    mdcursor_t c;
    uint32_t count;
    if (!md_create_cursor(MetaData(), mdtid_DeclSecurity, &c, &count))
        return CLDB_E_RECORD_NOTFOUND;

    if (!md_find_row_from_cursor(c, mdtDeclSecurity_Parent, tk, &c))
        return CLDB_E_RECORD_NOTFOUND;

    uint32_t action = dwAction;
    if (!md_set_column_value_as_constant(c, mdtDeclSecurity_Action, action))
        return E_FAIL;

    uint8_t const* permission = (uint8_t const*)pvPermission;
    uint32_t permissionLength = cbPermission;
    if (!md_set_column_value_as_blob(c, mdtDeclSecurity_PermissionSet, permission, permissionLength))
        return E_FAIL;

    if (!md_cursor_to_token(c, ppm))
        return CLDB_E_FILE_CORRUPT;

    return LogToken(*ppm);
}

HRESULT MetadataEmit::DefinePinvokeMap(
        mdToken     tk,
        DWORD       dwMappingFlags,
        LPCWSTR     szImportName,
        mdModuleRef mrImportDLL)
{
    HRESULT hr;
    mdcursor_t c;
    if (!md_token_to_cursor(MetaData(), tk, &c))
        return CLDB_E_FILE_CORRUPT;

    if (TypeFromToken(tk) == mdtMethodDef)
    {
        RETURN_IF_FAILED(AddFlag(MetaData(), tk, mdtMethodDef_Flags, mdPinvokeImpl));
    }
    else if (TypeFromToken(tk) == mdtFieldDef)
    {
        RETURN_IF_FAILED(AddFlag(MetaData(), tk, mdtField_Flags, fdPinvokeImpl));
    }
    if (_md_ptr.UpdateMode() == MDUpdateENC)
    {
        mdcursor_t existing;
        uint32_t count;
        if (md_create_cursor(MetaData(), mdtid_ImplMap, &existing, &count)
            && md_find_row_from_cursor(existing, mdtImplMap_MemberForwarded, tk, &existing))
        {
            RETURN_IF_FAILED(LogToken(tk));
            return SetPinvokeMap(tk, dwMappingFlags, szImportName, mrImportDLL);
        }
    }

    mdcursor_t row_to_edit;
    md_added_row_t added_row_wrapper;

    if (!md_append_row(MetaData(), mdtid_ImplMap, &row_to_edit))
        return E_FAIL;
    added_row_wrapper = md_added_row_t(row_to_edit);

    if (!md_set_column_value_as_token(row_to_edit, mdtImplMap_MemberForwarded, tk))
        return E_FAIL;

    if (dwMappingFlags == std::numeric_limits<uint32_t>::max())
    {
        // Unspecified by the user, set to the default.
        dwMappingFlags = 0;
    }

    uint32_t mappingFlags = dwMappingFlags;
    if (!md_set_column_value_as_constant(row_to_edit, mdtImplMap_MappingFlags, mappingFlags))
        return E_FAIL;

    pal::StringConvert<WCHAR, char> cvt(szImportName);
    char const* name = cvt;
    if (!md_set_column_value_as_utf8(row_to_edit, mdtImplMap_ImportName, name))
        return E_FAIL;

    if (IsNilToken(mrImportDLL))
    {
        // TODO: If the token is nil, create a module ref to "" (if it doesn't exist) and use that.
    }

    if (!md_set_column_value_as_token(row_to_edit, mdtImplMap_ImportScope, mrImportDLL))
        return E_FAIL;

    RETURN_IF_FAILED(LogToken(tk));
    return LogRow(row_to_edit);
}

HRESULT MetadataEmit::SetPinvokeMap(
        mdToken     tk,
        DWORD       dwMappingFlags,
        LPCWSTR     szImportName,
        mdModuleRef mrImportDLL)
{
    mdcursor_t c;
    if (!md_token_to_cursor(MetaData(), tk, &c))
        return CLDB_E_FILE_CORRUPT;

    mdcursor_t implMapCursor;
    uint32_t numImplMaps;
    if (!md_create_cursor(MetaData(), mdtid_ImplMap, &implMapCursor, &numImplMaps))
        return E_FAIL;

    mdcursor_t row_to_edit;
    if (!md_find_row_from_cursor(implMapCursor, mdtImplMap_MemberForwarded, tk, &row_to_edit))
        return CLDB_E_RECORD_NOTFOUND;

    if (dwMappingFlags != std::numeric_limits<uint32_t>::max())
    {
        uint32_t mappingFlags = dwMappingFlags;
        if (!md_set_column_value_as_constant(row_to_edit, mdtImplMap_MappingFlags, mappingFlags))
            return E_FAIL;
    }

    if (szImportName != nullptr)
    {
        pal::StringConvert<WCHAR, char> cvt(szImportName);
        char const* name = cvt;
        if (!md_set_column_value_as_utf8(row_to_edit, mdtImplMap_ImportName, name))
            return E_FAIL;
    }

    if (!md_set_column_value_as_token(row_to_edit, mdtImplMap_ImportScope, mrImportDLL))
        return E_FAIL;

    return LogRow(row_to_edit);
}

HRESULT MetadataEmit::DeletePinvokeMap(
        mdToken     tk)
{
    HRESULT hr;
    mdcursor_t removedImplMap;
    assert(TypeFromToken(tk) == mdtFieldDef || TypeFromToken(tk) == mdtMethodDef);
    assert(!IsNilToken(tk));

    RETURN_IF_FAILED(DeleteParentedToken(
        MetaData(),
        tk,
        mdtid_ImplMap,
        mdtImplMap_MemberForwarded,
        &removedImplMap));
    RETURN_IF_FAILED(LogRow(removedImplMap));

    RETURN_IF_FAILED(RemoveFlag(
        MetaData(),
        tk,
        TypeFromToken(tk) == mdtFieldDef ? mdtField_Flags : mdtMethodDef_Flags,
        TypeFromToken(tk) == mdtFieldDef ? (uint32_t)fdPinvokeImpl : (uint32_t)mdPinvokeImpl));

    return LogToken(tk);
}


HRESULT MetadataEmit::DefineCustomAttribute(
        mdToken     tkOwner,
        mdToken     tkCtor,
        void const  *pCustomAttribute,
        ULONG       cbCustomAttribute,
        mdCustomAttribute *pcv)
{
    if (TypeFromToken(tkOwner) == mdtCustomAttribute)
        return E_INVALIDARG;

    if (IsNilToken(tkOwner)
        || IsNilToken(tkCtor)
        || (TypeFromToken(tkCtor) != mdtMethodDef
            && TypeFromToken(tkCtor) != mdtMemberRef) )
    {
        return E_INVALIDARG;
    }

    // TODO: Recognize pseudoattributes and handle them appropriately.

    // We hand out tokens here, so we can't move rows to keep the parent column sorted.
    md_added_row_t new_row;
    if (!md_append_row(MetaData(), mdtid_CustomAttribute, &new_row))
        return E_FAIL;

    if (!md_set_column_value_as_token(new_row, mdtCustomAttribute_Parent, tkOwner))
        return E_FAIL;

    if (!md_set_column_value_as_token(new_row, mdtCustomAttribute_Type, tkCtor))
        return E_FAIL;

    uint8_t const* pCustomAttributeBlob = (uint8_t const*)pCustomAttribute;
    uint32_t customAttributeBlobLen = cbCustomAttribute;
    if (!md_set_column_value_as_blob(new_row, mdtCustomAttribute_Value, pCustomAttributeBlob, customAttributeBlobLen))
        return E_FAIL;

    if (!md_cursor_to_token(new_row, pcv))
        return CLDB_E_FILE_CORRUPT;

    return LogToken(*pcv);
}

HRESULT MetadataEmit::SetCustomAttributeValue(
        mdCustomAttribute pcv,
        void const  *pCustomAttribute,
        ULONG       cbCustomAttribute)
{
    if (TypeFromToken(pcv) != mdtCustomAttribute)
        return E_INVALIDARG;

    mdcursor_t c;
    if (!md_token_to_cursor(MetaData(), pcv, &c))
        return CLDB_E_FILE_CORRUPT;

    uint8_t const* pCustomAttributeBlob = (uint8_t const*)pCustomAttribute;
    uint32_t customAttributeBlobLen = cbCustomAttribute;
    if (!md_set_column_value_as_blob(c, mdtCustomAttribute_Value, pCustomAttributeBlob, customAttributeBlobLen))
        return E_FAIL;

    return LogToken(pcv);
}

namespace
{
    // Determine the blob size base of the ELEMENT_TYPE_* associated with the blob.
    // This cannot be a table lookup because ELEMENT_TYPE_STRING is an unicode string.
    uint32_t GetSizeOfConstantBlob(
        int32_t  type,
        void const* pValue,
        uint32_t  strLen)
    {
        uint32_t size = 0;

        switch (type)
        {
        case ELEMENT_TYPE_BOOLEAN:
            size = sizeof(bool);
            break;
        case ELEMENT_TYPE_I1:
        case ELEMENT_TYPE_U1:
            size = sizeof(uint8_t);
            break;
        case ELEMENT_TYPE_CHAR:
        case ELEMENT_TYPE_I2:
        case ELEMENT_TYPE_U2:
            size = sizeof(uint16_t);
            break;
        case ELEMENT_TYPE_I4:
        case ELEMENT_TYPE_U4:
        case ELEMENT_TYPE_R4:
            size = sizeof(uint32_t);

            break;

        case ELEMENT_TYPE_I8:
        case ELEMENT_TYPE_U8:
        case ELEMENT_TYPE_R8:
            size = sizeof(uint64_t);
            break;

        case ELEMENT_TYPE_STRING:
            if (pValue == 0)
                size = 0;
            else
            if (strLen != (uint32_t) -1)
                size = strLen * sizeof(WCHAR);
            else
                size = (uint32_t)(sizeof(WCHAR) * minipal_u16_strlen((CHAR16_T*)pValue));
            break;

        case ELEMENT_TYPE_CLASS:
            // The only legal value is a null pointer, and on 32 bit platforms we've already
            // stored 32 bits, so we will use just 32 bits of null.  If the type is
            // E_T_CLASS, the caller should know that the value is always null anyway.
            size = sizeof(uint32_t);
            break;
        default:
            assert(!"Not a valid type to specify default value!");
            break;
        }
        return size;
    }
}

HRESULT MetadataEmit::DefineField(
        mdTypeDef   td,
        LPCWSTR     szName,
        DWORD       dwFieldFlags,
        PCCOR_SIGNATURE pvSigBlob,
        ULONG       cbSigBlob,
        DWORD       dwCPlusTypeFlag,
        void const  *pValue,
        ULONG       cchValue,
        mdFieldDef  *pmd)
{
    HRESULT hr;
    pal::StringConvert<WCHAR, char> cvt(szName);
    if (!cvt.Success())
        return E_INVALIDARG;

    char const* name = cvt;

    md_added_row_t c;
    mdcursor_t typeDef;
    if (!md_token_to_cursor(MetaData(), td, &typeDef))
        return CLDB_E_FILE_CORRUPT;

    if (!md_add_new_row_to_list(typeDef, mdtTypeDef_FieldList, &c))
        return E_FAIL;

    if (!md_set_column_value_as_utf8(c, mdtField_Name, name))
        return E_FAIL;

    bool hasConstant = false;
    // See if there is a Constant.
    if ((dwCPlusTypeFlag != ELEMENT_TYPE_VOID && dwCPlusTypeFlag != ELEMENT_TYPE_END &&
         dwCPlusTypeFlag != UINT32_MAX) &&
        (pValue || (pValue == 0 && (dwCPlusTypeFlag == ELEMENT_TYPE_STRING ||
                                    dwCPlusTypeFlag == ELEMENT_TYPE_CLASS))))
    {
        hasConstant = true;
    }

    if (dwFieldFlags != std::numeric_limits<DWORD>::max())
    {
        // TODO: Handle reserved flags
        uint32_t fieldFlags = dwFieldFlags;

        // If the field name has the special name for enum fields,
        // set the special name and RTSpecialName flags.
        // COMPAT: CoreCLR does not check if the field is actually in an enum type.
        if (strcmp(name, COR_ENUM_FIELD_NAME) == 0)
        {
            fieldFlags |= fdRTSpecialName | fdSpecialName;
        }
        if (!md_set_column_value_as_constant(c, mdtField_Flags, fieldFlags))
            return E_FAIL;
    }
    else
    {
        uint32_t fieldFlags = 0;

        // If the field name has the special name for enum fields,
        // set the special name and RTSpecialName flags.
        // COMPAT: CoreCLR does not check if the field is actually in an enum type.
        if (strcmp(name, COR_ENUM_FIELD_NAME) == 0)
        {
            fieldFlags |= fdRTSpecialName | fdSpecialName;
        }
        if (!md_set_column_value_as_constant(c, mdtField_Flags, fieldFlags))
            return E_FAIL;
    }

    uint8_t const* sig = (uint8_t const*)pvSigBlob;
    uint32_t sigLength = cbSigBlob;
    if (sigLength != 0)
    {
        if (!md_set_column_value_as_blob(c, mdtField_Signature, sig, sigLength))
            return E_FAIL;
    }

    mdToken constantToken = mdTokenNil;
    if (hasConstant)
    {
        md_added_row_t constant;
        if (!md_append_row(MetaData(), mdtid_Constant, &constant))
            return E_FAIL;

        if (!md_set_column_value_as_cursor(constant, mdtConstant_Parent, c))
            return E_FAIL;

        uint32_t type = dwCPlusTypeFlag;
        if (!md_set_column_value_as_constant(constant, mdtConstant_Type, type))
            return E_FAIL;

        uint64_t defaultConstantValue = 0;
        uint8_t const* pConstantValue = (uint8_t const*)pValue;
        if (pConstantValue == nullptr)
            pConstantValue = (uint8_t const*)&defaultConstantValue;

        uint32_t constantSize = GetSizeOfConstantBlob(dwCPlusTypeFlag, pConstantValue, cchValue);
        if (!md_set_column_value_as_blob(constant, mdtConstant_Value, pConstantValue, constantSize))
            return E_FAIL;
        if (!md_cursor_to_token(constant, &constantToken))
            return CLDB_E_FILE_CORRUPT;
    }

    if (!md_cursor_to_token(c, pmd))
        return CLDB_E_FILE_CORRUPT;

    RETURN_IF_FAILED(LogToken(td, ENCFieldCreate));
    RETURN_IF_FAILED(LogToken(*pmd));
    if (hasConstant)
        RETURN_IF_FAILED(LogToken(constantToken | 0x80000000u));
    return S_OK;
}

HRESULT MetadataEmit::DefineProperty(
        mdTypeDef   td,
        LPCWSTR     szProperty,
        DWORD       dwPropFlags,
        PCCOR_SIGNATURE pvSig,
        ULONG       cbSig,
        DWORD       dwCPlusTypeFlag,
        void const  *pValue,
        ULONG       cchValue,
        mdMethodDef mdSetter,
        mdMethodDef mdGetter,
        mdMethodDef rmdOtherMethods[],
        mdProperty  *pmdProp)
{
    bool mapCreated = false;
    return FindOrCreateParentedRow(
        MetaData(),
        td,
        mdtid_PropertyMap,
        mdtPropertyMap_Parent,
        [=, &mapCreated] (mdcursor_t map)
        {
            HRESULT hr;
            if (mapCreated)
                RETURN_IF_FAILED(LogRow(map));

            md_added_row_t c;
            if (!md_add_new_row_to_list(map, mdtPropertyMap_PropertyList, &c))
                return E_FAIL;

            pal::StringConvert<WCHAR, char> cvt(szProperty);
            if (!cvt.Success())
                return E_INVALIDARG;

            char const* name = cvt;
            if (!md_set_column_value_as_utf8(c, mdtProperty_Name, name))
                return E_FAIL;


            if (pvSig != nullptr)
            {
                uint8_t const* sig = (uint8_t const*)pvSig;
                uint32_t sigLength = cbSig;
                if (!md_set_column_value_as_blob(c, mdtProperty_Type, sig, sigLength))
                    return E_FAIL;
            }

            uint32_t propFlags = (uint32_t)dwPropFlags;
            if (propFlags != std::numeric_limits<uint32_t>::max())
            {
                propFlags &= ~prReservedMask;
            }
            else
            {
                propFlags = 0;
            }

            bool hasConstant = false;
            // See if there is a Constant.
            if ((dwCPlusTypeFlag != ELEMENT_TYPE_VOID && dwCPlusTypeFlag != ELEMENT_TYPE_END &&
                dwCPlusTypeFlag != UINT32_MAX) &&
                (pValue || (pValue == 0 && (dwCPlusTypeFlag == ELEMENT_TYPE_STRING ||
                                            dwCPlusTypeFlag == ELEMENT_TYPE_CLASS))))
            {
                if (propFlags == std::numeric_limits<uint32_t>::max())
                    propFlags = 0;
                propFlags |= prHasDefault;
                hasConstant = true;
            }

            if (!md_set_column_value_as_constant(c, mdtProperty_Flags, propFlags))
                return E_FAIL;

            if (!md_cursor_to_token(c, pmdProp))
                return CLDB_E_FILE_CORRUPT;
            RETURN_IF_FAILED(LogRow(map, ENCPropertyCreate));
            RETURN_IF_FAILED(LogToken(*pmdProp));

            if (mdGetter != mdMethodDefNil)
            {
                RETURN_IF_FAILED(AddMethodSemantic(c, msGetter, mdGetter));
            }

            if (mdSetter != mdMethodDefNil)
            {
                RETURN_IF_FAILED(AddMethodSemantic(c, msSetter, mdSetter));
            }

            if (rmdOtherMethods)
            {
                for (size_t i = 0; RidFromToken(rmdOtherMethods[i]) != mdTokenNil; ++i)
                {
                    RETURN_IF_FAILED(AddMethodSemantic(c, msOther, rmdOtherMethods[i]));
                }
            }

            if (hasConstant)
            {
                md_added_row_t constant;
                if (!md_append_row(MetaData(), mdtid_Constant, &constant))
                    return E_FAIL;

                if (!md_set_column_value_as_cursor(constant, mdtConstant_Parent, c))
                    return E_FAIL;

                uint32_t type = dwCPlusTypeFlag;
                if (!md_set_column_value_as_constant(constant, mdtConstant_Type, type))
                    return E_FAIL;

                uint64_t defaultConstantValue = 0;
                uint8_t const* pConstantValue = (uint8_t const*)pValue;
                if (pConstantValue == nullptr)
                    pConstantValue = (uint8_t const*)&defaultConstantValue;

                uint32_t constantSize = GetSizeOfConstantBlob(dwCPlusTypeFlag, pConstantValue, cchValue);
                if (!md_set_column_value_as_blob(constant, mdtConstant_Value, pConstantValue, constantSize))
                    return E_FAIL;
                RETURN_IF_FAILED(LogRow(constant));
            }

            return S_OK;
        }, nullptr, &mapCreated
    );
}

HRESULT MetadataEmit::DefineParam(
        mdMethodDef md,
        ULONG       ulParamSeq,
        LPCWSTR     szName,
        DWORD       dwParamFlags,
        DWORD       dwCPlusTypeFlag,
        void const  *pValue,
        ULONG       cchValue,
        mdParamDef  *ppd)
{
    HRESULT hr;
    pal::StringConvert<WCHAR, char> cvt(szName);
    if (!cvt.Success())
        return E_INVALIDARG;

    char const* name = cvt;

    md_added_row_t c;
    mdcursor_t method;
    if (!md_token_to_cursor(MetaData(), md, &method))
        return CLDB_E_FILE_CORRUPT;

    if (!md_add_new_row_to_sorted_list(method, mdtMethodDef_ParamList, mdtParam_Sequence, (uint32_t)ulParamSeq, &c))
        return E_FAIL;

    if (!md_set_column_value_as_utf8(c, mdtParam_Name, name))
        return E_FAIL;

    bool hasConstant = false;
    // See if there is a Constant.
    if ((dwCPlusTypeFlag != ELEMENT_TYPE_VOID && dwCPlusTypeFlag != ELEMENT_TYPE_END &&
         dwCPlusTypeFlag != UINT32_MAX) &&
        (pValue || (pValue == 0 && (dwCPlusTypeFlag == ELEMENT_TYPE_STRING ||
                                    dwCPlusTypeFlag == ELEMENT_TYPE_CLASS))))
    {
        hasConstant = true;
    }

    if (dwParamFlags != std::numeric_limits<DWORD>::max())
    {
        // TODO: Handle reserved flags
        uint32_t flags = dwParamFlags;

        if (!md_set_column_value_as_constant(c, mdtParam_Flags, flags))
            return E_FAIL;
    }
    else
    {
        uint32_t flags = 0;
        if (!md_set_column_value_as_constant(c, mdtParam_Flags, flags))
            return E_FAIL;
    }

    mdToken constantToken = mdTokenNil;
    if (hasConstant)
    {
        md_added_row_t constant;
        if (!md_append_row(MetaData(), mdtid_Constant, &constant))
            return E_FAIL;

        if (!md_set_column_value_as_cursor(constant, mdtConstant_Parent, c))
            return E_FAIL;

        uint32_t type = dwCPlusTypeFlag;
        if (!md_set_column_value_as_constant(constant, mdtConstant_Type, type))
            return E_FAIL;

        uint64_t defaultConstantValue = 0;
        uint8_t const* pConstantValue = (uint8_t const*)pValue;
        if (pConstantValue == nullptr)
            pConstantValue = (uint8_t const*)&defaultConstantValue;

        uint32_t constantSize = GetSizeOfConstantBlob(dwCPlusTypeFlag, pConstantValue, cchValue);
        if (!md_set_column_value_as_blob(constant, mdtConstant_Value, pConstantValue, constantSize))
            return E_FAIL;
        if (!md_cursor_to_token(constant, &constantToken))
            return CLDB_E_FILE_CORRUPT;
    }

    if (!md_cursor_to_token(c, ppd))
        return CLDB_E_FILE_CORRUPT;

    RETURN_IF_FAILED(LogToken(md, ENCParamCreate));
    RETURN_IF_FAILED(LogToken(*ppd));
    if (hasConstant)
        RETURN_IF_FAILED(LogToken(constantToken | 0x80000000u));
    return S_OK;
}

HRESULT MetadataEmit::SetFieldProps(
        mdFieldDef  fd,
        DWORD       dwFieldFlags,
        DWORD       dwCPlusTypeFlag,
        void const  *pValue,
        ULONG       cchValue)
{
    HRESULT hr;
    mdcursor_t c;
    if (!md_token_to_cursor(MetaData(), fd, &c))
        return CLDB_E_FILE_CORRUPT;

    bool hasConstant = false;
    // See if there is a Constant.
    if ((dwCPlusTypeFlag != ELEMENT_TYPE_VOID && dwCPlusTypeFlag != ELEMENT_TYPE_END &&
         dwCPlusTypeFlag != UINT32_MAX) &&
        (pValue || (pValue == 0 && (dwCPlusTypeFlag == ELEMENT_TYPE_STRING ||
                                    dwCPlusTypeFlag == ELEMENT_TYPE_CLASS))))
    {
        hasConstant = true;
    }

    if (dwFieldFlags != std::numeric_limits<DWORD>::max())
    {
        // TODO: Handle reserved flags
        uint32_t fieldFlags = dwFieldFlags;
        if (!md_set_column_value_as_constant(c, mdtField_Flags, fieldFlags))
            return E_FAIL;
    }

    if (hasConstant)
    {
        // Create or update the Constant record that points to this field.
        mdcursor_t constantRow;
        RETURN_IF_FAILED(FindOrCreateParentedRow(MetaData(), fd, mdtid_Constant, mdtConstant_Parent, [=](mdcursor_t constant)
        {
            uint32_t type = dwCPlusTypeFlag;
            if (!md_set_column_value_as_constant(constant, mdtConstant_Type, type))
                return E_FAIL;

            uint64_t defaultConstantValue = 0;
            uint8_t const* pConstantValue = (uint8_t const*)pValue;
            if (pConstantValue == nullptr)
                pConstantValue = (uint8_t const*)&defaultConstantValue;

            uint32_t constantSize = GetSizeOfConstantBlob(dwCPlusTypeFlag, pConstantValue, cchValue);
            if (!md_set_column_value_as_blob(constant, mdtConstant_Value, pConstantValue, constantSize))
                return E_FAIL;

            return S_OK;
        }, &constantRow));
        RETURN_IF_FAILED(LogToken(fd));
        return LogRow(constantRow);
    }
    return LogToken(fd);
}

HRESULT MetadataEmit::SetPropertyProps(
        mdProperty  pr,
        DWORD       dwPropFlags,
        DWORD       dwCPlusTypeFlag,
        void const  *pValue,
        ULONG       cchValue,
        mdMethodDef mdSetter,
        mdMethodDef mdGetter,
        mdMethodDef rmdOtherMethods[])
{
    HRESULT hr;
    mdcursor_t c;
    if (!md_token_to_cursor(MetaData(), pr, &c))
        return CLDB_E_FILE_CORRUPT;

    if (dwPropFlags != std::numeric_limits<DWORD>::max())
    {
        dwPropFlags &= ~prReservedMask;
    }

    bool hasConstant = false;
    // See if there is a Constant.
    if ((dwCPlusTypeFlag != ELEMENT_TYPE_VOID && dwCPlusTypeFlag != ELEMENT_TYPE_END &&
        dwCPlusTypeFlag != UINT32_MAX) &&
        (pValue || (pValue == 0 && (dwCPlusTypeFlag == ELEMENT_TYPE_STRING ||
                                    dwCPlusTypeFlag == ELEMENT_TYPE_CLASS))))
    {
        if (dwPropFlags == std::numeric_limits<DWORD>::max())
            dwPropFlags = 0;
        dwPropFlags |= prHasDefault;
        hasConstant = true;
    }

    if (dwPropFlags != std::numeric_limits<DWORD>::max())
    {
        // TODO: Preserve reserved flags
        uint32_t flags = dwPropFlags;
        if (!md_set_column_value_as_constant(c, mdtProperty_Flags, flags))
            return E_FAIL;
    }

    if (mdGetter != mdMethodDefNil)
    {
        RETURN_IF_FAILED(RemoveSemantics(pr, msGetter));
        RETURN_IF_FAILED(AddMethodSemantic(c, msGetter, mdGetter));
    }

    if (mdSetter != mdMethodDefNil)
    {
        RETURN_IF_FAILED(RemoveSemantics(pr, msSetter));
        RETURN_IF_FAILED(AddMethodSemantic(c, msSetter, mdSetter));
    }

    if (rmdOtherMethods)
    {
        RETURN_IF_FAILED(RemoveSemantics(pr, msOther));
        for (size_t i = 0; RidFromToken(rmdOtherMethods[i]) != mdTokenNil; ++i)
        {
            RETURN_IF_FAILED(AddMethodSemantic(c, msOther, rmdOtherMethods[i]));
        }
    }

    if (hasConstant)
    {
        // Create or update the Constant record that points to this property.
        mdcursor_t constantRow;
        RETURN_IF_FAILED(FindOrCreateParentedRow(MetaData(), pr, mdtid_Constant, mdtConstant_Parent, [=](mdcursor_t constant)
        {
            uint32_t type = dwCPlusTypeFlag;
            if (!md_set_column_value_as_constant(constant, mdtConstant_Type, type))
                return E_FAIL;

            uint64_t defaultConstantValue = 0;
            uint8_t const* pConstantValue = (uint8_t const*)pValue;
            if (pConstantValue == nullptr)
                pConstantValue = (uint8_t const*)&defaultConstantValue;

            uint32_t constantSize = GetSizeOfConstantBlob(dwCPlusTypeFlag, pConstantValue, cchValue);
            if (!md_set_column_value_as_blob(constant, mdtConstant_Value, pConstantValue, constantSize))
                return E_FAIL;

            return S_OK;
        }, &constantRow));
        RETURN_IF_FAILED(LogToken(pr));
        return LogRow(constantRow);
    }

    return LogToken(pr);
}

HRESULT MetadataEmit::SetParamProps(
        mdParamDef  pd,
        LPCWSTR     szName,
        DWORD       dwParamFlags,
        DWORD       dwCPlusTypeFlag,
        void const  *pValue,
        ULONG       cchValue)
{
    HRESULT hr;
    mdcursor_t c;
    if (!md_token_to_cursor(MetaData(), pd, &c))
        return CLDB_E_FILE_CORRUPT;

    pal::StringConvert<WCHAR, char> cvt(szName);
    if (!cvt.Success())
        return E_INVALIDARG;

    char const* name = cvt;
    if (!md_set_column_value_as_utf8(c, mdtParam_Name, name))
        return E_FAIL;

    bool hasConstant = false;
    // See if there is a Constant.
    if ((dwCPlusTypeFlag != ELEMENT_TYPE_VOID && dwCPlusTypeFlag != ELEMENT_TYPE_END &&
         dwCPlusTypeFlag != UINT32_MAX) &&
        (pValue || (pValue == 0 && (dwCPlusTypeFlag == ELEMENT_TYPE_STRING ||
                                    dwCPlusTypeFlag == ELEMENT_TYPE_CLASS))))
    {
        hasConstant = true;
    }

    if (dwParamFlags != std::numeric_limits<DWORD>::max())
    {
        // TODO: Handle reserved flags
        uint32_t flags = dwParamFlags;
        if (!md_set_column_value_as_constant(c, mdtParam_Flags, flags))
            return E_FAIL;
    }

    if (hasConstant)
    {
        // Create or update the Constant record that points to this field.
        mdcursor_t constantRow;
        RETURN_IF_FAILED(FindOrCreateParentedRow(MetaData(), pd, mdtid_Constant, mdtConstant_Parent, [=](mdcursor_t constant)
        {
            uint32_t type = dwCPlusTypeFlag;
            if (!md_set_column_value_as_constant(constant, mdtConstant_Type, type))
                return E_FAIL;

            uint64_t defaultConstantValue = 0;
            uint8_t const* pConstantValue = (uint8_t const*)pValue;
            if (pConstantValue == nullptr)
                pConstantValue = (uint8_t const*)&defaultConstantValue;

            uint32_t constantSize = GetSizeOfConstantBlob(dwCPlusTypeFlag, pConstantValue, cchValue);
            if (!md_set_column_value_as_blob(constant, mdtConstant_Value, pConstantValue, constantSize))
                return E_FAIL;

            return S_OK;
        }, &constantRow));
        RETURN_IF_FAILED(LogToken(pd));
        return LogRow(constantRow);
    }

    return LogToken(pd);
}


HRESULT MetadataEmit::DefineSecurityAttributeSet(
        mdToken     tkObj,
        COR_SECATTR rSecAttrs[],
        ULONG       cSecAttrs,
        ULONG       *pulErrorAttr)
{
    // Not implemented in CoreCLR
    UNREFERENCED_PARAMETER(tkObj);
    UNREFERENCED_PARAMETER(rSecAttrs);
    UNREFERENCED_PARAMETER(cSecAttrs);
    UNREFERENCED_PARAMETER(pulErrorAttr);
    return E_NOTIMPL;
}

HRESULT MetadataEmit::ApplyEditAndContinue(
        IUnknown    *pImport)
{
    if (pImport == nullptr)
        return E_INVALIDARG;

    HRESULT hr;
    minipal::com_ptr<IDNMDOwner> delta;
    RETURN_IF_FAILED(pImport->QueryInterface(IID_IDNMDOwner, (void**)&delta));
    if (delta->MetaData() == MetaData())
        return E_INVALIDARG;

    MetadataSnapshot deltaSnapshot;
    RETURN_IF_FAILED(SnapshotDelta(pImport, delta.p, deltaSnapshot));
    RETURN_IF_FAILED(ValidateNonRemappingDelta(deltaSnapshot));

    // An empty ENCMap has no initialized table ID in DNMD. An identity entry
    // for the mandatory Module row initializes it without remapping any token.
    {
        md_added_row_t identityMap{ mdcursor_t{} };
        if (!md_append_row(deltaSnapshot.handle.get(), mdtid_ENCMap, &identityMap)
            || !md_set_column_value_as_constant(identityMap, mdtENCMap_Token, MD_MODULE_TOKEN))
            return E_FAIL;
    }

    MetadataSnapshot updated;
    RETURN_IF_FAILED(CloneMetadata(MetaData(), updated));
    RETURN_IF_FAILED(ClearENCLog(updated));
    if (!md_apply_delta(updated.handle.get(), deltaSnapshot.handle.get()))
        return E_INVALIDARG;
    RETURN_IF_FAILED(RestoreDeltaHeaps(updated.handle.get(), deltaSnapshot.handle.get()));

    mdcursor_t row;
    uint32_t count;
    if (md_create_cursor(deltaSnapshot.handle.get(), mdtid_ENCLog, &row, &count))
    {
        for (uint32_t i = 0; i < count; ++i)
        {
            uint32_t token, operation;
            if (!md_get_column_value_as_constant(row, mdtENCLog_Token, &token)
                || !md_get_column_value_as_constant(row, mdtENCLog_Op, &operation))
                return E_INVALIDARG;
            RETURN_IF_FAILED(AppendENCLog(updated.handle.get(), token, operation));
            if (i + 1 < count && !md_cursor_next(&row))
                return E_INVALIDARG;
        }
    }

    if (!md_validate(updated.handle.get()))
        return E_INVALIDARG;
    return _md_ptr.ReplaceMetaData(std::move(updated.handle), std::move(updated.image));
}

HRESULT MetadataEmit::TranslateSigWithScope(
        IMetaDataAssemblyImport *pAssemImport,
        void const  *pbHashValue,
        ULONG       cbHashValue,
        IMetaDataImport *import,
        PCCOR_SIGNATURE pbSigBlob,
        ULONG       cbSigBlob,
        IMetaDataAssemblyEmit *pAssemEmit,
        IMetaDataEmit *emit,
        PCOR_SIGNATURE pvTranslatedSig,
        ULONG       cbTranslatedSigMax,
        ULONG       *pcbTranslatedSig)
{
    HRESULT hr;
    minipal::com_ptr<IDNMDOwner> assemImport{};

    if (pAssemImport != nullptr)
        RETURN_IF_FAILED(pAssemImport->QueryInterface(IID_IDNMDOwner, (void**)&assemImport));

    minipal::com_ptr<IDNMDOwner> assemEmit{};
    if (pAssemEmit != nullptr)
        RETURN_IF_FAILED(pAssemEmit->QueryInterface(IID_IDNMDOwner, (void**)&assemEmit));

    if (import == nullptr || emit == nullptr)
        return E_INVALIDARG;

    minipal::com_ptr<IDNMDOwner> moduleImport{};
    RETURN_IF_FAILED(import->QueryInterface(IID_IDNMDOwner, (void**)&moduleImport));

    minipal::com_ptr<IDNMDOwner> moduleEmit{};
    RETURN_IF_FAILED(emit->QueryInterface(IID_IDNMDOwner, (void**)&moduleEmit));

    inline_span<uint8_t> translatedSig;
    HRESULT logStatus = S_OK;
    RETURN_IF_FAILED(ImportSignatureIntoModule(
        MetaDataOrNull(assemImport.p),
        moduleImport->MetaData(),
        { reinterpret_cast<uint8_t const*>(pbHashValue), cbHashValue },
        MetaDataOrNull(assemEmit.p),
        moduleEmit->MetaData(),
        { pbSigBlob, cbSigBlob },
        [&](mdcursor_t row)
        {
            if (SUCCEEDED(logStatus) && moduleEmit->UpdateMode() == MDUpdateENC)
            {
                mdToken token;
                logStatus = md_cursor_to_token(row, &token)
                    ? AppendENCLog(moduleEmit->MetaData(), token, ENCUpdate)
                    : CLDB_E_FILE_CORRUPT;
            }
        },
        translatedSig));
    RETURN_IF_FAILED(logStatus);

    std::copy_n(translatedSig.begin(), std::min(translatedSig.size(), (size_t)cbTranslatedSigMax), (uint8_t*)pvTranslatedSig);

    *pcbTranslatedSig = (ULONG)translatedSig.size();
    return translatedSig.size() > cbTranslatedSigMax ? CLDB_S_TRUNCATION : S_OK;
}

HRESULT MetadataEmit::SetMethodImplFlags(
        mdMethodDef md,
        DWORD       dwImplFlags)
{
    mdcursor_t c;
    if (!md_token_to_cursor(MetaData(), md, &c))
        return E_INVALIDARG;

    uint32_t flags = (uint32_t)dwImplFlags;
    if (!md_set_column_value_as_constant(c, mdtMethodDef_ImplFlags, flags))
        return E_FAIL;

    return LogToken(md);
}

HRESULT MetadataEmit::SetFieldRVA(
        mdFieldDef  fd,
        ULONG       ulRVA)
{
    uint32_t rva = (uint32_t)ulRVA;

    mdcursor_t fieldRva;
    HRESULT hr = FindOrCreateParentedRow(MetaData(), fd, mdtid_FieldRva, mdtFieldRva_Field, [=](mdcursor_t c)
    {
        if (!md_set_column_value_as_constant(c, mdtFieldRva_Rva, rva))
            return E_FAIL;

        return S_OK;
    }, &fieldRva);

    RETURN_IF_FAILED(hr);

    mdcursor_t field;
    if (!md_token_to_cursor(MetaData(), fd, &field))
        return E_INVALIDARG;

    uint32_t flags;
    if (!md_get_column_value_as_constant(field, mdtField_Flags, &flags))
        return CLDB_E_FILE_CORRUPT;

    flags |= fdHasFieldRVA;
    if (!md_set_column_value_as_constant(field, mdtField_Flags, flags))
        return E_FAIL;

    RETURN_IF_FAILED(LogToken(fd));
    return LogRow(fieldRva);
}

HRESULT MetadataEmit::Merge(
        IMetaDataImport *pImport,
        IMapToken   *pHostMapToken,
        IUnknown    *pHandler)
{
    // Not Implemented in CoreCLR
    UNREFERENCED_PARAMETER(pImport);
    UNREFERENCED_PARAMETER(pHostMapToken);
    UNREFERENCED_PARAMETER(pHandler);
    return E_NOTIMPL;
}

HRESULT MetadataEmit::MergeEnd()
{
    // Not Implemented in CoreCLR
    return E_NOTIMPL;
}

HRESULT MetadataEmit::DefineMethodSpec(
        mdToken     tkParent,
        PCCOR_SIGNATURE pvSigBlob,
        ULONG       cbSigBlob,
        mdMethodSpec *pmi)
{
    if (TypeFromToken(tkParent) != mdtMethodDef && TypeFromToken(tkParent) != mdtMemberRef)
        return META_E_BAD_INPUT_PARAMETER;

    if (cbSigBlob == 0 || pvSigBlob == nullptr || pmi == nullptr)
        return META_E_BAD_INPUT_PARAMETER;

    if (CheckDuplicates(MDDupMethodSpec))
    {
        HRESULT hr = FindExisting(mdtid_MethodSpec, DuplicateHash(pvSigBlob, cbSigBlob), [&](mdcursor_t row)
        {
            HRESULT match = MatchToken(row, mdtMethodSpec_Method, tkParent);
            return match == S_OK ? MatchBlob(row, mdtMethodSpec_Instantiation, pvSigBlob, cbSigBlob) : match;
        }, pmi);
        if (hr == S_OK)
            return META_S_DUPLICATE;
        if (FAILED(hr))
            return hr;
    }

    md_added_row_t c;
    if (!md_append_row(MetaData(), mdtid_MethodSpec, &c))
        return E_FAIL;

    if (!md_set_column_value_as_token(c, mdtMethodSpec_Method, tkParent))
        return E_FAIL;

    uint32_t sigLength = cbSigBlob;
    if (!md_set_column_value_as_blob(c, mdtMethodSpec_Instantiation, pvSigBlob, sigLength))
        return E_FAIL;

    if (!md_cursor_to_token(c, pmi))
        return CLDB_E_FILE_CORRUPT;

    return LogToken(*pmi);
}

HRESULT MetadataEmit::GetDeltaSaveSize(
        CorSaveSize fSave,
        DWORD       *pdwSaveSize)
{
    UNREFERENCED_PARAMETER(fSave);
    UNREFERENCED_PARAMETER(pdwSaveSize);
    return _md_ptr.UpdateMode() == MDUpdateENC ? E_NOTIMPL : META_E_NOT_IN_ENC_MODE;
}

HRESULT MetadataEmit::SaveDelta(
        LPCWSTR     szFile,
        DWORD       dwSaveFlags)
{
    UNREFERENCED_PARAMETER(szFile);
    UNREFERENCED_PARAMETER(dwSaveFlags);
    return _md_ptr.UpdateMode() == MDUpdateENC ? E_NOTIMPL : META_E_NOT_IN_ENC_MODE;
}

HRESULT MetadataEmit::SaveDeltaToStream(
        IStream     *pIStream,
        DWORD       dwSaveFlags)
{
    UNREFERENCED_PARAMETER(pIStream);
    UNREFERENCED_PARAMETER(dwSaveFlags);
    return _md_ptr.UpdateMode() == MDUpdateENC ? E_NOTIMPL : META_E_NOT_IN_ENC_MODE;
}

HRESULT MetadataEmit::SaveDeltaToMemory(
        void        *pbData,
        ULONG       cbData)
{
    UNREFERENCED_PARAMETER(pbData);
    UNREFERENCED_PARAMETER(cbData);
    return _md_ptr.UpdateMode() == MDUpdateENC ? E_NOTIMPL : META_E_NOT_IN_ENC_MODE;
}

HRESULT MetadataEmit::DefineGenericParam(
        mdToken      tk,
        ULONG        ulParamSeq,
        DWORD        dwParamFlags,
        LPCWSTR      szname,
        DWORD        reserved,
        mdToken      rtkConstraints[],
        mdGenericParam *pgp)
{
    HRESULT hr;
    if (reserved != 0)
        return META_E_BAD_INPUT_PARAMETER;

    if (TypeFromToken(tk) != mdtMethodDef && TypeFromToken(tk) != mdtTypeDef)
        return META_E_BAD_INPUT_PARAMETER;

    // TODO: Check for duplicates

    md_added_row_t c;
    if (!md_append_row(MetaData(), mdtid_GenericParam, &c))
        return E_FAIL;

    if (!md_set_column_value_as_token(c, mdtGenericParam_Owner, tk))
        return E_FAIL;

    uint32_t paramSeq = ulParamSeq;
    if (!md_set_column_value_as_constant(c, mdtGenericParam_Number, paramSeq))
        return E_FAIL;

    uint32_t flags = dwParamFlags;
    if (!md_set_column_value_as_constant(c, mdtGenericParam_Flags, flags))
        return E_FAIL;

    if (szname != nullptr)
    {
        pal::StringConvert<WCHAR, char> cvt(szname);
        if (!cvt.Success())
            return E_INVALIDARG;

        char const* name = cvt;
        if (!md_set_column_value_as_utf8(c, mdtGenericParam_Name, name))
            return E_FAIL;
    }
    else
    {
        char const* name = nullptr;
        if (!md_set_column_value_as_utf8(c, mdtGenericParam_Name, name))
            return E_FAIL;
    }

    if (!md_cursor_to_token(c, pgp))
        return CLDB_E_FILE_CORRUPT;
    RETURN_IF_FAILED(LogToken(*pgp));

    if (rtkConstraints != nullptr)
    {
        for (size_t i = 0; RidFromToken(rtkConstraints[i]) != mdTokenNil; i++)
        {
            md_added_row_t added_row;
            if (!md_append_row(MetaData(), mdtid_GenericParamConstraint, &added_row))
                return E_FAIL;

            if (!md_set_column_value_as_cursor(added_row, mdtGenericParamConstraint_Owner, c))
                return E_FAIL;

            if (!md_set_column_value_as_token(added_row, mdtGenericParamConstraint_Constraint, rtkConstraints[i]))
                return E_FAIL;

            mdToken token;
            if (!md_cursor_to_token(added_row, &token))
                return CLDB_E_FILE_CORRUPT;
            RETURN_IF_FAILED(LogToken(token));
        }
    }

    return S_OK;
}

HRESULT MetadataEmit::SetGenericParamProps(
        mdGenericParam gp,
        DWORD        dwParamFlags,
        LPCWSTR      szName,
        DWORD        reserved,
        mdToken      rtkConstraints[])
{
    HRESULT hr;
    if (reserved != 0)
        return META_E_BAD_INPUT_PARAMETER;

    mdcursor_t c;
    if (!md_token_to_cursor(MetaData(), gp, &c))
        return E_INVALIDARG;

    uint32_t flags = dwParamFlags;
    if (!md_set_column_value_as_constant(c, mdtGenericParam_Flags, flags))
        return E_FAIL;

    if (szName != nullptr)
    {
        pal::StringConvert<WCHAR, char> cvt(szName);
        if (!cvt.Success())
            return E_INVALIDARG;

        char const* name = cvt;
        if (!md_set_column_value_as_utf8(c, mdtGenericParam_Name, name))
            return E_FAIL;
    }

    if (rtkConstraints != nullptr)
    {
        // Delete all existing constraints
        mdcursor_t constraint;
        uint32_t count;
        if (md_create_cursor(MetaData(), mdtid_GenericParamConstraint, &constraint, &count))
        {
            md_range_result_t result = md_find_range_from_cursor(constraint, mdtGenericParamConstraint_Owner, gp, &constraint, &count);
            if (result != MD_RANGE_NOT_FOUND)
            {
                for (uint32_t i = 0; i < count; ++i, md_cursor_next(&constraint))
                {
                    mdToken parent;
                    if (!md_get_column_value_as_token(constraint, mdtGenericParamConstraint_Owner, &parent))
                        return E_FAIL;

                    if (parent == gp)
                    {
                        parent = mdGenericParamNil;
                        if (!md_set_column_value_as_token(constraint, mdtGenericParamConstraint_Owner, parent))
                            return E_FAIL;
                        mdToken token;
                        if (!md_cursor_to_token(constraint, &token))
                            return CLDB_E_FILE_CORRUPT;
                        RETURN_IF_FAILED(LogToken(token));
                    }
                }
            }
        }

        for (size_t i = 0; RidFromToken(rtkConstraints[i]) != mdTokenNil; i++)
        {
            md_added_row_t added_row;
            if (!md_append_row(MetaData(), mdtid_GenericParamConstraint, &added_row))
                return E_FAIL;

            if (!md_set_column_value_as_cursor(added_row, mdtGenericParamConstraint_Owner, c))
                return E_FAIL;

            if (!md_set_column_value_as_token(added_row, mdtGenericParamConstraint_Constraint, rtkConstraints[i]))
                return E_FAIL;

            mdToken token;
            if (!md_cursor_to_token(added_row, &token))
                return CLDB_E_FILE_CORRUPT;
            RETURN_IF_FAILED(LogToken(token));
        }
    }

    return LogToken(gp);
}

HRESULT MetadataEmit::ResetENCLog()
{
    if (_md_ptr.UpdateMode() != MDUpdateENC)
        return META_E_NOT_IN_ENC_MODE;

    mdcursor_t row;
    uint32_t count;
    if (!md_create_cursor(MetaData(), mdtid_ENCLog, &row, &count))
        return S_OK;

    MetadataSnapshot updated;
    HRESULT hr = CloneMetadata(MetaData(), updated);
    if (FAILED(hr))
        return hr;
    hr = ClearENCLog(updated);
    if (FAILED(hr))
        return hr;
    return _md_ptr.ReplaceMetaData(std::move(updated.handle), std::move(updated.image));
}

HRESULT MetadataEmit::DefineAssembly(
        void const  *pbPublicKey,
        ULONG       cbPublicKey,
        ULONG       ulHashAlgId,
        LPCWSTR     szName,
        ASSEMBLYMETADATA const *pMetaData,
        DWORD       dwAssemblyFlags,
        mdAssembly  *pma)
{
    if (szName == nullptr || pMetaData == nullptr || pma == nullptr)
        return E_INVALIDARG;

    pal::StringConvert<WCHAR, char> cvt(szName);
    if (!cvt.Success())
        return E_INVALIDARG;

    mdcursor_t c;
    uint32_t count;
    if (!md_create_cursor(MetaData(), mdtid_Assembly, &c, &count))
    {
        if (md_append_row(MetaData(), mdtid_Assembly, &c))
        {
            md_commit_row_add(c);
        }
        else
        {
            return E_FAIL;
        }
    }

    uint32_t assemblyFlags = dwAssemblyFlags;
    if (cbPublicKey != 0)
    {
        assemblyFlags |= afPublicKey;
    }

    const uint8_t* publicKey = (const uint8_t*)pbPublicKey;
    if (publicKey != nullptr)
    {
        uint32_t publicKeyLength = cbPublicKey;
        if (!md_set_column_value_as_blob(c, mdtAssembly_PublicKey, publicKey, publicKeyLength))
            return E_FAIL;
    }
    else
    {
        uint32_t publicKeyLength = 0;
        if (!md_set_column_value_as_blob(c, mdtAssembly_PublicKey, publicKey, publicKeyLength))
            return E_FAIL;
    }

    if (!md_set_column_value_as_constant(c, mdtAssembly_Flags, assemblyFlags))
        return E_FAIL;

    char const* name = cvt;
    if (!md_set_column_value_as_utf8(c, mdtAssembly_Name, name))
        return E_FAIL;

    uint32_t hashAlgId = ulHashAlgId;
    if (!md_set_column_value_as_constant(c, mdtAssembly_HashAlgId, hashAlgId))
        return E_FAIL;

    uint32_t majorVersion = pMetaData->usMajorVersion != std::numeric_limits<uint16_t>::max() ? pMetaData->usMajorVersion : 0;
    if (!md_set_column_value_as_constant(c, mdtAssembly_MajorVersion, majorVersion))
        return E_FAIL;

    uint32_t minorVersion = pMetaData->usMinorVersion != std::numeric_limits<uint16_t>::max() ? pMetaData->usMinorVersion : 0;
    if (!md_set_column_value_as_constant(c, mdtAssembly_MinorVersion, minorVersion))
        return E_FAIL;

    uint32_t buildNumber = pMetaData->usBuildNumber != std::numeric_limits<uint16_t>::max() ? pMetaData->usBuildNumber : 0;
    if (!md_set_column_value_as_constant(c, mdtAssembly_BuildNumber, buildNumber))
        return E_FAIL;

    uint32_t revisionNumber = pMetaData->usRevisionNumber != std::numeric_limits<uint16_t>::max() ? pMetaData->usRevisionNumber : 0;
    if (!md_set_column_value_as_constant(c, mdtAssembly_RevisionNumber, revisionNumber))
        return E_FAIL;

    if (pMetaData->szLocale != nullptr)
    {
        pal::StringConvert<WCHAR, char> cvtLocale(pMetaData->szLocale);
        if (!cvtLocale.Success())
            return E_INVALIDARG;

        char const* locale = cvtLocale;
        if (!md_set_column_value_as_utf8(c, mdtAssembly_Culture, locale))
            return E_FAIL;
    }
    else
    {
        char const* locale = "";
        if (!md_set_column_value_as_utf8(c, mdtAssembly_Culture, locale))
            return E_FAIL;
    }

    if (!md_cursor_to_token(c, pma))
        return E_FAIL;

    return LogToken(*pma);
}

HRESULT MetadataEmit::DefineAssemblyRef(
        void const  *pbPublicKeyOrToken,
        ULONG       cbPublicKeyOrToken,
        LPCWSTR     szName,
        ASSEMBLYMETADATA const *pMetaData,
        void const  *pbHashValue,
        ULONG       cbHashValue,
        DWORD       dwAssemblyRefFlags,
        mdAssemblyRef *pmdar)
{
    if (szName == nullptr || pMetaData == nullptr || pmdar == nullptr)
        return E_INVALIDARG;

    pal::StringConvert<WCHAR, char> cvt(szName);
    if (!cvt.Success())
        return E_INVALIDARG;

    pal::StringConvert<WCHAR, char> cvtLocale(pMetaData->szLocale == nullptr ? W("") : pMetaData->szLocale);
    if (!cvtLocale.Success())
        return E_INVALIDARG;

    uint32_t majorVersion = pMetaData->usMajorVersion != std::numeric_limits<uint16_t>::max() ? pMetaData->usMajorVersion : 0;
    uint32_t minorVersion = pMetaData->usMinorVersion != std::numeric_limits<uint16_t>::max() ? pMetaData->usMinorVersion : 0;
    uint32_t buildNumber = pMetaData->usBuildNumber != std::numeric_limits<uint16_t>::max() ? pMetaData->usBuildNumber : 0;
    uint32_t revisionNumber = pMetaData->usRevisionNumber != std::numeric_limits<uint16_t>::max() ? pMetaData->usRevisionNumber : 0;
    if (CheckDuplicates(MDDupAssemblyRef))
    {
        char const* name = cvt;
        char const* locale = cvtLocale;
        uint32_t keyLength = pbPublicKeyOrToken == nullptr ? 0 : cbPublicKeyOrToken;
        bool unifyVersion = EqualsIgnoreAsciiCase(name, "mscorlib") ||
            EqualsIgnoreAsciiCase(name, "microsoft.visualc");
        HRESULT hr = FindExisting(mdtid_AssemblyRef, DuplicateNameHash(name), [&](mdcursor_t row)
        {
            HRESULT match = MatchString(row, mdtAssemblyRef_Name, name);
            if (match != S_OK)
                return match;
            match = MatchString(row, mdtAssemblyRef_Culture, locale);
            if (match != S_OK)
                return match;
            match = MatchConstant(row, mdtAssemblyRef_MajorVersion, majorVersion);
            if (match != S_OK)
                return match;
            match = MatchConstant(row, mdtAssemblyRef_MinorVersion, minorVersion);
            if (match != S_OK)
                return match;
            if (!unifyVersion)
            {
                match = MatchConstant(row, mdtAssemblyRef_BuildNumber, buildNumber);
                if (match != S_OK)
                    return match;
                match = MatchConstant(row, mdtAssemblyRef_RevisionNumber, revisionNumber);
                if (match != S_OK)
                    return match;
            }
            return MatchAssemblyRefKey(row, pbPublicKeyOrToken, keyLength, dwAssemblyRefFlags);
        }, pmdar);
        if (hr == S_OK)
            return META_S_DUPLICATE;
        if (FAILED(hr))
            return hr;
    }

    md_added_row_t c;
    if (!md_append_row(MetaData(), mdtid_AssemblyRef, &c))
        return E_FAIL;

    const uint8_t* publicKey = (const uint8_t*)pbPublicKeyOrToken;
    if (publicKey != nullptr)
    {
        uint32_t publicKeyLength = cbPublicKeyOrToken;
        if (!md_set_column_value_as_blob(c, mdtAssemblyRef_PublicKeyOrToken, publicKey, publicKeyLength))
            return E_FAIL;
    }
    else
    {
        uint32_t publicKeyLength = 0;
        if (!md_set_column_value_as_blob(c, mdtAssemblyRef_PublicKeyOrToken, publicKey, publicKeyLength))
            return E_FAIL;
    }

    if (pbHashValue != nullptr)
    {
        uint8_t const* hashValue = (uint8_t const*)pbHashValue;
        uint32_t hashValueLength = cbHashValue;
        if (!md_set_column_value_as_blob(c, mdtAssemblyRef_HashValue, hashValue, hashValueLength))
            return E_FAIL;
    }
    else
    {
        uint8_t const* hashValue = nullptr;
        uint32_t hashValueLength = 0;
        if (!md_set_column_value_as_blob(c, mdtAssemblyRef_HashValue, hashValue, hashValueLength))
            return E_FAIL;
    }

    uint32_t assemblyFlags = PrepareForSaving(dwAssemblyRefFlags);
    if (!md_set_column_value_as_constant(c, mdtAssemblyRef_Flags, assemblyFlags))
        return E_FAIL;

    char const* name = cvt;
    if (!md_set_column_value_as_utf8(c, mdtAssemblyRef_Name, name))
        return E_FAIL;

    if (!md_set_column_value_as_constant(c, mdtAssemblyRef_MajorVersion, majorVersion))
        return E_FAIL;

    if (!md_set_column_value_as_constant(c, mdtAssemblyRef_MinorVersion, minorVersion))
        return E_FAIL;

    if (!md_set_column_value_as_constant(c, mdtAssemblyRef_BuildNumber, buildNumber))
        return E_FAIL;

    if (!md_set_column_value_as_constant(c, mdtAssemblyRef_RevisionNumber, revisionNumber))
        return E_FAIL;

    char const* locale = cvtLocale;
    if (!md_set_column_value_as_utf8(c, mdtAssemblyRef_Culture, locale))
        return E_FAIL;

    if (!md_cursor_to_token(c, pmdar))
        return E_FAIL;

    return LogToken(*pmdar);
}

HRESULT MetadataEmit::DefineFile(
        LPCWSTR     szName,
        void const  *pbHashValue,
        ULONG       cbHashValue,
        DWORD       dwFileFlags,
        mdFile      *pmdf)
{

    pal::StringConvert<WCHAR, char> cvt(szName);
    if (!cvt.Success())
        return E_INVALIDARG;

    char const* name = cvt;
    if (CheckDuplicates(MDDupFile))
    {
        HRESULT hr = FindExisting(mdtid_File, DuplicateNameHash(name),
            [&](mdcursor_t row) { return MatchString(row, mdtFile_Name, name); }, pmdf);
        if (hr == S_OK)
            return META_S_DUPLICATE;
        if (FAILED(hr))
            return hr;
    }

    md_added_row_t c;
    if (!md_append_row(MetaData(), mdtid_File, &c))
        return E_FAIL;

    if (!md_set_column_value_as_utf8(c, mdtFile_Name, name))
        return E_FAIL;

    if (pbHashValue != nullptr)
    {
        uint8_t const* hashValue = (uint8_t const*)pbHashValue;
        uint32_t hashValueLength = cbHashValue;
        if (!md_set_column_value_as_blob(c, mdtFile_HashValue, hashValue, hashValueLength))
            return E_FAIL;
    }
    else
    {
        uint8_t const* hashValue = nullptr;
        uint32_t hashValueLength = 0;
        if (!md_set_column_value_as_blob(c, mdtFile_HashValue, hashValue, hashValueLength))
            return E_FAIL;
    }

    uint32_t fileFlags = dwFileFlags != std::numeric_limits<uint32_t>::max() ? dwFileFlags : 0;
    if (!md_set_column_value_as_constant(c, mdtFile_Flags, fileFlags))
        return E_FAIL;

    if (!md_cursor_to_token(c, pmdf))
        return E_FAIL;

    return LogToken(*pmdf);
}

HRESULT MetadataEmit::DefineExportedType(
        LPCWSTR     szName,
        mdToken     tkImplementation,
        mdTypeDef   tkTypeDef,
        DWORD       dwExportedTypeFlags,
        mdExportedType   *pmdct)
{
    pal::StringConvert<WCHAR, char> cvt(szName);
    if (!cvt.Success())
        return E_INVALIDARG;

    char const* ns;
    char const* name;
    SplitTypeName(cvt, &ns, &name);

    if (CheckDuplicates(MDDupExportedType))
    {
        bool nested = TypeFromToken(tkImplementation) == mdtExportedType && !IsNilToken(tkImplementation);
        HRESULT hr = FindExisting(mdtid_ExportedType, DuplicateNameHash(name), [&](mdcursor_t row)
        {
            HRESULT match = MatchString(row, mdtExportedType_TypeNamespace, ns);
            if (match != S_OK)
                return match;
            match = MatchString(row, mdtExportedType_TypeName, name);
            if (match != S_OK)
                return match;

            mdToken implementation;
            if (!md_get_column_value_as_token(row, mdtExportedType_Implementation, &implementation))
                return CLDB_E_FILE_CORRUPT;
            bool existingNested = TypeFromToken(implementation) == mdtExportedType && !IsNilToken(implementation);
            if (nested != existingNested)
                return S_FALSE;
            return !nested || implementation == tkImplementation ? S_OK : S_FALSE;
        }, pmdct);
        if (hr == S_OK)
            return META_S_DUPLICATE;
        if (FAILED(hr))
            return hr;
    }

    md_added_row_t c;
    if (!md_append_row(MetaData(), mdtid_ExportedType, &c))
        return E_FAIL;

    if (!md_set_column_value_as_utf8(c, mdtExportedType_TypeNamespace, ns))
        return E_FAIL;
    if (!md_set_column_value_as_utf8(c, mdtExportedType_TypeName, name))
        return E_FAIL;

    if (!IsNilToken(tkImplementation))
    {
        if (!md_set_column_value_as_token(c, mdtExportedType_Implementation, tkImplementation))
            return E_FAIL;
    }
    else
    {
        // COMPAT: When the implementation column isn't defined, it is defaulted to the 0 value.
        // For the Implementation coded index, the nil File token is the 0 value;
        mdToken nilToken = mdFileNil;
        if (!md_set_column_value_as_token(c, mdtExportedType_Implementation, nilToken))
            return E_FAIL;
    }

    if (!IsNilToken(tkTypeDef))
    {
        if (!md_set_column_value_as_constant(c, mdtExportedType_TypeDefId, tkTypeDef))
            return E_FAIL;
    }
    else
    {
        mdToken nilToken = 0;
        if (!md_set_column_value_as_constant(c, mdtExportedType_TypeDefId, nilToken))
            return E_FAIL;
    }

    uint32_t exportedTypeFlags = dwExportedTypeFlags != std::numeric_limits<uint32_t>::max() ? dwExportedTypeFlags : 0;
    if (!md_set_column_value_as_constant(c, mdtExportedType_Flags, exportedTypeFlags))
        return E_FAIL;

    if (!md_cursor_to_token(c, pmdct))
        return E_FAIL;

    return LogToken(*pmdct);
}

HRESULT MetadataEmit::DefineManifestResource(
        LPCWSTR     szName,
        mdToken     tkImplementation,
        DWORD       dwOffset,
        DWORD       dwResourceFlags,
        mdManifestResource  *pmdmr)
{
    // TODO: check for duplicates
    md_added_row_t c;
    if (!md_append_row(MetaData(), mdtid_ManifestResource, &c))
        return E_FAIL;

    pal::StringConvert<WCHAR, char> cvt(szName);
    if (!cvt.Success())
        return E_INVALIDARG;

    char const* name = cvt;
    if (!md_set_column_value_as_utf8(c, mdtManifestResource_Name, name))
        return E_FAIL;

    if (!IsNilToken(tkImplementation))
    {
        if (!md_set_column_value_as_token(c, mdtManifestResource_Implementation, tkImplementation))
            return E_FAIL;
    }
    else
    {
        // COMPAT: When the implementation column isn't defined, it is defaulted to the 0 value.
        // For the Implementation coded index, the nil File token is the 0 value;
        mdToken nilToken = mdFileNil;
        if (!md_set_column_value_as_token(c, mdtManifestResource_Implementation, nilToken))
            return E_FAIL;
    }

    uint32_t offset = dwOffset != std::numeric_limits<uint32_t>::max() ? dwOffset : 0;
    if (!md_set_column_value_as_constant(c, mdtManifestResource_Offset, offset))
        return E_FAIL;

    uint32_t resourceFlags = dwResourceFlags != std::numeric_limits<uint32_t>::max() ? dwResourceFlags : 0;
    if (!md_set_column_value_as_constant(c, mdtManifestResource_Flags, resourceFlags))
        return E_FAIL;

    if (!md_cursor_to_token(c, pmdmr))
        return E_FAIL;

    return LogToken(*pmdmr);
}

HRESULT MetadataEmit::SetAssemblyProps(
        mdAssembly  pma,
        void const  *pbPublicKey,
        ULONG       cbPublicKey,
        ULONG       ulHashAlgId,
        LPCWSTR     szName,
        ASSEMBLYMETADATA const *pMetaData,
        DWORD       dwAssemblyFlags)
{
    mdcursor_t c;
    if (!md_token_to_cursor(MetaData(), pma, &c))
        return E_INVALIDARG;

    uint32_t assemblyFlags = dwAssemblyFlags;
    if (cbPublicKey != 0)
    {
        assemblyFlags |= afPublicKey;
    }

    const uint8_t* publicKey = (const uint8_t*)pbPublicKey;
    if (publicKey != nullptr)
    {
        uint32_t publicKeyLength = cbPublicKey;
        if (!md_set_column_value_as_blob(c, mdtAssembly_PublicKey, publicKey, publicKeyLength))
            return E_FAIL;
    }

    if (!md_set_column_value_as_constant(c, mdtAssembly_Flags, assemblyFlags))
        return E_FAIL;

    if (szName != nullptr)
    {
        pal::StringConvert<WCHAR, char> cvt(szName);
        if (!cvt.Success())
            return E_INVALIDARG;

        char const* name = cvt;
        if (!md_set_column_value_as_utf8(c, mdtAssembly_Name, name))
            return E_FAIL;
    }

    if (ulHashAlgId != std::numeric_limits<uint32_t>::max())
    {
        uint32_t hashAlgId = ulHashAlgId;
        if (!md_set_column_value_as_constant(c, mdtAssembly_HashAlgId, hashAlgId))
            return E_FAIL;
    }

    if (pMetaData->usMajorVersion != std::numeric_limits<uint16_t>::max())
    {
        uint32_t majorVersion = pMetaData->usMajorVersion;
        if (!md_set_column_value_as_constant(c, mdtAssembly_MajorVersion, majorVersion))
            return E_FAIL;
    }

    if (pMetaData->usMinorVersion != std::numeric_limits<uint16_t>::max())
    {
        uint32_t minorVersion = pMetaData->usMinorVersion;
        if (!md_set_column_value_as_constant(c, mdtAssembly_MinorVersion, minorVersion))
            return E_FAIL;
    }

    if (pMetaData->usBuildNumber != std::numeric_limits<uint16_t>::max())
    {
        uint32_t buildNumber = pMetaData->usBuildNumber;
        if (!md_set_column_value_as_constant(c, mdtAssembly_BuildNumber, buildNumber))
            return E_FAIL;
    }

    if (pMetaData->usRevisionNumber != std::numeric_limits<uint16_t>::max())
    {
        uint32_t revisionNumber = pMetaData->usRevisionNumber;
        if (!md_set_column_value_as_constant(c, mdtAssembly_RevisionNumber, revisionNumber))
            return E_FAIL;
    }

    if (pMetaData->szLocale != nullptr)
    {
        pal::StringConvert<WCHAR, char> cvtLocale(pMetaData->szLocale);
        if (!cvtLocale.Success())
            return E_INVALIDARG;

        char const* locale = cvtLocale;
        if (!md_set_column_value_as_utf8(c, mdtAssembly_Culture, locale))
            return E_FAIL;
    }

    return LogToken(pma);
}

HRESULT MetadataEmit::SetAssemblyRefProps(
        mdAssemblyRef ar,
        void const  *pbPublicKeyOrToken,
        ULONG       cbPublicKeyOrToken,
        LPCWSTR     szName,
        ASSEMBLYMETADATA const *pMetaData,
        void const  *pbHashValue,
        ULONG       cbHashValue,
        DWORD       dwAssemblyRefFlags)
{
    mdcursor_t c;
    if (!md_token_to_cursor(MetaData(), ar, &c))
        return E_INVALIDARG;

    uint32_t assemblyFlags = dwAssemblyRefFlags;
    if (cbPublicKeyOrToken != 0)
    {
        assemblyFlags |= afPublicKey;
    }

    const uint8_t* publicKey = (const uint8_t*)pbPublicKeyOrToken;
    if (publicKey != nullptr)
    {
        uint32_t publicKeyLength = cbPublicKeyOrToken;
        if (!md_set_column_value_as_blob(c, mdtAssemblyRef_PublicKeyOrToken, publicKey, publicKeyLength))
            return E_FAIL;
    }

    if (pbHashValue != nullptr)
    {
        uint8_t const* hashValue = (uint8_t const*)pbHashValue;
        uint32_t hashValueLength = cbHashValue;
        if (!md_set_column_value_as_blob(c, mdtAssemblyRef_HashValue, hashValue, hashValueLength))
            return E_FAIL;
    }

    if (!md_set_column_value_as_constant(c, mdtAssemblyRef_Flags, assemblyFlags))
        return E_FAIL;

    if (szName != nullptr)
    {
        _duplicateIndexes.erase(mdtid_AssemblyRef);
        pal::StringConvert<WCHAR, char> cvt(szName);
        if (!cvt.Success())
            return E_INVALIDARG;

        char const* name = cvt;
        if (!md_set_column_value_as_utf8(c, mdtAssemblyRef_Name, name))
            return E_FAIL;
    }

    if (pMetaData->usMajorVersion != std::numeric_limits<uint16_t>::max())
    {
        uint32_t majorVersion = pMetaData->usMajorVersion;
        if (!md_set_column_value_as_constant(c, mdtAssemblyRef_MajorVersion, majorVersion))
            return E_FAIL;
    }

    if (pMetaData->usMinorVersion != std::numeric_limits<uint16_t>::max())
    {
        uint32_t minorVersion = pMetaData->usMinorVersion;
        if (!md_set_column_value_as_constant(c, mdtAssemblyRef_MinorVersion, minorVersion))
            return E_FAIL;
    }

    if (pMetaData->usBuildNumber != std::numeric_limits<uint16_t>::max())
    {
        uint32_t buildNumber = pMetaData->usBuildNumber;
        if (!md_set_column_value_as_constant(c, mdtAssemblyRef_BuildNumber, buildNumber))
            return E_FAIL;
    }

    if (pMetaData->usRevisionNumber != std::numeric_limits<uint16_t>::max())
    {
        uint32_t revisionNumber = pMetaData->usRevisionNumber;
        if (!md_set_column_value_as_constant(c, mdtAssemblyRef_RevisionNumber, revisionNumber))
            return E_FAIL;
    }

    if (pMetaData->szLocale != nullptr)
    {
        pal::StringConvert<WCHAR, char> cvtLocale(pMetaData->szLocale);
        if (!cvtLocale.Success())
            return E_INVALIDARG;

        char const* locale = cvtLocale;
        if (!md_set_column_value_as_utf8(c, mdtAssemblyRef_Culture, locale))
            return E_FAIL;
    }

    return LogToken(ar);
}

HRESULT MetadataEmit::SetFileProps(
        mdFile      file,
        void const  *pbHashValue,
        ULONG       cbHashValue,
        DWORD       dwFileFlags)
{
    mdcursor_t c;
    if (!md_token_to_cursor(MetaData(), file, &c))
        return E_INVALIDARG;

    if (pbHashValue != nullptr)
    {
        uint8_t const* hashValue = (uint8_t const*)pbHashValue;
        uint32_t hashValueLength = cbHashValue;
        if (!md_set_column_value_as_blob(c, mdtFile_HashValue, hashValue, hashValueLength))
            return E_FAIL;
    }

    if (dwFileFlags != std::numeric_limits<uint32_t>::max())
    {
        uint32_t fileFlags = dwFileFlags;
        if (!md_set_column_value_as_constant(c, mdtFile_Flags, fileFlags))
            return E_FAIL;
    }

    return LogToken(file);
}

HRESULT MetadataEmit::SetExportedTypeProps(
        mdExportedType   ct,
        mdToken     tkImplementation,
        mdTypeDef   tkTypeDef,
        DWORD       dwExportedTypeFlags)
{
    mdcursor_t c;
    if (!md_token_to_cursor(MetaData(), ct, &c))
        return E_INVALIDARG;

    if (!IsNilToken(tkImplementation))
    {
        if (!md_set_column_value_as_token(c, mdtExportedType_Implementation, tkImplementation))
            return E_FAIL;
    }

    if (!IsNilToken(tkTypeDef))
    {
        if (!md_set_column_value_as_token(c, mdtExportedType_TypeDefId, tkTypeDef))
            return E_FAIL;
    }

    if (dwExportedTypeFlags != std::numeric_limits<uint32_t>::max())
    {
        uint32_t exportedTypeFlags = dwExportedTypeFlags;
        if (!md_set_column_value_as_constant(c, mdtExportedType_Flags, exportedTypeFlags))
            return E_FAIL;
    }

    return LogToken(ct);
}

HRESULT MetadataEmit::SetManifestResourceProps(
        mdManifestResource  mr,
        mdToken     tkImplementation,
        DWORD       dwOffset,
        DWORD       dwResourceFlags)
{
    mdcursor_t c;
    if (!md_token_to_cursor(MetaData(), mr, &c))
        return E_INVALIDARG;

    if (!IsNilToken(tkImplementation))
    {
        if (!md_set_column_value_as_token(c, mdtManifestResource_Implementation, tkImplementation))
            return E_FAIL;
    }

    if (dwOffset != std::numeric_limits<uint32_t>::max())
    {
        uint32_t offset = dwOffset;
        if (!md_set_column_value_as_constant(c, mdtManifestResource_Offset, offset))
            return E_FAIL;
    }

    if (dwResourceFlags != std::numeric_limits<uint32_t>::max())
    {
        uint32_t resourceFlags = dwResourceFlags;
        if (!md_set_column_value_as_constant(c, mdtManifestResource_Flags, resourceFlags))
            return E_FAIL;
    }

    return LogToken(mr);
}

HRESULT MetadataEmit::DefineMethodSemanticsHelper(mdToken tkAssociation, DWORD dwFlags, mdMethodDef md)
{
    if ((TypeFromToken(tkAssociation) != mdtProperty && TypeFromToken(tkAssociation) != mdtEvent)
        || TypeFromToken(md) != mdtMethodDef || IsNilToken(md))
        return E_INVALIDARG;

    mdcursor_t association;
    mdcursor_t method;
    if (!md_token_to_cursor(MetaData(), tkAssociation, &association)
        || !md_token_to_cursor(MetaData(), md, &method))
        return CLDB_E_RECORD_NOTFOUND;

    return AddMethodSemantic(association, static_cast<CorMethodSemanticsAttr>(dwFlags), md);
}

HRESULT MetadataEmit::SetFieldLayoutHelper(mdFieldDef fd, ULONG ulOffset)
{
    if (TypeFromToken(fd) != mdtFieldDef || IsNilToken(fd) || ulOffset == UINT32_MAX)
        return E_INVALIDARG;

    mdcursor_t field;
    if (!md_token_to_cursor(MetaData(), fd, &field))
        return CLDB_E_RECORD_NOTFOUND;

    mdcursor_t layout;
    HRESULT hr = FindOrCreateParentedRow(MetaData(), fd, mdtid_FieldLayout, mdtFieldLayout_Field, [ulOffset](mdcursor_t row)
    {
        return md_set_column_value_as_constant(row, mdtFieldLayout_Offset, ulOffset) ? S_OK : E_FAIL;
    }, &layout);
    return FAILED(hr) ? hr : LogRow(layout);
}

HRESULT MetadataEmit::DefineEventHelper(mdTypeDef td, LPCWSTR szEvent, DWORD dwEventFlags, mdToken tkEventType, mdEvent *pmdEvent)
{
    return DefineEvent(td, szEvent, dwEventFlags, tkEventType,
        mdMethodDefNil, mdMethodDefNil, mdMethodDefNil, nullptr, pmdEvent);
}

HRESULT MetadataEmit::AddDeclarativeSecurityHelper(
    mdToken tk, DWORD dwAction, void const *pValue, DWORD cbValue, mdPermission *pmdPermission)
{
    if ((TypeFromToken(tk) != mdtTypeDef && TypeFromToken(tk) != mdtMethodDef && TypeFromToken(tk) != mdtAssembly)
        || IsNilToken(tk) || pmdPermission == nullptr || (cbValue != 0 && pValue == nullptr)
        || dwAction == 0 || dwAction > dclMaximumValue)
        return E_INVALIDARG;

    return DefinePermissionSet(tk, dwAction, pValue, cbValue, pmdPermission);
}

HRESULT MetadataEmit::SetResolutionScopeHelper(mdTypeRef tr, mdToken rs)
{
    if (TypeFromToken(tr) != mdtTypeRef || IsNilToken(tr))
        return E_INVALIDARG;

    mdcursor_t typeRef;
    if (!md_token_to_cursor(MetaData(), tr, &typeRef))
        return CLDB_E_RECORD_NOTFOUND;

    return md_set_column_value_as_token(typeRef, mdtTypeRef_ResolutionScope, rs) ? LogToken(tr) : E_FAIL;
}

HRESULT MetadataEmit::SetManifestResourceOffsetHelper(mdManifestResource mr, ULONG ulOffset)
{
    if (TypeFromToken(mr) != mdtManifestResource || IsNilToken(mr))
        return E_INVALIDARG;

    mdcursor_t resource;
    if (!md_token_to_cursor(MetaData(), mr, &resource))
        return CLDB_E_RECORD_NOTFOUND;

    return md_set_column_value_as_constant(resource, mdtManifestResource_Offset, ulOffset) ? LogToken(mr) : E_FAIL;
}

HRESULT MetadataEmit::SetTypeParent(mdTypeDef td, mdToken tkExtends)
{
    if (TypeFromToken(td) != mdtTypeDef || IsNilToken(td) || tkExtends == UINT32_MAX)
        return E_INVALIDARG;

    return SetTypeDefProps(td, UINT32_MAX, tkExtends, nullptr);
}

HRESULT MetadataEmit::AddInterfaceImpl(mdTypeDef td, mdToken tkInterface)
{
    if (TypeFromToken(td) != mdtTypeDef || IsNilToken(td) || IsNilToken(tkInterface)
        || (TypeFromToken(tkInterface) != mdtTypeDef
            && TypeFromToken(tkInterface) != mdtTypeRef
            && TypeFromToken(tkInterface) != mdtTypeSpec))
        return E_INVALIDARG;

    mdcursor_t typeDef;
    if (!md_token_to_cursor(MetaData(), td, &typeDef))
        return CLDB_E_RECORD_NOTFOUND;

    mdcursor_t existing;
    uint32_t count;
    if (md_create_cursor(MetaData(), mdtid_InterfaceImpl, &existing, &count))
    {
        for (uint32_t i = 0; i < count; ++i)
        {
            mdToken parent;
            mdToken iface;
            if (!md_get_column_value_as_token(existing, mdtInterfaceImpl_Class, &parent)
                || !md_get_column_value_as_token(existing, mdtInterfaceImpl_Interface, &iface))
                return CLDB_E_FILE_CORRUPT;

            if (parent == td && iface == tkInterface)
                return S_OK;

            if (i + 1 < count && !md_cursor_next(&existing))
                return CLDB_E_FILE_CORRUPT;
        }
    }

    md_added_row_t row;
    if (!md_append_row(MetaData(), mdtid_InterfaceImpl, &row)
        || !md_set_column_value_as_token(row, mdtInterfaceImpl_Class, td)
        || !md_set_column_value_as_token(row, mdtInterfaceImpl_Interface, tkInterface))
        return E_FAIL;

    mdToken token;
    if (!md_cursor_to_token(row, &token))
        return CLDB_E_FILE_CORRUPT;
    return LogToken(token);
}

HRESULT MetadataEmit::ChangeMvid(REFGUID newMvid)
{
    mdcursor_t module;
    if (!md_token_to_cursor(MetaData(), TokenFromRid(1, mdtModule), &module))
        return CLDB_E_FILE_CORRUPT;

    mdguid_t mvid;
    static_assert(sizeof(mvid) == sizeof(newMvid));
    std::memcpy(&mvid, &newMvid, sizeof(mvid));
    return md_set_column_value_as_guid(module, mdtModule_Mvid, mvid) ? LogToken(MD_MODULE_TOKEN) : E_FAIL;
}

HRESULT MetadataEmit::SetMDUpdateMode(ULONG updateMode, ULONG* previousUpdateMode)
{
    ULONG originalMode = _md_ptr.UpdateMode();
    HRESULT hr = _md_ptr.SetUpdateMode(updateMode);
    if (FAILED(hr))
        return hr;

    if (previousUpdateMode != nullptr)
        *previousUpdateMode = originalMode;
    return S_OK;
}
