 //------------------------------------------------------------------------------
// <copyright file="DirectoryEntryConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

 namespace System.DirectoryServices.Design {    
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System;            
    using System.Reflection;
    using System.Collections; 
    using System.Collections.Specialized; 
    using System.Globalization;
        
    /// <include file='doc\DirectoryEntryConverter.uex' path='docs/doc[@for="DirectoryEntryConverter"]/*' />
    /// <internalonly/>
    internal class DirectoryEntryConverter : TypeConverter {
        private static StandardValuesCollection values;        
        private static Hashtable componentsCreated = new Hashtable(StringComparer.OrdinalIgnoreCase);
                        
        /// <include file='doc\DirectoryEntryConverter.uex' path='docs/doc[@for="DirectoryEntryConverter.CanConvertFrom"]/*' />
        /// <internalonly/>                               
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            if (sourceType == typeof(string)) {
                return true;
            }
            return base.CanConvertFrom(context, sourceType);
        }                        
                                 
        /// <include file='doc\DirectoryEntryConverter.uex' path='docs/doc[@for="DirectoryEntryConverter.ConvertFrom"]/*' />
        /// <internalonly/>                 
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
            if (value != null && value is string) {
               string text = ((string)value).Trim();
            
                if (text.Length == 0)
                    return null;

                if (text.CompareTo(Res.GetString(Res.DSNotSet)) != 0) {
                    DirectoryEntry newEntry = GetFromCache(text);
                    if (newEntry == null) {                                          
                        newEntry = new DirectoryEntry(text);  
                        componentsCreated[text] = newEntry;
                        if (context != null)
                            context.Container.Add(newEntry);                            
                             
                        return newEntry;                                            
                    }                        
                }                    
            }
            
            return null;
        }
                                                                                   
        /// <include file='doc\DirectoryEntryConverter.uex' path='docs/doc[@for="DirectoryEntryConverter.ConvertTo"]/*' />
        /// <internalonly/>                 
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
            if (destinationType != null && destinationType == typeof(string)) {                
                if (value != null)
                    return((DirectoryEntry)value).Path;
                else
                    return Res.GetString(Res.DSNotSet);
            }
            
            return base.ConvertTo(context, culture, value, destinationType);
        }         
        
        /// <include file='doc\MessageFormatterConverter.uex' path='docs/doc[@for="DirectoryEntryConverter.GetStandardValues"]/*' />
        /// <internalonly/>            
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) {
            if (values == null) {
                values = new StandardValuesCollection(new object[] {null});
            }
            return values;
        }
        
        internal static DirectoryEntry GetFromCache(string path) {
            if (componentsCreated.ContainsKey(path)) {
                DirectoryEntry existingComponent = (DirectoryEntry)componentsCreated[path];
                if (existingComponent.Site == null)
                    componentsCreated.Remove(path);
                else {                                                                                        
                    if (existingComponent.Path == path)                                
                        return existingComponent;
                    else                            
                        componentsCreated.Remove(path);    
                }                                                                    
            }
            
            return null;
        }
                                 
                                 
        /// <include file='doc\MessageFormatterConverter.uex' path='docs/doc[@for="DirectoryEntryConverter.GetStandardValuesExclusive"]/*' />
        /// <internalonly/>                                   
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) {
            return false;
        }
        
        /// <include file='doc\MessageFormatterConverter.uex' path='docs/doc[@for="DirectoryEntryConverter.GetStandardValuesSupported"]/*' />
        /// <internalonly/>                        
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) {
            return true;
        }                   
    }                                    
}            
  
