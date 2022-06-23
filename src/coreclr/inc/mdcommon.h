// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// MDCommon.h
//
// Common header file for both MD and COMPLIB subdirectories
//
//*****************************************************************************

#ifndef __MDCommon_h__
#define __MDCommon_h__

// File types for the database.
enum FILETYPE
{
	FILETYPE_UNKNOWN,					// Unknown or undefined type.
	FILETYPE_CLB,						// Native .clb file format.
	FILETYPE_CLX, 					    // An obsolete file format.
	FILETYPE_NTPE,						// Windows PE executable.
	FILETYPE_NTOBJ, 					// .obj file format (with .clb embedded).
	FILETYPE_TLB						// Typelib format.
};

enum MAPPINGTYPE
{
    MTYPE_NOMAPPING,                        // No mapped file
    MTYPE_FLAT,                             // Mapped as a flat file
    MTYPE_IMAGE                             // Mapped with the SEC_IMAGE flag
};


#define SCHEMA_STREAM_A             "#Schema"
#define STRING_POOL_STREAM_A        "#Strings"
#define BLOB_POOL_STREAM_A          "#Blob"
#define US_BLOB_POOL_STREAM_A       "#US"
#define GUID_POOL_STREAM_A          "#GUID"
#define COMPRESSED_MODEL_STREAM_A   "#~"
#define ENC_MODEL_STREAM_A          "#-"
#define MINIMAL_MD_STREAM_A         "#JTD"
#ifdef FEATURE_METADATA_EMIT_PORTABLE_PDB
#define PDB_STREAM_A                "#Pdb"
#endif // FEATURE_METADATA_EMIT_PORTABLE_PDB

#define SCHEMA_STREAM               W("#Schema")
#define STRING_POOL_STREAM          W("#Strings")
#define BLOB_POOL_STREAM            W("#Blob")
#define US_BLOB_POOL_STREAM         W("#US")
#define GUID_POOL_STREAM            W("#GUID")
#define COMPRESSED_MODEL_STREAM     W("#~")
#define ENC_MODEL_STREAM            W("#-")
#define MINIMAL_MD_STREAM           W("#JTD")
#ifdef FEATURE_METADATA_EMIT_PORTABLE_PDB
#define PDB_STREAM                  W("#Pdb")
#endif // FEATURE_METADATA_EMIT_PORTABLE_PDB

#endif // __MDCommon_h__
