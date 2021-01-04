// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

int GetVersionResilientTypeHashCode(TypeHandle type);

bool GetVersionResilientTypeHashCode(IMDInternalImport *pMDImport, mdExportedType token, int * pdwHashCode);

int GetVersionResilientMethodHashCode(MethodDesc *pMD);

int GetVersionResilientModuleHashCode(Module* pModule);

bool GetVersionResilientILCodeHashCode(MethodDesc *pMD, int* hashCode, unsigned* ilSize);
