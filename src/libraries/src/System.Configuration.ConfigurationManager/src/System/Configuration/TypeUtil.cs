// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Configuration.Internal;
using System.Reflection;

namespace System.Configuration
{
    internal static class TypeUtil
    {
        internal const string ConfigurationManagerAssemblyName = "System.Configuration.ConfigurationManager";

        // Deliberately not being explicit about the versions to make
        // things simpler for consumers of System.Configuration.
        private static string[] s_implicitAssemblies =
        {
            // Historically we would find types in System.dll here.
            // This is because Configuration used to live in System.dll
            // and by resolution rules, typenames without an assembly
            // specifier would be found there:
            //
            // (current executing assembly -> mscorlib / now s.p.c.l)
            //
            // When Configuration types moved to System.Configuration
            // the implicit lookup for System had to be added to keep
            // existing configuration files working.

            // We also need to find types in mscorlib.dll as currently
            // only types found in System.Private.CoreLib.dll are
            // implicitly resolved. If this changes we can remove the
            // explicit mscorlib lookup.

            "mscorlib",
            "System",
        };

        /// <summary>
        /// Find type references that used to be found without assembly names
        /// </summary>
        private static Type GetImplicitType(string typeString)
        {
            // Since the config APIs were originally implemented in System.dll,
            // references to types without assembly names could be resolved if
            // they lived in System.dll.
            //
            // On NetFX we would try to emulate that behavior by looking in
            // System. As types have moved around in CoreFX- we'll try to load
            // from the a variety of assemblies to mimick the old behavior.

            // Don't bother to look around if we've already got something that
            // is clearly not a simple type name.
            if (string.IsNullOrEmpty(typeString) || typeString.IndexOf(',') != -1) // string.Contains(char) is .NetCore2.1+ specific
                return null;

            // Ignore all exceptions, otherwise callers will get unexpected
            // exceptions not related to the original failure to load the
            // desired type.

            // First attempt is against our own System.Configuration types
            try
            {
                Assembly configurationAssembly = typeof(ConfigurationException).Assembly;
                Type type = configurationAssembly.GetType(typeString, false);
                if (type != null)
                    return type;
            }
            catch { }

            foreach (string assembly in s_implicitAssemblies)
            {
                try
                {
                    Type type = Type.GetType($"{typeString}, {assembly}");
                    if (type != null)
                        return type;
                }
                catch { }
            }

            return null;
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
                type = GetImplicitType(typeString);
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
                type = GetImplicitType(typeString);
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
                throw new TypeLoadException(SR.Format(SR.TypeNotPublic, type.AssemblyQualifiedName));

            return ctor;
        }

        internal static Type VerifyAssignableType(Type baseType, Type type, bool throwOnError)
        {
            if (baseType.IsAssignableFrom(type))
                return type;

            if (throwOnError)
            {
                throw new TypeLoadException(
                    SR.Format(SR.Config_type_doesnt_inherit_from_type, type.FullName, baseType.FullName));
            }

            return null;
        }
    }
}
