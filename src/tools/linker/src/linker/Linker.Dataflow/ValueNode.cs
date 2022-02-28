// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using ILLink.Shared.DataFlow;
using Mono.Cecil;
using Mono.Linker;
using Mono.Linker.Dataflow;
using FieldDefinition = Mono.Cecil.FieldDefinition;
using GenericParameter = Mono.Cecil.GenericParameter;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;
using TypeDefinition = Mono.Cecil.TypeDefinition;

namespace ILLink.Shared.TrimAnalysis
{
	// These are extension methods because we want to allow the use of them on null 'this' pointers.
	internal static class SingleValueExtensions
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
		public static bool DetectCycle (this SingleValue node, HashSet<SingleValue> seenNodes, HashSet<SingleValue>? allNodesSeen)
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
			case ConstIntValue:
			case MethodParameterValue:
			case MethodThisParameterValue:
			case MethodReturnValue:
			case GenericParameterValue:
			case RuntimeTypeHandleForGenericParameterValue:
			case SystemReflectionMethodBaseValue:
			case RuntimeMethodHandleValue:
			case FieldValue:
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
	}

	/// <summary>
	/// This is a System.Type value which represents generic parameter (basically result of typeof(T))
	/// Its actual type is unknown, but it can have annotations.
	/// </summary>
	partial record GenericParameterValue
	{
		public GenericParameterValue (GenericParameter genericParameter, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
		{
			GenericParameter = new (genericParameter);
			DynamicallyAccessedMemberTypes = dynamicallyAccessedMemberTypes;
		}

		public partial bool HasDefaultConstructorConstraint () => GenericParameter.GenericParameter.HasDefaultConstructorConstraint;

		public override DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes { get; }

		public override IEnumerable<string> GetDiagnosticArgumentsForAnnotationMismatch ()
			=> new string[] { GenericParameter.GenericParameter.Name, DiagnosticUtilities.GetGenericParameterDeclaringMemberDisplayName (GenericParameter.GenericParameter) };

		public override string ToString () => this.ValueToString (GenericParameter, DynamicallyAccessedMemberTypes);
	}

	/// <summary>
	/// This is the System.RuntimeMethodHandle equivalent to a <see cref="SystemReflectionMethodBaseValue"/> node.
	/// </summary>
	partial record RuntimeMethodHandleValue
	{
		public RuntimeMethodHandleValue (MethodDefinition methodRepresented) => MethodRepresented = methodRepresented;

		public readonly MethodDefinition MethodRepresented;

		public override string ToString () => this.ValueToString (MethodRepresented);
	}

	/// <summary>
	/// A value that came from a method parameter - such as the result of a ldarg.
	/// </summary>
	partial record MethodParameterValue : IValueWithStaticType
	{
		public MethodParameterValue (TypeDefinition? staticType, MethodDefinition method, int parameterIndex, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
		{
			StaticType = staticType;
			Method = method;
			ParameterIndex = parameterIndex;
			DynamicallyAccessedMemberTypes = dynamicallyAccessedMemberTypes;
		}

		public readonly MethodDefinition Method;

		/// <summary>
		/// This is the index of non-implicit parameter - so the index into MethodDefinition.Parameters array.
		/// It's NOT the IL parameter index which could be offset by 1 if the method has an implicit this.
		/// </summary>
		public readonly int ParameterIndex;

		public ParameterDefinition ParameterDefinition => Method.Parameters[ParameterIndex];

		public override DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes { get; }

		public override IEnumerable<string> GetDiagnosticArgumentsForAnnotationMismatch ()
			=> new string[] { DiagnosticUtilities.GetParameterNameForErrorMessage (ParameterDefinition), DiagnosticUtilities.GetMethodSignatureDisplayName (Method) };

		public TypeDefinition? StaticType { get; }

		public override string ToString () => this.ValueToString (Method, ParameterIndex, DynamicallyAccessedMemberTypes);
	}

	/// <summary>
	/// A value that came from the implicit this parameter of a method
	/// </summary>
	partial record MethodThisParameterValue : IValueWithStaticType
	{
		public MethodThisParameterValue (MethodDefinition method, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
		{
			Method = method;
			DynamicallyAccessedMemberTypes = dynamicallyAccessedMemberTypes;
		}

		public readonly MethodDefinition Method;

		public override DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes { get; }

		public override IEnumerable<string> GetDiagnosticArgumentsForAnnotationMismatch ()
			=> new string[] { Method.GetDisplayName () };

		public TypeDefinition? StaticType => Method.DeclaringType;

		public override string ToString () => this.ValueToString (Method, DynamicallyAccessedMemberTypes);
	}

	/// <summary>
	/// Return value from a method
	/// </summary>
	partial record MethodReturnValue : IValueWithStaticType
	{
		public MethodReturnValue (TypeDefinition? staticType, MethodDefinition method, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
		{
			StaticType = staticType;
			Method = method;
			DynamicallyAccessedMemberTypes = dynamicallyAccessedMemberTypes;
		}

		public readonly MethodDefinition Method;

		public override DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes { get; }

		public override IEnumerable<string> GetDiagnosticArgumentsForAnnotationMismatch ()
			=> new string[] { DiagnosticUtilities.GetMethodSignatureDisplayName (Method) };

		public TypeDefinition? StaticType { get; }

		public override string ToString () => this.ValueToString (Method, DynamicallyAccessedMemberTypes);
	}

	/// <summary>
	/// A representation of a field. Typically a result of ldfld.
	/// </summary>
	partial record FieldValue : IValueWithStaticType
	{
		public FieldValue (TypeDefinition? staticType, FieldDefinition fieldToLoad, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
		{
			StaticType = staticType;
			Field = fieldToLoad;
			DynamicallyAccessedMemberTypes = dynamicallyAccessedMemberTypes;
		}

		public readonly FieldDefinition Field;

		public override DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes { get; }

		public override IEnumerable<string> GetDiagnosticArgumentsForAnnotationMismatch ()
			=> new string[] { Field.GetDisplayName () };

		public TypeDefinition? StaticType { get; }

		public override string ToString () => this.ValueToString (Field, DynamicallyAccessedMemberTypes);
	}

	partial record ArrayValue
	{
		static ValueSetLattice<SingleValue> MultiValueLattice => default;

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
		ArrayValue (SingleValue size, TypeReference elementType)
		{
			Size = size;
			ElementType = elementType;
			IndexValues = new Dictionary<int, ValueBasicBlockPair> ();
		}

		public TypeReference ElementType { get; }
		public Dictionary<int, ValueBasicBlockPair> IndexValues { get; }

		public partial bool TryGetValueByIndex (int index, out MultiValue value)
		{
			if (IndexValues.TryGetValue (index, out var valuePair)) {
				value = valuePair.Value;
				return true;
			}

			value = default;
			return false;
		}

		public override int GetHashCode ()
		{
			return HashCode.Combine (GetType ().GetHashCode (), Size);
		}

		public bool Equals (ArrayValue? otherArr)
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

		public override string ToString ()
		{
			StringBuilder result = new ();
			result.Append ("Array Size:");
			result.Append (this.ValueToString (Size));

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

					result.Append (v.ToString ());
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
