using System;
using System.Linq;

using UnityEngine;
using KSP.Sim.ResourceSystem;
using KSP.Game;

using BepInEx;
using SpaceWarp;
using SpaceWarp.API.Mods;
using JetBrains.Annotations;

namespace Zoomer
{
    [BepInDependency(SpaceWarpPlugin.ModGuid,SpaceWarpPlugin.ModVer)]
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

        // GUI.
        //private static GameState[] validScenes = new[] { GameState.FlightView, GameState.Map3DView };
        private static GameState[] validScenes = new[] { GameState.FlightView };
        private static bool ValidScene => validScenes.Contains(GameManager.Instance.Game.GlobalGameState.GetState());

        // FOV.

        public float defaultFOV = 60f;
        float _fov = 60f;
        public float FOV
        {
            get => _fov;
            set
            {
                if (_fov != value)
                {
                    _fov = Mathf.Clamp(value, 5, 100);
                    SetFOV(_fov);
                }
            }
        }

        private float lastClicked = Time.time;

        #endregion

        #region Main

        public override void OnInitialized()
        {
            Instance = this;
        }

        private bool CheckScene()
        {
            // Checks to make sure we're in a valid scene.

            bool previous = sceneValid;
            sceneValid = ValidScene;

            if (sceneValid != previous && !sceneValid)
            {
                ResetFOV();
            }

            return sceneValid;
        }

        void Update()
        {
            if (!CheckScene())
                return;

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
                // Keep track of double-clicks to reset FOV.

                if (Time.realtimeSinceStartup - lastClicked < 0.33f)
                {
                    ResetFOV();
                    Logger.LogInfo($"Reset FOV to {defaultFOV}");

                    GameManager.Instance.Game.CameraManager.FlightCamera.ActiveSolution.ResetGimbalAndCamera(true);
                }
                else
                {
                    lastClicked = Time.realtimeSinceStartup;
                }
            }
        }

        #endregion

        #region Functions

        void SetFOV(float fov)
        {
            GameManager.Instance.Game.CameraManager.FlightCamera.ActiveSolution.SetCameraFieldOfView(fov);
        }

        void ResetFOV()
        {
            FOV = defaultFOV;
        }

        #endregion
    }
}