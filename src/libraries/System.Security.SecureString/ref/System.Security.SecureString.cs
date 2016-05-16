// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Security
{
    public sealed partial class SecureString : System.IDisposable
    {
        public SecureString() { }
        [System.CLSCompliantAttribute(false)]
        public unsafe SecureString(char* value, int length) { }
        public int Length { get { return default(int); } }
        public void AppendChar(char c) { }
        public void Clear() { }
        public System.Security.SecureString Copy() { return default(System.Security.SecureString); }
        public void Dispose() { }
        public void InsertAt(int index, char c) { }
        public bool IsReadOnly() { return default(bool); }
        public void MakeReadOnly() { }
        public void RemoveAt(int index) { }
        public void SetAt(int index, char c) { }
    }
    public static partial class SecureStringMarshal
    {
        public static System.IntPtr SecureStringToCoTaskMemAnsi(System.Security.SecureString s) { return default(System.IntPtr); }
        public static System.IntPtr SecureStringToCoTaskMemUnicode(System.Security.SecureString s) { return default(System.IntPtr); }
        public static System.IntPtr SecureStringToGlobalAllocAnsi(System.Security.SecureString s) { return default(System.IntPtr); }
        public static System.IntPtr SecureStringToGlobalAllocUnicode(System.Security.SecureString s) { return default(System.IntPtr); }
    }
}
