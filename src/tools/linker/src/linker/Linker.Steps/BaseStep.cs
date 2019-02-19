//
// BaseStep.cs
//
// Author:
//   Jb Evain (jbevain@novell.com)
//
// (C) 2007 Novell, Inc.
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

using Mono.Cecil;

namespace Mono.Linker.Steps {

	public abstract class BaseStep : IStep {

		private LinkContext _context;

		public LinkContext Context {
			get { return _context; }
		}

		public AnnotationStore Annotations {
			get { return _context.Annotations; }
		}

		public Tracer Tracer {
			get { return _context.Tracer; }
		}

		public MarkingHelpers MarkingHelpers => _context.MarkingHelpers;

		public void Process (LinkContext context)
		{
			_context = context;

			if (!ConditionToProcess ())
				return;

			Process ();

			foreach (AssemblyDefinition assembly in context.GetAssemblies ())
				ProcessAssembly (assembly);

			EndProcess ();
		}

		protected virtual bool ConditionToProcess ()
		{
			return true;
		}

		protected virtual void Process ()
		{
		}

		protected virtual void EndProcess ()
		{
		}

		protected virtual void ProcessAssembly (AssemblyDefinition assembly)
		{
		}
	}
}
