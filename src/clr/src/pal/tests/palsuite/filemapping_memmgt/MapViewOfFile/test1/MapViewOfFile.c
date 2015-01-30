//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=============================================================
**
** Source: MapViewOfFile.c
**
**
** Purpose: Positive test the MapViewOfFile API.
**          Call MapViewOfFile with access FILE_MAP_READ.
**
**
**============================================================*/
#include <palsuite.h>
#define MAPPINGSIZE 8192
int __cdecl main(int argc, char *argv[])
{

    HANDLE  hFile = INVALID_HANDLE_VALUE;
    LPSTR   buf = NULL;
    CHAR    ch[MAPPINGSIZE];
    CHAR    lpFileName[] = "test.tmp";
    DWORD   dwBytesWritten = 0;
    DWORD   dwInitialSize = 0;
    DWORD   dwFinalSize = 0;
    BOOL    bRetVal = FALSE;

    HANDLE hFileMapping = 0;
    LPVOID lpMapViewAddress = NULL;

    /* Initialize the PAL environment.
     */
    if( 0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    /* Create a file handle with CreateFile.
     */
    hFile = CreateFile( lpFileName,
                        GENERIC_WRITE|GENERIC_READ,
                        FILE_SHARE_READ|FILE_SHARE_WRITE,
                        NULL,
                        OPEN_ALWAYS,
                        FILE_ATTRIBUTE_NORMAL | FILE_FLAG_NO_BUFFERING, 
                        NULL);
    
    if (hFile == INVALID_HANDLE_VALUE)
    {
        Fail( "ERROR: %u :unable to create file \"%s\".\n",
              GetLastError(), lpFileName);
    }

    /* Get the initial size of file, for latter tests.
     */
    dwInitialSize = GetFileSize (hFile, NULL); 
    if ( INVALID_FILE_SIZE == dwInitialSize )
    {
        Fail("ERROR:%u: The created file \"%s\" has an invalid "
             "file size.\n",GetLastError(),lpFileName);
    }

    /*
     * An application must meet certain requirements when working 
     * with files opened with FILE_FLAG_NO_BUFFERING: 
     *      File access must begin at byte offsets within the file that 
     *      are integer multiples of the volume's sector size. To determine a 
     *      volume's sector size, call the GetDiskFreeSpace function. 
     *
     *      File access must be for numbers of bytes that are integer 
     *      multiples of the volume's sector size. For example, if the
     *      sector size is 512 bytes, an application can request reads and 
     *      writes of 512, 1024, or 2048 bytes, but not of 335, 981, or 7171 bytes. 
     *
     *      Buffer addresses for read and write operations must be sector 
     *      aligned (aligned on addresses in memory that are integer multiples 
     *      of the volume's sector size). One way to sector align buffers is to use the 
     *      VirtualAlloc function to allocate the buffers. This function allocates memory 
     *      that is aligned on addresses that are integer multiples of the system's page size. 
     *      Because both page and volume sector sizes are powers of 2, memory aligned by multiples 
     *      of the system's page size is also aligned by multiples of the volume's sector size. 
     */
    buf = (LPSTR)VirtualAlloc(  NULL,               /* Let the system decide the location. */
                                MAPPINGSIZE / 2,    /* One page, the smallest you can request */
                                MEM_COMMIT,         /* Reserve and commit in one pass */
                                PAGE_READWRITE );   /* Allow reading and writting. */

    if ( NULL == buf )
    {
        Trace( "VirtualAlloc failed! LastError=%d\n", GetLastError() );
        CloseHandle( hFile );
        Fail("");
    }
    

    /* 
     * Write to the File handle.
     * The reminder will be padded with zeros. 
     */ 
    strncpy( buf, 
             "thats not a test string....THIS is a test string", 
             MAPPINGSIZE / 2 );

    bRetVal = WriteFile(hFile,
                        buf,
                        MAPPINGSIZE / 2,
                        &dwBytesWritten,
                        NULL);

    if ( FALSE == bRetVal )
    {
        Trace( "ERROR: %u :unable to write to file handle hFile=0x%lx\n",
              GetLastError(), hFile);
        CloseHandle(hFile);
        VirtualFree( buf, 0, MEM_RELEASE );
        Fail("");
    }

    /* Create a unnamed file-mapping object with file handle FileHandle
     * and with PAGE_READWRITE protection.
    */
    hFileMapping = CreateFileMapping(  hFile,
                                       NULL,            /*not inherited*/
                                       PAGE_READWRITE,  /*read and wite*/
                                       0,               /*high-order of object size*/
                                       MAPPINGSIZE,     /*low-orger of object size*/
                                       NULL);           /*unnamed object*/

    if( NULL == hFileMapping )
    {
        Trace("ERROR:%u: Failed to create File Mapping.\n", GetLastError());
        CloseHandle(hFile);
        VirtualFree( buf, 0, MEM_RELEASE );
        Fail("");
    }

    /* maps a view of a file into the address space of the calling process.
     */
    lpMapViewAddress = MapViewOfFile( hFileMapping,
                                      FILE_MAP_READ,  /* access code */
                                      0,              /*high order offset*/
                                      0,              /*low order offset*/
                                      MAPPINGSIZE);   /* number of bytes for map */

    if( NULL == lpMapViewAddress )
    {
        Trace( "ERROR:%u: Failed to call MapViewOfFile API to map"
              " a view of file!\n", GetLastError());
        CloseHandle(hFile);
        CloseHandle(hFileMapping);
        VirtualFree( buf, 0, MEM_RELEASE );
        Fail("");
    }

    /* Verify that the size of the file has increased to 
     * accomidate the MapView. 
     */
    dwFinalSize = GetFileSize (hFile, NULL); 
    if ( (dwFinalSize <= dwInitialSize) && (dwFinalSize != MAPPINGSIZE))
    {
        
        CloseHandle(hFile);
        CloseHandle(hFileMapping);
        VirtualFree( buf, 0, MEM_RELEASE );
        UnmapViewOfFile(lpMapViewAddress);
        
        Fail( "ERROR: Size of the file was expected to "
              "increase from \"%d\", to \"%d\".\n ",
              dwInitialSize,
              dwFinalSize);
    }

    /* Copy the MapViewOfFile to buffer, so we can
     * compare with value read from file directly.
     */
    memcpy(ch, (LPCSTR)lpMapViewAddress, MAPPINGSIZE);
    if (memcmp(ch, buf, strlen(buf)) != 0)
    {
        CloseHandle(hFile);
        CloseHandle(hFileMapping);
        UnmapViewOfFile(lpMapViewAddress) ;
        VirtualFree( buf, 0, MEM_RELEASE );
        
        Fail( "ERROR: MapViewOfFile not equal to file contents "
              "retrieved \"%s\", expected \"%s\".\n",
              ch,
              buf);
    }

    /* Unmap the view of file.
     */
    if( FALSE == UnmapViewOfFile(lpMapViewAddress) )
    {
        Trace( "\nFailed to call UnmapViewOfFile API to unmap the "
              "view of a file, error code=%u\n", GetLastError());
        CloseHandle(hFile);
        CloseHandle(hFileMapping);
        VirtualFree( buf, 0, MEM_RELEASE );
        Fail("");
    }

    /* Close handle to create file.
     */
    if( FALSE == CloseHandle(hFile) )
    {
        Trace( "ERROR:%u:Failed to call CloseHandle API to close a file handle.",
              GetLastError());
        CloseHandle(hFileMapping);
        VirtualFree( buf, 0, MEM_RELEASE );
        Fail("");
    }

    if( FALSE == CloseHandle(hFileMapping) )
    {
        Trace( "ERROR:%u:Failed to call CloseHandle API to close a "
              "filemapping handle.",GetLastError());
        VirtualFree( buf, 0, MEM_RELEASE );
        Fail("");
    }
    
    VirtualFree( buf, 0, MEM_RELEASE );
    PAL_Terminate();
    return PASS;
}

