// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Internal.TypeSystem;

using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysis.ReadyToRun;

namespace ILCompiler.PEWriter
{
    /// <summary>
    /// Helper class used to calculate code layout quality heuristics w.r.t. given call chain profile.
    /// </summary>
    public class ProfileFileBuilder
    {
        private enum CrossPageCall : byte
        {
            No,
            Yes,
            Unresolved,
        }

        private class CallInfo
        {
            public readonly MethodDesc Caller;
            public readonly OutputNode CallerNode;
            public readonly int CallerRVA;
            public readonly MethodDesc Callee;
            public readonly OutputNode CalleeNode;
            public readonly int CalleeRVA;
            public readonly int CallCount;
            public readonly CrossPageCall CallType;

            public CallInfo(MethodDesc caller, OutputNode callerNode, int callerRVA, MethodDesc callee, OutputNode calleeNode, int calleeRVA, int callCount, CrossPageCall callType)
            {
                Caller = caller;
                CallerNode = callerNode;
                CallerRVA = callerRVA;
                Callee = callee;
                CalleeNode = calleeNode;
                CalleeRVA = calleeRVA;
                CallCount = callCount;
                CallType = callType;
            }
        }

        private readonly OutputInfoBuilder _outputInfoBuilder;
        private readonly CallChainProfile _callChainProfile;
        private readonly TargetDetails _targetDetails;
        private readonly int _pageSize;

        private Dictionary<MethodDesc, ISymbolDefinitionNode> _symbolMethodMap;
        private List<CallInfo> _callInfo;

        public ProfileFileBuilder(OutputInfoBuilder outputInfoBuilder, CallChainProfile callChainProfile, TargetDetails targetDetails)
        {
            _outputInfoBuilder = outputInfoBuilder;
            _callChainProfile = callChainProfile;
            _targetDetails = targetDetails;
            _pageSize = _targetDetails.Architecture switch
            {
                TargetArchitecture.X86 => 0x00001000,
                TargetArchitecture.X64 => 0x00010000,
                TargetArchitecture.ARM => 0x00001000,
                TargetArchitecture.ARM64 => 0x00010000,
                _ => throw new NotImplementedException(_targetDetails.Architecture.ToString())
            };
        }

        public void SaveProfile(string profileFileName)
        {
            Console.WriteLine("Emitting profile file: {0}", profileFileName);

            CalculateCallInfo();
            using (StreamWriter writer = new StreamWriter(profileFileName))
            {
                writer.WriteLine("CHARACTERISTIC            | PAIR COUNT | CALL COUNT | PERCENTAGE");
                writer.WriteLine("----------------------------------------------------------------");
                int callCount = _callInfo.Sum(info => info.CallCount);
                double percentFactor = 100.0 / Math.Max(callCount, 1);
                writer.WriteLine("ENTRIES TOTAL             | {0,10} | {1,10} | {2,10:F2}", _callInfo.Count, callCount, callCount * percentFactor);
                int resolvedPairCount = _callInfo.Sum(info => info.CallType == CrossPageCall.Unresolved ? 0 : 1);
                int resolvedCallCount = _callInfo.Sum(info => info.CallType == CrossPageCall.Unresolved ? 0 : info.CallCount);
                writer.WriteLine("RESOLVED ENTRIES          | {0,10} | {1,10} | {2,10:F2}", resolvedPairCount, resolvedCallCount, resolvedCallCount * percentFactor);
                int unresolvedPairCount = _callInfo.Count - resolvedPairCount;
                int unresolvedCallCount = callCount - resolvedCallCount;
                writer.WriteLine("UNRESOLVED ENTRIES        | {0,10} | {1,10} | {2,10:F2}", unresolvedPairCount, unresolvedCallCount, unresolvedCallCount * percentFactor);
                int nearPairCount = _callInfo.Sum(info => info.CallType == CrossPageCall.No ? 1 : 0);
                int nearCallCount = _callInfo.Sum(info => info.CallType == CrossPageCall.No ? info.CallCount : 0);
                writer.WriteLine("NEAR (INTRA-PAGE) CALLS   | {0,10} | {1,10} | {2,10:F2}", nearPairCount, nearCallCount, nearCallCount * percentFactor);
                int farPairCount = _callInfo.Sum(info => info.CallType == CrossPageCall.Yes ? 1 : 0);
                int farCallCount = _callInfo.Sum(info => info.CallType == CrossPageCall.Yes ? info.CallCount : 0);
                writer.WriteLine("FAR (CROSS-PAGE) CALLS    | {0,10} | {1,10} | {2,10:F2}", farPairCount, farCallCount, farCallCount * percentFactor);

                writer.WriteLine();
                writer.WriteLine("CALLER RVA | CALLER LEN | CALLEE RVA | CALLEE LEN |      COUNT | FAR (CROSS-PAGE) CALLS (CALLER -> CALLEE)");
                writer.WriteLine("----------------------------------------------------------------------------------------------------------");
                DumpCallInfo(writer, _callInfo.Where(info => info.CallType == CrossPageCall.Yes).OrderByDescending(info => info.CallCount));

                writer.WriteLine();
                writer.WriteLine("CALLER RVA | CALLER LEN | CALLEE RVA | CALLEE LEN |      COUNT | NEAR (INTRA-PAGE) CALLS (CALLER -> CALLEE)");
                writer.WriteLine("-----------------------------------------------------------------------------------------------------------");
                DumpCallInfo(writer, _callInfo.Where(info => info.CallType == CrossPageCall.No).OrderByDescending(info => info.CallCount));
            }
        }

