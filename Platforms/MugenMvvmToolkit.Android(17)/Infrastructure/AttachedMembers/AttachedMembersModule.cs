#region Copyright
// ****************************************************************************
// <copyright file="AttachedMembersModule.cs">
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
using System;
using System.Collections;
using System.Collections.Generic;
using Android.App;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Support.V7.View;
using Android.Views;
using Android.Widget;
using Java.Lang;
using JetBrains.Annotations;
using MugenMvvmToolkit.Binding;
using MugenMvvmToolkit.Binding.Core;
using MugenMvvmToolkit.Binding.Interfaces;
using MugenMvvmToolkit.Binding.Interfaces.Models;
using MugenMvvmToolkit.Binding.Models;
using MugenMvvmToolkit.Binding.Models.EventArg;
using MugenMvvmToolkit.Interfaces;
using MugenMvvmToolkit.Interfaces.Models;
using MugenMvvmToolkit.Models;
using Object = Java.Lang.Object;

// ReSharper disable once CheckNamespace
namespace MugenMvvmToolkit.Infrastructure
{
    [Preserve(AllMembers = true)]
    public partial class AttachedMembersModule : IModule
    {
        #region Nested types

        private sealed class DateChangedListener : Object, DatePicker.IOnDateChangedListener
        {
            #region Implementation of IOnDateChangedListener

            public void OnDateChanged(DatePicker view, int year, int monthOfYear, int dayOfMonth)
            {
                EventHandler dateChanged = DateChanged;
                if (dateChanged != null)
                    dateChanged(view, EventArgs.Empty);
            }

            #endregion

            #region Events

            public event EventHandler DateChanged;

            #endregion
        }

        private sealed class ContentChangeListener : Object, ViewGroup.IOnHierarchyChangeListener
        {
            #region Fields

            public readonly static ContentChangeListener Instance = new ContentChangeListener();

            #endregion

            #region Implementation of IOnHierarchyChangeListener

            public void OnChildViewAdded(View parent, View child)
            {
                var viewGroup = (ViewGroup)parent;
                if (viewGroup.IndexOfChild(child) == 0)
                {
                    var dataContext = BindingProvider.Instance.ContextManager.GetBindingContext(child);
                    dataContext.DataContextChanged += BindingContextChanged;
                    UpdataContext(viewGroup, child, dataContext);
                }
                GlobalViewParentListener.Instance.OnChildViewAdded(parent, child);
            }

            public void OnChildViewRemoved(View parent, View child)
            {
                var viewGroup = (ViewGroup)parent;
                if (viewGroup.ChildCount == 0)
                {
                    BindingProvider.Instance.ContextManager.GetBindingContext(child).DataContextChanged -= BindingContextChanged;
                    ContentMember.SetValue(viewGroup, RemoveViewValue);
                }
                GlobalViewParentListener.Instance.OnChildViewRemoved(parent, child);
            }

            #endregion

            #region Methods

            private static void BindingContextChanged(IBindingContext context, EventArgs args)
            {
                UpdataContext(null, context.Source as View, context);
            }

            private static void UpdataContext(View parent, View view, IBindingContext context)
            {
                if (view == null)
                    return;
                if (parent == null)
                    parent = view.Parent as View;
                if (parent != null &&
                    !Equals(BindingProvider.Instance.ContextManager.GetBindingContext(parent).DataContext,
                        context.DataContext))
                    ContentMember.SetValue(parent, new[] { context.DataContext, AddViewValue });
            }

            #endregion
        }

        #endregion

        #region Fields

        private static readonly IAttachedBindingMemberInfo<DatePicker, DatePicker.IOnDateChangedListener> DateChangedListenerMember;
        private static readonly IAttachedBindingMemberInfo<ViewGroup, ViewGroupItemsSourceGenerator> ViewGroupItemsSourceGeneratorMember;

        private static readonly IAttachedBindingMemberInfo<TabHost, TabHostItemsSourceGenerator> TabHostItemsSourceGeneratorMember;
        private static readonly IAttachedBindingMemberInfo<TabHost, object> TabHostSelectedItemMember;

        internal static readonly IAttachedBindingMemberInfo<AdapterView, int> AdapterViewSelectedPositionMember;
        private static readonly IAttachedBindingMemberInfo<AdapterView, object> AdapterViewSelectedItemMember;

#if !API8
        internal static readonly IAttachedBindingMemberInfo<ViewGroup, bool> AddToBackStackMember;
#endif
        private static readonly IAttachedBindingMemberInfo<ViewGroup, object> ContentMember;
        private static readonly IAttachedBindingMemberInfo<ViewGroup, int?> ContentTemplateIdMember;
        private static readonly IAttachedBindingMemberInfo<ViewGroup, IDataTemplateSelector> ContentTemplateSelectorMember;

