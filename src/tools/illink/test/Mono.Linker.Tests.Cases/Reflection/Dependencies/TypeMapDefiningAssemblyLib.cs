// Licensed to the .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.Reflection.Dependencies;

[assembly: TypeMap<DefiningAssemblyGroup>("unconditional", typeof(DefiningAssemblyTarget))]
[assembly: TypeMap<DefiningAssemblyGroup>("before", typeof(DefiningAssemblyProxyBefore), typeof(DefiningAssemblySourceBefore))]
[assembly: TypeMap<DefiningAssemblyGroup>("after", typeof(DefiningAssemblyProxyAfter), typeof(DefiningAssemblySourceAfter))]
[assembly: TypeMap<DefiningAssemblyGroup>("unused", typeof(DefiningAssemblyUnusedTarget), typeof(DefiningAssemblyUnusedSource))]
[assembly: TypeMapAssociation<DefiningAssemblyGroup>(typeof(DefiningAssemblySourceBefore), typeof(DefiningAssemblyProxyBefore))]
[assembly: TypeMapAssociation<DefiningAssemblyGroup>(typeof(DefiningAssemblySourceAfter), typeof(DefiningAssemblyProxyAfter))]
[assembly: TypeMapAssociation<DefiningAssemblyGroup>(typeof(DefiningAssemblyUnusedSource), typeof(DefiningAssemblyUnusedTarget))]
[assembly: TypeMapAssemblyTarget<DefiningAssemblyUnusedGroup>("DoesNotExist")]
[assembly: TypeMapAssemblyTarget<DefiningAssemblyGroup>("TypeMapDefiningAssemblyTargetLib")]
[assembly: TypeMap<DefiningAssemblyExternalOnlyGroup>("external", typeof(DefiningAssemblyExternalOnlyTarget))]
[assembly: TypeMapAssociation<DefiningAssemblyExternalOnlyGroup>(typeof(DefiningAssemblySourceBefore), typeof(DefiningAssemblyUnusedProxy))]
[assembly: TypeMap<DefiningAssemblyProxyOnlyGroup>("unused", typeof(DefiningAssemblyUnusedExternalTarget))]
[assembly: TypeMapAssociation<DefiningAssemblyProxyOnlyGroup>(typeof(DefiningAssemblySourceBefore), typeof(DefiningAssemblyProxyOnlyTarget))]

namespace Mono.Linker.Tests.Cases.Reflection.Dependencies
{
    public class DefiningAssemblyGroup { }
    public class DefiningAssemblyTarget { }
    public class DefiningAssemblySourceBefore { }
    public class DefiningAssemblySourceAfter { }
    public class DefiningAssemblyProxyBefore { }
    public class DefiningAssemblyProxyAfter { }
    public class DefiningAssemblyUnusedSource { }
    public class DefiningAssemblyUnusedTarget { }
    public class DefiningAssemblyUnusedGroup { }
    public class DefiningAssemblyExternalOnlyGroup { }
    public class DefiningAssemblyExternalOnlyTarget { }
    public class DefiningAssemblyUnusedProxy { }
    public class DefiningAssemblyProxyOnlyGroup { }
    public class DefiningAssemblyProxyOnlyTarget { }
    public class DefiningAssemblyUnusedExternalTarget { }
}
