using System;
using System.Collections.Generic;
using System.IO;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;
using UnityEngine.InputSystem;


[assembly: MelonInfo(typeof(FlyoutFlightRecorder.Core), "FlyoutFlightRecorder", "1.0.0", "HerrTom", null)]
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

        public override void OnUpdate()
        {
            // Check if the toggle action was performed this frame
            if (toggleRecordingAction.triggered)
            {
                isRecordingEnabled = !isRecordingEnabled;
                MelonLogger.Msg($"Recording {(isRecordingEnabled ? "enabled" : "disabled")}.");

                if (isRecordingEnabled)
                {
                    // Create a new file with a timestamp in the filename
                    string timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH-mm-ss");
                    filePath = Path.Combine(MelonEnvironment.UserDataDirectory, $"FlightData_{timestamp}.csv");
                    headersWritten = false; // Reset headers flag for new file
                }
                else
                {
                    // Hide the ChannelPanel when recording stops
                    var flightUI = UnityEngine.Object.FindObjectOfType<Il2Cpp.FlightUI>();
                    if (flightUI != null)
                    {
                        var channelObject = flightUI.transform.Find("ChannelPanel");
                        if (channelObject.gameObject.activeSelf)
                        {
                            MelonLogger.Msg("Recording stopped. Hiding ChannelPanel.");
                            channelObject.gameObject.SetActive(false); // Hide the panel
                        }
                    }
                }
            }

            // Only record data if recording is enabled
            if (isRecordingEnabled)
            {
                // Get the FlightUI object
                var flightUI = UnityEngine.Object.FindObjectOfType<Il2Cpp.FlightUI>();
                if (flightUI == null) return; // Make sure it exists

                // Check if the ChannelPanel is active and force it to be active if it's not
                var channelObject = flightUI.transform.Find("ChannelPanel");
                if (!channelObject.gameObject.activeSelf)
                {
                    MelonLogger.Msg("ChannelPanel is inactive. Forcing it to active.");
                    channelObject.gameObject.SetActive(true); // Force the panel to be active
                }

                RecordData(); // Record the data
            }

            base.OnUpdate();
        }

        private void RecordData()
        {
            try
            {
                // Get the FlightUI object
                var flightUI = UnityEngine.Object.FindObjectOfType<Il2Cpp.FlightUI>();
                if (flightUI == null) return; // Make sure it exists

                // Get the channel panel text
                var channelContent = flightUI.transform.Find("ChannelPanel/Content");

                var channelComponent = channelContent.GetComponent<Il2CppTMPro.TextMeshProUGUI>();
                var channelText = channelComponent.text;

                // Split the text into lines
                string[] lines = channelText.Split('\n');

                // Verify that the extracted text has the expected format
                if (lines.Length < 2)
                {
                    return; // Skip further processing if the text is not in the expected format
                }

                // Create lists to hold the keys (headers) and values
                List<string> keys = new List<string> { };
                List<string> values = new List<string> { };

                // Parse each line to extract the key and value
                foreach (string line in lines)
                {
                    var parts = line.Split(new[] { ':' }, 2); // Split at the first colon
                    if (parts.Length == 2)
                    {
                        keys.Add(parts[0].Trim()); // Add key (column name)
                        values.Add(parts[1].Trim()); // Add value
                    }
                }

                // Write headers only once
                if (!headersWritten)
                {
                    File.AppendAllText(filePath, string.Join(",", keys) + "\n"); // Write header
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
