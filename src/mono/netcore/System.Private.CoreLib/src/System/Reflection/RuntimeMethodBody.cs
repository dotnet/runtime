// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Reflection {

	internal sealed class RuntimeMethodBody : MethodBody
	{
		ExceptionHandlingClause[] clauses;
		LocalVariableInfo[] locals;
		byte[] il;
		bool init_locals;
		int sig_token;
		int max_stack;

		// Called by the runtime
		internal RuntimeMethodBody (ExceptionHandlingClause[] clauses, LocalVariableInfo[] locals,
									byte[] il, bool init_locals, int sig_token, int max_stack)
		{
			this.clauses = clauses;
			this.locals = locals;
			this.il = il;
			this.init_locals = init_locals;
			this.sig_token = sig_token;
			this.max_stack = max_stack;
		}

        public override int LocalSignatureMetadataToken => sig_token;
        public override IList<LocalVariableInfo> LocalVariables => Array.AsReadOnly (locals);
        public override int MaxStackSize => max_stack;
        public override bool InitLocals => init_locals;
        public override byte[] GetILAsByteArray() => il;
        public override IList<ExceptionHandlingClause> ExceptionHandlingClauses => Array.AsReadOnly (clauses);
	}

}


