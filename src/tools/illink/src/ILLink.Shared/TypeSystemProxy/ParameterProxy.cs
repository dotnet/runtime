// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace ILLink.Shared.TypeSystemProxy
{
	internal partial struct ParameterProxy
	{
		public ParameterProxy (MethodProxy method, ParameterIndex index)
		{
			if ((int) index < 0 || (int) index >= method.GetParametersCount ())
				throw new InvalidOperationException ($"Parameter of index {(int) index} does not exist on method {method.GetDisplayName ()} with {method.GetParametersCount ()}");
			Method = method;
			Index = index;
		}

		public MethodProxy Method { get; }

		public ParameterIndex Index { get; }

		/// <summary>
		/// The index of the entry in the '.parameters' metadata section corresponding to this parameter.
		/// Maps to the index of the parameter in Cecil's MethodReference.Parameters or Roslyn's IMethodSymbol.Parameters
		/// Throws if the parameter is the implicit 'this' parameter.
		/// </summary>
		public int MetadataIndex {
			get {
				if (Method.HasImplicitThis ()) {
					if (IsImplicitThis)
						throw new InvalidOperationException ("Cannot get metadata index of the implicit 'this' parameter");
					return (int) Index - 1;
				}
				return (int) Index;
			}
		}

		public partial ReferenceKind GetReferenceKind ();

		public partial string GetDisplayName ();

		public bool IsImplicitThis => Method.HasImplicitThis () && Index == (ParameterIndex) 0;

		public partial bool IsTypeOf (string typeName);

		public IEnumerable<string> GetDiagnosticArgumentsForAnnotationMismatch ()
			=> IsImplicitThis ?
				new string[] { Method.GetDisplayName () }

				: new string[] { GetDisplayName (), Method.GetDisplayName () };
	}
}
