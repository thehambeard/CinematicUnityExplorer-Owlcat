using System.Collections.Generic;

using HarmonyLib;
using UnityEngine;
using UnityExplorer;
using UnityExplorer.UI;
using UnityExplorer.UI.Panels;
using UniverseLib.Input;
#if UNHOLLOWER
using IL2CPPUtils = UnhollowerBaseLib.UnhollowerUtils;
#endif
#if INTEROP
using IL2CPPUtils = Il2CppInterop.Common.Il2CppInteropUtils;
#endif

namespace UniverseLib.Input
{
    public static class IInputManager {
        // TODO: Refactor this to have the input system used on the game saved in a variable,
        // and just call the methods of this variable in each function, like a proper wrapper.
        private static InputType currentInputType;

        public static void Setup(){
            currentInputType = InputType.InputSystem;

            switch (currentInputType){
                case InputType.Legacy:
                    ILegacyInput.Init();
                    break;
                case InputType.InputSystem:
                case InputType.None:
                    break;
            }
        }

        public static bool GetKey(KeyCode key){
            switch(currentInputType){
                case InputType.Legacy:
                    return ILegacyInput.GetKey(key);
                case InputType.InputSystem:
                case InputType.None:
                default:
                    return InputManager.GetKey(key);
            }
        }

        public static bool GetKeyDown(KeyCode key){
            switch(currentInputType){
                case InputType.Legacy:
                    return ILegacyInput.GetKeyDown(key);
                case InputType.InputSystem:
                case InputType.None:
                default:
                    return InputManager.GetKeyDown(key);
            }
        }

        public static bool GetKeyUp(KeyCode key){
            switch(currentInputType){
                case InputType.Legacy:
                    return ILegacyInput.GetKeyUp(key);
                case InputType.InputSystem:
                case InputType.None:
                default:
                    return InputManager.GetKeyUp(key);
            }
        }

        public static bool GetMouseButton(int button){
            switch(currentInputType){
                case InputType.Legacy:
                    return ILegacyInput.GetMouseButton(button);
                case InputType.InputSystem:
                case InputType.None:
                default:
                    return InputManager.GetMouseButton(button);
            }
        }

        public static bool GetMouseButtonDown(int button){
            switch(currentInputType){
                case InputType.Legacy:
                    return ILegacyInput.GetMouseButtonDown(button);
                case InputType.InputSystem:
                case InputType.None:
                default:
                    return InputManager.GetMouseButtonDown(button);
            }
        }

        // We won't mock this as it made it impossible to drag the mod panels across the screen in some games for some reason.
        // It might affect some games that use custom classes to control the camera, but those would still probably need to be
        // manually disabled because of the camera position control override. Should try it out with more games.
        public static Vector3 MousePosition => (Vector3)InputManager.MousePosition;
    }

    public static class ILegacyInput {
        public static Dictionary<KeyCode, bool> getKeyDict = new Dictionary<KeyCode, bool>();
        public static Dictionary<KeyCode, bool> getKeyDownDict = new Dictionary<KeyCode, bool>();
        public static Dictionary<KeyCode, bool> getKeyUpDict = new Dictionary<KeyCode, bool>();

        public static Dictionary<int, bool> getMouseButton = new Dictionary<int, bool>();
        public static Dictionary<int, bool> getMouseButtonDown = new Dictionary<int, bool>();
        
        // Wrapped methods

        public static bool GetKey(KeyCode key){
            if (key == KeyCode.None) return false;
            // Trigger the original InputManager method
            InputManager.GetKey(key);
            return getKeyDict[key];
        }

        public static bool GetKeyDown(KeyCode key){
            if (key == KeyCode.None) return false;
            // Trigger the original InputManager method
            InputManager.GetKeyDown(key);
            return getKeyDownDict[key];
        }

        public static bool GetKeyUp(KeyCode key){
            if (key == KeyCode.None) return false;
            // Trigger the original InputManager method
            InputManager.GetKeyUp(key);
            return getKeyUpDict[key];
        }

