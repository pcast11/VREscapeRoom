using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class ViveInput : MonoBehaviour {

    [SteamVR_DefaultAction("InteractUI")]

    public SteamVR_Action_Boolean clickAction;
    

    public SteamVR_Action_Boolean touchpadAction;

	// Update is called once per frame
	void Update () {
        if (SteamVR_Input._default.inActions.Teleport.GetStateDown(SteamVR_Input_Sources.Any))
        {
            print("Teleport down");
        }

        if (SteamVR_Input._default.inActions.InteractUI.GetStateDown(SteamVR_Input_Sources.Any))
        {
            print("Interact down");
        }

        bool triggerValue = clickAction.GetChanged(SteamVR_Input_Sources.Any);

        if (triggerValue)
        {
            print(triggerValue);
        }

        bool touchpadValue = touchpadAction.GetChanged(SteamVR_Input_Sources.Any);

        if (touchpadValue)
        {
            print(touchpadValue);
        }

        /*
        Vector2 touchpadValue = touchpadAction.GetAxis(SteamVR_Input_Sources.Any);

        if(touchpadValue != Vector2.zero)
        {
            print(touchpadValue);
        }
        */
    }
}
