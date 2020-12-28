// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Internal.CommandLine
{
    public partial class ArgumentSyntax
    {
        private static readonly Func<string, string> s_stringParser = v => v;
        private static readonly Func<string, bool> s_booleanParser = v => bool.Parse(v);
        private static readonly Func<string, int> s_int32Parser = v => int.Parse(v, CultureInfo.InvariantCulture);

        // Commands

        public ArgumentCommand<string> DefineCommand(string name)
        {
            return DefineCommand(name, name);
        }

        public ArgumentCommand<T> DefineCommand<T>(string name, ref T command, T value, string help)
        {
            var result = DefineCommand(name, value);
            result.Help = help;

            if (_activeCommand == result)
                command = value;

            return result;
        }

        public ArgumentCommand<string> DefineCommand(string name, ref string value, string help)
        {
            return DefineCommand(name, ref value, name, help);
        }

        // Options

        public Argument<string> DefineOption(string name, string defaultValue)
        {
            return DefineOption(name, defaultValue, s_stringParser);
        }

        public Argument<bool> DefineOption(string name, bool defaultValue)
        {
            return DefineOption(name, defaultValue, s_booleanParser);
        }

        public Argument<int> DefineOption(string name, int defaultValue)
        {
            return DefineOption(name, defaultValue, s_int32Parser);
        }

        public Argument<string> DefineOption(string name, string defaultValue, bool requireValue)
        {
            return DefineOption(name, defaultValue, s_stringParser, requireValue);
        }

        public Argument<bool> DefineOption(string name, bool defaultValue, bool requireValue)
        {
            return DefineOption(name, defaultValue, s_booleanParser, requireValue);
        }

        public Argument<int> DefineOption(string name, int defaultValue, bool requireValue)
        {
            return DefineOption(name, defaultValue, s_int32Parser, requireValue);
        }

        public Argument<T> DefineOption<T>(string name, T defaultValue, Func<string, T> valueConverter)
        {
            return DefineOption(name, defaultValue, valueConverter, true);
        }

        public Argument<T> DefineOption<T>(string name, ref T value, Func<string, T> valueConverter, string help)
        {
            return DefineOption(name, ref value, valueConverter, true, help);
        }

        public Argument<T> DefineOption<T>(string name, ref T value, Func<string, T> valueConverter, bool requireValue, string help)
        {
            var option = DefineOption(name, value, valueConverter, requireValue);
            option.Help = help;

            value = option.Value;
            return option;
        }

        public Argument<string> DefineOption(string name, ref string value, string help)
        {
            return DefineOption(name, ref value, s_stringParser, help);
        }

        public Argument<bool> DefineOption(string name, ref bool value, string help)
        {
            return DefineOption(name, ref value, s_booleanParser, help);
        }

        public Argument<int> DefineOption(string name, ref int value, string help)
        {
            return DefineOption(name, ref value, s_int32Parser, help);
        }

        public Argument<string> DefineOption(string name, ref string value, bool requireValue, string help)
        {
            return DefineOption(name, ref value, s_stringParser, requireValue, help);
        }

        public Argument<bool> DefineOption(string name, ref bool value, bool requireValue, string help)
        {
            return DefineOption(name, ref value, s_booleanParser, requireValue, help);
        }

        public Argument<int> DefineOption(string name, ref int value, bool requireValue, string help)
        {
            return DefineOption(name, ref value, s_int32Parser, requireValue, help);
        }

        // Option lists

        public ArgumentList<string> DefineOptionList(string name, IReadOnlyList<string> defaultValue)
        {
            return DefineOptionList(name, defaultValue, s_stringParser);
        }

        public ArgumentList<int> DefineOptionList(string name, IReadOnlyList<int> defaultValue)
        {
            return DefineOptionList(name, defaultValue, s_int32Parser);
        }

        public ArgumentList<string> DefineOptionList(string name, IReadOnlyList<string> defaultValue, bool requireValue)
        {
            return DefineOptionList(name, defaultValue, s_stringParser, requireValue);
        }

        public ArgumentList<int> DefineOptionList(string name, IReadOnlyList<int> defaultValue, bool requireValue)
        {
            return DefineOptionList(name, defaultValue, s_int32Parser, requireValue);
        }

        public ArgumentList<T> DefineOptionList<T>(string name, IReadOnlyList<T> defaultValue, Func<string, T> valueConverter)
        {
            return DefineOptionList(name, defaultValue, valueConverter, true);
        }

        public ArgumentList<T> DefineOptionList<T>(string name, ref IReadOnlyList<T> value, Func<string, T> valueConverter, string help)
        {
            return DefineOptionList(name, ref value, valueConverter, true, help);
        }

        public ArgumentList<T> DefineOptionList<T>(string name, ref IReadOnlyList<T> value, Func<string, T> valueConverter, bool requireValue, string help)
        {
            var optionList = DefineOptionList(name, value, valueConverter, requireValue);
            optionList.Help = help;

            value = optionList.Value;
            return optionList;
        }

        public ArgumentList<string> DefineOptionList(string name, ref IReadOnlyList<string> value, string help)
        {
            return DefineOptionList(name, ref value, s_stringParser, help);
        }

        public ArgumentList<int> DefineOptionList(string name, ref IReadOnlyList<int> value, string help)
        {
            return DefineOptionList(name, ref value, s_int32Parser, help);
        }

        public ArgumentList<string> DefineOptionList(string name, ref IReadOnlyList<string> value, bool requireValue, string help)
        {
            return DefineOptionList(name, ref value, s_stringParser, requireValue, help);
        }

        public ArgumentList<int> DefineOptionList(string name, ref IReadOnlyList<int> value, bool requireValue, string help)
        {
            return DefineOptionList(name, ref value, s_int32Parser, requireValue, help);
        }

        // Parameters

        public Argument<string> DefineParameter(string name, string defaultValue)
        {
            return DefineParameter(name, defaultValue, s_stringParser);
        }

        public Argument<bool> DefineParameter(string name, bool defaultValue)
        {
            return DefineParameter(name, defaultValue, s_booleanParser);
        }

        public Argument<int> DefineParameter(string name, int defaultValue)
        {
            return DefineParameter(name, defaultValue, s_int32Parser);
        }

        public Argument<T> DefineParameter<T>(string name, ref T value, Func<string, T> valueConverter, string help)
        {
            var parameter = DefineParameter(name, value, valueConverter);
            parameter.Help = help;

            value = parameter.Value;
            return parameter;
        }

        public Argument<string> DefineParameter(string name, ref string value, string help)
        {
            return DefineParameter(name, ref value, s_stringParser, help);
        }

        public Argument<bool> DefineParameter(string name, ref bool value, string help)
        {
            return DefineParameter(name, ref value, s_booleanParser, help);
        }

        public Argument<int> DefineParameter(string name, ref int value, string help)
        {
            return DefineParameter(name, ref value, s_int32Parser, help);
        }

        // Parameter list

        public ArgumentList<string> DefineParameterList(string name, IReadOnlyList<string> defaultValue)
        {
            return DefineParameterList(name, defaultValue, s_stringParser);
        }

        public ArgumentList<int> DefineParameterList(string name, IReadOnlyList<int> defaultValue)
        {
            return DefineParameterList(name, defaultValue, s_int32Parser);
        }

        public ArgumentList<T> DefineParameterList<T>(string name, ref IReadOnlyList<T> value, Func<string, T> valueConverter, string help)
        {
            var parameterList = DefineParameterList(name, value, valueConverter);
            parameterList.Help = help;

            value = parameterList.Value;
            return parameterList;
        }

        public ArgumentList<string> DefineParameterList(string name, ref IReadOnlyList<string> value, string help)
        {
            return DefineParameterList(name, ref value, s_stringParser, help);
        }

        public ArgumentList<int> DefineParameterList(string name, ref IReadOnlyList<int> value, string help)
        {
            return DefineParameterList(name, ref value, s_int32Parser, help);
        }
    }
}
