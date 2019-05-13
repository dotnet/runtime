// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Concurrent;
using System.Text.Json.Serialization.Policies;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class DefaultIEnumerableConstructibleConverter : JsonEnumerableConverter
    {
        public static ConcurrentDictionary<Type, JsonPropertyInfo> s_objectJsonProperties = new ConcurrentDictionary<Type, JsonPropertyInfo>();

        public override IEnumerable CreateFromList(ref ReadStack state, IList sourceList, JsonSerializerOptions options)
        {
            Type enumerableType = state.Current.JsonPropertyInfo.RuntimePropertyType;
            JsonClassInfo elementClassInfo = state.Current.JsonPropertyInfo.ElementClassInfo;

            JsonPropertyInfo propertyInfo;
            if (elementClassInfo.ClassType == ClassType.Object)
            {
                Type objectType = elementClassInfo.Type;

                if (s_objectJsonProperties.ContainsKey(objectType))
                {
                    propertyInfo = s_objectJsonProperties[objectType];
                }
                else
                {
                    propertyInfo = JsonClassInfo.CreateProperty(objectType, objectType, null, typeof(object), options);
                    s_objectJsonProperties[objectType] = propertyInfo;
                }
            }
            else
            {
                propertyInfo = elementClassInfo.GetPolicyProperty();
            }

            return propertyInfo.CreateIEnumerableConstructibleType(enumerableType, sourceList);
        }
    }
}
