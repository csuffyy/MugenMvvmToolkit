#region Copyright
// ****************************************************************************
// <copyright file="IApplicationStateManager.cs">
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
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MugenMvvmToolkit.Interfaces.Models;

namespace MugenMvvmToolkit.Interfaces
{
    /// <summary>
    ///     Represents the application state manager.
    /// </summary>
    public interface IApplicationStateManager
    {
        /// <summary>
        ///     Occurs on save element state.
        /// </summary>
        void EncodeState([NotNull] NSObject item, [NotNull] NSCoder state, IDataContext context = null);

        /// <summary>
        ///     Occurs on load element state.
        /// </summary>
        void DecodeState([NotNull] NSObject item, [NotNull] NSCoder state, IDataContext context = null);

        /// <summary>
        ///     Tries to restore view controller.
        /// </summary>
        [CanBeNull]
        UIViewController GetViewController([NotNull] string[] restorationIdentifierComponents, [NotNull] NSCoder coder,
            IDataContext context = null);
    }
}