// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class ListChangedEventArgsTests
    {
        [Theory]
        [InlineData(ListChangedType.ItemAdded, -1)]
        [InlineData(ListChangedType.ItemChanged, 0)]
        [InlineData(ListChangedType.ItemDeleted, 1)]
        [InlineData(ListChangedType.ItemMoved, 2)]
        [InlineData(ListChangedType.PropertyDescriptorAdded, 3)]
        [InlineData(ListChangedType.PropertyDescriptorDeleted, 4)]
        [InlineData(ListChangedType.PropertyDescriptorChanged, 5)]
        [InlineData(ListChangedType.Reset, 6)]
        [InlineData(ListChangedType.Reset - 1, 7)]
        [InlineData(ListChangedType.PropertyDescriptorChanged + 1, 8)]
        public void Ctor_ListChangedType_Int(ListChangedType listChangedType, int newIndex)
        {
            var args = new ListChangedEventArgs(listChangedType, newIndex);
            Assert.Equal(listChangedType, args.ListChangedType);
            Assert.Equal(newIndex, args.NewIndex);
            Assert.Equal(-1, args.OldIndex);
            Assert.Null(args.PropertyDescriptor);
        }

        public static IEnumerable<object[]> Ctor_ListChangedType_PropertyDescriptor_TestData()
        {
            yield return new object[] { ListChangedType.ItemAdded, null };
            yield return new object[] { ListChangedType.ItemChanged, new MockPropertyDescriptor() };
            yield return new object[] { ListChangedType.ItemDeleted, new MockPropertyDescriptor() };
            yield return new object[] { ListChangedType.ItemMoved, new MockPropertyDescriptor() };
            yield return new object[] { ListChangedType.PropertyDescriptorAdded, new MockPropertyDescriptor() };
            yield return new object[] { ListChangedType.PropertyDescriptorDeleted, new MockPropertyDescriptor() };
            yield return new object[] { ListChangedType.PropertyDescriptorChanged, new MockPropertyDescriptor() };
            yield return new object[] { ListChangedType.Reset, new MockPropertyDescriptor() };
            yield return new object[] { ListChangedType.Reset - 1, new MockPropertyDescriptor() };
            yield return new object[] { ListChangedType.PropertyDescriptorChanged + 1, new MockPropertyDescriptor() };
        }

        [Theory]
        [MemberData(nameof(Ctor_ListChangedType_PropertyDescriptor_TestData))]
        public void Ctor_ListChangedType_PropertyDescriptor(ListChangedType listChangedType, PropertyDescriptor propDesc)
        {
            var args = new ListChangedEventArgs(listChangedType, propDesc);
            Assert.Equal(listChangedType, args.ListChangedType);
            Assert.Equal(0, args.NewIndex);
            Assert.Equal(0, args.OldIndex);
            Assert.Same(propDesc, args.PropertyDescriptor);
        }

        [Theory]
        [InlineData(ListChangedType.ItemAdded, -1, 0)]
        [InlineData(ListChangedType.ItemChanged, 0, 1)]
        [InlineData(ListChangedType.ItemDeleted, 1, 2)]
        [InlineData(ListChangedType.ItemMoved, 2, 3)]
        [InlineData(ListChangedType.PropertyDescriptorAdded, 3, 4)]
        [InlineData(ListChangedType.PropertyDescriptorDeleted, 4, 5)]
        [InlineData(ListChangedType.PropertyDescriptorChanged, 5, 6)]
        [InlineData(ListChangedType.Reset, 6, 7)]
        [InlineData(ListChangedType.Reset - 1, 7, 8)]
        [InlineData(ListChangedType.PropertyDescriptorChanged + 1, 8, -1)]
        public void Ctor_ListChangedType_Int_Int(ListChangedType listChangedType, int newIndex, int oldIndex)
        {
            var args = new ListChangedEventArgs(listChangedType, newIndex, oldIndex);
            Assert.Equal(listChangedType, args.ListChangedType);
            Assert.Equal(newIndex, args.NewIndex);
            Assert.Equal(oldIndex, args.OldIndex);
            Assert.Null(args.PropertyDescriptor);
        }

        public static IEnumerable<object[]> Ctor_ListChangedType_Int_PropertyDescriptor_TestData()
        {
            yield return new object[] { ListChangedType.ItemAdded, -1, null };
            yield return new object[] { ListChangedType.ItemChanged, 0, new MockPropertyDescriptor() };
            yield return new object[] { ListChangedType.ItemDeleted, 1, new MockPropertyDescriptor() };
            yield return new object[] { ListChangedType.ItemMoved, 2, new MockPropertyDescriptor() };
            yield return new object[] { ListChangedType.PropertyDescriptorAdded, 3, new MockPropertyDescriptor() };
            yield return new object[] { ListChangedType.PropertyDescriptorDeleted, 4, new MockPropertyDescriptor() };
            yield return new object[] { ListChangedType.PropertyDescriptorChanged, 5, new MockPropertyDescriptor() };
            yield return new object[] { ListChangedType.Reset, 6, new MockPropertyDescriptor() };
            yield return new object[] { ListChangedType.Reset - 1, 7, new MockPropertyDescriptor() };
            yield return new object[] { ListChangedType.PropertyDescriptorChanged + 1, 8, new MockPropertyDescriptor() };
        }

        [Theory]
        [MemberData(nameof(Ctor_ListChangedType_Int_PropertyDescriptor_TestData))]
        public void Ctor_ListChangedType_Int_PropertyDescriptor(ListChangedType listChangedType, int newIndex, PropertyDescriptor propDesc)
        {
            var args = new ListChangedEventArgs(listChangedType, newIndex, propDesc);
            Assert.Equal(listChangedType, args.ListChangedType);
            Assert.Equal(newIndex, args.NewIndex);
            Assert.Equal(newIndex, args.OldIndex);
            Assert.Same(propDesc, args.PropertyDescriptor);
        }
    }
}
