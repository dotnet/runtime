// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace JsonToItemsTaskFactory
{

    /// <summary>Reads a json input blob and populates some output items</summary>
    ///
    /// <example>JSON should follow this structure - the toplevel "properties" and "items" keys are exact, other keys are arbitrary.
    /// <code>
    /// {
    ///    "properties" : {
    ///      "propName1": "value1",
    ///      "propName2": "value"
    ///    },
    ///    "items" : {
    ///      "itemName1": [ "stringValue", { "identity": "anotherValue", "metadataKey": "metadataValue", ... }, "thirdValue" ],
    ///      "itemName2": [ ... ]
    /// }
    /// </code>
    ///
    /// A task can be declared by
    ///
    /// <code>
    /// <UsingTask AssemblyFile="..." TaskName="MyJsonReader" TaskFactory="Microsoft.DotNet.Runtime.Tasks.JsonToItemsTaskFactory">
    ///   <ParameterGroup>
    ///     <PropName1 ParameterType="System.String" Required="False" Output="True" />
    ///     <ItemName1 ParameterType="Microsoft.Build.Framework.ITaskItem[]" Required="False" Output="True" />
    ///   <ParameterGroup>
    /// </UsingTask>
    /// </code>
    ///
    /// And then used in a target.  The `JsonFilePath' attribute is used to specify the json file to read.
    ///
    /// <code>
    /// <Target Name="UseMyReader">
    ///   <MyJsonReader JsonFilePath="foo.json">
    ///     <Output TaskParameter="PropName1" PropertyName="MyParsedProperty" />
    ///     <Output TaskParameter="ItemName1" ItemName="MyParsedItems" />
    ///   </MyJsonReader>
    ///   <Message Priority="High" Text=" Got property $(MyParsedProperty) and items @(MyParsedItems)" />
    /// </Target>
    /// </code>
    /// </example>
    public class JsonToItemsTaskFactory : ITaskFactory
    {
        private const string JsonFilePath = "JsonFilePath";
        private TaskPropertyInfo[]? _taskProperties;
        private string? _taskName;

        private bool _logDebugTask;

        public JsonToItemsTaskFactory() {}

        public string FactoryName => "JsonToItemsTaskFactory";

        public Type TaskType => typeof(JsonToItemsTask);

        public bool Initialize(string taskName, IDictionary<string, TaskPropertyInfo> parameterGroup, string? taskBody, IBuildEngine taskFactoryLoggingHost)
        {
            _taskName = taskName;
            if (taskBody != null && taskBody.StartsWith("debug", StringComparison.InvariantCultureIgnoreCase))
                _logDebugTask = true;
            var log = new TaskLoggingHelper(taskFactoryLoggingHost, _taskName);
            if (!ValidateParameterGroup (parameterGroup, log))
                return false;
            _taskProperties = new TaskPropertyInfo[parameterGroup.Count + 1];
            _taskProperties[0] = new TaskPropertyInfo(nameof(JsonFilePath), typeof(string), output: false, required: true);
            parameterGroup.Values.CopyTo(_taskProperties, 1);
            return true;
        }

        public TaskPropertyInfo[] GetTaskParameters() => _taskProperties!;

        public ITask CreateTask(IBuildEngine taskFactoryLoggingHost)
        {
            var log = new TaskLoggingHelper(taskFactoryLoggingHost, _taskName);
            if (_logDebugTask) log.LogMessage(MessageImportance.Low, "CreateTask called");
            return new JsonToItemsTask(_taskName!, _logDebugTask);
        }

        public void CleanupTask(ITask task) {}

        internal bool ValidateParameterGroup(IDictionary<string, TaskPropertyInfo> parameterGroup, TaskLoggingHelper log)
        {
            var taskName = _taskName ?? "";
            foreach (var kvp in parameterGroup)
            {
                var propName = kvp.Key;
                var propInfo = kvp.Value;
                if (string.Equals(propName, nameof(JsonFilePath), StringComparison.InvariantCultureIgnoreCase))
                {
                    log.LogError($"Task {taskName}: {nameof(JsonFilePath)} parameter must not be declared. It is implicitly added by the task.");
                    continue;
                }

                if (!propInfo.Output)
                {
                    log.LogError($"Task {taskName}: parameter {propName} is not an output. All parameters except {nameof(JsonFilePath)} must be outputs");
                    continue;
                }
                if (propInfo.Required)
                {
                    log.LogError($"Task {taskName}: parameter {propName} is an output but is marked required. That's not supported.");
                }
                if (typeof(ITaskItem[]).IsAssignableFrom(propInfo.PropertyType))
                    continue; // ok, an item list
                if (typeof(string).IsAssignableFrom(propInfo.PropertyType))
                    continue; // ok, a string property

                log.LogError($"Task {taskName}: parameter {propName} is not an output of type System.String or Microsoft.Build.Framework.ITaskItem[]");
            }
            return !log.HasLoggedErrors;
        }

        public class JsonToItemsTask : IGeneratedTask
        {
            private IBuildEngine? _buildEngine;
            public IBuildEngine BuildEngine { get => _buildEngine!; set { _buildEngine = value; SetBuildEngine(value);} }
            public ITaskHost? HostObject { get; set; }

            private TaskLoggingHelper? _log;
            private TaskLoggingHelper Log { get => _log!; set { _log = value; } }

            private void SetBuildEngine(IBuildEngine buildEngine)
            {
                Log = new TaskLoggingHelper(buildEngine, TaskName);
            }

            private string? jsonFilePath;

            private readonly bool _logDebugTask; // print stuff to the log for debugging the task

            private JsonModelRoot? jsonModel;
            public string TaskName {get;}
            public JsonToItemsTask(string taskName, bool logDebugTask = false)
            {
                TaskName = taskName;
                _logDebugTask = logDebugTask;
            }

            public bool Execute()
            {
                if (!JsonToItemsReader.TryRead(jsonFilePath, Log, out JsonModelRoot? json))
                    return false;

                if (_logDebugTask)
                {
                    LogParsedJson(json);
                }
                jsonModel = json;
                return true;
            }

            internal void LogParsedJson (JsonModelRoot json)
            {
                if (json.Properties != null)
                {
                    Log.LogMessage(MessageImportance.Low, "json has properties: ");
                    foreach (var property in json.Properties)
                    {
                        Log.LogMessage(MessageImportance.Low, $"  {property.Key} = {property.Value}");
                    }
                }
                if (json.Items != null)
                {
                    Log.LogMessage(MessageImportance.Low, "items: ");
                    foreach (var item in json.Items)
                    {
                        Log.LogMessage(MessageImportance.Low, $"  {item.Key} = [");
                        foreach (var value in item.Value)
                        {
                            Log.LogMessage(MessageImportance.Low, $"    {value.Identity}");
                            if (value.Metadata != null)
                            {
                                Log.LogMessage(MessageImportance.Low, "       and some metadata, too");
                            }
                        }
                        Log.LogMessage(MessageImportance.Low, "  ]");
                    }
                }
            }

            public object? GetPropertyValue(TaskPropertyInfo property)
            {
                bool isItem = false;
                if (typeof(ITaskItem[]).IsAssignableFrom(property.PropertyType))
                {
                    if (_logDebugTask) Log.LogMessage(MessageImportance.Low, "GetPropertyValue called with @({0})", property.Name);
                    isItem = true;
                }
                else
                {
                    if (_logDebugTask) Log.LogMessage(MessageImportance.Low, "GetPropertyValue called with $({0})", property.Name);
                }
                if (!isItem)
                {
                    if (jsonModel?.Properties != null &&  jsonModel.Properties.TryGetValue(property.Name, out var value))
                    {
                        return value;
                    }
                    Log.LogError("Property {0} not found in {1}", property.Name, jsonFilePath);
                    throw new Exception();
                }
                else
                {
                    if (jsonModel?.Items != null && jsonModel.Items.ContainsKey(property.Name))
                    {
                        return jsonModel.GetItems(property.Name);
                    }

                }
                return null;
            }

            public void SetPropertyValue(TaskPropertyInfo property, object? value)
            {
                if (_logDebugTask) Log.LogMessage(MessageImportance.Low, "SetPropertyValue called with {0}", property.Name);
                if (property.Name == "JsonFilePath")
                {
                    jsonFilePath = (string)value!;
                }
                else
                    throw new Exception($"JsonToItemsTask {TaskName} cannot set property {property.Name}");
            }

        }
    }
}
