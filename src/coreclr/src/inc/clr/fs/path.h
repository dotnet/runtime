// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//
// This header provides general path-related file system services.

#ifndef _clr_fs_Path_h_
#define _clr_fs_Path_h_

#include "clrtypes.h"

#include "strsafe.h"

#include "clr/str.h"

namespace clr
{
    namespace fs
    {
        class Path
        {
        public:
            //-----------------------------------------------------------------------------------------
            // Returns true if wzPath represents a relative path.
            static inline bool
            IsRelative(LPCWSTR wzPath)
            {
                _ASSERTE(wzPath != nullptr);

                // Similar to System.IO.Path.IsRelative()
#if TARGET_UNIX
                if(wzPath[0] == VOLUME_SEPARATOR_CHAR_W)
                {
                    return false;
                }
#else
                // Check for a paths like "C:\..." or "\\...". Additional notes:
                // - "\\?\..." - long format paths are considered as absolute paths due to the "\\" prefix
                // - "\..." - these paths are relative, as they depend on the current drive
                // - "C:..." and not "C:\..." - these paths are relative, as they depend on the current directory for drive C
                if (wzPath[0] != W('\0') &&
                    wzPath[1] == VOLUME_SEPARATOR_CHAR_W &&
                    wzPath[2] == DIRECTORY_SEPARATOR_CHAR_W &&
                    (
                        (wzPath[0] >= W('A') && wzPath[0] <= W('Z')) ||
                        (wzPath[0] >= W('a') && wzPath[0] <= W('z'))
                    ))
                {
                    return false;
                }
                if (wzPath[0] == DIRECTORY_SEPARATOR_CHAR_W && wzPath[1] == DIRECTORY_SEPARATOR_CHAR_W)
                {
                    return false;
                }
#endif

                return true;
            }
        };
    }
}

#endif // _clr_fs_Path_h_
