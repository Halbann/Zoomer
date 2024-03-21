using System;
using System.Collections;

using UnityEngine;

using KSP.Game;
using KSP.Rendering;
using KSP.Sim;
using KSP.Input;

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
        public const string ModGuid = "Zoomer";
        public const string ModName = "Zoomer";
        public const string ModVer = "0.2.0";

        /// Singleton instance of the plugin class
        [PublicAPI]
        public static Zoomer Instance { get; set; }

        private static bool sceneValid = false;

        #region Fields

        // FOV.

        public double defaultFOV = 60f;

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

        // Pan.

        private GameObject _physicsCameraObject;
        public GameObject PhysicsCameraObject
        {
            get
            {
                if (_physicsCameraObject == null)
                {
                    var stack = Game.CameraManager
                        .GetCameraRenderStack(CameraID.Flight, RenderSpaceType.PhysicsSpace);

                    _physicsCameraObject = stack.GetMainRenderCamera().gameObject;
                }

                return _physicsCameraObject;
            }
        }

        private GameObject _scaledCameraObject;
        public GameObject ScaledCameraObject
        {
            get
            {
                if (_scaledCameraObject == null)
                {
                    var stack = Game.CameraManager
                        .GetCameraRenderStack(CameraID.Flight, RenderSpaceType.ScaleSpace);

                    _scaledCameraObject = stack.GetMainRenderCamera().gameObject;
                }

                return _scaledCameraObject;
            }
        }

        #endregion

        #region Main

        public override void OnInitialized()
        {
            Instance = this;

            SpaceWarp.API.Game.Messages.StateChanges.FlightViewEntered += m => sceneValid = true;
            SpaceWarp.API.Game.Messages.StateChanges.FlightViewLeft += m => sceneValid = false;
            SpaceWarp.API.Game.Messages.StateChanges.FlightViewLeft += m => ResetPan();


            // There is no event for toggling UI visibility, so we listen to events that can trigger it.

            GlobalInputDefinition globalInputDefinition;
            if (Game.InputManager.TryGetInputDefinition<GlobalInputDefinition>(out globalInputDefinition))
            {
                globalInputDefinition.BindAction(Game.Input.Global.TogglePauseMenu.name, new Action(OnToggleEscapeMenu));
                globalInputDefinition.BindAction(Game.Input.Global.ToggleUIVisibility.name, new Action(OnToggleUIVisibility));
            }
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

        internal void Update()
        {
            if (!CheckScene())
                return;

            ZoomUpdate();
            PanUpdate();
        }

        #endregion

        #region Functions

        private void ZoomUpdate()
        {
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
                    ResetFlightCamera();
                }
                else
                {
                    lastClicked = Time.realtimeSinceStartup;
                }
            }
        }

        private void PanUpdate()
        {
            // Panning
            if (!Input.GetMouseButton(2)) // Middle mouse button
                return;

            // Panning Axis and Speed controls
            float panSpeed = 0.25f;
            Vector3 pan = new Vector3(-Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X"), 0) * panSpeed;

            PanCamera(pan);
        }

        public void ResetFlightCamera()
        {
            ResetFOV();
            ResetPan();
            Game.CameraManager.FlightCamera.ActiveSolution.ResetGimbalAndCamera(true);
        }

        public void SetFOV(double fov)
        {
            GameManager.Instance.Game.CameraManager.FlightCamera.ActiveSolution.SetCameraFieldOfView(fov);
            //Logger.LogInfo($"Set FOV to {fov}");
        }

        public void ResetFOV()
        {
            FOV = defaultFOV;
            //Logger.LogInfo($"Reset FOV to {defaultFOV}");
        }

        public void PanCamera(Vector3 pan)
        {
            if (PhysicsCameraObject == null)
                return;

            // Get the current rotation 
            Quaternion currentRotation = PhysicsCameraObject.transform.localRotation;

            // Use pan vec to set the new rotation
            Quaternion newRotation = Quaternion.Euler(pan) * currentRotation;

            // Re-orient so that we don't change the roll of the camera.
            // This is subjective but it's how KSP1 does it and it means that camera doesn't
            // gradually induce more and more roll as you pan back and forth.
            // A given input will always result in the same final camera orientation.
            newRotation.eulerAngles = new Vector3(newRotation.eulerAngles.x, newRotation.eulerAngles.y, 0);

            // Set the new rotation to the cameras
            PhysicsCameraObject.transform.localRotation = newRotation;
            ScaledCameraObject.transform.localRotation = newRotation;
        }

        public void ResetPan()
        {
            // Reset the local rotations of flightCameraObject and scaledCameraObject
            if (PhysicsCameraObject != null && ScaledCameraObject != null)
            {
                PhysicsCameraObject.transform.localRotation = Quaternion.identity;
                ScaledCameraObject.transform.localRotation = Quaternion.identity;
            }
        }

        #endregion

        #region Outlines

        private void OnToggleEscapeMenu() =>
            StartCoroutine(OnUIVisibilityChange());

        private void OnToggleUIVisibility() =>
            StartCoroutine(OnUIVisibilityChange());


        private IEnumerator OnUIVisibilityChange()
        {
            yield return null;

            var viewController = GameManager.Instance.Game.UI.ViewController;

            if (viewController.CurrentView == UIStateViews.UIHiddenView)
            {
                Debug.Log("UI is hidden");
                ToggleOutlineVisibility(false);
            }
            else
            {
                Debug.Log("UI is not hidden");
                ToggleOutlineVisibility(true);
            }
        }

        public void ToggleOutlineVisibility(bool visible)
        {
            // Hide the green outline around parts when the UI is hidden.

            if (PhysicsCameraObject == null)
                return;

            var outliner = PhysicsCameraObject.GetComponent<KSP.OutlineEffect>();
            if (outliner == null)
                return;

            outliner.enabled = visible;
        }

        #endregion
    }
}
