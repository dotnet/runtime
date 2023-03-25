// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

using ILCompiler;
using Internal.TypeSystem;

namespace Mono.Linker.Tests.TestCasesRunner
{

	/// <summary>
	/// Represents a non-leaf multifile compilation group where types contained in the group are always fully expanded.
	/// </summary>
	public class TestInfraMultiFileSharedCompilationModuleGroup : MultiFileCompilationModuleGroup
	{
		public TestInfraMultiFileSharedCompilationModuleGroup (CompilerTypeSystemContext context, IEnumerable<ModuleDesc> compilationModuleSet)
			: base (context, compilationModuleSet)
		{
		}

		public override bool ShouldProduceFullVTable (TypeDesc type)
		{
			return false;
		}

		public override bool ShouldPromoteToFullType (TypeDesc type)
		{
			return ShouldProduceFullVTable (type);
		}

		public override bool PresenceOfEETypeImpliesAllMethodsOnType (TypeDesc type)
		{
			return (type.HasInstantiation || type.IsArray) && ShouldProduceFullVTable (type) &&
				   type.ConvertToCanonForm (CanonicalFormKind.Specific).IsCanonicalSubtype (CanonicalFormKind.Any);
		}

		public override bool AllowInstanceMethodOptimization (MethodDesc method)
		{
			// Both the instance methods and the owning type are homed in a single compilation group
			// so if we're able to generate the body, we would also generate the owning type here
			// and nowhere else.
			if (ContainsMethodBody (method, unboxingStub: false)) {
				TypeDesc owningType = method.OwningType;
				return owningType.IsDefType && !owningType.HasInstantiation && !method.HasInstantiation;
			}
			return false;
		}

		public override bool AllowVirtualMethodOnAbstractTypeOptimization (MethodDesc method)
		{
			// Not really safe to do this since we need to assume IgnoreAccessChecks
			// and we wouldn't know all derived types when compiling methods on the type
			// that introduces this method.
			return false;
		}
	}
}
