// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Data
{
    internal delegate void DataRowCreatedEventHandler(object sender, DataRow r);
    internal delegate void DataSetClearEventhandler(object sender, DataTable? table);
}
