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
using System;
using System.Collections.Generic;

namespace Sedna
{
    /// <summary>
    /// This class wraps <see cref="ButtonSpinner"/> and merges it with the functionality of
    /// <see cref="ItemsControl"/>, so clicking the spinner buttons cycles through the options
    /// contained within the item list.
    /// </summary>
    internal class ButtonSpinnerSelectionHandler
    {
        /// <summary>
        /// The text block that displays the current selection
        /// </summary>
        private readonly TextBlock ContentBlock;


        /// <summary>
        /// The underlying spinner object
        /// </summary>
        private readonly ButtonSpinner Spinner;


        /// <summary>
        /// The list of items that the spinner can cycle through
        /// </summary>
        public IReadOnlyList<string> Items { get; set; }


        /// <summary>
        /// This is fired when the spinner's selection changes
        /// </summary>
        public event EventHandler<string> SelectionChanged;


        /// <summary>
        /// Creates a new <see cref="ButtonSpinnerSelectionHandler"/> instance.
        /// </summary>
        /// <param name="Spinner">The spinner to wrap</param>
        public ButtonSpinnerSelectionHandler(ButtonSpinner Spinner)
        {
            this.Spinner = Spinner;
            ContentBlock = (TextBlock)Spinner.Content;
            Spinner.Spin += SelectionSpinner_Spin;
            Spinner.ValidSpinDirection = ValidSpinDirections.None;
        }


        /// <summary>
        /// The item that is currently selected by this spinner
        /// </summary>
        public string SelectedItem
        {
            get
            {
                return ContentBlock.Text;
            }
            set
            {
                ContentBlock.Text = value;
                if(value == Items[0])
                {
                    Spinner.ValidSpinDirection = ValidSpinDirections.Decrease;
                }
                else if(value == Items[Items.Count - 1])
                {
                    Spinner.ValidSpinDirection = ValidSpinDirections.Increase;
                }
                else
                {
                    Spinner.ValidSpinDirection = ValidSpinDirections.Increase | ValidSpinDirections.Decrease;
                }
            }
        }


        /// <summary>
        /// The index of the item that is currently selected
        /// </summary>
        public int SelectedIndex
        {
            get
            {
                for(int i = 0; i < Items.Count; i++)
                {
                    if(Items[i] == ContentBlock.Text)
                    {
                        return i;
                    }
                }
                return -1;
            }
            set
            {
                ContentBlock.Text = Items[value];
                if(value == 0)
                {
                    Spinner.ValidSpinDirection = ValidSpinDirections.Decrease;
                }
                else if (value == Items.Count - 1)
                {
                    Spinner.ValidSpinDirection = ValidSpinDirections.Increase;
                }
                else
                {
                    Spinner.ValidSpinDirection = ValidSpinDirections.Increase | ValidSpinDirections.Decrease;
                }
            }
        }


        /// <summary>
        /// Handle the spin event by moving the selected index.
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">The type of spin event that just occurred</param>
        private void SelectionSpinner_Spin(object sender, SpinEventArgs e)
        {
            if (e.Direction == SpinDirection.Increase)
            {
                SelectedIndex--;
            }
            else
            {
                SelectedIndex++;
            }

            SelectionChanged?.Invoke(this, ContentBlock.Text);
        }

    }
}
