using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "DOOHServerConfig", menuName = "DOOH/Server Config")]
public class DOOHServerConfig : ScriptableObject
{
    [Header("Environment")]
    [SerializeField] private string environmentName = "Local";

    [Header("Server")]
    [SerializeField] private string baseUrl = "http://127.0.0.1:8000";
    [FormerlySerializedAs("encountersPath")]
    [SerializeField] private string profilesPath = "/profiles/recent";

    [Header("Request")]
    [SerializeField] private float timeoutSeconds = 10f;
    [SerializeField] private float pollingIntervalSeconds = 5f;

    public string EnvironmentName => environmentName;
    public string BaseUrl => baseUrl;
    public string ProfilesPath => profilesPath;
    public float TimeoutSeconds => timeoutSeconds;
    public float PollingIntervalSeconds => pollingIntervalSeconds;
}
