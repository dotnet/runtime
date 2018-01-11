// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.UnitTesting;
using Xunit;

namespace System.ComponentModel.Composition.ReflectionModel
{
    public class LazyMemberInfoTests
    {
        [Fact]
        public void Constructor_PassMember()
        {
            foreach (var memberAndAccessorsInfo in GetMembersAndAccessors(typeof(LazyMemberTestClass)))
            {
                MemberInfo member = memberAndAccessorsInfo.Item1;
                MemberTypes memberType = memberAndAccessorsInfo.Item2.Item1;
                MemberInfo[] accessors = memberAndAccessorsInfo.Item2.Item2;

                LazyMemberInfo lazy = new LazyMemberInfo(member);
                Assert.Equal(memberType, lazy.MemberType);
                Assert.Equal(accessors.Length, lazy.GetAccessors().Length);
                Assert.True(accessors.SequenceEqual(lazy.GetAccessors()));
            }
        }

        [Fact]
        public void Constructor_PassNullMember()
        {
            Assert.Throws<ArgumentNullException>("member", () =>
                {
                    LazyMemberInfo lazy = new LazyMemberInfo((MemberInfo)null);
                });
        }

        [Fact]
        public void Constructor_PassAccessors()
        {
            foreach (var memberAndAccessorsInfo in GetMembersAndAccessors(typeof(LazyMemberTestClass)))
            {
                MemberInfo member = memberAndAccessorsInfo.Item1;
                MemberTypes memberType = memberAndAccessorsInfo.Item2.Item1;
                MemberInfo[] accessors = memberAndAccessorsInfo.Item2.Item2;

                LazyMemberInfo lazy = new LazyMemberInfo(memberType, accessors);
                Assert.Equal(memberType, lazy.MemberType);
                Assert.Equal(accessors.Length, lazy.GetAccessors().Length);
                Assert.True(accessors.SequenceEqual(lazy.GetAccessors()));
            }
        }

        [Fact]
        public void Constructor_PassInvalidAccessors()
        {
            foreach (var memberAndAccessorsInfo in GetMembersAndAccessors(typeof(LazyMemberTestClass)))
            {
                MemberInfo member = memberAndAccessorsInfo.Item1;
                MemberTypes memberType = memberAndAccessorsInfo.Item2.Item1;
                MemberInfo[] accessors = memberAndAccessorsInfo.Item2.Item2;

                foreach (MemberTypes wrongMemberType in GetValidMemberTypes())
                {
                    if (wrongMemberType == memberType)
                    {
                        continue;
                    }
                    Assert.Throws<ArgumentException>("accessors", () =>
                    {
                        LazyMemberInfo lazy = new LazyMemberInfo(wrongMemberType, accessors);
                    });
                }
            }
        }

        [Fact]
        public void Constructor_PassAccessorsInvalidMemberType()
        {
            MemberTypes[] validMemberTypes = GetValidMemberTypes().ToArray();
            foreach (MemberTypes memberType in GetMemberTypeValues())
            {
                if (!validMemberTypes.Contains(memberType))
                {
                    Assert.Throws<ArgumentException>("memberType", () =>
                    {
                        LazyMemberInfo lazy = new LazyMemberInfo(memberType, typeof(LazyMemberTestClass));
                    });
                }
            }
        }

        [Fact]
        public void Constructor_PassNullAccessors()
        {
            Assert.Throws<ArgumentNullException>("accessors", () =>
            {
                LazyMemberInfo lazy = new LazyMemberInfo(MemberTypes.Field, (MemberInfo[])null);
            });
        }

        [Fact]
        public void Constructor_PassAccessorsWithNulls()
        {
            Assert.Throws<ArgumentException>("accessors", () =>
            {
                LazyMemberInfo lazy = new LazyMemberInfo(MemberTypes.Field, new MemberInfo[] { null, null });
            });
        }

        [Fact]
        public void Constructor_PassAccessorCreators()
        {
            foreach (var memberAndAccessorsInfo in GetMembersAndAccessors(typeof(LazyMemberTestClass)))
            {
                MemberInfo member = memberAndAccessorsInfo.Item1;
                MemberTypes memberType = memberAndAccessorsInfo.Item2.Item1;
                MemberInfo[] accessors = memberAndAccessorsInfo.Item2.Item2;

                LazyMemberInfo lazy = new LazyMemberInfo(memberType, () => accessors);
                Assert.Equal(memberType, lazy.MemberType);
                Assert.Equal(accessors.Length, lazy.GetAccessors().Length);
                Assert.True(accessors.SequenceEqual(lazy.GetAccessors()));
            }
        }

