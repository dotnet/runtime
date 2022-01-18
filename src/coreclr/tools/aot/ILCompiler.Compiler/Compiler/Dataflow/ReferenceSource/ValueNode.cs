// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Mono.Cecil;
using FieldDefinition = Mono.Cecil.FieldDefinition;
using GenericParameter = Mono.Cecil.GenericParameter;
using TypeDefinition = Mono.Cecil.TypeDefinition;

namespace Mono.Linker.Dataflow
{
	public enum ValueNodeKind
	{
		Invalid,                        // in case the Kind field is not initialized properly

		Unknown,                        // unknown value, has StaticType from context

		Null,                           // known value
		SystemType,                     // known value - TypeRepresented
		RuntimeTypeHandle,              // known value - TypeRepresented
		KnownString,                    // known value - Contents
		ConstInt,                       // known value - Int32
		AnnotatedString,                // string with known annotation

		MethodParameter,                // symbolic placeholder
		MethodReturn,                   // symbolic placeholder

		RuntimeMethodHandle,            // known value - MethodRepresented
		SystemReflectionMethodBase,     // known value - MethodRepresented

		RuntimeTypeHandleForGenericParameter, // symbolic placeholder for generic parameter
		SystemTypeForGenericParameter,        // symbolic placeholder for generic parameter

		MergePoint,                     // structural, multiplexer - Values
		GetTypeFromString,              // structural, could be known value - KnownString
		Array,                          // structural, could be known value - Array

		LoadField,                      // structural, could be known value - InstanceValue
	}

	/// <summary>
	/// A ValueNode represents a value in the IL dataflow analysis.  It may not contain complete information as it is a
	/// best-effort representation.  Additionally, as the analysis is linear and does not account for control flow, any
	/// given ValueNode may represent multiple values simultaneously.  (This occurs, for example, at control flow join
	/// points when both paths yield values on the IL stack or in a local.)
	/// </summary>
	public abstract class ValueNode : IEquatable<ValueNode>
	{
		public ValueNode ()
		{
#if false // Helpful for debugging a cycle that has inadvertently crept into the graph
			if (this.DetectCycle(new HashSet<ValueNode>()))
			{
				throw new Exception("Found a cycle");
			}
#endif
		}

		/// <summary>
		/// The 'kind' of value node -- this represents the most-derived type and allows us to switch over and do
		/// equality checks without the cost of casting.  Intermediate non-leaf types in the ValueNode hierarchy should
		/// be abstract.
		/// </summary>
		public ValueNodeKind Kind { get; protected set; }

		/// <summary>
		/// The IL type of the value, represented as closely as possible, but not always exact.  It can be null, for
		/// example, when the analysis is imprecise or operating on malformed IL.
		/// </summary>
		public TypeDefinition? StaticType { get; protected set; }

		/// <summary>
		/// Allows the enumeration of the direct children of this node.  The ChildCollection struct returned here
		/// supports 'foreach' without allocation.
		/// </summary>
		public ChildCollection Children { get { return new ChildCollection (this); } }

		/// <summary>
		/// This property allows you to enumerate all 'unique values' represented by a given ValueNode.  The basic idea
		/// is that there will be no MergePointValues in the returned ValueNodes and all structural operations will be
		/// applied so that each 'unique value' can be considered on its own without regard to the structure that led to
		/// it.
		/// </summary>
		public UniqueValueCollection UniqueValuesInternal {
			get {
				return new UniqueValueCollection (this);
			}
		}

		/// <summary>
		/// This protected method is how nodes implement the UniqueValues property.  It is protected because it returns
		/// an IEnumerable and we want to avoid allocating an enumerator for the exceedingly common case of there being
		/// only one value in the enumeration.  The UniqueValueCollection returned by the UniqueValues property handles
		/// this detail.
		/// </summary>
		protected abstract IEnumerable<ValueNode> EvaluateUniqueValues ();

		/// <summary>
		/// RepresentsExactlyOneValue is used by the UniqueValues property to allow us to bypass allocating an
		/// enumerator to return just one value.  If a node returns 'true' from RepresentsExactlyOneValue, it must also
		/// return that one value from GetSingleUniqueValue.  If it always returns 'false', it doesn't need to implement
		/// GetSingleUniqueValue.
		/// </summary>
		protected virtual bool RepresentsExactlyOneValue { get { return false; } }

		/// <summary>
		/// GetSingleUniqueValue is called if, and only if, RepresentsExactlyOneValue returns true.  It allows us to
		/// bypass the allocation of an enumerator for the common case of returning exactly one value.
		/// </summary>
		protected virtual ValueNode GetSingleUniqueValue ()
		{
			// Not implemented because RepresentsExactlyOneValue returns false and, therefore, this method should be
			// unreachable.
			throw new NotImplementedException ();
		}

		protected abstract int NumChildren { get; }
		protected abstract ValueNode ChildAt (int index);

		public virtual bool Equals (ValueNode? other)
		{
			return other != null && this.Kind == other.Kind && this.StaticType == other.StaticType;
		}

		public abstract override int GetHashCode ();

