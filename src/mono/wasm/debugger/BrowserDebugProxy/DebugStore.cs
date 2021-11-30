// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection.PortableExecutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis.Debugging;
using System.IO.Compression;
using System.Reflection;

namespace Microsoft.WebAssembly.Diagnostics
{
    internal static class PortableCustomDebugInfoKinds
    {
        public static readonly Guid AsyncMethodSteppingInformationBlob = new Guid("54FD2AC5-E925-401A-9C2A-F94F171072F8");

        public static readonly Guid StateMachineHoistedLocalScopes = new Guid("6DA9A61E-F8C7-4874-BE62-68BC5630DF71");

        public static readonly Guid DynamicLocalVariables = new Guid("83C563C4-B4F3-47D5-B824-BA5441477EA8");

        public static readonly Guid TupleElementNames = new Guid("ED9FDF71-8879-4747-8ED3-FE5EDE3CE710");

        public static readonly Guid DefaultNamespace = new Guid("58b2eab6-209f-4e4e-a22c-b2d0f910c782");

        public static readonly Guid EncLocalSlotMap = new Guid("755F52A8-91C5-45BE-B4B8-209571E552BD");

        public static readonly Guid EncLambdaAndClosureMap = new Guid("A643004C-0240-496F-A783-30D64F4979DE");

        public static readonly Guid SourceLink = new Guid("CC110556-A091-4D38-9FEC-25AB9A351A6A");

        public static readonly Guid EmbeddedSource = new Guid("0E8A571B-6926-466E-B4AD-8AB04611F5FE");

        public static readonly Guid CompilationMetadataReferences = new Guid("7E4D4708-096E-4C5C-AEDA-CB10BA6A740D");

        public static readonly Guid CompilationOptions = new Guid("B5FEEC05-8CD0-4A83-96DA-466284BB4BD8");
    }

    internal static class HashKinds
    {
        public static readonly Guid SHA1 = new Guid("ff1816ec-aa5e-4d10-87f7-6f4963833460");
        public static readonly Guid SHA256 = new Guid("8829d00f-11b8-4213-878b-770e8597ac16");
    }

    internal class BreakpointRequest
    {
        public string Id { get; private set; }
        public string Assembly { get; private set; }
        public string File { get; private set; }
        public int Line { get; private set; }
        public int Column { get; private set; }
        public string Condition { get; private set; }
        public MethodInfo Method { get; set; }

        private JObject request;

        public bool IsResolved => Assembly != null;
        public List<Breakpoint> Locations { get; set; } = new List<Breakpoint>();

        public override string ToString() => $"BreakpointRequest Assembly: {Assembly} File: {File} Line: {Line} Column: {Column}";

        public object AsSetBreakpointByUrlResponse(IEnumerable<object> jsloc) => new { breakpointId = Id, locations = Locations.Select(l => l.Location.AsLocation()).Concat(jsloc) };

        public BreakpointRequest()
        { }

        public BreakpointRequest(string id, MethodInfo method)
        {
            Id = id;
            Method = method;
        }

        public BreakpointRequest(string id, JObject request)
        {
            Id = id;
            this.request = request;
            Condition = request?["condition"]?.Value<string>();
        }

        public static BreakpointRequest Parse(string id, JObject args)
        {
            return new BreakpointRequest(id, args);
        }

        public BreakpointRequest Clone() => new BreakpointRequest { Id = Id, request = request };

        public bool IsMatch(SourceFile sourceFile)
        {
            string url = request?["url"]?.Value<string>();
            if (url == null)
            {
                string urlRegex = request?["urlRegex"].Value<string>();
                var regex = new Regex(urlRegex);
                return regex.IsMatch(sourceFile.Url.ToString()) || regex.IsMatch(sourceFile.DocUrl);
            }

            return sourceFile.Url.ToString() == url || sourceFile.DotNetUrl == url;
        }

        public bool TryResolve(SourceFile sourceFile)
        {
            if (!IsMatch(sourceFile))
                return false;

            int? line = request?["lineNumber"]?.Value<int>();
            int? column = request?["columnNumber"]?.Value<int>();

            if (line == null || column == null)
                return false;

            Assembly = sourceFile.AssemblyName;
            File = sourceFile.DebuggerFileName;
            Line = line.Value;
            Column = column.Value;
            return true;
        }

        public bool TryResolve(DebugStore store)
        {
            if (request == null || store == null)
                return false;

            return store.AllSources().FirstOrDefault(source => TryResolve(source)) != null;
        }

    }

    internal class VarInfo
    {
        public VarInfo(LocalVariable v, MetadataReader pdbReader)
        {
            this.Name = pdbReader.GetString(v.Name);
            this.Index = v.Index;
        }

