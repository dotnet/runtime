// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Management
{
    [System.ComponentModel.TypeConverter(typeof(ManagementPathConverter))]
    public partial class ManagementPath { }
    internal class ManagementPathConverter { }

    [System.ComponentModel.TypeConverter(typeof(ManagementQueryConverter))]
    public abstract partial class ManagementQuery { }
    internal class ManagementQueryConverter { }

    [System.ComponentModel.TypeConverter(typeof(ManagementScopeConverter))]
    public partial class ManagementScope { }
    internal class ManagementScopeConverter { }
}
