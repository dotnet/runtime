// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CSharp.RuntimeBinder.Errors
{
    ////////////////////////////////////////////////////////////////////////////////
    // CParameterizedError
    //
    // This object is the unrealized error that is generated prior to
    // becoming a CError.  It only has the spans, error number, and parameters.

    internal class CParameterizedError
    {
        private ErrorCode _errorNumber;
        private ErrArg[] _arguments;

        public void Initialize(ErrorCode errorNumber, ErrArg[] arguments)
        {
            _errorNumber = errorNumber;
            _arguments = (ErrArg[])arguments.Clone();
        }

        public int GetParameterCount()
        {
            return _arguments.Length;
        }

        public ErrArg GetParameter(int index)
        {
            return _arguments[index];
        }

        public ErrorCode GetErrorNumber()
        {
            return _errorNumber;
        }
    }
}
