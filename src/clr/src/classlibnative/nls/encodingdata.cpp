// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"

#include <mlang.h>

#include "nlsinfo.h"

//
// Encoding data tables
//

//
// Index an encoding name into an codepage in CodePageDataTable.
//
// Please KEEP this table SORTED ALPHABETICALLY! We do a binary search on this array.
const EncodingDataItem COMNlsInfo::EncodingDataTable[] = {
    // encoding name, codepage.
    {"ANSI_X3.4-1968", 20127 },
    {"ANSI_X3.4-1986", 20127 },
    {"ascii", 20127 },
    {"cp367", 20127 },
    {"cp819", 28591 },
    {"csASCII", 20127 },
    {"csISOLatin1", 28591 },
    {"csUnicode11UTF7", 65000 },
    {"IBM367", 20127 },
    {"ibm819", 28591 },
    {"ISO-10646-UCS-2", 1200 },
    {"iso-8859-1", 28591 },
    {"iso-ir-100", 28591 },
    {"iso-ir-6", 20127 },
    {"ISO646-US", 20127 },
    {"iso8859-1", 28591 },
    {"ISO_646.irv:1991", 20127 },
    {"iso_8859-1", 28591 },
    {"iso_8859-1:1987", 28591 },
    {"l1", 28591 },
    {"latin1", 28591 },
    {"ucs-2", 1200 },
    {"unicode", 1200}, 
    {"unicode-1-1-utf-7", 65000 },
    {"unicode-1-1-utf-8", 65001 },
    {"unicode-2-0-utf-7", 65000 },
    {"unicode-2-0-utf-8", 65001 },
    // People get confused about the FFFE here.  We can't change this because it'd break existing apps.
    // This has been this way for a long time, including in Mlang.
    {"unicodeFFFE", 1201},             // Big Endian, BOM seems backwards, think of the BOM in little endian order.
    {"us", 20127 },
    {"us-ascii", 20127 },
    {"utf-16", 1200 },
    {"UTF-16BE", 1201}, 
    {"UTF-16LE", 1200},        
    {"utf-32", 12000 },
    {"UTF-32BE", 12001 },
    {"UTF-32LE", 12000 },
    {"utf-7", 65000 },
    {"utf-8", 65001 },
    {"x-unicode-1-1-utf-7", 65000 },
    {"x-unicode-1-1-utf-8", 65001 },
    {"x-unicode-2-0-utf-7", 65000 },
    {"x-unicode-2-0-utf-8", 65001 },
    
};

const int COMNlsInfo::m_nEncodingDataTableItems =
    sizeof(COMNlsInfo::EncodingDataTable)/sizeof(EncodingDataItem);

// Working set optimization: 
// 1. code page, family code page stored as unsigned short
// 2. if web/header/body names are the same, only web name is stored; otherwise, we store "|webname|headername|bodyname"
// 3. Move flags before names to fill gap on 64-bit platforms

#define MapCodePageDataItem(cp, fcp, names, flags)  { cp, fcp, flags, names }
//
// Information about codepages.
//
const CodePageDataItem COMNlsInfo::CodePageDataTable[] = {


// Total Items: 
// code page, family code page, web name, header name, body name, flags

    MapCodePageDataItem(  1200,  1200, "utf-16",      MIMECONTF_SAVABLE_BROWSER), // "Unicode"
    MapCodePageDataItem(  1201,  1200, "utf-16BE",    0), // Big Endian, old FFFE BOM seems backwards, think of the BOM in little endian order.
    MapCodePageDataItem(  12000, 1200, "utf-32", 0), // "Unicode (UTF-32)"
    MapCodePageDataItem(  12001, 1200, "utf-32BE", 0), // "Unicode (UTF-32 Big Endian)"
    MapCodePageDataItem(  20127, 1252, "us-ascii", MIMECONTF_MAILNEWS | MIMECONTF_SAVABLE_MAILNEWS), // "US-ASCII"
    MapCodePageDataItem(  28591,  1252, "iso-8859-1",  MIMECONTF_MAILNEWS | MIMECONTF_BROWSER | MIMECONTF_SAVABLE_MAILNEWS | MIMECONTF_SAVABLE_BROWSER), // "Western European (ISO)"
    MapCodePageDataItem(  65000, 1200, "utf-7", MIMECONTF_MAILNEWS | MIMECONTF_SAVABLE_MAILNEWS), // "Unicode (UTF-7)"
    MapCodePageDataItem(  65001, 1200, "utf-8", MIMECONTF_MAILNEWS | MIMECONTF_BROWSER | MIMECONTF_SAVABLE_MAILNEWS | MIMECONTF_SAVABLE_BROWSER), // "Unicode (UTF-8)"


    // End of data.
    MapCodePageDataItem( 0, 0, NULL, 0),

};

const int COMNlsInfo::m_nCodePageTableItems = 
    sizeof(COMNlsInfo::CodePageDataTable)/sizeof(CodePageDataItem);