        [Fact]
        public void Constructor_PassInvalidAccessorCreators()
        {
            foreach (var memberAndAccessorsInfo in GetMembersAndAccessors(typeof(LazyMemberTestClass)))
            {
                MemberInfo member = memberAndAccessorsInfo.Item1;
                MemberTypes memberType = memberAndAccessorsInfo.Item2.Item1;
                MemberInfo[] accessors = memberAndAccessorsInfo.Item2.Item2;

                foreach (MemberTypes wrongMemberType in GetValidMemberTypes())
                {
                    if (wrongMemberType == memberType)
                    {
                        continue;
                    }
                    LazyMemberInfo lazy = new LazyMemberInfo(wrongMemberType, () => accessors);
                    ExceptionAssert.Throws<InvalidOperationException>(() =>
                    {
                        lazy.GetAccessors();
                    });
                }
            }
        }

        [Fact]
        public void Constructor_PassAccessorCreatorsWithInvalidMemberType()
        {
            MemberTypes[] validMemberTypes = GetValidMemberTypes().ToArray();
            foreach (MemberTypes memberType in GetMemberTypeValues())
            {
                if (!validMemberTypes.Contains(memberType))
                {
                    Assert.Throws<ArgumentException>("memberType", () =>
                    {
                        LazyMemberInfo lazy = new LazyMemberInfo(memberType, () => new MemberInfo[] { typeof(LazyMemberTestClass) });
                    });
                }
            }
        }

        [Fact]
        public void Constructor_PassNullAccessorCreators()
        {
            Assert.Throws<ArgumentNullException>("accessorsCreator", () =>
            {
                LazyMemberInfo lazy = new LazyMemberInfo(MemberTypes.Field, (Func<MemberInfo[]>)null);
            });
        }

        private static IEnumerable<Tuple<MemberInfo, Tuple<MemberTypes, MemberInfo[]>>> GetMembersAndAccessors(Type type)
        {
            yield return new Tuple<MemberInfo, Tuple<MemberTypes, MemberInfo[]>>(
                type, new Tuple<MemberTypes, MemberInfo[]>(type.MemberType, new MemberInfo[] { type }));

            foreach (MemberInfo member in type.GetMembers())
            {
                MemberInfo[] accessors = null;
                if (member.MemberType == MemberTypes.Property)
                {
                    PropertyInfo property = (PropertyInfo)member;
                    accessors = new MemberInfo[] { property.GetGetMethod(true), property.GetSetMethod(true) };
                }
                else if (member.MemberType == MemberTypes.Event)
                {
                    EventInfo event_ = (EventInfo)member;
                    accessors = new MemberInfo[] { event_.GetRaiseMethod(true), event_.GetAddMethod(true), event_.GetRemoveMethod(true) };
                }
                else
                {
                    accessors = new MemberInfo[] { member };
                }

                yield return new Tuple<MemberInfo, Tuple<MemberTypes, MemberInfo[]>>(
                    member, new Tuple<MemberTypes, MemberInfo[]>(member.MemberType, accessors));
            }
        }

        private static IEnumerable<MemberTypes> GetMemberTypeValues()
        {
            yield return MemberTypes.All;
            yield return MemberTypes.Constructor;
            yield return MemberTypes.Custom;
            yield return MemberTypes.Event;
            yield return MemberTypes.Field;
            yield return MemberTypes.Method;
            yield return MemberTypes.NestedType;
            yield return MemberTypes.Property;
            yield return MemberTypes.TypeInfo;
        }

        private static IEnumerable<MemberTypes> GetValidMemberTypes()
        {
            yield return MemberTypes.TypeInfo;
            yield return MemberTypes.NestedType;
            yield return MemberTypes.Constructor;
            yield return MemberTypes.Field;
            yield return MemberTypes.Method;
            yield return MemberTypes.Property;
            yield return MemberTypes.Event;
        }

        public class LazyMemberTestClass
        {
            public LazyMemberTestClass() { }
            public string Property { get; set; }
            public string SetProperty { set { } }
            public string GetProperty { get { return null; } }
            public string Field;
            public void Method() { this.Event(this, new EventArgs()); }
            public event EventHandler Event;
        }
    }
}
