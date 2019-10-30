// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.SqlServer.TDS.Authentication;
using Microsoft.SqlServer.TDS.ColInfo;
using Microsoft.SqlServer.TDS.ColMetadata;
using Microsoft.SqlServer.TDS.Done;
using Microsoft.SqlServer.TDS.EnvChange;
using Microsoft.SqlServer.TDS.Error;
using Microsoft.SqlServer.TDS.FeatureExtAck;
using Microsoft.SqlServer.TDS.Info;
using Microsoft.SqlServer.TDS.LoginAck;
using Microsoft.SqlServer.TDS.Row;
using Microsoft.SqlServer.TDS.SSPI;
using Microsoft.SqlServer.TDS.SessionState;
using Microsoft.SqlServer.TDS.ReturnStatus;
using Microsoft.SqlServer.TDS.Order;

namespace Microsoft.SqlServer.TDS
{
    /// <summary>
    /// Constructor for TDS tokens
    /// </summary>
    public class TDSTokenFactory
    {
        /// <summary>
        /// Reconstruct token sequence from the stream
        /// </summary>
        /// <returns>Token collection</returns>
        public static IList<TDSPacketToken> Create(Stream source)
        {
            // Prepare collection
            IList<TDSPacketToken> tokens = new List<TDSPacketToken>();

            // Process the whole stream
            while (source.Position < source.Length)
            {
                // Read token type
                TDSTokenType tokenType = (TDSTokenType)source.ReadByte();

                // Call corresponding constructor based on the token type
                switch (tokenType)
                {
                    case TDSTokenType.EnvironmentChange:
                        {
                            tokens.Add(new TDSEnvChangeToken());
                            break;
                        }
                    case TDSTokenType.Info:
                        {
                            tokens.Add(new TDSInfoToken());
                            break;
                        }
                    case TDSTokenType.Error:
                        {
                            tokens.Add(new TDSErrorToken());
                            break;
                        }
                    case TDSTokenType.Done:
                        {
                            tokens.Add(new TDSDoneToken());
                            break;
                        }
                    case TDSTokenType.DoneInProc:
                        {
                            tokens.Add(new TDSDoneInProcToken());
                            break;
                        }
                    case TDSTokenType.DoneProcedure:
                        {
                            tokens.Add(new TDSDoneProcedureToken());
                            break;
                        }
                    case TDSTokenType.LoginAcknowledgement:
                        {
                            tokens.Add(new TDSLoginAckToken());
                            break;
                        }
                    case TDSTokenType.ColumnInfo:
                        {
                            tokens.Add(new TDSColInfoToken());
                            break;
                        }
                    case TDSTokenType.ColumnMetadata:
                        {
                            tokens.Add(new TDSColMetadataToken());
                            break;
                        }
                    case TDSTokenType.Row:
                        {
                            // Find column metadata token
                            TDSColMetadataToken columnMetadata = tokens.Where(t => t is TDSColMetadataToken).LastOrDefault() as TDSColMetadataToken;

                            // Column metadata must be immediately preceeding row
                            if (columnMetadata == null)
                            {
                                throw new Exception("No column metadata is available for row token");
                            }

                            tokens.Add(new TDSRowToken(columnMetadata));
                            break;
                        }
                    case TDSTokenType.NBCRow:
                        {
                            // Find column metadata token
                            TDSColMetadataToken columnMetadata = tokens.Where(t => t is TDSColMetadataToken).LastOrDefault() as TDSColMetadataToken;

                            // Column metadata must be immediately preceeding row
                            if (columnMetadata == null)
                            {
                                throw new Exception("No column metadata is available for null-byte compression row token");
                            }

                            tokens.Add(new TDSNBCRowToken(columnMetadata));
                            break;
                        }
                    case TDSTokenType.SSPI:
                        {
                            tokens.Add(new TDSSSPIToken());
                            break;
                        }
                    case TDSTokenType.FedAuthInfo:
                        {
                            tokens.Add(new TDSFedAuthInfoToken());
                            break;
                        }
                    case TDSTokenType.FeatureExtAck:
                        {
                            tokens.Add(new TDSFeatureExtAckToken());
                            break;
                        }
                    case TDSTokenType.SessionState:
                        {
                            tokens.Add(new TDSSessionStateToken());
                            break;
                        }
                    case TDSTokenType.ReturnStatus:
                        {
                            tokens.Add(new TDSReturnStatusToken());
                            break;
                        }
                    case TDSTokenType.Order:
                        {
                            tokens.Add(new TDSOrderToken());
                            break;
                        }
                    default:
                        {
                            // Either the token has not been implemented yet or is invalid
                            throw new NotImplementedException(string.Format("Token \"{0}\" is not recognized", tokenType));
                        }
                }

                // Inflate the last token from the stream
                if (!tokens.Last().Inflate(source))
                {
                    // Throw exception at this point since this operation is not interruptable (yet)
                    throw new Exception(string.Format("Token \"{0}\" inflation failed", tokenType));
                }
            }

            return tokens;
        }
    }
}
