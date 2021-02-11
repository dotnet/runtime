// IMyInterface.cs created with MonoDevelop
// User: lluis at 15:47 18/05/2007
//

using System;

namespace Application
{
	public interface IMyInterface
	{
		void Run ();
#if WITH_STOP
		void Stop ();
#endif
	}
}
