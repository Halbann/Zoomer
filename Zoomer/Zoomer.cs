using BepInEx;
using BepInEx.Logging;
using JetBrains.Annotations;
using KSP.Game;
using KSP.Messages;
using SpaceWarp;
using SpaceWarp.API.Mods;
using System;
using UnityEngine;

namespace Zoomer
{
    [BepInDependency(SpaceWarpPlugin.ModGuid, SpaceWarpPlugin.ModVer)]
    [BepInPlugin(ModGuid, ModName, ModVer)]
    public class Zoomer : BaseSpaceWarpPlugin
    {
        public const string ModGuid = "com.github.halbann.zoomer";
        public const string ModName = "Zoomer";
        public const string ModVer = MyPluginInfo.PLUGIN_VERSION;

        /// Singleton instance of the plugin class
        [PublicAPI]
        public static Zoomer Instance { get; set; }

        private static bool sceneValid = false;

        #region Fields

        // FOV.

        public double defaultFOV = 60f;
        public GameState CurrentGameState => (GameState)(GameManager.Instance?.Game?.GlobalGameState?.GetGameState().GameState);

        public double FOV
        {
            get => Game.CameraManager.FlightCamera.Shot.FieldOfView;
            set
            {
                if (FOV != value)
                {
                    double fov = Math.Min(Math.Max(value, 5), 100);
                    SetFOV(fov);
                }
            }
        }

        private float lastClicked = Time.time;

        #endregion

        #region Main

        public override void OnInitialized()
        {
            Instance = this;

            SpaceWarp.API.Game.Messages.StateChanges.FlightViewEntered += m => sceneValid = true;
            SpaceWarp.API.Game.Messages.StateChanges.FlightViewLeft += m => sceneValid = false;
        }
        private bool CheckScene()
        {
            // Checks to make sure we're in a valid scene.

            bool previous = sceneValid;

            if (sceneValid != previous && !sceneValid)
            {
                ResetFOV();
                ResetPan();
            }

            return sceneValid;
        }

        void Update()
        {
            if (!CheckScene())
                return;

            SpaceWarp.API.Game.Messages.StateChanges.FlightViewEntered += m => sceneValid = true;
            SpaceWarp.API.Game.Messages.StateChanges.FlightViewLeft += m => sceneValid = false;

            // Find the Flight Camera in the scene for camera panning
            GameObject flightCameraObject = GameObject.Find("FlightCameraPhysics_Main");
            GameObject scaledCameraObject = GameObject.Find("FlightCameraScaled_Main");

            // Disable the game's distance adjustment when the zoom adjustment key is held.
            if (Input.GetKeyDown(KeyCode.LeftAlt))
            {
                Game.Input.Flight.CameraZoom.Disable();
            }

            if (Input.GetKey(KeyCode.LeftAlt))
            {
                // Adjust FOV with mouse wheel when zoom adjustment key is held.

                FOV += Input.GetAxis("Mouse ScrollWheel") * 10f;
            }
            else if (!Game.Input.Flight.CameraZoom.enabled)
            {
                // Re-enable mouse wheel on exiting zoom adjustment.

                Game.Input.Flight.CameraZoom.Enable();
            }
            else if (Input.GetMouseButtonDown(2)) // 2 corresponds to the middle mouse button
            {
                // Keep track of double-clicks to reset Camera.

                if (Time.realtimeSinceStartup - lastClicked < 0.33f)
                {
                    ResetFOV();
                    Game.CameraManager.FlightCamera.ActiveSolution.ResetGimbalAndCamera(true);
                    ResetPan();
                }
                else
                {
                    lastClicked = Time.realtimeSinceStartup;
                }
            }
            if (Input.GetMouseButton(2)) // Middle mouse button
            {
                // Panning Axis and Speed controls
                float panSpeed = 0.25f;
                Vector3 pan = new Vector3(-Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X"), 0) * panSpeed;


                if (flightCameraObject != null)
                {
                    // Get the current rotation 
                    Quaternion currentRotation = flightCameraObject.transform.localRotation;

                    // Use pan vec to set the new rotation
                    Quaternion newRotation = Quaternion.Euler(pan) * currentRotation;

                    // Set the new rotation to the cameras
                    flightCameraObject.transform.localRotation = newRotation;
                    scaledCameraObject.transform.localRotation = newRotation;
                }
                Game.Messages.PersistentSubscribe<GameStateEnteredMessage>(msg =>
                {
                    var message = msg as GameStateEnteredMessage;
                    switch (message != null ? message.StateBeingEntered : default)
                    {
                        case GameState.KerbalSpaceCenter:
                            ResetPan();
                            break;
                    }
                });
            }
        }


        #endregion

        #region Functions

        void SetFOV(double fov)
        {
            GameManager.Instance.Game.CameraManager.FlightCamera.ActiveSolution.SetCameraFieldOfView(fov);
            //Logger.LogInfo($"Set FOV to {fov}");
        }

        void ResetFOV()
        {
            FOV = defaultFOV;
            //Logger.LogInfo($"Reset FOV to {defaultFOV}");
        }
        void ResetPan()
        {
            // Find the Flight Camera in the scene for camera panning
            GameObject flightCameraObject = GameObject.Find("FlightCameraPhysics_Main");
            GameObject scaledCameraObject = GameObject.Find("FlightCameraScaled_Main");

            // Reset the local rotations of flightCameraObject and scaledCameraObject
            if (flightCameraObject != null && scaledCameraObject != null)
            {
                flightCameraObject.transform.localRotation = Quaternion.identity;
                scaledCameraObject.transform.localRotation = Quaternion.identity;
            }
        }
        #endregion
    }
}