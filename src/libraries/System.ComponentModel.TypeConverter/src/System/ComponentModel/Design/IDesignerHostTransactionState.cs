// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.ComponentModel.Design
{
    /// <summary>
    ///    <para>
    ///       Methods for the Designer host to report on the state of transactions.
    ///    </para>
    /// </summary>
    public interface IDesignerHostTransactionState
    {
        bool IsClosingTransaction { get; }
    }
}

