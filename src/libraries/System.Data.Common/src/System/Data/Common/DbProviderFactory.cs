// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.



//------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Security;


namespace System.Data.Common
{
    public abstract class DbProviderFactory
    {
        protected DbProviderFactory()
        {
        }


        public virtual DbCommand CreateCommand()
        {
            return null;
        }


        public virtual DbConnection CreateConnection()
        {
            return null;
        }

        public virtual DbConnectionStringBuilder CreateConnectionStringBuilder()
        {
            return null;
        }


        public virtual DbParameter CreateParameter()
        {
            return null;
        }
    }
}

