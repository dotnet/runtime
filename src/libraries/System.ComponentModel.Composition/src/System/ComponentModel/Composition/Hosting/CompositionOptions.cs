// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.Composition.Hosting
{
    /// <summary>
    ///     Defines the Constructor settings for export providers.
    /// </summary>
    [Flags]
    public enum CompositionOptions
    {
        Default                = 0x0000,
        DisableSilentRejection = 0x0001,
        IsThreadSafe           = 0x0002,
        ExportCompositionService = 0x0004
    }
}
