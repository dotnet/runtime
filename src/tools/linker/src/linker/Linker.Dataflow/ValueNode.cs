// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using ILLink.Shared.DataFlow;
using Mono.Cecil;
using FieldDefinition = Mono.Cecil.FieldDefinition;
using GenericParameter = Mono.Cecil.GenericParameter;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<Mono.Linker.Dataflow.ValueNode>;
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
		public static bool DetectCycle (this ValueNode node, HashSet<ValueNode> seenNodes, HashSet<ValueNode>? allNodesSeen)
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

			case ValueNodeKind.Array:
				ArrayValue av = (ArrayValue) node;
				foundCycle = av.Size.DetectCycle (seenNodes, allNodesSeen);
				foreach (ValueBasicBlockPair pair in av.IndexValues.Values) {
					foreach (var v in pair.Value) {
						foundCycle |= v.DetectCycle (seenNodes, allNodesSeen);
					}
				}
				break;

			default:
				throw new Exception (String.Format ("Unknown node kind: {0}", node.Kind));
			}
			seenNodes.Remove (node);

			return foundCycle;
		}

		public static int? AsConstInt (this ValueNode node)
		{
			if (node is ConstIntValue constInt)
				return constInt.Value;

			return null;
		}

		public static int? AsConstInt (this in MultiValue value)
		{
			if (value.AsSingleValue () is ConstIntValue constInt)
				return constInt.Value;

			return null;
		}

		public static ValueNode? AsSingleValue (this in MultiValue node)
		{
			if (node.Count () != 1)
				return null;

			return node.Single ();
		}
	}

	internal static class ValueNodeDump
	{
		internal static string ValueNodeToString (ValueNode node, params object[] args)
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
		static ValueSetLattice<ValueNode> MultiValueLattice => default;

		public static MultiValue Create (MultiValue size, TypeReference elementType)
		{
			MultiValue result = MultiValueLattice.Top;
			foreach (var sizeValue in size) {
				result = MultiValueLattice.Meet (result, new MultiValue (new ArrayValue (sizeValue, elementType)));
			}

			return result;
		}

		public static MultiValue Create (int size, TypeReference elementType)
		{
			return new MultiValue (new ArrayValue (new ConstIntValue (size), elementType));
		}

		/// <summary>
		/// Constructs an array value of the given size
		/// </summary>
		private ArrayValue (ValueNode size, TypeReference elementType)
		{
			Kind = ValueNodeKind.Array;

			// Should be System.Array (or similar), but we don't have a use case for it
			StaticType = null;

			Size = size;
			ElementType = elementType;
			IndexValues = new Dictionary<int, ValueBasicBlockPair> ();
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
			StringBuilder result = new ();
			result.Append ("Array Size:");
			result.Append (ValueNodeDump.ValueNodeToString (this, Size));

			result.Append (", Values:(");
			bool first = true;
			foreach (var element in IndexValues) {
				if (!first) {
					result.Append (",");
					first = false;
				}

				result.Append ("(");
				result.Append (element.Key);
				result.Append (",(");
				bool firstValue = true;
				foreach (var v in element.Value.Value) {
					if (firstValue) {
						result.Append (",");
						firstValue = false;
					}

					result.Append (ValueNodeDump.ValueNodeToString (v));
				}
				result.Append ("))");
			}
			result.Append (')');

			return result.ToString ();
		}
	}

	#region ValueNode Collections
	public class ValueNodeList : List<MultiValue>
	{
		public ValueNodeList ()
		{
		}

		public ValueNodeList (int capacity)
			: base (capacity)
		{
		}

		public ValueNodeList (List<MultiValue> other)
			: base (other)
		{
		}

		public override int GetHashCode ()
		{
			HashCode hashCode = new HashCode ();
			foreach (var item in this)
				hashCode.Add (item.GetHashCode ());
			return hashCode.ToHashCode ();
		}

		public override bool Equals (object? other)
		{
			if (!(other is ValueNodeList otherList))
				return false;

			if (otherList.Count != Count)
				return false;

			for (int i = 0; i < Count; i++) {
				if (!otherList[i].Equals (this[i]))
					return false;
			}
			return true;
		}
	}
	#endregion


	public struct ValueBasicBlockPair
	{
		public MultiValue Value;
		public int BasicBlockIndex;
	}
}