		/// <summary>
		/// Each node type must implement this to stringize itself.  The expectation is that it is implemented using
		/// ValueNodeDump.ValueNodeToString(), passing any non-ValueNode properties of interest (e.g.
		/// SystemTypeValue.TypeRepresented).  Properties that are invariant on a particular node type
		/// should be omitted for clarity.
		/// </summary>
		protected abstract string NodeToString ();

		public override string ToString ()
		{
			return NodeToString ();
		}

		public override bool Equals (object? other)
		{
			if (!(other is ValueNode))
				return false;

			return this.Equals ((ValueNode) other);
		}

		#region Specialized Collection Nested Types
		/// <summary>
		/// ChildCollection struct is used to wrap the operations on a node involving its children.  In particular, the
		/// struct implements a GetEnumerator method that is used to allow "foreach (ValueNode node in myNode.Children)"
		/// without heap allocations.
		/// </summary>
		public struct ChildCollection : IEnumerable<ValueNode>
		{
			/// <summary>
			/// Enumerator for children of a ValueNode.  Allows foreach(var child in node.Children) to work without
			/// allocating a heap-based enumerator.
			/// </summary>
			public struct Enumerator : IEnumerator<ValueNode>
			{
				int _index;
				readonly ValueNode _parent;

				public Enumerator (ValueNode parent)
				{
					_parent = parent;
					_index = -1;
				}

				public ValueNode Current { get { return _parent.ChildAt (_index); } }

				object System.Collections.IEnumerator.Current { get { return Current; } }

				public bool MoveNext ()
				{
					_index++;
					return (_parent != null) ? (_index < _parent.NumChildren) : false;
				}

				public void Reset ()
				{
					_index = -1;
				}

				public void Dispose ()
				{
				}
			}

			readonly ValueNode _parentNode;

			public ChildCollection (ValueNode parentNode) { _parentNode = parentNode; }

			// Used by C# 'foreach', when strongly typed, to avoid allocation.
			public Enumerator GetEnumerator ()
			{
				return new Enumerator (_parentNode);
			}

			IEnumerator<ValueNode> IEnumerable<ValueNode>.GetEnumerator ()
			{
				// note the boxing!
				return new Enumerator (_parentNode);
			}
			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
			{
				// note the boxing!
				return new Enumerator (_parentNode);
			}

			public int Count { get { return (_parentNode != null) ? _parentNode.NumChildren : 0; } }
		}

		/// <summary>
		/// UniqueValueCollection is used to wrap calls to ValueNode.EvaluateUniqueValues.  If a ValueNode represents
		/// only one value, then foreach(ValueNode value in node.UniqueValues) will not allocate a heap-based enumerator.
		///
		/// This is implented by having each ValueNode tell us whether or not it represents exactly one value or not.
		/// If it does, we fetch it with ValueNode.GetSingleUniqueValue(), otherwise, we fall back to the usual heap-
		/// based IEnumerable returned by ValueNode.EvaluateUniqueValues.
		/// </summary>
		public struct UniqueValueCollection : IEnumerable<ValueNode>
		{
			readonly IEnumerable<ValueNode>? _multiValueEnumerable;
			readonly ValueNode? _treeNode;

			public UniqueValueCollection (ValueNode node)
			{
				if (node.RepresentsExactlyOneValue) {
					_multiValueEnumerable = null;
					_treeNode = node;
				} else {
					_multiValueEnumerable = node.EvaluateUniqueValues ();
					_treeNode = null;
				}
			}

			public Enumerator GetEnumerator ()
			{
				return new Enumerator (_treeNode, _multiValueEnumerable);
			}

			IEnumerator<ValueNode> IEnumerable<ValueNode>.GetEnumerator ()
			{
				if (_multiValueEnumerable != null) {
					return _multiValueEnumerable.GetEnumerator ();
				}

				// note the boxing!
				return GetEnumerator ();
			}

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
			{
				if (_multiValueEnumerable != null) {
					return _multiValueEnumerable.GetEnumerator ();
				}

				// note the boxing!
				return GetEnumerator ();
			}


			public struct Enumerator : IEnumerator<ValueNode>
			{
				readonly IEnumerator<ValueNode>? _multiValueEnumerator;
				readonly ValueNode? _singleValueNode;
				int _index;

				public Enumerator (ValueNode? treeNode, IEnumerable<ValueNode>? multiValueEnumerable)
				{
					Debug.Assert (treeNode != null || multiValueEnumerable != null);
					_singleValueNode = treeNode?.GetSingleUniqueValue ();
					_multiValueEnumerator = multiValueEnumerable?.GetEnumerator ();
					_index = -1;
				}

				public void Reset ()
				{
					if (_multiValueEnumerator != null) {
						_multiValueEnumerator.Reset ();
						return;
					}

					_index = -1;
				}

				public bool MoveNext ()
				{
					if (_multiValueEnumerator != null)
						return _multiValueEnumerator.MoveNext ();

					_index++;
					return _index == 0;
				}

				public ValueNode Current {
					get {
						if (_multiValueEnumerator != null)
							return _multiValueEnumerator.Current;

						if (_index == 0)
							return _singleValueNode!;

						throw new InvalidOperationException ();
					}
				}

				object System.Collections.IEnumerator.Current { get { return Current; } }

