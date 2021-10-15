// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using Debug = System.Diagnostics.Debug;

namespace ILTrim
{
    /// <summary>
    /// Manages tokens in a single output assembly and assigns new tokens to the tokens in the input.
    /// </summary>
    public sealed partial class TokenMap
    {
        private readonly MetadataReader _reader;
        private readonly EntityHandle[][] _tokenMap;
        private readonly int[] _preAssignedRowIdsPerTable;

        public EntityHandle MapToken(EntityHandle sourceToken)
        {
            bool gotIndex = MetadataTokens.TryGetTableIndex(sourceToken.Kind, out TableIndex index);
            Debug.Assert(gotIndex); // Not expected we would need to map tokens that don't have a table

            EntityHandle result = _tokenMap[(int)index][MetadataTokens.GetRowNumber(sourceToken)];

            // If this hits, whoever is asking for the token mapping didn't declare the dependency
            // during the dependency analysis phase. The fix is to declare the dependency.
            Debug.Assert(!result.IsNil || sourceToken.IsNil);

            return result;
        }

        private TokenMap(MetadataReader reader, EntityHandle[][] tokenMap, int[] preAssignedRowIdsPerTable)
        {
            _reader = reader;
            _tokenMap = tokenMap;
            _preAssignedRowIdsPerTable = preAssignedRowIdsPerTable;
        }

        public sealed class Builder
        {
            private readonly MetadataReader _reader;
            private readonly EntityHandle[][] _tokenMap;
            private readonly int[] _preAssignedRowIdsPerTable;

            public Builder(MetadataReader source)
            {
                _tokenMap = new EntityHandle[MetadataTokens.TableCount][];
                _reader = source;

                for (int tableIndex = 0; tableIndex < _tokenMap.Length; tableIndex++)
                {
                    int rowCount = source.GetTableRowCount((TableIndex)tableIndex);

                    // Allocate one extra row for convenience (the unused RID 0 will get a row too)
                    var handles = new EntityHandle[rowCount + 1];

                    // RID 0 always maps to RID 0
                    handles[0] = MetadataTokens.EntityHandle((TableIndex)tableIndex, 0);

                    // AssemblyReferences tokens are tracked by AssemblyReferenceNode instead.
                    if (tableIndex != (int)TableIndex.AssemblyRef)
                        _tokenMap[tableIndex] = handles;
                }

                _preAssignedRowIdsPerTable = new int[MetadataTokens.TableCount];
            }

            public EntityHandle AddTokenMapping(EntityHandle sourceToken)
            {
                bool gotIndex = MetadataTokens.TryGetTableIndex(sourceToken.Kind, out TableIndex index);
                Debug.Assert(gotIndex); // Not expected we would need to map tokens that don't have a table

                int assignedRowId = ++_preAssignedRowIdsPerTable[(int)index];

                EntityHandle result = MetadataTokens.EntityHandle(index, assignedRowId);

                Debug.Assert(_tokenMap[(int)index][MetadataTokens.GetRowNumber(sourceToken)].IsNil);
                _tokenMap[(int)index][MetadataTokens.GetRowNumber(sourceToken)] = result;

                return result;
            }

            public EntityHandle AddToken(TableIndex index)
            {
                int assignedRowId = ++_preAssignedRowIdsPerTable[(int)index];

                EntityHandle result = MetadataTokens.EntityHandle(index, assignedRowId);
                return result;
            }

            public EntityHandle MapToken(EntityHandle sourceToken)
            {
                bool gotIndex = MetadataTokens.TryGetTableIndex(sourceToken.Kind, out TableIndex index);
                Debug.Assert(gotIndex); // Not expected we would need to map tokens that don't have a table

                EntityHandle result = _tokenMap[(int)index][MetadataTokens.GetRowNumber(sourceToken)];

                // If this hits, whoever is asking for the token mapping didn't declare the dependency
                // during the dependency analysis phase. The fix is to declare the dependency.
                Debug.Assert(!result.IsNil || sourceToken.IsNil);

                return result;
            }

            public TokenMap ToTokenMap()
            {
                return new TokenMap(_reader, _tokenMap, _preAssignedRowIdsPerTable);
            }
        }
    }
}
