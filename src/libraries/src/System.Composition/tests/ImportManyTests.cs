// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Composition.UnitTests
{
    public class ImportManyTests : ContainerTests
    {
        public interface IA { }

        [Export(typeof(IA))]
        public class A : IA { }

        [Export(typeof(IA))]
        public class A2 : IA { }

        [Export]
        public class ImportManyIA
        {
            public IEnumerable<IA> Items;

            [ImportingConstructor]
            public ImportManyIA([ImportMany] IEnumerable<IA> items)
            {
                Items = items;
            }
        }
        [Export]
        public class ImportManyPropsOfA
        {
            [ImportMany]
            public IEnumerable<IA> AllA { get; set; }
            public ImportManyPropsOfA()
            {
            }
        }
        [Fact]
        public void ImportsMany()
        {
            var cc = CreateContainer(typeof(A), typeof(A2), typeof(ImportManyIA));
            var im = cc.GetExport<ImportManyIA>();
            Assert.Equal(2, im.Items.Count());
        }

        [Fact]
        public void ImportsManyProperties()
        {
            var cc = CreateContainer(typeof(A), typeof(A2), typeof(ImportManyPropsOfA));
            var im = cc.GetExport<ImportManyPropsOfA>();
            Assert.Equal(2, im.AllA.Count());
        }
    }
}
