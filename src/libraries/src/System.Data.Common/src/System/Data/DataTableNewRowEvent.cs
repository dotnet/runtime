// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Data
{
    public sealed class DataTableNewRowEventArgs : EventArgs
    {
        public DataTableNewRowEventArgs(DataRow dataRow)
        {
            Row = dataRow;
        }

        public DataRow Row { get; }
    }
}
