//
// MoonlightAssemblyStep.cs
//
// Author:
//   Jb Evain (jbevain@novell.com)
//
// (C) 2009 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using System.IO;

using Mono.Linker;
using Mono.Linker.Steps;

using Mono.Cecil;

namespace Mono.Tuner {

	public class MoonlightAssemblyStep : IStep {

		public void Process (LinkContext context)
		{
			CustomizePipeline (context.Pipeline);
			ProcessAssemblies (context);
		}

		static void ProcessAssemblies (LinkContext context)
		{
			foreach (AssemblyDefinition assembly in context.GetAssemblies ())
				context.Annotations.SetAction (assembly, AssemblyAction.Link);
		}

		protected virtual void CustomizePipeline (Pipeline pipeline)
		{
			pipeline.RemoveStep (typeof (LoadI18nAssemblies));
			pipeline.RemoveStep (typeof (BlacklistStep));
			pipeline.RemoveStep (typeof (TypeMapStep));
			pipeline.RemoveStep (typeof (MarkStep));
			pipeline.RemoveStep (typeof (SweepStep));
			pipeline.RemoveStep (typeof (CleanStep));
			pipeline.RemoveStep (typeof (RegenerateGuidStep));
			pipeline.AddStepBefore (typeof (OutputStep), new InjectSecurityAttributes ());
		}
	}
}
