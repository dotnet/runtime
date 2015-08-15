// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


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
