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

namespace Sedna
{
    public class MotorControlWindow : Window
    {
        private readonly TextBlock ShaftPositionBox;
        private readonly TextBlock CoilAStalledBox;
        private readonly TextBlock CoilBStalledBox;
        private readonly TextBlock OvercurrentBox;
        private readonly TextBlock ThermalShutdownBox;
        private readonly TextBlock ThermalWarningBox;
        private readonly TextBlock UndervoltageBox;
        private readonly TextBlock UnknownCommandBox;
        private readonly TextBlock LastCommandFailedBox;
        private readonly TextBlock MotorStateBox;
        private readonly TextBlock DirectionBox;
        private readonly TextBlock KillSwitchTriggerBox;
        private readonly TextBlock KillSwitchActiveBox;
        private readonly TextBlock IsBusyBox;
        private readonly TextBlock HiZBox;
        private readonly Slider RpmSlider;
        private readonly TextBlock RpmBox;


        private Amt22 Encoder;

        private L6470 Driver;

        public MotorControlWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            ShaftPositionBox = this.FindControl<TextBlock>("ShaftPositionBox");
            CoilAStalledBox = this.FindControl<TextBlock>("CoilAStalledBox");
            CoilBStalledBox = this.FindControl<TextBlock>("CoilBStalledBox");
            OvercurrentBox = this.FindControl<TextBlock>("OvercurrentBox");
            ThermalShutdownBox = this.FindControl<TextBlock>("ThermalShutdownBox");
            ThermalWarningBox = this.FindControl<TextBlock>("ThermalWarningBox");
            UndervoltageBox = this.FindControl<TextBlock>("UndervoltageBox");
            UnknownCommandBox = this.FindControl<TextBlock>("UnknownCommandBox");
            LastCommandFailedBox = this.FindControl<TextBlock>("LastCommandFailedBox");
            MotorStateBox = this.FindControl<TextBlock>("MotorStateBox");
            DirectionBox = this.FindControl<TextBlock>("DirectionBox");
            KillSwitchTriggerBox = this.FindControl<TextBlock>("KillSwitchTriggerBox");
            KillSwitchActiveBox = this.FindControl<TextBlock>("KillSwitchActiveBox");
            IsBusyBox = this.FindControl<TextBlock>("IsBusyBox");
            HiZBox = this.FindControl<TextBlock>("HiZBox");
            RpmSlider = this.FindControl<Slider>("RpmSlider");
            RpmBox = this.FindControl<TextBlock>("RpmBox");
        }

        public void SetHardware(Amt22 Encoder, L6470 Driver)
        {
            this.Encoder = Encoder;
            this.Driver = Driver;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void RefreshStatus_Click(object sender, RoutedEventArgs e)
        {
            ushort position = Encoder.GetPosition();
            ShaftPositionBox.Text = position.ToString();

            L6470Status status = Driver.GetStatus();
            CoilAStalledBox.Text = (status.BridgeAStalled ? "YES" : "");
            CoilBStalledBox.Text = (status.BridgeBStalled ? "YES" : "");
            OvercurrentBox.Text = (status.OvercurrentDetected ? "YES" : "");
            ThermalShutdownBox.Text = (status.ThermalShutdownTriggered ? "YES" : "");
            ThermalWarningBox.Text = (status.ThermalWarningTriggered ? "YES" : "");
            UndervoltageBox.Text = (status.UndervoltageDetected ? "YES" : "");
            UnknownCommandBox.Text = (status.ReceivedUnknownCommand ? "YES" : "");
            LastCommandFailedBox.Text = (status.LastCommandFailed ? "YES" : "");
            MotorStateBox.Text = status.MotorState.ToString();
            DirectionBox.Text = status.Direction.ToString();
            KillSwitchTriggerBox.Text = (status.KillSwitchTriggered ? "YES" : "");
            KillSwitchActiveBox.Text = (status.KillSwitchActive ? "YES" : "");
            IsBusyBox.Text = (status.IsBusy ? "YES" : "");
            HiZBox.Text = (status.BridgesActive ? "YES" : "");
        }

        public void ForwardRadio_Checked(object sender, RoutedEventArgs e)
        {
            Driver.Direction = MotorDirection.Forward;
        }

        public void ReverseRadio_Checked(object sender, RoutedEventArgs e)
        {
            Driver.Direction = MotorDirection.Reverse;
        }

        public void RpmSlider_PropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property.Name != "Value")
            {
                return;
            }

            double rpm = (double)e.NewValue;
            RpmBox.Text = $"{rpm.ToString("N2")} RPM";
        }

        public void RunButton_Click(object sender, RoutedEventArgs e)
        {
            Driver.Run();
        }

        public void SoftStopButton_Click(object sender, RoutedEventArgs e)
        {
            Driver.SoftStop();
        }

        public void HardStopButton_Click(object sender, RoutedEventArgs e)
        {
            Driver.HardStop();
        }

        public void SoftHiZButton_Click(object sender, RoutedEventArgs e)
        {
            Driver.SoftHiZ();
        }

        public void AbortButton_Click(object sender, RoutedEventArgs e)
        {
            Driver.HardHiZ();
        }

    }
}
