// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// clang-format off
#ifndef HANDLE_KIND
#error  Define HANDLE_KIND before including this file.
#endif

HANDLE_KIND(GTF_ICON_SCOPE_HDL       , "scope"                      , HKF_INVARIANT) // scope handle
HANDLE_KIND(GTF_ICON_CLASS_HDL       , "class"                      , HKF_INVARIANT) // class handle
HANDLE_KIND(GTF_ICON_METHOD_HDL      , "method"                     , HKF_INVARIANT) // method handle
HANDLE_KIND(GTF_ICON_FIELD_HDL       , "field"                      , HKF_INVARIANT) // field handle
HANDLE_KIND(GTF_ICON_STATIC_HDL      , "static"                     , 0)             // handle to static data
HANDLE_KIND(GTF_ICON_STR_HDL         , "string"                     , HKF_INVARIANT | HKF_NONNULL) // pinned handle pointing to a string object
HANDLE_KIND(GTF_ICON_OBJ_HDL         , "object"                     , 0)             // object handle (e.g. frozen string or Type object)
HANDLE_KIND(GTF_ICON_CONST_PTR       , "const ptr"                  , HKF_INVARIANT) // pointer to immutable data, (e.g. IAT_PPVALUE)
HANDLE_KIND(GTF_ICON_GLOBAL_PTR      , "global ptr"                 , 0)             // pointer to mutable data (e.g. from the VM state)
HANDLE_KIND(GTF_ICON_VARG_HDL        , "vararg"                     , HKF_INVARIANT) // var arg cookie handle
HANDLE_KIND(GTF_ICON_PINVKI_HDL      , "pinvoke"                    , 0)             // pinvoke calli handle
HANDLE_KIND(GTF_ICON_TOKEN_HDL       , "token"                      , HKF_INVARIANT) // token handle (other than class, method or field)
HANDLE_KIND(GTF_ICON_TLS_HDL         , "tls"                        , HKF_INVARIANT) // TLS ref with offset
HANDLE_KIND(GTF_ICON_FTN_ADDR        , "ftn"                        , 0)             // function address
HANDLE_KIND(GTF_ICON_CIDMID_HDL      , "cid/mid"                    , HKF_INVARIANT) // class ID or a module ID
HANDLE_KIND(GTF_ICON_BBC_PTR         , "bbc"                        , 0)             // basic block count pointer
HANDLE_KIND(GTF_ICON_STATIC_BOX_PTR  , "static box ptr"             , 0)             // address of the box for a STATIC_IN_HEAP field
HANDLE_KIND(GTF_ICON_FIELD_SEQ       , "field seq"                  , 0)             // FieldSeq* (used only as VNHandle)
HANDLE_KIND(GTF_ICON_STATIC_ADDR_PTR , "static base addr cell"      , HKF_INVARIANT | HKF_NONNULL) // pointer to a static base address
HANDLE_KIND(GTF_ICON_SECREL_OFFSET   , "relative offset in section" , HKF_INVARIANT) // offset in a certain section.
HANDLE_KIND(GTF_ICON_TLSGD_OFFSET    , "tls global dynamic offset"  , HKF_INVARIANT) // argument to tls_get_addr.

#undef HANDLE_KIND
// clang-format on
