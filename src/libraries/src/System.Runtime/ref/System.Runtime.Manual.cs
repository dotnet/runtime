// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;

// This is required for ProjectN to extend reflection. Once we make extensibility via contracts work on desktop, this can be removed.
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("System.Private.Reflection.Extensibility, PublicKey=002400000480000094000000060200000024000052534131000400000100010007D1FA57C4AED9F0A32E84AA0FAEFD0DE9E8FD6AEC8F87FB03766C834C99921EB23BE79AD9D5DCC1DD9AD236132102900B723CF980957FC4E177108FC607774F29E8320E92EA05ECE4E821C0A5EFE8F1645C4C0C93C1AB99285D622CAA652C1DFAD63D745D6F2DE5F17E5EAF0FC4963D261C8A12436518206DC093344D5AD293")]

namespace System
{
    public partial struct Single
    {
        public const float MinValue = (float)-3.40282346638528859e+38;
        public const float Epsilon = (float)1.4e-45;
        public const float MaxValue = (float)3.40282346638528859e+38;
        public const float PositiveInfinity = (float)1.0 / (float)0.0;
        public const float NegativeInfinity = (float)-1.0 / (float)0.0;
        public const float NaN = (float)0.0 / (float)0.0;
    }
    public partial struct Double
    {
        public const double MinValue = -1.7976931348623157E+308;
        public const double MaxValue = 1.7976931348623157E+308;

        // Note Epsilon should be a double whose hex representation is 0x1
        // on little endian machines.
        public const double Epsilon = 4.9406564584124654E-324;
        public const double NegativeInfinity = (double)-1.0 / (double)(0.0);
        public const double PositiveInfinity = (double)1.0 / (double)(0.0);
        public const double NaN = (double)0.0 / (double)0.0;
    }
    public partial class Type
    {
        // Members promoted from MemberInfo
        public abstract Type DeclaringType { get; }
        public abstract string Name { get; }
    }
}

namespace System.Runtime.InteropServices
{
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct GCHandle
    {
        public bool IsAllocated { get { return default(bool); } }
        public object Target { [System.Security.SecurityCriticalAttribute]get { return default(object); } [System.Security.SecurityCriticalAttribute]set { } }
        [System.Security.SecurityCriticalAttribute]
        public System.IntPtr AddrOfPinnedObject() { return default(System.IntPtr); }
        [System.Security.SecurityCriticalAttribute]
        public static System.Runtime.InteropServices.GCHandle Alloc(object value) { return default(System.Runtime.InteropServices.GCHandle); }
        [System.Security.SecurityCriticalAttribute]
        public static System.Runtime.InteropServices.GCHandle Alloc(object value, System.Runtime.InteropServices.GCHandleType type) { return default(System.Runtime.InteropServices.GCHandle); }
        public override bool Equals(object o) { return default(bool); }
        [System.Security.SecurityCriticalAttribute]
        public void Free() { }
        [System.Security.SecurityCriticalAttribute]
        public static System.Runtime.InteropServices.GCHandle FromIntPtr(System.IntPtr value) { return default(System.Runtime.InteropServices.GCHandle); }
        public override int GetHashCode() { return default(int); }
        public static bool operator ==(System.Runtime.InteropServices.GCHandle a, System.Runtime.InteropServices.GCHandle b) { return default(bool); }
        [System.Security.SecurityCriticalAttribute]
        public static explicit operator System.Runtime.InteropServices.GCHandle(System.IntPtr value) { return default(System.Runtime.InteropServices.GCHandle); }
        public static explicit operator System.IntPtr(System.Runtime.InteropServices.GCHandle value) { return default(System.IntPtr); }
        public static bool operator !=(System.Runtime.InteropServices.GCHandle a, System.Runtime.InteropServices.GCHandle b) { return default(bool); }
        public static System.IntPtr ToIntPtr(System.Runtime.InteropServices.GCHandle value) { return default(System.IntPtr); }
    }
    public enum GCHandleType
    {
        Normal = 2,
        Pinned = 3,
        Weak = 0,
        WeakTrackResurrection = 1,
    }
}
