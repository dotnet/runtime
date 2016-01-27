// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//------------------------------------------------------------------------------

namespace System.Data.Common
{
    public abstract class DbException :
        Exception
    {
        protected DbException() : base()
        {
        }

        protected DbException(System.String message) : base(message)
        {
        }

        protected DbException(System.String message, System.Exception innerException) : base(message, innerException)
        {
        }
    }
}
