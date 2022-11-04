// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using Debug = System.Diagnostics.Debug;
using ConditionalAttribute = System.Diagnostics.ConditionalAttribute;
using Internal.LowLevelLinq;
using Internal.NativeFormat;
using Graph = Internal.Metadata.NativeFormat.Writer.AdjacencyGraph;

namespace Internal.Metadata.NativeFormat.Writer
{
    internal sealed class Edge
    {
        public MetadataRecord Source;
        public MetadataRecord Target;
        public Edge(MetadataRecord source, MetadataRecord target)
        {
            Source = source;
            Target = target;
        }
    };
    internal sealed class AdjacencyGraph
    {
        private HashSet<MetadataRecord> _vertices = new HashSet<MetadataRecord>();
        // private Dictionary<MetadataRecord, HashSet<Edge>> _edges = new Dictionary<MetadataRecord, HashSet<Edge>>();

        public void AddVertex(MetadataRecord v)
        {
            _vertices.Add(v);
        }

#if false
        public void AddEdge(Edge e)
        {
            HashSet<Edge> vedges;
            if (!_edges.TryGetValue(e.Source, out vedges))
            {
                vedges = new HashSet<Edge>();
                _edges.Add(e.Source, vedges);
            }
            vedges.Add(e);
        }
#endif

        public bool ContainsVertex(MetadataRecord v)
        {
            return _vertices.Contains(v);
        }

        public IEnumerable<MetadataRecord> Vertices
        { get { return _vertices; } }
    }

    internal partial interface IRecordVisitor
    {
        // Adds edge
        DstT Visit<SrcT, DstT>(SrcT src, DstT dst)
            where SrcT : MetadataRecord
            where DstT : MetadataRecord;

        // Adds grouped edges
        Dictionary<string, DstT> Visit<SrcT, DstT>(SrcT src, IEnumerable<KeyValuePair<string, DstT>> dst)
            where SrcT : MetadataRecord
            where DstT : MetadataRecord;

        // Adds grouped edges
        List<DstT> Visit<SrcT, DstT>(SrcT src, List<DstT> dst)
            where SrcT : MetadataRecord
            where DstT : MetadataRecord;
    }

    internal sealed class SourceVertex : MetadataRecord
    {
        public override HandleType HandleType
        {
            get { throw new NotImplementedException(); }
        }

        internal override void Save(NativeWriter writer)
        {
            throw new NotImplementedException();
        }

        internal override void Visit(IRecordVisitor visitor)
        {
            throw new NotImplementedException();
        }
    }

    internal abstract class RecordVisitorBase : IRecordVisitor
    {
#if false
        private class SequenceComparer<T>
            : IEqualityComparer<IEnumerable<T>>
        {
            public bool Equals(IEnumerable<T> x, IEnumerable<T> y)
            {
                return
                    Object.ReferenceEquals(x, y) ||
                    (x != null &&
                     y != null &&
                     x.GetType() == y.GetType() &&
                     x.SequenceEqual(y));
            }

            public static int GetHashCode(T o)
            {
                if (o == null)
                    return 0;
                return o.GetHashCode();
            }

            public int GetHashCode(IEnumerable<T> obj)
            {
                if (obj == null)
                    return 0;
                return obj.Aggregate(0, (h, o) => h ^ GetHashCode(o));
            }
        }

        Dictionary<IEnumerable<MetadataRecord>, object> _listPool = new Dictionary<IEnumerable<MetadataRecord>, object>(new SequenceComparer<MetadataRecord>());

        private List<T> GetPooledArray<T>(List<T> rec) where T : MetadataRecord
        {
            if (rec == null || rec.Count() == 0)
                return rec;

            object pooledRecord;
            if (_listPool.TryGetValue(rec, out pooledRecord) && pooledRecord != rec)
            {
                if (rec.GetType().GetElementType() == typeof(MetadataRecord))
                    _stats.ArraySizeSavings += rec.Count() * sizeof(int) - 3;
                else
                    _stats.ArraySizeSavings += rec.Count() * 3 - 3;
                Debug.Assert(rec.GetType() == pooledRecord.GetType());
                rec = (List<T>)pooledRecord;
            }
            else
            {
                _listPool[rec] = rec;
            }
            return rec;
        }

