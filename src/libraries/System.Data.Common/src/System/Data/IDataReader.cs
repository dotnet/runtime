//------------------------------------------------------------------------------
// <copyright file="IDataReader.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">markash</owner>
// <owner current="true" primary="false">laled</owner>
//------------------------------------------------------------------------------

namespace System.Data {
    using System;

    public interface IDataReader: IDisposable, IDataRecord {

        int Depth {
            get;
        }

        bool IsClosed {
            get;
        }

        int RecordsAffected {
            get;
        }

        void Close();

        DataTable GetSchemaTable();

        bool NextResult();

        bool Read();
    }
}
