using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum UIType
{
    Subtitle,
    QuestionAnswer,
}


public class ConvaiChatUIHandler : MonoBehaviour
{
    public string characterName = "Character";
    public string characterText;
    public Color characterTextColor = new Color(1f, 1f, 1f);

    public string userName = "User";
    public string userText;
    public Color userTextColor = new Color(1f, 1f, 1f);

    public bool isCharacterTalking = false;
    public bool isUserTalking = false;

    public TextMeshProUGUI userTextField;
    public TextMeshProUGUI characterTextField;

    public GameObject userTalkingMarker;

    public UIType uitype;

    // Start is called before the first frame update
    void Start()
    {
        if (userName == "")
        {
            userName = "User";
        }

        if (characterName == "")
        {
            characterName = "Character";
        }

        switch (uitype)
        {
            case UIType.Subtitle:
                {
                    break;
                }

            case UIType.QuestionAnswer:
                {
                    break;
                }

            default:
                {
                    break;
                }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isUserTalking)
        {
            userTalkingMarker?.SetActive(true);
        }
        else
        {
            userTalkingMarker?.SetActive(false);
        }

        switch (uitype)
        {
            case UIType.Subtitle:
                {
                    if (isCharacterTalking)
                    {
                        if (characterText != "")
                        {
                            userTextField.text = $"<color=#{UnityEngine.ColorUtility.ToHtmlStringRGB(characterTextColor)}><b>{characterName}</b>: {characterText}</color>";
                        }
                        else
                        {
                            userTextField.text = "";
                        }
                    }
                    else
                    {
                        if (userText != "")
                        {
                            userTextField.text = $"<color=#{UnityEngine.ColorUtility.ToHtmlStringRGB(userTextColor)}><b>{userName}</b>: {userText}</color>";
                        }
                        else
                        {
                            userTextField.text = "";
                        }
                    }

                    break;
                }

            case UIType.QuestionAnswer:
                {
                    userTextField.text = $"{userText}";

                    characterTextField.text = $"<color=#{UnityEngine.ColorUtility.ToHtmlStringRGB(characterTextColor)}>{characterText}</color>";

                    break;
                }

            default:
                {
                    break;
                }
        }
    }

}
