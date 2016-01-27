// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



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

