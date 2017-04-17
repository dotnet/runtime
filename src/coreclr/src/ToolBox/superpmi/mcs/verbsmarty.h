//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

//----------------------------------------------------------
// verbSmarty.h - verb that outputs Smarty test ID for mc
//----------------------------------------------------------
#ifndef _verbSmarty
#define _verbSmarty

class verbSmarty
{
public:
    verbSmarty(HANDLE hFile);
    void DumpTestInfo(int testID);
    static int DoWork(const char* nameOfInput, const char* nameofOutput, int indexCount, const int* indexes);

private:
    HANDLE m_hFile;
};
#endif