        private struct Stats
        {
            public int ArraySizeSavings;
        }
        private Stats _stats = new Stats();
#endif

        private Dictionary<MetadataRecord, MetadataRecord> _recordPool = new Dictionary<MetadataRecord, MetadataRecord>();

        public RecordVisitorBase()
        {
            _graph.AddVertex(MetaSourceVertex);
        }

        internal T MapToPooledRecord<T>(T rec) where T : MetadataRecord
        {
            return (T)_recordPool[rec];
        }

        private T GetPooledRecord<T>(T rec) where T : MetadataRecord
        {
            if (rec == null)
                return rec;

            MetadataRecord pooledRecord;
            if (_recordPool.TryGetValue(rec, out pooledRecord) && pooledRecord != rec)
            {
                Debug.Assert(rec.GetType() == pooledRecord.GetType());
                rec = (T)pooledRecord;
            }
            else
            {
                _recordPool[rec] = rec;
            }
            return rec;
        }

        // Adds a Vertex
        public T Visit<T>(T rec)
            where T : MetadataRecord
        {
            rec = GetPooledRecord(rec);

            if (rec == null)
                return rec;

            if (_graph.ContainsVertex(rec))
                return rec;

            _graph.AddVertex(rec);
            _queue.Enqueue(rec);

            return rec;
        }

        // Adds Edges
        public Dictionary<string, DstT> Visit<SrcT, DstT>(SrcT src, IEnumerable<KeyValuePair<string, DstT>> dst)
            where SrcT : MetadataRecord
            where DstT : MetadataRecord
        {
            var res = new Dictionary<string, DstT>();

            foreach (var kv in dst)
            {
                res.Add(kv.Key, Visit(src, kv.Value, true));
            }

            return res;
        }

        public void Run(IEnumerable<MetadataRecord> records)
        {
            foreach (var rec in records)
            {
                Visit((MetadataRecord)null, rec);
            }

            while (_queue.Count != 0)
            {
                _queue.Dequeue().Visit(this);
            }
        }

        // Adds Edges
        public List<DstT> Visit<SrcT, DstT>(SrcT src, List<DstT> dst)
            where SrcT : MetadataRecord
            where DstT : MetadataRecord
        {
#if false
            return GetPooledArray(dst.Select(d => Visit(src, d, true)).ToList());
#else
            var result = new List<DstT>(dst.Count);
            foreach (var destNode in dst)
                result.Add(Visit(src, destNode, true));

            return result;
#endif
        }

        // Adds Edge
        public DstT Visit<SrcT, DstT>(SrcT src, DstT dst)
            where SrcT : MetadataRecord
            where DstT : MetadataRecord
        {
            return Visit(src, dst, src == null);
        }

        // Adds Edge
        internal DstT Visit<SrcT, DstT>(SrcT src, DstT dst, bool isChild)
            where SrcT : MetadataRecord
            where DstT : MetadataRecord
        {
            var res = Visit(dst);

#if false
            if (res != null)
            {
                _graph.AddEdge(new Edge(src ?? MetaSourceVertex, res));
            }
#endif

            return res;
        }

        protected Queue<MetadataRecord> _queue = new Queue<MetadataRecord>();
        protected Graph _graph = new Graph();

        public Graph Graph { get { return _graph; } }
        public readonly MetadataRecord MetaSourceVertex = new SourceVertex();
    }

    internal sealed class RecordVisitor : RecordVisitorBase
    {
    }


    internal sealed partial class MetadataHeader : MetadataRecord
    {
        public const uint Signature = 0xDEADDFFD;
        public List<ScopeDefinition> ScopeDefinitions = new List<ScopeDefinition>();

        internal override void Save(NativeWriter writer)
        {
            writer.WriteUInt32(Signature);
            writer.Write(ScopeDefinitions);
        }

