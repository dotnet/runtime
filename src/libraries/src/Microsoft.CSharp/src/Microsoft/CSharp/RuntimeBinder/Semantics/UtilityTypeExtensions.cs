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

            Debug.Assert(type is NullableType); // Is even this case possible?
            return Array.Empty<AggregateType>();
        }
    }
}
