// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Threading
{
    public sealed partial class RegisteredWaitHandle : MarshalByRefObject
    {
        /// <summary>
        /// The <see cref="PortableThreadPool.WaitThread"/> this <see cref="RegisteredWaitHandle"/> was registered on.
        /// </summary>
        internal PortableThreadPool.WaitThread? WaitThread { get; set; }

        private bool UnregisterPortable(WaitHandle waitObject)
        {
            // The registered wait handle must have been registered by this time, otherwise the instance is not handed out to
            // the caller of the public variants of RegisterWaitForSingleObject
            Debug.Assert(WaitThread != null);

            s_callbackLock.Acquire();
            bool needToRollBackRefCountOnException = false;
            try
            {
                if (_unregisterCalled)
                {
                    return false;
                }

                UserUnregisterWaitHandle = waitObject?.SafeWaitHandle;
                UserUnregisterWaitHandle?.DangerousAddRef(ref needToRollBackRefCountOnException);

                UserUnregisterWaitHandleValue = UserUnregisterWaitHandle?.DangerousGetHandle() ?? IntPtr.Zero;

                if (_unregistered)
                {
                    SignalUserWaitHandle();
                    return true;
                }

                if (IsBlocking)
                {
                    _callbacksComplete = RentEvent();
                }
                else
                {
                    _removed = RentEvent();
                }
            }
            catch (Exception) // Rollback state on exception
            {
                if (_removed != null)
                {
                    ReturnEvent(_removed);
                    _removed = null;
                }
                else if (_callbacksComplete != null)
                {
                    ReturnEvent(_callbacksComplete);
                    _callbacksComplete = null;
                }

                UserUnregisterWaitHandleValue = IntPtr.Zero;

                if (needToRollBackRefCountOnException)
                {
                    UserUnregisterWaitHandle?.DangerousRelease();
                }

                UserUnregisterWaitHandle = null;
                throw;
            }
            finally
            {
                _unregisterCalled = true;
                s_callbackLock.Release();
            }

            WaitThread!.UnregisterWait(this);
            return true;
        }
    }
}
