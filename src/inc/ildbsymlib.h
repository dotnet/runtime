// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// IldbSymLib.h
//
// ILDB debug symbols functions implemented in IldbSymLib.lib
// Provides support for ILDB-format debug symbols using the same interfaces
// that diasymreader.dll exposes for PDB symbols.
//
//*****************************************************************************
#ifndef __IldbSymLib_h__
#define __IldbSymLib_h__

// Get the IClassFactory for one of the ILDB symbols CLSIDs
STDAPI IldbSymbolsGetClassObject(REFCLSID rclsid, REFIID riid, void** ppvObject);

// Create an inststance of an ILDB ISymUnmanagedReader, ISymUnmanagedWriter or ISymUnmanagedBinder
STDAPI IldbSymbolsCreateInstance(REFCLSID rclsid, REFIID riid, void** ppvIUnknown);

#endif // __IldbSymLib_h__
