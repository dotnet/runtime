// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel
{
    /// <summary>
    /// This attribute allows you to configure a filter that is used enable or disable toolbox items
    /// from being used on particular designers. For example, you may have a class of components
    /// that can only be used on a certain type of designer. You can configure a toolbox item
    /// filter to enforce that rule. For example, take a report designer that uses a component
    /// base class of ReportElement. You may want to make ReportElement toolbox items enabled only
    /// when a ReportDesigner is the active designer. To do this, you would add the following
    /// ToolboxItemFilter attributes to each class:
    ///
    /// [ToolboxItemFilter("MyReportFilterString", ToolboxItemFilterType.Require)]
    /// public class ReportElement : Component {}
    ///
    /// [ToolboxItemFilter("MyReportFilterString", ToolboxItemFilterType.Require)]
    /// public class ReportDesigner : Component {}
    ///
    /// These two filters specify that ReportElement toolbox items will only be
    /// enabled when a ReportDesigner is visible. By specifying a filter type of
    /// Require on the report designer class, this will disable any toolbox items
    /// that are not report elements. If the report designer specified a filter type
    /// of "Allow" instead of "Require", other components would be enabled when the
    /// report designer was active. ReportElements would still be disabled when
    /// other designers were active, however, because ReportElement requires designers
    /// to have the given filter string.
    ///
    /// Toolbox item filtering is a useful way to restrict toolbox item visibility to
    /// cases where it is appropriate. This can help to avoid confusion for users, but
    /// you should use caution not to make items unusually restrictive. If you have a
    /// general purpose component, for example, you should allow the component to appear
    /// on any designer.
    ///
    /// The ASP.NET and Windows Forms designers both use filter attributes to prevent
    /// each other's components from being enabled. This is a useful restriction because,
    /// since each has several duplicate class names, it may be confusing to users and
    /// they may not know which controls to choose.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class ToolboxItemFilterAttribute : Attribute
    {
        private string? _typeId;

        /// <summary>
        /// Initializes a new ToolboxItemFilterAttribute with the provide filter string and a filter type of
        /// "Allow".
        /// </summary>
        public ToolboxItemFilterAttribute(string filterString) : this(filterString, ToolboxItemFilterType.Allow)
        {
        }

        /// <summary>
        /// Initializes a new ToolboxItemFilterAttribute with the provide filter string and filter type.
        /// </summary>
        public ToolboxItemFilterAttribute(string filterString, ToolboxItemFilterType filterType)
        {
            FilterString = filterString ?? string.Empty;
            FilterType = filterType;
        }

        /// <summary>
        /// Retrieves the filter string for this attribute. The filter string is a user-defined string that
        /// is used to identify matching attributes.
        /// </summary>
        public string FilterString { get; }

        /// <summary>
        /// Retrieves the filter type for this attribute. The filter type determines how the filter string should
        /// be applied.
        /// </summary>
        public ToolboxItemFilterType FilterType { get; }

        /// <summary>
        /// The unique identifier for this attribute. All ToolboxItemFilterAttributes with the same filter string
        /// are considered the same, so they return the same TypeId.
        /// </summary>
        public override object TypeId => _typeId ?? (_typeId = GetType().FullName + FilterString);

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj == this)
            {
                return true;
            }

            return obj is ToolboxItemFilterAttribute other
                && other.FilterType.Equals(FilterType)
                && other.FilterString.Equals(FilterString);
        }

        public override int GetHashCode()
        {
            // No need to hash on filter type as well; there shouldn't be that many duplicates.
            return FilterString.GetHashCode();
        }

        public override bool Match([NotNullWhen(true)] object? obj)
        {
            return obj is ToolboxItemFilterAttribute other
                && other.FilterString.Equals(FilterString);
        }

        public override string ToString() => FilterString + "," + Enum.GetName(typeof(ToolboxItemFilterType), FilterType);
    }
}
