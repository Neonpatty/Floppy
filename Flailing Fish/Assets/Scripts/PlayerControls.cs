// GENERATED AUTOMATICALLY FROM 'Assets/Scripts/PlayerControls.inputactions'

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public class @PlayerControls : IInputActionCollection, IDisposable
{
    public InputActionAsset asset { get; }
    public @PlayerControls()
    {
        asset = InputActionAsset.FromJson(@"{
    ""name"": ""PlayerControls"",
    ""maps"": [
        {
            ""name"": ""MainControls"",
            ""id"": ""84a002d9-e5b1-4825-a067-b431ea5ee311"",
            ""actions"": [
                {
                    ""name"": ""Flop"",
                    ""type"": ""Button"",
                    ""id"": ""a9d76eb0-fb65-49e8-a96b-654b87203163"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""8c60fd45-4dae-49dd-a680-b27c46fe58ec"",
                    ""path"": ""<Keyboard>/space"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Flop"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""268a342f-e00f-49da-8766-712feae05cba"",
                    ""path"": ""<Touchscreen>/primaryTouch/tap"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Flop"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": []
}");
        // MainControls
        m_MainControls = asset.FindActionMap("MainControls", throwIfNotFound: true);
        m_MainControls_Flop = m_MainControls.FindAction("Flop", throwIfNotFound: true);
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(asset);
    }

    public InputBinding? bindingMask
    {
        get => asset.bindingMask;
        set => asset.bindingMask = value;
    }

    public ReadOnlyArray<InputDevice>? devices
    {
        get => asset.devices;
        set => asset.devices = value;
    }

    public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

    public bool Contains(InputAction action)
    {
        return asset.Contains(action);
    }

    public IEnumerator<InputAction> GetEnumerator()
    {
        return asset.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Enable()
    {
        asset.Enable();
    }

    public void Disable()
    {
        asset.Disable();
    }

    // MainControls
    private readonly InputActionMap m_MainControls;
    private IMainControlsActions m_MainControlsActionsCallbackInterface;
    private readonly InputAction m_MainControls_Flop;
    public struct MainControlsActions
    {
        private @PlayerControls m_Wrapper;
        public MainControlsActions(@PlayerControls wrapper) { m_Wrapper = wrapper; }
        public InputAction @Flop => m_Wrapper.m_MainControls_Flop;
        public InputActionMap Get() { return m_Wrapper.m_MainControls; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(MainControlsActions set) { return set.Get(); }
        public void SetCallbacks(IMainControlsActions instance)
        {
            if (m_Wrapper.m_MainControlsActionsCallbackInterface != null)
            {
                @Flop.started -= m_Wrapper.m_MainControlsActionsCallbackInterface.OnFlop;
                @Flop.performed -= m_Wrapper.m_MainControlsActionsCallbackInterface.OnFlop;
                @Flop.canceled -= m_Wrapper.m_MainControlsActionsCallbackInterface.OnFlop;
            }
            m_Wrapper.m_MainControlsActionsCallbackInterface = instance;
            if (instance != null)
            {
                @Flop.started += instance.OnFlop;
                @Flop.performed += instance.OnFlop;
                @Flop.canceled += instance.OnFlop;
            }
        }
    }
    public MainControlsActions @MainControls => new MainControlsActions(this);
    public interface IMainControlsActions
    {
        void OnFlop(InputAction.CallbackContext context);
    }
}
