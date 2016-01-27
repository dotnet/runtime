// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.ComponentModel
{
    public partial class Win32Exception : System.Exception
    {
        public Win32Exception() { }
        public Win32Exception(int error) { }
        public Win32Exception(int error, string message) { }
        public Win32Exception(string message) { }
        public Win32Exception(string message, System.Exception innerException) { }
        public int NativeErrorCode { get { return default(int); } }
    }
}
