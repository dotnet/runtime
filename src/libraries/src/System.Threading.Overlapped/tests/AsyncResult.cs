// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

public class AsyncResult : IAsyncResult
{
	public object AsyncState
	{
		get { throw new NotImplementedException(); }
	}

	public System.Threading.WaitHandle AsyncWaitHandle
	{
		get { throw new NotImplementedException(); }
	}

	public bool CompletedSynchronously
	{
		get { throw new NotImplementedException(); }
	}

	public bool IsCompleted
	{
		get { throw new NotImplementedException(); }
	}
}
