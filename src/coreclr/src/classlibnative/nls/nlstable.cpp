// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include <winwrap.h>
#include <excep.h>          // For COMPlusThrow
#include <appdomain.hpp>
#include <assembly.hpp>
#include "nlstable.h"       // Class declaration

#if FEATURE_CODEPAGES_FILE

/*=================================CreateSharedMemoryMapping==================================
**Action: Create a file mapping object which can be shared among different users under Windows NT/2000.
**        Actually its just a memory mapped section of the swapfile.
**Returns: The file mapping handle.  NULL if any error happens.
**Arguments:
**      pMappingName    the name of the file mapping object.
**      iSize           Size to use
**Exceptions: 
**Note:
**      This function creates a DACL which grants GENERIC_ALL access to members of the "Everyone" group.
**      Then create a security descriptor using this DACL.  Finally, use this SA to create the file mapping object.
** WARNING:
**      This creates a shared file or shared paged memory section (if hFile == INVALID_HANDLE_VALUE) that is shared machine-wide
**      Therefore for side-by-side to work, the mapping names must be unique per version!
**      We utilize this feature for code pages in case it hasn't changed across versions we can still reuse the
**      tables, but it seems suspicious for other applications (as commented in MapDataFile below)
==============================================================================*/
// static method
HANDLE NLSTable::CreateSharedMemoryMapping(const LPCWSTR pMappingName, const int iSize ) {
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(iSize > 0);
        PRECONDITION(CheckPointer(pMappingName));
    } CONTRACTL_END;
    
    HANDLE hFileMap = NULL;

    SECURITY_DESCRIPTOR sd ;
    SECURITY_ATTRIBUTES sa ; 

    //
    // Create the sid for the Everyone group.
    //
    SID_IDENTIFIER_AUTHORITY siaWorld = SECURITY_WORLD_SID_AUTHORITY;
    PSID pSID = NULL;     
    int nSidSize;
    
    PACL pDACL = NULL; 
    int nAclSize;

    CQuickBytes newBuffer;
    
    if (!AllocateAndInitializeSid(&siaWorld, 1, SECURITY_WORLD_RID, 0, 0, 0, 0, 0, 0, 0, &pSID)) {            
        goto ErrorExit;
    }

    nSidSize = GetLengthSid(pSID);

    //
    // Create Discretionary Access Control List (DACL).
    //
    
    // First calculate the size of the DACL, since this is a linked-list like structure which contains one or more 
    // ACE (access control entry)    
    nAclSize = sizeof(ACL)                          // the header structure of ACL
        + sizeof(ACCESS_ALLOWED_ACE) + nSidSize;     // and one "access allowed ACE".

    // We know the size needed for DACL now, so create it.        
    // An exception is thrown if OOM happens.
    pDACL = (PACL) (newBuffer.AllocThrows(nAclSize));
    if(!InitializeAcl( pDACL, nAclSize, ACL_REVISION ))
        goto ErrorExit;  

    // Add the "access allowed ACE", meaning:
    //    we will allow members of the "Everyone" group to have SECTION_MAP_READ | SECTION_QUERY access to the file mapping object.
    //    for memory sections the creator will still be allowed to create it.
    if(!AddAccessAllowedAce( pDACL, ACL_REVISION, SECTION_MAP_READ | SECTION_QUERY, pSID ))
        goto ErrorExit; 

    //
    // Create Security descriptor (SD).
    //
    if(!InitializeSecurityDescriptor( &sd, SECURITY_DESCRIPTOR_REVISION ))
        goto ErrorExit; 
    // Set the previously created DACL to this SD.
    if(!SetSecurityDescriptorDacl( &sd, TRUE, pDACL, FALSE ))
        goto ErrorExit; 

    // Create Security Attribute (SA).        
    sa.nLength = sizeof( sa ) ;
    sa.bInheritHandle = TRUE ; 
    sa.lpSecurityDescriptor = &sd ;

    //
    // Finally, create the file mapping using the SA.
    //

    // If we are on Windows 2000 or later, try to open it in global namespace.  The \global namespace is ignored if
    // Terminal service is not running.
    WCHAR globalSectionName[MAX_LONGPATH];
    wcscpy_s(globalSectionName, COUNTOF(globalSectionName), W("Global\\"));
    if (wcslen(pMappingName) + wcslen(globalSectionName) >= MAX_LONGPATH) {
        goto ErrorExit;            
    }
    wcscat_s(globalSectionName, COUNTOF(globalSectionName), pMappingName);
    
    // Try to create the section in the Global\ namespace.  The CreateFileMapping() will ignore Global\ namespace if Terminal Service
    // is not running.
    hFileMap = WszCreateFileMapping(INVALID_HANDLE_VALUE, &sa, PAGE_READWRITE, 0, iSize, globalSectionName);
    
    // If not allowed to be global (like terminal server) or not WinNT, then open it in local namespace.
    if (hFileMap == NULL)
    {
        // Not WinNT or access denied for Global\ namespace, try the local namespace.  When Terminal service is running, the Local\ namespace
        // means the namespace "Sessions\<n>\BasedNamedObjects".
        hFileMap = WszCreateFileMapping(INVALID_HANDLE_VALUE, &sa, PAGE_READWRITE, 0, iSize, pMappingName);
    }        
    
