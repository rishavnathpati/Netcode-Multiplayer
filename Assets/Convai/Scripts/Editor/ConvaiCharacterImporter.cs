
#if UNITY_EDITOR

using UnityEngine;
using UnityEngine.UIElements;

using UnityEditor;
using Newtonsoft.Json;

using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System;

using UnityEditor.Animations;

using Convai;
using ReadyPlayerMe;
using TMPro;


using UnityEditor.UIElements;

public class ConvaiCharacterImporter : EditorWindow
{
    [MenuItem("Convai/Character Importer")]
    public static void CharacterImporter()
    {
        ConvaiCharacterImporter wnd = GetWindow<ConvaiCharacterImporter>();
        wnd.titleContent = new GUIContent("Character Importer");
    }

    class GetRequest
    {
        [JsonProperty("charID")] public string charID;

        public GetRequest(string charID)
        {
            this.charID = charID;
        }
    }

    class ModelDetails
    {
        public string modelType;
        public string modelLink;
        public string modelPlaceholder;
    }

    class GetResponse
    {
        public string character_name;
        public string user_id;
        public string character_id;
        public string voice_type;
        public string timestamp;
        public string[] character_actions;
        public string[] character_emotions;
        public ModelDetails model_details;
        public string backstory;
    }

    async void DownloadCharacter(string characterID)
    {
        GetRequest getRequest = new GetRequest(characterID);

        string stringGetRequest = JsonConvert.SerializeObject(getRequest);

        ConvaiAPIKeySetup APIKeyScriptableObject = null;

        if (File.Exists("Assets/Resources/ConvaiAPIKey.asset"))
        {
            APIKeyScriptableObject = Resources.Load<ConvaiAPIKeySetup>("ConvaiAPIKey");
        }
        else
        {
            Debug.LogError("No API Key file found. Please add the API Key.");
        }

        // Create a new HttpWebRequest object
        var request = WebRequest.Create("https://api.convai.com/character/get");
        request.Method = "post";

        // Set the request headers
        request.ContentType = "application/json";

        // Convert the json string to bytes
        byte[] jsonBytes = Encoding.UTF8.GetBytes(stringGetRequest);

        request.Headers.Add("CONVAI-API-KEY", APIKeyScriptableObject.APIKey);

        string modelLink = "";
        string characterName = "";

        GameObject gameObject = null;

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

                        //Debug.Log($"{responseContent}");
                        var getResponseContent = JsonConvert.DeserializeObject<GetResponse>(responseContent);
                        //Debug.Log(getResponseContent.model_details.modelLink);
                        modelLink = getResponseContent.model_details.modelLink;
                        characterName = getResponseContent.character_name;

                        AvatarLoader avatarLoader = new AvatarLoader();

                        if (File.Exists("Assets/Convai/RPMSetupAssets/Avatar Config.asset"))
                        {
                            avatarLoader.AvatarConfig = AssetDatabase.LoadAssetAtPath<AvatarConfig>("Assets/Convai/RPMSetupAssets/Avatar Config.asset");
                        }

                        avatarLoader.SaveInProjectFolder = true;

                        avatarLoader.OnCompleted += (_, args) =>
                        {
                            gameObject = args.Avatar;
                            AvatarAnimatorHelper.SetupAnimator(args.Metadata.BodyType, gameObject);

                            gameObject.tag = "Character";

                            gameObject.AddComponent<CapsuleCollider>();

                            // setup capsule collider
                            gameObject.GetComponent<CapsuleCollider>().center = new Vector3(0, 0.9f, 0);
                            gameObject.GetComponent<CapsuleCollider>().radius = 0.3f;
                            gameObject.GetComponent<CapsuleCollider>().height = 1.8f;
                            gameObject.GetComponent<CapsuleCollider>().isTrigger = true;

                            gameObject.AddComponent<AudioSource>();

                            gameObject.AddComponent<Animator>();

                            if (File.Exists("Assets/Convai/Animators/NPC Animator.controller"))
                            {
                                gameObject.GetComponent<Animator>().runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Convai/Animators/NPC Animator.controller");
                            }

                            if (File.Exists("Assets/Convai/AnimationAvatars/FeminineAnimationAvatar.asset"))
                            {
                                gameObject.GetComponent<Animator>().avatar = AssetDatabase.LoadAssetAtPath<Avatar>("Assets/Convai/AnimationAvatars/FeminineAnimationAvatar.asset");
                            }

                            gameObject.AddComponent<ConvaiNPC>();
                            gameObject.GetComponent<ConvaiNPC>().sessionID = "-1";
                            gameObject.GetComponent<ConvaiNPC>().CharacterID = characterID;

                            gameObject.AddComponent<OVRLipSync>();

                            gameObject.AddComponent<OVRLipSyncContext>();
                            gameObject.GetComponent<OVRLipSyncContext>().audioLoopback = true;

                            gameObject.AddComponent<OVRLipSyncContextMorphTarget>();
                            gameObject.GetComponent<OVRLipSyncContextMorphTarget>().skinnedMeshRenderer = gameObject.transform.GetChild(2).GetComponent<SkinnedMeshRenderer>();
                            gameObject.GetComponent<OVRLipSyncContextMorphTarget>().visemeToBlendTargets = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };


                            /*
                            gameObject.AddComponent<ConvaiLipSync>();
                            gameObject.GetComponent<ConvaiLipSync>().mouthMeshRenderer = gameObject.transform.GetChild(2).GetComponent<SkinnedMeshRenderer>();
                            gameObject.GetComponent<ConvaiLipSync>().mouthOpenWeight
                            = 3.0f;
                            gameObject.GetComponent<ConvaiLipSync>().indexOfMouthOpen = 0;
                            gameObject.GetComponent<ConvaiLipSync>().teethMeshRenderer = gameObject.transform.GetChild(3).GetComponent<SkinnedMeshRenderer>();
                            gameObject.GetComponent<ConvaiLipSync>().teethOpenWeight
                            = 3.0f;
                            gameObject.GetComponent<ConvaiLipSync>().indexOfTeethOpen = 0;
                            */

                            gameObject.name = $"Convai NPC {characterName}";

                            EditorUtilities.CreatePrefab(gameObject, $"Assets/Convai/Prefabs/{gameObject.name}.prefab");
                        };

