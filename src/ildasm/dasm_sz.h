//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

#ifndef _DASM_SZ_H_
#define _DASM_SZ_H_

unsigned SizeOfValueType(mdToken tk, IMDInternalImport* pImport);

unsigned SizeOfField(mdToken tk, IMDInternalImport* pImport);

unsigned SizeOfField(PCCOR_SIGNATURE *ppSig, ULONG cSig, IMDInternalImport* pImport);

#endif
