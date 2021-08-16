// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Configuration
{
    internal sealed class DeclarationUpdate : Update
    {
        internal DeclarationUpdate(string configKey, bool moved, string updatedXml) : base(configKey, moved, updatedXml) { }
    }
}
