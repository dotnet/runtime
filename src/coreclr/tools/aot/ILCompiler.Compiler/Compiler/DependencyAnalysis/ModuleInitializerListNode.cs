// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.DependencyAnalysis
{
    internal sealed class ModuleInitializerListNode : ObjectNode, ISymbolDefinitionNode
    {
        private readonly ObjectAndOffsetSymbolNode _endSymbol;

        public ModuleInitializerListNode()
        {
            _endSymbol = new ObjectAndOffsetSymbolNode(this, 0, "__module_initializers_End", true);
        }

        public ISymbolNode EndSymbol => _endSymbol;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("__module_initializers");
        }

        public int Offset => 0;

        public override bool IsShareable => false;

        public override ObjectNodeSection Section => ObjectNodeSection.DataSection;

        public override bool StaticDependenciesAreComputed => true;

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            // This is a summary node that doesn't introduce dependencies.
            if (relocsOnly)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });

            var modulesWithCctor = new List<ModuleDesc>();

            foreach (var methodNode in factory.MetadataManager.GetCompiledMethodBodies())
            {
                MethodDesc method = methodNode.Method;
                if (method.OwningType is MetadataType mdType
                    && mdType.IsModuleType && method.IsStaticConstructor)
                {
                    modulesWithCctor.Add(mdType.Module);
                }
            }

            // We have a list of modules with a class constructor.
            // Do a topological sort based on the assembly references of each module.
            // This is an approximation that tries to deal with module initializers that might have
            // dependencies on each other. The spec doesn't guarantee any ordering so this is best effort.
            List<ModuleDesc> sortedModules = new List<ModuleDesc>();

            var graphFactory = new ModuleGraphFactory();

            // This is a list because we want to keep a stable sort order.
            var allModules = new List<ModuleGraphNode>();

            // Seed the graph with the list of modules we're interested in
            foreach (ModuleDesc module in modulesWithCctor)
                allModules.Add(graphFactory.GetNode(module));

            // Expand the graph to include all the nodes in between the interesting ones
            var modulesToExpand = new Queue<ModuleGraphNode>(allModules);
            while (modulesToExpand.Count > 0)
            {
                ModuleGraphNode node = modulesToExpand.Dequeue();
                foreach (var reference in node.Edges)
                {
                    if (!allModules.Contains(reference))
                    {
                        allModules.Add(reference);
                        modulesToExpand.Enqueue(reference);
                    }
                }
            }

            // Now sort the nodes
            //
            // We start with the modules that don't reference any other modules.
            // Then we add modules that reference the modules we already sorted.
            // Etc. until we figured out the order of all modules with a cctor.
            //
            // This might appear counter intuitive (the cctor of the entrypoint module
            // will likely run last), but if the entrypoint module cctor calls into another
            // module that has a cctor, CoreCLR will run that cctor first.
            //
            // If a module doesn't call into other modules with a cctor, it doesn't matter
            // when we sort it. If it does call into one, it should run before.
            var markedModules = new HashSet<ModuleGraphNode>();
            while (sortedModules.Count != modulesWithCctor.Count)
            {
                bool madeProgress = false;

                // Add nodes that have all their dependencies already satisfied.
                foreach (var module in allModules)
                {
                    if (!markedModules.Contains(module) && module.Satisfies(markedModules))
                    {
                        madeProgress = true;
                        markedModules.Add(module);
                        if (modulesWithCctor.Contains(module.Module))
                            sortedModules.Add(module.Module);
                    }
                }

                // If we haven't made progress, there's a cycle. Pick the first unmarked node as victim.
                if (!madeProgress)
                {
                    foreach (var module in allModules)
                    {
                        if (!markedModules.Contains(module))
                        {
                            markedModules.Add(module);
                            if (modulesWithCctor.Contains(module.Module))
                                sortedModules.Add(module.Module);
                            break;
                        }
                    }
                }
            }

            // The data structure is a flat list of module constructors to call.
            // This is insufficient for the purposes of ordering in the multi-object-module mode.
            // If this mode ever becomes more interesting, we'll need to do the sorting at
            // the time of startup. (Linker likely can't do it, unfortunately.)

            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.RequireInitialAlignment(factory.Target.PointerSize);
            builder.AddSymbol(this);
            builder.AddSymbol(_endSymbol);

            foreach (var module in sortedModules)
            {
                builder.EmitPointerReloc(factory.MethodEntrypoint(module.GetGlobalModuleType().GetStaticConstructor()));
            }

            var result = builder.ToObjectData();

            _endSymbol.SetSymbolOffset(result.Data.Length);

            return result;
        }

        public override int ClassCode => 0x4732738;

        private sealed class ModuleGraphNode
        {
            private readonly ModuleGraphFactory _factory;
            private readonly ModuleDesc[] _edges;
            private ModuleGraphNode[] _edgeNodes;

            public ModuleDesc Module { get; }
            public ModuleGraphNode[] Edges
            {
                get
                {
                    if (_edgeNodes != null)
                    {
                        return _edgeNodes;
                    }

                    var edgeNodes = default(ArrayBuilder<ModuleGraphNode>);
                    foreach (var edge in _edges)
                        edgeNodes.Add(_factory.GetNode(edge));
                    return _edgeNodes = edgeNodes.ToArray();
                }
            }
            public bool Satisfies(HashSet<ModuleGraphNode> markedNodes)
            {
                foreach (var edge in Edges)
                    if (!markedNodes.Contains(edge))
                        return false;
                return true;
            }
            public ModuleGraphNode(ModuleGraphFactory factory, ModuleDesc module, ModuleDesc[] edges)
                => (_factory, Module, _edges) = (factory, module, edges);
        }

        private sealed class ModuleGraphFactory
        {
            private readonly Dictionary<ModuleDesc, ModuleGraphNode> _nodes = new Dictionary<ModuleDesc, ModuleGraphNode>();

            public ModuleGraphNode GetNode(ModuleDesc module)
            {
                if (_nodes.TryGetValue(module, out ModuleGraphNode result))
                    return result;

                if (module is EcmaModule ecmaModule)
                {
                    ArrayBuilder<ModuleDesc> referencedAssemblies = default(ArrayBuilder<ModuleDesc>);
                    var reader = ecmaModule.MetadataReader;
                    foreach (var assemblyReferenceHandle in reader.AssemblyReferences)
                    {
                        var assemblyReference = reader.GetAssemblyReference(assemblyReferenceHandle);
                        string assemblyName = reader.GetString(assemblyReference.Name);

                        try
                        {
                            var reference = module.Context.ResolveAssembly(new System.Reflection.AssemblyName(assemblyName));
                            referencedAssemblies.Add(reference);
                        }
                        catch (TypeSystemException) { }
                    }
                    result = new ModuleGraphNode(this, module, referencedAssemblies.ToArray());
                }
                else
                {
                    result = new ModuleGraphNode(this, module, Array.Empty<ModuleDesc>());
                }

                _nodes.Add(module, result);
                return result;
            }
        }
    }
}
