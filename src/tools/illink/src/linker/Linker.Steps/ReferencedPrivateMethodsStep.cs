// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using ILLink.Shared.TypeSystemProxy;

namespace Mono.Linker.Steps
{
	// This step searches the CIL of each method in each class and finds instances
	// of LDFTN opcode. In each class, the methods referenced in this way are gathered
	// and marked with ReferencedPrivateMethodAttribute afterwards. This is to facilitate
	// generation of direct calls for certain private methods when doing AOT with LLVM.
	// If a method's pointer is taken, it can be passed to another assembly and called
	// from there. Any attempt to make that call direct is doomed and therefore the
	// involved methods must be marked.
	public class ReferencedPrivateMethodsStep : BaseStep
	{
		public ReferencedPrivateMethodsStep ()
		{
		}

		//private const string CtorName = ".ctor";
		//private const string RpmaNamespace = "Mono.Linker.Attributes";
		//private const string RpmaName = "ReferencedPrivateMethodAttribute";

		// [System.AttributeUsageAttribute(System.AttributeTargets.Method, AllowMultiple=false, Inherited=false)]
		/*private static readonly byte[] AttrUsageBlob = new byte[] {
			0x01, 0x00, 0x40, 0x00, 0x00, 0x00, 0x02, 0x00, 0x54, 0x02, 0x0d, 0x41, 0x6c, 0x6c, 0x6f, 0x77,
			0x4d, 0x75, 0x6c, 0x74, 0x69, 0x70, 0x6c, 0x65, 0x00, 0x54, 0x02, 0x09, 0x49, 0x6e, 0x68, 0x65,
			0x72, 0x69, 0x74, 0x65, 0x64, 0x00};*/

		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			Console.WriteLine(string.Format("=== {0}", assembly.Name));

			HashSet<MethodDefinition> referencedMethods = new HashSet<MethodDefinition>();
			foreach (var type in assembly.MainModule.Types) {
				foreach (MethodDefinition method in type.Methods)
					FindReferencedMethods (method, referencedMethods);
			}

			if (referencedMethods.Count > 0) {
				MethodReference? rpmaAttrCtor = EnsureRpmaAttribute (assembly);
				if (rpmaAttrCtor is null)
					throw new ApplicationException("Failed to locate or create ReferencedPrivateMethodAttribute:.ctor.");

				foreach (MethodDefinition method in referencedMethods)
				{
					Console.WriteLine(string.Format("--- {0}::{1} is bonkers.",
						method.DeclaringType.Name, method.Name));

					AnnotateMethod (method, rpmaAttrCtor);
				}
			}
		}

		protected override void EndProcess ()
		{
		}

		void FindReferencedMethods (MethodDefinition method, HashSet<MethodDefinition> refd)
		{
			MethodBody? body = method.Body;
			if (body is null)
				return;

			Collection<Instruction> instructions = Context.GetMethodIL(body).Instructions;

			foreach(Instruction i in instructions) {
				if (i.OpCode.Code == Code.Ldftn && i.Operand is MethodReference ldftnMethod) {
					MethodDefinition? def = Context.TryResolve(ldftnMethod);

					if(def is not null)
						refd.Add(def);
				}
			}
		}

		void AnnotateMethod (MethodDefinition method, MethodReference attrCtor)
		{
			CustomAttribute ca = new CustomAttribute(attrCtor);
			method.CustomAttributes.Add(ca);
			Annotations.Mark (ca, new DependencyInfo (DependencyKind.CustomAttribute, ca));
		}

		private const string RpmaNamespace = "System.Runtime";
		private const string RpmaAttrName = "ReferencedPrivateMethodAttribute";

