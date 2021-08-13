// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//

#ifndef _REFCLASSWRITER_H_
#define _REFCLASSWRITER_H_

#include "iceefilegen.h"

// RefClassWriter
// This will create a Class
class RefClassWriter {
protected:
    friend class COMDynamicWrite;
	IMetaDataEmit2*			m_emitter;			// Emit interface.
	IMetaDataImport*		m_importer;			// Import interface.
	IMDInternalImport*		m_internalimport;	// Scopeless internal import interface
    ICeeGenInternal*	    m_pCeeGen;
    ICeeFileGen*            m_pCeeFileGen;
    HCEEFILE                m_ceeFile;
	IMetaDataEmitHelper*	m_pEmitHelper;
	ULONG					m_ulResourceSize;
    mdFile                  m_tkFile;

public:
    RefClassWriter() {
        LIMITED_METHOD_CONTRACT;
    }

	HRESULT		Init(ICeeGenInternal *pCeeGen, IUnknown *pUnk, LPCWSTR szName);

	IMetaDataEmit2* GetEmitter() {
        LIMITED_METHOD_CONTRACT;
		return m_emitter;
	}

	IMetaDataEmitHelper* GetEmitHelper() {
        LIMITED_METHOD_CONTRACT;
		return m_pEmitHelper;
	}

	IMetaDataImport* GetRWImporter() {
        LIMITED_METHOD_CONTRACT;
		return m_importer;
	}

	IMDInternalImport* GetMDImport() {
        LIMITED_METHOD_CONTRACT;
		return m_internalimport;
	}

    ICeeGenInternal* GetCeeGen() {
        LIMITED_METHOD_CONTRACT;
		return m_pCeeGen;
	}

	ICeeFileGen* GetCeeFileGen() {
        LIMITED_METHOD_CONTRACT;
		return m_pCeeFileGen;
	}

	HCEEFILE GetHCEEFILE() {
        LIMITED_METHOD_CONTRACT;
		return m_ceeFile;
	}

	~RefClassWriter();
};

#endif	// _REFCLASSWRITER_H_
