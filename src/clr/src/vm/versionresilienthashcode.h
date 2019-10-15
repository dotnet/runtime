// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

int GetVersionResilientTypeHashCode(TypeHandle type);

bool GetVersionResilientTypeHashCode(IMDInternalImport *pMDImport, mdExportedType token, int * pdwHashCode);

int GetVersionResilientMethodHashCode(MethodDesc *pMD);
