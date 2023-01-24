// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ILCompiler.DependencyAnalysis
{
    public partial class NodeFactory
    {
        private InitialInterfaceDispatchStubNode _initialInterfaceDispatchStubNode;

        public InitialInterfaceDispatchStubNode InitialInterfaceDispatchStub
        {
            get
            {
                _initialInterfaceDispatchStubNode ??= new InitialInterfaceDispatchStubNode();

                return _initialInterfaceDispatchStubNode;
            }
        }

    }
}
