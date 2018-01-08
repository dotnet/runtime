// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml.Serialization;

namespace Microsoft.XmlSerializer.Generator
{
    internal class Sgen
    {
        public static int Main(string[] args)
        {
            Sgen sgen = new Sgen();
            return sgen.Run(args);
        }

        private int Run(string[] args)
        {
            string assembly = null;
            List<string> types = new List<string>();
            string codePath = null;
            var errs = new ArrayList();
            bool force = false;
            bool proxyOnly = false;
            bool disableRun = true;
            bool noLogo = false;
            bool parsableErrors = false;
            bool silent = false;
            bool warnings = false;

            try
            {
                for (int i = 0; i < args.Length; i++)
                {
                    string arg = args[i];
                    string value = string.Empty;

                    if (arg.StartsWith("/") || arg.StartsWith("-"))
                    {
                        int colonPos = arg.IndexOf(":");
                        if (colonPos != -1)
                        {
                            value = arg.Substring(colonPos + 1).Trim();
                            arg = arg.Substring(0, colonPos).Trim();
                        }
                    }

                    string originalArg = arg;
                    arg = arg.ToLower(CultureInfo.InvariantCulture);

                    if (ArgumentMatch(arg, "?") || ArgumentMatch(arg, "help"))
                    {
                        WriteHeader();
                        WriteHelp();
                        return 0;
                    }
                    else if (ArgumentMatch(arg, "force"))
                    {
                        force = true;
                    }
                    else if (ArgumentMatch(arg, "proxytypes"))
                    {
                        proxyOnly = true;
                    }
                    else if (ArgumentMatch(arg, "out"))
                    {
                        if (codePath != null)
                        {
                            errs.Add(SR.Format(SR.ErrInvalidArgument, "/out", arg));
                        }

                        codePath = value;
                    }
                    else if (ArgumentMatch(arg, "type"))
                    {
                        if (value != string.Empty)
                        {
                            string[] typelist = value.Split(';');
                            foreach (var type in typelist)
                            {
                                types.Add(type);
                            }
                        }
                    }
                    else if (ArgumentMatch(arg, "assembly"))
                    {
                        if (assembly != null)
                        {
                            errs.Add(SR.Format(SR.ErrInvalidArgument, "/assembly", arg));
                        }

                        assembly = value;
                    }
                    else if (ArgumentMatch(arg, "quiet"))
                    {
                        disableRun = false;
                    }
                    else if (ArgumentMatch(arg, "nologo"))
                    {
                        noLogo = true;
                    }
                    else if (ArgumentMatch(arg, "silent"))
                    {
                        silent = true;
                    }
                    else if (ArgumentMatch(arg, "parsableerrors"))
                    {
                        parsableErrors = true;
                    }
                    else if (ArgumentMatch(arg, "verbose"))
                    {
                        warnings = true;
                    }
                    else
                    {
                        if (arg.EndsWith(".dll") || arg.EndsWith(".exe"))
                        {
                            if (assembly != null)
                            {
                                errs.Add(SR.Format(SR.ErrInvalidArgument, "/assembly", arg));
                            }

                            assembly = originalArg;
                        }
                        else
                        {
                            errs.Add(SR.Format(SR.ErrInvalidArgument, arg));
                        }
                    }
                }

                if (!noLogo)
                {
                    WriteHeader();
                }

                if (errs.Count > 0)
                {
                    foreach (string err in errs)
                    {
                        Console.Error.WriteLine(FormatMessage(parsableErrors, true, SR.Format(SR.Warning, err)));
                    }
                }

                if (args.Length == 0 || assembly == null)
                {
                    if (assembly == null)
                    {
                        Console.Error.WriteLine(FormatMessage(parsableErrors, false, SR.Format(SR.ErrMissingRequiredArgument, SR.Format(SR.ErrAssembly, "assembly"))));
                    }

                    WriteHelp();
                    return 0;
                }

                if(disableRun)
                {
                    Console.WriteLine("This tool is not intended to be used directly.");
                    Console.WriteLine("Please refer to https://go.microsoft.com/fwlink/?linkid=858594 on how to use it.");
                    return 0;
                }

                GenerateFile(types, assembly, proxyOnly, silent, warnings, force, codePath, parsableErrors);
            }
            catch (Exception e)
            {
                if (e is ThreadAbortException || e is StackOverflowException || e is OutOfMemoryException)
                {
                    throw;
                }

                WriteError(e, parsableErrors);
                return 1;
            }

            return 0;
        }

