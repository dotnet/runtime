// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mono.Linker.Steps
{
	public class CodeRewriterStep : BaseStep
	{
		AssemblyDefinition? assembly;
		AssemblyDefinition Assembly {
			get {
				Debug.Assert (assembly != null);
				return assembly;
			}
		}

		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			if (Annotations.GetAction (assembly) != AssemblyAction.Link)
				return;

			this.assembly = assembly;

			foreach (var type in assembly.MainModule.Types)
				ProcessType (type);
		}

		void ProcessType (TypeDefinition type)
		{
			foreach (var method in type.Methods) {
				if (method.HasBody)
					ProcessMethod (method);
			}

			if (type.HasFields && Annotations.HasSubstitutedInit (type)) {
				AddFieldsInitializations (type);
			}

			foreach (var nested in type.NestedTypes)
				ProcessType (nested);
		}

		void AddFieldsInitializations (TypeDefinition type)
		{
			Instruction ret;
			LinkerILProcessor processor;

			var cctor = type.Methods.FirstOrDefault (MethodDefinitionExtensions.IsStaticConstructor);
			if (cctor == null) {
				type.Attributes |= TypeAttributes.BeforeFieldInit;

				var method = new MethodDefinition (".cctor",
					MethodAttributes.Static | MethodAttributes.Private | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.HideBySig,
					Assembly.MainModule.TypeSystem.Void);

				type.Methods.Add (method);

				processor = method.Body.GetLinkerILProcessor ();
				ret = Instruction.Create (OpCodes.Ret);
				processor.Append (ret);
			} else {
				var body = cctor.Body;
#pragma warning disable RS0030 // After MarkStep all methods should be processed and thus accessing Cecil directly is the right approach
				var instructions = body.Instructions;
#pragma warning restore RS0030
				ret = instructions.Last (l => l.OpCode.Code == Code.Ret);
				processor = body.GetLinkerILProcessor ();

				for (int i = 0; i < instructions.Count; ++i) {
					var instr = instructions[i];
					if (instr.OpCode.Code != Code.Stsfld)
						continue;

					var field = (FieldReference) instr.Operand;
					if (!Annotations.HasSubstitutedInit (field.Resolve ()))
						continue;

					processor.Replace (instr, Instruction.Create (OpCodes.Pop));
				}
			}

			foreach (var field in type.Fields) {
				if (!Annotations.HasSubstitutedInit (field))
					continue;

				Context.Annotations.TryGetFieldUserValue (field, out object? value);

				var valueInstr = CreateConstantResultInstruction (Context, field.FieldType, value);
				if (valueInstr == null)
					throw new NotImplementedException (field.FieldType.ToString ());

				processor.InsertBefore (ret, valueInstr);
				processor.InsertBefore (ret, Instruction.Create (OpCodes.Stsfld, field));
			}
		}

		void ProcessMethod (MethodDefinition method)
		{
			switch (Annotations.GetAction (method)) {
			case MethodAction.ConvertToStub:
				RewriteBodyToStub (method);
				break;
			case MethodAction.ConvertToThrow:
				RewriteBodyToLinkedAway (method);
				break;
			}
		}

		protected virtual void RewriteBodyToLinkedAway (MethodDefinition method)
		{
			method.ImplAttributes &= ~(MethodImplAttributes.AggressiveInlining | MethodImplAttributes.Synchronized);
			method.ImplAttributes |= MethodImplAttributes.NoInlining;

			method.Body = CreateThrowLinkedAwayBody (method);

			method.ClearDebugInformation ();
		}

		protected virtual void RewriteBodyToStub (MethodDefinition method)
		{
			if (!method.IsIL)
				throw new NotImplementedException ();

			method.Body = CreateStubBody (method);

			method.ClearDebugInformation ();
		}

		MethodBody CreateThrowLinkedAwayBody (MethodDefinition method)
		{
			var body = new MethodBody (method);
			var il = body.GetLinkerILProcessor ();
			MethodReference? ctor;

			// Makes the body verifiable
			if (method.IsConstructor && !method.DeclaringType.IsValueType) {
				ctor = Assembly.MainModule.ImportReference (Context.MarkedKnownMembers.ObjectCtor);

				il.Emit (OpCodes.Ldarg_0);
				il.Emit (OpCodes.Call, ctor);
			}

			// import the method into the current assembly
			ctor = Context.MarkedKnownMembers.NotSupportedExceptionCtorString;
			ctor = Assembly.MainModule.ImportReference (ctor);

			il.Emit (OpCodes.Ldstr, "Linked away");
			il.Emit (OpCodes.Newobj, ctor);
			il.Emit (OpCodes.Throw);

			return body;
		}

		MethodBody CreateStubBody (MethodDefinition method)
		{
			var body = new MethodBody (method);

#pragma warning disable RS0030 // MethodReference.Parameters is banned. This code already works and doesn't need to be changed
			if (method.HasParameters && method.Parameters.Any (l => l.IsOut))
				throw new NotSupportedException ($"Cannot replace body of method '{method.GetDisplayName ()}' because it has an out parameter.");
#pragma warning restore RS0030

			var il = body.GetLinkerILProcessor ();
			if (method.IsInstanceConstructor () && !method.DeclaringType.IsValueType) {
				var baseType = Context.Resolve (method.DeclaringType.BaseType);
				if (baseType is null)
					return body;

				MethodReference base_ctor = baseType.GetDefaultInstanceConstructor (Context);
				if (base_ctor == null)
					throw new NotSupportedException ($"Cannot replace constructor for '{method.DeclaringType}' when no base default constructor exists");

				base_ctor = Assembly.MainModule.ImportReference (base_ctor);

				il.Emit (OpCodes.Ldarg_0);
				il.Emit (OpCodes.Call, base_ctor);
			}

			switch (method.ReturnType.MetadataType) {
			case MetadataType.Void:
				break;
			default:
				var instruction = CreateConstantResultInstruction (Context, method);
				if (instruction != null) {
					il.Append (instruction);
				} else {
					StubComplexBody (method, body, il);
				}
				break;
			}

			il.Emit (OpCodes.Ret);
			return body;
		}

		static void StubComplexBody (MethodDefinition method, MethodBody body, LinkerILProcessor il)
		{
			switch (method.ReturnType.MetadataType) {
			case MetadataType.MVar:
			case MetadataType.ValueType:
				var vd = new VariableDefinition (method.ReturnType);
#pragma warning disable RS0030 // Anything after MarkStep should not use ILProvider since all methods are guaranteed processed
				body.Variables.Add (vd);
#pragma warning restore RS0030
				body.InitLocals = true;

				il.Emit (OpCodes.Ldloca_S, vd);
				il.Emit (OpCodes.Initobj, method.ReturnType);
				il.Emit (OpCodes.Ldloc_0);
				return;
			case MetadataType.Pointer:
			case MetadataType.IntPtr:
			case MetadataType.UIntPtr:
				il.Emit (OpCodes.Ldc_I4_0);
				il.Emit (OpCodes.Conv_I);
				return;
			}

			throw new NotImplementedException (method.FullName);
		}

		public static Instruction? CreateConstantResultInstruction (LinkContext context, MethodDefinition method)
		{
			context.Annotations.TryGetMethodStubValue (method, out object? value);
			return CreateConstantResultInstruction (context, method.ReturnType, value);
		}

		public static Instruction? CreateConstantResultInstruction (LinkContext context, TypeReference inputRtype, object? value = null)
		{
			TypeReference? rtype = inputRtype;
			switch (rtype.MetadataType) {
			case MetadataType.ValueType:
				var definition = context.TryResolve (rtype);
				if (definition?.IsEnum == true) {
					rtype = definition.GetEnumUnderlyingType ();
				}

				break;
			case MetadataType.GenericInstance:
				rtype = context.TryResolve (rtype);
				break;
			}

			if (rtype == null)
				return null;

			switch (rtype.MetadataType) {
			case MetadataType.Boolean:
				if (value is int bintValue && bintValue == 1)
					return Instruction.Create (OpCodes.Ldc_I4_1);

				return Instruction.Create (OpCodes.Ldc_I4_0);

			case MetadataType.String:
				if (value is string svalue)
					return Instruction.Create (OpCodes.Ldstr, svalue);

				return Instruction.Create (OpCodes.Ldnull);

			case MetadataType.Object:
			case MetadataType.Array:
			case MetadataType.Class:
				Debug.Assert (value == null);
				return Instruction.Create (OpCodes.Ldnull);

			case MetadataType.Double:
				if (value is double dvalue)
					return Instruction.Create (OpCodes.Ldc_R8, dvalue);

				Debug.Assert (value == null);
				return Instruction.Create (OpCodes.Ldc_R8, 0.0);

			case MetadataType.Single:
				if (value is float fvalue)
					return Instruction.Create (OpCodes.Ldc_R4, fvalue);

				Debug.Assert (value == null);
				return Instruction.Create (OpCodes.Ldc_R4, 0.0f);

			case MetadataType.Char:
			case MetadataType.Byte:
			case MetadataType.SByte:
			case MetadataType.Int16:
			case MetadataType.UInt16:
			case MetadataType.Int32:
			case MetadataType.UInt32:
				if (value is int intValue)
					return Instruction.Create (OpCodes.Ldc_I4, intValue);

				Debug.Assert (value == null);
				return Instruction.Create (OpCodes.Ldc_I4_0);

			case MetadataType.UInt64:
			case MetadataType.Int64:
				if (value is long longValue)
					return Instruction.Create (OpCodes.Ldc_I8, longValue);

				Debug.Assert (value == null);
				return Instruction.Create (OpCodes.Ldc_I8, 0L);
			}

			return null;
		}
	}
}
