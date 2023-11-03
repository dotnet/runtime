// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Reflection;
using Internal.TypeSystem.Ecma;

namespace Internal.TypeSystem
{
    public enum EffectiveVisibility
    {
        Private,
        Public,
        Family,
        Assembly,
        FamilyAndAssembly,
        FamilyOrAssembly,
    }

    public static class EffectiveVisibilityExtensions
    {
        private static EffectiveVisibility ToEffectiveVisibility(this TypeAttributes typeAttributes)
        {
            return (typeAttributes & TypeAttributes.VisibilityMask) switch
            {
                TypeAttributes.Public or TypeAttributes.NestedPublic => EffectiveVisibility.Public,
                TypeAttributes.NotPublic => EffectiveVisibility.Assembly,
                TypeAttributes.NestedPrivate => EffectiveVisibility.Private,
                TypeAttributes.NestedAssembly => EffectiveVisibility.Assembly,
                TypeAttributes.NestedFamily => EffectiveVisibility.Family,
                TypeAttributes.NestedFamANDAssem => EffectiveVisibility.FamilyAndAssembly,
                TypeAttributes.NestedFamORAssem => EffectiveVisibility.FamilyOrAssembly,
#if NETSTANDARD2_0
                _ => throw new Exception(),
#else
                _ => throw new UnreachableException()
#endif
            };
        }
        private static EffectiveVisibility ToEffectiveVisibility(this MethodAttributes typeAttributes)
        {
            return (typeAttributes & MethodAttributes.MemberAccessMask) switch
            {
                // PrivateScope == Compiler-Controlled in the ECMA spec. A member with this accessibility
                // is only accessible through a MemberDef, not a MemberRef.
                // As a result, it's only accessible within the current assembly, which is effectively the same rules as
                // Family for our case.
                MethodAttributes.PrivateScope => EffectiveVisibility.Assembly,
                MethodAttributes.Public => EffectiveVisibility.Public,
                MethodAttributes.Private => EffectiveVisibility.Private,
                MethodAttributes.Assembly => EffectiveVisibility.Assembly,
                MethodAttributes.Family => EffectiveVisibility.Family,
                MethodAttributes.FamANDAssem => EffectiveVisibility.FamilyAndAssembly,
                MethodAttributes.FamORAssem => EffectiveVisibility.FamilyOrAssembly,
#if NETSTANDARD2_0
                _ => throw new Exception(),
#else
                _ => throw new UnreachableException()
#endif
            };
        }

        private static EffectiveVisibility ToEffectiveVisibility(this FieldAttributes typeAttributes)
        {
            return (typeAttributes & FieldAttributes.FieldAccessMask) switch
            {
                // PrivateScope == Compiler-Controlled in the ECMA spec. A member with this accessibility
                // is only accessible through a MemberDef, not a MemberRef.
                // As a result, it's only accessible within the current assembly, which is effectively the same rules as
                // Family for our case.
                FieldAttributes.PrivateScope => EffectiveVisibility.Assembly,
                FieldAttributes.Public => EffectiveVisibility.Public,
                FieldAttributes.Private => EffectiveVisibility.Private,
                FieldAttributes.Assembly => EffectiveVisibility.Assembly,
                FieldAttributes.Family => EffectiveVisibility.Family,
                FieldAttributes.FamANDAssem => EffectiveVisibility.FamilyAndAssembly,
                FieldAttributes.FamORAssem => EffectiveVisibility.FamilyOrAssembly,
#if NETSTANDARD2_0
                _ => throw new Exception(),
#else
                _ => throw new UnreachableException()
#endif
            };
        }

