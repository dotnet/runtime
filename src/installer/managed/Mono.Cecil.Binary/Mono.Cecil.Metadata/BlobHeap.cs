//
// BlobHeap.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2005 Jb Evain
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

namespace Mono.Cecil.Metadata {

	using System;
	using System.Collections;
	using System.IO;

	internal class BlobHeap : MetadataHeap {

		internal BlobHeap (MetadataStream stream) : base (stream, MetadataStream.Blob)
		{
		}

		public byte [] Read (uint index)
		{
			return ReadBytesFromStream (index);
		}

		public BinaryReader GetReader (uint index)
		{
			return new BinaryReader (new MemoryStream (Read (index)));
		}

		public override void Accept (IMetadataVisitor visitor)
		{
			visitor.VisitBlobHeap (this);
		}
	}

	class ByteArrayEqualityComparer : IHashCodeProvider, IComparer {

		public static readonly ByteArrayEqualityComparer Instance = new ByteArrayEqualityComparer ();

		public int GetHashCode (object obj)
		{
			byte [] array = (byte []) obj;

			int hash = 0;
			for (int i = 0; i < array.Length; i++)
				hash = (hash * 37) ^ array [i];

			return hash;
		}

		public int Compare (object a, object b)
		{
			byte [] x = (byte []) a;
			byte [] y = (byte []) b;

			if (x == null || y == null)
				return x == y ? 0 : 1;

			if (x.Length != y.Length)
				return 1;

			for (int i = 0; i < x.Length; i++)
				if (x [i] != y [i])
					return 1;

			return 0;
		}
	}
}