        public override HandleType HandleType
        {
            get { throw new NotImplementedException(); }
        }

        internal override void Visit(IRecordVisitor visitor)
        {
            ScopeDefinitions = visitor.Visit(this, ScopeDefinitions);
        }
    }

    public partial class MetadataWriter
    {
        internal MetadataHeader _metadataHeader = new MetadataHeader();

        public List<MetadataRecord> AdditionalRootRecords { get; private set; }

        public List<ScopeDefinition> ScopeDefinitions
        {
            get { return _metadataHeader.ScopeDefinitions; }
        }

        private RecordVisitor _visitor;

        public MetadataWriter()
        {
            AdditionalRootRecords = new List<MetadataRecord>();
        }

        public int GetRecordHandle(MetadataRecord rec)
        {
            var realRec = _visitor.MapToPooledRecord(rec);

            Debug.Assert(realRec.Handle.Offset != 0);

            return realRec.Handle._value;
        }

        public void Write(Stream stream)
        {
            _visitor = new RecordVisitor();

            _visitor.Run(ScopeDefinitions.AsEnumerable());
            _visitor.Run(AdditionalRootRecords.AsEnumerable());

            IEnumerable<MetadataRecord> records = _visitor.Graph.Vertices.Where(v => v != _visitor.MetaSourceVertex);

            var writer = new NativeWriter();

            var section = writer.NewSection();

            _metadataHeader.ScopeDefinitions = ScopeDefinitions;
            section.Place(_metadataHeader);

            foreach (var rec in records)
            {
                section.Place(rec);
            }

            writer.Save(stream);

            if (LogWriter != null)
            {
                // Create a CSV file, one line per meta-data record.
                LogWriter.WriteLine("Handle, Kind, Name, Children");
                // needed to enumerate children of a meta-data record
                var childVisitor = new WriteChildrenVisitor(LogWriter);

                foreach (var rec in records)
                {
                    // First the metadata handle
                    LogWriter.Write(rec.Handle._value.ToString("x8"));
                    LogWriter.Write(", ");

                    // Next the handle type
                    LogWriter.Write(rec.HandleType.ToString());
                    LogWriter.Write(", ");

                    // 3rd, the name, Quote the string if not already quoted
                    string asString = rec.ToString(false);
                    bool alreadyQuoted = asString.StartsWith("\"") && asString.EndsWith("\"");
                    if (!alreadyQuoted)
                    {
                        LogWriter.Write("\"");
                        asString = asString.Replace("\\", "\\\\").Replace("\"", "\\\"");  // Quote " and \
                    }
                    // TODO we assume that a quoted string is escaped properly
                    LogWriter.Write(asString);

                    if (!alreadyQuoted)
                        LogWriter.Write("\"");
                    LogWriter.Write(", ");

                    // Finally write out the handle IDs for my children
                    LogWriter.Write("\"");
                    childVisitor.Reset();
                    rec.Visit(childVisitor);
                    LogWriter.Write("\"");
                    LogWriter.WriteLine();
                }
                LogWriter.Flush();
            }
        }

        // WriteChildrenVisitor is a helper class needed to write out the list of the
        // handles (as space separated hex numbers) of all children of a given node
        // to the 'logWriter' text stream.  It simply implements the IRecordVisitor
        // interface to hook the callbacks needed for the MetadataRecord.Visit API.
        // It is only used in the Write() method above.
        private sealed class WriteChildrenVisitor : IRecordVisitor
        {
            public WriteChildrenVisitor(TextWriter logWriter)
            {
                _logWriter = logWriter;
            }

            // Resets the state back to what is was just after the constructor is called.
            public void Reset() { _notFirst = false; }

            // All visits come to here for every child.  Here we simply print the handle as hex.
            public void Log(MetadataRecord rec)
            {
                if (rec == null)
                    return;
                if (_notFirst)
                    _logWriter.Write(" ");
                else
                    _notFirst = true;
                _logWriter.Write(rec.Handle._value.ToString("x"));
            }