        private static IBindingMemberInfo _rawAdapterMember;
        private static readonly object AddViewValue = new object();
        private readonly static object[] RemoveViewValue = { null };

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="AttachedMembersModule" /> class.
        /// </summary>
        static AttachedMembersModule()
        {
            DateChangedListenerMember = AttachedBindingMember
                .CreateAutoProperty<DatePicker, DatePicker.IOnDateChangedListener>("#DateChangedListener", null, null, DateChangedListenerDefaultValue);
            ViewGroupItemsSourceGeneratorMember = AttachedBindingMember
                .CreateAutoProperty<ViewGroup, ViewGroupItemsSourceGenerator>(
                    "#ItemsSourceGeneratorMember", null, null, (@group, info) => new ViewGroupItemsSourceGenerator(@group));
            //Menu
            MenuItemClickListenerMember = AttachedBindingMember
                .CreateAutoProperty<IMenuItem, IMenuItemOnMenuItemClickListener>("#ClickListener", null, null, MenuItemClickListenerAttached);
            MenuParentMember = AttachedBindingMember
                .CreateAutoProperty<object, object>(AttachedMemberConstants.Parent);
            MenuItemsSourceGeneratorMember = AttachedBindingMember
                .CreateAutoProperty<IMenu, MenuItemsSourceGenerator>("#MenuItemsSourceGeneratorMember");
            MenuItemsSourceMember = AttachedBindingMember
                .CreateAutoProperty<IMenu, IEnumerable>(AttachedMemberConstants.ItemsSource, MenuItemsSourceChanged);

#if !API8
            MenuItemActionViewMember = AttachedBindingMember
                .CreateNotifiableMember<IMenuItem, object>("ActionView", (info, item, arg3) => item.GetActionView(), (info, item, arg3) => MenuItemUpdateActionView(item, arg3[0]));
            MenuItemActionViewSelectorMember = AttachedBindingMember
                .CreateAutoProperty<IMenuItem, IDataTemplateSelector>("ActionViewTemplateSelector", (o, args) => RefreshValue(o, MenuItemActionViewMember));
            MenuItemActionViewBindMember = AttachedBindingMember.CreateAutoProperty<IMenuItem, string>("#ActionViewBind");

            MenuItemActionProviderMember = AttachedBindingMember
                .CreateNotifiableMember<IMenuItem, object>("ActionProvider", (info, item, arg3) => item.GetActionProvider(), (info, item, arg3) => MenuItemUpdateActionProvider(item, arg3[0]));
            MenuItemActionProviderSelectorMember = AttachedBindingMember
                .CreateAutoProperty<IMenuItem, IDataTemplateSelector>("ActionProviderTemplateSelector", (o, args) => RefreshValue(o, MenuItemActionProviderMember));
            MenuItemActionProviderBindMember = AttachedBindingMember.CreateAutoProperty<IMenuItem, string>("#ActionProviderBind");

            AddToBackStackMember = AttachedBindingMember.CreateAutoProperty<ViewGroup, bool>("AddToBackStack");
#endif
            //View
            ViewAttachedParentMember = AttachedBindingMember.CreateAutoProperty<View, object>("#Parent", ViewAttachedParentChanged);
            ViewParentListenerMember = AttachedBindingMember.CreateAutoProperty<View, ParentListener>(
                "#ParentListener", defaultValue: (view, info) => new ParentListener(view));
            DisableValidationMember = AttachedBindingMember.CreateAutoProperty<View, bool>("DisableValidation");

            //ViewGroup
            ContentMember = AttachedBindingMember
                .CreateAutoProperty<ViewGroup, object>(AttachedMemberConstants.Content, ContentMemberChanged, ContentMemberAttached);
            ContentTemplateIdMember = AttachedBindingMember
                .CreateAutoProperty<ViewGroup, int?>(AttachedMemberConstants.ContentTemplate, ContentTemplateIdChanged);
            ContentTemplateSelectorMember = AttachedBindingMember
                .CreateAutoProperty<ViewGroup, IDataTemplateSelector>(AttachedMemberConstants.ContentTemplateSelector, ContentTemplateSelectorChanged);

            //AdapterView
            AdapterViewSelectedPositionMember =
                AttachedBindingMember.CreateAutoProperty<AdapterView, int>("SelectedItemPosition",
                    AdapterViewSelectedItemPositionChanged, AdapterViewSelectedMemberAttached);
            AdapterViewSelectedItemMember = AttachedBindingMember.CreateAutoProperty<AdapterView, object>(
                AttachedMemberConstants.SelectedItem, AdapterViewSelectedItemChanged);

            //TabHost
            TabHostItemsSourceGeneratorMember =
                AttachedBindingMember.CreateAutoProperty<TabHost, TabHostItemsSourceGenerator>("#TabHostItemsSourceGeneratorMember");
            TabHostSelectedItemMember = AttachedBindingMember.CreateAutoProperty<TabHost, object>(AttachedMemberConstants.SelectedItem, TabHostSelectedItemChanged); ;

#if !API8
            //Action bar
            ActionBarTabContentIdMember = AttachedBindingMember
                .CreateAutoProperty<ActionBar, int?>("TabContentId");
            ActionBarItemsSourceAdatapterMember = AttachedBindingMember
                .CreateAutoProperty<ActionBar, ItemsSourceAdapter>("#ItemsSourceAdatapter");
            ActionBarTabItemsSourceGeneratorMember = AttachedBindingMember
                .CreateAutoProperty<ActionBar, ActionBarTabItemsSourceGenerator>("#ActionBarTabItemsSourceGenerator");
            ActionBarItemsSourceMember = AttachedBindingMember.CreateAutoProperty<ActionBar, IEnumerable>(
                AttachedMemberConstants.ItemsSource, (bar, args) => ActionBarUpdateItemsSource(bar));
            ActionBarSelectedItemMember = AttachedBindingMember.CreateAutoProperty<ActionBar, object>(AttachedMemberConstants.SelectedItem, ActionBarSelectedItemChanged);
            //Context action bar
            ActionBarContextActionBarTemplateMember = AttachedBindingMember.CreateAutoProperty<ActionBar, int?>("ContextActionBarTemplate");
            ActionBarActionModeTemplateMember = AttachedBindingMember.CreateAutoProperty<ActionBar, ActionMode>("#BindableActionMode");
            ActionBarContextActionBarVisibleMember = AttachedBindingMember.CreateAutoProperty<ActionBar, bool>("ContextActionBarVisible", ActionBarContextActionBarVisibleChanged);

            //ActioBar.Tab
            ActionBarTabParentMember = AttachedBindingMember.CreateAutoProperty<ActionBar.Tab, ActionBar>(AttachedMemberConstants.Parent);
            ActionBarTabContentMember = AttachedBindingMember.CreateAutoProperty<ActionBar.Tab, object>(AttachedMemberConstants.Content);
            ActionBarTabContentInternalMember = AttachedBindingMember.CreateAutoProperty<ActionBar.Tab, object>("ContentInternal");
#endif

        }

