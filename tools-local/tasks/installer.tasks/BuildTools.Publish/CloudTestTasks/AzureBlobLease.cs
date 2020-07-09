// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Build.CloudTestTasks
{
    public partial class AzureBlobLease
    {
        private string _containerName;
        private string _blobName;
        private TimeSpan _maxWait;
        private TimeSpan _delay;
        private const int s_MaxWaitDefault = 60; // seconds
        private const int s_DelayDefault = 500; // milliseconds
        private CancellationTokenSource _cancellationTokenSource;
        private Task _leaseRenewalTask;
        private string _connectionString;
        private string _accountName;
        private string _accountKey;
        private Microsoft.Build.Utilities.TaskLoggingHelper _log;
        private string _leaseId;
        private string _leaseUrl;

        public AzureBlobLease(string accountName, string accountKey, string connectionString, string containerName, string blobName, Microsoft.Build.Utilities.TaskLoggingHelper log, string maxWait = null, string delay = null)
        {
            _accountName = accountName;
            _accountKey = accountKey;
            _connectionString = connectionString;
            _containerName = containerName;
            _blobName = blobName;
            _maxWait = !string.IsNullOrWhiteSpace(maxWait) ? TimeSpan.Parse(maxWait) : TimeSpan.FromSeconds(s_MaxWaitDefault);
            _delay = !string.IsNullOrWhiteSpace(delay) ? TimeSpan.Parse(delay) : TimeSpan.FromMilliseconds(s_DelayDefault);
            _log = log;
            _leaseUrl = $"{AzureHelper.GetBlobRestUrl(_accountName, _containerName, _blobName)}?comp=lease";
        }

        public string Acquire()
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            while (stopWatch.ElapsedMilliseconds < _maxWait.TotalMilliseconds)
            {
                try
                {
                    string leaseId = AcquireLeaseOnBlobAsync().GetAwaiter().GetResult();
                    _cancellationTokenSource = new CancellationTokenSource();
                    _leaseRenewalTask = Task.Run(() =>
                    { AutoRenewLeaseOnBlob(this, _log); },
                      _cancellationTokenSource.Token);
                    _leaseId = leaseId;
                    return _leaseId;
                }
                catch (Exception e)
                {
                    _log.LogMessage($"Retrying lease acquisition on {_blobName}, {e.Message}");
                    Thread.Sleep(_delay);
                }
            }
            ResetLeaseRenewalTaskState();
            throw new Exception($"Unable to acquire lease on {_blobName}");

        }

        public void Release()
        {
            // Cancel the lease renewal task since we are about to release the lease.
            ResetLeaseRenewalTaskState();

            using (HttpClient client = new HttpClient())
            {
                Tuple<string, string> leaseAction = new Tuple<string, string>("x-ms-lease-action", "release");
                Tuple<string, string> headerLeaseId = new Tuple<string, string>("x-ms-lease-id", _leaseId);
                List<Tuple<string, string>> additionalHeaders = new List<Tuple<string, string>>() { leaseAction, headerLeaseId };
                var request = AzureHelper.RequestMessage("PUT", _leaseUrl, _accountName, _accountKey, additionalHeaders);
                using (HttpResponseMessage response = AzureHelper.RequestWithRetry(_log, client, request).GetAwaiter().GetResult())
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        _log.LogMessage($"Unable to release lease on container/blob {_containerName}/{_blobName}.");
                    }
                }
            }
        }

        private async Task<string> AcquireLeaseOnBlobAsync()
        {
            _log.LogMessage(MessageImportance.Low, $"Requesting lease for container/blob '{_containerName}/{_blobName}'.");
            string leaseId = string.Empty;
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    Tuple<string, string> leaseAction = new Tuple<string, string>("x-ms-lease-action", "acquire");
                    Tuple<string, string> leaseDuration = new Tuple<string, string>("x-ms-lease-duration", "60" /* seconds */);
                    List<Tuple<string, string>> additionalHeaders = new List<Tuple<string, string>>() { leaseAction, leaseDuration };
                    var request = AzureHelper.RequestMessage("PUT", _leaseUrl, _accountName, _accountKey, additionalHeaders);
                    using (HttpResponseMessage response = await AzureHelper.RequestWithRetry(_log, client, request))
                    {
                        leaseId = response.Headers.GetValues("x-ms-lease-id").FirstOrDefault();
                    }
                }
                catch (Exception e)
                {
                    _log.LogErrorFromException(e, true);
                }
            }

            return leaseId;
        }
        private static void AutoRenewLeaseOnBlob(AzureBlobLease instance, Microsoft.Build.Utilities.TaskLoggingHelper log)
        {
            TimeSpan maxWait = TimeSpan.FromSeconds(s_MaxWaitDefault);
            TimeSpan delay = TimeSpan.FromMilliseconds(s_DelayDefault);
            TimeSpan waitFor = maxWait;
            CancellationToken token = instance._cancellationTokenSource.Token;

            while (true)
            {
                token.ThrowIfCancellationRequested();

                try
                {
                    log.LogMessage(MessageImportance.Low, $"Requesting lease for container/blob '{instance._containerName}/{instance._blobName}'.");
                    using (HttpClient client = new HttpClient())
                    {
                        Tuple<string, string> leaseAction = new Tuple<string, string>("x-ms-lease-action", "renew");
                        Tuple<string, string> headerLeaseId = new Tuple<string, string>("x-ms-lease-id", instance._leaseId);
                        List<Tuple<string, string>> additionalHeaders = new List<Tuple<string, string>>() { leaseAction, headerLeaseId };
                        var request = AzureHelper.RequestMessage("PUT", instance._leaseUrl, instance._accountName, instance._accountKey, additionalHeaders);
                        using (HttpResponseMessage response = AzureHelper.RequestWithRetry(log, client, request).GetAwaiter().GetResult())
                        {
                            if (!response.IsSuccessStatusCode)
                            {
                                throw new Exception("Unable to acquire lease.");
                            }
                        }
                    }
                    waitFor = maxWait;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Rerying lease renewal on {instance._containerName}, {e.Message}");
                    waitFor = delay;
                }
                token.ThrowIfCancellationRequested();

                Task.Delay(waitFor, token).Wait();
            }
        }

        private void ResetLeaseRenewalTaskState()
        {
            // Cancel the lease renewal task if it was created
            if (_leaseRenewalTask != null)
            {
                _cancellationTokenSource.Cancel();

                // Block until the task ends. It can throw if we cancelled it before it completed.
                try
                {
                    _leaseRenewalTask.Wait();
                }
                catch (Exception)
                {
                    // Ignore the caught exception as it will be expected.
                }

                _leaseRenewalTask = null;
            }
        }

    }
}
