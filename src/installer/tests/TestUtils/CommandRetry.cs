
using System;
using System.Diagnostics;
using System.Threading;
using FluentAssertions;
using Microsoft.DotNet.CoreSetup.Test;

namespace Microsoft.DotNet.Cli.Build.Framework
{
    public class RetriedCommand
    {
        string _commandName;
        public RetriedCommand(string commandName)
        {
            _commandName = commandName;
        }

        protected virtual Command CreateCommand()
        {
            return Command.Create(_commandName);
        }

        protected virtual int MaxRetries => 3;
        protected virtual int RetryDelay => 50;

        protected virtual CommandResult ExecuteCommand(Command command)
        {
            return command.Execute();
        }

        /// <summary>
        /// Validates the command. This should return true on a successful command result, false when it should be retried, and throw an exception when it should not be retried.
        /// </summary>
        protected virtual bool Validate(CommandResult result)
        {
            result.Should().Pass();
            return true;
        }

        public void Run()
        {
            for (int i = 0; i < MaxRetries; i++)
            {
                Command command = CreateCommand();
                CommandResult result = ExecuteCommand(command);
                if (Validate(result))
                {
                    return;
                }
                Thread.Sleep(50);
            }
            throw new UnreachableException("Should not reach here");
        }
    }

    public class CustomizableCommand(string commandName, Func<CommandResult, bool>? _validate = null, Func<Command>? _createCommand = null) : RetriedCommand(commandName)
    {
        protected override bool Validate(CommandResult result)
        {
            if (_validate is not null)
            {
                return _validate(result);
            }
            return base.Validate(result);
        }

        protected override Command CreateCommand()
        {
            if (_createCommand is not null)
            {
                return _createCommand();
            }
            return base.CreateCommand();
        }
    }

    public class RetryOnSigKillCommand(string commandName, Func<CommandResult, bool>? _validate = null, Func<Command>? _createCommand = null) : CustomizableCommand(commandName, _validate, _createCommand)
    {
        protected override bool Validate(CommandResult result)
        {
            if (result.ExitCode == 139)
            {
                return false;
            }
            return base.Validate(result);
        }
    }
}
