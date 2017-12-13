//
// AssemblyAction.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2006 Jb Evain
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

namespace Mono.Linker {

	public enum AssemblyAction {
		// Ignore the assembly
		Skip,
		// Copy the existing files, assembly and symbols, into the output destination. E.g. .dll and .mdb
		// The linker still analyzes the assemblies (to know what they require) but does not modify them.
		Copy,
		// Copy the existing files, assembly and symbols, into the output destination if and only if
		// anything from the assembly is used.
		// The linker still analyzes the assemblies (to know what they require) but does not modify them.
		CopyUsed,
		// Link the assembly
		Link,
		// Remove the assembly from the output
		Delete,
		// Save the assembly/symbols in memory without linking it. 
		// E.g. useful to remove unneeded assembly references (as done in SweepStep), 
		//  resolving [TypeForwardedTo] attributes (like PCL) to their final location
		Save,
		// Keep all types, methods, and fields but add System.Runtime.BypassNGenAttribute to unmarked methods.
		AddBypassNGen,
		// Keep all types, methods, and fields in marked assemblies but add System.Runtime.BypassNGenAttribute to unmarked methods.
		// Delete unmarked assemblies.
		AddBypassNGenUsed
	}
}
