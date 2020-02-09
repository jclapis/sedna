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
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using GPhoto2.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Sedna
{
    /// <summary>
    /// The main window.
    /// </summary>
    public class MainWindow : Window
    {
        /// <summary>
        /// The image component that shows the viewfinder feed from the camera
        /// </summary>
        private readonly Image ViewfinderImage;


        /// <summary>
        /// The underlying bitmap source that viewfinder data will be written to for display
        /// </summary>
        private readonly WriteableBitmap ViewfinderBitmap;


        /// <summary>
        /// The camera selection combobox
        /// </summary>
        private readonly ComboBox CameraSelectionBox;


        /// <summary>
        /// The ISO setting combobox
        /// </summary>
        private readonly ComboBox IsoBox;


        /// <summary>
        /// A logger for capturing status and debug messages
        /// </summary>
        private readonly Logger Logger;


        /// <summary>
        /// The context for GPhoto2
        /// </summary>
        private readonly Context Context;


        /// <summary>
        /// The cameras that are connected to the machine
        /// </summary>
        private IReadOnlyList<Camera> Cameras;


        /// <summary>
        /// An internal flag used to disable setting setters during a camera refresh
        /// </summary>
        private bool RefreshingCameras;


        /// <summary>
        /// Creates the main window.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            // Set up the logger
            TextBox logBox = this.FindControl<TextBox>("LogBox");
            Logger = new Logger(logBox);

            // Get all of the named components
            CameraSelectionBox = this.FindControl<ComboBox>("CameraSelectionBox");
            ViewfinderImage = this.FindControl<Image>("ViewfinderImage");
            IsoBox = this.FindControl<ComboBox>("IsoBox");

            // Set up the viewfinder
            ViewfinderBitmap = new WriteableBitmap(new PixelSize(1920, 1080), new Vector(96, 96), PixelFormat.Bgra8888);
            ViewfinderImage.Source = ViewfinderBitmap;

            Context = new Context();
        }


        /// <summary>
        /// Initializes Avalonia's components upon loading
        /// </summary>
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }


        /// <summary>
        /// Disposes of the GPhoto2 context upon closing.
        /// </summary>
        /// <param name="e">Not used</param>
        protected override void OnClosed(EventArgs e)
        {
            Context.Dispose();
        }


        /// <summary>
        /// Refreshes the catalog of connected cameras.
        /// </summary>
        public void RefreshCameras(object Sender, RoutedEventArgs Args)
        {
            try
            {
                RefreshingCameras = true;
                Logger.Debug("Refreshing cameras.");
                Camera selectedCamera = (Camera)CameraSelectionBox.SelectedItem;
                Cameras = Context.GetCameras();
                Logger.Info($"Cameras refreshed, {Cameras.Count} found.");
                CameraSelectionBox.Items = Cameras;

                int newCameraIndex = 0;
                if (selectedCamera != null)
                {
                    for (int i = 0; i < Cameras.Count; i++)
                    {
                        Camera camera = Cameras[i];
                        if (camera.ModelName == selectedCamera.ModelName)
                        {
                            newCameraIndex = i;
                        }
                    }
                }

                if (newCameraIndex < Cameras.Count)
                {
                    CameraSelectionBox.SelectedIndex = newCameraIndex;
                }

                RefreshISOs();
            }
            catch(Exception ex)
            {
                Logger.Error($"Error refreshing cameras: {ex.Message}");
            }
            finally
            {
                RefreshingCameras = false;
            }
        }


        /// <summary>
        /// Refreshes the list of ISO settings available in the current camera.
        /// </summary>
        private void RefreshISOs()
        {
            Camera camera = (Camera)CameraSelectionBox.SelectedItem;
            if(camera == null)
            {
                return;
            }

            ConfigurationSection imageSettings = camera.Configuration.Sections.First(
                candidate => candidate.Title == "Image Settings");
            SelectionSetting isoSetting = (SelectionSetting)imageSettings.Settings.First(
                candidate => candidate.Title == "ISO Speed");

            IsoBox.Items = isoSetting.Options;
            string currentSetting = isoSetting.Value;
            IsoBox.SelectedItem = currentSetting;
        }


        /// <summary>
        /// Updates the camera when the user changes the ISO setting.
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        public void IsoBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(RefreshingCameras)
            {
                return;
            }

            string setting = (string)IsoBox.SelectedItem;
            if(setting == null)
            {
                return;
            }

            try
            {
                Camera camera = (Camera)CameraSelectionBox.SelectedItem;
                if (camera == null)
                {
                    return;
                }
                ConfigurationSection imageSettings = camera.Configuration.Sections.First(
                    candidate => candidate.Title == "Image Settings");
                SelectionSetting isoSetting = (SelectionSetting)imageSettings.Settings.First(
                    candidate => candidate.Title == "ISO Speed");

                isoSetting.Value = setting;
                camera.UpdateConfiguration();
            }
            catch(Exception ex)
            {
                Logger.Error($"Error updating ISO setting: {ex.Message}");
            }
        }


        /// <summary>
        /// NYI
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="Args"></param>
        public void CaptureButton_Click(object Sender, RoutedEventArgs Args)
        {

        }


        /// <summary>
        /// Updates the viewfinder image with new image data.
        /// </summary>
        /// <param name="NewData">The new data to show in the viewfinder</param>
        private void UpdateViewfinder(byte[] NewData)
        {
            using (ILockedFramebuffer buffer = ViewfinderBitmap.Lock())
            {
                Marshal.Copy(NewData, 0, buffer.Address, NewData.Length);
            }
            ViewfinderImage.InvalidateVisual();
        }

    }
}
