// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Reflection;

#if ES_BUILD_STANDALONE
namespace Microsoft.Diagnostics.Tracing
#else
namespace System.Diagnostics.Tracing
#endif
{
    /// <summary>
    /// TraceLogging: Each PropertyAccessor instance encapsulates the information
    /// needed to read a particular property from an instance of ContainerType
    /// and write the value to a DataCollector. Used by InvokeTypeInfo.
    /// </summary>
    /// <typeparam name="ContainerType">
    /// The type of the object from which properties are read.
    /// </typeparam>
    internal abstract class PropertyAccessor<ContainerType>
    {
        public abstract void Write(TraceLoggingDataCollector collector, ref ContainerType value);
        public abstract object GetData(ContainerType value);

        public static PropertyAccessor<ContainerType> Create(PropertyAnalysis property)
        {
            // Due to current Project N limitations on handling generic instantiations with
            // 2 generic parameters we have to explicitly create the instantiations that we consider 
            // important to EventSource performance (we have considered int, long, string for the moment). 
            // Everything else is handled by NonGenericPropertyWriter that ends up boxing the container object. 
            var retType = property.getterInfo.ReturnType;
            if (!Statics.IsValueType(typeof(ContainerType)))
            {
                if (retType == typeof(int))
                    return new ClassPropertyWriter<ContainerType, int>(property);
                else if (retType == typeof(long))
                    return new ClassPropertyWriter<ContainerType, long>(property);
                else if (retType == typeof(string))
                    return new ClassPropertyWriter<ContainerType, string>(property);
            }
            else
            {
                // Handle the case if it is a struct (DD 1027919)
            }

            // Otherwise use the boxing one.  
            return new NonGenericProperytWriter<ContainerType>(property);
        }
    }

    /// <summary>
    /// The type specific version of the property writers uses generics in a way 
    /// that Project N can't handle at the moment.   To avoid this we simply 
    /// use reflection completely.  
    /// </summary>
    internal class NonGenericProperytWriter<ContainerType> : PropertyAccessor<ContainerType>
    {
        public NonGenericProperytWriter(PropertyAnalysis property)
        {
            getterInfo = property.getterInfo;
            typeInfo = property.typeInfo;
        }

        public override void Write(TraceLoggingDataCollector collector, ref ContainerType container)
        {
            object value = container == null
                ? null
                : getterInfo.Invoke((object)container, null);
            this.typeInfo.WriteObjectData(collector, value);
        }

        public override object GetData(ContainerType container)
        {
            return container == null
                ? default(ValueType)
                : getterInfo.Invoke((object)container, null);
        }

        private readonly TraceLoggingTypeInfo typeInfo;
        private readonly MethodInfo getterInfo;
    }

    /// <summary>
    /// Implementation of PropertyAccessor for use when ContainerType is a
    /// value type.
    /// </summary>
    /// <typeparam name="ContainerType">The type of the object from which properties are read.</typeparam>
    /// <typeparam name="ValueType">Type of the property being read.</typeparam>
    internal class StructPropertyWriter<ContainerType, ValueType>
            : PropertyAccessor<ContainerType>
    {
        private delegate ValueType Getter(ref ContainerType container);
        private readonly TraceLoggingTypeInfo<ValueType> valueTypeInfo;
        private readonly Getter getter;

        public StructPropertyWriter(PropertyAnalysis property)
        {
            this.valueTypeInfo = (TraceLoggingTypeInfo<ValueType>)property.typeInfo;
            this.getter = (Getter)Statics.CreateDelegate(
                typeof(Getter),
                property.getterInfo);
        }

        public override void Write(TraceLoggingDataCollector collector, ref ContainerType container)
        {
            var value = container == null
                ? default(ValueType)
                : getter(ref container);
            this.valueTypeInfo.WriteData(collector, ref value);
        }

        public override object GetData(ContainerType container)
        {
            return container == null
                ? default(ValueType)
                : getter(ref container);
        }
    }

    /// <summary>
    /// Implementation of PropertyAccessor for use when ContainerType is a
    /// reference type.
    /// </summary>
    /// <typeparam name="ContainerType">The type of the object from which properties are read.</typeparam>
    /// <typeparam name="ValueType">Type of the property being read.</typeparam>
    internal class ClassPropertyWriter<ContainerType, ValueType>
            : PropertyAccessor<ContainerType>
    {
        private delegate ValueType Getter(ContainerType container);
        private readonly TraceLoggingTypeInfo<ValueType> valueTypeInfo;
        private readonly Getter getter;

        public ClassPropertyWriter(PropertyAnalysis property)
        {
            this.valueTypeInfo = (TraceLoggingTypeInfo<ValueType>)property.typeInfo;
            this.getter = (Getter)Statics.CreateDelegate(
                typeof(Getter),
                property.getterInfo);
        }

        public override void Write(TraceLoggingDataCollector collector, ref ContainerType container)
        {
            var value = container == null
                ? default(ValueType)
                : getter(container);
            this.valueTypeInfo.WriteData(collector, ref value);
        }

        public override object GetData(ContainerType container)
        {
            return container == null
                ? default(ValueType)
                : getter(container);
        }
    }
}
