// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using ILCompiler.DependencyAnalysisFramework;
using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class ProfileDataNode : EmbeddedObjectNode, ISymbolDefinitionNode
    {
        MethodWithGCInfo _methodNode;
        private byte[] _profileData;
        private int _ilSize;
        private int _blockCount;
        private TargetDetails _targetDetails;

        public ProfileDataNode(MethodWithGCInfo methodNode, TargetDetails targetDetails)
        {
            _methodNode = methodNode;
            _targetDetails = targetDetails;
        }

        public void SetProfileData(int ilSize, int blockCount, byte[] data)
        {
            Debug.Assert(_profileData == null); // This function should not be called twice on the same object

            _profileData = data;
            _ilSize = ilSize;
            _blockCount = blockCount;
        }

        public override bool StaticDependenciesAreComputed => _profileData != null;

        public override int ClassCode => 274394286;

        private int OffsetFromStartOfObjectToSymbol
        {
            get
            {
                int offset = 0
                    // sizeof(CORCOMPILE_METHOD_PROFILE_LIST)
                    + _targetDetails.PointerSize                // (CORCOMPILE_METHOD_PROFILE_LIST::next)
                                                                // sizeof(CORBBTPROF_METHOD_HEADER)
                    + sizeof(int)                               // (CORBBTPROF_METHOD_HEADER::size)
                    + sizeof(int)                               // (CORBBTPROF_METHOD_HEADER::cDetail)
                                                                // Next field is a CORBBT_METHOD_INFO struct   (CORBBTPROF_METHOD_HEADER::method)
                     + sizeof(int)                               // (CORBBT_METHOD_INFO::token)
                     + sizeof(int)                               // (CORBBT_METHOD_INFO::ILSize)
                     + sizeof(int);                              // (CORBBT_METHOD_INFO::cBlock)
                // At this offset lies the block counts
                return offset;
            }
        }

        int ISymbolDefinitionNode.Offset
        {
            get
            {
                // At this offset lies the block counts
                return OffsetFromStartOfObjectToSymbol + OffsetFromBeginningOfArray;
            }
        }

        int ISymbolNode.Offset => 0;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("ProfileDataNode->");
            _methodNode.AppendMangledName(nameMangler, sb);
        }

        protected override void OnMarked(NodeFactory factory)
        {
            factory.ProfileDataSection.AddEmbeddedObject(this);
        }

        public override void EncodeData(ref ObjectDataBuilder dataBuilder, NodeFactory factory, bool relocsOnly)
        {
            ProfileDataNode nextElementInList = ((ProfileDataSectionNode)ContainingNode).NextElementToEncode;
            if (nextElementInList != null)
                dataBuilder.EmitPointerReloc(nextElementInList, -OffsetFromStartOfObjectToSymbol);
            else
                dataBuilder.EmitZeroPointer();

            if (relocsOnly)
            {
                return;
            }

            EcmaMethod ecmaMethod = (EcmaMethod)_methodNode.Method.GetTypicalMethodDefinition();
            int startOffset = dataBuilder.CountBytes;
            var reservation = dataBuilder.ReserveInt();

            dataBuilder.EmitInt(0); // CORBBTPROF_METHOD_HEADER::cDetail
            dataBuilder.EmitInt(ecmaMethod.MetadataReader.GetToken(ecmaMethod.Handle)); // CORBBT_METHOD_INFO::token
            dataBuilder.EmitInt(_ilSize); // CORBBT_METHOD_INFO::ILSize
            dataBuilder.EmitInt(_blockCount); // CORBBT_METHOD_INFO::cBlock
            int sizeOfCORBBTPROF_METHOD_HEADER = dataBuilder.CountBytes - startOffset;

            Debug.Assert(sizeOfCORBBTPROF_METHOD_HEADER == (OffsetFromStartOfObjectToSymbol - _targetDetails.PointerSize));
            dataBuilder.EmitInt(reservation, sizeOfCORBBTPROF_METHOD_HEADER + _profileData.Length);

            dataBuilder.EmitBytes(_profileData);

            while ((dataBuilder.CountBytes & (dataBuilder.TargetPointerSize - 1)) != 0)
                dataBuilder.EmitByte(0);
        }

        protected override string GetName(NodeFactory context)
        {
            Utf8StringBuilder sb = new Utf8StringBuilder();
            sb.Append("ProfileDataNode->");
            _methodNode.AppendMangledName(context.NameMangler, sb);
            return sb.ToString();
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context)
        {
            return Array.Empty<DependencyListEntry>();
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return comparer.Compare(_methodNode, ((ProfileDataNode)other)._methodNode);
        }
    }
}
