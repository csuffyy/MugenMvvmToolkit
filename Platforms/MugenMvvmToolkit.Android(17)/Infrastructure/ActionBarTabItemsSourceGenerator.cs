#region Copyright
// ****************************************************************************
// <copyright file="ActionBarTabItemsSourceGenerator.cs">
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

using System.Collections;
using Android.App;
using Android.Support.V7.App;
using Android.Widget;
using MugenMvvmToolkit.Binding;
using MugenMvvmToolkit.Binding.Interfaces;
using MugenMvvmToolkit.Interfaces.Models;
using MugenMvvmToolkit.Models;

namespace MugenMvvmToolkit.Infrastructure
{
    internal sealed class ActionBarTabItemsSourceGenerator : ItemsSourceGeneratorBase
    {
        #region Fields

        private readonly ActionBar _actionBar;
        private readonly ActionBarTabTemplate _tabTemplate;

        #endregion

        #region Constructors

        private ActionBarTabItemsSourceGenerator(ActionBar actionBar, ActionBarTabTemplate tabTemplate)
        {
            Should.NotBeNull(actionBar, "actionBar");
            Should.NotBeNull(tabTemplate, "tabTemplate");
            _actionBar = actionBar;
            _tabTemplate = tabTemplate;
            TryListenActivity(_actionBar.ThemedContext);
        }

        #endregion

        #region Overrides of ItemsSourceGeneratorBase

        protected override IEnumerable ItemsSource { get; set; }

        protected override void Add(int insertionIndex, int count)
        {
            for (int i = 0; i < count; i++)
            {
                int index = insertionIndex + i;
                var tab = CreateTab(index);
                _actionBar.AddTab(tab, index, false);
            }
        }

        protected override void Remove(int removalIndex, int count)
        {
            for (int i = 0; i < count; i++)
            {
                int index = removalIndex + i;
                Remove(index);
            }
            if (_actionBar.TabCount == 0)
                OnEmptyTab();
        }

        protected override void Replace(int startIndex, int count)
        {
            for (int i = 0; i < count; i++)
            {
                int index = startIndex + i;
                Remove(index);
                var tab = CreateTab(index);
                _actionBar.AddTab(tab, index, false);
            }
        }

        protected override void Refresh()
        {
            for (int i = 0; i < _actionBar.TabCount; i++)
                _tabTemplate.ClearTab(_actionBar, _actionBar.GetTabAt(i));
            _actionBar.RemoveAllTabs();

            IEnumerable itemsSource = ItemsSource;
            if (itemsSource == null)
                return;

            var selectedItem = PlatformDataBindingModule.ActionBarSelectedItemMember.GetValue(_actionBar, null);

            int count = itemsSource.Count();
            for (int index = 0; index < count; index++)
            {
                var item = GetItem(index);
                var tab = CreateTab(item);
                _actionBar.AddTab(tab, index, ReferenceEquals(selectedItem, item));
            }
            if (count == 0)
                OnEmptyTab();
        }

        #endregion

        #region Methods

        public static void Set(ActionBar actionBar, ActionBarTabTemplate tabTemplate)
        {
            ServiceProvider.AttachedValueProvider.SetValue(actionBar, Key, new ActionBarTabItemsSourceGenerator(actionBar, tabTemplate));
        }
        
        public void SetSelectedItem(object selectedItem, IDataContext context = null)
        {
            IBindingContextManager contextManager = BindingServiceProvider.ContextManager;
            for (int i = 0; i < _actionBar.TabCount; i++)
            {
                var tab = _actionBar.GetTabAt(i);
                if (contextManager.GetBindingContext(tab).Value == selectedItem)
                {
                    if (tab.Position != _actionBar.SelectedNavigationIndex)
                        tab.Select();
                    return;
                }
            }
        }

        private void OnEmptyTab()
        {
            PlatformDataBindingModule
                .ActionBarSelectedItemMember
                .SetValue(_actionBar, BindingExtensions.NullValue);
            var value = Views.ActionBar.GetTabContentId(_actionBar);
            if (value == null)
                return;
            var layout = ((Activity)_actionBar.ThemedContext).FindViewById<FrameLayout>(value.Value);
            if (layout != null)
                layout.RemoveAllViews();
        }

        private void Remove(int index)
        {
            var tabAt = _actionBar.GetTabAt(index);
            _actionBar.RemoveTabAt(index);
            if (tabAt != null)
                _tabTemplate.ClearTab(_actionBar, tabAt);
        }

        private ActionBar.Tab CreateTab(int index)
        {
            return CreateTab(GetItem(index));
        }

        private ActionBar.Tab CreateTab(object item)
        {
            return _tabTemplate.CreateTab(_actionBar, item);
        }

        #endregion
    }
}