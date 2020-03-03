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