            public DstT Visit<SrcT, DstT>(SrcT src, DstT dst) where SrcT : MetadataRecord where DstT : MetadataRecord
            {
                Log(dst);
                return dst;
            }
            public Dictionary<string, DstT> Visit<SrcT, DstT>(SrcT src, IEnumerable<KeyValuePair<string, DstT>> dst) where SrcT : MetadataRecord where DstT : MetadataRecord
            {
                foreach (var keyValue in dst)
                    Log(keyValue.Value);
                return dst as Dictionary<string, DstT>;
            }
            public List<DstT> Visit<SrcT, DstT>(SrcT src, List<DstT> dst) where SrcT : MetadataRecord where DstT : MetadataRecord
            {
                foreach (var elem in dst)
                    Log(elem);
                return dst.ToList();
            }

            private bool _notFirst;           // The first child should not have a space before it.  This tracks this
            private TextWriter _logWriter;    // Where we write output to
        }

        public TextWriter LogWriter;
    }

    internal sealed class ReentrancyGuardStack
    {
        private MetadataRecord[] _array;
        private int _size;

        public ReentrancyGuardStack()
        {
            // Start with a non-zero initial size. With a bit of luck this will prevent memory allocations
            // when Push() is used.
            _array = new MetadataRecord[8];
            _size = 0;
        }

        public bool Contains(MetadataRecord item)
        {
            int count = _size;
            while (count-- > 0)
            {
                // Important: we use ReferenceEquals because this method will be called from Equals()
                // on 'record'. This is also why we can't use System.Collections.Generic.Stack.
                if (ReferenceEquals(item, _array[count]))
                    return true;
            }
            return false;
        }

        public MetadataRecord Pop()
        {
            if (_size == 0)
                throw new InvalidOperationException();
            MetadataRecord record = _array[--_size];
            _array[_size] = null;
            return record;
        }

        public void Push(MetadataRecord item)
        {
            if (_size == _array.Length)
                Array.Resize(ref _array, 2 * _array.Length);
            _array[_size++] = item;
        }
    }

    public abstract class MetadataRecord : Vertex
    {
        protected int _hash;

        // debug-only guard against reentrancy in GetHashCode()
        private bool _gettingHashCode;

        [Conditional("DEBUG")]
        protected void EnterGetHashCode()
        {
            Debug.Assert(!_gettingHashCode);
            _gettingHashCode = true;
        }

        [Conditional("DEBUG")]
        protected void LeaveGetHashCode()
        {
            Debug.Assert(_gettingHashCode);
            _gettingHashCode = false;
        }

        public abstract HandleType HandleType { get; }

        internal int HandleOffset
        {
            get
            {
                return _offset & 0x00FFFFFF;
            }
        }

        internal Handle Handle
        {
            get
            {
                return new Handle(HandleType, HandleOffset);
            }
        }

        internal abstract void Visit(IRecordVisitor visitor);

        public override string ToString()
        {
            return "[@TODO:" + this.GetType().ToString() + "]";
        }

        public virtual string ToString(bool includeHandleValue)
        {
            return ToString();
        }

        protected static string ToString<T>(IEnumerable<T> arr, string sep = ", ", bool includeHandleValue = false) where T : MetadataRecord
        {
            return string.Join(sep, arr.Select(v => v.ToString(includeHandleValue)));
        }
    }

    public interface ICustomAttributeMetadataRecord
    {
        IList<CustomAttribute> GetCustomAttributes();
    }

    public abstract partial class Blob : MetadataRecord
    {
    }

    /// <summary>
    /// Supplements generated class with convenient coversion operators
    /// </summary>
    public partial class ConstantStringValue
    {
        public static explicit operator string(ConstantStringValue value)
        {
            if (value == null)
                return null;
            else
                return value.Value;
        }

        public static explicit operator ConstantStringValue(string value)
        {
            return new ConstantStringValue() { Value = value };
        }
    }

