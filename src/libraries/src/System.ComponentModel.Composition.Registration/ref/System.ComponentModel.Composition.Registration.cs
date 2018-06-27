// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.ComponentModel.Composition.Registration
{
    public sealed partial class ExportBuilder
    {
        public ExportBuilder() { }
        public System.ComponentModel.Composition.Registration.ExportBuilder AddMetadata(string name, System.Func<System.Type, object> itemFunc) { throw null; }
        public System.ComponentModel.Composition.Registration.ExportBuilder AddMetadata(string name, object value) { throw null; }
        public System.ComponentModel.Composition.Registration.ExportBuilder AsContractName(string contractName) { throw null; }
        public System.ComponentModel.Composition.Registration.ExportBuilder AsContractType(System.Type type) { throw null; }
        public System.ComponentModel.Composition.Registration.ExportBuilder AsContractType<T>() { throw null; }
        public System.ComponentModel.Composition.Registration.ExportBuilder Inherited() { throw null; }
    }
    public sealed partial class ImportBuilder
    {
        public ImportBuilder() { }
        public System.ComponentModel.Composition.Registration.ImportBuilder AllowDefault() { throw null; }
        public System.ComponentModel.Composition.Registration.ImportBuilder AllowRecomposition() { throw null; }
        public System.ComponentModel.Composition.Registration.ImportBuilder AsContractName(string contractName) { throw null; }
        public System.ComponentModel.Composition.Registration.ImportBuilder AsContractType(System.Type type) { throw null; }
        public System.ComponentModel.Composition.Registration.ImportBuilder AsContractType<T>() { throw null; }
        public System.ComponentModel.Composition.Registration.ImportBuilder AsMany(bool isMany = true) { throw null; }
        public System.ComponentModel.Composition.Registration.ImportBuilder RequiredCreationPolicy(System.ComponentModel.Composition.CreationPolicy requiredCreationPolicy) { throw null; }
        public System.ComponentModel.Composition.Registration.ImportBuilder Source(System.ComponentModel.Composition.ImportSource source) { throw null; }
    }
    public partial class ParameterImportBuilder
    {
        public ParameterImportBuilder() { }
        public T Import<T>() { throw null; }
        public T Import<T>(System.Action<System.ComponentModel.Composition.Registration.ImportBuilder> configure) { throw null; }
    }
    public partial class PartBuilder
    {
        internal PartBuilder() { }
        public System.ComponentModel.Composition.Registration.PartBuilder AddMetadata(string name, System.Func<System.Type, object> itemFunc) { throw null; }
        public System.ComponentModel.Composition.Registration.PartBuilder AddMetadata(string name, object value) { throw null; }
        public System.ComponentModel.Composition.Registration.PartBuilder Export() { throw null; }
        public System.ComponentModel.Composition.Registration.PartBuilder Export(System.Action<System.ComponentModel.Composition.Registration.ExportBuilder> exportConfiguration) { throw null; }
        public System.ComponentModel.Composition.Registration.PartBuilder ExportInterfaces() { throw null; }
        public System.ComponentModel.Composition.Registration.PartBuilder ExportInterfaces(System.Predicate<System.Type> interfaceFilter) { throw null; }
        public System.ComponentModel.Composition.Registration.PartBuilder ExportInterfaces(System.Predicate<System.Type> interfaceFilter, System.Action<System.Type, System.ComponentModel.Composition.Registration.ExportBuilder> exportConfiguration) { throw null; }
        public System.ComponentModel.Composition.Registration.PartBuilder ExportProperties(System.Predicate<System.Reflection.PropertyInfo> propertyFilter) { throw null; }
        public System.ComponentModel.Composition.Registration.PartBuilder ExportProperties(System.Predicate<System.Reflection.PropertyInfo> propertyFilter, System.Action<System.Reflection.PropertyInfo, System.ComponentModel.Composition.Registration.ExportBuilder> exportConfiguration) { throw null; }
        public System.ComponentModel.Composition.Registration.PartBuilder ExportProperties<T>(System.Predicate<System.Reflection.PropertyInfo> propertyFilter) { throw null; }
        public System.ComponentModel.Composition.Registration.PartBuilder ExportProperties<T>(System.Predicate<System.Reflection.PropertyInfo> propertyFilter, System.Action<System.Reflection.PropertyInfo, System.ComponentModel.Composition.Registration.ExportBuilder> exportConfiguration) { throw null; }
        public System.ComponentModel.Composition.Registration.PartBuilder Export<T>() { throw null; }
        public System.ComponentModel.Composition.Registration.PartBuilder Export<T>(System.Action<System.ComponentModel.Composition.Registration.ExportBuilder> exportConfiguration) { throw null; }
        public System.ComponentModel.Composition.Registration.PartBuilder ImportProperties(System.Predicate<System.Reflection.PropertyInfo> propertyFilter) { throw null; }
        public System.ComponentModel.Composition.Registration.PartBuilder ImportProperties(System.Predicate<System.Reflection.PropertyInfo> propertyFilter, System.Action<System.Reflection.PropertyInfo, System.ComponentModel.Composition.Registration.ImportBuilder> importConfiguration) { throw null; }
        public System.ComponentModel.Composition.Registration.PartBuilder ImportProperties<T>(System.Predicate<System.Reflection.PropertyInfo> propertyFilter) { throw null; }
        public System.ComponentModel.Composition.Registration.PartBuilder ImportProperties<T>(System.Predicate<System.Reflection.PropertyInfo> propertyFilter, System.Action<System.Reflection.PropertyInfo, System.ComponentModel.Composition.Registration.ImportBuilder> importConfiguration) { throw null; }
        public System.ComponentModel.Composition.Registration.PartBuilder SelectConstructor(System.Func<System.Reflection.ConstructorInfo[], System.Reflection.ConstructorInfo> constructorFilter) { throw null; }
        public System.ComponentModel.Composition.Registration.PartBuilder SelectConstructor(System.Func<System.Reflection.ConstructorInfo[], System.Reflection.ConstructorInfo> constructorFilter, System.Action<System.Reflection.ParameterInfo, System.ComponentModel.Composition.Registration.ImportBuilder> importConfiguration) { throw null; }
        public System.ComponentModel.Composition.Registration.PartBuilder SetCreationPolicy(System.ComponentModel.Composition.CreationPolicy creationPolicy) { throw null; }
    }
    public partial class PartBuilder<T> : System.ComponentModel.Composition.Registration.PartBuilder
    {
        internal PartBuilder() { }
        public System.ComponentModel.Composition.Registration.PartBuilder<T> ExportProperty(System.Linq.Expressions.Expression<System.Func<T, object>> propertyFilter) { throw null; }
        public System.ComponentModel.Composition.Registration.PartBuilder<T> ExportProperty(System.Linq.Expressions.Expression<System.Func<T, object>> propertyFilter, System.Action<System.ComponentModel.Composition.Registration.ExportBuilder> exportConfiguration) { throw null; }
        public System.ComponentModel.Composition.Registration.PartBuilder<T> ExportProperty<TContract>(System.Linq.Expressions.Expression<System.Func<T, object>> propertyFilter) { throw null; }
        public System.ComponentModel.Composition.Registration.PartBuilder<T> ExportProperty<TContract>(System.Linq.Expressions.Expression<System.Func<T, object>> propertyFilter, System.Action<System.ComponentModel.Composition.Registration.ExportBuilder> exportConfiguration) { throw null; }
        public System.ComponentModel.Composition.Registration.PartBuilder<T> ImportProperty(System.Linq.Expressions.Expression<System.Func<T, object>> propertyFilter) { throw null; }
        public System.ComponentModel.Composition.Registration.PartBuilder<T> ImportProperty(System.Linq.Expressions.Expression<System.Func<T, object>> propertyFilter, System.Action<System.ComponentModel.Composition.Registration.ImportBuilder> importConfiguration) { throw null; }
        public System.ComponentModel.Composition.Registration.PartBuilder<T> ImportProperty<TContract>(System.Linq.Expressions.Expression<System.Func<T, object>> propertyFilter) { throw null; }
        public System.ComponentModel.Composition.Registration.PartBuilder<T> ImportProperty<TContract>(System.Linq.Expressions.Expression<System.Func<T, object>> propertyFilter, System.Action<System.ComponentModel.Composition.Registration.ImportBuilder> importConfiguration) { throw null; }
        public System.ComponentModel.Composition.Registration.PartBuilder<T> SelectConstructor(System.Linq.Expressions.Expression<System.Func<System.ComponentModel.Composition.Registration.ParameterImportBuilder, T>> constructorFilter) { throw null; }
    }
    public partial class RegistrationBuilder : System.Reflection.Context.CustomReflectionContext
    {
        public RegistrationBuilder() { }
        public System.ComponentModel.Composition.Registration.PartBuilder ForType(System.Type type) { throw null; }
        public System.ComponentModel.Composition.Registration.PartBuilder ForTypesDerivedFrom(System.Type type) { throw null; }
        public System.ComponentModel.Composition.Registration.PartBuilder<T> ForTypesDerivedFrom<T>() { throw null; }
        public System.ComponentModel.Composition.Registration.PartBuilder ForTypesMatching(System.Predicate<System.Type> typeFilter) { throw null; }
        public System.ComponentModel.Composition.Registration.PartBuilder<T> ForTypesMatching<T>(System.Predicate<System.Type> typeFilter) { throw null; }
        public System.ComponentModel.Composition.Registration.PartBuilder<T> ForType<T>() { throw null; }
        protected override System.Collections.Generic.IEnumerable<object> GetCustomAttributes(System.Reflection.MemberInfo member, System.Collections.Generic.IEnumerable<object> declaredAttributes) { throw null; }
        protected override System.Collections.Generic.IEnumerable<object> GetCustomAttributes(System.Reflection.ParameterInfo parameter, System.Collections.Generic.IEnumerable<object> declaredAttributes) { throw null; }
    }
}