        private static EffectiveVisibility ConstrainToVisibility(this EffectiveVisibility visibility, EffectiveVisibility enclosingVisibility)
        {
            return (visibility, enclosingVisibility) switch
            {
                (_, _) when visibility == enclosingVisibility => visibility,
                (_, EffectiveVisibility.Private) => EffectiveVisibility.Private,
                (EffectiveVisibility.Private, _) => EffectiveVisibility.Private,
                (EffectiveVisibility.Public, _) => enclosingVisibility,
                (_, EffectiveVisibility.Public) => visibility,
                (EffectiveVisibility.FamilyOrAssembly, _) => enclosingVisibility,
                (_, EffectiveVisibility.FamilyOrAssembly) => visibility,
                (EffectiveVisibility.Family, EffectiveVisibility.Assembly) => EffectiveVisibility.FamilyAndAssembly,
                (EffectiveVisibility.Family, EffectiveVisibility.FamilyAndAssembly) => EffectiveVisibility.FamilyAndAssembly,
                (EffectiveVisibility.Assembly, EffectiveVisibility.Family) => EffectiveVisibility.FamilyAndAssembly,
                (EffectiveVisibility.Assembly, EffectiveVisibility.FamilyAndAssembly) => EffectiveVisibility.FamilyAndAssembly,
                (EffectiveVisibility.FamilyAndAssembly, EffectiveVisibility.Family) => EffectiveVisibility.FamilyAndAssembly,
                (EffectiveVisibility.FamilyAndAssembly, EffectiveVisibility.Assembly) => EffectiveVisibility.FamilyAndAssembly,
#if NETSTANDARD2_0
                _ => throw new Exception(),
#else
                _ => throw new UnreachableException(),
#endif
            };
        }

        public static bool IsExposedOutsideOfThisAssembly(this EffectiveVisibility visibility, bool anyInternalsVisibleTo)
        {
            return visibility is EffectiveVisibility.Public or EffectiveVisibility.Family
                || (anyInternalsVisibleTo && visibility is EffectiveVisibility.Assembly or EffectiveVisibility.FamilyOrAssembly);
        }

        public static EffectiveVisibility GetEffectiveVisibility(this EcmaMethod method)
        {
            EffectiveVisibility visibility = method.Attributes.ToEffectiveVisibility();

            for (EcmaType type = (EcmaType)method.OwningType; type is not null; type = (EcmaType)type.ContainingType)
            {
                visibility = visibility.ConstrainToVisibility(type.Attributes.ToEffectiveVisibility());
            }
            return visibility;
        }

        public static EffectiveVisibility GetEffectiveVisibility(this EcmaType type)
        {
            EffectiveVisibility visibility = type.Attributes.ToEffectiveVisibility();
            type = (EcmaType)type.ContainingType;
            for (; type is not null; type = (EcmaType)type.ContainingType)
            {
                visibility = visibility.ConstrainToVisibility(type.Attributes.ToEffectiveVisibility());
            }
            return visibility;
        }

        public static EffectiveVisibility GetEffectiveVisibility(this TypeDesc type)
        {
            var definitionType = type.GetTypeDefinition();
            if (definitionType is MetadataType)
            {
                if (definitionType is EcmaType ecmaType)
                {
                    return ecmaType.GetEffectiveVisibility();
                }
                return EffectiveVisibility.Public;
            }
            else
            {
                return EffectiveVisibility.Public;
            }
        }

        public static EffectiveVisibility GetEffectiveVisibility(this EcmaField field)
        {
            // Treat all non-Ecma fields as always having public visibility
            EffectiveVisibility visibility = field.Attributes.ToEffectiveVisibility();

            for (EcmaType type = (EcmaType)field.OwningType; type is not null; type = (EcmaType)type.ContainingType)
            {
                visibility = visibility.ConstrainToVisibility(type.Attributes.ToEffectiveVisibility());
            }
            return visibility;
        }

        // Get the visibility declared on the field itself
        // Treat all non-Ecma fields as always having public visibility
        public static EffectiveVisibility GetAttributeEffectiveVisibility(this FieldDesc field)
        {
            if (field is EcmaField ecmaField)
            {
                return ecmaField.Attributes.ToEffectiveVisibility();
            }
            else
            {
                return EffectiveVisibility.Public;
            }
        }

        public static EffectiveVisibility GetEffectiveVisibility(this FieldDesc field)
        {
            if (field is EcmaField ecmaField)
            {
                return GetEffectiveVisibility(ecmaField);
            }
            else
            {
                return EffectiveVisibility.Public;
            }
        }
    }
}