				public void Dispose ()
				{
				}
			}
		}
		#endregion
	}

	/// <summary>
	/// LeafValueNode represents a 'leaf' in the expression tree.  In other words, the node has no ValueNode children.
	/// It *may* still have non-ValueNode 'properties' that are interesting.  This class serves, primarily, as a way to
	/// collect up the very common implmentation of NumChildren/ChildAt for leaf nodes and the "represents exactly one
	/// value" optimization.  These things aren't on the ValueNode base class because, otherwise, new node types
	/// deriving from ValueNode may 'forget' to implement these things.  So this class allows them to remain abstract in
	/// ValueNode while still having a common implementation for all the leaf nodes.
	/// </summary>
	public abstract class LeafValueNode : ValueNode
	{
		protected override int NumChildren { get { return 0; } }
		protected override ValueNode ChildAt (int index) { throw new InvalidOperationException (); }

		protected override bool RepresentsExactlyOneValue { get { return true; } }

		protected override ValueNode GetSingleUniqueValue () { return this; }


		protected override IEnumerable<ValueNode> EvaluateUniqueValues ()
		{
			// Leaf values should not represent more than one value.  This method should be unreachable as long as
			// RepresentsExactlyOneValue returns true.
			throw new NotImplementedException ();
		}
	}

	// These are extension methods because we want to allow the use of them on null 'this' pointers.
	internal static class ValueNodeExtensions
	{
		/// <summary>
		/// Returns true if a ValueNode graph contains a cycle
		/// </summary>
		/// <param name="node">Node to evaluate</param>
		/// <param name="seenNodes">Set of nodes previously seen on the current arc. Callers may pass a non-empty set
		/// to test whether adding that set to this node would create a cycle. Contents will be modified by the walk
		/// and should not be used by the caller after returning</param>
		/// <param name="allNodesSeen">Optional. The set of all nodes encountered during a walk after DetectCycle returns</param>
		/// <returns></returns>
		public static bool DetectCycle (this ValueNode? node, HashSet<ValueNode> seenNodes, HashSet<ValueNode>? allNodesSeen)
		{
			if (node == null)
				return false;

			if (seenNodes.Contains (node))
				return true;

			seenNodes.Add (node);

			if (allNodesSeen != null) {
				allNodesSeen.Add (node);
			}

			bool foundCycle = false;
			switch (node.Kind) {
			//
			// Leaf nodes
			//
			case ValueNodeKind.Unknown:
			case ValueNodeKind.Null:
			case ValueNodeKind.SystemType:
			case ValueNodeKind.RuntimeTypeHandle:
			case ValueNodeKind.KnownString:
			case ValueNodeKind.AnnotatedString:
			case ValueNodeKind.ConstInt:
			case ValueNodeKind.MethodParameter:
			case ValueNodeKind.MethodReturn:
			case ValueNodeKind.SystemTypeForGenericParameter:
			case ValueNodeKind.RuntimeTypeHandleForGenericParameter:
			case ValueNodeKind.SystemReflectionMethodBase:
			case ValueNodeKind.RuntimeMethodHandle:
			case ValueNodeKind.LoadField:
				break;

			//
			// Nodes with children
			//
			case ValueNodeKind.MergePoint:
				foreach (ValueNode val in ((MergePointValue) node).Values) {
					if (val.DetectCycle (seenNodes, allNodesSeen)) {
						foundCycle = true;
					}
				}
				break;

			case ValueNodeKind.GetTypeFromString:
				GetTypeFromStringValue gtfsv = (GetTypeFromStringValue) node;
				foundCycle = gtfsv.AssemblyIdentity.DetectCycle (seenNodes, allNodesSeen);
				foundCycle |= gtfsv.NameString.DetectCycle (seenNodes, allNodesSeen);
				break;

			case ValueNodeKind.Array:
				ArrayValue av = (ArrayValue) node;
				foundCycle = av.Size.DetectCycle (seenNodes, allNodesSeen);
				foreach (ValueBasicBlockPair pair in av.IndexValues.Values) {
					foundCycle |= pair.Value.DetectCycle (seenNodes, allNodesSeen);
				}
				break;

			default:
				throw new Exception (String.Format ("Unknown node kind: {0}", node.Kind));
			}
			seenNodes.Remove (node);

			return foundCycle;
		}

		public static ValueNode.UniqueValueCollection UniqueValues (this ValueNode? node)
		{
			if (node == null)
				return new ValueNode.UniqueValueCollection (UnknownValue.Instance);

			return node.UniqueValuesInternal;
		}

		public static int? AsConstInt (this ValueNode? node)
		{
			if (node is ConstIntValue constInt)
				return constInt.Value;
			return null;
		}
	}

	internal static class ValueNodeDump
	{
		internal static string ValueNodeToString (ValueNode? node, params object[] args)
		{
			if (node == null)
				return "<null>";

			StringBuilder sb = new StringBuilder ();
			sb.Append (node.Kind.ToString ());
			sb.Append ("(");
			if (args != null) {
				for (int i = 0; i < args.Length; i++) {
					if (i > 0)
						sb.Append (",");
					sb.Append (args[i] == null ? "<null>" : args[i].ToString ());
				}
			}
			sb.Append (")");
			return sb.ToString ();
		}

