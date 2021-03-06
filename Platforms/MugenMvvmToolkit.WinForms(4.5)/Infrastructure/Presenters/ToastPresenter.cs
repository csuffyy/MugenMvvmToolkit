﻿#region Copyright
// ****************************************************************************
// <copyright file="ToastPresenter.cs">
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
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using JetBrains.Annotations;
using MugenMvvmToolkit.Controls;
using MugenMvvmToolkit.Interfaces;
using MugenMvvmToolkit.Interfaces.Models;
using MugenMvvmToolkit.Interfaces.Presenters;
using MugenMvvmToolkit.Models;

namespace MugenMvvmToolkit.Infrastructure.Presenters
{
    /// <summary>
    ///     Provides functionality to present a timed message.
    /// </summary>
    public class ToastPresenter : IToastPresenter
    {
        #region Fields

        private const int TimerInterval = 45;
        private static readonly string ControlName;
        private readonly IThreadManager _threadManager;

        #endregion

        #region Constructors

        static ToastPresenter()
        {
            ControlName = Guid.NewGuid().ToString("n");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ToastPresenter"/> class.
        /// </summary>
        public ToastPresenter([NotNull] IThreadManager threadManager)
        {
            Should.NotBeNull(threadManager, "threadManager");
            _threadManager = threadManager;
            Background = Color.FromArgb(255, 105, 105, 105);
            Foreground = Color.FromArgb(255, 247, 247, 247);
        }

        #endregion

        #region Properties

        public Color? Glow { get; set; }

        public Color Background { get; set; }

        public Color Foreground { get; set; }

        public Font Font { get; set; }

        #endregion

        #region Implementation of IToastPresenter

        /// <summary>
        ///     Shows the specified message.
        /// </summary>
        public Task ShowAsync(object content, float duration, ToastPosition position = ToastPosition.Bottom, IDataContext context = null)
        {
            var tcs = new TaskCompletionSource<object>();
            if (_threadManager.IsUiThread)
                ShowInternal(content, duration, position, context, tcs);
            else
                _threadManager.InvokeOnUiThreadAsync(() => ShowInternal(content, duration, position, context, tcs));
            return tcs.Task;
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Shows the specified message.
        /// </summary>
        protected virtual void ShowInternal(object content, float duration, ToastPosition position, IDataContext context, TaskCompletionSource<object> tcs)
        {
            Form activeForm = Form.ActiveForm;
            if (activeForm == null)
            {
                tcs.SetResult(null);
                return;
            }
            foreach (var result in activeForm.Controls.Find(ControlName, false).OfType<ToastMessageControl>())
            {
                if (duration >= result.Duration)
                    ClearControl(result);
            }
            var control = GetToastControl(activeForm, content, duration, tcs);
            control.Name = ControlName;
            activeForm.Controls.Add(control);
            SetPosition(activeForm, control, position);
            control.BringToFront();
            var timer = new Timer { Interval = TimerInterval, Tag = control };
            timer.Tick += TimerTick;
            timer.Start();
            control.Tag = timer;
        }

        [NotNull]
        protected virtual ToastMessageControl GetToastControl(Form form, object content, float duration, TaskCompletionSource<object> tcs)
        {
            string msg = content == null ? "(null)" : content.ToString();
            var control = new ToastMessageControl(msg, duration, Background, Foreground, Glow, tcs)
            {
                IsTransparent = true
            };
            Font font = Font;
            if (font == null)
                font = form.Font;
            else
                control.Font = font;
            using (Graphics gr = control.CreateGraphics())
            {
                SizeF textSize = gr.MeasureString(msg, font);
                control.Height = (int)textSize.Height + 25;
                control.Width = (int)textSize.Width + 35;
                if (textSize.Width > form.Width - 100)
                {
                    control.Width = form.Width - 100;
                    var hf = textSize.Width / control.Width;
                    control.Height += (int)(textSize.Height * hf);
                }
                if (control.Height > form.Height)
                    control.Height = form.Height;
            }
            return control;
        }

        private static void SetPosition(Control parent, Control control, ToastPosition position)
        {
            control.Left = (parent.ClientSize.Width - control.Width) / 2;
            switch (position)
            {
                case ToastPosition.Bottom:
                    control.Top = parent.ClientSize.Height - control.Height - 20;
                    control.Anchor = AnchorStyles.Bottom;
                    break;
                case ToastPosition.Center:
                    control.Top = (parent.ClientSize.Height - control.Height) / 2;
                    control.Anchor = AnchorStyles.None;
                    break;
                case ToastPosition.Top:
                    control.Top = 20;
                    control.Anchor = AnchorStyles.Top;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("position");
            }
        }

        private static void TimerTick(object sender, EventArgs args)
        {
            var control = (ToastMessageControl)((Timer)sender).Tag;
            if (control.Duration > 0)
            {
                control.Duration -= TimerInterval;
                if (control.AlphaValue == 1f)
                    return;
                control.AlphaValue += 0.1f;
                if (control.AlphaValue > 1f)
                    control.AlphaValue = 1f;
                control.Refresh();
            }
            else
            {
                if (control.AlphaValue == 0f)
                {
                    ClearControl(control);
                    return;
                }
                control.AlphaValue -= 0.1f;
                if (control.AlphaValue < 0f)
                    control.AlphaValue = 0f;
                control.Refresh();
            }
        }

        private static void ClearControl(ToastMessageControl control)
        {
            control.TaskCompletionSource.TrySetResult(null);
            if (control.Parent != null)
                control.Parent.Controls.Remove(control);
            ((Timer)control.Tag).Dispose();
            control.Dispose();
        }

        #endregion
    }
}