ErrorExit:    
    if(pSID)
        FreeSid( pSID );

    // If still not allowed, try building one with no name
    if (hFileMap == NULL)
    {
        hFileMap = WszCreateFileMapping( 
            INVALID_HANDLE_VALUE, NULL, PAGE_READWRITE, 0, iSize, NULL);    
    }

    return (hFileMap) ;
}

/*=================================OpenOrCreateMemoryMapping==================================
**Action: Opens an existing memory mapped object, or creates a new one (by calling above fn).
**        Worst case just allocate some memory.
**Returns: The pointer to our memory.  NULL if any error happens.
**Arguments:
**      pMappingName    the name of the file mapping object.
**      iSize           Size to use
**Exceptions:
**
**IMPORTANT:
**      Memory mapped sections are cleared when set.  We expect the caller to set the last int
**      to a non-zero value, so we test this flag.  If it is still zero when we open it, we
**      assume that we've gotten a result in an unfinished state and allocate a new one instead
**      of trying to use the one with the zeros.
**
**Note:
**      This function creates a DACL which grants GENERIC_ALL access to members of the "Everyone" group.
**      Then create a security descriptor using this DACL.  Finally, use this SA to create the file mapping object.
** WARNING:
**      This creates a shared file or shared paged memory section (if hFile == INVALID_HANDLE_VALUE) that is shared machine-wide
**      Therefore for side-by-side to work, the mapping names must be unique per version!
**      We utilize this feature for code pages in case it hasn't changed across versions we can still reuse the
**      tables, but it seems suspicious for other applications (as commented in MapDataFile below)
==============================================================================*/
PBYTE NLSTable::OpenOrCreateMemoryMapping(const LPCWSTR pMappingName, const int iSize, HANDLE* mappedFile)
{
    CONTRACTL
    {
        THROWS;
        DISABLED(GC_TRIGGERS); // 
        MODE_ANY;
        PRECONDITION(iSize % 4 == 0);
        PRECONDITION(iSize > 0);
        PRECONDITION(CheckPointer(pMappingName));
        PRECONDITION(CheckPointer(mappedFile));
    } CONTRACTL_END;

    _ASSERTE(pMappingName != NULL); // Must have a string name.
    _ASSERTE(iSize > 0);            // Pointless to have <= 0 allocation
    _ASSERTE(iSize % 4 == 0);       // Need 4 byte alignment for flag check

    LPVOID  pResult = NULL;

    // Try creating/opening it.
    HANDLE  hMap = NULL;

    *mappedFile = hMap;
    // Calls into OS a lot, should switch to preemp mode
    GCX_PREEMP();

    // First try opening it.  It might already be in existence
    // The assumption here is that it's rare that we will hit the cases where two or more threads are trying to create
    // the named section at the same time.  Therefore, the following code does not use critical section or mutex trying
    // to synchornize different threads.

    // Try to open it in global namespace. The global\ namespace is ignored if terminal service is not running.
    WCHAR globalSectionName[MAX_LONGPATH];
    wcscpy_s(globalSectionName, COUNTOF(globalSectionName), W("Global\\"));
    if (wcslen(pMappingName) + wcslen(globalSectionName) >= MAX_LONGPATH)
        return NULL;
    
    wcscat_s(globalSectionName, COUNTOF(globalSectionName), pMappingName);
    
    hMap = WszOpenFileMapping(FILE_MAP_READ, TRUE, globalSectionName);
    if (hMap == NULL) {
        // If access is denied for global\namespace or the name is not opened in global namespace, try the local namespace.
        // Also if we're rotor or win 9x.
        hMap = WszOpenFileMapping(FILE_MAP_READ, TRUE, pMappingName);       
    }  
    
    if (hMap != NULL) {
        // We got a section, map a view, READ ONLY!
        pResult = MapViewOfFile( hMap, FILE_MAP_READ, 0, 0, 0);

        // Anything found?
        if (pResult != NULL)
        {
            // Make sure our result is allocated.  We expect a non-0 flag to be set for last int of our section
            const int* pFlag = (int*)(((BYTE*)pResult) + iSize - 4);
            if (*pFlag != 0)
            {
                *mappedFile = hMap;
                // Found a valid already opened section!
                return (PBYTE)pResult;
            }

            // Couldn't find it, unmap it.
            UnmapViewOfFile(pResult);
            pResult = NULL;
        }

        // We can't use this one, so close it
        CloseHandle(hMap);
        hMap = NULL;
    }
    
    // Didn't get a section, try to create one, NT/XP/.Net gets security permissions, 9X doesn't,
    // but our helper fn takes care of that for us.
    hMap = NLSTable::CreateSharedMemoryMapping(pMappingName, iSize);

    // Were we successfull?
    if (hMap != NULL)
    {
        // We have hMap, try to get our section
        pResult = MapViewOfFile( hMap, FILE_MAP_ALL_ACCESS, 0, 0, 0);

        // Don't close hMap unless we aren't using it.
        // That confuses the mapping stuff and we lose the name, it'll close when runtime shuts down.
        if (pResult == NULL)
        {
            CloseHandle(hMap);
            hMap = NULL;
        }
        // There is no need to zero out the mapCodePageCached field, since the initial contents of the pages in the file mapping object are zero.
        
        *mappedFile = hMap;
    }

    return (PBYTE)pResult;
}

#endif