		static string GetIndent (int level)
		{
			StringBuilder sb = new StringBuilder (level * 2);
			for (int i = 0; i < level; i++)
				sb.Append ("  ");
			return sb.ToString ();
		}

		public static void DumpTree (this ValueNode node, System.IO.TextWriter? writer = null, int indentLevel = 0)
		{
			if (writer == null)
				writer = Console.Out;

			writer.Write (GetIndent (indentLevel));
			if (node == null) {
				writer.WriteLine ("<null>");
				return;
			}

			writer.WriteLine (node);
			foreach (ValueNode child in node.Children) {
				child.DumpTree (writer, indentLevel + 1);
			}
		}
	}

	/// <summary>
	/// Represents an unknown value.
	/// </summary>
	class UnknownValue : LeafValueNode
	{
		private UnknownValue ()
		{
			Kind = ValueNodeKind.Unknown;
			StaticType = null;
		}

		public static UnknownValue Instance { get; } = new UnknownValue ();

		public override bool Equals (ValueNode? other)
		{
			return base.Equals (other);
		}

		public override int GetHashCode ()
		{
			// All instances of UnknownValue are equivalent, so they all hash to the same hashcode.  This one was
			// chosen for no particular reason at all.
			return 0x98052;
		}

		protected override string NodeToString ()
		{
			return ValueNodeDump.ValueNodeToString (this);
		}
	}

	class NullValue : LeafValueNode
	{
		private NullValue ()
		{
			Kind = ValueNodeKind.Null;
			StaticType = null;
		}

		public override bool Equals (ValueNode? other)
		{
			return base.Equals (other);
		}

		public static NullValue Instance { get; } = new NullValue ();

		public override int GetHashCode ()
		{
			// All instances of NullValue are equivalent, so they all hash to the same hashcode.  This one was
			// chosen for no particular reason at all.
			return 0x90210;
		}

		protected override string NodeToString ()
		{
			return ValueNodeDump.ValueNodeToString (this);
		}
	}

	/// <summary>
	/// This is a known System.Type value.  TypeRepresented is the 'value' of the System.Type.
	/// </summary>
	class SystemTypeValue : LeafValueNode
	{
		public SystemTypeValue (TypeDefinition typeRepresented)
		{
			Kind = ValueNodeKind.SystemType;

			// Should be System.Type - but we don't have any use case where tracking it like that would matter
			StaticType = null;

			TypeRepresented = typeRepresented;
		}

		public TypeDefinition TypeRepresented { get; private set; }

		public override bool Equals (ValueNode? other)
		{
			if (!base.Equals (other))
				return false;

			return Equals (this.TypeRepresented, ((SystemTypeValue) other).TypeRepresented);
		}

		public override int GetHashCode ()
		{
			return HashCode.Combine (Kind, TypeRepresented);
		}

		protected override string NodeToString ()
		{
			return ValueNodeDump.ValueNodeToString (this, TypeRepresented);
		}
	}

	/// <summary>
	/// This is the System.RuntimeTypeHandle equivalent to a <see cref="SystemTypeValue"/> node.
	/// </summary>
	class RuntimeTypeHandleValue : LeafValueNode
	{
		public RuntimeTypeHandleValue (TypeDefinition typeRepresented)
		{
			Kind = ValueNodeKind.RuntimeTypeHandle;

			// Should be System.RuntimeTypeHandle, but we don't have a use case for it like that
			StaticType = null;

			TypeRepresented = typeRepresented;
		}

		public TypeDefinition TypeRepresented { get; }

		public override bool Equals (ValueNode? other)
		{
			if (!base.Equals (other))
				return false;

			return Equals (this.TypeRepresented, ((RuntimeTypeHandleValue) other).TypeRepresented);
		}

		public override int GetHashCode ()
		{
			return HashCode.Combine (Kind, TypeRepresented);
		}

		protected override string NodeToString ()
		{
			return ValueNodeDump.ValueNodeToString (this, TypeRepresented);
		}
	}

	/// <summary>
	/// This is a System.Type value which represents generic parameter (basically result of typeof(T))
	/// Its actual type is unknown, but it can have annotations.
	/// </summary>
	class SystemTypeForGenericParameterValue : LeafValueWithDynamicallyAccessedMemberNode
	{
		public SystemTypeForGenericParameterValue (GenericParameter genericParameter, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
			: base (genericParameter)
		{
			Kind = ValueNodeKind.SystemTypeForGenericParameter;

			// Should be System.Type, but we don't have a use case for it
			StaticType = null;

			GenericParameter = genericParameter;
			DynamicallyAccessedMemberTypes = dynamicallyAccessedMemberTypes;
		}

		public GenericParameter GenericParameter { get; }

		public override bool Equals (ValueNode? other)
		{
			if (!base.Equals (other))
				return false;

			var otherValue = (SystemTypeForGenericParameterValue) other;
			return this.GenericParameter == otherValue.GenericParameter && this.DynamicallyAccessedMemberTypes == otherValue.DynamicallyAccessedMemberTypes;
		}

		public override int GetHashCode ()
		{
			return HashCode.Combine (Kind, GenericParameter, DynamicallyAccessedMemberTypes);
		}

		protected override string NodeToString ()
		{
			return ValueNodeDump.ValueNodeToString (this, GenericParameter, DynamicallyAccessedMemberTypes);
		}
	}

