#region Copyright
// ****************************************************************************
// <copyright file="MvvmFragmentMediator.cs">
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
using System.ComponentModel;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V4.App;
using Android.Util;
using Android.Views;
using JetBrains.Annotations;
using MugenMvvmToolkit.Binding;
using MugenMvvmToolkit.Binding.Interfaces;
using MugenMvvmToolkit.Binding.Interfaces.Models;
using MugenMvvmToolkit.Binding.Models;
using MugenMvvmToolkit.DataConstants;
using MugenMvvmToolkit.Interfaces.Mediators;
using MugenMvvmToolkit.Interfaces.Models;
using MugenMvvmToolkit.Interfaces.ViewModels;
using MugenMvvmToolkit.Interfaces.Views;
using MugenMvvmToolkit.Models;
using MugenMvvmToolkit.Views;

namespace MugenMvvmToolkit.Infrastructure.Mediators
{
    public class MvvmFragmentMediator : MediatorBase<Fragment>, IMvvmFragmentMediator
    {
        #region Nested types

        private sealed class DialogInterfaceOnKeyListener : Java.Lang.Object, IDialogInterfaceOnKeyListener
        {
            #region Fields

            private readonly MvvmFragmentMediator _mediator;

            #endregion

            #region Constructors

            public DialogInterfaceOnKeyListener(MvvmFragmentMediator mediator)
            {
                _mediator = mediator;
            }

            #endregion

            #region Implementation of IDialogInterfaceOnKeyListener

            bool IDialogInterfaceOnKeyListener.OnKey(IDialogInterface dialog, Keycode keyCode, KeyEvent e)
            {
                if (keyCode != Keycode.Back || e.Action != KeyEventActions.Up)
                    return false;
                _mediator._dialogFragment.Dismiss();
                return true;
            }

            #endregion
        }

        #endregion

        #region Fields

        /// <summary>
        /// Gets the attached member for view.
        /// </summary>
        public static readonly IAttachedBindingMemberInfo<View, Fragment> FragmentViewMember;

        /// <summary>
        /// Gets the constant that returns current fragment.
        /// </summary>
        public static readonly DataConstant<Fragment> CurrentFragment;

        private readonly DialogFragment _dialogFragment;
        private List<IDataBinding> _bindings;
        private DialogInterfaceOnKeyListener _keyListener;
        private object _oldContext;

        #endregion

        #region Constructors

        static MvvmFragmentMediator()
        {
            FragmentViewMember = AttachedBindingMember.CreateAutoProperty<View, Fragment>("!$fragment");
            CurrentFragment = DataConstant.Create(() => CurrentFragment, true);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MediatorBase{TTarget}" /> class.
        /// </summary>
        public MvvmFragmentMediator([NotNull] Fragment target)
            : base(target)
        {
            _dialogFragment = target as DialogFragment;
        }

        #endregion

        #region Implementation of IMvvmFragmentMediator

        /// <summary>
        ///     Gets the <see cref="IMvvmFragmentMediator.Fragment" />.
        /// </summary>
        Fragment IMvvmFragmentMediator.Fragment
        {
            get { return Target; }
        }

        /// <summary>
        ///     Called when a fragment is first attached to its activity.
        /// </summary>
        public virtual void OnAttach(Activity activity, Action<Activity> baseOnAttach)
        {
            baseOnAttach(activity);
        }

        /// <summary>
        ///     Initialize the contents of the Activity's standard options menu.
        /// </summary>
        public virtual void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater,
            Action<IMenu, MenuInflater> baseOnCreateOptionsMenu)
        {
            if (Target.Activity == null || Target.View == null)
                baseOnCreateOptionsMenu(menu, inflater);
            else
            {
                var optionsMenu = Target.View.FindViewById<OptionsMenu>(Resource.Id.OptionsMenu);
                if (optionsMenu != null)
                    optionsMenu.Inflate(Target.Activity, menu);
            }
        }

