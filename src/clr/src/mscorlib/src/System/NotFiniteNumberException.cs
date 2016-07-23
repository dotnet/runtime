// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System {
    
    using System;
    using System.Runtime.Serialization;
    using System.Security.Permissions;
    using System.Diagnostics.Contracts;

    [System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    public class NotFiniteNumberException : ArithmeticException {
        private double _offendingNumber;    
    
        public NotFiniteNumberException() 
            : base(Environment.GetResourceString("Arg_NotFiniteNumberException")) {
            _offendingNumber = 0;
            SetErrorCode(__HResults.COR_E_NOTFINITENUMBER);
        }

        public NotFiniteNumberException(double offendingNumber) 
            : base() {
            _offendingNumber = offendingNumber;
            SetErrorCode(__HResults.COR_E_NOTFINITENUMBER);
        }

        public NotFiniteNumberException(String message) 
            : base(message) {
            _offendingNumber = 0;
            SetErrorCode(__HResults.COR_E_NOTFINITENUMBER);
        }

        public NotFiniteNumberException(String message, double offendingNumber) 
            : base(message) {
            _offendingNumber = offendingNumber;
            SetErrorCode(__HResults.COR_E_NOTFINITENUMBER);
        }

        public NotFiniteNumberException(String message, Exception innerException) 
            : base(message, innerException) {
            SetErrorCode(__HResults.COR_E_NOTFINITENUMBER);
        }
        
        public NotFiniteNumberException(String message, double offendingNumber, Exception innerException) 
            : base(message, innerException) {
            _offendingNumber = offendingNumber;
            SetErrorCode(__HResults.COR_E_NOTFINITENUMBER);
        }

        protected NotFiniteNumberException(SerializationInfo info, StreamingContext context) : base(info, context) {
            _offendingNumber = info.GetInt32("OffendingNumber");
        }

        public double OffendingNumber {
            get { return _offendingNumber; }
        }

        [System.Security.SecurityCritical]  // auto-generated_required
        public override void GetObjectData(SerializationInfo info, StreamingContext context) {
            if (info==null) {
                throw new ArgumentNullException("info");
            }
            Contract.EndContractBlock();
            base.GetObjectData(info, context);
            info.AddValue("OffendingNumber", _offendingNumber, typeof(Int32));
        }
    }
}
