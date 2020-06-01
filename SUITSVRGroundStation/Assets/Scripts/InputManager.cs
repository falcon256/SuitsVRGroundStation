using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI; 

public class InputManager : MonoBehaviour
{
    private static InputManager singleton;

    public Text[] buttons;

    public UnityEvent[] buttonActions;

    public GameObject arrowPrefab;
    public GameObject circlePrefab;
    public GameObject rectanglePrefab;
    public GameObject textPrefab;
    public GameObject xPrefab;
    public GameObject rightHand;

    public bool isDrawing = false;
    public bool isPlacingIcons = false; 

    private int currentOptionSelected; 

    private void Start()
    {
        if (!singleton)
            singleton = this;
        else
            Debug.LogError("InputManager Singleton duplication.");
        currentOptionSelected = 0;
        buttons[currentOptionSelected].color = Color.red;
        buttonActions[currentOptionSelected].Invoke();
    }

    public static InputManager getSingleton()
    {
        return singleton;
    }

    private void toggleOption()
    {
        buttons[currentOptionSelected].color = Color.black;
        buttons[currentOptionSelected].fontStyle = FontStyle.Normal; 
        if (currentOptionSelected >= buttons.Length - 1)
        {
            currentOptionSelected = 0; 
        } else
        {
            currentOptionSelected++; 
        }
        buttons[currentOptionSelected].color = Color.red;
        buttons[currentOptionSelected].fontStyle = FontStyle.Bold;
        buttonActions[currentOptionSelected].Invoke(); 
    }

    private void Update()
    {
        if (OVRInput.GetUp(OVRInput.Button.Two) || Input.GetKeyUp(KeyCode.DownArrow)) 
        {
            toggleOption();
        }

        if (isPlacingIcons)
        {
            if (OVRInput.GetUp(OVRInput.Button.SecondaryHandTrigger) || Input.GetKeyUp(KeyCode.Space))
            {
                Vector3 placementLocation = rightHand.transform.position;
                Quaternion placementRotation = rightHand.transform.rotation;
                foreach (Transform childTransform in rightHand.transform)
                {                   
                    GameObject child = childTransform.gameObject;
                    //GameObject newIcon = Instantiate(child);
                    //newIcon.transform.position = child.transform.position;
                    //newIcon.transform.rotation = child.transform.rotation;
                    //newIcon.transform.localScale = new Vector3(1,1,1); 
                    //newIcon.transform.parent = null; 
                    placementLocation = child.transform.position;
                    placementRotation = child.transform.rotation;
                }
                
                
                switch (currentOptionSelected)
                {
                    case (1):
                        PhotonRPCLinks.getSingleton().sendIconSpawn(placementLocation, placementRotation, PhotonRPCLinks.iconType.ARROW);
                        break;
                    case (2):
                        PhotonRPCLinks.getSingleton().sendIconSpawn(placementLocation, placementRotation, PhotonRPCLinks.iconType.RECTANGLE);
                        break;
                    case (3):
                        PhotonRPCLinks.getSingleton().sendIconSpawn(placementLocation, placementRotation, PhotonRPCLinks.iconType.CIRCLE);
                        break;
                    case (5):
                        PhotonRPCLinks.getSingleton().sendIconSpawn(placementLocation, placementRotation, PhotonRPCLinks.iconType.X);
                        break;
                    default:
                        Debug.LogError("Not sure how this happenened, but good job.");
                        break;
                }
            }
        }
    }

    public void attachIconToHand(GameObject icon)
    {

        rightHand.GetComponent<MeshRenderer>().enabled = false;
        GameObject newIcon = Instantiate(icon);
        foreach (Transform childTransform in rightHand.transform)
        {
            GameObject child = childTransform.gameObject;
            Destroy(child); 
        }
        newIcon.transform.SetParent(rightHand.transform, true);
        newIcon.transform.localPosition = new Vector3(0, 0, 0);
    }

    public void arrowMode()
    {
        if (rightHand.GetComponent<MeshRenderer>())
            rightHand.GetComponent<MeshRenderer>().enabled = false;
        isDrawing = false;
        isPlacingIcons = true; 
        attachIconToHand(arrowPrefab); 
    }

    public void circleMode()
    {
        if (rightHand.GetComponent<MeshRenderer>())
            rightHand.GetComponent<MeshRenderer>().enabled = false;
        isDrawing = false;
        isPlacingIcons = true; 
        attachIconToHand(circlePrefab); 
    }

    public void xMode()
    {
        if (rightHand.GetComponent<MeshRenderer>())
            rightHand.GetComponent<MeshRenderer>().enabled = false;
        isDrawing = false;
        isPlacingIcons = true; 
        attachIconToHand(xPrefab); 
    }

    public void rectangleMode()
    {
        if (rightHand.GetComponent<MeshRenderer>())
            rightHand.GetComponent<MeshRenderer>().enabled = false;
        isDrawing = false;
        isPlacingIcons = true; 
        attachIconToHand(rectanglePrefab); 
    }

    public void drawMode()
    {
        if(rightHand.GetComponent<MeshRenderer>())
            rightHand.GetComponent<MeshRenderer>().enabled = true;
        isDrawing = true;
        isPlacingIcons = false;
        foreach (Transform childTransform in rightHand.transform)
        {
            GameObject child = childTransform.gameObject;
            Destroy(child);
        }
    }

    public void textBoxMode()
    {
        if (rightHand.GetComponent<MeshRenderer>())
            rightHand.GetComponent<MeshRenderer>().enabled = true;
        isDrawing = false;
        isPlacingIcons = false;
        foreach (Transform childTransform in rightHand.transform)
        {
            GameObject child = childTransform.gameObject;
            Destroy(child);
        }
        
    }
}