        private void GenerateFile(List<string> typeNames, string assemblyName, bool proxyOnly, bool silent, bool warnings, bool force, string outputDirectory, bool parsableerrors)
        {
            Assembly assembly = LoadAssembly(assemblyName, true);
            Type[] types;

            if (typeNames == null || typeNames.Count == 0)
            {
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException typeException)
                {
                    List<Type> loadedTypes = new List<Type>();
                    foreach (Type type in typeException.Types)
                    {
                        if (type != null)
                        {
                            loadedTypes.Add(type);
                        }
                    }

                    types = loadedTypes.ToArray();
                }
            }
            else
            {
                types = new Type[typeNames.Count];
                int typeIndex = 0;
                foreach (string typeName in typeNames)
                {
                    Type type = assembly.GetType(typeName);
                    if (type == null)
                    {
                        Console.Error.WriteLine(FormatMessage(parsableerrors, false, SR.Format(SR.ErrorDetails, SR.Format(SR.ErrLoadType, typeName, assemblyName))));
                    }

                    types[typeIndex++] = type;
                }
            }

            var mappings = new ArrayList();
            var importedTypes = new ArrayList();
            var importer = new XmlReflectionImporter();

            for (int i = 0; i < types.Length; i++)
            {
                Type type = types[i];

                if (type != null)
                {
                    bool isObsolete = false;
                    object[] obsoleteAttributes = type.GetCustomAttributes(typeof(ObsoleteAttribute), false);
                    foreach (object attribute in obsoleteAttributes)
                    {
                        if (((ObsoleteAttribute)attribute).IsError)
                        {
                            isObsolete = true;
                            break;
                        }
                    }

                    if (isObsolete)
                    {
                        continue;
                    }
                }

                if (!proxyOnly)
                {
                    ImportType(type, mappings, importedTypes, warnings, importer, parsableerrors);
                }
            }