    public partial class ScopeDefinition
    {
        public override string ToString()
        {
            return ToString(true);
        }
        public override string ToString(bool includeHandleValue)
        {
            return Name.ToString() + (includeHandleValue ? string.Format(" ({0:x})", Handle._value) : "");
        }
    }

    public partial class ScopeReference
    {
        public override string ToString()
        {
            return ToString(true);
        }
        public override string ToString(bool includeHandleValue)
        {
            return Name.ToString() + (includeHandleValue ? string.Format(" ({0:x})", Handle._value) : "");
        }
    }

    public partial class NamespaceDefinition
    {
        public override string ToString()
        {
            return ToString(true);
        }

        public override string ToString(bool includeHandleValue)
        {
            string str;

            if (Name != null && !string.IsNullOrEmpty(Name.Value))
            {
                str = Name.Value;
            }
            else
            {
                str = string.Empty;
            }

            if (includeHandleValue)
                str += string.Format("({0})", Handle.ToString());

            if (this.ParentScopeOrNamespace != null)
            {
                var pns = this.ParentScopeOrNamespace as NamespaceDefinition;
                if (pns != null)
                {
                    if (!string.IsNullOrEmpty(pns.ToString(false)))
                        str = pns.ToString(false) + '.' + str;
                }
            }
            return str;
        }
    }

    public partial class NamespaceReference
    {
        public override string ToString()
        {
            return ToString(true);
        }
        public override string ToString(bool includeHandleValue)
        {
            string str;

            if (Name != null && !string.IsNullOrEmpty(Name.Value))
            {
                str = Name.Value;
            }
            else
            {
                str = string.Empty;
            }

            if (includeHandleValue)
                str += string.Format("({0})", Handle.ToString());

            if (this.ParentScopeOrNamespace != null)
            {
                var pns = this.ParentScopeOrNamespace as NamespaceReference;
                if (pns != null)
                {
                    if (!string.IsNullOrEmpty(pns.ToString(false)))
                        str = pns.ToString(false) + '.' + str;
                }
                else
                {
                    //str = ParentScopeOrNamespace.ToString() + " : " + str;
                }
            }
            return str;
        }
    }

    public partial class TypeDefinition
    {
        public override string ToString()
        {
            return ToString(false);
        }
        public override string ToString(bool includeHandleValue)
        {
            string str;
            if (this.EnclosingType != null)
            {
                str = this.EnclosingType.ToString(false) + "+" + Name.Value;
                if (includeHandleValue)
                    str += string.Format(" ({0:x})", Handle._value);
                return str;
            }
            else if (this.NamespaceDefinition != null && this.NamespaceDefinition.Name != null)
            {
                str = this.NamespaceDefinition.ToString(false) + "." + Name.Value;
                if (includeHandleValue)
                    str += string.Format(" ({0:x})", Handle._value);
                return str;
            }
            str = Name.Value + string.Format(" ({0:x})", Handle._value);
            if (includeHandleValue)
                str += string.Format(" ({0:x})", Handle._value);
            return str;
        }
    }

    public partial class TypeReference
    {
        public override string ToString()
        {
            return ToString(false);
        }
        public override string ToString(bool includeHandleValue)
        {
            string s = "";
            if (ParentNamespaceOrType is NamespaceReference)
                s += ParentNamespaceOrType.ToString(false) + ".";
            if (ParentNamespaceOrType is TypeReference)
                s += ParentNamespaceOrType.ToString(false) + "+";
            s += TypeName.Value;
            if (includeHandleValue)
                s += string.Format(" ({0:x})", Handle._value);
            return s;
        }
    }

    public partial class TypeForwarder
    {
        public override string ToString()
        {
            return this.Name.Value + " -> " + this.Scope.Name.Value;
        }
    }

    public partial class GenericParameter
    {
        public override string ToString()
        {
            return Kind.FlagsToString() + " " + Name.Value + "(" + Number.ToString() + ")";
        }
    }

    public partial class Field
    {
        public override string ToString()
        {
            return Name.Value;
        }
    }

    public partial class Method
    {
        public override string ToString()
        {
            return Signature.ToString(Name.Value);
        }
    }

