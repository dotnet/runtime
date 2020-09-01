// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.Serialization
{
    public interface IExtensibleDataObject
    {
        ExtensionDataObject? ExtensionData { get; set; }
    }
}
