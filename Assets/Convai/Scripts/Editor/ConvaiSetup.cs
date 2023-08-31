
#if UNITY_EDITOR

using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;

using UnityEditor;
using System.IO;
using System.Collections.Generic;

using Newtonsoft.Json;
using System.Text;
using System.Net;
using System;
using System.Net.Http;
using System.Threading.Tasks;

using Convai;

#if UNITY_2021_1_OR_NEWER
// code for Unity 2019.1 or newer
#else
    // code for older Unity versions
#endif

public class ConvaiSetup : EditorWindow
{
    [MenuItem("Convai/Convai Setup")]
    public static void SetupConvaiAPIKey()
    {
        ConvaiSetup wnd = GetWindow<ConvaiSetup>();
        wnd.titleContent = new GUIContent("Convai Setup");
    }

    [MenuItem("Convai/Documentation")]
    public static void OpenDocumentation()
    {
        Application.OpenURL("https://docs.convai.com/plugins-and-integrations/unity-plugin");
    }
    public class UpdateSource
    {
        [JsonProperty("referral_source")] public string referral_source;

        public UpdateSource(string referral_source)
        {
            this.referral_source = referral_source;
        }
    }

    public class referralSourceStatus
    {
        [JsonProperty("referral_source_status")] public string referral_source_status;
        [JsonProperty("status")] public string status;
    }

    async Task<string> checkReferralStatus(string url, string apiKey)
    {
        // Create a new HttpWebRequest object
        var request = WebRequest.Create(url);
        request.Method = "post";

        // Set the request headers
        request.ContentType = "application/json";

        string bodyJsonString = "{}";

        // Convert the json string to bytes
        byte[] jsonBytes = Encoding.UTF8.GetBytes(bodyJsonString);

        referralSourceStatus referralStatus;

        request.Headers.Add("CONVAI-API-KEY", apiKey);

        // Write the data to the request stream
        using (Stream requestStream = await request.GetRequestStreamAsync())
        {
            await requestStream.WriteAsync(jsonBytes, 0, jsonBytes.Length);
        }

        // Get the response from the server
        try
        {
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                using (Stream streamResponse = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(streamResponse))
                    {
                        string responseContent = reader.ReadToEnd();

                        referralStatus = JsonConvert.DeserializeObject<referralSourceStatus>(responseContent);
                    }
                }
                return referralStatus.referral_source_status;
            }
        }
        catch (WebException e)
        {
            Debug.LogError(e.Message + "\nPlease check if API Key is correct.");
            return "";
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            return "";
        }
    }

    async Task<bool> beginButtonTask(string apiKey)
    {
        ConvaiAPIKeySetup aPIKeySetup = CreateInstance<ConvaiAPIKeySetup>();

        aPIKeySetup.APIKey = apiKey;

        if (apiKey != "")
        {
            string referralStatus = await checkReferralStatus("https://api.convai.com/user/referral-source-status", apiKey);

            if (referralStatus != "")
            {
                if (referralStatus == "undefined")
                {
                    if (!File.Exists("Assets/Resources/ConvaiAPIKey.asset"))
                    {
                        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                        {
                            AssetDatabase.CreateFolder("Assets", "Resources");
                        }

                        AssetDatabase.CreateAsset(aPIKeySetup, "Assets/Resources/ConvaiAPIKey.asset");
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }
                    else
                    {
                        AssetDatabase.DeleteAsset("Assets/Resources/APIKey.asset");
                        AssetDatabase.Refresh();
                        AssetDatabase.CreateAsset(aPIKeySetup, "Assets/Resources/ConvaiAPIKey.asset");
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }

                    return true;
                }
                else
                {
                    if (!File.Exists("Assets/Resources/APIKey.asset"))
                    {
                        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                        {
                            AssetDatabase.CreateFolder("Assets", "Resources");
                        }

                        AssetDatabase.CreateAsset(aPIKeySetup, "Assets/Resources/ConvaiAPIKey.asset");
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }
                    else
                    {
                        AssetDatabase.DeleteAsset("Assets/Resources/APIKey.asset");
                        AssetDatabase.Refresh();
                        AssetDatabase.CreateAsset(aPIKeySetup, "Assets/Resources/ConvaiAPIKey.asset");
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }

                    return false;
                }
            }
            else
            {
                Debug.LogError("Please enter a valid API Key.");
                return false;
            }
        }
        else
        {
            Debug.LogError("Please enter a valid API Key.");
            return false;
        }
    }

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        VisualElement page2 = new ScrollView();

        root.Add(new Label(""));

        Image convaiLogoImage = new Image();
        convaiLogoImage.image = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Convai/Images/color.png");
        convaiLogoImage.style.height = 100;

        convaiLogoImage.style.paddingBottom = 10;
        convaiLogoImage.style.paddingTop = 10;
        convaiLogoImage.style.paddingRight = 10;
        convaiLogoImage.style.paddingLeft = 10;

        root.Add(convaiLogoImage);

        Label convaiSetupLabel = new Label("Enter your API Key:");
        convaiSetupLabel.style.fontSize = 16;

        TextField APIKeyTextField = new TextField("", -1, false, true, '*');

        Button beginButton = new Button(async () =>
        {
            await beginButtonTask(APIKeyTextField.text);
            Close();
        });

        beginButton.text = "Begin!";

        beginButton.style.fontSize = 16;
        beginButton.style.unityFontStyleAndWeight = FontStyle.Bold;
        beginButton.style.alignSelf = Align.Center;

        beginButton.style.paddingBottom = 10;
        beginButton.style.paddingLeft = 30;
        beginButton.style.paddingRight = 30;
        beginButton.style.paddingTop = 10;

        Button docsLink = new Button(() =>
        {
            Application.OpenURL("https://docs.convai.com/api-docs/plugins/unity-plugin");
        });

        docsLink.text = "How do I find my API key?";
        docsLink.style.alignSelf = Align.Center;
        docsLink.style.paddingBottom = 5;
        docsLink.style.paddingLeft = 50;
        docsLink.style.paddingRight = 50;
        docsLink.style.paddingTop = 5;

        page2.Add(convaiSetupLabel);
        page2.Add(new Label(""));
        page2.Add(APIKeyTextField);
        page2.Add(new Label(""));
        page2.Add(beginButton);
        page2.Add(new Label(""));
        page2.Add(docsLink);

        page2.style.marginBottom = 20;
        page2.style.marginLeft = 20;
        page2.style.marginRight = 20;
        page2.style.marginTop = 20;

        root.Add(page2);
    }
}

#endif