// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

namespace TestUtilities
{
    /// <summary>
    /// Used to tag test methods or types which are created for a given WorkItem
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class WorkItemAttribute : TraitAttribute
    {
        public const string WorkItemAttributeName = "WorkItem";

        private readonly int id;
        private readonly string description;

        public int Id
        {
            get { return id; }
        }

        public string Description
        {
            get { return description; }
        }

        public WorkItemAttribute(int id)
            : this(id, string.Empty)
        {
        }

        public WorkItemAttribute(int id, string description)
            : base(WorkItemAttributeName, id.ToString())
        {
            this.id = id;
            this.description = description;
        }
    }
}