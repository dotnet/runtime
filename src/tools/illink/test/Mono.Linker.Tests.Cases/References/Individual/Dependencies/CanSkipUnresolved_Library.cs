using System;

namespace Mono.Linker.Tests.Cases.References.Individual.Dependencies
{
	public class CanSkipUnresolved_Library
	{
		public class TypeWithMissingMethod
		{
#if !EXCLUDE_STUFF
			public void GoingToBeMissing ()
			{

			}
#endif
		}

#if !EXCLUDE_STUFF
		public class TypeThatWillBeMissing
		{
		}
#endif
	}
}
