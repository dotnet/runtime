// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json.Serialization
{
    public static partial class JsonSerializer
    {
        private static void HandleStartObject(JsonSerializerOptions options, ref ReadStack state)
        {
            if (state.Current.Skip())
            {
                state.Push();
                state.Current.Drain = true;
                return;
            }

            if (state.Current.IsEnumerable() || state.Current.IsPropertyEnumerable())
            {
                // An array of objects either on the current property or on a list
                Type objType = state.Current.GetElementType();
                state.Push();
                state.Current.JsonClassInfo = options.GetOrAddClass(objType);
            }
            else if (state.Current.JsonPropertyInfo != null)
            {
                // Nested object
                Type objType = state.Current.JsonPropertyInfo.PropertyType;
                state.Push();
                state.Current.JsonClassInfo = options.GetOrAddClass(objType);
            }

            JsonClassInfo classInfo = state.Current.JsonClassInfo;
            state.Current.ReturnValue = classInfo.CreateObject();
        }

        private static bool HandleEndObject(JsonSerializerOptions options, ref ReadStack state)
        {
            bool isLastFrame = state.IsLastFrame;
            if (state.Current.Drain)
            {
                state.Pop();
                return isLastFrame;
            }

            object value = state.Current.ReturnValue;

            if (isLastFrame)
            {
                state.Current.Reset();
                state.Current.ReturnValue = value;
                return true;
            }

            state.Pop();
            ReadStackFrame.SetReturnValue(value, options, ref state.Current);
            return false;
        }
    }
}
