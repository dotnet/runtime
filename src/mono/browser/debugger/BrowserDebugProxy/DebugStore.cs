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
using System.IO.Compression;
using System.Reflection;
using System.Diagnostics;
using System.Text;
using Microsoft.SymbolStore;
using Microsoft.SymbolStore.SymbolStores;
using Microsoft.FileFormats.PE;
using Microsoft.Extensions.Primitives;
using Microsoft.NET.WebAssembly.Webcil;
using System.Net.Security;
using Microsoft.FileFormats.PDB;

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

    internal sealed class BreakpointRequest
    {
        public string Id { get; private set; }
        public string Assembly { get; private set; }
        public string File { get; private set; }
        public int Line { get; set; }
        public int Column { get; set; }
        public string Condition { get; set; }
        public MethodInfo Method { get; set; }

        private JObject request;

        public bool IsResolved => Assembly != null;
        public List<Breakpoint> Locations { get; set; } = new List<Breakpoint>();

        public override string ToString() => $"BreakpointRequest Assembly: {Assembly} File: {File} Line: {Line} Column: {Column}, Id: {Id}";

        public object AsSetBreakpointByUrlResponse(IEnumerable<object> jsloc) => new { breakpointId = Id, locations = Locations.Select(l => l.Location.AsLocation()).Concat(jsloc) };

        public BreakpointRequest()
        { }


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
                return regex.IsMatch(sourceFile.Url.ToString()) || regex.IsMatch(sourceFile.FilePath);
            }

            return sourceFile.Url.ToString() == url || sourceFile.DotNetUrlEscaped == url;
        }

        public bool TryResolve(SourceFile sourceFile)
        {
            if (!IsMatch(sourceFile))
                return false;

            int? line = request?["lineNumber"]?.Value<int>();
            int column = request?["columnNumber"]?.Value<int>() ?? 0;

            if (line == null)
                return false;

            Assembly = sourceFile.AssemblyName;
            File = sourceFile.FilePath;
            Line = line.Value;
            Column = column;
            return true;
        }

        public bool TryResolve(DebugStore store)
        {
            if (request == null || store == null)
                return false;

            return store.AllSources().FirstOrDefault(TryResolve) != null;
        }

        public bool CompareRequest(JObject req)
          => this.request["url"].Value<string>() == req["url"].Value<string>() &&
                this.request["lineNumber"].Value<int>() == req["lineNumber"].Value<int>() &&
                this.request["columnNumber"].Value<int>() == req["columnNumber"].Value<int>();

        public void UpdateCondition(string condition)
        {
            Condition = condition;
            foreach (var loc in Locations)
            {
                loc.Condition = condition;
            }
        }

    }

    internal sealed class VarInfo
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

    internal sealed class IlLocation
    {
        public IlLocation(MethodInfo method, int offset)
        {
            Method = method;
            Offset = offset;
        }

        public MethodInfo Method { get; }
        public int Offset { get; }
    }

    internal sealed class SourceLocation
    {
        private readonly SourceId id;
        private readonly int line;
        private readonly int column;
        private readonly IlLocation ilLocation;

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

        internal sealed class LocationComparer : EqualityComparer<SourceLocation>
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

    internal sealed class SourceId
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

    internal sealed class MethodInfo
    {
        private readonly MethodDefinition methodDef;
        internal SourceFile Source { get; set; }

        public SourceId SourceId => Source.SourceId;

        public string SourceName => Source.FilePath;

        public string Name { get; }
        public MethodDebugInformation DebugInformation;
        public MethodDefinitionHandle methodDefHandle;
        internal MetadataReader pdbMetadataReader;
        internal bool hasDebugInformation;

        public SourceLocation StartLocation { get; set; }
        public SourceLocation EndLocation { get; set; }
        public AssemblyInfo Assembly { get; }
        public int Token { get; }
        internal bool IsEnCMethod;
        internal LocalScopeHandleCollection localScopes;
        public bool IsStatic() => (Attributes & MethodAttributes.Static) != 0;
        public MethodAttributes Attributes { get; }
        public int IsAsync { get; set; }
        public DebuggerAttributesInfo DebuggerAttrInfo { get; set; }
        public TypeInfo TypeInfo { get; }
        public bool HasSequencePoints { get => hasDebugInformation && !DebugInformation.SequencePointsBlob.IsNil; }
        private ParameterInfo[] _parametersInfo;
        public int KickOffMethod { get; }
        internal bool IsCompilerGenerated { get; }
        private AsyncScopeDebugInformation[] _asyncScopes { get; set; }
        private static SignatureTypeProvider _signatureTypeProvider = new();

        public MethodInfo(AssemblyInfo assembly, string methodName, int methodToken, TypeInfo type, MethodAttributes attrs)
        {
            this.IsAsync = -1;
            this.Assembly = assembly;
            this.Attributes = attrs;
            this.Name = methodName;
            this.Token = methodToken;
            this.TypeInfo = type;
            TypeInfo.Methods.Add(this);
            assembly.Methods[methodToken] = this;
            _asyncScopes = Array.Empty<AsyncScopeDebugInformation>();
        }

        public MethodInfo(AssemblyInfo assembly, MethodDefinitionHandle methodDefHandle, int token, SourceFile source, TypeInfo type, MetadataReader asmMetadataReader, MetadataReader pdbMetadataReader, bool fromEnC)
        {
            this.IsAsync = -1;
            this.Assembly = assembly;
            this.methodDef = asmMetadataReader.GetMethodDefinition(methodDefHandle);
            this.Attributes = methodDef.Attributes;
            this.Source = source;
            this.Token = token;
            this.methodDefHandle = methodDefHandle;
            this.Name = assembly.EnCGetString(methodDef.Name);
            this.pdbMetadataReader = pdbMetadataReader;
            UpdatePdbInformation(methodDefHandle);
            if (hasDebugInformation && !DebugInformation.GetStateMachineKickoffMethod().IsNil)
                this.KickOffMethod = asmMetadataReader.GetRowNumber(DebugInformation.GetStateMachineKickoffMethod());
            else
                this.KickOffMethod = -1;
            this.IsEnCMethod = false;
            this.TypeInfo = type;
            DebuggerAttrInfo = new DebuggerAttributesInfo();
            //we need to loop in all the CustomAttributes from asmMetadataReader because methodDef.GetCustomAttributes() does not work correctly on EnC metadata
            var customAttributes = fromEnC ? asmMetadataReader.CustomAttributes : methodDef.GetCustomAttributes();
            foreach (CustomAttributeHandle cattr in customAttributes)
            {
                var ca = asmMetadataReader.GetCustomAttribute(cattr);
                if (fromEnC && (ca.Parent.Kind != HandleKind.MethodDefinition || ca.Parent.GetHashCode() != (token | (int)TokenType.MdtMethodDef)))
                    continue;
                if (!assembly.TryGetCustomAttributeName(cattr, asmMetadataReader, out string name))
                    continue;
                switch (name)
                {
                    case "DebuggerHiddenAttribute":
                        DebuggerAttrInfo.HasDebuggerHidden = true;
                        break;
                    case "DebuggerStepThroughAttribute":
                        DebuggerAttrInfo.HasStepThrough = true;
                        break;
                    case "DebuggerNonUserCodeAttribute":
                        DebuggerAttrInfo.HasNonUserCode = true;
                        break;
                    case "DebuggerStepperBoundaryAttribute":
                        DebuggerAttrInfo.HasStepperBoundary = true;
                        break;
                    case nameof(CompilerGeneratedAttribute):
                        IsCompilerGenerated = true;
                        break;
                }
            }
            if (!hasDebugInformation)
                DebuggerAttrInfo.HasNonUserCode = true;
            DebuggerAttrInfo.ClearInsignificantAttrFlags();
        }

        public bool ContainsAsyncScope(int oneBasedIdx, int offset)
        {
            int arrIdx = oneBasedIdx - 1;
            return arrIdx >= 0 && arrIdx < _asyncScopes.Length &&
                    offset >= _asyncScopes[arrIdx].StartOffset && offset <= _asyncScopes[arrIdx].EndOffset;
        }

        public ParameterInfo[] GetParametersInfo()
        {
            if (_parametersInfo != null)
                return _parametersInfo;

            var signature = methodDef.Signature;
            var sigReader = Assembly.asmMetadataReader.GetBlobReader(signature);
            var decoder = new SignatureDecoder<ElementType, object>(_signatureTypeProvider, Assembly.asmMetadataReader, genericContext: null);
            MethodSignature<ElementType> methodSignature = decoder.DecodeMethodSignature(ref sigReader);

            var paramsHandles = methodDef.GetParameters().ToArray();
            var paramsCnt = paramsHandles.Length;
            var paramsInfo = new ParameterInfo[paramsCnt];

            int paramInx = 0;
            foreach (var paramHandle in paramsHandles)
            {
                var parameter = Assembly.asmMetadataReader.GetParameter(paramHandle);
                var paramName = Assembly.EnCGetString(parameter.Name);
                if (string.IsNullOrEmpty(paramName))
                {
                    continue;
                }
                var isOptional = parameter.Attributes.HasFlag(ParameterAttributes.Optional) && parameter.Attributes.HasFlag(ParameterAttributes.HasDefault);
                if (isOptional)
                {
                    var constantHandle = parameter.GetDefaultValue();
                    var blobHandle = Assembly.asmMetadataReader.GetConstant(constantHandle);
                    var paramBytes = Assembly.asmMetadataReader.GetBlobBytes(blobHandle.Value);
                    paramsInfo[paramInx] = new ParameterInfo(
                        paramName,
                        blobHandle.TypeCode,
                        paramBytes
                    );
                }
                else
                {
                    paramsInfo[paramInx] = new ParameterInfo(
                        paramName,
                        methodSignature.ParameterTypes[paramInx]
                    );
                }
                paramInx++;
            }
            _parametersInfo = paramsInfo;
            return paramsInfo;
        }

        public void UpdatePdbInformation(MethodDefinitionHandle methodDefHandleParm)
        {
            if (pdbMetadataReader == null || methodDefHandleParm.ToDebugInformationHandle().IsNil)
                return;
            DebugInformation = pdbMetadataReader.GetMethodDebugInformation(methodDefHandleParm.ToDebugInformationHandle());
            if (Source == null && !DebugInformation.Document.IsNil)
            {
                var document = pdbMetadataReader.GetDocument(DebugInformation.Document);
                var documentName = pdbMetadataReader.GetString(document.Name);
                Source = Assembly.GetOrAddSourceFile(DebugInformation.Document, documentName);
                Source.AddMethod(this);
            }
            hasDebugInformation = true;
            if (HasSequencePoints && Source != null)
            {
                var sps = DebugInformation.GetSequencePoints();
                SequencePoint start = sps.First();
                SequencePoint end = sps.First();
                Source.BreakableLines.Add(start.StartLine);

                foreach (SequencePoint sp in sps)
                {
                    if (Source.BreakableLines.Last<int>() != sp.StartLine)
                        Source.BreakableLines.Add(sp.StartLine);

                    if (sp.IsHidden)
                        continue;

                    if (sp.StartLine < start.StartLine)
                        start = sp;
                    else if (sp.StartLine == start.StartLine && sp.StartColumn < start.StartColumn)
                        start = sp;

                    if (end.EndLine == SequencePoint.HiddenLine)
                        end = sp;
                    if (sp.EndLine > end.EndLine)
                        end = sp;
                    else if (sp.EndLine == end.EndLine && sp.EndColumn > end.EndColumn)
                        end = sp;
                }

                StartLocation = new SourceLocation(this, start);
                EndLocation = new SourceLocation(this, end);
            }
            localScopes = pdbMetadataReader.GetLocalScopes(methodDefHandleParm);

            byte[] scopeDebugInformation =
                    (from cdiHandle in pdbMetadataReader.GetCustomDebugInformation(methodDefHandleParm)
                    let cdi = pdbMetadataReader.GetCustomDebugInformation(cdiHandle)
                    where pdbMetadataReader.GetGuid(cdi.Kind) == PortableCustomDebugInfoKinds.StateMachineHoistedLocalScopes
                    select pdbMetadataReader.GetBlobBytes(cdi.Value)).FirstOrDefault();

            if (scopeDebugInformation != null)
            {
                _asyncScopes = new AsyncScopeDebugInformation[scopeDebugInformation.Length / 8];
                for (int i = 0; i < _asyncScopes.Length; i++)
                {
                    int scopeOffset = BitConverter.ToInt32(scopeDebugInformation, i * 8);
                    int scopeLen = BitConverter.ToInt32(scopeDebugInformation, (i * 8) + 4);
                    _asyncScopes[i] = new AsyncScopeDebugInformation(scopeOffset, scopeOffset + scopeLen);
                }
            }

            _asyncScopes ??= Array.Empty<AsyncScopeDebugInformation>();
        }

        public void UpdateEnC(MetadataReader pdbMetadataReaderParm, int methodIdx)
        {
            this.DebugInformation = pdbMetadataReaderParm.GetMethodDebugInformation(MetadataTokens.MethodDebugInformationHandle(methodIdx));
            this.pdbMetadataReader = pdbMetadataReaderParm;
            this.IsEnCMethod = true;
            UpdatePdbInformation(MetadataTokens.MethodDefinitionHandle(methodIdx));
        }

        public SourceLocation GetLocationByIl(int pos)
        {
            SequencePoint? prev = null;
            if (HasSequencePoints) {
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

        public sealed class DebuggerAttributesInfo
        {
            internal bool HasDebuggerHidden { get; set; }
            internal bool HasStepThrough { get; set; }
            internal bool HasNonUserCode { get; set; }
            public bool HasStepperBoundary { get; internal set; }

            internal void ClearInsignificantAttrFlags()
            {
                // hierarchy: hidden > stepThrough > nonUserCode > boundary
                if (HasDebuggerHidden)
                    HasStepThrough = HasNonUserCode = HasStepperBoundary = false;
                else if (HasStepThrough)
                    HasNonUserCode = HasStepperBoundary = false;
                else if (HasNonUserCode)
                    HasStepperBoundary = false;
            }

            public bool DoAttributesAffectCallStack(bool justMyCodeEnabled)
            {
                return HasStepThrough ||
                    HasDebuggerHidden ||
                    HasStepperBoundary ||
                    (HasNonUserCode && justMyCodeEnabled);
            }

            public bool ShouldStepOut(EventKind eventKind)
            {
                return HasDebuggerHidden || (HasStepperBoundary && eventKind == EventKind.Step);
            }
        }
        public bool IsLexicallyContainedInMethod(MethodInfo containerMethod)
            => (StartLocation.Line > containerMethod.StartLocation.Line ||
                    (StartLocation.Line == containerMethod.StartLocation.Line && StartLocation.Column > containerMethod.StartLocation.Column)) &&
                (EndLocation.Line < containerMethod.EndLocation.Line ||
                    (EndLocation.Line == containerMethod.EndLocation.Line && EndLocation.Column < containerMethod.EndLocation.Column));

        internal sealed class SourceComparer : EqualityComparer<MethodInfo>
        {
            public override bool Equals(MethodInfo l1, MethodInfo l2)
            {
                if (l1.Source.Id == l2.Source.Id)
                    return true;
                return false;
            }

            public override int GetHashCode(MethodInfo loc)
            {
                return loc.Source.Id;
            }
        }

        private record struct AsyncScopeDebugInformation(int StartOffset, int EndOffset);
    }

    internal sealed class ParameterInfo
    {
        public string Name { get; init; }

        public ElementType? TypeCode { get; init; }

        public object Value { get; init; }
        public ParameterInfo(string name, ElementType type)
        {
            Name = name;
            TypeCode = type;
        }
        public ParameterInfo(string name, ConstantTypeCode? typeCode = null, byte[] value = null)
        {
            Name = name;
            if (value == null)
                return;
            switch (typeCode)
            {
                case ConstantTypeCode.Boolean:
                    Value = BitConverter.ToBoolean(value) ? 1 : 0;
                    TypeCode = ElementType.Boolean;
                    break;
                case ConstantTypeCode.Char:
                    Value = (int)BitConverter.ToChar(value);
                    TypeCode = ElementType.Char;
                    break;
                case ConstantTypeCode.Byte:
                    Value = (uint)value[0];
                    TypeCode = ElementType.U1;
                    break;
                case ConstantTypeCode.SByte:
                    Value = (int)value[0];
                    TypeCode = ElementType.I1;
                    break;
                case ConstantTypeCode.Int16:
                    Value = (int)BitConverter.ToUInt16(value, 0);
                    TypeCode = ElementType.I2;
                    break;
                case ConstantTypeCode.UInt16:
                    Value = (uint)BitConverter.ToUInt16(value, 0);
                    TypeCode = ElementType.U2;
                    break;
                case ConstantTypeCode.Int32:
                    Value = BitConverter.ToInt32(value, 0);
                    TypeCode = ElementType.I4;
                    break;
                case ConstantTypeCode.UInt32:
                    Value = BitConverter.ToUInt32(value, 0);
                    TypeCode = ElementType.U4;
                    break;
                case ConstantTypeCode.Int64:
                    Value = BitConverter.ToInt64(value, 0);
                    TypeCode = ElementType.I8;
                    break;
                case ConstantTypeCode.UInt64:
                    Value = BitConverter.ToUInt64(value, 0);
                    TypeCode = ElementType.U8;
                    break;
                case ConstantTypeCode.Single:
                    Value = BitConverter.ToSingle(value, 0);
                    TypeCode = ElementType.R4;
                    break;
                case ConstantTypeCode.Double:
                    Value = BitConverter.ToDouble(value, 0);
                    TypeCode = ElementType.R8;
                    break;
                case ConstantTypeCode.String:
                    Value = Encoding.Unicode.GetString(value);
                    TypeCode = ElementType.String;
                    break;
                case ConstantTypeCode.NullReference:
                    Value = (byte)ValueTypeId.Null;
                    TypeCode = null;
                    break;
            }
        }
    }

    internal sealed class TypeInfo
    {
        private readonly ILogger logger;
        internal AssemblyInfo assembly;
        internal int Token { get; }
        internal string Namespace { get; }
        internal bool IsCompilerGenerated { get; }
        private bool NonUserCode { get; }
        public string FullName { get; }
        internal bool IsNonUserCode => assembly.pdbMetadataReader == null || NonUserCode;
        public List<MethodInfo> Methods { get; } = new();
        public Dictionary<string, DebuggerBrowsableState?> DebuggerBrowsableFields = new();
        public Dictionary<string, DebuggerBrowsableState?> DebuggerBrowsableProperties = new();

        internal TypeInfo(AssemblyInfo assembly, string typeName, int typeToken, ILogger logger)
        {
            this.logger = logger;
            this.assembly = assembly;
            FullName = typeName;
            Token = typeToken;
        }

        internal TypeInfo(AssemblyInfo assembly, TypeDefinitionHandle typeHandle, TypeDefinition type, MetadataReader metadataReader, ILogger logger)
        {
            this.logger = logger;
            this.assembly = assembly;
            Token = MetadataTokens.GetToken(metadataReader, typeHandle);
            string name = assembly.EnCGetString(type.Name);
            var declaringType = type;
            while (declaringType.IsNested)
            {
                declaringType = metadataReader.GetTypeDefinition(declaringType.GetDeclaringType());
                name = metadataReader.GetString(declaringType.Name) + "." + name;
            }
            Namespace = assembly.EnCGetString(declaringType.Namespace);
            if (Namespace.Length > 0)
                FullName = Namespace + "." + name;
            else
                FullName = name;

            foreach (var field in type.GetFields())
            {
                try
                {
                    var fieldDefinition = metadataReader.GetFieldDefinition(field);
                    var fieldName = assembly.EnCGetString(fieldDefinition.Name);
                    AppendToBrowsable(DebuggerBrowsableFields, fieldDefinition.GetCustomAttributes(), fieldName);
                }
                catch (Exception ex)
                {
                    logger.LogDebug($"Failed to read browsable attributes of a field. ({ex.Message})");
                    continue;
                }
            }

            foreach (var prop in type.GetProperties())
            {
                try
                {
                    var propDefinition = metadataReader.GetPropertyDefinition(prop);
                    var propName = assembly.EnCGetString(propDefinition.Name);
                    AppendToBrowsable(DebuggerBrowsableProperties, propDefinition.GetCustomAttributes(), propName);
                }
                catch (Exception ex)
                {
                    logger.LogDebug($"Failed to read browsable attributes of a property. ({ex.Message})");
                    continue;
                }
            }

            foreach (CustomAttributeHandle cattr in type.GetCustomAttributes())
            {
                if (!assembly.TryGetCustomAttributeName(cattr, metadataReader, out string attributeName))
                    continue;
                switch (attributeName)
                {
                    case nameof(CompilerGeneratedAttribute):
                        IsCompilerGenerated = true;
                        break;
                    case nameof(DebuggerNonUserCodeAttribute):
                        NonUserCode = true;
                        break;
                }
            }

            void AppendToBrowsable(Dictionary<string, DebuggerBrowsableState?> dict, CustomAttributeHandleCollection customAttrs, string fieldName)
            {
                foreach (var cattr in customAttrs)
                {
                    try
                    {
                        var ctorHandle = metadataReader.GetCustomAttribute(cattr).Constructor;
                        if (ctorHandle.Kind != HandleKind.MemberReference)
                            continue;
                        var container = metadataReader.GetMemberReference((MemberReferenceHandle)ctorHandle).Parent;
                        var valueBytes = metadataReader.GetBlobBytes(metadataReader.GetCustomAttribute(cattr).Value);
                        var attributeName = assembly.EnCGetString(metadataReader.GetTypeReference((TypeReferenceHandle)container).Name);
                        if (attributeName != "DebuggerBrowsableAttribute")
                            continue;
                        var state = (DebuggerBrowsableState)valueBytes[2];
                        if (!Enum.IsDefined(typeof(DebuggerBrowsableState), state))
                            continue;
                        dict.Add(fieldName, state);
                        break;
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
        }

        public override string ToString() => "TypeInfo('" + FullName + "')";
    }

    internal sealed class AssemblyInfo
    {
        private static int next_id;
        private readonly int id;
        private readonly ILogger logger;
        private readonly Dictionary<int, MethodInfo> methods = new Dictionary<int, MethodInfo>();
        private Dictionary<string, string> sourceLinkMappings = new Dictionary<string, string>();
        private readonly List<SourceFile> sources = new List<SourceFile>();
        internal string Url { get; }
        //The caller must keep the PEReader alive and undisposed throughout the lifetime of the metadata reader
        private IDisposable peReaderOrWebcilReader;
        internal MetadataReader asmMetadataReader { get; set; }
        internal MetadataReader pdbMetadataReader { get; set; }

        internal List<(MetadataReader asm, MetadataReader pdb)> enCMetadataReader = new();
        private int debugId;
        internal int PdbAge { get; private set; }
        internal System.Guid PdbGuid { get; private set; }
        internal bool IsPortableCodeView { get; set; }
        internal string PdbName { get; set; }
        public bool TriedToLoadSymbolsOnDemand { get; set; }

        private readonly Dictionary<int, SourceFile> _documentIdToSourceFileTable = new Dictionary<int, SourceFile>();
        public PdbChecksum[] PdbChecksums { get; set; }

        public void LoadInfoFromBytes(MonoProxy monoProxy, SessionId sessionId, AssemblyAndPdbData assemblyAndPdbData, CancellationToken token)
        {
            using var asmStream = new MemoryStream(assemblyAndPdbData.AsmBytes);
            if (assemblyAndPdbData.IsAsmMetadataOnly)
            {
                FromAssemblyAndPdbData(asmStream, assemblyAndPdbData);
            }
            else
            {
                try
                {
                    // First try to read it as a PE file, otherwise try it as a WebCIL file
                    var peReader = new PEReader(asmStream);
                    if (!peReader.HasMetadata)
                        throw new BadImageFormatException();
                    FromPEReader(monoProxy, sessionId, peReader, assemblyAndPdbData.PdbBytes, logger, token);
                }
                catch (BadImageFormatException)
                {
                    // This is a WebAssembly file
                    asmStream.Seek(0, SeekOrigin.Begin);
                    var webcilReader = new WebcilReader(asmStream);
                    FromWebcilReader(monoProxy, sessionId, webcilReader, assemblyAndPdbData.PdbBytes, logger, token);
                }
            }
        }

        public static AssemblyInfo FromBytes(MonoProxy monoProxy, SessionId sessionId, AssemblyAndPdbData assemblyAndPdbData, ILogger logger, CancellationToken token)
        {
            var assemblyInfo = new AssemblyInfo(logger);
            assemblyInfo.LoadInfoFromBytes(monoProxy, sessionId, assemblyAndPdbData, token);
            return assemblyInfo;
        }

        public static AssemblyInfo WithoutDebugInfo(ILogger logger)
        {
            return new AssemblyInfo(logger);
        }

        public static AssemblyInfo WithoutDebugInfo(string name, ILogger logger) => new AssemblyInfo(logger) { Name = name };

        private AssemblyInfo(ILogger logger)
        {
            debugId = -1;
            this.id = Interlocked.Increment(ref next_id);
            this.logger = logger;
        }
        private void FromAssemblyAndPdbData(Stream _stream, AssemblyAndPdbData assemblyAndPdbData)
        {
            var asmMetadataReader = MetadataReaderProvider.FromMetadataStream(_stream, MetadataStreamOptions.LeaveOpen).GetMetadataReader();
            Name = ReadAssemblyName(asmMetadataReader);
            if (assemblyAndPdbData.PdbBytes != null)
            {
                if (assemblyAndPdbData.PdbUncompressedSize > 0)
                {
                    byte[] decompressedBuffer;
                    using var compressedStream = new MemoryStream(assemblyAndPdbData.PdbBytes, writable: false);
                    using var deflateStream = new System.IO.Compression.DeflateStream(compressedStream, System.IO.Compression.CompressionMode.Decompress, leaveOpen: true);
                    decompressedBuffer = GC.AllocateUninitializedArray<byte>(assemblyAndPdbData.PdbUncompressedSize);
                    using var decompressedStream = new MemoryStream(decompressedBuffer, writable: true);
                    deflateStream.CopyTo(decompressedStream);
                    this.pdbMetadataReader = MetadataReaderProvider.FromPortablePdbStream(new MemoryStream(decompressedBuffer, writable: false)).GetMetadataReader();
                }
                else
                {
                    this.pdbMetadataReader = MetadataReaderProvider.FromPortablePdbStream(new MemoryStream(assemblyAndPdbData.PdbBytes, writable: false)).GetMetadataReader();
                }
            }
            this.asmMetadataReader = asmMetadataReader;
            PdbAge = assemblyAndPdbData.PdbAge;
            PdbGuid = assemblyAndPdbData.PdbGuid;
            PdbName = assemblyAndPdbData.PdbPath;
            IsPortableCodeView = assemblyAndPdbData.IsPortableCodeView;
            PdbChecksums = assemblyAndPdbData.PdbChecksums.ToArray();
            Populate();
        }

        private void FromPEReader(MonoProxy monoProxy, SessionId sessionId, PEReader peReader, byte[] pdb, ILogger logger, CancellationToken token)
        {
            var debugProvider = new PortableExecutableDebugMetadataProvider(peReader);

            var asmMetadataReader = PEReaderExtensions.GetMetadataReader(peReader);
            string name = ReadAssemblyName(asmMetadataReader);
            var summary = MetadataDebugSummary.Create(monoProxy, sessionId, name, debugProvider, pdb, token);

            LoadAssemblyInfo(peReader, name, asmMetadataReader, summary, logger);
        }
        private void FromWebcilReader(MonoProxy monoProxy, SessionId sessionId, WebcilReader wcReader, byte[] pdb, ILogger logger, CancellationToken token)
        {
            var debugProvider = new WebcilDebugMetadataProvider(wcReader);
            var asmMetadataReader = wcReader.GetMetadataReader();
            string name = ReadAssemblyName(asmMetadataReader);

            var summary = MetadataDebugSummary.Create(monoProxy, sessionId, name, debugProvider, pdb, token);

            LoadAssemblyInfo(wcReader, name, asmMetadataReader, summary, logger);
        }

        private static string ReadAssemblyName(MetadataReader asmMetadataReader)
        {
            var asmDef = asmMetadataReader.GetAssemblyDefinition();
            return asmDef.GetAssemblyName().Name + ".dll";
        }

        private unsafe void LoadAssemblyInfo(IDisposable owningReader, string name, MetadataReader asmMetadataReader, MetadataDebugSummary summary, ILogger logger)
        {
            peReaderOrWebcilReader = owningReader;
            var codeViewData = summary.CodeViewData;
            if (codeViewData != null)
            {
                PdbAge = codeViewData.Value.Age;
                PdbGuid = codeViewData.Value.Guid;
                PdbName = codeViewData.Value.Path;
            }
            IsPortableCodeView = summary.IsPortableCodeView;
            PdbChecksums = summary.PdbChecksums;
            this.asmMetadataReader = asmMetadataReader;
            Name = name;
            logger.LogTrace($"Info: loading AssemblyInfo with name {Name}");
            this.pdbMetadataReader = summary.PdbMetadataReader;
            Populate();
        }
        public bool TryGetCustomAttributeName(CustomAttributeHandle customAttribute, MetadataReader metadataReader, out string name)
        {
            name = "";
            try
            {
                EntityHandle ctorHandle = metadataReader.GetCustomAttribute(customAttribute).Constructor;
                if (ctorHandle.Kind != HandleKind.MemberReference)
                    return false;
                EntityHandle? container = ctorHandle.Kind switch
                {
                    HandleKind.MethodDefinition => EnCGetMethodDefinition((MethodDefinitionHandle)ctorHandle).GetDeclaringType(),
                    HandleKind.MemberReference => EnCGetMemberReference((MemberReferenceHandle)ctorHandle).Parent,
                    _ => null,
                };
                if (container == null)
                    return false;
                StringHandle? attributeTypeNameHandle = container.Value.Kind switch
                {
                    HandleKind.TypeDefinition => EnCGetTypeDefinition((TypeDefinitionHandle)container.Value).Name,
                    HandleKind.TypeReference => EnCGetTypeReference((TypeReferenceHandle)container.Value).Name,
                    HandleKind.TypeSpecification => null, // custom generic attributes, TypeSpecification does not keep the attribute name for them
                    _ => null,
                };
                if (attributeTypeNameHandle == null)
                    return false;
                name = EnCGetString(attributeTypeNameHandle.Value);
                return true;
            }
            catch (Exception e)
            {
                logger.LogError($"Not able to get CustomAttributeName {e}");
            }
            return false;
        }

        public async Task<int> GetDebugId(MonoSDBHelper sdbAgent, CancellationToken token)
        {
            if (debugId > 0)
                return debugId;
            debugId = await sdbAgent.GetAssemblyId(Name, token);
            return debugId;
        }

        public void SetDebugId(int id)
        {
            if (debugId <= 0 && debugId != id)
                debugId = id;
        }

        public bool EnC(MonoSDBHelper sdbAgent, byte[] meta, byte[] pdb)
        {
            var asmStream = new MemoryStream(meta);
            MetadataReader asmMetadataReader = MetadataReaderProvider.FromMetadataStream(asmStream).GetMetadataReader();
            var pdbStream = new MemoryStream(pdb);
            MetadataReader pdbMetadataReader = MetadataReaderProvider.FromPortablePdbStream(pdbStream).GetMetadataReader();
            enCMetadataReader.Add(new(asmMetadataReader, pdbMetadataReader));
            PopulateEnC(sdbAgent, asmMetadataReader, pdbMetadataReader);
            return true;
        }
        private static int GetTypeDefIdx(MetadataReader asmMetadataReaderParm, int number)
        {
            int i = 1;
            foreach (var encMapHandle in asmMetadataReaderParm.GetEditAndContinueMapEntries())
            {
                if (encMapHandle.Kind == HandleKind.TypeDefinition)
                {
                    if (asmMetadataReaderParm.GetRowNumber(encMapHandle) == number)
                        return i;
                    i++;
                }
            }
            return -1;
        }

        private static int GetMethodDebugInformationIdx(MetadataReader pdbMetadataReaderParm, int number)
        {
            int i = 1;
            foreach (var encMapHandle in pdbMetadataReaderParm.GetEditAndContinueMapEntries())
            {
                if (encMapHandle.Kind == HandleKind.MethodDebugInformation)
                {
                    if (pdbMetadataReaderParm.GetRowNumber(encMapHandle) == number)
                        return i;
                    i++;
                }
            }
            return -1;
        }

        public TypeReference EnCGetTypeReference(TypeReferenceHandle methodDefHandle)
        {
            var asmMetadataReaderLocal = asmMetadataReader;
            var typeIdx = methodDefHandle.GetHashCode();
            int i = 0;
            while (typeIdx > asmMetadataReaderLocal.TypeReferences.Count)
            {
                typeIdx -= asmMetadataReaderLocal.TypeReferences.Count;
                asmMetadataReaderLocal = enCMetadataReader[i].asm;
                i += 1;
            }
            return asmMetadataReaderLocal.GetTypeReference(MetadataTokens.TypeReferenceHandle(typeIdx));
        }

        public TypeDefinition EnCGetTypeDefinition(TypeDefinitionHandle methodDefHandle)
        {
            var asmMetadataReaderLocal = asmMetadataReader;
            var typeIdx = methodDefHandle.GetHashCode();
            int i = 0;
            while (typeIdx > asmMetadataReaderLocal.TypeDefinitions.Count)
            {
                typeIdx -= asmMetadataReaderLocal.TypeDefinitions.Count;
                asmMetadataReaderLocal = enCMetadataReader[i].asm;
                i += 1;
            }
            return asmMetadataReaderLocal.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(typeIdx));
        }
        public MethodDefinition EnCGetMethodDefinition(MethodDefinitionHandle methodDefHandle)
        {
            var asmMetadataReaderLocal = asmMetadataReader;
            var methodIdx = methodDefHandle.GetHashCode();
            int i = 0;
            while (methodIdx > asmMetadataReaderLocal.MethodDefinitions.Count)
            {
                methodIdx -= asmMetadataReaderLocal.MethodDefinitions.Count;
                asmMetadataReaderLocal = enCMetadataReader[i].asm;
                i += 1;
            }
            return asmMetadataReaderLocal.GetMethodDefinition(MetadataTokens.MethodDefinitionHandle(methodIdx));
        }

        public MemberReference EnCGetMemberReference(MemberReferenceHandle memberHandle)
        {
            var asmMetadataReaderLocal = asmMetadataReader;
            var memberIdx = memberHandle.GetHashCode();
            int i = 0;
            while (memberIdx > asmMetadataReaderLocal.MemberReferences.Count)
            {
                memberIdx -= asmMetadataReaderLocal.MemberReferences.Count;
                asmMetadataReaderLocal = enCMetadataReader[i].asm;
                i += 1;
            }
            return asmMetadataReaderLocal.GetMemberReference(MetadataTokens.MemberReferenceHandle(memberIdx));
        }

        public string EnCGetString(StringHandle strHandle)
        {
            var asmMetadataReaderLocal = asmMetadataReader;
            var strIdx = strHandle.GetHashCode();
            int i = 0;
            while (strIdx > asmMetadataReaderLocal.GetHeapSize(HeapIndex.String))
            {
                strIdx -= asmMetadataReaderLocal.GetHeapSize(HeapIndex.String);
                asmMetadataReaderLocal = enCMetadataReader[i].asm;
                i+=1;
            }
            return asmMetadataReaderLocal.GetString(MetadataTokens.StringHandle(strIdx));
        }

        private void PopulateEnC(MonoSDBHelper sdbAgent, MetadataReader asmMetadataReaderParm, MetadataReader pdbMetadataReaderParm)
        {
            TypeInfo typeInfo = null;
            int methodIdxAsm = 1;
            sdbAgent.ResetTypes(); // FIXME: only remove the cache for the affected type if fields or methods are added

            foreach (var entry in asmMetadataReaderParm.GetEditAndContinueLogEntries())
            {
                if (entry.Operation == EditAndContinueOperation.AddMethod ||
                    entry.Operation == EditAndContinueOperation.AddField)
                {
                    var typeHandle = (TypeDefinitionHandle)entry.Handle;
                    if (!TypesByToken.TryGetValue(MetadataTokens.GetToken(asmMetadataReaderParm, typeHandle), out typeInfo))
                    {
                        int typeDefIdx = GetTypeDefIdx(asmMetadataReaderParm, asmMetadataReaderParm.GetRowNumber(entry.Handle));
                        var typeDefinition = asmMetadataReaderParm.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(typeDefIdx));
                        StringHandle name = MetadataTokens.StringHandle(typeDefinition.Name.GetHashCode() & 127);

                        typeInfo = CreateTypeInfo(typeHandle, typeDefinition);
                    }
                }
                else if (entry.Operation == EditAndContinueOperation.Default)
                {
                    var entryRow = asmMetadataReader.GetRowNumber(entry.Handle);
                    if (entry.Handle.Kind == HandleKind.MethodDefinition)
                    {
                        int methodIdx = GetMethodDebugInformationIdx(pdbMetadataReaderParm, entryRow);
                        if (methods.TryGetValue(entryRow, out MethodInfo method))
                        {
                            method.UpdateEnC(pdbMetadataReaderParm, methodIdx);
                        }
                        else if (typeInfo != null)
                        {
                            var methodDebugInformation = pdbMetadataReaderParm.GetMethodDebugInformation(MetadataTokens.MethodDebugInformationHandle(methodIdx));
                            SourceFile source = null;
                            if (!methodDebugInformation.Document.IsNil)
                            {
                                var document = pdbMetadataReaderParm.GetDocument(methodDebugInformation.Document);
                                var documentName = pdbMetadataReaderParm.GetString(document.Name);
                                source = GetOrAddSourceFile(methodDebugInformation.Document, documentName);
                            }
                            var methodInfo = new MethodInfo(this, MetadataTokens.MethodDefinitionHandle(methodIdxAsm), entryRow, source, typeInfo, asmMetadataReaderParm, pdbMetadataReaderParm, fromEnC: true);
                            methods[entryRow] = methodInfo;

                            source?.AddMethod(methodInfo);

                            typeInfo.Methods.Add(methodInfo);
                        }
                        methodIdxAsm++;
                    }
                    else if (entry.Handle.Kind == HandleKind.FieldDefinition)
                    {
                        //Implement new instance field when it's supported on runtime
                    }
                }
                else
                {
                    logger.LogError($"Not supported EnC operation {entry.Operation}");
                }
            }
        }
        public SourceFile GetOrAddSourceFile(DocumentHandle doc, string documentName)
        {
            if (_documentIdToSourceFileTable.TryGetValue(documentName.GetHashCode(), out SourceFile source))
                return source;

            var src = new SourceFile(this, _documentIdToSourceFileTable.Count, doc, documentName, sourceLinkMappings);
            _documentIdToSourceFileTable[documentName.GetHashCode()] = src;
            return src;
        }

        private void Populate()
        {
            if (pdbMetadataReader != null)
                ProcessSourceLink();

            foreach (TypeDefinitionHandle type in asmMetadataReader.TypeDefinitions)
            {
                var typeDefinition = asmMetadataReader.GetTypeDefinition(type);
                var typeInfo = CreateTypeInfo(type, typeDefinition);

                foreach (MethodDefinitionHandle method in typeDefinition.GetMethods())
                {
                    SourceFile source = null;
                    if (pdbMetadataReader != null)
                    {
                        MethodDebugInformation methodDebugInformation = pdbMetadataReader.GetMethodDebugInformation(method.ToDebugInformationHandle());
                        if (!methodDebugInformation.Document.IsNil)
                        {
                            var document = pdbMetadataReader.GetDocument(methodDebugInformation.Document);
                            var documentName = pdbMetadataReader.GetString(document.Name);
                            source = GetOrAddSourceFile(methodDebugInformation.Document, documentName);
                        }
                    }
                    var methodInfo = new MethodInfo(this, method, asmMetadataReader.GetRowNumber(method), source, typeInfo, asmMetadataReader, pdbMetadataReader, fromEnC: false);
                    methods[asmMetadataReader.GetRowNumber(method)] = methodInfo;

                    source?.AddMethod(methodInfo);

                    typeInfo.Methods.Add(methodInfo);
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

        public TypeInfo CreateTypeInfo(TypeDefinitionHandle typeHandle, TypeDefinition type)
        {
            var typeInfo = new TypeInfo(this, typeHandle, type, asmMetadataReader, logger);
            TypesByName[typeInfo.FullName] = typeInfo;
            TypesByToken[typeInfo.Token] = typeInfo;
            return typeInfo;
        }

        public TypeInfo CreateTypeInfo(string typeName, int typeToken)
        {
            var typeInfo = new TypeInfo(this, typeName, typeToken, logger);
            TypesByName[typeInfo.FullName] = typeInfo;
            TypesByToken[typeInfo.Token] = typeInfo;
            return typeInfo;
        }

        public IEnumerable<SourceFile> Sources => this._documentIdToSourceFileTable.Values;
        public Dictionary<int, MethodInfo> Methods => this.methods;

        public Dictionary<string, TypeInfo> TypesByName { get; } = new();
        public Dictionary<int, TypeInfo> TypesByToken { get; } = new();
        public int Id => id;
        public string Name { get; set; }
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

        internal async Task LoadPDBFromSymbolServer(MonoProxy proxy, MonoSDBHelper sdbHelper, SessionId id, DebugStore debugStore, CancellationToken token)
        {
            try
            {
                if (TriedToLoadSymbolsOnDemand)
                    return;
                if (asmMetadataReader is null) //it means that the assembly was not loaded before because JMC was enabled
                {
                    var ret = await sdbHelper.GetDataFromAssemblyAndPdbAsync(Name, false, token);
                    LoadInfoFromBytes(proxy, id, ret, token);
                }
                var pdbName = Path.GetFileName(PdbName);
                var pdbGuid = PdbGuid.ToString("N").ToUpperInvariant() + (IsPortableCodeView ? "FFFFFFFF" : PdbAge);
                var key = $"{pdbName}/{pdbGuid}/{pdbName}";
                SymbolStoreFile file = await debugStore.symbolStore.GetFile(new SymbolStoreKey(key, PdbName, false, PdbChecksums), token);
                TriedToLoadSymbolsOnDemand = true;
                if (file == null)
                    return;
                var pdbStream = new MemoryStream();
                file.Stream.Position = 0;
                await file.Stream.CopyToAsync(pdbStream, token);
                pdbStream.Position = 0;
                pdbMetadataReader = MetadataReaderProvider.FromPortablePdbStream(pdbStream).GetMetadataReader();
                if (pdbMetadataReader == null)
                    return;
                ProcessSourceLink();
                foreach (var method in this.Methods)
                {
                    method.Value.pdbMetadataReader = pdbMetadataReader;
                    method.Value.UpdatePdbInformation(method.Value.methodDefHandle);
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to load symbols from symbol server. ({ex.Message})");
            }
        }
    }

    internal sealed partial class SourceFile
    {
        [GeneratedRegex(@"([:/])")]
        private static partial Regex RegexForEscapeFileName();

        private readonly Dictionary<int, MethodInfo> methods;
        private readonly AssemblyInfo assembly;
        private readonly Document doc;
        private readonly DocumentHandle docHandle;
        internal List<int> BreakableLines { get; }

        public string FilePath { get; init; }
        public string FileUriEscaped { get; init; }
        public string DotNetUrlEscaped { get; init; }

        public Uri Url { get; init; }
        public Uri SourceLinkUri { get; set; }

        public int Id { get; }
        public string AssemblyName => assembly.Name;
        public SourceId SourceId => new SourceId(assembly.Id, this.Id);
        public IEnumerable<MethodInfo> Methods => this.methods.Values;
        private static readonly SHA256 _sha256 = System.Security.Cryptography.SHA256.Create();
        private string _relativePath;

        internal SourceFile(AssemblyInfo assembly, int id, DocumentHandle docHandle, string documentName, Dictionary<string, string> sourceLinkMappings)
        {
            this.methods = new Dictionary<int, MethodInfo>();
            GetSourceLinkUrl(documentName, sourceLinkMappings);
            this.assembly = assembly;
            this.Id = id;
            this.doc = assembly.pdbMetadataReader.GetDocument(docHandle);
            this.docHandle = docHandle;
            this.BreakableLines = new List<int>();

            this.FilePath = documentName;

            string escapedDocumentName = EscapePathForUri(documentName.Replace("\\", "/"));
            this.FileUriEscaped = $"file://{(OperatingSystem.IsWindows() ? "/" : "")}{escapedDocumentName}";
            this.DotNetUrlEscaped = $"dotnet://{assembly.Name}/{escapedDocumentName}";
            if (!File.Exists(documentName) && SourceLinkUri != null)
            {
                string sourceLinkCachedPathPartial = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SourceServer", GetHashOfString(SourceLinkUri.AbsoluteUri));
                string sourceLinkCachedPath = Path.Combine(sourceLinkCachedPathPartial, _relativePath);
                if (File.Exists(sourceLinkCachedPath)) //first try to find on cache using relativePath as it's done by VS while debugging
                {
                    this.FilePath = sourceLinkCachedPath;
                    escapedDocumentName = EscapePathForUri(this.FilePath.Replace("\\", "/"));
                }
                else
                {
                    sourceLinkCachedPath = Path.Combine(sourceLinkCachedPathPartial, Path.GetFileName(_relativePath));
                    if (File.Exists(sourceLinkCachedPath)) //second try to find on cache without relativePath as it's done by VS when using "Go To Definition (F12)"
                    {
                        this.FilePath = sourceLinkCachedPath;
                        escapedDocumentName = EscapePathForUri(this.FilePath.Replace("\\", "/"));
                    }
                }
                this.FileUriEscaped = $"file://{(OperatingSystem.IsWindows() ? "/" : "")}{escapedDocumentName}";
            }
            this.Url = new Uri(File.Exists(this.FilePath) ? FileUriEscaped : DotNetUrlEscaped, UriKind.Absolute);
        }

        private void GetSourceLinkUrl(string document, Dictionary<string, string> sourceLinkMappings)
        {
            if (sourceLinkMappings.TryGetValue(document, out string url))
            {
                SourceLinkUri = new Uri(url);
                return;
            }

            foreach (KeyValuePair<string, string> sourceLinkDocument in sourceLinkMappings)
            {
                string key = sourceLinkDocument.Key;

                if (!key.EndsWith("*", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string keyTrim = key.TrimEnd('*');

                if (document.StartsWith(keyTrim, StringComparison.OrdinalIgnoreCase))
                {
                    _relativePath = document.Replace(keyTrim, "");
                    SourceLinkUri = new Uri(sourceLinkDocument.Value.TrimEnd('*') + _relativePath);
                    return;
                }
            }
        }

        private static string GetHashOfString(string str)
        {
            byte[] bytes = _sha256.ComputeHash(UnicodeEncoding.Unicode.GetBytes(str));
            StringBuilder builder = new StringBuilder(bytes.Length*2);
            foreach (byte b in bytes)
            {
                builder.Append(b.ToString("x2"));
            }
            return builder.ToString();
        }

        private static string EscapePathForUri(string path)
        {
            var builder = new StringBuilder();
            foreach (var part in RegexForEscapeFileName().Split(path))
            {
                if (part == ":" || part == "/")
                    builder.Append(part);
                else
                    builder.Append(Uri.EscapeDataString(part));
            }
            return builder.ToString();
        }

        internal void AddMethod(MethodInfo mi)
        {
            if (!this.methods.ContainsKey(mi.Token))
            {
                this.methods[mi.Token] = mi;
            }
        }

        public (int startLine, int startColumn, int endLine, int endColumn) GetExtents()
        {
            MethodInfo start = Methods.OrderBy(m => m.StartLocation.Line).ThenBy(m => m.StartLocation.Column).First();
            MethodInfo end = Methods.OrderByDescending(m => m.EndLocation.Line).ThenByDescending(m => m.EndLocation.Column).First();
            return (start.StartLocation.Line, start.StartLocation.Column, end.EndLocation.Line, end.EndLocation.Column);
        }

        private static async Task<MemoryStream> GetDataAsync(Uri uri, CancellationToken token)
        {
            var mem = new MemoryStream();
            try
            {
                if (uri.IsFile && File.Exists(uri.LocalPath))
                {
                    using (FileStream file = File.Open(uri.LocalPath, FileMode.Open))
                    {
                        await file.CopyToAsync(mem, token).ConfigureAwait(false);
                        mem.Position = 0;
                    }
                }
                else if (uri.Scheme == "http" || uri.Scheme == "https")
                {
                    using (Stream stream = await MonoProxy.HttpClient.GetStreamAsync(uri, token))
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

            foreach (Uri url in new[] { new Uri(FileUriEscaped), SourceLinkUri })
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
                url = Url.OriginalString,
                executionContextId,
                executionContextAuxData,
                //hash:  should be the v8 hash algo, managed implementation is pending
                dotNetUrl = DotNetUrlEscaped
            };
        }
    }

    internal sealed class DebugStore
    {
        internal List<AssemblyInfo> assemblies = new List<AssemblyInfo>();
        private readonly ILogger logger;
        internal readonly MonoProxy monoProxy;
        private readonly ITracer _tracer;
        internal Microsoft.SymbolStore.SymbolStores.SymbolStore symbolStore;

        // The constructor can get invoked multiple times, but only *one* of
        // the instances will be used.
        // So, keep this light, and repeatable
        public DebugStore(MonoProxy monoProxy, ILogger logger)
        {
            this.logger = logger;
            this.monoProxy = monoProxy;
            this._tracer = new Tracer(logger);
        }

        private sealed class DebugItem
        {
            public string Url { get; set; }
            public Task<AssemblyAndPdbData> DataTask { get; set; }
            public Task<byte[][]> ByteArrayTask { get; set; }
        }

        public static IEnumerable<MethodInfo> EnC(MonoSDBHelper sdbAgent, AssemblyInfo asm, byte[] meta_data, byte[] pdb_data)
        {
            asm.EnC(sdbAgent, meta_data, pdb_data);
            return GetEnCMethods(asm);
        }

        public static IEnumerable<MethodInfo> GetEnCMethods(AssemblyInfo asm)
        {
            foreach (var method in asm.Methods)
            {
                if (method.Value.IsEnCMethod)
                    yield return method.Value;
            }
        }

        public IEnumerable<SourceFile> Add(SessionId id, AssemblyAndPdbData assemblyAndPdbData, CancellationToken token)
        {
            AssemblyInfo assembly;
            try
            {
                assembly = AssemblyInfo.FromBytes(monoProxy, id, assemblyAndPdbData, logger, token);
            }
            catch (Exception e)
            {
                logger.LogError($"Failed to load assembly: ({e.Message})");
                yield break;
            }

            if (assembly == null)
                yield break;

            if (GetAssemblyByName(assembly.Name) != null)
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

        public async IAsyncEnumerable<SourceFile> Load(SessionId id, string[] loaded_files, ExecutionContext context, bool useDebuggerProtocol, [EnumeratorCancellation] CancellationToken token)
        {
            var asm_files = new List<string>();
            List<DebugItem> steps = new List<DebugItem>();

            if (!useDebuggerProtocol)
            {
                var pdb_files = new List<string>();
                foreach (string file_name in loaded_files)
                {
                    if (file_name.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase))
                        pdb_files.Add(file_name);
                    else
                        asm_files.Add(file_name);
                }

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
                                ByteArrayTask = Task.WhenAll(MonoProxy.HttpClient.GetByteArrayAsync(url, token), pdb != null ? MonoProxy.HttpClient.GetByteArrayAsync(pdb, token) : Task.FromResult<byte[]>(null)),
                            });
                    }
                    catch (Exception e)
                    {
                        logger.LogDebug($"Failed to read {url} ({e.Message})");
                    }
                }
            }
            else
            {
                foreach (string file_name in loaded_files)
                {
                    if (file_name.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase))
                        continue;
                    try
                    {
                        string unescapedFileName = Uri.UnescapeDataString(file_name);
                        steps.Add(
                            new DebugItem
                            {
                                Url = file_name,
                                DataTask = context.SdbAgent.GetDataFromAssemblyAndPdbAsync(Path.GetFileName(unescapedFileName), false, token)
                            });
                    }
                    catch (Exception e)
                    {
                        logger.LogDebug($"Failed to read {file_name} ({e.Message})");
                    }
                }
            }

            foreach (DebugItem step in steps)
            {
                AssemblyInfo assembly = null;
                try
                {
                    AssemblyAndPdbData assemblyAndPdbData;
                    if (step.ByteArrayTask != null)
                    {
                        byte[][] byteArray = await step.ByteArrayTask.ConfigureAwait(false);
                        assemblyAndPdbData = new AssemblyAndPdbData(byteArray[0], byteArray[1]);
                    }
                    else
                    {
                        assemblyAndPdbData = await step.DataTask.ConfigureAwait(false);
                    }
                    if (assemblyAndPdbData == null || assemblyAndPdbData.AsmBytes == null)
                    {
                        var unescapedFileName = Uri.UnescapeDataString(step.Url);
                        assemblies.Add(AssemblyInfo.WithoutDebugInfo(Path.GetFileName(unescapedFileName), logger));
                        logger.LogDebug($"Bytes from assembly {step.Url} is NULL");
                        continue;
                    }
                    assembly = AssemblyInfo.FromBytes(monoProxy, id, assemblyAndPdbData, logger, token);
                }
                catch (Exception e)
                {
                    logger.LogError($"Failed to load {step.Url} ({e.Message}) (stack={e.StackTrace})");
                }
                if (assembly == null)
                    continue;

                if (GetAssemblyByName(assembly.Name) != null)
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

        public AssemblyInfo GetAssemblyByName(string name)
        {
            var nameOnly = Path.GetFileNameWithoutExtension(name.AsSpan());
            foreach (var asm in assemblies)
            {
                if (MemoryExtensions.Equals(nameOnly, Path.GetFileNameWithoutExtension(asm.Name.AsSpan()), StringComparison.InvariantCultureIgnoreCase))
                    return asm;
            }
            return null;
        }

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

            if (end.Column < spStart.Column && end.Line == spStart.Line && end.Column != -1)
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
                res.AddRange(FindBreakpointLocations(start, end, method));
            return res;
        }

        public static IEnumerable<SourceLocation> FindBreakpointLocations(SourceLocation start, SourceLocation end, MethodInfo method)
        {
            if (!method.HasSequencePoints)
                yield break;
            foreach (SequencePoint sequencePoint in method.DebugInformation.GetSequencePoints())
            {
                if (!sequencePoint.IsHidden && Match(sequencePoint, start, end))
                    yield return new SourceLocation(method, sequencePoint);
            }
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

        public IEnumerable<SourceLocation> FindBreakpointLocations(BreakpointRequest request, bool ifNoneFoundThenFindNext = false)
        {
            request.TryResolve(this);

            AssemblyInfo asm = assemblies.FirstOrDefault(a => a.Name.Equals(request.Assembly, StringComparison.OrdinalIgnoreCase));
            SourceFile sourceFile = asm?.Sources?.SingleOrDefault(s => s.FilePath.Equals(request.File, StringComparison.OrdinalIgnoreCase));

            if (sourceFile == null)
                yield break;

            List<MethodInfo> methodList = FindMethodsContainingLine(sourceFile, request.Line);
            if (methodList.Count == 0)
                yield break;

            List<SourceLocation> locations = new List<SourceLocation>();
            foreach (var method in methodList)
            {
                foreach (SequencePoint sequencePoint in method.DebugInformation.GetSequencePoints())
                {
                    if (!sequencePoint.IsHidden &&
                            Match(sequencePoint, request.Line, request.Column) &&
                            sequencePoint.StartLine - 1 == request.Line &&
                            (request.Column == 0 || sequencePoint.StartColumn - 1 == request.Column))
                    {
                        // Found an exact match
                        locations.Add(new SourceLocation(method, sequencePoint));
                    }
                }
            }
            if (locations.Count == 0 && ifNoneFoundThenFindNext)
            {
                (MethodInfo method, SequencePoint seqPoint)? closest = null;
                foreach (var method in methodList)
                {
                    foreach (SequencePoint sequencePoint in method.DebugInformation.GetSequencePoints())
                    {
                        if (!sequencePoint.IsHidden &&
                                sequencePoint.StartLine > request.Line &&
                                (closest is null || closest.Value.seqPoint.StartLine > sequencePoint.StartLine))
                        {
                            // sequence points in a method are ordered,
                            // and we found the one right after request.Line
                            closest = (method, sequencePoint);
                            // .. and now we can look for it in other methods
                            break;
                        }
                    }
                }

                if (closest is not null)
                    locations.Add(new SourceLocation(closest.Value.method, closest.Value.seqPoint));
            }

            foreach (SourceLocation loc in locations)
                yield return loc;

            static List<MethodInfo> FindMethodsContainingLine(SourceFile sourceFile, int line)
            {
                List<MethodInfo> ret = new();
                foreach (MethodInfo method in sourceFile.Methods)
                {
                    if (method.DebugInformation.SequencePointsBlob.IsNil)
                        continue;
                    if (!(method.StartLocation.Line <= line && line <= method.EndLocation.Line))
                        continue;
                    ret.Add(method);
                }
                return ret;
            }
        }

        public string ToUrl(SourceLocation location) => location != null ? GetFileById(location.Id).Url.OriginalString : "";

        internal async Task ReloadAllPDBsFromSymbolServersAndSendSources(MonoProxy monoProxy, SessionId id, ExecutionContext context, CancellationToken token)
        {
            if (symbolStore == null)
                return;
            monoProxy.SendLog(id, "Loading symbols from symbol servers.", token);
            foreach (var asm in assemblies.Where(asm => asm.pdbMetadataReader == null))
            {
                asm.TriedToLoadSymbolsOnDemand = false; //force to load again because added another symbol server
                await asm.LoadPDBFromSymbolServer(monoProxy, context.SdbAgent, id, this, token);
                foreach (var source in asm.Sources)
                    await monoProxy.OnSourceFileAdded(id, source, context, token);
            }
            monoProxy.SendLog(id, "Symbols from symbol servers completely loaded.", token);
        }

        internal void UpdateSymbolStore(List<string> urlSymbolServerList, string cachePathSymbolServer)
        {
            symbolStore = null;
            foreach (var urlServer in urlSymbolServerList)
            {
                if (string.IsNullOrEmpty(urlServer))
                    continue;
                try
                {
                    symbolStore = new HttpSymbolStore(_tracer, symbolStore, new Uri($"{urlServer}/"), null);
                }
                catch (Exception ex)
                {
                    logger.LogError($"Failed to create HttpSymbolStore for this URL - {urlServer} - {ex.Message}");
                }
            }
            if (!string.IsNullOrEmpty(cachePathSymbolServer))
            {
                try
                {
                    symbolStore = new CacheSymbolStore(_tracer, symbolStore, cachePathSymbolServer);
                }
                catch (Exception ex)
                {
                    logger.LogError($"Failed to create CacheSymbolStore for this path - {cachePathSymbolServer} - {ex.Message}");
                }
            }
        }
        public sealed class Tracer : ITracer
        {
            private readonly ILogger _logger;

            public Tracer(ILogger logger)
            {
                this._logger = logger;
            }

            public void WriteLine(string message) => _logger.LogTrace(message);

            public void WriteLine(string format, params object[] arguments) => _logger.LogTrace(format, arguments);

            public void Information(string message) => _logger.LogTrace(message);

            public void Information(string format, params object[] arguments) => _logger.LogTrace(format, arguments);

            public void Warning(string message) => _logger.LogTrace(message);

            public void Warning(string format, params object[] arguments) => _logger.LogTrace(format, arguments);

            public void Error(string message) => _logger.LogTrace(message);

            public void Error(string format, params object[] arguments) => _logger.LogTrace(format, arguments);

            public void Verbose(string message) => _logger.LogTrace(message);

            public void Verbose(string format, params object[] arguments) => _logger.LogTrace(format, arguments);
        }
    }
}
