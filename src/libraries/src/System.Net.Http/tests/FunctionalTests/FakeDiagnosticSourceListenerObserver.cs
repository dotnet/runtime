using System.Collections.Generic;
using System.Diagnostics;

namespace System.Net.Http.Functional.Tests
{
    public sealed class FakeDiagnosticListenerObserver : IObserver<DiagnosticListener>
    {
        private class FakeDiagnosticSourceWriteObserver : IObserver<KeyValuePair<string, object>>
        {
            private readonly Action<KeyValuePair<string, object>> _writeCallback;

            public FakeDiagnosticSourceWriteObserver(Action<KeyValuePair<string, object>> writeCallback)
            {
                _writeCallback = writeCallback;
            }

            public void OnCompleted()
            {
            }

            public void OnError(Exception error)
            {
            }

            public void OnNext(KeyValuePair<string, object> value)
            {
                _writeCallback(value);
            }
        }

        private readonly Action<KeyValuePair<string, object>> _writeCallback;

        private bool _writeObserverEnabled;

        public FakeDiagnosticListenerObserver(Action<KeyValuePair<string, object>> writeCallback)
        {
            _writeCallback = writeCallback;
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(DiagnosticListener value)
        {
            if (value.Name.Equals("HttpHandlerDiagnosticListener"))
            {
                value.Subscribe(new FakeDiagnosticSourceWriteObserver(_writeCallback), IsEnabled);
            }
        }

        public void Enable()
        {
            _writeObserverEnabled = true;
        }

        public void Disable()
        {
            _writeObserverEnabled = false;
        }

        private bool IsEnabled(string s)
        {
            return _writeObserverEnabled;
        }
    }
}