        /// <summary>
        ///     Called to have the fragment instantiate its user interface view.
        /// </summary>
        public virtual View OnCreateView(int? viewId, LayoutInflater inflater, ViewGroup container,
            Bundle savedInstanceState, Func<LayoutInflater, ViewGroup, Bundle, View> baseOnCreateView)
        {
            ClearBindings();
            if (viewId.HasValue)
            {
                if (_bindings == null)
                    _bindings = new List<IDataBinding>();
                Tuple<View, IList<IDataBinding>> tuple = inflater.CreateBindableView(viewId.Value, container, false);
                FragmentViewMember.SetValue(tuple.Item1, Target);
                BindingServiceProvider.ContextManager.GetBindingContext(tuple.Item1).Value = DataContext;
                _bindings.AddRange(tuple.Item2);
                return tuple.Item1;
            }
            return baseOnCreateView(inflater, container, savedInstanceState);
        }

        /// <summary>
        ///     Called when the target is starting.
        /// </summary>
        public void OnCreate(Bundle savedInstanceState, Action<Bundle> baseOnCreate)
        {
            Tracer.Info("OnCreate fragment({0})", Target);
            //NOTE: checking if fragment is not recreated.
            if (_oldContext == null)
            {
                OnCreate(savedInstanceState);
            }
            else
            {
                RestoreContext(_oldContext);
                _oldContext = null;
            }
            baseOnCreate(savedInstanceState);

            var viewModel = BindingContext.Value as IViewModel;
            if (viewModel != null)
            {
                if (!viewModel.Settings.Metadata.Contains(ViewModelConstants.StateNotNeeded) && !viewModel.Settings.Metadata.Contains(ViewModelConstants.StateManager))
                    viewModel.Settings.Metadata.AddOrUpdate(ViewModelConstants.StateManager, this);
                viewModel.Settings.Metadata.AddOrUpdate(CurrentFragment, Target);
            }
        }

        /// <summary>
        ///     Called immediately after
        ///     <c>
        ///         <see
        ///             cref="M:Android.App.Fragment.OnCreateView(Android.Views.LayoutInflater, Android.Views.ViewGroup, Android.Views.ViewGroup)" />
        ///     </c>
        ///     has returned, but before any saved state has been restored in to the view.
        /// </summary>
        public virtual void OnViewCreated(View view, Bundle savedInstanceState, Action<View, Bundle> baseOnViewCreated)
        {
            if (_dialogFragment == null)
            {
                if (view != null)
                {
                    var actionBar = view.FindViewById<Views.ActionBar>(Resource.Id.ActionBarView);
                    if (actionBar != null)
                        actionBar.Apply(Target.Activity);
                }
            }
            else
            {
                var dialog = _dialogFragment.Dialog;
                if (dialog != null)
                {
                    TrySetTitleBinding();
                    if (_keyListener == null)
                        _keyListener = new DialogInterfaceOnKeyListener(this);
                    dialog.SetOnKeyListener(_keyListener);
                }
            }
            baseOnViewCreated(view, savedInstanceState);
        }

        /// <summary>
        ///     Called when the fragment is no longer in use.
        /// </summary>
        public override void OnDestroy(Action baseOnDestroy)
        {
            Tracer.Info("OnDestroy fragment({0})", Target);
            var handler = Destroyed;
            if (handler != null)
                handler((IWindowView)Target, EventArgs.Empty);

            _oldContext = BindingContext.Value;
            ClearBindings();
            var viewModel = _oldContext as IViewModel;
            if (viewModel != null)
            {
                viewModel.Settings.Metadata.Remove(CurrentFragment);
                object stateManager;
                if (viewModel.Settings.Metadata.TryGetData(ViewModelConstants.StateManager, out stateManager) &&
                    stateManager == this)
                    viewModel.Settings.Metadata.Remove(ViewModelConstants.StateManager);
            }
            base.OnDestroy(baseOnDestroy);
        }

        /// <summary>
        ///     Called when the fragment is no longer attached to its activity.
        /// </summary>
        public virtual void OnDetach(Action baseOnDetach)
        {
            baseOnDetach();
        }

