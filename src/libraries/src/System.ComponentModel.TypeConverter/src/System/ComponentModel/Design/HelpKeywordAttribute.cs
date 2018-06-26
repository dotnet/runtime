// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.ComponentModel.Design
{
    /// <summary>
    /// Allows specification of the context keyword that will be specified for this class or member. By default,
    /// the help keyword for a class is the Type's full name, and for a member it's the full name of the type that declared the property,
    /// plus the property name itself.
    ///
    /// For example, consider System.Windows.Forms.Button and it's Text property:
    ///
    /// The class keyword is "System.Windows.Forms.Button", but the Text property keyword is "System.Windows.Forms.Control.Text", because the Text
    /// property is declared on the System.Windows.Forms.Control class rather than the Button class itself; the Button class inherits the property. 
    /// By contrast, the DialogResult property is declared on the Button so its keyword would be "System.Windows.Forms.Button.DialogResult".
    ///
    /// When the help system gets the keywords, it will first look at this attribute. At the class level, it will return the string specified by the 
    /// HelpContextAttribute. Note this will not be used for members of the Type in question. They will still reflect the declaring Type's actual
    /// full name, plus the member name. To override this, place the attribute on the member itself.
    ///
    /// Example:
    ///
    /// [HelpKeywordAttribute(typeof(Component))] 
    /// public class MyComponent : Component {
    /// 
    /// 
    /// public string Property1 { get{return "";};
    ///
    /// [HelpKeywordAttribute("SomeNamespace.SomeOtherClass.Property2")]
    /// public string Property2 { get{return "";};
    ///
    /// }
    ///
    ///
    /// For the above class (default without attribution):
    ///
    /// Class keyword: "System.ComponentModel.Component" ("MyNamespace.MyComponent')
    /// Property1 keyword: "MyNamespace.MyComponent.Property1" (default)
    /// Property2 keyword: "SomeNamespace.SomeOtherClass.Property2" ("MyNamespace.MyComponent.Property2")
    ///
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
    public sealed class HelpKeywordAttribute : Attribute
    {
        /// <summary>
        /// Default value for HelpKeywordAttribute, which is null. 
        /// </summary>
        public static readonly HelpKeywordAttribute Default = new HelpKeywordAttribute();

        /// <summary>
        /// Default constructor, which creates an attribute with a null HelpKeyword.
        /// </summary>
        public HelpKeywordAttribute()
        {
        }

        /// <summary>
        /// Creates a HelpKeywordAttribute with the value being the given keyword string.
        /// </summary>
        public HelpKeywordAttribute(string keyword)
        {
            HelpKeyword = keyword ?? throw new ArgumentNullException(nameof(keyword));
        }

        /// <summary>
        /// Creates a HelpKeywordAttribute with the value being the full name of the given type.
        /// </summary>
        public HelpKeywordAttribute(Type t)
        {
            if (t == null)
            {
                throw new ArgumentNullException(nameof(t));
            }

            HelpKeyword = t.FullName;
        }

        /// <summary>
        /// Retrieves the HelpKeyword this attribute supplies.
        /// </summary>
        public string HelpKeyword { get; }

        /// <summary>
        /// Two instances of a HelpKeywordAttribute are equal if they're HelpKeywords are equal.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }
            if ((obj != null) && (obj is HelpKeywordAttribute))
            {
                return ((HelpKeywordAttribute)obj).HelpKeyword == HelpKeyword;
            }

            return false;
        }

        /// <summary>
        /// </summary>
        public override int GetHashCode() => base.GetHashCode();

        /// <summary>
        /// Returns true if this Attribute's HelpKeyword is null.
        /// </summary>
        public override bool IsDefaultAttribute() => Equals(Default);
    }
}
