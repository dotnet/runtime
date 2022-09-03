// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

int GetVersionResilientTypeHashCode(TypeHandle type);

bool GetVersionResilientTypeHashCode(IMDInternalImport *pMDImport, mdExportedType token, int * pdwHashCode);

int GetVersionResilientMethodHashCode(MethodDesc *pMD);

// Compute hashCode for a MethodDef (Note, this is not current capable of computing a hashcode for a MethodSpec, or MemberRef)
bool GetVersionResilientMethodDefHashCode(IMDInternalImport *pMDImport, mdExportedType token, int * pdwHashCode);

int GetVersionResilientModuleHashCode(Module* pModule);

bool GetVersionResilientILCodeHashCode(MethodDesc *pMD, int* hashCode, unsigned* ilSize);
