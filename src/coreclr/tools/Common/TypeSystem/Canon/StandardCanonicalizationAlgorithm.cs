// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem
{
    /// <summary>
    /// Contains utility functionality for canonicalization used by multiple types.
    /// </summary>
    public static class StandardCanonicalizationAlgorithm
    {
        /// <summary>
        /// Returns a new instantiation that canonicalizes all types in <paramref name="instantiation"/>
        /// if possible under the policy of '<paramref name="kind"/>'
        /// </summary>
        /// <param name="instantiation">Instantiation to canonicalize.</param>
        /// <param name="kind">The type of canonicalization to apply.</param>
        /// <param name="changed">True if the returned instantiation is different from '<paramref name="instantiation"/>'.</param>
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

        // Helper API to convert a type to its canonical or universal canonical form.
        // Note that for now, there is no mixture between specific canonical and universal canonical forms,
        // meaning that the canonical form or Foo<string, int> can either be Foo<__Canon, int> or
        // Foo<__UniversalCanon, __UniversalCanon>. It cannot be Foo<__Canon, __UniversalCanon> (yet)
        // for simplicity. We can always change that rule in the futue and add support for the mixture, but
        // for now we are keeping it simple.
        public static TypeDesc ConvertToCanon(TypeDesc typeToConvert, CanonicalFormKind kind)
        {
            // Wrap the call to the version that potentially modifies the parameter. External
            // callers are not interested in that.
            return ConvertToCanon(typeToConvert, ref kind);
        }

        private static TypeDesc ConvertToCanon(TypeDesc typeToConvert, ref CanonicalFormKind kind)
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
                        return context.CanonType;
                    else if (typeToConvert.HasInstantiation)
                    {
                        TypeDesc convertedType = typeToConvert.ConvertToCanonForm(CanonicalFormKind.Specific);
                        if (convertedType.IsCanonicalSubtype(CanonicalFormKind.Universal))
                        {
                            kind = CanonicalFormKind.Universal;
                            return context.UniversalCanonType;
                        }
                        return convertedType;
                    }
                    else
                        return typeToConvert;
                }
                else if (typeToConvert.IsArray)
                {
                    return context.CanonType;
                }
                else
                {
                    TypeDesc convertedType = typeToConvert.ConvertToCanonForm(CanonicalFormKind.Specific);
                    if (convertedType.IsCanonicalSubtype(CanonicalFormKind.Universal))
                    {
                        kind = CanonicalFormKind.Universal;
                        return context.UniversalCanonType;
                    }
                    return convertedType;
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
