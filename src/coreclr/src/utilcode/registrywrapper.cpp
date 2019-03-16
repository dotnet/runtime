// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// File: registrywrapper.cpp
//

//
// Wrapper around Win32 Registry Functions allowing redirection of .NET 
// Framework root registry location
//
// Notes on Offline Ngen Implementation:
//
// This implementation redirects file accesses to the GAC, NIC, framework directory
// and registry accesses to root store and fusion.
// Essentially, if we open a file or reg key directly from the CLR, we redirect it
// into the mounted VHD specified in the COMPLUS config values.
//
// Terminology:
//  Host Machine - The machine running a copy of windows that mounts a VHD to 
//      compile the assemblies within.  This is the build machine in the build lab.
//
//  Target Machine - The VHD that gets mounted inside the host.  We compile
//      native images storing them inside the target.  This is the freshly build
//      copy of windows in the build lab.
//
// The OS itself pulls open all manner of registry keys and files as side-effects
// of our API calls. Here is a list of things the redirection implementation does
// not cover:
//
// - COM
//      We use COM in Ngen to create and communicate with worker processes.  In
//      order to marshal arguments between ngen and worker, mscoree.tlb is loaded.
//      The COM system from the loaded and running OS is used, which means the host
//      machine's mscoree.tlb gets loaded for marshalling arguments for the CLR
//      running on the target machine. In the next release (4.5) the ngen interfaces
//      don't expect to change for existing ngen operations.  If new functionality
//      is added, new interfaces would be added, but existing ones will not be
//      altered since we have a high compat bar with an inplace release.  mscoree.tlb
//      also contains interfaces for mscoree shim and gchost which again we expect to
//      remain compatible in this next product release.  In order to fix this, we will
//      need support from Windows for using the COM system on another copy of Windows.
// - Registry Accesses under
//      - HKLM\software[\Wow6432Node]\policies\microsoft : SQM, Cryptography, MUI, codeidentifiers, appcompat, RPC 
//      - HKLM\software[\Wow6432Node]\RPC,OLE,COM and under these keys
//      - HKLM\Software\Microsoft\Cryptography and under
//      - HKLM\Software\Microsoft\SQMClient
//      - HKLM\Software[\Wow6432Node]\Microsoft\Windows\Windows Error Reporting\WMR and under 
//
//      These locations are not accessed directly by the CLR, but looked up by Windows
//      as part of other API calls.  It is safer that we are accessing these
//      on the host machine since they correspond with the actively running copy of 
//      Windows.  If we could somehow redirect these to the target VM, we would have
//      Windows 7/2K8 OS looking up its config keys from a Win8 registry.  If Windows
//      has made incompatible changes here, such as moving the location or redefining
//      values, we would break.
//  - Accesses from C:\Windows\System32 and C:\Windows\Syswow64 and HKCU
//      HKCU does not contain any .NET Framework settings (Microsoft\.NETFramework
//      is empty).
//      There are various files accessed from C:\Windows\System32 and these are a
//      function of the OS loader.  We load an executable and it automatically
//      pulls in kernel32.dll, for example.  This should not be a problem for running
//      the CLR, since v4.5 will run on Win2K8, and for offline ngen compilation, we
//      will not be using the new Win8 APIs for AppX.  We had considered setting
//      the PATH to point into the target machine's System32 directory, but the
//      Windows team advised us that would break pretty quickly due to Api-sets
//      having their version numbers rev'd and the Win2k8 host machine not having
//      the ability to load them.
//
//
//*****************************************************************************
#include "stdafx.h"
#include "registrywrapper.h"
#include "clrconfig.h"
#include "strsafe.h"

