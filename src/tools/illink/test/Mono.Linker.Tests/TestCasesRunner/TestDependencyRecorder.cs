// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public class TestDependencyRecorder : IDependencyRecorder
	{
		public record struct Dependency
		{
			public string Source;
			public string Target;
			public bool Marked;
			public string DependencyKind;
		}

		public List<Dependency> Dependencies = new List<Dependency> ();

		public void RecordDependency (object source, object target, bool marked)
		{
			Dependencies.Add (new Dependency () {
				Source = source.ToString (),
				Target = target.ToString (),
				Marked = marked
			});
		}

		public void RecordDependency (object target, in DependencyInfo reason, bool marked)
		{
			Dependencies.Add (new Dependency () {
				Source = reason.Source?.ToString (),
				Target = target.ToString (),
				Marked = marked,
				DependencyKind = reason.Kind.ToString ()
			});
		}

		public void FinishRecording ()
		{

		}
	}
}
