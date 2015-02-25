// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;

namespace System.Reflection.Internal
{
    internal static class LightUpHelper
    {
        internal static Type GetType(string typeName, params string[] assemblyNames)
        {
            foreach (string assemblyName in assemblyNames)
            {
                Type type = null;

                try
                {
                    type = Type.GetType(typeName + "," + assemblyName, throwOnError: false);
                }
                catch (IOException)
                {
                    // Should be catch(FileLoadException), but it's not available in our current 
                    // profile. It can still be thrown even when throwOnError is false.
                }

                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        internal static MethodInfo GetMethod(Type type, string name, params Type[] parameterTypes)
        {
            try
            {
                return type.GetRuntimeMethod(name, parameterTypes);
            }
            catch (AmbiguousMatchException)
            {
                // This is technically possible even when paramter types are passed
                // as the default binder allows for "widening conversions"
                // which can cause there to be more than one match. However, we
                // don't expect to hit this as the parameter types we pass are
                // specified to match known definitions precisely.

                Debug.Assert(false, "Current platform has ambiguous match for: " + type.FullName + "." + name);
                return null;
            }
        }
    }
}