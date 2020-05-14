using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    internal sealed class SingleEventValueTaskSource : IValueTaskSource
    {
        private ManualResetValueTaskSourceCore<int> _source;

        public SingleEventValueTaskSource()
        {
            _source.RunContinuationsAsynchronously = true;
        }

        public bool IsSet { get; private set; }

        public bool TryComplete()
        {
            if (!IsSet)
            {
                IsSet = true;
                _source.SetResult(1);

                return true;
            }

            return false;
        }

        public bool TryCompleteException(Exception e)
        {
            if (!IsSet)
            {
                IsSet = true;
                _source.SetException(e);

                return true;
            }

            return false;
        }

        public void GetResult(short token) => _source.GetResult(token);

        public ValueTaskSourceStatus GetStatus(short token) => _source.GetStatus(token);

        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _source.OnCompleted(continuation, state, token, flags);

        public ValueTask GetTask() => new ValueTask(this, _source.Version);
    }
}
