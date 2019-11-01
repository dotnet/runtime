// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _DASM_SZ_H_
#define _DASM_SZ_H_

unsigned SizeOfValueType(mdToken tk, IMDInternalImport* pImport);

unsigned SizeOfField(mdToken tk, IMDInternalImport* pImport);

unsigned SizeOfField(PCCOR_SIGNATURE *ppSig, ULONG cSig, IMDInternalImport* pImport);

#endif
