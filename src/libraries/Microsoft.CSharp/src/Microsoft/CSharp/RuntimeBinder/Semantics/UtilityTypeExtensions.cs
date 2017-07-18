// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal static class UtilityTypeExtensions
    {
        private static IEnumerable<AggregateType> InterfaceAndBases(this AggregateType type)
        {
            Debug.Assert(type != null);
            yield return type;
            foreach (AggregateType t in type.GetIfacesAll().Items)
                yield return t;
        }

        private static IEnumerable<AggregateType> AllConstraintInterfaces(this TypeArray constraints)
        {
            Debug.Assert(constraints != null);
            foreach (AggregateType c in constraints.Items)
                foreach (AggregateType t in c.InterfaceAndBases())
                    yield return t;
        }

        private static IEnumerable<AggregateType> TypeAndBaseClasses(this AggregateType type)
        {
            Debug.Assert(type != null);
            AggregateType t = type;
            while (t != null)
            {
                yield return t;
                t = t.GetBaseClass();
            }
        }

        private static IEnumerable<AggregateType> TypeAndBaseClassInterfaces(this AggregateType type)
        {
            Debug.Assert(type != null);
            foreach (AggregateType b in type.TypeAndBaseClasses())
                foreach (AggregateType t in b.GetIfacesAll().Items)
                    yield return t;
        }

        public static IEnumerable<AggregateType> AllPossibleInterfaces(this CType type)
        {
            Debug.Assert(type != null);
            if (type is AggregateType ats)
            {
                return ats.TypeAndBaseClassInterfaces();
            }

            if (type is TypeParameterType typeParameter)
            {
                return AllPossibleInterfaces(typeParameter);
            }

            return Array.Empty<AggregateType>();
        }

        private static IEnumerable<AggregateType> AllPossibleInterfaces(TypeParameterType type)
        {
            foreach (AggregateType t in type.GetEffectiveBaseClass().TypeAndBaseClassInterfaces())
            {
                yield return t;
            }

            foreach (AggregateType t in type.GetInterfaceBounds().AllConstraintInterfaces())
            {
                yield return t;
            }
        }
    }
}
