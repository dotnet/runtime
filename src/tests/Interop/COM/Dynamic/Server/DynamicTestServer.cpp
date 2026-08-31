// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <ComHelpers.h>
#include <Contract.h>

#include "BasicTest.h"
#include "CollectionTest.h"
#include "EventTest.h"
#include "ParametersTest.h"

STDAPI DllGetClassObject(_In_ REFCLSID rclsid, _In_ REFIID riid, _Out_ LPVOID FAR* ppv)
{
    if (rclsid == __uuidof(BasicTest))
         return ClassFactoryBasic<BasicTest>::Create(riid, ppv);

    if (rclsid == __uuidof(CollectionTest))
         return ClassFactoryBasic<CollectionTest>::Create(riid, ppv);

    if (rclsid == __uuidof(EventTest))
         return ClassFactoryBasic<EventTest>::Create(riid, ppv);

    if (rclsid == __uuidof(ParametersTest))
         return ClassFactoryBasic<ParametersTest>::Create(riid, ppv);

    return CLASS_E_CLASSNOTAVAILABLE;
}
