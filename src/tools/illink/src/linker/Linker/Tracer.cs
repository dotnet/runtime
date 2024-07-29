// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//
// Tracer.cs
//
// Author:
//  Radek Doulik <radou@microsoft.com>
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

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Mono.Linker
{
	public class Tracer
	{
		protected readonly LinkContext context;

		List<IDependencyRecorder>? recorders;

		public Tracer (LinkContext context)
		{
			this.context = context;
		}

		public void Finish ()
		{
			if (recorders != null) {
				foreach (var recorder in recorders) {
					recorder.FinishRecording ();
					if (recorder is IDisposable disposableRecorder)
						disposableRecorder.Dispose ();
				}
			}

			recorders = null;
		}

		public void AddRecorder (IDependencyRecorder recorder)
		{
			recorders ??= new List<IDependencyRecorder> ();

			recorders.Add (recorder);
		}

		[MemberNotNullWhen (true, nameof(recorders))]
		bool IsRecordingEnabled ()
		{
			return recorders != null;
		}

		public void AddDirectDependency (object target, in DependencyInfo reason, bool marked)
		{
			if (IsRecordingEnabled ()) {
				foreach (IDependencyRecorder recorder in recorders)
					recorder.RecordDependency (target, reason, marked);
			}
		}
	}
}
