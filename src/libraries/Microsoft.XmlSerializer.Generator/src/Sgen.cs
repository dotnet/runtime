// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Serialization;

namespace Microsoft.XmlSerializer.Generator
{
    internal sealed class Sgen
    {
        public static int Main(string[] args)
        {
            Sgen sgen = new Sgen();
            return sgen.Run(args);
        }

        private static string s_references = string.Empty;
        private static readonly Dictionary<string, string> s_referencedic = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

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

            AppDomain.CurrentDomain.AssemblyResolve += SgenAssemblyResolver;

            try
            {
                args = ParseResponseFile(args);

                for (int i = 0; i < args.Length; i++)
                {
                    string arg = args[i];

                    if (ArgumentMatch(arg, "help") || ShortNameArgumentMatch(arg, "h"))
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
                    else if (ArgumentMatch(arg, "out") || ShortNameArgumentMatch(arg, "o"))
                    {
                        i++;
                        if (i >= args.Length || codePath != null)
                        {
                            errs.Add(SR.Format(SR.ErrInvalidArgument, arg));
                        }
                        else
                        {
                            codePath = args[i];
                        }
                    }
                    else if (ArgumentMatch(arg, "type"))
                    {
                        i++;
                        if (i >= args.Length)
                        {
                            errs.Add(SR.Format(SR.ErrInvalidArgument, arg));
                        }
                        else
                        {
                            string[] typelist = args[i].Split(';');
                            foreach (var type in typelist)
                            {
                                types.Add(type);
                            }
                        }
                    }
                    else if (ArgumentMatch(arg, "assembly") || ShortNameArgumentMatch(arg, "a"))
                    {
                        i++;
                        if (i >= args.Length || assembly != null)
                        {
                            errs.Add(SR.Format(SR.ErrInvalidArgument, arg));
                        }
                        else
                        {
                            assembly = args[i];
                        }
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
                    else if (ArgumentMatch(arg, "reference"))
                    {
                        i++;
                        if (i >= args.Length)
                        {
                            errs.Add(SR.Format(SR.ErrInvalidArgument, arg));
                        }
                        else
                        {
                            //if there are multiple --reference switches, the last one will overwrite previous ones.
                            s_references = args[i];
                        }
                    }
                    else
                    {
                        if (arg.EndsWith(".dll", StringComparison.InvariantCultureIgnoreCase) || arg.EndsWith(".exe", StringComparison.InvariantCultureIgnoreCase))
                        {
                            if (assembly != null)
                            {
                                errs.Add(SR.Format(SR.ErrInvalidArgument, arg));
                            }

                            assembly = arg;
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

                if (disableRun)
                {
                    Console.WriteLine("This tool is not intended to be used directly.");
                    Console.WriteLine("Please refer to https://go.microsoft.com/fwlink/?linkid=858594 on how to use it.");
                    return 0;
                }

                if (!string.IsNullOrEmpty(s_references))
                {
                    ParseReferences();
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

                try
                {
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
                }
                //Ignore the FileNotFoundException when call GetCustomAttributes e.g. if the type uses the attributes defined in a different assembly
                catch (FileNotFoundException e)
                {
                    if (warnings)
                    {
                        Console.Out.WriteLine(FormatMessage(parsableerrors, true, SR.Format(SR.InfoIgnoreType, type.FullName)));
                        WriteWarning(e, parsableerrors);
                    }

                    continue;
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

                if (!Directory.Exists(outputDirectory))
                {
                    //We need double quote the path to escpate the space in the path.
                    //However when a path ending with backslash, if followed by double quote, it becomes an escapte sequence
                    //e.g. "obj\Debug\netcoreapp2.0\", it will be converted as obj\Debug\netcoreapp2.0", which is not valid and not exist
                    //We need remove the ending quote for this situation
                    if (!outputDirectory.EndsWith("\"", StringComparison.Ordinal) || !Directory.Exists(outputDirectory = outputDirectory.Remove(outputDirectory.Length - 1)))
                    {
                        throw new ArgumentException(SR.Format(SR.ErrDirectoryNotExists, outputDirectory));
                    }
                }

                string serializerName = GetXmlSerializerAssemblyName(serializableTypes[0], null);
                string codePath = Path.Combine(outputDirectory, serializerName + ".cs");

                if (!force)
                {
                    if (File.Exists(codePath))
                        throw new InvalidOperationException(SR.Format(SR.ErrSerializerExists, codePath, nameof(force)));
                }

                if (Directory.Exists(codePath))
                {
                    throw new InvalidOperationException(SR.Format(SR.ErrDirectoryExists, codePath));
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


        private bool ArgumentMatch(string arg, string formal)
        {
            // Full name format, eg: --assembly
            if (arg.Length < 3 || arg[0] != '-' || arg[1] != '-')
            {
                return false;
            }
            arg = arg.Substring(2);
            return arg.Equals(formal, StringComparison.InvariantCultureIgnoreCase);
        }

        private bool ShortNameArgumentMatch(string arg, string shortName)
        {
            // Short name format, eg: -a
            if (arg.Length < 2 || arg[0] != '-')
            {
                return false;
            }
            arg = arg.Substring(1);
            return arg.Equals(shortName, StringComparison.InvariantCultureIgnoreCase);
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
            Console.WriteLine($".NET Xml Serialization Generation Utility, Version {ThisAssembly.InformationalVersion}]");
        }

        private void WriteHelp()
        {
            Console.Out.WriteLine(SR.HelpDescription);
            Console.Out.WriteLine(SR.Format(SR.HelpUsage, this.GetType().Assembly.GetName().Name.Substring("dotnet-".Length)));
            Console.Out.WriteLine(SR.HelpDevOptions);
            Console.Out.WriteLine(SR.Format(SR.HelpAssembly, "-a", "--assembly"));
            Console.Out.WriteLine(SR.Format(SR.HelpType, "--type"));
            Console.Out.WriteLine(SR.Format(SR.HelpProxy, "--proxytypes"));
            Console.Out.WriteLine(SR.Format(SR.HelpForce, "--force"));
            Console.Out.WriteLine(SR.Format(SR.HelpOut, "-o", "--out"));

            Console.Out.WriteLine(SR.HelpMiscOptions);
            Console.Out.WriteLine(SR.Format(SR.HelpHelp, "-h", "--help"));
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

        private static void WriteWarning(Exception e, bool parsableerrors)
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
                throw new ArgumentNullException(nameof(type));
            }
            return GetTempAssemblyName(type.Assembly.GetName(), defaultNamespace);
        }

        private static string GetTempAssemblyName(AssemblyName parent, string ns)
        {
            return parent.Name + ".XmlSerializers" + (ns == null || ns.Length == 0 ? "" : "." + ns.GetHashCode());
        }

        private static void ParseReferences()
        {
            var referencelist = new List<string>();
            if (s_references.Length > 0)
            {
                foreach (var entry in s_references.Split(';'))
                {
                    string trimentry = entry.Trim();
                    if (string.IsNullOrEmpty(trimentry))
                        continue;
                    referencelist.Add(trimentry);
                }
            }

            foreach (var reference in referencelist)
            {
                if (reference.EndsWith(".dll") || reference.EndsWith(".exe"))
                {
                    if (File.Exists(reference))
                    {
                        string filename = Path.GetFileNameWithoutExtension(reference);
                        if (!string.IsNullOrEmpty(filename))
                        {
                            s_referencedic.Add(filename, reference);
                        }
                    }
                }

            }
        }

        private static Assembly SgenAssemblyResolver(object source, ResolveEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(e.Name) || e.Name.Split(',').Length == 0)
                {
                    return null;
                }

                string assemblyname = e.Name.Split(',')[0];
                if (string.IsNullOrEmpty(assemblyname))
                {
                    return null;
                }

                if (s_referencedic.ContainsKey(assemblyname))
                {
                    string reference = s_referencedic[assemblyname];

                    // For System.ServiceModel.Primitives, we need to load its runtime assembly rather than reference assembly
                    if (assemblyname.Equals("System.ServiceModel.Primitives"))
                    {
                        // Replace "ref" with "lib" in the assembly's path, the path looks like:
                        // dir\.nuget\packages\system.servicemodel.primitives\4.5.3\ref\netstandard2.0\System.ServiceModel.Primitives.dll;
                        string pattern = @"\\ref\\netstandard\d*\.?\d*\.?\d*\\System.ServiceModel.Primitives.dll";
                        Match match = null;
                        try
                        {
                            match = Regex.Match(reference, pattern);
                        }
                        catch { }

                        if (match != null && match.Success)
                        {
                            int index = match.Index + 1;
                            StringBuilder sb = new StringBuilder(reference);
                            sb.Remove(index, "ref".Length);
                            sb.Insert(index, "lib");
                            reference = sb.ToString();
                        }
                    }

                    if (!string.IsNullOrEmpty(reference))
                    {
                        if (File.Exists(reference))
                        {
                            return Assembly.LoadFrom(reference);
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                if (exp is ThreadAbortException || exp is StackOverflowException || exp is OutOfMemoryException)
                {
                    throw;
                }

                WriteWarning(exp, true);
            }

            return null;
        }

        private string[] ParseResponseFile(string[] args)
        {
            var parsedArgs = new List<string>();
            foreach (string arg in args)
            {
                if (!arg.EndsWith(".rsp"))
                {
                    parsedArgs.Add(arg);
                }
                else
                {
                    try
                    {
                        foreach (string line in File.ReadAllLines(arg))
                        {
                            int i = line.Trim().IndexOf(' ');
                            if (i < 0)
                            {
                                parsedArgs.Add(line);
                            }
                            else
                            {
                                parsedArgs.Add(line.Substring(0, i));
                                parsedArgs.Add(line.Substring(i + 1));
                            }
                        }
                    }
                    //If for any reasons the rsp file is not generated, this argument will be ignored and serializer will be generated with default settings
                    catch (FileNotFoundException)
                    { }

                }
            }
            return parsedArgs.ToArray();
        }
    }
}