        public static bool GetMouseButton(int button){
            // Trigger the original InputManager method
            InputManager.GetMouseButton(button);
            return getMouseButton[button];
        }

        public static bool GetMouseButtonDown(int button){
            // Trigger the original InputManager method
            InputManager.GetMouseButtonDown(button);
            return getMouseButtonDown[button];
        }

        // Patch the input methods of the legacy input system
        public static void Init(){
            Type t_Input = ReflectionUtility.GetTypeByName("UnityEngine.Input");

            try
            {
                MethodInfo getKeyTarget = t_Input.GetMethod("GetKey", new Type[] {typeof(string)});
                //ExplorerCore.LogWarning(getKeyTarget);
#if CPP
                if (IL2CPPUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(getKeyTarget) == null)
                    throw new Exception();
#endif
                ExplorerCore.Harmony.Patch(getKeyTarget,
                    postfix: new(AccessTools.Method(typeof(ILegacyInput), nameof(OverrideKeyString))));
            }
            catch { }

            try
            {
                MethodInfo getKeyTarget = t_Input.GetMethod("GetKey", new Type[] {typeof(KeyCode)});
                //ExplorerCore.LogWarning(getKeyTarget);
#if CPP
                if (IL2CPPUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(getKeyTarget) == null)
                    throw new Exception();
#endif
                ExplorerCore.Harmony.Patch(getKeyTarget,
                    postfix: new(AccessTools.Method(typeof(ILegacyInput), nameof(OverrideKeyKeyCode))));
            }
            catch {  }

            try
            {
                MethodInfo getKeyDownTarget = t_Input.GetMethod("GetKeyDown", new Type[] {typeof(string)});
                //ExplorerCore.LogWarning(getKeyDownTarget);
#if CPP
                if (IL2CPPUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(getKeyDownTarget) == null)
                    throw new Exception();
#endif
                ExplorerCore.Harmony.Patch(getKeyDownTarget,
                    postfix: new(AccessTools.Method(typeof(ILegacyInput), nameof(OverrideKeyDownString))));
            }
            catch {  }

            try
            {
                MethodInfo getKeyDownTarget = t_Input.GetMethod("GetKeyDown", new Type[] {typeof(KeyCode)});
                //ExplorerCore.LogWarning(getKeyDownTarget);
#if CPP
                if (IL2CPPUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(getKeyDownTarget) == null)
                    throw new Exception();
#endif
                ExplorerCore.Harmony.Patch(getKeyDownTarget,
                    postfix: new(AccessTools.Method(typeof(ILegacyInput), nameof(OverrideKeyDownKeyCode))));
            }
            catch {  }

            try
            {
                MethodInfo getKeyUpTarget = t_Input.GetMethod("GetKeyUp", new Type[] {typeof(string)});
                //ExplorerCore.LogWarning(getKeyUpTarget);
#if CPP
                if (IL2CPPUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(getKeyUpTarget) == null)
                    throw new Exception();
#endif
                ExplorerCore.Harmony.Patch(getKeyUpTarget,
                    postfix: new(AccessTools.Method(typeof(ILegacyInput), nameof(OverrideKeyUpString))));
            }
            catch {  }

            try
            {
                MethodInfo getKeyUpTarget = t_Input.GetMethod("GetKeyUp", new Type[] {typeof(KeyCode)});
                //ExplorerCore.LogWarning(getKeyUpTarget);
#if CPP
                if (IL2CPPUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(getKeyUpTarget) == null)
                    throw new Exception();
#endif
                ExplorerCore.Harmony.Patch(getKeyUpTarget,
                    postfix: new(AccessTools.Method(typeof(ILegacyInput), nameof(OverrideKeyUpKeyCode))));
            }
            catch {  }

            try
            {
                MethodInfo getMouseButtonTarget = t_Input.GetMethod("GetMouseButton", new Type[] {typeof(int)});
                //ExplorerCore.LogWarning(getMouseButtonTarget);
#if CPP
                if (IL2CPPUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(getMouseButtonTarget) == null)
                    throw new Exception();
#endif
                ExplorerCore.Harmony.Patch(getMouseButtonTarget,
                    postfix: new(AccessTools.Method(typeof(ILegacyInput), nameof(OverrideMouseButton))));
            }
            catch {  }

            try
            {
                MethodInfo getMouseButtonDownTarget = t_Input.GetMethod("GetMouseButtonDown", new Type[] {typeof(int)});
                //ExplorerCore.LogWarning(getMouseButtonDownTarget);
#if CPP
                if (IL2CPPUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(getMouseButtonDownTarget) == null)
                    throw new Exception();
#endif
                ExplorerCore.Harmony.Patch(getMouseButtonDownTarget,
                    postfix: new(AccessTools.Method(typeof(ILegacyInput), nameof(OverrideMouseButtonDown))));
            }
            catch {  }
        }