	/// <summary>
	/// This is the System.RuntimeTypeHandle equivalent to a <see cref="SystemTypeForGenericParameterValue"/> node.
	/// </summary>
	class RuntimeTypeHandleForGenericParameterValue : LeafValueNode
	{
		public RuntimeTypeHandleForGenericParameterValue (GenericParameter genericParameter)
		{
			Kind = ValueNodeKind.RuntimeTypeHandleForGenericParameter;

			// Should be System.RuntimeTypeHandle, but we don't have a use case for it
			StaticType = null;

			GenericParameter = genericParameter;
		}

		public GenericParameter GenericParameter { get; }

		public override bool Equals (ValueNode? other)
		{
			if (!base.Equals (other))
				return false;

			return Equals (this.GenericParameter, ((RuntimeTypeHandleForGenericParameterValue) other).GenericParameter);
		}

		public override int GetHashCode ()
		{
			return HashCode.Combine (Kind, GenericParameter);
		}

		protected override string NodeToString ()
		{
			return ValueNodeDump.ValueNodeToString (this, GenericParameter);
		}
	}

	/// <summary>
	/// This is the System.RuntimeMethodHandle equivalent to a <see cref="SystemReflectionMethodBaseValue"/> node.
	/// </summary>
	class RuntimeMethodHandleValue : LeafValueNode
	{
		public RuntimeMethodHandleValue (MethodDefinition methodRepresented)
		{
			Kind = ValueNodeKind.RuntimeMethodHandle;

			// Should be System.RuntimeMethodHandle, but we don't have a use case for it
			StaticType = null;

			MethodRepresented = methodRepresented;
		}

		public MethodDefinition MethodRepresented { get; }

		public override bool Equals (ValueNode? other)
		{
			if (!base.Equals (other))
				return false;

			return Equals (this.MethodRepresented, ((RuntimeMethodHandleValue) other).MethodRepresented);
		}

		public override int GetHashCode ()
		{
			return HashCode.Combine (Kind, MethodRepresented);
		}

		protected override string NodeToString ()
		{
			return ValueNodeDump.ValueNodeToString (this, MethodRepresented);
		}
	}

	/// <summary>
	/// This is a known System.Reflection.MethodBase value.  MethodRepresented is the 'value' of the MethodBase.
	/// </summary>
	class SystemReflectionMethodBaseValue : LeafValueNode
	{
		public SystemReflectionMethodBaseValue (MethodDefinition methodRepresented)
		{
			Kind = ValueNodeKind.SystemReflectionMethodBase;

			// Should be System.Reflection.MethodBase, but we don't have a use case for it
			StaticType = null;

			MethodRepresented = methodRepresented;
		}

		public MethodDefinition MethodRepresented { get; private set; }

		public override bool Equals (ValueNode? other)
		{
			if (!base.Equals (other))
				return false;

			return Equals (this.MethodRepresented, ((SystemReflectionMethodBaseValue) other).MethodRepresented);
		}

		public override int GetHashCode ()
		{
			return HashCode.Combine (Kind, MethodRepresented);
		}

		protected override string NodeToString ()
		{
			return ValueNodeDump.ValueNodeToString (this, MethodRepresented);
		}
	}

	/// <summary>
	/// A known string - such as the result of a ldstr.
	/// </summary>
	class KnownStringValue : LeafValueNode
	{
		public KnownStringValue (string contents)
		{
			Kind = ValueNodeKind.KnownString;

			// Should be System.String, but we don't have a use case for it
			StaticType = null;

			Contents = contents;
		}

		public string Contents { get; private set; }

		public override bool Equals (ValueNode? other)
		{
			if (!base.Equals (other))
				return false;

			return this.Contents == ((KnownStringValue) other).Contents;
		}

		public override int GetHashCode ()
		{
			return HashCode.Combine (Kind, Contents);
		}

		protected override string NodeToString ()
		{
			return ValueNodeDump.ValueNodeToString (this, "\"" + Contents + "\"");
		}
	}

	/// <summary>
	/// Base class for all nodes which can have dynamically accessed member annotation.
	/// </summary>
	abstract class LeafValueWithDynamicallyAccessedMemberNode : LeafValueNode
	{
		public LeafValueWithDynamicallyAccessedMemberNode (IMetadataTokenProvider sourceContext)
		{
			SourceContext = sourceContext;
		}

		public IMetadataTokenProvider SourceContext { get; private set; }

		/// <summary>
		/// The bitfield of dynamically accessed member types the node guarantees
		/// </summary>
		public DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes { get; protected set; }

		public override bool Equals (ValueNode? other)
		{
			if (!base.Equals (other))
				return false;

			var otherValue = (LeafValueWithDynamicallyAccessedMemberNode) other;
			return SourceContext == otherValue.SourceContext
				&& DynamicallyAccessedMemberTypes == otherValue.DynamicallyAccessedMemberTypes;
		}
	}

