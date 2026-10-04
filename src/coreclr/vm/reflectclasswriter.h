// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//

#ifndef _REFCLASSWRITER_H_
#define _REFCLASSWRITER_H_

// RefClassWriter
// This will create a Class
class RefClassWriter {
protected:
    friend class COMDynamicWrite;
	IMDInternalEmit*		m_emitter;			// Emit interface.
	IMDInternalImport*		m_internalimport;	// Scopeless internal import interface
	ULONG					m_ulResourceSize;

public:
    RefClassWriter() {
        LIMITED_METHOD_CONTRACT;
    }

	HRESULT		Init(
        IMDInternalEmit *pEmitter,
        IMDInternalImport *pInternalImport,
        LPCWSTR szName);

	IMDInternalEmit* GetEmitter() {
        LIMITED_METHOD_CONTRACT;
		return m_emitter;
	}

	IMDInternalImport* GetMDImport() {
        LIMITED_METHOD_CONTRACT;
		return m_internalimport;
	}

	~RefClassWriter();
};

#endif	// _REFCLASSWRITER_H_
