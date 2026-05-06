// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem
{
    /// <summary>
    /// Contains utility functionality for canonicalization used by multiple types.
    /// This implementation can handle runtime determined types and the various fallouts from using them
    /// (e.g. the ability to upgrade <see cref="CanonicalFormKind.Specific"/> to <see cref="CanonicalFormKind.Universal"/>
    /// if somewhere within the construction of the type we encounter a universal form).
    /// </summary>
    public static class RuntimeDeterminedCanonicalizationAlgorithm
    {
        public static Instantiation ConvertInstantiationToCanonForm(Instantiation instantiation, CanonicalFormKind kind, out bool changed)
        {
            TypeDesc[] canonInstantiation = null;

            CanonicalFormKind currentKind = kind;
            CanonicalFormKind startLoopKind;

            // This logic is wrapped in a loop because we might potentially need to restart canonicalization if the policy
            // changes due to one of the instantiation arguments already being universally canonical.
            do
            {
                startLoopKind = currentKind;

                for (int instantiationIndex = 0; instantiationIndex < instantiation.Length; instantiationIndex++)
                {
                    TypeDesc typeToConvert = instantiation[instantiationIndex];
                    TypeDesc canonForm = ConvertToCanon(typeToConvert, ref currentKind);
                    if (typeToConvert != canonForm || canonInstantiation != null)
                    {
                        if (canonInstantiation == null)
                        {
                            canonInstantiation = new TypeDesc[instantiation.Length];
                            for (int i = 0; i < instantiationIndex; i++)
                                canonInstantiation[i] = instantiation[i];
                        }

                        canonInstantiation[instantiationIndex] = canonForm;
                    }
                }

                // Optimization: even if canonical policy changed, we don't actually need to re-run the loop
                // for instantiations that only have a single element.
                if (instantiation.Length == 1)
                {
                    break;
                }

            } while (currentKind != startLoopKind);


            changed = canonInstantiation != null;
            if (changed)
            {
                return new Instantiation(canonInstantiation);
            }

            return instantiation;
        }

        public static TypeDesc ConvertToCanon(TypeDesc typeToConvert, CanonicalFormKind kind)
        {
            // Wrap the call to the version that potentially modifies the parameter. External
            // callers are not interested in that.
            return ConvertToCanon(typeToConvert, ref kind);
        }

        public static TypeDesc ConvertToCanon(TypeDesc typeToConvert, ref CanonicalFormKind kind)
        {
            TypeSystemContext context = typeToConvert.Context;
            if (kind == CanonicalFormKind.Universal)
            {
                return context.UniversalCanonType;
            }
            else if (kind == CanonicalFormKind.Specific)
            {
                if (typeToConvert == context.UniversalCanonType)
                {
                    kind = CanonicalFormKind.Universal;
                    return context.UniversalCanonType;
                }
                else if (typeToConvert.IsSignatureVariable)
                {
                    return typeToConvert;
                }
                else if (typeToConvert.IsDefType)
                {
                    if (!typeToConvert.IsValueType)
                    {
                        return context.CanonType;
                    }
                    else if (typeToConvert.HasInstantiation)
                    {
                        TypeDesc canonicalType = typeToConvert.ConvertToCanonForm(CanonicalFormKind.Specific);

                        // This is a generic struct type. If the generic struct is instantiated over universal canon,
                        // the entire struct becomes universally canonical.
                        if (canonicalType.IsCanonicalSubtype(CanonicalFormKind.Universal))
                        {
                            kind = CanonicalFormKind.Universal;
                            return context.UniversalCanonType;
                        }

                        return canonicalType;
                    }
                    else if (typeToConvert.IsRuntimeDeterminedType)
                    {
                        // For non-universal canon cases, RuntimeDeterminedType's passed into this function are either
                        // reference types (which are turned into normal Canon), or instantiated types (which are handled
                        // by the above case.). But for UniversalCanon, we can have non-instantiated universal canon
                        // which will reach this case.

                        // We should only ever reach this for T__UniversalCanon.
                        Debug.Assert(((RuntimeDeterminedType)typeToConvert).CanonicalType == context.UniversalCanonType);

                        kind = CanonicalFormKind.Universal;
                        return context.UniversalCanonType;
                    }
                    else
                    {
                        return typeToConvert;
                    }
                }
                else if (typeToConvert.IsArray)
                {
                    return context.CanonType;
                }
                else
                {
                    if (typeToConvert.IsCanonicalSubtype(CanonicalFormKind.Universal))
                    {
                        kind = CanonicalFormKind.Universal;
                        return context.UniversalCanonType;
                    }

                    return typeToConvert.ConvertToCanonForm(CanonicalFormKind.Specific);
                }
            }
            else
            {
                Debug.Assert(false);
                return null;
            }
        }
    }
}