            if (importedTypes.Count > 0)
            {
                var serializableTypes = (Type[])importedTypes.ToArray(typeof(Type));
                var allMappings = (XmlMapping[])mappings.ToArray(typeof(XmlMapping));

                bool gac = assembly.GlobalAssemblyCache;
                outputDirectory = outputDirectory == null ? (gac ? Environment.CurrentDirectory : Path.GetDirectoryName(assembly.Location)) : outputDirectory;
                string serializerName = GetXmlSerializerAssemblyName(serializableTypes[0], null);
                string codePath = Path.Combine(outputDirectory, serializerName + ".cs");

                if (!force)
                {
                    if (File.Exists(codePath))
                        throw new InvalidOperationException(SR.Format(SR.ErrSerializerExists, codePath, "force"));
                }

                if (Directory.Exists(codePath))
                {
                    throw new InvalidOperationException(SR.Format(SR.ErrDirectoryExists, codePath));
                }

                if (!Directory.Exists(outputDirectory))
                {
                    throw new ArgumentException(SR.Format(SR.ErrDirectoryNotExists, codePath, outputDirectory));
                }

                bool success = false;
                bool toDeleteFile = true;

                try
                {
                    if (File.Exists(codePath))
                    {
                        File.Delete(codePath);
                    }

                    using (FileStream fs = File.Create(codePath))
                    {
                        MethodInfo method = typeof(System.Xml.Serialization.XmlSerializer).GetMethod("GenerateSerializer", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                        if (method == null)
                        {
                            Console.Error.WriteLine(FormatMessage(parsableerrors: false, warning: false, message: SR.GenerateSerializerNotFound));
                        }
                        else
                        { 
                            success = (bool)method.Invoke(null, new object[] { serializableTypes, allMappings, fs });
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    toDeleteFile = false;
                    throw new UnauthorizedAccessException(SR.Format(SR.DirectoryAccessDenied, outputDirectory));
                }
                finally
                {
                    if (!success && toDeleteFile && File.Exists(codePath))
                    {
                        File.Delete(codePath);
                    }
                }

                if (success)
                {
                    if (!silent)
                    {
                        Console.Out.WriteLine(SR.Format(SR.InfoFileName, codePath));
                        Console.Out.WriteLine(SR.Format(SR.InfoGeneratedFile, assembly.Location, codePath));
                    }
                }
                else
                {
                    Console.Out.WriteLine(FormatMessage(parsableerrors, false, SR.Format(SR.ErrGenerationFailed, assembly.Location)));
                }
            }
            else
            {
                Console.Out.WriteLine(FormatMessage(parsableerrors, true, SR.Format(SR.InfoNoSerializableTypes, assembly.Location)));
            }
        }

        // assumes all same case.        
        private bool ArgumentMatch(string arg, string formal)
        {
            if (arg[0] != '/' && arg[0] != '-')
            {
                return false;
            }

            arg = arg.Substring(1);
            return (arg == formal || (arg.Length == 1 && arg[0] == formal[0]));
        }

        private void ImportType(Type type, ArrayList mappings, ArrayList importedTypes, bool verbose, XmlReflectionImporter importer, bool parsableerrors)
        {
            XmlTypeMapping xmlTypeMapping = null;
            var localImporter = new XmlReflectionImporter();
            try
            {
                xmlTypeMapping = localImporter.ImportTypeMapping(type);
            }
            catch (Exception e)
            {
                if (e is ThreadAbortException || e is StackOverflowException || e is OutOfMemoryException)
                {
                    throw;
                }

                if (verbose)
                {
                    Console.Out.WriteLine(FormatMessage(parsableerrors, true, SR.Format(SR.InfoIgnoreType, type.FullName)));
                    WriteWarning(e, parsableerrors);
                }

                return;
            }
            if (xmlTypeMapping != null)
            {
                xmlTypeMapping = importer.ImportTypeMapping(type);
                mappings.Add(xmlTypeMapping);
                importedTypes.Add(type);
            }
        }

        private static Assembly LoadAssembly(string assemblyName, bool throwOnFail)
        {
            Assembly assembly = null;
            string path = Path.IsPathRooted(assemblyName) ? assemblyName : Path.GetFullPath(assemblyName);
            assembly = Assembly.LoadFile(path);
            if (assembly == null)
            {
                throw new InvalidOperationException(SR.Format(SR.ErrLoadAssembly, assemblyName));
            }

            return assembly;
        }

        private void WriteHeader()
        {
            // do not localize Copyright header
            Console.WriteLine(String.Format(CultureInfo.CurrentCulture, "[Microsoft (R) .NET Core Xml Serialization Generation Utility, Version {0}]", ThisAssembly.InformationalVersion));
            Console.WriteLine("Copyright (C) Microsoft Corporation. All rights reserved.");
        }

        private void WriteHelp()
        {
            Console.Out.WriteLine(SR.Format(SR.HelpDescription));
            Console.Out.WriteLine(SR.Format(SR.HelpUsage, this.GetType().Assembly.GetName().Name));
            Console.Out.WriteLine(SR.Format(SR.HelpDevOptions));
            Console.Out.WriteLine(SR.Format(SR.HelpAssembly, "/assembly:", "/a:"));
            Console.Out.WriteLine(SR.Format(SR.HelpType, "/type:", "/t:"));
            Console.Out.WriteLine(SR.Format(SR.HelpProxy, "/proxytypes", "/p"));
            Console.Out.WriteLine(SR.Format(SR.HelpForce, "/force", "/f"));
            Console.Out.WriteLine(SR.Format(SR.HelpOut, "/out:", "/o:"));

            Console.Out.WriteLine(SR.Format(SR.HelpMiscOptions));
            Console.Out.WriteLine(SR.Format(SR.HelpHelp, "/?", "/help"));
        }

        private static string FormatMessage(bool parsableerrors, bool warning, string message)
        {
            return FormatMessage(parsableerrors, warning, "SGEN1", message);
        }

        private static string FormatMessage(bool parsableerrors, bool warning, string code, string message)
        {
            if (!parsableerrors)
            {
                return message;
            }

            return "SGEN: " + (warning ? "warning " : "error ") + code + ": " + message;
        }

        private static void WriteError(Exception e, bool parsableerrors)
        {
            Console.Error.WriteLine(FormatMessage(parsableerrors, false, e.Message));
            if (e.InnerException != null)
            {
                WriteError(e.InnerException, parsableerrors);
            }
        }

        static void WriteWarning(Exception e, bool parsableerrors)
        {
            Console.Out.WriteLine(FormatMessage(parsableerrors, true, e.Message));
            if (e.InnerException != null)
            {
                WriteWarning(e.InnerException, parsableerrors);
            }
        }

        private static string GetXmlSerializerAssemblyName(Type type)
        {
            return GetXmlSerializerAssemblyName(type, null);
        }

        private static string GetXmlSerializerAssemblyName(Type type, string defaultNamespace)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }
            return GetTempAssemblyName(type.Assembly.GetName(), defaultNamespace);
        }

        private static string GetTempAssemblyName(AssemblyName parent, string ns)
        {
            return parent.Name + ".XmlSerializers" + (ns == null || ns.Length == 0 ? "" : "." + ns.GetHashCode());
        }
    }
}