	/// <summary>
	/// A value that came from a method parameter - such as the result of a ldarg.
	/// </summary>
	class MethodParameterValue : LeafValueWithDynamicallyAccessedMemberNode
	{
		public MethodParameterValue (TypeDefinition? staticType, int parameterIndex, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes, IMetadataTokenProvider sourceContext)
			: base (sourceContext)
		{
			Kind = ValueNodeKind.MethodParameter;
			StaticType = staticType;
			ParameterIndex = parameterIndex;
			DynamicallyAccessedMemberTypes = dynamicallyAccessedMemberTypes;
		}

		public int ParameterIndex { get; }

		public override bool Equals (ValueNode? other)
		{
			if (!base.Equals (other))
				return false;

			var otherValue = (MethodParameterValue) other;
			return this.ParameterIndex == otherValue.ParameterIndex;
		}

		public override int GetHashCode ()
		{
			return HashCode.Combine (Kind, ParameterIndex, DynamicallyAccessedMemberTypes);
		}

		protected override string NodeToString ()
		{
			return ValueNodeDump.ValueNodeToString (this, ParameterIndex, DynamicallyAccessedMemberTypes);
		}
	}

	/// <summary>
	/// String with a known annotation.
	/// </summary>
	class AnnotatedStringValue : LeafValueWithDynamicallyAccessedMemberNode
	{
		public AnnotatedStringValue (IMetadataTokenProvider sourceContext, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
			: base (sourceContext)
		{
			Kind = ValueNodeKind.AnnotatedString;

			// Should be System.String, but we don't have a use case for it
			StaticType = null;

			DynamicallyAccessedMemberTypes = dynamicallyAccessedMemberTypes;
		}

		public override bool Equals (ValueNode? other)
		{
			return base.Equals (other);
		}

		public override int GetHashCode ()
		{
			return HashCode.Combine (Kind, DynamicallyAccessedMemberTypes);
		}

		protected override string NodeToString ()
		{
			return ValueNodeDump.ValueNodeToString (this, DynamicallyAccessedMemberTypes);
		}
	}

	/// <summary>
	/// Return value from a method
	/// </summary>
	class MethodReturnValue : LeafValueWithDynamicallyAccessedMemberNode
	{
		public MethodReturnValue (TypeDefinition? staticType, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes, IMetadataTokenProvider sourceContext)
			: base (sourceContext)
		{
			Kind = ValueNodeKind.MethodReturn;
			StaticType = staticType;
			DynamicallyAccessedMemberTypes = dynamicallyAccessedMemberTypes;
		}

		public override bool Equals (ValueNode? other)
		{
			return base.Equals (other);
		}

		public override int GetHashCode ()
		{
			return HashCode.Combine (Kind, DynamicallyAccessedMemberTypes);
		}

		protected override string NodeToString ()
		{
			return ValueNodeDump.ValueNodeToString (this, DynamicallyAccessedMemberTypes);
		}
	}

	/// <summary>
	/// A merge point commonly occurs due to control flow in a method body.  It represents a set of values
	/// from different paths through the method.  It is the reason for EvaluateUniqueValues, which essentially
	/// provides an enumeration over all the concrete values represented by a given ValueNode after 'erasing'
	/// the merge point nodes.
	/// </summary>
	class MergePointValue : ValueNode
	{
		private MergePointValue (ValueNode one, ValueNode two)
		{
			Kind = ValueNodeKind.MergePoint;
			StaticType = null;
			m_values = new ValueNodeHashSet ();

			if (one.Kind == ValueNodeKind.MergePoint) {
				MergePointValue mpvOne = (MergePointValue) one;
				foreach (ValueNode value in mpvOne.Values)
					m_values.Add (value);
			} else
				m_values.Add (one);

			if (two.Kind == ValueNodeKind.MergePoint) {
				MergePointValue mpvTwo = (MergePointValue) two;
				foreach (ValueNode value in mpvTwo.Values)
					m_values.Add (value);
			} else
				m_values.Add (two);
		}

		public MergePointValue ()
		{
			Kind = ValueNodeKind.MergePoint;
			m_values = new ValueNodeHashSet ();
		}

		public void AddValue (ValueNode node)
		{
			// we are mutating our state, so we must invalidate any cached knowledge
			//InvalidateIsOpen ();

			if (node.Kind == ValueNodeKind.MergePoint) {
				foreach (ValueNode value in ((MergePointValue) node).Values)
					m_values.Add (value);
			} else
				m_values.Add (node);

#if false
			if (this.DetectCycle(new HashSet<ValueNode>()))
			{
				throw new Exception("Found a cycle");
			}
#endif
		}

		readonly ValueNodeHashSet m_values;

		public ValueNodeHashSet Values { get { return m_values; } }

		protected override int NumChildren { get { return Values.Count; } }
		protected override ValueNode ChildAt (int index)
		{
			if (index < NumChildren)
				return Values.ElementAt (index);
			throw new InvalidOperationException ();
		}

		public static ValueNode? MergeValues (ValueNode? one, ValueNode? two)
		{
			if (one == null)
				return two;
			else if (two == null)
				return one;
			else if (one.Equals (two))
				return one;
			else
				return new MergePointValue (one, two);
		}

