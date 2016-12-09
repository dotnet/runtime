// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Configuration.Internal;
using System.Reflection;

namespace System.Configuration
{
    internal static class TypeUtil
    {
        // Since the config APIs were originally implemented in System.dll,
        // references to types without assembly names could be resolved to
        // System.dll in Everett. Emulate that behavior by trying to get the
        // type from System.dll
        private static Type GetLegacyType(string typeString)
        {
            Type type = null;

            // Ignore all exceptions, otherwise callers will get unexpected
            // exceptions not related to the original failure to load the
            // desired type.
            try
            {
                Assembly systemAssembly = typeof(ConfigurationException).Assembly;
                type = systemAssembly.GetType(typeString, false);
            }
            catch { }

            return type;
        }

        // Get the type specified by typeString. If it fails, try to retrieve it 
        // as a type from System.dll. If that fails,  return null or throw the original
        // exception as indicated by throwOnError.
        internal static Type GetType(string typeString, bool throwOnError)
        {
            Type type = null;
            Exception originalException = null;

            try
            {
                type = Type.GetType(typeString, throwOnError);
            }
            catch (Exception e)
            {
                originalException = e;
            }

            if (type == null)
            {
                type = GetLegacyType(typeString);
                if ((type == null) && (originalException != null))
                    throw originalException;
            }

            return type;
        }

        // Ask the host to get the type specified by typeString. If it fails, try to retrieve it 
        // as a type from System.dll. If that fails, return null or throw the original
        // exception as indicated by throwOnError.
        internal static Type GetType(IInternalConfigHost host, string typeString, bool throwOnError)
        {
            Type type = null;
            Exception originalException = null;

            try
            {
                type = host.GetConfigType(typeString, throwOnError);
            }
            catch (Exception e)
            {
                originalException = e;
            }

            if (type == null)
            {
                type = GetLegacyType(typeString);
                if ((type == null) && (originalException != null))
                    throw originalException;
            }

            return type;
        }

        internal static T CreateInstance<T>(string typeString)
        {
            Type type = GetType(typeString, true);
            VerifyAssignableType(typeof(T), type, true);
            return (T)Activator.CreateInstance(type, true);
        }

        internal static object CreateInstance(Type type)
        {
            return Activator.CreateInstance(type, true);
        }

        internal static ConstructorInfo GetConstructor(Type type, Type baseType, bool throwOnError)
        {
            type = VerifyAssignableType(baseType, type, throwOnError);
            if (type == null)
                return null;

            const BindingFlags BindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            ConstructorInfo ctor = type.GetConstructor(BindingFlags, null, CallingConventions.HasThis, Type.EmptyTypes,
                null);
            if ((ctor == null) && throwOnError)
                throw new TypeLoadException(string.Format(SR.TypeNotPublic, type.AssemblyQualifiedName));

            return ctor;
        }

        internal static Type VerifyAssignableType(Type baseType, Type type, bool throwOnError)
        {
            if (baseType.IsAssignableFrom(type))
                return type;

            if (throwOnError)
            {
                throw new TypeLoadException(
                    string.Format(SR.Config_type_doesnt_inherit_from_type, type.FullName, baseType.FullName));
            }

            return null;
        }
    }
}