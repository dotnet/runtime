// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            foreach (AggregateType t in type.GetIfacesAll().ToArray())
                yield return t;
        }

        private static IEnumerable<AggregateType> AllConstraintInterfaces(this TypeArray constraints)
        {
            Debug.Assert(constraints != null);
            foreach (AggregateType c in constraints.ToArray())
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
                foreach (AggregateType t in b.GetIfacesAll().ToArray())
                    yield return t;
        }

        public static IEnumerable<CType> AllPossibleInterfaces(this CType type)
        {
            Debug.Assert(type != null);
            if (type.IsAggregateType())
            {
                foreach (CType t in type.AsAggregateType().TypeAndBaseClassInterfaces())
                    yield return t;
            }
            else if (type.IsTypeParameterType())
            {
                foreach (CType t in type.AsTypeParameterType().GetEffectiveBaseClass().TypeAndBaseClassInterfaces())
                    yield return t;
                foreach (CType t in type.AsTypeParameterType().GetInterfaceBounds().AllConstraintInterfaces())
                    yield return t;
            }
        }
    }
}
