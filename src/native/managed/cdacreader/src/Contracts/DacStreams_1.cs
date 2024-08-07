// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.DataContractReader.Data;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal class DacStreams_1 : IDacStreams
{
    private readonly Target _target;

    private const uint MiniMetadataSignature = 0x6d727473;
    private const uint EENameStreamSignature = 0x614e4545;

    private const uint MiniMetaDataStreamsHeaderSize = 12;
    private const uint MiniMetadataStream_MiniMetadataSignature_Offset = 0;
    private const uint MiniMetadataStream_TotalSize_Offset = 4;
    private const uint MiniMetadataStream_CountOfStreams_Offset = 8;

    private const uint EENameStreamHeaderSize = 8;
    private const uint EENameStream_EENameStreamSignature_Offset = 0;
    private const uint EENameStream_CountOfNames_Offset = 4;

    internal DacStreams_1(Target target)
    {
        _target = target;
    }

    public virtual string? StringFromEEAddress(TargetPointer address)
    {
        // We use the data subsystem to handle caching results from processing this data
        var dictionary = _target.ProcessedData.GetOrAdd<DacStreams_1_Data>(0).EEObjectToString;

        dictionary.TryGetValue(address, out string? result);
        return result;
    }

    internal class DacStreams_1_Data : IData<DacStreams_1_Data>
    {
        static DacStreams_1_Data IData<DacStreams_1_Data>.Create(Target target, TargetPointer address) => new DacStreams_1_Data(target);

        public DacStreams_1_Data(Target target)
        {
            EEObjectToString = GetEEAddressToStringMap(target);
        }

        public readonly Dictionary<TargetPointer, string> EEObjectToString;

        internal static Dictionary<TargetPointer, string> GetEEAddressToStringMap(Target target)
        {
            TargetPointer miniMetaDataBuffAddress = target.ReadPointer(target.ReadGlobalPointer(Constants.Globals.MiniMetaDataBuffAddress));
            uint miniMetaDataBuffMaxSize = target.Read<uint>(target.ReadGlobalPointer(Constants.Globals.MiniMetaDataBuffMaxSize));
            ulong miniMetaDataBuffEnd = miniMetaDataBuffAddress + miniMetaDataBuffMaxSize;

            Dictionary<TargetPointer, string> stringToAddress = new();
            if (miniMetaDataBuffMaxSize < 20)
            {
                // buffer isn't long enough to hold required headers
                return stringToAddress;
            }

            if (target.Read<uint>(miniMetaDataBuffAddress + MiniMetadataStream_MiniMetadataSignature_Offset) != MiniMetadataSignature)
            {
                // Magic number is incorrect
                return stringToAddress;
            }


            uint totalSize = target.Read<uint>(miniMetaDataBuffAddress + MiniMetadataStream_TotalSize_Offset);
            if (totalSize > miniMetaDataBuffMaxSize)
            {
                // totalSize is inconsistent with miniMetaDataBuffMaxSize
                return stringToAddress;
            }

            byte[] bytes = new byte[totalSize];
            ReadOnlySpan<byte> miniMdBuffer = bytes.AsSpan();
            target.ReadBuffer(miniMetaDataBuffAddress, bytes);
            uint countStreams = target.Read<uint>(miniMetaDataBuffAddress + MiniMetadataStream_CountOfStreams_Offset);
            if (countStreams != 1)
            {
                // This implementation is only aware of 1 possible stream type, so only 1 can exist
                return stringToAddress;
            }
            ulong eeNameStreamAddress = miniMetaDataBuffAddress + MiniMetaDataStreamsHeaderSize;
            uint eeNameSig = target.Read<uint>(eeNameStreamAddress + EENameStream_EENameStreamSignature_Offset);
            if (eeNameSig != EENameStreamSignature)
            {
                // name of first stream is not 0x614e4545 == "EENa"
                return stringToAddress;
            }
            uint countNames = target.Read<uint>(eeNameStreamAddress + EENameStream_CountOfNames_Offset);

            ulong currentAddress = eeNameStreamAddress + EENameStreamHeaderSize;

            for (int i = 0; i < countNames; i++)
            {
                if (currentAddress >= miniMetaDataBuffEnd)
                    break;
                TargetPointer eeObjectPointer = target.ReadPointer(currentAddress);
                currentAddress += (uint)target.PointerSize;
                int stringLen = miniMdBuffer.Slice((int)(currentAddress - miniMetaDataBuffAddress)).IndexOf((byte)0);
                if (stringLen == -1)
                    break;

                try
                {
                    string name = Encoding.UTF8.GetString(miniMdBuffer.Slice((int)(currentAddress - miniMetaDataBuffAddress), stringLen));
                    stringToAddress.Add(eeObjectPointer, name);
                }
                catch
                {
                    // Tolerate malformed strings without causing all lookups to fail
                }

                currentAddress += (uint)stringLen + 1;
            }

            return stringToAddress;
        }
    }
}