        #endregion

        #region Methods

        private static void Register([NotNull] IBindingMemberProvider memberProvider)
        {
            Should.NotBeNull(memberProvider, "memberProvider");
            RegisterMenuMembers(memberProvider);
            RegisterViewMembers(memberProvider);
#if !API8
            RegisterActionBarMembers(memberProvider);
            //Dialog
            memberProvider.Register(AttachedBindingMember.CreateAutoProperty<Dialog, object>("Title",
                (dialog, args) => dialog.SetTitle(args.NewValue.ToStringSafe())));
#endif
            //Activity
            memberProvider.Register(AttachedBindingMember.CreateAutoProperty<Activity, string>("Title",
                (activity, args) => activity.Title = args.NewValue, defaultValue: (activity, info) => activity.Title));

            //CompoundButton
            memberProvider.Register(AttachedBindingMember
                .CreateMember<CompoundButton, bool>("Checked", (info, btn, arg3) => btn.Checked,
                    (info, btn, arg3) => btn.Checked = (bool)arg3[0], null, "CheckedChange"));

            //RatingBar
            memberProvider.Register(AttachedBindingMember
                .CreateMember<RatingBar, float>("Rating", (info, btn, arg3) => btn.Rating,
                    (info, btn, arg3) => btn.Rating = (float)arg3[0], null, "RatingBarChange"));

            //AdapterView
            _rawAdapterMember = memberProvider.GetBindingMember(typeof(AdapterView), "RawAdapter", false, true);
            memberProvider.Register(AttachedBindingMember
                .CreateAutoProperty<AdapterView, int?>(AttachedMemberNames.DropDownItemTemplate,
                    ViewGroupTemplateChanged));
            memberProvider.Register(AttachedBindingMember
                .CreateAutoProperty<AdapterView, int?>(AttachedMemberNames.DropDownItemTemplateSelector,
                    ViewGroupTemplateChanged));
            memberProvider.Register(AdapterViewSelectedItemMember);
            memberProvider.Register(AdapterViewSelectedPositionMember);

            //ViewGroup
            memberProvider.Register(AttachedBindingMember
                .CreateAutoProperty<ViewGroup, IEnumerable>(AttachedMemberConstants.ItemsSource, ViewGroupItemsSourceChanged));
            memberProvider.Register(AttachedBindingMember
                .CreateAutoProperty<ViewGroup, int?>(AttachedMemberConstants.ItemTemplate, ViewGroupTemplateChanged));
            memberProvider.Register(AttachedBindingMember
                .CreateAutoProperty<ViewGroup, IDataTemplateSelector>(AttachedMemberConstants.ItemTemplateSelector, ViewGroupTemplateChanged));

            memberProvider.Register(ViewGroupItemsSourceGeneratorMember);
            memberProvider.Register(ContentMember);
            memberProvider.Register(ContentTemplateIdMember);
            memberProvider.Register(ContentTemplateSelectorMember);
#if !API8
            memberProvider.Register(AddToBackStackMember);
#endif

            //TabHost
            memberProvider.Register(TabHostItemsSourceGeneratorMember);
            memberProvider.Register(TabHostSelectedItemMember);
            memberProvider.Register(AttachedBindingMember
                .CreateAutoProperty<TabHost, IEnumerable>(AttachedMemberConstants.ItemsSource, TabHostItemsSourceChanged));
            memberProvider.Register(AttachedBindingMember
                .CreateAutoProperty<TabHost, int?>(AttachedMemberConstants.ItemTemplate, TabHostTemplateChanged));
            memberProvider.Register(AttachedBindingMember
                .CreateAutoProperty<TabHost, IDataTemplateSelector>(AttachedMemberConstants.ItemTemplateSelector,
                    TabHostTemplateChanged));
            memberProvider.Register(AttachedBindingMember
                .CreateAutoProperty<TabHost, int?>(AttachedMemberConstants.ContentTemplate, TabHostTemplateChanged));
            memberProvider.Register(AttachedBindingMember
                .CreateAutoProperty<TabHost, IDataTemplateSelector>(AttachedMemberConstants.ContentTemplateSelector,
                    TabHostTemplateChanged));
            memberProvider.Register(AttachedBindingMember.CreateAutoProperty<TabHost.TabSpec, string>("Title",
                (spec, args) => spec.SetIndicator(args.NewValue)));


            //DatePicker
            memberProvider.Register(DateChangedListenerMember);
            memberProvider.Register(AttachedBindingMember
                .CreateMember<DatePicker, DateTime>("SelectedDate",
                    (info, picker, arg3) => picker.DateTime,
                    (info, picker, arg3) => picker.DateTime = (DateTime)arg3[0], ObserveSelectedDate));

            memberProvider.Register(AttachedBindingMember
                .CreateMember<DatePicker, DateTime>("DateTime",
                    (info, picker, arg3) => picker.DateTime,
                    (info, picker, arg3) => picker.DateTime = (DateTime)arg3[0], ObserveSelectedDate));

            //TimePicker
            memberProvider.Register(AttachedBindingMember
                .CreateMember<TimePicker, TimeSpan>("Value", GetTimePickerValue, SetTimePickerValue,
                    ObserveTimePickerValue));
            memberProvider.Register(AttachedBindingMember
                .CreateMember<TimePicker, TimeSpan>("SelectedTime", GetTimePickerValue, SetTimePickerValue,
                    ObserveTimePickerValue));

            //ImageView
            memberProvider.Register(AttachedBindingMember.CreateAutoProperty<ImageView, object>("ImageSource",
                (view, args) =>
                {
                    if (args.NewValue == null)
                    {
                        view.SetImageBitmap(null);
                        return;
                    }
                    var bitmap = args.NewValue as Bitmap;
                    if (bitmap != null)
                    {
                        view.SetImageBitmap(bitmap);
                        return;
                    }
                    var drawable = args.NewValue as Drawable;
                    if (drawable != null)
                    {
                        view.SetImageDrawable(drawable);
                        return;
                    }
                    var uri = args.NewValue as Android.Net.Uri;
                    if (uri != null)
                    {
                        view.SetImageURI(uri);
                        return;
                    }
                    view.SetImageResource((int)args.NewValue);
                }));
        }

#if !API8
        private static void RefreshValue(object target, IBindingMemberInfo member)
        {
            member.SetValue(target, new[] { member.GetValue(target, null) });
        }
#endif
        private static object GetAdapter(AdapterView item)
        {
            return _rawAdapterMember.GetValue(item, null);
        }

