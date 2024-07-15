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

    internal DacStreams_1(Target target)
    {
        _target = target;
    }

    public virtual string? StringFromEEAddress(TargetPointer address)
    {
        TargetPointer miniMetaDataBuffAddress = _target.ReadPointer(_target.ReadGlobalPointer(Constants.Globals.MiniMetaDataBuffAddress));
        uint miniMetaDataBuffMaxSize = _target.Read<uint>(_target.ReadGlobalPointer(Constants.Globals.MiniMetaDataBuffMaxSize));

        // We use the data subsystem to handle caching results from processing this data
        var dictionary = _target.ProcessedData.GetOrAdd<(TargetPointer, uint), DacStreams_1_Data>((miniMetaDataBuffAddress, miniMetaDataBuffMaxSize)).EEObjectToString;

        dictionary.TryGetValue(address, out string? result);
        return result;
    }

    internal class DacStreams_1_Data : IData<DacStreams_1_Data, (TargetPointer, uint)>
    {
        static DacStreams_1_Data IData<DacStreams_1_Data, (TargetPointer, uint)>.Create(Target target, (TargetPointer, uint) key) => new DacStreams_1_Data(target, key);

        public DacStreams_1_Data(Target target, (TargetPointer miniMetaDataBuffAddress, uint miniMetaDataBuffMaxSize) key)
        {
            EEObjectToString = GetEEAddressToStringMap(target, key.miniMetaDataBuffAddress, key.miniMetaDataBuffMaxSize);
        }

        public readonly Dictionary<TargetPointer, string> EEObjectToString;

        internal static Dictionary<TargetPointer, string> GetEEAddressToStringMap(Target target, TargetPointer miniMetaDataBuffAddress, uint miniMetaDataBuffMaxSize)
        {
            Dictionary<TargetPointer, string> stringToAddress = new();
            if (miniMetaDataBuffMaxSize < 20)
            {
                // buffer isn't long enough to hold required headers
                return stringToAddress;
            }

            if (target.Read<uint>(miniMetaDataBuffAddress) != 0x6d727473)
            {
                // Magic number is incorrect
                return stringToAddress;
            }


            uint totalSize = target.Read<uint>(miniMetaDataBuffAddress + 0x4);
            if (totalSize > miniMetaDataBuffMaxSize)
            {
                // totalSize is inconsistent with miniMetaDataBuffMaxSize
                return stringToAddress;
            }

            byte[] bytes = new byte[totalSize];
            ReadOnlySpan<byte> miniMdBuffer = bytes.AsSpan();
            target.ReadBuffer(miniMetaDataBuffAddress, bytes);
            uint countStreams = target.Read<uint>(miniMetaDataBuffAddress + 0x8);
            if (countStreams != 1)
            {
                // This implementation is only aware of 1 possible stream type, so only 1 can exist
                return stringToAddress;
            }
            uint eeNameSig = target.Read<uint>(miniMetaDataBuffAddress + 0xC);
            if (eeNameSig != 0x614e4545)
            {
                // name of first stream is not 0x614e4545 == "EENa"
                return stringToAddress;
            }
            uint countNames = target.Read<uint>(miniMetaDataBuffAddress + 0x10);

            uint currentOffset = 20;

            for (int i = 0; i < countNames; i++)
            {
                if ((currentOffset + target.PointerSize) > miniMetaDataBuffMaxSize)
                    break;
                TargetPointer eeObjectPointer = target.ReadPointer(miniMetaDataBuffAddress + currentOffset);
                currentOffset += (uint)target.PointerSize;
                int stringLen = miniMdBuffer.Slice((int)currentOffset).IndexOf((byte)0);
                if (stringLen == -1)
                    break;

                try
                {
                    string name = Encoding.UTF8.GetString(miniMdBuffer.Slice((int)currentOffset, stringLen));
                    stringToAddress.Add(eeObjectPointer, name);
                }
                catch
                {
                    // Tolerate malformed strings without causing all lookups to fail
                }

                currentOffset += (uint)stringLen + 1;
            }

            return stringToAddress;
        }
    }
}
