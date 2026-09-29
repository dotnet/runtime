// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _METADATAEMITHELPER_H_
#define _METADATAEMITHELPER_H_

#include "cor.h"

EXTERN_GUID(IID_IMetaDataEmitHelper, 0x5c240ae4, 0x1e09, 0x11d3, 0x94, 0x24, 0x0, 0x0, 0xf8, 0x8, 0x34, 0x60);

#undef INTERFACE
#define INTERFACE IMetaDataEmitHelper
DECLARE_INTERFACE_(IMetaDataEmitHelper, IUnknown)
{
    STDMETHOD(DefineMethodSemanticsHelper)(
        mdToken tkAssociation,
        DWORD dwFlags,
        mdMethodDef md) PURE;

    STDMETHOD(SetFieldLayoutHelper)(
        mdFieldDef fd,
        ULONG ulOffset) PURE;

    STDMETHOD(DefineEventHelper)(
        mdTypeDef td,
        LPCWSTR szEvent,
        DWORD dwEventFlags,
        mdToken tkEventType,
        mdEvent *pmdEvent) PURE;

    STDMETHOD(AddDeclarativeSecurityHelper)(
        mdToken tk,
        DWORD dwAction,
        void const *pValue,
        DWORD cbValue,
        mdPermission *pmdPermission) PURE;

    STDMETHOD(SetResolutionScopeHelper)(
        mdTypeRef tr,
        mdToken rs) PURE;

    STDMETHOD(SetManifestResourceOffsetHelper)(
        mdManifestResource mr,
        ULONG ulOffset) PURE;

    STDMETHOD(SetTypeParent)(
        mdTypeDef td,
        mdToken tkExtends) PURE;

    STDMETHOD(AddInterfaceImpl)(
        mdTypeDef td,
        mdToken tkInterface) PURE;
};

#endif // _METADATAEMITHELPER_H_
