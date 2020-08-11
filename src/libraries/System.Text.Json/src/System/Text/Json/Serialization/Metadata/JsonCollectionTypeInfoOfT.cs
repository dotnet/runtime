// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// todo
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class JsonCollectionTypeInfo<T> : JsonTypeInfo<T>
    {
        /// <summary>
        /// todo
        /// </summary>
        /// <param name="createObjectFunc"></param>
        /// <param name="converter"></param>
        /// <param name="elementInfo"></param>
        /// <param name="options"></param>
        public JsonCollectionTypeInfo(
            ConstructorDelegate createObjectFunc,
            JsonConverter<T> converter,
            JsonClassInfo elementInfo,
            JsonSerializerOptions? options) : base(typeof(T), options, ClassType.Enumerable)
        {
            ConverterBase = converter;

            ElementType = converter.ElementType;
            ElementClassInfo = elementInfo;
            CreateObject = createObjectFunc;

            PropertyInfoForClassInfo = CreatePropertyInfoForClassInfo(Type, Type, converter, Options);
        }

        /// <summary>
        /// todo
        /// </summary>
        public void CompleteInitialization()
        {
            _isInitialized = true;

            //todo: should we not add?
            Options.AddJsonClassInfo(this);
        }
    }
}
