// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Runtime.InteropServices
{
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct OSPlatform : System.IEquatable<System.Runtime.InteropServices.OSPlatform>
    {
        public static System.Runtime.InteropServices.OSPlatform Linux { get { return default(System.Runtime.InteropServices.OSPlatform); } }
        public static System.Runtime.InteropServices.OSPlatform OSX { get { return default(System.Runtime.InteropServices.OSPlatform); } }
        public static System.Runtime.InteropServices.OSPlatform Windows { get { return default(System.Runtime.InteropServices.OSPlatform); } }
        public static System.Runtime.InteropServices.OSPlatform Create(string osPlatform) { return default(System.Runtime.InteropServices.OSPlatform); }
        public override bool Equals(object obj) { return default(bool); }
        public bool Equals(System.Runtime.InteropServices.OSPlatform other) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static bool operator ==(System.Runtime.InteropServices.OSPlatform left, System.Runtime.InteropServices.OSPlatform right) { return default(bool); }
        public static bool operator !=(System.Runtime.InteropServices.OSPlatform left, System.Runtime.InteropServices.OSPlatform right) { return default(bool); }
        public override string ToString() { return default(string); }
    }
    public static partial class RuntimeInformation
    {
        public static bool IsOSPlatform(System.Runtime.InteropServices.OSPlatform osPlatform) { return default(bool); }
    }
}
