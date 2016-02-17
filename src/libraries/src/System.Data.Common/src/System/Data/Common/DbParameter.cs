// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//------------------------------------------------------------------------------

using System;
using System.Data;

namespace System.Data.Common
{
    public abstract class DbParameter : 
        IDbDataParameter
    {
        protected DbParameter() : base()
        {
        }

        abstract public DbType DbType
        {
            get;
            set;
        }

        public abstract void ResetDbType();

        abstract public ParameterDirection Direction
        {
            get;
            set;
        }

        abstract public Boolean IsNullable
        {
            get;
            set;
        }

        abstract public String ParameterName
        {
            get;
            set;
        }

        virtual public byte Precision
        {
            get
            {
                return 0;
            }
            set
            {
            }
        }

        virtual public byte Scale
        {
            get
            {
                return 0;
            }
            set
            {
            }
        }

        abstract public int Size
        {
            get;
            set;
        }

        abstract public String SourceColumn
        {
            get;
            set;
        }

        abstract public bool SourceColumnNullMapping
        {
            get;
            set;
        }

        abstract public object Value
        {
            get;
            set;
        }

        public virtual DataRowVersion SourceVersion
        {
            get { return DataRowVersion.Default; }
            set { }
        }
    }
}