        private static void SetAdapter(AdapterView item, IAdapter adapter)
        {
            _rawAdapterMember.SetValue(item, new object[] { adapter });
        }

        #region TabHost

        private static void TabHostSelectedItemChanged(TabHost tabHost, AttachedMemberChangedEventArgs<object> arg)
        {
            var generator = TabHostItemsSourceGeneratorMember.GetValue(tabHost, null);
            if (generator != null)
                generator.SetSelectedItem(arg.NewValue);
        }

        private static void TabHostTemplateChanged<T>(TabHost tabHost, AttachedMemberChangedEventArgs<T> args)
        {
            var generator = TabHostItemsSourceGeneratorMember.GetValue(tabHost, null);
            if (generator != null)
                generator.Reset();
        }

        private static void TabHostItemsSourceChanged(TabHost tabHost, AttachedMemberChangedEventArgs<IEnumerable> arg)
        {
            var generator = TabHostItemsSourceGeneratorMember.GetValue(tabHost, null);
            if (generator == null)
            {
                generator = new TabHostItemsSourceGenerator(tabHost);
                TabHostItemsSourceGeneratorMember.SetValue(tabHost, new object[] { generator });
            }
            generator.Update(arg.NewValue);
        }

        #endregion

        #region AdapterView

        private static void AdapterViewSelectedItemPositionChanged(AdapterView sender,
            AttachedMemberChangedEventArgs<int> args)
        {
            sender.SetSelection(args.NewValue);

            var adapter = GetAdapter(sender) as ItemsSourceAdapter;
            if (adapter == null)
                return;
            object item = adapter.GetRawItem(args.NewValue);
            AdapterViewSelectedItemMember.SetValue(sender, new[] { item });
        }

