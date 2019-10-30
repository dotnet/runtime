// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// File: SecurityWrapper.h
//
// Wrapper around Win32 Security functions
//
//*****************************************************************************


#ifndef _SECURITY_WRAPPER_H
#define _SECURITY_WRAPPER_H

#ifdef FEATURE_PAL
#error This file should not be included on non-Windows platforms.
#endif

//-----------------------------------------------------------------------------
// Wrapper around a PSID.
// This class does not own the memory.
//-----------------------------------------------------------------------------
class Sid
{
public:
    // Initial the Sid wrapper around an existing SID.
    Sid(PSID pSid);
    static bool Equals(const Sid & a, const Sid & b) { return Equals(a.m_pSid, b.m_pSid); }
    static bool Equals(const Sid & a, PSID b)        { return Equals(a.m_pSid, b); }
    static bool Equals(PSID a, const Sid & b)        { return Equals(a, b.m_pSid); }    
    static bool Equals(PSID a, PSID b);

    PSID RawSid() { return m_pSid; }
protected:
    // Pointer to Sid buffer. We don't owner the data.
    PSID m_pSid;  
};

//-----------------------------------------------------------------------------
// Wrapper around a PSID with buffer.
//-----------------------------------------------------------------------------
class SidBuffer
{
public:
    SidBuffer();
    ~SidBuffer();

    // Get the underlying sid
    Sid GetSid();

    // Do we not have a sid? This will be true if init fails.
    bool IsNull() { return m_pBuffer == NULL; }

    // Go to definitions to see detailed comments
    HRESULT InitFromProcessNoThrow(DWORD pid);
    void InitFromProcess(DWORD pid); // throws
    HRESULT InitFromProcessUserNoThrow(DWORD pid);
    void InitFromProcessUser(DWORD pid); // throws
    HRESULT InitFromProcessAppContainerSidNoThrow(DWORD pid);

protected:
    BYTE * m_pBuffer;
};

//-----------------------------------------------------------------------------
// Access Control List.
//-----------------------------------------------------------------------------
class Dacl
{
public:
    Dacl(PACL pAcl);

    SIZE_T GetAceCount();    
    ACE_HEADER * GetAce(SIZE_T dwAceIndex);
protected:
    PACL m_acl;
};

//-----------------------------------------------------------------------------
// Represent a win32 SECURITY_DESCRIPTOR object.
// (Note there's a "SecurityDescriptor" class in the VM for managed goo, 
// so we prefix this with "Win32" to avoid a naming collision.)
//-----------------------------------------------------------------------------
class Win32SecurityDescriptor
{
public:
    Win32SecurityDescriptor();
    ~Win32SecurityDescriptor();

    HRESULT InitFromHandleNoThrow(HANDLE h);
    void InitFromHandle(HANDLE h); // throws

    // Gets the owner SID from this SecurityDescriptor.
    HRESULT GetOwnerNoThrow( PSID* ppSid );
    Sid GetOwner(); // throws
    Dacl GetDacl(); // throws

protected:
    PSECURITY_DESCRIPTOR m_pDesc;
};


#endif // _SECURITY_WRAPPER_H
