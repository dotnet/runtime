// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;

namespace System.Data.ProviderBase
{
    internal abstract partial class DbConnectionClosed : DbConnectionInternal
    {
        protected override void Activate() => throw ADP.ClosedConnectionError();

    }

}
