// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System
{
    public class NotFiniteNumberException : ArithmeticException
    {
        private double _offendingNumber;

        public NotFiniteNumberException()
            : base(SR.Arg_NotFiniteNumberException)
        {
            _offendingNumber = 0;
            HResult = __HResults.COR_E_NOTFINITENUMBER;
        }

        public NotFiniteNumberException(double offendingNumber)
            : base()
        {
            _offendingNumber = offendingNumber;
            HResult = __HResults.COR_E_NOTFINITENUMBER;
        }

        public NotFiniteNumberException(String message)
            : base(message)
        {
            _offendingNumber = 0;
            HResult = __HResults.COR_E_NOTFINITENUMBER;
        }

        public NotFiniteNumberException(String message, double offendingNumber)
            : base(message)
        {
            _offendingNumber = offendingNumber;
            HResult = __HResults.COR_E_NOTFINITENUMBER;
        }

        public NotFiniteNumberException(String message, Exception innerException)
            : base(message, innerException)
        {
            HResult = __HResults.COR_E_NOTFINITENUMBER;
        }

        public NotFiniteNumberException(String message, double offendingNumber, Exception innerException)
            : base(message, innerException)
        {
            _offendingNumber = offendingNumber;
            HResult = __HResults.COR_E_NOTFINITENUMBER;
        }

        protected NotFiniteNumberException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            throw new PlatformNotSupportedException();
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

        public double OffendingNumber
        {
            get { return _offendingNumber; }
        }
    }
}
