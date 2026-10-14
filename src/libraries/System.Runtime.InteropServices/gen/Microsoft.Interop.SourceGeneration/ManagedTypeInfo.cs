// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Interop
{
    /// <summary>
    /// A discriminated union that contains enough info about a managed type to determine a marshalling generator and generate code.
    /// </summary>
    public abstract record ManagedTypeInfo(string FullTypeName, string DiagnosticFormattedName)
    {
        public virtual bool Equals(ManagedTypeInfo? other)
        {
            return other is not null
                && FullTypeName == other.FullTypeName
                && DiagnosticFormattedName == other.DiagnosticFormattedName;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(FullTypeName, DiagnosticFormattedName);
        }

        protected ManagedTypeInfo(ManagedTypeInfo original)
        {
            FullTypeName = original.FullTypeName;
            DiagnosticFormattedName = original.DiagnosticFormattedName;
        }

    }

    public sealed record WellKnownTypeInfo(string FullTypeName, string DiagnosticFormattedName, WellKnownType SpecialType) : ManagedTypeInfo(FullTypeName, DiagnosticFormattedName)
    {
        public static readonly WellKnownTypeInfo Byte = new("byte", "byte", WellKnownType.System_Byte);
        public static readonly WellKnownTypeInfo SByte = new("sbyte", "sbyte", WellKnownType.System_SByte);
        public static readonly WellKnownTypeInfo Int16 = new("short", "short", WellKnownType.System_Int16);
        public static readonly WellKnownTypeInfo UInt16 = new("ushort", "ushort", WellKnownType.System_UInt16);
        public static readonly WellKnownTypeInfo Int32 = new("int", "int", WellKnownType.System_Int32);
        public static readonly WellKnownTypeInfo UInt32 = new("uint", "uint", WellKnownType.System_UInt32);
        public static readonly WellKnownTypeInfo Void = new("void", "void", WellKnownType.System_Void);
        public static readonly WellKnownTypeInfo String = new("string", "string", WellKnownType.System_String);
        public static readonly WellKnownTypeInfo Boolean = new("bool", "bool", WellKnownType.System_Boolean);
        public static readonly WellKnownTypeInfo IntPtr = new("nint", "nint", WellKnownType.System_IntPtr);

        public bool Equals(WellKnownTypeInfo? other)
        {
            return other is not null && SpecialType == other.SpecialType;
        }

        public override int GetHashCode()
        {
            return (int)SpecialType;
        }
    }

    public sealed record EnumTypeInfo(string FullTypeName, string DiagnosticFormattedName, WellKnownType UnderlyingType) : ManagedTypeInfo(FullTypeName, DiagnosticFormattedName);

    public sealed record PointerTypeInfo(string FullTypeName, string DiagnosticFormattedName, bool IsFunctionPointer) : ManagedTypeInfo(FullTypeName, DiagnosticFormattedName);

    public sealed record SzArrayType(ManagedTypeInfo ElementTypeInfo) : ManagedTypeInfo($"{ElementTypeInfo.FullTypeName}[]", $"{ElementTypeInfo.DiagnosticFormattedName}[]");

    public sealed record DelegateTypeInfo(string FullTypeName, string DiagnosticFormattedName, bool IsGeneric) : ManagedTypeInfo(FullTypeName, DiagnosticFormattedName);

    public sealed record TypeParameterTypeInfo(string FullTypeName, string DiagnosticFormattedName) : ManagedTypeInfo(FullTypeName, DiagnosticFormattedName);

    public sealed record ValueTypeInfo(string FullTypeName, string DiagnosticFormattedName, bool IsByRefLike) : ManagedTypeInfo(FullTypeName, DiagnosticFormattedName);

    public sealed record ReferenceTypeInfo(string FullTypeName, string DiagnosticFormattedName) : ManagedTypeInfo(FullTypeName, DiagnosticFormattedName);
}
