﻿#region Copyright
// ****************************************************************************
// <copyright file="DataBindingExtension.cs">
// Copyright © Vyacheslav Volkov 2012-2014
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
using System;
using System.Collections;
using MugenMvvmToolkit.Binding;
using MugenMvvmToolkit.Binding.Core;
using MugenMvvmToolkit.Binding.Interfaces.Models;
using MugenMvvmToolkit.Models.Exceptions;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace MugenMvvmToolkit.MarkupExtensions
{
    /// <summary>
    ///     Provides high-level access to the definition of a binding, which connects the properties of binding target objects.
    /// </summary>
    [ContentProperty("Path")]
    public partial class DataBindingExtension : IMarkupExtension
    {
        #region Fields

        private static IBindingMemberInfo _getXamlNodeProvider;
        private static IBindingMemberInfo _getXamlNode;
        private static IBindingMemberInfo _getParentNode;
        private static IBindingMemberInfo _getProperties;
        private static IBindingMemberInfo _getLocalName;

        #endregion

        #region Implementation of IMarkupExtension

        public object ProvideValue(IServiceProvider serviceProvider)
        {
            var target = serviceProvider.GetService<IProvideValueTarget>();
            if (target == null)
                return GetEmptyValue();

            var targetObject = target.TargetObject;
            string path;
            var targetProperty = GetTargetProperty(targetObject, serviceProvider, out path);
            if (targetObject == null || (targetProperty == null && path == null))
            {
                Tracer.Error("DataBindingExtension cannot obtain target property on '{0}' object", targetObject);
                return GetEmptyValue();
            }

            var binding = HasValue
                ? CreateBindingBuilder(targetObject, path).Build()
                : CreateBinding(targetObject, path);

            if (ServiceProvider.DesignTimeManager.IsDesignMode && binding is InvalidDataBinding)
                throw new DesignTimeException(((InvalidDataBinding)binding).Exception);
            return GetDefaultValue(targetObject, targetProperty == null ? null : targetProperty.Member, binding, path);
        }

        #endregion

        #region Methods

        protected virtual IBindingMemberInfo GetTargetProperty(object targetObject, IServiceProvider serviceProvider, out string path)
        {
            //NOTE Xamarin doesn't support this property.
            //            return serviceProvider.GetService<IProvideValueTarget>().TargetProperty;

            //Making some reflection magic.
            path = null;
            if (targetObject == null)
                return null;

            UpdateMember(ref _getXamlNodeProvider, serviceProvider, "IXamlNodeProvider");
            var xamlNodeProvider = _getXamlNodeProvider.GetValue(serviceProvider, null);
            if (xamlNodeProvider == null)
                return null;

            UpdateMember(ref _getXamlNode, xamlNodeProvider, "XamlNode");
            var xamlNode = _getXamlNode.GetValue(xamlNodeProvider, null);
            if (xamlNode == null)
                return null;

            UpdateMember(ref _getParentNode, xamlNode, "Parent");
            var parentNode = _getParentNode.GetValue(xamlNode, null);
            if (parentNode == null)
                return null;

            UpdateMember(ref _getProperties, parentNode, "Properties");
            var properties = (IDictionary)_getProperties.GetValue(parentNode, null);
            if (properties == null)
                return null;

            object xmlName = null;
            foreach (DictionaryEntry entry in properties)
            {
                if (ReferenceEquals(entry.Value, xamlNode))
                {
                    xmlName = entry.Key;
                    break;
                }
            }
            if (xmlName == null)
                return null;

            UpdateMember(ref _getLocalName, xmlName, "LocalName");
            path = (string)_getLocalName.GetValue(xmlName, null);
            return BindingServiceProvider.MemberProvider.GetBindingMember(targetObject.GetType(), path, false, false);
        }

        private static void UpdateMember(ref IBindingMemberInfo member, object target, string path)
        {
            var type = target.GetType();
            if (member == null || member.Member == null || member.Member.DeclaringType != type)
                member = BindingServiceProvider.MemberProvider.GetBindingMember(type, path, true, true);
        }

        #endregion
    }
}