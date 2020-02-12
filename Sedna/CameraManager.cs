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
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using GPhoto2.Net;
using System;
using System.Collections.Generic;
using System.IO;

namespace Sedna
{
    /// <summary>
    /// This class manages all of the camera I/O and settings modifications.
    /// </summary>
    internal class CameraManager : IDisposable
    {
        /// <summary>
        /// The title of the ISO setting
        /// </summary>
        private const string IsoTitle = "ISO Speed";


        /// <summary>
        /// The title of the exposure setting
        /// </summary>
        private const string ExposureTitle = "Shutter Speed";


        /// <summary>
        /// The title of the image format setting
        /// </summary>
        private const string ImageFormatTitle = "Image Format";


        /// <summary>
        /// The title of the camera output setting
        /// </summary>
        private const string CameraOutputTitle = "Camera Output";


        /// <summary>
        /// The context for GPhoto2
        /// </summary>
        private readonly Context Context;


        /// <summary>
        /// A logger for capturing status and debug messages
        /// </summary>
        private readonly Logger Logger;


        /// <summary>
        /// The cameras that are connected to the machine
        /// </summary>
        private IReadOnlyList<Camera> Cameras;


        /// <summary>
        /// The camera that is currently selected and active
        /// </summary>
        private Camera ActiveCamera;


        /// <summary>
        /// Creates a new <see cref="CameraManager"/> instance.
        /// </summary>
        /// <param name="Logger">A logger for capturing status and debug messages</param>
        public CameraManager(Logger Logger)
        {
            this.Logger = Logger;
            Context = new Context();
            Context.StatusNotification += Context_StatusNotification;
            Context.ProgressStarted += Context_ProgressStarted;
            Context.ProgressUpdated += Context_ProgressUpdated;
            Context.ProgressStopped += Context_ProgressStopped;
            Context.MessageReceived += Context_MessageReceived;
            Context.IdleNotification += Context_IdleNotification;
            Context.ErrorOccurred += Context_ErrorOccurred;
        }

        private void Context_ErrorOccurred(object sender, string e)
        {
            Logger.Error($"Camera sent an error: {e}");
        }

        private void Context_IdleNotification(object sender, EventArgs e)
        {
            Logger.Info($"GPhoto2 is now idle.");
        }

        private void Context_MessageReceived(object sender, string e)
        {
            Logger.Info($"New message: {e}");
        }

        private void Context_ProgressStopped(object sender, uint e)
        {
            Logger.Info($"Op {e} complete!");
        }

        private void Context_ProgressUpdated(object sender, ProgressUpdateArgs e)
        {
            Logger.Info($"Progress updated for op {e.ProgressID}: {e.Progress.ToString("P2")}");
        }

        private void Context_ProgressStarted(object sender, ProgressStartArgs e)
        {
            Logger.Info($"Progress started for op {e.ProgressID}: {e.Message}");
        }

        private void Context_StatusNotification(object sender, string e)
        {
            Logger.Info($"New status message: {e}");
        }


        /// <summary>
        /// Refreshes and retrieves the list of cameras that are currently connected.
        /// </summary>
        /// <returns>The list of connected cameras</returns>
        public IReadOnlyList<Camera> RefreshCameras()
        {
            // Clear the current camera list
            Logger.Debug("Clearing existing cameras...");
            if (Cameras != null)
            {
                foreach(Camera camera in Cameras)
                {
                    camera.Disconnect();
                    camera.Dispose();
                }
                Cameras = null;
            }
            ActiveCamera = null;

            Logger.Debug("Cameras cleared, scanning for connected cameras...");
            IReadOnlyList<Camera> cameras = Context.GetCameras();
            Logger.Info($"Cameras refreshed, {cameras.Count} found.");
            
            return cameras;
        }


        /// <summary>
        /// Sets the camera to use.
        /// </summary>
        /// <param name="Camera">The camera to use</param>
        public void SetActiveCamera(Camera Camera)
        {
            if(Camera == ActiveCamera)
            {
                return;
            }

            //ActiveCamera?.Disconnect();
            //Camera.Connect();
            ActiveCamera = Camera;
        }


        /// <summary>
        /// Gets the list of ISO settings the camera supports, along with the current setting.
        /// </summary>
        /// <returns>The camera's supported ISO settings and the current one</returns>
        public (IReadOnlyList<string> Options, string Current) GetIsoSettings()
        {
            CameraConfiguration config = ActiveCamera.Configuration;
            SelectionSetting setting = FindSetting<SelectionSetting>(IsoTitle, config);
            return (setting.Options, setting.Value);
        }


        /// <summary>
        /// Sets the ISO setting on the camera.
        /// </summary>
        /// <param name="Iso">The new ISO setting</param>
        public void SetIso(string Iso)
        {
            CameraConfiguration config = ActiveCamera.Configuration;
            SelectionSetting setting = FindSetting<SelectionSetting>(IsoTitle, config);
            setting.Value = Iso;
            ActiveCamera.UpdateConfiguration();
        }


