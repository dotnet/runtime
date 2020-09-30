using System;
using System.Threading;
using System.Reflection;

public class ThreadEx
{
	public static void Abort(Thread thread)
	{
		MethodInfo abort = null;
		foreach(MethodInfo m in thread.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
		{
			if (m.Name.Equals("AbortInternal") && m.GetParameters().Length == 0) abort = m;
		}
		if (abort == null)
		{
			throw new Exception("Failed to get Thread.Abort method");
		}
		abort.Invoke(thread, new object[0]);
	}
}