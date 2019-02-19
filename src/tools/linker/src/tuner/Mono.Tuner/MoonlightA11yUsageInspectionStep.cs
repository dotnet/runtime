//
// MoonlightA11yUsageInspectionStep.cs
//
// Author:
//   Andrés G. Aragoneses (aaragoneses@novell.com)
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

	public class MoonlightA11yUsageInspectionStep : MoonlightAssemblyStep {

		protected override void CustomizePipeline (Pipeline pipeline)
		{
			pipeline.ReplaceStep (typeof (MarkStep), new MoonlightA11yApiMarker ());
			pipeline.ReplaceStep (typeof (SweepStep), new MoonlightA11yDescriptorGenerator ());
			pipeline.RemoveStep (typeof (LoadI18nAssemblies));
			pipeline.RemoveStep (typeof (CleanStep));
			pipeline.RemoveStep (typeof (RegenerateGuidStep));
			pipeline.RemoveStep (typeof (OutputStep));
		}
	}
}
