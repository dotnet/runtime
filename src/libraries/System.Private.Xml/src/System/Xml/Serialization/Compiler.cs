// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Serialization
{
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Collections;
    using System.IO;
    using System;
    using System.Text;
    using System.ComponentModel;
    using System.Security;
    using System.Diagnostics;
    using System.Threading;
    using System.Xml.Serialization.Configuration;
    using System.Globalization;
    using System.Runtime.Versioning;
    using System.Runtime.CompilerServices;
    using System.Collections.Generic;
    using System.Linq;

    internal class Compiler
    {
        private bool _debugEnabled = DiagnosticsSwitches.KeepTempFiles.Enabled;
        private StringWriter _writer = new StringWriter(CultureInfo.InvariantCulture);

#if !FEATURE_SERIALIZATION_UAPAOT
        // SxS: This method does not take any resource name and does not expose any resources to the caller.
        // It's OK to suppress the SxS warning.
        internal void AddImport(Type type, Hashtable types)
        {
            if (type == null)
                return;
            if (TypeScope.IsKnownType(type))
                return;
            if (types[type] != null)
                return;
            types[type] = type;
            Type baseType = type.BaseType;
            if (baseType != null)
                AddImport(baseType, types);

            Type declaringType = type.DeclaringType;
            if (declaringType != null)
                AddImport(declaringType, types);

            foreach (Type intf in type.GetInterfaces())
                AddImport(intf, types);

            ConstructorInfo[] ctors = type.GetConstructors();
            for (int i = 0; i<ctors.Length; i++)
            {
                ParameterInfo[] parms = ctors[i].GetParameters();
                for (int j = 0; j<parms.Length; j++)
                {
                    AddImport(parms[j].ParameterType, types);
                }
            }

            if (type.IsGenericType)
            {
                Type[] arguments = type.GetGenericArguments();
                for (int i = 0; i<arguments.Length; i++)
                {
                    AddImport(arguments[i], types);
                }
            }

            Module module = type.Module;
            Assembly assembly = module.Assembly;
            if (DynamicAssemblies.IsTypeDynamic(type))
            {
                DynamicAssemblies.Add(assembly);
                return;
            }

            object[] typeForwardedFromAttribute = type.GetCustomAttributes(typeof(TypeForwardedFromAttribute), false);
            if (typeForwardedFromAttribute.Length > 0)
            {
                TypeForwardedFromAttribute originalAssemblyInfo = typeForwardedFromAttribute[0] as TypeForwardedFromAttribute;
                Assembly originalAssembly = Assembly.Load(new AssemblyName(originalAssemblyInfo.AssemblyFullName));
                //_imports[originalAssembly] = originalAssembly.Location;
            }
            //_imports[assembly] = assembly.Location;
        }

        // SxS: This method does not take any resource name and does not expose any resources to the caller.
        // It's OK to suppress the SxS warning.
        internal void AddImport(Assembly assembly)
        {
            //_imports[assembly] = assembly.Location;
        }

        internal void Close() { }

        internal TextWriter Source
        {
            get { return _writer; }
        }
#endif

        internal static string GetTempAssemblyName(AssemblyName parent, string ns)
        {
            return parent.Name + ".XmlSerializers" + (ns == null || ns.Length == 0 ? "" : "." + ns.GetHashCode());
        }
    }
}


