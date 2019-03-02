// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Text.Json.Serialization.Policies;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class DefaultArrayConverter : JsonEnumerableConverter
    {
        public override IEnumerable CreateFromList(Type elementType, IList sourceList)
        {
            Array array;

            if (sourceList.Count > 0 && sourceList[0] is Array probe)
            {
                array = Array.CreateInstance(probe.GetType(), sourceList.Count);

                int i = 0;
                foreach (IList child in sourceList)
                {
                    if (child is Array childArray)
                    {
                        array.SetValue(childArray, i++);
                    }
                }
            }
            else
            {
                array = Array.CreateInstance(elementType, sourceList.Count);
                sourceList.CopyTo(array, 0);
            }

            return array;
        }
    }
}
