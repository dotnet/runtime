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
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Pdb;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.WebAssembly.Diagnostics
{
    internal class BreakpointRequest
    {
        public string Id { get; private set; }
        public string Assembly { get; private set; }
        public string File { get; private set; }
        public int Line { get; private set; }
        public int Column { get; private set; }
        public MethodInfo Method { get; private set; }

        private JObject request;

        public bool IsResolved => Assembly != null;
        public List<Breakpoint> Locations { get; } = new List<Breakpoint>();

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
        public VarInfo(VariableDebugInformation v)
        {
            this.Name = v.Name;
            this.Index = v.Index;
        }

        public VarInfo(ParameterDefinition p)
        {
            this.Name = p.Name;
            this.Index = (p.Index + 1) * -1;
        }

        public string Name { get; }
        public int Index { get; }

        public override string ToString() => $"(var-info [{Index}] '{Name}')";
    }

    internal class CliLocation
    {
        public CliLocation(MethodInfo method, int offset)
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
        private CliLocation cliLoc;

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
            this.cliLoc = new CliLocation(mi, sp.Offset);
        }

        public SourceId Id { get => id; }
        public int Line { get => line; }
        public int Column { get => column; }
        public CliLocation CliLocation => this.cliLoc;

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

        public static bool operator ==(SourceId a, SourceId b) => ((object)a == null) ? (object)b == null : a.Equals(b);

        public static bool operator !=(SourceId a, SourceId b) => !a.Equals(b);
    }

    internal class MethodInfo
    {
        private MethodDefinition methodDef;
        private SourceFile source;

        public SourceId SourceId => source.SourceId;

        public string Name => methodDef.Name;
        public MethodDebugInformation DebugInformation => methodDef.DebugInformation;

        public SourceLocation StartLocation { get; }
        public SourceLocation EndLocation { get; }
        public AssemblyInfo Assembly { get; }
        public uint Token => methodDef.MetadataToken.RID;

        public MethodInfo(AssemblyInfo assembly, MethodDefinition methodDef, SourceFile source)
        {
            this.Assembly = assembly;
            this.methodDef = methodDef;
            this.source = source;

            Mono.Collections.Generic.Collection<SequencePoint> sps = DebugInformation.SequencePoints;
            if (sps == null || sps.Count < 1)
                return;

            SequencePoint start = sps[0];
            SequencePoint end = sps[0];

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

        public SourceLocation GetLocationByIl(int pos)
        {
            SequencePoint prev = null;
            foreach (SequencePoint sp in DebugInformation.SequencePoints)
            {
                if (sp.Offset > pos)
                    break;
                prev = sp;
            }

            if (prev != null)
                return new SourceLocation(this, prev);

            return null;
        }

        public VarInfo[] GetLiveVarsAt(int offset)
        {
            var res = new List<VarInfo>();

            res.AddRange(methodDef.Parameters.Select(p => new VarInfo(p)));
            res.AddRange(methodDef.DebugInformation.GetScopes()
                .Where(s => s.Start.Offset <= offset && (s.End.IsEndOfMethod || s.End.Offset > offset))
                .SelectMany(s => s.Variables)
                .Where(v => !v.IsDebuggerHidden)
                .Select(v => new VarInfo(v)));

            return res.ToArray();
        }

        public override string ToString() => "MethodInfo(" + methodDef.FullName + ")";
    }

    internal class TypeInfo
    {
        private AssemblyInfo assembly;
        private TypeDefinition type;
        private List<MethodInfo> methods;

        public TypeInfo(AssemblyInfo assembly, TypeDefinition type)
        {
            this.assembly = assembly;
            this.type = type;
            methods = new List<MethodInfo>();
        }

        public string Name => type.Name;
        public string FullName => type.FullName;
        public List<MethodInfo> Methods => methods;

        public override string ToString() => "TypeInfo('" + FullName + "')";
    }

    internal class AssemblyInfo
    {
        private static int next_id;
        private ModuleDefinition image;
        private readonly int id;
        private readonly ILogger logger;
        private Dictionary<uint, MethodInfo> methods = new Dictionary<uint, MethodInfo>();
        private Dictionary<string, string> sourceLinkMappings = new Dictionary<string, string>();
        private Dictionary<string, TypeInfo> typesByName = new Dictionary<string, TypeInfo>();
        private readonly List<SourceFile> sources = new List<SourceFile>();
        internal string Url { get; }
        public bool TriedToLoadSymbolsOnDemand { get; set; }

        public AssemblyInfo(IAssemblyResolver resolver, string url, byte[] assembly, byte[] pdb)
        {
            this.id = Interlocked.Increment(ref next_id);

            try
            {
                Url = url;
                ReaderParameters rp = new ReaderParameters( /*ReadingMode.Immediate*/ );
                rp.AssemblyResolver = resolver;
                // set ReadSymbols = true unconditionally in case there
                // is an embedded pdb then handle ArgumentException
                // and assume that if pdb == null that is the cause
                rp.ReadSymbols = true;
                rp.SymbolReaderProvider = new PdbReaderProvider();
                if (pdb != null)
                    rp.SymbolStream = new MemoryStream(pdb);
                rp.ReadingMode = ReadingMode.Immediate;

                this.image = ModuleDefinition.ReadModule(new MemoryStream(assembly), rp);
            }
            catch (BadImageFormatException ex)
            {
                logger.LogWarning($"Failed to read assembly as portable PDB: {ex.Message}");
            }
            catch (ArgumentException)
            {
                // if pdb == null this is expected and we
                // read the assembly without symbols below
                if (pdb != null)
                    throw;
            }

            if (this.image == null)
            {
                ReaderParameters rp = new ReaderParameters( /*ReadingMode.Immediate*/ );
                rp.AssemblyResolver = resolver;
                if (pdb != null)
                {
                    rp.ReadSymbols = true;
                    rp.SymbolReaderProvider = new PdbReaderProvider();
                    rp.SymbolStream = new MemoryStream(pdb);
                }

                rp.ReadingMode = ReadingMode.Immediate;

                this.image = ModuleDefinition.ReadModule(new MemoryStream(assembly), rp);
            }

            Populate();
        }

        public AssemblyInfo(ILogger logger)
        {
            this.logger = logger;
        }

        public ModuleDefinition Image => image;

        public void ClearDebugInfo()
        {
            foreach (TypeDefinition type in image.GetTypes())
            {
                var typeInfo = new TypeInfo(this, type);
                typesByName[type.FullName] = typeInfo;

                foreach (MethodDefinition method in type.Methods)
                {
                    method.DebugInformation = null;
                }
            }
        }

        public void Populate()
        {
            ProcessSourceLink();

            var d2s = new Dictionary<Document, SourceFile>();

            SourceFile FindSource(Document doc)
            {
                if (doc == null)
                    return null;

                if (d2s.TryGetValue(doc, out SourceFile source))
                    return source;

                var src = new SourceFile(this, sources.Count, doc, GetSourceLinkUrl(doc.Url));
                sources.Add(src);
                d2s[doc] = src;
                return src;
            };

            foreach (TypeDefinition type in image.GetTypes())
            {
                var typeInfo = new TypeInfo(this, type);
                typesByName[type.FullName] = typeInfo;

                foreach (MethodDefinition method in type.Methods)
                {
                    foreach (SequencePoint sp in method.DebugInformation.SequencePoints)
                    {
                        SourceFile source = FindSource(sp.Document);
                        var methodInfo = new MethodInfo(this, method, source);
                        methods[method.MetadataToken.RID] = methodInfo;
                        if (source != null)
                            source.AddMethod(methodInfo);

                        typeInfo.Methods.Add(methodInfo);
                    }
                }
            }
        }

        private void ProcessSourceLink()
        {
            CustomDebugInformation sourceLinkDebugInfo = image.CustomDebugInformations.FirstOrDefault(i => i.Kind == CustomDebugInformationKind.SourceLink);

            if (sourceLinkDebugInfo != null)
            {
                string sourceLinkContent = ((SourceLinkDebugInformation)sourceLinkDebugInfo).Content;

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

                if (Path.GetFileName(key) != "*")
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

        public Dictionary<string, TypeInfo> TypesByName => this.typesByName;
        public int Id => id;
        public string Name => image.Name;

        // "System.Threading", instead of "System.Threading, Version=5.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"
        public string AssemblyNameUnqualified => image.Assembly.Name.Name;

        public SourceFile GetDocById(int document)
        {
            return sources.FirstOrDefault(s => s.SourceId.Document == document);
        }

        public MethodInfo GetMethodByToken(uint token)
        {
            methods.TryGetValue(token, out MethodInfo value);
            return value;
        }

        public TypeInfo GetTypeByName(string name)
        {
            typesByName.TryGetValue(name, out TypeInfo res);
            return res;
        }
    }

    internal class SourceFile
    {
        private Dictionary<uint, MethodInfo> methods;
        private AssemblyInfo assembly;
        private int id;
        private Document doc;

        internal SourceFile(AssemblyInfo assembly, int id, Document doc, Uri sourceLinkUri)
        {
            this.methods = new Dictionary<uint, MethodInfo>();
            this.SourceLinkUri = sourceLinkUri;
            this.assembly = assembly;
            this.id = id;
            this.doc = doc;
            this.DebuggerFileName = doc.Url.Replace("\\", "/").Replace(":", "");

            this.SourceUri = new Uri((Path.IsPathRooted(doc.Url) ? "file://" : "") + doc.Url, UriKind.RelativeOrAbsolute);
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
                this.methods[mi.Token] = mi;
        }

        public string DebuggerFileName { get; }
        public string Url { get; }
        public string AssemblyName => assembly.Name;
        public string DotNetUrl => $"dotnet://{assembly.Name}/{DebuggerFileName}";

        public SourceId SourceId => new SourceId(assembly.Id, this.id);
        public Uri SourceLinkUri { get; }
        public Uri SourceUri { get; }

        public IEnumerable<MethodInfo> Methods => this.methods.Values;

        public string DocUrl => doc.Url;

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

        private static HashAlgorithm GetHashAlgorithm(DocumentHashAlgorithm algorithm)
        {
            switch (algorithm)
            {
                case DocumentHashAlgorithm.SHA1:
#pragma warning disable CA5350 // Do Not Use Weak Cryptographic Algorithms
                    return SHA1.Create();
#pragma warning restore CA5350 // Do Not Use Weak Cryptographic Algorithms
                case DocumentHashAlgorithm.SHA256:
                    return SHA256.Create();
                case DocumentHashAlgorithm.MD5:
#pragma warning disable CA5351 // Do Not Use Broken Cryptographic Algorithms
                    return MD5.Create();
#pragma warning restore CA5351 // Do Not Use Broken Cryptographic Algorithms
            }
            return null;
        }

        private bool CheckPdbHash(byte[] computedHash)
        {
            if (computedHash.Length != doc.Hash.Length)
                return false;

            for (int i = 0; i < computedHash.Length; i++)
                if (computedHash[i] != doc.Hash[i])
                    return false;

            return true;
        }

        private byte[] ComputePdbHash(Stream sourceStream)
        {
            HashAlgorithm algorithm = GetHashAlgorithm(doc.HashAlgorithm);
            if (algorithm != null)
                using (algorithm)
                    return algorithm.ComputeHash(sourceStream);

            return Array.Empty<byte>();
        }

        public async Task<Stream> GetSourceAsync(bool checkHash, CancellationToken token = default(CancellationToken))
        {
            if (doc.EmbeddedSource.Length > 0)
                return new MemoryStream(doc.EmbeddedSource, false);

            foreach (Uri url in new[] { SourceUri, SourceLinkUri })
            {
                MemoryStream mem = await GetDataAsync(url, token).ConfigureAwait(false);
                if (mem != null && (!checkHash || CheckPdbHash(ComputePdbHash(mem))))
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
        private List<AssemblyInfo> assemblies = new List<AssemblyInfo>();
        private readonly HttpClient client;
        private readonly ILogger logger;
        private readonly IAssemblyResolver resolver;

        public DebugStore(ILogger logger, HttpClient client)
        {
            this.client = client;
            this.logger = logger;
            this.resolver = new DefaultAssemblyResolver();
        }

        public DebugStore(ILogger logger) : this(logger, new HttpClient())
        { }

        private class DebugItem
        {
            public string Url { get; set; }
            public Task<byte[][]> Data { get; set; }
        }

        public IEnumerable<SourceFile> Add(SessionId sessionId, byte[] assembly_data, byte[] pdb_data)
        {
            AssemblyInfo assembly = null;
            try
            {
                assembly = new AssemblyInfo(this.resolver, sessionId.ToString(), assembly_data, pdb_data);
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

        public async IAsyncEnumerable<SourceFile> Load(SessionId sessionId, string[] loaded_files, [EnumeratorCancellation] CancellationToken token)
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
                    assembly = new AssemblyInfo(this.resolver, step.Url, bytes[0], bytes[1]);
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
                foreach (SequencePoint sequencePoint in method.DebugInformation.SequencePoints)
                {
                    if (!sequencePoint.IsHidden && Match(sequencePoint, start, end))
                        res.Add(new SourceLocation(method, sequencePoint));
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
                foreach (SequencePoint sequencePoint in method.DebugInformation.SequencePoints)
                {
                    if (!sequencePoint.IsHidden && Match(sequencePoint, request.Line, request.Column))
                        yield return new SourceLocation(method, sequencePoint);
                }
            }
        }

        public string ToUrl(SourceLocation location) => location != null ? GetFileById(location.Id).Url : "";
    }
}
