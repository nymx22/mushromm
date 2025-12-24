// using UnityEngine;

// /// <summary>
// /// Simple standalone test to verify speaker connection.
// /// Generates continuous low-frequency audio for vibration testing.
// /// Attach to any GameObject and press Play.
// /// </summary>
// public class SimpleSpeakerTest : MonoBehaviour
// {
//     [Header("Test Settings")]
//     [SerializeField, Range(20f, 100f), Tooltip("Frequency in Hz (lower = more vibration)")]
//     private float testFrequency = 40f;
    
//     [SerializeField, Range(0f, 1f), Tooltip("Volume level (0-1)")]
//     private float testVolume = 0.5f;
    
//     [SerializeField, Tooltip("Start playing automatically when scene loads")]
//     private bool autoStart = true;
    
//     [SerializeField, Tooltip("Show on-screen controls")]
//     private bool showGUI = true;

//     private AudioSource audioSource;
//     private double phase = 0.0;
//     private double cachedSampleRate = 44100.0;
//     private bool isPlaying = false;
//     private int filterReadCallCount = 0;
    
//     void Start()
//     {
//         // Cache sample rate on main thread (required for OnAudioFilterRead)
//         cachedSampleRate = AudioSettings.outputSampleRate;
//         Debug.Log($"SimpleSpeakerTest: Sample rate = {cachedSampleRate} Hz");
//         Debug.Log($"SimpleSpeakerTest: Audio output device = {AudioSettings.driverCapabilities}");
        
//         // Setup audio source
//         audioSource = GetComponent<AudioSource>();
//         if (audioSource == null)
//         {
//             audioSource = gameObject.AddComponent<AudioSource>();
//             Debug.Log("SimpleSpeakerTest: Created AudioSource component");
//         }
        
//         audioSource.playOnAwake = false;
//         audioSource.loop = true;
//         audioSource.volume = 1f; // Set to 1, we control volume in OnAudioFilterRead
//         audioSource.spatialBlend = 0f; // 2D sound (not 3D)
//         audioSource.priority = 0; // High priority
//         audioSource.bypassEffects = true;
//         audioSource.bypassListenerEffects = true;
//         audioSource.bypassReverbZones = true;
        
//         // Create a dummy AudioClip to ensure AudioSource can play
//         // OnAudioFilterRead will override the audio data
//         int samples = (int)(cachedSampleRate * 1.0f); // 1 second of audio
//         AudioClip dummyClip = AudioClip.Create("SpeakerTestClip", samples, 2, (int)cachedSampleRate, true, OnAudioRead);
//         audioSource.clip = dummyClip;
        
//         // Check for AudioListener (required for audio playback)
//         AudioListener listener = FindObjectOfType<AudioListener>();
//         if (listener == null)
//         {
//             Debug.LogWarning("SimpleSpeakerTest: No AudioListener found in scene! Audio won't play without one.");
//         }
//         else
//         {
//             Debug.Log($"SimpleSpeakerTest: AudioListener found on {listener.gameObject.name}");
//         }
        
//         Debug.Log("SimpleSpeakerTest: AudioSource configured");
        
//         if (autoStart)
//         {
//             StartTest();
//         }
//     }
    
//     void OnAudioRead(float[] data)
//     {
//         // This is called for the dummy clip, but OnAudioFilterRead should override it
//         // Fill with silence as fallback
//         for (int i = 0; i < data.Length; i++)
//         {
//             data[i] = 0f;
//         }
//     }
    
//     void Update()
//     {
//         // Allow real-time frequency/volume adjustment in Play mode
//         // (Changes will take effect on next audio buffer)
//     }
    
//     /// <summary>
//     /// Start the audio test
//     /// </summary>
//     public void StartTest()
//     {
//         if (audioSource != null)
//         {
//             if (!audioSource.isPlaying)
//             {
//                 audioSource.Play();
//                 isPlaying = true;
//                 filterReadCallCount = 0;
//                 Debug.Log($"SimpleSpeakerTest: Started - {testFrequency}Hz at {testVolume * 100:F0}% volume");
//                 Debug.Log($"SimpleSpeakerTest: AudioSource.isPlaying = {audioSource.isPlaying}");
//             }
//             else
//             {
//                 Debug.LogWarning("SimpleSpeakerTest: Already playing!");
//             }
//         }
//         else
//         {
//             Debug.LogError("SimpleSpeakerTest: AudioSource is null!");
//         }
//     }
    
