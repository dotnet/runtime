using System.Collections.Generic;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public class TestDependencyRecorder : IDependencyRecorder
	{
		public struct Dependency
		{
			public string Source;
			public string Target;
			public bool Marked;
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
				Marked = marked
			});
		}
	}
}
