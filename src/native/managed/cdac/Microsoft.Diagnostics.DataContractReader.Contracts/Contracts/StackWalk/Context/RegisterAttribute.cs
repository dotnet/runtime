// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

[Flags]
public enum RegisterType : byte
{
    General = 0x01,
    Control = 0x02,
    Segments = 0x03,
    FloatingPoint = 0x04,
    Debug = 0x05,
    TypeMask = 0x0f,

    ProgramCounter = 0x10,
    StackPointer = 0x20,
    FramePointer = 0x40,
}


[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class RegisterAttribute : Attribute
{
    /// <summary>
    /// Gets or sets optional name override
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets register type and flags
    /// </summary>
    public RegisterType RegisterType { get; }

    public RegisterAttribute(RegisterType registerType)
    {
        RegisterType = registerType;
    }
}
