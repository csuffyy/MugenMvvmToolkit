#region Copyright
// ****************************************************************************
// <copyright file="DataTemplateSelectorBase.cs">
// Copyright � Vyacheslav Volkov 2012-2014
// </copyright>
// ****************************************************************************
// <author>Vyacheslav Volkov</author>
// <email>vvs0205@outlook.com</email>
// <project>MugenMvvmToolkit</project>
// <web>https://github.com/MugenMvvmToolkit/MugenMvvmToolkit</web>
// <license>
// See license.txt in this solution or http://opensource.org/licenses/MS-PL
// </license>
// ****************************************************************************
#endregion
using MugenMvvmToolkit.Binding.Builders;
using MugenMvvmToolkit.Interfaces;

namespace MugenMvvmToolkit.Infrastructure
{
    /// <summary>
    ///     DataTemplateSelector allows the app writer to provide custom template selection logic.
    /// </summary>
    public abstract class DataTemplateSelectorBase<TSource, TTemplate> : IDataTemplateSelector where TTemplate : class
    {
        #region Implementation of IDataTemplateSelector

        /// <summary>
        ///     Override this method to return an app specific template.
        /// </summary>
        /// <param name="item">The data content</param>
        /// <param name="container">The element to which the template will be applied</param>
        /// <returns>
        ///     An app-specific template to apply, or null.
        /// </returns>
        public object SelectTemplate(object item, object container)
        {
            TTemplate template = SelectTemplate((TSource)item, container);
            if (SupportInitialize && template != null)
            {
                var bindingSet = new BindingSet<TTemplate, TSource>(template);
                Initialize(template, bindingSet);
                bindingSet.Apply();
            }
            return template;
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Specifies that this template supports initialize method default is <c>true</c>.
        /// </summary>
        protected virtual bool SupportInitialize
        {
            get { return true; }
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Override this method to return an app specific template.
        /// </summary>
        /// <param name="item">The data content</param>
        /// <param name="container">The element to which the template will be applied</param>
        /// <returns>
        ///     An app-specific template to apply, or null.
        /// </returns>
        protected abstract TTemplate SelectTemplate(TSource item, object container);

        /// <summary>
        ///     Initializes the specified template.
        /// </summary>
        protected abstract void Initialize(TTemplate template, BindingSet<TTemplate, TSource> bindingSet);

        #endregion
    }
}