using System;
using System.Collections.Generic;
using System.IO;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;
using UnityEngine.InputSystem;


[assembly: MelonInfo(typeof(FlyoutFlightRecorder.Core), "FlyoutFlightRecorder", "1.1.0", "HerrTom", null)]
[assembly: MelonGame("Stonext Games", "Flyout")]

namespace FlyoutFlightRecorder
{
    public class Core : MelonPlugin
    {
        private bool isRecordingEnabled = false; // Toggle for recording
        private string filePath;
        private bool headersWritten = false;

        // Define the input action for toggling recording
        private InputAction toggleRecordingAction;

        public override void OnInitializeMelon()
        {
            // Define the default key in case the CFG file doesn't exist or is incomplete
            string toggleRecordingKey = "<Keyboard>/numpadMultiply";

            // Load the keybinding from the config file
            string configFilePath = Path.Combine(MelonEnvironment.UserDataDirectory, "FlyoutFlightRecorder.cfg");

            if (File.Exists(configFilePath))
            {
                try
                {
                    // Read the config file
                    var lines = File.ReadAllLines(configFilePath);
                    foreach (var line in lines)
                    {
                        // Look for the toggle key in the [Keybindings] section
                        if (line.StartsWith("ToggleRecordingKey"))
                        {
                            toggleRecordingKey = line.Split('=')[1].Trim();
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MelonLogger.Error("Error reading config file: " + ex.Message);
                }
            }
            else
            {
                // If the config file doesn't exist, create it with default values
                File.WriteAllText(configFilePath, "[Keybindings]\nToggleRecordingKey = <Keyboard>/numpadMultiply\n");
                MelonLogger.Msg("Config file not found. Creating a new one with default keybinding.");
            }

            // Initialize the input action with the keybinding from the config file
            toggleRecordingAction = new InputAction(binding: toggleRecordingKey);
            toggleRecordingAction.Enable(); // Enable the action to listen for input

            // Log that the initialization is completed!
            MelonLogger.Msg($"FlightUI Recorder started. Press '{toggleRecordingKey}' to toggle recording on or off.");
            base.OnInitializeMelon();
        }
        private void ToggleRecording()
        {
            // Toggle recording if the Craft and Flight objects are valid
            isRecordingEnabled = !isRecordingEnabled;
            MelonLogger.Msg($"Recording {(isRecordingEnabled ? "enabled" : "disabled")}.");
            if (isRecordingEnabled)
            {

                // Attempt to grab the Craft and Flight objects
                var craft = UnityEngine.Object.FindObjectOfType<Il2Cpp.Craft>();

                if (craft != null && craft.flight != null)
                {
                    // Create a new file with a timestamp in the filename
                    string timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH-mm-ss");
                    filePath = Path.Combine(MelonEnvironment.UserDataDirectory, $"FlightData_{timestamp}.csv");
                    headersWritten = false; // Reset headers flag for new file

                }
                else
                {
                    MelonLogger.Warning("Unable to enable recording: Craft and Flight object not found.");
                }
            }
        }
        public override void OnLateUpdate()
        {
            // Check if the toggle action was performed this frame
                if (toggleRecordingAction.WasPerformedThisFrame())
            {
                ToggleRecording();
            }
            base.OnLateUpdate();
        }
        public override void OnFixedUpdate()
        {
            // Only record data if recording is enabled
            if (isRecordingEnabled)
            {
                RecordData();
            }

            base.OnFixedUpdate();
        }

        public override void OnGUI()
        {
            if (isRecordingEnabled)
            {
                // Create a new GUI style for the recording label
                GUIStyle style = new GUIStyle();
                style.fontSize = 24; // Set the font size
                style.normal.textColor = Color.red; // Set the text color to red

                // Draw the label in the top-left corner
                GUI.Label(new Rect(10, 10, 200, 50), "RECORDING", style);
            }
            base.OnGUI();
        }

        private void RecordData()
        {
            try
            {
                // Grab data directly from the craft
                var craft = UnityEngine.Object.FindObjectOfType < Il2Cpp.Craft >();
                var flight = craft.flight;

                // Get the thrust, drag, and lift
                var thrust = craft.ThrustForce.magnitude;
                var drag = craft.DragForce.magnitude;
                var lift = craft.LiftForce.magnitude;
                var refSize = craft.refSize;

                // Create lists to hold the keys (headers) and values
                List<string> keys = new List<string> { };
                List<float> values = new List<float> { };

                // Get data pairs
                keys.Add("Time"); // Column
                values.Add(flight.time); // Value

                keys.Add("Altitude"); // Column
                values.Add(flight.altitude); // Value

                keys.Add("Airspeed"); // Column
                values.Add(flight.airspeed); // Value

                keys.Add("G"); // Column
                values.Add(flight.g); // Value

                keys.Add("Mach"); // Column
                values.Add(flight.mach); // Value

                keys.Add("Turn Rate"); // Column
                values.Add(flight.turnRate); // Value

                keys.Add("Roll Rate"); // Column
                values.Add(flight.rollRate); // Value

                keys.Add("Mass"); // Column
                values.Add(craft.flightData.mass); // Value

                keys.Add("Thrust"); // Column
                values.Add(thrust); // Value

                keys.Add("Drag"); // Column
                values.Add(drag); // Value

                keys.Add("Lift"); // Column
                values.Add(lift); // Value

                keys.Add("Ref Area"); // Column
                values.Add(refSize); // Value

                keys.Add("Dynamic Pressure"); // Column
                values.Add(craft.dynamicPressure); // Value

                keys.Add("Alpha"); // Column
                values.Add(craft.Command.alpha); // Value

                keys.Add("Lift Coeff"); // Column
                values.Add(lift / (craft.dynamicPressure * refSize)); // Value

                keys.Add("Drag Coeff"); // Column
                values.Add(drag / (craft.dynamicPressure * refSize)); // Value

                // Write headers only once
                if (!headersWritten)
                {
                    File.AppendAllText(filePath, string.Join(",", keys) + "\n");
                    headersWritten = true;
                }

                // Write the values (append each row)
                File.AppendAllText(filePath, string.Join(",", values) + "\n");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("Error recording data: " + ex.Message);
            }
        }
    }
}
