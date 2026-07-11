# Complete code for InstallerTasks.cs
using System;
using System.Threading;

namespace Microsoft.CSharp.Core.Tasks
{
    public class InstallerTasks
    {
        private Mutex _mutex;

        public InstallerTasks()
        {
            _mutex = new Mutex();
        }

        public void Install()
        {
            try
            {
                _mutex.WaitOne();
                // Install code here
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is OperationCanceledException)
            {
                LogMutexReleaseFailure(ex);
                throw;
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
        }

        private void LogMutexReleaseFailure(Exception ex)
        {
            Console.WriteLine($"Mutex release failure: {ex.Message}");
            Console.WriteLine($"Mutex ID: {_mutex.Id}");
            Console.WriteLine($"Mutex Release ID: {_mutex.ReleaseId}");
        }
    }
}