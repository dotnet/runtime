//
// IDependencyRecorder.cs
//
// Copyright (C) 2017 Microsoft Corporation (http://www.microsoft.com)
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

namespace Mono.Linker
{
	/// <summary>
	/// Abstraction exposed by the linker (mostly MarkStep, but not only) - it will call this interface
	/// every time it finds a dependency between two parts of the dependency graph.
	/// </summary>
	public interface IDependencyRecorder
	{
		/// <summary>
		/// Reports a dependency detected by the linker.
		/// </summary>
		/// <param name="source">The source of the dependency (for example the caller method).</param>
		/// <param name="target">The target of the dependency (for example the callee method).</param>
		/// <param name="marked">true if the target is also marked by the MarkStep.</param>
		/// <remarks>The source and target are typically Cecil metadata objects (MethodDefinition, TypeDefinition, ...)
		/// but they can also be the linker steps or really any other object.</remarks>
		void RecordDependency (object source, object target, bool marked);
	}
}
