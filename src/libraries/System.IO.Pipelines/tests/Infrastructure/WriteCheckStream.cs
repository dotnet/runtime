using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Pipelines.Tests.Infrastructure
{
    public class WriteCheckMemoryStream : Stream
    {
        private readonly MemoryStream _ms = new MemoryStream();
        private int _writeCnt = 0;
        private int _waitCnt = 0;
        private TaskCompletionSource<object> _waitSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        public CancellationTokenSource MidWriteCancellation { get; set; }
        private readonly object _lock = new object();

        public override bool CanRead => _ms.CanRead;

        public override bool CanSeek => _ms.CanSeek;

        public override bool CanWrite => _ms.CanWrite;

        public override long Length => _ms.Length;

        public override long Position { get => _ms.Position; set => _ms.Position = value; }

        public override void Flush() => _ms.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _ms.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => _ms.Seek(offset, origin);
        public override void SetLength(long value) => _ms.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count)
        {
            lock (_lock)
            {
                _ms.Write(buffer, offset, count);
                MidWriteCancellation?.Cancel();
                _writeCnt += count;
                CheckWaitCount();
            }
        }

        public byte[] ToArray() => _ms.ToArray();

        public Task WaitForBytesWrittenAsync(int cnt)
        {
            if (cnt <= 0)
                throw new ArgumentException($"{nameof(cnt)} must be greater than 0");
            lock (_lock)
            {
                _waitCnt = cnt;
                _waitSource.TrySetCanceled();
                _waitSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                CheckWaitCount();
                return _waitSource.Task;
            }
        }
        
        private void CheckWaitCount() 
        {
            if (_waitCnt > 0 && _writeCnt >= _waitCnt)
            {
                _writeCnt = 0;
                _waitCnt = 0;
                _waitSource.TrySetResult(null);
            }
        }

        public override string ToString() => Encoding.ASCII.GetString(_ms.ToArray());
    }
}
