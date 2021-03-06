﻿#region Copyright
// ****************************************************************************
// <copyright file="XamarinFormsBootstrapperBase.cs">
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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using MugenMvvmToolkit.Infrastructure.Navigation;
using MugenMvvmToolkit.Infrastructure.Presenters;
using MugenMvvmToolkit.Interfaces;
using MugenMvvmToolkit.Interfaces.Models;
using MugenMvvmToolkit.Interfaces.Navigation;
using MugenMvvmToolkit.Interfaces.Presenters;
using MugenMvvmToolkit.Interfaces.ViewModels;
using MugenMvvmToolkit.Interfaces.Views;
using MugenMvvmToolkit.Models;
using MugenMvvmToolkit.ViewModels;
using Xamarin.Forms;

namespace MugenMvvmToolkit.Infrastructure
{
    /// <summary>
    ///     Represents the base class that is used to start MVVM application.
    /// </summary>
    public abstract class XamarinFormsBootstrapperBase : BootstrapperBase
    {
        #region Nested types

        internal interface IPlatformService
        {
            PlatformInfo GetPlatformInfo();

            ICollection<Assembly> GetAssemblies();
        }

        #endregion

        #region Fields

        protected static readonly DataConstant<bool> WrapToNavigationPageConstant;
        private readonly PlatformInfo _platform;
        private readonly IPlatformService _platformService;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="XamarinFormsBootstrapperBase" /> class.
        /// </summary>
        static XamarinFormsBootstrapperBase()
        {
            WrapToNavigationPageConstant = DataConstant.Create(() => WrapToNavigationPageConstant);
            if (Device.OS != TargetPlatform.WinPhone)
                LinkerInclude.Initialize();
            DynamicMultiViewModelPresenter.CanShowViewModelDefault = CanShowViewModelTabPresenter;
            DynamicViewModelNavigationPresenter.CanShowViewModelDefault = CanShowViewModelNavigationPresenter;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="XamarinFormsBootstrapperBase" /> class.
        /// </summary>
        protected XamarinFormsBootstrapperBase()
        {
            var assembly = TryLoadAssembly(BindingAssemblyName, null);
            if (assembly != null)
            {
                var serviceType = typeof(IPlatformService).GetTypeInfo();
                serviceType = assembly.DefinedTypes.FirstOrDefault(serviceType.IsAssignableFrom);
                if (serviceType != null)
                    _platformService = (IPlatformService)Activator.CreateInstance(serviceType.AsType());
            }
            _platform = _platformService == null
                ? XamarinFormsExtensions.GetPlatformInfo()
                : _platformService.GetPlatformInfo();
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the name of binding assembly.
        /// </summary>
        protected static string BindingAssemblyName
        {
            get
            {
                return Device.OnPlatform("MugenMvvmToolkit.Xamarin.Forms.iOS", "MugenMvvmToolkit.Xamarin.Forms.Android",
                    "MugenMvvmToolkit.Xamarin.Forms.WinPhone");
            }
        }

        #endregion

        #region Overrides of BootstrapperBase

        /// <summary>
        ///     Gets the current platform.
        /// </summary>
        public override PlatformInfo Platform
        {
            get { return _platform; }
        }


        /// <summary>
        ///     Gets the application assemblies.
        /// </summary>
        protected override ICollection<Assembly> GetAssemblies()
        {
            if (_platformService == null)
                return base.GetAssemblies();
            return _platformService.GetAssemblies();
        }

        #endregion

        #region Methods

        public Page Start(bool wrapToNavigationPage, IDataContext context = null)
        {
            context = context.ToNonReadOnly();
            context.AddOrUpdate(WrapToNavigationPageConstant, wrapToNavigationPage);
            return Start(context);
        }

        public virtual Page Start(IDataContext context = null)
        {
            context = context.ToNonReadOnly();
            if (!context.Contains(WrapToNavigationPageConstant))
                context.Add(WrapToNavigationPageConstant, true);
            Initialize();
            context = context.ToNonReadOnly();
            var viewModelType = GetMainViewModelType();
            var viewModel = CreateMainViewModel(viewModelType, context);
            var view = (Page)ViewManager.GetOrCreateView(viewModel, null, context).GetUnderlyingView();
            var page = view as NavigationPage ?? CreateNavigationPage(view, context);
            if (page == null)
                return view;
            IocContainer.BindToConstant<INavigationService>(new NavigationService(page));
            return page;
        }

        /// <summary>
        ///     Creates the main view model.
        /// </summary>
        [NotNull]
        protected virtual IViewModel CreateMainViewModel([NotNull] Type viewModelType, [NotNull] IDataContext context)
        {
            return IocContainer
                .Get<IViewModelProvider>()
                .GetViewModel(viewModelType, context);
        }

        /// <summary>
        ///     Gets the type of main view model.
        /// </summary>
        [NotNull]
        protected abstract Type GetMainViewModelType();

        /// <summary>
        /// Creates an instance of <see cref="NavigationPage"/>
        /// </summary>
        [CanBeNull]
        protected virtual NavigationPage CreateNavigationPage(Page mainPage, IDataContext context)
        {
            if (context.GetData(WrapToNavigationPageConstant))
                return new NavigationPage(mainPage);
            return null;
        }

        private static bool CanShowViewModelTabPresenter(IViewModel viewModel, IDataContext dataContext, IViewModelPresenter arg3)
        {
            var viewName = viewModel.GetViewName(dataContext);
            var container = viewModel.GetIocContainer(true);
            var mappingProvider = container.Get<IViewMappingProvider>();
            var mappingItem = mappingProvider.FindMappingForViewModel(viewModel.GetType(), viewName, false);
            return mappingItem == null ||
                   typeof(ITabView).GetTypeInfo().IsAssignableFrom(mappingItem.ViewType.GetTypeInfo()) ||
                   !typeof(Page).GetTypeInfo().IsAssignableFrom(mappingItem.ViewType.GetTypeInfo());
        }

        private static bool CanShowViewModelNavigationPresenter(IViewModel viewModel, IDataContext dataContext, IViewModelPresenter arg3)
        {
            var viewName = viewModel.GetViewName(dataContext);
            var container = viewModel.GetIocContainer(true);
            var mappingProvider = container.Get<IViewMappingProvider>();
            var mappingItem = mappingProvider.FindMappingForViewModel(viewModel.GetType(), viewName, false);
            return mappingItem != null && typeof(Page).GetTypeInfo().IsAssignableFrom(mappingItem.ViewType.GetTypeInfo());
        }

        #endregion
    }
}