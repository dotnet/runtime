// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;
using Internal.TypeSystem.TypesDebugInfo;
using ObjectData = ILCompiler.DependencyAnalysis.ObjectNode.ObjectData;
using static ILCompiler.DependencyAnalysis.RelocType;

namespace ILCompiler.ObjectWriter
{
    public abstract partial class ObjectWriter
    {
        // Debugging
        private UserDefinedTypeDescriptor _userDefinedTypeDescriptor;

        private protected abstract void EmitUnwindInfo(
            SectionWriter sectionWriter,
            INodeWithCodeInfo nodeWithCodeInfo,
            string currentSymbolName);

        private protected uint GetVarTypeIndex(bool isStateMachineMoveNextMethod, DebugVarInfoMetadata debugVar)
        {
            uint typeIndex;
            try
            {
                if (isStateMachineMoveNextMethod && debugVar.DebugVarInfo.VarNumber == 0)
                {
                    typeIndex = _userDefinedTypeDescriptor.GetStateMachineThisVariableTypeIndex(debugVar.Type);
                    // FIXME
                    // varName = "locals";
                }
                else
                {
                    typeIndex = _userDefinedTypeDescriptor.GetVariableTypeIndex(debugVar.Type);
                }
            }
            catch (TypeSystemException)
            {
                typeIndex = 0; // T_NOTYPE
                // FIXME
                // Debug.Fail();
            }
            return typeIndex;
        }

        private protected abstract ITypesDebugInfoWriter CreateDebugInfoBuilder();

        private protected abstract void EmitDebugFunctionInfo(
            uint methodTypeIndex,
            string methodName,
            SymbolDefinition methodSymbol,
            INodeWithDebugInfo debugNode,
            bool hasSequencePoints);

        private protected virtual void EmitDebugThunkInfo(
            string methodName,
            SymbolDefinition methodSymbol,
            INodeWithDebugInfo debugNode)
        {
        }

        private protected abstract void EmitDebugSections(IDictionary<string, SymbolDefinition> definedSymbols);

        partial void EmitDebugInfo(IReadOnlyCollection<DependencyNode> nodes, Logger logger)
        {
            if (logger.IsVerbose)
                logger.LogMessage($"Emitting debug information");

            _userDefinedTypeDescriptor = new UserDefinedTypeDescriptor(CreateDebugInfoBuilder(), _nodeFactory);

            foreach (DependencyNode depNode in nodes)
            {
                ObjectNode node = depNode as ObjectNode;
                if (node is null || node.ShouldSkipEmittingObjectNode(_nodeFactory))
                {
                    continue;
                }

                ISymbolNode symbolNode = node as ISymbolNode;
                ISymbolNode deduplicatedSymbolNode = _nodeFactory.ObjectInterner.GetDeduplicatedSymbol(_nodeFactory, symbolNode);
                if (deduplicatedSymbolNode != symbolNode)
                {
                    continue;
                }

                // Ensure any allocated MethodTables have debug info
                if (node is ConstructedEETypeNode methodTable)
                {
                    _userDefinedTypeDescriptor.GetTypeIndex(methodTable.Type, needsCompleteType: true);
                }

                if (node is INodeWithDebugInfo debugNode and ISymbolDefinitionNode symbolDefinitionNode)
                {
                    string methodName = GetMangledName(symbolDefinitionNode);
                    if (_definedSymbols.TryGetValue(methodName, out var methodSymbol))
                    {
                        if (node is IMethodNode methodNode)
                        {
                            bool hasSequencePoints = debugNode.GetNativeSequencePoints().Any();
                            uint methodTypeIndex = hasSequencePoints ? _userDefinedTypeDescriptor.GetMethodFunctionIdTypeIndex(methodNode.Method) : 0;
                            EmitDebugFunctionInfo(methodTypeIndex, methodName, methodSymbol, debugNode, hasSequencePoints);
                        }
                        else
                        {
                            EmitDebugThunkInfo(methodName, methodSymbol, debugNode);
                        }
                    }
                }
            }

            // Ensure all fields associated with generated static bases have debug info
            foreach (MetadataType typeWithStaticBase in _nodeFactory.MetadataManager.GetTypesWithStaticBases())
            {
                _userDefinedTypeDescriptor.GetTypeIndex(typeWithStaticBase, needsCompleteType: true);
            }

            EmitDebugSections(_definedSymbols);
        }

        private protected abstract void CreateEhSections();

        partial void PrepareForUnwindInfo() => CreateEhSections();

        partial void EmitUnwindInfoForNode(ObjectNode node, string currentSymbolName, SectionWriter sectionWriter)
        {
            if (node is INodeWithCodeInfo nodeWithCodeInfo)
            {
                EmitUnwindInfo(sectionWriter, nodeWithCodeInfo, currentSymbolName);
            }
        }

        partial void HandleControlFlowForRelocation(ISymbolNode relocTarget, string relocSymbolName)
        {
            if (relocTarget is IMethodNode or AssemblyStubNode or AddressTakenExternFunctionSymbolNode)
            {
                // For now consider all method symbols address taken.
                // We could restrict this in the future to those that are referenced from
                // reflection tables, EH tables, were actually address taken in code, or are referenced from vtables.
                EmitReferencedMethod(relocSymbolName);
            }
        }
    }
}
