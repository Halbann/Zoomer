using System;
using System.Linq;

using UnityEngine;
using KSP.Sim.ResourceSystem;
using KSP.Game;

using BepInEx;
using SpaceWarp;
using SpaceWarp.API.Mods;

namespace Zoomer
{
    [BepInDependency(SpaceWarpPlugin.ModGuid,SpaceWarpPlugin.ModVer)]
    [BepInPlugin(ModGuid, ModName, ModVer)]
    public class Zoomer : BaseSpaceWarpPlugin
    {
        public const string ModGuid = "com.github.halbann.zoomer";
        public const string ModName = "Zoomer";
        public const string ModVer = MyPluginInfo.PLUGIN_VERSION;

        #region Fields

        // GUI.
        private static GameState[] validScenes = new[] { GameState.FlightView, GameState.Map3DView };
        private static bool ValidScene => validScenes.Contains(GameManager.Instance.Game.GlobalGameState.GetState());

        // FOV.

        float defaultFOV = 60f;
        float _fov = 60f;
        float FOV
        {
            get => _fov;
            set
            {
                if (_fov != value)
                {
                    _fov = Mathf.Clamp(value, 5, 90);
                    SetFOV(_fov);
                }
            }
        }

        float lastClicked = Time.time;

        #endregion

        #region Main

        public override void OnInitialized()
        {
            gameObject.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(gameObject);
        }

        void Update()
        {
            // Disable mouse wheel on entering zoom adjustment.
            if (Input.GetKeyDown(KeyCode.LeftAlt))
            {
                SessionManager.AXIS_MOUSEWHEEL.axisBinding.primary.scale = 0;
            }

            if (Input.GetKey(KeyCode.LeftAlt))
            {
                FOV += Input.GetAxis("Mouse ScrollWheel") * 5f;
            }
            else if (SessionManager.AXIS_MOUSEWHEEL.axisBinding.primary.scale == 0)
            {
                // Enable mouse wheel on exiting zoom adjustment.
                SessionManager.AXIS_MOUSEWHEEL.axisBinding.primary.scale = 1;
            }

            if (Input.GetKeyDown(KeyCode.Mouse2))
            {
                if (Time.realtimeSinceStartup - lastClicked < 0.33f)
                {
                    //FOV = defaultFOV;
                    _fov = defaultFOV;
                    Logger.LogInfo($"Reset FOV to {defaultFOV}");

                    SessionManager.MOUSE_MIDDLE.keyBinding.primary = null;

                    GameManager.Instance.Game.CameraManager.FlightCamera.ActiveSolution.ResetGimbalAndCamera(true);
                    GameManager.Instance.Game.CameraManager.FlightCamera.ResetCameraTweakables();
                    GameManager.Instance.Game.CameraManager.FlightCamera.ActiveSolution.RefreshShot();
                }
                else
                    lastClicked = Time.realtimeSinceStartup;
            }
        }

        #endregion

        #region Functions
        void SetFOV(float fov)
        {
            GameManager.Instance.Game.CameraManager.FlightCamera.ActiveSolution.SetCameraFieldOfView(fov);
        }

        #endregion
    }
}