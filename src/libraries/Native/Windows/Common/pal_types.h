// Licensed to the .NET Foundation under one or more agreements.
// // The .NET Foundation licenses this file to you under the MIT license.
// // See the LICENSE file in the project root for more information.
//
#pragma once
//
#include <windows.h>

struct dirent
{
  char d_name[MAX_PATH];
};

typedef struct dir_st
{
  HANDLE handle;
  WIN32_FIND_DATA FindFileData;
  struct dirent *next;
} DIR;

