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
	/// <summary>
	/// A ValueNode represents a value in the IL dataflow analysis.  It may not contain complete information as it is a
	/// best-effort representation.  Additionally, as the analysis is linear and does not account for control flow, any
	/// given ValueNode may represent multiple values simultaneously.  (This occurs, for example, at control flow join
	/// points when both paths yield values on the IL stack or in a local.)
	/// </summary>
	public abstract record ValueNode
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
		/// The IL type of the value, represented as closely as possible, but not always exact.  It can be null, for
		/// example, when the analysis is imprecise or operating on malformed IL.
		/// </summary>
		public TypeDefinition? StaticType { get; init; }

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
	}

	/// <summary>
	/// LeafValueNode represents a 'leaf' in the expression tree.  In other words, the node has no ValueNode children.
	/// It *may* still have non-ValueNode 'properties' that are interesting.  This class serves, primarily, as a way to
	/// collect up the very common implmentation of NumChildren/ChildAt for leaf nodes and the "represents exactly one
	/// value" optimization.  These things aren't on the ValueNode base class because, otherwise, new node types
	/// deriving from ValueNode may 'forget' to implement these things.  So this class allows them to remain abstract in
	/// ValueNode while still having a common implementation for all the leaf nodes.
	/// </summary>
	public abstract record LeafValueNode : ValueNode
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
			switch (node) {
			//
			// Leaf nodes
			//
			case UnknownValue:
			case NullValue:
			case SystemTypeValue:
			case RuntimeTypeHandleValue:
			case KnownStringValue:
			case AnnotatedStringValue:
			case ConstIntValue:
			case MethodParameterValue:
			case MethodReturnValue:
			case SystemTypeForGenericParameterValue:
			case RuntimeTypeHandleForGenericParameterValue:
			case SystemReflectionMethodBaseValue:
			case RuntimeMethodHandleValue:
			case LoadFieldValue:
				break;

			//
			// Nodes with children
			//
			case ArrayValue:
				ArrayValue av = (ArrayValue) node;
				foundCycle = av.Size.DetectCycle (seenNodes, allNodesSeen);
				foreach (ValueBasicBlockPair pair in av.IndexValues.Values) {
					foreach (var v in pair.Value) {
						foundCycle |= v.DetectCycle (seenNodes, allNodesSeen);
					}
				}
				break;

			default:
				throw new Exception (String.Format ("Unknown node type: {0}", node.GetType ().Name));
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
			sb.Append (node.GetType ().Name);
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
	record UnknownValue : LeafValueNode
	{
		private UnknownValue ()
		{
			StaticType = null;
		}

		public static UnknownValue Instance { get; } = new UnknownValue ();

		protected override string NodeToString ()
		{
			return ValueNodeDump.ValueNodeToString (this);
		}
	}

	record NullValue : LeafValueNode
	{
		private NullValue ()
		{
			StaticType = null;
		}

		public static NullValue Instance { get; } = new NullValue ();

		protected override string NodeToString ()
		{
			return ValueNodeDump.ValueNodeToString (this);
		}
	}

	/// <summary>
	/// This is a known System.Type value.  TypeRepresented is the 'value' of the System.Type.
	/// </summary>
	record SystemTypeValue : LeafValueNode
	{
		public SystemTypeValue (TypeDefinition typeRepresented)
		{
			// Should be System.Type - but we don't have any use case where tracking it like that would matter
			StaticType = null;

			TypeRepresented = typeRepresented;
		}

		public TypeDefinition TypeRepresented { get; }

		protected override string NodeToString ()
		{
			return ValueNodeDump.ValueNodeToString (this, TypeRepresented);
		}
	}

	/// <summary>
	/// This is the System.RuntimeTypeHandle equivalent to a <see cref="SystemTypeValue"/> node.
	/// </summary>
	record RuntimeTypeHandleValue : LeafValueNode
	{
		public RuntimeTypeHandleValue (TypeDefinition typeRepresented)
		{
			// Should be System.RuntimeTypeHandle, but we don't have a use case for it like that
			StaticType = null;

			TypeRepresented = typeRepresented;
		}

		public TypeDefinition TypeRepresented { get; }

		protected override string NodeToString ()
		{
			return ValueNodeDump.ValueNodeToString (this, TypeRepresented);
		}
	}

	/// <summary>
	/// This is a System.Type value which represents generic parameter (basically result of typeof(T))
	/// Its actual type is unknown, but it can have annotations.
	/// </summary>
	record SystemTypeForGenericParameterValue : LeafValueWithDynamicallyAccessedMemberNode
	{
		public SystemTypeForGenericParameterValue (GenericParameter genericParameter, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
			: base (genericParameter, dynamicallyAccessedMemberTypes)
		{
			// Should be System.Type, but we don't have a use case for it
			StaticType = null;

			GenericParameter = genericParameter;
		}

		public GenericParameter GenericParameter { get; }

		protected override string NodeToString ()
		{
			return ValueNodeDump.ValueNodeToString (this, GenericParameter, DynamicallyAccessedMemberTypes);
		}
	}

	/// <summary>
	/// This is the System.RuntimeTypeHandle equivalent to a <see cref="SystemTypeForGenericParameterValue"/> node.
	/// </summary>
	record RuntimeTypeHandleForGenericParameterValue : LeafValueNode
	{
		public RuntimeTypeHandleForGenericParameterValue (GenericParameter genericParameter)
		{
			// Should be System.RuntimeTypeHandle, but we don't have a use case for it
			StaticType = null;

			GenericParameter = genericParameter;
		}

		public GenericParameter GenericParameter { get; }

		protected override string NodeToString ()
		{
			return ValueNodeDump.ValueNodeToString (this, GenericParameter);
		}
	}

	/// <summary>
	/// This is the System.RuntimeMethodHandle equivalent to a <see cref="SystemReflectionMethodBaseValue"/> node.
	/// </summary>
	record RuntimeMethodHandleValue : LeafValueNode
	{
		public RuntimeMethodHandleValue (MethodDefinition methodRepresented)
		{
			// Should be System.RuntimeMethodHandle, but we don't have a use case for it
			StaticType = null;

			MethodRepresented = methodRepresented;
		}

		public MethodDefinition MethodRepresented { get; }

		protected override string NodeToString ()
		{
			return ValueNodeDump.ValueNodeToString (this, MethodRepresented);
		}
	}

	/// <summary>
	/// This is a known System.Reflection.MethodBase value.  MethodRepresented is the 'value' of the MethodBase.
	/// </summary>
	record SystemReflectionMethodBaseValue : LeafValueNode
	{
		public SystemReflectionMethodBaseValue (MethodDefinition methodRepresented)
		{
			// Should be System.Reflection.MethodBase, but we don't have a use case for it
			StaticType = null;

			MethodRepresented = methodRepresented;
		}

		public MethodDefinition MethodRepresented { get; }

		protected override string NodeToString ()
		{
			return ValueNodeDump.ValueNodeToString (this, MethodRepresented);
		}
	}

	/// <summary>
	/// A known string - such as the result of a ldstr.
	/// </summary>
	record KnownStringValue : LeafValueNode
	{
		public KnownStringValue (string contents)
		{
			// Should be System.String, but we don't have a use case for it
			StaticType = null;

			Contents = contents;
		}

		public string Contents { get; private set; }

		protected override string NodeToString ()
		{
			return ValueNodeDump.ValueNodeToString (this, "\"" + Contents + "\"");
		}
	}

	/// <summary>
	/// Base class for all nodes which can have dynamically accessed member annotation.
	/// </summary>
	abstract record LeafValueWithDynamicallyAccessedMemberNode : LeafValueNode
	{
		public LeafValueWithDynamicallyAccessedMemberNode (IMetadataTokenProvider sourceContext, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
		{
			SourceContext = sourceContext;
			DynamicallyAccessedMemberTypes = dynamicallyAccessedMemberTypes;
		}

		public IMetadataTokenProvider SourceContext { get; }

		/// <summary>
		/// The bitfield of dynamically accessed member types the node guarantees
		/// </summary>
		public DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes { get; }
	}

	/// <summary>
	/// A value that came from a method parameter - such as the result of a ldarg.
	/// </summary>
	record MethodParameterValue : LeafValueWithDynamicallyAccessedMemberNode
	{
		public MethodParameterValue (TypeDefinition? staticType, int parameterIndex, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes, IMetadataTokenProvider sourceContext)
			: base (sourceContext, dynamicallyAccessedMemberTypes)
		{
			StaticType = staticType;
			ParameterIndex = parameterIndex;
		}

		public int ParameterIndex { get; }

		protected override string NodeToString ()
		{
			return ValueNodeDump.ValueNodeToString (this, ParameterIndex, DynamicallyAccessedMemberTypes);
		}
	}

	/// <summary>
	/// String with a known annotation.
	/// </summary>
	record AnnotatedStringValue : LeafValueWithDynamicallyAccessedMemberNode
	{
		public AnnotatedStringValue (IMetadataTokenProvider sourceContext, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
			: base (sourceContext, dynamicallyAccessedMemberTypes)
		{
			// Should be System.String, but we don't have a use case for it
			StaticType = null;
		}

		protected override string NodeToString ()
		{
			return ValueNodeDump.ValueNodeToString (this, DynamicallyAccessedMemberTypes);
		}
	}

	/// <summary>
	/// Return value from a method
	/// </summary>
	record MethodReturnValue : LeafValueWithDynamicallyAccessedMemberNode
	{
		public MethodReturnValue (TypeDefinition? staticType, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes, IMetadataTokenProvider sourceContext)
			: base (sourceContext, dynamicallyAccessedMemberTypes)
		{
			StaticType = staticType;
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
	record LoadFieldValue : LeafValueWithDynamicallyAccessedMemberNode
	{
		public LoadFieldValue (TypeDefinition? staticType, FieldDefinition fieldToLoad, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
			: base (fieldToLoad, dynamicallyAccessedMemberTypes)
		{
			StaticType = staticType;
			Field = fieldToLoad;
		}

		public FieldDefinition Field { get; }

		protected override string NodeToString ()
		{
			return ValueNodeDump.ValueNodeToString (this, Field, DynamicallyAccessedMemberTypes);
		}
	}

	/// <summary>
	/// Represents a ldc on an int32.
	/// </summary>
	record ConstIntValue : LeafValueNode
	{
		public ConstIntValue (int value)
		{
			// Should be System.Int32, but we don't have a usecase for it right now
			StaticType = null;

			Value = value;
		}

		public int Value { get; }

		protected override string NodeToString ()
		{
			return ValueNodeDump.ValueNodeToString (this, Value);
		}
	}

	record ArrayValue : ValueNode
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
			return HashCode.Combine (GetType ().GetHashCode (), Size);
		}

		public virtual bool Equals (ArrayValue? otherArr)
		{
			if (otherArr == null)
				return false;

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
