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
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using GPhoto2.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Path = System.IO.Path;

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
        /// The camera selection combobox
        /// </summary>
        private readonly ComboBox CameraSelectionBox;


        /// <summary>
        /// The ISO setting combobox
        /// </summary>
        private readonly ComboBox IsoBox;


        /// <summary>
        /// The manager for the exposure setting box
        /// </summary>
        private readonly ButtonSpinnerSelectionHandler ExposureBox;


        /// <summary>
        /// The image format setting combobox
        /// </summary>
        private readonly ComboBox ImageFormatBox;


        /// <summary>
        /// The camera output setting combobox
        /// </summary>
        private readonly ComboBox CameraOutputBox;


        /// <summary>
        /// The capture toggle checkbnox
        /// </summary>
        private readonly CheckBox CaptureToggle;


        /// <summary>
        /// The tab control with all of the image tabs and the viewfinder
        /// </summary>
        private readonly TabControl ImageTabControl;


        /// <summary>
        /// A logger for capturing status and debug messages
        /// </summary>
        private readonly Logger Logger;


        /// <summary>
        /// The camera I/O and settings manager
        /// </summary>
        private readonly CameraManager CameraManager;


        /// <summary>
        /// The slider for the live view speed
        /// </summary>
        private readonly Slider ViewfinderSpeedSlider;


        /// <summary>
        /// The label for the live view speed
        /// </summary>
        private readonly TextBlock ViewfinderSpeedLabel;


        /// <summary>
        /// The vertical crosshair bar
        /// </summary>
        private readonly Rectangle VerticalCrosshairBar;


        /// <summary>
        /// The horizontal crosshair bar
        /// </summary>
        private readonly Rectangle HorizontalCrosshairBar;


        /// <summary>
        /// The crosshair circle
        /// </summary>
        private readonly Ellipse CrosshairCircle;


        /// <summary>
        /// The underlying bitmap source that viewfinder data will be written to for display
        /// </summary>
        private WriteableBitmap ViewfinderBitmap;


        /// <summary>
        /// An internal flag used to disable setting setters during a camera refresh
        /// </summary>
        private bool RefreshingCameras;


        /// <summary>
        /// True when the viewfinder is enabled.
        /// </summary>
        private bool IsLiveViewActive;


        /// <summary>
        /// The number of milliseconds to wait between refreshing the live view.
        /// </summary>
        private int LiveViewDelay;


        private Task LiveViewTask;


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
            ButtonSpinner exposureSpinner = this.FindControl<ButtonSpinner>("ExposureBox");
            ExposureBox = new ButtonSpinnerSelectionHandler(exposureSpinner);
            ExposureBox.SelectionChanged += ExposureBox_SelectionChanged;
            ImageTabControl = this.FindControl<TabControl>("ImageTabControl");
            ImageFormatBox = this.FindControl<ComboBox>("ImageFormatBox");
            CameraOutputBox = this.FindControl<ComboBox>("CameraOutputBox");
            CaptureToggle = this.FindControl<CheckBox>("CaptureToggle");
            ViewfinderSpeedSlider = this.FindControl<Slider>("ViewfinderSpeedSlider");
            ViewfinderSpeedLabel = this.FindControl<TextBlock>("ViewfinderSpeedLabel");
            VerticalCrosshairBar = this.FindControl<Rectangle>("VerticalCrosshairBar");
            HorizontalCrosshairBar = this.FindControl<Rectangle>("HorizontalCrosshairBar");
            CrosshairCircle = this.FindControl<Ellipse>("CrosshairCircle");

            // Misc other setup
            LiveViewDelay = 50;

            if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                CameraManager = new CameraManager(Logger);
            }

            RefreshCameras();
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
            if(CameraManager != null)
            {
                CameraManager.Dispose();
            }
        }


        /// <summary>
        /// Refreshes the catalog of connected cameras.
        /// </summary>
        private void RefreshCameras()
        {
            try
            {
                RefreshingCameras = true;

                CameraSelectionBox.Items = null;
                IsoBox.Items = null;

                IReadOnlyList<Camera> cameras = CameraManager?.RefreshCameras();
                if(cameras == null)
                {
                    return;
                }

                CameraSelectionBox.Items = cameras;
                if(cameras.Count > 0)
                {
                    // Set the active camera to the first one in the list
                    Camera activeCamera = cameras[0];
                    CameraSelectionBox.SelectedIndex = 0;
                    CameraManager.SetActiveCamera(activeCamera);

                    // Get the ISO settings
                    (IReadOnlyList<string> isoOptions, string iso) = CameraManager.GetIsoSettings();
                    IsoBox.Items = isoOptions;
                    IsoBox.SelectedItem = iso;

                    // Get the exposure settings
                    (IReadOnlyList<string> exposureOptions, string exposure) = CameraManager.GetExposureSettings();
                    ExposureBox.Items = exposureOptions;
                    ExposureBox.SelectedItem = exposure;

                    // Get the image format settings
                    (IReadOnlyList<string> imageFormatOptions, string imageFormat) = CameraManager.GetImageFormatSettings();
                    ImageFormatBox.Items = imageFormatOptions;
                    ImageFormatBox.SelectedItem = imageFormat;

                    // Get the camera output settings
                    (IReadOnlyList<string> cameraOutputOptions, string cameraOutput) = CameraManager.GetCameraOutputSettings();
                    CameraOutputBox.Items = cameraOutputOptions;
                    CameraOutputBox.SelectedItem = cameraOutput;
                }
            }
            catch(Exception ex)
            {
                Logger.Error($"Error refreshing cameras: {ex.GetDetails()}");
            }
            finally
            {
                RefreshingCameras = false;
            }
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

            try
            {
                string setting = (string)IsoBox.SelectedItem;
                CameraManager.SetIso(setting);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error setting ISO: {ex.GetDetails()}");
            }
        }


        /// <summary>
        /// Updates the camera when the user changes the exposure setting.
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="NewSetting">The new exposure setting</param>
        public void ExposureBox_SelectionChanged(object sender, string NewSetting)
        {
            if (RefreshingCameras)
            {
                return;
            }

            try
            {
                CameraManager.SetExposure(NewSetting);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error setting exposure: {ex.GetDetails()}");
            }
        }


        /// <summary>
        /// Updates the camera when the user changes the image format setting.
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        public void ImageFormatBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RefreshingCameras)
            {
                return;
            }

            try
            {
                string setting = (string)ImageFormatBox.SelectedItem;
                CameraManager.SetImageFormat(setting);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error setting image format: {ex.GetDetails()}");
            }
        }


        /// <summary>
        /// Updates the camera when the user changes the camera output setting.
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        public void CameraOutputBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RefreshingCameras)
            {
                return;
            }

            try
            {
                string setting = (string)CameraOutputBox.SelectedItem;
                if(setting.Contains("PC"))
                {
                    // Set the output before starting the live view thread
                    CameraManager.SetCameraOutput(setting);
                    if (!IsLiveViewActive)
                    {
                        // Set up the viewfinder image
                        ViewfinderBitmap = CameraManager.CreateWriteableBitmapForPreview();
                        ViewfinderImage.Source = ViewfinderBitmap;

                        IsLiveViewActive = true;
                        LiveViewTask = Task.Run(UpdateViewfinder);
                    }
                }
                else
                { 
                    // Kill the live view thread before setting the output
                    if (IsLiveViewActive)
                    {
                        IsLiveViewActive = false;
                        LiveViewTask.Wait();
                        LiveViewTask = null;
                    }
                    CameraManager.SetCameraOutput(setting);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error setting camera output: {ex.GetDetails()}");
            }
        }


        /// <summary>
        /// Captures an image from the camera.
        /// </summary>
        /// <param name="Sender">Not used</param>
        /// <param name="Args">Not used</param>
        public void CaptureButton_Click(object Sender, RoutedEventArgs Args)
        {
            if(CameraManager == null)
            {
                return;
            }

            Bitmap imageData;
            string filename;

            // Get the image from the camera and create the UI components for it
            try
            {
                (imageData, filename) = CameraManager.CaptureImage();

                // Create the close button for the tab that will hold this image
                MenuItem closeItem = new MenuItem()
                {
                    Header = "_Close"
                };
                closeItem.Click += CloseImageTab;

                // Create the new tab for the image
                TabItem imageTab = new TabItem()
                {
                    ContextMenu = new ContextMenu
                    {
                        Items = new MenuItem[] { closeItem }
                    },
                    Header = filename,
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(1),
                    FontSize = 14,
                    Content = new Border
                    {
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(4),
                        Child = new Image()
                        {
                            Source = imageData
                        }
                    }
                };

                // Add it to the list of tabs and set it as the active one
                List<TabItem> tabs = new List<TabItem>();
                foreach(TabItem tab in ImageTabControl.Items)
                {
                    tabs.Add(tab);
                }
                tabs.Add(imageTab);
                ImageTabControl.Items = tabs;
                ImageTabControl.SelectedIndex = tabs.Count - 1;
            }
            catch(Exception ex)
            {
                Logger.Error($"Error capturing image: {ex.GetDetails()}");
                return;
            }

            // Save the image out to a file
            try
            {
                string dirName = DateTime.Now.ToString("MM-dd-yy");
                Directory.CreateDirectory(Path.Combine("Images", dirName));
                imageData.Save(Path.Combine("Images", dirName, filename));
            }
            catch(Exception ex)
            {
                Logger.Error($"Error saving image {filename} to file: {ex.GetDetails()}");
            }
        }


        /// <summary>
        /// Toggles the crosshairs in the viewfinder.
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">Not used</param>
        public void Crosshairs_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox crosshairBox = (CheckBox)e.Source;
            if(crosshairBox.IsChecked == true)
            {
                VerticalCrosshairBar.Opacity = 1;
                HorizontalCrosshairBar.Opacity = 1;
                CrosshairCircle.Opacity = 1;
            }
            else
            {
                VerticalCrosshairBar.Opacity = 0;
                HorizontalCrosshairBar.Opacity = 0;
                CrosshairCircle.Opacity = 0;
            }
        }


        /// <summary>
        /// Closes an image tab when the user clicks the Close button from its context menu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseImageTab(object sender, RoutedEventArgs e)
        {
            MenuItem contextMenu = (MenuItem)sender;

            // Traverse the tree until we get to the TabItem
            IControl parent = contextMenu.Parent;
            TabItem closingTab;
            while(!(parent is TabItem) && parent != null)
            {
                parent = parent.Parent;
            }
            closingTab = (TabItem)parent;

            // Make a new list that doesn't include the closing tab
            List<TabItem> tabItems = new List<TabItem>();
            foreach(TabItem existingTab in ImageTabControl.Items)
            {
                if(existingTab != closingTab)
                {
                    tabItems.Add(existingTab);
                }
            }

            ImageTabControl.Items = tabItems;
        }


        /// <summary>
        /// Updates the viewfinder image with new image data.
        /// </summary>
        /// <param name="NewData">The new data to show in the viewfinder</param>
        private void UpdateViewfinder()
        {
            while(IsLiveViewActive)
            {
                try
                {
                    CameraManager.UpdatePreview(ViewfinderBitmap);
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        ViewfinderImage.InvalidateVisual();
                    });
                    Thread.Sleep(LiveViewDelay);
                }
                catch(Exception ex)
                {
                    Logger.Error($"Error updating live view: {ex.GetDetails()}");
                }

            }
        }

        public void ViewfinderSpeedSlider_PropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            if(e.Property.Name != "Value")
            {
                return;
            }

            int fps = (int)(double)e.NewValue;
            LiveViewDelay = 1000 / fps;
            ViewfinderSpeedLabel.Text = $"Viewfinder FPS: {fps}";
        }


        public void MotorButton_Click(object sender, RoutedEventArgs e)
        {
            MotorControlWindow window = new MotorControlWindow
            {
                Owner = this
            };

            Amt22 encoder = new Amt22(17, Logger);
            L6470 driver = new L6470(18, 0.9, 1.7);
            window.SetHardware(encoder, driver);
            window.Show();
        }

    }
}
