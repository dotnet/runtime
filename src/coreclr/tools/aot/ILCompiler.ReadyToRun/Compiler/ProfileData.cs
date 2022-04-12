// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using ILCompiler.IBC;

using Internal.Pgo;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler
{
    [Flags]
    public enum MethodProfilingDataFlags
    {
        // Important: update toolbox\ibcmerge\ibcmerge.cs if you change these
        ReadMethodCode = 0,  // 0x00001  // Also means the method was executed
        ReadMethodDesc = 1,  // 0x00002
        RunOnceMethod = 2,  // 0x00004
        RunNeverMethod = 3,  // 0x00008
                             //  MethodStoredDataAccess        = 4,  // 0x00010  // obsolete
        WriteMethodDesc = 5,  // 0x00020
                              //  ReadFCallHash                 = 6,  // 0x00040  // obsolete
        ReadGCInfo = 7,  // 0x00080
        CommonReadGCInfo = 8,  // 0x00100
                               //  ReadMethodDefRidMap           = 9,  // 0x00200  // obsolete
        ReadCerMethodList = 10, // 0x00400
        ReadMethodPrecode = 11, // 0x00800
        WriteMethodPrecode = 12, // 0x01000
        ExcludeHotMethodCode = 13, // 0x02000  // Hot method should be excluded from the ReadyToRun image
        ExcludeColdMethodCode = 14, // 0x04000  // Cold method should be excluded from the ReadyToRun image
        DisableInlining = 15, // 0x08000  // Disable inlining of this method in optimized AOT native code
    }

    public class MethodProfileData
    {
        public MethodProfileData(MethodDesc method, MethodProfilingDataFlags flags, double exclusiveWeight, Dictionary<MethodDesc, int> callWeights, uint scenarioMask, PgoSchemaElem[] schemaData)
        {
            if (method == null)
                throw new ArgumentNullException("method");

            Method = method;
            Flags = flags;
            ScenarioMask = scenarioMask;
            ExclusiveWeight = exclusiveWeight;
            CallWeights = callWeights;
            SchemaData = schemaData;
        }

        public readonly MethodDesc Method;
        public readonly MethodProfilingDataFlags Flags;
        public readonly uint ScenarioMask;
        public readonly double ExclusiveWeight;
        public readonly Dictionary<MethodDesc, int> CallWeights;
        public readonly PgoSchemaElem[] SchemaData;
    }

    public abstract class ProfileData
    {
        public abstract bool PartialNGen { get; }
        public abstract MethodProfileData GetMethodProfileData(MethodDesc m);
        public abstract IEnumerable<MethodProfileData> GetAllMethodProfileData();
        public abstract byte[] GetMethodBlockCount(MethodDesc m);

        public static void MergeProfileData(ref bool partialNgen, Dictionary<MethodDesc, MethodProfileData> mergedProfileData, ProfileData profileData)
        {
            if (profileData.PartialNGen)
                partialNgen = true;

            PgoSchemaElem[][] schemaElemMergerArray = new PgoSchemaElem[2][];

            foreach (MethodProfileData data in profileData.GetAllMethodProfileData())
            {
                MethodProfileData dataToMerge;
                if (mergedProfileData.TryGetValue(data.Method, out dataToMerge))
                {
                    var mergedCallWeights = data.CallWeights;
                    if (mergedCallWeights == null)
                    {
                        mergedCallWeights = dataToMerge.CallWeights;
                    }
                    else if (dataToMerge.CallWeights != null)
                    {
                        mergedCallWeights = new Dictionary<MethodDesc, int>(data.CallWeights);
                        foreach (var entry in dataToMerge.CallWeights)
                        {
                            if (mergedCallWeights.TryGetValue(entry.Key, out var initialWeight))
                            {
                                mergedCallWeights[entry.Key] = initialWeight + entry.Value;
                            }
                            else
                            {
                                mergedCallWeights[entry.Key] = entry.Value;
                            }
                        }
                    }

                    PgoSchemaElem[] mergedSchemaData;
                    if (data.SchemaData == null)
                    {
                        mergedSchemaData = dataToMerge.SchemaData;
                    }
                    else if (dataToMerge.SchemaData == null)
                    {
                        mergedSchemaData = data.SchemaData;
                    }
                    else
                    {
                        // Actually merge
                        schemaElemMergerArray[0] = dataToMerge.SchemaData;
                        schemaElemMergerArray[1] = data.SchemaData;
                        mergedSchemaData = PgoProcessor.Merge<TypeSystemEntityOrUnknown, TypeSystemEntityOrUnknown>(schemaElemMergerArray);
                    }
                    mergedProfileData[data.Method] = new MethodProfileData(data.Method, dataToMerge.Flags | data.Flags, data.ExclusiveWeight + dataToMerge.ExclusiveWeight, mergedCallWeights, dataToMerge.ScenarioMask | data.ScenarioMask, mergedSchemaData);
                }
                else
                {
                    mergedProfileData.Add(data.Method, data);
                }
            }
        }
    }

    public class EmptyProfileData : ProfileData
    {
        private static readonly EmptyProfileData s_singleton = new EmptyProfileData();

        private EmptyProfileData()
        {
        }

        public override bool PartialNGen => false;

        public static EmptyProfileData Singleton => s_singleton;

        public override MethodProfileData GetMethodProfileData(MethodDesc m)
        {
            return null;
        }

        public override IEnumerable<MethodProfileData> GetAllMethodProfileData()
        {
            return Array.Empty<MethodProfileData>();
        }

        public override byte[] GetMethodBlockCount(MethodDesc m)
        {
            return null;
        }
    }
}
