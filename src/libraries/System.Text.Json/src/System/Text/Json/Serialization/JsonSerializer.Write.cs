// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;

namespace System.Text.Json.Serialization
{
    public static partial class JsonSerializer
    {
        private static bool Write(
            ref JsonWriterState writerState,
            IBufferWriter<byte> bufferWriter,
            int flushThreshold,
            JsonSerializerOptions options,
            ref WriteStack state)
        {
            Utf8JsonWriter writer = new Utf8JsonWriter(bufferWriter, writerState);

            bool isFinalBlock = Write(
                ref writer,
                flushThreshold,
                options,
                ref state);

            writer.Flush(isFinalBlock: isFinalBlock);
            writerState = writer.GetCurrentState();

            return isFinalBlock;
        }

        private static bool Write(
            ref Utf8JsonWriter writer,
            int flushThreshold,
            JsonSerializerOptions options,
            ref WriteStack state)
        {
            bool continueWriting = true;
            bool finishedSerializing;
            do
            {
                switch (state.Current.JsonClassInfo.ClassType)
                {
                    case ClassType.Enumerable:
                        finishedSerializing = WriteEnumerable(options, ref writer, ref state);
                        break;
                    case ClassType.Object:
                        finishedSerializing = WriteObject(options, ref writer, ref state);
                        break;
                    default:
                        finishedSerializing = WriteValue(options, ref writer, ref state.Current);
                        break;
                }

                if (flushThreshold >= 0 && writer.BytesWritten > flushThreshold)
                {
                    return false;
                }

                if (finishedSerializing && writer.CurrentDepth == 0)
                {
                    continueWriting = false;
                }
            } while (continueWriting);

            return true;
        }        
    }
}