        /// <summary>
        ///     Called when a fragment is being created as part of a view layout
        ///     inflation, typically from setting the content view of an activity.
        /// </summary>
        public virtual void OnInflate(Activity activity, IAttributeSet attrs, Bundle savedInstanceState,
            Action<Activity, IAttributeSet, Bundle> baseOnInflate)
        {
            ClearBindings();
            List<string> strings = ViewFactory.ReadStringAttributeValue(activity, attrs, Resource.Styleable.Binding, null);
            if (strings != null && strings.Count != 0)
            {
                _bindings = new List<IDataBinding>();
                foreach (string bind in strings)
                    _bindings.AddRange(BindingServiceProvider.BindingProvider.CreateBindingsFromString(Target, bind, null));
            }
            baseOnInflate(activity, attrs, savedInstanceState);
        }

        /// <summary>
        ///     Called when the Fragment is no longer resumed.
        /// </summary>
        public virtual void OnPause(Action baseOnPause)
        {
            baseOnPause();
        }

        /// <summary>
        ///     Called when the fragment is visible to the user and actively running.
        /// </summary>
        public virtual void OnResume(Action baseOnResume)
        {
            baseOnResume();
        }

        /// <summary>
        ///     Called when the Fragment is visible to the user.
        /// </summary>
        public virtual void OnStart(Action baseOnStart)
        {
            baseOnStart();
            var view = Target.View;
            if (view != null)
                view.ListenParentChange();
        }

        /// <summary>
        ///     Called when the Fragment is no longer started.
        /// </summary>
        public virtual void OnStop(Action baseOnStop)
        {
            baseOnStop();
        }

        /// <summary>
        ///     This method will be invoked when the dialog is canceled.
        /// </summary>
        public virtual void OnCancel(IDialogInterface dialog, Action<IDialogInterface> baseOnCancel)
        {
            var handler = Canceled;
            if (handler != null)
                handler((IWindowView)Target, EventArgs.Empty);
            baseOnCancel(dialog);
        }

        /// <summary>
        ///     Dismiss the fragment and its dialog.
        /// </summary>
        public virtual void Dismiss(Action baseDismiss)
        {
            if (OnClosing())
                baseDismiss();
        }

        /// <summary>
        ///     Occurred on closing window.
        /// </summary>
        public virtual event EventHandler<IWindowView, CancelEventArgs> Closing;

        /// <summary>
        ///     Occurred on closed window.
        /// </summary>
        public virtual event EventHandler<IWindowView, EventArgs> Canceled;

        /// <summary>
        ///     Occurred on destroyed view.
        /// </summary>
        public event EventHandler<IWindowView, EventArgs> Destroyed;

        #endregion

        #region Overrides of MediatorBase<Fragment>

        /// <summary>
        ///     Occurs when the DataContext property changed.
        /// </summary>
        protected override void OnDataContextChanged(object oldValue, object newValue)
        {
            base.OnDataContextChanged(oldValue, newValue);
            View view = Target.View;
            if (view != null)
                BindingServiceProvider.ContextManager
                    .GetBindingContext(view)
                    .Value = DataContext;
        }

        #endregion

        #region Methods

        private void ClearBindings()
        {
            if (_bindings == null)
                return;
            foreach (IDataBinding binding in _bindings)
                binding.Dispose();
            _bindings.Clear();
        }

        private bool OnClosing()
        {
            var closing = Closing;
            if (closing == null)
                return true;
            var args = new CancelEventArgs();
            closing((IWindowView)Target, args);
            return !args.Cancel;
        }

        private void TrySetTitleBinding()
        {
            var hasDisplayName = BindingContext.Value as IHasDisplayName;
            if (_dialogFragment == null || hasDisplayName == null)
                return;
            var dialog = _dialogFragment.Dialog;
            if (dialog == null)
                return;

            if (_bindings == null)
                _bindings = new List<IDataBinding>();
            var bindings = BindingServiceProvider
                .BindingProvider
                .CreateBindingsFromString(dialog, "Title DisplayName", new object[] { hasDisplayName });
            _bindings.AddRange(bindings);
        }

        #endregion
    }
}