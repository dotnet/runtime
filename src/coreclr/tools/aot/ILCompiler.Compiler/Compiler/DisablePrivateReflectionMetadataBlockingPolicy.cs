// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Reflection;

using ILCompiler.Dataflow;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler
{
    /// <summary>
    /// Metadata blocking policy that blocks, in addition to whatever the wrapped policy blocks, the types that are not
    /// externally visible (a type is visible if it is public and so are all the types that contain it) in assemblies annotated with
    /// System.Runtime.CompilerServices.DisablePrivateReflectionAttribute (and their members and nested types).
    /// The attribute is matched by name, so any assembly can define it.
    /// </summary>
    /// <remarks>
    /// Types blocked this way are "stripped": they are blocked, but they don't make the types that merely mention them blocked
    /// (see <see cref="MetadataBlockingPolicy.IsStripped(MetadataType)"/>).
    /// Some types can't be stripped and silently keep their metadata: enums and delegates (the runtime needs their metadata),
    /// top-level attribute classes (attributes are meant to be read, and the compiler embeds some in every assembly),
    /// types that declare generic virtual methods (the runtime type loader identifies them by metadata), generic types
    /// (unless nested in a stripped type, as the closures and iterators that the compiler generates are), and the types that contain
    /// one of those as a nested type (the metadata of a nested type requires the metadata of the type that contains it).
    /// </remarks>
    public sealed class DisablePrivateReflectionMetadataBlockingPolicy : MetadataBlockingPolicy
    {
        private const string AttributeNamespace = "System.Runtime.CompilerServices";
        private const string AttributeName = "DisablePrivateReflectionAttribute";

        private readonly MetadataBlockingPolicy _inner;
        private readonly ConcurrentDictionary<EcmaAssembly, bool> _assemblies = new();
        private readonly ConcurrentDictionary<MetadataType, bool> _strippedTypes = new();
        private readonly ConcurrentDictionary<MetadataType, bool> _typesThatNeedMetadata = new();
        private readonly ConcurrentDictionary<EcmaType, MemberVisibility> _memberVisibilities = new();

        public DisablePrivateReflectionMetadataBlockingPolicy(MetadataBlockingPolicy inner)
        {
            _inner = inner;
        }

        public override bool IsStripped(MetadataType type)
        {
            type = (MetadataType)type.GetTypeDefinition();

            if (type is not EcmaType ecmaType || ecmaType.Module.Assembly is not EcmaAssembly assembly || !HasAttribute(assembly))
                return false;

            if (!_strippedTypes.TryGetValue(type, out bool isStripped))
                isStripped = _strippedTypes.GetOrAdd(type, ComputeIsStripped(ecmaType));

            return isStripped;
        }

        private bool ComputeIsStripped(EcmaType type)
        {
            // The global <Module> type is what the runtime uses to identify the assembly (for example for the lazy binding
            // of P/Invokes and Assembly.GetExecutingAssembly()), so it needs its metadata.
            if (type.IsModuleType || IsExternallyVisible(type) || IsTopLevelAttribute(type) || NeedsMetadata(type))
                return false;

            // Generic types need their metadata at runtime, unless they are nested in a stripped type
            // (like the closures and iterators of the generic methods of that type).
            if (type.HasInstantiation)
                return type.ContainingType is MetadataType containingType && IsStripped(containingType);

            return true;
        }

        private bool HasAttribute(EcmaAssembly assembly)
        {
            if (!_assemblies.TryGetValue(assembly, out bool hasAttribute))
                hasAttribute = _assemblies.GetOrAdd(assembly, assembly.HasAssemblyCustomAttribute(AttributeNamespace, AttributeName));

            return hasAttribute;
        }

        private static bool IsExternallyVisible(EcmaType type)
        {
            while (true)
            {
                TypeAttributes visibility = type.Attributes & TypeAttributes.VisibilityMask;

                if (visibility == TypeAttributes.Public)
                    return true;

                if (visibility != TypeAttributes.NestedPublic || type.ContainingType is not EcmaType containingType)
                    return false;

                type = containingType;
            }
        }

        // Attributes are meant to be read, and the compiler embeds some in every assembly (for example NullableAttribute) to describe
        // the public types.
        private static bool IsTopLevelAttribute(EcmaType type)
            => type.ContainingType is null && IsAttributeClass(type);

        private static bool IsAttributeClass(EcmaType type)
        {
            for (DefType baseType = type.BaseType; baseType is not null; baseType = baseType.BaseType)
            {
                if (baseType.IsTypeOf("System", "Attribute"))
                    return true;
            }

            return false;
        }

        // Types that need to keep their metadata, and the types that contain them
        private bool NeedsMetadata(MetadataType type)
        {
            if (!_typesThatNeedMetadata.TryGetValue(type, out bool needsMetadata))
                needsMetadata = _typesThatNeedMetadata.GetOrAdd(type, ComputeNeedsMetadata(type));

            return needsMetadata;
        }

        private bool ComputeNeedsMetadata(MetadataType type)
        {
            if (type.IsEnum || type.IsDelegate)
                return true;

            foreach (MethodDesc method in type.GetMethods())
            {
                if (method.HasInstantiation && method.IsVirtual)
                    return true;
            }

            foreach (MetadataType nestedType in type.GetNestedTypes())
            {
                if (NeedsMetadata(nestedType))
                    return true;
            }

            return false;
        }

        public override bool IsBlocked(MetadataType type) => _inner.IsBlocked(type) || IsStripped(type);

        public override bool IsHardBlocked(MetadataType type) => _inner.IsHardBlocked(type);

        public override bool IsBlocked(MethodDesc method) => _inner.IsBlocked(method) || IsStripped(method);

        public override bool IsBlocked(FieldDesc field) => _inner.IsBlocked(field) || IsStripped(field);

        public override bool IsStripped(MethodDesc method)
        {
            if (method.GetTypicalMethodDefinition() is not EcmaMethod ecmaMethod || ecmaMethod.OwningType is not EcmaType owningType)
                return false;

            MethodAttributes attributes = ecmaMethod.Attributes;

            return GetMemberVisibility(owningType) switch
            {
                MemberVisibility.All => false,
                MemberVisibility.PublicOnly => (attributes & MethodAttributes.MemberAccessMask) != MethodAttributes.Public,
                MemberVisibility.AttributeMembers => (attributes & MethodAttributes.MemberAccessMask) != MethodAttributes.Public
                    && (attributes & (MethodAttributes.SpecialName | MethodAttributes.RTSpecialName)) == 0,
                _ => true,
            };
        }

        public override bool IsStripped(FieldDesc field)
        {
            if (field.GetTypicalFieldDefinition() is not EcmaField ecmaField || ecmaField.OwningType is not EcmaType owningType)
                return false;

            return GetMemberVisibility(owningType) switch
            {
                MemberVisibility.All or MemberVisibility.AttributeMembers => false,
                MemberVisibility.PublicOnly => (ecmaField.Attributes & FieldAttributes.FieldAccessMask) != FieldAttributes.Public,
                _ => true,
            };
        }

        private MemberVisibility GetMemberVisibility(EcmaType type)
        {
            if (type.Module.Assembly is not EcmaAssembly assembly || !HasAttribute(assembly))
                return MemberVisibility.All;

            if (!_memberVisibilities.TryGetValue(type, out MemberVisibility visibility))
                visibility = _memberVisibilities.GetOrAdd(type, ComputeMemberVisibility(type));

            return visibility;
        }

        // The members of a type are hidden if they are not public, and all of them are if the type is not externally visible
        // (also if the type keeps its metadata). Not subject to this are the global <Module> type and the enums and delegates (the runtime
        // needs the members that they have). The attribute classes keep the members that attributes are instantiated through (constructors,
        // fields and property setters, whatever their accessibility). This must match the rules of the CoreCLR runtime (RuntimeTypeHandle_GetReflectionVisibility).
        private MemberVisibility ComputeMemberVisibility(EcmaType type)
        {
            if (type.IsModuleType)
                return MemberVisibility.All;

            if (IsStripped(type))
                return MemberVisibility.None;

            if (type.IsEnum || type.IsDelegate)
                return MemberVisibility.All;

            if (IsAttributeClass(type))
                return MemberVisibility.AttributeMembers;

            return IsExternallyVisible(type) ? MemberVisibility.PublicOnly : MemberVisibility.None;
        }

        private enum MemberVisibility : byte
        {
            // Members are not hidden
            All,

            // Only public members are available
            PublicOnly,

            // No member is available
            None,

            // The constructors, fields and properties and the public members are available
            AttributeMembers,
        }
    }
}
