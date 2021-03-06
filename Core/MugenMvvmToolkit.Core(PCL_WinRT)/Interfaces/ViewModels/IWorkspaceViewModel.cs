﻿#region Copyright
// ****************************************************************************
// <copyright file="IWorkspaceViewModel.cs">
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

using MugenMvvmToolkit.Interfaces.Models;

namespace MugenMvvmToolkit.Interfaces.ViewModels
{
    /// <summary>
    ///     Represents the base interface for the view model, that can be displayed in the UI.
    /// </summary>
    public interface IWorkspaceViewModel : IHasDisplayName, ISelectable, ICloseableViewModel
    {
    }
}