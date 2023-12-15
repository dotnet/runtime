// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Debug = System.Diagnostics.Debug;
using System;

namespace Internal.TypeSystem
{
    public class TypeIdentifierData : IEquatable<TypeIdentifierData>
    {
        public TypeIdentifierData(string scope, string name)
        {
            Debug.Assert(scope != null);
            Debug.Assert(name != null);
            Scope = scope;
            Name = name;
        }

        public static readonly TypeIdentifierData Empty = new TypeIdentifierData("", "");

        public string Scope { get; }
        public string Name { get; }

        public bool Equals(TypeIdentifierData other)
        {
            if (Scope != other.Scope)
                return false;
            return Name == other.Name;
        }

        public override int GetHashCode()
        {
            return Scope.GetHashCode() ^ Name.GetHashCode();
        }

        public override bool Equals(object o)
        {
            if (o is TypeIdentifierData other)
                return Equals(other);
            return false;
        }
    }

    public partial class TypeDesc
    {
        public virtual TypeIdentifierData TypeIdentifierData => null;

        public bool IsTypeDefEquivalent => TypeIdentifierData != null;

        public bool HasTypeEquivalence
        {
            get
            {
                if (!Context.SupportsTypeEquivalence)
                    return false;
                if (IsTypeDefEquivalent)
                    return true;
                if (HasInstantiation)
                {
                    foreach (var type in Instantiation)
                    {
                        if (type.HasTypeEquivalence)
                            return true;
                    }
                }

                return false;
            }
        }

        public virtual bool IsWindowsRuntime => false;

        public virtual bool IsComImport => false;

        public virtual bool IsComEventInterface => false;

        public bool TypeHasCharacteristicsRequiredToBeTypeEquivalent
        {
            get
            {
                if (this is not DefType)
                    return false;

                var defType = (DefType)this;

                // 1. Type is a COMImport/COMEvent interface, enum, struct, or delegate
                if (!(IsInterface && (IsComImport || IsComEventInterface)) && !IsValueType && !IsDelegate)
                    return false;

                // 2. Type is not generic
                if (HasInstantiation)
                    return false;

                // 3. Type is externally visible (i.e public)
                if (!((TypeDesc)defType).GetEffectiveVisibility().IsExposedOutsideOfThisAssembly(false))
                    return false;

                // 4. Type is not tdWindowsRuntime
                if (IsWindowsRuntime)
                    return false;

                var containingType = defType.ContainingType;
                if (defType.ContainingType != null)
                {
                    if (containingType.TypeIdentifierData == null)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        // A type may be type equivalent for the purposes of signature comparison, but not permitted
        // to actually be loaded. This predicate checks for the loadability with regards
        // to type equivalence rules for types that have a TypeIdentifierData
        public bool TypeHasCharacteristicsRequiredToBeLoadableTypeEquivalentType
        {
            get
            {
                // There is a set of checks that must be passed to be eligible implicitly for
                // type equivalence. We need to pass these checks for explicitly type equivalent
                // types as well.
                if (!TypeHasCharacteristicsRequiredToBeTypeEquivalent)
                    return false;

                // A type equivalent structure type MUST not have any non-static methods
                if (IsValueType)
                {
                    foreach (var method in GetMethods())
                    {
                        // Note that while a type equivalent structure MAY have static methods, if it does so it will never compare as
                        // equivalent to another type. This is an odd quirk, but seems to be consistent in the runtime since the feature was built.
                        if (!method.Signature.IsStatic)
                            return false;
                    }

                    foreach (var field in GetFields())
                    {
                        if (field.IsLiteral)
                        {
                            // Literal fields are ok
                            continue;
                        }

                        if (field.IsStatic)
                        {
                            return false;
                        }

                        if (field.GetEffectiveVisibility() != EffectiveVisibility.Public)
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
        }
    }
}
