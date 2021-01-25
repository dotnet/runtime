// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.ProviderBase;
using System.Diagnostics;

namespace System.Data.OleDb
{
    internal sealed class OleDbReferenceCollection : DbReferenceCollection
    {
        internal const int Closing = 0;
        internal const int Canceling = -1;

        internal const int CommandTag = 1;
        internal const int DataReaderTag = 2;

        public override void Add(object value, int tag)
        {
            base.AddItem(value, tag);
        }

        protected override void NotifyItem(int message, int tag, object value)
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

        public override void Remove(object value)
        {
            base.RemoveItem(value);
        }

    }
}
