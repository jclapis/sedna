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

using Avalonia;
using Avalonia.Controls;
using System;
using System.Text;

namespace Sedna
{
    /// <summary>
    /// This contains some extension methods for the Avalonia classes.
    /// </summary>
    internal static class Extensions
    {
        /// <summary>
        /// Scrolls a <see cref="ScrollViewer"/> to the bottom-left corner. This is temporary until
        /// https://github.com/AvaloniaUI/Avalonia/pull/3532 is accepted.
        /// </summary>
        /// <param name="ScrollViewer">The ScrollViewer to scroll</param>
        public static void ScrollToEnd(this ScrollViewer ScrollViewer)
        {
            ScrollViewer.Offset = new Vector(double.NegativeInfinity, double.PositiveInfinity);
        }


        /// <summary>
        /// Gets the message and stack trace for an exception and all of its inner exceptions.
        /// </summary>
        /// <param name="Ex">The exception to get the details for</param>
        /// <returns>The details of the exception</returns>
        public static string GetDetails(this Exception Ex)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(Ex.Message);
            builder.AppendLine(Ex.StackTrace);

            Exception inner = Ex.InnerException;
            while(inner != null)
            {
                builder.AppendLine("Inner:");
                builder.AppendLine(inner.Message);
                builder.AppendLine(inner.StackTrace);
                inner = inner.InnerException;
            }

            return builder.ToString();
        }

    }
}
