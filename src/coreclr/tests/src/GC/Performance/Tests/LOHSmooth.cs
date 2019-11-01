// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Diagnostics;

public class VSTSTest
{
    static int _threadCount = 16;
    static int _minSize = 200 * 1024;
    static int _maxSize = 250 * 1024;
    static int _holdSize = 100;
    static int _maxCount = 100;
    static int _count = 0;
    static bool _done = false;
    static long _maxPrivate = 0;
    static object lockObject = new Object();
        
    // each worker will hold *in the worst case scenario* 100 * 250k * 2 = 50MB
    // all 16 threads will hold in the worst case scenario 800MB
    static void Worker(object ctx)
    {
        string[] s = new string[_holdSize];
        Random rnd = new Random();
        for (int i = 0; i < _holdSize; i++)
        {
            Thread.Sleep(rnd.Next(50, 100));
            s[i] = new string('x', rnd.Next(_minSize, _maxSize));
     
	    long _private = Process.GetCurrentProcess().PrivateMemorySize64;
		
	    lock (lockObject)
	    {
		if (_private > _maxPrivate)
		    _maxPrivate = _private;
	    }
        }
        
	if (_count++ >= _maxCount)
		_done = true;
	
	if (!_done)
		ThreadPool.QueueUserWorkItem(Worker);
    }

    static public void Allocate()
    {
        for (int i = 0; i < _threadCount; i++)
        {
            ThreadPool.QueueUserWorkItem(Worker);
        }
    }

    public static void Main(string[] args)
    {		    
        Allocate();    
        while (!_done)
        {
            Thread.Sleep(0);
        }
    }
}
