// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        // There are three conditions to consider for an object (primitive value, enumerable or object) being processed here:
        // 1) The object type was specified as the root-level return type to a Deserialize method.
        // 2) The object is a property on a parent object.
        // 3) The object is an element in an enumerable.
        private static bool Write(
            Utf8JsonWriter writer,
            int originalWriterDepth,
            int flushThreshold,
            JsonSerializerOptions options,
            ref WriteStack state)
        {
            bool finishedSerializing;

            try
            {
                do
                {
                    switch (state.Current.JsonClassInfo.ClassType)
                    {
                        case ClassType.Enumerable:
                            finishedSerializing = HandleEnumerable(state.Current.JsonClassInfo.ElementClassInfo, options, writer, ref state);
                            break;
                        case ClassType.Value:
                            Debug.Assert(state.Current.JsonPropertyInfo.ClassType == ClassType.Value);
                            state.Current.JsonPropertyInfo.Write(ref state, writer);
                            finishedSerializing = true;
                            break;
                        case ClassType.Dictionary:
                        case ClassType.IDictionaryConstructible:
                            finishedSerializing = HandleDictionary(state.Current.JsonClassInfo.ElementClassInfo, options, writer, ref state);
                            break;
                        default:
                            Debug.Assert(state.Current.JsonClassInfo.ClassType == ClassType.Object ||
                                state.Current.JsonClassInfo.ClassType == ClassType.Unknown);

                            finishedSerializing = WriteObject(options, writer, ref state);
                            break;
                    }

                    if (finishedSerializing)
                    {
                        if (writer.CurrentDepth == originalWriterDepth)
                        {
                            break;
                        }
                    }
                    else if (writer.CurrentDepth >= options.EffectiveMaxDepth)
                    {
                        ThrowHelper.ThrowInvalidOperationException_SerializerCycleDetected(options.MaxDepth);
                    }

                    // If serialization is not finished and we surpass flush threshold then return false which will flush stream.
                    if (flushThreshold >= 0 && writer.BytesPending > flushThreshold)
                    {
                        return false;
                    }
                } while (true);
            }
            catch (InvalidOperationException ex) when (ex.Source == ThrowHelper.ExceptionSourceValueToRethrowAsJsonException)
            {
                ThrowHelper.ReThrowWithPath(state, ex);
            }
            catch (JsonException ex)
            {
                ThrowHelper.AddExceptionInformation(state, ex);
                throw;
            }

            return true;
        }
    }
}
