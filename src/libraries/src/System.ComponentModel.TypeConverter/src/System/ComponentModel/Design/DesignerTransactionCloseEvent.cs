// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Security.Permissions;

namespace System.ComponentModel.Design
{
    /// <summary>
    ///    <para>[To be supplied.]</para>
    /// </summary>
    public class DesignerTransactionCloseEventArgs : EventArgs
    {
        /// <summary>
        ///     Creates a new event args.  Commit is true if the transaction is committed.  This
        ///     defaults the LastTransaction property to true.
        /// </summary>
        [Obsolete("This constructor is obsolete. Use DesignerTransactionCloseEventArgs(bool, bool) instead.  http://go.microsoft.com/fwlink/?linkid=14202")]
        public DesignerTransactionCloseEventArgs(bool commit) : this(commit, true)
        {
        }

        /// <summary>
        ///     Creates a new event args.  Commit is true if the transaction is committed, and
        ///     lastTransaction is true if this is the last transaction to close.
        /// </summary>
        public DesignerTransactionCloseEventArgs(bool commit, bool lastTransaction)
        {
            TransactionCommitted = commit;
            LastTransaction = lastTransaction;
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public bool TransactionCommitted { get; }

        /// <summary>
        ///    Returns true if this is the last transaction to close.
        /// </summary>
        public bool LastTransaction { get; }
    }
}
