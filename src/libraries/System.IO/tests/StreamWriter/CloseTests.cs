// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;

namespace StreamWriterTests
{
	public class CloseTests
	{
		[Fact]
		public static void AfterDisposeThrows()
		{
			StreamWriter sw2;

			// [] Calling methods after closing the stream should throw
			//-----------------------------------------------------------------
			sw2 = new StreamWriter(new MemoryStream());
			sw2.Dispose();

			Assert.Throws<ObjectDisposedException>(() => sw2.Write('A'));
			Assert.Throws<ObjectDisposedException>(() => sw2.Write("hello"));
			Assert.Throws<ObjectDisposedException>(() => sw2.Flush());
			Assert.Null(sw2.BaseStream);

			Assert.Throws<ObjectDisposedException>(() => sw2.AutoFlush = true);
		}

		[Fact]
		public static void CloseCausesFlush() {
		 StreamWriter sw2;
			MemoryStream memstr2;

			// [] Check that flush updates the underlying stream
			//-----------------------------------------------------------------
			memstr2 = new MemoryStream();
			sw2 = new StreamWriter(memstr2);

			var strTemp = "HelloWorld" ;
			sw2.Write( strTemp);
			Assert.Equal(0, memstr2.Length);

			sw2.Flush();
			Assert.Equal(strTemp.Length, memstr2.Length);
		}
		[Fact]
		public static void CantFlushAfterDispose() {
			// [] Flushing closed writer should throw
			//-----------------------------------------------------------------
			
			MemoryStream memstr2 = new MemoryStream();
			StreamWriter sw2 = new StreamWriter(memstr2);
			
			sw2.Dispose();
			Assert.Throws<ObjectDisposedException>(() => sw2.Flush());
		}
	}
}
