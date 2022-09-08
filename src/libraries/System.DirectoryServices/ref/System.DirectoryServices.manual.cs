// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.DirectoryServices
{
    using System.DirectoryServices.Design;
    
    [System.ComponentModel.TypeConverter(typeof(DirectoryEntryConverter))]
    public partial class DirectoryEntry { }
}

namespace System.DirectoryServices.Design
{
    internal sealed class DirectoryEntryConverter { }
}