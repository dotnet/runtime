using System.Threading;

namespace System.Net.Security
{
    internal class LazyAsyncResult : IAsyncResult
    {
        public LazyAsyncResult(SslState sslState, object asyncState, AsyncCallback asyncCallback)
        {
        }

        public object AsyncState
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public WaitHandle AsyncWaitHandle
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool CompletedSynchronously
        {
            get
            {
                return true;
            }
        }

        public bool IsCompleted
        {
            get
            {
                return true;
            }
        }
    }
}