    public partial class QualifiedMethod
    {
        public override string ToString()
        {
            return EnclosingType.ToString(false) + "." + Method.ToString();
        }
    }

    public partial class Property
    {
        public override string ToString()
        {
            return Name.Value;
        }
    }

    public partial class Event
    {
        public override string ToString()
        {
            return Name.Value;
        }
    }

    public partial class SZArraySignature
    {
        public override string ToString()
        {
            return ElementType.ToString() + "[]";
        }
    }

    public partial class ArraySignature
    {
        public override string ToString()
        {
            return ElementType.ToString() + "[" + new string(',', Rank - 1) + "]";
        }
    }

    public partial class TypeSpecification
    {
        public override string ToString()
        {
            return Signature.ToString();
        }
    }

    public partial class TypeInstantiationSignature
    {
        public override string ToString()
        {
            return this.GenericType.ToString() + "<" + string.Join(", ", this.GenericTypeArguments.Select(ga => ga.ToString())) + ">";
        }
    }

    /* COMPLETENESS
    public partial class MethodImpl
    {
        public override string ToString()
        {
            return this.MethodDeclaration.ToString();
        }
    }*/

    public partial class MethodInstantiation
    {
        public override string ToString()
        {
            return Method.ToString()
                + "(Arguments: "
                + "<"
                + string.Join(", ", this.GenericTypeArguments.Select(ga => ga.ToString()))
                + ">";
        }
    }

    public partial class ByReferenceSignature
    {
        public override string ToString()
        {
            return "ref " + Type.ToString();
        }
    }

    public partial class CustomAttribute
    {
        public override string ToString()
        {
            string str = Constructor.ToString();
            str += "(" + string.Join(", ", FixedArguments.Select(fa => fa.ToString()))
                + string.Join(", ", NamedArguments.Select(na => na.ToString())) + ")";
            str += "(ctor: " + Constructor.Handle.ToString();
            return str;
        }
    }

    public partial class NamedArgument
    {
        public override string ToString()
        {
            return Name + " = " + Value.ToString();
        }
    }

    public partial class MemberReference
    {
        public override string ToString()
        {
            return Parent.ToString() + "." + Name.Value + " (Signature: " + Signature.ToString() + ")";
        }
    }

    public partial class MethodSemantics
    {
        public override string ToString()
        {
            string str = Enum.GetName(typeof(MethodSemanticsAttributes), Attributes);
            return str + " : " + Method.ToString();
        }
    }

    public partial class MethodSignature
    {
        public override string ToString()
        {
            return ToString(" ");
        }

        public string ToString(string name)
        {
            return string.Join(" ", new string[] {
                CallingConvention.FlagsToString(),
                ReturnType.ToString(false),
                name
                    + (GenericParameterCount == 0 ? "" : "`" + GenericParameterCount.ToString())
                    + "(" + string.Join(", ", Parameters.Select(p => p.ToString(false))) +
                    string.Join(", ", VarArgParameters.Select(p => p.ToString(false))) + ")"}.Where(e => !string.IsNullOrWhiteSpace(e)));
        }
    }

    public partial class PropertySignature
    {
        public override string ToString()
        {
            return string.Join(" ", Enum.GetName(typeof(CallingConventions), CallingConvention),
                Type.ToString()) + "(" + ToString(Parameters) + ")";
        }
    }

    public partial class FieldSignature
    {
        public override string ToString()
        {
            return Type.ToString();
        }
    }

    public partial class ModifiedType
    {
        public override string ToString()
        {
            return "[" + (IsOptional ? "opt : " : "req : ") + ModifierType.ToString() + "] " +
                Type.ToString();
        }
    }

    public partial class TypeVariableSignature
    {
        public override string ToString()
        {
            return "!" + Number;
        }
    }

    public partial class MethodTypeVariableSignature
    {
        public override string ToString()
        {
            return "!!" + Number;
        }
    }