        private void DumpCallInfo(StreamWriter writer, IEnumerable<CallInfo> callInfos)
        {
            foreach (CallInfo callInfo in callInfos)
            {
                writer.Write($@"{callInfo.CallerRVA,10:X8} | ");
                writer.Write($@"{callInfo.CallerNode.Length,10:X8} | ");
                writer.Write($@"{callInfo.CalleeRVA,10:X8} | ");
                writer.Write($@"{callInfo.CalleeNode.Length,10:X8} | ");
                writer.Write($@"{callInfo.CallCount,10} | ");
                writer.WriteLine($@"{callInfo.Caller.ToString()} -> {callInfo.Callee.ToString()}");
            }
        }

        private void CalculateSymbolMethodMap()
        {
            if (_symbolMethodMap != null)
            {
                // Already calculated
                return;
            }
            _symbolMethodMap = new Dictionary<MethodDesc, ISymbolDefinitionNode>();
            foreach (KeyValuePair<ISymbolDefinitionNode, MethodWithGCInfo> kvpSymbolMethod in _outputInfoBuilder.MethodSymbolMap)
            {
                _symbolMethodMap.Add(kvpSymbolMethod.Value.Method, kvpSymbolMethod.Key);
            }
        }

        private void CalculateCallInfo()
        {
            if (_callInfo != null)
            {
                // Already calculated
                return;
            }

            CalculateSymbolMethodMap();

            _callInfo = new List<CallInfo>();
            foreach (KeyValuePair<MethodDesc, Dictionary<MethodDesc, int>> kvpCallerCalleeCount in _callChainProfile.ResolvedProfileData)
            {
                OutputNode callerNode = null;
                int callerRVA = 0;
                if (_symbolMethodMap.TryGetValue(kvpCallerCalleeCount.Key, out ISymbolDefinitionNode callerSymbol) &&
                    _outputInfoBuilder.NodeSymbolMap.TryGetValue(callerSymbol, out callerNode))
                {
                    callerRVA = _outputInfoBuilder.Sections[callerNode.SectionIndex].RVAWhenPlaced + callerNode.Offset;
                }

                foreach (KeyValuePair<MethodDesc, int> kvpCalleeCount in kvpCallerCalleeCount.Value)
                {
                    OutputNode calleeNode = null;
                    int calleeRVA = 0;
                    if (_symbolMethodMap.TryGetValue(kvpCalleeCount.Key, out ISymbolDefinitionNode calleeSymbol) &&
                        _outputInfoBuilder.NodeSymbolMap.TryGetValue(calleeSymbol, out calleeNode))
                    {
                        calleeRVA = _outputInfoBuilder.Sections[calleeNode.SectionIndex].RVAWhenPlaced + calleeNode.Offset;
                    }

                    _callInfo.Add(new CallInfo(
                        caller: kvpCallerCalleeCount.Key,
                        callerNode: callerNode,
                        callerRVA: callerRVA,
                        callee: kvpCalleeCount.Key,
                        calleeNode: calleeNode,
                        calleeRVA: calleeRVA,
                        callCount: kvpCalleeCount.Value,
                        callType: GetCallType(callerNode, callerRVA, calleeNode, calleeRVA)));
                }
            }
        }

        private CrossPageCall GetCallType(OutputNode caller, int callerRVA, OutputNode callee, int calleeRVA)
        {
            if (caller == null || callee == null)
            {
                return CrossPageCall.Unresolved;
            }
            int callerStartPage = callerRVA / _pageSize;
            int callerEndPage = (callerRVA + caller.Length - 1) / _pageSize;
            int calleePage = calleeRVA / _pageSize;

            if (callerStartPage == calleePage && callerEndPage == calleePage)
            {
                // The entire caller and the callee entrypoint are on the same page, no cross-page call penalty
                return CrossPageCall.No;
            }

            // Pessimistic estimate - we don't know where exactly the call is, we just know that it might cross a page.
            return CrossPageCall.Yes;
        }
    }
}