		MethodReference EnsureRpmaAttribute (AssemblyDefinition assembly)
		{
			ModuleDefinition targetModule = assembly.MainModule;
			AssemblyDefinition? coreLibAssy = Context.Resolve (targetModule.TypeSystem.CoreLibrary);
			if (coreLibAssy is null)
				throw new InvalidOperationException();

			ModuleDefinition corelibMainModule = coreLibAssy.MainModule;
			TypeReference rpmaAttributeRef = new TypeReference (RpmaNamespace, RpmaAttrName, corelibMainModule, targetModule.TypeSystem.CoreLibrary);
			TypeDefinition? rpmaAttributeDef = corelibMainModule.MetadataResolver.Resolve (rpmaAttributeRef);
			MethodDefinition? rpmaAttributeCtor = null;

			TypeReference systemAttributeRef = new TypeReference ("System", "Attribute", corelibMainModule, targetModule.TypeSystem.CoreLibrary);
			TypeReference systemAttribute = corelibMainModule.MetadataResolver.Resolve (systemAttributeRef);
			systemAttribute = corelibMainModule.ImportReference (systemAttribute);

			if (rpmaAttributeDef is null) {
				if (systemAttribute is null)
					throw new System.ApplicationException ("System.Attribute is not found in " + targetModule.TypeSystem.CoreLibrary.Name);

				MethodReference systemAttributeCtorRef = new MethodReference (".ctor", corelibMainModule.TypeSystem.Void, systemAttributeRef);
				MethodReference systemAttributeCtor = corelibMainModule.MetadataResolver.Resolve (systemAttributeCtorRef);
				systemAttributeCtor = corelibMainModule.ImportReference (systemAttributeCtor);

				if (systemAttributeCtor is null)
					throw new System.ApplicationException ("System.Attribute has no default constructor");

				rpmaAttributeDef = new TypeDefinition (RpmaNamespace, RpmaAttrName, TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit, systemAttribute);
				coreLibAssy.MainModule.Types.Add (rpmaAttributeDef);

				if (Annotations.GetAction (coreLibAssy) == AssemblyAction.Copy) {
					Annotations.SetAction (coreLibAssy, AssemblyAction.Save);
				}

				const MethodAttributes ctorAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
				rpmaAttributeCtor = new MethodDefinition (".ctor", ctorAttributes, coreLibAssy.MainModule.TypeSystem.Void);
#pragma warning disable RS0030 // Anything after MarkStep should use Cecil directly as all method bodies should be processed by this point
				var instructions = rpmaAttributeCtor.Body.Instructions;
#pragma warning restore RS0030
				instructions.Add (Instruction.Create (OpCodes.Ldarg_0));
				instructions.Add (Instruction.Create (OpCodes.Call, systemAttributeCtor));
				instructions.Add (Instruction.Create (OpCodes.Ret));

				rpmaAttributeDef.Methods.Add (rpmaAttributeCtor);

			} else {
				foreach (MethodDefinition method in rpmaAttributeDef.Methods) {
					if (method.IsConstructor && !method.IsStatic && !method.HasMetadataParameters ()) {
						rpmaAttributeCtor = method;
						break;
					}
				}

				if (rpmaAttributeCtor == null)
					throw new System.ApplicationException ("System.Runtime.ReferencedPrivateMethodAttribute has no default constructor");
			}

			return targetModule.ImportReference (rpmaAttributeCtor);
		}

		/*
		void EmitRpmaAttribute (AssemblyDefinition assy)
		{
			TypeDefinition baseType = BCL.FindPredefinedType(WellKnownType.System_Attribute, Context) ??
				throw new InvalidOperationException("Cannot find type System.Attribute in BCL.");

			MethodReference attrCtor = FindMethodStrict(baseType, CtorName);
			attrCtor = assy.MainModule.ImportReference (attrCtor);

			TypeDefinition attrUsageType = BCL.FindPredefinedType(WellKnownType.System_AttributeUsageAttribute, Context) ??
				throw new InvalidOperationException("Cannot find type System.AttributeUsageAttribute in BCL.");

			MethodReference attrUsageCtor = FindMethodStrict(attrUsageType, CtorName);
			attrUsageCtor = assy.MainModule.ImportReference (attrUsageCtor);

			string typeNamespace = RpmaNamespace;// + "." + assy.Name.Name.Replace(".", "_");

			TypeDefinition type = new TypeDefinition(typeNamespace, RpmaName,
				TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit | TypeAttributes.Public,
				baseType);
			type.CustomAttributes.Add(new CustomAttribute(attrUsageCtor, AttrUsageBlob));

			MethodDefinition ctor = new MethodDefinition(CtorName,
				MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.RTSpecialName,
				type);

			type.Methods.Add(ctor);

			MethodBody ctorBody = ctor.Body;
			ctorBody.MaxStackSize = 8;

			LinkerILProcessor processor = ctorBody.GetLinkerILProcessor();
			processor.Append(Instruction.Create(OpCodes.Ldarg_0));
			processor.Append(Instruction.Create(OpCodes.Call, attrCtor));
			processor.Append(Instruction.Create(OpCodes.Ret));
			ctor.Body = ctorBody;

			ModuleDefinition mod = assy.MainModule;
			mod.Types.Add(type);

			rpmaCtor = ctor;
		}

		static MethodReference FindMethodStrict(TypeDefinition type, string name)
		{
			MethodReference? method = type.Methods.FirstOrDefault((x) => x.Name == name);
			if (method is null)
				throw new InvalidOperationException(string.Format("Could not find {0}:{1} in BCL.", type.Name, name));

			return method;
		}*/
	}
}
