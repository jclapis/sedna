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
        /// Captures an image from the active camera.
        /// </summary>
        /// <returns>The image's data and its name</returns>
        public (Bitmap ImageSource, string Filename) CaptureImage()
        {
            CameraFile file = ActiveCamera.Capture(CameraCaptureType.Image);

            using(MemoryStream stream = new MemoryStream(file.Data))
            {
                Bitmap bitmap = new Bitmap(stream);
                return (bitmap, file.Name);
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
