// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading;
using System.Reflection;

using TestLibrary;

namespace BinderTracingTests
{
    internal class BindOperation
    {
        public AssemblyName AssemblyName { get; internal set; }
        public string AssemblyPath { get; internal set; }
        public AssemblyName RequestingAssembly { get; internal set; }
        public string AssemblyLoadContext { get; internal set; }
        public string RequestingAssemblyLoadContext { get; internal set; }

        public bool Success { get; internal set; }
        public AssemblyName ResultAssemblyName { get; internal set; }
        public string ResultAssemblyPath { get; internal set; }
        public bool Cached { get; internal set; }

        public Guid ActivityId { get; internal set; }
        public Guid ParentActivityId { get; internal set; }

        public bool Completed { get; internal set; }
        public bool Nested { get; internal set; }

        public List<ResolutionAttempt> ResolutionAttempts { get; internal set; }

        public List<HandlerInvocation> AssemblyLoadContextResolvingHandlers { get; internal set; }
        public List<HandlerInvocation> AppDomainAssemblyResolveHandlers { get; internal set; }
        public LoadFromHandlerInvocation AssemblyLoadFromHandler { get; internal set; }

        public List<ProbedPath> ProbedPaths { get; internal set; }

        public List<BindOperation> NestedBinds { get; internal set; }

