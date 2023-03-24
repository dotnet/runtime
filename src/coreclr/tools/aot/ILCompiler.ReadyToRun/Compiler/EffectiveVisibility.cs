// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Reflection;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler
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
                _ => throw new UnreachableException()
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
                _ => throw new UnreachableException()
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
                _ => throw new UnreachableException(),
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
    }
}
