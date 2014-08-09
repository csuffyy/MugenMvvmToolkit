#region Copyright
// ****************************************************************************
// <copyright file="AttachedMembersModuleView.cs">
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
using System.Collections.Generic;
using System.Linq;
using Android.Views;
using MugenMvvmToolkit.Binding;
using MugenMvvmToolkit.Binding.Core;
using MugenMvvmToolkit.Binding.Interfaces;
using MugenMvvmToolkit.Binding.Interfaces.Models;
using MugenMvvmToolkit.Binding.Models;
using MugenMvvmToolkit.Binding.Models.EventArg;
using MugenMvvmToolkit.Models;

// ReSharper disable once CheckNamespace
namespace MugenMvvmToolkit.Infrastructure
{
    public partial class AttachedMembersModule
    {
        #region Nested types

        private abstract class LayoutObserver : DisposableObject
        {
            #region Fields

            private WeakReference _viewReference;

            #endregion

            #region Constructors

            /// <summary>
            ///     Initializes a new instance of the <see cref="LayoutObserver" /> class.
            /// </summary>
            protected LayoutObserver(View view)
            {
                _viewReference = MvvmExtensions.GetWeakReference(view);
#if API17
                view.LayoutChange += OnGlobalLayoutChanged;
#else
                if (view.ViewTreeObserver.IsAlive)
                    view.ViewTreeObserver.GlobalLayout += OnGlobalLayoutChanged;
#endif
            }

            #endregion

            #region Methods

            protected View GetView()
            {
                var viewReference = _viewReference;
                if (viewReference == null)
                    return null;
                return (View)viewReference.Target;
            }

            private void OnGlobalLayoutChanged(object sender, EventArgs eventArgs)
            {
                if (IsDisposed)
                    return;
                var view = GetView();
                if (view == null)
                    Dispose();
                else
                    OnGlobalLayoutChangedInternal(view, sender, eventArgs);
            }

            protected abstract void OnGlobalLayoutChangedInternal(View view, object sender, EventArgs eventArgs);

            #endregion

            #region Overrides of DisposableObjectBase

            /// <summary>
            ///     Releases resources held by the object.
            /// </summary>
            protected override void OnDispose(bool disposing)
            {
                base.OnDispose(disposing);
                if (disposing)
                {
                    try
                    {
                        var view = (View)_viewReference.Target;
#if API17
                        view.LayoutChange -= OnGlobalLayoutChanged;
#else
                        if (view != null && view.ViewTreeObserver.IsAlive)
                            view.ViewTreeObserver.GlobalLayout -= OnGlobalLayoutChanged;
#endif
                    }
                    catch (Exception e)
                    {
                        Tracer.Warn(e.Flatten());
                    }
                    finally
                    {
                        _viewReference = null;
                    }
                }
            }

            #endregion
        }

        private sealed class VisiblityObserver : LayoutObserver
        {
            #region Fields

            private readonly WeakReference _listenerRef;
            private ViewStates _oldValue;

            #endregion

            #region Constructors

            /// <summary>
            ///     Initializes a new instance of the <see cref="VisiblityObserver" /> class.
            /// </summary>
            public VisiblityObserver(View view, IEventListener handler)
                : base(view)
            {
                _listenerRef = ServiceProvider.WeakReferenceFactory(handler, true);
                _oldValue = view.Visibility;
            }

            #endregion

            #region Overrides of LayoutObserver

            protected override void OnGlobalLayoutChangedInternal(View view, object sender, EventArgs eventArgs)
            {
                ViewStates visibility = view.Visibility;
                if (_oldValue == visibility)
                    return;
                _oldValue = visibility;
                var listener = (IEventListener)_listenerRef.Target;
                if (listener == null)
                    Dispose();
                else
                    listener.Handle(sender, eventArgs);
            }

            #endregion
        }

        private sealed class ParentListener
        {
            #region Fields

            private readonly View _view;
            private WeakReference _parentReference;

            #endregion

            #region Constructors

            public ParentListener(View view)
            {
                _view = view;
                _parentReference = MvvmExtensions.GetWeakReference(view.Id == Android.Resource.Id.Content ? null : view.Parent);
            }

            #endregion

            #region Events

            public event EventHandler Changed;

            public void Raise(object sender)
            {
                if (_view.Id == Android.Resource.Id.Content || ReferenceEquals(_view.Parent, _parentReference.Target))
                    return;
                _parentReference = MvvmExtensions.GetWeakReference(_view.Parent);
                var handler = Changed;
                if (handler != null)
                    handler(sender, EventArgs.Empty);
            }

            #endregion
        }

        #endregion

        #region Fields

        /// <summary>
        /// Gets the attached parent member for view.
        /// </summary>
        public static readonly IAttachedBindingMemberInfo<View, object> ViewAttachedParentMember;

        private static readonly IAttachedBindingMemberInfo<View, ParentListener> ViewParentListenerMember;
        private static readonly IAttachedBindingMemberInfo<View, bool> DisableValidationMember;

        #endregion

        #region Methods

        internal static void RaiseParentChanged(View view)
        {
            var parentListener = ViewParentListenerMember.GetValue(view, null);
            if (parentListener != null)
                parentListener.Raise(view);
        }

