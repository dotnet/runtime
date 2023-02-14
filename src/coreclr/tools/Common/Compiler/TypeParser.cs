// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;
using System.Diagnostics;

namespace ILCompiler
{
    public class TypeParser
    {
        public static TypeDesc GetType(ModuleDesc assembly, string fullName, bool throwIfNotFound)
        {
            Debug.Assert(!string.IsNullOrEmpty(fullName));
            var position = fullName.IndexOf('/');
            if (position > 0)
                return GetNestedType(assembly, fullName, throwIfNotFound);
            string @namespace, name;
            SplitFullName(fullName, out @namespace, out name);

            return assembly.GetType(@namespace, name, throwIfNotFound);
        }

        private static MetadataType GetNestedType(ModuleDesc assembly, string fullName, bool throwIfNotFound)
        {
            var names = fullName.Split('/');
            var type = GetType(assembly, names[0], throwIfNotFound);

            if (type == null)
                return null;

            MetadataType typeReference = (MetadataType)type;
            for (int i = 1; i < names.Length; i++)
            {
                var nested_type = typeReference.GetNestedType(names[i]);
                if (nested_type == null)
                    return null;

                typeReference = nested_type;
            }

            return typeReference;
        }

        public static void SplitFullName(string fullName, out string @namespace, out string name)
        {
            var last_dot = fullName.LastIndexOf('.');

            if (last_dot == -1)
            {
                @namespace = string.Empty;
                name = fullName;
            }
            else
            {
                @namespace = fullName.Substring(0, last_dot);
                name = fullName.Substring(last_dot + 1);
            }
        }
    }
}
