// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Pairing factory for the validation shim. Every COM object the production cDAC hands back is paired
// with the equivalent object the legacy DAC produced for the same operation, and the caller only ever
// sees the paired proxy. Pairing is cached per session so repeated calls return the same proxy and
// interface-pointer identity stays stable for the caller.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

internal abstract partial class ShimProxy
{

    internal static IMetaDataImport2? PairIMetaDataImport2(ValidationSession session, IMetaDataImport2? cdac, IMetaDataImport2? dac)
    {
        object? key = (object?)cdac ?? dac;
        if (key is null)
            return null;

        return session.GetOrCreateProxy(key, cdac, dac,
            static (s, c, d) => new MetaDataImportProxy(s, c, d));
    }

    internal static ISOSHandleEnum? PairISOSHandleEnum(ValidationSession session, ISOSHandleEnum? cdac, ISOSHandleEnum? dac)
    {
        object? key = (object?)cdac ?? dac;
        if (key is null)
            return null;

        return session.GetOrCreateProxy(key, cdac, dac,
            static (s, c, d) => new SOSHandleEnumProxy(s, c, d));
    }

    internal static ISOSMemoryEnum? PairISOSMemoryEnum(ValidationSession session, ISOSMemoryEnum? cdac, ISOSMemoryEnum? dac)
    {
        object? key = (object?)cdac ?? dac;
        if (key is null)
            return null;

        return session.GetOrCreateProxy(key, cdac, dac,
            static (s, c, d) => new SOSMemoryEnumProxy(s, c, d));
    }

    internal static ISOSMethodEnum? PairISOSMethodEnum(ValidationSession session, ISOSMethodEnum? cdac, ISOSMethodEnum? dac)
    {
        object? key = (object?)cdac ?? dac;
        if (key is null)
            return null;

        return session.GetOrCreateProxy(key, cdac, dac,
            static (s, c, d) => new SOSMethodEnumProxy(s, c, d));
    }

    internal static ISOSStackRefEnum? PairISOSStackRefEnum(ValidationSession session, ISOSStackRefEnum? cdac, ISOSStackRefEnum? dac)
    {
        object? key = (object?)cdac ?? dac;
        if (key is null)
            return null;

        return session.GetOrCreateProxy(key, cdac, dac,
            static (s, c, d) => new SOSStackRefEnumProxy(s, c, d));
    }

    internal static ISOSStackRefErrorEnum? PairISOSStackRefErrorEnum(ValidationSession session, ISOSStackRefErrorEnum? cdac, ISOSStackRefErrorEnum? dac)
    {
        object? key = (object?)cdac ?? dac;
        if (key is null)
            return null;

        return session.GetOrCreateProxy(key, cdac, dac,
            static (s, c, d) => new SOSStackRefErrorEnumProxy(s, c, d));
    }

    internal static ISOSStressLogMsgEnum? PairISOSStressLogMsgEnum(ValidationSession session, ISOSStressLogMsgEnum? cdac, ISOSStressLogMsgEnum? dac)
    {
        object? key = (object?)cdac ?? dac;
        if (key is null)
            return null;

        return session.GetOrCreateProxy(key, cdac, dac,
            static (s, c, d) => new SOSStressLogMsgEnumProxy(s, c, d));
    }

    internal static ISOSStressLogThreadEnum? PairISOSStressLogThreadEnum(ValidationSession session, ISOSStressLogThreadEnum? cdac, ISOSStressLogThreadEnum? dac)
    {
        object? key = (object?)cdac ?? dac;
        if (key is null)
            return null;

        return session.GetOrCreateProxy(key, cdac, dac,
            static (s, c, d) => new SOSStressLogThreadEnumProxy(s, c, d));
    }

    internal static IXCLRDataAppDomain? PairIXCLRDataAppDomain(ValidationSession session, IXCLRDataAppDomain? cdac, IXCLRDataAppDomain? dac)
    {
        object? key = (object?)cdac ?? dac;
        if (key is null)
            return null;

        return session.GetOrCreateProxy(key, cdac, dac,
            static (s, c, d) => new ClrDataAppDomainProxy(s, c, d));
    }

    internal static IXCLRDataAssembly? PairIXCLRDataAssembly(ValidationSession session, IXCLRDataAssembly? cdac, IXCLRDataAssembly? dac)
    {
        object? key = (object?)cdac ?? dac;
        if (key is null)
            return null;

        return session.GetOrCreateProxy(key, cdac, dac,
            static (s, c, d) => new ClrDataAssemblyProxy(s, c, d));
    }

