// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.



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
