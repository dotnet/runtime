// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;

namespace System.UnitTesting
{
    public static class TestServices
    {
        public static string GenerateRandomString()
        {
            return Guid.NewGuid().ToString().Replace('-', '_');
        }

        public static IEnumerable<TEnum> GetEnumValues<TEnum>() where TEnum : struct
        {   // Silverlight 2.0 does not have Enum.GetValues() 
            // so we need to write our own

            foreach (FieldInfo field in typeof(TEnum).GetFields())
            {
                if (!field.IsLiteral)
                    continue;

                yield return (TEnum)field.GetRawConstantValue();
            }
        }
    }
}
