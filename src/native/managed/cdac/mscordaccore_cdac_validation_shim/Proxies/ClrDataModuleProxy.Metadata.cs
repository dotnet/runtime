// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

internal sealed unsafe partial class ClrDataModuleProxy
{
    private MetaDataImportProxy? _metaDataImportProxy;

    /// <summary>
    /// The DAC answers a QI for <c>IMetaDataImport</c> on a module by handing out a separate metadata
    /// object rather than aggregating, so the shim has to pair the two metadata objects the same way
    /// it pairs every other child. As in the production cDAC, the <c>IMetaDataImport2</c> vtable is
    /// returned for an <c>IMetaDataImport</c> request because consumers such as ClrMD call through
    /// <c>IMetaDataImport2</c> slots on a pointer they obtained as <c>IMetaDataImport</c>.
    /// </summary>
    partial void GetCustomInterface(ref Guid iid, ref nint ppv, ref CustomQueryInterfaceResult? result)
    {
        if (iid != typeof(IMetaDataImport).GUID)
            return;

        MetaDataImportProxy? proxy = _metaDataImportProxy;
        if (proxy is null)
        {
            IMetaDataImport2? cdacImport = QueryMetaDataImport(CDacObject);
            IMetaDataImport2? dacImport = QueryMetaDataImport(DacObject);
            if (cdacImport is null && dacImport is null)
            {
                result = CustomQueryInterfaceResult.Failed;
                return;
            }

            proxy = (MetaDataImportProxy?)ShimProxy.PairIMetaDataImport2(_session, cdacImport, dacImport);
            _metaDataImportProxy ??= proxy;
            proxy = _metaDataImportProxy;
        }

        if (proxy is null)
        {
            result = CustomQueryInterfaceResult.Failed;
            return;
        }

        ppv = (nint)ComInterfaceMarshaller<IMetaDataImport2>.ConvertToUnmanaged(proxy);
        result = CustomQueryInterfaceResult.Handled;
    }

    private static IMetaDataImport2? QueryMetaDataImport(object? module)
    {
        if (module is null)
            return null;

        // The underlying objects answer the QI for IMetaDataImport with an IMetaDataImport2 vtable
        // (see the production cDAC's ClrDataModule and the native DAC's ClrDataModule::QueryInterface),
        // so requesting IMetaDataImport2 directly is not sufficient - go through IMetaDataImport.
        IMetaDataImport? import = module as IMetaDataImport;
        return import as IMetaDataImport2 ?? module as IMetaDataImport2;
    }
}