    public partial class Parameter
    {
        public override string ToString()
        {
            string flags = Flags.FlagsToString();
            return string.Format("{0}{1} (Seq:{2}) {3}",
                flags,
                Name.ToString(),
                Sequence,
                (DefaultValue == null ? "" : " = " + DefaultValue.ToString()));
        }
    }

    public partial class PointerSignature
    {
        public override string ToString()
        {
            return Type.ToString() + "*";
        }
    }

    public static class EnumHelpers
    {
        public static string FlagsToString<T>(this T value) where T : struct, Enum, IConvertible
        {
            var flags = Enum.GetValues<T>().Where(
                eVal => (((IConvertible)eVal).ToInt32(null) != 0) && ((((IConvertible)value).ToInt32(null) & ((IConvertible)eVal).ToInt32(null)) == ((IConvertible)eVal).ToInt32(null)));
            if (flags.Count() == 0)
                return "";
            else
                return "[" + string.Join(" | ", flags.Select(Enum.GetName<T>)) + "] ";
        }
    }

    public static class ListExtensions
    {
        public static T FirstOrDefault<T>(this List<T> list)
        {
            if (list.Count != 0)
                return list[0];
            return default(T);
        }
        public static T First<T>(this List<T> list) where T : class
        {
            if (list.Count != 0)
                return list[0];
            return null;
        }
    }

    public static partial class DictionaryExtensions
    {
        internal static T FirstOrDefault<T>(this Dictionary<string, T> dict)
        {
            if (dict.Count != 0)
                foreach (var value in dict.Values)
                    return value;
            return default(T);
        }
        internal static T First<T>(this Dictionary<string, T> dict) where T : class
        {
            if (dict.Count != 0)
                foreach (var value in dict.Values)
                    return value;
            return null;
        }

        internal static IEnumerable<T> AsSingleEnumerable<T>(this T value)
        {
            yield return value;
        }
    }

    public static partial class SignatureHelpers
    {
        public static SZArraySignature AsSZArray(this MetadataRecord record)
        {
            return new SZArraySignature() { ElementType = record };
        }
    }

    // SequenceEquals on IEnumerable is painfully slow and allocates memory.
    public static class SequenceExtensions
    {
        public static bool SequenceEqual<T>(this List<T> first, List<T> second)
        {
            return first.SequenceEqual(second, null);
        }

        public static bool SequenceEqual<T>(this List<T> first, List<T> second, IEqualityComparer<T> comparer)
        {
            if (first.Count != second.Count)
            {
                return false;
            }

            comparer ??= EqualityComparer<T>.Default;

            for (int i = 0; i < first.Count; i++)
            {
                if (!comparer.Equals(first[i], second[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool SequenceEqual<T>(this T[] first, T[] second)
        {
            return first.SequenceEqual(second, null);
        }

        public static bool SequenceEqual<T>(this T[] first, T[] second, IEqualityComparer<T> comparer)
        {
            if (first.Length != second.Length)
            {
                return false;
            }

            comparer ??= EqualityComparer<T>.Default;

            for (int i = 0; i < first.Length; i++)
            {
                if (!comparer.Equals(first[i], second[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }

    // Distinguishes positive and negative zeros for float and double values
    public static class CustomComparer
    {
        public static unsafe bool Equals(float x, float y)
        {
            return *(int*)&x == *(int*)&y;
        }

        public static bool Equals(double x, double y)
        {
            return BitConverter.DoubleToInt64Bits(x) == BitConverter.DoubleToInt64Bits(y);
        }
    }

    public sealed class SingleComparer : IEqualityComparer<float>
    {
        public static readonly SingleComparer Instance = new SingleComparer();

        public bool Equals(float x, float y) => CustomComparer.Equals(x, y);
        public int GetHashCode(float obj) => obj.GetHashCode();
    }

    public sealed class DoubleComparer : IEqualityComparer<double>
    {
        public static readonly DoubleComparer Instance = new DoubleComparer();

        public bool Equals(double x, double y) => CustomComparer.Equals(x, y);
        public int GetHashCode(double obj) => obj.GetHashCode();
    }
}