        private static void RegisterViewMembers(IBindingMemberProvider memberProvider)
        {
            //View
            memberProvider.Register(ViewAttachedParentMember);
            memberProvider.Register(ViewParentListenerMember);
            memberProvider.Register(DisableValidationMember);
            memberProvider.Register(AttachedBindingMember.CreateAutoProperty<View, int>(AttachedMemberNames.MenuTemplate));

#if !API8
            memberProvider.Register(AttachedBindingMember.CreateAutoProperty<View, int>(AttachedMemberNames.PopupMenuTemplate));
            memberProvider.Register(AttachedBindingMember.CreateAutoProperty<View, string>(AttachedMemberNames.PopupMenuEvent, PopupMenuEventChanged));
            memberProvider.Register(AttachedBindingMember.CreateAutoProperty<View, string>(AttachedMemberNames.PlacementTargetPath));
#endif

            memberProvider.Register(AttachedBindingMember.CreateMember<View, object>(AttachedMemberConstants.SetErrorsMethod, null, SetErrorValue));
            memberProvider.Register(AttachedBindingMember.CreateMember<View, object>(AttachedMemberConstants.Parent,
                    GetViewParentValue, null, ObserveViewParent));
            memberProvider.Register(AttachedBindingMember.CreateMember<View, object>(AttachedMemberConstants.FindByNameMethod,
                    ViewFindByNameMember, null));
            memberProvider.Register(AttachedBindingMember.CreateMember<View, bool>(AttachedMemberConstants.Focused,
                    (info, view, arg3) => view.IsFocused, null, memberChangeEventName: "FocusChange"));
            memberProvider.Register(AttachedBindingMember.CreateMember<View, bool>(AttachedMemberConstants.Enabled,
                    (info, view, arg3) => view.Enabled, (info, view, arg3) => view.Enabled = (bool)arg3[0]));
            memberProvider.Register(AttachedBindingMember.CreateMember<View, ViewStates>("Visibility",
                    (info, view, arg3) => view.Visibility, (info, view, arg3) => view.Visibility = (ViewStates)arg3[0],
                    ObserveViewVisibility));
            memberProvider.Register(AttachedBindingMember.CreateMember<View, bool>("Visible",
                    (info, view, arg3) => view.Visibility == ViewStates.Visible,
                    (info, view, arg3) => view.Visibility = ((bool)arg3[0]) ? ViewStates.Visible : ViewStates.Gone,
                    ObserveViewVisibility));
            memberProvider.Register(AttachedBindingMember.CreateMember<View, bool>("Hidden",
                    (info, view, arg3) => view.Visibility != ViewStates.Visible,
                    (info, view, arg3) => view.Visibility = ((bool)arg3[0]) ? ViewStates.Gone : ViewStates.Visible,
                    ObserveViewVisibility));
        }

        private static IDisposable ObserveViewVisibility(IBindingMemberInfo bindingMemberInfo, View view, IEventListener arg3)
        {
            return new VisiblityObserver(view, arg3);
        }

        private static IDisposable ObserveViewParent(IBindingMemberInfo bindingMemberInfo, View view, IEventListener arg3)
        {
            var parentListener = ViewParentListenerMember.GetValue(view, null);
            var handler = arg3.ToWeakEventHandler<EventArgs>();
            parentListener.Changed += handler.Handle;
            handler.Unsubscriber = WeakActionToken.Create(parentListener, handler, (listener, eventHandler) => listener.Changed -= eventHandler.Handle, false);
            return handler;
        }

        private static object GetViewParentValue(IBindingMemberInfo arg1, View arg2, object[] arg3)
        {
            var value = ViewAttachedParentMember.GetValue(arg2, arg3);
            if (value != null)
                return value;
            if (arg2.Id == Android.Resource.Id.Content)
                return arg2.Context;
            return arg2.Parent;
        }

        private static void ViewAttachedParentChanged(View arg1, AttachedMemberChangedEventArgs<object> arg2)
        {
            RaiseParentChanged(arg1);
        }

        private static object ViewFindByNameMember(IBindingMemberInfo bindingMemberInfo, View target, object[] arg3)
        {
            var tag = arg3[0].ToStringSafe();
            return target.RootView.FindViewWithTag(tag);
        }

        private static object SetErrorValue(IBindingMemberInfo bindingMemberInfo, View view, object[] arg3)
        {
            if (DisableValidationMember.GetValue(view, null))
                return null;
            IBindingMemberInfo errorMember = BindingProvider
                .Instance
                .MemberProvider
                .GetBindingMember(view.GetType(), "Error", false, false);
            if (errorMember == null)
                return null;
            var errors = (ICollection<object>)arg3[0];
            object[] error = errors == null || errors.Count == 0
                ? BindingExtensions.NullValue
                : new object[] { errors.FirstOrDefault().ToStringSafe() };
            errorMember.SetValue(view, error);
            return null;
        }

#if !API8
        private static void PopupMenuEventChanged(View view, AttachedMemberChangedEventArgs<string> args)
        {
            if (string.IsNullOrEmpty(args.NewValue))
                return;
            IBindingMemberInfo member = BindingProvider.Instance
                                                       .MemberProvider
                                                       .GetBindingMember(view.GetType(), args.NewValue, false, true);
            var presenter = ServiceProvider.AttachedValueProvider.GetOrAdd(view, "!@popup", (view1, o) => new PopupMenuPresenter(view1), null);
            var unsubscriber = member.SetValue(view, new object[] { presenter }) as IDisposable;
            presenter.Update(unsubscriber);
        }
#endif
        #endregion
    }
}