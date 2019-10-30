// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//
// This header provides general filename-related file system services.

#ifndef _clr_fs_File_h_
#define _clr_fs_File_h_

#include "clrtypes.h"
#include "cor.h" // SELECTANY

namespace clr
{
    namespace fs
    {
        // This list taken from ndp/clr/src/bcl/system/io/path.cs
        SELECTANY WCHAR const g_rgInvalidFileNameChars[] =
            { W('"'), W('<'), W('>'), W('|'), W('\0'), (WCHAR)1, (WCHAR)2, (WCHAR)3, (WCHAR)4, (WCHAR)5, (WCHAR)6,
              (WCHAR)7, (WCHAR)8, (WCHAR)9, (WCHAR)10, (WCHAR)11, (WCHAR)12, (WCHAR)13, (WCHAR)14,
              (WCHAR)15, (WCHAR)16, (WCHAR)17, (WCHAR)18, (WCHAR)19, (WCHAR)20, (WCHAR)21, (WCHAR)22,
              (WCHAR)23, (WCHAR)24, (WCHAR)25, (WCHAR)26, (WCHAR)27, (WCHAR)28, (WCHAR)29, (WCHAR)30,
              (WCHAR)31, W(':'), W('*'), W('?'), W('\\'), W('/') };

        class File
        {
        public:
            static inline bool Exists(
                LPCWSTR wzFilePath)
            {
                DWORD attrs = WszGetFileAttributes(wzFilePath);
                return (attrs != INVALID_FILE_ATTRIBUTES) && !(attrs & FILE_ATTRIBUTE_DIRECTORY);
            }
        };
    }
}

#endif // _clr_fs_File_h_
