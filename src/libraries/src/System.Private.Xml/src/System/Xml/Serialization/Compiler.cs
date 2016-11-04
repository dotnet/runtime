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
    using System.CodeDom.Compiler;
    using System.Security;
    using System.Diagnostics;
    using System.Security.Policy;
    using System.Threading;
    using System.Xml.Serialization.Configuration;
    using System.Globalization;
    using System.Runtime.Versioning;
    using System.Runtime.CompilerServices;
    using Collections.Generic;
    using Linq;

    internal class Compiler
    {
        private bool _debugEnabled = DiagnosticsSwitches.KeepTempFiles.Enabled;
        private Hashtable _imports = new Hashtable();
        private StringWriter _writer = new StringWriter(CultureInfo.InvariantCulture);

        protected string[] Imports
        {
            get
            {
                string[] array = new string[_imports.Values.Count];
                _imports.Values.CopyTo(array, 0);
                return array;
            }
        }

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
            Type baseType = type.GetTypeInfo().BaseType;
            if (baseType != null)
                AddImport(baseType, types);

            Type declaringType = type.DeclaringType;
            if (declaringType != null)
                AddImport(declaringType, types);

            foreach (Type intf in type.GetInterfaces())
                AddImport(intf, types);

            ConstructorInfo[] ctors = type.GetConstructors();
            for (int i = 0; i < ctors.Length; i++)
            {
                ParameterInfo[] parms = ctors[i].GetParameters();
                for (int j = 0; j < parms.Length; j++)
                {
                    AddImport(parms[j].ParameterType, types);
                }
            }

            if (type.GetTypeInfo().IsGenericType)
            {
                Type[] arguments = type.GetGenericArguments();
                for (int i = 0; i < arguments.Length; i++)
                {
                    AddImport(arguments[i], types);
                }
            }

            Module module = type.GetTypeInfo().Module;
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

        internal TextWriter Source
        {
            get { return _writer; }
        }

        internal void Close() { }

        internal static string GetTempAssemblyPath(string baseDir, Assembly assembly, string defaultNamespace)
        {
            if (assembly.IsDynamic)
            {
                throw new InvalidOperationException(SR.XmlPregenAssemblyDynamic);
            }

            try
            {
                if (baseDir != null && baseDir.Length > 0)
                {
                    // check that the dirsctory exists
                    if (!Directory.Exists(baseDir))
                    {
                        throw new UnauthorizedAccessException(SR.Format(SR.XmlPregenMissingDirectory, baseDir));
                    }
                }
                else
                {
                    baseDir = Path.GetTempPath();
                    // check that the dirsctory exists
                    if (!Directory.Exists(baseDir))
                    {
                        throw new UnauthorizedAccessException(SR.XmlPregenMissingTempDirectory);
                    }
                }
                if (baseDir.EndsWith("\\", StringComparison.Ordinal))
                    baseDir += GetTempAssemblyName(assembly.GetName(), defaultNamespace);
                else
                    baseDir += "\\" + GetTempAssemblyName(assembly.GetName(), defaultNamespace);
            }
            finally
            {
            }

            return baseDir + ".dll";
        }

        internal static string GetTempAssemblyName(AssemblyName parent, string ns)
        {
            return parent.Name + ".XmlSerializers" + (ns == null || ns.Length == 0 ? "" : "." + ns.GetHashCode());
        }

        // SxS: This method does not take any resource name and does not expose any resources to the caller.
        // It's OK to suppress the SxS warning.
        internal Assembly Compile(Assembly parent, string ns, XmlSerializerCompilerParameters xmlParameters, Evidence evidence)
        {
            CodeDomProvider codeProvider = new Microsoft.CSharp.CSharpCodeProvider();
            CompilerParameters parameters = xmlParameters.CodeDomParameters;
            parameters.ReferencedAssemblies.AddRange(Imports);

            if (_debugEnabled)
            {
                parameters.GenerateInMemory = false;
                parameters.IncludeDebugInformation = true;
                parameters.TempFiles.KeepFiles = true;
            }

            if (parent != null && (parameters.OutputAssembly == null || parameters.OutputAssembly.Length == 0))
            {
                string assemblyName = AssemblyNameFromOptions(parameters.CompilerOptions);
                if (assemblyName == null)
                    assemblyName = GetTempAssemblyPath(parameters.TempFiles.TempDir, parent, ns);
                // CONSIDER: throw if OutputAssembly alredy specified or remove the /out switch from the compilerOptions
                parameters.OutputAssembly = assemblyName;
            }

            if (parameters.CompilerOptions == null || parameters.CompilerOptions.Length == 0)
                parameters.CompilerOptions = "/nostdlib";
            else
                parameters.CompilerOptions += " /nostdlib";

            parameters.CompilerOptions += " /D:_DYNAMIC_XMLSERIALIZER_COMPILATION";

            CompilerResults results = null;
            Assembly assembly = null;
            try
            {
                results = codeProvider.CompileAssemblyFromSource(parameters, _writer.ToString());
                // check the output for errors or a certain level-1 warning (1595)
                if (results.Errors.Count > 0)
                {
                    StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture);
                    stringWriter.WriteLine(SR.Format(SR.XmlCompilerError, results.NativeCompilerReturnValue.ToString(CultureInfo.InvariantCulture)));
                    bool foundOne = false;
                    foreach (CompilerError e in results.Errors)
                    {
                        // clear filename. This makes ToString() print just error number and message.
                        e.FileName = "";
                        if (!e.IsWarning || e.ErrorNumber == "CS1595")
                        {
                            foundOne = true;
                            stringWriter.WriteLine(e.ToString());
                        }
                    }
                    if (foundOne)
                    {
                        throw new InvalidOperationException(stringWriter.ToString());
                    }
                }
                assembly = results.CompiledAssembly;
            }
            catch (UnauthorizedAccessException)
            {
                // try to get the user token
                string user = GetCurrentUser();
                if (user == null || user.Length == 0)
                {
                    throw new UnauthorizedAccessException(SR.XmlSerializerAccessDenied);
                }
                else
                {
                    throw new UnauthorizedAccessException(SR.Format(SR.XmlIdentityAccessDenied, user));
                }
            }
            catch (FileLoadException fle)
            {
                throw new InvalidOperationException(SR.XmlSerializerCompileFailed, fle);
            }
            // somehow we got here without generating an assembly
            if (assembly == null) throw new InvalidOperationException(SR.XmlInternalError);

            return assembly;
        }

        private static string AssemblyNameFromOptions(string options)
        {
            if (options == null || options.Length == 0)
                return null;

            string outName = null;
            string[] flags = CultureInfo.InvariantCulture.TextInfo.ToLower(options).Split(null);
            for (int i = 0; i < flags.Length; i++)
            {
                string val = flags[i].Trim();
                if (val.StartsWith("/out:", StringComparison.Ordinal))
                {
                    outName = val.Substring(5);
                }
            }
            return outName;
        }

        internal static string GetCurrentUser()
        {
            return "";
        }
    }
}


