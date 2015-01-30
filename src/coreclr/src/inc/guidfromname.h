//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


#ifndef GUIDFROMNAME_H_
#define GUIDFROMNAME_H_

// GuidFromName.h - function prototype

void CorGuidFromNameW
(
    GUID *  pGuidResult,        // resulting GUID
    LPCWSTR wzName,             // the unicode name from which to generate a GUID
    SIZE_T  cchName             // name length in count of unicode character.
                                // -1 if lstrlen(wzName)+1 should be used
);

#endif // GUIDFROMNAME_H_
