// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Runtime.InteropServices
{
    public enum Architecture
    {
        Arm = 2,
        Arm64 = 3,
        X64 = 1,
        X86 = 0,
    }
    public readonly partial struct OSPlatform : System.IEquatable<System.Runtime.InteropServices.OSPlatform>
    {
        private readonly object _dummy;
        public static System.Runtime.InteropServices.OSPlatform Linux { get { throw null; } }
        public static System.Runtime.InteropServices.OSPlatform OSX { get { throw null; } }
        public static System.Runtime.InteropServices.OSPlatform Windows { get { throw null; } }
        public static System.Runtime.InteropServices.OSPlatform Create(string osPlatform) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.Runtime.InteropServices.OSPlatform other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Runtime.InteropServices.OSPlatform left, System.Runtime.InteropServices.OSPlatform right) { throw null; }
        public static bool operator !=(System.Runtime.InteropServices.OSPlatform left, System.Runtime.InteropServices.OSPlatform right) { throw null; }
        public override string ToString() { throw null; }
    }
    public static partial class RuntimeInformation
    {
        public static string FrameworkDescription { get { throw null; } }
        public static System.Runtime.InteropServices.Architecture OSArchitecture { get { throw null; } }
        public static string OSDescription { get { throw null; } }
        public static System.Runtime.InteropServices.Architecture ProcessArchitecture { get { throw null; } }
        public static bool IsOSPlatform(System.Runtime.InteropServices.OSPlatform osPlatform) { throw null; }
    }
}
