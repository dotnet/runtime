// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data.ProviderBase;
using System.Diagnostics;

namespace System.Data.OleDb
{
    sealed internal class OleDbReferenceCollection : DbReferenceCollection
    {
        internal const int Closing = 0;
        internal const int Canceling = -1;

        internal const int CommandTag = 1;
        internal const int DataReaderTag = 2;

        override public void Add(object value, int tag)
        {
            base.AddItem(value, tag);
        }

        override protected void NotifyItem(int message, int tag, object value)
        {
            bool canceling = (Canceling == message);
            if (CommandTag == tag)
            {
                ((OleDbCommand)value).CloseCommandFromConnection(canceling);
            }
            else if (DataReaderTag == tag)
            {
                ((OleDbDataReader)value).CloseReaderFromConnection(canceling);
            }
            else
            {
                Debug.Assert(false, "shouldn't be here");
            }
        }

        override public void Remove(object value)
        {
            base.RemoveItem(value);
        }

    }
}