    internal static IXCLRDataExceptionState? PairIXCLRDataExceptionState(ValidationSession session, IXCLRDataExceptionState? cdac, IXCLRDataExceptionState? dac)
    {
        object? key = (object?)cdac ?? dac;
        if (key is null)
            return null;

        return session.GetOrCreateProxy(key, cdac, dac,
            static (s, c, d) => new ClrDataExceptionStateProxy(s, c, d));
    }

    internal static IXCLRDataFrame? PairIXCLRDataFrame(ValidationSession session, IXCLRDataFrame? cdac, IXCLRDataFrame? dac)
    {
        object? key = (object?)cdac ?? dac;
        if (key is null)
            return null;

        return session.GetOrCreateProxy(key, cdac, dac,
            static (s, c, d) => new ClrDataFrameProxy(s, c, d));
    }

    internal static IXCLRDataMethodDefinition? PairIXCLRDataMethodDefinition(ValidationSession session, IXCLRDataMethodDefinition? cdac, IXCLRDataMethodDefinition? dac)
    {
        object? key = (object?)cdac ?? dac;
        if (key is null)
            return null;

        return session.GetOrCreateProxy(key, cdac, dac,
            static (s, c, d) => new ClrDataMethodDefinitionProxy(s, c, d));
    }

    internal static IXCLRDataMethodInstance? PairIXCLRDataMethodInstance(ValidationSession session, IXCLRDataMethodInstance? cdac, IXCLRDataMethodInstance? dac)
    {
        object? key = (object?)cdac ?? dac;
        if (key is null)
            return null;

        return session.GetOrCreateProxy(key, cdac, dac,
            static (s, c, d) => new ClrDataMethodInstanceProxy(s, c, d));
    }

    internal static IXCLRDataModule? PairIXCLRDataModule(ValidationSession session, IXCLRDataModule? cdac, IXCLRDataModule? dac)
    {
        object? key = (object?)cdac ?? dac;
        if (key is null)
            return null;

        return session.GetOrCreateProxy(key, cdac, dac,
            static (s, c, d) => new ClrDataModuleProxy(s, c, d));
    }

    internal static IXCLRDataProcess? PairIXCLRDataProcess(ValidationSession session, IXCLRDataProcess? cdac, IXCLRDataProcess? dac)
    {
        object? key = (object?)cdac ?? dac;
        if (key is null)
            return null;

        return session.GetOrCreateProxy(key, cdac, dac,
            static (s, c, d) => new SOSDacImplProxy(s, c, d));
    }

    internal static IXCLRDataStackWalk? PairIXCLRDataStackWalk(ValidationSession session, IXCLRDataStackWalk? cdac, IXCLRDataStackWalk? dac)
    {
        object? key = (object?)cdac ?? dac;
        if (key is null)
            return null;

        return session.GetOrCreateProxy(key, cdac, dac,
            static (s, c, d) => new ClrDataStackWalkProxy(s, c, d));
    }

    internal static IXCLRDataTask? PairIXCLRDataTask(ValidationSession session, IXCLRDataTask? cdac, IXCLRDataTask? dac)
    {
        object? key = (object?)cdac ?? dac;
        if (key is null)
            return null;

        return session.GetOrCreateProxy(key, cdac, dac,
            static (s, c, d) => new ClrDataTaskProxy(s, c, d));
    }

    internal static IXCLRDataTypeDefinition? PairIXCLRDataTypeDefinition(ValidationSession session, IXCLRDataTypeDefinition? cdac, IXCLRDataTypeDefinition? dac)
    {
        object? key = (object?)cdac ?? dac;
        if (key is null)
            return null;

        return session.GetOrCreateProxy(key, cdac, dac,
            static (s, c, d) => new ClrDataTypeDefinitionProxy(s, c, d));
    }

    internal static IXCLRDataTypeInstance? PairIXCLRDataTypeInstance(ValidationSession session, IXCLRDataTypeInstance? cdac, IXCLRDataTypeInstance? dac)
    {
        object? key = (object?)cdac ?? dac;
        if (key is null)
            return null;

        return session.GetOrCreateProxy(key, cdac, dac,
            static (s, c, d) => new ClrDataTypeInstanceProxy(s, c, d));
    }

    internal static IXCLRDataValue? PairIXCLRDataValue(ValidationSession session, IXCLRDataValue? cdac, IXCLRDataValue? dac)
    {
        object? key = (object?)cdac ?? dac;
        if (key is null)
            return null;

        return session.GetOrCreateProxy(key, cdac, dac,
            static (s, c, d) => new ClrDataValueProxy(s, c, d));
    }

}
