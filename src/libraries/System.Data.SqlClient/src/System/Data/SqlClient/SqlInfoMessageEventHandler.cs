// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//------------------------------------------------------------------------------



namespace System.Data.SqlClient
{
    /// <devdoc>
    ///    <para>
    ///       Represents the method that will handle the <see cref='System.Data.SqlClient.SqlConnection.InfoMessage'/> event of a <see cref='System.Data.SqlClient.SqlConnection'/>.
    ///    </para>
    /// </devdoc>
    public delegate void SqlInfoMessageEventHandler(object sender, SqlInfoMessageEventArgs e);
}
