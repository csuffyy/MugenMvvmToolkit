﻿#region Copyright
// ****************************************************************************
// <copyright file="ItemsSourceGenerator.cs">
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
using System.Collections;
using System.ComponentModel;
#if WINFORMS
using System.Windows.Forms;
#endif
using JetBrains.Annotations;
using MugenMvvmToolkit.Binding;
using MugenMvvmToolkit.Binding.Builders;
using MugenMvvmToolkit.Binding.Interfaces.Models;
using MugenMvvmToolkit.Converters;
using MugenMvvmToolkit.Interfaces;
using MugenMvvmToolkit.Interfaces.Models;
using MugenMvvmToolkit.Interfaces.ViewModels;

namespace MugenMvvmToolkit.Infrastructure
{
    internal class ItemsSourceGenerator : ItemsSourceGeneratorBase
    {
        #region Fields

        private readonly IBindingMemberInfo _itemTemplateMember;
        private readonly object _view;
#if WINFORMS
        private readonly bool _isTabControl;
#endif
        #endregion

        #region Constructors

        private ItemsSourceGenerator([NotNull] object view)
        {
            Should.NotBeNull(view, "view");
#if WINFORMS
            ListenDisposeEvent(view as IComponent);
            _isTabControl = view is TabControl;
#endif
            _view = view;
            _itemTemplateMember = BindingServiceProvider
                .MemberProvider
                .GetBindingMember(view.GetType(), AttachedMemberConstants.ItemTemplate, false, false);
        }

        #endregion

        #region Overrides of ItemsSourceGeneratorBase

        protected override IEnumerable ItemsSource { get; set; }

        protected override void Add(int insertionIndex, int count)
        {
            ICollectionViewManager viewManager = GetCollectionViewManager();
            for (int i = 0; i < count; i++)
            {
                int index = insertionIndex + i;
                viewManager.Insert(_view, index, GetItemFromTemplate(index));
            }
        }

        protected override void Remove(int removalIndex, int count)
        {
            ICollectionViewManager viewManager = GetCollectionViewManager();
            for (int i = 0; i < count; i++)
                viewManager.RemoveAt(_view, removalIndex + i);
        }

        protected override void Replace(int startIndex, int count)
        {
            ICollectionViewManager viewManager = GetCollectionViewManager();
            for (int i = 0; i < count; i++)
            {
                int index = startIndex + i;
                viewManager.RemoveAt(_view, index);
                viewManager.Insert(_view, index, GetItemFromTemplate(index));
            }
        }

        protected override void Refresh()
        {
            ICollectionViewManager viewManager = GetCollectionViewManager();
            viewManager.Clear(_view);
            int count = ItemsSource.Count();
            for (int i = 0; i < count; i++)
                viewManager.Insert(_view, i, GetItemFromTemplate(i));
        }

        #endregion

        #region Methods

        public static IItemsSourceGenerator GetOrAdd(object item)
        {
            return ServiceProvider
                .AttachedValueProvider
                .GetOrAdd(item, Key, (o, o1) => new ItemsSourceGenerator(o), null);
        }

        private ICollectionViewManager GetCollectionViewManager()
        {
            return PlatformDataBindingModule
                .CollectionViewManagerMember
                .GetValue(_view, null) ?? DefaultCollectionViewManager.Instance;
        }

        private object GetItemFromTemplate(int index)
        {
            object item = GetItem(index);
            if (_itemTemplateMember == null)
            {
#if WINFORMS
                if (_isTabControl)
                    return CreateDefaultTabPage(item);
#endif
                return GetDefaultTemplate(item);
            }
            var selector = (IDataTemplateSelector)_itemTemplateMember.GetValue(_view, null);
            if (selector == null)
            {
#if WINFORMS
                if (_isTabControl)
                    return CreateDefaultTabPage(item);
#endif
                return GetDefaultTemplate(item);
            }
            return selector.SelectTemplateWithContext(item, _view);
        }

        private static object GetDefaultTemplate(object item)
        {
            if (item is IViewModel)
                return ViewModelToViewConverter.Instance.Convert(item);
            return item;
        }

#if WINFORMS
        private static TabPage CreateDefaultTabPage(object item)
        {
            var viewModel = item as IViewModel;
            if (viewModel == null)
                return new TabPage(item == null ? "null" : item.ToString());
            var page = new TabPage();
            var set = new BindingSet<TabPage, IViewModel>(page);
            set.BindFromExpression("Content ;");
            if (viewModel is IHasDisplayName)
                set.BindFromExpression("Text DisplayName;");
            set.Apply();
            BindingServiceProvider.ContextManager.GetBindingContext(page).Value = item;
            return page;
        }
#endif
        #endregion
    }
}