                        avatarLoader.LoadAvatar(modelLink);
                    }
                }
            }
        }
        catch (WebException e)
        {
            Debug.LogError(e.Message + "\nPlease check if Character ID is correct.");
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }

        /*
        string glbFilePath = Path.Combine("Assets", "Convai Character Models", Path.GetFileName(modelLink));

        // Download the GLB model using HttpClient
        using (var httpClient = new HttpClient())
        {
            var glbBytes = await httpClient.GetByteArrayAsync(modelLink);

            if (!AssetDatabase.IsValidFolder("Assets/Convai Character Models"))
            {
                AssetDatabase.CreateFolder("Assets", "Convai Character Models");
            }

            File.WriteAllBytes(glbFilePath, glbBytes);
            AssetDatabase.Refresh();

            GameObject result = Importer.LoadFromFile(glbFilePath);

            GameObject gameObject = new GameObject(characterID);

            result.transform.parent = gameObject.transform;

            for (int i = 0; i < result.transform.childCount; i++)
            {
                if (result.transform.GetChild(i).name == "Wolf3D_Avatar")
                {
                    result.transform.GetChild(i).name = "Renderer_Avatar";
                    result.transform.GetChild(i).transform.parent = gameObject.transform;
                }
            }

            gameObject.tag = "Character";

            gameObject.AddComponent<CapsuleCollider>();
            // setup capsule collider
            gameObject.GetComponent<CapsuleCollider>().center = new Vector3(0, 0.9f, 0);
            gameObject.GetComponent<CapsuleCollider>().radius = 0.3f;
            gameObject.GetComponent<CapsuleCollider>().height = 1.8f;
            gameObject.GetComponent<CapsuleCollider>().isTrigger = true;

            gameObject.AddComponent<AudioSource>();

            gameObject.AddComponent<Animator>();
            if (File.Exists("Assets/Convai/Animators/NPC Animator.controller"))
            {
                gameObject.GetComponent<Animator>().runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Convai/Animators/NPC Animator.controller");
            }
            if (File.Exists("Assets/Convai/AnimationAvatars/FeminineAnimationAvatar.asset"))
            {
                gameObject.GetComponent<Animator>().avatar = AssetDatabase.LoadAssetAtPath<Avatar>("Assets/Convai/AnimationAvatars/FeminineAnimationAvatar.asset");
            }
            // setup animator Assets/Convai/Animators

            gameObject.AddComponent<ConvaiNPC>();
            gameObject.GetComponent<ConvaiNPC>().sessionID = "-1";
            gameObject.GetComponent<ConvaiNPC>().CharacterID = characterID;

            Close();
        }
        */

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

        Label convaiCharacterIDLabel = new Label("Enter your Character ID: ");
        convaiCharacterIDLabel.style.fontSize = 16;

        TextField CharacterIDTextField = new TextField();

        Button downloadButton = new Button(() =>
        {
            DownloadCharacter(CharacterIDTextField.text);
        });

        downloadButton.text = "Import!";

        downloadButton.style.fontSize = 16;
        downloadButton.style.unityFontStyleAndWeight = FontStyle.Bold;
        downloadButton.style.alignSelf = Align.Center;

        downloadButton.style.paddingBottom = 10;
        downloadButton.style.paddingLeft = 30;
        downloadButton.style.paddingRight = 30;
        downloadButton.style.paddingTop = 10;

        Button docsLink = new Button(() =>
        {
            Application.OpenURL("https://docs.convai.com/api-docs-restructure/plugins-and-integrations/unity-plugin/creating-characters");
        });

        docsLink.text = "How do I create a character?";
        docsLink.style.alignSelf = Align.Center;
        docsLink.style.paddingBottom = 5;
        docsLink.style.paddingLeft = 50;
        docsLink.style.paddingRight = 50;
        docsLink.style.paddingTop = 5;

        page2.Add(convaiCharacterIDLabel);
        page2.Add(new Label(""));
        page2.Add(CharacterIDTextField);
        page2.Add(new Label(""));
        page2.Add(downloadButton);
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