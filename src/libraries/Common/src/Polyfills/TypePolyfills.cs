// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System;

/// <summary>Provides downlevel polyfills for instance members on <see cref="Type"/>.</summary>
internal static class TypePolyfills
{
    extension(Type type)
    {
        public bool IsByRefLike
        {
            get
            {
                if (!type.IsValueType)
                {
                    return false;
                }

                object[] attributes = type.GetCustomAttributes(inherit: false);

                for (int i = 0; i < attributes.Length; i++)
                {
                    if (attributes[i].GetType().FullName == "System.Runtime.CompilerServices.IsByRefLikeAttribute")
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
