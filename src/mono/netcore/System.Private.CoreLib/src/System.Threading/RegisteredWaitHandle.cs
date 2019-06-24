// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Threading
{
	public sealed class RegisteredWaitHandle : MarshalByRefObject
	{
		WaitHandle _waitObject;
		WaitOrTimerCallback _callback;
		object _state;
		WaitHandle _finalEvent;
		ManualResetEvent _cancelEvent;
		TimeSpan _timeout;
		int _callsInProcess;
		bool _executeOnlyOnce;
		bool _unregistered;

		internal RegisteredWaitHandle (WaitHandle waitObject, WaitOrTimerCallback callback, object state, TimeSpan timeout, bool executeOnlyOnce)
		{
			_waitObject = waitObject;
			_callback = callback;
			_state = state;
			_timeout = timeout;
			_executeOnlyOnce = executeOnlyOnce;
			_cancelEvent = new ManualResetEvent (false);
		}

		internal void Wait (object? state)
		{
			bool release = false;
			try {
				_waitObject.SafeWaitHandle.DangerousAddRef (ref release);
				try {
					WaitHandle[] waits = new WaitHandle[] {_waitObject, _cancelEvent};
					do {
						int signal = WaitHandle.WaitAny (waits, _timeout, false);
						if (!_unregistered) {
							lock (this) {
								_callsInProcess++;
							}
							ThreadPool.QueueUserWorkItem (new WaitCallback (DoCallBack), (signal == WaitHandle.WaitTimeout));
						}
					} while (!_unregistered && !_executeOnlyOnce);
				} catch {
				}

				lock (this) {
					_unregistered = true;
					if (_callsInProcess == 0 && _finalEvent != null)
						throw new NotImplementedException ();
				}
			} catch (ObjectDisposedException) {
				// Can happen if we called Unregister before we had time to execute Wait
				if (release)
					throw;
			} finally {
				if (release)
					_waitObject.SafeWaitHandle.DangerousRelease ();
			}
		}

		void DoCallBack (object? timedOut)
		{
			try {
				if (_callback != null)
					_callback (_state, (bool)timedOut!);
			} finally {
				lock (this) {
					_callsInProcess--;
					if (_unregistered && _callsInProcess == 0 && _finalEvent != null) {
						EventWaitHandle.Set (_finalEvent.SafeWaitHandle);
						_finalEvent = null;
					}
				}
			}
		}

		public bool Unregister(WaitHandle waitObject) 
		{
			lock (this) {
				if (_unregistered)
					return false;

				_finalEvent = waitObject;
				_unregistered = true;
				_cancelEvent.Set();

				return true;
			}
		}
	}
}