//     /// <summary>
//     /// Stop the audio test
//     /// </summary>
//     public void StopTest()
//     {
//         if (audioSource != null && audioSource.isPlaying)
//         {
//             audioSource.Stop();
//             isPlaying = false;
//             phase = 0.0; // Reset phase
//             Debug.Log("SimpleSpeakerTest: Stopped");
//         }
//     }
    
//     /// <summary>
//     /// Toggle play/stop
//     /// </summary>
//     public void ToggleTest()
//     {
//         if (isPlaying)
//             StopTest();
//         else
//             StartTest();
//     }
    
//     /// <summary>
//     /// Called by Unity's audio system to generate audio samples
//     /// </summary>
//     void OnAudioFilterRead(float[] data, int channels)
//     {
//         if (!isPlaying) 
//         {
//             // Fill with silence when not playing
//             for (int i = 0; i < data.Length; i++)
//             {
//                 data[i] = 0f;
//             }
//             return;
//         }
        
//         filterReadCallCount++;
//         if (filterReadCallCount == 1)
//         {
//             Debug.Log($"SimpleSpeakerTest: OnAudioFilterRead called! Channels: {channels}, Data length: {data.Length}");
//         }
        
//         double increment = testFrequency / cachedSampleRate;
        
//         for (int i = 0; i < data.Length; i += channels)
//         {
//             // Generate sine wave at test frequency
//             float sample = Mathf.Sin((float)phase * 2f * Mathf.PI) * testVolume;
            
//             // Apply to all channels (mono -> stereo if needed)
//             for (int c = 0; c < channels; c++)
//             {
//                 data[i + c] = sample;
//             }
            
//             phase += increment;
//             if (phase > 1.0) phase -= 1.0;
//         }
//     }
    
//     void OnGUI()
//     {
//         if (!showGUI) return;
        
//         GUILayout.BeginArea(new Rect(10, 10, 350, 250));
//         GUILayout.Box("Speaker Test Controls");
        
//         GUILayout.Space(10);
//         GUILayout.Label($"Status: {(isPlaying ? "PLAYING" : "STOPPED")}", GUI.skin.label);
//         if (audioSource != null)
//         {
//             GUILayout.Label($"AudioSource.isPlaying: {audioSource.isPlaying}", GUI.skin.label);
//         }
//         GUILayout.Label($"Frequency: {testFrequency:F1} Hz", GUI.skin.label);
//         GUILayout.Label($"Volume: {testVolume * 100:F0}%", GUI.skin.label);
//         GUILayout.Label($"Sample Rate: {cachedSampleRate} Hz", GUI.skin.label);
//         if (filterReadCallCount > 0)
//         {
//             GUILayout.Label($"Filter calls: {filterReadCallCount}", GUI.skin.label);
//         }
        
//         GUILayout.Space(10);
        
//         if (GUILayout.Button(isPlaying ? "Stop Test" : "Start Test", GUILayout.Height(30)))
//         {
//             ToggleTest();
//         }
        
//         GUILayout.Space(10);
        
//         GUILayout.Label("Frequency (Hz):");
//         testFrequency = GUILayout.HorizontalSlider(testFrequency, 20f, 100f);
//         GUILayout.Label($"  {testFrequency:F1} Hz");
        
//         GUILayout.Space(5);
        
//         GUILayout.Label("Volume:");
//         testVolume = GUILayout.HorizontalSlider(testVolume, 0f, 1f);
//         GUILayout.Label($"  {testVolume * 100:F0}%");
        
//         GUILayout.Space(10);
//         GUILayout.Label("Tip: Lower frequency (20-40Hz) = more vibration", GUI.skin.box);
        
//         GUILayout.EndArea();
//     }
    
//     void OnDisable()
//     {
//         if (audioSource != null)
//         {
//             audioSource.Stop();
//         }
//     }
// }

