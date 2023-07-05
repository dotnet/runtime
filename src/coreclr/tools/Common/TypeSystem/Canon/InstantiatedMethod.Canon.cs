// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem
{
    // Implements generic method canonicalization
    public partial class InstantiatedMethod
    {
        /// <summary>
        /// Stores a cached version of the canonicalized form of this method since
        /// calculating it is a recursive operation
        /// </summary>
        private InstantiatedMethod _specificCanonCache;
        private InstantiatedMethod _universalCanonCache;

        /// <summary>
        /// Returns the result of canonicalizing this method over the given kind of Canon
        /// </summary>
        public override MethodDesc GetCanonMethodTarget(CanonicalFormKind kind)
        {
            InstantiatedMethod canonicalMethodResult = GetCachedCanonValue(kind);
            if (canonicalMethodResult == null)
            {
                bool instantiationChanged;
                Instantiation canonInstantiation = Context.ConvertInstantiationToCanonForm(Instantiation, kind, out instantiationChanged);
                MethodDesc openMethodOnCanonicalizedType = _methodDef.GetCanonMethodTarget(kind);

                if (instantiationChanged || (openMethodOnCanonicalizedType != _methodDef))
                {
                    canonicalMethodResult = Context.GetInstantiatedMethod(openMethodOnCanonicalizedType, canonInstantiation);
                }
                else
                {
                    canonicalMethodResult = this;
                }

                // If the method instantiation is universal, we use a __UniversalCanon for all instantiation arguments for simplicity.
                // This is to not end up having method instantiations like Foo<__UniversalCanon>.Method<int> or Foo<__UniversalCanon>.Method<string>
                // or Foo<__UniversalCanon>.Method<__Canon> or Foo<int>.Method<__UniversalCanon>
                // It should just be Foo<__UniversalCanon>.Method<__UniversalCanon>
                if ((kind == CanonicalFormKind.Specific) &&
                    canonicalMethodResult.IsCanonicalMethod(CanonicalFormKind.Universal))
                {
                    canonicalMethodResult = (InstantiatedMethod)canonicalMethodResult.GetCanonMethodTarget(CanonicalFormKind.Universal);
                }

                SetCachedCanonValue(kind, canonicalMethodResult);
            }

            return canonicalMethodResult;
        }

        private InstantiatedMethod GetCachedCanonValue(CanonicalFormKind kind)
        {
            switch (kind)
            {
                case CanonicalFormKind.Specific:
                    return _specificCanonCache;

                case CanonicalFormKind.Universal:
                    return _universalCanonCache;

                default:
                    Debug.Fail("Invalid CanonicalFormKind: " + kind);
                    return null;
            }
        }

        private void SetCachedCanonValue(CanonicalFormKind kind, InstantiatedMethod value)
        {
            switch (kind)
            {
                case CanonicalFormKind.Specific:
                    Debug.Assert(_specificCanonCache == null || _specificCanonCache == value);
                    _specificCanonCache = value;
                    break;

                case CanonicalFormKind.Universal:
                    Debug.Assert(_universalCanonCache == null || _universalCanonCache == value);
                    _universalCanonCache = value;
                    break;

                default:
                    Debug.Fail("Invalid CanonicalFormKind: " + kind);
                    break;
            }
        }

        /// <summary>
        /// True if either the containing type instantiation or any of this method's generic arguments
        /// are canonical
        /// </summary>
        public override bool IsCanonicalMethod(CanonicalFormKind policy)
        {
            if (OwningType.HasInstantiation && OwningType.IsCanonicalSubtype(policy))
            {
                return true;
            }

            foreach (TypeDesc type in Instantiation)
            {
                if (type.IsCanonicalSubtype(policy))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
