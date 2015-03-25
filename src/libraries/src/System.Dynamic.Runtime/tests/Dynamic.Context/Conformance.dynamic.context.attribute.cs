// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq.Expressions;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.attribute.publickeydecl001.publickeydecl001
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.attribute.publickeydecl001.publickeydecl001;
    // <Title>
    // Using PublicKey attribute as part of the InternalsVisibleTo attribute
    // </Title>
    // <Description>
    // Using PublicKey attribute as part of the InternalsVisibleTo attribute
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    //[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("publickey001,PublicKey=002400000480000094000000060200000024000052534131000400000100010027be9b82deaeb8b98e5f455cbbde386f80f5c19516548cac1f2ffeea96f1712f51540946f2a8133c03d349cf0611788215da54989dcc88ee262c385d2c17343cf5cb969c436fb94fe399cc46f74bf0af2eb1f46aed1fb0adee16721c40ae02baf3d3d8b50a2dd6466829465db165a0f2915f74e1b67a63b4cf76c215cdba4ba6")]
    public class A
    {
        public string returnme()
        {
            return "hello world";
        }

        int unit = 0;
        public int Unit
        {
            get
            {
                return unit;
            }

            set
            {
                unit = value;
            }
        }
    }
    // </Code>
}