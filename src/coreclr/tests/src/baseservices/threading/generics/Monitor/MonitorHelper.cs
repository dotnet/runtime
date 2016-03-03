using System;
using System.Threading;
delegate void MonitorDelegate(object monitor);
delegate void MonitorDelegateTS(object monitor,int timeout);
	
class TestHelper
{
	private int m_iSharedData;
	private int m_iRequestedEntries;
	public ManualResetEvent m_Event;
	public bool m_bError;

	public bool Error
	{		
		set
		{
			lock(typeof(TestHelper))
			{
				m_bError = value;
			}
		}
		get
		{
			lock(typeof(TestHelper))
			{
				return m_bError;
			}
		}
	}

	public TestHelper(int num)
	{
		m_Event = new ManualResetEvent(false);
		m_iSharedData = 0;
		m_iRequestedEntries = num;
		m_bError = false;
	}
	
	public void DoWork()
	{
		int snapshot = m_iSharedData;
		Thread.Sleep(5);
#if (DEBUG)
		Console.WriteLine("Entering Monitor: " + m_iSharedData);
#endif
		m_iSharedData++;
		Thread.Sleep(1);
		if(m_iSharedData != snapshot + 1)
		{
			Error = true;
			Console.WriteLine("Failure!!!");
		}
#if (DEBUG)
		Console.WriteLine("Leaving Monitor: " + m_iSharedData);        
#endif
		if(m_iSharedData == m_iRequestedEntries)
			m_Event.Set();
	}	
	public void Consumer(object monitor)
	{
		lock(monitor)
		{
			DoWork();
		}	
	}
	public void ConsumerTryEnter(object monitor,int timeout)
	{
		try
		{
			bool tookLock = false;
			
			Monitor.TryEnter(monitor,timeout, ref tookLock);

			while(!tookLock) {				
				Thread.Sleep(0);
				Monitor.TryEnter(monitor,timeout, ref tookLock);
			}

			DoWork();
		}
		finally
		{
			Monitor.Exit(monitor);
		}
	}
}