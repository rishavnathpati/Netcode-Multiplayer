using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Convai.gRPCAPI;

public class ConvaiTalkButtonHandler : Button
{

    private ConvaiGRPCAPI grpcAPI;

    protected override void Awake()
    {
        base.Awake();
        grpcAPI = FindObjectOfType<ConvaiGRPCAPI>();
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        grpcAPI.activeConvaiNPC.StartListening();
        Debug.Log(gameObject.name + " Was Clicked.");
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        grpcAPI.activeConvaiNPC.StopListening();
        Debug.Log(gameObject.name + " Was Released.");
    }
}
