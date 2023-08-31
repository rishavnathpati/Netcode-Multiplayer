using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Convai
{
    [CreateAssetMenu(fileName = "API Key", menuName = "Convai/API Key")]
    public class ConvaiAPIKeySetup : ScriptableObject
    {
        public string APIKey;
    }
}
