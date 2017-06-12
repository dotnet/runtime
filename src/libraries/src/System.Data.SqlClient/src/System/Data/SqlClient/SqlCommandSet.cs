// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace System.Data.SqlClient
{
    internal sealed class SqlCommandSet
    {
        private const string SqlIdentifierPattern = "^@[\\p{Lo}\\p{Lu}\\p{Ll}\\p{Lm}_@#][\\p{Lo}\\p{Lu}\\p{Ll}\\p{Lm}\\p{Nd}\uff3f_@#\\$]*$";
        private static readonly Regex s_sqlIdentifierParser = new Regex(SqlIdentifierPattern, RegexOptions.ExplicitCapture | RegexOptions.Singleline);

        private List<LocalCommand> _commandList = new List<LocalCommand>();

        private SqlCommand _batchCommand;

        private sealed class LocalCommand
        {
            internal readonly string CommandText;
            internal readonly SqlParameterCollection Parameters;
            internal readonly int ReturnParameterIndex;
            internal readonly CommandType CmdType;

            internal LocalCommand(string commandText, SqlParameterCollection parameters, int returnParameterIndex, CommandType cmdType)
            {
                Debug.Assert(0 <= commandText.Length, "no text");
                CommandText = commandText;
                Parameters = parameters;
                ReturnParameterIndex = returnParameterIndex;
                CmdType = cmdType;
            }
        }

        internal SqlCommandSet() : base()
        {
            _batchCommand = new SqlCommand();
        }

        private SqlCommand BatchCommand
        {
            get
            {
                SqlCommand command = _batchCommand;
                if (null == command)
                {
                    throw ADP.ObjectDisposed(this);
                }
                return command;
            }
        }

        internal int CommandCount => CommandList.Count;

        private List<LocalCommand> CommandList
        {
            get
            {
                List<LocalCommand> commandList = _commandList;
                if (null == commandList)
                {
                    throw ADP.ObjectDisposed(this);
                }
                return commandList;
            }
        }

        internal int CommandTimeout
        {
            set
            {
                BatchCommand.CommandTimeout = value;
            }
        }

        internal SqlConnection Connection
        {
            get
            {
                return BatchCommand.Connection;
            }
            set
            {
                BatchCommand.Connection = value;
            }
        }

        internal SqlTransaction Transaction
        {
            set
            {
                BatchCommand.Transaction = value;
            }
        }

        internal void Append(SqlCommand command)
        {
            ADP.CheckArgumentNull(command, nameof(command));

            string cmdText = command.CommandText;
            if (string.IsNullOrEmpty(cmdText))
            {
                throw ADP.CommandTextRequired(nameof(Append));
            }

            CommandType commandType = command.CommandType;
            switch (commandType)
            {
                case CommandType.Text:
                case CommandType.StoredProcedure:
                    break;
                case CommandType.TableDirect:
                    throw SQL.NotSupportedCommandType(commandType);
                default:
                    throw ADP.InvalidCommandType(commandType);
            }

            SqlParameterCollection parameters = null;

            SqlParameterCollection collection = command.Parameters;
            if (0 < collection.Count)
            {
                parameters = new SqlParameterCollection();

                // clone parameters so they aren't destroyed
                for (int i = 0; i < collection.Count; ++i)
                {
                    SqlParameter p = new SqlParameter();
                    collection[i].CopyTo(p);
                    parameters.Add(p);

                    // SQL Injection awareness
                    if (!s_sqlIdentifierParser.IsMatch(p.ParameterName))
                    {
                        throw ADP.BadParameterName(p.ParameterName);
                    }
                }

                foreach (SqlParameter p in parameters)
                {
                    // deep clone the parameter value if byte[] or char[]
                    object obj = p.Value;
                    byte[] byteValues = (obj as byte[]);
                    if (null != byteValues)
                    {
                        int offset = p.Offset;
                        int size = p.Size;
                        int countOfBytes = byteValues.Length - offset;
                        if ((0 != size) && (size < countOfBytes))
                        {
                            countOfBytes = size;
                        }
                        byte[] copy = new byte[Math.Max(countOfBytes, 0)];
                        Buffer.BlockCopy(byteValues, offset, copy, 0, copy.Length);
                        p.Offset = 0;
                        p.Value = copy;
                    }
                    else
                    {
                        char[] charValues = (obj as char[]);
                        if (null != charValues)
                        {
                            int offset = p.Offset;
                            int size = p.Size;
                            int countOfChars = charValues.Length - offset;
                            if ((0 != size) && (size < countOfChars))
                            {
                                countOfChars = size;
                            }
                            char[] copy = new char[Math.Max(countOfChars, 0)];
                            Buffer.BlockCopy(charValues, offset, copy, 0, copy.Length * 2);
                            p.Offset = 0;
                            p.Value = copy;
                        }
                        else
                        {
                            ICloneable cloneable = (obj as ICloneable);
                            if (null != cloneable)
                            {
                                p.Value = cloneable.Clone();
                            }
                        }
                    }
                }
            }

            int returnParameterIndex = -1;
            if (null != parameters)
            {
                for (int i = 0; i < parameters.Count; ++i)
                {
                    if (ParameterDirection.ReturnValue == parameters[i].Direction)
                    {
                        returnParameterIndex = i;
                        break;
                    }
                }
            }
            LocalCommand cmd = new LocalCommand(cmdText, parameters, returnParameterIndex, command.CommandType);
            CommandList.Add(cmd);
        }

        internal static void BuildStoredProcedureName(StringBuilder builder, string part)
        {
            if ((null != part) && (0 < part.Length))
            {
                if ('[' == part[0])
                {
                    int count = 0;
                    foreach (char c in part)
                    {
                        if (']' == c)
                        {
                            count++;
                        }
                    }
                    if (1 == (count % 2))
                    {
                        builder.Append(part);
                        return;
                    }
                }

                // the part is not escaped, escape it now
                SqlServerEscapeHelper.EscapeIdentifier(builder, part);
            }
        }

        internal void Clear()
        {
            DbCommand batchCommand = BatchCommand;
            if (null != batchCommand)
            {
                batchCommand.Parameters.Clear();
                batchCommand.CommandText = null;
            }
            List<LocalCommand> commandList = _commandList;
            if (null != commandList)
            {
                commandList.Clear();
            }
        }

        internal void Dispose()
        {
            SqlCommand command = _batchCommand;
            _commandList = null;
            _batchCommand = null;

            if (null != command)
            {
                command.Dispose();
            }
        }

        internal int ExecuteNonQuery()
        {
            ValidateCommandBehavior(nameof(ExecuteNonQuery), CommandBehavior.Default);

            BatchCommand.BatchRPCMode = true;
            BatchCommand.ClearBatchCommand();
            BatchCommand.Parameters.Clear();
            for (int ii = 0; ii < _commandList.Count; ii++)
            {
                LocalCommand cmd = _commandList[ii];
                BatchCommand.AddBatchCommand(cmd.CommandText, cmd.Parameters, cmd.CmdType);
            }

            return BatchCommand.ExecuteBatchRPCCommand();
        }

        internal SqlParameter GetParameter(int commandIndex, int parameterIndex)
            => CommandList[commandIndex].Parameters[parameterIndex];

        internal bool GetBatchedAffected(int commandIdentifier, out int recordsAffected, out Exception error)
        {
            error = BatchCommand.GetErrors(commandIdentifier);
            int? affected = BatchCommand.GetRecordsAffected(commandIdentifier);
            recordsAffected = affected.GetValueOrDefault();
            return affected.HasValue;
        }

        internal int GetParameterCount(int commandIndex)
            => CommandList[commandIndex].Parameters.Count;

        private void ValidateCommandBehavior(string method, CommandBehavior behavior)
        {
            if (0 != (behavior & ~(CommandBehavior.SequentialAccess | CommandBehavior.CloseConnection)))
            {
                ADP.ValidateCommandBehavior(behavior);
                throw ADP.NotSupportedCommandBehavior(behavior & ~(CommandBehavior.SequentialAccess | CommandBehavior.CloseConnection), method);
            }
        }
    }
}

