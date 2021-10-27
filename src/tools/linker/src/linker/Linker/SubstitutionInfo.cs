// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Mono.Cecil;

namespace Mono.Linker
{
	public class SubstitutionInfo
	{
		public Dictionary<MethodDefinition, MethodAction> MethodActions { get; }
		public Dictionary<MethodDefinition, object?> MethodStubValues { get; }
		public Dictionary<FieldDefinition, object?> FieldValues { get; }
		public HashSet<FieldDefinition> FieldInit { get; }

		public SubstitutionInfo ()
		{
			MethodActions = new Dictionary<MethodDefinition, MethodAction> ();
			MethodStubValues = new Dictionary<MethodDefinition, object?> ();
			FieldValues = new Dictionary<FieldDefinition, object?> ();
			FieldInit = new HashSet<FieldDefinition> ();
		}

		public void SetMethodAction (MethodDefinition method, MethodAction action)
		{
			MethodActions[method] = action;
		}

		public void SetMethodStubValue (MethodDefinition method, object? value)
		{
			MethodStubValues[method] = value;
		}

		public void SetFieldValue (FieldDefinition field, object? value)
		{
			FieldValues[field] = value;
		}

		public void SetFieldInit (FieldDefinition field)
		{
			FieldInit.Add (field);
		}
	}
}