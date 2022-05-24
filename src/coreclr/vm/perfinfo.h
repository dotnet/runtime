// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
// File: perfinfo.h
//

#ifndef PERFINFO_H
#define PERFINFO_H


#include "sstring.h"
#include "fstream.h"

/*
   A perfinfo-%d.map is created for every process that is created with manage code, the %d
   being repaced with the process ID.
   Every line in the perfinfo-%d.map is a type and value, separated by sDelimiter character: type;value
   type represents what the user might want to do with its given value. value has a format chosen by
   the user for parsing later on.
*/
class PerfInfo {
public:
    PerfInfo(int pid);
    ~PerfInfo();
    void LogImage(PEAssembly* pPEAssembly, WCHAR* guid);

private:
    CFileStream* m_Stream;

    const char sDelimiter = ';';

    void OpenFile(SString& path);

    void WriteLine(SString& type, SString& value);

};


#endif
