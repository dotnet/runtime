// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Data
{
    public sealed class StatementCompletedEventArgs : EventArgs
    {
        public StatementCompletedEventArgs(int recordCount)
        {
            RecordCount = recordCount;
        }

        public int RecordCount { get; }
    }
}
