//
// DocumentLanguage.cs
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

namespace Mono.Cecil.Cil {

	using System;

	internal abstract class DocumentLanguage {
#if CF_2_0
		public static readonly Guid None = new Guid ("00000000-0000-0000-0000-000000000000");
		public static readonly Guid C = new Guid ("63a08714-fc37-11d2-904c-00c04fa302a1");
		public static readonly Guid Cpp = new Guid ("3a12d0b7-c26c-11d0-b442-00a0244a1dd2");
		public static readonly Guid CSharp = new Guid ("3f5162f8-07c6-11d3-9053-00c04fa302a1");
		public static readonly Guid Basic = new Guid ("3a12d0b8-c26c-11d0-b442-00a0244a1dd2");
		public static readonly Guid Java = new Guid ("3a12d0b4-c26c-11d0-b442-00a0244a1dd2");
		public static readonly Guid Cobol = new Guid ("af046cd1-d0e1-11d2-977c-00a0c9b4d50c");
		public static readonly Guid Pascal = new Guid ("af046cd2-d0e1-11d2-977c-00a0c9b4d50c");
		public static readonly Guid CIL = new Guid ("af046cd3-d0e1-11d2-977c-00a0c9b4d50c");
		public static readonly Guid JScript = new Guid ("3a12d0b6-c26c-11d0-b442-00a0244a1dd2");
		public static readonly Guid SMC = new Guid ("0d9b9f7b-6611-11d3-bd2a-0000f80849bd");
		public static readonly Guid MCpp = new Guid ("4b35fde8-07c6-11d3-9053-00c04fa302a1");
#else
		public static readonly Guid None = new Guid (0x00000000, 0x0000, 0x0000, 0x00, 0x00, 0x0, 0x00, 0x00, 0x00, 0x00, 0x00);
		public static readonly Guid C = new Guid (0x63a08714, 0xfc37, 0x11d2, 0x90, 0x4c, 0x0, 0xc0, 0x4f, 0xa3, 0x02, 0xa1);
		public static readonly Guid Cpp = new Guid (0x3a12d0b7, 0xc26c, 0x11d0, 0xb4, 0x42, 0x0, 0xa0, 0x24, 0x4a, 0x1d, 0xd2);
		public static readonly Guid CSharp = new Guid (0x3f5162f8, 0x07c6, 0x11d3, 0x90, 0x53, 0x0, 0xc0, 0x4f, 0xa3, 0x02, 0xa1);
		public static readonly Guid Basic = new Guid (0x3a12d0b8, 0xc26c, 0x11d0, 0xb4, 0x42, 0x0, 0xa0, 0x24, 0x4a, 0x1d, 0xd2);
		public static readonly Guid Java = new Guid (0x3a12d0b4, 0xc26c, 0x11d0, 0xb4, 0x42, 0x0, 0xa0, 0x24, 0x4a, 0x1d, 0xd2);
		public static readonly Guid Cobol = new Guid (0xaf046cd1, 0xd0e1, 0x11d2, 0x97, 0x7c, 0x0, 0xa0, 0xc9, 0xb4, 0xd5, 0xc);
		public static readonly Guid Pascal = new Guid (0xaf046cd2, 0xd0e1, 0x11d2, 0x97, 0x7c, 0x0, 0xa0, 0xc9, 0xb4, 0xd5, 0xc);
		public static readonly Guid CIL = new Guid (0xaf046cd3, 0xd0e1, 0x11d2, 0x97, 0x7c, 0x0, 0xa0, 0xc9, 0xb4, 0xd5, 0xc);
		public static readonly Guid JScript = new Guid (0x3a12d0b6, 0xc26c, 0x11d0, 0xb4, 0x42, 0x0, 0xa0, 0x24, 0x4a, 0x1d, 0xd2);
		public static readonly Guid SMC = new Guid (0xd9b9f7b, 0x6611, 0x11d3, 0xbd, 0x2a, 0x0, 0x0, 0xf8, 0x8, 0x49, 0xbd);
		public static readonly Guid MCpp = new Guid (0x4b35fde8, 0x07c6, 0x11d3, 0x90, 0x53, 0x0, 0xc0, 0x4f, 0xa3, 0x02, 0xa1);
#endif
	}
}
