// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.



//------------------------------------------------------------------------------


namespace System.Data
{
    public sealed class StatementCompletedEventArgs : System.EventArgs
    {
        private readonly int _recordCount;

        public StatementCompletedEventArgs(int recordCount)
        {
            _recordCount = recordCount;
        }

        public int RecordCount
        {
            get
            {
                return _recordCount;
            }
        }
    }
}

