//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*

 *
 * Example of minimal program running under PAL.
 *
 * Run it using:
 *   export PAL_DBG_CHANNELS="+all.all"
 *   export LD_LIBRARY_PATH=$LD_LIBRARY_PATH:..
 *   ./example1
 *
 * With the PAL_DEBUG_CHANNELS environment variable set as above you
 * should see a trace output when the program runs. Setting
 * LD_LIBRARY_PATH is necessary unless you have installed librotor_pal.so in
 * a standard location.
 *
 * Build notes :
 * Since the PAL uses pthreads, some options must be passed to gcc to tell it
 * to link against thread-safe versions of the standard libraries.
 * On FreeBSD, use gcc -pthread
 *
 */

#include <pal.h>
extern void *dlopen(const char *file, int mode);

int main(int argc, char *argv[])
{
  WCHAR  src[4] = {'f', 'o', 'o', '\0'};
  WCHAR dest[4] = {'b', 'a', 'r', '\0'};
  WCHAR  dir[5] = {'/', 't', 'm', 'p', '\0'};
  HANDLE h;
  unsigned int b;

  PAL_Initialize(argc, (const char**)argv);
  SetCurrentDirectoryW(dir);
  SetCurrentDirectoryW(dir);
  h =  CreateFileW(src, GENERIC_WRITE, FILE_SHARE_READ, NULL, CREATE_NEW, 0, NULL);
  WriteFile(h, "Testing\n", 8, &b, FALSE);
  CloseHandle(h);
  CopyFileW(src, dest, FALSE);
  DeleteFileW(src);
  PAL_Terminate();
  return 0;
}

