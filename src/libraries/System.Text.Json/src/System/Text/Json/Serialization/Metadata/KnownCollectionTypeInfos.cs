// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Converters;
using System.Threading.Tasks;

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// todo
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class KnownCollectionTypeInfos<T>
    {
        private static JsonTypeInfo<List<T>>? s_list;
        /// <summary>
        /// todo
        /// </summary>
        public static JsonTypeInfo<List<T>> GetList(JsonClassInfo elementInfo, JsonSerializerContext context)
        {
            // todo: support obtaining existing converter
            //if (context._options.HasCustomConverters)
            //{
                //JsonConverter<List<T>> converter = (JsonConverter<List<T>>)context._options.GetConverter(typeof(List<T>));
                //return new JsonCollectionTypeInfo<List<T>>(CreateList, converter, elementInfo, context._options);
            //}

            if (s_list == null)
            {
                s_list = new JsonCollectionTypeInfo<List<T>>(CreateList, new ListOfTConverter<List<T>, T>(), elementInfo, context._options);
            }

            return s_list;
        }

        private static List<T> CreateList()
        {
            return new List<T>();
        }

        // todo: duplicate the above code for each supported collection type (IEnumerable, IEnumerable<T>, array, etc)
    }
}
