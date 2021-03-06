#region Copyright
// ****************************************************************************
// <copyright file="NavigatingCancelEventArgs.cs">
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

using JetBrains.Annotations;
using MugenMvvmToolkit.Interfaces.Models;

namespace MugenMvvmToolkit.Models.EventArg
{
    /// <summary>
    ///     Provides event data for the OnNavigatingFrom callback that can be used to cancel a navigation request from
    ///     origination.
    /// </summary>
    public class NavigatingCancelEventArgs : NavigatingCancelEventArgsBase
    {
        #region Fields

        private readonly bool _isCancelable;
        private readonly IViewMappingItem _mapping;
        private readonly NavigationMode _navigationMode;
        private readonly object _parameter;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="NavigatingCancelEventArgs" /> class with the
        ///     <see cref="P:System.ComponentModel.CancelEventArgs.Cancel" /> property set to false.
        /// </summary>
        public NavigatingCancelEventArgs(IViewMappingItem mapping, NavigationMode navigationMode, object parameter)
        {
            _mapping = mapping;
            _navigationMode = navigationMode;
            _parameter = parameter;
            _isCancelable = true;
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the mapping item to navigate.
        /// </summary>
        [CanBeNull]
        public IViewMappingItem Mapping
        {
            get { return _mapping; }
        }

        /// <summary>
        ///     Gets any Parameter object passed to the target page for the navigation.
        /// </summary>
        public object Parameter
        {
            get { return _parameter; }
        }

        #endregion

        #region Overrides of NavigatingCancelEventArgsBase

        /// <summary>
        ///     Specifies whether a pending navigation should be canceled.
        /// </summary>
        /// <returns>
        ///     true to cancel the pending cancelable navigation; false to continue with navigation.
        /// </returns>
        public override bool Cancel { get; set; }

        /// <summary>
        ///     Gets a value that indicates the type of navigation that is occurring.
        /// </summary>
        public override NavigationMode NavigationMode
        {
            get { return _navigationMode; }
        }

        /// <summary>
        ///     Gets a value that indicates whether you can cancel the navigation.
        /// </summary>
        public override bool IsCancelable
        {
            get { return _isCancelable; }
        }

        #endregion
    }
}