// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.



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
