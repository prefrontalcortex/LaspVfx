using UnityEditor;
using UnityEngine;

namespace UnityVFXReactiveAudio.Editor
{
    [CustomEditor(typeof(AudioCapture))]
    public class AudioCaptureEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var capture = target as AudioCapture;
            if (capture == null) return;
            
            EditorGUILayout.HelpBox("Add this Component to an AudioSource or AudioListener to capture audio data. To track AudioMixer influences, please add this component to the AudioListener.", MessageType.Info);
            var isValid = capture.gameObject.GetComponent<AudioSource>()|| capture.gameObject.GetComponent<AudioListener>();
            if (!isValid)
                EditorGUILayout.HelpBox("This GameObject does not have an AudioSource or AudioListener component.", MessageType.Error);
            
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("Capture Info", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Is Ready", capture.IsReady.ToString());
                EditorGUILayout.LabelField("Channel Count", capture.ChannelCount.ToString());
                EditorGUILayout.LabelField("Sample Rate", capture.SampleRate.ToString());
                for (int i = 0; i < capture.ChannelCount; i++)
                    EditorGUILayout.LabelField($"Channel {i} Level", capture.GetChannelLevel(i).ToString());
                EditorGUI.indentLevel--;
                Repaint();
            }
        }
        
    }
}