        public VarInfo(Parameter p, MetadataReader pdbReader)
        {
            this.Name = pdbReader.GetString(p.Name);
            this.Index = (p.SequenceNumber) * -1;
        }

        public string Name { get; }
        public int Index { get; }

        public override string ToString() => $"(var-info [{Index}] '{Name}')";
    }

    internal class IlLocation
    {
        public IlLocation(MethodInfo method, int offset)
        {
            Method = method;
            Offset = offset;
        }

        public MethodInfo Method { get; }
        public int Offset { get; }
    }

    internal class SourceLocation
    {
        private SourceId id;
        private int line;
        private int column;
        private IlLocation ilLocation;

        public SourceLocation(SourceId id, int line, int column)
        {
            this.id = id;
            this.line = line;
            this.column = column;
        }

        public SourceLocation(MethodInfo mi, SequencePoint sp)
        {
            this.id = mi.SourceId;
            this.line = sp.StartLine - 1;
            this.column = sp.StartColumn - 1;
            this.ilLocation = new IlLocation(mi, sp.Offset);
        }

        public SourceId Id { get => id; }
        public int Line { get => line; }
        public int Column { get => column; }
        public IlLocation IlLocation => this.ilLocation;

        public override string ToString() => $"{id}:{Line}:{Column}";

        public static SourceLocation Parse(JObject obj)
        {
            if (obj == null)
                return null;

            if (!SourceId.TryParse(obj["scriptId"]?.Value<string>(), out SourceId id))
                return null;

            int? line = obj["lineNumber"]?.Value<int>();
            int? column = obj["columnNumber"]?.Value<int>();
            if (id == null || line == null || column == null)
                return null;

            return new SourceLocation(id, line.Value, column.Value);
        }

        internal class LocationComparer : EqualityComparer<SourceLocation>
        {
            public override bool Equals(SourceLocation l1, SourceLocation l2)
            {
                if (l1 == null && l2 == null)
                    return true;
                else if (l1 == null || l2 == null)
                    return false;

                return (l1.Line == l2.Line &&
                    l1.Column == l2.Column &&
                    l1.Id == l2.Id);
            }

            public override int GetHashCode(SourceLocation loc)
            {
                int hCode = loc.Line ^ loc.Column;
                return loc.Id.GetHashCode() ^ hCode.GetHashCode();
            }
        }

        internal object AsLocation() => new
        {
            scriptId = id.ToString(),
            lineNumber = line,
            columnNumber = column
        };
    }

    internal class SourceId
    {
        private const string Scheme = "dotnet://";

        private readonly int assembly, document;

        public int Assembly => assembly;
        public int Document => document;

        internal SourceId(int assembly, int document)
        {
            this.assembly = assembly;
            this.document = document;
        }

        public SourceId(string id)
        {
            if (!TryParse(id, out assembly, out document))
                throw new ArgumentException("invalid source identifier", nameof(id));
        }

        public static bool TryParse(string id, out SourceId source)
        {
            source = null;
            if (!TryParse(id, out int assembly, out int document))
                return false;

            source = new SourceId(assembly, document);
            return true;
        }

        private static bool TryParse(string id, out int assembly, out int document)
        {
            assembly = document = 0;
            if (id == null || !id.StartsWith(Scheme, StringComparison.Ordinal))
                return false;

            string[] sp = id.Substring(Scheme.Length).Split('_');
            if (sp.Length != 2)
                return false;

            if (!int.TryParse(sp[0], out assembly))
                return false;

            if (!int.TryParse(sp[1], out document))
                return false;

            return true;
        }

        public override string ToString() => $"{Scheme}{assembly}_{document}";

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            SourceId that = obj as SourceId;
            return that.assembly == this.assembly && that.document == this.document;
        }

        public override int GetHashCode() => assembly.GetHashCode() ^ document.GetHashCode();

        public static bool operator ==(SourceId a, SourceId b) => a is null ? b is null : a.Equals(b);

