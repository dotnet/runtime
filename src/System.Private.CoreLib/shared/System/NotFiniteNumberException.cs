// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Runtime.Serialization;

namespace System
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class NotFiniteNumberException : ArithmeticException
    {
        private double _offendingNumber;

        public NotFiniteNumberException()
            : base(SR.Arg_NotFiniteNumberException)
        {
            _offendingNumber = 0;
            HResult = HResults.COR_E_NOTFINITENUMBER;
        }

        public NotFiniteNumberException(double offendingNumber)
            : base()
        {
            _offendingNumber = offendingNumber;
            HResult = HResults.COR_E_NOTFINITENUMBER;
        }

        public NotFiniteNumberException(string? message)
            : base(message)
        {
            _offendingNumber = 0;
            HResult = HResults.COR_E_NOTFINITENUMBER;
        }

        public NotFiniteNumberException(string? message, double offendingNumber)
            : base(message)
        {
            _offendingNumber = offendingNumber;
            HResult = HResults.COR_E_NOTFINITENUMBER;
        }

        public NotFiniteNumberException(string? message, Exception? innerException)
            : base(message, innerException)
        {
            HResult = HResults.COR_E_NOTFINITENUMBER;
        }

        public NotFiniteNumberException(string? message, double offendingNumber, Exception? innerException)
            : base(message, innerException)
        {
            _offendingNumber = offendingNumber;
            HResult = HResults.COR_E_NOTFINITENUMBER;
        }

        protected NotFiniteNumberException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            _offendingNumber = info.GetDouble("OffendingNumber"); // Do not rename (binary serialization)
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("OffendingNumber", _offendingNumber, typeof(double)); // Do not rename (binary serialization)
        }

        public double OffendingNumber
        {
            get { return _offendingNumber; }
        }
    }
}
