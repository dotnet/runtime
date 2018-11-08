// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Xunit;

namespace System.Reflection.Tests
{
    public static partial class ModuleTests
    {
        [Fact]
        public static void ModuleAssembly()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_SimpleAssemblyImage);
                Module m = a.ManifestModule;
                Assert.Equal(a, m.Assembly);
            }
        }

        [Fact]
        public static void ModuleMvid()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_SimpleAssemblyImage);
                Module m = a.ManifestModule;
                Guid mvid = m.ModuleVersionId;
                Assert.Equal(TestData.s_SimpleAssemblyMvid, mvid);
            }
        }

        [Fact]
        public static void ModuleMetadataToken()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_SimpleAssemblyImage);
                Module m = a.ManifestModule;
                Assert.Equal(0x00000001, m.MetadataToken);
            }
        }

        [Fact]
        public static void ModuleIsResource()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_SimpleAssemblyImage);
                Module m = a.ManifestModule;
                Assert.False(m.IsResource());
            }
        }

        [Fact]
        public static void ModuleFullyQualifiedNameFromPath()
        {
            using (TempFile tf = TempFile.Create(TestData.s_SimpleAssemblyImage))
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                string path = tf.Path;
                Assembly a = lc.LoadFromAssemblyPath(path);
                Module m = a.ManifestModule;
                Assert.Equal(path, m.FullyQualifiedName);
            }
        }

        [Fact]
        public static void ModuleFullyQualifiedNameFromByteArray()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_SimpleAssemblyImage);
                Module m = a.ManifestModule;
                Assert.Equal(string.Empty, m.FullyQualifiedName);
            }
        }

        [Fact]
        public static void ModuleGetNameFromPath()
        {
            using (TempFile tf = TempFile.Create(TestData.s_SimpleAssemblyImage))
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                string path = tf.Path;
                Assembly a = lc.LoadFromAssemblyPath(path);
                Module m = a.ManifestModule;
                string name = Path.GetFileName(path);
                Assert.Equal(name, m.Name);
            }
        }

        [Fact]
        public static void ModuleGetNameFromByteArray()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_SimpleAssemblyImage);
                Module m = a.ManifestModule;
                Assert.Equal(string.Empty, m.Name);
            }
        }

        [Fact]
        public static void ModuleScopeNameFromPath()
        {
            using (TempFile tf = TempFile.Create(TestData.s_SimpleAssemblyImage))
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                string path = tf.Path;
                Assembly a = lc.LoadFromAssemblyPath(path);
                Module m = a.ManifestModule;
                Assert.Equal("SimpleAssembly.dll", m.ScopeName);
            }
        }

        [Fact]
        public static void ModuleScopeNameFromByteArray()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_SimpleAssemblyImage);
                Module m = a.ManifestModule;
                Assert.Equal("SimpleAssembly.dll", m.ScopeName);
            }
        }

        [Fact]
        public static void GetPEKindAnyCpu()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_PlatformAnyCpu);
                Module m = a.ManifestModule;

                m.GetPEKind(out PortableExecutableKinds peKind, out ImageFileMachine machine);
                Assert.Equal(PortableExecutableKinds.ILOnly, peKind);
                Assert.Equal(ImageFileMachine.I386, machine);

                AssemblyName an = a.GetName(copiedName: false);
                ProcessorArchitecture pa = an.ProcessorArchitecture;
                Assert.Equal(ProcessorArchitecture.MSIL, pa);
            }
        }

        [Fact]
        public static void GetPEKindAnyCpu32BitPreferred()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_PlatformAnyCpu32BitPreferred);
                Module m = a.ManifestModule;

                m.GetPEKind(out PortableExecutableKinds peKind, out ImageFileMachine machine);
                Assert.Equal(PortableExecutableKinds.ILOnly | PortableExecutableKinds.Preferred32Bit, peKind);
                Assert.Equal(ImageFileMachine.I386, machine);

                AssemblyName an = a.GetName(copiedName: false);
                ProcessorArchitecture pa = an.ProcessorArchitecture;
                Assert.Equal(ProcessorArchitecture.MSIL, pa);
            }
        }

        [Fact]
        public static void GetPEKindX86()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_PlatformX86);
                Module m = a.ManifestModule;

                m.GetPEKind(out PortableExecutableKinds peKind, out ImageFileMachine machine);
                Assert.Equal(PortableExecutableKinds.ILOnly | PortableExecutableKinds.Required32Bit, peKind);
                Assert.Equal(ImageFileMachine.I386, machine);

                AssemblyName an = a.GetName(copiedName: false);
                ProcessorArchitecture pa = an.ProcessorArchitecture;
                Assert.Equal(ProcessorArchitecture.X86, pa);
            }
        }

        [Fact]
        public static void GetPEKindX64()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_PlatformX64);
                Module m = a.ManifestModule;

                m.GetPEKind(out PortableExecutableKinds peKind, out ImageFileMachine machine);
                Assert.Equal(PortableExecutableKinds.ILOnly | PortableExecutableKinds.PE32Plus, peKind);
                Assert.Equal(ImageFileMachine.AMD64, machine);

                AssemblyName an = a.GetName(copiedName: false);
                ProcessorArchitecture pa = an.ProcessorArchitecture;
                Assert.Equal(ProcessorArchitecture.Amd64, pa);
            }
        }

        [Fact]
        public static void GetPEKindItanium()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_PlatformItanium);
                Module m = a.ManifestModule;

                m.GetPEKind(out PortableExecutableKinds peKind, out ImageFileMachine machine);
                Assert.Equal(PortableExecutableKinds.ILOnly | PortableExecutableKinds.PE32Plus, peKind);
                Assert.Equal(ImageFileMachine.IA64, machine);

                AssemblyName an = a.GetName(copiedName: false);
                ProcessorArchitecture pa = an.ProcessorArchitecture;
                Assert.Equal(ProcessorArchitecture.IA64, pa);
            }
        }

        [Fact]
        public static void GetPEKindArm()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_PlatformArm);
                Module m = a.ManifestModule;

                m.GetPEKind(out PortableExecutableKinds peKind, out ImageFileMachine machine);
                Assert.Equal(PortableExecutableKinds.ILOnly, peKind);
                Assert.Equal(ImageFileMachine.ARM, machine);

                AssemblyName an = a.GetName(copiedName: false);
                ProcessorArchitecture pa = an.ProcessorArchitecture;
                Assert.Equal(ProcessorArchitecture.Arm, pa);
            }
        }
    }
}
