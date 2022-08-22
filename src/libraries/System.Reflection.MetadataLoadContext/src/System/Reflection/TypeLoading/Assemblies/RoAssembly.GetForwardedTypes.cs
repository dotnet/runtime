// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Collections.Generic;

namespace System.Reflection.TypeLoading
{
    /// <summary>
    /// Base class for all Assembly objects created by a MetadataLoadContext.
    /// </summary>
    internal abstract partial class RoAssembly
    {
        public sealed override Type[] GetForwardedTypes()
        {
            List<Type> types = new List<Type>();
            List<Exception>? exceptions = null;

            IterateTypeForwards(
                delegate (RoAssembly redirectedAssembly, ReadOnlySpan<byte> ns, ReadOnlySpan<byte> name)
                {
                    Type? type = null;
                    Exception? exception = null;
                    if (redirectedAssembly is RoExceptionAssembly exceptionAssembly)
                    {
                        exception = exceptionAssembly.Exception;
                    }
                    else
                    {
                        // GetTypeCore() will follow any further type-forwards if needed.
                        type = redirectedAssembly.GetTypeCore(ns, name, ignoreCase: false, out Exception? e);
                        if (type == null)
                        {
                            exception = e;
                        }
                    }

                    Debug.Assert((type != null) != (exception != null)); // Exactly one of these must be non-null.

                    if (type != null)
                    {
                        types.Add(type);
                        AddPublicNestedTypes(type, types);
                    }
                    else
                    {
                        exceptions ??= new List<Exception>();
                        exceptions.Add(exception!);
                    }
                }
            );

            if (exceptions != null)
            {
                int numTypes = types.Count;
                int numExceptions = exceptions.Count;
                types.AddRange(new Type[numExceptions]); // add one null Type for each exception.
                exceptions.InsertRange(0, new Exception[numTypes]); // align the Exceptions with the null Types.
                throw new ReflectionTypeLoadException(types.ToArray(), exceptions.ToArray());
            }

            return types.ToArray();
        }

        private static void AddPublicNestedTypes(Type type, List<Type> types)
        {
            foreach (Type nestedType in type.GetNestedTypes(BindingFlags.Public))
            {
                types.Add(nestedType);
                AddPublicNestedTypes(nestedType, types);
            }
        }

        /// <summary>
        /// Intentionally excludes forwards to nested types.
        /// </summary>
        protected delegate void TypeForwardHandler(RoAssembly redirectedAssembly, ReadOnlySpan<byte> ns, ReadOnlySpan<byte> name);
        protected abstract void IterateTypeForwards(TypeForwardHandler handler);
    }
}