        /// <summary>
        /// Gets the list of exposure settings the camera supports, along with the current setting.
        /// </summary>
        /// <returns>The camera's supported exposure settings and the current one</returns>
        public (IReadOnlyList<string> Options, string Current) GetExposureSettings()
        {
            CameraConfiguration config = ActiveCamera.Configuration;
            SelectionSetting setting = FindSetting<SelectionSetting>(ExposureTitle, config);
            return (setting.Options, setting.Value);
        }


        /// <summary>
        /// Sets the exposure setting on the camera.
        /// </summary>
        /// <param name="Exposure">The new exposure setting</param>
        public void SetExposure(string Exposure)
        {
            CameraConfiguration config = ActiveCamera.Configuration;
            SelectionSetting setting = FindSetting<SelectionSetting>(ExposureTitle, config);
            setting.Value = Exposure;
            ActiveCamera.UpdateConfiguration();
        }


        /// <summary>
        /// Gets the list of image format settings the camera supports, along with the current setting.
        /// </summary>
        /// <returns>The camera's supported image format settings and the current one</returns>
        public (IReadOnlyList<string> Options, string Current) GetImageFormatSettings()
        {
            CameraConfiguration config = ActiveCamera.Configuration;
            SelectionSetting setting = FindSetting<SelectionSetting>(ImageFormatTitle, config);
            return (setting.Options, setting.Value);
        }


        /// <summary>
        /// Sets the image format setting on the camera.
        /// </summary>
        /// <param name="ImageFormat">The new image format setting</param>
        public void SetImageFormat(string ImageFormat)
        {
            CameraConfiguration config = ActiveCamera.Configuration;
            SelectionSetting setting = FindSetting<SelectionSetting>(ImageFormatTitle, config);
            setting.Value = ImageFormat;
            ActiveCamera.UpdateConfiguration();
        }


        /// <summary>
        /// Gets the list of camera output settings the camera supports, along with the current setting.
        /// </summary>
        /// <returns>The camera's supported camera output settings and the current one</returns>
        public (IReadOnlyList<string> Options, string Current) GetCameraOutputSettings()
        {
            CameraConfiguration config = ActiveCamera.Configuration;
            SelectionSetting setting = FindSetting<SelectionSetting>(CameraOutputTitle, config);
            return (setting.Options, setting.Value);
        }


        /// <summary>
        /// Sets the camera output setting on the camera.
        /// </summary>
        /// <param name="CameraOutput">The new camera output setting</param>
        public void SetCameraOutput(string CameraOutput)
        {
            CameraConfiguration config = ActiveCamera.Configuration;
            SelectionSetting setting = FindSetting<SelectionSetting>(CameraOutputTitle, config);
            setting.Value = CameraOutput;
            ActiveCamera.UpdateConfiguration();
        }


        /// <summary>
        /// Captures an image from the active camera.
        /// </summary>
        /// <returns>The image's data and its name</returns>
        unsafe public (Bitmap ImageSource, string Filename) CaptureImage()
        {
            using (CameraFile file = ActiveCamera.Capture(CameraCaptureType.Image))
            using (UnmanagedMemoryStream stream = new UnmanagedMemoryStream((byte*)file.Data.ToPointer(), (long)file.Size))
            {
                Logger.Debug($"Image MIME: {file.MimeType}");
                Bitmap bitmap = new Bitmap(stream);
                return (bitmap, file.Name);
            }
        }


        /// <summary>
        /// Captures a preview image from the active camera.
        /// </summary>
        /// <returns>A preview image from the camera</returns>
        unsafe public Bitmap Preview()
        {
            using (CameraFile preview = ActiveCamera.Preview())
            using (UnmanagedMemoryStream stream = new UnmanagedMemoryStream((byte*)preview.Data.ToPointer(), (long)preview.Size))
            {
                Logger.Debug($"Preview MIME: {preview.MimeType}");
                Bitmap bitmap = new Bitmap(stream);
                return bitmap;
            }
        }


        /// <summary>
        /// Finds the setting in the given config with the provided title.
        /// </summary>
        /// <typeparam name="TSetting">The type of setting to find</typeparam>
        /// <param name="Title">The title of the desired setting</param>
        /// <param name="Configuration">The configuration to search</param>
        /// <returns>The setting if it was found, or null if it wasn't.</returns>
        private static TSetting FindSetting<TSetting>(string Title, CameraConfiguration Configuration)
            where TSetting : Setting
        {
            foreach(ConfigurationSection section in Configuration.Sections)
            {
                foreach(Setting setting in section.Settings)
                {
                    if(setting.Title == Title)
                    {
                        return (TSetting)setting;
                    }
                }
            }

            return null;
        }


        #region IDisposable Support
        private bool DisposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!DisposedValue)
            {
                if (disposing)
                {
                    ActiveCamera = null;
                    if(Cameras != null)
                    {
                        foreach (Camera camera in Cameras)
                        {
                            try
                            {
                                camera.Disconnect();
                                camera.Dispose();
                            }
                            catch (Exception) { }
                        }
                        Cameras = null;
                    }

                    Context.Dispose();
                }
                DisposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

    }
}
