// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: 
**
**
===========================================================*/
namespace System.Configuration.Assemblies {
    using System;
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    [Obsolete("The AssemblyHash class has been deprecated. http://go.microsoft.com/fwlink/?linkid=14202")]
    public struct AssemblyHash : ICloneable
    {
        private AssemblyHashAlgorithm _Algorithm;
        private byte[] _Value;
        
        [Obsolete("The AssemblyHash class has been deprecated. http://go.microsoft.com/fwlink/?linkid=14202")]
        public static readonly AssemblyHash Empty = new AssemblyHash(AssemblyHashAlgorithm.None, null);
    
        [Obsolete("The AssemblyHash class has been deprecated. http://go.microsoft.com/fwlink/?linkid=14202")]
        public AssemblyHash(byte[] value) {
            _Algorithm = AssemblyHashAlgorithm.SHA1;
            _Value = null;
    
            if (value != null) {
                int length = value.Length;
                _Value = new byte[length];
                Array.Copy(value, _Value, length);
            }
        }
    
        [Obsolete("The AssemblyHash class has been deprecated. http://go.microsoft.com/fwlink/?linkid=14202")]
        public AssemblyHash(AssemblyHashAlgorithm algorithm, byte[] value) {
            _Algorithm = algorithm;
            _Value = null;
    
            if (value != null) {
                int length = value.Length;
                _Value = new byte[length];
                Array.Copy(value, _Value, length);
            }
        }
    
        // Hash is made up of a byte array and a value from a class of supported 
        // algorithm types.
        [Obsolete("The AssemblyHash class has been deprecated. http://go.microsoft.com/fwlink/?linkid=14202")]
        public AssemblyHashAlgorithm Algorithm {
            get { return _Algorithm; }
            set { _Algorithm = value; }
        }

        [Obsolete("The AssemblyHash class has been deprecated. http://go.microsoft.com/fwlink/?linkid=14202")]
        public byte[] GetValue() {
            return _Value;
        }

        [Obsolete("The AssemblyHash class has been deprecated. http://go.microsoft.com/fwlink/?linkid=14202")]
        public void SetValue(byte[] value) {
            _Value = value;
        }
    
        [Obsolete("The AssemblyHash class has been deprecated. http://go.microsoft.com/fwlink/?linkid=14202")]
        public Object Clone() {
            return new AssemblyHash(_Algorithm, _Value);
        }
    }

}
