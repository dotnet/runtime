// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//------------------------------------------------------------------------------

using System.Diagnostics;
using System.Data.ProviderBase;

namespace System.Data.SqlClient
{
    sealed internal class SqlReferenceCollection : DbReferenceCollection
    {
        internal const int DataReaderTag = 1;
        internal const int CommandTag = 2;
        internal const int BulkCopyTag = 3;

        override public void Add(object value, int tag)
        {
            Debug.Assert(DataReaderTag == tag || CommandTag == tag || BulkCopyTag == tag, "unexpected tag?");
            Debug.Assert(DataReaderTag != tag || value is SqlDataReader, "tag doesn't match object type: SqlDataReader");
            Debug.Assert(CommandTag != tag || value is SqlCommand, "tag doesn't match object type: SqlCommand");
            Debug.Assert(BulkCopyTag != tag || value is SqlBulkCopy, "tag doesn't match object type: SqlBulkCopy");

            base.AddItem(value, tag);
        }

        internal void Deactivate()
        {
            base.Notify(0);
        }

        internal SqlDataReader FindLiveReader(SqlCommand command)
        {
            if (command == null)
            {
                // if null == command, will find first live datareader
                return FindItem<SqlDataReader>(DataReaderTag, (dataReader) => (!dataReader.IsClosed));
            }
            else
            {
                // else will find live datareader associated with the command
                return FindItem<SqlDataReader>(DataReaderTag, (dataReader) => ((!dataReader.IsClosed) && (command == dataReader.Command)));
            }
        }

        // Finds a SqlCommand associated with the given StateObject
        internal SqlCommand FindLiveCommand(TdsParserStateObject stateObj)
        {
            return FindItem<SqlCommand>(CommandTag, (command) => (command.StateObject == stateObj));
        }

        override protected void NotifyItem(int message, int tag, object value)
        {
            Debug.Assert(0 == message, "unexpected message?");
            Debug.Assert(DataReaderTag == tag || CommandTag == tag || BulkCopyTag == tag, "unexpected tag?");

            if (tag == DataReaderTag)
            {
                Debug.Assert(value is SqlDataReader, "Incorrect object type");
                var rdr = (SqlDataReader)value;
                if (!rdr.IsClosed)
                {
                    rdr.CloseReaderFromConnection();
                }
            }
            else if (tag == CommandTag)
            {
                Debug.Assert(value is SqlCommand, "Incorrect object type");
                ((SqlCommand)value).OnConnectionClosed();
            }
            else if (tag == BulkCopyTag)
            {
                Debug.Assert(value is SqlBulkCopy, "Incorrect object type");
                ((SqlBulkCopy)value).OnConnectionClosed();
            }
        }

        override public void Remove(object value)
        {
            Debug.Assert(value is SqlDataReader || value is SqlCommand || value is SqlBulkCopy, "SqlReferenceCollection.Remove expected a SqlDataReader or SqlCommand or SqlBulkCopy");

            base.RemoveItem(value);
        }
    }
}