        private static void AdapterViewSelectedItemChanged(AdapterView sender,
            AttachedMemberChangedEventArgs<object> args)
        {
            var adapter = GetAdapter(sender) as ItemsSourceAdapter;
            if (adapter == null)
                return;
            int position = adapter.GetPosition(args.NewValue);
            AdapterViewSelectedPositionMember.SetValue(sender, new object[] { position });
        }

        private static void AdapterViewSelectedMemberAttached(AdapterView adapterView,
            MemberAttachedEventArgs memberAttached)
        {
            if (adapterView is ListView)
                adapterView.ItemClick += (sender, args) => AdapterViewSelectedPositionMember.SetValue(sender, new object[] { args.Position });
            else
            {
                adapterView.ItemSelected += (sender, args) => AdapterViewSelectedPositionMember.SetValue(sender, new object[] { args.Position });
                adapterView.NothingSelected += (sender, args) => AdapterViewSelectedPositionMember.SetValue(sender, new object[] { -1 });
            }
            AdapterViewSelectedPositionMember.SetValue(adapterView, new object[] { adapterView.SelectedItemPosition });
        }

        #endregion

        #region DatePicker

        private static IDisposable ObserveSelectedDate(IBindingMemberInfo bindingMemberInfo, DatePicker datePicker,
            IEventListener arg3)
        {
            var value = (DateChangedListener)DateChangedListenerMember.GetValue(datePicker, null);
            var handler = arg3.ToWeakEventHandler<EventArgs>(false);
            value.DateChanged += handler.Handle;
            handler.Unsubscriber = WeakActionToken.Create(value, handler, (listener, eventHandler) => listener.DateChanged -= eventHandler.Handle, false);
            return handler;
        }

        private static DatePicker.IOnDateChangedListener DateChangedListenerDefaultValue(DatePicker sender, IBindingMemberInfo bindingMemberInfo)
        {
            var listener = new DateChangedListener();
            sender.Init(sender.Year, sender.Month, sender.DayOfMonth, listener);
            return listener;
        }

        #endregion

        #region TimePicker

