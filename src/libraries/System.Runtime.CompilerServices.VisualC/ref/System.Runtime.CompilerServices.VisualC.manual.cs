// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// We need to add an InternalsVisibleToAttribute here to mscorlib since we need to expose these types via type forwards in mscorlib
// since tooling expects these types to live there and not in System.Runtime.CompilerServices.VisualC, but we don't want to expose
// these types publicly.
[assembly:System.Runtime.CompilerServices.InternalsVisibleTo("mscorlib, PublicKey=00000000000000000400000000000000")]

namespace System.Runtime.CompilerServices
{
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    internal sealed class AssemblyAttributesGoHere
    {
        internal AssemblyAttributesGoHere()
        {
        }
    }
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    internal sealed class AssemblyAttributesGoHereS
    {
        internal AssemblyAttributesGoHereS()
        {
        }
    }
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    internal sealed class AssemblyAttributesGoHereM
    {
        internal AssemblyAttributesGoHereM()
        {
        }
    }
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    internal sealed class AssemblyAttributesGoHereSM
    {
        internal AssemblyAttributesGoHereSM()
        {
        }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.All)]
    internal sealed class DecoratedNameAttribute : System.Attribute
    {
        public DecoratedNameAttribute(string decoratedName) {}
    }
    [AttributeUsageAttribute(AttributeTargets.Class |
                    AttributeTargets.Constructor |
                    AttributeTargets.Method |
                    AttributeTargets.Field |
                    AttributeTargets.Event |
                    AttributeTargets.Property)]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    internal sealed class SuppressMergeCheckAttribute : Attribute
    {
        public SuppressMergeCheckAttribute()
        {}
    }
}