        public static bool operator !=(SourceId a, SourceId b) => !a.Equals(b);
    }

    internal class MethodInfo
    {
        private MethodDefinition methodDef;
        private SourceFile source;

        public SourceId SourceId => source.SourceId;

        public string Name { get; }
        public MethodDebugInformation DebugInformation;
        public MethodDefinitionHandle methodDefHandle;
        private MetadataReader pdbMetadataReader;

        public SourceLocation StartLocation { get; set; }
        public SourceLocation EndLocation { get; set; }
        public AssemblyInfo Assembly { get; }
        public int Token { get; }
        internal bool IsEnCMethod;
        internal LocalScopeHandleCollection localScopes;
        public bool IsStatic() => (methodDef.Attributes & MethodAttributes.Static) != 0;
        public int IsAsync { get; set; }
        public bool IsHiddenFromDebugger { get; }
        public TypeInfo TypeInfo { get; }

        public MethodInfo(AssemblyInfo assembly, MethodDefinitionHandle methodDefHandle, int token, SourceFile source, TypeInfo type, MetadataReader asmMetadataReader, MetadataReader pdbMetadataReader)
        {
            this.IsAsync = -1;
            this.Assembly = assembly;
            this.methodDef = asmMetadataReader.GetMethodDefinition(methodDefHandle);
            this.DebugInformation = pdbMetadataReader.GetMethodDebugInformation(methodDefHandle.ToDebugInformationHandle());
            this.source = source;
            this.Token = token;
            this.methodDefHandle = methodDefHandle;
            this.Name = asmMetadataReader.GetString(methodDef.Name);
            this.pdbMetadataReader = pdbMetadataReader;
            this.IsEnCMethod = false;
            this.TypeInfo = type;
            if (!DebugInformation.SequencePointsBlob.IsNil)
            {
                var sps = DebugInformation.GetSequencePoints();
                SequencePoint start = sps.First();
                SequencePoint end = sps.First();

                foreach (SequencePoint sp in sps)
                {
                    if (sp.StartLine < start.StartLine)
                        start = sp;
                    else if (sp.StartLine == start.StartLine && sp.StartColumn < start.StartColumn)
                        start = sp;

                    if (sp.EndLine > end.EndLine)
                        end = sp;
                    else if (sp.EndLine == end.EndLine && sp.EndColumn > end.EndColumn)
                        end = sp;
                }

                StartLocation = new SourceLocation(this, start);
                EndLocation = new SourceLocation(this, end);

                foreach (var cattr in methodDef.GetCustomAttributes())
                {
                    var ctorHandle = asmMetadataReader.GetCustomAttribute(cattr).Constructor;
                    if (ctorHandle.Kind == HandleKind.MemberReference)
                    {
                        var container = asmMetadataReader.GetMemberReference((MemberReferenceHandle)ctorHandle).Parent;
                        var name = asmMetadataReader.GetString(asmMetadataReader.GetTypeReference((TypeReferenceHandle)container).Name);
                        if (name == "DebuggerHiddenAttribute")
                        {
                            this.IsHiddenFromDebugger = true;
                            break;
                        }

                    }
                }
            }
            localScopes = pdbMetadataReader.GetLocalScopes(methodDefHandle);
        }

        public void UpdateEnC(MetadataReader asmMetadataReader, MetadataReader pdbMetadataReaderParm, int method_idx)
        {
            this.DebugInformation = pdbMetadataReaderParm.GetMethodDebugInformation(MetadataTokens.MethodDebugInformationHandle(method_idx));
            this.pdbMetadataReader = pdbMetadataReaderParm;
            this.IsEnCMethod = true;
            if (!DebugInformation.SequencePointsBlob.IsNil)
            {
                var sps = DebugInformation.GetSequencePoints();
                SequencePoint start = sps.First();
                SequencePoint end = sps.First();

                foreach (SequencePoint sp in sps)
                {
                    if (sp.StartLine < start.StartLine)
                        start = sp;
                    else if (sp.StartLine == start.StartLine && sp.StartColumn < start.StartColumn)
                        start = sp;

                    if (sp.EndLine > end.EndLine)
                        end = sp;
                    else if (sp.EndLine == end.EndLine && sp.EndColumn > end.EndColumn)
                        end = sp;
                }

                StartLocation = new SourceLocation(this, start);
                EndLocation = new SourceLocation(this, end);
            }
            localScopes = pdbMetadataReader.GetLocalScopes(MetadataTokens.MethodDefinitionHandle(method_idx));
        }

        public SourceLocation GetLocationByIl(int pos)
        {
            SequencePoint? prev = null;
            if (!DebugInformation.SequencePointsBlob.IsNil) {
                foreach (SequencePoint sp in DebugInformation.GetSequencePoints())
                {
                    if (sp.Offset > pos)
                    {
                        //get the earlier line number if the offset is in a hidden sequence point and has a earlier line number available
                        // if is doesn't continue and get the next line number that is not in a hidden sequence point
                        if (sp.IsHidden && prev == null)
                            continue;
                        break;
                    }

                    if (!sp.IsHidden)
                        prev = sp;
                }

                if (prev.HasValue)
                    return new SourceLocation(this, prev.Value);
            }
            return null;
        }

        public VarInfo[] GetLiveVarsAt(int offset)
        {
            var res = new List<VarInfo>();
            foreach (var parameterHandle in methodDef.GetParameters())
            {
                var parameter = Assembly.asmMetadataReader.GetParameter(parameterHandle);
                res.Add(new VarInfo(parameter, Assembly.asmMetadataReader));
            }


            foreach (var localScopeHandle in localScopes)
            {
                var localScope = pdbMetadataReader.GetLocalScope(localScopeHandle);
                if (localScope.StartOffset <= offset && localScope.EndOffset > offset)
                {
                    var localVariables = localScope.GetLocalVariables();
                    foreach (var localVariableHandle in localVariables)
                    {
                        var localVariable = pdbMetadataReader.GetLocalVariable(localVariableHandle);
                        if (localVariable.Attributes != LocalVariableAttributes.DebuggerHidden)
                            res.Add(new VarInfo(localVariable, pdbMetadataReader));
                    }
                }
            }
            return res.ToArray();
        }

        public override string ToString() => "MethodInfo(" + Name + ")";
    }

    internal class TypeInfo
    {
        internal AssemblyInfo assembly;
        private TypeDefinition type;
        private List<MethodInfo> methods;
        internal int Token { get; }
        internal string Namespace { get; }

        public TypeInfo(AssemblyInfo assembly, TypeDefinitionHandle typeHandle, TypeDefinition type)
        {
            this.assembly = assembly;
            var metadataReader = assembly.asmMetadataReader;
            Token = MetadataTokens.GetToken(metadataReader, typeHandle);
            this.type = type;
            methods = new List<MethodInfo>();
            Name = metadataReader.GetString(type.Name);
            var declaringType = type;
            while (declaringType.IsNested)
            {
                declaringType = metadataReader.GetTypeDefinition(declaringType.GetDeclaringType());
                Name = metadataReader.GetString(declaringType.Name) + "." + Name;
            }
            Namespace = metadataReader.GetString(declaringType.Namespace);
            if (Namespace.Length > 0)
                FullName = Namespace + "." + Name;
            else
                FullName = Name;
        }

        public TypeInfo(AssemblyInfo assembly, string name)
        {
            Name = name;
            FullName = name;
        }

        public string Name { get; }
        public string FullName { get; }
        public List<MethodInfo> Methods => methods;

        public override string ToString() => "TypeInfo('" + FullName + "')";
    }


    internal class AssemblyInfo
    {
        private static int next_id;
        private readonly int id;
        private readonly ILogger logger;
        private Dictionary<int, MethodInfo> methods = new Dictionary<int, MethodInfo>();
        private Dictionary<string, string> sourceLinkMappings = new Dictionary<string, string>();
        private readonly List<SourceFile> sources = new List<SourceFile>();
        internal string Url { get; }
        internal MetadataReader asmMetadataReader { get; }
        internal MetadataReader pdbMetadataReader { get; set; }
        internal List<MemoryStream> enCMemoryStream  = new List<MemoryStream>();
        internal List<MetadataReader> enCMetadataReader  = new List<MetadataReader>();
        internal PEReader peReader;
        internal MemoryStream asmStream;
        internal MemoryStream pdbStream;
        public int DebugId { get; set; }

        public bool TriedToLoadSymbolsOnDemand { get; set; }

        public unsafe AssemblyInfo(string url, byte[] assembly, byte[] pdb)
        {
            this.id = Interlocked.Increment(ref next_id);
            asmStream = new MemoryStream(assembly);
            peReader = new PEReader(asmStream);
            asmMetadataReader = PEReaderExtensions.GetMetadataReader(peReader);
            if (pdb != null)
            {
                pdbStream = new MemoryStream(pdb);
                pdbMetadataReader = MetadataReaderProvider.FromPortablePdbStream(pdbStream).GetMetadataReader();
            }
            else
            {
                var entries = peReader.ReadDebugDirectory();
                var embeddedPdbEntry = entries.FirstOrDefault(e => e.Type == DebugDirectoryEntryType.EmbeddedPortablePdb);
                if (embeddedPdbEntry.DataSize != 0)
                {
                    pdbMetadataReader = peReader.ReadEmbeddedPortablePdbDebugDirectoryData(embeddedPdbEntry).GetMetadataReader();
                }
            }
            Name = asmMetadataReader.GetAssemblyDefinition().GetAssemblyName().Name + ".dll";
            AssemblyNameUnqualified = asmMetadataReader.GetAssemblyDefinition().GetAssemblyName().Name + ".dll";
            Populate();
        }

        public bool EnC(byte[] meta, byte[] pdb)
        {
            var asmStream = new MemoryStream(meta);
            MetadataReader asmMetadataReader = MetadataReaderProvider.FromMetadataStream(asmStream).GetMetadataReader();
            var pdbStream = new MemoryStream(pdb);
            MetadataReader pdbMetadataReader = MetadataReaderProvider.FromPortablePdbStream(pdbStream).GetMetadataReader();
            enCMemoryStream.Add(asmStream);
            enCMemoryStream.Add(pdbStream);
            enCMetadataReader.Add(asmMetadataReader);
            enCMetadataReader.Add(pdbMetadataReader);
            PopulateEnC(asmMetadataReader, pdbMetadataReader);
            return true;
        }

        public AssemblyInfo(ILogger logger)
        {
            this.logger = logger;
        }

        private void PopulateEnC(MetadataReader asmMetadataReaderParm, MetadataReader pdbMetadataReaderParm)
        {
            int i = 1;
            foreach (EntityHandle encMapHandle in asmMetadataReaderParm.GetEditAndContinueMapEntries())
            {
                if (encMapHandle.Kind == HandleKind.MethodDebugInformation)
                {
                    var method = methods[asmMetadataReader.GetRowNumber(encMapHandle)];
                    method.UpdateEnC(asmMetadataReaderParm, pdbMetadataReaderParm, i);
                    i++;
                }
            }
        }

        private void Populate()
        {
            var d2s = new Dictionary<int, SourceFile>();

            SourceFile FindSource(DocumentHandle doc, int rowid, string documentName)
            {
                if (d2s.TryGetValue(rowid, out SourceFile source))
                    return source;

                var src = new SourceFile(this, sources.Count, doc, GetSourceLinkUrl(documentName), documentName);
                sources.Add(src);
                d2s[rowid] = src;
                return src;
            };

            foreach (DocumentHandle dh in asmMetadataReader.Documents)
            {
                var document = asmMetadataReader.GetDocument(dh);
            }

            if (pdbMetadataReader != null)
                ProcessSourceLink();

            foreach (TypeDefinitionHandle type in asmMetadataReader.TypeDefinitions)
            {
                var typeDefinition = asmMetadataReader.GetTypeDefinition(type);

                var typeInfo = new TypeInfo(this, type, typeDefinition);
                TypesByName[typeInfo.FullName] = typeInfo;
                TypesByToken[typeInfo.Token] = typeInfo;
                if (pdbMetadataReader != null)
                {
                    foreach (MethodDefinitionHandle method in typeDefinition.GetMethods())
                    {
                        var methodDefinition = asmMetadataReader.GetMethodDefinition(method);
                        if (!method.ToDebugInformationHandle().IsNil)
                        {
                            var methodDebugInformation = pdbMetadataReader.GetMethodDebugInformation(method.ToDebugInformationHandle());
                            if (!methodDebugInformation.Document.IsNil)
                            {
                                var document = pdbMetadataReader.GetDocument(methodDebugInformation.Document);
                                var documentName = pdbMetadataReader.GetString(document.Name);
                                SourceFile source = FindSource(methodDebugInformation.Document, asmMetadataReader.GetRowNumber(methodDebugInformation.Document), documentName);
                                var methodInfo = new MethodInfo(this, method, asmMetadataReader.GetRowNumber(method), source, typeInfo, asmMetadataReader, pdbMetadataReader);
                                methods[asmMetadataReader.GetRowNumber(method)] = methodInfo;

                                if (source != null)
                                    source.AddMethod(methodInfo);

                                typeInfo.Methods.Add(methodInfo);
                            }
                        }
                    }
                }
            }

        }

        private void ProcessSourceLink()
        {
            var sourceLinkDebugInfo =
                    (from cdiHandle in pdbMetadataReader.GetCustomDebugInformation(EntityHandle.ModuleDefinition)
                     let cdi = pdbMetadataReader.GetCustomDebugInformation(cdiHandle)
                     where pdbMetadataReader.GetGuid(cdi.Kind) == PortableCustomDebugInfoKinds.SourceLink
                     select pdbMetadataReader.GetBlobBytes(cdi.Value)).SingleOrDefault();

            if (sourceLinkDebugInfo != null)
            {
                var sourceLinkContent = System.Text.Encoding.UTF8.GetString(sourceLinkDebugInfo, 0, sourceLinkDebugInfo.Length);

                if (sourceLinkContent != null)
                {
                    JToken jObject = JObject.Parse(sourceLinkContent)["documents"];
                    sourceLinkMappings = JsonConvert.DeserializeObject<Dictionary<string, string>>(jObject.ToString());
                }
            }
        }

        private Uri GetSourceLinkUrl(string document)
        {
            if (sourceLinkMappings.TryGetValue(document, out string url))
                return new Uri(url);

            foreach (KeyValuePair<string, string> sourceLinkDocument in sourceLinkMappings)
            {
                string key = sourceLinkDocument.Key;

                if (!key.EndsWith("*"))
                {
                    continue;
                }

                string keyTrim = key.TrimEnd('*');

                if (document.StartsWith(keyTrim, StringComparison.OrdinalIgnoreCase))
                {
                    string docUrlPart = document.Replace(keyTrim, "");
                    return new Uri(sourceLinkDocument.Value.TrimEnd('*') + docUrlPart);
                }
            }

            return null;
        }

        public IEnumerable<SourceFile> Sources => this.sources;
        public Dictionary<int, MethodInfo> Methods => this.methods;

        public Dictionary<string, TypeInfo> TypesByName { get; } = new();
        public Dictionary<int, TypeInfo> TypesByToken { get; } = new();
        public int Id => id;
        public string Name { get; }
        public bool HasSymbols => pdbMetadataReader != null;

        // "System.Threading", instead of "System.Threading, Version=5.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"
        public string AssemblyNameUnqualified { get; }

        public SourceFile GetDocById(int document)
        {
            return sources.FirstOrDefault(s => s.SourceId.Document == document);
        }

        public MethodInfo GetMethodByToken(int token)
        {
            methods.TryGetValue(token, out MethodInfo value);
            return value;
        }

        public TypeInfo GetTypeByName(string name)
        {
            TypesByName.TryGetValue(name, out TypeInfo res);
            return res;
        }

        internal void UpdatePdbInformation(Stream streamToReadFrom)
        {
            pdbStream = new MemoryStream();
            streamToReadFrom.CopyTo(pdbStream);
            pdbMetadataReader = MetadataReaderProvider.FromPortablePdbStream(pdbStream).GetMetadataReader();
        }
    }
    internal class SourceFile
    {
        private Dictionary<int, MethodInfo> methods;
        private AssemblyInfo assembly;
        private int id;
        private Document doc;
        private DocumentHandle docHandle;
        private string url;

        internal SourceFile(AssemblyInfo assembly, int id, DocumentHandle docHandle, Uri sourceLinkUri, string url)
        {
            this.methods = new Dictionary<int, MethodInfo>();
            this.SourceLinkUri = sourceLinkUri;
            this.assembly = assembly;
            this.id = id;
            this.doc = assembly.pdbMetadataReader.GetDocument(docHandle);
            this.docHandle = docHandle;
            this.url = url;
            this.DebuggerFileName = url.Replace("\\", "/").Replace(":", "");

            this.SourceUri = new Uri((Path.IsPathRooted(url) ? "file://" : "") + url, UriKind.RelativeOrAbsolute);
            if (SourceUri.IsFile && File.Exists(SourceUri.LocalPath))
            {
                this.Url = this.SourceUri.ToString();
            }
            else
            {
                this.Url = DotNetUrl;
            }
        }

        internal void AddMethod(MethodInfo mi)
        {
            if (!this.methods.ContainsKey(mi.Token))
            {
                this.methods[mi.Token] = mi;
            }
        }

        public string DebuggerFileName { get; }
        public string Url { get; }
        public string AssemblyName => assembly.Name;
        public string DotNetUrl => $"dotnet://{assembly.Name}/{DebuggerFileName}";

        public SourceId SourceId => new SourceId(assembly.Id, this.id);
        public Uri SourceLinkUri { get; }
        public Uri SourceUri { get; }

        public IEnumerable<MethodInfo> Methods => this.methods.Values;

        public string DocUrl => url;

        public (int startLine, int startColumn, int endLine, int endColumn) GetExtents()
        {
            MethodInfo start = Methods.OrderBy(m => m.StartLocation.Line).ThenBy(m => m.StartLocation.Column).First();
            MethodInfo end = Methods.OrderByDescending(m => m.EndLocation.Line).ThenByDescending(m => m.EndLocation.Column).First();
            return (start.StartLocation.Line, start.StartLocation.Column, end.EndLocation.Line, end.EndLocation.Column);
        }

        private async Task<MemoryStream> GetDataAsync(Uri uri, CancellationToken token)
        {
            var mem = new MemoryStream();
            try
            {
                if (uri.IsFile && File.Exists(uri.LocalPath))
                {
                    using (FileStream file = File.Open(SourceUri.LocalPath, FileMode.Open))
                    {
                        await file.CopyToAsync(mem, token).ConfigureAwait(false);
                        mem.Position = 0;
                    }
                }
                else if (uri.Scheme == "http" || uri.Scheme == "https")
                {
                    using (var client = new HttpClient())
                    using (Stream stream = await client.GetStreamAsync(uri, token))
                    {
                        await stream.CopyToAsync(mem, token).ConfigureAwait(false);
                        mem.Position = 0;
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
            return mem;
        }

        private static HashAlgorithm GetHashAlgorithm(Guid algorithm)
        {
            if (algorithm.Equals(HashKinds.SHA1))
#pragma warning disable CA5350 // Do Not Use Weak Cryptographic Algorithms
                return SHA1.Create();
#pragma warning restore CA5350 // Do Not Use Weak Cryptographic Algorithms
            if (algorithm.Equals(HashKinds.SHA256))
                return SHA256.Create();
            return null;
        }

        private bool CheckPdbHash(byte[] computedHash)
        {
            var hash = assembly.pdbMetadataReader.GetBlobBytes(doc.Hash);
            if (computedHash.Length != hash.Length)
                return false;

            for (int i = 0; i < computedHash.Length; i++)
                if (computedHash[i] != hash[i])
                    return false;

            return true;
        }

        private byte[] ComputePdbHash(Stream sourceStream)
        {
            HashAlgorithm algorithm = GetHashAlgorithm(assembly.pdbMetadataReader.GetGuid(doc.HashAlgorithm));
            if (algorithm != null)
                using (algorithm)
                    return algorithm.ComputeHash(sourceStream);
            return Array.Empty<byte>();
        }

        public async Task<Stream> GetSourceAsync(bool checkHash, CancellationToken token = default(CancellationToken))
        {
            var reader = assembly.pdbMetadataReader;
            byte[] bytes = (from handle in reader.GetCustomDebugInformation(docHandle)
                            let cdi = reader.GetCustomDebugInformation(handle)
                            where reader.GetGuid(cdi.Kind) == PortableCustomDebugInfoKinds.EmbeddedSource
                            select reader.GetBlobBytes(cdi.Value)).SingleOrDefault();

            if (bytes != null)
            {
                int uncompressedSize = BitConverter.ToInt32(bytes, 0);
                var stream = new MemoryStream(bytes, sizeof(int), bytes.Length - sizeof(int));

                if (uncompressedSize != 0)
                {
                    return new DeflateStream(stream, CompressionMode.Decompress);
                }
            }


            foreach (Uri url in new[] { SourceUri, SourceLinkUri })
            {
                MemoryStream mem = await GetDataAsync(url, token).ConfigureAwait(false);
                if (mem != null && mem.Length > 0 && (!checkHash || CheckPdbHash(ComputePdbHash(mem))))
                {
                    mem.Position = 0;
                    return mem;
                }
            }

            return MemoryStream.Null;
        }

        public object ToScriptSource(int executionContextId, object executionContextAuxData)
        {
            return new
            {
                scriptId = SourceId.ToString(),
                url = Url,
                executionContextId,
                executionContextAuxData,
                //hash:  should be the v8 hash algo, managed implementation is pending
                dotNetUrl = DotNetUrl,
            };
        }
    }

    internal class DebugStore
    {
        internal List<AssemblyInfo> assemblies = new List<AssemblyInfo>();
        private readonly HttpClient client;
        private readonly ILogger logger;

        public DebugStore(ILogger logger, HttpClient client)
        {
            this.client = client;
            this.logger = logger;
        }

        public DebugStore(ILogger logger) : this(logger, new HttpClient())
        { }

        private class DebugItem
        {
            public string Url { get; set; }
            public Task<byte[][]> Data { get; set; }
        }

        public IEnumerable<MethodInfo> EnC(AssemblyInfo asm, byte[] meta_data, byte[] pdb_data)
        {
            asm.EnC(meta_data, pdb_data);
            foreach (var method in asm.Methods)
            {
                if (method.Value.IsEnCMethod)
                    yield return method.Value;
            }
        }

        public IEnumerable<SourceFile> Add(string name, byte[] assembly_data, byte[] pdb_data)
        {
            AssemblyInfo assembly = null;
            try
            {
                assembly = new AssemblyInfo(name, assembly_data, pdb_data);
            }
            catch (Exception e)
            {
                logger.LogDebug($"Failed to load assembly: ({e.Message})");
                yield break;
            }

            if (assembly == null)
                yield break;

            if (GetAssemblyByUnqualifiedName(assembly.AssemblyNameUnqualified) != null)
            {
                logger.LogDebug($"Skipping adding {assembly.Name} into the debug store, as it already exists");
                yield break;
            }

            assemblies.Add(assembly);
            foreach (var source in assembly.Sources)
            {
                yield return source;
            }
        }

        public async IAsyncEnumerable<SourceFile> Load(string[] loaded_files, [EnumeratorCancellation] CancellationToken token)
        {
            var asm_files = new List<string>();
            var pdb_files = new List<string>();
            foreach (string file_name in loaded_files)
            {
                if (file_name.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase))
                    pdb_files.Add(file_name);
                else
                    asm_files.Add(file_name);
            }

            List<DebugItem> steps = new List<DebugItem>();
            foreach (string url in asm_files)
            {
                try
                {
                    string candidate_pdb = Path.ChangeExtension(url, "pdb");
                    string pdb = pdb_files.FirstOrDefault(n => n == candidate_pdb);

                    steps.Add(
                        new DebugItem
                        {
                            Url = url,
                            Data = Task.WhenAll(client.GetByteArrayAsync(url, token), pdb != null ? client.GetByteArrayAsync(pdb, token) : Task.FromResult<byte[]>(null))
                        });
                }
                catch (Exception e)
                {
                    logger.LogDebug($"Failed to read {url} ({e.Message})");
                }
            }

            foreach (DebugItem step in steps)
            {
                AssemblyInfo assembly = null;
                try
                {
                    byte[][] bytes = await step.Data.ConfigureAwait(false);
                    assembly = new AssemblyInfo(step.Url, bytes[0], bytes[1]);
                }
                catch (Exception e)
                {
                    logger.LogDebug($"Failed to load {step.Url} ({e.Message})");
                }
                if (assembly == null)
                    continue;

                if (GetAssemblyByUnqualifiedName(assembly.AssemblyNameUnqualified) != null)
                {
                    logger.LogDebug($"Skipping loading {assembly.Name} into the debug store, as it already exists");
                    continue;
                }

                assemblies.Add(assembly);
                foreach (SourceFile source in assembly.Sources)
                    yield return source;
            }
        }

        public IEnumerable<SourceFile> AllSources() => assemblies.SelectMany(a => a.Sources);

        public SourceFile GetFileById(SourceId id) => AllSources().SingleOrDefault(f => f.SourceId.Equals(id));

        public AssemblyInfo GetAssemblyByName(string name) => assemblies.FirstOrDefault(a => a.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));

        public AssemblyInfo GetAssemblyByUnqualifiedName(string name) => assemblies.FirstOrDefault(a => a.AssemblyNameUnqualified.Equals(name, StringComparison.InvariantCultureIgnoreCase));

        /*
        V8 uses zero based indexing for both line and column.
        PPDBs uses one based indexing for both line and column.
        */
        private static bool Match(SequencePoint sp, SourceLocation start, SourceLocation end)
        {
            (int Line, int Column) spStart = (Line: sp.StartLine - 1, Column: sp.StartColumn - 1);
            (int Line, int Column) spEnd = (Line: sp.EndLine - 1, Column: sp.EndColumn - 1);

            if (start.Line > spEnd.Line)
                return false;

            if (start.Column > spEnd.Column && start.Line == spEnd.Line)
                return false;

            if (end.Line < spStart.Line)
                return false;

            if (end.Column < spStart.Column && end.Line == spStart.Line)
                return false;

            return true;
        }

        public List<SourceLocation> FindPossibleBreakpoints(SourceLocation start, SourceLocation end)
        {
            //XXX FIXME no idea what todo with locations on different files
            if (start.Id != end.Id)
            {
                logger.LogDebug($"FindPossibleBreakpoints: documents differ (start: {start.Id}) (end {end.Id}");
                return null;
            }

            SourceId sourceId = start.Id;

            SourceFile doc = GetFileById(sourceId);

            var res = new List<SourceLocation>();
            if (doc == null)
            {
                logger.LogDebug($"Could not find document {sourceId}");
                return res;
            }

            foreach (MethodInfo method in doc.Methods)
            {
                if (!method.DebugInformation.SequencePointsBlob.IsNil)
                {
                    foreach (SequencePoint sequencePoint in method.DebugInformation.GetSequencePoints())
                    {
                        if (!sequencePoint.IsHidden && Match(sequencePoint, start, end))
                            res.Add(new SourceLocation(method, sequencePoint));
                    }
                }
            }
            return res;
        }

        /*
        V8 uses zero based indexing for both line and column.
        PPDBs uses one based indexing for both line and column.
        */
        private static bool Match(SequencePoint sp, int line, int column)
        {
            (int line, int column) bp = (line: line + 1, column: column + 1);

            if (sp.StartLine > bp.line || sp.EndLine < bp.line)
                return false;

            //Chrome sends a zero column even if getPossibleBreakpoints say something else
            if (column == 0)
                return true;

            if (sp.StartColumn > bp.column && sp.StartLine == bp.line)
                return false;

            if (sp.EndColumn < bp.column && sp.EndLine == bp.line)
                return false;

            return true;
        }

        public IEnumerable<SourceLocation> FindBreakpointLocations(BreakpointRequest request)
        {
            request.TryResolve(this);

            AssemblyInfo asm = assemblies.FirstOrDefault(a => a.Name.Equals(request.Assembly, StringComparison.OrdinalIgnoreCase));
            SourceFile sourceFile = asm?.Sources?.SingleOrDefault(s => s.DebuggerFileName.Equals(request.File, StringComparison.OrdinalIgnoreCase));

            if (sourceFile == null)
                yield break;

            foreach (MethodInfo method in sourceFile.Methods)
            {
                if (!method.DebugInformation.SequencePointsBlob.IsNil)
                {
                    foreach (SequencePoint sequencePoint in method.DebugInformation.GetSequencePoints())
                    {
                        if (!sequencePoint.IsHidden && Match(sequencePoint, request.Line, request.Column))
                            yield return new SourceLocation(method, sequencePoint);
                    }
                }
            }
        }

        public string ToUrl(SourceLocation location) => location != null ? GetFileById(location.Id).Url : "";
    }
}