        private static object SetTimePickerValue(IBindingMemberInfo bindingMemberInfo, TimePicker timePicker,
            IList<object> arg3)
        {
            var value = (TimeSpan)arg3[0];
            timePicker.CurrentHour = new Integer(value.Hours);
            timePicker.CurrentMinute = new Integer(value.Minutes);
            return null;
        }

        private static TimeSpan GetTimePickerValue(IBindingMemberInfo bindingMemberInfo, TimePicker timePicker,
            IList<object> arg3)
        {
            int currentHour = timePicker.CurrentHour.IntValue();
            int currentMinute = timePicker.CurrentMinute.IntValue();
            return new TimeSpan(currentHour, currentMinute, 0);
        }

        private static IDisposable ObserveTimePickerValue(IBindingMemberInfo bindingMemberInfo, TimePicker timePicker,
            IEventListener arg3)
        {
            var eventHandler = arg3.ToWeakEventHandler<TimePicker.TimeChangedEventArgs>();
            timePicker.TimeChanged += eventHandler.Handle;
            eventHandler.Unsubscriber = WeakActionToken.Create(timePicker, eventHandler,
                (picker, handler) => picker.TimeChanged -= handler.Handle, true);
            return eventHandler;
        }

        #endregion

        #region ViewGroup

        private static void ViewGroupTemplateChanged<T>(object sender, AttachedMemberChangedEventArgs<T> args)
        {
            var container = sender as AdapterView;
            if (container == null)
            {
                var sourceGenerator = (ViewGroupItemsSourceGenerator)ViewGroupItemsSourceGeneratorMember.GetValue(sender, null);
                if (sourceGenerator != null)
                    sourceGenerator.Reset();
                return;
            }
            var adapter = GetAdapter(container) as BaseAdapter;
            if (adapter != null)
                adapter.NotifyDataSetChanged();
        }

        private static void ViewGroupItemsSourceChanged(ViewGroup sender, AttachedMemberChangedEventArgs<IEnumerable> args)
        {
            var container = sender as AdapterView;
            if (container == null)
            {
                var sourceGenerator = ViewGroupItemsSourceGeneratorMember.GetValue(sender, null);
                if (sourceGenerator != null)
                    sourceGenerator.Update(args.NewValue);
                return;
            }
            var adapter = GetAdapter(container) as ItemsSourceAdapter;
            if (adapter == null)
            {
                adapter = new ItemsSourceAdapter(container, container.Context, true);
                SetAdapter(container, adapter);
            }
            adapter.ItemsSource = args.NewValue;
        }

        private static void ContentMemberAttached(ViewGroup viewGroup, MemberAttachedEventArgs args)
        {
            viewGroup.ListenParentChange();
            viewGroup.SetOnHierarchyChangeListener(ContentChangeListener.Instance);
        }

        private static void ContentTemplateSelectorChanged(ViewGroup sender,
            AttachedMemberChangedEventArgs<IDataTemplateSelector> args)
        {
            UpdateContent(sender, ContentMember.GetValue(sender, null), args.Args);
        }

        private static void ContentMemberChanged(ViewGroup sender, AttachedMemberChangedEventArgs<object> args)
        {
            UpdateContent(sender, args.NewValue, args.Args);
        }

        private static void ContentTemplateIdChanged(ViewGroup sender, AttachedMemberChangedEventArgs<int?> args)
        {
            UpdateContent(sender, ContentMember.GetValue(sender, null), args.Args);
        }

        private static void UpdateContent(ViewGroup sender, object newContent, object[] args)
        {
            if (newContent == null)
            {
                if (args != RemoveViewValue)
                    sender.RemoveAllViews();
                return;
            }
            //NOTE cheking if it's a view group listener.
            if (args != null && args.Length == 2 && args[1] == AddViewValue)
                return;
            var templateId = ContentTemplateIdMember.GetValue(sender, null);
            var templateSelector = ContentTemplateSelectorMember.GetValue(sender, null);
            sender.SetContentView(newContent, templateId, templateSelector);
        }

        #endregion

        #endregion

        #region Implementation of IModule

        /// <summary>
        ///     Gets the priority.
        /// </summary>
        public int Priority
        {
            get { return int.MinValue; }
        }

        /// <summary>
        ///     Loads the current module.
        /// </summary>
        public bool Load(IModuleContext context)
        {
            Register(BindingProvider.Instance.MemberProvider);
            return true;
        }

        /// <summary>
        ///     Unloads the current module.
        /// </summary>
        void IModule.Unload(IModuleContext context)
        {
        }

        #endregion
    }
}