		protected override IEnumerable<ValueNode> EvaluateUniqueValues ()
		{
			foreach (ValueNode value in Values) {
				foreach (ValueNode uniqueValue in value.UniqueValuesInternal) {
					yield return uniqueValue;
				}
			}
		}

		public override bool Equals (ValueNode? other)
		{
			if (!base.Equals (other))
				return false;

			MergePointValue otherMpv = (MergePointValue) other;
			if (this.Values.Count != otherMpv.Values.Count)
				return false;

			foreach (ValueNode value in this.Values) {
				if (!otherMpv.Values.Contains (value))
					return false;
			}
			return true;
		}

		public override int GetHashCode ()
		{
			return HashCode.Combine (Kind, Values);
		}

		protected override string NodeToString ()
		{
			return ValueNodeDump.ValueNodeToString (this);
		}
	}

	delegate TypeDefinition TypeResolver (string assemblyString, string typeString);

	/// <summary>
	/// The result of a Type.GetType.
	/// AssemblyIdentity is the scope in which to resolve if the type name string is not assembly-qualified.
	/// </summary>

#pragma warning disable CA1812 // GetTypeFromStringValue is never instantiated
	class GetTypeFromStringValue : ValueNode
	{
		private readonly TypeResolver _resolver;

		public GetTypeFromStringValue (TypeResolver resolver, ValueNode assemblyIdentity, ValueNode nameString)
		{
			_resolver = resolver;
			Kind = ValueNodeKind.GetTypeFromString;

			// Should be System.Type, but we don't have a use case for it
			StaticType = null;

			AssemblyIdentity = assemblyIdentity;
			NameString = nameString;
		}

		public ValueNode AssemblyIdentity { get; private set; }

		public ValueNode NameString { get; private set; }

		protected override int NumChildren { get { return 2; } }
		protected override ValueNode ChildAt (int index)
		{
			if (index == 0) return AssemblyIdentity;
			if (index == 1) return NameString;
			throw new InvalidOperationException ();
		}

		protected override IEnumerable<ValueNode> EvaluateUniqueValues ()
		{
			HashSet<string>? names = null;

			foreach (ValueNode nameStringValue in NameString.UniqueValuesInternal) {
				if (nameStringValue.Kind == ValueNodeKind.KnownString) {
					if (names == null) {
						names = new HashSet<string> ();
					}

					string typeName = ((KnownStringValue) nameStringValue).Contents;
					names.Add (typeName);
				}
			}

			bool foundAtLeastOne = false;

			if (names != null) {
				foreach (ValueNode assemblyValue in AssemblyIdentity.UniqueValuesInternal) {
					if (assemblyValue.Kind == ValueNodeKind.KnownString) {
						string assemblyName = ((KnownStringValue) assemblyValue).Contents;

						foreach (string name in names) {
							TypeDefinition typeDefinition = _resolver (assemblyName, name);
							if (typeDefinition != null) {
								foundAtLeastOne = true;
								yield return new SystemTypeValue (typeDefinition);
							}
						}
					}
				}
			}

			if (!foundAtLeastOne)
				yield return UnknownValue.Instance;
		}

		public override bool Equals (ValueNode? other)
		{
			if (!base.Equals (other))
				return false;

			GetTypeFromStringValue otherGtfs = (GetTypeFromStringValue) other;

			return this.AssemblyIdentity.Equals (otherGtfs.AssemblyIdentity) &&
				this.NameString.Equals (otherGtfs.NameString);
		}

		public override int GetHashCode ()
		{
			return HashCode.Combine (Kind, AssemblyIdentity, NameString);
		}

		protected override string NodeToString ()
		{
			return ValueNodeDump.ValueNodeToString (this, NameString);
		}
	}

	/// <summary>
	/// A representation of a ldfld.  Note that we don't have a representation of objects containing fields
	/// so there isn't much that can be done with this node type yet.
	/// </summary>
	class LoadFieldValue : LeafValueWithDynamicallyAccessedMemberNode
	{
		public LoadFieldValue (TypeDefinition? staticType, FieldDefinition fieldToLoad, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
			: base (fieldToLoad)
		{
			Kind = ValueNodeKind.LoadField;
			StaticType = staticType;
			Field = fieldToLoad;
			DynamicallyAccessedMemberTypes = dynamicallyAccessedMemberTypes;
		}

		public FieldDefinition Field { get; private set; }

		public override bool Equals (ValueNode? other)
		{
			if (!base.Equals (other))
				return false;

			LoadFieldValue otherLfv = (LoadFieldValue) other;
			return Equals (this.Field, otherLfv.Field);
		}

		public override int GetHashCode ()
		{
			return HashCode.Combine (Kind, Field, DynamicallyAccessedMemberTypes);
		}

		protected override string NodeToString ()
		{
			return ValueNodeDump.ValueNodeToString (this, Field, DynamicallyAccessedMemberTypes);
		}
	}

	/// <summary>
	/// Represents a ldc on an int32.
	/// </summary>
	class ConstIntValue : LeafValueNode
	{
		public ConstIntValue (int value)
		{
			Kind = ValueNodeKind.ConstInt;

			// Should be System.Int32, but we don't have a usecase for it right now
			StaticType = null;

			Value = value;
		}

