﻿#region Copyright
// ****************************************************************************
// <copyright file="MessagePresenter.cs">
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
using System.Threading.Tasks;
using System.Windows.Forms;
using MugenMvvmToolkit.Interfaces;
using MugenMvvmToolkit.Interfaces.Models;
using MugenMvvmToolkit.Interfaces.Presenters;
using MugenMvvmToolkit.Models;

namespace MugenMvvmToolkit.Infrastructure.Presenters
{
    /// <summary>
    ///     Represents the base implementation of <see cref="IMessagePresenter" />.
    /// </summary>
    public sealed class MessagePresenter : IMessagePresenter
    {
        #region Fields

        private readonly IThreadManager _threadManager;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="MessagePresenter" /> class.
        /// </summary>
        public MessagePresenter(IThreadManager threadManager)
        {
            Should.NotBeNull(threadManager, "threadManager");
            _threadManager = threadManager;
        }

        #endregion

        #region Implementation of IMessagePresenter

        /// <summary>
        ///     Displays a message box that has a message, title bar caption, button, and icon; and that accepts a default message
        ///     box result and returns a result.
        /// </summary>
        /// <param name="messageBoxText">A <see cref="T:System.String" /> that specifies the text to display.</param>
        /// <param name="caption">A <see cref="T:System.String" /> that specifies the title bar caption to display.</param>
        /// <param name="button">A <see cref="MessageButton" /> value that specifies which button or buttons to display.</param>
        /// <param name="icon">A <see cref="MessageImage" /> value that specifies the icon to display.</param>
        /// <param name="defaultResult">
        ///     A <see cref="MessageResult" /> value that specifies the default result of the message
        ///     box.
        /// </param>
        /// <param name="context">The specified context.</param>
        /// <returns>A <see cref="MessageResult" /> value that specifies which message box button is clicked by the user.</returns>
        public Task<MessageResult> ShowAsync(string messageBoxText, string caption = "",
            MessageButton button = MessageButton.Ok, MessageImage icon = MessageImage.None,
            MessageResult defaultResult = MessageResult.None, IDataContext context = null)
        {
            if (_threadManager.IsUiThread)
                return ToolkitExtensions.FromResult(ShowMessage(messageBoxText, caption, button, icon, defaultResult));
            var tcs = new TaskCompletionSource<MessageResult>();
            _threadManager.InvokeOnUiThreadAsync(
                () => tcs.SetResult(ShowMessage(messageBoxText, caption, button, icon, defaultResult)));
            return tcs.Task;
        }

        #endregion

        #region Methods

        private MessageResult ShowMessage(string messageBoxText, string caption, MessageButton button, MessageImage icon,
            MessageResult defaultResult)
        {
            DialogResult result = MessageBox.Show(messageBoxText, caption, ConvertButtons(button), ConvertImage(icon),
                ConvertDefaultResult(button, defaultResult));
            return ConvertResult(result);
        }

        private static MessageResult ConvertResult(DialogResult result)
        {
            switch (result)
            {
                case DialogResult.OK:
                    return MessageResult.Ok;
                case DialogResult.Cancel:
                    return MessageResult.Cancel;
                case DialogResult.Abort:
                    return MessageResult.Abort;
                case DialogResult.Retry:
                    return MessageResult.Retry;
                case DialogResult.Ignore:
                    return MessageResult.Ignore;
                case DialogResult.Yes:
                    return MessageResult.Yes;
                case DialogResult.No:
                    return MessageResult.No;
                default:
                    return MessageResult.None;
            }
        }

        private static MessageBoxDefaultButton ConvertDefaultResult(MessageButton buttons, MessageResult results)
        {
            switch (results)
            {
                case MessageResult.Ok:
                    return MessageBoxDefaultButton.Button1;
                case MessageResult.Cancel:
                    if (buttons == MessageButton.YesNoCancel)
                        return MessageBoxDefaultButton.Button3;
                    return MessageBoxDefaultButton.Button2;
                case MessageResult.No:
                    return MessageBoxDefaultButton.Button2;
                case MessageResult.Yes:
                    return MessageBoxDefaultButton.Button1;
                case MessageResult.Abort:
                    return MessageBoxDefaultButton.Button1;
                case MessageResult.Retry:
                    if (buttons == MessageButton.AbortRetryIgnore)
                        return MessageBoxDefaultButton.Button2;
                    return MessageBoxDefaultButton.Button1;
                case MessageResult.Ignore:
                    return MessageBoxDefaultButton.Button3;
                default:
                    return MessageBoxDefaultButton.Button1;
            }
        }

        private static MessageBoxButtons ConvertButtons(MessageButton buttons)
        {
            switch (buttons)
            {
                case MessageButton.Ok:
                    return MessageBoxButtons.OK;
                case MessageButton.OkCancel:
                    return MessageBoxButtons.OKCancel;
                case MessageButton.YesNo:
                    return MessageBoxButtons.YesNo;
                case MessageButton.YesNoCancel:
                    return MessageBoxButtons.YesNoCancel;
                case MessageButton.AbortRetryIgnore:
                    return MessageBoxButtons.AbortRetryIgnore;
                case MessageButton.RetryCancel:
                    return MessageBoxButtons.RetryCancel;
                default:
                    return MessageBoxButtons.OK;
            }
        }

        private static MessageBoxIcon ConvertImage(MessageImage images)
        {
            switch (images)
            {
                case MessageImage.Asterisk:
                    return MessageBoxIcon.Asterisk;
                case MessageImage.Error:
                    return MessageBoxIcon.Error;
                case MessageImage.Exclamation:
                    return MessageBoxIcon.Exclamation;
                case MessageImage.Hand:
                    return MessageBoxIcon.Hand;
                case MessageImage.Information:
                    return MessageBoxIcon.Information;
                case MessageImage.None:
                    return MessageBoxIcon.None;
                case MessageImage.Question:
                    return MessageBoxIcon.Question;
                case MessageImage.Stop:
                    return MessageBoxIcon.Stop;
                case MessageImage.Warning:
                    return MessageBoxIcon.Warning;
                default:
                    return MessageBoxIcon.None;
            }
        }

        #endregion
    }
}