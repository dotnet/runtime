﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mono.Linker.Dataflow
{
	static class ScannerExtensions
	{
		public static bool IsControlFlowInstruction (in this OpCode opcode)
		{
			return opcode.FlowControl == FlowControl.Branch
				|| opcode.FlowControl == FlowControl.Cond_Branch
				|| (opcode.FlowControl == FlowControl.Return && opcode.Code != Code.Ret);
		}

		public static HashSet<int> ComputeBranchTargets (this MethodBody methodBody)
		{
			HashSet<int> branchTargets = new HashSet<int> ();
			foreach (Instruction operation in methodBody.Instructions) {
				if (!operation.OpCode.IsControlFlowInstruction ())
					continue;
				Object value = operation.Operand;
				if (value is Instruction inst) {
					branchTargets.Add (inst.Offset);
				} else if (value is Instruction[] instructions) {
					foreach (Instruction switchLabel in instructions) {
						branchTargets.Add (switchLabel.Offset);
					}
				}
			}
			foreach (ExceptionHandler einfo in methodBody.ExceptionHandlers) {
				if (einfo.HandlerType == ExceptionHandlerType.Filter) {
					branchTargets.Add (einfo.FilterStart.Offset);
				}
				branchTargets.Add (einfo.HandlerStart.Offset);
			}
			return branchTargets;
		}

		public static bool IsByRefOrPointer (this TypeReference typeRef)
		{
			return typeRef.IsByReference || typeRef.IsPointer;
		}
	}

}
