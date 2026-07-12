// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader;

/// <summary>
/// Base exception for failures to retrieve a data contract from a target.
/// </summary>
public abstract class ContractNotAvailableException : Exception
{
    private const int E_NOTIMPL = unchecked((int)0x80004001);

    /// <summary>
    /// Initializes a new instance of the <see cref="ContractNotAvailableException"/> class.
    /// </summary>
    /// <param name="contractName">The name of the requested contract.</param>
    /// <param name="contractVersion">The target-advertised version of the requested contract, or <see langword="null"/> if the target did not advertise the contract.</param>
    /// <param name="message">The exception message.</param>
    protected ContractNotAvailableException(string contractName, string? contractVersion, string message)
        : base(message)
    {
        ContractName = contractName;
        ContractVersion = contractVersion;
        HResult = E_NOTIMPL;
    }

    /// <summary>
    /// Gets the name of the requested contract.
    /// </summary>
    public string ContractName { get; }

    /// <summary>
    /// Gets the target-advertised version of the requested contract, or <see langword="null"/> if the target did not advertise the contract.
    /// </summary>
    public string? ContractVersion { get; }
}

/// <summary>
/// Exception thrown when the target's contract descriptor does not advertise the requested contract.
/// </summary>
public sealed class ContractMissingException : ContractNotAvailableException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContractMissingException"/> class.
    /// </summary>
    /// <param name="contractName">The name of the requested contract.</param>
    public ContractMissingException(string contractName)
        : base(contractName, null, $"Contract '{contractName}' is not advertised by the target.")
    { }
}

/// <summary>
/// Exception thrown when the target advertises the requested contract, but this cDAC cannot provide an implementation for the advertised version.
/// </summary>
public abstract class ContractUnsupportedException : ContractNotAvailableException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContractUnsupportedException"/> class.
    /// </summary>
    /// <param name="contractName">The name of the requested contract.</param>
    /// <param name="contractVersion">The target-advertised version of the requested contract.</param>
    /// <param name="message">The exception message.</param>
    protected ContractUnsupportedException(string contractName, string contractVersion, string message)
        : base(contractName, contractVersion, message)
    { }
}

/// <summary>
/// Exception thrown when the target advertises a contract version that this cDAC does not recognize, typically because the target runtime is newer than this cDAC.
/// </summary>
public sealed class ContractUnrecognizedException : ContractUnsupportedException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContractUnrecognizedException"/> class.
    /// </summary>
    /// <param name="contractName">The name of the requested contract.</param>
    /// <param name="contractVersion">The target-advertised version of the requested contract.</param>
    public ContractUnrecognizedException(string contractName, string contractVersion)
        : base(contractName, contractVersion, $"Contract '{contractName}' version {contractVersion} is advertised by the target but is not recognized by this cDAC.")
    { }
}

/// <summary>
/// Exception thrown when the target advertises a contract version that this cDAC recognizes but intentionally does not implement, typically because the target runtime is too old.
/// </summary>
public sealed class ContractObsoleteException : ContractUnsupportedException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContractObsoleteException"/> class.
    /// </summary>
    /// <param name="contractName">The name of the requested contract.</param>
    /// <param name="contractVersion">The target-advertised version of the requested contract.</param>
    public ContractObsoleteException(string contractName, string contractVersion)
        : base(contractName, contractVersion, $"Contract '{contractName}' version {contractVersion} is advertised by the target and recognized by this cDAC, but is intentionally not implemented.")
    { }
}

/// <summary>
/// Exception thrown when eager validation of the contracts required to service an SOS /
/// <c>IXCLRDataProcess</c> interface fails during creation. Unlike the lazy
/// <see cref="ContractNotAvailableException"/> hierarchy (which carries <c>E_NOTIMPL</c> so that
/// individual SOS APIs degrade gracefully), this exception carries a distinct <see cref="CdacHResults"/>
/// value identifying the failure category so the native loader can decide how to proceed.
/// </summary>
public sealed class ContractValidationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContractValidationException"/> class.
    /// </summary>
    /// <param name="hResult">The <see cref="CdacHResults"/> value describing the validation failure category.</param>
    /// <param name="inner">The underlying <see cref="ContractNotAvailableException"/> describing which contract failed and why.</param>
    public ContractValidationException(int hResult, ContractNotAvailableException inner)
        : base(inner.Message, inner)
    {
        HResult = hResult;
        ContractName = inner.ContractName;
        ContractVersion = inner.ContractVersion;
    }

    /// <summary>
    /// Gets the name of the contract whose validation failed.
    /// </summary>
    public string ContractName { get; }

    /// <summary>
    /// Gets the target-advertised version of the contract whose validation failed, or <see langword="null"/> if the target did not advertise the contract.
    /// </summary>
    public string? ContractVersion { get; }
}