        // Postfix functions

        public static void OverrideKeyString(ref bool __result, ref string name)
        {
            KeyCode thisKeyCode = (KeyCode) System.Enum.Parse(typeof(KeyCode), name);
            getKeyDict[thisKeyCode] = __result;
            if (FreeCamPanel.ShouldOverrideInput()){
                __result = false;
            }
        }

        public static void OverrideKeyKeyCode(ref bool __result, ref KeyCode key)
        {
            if (key == KeyCode.None) return;

            getKeyDict[key] = __result;
            if (FreeCamPanel.ShouldOverrideInput()){
                __result = false;
            }
        }

        public static void OverrideKeyDownString(ref bool __result, ref string name)
        {
            KeyCode thisKeyCode = (KeyCode) System.Enum.Parse(typeof(KeyCode), name);
            getKeyDownDict[thisKeyCode] = __result;
            if (FreeCamPanel.ShouldOverrideInput()){
                __result = false;
            }
        }

        public static void OverrideKeyDownKeyCode(ref bool __result, ref KeyCode key)
        {
            if (key == KeyCode.None) return;

            getKeyDownDict[key] = __result;
            if (FreeCamPanel.ShouldOverrideInput()){
                __result = false;
            }
        }

        public static void OverrideKeyUpString(ref bool __result, ref string name)
        {
            KeyCode thisKeyCode = (KeyCode) System.Enum.Parse(typeof(KeyCode), name);
            getKeyUpDict[thisKeyCode] = __result;
            if (FreeCamPanel.ShouldOverrideInput()){
                __result = false;
            }
        }

        public static void OverrideKeyUpKeyCode(ref bool __result, ref KeyCode key)
        {
            if (key == KeyCode.None) return;

            getKeyUpDict[key] = __result;
            if (FreeCamPanel.ShouldOverrideInput()){
                __result = false;
            }
        }

        public static void OverrideMouseButton(ref bool __result, ref int button)
        {
            getMouseButton[button] = __result;
            // Since CinematicUnityExplorer uses Unity's native UI for its menu, we can't switch off the mouse interaction with it on this wrapper.
            // Therefore, if we still want to interact with the Unity Explorer menu we would need to let the button action pass through when it's open.
            if (FreeCamPanel.ShouldOverrideInput() && !(button == 0 && UIManager.ShowMenu)){
                __result = false;
            }
        }

        public static void OverrideMouseButtonDown(ref bool __result, ref int button)
        {
            getMouseButtonDown[button] = __result;
            // Since CinematicUnityExplorer uses Unity's native UI for its menu, we can't switch off the mouse interaction with it on this wrapper.
            // Therefore, if we still want to interact with the Unity Explorer menu we would need to let the button action pass through when it's open.
            if (FreeCamPanel.ShouldOverrideInput() && !(button == 0 && UIManager.ShowMenu)){
                __result = false;
            }
        }
    }
}
