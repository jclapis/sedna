/* ========================================================================
 * Copyright (C) 2020 Joe Clapis.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * ======================================================================== */

using Avalonia.Controls;
using Avalonia.Threading;
using System;

namespace Sedna
{
    /// <summary>
    /// This logs messages to the UI's log box.
    /// </summary>
    public class Logger
    {
        /// <summary>
        /// The text box in the UI to add the messages to
        /// </summary>
        private readonly TextBox Box;


        /// <summary>
        /// Creates a new <see cref="Logger"/> instance.
        /// </summary>
        /// <param name="Box">The text box in the UI to add the messages to</param>
        public Logger(TextBox Box)
        {
            this.Box = Box;
        }


        /// <summary>
        /// Logs a debug-level message.
        /// </summary>
        /// <param name="Message">The message to log</param>
        public void Debug(string Message)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                Box.Text += $"[Debug] - {Message}{Environment.NewLine}";
                ((ScrollViewer)Box.Parent).ScrollToEnd();
            });
        }


        /// <summary>
        /// Logs an info-level message.
        /// </summary>
        /// <param name="Message">The message to log</param>
        public void Info(string Message)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                Box.Text += $"[Info] - {Message}{Environment.NewLine}";
                ((ScrollViewer)Box.Parent).ScrollToEnd();
            });
        }


        /// <summary>
        /// Logs an error-level message.
        /// </summary>
        /// <param name="Message">The message to log</param>
        public void Error(string Message)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                Box.Text += $"[ERROR] - {Message}{Environment.NewLine}";
                ((ScrollViewer)Box.Parent).ScrollToEnd();
            });
        }

    }
}
