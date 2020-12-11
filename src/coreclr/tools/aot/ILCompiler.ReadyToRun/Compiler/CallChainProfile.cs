// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

using ILCompiler.IBC;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler
{
    // This is a sample of the Json format the call chain data is stored in:
    //
    // {
    //   "state_size": 5,
    //   "chain": [
    //     [
    //       [
    //         "mscorlib.ni.dll!System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture",
    //         "mscorlib.ni.dll!System.Runtime.ExceptionServices.ExceptionDispatchInfo..ctor",
    //         "clr.dll!ExceptionNative::GetStackTracesDeepCopy",
    //         "clr.dll!ExceptionObject::GetStackTrace",
    //         "clr.dll!SpinLock::GetLock"
    //       ],
    //       [
    //         [
    //           "clr.dll!SpinLock::SpinToAcquire"
    //         ],
    //         [
    //           1
    //         ]
    //       ]
    //     ],
    //     [
    //       [
    //         "System.Core.ni.dll!System.Collections.Generic.HashSet`1[System.__Canon]..ctor",
    //         "System.Core.ni.dll!System.Collections.Generic.HashSet`1[System.__Canon].UnionWith",
    //         "System.Core.ni.dll!System.Linq.Enumerable+WhereSelectEnumerableIterator`2[System.__Canon,System.__Canon].MoveNext",
    //         "System.Core.ni.dll!System.Collections.Generic.HashSet`1[System.__Canon].Contains",
    //         "System.Core.ni.dll!System.Collections.Generic.HashSet`1[System.__Canon].InternalGetHashCode"
    //       ],
    //       [
    //         [
    //           "___END__",
    //           "mscorlib.ni.dll!System.Collections.Generic.GenericEqualityComparer`1[System.__Canon].GetHashCode"
    //         ],
    //         [
    //           2,
    //           3
    //         ]
    //       ]
    //     ]
    //   ]
    // }
    public class CallChainProfile
    {
        Dictionary<List<string>, Dictionary<string, int>> _profileData = new Dictionary<List<string>, Dictionary<string, int>>();

        public CallChainProfile(string callChainProfileFile)
        {
            AddCallChainAnalysisData(callChainProfileFile);
            WriteMethodAnalysis();
        }

        private void AddCallChainAnalysisData(string jsonProfileFile)
        {
            using (StreamReader stream = File.OpenText(jsonProfileFile))
            using (JsonDocument document = JsonDocument.Parse(stream.BaseStream))
            {
                JsonElement root = document.RootElement;
                JsonElement chainsRoot = root.GetProperty("chain");

                foreach (JsonElement chain in chainsRoot.EnumerateArray())
                {
                    // Each chain contains 2 arrays: the key (of chain length), a list of methods which follow the chain, a list of counts for each respective method
                    List<string> keyParts = new List<string>();
                    Dictionary<string, int> followingMethodCounts = new Dictionary<string, int>();
                    bool readingKey = true;
                    foreach (JsonElement keyElement in chain.EnumerateArray())
                    {
                        if (readingKey)
                        {
                            // Build key method names
                            foreach (JsonElement keyPartElement in keyElement.EnumerateArray())
                            {
                                if (!keyPartElement.GetString().Equals("___BEGIN__"))
                                {
                                    keyParts.Add(keyPartElement.GetString());
                                }
                            }
                            readingKey = false;
                        }
                        else
                        {
                            bool readingMethodNames = true;
                            List<string> followingMethodList = new List<string>();
                            foreach (JsonElement methodListArray in keyElement.EnumerateArray())
                            {
                                if (readingMethodNames)
                                {
                                    // Read the array of methods which follow the "keyParts" chain
                                    foreach (JsonElement followingMethods in methodListArray.EnumerateArray())
                                    {
                                        followingMethodList.Add(followingMethods.GetString());

                                    }

                                    readingMethodNames = false;
                                }
                                else
                                {
                                    // Read the array of call counts
                                    int index = 0;
                                    foreach (JsonElement methodCount in methodListArray.EnumerateArray())
                                    {
                                        followingMethodCounts.Add(followingMethodList[index], methodCount.GetInt32());
                                        index++;
                                    }
                                }
                            }
                        }
                    }
                    _profileData.Add(keyParts, followingMethodCounts);
                }
            }
        }

        private void WriteMethodAnalysis()
        {
            Console.WriteLine($"Call chain key count: {_profileData.Keys.Count}");
            var systemNamespaceUseCount = _profileData.Keys.Count(key => key.Any(keyElement => keyElement.Contains("System.") || keyElement.Contains("Microsoft.")));
            Console.WriteLine($"Keys with framework types count: {systemNamespaceUseCount}");
        }
    }

}

