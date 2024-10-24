#ifndef _SRC_INC_DNMD_PDB_H
#define _SRC_INC_DNMD_PDB_H

#include <dnmd.h>

#ifdef __cplusplus
extern "C" {
#endif

// Methods to parse specialized blob formats defined in the Portable PDB spec.
// https://github.com/dotnet/runtime/blob/main/docs/design/specs/PortablePdb-Metadata.md

typedef enum md_blob_parse_result__
{
    mdbpr_Success,
    mdbpr_InvalidBlob,
    mdbpr_InvalidArgument,
    mdbpr_InsufficientBuffer
} md_blob_parse_result_t;

// Parse a DocumentName blob into a UTF-8 string.
md_blob_parse_result_t md_parse_document_name(mdhandle_t handle, uint8_t const* blob, size_t blob_len, char const* name, size_t* name_len);

// Parse a SequencePoints blob.
typedef struct md_sequence_points__
{
    mdToken signature;
    mdcursor_t document;
    uint32_t record_count;
    struct
    {
        enum
        {
            mdsp_DocumentRecord,
            mdsp_SequencePointRecord,
            mdsp_HiddenSequencePointRecord,
        } kind;
        union
        {
            struct
            {
                mdcursor_t document;
            } document;
            struct
            {
                uint32_t rolling_il_offset;
                uint32_t delta_lines;
                int64_t delta_columns;
                int64_t rolling_start_line;
                int64_t rolling_start_column;
            } sequence_point;
            struct
            {
                uint32_t rolling_il_offset;
            } hidden_sequence_point;
        };
    } records[];
} md_sequence_points_t;
md_blob_parse_result_t md_parse_sequence_points(mdcursor_t method_debug_information, uint8_t const* blob, size_t blob_len, md_sequence_points_t* sequence_points, size_t* buffer_len);

// Parse a LocalConstantSig blob.
typedef struct md_local_constant_sig__
{
    enum
    {
        mdck_PrimitiveConstant,
        mdck_EnumConstant,
        mdck_GeneralConstant
    } constant_kind;

    union
    {
        struct
        {
            uint8_t type_code; // ELEMENT_TYPE_* - ECMA-335 II.23.1.16
        } primitive;
        struct
        {
            uint8_t type_code; // ELEMENT_TYPE_* - ECMA-335 II.23.1.16
            mdToken enum_type; // See ECMA-335 II.14.3 for Enum restrictions.
        } enum_constant;
        struct
        {
            enum
            {
                mdgc_ValueType,
                mdgc_Class,
                mdgc_Object
            } kind;
            mdToken type; // TypeDefOrRefOrSpecEncoded - ECMA-335 II.23.2.8
        } general;
    };

    uint8_t const* value_blob;
    size_t value_len;

    uint32_t custom_modifier_count;
    struct
    {
        bool required; // Differentiate modreq vs modopt.
        mdToken type; // Custom modifier - ECMA-335 II.23.2.7
    } custom_modifiers[];
} md_local_constant_sig_t;
md_blob_parse_result_t md_parse_local_constant_sig(mdhandle_t handle, uint8_t const* blob, size_t blob_len, md_local_constant_sig_t* local_constant_sig, size_t* buffer_len);

// Parse an Imports blob.
typedef struct md_imports__
{
    uint32_t count;
    struct
    {
        enum
        {
            mdidk_ImportNamespace = 1,
            mdidk_ImportAssemblyNamespace = 2,
            mdidk_ImportType = 3,
            mdidk_ImportXmlNamespace = 4,
            mdidk_ImportAssemblyReferenceAlias = 5,
            mdidk_AliasAssemblyReference = 6,
            mdidk_AliasNamespace = 7,
            mdidk_AliasAssemblyNamespace = 8,
            mdidk_AliasType = 9,
        } kind;
        char const* alias;
        uint32_t alias_len;
        mdToken assembly;
        char const* target_namespace;
        uint32_t target_namespace_len;
        mdToken target_type;
    } imports[];
} md_imports_t;
md_blob_parse_result_t md_parse_imports(mdhandle_t handle, uint8_t const* blob, size_t blob_len, md_imports_t* imports, size_t* buffer_len);

#ifdef __cplusplus
}
#endif

#endif // _SRC_INC_DNMD_PDB_H

