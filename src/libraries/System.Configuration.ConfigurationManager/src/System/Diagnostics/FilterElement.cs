//------------------------------------------------------------------------------
// <copyright file="FilterElement.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
using System.Configuration;
using System;

namespace System.Diagnostics {
    internal class FilterElement : TypedElement {
        public FilterElement() : base(typeof(TraceFilter)) {}

        public TraceFilter GetRuntimeObject() {
            TraceFilter newFilter = (TraceFilter) BaseGetRuntimeObject();
            newFilter.initializeData = InitData;
            return newFilter;
        }

        internal TraceFilter RefreshRuntimeObject(TraceFilter filter) {
            if (Type.GetType(TypeName) != filter.GetType() || InitData != filter.initializeData) {
                // type or initdata changed
                _runtimeObject = null;
                return GetRuntimeObject();
            }
            else {
                return filter;
            }
        }
    }
}