		public int Value { get; private set; }

		public override int GetHashCode ()
		{
			return HashCode.Combine (Kind, Value);
		}

		public override bool Equals (ValueNode? other)
		{
			if (!base.Equals (other))
				return false;

			ConstIntValue otherCiv = (ConstIntValue) other;
			return Value == otherCiv.Value;
		}

		protected override string NodeToString ()
		{
			return ValueNodeDump.ValueNodeToString (this, Value);
		}
	}

	class ArrayValue : ValueNode
	{
		protected override int NumChildren => 1 + IndexValues.Count;

		/// <summary>
		/// Constructs an array value of the given size
		/// </summary>
		public ArrayValue (ValueNode? size, TypeReference elementType)
		{
			Kind = ValueNodeKind.Array;

			// Should be System.Array (or similar), but we don't have a use case for it
			StaticType = null;

			Size = size ?? UnknownValue.Instance;
			ElementType = elementType;
			IndexValues = new Dictionary<int, ValueBasicBlockPair> ();
		}

		private ArrayValue (ValueNode size, TypeReference elementType, Dictionary<int, ValueBasicBlockPair> indexValues)
			: this (size, elementType)
		{
			IndexValues = indexValues;
		}

		public ValueNode Size { get; }
		public TypeReference ElementType { get; }
		public Dictionary<int, ValueBasicBlockPair> IndexValues { get; }

		public override int GetHashCode ()
		{
			return HashCode.Combine (Kind, Size);
		}

		public override bool Equals (ValueNode? other)
		{
			if (!base.Equals (other))
				return false;

			ArrayValue otherArr = (ArrayValue) other;
			bool equals = Size.Equals (otherArr.Size);
			equals &= IndexValues.Count == otherArr.IndexValues.Count;
			if (!equals)
				return false;

			// If both sets T and O are the same size and "T intersect O" is empty, then T == O.
			HashSet<KeyValuePair<int, ValueBasicBlockPair>> thisValueSet = new (IndexValues);
			HashSet<KeyValuePair<int, ValueBasicBlockPair>> otherValueSet = new (otherArr.IndexValues);
			thisValueSet.ExceptWith (otherValueSet);
			return thisValueSet.Count == 0;
		}

		protected override string NodeToString ()
		{
			// TODO: Use StringBuilder and remove Linq usage.
			return $"(Array Size:{ValueNodeDump.ValueNodeToString (this, Size)}, Values:({string.Join (',', IndexValues.Select (v => $"({v.Key},{ValueNodeDump.ValueNodeToString (v.Value.Value)})"))})";
		}

		protected override IEnumerable<ValueNode> EvaluateUniqueValues ()
		{
			foreach (var sizeConst in Size.UniqueValuesInternal)
				yield return new ArrayValue (sizeConst, ElementType, IndexValues);
		}

		protected override ValueNode ChildAt (int index)
		{
			if (index == 0) return Size;
			if (index - 1 <= IndexValues.Count)
				return IndexValues.Values.ElementAt (index - 1).Value!;

			throw new InvalidOperationException ();
		}
	}

	#region ValueNode Collections
	public class ValueNodeList : List<ValueNode?>
	{
		public ValueNodeList ()
		{
		}

		public ValueNodeList (int capacity)
			: base (capacity)
		{
		}

		public ValueNodeList (List<ValueNode> other)
			: base (other)
		{
		}

		public override int GetHashCode ()
		{
			return HashUtils.CalcHashCodeEnumerable (this);
		}

		public override bool Equals (object? other)
		{
			if (!(other is ValueNodeList otherList))
				return false;

			if (otherList.Count != Count)
				return false;

			for (int i = 0; i < Count; i++) {
				if (!(otherList[i]?.Equals (this[i]) ?? (this[i] is null)))
					return false;
			}
			return true;
		}
	}

	class ValueNodeHashSet : HashSet<ValueNode>
	{
		public override int GetHashCode ()
		{
			return HashUtils.CalcHashCodeEnumerable (this);
		}

		public override bool Equals (object? other)
		{
			if (!(other is ValueNodeHashSet otherSet))
				return false;

			if (otherSet.Count != Count)
				return false;

			IEnumerator<ValueNode> thisEnumerator = this.GetEnumerator ();
			IEnumerator<ValueNode> otherEnumerator = otherSet.GetEnumerator ();

			for (int i = 0; i < Count; i++) {
				thisEnumerator.MoveNext ();
				otherEnumerator.MoveNext ();
				if (!thisEnumerator.Current.Equals (otherEnumerator.Current))
					return false;
			}
			return true;
		}
	}
	#endregion

	static class HashUtils
	{
		public static int CalcHashCodeEnumerable<T> (IEnumerable<T> list) where T : class?
		{
			HashCode hashCode = new HashCode ();
			foreach (var item in list)
				hashCode.Add (item);
			return hashCode.ToHashCode ();
		}
	}

	public struct ValueBasicBlockPair
	{
		public ValueNode? Value;
		public int BasicBlockIndex;
	}
}
