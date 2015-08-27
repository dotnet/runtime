// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.



//------------------------------------------------------------------------------

using Res = System.SR;


namespace System.Data
{
    public sealed class OperationAbortedException : Exception
    {
        private OperationAbortedException(string message, Exception innerException) : base(message, innerException)
        {
            HResult = unchecked((int)0x80131936);
        }


        static internal OperationAbortedException Aborted(Exception inner)
        {
            OperationAbortedException e;
            if (inner == null)
            {
                e = new OperationAbortedException(Res.GetString(Res.ADP_OperationAborted), null);
            }
            else
            {
                e = new OperationAbortedException(Res.GetString(Res.ADP_OperationAbortedExceptionMessage), inner);
            }
            return e;
        }
    }
}
