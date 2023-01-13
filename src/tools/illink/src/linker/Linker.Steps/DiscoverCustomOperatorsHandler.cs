// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using ILLink.Shared.TypeSystemProxy;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public class DiscoverOperatorsHandler : IMarkHandler
	{
		LinkContext? _context;
		LinkContext Context {
			get {
				Debug.Assert (_context != null);
				return _context;
			}
		}

		bool _seenLinqExpressions;
		readonly HashSet<TypeDefinition> _trackedTypesWithOperators;
		Dictionary<TypeDefinition, List<MethodDefinition>>? _pendingOperatorsForType;

		Dictionary<TypeDefinition, List<MethodDefinition>> PendingOperatorsForType {
			get {
				_pendingOperatorsForType ??= new Dictionary<TypeDefinition, List<MethodDefinition>> ();
				return _pendingOperatorsForType;
			}
		}

		public DiscoverOperatorsHandler ()
		{
			_trackedTypesWithOperators = new HashSet<TypeDefinition> ();
		}

		public void Initialize (LinkContext context, MarkContext markContext)
		{
			_context = context;
			markContext.RegisterMarkTypeAction (ProcessType);
		}

		void ProcessType (TypeDefinition type)
		{
			CheckForLinqExpressions (type);

			// Check for custom operators and either:
			// - mark them, if Linq.Expressions was already marked, or
			// - track them to be marked in case Linq.Expressions is marked later
			var hasOperators = ProcessCustomOperators (type, mark: _seenLinqExpressions);
			if (!_seenLinqExpressions) {
				if (hasOperators)
					_trackedTypesWithOperators.Add (type);
				return;
			}

			// Mark pending operators defined on other types that reference this type
			// (these are only tracked if we have already seen Linq.Expressions)
			if (PendingOperatorsForType.TryGetValue (type, out var pendingOperators)) {
				foreach (var customOperator in pendingOperators)
					MarkOperator (customOperator);
				PendingOperatorsForType.Remove (type);
			}
		}

		void CheckForLinqExpressions (TypeDefinition type)
		{
			if (_seenLinqExpressions)
				return;

			if (type.Namespace != "System.Linq.Expressions" || type.Name != "Expression")
				return;

			_seenLinqExpressions = true;

			foreach (var markedType in _trackedTypesWithOperators)
				ProcessCustomOperators (markedType, mark: true);

			_trackedTypesWithOperators.Clear ();
		}

		void MarkOperator (MethodDefinition method)
		{
			Context.Annotations.Mark (method, new DependencyInfo (DependencyKind.PreservedOperator, method.DeclaringType), new MessageOrigin (method.DeclaringType));
		}

		bool ProcessCustomOperators (TypeDefinition type, bool mark)
		{
			if (!type.HasMethods)
				return false;

			bool hasCustomOperators = false;
			foreach (var method in type.Methods) {
				if (!IsOperator (method, out var otherType))
					continue;

				if (!mark)
					return true;

				Debug.Assert (_seenLinqExpressions);
				hasCustomOperators = true;

				if (otherType == null || Context.Annotations.IsMarked (otherType)) {
					MarkOperator (method);
					continue;
				}

				// Wait until otherType gets marked to mark the operator.
				if (!PendingOperatorsForType.TryGetValue (otherType, out var pendingOperators)) {
					pendingOperators = new List<MethodDefinition> ();
					PendingOperatorsForType.Add (otherType, pendingOperators);
				}
				pendingOperators.Add (method);
			}
			return hasCustomOperators;
		}

		TypeDefinition? _nullableOfT;
		TypeDefinition? NullableOfT {
			get {
				_nullableOfT ??= BCL.FindPredefinedType (WellKnownType.System_Nullable_T, Context);
				return _nullableOfT;
			}
		}

		TypeDefinition? NonNullableType (TypeReference type)
		{
			var typeDef = Context.TryResolve (type);
			if (typeDef == null)
				return null;

			if (!typeDef.IsValueType || typeDef != NullableOfT)
				return typeDef;

			// Unwrap Nullable<T>
			Debug.Assert (typeDef.HasGenericParameters);
			// The original type reference might be a TypeSpecification like array of Nullable<T>
			// that we need to unwrap until we get to the Nullable<T>
			while (!type.IsGenericInstance)
				type = ((TypeSpecification) type).ElementType;
			var nullableType = type as GenericInstanceType;
			Debug.Assert (nullableType != null && nullableType.HasGenericArguments && nullableType.GenericArguments.Count == 1);
			return Context.TryResolve (nullableType.GenericArguments[0]);
		}

		bool IsOperator (MethodDefinition method, out TypeDefinition? otherType)
		{
			otherType = null;

			if (!method.IsStatic || !method.IsPublic || !method.IsSpecialName || !method.Name.StartsWith ("op_"))
				return false;

			var operatorName = method.Name.Substring (3);
			var self = method.DeclaringType;

			switch (operatorName) {
			// Unary operators
			case "UnaryPlus":
			case "UnaryNegation":
			case "LogicalNot":
			case "OnesComplement":
			case "Increment":
			case "Decrement":
			case "True":
			case "False":
				// Parameter type of a unary operator must be the declaring type
				if (method.GetMetadataParametersCount () != 1 || NonNullableType (method.GetParameter ((ParameterIndex) 0).ParameterType) != self)
					return false;
				// ++ and -- must return the declaring type
				if (operatorName is "Increment" or "Decrement" && NonNullableType (method.ReturnType) != self)
					return false;
				return true;
			// Binary operators
			case "Addition":
			case "Subtraction":
			case "Multiply":
			case "Division":
			case "Modulus":
			case "BitwiseAnd":
			case "BitwiseOr":
			case "ExclusiveOr":
			case "LeftShift":
			case "RightShift":
			case "Equality":
			case "Inequality":
			case "LessThan":
			case "GreaterThan":
			case "LessThanOrEqual":
			case "GreaterThanOrEqual":
				if (method.GetMetadataParametersCount () != 2)
					return false;
				var nnLeft = NonNullableType (method.GetParameter ((ParameterIndex) 0).ParameterType);
				var nnRight = NonNullableType (method.GetParameter ((ParameterIndex) 1).ParameterType);
				if (nnLeft == null || nnRight == null)
					return false;
				// << and >> must take the declaring type and int
				if (operatorName is "LeftShift" or "RightShift" && (nnLeft != self || nnRight.MetadataType != MetadataType.Int32))
					return false;
				// At least one argument must be the declaring type
				if (nnLeft != self && nnRight != self)
					return false;
				if (nnLeft != self)
					otherType = nnLeft;
				if (nnRight != self)
					otherType = nnRight;
				return true;
			// Conversion operators
			case "Implicit":
			case "Explicit":
				if (method.GetMetadataParametersCount () != 1)
					return false;
				var nnSource = NonNullableType (method.GetParameter ((ParameterIndex) 0).ParameterType);
				var nnTarget = NonNullableType (method.ReturnType);
				// Exactly one of source/target must be the declaring type
				if (nnSource == self == (nnTarget == self))
					return false;
				otherType = nnSource == self ? nnTarget : nnSource;
				return true;
			default:
				return false;
			}
		}
	}
}
