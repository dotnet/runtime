// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Tests
{
    public static class ReflectionOnlyRestrictions
    {
        [Fact]
        public static void TestRestrictions()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromAssemblyPath(typeof(TopLevelType).Assembly.Location);

                Assert.Throws<NotSupportedException>(() => a.CodeBase);
                Assert.Throws<NotSupportedException>(() => a.EscapedCodeBase);
                Assert.Throws<NotSupportedException>(() => a.GetObjectData(null, default));
                Assert.Throws<NotSupportedException>(() => a.GetSatelliteAssembly(null));
                Assert.Throws<NotSupportedException>(() => a.GetSatelliteAssembly(null, null));

                foreach (TypeInfo t in a.DefinedTypes)
                {
                    Assert.Throws<InvalidOperationException>(() => t.IsSecurityCritical);
                    Assert.Throws<InvalidOperationException>(() => t.IsSecuritySafeCritical);
                    Assert.Throws<InvalidOperationException>(() => t.IsSecurityTransparent);

                    foreach (MemberInfo mem in t.GetMember("*", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                    {
                        ICustomAttributeProvider icp = mem;
                        Assert.Throws<InvalidOperationException>(() => icp.GetCustomAttributes(inherit: false));
                        Assert.Throws<InvalidOperationException>(() => icp.GetCustomAttributes(null, inherit: false));
                        Assert.Throws<InvalidOperationException>(() => icp.IsDefined(null, inherit: false));

                        if (mem is ConstructorInfo c)
                        {
                            Assert.Throws<InvalidOperationException>(() => c.Invoke(Array.Empty<object>()));
                            Assert.Throws<InvalidOperationException>(() => c.Invoke(default(BindingFlags), null, Array.Empty<object>(), null));
                            Assert.Throws<InvalidOperationException>(() => c.Invoke(null, Array.Empty<object>()));
                            Assert.Throws<InvalidOperationException>(() => c.Invoke(null, default(BindingFlags), null, Array.Empty<object>(), null));
                            Assert.Throws<InvalidOperationException>(() => c.MethodHandle);
                            Assert.Throws<InvalidOperationException>(() => c.IsSecurityCritical);
                            Assert.Throws<InvalidOperationException>(() => c.IsSecuritySafeCritical);
                            Assert.Throws<InvalidOperationException>(() => c.IsSecurityTransparent);
                        }

                        if (mem is EventInfo e)
                        {
                            Assert.Throws<InvalidOperationException>(() => e.AddEventHandler(null, null));
                            Assert.Throws<InvalidOperationException>(() => e.RemoveEventHandler(null, null));
                        }

                        if (mem is FieldInfo f)
                        {
                            Assert.Throws<InvalidOperationException>(() => f.FieldHandle);
                            Assert.Throws<InvalidOperationException>(() => f.GetValue(null));
                            Assert.Throws<InvalidOperationException>(() => f.GetValueDirect(default));
                            Assert.Throws<InvalidOperationException>(() => f.SetValue(null, null));
                            Assert.Throws<InvalidOperationException>(() => f.SetValueDirect(default, null));
                            Assert.Throws<InvalidOperationException>(() => f.IsSecurityCritical);
                            Assert.Throws<InvalidOperationException>(() => f.IsSecuritySafeCritical);
                            Assert.Throws<InvalidOperationException>(() => f.IsSecurityTransparent);
                        }

                        if (mem is MethodInfo m)
                        {
                            Assert.Throws<InvalidOperationException>(() => m.Invoke(null, Array.Empty<object>()));
                            Assert.Throws<InvalidOperationException>(() => m.Invoke(null, default(BindingFlags), null, Array.Empty<object>(), null));
                            Assert.Throws<InvalidOperationException>(() => m.MethodHandle);
                            Assert.Throws<InvalidOperationException>(() => m.CreateDelegate(null));
                            Assert.Throws<InvalidOperationException>(() => m.CreateDelegate(null, null));
                            Assert.Throws<InvalidOperationException>(() => m.IsSecurityCritical);
                            Assert.Throws<InvalidOperationException>(() => m.IsSecuritySafeCritical);
                            Assert.Throws<InvalidOperationException>(() => m.IsSecurityTransparent);
                        }

                        if (mem is PropertyInfo p)
                        {
                            Assert.Throws<InvalidOperationException>(() => p.GetValue(null));
                            Assert.Throws<InvalidOperationException>(() => p.SetValue(null, null));
                            Assert.Throws<InvalidOperationException>(() => p.GetConstantValue());
                        }
                    }
                }
            }
        }
    }
}
