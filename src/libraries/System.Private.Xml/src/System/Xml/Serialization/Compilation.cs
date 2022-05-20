// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.IO;
using System.Text;
using System.Threading;
using System.Security;
using System.Globalization;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace System.Xml.Serialization
{
    internal sealed class TempAssembly
    {
        internal const string GeneratedAssemblyNamespace = "Microsoft.Xml.Serialization.GeneratedAssembly";
        private readonly Assembly? _assembly;
        private XmlSerializerImplementation? _contract;
        private IDictionary? _writerMethods;
        private IDictionary? _readerMethods;
        private TempMethodDictionary? _methods;

        internal sealed class TempMethod
        {
            internal MethodInfo? writeMethod;
            internal MethodInfo? readMethod;
            internal string? name;
            internal string? ns;
            internal bool isSoap;
            internal string? methodKey;
        }

        private TempAssembly()
        {
        }

        internal TempAssembly(XmlMapping[] xmlMappings, Assembly assembly, XmlSerializerImplementation? contract)
        {
            _assembly = assembly;
            InitAssemblyMethods(xmlMappings);
            _contract = contract;
        }

        [RequiresUnreferencedCode("calls GenerateRefEmitAssembly")]
        internal TempAssembly(XmlMapping[] xmlMappings, Type?[] types, string? defaultNamespace, string? location)
        {
            bool containsSoapMapping = false;
            for (int i = 0; i < xmlMappings.Length; i++)
            {
                xmlMappings[i].CheckShallow();
                if (xmlMappings[i].IsSoap)
                {
                    containsSoapMapping = true;
                }
            }

            // We will make best effort to use RefEmit for assembly generation
            bool fallbackToCSharpAssemblyGeneration = false;

            if (!containsSoapMapping && !TempAssembly.UseLegacySerializerGeneration)
            {
                try
                {
                    _assembly = GenerateRefEmitAssembly(xmlMappings, types, defaultNamespace);
                }
                // Only catch and handle known failures with RefEmit
                catch (CodeGeneratorConversionException)
                {
                    fallbackToCSharpAssemblyGeneration = true;
                }
                // Add other known exceptions here...
                //
            }
            else
            {
                fallbackToCSharpAssemblyGeneration = true;
            }

            if (fallbackToCSharpAssemblyGeneration)
            {
                throw new PlatformNotSupportedException(SR.CompilingScriptsNotSupported);
            }

#if DEBUG
            // use exception in the place of Debug.Assert to avoid throwing asserts from a server process such as aspnet_ewp.exe
            if (_assembly == null)
                throw new InvalidOperationException(SR.Format(SR.XmlInternalErrorDetails, "Failed to generate XmlSerializer assembly, but did not throw"));
#endif
            InitAssemblyMethods(xmlMappings);
        }

        internal static bool UseLegacySerializerGeneration
        {
            get
            {
                return false;
            }
        }

        internal XmlSerializerImplementation Contract
        {
            [RequiresUnreferencedCode("calls GetTypeFromAssembly")]
            get
            {
                if (_contract == null)
                {
                    _contract = (XmlSerializerImplementation)Activator.CreateInstance(GetTypeFromAssembly(_assembly!, "XmlSerializerContract"))!;
                }
                return _contract;
            }
        }

        internal void InitAssemblyMethods(XmlMapping[] xmlMappings)
        {
            _methods = new TempMethodDictionary();
            for (int i = 0; i < xmlMappings.Length; i++)
            {
                TempMethod method = new TempMethod();
                method.isSoap = xmlMappings[i].IsSoap;
                method.methodKey = xmlMappings[i].Key;
                XmlTypeMapping? xmlTypeMapping = xmlMappings[i] as XmlTypeMapping;
                if (xmlTypeMapping != null)
                {
                    method.name = xmlTypeMapping.ElementName;
                    method.ns = xmlTypeMapping.Namespace;
                }
                _methods.Add(xmlMappings[i].Key!, method);
            }
        }

        /// <devdoc>
        ///    <para>
        ///    Attempts to load pre-generated serialization assembly.
        ///    First check for the [XmlSerializerAssembly] attribute
        ///    </para>
        /// </devdoc>
        // SxS: This method does not take any resource name and does not expose any resources to the caller.
        // It's OK to suppress the SxS warning.
        [RequiresUnreferencedCode("calls LoadFrom")]
        [UnconditionalSuppressMessage("SingleFile", "IL3000: Avoid accessing Assembly file path when publishing as a single file",
            Justification = "Annotating this as dangerous will make the core of the serializer to be marked as not safe, instead " +
            "this pattern is only dangerous if using sgen only. See https://github.com/dotnet/runtime/issues/50820")]
        internal static Assembly? LoadGeneratedAssembly(Type type, string? defaultNamespace, out XmlSerializerImplementation? contract)
        {
            Assembly? serializer = null;
            contract = null;
            string? serializerName;

            using (AssemblyLoadContext.EnterContextualReflection(type.Assembly))
            {
                // check to see if we loading explicit pre-generated assembly
                object[] attrs = type.GetCustomAttributes(typeof(System.Xml.Serialization.XmlSerializerAssemblyAttribute), false);
                if (attrs.Length == 0)
                {
                    // Guess serializer name: if parent assembly signed use strong name
                    AssemblyName name = type.Assembly.GetName();
                    serializerName = Compiler.GetTempAssemblyName(name, defaultNamespace);
                    // use strong name
                    name.Name = serializerName;
                    name.CodeBase = null;
                    name.CultureInfo = CultureInfo.InvariantCulture;

                    try
                    {
                        serializer = Assembly.Load(name);
                    }
                    catch (Exception e)
                    {
                        if (e is OutOfMemoryException)
                        {
                            throw;
                        }
                    }

                    serializer ??= LoadAssemblyByPath(type, serializerName);

                    if (serializer == null)
                    {
                        if (XmlSerializer.Mode == SerializationMode.PreGenOnly)
                        {
                            throw new Exception(SR.Format(SR.FailLoadAssemblyUnderPregenMode, serializerName));
                        }

                        return null;
                    }

                    if (!IsSerializerVersionMatch(serializer, type, defaultNamespace))
                    {
                        XmlSerializationEventSource.Log.XmlSerializerExpired(serializerName, type.FullName!);
                        return null;
                    }
                }
                else
                {
                    System.Xml.Serialization.XmlSerializerAssemblyAttribute assemblyAttribute = (System.Xml.Serialization.XmlSerializerAssemblyAttribute)attrs[0];
                    if (assemblyAttribute.AssemblyName != null && assemblyAttribute.CodeBase != null)
                        throw new InvalidOperationException(SR.Format(SR.XmlPregenInvalidXmlSerializerAssemblyAttribute, "AssemblyName", "CodeBase"));

                    // found XmlSerializerAssemblyAttribute attribute, it should have all needed information to load the pre-generated serializer
                    if (assemblyAttribute.AssemblyName != null)
                    {
                        serializerName = assemblyAttribute.AssemblyName;
                        serializer = Assembly.Load(serializerName); // LoadWithPartialName just does this in .Net Core; changing the obsolete call.
                    }
                    else if (assemblyAttribute.CodeBase != null && assemblyAttribute.CodeBase.Length > 0)
                    {
                        serializerName = assemblyAttribute.CodeBase;
                        serializer = Assembly.LoadFrom(serializerName);
                    }
                    else
                    {
                        serializerName = type.Assembly.FullName;
                        serializer = type.Assembly;
                    }
                    if (serializer == null)
                    {
                        throw new FileNotFoundException(null, serializerName);
                    }
                }
                Type contractType = GetTypeFromAssembly(serializer, "XmlSerializerContract");
                contract = (XmlSerializerImplementation)Activator.CreateInstance(contractType)!;
                if (contract.CanSerialize(type))
                    return serializer;
            }

            return null;
        }

        [RequiresUnreferencedCode("calls LoadFile")]
        [UnconditionalSuppressMessage("SingleFile", "IL3000: Avoid accessing Assembly file path when publishing as a single file",
            Justification = "Annotating this as dangerous will make the core of the serializer to be marked as not safe, instead " +
            "this pattern is only dangerous if using sgen only. See https://github.com/dotnet/runtime/issues/50820")]
        private static Assembly? LoadAssemblyByPath(Type type, string assemblyName)
        {
            Assembly? assembly = null;
            string? path = null;

            try
            {
                if (!string.IsNullOrEmpty(type.Assembly.Location))
                {
                    path = Path.Combine(Path.GetDirectoryName(type.Assembly.Location)!, $"{assemblyName}.dll");
                }

                if ((string.IsNullOrEmpty(path) || !File.Exists(path)) && !string.IsNullOrEmpty(Assembly.GetEntryAssembly()?.Location))
                {
                    path = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!, $"{assemblyName}.dll");
                }

                if ((string.IsNullOrEmpty(path) || !File.Exists(path)) && !string.IsNullOrEmpty(AppContext.BaseDirectory))
                {
                    path = Path.Combine(Path.GetDirectoryName(AppContext.BaseDirectory)!, $"{assemblyName}.dll");
                }

                if (!string.IsNullOrEmpty(path))
                {
                    assembly = Assembly.LoadFile(path);
                }
            }
            catch (Exception e)
            {
                if (e is ThreadAbortException || e is StackOverflowException || e is OutOfMemoryException)
                {
                    throw;
                }
            }

            return assembly;
        }

        private static bool IsSerializerVersionMatch(Assembly serializer, Type type, string? defaultNamespace)
        {
            if (serializer == null)
                return false;
            object[] attrs = serializer.GetCustomAttributes(typeof(XmlSerializerVersionAttribute), false);
            if (attrs.Length != 1)
                return false;

            XmlSerializerVersionAttribute assemblyInfo = (XmlSerializerVersionAttribute)attrs[0];
            if (assemblyInfo.ParentAssemblyId == GenerateAssemblyId(type) && assemblyInfo.Namespace == defaultNamespace)
                return true;
            return false;
        }

        private static string GenerateAssemblyId(Type type)
        {
            Module[] modules = type.Assembly.GetModules();
            var list = new ArrayList();
            for (int i = 0; i < modules.Length; i++)
            {
                list.Add(modules[i].ModuleVersionId.ToString());
            }

            list.Sort();
            var sb = new StringBuilder();

            for (int i = 0; i < list.Count; i++)
            {
                sb.Append(list[i]!.ToString());
                sb.Append(',');
            }

            return sb.ToString();
        }

        [RequiresUnreferencedCode("calls GenerateBegin")]
        internal static bool GenerateSerializerToStream(XmlMapping[] xmlMappings, Type?[] types, string? defaultNamespace, Assembly? assembly, Hashtable assemblies, Stream stream)
        {
            var compiler = new Compiler();
            var scopeTable = new Hashtable();
            foreach (XmlMapping mapping in xmlMappings)
                scopeTable[mapping.Scope!] = mapping;

            var scopes = new TypeScope[scopeTable.Keys.Count];
            scopeTable.Keys.CopyTo(scopes, 0);
            assemblies.Clear();
            var importedTypes = new Hashtable();

            foreach (TypeScope scope in scopes)
            {
                foreach (Type t in scope.Types)
                {
                    compiler.AddImport(t, importedTypes);
                    Assembly a = t.Assembly;
                    string name = a.FullName!;
                    if (assemblies[name] != null)
                    {
                        continue;
                    }

                    assemblies[name] = a;
                }
            }

            for (int i = 0; i < types.Length; i++)
            {
                compiler.AddImport(types[i], importedTypes);
            }

            var writer = new IndentedWriter(compiler.Source, false);
            writer.WriteLine("[assembly:System.Security.AllowPartiallyTrustedCallers()]");
            writer.WriteLine("[assembly:System.Security.SecurityTransparent()]");
            writer.WriteLine("[assembly:System.Security.SecurityRules(System.Security.SecurityRuleSet.Level1)]");

            if (assembly != null && types.Length > 0)
            {
                for (int i = 0; i < types.Length; i++)
                {
                    Type? type = types[i];
                    if (type == null)
                    {
                        continue;
                    }

                    if (DynamicAssemblies.IsTypeDynamic(type))
                    {
                        throw new InvalidOperationException(SR.Format(SR.XmlPregenTypeDynamic, types[i]!.FullName));
                    }
                }

                writer.Write("[assembly:");
                writer.Write(typeof(XmlSerializerVersionAttribute).FullName);
                writer.Write("(");
                writer.Write("ParentAssemblyId=");
                ReflectionAwareCodeGen.WriteQuotedCSharpString(writer, GenerateAssemblyId(types[0]!));
                writer.Write(", Version=");
                ReflectionAwareCodeGen.WriteQuotedCSharpString(writer, ThisAssembly.Version);
                if (defaultNamespace != null)
                {
                    writer.Write(", Namespace=");
                    ReflectionAwareCodeGen.WriteQuotedCSharpString(writer, defaultNamespace);
                }

                writer.WriteLine(")]");
            }

            var classes = new CodeIdentifiers();
            classes.AddUnique("XmlSerializationWriter", "XmlSerializationWriter");
            classes.AddUnique("XmlSerializationReader", "XmlSerializationReader");
            string? suffix = null;

            if (types != null && types.Length == 1 && types[0] != null)
            {
                suffix = CodeIdentifier.MakeValid(types[0]!.Name);
                if (types[0]!.IsArray)
                {
                    suffix += "Array";
                }
            }

            writer.WriteLine($"namespace {GeneratedAssemblyNamespace} {{");
            writer.Indent++;
            writer.WriteLine();

            string writerClass = $"XmlSerializationWriter{suffix}";
            writerClass = classes.AddUnique(writerClass, writerClass);
            var writerCodeGen = new XmlSerializationWriterCodeGen(writer, scopes, "public", writerClass);
            writerCodeGen.GenerateBegin();
            string?[] writeMethodNames = new string[xmlMappings.Length];

            for (int i = 0; i < xmlMappings.Length; i++)
            {
                writeMethodNames[i] = writerCodeGen.GenerateElement(xmlMappings[i]);
            }

            writerCodeGen.GenerateEnd();
            writer.WriteLine();

            string readerClass = $"XmlSerializationReader{suffix}";
            readerClass = classes.AddUnique(readerClass, readerClass);
            var readerCodeGen = new XmlSerializationReaderCodeGen(writer, scopes, "public", readerClass);
            readerCodeGen.GenerateBegin();
            string?[] readMethodNames = new string[xmlMappings.Length];
            for (int i = 0; i < xmlMappings.Length; i++)
            {
                readMethodNames[i] = readerCodeGen.GenerateElement(xmlMappings[i])!;
            }

            readerCodeGen.GenerateEnd(readMethodNames, xmlMappings, types);

            string baseSerializer = readerCodeGen.GenerateBaseSerializer("XmlSerializer1", readerClass, writerClass, classes);
            var serializers = new Hashtable();
            for (int i = 0; i < xmlMappings.Length; i++)
            {
                if (serializers[xmlMappings[i].Key!] == null)
                {
                    serializers[xmlMappings[i].Key!] = readerCodeGen.GenerateTypedSerializer(readMethodNames[i], writeMethodNames[i], xmlMappings[i], classes, baseSerializer, readerClass, writerClass);
                }
            }

            readerCodeGen.GenerateSerializerContract("XmlSerializerContract", xmlMappings, types!, readerClass, readMethodNames, writerClass, writeMethodNames, serializers);
            writer.Indent--;
            writer.WriteLine("}");

            string codecontent = compiler.Source.ToString()!;
            byte[] info = new UTF8Encoding(true).GetBytes(codecontent);
            stream.Write(info, 0, info.Length);
            stream.Flush();
            return true;
        }

        [RequiresUnreferencedCode("calls GenerateElement")]
        internal static Assembly GenerateRefEmitAssembly(XmlMapping[] xmlMappings, Type?[] types, string? defaultNamespace)
        {
            var mainType = (types.Length > 0) ? types[0] : null;
            Assembly? mainAssembly = mainType?.Assembly;
            var scopeTable = new Dictionary<TypeScope, XmlMapping>();
            foreach (XmlMapping mapping in xmlMappings)
                scopeTable[mapping.Scope!] = mapping;
            TypeScope[] scopes = new TypeScope[scopeTable.Keys.Count];
            scopeTable.Keys.CopyTo(scopes, 0);

            using (AssemblyLoadContext.EnterContextualReflection(mainAssembly))
            {
                // Before generating any IL, check each mapping and supported type to make sure
                // they are compatible with the current ALC
                for (int i = 0; i < types.Length; i++)
                    VerifyLoadContext(types[i], mainAssembly);
                foreach (var mapping in xmlMappings)
                    VerifyLoadContext(mapping.Accessor.Mapping?.TypeDesc?.Type, mainAssembly);

                string assemblyName = "Microsoft.GeneratedCode";
                AssemblyBuilder assemblyBuilder = CodeGenerator.CreateAssemblyBuilder(assemblyName);
                // Add AssemblyVersion attribute to match parent assembly version
                if (mainType != null)
                {
                    ConstructorInfo AssemblyVersionAttribute_ctor = typeof(AssemblyVersionAttribute).GetConstructor(
                        new Type[] { typeof(string) }
                        )!;
                    string assemblyVersion = mainType.Assembly.GetName().Version!.ToString();
                    assemblyBuilder.SetCustomAttribute(new CustomAttributeBuilder(AssemblyVersionAttribute_ctor, new object[] { assemblyVersion }));
                }
                CodeIdentifiers classes = new CodeIdentifiers();
                classes.AddUnique("XmlSerializationWriter", "XmlSerializationWriter");
                classes.AddUnique("XmlSerializationReader", "XmlSerializationReader");
                string? suffix = null;
                if (mainType != null)
                {
                    suffix = CodeIdentifier.MakeValid(mainType.Name);
                    if (mainType.IsArray)
                    {
                        suffix += "Array";
                    }
                }

                ModuleBuilder moduleBuilder = CodeGenerator.CreateModuleBuilder(assemblyBuilder, assemblyName);

            string writerClass = $"XmlSerializationWriter{suffix}";
            writerClass = classes.AddUnique(writerClass, writerClass);
            XmlSerializationWriterILGen writerCodeGen = new XmlSerializationWriterILGen(scopes, "public", writerClass);
            writerCodeGen.ModuleBuilder = moduleBuilder;

                writerCodeGen.GenerateBegin();
                string[] writeMethodNames = new string[xmlMappings.Length];

                for (int i = 0; i < xmlMappings.Length; i++)
                {
                    writeMethodNames[i] = writerCodeGen.GenerateElement(xmlMappings[i])!;
                }
                Type writerType = writerCodeGen.GenerateEnd();

            string readerClass = $"XmlSerializationReader{suffix}";
            readerClass = classes.AddUnique(readerClass, readerClass);
            XmlSerializationReaderILGen readerCodeGen = new XmlSerializationReaderILGen(scopes, "public", readerClass);

                readerCodeGen.ModuleBuilder = moduleBuilder;
                readerCodeGen.CreatedTypes.Add(writerType.Name, writerType);

                readerCodeGen.GenerateBegin();
                string[] readMethodNames = new string[xmlMappings.Length];
                for (int i = 0; i < xmlMappings.Length; i++)
                {
                    readMethodNames[i] = readerCodeGen.GenerateElement(xmlMappings[i])!;
                }
                readerCodeGen.GenerateEnd(readMethodNames, xmlMappings, types!);

                string baseSerializer = readerCodeGen.GenerateBaseSerializer("XmlSerializer1", readerClass, writerClass, classes);
                var serializers = new Dictionary<string, string>();
                for (int i = 0; i < xmlMappings.Length; i++)
                {
                    if (!serializers.ContainsKey(xmlMappings[i].Key!))
                    {
                        serializers[xmlMappings[i].Key!] = readerCodeGen.GenerateTypedSerializer(readMethodNames[i], writeMethodNames[i], xmlMappings[i], classes, baseSerializer, readerClass, writerClass);
                    }
                }
                readerCodeGen.GenerateSerializerContract("XmlSerializerContract", xmlMappings, types!, readerClass, readMethodNames, writerClass, writeMethodNames, serializers);

                return writerType.Assembly;
            }
        }

        private static MethodInfo GetMethodFromType(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type, string methodName)
        {
            MethodInfo? method = type.GetMethod(methodName);
            if (method != null)
                return method;

            // Not support pregen.  Workaround SecurityCritical required for assembly.CodeBase api.
            MissingMethodException missingMethod = new MissingMethodException($"{type.FullName}::{methodName}");
            throw missingMethod;
        }

        [RequiresUnreferencedCode("calls GetType")]
        internal static Type GetTypeFromAssembly(Assembly assembly, string typeName)
        {
            typeName = $"{GeneratedAssemblyNamespace}.{typeName}";
            Type? type = assembly.GetType(typeName);
            if (type == null)
                throw new InvalidOperationException(SR.Format(SR.XmlMissingType, typeName, assembly.FullName));
            return type;
        }

        internal bool CanRead(XmlMapping mapping, XmlReader xmlReader)
        {
            if (mapping == null)
                return false;

            if (mapping.Accessor.Any)
            {
                return true;
            }
            TempMethod method = _methods![mapping.Key!];
            return xmlReader.IsStartElement(method.name!, method.ns!);
        }

        [return: NotNullIfNotNull("encodingStyle")]
        private string? ValidateEncodingStyle(string? encodingStyle, string methodKey)
        {
            if (encodingStyle != null && encodingStyle.Length > 0)
            {
                if (_methods![methodKey].isSoap)
                {
                    if (encodingStyle != Soap.Encoding && encodingStyle != Soap12.Encoding)
                    {
                        throw new InvalidOperationException(SR.Format(SR.XmlInvalidEncoding3, encodingStyle, Soap.Encoding, Soap12.Encoding));
                    }
                }
                else
                {
                    throw new InvalidOperationException(SR.Format(SR.XmlInvalidEncodingNotEncoded1, encodingStyle));
                }
            }
            else
            {
                if (_methods![methodKey].isSoap)
                {
                    encodingStyle = Soap.Encoding;
                }
            }
            return encodingStyle;
        }

        internal static void VerifyLoadContext(Type? t, Assembly? assembly)
        {
            // The quick case, t is null or in the same assembly
            if (t == null || assembly == null || t.Assembly == assembly)
                return;

            // No worries if the type is not collectible
            var typeALC = AssemblyLoadContext.GetLoadContext(t.Assembly);
            if (typeALC == null || !typeALC.IsCollectible)
                return;

            // Collectible types should be in the same collectible context
            var baseALC = AssemblyLoadContext.GetLoadContext(assembly) ?? AssemblyLoadContext.CurrentContextualReflectionContext;
            if (typeALC != baseALC)
                throw new InvalidOperationException(SR.Format(SR.XmlTypeInBadLoadContext, t.FullName));
        }

        [RequiresUnreferencedCode("calls Contract")]
        internal object? InvokeReader(XmlMapping mapping, XmlReader xmlReader, XmlDeserializationEvents events, string? encodingStyle)
        {
            XmlSerializationReader? reader;
            try
            {
                encodingStyle = ValidateEncodingStyle(encodingStyle, mapping.Key!);
                reader = Contract.Reader;
                reader.Init(xmlReader, events, encodingStyle, this);
                if (_methods![mapping.Key!].readMethod == null)
                {
                    if (_readerMethods == null)
                    {
                        _readerMethods = Contract.ReadMethods;
                    }
                    string? methodName = (string?)_readerMethods[mapping.Key!];
                    if (methodName == null)
                    {
                        throw new InvalidOperationException(SR.Format(SR.XmlNotSerializable, mapping.Accessor.Name));
                    }
                    _methods[mapping.Key!].readMethod = GetMethodFromType(reader.GetType(), methodName);
                }
                return _methods[mapping.Key!].readMethod!.Invoke(reader, Array.Empty<object>());
            }
            catch (SecurityException e)
            {
                throw new InvalidOperationException(SR.XmlNoPartialTrust, e);
            }
        }

        [RequiresUnreferencedCode("calls Contract")]
        internal void InvokeWriter(XmlMapping mapping, XmlWriter xmlWriter, object? o, XmlSerializerNamespaces? namespaces, string? encodingStyle, string? id)
        {
            XmlSerializationWriter? writer;
            try
            {
                encodingStyle = ValidateEncodingStyle(encodingStyle, mapping.Key!);
                writer = Contract.Writer;
                writer.Init(xmlWriter, namespaces, encodingStyle, id, this);
                if (_methods![mapping.Key!].writeMethod == null)
                {
                    if (_writerMethods == null)
                    {
                        _writerMethods = Contract.WriteMethods;
                    }
                    string? methodName = (string?)_writerMethods[mapping.Key!];
                    if (methodName == null)
                    {
                        throw new InvalidOperationException(SR.Format(SR.XmlNotSerializable, mapping.Accessor.Name));
                    }
                    _methods[mapping.Key!].writeMethod = GetMethodFromType(writer.GetType(), methodName);
                }
                _methods[mapping.Key!].writeMethod!.Invoke(writer, new object?[] { o });
            }
            catch (SecurityException e)
            {
                throw new InvalidOperationException(SR.XmlNoPartialTrust, e);
            }
        }

        internal sealed class TempMethodDictionary : Dictionary<string, TempMethod>
        {
        }
    }

    internal sealed class TempAssemblyCacheKey
    {
        private readonly string? _ns;
        private readonly Type _type;

        internal TempAssemblyCacheKey(string? ns, Type type)
        {
            _type = type;
            _ns = ns;
        }

        public override bool Equals([NotNullWhen(true)] object? o)
        {
            TempAssemblyCacheKey? key = o as TempAssemblyCacheKey;
            if (key == null)
                return false;
            return (key._type == _type && key._ns == _ns);
        }

        public override int GetHashCode()
        {
            return ((_ns != null ? _ns.GetHashCode() : 0) ^ (_type != null ? _type.GetHashCode() : 0));
        }
    }

    internal sealed class TempAssemblyCache
    {
        private Dictionary<TempAssemblyCacheKey, TempAssembly> _fastCache = new Dictionary<TempAssemblyCacheKey, TempAssembly>();
        private ConditionalWeakTable<Assembly, Dictionary<TempAssemblyCacheKey, TempAssembly>> _collectibleCaches = new ConditionalWeakTable<Assembly, Dictionary<TempAssemblyCacheKey, TempAssembly>>();

        internal TempAssembly? this[string? ns, Type t]
        {
            get
            {
                TempAssembly? tempAssembly;
                TempAssemblyCacheKey key = new TempAssemblyCacheKey(ns, t);

                if (_fastCache.TryGetValue(key, out tempAssembly))
                    return tempAssembly;

                if (_collectibleCaches.TryGetValue(t.Assembly, out var cCache))
                    cCache.TryGetValue(key, out tempAssembly);

                return tempAssembly;
            }
        }

        internal void Add(string? ns, Type t, TempAssembly assembly)
        {
            lock (this)
            {
                TempAssembly? tempAssembly = this[ns, t];
                if (tempAssembly == assembly)
                    return;

                AssemblyLoadContext? alc = AssemblyLoadContext.GetLoadContext(t.Assembly);
                TempAssemblyCacheKey key = new TempAssemblyCacheKey(ns, t);
                Dictionary<TempAssemblyCacheKey, TempAssembly>? cache;

                if (alc != null && alc.IsCollectible)
                {
                    cache = _collectibleCaches.TryGetValue(t.Assembly, out var c)   // Clone or create
                        ? new Dictionary<TempAssemblyCacheKey, TempAssembly>(c)
                        : new Dictionary<TempAssemblyCacheKey, TempAssembly>();
                    cache[key] = assembly;
                    _collectibleCaches.AddOrUpdate(t.Assembly, cache);
                }
                else
                {
                    cache = new Dictionary<TempAssemblyCacheKey, TempAssembly>(_fastCache); // Clone
                    cache[key] = assembly;
                    _fastCache = cache;
                }
            }
        }
    }

    internal static class ThisAssembly
    {
        internal const string Version = "1.0.0.0";
        internal const string InformationalVersion = "1.0.0.0";
    }
}