        public BindOperation()
        {
            ResolutionAttempts = new List<ResolutionAttempt>();
            AssemblyLoadContextResolvingHandlers = new List<HandlerInvocation>();
            AppDomainAssemblyResolveHandlers = new List<HandlerInvocation>();
            ProbedPaths = new List<ProbedPath>();
            NestedBinds = new List<BindOperation>();
        }

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(AssemblyName);
            sb.Append($" - Request: Path={AssemblyPath}, ALC={AssemblyLoadContext}, RequestingAssembly={RequestingAssembly}, RequestingALC={RequestingAssemblyLoadContext}");
            sb.Append($" - Result: Success={Success}, Name={ResultAssemblyName}, Path={ResultAssemblyPath}, Cached={Cached}");
            return sb.ToString();
        }
    }

    internal class ResolutionAttempt
    {
        public enum ResolutionStage : ushort
        {
            FindInLoadContext,
            AssemblyLoadContextLoad,
            ApplicationAssemblies,
            DefaultAssemblyLoadContextFallback,
            ResolveSatelliteAssembly,
            AssemblyLoadContextResolvingEvent,
            AppDomainAssemblyResolveEvent,
        }

        public enum ResolutionResult : ushort
        {
            Success,
            AssemblyNotFound,
            IncompatibleVersion,
            MismatchedAssemblyName,
            Failure,
            Exception,
        }

        public AssemblyName AssemblyName { get; internal set; }
        public ResolutionStage Stage { get; internal set; }
        public string AssemblyLoadContext { get; internal set; }
        public ResolutionResult Result { get; internal set; }
        public AssemblyName ResultAssemblyName { get; internal set; }
        public string ResultAssemblyPath { get; internal set; }
        public string ErrorMessage { get; internal set; }

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine(Stage.ToString());
            sb.AppendLine($"  AssemblyName={AssemblyName.FullName}");
            sb.AppendLine($"  ALC={AssemblyLoadContext}");
            sb.AppendLine($"  Result={Result}");
            sb.AppendLine($"  ResultAssemblyName={ResultAssemblyName?.FullName}");
            sb.AppendLine($"  ResultAssemblyPath={ResultAssemblyPath}");
            sb.Append($"  ErrorMessage={ErrorMessage}");
            return sb.ToString();
        }
    }

    internal class HandlerInvocation
    {
        public AssemblyName AssemblyName { get; internal set; }
        public string HandlerName { get; internal set; }
        public string AssemblyLoadContext { get; internal set; }

        public AssemblyName ResultAssemblyName { get; internal set; }
        public string ResultAssemblyPath { get; internal set; }

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append($"{HandlerName} - ");
            sb.Append($"Request: Name={AssemblyName.FullName}");
            if (!string.IsNullOrEmpty(AssemblyLoadContext))
                sb.Append($", ALC={AssemblyLoadContext}");

            sb.Append($" - Result: Name={ResultAssemblyName?.FullName}, Path={ResultAssemblyPath}");
            return sb.ToString();
        }
    }

    internal class LoadFromHandlerInvocation
    {
        public AssemblyName AssemblyName { get; internal set; }
        public bool IsTrackedLoad { get; internal set; }
        public string RequestingAssemblyPath { get; internal set; }
        public string ComputedRequestedAssemblyPath { get; internal set; }
    }

    internal class ProbedPath
    {
        public enum PathSource : ushort
        {
            ApplicationAssemblies,
            Unused,
            AppPaths,
            PlatformResourceRoots,
            SatelliteSubdirectory
        }

        public string FilePath { get; internal set; }
        public PathSource Source { get; internal set; }
        public int Result { get; internal set; }

        public override string ToString()
        {
            return $"{FilePath} - Source={Source}, Result={Result}";
        }
    }

    internal sealed class BinderEventListener : EventListener
    {
        private const EventKeywords AssemblyLoaderKeyword = (EventKeywords)0x4;

        private readonly object eventsLock = new object();
        private readonly Dictionary<Guid, BindOperation> bindOperations = new Dictionary<Guid, BindOperation>();
        private readonly string[] loadsToTrack;

        public BinderEventListener(string[] loadsToTrack, bool log = false)
        {
            this.loadsToTrack = loadsToTrack;
        }

        public BindOperation[] WaitAndGetEventsForAssembly(AssemblyName assemblyName)
        {
            Assert.IsTrue(IsLoadToTrack(assemblyName.Name), $"Waiting for load for untracked name: {assemblyName.Name}. Tracking loads for: {string.Join(", ", loadsToTrack)}");

            const int waitIntervalInMs = 50;
            int waitTimeoutInMs = Environment.GetEnvironmentVariable("COMPlus_GCStress") == null
                ? 30 * 1000
                : int.MaxValue;

            int timeWaitedInMs = 0;
            while (true)
            {
                lock (eventsLock)
                {
                    var events = bindOperations.Values.Where(e => e.Completed && Helpers.AssemblyNamesMatch(e.AssemblyName, assemblyName) && !e.Nested);
                    if (events.Any())
                    {
                        return events.ToArray();
                    }
                }

                Thread.Sleep(waitIntervalInMs);
                timeWaitedInMs += waitIntervalInMs;

                if (timeWaitedInMs > waitTimeoutInMs)
                {
                    var msg = new System.Text.StringBuilder($"Timed out waiting for bind events for {assemblyName}");
                    msg.AppendLine(GetReceivedEventsAsString());
                    throw new TimeoutException(msg.ToString());
                }
            }
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == "Microsoft-Windows-DotNETRuntime")
            {
                EnableEvents(eventSource, EventLevel.Verbose, AssemblyLoaderKeyword);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs data)
        {
            if (data.EventSource.Name != "Microsoft-Windows-DotNETRuntime")
                return;

            object GetData(string name)
            {
                int index = data.PayloadNames.IndexOf(name);
                return index >= 0 ? data.Payload[index] : null;
            };
            string GetDataString(string name) => GetData(name) as string;

            switch (data.EventName)
            {
                case "AssemblyLoadStart":
                {
                    BindOperation bindOperation = ParseAssemblyLoadStartEvent(data, GetDataString);
                    if (!IsLoadToTrack(bindOperation.AssemblyName.Name))
                        return;

                    lock (eventsLock)
                    {
                        Assert.IsTrue(!bindOperations.ContainsKey(data.ActivityId), "AssemblyLoadStart should not exist for same activity ID ");
                        bindOperation.Nested = bindOperations.ContainsKey(data.RelatedActivityId);
                        bindOperations.Add(data.ActivityId, bindOperation);
                        if (bindOperation.Nested)
                        {
                            bindOperations[data.RelatedActivityId].NestedBinds.Add(bindOperation);
                        }
                    }
                    break;
                }
                case "AssemblyLoadStop":
                {
                    string assemblyName = GetDataString("AssemblyName");
                    if (!IsLoadToTrack(new AssemblyName(assemblyName).Name))
                        return;

                    bool success = (bool)GetData("Success");
                    string resultName = GetDataString("ResultAssemblyName");
                    lock (eventsLock)
                    {
                        if (!bindOperations.ContainsKey(data.ActivityId))
                            Assert.Fail(GetMissingAssemblyBindStartMessage(data, $"Success={success}, Name={resultName}"));

                        BindOperation bind = bindOperations[data.ActivityId];
                        bind.Success = success;
                        if (!string.IsNullOrEmpty(resultName))
                        {
                            bind.ResultAssemblyName = new AssemblyName(resultName);
                        }
                        bind.ResultAssemblyPath = GetDataString("ResultAssemblyPath");
                        bind.Cached = (bool)GetData("Cached");
                        bind.Completed = true;
                    }
                    break;
                }
                case "ResolutionAttempted":
                {
                    ResolutionAttempt attempt = ParseResolutionAttemptedEvent(GetData, GetDataString);
                    if (!IsLoadToTrack(attempt.AssemblyName.Name))
                        return;

                    lock (eventsLock)
                    {
                        if (!bindOperations.ContainsKey(data.ActivityId))
                            Assert.Fail(GetMissingAssemblyBindStartMessage(data, attempt.ToString()));

                        BindOperation bind = bindOperations[data.ActivityId];
                        bind.ResolutionAttempts.Add(attempt);
                    }
                    break;
                }
                case "AssemblyLoadContextResolvingHandlerInvoked":
                {
                    HandlerInvocation handlerInvocation = ParseHandlerInvokedEvent(GetDataString);
                    if (!IsLoadToTrack(handlerInvocation.AssemblyName.Name))
                        return;

                    lock (eventsLock)
                    {
                        if (!bindOperations.ContainsKey(data.ActivityId))
                            Assert.Fail(GetMissingAssemblyBindStartMessage(data, handlerInvocation.ToString()));

                        BindOperation bind = bindOperations[data.ActivityId];
                        bind.AssemblyLoadContextResolvingHandlers.Add(handlerInvocation);
                    }
                    break;
                }
                case "AppDomainAssemblyResolveHandlerInvoked":
                {
                    HandlerInvocation handlerInvocation = ParseHandlerInvokedEvent(GetDataString);
                    if (!IsLoadToTrack(handlerInvocation.AssemblyName.Name))
                        return;

                    lock (eventsLock)
                    {
                        if (!bindOperations.ContainsKey(data.ActivityId))
                            Assert.Fail(GetMissingAssemblyBindStartMessage(data, handlerInvocation.ToString()));

                        BindOperation bind = bindOperations[data.ActivityId];
                        bind.AppDomainAssemblyResolveHandlers.Add(handlerInvocation);
                    }
                    break;
                }
                case "AssemblyLoadFromResolveHandlerInvoked":
                {
                    LoadFromHandlerInvocation loadFrom = ParseLoadFromHandlerInvokedEvent(GetData, GetDataString);
                    if (!IsLoadToTrack(loadFrom.AssemblyName.Name))
                        return;

                    lock (eventsLock)
                    {
                        if (!bindOperations.ContainsKey(data.ActivityId))
                            Assert.Fail(GetMissingAssemblyBindStartMessage(data, loadFrom.ToString()));

                        BindOperation bind = bindOperations[data.ActivityId];
                        bind.AssemblyLoadFromHandler = loadFrom;
                    }
                    break;
                }
                case "KnownPathProbed":
                {
                    ProbedPath probedPath = ParseKnownPathProbedEvent(GetData, GetDataString);
                    string name = System.IO.Path.GetFileNameWithoutExtension(probedPath.FilePath);
                    if (!IsLoadToTrack(name.EndsWith(".ni") ? name.Remove(name.Length - 3) : name))
                        return;

                    lock (eventsLock)
                    {
                        if (!bindOperations.ContainsKey(data.ActivityId))
                            Assert.Fail(GetMissingAssemblyBindStartMessage(data, probedPath.ToString()));

                        BindOperation bind = bindOperations[data.ActivityId];
                        bind.ProbedPaths.Add(probedPath);
                    }
                    break;
                }
            }
        }

        private bool IsLoadToTrack(string name)
        {
            return this.loadsToTrack.Any(n => n.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }

        private string GetMissingAssemblyBindStartMessage(EventWrittenEventArgs data, string parsedEventAsString)
        {
            var msg = new System.Text.StringBuilder();
            msg.AppendLine($"{data.EventName} (ActivityId: {data.ActivityId}) should have a matching AssemblyBindStart");
            msg.AppendLine($"Parsed event: {parsedEventAsString}");
            msg.AppendLine(GetReceivedEventsAsString());
            return msg.ToString();
        }

        private string GetReceivedEventsAsString()
        {
            var msg = new System.Text.StringBuilder();
            msg.AppendLine("Received events:");
            lock (eventsLock)
            {
                foreach (BindOperation op in bindOperations.Values)
                {
                    msg.AppendLine(op.ToString());
                    msg.AppendLine($" - ActivityId: {op.ActivityId}");
                    msg.AppendLine($" - Completed: {op.Completed}");
                    msg.AppendLine($" - Nested: {op.Nested}");
                }
            }

            return msg.ToString();
        }

        private BindOperation ParseAssemblyLoadStartEvent(EventWrittenEventArgs data, Func<string, string> getDataString)
        {
            var bindOperation = new BindOperation()
            {
                AssemblyName = new AssemblyName(getDataString("AssemblyName")),
                AssemblyPath = getDataString("AssemblyPath"),
                AssemblyLoadContext = getDataString("AssemblyLoadContext"),
                RequestingAssemblyLoadContext = getDataString("RequestingAssemblyLoadContext"),
                ActivityId = data.ActivityId,
                ParentActivityId = data.RelatedActivityId,
            };
            string requestingAssembly = getDataString("RequestingAssembly");
            if (!string.IsNullOrEmpty(requestingAssembly) && requestingAssembly != "NULL")
            {
                bindOperation.RequestingAssembly = new AssemblyName(requestingAssembly);
            }

            return bindOperation;
        }

        private ResolutionAttempt ParseResolutionAttemptedEvent(Func<string, object> getData, Func<string, string> getDataString)
        {
            var attempt = new ResolutionAttempt()
            {
                AssemblyName = new AssemblyName(getDataString("AssemblyName")),
                Stage = (ResolutionAttempt.ResolutionStage)getData("Stage"),
                AssemblyLoadContext = getDataString("AssemblyLoadContext"),
                Result = (ResolutionAttempt.ResolutionResult)getData("Result"),
                ResultAssemblyPath = getDataString("ResultAssemblyPath"),
                ErrorMessage = getDataString("ErrorMessage")
            };
            string resultName = getDataString("ResultAssemblyName");
            if (!string.IsNullOrEmpty(resultName) && resultName != "NULL")
            {
                attempt.ResultAssemblyName = new AssemblyName(resultName);
            }

            return attempt;
        }

        private HandlerInvocation ParseHandlerInvokedEvent(Func<string, string> getDataString)
        {
            var handlerInvocation = new HandlerInvocation()
            {
                AssemblyName = new AssemblyName(getDataString("AssemblyName")),
                HandlerName = getDataString("HandlerName"),
                AssemblyLoadContext = getDataString("AssemblyLoadContext"),
                ResultAssemblyPath = getDataString("ResultAssemblyPath")
            };
            string resultName = getDataString("ResultAssemblyName");
            if (!string.IsNullOrEmpty(resultName) && resultName != "NULL")
            {
                handlerInvocation.ResultAssemblyName = new AssemblyName(resultName);
            }

            return handlerInvocation;
        }

        private LoadFromHandlerInvocation ParseLoadFromHandlerInvokedEvent(Func<string, object> getData, Func<string, string> getDataString)
        {
            var loadFrom = new LoadFromHandlerInvocation()
            {
                AssemblyName = new AssemblyName(getDataString("AssemblyName")),
                IsTrackedLoad = (bool)getData("IsTrackedLoad"),
                RequestingAssemblyPath = getDataString("RequestingAssemblyPath"),
                ComputedRequestedAssemblyPath = getDataString("ComputedRequestedAssemblyPath"),
            };
            return loadFrom;
        }

        private ProbedPath ParseKnownPathProbedEvent(Func<string, object> getData, Func<string, string> getDataString)
        {
            var probedPath = new ProbedPath()
            {
                FilePath = getDataString("FilePath"),
                Source = (ProbedPath.PathSource)getData("Source"),
                Result = (int)getData("Result"),
            };
            return probedPath;
        }
    }
}
