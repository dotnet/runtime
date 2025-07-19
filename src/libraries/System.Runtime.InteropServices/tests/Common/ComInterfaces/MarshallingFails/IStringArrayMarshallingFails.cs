// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces.MarshallingFails
{
    /// <summary>
    /// Has methods that marshal a string array in different ways
    /// </summary>
    [GeneratedComInterface(StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(StringMarshallingFails))]
    [Guid("BE11C211-76D5-496F-A117-82F5D13208F7")]
    internal partial interface IStringArrayMarshallingFails
    {
        public void Param([MarshalUsing(ConstantElementCount = 10)] string[] value);
        public void InParam([MarshalUsing(ConstantElementCount = 10)] in string[] value);
        public void RefParam([MarshalUsing(ConstantElementCount = 10)] ref string[] value);
        public void OutParam([MarshalUsing(ConstantElementCount = 10)] out string[] value);
        public void ByValueOutParam([MarshalUsing(ConstantElementCount = 10)][Out] string[] value);
        public void ByValueInOutParam([MarshalUsing(ConstantElementCount = 10)][In, Out] string[] value);
        [return: MarshalUsing(ConstantElementCount = 10)]
        public string[] ReturnValue();
    }

    /// <summary>
    /// Implements IStringArrayMarshallingFails.
    /// </summary>
    [GeneratedComClass]
    internal partial class IStringArrayMarshallingFailsImpl : IStringArrayMarshallingFails
    {
        public static string[] StartingStrings { get; } = new string[] { "Hello", "World", "Lorem", "Ipsum", "Dolor", "Sample", "Text", ".Net", "Interop", "string" };
        private string[] _strings = StartingStrings;
        public void ByValueInOutParam([In, MarshalUsing(ConstantElementCount = 10), Out] string[] value) => value[0] = _strings[0];
        public void ByValueOutParam([MarshalUsing(ConstantElementCount = 10), Out] string[] value) => value = _strings;
        public void InParam([MarshalUsing(ConstantElementCount = 10)] in string[] value) => value[0] = _strings[0];
        public void OutParam([MarshalUsing(ConstantElementCount = 10)] out string[] value) => value = _strings;
        public void Param([MarshalUsing(ConstantElementCount = 10)] string[] value) => value[0] = _strings[0];
        public void RefParam([MarshalUsing(ConstantElementCount = 10)] ref string[] value) => value[0] = _strings[0];
        [return: MarshalUsing(ConstantElementCount = 10)]
        public string[] ReturnValue() => _strings;
    }

    /// <summary>
    /// Marshals and unmarshals elements of string arrays, throwing an exception instead of marshalling the element number <see cref="ThrowOnNthMarshalledElement"/>
    /// </summary>
    [CustomMarshaller(typeof(string), MarshalMode.ElementIn, typeof(StringMarshallingFails))]
    [CustomMarshaller(typeof(string), MarshalMode.ElementOut, typeof(StringMarshallingFails))]
    [CustomMarshaller(typeof(string), MarshalMode.ElementRef, typeof(StringMarshallingFails))]
    internal static unsafe class StringMarshallingFails
    {
        static int _marshalledCount = 0;
        const int ThrowOnNthMarshalledElement = 4;
        public static nint ConvertToUnmanaged(string managed)
        {
            if (++_marshalledCount == ThrowOnNthMarshalledElement)
            {
                _marshalledCount = 0;
                throw new MarshallingFailureException("This marshaller throws on the Nth element marshalled where N = " + ThrowOnNthMarshalledElement);
            }
            return (nint)Utf8StringMarshaller.ConvertToUnmanaged(managed);
        }

        public static string ConvertToManaged(nint unmanaged)
        {
            return Utf8StringMarshaller.ConvertToManaged((byte*)unmanaged);
        }
    }
}
